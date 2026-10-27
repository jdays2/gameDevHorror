"""Skinless projected volume proposal on the current clean single control.

No source/target RGBA changes. Outlines are authoring proposals, not observed
hidden muscles or physics3D volumes. Knee/hip attachment zones stay fixed.
"""
import json
import numpy as np
from PIL import Image,ImageDraw
from source_ownership_study import HERE,FAMILY,digest
from native_uv_painter import ORIGIN,FLIP,PPU,JOINT_RADII
from qa_parts import FONT,green


def main():
    out=HERE/'UpperEnvelopePlan01'
    if out.exists():
        raise FileExistsError('Envelope draft is frozen')
    out.mkdir()
    folder=HERE/'ClosedMiddleOwnedStudy02'
    record=json.loads((folder/'proof.json').read_text())['cases'][1]
    pose=record['pose']
    image=Image.open(folder/'Raw'/f'{record["name"]}.png').convert('RGBA')
    # Render every panel from the same frozen pixels, at the same224PPU.
    canvas=Image.new('RGB',(1728,700),'#00ff00')
    draw=ImageDraw.Draw(canvas)
    colors=[(255,100,50),(255,190,50),(0,120,255),(0,210,255),(170,40,255),(250,80,220)]
    target_records=[]
    for column,width in enumerate([1.0,1.3,1.5]):
        panel=green(np.asarray(image))
        pd=ImageDraw.Draw(panel)
        for leg in range(6):
            hip,knee=[ORIGIN+FLIP@np.asarray(pose[key][leg][:2])*PPU for key in ['hips','knees']]
            axis=hip-knee
            length=np.linalg.norm(axis)
            axis/=length
            normal=np.array([-axis[1],axis[0]])
            start=JOINT_RADII[leg]+8
            phase=np.linspace(0,1,80)
            axial=start+(length-start)*phase
            # Baseline width is a draft visible-walking-shaft envelope. The
            # endpoint radii are unchanged; this is not a fitted body mask.
            radius=6.5 if leg<2 else 5.5 if leg<4 else 6.0
            profile=1+(width-1)*16*phase*phase*(1-phase)*(1-phase)
            centres=knee+axial[:,None]*axis
            upper=centres+(radius*profile)[:,None]*normal
            lower=centres-(radius*profile)[:,None]*normal
            polygon=np.vstack([upper,lower[::-1]])
            if column:
                pd.line([tuple(p) for p in polygon]+[tuple(polygon[0])],fill=colors[leg],width=1)
                pd.line([tuple(knee),tuple(hip)],fill=colors[leg],width=1)
                pd.ellipse((hip[0]-2,hip[1]-2,hip[0]+2,hip[1]+2),fill=colors[leg])
            if column:
                target_records.append({'leg':leg,'widthMultiplierAtPeak':width,
                    'jointZoneEndDistanceFromKneePixels':start,'hipDistancePixels':float(length),
                    'draftBaseHalfWidthPixels':radius,'draftPeakHalfWidthPixels':radius*width,
                    'envelopePolygonPixels':polygon.tolist(),
                    'attachment':'same knee and hip; zero expansion at protected-zone edge and at hip'})
        canvas.paste(panel,(column*576,110))
        draw.text((column*576+8,8),f'Frozen control / envelope peak x{width:.1f}',fill='black',font=FONT)
        draw.text((column*576+8,32),'Draft outlines only; native skin unchanged',fill='black',font=FONT)
        draw.text((column*576+8,56),'Joint radius, hip and distal blade stay fixed',fill='black',font=FONT)
        draw.text((column*576+8,80),'Not measured3D anatomy / not a skin PASS',fill='black',font=FONT)
    canvas.save(out/'upper-envelope-options-on-frozen-control.png')
    report={'status':'DRAFT projected soft envelope only; no skin/alpha/bone edits or art approval',
        'sourceRawSha256':digest(folder/'Raw'/f'{record["name"]}.png'),
        'sourcePoseSha256':digest(FAMILY/'ClosedMiddleStudy02/poses.json'),
        'profile':'1+(peak-1)*16*t²*(1-t)², t along kneeZoneEnd..hip; endpoints1 with derivative0',
        'functionalGroups':'same six chains and 3 existing functional groups per leg; no new parts',
        'proposal':target_records,
        'implementationConstraint':'Any later material widening must remain a single continuous source UV sample; no hard chart owner or alpha/cap blend. Full-limb silhouette/Jacobian/pair checks must repeat.'}
    (out/'envelope-proposal.json').write_text(json.dumps(report,indent=2))
    print(out/'upper-envelope-options-on-frozen-control.png')


if __name__=='__main__':
    main()
