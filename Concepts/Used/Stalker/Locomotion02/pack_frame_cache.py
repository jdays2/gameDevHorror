"""Rebuild the ignored exact-frame viewer cache from the versioned videos."""
from pathlib import Path
import subprocess
from PIL import Image

ROOT=Path(__file__).resolve().parent
FFMPEG=ROOT.parents[5]/'work/creature-tools/Lib/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'

def rebuild():
    for prefix in ('close','game','morgue','morgue-lit'):
        folder=ROOT/'Frames'/prefix
        folder.mkdir(parents=True,exist_ok=True)
        decoder=subprocess.Popen([str(FFMPEG),'-hide_banner','-loglevel','error','-i',str(ROOT/(prefix+'.mp4')),
            '-f','rawvideo','-pix_fmt','rgb24','-'],stdout=subprocess.PIPE)
        index=0
        while True:
            pixels=decoder.stdout.read(1280*720*3)
            if not pixels:
                break
            Image.frombytes('RGB',(1280,720),pixels).save(folder/f'{index:03d}.webp',quality=90,method=4)
            index+=1
        if decoder.wait()!=0:
            raise RuntimeError('Cannot decode '+prefix)
        print(prefix,index,'frames')

if __name__=='__main__':
    rebuild()
