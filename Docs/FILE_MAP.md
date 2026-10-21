# Карта файлов

Сформирована из текущих файлов проекта. Это навигация, а не доказательство использования или соответствия SOD. [Реестр проверок](Reports/CodeAudit.json) и [архитектура](ARCHITECTURE.md). Обновление: `Tools/Maintenance/audit_documentation.py --write-file-map`.

## AgentScripts

- [AssignPlayerInput.cs](../AgentScripts/AssignPlayerInput.cs)
- [AuditProjectReferences.cs](../AgentScripts/AuditProjectReferences.cs)
- [CheckDirectionalDamage.cs](../AgentScripts/CheckDirectionalDamage.cs)
- [CheckGroundAudioScope.cs](../AgentScripts/CheckGroundAudioScope.cs)
- [CheckInjectedActorCreation.cs](../AgentScripts/CheckInjectedActorCreation.cs)
- [CheckWorldServices.cs](../AgentScripts/CheckWorldServices.cs)
- [CleanUnusedShadowOverrides.cs](../AgentScripts/CleanUnusedShadowOverrides.cs)
- [InstallDirectionalDamage.cs](../AgentScripts/InstallDirectionalDamage.cs)
- [InstallPauseMenu.cs](../AgentScripts/InstallPauseMenu.cs)
- [InstallWorldComposition.cs](../AgentScripts/InstallWorldComposition.cs)
- [OrganizeMaterials.cs](../AgentScripts/OrganizeMaterials.cs)

## Assets/EmergencyShift/Editor/Authoring

- [DialoguePrefabAuthoring.cs](../Assets/EmergencyShift/Editor/Authoring/DialoguePrefabAuthoring.cs)
- [DialogueSandboxAuthoring.cs](../Assets/EmergencyShift/Editor/Authoring/DialogueSandboxAuthoring.cs)
- [EnvironmentLibraryReviewBuilder.cs](../Assets/EmergencyShift/Editor/Authoring/EnvironmentLibraryReviewBuilder.cs)
- [EnvironmentReviewBuilder.cs](../Assets/EmergencyShift/Editor/Authoring/EnvironmentReviewBuilder.cs)
- [EnvironmentSpriteAuthoring.cs](../Assets/EmergencyShift/Editor/Authoring/EnvironmentSpriteAuthoring.cs)
- [FloorMarkingsAuthoring.cs](../Assets/EmergencyShift/Editor/Authoring/FloorMarkingsAuthoring.cs)
- [FurnitureLightStudyBuilder.cs](../Assets/EmergencyShift/Editor/Authoring/FurnitureLightStudyBuilder.cs)
- [GameplayHudAuthoring.cs](../Assets/EmergencyShift/Editor/Authoring/GameplayHudAuthoring.cs)
- [HospitalServicePrefabAuthoring.cs](../Assets/EmergencyShift/Editor/Authoring/HospitalServicePrefabAuthoring.cs)
- [LevelAuthoringSetup.cs](../Assets/EmergencyShift/Editor/Authoring/LevelAuthoringSetup.cs)
- [LevelPersistenceAuthoring.cs](../Assets/EmergencyShift/Editor/Authoring/LevelPersistenceAuthoring.cs)
- [NarrativeEnvironmentBuilder.cs](../Assets/EmergencyShift/Editor/Authoring/NarrativeEnvironmentBuilder.cs)
- [PauseMenuAuthoring.cs](../Assets/EmergencyShift/Editor/Authoring/PauseMenuAuthoring.cs)
- [SandboxSceneBuilder.cs](../Assets/EmergencyShift/Editor/Authoring/SandboxSceneBuilder.cs)
- [ShotgunIntegrationAuthoring.cs](../Assets/EmergencyShift/Editor/Authoring/ShotgunIntegrationAuthoring.cs)

## Assets/EmergencyShift/Editor/Build

- [ThirdPartyLicenseBuildStep.cs](../Assets/EmergencyShift/Editor/Build/ThirdPartyLicenseBuildStep.cs)

## Assets/EmergencyShift/Editor/Inspectors

- [EnemyPatrol2DEditor.cs](../Assets/EmergencyShift/Editor/Inspectors/EnemyPatrol2DEditor.cs)
- [LevelAudioDirectorEditor.cs](../Assets/EmergencyShift/Editor/Inspectors/LevelAudioDirectorEditor.cs)

## Assets/EmergencyShift/Runtime/Combat/Weapons

- [BulletProjectile2D.cs](../Assets/EmergencyShift/Runtime/Combat/Weapons/BulletProjectile2D.cs)
- [FireOwnedWeaponCommand.cs](../Assets/EmergencyShift/Runtime/Combat/Weapons/FireOwnedWeaponCommand.cs)
- [IWeaponAimSource2D.cs](../Assets/EmergencyShift/Runtime/Combat/Weapons/IWeaponAimSource2D.cs)
- [IWeaponShotView.cs](../Assets/EmergencyShift/Runtime/Combat/Weapons/IWeaponShotView.cs)
- [IWeaponUseGate.cs](../Assets/EmergencyShift/Runtime/Combat/Weapons/IWeaponUseGate.cs)
- [MagazineWeaponCycle.cs](../Assets/EmergencyShift/Runtime/Combat/Weapons/MagazineWeaponCycle.cs)
- [MagazineWeaponSettings.cs](../Assets/EmergencyShift/Runtime/Combat/Weapons/MagazineWeaponSettings.cs)
- [PumpShotgunCycle.cs](../Assets/EmergencyShift/Runtime/Combat/Weapons/PumpShotgunCycle.cs)
- [PumpShotgunSettings.cs](../Assets/EmergencyShift/Runtime/Combat/Weapons/PumpShotgunSettings.cs)
- [WeaponAimEnvironment2D.cs](../Assets/EmergencyShift/Runtime/Combat/Weapons/WeaponAimEnvironment2D.cs)
- [WeaponAimRig2D.cs](../Assets/EmergencyShift/Runtime/Combat/Weapons/WeaponAimRig2D.cs)
- [WeaponAimSource2D.cs](../Assets/EmergencyShift/Runtime/Combat/Weapons/WeaponAimSource2D.cs)
- [WeaponController.cs](../Assets/EmergencyShift/Runtime/Combat/Weapons/WeaponController.cs)
- [WeaponControllerFactory.cs](../Assets/EmergencyShift/Runtime/Combat/Weapons/WeaponControllerFactory.cs)
- [WeaponCycle.cs](../Assets/EmergencyShift/Runtime/Combat/Weapons/WeaponCycle.cs)
- [WeaponCycleCommands.cs](../Assets/EmergencyShift/Runtime/Combat/Weapons/WeaponCycleCommands.cs)
- [WeaponInputIntent.cs](../Assets/EmergencyShift/Runtime/Combat/Weapons/WeaponInputIntent.cs)
- [WeaponInstaller.cs](../Assets/EmergencyShift/Runtime/Combat/Weapons/WeaponInstaller.cs)
- [WeaponInventory.cs](../Assets/EmergencyShift/Runtime/Combat/Weapons/WeaponInventory.cs)
- [WeaponInventoryCommands.cs](../Assets/EmergencyShift/Runtime/Combat/Weapons/WeaponInventoryCommands.cs)
- [WeaponOwnerModel.cs](../Assets/EmergencyShift/Runtime/Combat/Weapons/WeaponOwnerModel.cs)
- [WeaponSettings.cs](../Assets/EmergencyShift/Runtime/Combat/Weapons/WeaponSettings.cs)
- [WeaponSettingsCatalog.cs](../Assets/EmergencyShift/Runtime/Combat/Weapons/WeaponSettingsCatalog.cs)
- [WeaponShotPlan.cs](../Assets/EmergencyShift/Runtime/Combat/Weapons/WeaponShotPlan.cs)
- [WeaponShotRequest.cs](../Assets/EmergencyShift/Runtime/Combat/Weapons/WeaponShotRequest.cs)

## Assets/EmergencyShift/Runtime/Core

- [GameplayInputBlock.cs](../Assets/EmergencyShift/Runtime/Core/GameplayInputBlock.cs)
- [WorldMotionPause.cs](../Assets/EmergencyShift/Runtime/Core/WorldMotionPause.cs)

## Assets/EmergencyShift/Runtime/Core/Commands

