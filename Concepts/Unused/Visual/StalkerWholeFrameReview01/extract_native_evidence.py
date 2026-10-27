"""Mechanical extraction only: no resizing, filtering, keying, painting or registration.

Run with bundled Python from any directory. All output stays beside this script.
Sprite YAML uses a bottom-left texture rect; Pillow uses top-left pixel edges.
"""
from pathlib import Path
import hashlib
import json
import math
import re

import numpy as np
from PIL import Image, ImageDraw

OUT = Path(__file__).resolve().parent
PROJECT = OUT.parents[3]
ART = PROJECT / "Assets/EmergencyShift/Art/Enemies/Stalker"


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def parse_sprite(path):
    raw = path.read_text(encoding="utf-8")
    rect = re.search(r"m_Rect:\s+serializedVersion: \d+\s+x: ([\d.\-]+)\s+y: ([\d.\-]+)\s+width: ([\d.\-]+)\s+height: ([\d.\-]+)", raw)
    x, y, w, h = [int(float(v)) for v in rect.groups()]
    pivot = re.search(r"m_Pivot: \{x: ([\d.eE+\-]+), y: ([\d.eE+\-]+)\}", raw)
    px, py = [float(v) for v in pivot.groups()]
    ppu = float(re.search(r"m_PixelsToUnits: ([\d.eE+\-]+)", raw).group(1))
    texture_guid = re.search(r"texture: \{fileID: \d+, guid: ([\da-f]+)", raw).group(1)
    texture = next(path.with_suffix("") for path in (ART / "PaleFlesh").glob("*.png.meta") if texture_guid in path.read_text())
    sheet = Image.open(texture).convert("RGBA")
    box = (x, sheet.height - y - h, x + w, sheet.height - y)
    image = sheet.crop(box)
    assert 0 <= box[0] < box[2] <= sheet.width and 0 <= box[1] < box[3] <= sheet.height
    alpha = np.asarray(image)[:, :, 3]
    values, counts = np.unique(alpha, return_counts=True)
    p_top = (px * w, (1 - py) * h)
    integer_pivot = [round(v) for v in p_top]
    return image, {
        "name": path.stem,
        "sprite_asset": str(path.relative_to(PROJECT)),
        "sprite_sha256": sha(path),
        "texture": str(texture.relative_to(PROJECT)),
        "texture_sha256": sha(texture),
        "texture_size": list(sheet.size),
        "unity_rect_bottom_left_xywh": [x, y, w, h],
        "pillow_crop_left_top_right_bottom": list(box),
        "pivot_normalized_bottom_left": [px, py],
        "pivot_pixels_top_left": list(p_top),
        "pivot_pixels_top_left_integer_for_sheet": integer_pivot,
        "integer_placement_error_pixels": [integer_pivot[i] - p_top[i] for i in range(2)],
        "pixels_per_unit": ppu,
        "rect_world_units_wh": [w / ppu, h / ppu],
        "alpha_min_max": [int(alpha.min()), int(alpha.max())],
        "alpha_zero_count": int((alpha == 0).sum()),
        "alpha_partial_count": int(((alpha > 0) & (alpha < 255)).sum()),
        "alpha_full_count": int((alpha == 255).sum()),
        "alpha_histogram": dict(zip(map(str, values.tolist()), counts.tolist())),
        "alpha_buckets": {f"{lo}-{hi}":int(((alpha >= lo) & (alpha <= hi)).sum()) for lo,hi in ((0,0),(1,31),(32,127),(128,223),(224,249),(250,255))},
        "alpha_bounds_top_left": {str(threshold): list(image.getchannel("A").point(lambda x: 255 if x > threshold else 0).getbbox() or []) for threshold in (0,32,128,249)},
        "edge_pixel_counts_alpha_gt32": {"left":int((alpha[:,0] > 32).sum()),"right":int((alpha[:,-1] > 32).sum()),"top":int((alpha[0,:] > 32).sum()),"bottom":int((alpha[-1,:] > 32).sum())},
        "rgba_crop_sha256_unencoded": hashlib.sha256(image.tobytes()).hexdigest(),
    }


