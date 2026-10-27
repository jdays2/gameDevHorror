"""Prepare immutable native current Sleeper27/29/20 pages; preserve all camera RGB."""
from pathlib import Path
import hashlib
import json
import numpy as np
from PIL import Image,ImageDraw

HERE=Path(__file__).resolve().parent
PROJECT=HERE.parents[4]
SOURCE=PROJECT/"Concepts/Used/Stalker/Postures05/Scenarios/sleeper-final"
CROP=(104,96,568,536)
SIZE=(1428,1422)
CONTEXT=(1153,1154,1155,1156,1160,1161,1162,1173,1174,1175,1176,1177,
         1227,1228,1229,1230,1231,1232,1233,1234,1235,1236,1237,
         1274,1275,1281,1282,1283,1284,1285,1295,1364)


def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    run_path=SOURCE/"run.json";run_sha=sha(run_path);run=json.loads(run_path.read_text())
    assert run["completedAll"] and len(run["completed"])==3 and not run["captureErrors"]
    assert all(p["frames"]==1365 and p["scenarios"]==22 for p in run["completed"])
    profiles=[];unique=[];groups={};union=[640,640,0,0]
    for p,row in enumerate(run["completed"]):
        key=row["key"];mp=SOURCE/key/"manifest.json";msha=sha(mp);m=json.loads(mp.read_text())
        assert len(m["frames"])==1365
        records=[]
        for f in m["frames"]:
            if not f["scenario"].startswith(("27-","29-","20-")):continue
            i=f["frame"];path=SOURCE/key/f"frame-{i:05d}.png";psha=sha(path)
            with Image.open(path) as im:assert im.size==(640,640);rgb=im.convert("RGB")
            raw=rgb.tobytes();digest=hashlib.sha256(raw).hexdigest();a=np.asarray(rgb)
            y,x=np.nonzero(np.any(a!=(0,255,0),axis=2));b=[int(x.min()),int(y.min()),int(x.max())+1,int(y.max())+1]
            assert b[0]>=104 and b[1]>=96 and b[2]<=568 and b[3]<=536,(key,i,b)
            union=[min(union[0],b[0]),min(union[1],b[1]),max(union[2],b[2]),max(union[3],b[3])]
            r={"profile":p,"key":key,"frame":i,"pngSha256":psha,"decodedRgbSha256":digest,"foregroundBounds":b,"facts":f};records.append(r)
            if digest not in groups:groups[digest]={"representative":{"key":key,"frame":i},"aliases":[]};unique.append(r)
            groups[digest]["aliases"].append({"key":key,"frame":i})
        assert [r["frame"] for r in records]==list(range(1114,1365))
        assert sha(mp)==msha
        profiles.append({"profile":p,"key":key,"manifestSha256":msha,"sourceVariantConfiguration":m["sourceVariantConfiguration"],"sourceNote":m["note"],"attackDuration":m["AttackDuration"],"strikeAt":m["StrikeAt"],"spans":[s for s in m["spans"] if s["scenario"].startswith(("27-","29-","20-"))],"records":records})
    assert len(unique)==140
    context=[r for p in profiles for r in p["records"] if r["frame"] in CONTEXT]
    pages=[]
    for kind,records in (("unique",unique),("temporal",context)):
        (HERE/kind).mkdir(exist_ok=True)
        for first in range(0,len(records),9):
            subset=records[first:first+9];sheet=Image.new("RGB",SIZE,(20,24,28));draw=ImageDraw.Draw(sheet)
            for slot,r in enumerate(subset):
                with Image.open(SOURCE/r["key"]/f"frame-{r['frame']:05d}.png") as im:native=im.convert("RGB").crop(CROP)
                x,y=slot%3*476+6,slot//3*474+28;sheet.paste(native,(x,y));f=r["facts"]
                draw.text((x,y-27),f"P{r['profile']} F{r['frame']} L{f['local']:02d} {f['state']}",fill="white")
                draw.text((x,y-14),f"{f['body']} fold{f['fold']:.3f} eye{f['closure']:.3f} visible{int(f['bodyEnabled'])}",fill="white")
                assert sheet.crop((x,y,x+464,y+440)).tobytes()==native.tobytes()
            path=HERE/kind/f"page-{first//9:03d}.png";sheet.save(path)
            pages.append({"type":kind,"page":first//9,"path":str(path),"sha256":sha(path),"sizePixels":list(SIZE),"nativePayloadExact":True,"actuallyViewedOriginal":False,"frames":[{"key":r["key"],"profile":r["profile"],"frame":r["frame"],"decodedRgbSha256":r["decodedRgbSha256"]} for r in subset]})
    assert sha(run_path)==run_sha
    immutable={"sourceRoot":str(SOURCE),"sourceRunSha256":run_sha,"crop":list(CROP),"foregroundUnion":union,"nativeScale":1,"pages":pages}
    ip=HERE/"page-manifest.json";ip.write_text(json.dumps(immutable,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    coverage={"status":"PREPARED_NOT_VIEWED","sourceRoot":str(SOURCE),"sourceRunSha256":run_sha,"pageManifestSha256":sha(ip),"sourceInstances":753,"uniqueFullRgb":140,"exactDuplicateInstances":613,"profiles":profiles,"exactRgbGroups":groups,"pages":pages,"fixedCrop":list(CROP),"foregroundUnion":union,"resampling":False,"manualReviewBatches":[],"limits":"Current full Sleeper27/29/20 only, no focused visual inheritance or approximate RGB equality; presentation fixture scope."}
    (HERE/"review-coverage.json").write_text(json.dumps(coverage,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    print(json.dumps({"sourceInstances":753,"uniqueRGB":140,"uniquePages":16,"temporalPages":11,"contextInstances":len(context),"sourceRunSha256":run_sha,"pageManifestSha256":sha(ip),"foregroundUnion":union}),flush=True)


if __name__=="__main__":main()
