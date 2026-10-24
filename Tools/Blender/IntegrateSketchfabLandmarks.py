"""Fit three licensed Sketchfab buildings into existing campus parcels."""
import bpy, bmesh, json, math, os, re, shutil, sys
from mathutils import Matrix, Vector
sys.dont_write_bytecode=True
sys.path.insert(0,os.path.dirname(__file__))
from PeripheralGeometry import polygon_near_segment
root, output=map(os.path.abspath,sys.argv[sys.argv.index('--')+1:][:2])
if os.path.exists(output):raise RuntimeError('Use a new candidate directory.')
os.makedirs(output)
source=os.path.join(root,'ArtSource/Blender/CanThoCampus')
shutil.copytree(os.path.join(source,'Textures'),os.path.join(output,'Textures'))
with open(os.path.join(source,'CanThoCampus.layout.json'),encoding='utf-8') as f:layout=json.load(f)
assert layout['artRevision']==11 and not layout.get('sketchfabLandmarks')
report={'sourceRevision':11,'artRevision':12,'placements':[],'removedObjects':[]}
palettes=[]; material_cache={}; images={}; templates={}
def bounds(objects):
    pts=[o.matrix_world @ v.co for o in objects for v in o.data.vertices]
    return Vector([min(p[i] for p in pts) for i in range(3)]),Vector([max(p[i] for p in pts) for i in range(3)])
