"""Photo-informed architecture and fictional MEKONG retrofit, revision 2.
Reads the current editable source and writes a NEW source/FBX/layout directory.
Reference photos are never used as game textures. No third-party models are used.
"""
import bpy, bmesh, math, os, sys, json, random
from mathutils import Vector
random.seed(2066)
args=sys.argv[sys.argv.index('--')+1:]
source,output=map(os.path.abspath,args[:2]); render='--render' in args
os.makedirs(output,exist_ok=True)
if os.path.exists(os.path.join(output,'CanThoCampus.blend')): raise RuntimeError('Output exists; use a new directory.')
bpy.ops.wm.open_mainfile(filepath=source)
project=os.path.abspath(os.path.join(os.path.dirname(__file__),'../..'))
layout_flag=args.index('--layout') if '--layout' in args else -1
layout_path=args[layout_flag+1] if layout_flag>=0 else os.path.join(os.path.dirname(source),'CanThoCampus.layout.json')
if not os.path.isfile(layout_path): layout_path=os.path.join(project,'Assets/_Project/Art/Environments/CanThoCampus/CanThoCampus.layout.json')
with open(layout_path,encoding='utf-8') as f: layout=json.load(f)
if layout.get('artRevision',1)!=1: raise RuntimeError('This upgrade expects the revision 1 source/layout.')
scene=bpy.context.scene
materials={}; palette=[]; cache={}; zone='30_MekongRetrofit'

def cv(p): return Vector((-p[0],-p[2],p[1]))
def xyz(p): return dict(x=p[0],y=p[1],z=p[2])
def link(obj):
    col=bpy.data.collections.get(zone)
    if col is None:
        col=bpy.data.collections.new(zone); scene.collection.children.link(col)
    for existing in list(obj.users_collection): existing.objects.unlink(obj)
    col.objects.link(obj); return obj
def define(name,color,roughness=0.7,metallic=0,emission=0,existing=None):
    material=existing or bpy.data.materials.new('CT2_'+name)
    material.name='CT2_'+name; material.diffuse_color=(*color,1)
    if material.node_tree is None: material.use_nodes=True
    bsdf=material.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value=(*color,1); bsdf.inputs['Metallic'].default_value=metallic
    bsdf.inputs['Roughness'].default_value=roughness
    bsdf.inputs['Emission Color'].default_value=(*color,1); bsdf.inputs['Emission Strength'].default_value=emission
    materials[name]=material
    palette.append(dict(name=material.name,color=dict(r=color[0],g=color[1],b=color[2],a=1),roughness=roughness,metallic=metallic,emission=emission))

colors={'Ivory':(0.58,0.66,0.66),'White':(0.76,0.83,0.82),'Concrete':(0.25,0.31,0.34),
    'Roof':(0.32,0.13,0.09),'RoofLight':(0.48,0.24,0.15),'Glass':(0.035,0.16,0.21),
    'Teal':(0.025,0.17,0.2),'Asphalt':(0.08,0.105,0.12),'Paving':(0.4,0.45,0.43),
    'Grass':(0.19,0.29,0.14),'GrassLight':(0.29,0.39,0.19),'Leaf':(0.08,0.23,0.11),
    'LeafLight':(0.2,0.36,0.15),'Pink':(0.56,0.18,0.27),'Bark':(0.22,0.15,0.085),
    'Yellow':(0.95,0.58,0.12),'Metal':(0.08,0.13,0.17),'Cyan':(0.035,0.72,0.86),
    'Orange':(0.85,0.25,0.045),'Sand':(0.47,0.4,0.28),'Water':(0.08,0.25,0.28)}
for entry in layout['materials']:
    key=entry['name'][3:]
    define(key,colors[key],0.25 if key=='Glass' else 0.65,0.55 if key in ('Metal','Glass') else 0,
        2 if key=='Cyan' else 0,bpy.data.materials.get(entry['name']))
