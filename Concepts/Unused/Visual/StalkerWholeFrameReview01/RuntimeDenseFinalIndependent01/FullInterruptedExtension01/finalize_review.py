"""Lock actual full-run interrupted animation coverage, including the independent split."""
from pathlib import Path
from datetime import datetime, timezone
from collections import Counter
import hashlib
import json
import numpy as np
from PIL import Image

HERE=Path(__file__).resolve().parent
PEER=HERE.parent.parent/"RuntimeDenseFinal01/FullInterruptionSecondHalf01/review-coverage.json"
PEER_SHA="5969f604af97a9011a1c093483e980df04ff08a95e550b012c5099f93c01567a"


def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    out=HERE/"review-coverage.json";d=json.loads(out.read_text(encoding="utf-8"));source=Path(d["sourceRoot"])
    assert sha(HERE/"page-manifest.json")==d["pageManifestSha256"]
    assert sha(source/"run.json")==d["runSha256"]
    assert sha(PEER)==PEER_SHA
    peer=json.loads(PEER.read_text(encoding="utf-8"))
    assert peer["actualPagesViewed"]==31 and peer["shownUniqueActualFrames"]==365
    assert peer["pageRangeInclusive"]==[32,62] and peer["pageManifestSha256"]==d["pageManifestSha256"]
    assert peer["allPageHashesUnchanged"] and peer["allSourcePngAndDecodedRgbHashesMatched"] and peer["allPayloadsByteExact"]
    assert not peer["findings"]
    peer_pages={p["page"]:p for p in peer["pages"]}
    assert set(peer_pages)==set(range(32,63))
    for p in d["pages"]:
        if p["type"]=="unique" and p["page"]>=32:
            pp=peer_pages[p["page"]]
            assert pp["sha256"]==p["sha256"] and pp["frames"]==p["frames"]
            p["actuallyViewedOriginal"]=True;p["reviewer"]="native_whole_audit"
    records={}
    for p in d["profiles"]:
        assert sha(source/p["key"]/"manifest.json")==p["manifestSha256"]
        assert [r["frame"] for r in p["records"]]==list(range(1319,1450))
        for r in p["records"]:
            identity=(r["key"],r["frame"]);assert identity not in records;records[identity]=r
            f=r["facts"]
            assert f["bodyEnabled"] and f["death"]==0 and f["scale"]==[1.0,1.0,1.0]
            assert f["bodyLocalPosition"]==[0.0,0.0,0.0] and f["bodyLocalScale"]==[1.0,1.0,1.0] and f["bodyLocalRotation"]==0
        for cut in p["firstCuts"]:
            images=[]
            for i in (cut["before"],cut["after"]):
                with Image.open(source/p["key"]/f"frame-{i:05d}.png") as im:images.append(np.asarray(im.convert("RGB"),dtype=np.int16))
            a,b=images;diff=np.abs(a-b)
            cut["changedRgbPixels"]=int(np.any(diff,axis=2).sum());cut["maximumChannelDifference"]=int(diff.max())
            cut["foregroundMaskChangedPixels"]=int(np.count_nonzero(np.any(a!=(0,255,0),axis=2)!=np.any(b!=(0,255,0),axis=2)))
        for bridge in p["bridges"]:
            rows=[records[(p["key"],i)] for i in bridge["frames"]]
            assert [int(r["facts"]["body"].split("-")[-1]) for r in rows]==list(range(14))
            assert all(r["facts"]["fold"]==0 for r in rows)
        assert p["firstPositiveFearFoldFrame"]==1431
    assert len(records)==1179 and len(d["exactRgbGroups"])==749
    checked=set()
    for digest,g in d["exactRgbGroups"].items():
        r=g["representative"]
        with Image.open(source/r["key"]/f"frame-{r['frame']:05d}.png") as im:expected=im.convert("RGB").tobytes()
        assert hashlib.sha256(expected).hexdigest()==digest
        for a in g["aliases"]:
            identity=(a["key"],a["frame"]);assert identity not in checked;record=records[identity]
            path=source/a["key"]/f"frame-{a['frame']:05d}.png";assert sha(path)==record["pngSha256"]
            with Image.open(path) as im:assert im.size==(640,640);actual=im.convert("RGB").tobytes()
            assert actual==expected and record["decodedRgbSha256"]==digest,identity
            checked.add(identity)
    assert checked==set(records)
    counts=Counter();unique=[];contexts=[]
    for p in d["pages"]:
        assert p["actuallyViewedOriginal"] is True and p["nativePayloadExact"] is True
        path=Path(p["path"]);assert sha(path)==p["sha256"]
        with Image.open(path) as im:assert im.size==(1392,1326);sheet=im.convert("RGB")
        for slot,f in enumerate(p["frames"]):
            r=records[(f["key"],f["frame"])];assert r["decodedRgbSha256"]==f["decodedRgbSha256"]
            with Image.open(source/f["key"]/f"frame-{f['frame']:05d}.png") as im:native=im.convert("RGB").crop((168,120,504,528))
            x,y=slot%4*348+6,slot//4*442+30
            assert sheet.crop((x,y,x+336,y+408)).tobytes()==native.tobytes()
            if p["type"]=="unique":unique.append(f["decodedRgbSha256"])
            else:contexts.append((f["profile"],f["frame"]))
        p["sha256UnchangedAfterView"]=True;p["payloadRecheckedAfterView"]=True;counts[p["type"]]+=1
    assert counts=={"unique":63,"temporal":16}
    assert len(unique)==749 and set(unique)==set(d["exactRgbGroups"])
    context=(1340,1341,1342,1347,1348,1349,1360,1361,1362,1407,1408,1409,1414,1415,1416,1427,1428,1429,1430,1431,1432)
    assert contexts==[(p,f) for p in range(9) for f in context]
    assert sha(source/"run.json")==d["runSha256"]
    d["status"]="PASS — actual current full-run scenarios27/28 all9, independent native visual and exact RGB proof"
    d["peerReview"]={"path":str(PEER),"sha256":PEER_SHA,"actuallyViewedUniquePages":[32,62],"uniqueFrameCount":365}
    d["visualReviewBatches"].append({"reviewer":"native_whole_audit","type":"unique","firstPage":32,"lastPage":62,"pages":31,"method":"Actually viewed original1x; immutable referenced proof","findings":"No actionable visual defect; phase-specific bridge, mouth retraction and fear folding coherent."})
    d["findings"]=["No actionable limb reset, mouth pop, skin seam, transparent joint core, clipped tip or body-origin jump observed in the current full-run frames.","All18 first cuts retain strike-10. All18 return bridges select 00..13 in order before current-phase walk; fear fold starts only at1431 after1429 endpoint."]
    d["completion"]={"sourceInstancesDirectByteCompared":1179,"uniqueFullRgbActuallyViewed":749,"exactDuplicateInstances":430,"uniqueNativePagesActuallyViewed":63,"temporalNativePagesActuallyViewed":16,"chronologicalContextInstances":189,"all1179SourcePngHashesUnchanged":True,"runAnd9ManifestHashesUnchanged":True,"all79PageHashesUnchanged":True,"all938NativeTilePayloadsByteExact":True,"focusedRunVisualInheritance":False,"approximateEquality":False,"sharedCoverageModified":False,"unityCalls":0,"productionEdits":0,"finalAuthoring":"N/A — review artifacts only","verifiedAtUtc":datetime.now(timezone.utc).isoformat()}
    out.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    files=("prepare_review.py","finalize_review.py","page-manifest.json","review-coverage.json","independent-art-review.md")
    hashes={name:sha(HERE/name) for name in files}
    (HERE/"completion-hashes.json").write_text(json.dumps({"status":d["status"],"sha256":hashes,"peerProofSha256":PEER_SHA},ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    print(json.dumps({"status":d["status"],"coverageSha256":hashes["review-coverage.json"],"sourceRunSha256":d["runSha256"],**d["completion"]},ensure_ascii=False),flush=True)


if __name__=="__main__":main()
