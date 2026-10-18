"""Archive owner-supplied reference attachments and assemble labelled contact sheets."""
from pathlib import Path
import hashlib
import json
import math
import shutil

from PIL import Image, ImageDraw, ImageFont, ImageOps


ROOT = Path(__file__).resolve().parents[2]
FAMILY = ROOT / 'Concepts/Unused/Environment/HospitalReferences01'
ATTACHMENTS = Path('C:/Users/Mark/AppData/Local/Temp')
REFERENCES = {
    'morgue': [
        ('a1cc33dd-baf2-4b09-9064-563f83748a58', 'M01', 'Шахматная плитка, сталь, каталка'),
        ('7074da9f-3596-4ecd-a60f-302635a200e9', 'M02', 'Мелкая плитка, секционный стол'),
        ('db466a15-6255-41bc-921b-9eeb34c9e583', 'M03', 'Старый пол, холодильные секции'),
        ('3aa14c07-f4f1-4f14-a028-d3f2c69ac860', 'M04', 'Выцветшая сине-кремовая клетка'),
        ('cae2348d-6d0d-4f89-b4f5-6086427250c1', 'M05', 'Серая плитка, каталка, лампа'),
    ],
    'corridors': [
        ('1975ae0d-023f-4cf2-b76f-17ef1bce84d8', 'C01', 'Светлый центр, синий борт'),
        ('7d41279b-4836-4f3b-a44a-ede58107c7e3', 'C02', 'Сплошной синий пол'),
        ('093f9544-255b-4e39-8351-7986ff0d0e38', 'C03', 'Цветовые зоны перед лифтами'),
        ('ec436401-f7c0-47ff-995f-d93140d7b3a0', 'C04', 'Зелёное зернистое покрытие'),
        ('e3b2e0e4-13f9-481e-91b5-fbcfa9f74ed2', 'C05', 'Светлая крошка, линии и пунктир'),
        ('7c929414-a164-48e8-a3c1-b612f95774ab', 'C06', 'Серое покрытие, ветвление линий'),
    ],
    'laboratories': [
        ('ab745bb2-a1e0-4527-b679-3be06829b9db', 'L01', 'Мелкая крошка, длинная решётка'),
        ('8d3b9626-9e9d-4826-bbb3-fceb1855f1d7', 'L02', 'Серый пол, пристенная линия'),
        ('85ddcf53-6726-4a3e-a42e-426b9ab71932', 'L03', 'Серый пол, мойка и вытяжной шкаф'),
        ('f6dd0ece-cba6-4f76-9fac-663dfe649c9f', 'L04', 'Техзона, синий пол и жёлтая линия'),
        ('7448546c-578b-42d5-a0c4-d2d7f751f86f', 'L05', 'Соотношение рабочих зон и проходов'),
        ('e3c8978f-1a23-4642-9b2f-275276b0d3a2', 'L06', 'Плитка, длинные рабочие острова'),
    ],
}


def main():
    (FAMILY / 'Images').mkdir(parents=True, exist_ok=True)
    records = []
    font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 21)
    for group, references in REFERENCES.items():
        width, height, gap, columns = 480, 360, 16, 3
        rows = math.ceil(len(references) / columns)
        board = Image.new('RGB', (columns * (width + gap) + gap, rows * (height + gap) + gap), '#192023')
        draw = ImageDraw.Draw(board)
        for index, (attachment_id, ref_id, note) in enumerate(references):
            source = ATTACHMENTS / f'codex-clipboard-{attachment_id}.png'
            destination = FAMILY / 'Images' / f'{ref_id}.png'
            if not destination.exists():
                shutil.copy2(source, destination)
            if source.exists():
                assert destination.read_bytes() == source.read_bytes()
            with Image.open(destination) as original:
                pixels = list(original.size)
                thumbnail = ImageOps.contain(original.convert('RGB'), (width, height - 40))
            x = gap + (index % columns) * (width + gap)
            y = gap + (index // columns) * (height + gap)
            board.paste(thumbnail, (x + (width - thumbnail.width) // 2, y))
            draw.text((x, y + height - 29), f'{ref_id} · {note}', fill='#e1e4de', font=font)
            records.append({'id': ref_id, 'group': group, 'file': 'Images/' + destination.name,
                            'sourceFilename': source.name, 'note': note, 'pixels': pixels,
                            'sha256': hashlib.sha256(destination.read_bytes()).hexdigest()})
        board.save(FAMILY / f'{group}.png')
    provenance = {'date': '2026-10-18', 'source': 'Images attached by project owner in this conversation',
                  'usage': 'Visual reference only; no Unity import or direct texture extraction',
                  'originalAuthorAndLicense': 'Not supplied; no redistribution/game-asset license inferred',
                  'processor': 'Tools/Art/collect_hospital_references_01.py', 'entries': records}
    (FAMILY / 'provenance.json').write_text(json.dumps(provenance, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({'references': len(records), 'boards': len(REFERENCES)}))


if __name__ == '__main__':
    main()
