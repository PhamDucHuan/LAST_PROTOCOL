"""Revision7 ->8: additive infill and waterfront dressing, no source overwrite."""
import bpy, json, math, os, random, shutil, sys
from mathutils import Vector
sys.dont_write_bytecode=True
sys.path.insert(0,os.path.dirname(__file__))
from PlanCampusStreetscape import plan
from PeripheralGeometry import inside, rectangle, overlap, distance_segment, sample_path
source,output=map(os.path.abspath,sys.argv[sys.argv.index('--')+1:][:2])
if os.path.exists(output):raise RuntimeError('Existing candidate preserved.')
with open(os.path.join(os.path.dirname(source),'CanThoCampus.layout.json'),encoding='utf-8') as f:layout=json.load(f)
if layout['artRevision']!=7:raise RuntimeError('Expected revision7.')
infill=plan(layout)
bpy.ops.wm.open_mainfile(filepath=source)
rng=random.Random(20261018);cache={};zone='64_Infill_0'
def cv(p):return Vector((-p[0],-p[2],p[1]))
def xyz(p):return dict(zip('xyz',[round(v,5) for v in p]))
def collection(name):
    c=bpy.data.collections.get(name)
    if c is None:c=bpy.data.collections.new(name);bpy.context.scene.collection.children.link(c)
    return c
def sector(x,z):return str((1 if x>0 else 0)+(2 if z>80 else 0))
def box(name,p,size,mat='Concrete',solid=False,yaw=0):
    key='CT5_'+mat
    if key not in cache:
        bpy.ops.mesh.primitive_cube_add(size=1);o=bpy.context.object;o.data.materials.append(bpy.data.materials[key])
        cache[key]=o.data;bpy.data.objects.remove(o,do_unlink=True)
    o=bpy.data.objects.new(name,cache[key]);collection(zone).objects.link(o)
    o.location=cv(p);o.scale=(size[0],size[2],size[1]);o.rotation_euler.z=-yaw
    if solid:layout['collisions'].append(dict(name=name,position=xyz(p),size=xyz(size),euler=xyz((0,math.degrees(yaw),0))))
    return o
def strip(name,a,b,width,y,mat,height=.04):
    return box(name,((a[0]+b[0])/2,y,(a[1]+b[1])/2),(math.dist(a,b),height,width),mat,yaw=-math.atan2(b[1]-a[1],b[0]-a[0]))

# House fronts face their own local lane; original 167 parcels stay untouched.
for index,h in enumerate(infill['houses'],168):
    x,z,w,d,height=h['x'],h['z'],h['w'],h['d'],h['height'];fx,fz=h['front'];rx,rz=fz,-fx
    yaw=math.atan2(fx,fz);zone='64_Infill_'+sector(x,z);name='District house %03d'%index
    def local(label,p,size,mat,solid=False):
        return box(label,(x+rx*p[0]+fx*p[2],p[1],z+rz*p[0]+fz*p[2]),size,mat,solid,yaw)
    local('Infill parcel paving',(0,.025,1),(w+1,.04,d+4),'Concrete')
    local(name,(0,height/2,0),(w,height,d),'WarmPaint' if index%3 else 'Plaster',True)
    local('Infill plinth',(0,.25,0),(w+.12,.5,d+.12),'Concrete')
    local('Infill roof coping',(0,height+.08,0),(w+.35,.16,d+.35),'SilverCladding')
    local('Infill shop shutter',(0,1.25,d/2+.05),(w*.7,2.5,.12),'BluePaint' if index%2 else 'SilverCladding')
    local('Infill entrance reveal',(-w*.37,1.15,d/2+.13),(.9,2.3,.12),'Metal')
    local('Infill awning',(0,2.9,d/2+.85),(w+.2,.13,1.8),'Terracotta')
    for floor in range(1,round((height-.4)/3.2)):
        y=floor*3.2+1.45
        for dx in [-w*.23,w*.23]:
            for side in [-1,1]:
                local('Infill window frame',(dx,y,side*(d/2+.04)),(w*.33,1.9,.12),'Plaster')
                local('Infill glazing',(dx,y,side*(d/2+.12)),(w*.28,1.6,.06),'Glass')
                local('Infill mullion',(dx,y,side*(d/2+.17)),(.055,1.6,.045),'SilverCladding')
        local('Infill balcony slab',(0,y-.98,d/2+.35),(w+.15,.16,.9),'Concrete')
        local('Infill balcony rail',(0,y-.48,d/2+.76),(w,.08,.08),'Metal')
        for side in [-1,1]:
            local('Infill side window',(side*(w/2+.06),y,0),(.10,1.5,1.3),'Glass')
    local('Infill downpipe',(-w*.45,height/2,d/2+.15),(.09,height,.09),'Metal')
    local('Infill solar array',(0,height+.25,-d*.18),(w*.65,.12,d*.4),'Glass')
    local('Infill roof tank',(w*.28,height+.65,d*.26),(1,1.1,1.1),'SilverCladding')
    layout['neighborhoodHouses'].append(dict(name=name,position=xyz((x,0,z)),size=xyz((w,height,d)),euler=xyz((0,math.degrees(yaw),0)),district=h['district'],front=xyz((fx,0,fz))))

