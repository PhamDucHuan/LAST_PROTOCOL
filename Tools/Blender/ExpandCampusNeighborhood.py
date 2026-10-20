"""Expand editable campus revision5 with a playable, reference-inspired neighborhood.

No external artwork or map tiles are embedded. Original campus geometry and fence
are retained. New output only, suitable for staging before promoting Unity assets.
"""
import bpy
import json
import math
import os
import random
import shutil
import sys
from mathutils import Vector

sys.dont_write_bytecode = True
source, output = map(os.path.abspath, sys.argv[sys.argv.index('--') + 1:][:2])
if os.path.exists(output):
    raise RuntimeError('Existing output preserved; use a new candidate folder.')
with open(os.path.join(os.path.dirname(source), 'CanThoCampus.layout.json'), encoding='utf-8') as f:
    layout = json.load(f)
if layout.get('artRevision') != 5:
    raise RuntimeError('Expected matching editable revision5 source and layout.')
bpy.ops.wm.open_mainfile(filepath=source)
os.makedirs(output)
shutil.copytree(os.path.join(os.path.dirname(source), 'Textures'), os.path.join(output, 'Textures'))
random.seed(19662049)
cache = {}
zone = '50_NeighborhoodRoads'
houses = []


def cv(p): return Vector((-p[0], -p[2], p[1]))
def xyz(p): return dict(x=round(p[0], 4), y=round(p[1], 4), z=round(p[2], 4))
def collection():
    c = bpy.data.collections.get(zone)
    if c is None:
        c = bpy.data.collections.new(zone)
        bpy.context.scene.collection.children.link(c)
    return c


def box(name, p, size, mat='Plaster', solid=False):
    key = ('box', mat)
    if key not in cache:
        bpy.ops.mesh.primitive_cube_add(size=1)
        tmp = bpy.context.object
        tmp.data.materials.append(bpy.data.materials['CT5_' + mat])
        cache[key] = tmp.data
        bpy.data.objects.remove(tmp, do_unlink=True)
    obj = bpy.data.objects.new(name, cache[key]); collection().objects.link(obj)
    obj.location = cv(p); obj.scale = (size[0], size[2], size[1])
    if solid:
        layout['collisions'].append(dict(name=name, position=xyz(p), size=xyz(size)))
    return obj


def cylinder(name, p, r, h, mat='Metal'):
    key = ('cylinder', mat)
    if key not in cache:
        bpy.ops.mesh.primitive_cylinder_add(vertices=10, radius=1, depth=1)
        tmp = bpy.context.object; tmp.data.materials.append(bpy.data.materials['CT5_' + mat])
        cache[key] = tmp.data; bpy.data.objects.remove(tmp, do_unlink=True)
    obj = bpy.data.objects.new(name, cache[key]); collection().objects.link(obj)
    obj.location = cv(p); obj.scale = (r, r, h)
    return obj


font = bpy.data.fonts.load('C:/Windows/Fonts/arialbd.ttf')
def sign(text, p, width, north=True):
    data = bpy.data.curves.new(text, 'FONT'); data.body = text; data.font = font
    data.size = .48; data.align_x = 'CENTER'; data.resolution_u = 1; data.extrude = 0
    data.materials.append(bpy.data.materials['CT5_Plaster'])
    obj = bpy.data.objects.new(text, data); collection().objects.link(obj); obj.location = cv(p)
    obj.rotation_euler = (math.pi / 2, 0, 0 if north else math.pi)
    bpy.context.view_layer.update()
    if obj.dimensions.x > width: obj.scale *= width / obj.dimensions.x
    bpy.ops.object.select_all(action='DESELECT'); obj.select_set(True); bpy.context.view_layer.objects.active = obj
    bpy.ops.object.convert(target='MESH')


# Replace the original generic single frontage, and move the outer world limit.
# The campus perimeter collection / gates are deliberately not touched.
bpy.data.batch_remove(ids=tuple(bpy.data.collections['26_StreetFront'].objects))
layout['collisions'] = [c for c in layout['collisions'] if c['name'] != 'Neighbourhood shophouse']
ground = bpy.data.objects['Regional ground']; ground.location = cv((-15, -.25, 5)); ground.scale = (560, 640, .5)
for c in layout['collisions']:
    if c['name'] == 'Campus soil': c.update(position=xyz((-15, -.25, 5)), size=xyz((560, .5, 640)))
    if c['name'] in ['West retaining wall', 'East retaining wall']:
        c['position']['z'] = 5; c['size']['z'] = 640
        obj = bpy.data.objects[c['name']]; obj.location = cv(tuple(c['position'][k] for k in ('x','y','z')))
        obj.scale = (c['size']['x'], c['size']['z'], c['size']['y'])
    if c['name'] == 'South retaining wall':
        c['position']['z'] = -314; bpy.data.objects[c['name']].location = cv((-15, 1, -314))
layout['bounds'] = dict(center=xyz((-15, 10, 5)), size=xyz((560, 40, 640)))

