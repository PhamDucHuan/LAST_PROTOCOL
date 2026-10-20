"""Export/render only the generated CanThoCampus.blend, without resaving the source."""
import bpy
import os
import sys
import json

def finish(output, render):
    scene=bpy.context.scene
    with open(os.path.join(output,'CanThoCampus.layout.json'),encoding='utf-8') as f: layout=json.load(f)
    if layout.get('version') != 1 or '02_Administration' not in bpy.data.collections:
        raise RuntimeError('Not the generated LAST_PROTOCOL campus source.')
    groups={}
    source_count=0
    for obj in list(scene.objects):
        if obj.type!='MESH': continue
        source_count+=1
        key=(obj.users_collection[0].name,obj.data.materials[0].name if obj.data.materials else 'None')
        groups.setdefault(key,[]).append(obj)
    exports=[]
    # Build merged meshes directly. Shared primitive meshes remain editable in the source;
    # joining thousands of their object instances with the operator is unreliable in 5.2.
    for (collection_name,material_name),objects in groups.items():
        vertices,faces,uvs,smooth=[],[],[],[]
        for obj in objects:
            offset=len(vertices)
            vertices.extend(tuple(obj.matrix_world @ v.co) for v in obj.data.vertices)
            faces.extend(tuple(offset+i for i in poly.vertices) for poly in obj.data.polygons)
            smooth.extend(poly.use_smooth for poly in obj.data.polygons)
            layer=obj.data.uv_layers.active
            metres=obj.data.materials[0].get('lp_uv_metres',0) if obj.data.materials else 0
            if metres:
                normal_matrix=obj.matrix_world.to_3x3().inverted_safe().transposed()
                for poly in obj.data.polygons:
                    normal=normal_matrix @ poly.normal
                    axis=max(range(3),key=lambda i: abs(normal[i]))
                    for loop_index in poly.loop_indices:
                        p=obj.matrix_world @ obj.data.vertices[obj.data.loops[loop_index].vertex_index].co
                        uvs.append((-p.y/metres,p.z/metres) if axis==0 else
                                   (-p.x/metres,p.z/metres) if axis==1 else (-p.x/metres,-p.y/metres))
            else:
                uvs.extend(tuple(layer.data[i].uv) if layer else (0,0) for i in range(len(obj.data.loops)))
        data=bpy.data.meshes.new(collection_name+'__'+material_name)
        data.from_pydata(vertices,[],faces); data.materials.append(bpy.data.materials[material_name])
        for poly,flag in zip(data.polygons,smooth): poly.use_smooth=flag
        uv=data.uv_layers.new(name='UVMap')
        for i,value in enumerate(uvs): uv.data[i].uv=value
        data.update()
        obj=bpy.data.objects.new(data.name,data)
        bpy.data.collections[collection_name].objects.link(obj)
        exports.append(obj)
    # One dependency-graph update instead of quadratic per-instance deletions.
    bpy.data.batch_remove(ids=tuple(obj for objects in groups.values() for obj in objects))
    bpy.ops.object.select_all(action='DESELECT')
    for obj in exports: obj.select_set(True)
    bpy.ops.export_scene.fbx(filepath=os.path.join(output,'CanThoCampus.fbx'),use_selection=True,object_types={'MESH'},
        global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',
        use_space_transform=True,bake_space_transform=False,bake_anim=False,add_leaf_bones=False)
    stats=dict(blender=bpy.app.version_string,sourceObjects=source_count,renderers=len(exports),
        triangles=sum(len(poly.vertices)-2 for obj in exports for poly in obj.data.polygons),
        collisionBoxes=len(layout['collisions']),landmarkEntries=len(layout['landmarks']),result='PASS')
    with open(os.path.join(output,'BuildStats.json'),'w') as f: json.dump(stats,f,indent=2)
    print('LAST_PROTOCOL_CANTHO_EXPORT_PASS '+json.dumps(stats),flush=True)
    if render:
        for cam,name in [('Campus Overview','CanTho-Overview'),('Gate B Arrival','CanTho-GateB'),('Turtle Hall Walk','CanTho-TurtleHall')]:
            scene.camera=bpy.data.objects[cam]; scene.render.filepath=os.path.join(output,name+'.png')
            bpy.ops.render.render(write_still=True)

if __name__=='__main__':
    args=sys.argv[sys.argv.index('--')+1:]
    output=os.path.abspath(args[0])
    bpy.ops.wm.open_mainfile(filepath=os.path.join(output,'CanThoCampus.blend'))
    finish(output,'--render' in args)
