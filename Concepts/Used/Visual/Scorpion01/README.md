# Скорпион · импорт TASK-005 · r8

Текущее представление r12 — четыре бледные палитры; default FleshPink общий для тела и частей, остальные варианты готовы для выбора. Прежний BurgundyShell сохранён. [Нативное сравнение и проверка](../../../../Docs/Validation/ScorpionPalettesR12/README.md). Исходные PNG r8 и риг не менялись; у Run/Attack повышено качество импорта при прежнем форматеBC7 и размерах.

Источник — [native-r8-heavy-chela](../../../Unused/Visual/Scorpion01/README.md): широкие округлые ладони и короткие тёмные пальцы по новому фото. Ноги r7, цельный корпус r6, десять глаз и хвост сохранены. [Контракт](../../../../Docs/Enemies/SCORPION.md).

Атласы/клипы/Animator/EyeGlow/ScorpionVisual.prefab перечислены с SHA-256 в import-manifest.json. Used означает импорт, не художественную приёмку. Игровой Scorpion.prefab использует те же рисунки и TimedSpriteMotion; ожидание —Run00. Ресурсы —runtime-manifest.json. Прежняя комната04 не перестраивается.

Каркас →объёмы →40ID/крепления →кожа →56 кадров осмотрены отдельно. Плавная линия разделения у шарнира и подготовка4° исправляют выемку/пересечение новой широкой ладони. IK офлайн. Промпт —r8-generation.json. Прежние версии Historical в Git/history-r7.

[Импорт](../../../../Docs/Validation/CreatureVisuals01/scorpion-r8.json), [визуальная QA](../../../../Docs/Validation/CreatureVisuals01/ScorpionR8VisualQA.md), [кинематика](../../../../Docs/Validation/CreatureVisuals01/ScorpionR8MotionQA.md). Notion пропущен по поручению владельца.

Представление r10: бордовая lit-палитра, глаза.12 и разрыв на12 компонентов каждой из56 фаз. Источники движения r8 сохранены. [Независимый postpack QA](../../../Unused/Visual/Scorpion01/rupture-r10/qa_postpack-review.md), [нативная проверка](../../../../Docs/Validation/ScorpionR10/README.md).
