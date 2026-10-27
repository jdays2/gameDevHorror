"""Independent real-PNG QA for a parts export, without joint exclusion spheres."""
from pathlib import Path
import argparse
import hashlib
import json
import re
import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE=Path(__file__).resolve().parent
FONT=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',16)
COLORS=np.array([[1,.25,.25],[1,.7,.7],[.2,.8,1],[.2,.3,1],[1,1,.1],[1,.6,.1]],dtype='float32')


def components(binary):
    """Eight-connected run-length union-find; retain exact full resolution."""
    parents=[]
    areas=[]
    bounds=[]
    previous=[]
    def find(index):
        while parents[index]!=index:
            parents[index]=parents[parents[index]]
            index=parents[index]
        return index
    def union(a,b):
        a,b=find(a),find(b)
        if a==b:
            return
        if areas[a]<areas[b]:
            a,b=b,a
        parents[b]=a
        areas[a]+=areas[b]
        bounds[a]=[min(bounds[a][0],bounds[b][0]),min(bounds[a][1],bounds[b][1]),
            max(bounds[a][2],bounds[b][2]),max(bounds[a][3],bounds[b][3])]
    for y,row in enumerate(binary):
        edge=np.diff(np.r_[False,row,False].astype('int8'))
        starts=np.flatnonzero(edge==1)
        ends=np.flatnonzero(edge==-1)
        current=[]
        cursor=0
        for left,right in zip(starts,ends):
            index=len(parents)
            parents.append(index)
            areas.append(int(right-left))
            bounds.append([int(left),y,int(right),y+1])
            while cursor<len(previous) and previous[cursor][1]<left:
                cursor+=1
            j=cursor
            while j<len(previous) and previous[j][0]<=right:
                union(index,previous[j][2])
                j+=1
            current.append((left,right,index))
        previous=current
    roots={find(index) for index in range(len(parents))}
    return sorted([{'pixels':areas[index],'bbox':bounds[index]} for index in roots],key=lambda item:-item['pixels'])


def load_mask(path):
    return np.asarray(Image.open(path).convert('RGBA'))


def bounded_holes(opaque):
    """Transparent components enclosed by this limb, never whole-body gaps.

    Crop with a transparent margin, then reject every inverse component that
    reaches the outer margin. Eight-connected background keeps diagonal exits
    open rather than reporting normal antialiased edge notches as flesh holes.
    """
    y,x=np.nonzero(opaque)
    if not len(x):
        return []
    x0,y0=max(0,int(x.min())-1),max(0,int(y.min())-1)
    x1,y1=min(opaque.shape[1],int(x.max())+2),min(opaque.shape[0],int(y.max())+2)
    inverse=~opaque[y0:y1,x0:x1]
    holes=[]
    for part in components(inverse):
        left,top,right,bottom=part['bbox']
        if left==0 or top==0 or right==inverse.shape[1] or bottom==inverse.shape[0]:
            continue
        holes.append({'pixels':part['pixels'],'bbox':[left+x0,top+y0,right+x0,bottom+y0]})
    return holes


def torso_path(folder,name):
    specific=folder/'Masks'/f'torso-{name}.png'
    return specific if specific.exists() else folder/'Masks/torso.png'


def green(rgba):
    a=rgba[...,3:4].astype('float32')/255
    rgb=rgba[...,:3].astype('float32')*a+np.array([0,255,0],dtype='float32')*(1-a)
    return Image.fromarray(np.clip(rgb,0,255).round().astype('uint8'))


