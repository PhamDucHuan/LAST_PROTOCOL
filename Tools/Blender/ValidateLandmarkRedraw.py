"""Prove that only the two redrawn Blender collections/layout fields changed."""
import bpy, hashlib, json, os, sys
from array import array
original,candidate,output=map(os.path.abspath,sys.argv[sys.argv.index('--')+1:][:3])
targets={'24_TurtleAuditorium','05_Classrooms_A3'}
def snapshot(path):
    bpy.ops.wm.open_mainfile(filepath=os.path.join(path,'CanThoCampus.blend'),load_ui=False,use_scripts=False)
    excluded={o.name for c in bpy.data.collections if c.name in targets for o in c.all_objects}
    objects={};cache={}
    for o in bpy.context.scene.objects:
        if o.name in excluded:continue
        digest=None
        if o.type=='MESH':
            key=o.data.as_pointer()
            if key not in cache:
                h=hashlib.sha256()
                for coll,field,n,kind in [(o.data.vertices,'co',3,'f'),(o.data.loops,'vertex_index',1,'i')]:
                    values=array(kind,[0])*(len(coll)*n);coll.foreach_get(field,values);h.update(values.tobytes())
                for layer in o.data.uv_layers:
                    values=array('f',[0])*(len(layer.data)*2);layer.data.foreach_get('uv',values);h.update(values.tobytes())
                h.update(str([m.name if m else None for m in o.data.materials]).encode());cache[key]=h.hexdigest()
            digest=cache[key]
        objects[o.name]=dict(type=o.type,matrix=[list(r) for r in o.matrix_world],geometry=digest,groups=sorted(c.name for c in o.users_collection))
    return objects
a=snapshot(original);b=snapshot(candidate)
assert a==b,('Unrelated Blender objects changed',list(set(a)^set(b))[:10],[n for n in a.keys()&b.keys() if a[n]!=b[n]][:10])
with open(os.path.join(original,'CanThoCampus.layout.json'),encoding='utf-8') as f:old=json.load(f)
with open(os.path.join(candidate,'CanThoCampus.layout.json'),encoding='utf-8') as f:new=json.load(f)
allowed={'artRevision','note','sketchfabLandmarks','landmarkRedraw'}
assert {k:v for k,v in old.items() if k not in allowed}=={k:v for k,v in new.items() if k not in allowed}
assert old['artRevision']==12 and new['artRevision']==13
assert old['materials']==new['materials']
assert old['collisions']==new['collisions']
assert old['sketchfabLandmarks'][2]==new['sketchfabLandmarks'][2], 'Sports metadata changed'
for oldentry,newentry in zip(old['sketchfabLandmarks'][:2],new['sketchfabLandmarks'][:2]):
    assert oldentry=={k:v for k,v in newentry.items() if k!='representation'}
retained=old['collisions']
for mat in new['materials']:
    for key in ['baseMap','normalMap']:
        if mat.get(key):assert os.path.isfile(os.path.join(candidate,mat[key])),mat
with open(os.path.join(candidate,'BuildStats.json')) as f:stats=json.load(f)
assert stats['triangles']<600000 and stats['renderers']<500,stats
report=dict(passed=True,unrelatedBlenderObjects=len(a),unrelatedGeometryUVsMaterialsTransformsPreserved=True,
    retainedCollisions=len(retained),removedCollisions=len(old['collisions'])-len(retained),newCollisions=0,sportsHallGeometryPreserved=True,collisionEnvelopesAndNavigationUnchanged=True,
    allLandmarksRoadsLandTreesAndGameplayAnchorsPreserved=True,stats=stats)
with open(output,'w',encoding='utf-8') as f:json.dump(report,f,indent=2)
print('LANDMARK_REDRAW_PRESERVATION_PASS',report,flush=True)
