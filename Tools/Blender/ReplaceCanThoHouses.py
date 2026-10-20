"""Replace the old street houses and nearby infill with licensed imported art."""
import bpy,json,math,os,shutil,sys,collections
from mathutils import Matrix,Vector
sys.dont_write_bytecode=True
sys.path.insert(0,os.path.dirname(__file__))
root,prepared,output=map(os.path.abspath,sys.argv[sys.argv.index('--')+1:][:3])
if os.path.exists(output):raise RuntimeError('Use a new candidate directory.')
source=os.path.join(root,'ArtSource/Blender/CanThoCampus')
os.makedirs(os.path.join(output,'Textures'))
for directory in [os.path.join(source,'Textures'),os.path.join(prepared,'Textures')]:
    for name in os.listdir(directory):shutil.copy2(os.path.join(directory,name),os.path.join(output,'Textures',name))
with open(os.path.join(source,'CanThoCampus.layout.json'),encoding='utf-8') as f:layout=json.load(f)
with open(os.path.join(prepared,'PreparedAssets.json'),encoding='utf-8') as f:bank=json.load(f)
if layout['artRevision']!=10:raise ValueError('Expected revision 10 baseline.')
bpy.ops.wm.open_mainfile(filepath=os.path.join(source,'CanThoCampus.blend'),load_ui=False,use_scripts=False)
def tris(o):return sum(len(p.vertices)-2 for p in o.data.polygons)
def bounds(o):
    p=[o.matrix_world@Vector(v) for v in o.bound_box]
    return Vector([min(v[i] for v in p) for i in range(3)]),Vector([max(v[i] for v in p) for i in range(3)])
def xyz(x,y,z):return dict(x=round(x,5),y=round(y,5),z=round(z,5))
total=sum(tris(o) for o in bpy.context.scene.objects if o.type=='MESH')
original=total;removed=[]
for c in bpy.data.collections:
    if not c.name.startswith(('60_Geo_','64_Infill_')):continue
    for o in list(c.objects):
        if o.type!='MESH' or o.name.startswith(('Local lane paving','Local residential lane','Infill parcel paving')):continue
        total-=tris(o);removed.append(o)
removed_names=[o.name for o in removed]
bpy.data.batch_remove(ids=tuple(removed))
with bpy.data.libraries.load(os.path.join(prepared,'ModernHouses.Prepared.blend'),link=False) as (a,b):b.objects=[n for n in a.objects if n.startswith('ModernHouse') or n=='TownHouse']
templates={o.name:o for o in b.objects if o}
assert len(templates)==21,len(templates)
for o in templates.values():bpy.context.scene.collection.objects.link(o)
bpy.context.view_layer.update()
template_bounds={n:bounds(o) for n,o in templates.items()}
colliders={c['name']:c for c in layout['collisions'] if c['name'].startswith(('District house','Infill building'))}
infill={h['name']:h for h in layout['infillBuildings']}
placements=[];variants=collections.Counter()
def place(h,variant,old_objects=()):
    global total
    old=json.loads(json.dumps(h));p,s,f=old['position'],old['size'],old['front'];fx,fz=f['x'],f['z'];rx,rz=fz,-fx
    template=templates[variant];lo,hi=template_bounds[variant];dims=hi-lo
    w=s['x']-.2
    # Narrow urban lots: preserve the source proportions with a bounded depth
    # adjustment, aligning the authored entrance to the old street frontage.
    d=min(s['z']-.2,w*dims.y/dims.x*1.4)
    height=min(s['y'],8.6 if variant!='TownHouse' else 7.5)
    x=p['x']+fx*(s['z']-d)/2;z=p['z']+fz*(s['z']-d)/2
    transform=Matrix(((-rx*w/dims.x,fx*d/dims.y,0,-x),(-rz*w/dims.x,fz*d/dims.y,0,-z),(0,0,height/dims.z,0),(0,0,0,1)))
    data=template.data.copy();data.transform(transform)
    if transform.determinant()<0:data.flip_normals()
    sector=(1 if x>-100 else 0)+(2 if z>60 else 0)
    cname='69_ModernHouses_'+str(sector)
    c=bpy.data.collections.get(cname)
    if c is None:c=bpy.data.collections.new(cname);bpy.context.scene.collection.children.link(c)
    obj=bpy.data.objects.new('Imported '+variant+' '+h['name'],data);c.objects.link(obj)
    for o in old_objects:total-=tris(o);removed_names.append(o.name)
    if old_objects:bpy.data.batch_remove(ids=tuple(old_objects))
    total+=tris(obj)
    h.update(position=xyz(x,0,z),size=xyz(w,height,d),family=variant)
    if h['name'] in infill:infill[h['name']].update(position=h['position'],size=h['size'],family=variant)
    colliders[h['name']].update(position=xyz(x,height/2,z),size=h['size'])
    entry=dict(name=h['name'],family=variant,replaces=h['name'],position=h['position'],size=h['size'],front=h['front'],euler=h['euler'],district=h['district'],originalParcel=old,triangles=tris(obj))
    placements.append(entry);variants[variant]+=1
