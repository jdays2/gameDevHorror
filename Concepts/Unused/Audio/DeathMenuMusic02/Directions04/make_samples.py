"""Make exactly20-second auditions, preserving source MP3s. No Unity/production export.

python make_samples.py --analysis-root <outputs/audio-reference-analysis>
"""
from pathlib import Path
import argparse
import hashlib
import json
import subprocess
import sys

parser=argparse.ArgumentParser()
parser.add_argument('--analysis-root',required=True,type=Path)
args=parser.parse_args()
analysis_root=args.analysis_root.resolve()
root=Path(__file__).resolve().parent
sys.path[:0]=[str(analysis_root/'runtime'),str(analysis_root/'inference-runtime')]
import numpy as np
import scipy.signal as signal
import soundfile as sf
import imageio_ffmpeg
ffmpeg=imageio_ffmpeg.get_ffmpeg_exe()
names=['a_piano','b_guitar','c_cello','d_vibraphone']
parts=[]
records=[]
for name in names:
 source=root/f'Originals/{name}.mp3'
 raw=subprocess.check_output([ffmpeg,'-v','error','-i',str(source),'-f','f32le','-ac','2','-ar','48000','-'])
 decoded=np.frombuffer(raw,dtype='<f4').reshape(-1,2).astype(np.float64)
 if len(decoded)<20*48000:
  raise ValueError(f'{name}: less than20seconds')
 clip=decoded[:20*48000].copy()
 source_rms=float(20*np.log10(np.sqrt(np.mean(clip**2))))
 peak=float(20*np.log10(np.max(abs(signal.resample_poly(clip,4,1,axis=0)))))
 gain_db=min(-20-source_rms,-3-peak)
 clip*=10**(gain_db/20)
 attack=3840
 release=24000
 clip[:attack]*=np.sin(np.linspace(0,np.pi/2,attack))[:,None]**2
 clip[-release:]*=np.cos(np.linspace(0,np.pi/2,release))[:,None]**2
 wav=root/f'Previews/{name}_20s.wav'
 mp3=root/f'Previews/{name}_20s.mp3'
 sf.write(wav,clip,48000,subtype='PCM_24')
 subprocess.run([ffmpeg,'-v','error','-y','-i',str(wav),'-codec:a','libmp3lame','-b:a','192k',str(mp3)],check=True)
 written,rate=sf.read(wav)
 # Decode the delivered MP3 to check that all four playable artifacts are sound.
 check=subprocess.check_output([ffmpeg,'-v','error','-i',str(mp3),'-f','f32le','-ac','2','-ar','48000','-'])
 delivered=np.frombuffer(check,dtype='<f4').reshape(-1,2)
 records.append({'id':name,'source':str(source.relative_to(root)).replace('\\','/'),
  'source_seconds':len(decoded)/48000,'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),
  'excerpt_source_seconds':[0,20],'preview_wav':str(wav.relative_to(root)).replace('\\','/'),
  'preview_mp3':str(mp3.relative_to(root)).replace('\\','/'),'samples_per_channel':len(written),'seconds':len(written)/rate,
  'sample_rate':rate,'channels':written.shape[1],'format':sf.info(wav).subtype,
  'source_excerpt_rms_dbfs':source_rms,'constant_gain_db':gain_db,
  'preview_rms_dbfs':float(20*np.log10(np.sqrt(np.mean(written**2)))),
  'preview_true_peak_4x_dbtp':float(20*np.log10(np.max(abs(signal.resample_poly(written,4,1,axis=0))))),
  'finite_wav':bool(np.isfinite(written).all()),'delivered_mp3_seconds':len(delivered)/48000,
  'finite_mp3':bool(np.isfinite(delivered).all()),'mp3_sha256':hashlib.sha256(mp3.read_bytes()).hexdigest()})
 parts.extend([written,np.zeros((48000,2))])
# Encode one playlist via stdin; no redundant long WAV is retained.
playlist=np.concatenate(parts[:-1]).astype('<f4')
subprocess.run([ffmpeg,'-v','error','-y','-f','f32le','-ac','2','-ar','48000','-i','-','-codec:a','libmp3lame','-b:a','192k',str(root/'Previews/all_four_83s.mp3')],input=playlist.tobytes(),check=True)
out={'task':'TASK-006','stage':'Four20second timbre samples for owner choice; not production music',
 'owner_direction':'просто создай несколько коротких семплов на15-20секунд,я тогда укажу направление',
 'processing':'First20seconds from each unchanged generated source; one constant gain toward-20dBFS RMS capped at-3dBTP4x;80ms entrance and500ms exit;no EQ/compression/pitch/time stretch/stem mixing.',
 'instrument_labels':'Requested timbral directions, not verified instrument credits. Owner judges sound. No direct auditory review by agent.',
 'production_intro_loop_prepared':False,'integrated':False,'samples':records,
 'playlist':'A0–20;gap20–21;B21–41;gap41–42;C42–62;gap62–63;D63–83seconds'}
(root/'processing.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps({'samples':[{k:r[k] for k in ['id','seconds','preview_rms_dbfs','preview_true_peak_4x_dbtp','finite_mp3']} for r in records],'playlist_seconds':len(playlist)/48000},indent=2))
