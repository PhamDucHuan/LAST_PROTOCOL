"""Redraw the AnLe reference landmarks with the existing campus surface library.

Only two editable collections change. Collision envelopes, courtyards, roads,
sports hall, materials and all gameplay layout entries are preserved.
"""
import bpy, bmesh, json, math, os, shutil, sys
from mathutils import Vector
sys.dont_write_bytecode=True
sys.path.insert(0,os.path.dirname(__file__))
from PeripheralGeometry import polygon_near_segment
root,output=map(os.path.abspath,sys.argv[sys.argv.index('--')+1:][:2])
source=os.path.join(root,'ArtSource/Blender/CanThoCampus')
if os.path.exists(output):raise RuntimeError('Choose a new candidate directory.')
with open(os.path.join(source,'CanThoCampus.layout.json'),encoding='utf-8') as f:layout=json.load(f)
assert layout['artRevision']==12
os.makedirs(output);shutil.copytree(os.path.join(source,'Textures'),os.path.join(output,'Textures'))
shutil.copy2(os.path.join(source,'SketchfabIntegration.json'),output)
bpy.ops.wm.open_mainfile(filepath=os.path.join(source,'CanThoCampus.blend'),use_scripts=False,load_ui=False)
targets=['24_TurtleAuditorium','05_Classrooms_A3']
for name in targets:bpy.data.batch_remove(ids=tuple(bpy.data.collections[name].objects))
mats={m.name[4:]:m for m in bpy.data.materials if m.name.startswith('CT5_')}
cache={};zone=targets[0]
def cv(p):return Vector((-p[0],-p[2],p[1]))
def link(o):bpy.data.collections[zone].objects.link(o);return o
def box(name,p,s,mat='Plaster'):
    if mat not in cache:
        data=bpy.data.meshes.new('Redraw unit '+mat)
        data.from_pydata([(-.5,-.5,-.5),(.5,-.5,-.5),(.5,.5,-.5),(-.5,.5,-.5),(-.5,-.5,.5),(.5,-.5,.5),(.5,.5,.5),(-.5,.5,.5)],[],[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)])
        data.materials.append(mats[mat]);cache[mat]=data
    o=link(bpy.data.objects.new(name,cache[mat]));o.location=cv(p);o.scale=(s[0],s[2],s[1]);return o