define('Hull',(0.34,0.44,0.47),0.42,0.65)
define('Ceramic',(0.67,0.75,0.74),0.48,0.12)
define('WarmLight',(1,0.53,0.18),0.4,0.1,2.2)
define('Screen',(0.02,0.085,0.115),0.3,0.4)

def collider(name,p,s): layout['collisions'].append(dict(name=name,position=xyz(p),size=xyz(s)))
def box(name,p,s,material='Hull',solid=False,bevel=False):
    key=(material,bevel)
    if key not in cache:
        bpy.ops.mesh.primitive_cube_add(size=1)
        temp=bpy.context.object
        if bevel:
            modifier=temp.modifiers.new('Machined corners','BEVEL'); modifier.width=0.055; modifier.segments=2
            bpy.context.view_layer.objects.active=temp; bpy.ops.object.modifier_apply(modifier=modifier.name)
        temp.data.materials.append(materials[material]); cache[key]=temp.data
        bpy.data.objects.remove(temp,do_unlink=True)
    obj=link(bpy.data.objects.new(name,cache[key])); obj.location=cv(p); obj.scale=(s[0],s[2],s[1])
    if solid: collider(name,p,s)
    return obj
def beam(name,a,b,r=0.08,material='White',vertices=8):
    pa,pb=cv(a),cv(b); vector=pb-pa
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=vector.length,location=(pa+pb)*0.5)
    obj=link(bpy.context.object); obj.name=name; obj.rotation_euler=vector.to_track_quat('Z','Y').to_euler()
    obj.data.materials.append(materials[material]); return obj
def mesh(name,points,faces,material='Hull'):
    data=bpy.data.meshes.new(name); data.from_pydata([cv(p) for p in points],[],faces); data.materials.append(materials[material])
    bm=bmesh.new(); bm.from_mesh(data); bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces)); bm.to_mesh(data); bm.free(); data.update()
    return link(bpy.data.objects.new(name,data))
font=bpy.data.fonts.get('Arial Bold') or bpy.data.fonts.load('C:/Windows/Fonts/arialbd.ttf')
def sign(text,p,width,size=0.7,material='White',north=False):
    data=bpy.data.curves.new('Display '+text,'FONT'); data.body=text; data.font=font; data.align_x='CENTER'
    data.size=size; data.extrude=0.008; data.resolution_u=2
    obj=link(bpy.data.objects.new('Display '+text,data)); obj.location=cv(p); obj.rotation_euler=(math.pi/2,0,0 if north else math.pi)
    data.materials.append(materials[material]); bpy.context.view_layer.update()
    if obj.dimensions.x>width: obj.scale*=width/obj.dimensions.x
    bpy.ops.object.select_all(action='DESELECT'); obj.select_set(True); bpy.context.view_layer.objects.active=obj; bpy.ops.object.convert(target='MESH')
    return obj
def clear_zone(name):
    for obj in list(bpy.data.collections[name].objects): bpy.data.objects.remove(obj,do_unlink=True)
def replace_body(name,p,s):
    entry=next(e for e in layout['collisions'] if e['name']==name); entry['position']=xyz(p); entry['size']=xyz(s)
def arc_band(name,x,z,y,w,height,depth,material,bulge=2.6):
    pts=[]; n=20
    for i in range(n+1):
        xx=-w/2+w*i/n; zz=z-bulge*(1-(xx/(w/2))**2)
        pts.extend([(x+xx,y,zz),(x+xx,y+height,zz),(x+xx,y,zz+depth),(x+xx,y+height,zz+depth)])
    faces=[]
    for i in range(n):
        a=i*4;b=(i+1)*4
        faces.extend([(a,b,b+1,a+1),(a+2,a+3,b+3,b+2),(a+1,b+1,b+3,a+3),(a,a+2,b+2,b)])
    faces.extend([(0,1,3,2),(n*4,n*4+2,n*4+3,n*4+1)])
    return mesh(name,pts,faces,material)

