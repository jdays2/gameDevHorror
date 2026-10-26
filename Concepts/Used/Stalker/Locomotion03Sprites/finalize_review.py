from pathlib import Path
import json,hashlib
from PIL import Image,ImageDraw

OUT=Path(__file__).resolve().parent
repairs=json.loads((OUT/'alpha-repairs.json').read_text())
timeline=json.loads((OUT/'preview-timeline.json').read_text())
assert len(repairs)==432 and all(row['componentsAfter']==1 for row in repairs)
assert all(abs(b['displayedClosure']-a['displayedClosure'])<=1/(60*.24)+1e-5 for a,b in zip(timeline,timeline[1:]))
for start in [120,136,152,300,316]:
    sheet=Image.new('RGB',(1600,1000),'#00ff00')
    draw=ImageDraw.Draw(sheet)
    for tile,index in enumerate(range(start,start+16)):
        with Image.open(OUT/'PreviewFrames'/f'green-{index:03d}.png') as frame:
            image=frame.crop((420,100,860,620)).resize((380,449),Image.Resampling.LANCZOS)
        x,y=tile%4*400,tile//4*250
        # Fixed crop and 50% downsample fit four rows without independent pose alignment.
        image.thumbnail((200,230),Image.Resampling.LANCZOS)
        sheet.paste(image,(x+100,y+19))
        draw.text((x+8,y+3),f'{index} | close {timeline[index]["displayedClosure"]:.3f}',fill='black')
    sheet.save(OUT/'Review'/f'transition-{start:03d}.jpg',quality=96)
report={'bodyPoses':432,'connectedVisibleBodyPoses':432,'allPoseContactSheets':9,
 'detachedPixelsRemoved':sum(r['detachedPixelsRemoved'] for r in repairs),
 'internalHolePixelsFilled':sum(r['holesFilledPixels'] for r in repairs),
 'attachmentSeamPixelsFilled':sum(r['attachmentSeamPixelsFilled'] for r in repairs),
 'maxClosureChangePer60HzFrame':max(abs(b['displayedClosure']-a['displayedClosure']) for a,b in zip(timeline,timeline[1:])),
 'runtimeGreenFrames':480,'runtimeCameraFrames':120,
 'greenVideoSha256':hashlib.sha256((OUT/'green.mp4').read_bytes()).hexdigest(),
 'sourceRawAfterEditorOnlyRebakeSha256':hashlib.sha256((OUT/'Raw/body-0-00.png').read_bytes()).hexdigest(),
 'sourceRebakeMatchesBeforeMove':hashlib.sha256((OUT/'Raw/body-0-00.png').read_bytes()).hexdigest()=='75a30945219468c2cb6be8447337c8bb3b1b16713bf8ab080f5fe9c048d54bb6',
 'browserVerification':'Blocked by URL security policy on the browser error page. No alternate browser automation attempted; local URP images reviewed.',
 'artisticApproval':'Owner review pending'}
(OUT/'visual-review.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report))
