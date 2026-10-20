"""Repair revision5 image paths into a new portable source, without rebuilding art."""
import bpy, os, sys, json, shutil
from mathutils import Vector
sys.dont_write_bytecode=True
args=sys.argv[sys.argv.index('--')+1:]
source,output=map(os.path.abspath,args[:2])
if os.path.exists(output):raise RuntimeError('Output preserved; choose a new directory.')
os.makedirs(output)
shutil.copytree(os.path.join(os.path.dirname(source),'Textures'),os.path.join(output,'Textures'))
shutil.copy2(os.path.join(os.path.dirname(source),'CanThoCampus.layout.json'),output)
bpy.ops.wm.open_mainfile(filepath=source)
for image in bpy.data.images:
    if image.name.startswith('CT5_'):
        image.filepath=os.path.join(output,'Textures',image.name+'.png')
        image.reload();image.pack()
        image.filepath='//Textures/'+image.name+'.png'
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(output,'CanThoCampus.blend'),relative_remap=False)
sys.path.insert(0,os.path.dirname(__file__))
from FinishCanThoCampus import finish
finish(output,False)
def cv(p):return Vector((-p[0],-p[2],p[1]))
scene=bpy.context.scene
bpy.data.collections['90_InspectionLabels'].hide_render=True
for name,p,target in [('Campus-Overview',(240,240,-210),(-30,0,90)),
                      ('Administration-Realism',(-6,10,-39),(22,13,-2)),
                      ('Library-Realism',(82,10,-58),(57,8,-25)),
                      ('Turtle-Realism',(-66,8,-17),(-45,7,16)),
                      ('GateB-Realism',(23,4,-72),(-8,2,-47))]:
    camera=bpy.data.objects['Campus Overview'];scene.camera=camera
    camera.location=cv(p);camera.rotation_euler=(cv(target)-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.type='ORTHO' if name=='Campus-Overview' else 'PERSP';camera.data.ortho_scale=450;camera.data.lens=30
    scene.render.filepath=os.path.join(output,name+'.png');bpy.ops.render.render(write_still=True)
print('CAMPUS_SURFACES_PORTABLE_PASS',flush=True)