def mesh(name,points,faces,mat):
    data=bpy.data.meshes.new(name);data.from_pydata([cv(p) for p in points],[],faces);data.materials.append(mats[mat])
    bm=bmesh.new();bm.from_mesh(data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(data);bm.free();data.update()
    return link(bpy.data.objects.new(name,data))
def beam(name,a,b,r=.035,mat='Metal'):
    # Square section avoids extra cylinder faces in small architectural trim.
    pa,pb=cv(a),cv(b);v=pb-pa;o=box(name,(0,0,0),(1,1,1),mat)
    o.location=(pa+pb)*.5;o.scale=(r*2,r*2,v.length);o.rotation_euler=v.to_track_quat('Z','Y').to_euler();return o
font=bpy.data.fonts.get('Arial Bold') or bpy.data.fonts.load('C:/Windows/Fonts/arialbd.ttf')
def sign(text,p,size,width):
    data=bpy.data.curves.new(text,'FONT');data.body=text;data.font=font;data.align_x='CENTER';data.size=size;data.resolution_u=1;data.extrude=0
    data.materials.append(mats['LetterBlue']);o=link(bpy.data.objects.new(text,data));o.location=cv(p);o.rotation_euler=(math.pi/2,0,math.pi)
    bpy.context.view_layer.update()
    if o.dimensions.x>width:o.scale*=width/o.dimensions.x
    bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o;bpy.ops.object.convert(target='MESH')
def ring(x,z,w,d,clip,y):
    return [(x+u,y,z+v) for u,v in [(-w/2+clip,-d/2),(w/2-clip,-d/2),(w/2,-d/2+clip),(w/2,d/2-clip),(w/2-clip,d/2),(-w/2+clip,d/2),(-w/2,d/2-clip),(-w/2,-d/2+clip)]]
def band(name,lower,upper,mat,cap=False):
    n=len(lower);faces=[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    if cap:faces.extend([tuple(range(n-1,-1,-1)),tuple(range(n,2*n))])
    return mesh(name,lower+upper,faces,mat)
def window(x,y,z,w,h,side=0):
    # side=0: south/north façade, side=1: east/west façade.
    def part(n,dx,dy,ww,hh,dep,mat):
        return box(n,(x+dx if not side else x,y+dy,z if not side else z+dx),(ww,hh,dep) if not side else (dep,hh,ww),mat)
    part('Recessed campus glazing',0,0,w,h,.045,'Glass')
    for dx in [-w/2,w/2,0]:part('White window jamb',dx,0,.045,h+.08,.075,'Plaster')
    for dy in [-h/2,h/2]:part('White window lintel',0,dy,w+.08,.05,.075,'Plaster')
    part('Window transom',0,h*.15,w,.035,.08,'Plaster')

# Turtle: faceted auditorium, broad lower hips and raised shell roof.
# The three roof levels and small south porch follow the supplied reference.
x,z=-45.5,13.45
body0=ring(x,z,10.15,16.1,1.0,.12);body1=ring(x,z,10.15,16.1,1.0,3.80)
band('Turtle faceted plaster auditorium',body0,body1,'Plaster',True)
for y,h,mat,w,d in [(.22,.28,'Concrete',10.45,16.4),(3.56,.20,'BluePaint',10.45,16.4),(3.82,.18,'Plaster',10.6,16.55)]:
    band('Turtle continuous cornice',ring(x,z,w,d,1,y-h/2),ring(x,z,w,d,1,y+h/2),mat,True)
lower=ring(x,12.5,11.65,20.8,1.5,3.95);upper=ring(x,12.5,7.55,13.2,1.15,5.75)
band('Turtle terracotta hip roof',lower,upper,'Terracotta')
band('Turtle deep eaves fascia',[(a,b-.16,c) for a,b,c in lower],lower,'BluePaint')
for a,b in zip(lower,upper):beam('Turtle roof hip cap',a,b,.045,'Terracotta')
cler0=ring(x,12.5,7.35,12.9,1.0,5.6);cler1=ring(x,12.5,7.35,12.9,1.0,6.13)
band('Turtle raised clerestory glass',cler0,cler1,'Glass',True)
for a,b in zip(cler0,cler1):beam('Turtle clerestory mullion',a,b,.045,'Plaster')
top=ring(x,12.5,8.0,13.8,1.3,6.13)
# Rebuilt shell: two sloping longitudinal planes, hipped ends and a short ridge.
ridge=[(x,8.0,9.0),(x,8.0,16.0)]
mesh('Turtle raised silver shell',top+ridge,[(0,1,8),(1,2,8),(2,3,9,8),(3,4,9),(4,5,9),(5,6,9),(6,7,8,9),(7,0,8)],'SilverCladding')
beam('Turtle shell ridge',ridge[0],ridge[1],.045,'Metal')
for i in [0,1,2,7]:beam('Turtle shell seam',top[i],ridge[0],.025,'Metal')
for i in [3,4,5,6]:beam('Turtle shell seam',top[i],ridge[1],.025,'Metal')
for side in [-1,1]:
    for zz in [7,9.4,11.8,14.2,16.6,19.0]:
        window(x+side*5.09,2.05,zz,1.5,1.45,1)
        box('Turtle side buttress',(x+side*5.18,1.9,zz+1.02),(.13,3.38,.16),'Plaster')
for xx in [-3.1,3.1]:window(x+xx,2.05,z-8.07,1.25,1.5)
for xx in [-2.9,0,2.9]:window(x+xx,2.05,z+8.07,1.45,1.45)
box('Turtle entrance door recess',(x,1.30,5.365),(2.8,2.45,.055),'BluePaint')
for dx in [-.68,.68]:window(x+dx,1.28,5.32,1.20,2.25)
box('Turtle entrance sign band',(x,3.17,5.30),(6.9,.60,.12),'Plaster');sign('HỘI TRƯỜNG RÙA',(x,2.99,5.225),.40,6.4)
for dx in [-2.0,2.0]:box('Turtle porch column',(x+dx,1.5,3.48),(.20,2.85,.20),'Plaster')
mesh('Turtle terracotta porch roof',[(x-2.35,2.96,2.35),(x+2.35,2.96,2.35),(x+2.35,2.96,5.25),(x-2.35,2.96,5.25),(x,3.70,3.8)],[(0,1,4),(1,2,4),(2,3,4),(3,0,4),(3,2,1,0)],'Terracotta')
for i in range(3):box('Turtle tiled entrance step',(x,.06+i*.08,2.7+i*.29),(4.55,.12+i*.16,1.10-i*.26),'RedPaver')

# A3: retain each wing's surveyed envelope and open routes between rows.
zone=targets[1]
wings=[c for c in layout['collisions'] if c['name'].startswith('Sketchfab A3 wing ')]
for i,c in enumerate(wings):
    p,s=c['position'],c['size'];x,z=p['x'],p['z'];w,d=s['x'],s['z']
    if w<2: # compact connector between the rear pavilions
        box('A3 rear connecting lobby',(x,1.45,z),(w-.12,2.8,d-.10),'Plaster')
        box('A3 connector roof',(x,3.00,z),(w, .16,d),'Terracotta');continue
    wallh=3.24 if w<6 else 3.05
    box('A3 classroom wing '+str(i+1),(x,wallh/2+.10,z),(w-.34,wallh,d-.40),'Plaster')
    box('A3 damp proof plinth',(x,.18,z),(w-.22,.20,d-.25),'Concrete')
    box('A3 blue eaves band',(x,wallh+.08,z),(w-.06,.18,d-.06),'BluePaint')
    yy=wallh+.20
    mesh('A3 low pitched tiled roof',[(x-w/2,yy,z-d/2),(x+w/2,yy,z-d/2),(x+w/2,yy,z+d/2),(x-w/2,yy,z+d/2),(x-w/2,4.13,z),(x+w/2,4.13,z)],[(0,1,5,4),(2,3,4,5),(0,4,3),(1,2,5),(3,2,1,0)],'Terracotta')
    beam('A3 terracotta ridge',(x-w/2+.03,4.14,z),(x+w/2-.03,4.14,z),.035,'Terracotta')
    bays=max(3,round(w/1.75));spacing=(w-.5)/bays
    for side in [-1,1]:
        face=z+side*(d/2-.18)
        box('A3 shaded corridor fascia',(x,2.83,face),(w-.16,.13,.22),'Plaster')
        for j in range(bays):
            xx=x-w/2+.25+spacing*(j+.5)
            if side==-1 and j%3==0:
                box('A3 classroom blue door',(xx,1.15,face),(min(.77,spacing*.58),2.07,.065),'BluePaint')
                box('A3 door upper light',(xx,1.73,face-.04),(.50,.57,.028),'Glass')
                box('A3 door handle',(xx+.22,1.06,face-.065),(.04,.16,.04),'Metal')
            else:window(xx,1.84,face,min(1.0,spacing*.70),1.12)
            if j<bays-1:box('A3 slender corridor pier',(xx+spacing/2,1.49,face+side*.06),(.10,2.77,.12),'Plaster')
        for dy in [2.98]:box('A3 continuous ventilation strip',(x,dy,face),(w-.6,.12,.05),'BluePaint')
    for side in [-1,1]:
        # Vents and downpipes remain inside the existing full roof footprint.
        xx=x+side*(w/2-.10)
        box('A3 side ventilation grille',(xx,2.40,z),(.06,.45,max(.6,d-.9)),'BluePaint')
        for dz in [-d*.28,d*.28]:box('A3 rainwater pipe',(xx,1.60,z+dz),(.06,3.0,.06),'Plaster')
    if i==0:
        box('A3 identity sign fascia',(x,3.06,z-d/2+.04),(w-.35,.43,.06),'Plaster')
        sign('NHÀ HỌC A3',(x,2.94,z-d/2-.005),.29,w-.65)

bpy.context.view_layer.update()
report={'passed':True,'sourceRevision':12,'artRevision':13,'style':'Existing CT5 campus surfaces; new editable architectural geometry',
        'referenceAuthor':'AnLe','referenceLicense':'CC BY 4.0','groundExpanded':False,'collisionsChanged':False,'placements':[]}
for name in targets:
    obs=list(bpy.data.collections[name].objects);points=[o.matrix_world @ v.co for o in obs for v in o.data.vertices]
    lo=[min(p[i] for p in points) for i in range(3)];hi=[max(p[i] for p in points) for i in range(3)]
    xmin,xmax,zmin,zmax=-hi[0],-lo[0],-hi[1],-lo[1]
    poly=[(xmin,zmin),(xmax,zmin),(xmax,zmax),(xmin,zmax)]
    for road in layout['roads']:
        assert not polygon_near_segment(poly,(road['a']['x'],road['a']['z']),(road['b']['x'],road['b']['z']),road['width']/2+2),(name,road)
    tris=sum(len(p.vertices)-2 for o in obs for p in o.data.polygons)
    report['placements'].append(dict(collection=name,unityMin=[xmin,lo[2],zmin],unityMax=[xmax,hi[2],zmax],objects=len(obs),triangles=tris,materials=sorted(set(o.data.materials[0].name for o in obs))))
layout['artRevision']=13;layout['landmarkRedraw']=report
layout['note']='Revision13: Turtle Hall and A3 redrawn from AnLe references using the existing campus material library. Existing footprints, collision proxies, roads, trees, sports hall and gameplay retained.'
for entry in layout['sketchfabLandmarks']:
    if entry['collection'] in targets:entry['representation']='Redrawn architecture from AnLe reference; shared campus CT5 materials'
for name,data in [('CanThoCampus.layout.json',layout),('LandmarkRedraw.json',report)]:
    with open(os.path.join(output,name),'w',encoding='utf-8') as f:json.dump(data,f,indent=2,ensure_ascii=False)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(output,'CanThoCampus.blend'),relative_remap=False)
from FinishCanThoCampus import finish
finish(output,False)
print('LANDMARK_REDRAW_PASS '+json.dumps(report),flush=True)
