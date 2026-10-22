# Уровень, цели и сохранение

Поддерживаются локальные авторенные объекты сцены. Кампании и динамического spawn-каталога нет; в MechanicsSandbox checkpoint-контроллер намеренно отсутствует.

Архитектурная цель — [ADR-001](../Decisions/001-sod-architecture.md); фактическая карта ниже перенесена из ARCHITECTURE без изменения поведения. При кодовой миграции обновлять её по исходникам.

## Уровень и сохранение

- **LevelDefinition** описывает стабильный levelId, путь сцены, версию совместимости сохранения, карту выхода и приоритетные правила целей. **LevelCatalog** находит описание по ID. Он не создаёт комнаты. nextLevelId зарезервирован: автоматический переход кампании пока не подключён.
- **LevelObjectiveController2D** пересчитывает цели по событиям карт/владения оружием. Первый подходящий rule побеждает: карта выхода имеет приоритет над остальными условиями. Цели не сканируются каждый Update.
- **LevelCompletionView2D** отображает цель/результат на существующем Canvas. **LevelRunController2D** владеет завершением и временем попытки. **LevelParticipant2D** останавливает явно назначенную деятельность при выходе; сценарий уровня не импортирует конкретный вид врага.
- **PersistentEntity2D** хранит уникальный ID расставленного объекта. Его компоненты **ICheckpointParticipant** пишут отдельные versioned payload. Добавление нового вида требует его адаптера состояния, а не нового if в общем загрузчике.
- **CheckpointSessionModel/commands/controller** владеют pending ID, visits и последней успешной точкой; factory создаёт личную session через DI. **LevelCheckpointController2D** координирует native ожидание/подписку run и закрывает их при disable/destroy/rebind. **CheckpointSceneAdapter2D** снимает и применяет одну сцену после полной preflight. **CheckpointStore** проверяет версию/ID и атомарно заменяет JSON через временный файл, сохраняя предыдущий .bak.

Сохраняются здоровье, позиция/направление/фонарь, карты, оружие и патроны, установленные модули; остаток в коробках/собранные предметы; двери/разблокировка; активированные группы; HP/агрессия/состояние/таймеры/маршрут Сталкеров; положение, цвет и форма осевшей крови/частей. Восстановление не вызывает Died и не генерирует разрыв повторно. Конфиги не сериализуются целиком: оружие восстанавливается через contentId/WeaponCatalog.

Перед изменением мира загрузчик проверяет levelId/saveRevision, полный набор ID/ключей, позы/время и payload участников: enum Сталкера, модули, карты, вложенные останки и обязательные bool. В картах отвергаются пустые/пробельные/повторяющиеся ID; null вместо всей коллекции остаётся legacy-пустым набором. Явное false отличается от отсутствующего поля; старые поддерживаемые форматы сохраняются. Clear отменяет ожидающую запись до удаления слота. Числовые диапазоны, версии и авторинг собраны в одном месте — [политике checkpoint](../Guides/CHECKPOINTS.md).

## Проверки и связанные документы

UseStore overrides принадлежат CheckpointPathRegistry в world DI scope: reload наследует тот же путь, другой world изолирован; Dispose очищает реестр и закрывает доступ. Максимум 128 явных overrides; статической mutable карты нет. Session snapshot копирует посещённые ID, malformed visits отвергаются до IO. PersistentEntity проверяет ID/layout/payload до direct Prepare/Restore. Форматы и политика слотов сохранены. [CheckpointSession35](../Reports/CheckpointSession35.md).

Readiness также проверяет самостоятельные RemainsRecord2D и их emitting/settled: отключение или удаление источника не теряет ожидающий разлёт. Restore заменяет root свежим view, direct повторная инициализация занятого record отклоняется. Это lifecycle представления, без новой поддержки сохранения runtime spawn/despawn. [NativeRemains37](../Reports/NativeRemains37.md).

Actor checkpoint adapters используют value-only ActorCheckpointValidation до PrepareRestore и direct Restore: таймеры/направления/closure/melee/patrol/health проверяются существующими чистыми моделями, visualRotation — как unit quaternion. Новые eye values проверяются до clamp, legacy eyeBurned-only сохранён. Направление игрока конечное без overflow квадрата длины; pristine Sleeper также проверяет format. Политика/форматы не менялись. [ActorCheckpoint34](../Reports/ActorCheckpoint34.md).

Цели и прохождение имеют отдельные личные ObjectiveProgressModel/LevelRunModel и команды, созданные LevelProgressInstaller/factory. Rules копируются при Configure/enable; состояние и scaled clock не принадлежат SO/MonoBehaviour. Native фасады сохраняют прежний API, фактические подписки, выбор владельца и scene stop/reload. Finish фиксируется один раз до stop/event; RestoreElapsed переживает поздний Start, обычный timer учитывает паузу. Формат checkpoint не менялся. [Перенос, BUG-027 и реальные lifecycle-проверки](../Reports/LevelProgress29.md).

LevelDefinition/LevelCatalog и inline LevelObjectiveRule имеют readonly runtime API со старыми serialized names и enum values. Lookup валидирует весь каталог до первого match: nonempty IDs, saveRevision, supplied scene-path shape, null/duplicate entries и неизвестные rules. Пустой/null catalog поддержан; scenePath может отсутствовать у legacy/transient fixtures, nextLevelId остаётся резервом. Чистый ObjectiveRuleMatcher использует IObjectiveFacts, unknown условие не считается Always. Текущих production LevelDefinitions нет после удаления FirstShift; новые сцены/кампания не создавались. [Контракт и BUG-026](../Reports/LevelConfig28.md).

Контракт и авторинг: [Уровень, цели и сохранение](../Guides/CHECKPOINTS.md). Проверки выбирать по [VALIDATION](../VALIDATION.md); прежние результаты — [Reports/ValidationIndex](../Reports/ValidationIndex.md). Полная архитектурная сверка — текущий этап [Roadmap](https://app.notion.com/p/3ff8d6dc06b681b6950bd8115222669c?pvs=204).

Editor LevelPersistenceAuthoring до IO/ID/component mutation проверяет stable Edit, существующую загруженную level scene и сконфигурированные run/player/definition. MechanicsSandbox и удалённые scene definitions не принимаются. Historical CheckpointRegressionProbe явно отказывается вне retired FirstShift fixture; старые8 volumes/44 IDs не считаются проверкой текущих сцен. Production persistence проверяется актуальными EditMode/PlayMode fixtures.
