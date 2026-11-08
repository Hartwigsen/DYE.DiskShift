using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
namespace DiskShift;
public record UpdateResult(string Message, string? Url = null);
public static class Updates
{
    public const string Repository = "https://github.com/Hartwigsen/DYE.DiskShift";
    public static string Current => typeof(Updates).Assembly.GetName().Version!.ToString(3);
    public static void Open(string url) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    public static UpdateResult Parse(string json, Version current)
    {
        using var data = JsonDocument.Parse(json);
        var latest = current; string? url = null;
        foreach (var release in data.RootElement.EnumerateArray())
        {
            if (release.GetProperty("draft").GetBoolean()) continue;
            var tag = release.GetProperty("tag_name").GetString() ?? "";
            var link = release.GetProperty("html_url").GetString() ?? "";
            if (Version.TryParse(tag.TrimStart('v', 'V'), out var version) && version > latest && link.StartsWith(Repository + "/releases/tag/", StringComparison.Ordinal)) { latest = version; url = link; }
        }
        return url == null ? new("Kein neuerer Release gefunden. Testreleases werden berücksichtigt.") : new($"DYE.DiskShift {latest} ist verfügbar.", url);
    }
    public static async Task<UpdateResult> Check()
    {
        try
        {
            string json;
            var gh = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "GitHub CLI", "gh.exe");
            if (File.Exists(gh))
            {
                var start = new ProcessStartInfo(gh) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
                start.ArgumentList.Add("api"); start.ArgumentList.Add("repos/Hartwigsen/DYE.DiskShift/releases?per_page=100");
                using var process = Process.Start(start)!;
                var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                try { await process.WaitForExitAsync(timeout.Token); }
                finally { if (!process.HasExited) process.Kill(true); }
                json = await output; await error;
                if (process.ExitCode != 0) return new("Updateprüfung nicht möglich. Für das private Repository muss die lokale GitHub-CLI mit einem berechtigten Konto angemeldet sein.");
            }
            else
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
                client.DefaultRequestHeaders.UserAgent.ParseAdd("DYE.DiskShift/" + Current);
                using var response = await client.GetAsync("https://api.github.com/repos/Hartwigsen/DYE.DiskShift/releases?per_page=100");
                if (!response.IsSuccessStatusCode) return new("Updateprüfung nicht möglich: privates Repository oder eingeschränkter GitHub-Zugriff. Releases im Browser öffnen oder GitHub-CLI anmelden.");
                json = await response.Content.ReadAsStringAsync();
            }
            return Parse(json, Version.Parse(Current));
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or JsonException or OperationCanceledException or System.ComponentModel.Win32Exception or InvalidOperationException or FormatException)
        { return new("Updateprüfung derzeit nicht möglich. Netzwerk oder GitHub-Zugriff prüfen."); }
    }
}

