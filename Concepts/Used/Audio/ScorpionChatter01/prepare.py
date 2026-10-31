"""Convert three original short vocalizations. No pitch/EQ/layering in source processing."""
from pathlib import Path
import argparse, hashlib, json, subprocess, wave
import numpy as np

ROOT=Path(__file__).resolve().parent
PROJECT=ROOT.parents[3]

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--ffmpeg',required=True)
    args=parser.parse_args()
    result=[]
    for index in range(1,4):
        source=ROOT/f'Originals/squeak-{index}.mp3'
        raw=subprocess.run([args.ffmpeg,'-v','error','-i',str(source),'-ac','1','-ar','32000','-f','f32le','-'],capture_output=True,check=True).stdout
        audio=np.frombuffer(raw,dtype='<f4').copy()
        gain=min(1,.708/max(.000001,float(np.abs(audio).max())))
        audio*=gain
        audio[:64]*=np.linspace(0,1,64)
        audio[-480:]*=np.linspace(1,0,480)
        output=PROJECT/f'Assets/EmergencyShift/Audio/Enemies/Scorpion/Squeak{index}.wav'
        with wave.open(str(output),'wb') as wav:
            wav.setparams((1,2,32000,0,'NONE','not compressed'))
            wav.writeframes(np.round(audio*32767).astype('<i2').tobytes())
        result.append(dict(source=source.relative_to(PROJECT).as_posix(),output=output.relative_to(PROJECT).as_posix(),seconds=len(audio)/32000,
                           gain=gain,peak=float(np.abs(audio).max()),originalSha256=hashlib.sha256(source.read_bytes()).hexdigest(),
                           outputSha256=hashlib.sha256(output.read_bytes()).hexdigest()))
    (ROOT/'processing.json').write_text(json.dumps(dict(clips=result,mono=True,pcmBits=16,sampleRate=32000,pitch=False,equalization=False,layers=False),indent=2)+'\n')
    print(json.dumps(result))

if __name__=='__main__':
    main()
