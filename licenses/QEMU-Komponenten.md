# Mitgelieferte QEMU-Komponenten

qemu-img 11.1.0 (v11.1.0-12130-ge470268ff4), aus C:\Program Files\qemu.
Windows-Builds: https://qemu.weilnetz.de/w64/
QEMU: https://www.qemu.org/ und https://github.com/qemu/qemu

qemu-dependencies.json enthält die rekursiv ermittelten PE-Importe.
Die Windows-Systembibliotheken werden nicht mitgeliefert.
Nicht benötigte Systememulatoren und Firmware sind nicht enthalten.

Die Bibliotheken umfassen unter anderem GLib, gettext, libiconv, libffi,
PCRE2, zlib, bzip2, Brotli, Zstandard, curl, OpenSSL, GnuTLS, GMP,
Nettle/Hogweed, libtasn1, libidn2, libunistring, libpsl, p11-kit,
libssh, libssh2, libnfs und MinGW-Laufzeitbibliotheken.
Diese Komponenten haben eigene Lizenzbedingungen. QEMUs GPL-Text allein
ersetzt deren Lizenz- und Quellcodepflichten nicht.

OFFEN vor externer / kommerzieller Weitergabe:
- Vollständigen passenden QEMU-Build-Quellstand und Build-Skripte sichern.
- Versionsgenaue Lizenztexte, Urheberhinweise und erforderliche Quellen der
  mitgelieferten Bibliotheken zusammenstellen.
- Endgültige Nutzungsbedingungen der eigenständigen DYE-GUI festlegen.
Ein API-Lookup des Build-Kürzels e470268ff4 im stweil-GitHub-Mirror ergab
keinen passenden Commit; ein beliebiger QEMU-Release-Quellstand wäre kein
belegter Ersatz für den verwendeten Build.

Dieses Paket wurde lokal gebaut und getestet, ist aber noch kein fertig
geprüftes kommerzielles Auslieferungspaket.
