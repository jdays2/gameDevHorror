# Пофайловый аудит SOD · сохранения уровня, доступ и подбор · 08

Дата: 2026-10-21. База Git: `2438d98`; проверено текущее рабочее дерево. Игровой C#, тесты, сцены/prefab, SO, звуки, графика, GUID и ручные параметры не менялись. Это чтение и фиксация замечаний, без исправлений.

## Покрытие

Полностью прочитаны **39 C# / 3282 строки**: хранилище/DTO/контроллер и адаптеры checkpoint, уровень/цели/участники, карты/двери/подбор фонаря, два контракта взаимодействия, профили двери/камеры, completion View, authoring, историческая probe и четыре тестовых файла. По каждому в [CodeAudit.json](CodeAudit.json) сохранены ответственность, зависимости, использование, вывод, SHA-256 и история предыдущего заключения.

Выборка: **13 reviewed_boundary / 26 partial**. Из очереди выведен 31 файл; перечитаны 6 partial и 2 ранее проверенные границы. Общий реестр: **269 C# (250 Unity + 11 AgentScripts + 8 Concepts), 112 reviewed_boundary / 86 partial / 71 queued**, `complete=false`. Partial здесь включает полностью прочитанные файлы с конкретным долгом; reviewed_boundary подтверждает описанную границу, а не целую фичу.

PersistentEntity2D переведён из reviewed_boundary в partial: прежняя проверка ID/SaveKey сохраняется, но обнаружен непроверенный pose перед Transform restore. Число отмеченных границ поэтому не равно числу новых прочитанных файлов.

## Основные выводы SOD

- **Сохранение:** DTO не хранит живые Unity-ссылки. Хранилище сначала пишет полный temp и затем Replace/Move, сохраняя backup; загрузчик проверяет ID/структуру/participants до трёх этапов Validate → PrepareRestore → Restore. Но общей numeric/presence-проверки недостаточно: BUG-011 и BUG-013 ниже. Прежний BUG-009 модулей игрока остаётся открытым.
- **Оркестрация:** LevelCheckpointController2D объединяет store/path, ожидание дверей/останков, scene traversal, применение состояния, уведомления и камеру. Static mutable sessionPaths принадлежит всему процессу, не личному scope. Нужны явные service/adapter boundaries; это долг, а не выполненная миграция. WorldSettled делает GetRootGameObjects/GetComponentsInChildren каждый кадр ожидания; нагрузка не измерялась, новый manager/pool не вводился.
- **Доступ и подбор:** PlayerAccessCards2D хранит личный HashSet и события, но Model карт остаётся MonoBehaviour. DoorAccess совмещает проверку доступа, prompt/notification и таймеры. PlayerInteraction совмещает input/controller, выбор/physics/LOS и очередь HUD; injected input/block/registry уже есть, очередь ограничена восемью. IAutoPickup/IInteractable зависят от конкретного PlayerInteraction. Этим границам предстоит SOD-миграция; предметы сейчас не подменялись.
- **Владение фонарём:** FlashlightOwnership и AcquireFlashlightCommand — чистая небольшая граница личного состояния, CanExecute/повторная проверка, обычный отказ duplicate. FlashlightPickup делегирует приобретение и не Destroy-ит предмет. Известное отсутствие checkpoint adapter у подбираемого фонаря в маршрутной пробе не исправлялось и не выдаётся за подключённую persistence.
- **Настройки и цели:** LevelDefinition.Matches читает конкретные player MB, LevelCatalog не валидирует уникальность всех IDs, у LevelDefinition нет полной проверки rules/revision. Неизвестный ObjectiveCondition попадает в default=true. У DoorAccessFeedbackProfile Min/Range не заменяют finite validation. Некорректность текущих SO не доказана. CameraFramingProfile, напротив, явно проверяет finite/ranges/aspect и считает масштаб до/после подбора и роста дальности.
- **Жизненные циклы:** Door2D парно регистрируется в scope, прекращает coroutines при disable и доводит движущуюся створку; Restore не запускает движение повторно. DoorAudio парно отписывается и останавливает оба источника; косметический System.Random не задаёт доступ/урон. Objective и completion View подписываются событиями; активный runtime Configure не переподписывает источники, re-enable после завершения отдельно не проверялся. AwaitingAccess не входит в IsMoving — политика стабильного снимка на время подтверждения требует уточнения, игровой контрпример не заявляется.
- **Авторинг и развитие:** LevelPersistenceAuthoring не запускался. Он сохраняет обычные непустые уникальные IDs, добавляет generic PersistentEntity pass, но сохраняет legacy типовые ветки/FirstShift templates/hardcoded scenePath, не имеет явных Undo/Play guards; часть if без скобок. CheckpointRegressionProbe — opt-in исторический FirstShift fixture на 8 volumes/44 объекта, не рабочее свидетельство текущих сцен. Её запуск до Guard не проверяет null controller. Это записано как ограничение инфраструктуры, а не обещание актуальной регрессии.

