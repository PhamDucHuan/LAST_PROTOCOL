"""Revision6 ->7: redraw periphery from an authored Google Maps reference plan.

Reuse editable houses, preserve campus, export a new candidate only. No web tiles,
real business logos, downloaded models, Unity packages or account actions.
"""
import bpy,bmesh,json,math,os,random,shutil,sys
from mathutils import Matrix,Vector
sys.dont_write_bytecode=True
source,plan_path,output=map(os.path.abspath,sys.argv[sys.argv.index('--')+1:][:3])
if os.path.exists(output):raise RuntimeError('Existing candidate preserved.')
with open(os.path.join(os.path.dirname(source),'CanThoCampus.layout.json'),encoding='utf-8') as f:layout=json.load(f)
with open(plan_path,encoding='utf-8') as f:plan=json.load(f)
if layout['artRevision']!=6:raise RuntimeError('Expected matching revision6 source/layout.')
sys.path.insert(0,os.path.dirname(__file__))
from PeripheralGeometry import inside,distance_segment,polygon_near_segment,overlap,rectangle,sample_path
bpy.ops.wm.open_mainfile(filepath=source)
random.seed(20261017)
def cv(p):return Vector((-p[0],-p[2],p[1]))
def xyz(p):return dict(zip('xyz',[round(v,5) for v in p]))
campus=[(p['x'],p['z']) for p in layout['campusBoundary']]
water=plan['waterBoundary'];roads=plan['roads'];old_houses=layout['neighborhoodHouses']
segments=[(r,a,b) for r in roads for a,b in zip(r['points'],r['points'][1:])]

# Decide parcel placement before changing any object. Include awning clearance in
# lot footprints, use oriented separating axes, and reserve every street corridor.
candidate_slots={r['id']:sample_path(r['points'],12) for r in roads}
for r in roads:
    random.shuffle(candidate_slots[r['id']])
parcels=[];new_houses=[];placements=[]
def place(h,r):
    w=h['size']['x'];depth=min(h['size']['z'],18)
    slots=candidate_slots[r['id']]
    while slots:
        point,tangent=slots.pop();tx,tz=tangent
        for side in [-1,1]:
            nx,nz=-tz*side,tx*side
            offset=r['width']/2+3+depth/2+3
            x,z=point[0]+nx*offset,point[1]+nz*offset;front=(-nx,-nz)
            poly=rectangle(x+front[0]*1.5,z+front[1]*1.5,w+.4,depth+3,front)
            if any(p[0]<-545 or p[0]>365 or p[1]<-375 or p[1]>435 for p in poly):continue
            if any(inside(p,campus) or inside(p,water) for p in poly):continue
            if any(polygon_near_segment(poly,a,b,3) for a,b in zip(campus,campus[1:]+campus[:1])):continue
            if any(polygon_near_segment(poly,a,b,3) for a,b in zip(water,water[1:]+water[:1])):continue
            if any(polygon_near_segment(poly,a,b,road['width']/2+2) for road,a,b in segments):continue
            if any(overlap(poly,other,1.1) for other in parcels):continue
            yaw=math.atan2(front[0],front[1]);parcels.append(poly)
            new_houses.append(dict(name=h['name'],position=xyz((x,0,z)),size=xyz((w,h['size']['y'],depth)),
                                   euler=xyz((0,math.degrees(yaw),0)),district=r['id'],front=xyz((front[0],0,front[1]))))
            placements.append((h,x,z,depth,yaw,r['id']));return True
    return False

assigned={r['id']:0 for r in roads};index=0
while index<len(old_houses):
    advanced=False
    for r in roads:
        if index>=len(old_houses):break
        if assigned[r['id']]>=r['quota']:continue
        if place(old_houses[index],r):assigned[r['id']]+=1;index+=1;advanced=True
    if not advanced:break
while index<len(old_houses):
    advanced=False
    for r in roads:
        if place(old_houses[index],r):assigned[r['id']]+=1;index+=1;advanced=True;break
    if not advanced:raise RuntimeError('Only '+str(index)+' safe parcels available; original source preserved.')
print('PERIPHERAL_PARCELS '+json.dumps(assigned),flush=True)

def collection(name):
    c=bpy.data.collections.get(name)
    if c is None:c=bpy.data.collections.new(name);bpy.context.scene.collection.children.link(c)
    return c

# Snapshot matrices once: avoid rebuilding the dependency graph for every house.
house_objects=[]
for c in list(bpy.data.collections):
    if c.name.startswith(('51_Neighborhood','52_Neighborhood','53_Neighborhood')):
        house_objects.extend((o,o.matrix_world.copy()) for o in c.objects)
