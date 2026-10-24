"""Read-only inspection of the existing parcels before importing Sketchfab art."""
import bpy, json, os, sys
from mathutils import Vector

root, output = map(os.path.abspath, sys.argv[sys.argv.index('--') + 1:][:2])
os.makedirs(output, exist_ok=True)
source = os.path.join(root, 'ArtSource/Blender/CanThoCampus')
with open(os.path.join(source, 'CanThoCampus.layout.json'), encoding='utf-8') as f:
    layout = json.load(f)
bpy.ops.wm.open_mainfile(filepath=os.path.join(source, 'CanThoCampus.blend'), load_ui=False, use_scripts=False)
target_ids = {'24_TurtleAuditorium', '05_Classrooms_A3', '13_SportEducation', '25_Sports'}
targets = [c for c in bpy.data.collections if c.name in target_ids]
report = {'artRevision': layout['artRevision'], 'collections': [], 'landmarks': [], 'collisions': []}
for collection in targets:
    objects = [o for o in collection.all_objects if o.type == 'MESH']
    points = [o.matrix_world @ Vector(v) for o in objects for v in o.bound_box]
    lo = [min(p[i] for p in points) for i in range(3)]
    hi = [max(p[i] for p in points) for i in range(3)]
    report['collections'].append(dict(name=collection.name, objects=len(objects),
        triangles=sum(len(p.vertices)-2 for o in objects for p in o.data.polygons),
        unityMin=[-hi[0],lo[2],-hi[1]], unityMax=[-lo[0],hi[2],-lo[1]],
        objectNames=[o.name for o in objects]))
report['landmarks'] = [x for x in layout['landmarks'] if x['id'] in target_ids or x['id'].startswith('05_Classrooms_A3_')]
report['collisions'] = [dict(index=i, **c) for i,c in enumerate(layout['collisions'])
                        if any(t in c['name'] for t in ('Turtle', 'A3', 'THI ĐẤU', 'Sports', 'Gym'))]
report['preserve'] = dict(roads=layout['roads'], bounds=layout['bounds'],
                          turtleRoadClearance=layout.get('turtleRoadClearance'),
                          roadTreeClearance=layout.get('roadTreeClearance',{}).get('movedTrees'))
with open(os.path.join(output,'ExistingParcels.json'),'w',encoding='utf-8') as f:
    json.dump(report,f,indent=2,ensure_ascii=False)
print('LANDMARK_PARCEL_INSPECTION_PASS', [(x['name'],x['unityMin'],x['unityMax']) for x in report['collections']], flush=True)