lanes=[]
for i,l in enumerate(infill['lanes']):
    a,b=l['a'],l['b'];zone='64_Infill_'+sector(*b)
    strip('Local lane paving',a,b,6,.032,'Walk')
    strip('Local residential lane',a,b,4,.062,'Asphalt')
    lanes.append(dict(id='local_lane_'+str(i+1),title='Hẻm dân cư mô phỏng '+str(i+1),width=4,points=[xyz((x,0,z)) for x,z in [a,b]]))

campus=[(p['x'],p['z']) for p in layout['campusBoundary']];water=[(p['x'],p['z']) for p in layout['waterBoundary']]
route=[(p['x'],p['z']) for p in layout['neighborhoodRoute']]
lots=[]
for h in layout['neighborhoodHouses']:
    p,s,f=h['position'],h['size'],h['front'];lots.append(rectangle(p['x']+f['x']*1.5,p['z']+f['z']*1.5,s['x']+.4,s['z']+3,(f['x'],f['z'])))
occupied=[];trees=[];benches=[];lamps=[]
def free(x,z,r=.7):
    if not(-543<x<365 and -375<z<435) or inside((x,z),campus) or inside((x,z),water):return False
    poly=rectangle(x,z,r*2,r*2,(0,1))
    if any(overlap(poly,lot,.3) for lot in lots):return False
    if any(distance_segment((x,z),a,b)<r+1.2 for a,b in zip(route,route[1:])):return False
    if any(distance_segment((x,z),l['a'],l['b'])<r+3 for l in infill['lanes']):return False
    if any(math.dist((x,z),(a,b))<r+rr+.7 for a,b,rr in occupied):return False
    # Existing street furniture/food court is retained; use its boxes as reserves.
    for c in layout['collisions'][:895]:
        if c['name'] in ('Campus soil','Bung Xang water exclusion') or c['name'].startswith('District house'):continue
        p,s=c['position'],c['size']
        if abs(x-p['x'])<s['x']/2+r+.5 and abs(z-p['z'])<s['z']/2+r+.5:return False
    return True
card=bpy.data.meshes['Leaf card']
bpy.ops.mesh.primitive_cylinder_add(vertices=8,radius=1,depth=1);o=bpy.context.object;o.data.materials.append(bpy.data.materials['CT5_Bark']);trunk=o.data;bpy.data.objects.remove(o,do_unlink=True)
def tree(x,z):
    if not free(x,z,1):return
    occupied.append((x,z,1));height=rng.uniform(4.8,6.8);zone_name='65_StreetscapeTrees_'+sector(x,z)
    o=bpy.data.objects.new('Peripheral shade tree trunk',trunk);collection(zone_name).objects.link(o);o.location=cv((x,height/2,z));o.scale=(.22,.22,height)
    layout['collisions'].append(dict(name='Peripheral shade tree trunk',position=xyz((x,height/2,z)),size=xyz((.5,height,.5))))
    for i in range(32):
        a=rng.uniform(0,math.tau);r=3*math.sqrt(rng.random());o=bpy.data.objects.new('Peripheral leaf cluster',card);collection(zone_name).objects.link(o)
        o.location=cv((x+math.cos(a)*r,height+rng.uniform(-.5,1.8),z+math.sin(a)*r));o.rotation_euler=(rng.uniform(-.9,.9),rng.uniform(-.7,.7),rng.uniform(0,math.tau));o.scale=(3.1,1,2.7)
    trees.append(xyz((x,0,z)))
def bench(x,z,yaw):
    global zone
    if not free(x,z,1.4):return
    occupied.append((x,z,1.4));zone='66_StreetscapeProps_'+sector(x,z)
    box('Shore bench seat',(x,.48,z),(2,.16,.7),'WarmPaint',True,yaw)
    for dx in [-.75,.75]:
        box('Shore bench leg',(x+math.cos(yaw)*dx,.24,z-math.sin(yaw)*dx),(.12,.48,.5),'Metal',yaw=yaw)
    box('Shore bench back',(x-math.sin(yaw)*.3,.88,z-math.cos(yaw)*.3),(2,.7,.12),'WarmPaint',yaw=yaw)
    benches.append(xyz((x,0,z)))
