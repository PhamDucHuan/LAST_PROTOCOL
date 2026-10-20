"""Reference-informed CTU route, revision 3. Original meshes, no imported map tiles.
Input is the fenced revision 2 source. Writes a NEW source/layout/FBX directory.
Unity coordinates: X along the street, Z towards the campus interior.
"""
import bpy, bmesh, json, math, os, random, sys
from mathutils import Vector
random.seed(1966)
args = sys.argv[sys.argv.index('--') + 1:]
source, output = map(os.path.abspath, args[:2])
if os.path.exists(output): raise RuntimeError('Output exists; choose a new directory.')
with open(os.path.join(os.path.dirname(source), 'CanThoCampus.layout.json'), encoding='utf-8') as f: layout = json.load(f)
if layout.get('artRevision') != 2 or layout.get('campusFenceRevision') != 1:
    raise RuntimeError('Expected fenced revision 2 source and matching layout.')
bpy.ops.wm.open_mainfile(filepath=source)
scene = bpy.context.scene
zone = '36_CampusRoute'; cache = {}; mats = {m.name: m for m in bpy.data.materials}
def cv(p): return Vector((-p[0], -p[2], p[1]))
def xyz(p): return dict(x=p[0], y=p[1], z=p[2])
def material(name, color, roughness=.7, metallic=0, emission=0):
    m = bpy.data.materials.new('CT3_' + name); m.diffuse_color = (*color, 1); m.use_nodes = True
    node = m.node_tree.nodes.get('Principled BSDF'); node.inputs['Base Color'].default_value = (*color, 1)
    node.inputs['Roughness'].default_value = roughness; node.inputs['Metallic'].default_value = metallic
    node.inputs['Emission Color'].default_value = (*color, 1); node.inputs['Emission Strength'].default_value = emission
    mats[name] = m
    layout['materials'].append(dict(name=m.name, color=dict(r=color[0],g=color[1],b=color[2],a=1), roughness=roughness, metallic=metallic, emission=emission))
for entry in [('Plaster',(.68,.7,.64),.85,0), ('FrameBlue',(.24,.34,.43),.72,.1),
              ('Glazing',(.23,.38,.4),.27,.35), ('Aluminium',(.43,.51,.5),.44,.6),
              ('Walk',(.46,.43,.37),.9,0), ('RedPaver',(.38,.25,.2),.9,0),
              ('Stone',(.37,.34,.27),.94,0), ('LeafDeep',(.06,.15,.055),.95,0),
              ('LeafMid',(.12,.24,.075),.95,0), ('LeafSun',(.24,.34,.11),.95,0)]: material(*entry)
for key in ['White','Concrete','Ceramic','Hull','Metal','Cyan','Bark','Grass','GrassLight','Yellow','Screen','WarmLight','Teal','Roof']:
    name='CT2_'+key; existing=bpy.data.materials.get(name)
    if existing is None:
        entry=next(e for e in layout['materials'] if e['name']==name); color=entry['color']
        existing=bpy.data.materials.new(name); existing.diffuse_color=(color['r'],color['g'],color['b'],1); existing.use_nodes=True
        node=existing.node_tree.nodes.get('Principled BSDF'); node.inputs['Base Color'].default_value=existing.diffuse_color
        node.inputs['Roughness'].default_value=entry['roughness']; node.inputs['Metallic'].default_value=entry['metallic']
        node.inputs['Emission Color'].default_value=existing.diffuse_color; node.inputs['Emission Strength'].default_value=entry['emission']
    mats[key] = existing
def link(obj):
    collection = bpy.data.collections.get(zone)
    if collection is None: collection=bpy.data.collections.new(zone); scene.collection.children.link(collection)
    collection.objects.link(obj); return obj
