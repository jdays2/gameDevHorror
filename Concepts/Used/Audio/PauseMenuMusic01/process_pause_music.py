"""Prepare Below the Ward 02 for a sample-accurate pause-menu music loop.

Runtime libraries are supplied explicitly, never copied into the content family.
Example: python process_pause_music.py --python-lib <librosa/scipy> --python-lib
<soundfile/imageio_ffmpeg> --analyze-only
"""
from pathlib import Path
import argparse
import hashlib
import json
import subprocess
import sys

ROOT = Path(__file__).resolve().parent
PROJECT = ROOT.parents[3]
parser = argparse.ArgumentParser()
parser.add_argument('--python-lib', action='append', default=[])
parser.add_argument('--ffmpeg')
parser.add_argument('--analyze-only', action='store_true')
args = parser.parse_args()
for library in args.python_lib:
    sys.path.insert(0, library)
import numpy as np
import librosa
from scipy import signal
import soundfile as sf

SOURCE = PROJECT / 'Concepts/Unused/Audio/MenuThemes02/Originals/below_the_ward_02.mp3'
if args.ffmpeg:
    FFMPEG = args.ffmpeg
else:
    import imageio_ffmpeg
    FFMPEG = imageio_ffmpeg.get_ffmpeg_exe()
SR = 48000
raw = subprocess.check_output([FFMPEG, '-v','error','-i',str(SOURCE),'-f','f32le','-ac','2','-ar',str(SR),'-'])
audio = np.frombuffer(raw, dtype='<f4').reshape(-1,2).copy()

def db(value):
    return float(20*np.log10(max(float(value),1e-12)))

def analyze():
    rate = 12000
    hop = 120
    y = signal.resample_poly(audio.mean(axis=1),1,4)
    spectrum = librosa.stft(y,n_fft=1024,hop_length=hop)
    harmonic, percussive = librosa.decompose.hpss(spectrum)
    power = np.abs(spectrum)**2
    times = librosa.frames_to_time(np.arange(spectrum.shape[1]),sr=rate,hop_length=hop)
    perc = librosa.istft(percussive,hop_length=hop,length=len(y))
    onset = librosa.onset.onset_strength(y=perc,sr=rate,hop_length=hop,n_fft=1024)
    chroma = librosa.feature.chroma_stft(S=np.abs(harmonic)**2,sr=rate,n_fft=1024,hop_length=hop)
    mfcc = librosa.feature.mfcc(S=librosa.power_to_db(power),sr=rate,n_mfcc=8)
    rms = librosa.feature.rms(S=np.abs(spectrum),frame_length=1024,hop_length=hop)[0]
    sections=[]
    for start in np.arange(0,len(y)/rate,5):
        mask=(times>=start)&(times<start+5)
        sections.append({'start_s':float(start),'rms_dbfs':round(db(np.sqrt(np.mean(rms[mask]**2))),2),
            'percussive_power_share':round(float(np.sum(np.abs(percussive[:,mask])**2)/np.sum(power[:,mask])),3),
            'onset_mean':round(float(onset[mask].mean()),3),
            'chroma_top':np.argsort(chroma[:,mask].mean(axis=1))[::-1][:4].tolist()})
    tempos=[]
    for begin,end in [(20,160),(35,110),(70,120),(110,165)]:
        env=onset[int(begin*rate/hop):int(end*rate/hop)]
        ac=librosa.autocorrelate(env-env.mean(),max_size=300)
        peaks,_=signal.find_peaks(ac)
        peaks=[p for p in peaks if 50<=60*rate/hop/p<=160]
        candidates=[]
        for p in sorted(peaks,key=lambda p:ac[p],reverse=True)[:8]:
            d=ac[p-1]-2*ac[p]+ac[p+1]
            offset=.5*(ac[p-1]-ac[p+1])/d if d else 0
            candidates.append({'bpm':round(float(60*rate/hop/(p+offset)),3),'ac_relative':round(float(ac[p]/ac[0]),4)})
        tempos.append({'range_s':[begin,end],'candidates':candidates})
    feature=np.vstack([chroma,mfcc])
    feature=(feature-feature.mean(axis=1,keepdims=True))/(feature.std(axis=1,keepdims=True)+1e-8)
    changes=[]
    for t in np.arange(8,len(y)/rate-8,1):
        a=feature[:,(times>=t-5)&(times<t)].mean(axis=1)
        b=feature[:,(times>=t)&(times<t+5)].mean(axis=1)
        changes.append((float(np.linalg.norm(a-b)),float(t)))
    selected=[]
    for value,t in sorted(changes,reverse=True):
        if all(abs(t-old['time_s'])>6 for old in selected):
            selected.append({'time_s':t,'feature_change':round(value,3)})
        if len(selected)>=12:break
    report={'source':str(SOURCE.relative_to(PROJECT)).replace('\\','/'),'sha256':hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
            'duration_s':len(audio)/SR,'decoded_sample_peak_dbfs':db(np.max(np.abs(audio))),
            'tempo_candidates':tempos,'five_second_windows':sections,'signal_change_candidates':sorted(selected,key=lambda x:x['time_s']),
            'limitations':'Signal structure and autocorrelation only, no direct auditory assessment.'}
    (ROOT/'structure_analysis.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    np.savez(ROOT/'structure_features.npz',times=times,onset=onset,chroma=chroma,mfcc=mfcc)
    print(json.dumps(report,ensure_ascii=False),flush=True)

if args.analyze_only:
    analyze()
