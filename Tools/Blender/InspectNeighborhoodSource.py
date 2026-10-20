"""Read-only names and transforms needed to migrate the generated environment."""
import bpy, json, sys
bpy.ops.wm.open_mainfile(filepath=sys.argv[sys.argv.index('--')+1])
for name in ['00_Ground','29_Boundary','26_StreetFront']:
    c=bpy.data.collections[name]
    print('SOURCE_COLLECTION',name,[(o.name,tuple(o.location),tuple(o.scale)) for o in list(c.objects)[:18]],flush=True)