groups={h['name']:[] for h in old_houses}
def footprint_distance(x,z,h):
    dx=max(abs(x-h['position']['x'])-h['size']['x']/2,0)
    dz=max(abs(z-h['position']['z'])-h['size']['z']/2,0)
    return dx*dx+dz*dz
for obj,matrix in house_objects:
    x,z=-matrix.translation.x,-matrix.translation.y
    h=min(old_houses,key=lambda h:footprint_distance(x,z,h))
    groups[h['name']].append((obj,matrix))
old_house_collisions={h['name']:[] for h in old_houses}
retained=[]
removed_prefixes=('Food stall counter','Outdoor food table','Outdoor stool','Neighborhood emergency cabinet',
                  'Emergency supply crate','Neighborhood streetlight pole','Alley wayfinding pillar')
for c in layout['collisions']:
    if c['name'].startswith(('District house','Awning support')):
        p=c['position'];h=min(old_houses,key=lambda h:footprint_distance(p['x'],p['z'],h))
        old_house_collisions[h['name']].append(c)
    elif not c['name'].startswith(removed_prefixes):retained.append(c)
for name in ['50_NeighborhoodRoads','55_NeighborhoodServices']:
    bpy.data.batch_remove(ids=tuple(bpy.data.collections[name].objects))
layout['collisions']=retained
for h,x,z,depth,yaw,district in placements:
    old=h['position'];factor=depth/h['size']['z']
    transform=Matrix.Translation(cv((x,0,z))) @ Matrix.Rotation(-yaw,4,'Z') @ Matrix.Diagonal((1,factor,1,1)) @ Matrix.Translation(-cv((old['x'],0,old['z'])))
    chunk=max(0,min(2,int((x+300)/220))) if district=='three_february' else max(0,min(2,int((z+150)/230)))
    target=collection('60_Geo_'+district+'_'+str(chunk))
    for obj,matrix in groups[h['name']]:
        obj.matrix_world=transform@matrix
        for c in list(obj.users_collection):c.objects.unlink(obj)
        target.objects.link(obj)
    for c in old_house_collisions[h['name']]:
        p=c['position'];v=transform@cv((p['x'],p['y'],p['z']))
        c['position']=xyz((-v.x,v.z,-v.y));c['size']['z']*=factor;c['euler']=xyz((0,math.degrees(yaw),0))
        layout['collisions'].append(c)
print('PERIPHERAL_HOUSES_MOVED '+str(len(house_objects)),flush=True)

# Continuous ground, new world limits; preserve actual campus fence/gates.
layout['bounds']=plan['bounds']
ground=bpy.data.objects['Regional ground'];ground.location=cv((-90,-.25,30));ground.scale=(940,840,.5)
boundaries={'West retaining wall':((-559,1,30),(2,2,840)), 'East retaining wall':((379,1,30),(2,2,840)),
            'North retaining wall':((-90,1,449),(940,2,2)), 'South retaining wall':((-90,1,-389),(940,2,2))}
for c in layout['collisions']:
    if c['name']=='Campus soil':c.update(position=xyz((-90,-.25,30)),size=xyz((940,.5,840)))
    if c['name'] in boundaries:
        p,s=boundaries[c['name']];c.update(position=xyz(p),size=xyz(s));obj=bpy.data.objects[c['name']]
        obj.location=cv(p);obj.scale=(s[0],s[2],s[1])
cache={};zone='61_PeripheralRoads'
def box(name,p,size,mat='Concrete',solid=False,yaw=0):
    material=mat if mat.startswith('CT') else 'CT5_'+mat
    if material not in cache:
        bpy.ops.mesh.primitive_cube_add(size=1);o=bpy.context.object;o.data.materials.append(bpy.data.materials[material])
        cache[material]=o.data;bpy.data.objects.remove(o,do_unlink=True)
    obj=bpy.data.objects.new(name,cache[material]);collection(zone).objects.link(obj)
    obj.location=cv(p);obj.scale=(size[0],size[2],size[1]);obj.rotation_euler.z=-yaw
    if solid:layout['collisions'].append(dict(name=name,position=xyz(p),size=xyz(size),euler=xyz((0,math.degrees(yaw),0))))
    return obj
def strip(name,a,b,width,y,mat,solid=False,height=.04):
    dx,dz=b[0]-a[0],b[1]-a[1];length=math.hypot(dx,dz)
    # A strip's long local axis is Unity X.
    yaw=-math.atan2(dz,dx)
    return box(name,((a[0]+b[0])/2,y,(a[1]+b[1])/2),(length,height,width),mat,solid,yaw)

