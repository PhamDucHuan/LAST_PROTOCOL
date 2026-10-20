"""Prepare the twenty architectural variants and Town House for game use."""
import bpy, bmesh, json, math, os, sys, collections
import numpy as np
from mathutils import Vector, Matrix
sys.dont_write_bytecode=True
root,out=map(os.path.abspath,sys.argv[sys.argv.index('--')+1:][:2])
os.makedirs(os.path.join(out,'Textures'),exist_ok=True)
assets=os.path.join(root,'ArtSource/ThirdParty/VietnamFreeAssets')
palettes=[];report=[];mirrored=0
def triangles(o):return sum(len(p.vertices)-2 for p in o.data.polygons)
def bounds(obs):
    pts=[o.matrix_world@Vector(p) for o in obs for p in o.bound_box]
    return Vector([min(p[i] for p in pts) for i in range(3)]),Vector([max(p[i] for p in pts) for i in range(3)])
def material(name,base,normal=None):
    mat=bpy.data.materials.new(name);mat.use_nodes=True;p=mat.node_tree.nodes.get('Principled BSDF');p.inputs['Roughness'].default_value=.82
    entry=dict(name=name,color=dict(r=1,g=1,b=1,a=1),roughness=.82,metallic=0,emission=0,normalScale=.6)
    for channel,path in [('Base',base),('Normal',normal)]:
        if not path:continue
        original=bpy.data.images.load(path,check_existing=True)
        resized=original.copy()
        if max(resized.size)>1024:resized.scale(1024,1024)
        im=bpy.data.images.new(name+'_'+channel,width=resized.size[0],height=resized.size[1],alpha=True)
        im.colorspace_settings.name='Non-Color' if channel=='Normal' else 'sRGB'
        im.pixels.foreach_set(np.asarray(resized.pixels,dtype=np.float32));bpy.data.images.remove(resized)
        im.filepath_raw=os.path.join(out,'Textures',im.name+'.png');im.file_format='PNG';im.save();im.pack();im.filepath='//Textures/'+im.name+'.png'
        tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=im
        if name.endswith('Palette'):tex.interpolation='Closest'
        if channel=='Base':mat.node_tree.links.new(tex.outputs['Color'],p.inputs['Base Color']);entry['baseMap']='Textures/'+im.name+'.png'
        else:
            nm=mat.node_tree.nodes.new('ShaderNodeNormalMap');nm.inputs['Strength'].default_value=.6
            mat.node_tree.links.new(tex.outputs['Color'],nm.inputs['Color']);mat.node_tree.links.new(nm.outputs['Normal'],p.inputs['Normal']);entry['normalMap']='Textures/'+im.name+'.png'
    palettes.append(entry);return mat
def planar(o):
    bm=bmesh.new();bm.from_mesh(o.data)
    bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.00001)
    bmesh.ops.dissolve_limit(bm,angle_limit=math.radians(.25),use_dissolve_boundaries=False,verts=list(bm.verts),edges=list(bm.edges),delimit={'UV','NORMAL'})
    bm.to_mesh(o.data);bm.free();o.data.update()
def reduce_detail(o,ratio):
    mod=o.modifiers.new('Game facade detail','DECIMATE');mod.ratio=ratio
    graph=bpy.context.evaluated_depsgraph_get()
    mesh=bpy.data.meshes.new_from_object(o.evaluated_get(graph),preserve_all_data_layers=True,depsgraph=graph)
    o.modifiers.remove(mod);o.data=mesh