# Replace repetitive hero blocks using architectural features visible in reference photos.
zone='02_Administration'; clear_zone(zone)
x,z,w,d,h=28,-2,24,22,25.6
box('Administration structure',(x,h/2,z),(w,h,d),'Ivory')
replace_body('NHÀ ĐIỀU HÀNH',(x,h/2,z),(w,h,d))
for side in [-1,1]:
    for floor in range(8):
        box('Administration side glazing',(x+side*(w/2+0.08),1.9+floor*3.2,z),(0.15,1.9,d-2),'Glass')
    for zz in range(-9,10,3): box('Administration side fin',(x+side*(w/2+0.3),h/2,z+zz),(0.7,h,0.25),'White')
for side in [-1,1]:
    for xx in [-9,-3,3,9]: box('Full-height structural fin',(x+xx,h/2,z+side*(d/2+0.25)),(0.6,h,0.65),'Ceramic')
    for floor in range(8):
        y=1.9+floor*3.2
        for xx in [-9,-6,-3,0,3,6,9]: box('Administration inset glazing',(x+xx,y,z+side*(d/2+0.08)),(2.2,1.85,0.12),'Glass')
        box('Administration frame band',(x,y+1.2,z+side*(d/2+0.28)),(w+1.3,0.35,0.8),'White')
for xx in [-10,-3,3,10]: box('Open rooftop frame column',(x+xx,h+2,z),(0.6,4,0.7),'White')
box('Open rooftop frame lintel',(x,h+4,z),(21,0.6,0.7),'White')
box('Administration smart canopy',(x,4,-15.6),(26,0.5,4),'Metal',bevel=True)
box('Administration canopy seam',(x,3.76,-17.61),(23,0.1,0.06),'Cyan')
sign('NHÀ ĐIỀU HÀNH',(x,4.3,-15),21,0.8)
for xx in [-10,10]: box('Vertical network channel',(x+xx,h/2,-13.65),(0.12,h-1,0.1),'Cyan')

zone='04_Hightech_ATL'; clear_zone(zone)
x,z,w,d,h=-33,-21,38,21,24
box('ATL structure',(x,h/2,z),(w,h,d),'Metal'); replace_body('CÔNG NGHỆ CAO • ATL',(x,h/2,z),(w,h,d))
box('ATL glass curtain wall',(x+11,h/2,z-d/2-0.1),(12,h-1,0.15),'Glass')
for side in [-1,1]:
    for floor in range(7):
        box('ATL side glass ribbon',(x+side*(w/2+0.08),2+floor*3.1,z),(0.15,1.9,d-2),'Glass')
        box('ATL side floor edge',(x+side*(w/2+0.25),3.2+floor*3.1,z),(0.6,0.25,d+0.3),'Ceramic')
    for zz in [-8,-4,0,4,8]: box('ATL side vertical rib',(x+side*(w/2+0.4),h/2,z+zz),(0.9,h,0.2),'White')
for xx in range(-17,18,2): box('ATL vertical brise soleil',(x+xx,h/2,z-d/2-0.6),(0.15,h,0.8),'White')
for level in range(24): box('ATL dense horizontal louvers',(x-10,1+level*0.95,z-d/2-0.95),(16,0.16,1.3),'Ceramic')
box('ATL central architectural screen',(x+1,h/2,z-d/2-1),(3.6,h,0.45),'White')
for level in range(16):
    for xx in [-0.9,0,0.9]: box('ATL perforation motif',(x+1+xx,1.5+level*1.35,z-d/2-1.24),(0.22,0.4,0.03),'Metal')
box('ATL entrance canopy',(x,3.6,z-d/2-2.6),(40,0.5,5.5),'Hull',bevel=True)
sign('CÔNG NGHỆ CAO  /  ATL',(x,4.35,z-d/2-1.3),30,0.9)
box('ATL illuminated lintel',(x,3.31,z-d/2-5.35),(37,0.08,0.05),'Cyan')
for xx in [-14,14]:
    box('ATL roof exoskeleton',(x+xx,h+1,z),(1.2,2,17),'White')
    beam('ATL roof brace',(x+xx,h+1,z-8),(x+xx,h+3.5,z+8),0.22,'Hull')

