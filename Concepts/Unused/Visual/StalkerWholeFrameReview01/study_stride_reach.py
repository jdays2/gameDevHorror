"""Math-only stride/lane proposals. No C#, canonical pose or painter writes.

Analytic support endpoints cover the whole stance interval: requested foot Y
is linear in phase, so the largest radial reach occurs at an endpoint.
"""
from pathlib import Path
import hashlib
import json
import math
import numpy as np
from PIL import Image,ImageDraw,ImageFont

OUT=Path(__file__).resolve().parent
PROJECT=OUT.parents[3]
SOURCE=PROJECT/"Concepts/Used/Stalker/Locomotion02/anatomy.json"
IDS=["LF","RF","LM","RM","LR","RR"]
OLD_FACTORS=[.65,.65,.70,.70,.72,.72]
INNER_FACTORS=[.4,.4,.4,.4,.72,.72]
SUPPORT=[.56,.56,.52,.52,.56,.56]


def lane(leg,guard=False,original=False,narrow_fore=False):
    side=-1 if leg%2==0 else 1
    if leg<2:
        inward=(.035 if guard else .04) if narrow_fore else (.075 if guard else .085)
        return np.array([-side*inward,(.35 if guard else .61) if original else (.30 if guard else .52)])
    if leg<4:
        return np.array([side*(.46 if guard else (.58 if original else .53)),(-.20 if guard else -.30) if original else (-.14 if guard else -.22)])
    return np.array([-side*(.09 if guard else .11),-((.35 if guard else .61) if original else (.30 if guard else .52))])