for r in roads:
    for a,b in zip(r['points'],r['points'][1:]):
        strip('Peripheral '+r['id']+' walk',a,b,r['width']+6,.026,'Walk')
        strip('Peripheral '+r['id']+' road',a,b,r['width'],.055,'Asphalt')
        length=math.dist(a,b);dx,dz=(b[0]-a[0])/length,(b[1]-a[1])/length
        if r['width']>=12:
            for t in range(8,int(length)-5,10):
                strip('Peripheral lane marking',(a[0]+dx*t,a[1]+dz*t),(a[0]+dx*(t+3),a[1]+dz*(t+3)),.12,.084,'Plaster')
for h in new_houses:
    p,s=h['position'],h['size']
    box('Residential parcel apron',(p['x'],.02,p['z']),(s['x']+1,.035,s['z']+5),'Concrete',yaw=math.radians(h['euler']['y']))

# Static water is a polygon, with thin interior strips only for navigation. A
# visible, continuous rail keeps characters safely on the outside promenade.
zone='62_BungXangWaterfront'
if 'CT2_Water' not in bpy.data.materials:
    entry=next(m for m in layout['materials'] if m['name']=='CT2_Water')
    material=bpy.data.materials.new('CT2_Water');material.use_nodes=True
    color=entry['color'];material.diffuse_color=tuple(color[k] for k in 'rgba')
    node=material.node_tree.nodes.get('Principled BSDF');node.inputs['Base Color'].default_value=material.diffuse_color
    node.inputs['Roughness'].default_value=entry['roughness'];node.inputs['Metallic'].default_value=entry['metallic']
data=bpy.data.meshes.new('Bung Xang water surface');data.from_pydata([cv((x,.032,z)) for x,z in water],[],[tuple(range(len(water)))])
data.materials.append(bpy.data.materials['CT2_Water']);bm=bmesh.new();bm.from_mesh(data)
bmesh.ops.triangulate(bm,faces=list(bm.faces));bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.normal_update()
bmesh.ops.reverse_faces(bm,faces=[f for f in bm.faces if f.normal.z<0]);bm.to_mesh(data);bm.free();data.update()
collection(zone).objects.link(bpy.data.objects.new('Bung Xang water surface',data))
for z in range(77,379,4):
    xs=[]
    for a,b in zip(water,water[1:]+water[:1]):
        if (a[1]>z)!=(b[1]>z):xs.append(a[0]+(z-a[1])*(b[0]-a[0])/(b[1]-a[1]))
    if len(xs)==2 and max(xs)-min(xs)>2:
        layout['collisions'].append(dict(name='Bung Xang water exclusion',position=xyz(((max(xs)+min(xs))/2,.75,z)),size=xyz((max(xs)-min(xs)-1,1.5,4))))
cx=sum(p[0] for p in water)/len(water);cz=sum(p[1] for p in water)/len(water)
for i,(a,b) in enumerate(zip(water,water[1:]+water[:1])):
    dx,dz=b[0]-a[0],b[1]-a[1];length=math.hypot(dx,dz);nx,nz=dz/length,-dx/length
    if nx*((a[0]+b[0])/2-cx)+nz*((a[1]+b[1])/2-cz)<0:nx,nz=-nx,-nz
    aa=(a[0]+nx*3,a[1]+nz*3);bb=(b[0]+nx*3,b[1]+nz*3)
    strip('Bung Xang promenade',aa,bb,4.8,.07,'Walk')
    rail_a=(a[0]+nx*.55,a[1]+nz*.55);rail_b=(b[0]+nx*.55,b[1]+nz*.55)
    # Collision follows a visible railing rather than an unmarked map edge.
    for y in [.45,.78,1.1]:strip('Waterfront guard rail',rail_a,rail_b,.08,y,'Metal',y==.78,.08 if y!=.78 else 1.1)
    for t in range(0,int(length)+1,3):
        f=min(1,t/length);x=rail_a[0]+(rail_b[0]-rail_a[0])*f;z=rail_a[1]+(rail_b[1]-rail_a[1])*f
        box('Waterfront rail post',(x,.58,z),(.10,1.16,.10),'Metal')

