# Konkrete Einrichtung für die gezeigte FTP-Struktur

Auf dem FTP-Server liegen derzeit lib.php und worker.php direkt unter /diskshift.dye.de; darunter existiert public/.

1. Den Inhalt des lokalen Ordners server/public/ (HTML-Seiten, assets/, .htaccess, index.php und cron.php) in den FTP-Unterordner /diskshift.dye.de/public/ übertragen. Große ZIP-Dateien unter downloads/ sind nicht nötig; die Downloadseiten verlinken auf GitHub.
2. Im ALL-INKL-KAS das Zielverzeichnis der Subdomain diskshift.dye.de auf /diskshift.dye.de/public/ einstellen. Erst umstellen, wenn dort auch index.html und assets/ liegen.
3. lib.php und worker.php bleiben unter /diskshift.dye.de/, also eine Ebene oberhalb des Domainziels.
4. Die private Konfiguration diskshift-config.php wird standardmäßig noch eine Ebene höher gesucht (im übergeordneten Webspace-Verzeichnis). Darin absolute Serverpfade für Speicher und PEM setzen. Der FTP-Pfad ist nicht automatisch der absolute PHP-Dateisystempfad. Keine Schlüssel in public/ ablegen.
5. GitHub App mit Issues-Schreibrecht ausschließlich im privaten Hartwigsen/DYE.DiskShift-Support installieren. App-ID, Installation-ID und PEM-Dateipfad konfigurieren. Die App darf keine Kundenberichte im öffentlichen Produkt-Repository anlegen.
6. Persönlichen Support-Code erzeugen, nur seinen SHA256-Hash in support_hashes hinterlegen, Code separat in der App eingeben. Signing-Secret und Cron-Passwort unabhängig erzeugen.
7. Cron einrichten. Erst anschließend mit einem ausdrücklich gekennzeichneten Testbericht die gesamte Übertragung samt privatem Issue prüfen.

Erwartete Antworten ohne Bericht-Übertragung:
- index.html: Website.
- GET /index.php: 405 bei richtig eingerichtetem Dienst (POST erforderlich), sonst 503 bei fehlender privater Konfiguration.
- 404: Datei nicht im Domainziel oder falsches Domainziel.

Die Upload-Dateien allein stellen den Dienst nicht fertig bereit. Vollständige Details: server/README.md.

## Vereinfachung: FTP-Zugang ohne übergeordnetes Verzeichnis
Konfiguration darf direkt neben lib.php liegen. Darin zeigen __DIR__-Pfade auf diskshift-private/data und diskshift-private/github-app.pem. Nur public ist Domainziel. Kunden benötigen keinen Support-Code mehr; bestehende support_hashes werden ignoriert.
