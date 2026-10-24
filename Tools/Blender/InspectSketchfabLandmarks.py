"""Inspect official GLB downloads in a separate Blender process."""
import bpy, json, os, sys, math
from mathutils import Vector
root, output = map(os.path.abspath, sys.argv[sys.argv.index('--')+1:][:2])
os.makedirs(output, exist_ok=True)
report=[]
for name in ['HoiTruongRua','NhaHocA3','NhaThiDauTDTT']:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=os.path.join(root,'ArtSource/ThirdParty/CanThoLandmarks',name+'_AnLe.glb'))
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    pts=[o.matrix_world @ Vector(v) for o in meshes for v in o.bound_box]
    lo=Vector([min(p[i] for p in pts) for i in range(3)]); hi=Vector([max(p[i] for p in pts) for i in range(3)])
    center=(lo+hi)/2; radius=max(hi-lo)
    mats=[]
    for mat in bpy.data.materials:
        p=next((n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None) if mat.use_nodes else None
        if p: mat.diffuse_color=p.inputs['Base Color'].default_value
        mats.append(dict(name=mat.name,color=list(mat.diffuse_color),doubleSided=not mat.use_backface_culling,
                         images=[n.image.name for n in mat.node_tree.nodes if n.type=='TEX_IMAGE' and n.image] if mat.use_nodes else []))
    group_bounds=[]
    for group in bpy.context.scene.objects:
        children=[o for o in group.children_recursive if o.type=='MESH']
        if group.type!='EMPTY' or not children:continue
        points=[o.matrix_world @ Vector(v) for o in children for v in o.bound_box]
        lower=[min(p[i] for p in points) for i in range(3)];upper=[max(p[i] for p in points) for i in range(3)]
        group_bounds.append(dict(name=group.name,parent=group.parent.name if group.parent else None,min=lower,max=upper,meshes=len(children)))
    report.append(dict(name=name,min=list(lo),max=list(hi),size=list(hi-lo),groups=group_bounds,triangles=sum(len(p.vertices)-2 for o in meshes for p in o.data.polygons),
                       objects=[dict(name=o.name,parent=o.parent.name if o.parent else None,triangles=sum(len(p.vertices)-2 for p in o.data.polygons),slots=len(o.data.materials),
                                     min=[min((o.matrix_world @ Vector(v))[i] for v in o.bound_box) for i in range(3)],
                                     max=[max((o.matrix_world @ Vector(v))[i] for v in o.bound_box) for i in range(3)]) for o in meshes],materials=mats,
                       images=[dict(name=im.name,size=list(im.size)) for im in bpy.data.images]))
    if '--no-render' in sys.argv:continue
    scene=bpy.context.scene; scene.render.engine='BLENDER_WORKBENCH'
    scene.display.shading.light='STUDIO';scene.display.shading.color_type='MATERIAL'
    scene.display.shading.show_shadows=True;scene.display.shading.show_cavity=True
    scene.render.resolution_x=900;scene.render.resolution_y=650;scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG'
    camdata=bpy.data.cameras.new('Inspect');cam=bpy.data.objects.new('Inspect',camdata);scene.collection.objects.link(cam);scene.camera=cam
    camdata.type='ORTHO';camdata.ortho_scale=radius*1.45;camdata.clip_end=radius*20+100
    for index,direction in enumerate([(1,-1,.8),(-1,1,.8)]):
        cam.location=center+Vector(direction).normalized()*radius*2
        cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler()
        scene.render.filepath=os.path.join(output,name+'-'+str(index)+'.png');bpy.ops.render.render(write_still=True)
with open(os.path.join(output,'SourceInspection.json'),'w',encoding='utf-8') as f:json.dump(report,f,indent=2,ensure_ascii=False)
print('SKETCHFAB_SOURCE_INSPECTION_PASS',[(r['name'],r['size'],r['triangles']) for r in report],flush=True)
