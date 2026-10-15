"""Temporary pickup cues and two pistol/impact studies. Local NumPy 2.3.5 only.

Combat candidates are not automatically enabled in gameplay. Flesh examples reuse
the approved Stalker hits, avoiding another rejected creature timbre or duplicate files.
"""
from pathlib import Path
import argparse,hashlib,json,wave
import numpy as np
SR=48000
ROOT=Path(__file__).resolve().parents[2]
ASSETS=ROOT/'Assets/EmergencyShift/Audio/PlayerPrototype'
PREVIEW=ROOT/'Docs/Audio/PlayerPrototype'


def noise(rng,seconds,low,high):
    n=max(32,int(seconds*SR));f=np.fft.rfftfreq(n,1/SR)
    response=(1-np.exp(-(f/low)**4))/np.sqrt(1+(f/high)**8)
    x=np.fft.irfft(np.fft.rfft(rng.normal(size=n))*response,n=n)
    return x/max(float(np.sqrt(np.mean(x*x))),1e-8)


def add(dst,src,at,gain=1):
    start=int(at*SR);end=min(len(dst),start+len(src))
    if end>start:dst[start:end]+=src[:end-start]*gain


def master(x,level):
    x=x-np.mean(x);a=min(int(.0003*SR),len(x)//2);r=min(int(.012*SR),len(x)//2)
    x[:a]*=np.sin(np.linspace(0,np.pi/2,a))**2;x[-r:]*=np.cos(np.linspace(0,np.pi/2,r))**2
    x[0]=x[-1]=0
    return x*level/max(float(np.max(np.abs(x))),1e-8)


def click(rng,low=False):
    seconds=.045;t=np.arange(int(seconds*SR))/SR
    x=noise(rng,seconds,120,2400 if low else 5200)*np.exp(-t/.0019)
    x+=.2*np.sin(2*np.pi*rng.uniform(450,1000)*t)*np.exp(-t/.003)
    return master(x,.14)


def shot(rng,room=False):
    n=int(.38*SR);t=np.arange(n)/SR
    crack=noise(rng,.38,700,10000)*np.exp(-t/.007)
    body=noise(rng,.38,90,2300)*np.exp(-t/.027)
    mass=np.sin(2*np.pi*np.cumsum(90+160*np.exp(-t/.011))/SR)*np.exp(-t/.025)
    x=.60*crack+.44*body+.24*mass
    add(x,click(rng,True),.043,.25)
    if room:
        original=x.copy()
        for at,gain in [(.014,.22),(.031,.15),(.058,.10),(.092,.05)]:add(x,original,at,gain)
        tail=noise(rng,.16,180,4600)*np.exp(-np.arange(int(.16*SR))/SR/.031)
        add(x,tail,.013,.10)
    return master(np.tanh(x*.85),.66)


def impact(rng,wood=False):
    seconds=.23;n=int(seconds*SR);t=np.arange(n)/SR
    x=noise(rng,seconds,70 if wood else 250,1400 if wood else 6500)*np.exp(-t/(.012 if wood else .005))
    if wood:x+=.4*np.sin(2*np.pi*rng.uniform(140,230)*t)*np.exp(-t/.011)
    for i in range(4):
        grain=click(rng,wood)
        add(x,grain,.014+i*.027+rng.uniform(0,.009),(.6 if wood else .8)/(i+1))
    return master(x,.36 if wood else .42)


def pickup(rng,kind):
    x=np.zeros(int(.23*SR))
    times={'ammo':[(0,1),(.033,.6)],'weapon':[(0,.75),(.048,1),(.098,.30)],'upgrade':[(0,.5),(.052,.7),(.12,.4)]}[kind]
    for at,gain in times:add(x,click(rng,kind!='ammo'),at,gain)
    return master(x,.14 if kind=='ammo'else .18 if kind=='weapon'else .15)


def write(path,x,bits=24):
    assert np.isfinite(x).all()and np.max(np.abs(x))<.98
    q=np.rint(x*(2**(bits-1)-1)).astype(np.int32)
    if bits==24:
        q=q.reshape(-1);payload=np.stack([(q>>shift)&255 for shift in (0,8,16)],axis=-1).astype(np.uint8).tobytes()
    else:payload=q.astype('<i2').tobytes()
    with wave.open(str(path),'wb')as w:
        w.setnchannels(1 if x.ndim==1 else x.shape[1]);w.setsampwidth(bits//8);w.setframerate(SR);w.writeframes(payload)


def read24(path):
    with wave.open(str(path),'rb')as w:
        raw=np.frombuffer(w.readframes(w.getnframes()),dtype=np.uint8).reshape(-1,3).astype(np.int32)
    v=raw[:,0]|(raw[:,1]<<8)|(raw[:,2]<<16);return np.where(v&(1<<23),v-(1<<24),v).astype(float)/(2**23-1)


def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--force',action='store_true');args=parser.parse_args()
    if (ASSETS/'pickup_ammo_01.wav').exists()and not args.force:raise SystemExit('Output exists; deliberate overwrite requires --force.')
    ASSETS.mkdir(parents=True,exist_ok=True);PREVIEW.mkdir(parents=True,exist_ok=True)
    groups=[('pickup_ammo',2),('pickup_weapon',2),('pickup_upgrade',2),('shot_dry',3),('shot_room',3),('impact_concrete',3),('impact_wood',2)]
    clips={};entries=[]
    for group,(kind,count)in enumerate(groups):
        for variant in range(1,count+1):
            seed=202610140900+group*101+variant*23;rng=np.random.default_rng(seed)
            if kind.startswith('pickup_'):x=pickup(rng,kind.split('_')[1])
            elif kind.startswith('shot_'):x=shot(rng,kind=='shot_room')
            else:x=impact(rng,kind=='impact_wood')
            path=ASSETS/f'{kind}_{variant:02}.wav';write(path,x);clips[(kind,variant)]=x
            entries.append({'file':path.name,'category':kind,'seed':seed,'frames':len(x),'rate':SR,'channels':1,'bits':24,'peak':float(np.max(np.abs(x))),'sha256':hashlib.sha256(path.read_bytes()).hexdigest()})
    flesh=[read24(ROOT/f'Assets/EmergencyShift/Audio/Stalker/Membrane04/stalker_hit_{i:02}.wav')for i in (1,2)]
    for style in ['dry','room']:
        x=np.zeros((int(8*SR),2))
        def place(source,at,gain=1,pan=0):
            add(x[:,0],source,at,gain*np.cos((pan+1)*np.pi/4));add(x[:,1],source,at,gain*np.sin((pan+1)*np.pi/4))
        for i,at in enumerate([.5,1.6,3.0,4.1,5.6,6.8]):
            place(clips[('shot_'+style,1+i%3)],at,.95)
            if i in [2,3]:place(clips[('impact_concrete',1+i%3)],at+.045,.8,.25)
            if i>=4:place(flesh[i%2],at+.04,.9,-.2)
        write(PREVIEW/f'pistol-{style}-preview.wav',x,16)
    x=np.zeros(int(3.4*SR))
    for kind,at in [('ammo',.35),('weapon',1.35),('upgrade',2.35)]:add(x,clips[('pickup_'+kind,1)],at)
    write(PREVIEW/'pickup-preview.wav',np.column_stack([x,x]),16)
    x=np.zeros(int(3.6*SR))
    for source,at in [(clips[('impact_concrete',1)],.3),(clips[('impact_wood',1)],1.4),(flesh[0],2.5)]:add(x,source,at)
    write(PREVIEW/'impact-materials-preview.wav',np.column_stack([x,x]),16)
    report={'method':'Original local procedural DSP. Flesh audition reuses approved Stalker hit sources, no duplicate asset.',
        'numpyVersion':np.__version__,'sourceClips':entries,'pickupRuntimeEnabled':True,'combatRuntimeDefaultEnabled':False,
        'shootingTimeline':'0.5/1.6 dry shots; 3.0/4.1 concrete; 5.6/6.8 flesh. All staged, not Unity recording.',
        'materialPreviewTimeline':'0.3 concrete,1.4 wood,2.5 flesh; wood is a placeholder for future surfaces.'}
    (PREVIEW/'manifest.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({'newWAV':len(entries),'previews':4,'combatEnabledByDefault':False},indent=2))


if __name__=='__main__':main()
