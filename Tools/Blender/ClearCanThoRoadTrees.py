"""Relocate complete trees which obstruct retained road lanes; keep green islands."""
import bpy,json,math,os,shutil,sys
from mathutils import Matrix,Vector
sys.dont_write_bytecode=True
sys.path.insert(0,os.path.dirname(__file__))
from PeripheralGeometry import distance_segment,inside
source,output=map(os.path.abspath,sys.argv[sys.argv.index('--')+1:][:2])
if os.path.exists(output):raise RuntimeError('Use a new candidate directory.')
os.makedirs(output);shutil.copytree(os.path.join(source,'Textures'),os.path.join(output,'Textures'))
with open(os.path.join(source,'CanThoCampus.layout.json'),encoding='utf-8') as f:layout=json.load(f)
assert layout.get('turtleRoadClearance',{}).get('revision')==1
assert not layout.get('roadTreeClearance'),'Source already repaired; use the preserved baseline.'
bpy.ops.wm.open_mainfile(filepath=os.path.join(source,'CanThoCampus.blend'),load_ui=False,use_scripts=False)
roads=list(layout['roads'])
for key in ['peripheralRoads','localLanes']:
    for road in layout[key]:
        for a,b in zip(road['points'],road['points'][1:]):roads.append(dict(name=road['id'],a=a,b=b,width=road['width']))
segments=[((r['a']['x'],r['a']['z']),(r['b']['x'],r['b']['z']),r['width']/2)for r in roads]
trees=[(i,c)for i,c in enumerate(layout['collisions'])if 'trunk' in c['name'].lower()]
water=[(p['x'],p['z'])for p in layout['waterBoundary']]
def island(x,z):return math.hypot(x+22,z-22)<3.6 or math.hypot(x-64.32,z-27.16)<2.1
def road_clear(x,z,radius):
    return all(distance_segment((x,z),a,b)>=w+radius+1 for a,b,w in segments)
def safe(x,z,index,radius):
    if not road_clear(x,z,radius+.5) or inside((x,z),water):return False
    if not(-548<x<368 and -385<z<445):return False
    for j,c in enumerate(layout['collisions']):
        if j==index or c['name'] in ('Campus soil','Bung Xang water exclusion'):continue
        p,s=c['position'],c['size']
        if 'trunk' in c['name'].lower():
            if math.hypot(x-p['x'],z-p['z'])<4.5:return False
        elif s['y']>1:
            angle=math.radians(c.get('euler',{}).get('y',0));dx=x-p['x'];dz=z-p['z']
            localx=dx*math.cos(angle)-dz*math.sin(angle);localz=dx*math.sin(angle)+dz*math.cos(angle)
            if abs(localx)<s['x']/2+2 and abs(localz)<s['z']/2+2:return False
    return True
def suffix(name,prefix):
    assert name.startswith(prefix)
    return 0 if name==prefix else int(name[len(prefix)+1:])
campus=[c for _,c in trees if c['name'] in ('Shade tree trunk','Campus route tree trunk')]
peripheral=[c for _,c in trees if c['name']=='Peripheral shade tree trunk']
objects={id(c):[]for _,c in trees}
for obj in bpy.data.collections['27_Landscape'].objects:
    if obj.name.startswith('Shade trunk / branch'):index=suffix(obj.name,'Shade trunk / branch')//5
    elif obj.name.startswith('Leaf cluster card'):index=suffix(obj.name,'Leaf cluster card')//64
    elif obj.name.startswith('Branching shade trunk'):index=suffix(obj.name,'Branching shade trunk')
    elif obj.name.startswith('Shade tree branch'):index=suffix(obj.name,'Shade tree branch')//4
    elif obj.name.startswith('Photometric leaf cluster'):index=suffix(obj.name,'Photometric leaf cluster')//64
    else:raise RuntimeError('Unclassified campus tree part: '+obj.name)
    objects[id(campus[index])].append(obj)
for collection in bpy.data.collections:
    if not collection.name.startswith('65_StreetscapeTrees_'):continue
    for obj in collection.objects:
        if obj.name.startswith('Peripheral shade tree trunk'):index=suffix(obj.name,'Peripheral shade tree trunk')
        elif obj.name.startswith('Peripheral leaf cluster'):index=suffix(obj.name,'Peripheral leaf cluster')//32
        else:raise RuntimeError('Unclassified peripheral tree part: '+obj.name)
        objects[id(peripheral[index])].append(obj)
for _,c in trees:
    if c['name'].startswith('Vietnam Polygon trunk '):
        prefix=c['name'].replace('trunk','tree')+'__'
        objects[id(c)]=[o for o in bpy.data.collections['68_VietnamImported'].objects if o.name.startswith(prefix)]
assert all(objects.values())
repairs=[]
for index,c in trees:
    p,s=c['position'],c['size'];x,z=p['x'],p['z'];radius=max(s['x'],s['z'])/2
    if island(x,z) or road_clear(x,z,radius):continue
    target=None
    for step in range(1,101):
        distance=step*.5
        for n in range(96):
            angle=n*math.tau/96;xx=x+math.cos(angle)*distance;zz=z+math.sin(angle)*distance
            if safe(xx,zz,index,radius):target=(round(xx,5),round(zz,5));break
        if target:break
    if not target:raise RuntimeError('No safe location for '+str((x,z)))
    xx,zz=target;delta=Matrix.Translation(Vector((x-xx,z-zz,0)))
    parts=objects[id(c)]
    # The flush tree grate is separate from the replacement foliage collection.
    if c['name']=='Campus route tree trunk':
        for obj in bpy.data.collections['36_CampusRoute'].objects:
            if obj.name.startswith('Flush tree grate') and abs(obj.location.x+x)<.1 and abs(obj.location.y+z)<.1:parts.append(obj)
    for obj in parts:obj.matrix_world=delta @ obj.matrix_world
    for entry in layout['streetscapeTrees']:
        if abs(entry['x']-x)<.01 and abs(entry['z']-z)<.01:entry.update(x=xx,z=zz)
    for entry in layout['importedVietnamAssets']:
        if entry['name']==c['name'].replace('trunk','tree'):entry['position'].update(x=xx,z=zz)
    p.update(x=xx,z=zz)
    repairs.append(dict(collisionIndex=index,name=c['name'],old=dict(x=x,z=z),new=dict(x=xx,z=zz),objects=[o.name for o in parts]))
for _,c in trees:
    p,s=c['position'],c['size']
    assert island(p['x'],p['z']) or road_clear(p['x'],p['z'],max(s['x'],s['z'])/2),c
layout['roadTreeClearance']=dict(revision=1,minimumLaneSetback=1,movedTrees=len(repairs),repairs=repairs,groundExpanded=False)
with open(os.path.join(output,'CanThoCampus.layout.json'),'w',encoding='utf-8') as f:json.dump(layout,f,indent=2,ensure_ascii=False)
with open(os.path.join(output,'RoadTreeRepair.json'),'w')as f:json.dump(dict(passed=True,repairs=repairs,checkedTrees=len(trees),checkedRoadSegments=len(segments)),f,indent=2)
bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(output,'CanThoCampus.blend'),relative_remap=False)
from FinishCanThoCampus import finish
finish(output,False)
print('ROAD_TREE_CLEARANCE_PASS '+str(len(repairs))+' relocated trees',flush=True)
