"""Actual offline Blender volume reference. No production texture/UV or runtime rig."""
from pathlib import Path
import json
import sys
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parent
data=json.loads((ROOT/'anatomy-reference.json').read_text(encoding='utf-8'))
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
def mat(name,color):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
    bsdf=m.node_tree.nodes.get('Principled BSDF');bsdf.inputs['Base Color'].default_value=(*color,1);bsdf.inputs['Roughness'].default_value=.75
    return m
skin=mat('GuideVolume_NotFinalSkin',(.49,.40,.44));bone=mat('GuideRigidEnds',(.69,.68,.69));eye=mat('EyeLandmark',(.25,.25,.26))
def sphere(name,position,scale,material):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24,ring_count=12,location=position)
    o=bpy.context.object;o.name=name;o.scale=scale;o.data.materials.append(material)
    for face in o.data.polygons:face.use_smooth=True
    return o
def p(xy,z):return ((xy[0]-288)/224,(288-xy[1])/224,z)
def segment(name,a,b,r1,r2,material):
    a,b=Vector(a),Vector(b);delta=b-a
    bpy.ops.mesh.primitive_cone_add(vertices=20,radius1=r1,radius2=r2,depth=delta.length,location=(a+b)/2)
    o=bpy.context.object;o.name=name;o.rotation_euler=delta.to_track_quat('Z','Y').to_euler();o.data.materials.append(material)
    for face in o.data.polygons:face.use_smooth=True
    return o
# Torso masses follow the original compact triangular body, not a spider abdomen.
sphere('Core',(0,-.025,.13),(.245,.245,.11),skin)
for name,xy,scale in [('Fore',(288,232),(.075,.10,.065)),('Left',(240,320),(.075,.07,.07)),('Right',(336,320),(.075,.07,.07))]:
    sphere('Torso_'+name,p(xy,.13),scale,skin)
segment('TorsoLeft',p((288,232),.13),p((240,320),.13),.07,.07,skin)
segment('TorsoRight',p((288,232),.13),p((336,320),.13),.07,.07,skin)
segment('TorsoBack',p((240,320),.13),p((336,320),.13),.07,.07,skin)
for limb in data['points']:
    hip,knee,tip=p(limb['hip'],.13),p(limb['knee'],.10),p(limb['tip'],.015)
    inner=Vector(hip);inner.x*=.65;inner.y*=.65
    segment(limb['id']+'_connected_root',inner,hip,.072,.058,skin)
    sphere(limb['id']+'_root',hip,(.065,.065,.055),skin)
    segment(limb['id']+'_upper',hip,knee,.047,.038,skin)
    sphere(limb['id']+'_knee',knee,(.052,.052,.052),skin)
    end=Vector(knee).lerp(Vector(tip),.34)
    segment(limb['id']+'_sleeve',knee,end,.035,.028,skin)
    segment(limb['id']+'_rigid_tip',end,tip,.028,.001,bone)
sphere('EyeReference',(0,0,.242),(.098,.098,.018),eye)
bpy.ops.object.camera_add(location=(0,0,6))
camera=bpy.context.object;camera.name='StrictTopCamera';camera.data.type='ORTHO';camera.data.ortho_scale=576/224
camera.rotation_euler=(0,0,0)
scene=bpy.context.scene;scene.camera=camera;scene.render.engine='CYCLES';scene.cycles.samples=24
scene.render.resolution_x=576;scene.render.resolution_y=576;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA';scene.render.film_transparent=True
scene.view_settings.view_transform='Standard'
scene.world.color=(.25,.25,.25)
bpy.ops.object.light_add(type='AREA',location=(-1,1,4));light=bpy.context.object;light.data.energy=330;light.data.shape='DISK';light.data.size=4
scene.render.filepath=str(ROOT/'neutral-volume-guide.png')
if '--walk' not in sys.argv:
    bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'volume-guide.blend'))
    bpy.ops.render.render(write_still=True)
else:
    plan=json.loads((ROOT/'walk-plan.json').read_text(encoding='utf-8'))
    def move_segment(name,a,b):
        a,b=Vector(a),Vector(b);delta=b-a
        o=bpy.data.objects[name]
        zs=[v.co.z for v in o.data.vertices]
        o.location=(a+b)/2;o.rotation_euler=delta.to_track_quat('Z','Y').to_euler()
        o.scale.z=delta.length/(max(zs)-min(zs))
    for frame in plan['frames']:
        for limb in frame['points']:
            hip,knee,tip=p(limb['hip'],.13),p(limb['knee'],.10),p(limb['tip'],.015+limb['heightGuide'])
            move_segment(limb['id']+'_upper',hip,knee)
            bpy.data.objects[limb['id']+'_knee'].location=knee
            end=Vector(knee).lerp(Vector(tip),.34)
            move_segment(limb['id']+'_sleeve',knee,end)
            move_segment(limb['id']+'_rigid_tip',end,tip)
        scene.render.filepath=str(ROOT/f'walk-volume-{frame["frame"]:02}.png')
        bpy.ops.render.render(write_still=True)
