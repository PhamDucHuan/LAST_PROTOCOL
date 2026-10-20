"""Adapt licensed source art and replace four existing parcels, revision 9 -> 10."""
import bpy, bmesh, json, math, os, re, shutil, sys
import numpy as np
from mathutils import Vector, Matrix

sys.dont_write_bytecode = True
root, task, output = map(os.path.abspath, sys.argv[sys.argv.index('--')+1:][:3])
if os.path.exists(output): raise RuntimeError('Candidate already exists; use a new candidate.')
os.makedirs(os.path.join(output, 'Textures'))
source = os.path.join(root, 'ArtSource/Blender/CanThoCampus')
assets = os.path.join(root, 'ArtSource/ThirdParty/VietnamFreeAssets')
for f in os.listdir(os.path.join(source,'Textures')):
    shutil.copy2(os.path.join(source,'Textures',f),os.path.join(output,'Textures',f))
with open(os.path.join(source,'CanThoCampus.layout.json'),encoding='utf-8') as f: layout=json.load(f)
if layout['artRevision'] != 9: raise ValueError('Requires revision 9.')
report = {'sourceRevision':9, 'artRevision':10, 'sources':[], 'placements':[], 'removedObjects':[]}
palettes, material_cache, templates = [], {}, {}

def triangles(objects): return sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in objects)
def bounds(objects):
    pts=[o.matrix_world @ Vector(v) for o in objects for v in o.bound_box]
    return Vector([min(p[i] for p in pts) for i in range(3)]),Vector([max(p[i] for p in pts) for i in range(3)])
def simplify(o, ratio):
    mod=o.modifiers.new('Game mesh simplification','DECIMATE');mod.ratio=max(.005,min(1,ratio))
    dg=bpy.context.evaluated_depsgraph_get()
    data=bpy.data.meshes.new_from_object(o.evaluated_get(dg),preserve_all_data_layers=True,depsgraph=dg)
    o.modifiers.remove(mod);o.data=data

