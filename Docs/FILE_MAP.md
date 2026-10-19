# Карта файлов

Построена по фактическим собственным C# и сценам 2026-10-19. Пути относительно Assets/EmergencyShift. Это навигация, не утверждение использования или полного соответствия SOD; статусы и граф ссылок — [CodeAudit](Reports/CodeAudit.json). Контракты — [ARCHITECTURE](ARCHITECTURE.md).

## Runtime

### Runtime/Combat/Weapons

- [Runtime/Combat/Weapons/BulletProjectile2D.cs](../Assets/EmergencyShift/Runtime/Combat/Weapons/BulletProjectile2D.cs)
- [Runtime/Combat/Weapons/MagazineWeaponCycle.cs](../Assets/EmergencyShift/Runtime/Combat/Weapons/MagazineWeaponCycle.cs)
- [Runtime/Combat/Weapons/PumpShotgunCycle.cs](../Assets/EmergencyShift/Runtime/Combat/Weapons/PumpShotgunCycle.cs)
- [Runtime/Combat/Weapons/WeaponCycle.cs](../Assets/EmergencyShift/Runtime/Combat/Weapons/WeaponCycle.cs)
- [Runtime/Combat/Weapons/WeaponInventory.cs](../Assets/EmergencyShift/Runtime/Combat/Weapons/WeaponInventory.cs)

### Runtime/Core

- [Runtime/Core/GameplayInputBlock.cs](../Assets/EmergencyShift/Runtime/Core/GameplayInputBlock.cs)
- [Runtime/Core/WorldMotionPause.cs](../Assets/EmergencyShift/Runtime/Core/WorldMotionPause.cs)

### Runtime/Core/Commands

- [Runtime/Core/Commands/CommandResult.cs](../Assets/EmergencyShift/Runtime/Core/Commands/CommandResult.cs)
- [Runtime/Core/Commands/ICommand.cs](../Assets/EmergencyShift/Runtime/Core/Commands/ICommand.cs)

### Runtime/Core/Composition

- [Runtime/Core/Composition/AssemblyInfo.cs](../Assets/EmergencyShift/Runtime/Core/Composition/AssemblyInfo.cs)
- [Runtime/Core/Composition/ScenePrefabFactory.cs](../Assets/EmergencyShift/Runtime/Core/Composition/ScenePrefabFactory.cs)
- [Runtime/Core/Composition/SceneRegistry.cs](../Assets/EmergencyShift/Runtime/Core/Composition/SceneRegistry.cs)
- [Runtime/Core/Composition/WorldInstaller.cs](../Assets/EmergencyShift/Runtime/Core/Composition/WorldInstaller.cs)

### Runtime/Core/Contracts

- [Runtime/Core/Contracts/Damageable2D.cs](../Assets/EmergencyShift/Runtime/Core/Contracts/Damageable2D.cs)
- [Runtime/Core/Contracts/EnemyPresentationSource2D.cs](../Assets/EmergencyShift/Runtime/Core/Contracts/EnemyPresentationSource2D.cs)
- [Runtime/Core/Contracts/IAutoPickup.cs](../Assets/EmergencyShift/Runtime/Core/Contracts/IAutoPickup.cs)
- [Runtime/Core/Contracts/IInteractable.cs](../Assets/EmergencyShift/Runtime/Core/Contracts/IInteractable.cs)
- [Runtime/Core/Contracts/IThreatLight2D.cs](../Assets/EmergencyShift/Runtime/Core/Contracts/IThreatLight2D.cs)

### Runtime/Core/Randomness

- [Runtime/Core/Randomness/IRandomSource.cs](../Assets/EmergencyShift/Runtime/Core/Randomness/IRandomSource.cs)
- [Runtime/Core/Randomness/RandomSourceFactory.cs](../Assets/EmergencyShift/Runtime/Core/Randomness/RandomSourceFactory.cs)
- [Runtime/Core/Randomness/SystemRandomSource.cs](../Assets/EmergencyShift/Runtime/Core/Randomness/SystemRandomSource.cs)

### Runtime/Core/Scheduling

- [Runtime/Core/Scheduling/FrameWorkBudget.cs](../Assets/EmergencyShift/Runtime/Core/Scheduling/FrameWorkBudget.cs)

### Runtime/Core/Time

- [Runtime/Core/Time/IFrameClock.cs](../Assets/EmergencyShift/Runtime/Core/Time/IFrameClock.cs)
- [Runtime/Core/Time/IGameClock.cs](../Assets/EmergencyShift/Runtime/Core/Time/IGameClock.cs)
- [Runtime/Core/Time/IWorldTimeScale.cs](../Assets/EmergencyShift/Runtime/Core/Time/IWorldTimeScale.cs)
- [Runtime/Core/Time/UnityFrameClock.cs](../Assets/EmergencyShift/Runtime/Core/Time/UnityFrameClock.cs)
- [Runtime/Core/Time/UnityGameClock.cs](../Assets/EmergencyShift/Runtime/Core/Time/UnityGameClock.cs)
- [Runtime/Core/Time/UnityWorldTimeScale.cs](../Assets/EmergencyShift/Runtime/Core/Time/UnityWorldTimeScale.cs)

