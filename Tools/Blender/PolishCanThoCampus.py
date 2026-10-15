"""One revision of the generated source to separate coplanar road intersections.
Creates a new output folder; does not overwrite the input .blend or exported art.
"""
import bpy
import os
import sys
import shutil
import bmesh
from mathutils import Vector

args=sys.argv[sys.argv.index('--')+1:]
source,output=map(os.path.abspath,args[:2])
if os.path.exists(os.path.join(output,'CanThoCampus.blend')): raise RuntimeError('Use a new output directory.')
os.makedirs(output,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=os.path.join(source,'CanThoCampus.blend'))
roads=sorted([o for o in bpy.data.collections['00_Ground'].objects if ' road' in o.name],key=lambda o:o.name)
for index,obj in enumerate(roads): obj.location.z=0.065+(index+1)*0.002
walks=sorted([o for o in bpy.data.collections['00_Ground'].objects if ' walk' in o.name],key=lambda o:o.name)
for index,obj in enumerate(walks): obj.location.z=0.025+(index+1)*0.0008
bpy.context.view_layer.update()
for obj in bpy.context.scene.objects:
    if obj.name.startswith(('Lane divider','Crosswalk')):
        tops=[]
        for road in roads:
            local=road.matrix_world.inverted() @ obj.location
            if abs(local.x)<0.501 and abs(local.y)<0.501: tops.append(road.location.z+0.035)
        obj.location.z=max(tops,default=0.1)+0.009
if '--correct-roofs' in args:
    for obj in bpy.context.scene.objects:
        if obj.type=='MESH' and any(obj.data.name.startswith(name) for name in ('Terracotta roof','Guardhouse roof','Gym roof','Shop roof','Turtle shell roof')):
            bm=bmesh.new(); bm.from_mesh(obj.data)
            bmesh.ops.reverse_faces(bm,faces=list(bm.faces)); bm.to_mesh(obj.data); bm.free(); obj.data.update()
bpy.data.objects['Campus Overview'].data.ortho_scale=780
camera=bpy.data.objects['Turtle Hall Walk']; camera.location=(15,6,5)
camera.rotation_euler=(Vector((45,-16,5))-camera.location).to_track_quat('-Z','Y').to_euler()
shutil.copy2(os.path.join(source,'CanThoCampus.layout.json'),os.path.join(output,'CanThoCampus.layout.json'))
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(output,'CanThoCampus.blend'))
sys.path.insert(0,os.path.dirname(os.path.abspath(__file__)))
from FinishCanThoCampus import finish
finish(output,'--render' in args)
