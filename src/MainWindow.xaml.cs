using System.IO;
using System.Windows;
using Microsoft.Win32;
namespace DiskShift;
public partial class MainWindow : Window
{
    private string? source;
    private string? target;
    private ImageDetails? details;
    private CancellationTokenSource? operation;
    private readonly ConverterEngine engine = new(Path.Combine(AppContext.BaseDirectory, "qemu", "qemu-img.exe"));
    private string Format => Vmdk?.IsChecked == true ? "vmdk" : Qcow?.IsChecked == true ? "qcow2" : "vhdx";
    public MainWindow()
    {
        InitializeComponent();
        PreviewKeyDown += (_, e) => { if (e.Key == System.Windows.Input.Key.F1) { ShowHelp(this, new RoutedEventArgs()); e.Handled = true; } };
        Closing += (_, e) => { if (operation != null) { e.Cancel = true; MessageBox.Show(this, "Bitte zuerst den laufenden Vorgang abbrechen und dessen Ende abwarten.", "Vorgang läuft"); } };
        Loaded += async (_, _) => { try { EngineStatus.Text = (await engine.Version(CancellationToken.None)).Split('\n')[0].Trim(); } catch (Exception ex) { EngineStatus.Text = "QEMU nicht verfügbar"; Status.Text = ex.Message; } };
    }
    private void MinimizeWindow(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void MaximizeWindow(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void CloseWindow(object sender, RoutedEventArgs e) => Close();
    private void OpenRelease(object sender, RoutedEventArgs e)
    {
        var repository = typeof(MainWindow).Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyMetadataAttribute), false)
            .Cast<System.Reflection.AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == "RepositoryUrl")?.Value;
        if (!Uri.TryCreate(repository, UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.Host != "github.com")
        {
            MessageBox.Show(this, "Das GitHub-Repository ist für diesen Build noch nicht eingerichtet.", "Release-Informationen");
            return;
        }
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(repository!.TrimEnd('/') + "/releases/tag/v" + (typeof(MainWindow).Assembly.GetName().Version?.ToString(3) ?? "0.2.2")) { UseShellExecute = true }); }
        catch (Exception ex) { MessageBox.Show(this, "Die Release-Seite konnte nicht geöffnet werden: " + ex.Message); }
    }
    private void ShowHelp(object sender, RoutedEventArgs e) => new InfoWindow(this, false).ShowDialog();
    private void ShowLicenses(object sender, RoutedEventArgs e) => new InfoWindow(this, true).ShowDialog();
    private void Busy(bool value)
    {
        SourceButton.IsEnabled = TargetButton.IsEnabled = StartButton.IsEnabled = Vhdx.IsEnabled = Vmdk.IsEnabled = Qcow.IsEnabled = Verify.IsEnabled = !value;
        CancelButton.IsEnabled = value;
    }
    private async void ChooseSource(object sender, RoutedEventArgs e)
    {
        if (operation != null) return;
        var dialog = new OpenFileDialog { Title = "Virtuelle Festplatte auswählen", Filter = "Virtuelle Festplatten|*.hds;*.vhd;*.vhdx;*.vmdk;*.qcow2;*.img;*.raw|Alle Dateien|*.*" };
        if (dialog.ShowDialog(this) != true) return;
        source = dialog.FileName; HeaderDocument.Text = Path.GetFileName(source); details = null; SourcePath.Text = source;
        operation = new(); Busy(true);
        try
        {
            Status.Text = "Quelle wird untersucht …";
            details = await engine.Inspect(source, operation.Token);
            if (details.Format == "raw" && MessageBox.Show(this, "QEMU erkennt RAW. Das kann auch bedeuten, dass das Dateiformat unbekannt ist. Nur fortfahren, wenn diese Datei tatsächlich ein unkomprimiertes RAW-Datenträgerabbild ist.", "RAW-Abbild bestätigen", MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK) { details = null; throw new IOException("RAW-Auswahl verworfen."); }
            SourceInfo.Text = $"Erkannt: {details.Format.ToUpperInvariant()} · Virtuell: {details.VirtualSize / Math.Pow(1024,3):N2} GiB · Datei: {details.FileSize / Math.Pow(1024,3):N2} GiB";
            Status.Text = "Quelle erkannt · Ziel auswählen";
        }
        catch (OperationCanceledException) { Status.Text = "Untersuchung abgebrochen."; }
        catch (Exception ex) { SourceInfo.Text = "Quelle nicht freigegeben"; Status.Text = ex.Message; }
        finally { operation.Dispose(); operation = null; Busy(false); }
    }
    private void ChooseTarget(object sender, RoutedEventArgs e)
    {
        if (operation != null) return;
        var dialog = new SaveFileDialog { Title = "Neue Zieldatei festlegen", Filter = $"{Format.ToUpperInvariant()}-Festplatte|*.{Format}", DefaultExt = "." + Format, FileName = source == null ? "Konvertierte-Festplatte" : Path.GetFileNameWithoutExtension(source) + "-konvertiert", OverwritePrompt = true };
        if (dialog.ShowDialog(this) != true) return;
        target = dialog.FileName; TargetPath.Text = target;
    }
    private void FormatChanged(object sender, RoutedEventArgs e)
    {
        if (TargetPath == null || target == null) return;
        target = Path.ChangeExtension(target, Format); TargetPath.Text = target;
    }
    private async void Review(object sender, RoutedEventArgs e)
    {
        if (source == null || target == null || details == null) { Status.Text = "Bitte eine erkannte Quelle und ein neues Ziel auswählen."; return; }
        if (MessageBox.Show(this, $"Quelle: {source}\n\nZiel: {target}\n\nDie Quell-VM muss vollständig heruntergefahren sein. Jetzt konvertieren?", "Konvertierung starten", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        operation = new(); Busy(true); Progress.Value = 0;
        var progress = new Progress<(double Percent, string Phase)>(p => { Progress.Value = p.Percent; Status.Text = p.Phase; ProgressText.Text = $"{p.Percent:N1} %"; });
        try
        {
            await engine.Convert(source, target, Format, Verify.IsChecked == true, progress, operation.Token);
            Status.Text = Verify.IsChecked == true ? "Fertig · Datenvergleich erfolgreich" : "Fertig · ohne Datenvergleich";
            Progress.Value = 100; ProgressText.Text = "100 %";
            MessageBox.Show(this, "Die Zieldatei wurde erfolgreich erstellt:\n" + target, "Konvertierung abgeschlossen", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (OperationCanceledException) { Status.Text = "Abgebrochen · keine fertige Zieldatei erstellt"; ProgressText.Text = "Abgebrochen"; }
        catch (Exception ex) { Status.Text = "Fehler: " + ex.Message; MessageBox.Show(this, ex.Message, "Konvertierung fehlgeschlagen", MessageBoxButton.OK, MessageBoxImage.Error); }
        finally { operation.Dispose(); operation = null; Busy(false); }
    }
    private void Cancel(object sender, RoutedEventArgs e) { CancelButton.IsEnabled = false; operation?.Cancel(); Status.Text = "Vorgang wird abgebrochen …"; }
}