- [CommandResult.cs](../Assets/EmergencyShift/Runtime/Core/Commands/CommandResult.cs)
- [ICommand.cs](../Assets/EmergencyShift/Runtime/Core/Commands/ICommand.cs)

## Assets/EmergencyShift/Runtime/Core/Composition

- [AssemblyInfo.cs](../Assets/EmergencyShift/Runtime/Core/Composition/AssemblyInfo.cs)
- [CameraWorldProjection2D.cs](../Assets/EmergencyShift/Runtime/Core/Composition/CameraWorldProjection2D.cs)
- [ScenePrefabFactory.cs](../Assets/EmergencyShift/Runtime/Core/Composition/ScenePrefabFactory.cs)
- [SceneRegistry.cs](../Assets/EmergencyShift/Runtime/Core/Composition/SceneRegistry.cs)
- [WorldInstaller.cs](../Assets/EmergencyShift/Runtime/Core/Composition/WorldInstaller.cs)

## Assets/EmergencyShift/Runtime/Core/Contracts

- [Damageable2D.cs](../Assets/EmergencyShift/Runtime/Core/Contracts/Damageable2D.cs)
- [EnemyPresentationSource2D.cs](../Assets/EmergencyShift/Runtime/Core/Contracts/EnemyPresentationSource2D.cs)
- [IAutoPickup.cs](../Assets/EmergencyShift/Runtime/Core/Contracts/IAutoPickup.cs)
- [IInteractable.cs](../Assets/EmergencyShift/Runtime/Core/Contracts/IInteractable.cs)
- [IThreatLight2D.cs](../Assets/EmergencyShift/Runtime/Core/Contracts/IThreatLight2D.cs)
- [IWorldProjection2D.cs](../Assets/EmergencyShift/Runtime/Core/Contracts/IWorldProjection2D.cs)

## Assets/EmergencyShift/Runtime/Core/Randomness

- [IRandomSource.cs](../Assets/EmergencyShift/Runtime/Core/Randomness/IRandomSource.cs)
- [RandomSourceFactory.cs](../Assets/EmergencyShift/Runtime/Core/Randomness/RandomSourceFactory.cs)
- [SystemRandomSource.cs](../Assets/EmergencyShift/Runtime/Core/Randomness/SystemRandomSource.cs)

## Assets/EmergencyShift/Runtime/Core/Scheduling

- [FrameWorkBudget.cs](../Assets/EmergencyShift/Runtime/Core/Scheduling/FrameWorkBudget.cs)

## Assets/EmergencyShift/Runtime/Core/Time

- [IFrameClock.cs](../Assets/EmergencyShift/Runtime/Core/Time/IFrameClock.cs)
- [IGameClock.cs](../Assets/EmergencyShift/Runtime/Core/Time/IGameClock.cs)
- [IWorldTimeScale.cs](../Assets/EmergencyShift/Runtime/Core/Time/IWorldTimeScale.cs)
- [UnityFrameClock.cs](../Assets/EmergencyShift/Runtime/Core/Time/UnityFrameClock.cs)
- [UnityGameClock.cs](../Assets/EmergencyShift/Runtime/Core/Time/UnityGameClock.cs)
- [UnityWorldTimeScale.cs](../Assets/EmergencyShift/Runtime/Core/Time/UnityWorldTimeScale.cs)

## Assets/EmergencyShift/Runtime/Definitions

- [AudioMoodProfile.cs](../Assets/EmergencyShift/Runtime/Definitions/AudioMoodProfile.cs)
- [BloodPoolProfile.cs](../Assets/EmergencyShift/Runtime/Definitions/BloodPoolProfile.cs)
- [CameraFramingProfile.cs](../Assets/EmergencyShift/Runtime/Definitions/CameraFramingProfile.cs)
- [DoorAccessFeedbackProfile.cs](../Assets/EmergencyShift/Runtime/Definitions/DoorAccessFeedbackProfile.cs)
- [DoorAudioSet.cs](../Assets/EmergencyShift/Runtime/Definitions/DoorAudioSet.cs)
- [EnemyDeathAudioSet.cs](../Assets/EmergencyShift/Runtime/Definitions/EnemyDeathAudioSet.cs)
- [EnemyDefinition.cs](../Assets/EmergencyShift/Runtime/Definitions/EnemyDefinition.cs)
- [EnemyPatrolProfile.cs](../Assets/EmergencyShift/Runtime/Definitions/EnemyPatrolProfile.cs)
- [GameplayHudStyle.cs](../Assets/EmergencyShift/Runtime/Definitions/GameplayHudStyle.cs)
- [GoreProfile.cs](../Assets/EmergencyShift/Runtime/Definitions/GoreProfile.cs)
- [LevelCatalog.cs](../Assets/EmergencyShift/Runtime/Definitions/LevelCatalog.cs)
- [LevelDefinition.cs](../Assets/EmergencyShift/Runtime/Definitions/LevelDefinition.cs)
- [LocalLampAudioSet.cs](../Assets/EmergencyShift/Runtime/Definitions/LocalLampAudioSet.cs)
- [LocalLampSceneSettings.cs](../Assets/EmergencyShift/Runtime/Definitions/LocalLampSceneSettings.cs)
- [LocalLightProfile.cs](../Assets/EmergencyShift/Runtime/Definitions/LocalLightProfile.cs)
- [MagazineWeaponDefinition.cs](../Assets/EmergencyShift/Runtime/Definitions/MagazineWeaponDefinition.cs)
- [PlayerAudioSet.cs](../Assets/EmergencyShift/Runtime/Definitions/PlayerAudioSet.cs)
- [PlayerWorldAudioSet.cs](../Assets/EmergencyShift/Runtime/Definitions/PlayerWorldAudioSet.cs)
- [ProjectileVisualProfile.cs](../Assets/EmergencyShift/Runtime/Definitions/ProjectileVisualProfile.cs)
- [PumpShotgunDefinition.cs](../Assets/EmergencyShift/Runtime/Definitions/PumpShotgunDefinition.cs)
- [ShotVisualProfile.cs](../Assets/EmergencyShift/Runtime/Definitions/ShotVisualProfile.cs)
- [SleeperAudioProfile.cs](../Assets/EmergencyShift/Runtime/Definitions/SleeperAudioProfile.cs)
- [SleeperBehaviourProfile.cs](../Assets/EmergencyShift/Runtime/Definitions/SleeperBehaviourProfile.cs)
- [StalkerAudioSet.cs](../Assets/EmergencyShift/Runtime/Definitions/StalkerAudioSet.cs)
- [StalkerBehaviourProfile.cs](../Assets/EmergencyShift/Runtime/Definitions/StalkerBehaviourProfile.cs)
- [StalkerLocomotionProfile.cs](../Assets/EmergencyShift/Runtime/Definitions/StalkerLocomotionProfile.cs)
- [WeaponAudioProfile.cs](../Assets/EmergencyShift/Runtime/Definitions/WeaponAudioProfile.cs)
- [WeaponCatalog.cs](../Assets/EmergencyShift/Runtime/Definitions/WeaponCatalog.cs)
- [WeaponDefinition.cs](../Assets/EmergencyShift/Runtime/Definitions/WeaponDefinition.cs)

## Assets/EmergencyShift/Runtime/Development

- [AdaptiveAudioProbe.cs](../Assets/EmergencyShift/Runtime/Development/AdaptiveAudioProbe.cs)
- [CheckpointRegressionProbe.cs](../Assets/EmergencyShift/Runtime/Development/CheckpointRegressionProbe.cs)
- [CombatProbeTarget2D.cs](../Assets/EmergencyShift/Runtime/Development/CombatProbeTarget2D.cs)
- [CompositionFailureProbe.cs](../Assets/EmergencyShift/Runtime/Development/CompositionFailureProbe.cs)
- [CrowdReadinessProbe.cs](../Assets/EmergencyShift/Runtime/Development/CrowdReadinessProbe.cs)
- [DevelopmentBuildSmoke.cs](../Assets/EmergencyShift/Runtime/Development/DevelopmentBuildSmoke.cs)
- [EngineeringBenchmark2D.cs](../Assets/EmergencyShift/Runtime/Development/EngineeringBenchmark2D.cs)
- [EnvironmentLoadProbe.cs](../Assets/EmergencyShift/Runtime/Development/EnvironmentLoadProbe.cs)
- [FurnitureLightBenchmark.cs](../Assets/EmergencyShift/Runtime/Development/FurnitureLightBenchmark.cs)
- [FurnitureLightStudy2D.cs](../Assets/EmergencyShift/Runtime/Development/FurnitureLightStudy2D.cs)
- [FurnitureLightStudyProfile.cs](../Assets/EmergencyShift/Runtime/Development/FurnitureLightStudyProfile.cs)
- [SandboxRegressionProbe.cs](../Assets/EmergencyShift/Runtime/Development/SandboxRegressionProbe.cs)
- [SleeperRegressionProbe.cs](../Assets/EmergencyShift/Runtime/Development/SleeperRegressionProbe.cs)

