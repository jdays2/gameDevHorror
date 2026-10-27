"""Observed 2D source pose annotation; no anatomy approval and no art edit.

Source crop pixels are unchanged beneath separate diagnostic overlays.
Only this review folder is written. Hidden anatomy remains explicitly uncertain.
"""
from pathlib import Path
import json
import math
from PIL import Image, ImageDraw

OUT=Path(__file__).resolve().parent
CHAINS=[
    {"id":"LF","hip":[150,60],"knee":[43,88],"tip":[148,8],"hip_confidence":"inferred under overlapping skin","knee_confidence":"visible outer round skin mass, not measured bone center","tip_confidence":"visible pointed bone endpoint","proximal_envelope_width_px":28},
    {"id":"RF","hip":[198,60],"knee":[303,88],"tip":[204,8],"hip_confidence":"inferred under overlapping skin","knee_confidence":"visible outer round skin mass, not measured bone center","tip_confidence":"visible pointed bone endpoint","proximal_envelope_width_px":28},
    {"id":"LM","hip":[112,127],"knee":[32,157],"tip":None,"visible_distal_termination":[106,112],"hip_confidence":"inferred under torso; low confidence","knee_confidence":"visible outer round skin mass, not measured bone center","tip_confidence":"true tip is occluded by torso; the marked square is visible bone termination at occlusion, not proof of anatomical endpoint","proximal_envelope_width_px":25},
    {"id":"RM","hip":[236,127],"knee":[314,157],"tip":None,"visible_distal_termination":[242,112],"hip_confidence":"inferred under torso; low confidence","knee_confidence":"visible outer round skin mass, not measured bone center","tip_confidence":"true tip is occluded by torso; the marked square is visible bone termination at occlusion, not proof of anatomical endpoint","proximal_envelope_width_px":25},
    {"id":"LR","hip":[117,220],"knee":[44,225],"tip":[151,300],"hip_confidence":"inferred under lower torso; low confidence. Extra lower rounded skin folds cannot be assigned to a hidden joint from this image.","knee_confidence":"visible outer round skin mass, not measured bone center","tip_confidence":"visible pointed bone endpoint","proximal_envelope_width_px":28},
    {"id":"RR","hip":[230,220],"knee":[301,225],"tip":[195,300],"hip_confidence":"inferred under lower torso; low confidence. Extra lower rounded skin folds cannot be assigned to a hidden joint from this image.","knee_confidence":"visible outer round skin mass, not measured bone center","tip_confidence":"visible pointed bone endpoint","proximal_envelope_width_px":28},
]
SOFT_ENVELOPES={
    "LF":[[148,53],[138,77],[113,100],[78,110],[45,110],[36,99],[54,80],[92,56],[124,43]],
    "RF":[[199,53],[209,77],[234,100],[269,110],[302,110],[311,99],[293,80],[255,56],[223,43]],
    "LM":[[106,127],[104,145],[83,162],[62,178],[37,179],[21,163],[29,139],[55,128],[88,123]],
    "RM":[[241,127],[243,145],[264,162],[285,178],[310,179],[326,163],[318,139],[292,128],[259,123]],
    "LR":[[124,210],[116,230],[92,244],[71,252],[42,246],[28,232],[27,216],[44,202],[64,203],[85,211]],
    "RR":[[223,210],[231,230],[255,244],[276,252],[305,246],[319,232],[320,216],[303,202],[283,203],[262,211]],
}


def dashed(draw,a,b,color,width=1,dash=5):
    length=math.dist(a,b)
    for start in range(0,math.ceil(length),dash*2):
        t0,t1=start/length,min(start+dash,length)/length
        p0=tuple(a[i]+(b[i]-a[i])*t0 for i in range(2))
        p1=tuple(a[i]+(b[i]-a[i])*t1 for i in range(2))
        draw.line((p0,p1),fill=color,width=width)


