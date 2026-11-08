<?php
if (PHP_SAPI !== 'cli') { http_response_code(404); exit; }
$code = rtrim(strtr(base64_encode(random_bytes(36)), '+/', '-_'), '=');
echo "Support-Code (nur dem Kunden geben): ".$code.PHP_EOL;
echo "SHA256 (in support_hashes eintragen): ".hash('sha256',$code).PHP_EOL;
echo "Signing-Secret (nur bei Ersteinrichtung verwenden): ".bin2hex(random_bytes(32)).PHP_EOL;
