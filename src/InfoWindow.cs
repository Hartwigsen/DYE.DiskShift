using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
namespace DiskShift;
public sealed class InfoWindow : Window
{
    public InfoWindow(Window owner, bool license)
    {
        Owner = owner; Title = license ? "Lizenzen und Komponenten" : "Hilfe / Bedienung";
        Width = 760; Height = 650; MinWidth = 540; MinHeight = 400;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = (Brush)Application.Current.FindResource("Bg");
        FontFamily = new FontFamily("Segoe UI"); FontSize = 14;
        var panel = new DockPanel { Margin = new Thickness(24) };
        var title = new TextBlock { Text = Title, FontSize = 23, Margin = new Thickness(0,0,0,18) };
        DockPanel.SetDock(title, Dock.Top); panel.Children.Add(title);
        var close = new Button { Content = "Schließen", IsCancel = true, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0,16,0,0) };
        close.Click += (_, _) => Close(); DockPanel.SetDock(close, Dock.Bottom); panel.Children.Add(close);
        if (license)
        {
            var links = new WrapPanel { Margin = new Thickness(0,12,0,0) };
            foreach (var item in new[] { ("QEMU-Projekt", "https://www.qemu.org/"), ("QEMU-Lizenz", "https://github.com/qemu/qemu/blob/master/COPYING") })
            {
                var link = new Button { Content = item.Item1, Margin = new Thickness(0,0,10,0) };
                link.Click += (_, _) => { try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(item.Item2) { UseShellExecute = true }); } catch (Exception ex) { MessageBox.Show(this, "Der Link konnte nicht geöffnet werden: " + ex.Message); } };
                links.Children.Add(link);
            }
            DockPanel.SetDock(links, Dock.Bottom); panel.Children.Add(links);
        }
        panel.Children.Add(new TextBox { Text = license ? Licenses : Help, IsReadOnly = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(16), BorderThickness = new Thickness(0) });
        Content = panel;
        PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape) Close(); };
    }
    private const string Help = """
        DYE.DiskShift · Version 0.2.6

        Diese Version konvertiert lokale virtuelle Festplatten mit dem mitgelieferten qemu-img. Die Quelldatei wird nur gelesen. Zur Konvertierung werden keine Image-Dateien hochgeladen. Fehlerberichte mit optionalem Screenshot sind ein gesonderter Vorgang mit ausdrücklicher Bestätigung.

        1. Quelle auswählen
        Klicke auf „Datei auswählen …“ und wähle eine virtuelle Festplatte. Bei Parallels liegt die .hds-Datei häufig im .pvm-Paket unter einem .hdd-Ordner. Bei mehreren HDS-Dateien oder Snapshots zuerst die Zusammengehörigkeit prüfen; nicht beliebig eine Datei auswählen.
        QEMU erkennt das Quellformat und zeigt Dateigröße und virtuelle Kapazität an. RAW muss ausdrücklich bestätigt werden, weil eine unbekannte Datei ebenfalls als RAW erkannt werden kann. Images mit Backing-Dateien, internen Snapshots und verschlüsselte Image-Container werden abgewiesen.

        2. Zielformat wählen
        VHDX: für Hyper-V und das Einbinden unter Windows.
        VMDK: für VMware; die Kompatibilität hängt von der konkreten Variante ab.
        QCOW2: für QEMU-basierte Umgebungen.
        Das Format ändert weder Betriebssystem noch CPU-Architektur. Die Startfähigkeit einer VM ist damit nicht garantiert.

        3. Speicherort festlegen
        Wähle über „Speichern unter …“ einen neuen Dateinamen. Quelle und Ziel müssen verschieden sein. Bei einem Formatwechsel wird die Zielendung angepasst.

        4. Konvertieren
        Klicke auf „Konvertieren“ und bestätige die Zusammenfassung. Das Programm prüft die Quelle erneut und schätzt den Platzbedarf einschließlich Reserve. Die Schätzung garantiert nicht, dass der Speicherplatz bis zum Ende reicht. Vorhandene Ziele werden nicht überschrieben. Fortschritt und aktuelle Phase erscheinen unten. Ein vollständiger Datenvergleich ist standardmäßig aktiviert; er prüft die virtuellen Nutzdaten, nicht die Startfähigkeit oder das Dateisystem. Nach einem erfolgreichen Abschluss erscheint die gewählte Zieldatei. Während der Arbeit liegt die Ausgabe in einem temporären Unterordner am Zielort.

        Abbrechen und Fehler
        „Abbrechen“ beendet den laufenden QEMU-Prozess. Eine unvollständige Ausgabe wird entfernt; die Quelle bleibt erhalten. Warte auf die Abschlussmeldung, bevor du das Programm schließt. Nach einem Stromausfall können .diskshift-Ordner am Zielort verbleiben. Diese enthalten keine fertigen Ergebnisse.

        Vor einer Konvertierung
        Quell-VM vollständig herunterfahren, eine Sicherung behalten und ausreichend Speicherplatz vorsehen. Die Quelle darf während der Konvertierung nicht verändert werden.

        Daten aus einer fertigen VHDX lesen
        Datenträgerverwaltung → Aktion → Virtuelle Festplatte anfügen → Datei auswählen → „Schreibgeschützt“ aktivieren. Bei einer Aufforderung zum Initialisieren oder Formatieren abbrechen. Für BitLocker ist ein geeigneter Entsperrschlüssel erforderlich.

        Tastatur
        Tab: Bedienelement wechseln. Leertaste: Schaltfläche oder Format betätigen. F1: Hilfe öffnen. Escape: Hilfefenster schließen. Der Text lässt sich markieren und kopieren.
        """;
    private const string Licenses = """
        DYE.DiskShift
        Kostenlose Windows-Oberfläche unter der MIT-Lizenz. Der eigene Quellcode ist öffentlich verfügbar: https://github.com/Hartwigsen/DYE.DiskShift . Lizenztext: LICENSE im Programmordner. Die Lizenzen der Fremdkomponenten gelten unabhängig davon.

        QEMU / qemu-img
        Enthalten ist ein eigener Build von qemu-img 11.1.3 aus dem offiziellen QEMU-Tag v11.1.3 (Commit 005c61c0ef6f6ae615283440b4bb7be51b2c6ddd), als separates Programm aufgerufen. Paketversionen, Lizenzen, Build-Unterlagen und Prüfsummen liegen im qemu-Unterordner. Die passenden vollständigen Drittanbieterquellen stehen neben dem Softwaredownload unter https://diskshift.dye.de/downloads.html und im GitHub-Release bereit.

        QEMU steht als Gesamtprojekt unter der GNU General Public License, Version 2 (GPLv2). Einzelne Bestandteile haben eigene kompatible Lizenzen. Maßgeblich sind die Angaben des konkret verwendeten Pakets und seiner Quelldateien.

        Urheber: Fabrice Bellard und die QEMU-Mitwirkenden; Einzelheiten siehe Quelldateien. QEMU ist eine Marke von Fabrice Bellard. DYE.DiskShift ist kein offizielles QEMU-Produkt.

        Projekt: https://www.qemu.org/
        Quellcode: https://github.com/qemu/qemu
        Lizenzangaben: https://github.com/qemu/qemu/blob/master/LICENSE
        GPLv2: https://github.com/qemu/qemu/blob/master/COPYING

        Die GPL erlaubt kommerzielle Weitergabe unter ihren Bedingungen. Bei einer Weitergabe dieses Pakets sind Lizenzhinweise, der vollständige passende Quellcode bzw. eine zulässige Bereitstellung sowie die Lizenzen mitgelieferter Abhängigkeiten zu berücksichtigen. Die Links allein ersetzen diese Pflichten nicht. Für QEMU gelten die Gewährleistungsausschlüsse der GPL, soweit rechtlich zulässig.

        Microsoft .NET / WPF
        Dieses Paket enthält die .NET-Laufzeit mit WPF. Die mit der Laufzeit gelieferten Lizenz- und Drittanbieterhinweise befinden sich im Unterordner „licenses“ des Programmordners.
        https://github.com/dotnet/runtime
        https://github.com/dotnet/wpf

        Stand: 08.11.2026. Die MIT-Lizenz des eigenen Programms ersetzt keine Fremdlizenzen. Beim Weitergeben der QEMU-Komponenten müssen deren Lizenz- und Quellcodebedingungen erhalten bleiben. Siehe qemu/README-QUELLEN.txt und THIRD-PARTY.md.
        """;
}