zone='03_LearningResource'; clear_zone(zone)
x,z,w,d,h=57,-25,27,22,13.5
box('LRC primary volume',(x,h/2,z),(w,h,d),'Ivory')
replace_body('TRUNG TÂM HỌC LIỆU',(x,h/2,z-1.4),(w,h,d+2.8))
for side in [-1,1]:
    for floor in range(3): box('LRC side glazing',(x+side*(w/2+0.08),2.4+floor*3.5,z),(0.15,2.6,d-2),'Glass')
    for zz in range(-9,10,3): box('LRC side louver',(x+side*(w/2+0.3),5.4,z+zz),(0.65,9.5,0.18),'Ceramic')
for floor in range(3):
    y=1.1+floor*3.5
    arc_band('Curved LRC glazing',x,z-d/2-0.1,y,w,2.6,0.18,'Glass')
    arc_band('LRC curved floor edge',x,z-d/2-0.4,y+2.7,w+0.6,0.35,0.4,'Ceramic')
    for xx in range(-12,13,3):
        zz=z-d/2-0.1-2.6*(1-(xx/(w/2))**2)
        box('LRC window mullion',(x+xx,y+1.3,zz-0.12),(0.13,2.6,0.18),'Hull')
arc_band('LRC sculpted curved crown',x,z-d/2-0.5,10.9,w+1,3.3,0.6,'Ceramic')
for y in [11.4,12.2,13.0]: arc_band('Crown panel seam',x,z-d/2-0.52,y,w+1,0.035,0.62,'Hull')
arc_band('Archive energy seam',x,z-d/2-0.57,10.65,w+1,0.12,0.65,'Cyan')
box('LRC east sign blade',(x+10,8,z-d/2-2),(4,12,0.5),'Metal',bevel=True)
sign('HỌC LIỆU',(x+10,11,z-d/2-2.3),3.7,0.65,'WarmLight')
sign('MEKONG',(x+10,9.5,z-d/2-2.3),3.7,0.6,'Cyan')
for y in [2.2,4.5,6.8]: box('Archive sign glyph',(x+10,y,z-d/2-2.31),(2.6,0.12,0.05),'Cyan')

# The exterior reference has a large triangular gable, not a circular igloo.
zone='24_TurtleAuditorium'; clear_zone(zone)
x,z=-45,16; w,d=27,30
box('Turtle Hall auditorium body',(x,3.5,z),(w,7,d),'Ivory')
replace_body('Turtle auditorium body',(x,3.5,z),(w,7,d))
for side in [-1,1]:
    box('Hall side clerestory',(x+side*(w/2+0.08),4.3,z),(0.15,2.6,d-2),'Glass')
    for zz in range(-13,14,3): box('Hall side pilaster',(x+side*(w/2+0.25),3.5,z+zz),(0.6,7,0.35),'Ceramic')
# Closed gently curved gable shell, with white structural ribs and metal service panels.
points=[]; n=24
for i in range(n+1):
    xx=-17+34*i/n; yy=6.6+7.8*(1-(abs(xx)/17)**0.92)
    points.extend([(x+xx,yy,z-18),(x+xx,yy,z+18),(x+xx,yy+0.45,z-18),(x+xx,yy+0.45,z+18)])
faces=[]
for i in range(n):
    a=i*4;b=(i+1)*4
    faces.extend([(a,b,b+1,a+1),(a+2,a+3,b+3,b+2),(a,a+2,b+2,b),(a+1,b+1,b+3,a+3)])