def collider(name,p,s): layout['collisions'].append(dict(name=name,position=xyz(p),size=xyz(s)))
def box(name,p,s,mat='Plaster',solid=False):
    key=('box',mat)
    if key not in cache:
        bpy.ops.mesh.primitive_cube_add(size=1); temporary=bpy.context.object
        temporary.data.materials.append(mats[mat]); cache[key]=temporary.data; bpy.data.objects.remove(temporary,do_unlink=True)
    obj=link(bpy.data.objects.new(name,cache[key])); obj.location=cv(p); obj.scale=(s[0],s[2],s[1])
    if solid: collider(name,p,s)
    return obj
def ico(name,p,s,mat='LeafMid',smooth=True):
    key=('ico',mat,smooth)
    if key not in cache:
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,radius=1); temporary=bpy.context.object
        temporary.data.materials.append(mats[mat])
        for poly in temporary.data.polygons: poly.use_smooth=smooth
        cache[key]=temporary.data; bpy.data.objects.remove(temporary,do_unlink=True)
    obj=link(bpy.data.objects.new(name,cache[key])); obj.location=cv(p); obj.scale=(s[0],s[2],s[1]); return obj
def beam(name,a,b,r=.08,mat='Metal'):
    key=('cylinder',mat)
    if key not in cache:
        bpy.ops.mesh.primitive_cylinder_add(vertices=8,radius=1,depth=1); temporary=bpy.context.object
        temporary.data.materials.append(mats[mat]); cache[key]=temporary.data; bpy.data.objects.remove(temporary,do_unlink=True)
    pa,pb=cv(a),cv(b); vector=pb-pa; obj=link(bpy.data.objects.new(name,cache[key])); obj.location=(pa+pb)*.5
    obj.scale=(r,r,vector.length); obj.rotation_euler=vector.to_track_quat('Z','Y').to_euler(); return obj
def clear(name):
    for obj in list(bpy.data.collections[name].objects): bpy.data.objects.remove(obj,do_unlink=True)
def replace(name,p,s):
    entry=next(c for c in layout['collisions'] if c['name']==name); entry['position']=xyz(p); entry['size']=xyz(s)
font=bpy.data.fonts.get('Arial Bold') or bpy.data.fonts.load('C:/Windows/Fonts/arialbd.ttf')
def sign(text,p,size=.5,mat='FrameBlue',width=50):
    data=bpy.data.curves.new(text,'FONT'); data.body=text; data.font=font; data.size=size; data.align_x='CENTER'; data.extrude=.007; data.resolution_u=2
    obj=link(bpy.data.objects.new(text,data)); obj.location=cv(p); obj.rotation_euler=(math.pi/2,0,math.pi); data.materials.append(mats[mat])
    bpy.context.view_layer.update()
    if obj.dimensions.x>width: obj.scale*=width/obj.dimensions.x
    bpy.ops.object.select_all(action='DESELECT'); obj.select_set(True); bpy.context.view_layer.objects.active=obj; bpy.ops.object.convert(target='MESH')
def arc(name,x,z,y,width,height,mat,bulge=2.6,thickness=.18):
    points=[]; n=28
    for i in range(n+1):
        xx=-width/2+width*i/n; zz=z-bulge*(1-(xx/(width/2))**2)
        points.extend([(x+xx,y,zz),(x+xx,y+height,zz),(x+xx,y,zz+thickness),(x+xx,y+height,zz+thickness)])
    faces=[]
    for i in range(n):
        a=i*4; b=(i+1)*4; faces.extend([(a,b,b+1,a+1),(a+2,a+3,b+3,b+2),(a+1,b+1,b+3,a+3),(a,a+2,b+2,b)])
    faces.extend([(0,1,3,2),(n*4,n*4+2,n*4+3,n*4+1)])
    mesh=bpy.data.meshes.new(name); mesh.from_pydata([cv(p) for p in points],[],faces); mesh.materials.append(mats[mat])
    bm=bmesh.new(); bm.from_mesh(mesh); bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces)); bm.to_mesh(mesh); bm.free(); mesh.update()
    return link(bpy.data.objects.new(name,mesh))

