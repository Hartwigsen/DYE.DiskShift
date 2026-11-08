<?php
declare(strict_types=1);
require_once __DIR__.'/lib.php';
function work(array $c): array {
    $lock=fopen($c['storage'].'/worker.lock','c');
    if (!$lock || !flock($lock,LOCK_EX|LOCK_NB)) return ['processed'=>0];
    $db=database($c); cleanup($db,$c); $count=0;
    try {
        // Acquire credentials BEFORE claiming jobs: configuration/network errors here are safely retryable.
        $token=appToken($c);
        $jobs=$db->query("SELECT * FROM reports WHERE state='queued' ORDER BY created LIMIT 10")->fetchAll(PDO::FETCH_ASSOC);
        foreach($jobs as $job) {
            $q=$db->prepare("UPDATE reports SET state='sending' WHERE id=? AND state='queued'"); $q->execute([$job['id']]);
            if (!$q->rowCount()) continue;
            $data=json_decode($job['body'],true,16,JSON_THROW_ON_ERROR);
            // Quote customer input as a fenced block, so mentions and remote images are not interpreted.
            $safe=static fn(string $text): string => str_replace('`','ˋ',$text);
            $body="DiskShift-Bericht ".$job['id']."\n\n```text\nProblem:\n".$safe($data['problem'])."\n\nReproduktion:\n".$safe($data['steps'])."\n\nErwartet:\n".$safe($data['expected'])."\n\nVersion: ".$safe($data['version'])."\nSystem: ".$safe($data['os'])."\n```";
            if($job['image']) $body.="\n\n[Screenshot herunterladen – Link 30 Tage gültig](".imageLink($c,$job['id'],(int)$job['created']).")\nDieser Link gewährt Bildzugriff bis zum Ablauf; nicht weitergeben.";
            try {
                $issue=github($c,'POST','/repos/'.rawurlencode($c['owner']).'/'.rawurlencode($c['repo']).'/issues',['title'=>'[DiskShift] '.($data['summary']?:'Fehlerbericht'),'body'=>$body],$token);
                $q=$db->prepare("UPDATE reports SET state='sent',issue=? WHERE id=?"); $q->execute([(int)$issue['number'],$job['id']]); $count++;
            } catch(Throwable $e) {
                // A timeout may mean GitHub DID create the issue. Never automatically submit it again.
                $q=$db->prepare("UPDATE reports SET state='uncertain' WHERE id=?"); $q->execute([$job['id']]); error_log('DiskShift report requires reconciliation: '.$job['id']);
            }
        }
    } finally { flock($lock,LOCK_UN); fclose($lock); }
    return ['processed'=>$count];
}
if (PHP_SAPI==='cli' && realpath($_SERVER['SCRIPT_FILENAME'])===__FILE__) { try { echo json_encode(work(configuration())).PHP_EOL; } catch(Throwable $e) { fwrite(STDERR,"Support worker failed; check configuration and GitHub App.\n"); exit(1); } }
