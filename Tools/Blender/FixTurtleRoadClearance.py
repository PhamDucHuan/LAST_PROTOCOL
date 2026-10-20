"""Fit the complete Turtle Hall footprint inside its existing campus plot."""
import bpy,json,os,shutil,sys
from mathutils import Matrix,Vector
sys.dont_write_bytecode=True
sys.path.insert(0,os.path.dirname(__file__))
from PeripheralGeometry import polygon_near_segment,distance_segment
root,output=map(os.path.abspath,sys.argv[sys.argv.index('--')+1:][:2])
source=os.path.join(root,'ArtSource/Blender/CanThoCampus')
if os.path.exists(output):raise RuntimeError('Use a new candidate directory.')
os.makedirs(output)
shutil.copytree(os.path.join(source,'Textures'),os.path.join(output,'Textures'))
with open(os.path.join(source,'CanThoCampus.layout.json'),encoding='utf-8') as f:layout=json.load(f)
assert layout['artRevision']==11 and not layout.get('turtleRoadClearance')
bpy.ops.wm.open_mainfile(filepath=os.path.join(source,'CanThoCampus.blend'),load_ui=False,use_scripts=False)
objects=list(bpy.data.collections['24_TurtleAuditorium'].objects)
# Game (-45,16) -> (-45.5,13.5), width .9, depth .6, height unchanged.
transform=Matrix.Translation(Vector((45.5,-13.5,0))) @ Matrix.Diagonal((.9,.6,1,1)) @ Matrix.Translation(Vector((-45,16,0)))
fit=bpy.data.objects.new('Turtle Hall plot fit',None)
bpy.data.collections['24_TurtleAuditorium'].objects.link(fit)
fit.matrix_world=transform
# Parent scaling preserves the shear of rotated fascia pieces exactly. Assigning
# a nonuniform world matrix to each rotated object would decompose that shear.
for obj in objects:
    original=obj.matrix_world.copy()
    assert obj.parent is None,obj.name
    obj.parent=fit
    obj.matrix_parent_inverse=Matrix.Identity(4)
    obj.matrix_basis=original
for c in layout['collisions']:
    if c['name']=='Turtle auditorium body' or c['name'].startswith('Turtle Hall '):
        p,s=c['position'],c['size']
        p['x']=-45.5+(p['x']+45)*.9;p['z']=13.5+(p['z']-16)*.6
        s['x']*=.9;s['z']*=.6
for landmark in layout['landmarks']:
    if landmark['id']=='24_TurtleAuditorium':landmark['position'].update(x=-45.5,z=13.5)
bpy.context.view_layer.update()
points=[obj.matrix_world @ Vector(v) for obj in objects if obj.type=='MESH' for v in obj.bound_box]
lo=[min(p[i] for p in points) for i in range(3)];hi=[max(p[i] for p in points) for i in range(3)]
poly=[(-hi[0],-hi[1]),(-hi[0],-lo[1]),(-lo[0],-lo[1]),(-lo[0],-hi[1])]
checks=[]
for road in layout['roads']:
    a=(road['a']['x'],road['a']['z']);b=(road['b']['x'],road['b']['z'])
    assert not polygon_near_segment(poly,a,b,road['width']/2+3),road['name']
    checks.append(dict(name=road['name'],clear=True))
assert min(distance_segment((-22,22),a,b) for a,b in zip(poly,poly[1:]+poly[:1]))>9.7
# No other source object or collision entry is changed; keep imported-house revision 11.
layout['turtleRoadClearance']=dict(revision=1,originalCenter=dict(x=-45,z=16),center=dict(x=-45.5,z=13.5),
    widthScale=.9,depthScale=.6,heightScale=1,minimumSidewalkSetback=1,groundExpanded=False)
with open(os.path.join(output,'CanThoCampus.layout.json'),'w',encoding='utf-8') as f:json.dump(layout,f,indent=2,ensure_ascii=False)
report=dict(passed=True,changedGroup='24_TurtleAuditorium',changedObjects=len(objects),footprint=poly,roads=checks,
    originalArtRevision=11,artRevision=11,repair=layout['turtleRoadClearance'])
with open(os.path.join(output,'IntegrationValidation.json'),'w') as f:json.dump(report,f,indent=2)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(output,'CanThoCampus.blend'),relative_remap=False)
from FinishCanThoCampus import finish
finish(output,False)
print('TURTLE_ROAD_CLEARANCE_PASS '+json.dumps(report),flush=True)
