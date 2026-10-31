"""Independent diagnostic tags for actual approved menu audio; never an instrument credit.

python check_menu_timbre.py --analysis-root <outputs/audio-reference-analysis>
"""
from pathlib import Path
import argparse
import csv
import json
import subprocess
import sys

parser=argparse.ArgumentParser()
parser.add_argument('--analysis-root',required=True,type=Path)
args=parser.parse_args()
analysis_root=args.analysis_root.resolve()
root=Path(__file__).resolve().parent
project=root.parents[3]
sys.path[:0]=[str(analysis_root/'runtime'),str(analysis_root/'inference-runtime')]
import numpy as np
import onnxruntime as ort
import imageio_ffmpeg
labels=[row['display_name'] for row in csv.DictReader((analysis_root/'yamnet_class_map.csv').open(encoding='utf-8'))]
opts=ort.SessionOptions()
opts.intra_op_num_threads=4
session=ort.InferenceSession(str(analysis_root/'yamnet.onnx'),sess_options=opts,providers=['CPUExecutionProvider'])
path=project/'Concepts/Unused/Audio/MenuThemes02/Originals/below_the_ward_02.mp3'
raw=subprocess.check_output([imageio_ffmpeg.get_ffmpeg_exe(),'-v','error','-i',str(path),'-f','f32le','-ac','1','-ar','16000','-'])
y=np.frombuffer(raw,dtype='<f4').copy()
rows=[]
for start in [0,10,20,42.7,53.3,80,117,130]:
 scores=session.run(None,{'waveform':y[round(start*16000):round((start+10)*16000)]})[0].mean(axis=0)
 instruments={label:round(float(scores[i]),5) for i,label in enumerate(labels) if i in range(133,203)}
 rows.append({'start':start,'top_tags':{labels[i]:round(float(scores[i]),5) for i in np.argsort(scores)[::-1][:15]},'instrument_tags':instruments})
out={'source':str(path.relative_to(project)).replace('\\','/'),'method':'YAMNet event scores; diagnostic hypotheses only. Cannot prove lead instrument or override owner correction.','windows':rows}
(root/'menu-timbre-diagnostic.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps([{'start':r['start'],'top':r['top_tags']} for r in rows],ensure_ascii=False,indent=2))