def surface(old, family):
    key=family+'_'+('Atlas' if family=='Pho' else re.sub(r'[^A-Za-z0-9_]','_',old.name))
    if key in material_cache:return material_cache[key]
    name='CT10_'+key
    mat=bpy.data.materials.new(name);mat.use_nodes=True
    p=mat.node_tree.nodes.get('Principled BSDF')
    oldp=next((n for n in old.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None) if old.use_nodes else None
    color=list(oldp.inputs['Base Color'].default_value if oldp else old.diffuse_color)
    rough=float(oldp.inputs['Roughness'].default_value) if oldp else .8
    metal=float(oldp.inputs['Metallic'].default_value) if oldp else 0
    p.inputs['Base Color'].default_value=color;p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=metal
    entry=dict(name=name,color=dict(zip(('r','g','b','a'),color)),roughness=rough,metallic=metal,emission=0,normalScale=.55)
    nodes=[n for n in old.node_tree.nodes if n.type=='TEX_IMAGE' and n.image] if old.use_nodes else []
    for channel,terms in [('Base',('basecolor','base_color','base color','albedo','diffuse','_bc','palette','color')),('Normal',('normal','norm_'))]:
        image=next((n.image for n in nodes if any(t in (n.image.name+' '+n.label).lower() for t in terms)
                    and (channel=='Normal' or not any(t in n.image.name.lower() for t in ('rough','height','metal','normal','ambient')))),None)
        if channel=='Base' and oldp and oldp.inputs['Base Color'].is_linked:
            upstream=oldp.inputs['Base Color'].links[0].from_node
            if upstream.type=='TEX_IMAGE' and upstream.image:image=upstream.image
        if not image or min(image.size)==0:continue
        resized=image.copy()
        if max(resized.size)>1024:resized.scale(max(1,round(resized.size[0]*1024/max(resized.size))),max(1,round(resized.size[1]*1024/max(resized.size))))
        im=bpy.data.images.new(name+'_'+channel,width=resized.size[0],height=resized.size[1],alpha=True)
        im.colorspace_settings.name='Non-Color' if channel=='Normal' else 'sRGB'
        im.pixels.foreach_set(np.asarray(resized.pixels,dtype=np.float32))
        bpy.data.images.remove(resized)
        im.filepath_raw=os.path.join(output,'Textures',im.name+'.png');im.file_format='PNG';im.save();im.pack();im.filepath='//Textures/'+im.name+'.png'
        tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=im
        if family=='Pho':tex.interpolation='Closest'
        if channel=='Base':
            mat.node_tree.links.new(tex.outputs['Color'],p.inputs['Base Color']);entry['baseMap']='Textures/'+im.name+'.png'
            p.inputs['Base Color'].default_value=(1,1,1,1);entry['color']=dict(r=1,g=1,b=1,a=1)
        else:
            nm=mat.node_tree.nodes.new('ShaderNodeNormalMap');nm.inputs['Strength'].default_value=.55
            mat.node_tree.links.new(tex.outputs['Color'],nm.inputs['Color']);mat.node_tree.links.new(nm.outputs['Normal'],p.inputs['Normal']);entry['normalMap']='Textures/'+im.name+'.png'
    material_cache[key]=mat;palettes.append(entry);return mat

def prepare(family, filename, target):
    before=set(bpy.data.objects)
    if filename.endswith('.blend'):
        with bpy.data.libraries.load(filename,link=False) as (a,b):b.objects=list(a.objects)
        for o in b.objects:
            if o and o.type=='MESH':bpy.context.scene.collection.objects.link(o)
    elif filename.endswith('.glb'):bpy.ops.import_scene.gltf(filepath=filename)
    else:bpy.ops.import_scene.fbx(filepath=filename)
    objs=[o for o in bpy.context.scene.objects if o not in before and o.type=='MESH']
    if not objs:raise ValueError('No meshes: '+filename)
    for o in objs:
        o.data=o.data.copy();o.data.transform(o.matrix_world);o.matrix_world=Matrix.Identity(4)
        o.parent=None;o.matrix_world=Matrix.Identity(4)
    lo,hi=bounds(objs);center=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z))
    count=triangles(objs)
    for o in objs:
        if family=='Asian':
            # The walls/roof are already economical. Most source triangles are
            # in seven very dense metal gates: preserve the building shell.
            if not o.name.startswith('Base_house') and len(o.data.polygons)>12:simplify(o,.005)
        elif family!='HoiAn' and count>target and len(o.data.polygons)>12:simplify(o,target/count)
        o.data.transform(Matrix.Translation(-center))
    parts=[]
    for o in objs:
        for index,old in enumerate(list(o.data.materials)):
            if not old or not any(p.material_index==index for p in o.data.polygons):continue
            # The dense decorative tiles are separate from the continuous roof.
            # Keep the original roof base, walls and joinery intact at game LOD.
            if family=='HoiAn' and old.name.startswith('RoofTiles'):continue
            data=o.data.copy();bm=bmesh.new();bm.from_mesh(data)
            bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.material_index!=index],context='FACES')
            bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS')
            bm.to_mesh(data);bm.free();data.materials.clear();data.materials.append(surface(old,family))
            for p in data.polygons:p.material_index=0
            part=bpy.data.objects.new('__T_'+family+'_'+str(len(parts)),data);bpy.context.scene.collection.objects.link(part);parts.append(part)
        bpy.data.objects.remove(o,do_unlink=True)
    templates[family]=[o.name for o in parts]
    report['sources'].append(dict(family=family,originalTriangles=count,adaptedTriangles=triangles(parts),materials=len(set(o.data.materials[0].name for o in parts))))
    return parts

