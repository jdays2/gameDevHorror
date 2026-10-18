# Карта файлов

Свечение открытых настенных трубок: `Materials/TubeGlow.shader` рисует локальное белое ядро/цветную оболочку/ореол; `Materials/HospitalTubeGlow.mat` хранит общие силы трёх слоёв. Цвет и включение задаёт прежний Bulb/LocalLamp2D. `Materials/HospitalLampHousing.mat` и `HospitalLampHousingMask.asset` исключают свет трубок Blend Style 2 из освещения корпуса; фонарь/обычные источники Style 0 продолжают освещать его. Стандартный URP shader, исходные PNG не менялись. LocalLampConfiguration2D применяет настройки экземпляра без отдельного Update; свечение по-прежнему следует существующему Bulb/LocalLamp2D.

Пути относительно Assets/EmergencyShift. Получены из фактического дерева исходников; основная сцена — MechanicsSandbox.

## Runtime

| Файл | Назначение |
|---|---|
| `Runtime/Combat/Weapons/BulletProjectile2D.cs` | Летящая пуля с проверкой всего отрезка пути, чтобы не проскочить тонкую стену. |
| `Runtime/Combat/Weapons/MagazineWeaponCycle.cs` | Shared loaded/reserve ownership; ordinary reload swaps a magazine atomically. |
| `Runtime/Combat/Weapons/PumpShotgunCycle.cs` | Automatic pump and interruptible single-shell loading; no input, visuals or audio ownership. |
| `Runtime/Combat/Weapons/WeaponCycle.cs` | Instance-owned ammunition/cooldown, also the ordinary single-projectile weapon cycle. |
| `Runtime/Combat/Weapons/WeaponInventory.cs` | Owns independent weapon instances. Definitions are immutable content, snapshots contain only values/IDs. |
| `Runtime/Core/Contracts/Damageable2D.cs` | Combat contract. A projectile does not need to know the enemy species. |
| `Runtime/Core/Contracts/EnemyPresentationSource2D.cs` | EnemyPresentationSource2D |
| `Runtime/Core/Contracts/IAutoPickup.cs` | Ground loot is collected by proximity; doors keep IInteractable/E. |
| `Runtime/Core/Contracts/IInteractable.cs` | Контракт действия по E: двери и тестовые припасы. Наземный лут использует IAutoPickup. |
| `Runtime/Core/Contracts/IThreatLight2D.cs` | A defensive light stimulus. Ordinary decorative lights need not implement it. |
| `Runtime/Definitions/AudioMoodProfile.cs` | Shared tuning and replaceable sounds; runtime perception is owned by the scene director. |
| `Runtime/Definitions/BloodPoolProfile.cs` | Подготовленные процедурные формы крови, stable ID и общие параметры; выбранный вариант принадлежит отдельной смерти. |
| `Runtime/Definitions/DoorAudioSet.cs` | DoorAudioSet |
| `Runtime/Definitions/EnemyDeathAudioSet.cs` | Общий неизменяемый набор клипов разрыва для видовых аудиоадаптеров. |
| `Runtime/Definitions/EnemyDefinition.cs` | Shared tuning only. Current health, aggro and timers belong to an instance. |
| `Runtime/Definitions/EnemyPatrolProfile.cs` | EnemyPatrolProfile |
| `Runtime/Definitions/GoreProfile.cs` | Составы останков и выбор новых смертей; исторические ID могут быть доступны только восстановлению. |
| `Runtime/Definitions/LevelCatalog.cs` | LevelCatalog |
| `Runtime/Definitions/LevelDefinition.cs` | LevelDefinition |
| `Runtime/Definitions/LocalLampSceneSettings.cs` | Inline-настройки света, слышимости и длины отдельной лампы сцены; валидация/затухание. |
| `Runtime/Definitions/LocalLightProfile.cs` | Неизменяемый общий профиль обычных светильников: цвет/радиус/яркость/интервалы мерцания. |
| `Runtime/Definitions/LocalLampAudioSet.cs` | Общие записанные клипы лампы, тихий микс и дистанции слышимости; без состояния проигрывания. |
| `Runtime/Definitions/MagazineWeaponDefinition.cs` | MagazineWeaponDefinition |
| `Runtime/Definitions/PlayerAudioSet.cs` | Pickup cues are live; combat candidates can be auditioned before enabling gameplay audio. |
| `Runtime/Definitions/PlayerWorldAudioSet.cs` | PlayerWorldAudioSet |
| `Runtime/Definitions/ProjectileVisualProfile.cs` | One shared presentation for flying bullets and pellets; no volley or weapon state. |
| `Runtime/Definitions/PumpShotgunDefinition.cs` | Shared shotgun tuning. Each owned weapon gets its own cycle and ammunition. |
| `Runtime/Definitions/ShotVisualProfile.cs` | Общие параметры одного визуального варианта. Не хранит патроны/состояние игрока. |
| `Runtime/Definitions/StalkerAudioSet.cs` | Shared authored audio data: one asset instead of 49 references on every creature. |
| `Runtime/Definitions/StalkerBehaviourProfile.cs` | StalkerBehaviourProfile |
| `Runtime/Definitions/StalkerLocomotionProfile.cs` | Длина цикла шага, опорные паузы и сглаживание поворота; общее неизменяемое описание, без фазы экземпляра. |
| `Runtime/Definitions/WeaponAudioProfile.cs` | Weapon-specific recordings; impact materials remain in the shared world palette. |
| `Runtime/Definitions/WeaponCatalog.cs` | WeaponCatalog |
| `Runtime/Definitions/WeaponDefinition.cs` | Shared weapon data; ammunition/ownership stay on the player, never in this asset. |
| `Runtime/Development/AdaptiveAudioProbe.cs` | Temporary Editor Play probe/real main-mix recorder. Never saved and excluded from builds. |
| `Runtime/Development/CheckpointRegressionProbe.cs` | Opt-in end-to-end disk save/death/reload regression. Never authored in a scene. |
| `Runtime/Development/CombatProbeTarget2D.cs` | Development-only non-Stalker receiver used by smoke tests, never placed in production scenes. |
| `Runtime/Development/CrowdReadinessProbe.cs` | Temporary authored-prefab stress fixture. Records actual CPU markers, not a frame-rate guarantee. |
| `Runtime/Development/DevelopmentBuildSmoke.cs` | Only runs with --es-smoke-test in a development player. Ordinary game startup is unchanged. |
| `Runtime/Development/EmergencyShift.Development.asmdef` | Граница сборки и зависимости |
| `Runtime/Development/EngineeringBenchmark2D.cs` | Opt-in native-player baseline for pursuit and simultaneous death. Never saved in a scene. |
| `Runtime/Development/SandboxRegressionProbe.cs` | Opt-in physical sandbox route, shooting and restart checks. Never saved in a scene. |
| `Runtime/EmergencyShift.Runtime.asmdef` | Граница сборки и зависимости |
| `Runtime/Enemies/Common/CrowdAgent2D.cs` | Small-population local separation. Static walls still decide whether a step is safe. |
| `Runtime/Enemies/Common/EnemyChasePath2D.cs` | Ограниченный кэшируемый A* для погони за углами. Не управляет агрессией или телом. |
| `Runtime/Enemies/Common/EnemyHealth2D.cs` | Instance-owned health. Death is emitted once; no AI, sprite, sound or species dependency. |
| `Runtime/Enemies/Common/EnemyMeleeAttack2D.cs` | Reusable attack timeline and hit delivery. The brain decides when to start/interrupt. |
| `Runtime/Enemies/Common/EnemyMotor2D.cs` | EnemyMotor2D |
| `Runtime/Enemies/Common/EnemyPatrol2D.cs` | Только мирный маршрут: ограниченная область, случайные достижимые точки и паузы. |
| `Runtime/Enemies/Common/EnemyPerception2D.cs` | Shared sensing, independent of the decision to flee, sleep or attack. |
| `Runtime/Enemies/Common/NavigationWorkBudget2D.cs` | Caps expensive path rebuilds across actors, preserving cached steering between requests. |
| `Runtime/Enemies/Stalker/ShadowStalker2D.cs` | Species brain: permanent aggro, delayed light fear, retreat and melee. |
| `Runtime/Enemies/Stalker/StalkerAudio2D.cs` | Two bounded voices per creature. AI events drive reactions; real travel drives feet. |
| `Runtime/Enemies/Stalker/StalkerAudioThreat2D.cs` | Adapter only: it observes AI and actual audible playback; it never activates or resets aggro. |
| `Runtime/Enemies/Stalker/StalkerPresentationSource2D.cs` | Species adapter; a different AI can provide the same presentation facts. |
| `Runtime/Enemies/Stalker/StalkerSpriteAnimator.cs` | Совместимый компонент старых prefab; общее представление в CreatureSpriteAnimator2D. |
| `Runtime/Levels/EncounterActivation2D.cs` | One-shot authorable activation of already placed enemies. It never resets an active fight. |
| `Runtime/Levels/LevelContext2D.cs` | Explicit scene-scoped player binding; no global player singleton or per-frame scene searches. |
| `Runtime/Levels/LevelExit2D.cs` | LevelExit2D |
| `Runtime/Levels/LevelObjectiveController2D.cs` | LevelObjectiveController2D |
| `Runtime/Levels/LevelParticipant2D.cs` | Scene-authored lifetime boundary; the level does not know enemy species. |
| `Runtime/Levels/LevelRunController2D.cs` | Owns completion and run lifetime. Objectives and UI are separate components. |
| `Runtime/Persistence/CardCheckpoint2D.cs` | CardCheckpoint2D |
| `Runtime/Persistence/CheckpointData.cs` | CheckpointData |
| `Runtime/Persistence/CheckpointStore.cs` | Versioned local slot; write a complete temp file before replacing the last good save. |
| `Runtime/Persistence/CheckpointVolume2D.cs` | CheckpointVolume2D |
| `Runtime/Persistence/DoorCheckpoint2D.cs` | DoorCheckpoint2D |
| `Runtime/Persistence/EncounterCheckpoint2D.cs` | EncounterCheckpoint2D |
| `Runtime/Persistence/LevelCheckpointController2D.cs` | LevelCheckpointController2D |
| `Runtime/Persistence/PersistentEntity2D.cs` | PersistentEntity2D |
| `Runtime/Persistence/PlayerCheckpoint2D.cs` | PlayerCheckpoint2D |
| `Runtime/Persistence/StalkerCheckpoint2D.cs` | StalkerCheckpoint2D |
| `Runtime/Persistence/UpgradeCheckpoint2D.cs` | UpgradeCheckpoint2D |
| `Runtime/Persistence/WeaponLootCheckpoint2D.cs` | WeaponLootCheckpoint2D |
| `Runtime/Player/CameraFollow2D.cs` | Мягко следует за игроком после обновления движения. |
| `Runtime/Player/FlashlightUpgrades.cs` | Хранит модули игрока в текущем запуске комнаты; пересчитывает оптику от базы. |
| `Runtime/Player/PlayerAccessCards2D.cs` | Only access tokens for this run; not a general inventory or a save system. |
| `Runtime/Player/PlayerAim2D.cs` | Unsmooth cursor target on the XY floor; independent of the flashlight's rotation. |
| `Runtime/Player/PlayerAudio2D.cs` | Saved, bounded channels for pickup feedback and optional combat audition. |
| `Runtime/Player/PlayerCasingEjector2D.cs` | One spent casing per actual shot, with a bounded reusable collection of floor props. |
| `Runtime/Player/PlayerFootsteps2D.cs` | Contacts follow real travel, so walking into a wall or teleporting produces no fake steps. |
| `Runtime/Player/PlayerInteraction.cs` | E selects a door; nearby loot collects automatically with queued HUD feedback. |
| `Runtime/Player/PlayerLife2D.cs` | Два попадания, короткая защита после удара, поражение и перезапуск комнаты по R. |
| `Runtime/Player/PlayerMotor2D.cs` | Читает WASD и перемещает капсулу через физику Unity. |
| `Runtime/Player/PlayerUtilityAudio2D.cs` | Single saved channel for flashlight switch and empty trigger; no synthetic pickup beeps. |
| `Runtime/Player/PlayerWeapon2D.cs` | Player input, common aiming and projectile presentation; definitions create independent firing cycles. |
| `Runtime/Presentation/Audio/AudioThreatEmitter2D.cs` | Small scene-filtered registry. New enemy adapters expose state without changing the music director. |
| `Runtime/Presentation/Audio/EnemyDeathAudioVariants.cs` | Общая история выбора по набору на время Play; исключение немедленного повтора между особями. |
| `Runtime/Presentation/Audio/LevelAudioDirector2D.cs` | Scene-local layered atmosphere. Only perceived threats affect it; AI remains independent. |
| `Runtime/Presentation/Visuals/BloodHitSpray2D.cs` | Малый направленный выброс при попадании; не создаёт постоянную лужу живому врагу. |
| `Runtime/Presentation/Visuals/BloodPool2D.cs` | Подготовленные процедурные формы, выбор/масштаб на смерть и короткая анимация до оседания; поддержка старой круговой крови. |
| `Runtime/Presentation/Visuals/BloodRuptureBurst2D.cs` | Быстрый веер крови: летящие штрихи приходят в позиции постоянных капель лужи. |
| `Runtime/Presentation/Visuals/CreatureGroundShadow2D.cs` | Чёткие тени полного текущего силуэта, включая ноги, в плоской 2D-сцене. |
| `Runtime/Presentation/Visuals/EnemyDeathEffects2D.cs` | One death notification produces one blood/gib burst, independently of the AI species. |
| `Runtime/Presentation/Visuals/EnemyHitResponse2D.cs` | Короткая реакция изображения на пулю; здоровье и решения остаются вне представления. |
| `Runtime/Presentation/Visuals/GibMotion2D.cs` | Планарный импульс + условная высота: быстрый выброс, удар об пол и остановка. |
| `Runtime/Presentation/Visuals/GoreBurst2D.cs` | Одноразовый разрыв тела. Лужа и куски независимы от игрового поведения врага. |
| `Runtime/Presentation/Visuals/GoreSpawnBudget2D.cs` | Spread simultaneous corpse fragment construction across frames; never remove permanent blood. |
| `Runtime/Presentation/Visuals/PlayerShotShadow2D.cs` | Короткая проекция силуэта капсулы от настоящего света дула; плоское приближение высоты. |
| `Runtime/Presentation/Visuals/RemainsPartIdentity2D.cs` | Settled presentation data. No AI, physics or Update after landing. |
| `Runtime/Presentation/Visuals/RemainsRecord2D.cs` | Scene-owned settled remains. It survives the actor; no AI, damage or playback logic. |
| `Runtime/Presentation/Visuals/RemainsSnapshot.cs` | RemainsSnapshot |
| `Runtime/Presentation/Visuals/RemainsVisualStyle.cs` | Общее визуальное правило для трупов, кусков и редакторских образцов. |
| `Runtime/Presentation/Visuals/ShellCasing2D.cs` | Small top-down casing: planar motion plus virtual height, one first-floor-contact sound. |
| `Runtime/Presentation/Visuals/ShotEffect2D.cs` | Огонь и свет дула затухают вместе; штрихи используются для попаданий. |
| `Runtime/Sandbox/SandboxAmmoSupply2D.cs` | Reusable sandbox supply; refills reserve through the normal inventory limits. |
| `Runtime/Sandbox/SandboxHealthZone2D.cs` | Temporary shooting-range health; leaving preserves the health held before entry. |
| `Runtime/Sandbox/SandboxSession2D.cs` | Explicit sandbox shortcuts. No campaign objectives or checkpoint slot. |
| `Runtime/Sandbox/SandboxStationaryStalker2D.cs` | Anchors a real Stalker target; its hit, sound and death pipeline keeps running. |
| `Runtime/UI/AimReticle2D.cs` | Saved HUD graphic: a small ring/ticks at the pointer, with no enemy highlighting. |
| `Runtime/UI/LevelCompletionView2D.cs` | LevelCompletionView2D |
| `Runtime/UI/PrototypeHud.cs` | Отображает состояние игрока, одну контекстную подсказку и результат подбора. |
| `Runtime/World/Doors/Door2D.cs` | Двигает створку двери; коллайдер и тень двигаются вместе с ней. |
| `Runtime/World/Doors/DoorAccess2D.cs` | Optional lock. The door still owns interaction, movement, collision and audio. |
| `Runtime/World/Doors/DoorAudio2D.cs` | Recorded rail movement follows actual door events; two saved voices, no timed fake close. |
| `Runtime/World/Lighting/Flashlight2D.cs` | Управляет направлением, оптикой и обоими источниками света фонаря. |
| `Runtime/World/Lighting/LocalLampConfiguration2D.cs` | Применение Settings экземпляра и масштабирование Fixture с сохранением крепления; Gizmos радиусов. |
| `Runtime/World/Lighting/LocalLamp2D.cs` | Слабый локальный свет. Помогает видеть/подбирать, но не заменяет защитный луч фонаря. |
| `Runtime/World/Lighting/LocalLampAudio2D.cs` | Два голоса, XY-затухание/стена, гул и контакты по фактической яркости; cleanup экземпляра. |
| `Runtime/World/Loot/AccessCardPickup2D.cs` | AccessCardPickup2D |
| `Runtime/World/Loot/FlashlightUpgradePickup.cs` | Proximity pickup; preserves repeatable IDs and upgrade caps. |
| `Runtime/World/Loot/WeaponLootPickup.cs` | Automatic ground loot; full capacity leaves ammo on the floor. |
| `Runtime/World/Surfaces/GroundAudioSurface2D.cs` | Optional floor region; independent from the material of a bullet's target. |
| `Runtime/World/Surfaces/ImpactSurface2D.cs` | Optional collider/parent override; untagged scenery is concrete, Stalkers are flesh. |