def main():
    source=json.loads(SOURCE.read_text())
    hips=np.array([(np.asarray(x["hip"])-np.asarray(source["pivot"]))*np.array([1.,-1.])/224 for x in source["limbs"]])
    lengths=np.array([[x["upperLength"],x["lowerLength"]] for x in source["limbs"]])
    scenarios=[
        {"id":"unchanged_old_lanes","factors":OLD_FACTORS,"original":True,"preserve_world":False},
        {"id":"proposed_lanes_current_mounts","factors":OLD_FACTORS,"original":False,"preserve_world":False},
        {"id":"proposed_relative_lanes_inner_fore_middle_mounts","factors":INNER_FACTORS,"original":False,"preserve_world":False},
        {"id":"same_absolute_world_feet_inner_fore_middle_mounts","factors":INNER_FACTORS,"original":False,"preserve_world":True},
        {"id":"new_deep_foreX40Y65_middleXY40_relative_lanes","factors":[[.4,.65],[.4,.65],[.4,.4],[.4,.4],[.72,.72],[.72,.72]],"original":False,"preserve_world":False,"narrow_fore":True},
    ]
    results=[]
    plans=[]
    for scenario in scenarios:
        entry={"id":scenario["id"],"mount_factors":scenario["factors"],"foot_centers_are":"old-current world positions retained" if scenario["preserve_world"] else "specified offsets relative to chosen hip", "cycle_max_without_reach_clamp_by_leg":[],"cycle_samples":[]}
        for leg,label in enumerate(IDS):
            maximum_cycle=float("inf")
            limiting_fold=0
            for fold in np.linspace(0,.5,1001):
                stride=1+(.8-1)*(fold/.5)
                h=.13+.07*fold
                upper,lower=np.sqrt(lengths[leg]**2-(h/2)**2)
                radius=upper+lower-.0001
                relative=lane(leg,False,scenario["original"],scenario.get("narrow_fore",False))*(1-fold)+lane(leg,True,scenario["original"],scenario.get("narrow_fore",False))*fold
                if scenario["preserve_world"]:
                    relative+=hips[leg]*(OLD_FACTORS[leg]-scenario["factors"][leg])
                if abs(relative[0])>=radius:
                    allowed=-float("inf")
                else:
                    allowed=2*(math.sqrt(radius**2-relative[0]**2)-abs(relative[1]))/(SUPPORT[leg]*stride)
                if allowed<maximum_cycle:
                    maximum_cycle=float(allowed)
                    limiting_fold=float(fold)
            entry["cycle_max_without_reach_clamp_by_leg"].append({"id":label,"maximum_cycle_units":maximum_cycle,"limiting_fold":limiting_fold})
        for cycle in (1.2,1.25,1.3,1.325):
            margins=[]
            for fold in np.linspace(0,.5,1001):
                stride=1+(.8-1)*(fold/.5)
                h=.13+.07*fold
                for leg in range(6):
                    upper,lower=np.sqrt(lengths[leg]**2-(h/2)**2)
                    relative=lane(leg,False,scenario["original"],scenario.get("narrow_fore",False))*(1-fold)+lane(leg,True,scenario["original"],scenario.get("narrow_fore",False))*fold
                    if scenario["preserve_world"]:
                        relative+=hips[leg]*(OLD_FACTORS[leg]-scenario["factors"][leg])
                    amplitude=SUPPORT[leg]*.5*cycle*stride
                    max_radius=math.hypot(relative[0],abs(relative[1])+amplitude)
                    min_y=max(0,abs(relative[1])-amplitude)
                    min_radius=math.hypot(relative[0],min_y)
                    margins.append({"id":IDS[leg],"fold":float(fold),"upper_margin_after_clamp_epsilon":float(upper+lower-.0001-max_radius),"lower_margin_after_clamp_epsilon":float(min_radius-(abs(upper-lower)+.0001))})
            worst_upper=min(margins,key=lambda x:x["upper_margin_after_clamp_epsilon"])
            worst_lower=min(margins,key=lambda x:x["lower_margin_after_clamp_epsilon"])
            entry["cycle_samples"].append({"cycle_units":cycle,"stance_reach_no_clamp":worst_upper["upper_margin_after_clamp_epsilon"]>=0 and worst_lower["lower_margin_after_clamp_epsilon"]>=0,"worst_upper_margin":worst_upper,"worst_lower_margin":worst_lower})
        entry["maximum_cycle_units_all_legs"]=min(x["maximum_cycle_units"] for x in entry["cycle_max_without_reach_clamp_by_leg"])
        results.append(entry)
        if scenario["id"] in ("proposed_lanes_current_mounts","new_deep_foreX40Y65_middleXY40_relative_lanes"):
            for fold in (0,.5):
                for leg,label in enumerate(IDS):
                    mount=hips[leg]*scenario["factors"][leg]
                    relative=lane(leg,narrow_fore=scenario.get("narrow_fore",False))*(1-fold)+lane(leg,True,narrow_fore=scenario.get("narrow_fore",False))*fold
                    stride=1+(.8-1)*(fold/.5)
                    a=SUPPORT[leg]*.5*1.3*stride
                    plans.append({"scenario":scenario["id"],"fold":fold,"id":label,"mount_xy":mount.tolist(),"foot_world_center_xy":(mount+relative).tolist(),"relative_lane_xy":relative.tolist(),"stance_y_offset_half_range":a,"stance_world_endpoints_xy":[(mount+relative+np.array([0,a])).tolist(),(mount+relative-np.array([0,a])).tolist()]})
    report={"status":"MATH-ONLY PROPOSAL; no implementation or artistic tempo acceptance", "source_sha256":hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
        "assumptions":{"compact_stride_scale":.8,"compact_walking_fold":.5,"fore_hind_support_fraction":.56,"middle_support_fraction":.52,"stance_foot_height":0,"hip_height": ".13+.07*fold", "grounded_knee_height":"hip_height/2", "walking_folds_checked":"0..0.5 inclusive,1001samples; analytic interval endpoints cover every stance phase at each fold", "reach_clamp_epsilon":.0001},
        "critical_formula":"A=supportFraction*0.5*cycleDistance*strideScale. Runtime phase rate=speed/(cycleDistance*strideScale*actorScale). Increasing runtime cycle distance alone without increasing baked stance amplitude causes sliding.",
        "lerp_caveat":"Current C# interpolates extended->guarded with walkingFold itself (0..0.5), not walkingFold/.5. Compact row4 therefore uses the MIDPOINT of the two lane definitions; strideScale independently reaches .8 at fold.5.",
        "results":results,"recommended_unchanged_mounts_range":{"cycle_units":[1.2,1.3],"compact_stride_scale":.8,"normal_relative_lanes":{"fore":["inward .085","forward .52"],"middle":["outward .53","rear .22"],"hind":["inward .11","rear .52"]},"guarded_lane_definitions":{"fore":["inward .075","forward .30"],"middle":["outward .46","rear .14"],"hind":["inward .09","rear .30"]},"caveat":"Reach-only safe range under stated construct, not a skin, foot-ground material, sector, motion or art PASS.1.30 leaves about.01unit worst reach margin, so1.25 is more conservative. Actual3D length/physical anatomy is not inferred fromPNG."},
        "absolute_foot_plans_cycle1p3":plans,
        "tempo_at_unit_actor_scale":[{"cycle_units":c,"open_walk1p2_cycles_per_second":1.2/c,"open_pursuit1p5_cycles_per_second":1.5/c,"compact_retreat3p8_cycles_per_second":3.8/(c*.8)} for c in (1.2,1.25,1.3)],
        "limits":["Do not change mount factors while calling relative offsets world-fixed lanes. Keep explicit world centers in plans.","Keeping absolute feet while moving fore/middle mounts inward to.4 reduces reach and fails this1.2–1.3cycle proposal; see scenario4.","Full-tuck raised-pose interpolation and knee sectors need their own new canonical gate after one proposal is chosen.","Visual toe contact, actual authored displacement, footspeed and artistic calmness require native movement video after coherent art."]}
    (OUT/"stride-reach-study.json").write_text(json.dumps(report,indent=2),encoding="utf-8")
    chart=Image.new("RGB",(1200,820),(248,249,251))
    d=ImageDraw.Draw(chart)
    try:
        font=ImageFont.truetype("C:/Windows/Fonts/arial.ttf",18)
        small=ImageFont.truetype("C:/Windows/Fonts/arial.ttf",15)
    except OSError:
        font=ImageFont.load_default();small=font
    d.text((20,15),"Math proposal: cycle1.3 / compactstride.8; foreX.4Y.65,middleXY.4; absolute foot lanes",fill="#20252a",font=font)
    d.text((20,43),"Lines are full stance intervals, not bone/skin contours. No native motion or artistic tempo pass.",fill="#66727d",font=small)
    colors=["#c67800","#a55600","#087fb2","#075b90","#b94cb3","#7b2d8c"]
    for panel,fold in enumerate((0,.5)):
        project=lambda xy:(panel*600+300+xy[0]*260,420-xy[1]*260)
        d.rectangle((panel*600+15,85,panel*600+585,760),outline="#c7d0d8")
        d.text((panel*600+35,100),f'fold{fold:g}: hip dot -> lane center; vertical=stance travel',fill="#20252a",font=small)
        d.line((project((0,-1.1)),project((0,1.1))),fill="#dce1e7",width=1)
        d.line((project((-1.05,0)),project((1.05,0))),fill="#dce1e7",width=1)
        for leg,p in enumerate(x for x in plans if x["fold"]==fold and x["scenario"]=="new_deep_foreX40Y65_middleXY40_relative_lanes"):
            hip,center=project(p["mount_xy"]),project(p["foot_world_center_xy"])
            endpoints=[project(xy) for xy in p["stance_world_endpoints_xy"]]
            d.line((hip,center),fill="#a7afb7",width=1)
            d.line(endpoints,fill=colors[leg],width=4)
            for pp in (hip,center):
                x,y=pp;d.ellipse((x-3,y-3,x+3,y+3),fill=colors[leg])
            d.text((center[0]+5,center[1]-10),p["id"],fill=colors[leg],font=font)
        d.text((panel*600+35,715),"World XY at actor scale1; fore is up; floorcontactonlyinXYplan",fill="#66727d",font=small)
    d.text((20,783),"Critical: if mounts change to.4 while these world centers stay fixed, recompute reach; do not reuse the relative-offset result.",fill="#20252a",font=small)
    chart.save(OUT/"stride-reach-world-lanes-sketch.png")
    print(json.dumps({"results":[{"id":x["id"],"maximum_cycle":x["maximum_cycle_units_all_legs"],"samples":x["cycle_samples"]} for x in results]},indent=2))


if __name__=="__main__":
    main()
