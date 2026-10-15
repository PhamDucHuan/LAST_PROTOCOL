"""LAST_PROTOCOL original environment art, based on the user's Campus II diagram.
Coordinates below are Unity metres; the conversion was verified by the bridge probe.
Run in a NEW background Blender process. Existing .blend files are never loaded.
"""
import bpy
import math
import random
import json
import os
import sys
from mathutils import Vector

random.seed(1966)
argv = sys.argv[sys.argv.index('--') + 1:]
OUT = os.path.abspath(argv[0])
RENDER = '--render' in argv
os.makedirs(OUT, exist_ok=True)
if os.path.exists(os.path.join(OUT, 'CanThoCampus.blend')):
    raise RuntimeError('Use a new output directory; existing Blender source is preserved.')
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
collisions, landmarks, routes = [], [], []
collections, mesh_cache, materials, palette = {}, {}, {}, []
zone = '00_Ground'
road_index = 0

def cv(p):
    return (-p[0], -p[2], p[1])

def mat(name, rgb, roughness=0.8, metallic=0, emission=0):
    m = bpy.data.materials.new('CT_' + name)
    m.diffuse_color = (*rgb, 1)
    if m.node_tree is None: m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*rgb, 1)
    bsdf.inputs['Roughness'].default_value = roughness
    bsdf.inputs['Metallic'].default_value = metallic
    if emission:
        bsdf.inputs['Emission Color'].default_value = (*rgb, 1)
        bsdf.inputs['Emission Strength'].default_value = emission
    materials[name] = m
    palette.append(dict(name=m.name, color=dict(r=rgb[0],g=rgb[1],b=rgb[2],a=1), roughness=roughness, metallic=metallic, emission=emission))

for args in [
    ('Ivory',(0.84,0.8,0.65)), ('White',(0.92,0.93,0.84)), ('Concrete',(0.48,0.52,0.5)),
    ('Roof',(0.55,0.19,0.1)), ('RoofLight',(0.72,0.29,0.16)), ('Glass',(0.09,0.29,0.33),0.23,0.35),
    ('Teal',(0.04,0.29,0.32)), ('Asphalt',(0.13,0.17,0.19)), ('Paving',(0.69,0.66,0.55)),
    ('Grass',(0.32,0.43,0.22)), ('GrassLight',(0.42,0.51,0.28)), ('Leaf',(0.15,0.32,0.18)),
    ('LeafLight',(0.28,0.46,0.22)), ('Pink',(0.77,0.38,0.45)), ('Bark',(0.32,0.23,0.14)),
    ('Yellow',(0.98,0.69,0.16)), ('Metal',(0.16,0.22,0.24),0.55,0.55),
    ('Cyan',(0.06,0.8,0.83),0.35,0.1,1.8), ('Orange',(0.92,0.31,0.08)),
    ('Sand',(0.68,0.58,0.38)), ('Water',(0.13,0.38,0.39),0.2,0.3)]:
    mat(*args)

def link(obj):
    if zone not in collections:
        col = bpy.data.collections.new(zone)
        scene.collection.children.link(col)
        collections[zone] = col
    for col in list(obj.users_collection): col.objects.unlink(obj)
    collections[zone].objects.link(obj)
    return obj

def collision(name, p, s):
    collisions.append(dict(name=name, position=dict(x=p[0],y=p[1],z=p[2]), size=dict(x=s[0],y=s[1],z=s[2])))

def box(name, p, s, material='Concrete', solid=False):
    if material not in mesh_cache:
        bpy.ops.mesh.primitive_cube_add(size=1)
        temp = bpy.context.object
        temp.data.materials.append(materials[material])
        mesh_cache[material] = temp.data
        bpy.data.objects.remove(temp, do_unlink=True)
    obj = bpy.data.objects.new(name, mesh_cache[material])
    obj.location = cv(p)
    obj.scale = (s[0],s[2],s[1])
    link(obj)
    if solid: collision(name,p,s)
    return obj

def mesh(name, points, faces, material):
    data = bpy.data.meshes.new(name)
    # Roof face lists use Unity clockwise-facing order. The handedness conversion
    # already gives Blender its outward counter-clockwise order.
    data.from_pydata([cv(p) for p in points], [], faces)
    data.materials.append(materials[material]); data.update()
    return link(bpy.data.objects.new(name, data))

