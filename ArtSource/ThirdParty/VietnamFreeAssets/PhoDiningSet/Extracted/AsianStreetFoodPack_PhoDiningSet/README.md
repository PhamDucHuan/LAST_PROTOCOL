# Pho Dining Set - free sampler

A ready-to-assemble sidewalk dining set: plastic table and stools, a bowl of
pho, the condiment caddy that lives on every table, produce baskets, and the
bottle crate under the seat.

9 kits, 42 meshes, 18,010 triangles. A slice of the full pack "Asian
Street Food Carts & Stalls" (66 kits, 337 meshes), cut from
the same build and held to the same bar - nothing here is a lower-quality
teaser version.

## What is inside

- **Low Plastic Stools** (#08) - 4 mesh, 1,984 tris
- **Low Plastic Tables** (#10) - 3 mesh, 1,188 tris
- **Pho Bowl Set** (#19) - 3 mesh, 2,930 tris
- **Spoon & Side Bowls** (#19f) - 3 mesh, 1,908 tris
- **Produce Baskets** (#24) - 12 mesh, 6,036 tris
- **Condiment Set** (#25) - 8 mesh, 1,036 tris
- **Pepper Shaker** (#25b) - 1 mesh, 432 tris
- **Napkin Box** (#25c) - 1 mesh, 332 tris
- **Bottle Crate** (#28) - 7 mesh, 2,164 tris

## How it is built

- Metres, Z-up authored, pivots at the foot of each prop, so props drop onto a
  floor at y = 0 without nudging.
- One material for the whole sampler: `M_Pack01_Atlas`, one draw call per mesh.
  Colour atlas plus a metallic-roughness atlas, both 1024.
- Set the atlas textures to Point filtering. The palette is flat swatches, and
  bilinear filtering bleeds neighbouring colours across the swatch edges.
- The MR atlas uses the glTF channel layout: `g` is roughness, `b` is metallic,
  `r` unused. Import it as Linear, not sRGB.
- Holes are real geometry. No alpha cutout anywhere, so openings hold up at any
  camera angle and cost nothing in overdraw.
- Flat shading throughout, no smoothing groups to fight with.

## Formats

- `GLB/` - textures embedded, drop straight into Godot, Three.js or Blender.
- `FBX/` - textures referenced as `../Textures/palette_1024.png`. Keep the
  `Textures` folder next to the format folder or the material comes in white.
- `OBJ/` - `.obj` plus `.mtl`, same relative texture path.

Unity, Unreal and Godot all import these formats directly. `asset_list.csv`
lists every mesh with its triangle count.

## Licence

Commercial use is fine, redistributing the files is not. Full terms in
`LICENSE.txt`.

## The full pack

Asian Street Food Carts & Stalls - four modular vendor carts,
street and shopfront modules, and every prop that fills a sidewalk.
On itch.io, Unity Asset Store, Fab and CGTrader. alleybits.com
