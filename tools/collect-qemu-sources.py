"""Collect PE dependency closure, original packages, exact MSYS2 recipes and source archives.
Fails closed when an installed runtime package cannot be mapped to its build recipe.
"""
import hashlib,json,os,pathlib,re,shutil,struct,subprocess,tarfile,time,urllib.request
def command(*args,**kwargs):return subprocess.check_output(args,text=True,**kwargs).strip()
out=pathlib.Path('qemu-build-output').resolve(); binary=out/'binary'; prefix=pathlib.Path(command('cygpath','-w','/mingw64'))
cache=pathlib.Path(command('cygpath','-w','/var/cache/pacman/pkg'))
cache.mkdir(parents=True,exist_ok=True)
original_versions=dict(line.split(maxsplit=1) for line in (out/'build-info'/'all-build-packages.txt').read_text().splitlines() if line.strip())
def imports(path):
    b=path.read_bytes(); pe=struct.unpack_from('<I',b,60)[0]; n=struct.unpack_from('<H',b,pe+6)[0]; optsize=struct.unpack_from('<H',b,pe+20)[0]; opt=pe+24
    directory=opt+(112 if struct.unpack_from('<H',b,opt)[0]==0x20b else 96); rva=struct.unpack_from('<I',b,directory+8)[0]
    sections=[]
    for i in range(n):
        vs,va,rs,ptr=struct.unpack_from('<IIII',b,opt+optsize+40*i+8);sections.append((va,max(vs,rs),ptr))
    def offset(r):
        for va,size,ptr in sections:
            if va<=r<va+size:return ptr+r-va
        return r
    if not rva:return []
    p=offset(rva);result=[]
    while any(b[p:p+20]):
        q=offset(struct.unpack_from('<I',b,p+12)[0]); result.append(b[q:b.index(0,q)].decode()); p+=20
    return result
queue=[binary/'qemu-img.exe'];seen=set();packages=set();system=set()
while queue:
    file=queue.pop()
    for dll in imports(file):
        if dll.lower() in seen:continue
        seen.add(dll.lower()); source=prefix/'bin'/dll
        if source.exists():
            shutil.copy2(source,binary/dll);queue.append(source)
            packages.add(command('pacman','-Qqo','/mingw64/bin/'+dll))
        else:system.add(dll)
headers={'User-Agent':'DYE-DiskShift-source-collector'}
if os.environ.get('GH_TOKEN'):headers['Authorization']='Bearer '+os.environ['GH_TOKEN']
def fetch(url):
    for attempt in range(4):
        try:
            with urllib.request.urlopen(urllib.request.Request(url,headers=headers if 'api.github.com' in url else {'User-Agent':headers['User-Agent']}),timeout=90) as r:return r.read()
        except Exception:
            if attempt==3:raise
            time.sleep(3*(attempt+1))
def api(path):return json.loads(fetch('https://api.github.com/repos/msys2/MINGW-packages/'+path))
def download_tree(path,ref,dest):
    dest.mkdir(parents=True,exist_ok=True)
    for entry in api('contents/'+path+'?ref='+ref):
        if entry['type']=='dir':download_tree(entry['path'],ref,dest/entry['name'])
        elif entry['type']=='file':(dest/entry['name']).write_bytes(fetch(entry['download_url']))
        else:raise RuntimeError('Unsupported source recipe entry '+entry['path'])
records=[];done=set()
for package in sorted(packages):
    version=command('pacman','-Q',package).split()[1]
    if original_versions.get(package)!=version:raise RuntimeError('Runtime package drift since compile: '+package)
    archives=list(cache.glob(package+'-'+version.replace(':','_')+'-*.pkg.tar.*'))
    archives=[p for p in archives if not p.name.endswith('.sig')]
    if not archives:
        subprocess.run(['pacman','-Sw','--noconfirm','--cachedir',command('cygpath','-u',str(cache)),package],check=True)
        archives=[p for p in cache.glob(package+'-'+version.replace(':','_')+'-*.pkg.tar.*') if not p.name.endswith('.sig')]
    if not archives:raise RuntimeError('Original binary package missing: '+package+' '+version)
    archive=archives[0]
    info=command('bsdtar','-xOf',str(archive),'.PKGINFO')
    base=re.search(r'^pkgbase = (.+)$',info,re.M).group(1)
    package_info=out/'build-info'/('package-'+package+'.txt');package_info.write_text(info,encoding='utf-8')
    print('Runtime package:',package,version,'recipe:',base,flush=True)
    for installed in command('pacman','-Qlq',package).splitlines():
        if '/share/licenses/' in installed and not installed.endswith('/'):
            license_file=pathlib.Path(command('cygpath','-w',installed))
            relative=installed.split('/share/licenses/',1)[1]
            destination=out/'licenses'/relative
            if license_file.is_file():destination.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(license_file,destination)
    buildinfo=command('bsdtar','-xOf',str(archive),'.BUILDINFO')
    (out/'build-info'/('build-'+package+'.txt')).write_text(buildinfo,encoding='utf-8')
    recipe_hash=re.search(r'^pkgbuild_sha256sum = (.+)$',buildinfo,re.M)
    originals=out/'sources'/'original-binary-packages';originals.mkdir(exist_ok=True)
    shutil.copy2(archive,originals/archive.name)
    if (base,version) in done:continue
    done.add((base,version))
    match=None
    for page in range(1,5):
        history=api('commits?path='+base+'/PKGBUILD&per_page=30&page='+str(page))
        for item in history:
            sha=item['sha']; recipe=fetch('https://raw.githubusercontent.com/msys2/MINGW-packages/'+sha+'/'+base+'/PKGBUILD').decode()
            def value(name):
                m=re.search(r'^'+name+r'=[\'\"]?([^\s\'\"#]+)',recipe,re.M);return m.group(1) if m else ''
            candidate=(value('epoch')+':' if value('epoch') else '')+value('pkgver')+'-'+value('pkgrel')
            if candidate==version and (not recipe_hash or hashlib.sha256(recipe.encode()).hexdigest()==recipe_hash.group(1)):match=sha;break
        if match or not history:break
    if not match:raise RuntimeError('No exact recipe version for '+base+' '+version)
    dest=out/'sources'/'recipes'/base
    download_tree(base,match,dest)
    (dest/'DYE-UPSTREAM-COMMIT.txt').write_text(match+'\n',encoding='ascii')
    # makepkg verifies checksums and creates an all-source archive containing upstream tarballs/patches.
    msys_bash=prefix.parent/'usr'/'bin'/'bash.exe'
    subprocess.run([str(msys_bash),'-lc','cd "$1" && makepkg --allsource --skippgpcheck --noconfirm','source-archive',dest.as_posix()],check=True)
    if not list(dest.glob('*.src.tar.*')):raise RuntimeError('All-source archive missing: '+base)
    records.append({'package':package,'version':version,'pkgbase':base,'recipeCommit':match})
(out/'build-info'/'runtime-packages.json').write_text(json.dumps({'packages':records,'windowsSystemImports':sorted(system)},indent=2),encoding='utf-8')
shutil.copy2(__file__,out/'build-info'/'collect-qemu-sources.py')
hashes={str(p.relative_to(out)).replace('\\','/'):hashlib.sha256(p.read_bytes()).hexdigest() for p in out.rglob('*') if p.is_file()}
(out/'SHA256.json').write_text(json.dumps(hashes,indent=2),encoding='utf-8')
(out/'SOURCE-COLLECTION-COMPLETE.txt').write_text('Dependency recipes and all-source archives collected successfully.\n',encoding='ascii')