faces.extend([(0,1,3,2),(n*4,n*4+2,n*4+3,n*4+1)])
mesh('Turtle Hall modern shell',points,faces,'Hull')
for zz in [z-18.2,z-9,z,z+9,z+18.2]:
    for i in range(n):
        xx1=-17+34*i/n; xx2=-17+34*(i+1)/n
        y1=7.1+7.8*(1-(abs(xx1)/17)**0.92); y2=7.1+7.8*(1-(abs(xx2)/17)**0.92)
        beam('Hall shell structural rib',(x+xx1,y1,zz),(x+xx2,y2,zz),0.12,'White')
for side in [-1,1]:
    for i in range(12):
        xx=side*(1+i*1.2); yy=6.9+7.8*(1-(abs(xx)/17)**0.92)
        box('Hall front glazing',(x+xx,(yy+5)/2,z-15.06),(1.05,yy-5,0.12),'Glass')
        box('Hall front mullion',(x+xx,yy/2,z-15.25),(0.15,yy,0.3),'Ceramic')
box('Hall portal canopy',(x,4.3,z-17.2),(20,0.5,4.4),'Ceramic',bevel=True)
box('Hall portal lighting',(x,4.02,z-19.43),(18.5,0.1,0.07),'Cyan')
sign('HỘI TRƯỜNG RÙA',(x,4.65,z-17.6),18,0.9)
sign('CAMPUS ASSEMBLY  /  MEKONG ARCHIVE',(x,2.7,z-15.22),15,0.4,'WarmLight')
for xx in [-11,11]: box('Hall exterior pier',(x+xx,3.1,z-16.8),(0.7,6.2,0.9),'White')

# Gate retrofit retains the white monument signage and open approach visible in photos.
zone='30_MekongGates'
for x,letter in [(-77,'C'),(0,'B'),(77,'A')]:
    for side in [-1,1]:
        box('Gate ceramic armour',(x+side*8.8,3.1,-50.1),(2.2,6.2,0.45),'Ceramic',bevel=True)
        box('Gate scanner channel',(x+side*8.8,3.3,-50.35),(0.18,5.2,0.09),'Cyan')
        beam('Checkpoint overhead brace',(x+side*8.8,6.1,-49),(x+side*5.8,7.7,-49),0.24,'Hull')
        box('Gate camera housing',(x+side*7.9,4.8,-49.8),(0.5,0.4,0.8),'Metal',bevel=True)
        box('Gate scanner optic',(x+side*7.9,4.8,-50.22),(0.3,0.22,0.04),'WarmLight')
    box('Checkpoint visor',(x,7.7,-49),(12,0.5,2),'Metal',bevel=True)
    box('Checkpoint visor energy slit',(x,7.42,-50.03),(10.8,0.1,0.07),'Cyan')
    box('White monument sign',(x-22,2,-51),(10,4,1.2),'Ceramic',True,True)
    sign('ĐẠI HỌC CẦN THƠ',(x-22,2.7,-51.64),9,0.58,'Metal')
    sign('CANTHO UNIVERSITY',(x-22,1.9,-51.64),8.5,0.4,'Teal')
    sign('KHU II  /  SECTOR '+letter,(x-22,1.15,-51.64),8,0.33,'Metal')
    box('Monument sign base seam',(x-22,0.45,-51.66),(9,0.08,0.06),'Cyan')

zone='31_CampusRetrofits'
for landmark in layout['landmarks']:
    if landmark['id'] in ('02_Administration','03_LearningResource','04_Hightech_ATL','24_TurtleAuditorium'): continue
    body=next((c for c in layout['collisions'] if c['name']==landmark['title']),None)
    if body is None: continue
    p,s=body['position'],body['size']; x,z=p['x'],p['z']; w,d,h=s['x'],s['z'],s['y']
    for side in [-1,1]:
        box('Retrofit corner housing',(x+side*(w/2-0.5),h/2,z-d/2-0.28),(0.65,h,0.6),'Metal',bevel=True)
        box('Retrofit network conduit',(x+side*(w/2-0.5),h/2,z-d/2-0.62),(0.09,h-0.6,0.06),'Cyan')
    box('Equipment roof enclosure',(x,h+1.3,z),(w*0.22,1.8,d*0.28),'Hull',bevel=True)
    for xx in [-1,0,1]: box('Ventilation fin',(x+xx*w*0.06,h+2.25,z),(0.16,0.15,d*0.25),'Metal')
    box('Facade service plaque',(x-w/2+2.4,1.75,z-d/2-0.09),(2.8,1.3,0.12),'Screen',bevel=True)
    for i in range(3): box('Facade service glyph',(x-w/2+2.1+i*0.45,1.75,z-d/2-0.17),(0.14,0.6,0.02),'Cyan')