## Assets/EmergencyShift/Runtime/Dialogue

- [DialogueCommands.cs](../Assets/EmergencyShift/Runtime/Dialogue/DialogueCommands.cs)
- [DialogueController.cs](../Assets/EmergencyShift/Runtime/Dialogue/DialogueController.cs)
- [DialogueControllerFactory.cs](../Assets/EmergencyShift/Runtime/Dialogue/DialogueControllerFactory.cs)
- [DialogueGraph.cs](../Assets/EmergencyShift/Runtime/Dialogue/DialogueGraph.cs)
- [DialogueInstaller.cs](../Assets/EmergencyShift/Runtime/Dialogue/DialogueInstaller.cs)
- [DialogueInteraction2D.cs](../Assets/EmergencyShift/Runtime/Dialogue/DialogueInteraction2D.cs)
- [DialogueNode.cs](../Assets/EmergencyShift/Runtime/Dialogue/DialogueNode.cs)
- [DialogueOption.cs](../Assets/EmergencyShift/Runtime/Dialogue/DialogueOption.cs)
- [DialogueRun.cs](../Assets/EmergencyShift/Runtime/Dialogue/DialogueRun.cs)
- [DialogueSequence.cs](../Assets/EmergencyShift/Runtime/Dialogue/DialogueSequence.cs)
- [DialogueSession.cs](../Assets/EmergencyShift/Runtime/Dialogue/DialogueSession.cs)
- [DialogueStep.cs](../Assets/EmergencyShift/Runtime/Dialogue/DialogueStep.cs)
- [DialogueStyle.cs](../Assets/EmergencyShift/Runtime/Dialogue/DialogueStyle.cs)
- [DialogueTransition.cs](../Assets/EmergencyShift/Runtime/Dialogue/DialogueTransition.cs)

## Assets/EmergencyShift/Runtime/Enemies/Common

- [CrowdAgent2D.cs](../Assets/EmergencyShift/Runtime/Enemies/Common/CrowdAgent2D.cs)
- [EnemyChasePath2D.cs](../Assets/EmergencyShift/Runtime/Enemies/Common/EnemyChasePath2D.cs)
- [EnemyChasePathFactory.cs](../Assets/EmergencyShift/Runtime/Enemies/Common/EnemyChasePathFactory.cs)
- [EnemyHealth2D.cs](../Assets/EmergencyShift/Runtime/Enemies/Common/EnemyHealth2D.cs)
- [EnemyMeleeAttack2D.cs](../Assets/EmergencyShift/Runtime/Enemies/Common/EnemyMeleeAttack2D.cs)
- [EnemyMotor2D.cs](../Assets/EmergencyShift/Runtime/Enemies/Common/EnemyMotor2D.cs)
- [EnemyPatrol2D.cs](../Assets/EmergencyShift/Runtime/Enemies/Common/EnemyPatrol2D.cs)
- [EnemyPerception2D.cs](../Assets/EmergencyShift/Runtime/Enemies/Common/EnemyPerception2D.cs)
- [NavigationWorkBudget2D.cs](../Assets/EmergencyShift/Runtime/Enemies/Common/NavigationWorkBudget2D.cs)

## Assets/EmergencyShift/Runtime/Enemies/Sleeper

- [Sleeper2D.cs](../Assets/EmergencyShift/Runtime/Enemies/Sleeper/Sleeper2D.cs)
- [SleeperAudio2D.cs](../Assets/EmergencyShift/Runtime/Enemies/Sleeper/SleeperAudio2D.cs)
- [SleeperAudioThreat2D.cs](../Assets/EmergencyShift/Runtime/Enemies/Sleeper/SleeperAudioThreat2D.cs)
- [SleeperPresentationSource2D.cs](../Assets/EmergencyShift/Runtime/Enemies/Sleeper/SleeperPresentationSource2D.cs)

## Assets/EmergencyShift/Runtime/Enemies/Stalker

- [ShadowStalker2D.cs](../Assets/EmergencyShift/Runtime/Enemies/Stalker/ShadowStalker2D.cs)
- [StalkerAudio2D.cs](../Assets/EmergencyShift/Runtime/Enemies/Stalker/StalkerAudio2D.cs)
- [StalkerAudioThreat2D.cs](../Assets/EmergencyShift/Runtime/Enemies/Stalker/StalkerAudioThreat2D.cs)
- [StalkerPresentationSource2D.cs](../Assets/EmergencyShift/Runtime/Enemies/Stalker/StalkerPresentationSource2D.cs)
- [StalkerSpriteAnimator.cs](../Assets/EmergencyShift/Runtime/Enemies/Stalker/StalkerSpriteAnimator.cs)

## Assets/EmergencyShift/Runtime/Features/AccessControl

- [AccessControlFactory.cs](../Assets/EmergencyShift/Runtime/Features/AccessControl/AccessControlFactory.cs)
- [AccessControlInstaller.cs](../Assets/EmergencyShift/Runtime/Features/AccessControl/AccessControlInstaller.cs)
- [DoorAccessControllerFactory.cs](../Assets/EmergencyShift/Runtime/Features/AccessControl/DoorAccessControllerFactory.cs)

## Assets/EmergencyShift/Runtime/Features/AccessControl/Config

- [DoorAccessSettings.cs](../Assets/EmergencyShift/Runtime/Features/AccessControl/Config/DoorAccessSettings.cs)

## Assets/EmergencyShift/Runtime/Features/AccessControl/Logic

- [AccessCardController.cs](../Assets/EmergencyShift/Runtime/Features/AccessControl/Logic/AccessCardController.cs)
- [AccessCardModel.cs](../Assets/EmergencyShift/Runtime/Features/AccessControl/Logic/AccessCardModel.cs)
- [AddAccessCardCommand.cs](../Assets/EmergencyShift/Runtime/Features/AccessControl/Logic/AddAccessCardCommand.cs)
- [CheckDoorAccessCommand.cs](../Assets/EmergencyShift/Runtime/Features/AccessControl/Logic/CheckDoorAccessCommand.cs)
- [ConfigureDoorAccessCommand.cs](../Assets/EmergencyShift/Runtime/Features/AccessControl/Logic/ConfigureDoorAccessCommand.cs)
- [DoorAccessAttempt.cs](../Assets/EmergencyShift/Runtime/Features/AccessControl/Logic/DoorAccessAttempt.cs)
- [DoorAccessController.cs](../Assets/EmergencyShift/Runtime/Features/AccessControl/Logic/DoorAccessController.cs)
- [DoorAccessModel.cs](../Assets/EmergencyShift/Runtime/Features/AccessControl/Logic/DoorAccessModel.cs)
- [IAccessCardView.cs](../Assets/EmergencyShift/Runtime/Features/AccessControl/Logic/IAccessCardView.cs)
- [RestoreAccessCardsCommand.cs](../Assets/EmergencyShift/Runtime/Features/AccessControl/Logic/RestoreAccessCardsCommand.cs)
- [RestoreDoorAccessCommand.cs](../Assets/EmergencyShift/Runtime/Features/AccessControl/Logic/RestoreDoorAccessCommand.cs)

## Assets/EmergencyShift/Runtime/Features/DamageFeedback

- [DamageFeedbackInstaller.cs](../Assets/EmergencyShift/Runtime/Features/DamageFeedback/DamageFeedbackInstaller.cs)

## Assets/EmergencyShift/Runtime/Features/DamageFeedback/Config

