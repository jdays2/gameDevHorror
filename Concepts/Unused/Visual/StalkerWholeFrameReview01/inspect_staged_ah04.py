"""Independent six staged04 stills + body-version comparisons; native pixels."""
from pathlib import Path
import json
import hashlib
import numpy as np
from PIL import Image,ImageDraw,ImageFont

OUT=Path(__file__).resolve().parent
PROJECT=OUT.parents[3]
AUDIT=PROJECT/"Concepts/Used/Stalker/Postures05/CandidateAudit"
ROOT=AUDIT/"ActualHeldOut04"


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def composite(raw,color):
    rgba=np.asarray(raw.convert("RGBA"),float)/255
    rgb=rgba[:,:,:3]
    linear=np.where(rgb<=.04045,rgb/12.92,((rgb+.055)/1.055)**2.4)
    bg=np.array(color,float)/255
    bg=np.where(bg<=.04045,bg/12.92,((bg+.055)/1.055)**2.4)
    linear=linear*rgba[:,:,3:4]+bg*(1-rgba[:,:,3:4])
    encoded=np.where(linear<=.0031308,linear*12.92,1.055*np.maximum(linear,0)**(1/2.4)-.055)
    return Image.fromarray(np.rint(np.clip(encoded,0,1)*255).astype("uint8"))


def main():
    manifest=json.loads((ROOT/"manifest.json").read_text())
    frames=[]
    for name,expected in manifest["rawPngSha256"].items():
        actual=digest(ROOT/"Raw"/name)
        assert actual==expected
        frames.append({"name":name,"sha256":actual})
    selected=["entry-03-01","entry-03-02","entry-03-03","return-27-04","return-27-05","return-27-06"]
    font=ImageFont.truetype("C:/Windows/Fonts/arial.ttf",17)
    for label,color in (("green",(0,255,0)),("dark",(23,27,31)),("light",(240,239,235))):
        sheet=Image.new("RGB",(1728,1200),color)
        draw=ImageDraw.Draw(sheet)
        for i,name in enumerate(selected):
            x,y=i%3*576,i//3*600
            frame=composite(Image.open(ROOT/"Raw"/f"{name}.png"),color)
            sheet.paste(frame,(x,y+24))
            draw.text((x+4,y+3),name+" | exact576/224PPU",font=font,fill="white" if label=="dark" else "black")
        sheet.save(OUT/f"AH04-staged-six-native-{label}.png")
    body_comparisons=[]
    oldmanifest=json.loads((AUDIT/"TargetedActual03/manifest.json").read_text())
    for name in oldmanifest["rawPngSha256"]:
        if not name.startswith("body-"):
            continue
        a=np.asarray(Image.open(AUDIT/"TargetedActual03/Raw"/name).convert("RGBA"),int)
        b=np.asarray(Image.open(ROOT/"Raw"/name).convert("RGBA"),int)
        delta=abs(a-b)
        yy,xx=np.nonzero((delta>0).any(axis=-1))
        changed=(delta>0).any(axis=-1)
        visible=(a[:,:,3]>0)|(b[:,:,3]>0)
        body_comparisons.append({"name":name,"changedPixels":len(xx),"maximumRGBADelta":int(delta.max()),"changedAlphaPixels":int((delta[:,:,3]>0).sum()),"changedPixelsWithNonzeroAlpha":int((changed&visible).sum()),"maxChangedPixelAlpha":int(np.maximum(a[:,:,3],b[:,:,3])[changed].max()) if changed.any() else None,"bbox":[int(xx.min()),int(yy.min()),int(xx.max()+1),int(yy.max()+1)] if len(xx) else None})
    report={"sourceManifestSha256":digest(ROOT/"manifest.json"),"sourcePoseHashes":{key:value for key,value in manifest.items() if key.endswith("PosesSha256")},"rawPngsVerified":frames,"stagedStillsReviewed":selected,"bodyAgainstTA03PixelComparisons":body_comparisons,"compositing":"Linear light alpha composite from frozen straightRGBA; no spatial filtering, registration or scale change."}
    (OUT/"AH04-independent-staged-review-manifest.json").write_text(json.dumps(report,indent=2))
    print(json.dumps({"verifiedFrames":len(frames),"bodyComparisons":body_comparisons},indent=2))


if __name__=="__main__":
    main()