def sheet(items, name, columns, background):
    # One common origin at integer raster coordinates. Native scale remains 1:1.
    left = max(m["pivot_pixels_top_left_integer_for_sheet"][0] for _, m in items)
    top = max(m["pivot_pixels_top_left_integer_for_sheet"][1] for _, m in items)
    right = max(im.width - m["pivot_pixels_top_left_integer_for_sheet"][0] for im, m in items)
    bottom = max(im.height - m["pivot_pixels_top_left_integer_for_sheet"][1] for im, m in items)
    margin, label_h = 24, 46
    cw, ch = left + right + 2 * margin, top + bottom + 2 * margin + label_h
    rows = math.ceil(len(items) / columns)
    canvas = Image.new("RGBA", (cw * columns, ch * rows), background)
    draw = ImageDraw.Draw(canvas)
    placements = []
    for index, (im, metadata) in enumerate(items):
        col, row = index % columns, index // columns
        ox, oy = col * cw, row * ch
        origin = (ox + margin + left, oy + label_h + margin + top)
        pivot = metadata["pivot_pixels_top_left_integer_for_sheet"]
        xy = (origin[0] - pivot[0], origin[1] - pivot[1])
        canvas.alpha_composite(im, xy)
        # Labels and the pivot mark stay outside or around the sprite; no art processing.
        draw.text((ox + 12, oy + 10), metadata["name"], fill=(255,255,255,255))
        draw.text((ox + 12, oy + 25), f'{im.width}x{im.height} px; PPU {metadata["pixels_per_unit"]:g}; 1 source px = 1 output px', fill=(255,255,255,255))
        draw.rectangle((ox, oy, ox + cw - 1, oy + ch - 1), outline=(16,80,16,255))
        placements.append({"name":metadata["name"], "source_top_left_in_sheet":list(xy), "pivot_in_sheet":list(origin)})
    canvas.convert("RGB").save(OUT / name)
    return {"file":name, "cell_size": [cw,ch], "origin_in_cell": [margin+left,label_h+margin+top], "placements":placements}


def main():
    originals = {}
    for family in ("walk", "recoil", "attack"):
        path = ART / "PaleFlesh" / f"{family}-sheet.png"
        im = Image.open(path)
        rgba = np.asarray(im.convert("RGBA"))
        originals[family] = {
            "path":str(path.relative_to(PROJECT)), "sha256":sha(path),
            "mode":im.mode, "size":list(im.size),
            "alpha_min_max":[int(rgba[:,:,3].min()), int(rgba[:,:,3].max())],
            "alpha_zero_count":int((rgba[:,:,3]==0).sum()),
            "alpha_partial_count":int(((rgba[:,:,3]>0)&(rgba[:,:,3]<255)).sum()),
            "alpha_full_count":int((rgba[:,:,3]==255).sum()),
            "corner_rgba":rgba[0,0].tolist()
        }
    items = []
    for pattern in ("Recoil*-corrected.asset", "Walk*-corrected.asset", "Attack*.asset"):
        for path in sorted((ART / "Frames").glob(pattern)):
            item = parse_sprite(path)
            item[0].save(OUT / f'{path.stem}-exact-rgba.png')
            items.append(item)
    names = ["Recoil00-corrected", "Recoil01-corrected", "Recoil02-corrected", "Recoil03-corrected", "Walk00-corrected", "Walk04-corrected"]
    by_name = {m["name"]:(im,m) for im,m in items}
    sheets = [sheet([by_name[n] for n in names], "four-recoil-two-walk-native-green.png", 3, (0,196,0,255))]
    sheets += [sheet([item for item in items if item[1]["name"].startswith(prefix)], filename, columns, (0,196,0,255)) for prefix,filename,columns in (
        ("Recoil", "all-recoil-native-green.png", 2),
        ("Walk", "all-walk-native-green.png", 4),
        ("Attack", "all-attack-native-green.png", 2),
    )]
    for prefix in ("Recoil", "Walk", "Attack"):
        subset = [item for item in items if item[1]["name"].startswith(prefix)]
        for label,color in (("dark",(20,22,24,255)),("light",(230,230,230,255))):
            sheets.append(sheet(subset, f"all-{prefix.lower()}-native-{label}.png", 2 if prefix!="Walk" else 4, color))
    manifest = {
        "scope":"Review evidence only; no production files or Unity state modified.",
        "method":"Exact source texture crops by serialized Sprite rect. Source RGBA unchanged. Integer translation only; no resampling, filters, alpha keying or auto-fit. Native pixels, not equal world units: PPU differs across source families.",
        "sources": originals, "frames":[m for _,m in items], "sheets":sheets,
    }
    (OUT / "source-manifest.json").write_text(json.dumps(manifest,indent=2),encoding="utf-8")
    print(json.dumps({"sources": originals, "frames":[{k:m[k] for k in ("name","unity_rect_bottom_left_xywh","pivot_pixels_top_left","pixels_per_unit","alpha_min_max","alpha_zero_count","alpha_partial_count")} for _,m in items], "sheets":sheets},indent=2))


if __name__ == "__main__":
    main()
