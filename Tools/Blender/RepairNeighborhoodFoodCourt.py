"""Move the revision6 food court clear of the first housing lot; new output only."""
import bpy, json, os, shutil, sys
sys.dont_write_bytecode=True
source,output=map(os.path.abspath,sys.argv[sys.argv.index('--')+1:][:2])
if os.path.exists(output):raise RuntimeError('Existing candidate preserved.')
with open(os.path.join(os.path.dirname(source),'CanThoCampus.layout.json'),encoding='utf-8') as f:layout=json.load(f)
if layout['artRevision']!=6:raise RuntimeError('Expected revision6.')
stalls=[c for c in layout['collisions'] if c['name']=='Food stall counter']
if len(stalls)!=3 or any(c['position']['x']!=-277 for c in stalls):raise RuntimeError('Food court already moved or has custom positions; preserved.')
bpy.ops.wm.open_mainfile(filepath=source)
os.makedirs(output);shutil.copytree(os.path.join(os.path.dirname(source),'Textures'),os.path.join(output,'Textures'))
prefixes=('Food court','Food stall','Stall post','Outdoor food','Outdoor stool','KHU ẨM THỰC')
for obj in bpy.data.collections['55_NeighborhoodServices'].objects:
    if obj.name.startswith(prefixes):
        obj.location.x+=8
        if obj.name=='Food court paving':obj.scale.x=22
for c in layout['collisions']:
    if c['name'].startswith(prefixes):c['position']['x']-=8
for p in layout['neighborhoodChecks']:
    if p['x'] in (-272,-264) and p['z'] in (-138,-157):p['x']=-280
with open(os.path.join(output,'CanThoCampus.layout.json'),'w',encoding='utf-8') as f:json.dump(layout,f,ensure_ascii=False,indent=2)
bpy.context.view_layer.update();bpy.ops.wm.save_as_mainfile(filepath=os.path.join(output,'CanThoCampus.blend'),relative_remap=False)
sys.path.insert(0,os.path.dirname(__file__))
from FinishCanThoCampus import finish
finish(output,False)
print('CAMPUS_FOODCOURT_CLEARANCE_CORRECTED',flush=True)
