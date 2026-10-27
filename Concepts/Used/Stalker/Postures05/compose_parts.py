"""Offline straight-alpha whole-frame composition from the reviewed native parts."""
from pathlib import Path
import argparse
import hashlib
import json
import numpy as np
from PIL import Image, ImageDraw
from sprite_painter import SpritePainter

folder = Path(__file__).resolve().parent
project = folder.parents[3]


def encode(rgba, green=False):
    alpha = rgba[..., 3:4]
    rgb = rgba[..., :3]
    if green:
        rgb = rgb + np.array([0, 1, 0], dtype=np.float32) * (1-alpha)
    else:
        rgb = np.divide(rgb, alpha, out=np.zeros_like(rgb), where=alpha > 1e-8)
    rgb = np.where(rgb <= .0031308, rgb*12.92, 1.055*np.maximum(rgb, 0)**(1/2.4)-.055)
    if not green:
        rgb = np.concatenate([rgb, alpha], axis=-1)
    return Image.fromarray((np.clip(rgb, 0, 1)*255).round().astype('uint8'))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--attack', action='store_true')
    parser.add_argument('--preview', action='store_true')
    args = parser.parse_args()
    source = folder / ('attack-poses.json' if args.attack else 'poses.json')
    source_bytes = source.read_bytes()
    poses = json.loads(source_bytes)
    pose_digest = hashlib.sha256(source_bytes).hexdigest()
    out = folder
    if args.preview:
        out = folder / 'PreviewParts'
        poses = [pose for pose in poses if pose['phase'] in [0, 1, 12, 24, 25, 36]]
    (out / 'Raw').mkdir(parents=True, exist_ok=True)
    (out / 'Masks').mkdir(exist_ok=True)
    painter = SpritePainter(project)
    mouth = None
    if args.attack:
        from attack_mouth import AttackMouth, atomic_png
        mouth = AttackMouth(project, painter._torso)
    if args.preview:
        sheet = Image.new('RGB', (1600, 400*((len(poses)+3)//4)), '#00ff00')
    for index, pose in enumerate(poses):
        name = pose.get('name', f'body-{pose.get("row", 0)}-{pose["phase"]:02d}')
        whole, limbs, torso = painter.paint(pose)
        if mouth is not None:
            whole, torso, mouth_layer = mouth.apply(pose, whole, limbs, torso)
            atomic_png(encode(torso), out / 'Masks' / f'torso-{name}.png')
            atomic_png(encode(mouth_layer), out / 'Masks' / f'mouth-{name}.png')
        encode(whole).save(out / 'Raw' / f'{name}.png', compress_level=1)
        for leg, limb in enumerate(limbs):
            mask_name = f'leg-{name}-{leg}.png' if args.attack else f'leg-{pose["row"]}-{pose["phase"]:02d}-{leg}.png'
            encode(limb).save(out / 'Masks' / mask_name, compress_level=1)
        if index == 0:
            encode(torso).save(out / 'Masks/torso.png')
        if args.preview:
            green = encode(whole, green=True).resize((360, 360), Image.Resampling.LANCZOS)
            x, y = index % 4 * 400, index // 4 * 400
            sheet.paste(green, (x+20, y+30))
            ImageDraw.Draw(sheet).text((x+10, y+10), name, fill='black')
    if args.preview:
        sheet.save(out / 'paint-green.png')
    else:
        kind = 'attack' if args.attack else 'body'
        (folder / f'{kind}-composition.json').write_text(json.dumps({
            'poses': len(poses), 'alpha': 'straight', 'workingColor': 'linear premultiplied',
            'posesSha256': pose_digest,
            'painterSha256': hashlib.sha256((folder / 'sprite_painter.py').read_bytes()).hexdigest(),
            'sourceAtlasSha256': hashlib.sha256((project / 'Assets/EmergencyShift/Art/Enemies/Stalker/Locomotion02/rig-atlas.png').read_bytes()).hexdigest(),
            'sourcePoseSha256': hashlib.sha256(source.read_bytes()).hexdigest(),
            'mouthOverlayApplied': mouth is not None,
            'contourCapsuleClipping': False, 'runtimeIk': False,
            'method': 'Native upper/lower source paint, preserved transverse volume, source joint/root caps, corrected inner mounts'
        }, indent=2))
        if mouth is not None:
            (folder / 'attack-mouth-recipe.json').write_text(json.dumps(mouth.recipe, indent=2))
    print(f'{len(poses)} complete straight-alpha sprites and independent per-leg masks')


if __name__ == '__main__':
    raise SystemExit('Historical rejected parts-over experiment. Use compose_frames.py after the whole-material review.')
