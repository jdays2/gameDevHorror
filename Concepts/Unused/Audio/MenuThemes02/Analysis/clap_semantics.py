"""Relative CLAP text/audio cosine similarities, not instrument probabilities.

Xenova/clap-htsat-unfused quantized ONNX. Feature extraction matches the
unfused rand_trunc path: 48 kHz, Slaney mel filters, 1024 FFT, 480 hop,
64 bands 50..14000 Hz, power dB. Uses full, deterministic 10s windows.
"""
from pathlib import Path
import json
import subprocess
import sys

ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / 'runtime'))
sys.path.insert(0, str(ROOT / 'inference-runtime'))
import numpy as np
import librosa
import imageio_ffmpeg
import onnxruntime as ort
from tokenizers import Tokenizer

GROUPS = {
 'genre': [
  'Dark ambient horror video game soundtrack.',
  'Industrial electronic horror video game soundtrack.',
  'Industrial rock with electric guitar riffs and drums.',
  'Slow trip hop and downtempo breakbeat music.',
  'Synthwave music with warm analog synth arpeggios and a steady beat.',
  'New age ambient music with soft synthesizer pads.',
  'Gothic orchestral horror music with strings and choir.',
  'Electronic dance techno music with a four on the floor beat.',
  'A haunting melodic science fiction video game menu theme.',
  'Heavy metal music with distorted guitars.',
 ],
 'timbres': [
  'A repeating haunting bell and metallic mallet melody over an electronic drone.',
  'A haunting electronic organ melody over a low pulsing bass.',
  'A distorted electric guitar riff over heavy drums.',
  'An acoustic piano melody with reverb.',
  'Warm analog synthesizer arpeggios and lush sustained pads.',
  'A dark synthesizer drone with sonar like electronic beeps.',
  'Sampled drums and a low gritty electronic bass.',
  'Orchestral strings and a choir singing.',
  'A bright electronic lead melody with a driving bass sequence.',
  'Sustained ethereal choir like synthesizer tones.',
 ],
 'rhythm': [
  'Atmospheric music without a clear drum beat.',
  'A slow sparse electronic drum beat with deep bass pulses.',
  'A repeating breakbeat with syncopated kick and snare.',
  'A steady four on the floor dance beat.',
  'A fast aggressive rock drum beat.',
  'A slow mechanical industrial percussion rhythm.',
 ],
}
texts = [s for group in GROUPS.values() for s in group]
tokenizer = Tokenizer.from_file(str(ROOT / 'clap-model/tokenizer.json'))
tokenizer.enable_truncation(max_length=77)
tokenizer.enable_padding(length=77, pad_id=1, pad_token='<pad>')
tokens = np.array([x.ids for x in tokenizer.encode_batch(texts)], dtype=np.int64)
opts = ort.SessionOptions()
opts.intra_op_num_threads = 4
text_session = ort.InferenceSession(str(ROOT / 'clap-model/text_model_quantized.onnx'), sess_options=opts, providers=['CPUExecutionProvider'])
audio_session = ort.InferenceSession(str(ROOT / 'clap-model/audio_model_quantized.onnx'), sess_options=opts, providers=['CPUExecutionProvider'])
text_embeddings = text_session.run(None, {'input_ids': tokens})[0]
text_embeddings /= np.linalg.norm(text_embeddings, axis=-1, keepdims=True)
ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()

def features(y):
    mel = librosa.feature.melspectrogram(y=y, sr=48000, n_fft=1024, hop_length=480,
         win_length=1024, window='hann', center=True, pad_mode='reflect', power=2,
         n_mels=64, fmin=50, fmax=14000, htk=False, norm='slaney')
    return librosa.power_to_db(mel, ref=1.0, amin=1e-10, top_db=None).T[None,None].astype(np.float32)

specs = [
 ('alien_shooter_menu.mp3', [20,50]),
 ('alien_shooter_2_theme.mp3', [10,70]),
 ('previous_cold_standby.mp3', [10,60]),
]
results = []
for name, starts in specs:
    path = ROOT / name
    if not path.exists(): continue
    raw = subprocess.check_output([ffmpeg,'-v','error','-i',str(path),'-f','f32le','-ac','1','-ar','48000','-'])
    y = np.frombuffer(raw, dtype='<f4').copy()
    for start in starts:
        f = features(y[start*48000:(start+10)*48000])
        emb = audio_session.run(None, {'input_features': f})[0]
        emb /= np.linalg.norm(emb, axis=-1, keepdims=True)
        scores = (emb @ text_embeddings.T)[0]
        offset = 0
        ranked = {}
        for group, labels in GROUPS.items():
            pairs = list(zip(labels, scores[offset:offset+len(labels)]))
            ranked[group] = [{'description':label,'cosine_similarity':round(float(score),4)} for label,score in sorted(pairs,key=lambda x:x[1],reverse=True)]
            offset += len(labels)
        row = {'file':name,'start_seconds':start,'end_seconds':start+10,'ranked_descriptions':ranked}
        results.append(row)
        print(json.dumps(row, ensure_ascii=False), flush=True)
out = {'method':'CLAP relative cosine similarity; model tags are hypotheses, not verified hearing or calibrated probabilities. Scores only compare the listed descriptions.', 'model':'Xenova/clap-htsat-unfused quantized ONNX','results':results}
(ROOT / 'clap_semantics.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf-8')