def xyz(x,y,z):return dict(x=round(x,5),y=round(y,5),z=round(z,5))
def box(name,lo,hi):return dict(name=name,position=xyz((lo.x+hi.x)/2,(lo.z+hi.z)/2,(lo.y+hi.y)/2),size=xyz(hi.x-lo.x,hi.z-lo.z,hi.y-lo.y))
def material(old,family):
    key=(family,old.name)
    if key in material_cache:return material_cache[key]
    name='CT12_'+family+'_'+re.sub('[^A-Za-z0-9_]','_',old.name)
    mat=bpy.data.materials.new(name);mat.use_nodes=True
    p=mat.node_tree.nodes.get('Principled BSDF')
    oldp=next(n for n in old.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    color=list(oldp.inputs['Base Color'].default_value)
    p.inputs['Base Color'].default_value=color;p.inputs['Roughness'].default_value=.82
    entry=dict(name=name,color=dict(zip(('r','g','b','a'),color)),roughness=.82,metallic=0,emission=0,doubleSided=True)
    if oldp.inputs['Base Color'].is_linked:
        upstream=oldp.inputs['Base Color'].links[0].from_node
        if upstream.type=='TEX_IMAGE' and upstream.image:
            original=upstream.image; ikey=(family,original.name)
            if ikey not in images:
                im=original.copy();im.name='CT12_'+family+'_'+re.sub('[^A-Za-z0-9_]','_',original.name)
                im.filepath_raw=os.path.join(output,'Textures',im.name+'.png');im.file_format='PNG';im.save();im.pack();im.filepath='//Textures/'+im.name+'.png';images[ikey]=im
            im=images[ikey];tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=im
            mat.node_tree.links.new(tex.outputs['Color'],p.inputs['Base Color']);entry['baseMap']='Textures/'+im.name+'.png'
    material_cache[key]=mat;palettes.append(entry);return mat

bpy.ops.wm.read_factory_settings(use_empty=True)
for family,group in [('HoiTruongRua','group_8'),('NhaHocA3','SketchUp'),('NhaThiDauTDTT','group_4')]:
    previous=set(bpy.context.scene.objects)
    bpy.ops.import_scene.gltf(filepath=os.path.join(root,'ArtSource/ThirdParty/CanThoLandmarks',family+'_AnLe.glb'))
    # Names are unique per import because the preceding hierarchy is removed.
    target=next(o for o in bpy.context.scene.objects if o not in previous and o.name==group)
    excluded={o for o in target.children_recursive if o.name.startswith('instance_')}
    excluded.update(x for p in list(excluded) for x in p.children_recursive)
    selected=[o for o in target.children_recursive if o.type=='MESH' and len(o.data.polygons) and o not in excluded]
    a3groups={o.name:[x.name for x in o.children_recursive if x in selected] for o in target.children if o.type=='EMPTY' and not o.name.startswith('instance_')}
    if family=='NhaHocA3':a3groups['connector']=[o.name for o in selected if o.parent==target]
    subgroups={}
    if family=='HoiTruongRua':
        for name in ['group_9','group_10']:
            g=bpy.data.objects[name];subgroups[name]=[o.name for o in g.children_recursive if o in selected]
    parts=[]; mapping={}
    for o in selected:
        converted=[]
        for index,old in enumerate(o.data.materials):
            if not old or not any(p.material_index==index for p in o.data.polygons):continue
            data=o.data.copy();bm=bmesh.new();bm.from_mesh(data)
            bmesh.ops.delete(bm,geom=[p for p in bm.faces if p.material_index!=index],context='FACES')
            bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS');bm.to_mesh(data);bm.free()
            data.transform(o.matrix_world)
            if o.matrix_world.determinant()<0:data.flip_normals()
            data.materials.clear();data.materials.append(material(old,family))
            for p in data.polygons:p.material_index=0
            part=bpy.data.objects.new('__CT12_'+family+'_'+str(len(parts)),data);bpy.context.scene.collection.objects.link(part);parts.append(part);converted.append(part.name)
        mapping[o.name]=converted
    def mapped(names):return [n for name in names for n in mapping.get(name,[])]
    templates[family]=dict(objects=[o.name for o in parts],wings={n:mapped(names) for n,names in a3groups.items()},subgroups={n:mapped(names) for n,names in subgroups.items()})
    bpy.data.batch_remove(ids=tuple(o for o in bpy.context.scene.objects if o not in previous and o not in parts))
bank=os.path.join(output,'Landmarks.Prepared.blend');bpy.ops.wm.save_as_mainfile(filepath=bank)

bpy.ops.wm.open_mainfile(filepath=os.path.join(source,'CanThoCampus.blend'),load_ui=False,use_scripts=False)
for cname in ['24_TurtleAuditorium','05_Classrooms_A3','25_Sports']:
    old=list(bpy.data.collections[cname].objects);report['removedObjects'].extend(o.name for o in old);bpy.data.batch_remove(ids=tuple(old))
with bpy.data.libraries.load(bank,link=False) as (a,b):b.objects=[n for n in a.objects if n.startswith('__CT12_')]
bankobjects={o.name:o for o in b.objects}
newcolliders=[]
for family,cname,x,z,width,depth,height in [
    ('HoiTruongRua','24_TurtleAuditorium',-45.5,12.5,26,21,8.0),
    ('NhaHocA3','05_Classrooms_A3',96,-18.3,30,14.5,4.2),
    ('NhaThiDauTDTT','25_Sports',70,130,46,25.5,None)]:
    parts=[bankobjects[n] for n in templates[family]['objects']]
    for o in parts:bpy.data.collections[cname].objects.link(o)
    lo,hi=bounds(parts);size=hi-lo;cx,cy=(lo.x+hi.x)/2,(lo.y+hi.y)/2
    rotated=family=='HoiTruongRua'
    scale=min(width/(size.y if rotated else size.x),depth/(size.x if rotated else size.y))
    sz=height/size.z if height else scale
    # Source axes are baked once; destination Blender (-X,-Y,Z) maps to Unity (X,Z,Y).
    transform=Matrix(((0,-scale,0,-x+cy*scale),(scale,0,0,-z-cx*scale),(0,0,sz,-lo.z*sz),(0,0,0,1))) if rotated else Matrix(((-scale,0,0,-x+cx*scale),(0,-scale,0,-z+cy*scale),(0,0,sz,-lo.z*sz),(0,0,0,1)))
    for o in parts:o.data.transform(transform)
    a,b=bounds(parts)
    umin=Vector((-b.x,-b.y,a.z));umax=Vector((-a.x,-a.y,b.z))
    # In box(), axes are (Unity X, Unity Z, Unity Y).
    def collision(name,names):
        aa,bb=bounds([bankobjects[n] for n in names]);lower=Vector((-bb.x,-bb.y,max(0,aa.z)));upper=Vector((-aa.x,-aa.y,bb.z))
        return box(name,lower,upper)
    colliders=[]
    if family=='HoiTruongRua':
        for name,sub in [('Turtle auditorium body','group_9'),('Turtle Hall roof proxy','group_10')]:colliders.append(collision(name,templates[family]['subgroups'][sub]))
    elif family=='NhaHocA3':
        for i,(wing,names) in enumerate(templates[family]['wings'].items()):
            if names:colliders.append(collision('Sketchfab A3 wing '+str(i+1),names))
    else:colliders.append(collision('Sketchfab Sports hall',templates[family]['objects']))
    newcolliders.extend(colliders)
    entry=dict(family=family,collection=cname,position=xyz(x,0,z),size=xyz(umax.x-umin.x,umax.z-umin.z,umax.y-umin.y),horizontalScale=scale,verticalScale=sz,
               footprint=[dict(x=xx,z=zz) for xx,zz in [(umin.x,umin.y),(umax.x,umin.y),(umax.x,umax.y),(umin.x,umax.y)]],
               triangles=sum(len(p.vertices)-2 for o in parts for p in o.data.polygons),parts=len(parts),colliders=colliders)
    poly=[(p['x'],p['z']) for p in entry['footprint']]
    for road in layout['roads']:
        assert not polygon_near_segment(poly,(road['a']['x'],road['a']['z']),(road['b']['x'],road['b']['z']),road['width']/2+2), (family,entry['footprint'],road)
    report['placements'].append(entry)
    for i,o in enumerate(parts):o.name='Imported '+family+' '+str(i)

layout['collisions']=[c for c in layout['collisions'] if c['name'] not in ['NHÀ HỌC A3','Gym','Turtle auditorium body'] and not c['name'].startswith('Turtle Hall ')]+newcolliders
layout['materials'].extend(palettes);layout['artRevision']=12
layout['sketchfabLandmarks']=report['placements']
layout['landmarkImportRevision']=dict(revision=1,author='AnLe',license='CC BY 4.0',groundExpanded=False)
layout['note']='Revision12: licensed AnLe Turtle Hall, A3 classroom complex, and new sports hall replace only their existing parcels. Original roads, land, trees, other buildings and gameplay anchors retained.'
report['groundExpanded']=False
for name,data in [('CanThoCampus.layout.json',layout),('SketchfabIntegration.json',report)]:
    with open(os.path.join(output,name),'w',encoding='utf-8') as f:json.dump(data,f,indent=2,ensure_ascii=False)
bpy.context.view_layer.update();bpy.ops.wm.save_as_mainfile(filepath=os.path.join(output,'CanThoCampus.blend'),relative_remap=False)
from FinishCanThoCampus import finish
finish(output,False)
print('SKETCHFAB_LANDMARK_INTEGRATION_PASS',[(p['family'],p['size'],p['triangles']) for p in report['placements']],flush=True)
