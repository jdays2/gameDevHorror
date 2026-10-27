"""Targeted native no-fit material review, preserving version03 evidence."""
from pathlib import Path
import json
import hashlib
from PIL import Image,ImageDraw,ImageFont

OUT=Path(__file__).resolve().parent
PROJECT=OUT.parents[3]
AUDIT=PROJECT/"Concepts/Used/Stalker/Postures05/CandidateAudit"
ROOT=AUDIT/"TargetedActual03"


def sha(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()


def main():
    manifest=json.loads((ROOT/"manifest.json").read_text())
    evidence=[]
    for name,expected in manifest["rawPngSha256"].items():
        actual=sha(ROOT/"Raw"/name)
        assert actual==expected
        evidence.append({"name":name,"sha256":actual})
    source=Image.open(ROOT/"targeted-native-green.png")
    source.crop((0,0,1728,1272)).save(OUT/"TA03-first-six-native-green.png")
    source.crop((0,1272,1728,1908)).save(OUT/"TA03-last-three-native-green.png")
    select=["body-6-00","body-6-24","body-7-00","body-1-25","entry-12-07","return-36-01"]
    font=ImageFont.truetype("C:/Windows/Fonts/arial.ttf",17)
    for label,color in (("dark",(23,27,31,255)),("light",(240,239,235,255))):
        sheet=Image.new("RGBA",(1728,1200),color)
        draw=ImageDraw.Draw(sheet)
        for index,name in enumerate(select):
            x,y=index%3*576,index//3*600
            sheet.alpha_composite(Image.open(ROOT/"Raw"/f"{name}.png").convert("RGBA"),(x,y+24))
            draw.text((x+4,y+2),name+" | native224PPU",font=font,fill="white" if label=="dark" else "black")
        sheet.convert("RGB").save(OUT/f"TA03-six-native-{label}.png")
    contexts=[("body-6-00",(342,312,422,392)),("body-6-24",(156,314,236,394)),("entry-12-07",(246,98,326,178))]
    compare=Image.new("RGB",(960,1080),"#00ff00")
    draw=ImageDraw.Draw(compare)
    for row,(name,rect) in enumerate(contexts):
        for col,(folder,label) in enumerate(((AUDIT/"ActualHeldOut02","old AH02"),(ROOT,"new TA03"))):
            im=Image.open(folder/"Raw"/f"{name}.png").convert("RGBA")
            bg=Image.new("RGBA",im.size,"#00ff00")
            bg.alpha_composite(im)
            compare.paste(bg.crop(rect).resize((320,320),Image.Resampling.NEAREST),(col*480,row*360+35))
            draw.text((col*480+4,row*360+5),f"{name} {label} | exact4x",font=font,fill="black")
    compare.save(OUT/"TA03-distal-contact-before-after-4x-nearest.png")
    (OUT/"TA03-independent-reviewed-manifest.json").write_text(json.dumps({"sourceManifestSha256":sha(ROOT/"manifest.json"),"scope":"Nine targeted stills only. Attack bridge coordinates03 are superseded by pending04; endpoint shape evidence remains versioned.","rawPngsVerified":evidence,"sourcePoseHashes":{k:v for k,v in manifest.items() if k.endswith("PosesSha256")}},indent=2))


if __name__=="__main__":
    main()