### Runtime/Definitions

- [Runtime/Definitions/AudioMoodProfile.cs](../Assets/EmergencyShift/Runtime/Definitions/AudioMoodProfile.cs)
- [Runtime/Definitions/BloodPoolProfile.cs](../Assets/EmergencyShift/Runtime/Definitions/BloodPoolProfile.cs)
- [Runtime/Definitions/CameraFramingProfile.cs](../Assets/EmergencyShift/Runtime/Definitions/CameraFramingProfile.cs)
- [Runtime/Definitions/DoorAccessFeedbackProfile.cs](../Assets/EmergencyShift/Runtime/Definitions/DoorAccessFeedbackProfile.cs)
- [Runtime/Definitions/DoorAudioSet.cs](../Assets/EmergencyShift/Runtime/Definitions/DoorAudioSet.cs)
- [Runtime/Definitions/EnemyDeathAudioSet.cs](../Assets/EmergencyShift/Runtime/Definitions/EnemyDeathAudioSet.cs)
- [Runtime/Definitions/EnemyDefinition.cs](../Assets/EmergencyShift/Runtime/Definitions/EnemyDefinition.cs)
- [Runtime/Definitions/EnemyPatrolProfile.cs](../Assets/EmergencyShift/Runtime/Definitions/EnemyPatrolProfile.cs)
- [Runtime/Definitions/GameplayHudStyle.cs](../Assets/EmergencyShift/Runtime/Definitions/GameplayHudStyle.cs)
- [Runtime/Definitions/GoreProfile.cs](../Assets/EmergencyShift/Runtime/Definitions/GoreProfile.cs)
- [Runtime/Definitions/LevelCatalog.cs](../Assets/EmergencyShift/Runtime/Definitions/LevelCatalog.cs)
- [Runtime/Definitions/LevelDefinition.cs](../Assets/EmergencyShift/Runtime/Definitions/LevelDefinition.cs)
- [Runtime/Definitions/LocalLampAudioSet.cs](../Assets/EmergencyShift/Runtime/Definitions/LocalLampAudioSet.cs)
- [Runtime/Definitions/LocalLampSceneSettings.cs](../Assets/EmergencyShift/Runtime/Definitions/LocalLampSceneSettings.cs)
- [Runtime/Definitions/LocalLightProfile.cs](../Assets/EmergencyShift/Runtime/Definitions/LocalLightProfile.cs)
- [Runtime/Definitions/MagazineWeaponDefinition.cs](../Assets/EmergencyShift/Runtime/Definitions/MagazineWeaponDefinition.cs)
- [Runtime/Definitions/PlayerAudioSet.cs](../Assets/EmergencyShift/Runtime/Definitions/PlayerAudioSet.cs)
- [Runtime/Definitions/PlayerWorldAudioSet.cs](../Assets/EmergencyShift/Runtime/Definitions/PlayerWorldAudioSet.cs)
- [Runtime/Definitions/ProjectileVisualProfile.cs](../Assets/EmergencyShift/Runtime/Definitions/ProjectileVisualProfile.cs)
- [Runtime/Definitions/PumpShotgunDefinition.cs](../Assets/EmergencyShift/Runtime/Definitions/PumpShotgunDefinition.cs)
- [Runtime/Definitions/ShotVisualProfile.cs](../Assets/EmergencyShift/Runtime/Definitions/ShotVisualProfile.cs)
- [Runtime/Definitions/SleeperAudioProfile.cs](../Assets/EmergencyShift/Runtime/Definitions/SleeperAudioProfile.cs)
- [Runtime/Definitions/SleeperBehaviourProfile.cs](../Assets/EmergencyShift/Runtime/Definitions/SleeperBehaviourProfile.cs)
- [Runtime/Definitions/StalkerAudioSet.cs](../Assets/EmergencyShift/Runtime/Definitions/StalkerAudioSet.cs)
- [Runtime/Definitions/StalkerBehaviourProfile.cs](../Assets/EmergencyShift/Runtime/Definitions/StalkerBehaviourProfile.cs)
- [Runtime/Definitions/StalkerLocomotionProfile.cs](../Assets/EmergencyShift/Runtime/Definitions/StalkerLocomotionProfile.cs)
- [Runtime/Definitions/WeaponAudioProfile.cs](../Assets/EmergencyShift/Runtime/Definitions/WeaponAudioProfile.cs)
- [Runtime/Definitions/WeaponCatalog.cs](../Assets/EmergencyShift/Runtime/Definitions/WeaponCatalog.cs)
- [Runtime/Definitions/WeaponDefinition.cs](../Assets/EmergencyShift/Runtime/Definitions/WeaponDefinition.cs)

### Runtime/Development

