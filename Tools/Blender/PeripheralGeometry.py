"""Small deterministic 2D geometry utilities for reference-based street placement."""
import math

def distance_segment(p,a,b):
    dx,dz=b[0]-a[0],b[1]-a[1];length=dx*dx+dz*dz
    t=max(0,min(1,((p[0]-a[0])*dx+(p[1]-a[1])*dz)/length)) if length else 0
    return math.hypot(p[0]-a[0]-t*dx,p[1]-a[1]-t*dz)

def inside(p,polygon):
    result=False;x,z=p
    for a,b in zip(polygon,polygon[1:]+polygon[:1]):
        if (a[1]>z)!=(b[1]>z) and x < (b[0]-a[0])*(z-a[1])/(b[1]-a[1])+a[0]:result=not result
    return result

def crossing(a,b,c,d):
    def cross(p,q,r):return (q[0]-p[0])*(r[1]-p[1])-(q[1]-p[1])*(r[0]-p[0])
    return cross(a,b,c)*cross(a,b,d)<0 and cross(c,d,a)*cross(c,d,b)<0

def polygon_near_segment(poly,a,b,radius):
    if inside(a,poly) or inside(b,poly):return True
    for c,d in zip(poly,poly[1:]+poly[:1]):
        if crossing(a,b,c,d):return True
        if min(distance_segment(c,a,b),distance_segment(d,a,b),distance_segment(a,c,d),distance_segment(b,c,d))<radius:return True
    return False

def overlap(a,b,margin=0):
    # Separating axis test, including a small walking gap between lots.
    for poly in [a,b]:
        for p,q in zip(poly,poly[1:]+poly[:1]):
            dx,dz=q[0]-p[0],q[1]-p[1];length=math.hypot(dx,dz);axis=(-dz/length,dx/length)
            aa=[x*axis[0]+z*axis[1] for x,z in a];bb=[x*axis[0]+z*axis[1] for x,z in b]
            if max(aa)+margin<min(bb) or max(bb)+margin<min(aa):return False
    return True

def rectangle(x,z,w,d,front):
    fx,fz=front;rx,rz=fz,-fx
    return [(x+rx*s*w/2+fx*t*d/2,z+rz*s*w/2+fz*t*d/2) for s,t in [(-1,-1),(1,-1),(1,1),(-1,1)]]

def sample_path(points,spacing=12):
    result=[];travel=0;next_at=18
    for a,b in zip(points,points[1:]):
        dx,dz=b[0]-a[0],b[1]-a[1];length=math.hypot(dx,dz)
        while next_at<travel+length-10:
            t=(next_at-travel)/length
            if t>=0:result.append(((a[0]+dx*t,a[1]+dz*t),(dx/length,dz/length)))
            next_at+=spacing
        travel+=length
        next_at=max(next_at,travel+12)
    return result
