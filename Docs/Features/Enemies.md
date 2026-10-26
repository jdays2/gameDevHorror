# Враги и останки

Health, общая melee-атака, патруль, обе видовые FSM и расчёт пути имеют чистые личные модели и scoped-фабрики. Остальные config/style и игровые границы ещё переносятся.

EnemyDefinition, EnemyPatrolProfile, SleeperBehaviourProfile и StalkerBehaviourProfile доступны runtime C# только для чтения. Inspector сохраняет прежние поля и значения; модели получают проверенные immutable snapshots. Общие HP/движение и видовые таймеры проверяются до создания brain, не на каждом тике. Нулевая скорость преследования допустима. [Проверки конфигов](../Reports/EnemyConfig15.md).

Архитектурная цель — [ADR-001](../Decisions/001-sod-architecture.md); фактическая карта ниже перенесена из ARCHITECTURE без изменения поведения. При кодовой миграции обновлять её по исходникам.

## Сталкер и повторное использование

Опциональный [сценарный маршрут](ScriptedEncounters.md) временно владеет решениями конкретного Сталкера. До прибытия запрещены все пути включения агрессии; после прибытия сразу возвращается обычный brain. Стандартный prefab и остальные экземпляры сохраняют прежние правила.

| Ответственность | Владелец |
|---|---|
| Текущее HP и однократная смерть | [HealthModel/команды](Health.md), EnemyHealth2D — адаптер |
| Видимость, радиус, воздействие света | EnemyPerception2D, контракт IThreatLight2D |
| Управление телом, обход, путь | EnemyMotor2D — native movement; EnemyChasePathModel + geometry/schedule, прежний EnemyChasePath2D — API adapter |
| Соседи и бюджет поиска | CrowdAgent2D + NavigationWorkBudget2D |
| Тайминг выпада, дистанция/стена, доставка урона | EnemyMeleeModel/Controller/commands, отдельный EnemyMeleeEnvironment2D; EnemyMeleeAttack2D — bridge |
| Мирные точки/паузы | EnemyPatrolModel/Controller, личный RNG и отдельная геометрия; EnemyPatrol2D — scene/DTO/gizmo adapter |
| Решение, какое состояние нужно этому виду | Личные SleeperModel/Controller и StalkerModel/Controller; прежние MB — native/event/DTO adapters |
| Кровь/разрыв по событию смерти | EnemyDeathEffects2D + GoreBurst2D |
| Попадание/тени | EnemyHitResponse2D; CreatureGroundShadow2D через EnemyPresentationSource2D |
| Кадры | CreatureSpriteAnimator2D через EnemyPresentationSource2D; совместимая оболочка StalkerSpriteAnimator |
| Цельные кадры Сталкера | Immutable StalkerSpriteSet: 48 фаз × 9 степеней поджатия; StalkerSpriteAnimator выбирает целое тело по пройденному пути и личному визуальному закрытию. Риг/IK существуют только в Editor для подготовки рисунков. [Контракт и проверка](../../Concepts/Used/Stalker/Locomotion03Sprites/README.md) |
| Голос/контакты и сигнал угрозы | StalkerAudio2D / SleeperAudio2D и их AudioThreat-адаптеры |

Это композиция: следующий AI получает нужные компоненты и собственные решения, не наследуется от Сталкера и не добавляет `if (enemyType==...)` в его код. Общие модули не содержат собственных Update: AI вызывает их в физическом шаге. События отделяют реакции представления от смерти/укуса. Общий проигрыватель кадров извлечён при подключении второго вида, использующего тот же набор анатомии; звук и решения остаются видовыми. Для Спящего общая атака поддерживает фиксированное направление без обходного поворота, прежний режим Сталкера сохранён.

Общая melee-граница находится в `Features/Enemies`: immutable snapshot EnemyDefinition, личные флаги/cooldown, внедрённый clock и команды lifecycle/restore. Controller получает value-наблюдение, проверяет range/arc, затем отдельный Unity LOS/получатель. Урон одноразовый и не зависит от анимационного события; Snapshot/API и оба вида сохраняются. [Реальные проверки двух prefab и ограничения](../Reports/EnemyMelee01.md).

Патруль в той же feature хранит личные route/pause/stall и получает отдельный RNG. Настройки поиска/отступа/застревания задаёт EnemyPatrolProfile; модель читает immutable копию. Physics2D остаётся в environment, область и gizmos — в прежнем компоненте. Snapshot не менял формат. [Проверки и цена переноса](../Reports/EnemyPatrol02.md).

