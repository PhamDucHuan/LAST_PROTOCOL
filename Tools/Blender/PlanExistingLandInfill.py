"""Add parcels inside the revision8 ground only; retain every road and open campus."""
import collections
import json
import math
import os
import random
import sys

sys.dont_write_bytecode = True
sys.path.insert(0, os.path.dirname(__file__))
from PeripheralGeometry import inside, rectangle, overlap, polygon_near_segment, distance_segment


class Parcels:
    def __init__(self, cell=24):
        self.cell = cell
        self.buckets = collections.defaultdict(list)
        self.polygons = []

    def cells(self, poly, margin=2):
        a = [math.floor((min(p[i] for p in poly)-margin)/self.cell) for i in (0, 1)]
        b = [math.floor((max(p[i] for p in poly)+margin)/self.cell) for i in (0, 1)]
        return [(x, z) for x in range(a[0], b[0]+1) for z in range(a[1], b[1]+1)]

    def add(self, poly):
        index = len(self.polygons)
        self.polygons.append(poly)
        for cell in self.cells(poly):
            self.buckets[cell].append(index)

    def intersects(self, poly, margin=1.05):
        indices = {i for cell in self.cells(poly, margin+2) for i in self.buckets[cell]}
        return any(overlap(poly, self.polygons[i], margin) for i in indices)


def house_lot(h):
    p, s, f = h['position'], h['size'], h['front']
    return rectangle(p['x']+f['x']*1.5, p['z']+f['z']*1.5,
                     s['x']+.4, s['z']+3, (f['x'], f['z']))


