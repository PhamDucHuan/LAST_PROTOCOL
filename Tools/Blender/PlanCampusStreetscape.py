"""Deterministic fictional residential infill; preserve referenced main streets."""
import json, math, os, random, sys
sys.dont_write_bytecode=True
sys.path.insert(0,os.path.dirname(__file__))
from PeripheralGeometry import inside, rectangle, overlap, polygon_near_segment, distance_segment, sample_path

def plan(layout):
    rng=random.Random(20261018)
    campus=[(p['x'],p['z']) for p in layout['campusBoundary']]
    water=[(p['x'],p['z']) for p in layout['waterBoundary']]
    roads=[dict(id=r['id'],width=r['width'],points=[(p['x'],p['z']) for p in r['points']]) for r in layout['peripheralRoads']]
    segments=[(a,b,r['width']/2,r['id']) for r in roads for a,b in zip(r['points'],r['points'][1:])]
    route=[(p['x'],p['z']) for p in layout['neighborhoodRoute']]
    lots=[]
    for h in layout['neighborhoodHouses']:
        p,s,f=h['position'],h['size'],h['front']
        lots.append(rectangle(p['x']+f['x']*1.5,p['z']+f['z']*1.5,s['x']+.4,s['z']+3,(f['x'],f['z'])))
    def outside(poly):
        if any(x < -535 or x > 353 or z < -365 or z > 430 for x,z in poly):return False
        for boundary in [campus,water]:
            if any(inside(p,boundary) for p in poly):return False
            if any(polygon_near_segment(poly,a,b,7) for a,b in zip(boundary,boundary[1:]+boundary[:1])):return False
        return True
    slots=[]
    for r in roads:
        for p,t in sample_path(r['points'],3):
            for side in [-1,1]:
                n=(-t[1]*side,t[0]*side)
                a=(p[0]+n[0]*(r['width']/2+.3),p[1]+n[1]*(r['width']/2+.3))
                b=(a[0]+n[0]*120,a[1]+n[1]*120)
                corridor=rectangle((a[0]+b[0])/2,(a[1]+b[1])/2,5,math.dist(a,b),n)
                if not outside(corridor):continue
                if any(polygon_near_segment(poly,a,b,3) for poly in lots):continue
                if any(rid!=r['id'] and polygon_near_segment(corridor,c,d,w+3) for c,d,w,rid in segments):continue
                slots.append(dict(a=p,b=b,front=n,district=r['id']))
    rng.shuffle(slots)
    lanes=[];houses=[]
    for slot in slots:
        a,b=slot['a'],slot['b'];n=slot['front']
        if any(min(distance_segment(a,l['a'],l['b']),distance_segment(b,l['a'],l['b']))<35 for l in lanes):continue
        if any(polygon_near_segment(poly,a,b,3) for poly in lots):continue
        proposed=[]
        for distance in range(24,115,12):
            for side in [-1,1]:
                w=rng.choice([6.5,7,7.5]);d=rng.choice([10,12,14]);height=rng.choice([6.8,10.0])
                tangent=(n[1]*side,-n[0]*side);front=(-tangent[0],-tangent[1]);offset=4+d/2+2
                x=a[0]+n[0]*distance+tangent[0]*offset;z=a[1]+n[1]*distance+tangent[1]*offset
                poly=rectangle(x+front[0]*1.5,z+front[1]*1.5,w+.4,d+3,front)
                if not outside(poly):continue
                if any(polygon_near_segment(poly,c,e,radius+2) for c,e,radius,_ in segments):continue
                if any(polygon_near_segment(poly,c,e,2.2) for c,e in zip(route,route[1:])):continue
                if any(overlap(poly,other,1.1) for other in lots):continue
                if any(polygon_near_segment(poly,l['a'],l['b'],3) for l in lanes):continue
                if any(overlap(poly,h['lot'],1.1) for h in proposed):continue
                proposed.append(dict(x=x,z=z,w=w,d=d,height=height,front=front,lot=poly,district=slot['district']))
        if len(proposed)<6:continue
        lanes.append(slot)
        for h in proposed:
            if len(houses)>=84:break
            lots.append(h['lot']);houses.append(h)
        if len(houses)>=84:break
    if len(houses)<55:raise RuntimeError('Insufficient safe infill '+str(len(houses)))
    return dict(lanes=lanes,houses=houses)

if __name__=='__main__':
    with open(sys.argv[1],encoding='utf-8') as f:layout=json.load(f)
    result=plan(layout)
    with open(sys.argv[2],'w',encoding='utf-8') as f:json.dump(result,f,indent=2)
    print('STREETSCAPE_PLAN lanes='+str(len(result['lanes']))+' houses='+str(len(result['houses'])))