bpy.ops.wm.read_factory_settings(use_empty=True)
prepare('HoiAn',os.path.join(assets,'HoiAnHouse/HoiAnHouse_M2.blend'),6000)
prepare('Asian',os.path.join(assets,'AsianHouse/AsianHouse_HUY_TRAN_gltf.glb'),2300)
poly=os.path.join(task,'PolygonPackage/Assets/PolyOne/Polygon World - Vietnam')
trees=prepare('PolygonTree',os.path.join(poly,'Model/SM_Tree_v01.fbx'),700)
polyimage=bpy.data.images.load(os.path.join(poly,'Texture/Texture_Polygon World - Vietnam _BaseColor.png'))
for o in trees:
    mat=o.data.materials[0]
    if not mat.node_tree.nodes.get('Image Texture'):
        resized=polyimage.copy();resized.scale(1024,1024)
        im=bpy.data.images.new('CT10_PolygonTree_Base',width=1024,height=1024,alpha=True)
        im.pixels.foreach_set(np.asarray(resized.pixels,dtype=np.float32));bpy.data.images.remove(resized)
        im.filepath_raw=os.path.join(output,'Textures',im.name+'.png');im.file_format='PNG';im.save();im.pack();im.filepath='//Textures/'+im.name+'.png'
        tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=im;mat.node_tree.links.new(tex.outputs['Color'],mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
        next(p for p in palettes if p['name']==mat.name)['baseMap']='Textures/'+im.name+'.png'
phoroot=os.path.join(assets,'PhoDiningSet/Extracted/AsianStreetFoodPack_PhoDiningSet/GLB')
for kind,file,target in [('Table','SM_Furniture_TableLow_Red_01.glb',350),('Stool','SM_Furniture_StoolLow_Red_01.glb',360),('Bowl','SM_Cook_PhoBowl_Full_01.glb',420),('Condiments','SM_Cook_CondimentCaddy_01.glb',200)]:
    parts=prepare('Pho',os.path.join(phoroot,file),target)
    # Each prop shares the same palette material, but retains a separate template.
    templates['Pho'+kind]=templates.pop('Pho')
for o in list(bpy.context.scene.objects):
    if o.type!='MESH':bpy.data.objects.remove(o,do_unlink=True)
bank=os.path.join(output,'VietnamAssets.Prepared.blend')
bpy.ops.wm.save_as_mainfile(filepath=bank)

bpy.ops.wm.open_mainfile(filepath=os.path.join(source,'CanThoCampus.blend'),load_ui=False,use_scripts=False)
membership={o.name:c for c in bpy.data.collections for o in c.objects}
existing=list(bpy.context.scene.objects)
original_triangles=triangles([o for o in existing if o.type=='MESH'])
# Fewer sides on existing tree branches and fewer overlapping foliage cards free
# the budget for close-up architecture. Tree positions/colliders stay unchanged.
bark_cache={}
for o in existing:
    if o.type=='MESH' and o.data.materials and o.data.materials[0].name=='CT5_Bark' and len(o.data.polygons)>8:
        old=o.data
        if old.name not in bark_cache:
            simplify(o,.4);bark_cache[old.name]=o.data
        else:o.data=bark_cache[old.name]
leaves=[o for o in existing if o.type=='MESH' and o.data.materials and o.data.materials[0].name=='CT5_LeafCluster']
bpy.data.batch_remove(ids=tuple(leaves[3::4]))
report['treeOptimization']=dict(originalTriangles=original_triangles,removedOverlappingLeafCards=len(leaves[3::4]),treePositionsUnchanged=True)
collection=bpy.data.collections.new('68_VietnamImported');bpy.context.scene.collection.children.link(collection)
with bpy.data.libraries.load(bank,link=False) as (a,b):b.objects=[n for n in a.objects if n.startswith('__T_')]
bankobjs={o.name:o for o in b.objects if o}
for o in bankobjs.values():collection.objects.link(o);o.hide_render=True

def xyz(x,y,z):return dict(x=round(x,5),y=round(y,5),z=round(z,5))
def instantiate(family, name, x,z,w,d,h,front=(0,-1),ground=0):
    sourceobjs=[bankobjs[n] for n in templates[family]]
    lo,hi=bounds(sourceobjs);size=hi-lo;fx,fz=front;rx,rz=fz,-fx
    sx,sy,sz=w/size.x,d/size.y,h/size.z
    transform=Matrix(((-rx*sx,fx*sy,0,-x),(-rz*sx,fz*sy,0,-z),(0,0,sz,ground),(0,0,0,1)))
    instances=[]
    for i,o in enumerate(sourceobjs):
        data=o.data.copy();data.transform(transform)
        if transform.determinant()<0:data.flip_normals()
        instance=bpy.data.objects.new(name+'__'+str(i),data);collection.objects.link(instance);instances.append(instance)
    return instances

replacements=[('District house 181','HoiAn'),('District house 182','HoiAn'),('District house 044','Asian'),('District house 086','Asian')]
for house_name,family in replacements:
    house=next(h for h in layout['neighborhoodHouses'] if h['name']==house_name)
    old=json.loads(json.dumps(house));p,s,f=old['position'],old['size'],old['front'];fx,fz=f['x'],f['z'];rx,rz=fz,-fx
    group=membership[house_name]
    removed=[]
    for o in list(group.objects):
        if o.type!='MESH':continue
        lo,hi=bounds([o]);cx,cz=-(lo.x+hi.x)/2,-(lo.y+hi.y)/2
        dx,dz=cx-p['x'],cz-p['z'];a=dx*rx+dz*rz;c=dx*fx+dz*fz
        if abs(a)<s['x']/2+.65 and abs(c)<s['z']/2+1.1 and hi.z>0 and max(hi.x-lo.x,hi.y-lo.y)<s['z']+3:removed.append(o)
    report['removedObjects'].extend(o.name for o in removed);bpy.data.batch_remove(ids=tuple(removed))
    w=min(s['x']-.3,6.1);d=7.2 if family=='HoiAn' else s['z']-.3;h=5.1 if family=='HoiAn' else min(s['y'],13.2)
    x=p['x']+fx*(s['z']-d)/2;z=p['z']+fz*(s['z']-d)/2
    name='Vietnam '+family+' '+house_name
    objs=instantiate(family,name,x,z,w,d,h,(fx,fz))
    house.update(position=xyz(x,0,z),size=xyz(w,h,d))
    collision=next(c for c in layout['collisions'] if c['name']==house_name)
    collision.update(position=xyz(x,h/2,z),size=house['size'])
    report['placements'].append(dict(name=name,family=family,replaces=house_name,position=house['position'],size=house['size'],front=f,euler=house['euler'],triangles=triangles(objs),originalParcel=old))

# Dining sets replace the first three existing lakeside tables, keeping the old
# collision envelope so the established promenade and navigation remain clear.
for i,table in enumerate([c for c in layout['collisions'] if c['name']=='Lakeside table'][:3]):
    x,z=table['position']['x'],table['position']['z']
    instantiate('PhoTable','Vietnam Pho table '+str(i),x,z,1.15,.72,.72)
    for side in (-1,1):
        instantiate('PhoStool','Vietnam Pho stool '+str(i)+' '+str(side),x+side*.83,z,.35,.35,.35)
        instantiate('PhoBowl','Vietnam Pho bowl '+str(i)+' '+str(side),x+side*.28,z,.22,.22,.115,ground=.73)
    instantiate('PhoCondiments','Vietnam Pho condiments '+str(i),x,z,.24,.17,.22,ground=.73)
    report['placements'].append(dict(name='Vietnam Pho dining '+str(i),family='Pho',position=xyz(x,0,z),size=xyz(2,.95,.8),front=xyz(0,0,-1)))
    # Remove only procedural furniture in this exact existing table envelope.
    for o in list(bpy.context.scene.objects):
        if not o.name.startswith(('Lakeside table','Lakeside stool')) or o.type!='MESH':continue
        lo,hi=bounds([o]);cx,cz=-(lo.x+hi.x)/2,-(lo.y+hi.y)/2
        if abs(cx-x)<1.1 and abs(cz-z)<1.2:bpy.data.objects.remove(o,do_unlink=True)

# Use the low-poly pack's trees as landscape accents near the new houses.
for i,(x,z) in enumerate([(-246,-42),(-278,-43),(-226,254)]):
    instantiate('PolygonTree','Vietnam Polygon tree '+str(i),x,z,3,3,5)
    report['placements'].append(dict(name='Vietnam Polygon tree '+str(i),family='PolygonTree',position=xyz(x,0,z),size=xyz(3,5,3),front=xyz(0,0,-1)))
    layout['collisions'].append(dict(name='Vietnam Polygon trunk '+str(i),position=xyz(x,2,z),size=xyz(.5,4,.5)))
bpy.data.batch_remove(ids=tuple(bankobjs.values()))
newobjs=list(collection.objects)
total=triangles([o for o in bpy.context.scene.objects if o.type=='MESH'])
if total>=598500:
    ratio=max(.4,(triangles(newobjs)-(total-597500))/triangles(newobjs))
    for o in newobjs:
        if len(o.data.polygons)>40:simplify(o,ratio)
total=triangles([o for o in bpy.context.scene.objects if o.type=='MESH'])
if total>=600000:raise RuntimeError('Triangle budget exceeded: '+str(total))
layout['materials'].extend(palettes);layout['artRevision']=10
layout['note']='Revision10: licensed Vietnamese architecture and dining props on existing parcels. Original land, campus buildings, roads and water retained.'
layout['importedVietnamAssets']=report['placements']
layout['vietnamAssetRevision']=dict(sourceFamilies=4,houseReplacements=4,diningSets=3,trees=3,groundExpanded=False,sourceDate='2026-10-20')
report['triangles']=total;report['bounds']=layout['bounds']
with open(os.path.join(output,'CanThoCampus.layout.json'),'w',encoding='utf-8') as f:json.dump(layout,f,indent=2,ensure_ascii=False)
with open(os.path.join(output,'VietnamIntegration.json'),'w',encoding='utf-8') as f:json.dump(report,f,indent=2,ensure_ascii=False)
bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(output,'CanThoCampus.blend'),relative_remap=False)
sys.path.insert(0,os.path.dirname(__file__))
from FinishCanThoCampus import finish
finish(output,False)
print('VIETNAM_ASSET_INTEGRATION_PASS '+str(total),flush=True)
