"""Add side elevation detail to the first revision-2 candidate; output is always new."""
import bpy, os, sys, json, shutil, math
from mathutils import Vector
args=sys.argv[sys.argv.index('--')+1:]; source,output=map(os.path.abspath,args[:2])
os.makedirs(output,exist_ok=True)
if os.path.exists(os.path.join(output,'CanThoCampus.blend')): raise RuntimeError('Output already exists.')
bpy.ops.wm.open_mainfile(filepath=os.path.join(source,'CanThoCampus.blend'))
cache={}
def box(zone,name,p,s,mat):
    if mat not in cache:
        bpy.ops.mesh.primitive_cube_add(size=1); temp=bpy.context.object; temp.data.materials.append(bpy.data.materials['CT2_'+mat])
        cache[mat]=temp.data; bpy.data.objects.remove(temp,do_unlink=True)
    obj=bpy.data.objects.new(name,cache[mat]); bpy.data.collections[zone].objects.link(obj)
    obj.location=(-p[0],-p[2],p[1]); obj.scale=(s[0],s[2],s[1])
zone='02_Administration';x,z,w,d,h=28,-2,24,22,25.6
for side in [-1,1]:
    for floor in range(8): box(zone,'Administration side glazing',(x+side*(w/2+0.08),1.9+floor*3.2,z),(0.15,1.9,d-2),'Glass')
    for zz in range(-9,10,3): box(zone,'Administration side fin',(x+side*(w/2+0.3),h/2,z+zz),(0.7,h,0.25),'White')
zone='04_Hightech_ATL';x,z,w,d,h=-33,-21,38,21,24
for side in [-1,1]:
    for floor in range(7):
        box(zone,'ATL side glass ribbon',(x+side*(w/2+0.08),2+floor*3.1,z),(0.15,1.9,d-2),'Glass')
        box(zone,'ATL side floor edge',(x+side*(w/2+0.25),3.2+floor*3.1,z),(0.6,0.25,d+0.3),'Ceramic')
    for zz in [-8,-4,0,4,8]: box(zone,'ATL side vertical rib',(x+side*(w/2+0.4),h/2,z+zz),(0.9,h,0.2),'White')
zone='03_LearningResource';x,z,w,d=57,-25,27,22
for side in [-1,1]:
    for floor in range(3): box(zone,'LRC side glazing',(x+side*(w/2+0.08),2.4+floor*3.5,z),(0.15,2.6,d-2),'Glass')
    for zz in range(-9,10,3): box(zone,'LRC side louver',(x+side*(w/2+0.3),5.4,z+zz),(0.65,9.5,0.18),'Ceramic')
zone='24_TurtleAuditorium';x,z,w,d=-45,16,27,30
for side in [-1,1]:
    box(zone,'Hall side clerestory',(x+side*(w/2+0.08),4.3,z),(0.15,2.6,d-2),'Glass')
    for zz in range(-13,14,3): box(zone,'Hall side pilaster',(x+side*(w/2+0.25),3.5,z+zz),(0.6,7,0.35),'Ceramic')
cam=bpy.data.objects['Turtle Hall Walk'];cam.location=(64,19,6)
cam.rotation_euler=(Vector((45,-12,8))-cam.location).to_track_quat('-Z','Y').to_euler()
shutil.copy2(os.path.join(source,'CanThoCampus.layout.json'),os.path.join(output,'CanThoCampus.layout.json'))
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(output,'CanThoCampus.blend'))
sys.path.insert(0,os.path.dirname(os.path.abspath(__file__)))
from FinishCanThoCampus import finish
finish(output,'--render' in args)
