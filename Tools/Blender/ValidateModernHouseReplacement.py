"""Validate actual replacement mesh bounds and preserve all unrelated layout."""
import bpy,json,os,sys,math
from mathutils import Vector
baseline,candidate=map(os.path.abspath,sys.argv[sys.argv.index('--')+1:][:2])
def read(folder,name):
    with open(os.path.join(folder,name),encoding='utf-8-sig') as f:return json.load(f)
a=read(baseline,'CanThoCampus.layout.json');b=read(candidate,'CanThoCampus.layout.json');r=read(candidate,'ModernHouseIntegration.json');stats=read(candidate,'BuildStats.json')
changed={'artRevision','note','materials','collisions','neighborhoodHouses','infillBuildings'}
checks={k:a[k]==b.get(k) for k in a if k not in changed}
replaced={p['replaces'] for p in r['placements']}
checks['atLeast247OldHousesReplaced']=len(replaced)>=247
checks['allTwentyVariantsAndTownHouseUsed']=len(r['summary']['variants'])==21
checks['all1351ParcelsRetained']=len(a['neighborhoodHouses'])==len(b['neighborhoodHouses'])==1351
checks['otherHousesUnchanged']=[h for h in a['neighborhoodHouses'] if h['name'] not in replaced]==[h for h in b['neighborhoodHouses'] if h['name'] not in replaced]
checks['otherCollidersUnchanged']=[c for c in a['collisions'] if c['name'] not in replaced and not c['name'].startswith('Awning support')]==[c for c in b['collisions'] if c['name'] not in replaced]
checks['renderBudget']=stats['triangles']<600000 and stats['renderers']<500
checks['portableTextureFiles']=all(os.path.isfile(os.path.join(candidate,m[key])) for m in b['materials'] for key in ('baseMap','normalMap') if m.get(key))
bpy.ops.wm.open_mainfile(filepath=os.path.join(candidate,'CanThoCampus.blend'),load_ui=False,use_scripts=False)
objects={o.name:o for o in bpy.context.scene.objects if o.type=='MESH' and o.name.startswith('Imported ')}
errors=[]
for p in r['placements']:
    obj=objects['Imported '+p['family']+' '+p['name']]
    old=p['originalParcel'];pos=old['position'];s=old['size'];f=old['front'];fx,fz=f['x'],f['z'];rx,rz=fz,-fx
    points=[obj.matrix_world@v.co for v in obj.data.vertices]
    for v in points:
        dx,dz=-v.x-pos['x'],-v.y-pos['z']
        if abs(dx*rx+dz*rz)>s['x']/2+.025 or abs(dx*fx+dz*fz)>s['z']/2+.025 or v.z<-.01 or v.z>p['size']['y']+.025:
            errors.append(p['name']);break
    if abs(min(v.z for v in points))>.01:errors.append(p['name']+' floating')
checks['ActualMeshesInsideOriginalParcelsAndOnGround']=not errors
result=dict(passed=all(checks.values()),checks=checks,errors=errors,stats=stats,summary=r['summary'])
with open(os.path.join(candidate,'IntegrationValidation.json'),'w',encoding='utf-8') as f:json.dump(result,f,indent=2)
print('MODERN_HOUSE_VALIDATION '+json.dumps(result),flush=True)
if not result['passed']:raise RuntimeError('Replacement validation failed')