def cylinder(name,p,r,h,material='Concrete',vertices=16):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=r, depth=h, location=cv(p))
    obj = link(bpy.context.object); obj.name=name; obj.data.materials.append(materials[material]); return obj

fontpath = 'C:/Windows/Fonts/arialbd.ttf'
font = bpy.data.fonts.load(fontpath)
def sign(text,p,width,size=0.8,material='White'):
    data = bpy.data.curves.new('Sign '+text, 'FONT')
    data.body=text; data.font=font; data.align_x='CENTER'; data.size=size; data.extrude=0.006; data.resolution_u=2
    obj=link(bpy.data.objects.new('Sign '+text,data)); obj.location=cv(p)
    obj.rotation_euler=(math.pi/2,0,math.pi)
    obj.data.materials.append(materials[material]); bpy.context.view_layer.update()
    if obj.dimensions.x > width: obj.scale *= width/obj.dimensions.x
    bpy.ops.object.select_all(action='DESELECT'); obj.select_set(True); bpy.context.view_layer.objects.active=obj
    bpy.ops.object.convert(target='MESH')
    return obj

def road(name,a,b,width=9,main=False):
    global road_index
    road_index += 1
    dx,dz=b[0]-a[0],b[1]-a[1]; length=math.hypot(dx,dz)
    cx,cz=(a[0]+b[0])/2,(a[1]+b[1])/2
    angle=math.atan2(dx,dz)
    for suffix,y,w,depth,m in [('walk',0.025,width+4,0.05,'Paving'),('road',0.065,width,0.07,'Asphalt')]:
        obj=box(name+' '+suffix,(cx,y + road_index * (0.002 if suffix == 'road' else 0.0008),cz),(w,depth,length),m); obj.rotation_euler.z=-angle
    if main:
        for distance in range(3,int(length)-2,9):
            t=distance/length
            ob=box('Lane divider',(a[0]+dx*t,0.109+road_index*0.002,a[1]+dz*t),(0.14,0.012,3.3),'Yellow'); ob.rotation_euler.z=-angle

def roof(name,x,z,w,d,y):
    points=[(x-w/2,y,z-d/2),(x+w/2,y,z-d/2),(x+w/2,y,z+d/2),(x-w/2,y,z+d/2),(x-w/2,y+3,z),(x+w/2,y+3,z)]
    mesh(name,points,[(0,1,5,4),(3,4,5,2),(0,4,3),(1,2,5),(0,3,2,1)],'Roof')
    box('Roof ridge',(x,y+3.02,z),(w+0.2,0.12,0.2),'RoofLight')

def building(key,title,x,z,w,d,floors=3,modern=False):
    global zone
    zone=key
    height=floors*3.2
    box('Foundation',(x,0.18,z),(w+2,0.36,d+2),'Paving')
    box(title,(x,height/2,z),(w,height,d),'Ivory',True)
    # Deep eaves, floor bands, repeated window bays and shaded ground-level arcade.
    for floor in range(floors):
        y=floor*3.2+1.8
        for xx in range(2,int(w)-1,3):
            px=x-w/2+xx
            box('South window',(px,y,z-d/2-0.055),(1.7,1.5,0.11),'Glass')
            box('North window',(px,y,z+d/2+0.055),(1.7,1.5,0.11),'Glass')
            if floor==0:
                box('Arcade pillar',(px,1.5,z-d/2-2),(0.35,3,0.35),'White')
        for zz in range(2,int(d)-1,4):
            for side in [-1,1]: box('Side window',(x+side*(w/2+0.055),y,z-d/2+zz),(0.11,1.5,2.2),'Glass')
        box('Floor sunshade',(x,(floor+1)*3.2,z),(w+1.1,0.22,d+1),'White')
    box('Entrance canopy',(x,3.05,z-d/2-1.6),(w+1,0.3,4.5),'White')
    box('Institution sign',(x,3.6,z-d/2-2.9),(min(w,35),1.3,0.25),'Teal')
    sign(title,(x,3.28,z-d/2-3.045),min(w-1,33),0.95)
    if modern:
        box('Flat roof parapet',(x,height+0.35,z),(w+1.1,0.7,d+1),'White')
        for xx in range(2,int(w)-1,5): box('Roof solar array',(x-w/2+xx,height+0.85,z),(3.2,0.18,d*0.55),'Glass')
        for xx in [-1,1]: box('Teal facade blade',(x+xx*(w/2-0.6),height/2,z-d/2-0.35),(0.5,height,0.5),'Teal')
    else: roof('Terracotta roof',x,z,w+2.8,d+2.5,height+0.15)
    landmarks.append(dict(id=key,title=title,position=dict(x=x,y=0,z=z),approach=dict(x=x,y=0,z=z-d/2-6)))

