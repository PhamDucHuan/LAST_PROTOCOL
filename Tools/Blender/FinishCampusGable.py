"""Photo correction to the glazed Turtle Hall gable; new portable revision5 output."""
import bpy, json, os, shutil, sys
from mathutils import Vector
sys.dont_write_bytecode=True
args=sys.argv[sys.argv.index('--')+1:];source,output=map(os.path.abspath,args[:2])
if os.path.exists(output):raise RuntimeError('Existing candidate preserved.')
os.makedirs(output);shutil.copytree(os.path.join(os.path.dirname(source),'Textures'),os.path.join(output,'Textures'))
with open(os.path.join(os.path.dirname(source),'CanThoCampus.layout.json'),encoding='utf-8') as f:layout=json.load(f)
bpy.ops.wm.open_mainfile(filepath=source)
collection=bpy.data.collections['24_TurtleAuditorium']
roof=next(o for o in collection.objects if o.name=='Turtle Hall concrete pitched shell')
points=[v.co[:] for v in roof.data.vertices]
sides=[tuple(p.vertices) for p in roof.data.polygons if len(p.vertices)==4]
gables=[tuple(p.vertices) for p in roof.data.polygons if len(p.vertices)==3]
material=roof.data.materials[0]
data=bpy.data.meshes.new('Open-ended concrete roof');data.from_pydata(points,[],sides);data.materials.append(material);data.update();roof.data=data
data=bpy.data.meshes.new('Glazed gable');data.from_pydata(points,[],gables);data.materials.append(bpy.data.materials['CT5_Glass']);data.update()
obj=bpy.data.objects.new('Turtle Hall glazed upper gables',data);collection.objects.link(obj)
for obj in collection.objects:
    if obj.name.startswith('Turtle Hall gable mullion'):
        obj.location.y += 1.15 if -obj.location.y < 16 else -1.15
layout['architecture']['turtleHallGable']='Glazed infill and exterior mullions under concrete roof shell'
with open(os.path.join(output,'CanThoCampus.layout.json'),'w',encoding='utf-8') as f:json.dump(layout,f,ensure_ascii=False,indent=2)
bpy.context.view_layer.update();bpy.ops.wm.save_as_mainfile(filepath=os.path.join(output,'CanThoCampus.blend'),relative_remap=False)
sys.path.insert(0,os.path.dirname(__file__))
from FinishCanThoCampus import finish
finish(output,False)
print('CAMPUS_GABLE_CORRECTION_PASS',flush=True)
