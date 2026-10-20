"""Densify revision5 leaf cards including the foreground groves; preserve colliders."""
import bpy, json, math, os, random, shutil, sys
from mathutils import Vector
sys.dont_write_bytecode=True
args=sys.argv[sys.argv.index('--')+1:];source,output=map(os.path.abspath,args[:2])
if os.path.exists(output):raise RuntimeError('Choose a new output directory.')
os.makedirs(output)
shutil.copytree(os.path.join(os.path.dirname(source),'Textures'),os.path.join(output,'Textures'))
with open(os.path.join(os.path.dirname(source),'CanThoCampus.layout.json'),encoding='utf-8') as f:layout=json.load(f)
if layout['artRevision']!=5:raise RuntimeError('Expected architecture revision5.')
bpy.ops.wm.open_mainfile(filepath=source)
def cv(p):return Vector((-p[0],-p[2],p[1]))
collection=bpy.data.collections['27_Landscape']
trunkdata=next(o.data for o in collection.objects if o.name.startswith('Branching shade trunk'))
card=bpy.data.meshes['Leaf card']
for obj in list(collection.objects):bpy.data.objects.remove(obj,do_unlink=True)
for obj in list(bpy.data.collections['36_CampusRoute'].objects):
    if obj.name.startswith(('Shade tree trunk','Shade tree branch','Layered broadleaf canopy')):bpy.data.objects.remove(obj,do_unlink=True)
def beam(a,b,r):
    aa,bb=cv(a),cv(b);v=bb-aa;o=bpy.data.objects.new('Shade trunk / branch',trunkdata);collection.objects.link(o)
    o.location=(aa+bb)/2;o.scale=(r,r,v.length);o.rotation_euler=v.to_track_quat('Z','Y').to_euler()
random.seed(19662048)
for entry in layout['collisions']:
    if entry['name'] not in ('Shade tree trunk','Campus route tree trunk'):continue
    p,s=entry['position'],entry['size'];x,z,h=p['x'],p['z'],s['y']
    beam((x,0,z),(x+.12,h,z),.21)
    for branch in range(4):
        a=branch*math.tau/4+random.random()*.5;beam((x,h*.58,z),(x+math.cos(a)*2.2,h+.2,z+math.sin(a)*2.2),.08)
    for i in range(64):
        a=random.uniform(0,math.tau);r=3.3*math.sqrt(random.random());o=bpy.data.objects.new('Leaf cluster card',card);collection.objects.link(o)
        o.location=cv((x+math.cos(a)*r,h+random.uniform(-.2,2),z+math.sin(a)*r))
        o.rotation_euler=(random.uniform(-.9,.9),random.uniform(-.7,.7),random.uniform(0,math.tau));o.scale=(3.2,1,2.8)
layout['architecture']['foregroundTrees']='Branching trunks and 64 cutout cards per tree; collision unchanged'
with open(os.path.join(output,'CanThoCampus.layout.json'),'w',encoding='utf-8') as f:json.dump(layout,f,ensure_ascii=False,indent=2)
for im in bpy.data.images:
    if im.name.startswith('CT5_'):
        im.filepath='//Textures/'+im.name+'.png'
bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(output,'CanThoCampus.blend'),relative_remap=False)
sys.path.insert(0,os.path.dirname(__file__))
from FinishCanThoCampus import finish
finish(output,False)
print('CAMPUS_CANOPIES_PASS',flush=True)