def tree(x,z,palm=False,pink=False):
    h=random.uniform(5.5,8)
    cylinder('Palm trunk' if palm else 'Tree trunk',(x,h/2,z),0.22 if palm else 0.32,h,'Bark',7)
    collision('Tree trunk',(x,h/2,z),(0.65,h,0.65))
    if palm:
        for a in range(7):
            t=a*math.tau/7
            points=[(x,h+0.5,z),(x+math.cos(t-0.12)*2.3,h+0.6,z+math.sin(t-0.12)*2.3),
                    (x+math.cos(t)*4,h-0.3,z+math.sin(t)*4),(x+math.cos(t+0.12)*2.3,h+0.6,z+math.sin(t+0.12)*2.3)]
            ob=mesh('Palm frond',points,[(0,1,2),(0,2,3),(2,1,0),(3,2,0)],'LeafLight' if a%2 else 'Leaf')
    else:
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=1,location=cv((x,h,z)))
        ob=link(bpy.context.object); ob.name='Pink trumpet canopy' if pink else 'Shade tree canopy'
        ob.scale=(3.5,3.1,3.2); ob.data.materials.append(materials['Pink' if pink else ('Leaf' if random.random()<0.5 else 'LeafLight')])

def bench(x,z):
    box('Bench seat',(x,0.55,z),(2.2,0.18,0.65),'Bark',True)
    box('Bench back',(x,0.95,z+0.3),(2.2,0.7,0.12),'Bark')
    for xx in [-0.75,0.75]: box('Bench foot',(x+xx,0.25,z),(0.18,0.5,0.55),'Concrete')

def gate(letter,x):
    global zone
    zone='01_Gates'
    for side in [-1,1]:
        box('Gate '+letter+' pylon',(x+side*8.8,3.3,-49),(1.6,6.6,2),'Ivory',True)
        box('Gate cap',(x+side*8.8,6.8,-49),(2.1,0.4,2.5),'Roof')
        box('Gate guardhouse',(x+side*14,1.9,-46),(5,3.8,5),'Ivory',True)
        roof('Guardhouse roof',x+side*14,-46,6.5,6.5,3.8)
        box('Gate glass',(x+side*14,2,-48.55),(3.5,1.2,0.12),'Glass')
    box('Gate lintel',(x,5.9,-49),(17.6,1.6,1.2),'Teal',True)
    sign('ĐẠI HỌC CẦN THƠ',(x,5.75,-49.66),15,1.0)
    sign('KHU II  •  CỔNG '+letter,(x,5.05,-49.66),12,0.45)
    for zz in range(-63,-54,2): box('Crosswalk',(x,0.119,zz),(12,0.016,0.8),'White')

zone='00_Ground'
box('Campus soil',(-15,-0.25,110),(560,0.5,430),'Grass',True)
box('South neighbourhood verge',(-15,0.015,-89),(555,0.03,29),'Sand')
road('Duong 3 thang 2',(-292,-64),(262,-64),16,True)
for x in [-77,0,77]: road('Gate approach',(x,-55),(x,52),10,True)
road('Main campus spine',(0,48),(-24,312),11,True)
for z,x1,x2 in [(50,0,225),(65,-175,0),(145,-175,240),(165,-230,225),(312,-230,235)]: road('Campus connector',(x1,z),(x2,z),8)
for x in [130,225]: road('Campus loop',(x,50),(x,312),8)
road('West academic loop',(-175,65),(-175,165),8)
road('West dorm loop',(-230,165),(-230,312),8)
road('West green loop',(-276,120),(-276,215),7)
road('West diagonal',(-276,120),(-175,65),7)
road('Dorm access',(-276,215),(-230,215),7)
# Broad entry/teammate recovery area; floor is nearly flush with the roads.
box('Central arrival plaza',(0,0.035,12),(32,0.07,62),'Paving')
for x in [-13,13]: box('Plaza teal inlay',(x,0.078,12),(0.16,0.014,58),'Teal')
for x in [-77,0,77]: gate('C' if x==-77 else 'B' if x==0 else 'A',x)