- [Runtime/Development/AdaptiveAudioProbe.cs](../Assets/EmergencyShift/Runtime/Development/AdaptiveAudioProbe.cs)
- [Runtime/Development/CheckpointRegressionProbe.cs](../Assets/EmergencyShift/Runtime/Development/CheckpointRegressionProbe.cs)
- [Runtime/Development/CombatProbeTarget2D.cs](../Assets/EmergencyShift/Runtime/Development/CombatProbeTarget2D.cs)
- [Runtime/Development/CrowdReadinessProbe.cs](../Assets/EmergencyShift/Runtime/Development/CrowdReadinessProbe.cs)
- [Runtime/Development/DevelopmentBuildSmoke.cs](../Assets/EmergencyShift/Runtime/Development/DevelopmentBuildSmoke.cs)
- [Runtime/Development/EngineeringBenchmark2D.cs](../Assets/EmergencyShift/Runtime/Development/EngineeringBenchmark2D.cs)
- [Runtime/Development/EnvironmentLoadProbe.cs](../Assets/EmergencyShift/Runtime/Development/EnvironmentLoadProbe.cs)
- [Runtime/Development/FurnitureLightBenchmark.cs](../Assets/EmergencyShift/Runtime/Development/FurnitureLightBenchmark.cs)
- [Runtime/Development/FurnitureLightStudy2D.cs](../Assets/EmergencyShift/Runtime/Development/FurnitureLightStudy2D.cs)
- [Runtime/Development/FurnitureLightStudyProfile.cs](../Assets/EmergencyShift/Runtime/Development/FurnitureLightStudyProfile.cs)
- [Runtime/Development/SandboxRegressionProbe.cs](../Assets/EmergencyShift/Runtime/Development/SandboxRegressionProbe.cs)
- [Runtime/Development/SleeperRegressionProbe.cs](../Assets/EmergencyShift/Runtime/Development/SleeperRegressionProbe.cs)

### Runtime/Dialogue

- [Runtime/Dialogue/DialogueInteraction2D.cs](../Assets/EmergencyShift/Runtime/Dialogue/DialogueInteraction2D.cs)
- [Runtime/Dialogue/DialogueRun.cs](../Assets/EmergencyShift/Runtime/Dialogue/DialogueRun.cs)
- [Runtime/Dialogue/DialogueSequence.cs](../Assets/EmergencyShift/Runtime/Dialogue/DialogueSequence.cs)
- [Runtime/Dialogue/DialogueSession.cs](../Assets/EmergencyShift/Runtime/Dialogue/DialogueSession.cs)
- [Runtime/Dialogue/DialogueStyle.cs](../Assets/EmergencyShift/Runtime/Dialogue/DialogueStyle.cs)

### Runtime/Enemies/Common

- [Runtime/Enemies/Common/CrowdAgent2D.cs](../Assets/EmergencyShift/Runtime/Enemies/Common/CrowdAgent2D.cs)
- [Runtime/Enemies/Common/EnemyChasePath2D.cs](../Assets/EmergencyShift/Runtime/Enemies/Common/EnemyChasePath2D.cs)
- [Runtime/Enemies/Common/EnemyChasePathFactory.cs](../Assets/EmergencyShift/Runtime/Enemies/Common/EnemyChasePathFactory.cs)
- [Runtime/Enemies/Common/EnemyHealth2D.cs](../Assets/EmergencyShift/Runtime/Enemies/Common/EnemyHealth2D.cs)
- [Runtime/Enemies/Common/EnemyMeleeAttack2D.cs](../Assets/EmergencyShift/Runtime/Enemies/Common/EnemyMeleeAttack2D.cs)
- [Runtime/Enemies/Common/EnemyMotor2D.cs](../Assets/EmergencyShift/Runtime/Enemies/Common/EnemyMotor2D.cs)
- [Runtime/Enemies/Common/EnemyPatrol2D.cs](../Assets/EmergencyShift/Runtime/Enemies/Common/EnemyPatrol2D.cs)
- [Runtime/Enemies/Common/EnemyPerception2D.cs](../Assets/EmergencyShift/Runtime/Enemies/Common/EnemyPerception2D.cs)
- [Runtime/Enemies/Common/NavigationWorkBudget2D.cs](../Assets/EmergencyShift/Runtime/Enemies/Common/NavigationWorkBudget2D.cs)

### Runtime/Enemies/Sleeper

- [Runtime/Enemies/Sleeper/Sleeper2D.cs](../Assets/EmergencyShift/Runtime/Enemies/Sleeper/Sleeper2D.cs)
- [Runtime/Enemies/Sleeper/SleeperAudio2D.cs](../Assets/EmergencyShift/Runtime/Enemies/Sleeper/SleeperAudio2D.cs)
- [Runtime/Enemies/Sleeper/SleeperAudioThreat2D.cs](../Assets/EmergencyShift/Runtime/Enemies/Sleeper/SleeperAudioThreat2D.cs)
- [Runtime/Enemies/Sleeper/SleeperPresentationSource2D.cs](../Assets/EmergencyShift/Runtime/Enemies/Sleeper/SleeperPresentationSource2D.cs)

### Runtime/Enemies/Stalker

