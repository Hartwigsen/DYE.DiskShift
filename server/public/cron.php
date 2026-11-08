<?php
declare(strict_types=1);
require dirname(__DIR__).'/worker.php';
try {
    $c=configuration();
    if (($_SERVER['PHP_AUTH_USER']??'')!=='worker' || !password_verify($_SERVER['PHP_AUTH_PW']??'',$c['worker_password_hash']??'')) { header('WWW-Authenticate: Basic realm="DiskShift worker"'); respond(401,['message'=>'Authentication required']); }
    respond(200,work($c));
} catch(Throwable $e) { respond(503,['message'=>'Worker unavailable']); }
