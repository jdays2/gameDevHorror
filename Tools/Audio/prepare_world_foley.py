"""Prepare door and pickup foley from attributed CC0 recordings, no synthesized sources."""
from pathlib import Path
import argparse,subprocess,json,hashlib,wave
import numpy as np
ROOT=Path(__file__).resolve().parents[2];SR=48000

def main():
 p=argparse.ArgumentParser();p.add_argument('--ffmpeg',required=True);args=p.parse_args()
 sources=ROOT/'Docs/Audio/WorldFoley/Sources';out=ROOT/'Assets/EmergencyShift/Audio/WorldFoley';out.mkdir(parents=True,exist_ok=True)
 raw={}
 for s in json.loads((sources/'sources.json').read_text(encoding='utf-8')):
  file=sources/s['file'];assert hashlib.sha256(file.read_bytes()).hexdigest()==s['sha256']
  raw[s['id']]=np.frombuffer(subprocess.check_output([args.ffmpeg,'-v','error','-i',str(file),'-f','f32le','-ar',str(SR),'-ac','1','-']),'<f4').astype(float)
 def cut(key,a,b):return raw[key][round(a*SR):round(b*SR)].copy()
 def prep(x,peak=.72):
  x-=x.mean();f=np.fft.rfftfreq(len(x),1/SR);x=np.fft.irfft(np.fft.rfft(x)*(1-np.exp(-(f/65)**4))/(1+(f/12000)**4),n=len(x))
  # Restrained transient compression raises quiet handling without adding synthetic tones.
  env=0.;gain=np.ones(len(x));attack=np.exp(-1/(SR*.001));release=np.exp(-1/(SR*.06))
  for i,v in enumerate(x):
   v=abs(v);k=attack if v>env else release;env=k*env+(1-k)*v;gain[i]=min(1,(max(.0001,env)/.08)**(-.45))
  x*=gain;x*=peak/max(1e-8,np.max(abs(x)))
  n=min(240,len(x)//4);x[:n]*=np.linspace(0,1,n);x[-n:]*=np.linspace(1,0,n);x[0]=x[-1]=0;return x
 def pickup(key,a,b,bodyStart):
  x=cut(key,a,b);body=cut('weapon',bodyStart,bodyStart+(b-a))
  # Recorded handling body prevents a single needle-like click from disappearing in the mix.
  x*=.045/max(1e-8,np.sqrt(np.mean(x*x)));body*=.08/max(1e-8,np.sqrt(np.mean(body*body)))
  return prep(x+body[:len(x)])
 clips={
 'pickup_ammo_01':pickup('ammo',1.93,2.28,.72), 'pickup_ammo_02':pickup('ammo',.02,.49,10.00),
 'pickup_weapon_01':prep(cut('weapon',.71,1.14)), 'pickup_weapon_02':prep(cut('weapon',12.79,13.39)),
 'pickup_upgrade_01':pickup('upgrade',1.33,1.78,.72), 'pickup_upgrade_02':pickup('upgrade',4.42,4.83,10.00),
 'door_start_open_01':prep(cut('door',.96,1.23),.65),'door_start_close_01':prep(cut('door',1.10,1.32),.65),
 'door_stop_01':prep(cut('door',2.02,2.29),.68),'door_stop_02':prep(cut('door',2.12,2.39),.68)}
 loop=prep(cut('door',1.30,1.76),.55);n=2880;fade=np.linspace(0,1,n);loop[:n]=loop[-n:]*(1-fade)+loop[:n]*fade;loop=loop[:-n];clips['door_slide_loop']=loop
 report=[]
 for name,x in clips.items():
  assert np.isfinite(x).all()and np.max(abs(x))<.9
  file=out/(name+'.wav');q=np.rint(x*32767).astype('<i2')
  with wave.open(str(file),'wb')as w:w.setnchannels(1);w.setsampwidth(2);w.setframerate(SR);w.writeframes(q.tobytes())
  report.append(dict(file=file.name,frames=len(x),seconds=len(x)/SR,peak=float(np.max(abs(x))),rms=float(np.sqrt(np.mean(x*x))),sha256=hashlib.sha256(file.read_bytes()).hexdigest()))
 # Audition only: fixed silence between each category; actual door motion uses events/loop.
 demo=np.zeros(round(7*SR))
 for name,start in [('door_start_open_01',.2),('door_stop_01',.53),('pickup_ammo_01',1.7),('pickup_weapon_01',3.1),('pickup_upgrade_01',4.7)]:
  x=clips[name]*.85;i=round(start*SR);demo[i:i+len(x)]+=x
 x=clips['door_slide_loop']*.4;i=round(.28*SR);demo[i:i+len(x)]+=x
 f=ROOT/'Docs/Audio/WorldFoley/door-and-pickups.wav'
 with wave.open(str(f),'wb')as w:w.setnchannels(1);w.setsampwidth(2);w.setframerate(SR);w.writeframes(np.rint(demo*32767).astype('<i2').tobytes())
 (ROOT/'Docs/Audio/WorldFoley/manifest.json').write_text(json.dumps(dict(clips=report,layeredPickups='Ammo/upgrade recorded clicks layered with recorded weapon handling body, RMS .045/.08 before processing; all windows in this tool.',processing='Mono48k/PCM16, DC/65Hz high pass/soft high rolloff, mild transient compression, linear gain/fades. Door slide wrap crossfade60ms. All source material recorded; MP3 quality limitation retained.',door='Open/close are designed variations of one recorded rail movement, not separately recorded directions; body loop stops on actual MotionStopped.',audition='Door0.2s, ammo1.7s, weapon3.1s, upgrade4.7s; offline preview only.'),ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
 print(json.dumps(report))
if __name__=='__main__':main()
