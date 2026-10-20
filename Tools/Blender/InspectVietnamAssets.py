"""Read-only survey of downloaded art before adapting it to the campus."""
import bpy, json, os, sys, tarfile, collections
from mathutils import Vector

root, out = map(os.path.abspath, sys.argv[sys.argv.index('--')+1:][:2])
os.makedirs(out, exist_ok=True)
report = {}

def inspect(label, filename):
    bpy.ops.wm.open_mainfile(filepath=filename, load_ui=False, use_scripts=False)
    objects = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    membership = {o.name:c.name for c in bpy.data.collections for o in c.objects}
    tris = {m.name:sum(len(p.vertices)-2 for p in m.polygons) for m in {o.data for o in objects}}
    groups = collections.Counter()
    for o in objects:
        groups[membership.get(o.name,'-')] += tris[o.data.name]
    info = {'file': filename, 'groups': dict(groups.most_common()), 'objects': []}
    for o in objects:
        points = [o.matrix_world @ Vector(p) for p in o.bound_box]
        info['objects'].append(dict(name=o.name, collection=membership.get(o.name,'-'),
            triangles=tris[o.data.name],
            min=[min(p[i] for p in points) for i in range(3)], max=[max(p[i] for p in points) for i in range(3)],
            materials=[m.name if m else None for m in o.data.materials]))
    info['materials'] = []
    for m in bpy.data.materials:
        if m.users:
            info['materials'].append({'name':m.name, 'images': [dict(name=n.image.name, path=n.image.filepath, packed=bool(n.image.packed_file), size=list(n.image.size)) for n in m.node_tree.nodes if n.type=='TEX_IMAGE' and n.image] if m.use_nodes else []})
    report[label] = info

base = os.path.join(root, 'ArtSource/ThirdParty/VietnamFreeAssets')
package = os.path.join(base, 'PolygonWorldVietnam/Polygon World - Vietnam.unitypackage')
polyout = os.path.join(out, 'PolygonPackage')
os.makedirs(polyout, exist_ok=True)
with tarfile.open(package) as archive:
    members = {m.name:m for m in archive.getmembers()}
    paths = []
    for n,m in members.items():
        if n.endswith('/pathname'):
            rawpath = archive.extractfile(m).read().decode('utf-8').strip('\x00\r\n ')
            path = rawpath.splitlines()[0].rstrip('\x00')
            if '..' in path.replace('\\','/').split('/') or not path.startswith('Assets/'):
                raise ValueError(path)
            assetname = n.rsplit('/',1)[0]+'/asset'
            if assetname in members:
                dest = os.path.join(polyout,path)
                os.makedirs(os.path.dirname(dest),exist_ok=True)
                with open(dest,'wb') as f: f.write(archive.extractfile(members[assetname]).read())
                paths.append(path)
    report['polygonFiles'] = paths

inspect('hoiAn', os.path.join(base,'HoiAnHouse/HoiAnHouse_M2.blend'))
inspect('asianHouse', os.path.join(base,'AsianHouse/AsianHouse_HUY_TRAN_blend.blend'))
inspect('campus', os.path.join(root,'ArtSource/Blender/CanThoCampus/CanThoCampus.blend'))
with open(os.path.join(out,'AssetSurvey.json'),'w',encoding='utf-8') as f: json.dump(report,f,indent=2,ensure_ascii=False)
print('ASSET_SURVEY_PASS', flush=True)