- [DamageFeedbackSettings.cs](../Assets/EmergencyShift/Runtime/Features/DamageFeedback/Config/DamageFeedbackSettings.cs)
- [DirectionalDamageStyle.cs](../Assets/EmergencyShift/Runtime/Features/DamageFeedback/Config/DirectionalDamageStyle.cs)

## Assets/EmergencyShift/Runtime/Features/DamageFeedback/Logic

- [DamagePulse.cs](../Assets/EmergencyShift/Runtime/Features/DamageFeedback/Logic/DamagePulse.cs)
- [DirectionalDamageController.cs](../Assets/EmergencyShift/Runtime/Features/DamageFeedback/Logic/DirectionalDamageController.cs)
- [DirectionalDamageFactory.cs](../Assets/EmergencyShift/Runtime/Features/DamageFeedback/Logic/DirectionalDamageFactory.cs)
- [DirectionalDamageModel.cs](../Assets/EmergencyShift/Runtime/Features/DamageFeedback/Logic/DirectionalDamageModel.cs)
- [IDirectionalDamageView.cs](../Assets/EmergencyShift/Runtime/Features/DamageFeedback/Logic/IDirectionalDamageView.cs)
- [ShowDamagePulseCommand.cs](../Assets/EmergencyShift/Runtime/Features/DamageFeedback/Logic/ShowDamagePulseCommand.cs)

## Assets/EmergencyShift/Runtime/Features/DamageFeedback/View

- [DirectionalDamageFeedbackView.cs](../Assets/EmergencyShift/Runtime/Features/DamageFeedback/View/DirectionalDamageFeedbackView.cs)

## Assets/EmergencyShift/Runtime/Features/Enemies

- [EnemyInstaller.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/EnemyInstaller.cs)
- [EnemyMeleeControllerFactory.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/EnemyMeleeControllerFactory.cs)
- [EnemyPatrolControllerFactory.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/EnemyPatrolControllerFactory.cs)
- [SleeperControllerFactory.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/SleeperControllerFactory.cs)
- [StalkerControllerFactory.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/StalkerControllerFactory.cs)

## Assets/EmergencyShift/Runtime/Features/Enemies/Config

- [EnemyMeleeSettings.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Config/EnemyMeleeSettings.cs)
- [EnemyPatrolSettings.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Config/EnemyPatrolSettings.cs)
- [SleeperSettings.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Config/SleeperSettings.cs)
- [StalkerSettings.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Config/StalkerSettings.cs)

## Assets/EmergencyShift/Runtime/Features/Enemies/Logic

- [ActivateStalkerAggroCommand.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/ActivateStalkerAggroCommand.cs)
- [BeginEnemyMeleeCommand.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/BeginEnemyMeleeCommand.cs)
- [BeginSleeperAttackCommand.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/BeginSleeperAttackCommand.cs)
- [BeginStalkerAttackCommand.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/BeginStalkerAttackCommand.cs)
- [CompleteSleeperAttackCommand.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/CompleteSleeperAttackCommand.cs)
- [CompleteStalkerAttackCommand.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/CompleteStalkerAttackCommand.cs)
- [EnemyChasePathModel.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/EnemyChasePathModel.cs)
- [EnemyMeleeController.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/EnemyMeleeController.cs)
- [EnemyMeleeModel.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/EnemyMeleeModel.cs)
- [EnemyMeleeObservation.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/EnemyMeleeObservation.cs)
- [EnemyMeleeState.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/EnemyMeleeState.cs)
- [EnemyPathSchedule.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/EnemyPathSchedule.cs)
- [EnemyPatrolBounds.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/EnemyPatrolBounds.cs)
- [EnemyPatrolController.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/EnemyPatrolController.cs)
- [EnemyPatrolModel.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/EnemyPatrolModel.cs)
- [EnemyPatrolState.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/EnemyPatrolState.cs)
- [EnemyPatrolStep.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/EnemyPatrolStep.cs)
- [FinishEnemyMeleeCommand.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/FinishEnemyMeleeCommand.cs)
- [IEnemyBrainMelee.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/IEnemyBrainMelee.cs)
- [IEnemyBrainMotor.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/IEnemyBrainMotor.cs)
- [IEnemyMeleeEnvironment.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/IEnemyMeleeEnvironment.cs)
- [IEnemyPathBuildBudget.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/IEnemyPathBuildBudget.cs)
- [IEnemyPathGeometry.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/IEnemyPathGeometry.cs)
- [IEnemyPathSchedule.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/IEnemyPathSchedule.cs)
- [IEnemyPatrolEnvironment.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/IEnemyPatrolEnvironment.cs)
- [InitializeEnemyPatrolCommand.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/InitializeEnemyPatrolCommand.cs)
- [InterruptEnemyMeleeCommand.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/InterruptEnemyMeleeCommand.cs)
- [IStalkerLocomotion.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/IStalkerLocomotion.cs)
- [KillSleeperCommand.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/KillSleeperCommand.cs)
- [KillStalkerCommand.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/KillStalkerCommand.cs)
- [ResetEnemyPatrolCommand.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/ResetEnemyPatrolCommand.cs)
- [RestoreEnemyMeleeCommand.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/RestoreEnemyMeleeCommand.cs)
- [RestoreEnemyPatrolCommand.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/RestoreEnemyPatrolCommand.cs)
- [RestoreSleeperCommand.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/RestoreSleeperCommand.cs)
- [RestoreStalkerCommand.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/RestoreStalkerCommand.cs)
- [SleeperController.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/SleeperController.cs)
- [SleeperIntent.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/SleeperIntent.cs)
- [SleeperModel.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/SleeperModel.cs)
- [SleeperState.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/SleeperState.cs)
- [SleeperStateSnapshot.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/SleeperStateSnapshot.cs)
- [StaggerSleeperCommand.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/StaggerSleeperCommand.cs)
- [StaggerStalkerCommand.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/StaggerStalkerCommand.cs)
- [StalkerController.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/StalkerController.cs)
- [StalkerIntent.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/StalkerIntent.cs)
- [StalkerModel.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/StalkerModel.cs)
- [StalkerObservation.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/StalkerObservation.cs)
- [StalkerState.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/StalkerState.cs)
- [StalkerStateSnapshot.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/StalkerStateSnapshot.cs)
- [WakeSleeperCommand.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/Logic/WakeSleeperCommand.cs)

## Assets/EmergencyShift/Runtime/Features/Enemies/View

- [EnemyBrainMelee2D.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/View/EnemyBrainMelee2D.cs)
- [EnemyBrainMotor2D.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/View/EnemyBrainMotor2D.cs)
- [EnemyMeleeEnvironment2D.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/View/EnemyMeleeEnvironment2D.cs)
- [EnemyPathGeometry2D.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/View/EnemyPathGeometry2D.cs)
- [EnemyPatrolEnvironment2D.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/View/EnemyPatrolEnvironment2D.cs)
- [ProfiledEnemyPathSchedule2D.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/View/ProfiledEnemyPathSchedule2D.cs)
- [StalkerLocomotion2D.cs](../Assets/EmergencyShift/Runtime/Features/Enemies/View/StalkerLocomotion2D.cs)

## Assets/EmergencyShift/Runtime/Features/Flashlight

- [FlashlightControllerFactory.cs](../Assets/EmergencyShift/Runtime/Features/Flashlight/FlashlightControllerFactory.cs)
- [FlashlightInstaller.cs](../Assets/EmergencyShift/Runtime/Features/Flashlight/FlashlightInstaller.cs)

## Assets/EmergencyShift/Runtime/Features/Flashlight/Config

- [FlashlightAimSettings.cs](../Assets/EmergencyShift/Runtime/Features/Flashlight/Config/FlashlightAimSettings.cs)
- [FlashlightUpgradeLimits.cs](../Assets/EmergencyShift/Runtime/Features/Flashlight/Config/FlashlightUpgradeLimits.cs)

## Assets/EmergencyShift/Runtime/Features/Flashlight/Logic