# Administration: blue-grey frames, inset windows/balconies, low side wings.
# Shift the tower four metres east to retain the existing four-player firing lane.
zone='02_Administration'; clear(zone); x,z,h=32,-2,25.6
box('Administration structure',(x,h/2,z),(24,h,22),'Plaster'); replace('NHÀ ĐIỀU HÀNH',(x,h/2,z),(24,h,22))
for side in [-1,1]:
    front=z+side*11.08
    for floor in range(8):
        y=1.5+floor*3.1
        for xx in [-9,-6,-3,0,3,6,9]:
            box('Recessed framed window',(x+xx,y,front),(2.35,2.1,.16),'Glazing')
            for offset in [-.65,0,.65]: box('Window aluminium mullion',(x+xx+offset,y,front+side*.1),(.055,2.12,.065),'Aluminium')
            box('White window sill',(x+xx,y-1.1,front+side*.16),(2.6,.13,.32),'White')
        box('Blue grey floor frame',(x,2.85+floor*3.1,front+side*.22),(24.6,.38,.55),'FrameBlue')
    for xx in [-11.6,-4.6,4.6,11.6]: box('Blue grey vertical frame',(x+xx,h/2,front+side*.38),(.6,h,.7),'FrameBlue')
    for xx in [-7.5,7.5]:
        for floor in range(1,8):
            y=3.1*floor+.65
            box('Balcony shadow recess',(x+xx,y+.5,front+side*.39),(3.6,1.7,.15),'Metal')
            box('Balcony floor',(x+xx,y-.1,front+side*.7),(3.7,.16,1),'Plaster')
            for offset in [-1.5,-1,-.5,0,.5,1,1.5]: box('Balcony guard rail',(x+xx+offset,y+.45,front+side*1.1),(.055,1,.05),'Aluminium')
            box('Balcony handrail',(x+xx,y+.95,front+side*1.1),(3.6,.06,.065),'White')
for side in [-1,1]:
    for floor in range(8):
        for zz in [-8,-4,0,4,8]: box('Side framed window',(x+side*12.08,1.5+floor*3.1,z+zz),(.16,2.1,2.65),'Glazing')
        box('Side blue floor edge',(x+side*12.22,2.85+floor*3.1,z),(.5,.38,22),'FrameBlue')
    for zz in [-10,-3,3,10]: box('Side vertical frame',(x+side*12.3,h/2,z+zz),(.6,h,.6),'FrameBlue')
for xx in [-10,-3,3,10]: box('Rooftop open frame',(x+xx,h+2,z),(.5,4,.6),'FrameBlue')
box('Rooftop lintel',(x,h+4,z),(21,.5,.6),'FrameBlue')
for xx,w in [(15.5,9),(50.5,13)]:
    box('Administration low wing',(xx,3.6,2),(w,7.2,18),'Plaster',True)
    for yy in [1.8,5]:
        box('Low wing windows',(xx,yy,-7.1),(w-1,1.9,.12),'Glazing')
        box('Low wing blue frame',(xx,yy+1.15,-7.25),(w+.2,.3,.4),'FrameBlue')
        for dx in range(-int(w/2)+1,int(w/2),2): box('Low wing frame',(xx+dx,yy,-7.22),(.12,1.9,.15),'White')
    box('Low wing roof coping',(xx,7.3,2),(w+.6,.25,18.6),'White')
box('Administration front portico',(32,3.8,-16),(29,.45,5),'White',True)
for xx in [20,26,38,44]: box('Administration portico pier',(xx,1.8,-17.8),(.5,3.6,.6),'FrameBlue',True)
sign('NHÀ ĐIỀU HÀNH ĐẠI HỌC CẦN THƠ',(32,4.15,-18.28),.58,width=27)
box('Retrofit canopy seam',(32,3.55,-18.3),(26,.07,.06),'Cyan')
for xx in [-10,10]: box('Subtle vertical service seam',(32+xx,12.5,-13.48),(.07,23,.07),'Cyan')
for xx in [-10,-5,0,5,10]:
    beam('Ceremonial flag pole',(32+xx,4.05,-16.3),(32+xx,6.2,-16.3),.025,'Aluminium')
    box('Ceremonial pennant',(32+xx+.2,5.8,-16.3),(.4,.7,.035),'Cyan' if xx==0 else 'Roof')

