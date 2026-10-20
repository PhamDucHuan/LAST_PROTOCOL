"""Correct roof pitch direction for the Unity-to-Blender X convention."""
import bpy,json,os,shutil,sys
sys.dont_write_bytecode=True
source,output=map(os.path.abspath,sys.argv[sys.argv.index('--')+1:][:2])
if os.path.exists(output):raise RuntimeError('Existing candidate preserved.')
bpy.ops.wm.open_mainfile(filepath=source)
with open(os.path.join(os.path.dirname(source),'CanThoCampus.layout.json'),encoding='utf-8') as f:layout=json.load(f)
if not layout.get('neighborhoodFacadeDetails') or layout.get('neighborhoodRoofCorrected'):raise RuntimeError('Unexpected source.')
count=0
for obj in bpy.context.scene.objects:
    if obj.name.startswith('Neighborhood pitched metal roof'):
        obj.rotation_euler.y *= -1;count+=1
if count!=84:raise RuntimeError('Expected 42 pairs of pitched roofs.')
os.makedirs(output);shutil.copytree(os.path.join(os.path.dirname(source),'Textures'),os.path.join(output,'Textures'))
layout['neighborhoodRoofCorrected']=True
with open(os.path.join(output,'CanThoCampus.layout.json'),'w',encoding='utf-8') as f:json.dump(layout,f,ensure_ascii=False,indent=2)
bpy.context.view_layer.update();bpy.ops.wm.save_as_mainfile(filepath=os.path.join(output,'CanThoCampus.blend'),relative_remap=False)
sys.path.insert(0,os.path.dirname(__file__))
from FinishCanThoCampus import finish
finish(output,False)
print('NEIGHBORHOOD_ROOF_PITCH_PASS '+str(count),flush=True)