- [Runtime/Enemies/Stalker/ShadowStalker2D.cs](../Assets/EmergencyShift/Runtime/Enemies/Stalker/ShadowStalker2D.cs)
- [Runtime/Enemies/Stalker/StalkerAudio2D.cs](../Assets/EmergencyShift/Runtime/Enemies/Stalker/StalkerAudio2D.cs)
- [Runtime/Enemies/Stalker/StalkerAudioThreat2D.cs](../Assets/EmergencyShift/Runtime/Enemies/Stalker/StalkerAudioThreat2D.cs)
- [Runtime/Enemies/Stalker/StalkerPresentationSource2D.cs](../Assets/EmergencyShift/Runtime/Enemies/Stalker/StalkerPresentationSource2D.cs)
- [Runtime/Enemies/Stalker/StalkerSpriteAnimator.cs](../Assets/EmergencyShift/Runtime/Enemies/Stalker/StalkerSpriteAnimator.cs)

### Runtime/Features/Health

- [Runtime/Features/Health/HealthInstaller.cs](../Assets/EmergencyShift/Runtime/Features/Health/HealthInstaller.cs)

### Runtime/Features/Health/Config

- [Runtime/Features/Health/Config/HealthSettings.cs](../Assets/EmergencyShift/Runtime/Features/Health/Config/HealthSettings.cs)

### Runtime/Features/Health/Logic

- [Runtime/Features/Health/Logic/DamageHealthCommand.cs](../Assets/EmergencyShift/Runtime/Features/Health/Logic/DamageHealthCommand.cs)
- [Runtime/Features/Health/Logic/HealthModel.cs](../Assets/EmergencyShift/Runtime/Features/Health/Logic/HealthModel.cs)
- [Runtime/Features/Health/Logic/HealthModelFactory.cs](../Assets/EmergencyShift/Runtime/Features/Health/Logic/HealthModelFactory.cs)
- [Runtime/Features/Health/Logic/SetHealthCapacityCommand.cs](../Assets/EmergencyShift/Runtime/Features/Health/Logic/SetHealthCapacityCommand.cs)

### Runtime/Features/PlayerInput

- [Runtime/Features/PlayerInput/PlayerInputInstaller.cs](../Assets/EmergencyShift/Runtime/Features/PlayerInput/PlayerInputInstaller.cs)

### Runtime/Features/PlayerInput/Logic

- [Runtime/Features/PlayerInput/Logic/IPlayerInput.cs](../Assets/EmergencyShift/Runtime/Features/PlayerInput/Logic/IPlayerInput.cs)

### Runtime/Features/PlayerInput/View

- [Runtime/Features/PlayerInput/View/InputActionPlayerInput.cs](../Assets/EmergencyShift/Runtime/Features/PlayerInput/View/InputActionPlayerInput.cs)

### Runtime/Levels

- [Runtime/Levels/EncounterActivation2D.cs](../Assets/EmergencyShift/Runtime/Levels/EncounterActivation2D.cs)
- [Runtime/Levels/LevelContext2D.cs](../Assets/EmergencyShift/Runtime/Levels/LevelContext2D.cs)
- [Runtime/Levels/LevelExit2D.cs](../Assets/EmergencyShift/Runtime/Levels/LevelExit2D.cs)
- [Runtime/Levels/LevelObjectiveController2D.cs](../Assets/EmergencyShift/Runtime/Levels/LevelObjectiveController2D.cs)
- [Runtime/Levels/LevelParticipant2D.cs](../Assets/EmergencyShift/Runtime/Levels/LevelParticipant2D.cs)
- [Runtime/Levels/LevelRegistriesInstaller.cs](../Assets/EmergencyShift/Runtime/Levels/LevelRegistriesInstaller.cs)
- [Runtime/Levels/LevelRunController2D.cs](../Assets/EmergencyShift/Runtime/Levels/LevelRunController2D.cs)

### Runtime/Persistence

- [Runtime/Persistence/CardCheckpoint2D.cs](../Assets/EmergencyShift/Runtime/Persistence/CardCheckpoint2D.cs)
- [Runtime/Persistence/CheckpointData.cs](../Assets/EmergencyShift/Runtime/Persistence/CheckpointData.cs)
- [Runtime/Persistence/CheckpointStore.cs](../Assets/EmergencyShift/Runtime/Persistence/CheckpointStore.cs)
- [Runtime/Persistence/CheckpointVolume2D.cs](../Assets/EmergencyShift/Runtime/Persistence/CheckpointVolume2D.cs)
- [Runtime/Persistence/DoorCheckpoint2D.cs](../Assets/EmergencyShift/Runtime/Persistence/DoorCheckpoint2D.cs)
- [Runtime/Persistence/EncounterCheckpoint2D.cs](../Assets/EmergencyShift/Runtime/Persistence/EncounterCheckpoint2D.cs)
- [Runtime/Persistence/LevelCheckpointController2D.cs](../Assets/EmergencyShift/Runtime/Persistence/LevelCheckpointController2D.cs)
- [Runtime/Persistence/PersistentEntity2D.cs](../Assets/EmergencyShift/Runtime/Persistence/PersistentEntity2D.cs)
- [Runtime/Persistence/PlayerCheckpoint2D.cs](../Assets/EmergencyShift/Runtime/Persistence/PlayerCheckpoint2D.cs)
- [Runtime/Persistence/SleeperCheckpoint2D.cs](../Assets/EmergencyShift/Runtime/Persistence/SleeperCheckpoint2D.cs)
- [Runtime/Persistence/StalkerCheckpoint2D.cs](../Assets/EmergencyShift/Runtime/Persistence/StalkerCheckpoint2D.cs)
- [Runtime/Persistence/UpgradeCheckpoint2D.cs](../Assets/EmergencyShift/Runtime/Persistence/UpgradeCheckpoint2D.cs)
- [Runtime/Persistence/WeaponLootCheckpoint2D.cs](../Assets/EmergencyShift/Runtime/Persistence/WeaponLootCheckpoint2D.cs)

