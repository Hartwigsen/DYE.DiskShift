# ALL-INKL: diskshift.dye.de

## Stand
Implementierter PHP-Dienst, lokal unter PHP 8.2 geprüft. Noch nicht auf ALL-INKL eingerichtet oder mit einer echten GitHub App getestet. Keine Zugangsdaten enthalten.

## Verzeichnislayout
Die Subdomain muss **ausschließlich auf `server/public`** zeigen. `lib.php`, `worker.php`, Konfiguration, PEM-Schlüssel und SQLite-Speicher dürfen nicht im öffentlich erreichbaren Verzeichnis liegen.

Beispiel (absolute Pfade an den Webspace anpassen):

```
/webspace/diskshift/server/public/       <- Document Root der Subdomain
/webspace/diskshift/server/lib.php
/webspace/diskshift/server/worker.php
/webspace/diskshift/diskshift-config.php <- Konfiguration
/webspace/diskshift-private/             <- Datenbank und Screenshots
/webspace/diskshift-app.pem              <- privater App-Schlüssel
```

`configuration()` lädt standardmäßig `diskshift-config.php` eine Ebene über dem server-Ordner. Alternativ kann DISKSHIFT_CONFIG einen absoluten Dateipfad vorgeben. Beispielkonfiguration kopieren und alle Pfade setzen. Schreibrechte nur für den PHP-Benutzer, kein 0777. SQLite-Dateien gehören ebenfalls außerhalb des Document Root. Konfiguration und Schlüssel dürfen nicht öffentlich herunterladbar sein. SSL-Zertifikat für diskshift.dye.de aktivieren.

Benötigt: PHP >= 8.2, PDO SQLite mit SQLite >= 3.35 (RETURNING), mbstring, OpenSSL, cURL, fileinfo. Auf dem tatsächlichen ALL-INKL-Tarif prüfen. PHP-Fehleranzeige öffentlich deaktivieren; Logs außerhalb des Webverzeichnisses führen. upload_max_filesize/post_max_size mindestens 4M, memory_limit mindestens 64M. Der Dienst verarbeitet JSON statt klassischem Dateiupload; eigener Request-Grenzwert 3 MiB.

## GitHub App
Unter dem Konto Hartwigsen eine GitHub App für diesen Dienst anlegen. Repository-Berechtigung **Issues: Read and write**, keine Contents-Schreibberechtigung. Nur für **Hartwigsen/DYE.DiskShift** installieren. Webhooks werden nicht benötigt. App-ID, Installation-ID und privaten PEM-Schlüssel in die Serverkonfiguration übernehmen; der Schlüssel bleibt auf dem Server.

Der Worker signiert einen RS256-JWT, fordert einen kurzlebigen Installationstoken an und begrenzt ihn erneut auf das Repository und Issues-Schreibzugriff. Es werden keine GitHub-Zugangsdaten an die Windows-App verteilt.

## Support-Codes und Cron
`php create-support-code.php` erzeugt einen zufälligen Kundencode und dessen SHA256-Hash. Nur den Hash in `support_hashes` speichern, den Code separat an den jeweiligen Kunden geben. Pro Kunde eigenen Code vergeben. Sperren durch Entfernen des Hashs. Ein Code ist eine widerrufbare Zugangsberechtigung, kein Beleg für eine bezahlte Lizenz. Das ausgegebene Signing-Secret nur bei der Ersteinrichtung setzen; späteres Wechseln macht bestehende Bildlinks ungültig.

Ein separates starkes Cron-Passwort erzeugen und mit PHP `password_hash()` hashen; nur den Hash als `worker_password_hash` speichern.

Worker alle fünf Minuten starten:

- CLI-Cron: `php /absoluter/pfad/server/worker.php`
- Oder HTTPS-Webcron auf `https://diskshift.dye.de/cron.php`, mit HTTP Basic Auth, Benutzer `worker` und dem separaten Cron-Passwort. Kennwort in den Auth-Feldern des Cron-Anbieters hinterlegen, nicht als Query-Parameter. Ob ALL-INKL-Webcron diese Authentifizierung im gebuchten Tarif unterstützt, muss vor Ort geprüft werden.

