"""Read-only postpack QA of shipped R10 atlas, metadata and stable sprite bank.

Run directly with Python/Pillow/numpy. Writes diagnostics only beside this file.
Does not import renderers, modify production files, or substitute raw reassembly.
"""
from pathlib import Path
import hashlib
import json
import re
import warnings

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

warnings.filterwarnings("ignore", category=DeprecationWarning)
HERE = Path(__file__).resolve().parent
PROJECT = next(p for p in HERE.parents if (p / "Assets/EmergencyShift").exists())
SOURCE = HERE.parent
EXPECTED_PARTS = [f"{side}{i}" for side in "LR" for i in range(1, 5)]
EXPECTED_PARTS += ["tail", "L-pedipalp", "R-pedipalp", "core"]


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def morph(mask, radius=1, erode=False):
    image = Image.fromarray(mask.astype("uint8") * 255)
    filter_type = ImageFilter.MinFilter if erode else ImageFilter.MaxFilter
    return np.asarray(image.filter(filter_type(radius * 2 + 1))) > 0


def bounds(mask):
    yy, xx = np.nonzero(mask)
    if not len(xx):
        return None
    return [int(xx.min()), int(yy.min()), int(xx.max()) + 1, int(yy.max()) + 1]


def premultiplied(image):
    value = np.asarray(image).astype("float32")
    value[:, :, :3] *= value[:, :, 3:4] / 255.0
    return value


def refs(text):
    return [(int(i), guid) for i, guid in re.findall(
        r"\{fileID: (-?\d+), guid: ([a-f0-9]+), type: \d+\}", text)]


def check_serialized(manifest, atlas_path):
    meta = Path(str(atlas_path) + ".meta").read_text(encoding="utf-8")
    guid = re.search(r"^guid: (\w+)", meta, re.M)[1]
    sprite_blocks = meta.split("    - serializedVersion: 2\n")[1:]
    sprites = {}
    for block in sprite_blocks:
        name = re.search(r"^      name: (.+)$", block, re.M)
        if name is None:
            continue
        rect = [int(re.search(rf"^        {field}: (-?\d+)$", block, re.M)[1])
                for field in ("x", "y", "width", "height")]
        sprites[name[1]] = (int(re.search(r"      internalID: (-?\d+)", block)[1]), rect,
                            "pivot: {x: 0.5, y: 0.5}" in block)
    expected_refs = []
    errors = []
    for pose in manifest["poses"]:
        for part in pose["parts"]:
            ident, rect, centered = sprites[part["name"]]
            x, y, w, h = part["rectTopLeft"]
            if rect != [x, manifest["size"][1] - y - h, w, h] or not centered:
                errors.append(part["name"])
            expected_refs.append((ident, guid))
    catalog_path = PROJECT / "Assets/EmergencyShift/Data/Enemies/Scorpion/RupturePosesR10.asset"
    catalog = catalog_path.read_text(encoding="utf-8")
    catalog_refs = []
    offsets = []
    for block in catalog.split("    parts:\n")[1:]:
        catalog_refs.extend(refs(block.split("    offsets:")[0]))
        offset_block = block.split("    offsets:\n")[1].split("  - source:")[0]
        offsets.extend([(float(x), float(y)) for x, y in re.findall(
            r"\{x: ([-\d.]+), y: ([-\d.]+)\}", offset_block)])
    expected_offsets = [tuple(part["offset"]) for pose in manifest["poses"] for part in pose["parts"]]
    bank_results = []
    for name in ("Scorpion", "ScorpionVisual"):
        path = PROJECT / f"Assets/EmergencyShift/Prefabs/Enemies/{name}.prefab"
        text = path.read_text(encoding="utf-8")
        match = re.search(r"^  fragments:\n((?:  - .*\n)+)", text, re.M)
        if match is None:
            bank_results.append({"prefab": name,
                                 "scope": "Visual-only prefab; no GoreBurst2D or sprite bank expected.",
                                 "has_gore_component": "EmergencyShift.GoreBurst2D" in text,
                                 "sha256": sha(path)})
            continue
        bank = refs(match[1]) if match else []
        bank_results.append({"prefab": name, "entries": len(bank),
                             "all_672_permanent_indices_match": bank[4:] == expected_refs,
                             "sha256": sha(path)})
    return {
        "atlas_sprite_count": len(sprites), "rect_or_pivot_errors": errors,
        "catalog_matches_manifest_order": catalog_refs == expected_refs,
        "catalog_offsets_match_manifest": offsets == expected_offsets,
        "banks": bank_results,
        "scope": "Current frozen catalogue order only; future bank reorder/removal still requires migration.",
    }


