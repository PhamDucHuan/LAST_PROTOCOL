"""Finish secondary elevations of a revision6 candidate; preserve all collision/layout."""
import bpy, json, os, shutil, sys
sys.dont_write_bytecode=True
source,output=map(os.path.abspath,sys.argv[sys.argv.index('--')+1:][:2])
if os.path.exists(output):raise RuntimeError('Existing output preserved.')
with open(os.path.join(os.path.dirname(source),'CanThoCampus.layout.json'),encoding='utf-8') as f:layout=json.load(f)
if layout['artRevision']!=6 or layout.get('neighborhoodFacadeDetails'):raise RuntimeError('Expected undecorated revision6.')
bpy.ops.wm.open_mainfile(filepath=source)
os.makedirs(output);shutil.copytree(os.path.join(os.path.dirname(source),'Textures'),os.path.join(output,'Textures'))
sys.path.insert(0,os.path.dirname(__file__))
from mathutils import Vector
zone='';cache={}
def set_zone(name):
    global zone
    zone=name
def box(name,p,size,mat):
    if mat not in cache:
        bpy.ops.mesh.primitive_cube_add(size=1);obj=bpy.context.object
        obj.data.materials.append(bpy.data.materials['CT5_'+mat]);cache[mat]=obj.data;bpy.data.objects.remove(obj,do_unlink=True)
    obj=bpy.data.objects.new(name,cache[mat]);bpy.data.collections[zone].objects.link(obj)
    obj.location=(-p[0],-p[2],p[1]);obj.scale=(size[0],size[2],size[1]);return obj
from NeighborhoodFacadeDetails import enrich
enrich(layout,box,set_zone)
layout['neighborhoodFacadeDetails']=True
layout['neighborhoodRoofCorrected']=True
with open(os.path.join(output,'CanThoCampus.layout.json'),'w',encoding='utf-8') as f:json.dump(layout,f,ensure_ascii=False,indent=2)
bpy.context.view_layer.update();bpy.ops.wm.save_as_mainfile(filepath=os.path.join(output,'CanThoCampus.blend'),relative_remap=False)
from FinishCanThoCampus import finish
finish(output,False)
print('NEIGHBORHOOD_SECONDARY_FACADES_PASS',flush=True)
