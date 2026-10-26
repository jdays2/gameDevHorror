"""Decode every real video frame into small, numbered review sheets."""
from pathlib import Path
import subprocess
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent
FFMPEG = ROOT.parents[6]/'work/creature-tools/Lib/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'
font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 16)

def sheets(video, prefix):
    stream = subprocess.Popen([str(FFMPEG), '-hide_banner', '-loglevel', 'error', '-i', str(video),
        '-vf', 'crop=540:600:700:70,scale=180:200', '-f', 'rawvideo', '-pix_fmt', 'rgb24', '-'], stdout=subprocess.PIPE)
    frame = 0
    while True:
        sheet = Image.new('RGB', (1080, 920), (22, 27, 31))
        draw = ImageDraw.Draw(sheet)
        found = False
        for cell in range(24):
            pixels = stream.stdout.read(180*200*3)
            if not pixels:
                break
            found = True
            x, y = cell%6*180, cell//6*230
            sheet.paste(Image.frombytes('RGB', (180, 200), pixels), (x, y+28))
            draw.text((x+5,y+4), f'#{frame:03d}  {frame/60:.2f}s', font=font, fill='white')
            frame += 1
        if not found:
            break
        sheet.save(ROOT/f'{prefix}-{(frame-1)//24:02d}.jpg', quality=94)
    assert stream.wait() == 0
    print(prefix, frame, 'real frames')

if __name__ == '__main__':
    sheets(ROOT.parent/'close.mp4', 'after')