def main():
    manifest_path = SOURCE / "rupture-r10.json"
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    atlas_path = PROJECT / manifest["atlas"]
    atlas = Image.open(atlas_path).convert("RGBA")
    references = {state: Image.open(PROJECT / f"Assets/EmergencyShift/Art/Enemies/Scorpion/{state.title()}.png").convert("RGBA")
                  for state in ("run", "attack")}
    raw_check = json.loads((HERE / "reassembly.json").read_text(encoding="utf-8"))
    rows = []
    rectangles = []
    names = []
    renders = []
    frames = []
    for pose in manifest["poses"]:
        canvas = Image.new("RGBA", (512, 512))
        filter_band = np.zeros((512, 512), dtype=bool)
        frame = pose["frame"]
        reference = references[pose["state"]].crop(
            (frame % 6 * 512, frame // 6 * 512, frame % 6 * 512 + 512, frame // 6 * 512 + 512))
        border_max = 0
        placements = []
        for part in pose["parts"]:
            x, y, w, h = part["rectTopLeft"]
            rectangles.append((part["name"], x, y, w, h))
            names.append(part["name"])
            crop = atlas.crop((x, y, x + w, y + h))
            px = 256 + part["offset"][0] * manifest["ppu"] - w / 2
            py = 274 - part["offset"][1] * manifest["ppu"] - h / 2
            placements.append(max(abs(px - round(px)), abs(py - round(py))))
            alpha = np.asarray(crop)[:, :, 3]
            border_max = max(border_max, int(max(alpha[0].max(), alpha[-1].max(), alpha[:, 0].max(), alpha[:, -1].max())))
            canvas.alpha_composite(crop, (round(px), round(py)))
            band = morph((alpha > 0) & (alpha < 255), 2)
            band_image = Image.new("L", (512, 512))
            band_image.paste(Image.fromarray(band.astype("uint8") * 255), (round(px), round(py)))
            filter_band |= np.asarray(band_image) > 0
        a = np.asarray(canvas)[:, :, 3]
        b = np.asarray(reference)[:, :, 3]
        error = np.max(np.abs(premultiplied(canvas) - premultiplied(reference)), axis=2)
        changed = error > 4
        # One final-atlas pixel tolerance is explicit and does not excuse large gaps.
        missing = (b >= 128) & ~morph(a >= 128)
        excess = (a >= 128) & ~morph(b >= 128)
        transparent_inside = morph(b >= 250, erode=True) & (a < 128)
        bound_a, bound_b = bounds(a >= 64), bounds(b >= 64)
        rows.append({
            "state": pose["state"], "frame": frame,
            "part_order_ok": [p["part"] for p in pose["parts"]] == EXPECTED_PARTS,
            "max_fractional_placement": max(placements), "max_crop_border_alpha": border_max,
            "assembled_bounds_alpha64": bound_a, "reference_bounds_alpha64": bound_b,
            "max_bounds_delta": max(abs(x - y) for x, y in zip(bound_a, bound_b)),
            "changed_premul_pixels_over4": int(changed.sum()),
            "max_premul_error": float(error.max()), "max_alpha_error": int(np.abs(a.astype(int) - b).max()),
            "changed_pixels_outside_filter_band": int((changed & ~filter_band).sum()),
            "missing_solid_outside_1px": int(missing.sum()), "excess_solid_outside_1px": int(excess.sum()),
            "transparent_in_reference_interior": int(transparent_inside.sum()),
            "candidate_geometry_bounds": bounds(missing | excess | transparent_inside),
        })
        renders.append(canvas)
        frames.append(reference)
    rect_overlap = []
    for i, (name, x, y, w, h) in enumerate(rectangles):
        for other, xx, yy, ww, hh in rectangles[i + 1:]:
            if min(x + w, xx + ww) > max(x, xx) and min(y + h, yy + hh) > max(y, yy):
                rect_overlap.append([name, other])
    out_of_bounds = [name for name, x, y, w, h in rectangles
                     if x < 0 or y < 0 or w <= 0 or h <= 0 or x + w > atlas.width or y + h > atlas.height]
    serial = check_serialized(manifest, atlas_path)
    geometry_candidates = [r for r in rows if any(r[key] for key in (
        "missing_solid_outside_1px", "excess_solid_outside_1px", "transparent_in_reference_interior"))
        or r["max_bounds_delta"] > 1 or r["max_fractional_placement"] > 1e-6 or not r["part_order_ok"]]
    report = {
        "method": "Recompose FINAL production crops at PPU200, center pivot and source pivot(256,274). Compare production512cells; premultipliedRGBA ignores hidden RGB. Filter band=2px dilation of each crop's fractional-alpha pixels.",
        "limitations": "Numeric gates locate candidates; they do not prove artistic approval, exact rendered URP colors, audio, runtime or future snapshot compatibility. One pixel silhouette tolerance and alpha>=64 bounds distinguish downsample AA from large geometry defects.",
        "inputs": {str(p.relative_to(PROJECT)): sha(p) for p in [manifest_path, atlas_path,
            PROJECT / "Assets/EmergencyShift/Art/Enemies/Scorpion/Run.png",
            PROJECT / "Assets/EmergencyShift/Art/Enemies/Scorpion/Attack.png"]},
        "atlas_size": list(atlas.size), "atlas_hash_matches_raw_check": sha(atlas_path) == raw_check["sha256"],
        "phase_counts": {s: sum(r["state"] == s for r in rows) for s in ("run", "attack")},
        "phase_layout_matches": len(rows) == 56 and {(r["state"], r["frame"]) for r in rows} ==
            {(state, frame) for state, count in (("run", 24), ("attack", 32)) for frame in range(count)},
        "sprites": len(names), "unique_names": len(set(names)), "out_of_bounds_rects": out_of_bounds,
        "overlapping_rects": rect_overlap, "serialized": serial,
        "geometry_candidates": geometry_candidates, "phases": rows,
    }
    (HERE / "qa_postpack.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    sheet = Image.new("RGB", (8 * 384, 7 * 220), (90, 90, 90))
    draw = ImageDraw.Draw(sheet)
    for index, (row, render) in enumerate(zip(rows, renders)):
        left, top = index % 8 * 384, index // 8 * 220
        draw.text((left + 4, top + 3), f'{row["state"]}{row["frame"]:02d}', fill="white")
        for col, background in enumerate(((225, 225, 225), (22, 25, 29))):
            tile = Image.new("RGBA", (512, 512), (*background, 255))
            tile.alpha_composite(render)
            sheet.paste(tile.convert("RGB").resize((192, 192), Image.Resampling.LANCZOS), (left + col * 192, top + 24))
    sheet.save(HERE / "qa_postpack-contact.jpg", quality=93)
    worst_indices = sorted(range(len(rows)), key=lambda i: rows[i]["changed_premul_pixels_over4"], reverse=True)[:4]
    detail = Image.new("RGB", (3 * 512, 4 * 544), (45, 45, 45))
    draw = ImageDraw.Draw(detail)
    for row_index, index in enumerate(worst_indices):
        y = row_index * 544
        row = rows[index]
        draw.text((8, y + 5), f'{row["state"]}{row["frame"]:02d}  atlas / production / premul error x8', fill="white")
        for col, render in enumerate((renders[index], frames[index])):
            tile = Image.new("RGBA", (512, 512), (210, 210, 210, 255))
            tile.alpha_composite(render)
            detail.paste(tile.convert("RGB"), (col * 512, y + 28))
        error = np.max(np.abs(premultiplied(renders[index]) - premultiplied(frames[index])), axis=2)
        heat = np.zeros((512, 512, 3), dtype="uint8")
        heat[:, :, 0] = np.minimum(error * 8, 255).astype("uint8")
        detail.paste(Image.fromarray(heat), (1024, y + 28))
    detail.save(HERE / "qa_postpack-worst.jpg", quality=95)
    print(json.dumps({"phases": len(rows), "sprites": len(names), "geometry_candidates": len(geometry_candidates),
                      "max_changed_premul_pixels_over4": max(r["changed_premul_pixels_over4"] for r in rows),
                      "max_errors_outside_filter_band": max(r["changed_pixels_outside_filter_band"] for r in rows),
                      "max_border_alpha": max(r["max_crop_border_alpha"] for r in rows), "serialized": serial}, indent=2))


if __name__ == "__main__":
    main()
