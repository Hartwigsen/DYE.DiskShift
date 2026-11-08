<?php
// Copy OUTSIDE the document root. Never commit real credentials.
return [
    'base_url' => 'https://diskshift.dye.de',
    'storage' => '/absolute/path/outside/webroot/diskshift-data',
    'signing_secret' => '', // at least 32 random bytes, represented as a 64-character hex string
    'worker_password_hash' => '', // password_hash() of a separate cron password
    'app_id' => '',
    'installation_id' => '',
    'private_key_file' => '/absolute/path/outside/webroot/diskshift-app.pem',
    'owner' => 'Hartwigsen',
    'repo' => 'DYE.DiskShift-Support',
    'max_storage_bytes' => 500 * 1024 * 1024,
];
