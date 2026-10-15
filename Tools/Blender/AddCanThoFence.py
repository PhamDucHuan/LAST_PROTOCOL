"""Add the campus perimeter to a NEW output directory; keep A/B/C open.
Run with Blender --background --python <this file> -- <source.blend> <output>.
The editable source is saved before the shared material/collection FBX merge.
"""
import bpy
import json
import math
import os
import sys

args = sys.argv[sys.argv.index('--') + 1:]
source, output = map(os.path.abspath, args[:2])
if os.path.exists(output):
    raise RuntimeError('Choose a new output directory; existing art is never overwritten.')
with open(os.path.join(os.path.dirname(source), 'CanThoCampus.layout.json'), encoding='utf-8') as f:
    layout = json.load(f)
if layout.get('artRevision') != 2 or layout.get('campusFenceRevision', 0) != 0:
    raise RuntimeError('Expected the unfenced revision 2 campus; refusing duplicate fences.')
bpy.ops.wm.open_mainfile(filepath=source)
if bpy.data.collections.get('35_CampusFence'):
    raise RuntimeError('Fence collection already exists; source/layout disagree.')
collection = bpy.data.collections.new('35_CampusFence')
bpy.context.scene.collection.children.link(collection)
cache = {}

def xyz(p):
    return dict(x=p[0], y=p[1], z=p[2])

def box(name, p, size, material):
    if material not in cache:
        bpy.ops.mesh.primitive_cube_add(size=1)
        temporary = bpy.context.object
        temporary.data.materials.append(bpy.data.materials['CT2_' + material])
        cache[material] = temporary.data
        bpy.data.objects.remove(temporary, do_unlink=True)
    obj = bpy.data.objects.new(name, cache[material])
    collection.objects.link(obj)
    obj.location = (-p[0], -p[2], p[1])
    obj.scale = (size[0], size[2], size[1])

def fence(name, start, end):
    # Axis-aligned spans meet existing gate pylons or boundary walls. One solid
    # collider per span prevents capsule/AI leakage between decorative pickets.
    dx, dz = end[0] - start[0], end[1] - start[1]
    length = math.hypot(dx, dz)
    ux, uz = dx / length, dz / length
    along_x = abs(ux) > 0.5
    def part(label, distance, y, width, height, depth, material):
        size = (width, height, depth) if along_x else (depth, height, width)
        box(name + ' ' + label, (start[0] + ux * distance, y, start[1] + uz * distance), size, material)
    part('concrete plinth', length / 2, 0.325, length, 0.65, 0.65, 'Concrete')
    part('ceramic coping', length / 2, 0.72, length, 0.14, 0.72, 'Ceramic')
    for y in (1.02, 2.92):
        part('metal rail', length / 2, y, length, 0.12, 0.12, 'Metal')
    part('top rail', length / 2, 3.12, length, 0.16, 0.18, 'Hull')
    # Narrow restrained lighting along the crest matches the existing retrofit.
    part('cyan crest', length / 2, 3.215, length, 0.035, 0.11, 'Cyan')
    panel_count = math.ceil(length / 4)
    spacing = length / panel_count
    for i in range(panel_count + 1):
        d = i * spacing
        part('structural post', d, 1.72, 0.32, 3.44, 0.36, 'Hull')
        part('post cap', d, 3.48, 0.46, 0.12, 0.48, 'Ceramic')
        part('status light', d, 2.95, 0.37, 0.17, 0.4, 'Cyan')
    for i in range(panel_count):
        # Six vertical bars per ~4 m panel; no spiked or floating geometry.
        for j in range(1, 7):
            part('vertical picket', (i + j / 7) * spacing, 1.92, 0.075, 2.24, 0.075, 'Metal')
    size = (length + 0.46, 3.54, 0.72) if along_x else (0.72, 3.54, length + 0.46)
    layout['collisions'].append(dict(name='Campus fence ' + name,
        position=xyz(((start[0] + end[0]) / 2, 1.77, (start[1] + end[1]) / 2)), size=xyz(size)))

# Frontage is behind the monument signs and in front of the guardhouses.
# Pylons occupy gate +/-[8,9.6] m; the 16 m clear opening stays unobstructed.
for name, a, b in [('Front west', -294, -86.6), ('Front C-B', -67.4, -9.6),
                   ('Front B-A', 9.6, 67.4), ('Front east', 86.6, 264)]:
    fence(name, (a, -49.5), (b, -49.5))
fence('West', (-294, -49.5), (-294, 324))
fence('North', (-294, 324), (264, 324))
fence('East', (264, -49.5), (264, 324))
layout['campusFenceRevision'] = 1
layout['campusFenceNote'] = 'Continuous campus perimeter with open A/B/C gates; street and shops remain outside.'
os.makedirs(output)
with open(os.path.join(output, 'CanThoCampus.layout.json'), 'w', encoding='utf-8') as f:
    json.dump(layout, f, ensure_ascii=False, indent=2)
bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(output, 'CanThoCampus.blend'))
sys.path.insert(0, os.path.dirname(__file__))
from FinishCanThoCampus import finish
finish(output, '--render' in args)
