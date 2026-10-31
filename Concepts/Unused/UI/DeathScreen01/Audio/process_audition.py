"""Prepare a SMALL audition, not production assets. No Unity writes.

python process_audition.py --analysis-root <outputs/audio-reference-analysis>
"""
from pathlib import Path
import argparse
import hashlib
import json
import subprocess
import sys

parser = argparse.ArgumentParser()
parser.add_argument('--analysis-root', required=True, type=Path)
args = parser.parse_args()
analysis_root = args.analysis_root.resolve()
sys.path[:0] = [str(analysis_root / 'runtime'), str(analysis_root / 'inference-runtime')]
import numpy as np
import scipy.signal as signal
import librosa
import soundfile as sf
import imageio_ffmpeg

root = Path(__file__).resolve().parent
sr = 48000
ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
source = root / 'Originals/last_ward_02.mp3'
raw = subprocess.check_output([ffmpeg, '-v', 'error', '-i', str(source), '-f', 'f32le', '-ac', '2', '-ar', str(sr), '-'])
x = np.frombuffer(raw, dtype='<f4').reshape(-1, 2).astype(np.float64)
y = librosa.resample(x.mean(axis=1).astype(np.float32), orig_sr=sr, target_sr=12000)
onset = librosa.onset.onset_strength(y=y, sr=12000, hop_length=120)
ac = librosa.autocorrelate(onset)
lag = np.arange(80, 121)[np.argmax(ac[80:121])]
beat_seconds = float(lag/100)
length = round(16 * beat_seconds * sr)
chroma = librosa.feature.chroma_stft(y=y, sr=12000, hop_length=120)
energy = librosa.feature.rms(y=y, hop_length=120)[0]
def boundary(t):
    i = round(t*100)
    return chroma[:, max(0,i-40):i+40].mean(axis=1), float(energy[max(0,i-40):i+40].mean())
candidates = []
for t in np.arange(4.0, min(10, len(x)/sr-length/sr-1), .01):
    a, ea = boundary(t)
    b, eb = boundary(t+length/sr)
    sim = float(a@b/(np.linalg.norm(a)*np.linalg.norm(b)+1e-12))
    level = abs(20*np.log10((ea+1e-9)/(eb+1e-9)))
    candidates.append({'start_seconds': round(float(t),2), 'length_seconds': length/sr,
        'chroma_similarity': sim, 'boundary_rms_difference_db': level, 'score': sim-.025*level})
best = max(candidates, key=lambda c:c['score'])
start = round(best['start_seconds']*sr)
overlap = round(.3*sr)
blend = round(.6*sr)
intro = x[:start+overlap].copy()
loop = x[start:start+length].copy()
theta = np.linspace(0,np.pi/2,blend)[:,None]
loop[-blend:] = loop[-blend:]*np.cos(theta)+x[start-blend:start]*np.sin(theta)
def true_peak(a):
    return float(np.max(np.abs(signal.resample_poly(a,4,1,axis=0))))
peak = max(true_peak(intro),true_peak(loop))
gain_db = -3 -20*np.log10(peak)
gain = 10**(gain_db/20)
intro *= gain
loop *= gain
sf.write(root/'Exports/LastWard02_Intro.wav',intro,sr,subtype='PCM_24')
sf.write(root/'Exports/LastWard02_Loop.wav',loop,sr,subtype='PCM_24')
intro,_ = sf.read(root/'Exports/LastWard02_Intro.wav')
loop,_ = sf.read(root/'Exports/LastWard02_Loop.wav')
full = np.concatenate([intro[:-overlap],loop,loop,loop])
fadein = round(1.4*sr)
fadeout = round(.8*sr)
full[:fadein] *= (np.sin(np.linspace(0,np.pi/2,fadein))**2)[:,None]
full[-fadeout:] *= (np.cos(np.linspace(0,np.pi/2,fadeout))**2)[:,None]
sf.write(root/'Previews/last_ward_intro_3loops.wav',full,sr,subtype='PCM_24')
sf.write(root/'Previews/last_ward_loop_seam.wav',np.concatenate([loop[-2*sr:],loop[:2*sr]]),sr,subtype='PCM_24')
sf.write(root/'Previews/last_ward_intro_join.wav',np.concatenate([intro[-overlap-2*sr:-overlap],loop[:2*sr]]),sr,subtype='PCM_24')
for stem in ['last_ward_intro_3loops','last_ward_loop_seam','last_ward_intro_join']:
    subprocess.run([ffmpeg,'-v','error','-y','-i',str(root/f'Previews/{stem}.wav'),'-codec:a','libmp3lame','-b:a','192k',str(root/f'Previews/{stem}.mp3')],check=True)