### Runtime/Player

- [Runtime/Player/AcquireFlashlightCommand.cs](../Assets/EmergencyShift/Runtime/Player/AcquireFlashlightCommand.cs)
- [Runtime/Player/CameraFollow2D.cs](../Assets/EmergencyShift/Runtime/Player/CameraFollow2D.cs)
- [Runtime/Player/FlashlightOwnership.cs](../Assets/EmergencyShift/Runtime/Player/FlashlightOwnership.cs)
- [Runtime/Player/FlashlightUpgrades.cs](../Assets/EmergencyShift/Runtime/Player/FlashlightUpgrades.cs)
- [Runtime/Player/PlayerAccessCards2D.cs](../Assets/EmergencyShift/Runtime/Player/PlayerAccessCards2D.cs)
- [Runtime/Player/PlayerAim2D.cs](../Assets/EmergencyShift/Runtime/Player/PlayerAim2D.cs)
- [Runtime/Player/PlayerAudio2D.cs](../Assets/EmergencyShift/Runtime/Player/PlayerAudio2D.cs)
- [Runtime/Player/PlayerCasingEjector2D.cs](../Assets/EmergencyShift/Runtime/Player/PlayerCasingEjector2D.cs)
- [Runtime/Player/PlayerFootsteps2D.cs](../Assets/EmergencyShift/Runtime/Player/PlayerFootsteps2D.cs)
- [Runtime/Player/PlayerInteraction.cs](../Assets/EmergencyShift/Runtime/Player/PlayerInteraction.cs)
- [Runtime/Player/PlayerLife2D.cs](../Assets/EmergencyShift/Runtime/Player/PlayerLife2D.cs)
- [Runtime/Player/PlayerMotor2D.cs](../Assets/EmergencyShift/Runtime/Player/PlayerMotor2D.cs)
- [Runtime/Player/PlayerUtilityAudio2D.cs](../Assets/EmergencyShift/Runtime/Player/PlayerUtilityAudio2D.cs)
- [Runtime/Player/PlayerWeapon2D.cs](../Assets/EmergencyShift/Runtime/Player/PlayerWeapon2D.cs)

### Runtime/Presentation/Audio

- [Runtime/Presentation/Audio/AudioThreatEmitter2D.cs](../Assets/EmergencyShift/Runtime/Presentation/Audio/AudioThreatEmitter2D.cs)
- [Runtime/Presentation/Audio/EnemyDeathAudioVariants.cs](../Assets/EmergencyShift/Runtime/Presentation/Audio/EnemyDeathAudioVariants.cs)
- [Runtime/Presentation/Audio/LevelAudioDirector2D.cs](../Assets/EmergencyShift/Runtime/Presentation/Audio/LevelAudioDirector2D.cs)

### Runtime/Presentation/Visuals