def merge(name,obs,mat):
    vertices=[];faces=[];uvs=[]
    for o in obs:
        offset=len(vertices);vertices.extend(tuple(v.co) for v in o.data.vertices)
        faces.extend(tuple(offset+i for i in p.vertices) for p in o.data.polygons)
        uv=o.data.uv_layers.active
        uvs.extend(tuple(uv.data[i].uv) if uv else (0,0) for i in range(len(o.data.loops)))
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(vertices,[],faces);mesh.materials.append(mat)
    uv=mesh.uv_layers.new(name='UVMap')
    for i,p in enumerate(uvs):uv.data[i].uv=p
    obj=bpy.data.objects.new(name,mesh);bpy.context.scene.collection.objects.link(obj)
    lo,hi=bounds([obj]);mesh.transform(Matrix.Translation(Vector((-(lo.x+hi.x)/2,-(lo.y+hi.y)/2,-lo.z))))
    bpy.data.batch_remove(ids=tuple(obs));return obj
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=os.path.join(assets,'ModernHouses20/LowPoly_Modern_Houses_fbx.FBX'))
objs=[o for o in bpy.context.scene.objects if o.type=='MESH'];groups=collections.defaultdict(list)
for o in objs:
    world=o.matrix_world.copy();o.data=o.data.copy();o.data.transform(world)
    if world.determinant()<0:o.data.flip_normals();mirrored+=1
    o.parent=None;o.matrix_world=Matrix.Identity(4)
bpy.context.view_layer.update()
for o in objs:
    lo,hi=bounds([o]);groups[(round((lo.x+hi.x)/50)*25,round((lo.y+hi.y)/50)*25)].append(o)
assert len(groups)==20,len(groups)
mat=material('CT11_ModernPalette',os.path.join(assets,'ModernHouses20/Extracted/textures/color_512x512.jpg'))
for i,(anchor,parts) in enumerate(sorted(groups.items()),1):
    before=sum(triangles(o) for o in parts);lo,hi=bounds(parts);kept=[];removed=[]
    for o in parts:
        a,b=bounds([o]);s=b-a
        # The authored gardens, low bollards, pots and base plates are separate.
        # Keep actual architecture and roof/door/window parts without collapsing walls.
        if b.z<lo.z+1.9 or (max(s.x,s.y)<1.3 and s.z<2.4):removed.append(o);continue
        planar(o)
        count=triangles(o)
        if count>120:reduce_detail(o,.35)
        elif count>60:reduce_detail(o,.65)
        kept.append(o)
    bpy.data.batch_remove(ids=tuple(removed))
    obj=merge('ModernHouse%02d'%i,kept,mat)
    report.append(dict(name=obj.name,sourceAnchor=anchor,originalTriangles=before,adaptedTriangles=triangles(obj),removedGardenParts=len(removed)))
before=set(bpy.context.scene.objects)
bpy.ops.import_scene.fbx(filepath=os.path.join(assets,'TownHouse/Extracted/Residental_building/residental_building.fbx'))
obs=[o for o in bpy.context.scene.objects if o not in before and o.type=='MESH']
mat=material('CT11_TownHouse',os.path.join(assets,'TownHouse/Extracted/Residental_building/textures/building_texture.png'),os.path.join(assets,'TownHouse/Extracted/Residental_building/textures/building_texture_N.png'))
original=sum(triangles(o) for o in obs)
for o in obs:
    world=o.matrix_world.copy();o.data=o.data.copy();o.data.transform(world)
    if world.determinant()<0:o.data.flip_normals();mirrored+=1
    o.parent=None;o.matrix_world=Matrix.Identity(4);planar(o);reduce_detail(o,.45)
obj=merge('TownHouse',obs,mat)
report.append(dict(name=obj.name,originalTriangles=original,adaptedTriangles=triangles(obj)))
for o in list(bpy.context.scene.objects):
    if o.type!='MESH':bpy.data.objects.remove(o,do_unlink=True)
with open(os.path.join(out,'PreparedAssets.json'),'w',encoding='utf-8') as f:json.dump(dict(templates=report,materials=palettes,mirroredSourceTransformsCorrected=mirrored),f,indent=2)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(out,'ModernHouses.Prepared.blend'))
print('MODERN_HOUSES_PREPARED '+json.dumps(report),flush=True)
