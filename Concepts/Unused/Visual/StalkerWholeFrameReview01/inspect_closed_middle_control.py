"""Mechanical independent composites of versioned M-knee.06/toe-.10 control."""
from pathlib import Path
import hashlib
import json
from PIL import Image

OUT=Path(__file__).resolve().parent
PROJECT=OUT.parents[3]
STUDY=PROJECT/"Concepts/Used/Stalker/Postures05/CandidateAudit/ClosedMiddleOwnedStudy02"
RAW=STUDY/"Raw/closed-middle-k0.06-t-0.10.png"
im=Image.open(RAW).convert("RGBA")
expected=json.loads((STUDY/"manifest.json").read_text())["rawPngSha256"][RAW.name]
actual=hashlib.sha256(RAW.read_bytes()).hexdigest()
assert actual==expected
for label,color in (("green",(0,255,0,255)),("dark",(20,22,24,255)),("light",(230,230,230,255))):
    bg=Image.new("RGBA",im.size,color)
    bg.alpha_composite(im)
    # Fixed304px context, same coordinates for every background, no resizing.
    bg.crop((136,136,440,440)).convert("RGB").save(OUT/f'closed-middle-k06-tminus10-native-{label}.png')
(OUT/"closed-middle-k06-tminus10-reviewed.json").write_text(json.dumps({"source":str(RAW.relative_to(PROJECT)),"sha256":actual,"native_canvas":list(im.size),"fixed_crop":[136,136,440,440],"operation":"Integer crop and RGBA composite only. No renderer, source modification, filter, fit or resizing."},indent=2),encoding="utf-8")
