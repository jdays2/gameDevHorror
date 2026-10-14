"""Three dry audio studies for review, NOT accepted game assets.

Original local synthesis: bone contacts on tile, sparse viscera, light distress.
Rebuilt without Prototype01's continuous harmonic oscillator or 65Hz wet modulation.
Python + NumPy 2.3.5; writes only Docs/Audio/StalkerStudies02, never Assets/.meta.
"""
from pathlib import Path
import argparse
import hashlib
import json
import math
import wave
import numpy as np

SR = 48000
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Docs/Audio/StalkerStudies02'


def edge(x, attack=.001, release=.018):
    x = x.copy()
    a, r = min(int(attack * SR), len(x)//2), min(int(release * SR), len(x)//2)
    if a: x[:a] *= np.sin(np.linspace(0, np.pi/2, a))**2
    if r: x[-r:] *= np.cos(np.linspace(0, np.pi/2, r))**2
    x[0] = x[-1] = 0
    return x


def noise(rng, seconds, low, high):
    n = max(32, int(seconds * SR))
    f = np.fft.rfftfreq(n, 1/SR)
    response = (1 - np.exp(-(f/max(low, 1))**4)) / np.sqrt(1 + (f/high)**8)
    x = np.fft.irfft(np.fft.rfft(rng.normal(size=n)) * response, n=n)
    return x / max(float(np.sqrt(np.mean(x*x))), 1e-8)


def place(dst, src, at, gain=1):
    start = int(at * SR)
    if start < 0: src, start = src[-start:], 0
    end = min(len(dst), start+len(src))
    if end > start: dst[start:end] += src[:end-start] * gain


def peak(x, level):
    x = edge(x - np.mean(x), .0004, .024)
    return x * level / max(float(np.max(np.abs(x))), 1e-8)


def tile_tip(rng):
    """One hard contact and brief sliding friction, not a human step/tripod recording."""
    duration = rng.uniform(.10, .16)
    x = np.zeros(int(duration * SR))
    t = np.arange(int(.065*SR))/SR
    # Short, inharmonic modes: a hard tip and a very small ceramic response.
    modes = np.zeros(len(t))
    for f, level, decay in [(1050,.20,.008),(2360,.42,.012),(3870,.32,.006),(6120,.12,.003)]:
        modes += level*np.sin(2*np.pi*f*rng.uniform(.9,1.1)*t)*np.exp(-t/(decay*rng.uniform(.8,1.3)))
    impact = noise(rng, .065, 1100, 8500)*np.exp(-t/.0014)
    place(x, edge(.40*modes+.30*impact,.00015,.008), .003)
    # Random asperities during the drag, decelerating; no fixed-frequency buzz.
    for i in range(int(rng.integers(7,15))):
        at = .012 + rng.uniform(0, duration*.68)
        seconds = rng.uniform(.0015,.006)
        grain = noise(rng, seconds, 1700, rng.uniform(4800,9500))
        grain *= np.exp(-np.arange(len(grain))/SR/(seconds*.18))
        place(x, edge(grain,.0001,.0006), at, rng.uniform(.025,.07))
    # Surface scrape is interrupted, not a constant hiss beneath the walk.
    friction = noise(rng, duration, 1200, 6500)
    knots = rng.uniform(0,1,7)**3
    envelope = np.interp(np.linspace(0,1,len(x)),np.linspace(0,1,len(knots)),knots)
    x += .025*friction*envelope*np.sin(np.linspace(0,np.pi,len(x)))**2
    return peak(x,.38*rng.uniform(.7,1))


def pressure(rng, seconds):
    """Internal tissue pressure: slow, nonperiodic motion, without audible breathing."""
    x = noise(rng, seconds, 65, rng.uniform(650,1100))
    n = len(x)
    knots = rng.uniform(.01,1,int(seconds*7)+5)**2
    env = np.interp(np.linspace(0,1,n),np.linspace(0,1,len(knots)),knots)
    env *= np.sin(np.linspace(0,np.pi,n))**2
    return edge(x*env,.025,.08)


def fluid_pop(rng, large=False):
    """A single pressure release: a damped fluid cavity, irregularly placed by caller."""
    seconds = rng.uniform(.045,.12) if large else rng.uniform(.012,.047)
    t = np.arange(int(seconds*SR))/SR
    start = rng.uniform(190,430) if large else rng.uniform(550,1450)
    rate = start*(1 + rng.uniform(.35,.9)*(t/max(seconds,.001))**.6)
    # Fewer than a handful of useful cycles. Not a sustained oscillator layer.
    body = np.sin(2*np.pi*np.cumsum(rate)/SR)*np.exp(-t/(seconds*rng.uniform(.10,.18)))
    turbulence = noise(rng, seconds, 120, 2400)*np.exp(-t/(seconds*.14))
    return edge(body*.42+turbulence*.20,.0003,.009)


def viscera(rng, seconds=1.6):
    x = pressure(rng, seconds)*.11
    # Uneven little releases and sticky separations, separated by internal silence.
    for _ in range(int(rng.integers(5,10))):
        at = rng.uniform(.07,seconds-.13)
        place(x,fluid_pop(rng,large=rng.random()<.6),at,rng.uniform(.12,.36))
    for _ in range(3):
        seconds_grain=rng.uniform(.06,.18)
        grain=noise(rng,seconds_grain,240,1800)
        grain*=np.sin(np.linspace(0,np.pi,len(grain)))**3
        place(x,edge(grain,.008,.025),rng.uniform(.05,seconds-.2),rng.uniform(.015,.035))
    return peak(x,.15)


def distress(rng, seconds, sharp=False):
    """Broken, aperiodic tissue squeal. No stable glottal tone/harmonic stack."""
    n = int(seconds*SR); x = np.zeros(n)
    at = .005
    while at < seconds-.04:
        progress = at/seconds
        length = min(rng.uniform(.028,.075),seconds-at)
        grain = noise(rng,length,110,6500 if sharp else 3800)
        f = np.fft.rfftfreq(len(grain),1/SR)
        center = (1500 if sharp else 780)*(1-.65*progress)*rng.uniform(.8,1.15)
        # Broad formants, changing every grain; no whistle-thin band or insect drone.
        response = .18+np.exp(-.5*((f-center)/(center*.30))**2)
        response += .45*np.exp(-.5*((f-center*2.25)/(center*.65))**2)
        grain = np.fft.irfft(np.fft.rfft(grain)*response,n=len(grain))
        grain *= np.sin(np.linspace(0,np.pi,len(grain)))**2
        place(x,grain,at,rng.uniform(.35,.8)*(1-.45*progress))
        at += length*rng.uniform(.43,1.20)
    env=np.interp(np.linspace(0,1,n),[0,.04,.23,.6,1],[0,1,.85,.43,0])
    x*=env
    place(x,viscera(rng,min(.35,seconds)),.01,.30)
    return peak(x,.52 if sharp else .28)


def gait(track, rng, start, end, rate, gain=1):
    """Rate controls number of distinct contacts; pitch/playback speed stays unchanged."""
    events=[]; at=start; index=0
    while at<end:
        source=tile_tip(rng)
        place(track,source,at,gain*rng.uniform(.8,1))
        events.append(round(at,4))
        # Alternating support groups with small irregularity, never a perfectly even metronome.
        spacing=(.78 if index%3==0 else 1.11)/rate
        at+=spacing*rng.uniform(.91,1.09);index+=1
    return events


def write_wav(path,x):
    x=edge(x,.004,.06)
    assert np.isfinite(x).all() and float(np.max(np.abs(x)))<.95
    quant=np.rint(x*(2**23-1)).astype(np.int32)
    payload=np.stack([(quant>>shift)&255 for shift in (0,8,16)],axis=-1).astype(np.uint8).tobytes()
    with wave.open(str(path),'wb') as w:
        w.setnchannels(1);w.setsampwidth(3);w.setframerate(SR);w.writeframes(payload)
    return {'file':path.name,'sampleRate':SR,'channels':1,'bits':24,'seconds':round(len(x)/SR,4),
        'peakDbfs':round(20*math.log10(max(float(np.max(np.abs(x))),1e-10)),3),
        'rmsDbfs':round(20*math.log10(max(float(np.sqrt(np.mean(x*x))),1e-10)),3),
        'sha256':hashlib.sha256(path.read_bytes()).hexdigest()}


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--force',action='store_true')
    args=parser.parse_args()
    if (OUT/'01-bone-on-tile.wav').exists() and not args.force:
        raise SystemExit('Studies exist; use --force only after reviewing changes.')
    OUT.mkdir(parents=True,exist_ok=True)
    clips=[]; events={}
    rng=np.random.default_rng(202610140201)
    movement=np.zeros(int(8*SR))
    events['slowContacts']=gait(movement,rng,.35,3.5,4.2,.9)
    events['fastContacts']=gait(movement,rng,4.3,7.65,9.0,1.05)
    clips.append(write_wav(OUT/'01-bone-on-tile.wav',movement))

    rng=np.random.default_rng(202610140202)
    idle=np.zeros(int(10*SR))
    for at,seconds,gain in [(.65,1.65,1),(5.6,1.9,.8)]:
        place(idle,viscera(rng,seconds),at,gain)
    clips.append(write_wav(OUT/'02-sparse-viscera.wav',idle))

    rng=np.random.default_rng(202610140203)
    light=np.zeros(int(8*SR))
    place(light,distress(rng,.42,True),.18)
    events['retreatContacts']=gait(light,rng,.72,3.35,10,.72)
    for at,seconds,gain in [(.85,.38,.90),(1.55,.48,.8),(2.4,.55,.6)]:
        place(light,distress(rng,seconds),at,gain)
    for at,gain in [(4.05,.45),(6.05,.28)]:
        place(light,viscera(rng,1.0),at,gain)
        # Brief shifts of folded limbs, not walking while stationary.
        for delay in [0,.065,.16]:place(light,tile_tip(rng),at+delay,.12*gain)
    clips.append(write_wav(OUT/'03-light-distress.wav',light))

    manifest={'status':'Unapproved studies; Prototype01 rejected. No runtime binding.',
        'method':'Original local procedural DSP, no recordings or service; built independently of Prototype01.',
        'numpyVersion':np.__version__,'seeds':[202610140201,202610140202,202610140203],
        'runtimeAttached':False,'clips':clips,'events':events,
        'limitations':'Objective file checks do not establish perceived quality. Cadence and intervals are staged examples, not finalized gameplay timing.'}
    (OUT/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    (OUT/'README.md').write_text('''# Сталкер — три новые звуковые пробы

Набор Prototype01 отклонён пользователем: жужжащий/тональный характер не подходит.
Эти пробы сделаны заново локальным процедурным синтезом. Они ещё не одобрены и не подключены к игре.

1. **01-bone-on-tile.wav, 8 с.** 0.35–3.5: медленный перебор костяных концов ног по плитке; 4.3–7.65: более частые контакты. Pitch не ускоряется вместе с темпом. Короткие скольжения между контактами; без низкого жужжания.
2. **02-sparse-viscera.wav, 10 с.** Два отдельных эпизода внутренних влажных движений, с тишиной между ними; без выраженного дыхания. В игре паузы должны быть случайными, это лишь постановочная проба.
3. **03-light-distress.wav, 8 с.** Вскрик → быстрый отход с прерывистыми реакциями боли → тихие влажные сокращения и дрожь сложенных конечностей. Это монтаж фаз, не новая реализация AI.

Все три файла — моно PCM24/48kHz. Относительные уровни сохранены: idle тише движения и резкой реакции. При прослушивании не включать отдельную автоматическую нормализацию каждого клипа.

Полный согласованный бриф: [STALKER_AUDIO_DIRECTION.md](../../STALKER_AUDIO_DIRECTION.md). Пасть/укус и смерть описаны в нём, но пока не перегенерированы: сначала проверяем характер этих трёх проб. Проверка формата/сигнала — Validation.json; художественное качество оценивается отдельно.
''',encoding='utf-8')
    print(json.dumps({'studies':clips,'output':str(OUT)},ensure_ascii=False,indent=2))


if __name__=='__main__':main()
