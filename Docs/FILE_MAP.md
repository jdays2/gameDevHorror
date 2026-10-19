# Карта файлов

Пути относительно Assets/EmergencyShift. Карта построена по фактическим файлам 2026-10-18. Ответственность систем и владение данными описаны в [архитектуре](ARCHITECTURE.md).

Графика остаётся по прежним адресам в `Art`; единый стиль описан в [ART_DIRECTION](ART_DIRECTION.md), реестр 80 действующих замен и 25 возвращённых изображений крови и исходники — в [SimplifiedArt01](../Concepts/Used/Visual/SimplifiedArt01/README.md). GUID сохранены, переносов рабочих ресурсов нет.

## Runtime

### Runtime/Combat/Weapons

- `Runtime/Combat/Weapons/BulletProjectile2D.cs`
- `Runtime/Combat/Weapons/MagazineWeaponCycle.cs`
- `Runtime/Combat/Weapons/PumpShotgunCycle.cs`
- `Runtime/Combat/Weapons/WeaponCycle.cs`
- `Runtime/Combat/Weapons/WeaponInventory.cs`

### Runtime/Core/Contracts

- `Runtime/Core/Contracts/Damageable2D.cs`
- `Runtime/Core/Contracts/EnemyPresentationSource2D.cs`
- `Runtime/Core/Contracts/IAutoPickup.cs`
- `Runtime/Core/Contracts/IInteractable.cs`
- `Runtime/Core/Contracts/IThreatLight2D.cs`

### Runtime/Core

- `Runtime/Core/GameplayInputBlock.cs`
- `Runtime/Core/WorldMotionPause.cs`

### Runtime/Definitions

- `Runtime/Definitions/AudioMoodProfile.cs`
- `Runtime/Definitions/BloodPoolProfile.cs`
- `Runtime/Definitions/CameraFramingProfile.cs`
- `Runtime/Definitions/DoorAudioSet.cs`
- `Runtime/Definitions/EnemyDeathAudioSet.cs`
- `Runtime/Definitions/EnemyDefinition.cs`
- `Runtime/Definitions/EnemyPatrolProfile.cs`
- `Runtime/Definitions/GameplayHudStyle.cs`
- `Runtime/Definitions/GoreProfile.cs`
- `Runtime/Definitions/LevelCatalog.cs`
- `Runtime/Definitions/LevelDefinition.cs`
- `Runtime/Definitions/LocalLampAudioSet.cs`
- `Runtime/Definitions/LocalLampSceneSettings.cs`
- `Runtime/Definitions/LocalLightProfile.cs`
- `Runtime/Definitions/MagazineWeaponDefinition.cs`
- `Runtime/Definitions/PlayerAudioSet.cs`
- `Runtime/Definitions/PlayerWorldAudioSet.cs`
- `Runtime/Definitions/ProjectileVisualProfile.cs`
- `Runtime/Definitions/PumpShotgunDefinition.cs`
- `Runtime/Definitions/ShotVisualProfile.cs`
- `Runtime/Definitions/SleeperAudioProfile.cs`
- `Runtime/Definitions/SleeperBehaviourProfile.cs`
- `Runtime/Definitions/StalkerAudioSet.cs`
- `Runtime/Definitions/StalkerBehaviourProfile.cs`
- `Runtime/Definitions/StalkerLocomotionProfile.cs`
- `Runtime/Definitions/WeaponAudioProfile.cs`
- `Runtime/Definitions/WeaponCatalog.cs`
- `Runtime/Definitions/WeaponDefinition.cs`

### Runtime/Development

- `Runtime/Development/AdaptiveAudioProbe.cs`
- `Runtime/Development/CheckpointRegressionProbe.cs`
- `Runtime/Development/CombatProbeTarget2D.cs`
- `Runtime/Development/CrowdReadinessProbe.cs`
- `Runtime/Development/DevelopmentBuildSmoke.cs`
- `Runtime/Development/EngineeringBenchmark2D.cs`
- `Runtime/Development/EnvironmentLoadProbe.cs`
- `Runtime/Development/SandboxRegressionProbe.cs`
- `Runtime/Development/SleeperRegressionProbe.cs`

### Runtime/Dialogue

- `Runtime/Dialogue/DialogueInteraction2D.cs`
- `Runtime/Dialogue/DialogueRun.cs`
- `Runtime/Dialogue/DialogueSequence.cs`
- `Runtime/Dialogue/DialogueSession.cs`
- `Runtime/Dialogue/DialogueStyle.cs`

### Runtime/Enemies/Common

- `Runtime/Enemies/Common/CrowdAgent2D.cs`
- `Runtime/Enemies/Common/EnemyChasePath2D.cs`
- `Runtime/Enemies/Common/EnemyHealth2D.cs`
- `Runtime/Enemies/Common/EnemyMeleeAttack2D.cs`
- `Runtime/Enemies/Common/EnemyMotor2D.cs`
- `Runtime/Enemies/Common/EnemyPatrol2D.cs`
- `Runtime/Enemies/Common/EnemyPerception2D.cs`
- `Runtime/Enemies/Common/NavigationWorkBudget2D.cs`

