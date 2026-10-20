"""Check the original land/roads and the four replacement parcels before Unity."""
import json, os, sys, math
source, candidate = map(os.path.abspath,sys.argv[1:3])
def read(p):
    with open(p,encoding='utf-8-sig') as f:return json.load(f)
a=read(os.path.join(source,'CanThoCampus.layout.json'));b=read(os.path.join(candidate,'CanThoCampus.layout.json'))
r=read(os.path.join(candidate,'VietnamIntegration.json'));stats=read(os.path.join(candidate,'BuildStats.json'))
changed={'artRevision','note','materials','collisions','neighborhoodHouses'}
checks={key:a[key]==b.get(key) for key in a if key not in changed}
checks['landBoundsUnchanged']=a['bounds']==b['bounds']
checks['allExistingHousesRetained']=len(a['neighborhoodHouses'])==len(b['neighborhoodHouses'])==1351
replaced={p['replaces'] for p in r['placements'] if 'replaces' in p}
checks['exactlyFourParcelsReplaced']=len(replaced)==4
checks['otherHouseMetadataRetained']=[h for h in a['neighborhoodHouses'] if h['name'] not in replaced]==[h for h in b['neighborhoodHouses'] if h['name'] not in replaced]
checks['otherCollidersRetained']=[c for c in a['collisions'] if c['name'] not in replaced]==[c for c in b['collisions'] if c['name'] not in replaced and not c['name'].startswith('Vietnam Polygon trunk ')]
checks['newSourcesPresent']={p['family'] for p in r['placements']}=={'HoiAn','Asian','Pho','PolygonTree'}
checks['renderBudget']=stats['triangles']<600000 and stats['renderers']<500
checks['replacementFootprintsInsideOriginalParcels']=True
for p in r['placements']:
    if 'originalParcel' not in p:continue
    old=p['originalParcel'];angle=math.radians(old['euler']['y']);fx,fz=old['front']['x'],old['front']['z'];rx,rz=fz,-fx
    for side in (-.5,.5):
        for depth in (-.5,.5):
            x=p['position']['x']+rx*side*p['size']['x']+fx*depth*p['size']['z']-old['position']['x']
            z=p['position']['z']+rz*side*p['size']['x']+fz*depth*p['size']['z']-old['position']['z']
            if abs(x*rx+z*rz)>old['size']['x']/2+.01 or abs(x*fx+z*fz)>old['size']['z']/2+.01:checks['replacementFootprintsInsideOriginalParcels']=False
checks['portableTextures']=all(os.path.isfile(os.path.join(candidate,m[k])) for m in b['materials'] for k in ('baseMap','normalMap') if m.get(k))
checks['newTexturesAreRealPngAtMost1K']=True
for m in b['materials']:
    if not m['name'].startswith('CT10_'):continue
    for key in ('baseMap','normalMap'):
        if not m.get(key):continue
        with open(os.path.join(candidate,m[key]),'rb') as image:header=image.read(24)
        if header[:8]!=b'\x89PNG\r\n\x1a\n' or max(int.from_bytes(header[16:20],'big'),int.from_bytes(header[20:24],'big'))>1024:checks['newTexturesAreRealPngAtMost1K']=False
result=dict(passed=all(checks.values()),checks=checks,stats=stats)
with open(os.path.join(candidate,'IntegrationValidation.json'),'w',encoding='utf-8') as f:json.dump(result,f,indent=2)
print(json.dumps(result))
if not result['passed']:sys.exit(1)
