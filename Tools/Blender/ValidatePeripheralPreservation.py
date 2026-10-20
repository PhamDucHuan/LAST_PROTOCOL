"""Read-only preservation and geometric checks for revision7 periphery."""
import bpy,hashlib,json,os,sys
sys.dont_write_bytecode=True
sys.path.insert(0,os.path.dirname(__file__))
from PeripheralGeometry import inside,rectangle,overlap,polygon_near_segment
original,candidate,result=map(os.path.abspath,sys.argv[sys.argv.index('--')+1:][:3])
def snapshot(path):
    bpy.ops.wm.open_mainfile(filepath=path);meshes={};objects={}
    for obj in bpy.context.scene.objects:
        if obj.type!='MESH':continue
        name=obj.users_collection[0].name
        if name in ('26_StreetFront','29_Boundary') or obj.name=='Regional ground' or name.startswith(('50_','51_','52_','53_','55_','60_','61_','62_','63_')):continue
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
checks={key:key in a and a[key]==b.get(key) for key in keys};checks['campusObjectsUnchanged']=before==after
removed=('District house','Awning support','Food stall counter','Outdoor food table','Outdoor stool',
         'Neighborhood emergency cabinet','Emergency supply crate','Neighborhood streetlight pole','Alley wayfinding pillar')
old=[c for c in a['collisions'] if c['name'] not in ('Campus soil','West retaining wall','East retaining wall','North retaining wall','South retaining wall') and not c['name'].startswith(removed)]
checks['campusCollisionBoxesUnchanged']=all(c in b['collisions'] for c in old)
campus=[(p['x'],p['z']) for p in b['campusBoundary']];water=[(p['x'],p['z']) for p in b['waterBoundary']]
polys=[]
for h in b['neighborhoodHouses']:
    p,s,f=h['position'],h['size'],h['front'];polys.append(rectangle(p['x']+f['x']*1.5,p['z']+f['z']*1.5,s['x']+.4,s['z']+3,(f['x'],f['z'])))
checks['housesOutsideCampusAndWater']=all(not inside(p,campus) and not inside(p,water) for poly in polys for p in poly)
checks['nonOverlappingHouseLots']=not any(overlap(p,q,.9) for i,p in enumerate(polys) for q in polys[i+1:])
roads=[(r,[(p['x'],p['z']) for p in r['points']]) for r in b['peripheralRoads']]
checks['allStreetCorridorsClear']=not any(polygon_near_segment(poly,p,q,r['width']/2+1.9) for poly in polys for r,points in roads for p,q in zip(points,points[1:]))
images=[i for i in bpy.data.images if i.name.startswith('CT5_')]
checks['portable21Textures']=len(images)==21 and all(i.packed_file and tuple(i.size)==(512,512) and i.filepath.replace('\\','/').startswith('//Textures/') and os.path.isfile(os.path.join(os.path.dirname(candidate),'Textures',i.name+'.png')) for i in images)
water_obj=bpy.data.objects['Bung Xang water surface'];checks['waterFacesUp']=all(p.normal.z>.99 for p in water_obj.data.polygons)
checks['same167Houses']=len(b['neighborhoodHouses'])==len(a['neighborhoodHouses'])==167
report=dict(passed=all(checks.values()),checks=checks,preservedMeshObjects=len(before),houseCounts={r['id']:sum(h['district']==r['id'] for h in b['neighborhoodHouses']) for r,_ in roads})
with open(result,'w',encoding='utf-8') as f:json.dump(report,f,ensure_ascii=False,indent=2)
if not report['passed']:raise RuntimeError(str(report))
print('CAMPUS_PERIPHERAL_PRESERVATION_PASS '+str(len(before)),flush=True)