FSM Спящего хранит экспозицию, агрессию, фазы и stagger в SleeperModel с settings/clock. Controller связывает команды с интерфейсами motor/melee, прежний Sleeper2D владеет native lifecycle, событиями и Snapshot. [Локальный живой цикл и ограничения](../Reports/SleeperBrain03.md).

FSM Сталкера хранит состояние, агрессию, страх, ожог и stagger в StalkerModel с settings/clock. Controller передаёт решения в отдельные locomotion/melee-порты; ShadowStalker2D сохраняет native lifecycle, события и DTO, включая миграцию eyeBurned. Подписка сброса патруля освобождается controller.Dispose. [Реальная проверка цикла и цена](../Reports/StalkerBrain04.md).

Световая реакция разделена по расстоянию: до 3.2 units — подход с расширением зрачка/краснотой, до 1.9 units — близкий контакт .33/.45 s, затем EyeBurn, вздрагивание .2 s и отход. Личный чистый StalkerEyeModel накапливает мутность за .3 s после порога и восстанавливает её вне луча за 10 s полной шкалы. При повторном сближении EyeBurn звучит снова, даже при оставшейся мутности. Клип имеет приоритет над бюджетом/шагами. Дверной сценарный разбег сохраняет свои задержки. Точные настройки и кадры — [Сталкер](../Enemies/STALKER.md), [MonsterEyes03](../Reports/MonsterEyes03.md).

Ожог обрабатывается по фактическому свету до выбора обычного/сценарного решения. ScriptedStalkerRoute2D передаёт тот же StalkerObservation в модель актёра, затем исполняет только собственное движение. Триггер маршрута не подменяет Exposed. SetStalkerScriptedControlCommand сбрасывает обычную реакцию/позу, сохраняя дозу и непрерывность светового контакта; переходы страха не являются источником повреждения или EyeBurnStarted.

Расчёт A* и личный кэш находятся в EnemyChasePathModel; Physics2D — в geometry, clock/RNG/квота — в schedule. Сохранены ограничения поиска и прежний recorder. Проверка размеров до умножения защищает массивы от переполнения. [BUG-021, настоящий обход стены и цена](../Reports/Navigation05.md). Параметры расписания и остальные configs/style остаются в общем проходе.

EnemyMotor2D выбирает свободное направление с учётом мирового Radius, как и path/CanStand/fixed-heading. Прежняя ошибка локального probe на масштабированных экземплярах исправлена без изменения размеров и масок; [проверки трёх масштабов](../Reports/MotorGeometry06.md).

## Три вида данных

1. **Общие настройки:** EnemyDefinition (HP, преследование, укус), StalkerBehaviourProfile (страх/отход), EnemyPatrolProfile (скорость/паузы). Несколько врагов ссылаются на один asset. Для разновидности — Duplicate asset и новая ссылка.
2. **Расстановка:** позиция, Detection Radius, Attacks Enabled, Patrol Enabled, Use Patrol Area, Area Offset/Size, маска стен. Индивидуальные данные сцены, не общий SO.
3. **Runtime:** Current HP, Aggro, State, путь, цель патруля, таймеры, патроны, модули. Только экземпляр, никогда не запись в общий asset.

При миграции созданы пять assets: Stalker-Default, два light-профиля с задержкой .45/.33 и два patrol-профиля со скоростью .6/.65. Сохранены значения31 экземпляра, пользовательские правки не усреднены. Отсутствующий обязательный профиль — явная ошибка, а не скрытая подмена дефолтом.

Конфиги существовали раньше: WeaponDefinition, ShotVisualProfile, StalkerAudioSet, PlayerAudioSet, PlayerWorldAudioSet, DoorAudioSet, AudioMoodProfile. Проблемой было неполное покрытие; теперь баланс Сталкера тоже вынесен. Смена дальности фонаря сама по себе не требует нового класса: Flashlight2D остаётся конкретным источником с IThreatLight2D. Будущий флаер потребует другой реализации и выбора стимулов; сейчас его нет.

Общий `EnemyDeathAudioSet` хранит три клипа разрыва. Видовые аудиоадаптеры выбирают их через `EnemyDeathAudioVariants` и играют на своих существующих источниках. Последний выбор хранится в runtime-кэше по набору на время Play, общий asset не меняется. У общего выбора нет знаний о brain/виде/состояниях врага. Новый числовой вариант смерти добавляется в набор; оружие, камера, урон и HUD не меняются.

