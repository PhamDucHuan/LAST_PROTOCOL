"""Read-only checks for portable image sources after the architecture pass."""
import bpy, json, os, sys
args=sys.argv[sys.argv.index('--')+1:]
source,result=map(os.path.abspath,args[:2])
bpy.ops.wm.open_mainfile(filepath=source)
with open(os.path.join(os.path.dirname(source),'CanThoCampus.layout.json'),encoding='utf-8') as f:layout=json.load(f)
checks=[]
for entry in layout['materials']:
    for key in ['baseMap','normalMap']:
        if not entry.get(key):continue
        relative=entry[key]
        image=bpy.data.images.get(os.path.splitext(os.path.basename(relative))[0])
        path=os.path.join(os.path.dirname(source),relative)
        checks.append(dict(path=relative,exists=os.path.isfile(path),packed=image is not None and image.packed_file is not None,
                           storedPath=image.filepath if image else None,
                           portable=image is not None and image.filepath.replace('\\','/').startswith('//Textures/'),
                           correctSize=image is not None and tuple(image.size)==(512,512)))
passed=len(checks)==21 and all(c['exists'] and c['packed'] and c['portable'] and c['correctSize'] for c in checks)
with open(result,'w',encoding='utf-8') as f:json.dump(dict(passed=passed,images=checks),f,indent=2)
if not passed:raise RuntimeError('Campus source portability validation failed.')
print('CAMPUS_SOURCE_TEXTURES_21_PASS',flush=True)
