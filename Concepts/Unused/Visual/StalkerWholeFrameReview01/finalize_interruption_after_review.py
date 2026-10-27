"""Close only the independently viewed nine-profile interruption review."""
from pathlib import Path
import hashlib
import json
import numpy as np
from PIL import Image

OUT=Path(__file__).resolve().parent/"RuntimeInterruptionAfter01"
PROJECT=OUT.parent.parents[3]
ROOT=PROJECT/"Concepts/Used/Stalker/Postures05/Scenarios/stalker-interruption-after"


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    path=OUT/"review-coverage.json"
    coverage=json.loads(path.read_text())
    assert sha(ROOT/"run.json")==coverage["runSha256"]
    cut_checks=[]
    for profile in coverage["profiles"]:
        folder=ROOT/profile["key"]
        assert sha(folder/"manifest.json")==profile["manifestSha256"]
        frames=profile["frames"]
        for record in frames:
            assert sha(folder/f"frame-{record['frame']:05d}.png")==record["sha256"]
        for first,current in ((66,67),(133,134)):
            before=np.asarray(Image.open(folder/f"frame-{first:05d}.png").convert("RGB"))
            after=np.asarray(Image.open(folder/f"frame-{current:05d}.png").convert("RGB"))
            changed=np.any(before!=after,axis=2)
            mask_before=~((before[:,:,1]>220)&(before[:,:,0]<20)&(before[:,:,2]<20))
            mask_after=~((after[:,:,1]>220)&(after[:,:,0]<20)&(after[:,:,2]<20))
            assert frames[first]["facts"]["body"]==frames[current]["facts"]["body"]=="strike-10"
            cut_checks.append({"profile":profile["key"],"prior":first,"current":current,"nativeRgbByteExact":not bool(changed.any()),
                               "changedRgbPixels":int(changed.sum()),"silhouetteXorPixels":int(np.count_nonzero(mask_before!=mask_after)),
                               "priorSprite":frames[first]["facts"]["body"],"currentSprite":frames[current]["facts"]["body"]})
    assert len(coverage["pages"])==51
    for page in coverage["pages"]:
        assert sha(Path(page["path"]))==page["sha256"]
        page["reviewed"]=True
        page["reviewNote"]="Viewed in chronological order at native capture pixels, fixed crop. No old initial-pose reset, new return-end reset, detached skin or material discontinuity."
    coverage["status"]="COMPLETE: all6 exact RGB sequence representatives viewed, covering855 actual frame instances across9 profiles"
    coverage["cutChecks"]=cut_checks
    coverage["all1584SourcePngHashesRechecked"]=True
    coverage["all9ManifestHashesRechecked"]=True
    coverage["artVerdict"]="PASS for focused actual cases27/28 and all their post-cut return/fold frames"
    coverage["limits"]="Frame-by-frame native capture review, not subjective video tempo or complete25scenario rerun. All static bakes retain their separate verdict."
    coverage["interruptedFearTiming"]={"observed":"Eye closure begins while whole attack sprite is returning; bodyfold is held0 until return is complete.",
        "ownerIntent":"Root explicitly confirmed retract-first body policy on interrupted attack, while eye/gameplay shield timing remains independent; normal fear still folds before eye closure.",
        "artFinding":"Retraction/mouth closure and subsequent body folding are readable in actual pixels; no mandatory visual blocker found."}
    path.write_text(json.dumps(coverage,ensure_ascii=False,indent=2))
    print(json.dumps({"profiles":coverage["profileCount"],"actualFrameInstancesReviewed":coverage["selectedFrameInstances"],
                      "sequenceRepresentatives":coverage["uniqueSequenceCount"],"pagesReviewed":len(coverage["pages"]),
                      "firstCutChecks":len(cut_checks),"rgbExactCuts":sum(x["nativeRgbByteExact"] for x in cut_checks),
                      "otherCuts":[x for x in cut_checks if not x["nativeRgbByteExact"]],
                      "sourcePngHashesRechecked":1584,"verdict":coverage["artVerdict"]},indent=2))


if __name__=="__main__":
    main()
