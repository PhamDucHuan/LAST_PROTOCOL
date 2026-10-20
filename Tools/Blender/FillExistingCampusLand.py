"""Revision8 ->9: dense, editable buildings on the original ground, with an atlas."""
import bpy
import json
import math
import os
import shutil
import sys
import numpy as np
from mathutils import Vector

sys.dont_write_bytecode = True
sys.path.insert(0, os.path.dirname(__file__))
from PlanExistingLandInfill import plan

source, output = map(os.path.abspath, sys.argv[sys.argv.index('--')+1:][:2])
if os.path.exists(output):
    raise RuntimeError('Candidate already exists; preserve it.')
with open(os.path.join(os.path.dirname(source), 'CanThoCampus.layout.json'), encoding='utf-8') as f:
    layout = json.load(f)
infill = plan(layout)
bpy.ops.wm.open_mainfile(filepath=source)
os.makedirs(output)
shutil.copytree(os.path.join(os.path.dirname(source), 'Textures'), os.path.join(output, 'Textures'))


def atlas():
    """Original facade drawing: shutters, frames, balconies, weathering and roofs."""
    size, tile = 2048, 512
    pixels = np.ones((size, size, 4), dtype=np.float32)
    height = np.zeros((size, size), dtype=np.float32)
    rng = np.random.default_rng(20261020)
    grain = rng.random((size, size), dtype=np.float32)-.5
    palettes = [(.78, .73, .58), (.66, .75, .72), (.73, .76, .79)]

    def rect(index, x0, y0, x1, y1, color, depth=.0):
        ox, oy = (index % 4)*tile, (index//4)*tile
        x0, x1 = ox+max(0, int(x0)), ox+min(tile, int(x1))
        y0, y1 = oy+max(0, int(y0)), oy+min(tile, int(y1))
        pixels[y0:y1, x0:x1, :3] = color
        height[y0:y1, x0:x1] = depth

    for index in range(9):
        floors = 2+index//3
        wall = np.array(palettes[index % 3])
        rect(index, 0, 0, 512, 512, wall)
        rect(index, 0, 0, 512, 35, wall*.62, -.03)
        step = 480/floors
        rect(index, 30, 40, 350, step-10, wall*.4, -.025)
        rect(index, 45, 43, 337, step-18, (.40, .44, .45), -.04)
        for y in range(48, int(step)-17, 9):
            rect(index, 45, y, 337, y+2, (.25, .29, .30), -.045)
        rect(index, 370, 40, 470, step-10, (.17, .22, .23), -.04)
        rect(index, 380, 51, 458, step-21, (.25, .36, .38), -.06)
        rect(index, 435, 65, 440, 85, (.68, .7, .65), .04)
        # An unbranded shop fascia avoids inventing real businesses/addresses.
        rect(index, 19, step-7, 493, step+16, wall*.50, .04)
        rect(index, 30, step-3, 480, step+9, (.28, .37, .39), .05)
        for floor in range(1, floors):
            y = step*floor+32
            for x in (49, 281):
                rect(index, x-11, y-10, x+181, y+step*.69+10, wall*.53, -.03)
                rect(index, x-5, y-5, x+175, y+step*.69+5, (.81, .82, .76), .08)
                rect(index, x+5, y+4, x+165, y+step*.69-5, (.18, .28, .31), -.06)
                rect(index, x+13, y+10, x+80, y+step*.63, (.29, .40, .42), -.07)
                rect(index, x+82, y+4, x+89, y+step*.69-5, (.67, .72, .71), .10)
                rect(index, x+5, y+step*.42, x+165, y+step*.42+4, (.52, .59, .59), .1)
            rect(index, 15, y-22, 497, y-12, wall*.47, .11)
            rect(index, 20, y-10, 492, y-4, (.72, .73, .69), .13)
            for x in range(24, 495, 25):
                rect(index, x, y-20, x+3, y+17, (.42, .47, .45), .16)
            rect(index, 22, y+14, 492, y+18, (.57, .62, .59), .16)
            # Side-mounted AC and weather stains are deliberately subtle.
            rect(index, 216, y+15, 259, y+45, (.82, .82, .74), .1)
            for k in range(5):
                rect(index, 222, y+19+k*4, 253, y+20+k*4, (.49, .52, .49), .11)
        rect(index, 0, 492, 512, 512, wall*.75, .12)
        rect(index, 7, 20, 12, 490, (.49, .52, .46), .06)

    # Four rear/side/roof tiles. No outside imagery is embedded in the game.
    for index in (9, 10):
        rect(index, 0, 0, 512, 512, (.63, .66, .61) if index == 9 else (.68, .66, .58))
        if index == 9:
            for y in (80, 230, 380):
                for x in (60, 210, 360):
                    rect(index, x-6, y-6, x+83, y+97, (.77, .78, .70), .05)
                    rect(index, x, y, x+77, y+91, (.23, .32, .34), -.06)
                    rect(index, x+35, y, x+40, y+91, (.56, .63, .60), .06)
    rect(11, 0, 0, 512, 512, (.59, .59, .53))
    for x in range(0, 512, 128):
        rect(11, x, 0, x+3, 512, (.39, .41, .37), -.04)
    for y in range(0, 512, 128):
        rect(11, 0, y, 512, y+3, (.39, .41, .37), -.04)
    for index, color in ((12, (.49, .24, .16)), (13, (.28, .42, .46)), (14, (.58, .61, .58)), (15, (.51, .28, .20))):
        rect(index, 0, 0, 512, 512, color)
        for x in range(0, 512, 16):
            rect(index, x, 0, x+3, 512, np.array(color)*.65, -.07)
            rect(index, x+4, 0, x+6, 512, np.minimum(np.array(color)*1.18, .95), .07)
        for y in range(80, 512, 112):
            rect(index, 0, y, 512, y+3, np.array(color)*.72, -.03)
    pixels[:, :, :3] = np.clip(pixels[:, :, :3]*(1+grain[:, :, None]*.055), .02, .96)
    # A thin gutter around every tile reduces mip bleeding.
    for y in range(4):
        for x in range(4):
            ox, oy = x*512, y*512
            pixels[oy:oy+4, ox:ox+512] = pixels[oy+4:oy+5, ox:ox+512]
            pixels[oy+508:oy+512, ox:ox+512] = pixels[oy+507:oy+508, ox:ox+512]
            pixels[oy:oy+512, ox:ox+4] = pixels[oy:oy+512, ox+4:ox+5]
            pixels[oy:oy+512, ox+508:ox+512] = pixels[oy:oy+512, ox+507:ox+508]

    def save_image(name, values, normal=False):
        im = bpy.data.images.new(name, width=size, height=size, alpha=True)
        im.colorspace_settings.name = 'Non-Color' if normal else 'sRGB'
        im.pixels.foreach_set(np.ascontiguousarray(values, dtype=np.float32).ravel())
        im.filepath_raw = os.path.join(output, 'Textures', name+'.png')
        im.file_format = 'PNG'
        im.save()
        im.pack()
        im.filepath = '//Textures/'+name+'.png'
        return im
    base = save_image('CT9_FacadeAtlas_Base', pixels)
    nx = (np.roll(height, 1, axis=1)-np.roll(height, -1, axis=1))*4
    ny = (np.roll(height, 1, axis=0)-np.roll(height, -1, axis=0))*4
    inv = 1/np.sqrt(nx*nx+ny*ny+1)
    pixels[:, :, :3] = np.stack((nx*inv*.5+.5, ny*inv*.5+.5, inv*.5+.5), axis=2)
    normal = save_image('CT9_FacadeAtlas_Normal', pixels, True)
    mat = bpy.data.materials.new('CT9_FacadeAtlas')
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Roughness'].default_value = .83
    tex = mat.node_tree.nodes.new('ShaderNodeTexImage'); tex.image = base
    mat.node_tree.links.new(tex.outputs['Color'], shader.inputs['Base Color'])
    tex = mat.node_tree.nodes.new('ShaderNodeTexImage'); tex.image = normal
    norm = mat.node_tree.nodes.new('ShaderNodeNormalMap'); norm.inputs['Strength'].default_value = .7
    mat.node_tree.links.new(tex.outputs['Color'], norm.inputs['Color'])
    mat.node_tree.links.new(norm.outputs['Normal'], shader.inputs['Normal'])
    layout['materials'].append(dict(name=mat.name, color=dict(r=1, g=1, b=1, a=1), roughness=.83, metallic=0, emission=0,
                                    baseMap='Textures/CT9_FacadeAtlas_Base.png', normalMap='Textures/CT9_FacadeAtlas_Normal.png', normalScale=.7))
    return mat


facade = atlas()
cache = {}


def collection(name):
    c = bpy.data.collections.get(name)
    if c is None:
        c = bpy.data.collections.new(name)
        bpy.context.scene.collection.children.link(c)
    return c


def xyz(x, y, z):
    return dict(x=round(x, 5), y=round(y, 5), z=round(z, 5))


def cv(p):
    return (-p[0], -p[2], p[1])


def uv_tile(index, count):
    # UV origin and image rows both use Blender's bottom-left convention.
    x, y = index % 4, index//4
    u0, v0 = (x*512+7)/2048, (y*512+7)/2048
    u1, v1 = (x*512+505)/2048, (y*512+505)/2048
    return [(u0, v0), (u1, v0), (u1, v1), (u0, v1)][:count]


def mesh(name, h, polygons):
    vertices, faces, coords = [], [], []
    x, z, fx, fz = h['x'], h['z'], *h['front']
    rx, rz = fz, -fx
    for points, tile in polygons:
        offset = len(vertices)
        vertices.extend(cv((x+rx*a+fx*c, b, z+rz*a+fz*c)) for a, b, c in points)
        # The game->Blender reflection reverses winding.
        faces.append(tuple(range(offset+len(points)-1, offset-1, -1)))
        coords.extend(reversed(uv_tile(tile, len(points))))
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    data.materials.append(facade)
    layer = data.uv_layers.new(name='UVMap')
    for i, coord in enumerate(coords):
        layer.data[i].uv = coord
    data.update()
    obj = bpy.data.objects.new(name, data)
    collection(h['collection']).objects.link(obj)


def detail_box(name, h, p, size, material):
    if material not in cache:
        bpy.ops.mesh.primitive_cube_add(size=1)
        o = bpy.context.object
        o.data.materials.append(bpy.data.materials['CT5_'+material])
        cache[material] = o.data
        bpy.data.objects.remove(o, do_unlink=True)
    fx, fz = h['front']; rx, rz = fz, -fx
    o = bpy.data.objects.new(name, cache[material])
    collection(h['collection']).objects.link(o)
    o.location = cv((h['x']+rx*p[0]+fx*p[2], p[1], h['z']+rz*p[0]+fz*p[2]))
    o.scale = (size[0], size[2], size[1])
    o.rotation_euler.z = -math.atan2(fx, fz)


new_houses, access = [], []
foreground = 0
for i, h in enumerate(infill['houses'], 252):
    w, d, top = h['w']/2, h['d']/2, h['height']
    h['collection'] = '67_ExistingLand_'+str((1 if h['x'] > -100 else 0)+(2 if h['z'] > 60 else 0))
    style = (min(4, max(2, h['floors']))-2)*3+h['facade'] % 3
    name = 'Infill building %04d' % i
    faces = [
        ([(-w, 0, d), (w, 0, d), (w, top, d), (-w, top, d)], style),
        ([(w, 0, -d), (-w, 0, -d), (-w, top, -d), (w, top, -d)], 9),
        ([(w, 0, d), (w, 0, -d), (w, top, -d), (w, top, d)], 9),
        ([(-w, 0, -d), (-w, 0, d), (-w, top, d), (-w, top, -d)], 9),
    ]
    roof_rise = 0 if h['roof'] == 'terrace' else 1.2
    if roof_rise:
        tile = {'red_sheet': 12, 'blue_sheet': 13, 'silver_sheet': 14}[h['roof']]
        faces.extend([
            ([(-w-.18, top, -d-.18), (-w-.18, top, d+.18), (0, top+roof_rise, d+.18), (0, top+roof_rise, -d-.18)], tile),
            ([(0, top+roof_rise, -d-.18), (0, top+roof_rise, d+.18), (w+.18, top, d+.18), (w+.18, top, -d-.18)], tile),
            ([(-w, top, d), (w, top, d), (0, top+roof_rise, d)], 10),
            ([(w, top, -d), (-w, top, -d), (0, top+roof_rise, -d)], 10),
        ])
    else:
        faces.append(([(-w, top, d), (w, top, d), (w, top, -d), (-w, top, -d)], 11))
    # Parapets/awnings/balconies are geometry for a limited number near retained roads.
    # Most detail is in the atlas, avoiding a renderer/object explosion.
    if foreground < 64 and i % 3 == 0:
        detail_box('Infill shop canopy', h, (0, 2.9, d+.55), (w*2+.2, .14, 1.25), 'Terracotta')
        if h['floors'] >= 2:
            detail_box('Infill balcony floor', h, (0, 3.3, d+.45), (w*2, .14, 1.05), 'Concrete')
            detail_box('Infill balcony handrail', h, (0, 4.2, d+.94), (w*2, .07, .07), 'Metal')
        foreground += 1
    if h['roof'] == 'terrace' and i % 4 == 0:
        detail_box('Infill rooftop water tank', h, (w*.55, top+.55, -d*.45), (1.15, 1.1, 1.25), 'SilverCladding')
    if h['roof'] == 'terrace' and i % 5 == 0:
        mesh('Infill rooftop solar panel', h,
             [([(-w*.7, top+.12, -d*.25), (w*.3, top+.12, -d*.25), (w*.3, top+.32, d*.3), (-w*.7, top+.32, d*.3)], 13)])
    mesh(name, h, faces)
    yaw = math.degrees(math.atan2(*h['front']))
    house = dict(name=name, position=xyz(h['x'], 0, h['z']), size=xyz(h['w'], top, h['d']),
                 euler=xyz(0, yaw, 0), district=h['district'], front=xyz(h['front'][0], 0, h['front'][1]))
    layout['neighborhoodHouses'].append(house)
    layout['collisions'].append(dict(name=name, position=xyz(h['x'], top/2, h['z']), size=house['size'], euler=house['euler']))
    entry = dict(**house, patch=h['patch'], floors=h['floors'], roof=h['roof'], roofRise=roof_rise)
    new_houses.append(entry)
    if i % 16 == 0:
        access.append(xyz(h['x']+h['front'][0]*(d+2.2), 0, h['z']+h['front'][1]*(d+2.2)))

layout['artRevision'] = 9
layout['note'] = 'Revision9: existing-land building infill. Original 940m x 840m ground, roads, water, campus and routes unchanged. Google Maps visual relationships, gameplay scale.'
layout['infillBuildings'] = new_houses
layout['infillAccessChecks'] = access
layout['buildingInfillRevision'] = dict(originalHouses=251, addedHouses=len(new_houses), totalHouses=len(layout['neighborhoodHouses']),
                                      groundExpanded=False, newRoads=0, patches=infill['patches'], atlasSize=2048,
                                      referenceDate='2026-10-20', originalBounds=layout['bounds'])
with open(os.path.join(output, 'CanThoCampus.layout.json'), 'w', encoding='utf-8') as f:
    json.dump(layout, f, indent=2, ensure_ascii=False)
bpy.context.view_layer.update()
triangles = sum(len(p.vertices)-2 for o in bpy.context.scene.objects if o.type == 'MESH' for p in o.data.polygons)
if triangles >= 600000:
    raise RuntimeError('Existing environment budget exceeded: '+str(triangles))
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(output, 'CanThoCampus.blend'), relative_remap=False)
from FinishCanThoCampus import finish
finish(output, False)
print('EXISTING_LAND_INFILL_PASS '+json.dumps(layout['buildingInfillRevision']), flush=True)
