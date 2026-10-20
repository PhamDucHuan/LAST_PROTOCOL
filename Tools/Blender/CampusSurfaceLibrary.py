"""Original tileable campus albedo/normal textures; no external bitmap assets.

Executed inside Blender, using its bundled numpy. All surfaces use a 2m tile;
normal maps and explicit mesh UVs also work in Unity's native URP/Lit shader.
"""
import bpy
import math
import os
import numpy as np


def create_library(output, layout):
    folder = os.path.join(output, 'Textures')
    os.makedirs(folder)
    size = 512
    yy, xx = np.mgrid[0:size, 0:size].astype(np.float32) / size
    grain = np.random.default_rng(2048).random((size, size), dtype=np.float32) - .5
    broad = sum(np.sin(xx*math.tau*k + np.cos(yy*math.tau*(k+1))) / k for k in [1, 2, 5, 13]) * .08
    palette = {}
    origin=bpy.data.objects.new('Campus surface coordinate reference',None)
    bpy.context.scene.collection.objects.link(origin)
    origin.scale=(2,2,2)

    def image(name, pixels, normal=False):
        im = bpy.data.images.new(name, width=size, height=size, alpha=True)
        im.colorspace_settings.name = 'Non-Color' if normal else 'sRGB'
        im.pixels.foreach_set(np.ascontiguousarray(pixels, dtype=np.float32).ravel())
        im.filepath_raw = os.path.join(folder, name + '.png')
        im.file_format = 'PNG'
        im.save()
        im.pack()
        return im

    def surface(key, color, height, variation, roughness=.82, tile=2):
        name = 'CT5_' + key
        rgba = np.ones((size, size, 4), dtype=np.float32)
        rgba[:,:,:3] = np.clip(np.asarray(color)[None,None,:] * (1 + variation[:,:,None]), .015, .96)
        base = image(name + '_Base', rgba)
        nx = (np.roll(height, 1, axis=1)-np.roll(height,-1,axis=1))*8
        ny = (np.roll(height, 1, axis=0)-np.roll(height,-1,axis=0))*8
        nz = np.ones_like(nx)
        inv = 1/np.sqrt(nx*nx + ny*ny + nz*nz)
        rgba[:,:,:3] = np.stack((nx*inv*.5+.5, ny*inv*.5+.5, nz*inv*.5+.5), axis=2)
        normal = image(name + '_Normal', rgba, True)
        material = bpy.data.materials.new(name)
        if material.node_tree is None: material.use_nodes = True
        node = material.node_tree.nodes.get('Principled BSDF')
        node.inputs['Roughness'].default_value = roughness
        tex = material.node_tree.nodes.new('ShaderNodeTexImage'); tex.image = base
        coords=material.node_tree.nodes.new('ShaderNodeTexCoord');coords.object=origin
        tex.projection='BOX';tex.projection_blend=.12
        material.node_tree.links.new(coords.outputs['Object'],tex.inputs['Vector'])
        material.node_tree.links.new(tex.outputs['Color'], node.inputs['Base Color'])
        tex = material.node_tree.nodes.new('ShaderNodeTexImage'); tex.image = normal
        tex.projection='BOX';tex.projection_blend=.12
        material.node_tree.links.new(coords.outputs['Object'],tex.inputs['Vector'])
        norm = material.node_tree.nodes.new('ShaderNodeNormalMap'); norm.inputs['Strength'].default_value = .5
        material.node_tree.links.new(tex.outputs['Color'], norm.inputs['Color'])
        material.node_tree.links.new(norm.outputs['Normal'], node.inputs['Normal'])
        material.diffuse_color = (*color, 1)
        material['lp_uv_metres'] = tile
        layout['materials'].append(dict(name=name,color=dict(r=1,g=1,b=1,a=1),roughness=roughness,metallic=0,emission=0,
            baseMap='Textures/'+name+'_Base.png',normalMap='Textures/'+name+'_Normal.png',normalScale=.5))
        palette[key] = material

    surface('Plaster',(.76,.75,.69),grain*.012,broad*.16+grain*.035,.88)
    surface('Concrete',(.43,.44,.42),grain*.025,broad*.25+grain*.075,.9)
    surface('BluePaint',(.30,.39,.47),grain*.01,broad*.12+grain*.025,.7)
    # Staggered .25m x .125m outdoor pavers in a 2m tile.
    row = np.floor(yy*16)
    bx = (xx*8 + (row%2)*.5)%1
    by = (yy*16)%1
    mortar = (bx<.035)|(by<.07)
    brickvar = np.sin(np.floor(xx*8 + (row%2)*.5)*71 + row*29)*.09
    surface('RedPaver',(.48,.255,.17),np.where(mortar,-.11,.02)+grain*.013,
            np.where(mortar,-.35,brickvar)+grain*.05,.9)
    # Scored concrete slabs, fine aggregate and weathered mortar.
    edge = ((xx*4)%1<.018)|((yy*4)%1<.018)
    surface('Walk',(.52,.50,.43),np.where(edge,-.09,0)+grain*.025,broad*.25+np.where(edge,-.24,0)+grain*.05,.9)
    aggregate = grain*.10 + np.sin(xx*math.tau*113)*np.cos(yy*math.tau*79)*.015
    surface('Asphalt',(.115,.13,.135),aggregate*.2,broad*.15+aggregate,.96)
    tx = (xx*12)%1
    ty = (yy*8)%1
    roofheight = np.sin(tx*math.tau)*.07 - np.where(ty<.08,.07,0)
    surface('Terracotta',(.42,.16,.085),roofheight+grain*.008,broad*.22+grain*.05+np.where(ty<.08,-.16,0),.82)
    panel = ((xx*2)%1<.014)|((yy*4)%1<.016)
    surface('SilverCladding',(.56,.59,.57),np.where(panel,-.06,0),np.where(panel,-.16,grain*.012),.48)
    grass = np.sin(xx*math.tau*117 + np.sin(yy*math.tau*31))*np.sin(yy*math.tau*89)
    surface('Grass',(.23,.31,.115),grain*.025+grass*.015,broad*.55+grain*.16+grass*.05,.96)
    bark = np.sin(xx*math.tau*17 + np.sin(yy*math.tau*3))*np.sin(xx*math.tau*37)
    surface('Bark',(.25,.19,.125),bark*.035,grain*.1+bark*.17,.94)

    # Leaf-cluster card atlas with genuine empty alpha; no painted rectangular canopy.
    rgba = np.zeros((size,size,4),dtype=np.float32)
    rng = np.random.default_rng(31)
    for index in range(31):
        cx, cy = rng.uniform(.12,.88,2)
        angle = rng.uniform(0,math.tau)
        dx,dy=xx-cx,yy-cy
        u=(dx*np.cos(angle)+dy*np.sin(angle))/.13
        v=(-dx*np.sin(angle)+dy*np.cos(angle))/.055
        mask = u*u+v*v < 1
        light = np.clip(.8+(1-u*u-v*v)*.25,0,1)
        leafcolor=np.array([.16,.29,.065])*(rng.uniform(.8,1.3))
        for channel in range(3): rgba[:,:,channel]=np.where(mask,leafcolor[channel]*light,rgba[:,:,channel])
        rgba[:,:,3]=np.where(mask,1,rgba[:,:,3])
    leaf_image=image('CT5_LeafCluster_Base',rgba)
    m=bpy.data.materials.new('CT5_LeafCluster')
    if m.node_tree is None: m.use_nodes=True
    node=m.node_tree.nodes.get('Principled BSDF');node.inputs['Roughness'].default_value=.85
    tex=m.node_tree.nodes.new('ShaderNodeTexImage');tex.image=leaf_image
    m.node_tree.links.new(tex.outputs['Color'],node.inputs['Base Color'])
    m.node_tree.links.new(tex.outputs['Alpha'],node.inputs['Alpha'])
    m.surface_render_method='DITHERED'
    layout['materials'].append(dict(name=m.name,color=dict(r=1,g=1,b=1,a=1),roughness=.85,metallic=0,emission=0,
        baseMap='Textures/CT5_LeafCluster_Base.png',alphaClip=True,doubleSided=True))
    palette['LeafCluster']=m
    return palette
