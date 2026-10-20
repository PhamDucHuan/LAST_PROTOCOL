"""Survey source geometry without modifying downloaded files or campus art."""
import bpy, json, os, sys, collections
from mathutils import Vector
root, out = map(os.path.abspath,sys.argv[sys.argv.index('--')+1:][:2])
os.makedirs(out,exist_ok=True)
result={}
def inspect(label):
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    membership={o.name:c.name for c in bpy.data.collections for o in c.objects}
    info=[];groups=collections.Counter()
    for o in meshes:
        points=[o.matrix_world@Vector(p) for p in o.bound_box]
        tris=sum(len(p.vertices)-2 for p in o.data.polygons)
        groups[membership.get(o.name,'-')]+=tris
        info.append(dict(name=o.name,group=membership.get(o.name),triangles=tris,min=[min(p[i] for p in points) for i in range(3)],max=[max(p[i] for p in points) for i in range(3)],materials=[m.name if m else None for m in o.data.materials]))
    result[label]=dict(groups=dict(groups),objects=info,materials=[dict(name=m.name,images=[dict(name=n.image.name,path=n.image.filepath,size=list(n.image.size),packed=bool(n.image.packed_file)) for n in m.node_tree.nodes if n.type=='TEX_IMAGE' and n.image]) for m in bpy.data.materials if m.users and m.use_nodes])
base=os.path.join(root,'ArtSource/ThirdParty/VietnamFreeAssets')
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=os.path.join(base,'ModernHouses20/LowPoly_Modern_Houses_fbx.FBX'))
inspect('houses20')
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=os.path.join(base,'TownHouse/Extracted/Residental_building/residental_building.fbx'))
inspect('townHouse')
bpy.ops.wm.open_mainfile(filepath=os.path.join(root,'ArtSource/Blender/CanThoCampus/CanThoCampus.blend'),load_ui=False,use_scripts=False)
inspect('campus')
with open(os.path.join(out,'SourceSurvey.json'),'w',encoding='utf-8') as f:json.dump(result,f,indent=2)
print('MODERN_HOUSE_SURVEY_PASS',flush=True)
