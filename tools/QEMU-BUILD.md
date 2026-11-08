# Eigenes qemu-img-Komplettpaket

## Ziel
Die nicht vollständig zugeordneten DLLs der früheren lokalen QEMU-Installation werden ersetzt. Buildquelle ist der offizielle QEMU-Tag v11.1.3, Commit `005c61c0ef6f6ae615283440b4bb7be51b2c6ddd`. Es wird nur `qemu-img.exe` gebaut, kein VM-Emulator und keine Firmware ausgeliefert.

## Build
Workflow `.github/workflows/qemu-bundle.yml` manuell starten. Die Action-Versionen sind als Commit-Hashes festgelegt. Der Windows-Runner richtet MSYS2 MINGW64 ein. `build-qemu.sh` zeichnet Konfiguration, Compiler und installierte Paketversionen auf und archiviert den verwendeten QEMU-Quellstand.

`collect-qemu-sources.py` bestimmt die rekursive DLL-Abhängigkeit des entstandenen Werkzeugs aus den PE-Importen. Für jede nicht zum Windows-System gehörende DLL werden das originale MSYS2-Binärpaket, Paketmetadaten und Build-Metadaten gesichert. Passende PKGBUILD-Stände werden anhand von Version und, wenn vorhanden, PKGBUILD-SHA256 aus `.BUILDINFO` gesucht. Lizenzdateien und vollständige `makepkg --allsource`-Archive werden gesammelt. PGP-Überprüfung der Upstream-Quellen wird beim Archivieren übersprungen; makepkg-Prüfsummen bleiben aktiv. Die Binärpakete werden zuvor durch pacman geprüft.

Der Build-Runner lädt Pakete aus den aktuellen MSYS2-Repositories. Die verwendeten Versionen werden für den ausgelieferten Build dokumentiert; dies ist kein Versprechen für einen bit-identischen Neubau auf einem beliebigen späteren Runner.

## Freigabeschritte
1. Erfolgreiches Build-Artefakt und `SOURCE-COLLECTION-COMPLETE.txt` prüfen.
2. Alle Dateiprüfsummen gegen `SHA256.json` prüfen.
3. Tatsächlich enthaltene Lizenztexte und Paketliste durchsehen; Windows-System-DLLs nicht kopieren.
4. qemu-img aus dem Paket lokal ausführen und Formate sowie nichtleere Testdaten prüfen. Den echten Parallels-Datenträger nur lesend erkennen, ohne erneute große Konvertierung.
5. Nur die neue verifizierte Binärdatei mit den dazugehörigen DLLs in die App aufnehmen.
6. Dazugehöriges Drittanbieter-Quellpaket und Build-Unterlagen neben dem Softwaredownload anbieten und die Links im Paket dokumentieren. Den Quellcode der eigenständigen GUI nicht in dieses Drittanbieterpaket aufnehmen.
7. Erst dann Downloadseite und öffentliche Versionsdatei auf das Komplettpaket umstellen.

Bei fehlendem Quellarchiv oder fehlgeschlagenem Test wird das Artefakt nicht als freigegebenes Komplettpaket bezeichnet. Die vollständigen Quelldateien sollen direkt bereitstehen; ein allgemeiner Projektlink oder eine unverbindliche Quellcode-Zusage genügt für diesen Ablauf nicht.