# Library: metal crown, staggered cladding, vertical and horizontal sun screens.
zone='03_LearningResource'; clear(zone); x,z=57,-25
box('LRC primary volume',(x,6.75,z),(27,13.5,22),'Plaster')
for floor in range(3):
    yy=1.1+floor*3.5
    arc('Library curved glazing',x,-36.1,yy,27,2.6,'Glazing')
    arc('Library floor edge',x,-36.4,yy+2.7,27.6,.35,'Aluminium',thickness=.4)
    for dx in range(-12,13,3):
        zz=-36.1-2.6*(1-(dx/13.5)**2)
        box('Library mullion',(x+dx,yy+1.3,zz-.13),(.11,2.6,.18),'Aluminium')
        if abs(dx)>5:
            for offset in [-.75,-.4,0,.4,.75]: box('Library vertical solar screen',(x+dx+offset,yy+1.3,zz-.36),(.065,2.4,.25),'Aluminium')
    for yy2 in [yy+.25,yy+.5,yy+.75]: arc('Library horizontal sun shade',x,-36.55,yy2,27.6,.07,'Aluminium',thickness=.45)
arc('Library silver curved crown',x,-36.5,10.9,28,3.3,'Aluminium',thickness=.6)
for yy in [11.4,12.05,12.7,13.35]: arc('Silver cladding horizontal joint',x,-36.52,yy,28,.018,'FrameBlue',thickness=.62)
for row in range(4):
    for dx in range(-12,13,3):
        offset=.75 if row%2 else 0; xx=dx+offset
        if abs(xx)>13: continue
        zz=-36.53-2.6*(1-(xx/14)**2)
        box('Staggered cladding vertical joint',(x+xx,11.2+row*.65,zz-.02),(.018,.59,.04),'FrameBlue')
for side in [-1,1]:
    for floor in range(3): box('Library side glazing',(x+side*13.58,2.4+floor*3.5,z),(.15,2.6,20),'Glazing')
    for zz in range(-9,10,2): box('Library side sun screen',(x+side*13.8,5.4,z+zz),(.45,9.5,.08),'Aluminium')
box('Library east name blade',(67,8,-38),(4,12,.5),'Plaster')
sign('ĐẠI HỌC',(67,12,-38.28),.44,'FrameBlue',3.9)
sign('CẦN THƠ',(67,11.25,-38.28),.44,'FrameBlue',3.9)
sign('TRUNG TÂM',(67,9.5,-38.28),.36,'FrameBlue',3.9)
sign('HỌC LIỆU',(67,8.85,-38.28),.44,'FrameBlue',3.9)
box('Library entry canopy',(57,3.7,-40.2),(18,.3,3.6),'Aluminium',True)
for xx in [49,65]: box('Library entry pier',(xx,1.8,-41.7),(.4,3.6,.4),'White',True)
box('Library service seam',(57,3.52,-42.03),(16,.06,.05),'Cyan')
sign('MEKONG / ARCHIVE ACCESS',(57,4,-42.04),.36,'FrameBlue',16)

# Keep gates/fences as previously authored; replace only generic red guard roofs
# with the white flat canopy profile visible in the gate reference.
zone='01_Gates'
for obj in list(bpy.data.collections[zone].objects):
    if obj.name.startswith('Guardhouse roof') or obj.name.startswith('Gate cap'): bpy.data.objects.remove(obj,do_unlink=True)
