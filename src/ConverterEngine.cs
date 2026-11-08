using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
namespace DiskShift;
public record ImageDetails(string Format, long VirtualSize, long FileSize);
public sealed class ConverterEngine
{
    public string Executable { get; }
    public ConverterEngine(string executable) { Executable = executable; }
    private async Task<string> Run(string[] args, CancellationToken token, Action<double>? progress = null)
    {
        var start = new ProcessStartInfo(Executable) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = Path.GetDirectoryName(Executable)! };
        foreach (var arg in args) start.ArgumentList.Add(arg);
        using var process = new Process { StartInfo = start };
        if (!process.Start()) throw new IOException("QEMU konnte nicht gestartet werden.");
        using var registration = token.Register(() => { try { if (!process.HasExited) process.Kill(true); } catch (InvalidOperationException) { } });
        async Task<string> Read(StreamReader reader)
        {
            var result = new StringBuilder(); var buffer = new char[4096]; var tail = "";
            int count;
            while ((count = await reader.ReadAsync(buffer.AsMemory(), CancellationToken.None)) > 0)
            {
                var part = new string(buffer, 0, count); result.Append(part);
                if (result.Length > 131072) result.Remove(0, result.Length - 65536);
                if (progress != null)
                {
                    var text = tail + part;
                    foreach (Match match in Regex.Matches(text, @"\(([0-9]+(?:\.[0-9]+)?)/100%\)"))
                        if (double.TryParse(match.Groups[1].Value, CultureInfo.InvariantCulture, out var value)) progress(value);
                    tail = text.Length > 80 ? text[^80..] : text;
                }
            }
            return result.ToString();
        }
        var stdout = Read(process.StandardOutput); var stderr = Read(process.StandardError);
        await process.WaitForExitAsync(CancellationToken.None);
        var output = await stdout; var error = await stderr;
        token.ThrowIfCancellationRequested();
        if (process.ExitCode != 0) throw new IOException($"QEMU meldet Fehler {process.ExitCode}:\n{error}\n{output}");
        return output;
    }
    public Task<string> Version(CancellationToken token) => Run(new[] { "--version" }, token);
    public async Task<ImageDetails> Inspect(string path, CancellationToken token)
    {
        if (!File.Exists(path)) throw new IOException("Die Quelldatei wurde nicht gefunden.");
        if (Path.GetFileName(path).StartsWith("._", StringComparison.Ordinal)) throw new IOException("Dies ist eine macOS-Metadatendatei. Bitte die eigentliche Festplattendatei ohne ._ am Anfang auswählen.");
        if (Path.GetExtension(path).Equals(".hds", StringComparison.OrdinalIgnoreCase))
        {
            var folder = Path.GetDirectoryName(path)!;
            if (Directory.GetFiles(folder, "*.hds").Count(f => !Path.GetFileName(f).StartsWith("._", StringComparison.Ordinal)) != 1) throw new IOException("Mehrere Parallels-HDS-Dateien: Snapshot- oder Teilimage-Ketten werden noch nicht unterstützt.");
            var descriptor = Path.Combine(folder, "DiskDescriptor.xml");
            if (File.Exists(descriptor))
            {
                var doc = XDocument.Load(descriptor);
                if (doc.Descendants("Shot").Count() > 1 || doc.Descendants("Image").Count() > 1)
                    throw new IOException("Das Parallels-Paket enthält mehrere Images oder Snapshots. Bitte zuerst in Parallels zusammenführen.");
            }
        }
        var output = await Run(new[] { "info", "--output=json", Path.GetFullPath(path) }, token);
        using var json = JsonDocument.Parse(output);
        var root = json.RootElement;
        if (root.TryGetProperty("backing-filename", out _) || root.TryGetProperty("full-backing-filename", out _) || (root.TryGetProperty("snapshots", out var shots) && shots.GetArrayLength() > 0))
            throw new IOException("Images mit Backing-Dateien oder internen Snapshots werden in dieser Version nicht konvertiert.");
        if (root.TryGetProperty("encrypted", out var encrypted) && encrypted.GetBoolean()) throw new IOException("Verschlüsselte Image-Container werden noch nicht unterstützt.");
        var format = root.GetProperty("format").GetString()!;
        if (!new[] { "parallels", "vhdx", "vpc", "vmdk", "qcow2", "raw" }.Contains(format)) throw new IOException($"Das Format {format} ist in dieser Version nicht freigegeben.");
        return new(format, root.GetProperty("virtual-size").GetInt64(), new FileInfo(path).Length);
    }
    public async Task Convert(string source, string target, string format, bool verify, IProgress<(double Percent, string Phase)> progress, CancellationToken token)
    {
        source = Path.GetFullPath(source); target = Path.GetFullPath(target);
        if (!new[] { "vhdx", "vmdk", "qcow2" }.Contains(format)) throw new IOException("Ungültiges Zielformat.");
        if (source.Equals(target, StringComparison.OrdinalIgnoreCase) || File.Exists(target) || Directory.Exists(target)) throw new IOException("Die Zieldatei muss neu sein und darf nicht die Quelle sein.");
        var parent = Path.GetDirectoryName(target)!;
        if (!Directory.Exists(parent)) throw new IOException("Der Zielordner existiert nicht.");
        // Keep a read handle that denies concurrent writes and replacement throughout the operation.
        using var sourceLock = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read);
        var details = await Inspect(source, token);
        progress.Report((0, "Speicherbedarf wird geprüft …"));
        // VHDX and VMDK do not implement measure. QCOW2 provides an allocation estimate;
        // add a margin for container overhead. This is not a reservation or a guarantee.
        var measure = await Run(new[] { "measure", "--output=json", "-f", details.Format, "-O", "qcow2", source }, token);
        using var measured = JsonDocument.Parse(measure);
        var required = measured.RootElement.GetProperty("required").GetInt64();
        var reserve = Math.Max(64L * 1024 * 1024, required / 10);
        var drive = new DriveInfo(Path.GetPathRoot(target)!);
        if (drive.AvailableFreeSpace < required + reserve) throw new IOException($"Zu wenig freier Speicher. Geschätzter Bedarf einschließlich Reserve: {(required + reserve) / Math.Pow(1024,3):N2} GiB.");
        var work = Path.Combine(parent, ".diskshift-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        var staged = Path.Combine(work, "image." + format);
        try
        {
            var args = new List<string> { "convert", "-p", "-f", details.Format, "-O", format };
            args.Add(source); args.Add(staged);
            progress.Report((0, "Konvertierung läuft …"));
            await Run(args.ToArray(), token, p => progress.Report((p, "Konvertierung läuft …")));
            token.ThrowIfCancellationRequested();
            if (verify)
            {
                progress.Report((0, "Datenvergleich läuft …"));
                await Run(new[] { "compare", "-p", "-f", details.Format, "-F", format, source, staged }, token, p => progress.Report((p, "Datenvergleich läuft …")));
            }
            token.ThrowIfCancellationRequested();
            File.Move(staged, target, false);
            progress.Report((100, verify ? "Fertig · Datenvergleich erfolgreich" : "Fertig · ohne Datenvergleich"));
        }
        finally
        {
            // Only remove our own staging file. Never delete the selected source or target.
            try { if (File.Exists(staged)) File.Delete(staged); if (Directory.Exists(work)) Directory.Delete(work, false); } catch (IOException) { }
        }
    }
}