def mark_pair(folder,record,out):
    name,stem=record['name'],record['maskStem']
    a,b=record['pair']
    masks=[load_mask(folder/'Masks'/f'leg-{stem}-{leg}.png') for leg in range(6)]
    torso=load_mask(torso_path(folder,name))
    whole=load_mask(folder/'Raw'/f'{name}.png')
    overlap=(masks[a][...,3]>128)&(masks[b][...,3]>128)&(torso[...,3]<=128)
    image=green(whole)
    marked=np.asarray(image).copy()
    marked[overlap]=(255,30,20)
    panel=Image.new('RGB',(1152,646),(0,255,0))
    panel.paste(Image.fromarray(marked),(0,70))
    colored=np.zeros((576,576,4),dtype='float32')
    for leg in [a,b]:
        alpha=masks[leg][...,3:4].astype('float32')/255
        color=np.concatenate([np.broadcast_to(COLORS[leg],(576,576,3))*alpha,alpha],axis=-1)
        colored=color+colored*(1-alpha)
    alpha=torso[...,3:4].astype('float32')/255
    body=np.concatenate([np.broadcast_to(.65,(576,576,3))*alpha,alpha],axis=-1)
    colored=body+colored*(1-alpha)
    rgb=colored[...,:3]+np.array([0,1,0])*(1-colored[...,3:4])
    rgb=(rgb*255).round().astype('uint8')
    rgb[overlap]=(255,30,20)
    panel.paste(Image.fromarray(rgb),(576,70))
    draw=ImageDraw.Draw(panel)
    draw.text((10,8),f'{name} | pair {a}/{b} | {record["pixelsOutsideTorso"]} opaque overlap pixels',fill='black',font=FONT)
    draw.text((10,33),'Red = actual alpha intersection outside actual torso; no hip-circle exemption',fill='black',font=FONT)
    panel.save(out/f'overlap-{name}-{a}-{b}.png')


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('folder',type=Path)
    parser.add_argument('--out',type=Path,default=HERE/'Preview16Audit')
    parser.add_argument('--expected-frames',type=int)
    parser.add_argument('--name-prefix')
    parser.add_argument('--contacts',action='store_true')
    args=parser.parse_args()
    folder=args.folder.resolve()
    out=args.out.resolve()
    out.mkdir(parents=True,exist_ok=True)
    pairs=[]
    frames=[]
    holes=[]
    digest=hashlib.sha256()
    torso_digest=hashlib.sha256()
    whole_digest=hashlib.sha256()
    keys=[]
    for path in sorted((folder/'Masks').glob('leg-*-0.png')):
        match=re.fullmatch(r'leg-(.+)-0.png',path.name)
        if match:
            stem=match.group(1)
            body_match=re.fullmatch(r'(\d+)-(\d+)',stem)
            if body_match:
                row,phase=map(int,body_match.groups())
                keys.append({'name':f'body-{row}-{phase:02d}','maskStem':stem,'row':row,'phase':phase})
            else:
                keys.append({'name':stem,'maskStem':stem,'row':None,'phase':None})
    if args.name_prefix:
        keys=[key for key in keys if key['name'].startswith(args.name_prefix)]
    if args.expected_frames is not None and len(keys)!=args.expected_frames:
        raise ValueError(f'Incomplete frame set: expected {args.expected_frames}, found {len(keys)}')
    for key in keys:
        name,stem=key['name'],key['maskStem']
        actual_torso_path=torso_path(folder,name)
        torso_digest.update(actual_torso_path.name.encode())
        torso_digest.update(actual_torso_path.read_bytes())
        torso=load_mask(actual_torso_path)[...,3]
        alphas=[]
        for leg in range(6):
            path=folder/'Masks'/f'leg-{stem}-{leg}.png'
            digest.update(path.name.encode())
            digest.update(path.read_bytes())
            alphas.append(load_mask(path)[...,3])
        frame_holes=[]
        for leg,alpha in enumerate(alphas):
            for threshold in [4,128]:
                enclosed=bounded_holes(alpha>threshold)
                if enclosed:
                    record={**key,'leg':leg,'alphaOver':threshold,'count':len(enclosed),
                        'pixels':sum(hole['pixels'] for hole in enclosed),
                        'largestPixels':enclosed[0]['pixels'],'largestBbox':enclosed[0]['bbox'],
                        'holes':enclosed}
                    holes.append(record)
                    frame_holes.append(record)
        frame_pairs=[]
        for a in range(6):
            for b in range(a+1,6):
                overlap=(alphas[a]>128)&(alphas[b]>128)&(torso<=128)
                if overlap.any():
                    y,x=np.nonzero(overlap)
                    record={**key,'pair':[a,b],'pixelsOutsideTorso':int(overlap.sum()),
                        'bbox':[int(x.min()),int(y.min()),int(x.max()+1),int(y.max()+1)]}
                    pairs.append(record)
                    frame_pairs.append(record)
        whole_path=folder/'Raw'/f'{name}.png'
        whole_digest.update(whole_path.name.encode())
        whole_digest.update(whole_path.read_bytes())
        whole=load_mask(whole_path)
        alpha=whole[...,3]
        parts=components(alpha>4)
        frame={**key,'torsoMask':actual_torso_path.name,'pairs':frame_pairs,'perLegBoundedHoles':frame_holes,'componentsAlphaOver4':parts,
            'detachedPixelsAlphaOver4':sum(part['pixels'] for part in parts[1:]),
            'borderPixelsAlphaOver1':int((alpha[0]>1).sum()+(alpha[-1]>1).sum()+(alpha[:,0]>1).sum()+(alpha[:,-1]>1).sum())}
        frames.append(frame)
    pairs.sort(key=lambda item:-item['pixelsOutsideTorso'])
    for record in pairs[:8]:
        mark_pair(folder,record,out)
    report={'source':str(folder),'masksSha256':digest.hexdigest(),
        'torsoMasksSha256':torso_digest.hexdigest(),'wholeFramesSha256':whole_digest.hexdigest(),'frames':len(frames),
        'comparison':'alpha >128 for both limbs; exclude only actual torso alpha >128; no spheres and no blanket tolerances',
        'maxPairPixels':max([pair['pixelsOutsideTorso'] for pair in pairs],default=0),
        'pairCandidates':pairs,'frameChecks':frames,
        'perLegBoundedHoles':holes,
        'perLegBoundedHolePixelsAlphaOver4':sum(record['pixels'] for record in holes if record['alphaOver']==4),
        'perLegBoundedHolePixelsAlphaOver128':sum(record['pixels'] for record in holes if record['alphaOver']==128),
        'largestBoundedHolePixels':max([record['largestPixels'] for record in holes],default=0),
        'holeInterpretation':'Each limb separately: inverse eight-connected flood fill excluding the crop outer component. Whole-body loops between torso and bent limbs are intentionally not filled.',
        'detachedPixelTotalAlphaOver4':sum(frame['detachedPixelsAlphaOver4'] for frame in frames),
        'borderPixelTotalAlphaOver1':sum(frame['borderPixelsAlphaOver1'] for frame in frames),
        'interpretation':'Large distal/other-chain intersections are failures. Tiny mount contacts require actual context and are retained in this report, not silently excused. Component connectivity does not prove artistic quality.'}
    (out/'qa.json').write_text(json.dumps(report,indent=2))
    if args.contacts:
        for row in sorted({key['row'] for key in keys if key['row'] is not None}):
            sheet=Image.new('RGB',(1600,1320),(0,255,0))
            draw=ImageDraw.Draw(sheet)
            for key in [key for key in keys if key['row']==row]:
                phase=key['phase']
                whole=load_mask(folder/'Raw'/f'{key["name"]}.png')
                image=green(whole).resize((192,192),Image.Resampling.LANCZOS)
                x,y=phase%8*200,phase//8*220
                sheet.paste(image,(x+4,y+24))
                draw.text((x+4,y+3),f'fold {row}/8 | phase {phase:02d}',fill='black',font=FONT)
            sheet.save(out/f'body-contact-{row}.jpg',quality=96)
    print(json.dumps({key:value for key,value in report.items() if key not in ['pairCandidates','frameChecks','perLegBoundedHoles']},indent=2))


if __name__=='__main__':
    main()