# Level-matched comparison: approved opening, 1s gap, candidate opening, 1s gap, candidate bed.
reference = root.parents[4]/'Concepts/Unused/Audio/MenuThemes02/Originals/below_the_ward_02.mp3'
rraw = subprocess.check_output([ffmpeg,'-v','error','-i',str(reference),'-f','f32le','-ac','2','-ar',str(sr),'-'])
ref = np.frombuffer(rraw,dtype='<f4').reshape(-1,2).astype(np.float64)
parts = [ref[:10*sr],x[:10*sr],x[10*sr:20*sr]]
matched = []
for p in parts:
    p = p.copy()*10**((-23-20*np.log10(np.sqrt(np.mean(p**2))))/20)
    edge = round(.08*sr)
    p[:edge] *= np.linspace(0,1,edge)[:,None]
    p[-edge:] *= np.linspace(1,0,edge)[:,None]
    matched.extend([p,np.zeros((sr,2))])
sf.write(root/'Previews/style_comparison.wav',np.concatenate(matched[:-1]),sr,subtype='PCM_24')
subprocess.run([ffmpeg,'-v','error','-y','-i',str(root/'Previews/style_comparison.wav'),'-codec:a','libmp3lame','-b:a','192k',str(root/'Previews/style_comparison.mp3')],check=True)
reports=[]
for file in sorted((root/'Exports').glob('*.wav')):
    a,rate = sf.read(file)
    reports.append({'file':str(file.relative_to(root)).replace('\\','/'),'samples':len(a),'duration_seconds':len(a)/rate,
        'sample_rate':rate,'channels':a.shape[1],'subtype':sf.info(file).subtype,
        'finite':bool(np.isfinite(a).all()),'true_peak_4x_dbtp':float(20*np.log10(true_peak(a))),
        'sha256':hashlib.sha256(file.read_bytes()).hexdigest()})
out={'status':'Concept audition only; no production integration; artistic listening pending','source':str(source.relative_to(root)),
    'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'source_is_lossy':True,
    'selection':best,'tempo_note':'Local onset autocorrelation in 50–75 BPM range; not a verified beat/bar transcription.',
    'estimated_local_bpm':60/beat_seconds,'intro_overlap_seconds':overlap/sr,'loop_tail_blend_seconds':blend/sr,
    'processing':'FFmpeg decode to stereo48k; exact crops; final0.6s equal-power blend with real pre-roll; one common constant gain; PCM24 export. No pitch/time stretch/EQ/compression.',
    'gain_db':float(gain_db),'preview_fadein_seconds':1.4,'preview_fadeout_seconds':.8,
    'intro_loop_overlap_max_abs_difference':float(np.max(abs(intro[-overlap:]-loop[:overlap]))),
    'loop_seam_adjacent_step_dbfs':float(20*np.log10(np.max(abs(loop[0]-loop[-1])))),
    'loop_seam_matches_natural_source_pair_max_error':float(np.max(abs((loop[0]-loop[-1])-(x[start]-x[start-1])*gain))),
    'preview_repeat_boundaries_seconds':[start/sr+length/sr,start/sr+2*length/sr],
    'style_comparison_sequence':'0–10s approved Below the Ward opening; 10–11s silence; 11–21s LastWard02 opening; 21–22s silence;22–32s LastWard02 bed. Each matched to-23dBFS RMS,80ms edge fades. RMS matching is not LUFS matching.',
    'exports':reports,'direct_auditory_review':False}
(root/'processing.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps(out,ensure_ascii=False,indent=2))
