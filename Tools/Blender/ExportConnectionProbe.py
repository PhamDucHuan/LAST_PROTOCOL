"""A disposable static-mesh probe; never opens or edits an existing .blend."""
import bpy
import json
import os
import sys

output = os.path.abspath(sys.argv[sys.argv.index("--") + 1])
os.makedirs(output, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.scene.unit_settings.system = 'METRIC'
bpy.context.scene.unit_settings.scale_length = 1.0

material = bpy.data.materials.new("BridgeProbeMaterial")
material.diffuse_color = (0.1, 0.6, 0.8, 1.0)

def box(name, location, dimensions):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(material)
    assert len(obj.data.uv_layers) > 0, name + " has no UV"
    return obj

box("Ground", (0, 0, -0.1), (8, 6, 0.2))
box("MeterCube", (-2, 0, 0.5), (1, 1, 1))
box("TallBox", (2, 0, 1.5), (1, 2, 3))
box("ForwardMarker", (0, -2, 0.2), (0.4, 0.4, 0.4))

blend_path = os.path.join(output, "ConnectionProbe.blend")
fbx_path = os.path.join(output, "ConnectionProbe.fbx")
bpy.ops.wm.save_as_mainfile(filepath=blend_path)
bpy.ops.export_scene.fbx(
    filepath=fbx_path,
    object_types={'MESH'},
    global_scale=1.0,
    apply_unit_scale=True,
    apply_scale_options='FBX_SCALE_UNITS',
    axis_forward='-Z',
    axis_up='Y',
    use_space_transform=True,
    bake_space_transform=False,
    bake_anim=False,
    add_leaf_bones=False,
)
assert os.path.getsize(blend_path) > 0
assert os.path.getsize(fbx_path) > 0
report = {
    "blenderVersion": bpy.app.version_string,
    "blend": blend_path,
    "fbx": fbx_path,
    "objects": [{"name": o.name, "dimensions": list(o.dimensions),
                 "location": list(o.location), "uvLayers": len(o.data.uv_layers)}
                for o in bpy.context.scene.objects if o.type == 'MESH'],
    "result": "PASS"
}
with open(os.path.join(output, "blender-result.json"), "w", encoding="utf-8") as handle:
    json.dump(report, handle, indent=2)
print("LAST_PROTOCOL_BLENDER_EXPORT_PASS " + bpy.app.version_string)