Кэш выбора теперь внедряется из world scope и освобождается вместе с ним; реестры CrowdAgent и голосов Сталкера принадлежат сцене. Прежние квоты навигации/фрагментов перенесены в экземплярные world services с IFrameClock. EnemyMotor получает отдельный путь через фабрику с часами/бюджетом/RNG. AI-решения, прочие аудиотаймеры/профили и визуальные адаптеры ещё требуют дальнейшей MVC-сверки. [Этап и проверки](../Reports/CodeMigration02.md).

## Останки и профили

**GoreProfile** хранит размер лужи, задержку, импульсы/высоту/гравитацию, tint, материал отделённого глаза и взвешенные варианты раскладки. Оба текущих профиля имеют по три пары eye03/flesh03: веса 7/13 дают 35% с глазом и 65% без. Для новых смертей старые ID отключены через restorationOnly; их составы остаются доступны снимкам, включая прежнее исходное тело.

Оба профиля используют необязательные `GoreVariant.pieces`: индексы рисунков, смещения, масштаб, множители импульса/подлёта и явную роль EyeSurface. С глазом получается 13/15/16 частей (8/9/10 мягких фрагментов); без — 14/17/19 (10/12/14 мягких), конечностей столько же: 3/4/4. Дополнение без глаза — обрывки кровавой мягкой ткани CreatureFlesh04, без суставов/когтей. В новых корпусах нет встроенного второго глаза. `replaceOriginalBody` скрывает прежний death-body и его тень. Четыре формы плоти общие, палитры видовые; renderer/оружие не проверяют вид. Индексы и прежние ID сохранены. Лужи BloodPool используют sorting order −1, ниже останков (0) и выше пола (−10).

**GoreBurst2D** производит разрыв. **RemainsRecord2D** на независимом корне сцены владеет лужей/частями и их снимком. **RemainsPartIdentity2D** связывает осевший кусок с индексом исходного спрайта. Удаление живого actor не удаляет корень останков. Снимок снимается до удаления actor: отдельного каталога останков удалённых/pool-возвращённых actor ещё нет. Пул врагов в проекте не используется.

На смерти GoreBurst читает степень через общий EnemyPresentationSource2D. RemainsEyeSurface применяет её один раз через общий материал/MPB: мёртвый глаз не отслеживает цель и не лечится. RemainsSnapshot version1 сохраняет hasEyeSurface/eyeDamage и точный tint; version0 без этих полей принимается как прежняя поверхность. Неизвестная версия, некорректная доза или отсутствующий необходимый материал отклоняются до изменения живого мира. Общая версия файла checkpoint и saveRevision не меняются. [Миграция и нативный restore](../Reports/MonsterEyes03.md).

Временные hit spray и rupture назначаются сцене источника до добавления/инициализации компонента эффекта; другая активная сцена не меняет их владельца. Они удаляются вместе с исходным уровнем, оставаясь независимыми от отключения actor. BloodPoolProfile отвергает неконечные компоненты размеров/диапазонов/длительностей/цветов до renderer; допустимые настройки и HDR не зажимаются. [Проверки BUG-015/017](../Reports/BloodSafety01.md).

GoreProfile/BloodPoolProfile и вложенные варианты имеют readonly runtime API, проверяют числа, составы и уникальные ID до выбора/генерации. Restoration-only ID сохраняются для загрузки. GoreBurst, шаги и гильзы используют отдельные личные cosmetic RNG из именованного level scope, не меняя gameplay seed factory. Serialized поля, текущие assets и формат checkpoint сохранены. [GoreConfig32](../Reports/GoreConfig32.md).

StalkerLocomotionProfile имеет readonly C# свойства и полную числовую validation до Animator.Awake; прежние serialized names/значения сохранены. Кадры/поза/глаз остаются native presentation, не управляют уроном или AI. [SharedDefinition36](../Reports/SharedDefinition36.md).

Разлёт и emitting/settled принадлежат самостоятельному scene-owned RemainsRecord2D: disable/destroy источника снимает только relay событий, не отменяя части. Матрица позы захватывается на смерти; checkpoint readiness учитывает records независимо от актора. Прежний budget/раскладки/физика и DTO сохранены. Hit reset сохраняет SurfaceZ трупа; геометрия теней окружения не менялась. [NativeRemains37](../Reports/NativeRemains37.md).

## Проверки и связанные документы

Контракт и авторинг: [Враги и останки](../Enemies/STALKER.md). Проверки выбирать по [VALIDATION](../VALIDATION.md); прежние результаты — [Reports/ValidationIndex](../Reports/ValidationIndex.md). Полная архитектурная сверка — текущий этап [Roadmap](https://app.notion.com/p/3ff8d6dc06b681b6950bd8115222669c?pvs=204).
