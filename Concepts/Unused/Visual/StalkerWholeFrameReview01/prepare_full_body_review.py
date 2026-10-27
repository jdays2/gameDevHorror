"""All432body native inspection pages; invoke only after frozen MASSREADY."""
from pathlib import Path
import argparse
import json
import hashlib
import numpy as np
from PIL import Image,ImageDraw,ImageFont
from inspect_staged_ah04 import composite

OUT=Path(__file__).resolve().parent
PROJECT=OUT.parents[3]
FAMILY=PROJECT/"Concepts/Used/Stalker/Postures05"


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--manifest",required=True)
    parser.add_argument("--raw",default=str(FAMILY/"Raw"))
    args=parser.parse_args()
    manifest_path=Path(args.manifest)
    frozen=json.loads(manifest_path.read_text())
    rawdir=Path(args.raw)
    poses=json.loads((FAMILY/"poses.json").read_bytes())
    assert len(poses)==432
    composition=json.loads((FAMILY/"body-composition.json").read_text())
    assert composition["poses"]==432 and composition["posesSha256"]==digest(FAMILY/"poses.json")
    assert composition["algorithm"]=="whole-native-continuous-polar"
    # A uniform576 canvas crop removes only the common empty frame margin.
    # It will be checked against every nonzero-alpha pixel before any page write.
    rect=(72,48,504,528)
    bounds=[]
    frames=[]
    pixels=[]
    for row in range(9):
        for phase in range(48):
            name=f"body-{row}-{phase:02d}.png"
            path=rawdir/name
            im=Image.open(path).convert("RGBA")
            assert im.size==(576,576)
            a=np.asarray(im)[:,:,3]
            yy,xx=np.nonzero(a>0)
            bbox=(int(xx.min()),int(yy.min()),int(xx.max()+1),int(yy.max()+1))
            assert bbox[0]>=rect[0] and bbox[1]>=rect[1] and bbox[2]<=rect[2] and bbox[3]<=rect[3],(name,bbox)
            bounds.append(bbox)
            frames.append({"name":name,"row":row,"phase":phase,"sha256":digest(path),"rawCanvasSize":[576,576],"nonzeroAlphaBounds":bbox})
            pixels.append(im)
    pages=OUT/"FullBodyFinal04NativePages"
    pages.mkdir(exist_ok=True)
    width,height=rect[2]-rect[0],rect[3]-rect[1]
    font=ImageFont.truetype("C:/Windows/Fonts/arial.ttf",17)
    page_records=[]
    for label,color in (("green",(0,255,0)),("dark",(23,27,31)),("light",(240,239,235))):
        for row in range(9):
            for block in range(8):
                sheet=Image.new("RGB",(3*width,2*(height+24)),color)
                draw=ImageDraw.Draw(sheet)
                names=[]
                for slot in range(6):
                    phase=block*6+slot
                    frame_index=row*48+phase
                    x,y=slot%3*width,slot//3*(height+24)
                    sheet.paste(composite(pixels[frame_index],color).crop(rect),(x,y+24))
                    name=frames[frame_index]["name"]
                    names.append(name)
                    draw.text((x+4,y+2),f"{name} | native224PPU | fixedcrop",font=font,fill="white" if label=="dark" else "black")
                path=pages/f"body-row{row}-phases{block*6:02d}-{block*6+5:02d}-{label}.png"
                sheet.save(path)
                page_records.append({"path":str(path),"background":label,"frames":names,"sha256":digest(path),"reviewed":False})
    report={"status":"PREPARED, visual review pending", "frozenManifestPath":str(manifest_path),"frozenManifestSha256":digest(manifest_path),"frozenManifest":frozen,"bodyComposition":composition,"bodyCompositionSha256":digest(FAMILY/"body-composition.json"),"bodyPosesSha256":digest(FAMILY/"poses.json"),"rawDirectory":str(rawdir),"frames":frames,"sharedCropPixels":rect,"cropScope":"Identical fixed pixel crop for all432; all nonzero-alpha pixels checked inside; no registration, scale change or spatial filtering. Image224PPU preserved. Backgrounds composited in linear light.","pages":page_records}
    (OUT/"FullBodyFinal04-review-coverage.json").write_text(json.dumps(report,indent=2))
    print(json.dumps({"frames":len(frames),"pages":len(page_records),"sharedCropPixels":rect,"overallNonzeroAlphaBounds":[min(b[0] for b in bounds),min(b[1] for b in bounds),max(b[2] for b in bounds),max(b[3] for b in bounds)]},indent=2))


if __name__=="__main__":
    main()
