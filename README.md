# DYE.DiskShift

Kostenloser Windows-Konverter für virtuelle Festplatten. Der eigene Code steht unter der MIT-Lizenz. QEMU und andere Fremdkomponenten behalten ihre eigenen Lizenzen.

## Komplettpaket 0.2.6
- Deutsche Oberfläche mit kompakter Kopfzeile, Hilfe und Lizenzinformationen.
- QEMU 11.1.3 und .NET/WPF enthalten; keine separate QEMU-Installation erforderlich.
- Quellen: Parallels/HDS, VHD, VHDX, VMDK, QCOW2 und RAW; Ziele: VHDX, VMDK, QCOW2.
- Format- und Größenanzeige, Fortschritt, Abbruch, neue Zieldatei und optionaler vollständiger Datenvergleich.
- Öffentliche Updateprüfung ohne GitHub-Anmeldung.
- Fehlerformular mit eigener Fensteraufnahme, Vorschau und optionalem Serverupload nach ausdrücklicher Bestätigung.

Downloads: https://github.com/Hartwigsen/DYE.DiskShift/releases/tag/v0.2.6
Produktseite: https://diskshift.dye.de/

Das Software-ZIP vollständig entpacken und DYE.DiskShift.exe ausführen. Alle Unterordner zusammenlassen. Testversion, noch keine Codesignatur und noch kein Test auf einem frischen Kundenrechner.

## Herkunft und Quellen der Fremdkomponenten
qemu-img wurde aus dem offiziellen Tag v11.1.3 / Commit 005c61c0ef6f6ae615283440b4bb7be51b2c6ddd gebaut. Der Workflow baut nur das benötigte Konvertierungswerkzeug. Sieben Runtime-Pakete sind anhand ihrer Originalpakete und PKGBUILD-Hashes zu den passenden Quellen zugeordnet. Der vollständige Source-/Build-Download wird neben dem Software-ZIP veröffentlicht.

Build: https://github.com/Hartwigsen/DYE.DiskShift/actions/runs/42504890094
Siehe tools/QEMU-BUILD.md, THIRD-PARTY.md und qemu/README-QUELLEN.txt im Softwarepaket. Vollständige Drittanbieterquellen sind im separaten ZIP direkt verfügbar; keine Anfrage erforderlich.

## Prüfung
20 lokale Tests bestanden, darunter RAW nach VHDX/VMDK/QCOW2 mit Datenvergleich, VHDX nach QCOW2, komprimiertes QCOW2, Abbruch, Überschreibschutz, unveränderte Quelle, Versionsvergleich und Support-Client. Die echte Parallels-Festplatte wurde nur lesend erkannt. 139 SHA256-Dateisummen des Build-Artefakts und alle sieben PKGBUILD-Hashes geprüft. Der PHP-Dienst besteht 17 lokale Prüfungen; ein produktiver GitHub-App-Test ist noch offen.

## Grenzen
Keine Garantie der VM-Startfähigkeit und kein Wechsel der Prozessorarchitektur. Backing-Dateien, interne Snapshots, mehrteilige Parallels-Images und verschlüsselte Image-Container werden abgewiesen. BitLocker wird durch eine Konvertierung nicht entschlüsselt. Quell-VM herunterfahren und eine Sicherung behalten. Platzbedarf wird geschätzt, nicht reserviert.

## Support und Webspace
Der Anwendungscode ist öffentlich. Kundenberichte und Screenshots gehören in das separate private Repository Hartwigsen/DYE.DiskShift-Support. Der Worker verweigert öffentliche Ziele. Die GitHub App muss in diesem Support-Repository installiert sein. Servereinrichtung und Inbetriebnahme: server/README.md. Keine produktiven Schlüssel oder Support-Codes im Repository speichern.

Die statischen Dateien aus server/public/index.html, downloads.html, downloads.json, assets/ und downloads/SHA256SUMS.txt werden auf diskshift.dye.de hochgeladen. Die großen Software-/Quellarchive können direkt vom öffentlichen GitHub-Release bezogen werden.

## Entwicklung
App bauen: dotnet publish src/DiskShift.csproj -c Release -r win-x64 --self-contained true -o Release-0.2.6
QEMU bauen: .github/workflows/qemu-bundle.yml / tools/build-qemu.sh
Tests: dotnet run --project tests/Smoke.csproj -- "E:\##Projet-2026\DYE DiskShift"
DISKSHIFT_QEMU_TEST_ENGINE kann den vollständigen Pfad zu einem zu prüfenden qemu-img.exe vorgeben. PHP-Tests: python tests/support_server.py (lokalen PHP-Pfad ggf. anpassen).

Logo: mit ImageGen erzeugtes Datenträger-/Pfeilmotiv in Mint/Petrol; Assets im Ordner assets/.