building('02_Administration','NHÀ ĐIỀU HÀNH',28,-2,24,22,5,True)
building('03_LearningResource','TRUNG TÂM HỌC LIỆU',57,-25,27,22,4,True)
building('04_Hightech_ATL','CÔNG NGHỆ CAO • ATL',-33,-21,38,21,5,True)
building('05_Classrooms_A3','NHÀ HỌC A3',96,-27,32,12,3)
building('05_Classrooms_A3','NHÀ HỌC A3',96,-5,32,12,3)
building('06_Humanities','KHXH & NHÂN VĂN',136,-10,16,36,3)
building('07_ICT','CÔNG NGHỆ THÔNG TIN & TRUYỀN THÔNG',30,101,43,25,4,True)
building('08_NaturalSciences','KHOA HỌC TỰ NHIÊN',97,97,53,80,4)
building('09_Agriculture','TRƯỜNG NÔNG NGHIỆP',-78,110,76,57,3)
building('10_Education','SƯ PHẠM',-116,43,32,28,3)
building('11_Classrooms_C','NHÀ HỌC C2',-93,49,15,22,3)
building('12_B1','NHÀ HỌC B1',2,151,26,22,3)
building('13_SportEducation','GIÁO DỤC THỂ CHẤT',39,152,26,22,2)
building('14_Economics','TRƯỜNG KINH TẾ',183,110,53,44,4,True)
building('15_PoliticalScience','KHOA HỌC CHÍNH TRỊ',170,65,17,22,3)
building('16_Engineering','TRƯỜNG BÁCH KHOA',11,218,43,44,4,True)
building('17_Research_RLC','PHỨC HỢP PHÒNG THÍ NGHIỆM • RLC',-80,205,80,39,5,True)
building('18_Biotech','CÔNG NGHỆ SINH HỌC',-139,202,24,36,3)
building('19_Fisheries','TRƯỜNG THỦY SẢN',-83,284,72,39,3,True)
building('20_Environment','MÔI TRƯỜNG & TÀI NGUYÊN',-246,165,24,42,3)
building('21_MekongResearch','NGHIÊN CỨU PHÁT TRIỂN ĐBSCL',-161,201,19,25,3)
for i in range(3): building('22_Dorm_B','KÝ TÚC XÁ B'+str(i+1),-190,250+i*20,62,12,5)
for i in range(3): building('23_Dorm_A','KÝ TÚC XÁ A'+str(i+1),183,215+i*22,62,13,5)

zone='24_TurtleAuditorium'
x,z=-45,16
cylinder('Auditorium drum',(x,2,z),14,4,'Ivory',40)
collision('Turtle auditorium body',(x,2,z),(25,4,25))
pts=[]; n=40; rings=9
for r in range(rings):
    angle=r*math.pi/2/(rings-1)
    for i in range(n):
        a=i*math.tau/n
        pts.append((x+18*math.cos(angle)*math.cos(a),4+7*math.sin(angle),z+18*math.cos(angle)*math.sin(a)))
faces=[]
for r in range(rings-1):
    for i in range(n): faces.append((r*n+i,r*n+(i+1)%n,(r+1)*n+(i+1)%n,(r+1)*n+i))
mesh('Turtle shell roof',pts,faces,'Roof')
for i in range(20):
    a=i*math.tau/20
    cylinder('Auditorium column',(x+14.6*math.cos(a),1.9,z+14.6*math.sin(a)),0.32,3.8,'White',8)