def lamp(x,z):
    global zone
    if not free(x,z,.4):return
    occupied.append((x,z,.4));zone='66_StreetscapeProps_'+sector(x,z)
    box('Streetscape solar lamp',(x,3,z),(.14,6,.14),'Metal',True)
    box('Streetscape lamp head',(x+.45,5.9,z),(1.2,.16,.5),'Plaster')
    box('Streetscape solar panel',(x,6.2,z),(1.1,.09,1.3),'Glass')
    box('Streetscape status light',(x+.12,1.8,z),(.05,.35,.12),'Status')
    lamps.append(xyz((x,0,z)))
for r in layout['peripheralRoads']:
    points=[(p['x'],p['z']) for p in r['points']]
    for p,t in sample_path(points,24):
        for side in [-1,1]:
            n=(-t[1]*side,t[0]*side);offset=r['width']/2+1.8
            tree(p[0]+n[0]*offset,p[1]+n[1]*offset)
    for p,t in sample_path(points,45):
        n=(-t[1],t[0]);offset=r['width']/2+.7;lamp(p[0]+n[0]*offset,p[1]+n[1]*offset)
        # Flat drains do not add tiny CharacterController obstacles.
        zone='66_StreetscapeProps_'+sector(*p)
        x,z=p[0]-n[0]*(r['width']/2-.3),p[1]-n[1]*(r['width']/2-.3)
        box('Road drain surround',(x,.09,z),(.7,.03,.9),'Concrete',yaw=-math.atan2(t[1],t[0]))
        for k in range(5):box('Road drain grate',(x+t[0]*(k-2)*.12,.11,z+t[1]*(k-2)*.12),(.05,.025,.7),'Metal',yaw=-math.atan2(t[1],t[0]))

cx=sum(x for x,z in water)/len(water);cz=sum(z for x,z in water)/len(water)
for a,b in zip(water,water[1:]+water[:1]):
    dx,dz=b[0]-a[0],b[1]-a[1];length=math.hypot(dx,dz);nx,nz=dz/length,-dx/length
    if nx*((a[0]+b[0])/2-cx)+nz*((a[1]+b[1])/2-cz)<0:nx,nz=-nx,-nz
    for t in range(10,int(length)-3,17):
        x=a[0]+dx*t/length;z=a[1]+dz*t/length
        tree(x+nx*9,z+nz*9)
        if t%2:bench(x+nx*5.7,z+nz*5.7,math.atan2(nx,nz))
        else:lamp(x+nx*5.9,z+nz*5.9)
    # Dark edging sits under the existing railing; no water geometry is changed.
    zone='66_StreetscapeProps_'+sector(*a)
    strip('Shore stone edging',a,b,.45,.12,'Concrete',.24)

# Small outdoor utility cabinets and recycling points beside benches, with collision.
for p in benches[::3]:
    x,z=p['x']+2.5,p['z']
    if not free(x,z,.65):continue
    occupied.append((x,z,.65));zone='66_StreetscapeProps_'+sector(x,z)
    box('Shore recycling cabinet',(x,.65,z),(.9,1.3,.7),'BluePaint',True)
    box('Recycling aperture',(x,1.05,z+.36),(.6,.15,.03),'Metal')

layout['artRevision']=8;layout['localLanes']=lanes
layout['streetscapeTrees']=trees;layout['streetscapeBenches']=benches;layout['streetscapeLamps']=lamps
layout['streetscapeRevision']=dict(originalHouses=167,addedHouses=len(infill['houses']),localLanes=len(lanes),trees=len(trees),benches=len(benches),lamps=len(lamps),
    note='Local lanes and parcels are fictional gameplay infill, not surveyed real addresses. Referenced main roads, campus, waterfront and existing parcels retained.')
layout['note']='Revision8: additive neighborhood infill, street shade and lakeside amenities over the revision7 reference-based road layout. Compressed fictional reconstruction.'
if len(trees)<60 or len(benches)<12:raise RuntimeError('Insufficient safe street furniture.')
os.makedirs(output);shutil.copytree(os.path.join(os.path.dirname(source),'Textures'),os.path.join(output,'Textures'))
with open(os.path.join(output,'CanThoCampus.layout.json'),'w',encoding='utf-8') as f:json.dump(layout,f,ensure_ascii=False,indent=2)
bpy.context.view_layer.update();bpy.ops.wm.save_as_mainfile(filepath=os.path.join(output,'CanThoCampus.blend'),relative_remap=False)
from FinishCanThoCampus import finish
finish(output,False)
print('STREETSCAPE_REVISION8_PASS '+json.dumps(layout['streetscapeRevision']),flush=True)
