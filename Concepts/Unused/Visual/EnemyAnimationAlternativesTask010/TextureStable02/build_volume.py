"""Blender offline fixed-material test. Run after skeleton and volume review.
Never writes imported game assets. One packed whole image, one closed surface,
actual armature weights, fixed UV and 24 skeletal poses; no frame repaint.
"""
from pathlib import Path
import json,sys,math
import bpy
import numpy as np
from mathutils import Vector,Matrix

ROOT=Path(__file__).resolve().parent
VALID=ROOT.parents[4]/'Docs/Validation/EnemyAnimationAlternativesTask010/TextureStable02'
data=json.loads((ROOT/'skeleton.json').read_text(encoding='utf-8'))
arrays=np.load(ROOT/'rest-surface.npz')
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)

mesh=bpy.data.meshes.new('ContinuousClosedRestVolume')
mesh.from_pydata(arrays['vertices'].tolist(),[],arrays['faces'].tolist());mesh.update()
obj=bpy.data.objects.new('Stalker_FixedMaterialVolume_TEST_NOT_PRODUCTION',mesh);bpy.context.collection.objects.link(obj)
uv=mesh.uv_layers.new(name='OneUnchangedRestProjection')
loopverts=np.empty(len(mesh.loops),dtype=np.int32);mesh.loops.foreach_get('vertex_index',loopverts)
uv.data.foreach_set('uv',arrays['uv'][loopverts].ravel())
for p in mesh.polygons:p.use_smooth=True

material=bpy.data.materials.new('ONE_fixed_whole_master_painted_light');material.use_nodes=True
nodes=material.node_tree.nodes;nodes.clear();links=material.node_tree.links
out=nodes.new('ShaderNodeOutputMaterial');tex=nodes.new('ShaderNodeTexImage')
tex.image=bpy.data.images.load(str(ROOT/'master.png'));tex.image.pack();tex.interpolation='Linear';tex.extension='CLIP'
em=nodes.new('ShaderNodeEmission');em.inputs['Strength'].default_value=1
links.new(tex.outputs['Color'],em.inputs['Color'])
transparent=nodes.new('ShaderNodeBsdfTransparent');mix=nodes.new('ShaderNodeMixShader')
geom=nodes.new('ShaderNodeNewGeometry');inv=nodes.new('ShaderNodeMath');inv.operation='SUBTRACT';inv.inputs[0].default_value=1
links.new(geom.outputs['Backfacing'],inv.inputs[1])
alpha=nodes.new('ShaderNodeMath');alpha.operation='MULTIPLY'
links.new(tex.outputs['Alpha'],alpha.inputs[0]);links.new(inv.outputs[0],alpha.inputs[1])
links.new(alpha.outputs[0],mix.inputs[0]);links.new(transparent.outputs[0],mix.inputs[1]);links.new(em.outputs[0],mix.inputs[2]);links.new(mix.outputs[0],out.inputs['Surface'])
obj.data.materials.append(material)

armdata=bpy.data.armatures.new('ProposedFixedLengthSkeleton');arm=bpy.data.objects.new('OFFLINE_RIG',armdata);bpy.context.collection.objects.link(arm)
bpy.context.view_layer.objects.active=arm;arm.select_set(True);obj.select_set(False)
bpy.ops.object.mode_set(mode='EDIT')
root=armdata.edit_bones.new('Torso');root.head=(0,0,.13);root.tail=(0,.15,.13)
for name,r in data['rest'].items():
    up=armdata.edit_bones.new(name+'_upper');up.head=r['hip'];up.tail=r['knee'];up.parent=root
    lo=armdata.edit_bones.new(name+'_lower');lo.head=r['knee'];lo.tail=r['tip'];lo.parent=up;lo.use_connect=True
bpy.ops.object.mode_set(mode='OBJECT')
names=['Torso']+[f'{name}_{part}' for name in data['rest'] for part in ('upper','lower')]
for col,name in enumerate(names):
    g=obj.vertex_groups.new(name=name)
    for i,w in enumerate(arrays['weights'][:,col]):
        if w>0:g.add([i],float(w),'REPLACE')
modifier=obj.modifiers.new('ActualWeightedArmature','ARMATURE');modifier.object=arm;modifier.use_deform_preserve_volume=False
obj.parent=arm
for pb in arm.pose.bones:pb.rotation_mode='QUATERNION'

