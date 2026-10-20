"""Photo-informed architecture/material pass, revision 4 -> 5, new output only."""
import bpy
import bmesh
import json
import math
import os
import random
import sys
from mathutils import Vector

sys.dont_write_bytecode = True
args = sys.argv[sys.argv.index('--')+1:]
source, output = map(os.path.abspath, args[:2])
if os.path.exists(output): raise RuntimeError('Existing output preserved; choose a new candidate directory.')
with open(os.path.join(os.path.dirname(source),'CanThoCampus.layout.json'),encoding='utf-8') as f: layout=json.load(f)
if layout.get('artRevision') != 4: raise RuntimeError('Expected matching editable revision4 source/layout.')
bpy.ops.wm.open_mainfile(filepath=source)
os.makedirs(output)
sys.path.insert(0,os.path.dirname(__file__))
from CampusSurfaceLibrary import create_library
mats=create_library(output,layout)
scene=bpy.context.scene
random.seed(19662048)
zone='02_Administration'
cache={}


def cv(p): return Vector((-p[0],-p[2],p[1]))
def uv(p): return (-p.x,p.z,-p.y)
def xyz(p): return dict(x=round(p[0],5),y=round(p[1],5),z=round(p[2],5))


def plain(key,color,roughness=.7,metallic=0,emission=0):
    m=bpy.data.materials.new('CT5_'+key)
    if m.node_tree is None: m.use_nodes=True
    n=m.node_tree.nodes.get('Principled BSDF')
    n.inputs['Base Color'].default_value=(*color,1)
    n.inputs['Roughness'].default_value=roughness
    n.inputs['Metallic'].default_value=metallic
    n.inputs['Emission Color'].default_value=(*color,1)
    n.inputs['Emission Strength'].default_value=emission
    m.diffuse_color=(*color,1)
    mats[key]=m
    layout['materials'].append(dict(name=m.name,color=dict(r=color[0],g=color[1],b=color[2],a=1),
                                   roughness=roughness,metallic=metallic,emission=emission))


plain('Glass',(.22,.31,.30),.22,.08)
plain('Metal',(.28,.31,.30),.5,.6)
plain('LetterBlue',(.055,.19,.36),.62)
plain('LetterRed',(.57,.065,.035),.7)
plain('Gold',(.42,.30,.095),.42,.65)
plain('WarmPaint',(.72,.64,.40),.86)
plain('Status',(.04,.48,.56),.45,.05,.35)
plain('Shadow',(.10,.12,.12),.95)

replace={'CT2_Ivory':'Plaster','CT2_White':'Plaster','CT3_Plaster':'Plaster','CT2_Ceramic':'Plaster',
         'CT2_Concrete':'Concrete','CT4_Stone':'Concrete','CT3_Stone':'Concrete',
         'CT3_FrameBlue':'BluePaint','CT3_RedPaver':'RedPaver',
         'CT4_Walk':'Walk','CT3_Walk':'Walk','CT2_Paving':'Walk',
         'CT4_Asphalt':'Asphalt','CT2_Asphalt':'Asphalt',
         'CT2_Roof':'Terracotta','CT2_RoofLight':'Terracotta',
         'CT4_Grass':'Grass','CT4_Green':'Grass','CT2_Grass':'Grass','CT2_GrassLight':'Grass',
         'CT2_Bark':'Bark','CT3_Aluminium':'SilverCladding',
         'CT3_Glazing':'Glass','CT2_Glass':'Glass','CT2_Cyan':'Status'}
for mesh in bpy.data.meshes:
    for index,material in enumerate(mesh.materials):
        if material and material.name in replace: mesh.materials[index]=mats[replace[material.name]]


def col():
    collection=bpy.data.collections.get(zone)
    if collection is None:
        collection=bpy.data.collections.new(zone);scene.collection.children.link(collection)
    return collection


def link(obj): col().objects.link(obj);return obj


def collider(name,p,size): layout['collisions'].append(dict(name=name,position=xyz(p),size=xyz(size)))


def box(name,p,size,mat='Plaster',solid=False):
    key=('box',mat)
    if key not in cache:
        bpy.ops.mesh.primitive_cube_add(size=1);tmp=bpy.context.object
        tmp.data.materials.append(mats[mat]);cache[key]=tmp.data;bpy.data.objects.remove(tmp,do_unlink=True)
    obj=link(bpy.data.objects.new(name,cache[key]));obj.location=cv(p);obj.scale=(size[0],size[2],size[1])
    if solid: collider(name,p,size)
    return obj