| `Runtime/Definitions/SleeperAudioProfile.cs` | SleeperAudioProfile — второй вид / общая инфраструктура представления и проверка. |
| `Runtime/Definitions/SleeperBehaviourProfile.cs` | SleeperBehaviourProfile — второй вид / общая инфраструктура представления и проверка. |
| `Runtime/Development/SleeperRegressionProbe.cs` | SleeperRegressionProbe — второй вид / общая инфраструктура представления и проверка. |
| `Runtime/Enemies/Sleeper/Sleeper2D.cs` | Sleeper2D — второй вид / общая инфраструктура представления и проверка. |
| `Runtime/Enemies/Sleeper/SleeperAudio2D.cs` | SleeperAudio2D — второй вид / общая инфраструктура представления и проверка. |
| `Runtime/Enemies/Sleeper/SleeperAudioThreat2D.cs` | SleeperAudioThreat2D — второй вид / общая инфраструктура представления и проверка. |
| `Runtime/Enemies/Sleeper/SleeperPresentationSource2D.cs` | SleeperPresentationSource2D — второй вид / общая инфраструктура представления и проверка. |
| `Runtime/Persistence/SleeperCheckpoint2D.cs` | SleeperCheckpoint2D — второй вид / общая инфраструктура представления и проверка. |
| `Runtime/Presentation/Visuals/CreatureSpriteAnimator2D.cs` | CreatureSpriteAnimator2D — второй вид / общая инфраструктура представления и проверка. |

