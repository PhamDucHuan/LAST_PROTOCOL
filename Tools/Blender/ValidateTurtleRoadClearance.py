"""Check the repair did not alter unrelated source geometry or transforms."""
import bpy,hashlib,json,os,sys
from array import array
from mathutils import Matrix,Vector
original,candidate,report=sys.argv[sys.argv.index('--')+1:][:3]
def snapshot(path):
    bpy.ops.wm.open_mainfile(filepath=os.path.abspath(path),load_ui=False,use_scripts=False)
    geometry={};objects={}
    groups={o.name:[] for o in bpy.context.scene.objects}
    for collection in bpy.data.collections:
        for obj in collection.objects:
            if obj.name in groups:groups[obj.name].append(collection.name)
    for obj in bpy.context.scene.objects:
        digest=None
        if obj.type=='MESH':
            key=obj.data.as_pointer()
            if key not in geometry:
                h=hashlib.sha256()
                for collection,field,count,kind in [(obj.data.vertices,'co',3,'f'),(obj.data.loops,'vertex_index',1,'i')]:
                    values=array(kind,[0])*(len(collection)*count);collection.foreach_get(field,values);h.update(values.tobytes())
                for layer in obj.data.uv_layers:
                    values=array('f',[0])*(len(layer.data)*2);layer.data.foreach_get('uv',values);h.update(values.tobytes())
                h.update(str([m.name if m else None for m in obj.data.materials]).encode())
                geometry[key]=h.hexdigest()
            digest=geometry[key]
        objects[obj.name]=dict(type=obj.type,matrix=[list(r) for r in obj.matrix_world],geometry=digest,
            groups=sorted(groups[obj.name]))
    return objects
a=snapshot(original);b=snapshot(candidate)
assert set(b)-set(a)=={'Turtle Hall plot fit'}
assert not set(a)-set(b)
transform=Matrix.Translation(Vector((45.5,-13.5,0))) @ Matrix.Diagonal((.9,.6,1,1)) @ Matrix.Translation(Vector((-45,16,0)))
changed=[]
for name,x in a.items():
    y=b[name]
    assert all(x[k]==y[k] for k in ['type','geometry','groups']),name
    expected=transform @ Matrix(x['matrix']) if '24_TurtleAuditorium' in x['groups'] else Matrix(x['matrix'])
    assert max(abs(expected[i][j]-y['matrix'][i][j]) for i in range(4) for j in range(4))<.00002,name
    if x['matrix']!=y['matrix']:changed.append(name)
assert len(changed)==82,len(changed)
result=dict(passed=True,totalSourceObjects=len(a),changedTransforms=len(changed),unchangedMeshGeometryAndUVs=True,changedObjects=changed)
with open(report,'w') as f:json.dump(result,f,indent=2)
print('TURTLE_SOURCE_PRESERVATION_PASS '+str(len(a))+' objects, '+str(len(changed))+' transforms')
