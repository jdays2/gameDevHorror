# Карта файлов

## v0.11 — пробная адаптивная аудиосистема

| Путь | Назначение |
|---|---|
| Docs/ADAPTIVE_AUDIO_GUIDE.md | Практика в Unity, Inspector, C# события/адаптеры/данные, новые уровни, ограничения |
| Scripts/AudioMoodProfile.cs, Settings/SurvivalAudio.asset | Фоновые клипы, уровни, память/сглаживание/duck |
| Scripts/LevelAudioDirector2D.cs | Сценовый контроллер3 синхронных петель, восприятие/пауза/смерть/устройство |
| Scripts/AudioThreatEmitter2D.cs, StalkerAudioThreat2D.cs | Контракт и адаптер без управления AI, scene-filtered реестр |
| Scripts/PlayerWorldAudioSet.cs, Audio/PlayerWorld/PlayerWorldAudio.asset |6 шагов/щелчок/пустой спуск и уровни |
| Scripts/PlayerFootsteps2D.cs, PlayerUtilityAudio2D.cs | Реальное перемещение/материал пола и питание/пустой спуск,3 канала |
| Prefabs/LevelAudio.prefab |3 сохранённых фоновых AudioSource; включён в LevelRig и Basement |
| Audio/Atmosphere/*.wav |3 подготовленные9с стерео PCM-петли |
| Audio/PlayerWorld/*.wav |6 шагов по3 материалам, переключатель/пустой спуск |
| Audio/WorldImpacts/*.wav |2 Concrete/1Wood взамен прежних выключенных заглушек |
| Editor/AdaptiveAudioSetup.cs | Аддитивная миграция/импорт, метка однократной настройки импактов |
| Editor/LevelAudioDirectorEditor.cs | Read-only состояние/восприятие в Play Inspector |
| Scripts/AdaptiveAudioProbe.cs | Временный Editor-only тест и real main-mix recorder |
| Docs/Audio/AdaptivePrototype/ | Sources/Processing/Validation/ScenePreservation/Recording/Build JSON |
| Docs/Demos/adaptive-audio-gameplay-v011.mp3 | Реальная36с запись Unity с постановочным движением/врагом/выстрелами |

Scripts/Editor/Prefabs/Audio/Settings относятся к Assets/EmergencyShift. Все новые Unity-файлы сопровождаются .meta. Добавлены события PlayerLife2D.HealthChanged, Flashlight2D.PowerChanged, PlayerWeapon2D.EmptyTriggerPulled, StalkerAudio2D.PerceptibleCue. GroundAudioSurface2D принимает сцену; PlayerAudio2D варьирует pitch импакта. DevelopmentBuildSmoke проверяет28 каналов/scene audio при смене на LevelTemplate.

Docs/Audio/StatePreviewV2 — реальный40-секундный офлайн-микс трёх состояний из новой серии фактур: build_preview.py, manifest.json, validation.json, README. Docs/Demos/audio-states-v2.mp3 — готовое превью. DistantMemory не используется; без Unity-интеграции.

Docs/Audio/RunwayStudies/StateTransitionStudy — учёт уже созданной4-кредитной ритмической заготовки и отказа от текущего направления после правки пользователя; микс не создан, новые генерации/интеграция не выполнены.

Docs/Audio/RunwayStudies/DistantMemory — оригинальная10-секундная атмосферная проба после референса White Noiz; общие эмоциональные принципы, источники, промпт/task ID/10 кредитов, без использования референсного аудио.

Docs/Audio/RunwayStudies/EmptyRoomRetry — одна10-секундная проба окружения после отклонения полной музыкальной версии; промпт/task ID/10 кредитов, оценка/loop не подтверждены.

Docs/Audio/RunwayStudies/HollowStillnessFull — новая полная версия эмбиента ≈2:58/8 кредитов: промпт/task ID/происхождение/критерии отсутствия существ, ожидание оценки; без интеграции.

Docs/Audio/RunwayStudies/AuthorStudy10s — один самостоятельный10-секундный атмосферный поиск после отклонения RoomTone6s; промпт/task ID/10 кредитов/ожидание оценки.

Docs/Audio/RunwayStudies/RoomTone6s — три отдельные 6-секундные пробы тонального шума помещения (18 кредитов), без монстров/событий; промпты/task ID/происхождение, ожидание оценки.

Docs/Audio/RunwayStudies/BasementTrio — три отдельные пробы подземного окружения A/B/C по15 с (45 кредитов), задания/происхождение/task ID и будущая граница между фоном и локальными событиями; художественная оценка ожидается.

Docs/Audio/RunwayStudies/AmbientSample03 — отдельная проба фонового эмбиента ≈21,9 с/4 кредита после отклонения бита/скрипки в пробе02; критерии, промпт, task ID, ожидание оценки.

Docs/Audio/RunwayStudies/OrganicSample02 — отдельная музыкальная проба ≈25,4 с/4 кредита: промпт, task ID, происхождение и статус ожидания одобрения; без временных ссылок/токенов и интеграции в Unity.

Здесь описаны файлы, которыми мы управляем, и назначение создаваемых Unity групп. Файлы сторонних пакетов и регенерируемый кеш не являются нашим исходным кодом.

Docs/AUDIO_MIX_PLAN.md — универсальная атмосфера без выбранного сеттинга, логика двух комнат/коридора/укрытий, статический разбор текущего аудиокода/банков, будущие шаги и сценарий сравнения общего микса без музыки/с музыкой. Внедрение/новые генерации не заявлены.

Docs/Audio/RunwayStudies/StandardCheck — manifest/README двух успешных проб Standard (музыка/SFX), промпты/задачи/длительность/расход7 кредитов; без временных авторизованных URL и без замены Unity-аудио.

Docs/AUDIO_DIRECTION.md — предложенная концепция меню/исследования/тревоги/боя/критического здоровья, переходов/микса/музыкальных проб; различает текущий MVP и будущие орду/инвентарь. Не означает утверждение или реализацию музыки.

Docs/OPTIMIZATION_BACKLOG.md — отложенные оптимизации/триггеры возврата/архитектурные ограничения. OPT-01: массовая гибель, отложена пользователем; не принимать за текущую задачу. Docs/MVP_PLAN.md начинается с актуальной таблицы исходных этапов после v0.10.

## Корень проекта

| Файл/папка | Назначение |
|---|---|
| `README.md` | Запуск проекта и порядок чтения документации |
| `AGENTS.md` | Требования к работе и дальнейшим изменениям |
| `CHANGELOG.md` | История изменения поведения и структуры |
| `.gitignore` | Исключает регенерируемые и личные файлы из Git |
| `.gitattributes` | Нормализует текстовые исходники и отмечает растровые/видео-файлы как бинарные |
| `Docs/PROGRESS.md` | Краткий текущий прогресс, проверки и очередь задач для репозитория |
| `Docs/MVP_STATUS.md` | Актуальная таблица готовых механик/пробелов MVP и рекомендуемый следующий срез |
| `Assets/EmergencyShift/` | Наши сцены, исходники, формы, материалы и префабы |
| `Assets/Settings/` | Служебные настройки исходного URP-шаблона; рабочий 2D pipeline находится в EmergencyShift/Settings |
| `Assets/Scenes/` | Исходная сцена шаблона; не включена в сборку прототипа |
| `Packages/manifest.json` | Прямые зависимости и их версии |
| `Packages/packages-lock.json` | Точные разрешённые зависимости, создаёт Unity |
| `ProjectSettings/ProjectVersion.txt` | Версия Unity, которой открывать проект |
| `ProjectSettings/*.asset` | Общие настройки: графика, ввод, физика, качество, сборка; меняются через Unity |
| `Library/`, `Temp/`, `Logs/` | Кеш, временные данные и логи; не хранить в Git |
| `UserSettings/` | Локальные настройки редактора; не хранить в Git |

## Наш игровой код

Все следующие файлы находятся в `Assets/EmergencyShift/Scripts/`.

| Файл | Назначение |
|---|---|
| `PlayerMotor2D.cs` | WASD → нормализованное направление → Rigidbody2D |
| `Flashlight2D.cs` | Мышь → поворот Light2D; F → включение |
| `PlayerInteraction.cs` | Выбор двери/E, локальный автоподбор лута и очередь уведомлений |
| `IInteractable.cs` | Контракт доступности, подсказки и действия двери по E |
| `FlashlightUpgradePickup.cs` | Настраиваемый наземный модуль, ограничения подбора, однократное исчезновение; подсказка без чисел, без отчёта после подбора |
| `FlashlightUpgrades.cs` | Накопленные бонусы повторяемых/уникальных модулей, пересчёт от базы и пределы |
| `PlayerWeapon2D.cs` | Владение пистолетом, патроны, ЛКМ, темп, модель, создание выстрела |
| `WeaponLootPickup.cs` | Наземный пистолет/коробка патронов через IAutoPickup, остаток неполной коробки |
| `BulletProjectile2D.cs` | Полёт и CircleCast по пути; один урон, блокирование стенами, удаление |
| `ShotEffect2D.cs` | Короткая вспышка/попадание, затухание Point Light и штрихов |
| `ShotVisualProfile.cs` | Общие данные визуального варианта, ScriptableObject |
| `WeaponDemoRecorder.cs` | Временный сравнительный recorder только UNITY_EDITOR, не входит в билд |
| `Door2D.cs` | Состояние двери и coroutine плавного открытия |
| `CameraFollow2D.cs` | Плавное следование ортографической камеры |
| `PrototypeHud.cs` | Тексты статуса фонаря и взаимодействия |
| `ShadowStalker2D.cs` | Состояния сталкера, движение, выдержка света, укус, получение урона и окончательная гибель |
| `StalkerSpriteAnimator.cs` | Выбор кадров, поворот спрайта, отдельный следящий зрачок |
| `CreatureGroundShadow2D.cs` | Контактная тень и короткая проекция от фонаря, видимость/направление/препятствия/гибель |
| `PlayerLife2D.cs` | Здоровье, реакция на попадание, поражение и перезапуск комнаты |
| `GoreBurst2D.cs` | Одноразовое создание независимых крови и кусков |
| `BloodPool2D.cs` | Раскладка пятен/капель с вариацией по seed |
| `GibMotion2D.cs` | Движение, отскок и остановка куска на полу |
| `StalkerDemoRecorder.cs` | Временная запись настоящего Game View; файл исключён из сборки через UNITY_EDITOR |

## Инструменты редактора

| Файл | Назначение |
|---|---|
| `Assets/EmergencyShift/Editor/PrototypeSceneBuilder.cs` | Однократное создание простых форм, материалов, префабов и сцены |
| `Assets/EmergencyShift/Editor/StarterBootstrap.cs` | Первоначальная подготовка при импорте и запуск Unity MCP |
| `Assets/EmergencyShift/Editor/FlashlightRevision.cs` | Обновление фонаря существующего префаба/сцены без пересборки уровня |
| `Assets/EmergencyShift/Editor/StalkerSceneSetup.cs` | Импорт/нарезка ходьбы и сжатия, префаб врага и слой препятствий |
| `Assets/EmergencyShift/Editor/StalkerCombatSetup.cs` | Боевые кадры, фрагменты, жизнь игрока, UI и префаб крови; меню тестового урона |
| `Assets/EmergencyShift/Editor/StalkerDemoTools.cs` | Размер Game View для записи 1280×720 |
| `Assets/EmergencyShift/Editor/CreatureShadowSetup.cs` | Общий материал силуэтной тени и два SpriteRenderer в префабе/сцене |
| `Assets/EmergencyShift/Editor/FlashlightUpgradeSetup.cs` | Создаёт предметный префаб, подключает игрока/HUD без расстановки лута |
| `Assets/EmergencyShift/Editor/WeaponPrototypeSetup.cs` | Профили/префабы пистолета/патронов/пули/вспышки, связи игрока/HUD и редакторское переключение концептов |
| `Assets/EmergencyShift/Editor/StalkerPaletteRevision.cs` | Новые текстуры/Rect/pivot без смены GUID, палитра глаз и визуальный стиль останков |
| `Assets/EmergencyShift/Editor/StalkerCorpseSetup.cs` | Добавление/удаление сохранённой группы из четырёх статических тестовых трупов |

## Ассеты, создаваемые инструментом

Дополнение v0.4.2: `Scripts/RemainsVisualStyle.cs` — общий цветовой множитель, порядок и Z останков для аниматора, разлёта и редакторских образцов. `Art/Stalker/PaleFlesh/` — четыре рабочие PNG и `PaletteFrameLayout.json` для 28 активных Sprite-ассетов. Старые листы в корне Art/Stalker и неиспользуемые ранние Walk/Recoil сохранены для истории. `Docs/Palette042Results.json` — восемь проверок палитры/слоёв/немедленной процедурной крови. `Docs/Previews/pale-living-and-remains.png` — Game View с временно усиленной подсветкой только для осмотра, сброшенной после Play.

| Файл | Назначение |
|---|---|
| `Scenes/BasementPrototype.unity` | Редактируемый уровень и все ссылки компонентов |
| `Prefabs/Player.prefab` | Капсула, физика и компоненты игрока |
| `Prefabs/Door.prefab` | Дверь со створкой, препятствием и тенью |
| `Prefabs/Wall.prefab` | Стена: однотонная форма, коллайдер и тень |
| `Prefabs/ShadowStalker.prefab` | Составной шестиногий враг; внешние ссылки назначаются экземпляру сцены |
| `Prefabs/BloodPool.prefab` | Переиспользуемая лужа: неровные пятна и капли |
| `Prefabs/FlashlightRangeUpgrade.prefab` | Повторяемый модуль +0.6 дальности/+6° угла, два lit-плейсхолдера и триггер; параметры в Inspector |
| `Prefabs/PistolPickup.prefab`, `AmmoPickup.prefab` | Пистолет с 8 патронами и коробка с 12, lit-плейсхолдеры/триггеры |
| `Prefabs/PistolBullet.prefab`, `PistolShotEffect.prefab` | Движущийся след/точка и одноразовая геометрическая вспышка/Point Light |
| `Settings/WeaponProfiles/` | WarmFlash/ColdTrail/RedPulse.asset и .meta: три редактируемых визуальных профиля |
| `Prefabs/StalkerRemainsTest.prefab` | Последняя поза смерти, восемь неподвижных кусков и отдельная лужа; без AI/физики |
| `Art/Stalker/WalkSheet.png`, `LightReactionSheet.png` | Исходные PNG ходьбы/сжатия |
| `Art/Stalker/AttackSheet.png`, `DeathSheet.png` | Листы атаки/гибели, созданные imagegen; атака скорректирована по пропорциям |
| `Art/Stalker/Frames/*-corrected.asset` | Исправленные Sprite-кадры ходьбы/сжатия с индивидуальными Rect и pivot |
| `Art/Stalker/Frames/Walk*.asset`, `Recoil*.asset` без corrected | Ранние неиспользуемые кадровые ассеты, сохранённые для истории |
| `Art/Stalker/CombatFrameLayout.json` | Rect, pivot и масштаб боевых кадров/фрагментов |
| `Art/Stalker/Frames/Attack*.asset`, `Death*.asset`, `Gib*.asset` | Кадры боя и части для эффекта разрыва |
| `Art/Stalker/EyeDisc.asset`, `BloodBlot.asset` | Простые кодовые формы радужки/зрачка и неровного пятна |
| `Art/Stalker/SoftGroundShadow.asset` | Неиспользуемая ранняя мягкая форма, сохранена для истории |
| `Materials/CreatureSilhouetteShadow.shader`, `.mat` | Полный альфа-силуэт текущего кадра, растяжение/сдвиг и обрезание по лучу/стене |
| `Shapes/Rectangle.asset` | Белая прямоугольная форма для однотонной геометрии |
| `Shapes/Capsule.asset` | Белая капсула для изображения игрока |
| `Materials/FlatLit.mat` | Материал окружения, реагирующий на 2D-свет |
| `Materials/FlatUnlit.mat` | Материал капсулы, сохраняющий её видимость |
| `Materials/NoFriction.physicsMaterial2D` | Нулевая упругость и трение игрока |
| `Settings/Prototype2DRenderer.asset` | 2D Renderer, обработка света и теней |
| `Settings/PrototypeURP.asset` | Рабочий URP pipeline |

Эти пути относятся к `Assets/EmergencyShift/`. `.meta` рядом с каждым ассетом содержит его GUID и параметры импорта. GUID связывает сцену со скриптами, материалами и префабами; переносить ассеты лучше через Project внутри Unity.

## Документы

WEAPONS.md — текущая механика, три концепта, настройка, архитектура и ограничения. Weapon06Results.json — 20 фактических проверок. Previews/pistol-before-pickup.png, pistol-equipped.png, pistol-killed-stalker.png — настоящий Game View. Demos/shooting-concepts-v06.mp4 — сравнительная запись; shooting-warm/cold/red-v06.mp4 — отдельные фрагменты; shooting-recording-summary.json — результат записи/декодирования.

`RepeatableUpgrades052Results.json` — фактические проверки трёх повторяемых находок/пределов/перезагрузки; `Previews/repeatable-upgrades-third.png` — свет/UI после третьей находки.

`InteractionUi051Results.json` — 6 проверок Fit Game View/реальных предмета и двери; `Previews/interaction-upgrade-ui.png` и `interaction-door-ui.png` — видимые UI-подсказки. StalkerDemoTools включает команду Fit Game View; это только редакторский масштаб.

`FLASHLIGHT_AND_LOOT.md` — общий концепт фонаря/лута. Первый технический срез v0.5 реализован; `FLASHLIGHT_UPGRADE_GUIDE.md` описывает добавление префаба, поля и архитектуру. `FlashlightUpgrade05Results.json` — фактические проверки v0.5; `Previews/flashlight-upgrade-before.png` и `after.png` (общий префикс flashlight-upgrade-) — предмет до/после подбора и его UI в Game View.

`CreatureShadows044Results.json` — 13 проверок чётких силуэтных теней; `Previews/silhouette-shadows-before.png`, `after.png`, `two-enemies.png` (общий префикс silhouette-shadows-) — снимки Game View. Предыдущие снимки/отчёт v0.4.3 сохранены как история.

`CreatureShadows043Results.json` — проверки теней в редакторе/Play. `Previews/creature-shadows-before.png` и `creature-shadows-after.png` — одинаковая поза без/с мягкими тенями; `creature-shadows-two-enemies.png` — два врага в луче. `Previews/shadow-probe-before.png` и `shadow-probe-native.png` — проба длинной нативной тени, не используемой в рабочем префабе. Все изображения сняты из настоящего Game View.

Копия снимка двух врагов: `../VisualConcepts/v05/creature-shadow-preview.png` относительно проекта. Она показана отдельным блоком `preview.html`; сами canvas-последовательности по-прежнему показывают только рисунок/зрачок, не рендер Unity.

`SPEC.md` — текущие требования; `GAME_DIRECTION.md` — направление survival horror, 2D/2.5D и будущие ключи/пароли/улучшения; `ARCHITECTURE.md` — связи и решения; `LEARNING_CSHARP.md` — уроки; `VALIDATION.md` — проверки; `MCP_SETUP.md` — подключения. `SmokeResults.json` содержит фактические результаты десяти проверок первоначального каркаса в Play Mode. `Previews/` содержит снимки игры для визуальной проверки; версия изменения отмечена в именах новых снимков.

`Assets/Screenshots/` — первоначальные снимки, созданные Unity MCP для визуальной проверки; в игровой сцене не используются. Отобранные снимки продублированы в `Docs/Previews/`.

При добавлении нашего нового файла добавить его в эту карту.

`ENEMIES.md` — описание сталкера, тестовых останков и будущего требования нескольких вариантов разорванного тела. `RestartInput04Results.json` — проверка события R через Input System. `TestCorpses041Results.json` — проверка сохранения/запуска тестовых трупов, отсутствия AI/коллайдеров, ссылок и сохранности ручных параметров. `Previews/test-corpses-flashlight.png` — обычный свет; `Previews/test-corpses-inspection.png` — временная подсветка только для осмотра, не сохранённая в сцене.

## Внешние визуальные концепты

`../VisualConcepts/v09/` — используемая серо-розовая палитра сталкера: четыре очищенных листа, раскладка, AlphaValidation, обзор, предыдущие Sprite-ассеты/.meta вне Unity и полные промпты. Текущий просмотр v05 показывает v09; встроенное видео v0.4 сохраняет прежнюю бордовую палитру и отмечено как историческое.

`../VisualConcepts/v07/` — два концепта спящего врага, агрессивно реагирующего на свет. `../VisualConcepts/v08/stalker-pale-example.png` — одиночный пробный светлый сталкер с кровью на конечностях; README рядом содержит полный промпт imagegen. Эти примеры не импортированы в игровой набор. Направление светлой палитры закреплено в `ENEMIES.md`.

`../VisualConcepts/` относительно корня проекта — концепты для обсуждения. Выбранные спрайты v05/v06 скопированы в проект и импортированы. `v05/preview.html` показывает четыре последовательности и взгляд, `v05/README.md` — историю уточнений. `v06/` содержит атаку/гибель, промпты и кадровую раскладку.

`MVP_PLAN.md` — предложенный план развития: объём MVP-1, боевой MVP-2, приоритеты P0/P1/P2, последовательность задач, маршрут рум, правила контрольных точек и критерии проверки. Документ не утверждает, что будущие механики уже реализованы.

`Flashlight02Results.json` — результаты отдельных проверок новой оптики и переключения F. Отчёт не считается окончательным подтверждением версии 0.2; ограничения описаны в `VALIDATION.md`.

`ANIMATION_REFERENCE.md` — эталон качества последующих существ. `Stalker03Results.json` — 16 фактических проверок движения/света; `StalkerCombat04Results.json` — 14 проверок боя, поражения, крови и физических препятствий. `Demos/stalker-demo-v04.mp4` — 40 секунд настоящего Game View; `Demos/recording-summary.json` — размеры/частота/состояния записи; `Demos/gore-preview.png` — кадр из записи. Покадровый исходник и подробная телеметрия находятся в рабочей папке вне проекта.

## Эффекты v0.6.1

| Файл | Назначение |
|---|---|
| `Scripts/BloodRuptureBurst2D.cs` | Временная взвесь/веер до постоянных капель BloodPool |
| `Editor/CombatEffectsRevision.cs` | Адресное обновление VFX, профилей и ссылок GoreBurst |
| `Shapes/MuzzleFlame.asset` | Геометрическая прозрачная маска пламени, включая текстуру-подассет |
| `Docs/CombatEffects061Results.json` | Восемь Edit/Play-проверок |
| `Docs/Demos/combat-effects-recording-v061.json` | Метрики трёх записанных разрывов/света, 450 кадров |
| `Docs/Demos/*v061.mp4`, `combat-*-v061.png` | Сравнение, три профиля, замедленный разрыв и проверенные кадры |

GibMotion2D теперь разделяет планарный импульс и условную высоту/посадку; BloodPool2D — постоянная геометрия и короткое появление при Dead. WeaponDemoRecorder дополнительно пишет телеметрию в рабочую папку, без записи тестового компонента в сцену.

## Свет и поведение v0.6.2

| Файл | Назначение |
|---|---|
| `Scripts/StalkerHitResponse2D.cs` | Временные цвет/сжатие/сдвиг корпуса, корректное восстановление после попадания/смерти |
| `Scripts/BloodHitSpray2D.cs` | Восемь малых направленных капель на .24 с без постоянной лужи |
| `Scripts/PlayerShotShadow2D.cs` | Силуэт капсулы от света дула, fade, отсечение длиной/стеной/радиусом |
| `Editor/CombatFeelSetup.cs` | Подключение реакций/теней, адресная правка VFX/камеры, сохранение overrides |
| `Materials/TransientGlow.shader`, `.mat` | Общий аддитивный материал вспышки |
| `Shapes/MuzzleGlow.asset` | Мягкая процедурная маска пламени, сохранённая редактором |
| `Docs/COMBAT_FEEL_PLAN.md` | Порядок/приоритеты, три варианта попадания, выбранное решение, итог/ограничения |
| `Docs/CombatFeel062Results.json`, `CombatFeel062Edges.json` | 15 основных проверок и две проверки прерывания укуса/потери видимости |
| `Docs/Demos/*v062.mp4`, `combat-*-v062.png`, `combat-feel-recording-v062.json` | Проверенные видео/кадры/метрики света, попаданий и гибели |

ShadowStalker2D дополнен домашним центром/радиусами, Dormant/Returning и stagger; Flashlight2D — AimTowards и параметрами угловой плавности. Остальные существующие файлы и .meta сохраняются; старые маски/записи не удаляются.

`Docs/CombatFeel062Final.json` — финальное направление/ограничение/отсечение тени, состояние сохранённой сцены, полная консоль с отдельно классифицированным служебным предупреждением MCP.

## Файлы v0.7 — свет и укрытия

| Файл/объект | Ответственность |
|---|---|
| Scripts/LocalLamp2D.cs | Настройки локального Light2D, перебои и проверка освещённости для подбора |
| Scripts/LightingDemoRecorder.cs | Только Editor: временная сценарная запись света без сохранения постановки |
| Editor/LightingPlaygroundSetup.cs | Идемпотентная подготовка стенда/префабов/слоя9/контуров |
| Prefabs/CoverCabinet.prefab | Высокий непрозрачный шкаф, коллайдер/ShadowCaster |
| Prefabs/CoverCrate.prefab | Непрозрачный ящик с простыми рейками |
| Prefabs/LowBench.prefab | Низкий стол, layer9, без ShadowCaster |
| Prefabs/EmergencyLamp.prefab | Локальная лампа, корпус/индикатор/Light2D/компонент |
| Scenes/BasementPrototype.unity: Occlusion playground | Четыре пропа, шесть ламп, укрытый сталкер и патроны |
| Scenes/BasementPrototype.unity: Boundary guide | 13 дочерних слабых контуров стен/створки |
| ProjectSettings/TagManager.asset | Имя LowObstacles у физического слоя9 |
| Docs/LIGHTING_PLAYGROUND.md | Текущие правила темноты, параметры и инструкция новичку |
| Docs/Lighting07Results.json | Наблюдения 15 Edit/Play-проверок |
| Docs/Demos/lighting-playground-v07.mp4 | Демонстрация света и обхода шкафа |

Пути Scripts/Editor/Prefabs/Scenes в таблице относительно Assets/EmergencyShift. .meta хранит GUID каждого нового ассета. PlayerInteraction/два Pickup используют общий IsCurrentlyLit; ShotEffect2D регистрирует текущую вспышку. Маски PlayerWeapon2D/Player.prefab, движение ShadowStalker2D и GoreBurst2D учитывают низкие препятствия. Fog/exploration исходников и ресурсов в текущей версии нет.

## v0.7.1 — файлы патруля

| Путь | Назначение |
|---|---|
| Assets/EmergencyShift/Scripts/StalkerPatrol2D.cs | Флаг, область, скорость/пауза, безопасный выбор мирной точки |
| Assets/EmergencyShift/Editor/StalkerPatrol2DEditor.cs | XY-маркеры границ, Undo, помощь/предупреждение Inspector |
| Assets/EmergencyShift/Scripts/ShadowStalker2D.cs | Patrolling, переключение засады/патруля/боя/возврата |
| Assets/EmergencyShift/Scripts/StalkerSpriteAnimator.cs | Прежняя ходьба также для Patrolling |
| Assets/EmergencyShift/Prefabs/ShadowStalker.prefab | Новый компонент, ожидание по умолчанию |
| Assets/EmergencyShift/Scenes/BasementPrototype.unity | Патруль3×4 у Cover Stalker, остальные ждут; ручные параметры сохранены |
| Docs/PATROL_GUIDE.md | Пошаговая настройка области/засады и ограничения |
| Docs/Patrol071Results.json | Наблюдения из Play и проверки сохранённой сцены |

Новые C# файлы сопровождаются .meta с GUID; сторонние пакеты не менялись.

`Docs/Demos/patrol-v071.png` — просмотренный Game View патрульного, с временной камерой/позицией игрока; постановка не сохранена в уровне.

## v0.7.2 — изменённые файлы агрессии

| Файл | Назначение изменения |
|---|---|
| Assets/EmergencyShift/Scripts/ShadowStalker2D.cs | Постоянный HasAggro, обнаружение вокруг тела, отмена возврата, усиленный страх/скорости, TargetDead |
| Assets/EmergencyShift/Scripts/StalkerChasePath2D.cs + .meta | Новый ограниченный кэшируемый A* для активной погони; чистый C# helper |
| Assets/EmergencyShift/Scripts/StalkerPatrol2D.cs | Необязательная область, шаги от текущей позиции, без возвращения к spawn |
| Assets/EmergencyShift/Editor/StalkerPatrol2DEditor.cs | Актуальные подсказки и условные маркеры области |
| Assets/EmergencyShift/Scripts/StalkerSpriteAnimator.cs | Returning исключён из используемых режимов ходьбы |
| Assets/EmergencyShift/Prefabs/ShadowStalker.prefab | Новые скорости/страх и поле Use Patrol Area, устаревшие настройки удалены |
| Assets/EmergencyShift/Scenes/BasementPrototype.unity | Настройки действующих экземпляров с сохранением ручных радиусов/позиций |
| Docs/PATROL_GUIDE.md | Переписанный актуальный гайд без возврата/забывания |
| Docs/Aggro072Results.json | Реальные наблюдения тестов и ограничения |

`Docs/Demos/stalker-fear-v072.png` — постановочный осмотр текущей позы сжатия на увеличенной дистанции света, не запись движения; условия указаны в VALIDATION.md.

## v0.8 — рабочий звук сталкера

| Путь | Назначение |
|---|---|
| Assets/EmergencyShift/Scripts/StalkerAudioSet.cs |Данные ScriptableObject,13 групп/общие лимиты |
| Assets/EmergencyShift/Scripts/StalkerAudio2D.cs |События/контакты/дистанция/панорама/фильтр/бюджеты |
| Assets/EmergencyShift/Scripts/ShadowStalker2D.cs |Уведомления о состоянии/уроне/фазах атаки, прежние правила AI |
| Assets/EmergencyShift/Scripts/GoreBurst2D.cs |FragmentLaunched |
| Assets/EmergencyShift/Scripts/GibMotion2D.cs |FirstLanded |
| Assets/EmergencyShift/Editor/StalkerAudioSetup.cs |Разовая запись ссылок в префаб/сцену, проверка сохранности AI |
| Assets/EmergencyShift/Audio/Stalker/StalkerMembrane.asset + .meta |49 ссылок, лимиты8/2/3 |
| Assets/EmergencyShift/Audio/Stalker/Membrane04/*.wav + .meta |Единственный рабочий набор,49 моно PCM24/48kHz |
| Assets/EmergencyShift/Prefabs/ShadowStalker.prefab |StalkerAudio2D и2 сохранённых дочерних источника/фильтра |
| Assets/EmergencyShift/Scenes/BasementPrototype.unity |Один AudioListener на Main Camera;7 экземпляров наследуют аудиопрефаб |
| Tools/Audio/generate_stalker_membrane_v04.py |Офлайн-производство рабочего набора/2 превью/manifest |
| Tools/Audio/stalker_membrane_dsp.py |Только используемые DSP-функции выбранного тембра |
| Tools/Audio/Reference/approved-idle-01.wav, approved-idle-02.wav |Точные утверждённые спокойные эталоны |
| Tools/Audio/requirements.txt |NumPy2.3.5 только для инструмента |
| Docs/STALKER_AUDIO.md |Подробный гайд новичку, Inspector, C#, границы |
| Docs/STALKER_AUDIO_DIRECTION.md |Краткий актуальный звуковой бриф |
| Docs/Audio/StalkerMembrane04/manifest.json |Seed/уровни/frames/SHA/производственные зависимости |
| Docs/Audio/StalkerMembrane04/Validation.json |Формат/хэши/эталоны/очистка/воспроизводимость |
| Docs/Audio/StalkerMembrane04/RuntimeValidation.json |Ссылки/сохранность параметров/реальные Play и DSP-проверки |
| Docs/Audio/StalkerMembrane04/stalker-full-encounter.wav |Постановочный21с пример, не запись Unity |
| Docs/Audio/StalkerMembrane04/stalker-event-catalogue.wav |Изолированный пример каждой группы |

Отклонённые наборы/генераторы и лишние дубли превью/ZIP удалены. Графика/анимации/исторические видео не очищались вне аудиозадачи.

## v0.8.1 — подбор и аудиокандидаты пистолета

| Файл/папка | Назначение |
|---|---|
|Scripts/IAutoPickup.cs|Контракт автоподбора и PickupKind для звукового события|
|Scripts/PlayerInteraction.cs|Локальный автоподбор, дверь/E, очередь уведомлений|
|Scripts/WeaponLootPickup.cs|Пистолет/патроны; частичная коробка сохраняет остаток|
|Scripts/FlashlightUpgradePickup.cs|Автоподбор модуля с прежними ID/лимитами|
|Scripts/ImpactSurface2D.cs|Материал цели/родителя и бетонный/мясной fallback|
|Scripts/PlayerAudioSet.cs|Общие массивы подбора/выстрела/материалов|
|Scripts/PlayerAudio2D.cs|Подписки, выбор варианта и6 ограниченных каналов|
|Editor/PlayerAudioSetup.cs|Разовая запись ссылок/каналов в активную сцену|
|Audio/PlayerPrototype/*.wav|17 исходников подбора/двух пистолетов/бетона/дерева|
|Audio/PlayerPrototype/PlayerPrototypeAudio.asset|Общие17 новых+4 прежних flesh-hit ссылок|
|Tools/Audio/generate_player_audio_prototype.py|Воспроизводимый локальный NumPy DSP, без сервиса|
|Docs/AUTO_PICKUP_AND_WEAPON_AUDIO.md|Настройка/прослушивание и руководство новичку|
|Docs/Audio/PlayerPrototype/manifest.json|Происхождение, SHA/frames/уровни исходников|
|Docs/Audio/PlayerPrototype/UnityImport.json|Реальный PCM-импорт/ссылки, сохранность AI|
|Docs/Audio/PlayerPrototype/RuntimeValidation.json|Проверки Play/каналов/материалов/настоящих пуль|
|Docs/Audio/PlayerPrototype/Validation.json|WAV/превью/хэши/клиппинг|
|Docs/Audio/PlayerPrototype/*-preview.wav|4 монтажа для прослушивания, не запись Unity|

Scripts/PlayerWeapon2D.cs и BulletProjectile2D.cs публикуют звукособытия успешного выстрела/реального первого столкновения, сохраняя прежний урон/свет. Все пути Scripts/Editor/Audio в таблице относительно Assets/EmergencyShift.

## v0.8.2 — реалистичные оружейные пробы

| Файл/папка | Назначение |
|---|---|
|Docs/WEAPON_SOUND_STUDIES.md|6 номеров для выбора, авторство/качество/метод и будущая отдельная гильза|
|Docs/Audio/WeaponRealism02/01…04-*.wav|4 варианта пистолета, по3 события, без добавленных гильз/попаданий|
|Docs/Audio/WeaponRealism02/05…06-*.wav|2 отдельных фоли-примера гильз: плитка/бетон, по3 разных падения|
|Docs/Audio/WeaponRealism02/Sources/*.mp3|6 неизменённых публичных CC0-превью, без авторизации/платного сервиса|
|Docs/Audio/WeaponRealism02/Sources/sources.json|Авторы/страницы/адреса превью/лицензии/SHA/дата|
|Docs/Audio/WeaponRealism02/manifest.json|Точные окна нарезки/уровни/размещение/происхождение|
|Docs/Audio/WeaponRealism02/Validation.json|Декодирование6 WAV, формат/сигнал/границы/хэши|
|Tools/Audio/prepare_weapon_realism_studies.py|Монтаж из сохранённых источников, NumPy2.3.5+явный путь FFmpeg; Unity не меняет|

## v0.8.3 — подключённый пистолет/гильзы

|Путь|Ответственность|
|---|---|
|Assets/EmergencyShift/Audio/PistolIndoor|6 shot+6 casing WAV/их .meta и PistolIndoorAudio.asset|
|Scripts/PlayerAudio2D.cs /PlayerAudioSet.cs|Пулы4shot/8casing, уровни/малые вариации/банки, отдельные RNG|
|Scripts/PlayerCasingEjector2D.cs|ShotFired → одна гильза, bounded pool48|
|Scripts/ShellCasing2D.cs|XY/условная высота/стены/однократная первая посадка/остановка|
|Scripts/GroundAudioSurface2D.cs|Материал floor-region/приоритет, без регионаConcrete|
|Prefabs/PistolCasing.prefab|Готовая геометрическая латунная заглушка/контактная тень|
|Editor/PistolAudioSetup.cs|Явное назначение выбранного набора/префаба/флагов|
|Editor/PlayerAudioSetup.cs|Уникальные сохранённые каналы; миграция4/8 без копирования последней ссылки|
|Scripts/PistolAudioDemoRecorder.cs|Только UNITY_EDITOR:480 кадров Game View+Unity main audio mix, не компонент сохранённой сцены|
|Tools/Audio/prepare_pistol_runtime.py|Обработка выбранных CC0 MP3 в12 WAV, без сервиса|
|Docs/PISTOL_AUDIO_GUIDE.md|Работа/настройки/C#/производство/видео/ограничения|
|Docs/Audio/PistolIndoor/manifest.json|Окна/фильтры/варианты/seed-пределы/источники/SHA|
|Docs/Audio/PistolIndoor/UnityImport.json|12 реальных PCM-импортов/ссылки/параметры7 врагов до/после|
|Docs/Audio/PistolIndoor/RuntimeValidation.json|15 фактических Play/DSP-проверок|
|Docs/Audio/PistolIndoor/Validation.json|WAV/формат/хэши/сигнал/сохранность49 сталкерских WAV|
|Docs/Audio/PistolIndoor/RecordingValidation.json|480 кадров/реальный микс/тайминг выстрелов и посадок/A-B/MP4 QA|
|Docs/Demos/pistol-*-v083.mp4|Сравнение16с и два отдельных8с эпизода, H.264/AAC|

Scripts/Editor/Prefabs в таблице относительно Assets/EmergencyShift. Источники остаются в Docs/Audio/WeaponRealism02/Sources с авторством/CC0; не удалять их ради неверного заявления «сгенерировал оригинальную запись».

## v0.8.4 — плотная вспышка

|Файл|Назначение|
|---|---|
|Assets/EmergencyShift/Materials/MuzzlePlume.shader|UV-маска/единое ядро/оранжевый край/ореол и короткое расширение|
|Assets/EmergencyShift/Materials/MuzzlePlume.mat|Общий материал сохранённого Fire plume|
|Assets/EmergencyShift/Editor/MuzzlePlumeSetup.cs|Адресное обновление только VFX-префаба и сохранение новых полей профилей|
|Assets/EmergencyShift/Prefabs/PistolShotEffect.prefab|Новый Fire plume, явная ссылка ShotEffect2D; прежний Light2D|
|Assets/EmergencyShift/Scripts/ShotEffect2D.cs|Раздельные времена огня/света, MaterialPropertyBlock, impact-ветка|
|Assets/EmergencyShift/Scripts/ShotVisualProfile.cs|Plume Seconds/Width/Length рядом с прежними настройками|
|Docs/MUZZLE_FLASH_GUIDE.md|Настройка/логика/файлы/доказательства/ограничения|
|Docs/Effects/MuzzlePlume/RuntimeValidation.json|12 направлений/профилей, импакт, выстрел/гильза/очистка/сохранность на момент проверки|
|Docs/Effects/MuzzlePlume/RecordingValidation.json|Факты480 кадров/реального микса/совпадающих выстрелов/декодирования MP4|
|Docs/Demos/muzzle-before-after-v084.mp4|16с: сначала прежний эффект, затем новая форма|
|Docs/Demos/muzzle-plume-v084.mp4|8с: только новый эффект|

Новые .meta хранят стабильные GUID шейдера/материала/редакторского инструмента. Старые CombatEffectsRevision/CombatFeelSetup вызывают MuzzlePlumeSetup.Configure, сохраняя новое оформление при подготовке префаба.

Docs/Effects/MuzzlePlume/FinalEditorState.json — финальная read-only проверка компиляции/шейдера после более поздних пользовательских правок сцены; сцену не сохраняет/не откатывает.

## v0.8.5 — синхронная вспышка

ShotEffect2D — общий срок/кривая огня и света, явное выключение перед Destroy. ShotVisualProfile и3 сохранённых профиля — меньший радиус/.095с; WeaponPrototypeSetup — соответствующие дефолты новых профилей. MuzzlePlume.shader — комментарий о синхронизации кривых. Новые Docs/Effects/MuzzleSync/RuntimeValidation.json и RecordingValidation.json фиксируют проверки/запись; Docs/Demos/muzzle-sync-v085.mp4 — актуальные8с. Сцена/расстановка не менялись.

## v0.9 — mouse aim

|Файл|Ответственность|
|---|---|
|Assets/EmergencyShift/Scripts/PlayerAim2D.cs|Точка Input System/XY, валидность ввода, явный сценарный target|
|Assets/EmergencyShift/Scripts/AimReticle2D.cs|UGUI-меш кольца/штрихов, позиция/видимость/возврат курсора|
|Assets/EmergencyShift/Editor/MouseAimSetup.cs|Адресные сохранённые ссылки Player/Flashlight/Weapon и один HUD reticle|
|Assets/EmergencyShift/Scripts/PlayerWeapon2D.cs|Поворот с учётом дула, прямая к target, LastShotDirection/Origin/Target, защита ближней цели|
|Assets/EmergencyShift/Scripts/Flashlight2D.cs|Плавное следование общей цели; сценарные AimAt/AimTowards|
|Assets/EmergencyShift/Scripts/PlayerCasingEjector2D.cs|Выброс по фактическому направлению оружия|
|Assets/EmergencyShift/Prefabs/Player.prefab|Сохранённый PlayerAim2D/явные ссылки компонентов|
|Assets/EmergencyShift/Scenes/BasementPrototype.unity|Камера aim и Aim reticle в прежнем HUD; пользовательская раскладка сохранена|
|Docs/MOUSE_AIM_GUIDE.md|Настройка/правила/архитектура/проверка для новичка|
|Docs/Weapons/MouseAim/RuntimeValidation.json|Реальный Input System/Play, попадания/преграды/сохранность настройки|

Новые .meta сохраняют GUID3 скриптов. BulletProjectile2D/звук/AI/баланс этим этапом не переписывались. WeaponPrototypeSetup подключает новые методы и при первоначальной подготовке оружия.

Docs/Weapons/MouseAim/RecordingValidation.json —480 кадров/основной микс/7 выстрелов во втором проходе/проверка кодирования; Docs/Demos/mouse-aim-v09.mp4 —8с с прицелом.

## v0.10 — мировой звук и подготовка контента

Игровой код в Assets/EmergencyShift/Scripts; ассеты ниже относительно Assets/EmergencyShift.

| Файл/группа | Ответственность |
|---|---|
| DoorAudioSet.cs / DoorAudio2D.cs / Door2D.cs | Банки/2 канала, события движения/реестр дверей |
| Audio/WorldFoley/*.wav + DoorFoley.asset |11 обработанных mono клипов/назначение |
| Editor/WorldFoleySetup.cs / PlayerAudioSetup.cs | Миграция scene-only аудио, уникальные12/8/4/1 каналы |
| Damageable2D.cs | Общий контракт попадания/живого состояния/hit-аудио |
| BulletProjectile2D / ImpactSurface2D / PlayerAudio2D | Получатель/поверхность без зависимости от конкретного врага |
| WeaponDefinition.cs / Settings/PistolDefinition.asset | Боевые данные отдельно от Ammo/HasWeapon |
| PlayerWeapon2D / WeaponLootPickup | Выбор definition/локальных overrides/находка |
| LevelContext2D.cs | Связь с игроком/фонарём своей сцены |
| CrowdAgent2D / NavigationWorkBudget2D | Раздвигание малой группы/≤2A*/кадр |
| GoreSpawnBudget2D / GoreBurst2D |≤32 новых частей/кадр |
| ShadowStalker2D / StalkerSpriteAnimator / StalkerAudio2D / BloodPool2D | Привязка/разная фаза/отключение конечных обновлений |
| EncounterActivation2D.cs / Prefabs/EncounterZone.prefab | Разовое включение неактивных акторов |
| Editor/ProjectReadinessSetup.cs | Адресная миграция с сохранением пользовательских параметров |
| Editor/LevelAuthoringSetup.cs | Создание rig/шаблона/зоны только при отсутствии |
| Prefabs/LevelRig.prefab / Scenes/LevelTemplate.unity | Player/камера/HUD/контекст, пустая редактируемая комната |
| WorldFoleyDemoRecorder / CrowdReadinessProbe | UNITY_EDITOR, временная запись/измерение, не сохранять |
| DevelopmentBuildSmoke / CombatProbeTarget2D | Editor/instrumentation, явный development CLI/отдельная мишень |
| Tools/Audio/prepare_world_foley.py (от корня) | Воспроизводимый монтаж/обработка реальных записей |
| Docs/Audio/WorldFoley (от корня) | CC0/SHA/исходники/manifest/Unity mix/факты |
| Docs/Readiness (от корня) | Сохранность миграции/CPU/скриншоты/регрессия/сборка |
| Docs/ARCHITECTURE_REVIEW.md | Готовность/изменения/ограничения/очередь |
| Docs/LEVEL_AUTHORING_GUIDE.md / WORLD_AUDIO_GUIDE.md | Пошаговые гайды |

Все .meta сохранены. Build binaries/кэши/локальная MCP-конфигурация и токены в Git не включаются.
