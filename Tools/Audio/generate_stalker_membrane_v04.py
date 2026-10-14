"""Complete Stalker sound pack around the approved Choices03 membrane motif.

Offline Python/NumPy 2.3.5. Original synthesis, no service/recording dependency.
Accepted quiet motifs are extracted sample-for-sample; new events are variations.
Writes AudioClip sources, auditions and manifest; does not wire Unity gameplay.
"""
from pathlib import Path
import argparse
import hashlib
import json
import wave
import zipfile
import numpy as np
import generate_stalker_choices_v03 as core

SR=48000
ROOT=Path(__file__).resolve().parents[2]
ASSETS=ROOT/'Assets/EmergencyShift/Audio/Stalker/Membrane04'
PREVIEW=ROOT/'Docs/Audio/StalkerMembrane04'
REFERENCE=ROOT/'Docs/Audio/StalkerChoices03/02-membrane.wav'


def read24(path):
    with wave.open(str(path),'rb')as w:
        assert (w.getnchannels(),w.getsampwidth(),w.getframerate())==(1,3,SR)
        raw=np.frombuffer(w.readframes(w.getnframes()),dtype=np.uint8).reshape(-1,3).astype(np.int32)
    x=raw[:,0]|(raw[:,1]<<8)|(raw[:,2]<<16)
    return np.where(x&(1<<23),x-(1<<24),x).astype(float)/(2**23-1)


