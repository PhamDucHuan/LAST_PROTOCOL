"""Read-only diagnostic for overlapping legacy ground near the shore road."""
import bpy,json,os,sys
source,output=sys.argv[sys.argv.index('--')+1:][:2]
bpy.ops.wm.open_mainfile(filepath=os.path.abspath(source));result=[]
for o in bpy.context.scene.objects:
    if o.type!='MESH' or not o.data.materials:continue
    m=o.data.materials[0].name
    if not any(s in m for s in ('Grass','Soil','Green','Walk','Asphalt')):continue
    if len(o.data.vertices)>100:continue
    vs=[o.matrix_world@v.co for v in o.data.vertices];xs=[-v.x for v in vs];zs=[-v.y for v in vs];ys=[v.z for v in vs]
    if max(xs)<-290 or min(xs)>-240 or max(zs)<95 or min(zs)>155:continue
    result.append(dict(name=o.name,collection=o.users_collection[0].name,material=m,position=tuple(o.location),vertices=[(-v.x,v.z,-v.y) for v in vs]))
with open(output,'w') as f:json.dump(result,f,indent=2)
print('SHORE_SURFACE_DIAGNOSTIC '+str(len(result)),flush=True)