# Flush surfaces use the single continuous collision ground; no raised invisible
# road colliders or step barriers. Keep drains outside the through lanes.
box('Neighborhood concrete base', (-15, .011, -195), (554, .022, 236), 'Concrete')
for z, width in [(-127, 10), (-190, 9), (-250, 10)]:
    box('Residential cross street', (-15, .036, z), (550, .032, width), 'Asphalt')
    for side in [-1, 1]:
        box('Residential sidewalk', (-15, .043, z + side * (width/2 + 1.5)), (550, .045, 3), 'Walk')
for x in [-77, 0, 77]:
    box('Student service alley', (x, .039, -169), (8, .038, 184), 'Asphalt')
    for side in [-1, 1]:
        box('Alley pedestrian edge', (x + side * 5.5, .046, -169), (3, .04, 184), 'Walk')
    for z in range(-75, -54, 3):
        box('Gate pedestrian crossing', (x, .13, z), (7, .025, 1.1), 'Plaster')
    for z in [-82, -122, -185, -245]:
        for dx in [-8.2, 8.2]:
            box('Storm drain', (x+dx, .075, z), (.7, .04, 1.4), 'Shadow')
            for dz in [-.5, -.25, 0, .25, .5]:
                box('Drain grate', (x+dx, .10, z+dz), (.65, .02, .045), 'Metal')


def house(x, z, w, depth, floors, index, label=None):
    global zone
    # Bounded regional/material batches retain culling, unlike one city mesh.
    sector = 0 if x < -84 else 1 if x < 84 else 2
    row = 0 if z > -130 else 1 if z > -200 else 2
    zone = '5%d_Neighborhood_%d' % (1 + row, sector)
    h = floors*3.2 + .4; front = z+depth/2; paint = ['Plaster','WarmPaint','BluePaint'][index%3]
    name = 'District house %03d' % index
    box(name, (x,h/2,z), (w,h,depth), paint, True)
    houses.append(dict(name=name, position=xyz((x,0,z)), size=xyz((w,h,depth))))
    box('House plinth', (x,.3,front+.08), (w,.6,.16), 'Concrete')
    box('House roof parapet', (x,h+.25,z), (w+.25,.5,depth+.25), 'Plaster')
    box('Recessed shop glazing', (x,1.65,front+.09), (w-1.1,2.6,.14), 'Glass')
    for dx in [-w/2+.4, w/2-.4]:
        box('Shopfront pillar', (x+dx,1.7,front+.23), (.25,3.4,.4), 'Concrete')
    box('Shop sign fascia', (x,3.1,front+.26), (w-.5,.8,.22), 'BluePaint' if index%2 else 'LetterRed')
    awning = box('Corrugated shop awning', (x,2.6,front+1.3), (w+.2,.12,2.8), 'SilverCladding' if index%3 else 'Terracotta')
    awning.rotation_euler.x = .12
    for dx in [-w/2+.2,w/2-.2]:
        # Supports match the canopy and have collision, clear of the alley.
        box('Awning support', (x+dx,1.2,front+2.45), (.09,2.4,.09), 'Metal', True)
    for floor in range(1,floors):
        y = floor*3.2+1.6
        for dx in [-w*.23,w*.23]:
            box('Framed upper window', (x+dx,y,front+.1), (w*.31,1.9,.12), 'Glass')
            box('Window transom', (x+dx,y+.18,front+.19), (w*.31,.06,.07), 'Plaster')
            box('Window mullion', (x+dx,y,front+.2), (.06,1.9,.06), 'Plaster')
        if index%3 != 0:
            box('Balcony slab', (x,y-1.15,front+.7), (w-.4,.16,1.5), 'Concrete')
            box('Balcony top rail', (x,y-.18,front+1.4), (w-.5,.07,.07), 'Metal')
            for dx in [-.38*w,-.19*w,0,.19*w,.38*w]:
                box('Balcony rail', (x+dx,y-.65,front+1.4), (.045,.95,.045), 'Metal')
    if index%3 == 0:
        cylinder('Rooftop domestic water tank', (x+.7,h+1,z), .7,1.65,'SilverCladding')
        box('Tank support', (x+.7,h+.15,z), (1.6,.25,1.6), 'Metal')
    if index%4 == 0:
        box('Aircon condenser', (x+w*.33,3.95,front+.44), (.8,.5,.5), 'Plaster')
        box('Aircon fan grille', (x+w*.33,3.95,front+.7), (.62,.35,.025), 'Shadow')
    if index%5 == 0:
        box('Future rooftop solar collector', (x,h+.6,z-2), (w*.8,.12,3.2), 'Glass')
        box('Solar panel frame', (x,h+.5,z-2), (w*.8+.15,.1,3.35), 'Metal')
    if label: sign(label,(x,2.9,front+.39),w-.8)


