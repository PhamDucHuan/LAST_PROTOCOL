"""Verify additive revision8 leaves every original mesh and collision intact."""
import bpy, hashlib, json, os, sys
sys.dont_write_bytecode=True
sys.path.insert(0,os.path.dirname(__file__))
from PeripheralGeometry import inside, rectangle, overlap, polygon_near_segment
original,candidate,result=map(os.path.abspath,sys.argv[sys.argv.index('--')+1:][:3])
def snapshot(path):
    bpy.ops.wm.open_mainfile(filepath=path);meshes={};objects={}
    collections={o.name:c.name for c in reversed(list(bpy.data.collections)) for o in c.objects}
    for obj in bpy.context.scene.objects:
        if obj.type!='MESH':continue
        data=obj.data
        if data.name not in meshes:
            value=repr(([tuple(v.co) for v in data.vertices],[tuple(p.vertices) for p in data.polygons],[m.name for m in data.materials]))
            meshes[data.name]=hashlib.sha256(value.encode()).hexdigest()
        objects[obj.name]=(collections[obj.name],meshes[data.name],tuple(tuple(row) for row in obj.matrix_world))
    return objects
before=snapshot(original);after=snapshot(candidate)
with open(os.path.join(os.path.dirname(original),'CanThoCampus.layout.json'),encoding='utf-8') as f:a=json.load(f)
with open(os.path.join(os.path.dirname(candidate),'CanThoCampus.layout.json'),encoding='utf-8') as f:b=json.load(f)
keys=['bounds','campusBoundary','fenceSpans','gates','landmarks','playableRoute','navigationChecks','roads','schematicPlots','layoutTransforms','architecture','peripheralRoads','waterBoundary','waterChecks','neighborhoodRoute','neighborhoodChecks','neighborhoodCameraProbe','neighborhoodCameraEuler']
checks={key:a[key]==b.get(key) for key in keys}
checks['allOriginalMeshesUnchanged']=all(after.get(k)==v for k,v in before.items())
checks['allOriginalCollisionsUnchanged']=b['collisions'][:len(a['collisions'])]==a['collisions']
checks['allOriginal167HousesUnchanged']=len(a['neighborhoodHouses'])==167 and b['neighborhoodHouses'][:167]==a['neighborhoodHouses']
polys=[]
for h in b['neighborhoodHouses']:
    p,s,f=h['position'],h['size'],h['front'];polys.append(rectangle(p['x']+f['x']*1.5,p['z']+f['z']*1.5,s['x']+.4,s['z']+3,(f['x'],f['z'])))
campus=[(p['x'],p['z']) for p in b['campusBoundary']];water=[(p['x'],p['z']) for p in b['waterBoundary']]
checks['allLotsOutsideCampusAndWater']=all(not inside(p,campus) and not inside(p,water) for poly in polys for p in poly)
checks['allLotsSeparated']=not any(overlap(p,q,.9) for i,p in enumerate(polys) for q in polys[i+1:])
streets=[(r,[(p['x'],p['z']) for p in r['points']]) for r in b['peripheralRoads']]
checks['mainRoadCorridorsClear']=not any(polygon_near_segment(poly,p,q,r['width']/2+1.9) for poly in polys for r,points in streets for p,q in zip(points,points[1:]))
checks['localLaneCorridorsClear']=all(not polygon_near_segment(poly,(l['points'][0]['x'],l['points'][0]['z']),(l['points'][1]['x'],l['points'][1]['z']),2.8) for poly in polys for l in b['localLanes'])
images=[i for i in bpy.data.images if i.name.startswith('CT5_')]
checks['portable21Textures']=len(images)==21 and all(i.packed_file and tuple(i.size)==(512,512) and i.filepath.replace('\\','/').startswith('//Textures/') and os.path.isfile(os.path.join(os.path.dirname(candidate),'Textures',i.name+'.png')) for i in images)
with open(os.path.join(os.path.dirname(candidate),'BuildStats.json')) as f:stats=json.load(f)
checks['existingRenderBudgetRetained']=stats['renderers']<500 and stats['triangles']<600000
report=dict(passed=all(checks.values()),checks=checks,preservedMeshObjects=len(before),addedHouses=len(b['neighborhoodHouses'])-167,streetscape=b['streetscapeRevision'],stats=stats)
with open(result,'w',encoding='utf-8') as f:json.dump(report,f,ensure_ascii=False,indent=2)
if not report['passed']:raise RuntimeError(str(report))
print('STREETSCAPE_PRESERVATION_PASS '+str(len(before)),flush=True)