- [AcquireFlashlightStateCommand.cs](../Assets/EmergencyShift/Runtime/Features/Flashlight/Logic/AcquireFlashlightStateCommand.cs)
- [FlashlightAimController.cs](../Assets/EmergencyShift/Runtime/Features/Flashlight/Logic/FlashlightAimController.cs)
- [FlashlightAimModel.cs](../Assets/EmergencyShift/Runtime/Features/Flashlight/Logic/FlashlightAimModel.cs)
- [FlashlightController.cs](../Assets/EmergencyShift/Runtime/Features/Flashlight/Logic/FlashlightController.cs)
- [FlashlightModule.cs](../Assets/EmergencyShift/Runtime/Features/Flashlight/Logic/FlashlightModule.cs)
- [FlashlightModuleRequest.cs](../Assets/EmergencyShift/Runtime/Features/Flashlight/Logic/FlashlightModuleRequest.cs)
- [FlashlightStateModel.cs](../Assets/EmergencyShift/Runtime/Features/Flashlight/Logic/FlashlightStateModel.cs)
- [FlashlightUpgradeController.cs](../Assets/EmergencyShift/Runtime/Features/Flashlight/Logic/FlashlightUpgradeController.cs)
- [FlashlightUpgradeModel.cs](../Assets/EmergencyShift/Runtime/Features/Flashlight/Logic/FlashlightUpgradeModel.cs)
- [IFlashlightAimEnvironment.cs](../Assets/EmergencyShift/Runtime/Features/Flashlight/Logic/IFlashlightAimEnvironment.cs)
- [IFlashlightOpticsSource.cs](../Assets/EmergencyShift/Runtime/Features/Flashlight/Logic/IFlashlightOpticsSource.cs)
- [IFlashlightUpgradeEnvironment.cs](../Assets/EmergencyShift/Runtime/Features/Flashlight/Logic/IFlashlightUpgradeEnvironment.cs)
- [IFlashlightView.cs](../Assets/EmergencyShift/Runtime/Features/Flashlight/Logic/IFlashlightView.cs)
- [InstallFlashlightModuleCommand.cs](../Assets/EmergencyShift/Runtime/Features/Flashlight/Logic/InstallFlashlightModuleCommand.cs)
- [RestoreFlashlightModulesCommand.cs](../Assets/EmergencyShift/Runtime/Features/Flashlight/Logic/RestoreFlashlightModulesCommand.cs)
- [RestoreFlashlightOwnershipCommand.cs](../Assets/EmergencyShift/Runtime/Features/Flashlight/Logic/RestoreFlashlightOwnershipCommand.cs)
- [SetFlashlightOpticsCommand.cs](../Assets/EmergencyShift/Runtime/Features/Flashlight/Logic/SetFlashlightOpticsCommand.cs)
- [SetFlashlightPowerCommand.cs](../Assets/EmergencyShift/Runtime/Features/Flashlight/Logic/SetFlashlightPowerCommand.cs)

## Assets/EmergencyShift/Runtime/Features/Flashlight/View

- [FlashlightAimEnvironment2D.cs](../Assets/EmergencyShift/Runtime/Features/Flashlight/View/FlashlightAimEnvironment2D.cs)
- [FlashlightObstruction2D.cs](../Assets/EmergencyShift/Runtime/Features/Flashlight/View/FlashlightObstruction2D.cs)

## Assets/EmergencyShift/Runtime/Features/Health

- [HealthInstaller.cs](../Assets/EmergencyShift/Runtime/Features/Health/HealthInstaller.cs)

## Assets/EmergencyShift/Runtime/Features/Health/Config

- [HealthSettings.cs](../Assets/EmergencyShift/Runtime/Features/Health/Config/HealthSettings.cs)

## Assets/EmergencyShift/Runtime/Features/Health/Logic

- [DamageHealthCommand.cs](../Assets/EmergencyShift/Runtime/Features/Health/Logic/DamageHealthCommand.cs)
- [HealthModel.cs](../Assets/EmergencyShift/Runtime/Features/Health/Logic/HealthModel.cs)
- [HealthModelFactory.cs](../Assets/EmergencyShift/Runtime/Features/Health/Logic/HealthModelFactory.cs)
- [SetHealthCapacityCommand.cs](../Assets/EmergencyShift/Runtime/Features/Health/Logic/SetHealthCapacityCommand.cs)

## Assets/EmergencyShift/Runtime/Features/PauseMenu

- [PauseMenuInstaller.cs](../Assets/EmergencyShift/Runtime/Features/PauseMenu/PauseMenuInstaller.cs)

## Assets/EmergencyShift/Runtime/Features/PauseMenu/Config

- [PauseMenuAudioConfig.cs](../Assets/EmergencyShift/Runtime/Features/PauseMenu/Config/PauseMenuAudioConfig.cs)

## Assets/EmergencyShift/Runtime/Features/PauseMenu/Logic

- [IPauseMenuEnvironment.cs](../Assets/EmergencyShift/Runtime/Features/PauseMenu/Logic/IPauseMenuEnvironment.cs)
- [OpenPauseMenuCommand.cs](../Assets/EmergencyShift/Runtime/Features/PauseMenu/Logic/OpenPauseMenuCommand.cs)
- [PauseMenuController.cs](../Assets/EmergencyShift/Runtime/Features/PauseMenu/Logic/PauseMenuController.cs)
- [PauseMenuModel.cs](../Assets/EmergencyShift/Runtime/Features/PauseMenu/Logic/PauseMenuModel.cs)
- [ResumePauseMenuCommand.cs](../Assets/EmergencyShift/Runtime/Features/PauseMenu/Logic/ResumePauseMenuCommand.cs)

## Assets/EmergencyShift/Runtime/Features/PauseMenu/View

- [PauseMenuEnvironment.cs](../Assets/EmergencyShift/Runtime/Features/PauseMenu/View/PauseMenuEnvironment.cs)
- [PauseMenuMusic.cs](../Assets/EmergencyShift/Runtime/Features/PauseMenu/View/PauseMenuMusic.cs)
- [PauseMenuPresenter.cs](../Assets/EmergencyShift/Runtime/Features/PauseMenu/View/PauseMenuPresenter.cs)
- [PauseMenuView.cs](../Assets/EmergencyShift/Runtime/Features/PauseMenu/View/PauseMenuView.cs)

## Assets/EmergencyShift/Runtime/Features/PlayerInput

- [PlayerInputInstaller.cs](../Assets/EmergencyShift/Runtime/Features/PlayerInput/PlayerInputInstaller.cs)

## Assets/EmergencyShift/Runtime/Features/PlayerInput/Logic

- [IPlayerInput.cs](../Assets/EmergencyShift/Runtime/Features/PlayerInput/Logic/IPlayerInput.cs)

## Assets/EmergencyShift/Runtime/Features/PlayerInput/View

- [InputActionPlayerInput.cs](../Assets/EmergencyShift/Runtime/Features/PlayerInput/View/InputActionPlayerInput.cs)

## Assets/EmergencyShift/Runtime/Features/PlayerMovement

- [PlayerMovementControllerFactory.cs](../Assets/EmergencyShift/Runtime/Features/PlayerMovement/PlayerMovementControllerFactory.cs)
- [PlayerMovementInstaller.cs](../Assets/EmergencyShift/Runtime/Features/PlayerMovement/PlayerMovementInstaller.cs)

## Assets/EmergencyShift/Runtime/Features/PlayerMovement/Config

- [PlayerMovementSettings.cs](../Assets/EmergencyShift/Runtime/Features/PlayerMovement/Config/PlayerMovementSettings.cs)

## Assets/EmergencyShift/Runtime/Features/PlayerMovement/Logic

- [PlayerMovementController.cs](../Assets/EmergencyShift/Runtime/Features/PlayerMovement/Logic/PlayerMovementController.cs)
- [PlayerMovementModel.cs](../Assets/EmergencyShift/Runtime/Features/PlayerMovement/Logic/PlayerMovementModel.cs)
- [SetMovementIntentCommand.cs](../Assets/EmergencyShift/Runtime/Features/PlayerMovement/Logic/SetMovementIntentCommand.cs)

## Assets/EmergencyShift/Runtime/Levels

- [EncounterActivation2D.cs](../Assets/EmergencyShift/Runtime/Levels/EncounterActivation2D.cs)
- [LevelContext2D.cs](../Assets/EmergencyShift/Runtime/Levels/LevelContext2D.cs)
- [LevelExit2D.cs](../Assets/EmergencyShift/Runtime/Levels/LevelExit2D.cs)
- [LevelObjectiveController2D.cs](../Assets/EmergencyShift/Runtime/Levels/LevelObjectiveController2D.cs)
- [LevelParticipant2D.cs](../Assets/EmergencyShift/Runtime/Levels/LevelParticipant2D.cs)
- [LevelRegistriesInstaller.cs](../Assets/EmergencyShift/Runtime/Levels/LevelRegistriesInstaller.cs)
- [LevelRunController2D.cs](../Assets/EmergencyShift/Runtime/Levels/LevelRunController2D.cs)

