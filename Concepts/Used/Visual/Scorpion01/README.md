# Скорпион · импорт TASK-005 · r7

Источник — [Scorpion01 native-r7-stocky](../../../Unused/Visual/Scorpion01/README.md): короткие массивные лапы, цельный корпус r6, компактные кровавые клешни,10 красных глаз, хвост с жалом. [Контракт](../../../../Docs/Enemies/SCORPION.md).

Визуальные атласы/клипы/Animator/EyeGlow/ScorpionVisual.prefab перечислены с SHA-256 в import-manifest.json. ScorpionVisual имеет Animator и2 SpriteRenderer; Used означает импорт, а не художественную приёмку.

Отдельный Scorpion.prefab использует те же рисунки и TimedSpriteMotion. Стая/движение/удар/тени/звуки подключены в комнате04 MechanicsSandbox. Ожидание игрового prefab — Run00, а не отличающаяся Neutral. Игровые файлы — runtime-manifest.json. [Проверки](../../../../Docs/Validation/ScorpionRoom01/README.md).

Каркас →объёмы →40 функциональных ID/креплений →кожа →56 кадров осмотрены отдельно. Локальный сектор перекрытия шарнира исправляет щель без широкой накладки в проём. IK работает офлайн. Промпты —r6-generation.json/r7-generation.json в источнике. Старые версии доступны в Git. Notion пропущен по поручению владельца.
