"""Revision 4: recompose the editable campus around the user's Campus II diagram.

Uses original project meshes, keeps the tested Gate B mission corridor, and writes
NEW outputs only. Diagram pixels are traced in the 1910x1280 displayed reference;
the .32 m/pixel scale is a gameplay estimate, not a cadastral measurement.
"""
import bpy
import json
import math
import os
import random
import sys
from mathutils import Matrix, Vector

sys.dont_write_bytecode = True
args = sys.argv[sys.argv.index('--') + 1:]
source, output = map(os.path.abspath, args[:2])
if os.path.exists(output):
    raise RuntimeError('Choose a new output directory; source/output are never overwritten.')
with open(os.path.join(os.path.dirname(source), 'CanThoCampus.layout.json'), encoding='utf-8') as stream:
    layout = json.load(stream)
if layout.get('artRevision') != 3:
    raise RuntimeError('Expected the editable revision 3 campus and matching layout.')
bpy.ops.wm.open_mainfile(filepath=source)
scene = bpy.context.scene
random.seed(20481016)
original_collisions = layout['collisions']
original_landmarks = layout['landmarks']
collisions, landmarks, plans, road_segments, plots = [], [], [], [], []
cache = {}
zone = '00_Ground'


def cv(p): return Vector((-p[0], -p[2], p[1]))
def xyz(p): return dict(x=round(p[0], 5), y=round(p[1], 5), z=round(p[2], 5))
def uv(p): return (-p.x, p.z, -p.y)
def world(px, py): return ((px - 1044) * .32, -49 + (1010 - py) * .32)


def collection(name):
    result = bpy.data.collections.get(name)
    if result is None:
        result = bpy.data.collections.new(name)
        scene.collection.children.link(result)
    return result


def link(obj):
    collection(zone).objects.link(obj)
    return obj


def material(name, color, roughness=.8, metallic=0, emission=0):
    name = 'CT4_' + name
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1)
    if m.node_tree is None:
        m.use_nodes = True
    node = m.node_tree.nodes.get('Principled BSDF')
    for key, value in [('Base Color', (*color, 1)), ('Roughness', roughness),
                       ('Metallic', metallic), ('Emission Color', (*color, 1)),
                       ('Emission Strength', emission)]:
        node.inputs[key].default_value = value
    layout['materials'].append(dict(name=name, color=dict(r=color[0], g=color[1], b=color[2], a=1),
                                   roughness=roughness, metallic=metallic, emission=emission))
    return name


materials = {
    'Stone': material('Stone', (.49, .51, .46)),
    'Walk': material('Walk', (.50, .49, .42)),
    'Asphalt': material('Asphalt', (.105, .13, .145)),
    'Grass': material('Grass', (.26, .36, .17)),
    'Green': material('Green', (.19, .29, .13)),
    'Dorm': material('DormCourtyard', (.48, .48, .41)),
    'Court': material('Court', (.23, .37, .37)),
    'Research': material('ResearchPlot', (.43, .36, .22)),
    'Science': material('SciencePlot', (.32, .41, .44)),
    'Soil': material('Soil', (.22, .27, .19)),
    'White': 'CT2_White', 'Hull': 'CT2_Hull', 'Metal': 'CT2_Metal',
    'Cyan': 'CT2_Cyan', 'Glass': 'CT3_Glazing', 'Bark': 'CT2_Bark',
    'Leaf': 'CT3_LeafMid', 'LeafSun': 'CT3_LeafSun', 'Roof': 'CT2_Roof',
    'Yellow': 'CT2_Yellow', 'Ceramic': 'CT2_Ceramic', 'Frame': 'CT3_FrameBlue'
}


def collision(name, p, size, yaw=0):
    data = dict(name=name, position=xyz(p), size=xyz(size))
    if abs(yaw) > .00001:
        data['euler'] = xyz((0, yaw, 0))
    collisions.append(data)


def box(name, p, size, mat='Stone', solid=False, angle=0):
    key = ('box', materials[mat])
    if key not in cache:
        bpy.ops.mesh.primitive_cube_add(size=1)
        temp = bpy.context.object
        temp.data.materials.append(bpy.data.materials[materials[mat]])
        cache[key] = temp.data
        bpy.data.objects.remove(temp, do_unlink=True)
    obj = link(bpy.data.objects.new(name, cache[key]))
    obj.location = cv(p)
    obj.scale = (size[0], size[2], size[1])
    obj.rotation_euler.z = angle
    if solid:
        collision(name, p, size, -math.degrees(angle))
    return obj