# Food court and near-future props are on a land pocket east of the water.
zone='63_PeripheralServices'
box('Lakeside food court paving',(-226,.06,280),(30,.10,40),'RedPaver')
for z in [267,280,293]:
    box('Lakeside food stall',(-235,.5,z),(3,1,2),'Plaster',True)
    box('Lakeside stall canopy',(-235,2.8,z),(5,.15,4),'Terracotta')
    for dx in [-2,2]:box('Lakeside stall post',(-235+dx,1.4,z),(.1,2.8,.1),'Metal')
    for dz in [-1.5,1.5]:
        box('Lakeside table',(-220,.75,z+dz),(2,.12,.8),'WarmPaint',True)
        for dd in [-.8,.8]:box('Lakeside stool',(-220,.24,z+dz+dd),(.45,.48,.45),'BluePaint',True)
for x,z in [(-246,-18),(-261,116),(-200,248),(208,30),(171,-171),(40,-125)]:
    box('Peripheral smart lamp pole',(x,3.2,z),(.13,6.4,.13),'Metal',True)
    box('Peripheral smart lamp head',(x+.6,6.35,z),(1.5,.15,.55),'Plaster')
    box('Peripheral solar collector',(x,6.65,z),(.9,.08,1.2),'Glass')
for x,z in [(-247,-34),(-207,318),(219,300)]:
    box('Peripheral emergency cabinet',(x,1,z),(1.3,2,.7),'Metal',True)
    box('Peripheral cabinet light',(x,1.35,z+.37),(1,.08,.04),'Status')

font=bpy.data.fonts.load('C:/Windows/Fonts/arialbd.ttf')
def sign(text,p,width):
    data=bpy.data.curves.new(text,'FONT');data.body=text;data.font=font;data.size=.48;data.align_x='CENTER';data.resolution_u=1
    data.materials.append(bpy.data.materials['CT5_Plaster']);obj=bpy.data.objects.new(text,data);collection(zone).objects.link(obj)
    obj.location=cv(p);obj.rotation_euler=(math.pi/2,0,0);bpy.context.view_layer.update()
    if obj.dimensions.x>width:obj.scale*=width/obj.dimensions.x
    bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj;bpy.ops.object.convert(target='MESH')
for text,x,z in [('BÚNG XÁNG / HẺM 51',-240,-49),('MẠC THIÊN TÍCH',10,-84),('TRẦN VĂN HOÀI',-85,-92),('MẬU THÂN',224,-82),('HƯỚNG NGUYỄN VĂN CỪ',-179,404),('BỜ HỒ BÚNG XÁNG',-227,253)]:
    box('Peripheral wayfinding post',(x,1.45,z),(.16,2.9,.16),'Metal',True)
    box('Peripheral street sign',(x,2.5,z),(7,.9,.10),'BluePaint');sign(text,(x,2.25,z+.08),6.6)

layout['artRevision']=7;layout['neighborhoodHouses']=new_houses
layout['peripheralRoads']=[dict(id=r['id'],title=r['title'],width=r['width'],points=[xyz((x,0,z)) for x,z in r['points']]) for r in roads]
layout['waterBoundary']=[xyz((x,0,z)) for x,z in water];layout['waterChecks']=[xyz((x,0,z)) for x,z in plan['waterChecks']]
layout['neighborhoodRoute']=[xyz((x,0,z)) for x,z in plan['route']]
checks=[(x,0,z) for r in roads for x,z in r['points']]
checks += [(-226,0,260),(-226,0,280),(-269,0,110)]
layout['neighborhoodChecks']=[xyz(p) for p in checks]
probe=next(h for h in new_houses if h['district']=='three_february')
p,s,f=probe['position'],probe['size'],probe['front']
layout['neighborhoodCameraProbe']=xyz((p['x']+f['x']*(s['z']/2+2.5),1.6,p['z']+f['z']*(s['z']/2+2.5)))
layout['neighborhoodCameraEuler']=probe['euler']
layout['peripheralRevision']=dict(plan='Design/Maps/CanThoCampusII/PERIPHERAL_PLAN.json',reference=plan['references'],parcelCounts=assigned,
    note=plan['basis'],limits=plan['limits'])
layout['note']='Revision7: peripheral street topology and west/northwest waterfront redrawn after direct Google Maps inspection. Campus schematic retained, distances compressed for gameplay.'
os.makedirs(output);shutil.copytree(os.path.join(os.path.dirname(source),'Textures'),os.path.join(output,'Textures'))
with open(os.path.join(output,'CanThoCampus.layout.json'),'w',encoding='utf-8') as f:json.dump(layout,f,ensure_ascii=False,indent=2)
bpy.context.view_layer.update();bpy.ops.wm.save_as_mainfile(filepath=os.path.join(output,'CanThoCampus.blend'),relative_remap=False)
from FinishCanThoCampus import finish
finish(output,False)
print('PERIPHERAL_REVISION7_PASS',flush=True)