## Editor

| Файл | Назначение |
|---|---|
| `Editor/Authoring/EnvironmentLibraryReviewBuilder.cs` | One-time library authoring; the saved scene owns all later placement edits. |
| `Editor/Authoring/EnvironmentReviewBuilder.cs` | Creates an editable prop review room once; never overwrites existing scenes or prefabs. |
| `Editor/Authoring/EnvironmentSpriteAuthoring.cs` | Shared offline sprite import for silhouette-based URP shadows. |
| `Editor/Authoring/FloorMarkingsAuthoring.cs` | Offline paint-mask rasterizer and prefab authoring. No runtime generation or scene replacement. |
| `Editor/Authoring/LevelAuthoringSetup.cs` | Creates reusable scene-authored scaffolding once; never replaces the user's playable level. |
| `Editor/Authoring/LevelPersistenceAuthoring.cs` | Explicit authoring command. Adds metadata/components; never rebuilds room geometry. |
| `Editor/Authoring/SandboxSceneBuilder.cs` | One-time authoring of editable test rooms; never runs at Play or replaces an existing sandbox. |
| `Editor/Authoring/ShotgunIntegrationAuthoring.cs` | Adds weapon fixtures to the existing range; never generates or replaces room geometry. |
| `Editor/EmergencyShift.Editor.asmdef` | Граница сборки и зависимости |
| `Editor/Inspectors/EnemyPatrol2DEditor.cs` | Границы патруля редактируются на сцене; данные остаются на конкретном враге. |
| `Editor/Inspectors/LevelAudioDirectorEditor.cs` | LevelAudioDirectorEditor |

