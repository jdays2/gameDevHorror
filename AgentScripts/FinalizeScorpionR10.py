"""Record this revision's actual content and provenance without publishing or touching scenes."""
from pathlib import Path
import hashlib
import json
import re

PROJECT = Path(__file__).resolve().parents[1]
FOLDER = PROJECT / 'Docs/Validation/ScorpionR10'


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def paragraph(text, prefix, replacement):
    pieces = text.split('\n\n')
    matches = [index for index, value in enumerate(pieces) if value.startswith(prefix)]
    if len(matches) != 1:
        raise ValueError(f'Expected one paragraph: {prefix}, found {len(matches)}')
    pieces[matches[0]] = replacement
    return '\n\n'.join(pieces)


def main():
    if (PROJECT/'Docs/Validation/ScorpionPaletteR11/squeak-import.json').exists():
        raise SystemExit('R11 is installed. Use FinalizeScorpionR11.py; the historical R10 writer would overwrite newer owner decisions.')
    path = PROJECT / 'Docs/Enemies/SCORPION.md'
    text = path.read_text(encoding='utf-8')
    text = paragraph(text, 'Текущий рисунок',
        'Текущая версия представления — **r10-burgundy-rupture**, геометрия/рисунки движения — прежние **native-r8-heavy-chela**. '
        'Панцирь тёмно-бордовый, десять глаз едва светятся красным, жидкость смерти жёлто-зелёная. '
        'Большие округлые клешни с короткими пальцами, восемь ног, корпус, хвост/жало и исходные56 фаз сохранены. '
        'Микродетали не добавлены. Цвет задаёт собственный lit palette shader скорпиона; источники не перекрашиваются независимо по кадрам. '
        'Отдельной атаки жалом/яда нет: токсичный цвет — оформление крови.')
    text = paragraph(text, 'После активации',
        'После активации агрессия постоянна; возврата домой и страха света нет. Общая навигация сначала выбирает маршрут и учитывает соседей, '
        'затем ScorpionModel поворачивает корпус по этому вектору:420°/s, разгон9 unit/s², торможение18 unit/s². '
        'Скорость падает при сильном расхождении курса; итоговое движение направлено вдоль корпуса. '
        'Отдельный адаптер ограничивает скорость перед соседом и стеной, не добавляя боковой вектор после поворота. '
        'Фактическая скорость Rigidbody передаётся обратно в модель для нового разгона после блокировки. '
        'Физический радиус тела.4 unit, личная дистанция.68 unit и clearance.22; чужие виды сохраняют прежние defaults. '
        'Фонарь освещает тело и тень; непрозрачная стена скрывает глаза.')
    text = paragraph(text, 'Пробный баланс',
        'Пробный баланс:3 HP, бег2.4 unit/s; атака клешнями.56s со скоростью1.897321 (около79%), '
        '1 урон, начало1.35/удар1.25 unit, окно40%, cooldown.7s. Во время хвата сохраняется направление показанных клешней; '
        'новый маршрут выбирается после recovery. Стена/сосед может замедлить или остановить тело. Окончательный баланс не утверждён.')
    text = paragraph(text, 'Ожидание игрового',
        'Ожидание игрового prefab — Run00 и соответствующие глаза. Вход в атаку согласован с фазой опоры; остаток фазы переносится '
        'в начало двухцикловой атаки. При блокировке остаток шага завершается коротким переступом. Темп согласован с фактически пройденным '
        'расстоянием. Перед ударом проверяется сектор цели, а Heading фиксируется по Facing корпуса, чтобы клешни и движение не расходились.')
    text = paragraph(text, 'Урон разрешает',
        'Урон разрешает общий EnemyMeleeController по времени, дистанции, сектору и LOS, один раз за атаку. PNG/звук урон не наносят. '
        'Смерть/смерть цели/disable отменяют незавершённый удар. Смерть выключает физику и создаёт одну жёлто-зелёную лужу и **12 частей** '
        'именно показанной фазы:8 ног,2 цельных педипальпа с клешнями, хвост и core. Целый рисунок скрывается при появлении полной замены. '
        'Три композиции отличаются силой и направлениями разлёта. Квота32 частей/кадр резервирует каждый комплект12 целиком; следующая смерть '
        'при необходимости ждёт кадр. Нового массового пула/OPT-01 нет. Куски темнее и ниже живых, принадлежат сцене и переживают отключение актёра. '
        'Это разъединение закрытых существующих компонентов; новых нарисованных внутренних срезов пока нет.')
    text = text.replace('## Представление и стоимость',
        'Каталог ScorpionRuptureSet содержит56 поз и672 части в одном атласе4096×2048, PPU200. '
        'Prefab хранит стабильный банк676 ссылок:4 legacy +56×12. Snapshot сохраняет вечный индекс части, transform и цвет; '
        'восстановление возвращает тот же sprite и порядок1–12 внутри SortingGroup0. Старые4 композиции доступны только восстановлению. '
        'Порядок каталога нельзя переставлять без миграции. Текущий контракт actor/Visual — единичный масштаб. '
        'Кампанийное сохранение Скорпиона не подключено.\n\n## Представление и стоимость') if 'Каталог ScorpionRuptureSet' not in text else text
    text = paragraph(text, '[Scorpion.prefab]',
        '[Scorpion.prefab](../../Assets/EmergencyShift/Prefabs/Enemies/Scorpion.prefab): общий мотор/здоровье/восприятие/melee, '
        'личный ScorpionModel/Controller, собственные представление и звуки. Два рисунка тела/глаз, две тени силуэта, пять AudioSource: '
        'голос попадания/смерти, клешни, контакты, присутствие, обнаружение. Клешни имеют отдельный канал, попадание их не прерывает. '
        'Проба плотного смыкания B340ms сохранена; A доступна в просмотрщике. Контакты следуют реальному перемещению. '
        'Короткие вариации присутствия звучат в ожидании3.5–6.5s и погоне1.6–3.2s, у особей разные задержки и без немедленного повтора. '
        'SqueakVolume=.3; расстояние/стены ослабляют звук. [Новые писки](../../Concepts/Used/Audio/ScorpionChatter01/README.md).')
    text = paragraph(text, 'Смерть использует',
        'Смерть использует общий CreatureRupture06 и world-owned EnemyDeathAudioVariants без немедленного повтора. Смертельный Hit не перебивает '
        'смерть. Живые каналы останавливаются, хвосты смерти/сигнала доигрывают; пауза сохраняет playhead, disable/unload останавливает всё. '
        'После хвостов ScorpionAudio2D отключается. Глаза: SpriteRenderer.color=(.12,.12,.12,1) в игровом и визуальном prefab; '
        'общий EyeGlow, маски/LOS и число глаз сохранены. При смерти emission выключен. Прежний A/B множителя.6 относится к исторической r9.')
    text = text.replace('Просмотрщик r9', 'Просмотрщик r10').replace('revision=discovery-death-r9', 'revision=burgundy-rupture-r10')
    text = text.replace('Проверки r9', 'Проверки r10').replace('../Validation/ScorpionR9/README.md', '../Validation/ScorpionR10/README.md')
    text = text.replace('три демонстрации останков и отдельные кнопки писка/смерти/клешней A/B',
        'три направления разлёта12 частей с выбором исходной позы и отдельные пробы звуков')
    path.write_text(text, encoding='utf-8')

    path = PROJECT / 'Concepts/Unused/Visual/Scorpion01/README.md'
    text = paragraph(path.read_text(encoding='utf-8'), 'Представление r',
        'Представление r10: [просмотрщик](http://127.0.0.1:8103/Scorpion01/viewer.html?revision=burgundy-rupture-r10) '
        'показывает бордовую палитру, глаза.12, жёлтую кровь и12 настоящих частей выбранной фазы. '
        'Исходные56 кадров r8 не менялись. build_rupture_r10.py экспортирует8 ног,2 педипальпа, хвост и core каждой фазы; '
        'после packing проверяются все56 сборок. Полный банк676 ссылок сохраняет индексы для восстановления. '
        '[Независимая QA](rupture-r10/qa_postpack-review.md), [нативные проверки](../../../../Docs/Validation/ScorpionR10/README.md). '
        'Существующий prefab править ReviseScorpionR10.Install; R9 отказывается откатывать установленную r10.')
    path.write_text(text, encoding='utf-8')
    path = PROJECT / 'Concepts/Used/Visual/Scorpion01/README.md'
    text = path.read_text(encoding='utf-8')
    if 'Представление r10:' not in text:
        text += '\nПредставление r10: бордовая lit-палитра, глаза.12 и разрыв на12 компонентов каждой из56 фаз. Источники движения r8 сохранены. '
        text += '[Независимый postpack QA](../../../Unused/Visual/Scorpion01/rupture-r10/qa_postpack-review.md), '
        text += '[нативная проверка](../../../../Docs/Validation/ScorpionR10/README.md).\n'
    path.write_text(text, encoding='utf-8')

    provenance_path = PROJECT / 'Concepts/Used/Audio/ScorpionChatter01/provenance.json'
    provenance = json.loads(provenance_path.read_text(encoding='utf-8'))
    catalog_path = PROJECT / 'Concepts/catalog.json'
    catalog = json.loads(catalog_path.read_text(encoding='utf-8'))
    for generation in provenance['generations']:
        source = provenance_path.parent / generation['original']
        target = source.relative_to(PROJECT).as_posix()
        catalog = [entry for entry in catalog if entry.get('to') != target]
        catalog.append(dict(source='Runway Sound Effects', taskId=generation['taskId'], to=target,
            reason='R10 original short arthropod squeak for idle/running. No reference sample copied.', sha256=digest(source)))
    catalog_path.write_text(json.dumps(catalog, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    docs = [PROJECT/'Docs/Enemies/SCORPION.md', PROJECT/'Concepts/Unused/Visual/Scorpion01/README.md',
            PROJECT/'Concepts/Used/Visual/Scorpion01/README.md', provenance_path.parent/'README.md']
    docs += list(FOLDER.glob('*.md'))
    missing = []
    for doc in docs:
        for link in re.findall(r'\]\(([^)]+)\)', doc.read_text(encoding='utf-8')):
            if link.startswith(('http:', 'https:', '#')):
                continue
            if not (doc.parent / link.split('#')[0]).exists():
                missing.append(dict(file=doc.relative_to(PROJECT).as_posix(), link=link))
    result = dict(task='TASK-005', revision='r10-burgundy-rupture', missingLocalLinks=missing,
        sourceMovementFramesChanged=False, provenance=provenance_path.relative_to(PROJECT).as_posix(),
        artisticApproval=False, stressTest=False, authoringReview='Not applicable: no authored scene-object edits.')
    (FOLDER/'content-checks.json').write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(result, ensure_ascii=False))


if __name__ == '__main__':
    main()