def set_pose(frame):
    for p in frame['points']:
        r=data['rest'][p['id']]
        for suffix,a,b in (('upper','hip','knee'),('lower','knee','tip')):
            name=p['id']+'_'+suffix;pb=arm.pose.bones[name]
            delta0=Vector(r[b])-Vector(r[a]);delta1=Vector(p[b])-Vector(p[a])
            q=delta0.rotation_difference(delta1)
            matrix=q.to_matrix().to_4x4()@arm.data.bones[name].matrix_local
            matrix.translation=Vector(p[a]);pb.matrix=matrix
            bpy.context.view_layer.update()
            pb.keyframe_insert(data_path='location',frame=frame['frame']+1)
            pb.keyframe_insert(data_path='rotation_quaternion',frame=frame['frame']+1)
            pb.keyframe_insert(data_path='scale',frame=frame['frame']+1)
    bpy.context.view_layer.update()

scene=bpy.context.scene
scene.render.engine='CYCLES';scene.cycles.samples=16;scene.cycles.use_denoising=False;scene.cycles.seed=0;scene.cycles.use_animated_seed=False
scene.cycles.max_bounces=1;scene.cycles.transparent_max_bounces=8
scene.render.resolution_x=640;scene.render.resolution_y=640;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA';scene.render.film_transparent=True
scene.render.dither_intensity=0;scene.render.filter_size=1
scene.view_settings.view_transform='Standard';scene.view_settings.look='None';scene.view_settings.exposure=0;scene.view_settings.gamma=1
scene.render.fps=32;scene.frame_start=1;scene.frame_end=24
bpy.ops.object.camera_add(location=(0,0,6));camera=bpy.context.object;camera.data.type='ORTHO';camera.data.ortho_scale=640/224;camera.rotation_euler=(0,0,0);scene.camera=camera
scene.world.color=(0,0,0)

# Clay volume pass FIRST. No texture is changed to generate this guide.
set_pose(data['frames'][0]);scene.frame_set(1)
if '--clay' in sys.argv:
    clay=bpy.data.materials.new('VolumeCheckOnly');clay.diffuse_color=(.5,.4,.43,1);clay.use_nodes=True
    clay.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(.22,.16,.18,1)
    clay.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.8
    obj.data.materials[0]=clay
    bpy.ops.object.light_add(type='AREA',location=(-1,1,4));bpy.context.object.data.energy=60;bpy.context.object.data.shape='DISK';bpy.context.object.data.size=3
    scene.render.filepath=str(VALID/'volume-top.png');bpy.ops.render.render(write_still=True)
    camera.location=(3,-4,3);camera.rotation_euler=(Vector((0,0,.1))-camera.location).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath=str(VALID/'volume-three-quarter.png');bpy.ops.render.render(write_still=True)
else:
    outdir=ROOT/'Frames';outdir.mkdir(exist_ok=True)
    stats=[]
    for f in data['frames']:
        scene.frame_set(f['frame']+1);set_pose(f)
        evaluated=obj.evaluated_get(bpy.context.evaluated_depsgraph_get());emesh=evaluated.to_mesh()
        verts=np.array([v.co[:] for v in emesh.vertices]);restv=arrays['vertices'];n=len(restv)//2
        top=arrays['faces'][np.all(arrays['faces']<n,axis=1)]
        tris=np.concatenate([top[:,[0,1,2]],top[:,[0,2,3]]])
        a=verts[tris[:,1],:2]-verts[tris[:,0],:2];b=verts[tris[:,2],:2]-verts[tris[:,0],:2]
        area=a[:,0]*b[:,1]-a[:,1]*b[:,0]
        flips=int((area<=0).sum());minarea=float(area.min())
        evaluated.to_mesh_clear()
        stats.append({'frame':f['frame'],'topTrianglesNonpositiveProjectedArea':flips,'minTopTriangleArea':minarea,'minVertexHeight':float(verts[:,2].min())})
        scene.render.filepath=str(outdir/f'walk-{f["frame"]:02}.png');bpy.ops.render.render(write_still=True)
    # Save the useful actual mesh/weights/packed texture and all24 keyed poses.
    scene.frame_set(1)
    bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'fixed-material-volume.blend'))
    (VALID/'render-geometry-check.json').write_text(json.dumps({'blender':bpy.app.version_string,'frames':stats,
        'note':'Area check is a diagnostic; actual RGBA all-frame review remains required. No Unity/runtime test.'},indent=2)+'\n',encoding='utf-8')
print('TASK010 fixed material volume complete')
