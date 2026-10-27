"""Independent double math for Final03 body + attack; audit writes only."""
from pathlib import Path
import json
import hashlib
import sys
import numpy as np
from study_stride_regions import unit,slerp,cross,intersections

OUT=Path(__file__).resolve().parent
PROJECT=OUT.parents[3]
FAMILY=PROJECT/"Concepts/Used/Stalker/Postures05"
IDS=["LF","RF","LM","RM","LR","RR"]


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def smooth(t):
    t=np.clip(t,0,1)
    return t*t*(3-2*t)


def main():
    staged_support="--staged-support04" in sys.argv
    source=json.loads((PROJECT/"Concepts/Used/Stalker/Locomotion02/anatomy.json").read_text())
    lengths=np.array([[x["upperLength"],x["lowerLength"]] for x in source["limbs"]])
    basehips=np.array([(np.array(l["hip"])-source["pivot"])*[1,-1]/224 for l in source["limbs"]])
    mounts=basehips*np.array([[.4,.4]]*4+[[.72,.72]]*2)
    source_cross=np.array([cross((np.array(l["tip"])-l["hip"])*[1,-1]/224,(np.array(l["knee"])-l["hip"])*[1,-1]/224) for l in source["limbs"]])
    bend=np.sign(source_cross)
    support=np.array([.65,.65,.35,.35,.56,.56])
    offsets=np.array([0,.5,.85,.35,0,.5])
    ext=np.array([[.04,.48],[-.04,.48],[-.56,-.30],[.56,-.30],[.11,-.48],[-.11,-.48]])
    guard=np.array([[.035,.30],[-.035,.30],[-.50,-.30],[.50,-.30],[.09,-.30],[-.09,-.30]])
    closed_knee=np.array([[-.34,.20],[.34,.20],[-.38,.06],[.38,.06],[-.34,-.154],[.34,-.154]])
    closed_toe=np.array([[-.068,.411],[.068,.411],[-.13,-.10],[.13,-.10],[-.06,-.349],[.06,-.349]])
    cu=closed_knee-mounts
    cl=closed_toe-closed_knee
    closed_upper=np.column_stack([cu,np.sqrt(lengths[:,0]**2-(cu*cu).sum(axis=1))])/lengths[:,0,None]
    closed_lower=np.column_stack([cl,-np.sqrt(lengths[:,1]**2-(cl*cl).sum(axis=1))])/lengths[:,1,None]
    clamp_records=[]

    def knee(h,f,height,tag):
        f=f.copy()
        up=np.sqrt(np.maximum(0,lengths[:,0]**2-(h[:,2]-height)**2))
        lo=np.sqrt(np.maximum(0,lengths[:,1]**2-(f[:,2]-height)**2))
        delta=f[:,:2]-h[:,:2]
        dist=np.linalg.norm(delta,axis=1)
        clipped=np.clip(dist,abs(up-lo)+.0001,up+lo-.0001)
        error=abs(dist-clipped)
        for leg in np.flatnonzero(error>1e-9):
            clamp_records.append({"pose":tag,"leg":IDS[leg],"clampDelta":float(error[leg])})
        axis=delta/dist[:,None]
        f[:,:2]=h[:,:2]+axis*clipped[:,None]
        along=(up*up-lo*lo+clipped*clipped)/(2*clipped)
        radius=np.sqrt(np.maximum(0,up*up-along*along))
        k=np.column_stack([h[:,:2]+axis*along[:,None]+np.column_stack([-axis[:,1],axis[:,0]])*(radius*bend)[:,None],height])
        return k,f

    def body_pose(row,phase):
        fold=row/8
        walking=min(fold,.5)
        tuck=max(0,min(1,(fold-.5)/.5))
        stride=(1-.2*walking/.5)*(1-tuck)
        q=(phase/48+offsets)%1
        swing=np.clip((q-support)/(1-support),0,1)
        amplitude=.5*1.25*support*stride
        off=np.where(q<support,amplitude*(1-2*q/support),amplitude*(-1+2*smooth(swing)))
        lift=np.where(q<support,0,np.sin(np.pi*swing)*.075*(1-walking)*(1-tuck))
        h=np.column_stack([mounts,np.full(6,.13+.07*fold)])
        f=np.column_stack([mounts+ext*(1-walking)+guard*walking,lift])
        f[:,1]+=off
        k,f=knee(h,f,(h[:,2]+f[:,2])*.5,f"body-{row}-{phase}")
        tuck_per_leg=np.full((6,1),tuck)
        tuck_per_leg[2:4]=tuck*(2-tuck)
        k_new=h+slerp(unit(k-h),closed_upper,tuck_per_leg)*lengths[:,0,None]
        f_new=k_new+slerp(unit(f-k),closed_lower,tuck_per_leg)*lengths[:,1,None]
        return np.array([h,k_new,f_new])

    expected_body={(r,p):body_pose(r,p) for r in range(9) for p in range(48)}
    def front(striking):
        h,k,f=expected_body[(0,0)].copy()
        f[2:,2]=0
        grounded,_=knee(h,f,h[:,2]*.5,"attack-front-nonattacking")
        k[2:]=grounded[2:]
        for leg in range(2):
            side=-1 if leg==0 else 1
            upper=np.array([side*.24,.38,-.04] if striking else [side*.30,.34,.04])
            lower=np.array([-side*.18,.39,-.06] if striking else [-side*.18,.18,-.02])
            k[leg]=h[leg]+upper/np.linalg.norm(upper)*lengths[leg,0]
            f[leg]=k[leg]+lower/np.linalg.norm(lower)*lengths[leg,1]
        return np.array([h,k,f])
    chamber,extended=front(False),front(True)
    def blend(a,b,t,tag):
        if t<=0:
            return a.copy()
        if t>=1:
            return b.copy()
        h=a[0]+(b[0]-a[0])*t
        f=a[2]+(b[2]-a[2])*t
        kh=a[1,:,2]+(b[1,:,2]-a[1,:,2])*t
        k,f=knee(h,f,kh,tag)
        return np.array([h,k,f])
    def settled(walking):
        h,k,f=walking.copy()
        f[2:,2]=0
        grounded,adjusted=knee(h,f,h[:,2]*.5,"support-settle-target04")
        k[2:]=grounded[2:]
        f[2:]=adjusted[2:]
        return np.array([h,k,f])
    actual_body=json.loads((FAMILY/"poses.json").read_text())
    actual_attack=json.loads((FAMILY/"attack-poses.json").read_text())
    def xyz(p):
        return np.array([p[key] for key in ("hips","knees","feet")])
    body_diff=[{"row":p["row"],"phase":p["phase"],"maxError":float(abs(xyz(p)-expected_body[(p["row"],p["phase"])]).max())} for p in actual_body]
    attack_diff=[]
    for p in actual_attack:
        group,phase,sample=p["group"],p["phase"],p["sample"]
        if group=="entry":
            walking=expected_body[(0,phase)]
            progress=sample/7
            if staged_support:
                support_target=settled(walking)
                e=blend(walking,support_target,smooth(progress/.3),p["name"]) if progress<.3 else blend(support_target,chamber,smooth((progress-.3)/.7),p["name"])
            else:
                e=blend(walking,chamber,smooth(progress),p["name"])
        elif group=="return":
            walking=expected_body[(0,phase)]
            progress=sample/7
            if staged_support:
                support_target=settled(walking)
                e=blend(chamber,support_target,smooth(progress/.7),p["name"]) if progress<.7 else blend(support_target,walking,smooth((progress-.7)/.3),p["name"])
            else:
                e=blend(chamber,walking,smooth(progress),p["name"])
        else:
            progress=sample/23
            reach=smooth(progress/.4) if progress<=.4 else 1-smooth((progress-.4)/.6)
            e=blend(chamber,extended,reach,p["name"])
        attack_diff.append({"name":p["name"],"maxError":float(abs(xyz(p)-e).max())})
    all_body=np.array([xyz(p) for p in actual_body]).reshape(9,48,3,6,3)
    h,k,f=all_body[:,:,0],all_body[:,:,1],all_body[:,:,2]
    crossing=[]
    for a in range(6):
        for b in range(a+1,6):
            hits,points=intersections(k[...,a,:2],f[...,a,:2],k[...,b,:2],f[...,b,:2])
            if hits.any():
                row,phase=np.nonzero(hits)
                crossing.append({"pair":[IDS[a],IDS[b]],"count":int(hits.sum()),"firstRowPhase":[int(row[0]),int(phase[0])]})
    stance=[]
    keyed={(p["row"],p["phase"]):xyz(p) for p in actual_body}
    for row in range(5):
        delta=np.array([0,1.25*(1-.2*(row/8)/.5)/48])
        for phase in range(48):
            q=(phase/48+offsets)%1
            qn=((phase+1)/48+offsets)%1
            same=(q<support)&(qn<support)&(abs(qn-q-1/48)<1e-8)
            residual=np.linalg.norm(keyed[(row,(phase+1)%48)][2,:,:2]-keyed[(row,phase)][2,:,:2]+delta,axis=1)
            stance.extend(float(v) for v in residual[same])
    attack_array=np.array([xyz(p) for p in actual_attack])
    attack_error=np.maximum(abs(np.linalg.norm(attack_array[:,1]-attack_array[:,0],axis=-1)-lengths[:,0]),abs(np.linalg.norm(attack_array[:,2]-attack_array[:,1],axis=-1)-lengths[:,1]))
    body_error=np.maximum(abs(np.linalg.norm(k-h,axis=-1)-lengths[:,0]),abs(np.linalg.norm(f-k,axis=-1)-lengths[:,1]))
    ak={(p["group"],p["phase"],p["sample"]):xyz(p) for p in actual_attack}
    entry_edges=max(float(abs(ak[(g,phase,sample)]-keyed[(0,phase)]).max()) for phase in range(48) for g,sample in (("entry",0),("return",7)))
    chamber_edges=max(float(abs(ak[(g,phase,sample)]-ak[("strike",-1,0)]).max()) for phase in range(48) for g,sample in (("entry",7),("return",0)))
    report={"status":"FORMAL OFFLINE COORDINATE GATE ONLY", "scope":"All432body +792attack independently recomputed from source/configured constants. No rendered skin, physical support or artistic tempo approval.","hashes":{name:digest(FAMILY/name) for name in ("poses.json","attack-poses.json","bones.json")},"metadata":json.loads((FAMILY/"bones.json").read_text()),"bodyCount":len(actual_body),"attackCount":len(actual_attack),"maxBodyCoordinateError":max(p["maxError"] for p in body_diff),"worstBody":max(body_diff,key=lambda p:p["maxError"]),"maxAttackCoordinateError":max(p["maxError"] for p in attack_diff),"worstAttack":max(attack_diff,key=lambda p:p["maxError"]),"maxBodyLengthError":float(body_error.max()),"maxAttackLengthError":float(attack_error.max()),"minBodyPointZ":float(all_body[...,2].min()),"minAttackPointZ":float(attack_array[...,2].min()),"minBodyWindingProduct":float((cross((f-h)[...,:2],(k-h)[...,:2])*source_cross).min()),"minForeMiddleKneeYGap":float((k[...,:2,1]-k[...,2:4,1]).min()),"minMiddleHindKneeYGap":float((k[...,2:4,1]-k[...,4:6,1]).min()),"minDistalProjectedLengthRatio":float((np.linalg.norm((f-k)[...,:2],axis=-1)/lengths[:,1]).min()),"minMaxWalkingFloorContacts":[int((abs(f[:5,...,2])<1e-6).sum(axis=-1).min()),int((abs(f[:5,...,2])<1e-6).sum(axis=-1).max())],"stancePairsChecked":len(stance),"maximumStanceWorldTranslationResidual":max(stance),"distalDistalCenterlineIntersections":crossing,"reachClampRecords":clamp_records,"maximumEntryReturnWalkingEdgeError":entry_edges,"maximumAllPhaseChamberEdgeError":chamber_edges,"strikeNonattackingFeetMaximumAbsoluteZ":float(max(abs(xyz(p)[2,2:,2]).max() for p in actual_attack if p["group"]=="strike")),"bodyCoordinateErrors":body_diff,"attackCoordinateErrors":attack_diff,"limits":["VirtualZ and true3D lengths are explicit authoring constructs, not measured from source PNG.","Nonnegative centerline points do not prove skin-floor clearance or grounded closed-body support.","Centerline gaps do not establish no overlap between actual thick RGBA surfaces.","Discrete432+792 agreement and support translation are not native motion/art cadence acceptance."]}
    report["pass"]=report["maxBodyCoordinateError"]<1e-6 and report["maxAttackCoordinateError"]<1e-6 and report["minBodyPointZ"]>=-1e-7 and report["minAttackPointZ"]>=-1e-7 and report["minBodyWindingProduct"]>0 and report["minForeMiddleKneeYGap"]>.1 and report["minMiddleHindKneeYGap"]>.1 and not crossing and not clamp_records and report["minMaxWalkingFloorContacts"][0]>=3
    attack_h,attack_k,attack_f=attack_array[:,0],attack_array[:,1],attack_array[:,2]
    report["minAttackSourceWindingProduct"]=float((cross((attack_f-attack_h)[...,:2],(attack_k-attack_h)[...,:2])*source_cross).min())
    report["minimumAttackFloorContacts"]=int((abs(attack_f[...,2])<1e-6).sum(axis=-1).min())
    report["attackFramesAtMinimumContacts"]=[p["name"] for p,count in zip(actual_attack,(abs(attack_f[...,2])<1e-6).sum(axis=-1)) if count==report["minimumAttackFloorContacts"]]
    report["allCoordinatesFinite"]=bool(np.isfinite(all_body).all() and np.isfinite(attack_array).all())
    report["pass"]=report["pass"] and report["minAttackSourceWindingProduct"]>0 and report["allCoordinatesFinite"]
    report["stagedSupport04"]=staged_support
    if staged_support:
        report["pass"]=report["pass"] and report["minimumAttackFloorContacts"]>=3
    (OUT/("Final04-independent-body-attack-coordinate-gate.json" if staged_support else "Final03-independent-body-attack-coordinate-gate.json")).write_text(json.dumps(report,indent=2))
    summary={key:value for key,value in report.items() if key not in ("bodyCoordinateErrors","attackCoordinateErrors","attackFramesAtMinimumContacts")}
    print(json.dumps(summary,indent=2))


if __name__=="__main__":
    main()
