"""Offline original sound design. NumPy only; no downloaded recordings or model/service.

Run from any directory: python Tools/Audio/generate_stalker_sfx.py
Existing output is protected; --force deliberately replaces this generator's files.
Unity imports the WAVs, not this script. Runtime audio playback is a separate task.
"""
from pathlib import Path
import argparse
import hashlib
import json
import math
import re
import wave
import zipfile
import numpy as np

SR = 48000
ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / "Assets/EmergencyShift/Audio/Stalker/Prototype01"
PREVIEW = ROOT / "Docs/Audio/StalkerPrototype01"


def norm(x):
    return x / max(float(np.sqrt(np.mean(x * x))), 1e-8)


def tone_noise(n, rng, low=100, high=6000):
    """Smooth broad bands avoid narrow ringing and white-noise hiss."""
    frequencies = np.fft.rfftfreq(n, 1 / SR)
    response = 1 / np.sqrt(1 + (frequencies / high) ** 8)
    if low:
        response *= (frequencies / low) ** 4 / np.sqrt(1 + (frequencies / low) ** 8)
    return norm(np.fft.irfft(np.fft.rfft(rng.normal(size=n)) * response, n=n))


def motion(n, rng, rate):
    knots = max(4, int(n / SR * rate) + 2)
    return np.interp(np.linspace(0, 1, n), np.linspace(0, 1, knots), rng.uniform(-1, 1, knots))


def shape(n, points):
    return np.interp(np.arange(n) / max(1, n - 1), *np.array(points).T)