for gate in [-77,0,77]:
    for side in [-1,1]:
        box('Guardhouse flat white canopy',(gate+side*14,4.05,-46),(6.5,.5,6.5),'White')
        box('Guardhouse window sill',(gate+side*14,1.32,-48.7),(4,.18,.4),'FrameBlue')
        for dx in [-1.2,0,1.2]: box('Guardhouse grille',(gate+side*14+dx,2,-48.68),(.065,1.2,.12),'Aluminium')

zone='36_CampusRoute'
# Pavers and flush surface seams stay visual; the continuous ground provides
# player collision. Raised walls, trunks and furniture have explicit colliders.
def walk(a,b,width=3.2,mat='Walk'):
    dx,dz=b[0]-a[0],b[1]-a[1]; length=math.hypot(dx,dz); yaw=math.atan2(dx,dz)
    obj=box('Pedestrian route pavers',((a[0]+b[0])/2,.11,(a[1]+b[1])/2),(width,.05,length),mat)
    obj.rotation_euler.z=-yaw
    for i in range(1,math.ceil(length/2)):
        d=i*2; xx=a[0]+dx*d/length; zz=a[1]+dz*d/length
        seam=box('Paving expansion joint',(xx,.139,zz),(width,.009,.025),'Concrete'); seam.rotation_euler.z=-yaw
route=[(0,-54),(0,-40),(0,-24),(12,-24),(32,-21),(40,-22),(40,-44),(57,-42)]
for a,b in zip(route[2:],route[3:]): walk(a,b)
# Wide, flat arrival forecourt and curb edges; retain the open 0..10 combat strip.
box('Administration forecourt',(30,.081,-20),(28,.06,12),'RedPaver')
for xx in [11,52]:
    for zz in [-36,-28,-20]: box('Sidewalk curb',(xx,.12,zz),(.18,.24,5),'Concrete')
box('Library forecourt',(57,.08,-42),(26,.06,6),'Walk')
for zz in [-46.4,-37.2]: box('Library curb edge',(57,.12,zz),(25,.24,.16),'Concrete')
# The road-facing commemorative stone sits on a planted island, off the route.
ico('Administration commemorative stone',(20,1.1,-27),(3.5,1.25,.9),'Stone',False)
collider('Campus route commemorative stone',(20,1.15,-27),(6.8,2.3,1.8))
sign('ĐẠI HỌC CẦN THƠ',(20,1.65,-27.92),.44,'White',6)
sign('CANTHO UNIVERSITY  /  31.03.1966',(20,.97,-27.93),.22,'White',5.9)
box('Monument planted island',(20,.13,-27),(9,.2,3),'GrassLight')
for xx in [16,24]: ico('Low monument shrub',(xx,.5,-27),(1.1,.5,.8),'LeafMid')
def shade_tree(x,z,h=9,radius=4):
    beam('Shade tree trunk',(x,0,z),(x,h-.8,z),.24,'Bark'); collider('Campus route tree trunk',(x,h/2,z),(.58,h,.58))
    for side in [-1,1]: beam('Shade tree branch',(x,h*.55,z),(x+side*radius*.55,h-.2,z+.6),.1,'Bark')
    for i in range(12):
        angle=i*math.tau/12+random.random()*.2; r=radius*random.uniform(.2,.65)
        ico('Layered broadleaf canopy',(x+math.cos(angle)*r,h+random.uniform(-.5,1.2),z+math.sin(angle)*r),
            (radius*.5,random.uniform(1,1.65),radius*.46),['LeafDeep','LeafMid','LeafSun'][i%3])
    box('Flush tree grate',(x,.09,z),(1.2,.06,1.2),'Concrete')
for x,z in [(12,-40),(25,-43),(36,-40),(88,-42),(94,-27),(97,-8),(86,15),(-17,-10),(-23,-37),(-20,20),(-20,40),(43,28),(62,31)]:
    shade_tree(x,z,random.uniform(8,10.5),random.uniform(3.4,4.5))