## Tests

| Файл | Назначение |
|---|---|
| `Tests/EditMode/EmergencyShift.EditModeTests.asmdef` | Граница сборки и зависимости |
| `Tests/EditMode/BloodPoolTests.cs` | Точная форма/transform после JSON, независимость экземпляров и старые снимки без variant ID. |
| `Tests/EditMode/GoreCompositionTests.cs` | Новые смерти без повторных ног/старого тела, 8–10 кусков плоти и исключение архивных составов. |
| `Tests/EditMode/EnemySystemsTests.cs` | EnemySystemsTests |
| `Tests/EditMode/MagazineWeaponTests.cs` | MagazineWeaponTests |
| `Tests/EditMode/LocalLampSceneSettingsTests.cs` | Независимость радиусов/отключение звука, ровный свет и валидация входных настроек. |
| `Tests/EditMode/LocalLampAudioTests.cs` | Близкая слышимость, плавное затухание и неизменность общего микса. |
| `Tests/EditMode/PersistenceTests.cs` | PersistenceTests |
| `Tests/EditMode/WeaponCycleTests.cs` | WeaponCycleTests |

## Настройки и сцены

| Файл | Тип |
|---|---|
| `Data/Audio/CreatureRupture06.asset` | Три общих разрыва смерти Сталкера и Спящего. |
| `Data/Audio/HospitalLampQuiet.asset` | Тихий микс записанных ламп, слышимость .35–2 единицы. |
| `Data/Audio/PistolMagazine.asset` | ScriptableObject / настройки рендера |
| `Data/Audio/ShotgunPump.asset` | ScriptableObject / настройки рендера |
| `Data/Audio/SurvivalAudio.asset` | ScriptableObject / настройки рендера |
| `Data/Enemies/Patrol-Default.asset` | ScriptableObject / настройки рендера |
| `Data/Enemies/Patrol-FirstShift.asset` | ScriptableObject / настройки рендера |
| `Data/Enemies/SandboxTarget-20HP.asset` | ScriptableObject / настройки рендера |
| `Data/Enemies/SandboxTarget-2HP.asset` | ScriptableObject / настройки рендера |
| `Data/Enemies/SandboxTarget-6HP.asset` | ScriptableObject / настройки рендера |
| `Data/Enemies/Stalker-Default.asset` | ScriptableObject / настройки рендера |
| `Data/Enemies/Stalker-Light-BasementPrototype.asset` | ScriptableObject / настройки рендера |
| `Data/Enemies/Stalker-Light-Default.asset` | ScriptableObject / настройки рендера |
| `Data/Gore/StalkerRupture.asset` | ScriptableObject / настройки рендера |
| `Data/Levels/Campaign.asset` | ScriptableObject / настройки рендера |
| `Data/Levels/Weapons.asset` | ScriptableObject / настройки рендера |
| `Data/Rendering/Prototype2DRenderer.asset` | ScriptableObject / настройки рендера |
| `Data/Rendering/PrototypeURP.asset` | ScriptableObject / настройки рендера |
| `Data/Weapons/CommonProjectile.asset` | ScriptableObject / настройки рендера |
| `Data/Weapons/PistolDefinition.asset` | ScriptableObject / настройки рендера |
| `Data/Weapons/PumpShotgun.asset` | ScriptableObject / настройки рендера |
| `Data/Weapons/ShotgunVisual.asset` | ScriptableObject / настройки рендера |
| `Scenes/ArtReview/EnvironmentLibraryReview.unity` | Редактируемая сцена |
| `Scenes/ArtReview/EnvironmentPropsDarkReview.unity` | Редактируемая сцена |
| `Scenes/ArtReview/EnvironmentPropsReview.unity` | Редактируемая сцена |
| `Scenes/ArtReview/FloorMarkingsReview.unity` | Редактируемая сцена |
| `Scenes/BasementPrototype.unity` | Редактируемая сцена |
| `Scenes/LevelTemplate.unity` | Редактируемая сцена |
| `Scenes/MechanicsSandbox.unity` | Редактируемая сцена |

| `Tests/EditMode/SleeperTests.cs` | Неполные/противоречивые checkpoint payload второго вида и ещё не активированная особь. |

| `Tests/EditMode/EnemyDeathAudioTests.cs` | Повторы между потребителями, независимость банков, пустые/единичные клипы и неизменность SO. |

## Процедурная внутренняя отделка стен

`Tools/Environment/prepare_wall_finishes_01.py` создаёт 9 исходников straight/corner/end и provenance. `Art/Environment/WallFinishes01` / `Prefabs/Environment/WallFinishes01` — три семейства, FlatLit, без Runtime-компонентов. Сохранённая группа MechanicsSandbox владеет расстановкой; физические преграды остаются на прежних Wall/Door. [Авторинг](Guides/INTERIOR_WALL_FINISHES.md).