- [Runtime/Presentation/Visuals/BloodHitSpray2D.cs](../Assets/EmergencyShift/Runtime/Presentation/Visuals/BloodHitSpray2D.cs)
- [Runtime/Presentation/Visuals/BloodPool2D.cs](../Assets/EmergencyShift/Runtime/Presentation/Visuals/BloodPool2D.cs)
- [Runtime/Presentation/Visuals/BloodRuptureBurst2D.cs](../Assets/EmergencyShift/Runtime/Presentation/Visuals/BloodRuptureBurst2D.cs)
- [Runtime/Presentation/Visuals/CreatureGroundShadow2D.cs](../Assets/EmergencyShift/Runtime/Presentation/Visuals/CreatureGroundShadow2D.cs)
- [Runtime/Presentation/Visuals/CreatureSpriteAnimator2D.cs](../Assets/EmergencyShift/Runtime/Presentation/Visuals/CreatureSpriteAnimator2D.cs)
- [Runtime/Presentation/Visuals/EnemyDeathEffects2D.cs](../Assets/EmergencyShift/Runtime/Presentation/Visuals/EnemyDeathEffects2D.cs)
- [Runtime/Presentation/Visuals/EnemyHitResponse2D.cs](../Assets/EmergencyShift/Runtime/Presentation/Visuals/EnemyHitResponse2D.cs)
- [Runtime/Presentation/Visuals/GibMotion2D.cs](../Assets/EmergencyShift/Runtime/Presentation/Visuals/GibMotion2D.cs)
- [Runtime/Presentation/Visuals/GoreBurst2D.cs](../Assets/EmergencyShift/Runtime/Presentation/Visuals/GoreBurst2D.cs)
- [Runtime/Presentation/Visuals/GoreSpawnBudget2D.cs](../Assets/EmergencyShift/Runtime/Presentation/Visuals/GoreSpawnBudget2D.cs)
- [Runtime/Presentation/Visuals/PlayerShotShadow2D.cs](../Assets/EmergencyShift/Runtime/Presentation/Visuals/PlayerShotShadow2D.cs)
- [Runtime/Presentation/Visuals/RemainsPartIdentity2D.cs](../Assets/EmergencyShift/Runtime/Presentation/Visuals/RemainsPartIdentity2D.cs)
- [Runtime/Presentation/Visuals/RemainsRecord2D.cs](../Assets/EmergencyShift/Runtime/Presentation/Visuals/RemainsRecord2D.cs)
- [Runtime/Presentation/Visuals/RemainsSnapshot.cs](../Assets/EmergencyShift/Runtime/Presentation/Visuals/RemainsSnapshot.cs)
- [Runtime/Presentation/Visuals/RemainsVisualStyle.cs](../Assets/EmergencyShift/Runtime/Presentation/Visuals/RemainsVisualStyle.cs)
- [Runtime/Presentation/Visuals/ShellCasing2D.cs](../Assets/EmergencyShift/Runtime/Presentation/Visuals/ShellCasing2D.cs)
- [Runtime/Presentation/Visuals/ShotEffect2D.cs](../Assets/EmergencyShift/Runtime/Presentation/Visuals/ShotEffect2D.cs)

### Runtime/Sandbox

- [Runtime/Sandbox/SandboxAmmoSupply2D.cs](../Assets/EmergencyShift/Runtime/Sandbox/SandboxAmmoSupply2D.cs)
- [Runtime/Sandbox/SandboxHealthZone2D.cs](../Assets/EmergencyShift/Runtime/Sandbox/SandboxHealthZone2D.cs)
- [Runtime/Sandbox/SandboxSession2D.cs](../Assets/EmergencyShift/Runtime/Sandbox/SandboxSession2D.cs)
- [Runtime/Sandbox/SandboxStationaryStalker2D.cs](../Assets/EmergencyShift/Runtime/Sandbox/SandboxStationaryStalker2D.cs)

### Runtime/UI

- [Runtime/UI/AimReticle2D.cs](../Assets/EmergencyShift/Runtime/UI/AimReticle2D.cs)
- [Runtime/UI/DialogueFadeGraphic.cs](../Assets/EmergencyShift/Runtime/UI/DialogueFadeGraphic.cs)
- [Runtime/UI/DialogueView.cs](../Assets/EmergencyShift/Runtime/UI/DialogueView.cs)
- [Runtime/UI/GameplayHudView.cs](../Assets/EmergencyShift/Runtime/UI/GameplayHudView.cs)
- [Runtime/UI/HudWeaponRow.cs](../Assets/EmergencyShift/Runtime/UI/HudWeaponRow.cs)
- [Runtime/UI/LevelCompletionView2D.cs](../Assets/EmergencyShift/Runtime/UI/LevelCompletionView2D.cs)
- [Runtime/UI/PrototypeHud.cs](../Assets/EmergencyShift/Runtime/UI/PrototypeHud.cs)

### Runtime/World/Doors

- [Runtime/World/Doors/Door2D.cs](../Assets/EmergencyShift/Runtime/World/Doors/Door2D.cs)
- [Runtime/World/Doors/DoorAccess2D.cs](../Assets/EmergencyShift/Runtime/World/Doors/DoorAccess2D.cs)
- [Runtime/World/Doors/DoorAccessFeedback2D.cs](../Assets/EmergencyShift/Runtime/World/Doors/DoorAccessFeedback2D.cs)
- [Runtime/World/Doors/DoorAudio2D.cs](../Assets/EmergencyShift/Runtime/World/Doors/DoorAudio2D.cs)

### Runtime/World/Lighting

- [Runtime/World/Lighting/CeilingLightOcclusion2D.cs](../Assets/EmergencyShift/Runtime/World/Lighting/CeilingLightOcclusion2D.cs)
- [Runtime/World/Lighting/Flashlight2D.cs](../Assets/EmergencyShift/Runtime/World/Lighting/Flashlight2D.cs)
- [Runtime/World/Lighting/FullHeightLightBlocker2D.cs](../Assets/EmergencyShift/Runtime/World/Lighting/FullHeightLightBlocker2D.cs)
- [Runtime/World/Lighting/LocalLamp2D.cs](../Assets/EmergencyShift/Runtime/World/Lighting/LocalLamp2D.cs)
- [Runtime/World/Lighting/LocalLampAudio2D.cs](../Assets/EmergencyShift/Runtime/World/Lighting/LocalLampAudio2D.cs)
- [Runtime/World/Lighting/LocalLampConfiguration2D.cs](../Assets/EmergencyShift/Runtime/World/Lighting/LocalLampConfiguration2D.cs)