Cron ist erforderlich: Ohne Worker bleiben Berichte sicher gespeichert, erzeugen aber noch kein GitHub-Issue. Der Worker entfernt lokale Berichte und Bilder nach 30 Tagen und alte Limit-Zähler nach 48 Stunden. Bei ausgefallenem Cron erfolgt keine fristgerechte Löschung. Backups und Webserver-Logs müssen eine eigene Löschregel bekommen. Die Issues auf GitHub werden nicht automatisch gelöscht.

## Schutz und Grenzen
HTTPS, zufällige persönliche Support-Codes, feste Bild-/Textgrenzen; fünf Berichte pro Code/Stunde, 30 Anfragen pro IP/Stunde, 500 Anfragen insgesamt/Stunde. Feste Zeitfenster, kein Captcha und kein vollständiger DDoS-Schutz. Server-Speicherbudget standardmäßig 500 MiB. Weitergeleitete IP-Header werden nicht vertraut; hinter einem Proxy muss REMOTE_ADDR vertrauenswürdig eingerichtet werden. Bei geteilten Kunden-IP-Adressen kann das IP-Limit mehrere Kunden betreffen.

Screenshots: ausschließlich PNG bis 2 MiB, maximal 4096 Pixel je Kante / 12 Millionen Pixel, Header-/Dimensionsprüfung. Kein vollständiger serverseitiger Bilddecoder. Ausgabe nur als Download mit nosniff, niemals als ausführbarer Webinhalt. Windows recodiert ausgewählte Bilder vorab als PNG. Screenshots können vertrauliche sichtbare Inhalte enthalten; die App zeigt eine Vorschau vor der ausdrücklichen Übertragung.

Bildlinks sind HMAC-signierte **Zugriffslinks**, 30 Tage ab Einreichung gültig. Jeder mit dem Link kann das Bild herunterladen, auch ohne GitHub-Anmeldung. Links daher nur im privaten Issue speichern und nicht weitergeben. Logs können die URL enthalten; Zugriff und Aufbewahrung der Logs entsprechend begrenzen. Ist eine erneute Personenanmeldung für jedes Bild nötig, muss stattdessen eine Admin-Anmeldung gebaut werden.

Berichtskennung + Inhaltsdigest schützen gegen doppelte Client-Wiederholungen. Bei unklarer Antwort während Issue-Erstellung setzt der Worker `uncertain`; nach Prozessabbruch kann `sending` stehen bleiben. Diese Datensätze **nicht blind erneut senden**: zuerst im privaten Repository nach der Berichtskennung suchen und die Issue-Nummer zuordnen. Wenn nachweislich kein Issue existiert, Status manuell auf `queued` zurücksetzen. Queue-Zustände und Cron müssen betrieblich überwacht werden. Keine automatische E-Mail-Benachrichtigung implementiert.

Kunden erhalten als Antwort ausschließlich Empfangsbestätigung und Berichtskennung, keine privaten GitHub-Zugangsdaten oder Issue-Links.

## Inbetriebnahmeprüfung
1. Subdomain, Document Root, HTTPS, Module, Pfade und Schreibrechte prüfen.
2. GitHub App installieren, Schlüssel und IDs hinterlegen, Support-Code bereitstellen.
3. Einen ausdrücklich als Test gekennzeichneten Bericht mit kleinem Bild senden.
4. Cron ausführen; privates Issue und Bilddownload prüfen.
5. Unauthentifizierten Zugriff auf Konfiguration, Datenbank und PEM sowie ungültige Bildsignaturen prüfen; diese dürfen keine Inhalte ausliefern.
6. Löschung, Backups und Zustände `uncertain`/`sending` überwachen.

Die Windows-App verwendet `https://diskshift.dye.de/index.php`. Der Domainname allein stellt den Dienst noch nicht bereit.