zone='32_MekongInfrastructure'
# Data tower fits between existing blocks, outside all spawn/combat fixtures.
x,z=75,-25
box('Mekong tower foundation',(x,0.6,z),(5,1.2,5),'Concrete',True,True)
box('Mekong data mast',(x,12.5,z),(2.8,24,2.8),'Metal',True,True)
for level in range(8):
    y=3+level*3
    box('Mast cooling module',(x,y,z),(4,1.2,4),'Hull',bevel=True)
    box('Mast luminous seam',(x,y-0.66,z),(3.7,0.13,3.7),'Cyan')
for side in [-1,1]: beam('Data mast angled antenna',(x+side*1.5,24,z),(x+side*4,29,z),0.12,'White')
sign('MEKONG / 01',(x,2.1,z-2.55),4.5,0.4,'Cyan')
# Distinctive utility columns and wayfinding, kept away from the central clear lane.
for x,z,label in [(-16,-26,'CỔNG B'),(60,44,'HỌC LIỆU'),(-31,146,'RLC / ATL'),(143,144,'KÝ TÚC XÁ')]:
    box('Wayfinding pedestal',(x,1.6,z),(1.1,3.2,0.7),'Metal',True,True)
    box('Wayfinding display',(x,2.1,z-0.37),(0.85,1.3,0.05),'Screen')
    for y in [1.8,2.1,2.4]: box('Wayfinding cyan line',(x,y,z-0.41),(0.6,0.05,0.03),'Cyan')
    sign(label,(x,3.1,z-0.42),2.8,0.35,'White')
for z in range(-20,280,24):
    for x in [-5.6,5.6]: box('Spine navigation luminaire',(x,0.19,z),(0.12,0.08,4),'Cyan')

def shelter(x,z):
    for side in [-1,1]: box('Shelter support',(x+side*3.5,1.7,z),(0.18,3.4,0.3),'Hull')
    box('Transit smart canopy',(x,3.4,z),(8.3,0.35,3.6),'Ceramic',bevel=True)
    box('Transit canopy light',(x,3.18,z-1.83),(7,0.08,0.05),'Cyan')
    box('Transit rear glass',(x,1.8,z+1.5),(7,2.8,0.12),'Glass')
    box('Transit bench',(x,0.55,z+0.5),(5,0.2,0.65),'Hull',True,True)
    box('Route information display',(x+2.7,1.8,z-0.1),(1,1.6,0.15),'Screen')
    sign('CTU / TRANSIT',(x,2.6,z-1.87),6.5,0.43,'Cyan')
zone='33_Transit'; shelter(-40,-76); shelter(118,-76)

def shuttle(x,z):
    box('Autonomous shuttle chassis',(x,0.8,z),(7.6,1.1,2.5),'Metal',True,True)
    box('Autonomous shuttle hull',(x,1.8,z),(7.1,2.5,2.7),'Ceramic',bevel=True)
    box('Shuttle wrap glazing',(x,2.2,z-1.38),(6.4,1.35,0.15),'Glass',bevel=True)
    for xx in [-2.4,-0.8,0.8,2.4]: box('Shuttle window mullion',(x+xx,2.2,z-1.48),(0.1,1.4,0.06),'Metal')
    box('Shuttle battery skirt',(x,0.7,z-1.44),(6.7,0.4,0.08),'Hull')
    box('Shuttle side light',(x,1.22,z-1.49),(6.5,0.07,0.04),'Cyan')
    for xx in [-2.4,2.4]:
        for side in [-1,1]:
            bpy.ops.mesh.primitive_cylinder_add(vertices=16,radius=0.53,depth=0.22,location=cv((x+xx,0.58,z+side*1.3)))
            obj=link(bpy.context.object); obj.name='Shuttle enclosed wheel'; obj.rotation_euler.x=math.pi/2; obj.data.materials.append(materials['Metal'])
    sign('MEKONG TRANSIT',(x,1.5,z-1.52),5.4,0.35,'Teal')
