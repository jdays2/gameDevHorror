"""Hypothetical offline gait math only: no canonical pose/C#/painter writes."""
from pathlib import Path
import json
import math
import sys
import numpy as np
from PIL import Image

OUT=Path(__file__).resolve().parent
PROJECT=OUT.parents[3]
SOURCE=PROJECT/"Concepts/Used/Stalker/Locomotion02/anatomy.json"
IDS=["LF","RF","LM","RM","LR","RR"]


def cross(a,b):
    return a[...,0]*b[...,1]-a[...,1]*b[...,0]


def unit(a):
    return a/np.linalg.norm(a,axis=-1,keepdims=True)


def slerp(a,b,t):
    cosine=np.clip((a*b).sum(axis=-1,keepdims=True),-1,1)
    theta=np.arccos(cosine)
    sine=np.sin(theta)
    safe=np.where(np.abs(sine)<1e-8,1,sine)
    result=np.sin((1-t)*theta)/safe*a+np.sin(t*theta)/safe*b
    return unit(np.where(np.abs(sine)<1e-8,(1-t)*a+t*b,result))


def intersections(a,b,c,d):
    u,v=b-a,d-c
    determinant=cross(u,v)
    safe=np.where(abs(determinant)<1e-10,1,determinant)
    x,y=cross(c-a,v)/safe,cross(c-a,u)/safe
    inside=(abs(determinant)>1e-10)&(x>1e-6)&(x<1-1e-6)&(y>1e-6)&(y<1-1e-6)
    point=a+x[...,None]*u
    return inside,point


