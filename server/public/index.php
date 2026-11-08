<?php
declare(strict_types=1);
require dirname(__DIR__) . '/lib.php';
try {
    $c=configuration(); $db=database($c);
    if ($_SERVER['REQUEST_METHOD']==='GET' && isset($_GET['image'])) {
        $id=(string)$_GET['image']; $expires=(string)($_GET['expires']??''); $sig=(string)($_GET['signature']??'');
        if (!preg_match('/^[a-f0-9-]{36}$/D',$id) || !ctype_digit($expires) || (int)$expires < time() || (int)$expires > time()+30*86400 || !hash_equals(hash_hmac('sha256',"$id:$expires",$c['signing_secret']),$sig)) respond(403,['message'=>'Bildlink ungültig oder abgelaufen.']);
        $q=$db->prepare('SELECT image FROM reports WHERE id=?'); $q->execute([$id]); $file=$q->fetchColumn();
        if (!$file || !is_file($c['storage'].'/'.$file)) respond(404,['message'=>'Bild nicht verfügbar.']);
        header('Content-Type: application/octet-stream'); header('Content-Disposition: attachment; filename="DiskShift-Screenshot.png"'); header('X-Content-Type-Options: nosniff'); header('Cache-Control: no-store');
        readfile($c['storage'].'/'.$file); exit;
    }
    if ($_SERVER['REQUEST_METHOD']!=='POST') respond(405,['message'=>'POST erforderlich.']);
    if ((int)($_SERVER['CONTENT_LENGTH']??0)>3*1024*1024) respond(413,['message'=>'Bericht zu groß.']);
    // Do not trust spoofable forwarding headers. Proxy deployments must supply a verified REMOTE_ADDR.
    $ip=hash_hmac('sha256',$_SERVER['REMOTE_ADDR']??'unknown',$c['signing_secret']);
    if (!limit($db,'global',500) || !limit($db,'ip:'.$ip,30)) { header('Retry-After: 3600'); respond(429,['message'=>'Zu viele Anfragen. Bitte später erneut versuchen.']); }
    $customer=hash('sha256',bearer()); $authorized=false;
    foreach($c['support_hashes'] as $hash) if (hash_equals($hash,$customer)) $authorized=true;
    if (!$authorized) respond(401,['message'=>'Support-Code ungültig oder gesperrt.']);
    $raw=file_get_contents('php://input',false,null,0,3*1024*1024+1);
    if (strlen($raw)>3*1024*1024) respond(413,['message'=>'Bericht zu groß.']);
    try { $data=json_decode($raw,true,16,JSON_THROW_ON_ERROR); } catch (JsonException) { respond(400,['message'=>'Ungültiger Bericht.']); }
    if (!is_array($data)) respond(400,['message'=>'Ungültiger Bericht.']);
    $id=$data['id']??'';
    if (!is_string($id) || !preg_match('/^[a-f0-9]{8}-[a-f0-9]{4}-4[a-f0-9]{3}-[89ab][a-f0-9]{3}-[a-f0-9]{12}$/D',$id)) respond(400,['message'=>'Ungültige Berichtskennung.']);
    foreach (['summary'=>180,'problem'=>5000,'steps'=>5000,'expected'=>5000,'version'=>32,'os'=>100] as $field=>$max) if (!isset($data[$field]) || !is_string($data[$field]) || mb_strlen($data[$field])>$max) respond(400,['message'=>'Ungültiges oder zu langes Textfeld.']);
    if (trim($data['problem'])==='') respond(400,['message'=>'Problembeschreibung fehlt.']);
    $image=null;
    if (!empty($data['png'])) {
        if (!is_string($data['png']) || strlen($data['png'])>2800000) respond(413,['message'=>'Screenshot zu groß.']);
        $image=base64_decode($data['png'],true); $size=$image===false?false:@getimagesizefromstring($image);
        if ($image===false || strlen($image)>2*1024*1024 || !$size || $size[2]!==IMAGETYPE_PNG || $size[0]>4096 || $size[1]>4096 || $size[0]*$size[1]>12000000) respond(400,['message'=>'Screenshot muss ein PNG bis 2 MiB und 4096 Pixeln sein.']);
    }
    unset($data['png']); $body=json_encode($data,JSON_UNESCAPED_UNICODE|JSON_THROW_ON_ERROR); $digest=hash('sha256',$body.($image??''));
    $db->exec('BEGIN IMMEDIATE');
    $q=$db->prepare('SELECT customer,digest FROM reports WHERE id=?'); $q->execute([$id]); $old=$q->fetch(PDO::FETCH_ASSOC);
    if ($old) { $db->exec('ROLLBACK'); if(!hash_equals($old['customer'],$customer)||!hash_equals($old['digest'],$digest)) respond(409,['message'=>'Berichtskennung bereits anderweitig verwendet.']); respond(202,['id'=>$id,'message'=>'Bericht bereits angenommen.']); }
    if (!limit($db,'customer:'.$customer,5)) { $db->exec('COMMIT'); respond(429,['message'=>'Maximal fünf Berichte pro Stunde und Support-Code.']); }
    $used=(int)$db->query('SELECT COALESCE(SUM(length(body)),0) FROM reports')->fetchColumn();
    foreach(glob($c['storage'].'/*.png')?:[] as $file) $used+=filesize($file);
    if ($used+strlen($body)+strlen($image??'')>($c['max_storage_bytes']??500*1024*1024)) { $db->exec('ROLLBACK'); respond(503,['message'=>'Support-Speicher derzeit ausgelastet.']); }
    $filename=$image===null?null:$id.'.png';
    if ($filename && file_put_contents($c['storage'].'/'.$filename,$image,LOCK_EX)===false) throw new RuntimeException('Image storage failed');
    $q=$db->prepare('INSERT INTO reports VALUES(?,?,?,?,?,?,?,NULL)'); $q->execute([$id,$customer,$digest,time(),$body,$filename,'queued']);
    $db->exec('COMMIT');
    respond(202,['id'=>$id,'message'=>'Bericht sicher angenommen und zur Bearbeitung vorgemerkt.']);
} catch (Throwable $e) { if(isset($db)&&$db->inTransaction())$db->rollBack(); error_log('DiskShift support request failed'); respond(503,['message'=>'Support-Dienst derzeit nicht verfügbar. Bitte später mit demselben Bericht erneut versuchen.']); }
