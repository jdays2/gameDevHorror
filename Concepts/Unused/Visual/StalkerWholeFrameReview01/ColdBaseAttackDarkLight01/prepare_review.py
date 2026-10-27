"""Read-only native-size review of the cold base attack; writes evidence here only."""
from pathlib import Path
import hashlib
import json
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
PROJECT = HERE.parents[4]
SOURCE = PROJECT / "Concepts/Used/Sleeper/Postures05"
BACKGROUNDS = {"dark": (18, 22, 26), "light": (235, 235, 235)}
CROP = (132, 63, 446, 520)
CELL = (326, 489)
SIZE = (CELL[0] * 4, CELL[1] * 3)


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def composite(rgba, colour):
    return Image.alpha_composite(Image.new("RGBA", rgba.size, (*colour, 255)), rgba).convert("RGB")


def main():
    dependencies = {name: sha(SOURCE / name) for name in (
        "attack-composition.json", "attack-sprites.json", "attack-packing.json")}
    composition = json.loads((SOURCE / "attack-composition.json").read_text())
    manifest = json.loads((SOURCE / "attack-sprites.json").read_text())
    expected = {r["name"]: r["rawPngSha256"] for r in composition["records"]}
    names = [r["name"] for r in manifest["frames"]]
    assert len(names) == len(set(names)) == 792
    assert set(names) == set(expected)
    groups, raw_hashes, alpha_union = {}, {}, [576, 576, 0, 0]
    for name in names:
        path = SOURCE / "Raw" / f"{name}.png"
        raw_hashes[name] = sha(path)
        assert raw_hashes[name] == expected[name], name
        with Image.open(path) as im:
            rgba = im.convert("RGBA")
        assert rgba.size == (576, 576)
        bbox = rgba.getchannel("A").getbbox()
        assert bbox and bbox[0] >= CROP[0] and bbox[1] >= CROP[1] and bbox[2] <= CROP[2] and bbox[3] <= CROP[3], (name, bbox)
        alpha_union = [min(alpha_union[0], bbox[0]), min(alpha_union[1], bbox[1]), max(alpha_union[2], bbox[2]), max(alpha_union[3], bbox[3])]
        digest = hashlib.sha256(rgba.tobytes()).hexdigest()
        groups.setdefault(digest, []).append(name)
    assert len(groups) == 468
    by_name = {name: digest for digest, aliases in groups.items() for name in aliases}
    strike_names = [f"strike-{i:02d}" for i in range(24)]
    strike_hashes = {by_name[name] for name in strike_names}
    records = [{"representative": name, "rgbaSha256": by_name[name], "aliases": groups[by_name[name]], "explicitStrike": True} for name in strike_names]
    records.extend({"representative": aliases[0], "rgbaSha256": digest, "aliases": aliases, "explicitStrike": False} for digest, aliases in groups.items() if digest not in strike_hashes)
    assert len(records) == 471
    pages = []
    for background, colour in BACKGROUNDS.items():
        folder = HERE / background
        folder.mkdir(exist_ok=True)
        for page in range((len(records) + 11) // 12):
            subset = records[page * 12:(page + 1) * 12]
            sheet = Image.new("RGB", SIZE, colour)
            draw = ImageDraw.Draw(sheet)
            for slot, record in enumerate(subset):
                with Image.open(SOURCE / "Raw" / f"{record['representative']}.png") as im:
                    rgba = im.convert("RGBA")
                assert hashlib.sha256(rgba.tobytes()).hexdigest() == record["rgbaSha256"]
                native = composite(rgba, colour).crop(CROP)
                x, y = slot % 4 * CELL[0] + 6, slot // 4 * CELL[1] + 26
                sheet.paste(native, (x, y))
                draw.text((x, y - 18), record["representative"], fill=(210, 210, 210) if background == "dark" else (25, 25, 25))
                assert sheet.crop((x, y, x + 314, y + 457)).tobytes() == native.tobytes()
            output = folder / f"page-{page:03d}.png"
            sheet.save(output)
            pages.append({"background": background, "page": page, "path": str(output.relative_to(PROJECT)), "sha256": sha(output), "representatives": [r["representative"] for r in subset], "coveredNames": sorted({n for r in subset for n in r["aliases"]}), "nativeCompositePayloadExact": True, "viewedOriginal": False})
    assert all(sha(SOURCE / name) == digest for name, digest in dependencies.items())
    assert all(sha(SOURCE / "Raw" / f"{name}.png") == digest for name, digest in raw_hashes.items())
    result = {"status": "PREPARED; VISUAL REVIEW PENDING", "source": str(SOURCE.relative_to(PROJECT)), "scope": "Base 792 cold attacks only; native skin/blood/mouth, edge opacity, clipping and limb contacts on DARK/LIGHT. No runtime playback or author approval claim.", "sourceDependencySha256": dependencies, "sourceRawPngSha256": raw_hashes, "inputHashesUnchanged": True, "sourceFrameCount": 792, "exactUniqueWholeRgba": 468, "exactDuplicateAliases": 324, "displayedTilesPerBackground": 471, "all24StrikeFramesExplicit": True, "pageSizePixels": list(SIZE), "fixedCropPixels": list(CROP), "nonzeroAlphaUnionPixels": alpha_union, "allNonzeroAlphaInsideFixedCrop": True, "resampling": False, "backgroundsRgb": BACKGROUNDS, "composition": "Pillow source-over exact 8bit RGBA onto opaque sRGB background; pixel payload independently compared before writing", "records": records, "pages": pages, "visualFindings": []}
    (HERE / "review-coverage.json").write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps({"sourceFrames": len(names), "uniqueRgba": len(groups), "explicitStrike": len(strike_names), "tilesEachBackground": len(records), "pages": len(pages), "size": SIZE, "alphaUnion": alpha_union, "dependencyHashes": dependencies}))


if __name__ == "__main__":
    main()
