# TASK-005 · Скорпион · native-r8-heavy-chela

По новому фото владельца клешни получили округлую тяжёлую ладонь и короткие загнутые тёмные пальцы. Компонент 1.5× к r7, средняя масса ладони дополнительно расширена на22%; пальцевые направляющие отдельно укорочены. Запястья/локти, восемь ног r7, корпус r6, хвост, десять глаз, canvas/pivot и темп сохранены. Микродетали не добавлены. [Контракт](../../../../Docs/Enemies/SCORPION.md).

Представление r9: [просмотрщик](http://127.0.0.1:8103/Scorpion01/viewer.html?revision=discovery-death-r9) дополнен писком/смертью и тремя иллюстративными раскладками останков.56 исходных кадров r8 не менялись. Глаза приглушены через SpriteRenderer.color=.6 в игровом и визуальном prefab; браузер показывает соответствующий множитель эмиссии. Нативный A/B проверен отдельно. Общие3фрагмента CreatureFlesh04 — внутренняя плоть без чужих конечностей; в Unity выпускаются из центра вместе с фактической потемневшей позой. [Проверки r9](../../../../Docs/Validation/ScorpionR9/README.md). Существующий prefab править ReviseScorpionR9.Install; InstallScorpion.Assets теперь только bootstrap и отказывается перезаписывать существующий prefab.

scorpion_parts_r8.py содержит заранее объявленные направляющие и объём: один неподвижный палец — продолжение ладони, один подвижный на шарнире. На беге хват спокойный. Подготовка отводит руки на4°, удар доворачивает на8°. Прежние8° подготовки после расширения ладони пересекали переднюю ногу в Attack07/08; траектория исправлена. Плавная линия разделения сохраняет край ладони при закрытии; прежний горизонтальный разрез давал квадратную выемку и наружный лоскут.

Проходы: независимые проекции →03-native-bones-on-skin.jpg →04-native-volumes.jpg →05-native-muscle-ids.jpg /40 функциональных агрегатов →кожа →24 Run +32 Attack. inspection.json разделяет осмотр и художественную приёмку.

chela-r8-heavy-paint.png — оригинальная генерация встроенным image_gen; [промпт/происхождение](r8-generation.json). Фото reference-heavy-chela-owner.png — только анатомический референс, его пиксели не входят в спрайт. Рисунок является UV-поверхностью; r8-native-registration.json фиксирует исходные ориентиры и независимый шаблон. Риг не подгоняется под случайные детали генерации.

Остальные поверхности: body-source-r6-sealed.png, tail-source-r4.png, leg-r7-paint.png. Кожа закрывает пустые гнёзда, маска глаз взята до крови. Прежние компоненты Historical; r7 нейтраль/план/проверки сохранены в history-r7 и Git. Сталкер/Спящий этой правкой не менялись.

Воспроизведение (Python, NumPy, Pillow):

```text
python build_native.py --plan
python build_native.py --prepare-r8
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

Run24×15ms=.36s; Attack32×18/17ms=.56s /два шага. IK офлайн. finish_assets.py проверяет опоры, длины, пересечения, alpha-края и границы; FPS не измеряет.

[Просмотрщик r8](http://127.0.0.1:8103/Scorpion01/viewer.html?revision=heavy-chela-r8) обслуживает две семьи и два разрешённых WAV на loopback. Кнопка «Послушать удар» и A/B сравнивают новые пробы; автозвук первой особи включается пользователем. [Звук](../../../Used/Audio/Scorpion01/README.md). Здесь нет AI/урона; игровой Scorpion.prefab в прежней R04 использует новые ресурсы.

[Визуальная QA](../../../../Docs/Validation/CreatureVisuals01/ScorpionR8VisualQA.md), [кинематика](../../../../Docs/Validation/CreatureVisuals01/ScorpionR8MotionQA.md), [импорт](../../../../Docs/Validation/CreatureVisuals01/scorpion-r8.json). Художественная приёмка остаётся владельцу. Notion пропущен по его поручению.
