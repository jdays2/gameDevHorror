"""Separate 2D functional volume proposal AFTER reviewing the bone-only plan.

No source art/painter change. Projected widths are draft authoring parameters,
not measured internal muscles, not a 3D physical collision/occlusion asset.
"""
from pathlib import Path
import json
import math
import numpy as np
from PIL import Image, ImageDraw, ImageFont

OUT=Path(__file__).resolve().parent
COLORS=["#d07800","#a85b00","#1584bb","#075b90","#b548ba","#7e267f"]
IDS=["LF","RF","LM","RM","LR","RR"]
JOINT_RADII=[12.5/224,12/224,10/224,10.5/224,12/224,11.5/224]


def ribbon(start,end,radii):
    start,end=np.asarray(start),np.asarray(end)
    axis=end-start
    norm=np.array([-axis[1],axis[0]])/np.linalg.norm(axis)
    centers=[start+(end-start)*t for t in np.linspace(0,1,len(radii))]
    return [c+norm*r for c,r in zip(centers,radii)]+[c-norm*r for c,r in reversed(list(zip(centers,radii)))]


def main():
    gate=json.loads((OUT/"canonical-bone-gate.json").read_text())
    assert gate["status"]=="FORMAL COORDINATE GATE PASS"
    poses=gate["selected_new_bone_plans"]
    chart=Image.new("RGB",(1400,880),(248,249,251))
    d=ImageDraw.Draw(chart)
    try:
        font=ImageFont.truetype("C:/Windows/Fonts/arial.ttf",18)
        small=ImageFont.truetype("C:/Windows/Fonts/arial.ttf",15)
    except OSError:
        font=ImageFont.load_default()
        small=font
    d.text((20,14),"Separate projected soft-envelope PROPOSAL: no skin or source concept, no automatic art approval.",fill="#20252a",font=font)
    d.text((20,42),"Same six chains and three functional groups per limb; widths below are draft 2D art parameters, not measured muscles.",fill="#20252a",font=small)
    records=[]
    for panel,p in enumerate(poses):
        xbase=panel*700
        project=lambda xy:(xbase+350+xy[0]*270,435-xy[1]*270)
        d.rectangle((xbase+15,80,xbase+685,790),outline="#ccd2da",width=1)
        d.text((xbase+30,95),f'row{p["row"]} phase0; TOP XY; hips and knee positions unchanged',fill="#20252a",font=font)
        d.line((project((-1.1,0)),project((1.1,0))),fill="#dce1e7",width=1)
        d.line((project((0,-1.1)),project((0,1.1))),fill="#dce1e7",width=1)
        for leg,label in enumerate(IDS):
            h,k,t=[np.asarray(p[key][leg][:2]) for key in ("hips","knees","feet")]
            pair=leg//2
            center_radius=([.047,.043,.050] if p["row"]==4 else [.072,.063,.075])[pair]
            root_radius=([.045,.040,.048] if p["row"]==4 else [.060,.055,.062])[pair]
            end_radius=JOINT_RADII[leg]*.88
            radii=[root_radius,center_radius*.92,center_radius,end_radius]
            poly=ribbon(h,k,radii)
            rgb=tuple(int(COLORS[leg][i:i+2],16) for i in (1,3,5))
            fill=tuple(int(v*.22+255*.78) for v in rgb)
            d.polygon([project(xy) for xy in poly],fill=fill,outline=COLORS[leg])
            px,py=project(k)
            rr=JOINT_RADII[leg]*270
            d.ellipse((px-rr,py-rr,px+rr,py+rr),fill=fill,outline=COLORS[leg],width=1)
            sleeve_end=k+(t-k)*.28
            sleeve=ribbon(k,sleeve_end,[JOINT_RADII[leg]*.8,JOINT_RADII[leg]*.65,JOINT_RADII[leg]*.45])
            d.polygon([project(xy) for xy in sleeve],fill=fill,outline=COLORS[leg])
            d.line([project(h),project(k),project(t)],fill=COLORS[leg],width=2)
            d.text((px+5,py-22),label,fill=COLORS[leg],font=font)
            records.append({"row":p["row"],"phase":0,"id":label,"hip_xy":h.tolist(),"knee_xy":k.tolist(),"toe_xy":t.tolist(),
                "functional_groups":[{"id":label+"-1","name":"proximal actuator","attachment":"same canonical hip->knee chain","projected_radii_root_peak_end": [root_radius,center_radius,end_radius],"fiber_direction":"hip->knee"},
                {"id":label+"-2","name":"joint stabilizer","attachment":"same canonical knee, no new joint","projected_radius":JOINT_RADII[leg]},
                {"id":label+"-3","name":"distal tendon sleeve","attachment":"knee->28% of existing distal chain","fiber_direction":"knee->toe","maximum_projected_radius":JOINT_RADII[leg]*.8}],
                "upper_envelope_polygon_xy":[xy.tolist() for xy in poly],"distal_sleeve_polygon_xy":[xy.tolist() for xy in sleeve]})
        d.text((xbase+30,750),"Only centerlines declare virtual Z. 2D ribbons do not establish surface height or floor clearance.",fill="#626e78",font=small)
    d.text((20,815),"Body-owned root cover must be judged on the actual whole artwork: no duplicated collar, loose point, hard ring or hole.",fill="#20252a",font=font)
    d.text((20,844),"Closed bulk should be drawn as continuous skin around these same chains; group outlines here are diagnostic and must not appear in clean art.",fill="#20252a",font=small)
    chart.save(OUT/"canonical-soft-envelope-proposal-row4-row8.png")
    proposal={"status":"DRAFT ENVELOPE PROPOSAL ONLY; not a passed muscle or skin stage", "bone_gate_source_hashes":gate["source_hashes"],
        "group_contract":"Six unchanged chains, three existing functional groups each =18; no new muscle/limb/joint. Skin covers diagnostic subdivisions.",
        "scope":"2D projected art envelope only; no collider, physics3D asset or body-volume resource. No source raster was altered.",
        "authoring_choice":"Peak proximal soft radius is deliberately fuller at closed row8 than row4 to address thin walking-shaft material. Values are explicit proposals, not measured hidden anatomy or approved final shape.",
        "constraints":["Keep canonical hips/knees/toes and stable group IDs.","Root belongs under one body-owned continuous skin cover in the whole 2D image; inspect all extreme phases and colors.","No circular patch, complementary alpha cut, duplicate root outline or isolated skin remnant.","Preserve bone/claw taper and markings; do not enlarge white blade to imitate flesh volume.","Use separate clean controls for source ownership then volume; compare native size on green/light/dark before any mass export.","Do not claim3D surface occlusion, support or floor clearance from these 2D outlines.","H01 remains FAIL; a formal coordinate gate does not approve new art."],"envelopes":records}
    (OUT/"canonical-soft-envelope-proposal.json").write_text(json.dumps(proposal,indent=2),encoding="utf-8")


if __name__=="__main__":
    main()