def fade(x, attack=.004, release=.025):
    x = x.copy()
    a, r = min(int(attack * SR), len(x) // 2), min(int(release * SR), len(x) // 2)
    if a:
        x[:a] *= np.sin(np.linspace(0, np.pi / 2, a)) ** 2
    if r:
        x[-r:] *= np.cos(np.linspace(0, np.pi / 2, r)) ** 2
    x[0] = 0; x[-1] = 0
    return x


def mix_at(track, source, seconds, gain=1):
    start = max(0, int(seconds * SR)); end = min(len(track), start + len(source))
    if end > start:
        track[start:end] += source[:end - start] * gain


def contact(rng, duration=.17):
    n = int(duration * SR); t = np.arange(n) / SR
    grit = tone_noise(n, rng, 700, 6500)
    skin = tone_noise(n, rng, 180, 1700)
    # Three contacts suggest a tripod of legs; no regular boot-step sample.
    pulses = sum(g * np.exp(-np.maximum(0, t - delay) / decay) * (t >= delay)
                 for delay, g, decay in [(0, .65, .011), (.025, .4, .009), (.067, .3, .015)])
    x = grit * pulses + .19 * skin * np.exp(-t / .05)
    x += .13 * np.sin(2 * np.pi * (210 * t - 200 * t * t)) * np.exp(-t / .025)
    return fade(x, .0015, .018)


def wet(rng, duration=.3):
    n = int(duration * SR); t = np.arange(n) / SR
    base = tone_noise(n, rng, 100, 1900)
    phase = np.cumsum(65 + 35 * motion(n, rng, 40)) * 2 * np.pi / SR
    x = base * (.25 + .75 * np.sin(phase) ** 4)
    x *= shape(n, [(0, 0), (.025, 1), (.2, .6), (.65, .3), (1, 0)])
    # Scattered gritty tissue transients give a fluid/crunch mix rather than a laser.
    for _ in range(5):
        delay = rng.uniform(0, duration * .65)
        grain = tone_noise(int(.026 * SR), rng, 1000, 6200)
        grain *= np.exp(-np.arange(len(grain)) / SR / .006)
        mix_at(x, fade(grain, .0007, .006), delay, rng.uniform(.15, .5))
    return fade(x, .002, .03)


def thud(rng, duration=.16):
    n = int(duration * SR); t = np.arange(n) / SR
    frequency = 65 + 115 * np.exp(-t / .018)
    x = np.sin(np.cumsum(frequency) * 2 * np.pi / SR) * np.exp(-t / .038)
    x += .42 * tone_noise(n, rng, 70, 800) * np.exp(-t / .025)
    return fade(x, .0015, .02)


def throat(rng, duration, f0, f1, brightness=1):
    """Irregular glottal excitation, breath and broad tissue resonances, not speech."""
    n = int(duration * SR); t = np.arange(n) / SR
    frequency = f0 + (f1 - f0) * shape(n, [(0, 0), (.25, 1), (1, .1)])
    frequency *= 1 + .13 * motion(n, rng, 23) + .05 * motion(n, rng, 83)
    phase = np.cumsum(frequency) * 2 * np.pi / SR
    excitation = np.zeros(n)
    for harmonic in range(1, 17):
        excitation += np.sin(phase * harmonic + rng.uniform(-.2, .2)) / harmonic ** .85
    rough = .35 + .65 * (motion(n, rng, 55) * .5 + .5)
    excitation *= rough * (.72 + .28 * np.sin(phase * .49) ** 2)
    excitation += .55 * tone_noise(n, rng, 200, 6500) * (.5 + .5 * motion(n, rng, 17))
    frequency_bins = np.fft.rfftfreq(n, 1 / SR)
    resonances = np.ones(len(frequency_bins)) * .07
    for center, width, level in [(310, 180, .6), (760, 300, 1), (1550, 450, .65), (3200, 850, .35)]:
        resonances += level * np.exp(-.5 * ((frequency_bins - center * brightness) / width) ** 2)
    resonances *= 1 / np.sqrt(1 + (frequency_bins / 7200) ** 8)
    body = norm(np.fft.irfft(np.fft.rfft(excitation) * resonances, n=n))
    body = np.tanh(body * 1.45) + .13 * tone_noise(n, rng, 900, 5500)
    return fade(body, .009, .06)


def build(kind, rng, duration):
    n = int(duration * SR); x = np.zeros(n)
    if kind == "step":
        return contact(rng, duration)
    if kind == "idle":
        exhale = throat(rng, duration, 48, 62, .68)
        x += exhale * shape(n, [(0, 0), (.1, .12), (.3, .35), (.55, .1), (.65, 0), (1, 0)])
        mix_at(x, contact(rng, .18), duration * .2, .14)
        mix_at(x, wet(rng, .25), duration * .7, .15)
    elif kind == "notice":
        x += throat(rng, duration, 72, 126, .85) * shape(n, [(0, 0), (.07, .5), (.28, 1), (.6, .55), (1, 0)])
        mix_at(x, wet(rng, .12), .03, .4)
    elif kind == "windup":
        x += throat(rng, duration, 70, 120, .7) * shape(n, [(0, 0), (.1, .3), (.6, 1), (.87, .95), (1, 0)])
        mix_at(x, contact(rng, .09), .01, .24)
    elif kind == "lunge":
        t = np.arange(n) / SR
        x += tone_noise(n, rng, 300, 5200) * np.exp(-.5 * ((t - .065) / .028) ** 2)
        mix_at(x, contact(rng, .12), .015, .4)
    elif kind == "bite":
        mix_at(x, thud(rng), .005, .8)
        mix_at(x, wet(rng, duration * .9), .008, 1.0)
        mix_at(x, contact(rng, .11), .015, .7)
    elif kind == "light_fear":
        x += throat(rng, duration, 145, rng.uniform(270, 330), 1.12) * shape(n, [(0, 0), (.025, 1), (.18, .95), (.42, .7), (.68, .28), (1, 0)])
        mix_at(x, wet(rng, .16), .01, .3)
    elif kind == "retreat":
        for beat in range(6):
            mix_at(x, contact(rng, .14), .01 + beat * .14 + rng.uniform(-.006, .006), rng.uniform(.6, 1))
        x += .05 * tone_noise(n, rng, 200, 3000) * shape(n, [(0, 0), (.05, 1), (.8, .8), (1, 0)])
    elif kind == "hit":
        mix_at(x, thud(rng, .15), .002, .8)
        mix_at(x, wet(rng, .22), .008, .8)
        mix_at(x, throat(rng, .2, 90, 65, .7), .03, .3)
    elif kind == "death":
        mix_at(x, thud(rng, .22), .004, 1)
        mix_at(x, wet(rng, .48), .005, 1.2)
        ripping = tone_noise(int(.46 * SR), rng, 180, 5200)
        ripping *= shape(len(ripping), [(0, 0), (.035, 1), (.22, .7), (.6, .4), (1, 0)])
        mix_at(x, ripping, .015, .65)
        mix_at(x, throat(rng, .42, 100, 55, .65), .015, .3)
        for i in range(9):
            mix_at(x, wet(rng, rng.uniform(.045, .11)), .15 + i * .12 + rng.uniform(-.04, .04), .32 * (1 - i / 11))
        for delay in [.22, .47, .79]:
            mix_at(x, contact(rng, .11), delay, .12)
    elif kind == "debris":
        mix_at(x, thud(rng, .12), .005, .4)
        mix_at(x, wet(rng, .17), .01, .7)
        mix_at(x, contact(rng, .12), .035, .35)
        mix_at(x, wet(rng, .06), duration * .67, .15)
    else:
        raise ValueError(kind)
    return x


CATALOG = [
    ("step", "Контакты конечностей", 4, .20, .15),
    ("idle", "Тихий скрежет и выдох", 3, 1.6, .24),
    ("notice", "Обнаружение игрока", 2, .65, .38),
    ("windup", "Подготовка укуса", 2, .24, .34),
    ("lunge", "Бросок", 2, .22, .32),
    ("bite", "Укус", 3, .42, .46),
    ("light_fear", "Испуг от света", 3, 1.1, .50),
    ("retreat", "Быстрый перестук при отступлении", 2, .94, .26),
    ("hit", "Попадание в плоть и короткий хрип", 3, .33, .40),
    ("death", "Разрыв тела и кровь", 3, 1.5, .62),
    ("debris", "Падение останков", 4, .35, .26),
]


def master(x, peak):
    n = len(x); f = np.fft.rfftfreq(n, 1 / SR)
    response = (f / 48) ** 4 / np.sqrt(1 + (f / 48) ** 8)
    response *= 1 / np.sqrt(1 + (f / 7800) ** 12)
    x = np.fft.irfft(np.fft.rfft(x) * response, n=n)
    x = fade(x - np.mean(x), .003, min(.045, n / SR * .15))
    x *= peak / max(float(np.max(np.abs(x))), 1e-8)
    return x


def write_pcm(path, x, bits=24):
    x = np.asarray(x)
    assert np.isfinite(x).all() and np.max(np.abs(x)) < 1
    quantized = np.rint(x * (2 ** (bits - 1) - 1)).astype(np.int32)
    if bits == 24:
        values = quantized.reshape(-1)
        payload = np.stack([(values >> shift) & 255 for shift in (0, 8, 16)], axis=1).astype(np.uint8).tobytes()
    else:
        payload = quantized.astype('<i2').tobytes()
    with wave.open(str(path), 'wb') as wav:
        wav.setnchannels(1 if x.ndim == 1 else x.shape[1]); wav.setsampwidth(bits // 8)
        wav.setframerate(SR); wav.writeframes(payload)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--force', action='store_true', help='Overwrite this generator\'s output, preserving existing .meta GUIDs.')
    args = parser.parse_args()
    if (ASSETS / 'stalker_step_01.wav').exists() and not args.force:
        raise SystemExit('Output exists. Review changes and use --force deliberately.')
    ASSETS.mkdir(parents=True, exist_ok=True); PREVIEW.mkdir(parents=True, exist_ok=True)
    clips = {}; manifest = []
    for category, (kind, title, count, duration, peak) in enumerate(CATALOG):
        for variant in range(1, count + 1):
            seed = 104_2026 + category * 113 + variant * 29
            rng = np.random.default_rng(seed)
            seconds = duration * rng.uniform(.94, 1.06)
            if kind == 'retreat': seconds = max(.94, seconds)
            x = master(build(kind, rng, seconds), peak * rng.uniform(.94, 1.0))
            name = f'stalker_{kind}_{variant:02}.wav'; path = ASSETS / name
            write_pcm(path, x); clips[(kind, variant)] = x
            manifest.append(dict(file=name,category=kind,title=title,variant=variant,seed=seed,
                sampleRate=SR,channels=1,bits=24,frames=len(x),durationSeconds=round(len(x)/SR,6),
                peakDbfs=round(20*math.log10(float(np.max(np.abs(x)))) ,3),
                rmsDbfs=round(20*math.log10(float(np.sqrt(np.mean(x*x)))) ,3),
                dcOffset=float(np.mean(x)),sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
    # Sequential audition: one example from each category, with enough silence to distinguish it.
    timeline=[]; segments=[]; cursor=.4
    for kind,title,*_ in CATALOG:
        source=clips[(kind,1)]
        if kind=='step':
            source=np.zeros(int(1.05*SR))
            for j in range(4):mix_at(source,clips[('step',j+1)],j*.24)
        timeline.append(dict(category=kind,title=title,startSeconds=round(cursor,3),endSeconds=round(cursor+len(source)/SR,3)))
        segments.append((cursor,source));cursor+=len(source)/SR+.65
    sampler=np.zeros(int(cursor*SR))
    for at,source in segments:mix_at(sampler,source,at)
    sampler*=.72/max(float(np.max(np.abs(sampler))),1e-8)
    write_pcm(PREVIEW/'stalker-showcase.wav',np.column_stack([sampler,sampler]),16)
    # A separate staged audio scene, useful for judging dynamics without changing Unity behavior.
    encounter=np.zeros((int(16*SR),2))
    def place(kind,variant,at,gain=1,pan=0):
        source=clips[(kind,variant)];left=np.cos((pan+1)*np.pi/4);right=np.sin((pan+1)*np.pi/4)
        mix_at(encounter[:,0],source,at,gain*left);mix_at(encounter[:,1],source,at,gain*right)
    for i in range(9):place('step',1+i%4,.45+i*.43,.35+i*.06,-.7+i*.08)
    place('idle',1,1.1,.7,-.5);place('notice',1,4.45,1,-.1)
    place('windup',1,5.5);place('lunge',1,5.7);place('bite',1,5.85)
    place('light_fear',1,7.2);place('retreat',1,7.8,.8,.3);place('retreat',2,8.5,.35,.65)
    place('hit',1,11.1);place('death',1,12.2)
    for i in range(4):place('debris',i+1,12.4+i*.15,.55,-.6+i*.4)
    encounter*=.72/max(float(np.max(np.abs(encounter))),1e-8)
    write_pcm(PREVIEW/'stalker-encounter.wav',encounter,16)
    report=dict(version='prototype01',method='Original local procedural synthesis and DSP; no neural generation, external recordings, or network service.',
        generator='Tools/Audio/generate_stalker_sfx.py',numpyVersion=np.__version__,runtimeAttached=False,
        clips=manifest,showcaseTimeline=timeline,encounterSeconds=16,
        encounterNote='Staged audio arrangement, not a Unity recording; panning and levels deliberately arranged.')
    (PREVIEW/'manifest.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    cue=['# Прослушивание сталкера — Prototype01','','Порядок в `stalker-showcase.wav`:','']
    cue+= [f"- {s['startSeconds']:.2f}–{s['endSeconds']:.2f} с — {s['title']}" for s in timeline]
    cue+=['','`stalker-encounter.wav` — постановочная 16-секундная сцена: подкрадывание, обнаружение, атака, испуг/отход, ранение/разрыв. Это монтаж аудио, не запись Unity.','',
        'Исходные 31 WAV: 48 кГц, 24 бит, моно, короткие one-shot. Новые варианты получены отдельным синтезом с разными seed, не только изменением pitch.','',
        'Это оригинальный процедурный прототип, не нейросетевая генерация или запись настоящих животных. Художественная оценка — после прослушивания пользователем. К игровым событиям ещё не подключён.','',
        'Файлы и назначение — manifest.json. Подробный гайд — ../../STALKER_AUDIO.md.']
    (PREVIEW/'README.md').write_text('\n'.join(cue)+'\n',encoding='utf-8')
    with zipfile.ZipFile(PREVIEW/'stalker-sfx-prototype01.zip','w',zipfile.ZIP_DEFLATED) as archive:
        for entry in manifest:archive.write(ASSETS/entry['file'],'Stalker/'+entry['file'])
        archive.write(PREVIEW/'manifest.json','manifest.json')
        archive.writestr('README.md',(PREVIEW/'README.md').read_text(encoding='utf-8').replace('../../STALKER_AUDIO.md','STALKER_AUDIO.md'))
        guide=ROOT/'Docs/STALKER_AUDIO.md'
        if guide.exists():
            # Audition/validation stay in the repository; make those links usable from the ZIP.
            base='https://github.com/jdays2/gameDevHorror/blob/main/Docs/'
            portable=re.sub(r'\]\((Audio/StalkerPrototype01/[^)]+)\)',
                lambda match:']('+base+match.group(1)+')',guide.read_text(encoding='utf-8'))
            archive.writestr('STALKER_AUDIO.md',portable)
    print(json.dumps(dict(clips=len(manifest),categories=len(CATALOG),showcaseSeconds=round(cursor,2),
        bytes=sum((ASSETS/e['file']).stat().st_size for e in manifest)),indent=2))


if __name__=='__main__':main()