def polygon(name, points, mat, y=.025):
    # Input follows Unity XZ. A single planar n-gon is triangulated by Blender.
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata([cv((x, y, z)) for x, z in points], [], [list(range(len(points)))])
    mesh.materials.append(bpy.data.materials[materials[mat]])
    mesh.update()
    if mesh.polygons[0].normal.z < 0:
        mesh.polygons[0].flip()
    return link(bpy.data.objects.new(name, mesh))


font = bpy.data.fonts.get('Arial Bold') or bpy.data.fonts.load('C:/Windows/Fonts/arialbd.ttf')


def sign(text, p, size=.65, width=24, mat='White', flat=False, inspection=False):
    curve = bpy.data.curves.new(text, 'FONT')
    curve.body = text
    curve.font = font
    curve.size = size
    curve.align_x = 'CENTER'
    curve.align_y = 'CENTER'
    curve.resolution_u = 2
    curve.extrude = 0 if flat else .005
    obj = link(bpy.data.objects.new(text, curve))
    obj.location = cv(p)
    obj.rotation_euler = (0, 0, math.pi) if flat else (math.pi / 2, 0, math.pi)
    curve.materials.append(bpy.data.materials[materials[mat]])
    bpy.context.view_layer.update()
    if obj.dimensions.x > width:
        obj.scale *= width / obj.dimensions.x
    if not inspection:
        bpy.ops.object.select_all(action='DESELECT')
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.convert(target='MESH')
    return obj


def clear(name):
    col = bpy.data.collections.get(name)
    if col:
        for obj in list(col.objects):
            bpy.data.objects.remove(obj, do_unlink=True)


# The image changes northern distances much more than the already-tested frontage.
# (old center X,Z, new center X,Z, footprint scale X,Z); height is preserved.
transforms = {
    '02_Administration': (32, -2, 22, -2, 1, 1),
    '03_LearningResource': (57, -25, 57, -25, 1, 1),
    '04_Hightech_ATL': (-33, -21, -33, -21, 1, 1),
    '06_Humanities': (136, -10, 122, -19, .65, .65),
    '07_ICT': (30, 101, 14, 74, .86, .88),
    '08_NaturalSciences': (97, 97, 78, 68, .64, .75),
    '09_Agriculture': (-78, 110, -68, 89, .86, .78),
    '10_Education': (-116, 43, -108, 36, .68, .8),
    '11_Classrooms_C': (-93, 49, -87, 40, .88, .8),
    '12_B1': (2, 151, -9, 120, 1, .82),
    '13_SportEducation': (39, 152, 22, 124, 1.05, .75),
    '14_Economics': (183, 110, 135, 91, .83, .68),
    '15_PoliticalScience': (170, 65, 124, 48, 1, .9),
    '16_Engineering': (11, 218, -9, 157, .8, .8),
    '17_Research_RLC': (-80, 205, -73, 144, .78, .78),
    '18_Biotech': (-139, 202, -125, 142, .65, .68),
    '19_Fisheries': (-83, 284, -73, 185, .84, .78),
    '20_Environment': (-246, 165, -217, 93, .75, .67),
    '21_MekongResearch': (-161, 201, -149, 139, 1.15, .88),
    '22_Dorm_B': (-190, 270, -153, 178, 1.12, .74),
    '23_Dorm_A': (183, 237, 135, 147, .88, .85),
    '24_TurtleAuditorium': (-45, 16, -45, 16, 1, 1),
}


def transform_point(p, t):
    x, y, z = p
    ox, oz, nx, nz, sx, sz = t
    return (nx + (x - ox) * sx, y, nz + (z - oz) * sz)


def transform_object(obj, t):
    ox, oz, nx, nz, sx, sz = t
    matrix = Matrix.Diagonal((sx, sz, 1, 1))
    matrix.translation = cv((nx - ox * sx, 0, nz - oz * sz))
    obj.matrix_world = matrix @ obj.matrix_world


