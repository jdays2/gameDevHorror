"""Package the Unity capture locally; no frame interpolation or external service."""
from pathlib import Path
import subprocess
from PIL import Image, ImageDraw, ImageFont
import json

ROOT=Path(__file__).resolve().parent
FFMPEG=ROOT.parents[5]/'work/creature-tools/Lib/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'
FONT='C\\:/Windows/Fonts/arial.ttf'
labels=(f"drawtext=fontfile='{FONT}':text='BEFORE':x=w/4-tw/2:y=24:fontsize=26:fontcolor=white,"
        f"drawtext=fontfile='{FONT}':text='AFTER':x=3*w/4-tw/2:y=24:fontsize=26:fontcolor=white")
for prefix,rate in [('close',60),('game',15),('morgue',15),('morgue-lit',15)]:
    subprocess.run([str(FFMPEG),'-hide_banner','-loglevel','error','-y','-framerate',str(rate),
        '-i',str(ROOT/'PreviewFrames'/f'{prefix}-%03d.png'),'-vf',labels,'-c:v','libx264','-crf','19',
        '-pix_fmt','yuv420p','-movflags','+faststart',str(ROOT/f'{prefix}.mp4')],check=True)
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',16)
sheet=Image.new('RGB',(1200,1040),(23,27,31));d=ImageDraw.Draw(sheet)
for j,frame in enumerate(range(0,480,20)):
    im=Image.open(ROOT/'PreviewFrames'/f'close-{frame:03}.png').crop((690,90,1240,680)).resize((200,220))
    x=j%6*200;y=j//6*260
    sheet.paste(im,(x,y+25));d.text((x+5,y+4),f'{frame/60:.2f} s',fill='white',font=font)
sheet.save(ROOT/'cycle-review.jpg',quality=92)
print('Packaged four local videos and 24-pose review.')

# Ordinary image frames make exact stepping inspectable even when an embedded
# browser does not include its hardware-decoded video surface in screenshots.
for prefix,rate in [('close',60),('game',15),('morgue',15),('morgue-lit',15)]:
    folder=ROOT/'Frames'/prefix
    folder.mkdir(parents=True,exist_ok=True)
    for path in sorted((ROOT/'PreviewFrames').glob(prefix+'-[0-9][0-9][0-9].png')):
        number=path.stem.rsplit('-',1)[1]
        Image.open(path).save(folder/(number+'.webp'),quality=90,method=4)
print('Packaged numbered WebP frames for exact local playback.')
