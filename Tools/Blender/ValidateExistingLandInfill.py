"""Check additive art, original land extent, footprint safety and export budget."""
import bpy
import hashlib
import json
import math
import os
import sys

sys.dont_write_bytecode = True
sys.path.insert(0, os.path.dirname(__file__))
from PlanExistingLandInfill import Parcels, house_lot
from PeripheralGeometry import inside, rectangle, polygon_near_segment

original, candidate, result = map(os.path.abspath, sys.argv[sys.argv.index('--')+1:][:3])


def snapshot(path):
    bpy.ops.wm.open_mainfile(filepath=path)
    objects, meshes = {}, {}
    owner = {o.name: c.name for c in reversed(list(bpy.data.collections)) for o in c.objects}
    for obj in bpy.context.scene.objects:
        if obj.type != 'MESH':
            continue
        data = obj.data
        if data.name not in meshes:
            value = repr(([tuple(v.co) for v in data.vertices], [tuple(p.vertices) for p in data.polygons],
                          [m.name for m in data.materials],
                          [tuple(uv.uv) for uv in data.uv_layers.active.data] if data.uv_layers.active else []))
            meshes[data.name] = hashlib.sha256(value.encode()).hexdigest()
        objects[obj.name] = (owner[obj.name], meshes[data.name], tuple(tuple(row) for row in obj.matrix_world))
    return objects


before = snapshot(original)
after = snapshot(candidate)
with open(os.path.join(os.path.dirname(original), 'CanThoCampus.layout.json'), encoding='utf-8') as f:
    a = json.load(f)
with open(os.path.join(os.path.dirname(candidate), 'CanThoCampus.layout.json'), encoding='utf-8') as f:
    b = json.load(f)
with open(os.path.join(os.path.dirname(candidate), 'BuildStats.json')) as f:
    stats = json.load(f)
keys = [k for k in a if k not in ('artRevision', 'note', 'materials', 'collisions', 'neighborhoodHouses')]
checks = {key+'Unchanged': a[key] == b.get(key) for key in keys}
checks['allOriginalMeshesTransformsMaterialsAndUVsUnchanged'] = all(after.get(k) == v for k, v in before.items())
checks['allOriginalCollisionsUnchanged'] = b['collisions'][:len(a['collisions'])] == a['collisions']
checks['allOriginal251HousesUnchanged'] = len(a['neighborhoodHouses']) == 251 and b['neighborhoodHouses'][:251] == a['neighborhoodHouses']
checks['originalMaterialsRetained'] = b['materials'][:len(a['materials'])] == a['materials']
checks['existingGroundOnly'] = not b['buildingInfillRevision']['groundExpanded'] and b['buildingInfillRevision']['newRoads'] == 0 and a['bounds'] == b['bounds']
ground = next(c for c in a['collisions'] if c['name'] == 'Campus soil')
p, s = ground['position'], ground['size']
minx, maxx, minz, maxz = p['x']-s['x']/2, p['x']+s['x']/2, p['z']-s['z']/2, p['z']+s['z']/2
campus = [(p['x'], p['z']) for p in b['campusBoundary']]
water = [(p['x'], p['z']) for p in b['waterBoundary']]
new = b['infillBuildings']
polys = [house_lot(h) for h in new]
checks['all1100AdditionsInsideOriginalSoil'] = len(new) == 1100 and all(minx+5 < x < maxx-5 and minz+5 < z < maxz-5 for poly in polys for x, z in poly)
checks['noCampusOrWaterEncroachment'] = all(not any(inside(p, boundary) for p in poly)
    and not any(polygon_near_segment(poly, u, v, margin) for u, v in zip(boundary, boundary[1:]+boundary[:1]))
    for poly in polys for boundary, margin in ((campus, 6), (water, 10)))
occupied = Parcels()
for h in a['neighborhoodHouses']:
    occupied.add(house_lot(h))
separated = True
for poly in polys:
    if occupied.intersects(poly, .9):
        separated = False
    occupied.add(poly)
checks['oldAndNewParcelsSeparated'] = separated
segments = [(r['width']/2+1.9, [(p['x'], p['z']) for p in r['points']]) for r in b['peripheralRoads']]
segments += [(r['width']/2+1.1, [(p['x'], p['z']) for p in r['points']]) for r in b['localLanes']]
segments += [(2.9, [(p['x'], p['z']) for p in b['neighborhoodRoute']])]
checks['existingRoadsLanesAndPlayableRouteClear'] = all(not polygon_near_segment(poly, u, v, margin)
    for poly in polys for margin, points in segments for u, v in zip(points, points[1:]))
checks['oneColliderPerAddedBuilding'] = all(sum(c['name'] == h['name'] for c in b['collisions']) == 1 for h in new)
checks['renderBudgetRetained'] = stats['triangles'] < 600000 and stats['renderers'] < 500
images = [i for i in bpy.data.images if i.name.startswith(('CT5_', 'CT9_'))]
checks['all23TexturesPackedAndPortable'] = len(images) == 23 and all(i.packed_file and i.filepath.replace('\\', '/').startswith('//Textures/')
    and os.path.isfile(os.path.join(os.path.dirname(candidate), 'Textures', i.name+'.png')) for i in images)
checks['atlasIs2048'] = all(tuple(bpy.data.images[n].size) == (2048, 2048) for n in ('CT9_FacadeAtlas_Base', 'CT9_FacadeAtlas_Normal'))
report = dict(passed=all(checks.values()), checks=checks, preservedMeshObjects=len(before),
              addedBuildings=len(new), totalBuildings=len(b['neighborhoodHouses']), stats=stats)
with open(result, 'w', encoding='utf-8') as f:
    json.dump(report, f, indent=2, ensure_ascii=False)
if not report['passed']:
    raise RuntimeError(str({k: v for k, v in checks.items() if not v}))
print('EXISTING_LAND_PRESERVATION_PASS '+json.dumps(stats), flush=True)