for name, t in transforms.items():
    for obj in list(bpy.data.collections[name].objects):
        transform_object(obj, t)
    plans.append(dict(id=name, sourceCenter=xyz((t[0], 0, t[1])), center=xyz((t[2], 0, t[3])),
                      footprintScale=xyz((t[4], 1, t[5]))))

# A3 is two close parallel classroom bars, south of the banyan garden.
for obj in bpy.data.collections['05_Classrooms_A3'].objects:
    x, y, z = uv(obj.location)
    t = (96, -27, 96, -27, 1, .66) if z < -16 else (96, -5, 96, -14, 1, .66)
    transform_object(obj, t)

# Existing standalone retrofit housings travel with their nearest authored faculty.
for obj in bpy.data.collections['31_CampusRetrofits'].objects:
    x, y, z = uv(obj.location)
    nearest = min(original_landmarks, key=lambda lm: (lm['position']['x'] - x)**2 + (lm['position']['z'] - z)**2)
    if nearest['id'] in transforms:
        transform_object(obj, transforms[nearest['id']])
    elif nearest['id'] == '05_Classrooms_A3':
        t = (96, -27, 96, -27, 1, .66) if z < -16 else (96, -5, 96, -14, 1, .66)
        transform_object(obj, t)

primary = {lm['title']: lm['id'] for lm in original_landmarks}
central_names = ('Administration ', 'Library ', 'Campus route ', 'Cycle shelter ',
                 'Route bench', 'Route wayfinding post', 'MEKONG emergency power cabinet')
for entry in original_collisions:
    name = entry['name']
    p = tuple(entry['position'][a] for a in 'xyz')
    s = tuple(entry['size'][a] for a in 'xyz')
    if name in primary:
        key = primary[name]
        if key == '05_Classrooms_A3':
            t = (96, -27, 96, -27, 1, .66) if p[2] < -16 else (96, -5, 96, -14, 1, .66)
        else:
            t = transforms[key]
        collision(name, transform_point(p, t), (s[0] * t[4], s[1], s[2] * t[5]))
    elif name == 'Turtle auditorium body':
        collision(name, p, s)
    elif name.startswith('Administration '):
        collision(name, transform_point(p, transforms['02_Administration']), s)
    elif name.startswith(central_names) and -65 < p[0] < 82 and p[2] < 46:
        collision(name, p, s)
    elif name.startswith(('Gate ', 'White monument sign', 'Neighbourhood shophouse', 'Transit bench')):
        collision(name, p, s)
    elif name in ('Campus soil', 'West retaining wall', 'East retaining wall', 'North retaining wall', 'South retaining wall'):
        collision(name, p, s)

for lm in original_landmarks:
    key = lm['id']
    p = tuple(lm['position'][a] for a in 'xyz')
    a = tuple(lm['approach'][v] for v in 'xyz')
    if key == '05_Classrooms_A3':
        t = (96, -27, 96, -27, 1, .66) if p[2] < -16 else (96, -5, 96, -14, 1, .66)
    else:
        t = transforms[key]
    landmarks.append(dict(id=key + ('_' + str(len(landmarks)) if key in ('05_Classrooms_A3', '22_Dorm_B', '23_Dorm_A') else ''),
                          title=lm['title'], position=xyz(transform_point(p, t)), approach=xyz(transform_point(a, t))))

# Regenerate obsolete roads, vegetation, sport facilities and perimeter as one revision.
for name in ['00_Ground', '25_Sports', '27_Landscape', '28_MekongSciFi',
             '32_MekongInfrastructure', '34_StreetDetail', '35_CampusFence']:
    clear(name)
# Keep only detailed foreground route props that still have matching collision.
for obj in list(bpy.data.collections['36_CampusRoute'].objects):
    x, y, z = uv(obj.location)
    if x >= 82 or x <= -65 or z >= 46:
        bpy.data.objects.remove(obj, do_unlink=True)

boundary = [(-90, -49.5), (124, -49.5), (129, -15), (128, -3), (139, 21),
            (159, 15), (180, 163), (181, 179), (155, 239), (129, 250),
            (109, 239), (103, 192), (90, 184), (53, 191), (34, 191),
            (16, 204), (-4, 199), (-47, 212), (-190, 206), (-229, 130),
            (-241, 78), (-182, 52), (-157, 52), (-162, 7), (-90, 11), (-83, 3)]
