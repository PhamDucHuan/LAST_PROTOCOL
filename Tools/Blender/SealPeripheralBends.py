"""Add round road/sidewalk caps at polyline bends, preserving all existing meshes."""
import bpy,json,os,shutil,sys
from mathutils import Vector
sys.dont_write_bytecode=True
sys.path.insert(0,os.path.dirname(__file__))
source,output=map(os.path.abspath,sys.argv[sys.argv.index('--')+1:][:2])
if os.path.exists(output):raise RuntimeError('Existing candidate preserved.')
with open(os.path.join(os.path.dirname(source),'CanThoCampus.layout.json'),encoding='utf-8') as f:layout=json.load(f)
if layout['artRevision']!=8:raise RuntimeError('Expected streetscape revision8.')
bpy.ops.wm.open_mainfile(filepath=source)
collection=bpy.data.collections['61_PeripheralRoads'];cache={};count=0
for mat in ['Walk','Asphalt']:
    bpy.ops.mesh.primitive_cylinder_add(vertices=24,radius=1,depth=1)
    o=bpy.context.object;o.data.materials.append(bpy.data.materials['CT5_'+mat]);cache[mat]=o.data;bpy.data.objects.remove(o,do_unlink=True)
for road in layout['peripheralRoads']:
    for p in road['points'][1:-1]:
        for mat,extra,top in [('Walk',3,.048),('Asphalt',0,.078)]:
            o=bpy.data.objects.new('Sealed road bend '+road['id']+' '+mat,cache[mat]);collection.objects.link(o)
            o.location=Vector((-p['x'],-p['z'],top-.02));r=road['width']/2+extra;o.scale=(r,r,.04);count+=1
layout['streetscapeRevision']['sealedBendSurfaces']=count
os.makedirs(output);shutil.copytree(os.path.join(os.path.dirname(source),'Textures'),os.path.join(output,'Textures'))
with open(os.path.join(output,'CanThoCampus.layout.json'),'w',encoding='utf-8') as f:json.dump(layout,f,ensure_ascii=False,indent=2)
bpy.context.view_layer.update();bpy.ops.wm.save_as_mainfile(filepath=os.path.join(output,'CanThoCampus.blend'),relative_remap=False)
from FinishCanThoCampus import finish
finish(output,False)
print('SEALED_ROAD_BENDS_PASS '+str(count),flush=True)
