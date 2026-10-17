"""Bake Stalker's fixed dead eye and three compatible corpse compositions.

Run from the Unity project with Pillow and numpy. Original death art is read
from Git, so repeated runs do not accumulate overlays. Live art is not written.
"""
from io import BytesIO
from pathlib import Path
import json
import subprocess

import numpy as np
from PIL import Image

from creature_eye import dead_eye_organ, pale_eye
from apply_sleeper_blood_02 import corpse_parts, patches


PROJECT = Path(__file__).resolve().parents[2]
ROOT = PROJECT / "Assets/EmergencyShift/Art/Enemies/Stalker"
SOURCE_REVISION = "7acb25c"
EYE_CENTRES = [(313, 320), (938, 325), (331, 929), (938, 932)]


def stalker_skin(image):
    """Convert only cool outer skin; keep the red tears and tissue intact."""
    data = np.asarray(image).copy()
    rgb = data[:, :, :3].astype(np.float32)
    redness = rgb[:, :, 0] - np.maximum(rgb[:, :, 1], rgb[:, :, 2])
    weight = np.clip((24 - redness) / 16, 0, 1)[:, :, None]
    luminance = (rgb[:, :, 0] * .24 + rgb[:, :, 1] * .64 + rgb[:, :, 2] * .12)[:, :, None]
    pink = luminance * np.array([1.12, .88, .95])
    data[:, :, :3] = np.rint(rgb * (1 - weight) + pink * weight).clip(0, 255).astype(np.uint8)
    return Image.fromarray(data)


def main():
    path = ROOT / "PaleFlesh/death-sheet.png"
    relative = path.relative_to(PROJECT).as_posix()
    source = subprocess.check_output(["git", "show", f"{SOURCE_REVISION}:{relative}"], cwd=PROJECT)
    original = Image.open(BytesIO(source)).convert("RGBA")
    atlas = Image.open(ROOT / "LargeEye01/eye-atlas.png").convert("RGBA")
    # Keep Stalker's pink membrane, with the same white ocular surface as
    # the other dead eye. The live eye atlas remains untouched.
    atlas = pale_eye(atlas, (312, 333), (149, 140))
    full_organ = dead_eye_organ(atlas)
    organ = full_organ.resize((150, 150), Image.Resampling.LANCZOS)
    output = original.copy()
    for centre in EYE_CENTRES:
        output.alpha_composite(organ, (round(centre[0] - 75), round(centre[1] - 289 / 560 * 150)))
    # Preserve the existing death silhouette, torn holes and atlas geometry.
    output.putalpha(original.getchannel("A"))
    data = np.asarray(output).copy()
    before = np.asarray(original)
    data[before[:, :, 3] == 0] = before[before[:, :, 3] == 0]
    output = Image.fromarray(data)
    output.save(path)
    assert np.array_equal(before[:, :, 3], data[:, :, 3])
    parts = corpse_parts(full_organ, patches(), ROOT / "Remains04", stalker_skin)
    print(json.dumps({"updatedDeathFrames": len(EYE_CENTRES), "corpseParts": len(parts), "alphaUnchanged": True, "sourceRevision": SOURCE_REVISION}))


if __name__ == "__main__":
    main()
