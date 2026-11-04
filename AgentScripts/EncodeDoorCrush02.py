"""Encode native game frames with measured timing; no synthesized or repainted imagery."""
from pathlib import Path
import json
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
for species in ("stalker", "sleeper"):
    source = ROOT / "work/door-crush02" / species
    frames = json.loads((source / "frames.json").read_text(encoding="utf-8-sig"))
    images = []
    durations = []
    for index, frame in enumerate(frames):
        with Image.open(source / frame["filename"]) as native:
            preview = native.convert("RGB")
            preview.thumbnail((960, 720), Image.Resampling.LANCZOS)
            images.append(preview)
        seconds = frames[index + 1]["seconds"] - frame["seconds"] if index + 1 < len(frames) else .7
        durations.append(max(20, round(seconds * 100) * 10))
    destination = ROOT / "Docs/Validation/DoorCrush02" / (species + "-live.webp")
    images[0].save(destination, save_all=True, append_images=images[1:], duration=durations, loop=0, quality=90, method=4)
    print(json.dumps({"species": species, "frames": len(images), "milliseconds": sum(durations),
                      "size": destination.stat().st_size, "file": str(destination)}))
    for image in images:
        image.close()