zone = '00_Ground'
box('Regional ground', (-15, -.25, 110), (560, .5, 430), 'Soil')
polygon('Campus II footprint', boundary, 'Walk', .012)


def plot(name, pixels, mat='Grass'):
    points = [world(*p) for p in pixels]
    polygon(name, points, mat, .025 + len(plots) * .00005)
    plots.append(dict(id=name, points=[xyz((x, 0, z)) for x, z in points]))


plot('KTX B courtyard', [(420,345),(507,262),(703,257),(696,386),(635,386),(632,356),(489,354)], 'Dorm')
plot('Fisheries plot', [(717,250),(891,235),(910,343),(714,349)])
plot('Research innovation plot', [(713,367),(910,362),(932,461),(709,462)], 'Research')
plot('Engineering plot', [(971,322),(1050,308),(1076,329),(1089,414),(1045,440),(986,439),(963,414)])
plot('KTX A courtyard', [(1335,326),(1548,317),(1563,352),(1535,369),(1550,472),(1395,491)], 'Dorm')
plot('Agriculture plot', [(719,518),(936,514),(958,628),(695,662)], 'Green')
plot('Greenhouse plot', [(584,540),(697,540),(683,655),(566,674)], 'Research')
plot('Natural sciences plot', [(1190,541),(1327,532),(1381,750),(1278,757),(1216,702)], 'Science')
plot('Economics plot', [(1365,569),(1423,537),(1460,548),(1474,535),(1515,554),(1560,547),(1557,591),(1496,645),(1440,668),(1381,631)], 'Green')
plot('LRC lawn', [(1157,795),(1223,792),(1243,877),(1167,877)], 'Grass')
plot('Banyan garden', [(1280,794),(1390,791),(1412,874),(1297,884)], 'Green')
plot('North reserve', [(1411,123),(1447,104),(1517,133),(1577,305),(1387,295),(1390,260)], 'Green')
plot('Western park', [(370,485),(548,485),(535,672),(480,683),(320,606)])


def road(name, pixels, width=8.5, stripes=False):
    for index, (aa, bb) in enumerate(zip(pixels, pixels[1:])):
        a, b = world(*aa), world(*bb)
        dx, dz = b[0] - a[0], b[1] - a[1]
        length = math.hypot(dx, dz)
        angle = math.atan2(dz, dx)
        center = ((a[0] + b[0]) / 2, (a[1] + b[1]) / 2)
        box(name + ' sidewalk', (center[0], .04, center[1]), (length + .6, .035, width + 4), 'Walk', angle=angle)
        box(name + ' carriageway', (center[0], .088 + len(road_segments) * .00005, center[1]),
            (length + .5, .04, width), 'Asphalt', angle=angle)
        road_segments.append(dict(name=name, a=xyz((a[0], 0, a[1])), b=xyz((b[0], 0, b[1])), width=width))
        if stripes:
            for d in range(7, max(7, int(length) - 6), 9):
                t = d / length
                box('Lane guidance', (a[0] + dx*t, .116, a[1] + dz*t), (3.1, .009, .12), 'Yellow', angle=angle)


road('Duong 3 thang 2', [(190,1057),(1820,1057)], 20, True)
road('Gate B spine', [(1044,1010),(1010,820),(995,775),(963,545),(940,470),(899,225)], 10, True)
road('Gate C spine', [(803,1010),(815,847),(815,670)], 8.5, True)
road('Gate A spine', [(1285,1010),(1272,936),(1245,772),(1204,701),(1179,526)], 9, True)
road('Southern cross campus', [(995,775),(1245,772),(1395,765)], 9)
road('East faculty loop', [(1275,970),(1445,947),(1395,765),(1340,510)], 8)
road('Sports cross campus', [(957,545),(1179,526),(1340,510)], 9)
road('Western cross campus', [(310,606),(475,696),(963,645)], 9)
road('Education crescent', [(995,726),(883,750),(751,807),(687,812),(627,770)], 7)
road('Western research axis', [(550,693),(589,358)], 8)
road('Western north cross', [(332,470),(940,470)], 8.5)
road('Environment access', [(310,606),(332,470),(425,354),(589,354),(700,354)], 7)
road('Engineering crescent', [(940,470),(1098,416),(1082,310),(949,234),(899,225)], 8)
road('Dorm A approach', [(1082,310),(1320,310),(1550,310),(1572,353),(1539,369),(1558,478),(1340,510)], 8)