box('Auditorium entrance',(x,1.9,z-14),(8,3.8,1),'Glass')
sign('HỘI TRƯỜNG RÙA',(x,3.4,z-15),15,0.9)
landmarks.append(dict(id=zone,title='HỘI TRƯỜNG RÙA',position=dict(x=x,y=0,z=z),approach=dict(x=x,y=0,z=z-22)))

zone='25_Sports'
box('Gym',(103,6,195),(35,12,40),'Ivory',True); roof('Gym roof',103,195,38,43,12)
sign('NHÀ THI ĐẤU',(103,6.5,174.8),27,1.4)
box('Basketball court',(80,0.04,242),(30,0.08,17),'RoofLight')
for xx in [-15,15]: box('Court baseline',(80+xx,0.09,242),(0.1,0.02,17),'White')
for zz in [-8.5,8.5]: box('Court sideline',(80,0.09,242+zz),(30,0.02,0.1),'White')
box('Court centre line',(80,0.09,242),(0.1,0.02,17),'White')

zone='26_StreetFront'
shop_names=['CÀ PHÊ SINH VIÊN','NHÀ SÁCH','CƠM TẤM','TẠP HÓA','PHOTOCOPY','TRÀ SỮA','BÁNH MÌ','CỬA HÀNG']
for i,x in enumerate(range(-255,251,17)):
    h=random.choice([6,9,12]); w=random.uniform(11,14)
    box('Neighbourhood shophouse',(x,h/2,-91),(w,h,17),'Ivory' if i%2 else 'White',True)
    box('Shophouse upper facade',(x,h-1,-82.4),(w,1.7,0.3),'RoofLight' if i%3 else 'Teal')
    box('Shopfront glass',(x,1.6,-82.42),(w-1.5,2.8,0.16),'Glass')
    # Street shops face north; turn sign object around its vertical axis.
    box('Shop awning',(x,3.4,-80.5),(w+0.7,0.2,3.3),'Teal' if i%2 else 'Orange')
    for xx in [-w/3,0,w/3]:
        for y in range(5,int(h),3): box('Shop window',(x+xx,y,-82.4),(2,1.7,0.12),'Glass')
    roof('Shop roof',x,-91,w+0.7,18,h)
    # Sign is readable from the campus approach looking out across the road.
    obj=sign(shop_names[i%len(shop_names)],(x,3.8,-82.1),w-0.5,0.75)
    obj.rotation_euler=(math.pi/2,0,0)

zone='27_Landscape'
# Shade and pink-trumpet trees along major pedestrian approaches.
for x in [-10,10]:
    for z in range(-35,285,20):
        # Do not put tree trunks in the central weapon test/spawn strip or ICT facade.
        if -20 < z < 40 or (x>0 and 82<z<124): continue
        tree(x,z,palm=(z<65))
for x in range(-250,246,20):
    for z in [132,284]:
        if -130<x<-30 and z==284: continue
        tree(x,z,pink=(x%60==10))
for x in [-267,244]:
    for z in range(10,301,22): tree(x,z)
for x,z in [(-29,-5),(-60,-5),(-29,38),(-60,38),(53,10),(65,10),(80,35),(102,35),(-145,85),(-145,104),(-145,145),(155,160)]: tree(x,z,pink=True)
for x,z in [(-18,-15),(18,-15),(-66,42),(-25,42),(52,34),(120,34)]: bench(x,z)
for x in range(-250,251,30):
    cylinder('Street lamp pole',(x,3.6,-51),0.1,7.2,'Metal',8)
    box('Lamp arm',(x+0.8,7.1,-51),(1.8,0.12,0.15),'Metal')
    box('Lamp head',(x+1.6,7.05,-51),(0.8,0.12,0.35),'White')
for z in range(38,281,28):
    cylinder('Campus lamp',(7,3.2,z),0.09,6.4,'Metal',8)
    box('Campus lamp head',(7,6.35,z),(1.1,0.16,0.6),'White')

