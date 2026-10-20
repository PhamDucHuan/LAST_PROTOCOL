"""Secondary elevations: make rear/side views read as homes, not blank warehouses."""
import math

def enrich(layout, box, set_zone):
    for index,h in enumerate(layout['neighborhoodHouses'],1):
        p,s=h['position'],h['size'];x,z,w,d,height=p['x'],p['z'],s['x'],s['z'],s['y']
        sector=0 if x < -84 else 1 if x < 84 else 2
        row=0 if z > -130 else 1 if z > -200 else 2
        set_zone('5%d_Neighborhood_%d' % (1+row,sector))
        floors=round((height-.4)/3.2)
        for floor in range(1,floors):
            y=floor*3.2+1.5
            for side in [-1,1]:
                for dz in [-d*.25,d*.18]:
                    box('House side window surround',(x+side*(w/2+.015),y,z+dz),(.09,1.9,1.6),'Plaster')
                    box('House side window glazing',(x+side*(w/2+.07),y,z+dz),(.04,1.65,1.36),'Glass')
                    box('House side window transom',(x+side*(w/2+.1),y+.15,z+dz),(.035,.055,1.4),'Metal')
            for dx in [-w*.23,w*.23]:
                box('House rear window surround',(x+dx,y,z-d/2-.025),(w*.32,1.9,.10),'Plaster')
                box('House rear window glazing',(x+dx,y,z-d/2-.09),(w*.28,1.66,.05),'Glass')
                box('House rear window mullion',(x+dx,y,z-d/2-.13),(.055,1.66,.035),'Plaster')
            box('Rear rain ledge',(x,y+1.08,z-d/2-.3),(w-.3,.12,.7),'Concrete')
        box('House rear service door',(x,1.1,z-d/2-.07),(1.2,2.2,.1),'SilverCladding')
        box('Rear downpipe',(x-w*.41,height*.5,z-d/2-.16),(.09,height,.09),'Metal')
        if index%4==1:
            # Two low corrugated pitches; water tank/solar rooftops remain varied.
            rise=.9; half=w/2+.22;length=math.sqrt(half*half+rise*rise)
            for side in [-1,1]:
                roof=box('Neighborhood pitched metal roof',(x+side*half/2,height+.48,z),
                         (length,.10,d+.5),'Terracotta' if index%3 else 'SilverCladding')
                roof.rotation_euler.y=-side*math.atan2(rise,half)
            box('Neighborhood roof ridge',(x,height+.98,z),(.15,.12,d+.6),'SilverCladding')
