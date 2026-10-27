# TASK-005 · Скорпион · native-r7-stocky

Сохранён цельный корпус r6, новая поверхность ноги leg-r7-paint.png следует заранее объявленной укороченной геометрии. Восемь двухзвенных ног: короткий массивный верхний сегмент, плотная голень, небольшая тёмная стопа вместо длинного светлого острия. Клешни компактны, поддерживающие руки укорочены. [Игровой контракт](../../../../Docs/Enemies/SCORPION.md).

Проходы: native-plan.json/независимые проекции →03-native-bones-on-skin.jpg →04-native-volumes.jpg →05-native-muscle-ids.jpg/40 функциональных агрегатов →кожа →24 Run +32 Attack. inspection.json фиксирует фактический осмотр; художественная приёмка не подразумевается.

scorpion_legs_r7.py содержит жёсткие ходовые сегменты/кап колена, постоянную поперечную толщину и перспективный план. scorpion_parts_r5.py сохраняет manus/fixed finger/один hinged finger и хвост. Малый общий диск шарнира дополнен нижним наружным сектором: исправляет Attack06 без квадратного выреза/второго пальца в проёме. Генерации — UV-источники для нативного макета; схема не подгоняется под ошибочный рисунок.

Текущие поверхности: body-source-r6-sealed.png; chela-r5-blood-paint.png →chela-source-r5.png; tail-source-r4.png; leg-r7-paint.png. Промпты —r6-generation.json/r7-generation.json/r5-generation.json. Фотографии владельца служат анатомическими референсами; stock-пиксели не копировались. Маска эмиссии взята до крови. Старые концепты/поверхности исторические; runtime Сталкера не менялся.

Воспроизведение (Python,NumPy,Pillow):

```text
python build_native.py --plan
python build_native.py
# Осмотреть кости до следующих проходов.
python build_native.py --volumes
python build_native.py --muscles
# После осмотра кожи/inspection.json:
python build_native.py --frames run
python build_native.py --frames attack
python finish_assets.py
python serve_review.py
```

Run24×15ms=.36s; Attack32×18/17ms=.56s/2 шага. Минимум4 опоры, постоянные длины, alpha-пересечения, границы кадров/циклов проверяет finish_assets.py. Не измеряет FPS. Живого IK в Unity нет.

[Просмотрщик](http://127.0.0.1:8103/Scorpion01/viewer.html?revision=stocky-r7) обслуживает2 семейства и production PNG только на loopback. Сближение с мишенью — художественный сценарий без AI/звука. Отдельный реальный Scorpion.prefab/комната04 используют этот набор. Нативная техническая проверка источника отделена от проверки комнаты. [QA56 кадров](../../../../Docs/Validation/CreatureVisuals01/ScorpionR7VisualQA.md), [движение/код](../../../../Docs/Validation/CreatureVisuals01/ScorpionR7MotionQA.md). Notion пропущен по поручению владельца.
