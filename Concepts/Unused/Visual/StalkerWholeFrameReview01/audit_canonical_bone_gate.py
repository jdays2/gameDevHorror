"""Independent gate for exported canonical coordinates, not Unity execution.

Writes only this audit folder. Plans contain no skin. Virtual 3D heights and
lengths are authored constructs; a PNG cannot verify them or grounded rest.
"""
from pathlib import Path
import hashlib
import json
import argparse
import numpy as np
from PIL import Image, ImageDraw, ImageFont

OUT=Path(__file__).resolve().parent
PROJECT=OUT.parents[3]
FAMILY=PROJECT/"Concepts/Used/Stalker/Postures05"
COLORS=["#d07800","#a85b00","#1584bb","#075b90","#b548ba","#7e267f"]
IDS=["LF","RF","LM","RM","LR","RR"]


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def cross2(a,b):
    return a[...,0]*b[...,1]-a[...,1]*b[...,0]


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--output-prefix",default="")
    args=parser.parse_args()
    source_path=PROJECT/"Concepts/Used/Stalker/Locomotion02/anatomy.json"
    source=json.loads(source_path.read_text())
    poses=json.loads((FAMILY/"poses.json").read_text())
    assert len(poses)==432
    assert len({(p["row"],p["phase"]) for p in poses})==432
    upper=np.array([x["upperLength"] for x in source["limbs"]])
    lower=np.array([x["lowerLength"] for x in source["limbs"]])
    source_winding=[]
    for limb in source["limbs"]:
        h,k,t=[np.asarray(limb[key])*np.array([1.,-1.])/224 for key in ("hip","knee","tip")]
        source_winding.append(cross2(t-h,k-h))
    source_winding=np.array(source_winding)
    records=[]
    failures=[]
    for p in poses:
        h,k,t=[np.asarray(p[key],dtype=float) for key in ("hips","knees","feet")]
        assert h.shape==k.shape==t.shape==(6,3)
        finite=bool(np.isfinite(np.concatenate([h,k,t])).all())
        upper_error=np.abs(np.linalg.norm(k-h,axis=1)-upper)
        lower_error=np.abs(np.linalg.norm(t-k,axis=1)-lower)
        actual=cross2((t-h)[:,:2],(k-h)[:,:2])
        winding_product=actual*source_winding
        sector_fore=k[:2,1]-k[2:4,1]
        sector_hind=k[2:4,1]-k[4:6,1]
        projected_ratio=np.linalg.norm((t-k)[:,:2],axis=1)/lower
        closed_directions=bool((t[:2,1]>k[:2,1]).all() and (t[4:6,1]<k[4:6,1]).all())
        record={"row":p["row"],"phase":p["phase"],"finite":finite,
            "max_upper_length_error":float(upper_error.max()),"max_lower_length_error":float(lower_error.max()),
            "minimum_point_z":float(min(h[:,2].min(),k[:,2].min(),t[:,2].min())),
            "minimum_foot_z":float(t[:,2].min()),"minimum_knee_z":float(k[:,2].min()),
            "minimum_source_winding_product":float(winding_product.min()),
            "minimum_fore_middle_knee_y_gap":float(sector_fore.min()),
            "minimum_middle_hind_knee_y_gap":float(sector_hind.min()),
            "minimum_distal_projection_length_ratio":float(projected_ratio.min()),
            "foot_contacts_z_le_1e-6":int((np.abs(t[:,2])<=1e-6).sum()),
            "closed_longitudinal_direction_signs":closed_directions}
        records.append(record)
        checks={"finite":finite,"length_error_le_1e-5":bool(max(upper_error.max(),lower_error.max())<=1e-5),
            "all_bone_points_z_ge_0":record["minimum_point_z"]>=-1e-7,
            "source_winding_preserved":bool((winding_product>0).all()),
            "fore_middle_sector_gap_gt_point1":bool((sector_fore>.1).all()),
            "middle_hind_sector_gap_gt_point1":bool((sector_hind>.1).all()),
            "distal_projection_over_half_length":bool((projected_ratio>.5).all())}
        if p["row"]==8:
            checks["fully_closed_fore_forward_hind_backward"]=closed_directions
        if not all(checks.values()):
            failures.append({"row":p["row"],"phase":p["phase"],"checks":checks})
    by_row=[]
    for row in range(9):
        r=[x for x in records if x["row"]==row]
        by_row.append({"row":row,"fold":row/8,"foot_contacts_min_max":[min(x["foot_contacts_z_le_1e-6"] for x in r),max(x["foot_contacts_z_le_1e-6"] for x in r)],
            "min_foot_z":min(x["minimum_foot_z"] for x in r),"min_knee_z":min(x["minimum_knee_z"] for x in r),
            "min_fore_middle_gap":min(x["minimum_fore_middle_knee_y_gap"] for x in r),
            "min_middle_hind_gap":min(x["minimum_middle_hind_knee_y_gap"] for x in r),
            "frames_with_closed_longitudinal_signs":sum(x["closed_longitudinal_direction_signs"] for x in r)})
    selected=[p for p in poses if p["row"] in (4,8) and p["phase"]==0]
    # Skinless plans FIRST. Shared axes do not register or fit the anatomy to art.
    chart=Image.new("RGB",(1800,1280),(248,249,251))
    draw=ImageDraw.Draw(chart)
    try:
        font=ImageFont.truetype("C:/Windows/Fonts/arial.ttf",18)
        small=ImageFont.truetype("C:/Windows/Fonts/arial.ttf",15)
    except OSError:
        font=ImageFont.load_default()
        small=font
    draw.text((20,12),"Canonical bone plans only: source skin is absent. Virtual 3D is authored, not measured from PNG.",fill="#20252a",font=font)
    draw.text((20,40),"Closed legs are raised; grounded body support, torso surfaces and 3D occlusion are not established.",fill="#20252a",font=font)
    def project(x,y,col,row,side=False):
        return (col*600+300+x*200,90+row*590+((470-y*200) if side else (280-y*200)))
    def stroke(points,color,width=2):
        draw.line(points,fill=color,width=width)
    def point(p,color):
        x,y=p
        draw.ellipse((x-4,y-4,x+4,y+4),fill=color,outline=color)
    def panel_axes(col,row,side=False):
        x0,y0=col*600,90+row*590
        draw.rectangle((x0+10,y0+10,x0+590,y0+570),outline="#c9d0d8",width=1)
        for x in np.arange(-1,1.01,.25):
            a=project(x,-.025 if side else -1.1,col,row,side)
            b=project(x,.7 if side else 1.1,col,row,side)
            stroke((a,b),"#e0e4e9",1)
            draw.text((a[0]-10,a[1]+8),f"{x:g}",fill="#66727d",font=small)
        levels=np.arange(0,.701,.1) if side else np.arange(-1,1.01,.25)
        for y in levels:
            a,b=project(-1.1,y,col,row,side),project(1.1,y,col,row,side)
            stroke((a,b),"#e0e4e9",1)
            draw.text((a[0]-37,a[1]-8),f"{y:g}",fill="#66727d",font=small)
        if side:
            stroke((project(-1.1,0,col,row,True),project(1.1,0,col,row,True)),"#c63737",3)
            draw.text((x0+45,y0+520),"Y rear < 0 < front; Z above floor",fill="#20252a",font=small)
        else:
            draw.text((x0+45,y0+540),"X left / right; Y rear < 0 < front",fill="#20252a",font=small)
    for row_index,p in enumerate(selected):
        h,k,t=[np.asarray(p[key],dtype=float) for key in ("hips","knees","feet")]
        panel_axes(0,row_index)
        draw.text((30,110+row_index*590),f'row {p["row"]}, phase0: TOP XY',fill="#20252a",font=font)
        poly=[project(*xy,0,row_index) for xy in np.vstack([h[[0,1,3,5,4,2],:2],h[0,:2]])]
        stroke(poly,"#89949d",1)
        for i,label in enumerate(IDS):
            line=np.vstack([h[i],k[i],t[i]])
            pixels=[project(*xy,0,row_index) for xy in line[:,:2]]
            draw.line(pixels,fill=COLORS[i],width=3)
            for pp in pixels:
                point(pp,COLORS[i])
            px,py=pixels[1]
            draw.text((px+5,py-18),label,fill=COLORS[i],font=font)
        for col,legs in enumerate(((0,2,4),(1,3,5)),start=1):
            panel_axes(col,row_index,True)
            draw.text((col*600+30,110+row_index*590),f'row {p["row"]}, phase0: SIDE {"L" if col==1 else "R"} (Y / virtual Z)',fill="#20252a",font=font)
            draw.text((col*600+30,145+row_index*590),"Hip -> knee -> toe; bone centerlines only",fill="#66727d",font=small)
            for i in legs:
                line=np.vstack([h[i],k[i],t[i]])
                pixels=[project(*yz,col,row_index,True) for yz in line[:,1:]]
                draw.line(pixels,fill=COLORS[i],width=3)
                for pp in pixels:
                    point(pp,COLORS[i])
                px,py=pixels[1]
                draw.text((px+5,py-18),IDS[i],fill=COLORS[i],font=font)
    chart.save(OUT/(args.output_prefix+"canonical-bone-plans-row4-row8-top-side.png"))
    closed=[p for p in poses if p["row"]==8]
    first=np.concatenate([np.asarray(closed[0][key]).ravel() for key in ("hips","knees","feet")])
    closed_phase_max=max(float(np.abs(np.concatenate([np.asarray(p[key]).ravel() for key in ("hips","knees","feet")])-first).max()) for p in closed)
    report={"status":"FORMAL COORDINATE GATE PASS" if not failures else "FORMAL COORDINATE GATE FAIL",
        "scope":"Offline 2D anatomical layout + authored virtual3D chains only. No art, physics, support, Unity runtime or occlusion acceptance.",
        "source_hashes":{"poses.json":digest(FAMILY/"poses.json"),"source_anatomy.json":digest(source_path),"StalkerPoseComposer.cs":digest(PROJECT/"Assets/EmergencyShift/Editor/Authoring/StalkerSprites/StalkerPoseComposer.cs")},
        "frames":len(poses),"source_winding_cross_foot_hip_knee_hip_xy_up":dict(zip(IDS,source_winding.tolist())),
        "maximum_bone_length_error":max(max(r["max_upper_length_error"],r["max_lower_length_error"]) for r in records),
        "minimum_source_winding_product":min(r["minimum_source_winding_product"] for r in records),
        "minimum_point_z":min(r["minimum_point_z"] for r in records),
        "minimum_distal_projection_length_ratio":min(r["minimum_distal_projection_length_ratio"] for r in records),
        "closed_maximum_phase_coordinate_difference":closed_phase_max,
        "by_row":by_row,"failures":failures,"selected_new_bone_plans":selected,
        "limits":["No true3D lengths, heights, body top/underside or floor contacts were measured from source PNG.","No feet contact floor in fully tucked rows; a body-rest support surface is not declared. This is not physically validated resting anatomy.","Nonnegative endpoints imply nonnegative straight bone centerlines, not that skin thickness clears the floor.","Source winding sign and knee sectors do not establish skin continuity, no self intersections or anatomical volume.","Current draw-order/body occlusion is a 2D art composition check, not verified3D occlusion.","Old H01 artwork remains FAIL; this report cannot approve a renderer or a new complete set."]}
    native=json.loads((OUT/"native-Recoil03-observed-chains.json").read_text())
    new_closed=next(p for p in selected if p["row"]==8)
    native_comparison=[]
    for i,observed in enumerate(native["chains"]):
        projected={key:(np.asarray(new_closed[arraykey][i][:2])*np.array([384.,-384.])+np.array([174.,166.])).tolist() for key,arraykey in (("hip","hips"),("knee","knees"),("tip","feet"))}
        item={"id":IDS[i],"canonical_xy_projected_to_native_crop":projected,
            "native_observed_hip_inferred":observed["hip"],"native_observed_knee_skin_mass":observed["knee"],
            "knee_error_pixels":float(np.linalg.norm(np.asarray(projected["knee"])-observed["knee"])),
            "inferred_hip_difference_pixels_not_a_fail":float(np.linalg.norm(np.asarray(projected["hip"])-observed["hip"]))}
        if observed["tip"] is not None:
            item["native_visible_pointed_tip"]=observed["tip"]
            item["tip_error_pixels"]=float(np.linalg.norm(np.asarray(projected["tip"])-observed["tip"]))
        else:
            item["native_true_tip"]=None
            item["native_visible_bone_termination_at_occlusion"]=observed["visible_distal_termination"]
            item["tip_comparison"]="Not comparable: canonical toe is behind/below knee in top view; native visible distal fragment runs toward torso/front, but its true tip is hidden. Do not force this uncertain endpoint or inferred hip into a claimed anatomy pass."
        native_comparison.append(item)
    report["native_closed_comparison"]={"registration":"One uniform PPU384 and eye crop anchor(174,166), no per-limb fit", "comparisons":native_comparison,
        "interpretation":"Fore/hind visible endpoints agree closely. Hip observations remain uncertain and substantially differ because canonical mounts stay under the current core. Middle endpoints cannot be certified against the painting. Source-winding preservation is relative to the open Walk00 anatomy, not proof that the native closed illustration shares all hidden anatomy."}
    (OUT/(args.output_prefix+"canonical-bone-gate.json")).write_text(json.dumps(report,indent=2),encoding="utf-8")
    print(json.dumps({k:report[k] for k in ("status","frames","maximum_bone_length_error","minimum_source_winding_product","minimum_point_z","minimum_distal_projection_length_ratio","closed_maximum_phase_coordinate_difference","by_row","failures")},indent=2))


if __name__=="__main__":
    main()
