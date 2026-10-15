"""Prepare the selected indoor pistol (#3) and concrete casings (#6), retaining source provenance.
NumPy 2.3.5 + explicit FFmpeg executable; no synthetic oscillators or new paid generation.
"""
from pathlib import Path
import argparse, hashlib, json, subprocess, wave
import numpy as np

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Assets/EmergencyShift/Audio/PistolIndoor'
REPORT=ROOT/'Concepts/Used/Audio/PistolIndoor'
SR=48000

def decode(ff,path,mono=False):
    channels=1 if mono else 2
    return np.frombuffer(subprocess.check_output([ff,'-v','error','-i',str(path),'-f','f32le','-ar',str(SR),'-ac',str(channels),'-']),'<f4').reshape(-1,channels).astype(float)

def eq(x,low_db,high_db):
    n=len(x);f=np.fft.rfftfreq(n,1/SR)
    low=1/(1+(f/280)**2);high=1-1/(1+(f/3200)**2)
    gain=10**((low_db*low+high_db*high)/20)
    hp=(1-np.exp(-(f/45)**4))
    return np.fft.irfft(np.fft.rfft(x,axis=0)*(gain*hp)[:,None],n=n,axis=0)

def edges(x):
    x-=x.mean(axis=0)
    x[:8]*=np.linspace(0,1,8)[:,None]
    x[-720:]*=np.linspace(1,0,720)[:,None]
    x[0]=x[-1]=0
    return x

def write(path,x):
    assert np.isfinite(x).all() and np.max(abs(x))<.95
    q=np.rint(x*8388607).astype(np.int32).reshape(-1)
    raw=np.stack([(q>>s)&255 for s in [0,8,16]],axis=-1).astype(np.uint8).tobytes()
    with wave.open(str(path),'wb')as w:
        w.setnchannels(x.shape[1]);w.setsampwidth(3);w.setframerate(SR);w.writeframes(raw)
    return dict(file=path.name,frames=len(x),rate=SR,channels=x.shape[1],bits=24,peak=float(np.max(abs(x))),rms=float(np.sqrt(np.mean(x*x))),sha256=hashlib.sha256(path.read_bytes()).hexdigest())

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--ffmpeg',required=True);p.add_argument('--force',action='store_true');args=p.parse_args()
    OUT.mkdir(parents=True,exist_ok=True);REPORT.mkdir(parents=True,exist_ok=True)
    if (OUT/'pistol_indoor_01.wav').exists()and not args.force:raise SystemExit('Use --force for intentional replacement.')
    sources=ROOT/'Docs/Audio/WeaponRealism02/Sources'
    metadata=json.loads((sources/'sources.json').read_text(encoding='utf-8'))
    selected=[s for s in metadata if s['id']in ['pistolIndoor','brass']]
    for s in selected:assert hashlib.sha256((sources/s['file']).read_bytes()).hexdigest()==s['sha256']
    raw=decode(args.ffmpeg,sources/'pistolIndoor.mp3')[round(.005*SR):round(.585*SR)]
    t=np.arange(len(raw))/SR
    # Leave the original attack intact, shorten late tiled-room reflections for the game mix.
    attack_end=.065;late=np.maximum(0,t-attack_end)
    shots=[];entries=[]
    for i,(lo,hi,decay)in enumerate([(.3,-1.1,.28),(.0,-.7,.30),(.5,-1.4,.27),(-.25,-.8,.32),(.2,-1.2,.29),(.45,-.6,.31)],1):
        x=eq(raw,lo,hi)*np.exp(-late/decay)[:,None]
        x=edges(x);x*=.70/np.max(abs(x));shots.append(x)
        item=write(OUT/f'pistol_indoor_{i:02}.wav',x);item.update(source='pistolIndoor',window=[.005,.585],lowDb=lo,highDb=hi,lateDecay=decay);entries.append(item)
    raw=decode(args.ffmpeg,sources/'brass.mp3',True)
    windows=[(.02,1.38),(2.30,3.65),(4.06,5.35),(5.41,6.90),(7.58,9.00),(9.90,11.65)]
    casings=[]
    for start,end in windows:
        x=eq(raw[round(start*SR):round(end*SR)].copy(),-.5,-1.5);x=edges(x);casings.append(x)
    gain=.55/max(np.max(abs(x))for x in casings)
    for i,((start,end),x)in enumerate(zip(windows,casings),1):
        item=write(OUT/f'casing_concrete_{i:02}.wav',x*gain);item.update(source='brass',window=[start,end],gain=gain);entries.append(item)
    # Keep the earliest 60 ms nearly identical. These are subtle edits of one recording, not six recorded takes.
    correlations=np.corrcoef(np.array([x[:2880].reshape(-1)for x in shots]))
    report=dict(selected=selected,clips=entries,shotVariants='Six close EQ/late-tail edits of the same selected recording. Runtime pitch +/-0.8%, gain +/-0.4dB. Not six independently recorded gunshots.',
                casingVariants='Six separate recorded concrete drops; clip already contains small bounces, play once at first landing.',
                sourceQuality='Public MP3 previews retained from audition; PCM24 export does not restore lossless quality.',attackMinCorrelation=float(correlations[np.triu_indices(6,1)].min()),
                processing='DC/high-pass45Hz, restrained broad EQ, late-reflection envelope, edge fades and linear gain. No oscillator/extra reverb/compressor.',numpy=np.__version__)
    (REPORT/'manifest.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(dict(clips=len(entries),attackMinCorrelation=report['attackMinCorrelation'])))

if __name__=='__main__':main()