# Background groves along empty green verges, estimated rather than surveyed.
for x,z in [(-115,-35),(-130,-35),(-145,-35),(-160,-35),(-180,-35),(120,-32),(140,-32),(160,-32),(180,-32),
            (-110,40),(-130,40),(150,38),(175,38),(200,38)]: shade_tree(x,z,random.uniform(9,12),4.6)
def bench(x,z):
    box('Route bench',(x,.51,z),(2.4,.18,.65),'Stone',True)
    for xx in [-.8,.8]: box('Bench concrete foot',(x+xx,.25,z),(.28,.5,.65),'Concrete')
    box('Route bench back',(x,.95,z+.3),(2.4,.7,.12),'Bark')
for p in [(29,-29),(35,-29),(89,-17),(-18,-34)]: bench(*p)
for x,z in [(8,-23),(37,-32),(72,-42)]:
    box('Route wayfinding post',(x,1.35,z),(.28,2.7,.28),'FrameBlue',True)
    box('Route wayfinding board',(x,2.4,z),(3,.8,.14),'FrameBlue')
    sign('HỌC LIỆU  →' if z!=-42 else 'AI CORE / 03',(x,2.28,z-.08),.32,'White',2.8)
    box('Route marker light',(x,1.7,z-.18),(.09,.6,.05),'Cyan')
def parking(x,z):
    box('Cycle shelter canopy',(x,2.8,z),(20,.22,5.6),'Aluminium',True)
    for dx in [-9,-3,3,9]: box('Cycle shelter column',(x+dx,1.4,z+1.8),(.16,2.8,.16),'FrameBlue',True)
    for dx in range(-8,9,2):
        beam('Cycle rack',(x+dx,.08,z),(x+dx,.85,z),.045,'Aluminium')
        # Compact parked scooters: static environmental props, no vehicles code.
        box('Parked scooter chassis',(x+dx,.5,z-1.3),(.55,.6,1.5),'FrameBlue')
        box('Scooter saddle',(x+dx,.86,z-1.2),(.5,.15,.75),'Metal')
        beam('Scooter steering',(x+dx,.7,z-1.9),(x+dx,1.2,z-1.9),.055,'Aluminium')
        beam('Scooter handlebar',(x+dx-.35,1.2,z-1.9),(x+dx+.35,1.2,z-1.9),.04,'Metal')
    collider('Campus route parked scooters',(x,.65,z-1.3),(18,1.3,2))
parking(-37,-43); parking(94,-12)
# Future features are small retrofits, leaving the real architectural silhouette.
for x,z in [(38,-31),(86,-23)]:
    box('MEKONG emergency power cabinet',(x,.85,z),(1.3,1.7,.7),'Hull',True)
    box('Power status display',(x,1.2,z-.37),(.9,.45,.06),'Screen')
    box('Power cabinet status',(x,1.2,z-.41),(.6,.06,.02),'Cyan')
    sign('MEKONG',(x,1.55,z-.4),.17,'White',1.1)
for z in [-39,-33,-27]: box('Gate B guidance inset',(-4.8,.13,z),(.08,.06,2),'Cyan')
for landmark in layout['landmarks']:
    if landmark['id']=='02_Administration': landmark['position']=xyz((32,0,-2)); landmark['approach']=xyz((32,0,-19))
layout['artRevision']=3; layout['playableRoute']=[xyz((x,0,z)) for x,z in route]
layout['navigationChecks'].extend(layout['playableRoute'])
layout['note']='Google Maps satellite and official CTU photographs inform the Gate B / administration / LRC route. Estimated, compressed game geometry; fictional future retrofit.'
os.makedirs(output)
with open(os.path.join(output,'CanThoCampus.layout.json'),'w',encoding='utf-8') as f: json.dump(layout,f,ensure_ascii=False,indent=2)
bpy.context.view_layer.update(); bpy.ops.wm.save_as_mainfile(filepath=os.path.join(output,'CanThoCampus.blend'))
sys.path.insert(0,os.path.dirname(__file__))
sys.dont_write_bytecode=True
from FinishCanThoCampus import finish
finish(output,'--render' in args)
