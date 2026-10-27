"""Freeze only current full-run interrupted attack frames, never borrow focused art PASS."""
from pathlib import Path
from collections import defaultdict
import hashlib
import json
import numpy as np
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
PROJECT = HERE.parents[5]
SHARED = HERE.parent.parent / "RuntimeDenseFinal01"
CROP = (168, 120, 504, 528)
SIZE = (1392, 1326)
CONTEXT = (1340,1341,1342,1347,1348,1349,1360,1361,1362,
           1407,1408,1409,1414,1415,1416,1427,1428,1429,1430,1431,1432)


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    provenance_path = SHARED / "review-coverage.json"
    provenance = json.loads(provenance_path.read_text(encoding="utf-8"))
    source = Path(provenance["sourceRoot"])
    source_run_sha = sha(source / "run.json")
    profiles, unique, groups = [], [], {}
    union = [640,640,0,0]
    for p, profile in enumerate(provenance["profiles"]):
        key = profile["key"]
        manifest_path = source / key / "manifest.json"
        assert sha(manifest_path) == profile["manifestSha256"]
        selected = [f for f in profile["frames"] if 1319 <= f["frame"] <= 1449]
        assert [f["frame"] for f in selected] == list(range(1319,1450))
        records = []
        for frame in selected:
            path = source / key / f"frame-{frame['frame']:05d}.png"
            assert sha(path) == frame["sha256"]
            with Image.open(path) as im:
                assert im.size == (640,640)
                rgb = im.convert("RGB")
            raw = rgb.tobytes()
            digest = hashlib.sha256(raw).hexdigest()
            assert digest == frame["decodedRgbSha256"]
            pixels = np.asarray(rgb)
            ys,xs = np.nonzero(np.any(pixels != (0,255,0),axis=2))
            bounds = [int(xs.min()),int(ys.min()),int(xs.max())+1,int(ys.max())+1]
            assert bounds[0]>=168 and bounds[1]>=120 and bounds[2]<=504 and bounds[3]<=528
            union = [min(union[0],bounds[0]),min(union[1],bounds[1]),max(union[2],bounds[2]),max(union[3],bounds[3])]
            record = {"profile":p,"key":key,"frame":frame["frame"],"pngSha256":frame["sha256"],"decodedRgbSha256":digest,"foregroundBounds":bounds,"facts":frame["facts"]}
            records.append(record)
            if digest not in groups:
                groups[digest] = {"representative":{"key":key,"frame":frame["frame"]},"aliases":[]}
                unique.append(record)
            groups[digest]["aliases"].append({"key":key,"frame":frame["frame"]})
        cuts = []
        for before,after in ((1340,1341),(1407,1408)):
            a,b = records[before-1319],records[after-1319]
            assert a["facts"]["body"]==b["facts"]["body"]=="strike-10"
            assert a["facts"]["fold"]==b["facts"]["fold"]==0
            cuts.append({"before":before,"after":after,"sameSelectedStrike10":True,"sameFullRgb":a["decodedRgbSha256"]==b["decodedRgbSha256"]})
        bridges = []
        for first,last,endpoint in ((1348,1361,1362),(1415,1428,1429)):
            rows=[r for r in records if first<=r["frame"]<=last]
            assert [int(r["facts"]["body"].split("-")[-1]) for r in rows]==list(range(14))
            assert all(r["facts"]["fold"]==0 for r in rows)
            end=records[endpoint-1319]
            assert end["facts"]["body"].startswith("body-0-") and end["facts"]["fold"]==0
            bridges.append({"frames":[r["frame"] for r in rows],"spriteSequence":[r["facts"]["body"] for r in rows],"endpointFrame":endpoint,"endpointSprite":end["facts"]["body"]})
        assert records[1430-1319]["facts"]["fold"]==0 and records[1431-1319]["facts"]["fold"]>0
        profiles.append({"profile":p,"key":key,"manifestSha256":profile["manifestSha256"],"firstCuts":cuts,"bridges":bridges,"firstPositiveFearFoldFrame":1431,"records":records})
    assert len(unique)==749 and sum(len(p["records"]) for p in profiles)==1179
    context=[r for p in profiles for r in p["records"] if r["frame"] in CONTEXT]
    pages=[]
    for kind,records in (("unique",unique),("temporal",context)):
        (HERE/kind).mkdir(exist_ok=True)
        for first in range(0,len(records),12):
            subset=records[first:first+12]
            sheet=Image.new("RGB",SIZE,(20,24,28));draw=ImageDraw.Draw(sheet)
            for slot,r in enumerate(subset):
                with Image.open(source/r["key"]/f"frame-{r['frame']:05d}.png") as im:native=im.convert("RGB").crop(CROP)
                x,y=slot%4*348+6,slot//4*442+30
                sheet.paste(native,(x,y));f=r["facts"]
                draw.text((x,y-29),f"P{r['profile']} F{r['frame']} L{f['local']:02d} {f['state']}",fill="white")
                draw.text((x,y-15),f"{f['body']} fold{f['fold']:.3f} eye{f['closure']:.3f}",fill="white")
                assert sheet.crop((x,y,x+336,y+408)).tobytes()==native.tobytes()
            path=HERE/kind/f"page-{first//12:03d}.png";sheet.save(path)
            pages.append({"type":kind,"page":first//12,"path":str(path),"sha256":sha(path),"sizePixels":list(SIZE),"frames":[{"key":r["key"],"profile":r["profile"],"frame":r["frame"],"decodedRgbSha256":r["decodedRgbSha256"]} for r in subset],"nativePayloadExact":True,"actuallyViewedOriginal":False})
    assert sha(source/"run.json")==source_run_sha
    page_manifest={"status":"IMMUTABLE_PREVIEW_PAGES_NOT_YET_VISUALLY_ACCEPTED","sourceRoot":str(source),"runSha256":source_run_sha,"fixedCrop":list(CROP),"foregroundUnion":union,"pages":pages}
    manifest_path=HERE/"page-manifest.json";manifest_path.write_text(json.dumps(page_manifest,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    coverage={"status":"PREPARED_NOT_VIEWED","sourceRoot":str(source),"runSha256":source_run_sha,"sourceInstances":1179,"uniqueFullRgb":749,"exactDuplicateInstances":430,"pageManifestSha256":sha(manifest_path),"fixedCrop":list(CROP),"foregroundUnion":union,"resampling":False,"profiles":profiles,"exactRgbGroups":groups,"pages":pages,"visualReviewBatches":[],"limits":"Only current stalker-dense-final scenarios27/28. No focused run visual inheritance, no approximate equality, no AI claim."}
    (HERE/"review-coverage.json").write_text(json.dumps(coverage,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    print(json.dumps({"instances":1179,"uniqueRgb":749,"uniquePages":63,"contextPages":16,"contextInstances":len(context),"manifestSha256":sha(manifest_path),"foregroundUnion":union}),flush=True)


if __name__=="__main__":main()
