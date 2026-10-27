"""Frozen AH02 provenance and native inspection boards; audit output only."""
from pathlib import Path
import json
import hashlib
import numpy as np
from PIL import Image,ImageDraw,ImageFont

OUT=Path(__file__).resolve().parent
PROJECT=OUT.parents[3]
ROOT=PROJECT/"Concepts/Used/Stalker/Postures05/CandidateAudit/ActualHeldOut02"
IDS=["LF","RF","LM","RM","LR","RR"]
SELECTED=["body-0-00","body-0-12","body-4-24","body-6-12","body-8-00","strike-09"]


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    manifest=json.loads((ROOT/"manifest.json").read_text())
    proof=json.loads((ROOT/"proof.json").read_text())
    poses=json.loads((ROOT/"poses.json").read_text())
    cases={p["name"]:p for p in poses}
    assert manifest["frames"]==proof["frames"]==len(poses)==59
    observed=[]
    for name,expected in manifest["rawPngSha256"].items():
        actual=sha(ROOT/"Raw"/name)
        observed.append({"name":name,"sha256":actual,"matchesFrozenManifest":actual==expected})
    assert all(r["matchesFrozenManifest"] for r in observed)
    font=ImageFont.truetype("C:/Windows/Fonts/arial.ttf",17)
    for label,color in (("dark",(23,27,31,255)),("light",(240,239,235,255))):
        sheet=Image.new("RGBA",(1728,1200),color)
        draw=ImageDraw.Draw(sheet)
        for i,name in enumerate(SELECTED):
            x,y=(i%3)*576,(i//3)*600
            frame=Image.open(ROOT/"Raw"/f"{name}.png").convert("RGBA")
            sheet.alpha_composite(frame,(x,y+24))
            draw.text((x+5,y+2),f"AH02 {name} | exact native576canvas/224PPU",fill="white" if label=="dark" else "black",font=font)
        sheet.convert("RGB").save(OUT/f"AH02-six-independent-native-{label}.png")
    names=["body-0-00","body-0-12","body-6-12","body-8-00"]
    joints=Image.new("RGB",(1296,840),"#00ff00")
    draw=ImageDraw.Draw(joints)
    for row,name in enumerate(names):
        p=cases[name]
        raw=Image.open(ROOT/"Raw"/f"{name}.png").convert("RGBA")
        bg=Image.new("RGBA",raw.size,"#00ff00")
        bg.alpha_composite(raw)
        for leg in range(6):
            xy=np.rint(np.array([288,288])+np.array(p["knees"][leg][:2])*[224,-224]).astype(int)
            crop=bg.crop((xy[0]-42,xy[1]-42,xy[0]+42,xy[1]+42)).resize((168,168),Image.Resampling.NEAREST)
            joints.paste(crop,(leg*216,row*210+35))
            draw.text((leg*216+3,row*210+7),f"{name} {IDS[leg]} |2x",fill="black",font=font)
    joints.save(OUT/"AH02-selected-six-joints-2x-nearest.png")
    pair_hits=[{"case":r["name"],"pairs":[p for p in r["all15Pairs"] if p["pixels"]]} for r in proof["cases"] if any(p["pixels"] for p in r["all15Pairs"])]
    report={"scope":"Independent visual review provenance. All54body+5attack green panels viewed. Six selected dark/light native composites and24joint nearest2x crops prepared for followup inspection. This manifest alone does not establish an art PASS.","frozenManifestSha256":sha(ROOT/"manifest.json"),"bodySourceHashMatchesCurrent":manifest["bodyPosesSha256"]==sha(ROOT.parent.parent/"poses.json"),"attackSourceHashMatchesCurrent":manifest["attackPosesSha256"]==sha(ROOT.parent.parent/"attack-poses.json"),"frames":len(observed),"rawPngHashes":observed,"proofCasesWithPairPixels":pair_hits,"selectedDarkLight":SELECTED,"selectedJointFrames":names,"limits":["59samples are not all432+792 skin frames.","Sparse stills cannot establish motion cadence, silhouette pop or full cycle closure.","No deliberate volume increase is present. OldH01 remains separate and failed."]}
    (OUT/"AH02-independent-reviewed-manifest.json").write_text(json.dumps(report,indent=2))
    print(json.dumps({key:report[key] for key in ("frames","bodySourceHashMatchesCurrent","attackSourceHashMatchesCurrent","proofCasesWithPairPixels")},indent=2))


if __name__=="__main__":
    main()
