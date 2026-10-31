"""Compare actual menu audio and death auditions, and prepare audition-only previews.

python analyze_and_preview.py --analysis-root <outputs/audio-reference-analysis>
No Unity access, no external upload, no production loop authoring.
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
root = Path(__file__).resolve().parent
project = root.parents[3]
sys.path[:0] = [str(analysis_root/'runtime'), str(analysis_root/'inference-runtime')]
import numpy as np
import scipy.signal as signal
import librosa
import soundfile as sf
import imageio_ffmpeg

# Same project model, preprocessing and genre/timbre/rhythm labels as the first study.
source = (analysis_root/'clap_semantics.py').read_text(encoding='utf-8')
scope = {'__file__': str(analysis_root/'clap_semantics.py')}
exec(compile(source.split('specs = [')[0], str(analysis_root/'clap_semantics.py'), 'exec'), scope)
ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
moods = [
 'A mournful electronic keyboard melody expressing grief, loss and regret.',
 'A cold ominous electronic horror game theme full of dread and suspense.',
 'A quiet fragile unresolved minor melody with sadness and resignation.',
 'An aggressive energetic electronic action soundtrack with a driving beat.',
 'An uplifting cheerful major key electronic melody.',
 'A sentimental acoustic piano ballad with orchestral strings.',
]
tok = scope['tokenizer'].encode_batch(moods)
emb_mood = scope['text_session'].run(None, {'input_ids':np.array([x.ids for x in tok],dtype=np.int64)})[0]
emb_mood /= np.linalg.norm(emb_mood,axis=-1,keepdims=True)
specs = [
 (project/'Concepts/Unused/Audio/MenuThemes02/Originals/below_the_ward_02.mp3',[0,10,42.7,80],'approved-menu'),
 (project/'Concepts/Unused/UI/DeathScreen01/Audio/Originals/last_ward_02.mp3',[0,10,20],'previous-death'),
 (root/'Originals/below_the_ward_loss_03.mp3',[0,10,20],'new-death'),
]
rows, vectors, measurements, decoded = [], [], [], {}
for path, starts, role in specs:
 raw = subprocess.check_output([ffmpeg,'-v','error','-i',str(path),'-f','f32le','-ac','2','-ar','48000','-'])
 x = np.frombuffer(raw,dtype='<f4').reshape(-1,2).astype(np.float64)
 decoded[role] = x
 low = librosa.resample(x.mean(axis=1).astype(np.float32),orig_sr=48000,target_sr=12000)
 measurements.append({'role':role,'file':str(path.relative_to(project)).replace('\\','/'),
  'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'seconds':len(x)/48000,
  'finite':bool(np.isfinite(x).all()),'sample_peak_dbfs':float(20*np.log10(abs(x).max())),
  'true_peak_4x_dbtp':float(20*np.log10(abs(signal.resample_poly(x,4,1,axis=0)).max())),
  'rms_dbfs':float(20*np.log10(np.sqrt(np.mean(x*x))))})
 for start in starts:
  clip = x.mean(axis=1)[round(start*48000):round((start+10)*48000)].astype(np.float32)
  emb = scope['audio_session'].run(None, {'input_features':scope['features'](clip)})[0]
  emb /= np.linalg.norm(emb,axis=-1,keepdims=True)
  scores = (emb@scope['text_embeddings'].T)[0]
  ranked,offset = {},0
  for group, labels in scope['GROUPS'].items():
   ranked[group] = [{'description':label,'similarity':round(float(score),4)} for label,score in sorted(zip(labels,scores[offset:offset+len(labels)]),key=lambda p:p[1],reverse=True)]
   offset += len(labels)
  ranked['mood'] = [{'description':label,'similarity':round(float(score),4)} for label,score in sorted(zip(moods,(emb@emb_mood.T)[0]),key=lambda p:p[1],reverse=True)]
  rows.append({'role':role,'start_seconds':start,'ranked':ranked})
  vectors.append(emb[0])
matrix = np.stack(vectors)@np.stack(vectors).T
for i,row in enumerate(rows):
 if row['role']=='approved-menu':continue
 row['similarity_to_menu']=[{'start_seconds':other['start_seconds'],'cosine':round(float(matrix[i,j]),4)} for j,other in enumerate(rows) if other['role']=='approved-menu']

# Source-safe audition: a constant gain, 80ms entrance and 800ms exit only.
new = decoded['new-death'].copy()
gain_db = min(0,-3-20*np.log10(abs(signal.resample_poly(new,4,1,axis=0)).max()))
new *= 10**(gain_db/20)
new[:3840] *= np.sin(np.linspace(0,np.pi/2,3840))[:,None]**2
new[-38400:] *= np.cos(np.linspace(0,np.pi/2,38400))[:,None]**2
sf.write(root/'Previews/below_the_ward_loss_03.wav',new,48000,subtype='PCM_24')
subprocess.run([ffmpeg,'-v','error','-y','-i',str(root/'Previews/below_the_ward_loss_03.wav'),'-codec:a','libmp3lame','-b:a','192k',str(root/'Previews/below_the_ward_loss_03.mp3')],check=True)

# Compare melodic identity and emotion at equal RMS, with labelled time ranges.
sequence = [('approved-menu',0,15),('new-death',0,15),('approved-menu',42.7,57.7),('new-death',15,30)]
parts=[]
for role,a,b in sequence:
 p=decoded[role][round(a*48000):round(b*48000)].copy()
 p*=10**((-23-20*np.log10(np.sqrt(np.mean(p*p))))/20)
 edge=4800
 p[:edge]*=np.sin(np.linspace(0,np.pi/2,edge))[:,None]**2
 p[-edge:]*=np.cos(np.linspace(0,np.pi/2,edge))[:,None]**2
 parts.extend([p,np.zeros((48000,2))])
sf.write(root/'Previews/menu_loss_comparison.wav',np.concatenate(parts[:-1]),48000,subtype='PCM_24')
subprocess.run([ffmpeg,'-v','error','-y','-i',str(root/'Previews/menu_loss_comparison.wav'),'-codec:a','libmp3lame','-b:a','192k',str(root/'Previews/menu_loss_comparison.mp3')],check=True)
out={'task':'TASK-006','method':'Same project CLAP, genre/timbre/rhythm labels plus explicit mood comparisons. Relative similarities, not calibrated probabilities or listening judgments.',
 'model':'Xenova/clap-htsat-unfused quantized ONNX','direct_listening':False,
 'measurements':measurements,'windows':rows,'preview_gain_db':float(gain_db),
 'preview_processing':'Only constant attenuation to <=-3dBTP4x,80ms entrance,800ms exit;stereoPCM24/48k plus MP3. Original retained unchanged. No production intro/loop yet.',
 'comparison':'0–15s approved menu opening;15–16s silence;16–31s new death opening;31–32s silence;32–47s approved menu loop region42.7–57.7;47–48s silence;48–63s new death15–30. Each excerpt-23dBFS RMS,100ms edges. Not LUFS matching.'}
(root/'analysis.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps({'measurements':measurements,'new_windows':[{'start':r['start_seconds'],'menu_similarity':r.get('similarity_to_menu'),**{g:v[:2] for g,v in r['ranked'].items()}} for r in rows if r['role']=='new-death']},ensure_ascii=False,indent=2))