def main():
    source=Image.open(OUT/"closed-control-native.png").convert("RGBA")
    base=Image.new("RGBA",source.size,(0,196,0,255))
    base.alpha_composite(source)
    bones=base.copy()
    draw=ImageDraw.Draw(bones)
    for c in CHAINS:
        h,k,t=c["hip"],c["knee"],c.get("tip") or c["visible_distal_termination"]
        dashed(draw,h,k,(255,225,0,255),1)
        draw.line((tuple(k),tuple(t)),fill=(0,255,255,255),width=1)
        for p,color in ((h,(255,225,0,255)),(k,(0,255,255,255))):
            x,y=p
            draw.ellipse((x-3,y-3,x+3,y+3),outline=color,width=1)
        x,y=t
        if c["tip"] is None:
            draw.rectangle((x-3,y-3,x+3,y+3),outline=(255,70,255,255),width=1)
        else:
            draw.ellipse((x-3,y-3,x+3,y+3),outline=(255,70,255,255),width=1)
        draw.text((k[0]+(6 if k[0]<174 else -22),k[1]-17),c["id"],fill=(255,255,0,255))
    bones.convert("RGB").save(OUT/"native-Recoil03-observed-chains.png")
    envelope=Image.new("RGBA",source.size,(0,0,0,0))
    ed=ImageDraw.Draw(envelope)
    for index,(label,points) in enumerate(SOFT_ENVELOPES.items()):
        color=(255,185,0,46) if index%2==0 else (0,190,255,46)
        ed.polygon([tuple(p) for p in points],fill=color)
        ed.line([tuple(p) for p in points]+[tuple(points[0])],fill=(color[0],color[1],color[2],180),width=1)
        c=CHAINS[index]
        ed.text((c["knee"][0]+(5 if c["knee"][0]<174 else -22),c["knee"][1]-5),label,fill=(255,255,255,255))
    masses=base.copy()
    masses.alpha_composite(envelope)
    md=ImageDraw.Draw(masses)
    # Unassigned lower fleshy loops are deliberately not named as extra muscles.
    for center in ((133,278),(212,278)):
        x,y=center
        md.ellipse((x-18,y-25,x+18,y+25),outline=(255,60,255,255),width=1)
        md.text((x-2,y-5),"?",fill=(255,60,255,255))
    masses.convert("RGB").save(OUT/"native-Recoil03-soft-envelope-observations.png")
    result={"source":"closed-control-native.png","coordinates":"exact crop pixels; top-left origin, x right, y down; anatomical front is up","purpose":"A 2D observational authoring reference, not an independent skeleton or a new anatomical acceptance", "uncertainty":"Manual points +/-3px; all hips and bone centers are inferential. Middle true tips are hidden, so tip=null. Native lower round skin folds remain unassigned; their appearance is not proof of an extra limb, muscle or bone. No true 3D lengths/heights/contacts can be confirmed from one PNG.", "chains":CHAINS,
        "soft_envelopes":{"status":"Separate rough projected soft-skin mass observations, not numbered internal muscles and not a passed muscle-authoring stage. Occluded parts of a polygon are inferential. Distal claw tissue overlaps these envelopes in projection, particularly LM/RM and LR/RR.","polygons_xy_down":SOFT_ENVELOPES,"unassigned_lower_skin_fold_centers_xy_down":[[133,278],[212,278]]}}
    for c in result["chains"]:
        end=c["tip"] or c["visible_distal_termination"]
        c["projected_distal_delta_xy_down"]=[end[i]-c["knee"][i] for i in range(2)]
        c["projected_distal_delta_xy_up"]=[end[0]-c["knee"][0],c["knee"][1]-end[1]]
        c["projected_lengths_px"]={"hip_to_knee":math.dist(c["hip"],c["knee"]),"knee_to_visible_distal_end":math.dist(c["knee"],end)}
    (OUT/"native-Recoil03-observed-chains.json").write_text(json.dumps(result,indent=2),encoding="utf-8")


if __name__=="__main__":
    main()
