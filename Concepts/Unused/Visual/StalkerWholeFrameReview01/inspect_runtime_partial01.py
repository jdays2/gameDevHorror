"""Evidence for completed first actual-runtime source, before exit-cache fix.

Only local audit outputs are written. Source captures are never modified.
"""
from pathlib import Path
import hashlib
import json
from PIL import Image, ImageDraw, ImageFont

OUT = Path(__file__).resolve().parent / "RuntimePartial01"
PROJECT = OUT.parent.parents[3]
SOURCE = PROJECT / "Concepts/Used/Stalker/Postures05/Scenarios/stalker-final/MechanicsSandbox-stalker-20hp-default-stationary-990b0376"


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    OUT.mkdir(exist_ok=True)
    raw = OUT / "RawEvidence"
    raw.mkdir(exist_ok=True)
    manifest = json.loads((SOURCE / "manifest.json").read_text(encoding="utf-8-sig"))
    frames = manifest["frames"]
    selected = list(range(464, 482)) + [1339,1340,1341,1342,1406,1407,1408,1409] + list(range(1449,1460))
    records = []
    for index in selected:
        path = SOURCE / f"frame-{index:05d}.png"
        (raw / path.name).write_bytes(path.read_bytes())
        records.append({"frame": index, "sha256": sha(path), "facts": frames[index]})
    font = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 17)

    def sheet(indices, name):
        # Four exact640 captures on a native1280x1360 page. No fit/crop/filter.
        page = Image.new("RGB", (1280, 1360), (23,27,31))
        draw = ImageDraw.Draw(page)
        for slot,index in enumerate(indices):
            frame = frames[index]
            x,y=slot%2*640,slot//2*680
            page.paste(Image.open(raw / f"frame-{index:05d}.png").convert("RGB"),(x,y+40))
            draw.text((x+4,y+1),f"{index}: {frame['state']} / {frame['body']}",font=font,fill="white")
            draw.text((x+4,y+20),f"fold {frame['fold']:.3f}; eye {frame['closure']:.3f}; phase {frame['phase']:.4f}",font=font,fill="white")
        path=OUT/name
        page.save(path)
        return {"name":name,"sha256":sha(path),"frames":indices,"rescale":False,"crop":False}

    pages = [sheet([1339,1340,1341,1342],"case27-attack-hit-actual-native.png"),
             sheet([1406,1407,1408,1409],"case28-attack-fear-actual-native.png")]
    for start in range(464,482,4):
        pages.append(sheet(list(range(start,min(start+4,482))),f"fear-native-{start}-{min(start+3,481)}.png"))
    walking=[]
    for i in range(46,135):
        prior,current=frames[i-1],frames[i]
        delta=(current["phase"]-prior["phase"])%1
        walking.append(delta*60)
    report={"scope":"PARTIAL actual runtime; one complete20HP source, not global9profile approval",
            "source":str(SOURCE),"manifestSha256":sha(SOURCE/"manifest.json"),"sourceFrameCount":len(frames),
            "recordedSourceConfig":manifest["sourceVariantConfiguration"],"sourceNote":manifest["note"],
            "records":records,"pages":pages,
            "interruptionVerdict":"FAIL: abrupt whole-pose strike10 to locomotion/fold selection at1340/1341 and1407/1408. All six limb arrangements change while phase stays fixed.",
            "walkUpMeasuredCyclesPerSecondAt60Hz":{"min":min(walking),"max":max(walking),"mean":sum(walking)/len(walking)},
            "walkCadenceScope":"Recorded phase advance only; subjective perceived tempo requires native playback.",
            "fearTimingFacts":"Fold starts465 while eye closure remains0 through473 (fold.625); closure begins474(fold.6944), reaches1 on477 as retreat starts. Retreat fold tends to.5 while gait advances.",
            "fearVisualReview":"Exact640 captures464-481 independently viewed. Body fold leads shutter closure, continuous skin; no additional material artifact observed.",
            "deathVisualReview":"Native1449/1450/1456/1457/1458/1459 independently viewed. Last live body-4-00 stays until1457; torso replaced by remains at1458, one death event and one remains root. Camera deliberately changes ortho1.65 to4.5 on1450; screen shrink is not body-scale change. Body local transform remains identity.",
            "reviewPending":"Other profiles and fixed exit cache are not present in this snapshot; no global runtime approval."}
    (OUT/"partial-runtime-evidence.json").write_text(json.dumps(report,ensure_ascii=False,indent=2))
    print(json.dumps({"records":len(records),"pages":[x["name"] for x in pages],"walkHz":report["walkUpMeasuredCyclesPerSecondAt60Hz"]},indent=2))


if __name__=="__main__":
    main()
