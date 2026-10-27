"""Annotation/measurement only. Candidate landmark pixels are explicit manual observations.

No geometric transformation of source art is performed. These are top-left crop
coordinates and must be reviewed on the annotated native image, not treated as
an independent skeleton or 3D measurement.
"""
from pathlib import Path
import json
import math
from PIL import Image, ImageDraw

OUT = Path(__file__).resolve().parent
POINTS = {
    "Walk00-corrected": {"core_apex": [187, 147], "core_left_corner": [130, 241], "core_right_corner": [242, 241], "eye_or_lid_center": [187,200]},
    "Recoil00-corrected": {"core_apex": [299,200], "core_left_corner": [209,346], "core_right_corner": [389,346], "eye_or_lid_center": [299,296]},
    "Recoil01-corrected": {"core_apex": [249,127], "core_left_corner": [156,265], "core_right_corner": [338,265], "eye_or_lid_center": [249,231]},
    "Recoil02-corrected": {"core_apex": [200,81], "core_left_corner": [106,235], "core_right_corner": [291,235], "eye_or_lid_center": [200,195]},
    "Recoil03-corrected": {"core_apex": [174,62], "core_left_corner": [76,218], "core_right_corner": [268,218], "eye_or_lid_center": [174,166]},
}


def main():
    manifest = json.loads((OUT / "source-manifest.json").read_text())
    meta = {m["name"]:m for m in manifest["frames"]}
    results = {}
    for name,points in POINTS.items():
        im = Image.open(OUT / (name + "-exact-rgba.png")).convert("RGBA")
        canvas = Image.new("RGBA", im.size, (0,196,0,255))
        canvas.alpha_composite(im)
        draw = ImageDraw.Draw(canvas)
        colors = [(255,255,0,255),(0,255,255,255),(0,255,255,255),(255,30,255,255)]
        for index,((label,point),color) in enumerate(zip(points.items(),colors)):
            x,y = point
            draw.ellipse((x-3,y-3,x+3,y+3),fill=color)
            draw.text((x+5,y-10),str(index+1),fill=color)
        pivot = meta[name]["pivot_pixels_top_left"]
        x,y = pivot
        draw.line((x-8,y,x+8,y),fill=(255,0,0,255))
        draw.line((x,y-8,x,y+8),fill=(255,0,0,255))
        canvas.convert("RGB").save(OUT / (name + "-core-landmarks-native.png"))
        left,right,apex = points["core_left_corner"],points["core_right_corner"],points["core_apex"]
        width = math.dist(left,right)
        baseline = (left[1]+right[1])/2
        height = baseline-apex[1]
        ppu = meta[name]["pixels_per_unit"]
        center = points["eye_or_lid_center"]
        results[name] = {"manual_top_left_crop_pixels":points, "ppu":ppu,
            "core_width_pixels":width, "core_height_pixels":height,
            "core_height_width_ratio":height/width,
            "core_width_units_at_serialized_ppu":width/ppu,
            "core_height_units_at_serialized_ppu":height/ppu,
            "eye_minus_pivot_units_xy_up":[(center[0]-pivot[0])/ppu,(pivot[1]-center[1])/ppu],
            "proposed_ppu_to_match_walk00_core_width":width/(112/224)}
    closed = Image.open(OUT / "Recoil03-corrected-exact-rgba.png").convert("RGBA")
    closed.save(OUT / "closed-control-native.png")
    green = Image.new("RGBA",closed.size,(0,196,0,255))
    green.alpha_composite(closed)
    green.convert("RGB").save(OUT / "closed-control-native-green.png")
    assert Image.open(OUT / "closed-control-native.png").tobytes() == closed.tobytes()
    (OUT / "closed-control-import-proposal.json").write_text(json.dumps({
        "status":"Evidence control only, not imported or artist-approved; unchanged complete native Recoil03 crop.",
        "source_sprite":meta["Recoil03-corrected"]["sprite_asset"],
        "source_texture":meta["Recoil03-corrected"]["texture"],
        "source_rgba_sha256_unencoded":meta["Recoil03-corrected"]["rgba_crop_sha256_unencoded"],
        "output":"closed-control-native.png",
        "texture_size":[347,325],
        "proposed_pixels_per_unit":384,
        "proposed_pivot_normalized_bottom_left":[174/347,1-166/325],
        "eye_anchor_pixels_top_left":[174,166],
        "reference":"Walk00-corrected PPU224, eye center(187,200), inner triangular core width112px = .5 world unit",
        "closed_core_world_units":{"width":192/384,"height":156/384},
        "walk_core_world_units":{"width":112/224,"height":94/224},
        "operation":"No resize, no filtering, no alpha change, no cutting into joints, no warp. Sprite PPU/pivot proposal changes presentation units only. Keep original pixels.",
        "limitation":"Homologous skin landmarks are manual +/-3px. Fully closed eye anchor uses historical eyeFrameOffsets, not a visible optical center. This registration removes scale/anchor mismatch, not every source-art difference."
    },indent=2),encoding="utf-8")
    (OUT / "manual-core-measurements.json").write_text(json.dumps({
        "warning":"Manual 2D visible-skin landmarks, +/-3 pixels; not skeleton proof. Points: 1 apex of triangular inner core below the round front cap, 2/3 basal inner core corners, 4 eye/lid center. Fully closed Recoil03 has no visible optical center: marker 4 uses the historical eyeFrameOffsets registration (23 px right, 14 px down from pivot), not an invented lid center. Red cross is serialized pivot. Native art unchanged except diagnostic annotations in separate output.",
        "measurements":results},indent=2),encoding="utf-8")
    print(json.dumps(results,indent=2))


if __name__ == "__main__":
    main()
