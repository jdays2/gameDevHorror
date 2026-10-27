"""Independent shaft width observations; reads frozen skin, writes audit only."""
from pathlib import Path
import sys
import json
import hashlib
import numpy as np
from PIL import Image

OUT = Path(__file__).resolve().parent
PROJECT = OUT.parents[3]
AUDIT = PROJECT / "Concepts/Used/Stalker/Postures05/CandidateAudit"
sys.path.insert(0, str(AUDIT))
sys.path.insert(0, str(AUDIT.parent))
from continuous_polar_painter import SpritePainter
from source_ownership_refined import owned_materials
from native_uv_painter import ORIGIN, FLIP, PPU


def width(alpha, knee, hip, fraction):
    delta = hip-knee
    normal = np.array([-delta[1], delta[0]])/np.linalg.norm(delta)
    distances = np.arange(-45,45.001,.125)
    points = np.rint(knee+fraction*delta+distances[:,None]*normal).astype(int)
    valid = ((points>=0).all(axis=1)&(points[:,0]<alpha.shape[1])&(points[:,1]<alpha.shape[0]))
    opaque = np.zeros(len(points),bool)
    opaque[valid] = alpha[points[valid,1],points[valid,0]]>127
    centre = len(distances)//2
    if not opaque[centre]:
        return {"fractionKneeToHip":fraction,"axisCovered":False,"widthPixels":None}
    a=b=centre
    while a>0 and opaque[a-1]:
        a-=1
    while b<len(opaque)-1 and opaque[b+1]:
        b+=1
    return {"fractionKneeToHip":fraction,"axisCovered":True,"widthPixels":float(distances[b]-distances[a]+.125),"edgeOffsetsPixels":[float(distances[a]),float(distances[b])]}


def main():
    folder=AUDIT/"ClosedMiddleOwnedStudy02"
    name="closed-middle-k0.06-t-0.10"
    pose=json.loads((folder/"proof.json").read_text())["cases"][1]["pose"]
    painter=SpritePainter(PROJECT)
    sources,_,_,_=owned_materials(painter)
    records=[]
    for i,limb in enumerate(painter.anatomy["limbs"]):
        target=np.array(Image.open(folder/"Masks"/f"leg-{name}-{i}.png"))[:,:,3]
        source=sources[i][:,:,3]*255
        sk,sh=[np.asarray(limb[key],float) for key in ("knee","hip")]
        tk,th=[ORIGIN+FLIP@np.asarray(pose[key][i][:2])*PPU for key in ("knees","hips")]
        record={"id":limb["id"],"sourceProjectedUpperLengthPixels":float(np.linalg.norm(sh-sk)),"targetProjectedUpperLengthPixels":float(np.linalg.norm(th-tk)),"samples":[]}
        for f in (.35,.5,.65):
            s,t=width(source,sk,sh,f),width(target,tk,th,f)
            record["samples"].append({"fractionKneeToHip":f,"source":s,"target":t,"targetToSourceWidthRatio":t["widthPixels"]/s["widthPixels"] if s["axisCovered"] and t["axisCovered"] else None})
        records.append(record)
    report={"status":"Width observations only; not automatic artistic acceptance", "rawSha256":hashlib.sha256((folder/"Raw"/f"{name}.png").read_bytes()).hexdigest(),"scope":"Perpendicular connected alpha>127 span at the same normalized knee-to-hip fraction. Isolated actual sourceowner02 skin and frozen target skin. Pixel edges sampled at1/8px via nearest lookup, therefore values are approximate raster widths. Proximal samples can include body-ownership boundary; no hidden muscle volume is measured.","records":records}
    (OUT/"closed-middle-upper-width-observations.json").write_text(json.dumps(report,indent=2))
    print(json.dumps(report,indent=2))


if __name__=="__main__":
    main()