def plan(layout):
    if layout['artRevision'] != 8:
        raise ValueError('This additive plan requires the existing revision8 layout.')
    rng = random.Random(20261020)
    land = next(c for c in layout['collisions'] if c['name'] == 'Campus soil')
    p, s = land['position'], land['size']
    limits = [p['x']-s['x']/2+7, p['x']+s['x']/2-7,
              p['z']-s['z']/2+7, p['z']+s['z']/2-7]
    campus = [(p['x'], p['z']) for p in layout['campusBoundary']]
    water = [(p['x'], p['z']) for p in layout['waterBoundary']]
    streets = [(r['id'], r['width'], [(p['x'], p['z']) for p in r['points']])
               for r in layout['peripheralRoads']]
    lanes = [(r['width'], [(p['x'], p['z']) for p in r['points']]) for r in layout['localLanes']]
    road_segments = [(a, b, w/2+2.15) for _, w, points in streets for a, b in zip(points, points[1:])]
    lane_segments = [(a, b, w/2+1.2) for w, points in lanes for a, b in zip(points, points[1:])]
    route_segments = []
    for key in ('neighborhoodRoute', 'playableRoute'):
        pts = [(p['x'], p['z']) for p in layout[key]]
        route_segments.extend((a, b, 3) for a, b in zip(pts, pts[1:]))
    reserve_points = [(p['x'], p['z']) for key in ('neighborhoodChecks', 'navigationChecks') for p in layout[key]]
    probe = layout['neighborhoodCameraProbe']
    reserve_points.append((probe['x'], probe['z']))
    occupied = Parcels()
    for h in layout['neighborhoodHouses']:
        occupied.add(house_lot(h))
    for c in layout['collisions']:
        if c['name'] in ('Campus soil', 'Bung Xang water exclusion') or c['name'].startswith('District house'):
            continue
        if c['size']['y'] < .35:
            continue
        p, s = c['position'], c['size']
        yaw = math.radians(c.get('euler', {}).get('y', 0))
        occupied.add(rectangle(p['x'], p['z'], s['x'], s['z'], (math.sin(yaw), math.cos(yaw))))

    # Broad blocks traced by eye from Maps: compact tube houses behind frontages,
    # long rental rows, and the developed bank outside the existing water buffer.
    # These are gameplay parcels, not cadastral footprints or real addresses.
    patches = [
        ('bung_xang_south', (-547, -279, -376, 68), (1, 0)),
        ('bung_xang_west_bank', (-548, -439, 80, 430), (1, 0)),
        ('three_february_back_rows', (-276, -78, -376, -89), (0, 1)),
        ('mac_thien_tich_blocks', (-75, 190, -376, -87), (.6, .8)),
        ('nguyen_van_cu_back_rows', (-260, 193, 219, 435), (0, 1)),
        ('mau_than_back_rows', (195, 365, -374, 429), (-1, 0)),
        ('hem51_remaining_parcels', (-276, -85, -44, 212), (1, 0)),
        ('northwest_bank', (-437, -263, 386, 435), (0, -1)),
    ]
    queues = []
    for patch_id, bounds, front in patches:
        x0, x1, z0, z1 = bounds
        fx, fz = front
        rx, rz = fz, -fx
        cx, cz = (x0+x1)/2, (z0+z1)/2
        span = int(math.hypot(x1-x0, z1-z0)/2)+30
        candidates = []
        row = 0
        for v in range(-span, span+1, 23):
            row += 1
            for col, u in enumerate(range(-span, span+1, 11)):
                # Unbuilt cross-passages make dense rows permeable on foot.
                if col % 12 == 11:
                    continue
                x = cx+rx*(u+(row % 2)*.7)+fx*v
                z = cz+rz*(u+(row % 2)*.7)+fz*v
                w, d = rng.choice([7.2, 8.1, 8.8]), rng.choice([13, 15, 16.5])
                floors = rng.choices([1, 2, 3, 4], [12, 42, 39, 7])[0]
                h = dict(x=x, z=z, w=w, d=d, height=3.15*floors+.35,
                         floors=floors, front=front, patch=patch_id,
                         roof=rng.choices(['red_sheet', 'blue_sheet', 'silver_sheet', 'terrace'], [38, 16, 17, 29])[0],
                         facade=rng.randrange(9))
                poly = rectangle(x+fx*1.5, z+fz*1.5, w+.4, d+3, front)
                if any(not (x0 < a < x1 and z0 < b < z1) for a, b in poly):
                    continue
                candidates.append((h, poly))
        # Spread additions across the block, avoiding a single dense edge.
        rng.shuffle(candidates)
        queues.append(candidates)

    houses = []
    reasons = collections.Counter()
    for index in range(max(map(len, queues))):
        for queue in queues:
            if index >= len(queue) or len(houses) >= 1100:
                continue
            h, poly = queue[index]
            if any(not (limits[0] < x < limits[1] and limits[2] < z < limits[3]) for x, z in poly):
                reasons['existing_ground_edge'] += 1
                continue
            invalid = False
            for boundary, clearance in ((campus, 6), (water, 10)):
                if (any(inside(p, boundary) for p in poly)
                        or any(polygon_near_segment(poly, a, b, clearance)
                               for a, b in zip(boundary, boundary[1:]+boundary[:1]))):
                    invalid = True
                    break
            if invalid:
                reasons['campus_or_water'] += 1
                continue
            if any(polygon_near_segment(poly, a, b, r) for a, b, r in road_segments+lane_segments+route_segments):
                reasons['retained_route'] += 1
                continue
            if any(inside(p, poly) or min(distance_segment(p, a, b) for a, b in zip(poly, poly[1:]+poly[:1])) < 2.3 for p in reserve_points):
                reasons['navigation_probe'] += 1
                continue
            if occupied.intersects(poly):
                reasons['existing_building_or_prop'] += 1
                continue
            h['district'] = min(streets, key=lambda r: min(distance_segment((h['x'], h['z']), a, b) for a, b in zip(r[2], r[2][1:])))[0]
            occupied.add(poly)
            houses.append(h)
    if len(houses) < 650:
        raise RuntimeError('Insufficient safe existing-land parcels: '+str(len(houses)))
    return dict(houses=houses, groundBounds=layout['bounds'],
                originalHouses=len(layout['neighborhoodHouses']),
                patches=dict(collections.Counter(h['patch'] for h in houses)),
                rejected=dict(reasons),
                note='Visual reference from Google Maps on 2026-10-20. All parcels remain within original ground. No new roads or terrain.')


if __name__ == '__main__':
    with open(sys.argv[1], encoding='utf-8') as f:
        layout = json.load(f)
    result = plan(layout)
    with open(sys.argv[2], 'w', encoding='utf-8') as f:
        json.dump(result, f, indent=2, ensure_ascii=False)
    print('EXISTING_LAND_PLAN '+json.dumps({k: v for k, v in result.items() if k != 'houses'}, ensure_ascii=False))
    print('SAFE_ADDITIONS '+str(len(result['houses'])))