## Assets/EmergencyShift/Runtime/Persistence

- [CardCheckpoint2D.cs](../Assets/EmergencyShift/Runtime/Persistence/CardCheckpoint2D.cs)
- [CheckpointBooleanPayload.cs](../Assets/EmergencyShift/Runtime/Persistence/CheckpointBooleanPayload.cs)
- [CheckpointData.cs](../Assets/EmergencyShift/Runtime/Persistence/CheckpointData.cs)
- [CheckpointStore.cs](../Assets/EmergencyShift/Runtime/Persistence/CheckpointStore.cs)
- [CheckpointValueValidation.cs](../Assets/EmergencyShift/Runtime/Persistence/CheckpointValueValidation.cs)
- [CheckpointVolume2D.cs](../Assets/EmergencyShift/Runtime/Persistence/CheckpointVolume2D.cs)
- [DoorCheckpoint2D.cs](../Assets/EmergencyShift/Runtime/Persistence/DoorCheckpoint2D.cs)
- [EncounterCheckpoint2D.cs](../Assets/EmergencyShift/Runtime/Persistence/EncounterCheckpoint2D.cs)
- [LevelCheckpointController2D.cs](../Assets/EmergencyShift/Runtime/Persistence/LevelCheckpointController2D.cs)
- [PersistentEntity2D.cs](../Assets/EmergencyShift/Runtime/Persistence/PersistentEntity2D.cs)
- [PlayerCheckpoint2D.cs](../Assets/EmergencyShift/Runtime/Persistence/PlayerCheckpoint2D.cs)
- [RemainsSnapshotValidation.cs](../Assets/EmergencyShift/Runtime/Persistence/RemainsSnapshotValidation.cs)
- [SleeperCheckpoint2D.cs](../Assets/EmergencyShift/Runtime/Persistence/SleeperCheckpoint2D.cs)
- [StalkerCheckpoint2D.cs](../Assets/EmergencyShift/Runtime/Persistence/StalkerCheckpoint2D.cs)
- [UpgradeCheckpoint2D.cs](../Assets/EmergencyShift/Runtime/Persistence/UpgradeCheckpoint2D.cs)
- [WeaponLootCheckpoint2D.cs](../Assets/EmergencyShift/Runtime/Persistence/WeaponLootCheckpoint2D.cs)

## Assets/EmergencyShift/Runtime/Player

- [AcquireFlashlightCommand.cs](../Assets/EmergencyShift/Runtime/Player/AcquireFlashlightCommand.cs)
- [CameraFollow2D.cs](../Assets/EmergencyShift/Runtime/Player/CameraFollow2D.cs)
- [FlashlightModuleValidation.cs](../Assets/EmergencyShift/Runtime/Player/FlashlightModuleValidation.cs)
- [FlashlightOwnership.cs](../Assets/EmergencyShift/Runtime/Player/FlashlightOwnership.cs)
- [FlashlightUpgrades.cs](../Assets/EmergencyShift/Runtime/Player/FlashlightUpgrades.cs)
- [PlayerAccessCards2D.cs](../Assets/EmergencyShift/Runtime/Player/PlayerAccessCards2D.cs)
- [PlayerAim2D.cs](../Assets/EmergencyShift/Runtime/Player/PlayerAim2D.cs)
- [PlayerAudio2D.cs](../Assets/EmergencyShift/Runtime/Player/PlayerAudio2D.cs)
- [PlayerCasingEjector2D.cs](../Assets/EmergencyShift/Runtime/Player/PlayerCasingEjector2D.cs)
- [PlayerFootsteps2D.cs](../Assets/EmergencyShift/Runtime/Player/PlayerFootsteps2D.cs)
- [PlayerInteraction.cs](../Assets/EmergencyShift/Runtime/Player/PlayerInteraction.cs)
- [PlayerLife2D.cs](../Assets/EmergencyShift/Runtime/Player/PlayerLife2D.cs)
- [PlayerMotor2D.cs](../Assets/EmergencyShift/Runtime/Player/PlayerMotor2D.cs)
- [PlayerUtilityAudio2D.cs](../Assets/EmergencyShift/Runtime/Player/PlayerUtilityAudio2D.cs)
- [PlayerWeapon2D.cs](../Assets/EmergencyShift/Runtime/Player/PlayerWeapon2D.cs)

## Assets/EmergencyShift/Runtime/Presentation/Audio

- [AudioThreatEmitter2D.cs](../Assets/EmergencyShift/Runtime/Presentation/Audio/AudioThreatEmitter2D.cs)
- [EnemyDeathAudioVariants.cs](../Assets/EmergencyShift/Runtime/Presentation/Audio/EnemyDeathAudioVariants.cs)
- [LevelAudioDirector2D.cs](../Assets/EmergencyShift/Runtime/Presentation/Audio/LevelAudioDirector2D.cs)

## Assets/EmergencyShift/Runtime/Presentation/Visuals

- [BloodHitSpray2D.cs](../Assets/EmergencyShift/Runtime/Presentation/Visuals/BloodHitSpray2D.cs)
- [BloodPool2D.cs](../Assets/EmergencyShift/Runtime/Presentation/Visuals/BloodPool2D.cs)
- [BloodRuptureBurst2D.cs](../Assets/EmergencyShift/Runtime/Presentation/Visuals/BloodRuptureBurst2D.cs)
- [CreatureGroundShadow2D.cs](../Assets/EmergencyShift/Runtime/Presentation/Visuals/CreatureGroundShadow2D.cs)
- [CreatureSpriteAnimator2D.cs](../Assets/EmergencyShift/Runtime/Presentation/Visuals/CreatureSpriteAnimator2D.cs)
- [EnemyDeathEffects2D.cs](../Assets/EmergencyShift/Runtime/Presentation/Visuals/EnemyDeathEffects2D.cs)
- [EnemyHitResponse2D.cs](../Assets/EmergencyShift/Runtime/Presentation/Visuals/EnemyHitResponse2D.cs)
- [GibMotion2D.cs](../Assets/EmergencyShift/Runtime/Presentation/Visuals/GibMotion2D.cs)
- [GoreBurst2D.cs](../Assets/EmergencyShift/Runtime/Presentation/Visuals/GoreBurst2D.cs)
- [GoreSpawnBudget2D.cs](../Assets/EmergencyShift/Runtime/Presentation/Visuals/GoreSpawnBudget2D.cs)
- [PlayerShotShadow2D.cs](../Assets/EmergencyShift/Runtime/Presentation/Visuals/PlayerShotShadow2D.cs)
- [RemainsPartIdentity2D.cs](../Assets/EmergencyShift/Runtime/Presentation/Visuals/RemainsPartIdentity2D.cs)
- [RemainsRecord2D.cs](../Assets/EmergencyShift/Runtime/Presentation/Visuals/RemainsRecord2D.cs)
- [RemainsSnapshot.cs](../Assets/EmergencyShift/Runtime/Presentation/Visuals/RemainsSnapshot.cs)
- [RemainsVisualStyle.cs](../Assets/EmergencyShift/Runtime/Presentation/Visuals/RemainsVisualStyle.cs)
- [ShellCasing2D.cs](../Assets/EmergencyShift/Runtime/Presentation/Visuals/ShellCasing2D.cs)
- [ShotEffect2D.cs](../Assets/EmergencyShift/Runtime/Presentation/Visuals/ShotEffect2D.cs)

## Assets/EmergencyShift/Runtime/Sandbox

- [SandboxAmmoSupply2D.cs](../Assets/EmergencyShift/Runtime/Sandbox/SandboxAmmoSupply2D.cs)
- [SandboxHealthZone2D.cs](../Assets/EmergencyShift/Runtime/Sandbox/SandboxHealthZone2D.cs)
- [SandboxSession2D.cs](../Assets/EmergencyShift/Runtime/Sandbox/SandboxSession2D.cs)
- [SandboxStationaryStalker2D.cs](../Assets/EmergencyShift/Runtime/Sandbox/SandboxStationaryStalker2D.cs)

