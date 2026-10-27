"""Independent exact runtime interruption pages and deduplicated sequence proof."""
from pathlib import Path
import hashlib
import json
from collections import defaultdict
import numpy as np
from PIL import Image,ImageDraw,ImageFont

OUT=Path(__file__).resolve().parent/"RuntimeInterruptionAfter01"
PROJECT=OUT.parent.parents[3]
ROOT=PROJECT/"Concepts/Used/Stalker/Postures05/Scenarios/stalker-interruption-after"


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    OUT.mkdir(exist_ok=True)
    run=json.loads((ROOT/"run.json").read_text(encoding="utf-8-sig"))
    assert run["completedAll"] and not run["captureErrors"]
    expected=[x for x in run["completed"] if x["species"]=="stalker"]
    groups=defaultdict(list)
    profiles=[]
    selected_total=0
    rect=(96,64,544,576)
    for profile in expected:
        folder=ROOT/profile["key"]
        manifest=json.loads((folder/"manifest.json").read_text(encoding="utf-8-sig"))
        assert len(manifest["frames"])==profile["frames"]==176
        records=[]
        for frame in manifest["frames"]:
            path=folder/f"frame-{frame['frame']:05d}.png"
            im=Image.open(path).convert("RGB")
            records.append({"frame":frame["frame"],"sha256":sha(path),"decodedRgbSha256":hashlib.sha256(im.tobytes()).hexdigest(),"facts":frame})
        profiles.append({"key":profile["key"],"manifestSha256":sha(folder/"manifest.json"),"config":manifest["sourceVariantConfiguration"],"frames":records})
        for scenario in ("27-attack-to-hit-state-cut","28-attack-to-light-fear-state-cut"):
            selected=[r for r in records if r["facts"]["scenario"]==scenario and r["facts"]["local"]>=18]
            selected_total+=len(selected)
            seq=hashlib.sha256("".join(r["decodedRgbSha256"] for r in selected).encode()).hexdigest()
            groups[(scenario,seq)].append({"key":profile["key"],"records":selected})
    font=ImageFont.truetype("C:/Windows/Fonts/arial.ttf",15)
    pages=[]
    group_records=[]
    for group_index,((scenario,seq),members) in enumerate(groups.items()):
        representative=members[0]
        records=representative["records"]
        group_records.append({"index":group_index,"scenario":scenario,"sequenceDecodedRgbSha256":seq,"members":[m["key"] for m in members],"representative":representative["key"],"selectedFrames":[r["frame"] for r in records]})
        for start in range(0,len(records),6):
            batch=records[start:start+6]
            sheet=Image.new("RGB",(1344,1104),(23,27,31))
            draw=ImageDraw.Draw(sheet)
            for slot,record in enumerate(batch):
                frame=record["facts"]
                source=Image.open(ROOT/representative["key"]/f"frame-{frame['frame']:05d}.png").convert("RGB")
                pix=np.asarray(source)
                non_green=~((pix[:,:,1]>220)&(pix[:,:,0]<20)&(pix[:,:,2]<20))
                ys,xs=np.nonzero(non_green)
                assert xs.min()>=rect[0] and xs.max()<rect[2] and ys.min()>=rect[1] and ys.max()<rect[3],(representative["key"],frame["frame"])
                x,y=slot%3*448,slot//3*552
                sheet.paste(source.crop(rect),(x,y+40))
                draw.text((x+3,y),f"G{group_index} {frame['frame']} local{frame['local']} {frame['state']} / {frame['body']}",font=font,fill="white")
                draw.text((x+3,y+19),f"fold{frame['fold']:.3f} eye{frame['closure']:.3f} phase{frame['phase']:.4f}",font=font,fill="white")
            path=OUT/f"group-{group_index}-locals-{batch[0]['facts']['local']:02d}-{batch[-1]['facts']['local']:02d}-native.png"
            sheet.save(path)
            pages.append({"path":str(path),"sha256":sha(path),"group":group_index,"frames":[r["frame"] for r in batch],"reviewed":False})
    report={"scope":"All9 actual profiles: exact native interruption neighbors fromlocal18 through scenario end; duplicate full RGB sequences proved byte-identical","runSha256":sha(ROOT/"run.json"),"runCompleted":run["completed"],"profileCount":len(expected),"profiles":profiles,"selectedFrameInstances":selected_total,"uniqueSequenceCount":len(groups),"groups":group_records,"pages":pages,"presentation":{"crop":rect,"scale":1,"fit":False,"filter":False,"nativePixels":"actual640 camera captures, no normalization or re-render"}}
    (OUT/"review-coverage.json").write_text(json.dumps(report,ensure_ascii=False,indent=2))
    print(json.dumps({"profiles":len(expected),"frameInstances":selected_total,"groups":group_records,"pages":len(pages)},indent=2))


if __name__=="__main__":
    main()
