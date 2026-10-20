"""Read-only inventory of the generated Blender source for a layout revision."""
import bpy
import json
import os
import sys
from mathutils import Vector

args = sys.argv[sys.argv.index('--') + 1:]
bpy.ops.wm.open_mainfile(filepath=os.path.abspath(args[0]))
result = []
for collection in bpy.data.collections:
    objects = [o for o in collection.objects if o.type == 'MESH']
    if not objects:
        continue
    points = [o.matrix_world @ Vector(p) for o in objects for p in o.bound_box]
    result.append(dict(name=collection.name, count=len(objects),
                       minimum=[min(p[i] for p in points) for i in range(3)],
                       maximum=[max(p[i] for p in points) for i in range(3)],
                       examples=[o.name for o in objects[:7]]))
with open(args[1], 'w', encoding='utf-8') as output:
    json.dump(result, output, ensure_ascii=False, indent=2)
print('CAMPUS_READ_ONLY_INVENTORY ' + str(len(result)))
