<?php
declare(strict_types=1);
function configuration(): array {
    $path = getenv('DISKSHIFT_CONFIG') ?: dirname(__DIR__) . '/diskshift-config.php';
    if (!is_file($path)) throw new RuntimeException('Service not configured');
    $c = require $path;
    if (!is_array($c) || strlen($c['signing_secret'] ?? '') < 64 || empty($c['support_hashes']) || empty($c['storage'])) throw new RuntimeException('Invalid configuration');
    if (!preg_match('~^https://[a-z0-9.-]+$~i', $c['base_url'] ?? '')) throw new RuntimeException('HTTPS base URL required');
    if (!is_dir($c['storage']) && !mkdir($c['storage'], 0700, true)) throw new RuntimeException('Storage unavailable');
    $storage = realpath($c['storage']); $public = realpath(__DIR__ . '/public');
    $normalizedStorage = strtolower(str_replace('\\', '/', $storage . DIRECTORY_SEPARATOR));
    $normalizedPublic = strtolower(str_replace('\\', '/', $public . DIRECTORY_SEPARATOR));
    if (str_starts_with($normalizedStorage, $normalizedPublic)) throw new RuntimeException('Storage must be outside document root');
    $c['storage'] = $storage;
    return $c;
}
function database(array $c): PDO {
    $db = new PDO('sqlite:' . $c['storage'] . '/reports.sqlite');
    $db->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
    $db->exec('PRAGMA busy_timeout=5000');
    $db->exec('CREATE TABLE IF NOT EXISTS reports (id TEXT PRIMARY KEY, customer TEXT NOT NULL, digest TEXT NOT NULL, created INTEGER NOT NULL, body TEXT NOT NULL, image TEXT, state TEXT NOT NULL, issue INTEGER)');
    $db->exec('CREATE TABLE IF NOT EXISTS limits (key TEXT PRIMARY KEY, bucket INTEGER NOT NULL, count INTEGER NOT NULL)');
    return $db;
}
function limit(PDO $db, string $key, int $max): bool {
    $bucket = intdiv(time(), 3600);
    $q = $db->prepare('INSERT INTO limits(key,bucket,count) VALUES(?,?,1) ON CONFLICT(key) DO UPDATE SET count=CASE WHEN bucket=excluded.bucket THEN count+1 ELSE 1 END,bucket=excluded.bucket RETURNING count');
    $q->execute([$key,$bucket]); return (int)$q->fetchColumn() <= $max;
}
function respond(int $status, array $body): never {
    http_response_code($status); header('Content-Type: application/json; charset=utf-8');
    header('Cache-Control: no-store'); header('X-Content-Type-Options: nosniff');
    echo json_encode($body, JSON_UNESCAPED_UNICODE | JSON_THROW_ON_ERROR); exit;
}
function bearer(): string {
    $header = $_SERVER['HTTP_AUTHORIZATION'] ?? $_SERVER['REDIRECT_HTTP_AUTHORIZATION'] ?? '';
    return preg_match('/^Bearer ([A-Za-z0-9_-]{32,128})$/D', $header, $m) ? $m[1] : '';
}
function imageLink(array $c, string $id, int $created): string {
    $expires = $created + 30 * 86400;
    $sig = hash_hmac('sha256', "$id:$expires", $c['signing_secret']);
    return $c['base_url'] . '/index.php?image=' . $id . '&expires=' . $expires . '&signature=' . $sig;
}
function github(array $c, string $method, string $path, array $payload, string $token): array {
    $curl = curl_init('https://api.github.com' . $path);
    curl_setopt_array($curl, [CURLOPT_CUSTOMREQUEST=>$method, CURLOPT_RETURNTRANSFER=>true, CURLOPT_TIMEOUT=>25,
        CURLOPT_CONNECTTIMEOUT=>8, CURLOPT_PROTOCOLS=>CURLPROTO_HTTPS, CURLOPT_FOLLOWLOCATION=>false,
        CURLOPT_HTTPHEADER=>['Authorization: Bearer ' . $token,'Accept: application/vnd.github+json','X-GitHub-Api-Version: 2026-03-10','User-Agent: DYE-DiskShift-Support','Content-Type: application/json']]);
    if ($method !== 'GET') curl_setopt($curl, CURLOPT_POSTFIELDS, json_encode($payload, JSON_THROW_ON_ERROR));
    $raw = curl_exec($curl); $code = curl_getinfo($curl, CURLINFO_HTTP_CODE); curl_close($curl);
    if ($raw === false || $code < 200 || $code >= 300) throw new RuntimeException('GitHub request not confirmed');
    return json_decode($raw, true, 32, JSON_THROW_ON_ERROR);
}
function appToken(array $c): string {
    if (!ctype_digit((string)$c['app_id']) || !ctype_digit((string)$c['installation_id'])) throw new RuntimeException('GitHub App IDs missing');
    $b64 = static fn(string $x): string => rtrim(strtr(base64_encode($x), '+/', '-_'), '=');
    $now = time();
    $jwt = $b64('{"alg":"RS256","typ":"JWT"}') . '.' . $b64(json_encode(['iat'=>$now-60,'exp'=>$now+540,'iss'=>(string)$c['app_id']], JSON_THROW_ON_ERROR));
    $pem = file_get_contents($c['private_key_file']);
    if ($pem === false || !openssl_sign($jwt, $signature, $pem, OPENSSL_ALGO_SHA256)) throw new RuntimeException('App key unavailable');
    $auth = github($c, 'POST', '/app/installations/' . $c['installation_id'] . '/access_tokens', ['repositories'=>[$c['repo']],'permissions'=>['issues'=>'write']], $jwt . '.' . $b64($signature));
    return $auth['token'];
}
function cleanup(PDO $db, array $c): void {
    $q=$db->prepare('SELECT image FROM reports WHERE created < ?'); $q->execute([time()-30*86400]);
    foreach ($q as $row) if ($row['image'] && preg_match('/^[a-f0-9-]{36}\.png$/D',$row['image'])) @unlink($c['storage'].'/'.$row['image']);
    $q=$db->prepare('DELETE FROM reports WHERE created < ?'); $q->execute([time()-30*86400]);
    $q=$db->prepare('DELETE FROM limits WHERE bucket < ?'); $q->execute([intdiv(time(),3600)-48]);
}