def write(path,x,bits=24):
    assert np.isfinite(x).all() and float(np.max(np.abs(x)))<.98
    q=np.rint(x*(2**(bits-1)-1)).astype(np.int32)
    if bits==24:
        q=q.reshape(-1)
        payload=np.stack([(q>>s)&255 for s in (0,8,16)],axis=-1).astype(np.uint8).tobytes()
    else:payload=q.astype('<i2').tobytes()
    with wave.open(str(path),'wb')as w:
        w.setnchannels(1 if x.ndim==1 else x.shape[1]);w.setsampwidth(bits//8);w.setframerate(SR);w.writeframes(payload)


def finish(x,level):
    return core.scale(core.envelope(x-np.mean(x),.0004,.012),level)


def pressure_drop(rng,large=False):
    # A short liquid impulse, not low continuous air noise.
    seconds=rng.uniform(.035,.075) if large else rng.uniform(.012,.035)
    t=np.arange(int(seconds*SR))/SR
    f0=rng.uniform(120,260) if large else rng.uniform(430,850)
    phase=2*np.pi*np.cumsum(f0*(1+.75*t/seconds))/SR
    x=np.sin(phase)*np.exp(-t/(seconds*.13))
    x+=.24*core.band(rng,len(t),150,2800)*np.exp(-t/(seconds*.09))
    return finish(x,rng.uniform(.15,.25))


def tear(rng,seconds=.13):
    x=np.zeros(int(seconds*SR))
    # Dense irregular microscopic releases, not a uniform noise envelope.
    at=.002
    while at<seconds-.013:
        n=int(rng.uniform(.003,.011)*SR)
        grain=core.band(rng,n,330,3600)*np.exp(-np.arange(n)/SR/(rng.uniform(.0008,.002)))
        core.add(x,core.envelope(grain,.00015,.001),at,rng.uniform(.1,.3))
        at+=rng.uniform(.003,.013)
    core.add(x,pressure_drop(rng,True),.003,.4)
    return finish(x,.24)


def flesh_impact(rng,seconds=.2):
    x=np.zeros(int(seconds*SR));t=np.arange(int(.07*SR))/SR
    body=core.band(rng,len(t),65,900)*np.exp(-t/.009)
    body+=.4*np.sin(2*np.pi*np.cumsum(160*np.exp(-t/.02)+60)/SR)*np.exp(-t/.012)
    core.add(x,core.envelope(body,.0006,.012),.002,.12)
    for at in [.006,.019,.043,.079]:
        core.add(x,pressure_drop(rng,rng.random()<.65),at,rng.uniform(.5,1))
    return finish(x,.35)


def scrape(rng):
    seconds=rng.uniform(.065,.105);x=np.zeros(int(seconds*SR));at=.002
    while at<seconds-.008:
        n=int(rng.uniform(.001,.004)*SR)
        grain=core.band(rng,n,500,2800)*np.exp(-np.arange(n)/SR/.0008)
        core.add(x,core.envelope(grain,.0001,.0004),at,rng.uniform(.06,.15))
        at+=rng.uniform(.002,.009)
    return finish(x,.13)


CATALOG=[
    ('step','Костяной контакт ног',6),('scrape','Короткое трение поверхности',3),
    ('idle','Спокойная мембрана',5),('notice','Малое напряжение при обнаружении',2),
    ('windup','Раскрытие пасти — предупреждение',3),('lunge','Бросок',3),('bite','Укус',4),
    ('light_fear','Первичная боль от света',3),('retreat_pain','Прерывистая боль при отходе',3),
    ('cower','Дрожь сжатого тела',3),('hit','Несмертельное ранение',4),
    ('death','Разрыв тела',4),('debris','Падение останков',6),
]


def build(kind,variant,rng,reference):
    if kind=='idle':
        # These first two slices preserve the liked calm part exactly, including its gain.
        if variant==1:return reference[int(.55*SR):int(.55*SR)+int(.19*SR)].copy()
        if variant==2:return reference[int(1.45*SR):int(1.45*SR)+int(.12*SR)].copy()
        return core.membrane(rng,rng.uniform(.13,.24))*rng.uniform(.8,1.05)
    if kind=='step':
        # Match contact energy, not just peaks: the old variants differed by ~3.5dB RMS.
        x=core.foot(rng)
        x=.10*np.tanh(x/.10)
        return x*(.007/max(float(np.sqrt(np.mean(x*x))),1e-10))
    if kind=='scrape':return scrape(rng)
    if kind=='notice':return core.membrane(rng,rng.uniform(.12,.17))*.95
    if kind=='retreat_pain':return core.membrane(rng,rng.uniform(.14,.23),True)*rng.uniform(.72,.92)
    if kind=='cower':
        x=np.zeros(int(.65*SR))
        core.add(x,core.membrane(rng,.13),.025,.48)
        core.add(x,core.membrane(rng,.09),rng.uniform(.3,.39),.25)
        core.add(x,core.snap(rng,True),.15,.12)
        return core.envelope(x)
    if kind=='windup':
        x=np.zeros(int(.34*SR))
        core.add(x,core.membrane(rng,.27),.01,2.0)
        core.add(x,tear(rng,.13),.09,.48)
        # A tiny release of air belongs to the opening mouth, never a looping idle layer.
        n=int(.025*SR);air=core.band(rng,n,900,4000)*np.sin(np.linspace(0,np.pi,n))**2
        core.add(x,core.envelope(air,.002,.008),.275,.018)
        return finish(x,.29)
    if kind=='lunge':
        x=np.zeros(int(.18*SR));core.add(x,core.foot(rng),.002,1)
        core.add(x,scrape(rng),.015,.65);core.add(x,core.membrane(rng,.10),.035,.8)
        return finish(x,.26)
    if kind=='bite':
        x=np.zeros(int(.33*SR));core.add(x,flesh_impact(rng),.006,1)
        core.add(x,tear(rng,.09),.018,.55);core.add(x,core.snap(rng),.012,.6)
        return finish(x,.47)
    if kind=='light_fear':
        x=np.zeros(int(.39*SR));core.add(x,core.membrane(rng,.29,True),.004,1.25)
        # More explicit immediate irritation, retaining the approved membrane underneath.
        core.add(x,core.small_squeal(rng,.11,.38),.006,1.0)
        return finish(x,.64)
    if kind=='hit':
        x=np.zeros(int(.27*SR));core.add(x,flesh_impact(rng,.18),.002,.85)
        core.add(x,core.membrane(rng,.14,True),.035,.6)
        return finish(x,.40)
    if kind=='death':
        seconds=rng.uniform(.80,1.08);x=np.zeros(int(seconds*SR))
        core.add(x,flesh_impact(rng,.22),.003,1.4)
        core.add(x,tear(rng,.19),.008,1.0)
        core.add(x,core.membrane(rng,.16,True),.005,.32)
        for at in [.021,.045,.078]:core.add(x,core.snap(rng),at,rng.uniform(.3,.6))
        for at in [.075,.14,.23,.33,.49,.66]:
            core.add(x,pressure_drop(rng,True),at*rng.uniform(.9,1.1),rng.uniform(.7,1.15)*(1-at*.6))
        return finish(x,.70)
    if kind=='debris':
        x=np.zeros(int(rng.uniform(.18,.29)*SR))
        core.add(x,flesh_impact(rng,.14),.004,.48)
        core.add(x,core.foot(rng),.015,.15)
        core.add(x,pressure_drop(rng),.09,.35)
        return finish(x,rng.uniform(.21,.31))
    raise ValueError(kind)


def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--force',action='store_true');args=parser.parse_args()
    if (ASSETS/'stalker_idle_01.wav').exists() and not args.force:raise SystemExit('Output exists; deliberate overwrite requires --force.')
    ASSETS.mkdir(parents=True,exist_ok=True);PREVIEW.mkdir(parents=True,exist_ok=True)
    reference=read24(REFERENCE);clips={};manifest=[]
    for group,(kind,title,count)in enumerate(CATALOG):
        for variant in range(1,count+1):
            seed=202610140400+group*83+variant*17
            x=build(kind,variant,np.random.default_rng(seed),reference)
            path=ASSETS/f'stalker_{kind}_{variant:02}.wav';write(path,x);clips[(kind,variant)]=x
            manifest.append(dict(file=path.name,category=kind,title=title,variant=variant,seed=seed,
                frames=len(x),sampleRate=SR,channels=1,bits=24,durationSeconds=len(x)/SR,
                peak=float(np.max(np.abs(x))),rms=float(np.sqrt(np.mean(x*x))),
                sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
    for variant,at,seconds in [(1,.55,.19),(2,1.45,.12)]:
        assert np.array_equal(read24(ASSETS/f'stalker_idle_{variant:02}.wav'),reference[int(at*SR):int(at*SR)+int(seconds*SR)])

    def place(track,kind,variant,at,gain=1,pan=0):
        source=clips[(kind,variant)]
        core.add(track[:,0],source,at,gain*np.cos((pan+1)*np.pi/4))
        core.add(track[:,1],source,at,gain*np.sin((pan+1)*np.pi/4))
    def gait(track,start,end,rate,gain=1,away=False):
        rng=np.random.default_rng(int(start*1000)+2026);at=start;i=0;events=[]
        while at<end:
            progress=(at-start)/(end-start)
            level=gain*((1-.67*progress)if away else(.65+.35*progress))
            pan=.75*progress if away else -.35*(1-progress)
            place(track,'step',1+i%6,at,level,pan)
            # No recurring loud sixth-step body accent; friction is a quiet separate layer.
            if i%3==0:place(track,'scrape',1+(i//3)%3,at+.015,level*.22,pan)
            events.append(at);at+=(.85 if i%3==0 else 1.08)/rate*rng.uniform(.91,1.09);i+=1
        return events

    # A fuller arranged scene. No gunshot sound is invented here; hits are body events only.
    scene=np.zeros((int(21*SR),2));place(scene,'idle',1,.45,1.6,-.4);place(scene,'idle',2,1.4,1.6,-.35)
    stalk=gait(scene,2.4,5.5,5.0,1.35)
    place(scene,'light_fear',1,6.05,1.2)
    retreat=gait(scene,6.22,8.65,14,1.7,True)
    for at,variant,gain,pan in [(6.6,1,.9,.15),(7.25,2,.7,.4),(8.0,3,.5,.65)]:place(scene,'retreat_pain',variant,at,gain,pan)
    place(scene,'cower',1,9.15,1.25,.65);place(scene,'idle',3,10.5,1.2,.25)
    chase=gait(scene,11.4,13.65,8.0,1.45)
    place(scene,'windup',1,14.1,1.1);place(scene,'lunge',1,14.43,1.1);place(scene,'bite',1,14.53,1)
    place(scene,'hit',1,16.1,1);place(scene,'hit',2,16.65,.85)
    place(scene,'death',1,17.65,1)
    for at,variant,gain,pan in [(17.92,1,.5,-.7),(18.13,2,.45,.55),(18.43,3,.32,.2)]:place(scene,'debris',variant,at,gain,pan)
    # Keep the original full-demo master gain, so accepted membrane events keep their level.
    master_gain=.84/(.70/np.sqrt(2));scene*=master_gain
    write(PREVIEW/'stalker-full-encounter.wav',scene,16)

    focus=np.zeros((int(7*SR),2));place(focus,'idle',1,.35,1.7)
    place(focus,'light_fear',2,1.25,1.15)
    focus_steps=gait(focus,1.43,4.1,14,1.9,True)
    for at,variant,gain,pan in [(1.75,1,1,.1),(2.55,2,.75,.35),(3.35,3,.5,.6)]:place(focus,'retreat_pain',variant,at,gain,pan)
    place(focus,'cower',2,4.7,1.3,.65)
    focus*=min(2.0,.80/float(np.max(np.abs(focus))));write(PREVIEW/'stalker-light-and-retreat.wav',focus,16)

    # Short revision audition: level-matched feet, sharper irritation, then retreat/cower.
    revision=np.zeros((int(6.5*SR),2))
    for i in range(6):place(revision,'step',i+1,.22+i*.22,1.35)
    place(revision,'idle',1,1.72,1.6)
    place(revision,'light_fear',1,2.3,1.0)
    gait(revision,2.47,4.6,14,1.7,True)
    place(revision,'retreat_pain',1,2.85,.8,.2);place(revision,'retreat_pain',2,3.6,.55,.5)
    place(revision,'cower',1,5.0,1.25,.65)
    revision*=1.6;write(PREVIEW/'stalker-revision-r2.wav',revision,16)

    # Sequential catalogue: boundaries allow distinct event families to be auditioned.
    timeline=[];cursor=.35
    for kind,title,_ in CATALOG:
        timeline.append(dict(category=kind,title=title,start=cursor,end=cursor+len(clips[(kind,1)])/SR))
        cursor+=len(clips[(kind,1)])/SR+.6
    sampler=np.zeros((int((cursor+.25)*SR),2))
    for entry in timeline:place(sampler,entry['category'],1,entry['start'],1.15)
    write(PREVIEW/'stalker-event-catalogue.wav',sampler,16)
    report=dict(version='Membrane04-r2',direction='Membrane sound approved by user; footstep balance revised and initial light irritation strengthened.',
        method='Original local synthesis and DSP; no external recordings/service.',numpyVersion=np.__version__,runtimeAttached=False,
        generator='Tools/Audio/generate_stalker_membrane_v04.py',dependencies=['generate_stalker_choices_v03.py'],
        reference='Docs/Audio/StalkerChoices03/02-membrane.wav',referenceSha256=hashlib.sha256(REFERENCE.read_bytes()).hexdigest(),
        exactQuietSlices=[dict(file='stalker_idle_01.wav',startSeconds=.55,seconds=.19),dict(file='stalker_idle_02.wav',startSeconds=1.45,seconds=.12)],
        clips=manifest,catalogueTimeline=timeline,encounterSeconds=21,retreatFocusSeconds=7,revisionAuditionSeconds=6.5,
        revisionNote='Six foot contacts RMS-matched to .007 with softened peaks; no sixth-contact body accent. Initial light fear .64 peak with stronger short cry. Other 40 sources unchanged.',
        stagedContactCounts=dict(stalk=len(stalk),retreat=len(retreat),chase=len(chase),focusRetreat=len(focus_steps)),
        previewNote='Arranged audio demonstration, not Unity capture; pan/distance envelopes are hand staged. Runtime cadence/mixer/crowd not implemented.')
    (PREVIEW/'manifest.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    cue=['# Сталкер — Membrane04-r2','','Мембрана одобрена пользователем; выровнены шаги и усилен начальный вскрик от света.','',
        '- stalker-revision-r2.wav: 6.5с, шесть выровненных шагов → спокойная мембрана → усиленный вскрик/бегство → сжатие.',
        '- stalker-full-encounter.wav: 21с, покой → приближение → свет/бегство → сжатие → погоня/укус → ранения → разрыв/падения.',
        '- stalker-light-and-retreat.wav: 7с, отдельно усилен быстрый перебор ног при отходе; звук постепенно удаляется.',
        '- stalker-event-catalogue.wav: изолированный пример каждой группы.','',
        'Это постановочные аудиомонтажи, не запись Unity. В игре звуки ещё не подключены.','',
        '49 исходников в13 группах — моно PCM24/48kHz. Превью стерео PCM16/48kHz. Все клипы однократные; бегство строится отдельными контактами, не фиксированной петлёй.',
        'Два первых idle сохраняют спокойные фрагменты выбранной пробы sample-for-sample. Остальные события развивают этот тембр.','',
        'Каталог по времени:','']
    cue += [f"- {e['start']:.2f}–{e['end']:.2f}с: {e['title']}"for e in timeline]
    (PREVIEW/'README.md').write_text('\n'.join(cue)+'\n',encoding='utf-8')
    with zipfile.ZipFile(PREVIEW/'stalker-membrane04.zip','w',zipfile.ZIP_DEFLATED)as archive:
        for entry in manifest:archive.write(ASSETS/entry['file'],'Stalker/'+entry['file'])
        archive.write(PREVIEW/'manifest.json','manifest.json');archive.write(PREVIEW/'README.md','README.md')
    print(json.dumps({'clips':len(manifest),'categories':len(CATALOG),'previews':4,'quietSlicesExact':True,'stagedRetreatContacts':len(retreat)},indent=2))


if __name__=='__main__':main()