Общий долг границ добавлен в [TASK-SOD-05](https://app.notion.com/p/3ff8d6dc06b681a2a256ce727fa02cff); в [TASK-SOD-07](https://app.notion.com/p/3ff8d6dc06b68194b2c3dc6b514ef292) сохраняются оставшиеся вопросы конфигов/совместимости. Готовность этих больших задач не отмечалась.

## BUG-011 · неполная проверка общего pose/time

[Карточка](https://app.notion.com/p/4008d6dc06b681e9bd1de832bb82310a), тип «Баг», высокий приоритет, «Запланировано».

CheckpointStore.Validate:69–93 проверяет finite только position.x/y. PersistentEntity2D.Validate:57–74 проверяет layout/part keys и adapters, но не Z/quaternion. Затем Apply:260–267 вызывает PrepareRestore и SetPositionAndRotation (PersistentEntity2D:80) для всех объектов. elapsedSeconds передаётся в RestoreElapsed (LevelRunController:61–64), где Mathf.Max не отвергает Infinity.

Контрпример для будущего теста: корректный Capture в памяти, отдельно заменить position.z или компонент rotation на NaN, либо elapsedSeconds на PositiveInfinity, передать в Apply. Требуется отказ до любых Transform/активации/таймеров и сохранность мира/старого слота. Дисковое JsonUtility round-trip и конкретная реакция Unity на неправильный Transform **не запускались**, их нельзя выводить из одного чтения. Тест NonFinitePositionIsRejected покрывает только X.

## BUG-012 · Clear не отменяет pending save

[Карточка](https://app.notion.com/p/4008d6dc06b6811d89cbf3a33cba5145), тип «Баг», высокий приоритет, «Запланировано».

RequestSave:110–123 запускает SaveWhenSettled, которая сначала yield return null:126–146. Clear:102–108 удаляет слот/backup и очищает visited/LastCheckpointId, но не останавливает pending. На следующем кадре либо после стабилизации остаётся путь к TrySaveNow и повторному созданию очищенного файла. OnFinished предварительно StopCoroutine делает; это **другой** корректный путь, не контрпример.

Проверить при исправлении: Clear в тот же кадр после RequestSave, во время движения/оседания, отсутствие файла и IsSaving после нескольких кадров, затем успешный новый запрос. Только отдельный temp slot. Корутина в Play не запускалась, исправлений нет.

## BUG-013 · format-only payload validation

[Карточка](https://app.notion.com/p/4008d6dc06b6818d988ffe8e78f77288), тип «Баг», высокий приоритет, «Запланировано».

DoorCheckpoint2D.ValidateCheckpoint:25–31 и соответствующие Card/Upgrade/Encounter:25–31 проверяют только non-null и format==1. Payload `{"format":1}` не содержит обязательных open/unlocked либо consumed; Restore читает эти bool напрямую. Ожидаемое значение отсутствующего bool — false; настоящий JsonUtility/EditMode-тест не запускался.

Нужны отдельные проверки отсутствия каждого поля, полный true/false payload, корректная legacy-совместимость и отказ Apply до изменения мира. Нельзя отвергать все false или молча менять saveRevision. Неиспользуемое remaining в трёх adapters не является необходимым состоянием. Это не дубликат nested modules BUG-009.

## Проверки и ограничения

Прочитаны **19 существующих тестовых случаев**: PersistenceTests — 11, DoorAccessTests — 3, FlashlightOwnershipTests — 4 (в том числе 2 aspect cases), LevelCompositionTests — 1. Это чтение, не новый успешный прогон; новые тесты не добавлялись.

`unity command editor_status --project-path <EmergencyShift> --caller plugin --skill unity-pipeline` сообщил отсутствие Pipeline instance. Второй Editor не запускался, пользовательские слоты не использовались; EditMode/Play/build/Profiler не выполнялись. В MechanicsSandbox и MorgueRouteTest checkpoint controller не добавлялся. Старый FirstShift fixture и его JSON не доказывают новые сценарии.

Проверены число и уникальность записей, актуальные SHA-256/полнота покрытия, сохранность неизменённых C# и согласованность счётчиков; audit_code.py проверяет инвентарь механически и не выносит semantic verdict. YAML/lexical users — подсказки, не AssetDatabase/unused-code сертификация. Локальные технические свидетельства остаются здесь, рабочие статусы — в [общей доске](https://app.notion.com/p/a1b30c6e0f8343168ea91ae5990ee9fb). Закрывается только [TASK-SOD-17](https://app.notion.com/p/4008d6dc06b681d88a50f2ef4e7bcf3a) о чтении выборки; три бага и общий проход остаются открытыми.

## Указатель файлов

| Файл | Ответственность | Результат |
|---|---|---|
| [Runtime/Persistence/CheckpointData.cs](../../Assets/EmergencyShift/Runtime/Persistence/CheckpointData.cs) | Versioned DTO и контракт participant | reviewed_boundary |
| [Runtime/Persistence/CheckpointStore.cs](../../Assets/EmergencyShift/Runtime/Persistence/CheckpointStore.cs) | Файловый slot, JSON и envelope validation | partial |
| [Runtime/Persistence/LevelCheckpointController2D.cs](../../Assets/EmergencyShift/Runtime/Persistence/LevelCheckpointController2D.cs) | Capture/validate/apply и ожидание стабильного мира | partial |
| [Runtime/Persistence/PersistentEntity2D.cs](../../Assets/EmergencyShift/Runtime/Persistence/PersistentEntity2D.cs) | Unity ID/participants и применение pose | partial |
| [Runtime/Persistence/CardCheckpoint2D.cs](../../Assets/EmergencyShift/Runtime/Persistence/CardCheckpoint2D.cs) | Payload состояния карты | partial |
| [Runtime/Persistence/DoorCheckpoint2D.cs](../../Assets/EmergencyShift/Runtime/Persistence/DoorCheckpoint2D.cs) | Payload open/unlocked двери | partial |
| [Runtime/Persistence/EncounterCheckpoint2D.cs](../../Assets/EmergencyShift/Runtime/Persistence/EncounterCheckpoint2D.cs) | Payload Activated авторенной группы | partial |
| [Runtime/Persistence/UpgradeCheckpoint2D.cs](../../Assets/EmergencyShift/Runtime/Persistence/UpgradeCheckpoint2D.cs) | Payload collected модуля | partial |
| [Runtime/Persistence/CheckpointVolume2D.cs](../../Assets/EmergencyShift/Runtime/Persistence/CheckpointVolume2D.cs) | Scene trigger запроса checkpoint | reviewed_boundary |
| [Runtime/Levels/EncounterActivation2D.cs](../../Assets/EmergencyShift/Runtime/Levels/EncounterActivation2D.cs) | Однократное включение авторенных actors | partial |
| [Runtime/Levels/LevelExit2D.cs](../../Assets/EmergencyShift/Runtime/Levels/LevelExit2D.cs) | Trigger намерения закончить уровень | reviewed_boundary |
| [Runtime/Levels/LevelObjectiveController2D.cs](../../Assets/EmergencyShift/Runtime/Levels/LevelObjectiveController2D.cs) | Выбор первой подходящей цели по событиям | partial |
| [Runtime/Levels/LevelParticipant2D.cs](../../Assets/EmergencyShift/Runtime/Levels/LevelParticipant2D.cs) | Остановка авторенного набора activity и Rigidbody | reviewed_boundary |
| [Runtime/Levels/LevelRunController2D.cs](../../Assets/EmergencyShift/Runtime/Levels/LevelRunController2D.cs) | Завершение/elapsed/restart попытки | partial |
| [Runtime/Definitions/LevelDefinition.cs](../../Assets/EmergencyShift/Runtime/Definitions/LevelDefinition.cs) | Общий level ID/compatibility/objective rules | partial |
| [Runtime/Definitions/LevelCatalog.cs](../../Assets/EmergencyShift/Runtime/Definitions/LevelCatalog.cs) | Поиск общего level definition по ID | partial |
| [Runtime/World/Doors/DoorAccess2D.cs](../../Assets/EmergencyShift/Runtime/World/Doors/DoorAccess2D.cs) | Проверка карты и reader feedback | partial |
| [Runtime/World/Doors/Door2D.cs](../../Assets/EmergencyShift/Runtime/World/Doors/Door2D.cs) | Доступ/interaction и движение створки | partial |
| [Runtime/World/Doors/DoorAudio2D.cs](../../Assets/EmergencyShift/Runtime/World/Doors/DoorAudio2D.cs) | Playback рельса/старта/остановки двери | partial |
| [Runtime/Player/PlayerAccessCards2D.cs](../../Assets/EmergencyShift/Runtime/Player/PlayerAccessCards2D.cs) | Личные access tokens игрока | partial |
| [Runtime/Player/PlayerInteraction.cs](../../Assets/EmergencyShift/Runtime/Player/PlayerInteraction.cs) | Target/pickup/reach и очередь HUD notices | partial |
| [Runtime/Player/AcquireFlashlightCommand.cs](../../Assets/EmergencyShift/Runtime/Player/AcquireFlashlightCommand.cs) | Проверяемое приобретение фонаря | reviewed_boundary |
| [Runtime/Player/FlashlightOwnership.cs](../../Assets/EmergencyShift/Runtime/Player/FlashlightOwnership.cs) | Личное состояние владения фонарём | reviewed_boundary |
| [Runtime/World/Loot/AccessCardPickup2D.cs](../../Assets/EmergencyShift/Runtime/World/Loot/AccessCardPickup2D.cs) | Авто-подбор одной карты | partial |
| [Runtime/World/Loot/FlashlightPickup2D.cs](../../Assets/EmergencyShift/Runtime/World/Loot/FlashlightPickup2D.cs) | Unity-мост одноразового подбора фонаря | reviewed_boundary |
| [Runtime/World/Loot/FlashlightUpgradePickup.cs](../../Assets/EmergencyShift/Runtime/World/Loot/FlashlightUpgradePickup.cs) | Авто-подбор одного модуля фонаря | partial |
| [Runtime/Core/Contracts/IAutoPickup.cs](../../Assets/EmergencyShift/Runtime/Core/Contracts/IAutoPickup.cs) | Контракт Unity-подбора и presentation kind | partial |
| [Runtime/Core/Contracts/IInteractable.cs](../../Assets/EmergencyShift/Runtime/Core/Contracts/IInteractable.cs) | Контракт Unity-interaction по E | partial |
| [Runtime/Definitions/DoorAccessFeedbackProfile.cs](../../Assets/EmergencyShift/Runtime/Definitions/DoorAccessFeedbackProfile.cs) | Общий reader sound/feedback tuning | partial |
| [Runtime/Definitions/DoorAudioSet.cs](../../Assets/EmergencyShift/Runtime/Definitions/DoorAudioSet.cs) | Общие записанные clips двери | reviewed_boundary |
| [Runtime/Definitions/CameraFramingProfile.cs](../../Assets/EmergencyShift/Runtime/Definitions/CameraFramingProfile.cs) | Данные и расчёт framing до/после подбора | reviewed_boundary |
| [Runtime/Persistence/PlayerCheckpoint2D.cs](../../Assets/EmergencyShift/Runtime/Persistence/PlayerCheckpoint2D.cs) | Player format1/2/3 DTO и restore | partial |
| [Runtime/UI/LevelCompletionView2D.cs](../../Assets/EmergencyShift/Runtime/UI/LevelCompletionView2D.cs) | Показ цели и результата попытки | reviewed_boundary |
| [Editor/Authoring/LevelPersistenceAuthoring.cs](../../Assets/EmergencyShift/Editor/Authoring/LevelPersistenceAuthoring.cs) | Явная подготовка checkpoint adapters/ID | partial |
| [Runtime/Development/CheckpointRegressionProbe.cs](../../Assets/EmergencyShift/Runtime/Development/CheckpointRegressionProbe.cs) | Opt-in историческая disk/death/reload probe | partial |
| [Tests/EditMode/PersistenceTests.cs](../../Assets/EmergencyShift/Tests/EditMode/PersistenceTests.cs) | Изолированные storage/ID/restore регрессии | partial |
| [Tests/EditMode/DoorAccessTests.cs](../../Assets/EmergencyShift/Tests/EditMode/DoorAccessTests.cs) | Регрессии доступа/feedback/restore | reviewed_boundary |
| [Tests/EditMode/FlashlightOwnershipTests.cs](../../Assets/EmergencyShift/Tests/EditMode/FlashlightOwnershipTests.cs) | Ownership command и framing регрессии | reviewed_boundary |
| [Tests/EditMode/LevelCompositionTests.cs](../../Assets/EmergencyShift/Tests/EditMode/LevelCompositionTests.cs) | Изоляция scoped уровней и общий world pause | reviewed_boundary |