# Small roundabouts are ground-level rings with low, traversable planted islands.
def roundabout(name, x, z, radius=6.2):
    bpy.ops.mesh.primitive_cylinder_add(vertices=48, radius=radius+2.5, depth=.04, location=cv((x,.119,z)))
    obj = bpy.context.object
    for c in list(obj.users_collection): c.objects.unlink(obj)
    link(obj)
    obj.name = name + ' asphalt ring'
    obj.data.materials.append(bpy.data.materials[materials['Asphalt']])
    bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=radius-1.8, depth=.12, location=cv((x,.13,z)))
    obj = bpy.context.object
    for c in list(obj.users_collection): c.objects.unlink(obj)
    link(obj)
    obj.name = name + ' low planted island'
    obj.data.materials.append(bpy.data.materials[materials['Grass']])


roundabout('Turtle Hall roundabout', -22, 22, 6.2)
roundabout('Science roundabout', *world(1245,772), 4.7)

# Supplemental landmarks explicitly named in the new reference.
def annex(key, title, x, z, w, d, h=7, facade='White'):
    global zone
    zone = key
    box(title, (x,h/2,z), (w,h,d), facade, True)
    box(title + ' flat roof', (x,h+.22,z), (w+1,.45,d+1), 'Ceramic')
    for y in [1.8, 4.9]:
        if y > h-1: continue
        box('Window ribbon', (x,y,z-d/2-.07), (w-1,1.7,.12), 'Glass')
        for xx in range(-int(w/2)+2,int(w/2),3):
            box('Vertical sun fin', (x+xx,y,z-d/2-.24), (.12,2,.45), 'Frame')
    box('Retrofit roof solar array', (x,h+.55,z), (w*.65,.16,d*.58), 'Glass')
    box('MEKONG service light', (x,h-.4,z-d/2-.1), (w-.5,.055,.05), 'Cyan')
    sign(title, (x,h-1,z-d/2-.28), .62, w-1)
    landmarks.append(dict(id=key, title=title, position=xyz((x,0,z)), approach=xyz((x,0,z-d/2-4))))


annex('37_Preparatory', 'KHOA DỰ BỊ DÂN TỘC', -135, 40, 27, 12, 6.4)
annex('38_PracticeSchool', 'THPT THỰC HÀNH SƯ PHẠM', -131, 13, 21, 10, 6.4)
annex('39_Law', 'KHOA LUẬT', -62, -5, 14, 10, 9.6)
annex('40_Languages', 'TRUNG TÂM NGOẠI NGỮ', -65, -22, 10, 12, 6.4)
annex('41_ClassroomC1', 'NHÀ HỌC C1', 26, 37, 37, 8, 9.6)
zone = '41_ClassroomC1'
box('C1 east return wing', (43,4.8,42), (6,9.6,13), 'White', True)
annex('42_YouthOffice', 'VĂN PHÒNG ĐOÀN TRƯỜNG', 15, 51, 14, 7, 3.5)
annex('43_ClassroomD2', 'NHÀ HỌC D2', 22, 16, 24, 5, 6.4)
zone = '02_Administration'
sign('D1 / KHOA SAU ĐẠI HỌC', (22,2.3,-18.48), .42, 20, 'Frame')
landmarks.append(dict(id='44_D1Postgraduate', title='D1 / KHOA SAU ĐẠI HỌC',
                      position=xyz((22,0,-16)), approach=xyz((22,0,-21))))

# Greenhouse roofs and shaded rows, rather than another generic faculty box.
zone = '45_Greenhouses'
for row in range(4):
    x, z = -140 + row*8.1, 82
    box('Plant nursery greenhouse', (x,1.5,z), (6.5,3,31), 'Glass', True)
    box('Greenhouse roof shading', (x,3.05,z), (6.9,.2,31.4), 'Ceramic')
    for zz in range(-14,15,4):
        box('Nursery shade screen', (x,3.25,z+zz), (6.8,.1,1.3), 'Grass')
    for side in [-1,1]:
        box('Nursery planted bed', (x+side*1.6,.45,z), (1.6,.6,28), 'Green')
landmarks.append(dict(id=zone, title='KHU NHÀ LƯỚI', position=xyz((-128,0,82)), approach=xyz((-128,0,61))))