zone='28_MekongSciFi'
# Fictional removable game dressing, not a claim about the real university.
box('Mekong relay plaza',(-48,0.05,170),(24,0.1,21),'Paving')
for x in [-56,-40]:
    box('AI relay plinth',(x,0.65,170),(3.5,1.3,3.5),'Metal',True)
    box('AI relay tower',(x,5,170),(1.4,8,1.4),'Metal',True)
    for y in [3,5,7,9]: box('Relay cyan band',(x,y,170),(1.6,0.17,1.6),'Cyan')
box('Mekong console',(-48,1.2,160),(4,2.4,1.5),'Teal',True)
sign('MEKONG / RELAY 01',(-48,1.7,159.18),3.8,0.24,'Cyan')
for x,z in [(-20,27),(20,27),(-22,64),(21,64),(50,137),(-40,153)]:
    box('Deployable cover',(x,0.7,z),(3,1.4,1),'Concrete',True)
    box('Cover warning',(x,1.05,z-0.51),(2.8,0.22,0.03),'Yellow')
for x,z in [(-16,-30),(16,-30)]:
    box('Arrival supply crate',(x,0.65,z),(1.6,1.3,1.4),'Teal',True)
    box('Crate stripe',(x,0.8,z-0.71),(1.5,0.1,0.03),'Cyan')

zone='29_Boundary'
# Landscape boundary is explicit so players cannot run off the navigation/ground.
for name,p,s in [
    ('West retaining wall',(-294,1,110),(2,2,430)),('East retaining wall',(264,1,110),(2,2,430)),
    ('North retaining wall',(-15,1,324),(560,2,2)),('South retaining wall',(-15,1,-104),(560,2,2))]: box(name,p,s,'Concrete',True)

layout=dict(version=1, title='Cần Thơ • Đại học Cần Thơ Khu II',
    note='Stylized gameplay adaptation of the user-supplied diagram; not a surveyed reconstruction.',
    bounds=dict(center=dict(x=-15,y=10,z=110),size=dict(x=560,y=40,z=430)),
    materials=palette, collisions=collisions, landmarks=landmarks,
    navigationChecks=[dict(x=0,y=0,z=-39),dict(x=-77,y=0,z=-39),dict(x=77,y=0,z=-39),
        dict(x=-45,y=0,z=-7),dict(x=57,y=0,z=-42),dict(x=30,y=0,z=82),dict(x=-80,y=0,z=177),
        dict(x=180,y=0,z=186),dict(x=-190,y=0,z=232)])
with open(os.path.join(OUT,'CanThoCampus.layout.json'),'w',encoding='utf-8') as f: json.dump(layout,f,ensure_ascii=False,indent=2)

# Source is editable by landmark collection. Preview cameras are not exported.
zone='90_Preview'
world=bpy.data.worlds.new('Mekong warm sky')
if world.node_tree is None: world.use_nodes=True
world.node_tree.nodes['Background'].inputs['Color'].default_value=(0.58,0.72,0.8,1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value=0.55; scene.world=world
sun_data=bpy.data.lights.new('Afternoon sunlight','SUN'); sun_data.energy=2.8; sun_data.angle=0.12
sun=link(bpy.data.objects.new('Afternoon sunlight',sun_data)); sun.rotation_euler=(0.45,-0.5,-0.4)
scene.render.engine='CYCLES'; scene.cycles.samples=24; scene.cycles.use_denoising=True
scene.render.resolution_x=1600; scene.render.resolution_y=1000; scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX'
def camera(name,p,target,ortho=None,lens=30):
    data=bpy.data.cameras.new(name); obj=link(bpy.data.objects.new(name,data)); obj.location=cv(p)
    obj.rotation_euler=(Vector(cv(target))-obj.location).to_track_quat('-Z','Y').to_euler()
    data.lens=lens; data.clip_end=2000
    if ortho: data.type='ORTHO'; data.ortho_scale=ortho
    return obj
overview=camera('Campus Overview',(370,410,-350),(-20,0,105),780)
entry=camera('Gate B Arrival',(24,8,-78),(0,5,-40),lens=25)
hall=camera('Turtle Hall Walk',(-15,5,-6),(-45,5,16),lens=25)
scene.camera=overview
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'CanThoCampus.blend'))

sys.path.insert(0,os.path.dirname(os.path.abspath(__file__)))
from FinishCanThoCampus import finish
finish(OUT, RENDER)
