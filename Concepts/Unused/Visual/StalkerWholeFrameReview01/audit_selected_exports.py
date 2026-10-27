"""Read-only checks on latest C# body/attack coordinates against independent math."""
from pathlib import Path
import json
import hashlib
import numpy as np

OUT=Path(__file__).resolve().parent
PROJECT=OUT.parents[3]
FAMILY=PROJECT/"Concepts/Used/Stalker/Postures05"
IDS=["LF","RF","LM","RM","LR","RR"]


def xyz(p):
    return np.array([p[key] for key in ("hips","knees","feet")],float)


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    body=json.loads((FAMILY/"poses.json").read_text())
    expected=json.loads((OUT/"independent-selected-candidate-coordinates.json").read_text())
    attack=json.loads((FAMILY/"attack-poses.json").read_text())
    source=json.loads((PROJECT/"Concepts/Used/Stalker/Locomotion02/anatomy.json").read_text())
    lengths=np.array([[l["upperLength"],l["lowerLength"]] for l in source["limbs"]])
    keys={(p["row"],p["phase"]):p for p in body}
    differences=[]
    for e in expected:
        p=keys[(e["row"],e["phase"])]
        differences.append({"row":e["row"],"phase":e["phase"],"maxAbsoluteCoordinateError":float(abs(xyz(p)-xyz(e)).max())})
    stance=[]
    support=np.array([.65,.65,.35,.35,.65,.65])
    offsets=np.array([0,.5,.85,.35,0,.5])
    for row in range(5):
        stride=1-.2*(row/8)/.5
        translation=np.array([0,1.25*stride/48])
        for phase in range(48):
            q=(phase/48+offsets)%1
            qnext=((phase+1)/48+offsets)%1
            same_stance=(q<support)&(qnext<support)&(abs(qnext-q-1/48)<1e-8)
            a=np.array(keys[(row,phase)]["feet"])[:,:2]
            b=np.array(keys[(row,(phase+1)%48)]["feet"])[:,:2]
            residual=np.linalg.norm(b-a+translation,axis=1)
            for leg in np.nonzero(same_stance)[0]:
                stance.append({"row":row,"phase":phase,"leg":IDS[leg],"worldSupportTranslationResidual":float(residual[leg])})
    rows=[]
    for p in attack:
        h,k,f=xyz(p)
        le=max(abs(np.linalg.norm(k-h,axis=1)-lengths[:,0]).max(),abs(np.linalg.norm(f-k,axis=1)-lengths[:,1]).max())
        rows.append({"name":p["name"],"group":p["group"],"phase":p["phase"],"sample":p["sample"],"minimumPointZ":float(min(h[:,2].min(),k[:,2].min(),f[:,2].min())),"minimumFootZ":float(f[:,2].min()),"maxLengthError":float(le),"floorContacts":int((abs(f[:,2])<1e-6).sum()),"nonattackingFootMaxZ":float(abs(f[2:,2]).max())})
    atk={(p["group"],p["phase"],p["sample"]):p for p in attack}
    edges=[]
    for phase in range(48):
        walking=keys[(0,phase)]
        for group,sample in (("entry",0),("return",7)):
            edges.append({"phase":phase,"group":group,"sample":sample,"maxAbsoluteCoordinateError":float(abs(xyz(atk[(group,phase,sample)])-xyz(walking)).max())})
    strike=[p for p in attack if p["group"]=="strike"]
    chamber=strike[0]
    chamber_edges=[]
    for phase in range(48):
        for group,sample in (("entry",7),("return",0)):
            chamber_edges.append(float(abs(xyz(atk[(group,phase,sample)])-xyz(chamber)).max()))
    stride=json.loads((OUT/"stride-selected-three-mounts-study.json").read_text())["root_selected_Mtoe_minus_point10_fore_mount_y_factors_three_only"][0]
    report={"scope":"Offline coordinate and virtual floor gate; no art, physical resting body, Unity runtime or motion-tempo approval", "sourceHashes":{name:digest(FAMILY/name) for name in ("poses.json","attack-poses.json","bones.json","attack-pose-validation.json")},"metadata":json.loads((FAMILY/"bones.json").read_text()),"bodyCount":len(body),"bodyMatchesIndependentMathWithin1e-6":max(x["maxAbsoluteCoordinateError"] for x in differences)<1e-6,"maximumBodyCoordinateErrorAgainstIndependentDoubleMath":max(x["maxAbsoluteCoordinateError"] for x in differences),"worstBodyCoordinateComparison":max(differences,key=lambda x:x["maxAbsoluteCoordinateError"]),"independentCandidateSummary":stride,"stancePairsChecked":len(stance),"maximumStanceWorldTranslationResidual":max(x["worldSupportTranslationResidual"] for x in stance),"worstStanceWorldTranslationResidual":max(stance,key=lambda x:x["worldSupportTranslationResidual"]),"attackCount":len(attack),"attackGroups":{g:sum(p["group"]==g for p in attack) for g in sorted(set(p["group"] for p in attack))},"minimumAttackPointZ":min(x["minimumPointZ"] for x in rows),"maximumAttackBoneLengthError":max(x["maxLengthError"] for x in rows),"minimumAttackFloorContacts":min(x["floorContacts"] for x in rows),"strikeNonattackingFeetMaximumAbsoluteZ":max(x["nonattackingFootMaxZ"] for x in rows if x["group"]=="strike"),"strikeFloorContactCounts":sorted(set(x["floorContacts"] for x in rows if x["group"]=="strike")),"bodyAttackEntryReturnMaximumEdgeError":max(x["maxAbsoluteCoordinateError"] for x in edges),"allPhaseChamberMaximumEdgeError":max(chamber_edges),"strikeFirstLastChamberError":float(abs(xyz(strike[0])-xyz(strike[-1])).max()),"floorOrLengthFailures":[x for x in rows if x["minimumPointZ"] < -1e-7 or x["maxLengthError"]>1e-5],"limits":["FootZ=0 is an authored contact, not measured support force or skin-floor clearance.","The432grid agreement checks exactly9folds x48phases; no continuous interpolation or final skin set is certified.","Gait frequency is v/(1.25*strideScale*actorScale); artistic calmness still requires native video.","No source PNG validates virtual Z heights or true3D bone lengths."]}
    (OUT/"selected-cadence-export-independent-gate.json").write_text(json.dumps(report,indent=2))
    compact={key:report[key] for key in ("bodyCount","bodyMatchesIndependentMathWithin1e-6","maximumBodyCoordinateErrorAgainstIndependentDoubleMath","attackCount","attackGroups","minimumAttackPointZ","maximumAttackBoneLengthError","minimumAttackFloorContacts","strikeNonattackingFeetMaximumAbsoluteZ","strikeFloorContactCounts","bodyAttackEntryReturnMaximumEdgeError","allPhaseChamberMaximumEdgeError","strikeFirstLastChamberError","floorOrLengthFailures")}
    print(json.dumps(compact,indent=2))


if __name__=="__main__":
    main()