def main():
    source=json.loads(SOURCE.read_text())
    basehips=np.array([(np.asarray(x["hip"])-source["pivot"])*[1,-1]/224 for x in source["limbs"]])
    factors=np.array([[.4,.65],[.4,.65],[.4,.4],[.4,.4],[.72,.72],[.72,.72]])
    mount=basehips*factors
    lengths=np.array([[x["upperLength"],x["lowerLength"]] for x in source["limbs"]])
    sourcecross=np.array([cross((np.asarray(x["tip"])-x["hip"])*[1,-1]/224,(np.asarray(x["knee"])-x["hip"])*[1,-1]/224) for x in source["limbs"]])
    fold=np.arange(9)[:,None,None]/8
    walking=np.minimum(fold,.5)
    tuck=np.clip((fold-.5)/.5,0,1)
    stride=(1-.2*walking/.5)*(1-tuck)
    phases=np.arange(48)[None,:,None]/48
    maskpath=PROJECT/"Concepts/Used/Stalker/Postures05/CandidateAudit/SourceOwnershipStudy01/body-source-ownership-mask.png"
    bodymask=np.asarray(Image.open(maskpath).convert("L"))>128
    def outside_body(points):
        xy=np.rint(points*np.array([224,-224])+np.array([187,200])).astype(int)
        x,y=xy[...,0],xy[...,1]
        valid=(x>=0)&(x<bodymask.shape[1])&(y>=0)&(y<bodymask.shape[0])
        covered=np.zeros(x.shape,dtype=bool)
        covered[valid]=bodymask[y[valid],x[valid]]
        return ~covered
    def simulate(cycle,mid_support,mid_phase,mid_ext,mid_guard,mid_toe_y=-.12,fh_support=.56,mid_knee_y=.02,fh_lane_y=.52,fore_factor_y=.65):
        mount=basehips*factors
        mount[:2,1]=basehips[:2,1]*fore_factor_y
        support=np.array([fh_support,fh_support,mid_support,mid_support,fh_support,fh_support])
        phaseoffset=np.array([0,.5,.5+mid_phase,mid_phase,0,.5])
        local=(phases+phaseoffset)%1
        amplitude=.5*cycle*support*stride
        swing=np.clip((local-support)/(1-support),0,1)
        smooth=swing*swing*(3-2*swing)
        offset=np.where(local<support,amplitude*(1-2*local/support),amplitude*(-1+2*smooth))
        lift=np.where(local<support,0,np.sin(swing*np.pi)*.075*(1-walking)*(1-tuck))
        ext=np.array([[.04,fh_lane_y],[-.04,fh_lane_y],[-mid_ext[0],mid_ext[1]],[mid_ext[0],mid_ext[1]],[.11,-fh_lane_y],[-.11,-fh_lane_y]])
        guard=np.array([[.035,.30],[-.035,.30],[-mid_guard[0],mid_guard[1]],[mid_guard[0],mid_guard[1]],[.09,-.30],[-.09,-.30]])
        h=np.broadcast_to(np.concatenate([mount,np.zeros((6,1))],axis=-1),(9,48,6,3)).copy()
        h[...,2]=np.broadcast_to(.13+.07*fold,(9,48,6))
        f=np.zeros_like(h)
        f[...,:2]=mount+ext*(1-walking[...,None])+guard*walking[...,None]
        f[...,1]+=offset
        f[...,2]=lift
        kz=(h[...,2]+f[...,2])*.5
        up=np.sqrt(lengths[:,0]**2-(h[...,2]-kz)**2)
        low=np.sqrt(lengths[:,1]**2-(f[...,2]-kz)**2)
        delta=f[...,:2]-h[...,:2]
        distance=np.linalg.norm(delta,axis=-1)
        clamp=np.clip(distance,abs(up-low)+.0001,up+low-.0001)
        clamp_error=abs(distance-clamp)
        axis=delta/distance[...,None]
        f[...,:2]=h[...,:2]+axis*clamp[...,None]
        along=(up**2-low**2+clamp**2)/(2*clamp)
        radius=np.sqrt(np.maximum(0,up**2-along**2))
        k=np.zeros_like(h)
        k[...,:2]=h[...,:2]+axis*along[...,None]+np.stack([-axis[...,1],axis[...,0]],axis=-1)*radius[...,None]*np.sign(sourcecross)[None,None,:,None]
        k[...,2]=kz
        ck=np.array([[-.34,.20],[.34,.20],[-.38,mid_knee_y],[.38,mid_knee_y],[-.34,-.154],[.34,-.154]])
        ct=np.array([[-.068,.411],[.068,.411],[-.13,mid_toe_y],[.13,mid_toe_y],[-.06,-.349],[.06,-.349]])
        uv=ck-mount
        lv=ct-ck
        cu=np.concatenate([uv,np.sqrt(lengths[:,0]**2-(uv**2).sum(axis=-1))[:,None]],axis=-1)/lengths[:,0,None]
        cl=np.concatenate([lv,-np.sqrt(lengths[:,1]**2-(lv**2).sum(axis=-1))[:,None]],axis=-1)/lengths[:,1,None]
        ku=slerp(unit(k-h),cu,tuck[...,None])
        fl=slerp(unit(f-k),cl,tuck[...,None])
        k=h+ku*lengths[:,0,None]
        f=k+fl*lengths[:,1,None]
        foregap=(k[...,:2,1]-k[...,2:4,1])
        hindgap=(k[...,2:4,1]-k[...,4:6,1])
        winding=cross((f-h)[...,:2],(k-h)[...,:2])*sourcecross
        lower_intersections=[]
        cross_upper=[]
        for a in range(6):
            for b in range(a+1,6):
                hits,points=intersections(k[...,a,:2],f[...,a,:2],k[...,b,:2],f[...,b,:2])
                if hits.any():
                    yy,xx=np.nonzero(hits)
                    lower_intersections.append({"pair":[IDS[a],IDS[b]],"count":int(hits.sum()),"first_row_phase":[int(yy[0]),int(xx[0])],"outside_body_count":int((hits&outside_body(points)).sum())})
            for b in range(6):
                if a==b:
                    continue
                hits,points=intersections(k[...,a,:2],f[...,a,:2],h[...,b,:2],k[...,b,:2])
                visible=hits&outside_body(points)
                if visible.any():
                    yy,xx=np.nonzero(visible)
                    cross_upper.append({"distal_upper_pair":[IDS[a],IDS[b]],"outside_body_count":int(visible.sum()),"first_row_phase":[int(yy[0]),int(xx[0])],"first_xy":points[yy[0],xx[0]].tolist()})
        footcontacts=(abs(f[...,2])<1e-6).sum(axis=-1)
        toe_min_index=np.unravel_index(np.argmin(f[...,2]),f[...,2].shape)
        coefficient=np.sin(np.pi*tuck)**2
        transition_lift=.015*coefficient
        translated_f=f[...,2]+transition_lift
        row_minima=np.array([f[row,...,2].min() for row in range(9)])
        coeff=coefficient[:,0,0]
        active=coeff>1e-8
        required_h=max(0,float(np.max((-row_minima[active])/coeff[active])))
        required_h_margin=max(0,float(np.max((.001-row_minima[active])/coeff[active])))
        error=max(float(abs(np.linalg.norm(k-h,axis=-1)-lengths[:,0]).max()),float(abs(np.linalg.norm(f-k,axis=-1)-lengths[:,1]).max()))
        summary={"cycle":cycle,"fore_mount_factor_xy":[.4,fore_factor_y],"fore_hind_support":fh_support,"fore_hind_extended_lane_y":fh_lane_y,"middle_support":mid_support,"middle_phase_add":mid_phase,"middle_extended_relative_xy":mid_ext,"middle_guarded_relative_xy":mid_guard,"closed_middle_toe_y":mid_toe_y,"closed_middle_knee_y":mid_knee_y,
            "max_grounded_pre_tuck_reach_clamp_error":float(clamp_error.max()),"pre_tuck_clamped_chain_count":int((clamp_error>1e-7).sum()),
            "min_fore_middle_knee_gap":float(foregap.min()),"min_middle_hind_knee_gap":float(hindgap.min()),"min_source_winding_product":float(winding.min()),"max_bone_error":error,
            "min_contacts_walking_rows0to4":int(footcontacts[:5].min()),"min_endpoint_z":float(min(h[...,2].min(),k[...,2].min(),f[...,2].min())),
            "minimum_toe_row_phase_leg":{"row":int(toe_min_index[0]),"phase":int(toe_min_index[1]),"leg":IDS[toe_min_index[2]],"z":float(f[...,2][toe_min_index])},
            "minimum_toe_z_by_row":[float(f[row,...,2].min()) for row in range(9)],
            "separate_uniform_virtual_transition_lift_proposal":{"formula":"After composing the raised virtual chain, translate ALL hip/knee/toeZ by.015*sin(pi*tuck)^2,tuck=clamp((fold-.5)/.5,0,1). Explicit hypothetical virtual body-height curve, not toe clamping; does not changeXY or3Dlengths. No canonical edit was made.","minimum_toe_z_by_row_after_translation":[float(translated_f[row].min()) for row in range(9)],"required_nominal_peak_H_for_z_ge0":required_h,"required_nominal_peak_H_for_raised_z_ge_point001":required_h_margin,"all432_endpoints_nonnegative_after_common_lift":bool(np.minimum(np.minimum(h[...,2],k[...,2]),f[...,2]).__add__(transition_lift).min()>=-1e-7),"fully_closed_translation":0,"walking_row_translation":0},
            "distal_distal_intersections":lower_intersections,"distal_upper_intersections_outside_source_owned_body":cross_upper}
        summary["formal_region_reach_candidate"]=summary["pre_tuck_clamped_chain_count"]==0 and summary["min_fore_middle_knee_gap"]>.1 and summary["min_middle_hind_knee_gap"]>.1 and summary["min_source_winding_product"]>0 and not lower_intersections and not cross_upper and summary["min_endpoint_z"]>=-1e-7
        summary["plus_min3_contacts"]=summary["formal_region_reach_candidate"] and summary["min_contacts_walking_rows0to4"]>=3
        if "--export-selected-coordinates" in sys.argv and cycle==1.25 and mid_support==.35 and mid_phase==.35 and mid_toe_y==-.10 and fh_support==.65 and mid_knee_y==.06 and fh_lane_y==.48 and fore_factor_y==.4:
            rows=[]
            for row in range(9):
                for phase in range(48):
                    rows.append({"row":row,"phase":phase,"hips":h[row,phase].tolist(),"knees":k[row,phase].tolist(),"feet":f[row,phase].tolist()})
            (OUT/"independent-selected-candidate-coordinates.json").write_text(json.dumps(rows,separators=(",",":")))
        return summary
    lane_sets=[((.56,-.30),(.50,-.30)),((.53,-.24),(.50,-.30)),((.53,-.26),(.50,-.30)),((.54,-.26),(.50,-.30)),((.53,-.22),(.46,-.14))]
    summaries=[]
    selected_only="--selected-only" in sys.argv
    if not selected_only:
        for ext,guard in lane_sets:
            for ms in (.35,.40,.45,.50,.52):
                for mp in np.arange(.1,.25001,.025):
                    summaries.append(simulate(1.25,ms,float(mp),ext,guard))
    selected=[simulate(1.25,.35,.15,(.56,-.30),(.50,-.30)),simulate(1.25,.50,.15,(.53,-.26),(.50,-.30))]
    valid=[x for x in summaries if x["formal_region_reach_candidate"]]
    support_valid=[x for x in valid if x["plus_min3_contacts"]]
    ranked=sorted(summaries,key=lambda x:(x["formal_region_reach_candidate"],x["min_fore_middle_knee_gap"],x["min_middle_hind_knee_gap"]),reverse=True)
    # Separate hypothetical middle-toe clearance variation; not a canonical edit.
    middle_toe_studies=[simulate(1.25,.35,.15,(.56,-.30),(.50,-.30),y) for y in (-.12,-.08,-.04)]
    root_one_candidate=simulate(1.25,.35,.35,(.56,-.30),(.50,-.30),mid_toe_y=-.04,fh_support=.65,mid_knee_y=.06,fh_lane_y=.48)
    root_v18_candidate=simulate(1.25,.35,.35,(.56,-.30),(.50,-.30),mid_toe_y=-.08,fh_support=.65,mid_knee_y=.06,fh_lane_y=.48)
    root_v20_mounts=[simulate(1.25,.35,.35,(.56,-.30),(.50,-.30),mid_toe_y=-.10,fh_support=.65,mid_knee_y=.06,fh_lane_y=.48,fore_factor_y=fy) for fy in (.4,.5,.65)]
    result={"status":"HYPOTHETICAL MATH/SKETCH ONLY; canonical files unchanged", "mount_factors_xy":factors.tolist(),"cycles_tested":[1.25],"frames_per_combination":432,
        "comparison_scope":"Own double-precision reproduction of current coordinate equations, modified only by explicit proposed cycle/support/phase/lanes/mounts. No C# invocation. Art and transition acceptance absent.",
        "body_mask_scope":"SourceOwnershipStudy01 binary body ownership used only to distinguish clearly outside-body centerline intersections. Its known underbelly trace defect means near-arch classification is provisional; this cannot pass actual skin overlaps.",
        "combinations":len(summaries),"region_reach_candidates":len(valid),"candidates_also_min3contacts":len(support_valid),"selected_examples":selected,"best_formal_candidates":valid[:20],"best_with_min3contacts":support_valid[:20],"highest_sector_ranked":ranked[:10],"closed_middle_toe_variations":middle_toe_studies,"root_FH65_mid35_phase35_candidate":root_one_candidate,"root_FH65_mid35_phase35_closedMtoe_minus_point08_candidate":root_v18_candidate,"root_selected_Mtoe_minus_point10_fore_mount_y_factors_three_only":root_v20_mounts,
        "contact_limit":"Middle support<.5 allows gaps with both middle feet lifted. With fore/hind support.56 and middlephase+.1..25, old minimum3contacts is not guaranteed; report actualcounts, do not call cadence/physics or artistic PASS.",
        "limits":["Zero centerline intersections does not establish no skin overlap: actual thick source surfaces/roots remain an art check.","Fixed48phases/9folds here are full authored grid, not continuous-time/interpolation proof.","A chosen proposal needs canonical C# reexport +independent gate before any skin mass export.","No actual3D body/skin surface, collision or grounded closed rest is certified."]}
    (OUT/("stride-selected-three-mounts-study.json" if selected_only else "stride-regions-study.json")).write_text(json.dumps(result,indent=2),encoding="utf-8")
    print(json.dumps({"selected_only":selected_only,"root_Mtoe_minus_point10_three_fore_y_mounts":root_v20_mounts},indent=2))


if __name__=="__main__":
    main()
