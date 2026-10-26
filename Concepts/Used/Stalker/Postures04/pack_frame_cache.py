"""Recover the local frame viewer cache from versioned videos."""
from pathlib import Path
import subprocess
from PIL import Image

OUT=Path(__file__).resolve().parent
FFMPEG=OUT.parents[5]/'work/creature-tools/Lib/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'
(OUT/'Frames').mkdir(exist_ok=True)
for name,count in [('green',480),('game',120)]:
    process=subprocess.Popen([str(FFMPEG),'-v','error','-i',str(OUT/f'{name}.mp4'),'-f','rawvideo','-pix_fmt','rgb24','-'],stdout=subprocess.PIPE)
    for index in range(count):
        raw=process.stdout.read(1280*720*3)
        if len(raw)!=1280*720*3:
            raise RuntimeError(f'{name}: missing frame {index}')
        Image.frombytes('RGB',(1280,720),raw).save(OUT/'Frames'/f'{name}-{index:03d}.webp',quality=92,method=4)
    if process.wait()!=0:
        raise RuntimeError('Video decoder failed')
print('Frame cache restored')
