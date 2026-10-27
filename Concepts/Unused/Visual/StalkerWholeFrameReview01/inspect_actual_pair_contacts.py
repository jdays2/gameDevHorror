"""Actual visible pair contacts isolated without repainting or reposing."""
from pathlib import Path
import json
import numpy as np
from PIL import Image,ImageDraw,ImageFont

OUT=Path(__file__).resolve().parent
PROJECT=OUT.parents[3]
ROOT=PROJECT/"Concepts/Used/Stalker/Postures05/CandidateAudit/ActualHeldOut02"


def main():
    proof=json.loads((ROOT/"proof.json").read_text())
    records={r["name"]:r for r in proof["cases"]}
    selected=[("body-6-00",(3,5)),("body-6-24",(2,4)),("body-2-25",(1,3)),("entry-12-07",(0,1))]
    sheet=Image.new("RGB",(1280,1440),"#00ff00")
    draw=ImageDraw.Draw(sheet)
    font=ImageFont.truetype("C:/Windows/Fonts/arial.ttf",16)
    def composite(image):
        bg=Image.new("RGBA",image.size,"#00ff00")
        bg.alpha_composite(image)
        return bg
    rows=[]
    for row,(name,pair) in enumerate(selected):
        entry=next(p for p in records[name]["all15Pairs"] if tuple(p["pair"])==pair)
        x0,y0,x1,y1=entry["bbox"]
        cx,cy=(x0+x1)//2,(y0+y1)//2
        rect=(cx-40,cy-40,cx+40,cy+40)
        raw=Image.open(ROOT/"Raw"/f"{name}.png").convert("RGBA")
        prefix=name[5:] if name.startswith("body-") else name
        skins=[Image.open(ROOT/"Masks"/f"leg-{prefix}-{leg}.png").convert("RGBA") for leg in pair]
        torso=Image.open(ROOT/"Masks"/f"torso-{name}.png").convert("RGBA")
        alphas=[np.asarray(im)[:,:,3] for im in skins]
        hit=(alphas[0]>128)&(alphas[1]>128)&(np.asarray(torso)[:,:,3]<=128)
        marked=np.asarray(composite(raw).convert("RGB")).copy()
        marked[hit]=(255,0,255)
        images=[composite(raw),*(composite(im) for im in skins),Image.fromarray(marked)]
        for col,(im,title) in enumerate(zip(images,["actual whole","isolated leg "+str(pair[0]),"isolated leg "+str(pair[1]),"overlap magenta"])):
            crop=im.crop(rect).resize((320,320),Image.Resampling.NEAREST)
            sheet.paste(crop,(col*320,row*360+40))
            draw.text((col*320+4,row*360+3),f"{name}: {title}",fill="black",font=font)
            draw.text((col*320+4,row*360+21),f"{entry['pixels']} px >128; exact4x",fill="black",font=font)
        rows.append({"name":name,"pair":pair,"sourceContact":entry,"contextRect":rect,"independentlyCountedOverlapPixels":int(hit.sum())})
    sheet.save(OUT/"AH02-genuine-contact-contexts-4x-nearest.png")
    (OUT/"AH02-contact-contexts.json").write_text(json.dumps(rows,indent=2))


if __name__=="__main__":
    main()
