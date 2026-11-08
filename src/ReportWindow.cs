using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
namespace DiskShift;
public sealed class ReportWindow : Window
{
    private readonly TextBox summary = Field(false), problem = Field(true), steps = Field(true), expected = Field(true);
    private readonly TextBlock status = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,12,0,0) };
    private bool screenshot;
    private static TextBox Field(bool multi) => new() { AcceptsReturn = multi, TextWrapping = TextWrapping.Wrap, MinHeight = multi ? 75 : 36, MaxLength = multi ? 5000 : 180, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0,0,0,16) };
    public ReportWindow(Window owner)
    {
        Owner = owner; Title = "Fehler melden · DYE.DiskShift"; Width = 740; Height = 780; MinWidth = 580; MinHeight = 500; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = (System.Windows.Media.Brush)FindResource("Panel"); FontFamily = new("Segoe UI"); FontSize = 13;
        var root = new DockPanel { Margin = new Thickness(22) };
        var heading = new TextBlock { Text = "Fehler melden", FontSize = 23, Margin = new Thickness(0,0,0,16) }; DockPanel.SetDock(heading, Dock.Top); root.Children.Add(heading);
        var actions = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0,16,0,0) };
        void Action(string text, System.Action handler) { var button = new Button { Content = text, Margin = new Thickness(6,0,0,0) }; button.Click += (_,_) => handler(); actions.Children.Add(button); }
        Action("Schließen", Close); Action("Bericht kopieren", () => Copy()); Action("GitHub-Issueformular öffnen", Open);
        DockPanel.SetDock(actions, Dock.Bottom); root.Children.Add(actions);
        var body = new StackPanel();
        body.Children.Add(new TextBlock { Text = "Prüfe den Bericht vor dem Einreichen. Die App erstellt selbst kein Issue. Das Repository ist privat; ein berechtigtes GitHub-Konto ist erforderlich. Es werden keine Dateipfade oder Protokolle automatisch beigefügt. Ein Token wird hier nicht benötigt.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,0,0,16) });
        void Add(string label, TextBox field) { body.Children.Add(new TextBlock { Text = label, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0,0,0,7) }); body.Children.Add(field); }
        Add("Zusammenfassung (optional)", summary); Add("Was ist passiert?", problem); Add("Schritte zur Reproduktion (optional)", steps); Add("Erwartetes Ergebnis (optional)", expected);
        var image = new Button { Content = "Screenshot auswählen …", HorizontalAlignment = HorizontalAlignment.Left };
        image.Click += (_,_) => { var dialog = new OpenFileDialog { Filter = "Bilder|*.png;*.jpg;*.jpeg" }; if (dialog.ShowDialog(this) == true) { screenshot = true; status.Text = "Ausgewählt: " + System.IO.Path.GetFileName(dialog.FileName) + ". Bitte selbst im GitHub-Formular anhängen; kein automatischer Upload."; } };
        body.Children.Add(image); body.Children.Add(status); root.Children.Add(new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }); Content = root;
        PreviewKeyDown += (_,e) => { if (e.Key == Key.Escape) Close(); else if (e.Key == Key.Enter && Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) { e.Handled = true; Open(); } };
    }
    private string Report() => $"## Problem\n{problem.Text}\n\n## Reproduktion\n{steps.Text}\n\n## Erwartetes Ergebnis\n{expected.Text}\n\n## Umgebung\nDYE.DiskShift {Updates.Current}\nWindows {Environment.OSVersion.Version}\n" + (screenshot ? "\nScreenshot wird separat angehängt.\n" : "");
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

