from pathlib import Path
import subprocess
from PIL import Image, ImageDraw

OUT=Path(__file__).resolve().parent
FFMPEG=OUT.parents[5]/'work/creature-tools/Lib/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'
# Workspace and project are nested separately; retain a caller override if moved.
if not FFMPEG.exists():
    FFMPEG=Path('C:/Users/Mark/Documents/Codex/2026-10-13/gkf/work/creature-tools/Lib/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe')
(OUT/'Frames').mkdir(exist_ok=True)
for prefix,fps in [('green',60),('game',15)]:
    subprocess.run([str(FFMPEG),'-y','-v','error','-framerate',str(fps),'-i',str(OUT/'PreviewFrames'/f'{prefix}-%03d.png'),
        '-c:v','libx264','-crf','17','-pix_fmt','yuv420p','-movflags','+faststart',str(OUT/f'{prefix}.mp4')],check=True)
    for frame in sorted((OUT/'PreviewFrames').glob(prefix+'-[0-9][0-9][0-9].png')):
        with Image.open(frame) as image:
            image.save(OUT/'Frames'/(frame.stem+'.webp'),quality=92,method=4)
sheet=Image.new('RGB',(1600,900),'#00ff00')
draw=ImageDraw.Draw(sheet)
for index,frame in enumerate([119,123,127,131,135,243,255,310]):
    image=Image.open(OUT/'PreviewFrames'/f'green-{frame:03d}.png')
    # Close-up centered crop, common camera and body origin for every sample.
    image=image.crop((420,100,860,620)).resize((360,424),Image.Resampling.LANCZOS)
    x,y=(index%4)*400,(index//4)*450
    sheet.paste(image,(x+20,y+22))
    draw.text((x+15,y+4),f'{frame/60:.2f} s | frame {frame}',fill='black')
sheet.save(OUT/'fear-green.jpg',quality=96)
Image.open(OUT/'PreviewFrames/green-132.png').save(OUT/'poster.jpg',quality=95)
print('480 green frames + 120 gameplay frames, videos and fear strip ready')