old_imports={p['replaces'] for p in layout['importedVietnamAssets'] if p.get('replaces')}
street=[h for h in layout['neighborhoodHouses'] if h['name'].startswith('District house') and h['name'] not in old_imports]
for i,h in enumerate(street):
    variant='TownHouse' if i%12==0 else 'ModernHouse%02d'%(i%20+1)
    place(h,variant)
# Remove the superseded procedural awning colliders along with their geometry.
layout['collisions']=[c for c in layout['collisions'] if not c['name'].startswith('Awning support')]
# The far infill remains inexpensive. Upgrade the next eighty visible parcels,
# distributing them along the retained main streets while enforcing the budget.
infill_obs=[o for c in bpy.data.collections if c.name.startswith('67_ExistingLand_') for o in c.objects if o.type=='MESH']
centers={o.name:bounds(o) for o in infill_obs}
from PeripheralGeometry import distance_segment
roads=[(a,b) for r in layout['peripheralRoads'] for a,b in zip(r['points'],r['points'][1:])]
def priority(h):
    p=(h['position']['x'],h['position']['z'])
    road_distance=min(distance_segment(p,(a['x'],a['z']),(b['x'],b['z'])) for a,b in roads)
    return road_distance+math.hypot(p[0]+60,p[1]+50)*.05
near=sorted([h for h in layout['neighborhoodHouses'] if h['name'] in infill],key=priority)
upgraded=0
for h in near:
    if upgraded>=80:break
    variant='ModernHouse%02d'%((upgraded*7+3)%20+1)
    if total+tris(templates[variant])>595000:continue
    p,s,f=h['position'],h['size'],h['front'];fx,fz=f['x'],f['z'];rx,rz=fz,-fx;old_objects=[]
    for o in infill_obs:
        a,b=centers[o.name];cx,cz=-(a.x+b.x)/2,-(a.y+b.y)/2;dx,dz=cx-p['x'],cz-p['z']
        if o.name==h['name'] or (abs(dx*rx+dz*rz)<s['x']/2+.3 and abs(dx*fx+dz*fz)<s['z']/2+1.0):old_objects.append(o)
    if not old_objects:raise RuntimeError('No original mesh for '+h['name'])
    names={o.name for o in old_objects};infill_obs=[o for o in infill_obs if o.name not in names]
    place(h,variant,old_objects);upgraded+=1
bpy.data.batch_remove(ids=tuple(templates.values()))
actual=sum(tris(o) for o in bpy.context.scene.objects if o.type=='MESH')
assert actual==total,(actual,total)
if actual>=600000:raise RuntimeError('Art budget exceeded: '+str(actual))
layout['materials'].extend(bank['materials']);layout['artRevision']=11
layout['note']='Revision11: imported Modern Houses and Town House replace existing street housing and selected infill, within the original parcels. Land, roads, water, campus buildings and previous Vietnamese assets retained.'
layout['importedModernHouses']=placements
layout['modernHouseRevision']=dict(originalRevision=10,streetReplacements=len(street),infillReplacements=upgraded,totalReplacements=len(placements),variants=dict(variants),groundExpanded=False)
report=dict(sourceRevision=10,artRevision=11,originalTriangles=original,triangles=actual,removedObjects=removed_names,placements=placements,summary=layout['modernHouseRevision'])
for name,value in [('CanThoCampus.layout.json',layout),('ModernHouseIntegration.json',report)]:
    with open(os.path.join(output,name),'w',encoding='utf-8') as f:json.dump(value,f,indent=2,ensure_ascii=False)
bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(output,'CanThoCampus.blend'),relative_remap=False)
from FinishCanThoCampus import finish
finish(output,False)
print('MODERN_HOUSE_REPLACEMENT_PASS '+json.dumps(layout['modernHouseRevision']),flush=True)
