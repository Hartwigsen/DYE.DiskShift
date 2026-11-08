using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
namespace DiskShift;
public sealed class ReportWindow : Window
{
    private readonly TextBox summary = Field(false), problem = Field(true), steps = Field(true), expected = Field(true);
    private readonly TextBlock status = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,12,0,0) };
    private byte[]? screenshot;
    private readonly CheckBox consent = new() { Content = "Bericht und ausgewählten Screenshot an diskshift.dye.de senden", Margin = new Thickness(0,8,0,8) };
    private readonly Image preview = new() { MaxHeight = 130, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0,8,0,8) };
    private string requestId = Guid.NewGuid().ToString();
    private string? lastDigest;
    private bool sending;
    private readonly Button sendButton = new() { Content = "Bericht senden", Margin = new Thickness(6,0,0,0) };
    private static TextBox Field(bool multi) => new() { AcceptsReturn = multi, TextWrapping = TextWrapping.Wrap, MinHeight = multi ? 75 : 36, MaxLength = multi ? 5000 : 180, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0,0,0,16) };
    public ReportWindow(Window owner)
    {
        Owner = owner; Title = "Fehler melden · DYE.DiskShift"; Width = 780; Height = 850; MinWidth = 650; MinHeight = 700; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = (System.Windows.Media.Brush)FindResource("Panel"); FontFamily = new("Segoe UI"); FontSize = 13;
        var root = new DockPanel { Margin = new Thickness(22) };
        var heading = new TextBlock { Text = "Fehler melden", FontSize = 23, Margin = new Thickness(0,0,0,16) }; DockPanel.SetDock(heading, Dock.Top); root.Children.Add(heading);
        var actions = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0,16,0,0) };
        void Action(string text, System.Action handler) { var button = new Button { Content = text, Margin = new Thickness(6,0,0,0) }; button.Click += (_,_) => handler(); actions.Children.Add(button); }
        Action("Schließen", Close); Action("Bericht kopieren", () => Copy());
        sendButton.Click += async (_,_) => await Send(); actions.Children.Add(sendButton);
        DockPanel.SetDock(actions, Dock.Bottom); root.Children.Add(actions);
        // Sending requirements and feedback must never disappear below the scroll viewport.
        var delivery = new StackPanel { Margin = new Thickness(0,12,0,0) };
        delivery.Children.Add(consent);
        status.MinHeight = 38; status.MaxHeight = 105;
        delivery.Children.Add(new ScrollViewer { Content = status, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, MaxHeight = 110 });
        DockPanel.SetDock(delivery, Dock.Bottom); root.Children.Add(delivery);
        var body = new StackPanel();
        body.Children.Add(new TextBlock { Text = "Prüfe deinen Bericht und den Screenshot. Beim Senden werden diese Angaben sowie Programm- und Windows-Version verschlüsselt an diskshift.dye.de übertragen. Kein GitHub-Konto erforderlich. Lokale Löschung ist nach 30 Tagen vorgesehen. Der Bericht wird an das private GitHub-Projekt weitergeleitet; dort gilt eine gesonderte Aufbewahrung. Es werden keine Protokolle oder Dateipfade automatisch beigefügt. Der Server verarbeitet zur Spambegrenzung auch die Verbindungs-IP.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,0,0,16) });
        void Add(string label, TextBox field) { body.Children.Add(new TextBlock { Text = label, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0,0,0,7) }); body.Children.Add(field); }
        Add("Zusammenfassung (optional)", summary); Add("Was ist passiert?", problem); Add("Schritte zur Reproduktion (optional)", steps); Add("Erwartetes Ergebnis (optional)", expected);
        var image = new Button { Content = "Screenshot auswählen …", HorizontalAlignment = HorizontalAlignment.Left };
        image.Click += (_,_) => { var dialog = new OpenFileDialog { Filter = "Bilder|*.png;*.jpg;*.jpeg" }; if (dialog.ShowDialog(this) == true) try { var bitmap = new System.Windows.Media.Imaging.BitmapImage(); bitmap.BeginInit(); bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad; bitmap.DecodePixelWidth = 1920; bitmap.UriSource = new Uri(dialog.FileName); bitmap.EndInit(); SetScreenshot(bitmap); } catch (Exception) { status.Text = "Bild konnte nicht geladen werden."; } };
        var capture = new Button { Content = "Anwendungsfenster aufnehmen", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0,8,0,0) };
        capture.Click += (_,_) => { try { var content = (FrameworkElement)owner.Content; var scale = Math.Min(1.0, 1920.0 / content.ActualWidth); var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap((int)Math.Ceiling(content.ActualWidth * scale), (int)Math.Ceiling(content.ActualHeight * scale), 96*scale, 96*scale, System.Windows.Media.PixelFormats.Pbgra32); bitmap.Render(content); SetScreenshot(bitmap); } catch (Exception) { status.Text = "Aufnahme fehlgeschlagen. Bitte ein Bild auswählen."; } };
        var remove = new Button { Content = "Screenshot entfernen", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0,8,0,0) }; remove.Click += (_,_) => { screenshot = null; preview.Source = null; status.Text = "Screenshot entfernt."; };
        body.Children.Add(image); body.Children.Add(capture); body.Children.Add(remove); body.Children.Add(preview);
        status.Text = "Zum Senden bitte der Übertragung zustimmen. Ein Konto oder Support-Code ist nicht erforderlich.";
        root.Children.Add(new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled }); Content = root;
        Closing += (_,e) => { if (sending) { e.Cancel = true; status.Text = "Bitte die Empfangsbestätigung oder das Zeitlimit abwarten."; } };
        PreviewKeyDown += async (_,e) => { if (e.Key == Key.Escape) Close(); else if (e.Key == Key.Enter && Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) { e.Handled = true; await Send(); } };
    }
    private string Report() => $"## Problem\n{problem.Text}\n\n## Reproduktion\n{steps.Text}\n\n## Erwartetes Ergebnis\n{expected.Text}\n\n## Umgebung\nDYE.DiskShift {Updates.Current}\nWindows {Environment.OSVersion.Version}\n" + (screenshot != null ? "\nScreenshot ausgewählt.\n" : "");
    private void SetScreenshot(System.Windows.Media.Imaging.BitmapSource bitmap)
    {
        var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder(); encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
        using var stream = new System.IO.MemoryStream(); encoder.Save(stream);
        if (stream.Length > 2*1024*1024) { status.Text = "Screenshot zu groß (maximal 2 MiB). Bitte einen kleineren Ausschnitt auswählen."; return; }
        screenshot = stream.ToArray(); preview.Source = bitmap; status.Text = "Screenshot zur Prüfung bereit. Sichtbare Dateinamen oder andere vertrauliche Inhalte vor dem Senden prüfen.";
    }
    private async Task Send()
    {
        if (sending) return;
        if (string.IsNullOrWhiteSpace(problem.Text)) { status.Text = "Bitte zuerst das Problem beschreiben."; problem.BringIntoView(); problem.Focus(); return; }
        if (consent.IsChecked != true) { status.Text = "Bitte die Übertragung an diskshift.dye.de bestätigen."; return; }
        var fields = new { summary = summary.Text, problem = problem.Text, steps = steps.Text, expected = expected.Text, version = Updates.Current, os = Environment.OSVersion.Version.ToString(), png = screenshot == null ? null : Convert.ToBase64String(screenshot) };
        var digest = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(fields)));
        if (lastDigest != null && lastDigest != digest) requestId = Guid.NewGuid().ToString(); lastDigest = digest;
        var report = new { id = requestId, fields.summary, fields.problem, fields.steps, fields.expected, fields.version, fields.os, fields.png };
        sending = true; sendButton.IsEnabled = false; consent.IsEnabled = false; status.Text = "Bericht wird übertragen …";
        try { status.Text = await SupportClient.Send(report); }
        catch (Exception ex) { status.Text = "Nicht bestätigt: " + ex.Message + " Erneutes Senden desselben Berichts verwendet dieselbe Kennung."; }
        finally { sending = false; sendButton.IsEnabled = true; consent.IsEnabled = true; }
    }
    private bool Copy()
    {
        try { Clipboard.SetText(summary.Text + "\n\n" + Report()); status.Text = "Bericht kopiert. Bei Bedarf auf GitHub einfügen."; return true; }
        catch (System.Runtime.InteropServices.ExternalException) { status.Text = "Zwischenablage belegt. Bitte erneut versuchen."; return false; }
    }
    private void Open()
    {
        if (string.IsNullOrWhiteSpace(problem.Text)) { status.Text = "Bitte zuerst das Problem beschreiben."; problem.Focus(); return; }
        if (!Copy()) return;
        var url = Updates.Repository + "/issues/new?title=" + Uri.EscapeDataString(string.IsNullOrWhiteSpace(summary.Text) ? "Fehlerbericht DYE.DiskShift " + Updates.Current : summary.Text);
        var report = Uri.EscapeDataString(Report()); if (url.Length + report.Length < 6000) url += "&body=" + report;
        try { Updates.Open(url); status.Text = "Formular geöffnet. Bericht prüfen, bei Bedarf einfügen, Screenshot anhängen und erst auf GitHub absenden."; }
        catch (Exception ex) { status.Text = "Browser konnte nicht geöffnet werden. Bericht ist kopiert. " + ex.Message; }
    }
}