shuttle(-27,-77)

zone='34_StreetDetail'
for x,z in [(-18,-21),(18,-21),(-65,-8),(80,40),(-137,152),(146,168)]:
    box('Planter ceramic bowl',(x,0.45,z),(3.2,0.9,1.8),'Concrete',True,True)
    box('Planter soil',(x,0.91,z),(2.9,0.04,1.5),'Sand')
    for xx in [-0.9,0,0.9]:
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=0.8,location=cv((x+xx,1.4,z)))
        obj=link(bpy.context.object); obj.name='Planter shrub'; obj.scale=(1,0.75,0.65); obj.data.materials.append(materials['LeafLight'])
for x in [-220,-140,-60,20,100,180]:
    box('Road service hatch',(x,0.112,-66),(1.8,0.025,0.8),'Metal')
    for offset in [-0.5,0,0.5]: box('Drain channel',(x+offset,0.128,-66),(0.035,0.01,0.65),'Concrete')
# Break up triangular canopies with clustered leaves and forked branches.
zone='27_Landscape'
old_canopies=[obj for obj in list(bpy.data.collections[zone].objects) if obj.name.startswith(('Shade tree canopy','Pink trumpet canopy'))]
for obj in old_canopies:
    p=(-obj.location.x,obj.location.z,-obj.location.y); pink=obj.name.startswith('Pink'); material='Pink' if pink else 'LeafLight'
    bpy.data.objects.remove(obj,do_unlink=True)
    for i,(dx,dy,dz,r) in enumerate([(-1.4,-0.3,0,2.4),(1.2,0.3,0.6,2.7),(0,1.8,-0.6,2.5)]):
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,radius=r,location=cv((p[0]+dx,p[1]+dy,p[2]+dz)))
        leaf=link(bpy.context.object); leaf.name='Clustered tropical canopy'; leaf.scale=(1,0.9,0.7); leaf.data.materials.append(materials[material if i%2 else ('Pink' if pink else 'Leaf')])
        beam('Forked tree branch',(p[0],p[1]-2,p[2]),(p[0]+dx,p[1]+dy,p[2]+dz),0.12,'Bark')

layout['artRevision']=2
layout['materials']=palette
layout['note']='Photo-informed CTU landmark silhouettes with a fictional near-future MEKONG retrofit; compressed gameplay scale.'
layout['detailStats']=dict(heroLandmarksRebuilt=4,referencePhotos=7,staticShuttles=1,transitShelters=2)
with open(os.path.join(output,'CanThoCampus.layout.json'),'w',encoding='utf-8') as f: json.dump(layout,f,ensure_ascii=False,indent=2)
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(0.35,0.49,0.62,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=0.45
bpy.data.lights['Afternoon sunlight'].energy=2.4
scene.cycles.samples=24
for name,p,target,lens in [('Gate B Arrival',(24,7,-77),(0,5,-40),28),('Turtle Hall Walk',(-64,6,-19),(-45,8,12),28)]:
    cam=bpy.data.objects[name]; cam.location=cv(p); cam.rotation_euler=(cv(target)-cam.location).to_track_quat('-Z','Y').to_euler(); cam.data.lens=lens
scene.camera=bpy.data.objects['Gate B Arrival']
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(output,'CanThoCampus.blend'))
sys.path.insert(0,os.path.dirname(os.path.abspath(__file__)))
from FinishCanThoCampus import finish
finish(output,render)