zone = '25_Sports'
box('Gym', (70,6,130), (46,12,24), 'White', True)
for xx in range(-21,22,5):
    box('Sports hall glazing', (70+xx,4.7,117.85), (3.8,6,.12), 'Glass')
    box('Sports hall rib', (70+xx,6,117.6), (.35,12,.6), 'Frame')
box('Sports hall floating roof', (70,12.3,130), (48,.6,26), 'Ceramic')
box('Sports hall rooftop solar field', (70,12.7,130), (40,.2,19), 'Glass')
sign('NHÀ THI ĐẤU MỚI', (70,10,117.45), 1, 42, 'Frame')
landmarks.append(dict(id='25_Sports', title='NHÀ THI ĐẤU MỚI', position=xyz((70,0,130)), approach=xyz((70,0,112))))
for lm in landmarks:
    if lm['id'] == '13_SportEducation': lm['title'] = 'NHÀ THI ĐẤU CŨ / GDTC'
zone = '13_SportEducation'
sign('NHÀ THI ĐẤU CŨ', (22,6.8,114.8), .6, 24, 'White')

# New linking wings give dormitory clusters a legible campus/courtyard shape.
zone = '22_Dorm_B'
box('Dorm B western return', (-185,8,180), (9,16,39), 'White', True)
zone = '23_Dorm_A'
box('Dorm A eastern return', (165,8,147), (8,16,47), 'White', True)

# A compact service node remains off the tested Library route.
zone = '32_MekongInfrastructure'
box('Mekong tower foundation', (75,.4,-25), (3,.8,3), 'Hull', True)
box('Mekong data mast', (75,8,-25), (1.4,16,1.4), 'Frame', True)
for y in range(2,16,3):
    box('Data mast energy seam', (75,y,-25.73), (.65,1.2,.05), 'Cyan')

# Fence follows the traced irregular perimeter. Oriented collision boxes must use
# exactly the same angles as the render geometry; broad AABBs would block roads.
zone = '35_CampusFence'
fence_spans = []


def fence(name, a, b):
    dx, dz = b[0]-a[0], b[1]-a[1]
    length = math.hypot(dx,dz)
    angle = math.atan2(dz,dx)
    ux, uz = dx/length, dz/length
    def part(label, distance, y, size, mat):
        return box(name+' '+label, (a[0]+ux*distance,y,a[1]+uz*distance), size, mat, angle=angle)
    part('concrete plinth', length/2,.33,(length,.66,.55),'Stone')
    part('cap coping', length/2,.73,(length,.12,.65),'Ceramic')
    for yy in [1.05,2.96,3.18]:
        part('horizontal rail', length/2,yy,(length,.12,.13),'Metal')
    part('luminous seam', length/2,3.27,(length,.028,.1),'Cyan')
    panels = max(1,math.ceil(length/4))
    for i in range(panels+1):
        part('structural post',i*length/panels,1.75,(.32,3.5,.34),'Hull')
    for i in range(panels):
        for j in range(1,6):
            part('vertical bar',(i+j/6)*length/panels,1.96,(.075,2.22,.075),'Metal')
    collision('Campus fence '+name, ((a[0]+b[0])/2,1.8,(a[1]+b[1])/2), (length+.3,3.6,.68), -math.degrees(angle))
    fence_spans.append(dict(name=name,a=xyz((a[0],0,a[1])),b=xyz((b[0],0,b[1]))))


for i,(a,b) in enumerate(zip(boundary,boundary[1:]+boundary[:1])):
    if i == 0:
        # Gate pylon outer edges are +/-9.6m; openings remain >=16m wide.
        for index,(lo,hi) in enumerate([(-90,-86.6),(-67.4,-9.6),(9.6,67.4),(86.6,124)]):
            fence('Front '+str(index+1),(lo,-49.5),(hi,-49.5))
    else:
        fence('Boundary '+str(i),a,b)


def inside(x,z, poly):
    odd=False
    for (ax,az),(bx,bz) in zip(poly,poly[1:]+poly[:1]):
        if (az>z)!=(bz>z) and x<(bx-ax)*(z-az)/(bz-az)+ax:
            odd=not odd
    return odd