# Deliberate gaps at all three gates become a walkable loop network. Narrow
# shophouse lots and varied heights recall the surroundings, without asserting
# exact survey/building identities or reproducing commercial logos.
index=0
for row,(z,depth) in enumerate([(-99,25),(-157,32),(-220,30),(-281,31)]):
    for a,b in [(-268,-90),(-64,-13),(13,64),(90,240)]:
        cursor=a
        while cursor < b-7:
            width=min(random.choice([7.5,9,10.5]),b-cursor)
            if width<7:break
            index+=1
            label=None
            if row==0 and index%4==0:
                label=['PHOTOCOPY','CƠM SINH VIÊN','SỬA XE','NHÀ SÁCH','CÀ PHÊ','TẠP HÓA'][index//4%6]
            house(cursor+width/2,z,width-.6,depth,random.choice([2,2,3,4]),index,label)
            cursor += width+1.3

# A small open food court replaces two rear lots, independent of interiors.
# Locate it beyond the west end of the continuous rows: through-path stays clear.
zone='55_NeighborhoodServices'
box('Food court paving', (-280,.044,-157),(22,.05,39),'RedPaver')
for z in [-145,-158,-171]:
    box('Food stall counter',(-285,.5,z),(3,1,2),'Plaster',True)
    box('Food stall canopy',(-285,2.9,z),(5,.16,4),'Terracotta')
    for dx in [-2,2]:box('Stall post',(-285+dx,1.45,z),( .1,2.9,.1),'Metal')
    for dz in [-1.5,1.5]:
        box('Outdoor food table',(-276,.75,z+dz),(2,.12,.8),'WarmPaint',True)
        for dd in [-.8,.8]:box('Outdoor stool',(-276,.24,z+dz+dd),(.45,.48,.45),'BluePaint',True)
sign('KHU ẨM THỰC',(-285,2.25,-142.85),4.5)

# Fictional technology is clearly secondary to the local architecture.
for x,z in [(-83,-118),(-6,-180),(83,-238)]:
    box('Neighborhood emergency cabinet',(x,1,z),(1.3,2,.7),'Metal',True)
    box('Cabinet status strip',(x,1.35,z+.37),(1,.08,.04),'Status')
    box('Emergency supply crate',(x+2,.5,z),(1.4,1,1),'BluePaint',True)
for x in [-86,9,86]:
    for z in [-117,-180,-240]:
        box('Neighborhood streetlight pole',(x,3.2,z),(.13,6.4,.13),'Metal',True)
        box('Neighborhood streetlight head',(x+.6,6.35,z),(1.5,.15,.55),'Plaster')
        box('Smart streetlight solar',(x,6.65,z),(.9,.08,1.2),'Glass')
for x in [-77,0,77]:
    box('Alley wayfinding pillar',(x+6.2,1.5,-81),(.22,3,.22),'Metal',True)
    box('Alley wayfinding panel',(x+6.2,2.5,-81),(3,1,.12),'BluePaint')
    sign('HẺM SINH VIÊN',(x+6.2,2.25,-80.92),2.8)

checks=[(x,0,z) for x in [-77,0,77] for z in [-79,-118,-127,-182,-190,-242,-250,-258]]
checks += [(-280,0,-138),(-280,0,-157),(-210,0,-127),(-145,0,-190),(160,0,-250)]
layout['neighborhoodChecks']=[xyz(p) for p in checks]
layout['neighborhoodHouses']=houses
layout['neighborhoodRoute']=[xyz(p) for p in [(0,0,-40),(0,0,-79),(0,0,-127),(-77,0,-127),(-77,0,-190),(77,0,-190),(77,0,-250),(0,0,-250),(0,0,-79)]]
layout['neighborhood']=dict(reference='Duong 3/2 and student alley / rental housing typology, inspired by Hem51 photographs; not a survey',
    districts=['Street frontage / shops','Student service alleys','Rental housing blocks','Open food court'],
    newHouseCount=len(houses), accessibleBuildingInteriors=False,
    provenance=['https://www.ctu.edu.vn/khu-2.html','https://plo.vn/can-tho-ket-xe-hem-51-nganh-chuc-nang-gan-nhu-bo-tay-post593267.html','https://luhanhvietnam.com.vn/du-lich/hem-51-can-tho-thien-duong-am-thuc.html'])
sys.path.insert(0,os.path.dirname(__file__))
from NeighborhoodFacadeDetails import enrich
def set_zone(name):
    global zone
    zone=name
enrich(layout,box,set_zone)
layout['neighborhoodFacadeDetails']=True
layout['neighborhoodRoofCorrected']=True
layout['artRevision']=6
layout['note']='Revision5 campus retained; playable reference-inspired student neighborhood added south of Duong3/2. Not a surveyed 1:1 reconstruction.'
with open(os.path.join(output,'CanThoCampus.layout.json'),'w',encoding='utf-8') as f:json.dump(layout,f,ensure_ascii=False,indent=2)
# Preserve packed images as well as their portable external counterparts.
bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(output,'CanThoCampus.blend'),relative_remap=False)
sys.path.insert(0,os.path.dirname(__file__))
from FinishCanThoCampus import finish
finish(output,False)
print('CAMPUS_NEIGHBORHOOD_REVISION6_PASS houses='+str(len(houses)),flush=True)