### Runtime/Enemies/Sleeper

- `Runtime/Enemies/Sleeper/Sleeper2D.cs`
- `Runtime/Enemies/Sleeper/SleeperAudio2D.cs`
- `Runtime/Enemies/Sleeper/SleeperAudioThreat2D.cs`
- `Runtime/Enemies/Sleeper/SleeperPresentationSource2D.cs`

### Runtime/Enemies/Stalker

- `Runtime/Enemies/Stalker/ShadowStalker2D.cs`
- `Runtime/Enemies/Stalker/StalkerAudio2D.cs`
- `Runtime/Enemies/Stalker/StalkerAudioThreat2D.cs`
- `Runtime/Enemies/Stalker/StalkerPresentationSource2D.cs`
- `Runtime/Enemies/Stalker/StalkerSpriteAnimator.cs`

### Runtime/Levels

- `Runtime/Levels/EncounterActivation2D.cs`
- `Runtime/Levels/LevelContext2D.cs`
- `Runtime/Levels/LevelExit2D.cs`
- `Runtime/Levels/LevelObjectiveController2D.cs`
- `Runtime/Levels/LevelParticipant2D.cs`
- `Runtime/Levels/LevelRunController2D.cs`

### Runtime/Persistence

- `Runtime/Persistence/CardCheckpoint2D.cs`
- `Runtime/Persistence/CheckpointData.cs`
- `Runtime/Persistence/CheckpointStore.cs`
- `Runtime/Persistence/CheckpointVolume2D.cs`
- `Runtime/Persistence/DoorCheckpoint2D.cs`
- `Runtime/Persistence/EncounterCheckpoint2D.cs`
- `Runtime/Persistence/LevelCheckpointController2D.cs`
- `Runtime/Persistence/PersistentEntity2D.cs`
- `Runtime/Persistence/PlayerCheckpoint2D.cs`
- `Runtime/Persistence/SleeperCheckpoint2D.cs`
- `Runtime/Persistence/StalkerCheckpoint2D.cs`
- `Runtime/Persistence/UpgradeCheckpoint2D.cs`
- `Runtime/Persistence/WeaponLootCheckpoint2D.cs`

### Runtime/Player

- `Runtime/Player/CameraFollow2D.cs`
- `Runtime/Player/FlashlightUpgrades.cs`
- `Runtime/Player/PlayerAccessCards2D.cs`
- `Runtime/Player/PlayerAim2D.cs`
- `Runtime/Player/PlayerAudio2D.cs`
- `Runtime/Player/PlayerCasingEjector2D.cs`
- `Runtime/Player/PlayerFootsteps2D.cs`
- `Runtime/Player/PlayerInteraction.cs`
- `Runtime/Player/PlayerLife2D.cs`
- `Runtime/Player/PlayerMotor2D.cs`
- `Runtime/Player/PlayerUtilityAudio2D.cs`
- `Runtime/Player/PlayerWeapon2D.cs`

### Runtime/Presentation/Audio

- `Runtime/Presentation/Audio/AudioThreatEmitter2D.cs`
- `Runtime/Presentation/Audio/EnemyDeathAudioVariants.cs`
- `Runtime/Presentation/Audio/LevelAudioDirector2D.cs`

### Runtime/Presentation/Visuals

- `Runtime/Presentation/Visuals/BloodHitSpray2D.cs`
- `Runtime/Presentation/Visuals/BloodPool2D.cs`
- `Runtime/Presentation/Visuals/BloodRuptureBurst2D.cs`
- `Runtime/Presentation/Visuals/CreatureGroundShadow2D.cs`
- `Runtime/Presentation/Visuals/CreatureSpriteAnimator2D.cs`
- `Runtime/Presentation/Visuals/EnemyDeathEffects2D.cs`
- `Runtime/Presentation/Visuals/EnemyHitResponse2D.cs`
- `Runtime/Presentation/Visuals/GibMotion2D.cs`
- `Runtime/Presentation/Visuals/GoreBurst2D.cs`
- `Runtime/Presentation/Visuals/GoreSpawnBudget2D.cs`
- `Runtime/Presentation/Visuals/PlayerShotShadow2D.cs`
- `Runtime/Presentation/Visuals/RemainsPartIdentity2D.cs`
- `Runtime/Presentation/Visuals/RemainsRecord2D.cs`
- `Runtime/Presentation/Visuals/RemainsSnapshot.cs`
- `Runtime/Presentation/Visuals/RemainsVisualStyle.cs`
- `Runtime/Presentation/Visuals/ShellCasing2D.cs`
- `Runtime/Presentation/Visuals/ShotEffect2D.cs`

### Runtime/Sandbox

