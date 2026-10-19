import hashlib
import json
import shutil
import subprocess
import wave
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
PROJECT = next((parent for parent in HERE.parents if (parent / 'Assets/EmergencyShift').is_dir()), HERE.parents[1] / 'outputs/EmergencyShift')
if not HERE.is_relative_to(PROJECT) and not (HERE / 'scene-before.unity').exists():
    shutil.copyfile(PROJECT / 'Assets/EmergencyShift/Scenes/MorgueRouteTest.unity', HERE / 'scene-before.unity')
    (HERE / 'git-before.txt').write_bytes(subprocess.check_output(['git', '-C', str(PROJECT), 'status', '--porcelain=v1']))
    (HERE / 'assets-before.json').write_text(json.dumps({p.relative_to(PROJECT).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest() for p in (PROJECT / 'Assets/EmergencyShift').rglob('*') if p.is_file()}, indent=2))

family = PROJECT / 'Concepts/Used/Environment/AccessDoorTrial01'
art = PROJECT / 'Assets/EmergencyShift/Art/Environment/AccessDoorTrial01'
audio = PROJECT / 'Assets/EmergencyShift/Audio/AccessDoorTrial01'
for folder in (family, art, audio):
    folder.mkdir(parents=True, exist_ok=True)

# Technical stencil: a card outline, magnetic stripe and contact chip. The wear
# comes from the existing floor paint alpha, keeping the established grain.
size = 512
alpha = Image.new('L', (size, size))
draw = ImageDraw.Draw(alpha)
draw.rounded_rectangle((66, 126, 446, 386), radius=24, outline=255, width=24)
draw.rectangle((87, 174, 425, 202), fill=255)
draw.rounded_rectangle((120, 253, 194, 316), radius=6, outline=255, width=16)
paint = Image.open(PROJECT / 'Assets/EmergencyShift/Art/Environment/FloorMarkings/thin-straight.png').convert('RGBA')
paint_alpha = np.asarray(paint.getchannel('A'))
# Sample within the painted centre, rather than its transparent background.
rows = np.where(paint_alpha.mean(axis=1) > 100)[0]
strip = paint.getchannel('A').crop((0, int(rows[0]), size, int(rows[-1]) + 1)).resize((size, size))
mask = np.asarray(alpha, dtype=np.float32) * (.72 + .28 * np.asarray(strip) / 255)
image = Image.new('RGBA', (size, size), (255, 255, 255, 0))
image.putalpha(Image.fromarray(mask.astype(np.uint8)))
image.save(art / 'card-stencil.png')
shutil.copyfile(art / 'card-stencil.png', family / 'card-stencil.png')
(family / 'card-stencil.svg').write_text('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512"><g fill="none" stroke="white" stroke-width="24"><rect x="78" y="138" width="356" height="236" rx="12"/><path d="M87 188H425" stroke-width="28"/><rect x="128" y="261" width="58" height="47" rx="2" stroke-width="16"/></g></svg>')

rate = 48000
def tone(length, frequency, amplitude):
    t = np.arange(round(length * rate)) / rate
    envelope = np.minimum(t / .008, 1) * np.minimum((length - t) / .022, 1)
    return amplitude * envelope * (np.sin(2 * np.pi * frequency * t) + .07 * np.sin(4 * np.pi * frequency * t))

signals = {
    'reader-denied': np.concatenate([tone(.11, 415, .42), np.zeros(round(.045 * rate)), tone(.13, 330, .42)]),
    'reader-granted': np.concatenate([tone(.065, 830, .32), np.zeros(round(.025 * rate)), tone(.09, 1108, .32)]),
}
for name, signal in signals.items():
    # Quiet, short, low-pass shaped electronic reader tones; no replacement of
    # recorded rail/latch sounds. PCM mono assets, no runtime synthesis.
    signal = np.convolve(signal, np.ones(5) / 5, mode='same')
    with wave.open(str(audio / f'{name}.wav'), 'wb') as out:
        out.setnchannels(1)
        out.setsampwidth(2)
        out.setframerate(rate)
        out.writeframes((np.clip(signal, -1, 1) * 32767).astype('<i2').tobytes())
    shutil.copyfile(audio / f'{name}.wav', family / f'{name}.wav')

entries = []
for path in [art / 'card-stencil.png', *audio.glob('*.wav')]:
    entries.append(dict(path=path.relative_to(PROJECT).as_posix(), sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
(family / 'provenance.json').write_text(json.dumps(dict(date='2026-10-19', status='In-game two-door trial; owner evaluation pending', author='Project-authored technical stencil and synthesized reader signals', method='prepare.py: geometric card stencil with existing floor paint alpha; offline PCM sine pairs with soft envelopes and FIR smoothing', paintSource='Assets/EmergencyShift/Art/Environment/FloorMarkings/thin-straight.png', license='Original project content; existing paint provenance remains in FloorMarkings01', production=entries), ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
if Path(__file__).resolve() != (family / 'prepare.py').resolve():
    shutil.copyfile(__file__, family / 'prepare.py')
(family / 'README.md').write_text('# AccessDoorTrial01\n\nПроба на H-P (зелёная) и H-O3 (синяя) в MorgueRouteTest. Рамки составлены из существующей разметки пола. Новая пиктограмма — технический трафарет карты с полосой и чипом; прозрачная матовая краска, без текста и свечения. Исходный SVG сохраняет геометрию, PNG содержит фактуру прежней краски.\n\nКороткие синтезированные звуки считывателя созданы офлайн (48 kHz mono PCM): нисходящая пара — отказ, восходящая — доступ. Это отдельные сигналы терминала; принятые записи движения/остановки дверей сохранены. Громкость .32, задержка открытия .24 с, повтор отказа не чаще .8 с. Художественное и звуковое одобрение ожидается.\n\nprepare.py — процедура подготовки; запуск сохранённого скрипта из этой папки найдёт корень Unity-проекта и повторно экспортирует только ресурсы этой пробы. Исходные модули разметки и сцены не меняет. Происхождение — provenance.json.\n', encoding='utf-8')
print(json.dumps(entries, indent=2))
