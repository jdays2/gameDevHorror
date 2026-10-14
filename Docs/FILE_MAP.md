# Карта файлов

Здесь описаны файлы, которыми мы управляем, и назначение создаваемых Unity групп. Файлы сторонних пакетов и регенерируемый кеш не являются нашим исходным кодом.

## Корень проекта

| Файл/папка | Назначение |
|---|---|
| `README.md` | Запуск проекта и порядок чтения документации |
| `AGENTS.md` | Требования к работе и дальнейшим изменениям |
| `CHANGELOG.md` | История изменения поведения и структуры |
| `.gitignore` | Исключает регенерируемые и личные файлы из Git |
| `.gitattributes` | Нормализует текстовые исходники и отмечает растровые/видео-файлы как бинарные |
| `Docs/PROGRESS.md` | Краткий текущий прогресс, проверки и очередь задач для репозитория |
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
| `PlayerInteraction.cs` | Выбор одной двери/предмета, локальный поиск, обработка E и уведомление |
| `IInteractable.cs` | Общий контракт доступности, подсказки и действия двери/предмета |
| `FlashlightUpgradePickup.cs` | Настраиваемый наземный модуль, ограничения подбора и однократное исчезновение |
| `FlashlightUpgrades.cs` | Уникальные модули на игроке и пересчёт оптики от базовых параметров |
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
| `Prefabs/FlashlightRangeUpgrade.prefab` | Наземный модуль дальности +0.8, два lit-плейсхолдера и триггер; настройки подбора в Inspector |
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