## Assets/EmergencyShift/Runtime/UI

- [AimReticle2D.cs](../Assets/EmergencyShift/Runtime/UI/AimReticle2D.cs)
- [DialogueFadeGraphic.cs](../Assets/EmergencyShift/Runtime/UI/DialogueFadeGraphic.cs)
- [DialogueView.cs](../Assets/EmergencyShift/Runtime/UI/DialogueView.cs)
- [GameplayHudView.cs](../Assets/EmergencyShift/Runtime/UI/GameplayHudView.cs)
- [HudWeaponRow.cs](../Assets/EmergencyShift/Runtime/UI/HudWeaponRow.cs)
- [LevelCompletionView2D.cs](../Assets/EmergencyShift/Runtime/UI/LevelCompletionView2D.cs)
- [PrototypeHud.cs](../Assets/EmergencyShift/Runtime/UI/PrototypeHud.cs)

## Assets/EmergencyShift/Runtime/World/Doors

- [Door2D.cs](../Assets/EmergencyShift/Runtime/World/Doors/Door2D.cs)
- [DoorAccess2D.cs](../Assets/EmergencyShift/Runtime/World/Doors/DoorAccess2D.cs)
- [DoorAccessFeedback2D.cs](../Assets/EmergencyShift/Runtime/World/Doors/DoorAccessFeedback2D.cs)
- [DoorAudio2D.cs](../Assets/EmergencyShift/Runtime/World/Doors/DoorAudio2D.cs)

## Assets/EmergencyShift/Runtime/World/Lighting

- [CeilingLightOcclusion2D.cs](../Assets/EmergencyShift/Runtime/World/Lighting/CeilingLightOcclusion2D.cs)
- [Flashlight2D.cs](../Assets/EmergencyShift/Runtime/World/Lighting/Flashlight2D.cs)
- [FullHeightLightBlocker2D.cs](../Assets/EmergencyShift/Runtime/World/Lighting/FullHeightLightBlocker2D.cs)
- [LocalLamp2D.cs](../Assets/EmergencyShift/Runtime/World/Lighting/LocalLamp2D.cs)
- [LocalLampAudio2D.cs](../Assets/EmergencyShift/Runtime/World/Lighting/LocalLampAudio2D.cs)
- [LocalLampConfiguration2D.cs](../Assets/EmergencyShift/Runtime/World/Lighting/LocalLampConfiguration2D.cs)

## Assets/EmergencyShift/Runtime/World/Loot

- [AccessCardPickup2D.cs](../Assets/EmergencyShift/Runtime/World/Loot/AccessCardPickup2D.cs)
- [FlashlightPickup2D.cs](../Assets/EmergencyShift/Runtime/World/Loot/FlashlightPickup2D.cs)
- [FlashlightUpgradePickup.cs](../Assets/EmergencyShift/Runtime/World/Loot/FlashlightUpgradePickup.cs)
- [WeaponLootPickup.cs](../Assets/EmergencyShift/Runtime/World/Loot/WeaponLootPickup.cs)

## Assets/EmergencyShift/Runtime/World/Surfaces

- [GroundAudioSurface2D.cs](../Assets/EmergencyShift/Runtime/World/Surfaces/GroundAudioSurface2D.cs)
- [GroundAudioSurfaceQuery2D.cs](../Assets/EmergencyShift/Runtime/World/Surfaces/GroundAudioSurfaceQuery2D.cs)
- [GroundSoundSurface.cs](../Assets/EmergencyShift/Runtime/World/Surfaces/GroundSoundSurface.cs)
- [IGroundAudioRegion.cs](../Assets/EmergencyShift/Runtime/World/Surfaces/IGroundAudioRegion.cs)
- [ImpactSurface2D.cs](../Assets/EmergencyShift/Runtime/World/Surfaces/ImpactSurface2D.cs)

## Assets/EmergencyShift/Tests/EditMode

- [AccessCardRestoreTests.cs](../Assets/EmergencyShift/Tests/EditMode/AccessCardRestoreTests.cs)
- [AccessControlModelTests.cs](../Assets/EmergencyShift/Tests/EditMode/AccessControlModelTests.cs)
- [BloodPoolProfileValidationTests.cs](../Assets/EmergencyShift/Tests/EditMode/BloodPoolProfileValidationTests.cs)
- [BloodPoolTests.cs](../Assets/EmergencyShift/Tests/EditMode/BloodPoolTests.cs)
- [CeilingLightOcclusionTests.cs](../Assets/EmergencyShift/Tests/EditMode/CeilingLightOcclusionTests.cs)
- [CheckpointBooleanTests.cs](../Assets/EmergencyShift/Tests/EditMode/CheckpointBooleanTests.cs)
- [CheckpointPayloadTests.cs](../Assets/EmergencyShift/Tests/EditMode/CheckpointPayloadTests.cs)
- [CheckpointValidationTests.cs](../Assets/EmergencyShift/Tests/EditMode/CheckpointValidationTests.cs)
- [DialogueControllerTests.cs](../Assets/EmergencyShift/Tests/EditMode/DialogueControllerTests.cs)
- [DialogueDataTests.cs](../Assets/EmergencyShift/Tests/EditMode/DialogueDataTests.cs)
- [DialoguePrefabTests.cs](../Assets/EmergencyShift/Tests/EditMode/DialoguePrefabTests.cs)
- [DirectionalDamageTests.cs](../Assets/EmergencyShift/Tests/EditMode/DirectionalDamageTests.cs)
- [DoorAccessTests.cs](../Assets/EmergencyShift/Tests/EditMode/DoorAccessTests.cs)
- [EnemyDeathAudioTests.cs](../Assets/EmergencyShift/Tests/EditMode/EnemyDeathAudioTests.cs)
- [EnemyMeleeModelTests.cs](../Assets/EmergencyShift/Tests/EditMode/EnemyMeleeModelTests.cs)
- [EnemyMotorGeometryTests.cs](../Assets/EmergencyShift/Tests/EditMode/EnemyMotorGeometryTests.cs)
- [EnemyNavigationSafetyTests.cs](../Assets/EmergencyShift/Tests/EditMode/EnemyNavigationSafetyTests.cs)
- [EnemyPathModelTests.cs](../Assets/EmergencyShift/Tests/EditMode/EnemyPathModelTests.cs)
- [EnemyPatrolModelTests.cs](../Assets/EmergencyShift/Tests/EditMode/EnemyPatrolModelTests.cs)
- [EnemySystemsTests.cs](../Assets/EmergencyShift/Tests/EditMode/EnemySystemsTests.cs)
- [EnvironmentPresentationTests.cs](../Assets/EmergencyShift/Tests/EditMode/EnvironmentPresentationTests.cs)
- [FlashlightOwnershipTests.cs](../Assets/EmergencyShift/Tests/EditMode/FlashlightOwnershipTests.cs)
- [FlashlightStateTests.cs](../Assets/EmergencyShift/Tests/EditMode/FlashlightStateTests.cs)
- [FlashlightUpgradeModelTests.cs](../Assets/EmergencyShift/Tests/EditMode/FlashlightUpgradeModelTests.cs)
- [GoreCompositionTests.cs](../Assets/EmergencyShift/Tests/EditMode/GoreCompositionTests.cs)
- [GroundAudioSurfaceQueryTests.cs](../Assets/EmergencyShift/Tests/EditMode/GroundAudioSurfaceQueryTests.cs)
- [HealthModelTests.cs](../Assets/EmergencyShift/Tests/EditMode/HealthModelTests.cs)
- [LevelCompositionFailureTests.cs](../Assets/EmergencyShift/Tests/EditMode/LevelCompositionFailureTests.cs)
- [LevelCompositionTests.cs](../Assets/EmergencyShift/Tests/EditMode/LevelCompositionTests.cs)
- [LocalLampAudioTests.cs](../Assets/EmergencyShift/Tests/EditMode/LocalLampAudioTests.cs)
- [LocalLampSceneSettingsTests.cs](../Assets/EmergencyShift/Tests/EditMode/LocalLampSceneSettingsTests.cs)
- [MagazineWeaponTests.cs](../Assets/EmergencyShift/Tests/EditMode/MagazineWeaponTests.cs)
- [PauseMenuEnvironmentTests.cs](../Assets/EmergencyShift/Tests/EditMode/PauseMenuEnvironmentTests.cs)
- [PauseMenuTests.cs](../Assets/EmergencyShift/Tests/EditMode/PauseMenuTests.cs)
- [PersistenceTests.cs](../Assets/EmergencyShift/Tests/EditMode/PersistenceTests.cs)
- [PlayerInputTests.cs](../Assets/EmergencyShift/Tests/EditMode/PlayerInputTests.cs)
- [PlayerMovementTests.cs](../Assets/EmergencyShift/Tests/EditMode/PlayerMovementTests.cs)
- [RemainsValidationTests.cs](../Assets/EmergencyShift/Tests/EditMode/RemainsValidationTests.cs)
- [SceneRegistryTests.cs](../Assets/EmergencyShift/Tests/EditMode/SceneRegistryTests.cs)
- [SleeperModelTests.cs](../Assets/EmergencyShift/Tests/EditMode/SleeperModelTests.cs)
- [SleeperTests.cs](../Assets/EmergencyShift/Tests/EditMode/SleeperTests.cs)
- [StalkerEyeTests.cs](../Assets/EmergencyShift/Tests/EditMode/StalkerEyeTests.cs)
- [StalkerModelTests.cs](../Assets/EmergencyShift/Tests/EditMode/StalkerModelTests.cs)
- [WeaponAimEnvironmentTests.cs](../Assets/EmergencyShift/Tests/EditMode/WeaponAimEnvironmentTests.cs)
- [WeaponControllerTests.cs](../Assets/EmergencyShift/Tests/EditMode/WeaponControllerTests.cs)
- [WeaponCycleTests.cs](../Assets/EmergencyShift/Tests/EditMode/WeaponCycleTests.cs)
- [WeaponSettingsTests.cs](../Assets/EmergencyShift/Tests/EditMode/WeaponSettingsTests.cs)
- [WorkBudgetTests.cs](../Assets/EmergencyShift/Tests/EditMode/WorkBudgetTests.cs)
- [WorldProjectionTests.cs](../Assets/EmergencyShift/Tests/EditMode/WorldProjectionTests.cs)