def segment_distance(x,z,a,b):
    dx,dz=b['x']-a['x'],b['z']-a['z']
    t=max(0,min(1,((x-a['x'])*dx+(z-a['z'])*dz)/(dx*dx+dz*dz)))
    return math.hypot(x-a['x']-t*dx,z-a['z']-t*dz)


def safe_tree(x,z):
    if not inside(x,z,boundary): return False
    # Preserve combat strip and the mission corridor, including the rendezvous discs.
    if -13<x<11 and -44<z<29: return False
    for p in layout['playableRoute']:
        if math.hypot(x-p['x'],z-p['z'])<3.5: return False
    for road in road_segments:
        if segment_distance(x,z,road['a'],road['b'])<road['width']/2+2.5: return False
    for col in collisions:
        if col['name']=='Campus soil' or col.get('euler'): continue
        p,s=col['position'],col['size']
        if abs(x-p['x'])<s['x']/2+1.7 and abs(z-p['z'])<s['z']/2+1.7: return False
    return True


zone = '27_Landscape'


def tree(x,z,h=8,r=3.4):
    box('Shade tree trunk',(x,h*.43,z),(.42,h*.86,.42),'Bark',True)
    for index in range(4):
        mat='Leaf' if index%2 else 'LeafSun'
        key=('ico',materials[mat])
        if key not in cache:
            bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=1)
            temp=bpy.context.object
            temp.data.materials.append(bpy.data.materials[materials[mat]])
            for p in temp.data.polygons: p.use_smooth=True
            cache[key]=temp.data
            bpy.data.objects.remove(temp,do_unlink=True)
        obj=link(bpy.data.objects.new('Broadleaf canopy',cache[key]))
        angle=index*math.tau/4
        obj.location=cv((x+math.cos(angle)*r*.35,h+(.5 if index%2 else 0),z+math.sin(angle)*r*.35))
        obj.scale=(r*.8,r*.75,r*.55)


# The garden and northern reserve are deliberate clusters; other trees are sparse.
for x,z in [(91,8),(101,7),(91,18),(102,19),(60,8),(64,19),
            (126,213),(141,216),(123,229),(136,234),(148,226),
            (-212,112),(-200,108),(-207,124),(-188,93),(-175,95)]:
    if safe_tree(x,z): tree(x,z,9.5,4)
added=0
for attempt in range(1100):
    if added>=95: break
    x,z=random.uniform(-230,174),random.uniform(-8,235)
    if safe_tree(x,z):
        tree(x,z,random.uniform(7,10),random.uniform(2.6,3.7))
        added+=1

# Recognizable lawns/garden and direction signs; future detail is a restrained retrofit.
zone = '46_Wayfinding'
for text,x,z in [('CỔNG B / TRUNG TÂM',-7,-32),('KHOA THỦY SẢN  /  KTX B',-39,158),
                 ('THỂ THAO  /  KTX A',43,111),('NHÀ LƯỚI  /  NÔNG NGHIỆP',-117,58),
                 ('VƯỜN BẰNG',95,29),('TRUNG TÂM HỌC LIỆU',62,28)]:
    box('Wayfinding post',(x,1.3,z),(.22,2.6,.22),'Frame',True)
    box('Wayfinding blade',(x,2.35,z),(6,.7,.16),'Frame')
    sign(text,(x,2.35,z-.1),.34,5.7)

landmarks.extend([
    dict(id='47_LRCLawn',title='BÃI CỎ HỌC LIỆU',position=xyz((63,0,14)),approach=xyz((65,0,28))),
    dict(id='48_BanyanGarden',title='VƯỜN BẰNG',position=xyz((96,0,8)),approach=xyz((96,0,28)))
])

