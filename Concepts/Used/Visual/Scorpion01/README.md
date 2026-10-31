# Скорпион · импорт TASK-005 · r8

R14: выбранная палитра BlushIvory сохранена; усилен eye tint .12→.32 без правок shader/масок/PNG. Добавлены жёлтые временные брызги попадания и насекомый голос боли. [Фактические проверки](../../../../Docs/Validation/ScorpionFeedbackR14/README.md). Сравнение палитр в браузере r12 — исторический художественный предпросмотр, обратная связь выстрелов проверяется в Unity.

Палитра r13 — выбранный владельцем вариант 4, **BlushIvory**, общий для тела и останков, включая восстановление. [Применение и проверка в Play](../../../../Docs/Validation/ScorpionPaletteR13/README.md). Другие палитры r12 и BurgundyShell сохранены для сравнения. Исходные PNG r8, риг, глаза, кровь и импорт не менялись после r12.

Источник — [native-r8-heavy-chela](../../../Unused/Visual/Scorpion01/README.md): широкие округлые ладони и короткие тёмные пальцы по новому фото. Ноги r7, цельный корпус r6, десять глаз и хвост сохранены. [Контракт](../../../../Docs/Enemies/SCORPION.md).

Атласы/клипы/Animator/EyeGlow/ScorpionVisual.prefab перечислены с SHA-256 в import-manifest.json. Used означает импорт, не художественную приёмку. Игровой Scorpion.prefab использует те же рисунки и TimedSpriteMotion; ожидание —Run00. Ресурсы —runtime-manifest.json. Прежняя комната04 не перестраивается.

Каркас →объёмы →40ID/крепления →кожа →56 кадров осмотрены отдельно. Плавная линия разделения у шарнира и подготовка4° исправляют выемку/пересечение новой широкой ладони. IK офлайн. Промпт —r8-generation.json. Прежние версии Historical в Git/history-r7.

[Импорт](../../../../Docs/Validation/CreatureVisuals01/scorpion-r8.json), [визуальная QA](../../../../Docs/Validation/CreatureVisuals01/ScorpionR8VisualQA.md), [кинематика](../../../../Docs/Validation/CreatureVisuals01/ScorpionR8MotionQA.md). Notion пропущен по поручению владельца.

Представление r10: бордовая lit-палитра, глаза.12 и разрыв на12 компонентов каждой из56 фаз. Источники движения r8 сохранены. [Независимый postpack QA](../../../Unused/Visual/Scorpion01/rupture-r10/qa_postpack-review.md), [нативная проверка](../../../../Docs/Validation/ScorpionR10/README.md).
