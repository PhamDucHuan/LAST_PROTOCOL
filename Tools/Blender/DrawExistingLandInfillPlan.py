"""Standalone vector review of existing ground and the added building footprints."""
import html
import json
import math
import os
import sys

sys.dont_write_bytecode = True
sys.path.insert(0, os.path.dirname(__file__))
from PeripheralGeometry import rectangle

with open(sys.argv[1], encoding='utf-8') as f:
    layout = json.load(f)
with open(sys.argv[2], encoding='utf-8') as f:
    infill = json.load(f)
shapes = []


def points(p):
    return ' '.join('%.2f,%.2f' % (x+580, 470-z) for x, z in p)


def polygon(p, color, stroke='none'):
    shapes.append('<polygon points="'+points(p)+'" fill="'+color+'" stroke="'+stroke+'" stroke-width="1"/>')


shapes.append('<rect x="20" y="20" width="940" height="840" fill="#e7eadf" stroke="#223b35" stroke-width="3"/>')
polygon([(p['x'], p['z']) for p in layout['campusBoundary']], '#bfceb0', '#4e715b')
polygon([(p['x'], p['z']) for p in layout['waterBoundary']], '#91c4d3')
for r in layout['peripheralRoads']+layout['localLanes']:
    shapes.append('<polyline points="'+points([(p['x'], p['z']) for p in r['points']])+'" fill="none" stroke="#627070" stroke-width="'+str(r['width'])+'" stroke-linejoin="round"/>')
for h in layout['neighborhoodHouses']:
    p, s, f = h['position'], h['size'], h['front']
    polygon(rectangle(p['x'], p['z'], s['x'], s['z'], (f['x'], f['z'])), '#8d9699')
for h in infill['houses']:
    polygon(rectangle(h['x'], h['z'], h['w'], h['d'], h['front']), '#b56942')
for landmark in layout['landmarks']:
    p = landmark['position']
    shapes.append('<circle cx="%.2f" cy="%.2f" r="3" fill="#3d6651"/>' % (p['x']+580, 470-p['z']))
labels = [('3 THANG 2', -80, -64), ('BUNG XANG', -265, 70), ('MAC THIEN TICH', 80, -130),
          ('TRAN VAN HOAI', -30, -280), ('MAU THAN', 225, 70), ('NGUYEN VAN CU', -120, 413)]
for text, x, z in labels:
    shapes.append('<text x="%s" y="%s" font-size="12" fill="#122c2e" paint-order="stroke" stroke="#f7faf3" stroke-width="3">%s</text>' % (x+580, 470-z, text))
document = '<svg xmlns="http://www.w3.org/2000/svg" width="1080" height="1040" viewBox="0 0 1000 970">'
document += '<rect width="1000" height="970" fill="#f7f8f3"/>'+''.join(shapes)
document += '<text x="22" y="895" font-family="sans-serif" font-size="22" fill="#17332d">LAST PROTOCOL / EXISTING LAND INFILL</text>'
document += '<text x="22" y="922" font-family="sans-serif" font-size="15" fill="#5e6663">1100 new buildings (ochre) / 251 existing buildings (grey) / original 940 x 840 m ground</text>'
document += '<text x="22" y="945" font-family="sans-serif" font-size="13" fill="#5e6663">Roads, campus and water retained. Google Maps visual reference; gameplay scale, not surveyed parcels.</text></svg>'
with open(sys.argv[3], 'w', encoding='utf-8') as f:
    f.write(document)
print('INFILL_PLAN_SVG '+sys.argv[3])
