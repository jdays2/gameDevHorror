"""Frozen H01 evidence only; no renderer execution or production writes."""
from pathlib import Path
import hashlib
import json
from PIL import Image, ImageDraw

OUT=Path(__file__).resolve().parent
PROJECT=OUT.parents[3]
H01=PROJECT/"Concepts/Used/Stalker/Postures05/CandidateAudit/HeldOut01"


def background(im,color):
    result=Image.new("RGBA",im.size,color)
    result.alpha_composite(im.convert("RGBA"))
    return result.convert("RGB")


def main():
    manifest=json.loads((H01/"manifest.json").read_text())
    checked=[]
    for name,expected in manifest["rawPngSha256"].items():
        path=H01/"Raw"/name
        got=hashlib.sha256(path.read_bytes()).hexdigest()
        assert got==expected, name
        checked.append({"name":name,"sha256":got})
    selected=["body-0-00.png","body-0-12.png","body-4-24.png","body-7-24.png","body-8-00.png","strike-09.png"]
    for name in selected:
        im=Image.open(H01/"Raw"/name).convert("RGBA")
        panel=Image.new("RGB",(1152,616),(0,0,0))
        d=ImageDraw.Draw(panel)
        for col,(label,color) in enumerate((("dark",(20,22,24,255)),("light",(230,230,230,255)))):
            d.rectangle((col*576,0,(col+1)*576-1,39),fill=color[:3])
            d.text((col*576+8,10),f'{name}: {label}; native576',fill=(240,240,240) if col==0 else (10,10,10))
            panel.paste(background(im,color),(col*576,40))
        panel.save(OUT/("H01-"+Path(name).stem+"-dark-light-native.png"))
    # Four fixed body contexts, same crop extent; no fit or interpolation.
    root_names=["body-0-00.png","body-0-12.png","body-0-24.png","body-0-36.png","body-7-24.png","body-8-00.png"]
    panel=Image.new("RGB",(3*460,2*440),(0,255,0))
    d=ImageDraw.Draw(panel)
    for i,name in enumerate(root_names):
        im=Image.open(H01/"Raw"/name).convert("RGBA")
        crop=background(im.crop((178,188,398,388)),(0,255,0,255))
        x,y=i%3*460,i//3*440
        d.text((x+8,y+8),name+" torso/root context; 2x nearest",fill="black")
        panel.paste(crop.resize((440,400),Image.Resampling.NEAREST),(x+8,y+32))
    panel.save(OUT/"H01-root-contexts-2x-nearest.png")
    (OUT/"H01-independent-reviewed-manifest.json").write_text(json.dumps({"version":"H01","manifest_sha256":hashlib.sha256((H01/"manifest.json").read_bytes()).hexdigest(),"raw_png_hashes_verified":checked,"inspection":"All21 frozen frames viewed on native green sheets. These six cases also composited against dark/light at native576, with fixed source crops for root scrutiny. No painter was run."},indent=2),encoding="utf-8")


if __name__=="__main__":
    main()