def beam(name,a,b,r=.05,mat='Metal'):
    key=('cylinder',mat)
    if key not in cache:
        bpy.ops.mesh.primitive_cylinder_add(vertices=10,radius=1,depth=1);tmp=bpy.context.object
        tmp.data.materials.append(mats[mat]);cache[key]=tmp.data;bpy.data.objects.remove(tmp,do_unlink=True)
    pa,pb=cv(a),cv(b);direction=pb-pa
    obj=link(bpy.data.objects.new(name,cache[key]));obj.location=(pa+pb)/2
    obj.scale=(r,r,direction.length);obj.rotation_euler=direction.to_track_quat('Z','Y').to_euler();return obj


def mesh(name,points,faces,mat):
    data=bpy.data.meshes.new(name);data.from_pydata([cv(p) for p in points],[],faces);data.materials.append(mats[mat])
    bm=bmesh.new();bm.from_mesh(data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(data);bm.free()
    data.update();return link(bpy.data.objects.new(name,data))


font=bpy.data.fonts.get('Arial Bold') or bpy.data.fonts.load('C:/Windows/Fonts/arialbd.ttf')


def sign(text,p,size=.5,width=24,mat='LetterBlue'):
    data=bpy.data.curves.new(text,'FONT');data.body=text;data.font=font;data.size=size;data.align_x='CENTER'
    data.extrude=.007;data.resolution_u=2;obj=link(bpy.data.objects.new(text,data));obj.location=cv(p)
    obj.rotation_euler=(math.pi/2,0,math.pi);data.materials.append(mats[mat]);bpy.context.view_layer.update()
    if obj.dimensions.x>width:obj.scale*=width/obj.dimensions.x
    bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
    bpy.ops.object.convert(target='MESH');return obj


def clear(name):
    for obj in list(bpy.data.collections[name].objects):bpy.data.objects.remove(obj,do_unlink=True)


def aircon(x,y,z):
    box('Air conditioner compressor',(x,y,z),(1,.55,.44),'Plaster')
    box('Compressor ventilation grille',(x,y,z-.23),(.82,.39,.035),'Shadow')
    for dx in [-.32,-.16,0,.16,.32]:box('Compressor grille rib',(x+dx,y,z-.26),(.04,.4,.025),'Metal')
    beam('Aircon condensate tube',(x+.4,y,z),(x+.4,y-.8,z),.024,'Plaster')


# Main administration: genuine window frames, curtains, ventilation and rainwater pipes.
zone='02_Administration'
for side in [-1,1]:
    front=-2+side*11.08
    for floor in range(8):
        y=1.5+floor*3.1
        for dx in [-9,-6,-3,0,3,6,9]:
            x=22+dx
            for xx in [-.7,0,.7]:box('Administration window secondary sash',(x+xx,y,front+side*.15),(.035,1.95,.04),'Plaster')
            box('Window horizontal transom',(x,y+.4,front+side*.16),(2.28,.04,.05),'Plaster')
            if (floor+dx)%4==0:
                for xx in [-.9,-.75,.75,.9]:box('Interior curtain behind glazing',(x+xx,y,front-side*.025),(.12,1.8,.04),'WarmPaint')
        if side==-1 and floor in [1,3,5,7]:
            for dx in [-4.4,4.4]:aircon(22+dx,y-.85,front-.65)
    for dx in [-11.2,11.2]:
        beam('Administration rainwater downpipe',(22+dx,.2,front+.55),(22+dx,25.4,front+.55),.075,'Plaster')
for dx in [-10,-5,0,5,10]:
    # Actual colored pennants replace the uniform sci-fi flags without game logic.
    box('Administration colored pennant',(22+dx+.2,5.8,-16.3),(.4,.7,.04),
        'LetterRed' if dx%10==0 else 'WarmPaint')

# Library lettering and jointed metal screen; the tested curved body/colliders remain.
zone='03_LearningResource'
for obj in list(col().objects):
    if any(obj.name.startswith(s) for s in ['ĐẠI HỌC','CẦN THƠ','TRUNG TÂM','HỌC LIỆU','MEKONG / ARCHIVE']):
        bpy.data.objects.remove(obj,do_unlink=True)
sign('ĐẠI HỌC CẦN THƠ',(67,12,-38.30),.38,3.9,'LetterRed')
sign('CAN THO UNIVERSITY',(67,11.25,-38.30),.29,3.9,'LetterRed')
sign('TRUNG TÂM HỌC LIỆU',(57,3.85,-42.06),.48,17,'LetterBlue')
for x in [44,69.7]:
    beam('Library rainwater pipe',(x,.15,-24),(x,13.6,-24),.065,'Metal')
for side in [-1,1]:
    for yy in [1.3,4.8,8.3]:
        for zz in range(-33,-15,3):
            box('Library side curtain wall transom',(57+side*13.75,yy,zz),(.08,.05,2.85),'Plaster')

# Ground-level open CTU entrances: remove the old overhead scanner/arch completely.
for name in ['01_Gates','30_MekongGates']:clear(name)
layout['collisions']=[e for e in layout['collisions'] if not e['name'].startswith(('Gate ','White monument sign'))]
zone='01_Gates'
for gate,x in [('C',-77),('B',0),('A',77)]:
    for side in [-1,1]:
        box('Gate '+gate+' pylon',(x+side*8.8,1.7,-49),(1.4,3.4,1.4),'Plaster',True)
        box('Gate small status sensor',(x+side*8.8,1.8,-49.73),(.18,.8,.045),'Status')
    sx=x+15 if gate=='C' else x-17
    box('Gate '+gate+' monument sign',(sx,1.5,-50),(13,3,1),'Plaster',True)
    sign('ĐẠI HỌC CẦN THƠ',(sx,2.05,-50.54),.66,12,'Gold')
    sign('CANTHO UNIVERSITY',(sx,1.3,-50.54),.34,11,'Gold')
    sign('KHU II  /  CỔNG '+gate,(sx,.8,-50.54),.26,9,'LetterBlue')
    gx=x+13
    box('Gate guardhouse',(gx,1.8,-45.7),(5,3.6,4.4),'Plaster',True)
    box('Gate guardhouse flat roof',(gx,3.72,-45.7),(5.6,.25,5),'Concrete')
    box('Gate guardhouse glazing',(gx,2,-47.98),(4.1,1.6,.10),'Glass')
    for xx in [-1.6,-.8,0,.8,1.6]:box('Guardhouse security grille',(gx+xx,2,-48.08),(.045,1.6,.05),'Metal')
    # Sliding leaf parked beside the opening, never across the 16m clear passage.
    for xx in range(0,5):box('Parked sliding gate picket',(x+10+xx*.45,1.4,-49.6),(.06,2.8,.06),'Metal')
    beam('Retracted sliding gate top rail',(x+9.9,2.7,-49.6),(x+12.2,2.7,-49.6),.045)

# Turtle Hall's pitched concrete roof, distinctive gable grille and two side screens.
zone='24_TurtleAuditorium';clear(zone)
layout['collisions']=[e for e in layout['collisions'] if e['name']!='Turtle auditorium body']
x,z,w,d=-45,16,27,30
box('Turtle auditorium body',(x,3.5,z),(w,7,d),'Plaster',True)
for side in [-1,1]:
    zz=z+side*d/2
    for dx in range(-12,13,2):box('Turtle Hall narrow glazing',(x+dx,5,zz+side*.08),(1.25,3.4,.12),'Glass')
    for dx in range(-12,13,3):box('Turtle Hall gable mullion',(x+dx,8,zz+side*1.35),(.14,7.5-abs(dx)*.28,.23),'Metal')
roofpoints=[(x-15,7,z-16),(x+15,7,z-16),(x+15,7,z+16),(x-15,7,z+16),(x,14,z-16),(x,14,z+16)]
mesh('Turtle Hall concrete pitched shell',roofpoints,[(0,4,5,3),(4,1,2,5)],'Concrete')
mesh('Turtle Hall glazed upper gables',roofpoints,[(0,1,4),(3,5,2)],'Glass')
for side in [-1,1]:
    beam('Turtle Hall bright gable fascia',(x-15,7,z+side*16),(x,14,z+side*16),.27,'Plaster')
    beam('Turtle Hall bright gable fascia',(x,14,z+side*16),(x+15,7,z+side*16),.27,'Plaster')
box('Turtle Hall porch roof',(x,3.65,-1),(22,.35,5),'Plaster',True)
for dx in [-9,-3,3,9]:box('Turtle Hall porch column',(x+dx,1.75,-2.5),(.5,3.5,.5),'Plaster',True)
for side in [-1,1]:
    for row in range(3):
        for column in range(2):
            px=x+side*12.8;py=5.7+row*1.25;pz=-.22
            beam('Turtle Hall breeze block diagonal',(px-.6,py-.5,pz),(px+.6,py+.5,pz),.07,'Plaster')
            beam('Turtle Hall breeze block diagonal',(px-.6,py+.5,pz),(px+.6,py-.5,pz),.07,'Plaster')
sign('HỘI TRƯỜNG RÙA',(x,3.9,-3.64),.75,21)
box('Turtle Hall Vietnam flag',(x,10.2,-.3),(1.5,1,.03),'LetterRed')
collider('Turtle Hall roof proxy',(x,10.5,z),(27,7,30))

# RLC: thin laboratory frontage with rear return wings instead of the solid slab.
zone='17_Research_RLC';clear(zone)
layout['collisions']=[e for e in layout['collisions'] if e['name']!='PHỨC HỢP PHÒNG THÍ NGHIỆM • RLC']
for obj in list(bpy.data.collections['31_CampusRetrofits'].objects):
    px,py,pz=uv(obj.location)
    if -108<px<-38 and 127<pz<163:bpy.data.objects.remove(obj,do_unlink=True)
box('PHỨC HỢP PHÒNG THÍ NGHIỆM • RLC',(-73,7.6,140),(62,15.2,11),'Plaster',True)
for xx in [-100,-46]:box('RLC rear return laboratory wing',(xx,7.6,151),(8,15.2,14),'Plaster',True)
for floor in range(4):
    yy=2+floor*3.6
    box('RLC recessed glazing ribbon',(-73,yy,134.4),(59,2.2,.15),'Glass')
    box('RLC horizontal sunshade',(-73,yy+1.4,133.9),(63,.24,1.35),'Plaster')
    for dx in range(-28,29,3):box('RLC vertical screen',(-73+dx,yy,133.6),(.12,2.45,1.1),'Plaster')
box('RLC entrance canopy',(-73,3.6,131.4),(23,.25,5),'Concrete',True)
for dx in [-10,10]:box('RLC entrance pillar',(-73+dx,1.8,130.5),(.4,3.6,.4),'Plaster',True)
sign('PHỨC HỢP PHÒNG THÍ NGHIỆM / RLC',(-73,4.15,128.85),.65,23)

# CICT's characteristic blue/white zigzag entrance, based on its official venue image.
zone='07_ICT'
box('CICT blue entrance canopy',(14,3.8,60),(24,.4,5.2),'LetterBlue',True)
for xx in [4,24]:box('CICT entrance support',(xx,1.8,60.2),(.4,3.6,.4),'Plaster',True)
for index in range(6):
    xa=2+index*4;xb=xa+4
    beam('CICT zigzag white fascia',(xa,4.5+(index%2)*.6,57.35),(xb,4.5+((index+1)%2)*.6,57.35),.14,'Plaster')
sign('TRƯỜNG CÔNG NGHỆ THÔNG TIN VÀ TRUYỀN THÔNG',(14,5.6,57.3),.52,31)
sign('COLLEGE OF INFORMATION AND COMMUNICATION TECHNOLOGY',(14,5.05,57.3),.26,28)

# Repeated regional architecture: framed sash windows, doors, gutters and AC units.
for collection in list(bpy.data.collections):
    if not any(collection.name.startswith(prefix) for prefix in [str(i).zfill(2)+'_' for i in range(5,24)]):continue
    if collection.name in ['07_ICT','17_Research_RLC']:continue
    zone=collection.name
    windows=[o for o in list(collection.objects) if o.name.startswith(('South window','North window'))]
    for index,obj in enumerate(windows):
        px,py,pz=uv(obj.location);width=obj.dimensions.x;height=obj.dimensions.z
        side=-1 if obj.name.startswith('South') else 1
        for dx in [-width/2,width/2,0]:box('Faculty window sash',(px+dx,py,pz+side*.08),(.055,height+.1,.055),'Plaster')
        box('Faculty window sill',(px,py-height/2,pz+side*.13),(width+.2,.10,.26),'Plaster')
        if side==-1 and index%23==0 and py>3:aircon(px,py-height/2-.35,pz-.4)
    if collection.name.startswith(('22_','23_')):
        for lm in [l for l in layout['landmarks'] if l['id'].startswith(collection.name+'_')]:
            px,pz=lm['position']['x'],lm['position']['z']
            box('Dorm warm yellow stair accent',(px,8,pz),(3.2,16,1),'WarmPaint')

# Replace blocky outer-grove canopies with branching trunks and alpha-cutout leaf cards.
zone='27_Landscape'
trunks=[e for e in layout['collisions'] if e['name'] in ('Shade tree trunk','Campus route tree trunk')]
clear(zone)
for obj in list(bpy.data.collections['36_CampusRoute'].objects):
    if obj.name.startswith(('Shade tree trunk','Shade tree branch','Layered broadleaf canopy')):
        bpy.data.objects.remove(obj,do_unlink=True)
cardmesh=bpy.data.meshes.new('Leaf card')
cardmesh.from_pydata([(-.5,0,-.5),(.5,0,-.5),(.5,0,.5),(-.5,0,.5)],[],[(0,1,2,3)])
cardmesh.materials.append(mats['LeafCluster']);carduv=cardmesh.uv_layers.new(name='UVMap')
for i,p in enumerate([(0,0),(1,0),(1,1),(0,1)]):carduv.data[i].uv=p
for entry in trunks:
    p,s=entry['position'],entry['size'];x,z=p['x'],p['z'];h=s['y']
    beam('Branching shade trunk',(x,0,z),(x+.12,h,z),.21,'Bark')
    for branch in range(4):
        a=branch*math.tau/4+random.random()*.5
        ex,ez=x+math.cos(a)*2.2,z+math.sin(a)*2.2
        beam('Shade tree branch',(x,h*.58,z),(ex,h+.2,ez),.08,'Bark')
    for index in range(64):
        a=random.uniform(0,math.tau);r=3.3*math.sqrt(random.random())
        obj=link(bpy.data.objects.new('Photometric leaf cluster',cardmesh))
        obj.location=cv((x+math.cos(a)*r,h+random.uniform(-.2,2),z+math.sin(a)*r))
        obj.rotation_euler=(random.uniform(-.9,.9),random.uniform(-.7,.7),random.uniform(0,math.tau))
        obj.scale=(3.2,1,2.8)

# Street furniture stays outside the route and all configured entry points.
zone='49_RealWorldDetails'
protected=layout['playableRoute']+[l['approach'] for l in layout['landmarks']]
for index,road in enumerate(layout['roads']):
    if index%3:continue
    a,b=road['a'],road['b'];dx,dz=b['x']-a['x'],b['z']-a['z'];length=math.hypot(dx,dz)
    x=(a['x']+b['x'])/2-dz/length*(road['width']/2+1.4)
    z=(a['z']+b['z'])/2+dx/length*(road['width']/2+1.4)
    if any(math.hypot(x-p['x'],z-p['z'])<4 for p in protected):continue
    beam('Campus street light',(x,0,z),(x,6,z),.075,'Metal')
    beam('Street light outreach',(x,6,z),(x+1,6.2,z),.055,'Metal')
    box('LED street light housing',(x+1,6.2,z),(1.1,.12,.35),'Metal')
    collider('Campus lamp post',(x,3,z),(.18,6,.18))
    box('Flush storm drain',(x-.3,.11,z+2),(.55,.03,.8),'Metal')

layout['artRevision']=5
layout['architectureRevision']=1
layout['note']='Revision4 schematic footprint retained; photo-informed architecture and original tiled surface textures, 16 October 2026. No surveyed measurements.'
layout['architecture'] = dict(reference='ArtSource/References/CanThoCampus/REFERENCES_REALISM.md',
    textureTileMetres=2,textureResolution=512,generatedTextureImages=21,
    upgraded=['Administration','Learning Resource Center','Turtle Hall','Gates A B C','RLC','CICT','Faculty windows','Dorm details','Tree groves'],
    note='Existing estimates retained outside observed facades. Most buildings remain exterior-only.')
with open(os.path.join(output,'CanThoCampus.layout.json'),'w',encoding='utf-8') as f:json.dump(layout,f,ensure_ascii=False,indent=2)
bpy.context.view_layer.update()
scene.camera=bpy.data.objects['Campus Overview']
scene.render.engine='BLENDER_EEVEE'
for image in bpy.data.images:
    if image.name.startswith('CT5_'):
        image.filepath='//Textures/'+image.name+'.png'
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(output,'CanThoCampus.blend'),relative_remap=False)
from FinishCanThoCampus import finish
finish(output,False)
if '--render' in args:
    bpy.data.collections['90_InspectionLabels'].hide_render=True
    for name,p,target in [('Campus-Overview',(240,240,-210),(-30,0,90)),
                          ('Administration-Realism',(-6,10,-39),(22,13,-2)),
                          ('Library-Realism',(82,10,-58),(57,8,-25)),
                          ('Turtle-Realism',(-66,8,-17),(-45,7,16)),
                          ('GateB-Realism',(23,4,-72),(-8,2,-47))]:
        cam=bpy.data.objects['Campus Overview'];cam.location=cv(p)
        cam.rotation_euler=(cv(target)-cam.location).to_track_quat('-Z','Y').to_euler()
        cam.data.type='ORTHO' if name=='Campus-Overview' else 'PERSP'
        cam.data.ortho_scale=450;cam.data.lens=30
        scene.render.filepath=os.path.join(output,name+'.png');bpy.ops.render.render(write_still=True)
print('CAMPUS_ARCHITECTURE_REVISION5_READY '+output,flush=True)
