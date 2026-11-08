import base64, hashlib, hmac, json, os, pathlib, secrets, socket, sqlite3, subprocess, time, urllib.request, urllib.error, uuid
root=pathlib.Path(__file__).resolve().parents[1]
php=r'C:\xampp\php\php.exe'
test=root/'test-results'/('support-'+time.strftime('%Y%m%d-%H%M%S')); test.mkdir(parents=True)
store=test/'private'; store.mkdir()
code=secrets.token_urlsafe(36); secret=secrets.token_hex(32)
config=test/'config.php'
config.write_text("<?php return "+"['base_url'=>'https://diskshift.dye.de','storage'=>"+repr(str(store).replace('\\','/'))+",'signing_secret'=>'"+secret+"','support_hashes'=>['"+hashlib.sha256(code.encode()).hexdigest()+"'],'worker_password_hash'=>'','app_id'=>'','installation_id'=>'','private_key_file'=>'','owner'=>'Hartwigsen','repo'=>'DYE.DiskShift','max_storage_bytes'=>524288000];",encoding='utf-8')
env=os.environ.copy();env['DISKSHIFT_CONFIG']=str(config)
sock=socket.socket();sock.bind(('127.0.0.1',0));port=sock.getsockname()[1];sock.close()
log=open(test/'php.log','w'); process=subprocess.Popen([php,'-S',f'127.0.0.1:{port}','-t',str(root/'server'/'public')],env=env,stdout=log,stderr=log,creationflags=subprocess.CREATE_NO_WINDOW)
url=f'http://127.0.0.1:{port}/index.php'; results=[]
def request(data=None,token=code,suffix=''):
    raw=None if data is None else json.dumps(data).encode()
    req=urllib.request.Request(url+suffix,data=raw,headers={'Content-Type':'application/json','Authorization':'Bearer '+token})
    try:
        with urllib.request.urlopen(req,timeout=5) as r:return r.status,r.read()
    except urllib.error.HTTPError as e:return e.code,e.read()
def check(name,fn):
    try:fn();results.append('PASS '+name)
    except Exception as e:results.append('FAIL '+name+': '+str(e))
def expect(expected,data=None,token=code,suffix=''):
    status,body=request(data,token,suffix)
    assert status==expected,(status,body.decode(errors='replace'))
    return body
def report():return dict(id=str(uuid.uuid4()),summary='Test',problem='Lokaler Testbericht',steps='',expected='',version='0.2.4',os='Test',png=None)
try:
    for _ in range(40):
        try:request();break
        except OSError:time.sleep(.1)
    check('GET ohne Bild ist nicht erlaubt',lambda:expect(405))
    check('Falscher Support-Code wird abgewiesen',lambda:expect(401,report(),secrets.token_urlsafe(36)))
    r=report()
    check('Bericht wird in Warteschlange gespeichert',lambda:expect(202,r))
    check('Wiederholung erzeugt keinen zweiten Bericht',lambda:expect(202,r))
    changed=dict(r,problem='Anderer Inhalt')
    check('Gleiche Kennung mit anderem Inhalt abgewiesen',lambda:expect(409,changed))
    check('Leere Problembeschreibung abgewiesen',lambda:expect(400,dict(report(),problem='')))
    check('Ungültiges Bild abgewiesen',lambda:expect(400,dict(report(),png=base64.b64encode(b'not a png').decode())))
    png=base64.b64decode('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jRZkAAAAASUVORK5CYII=')
    image=report();image['png']=base64.b64encode(png).decode()
    check('PNG-Bericht angenommen',lambda:expect(202,image))
    expires=str(int(time.time())+3600);sig=hmac.new(secret.encode(),(image['id']+':'+expires).encode(),hashlib.sha256).hexdigest()
    check('Screenshot ohne gültige Signatur gesperrt',lambda:expect(403,suffix='?image='+image['id']+'&expires='+expires+'&signature=wrong'))
    check('Gültiger Bildlink liefert das Bild',lambda: (expect(200,suffix='?image='+image['id']+'&expires='+expires+'&signature='+sig)==png) or (_ for _ in ()).throw(AssertionError('Bild verändert')))
    check('Abgelaufener Link gesperrt',lambda:expect(403,suffix='?image='+image['id']+'&expires=1&signature='+sig))
    for _ in range(3):expect(202,report())
    check('Kundenlimit greift',lambda:expect(429,report()))
    check('Überlange Beschreibung abgewiesen',lambda:expect(400,dict(report(),problem='X'*5001)))
    check('Zu großer HTTP-Body abgewiesen',lambda:expect(413,dict(report(),problem='X'*(3*1024*1024))))
    check('Cron ohne Anmeldung gesperrt',lambda: expect(401,suffix='/../cron.php'))
    db=sqlite3.connect(store/'reports.sqlite')
    assert db.execute('select count(*) from reports').fetchone()[0]==5
    results.append('PASS Genau fünf Berichte, keine Duplikate')
    result=subprocess.run([php,str(root/'server'/'worker.php')],env=env,capture_output=True)
    assert result.returncode==1
    assert db.execute("select count(*) from reports where state='queued'").fetchone()[0]==5
    results.append('PASS Fehlende GitHub-App lässt Warteschlange unverändert')
    db.close()
finally:
    process.terminate();process.wait(timeout=5);log.close()
    (test/'results.txt').write_text('\n'.join(results),encoding='utf-8')
print('\n'.join(results))
raise SystemExit(any(x.startswith('FAIL') for x in results))
