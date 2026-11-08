import struct,json,pathlib
root=pathlib.Path(r'E:\##Projet-2026\DYE DiskShift\Release-0.2.0\qemu')
installed=pathlib.Path(r'C:\Program Files\qemu')
def imports(path):
 b=path.read_bytes(); pe=struct.unpack_from('<I',b,60)[0]; n=struct.unpack_from('<H',b,pe+6)[0]; optsize=struct.unpack_from('<H',b,pe+20)[0]; opt=pe+24; magic=struct.unpack_from('<H',b,opt)[0]; directory=opt+(112 if magic==0x20b else 96); rva=struct.unpack_from('<I',b,directory+8)[0]
 sections=[]
 for i in range(n):
  off=opt+optsize+40*i; vs,va,rs,ptr=struct.unpack_from('<IIII',b,off+8); sections.append((va,max(vs,rs),ptr))
 def offset(r):
  for va,size,ptr in sections:
   if va<=r<va+size:return ptr+r-va
  return r
 if not rva:return []
 pos=offset(rva); names=[]
 while any(b[pos:pos+20]):
  name=struct.unpack_from('<I',b,pos+12)[0]; p=offset(name); names.append(b[p:b.index(0,p)].decode());pos+=20
 return names
keep={'qemu-img.exe'}; queue=['qemu-img.exe']; system=set()
while queue:
 f=queue.pop()
 for name in imports(installed/f):
  if (installed/name).exists():
   if name.lower() not in {x.lower() for x in keep}:keep.add(name);queue.append(name)
  else:system.add(name)
report={'bundled':sorted(keep),'windows_system_imports':sorted(system)}
(root.parent/'qemu-dependencies.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))
# Only remove redundant copies from the new package, never from the installation.
for f in root.glob('*.dll'):
 if f.name.lower() not in {x.lower() for x in keep}:f.unlink()