### Runtime/World/Loot

- [Runtime/World/Loot/AccessCardPickup2D.cs](../Assets/EmergencyShift/Runtime/World/Loot/AccessCardPickup2D.cs)
- [Runtime/World/Loot/FlashlightPickup2D.cs](../Assets/EmergencyShift/Runtime/World/Loot/FlashlightPickup2D.cs)
- [Runtime/World/Loot/FlashlightUpgradePickup.cs](../Assets/EmergencyShift/Runtime/World/Loot/FlashlightUpgradePickup.cs)
- [Runtime/World/Loot/WeaponLootPickup.cs](../Assets/EmergencyShift/Runtime/World/Loot/WeaponLootPickup.cs)

### Runtime/World/Surfaces

- [Runtime/World/Surfaces/GroundAudioSurface2D.cs](../Assets/EmergencyShift/Runtime/World/Surfaces/GroundAudioSurface2D.cs)
- [Runtime/World/Surfaces/ImpactSurface2D.cs](../Assets/EmergencyShift/Runtime/World/Surfaces/ImpactSurface2D.cs)

## Editor

### Editor/Authoring

- [Editor/Authoring/DialoguePrefabAuthoring.cs](../Assets/EmergencyShift/Editor/Authoring/DialoguePrefabAuthoring.cs)
- [Editor/Authoring/DialogueSandboxAuthoring.cs](../Assets/EmergencyShift/Editor/Authoring/DialogueSandboxAuthoring.cs)
- [Editor/Authoring/EnvironmentLibraryReviewBuilder.cs](../Assets/EmergencyShift/Editor/Authoring/EnvironmentLibraryReviewBuilder.cs)
- [Editor/Authoring/EnvironmentReviewBuilder.cs](../Assets/EmergencyShift/Editor/Authoring/EnvironmentReviewBuilder.cs)
- [Editor/Authoring/EnvironmentSpriteAuthoring.cs](../Assets/EmergencyShift/Editor/Authoring/EnvironmentSpriteAuthoring.cs)
- [Editor/Authoring/FloorMarkingsAuthoring.cs](../Assets/EmergencyShift/Editor/Authoring/FloorMarkingsAuthoring.cs)
- [Editor/Authoring/FurnitureLightStudyBuilder.cs](../Assets/EmergencyShift/Editor/Authoring/FurnitureLightStudyBuilder.cs)
- [Editor/Authoring/GameplayHudAuthoring.cs](../Assets/EmergencyShift/Editor/Authoring/GameplayHudAuthoring.cs)
- [Editor/Authoring/HospitalServicePrefabAuthoring.cs](../Assets/EmergencyShift/Editor/Authoring/HospitalServicePrefabAuthoring.cs)
- [Editor/Authoring/LevelAuthoringSetup.cs](../Assets/EmergencyShift/Editor/Authoring/LevelAuthoringSetup.cs)
- [Editor/Authoring/LevelPersistenceAuthoring.cs](../Assets/EmergencyShift/Editor/Authoring/LevelPersistenceAuthoring.cs)
- [Editor/Authoring/NarrativeEnvironmentBuilder.cs](../Assets/EmergencyShift/Editor/Authoring/NarrativeEnvironmentBuilder.cs)
- [Editor/Authoring/SandboxSceneBuilder.cs](../Assets/EmergencyShift/Editor/Authoring/SandboxSceneBuilder.cs)
- [Editor/Authoring/ShotgunIntegrationAuthoring.cs](../Assets/EmergencyShift/Editor/Authoring/ShotgunIntegrationAuthoring.cs)

### Editor/Build

- [Editor/Build/ThirdPartyLicenseBuildStep.cs](../Assets/EmergencyShift/Editor/Build/ThirdPartyLicenseBuildStep.cs)

### Editor/Inspectors

- [Editor/Inspectors/EnemyPatrol2DEditor.cs](../Assets/EmergencyShift/Editor/Inspectors/EnemyPatrol2DEditor.cs)
- [Editor/Inspectors/LevelAudioDirectorEditor.cs](../Assets/EmergencyShift/Editor/Inspectors/LevelAudioDirectorEditor.cs)

## Tests

### Tests/EditMode

