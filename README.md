# Aktueller Stand: öffentlicher App-Download 0.2.5

Der Quellcode bleibt privat. Das Paket `DYE.DiskShift-Oeffentlicher-Download-Webspace.zip` enthält die Startseite, Downloadseite, öffentliche Versionsdatei und ein portables App-ZIP für den Upload nach diskshift.dye.de. Noch nicht von hier auf den Webspace übertragen.

Das öffentliche App-Paket enthält .NET/WPF, aber **keine QEMU-Binärdateien**. QEMU separat vom Anbieter installieren; Standardpfad C:\Program Files\qemu. Die vorherigen internen Komplettpakete sind wegen unvollständiger versionsgenauer QEMU-Unterlagen nicht zur öffentlichen Weitergabe vorbereitet.

Die App verwendet zuerst https://diskshift.dye.de/downloads.json für Updates. Der Versionsknopf öffnet die öffentliche Downloadseite. Der Support-Server ist für ALL-INKL/PHP implementiert; Einrichtung und produktiver GitHub-App-Test sind noch offen. Siehe server/README.md.

## Historische Entwicklungsnotizen zu früheren internen Paketen
# DYE.DiskShift 0.2.3

Deutscher Windows-Konverter für virtuelle Festplatten. QEMU wird als separater Prozess verwendet. Gestaltung angelehnt an DYE PDF Studio.

## Start
Release-0.2.3\DYE.DiskShift.exe ausführen. Der gesamte Ordner muss zusammenbleiben. .NET und qemu-img sind enthalten; keine separate QEMU-Installation erforderlich. Windows x64. Auf einem zweiten, frischen Rechner noch nicht geprüft.

## Funktionen
- Lokale Quellformate: Parallels/HDS, VHD, VHDX, VMDK, QCOW2, RAW.
- Ausgabe: VHDX, VMDK, QCOW2.
- Echte Formaterkennung, virtuelle Kapazität und Dateigröße.
- RAW benötigt eine ausdrückliche Bestätigung.
- Fortschritt, Abbruch, standardmäßig vollständiger Datenvergleich.
- Quelldatei wird während des Vorgangs gegen konkurrierendes Schreiben geöffnet.
- Keine bestehenden Zieldateien überschreiben. Ausgabe entsteht zunächst in einem eigenen temporären Unterordner und wird nach Erfolg umbenannt.
- Geschätzter Platzbedarf über QEMU-QCOW2-Messung plus 10 % bzw. mindestens 64 MiB Reserve. Das ist keine Speicherreservierung oder Garantie.
- Bedienhilfe (F1), Lizenzdialog, Logo und Windows-Icon.

## Grenzen
Kein Betriebssystem- oder Architekturwechsel, keine Garantie der VM-Startfähigkeit. Kein VM-Player. Backing-Dateien, interne Snapshots, mehrteilige Parallels-Images und verschlüsselte Image-Container werden abgewiesen. macOS-AppleDouble-Dateien (._) werden nicht als Festplatten behandelt. BitLocker innerhalb eines Images wird durch Konvertierung nicht entschlüsselt. Bei Stromausfall können temporäre .diskshift-Ordner am Zielort verbleiben.

## Prüfung am 08.11.2026
12 Prüfungen erfolgreich: Paket-QEMU startet; RAW nach VHDX/VMDK/QCOW2 mit Datenvergleich; VHDX nach QCOW2; bestehendes Ziel und Quelle als Ziel abgewiesen; Abbruch; Snapshot-Kette abgewiesen; SHA256 der Quelle unverändert; temporäre Ausgaben entfernt; echtes Parallels-Image auf I: nur lesend erkannt. Die große Parallels-Datei wurde mit dieser App nicht erneut vollständig konvertiert. Kleine Testimages sind keine umfassende Formatzertifizierung.
Hauptfenster sowie Hilfe- und Lizenzdialog visuell geprüft.

## QEMU-Paket
qemu-img 11.1.0 (v11.1.0-12130-ge470268ff4) aus der vorhandenen lokalen Installation. qemu-dependencies.json listet rekursive DLL-Importe; qemu-sha256.csv enthält Prüfsummen. QEMU-Systememulatoren und Firmware werden für die Konvertierung nicht benötigt und nicht mitgeliefert.

## Weitergabe
Die Lizenztexte von .NET/WPF sowie QEMU liegen unter licenses. Die vollständigen versionsgenauen Quellen und Unterlagen der QEMU-DLLs sind noch offen. Dieses Paket ist für lokale Erprobung, noch nicht für kommerzielle Weitergabe vorbereitet. Siehe licenses/QEMU-Komponenten.md. Die Lizenz der DYE-GUI ist noch nicht festgelegt.

## Entwicklung
Build: dotnet publish src\DiskShift.csproj -c Release -r win-x64 --self-contained true -o Release-0.2.3
Tests: dotnet run --project tests\Smoke.csproj -- "E:\##Projet-2026\DYE DiskShift"
Das qemu-Unterverzeichnis muss aus dem geprüften Paket übernommen werden. Quellcode: src/. Keine externen NuGet-Bibliotheken außerhalb der .NET-Laufzeit.

## Bildzeichen
assets/DYE.DiskShift-Logo.png und assets/DYE.DiskShift.ico. Bildzeichen mit ImageGen erzeugt: versetzte Datenträger mit gegenläufigen Pfeilen, Mint/Petrol, transparenter Hintergrund, ohne Text.

## Kopfzeile 0.2.3
Kompakte dunkle Kopfzeile nach dem SolidCraft-Beispiel mit Logo, Datei-Menü, aktuellem Dateinamen, Hilfe, Lizenzen, Version und integrierten Fensterknöpfen. Menü und Maximieren/Wiederherstellen visuell geprüft. Der Konvertierungskern wurde dabei nicht geändert.


## GitHub
Privates Repository: https://github.com/Hartwigsen/DYE.DiskShift
Der Versionsknopf öffnet den Release-Tag der tatsächlich laufenden Programmversion. Zugriff auf private Releases erfordert ein berechtigtes GitHub-Konto.


## Fehlerberichte und Updates (0.2.3)
Fehler melden öffnet ein eigenes Fenster mit Zusammenfassung, Problem, Reproduktionsschritten und erwartetem Ergebnis. Der Bericht kann kopiert oder in einem GitHub-Issueformular vorbereitet werden. Es wird kein Issue automatisch erstellt. Ein ausgewählter Screenshot muss im Browser selbst angehängt werden; Dateipfade und Logs werden nicht automatisch beigefügt.
Beim Start erfolgt eine Updateprüfung, zusätzlich manuell über Updates. Neue Releases werden automatisch in einem Fenster angezeigt. Testreleases werden berücksichtigt. Ohne verfügbaren Zugriff wird kein Aktualitätsversprechen angezeigt. Das private Repository erfordert eine angemeldete GitHub-CLI unter dem Standard-Installationspfad; ohne CLI wird eine nicht authentifizierte API-Abfrage versucht, die bei privaten Repositories keinen Zugriff hat. Kein eingebauter Token und kein automatischer Download oder Programmaustausch.
14 automatisierte Prüfungen bestanden (11 Konverterprüfungen und 3 Versionsvergleichsprüfungen); Bericht- und Updatefenster visuell geprüft. GitHub-Issues wurden beim Test nicht eingereicht.


