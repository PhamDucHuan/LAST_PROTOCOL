"""Compare original campus objects/layout to expansion without writing either source."""
import bpy, hashlib, json, os, sys
args=sys.argv[sys.argv.index('--')+1:]
original,candidate,result=map(os.path.abspath,args[:3])
def snapshot(path):
    bpy.ops.wm.open_mainfile(filepath=path)
    meshes={};objects={}
    for obj in bpy.context.scene.objects:
        if obj.type!='MESH':continue
        name=obj.users_collection[0].name
        if name in ('26_StreetFront','29_Boundary') or obj.name=='Regional ground' or name.startswith(('50_','51_','52_','53_','55_')):continue
        data=obj.data
        if data.name not in meshes:
            value=repr(([tuple(v.co) for v in data.vertices],[tuple(p.vertices) for p in data.polygons],[m.name for m in data.materials]))
            meshes[data.name]=hashlib.sha256(value.encode()).hexdigest()
        objects[obj.name]=(name,meshes[data.name],tuple(tuple(row) for row in obj.matrix_world))
    return objects
before=snapshot(original);after=snapshot(candidate)
with open(os.path.join(os.path.dirname(original),'CanThoCampus.layout.json'),encoding='utf-8') as f:a=json.load(f)
with open(os.path.join(os.path.dirname(candidate),'CanThoCampus.layout.json'),encoding='utf-8') as f:b=json.load(f)
keys=['campusBoundary','fenceSpans','gates','landmarks','playableRoute','navigationChecks','roads','schematicPlots','layoutTransforms','architecture']
checks={key:key in a and a[key]==b.get(key) for key in keys}
checks['campusObjectsUnchanged']=before==after
checks['housesOutsideCampus']=all(h['position']['z']+h['size']['z']/2<-80 for h in b['neighborhoodHouses'])
oldCollisions=[c for c in a['collisions'] if c['name'] not in ('Campus soil','West retaining wall','East retaining wall','South retaining wall','Neighbourhood shophouse')]
checks['campusCollisionBoxesUnchanged']=all(c in b['collisions'] for c in oldCollisions)
images=[image for image in bpy.data.images if image.name.startswith('CT5_')]
checks['portable21Textures']=len(images)==21 and all(image.packed_file and tuple(image.size)==(512,512)
    and image.filepath.replace('\\','/').startswith('//Textures/')
    and os.path.isfile(os.path.join(os.path.dirname(candidate),'Textures',image.name+'.png')) for image in images)
roof_checks=[]
for obj in bpy.context.scene.objects:
    if not obj.name.startswith('Neighborhood pitched metal roof'):continue
    x,z=-obj.location.x,-obj.location.y
    home=min(b['neighborhoodHouses'],key=lambda h:(h['position']['x']-x)**2+(h['position']['z']-z)**2)
    roof_checks.append(obj.rotation_euler.y*(x-home['position']['x'])<0)
checks['roofsSlopeOutwardFromRidge']=len(roof_checks)==84 and all(roof_checks)
report=dict(passed=all(checks.values()),checks=checks,preservedMeshObjects=len(before))
with open(result,'w',encoding='utf-8') as f:json.dump(report,f,indent=2)
if not report['passed']:raise RuntimeError(str(report))
print('CAMPUS_PRESERVATION_PASS '+str(len(before)),flush=True)