# Inspection labels remain CURVES and are excluded from the FBX mesh export.
# They are hidden for normal source/beauty views and shown only for the plan render.
zone = '90_InspectionLabels'
labels=[('CỔNG C',-77,-57),('CỔNG B',0,-57),('CỔNG A',77,-57),
        ('ĐƯỜNG 3/2',-130,-67),('KTX B',-153,183),('THỦY SẢN',-73,188),
        ('RLC / NGHIÊN CỨU',-73,144),('BÁCH KHOA',-9,158),('KTX A',135,148),
        ('B1',-9,121),('THI ĐẤU CŨ',22,124),('THI ĐẤU MỚI',70,130),
        ('MÔI TRƯỜNG',-217,94),('VIỆN ĐBSCL',-149,139),('NHÀ LƯỚI',-128,82),
        ('NÔNG NGHIỆP',-68,89),('CNTT / TT',14,75),('KHTN',78,70),('KINH TẾ',135,92),
        ('CHÍNH TRỊ',124,48),('DỰ BỊ',-135,41),('SƯ PHẠM',-108,36),('C2',-87,40),
        ('THPT THỰC HÀNH',-131,13),('HỘI TRƯỜNG RÙA',-45,17),('C1',26,37),
        ('ĐOÀN TRƯỜNG',15,51),('NHÀ ĐIỀU HÀNH',22,-2),('D2',22,16),
        ('HỌC LIỆU',57,-25),('VƯỜN BẰNG',96,8),('A3',96,-21),('KHXH / NV',122,-19),
        ('KHOA LUẬT',-62,-5),('NGOẠI NGỮ',-65,-22),('ATL',-33,-21)]
for text,x,z in labels:
    sign(text,(x,42,z),2.25,35,'White',flat=True,inspection=True)
collection(zone).hide_render=True
collection(zone).hide_viewport=True

layout.update(artRevision=4,campusFenceRevision=2,
              note='Recomposed from user Campus II schematic, 16 October 2026. Relative diagram layout; estimated game scale, original future-retrofit architecture.',
              collisions=collisions,landmarks=landmarks,
              campusBoundary=[xyz((x,0,z)) for x,z in boundary],
              fenceSpans=fence_spans,roads=road_segments,schematicPlots=plots,
              layoutTransforms=plans,schematicRevision=1,
              reference=dict(file='CampusII-UserReference.png',displayWidth=1910,displayHeight=1280,
                             metresPerPixel=.32,originPixel=dict(x=1044,y=1010),gateBWorld=xyz((0,0,-49)),
                             note='Relative illustrative layout, not surveyed coordinates.'),
              gates=[dict(id=key,position=xyz((x,0,-49)),clearWidth=16) for key,x in [('C',-77),('B',0),('A',77)]])
# Each approach is checked against the new collision/nav mesh in Unity, not assumed safe.
checks=[xyz((0,0,15)),xyz((-77,0,-54)),xyz((0,0,-54)),xyz((77,0,-54))]
checks.extend(layout['playableRoute'])
checks.extend(lm['approach'] for lm in landmarks)
layout['navigationChecks']=checks

os.makedirs(output)
with open(os.path.join(output,'CanThoCampus.layout.json'),'w',encoding='utf-8') as stream:
    json.dump(layout,stream,ensure_ascii=False,indent=2)

# Source cameras remain editable. Plan view is north-up with street at the bottom.
def camera(name,p,target,ortho=0):
    obj=bpy.data.objects.get(name)
    if obj is None:
        data=bpy.data.cameras.new(name)
        obj=bpy.data.objects.new(name,data)
        scene.collection.objects.link(obj)
    obj.location=cv(p)
    obj.rotation_euler=(cv(target)-obj.location).to_track_quat('-Z','Y').to_euler()
    obj.data.type='ORTHO' if ortho else 'PERSP'
    if ortho: obj.data.ortho_scale=ortho
    return obj


camera('Campus Overview',(320,350,-300),(-30,0,95),500)
plan=camera('Campus Schematic',(-30,500,95),(-30,0,95),485)
plan.rotation_euler=(0,0,math.pi)
scene.camera=bpy.data.objects['Campus Overview']
scene.render.engine='CYCLES' if '--cycles' in args else 'BLENDER_EEVEE'
scene.render.resolution_x=1800
scene.render.resolution_y=1400
scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.render.film_transparent=False
bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(output,'CanThoCampus.blend'))
sys.path.insert(0,os.path.dirname(__file__))
from FinishCanThoCampus import finish
finish(output,False)
if '--render' in args:
    for name in ['Campus Schematic','Campus Overview','Gate B Arrival']:
        collection('90_InspectionLabels').hide_render=name=='Gate B Arrival'
        scene.camera=bpy.data.objects[name]
        scene.render.filepath=os.path.join(output,name.replace(' ','-')+'.png')
        bpy.ops.render.render(write_still=True)
print('CAMPUS_SCHEMATIC_REVISION4_READY '+output,flush=True)
