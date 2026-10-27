"""Full mass body provenance/core invariance; no automatic art acceptance."""
from pathlib import Path
import json
import hashlib
import numpy as np
from PIL import Image,ImageFilter

OUT=Path(__file__).resolve().parent
PROJECT=OUT.parents[3]
FAMILY=PROJECT/"Concepts/Used/Stalker/Postures05"


def main():
    ah04=FAMILY/"CandidateAudit/ActualHeldOut04"
    manifest=json.loads((ah04/"manifest.json").read_text())
    compare=[]
    for name in manifest["rawPngSha256"]:
        if not name.startswith("body-"):
            continue
        old=np.array(Image.open(ah04/"Raw"/name).convert("RGBA"),int)
        new=np.array(Image.open(FAMILY/"Raw"/name).convert("RGBA"),int)
        changed=(abs(old-new)>0).any(axis=-1)
        visible=(old[:,:,3]>0)|(new[:,:,3]>0)
        compare.append({"name":name,"changedPixels":int(changed.sum()),"changedPixelsWithNonzeroAlpha":int((changed&visible).sum()),"changedAlphaPixels":int((old[:,:,3]!=new[:,:,3]).sum()),"maxVisibleChannelDifference":int(abs(old-new)[visible].max())})
    torso=np.array(Image.open(FAMILY/"Masks/torso.png").convert("RGBA"))
    peak_alpha=int(torso[:,:,3].max())
    core=np.asarray(Image.fromarray((torso[:,:,3]==peak_alpha).astype("uint8")*255).filter(ImageFilter.MinFilter(3)))>0
    reference=np.array(Image.open(FAMILY/"Raw/body-0-00.png").convert("RGBA"))
    invariance=[]
    for row in range(9):
        for phase in range(48):
            name=f"body-{row}-{phase:02d}.png"
            frame=np.array(Image.open(FAMILY/"Raw"/name).convert("RGBA"))
            delta=abs(frame[core].astype(int)-reference[core].astype(int))
            invariance.append({"name":name,"maxCoreRGBADifference":int(delta.max()),"changedCorePixels":int((delta>0).any(axis=1).sum())})
    report={"scope":"Actual mass pixel provenance and torso registration only; not art/volume acceptance.","AH04ManifestSha256":hashlib.sha256((ah04/"manifest.json").read_bytes()).hexdigest(),"heldoutComparisons":compare,"coreMaskScope":"Actual fixed torso maximum native alpha eroded1px; raw576coordinates, no fitting. This source has noalpha255; selectedalpha254 is NOT fully opaque, so underlying limbs may alter its compositedRGB slightly.","selectedTorsoAlpha":peak_alpha,"corePixelCount":int(core.sum()),"maximumCoreRGBADifferenceAcross432":max(p["maxCoreRGBADifference"] for p in invariance),"coreInvariance":invariance}
    (OUT/"FullBodyFinal04-provenance-core-check.json").write_text(json.dumps(report,indent=2))
    print(json.dumps({"AH04BodyFramesCompared":len(compare),"maximumVisibleDifferenceAgainstHeldout":max(p["maxVisibleChannelDifference"] for p in compare),"corePixels":int(core.sum()),"maximumCoreRGBADifferenceAcross432":report["maximumCoreRGBADifferenceAcross432"]},indent=2))


if __name__=="__main__":
    main()