## Assets/EmergencyShift/Tests/PlayMode

- [AccessControlSceneTests.cs](../Assets/EmergencyShift/Tests/PlayMode/AccessControlSceneTests.cs)
- [AdaptiveAudioProbeRegressionTests.cs](../Assets/EmergencyShift/Tests/PlayMode/AdaptiveAudioProbeRegressionTests.cs)
- [CameraOpticsLifetimeTests.cs](../Assets/EmergencyShift/Tests/PlayMode/CameraOpticsLifetimeTests.cs)
- [CameraOpticsTestSource.cs](../Assets/EmergencyShift/Tests/PlayMode/CameraOpticsTestSource.cs)
- [CheckpointSaveLifetimeTests.cs](../Assets/EmergencyShift/Tests/PlayMode/CheckpointSaveLifetimeTests.cs)
- [DialogueSessionLifetimeTests.cs](../Assets/EmergencyShift/Tests/PlayMode/DialogueSessionLifetimeTests.cs)
- [EnemyMeleeSceneTests.cs](../Assets/EmergencyShift/Tests/PlayMode/EnemyMeleeSceneTests.cs)
- [EnemyMotorSceneTests.cs](../Assets/EmergencyShift/Tests/PlayMode/EnemyMotorSceneTests.cs)
- [EnemyNavigationSceneTests.cs](../Assets/EmergencyShift/Tests/PlayMode/EnemyNavigationSceneTests.cs)
- [EnemyPatrolSceneTests.cs](../Assets/EmergencyShift/Tests/PlayMode/EnemyPatrolSceneTests.cs)
- [FlashlightProgressionSceneTests.cs](../Assets/EmergencyShift/Tests/PlayMode/FlashlightProgressionSceneTests.cs)
- [PlayerMovementSceneTests.cs](../Assets/EmergencyShift/Tests/PlayMode/PlayerMovementSceneTests.cs)
- [SleeperBrainSceneTests.cs](../Assets/EmergencyShift/Tests/PlayMode/SleeperBrainSceneTests.cs)
- [StalkerBrainSceneTests.cs](../Assets/EmergencyShift/Tests/PlayMode/StalkerBrainSceneTests.cs)
- [TransientBloodSceneTests.cs](../Assets/EmergencyShift/Tests/PlayMode/TransientBloodSceneTests.cs)
- [WeaponSpreadLifetimeTests.cs](../Assets/EmergencyShift/Tests/PlayMode/WeaponSpreadLifetimeTests.cs)

## Concepts/Unused/Visual/SleeperSpriteRig01/AuthoringDrafts

- [SleeperJawBakeSource.cs](../Concepts/Unused/Visual/SleeperSpriteRig01/AuthoringDrafts/SleeperJawBakeSource.cs)
- [SleeperSpriteBaker.cs](../Concepts/Unused/Visual/SleeperSpriteRig01/AuthoringDrafts/SleeperSpriteBaker.cs)

## Concepts/Unused/Visual/SleeperSpriteRig01/PausedAttempt

- [draft-SleeperJawBakeSource.cs](../Concepts/Unused/Visual/SleeperSpriteRig01/PausedAttempt/draft-SleeperJawBakeSource.cs)
- [draft-SleeperSpriteBaker.cs](../Concepts/Unused/Visual/SleeperSpriteRig01/PausedAttempt/draft-SleeperSpriteBaker.cs)
- [SleeperJawBakeSource.cs](../Concepts/Unused/Visual/SleeperSpriteRig01/PausedAttempt/SleeperJawBakeSource.cs)
- [SleeperSpriteBaker.cs](../Concepts/Unused/Visual/SleeperSpriteRig01/PausedAttempt/SleeperSpriteBaker.cs)

## Concepts/Used/Environment/CardDoorIndicators01

- [create.cs](../Concepts/Used/Environment/CardDoorIndicators01/create.cs)

## Concepts/Used/Environment/RoomFinish01

- [create.cs](../Concepts/Used/Environment/RoomFinish01/create.cs)

## Сборки

- [EmergencyShift.Editor.asmdef](../Assets/EmergencyShift/Editor/EmergencyShift.Editor.asmdef)
- [EmergencyShift.Development.asmdef](../Assets/EmergencyShift/Runtime/Development/EmergencyShift.Development.asmdef)
- [EmergencyShift.Runtime.asmdef](../Assets/EmergencyShift/Runtime/EmergencyShift.Runtime.asmdef)
- [EmergencyShift.EditModeTests.asmdef](../Assets/EmergencyShift/Tests/EditMode/EmergencyShift.EditModeTests.asmdef)
- [EmergencyShift.PlayModeTests.asmdef](../Assets/EmergencyShift/Tests/PlayMode/EmergencyShift.PlayModeTests.asmdef)

## Сохранённые сцены

- [EnvironmentLibraryReview.unity](../Assets/EmergencyShift/Scenes/ArtReview/EnvironmentLibraryReview.unity)
- [EnvironmentNarrativeStressReview.unity](../Assets/EmergencyShift/Scenes/ArtReview/EnvironmentNarrativeStressReview.unity)
- [EnvironmentPropsDarkReview.unity](../Assets/EmergencyShift/Scenes/ArtReview/EnvironmentPropsDarkReview.unity)
- [EnvironmentPropsReview.unity](../Assets/EmergencyShift/Scenes/ArtReview/EnvironmentPropsReview.unity)
- [FloorMarkingsReview.unity](../Assets/EmergencyShift/Scenes/ArtReview/FloorMarkingsReview.unity)
- [FurnitureLightStudy.unity](../Assets/EmergencyShift/Scenes/ArtReview/FurnitureLightStudy.unity)
- [LightingPlayground.unity](../Assets/EmergencyShift/Scenes/ArtReview/LightingPlayground.unity)
- [BasementPrototype.unity](../Assets/EmergencyShift/Scenes/BasementPrototype.unity)
- [LevelTemplate.unity](../Assets/EmergencyShift/Scenes/LevelTemplate.unity)
- [MechanicsSandbox.unity](../Assets/EmergencyShift/Scenes/MechanicsSandbox.unity)
- [MorgueRouteTest.unity](../Assets/EmergencyShift/Scenes/MorgueRouteTest.unity)
