"""Read-only source study; writes diagnostic crops only beside this script."""
from pathlib import Path
import hashlib
import json
import numpy as np
from PIL import Image, ImageDraw

OUT = Path(__file__).resolve().parent
PROJECT = OUT.parents[3]
FAMILY = PROJECT / "Concepts/Used/Stalker/Postures05"
CANDIDATE = FAMILY / "CandidateAudit/SingleUVStudy"
ORIGIN = np.array([288.,288.])
FLIP = np.diag([1.,-1.])


def composite(image,background=(0,255,0,255)):
    result=Image.new("RGBA",image.size,background)
    result.alpha_composite(image.convert("RGBA"))
    return result.convert("RGB")


def around(image,center,size=64):
    x,y=np.rint(center).astype(int)
    return image.crop((x-size//2,y-size//2,x+size//2,y+size//2))


def main():
    anatomy=json.loads((PROJECT/"Concepts/Used/Stalker/Locomotion02/anatomy.json").read_text())
    pose=next(p for p in json.loads((FAMILY/"poses.json").read_text()) if p["row"]==7 and p["phase"]==0)
    atlas=Image.open(PROJECT/"Assets/EmergencyShift/Art/Enemies/Stalker/Locomotion02/rig-atlas.png").convert("RGBA")
    whole=Image.open(CANDIDATE/"single-control-raw.png").convert("RGBA")
    sheet=Image.new("RGB",(3*286,2*178),(0,255,0))
    draw=ImageDraw.Draw(sheet)
    records=[]
    for leg,limb in enumerate(anatomy["limbs"]):
        slot=leg*2+1
        src=atlas.crop((slot%4*358,slot//4*406,slot%4*358+358,slot//4*406+406))
        target=Image.open(CANDIDATE/f'leg-{limb["id"]}.png').convert("RGBA")
        knee=ORIGIN+FLIP@np.asarray(pose["knees"][leg][:2])*224
        hip=ORIGIN+FLIP@np.asarray(pose["hips"][leg][:2])*224
        col,row=leg%3,leg//3
        x,y=col*286,row*178
        draw.text((x+8,y+6),f'{limb["id"]} source | single UV, 2x nearest',fill="black")
        for index,(im,point) in enumerate(((src,limb["knee"]),(target,knee))):
            crop=composite(around(im,point))
            crop.save(OUT/f'singleUV-{limb["id"]}-{"source" if index==0 else "target"}-joint-native.png')
            sheet.paste(crop.resize((128,128),Image.Resampling.NEAREST),(x+8+index*136,y+28))
        # Whole-frame hip context, including torso overlap, retained separately.
        composite(around(whole,hip,80)).save(OUT/f'singleUV-{limb["id"]}-root-context-native.png')
        records.append({"id":limb["id"],"source_knee":limb["knee"],"target_knee":knee.tolist(),"target_hip":hip.tolist()})
    sheet.save(OUT/"singleUV-all-six-joints-source-compare.png")
    composite(around(whole,(288,294),272)).save(OUT/"singleUV-whole-native-tight.png")
    report={"candidate":"Concepts/Used/Stalker/Postures05/CandidateAudit/SingleUVStudy/single-control-raw.png", "candidate_sha256":hashlib.sha256((CANDIDATE/"single-control-raw.png").read_bytes()).hexdigest(), "pose":{"row":7,"phase":0}, "method":"Exact native crops for evidence; nearest-neighbor integer enlargement in comparison sheet. Candidate painter never executed or modified.", "joints":records}
    (OUT/"singleUV-independent-crop-manifest.json").write_text(json.dumps(report,indent=2),encoding="utf-8")
    study=FAMILY/"ClosedPoseStudy"
    if (study/"native-hook-directions.png").exists():
        proposed=Image.open(study/"native-hook-directions.png").convert("RGBA")
        proposed_pose=json.loads((study/"proposed-pose.json").read_text())
        for label,bg in (("green",(0,255,0,255)),("dark",(20,22,24,255)),("light",(230,230,230,255))):
            composite(around(proposed,(288,288),304),bg).save(OUT/f'closed-proposed-native-{label}.png')
        joints=Image.new("RGB",(3*286,2*178),(0,255,0))
        jd=ImageDraw.Draw(joints)
        for leg,limb in enumerate(anatomy["limbs"]):
            slot=leg*2+1
            src=atlas.crop((slot%4*358,slot//4*406,slot%4*358+358,slot//4*406+406))
            knee=ORIGIN+FLIP@np.asarray(proposed_pose["knees"][leg][:2])*224
            x,y=leg%3*286,leg//3*178
            jd.text((x+8,y+6),f'{limb["id"]} source | proposed, 2x nearest',fill="black")
            for index,(im,point) in enumerate(((src,limb["knee"]),(proposed,knee))):
                crop=composite(around(im,point))
                joints.paste(crop.resize((128,128),Image.Resampling.NEAREST),(x+8+index*136,y+28))
        joints.save(OUT/"closed-proposed-six-joints-source-compare.png")
        (OUT/"closed-proposed-reviewed-hashes.json").write_text(json.dumps({
            p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in (study/"native-hook-directions.png",study/"proposed-pose.json",study/"comparison.json")
        },indent=2),encoding="utf-8")


if __name__=="__main__":
    main()