- [Tests/EditMode/BloodPoolTests.cs](../Assets/EmergencyShift/Tests/EditMode/BloodPoolTests.cs)
- [Tests/EditMode/CeilingLightOcclusionTests.cs](../Assets/EmergencyShift/Tests/EditMode/CeilingLightOcclusionTests.cs)
- [Tests/EditMode/DialogueDataTests.cs](../Assets/EmergencyShift/Tests/EditMode/DialogueDataTests.cs)
- [Tests/EditMode/DialoguePrefabTests.cs](../Assets/EmergencyShift/Tests/EditMode/DialoguePrefabTests.cs)
- [Tests/EditMode/DoorAccessTests.cs](../Assets/EmergencyShift/Tests/EditMode/DoorAccessTests.cs)
- [Tests/EditMode/EnemyDeathAudioTests.cs](../Assets/EmergencyShift/Tests/EditMode/EnemyDeathAudioTests.cs)
- [Tests/EditMode/EnemySystemsTests.cs](../Assets/EmergencyShift/Tests/EditMode/EnemySystemsTests.cs)
- [Tests/EditMode/EnvironmentPresentationTests.cs](../Assets/EmergencyShift/Tests/EditMode/EnvironmentPresentationTests.cs)
- [Tests/EditMode/FlashlightOwnershipTests.cs](../Assets/EmergencyShift/Tests/EditMode/FlashlightOwnershipTests.cs)
- [Tests/EditMode/GoreCompositionTests.cs](../Assets/EmergencyShift/Tests/EditMode/GoreCompositionTests.cs)
- [Tests/EditMode/HealthModelTests.cs](../Assets/EmergencyShift/Tests/EditMode/HealthModelTests.cs)
- [Tests/EditMode/LevelCompositionTests.cs](../Assets/EmergencyShift/Tests/EditMode/LevelCompositionTests.cs)
- [Tests/EditMode/LocalLampAudioTests.cs](../Assets/EmergencyShift/Tests/EditMode/LocalLampAudioTests.cs)
- [Tests/EditMode/LocalLampSceneSettingsTests.cs](../Assets/EmergencyShift/Tests/EditMode/LocalLampSceneSettingsTests.cs)
- [Tests/EditMode/MagazineWeaponTests.cs](../Assets/EmergencyShift/Tests/EditMode/MagazineWeaponTests.cs)
- [Tests/EditMode/PersistenceTests.cs](../Assets/EmergencyShift/Tests/EditMode/PersistenceTests.cs)
- [Tests/EditMode/PlayerInputTests.cs](../Assets/EmergencyShift/Tests/EditMode/PlayerInputTests.cs)
- [Tests/EditMode/SceneRegistryTests.cs](../Assets/EmergencyShift/Tests/EditMode/SceneRegistryTests.cs)
- [Tests/EditMode/SleeperTests.cs](../Assets/EmergencyShift/Tests/EditMode/SleeperTests.cs)
- [Tests/EditMode/StalkerEyeTests.cs](../Assets/EmergencyShift/Tests/EditMode/StalkerEyeTests.cs)
- [Tests/EditMode/WeaponCycleTests.cs](../Assets/EmergencyShift/Tests/EditMode/WeaponCycleTests.cs)
- [Tests/EditMode/WorkBudgetTests.cs](../Assets/EmergencyShift/Tests/EditMode/WorkBudgetTests.cs)

## Сцены и контент

- [Scenes/ArtReview/EnvironmentLibraryReview.unity](../Assets/EmergencyShift/Scenes/ArtReview/EnvironmentLibraryReview.unity)
- [Scenes/ArtReview/EnvironmentNarrativeStressReview.unity](../Assets/EmergencyShift/Scenes/ArtReview/EnvironmentNarrativeStressReview.unity)
- [Scenes/ArtReview/EnvironmentPropsDarkReview.unity](../Assets/EmergencyShift/Scenes/ArtReview/EnvironmentPropsDarkReview.unity)
- [Scenes/ArtReview/EnvironmentPropsReview.unity](../Assets/EmergencyShift/Scenes/ArtReview/EnvironmentPropsReview.unity)
- [Scenes/ArtReview/FloorMarkingsReview.unity](../Assets/EmergencyShift/Scenes/ArtReview/FloorMarkingsReview.unity)
- [Scenes/ArtReview/FurnitureLightStudy.unity](../Assets/EmergencyShift/Scenes/ArtReview/FurnitureLightStudy.unity)
- [Scenes/ArtReview/LightingPlayground.unity](../Assets/EmergencyShift/Scenes/ArtReview/LightingPlayground.unity)
- [Scenes/BasementPrototype.unity](../Assets/EmergencyShift/Scenes/BasementPrototype.unity)
- [Scenes/LevelTemplate.unity](../Assets/EmergencyShift/Scenes/LevelTemplate.unity)
- [Scenes/MechanicsSandbox.unity](../Assets/EmergencyShift/Scenes/MechanicsSandbox.unity)
- [Scenes/MorgueRouteTest.unity](../Assets/EmergencyShift/Scenes/MorgueRouteTest.unity)

Общие профили: `Data/`, `Settings/`; actions: `Settings/EmergencyShiftControls.inputactions`. Переиспользуемые объекты: `Prefabs/`; world composition: `Resources/ProjectContext.prefab`. Рабочие изображения/звуки: `Art/`, `Audio/`, `Materials/`.

Стиль — [ART_DIRECTION](ART_DIRECTION.md), структура уровней и reuse — [LEVEL_AUTHORING](Guides/LEVEL_AUTHORING.md). Материалы/происхождение — [Concepts](../Concepts/README.md); сторонние зависимости — [Extenject](ThirdParty/Extenject.md).