- `Runtime/Sandbox/SandboxAmmoSupply2D.cs`
- `Runtime/Sandbox/SandboxHealthZone2D.cs`
- `Runtime/Sandbox/SandboxSession2D.cs`
- `Runtime/Sandbox/SandboxStationaryStalker2D.cs`

### Runtime/UI

- `Runtime/UI/AimReticle2D.cs`
- `Runtime/UI/DialogueFadeGraphic.cs`
- `Runtime/UI/DialogueView.cs`
- `Runtime/UI/GameplayHudView.cs`
- `Runtime/UI/HudWeaponRow.cs`
- `Runtime/UI/LevelCompletionView2D.cs`
- `Runtime/UI/PrototypeHud.cs`

### Runtime/World/Doors

- `Runtime/World/Doors/Door2D.cs`
- `Runtime/World/Doors/DoorAccess2D.cs`
- `Runtime/World/Doors/DoorAudio2D.cs`

### Runtime/World/Lighting

- `Runtime/World/Lighting/CeilingLightOcclusion2D.cs`
- `Runtime/World/Lighting/EnvironmentHeight2D.cs`
- `Runtime/World/Lighting/EnvironmentShadowLayers.cs`
- `Runtime/World/Lighting/Flashlight2D.cs`
- `Runtime/World/Lighting/FullHeightLightBlocker2D.cs`
- `Runtime/World/Lighting/LocalLamp2D.cs`
- `Runtime/World/Lighting/LocalLampAudio2D.cs`
- `Runtime/World/Lighting/LocalLampConfiguration2D.cs`

### Runtime/World/Loot

- `Runtime/World/Loot/AccessCardPickup2D.cs`
- `Runtime/World/Loot/FlashlightUpgradePickup.cs`
- `Runtime/World/Loot/WeaponLootPickup.cs`

### Runtime/World/Surfaces

- `Runtime/World/Surfaces/GroundAudioSurface2D.cs`
- `Runtime/World/Surfaces/ImpactSurface2D.cs`

## Editor

### Editor/Authoring

- `Editor/Authoring/DialoguePrefabAuthoring.cs`
- `Editor/Authoring/DialogueSandboxAuthoring.cs`
- `Editor/Authoring/EnvironmentHeightEditor.cs`
- `Editor/Authoring/EnvironmentLibraryReviewBuilder.cs`
- `Editor/Authoring/EnvironmentReviewBuilder.cs`
- `Editor/Authoring/EnvironmentSpriteAuthoring.cs`
- `Editor/Authoring/FloorMarkingsAuthoring.cs`
- `Editor/Authoring/GameplayHudAuthoring.cs`
- `Editor/Authoring/LevelAuthoringSetup.cs`
- `Editor/Authoring/LevelPersistenceAuthoring.cs`
- `Editor/Authoring/NarrativeEnvironmentBuilder.cs`
- `Editor/Authoring/SandboxSceneBuilder.cs`
- `Editor/Authoring/ShotgunIntegrationAuthoring.cs`

### Editor/Inspectors

- `Editor/Inspectors/EnemyPatrol2DEditor.cs`
- `Editor/Inspectors/LevelAudioDirectorEditor.cs`

## Tests

### Tests/EditMode

- `Tests/EditMode/BloodPoolTests.cs`
- `Tests/EditMode/CeilingLightOcclusionTests.cs`
- `Tests/EditMode/DialogueDataTests.cs`
- `Tests/EditMode/DialoguePrefabTests.cs`
- `Tests/EditMode/EnemyDeathAudioTests.cs`
- `Tests/EditMode/EnemySystemsTests.cs`
- `Tests/EditMode/EnvironmentHeightShadowTests.cs`
- `Tests/EditMode/EnvironmentPresentationTests.cs`
- `Tests/EditMode/GoreCompositionTests.cs`
- `Tests/EditMode/LocalLampAudioTests.cs`
- `Tests/EditMode/LocalLampSceneSettingsTests.cs`
- `Tests/EditMode/MagazineWeaponTests.cs`
- `Tests/EditMode/PersistenceTests.cs`
- `Tests/EditMode/SleeperTests.cs`
- `Tests/EditMode/StalkerEyeTests.cs`
- `Tests/EditMode/WeaponCycleTests.cs`

## Сцены и контент

- `Scenes/ArtReview/EnvironmentLibraryReview.unity`
- `Scenes/ArtReview/EnvironmentNarrativeStressReview.unity`
- `Scenes/ArtReview/EnvironmentPropsDarkReview.unity`
- `Scenes/ArtReview/EnvironmentPropsReview.unity`
- `Scenes/ArtReview/FloorMarkingsReview.unity`
- `Scenes/BasementPrototype.unity`
- `Scenes/LevelTemplate.unity`
- `Scenes/MechanicsSandbox.unity`

Общие профили: `Data/`, `Settings/`. Переиспользуемые объекты: `Prefabs/`. Рабочие изображения и звуки: `Art/`, `Audio/`, `Materials/`. Исходники и лицензии находятся вне Assets в [Concepts](../Concepts/README.md).
