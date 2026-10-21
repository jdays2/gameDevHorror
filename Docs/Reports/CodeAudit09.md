# Пофайловый аудит SOD · освещение, кровь, останки и полное покрытие C# · 09

Дата: 2026-10-21. База Git: `e39d140745b2fa1aebd9d7a6843b58363302448a`; проверено текущее рабочее дерево, включая существующие незакоммиченные файлы меню паузы. Игровой C#, тесты, сцены, prefab, конфиги, арт, звук и GUID не менялись. Это чтение и фиксация проблем, без исправлений.

## Покрытие и смысл статусов

Завершено **покрытие исходников 269/269 собственных C#**: 250 под Assets/EmergencyShift, 11 AgentScripts и 8 исторических Concepts. В этом проходе проверен оставшийся **121 файл / 20024 строки**: 70 reviewed_boundary / 51 partial. Выведен из очереди 71 файл; полностью перечитаны 50 ранее ограниченно проверенных источников. Остальные 148 имеют полное чтение в CodeAudit05–08 и актуальные SHA-256.

Общий реестр: **148 reviewed_boundary / 121 partial / 0 queued**. В [CodeAudit.json](CodeAudit.json) у каждого файла есть ответственность, зависимости, использование, заключение, область чтения, отчёт и актуальный reviewedSha256; прошлые заключения сохранены в reviewHistory. `sourceCoverageComplete=true`; старый `complete=false` сохранён из-за архитектурного долга и непроведённых runtime-проверок. **Partial означает полностью прочитанный файл с долгом, а не пропущенный файл.**

Два draft источника PausedAttempt побайтово равны соседним исходникам: SHA-256 jaw `567398345a98cc518edaf57b6f083a2283d728e5a7772281be1a94014266536a`, baker `d4f7af18dc5d4db0539435e64f5ea7516c2ab7ee0eb49b9536e81b9441fa06a4`. Jaw прочитан полностью; baker проверен как полностью прочитанный AuthoringDrafts + полный diff к PausedAttempt. Это архивный offline материал, не активный дизайн врага.

Покрытие относится к C# и связанным контрактам SOD, не к визуальной приёмке всех сцен/шейдеров/ассетов, сторонним пакетам, миграции всех фич или прохождению всей игры.

## Основные выводы

- **Освещение:** LocalLightProfile/LocalLampAudioSet явно проверяют числа и refs, runtime-фаза остаётся личной. LocalLamp2D совмещает визуальный цикл и запрос света; static active/FullHeightLightBlocker registry ещё не scoped. AnyIlluminates не ограничен owner scene — риск будущих additive consumers; текущие callers нельзя объявлять сломанными без проверки. Камера считает framing по ownership/range, но связана с concrete Flashlight2D. Авторенные потолочные лампы/их параметры не менялись.
- **Cookie и тени:** CeilingLightOcclusion освобождает Sprite/Texture при disable, кеширует geometry hash; automatic blockers фильтруются по сцене. Explicit blockers могут прийти извне. При перестроении до 512 лучей на лампу × blockers и upload 128²; это оценка структуры цикла, не измерение FPS. Отклонённые пользователем высотные/длинные тени не возвращались.
- **Кровь/останки:** один emitted gate и существующий world budget сохраняют one-shot death; постоянный scene record отделён от actor, settled части не продолжают AI. Тем не менее DTO содержит Unity Capture/Apply, а nested validation неполна (BUG-014). Transient sprays имеют неверный scene owner (BUG-015), validation blood config неполна (BUG-017). Горячая генерация и cosmetic Unity RNG остаются долгом; влияние общего RNG уже учтено BUG-010.
- **Core/scopes:** чистые HealthModel/settings/commands, clock/RNG adapters, quota и личные scene registries имеют понятную ответственность. Новая фабрика возвращает отдельный model. Scoped surfaces и audio death cache не требуют global lookup; player motor/footsteps/audio director ещё совмещают controller/state/Unity adapters.
- **HUD/инструменты:** GameplayHudView имеет подписки/отписки, но concrete player/polling и неполную validation style. PrototypeHud форматирует текст каждый Update. Development probes и one-off authoring не считаются production доменом. AdaptiveAudioProbe несовместима с текущим API (BUG-016). Старые builders с once-only/dirty guards не запускались; snapshot-проверка после мутации не является rollback.
- **Authoring risks:** CleanUnusedShadowOverrides.InspectChangesAt не имеет loaded/dirty guard, в отличие от Apply; всегда закрывает открываемую сцену. Поведение при уже открытой dirty target требует безопасного отдельного Editor-теста, потеря данных не объявляется воспроизведённой. NarrativeEnvironmentBuilder.PrepareLighting/HospitalServicePrefabAuthoring могут перезаписывать общие assets; GameplayHudAuthoring глобально ищет cards, не фильтруя scene. Эти ограничения сохранены в пофайловых выводах; утилиты не вызывались.
- **Исторические ассеты:** RoomFinish01/create.cs действительно собирает drain из нескольких GameObject — свидетельство для уже открытого BUG-007, без дубликата. Архивные Sleeper rig исходники не возвращены в игру, BUG-006 об индивидуальности врагов остаётся открыт.

## Новые баги в общей доске

Все четыре созданы с типом «Баг», высоким приоритетом и статусом «Запланировано». Проверка статическая; runtime-контрпримеры ещё не исполнялись. Старые баги не закрывались.

### [BUG-014 · Checkpoint останков не проверяет вложенные shadow и pool до изменения мира](https://app.notion.com/p/4008d6dc06b681e3bc92d86b33bef084)

GoreBurst2D.ValidateRemains:187–215 проверяет layout/индексы/body, но пропускает hasShadow=true с shadow=null, null элементы pool и неконечные значения вложенного pose. RemainsRecord2D.Restore:104–148 создаёт pool/части и затем вызывает saved.pool[i].Apply или part.shadow.Apply. LevelCheckpointController2D.Apply:233–272 сначала проверяет все entities, затем PrepareRestore/Restore.

Контрпример: В копии захваченного checkpoint мёртвого врага с частями установить parts[0].hasShadow=true и parts[0].shadow=null; вызвать ValidateRemains, затем полный Apply. Отдельно проверить hasPool=true и pool[0]=null. По исходникам validator допускает эти варианты, Restore обращается к null уже после начала изменения мира.

Ожидается: Полная вложенная проверка presence/finite/shape отклоняет snapshot до PrepareRestore; текущий мир остаётся неизменным.

Граница доказательства: Статический контрпример. Unity/JsonUtility и сохранность мира после отказа в этом проходе не запускались. Отдельно от BUG-011 общего entity pose и BUG-013 bool payload.

### [BUG-015 · Временные эффекты крови создаются в активной сцене вместо сцены врага](https://app.notion.com/p/4008d6dc06b681e49958cbdf3c845c32)

EnemyHitResponse2D.Play:64–66 создаёт root Bullet blood spray, GoreBurst2D.Play:93–95 — Blood rupture burst через new GameObject, без parent/MoveGameObjectToScene. В отличие от этого CreateRemainsRoot:178–184 явно закрепляет постоянные останки за gameObject.scene.

Контрпример: Загрузить сцены A и B additively, враг в A, активная B. Попасть/убить врага A и сразу проверить scene transient VFX, затем выгрузить A до окончания .24/.36 секунды эффекта.

Ожидается: Transient VFX принадлежат исходной сцене A и удаляются вместе с ней; активность другой сцены не меняет владельца.

Граница доказательства: Непривязанный root подтверждён исходниками; сценарий с двумя сценами и unload не выполнялся. Текущий одиночный уровень не объявляется визуально сломанным.

### [BUG-016 · AdaptiveAudioProbe использует удалённое поле Ammo и поверхности без injection](https://app.notion.com/p/4008d6dc06b6813a86a7c0946e195883)

AdaptiveAudioProbe.Set:63–71 бросает исключение при отсутствии private field; Tests:237 ищет <Ammo>k__BackingField. PlayerWeapon2D.Ammo:39 теперь вычисляемое property от inventory.ActiveCycle, backing field отсутствует. Временные GroundAudioSurface2D создаются на 246–250 без context.InjectScoped, тогда как их registry приходит через injection.

Контрпример: Запустить существующую opt-in аудиопробу в отдельном Play-сеансе и дойти до dry-fire шага. После актуализации шага проверить все три материала пола: временные regions должны зарегистрироваться в том же scope, что feet query.

Ожидается: Проба работает через актуальный weapon API/fixture и scoped surface registration, доходит до конца и реально различает поверхности.

Граница доказательства: Ошибка reflection доказана структурой кода, но нового Unity-прогона нет. Это сломанная проверка, не подтверждённый дефект звуков шагов игрока.

### [BUG-017 · BloodPoolProfile.IsValid допускает NaN размера и бесконечные параметры](https://app.notion.com/p/4008d6dc06b681be9cfdccad6f8fcd88)

BloodPoolProfile.IsValid:37–65 проверяет canvasSize только через <=0; для NaN условие ложно. Несколько диапазонов/growSeconds пропускают positive infinity, цвета не проверяются. BloodPool2D.InitializeTextured:151–175 использует canvasSize для width и localScale после успешного IsValid.

Контрпример: Клонировать валидный профиль в тесте, присвоить variants[0].canvasSize=float.NaN: IsValid по коду остаётся true. Отдельно growSeconds=PositiveInfinity или scaleRange.y=PositiveInfinity. Невалидный asset не сохранять в библиотеку.

Ожидается: Runtime validation отвергает все неконечные числа до создания/изменения renderer; инспекторные Min/Range не заменяют эту проверку.

Граница доказательства: Статический анализ валидатора; наличие плохих значений в текущих игровых asset не обнаружено/не утверждается. EditMode-контрпример не запускался.

## Проверки и статусы

Прочитаны 11 существующих тестовых файлов / 47 случаев (Test + TestCase); они не запускались. `unity command editor_status` с явным project-path/caller/skill вернул отсутствие Pipeline instance. Editor/Play не запускались, build/Profiler/скриншоты не создавались.

Механическая проверка `Tools/Maintenance/audit_code.py` сверяет инвентарь и SHA-256, но не выносит semantic verdict. Проверены полнота reviewScope/ответственности/зависимостей/usage, счётчики, отсутствие queued/stale источников и сохранность предшествующих изменений. Локальные Markdown links и GUID scan — проверка документации; внешние package GUID не приравниваются missing game refs.

`audit_code.py`: 269 источников, added/removed/changed/staleReviews пусты. Content scan: 232 внешних/неразрешённых GUID (включая package ссылки), одна прежняя битая ссылка Historical/HospitalService01 — уже BUG-005. Новых битых локальных ссылок отчёта нет. Сверены неизменность 148 прежних полных заключений и хеши 68 существовавших изменённых/неотслеживаемых файлов; в коммит входят только этот отчёт, реестр и одна новая строка индекса.

Рабочие статусы — [общая доска](https://app.notion.com/p/a1b30c6e0f8343168ea91ae5990ee9fb). Закрываются только [TASK-SOD-18](https://app.notion.com/p/4008d6dc06b68160b76fe101269b60f4) о чтении прохода и [TASK-SOD-03](https://app.notion.com/p/3ff8d6dc06b6819fbbc3c56bd63e6b5e) об инвентаре каждого файла. MVC/DI, полная validation, команды/зависимости, lifecycle/performance, сцены, финальная проверка и авторская приёмка остаются открытыми — соответствующие карточки совмещают аудит с переносом/проверкой исправлений.

## Пофайловый указатель этого прохода

Ниже компактный индекс. Полные замечания/зависимости/usage/history и хеши — в [реестре](CodeAudit.json); это техническое свидетельство, не дублирующий Notion план.

| Файл | Ответственность | Результат |
|---|---|---|
| [AgentScripts/AssignPlayerInput.cs](../../AgentScripts/AssignPlayerInput.cs) | Одноразовый input authoring | partial |
| [AgentScripts/AuditProjectReferences.cs](../../AgentScripts/AuditProjectReferences.cs) | Read-only отчёт ссылок | reviewed_boundary |
| [AgentScripts/CheckDirectionalDamage.cs](../../AgentScripts/CheckDirectionalDamage.cs) | Play fixture HUD крови | partial |
| [AgentScripts/CheckGroundAudioScope.cs](../../AgentScripts/CheckGroundAudioScope.cs) | Play fixture двух звуковых scopes | reviewed_boundary |
| [AgentScripts/CheckInjectedActorCreation.cs](../../AgentScripts/CheckInjectedActorCreation.cs) | Play fixture actor factories | reviewed_boundary |
| [AgentScripts/CheckWorldServices.cs](../../AgentScripts/CheckWorldServices.cs) | Play fixture world quotas/remains | reviewed_boundary |
| [AgentScripts/CleanUnusedShadowOverrides.cs](../../AgentScripts/CleanUnusedShadowOverrides.cs) | Проверка/очистка старых overrides | partial |
| [AgentScripts/InstallDirectionalDamage.cs](../../AgentScripts/InstallDirectionalDamage.cs) | Одноразовый authoring HUD крови | partial |
| [AgentScripts/InstallPauseMenu.cs](../../AgentScripts/InstallPauseMenu.cs) | Одноразовый authoring меню паузы | partial |
| [AgentScripts/InstallWorldComposition.cs](../../AgentScripts/InstallWorldComposition.cs) | Authoring ProjectContext | reviewed_boundary |
| [AgentScripts/OrganizeMaterials.cs](../../AgentScripts/OrganizeMaterials.cs) | Безопасный MoveAsset материалов | reviewed_boundary |
| [Editor/Authoring/EnvironmentLibraryReviewBuilder.cs](../../Assets/EmergencyShift/Editor/Authoring/EnvironmentLibraryReviewBuilder.cs) | Одноразовая scene библиотеки окружения | partial |
| [Editor/Authoring/EnvironmentReviewBuilder.cs](../../Assets/EmergencyShift/Editor/Authoring/EnvironmentReviewBuilder.cs) | Одноразовая scene сравнения окружения | partial |
| [Editor/Authoring/EnvironmentSpriteAuthoring.cs](../../Assets/EmergencyShift/Editor/Authoring/EnvironmentSpriteAuthoring.cs) | Offline import и Tight-outline sprite | reviewed_boundary |
| [Editor/Authoring/FloorMarkingsAuthoring.cs](../../Assets/EmergencyShift/Editor/Authoring/FloorMarkingsAuthoring.cs) | Offline генерация floor overlays | reviewed_boundary |
| [Editor/Authoring/FurnitureLightStudyBuilder.cs](../../Assets/EmergencyShift/Editor/Authoring/FurnitureLightStudyBuilder.cs) | Authoring dev A/B/C light scene | partial |
| [Editor/Authoring/GameplayHudAuthoring.cs](../../Assets/EmergencyShift/Editor/Authoring/GameplayHudAuthoring.cs) | Authoring reusable HUD в Canvas | partial |
| [Editor/Authoring/HospitalServicePrefabAuthoring.cs](../../Assets/EmergencyShift/Editor/Authoring/HospitalServicePrefabAuthoring.cs) | Authoring family hospital prefab | partial |
| [Editor/Authoring/LevelAuthoringSetup.cs](../../Assets/EmergencyShift/Editor/Authoring/LevelAuthoringSetup.cs) | Authoring missing level rig/template | partial |
| [Editor/Authoring/NarrativeEnvironmentBuilder.cs](../../Assets/EmergencyShift/Editor/Authoring/NarrativeEnvironmentBuilder.cs) | Одноразовая narrative stress scene | partial |
| [Editor/Authoring/SandboxSceneBuilder.cs](../../Assets/EmergencyShift/Editor/Authoring/SandboxSceneBuilder.cs) | Одноразовый authoring MechanicsSandbox | partial |
| [Editor/Build/ThirdPartyLicenseBuildStep.cs](../../Assets/EmergencyShift/Editor/Build/ThirdPartyLicenseBuildStep.cs) | Копирование сторонней лицензии в build | reviewed_boundary |
| [Editor/Inspectors/EnemyPatrol2DEditor.cs](../../Assets/EmergencyShift/Editor/Inspectors/EnemyPatrol2DEditor.cs) | Редактирование личной patrol area | reviewed_boundary |
| [Editor/Inspectors/LevelAudioDirectorEditor.cs](../../Assets/EmergencyShift/Editor/Inspectors/LevelAudioDirectorEditor.cs) | Диагностический Inspector audio director | reviewed_boundary |
| [Runtime/Core/Commands/CommandResult.cs](../../Assets/EmergencyShift/Runtime/Core/Commands/CommandResult.cs) | Значение результата команды | reviewed_boundary |
| [Runtime/Core/Commands/ICommand.cs](../../Assets/EmergencyShift/Runtime/Core/Commands/ICommand.cs) | Проверяемый контракт команды | reviewed_boundary |
| [Runtime/Core/Composition/AssemblyInfo.cs](../../Assets/EmergencyShift/Runtime/Core/Composition/AssemblyInfo.cs) | Доступ тестов к internal API | reviewed_boundary |
| [Runtime/Core/Composition/SceneRegistry.cs](../../Assets/EmergencyShift/Runtime/Core/Composition/SceneRegistry.cs) | Личный упорядоченный scene registry | reviewed_boundary |
| [Runtime/Core/Composition/WorldInstaller.cs](../../Assets/EmergencyShift/Runtime/Core/Composition/WorldInstaller.cs) | Composition общих world services | reviewed_boundary |
| [Runtime/Core/Contracts/Damageable2D.cs](../../Assets/EmergencyShift/Runtime/Core/Contracts/Damageable2D.cs) | Общий Unity combat receiver | reviewed_boundary |
| [Runtime/Core/Contracts/IThreatLight2D.cs](../../Assets/EmergencyShift/Runtime/Core/Contracts/IThreatLight2D.cs) | Контракт защитного светового стимула | reviewed_boundary |
| [Runtime/Core/Randomness/IRandomSource.cs](../../Assets/EmergencyShift/Runtime/Core/Randomness/IRandomSource.cs) | Контракт источника случайности | reviewed_boundary |
| [Runtime/Core/Randomness/RandomSourceFactory.cs](../../Assets/EmergencyShift/Runtime/Core/Randomness/RandomSourceFactory.cs) | Создание отдельных RNG streams | reviewed_boundary |
| [Runtime/Core/Randomness/SystemRandomSource.cs](../../Assets/EmergencyShift/Runtime/Core/Randomness/SystemRandomSource.cs) | Адаптер личного System.Random | reviewed_boundary |
| [Runtime/Core/Scheduling/FrameWorkBudget.cs](../../Assets/EmergencyShift/Runtime/Core/Scheduling/FrameWorkBudget.cs) | Квота работы на кадр | reviewed_boundary |
| [Runtime/Core/Time/IFrameClock.cs](../../Assets/EmergencyShift/Runtime/Core/Time/IFrameClock.cs) | Контракт номера кадра | reviewed_boundary |
| [Runtime/Core/Time/IGameClock.cs](../../Assets/EmergencyShift/Runtime/Core/Time/IGameClock.cs) | Контракт игрового времени | reviewed_boundary |
| [Runtime/Core/Time/IWorldTimeScale.cs](../../Assets/EmergencyShift/Runtime/Core/Time/IWorldTimeScale.cs) | Контракт global time adapter | reviewed_boundary |
| [Runtime/Core/Time/UnityFrameClock.cs](../../Assets/EmergencyShift/Runtime/Core/Time/UnityFrameClock.cs) | Unity адаптер номера кадра | reviewed_boundary |
| [Runtime/Core/Time/UnityGameClock.cs](../../Assets/EmergencyShift/Runtime/Core/Time/UnityGameClock.cs) | Unity адаптер scaled time | reviewed_boundary |
| [Runtime/Core/Time/UnityWorldTimeScale.cs](../../Assets/EmergencyShift/Runtime/Core/Time/UnityWorldTimeScale.cs) | Unity адаптер timeScale | reviewed_boundary |
| [Runtime/Definitions/AudioMoodProfile.cs](../../Assets/EmergencyShift/Runtime/Definitions/AudioMoodProfile.cs) | Общий адаптивный audio tuning | partial |
| [Runtime/Definitions/BloodPoolProfile.cs](../../Assets/EmergencyShift/Runtime/Definitions/BloodPoolProfile.cs) | Общие варианты лужи и размеры капель | partial |
| [Runtime/Definitions/EnemyDeathAudioSet.cs](../../Assets/EmergencyShift/Runtime/Definitions/EnemyDeathAudioSet.cs) | Общие клипы смерти | reviewed_boundary |
| [Runtime/Definitions/GameplayHudStyle.cs](../../Assets/EmergencyShift/Runtime/Definitions/GameplayHudStyle.cs) | Общие параметры оформления HUD | partial |
| [Runtime/Definitions/GoreProfile.cs](../../Assets/EmergencyShift/Runtime/Definitions/GoreProfile.cs) | Варианты состава разрыва тела | partial |
| [Runtime/Definitions/LocalLampAudioSet.cs](../../Assets/EmergencyShift/Runtime/Definitions/LocalLampAudioSet.cs) | Общие клипы и параметры слышимости лампы | reviewed_boundary |
| [Runtime/Definitions/LocalLampSceneSettings.cs](../../Assets/EmergencyShift/Runtime/Definitions/LocalLampSceneSettings.cs) | Личные overrides света/звука в сцене | partial |
| [Runtime/Definitions/LocalLightProfile.cs](../../Assets/EmergencyShift/Runtime/Definitions/LocalLightProfile.cs) | Общие световые данные и оценка цикла | reviewed_boundary |
| [Runtime/Development/AdaptiveAudioProbe.cs](../../Assets/EmergencyShift/Runtime/Development/AdaptiveAudioProbe.cs) | Историческая audio regression fixture | partial |
| [Runtime/Development/CombatProbeTarget2D.cs](../../Assets/EmergencyShift/Runtime/Development/CombatProbeTarget2D.cs) | Instrumentation fake damage target | reviewed_boundary |
| [Runtime/Development/CrowdReadinessProbe.cs](../../Assets/EmergencyShift/Runtime/Development/CrowdReadinessProbe.cs) | Editor crowd fixture | partial |
| [Runtime/Development/DevelopmentBuildSmoke.cs](../../Assets/EmergencyShift/Runtime/Development/DevelopmentBuildSmoke.cs) | Opt-in native smoke | partial |
| [Runtime/Development/EngineeringBenchmark2D.cs](../../Assets/EmergencyShift/Runtime/Development/EngineeringBenchmark2D.cs) | Dev benchmark crowd/path/death | partial |
| [Runtime/Development/EnvironmentLoadProbe.cs](../../Assets/EmergencyShift/Runtime/Development/EnvironmentLoadProbe.cs) | Историческая измерительная fixture окружения | partial |
| [Runtime/Development/FurnitureLightBenchmark.cs](../../Assets/EmergencyShift/Runtime/Development/FurnitureLightBenchmark.cs) | Dev native benchmark света мебели | partial |
| [Runtime/Development/FurnitureLightStudy2D.cs](../../Assets/EmergencyShift/Runtime/Development/FurnitureLightStudy2D.cs) | Dev управление сравнением освещения | partial |
| [Runtime/Development/FurnitureLightStudyProfile.cs](../../Assets/EmergencyShift/Runtime/Development/FurnitureLightStudyProfile.cs) | Общий профиль dev A/B/C исследования | reviewed_boundary |
| [Runtime/Development/SandboxRegressionProbe.cs](../../Assets/EmergencyShift/Runtime/Development/SandboxRegressionProbe.cs) | Исторический sandbox route probe | partial |
| [Runtime/Development/SleeperRegressionProbe.cs](../../Assets/EmergencyShift/Runtime/Development/SleeperRegressionProbe.cs) | Изолированная регрессия второго вида | partial |
| [Runtime/Features/Health/Config/HealthSettings.cs](../../Assets/EmergencyShift/Runtime/Features/Health/Config/HealthSettings.cs) | Неизменяемый health tuning | reviewed_boundary |
| [Runtime/Features/Health/HealthInstaller.cs](../../Assets/EmergencyShift/Runtime/Features/Health/HealthInstaller.cs) | Composition фабрики здоровья | reviewed_boundary |
| [Runtime/Features/Health/Logic/DamageHealthCommand.cs](../../Assets/EmergencyShift/Runtime/Features/Health/Logic/DamageHealthCommand.cs) | Команда нанесения урона | reviewed_boundary |
| [Runtime/Features/Health/Logic/HealthModel.cs](../../Assets/EmergencyShift/Runtime/Features/Health/Logic/HealthModel.cs) | Чистое личное состояние здоровья | reviewed_boundary |
| [Runtime/Features/Health/Logic/HealthModelFactory.cs](../../Assets/EmergencyShift/Runtime/Features/Health/Logic/HealthModelFactory.cs) | Создание индивидуальных HealthModel | reviewed_boundary |
| [Runtime/Features/Health/Logic/SetHealthCapacityCommand.cs](../../Assets/EmergencyShift/Runtime/Features/Health/Logic/SetHealthCapacityCommand.cs) | Команда смены временного capacity | reviewed_boundary |
| [Runtime/Levels/LevelRegistriesInstaller.cs](../../Assets/EmergencyShift/Runtime/Levels/LevelRegistriesInstaller.cs) | Composition scene registries/factories | reviewed_boundary |
| [Runtime/Player/CameraFollow2D.cs](../../Assets/EmergencyShift/Runtime/Player/CameraFollow2D.cs) | Камера и framing по текущему фонарю | partial |
| [Runtime/Player/PlayerFootsteps2D.cs](../../Assets/EmergencyShift/Runtime/Player/PlayerFootsteps2D.cs) | Шаги от фактического пути и материала | partial |
| [Runtime/Player/PlayerMotor2D.cs](../../Assets/EmergencyShift/Runtime/Player/PlayerMotor2D.cs) | Ввод движения и запись Rigidbody игрока | partial |
| [Runtime/Presentation/Audio/EnemyDeathAudioVariants.cs](../../Assets/EmergencyShift/Runtime/Presentation/Audio/EnemyDeathAudioVariants.cs) | Личный runtime cache вариантов смерти | reviewed_boundary |
| [Runtime/Presentation/Audio/LevelAudioDirector2D.cs](../../Assets/EmergencyShift/Runtime/Presentation/Audio/LevelAudioDirector2D.cs) | Выбор audio mood и mix по perception | partial |
| [Runtime/Presentation/Visuals/BloodHitSpray2D.cs](../../Assets/EmergencyShift/Runtime/Presentation/Visuals/BloodHitSpray2D.cs) | Короткий брызг крови при попадании | partial |
| [Runtime/Presentation/Visuals/BloodPool2D.cs](../../Assets/EmergencyShift/Runtime/Presentation/Visuals/BloodPool2D.cs) | Процедурная лужа и прилёт капель | partial |
| [Runtime/Presentation/Visuals/BloodRuptureBurst2D.cs](../../Assets/EmergencyShift/Runtime/Presentation/Visuals/BloodRuptureBurst2D.cs) | Короткий разрывной брызг при смерти | partial |
| [Runtime/Presentation/Visuals/CreatureGroundShadow2D.cs](../../Assets/EmergencyShift/Runtime/Presentation/Visuals/CreatureGroundShadow2D.cs) | Контактная тень персонажа/останка | partial |
| [Runtime/Presentation/Visuals/CreatureSpriteAnimator2D.cs](../../Assets/EmergencyShift/Runtime/Presentation/Visuals/CreatureSpriteAnimator2D.cs) | Общий visual animator персонажа | partial |
| [Runtime/Presentation/Visuals/EnemyDeathEffects2D.cs](../../Assets/EmergencyShift/Runtime/Presentation/Visuals/EnemyDeathEffects2D.cs) | Мост события смерти в gore presentation | reviewed_boundary |
| [Runtime/Presentation/Visuals/EnemyHitResponse2D.cs](../../Assets/EmergencyShift/Runtime/Presentation/Visuals/EnemyHitResponse2D.cs) | Визуальный отклик и кровь попадания | partial |
| [Runtime/Presentation/Visuals/GibMotion2D.cs](../../Assets/EmergencyShift/Runtime/Presentation/Visuals/GibMotion2D.cs) | Полёт/контакт/оседание одной части | partial |
| [Runtime/Presentation/Visuals/GoreBurst2D.cs](../../Assets/EmergencyShift/Runtime/Presentation/Visuals/GoreBurst2D.cs) | Однократная смерть и создание останков | partial |
| [Runtime/Presentation/Visuals/GoreSpawnBudget2D.cs](../../Assets/EmergencyShift/Runtime/Presentation/Visuals/GoreSpawnBudget2D.cs) | World quota существующего gore spawn | reviewed_boundary |
| [Runtime/Presentation/Visuals/RemainsPartIdentity2D.cs](../../Assets/EmergencyShift/Runtime/Presentation/Visuals/RemainsPartIdentity2D.cs) | Индекс и settled presentation одной части | reviewed_boundary |
| [Runtime/Presentation/Visuals/RemainsRecord2D.cs](../../Assets/EmergencyShift/Runtime/Presentation/Visuals/RemainsRecord2D.cs) | Постоянный scene record и restore останков | partial |
| [Runtime/Presentation/Visuals/RemainsSnapshot.cs](../../Assets/EmergencyShift/Runtime/Presentation/Visuals/RemainsSnapshot.cs) | DTO позы лужи и частей останков | partial |
| [Runtime/Presentation/Visuals/RemainsVisualStyle.cs](../../Assets/EmergencyShift/Runtime/Presentation/Visuals/RemainsVisualStyle.cs) | Общие правила порядка и цвета останков | reviewed_boundary |
| [Runtime/Sandbox/SandboxHealthZone2D.cs](../../Assets/EmergencyShift/Runtime/Sandbox/SandboxHealthZone2D.cs) | Тестовое временное здоровье в тире | reviewed_boundary |
| [Runtime/Sandbox/SandboxSession2D.cs](../../Assets/EmergencyShift/Runtime/Sandbox/SandboxSession2D.cs) | Sandbox reset/refill orchestration | partial |
| [Runtime/Sandbox/SandboxStationaryStalker2D.cs](../../Assets/EmergencyShift/Runtime/Sandbox/SandboxStationaryStalker2D.cs) | Адаптер неподвижной настоящей мишени | reviewed_boundary |
| [Runtime/UI/GameplayHudView.cs](../../Assets/EmergencyShift/Runtime/UI/GameplayHudView.cs) | Canvas gameplay View и подписки | partial |
| [Runtime/UI/PrototypeHud.cs](../../Assets/EmergencyShift/Runtime/UI/PrototypeHud.cs) | Legacy текстовый gameplay HUD | partial |
| [Runtime/World/Doors/DoorAccessFeedback2D.cs](../../Assets/EmergencyShift/Runtime/World/Doors/DoorAccessFeedback2D.cs) | View звука проверки доступа | reviewed_boundary |
| [Runtime/World/Lighting/CeilingLightOcclusion2D.cs](../../Assets/EmergencyShift/Runtime/World/Lighting/CeilingLightOcclusion2D.cs) | Cookie потолочного света по полным преградам | partial |
| [Runtime/World/Lighting/Flashlight2D.cs](../../Assets/EmergencyShift/Runtime/World/Lighting/Flashlight2D.cs) | Фонарь игрока: ownership/input/свет/LOS | partial |
| [Runtime/World/Lighting/FullHeightLightBlocker2D.cs](../../Assets/EmergencyShift/Runtime/World/Lighting/FullHeightLightBlocker2D.cs) | Регистрация и геометрия полной преграды | partial |
| [Runtime/World/Lighting/LocalLamp2D.cs](../../Assets/EmergencyShift/Runtime/World/Lighting/LocalLamp2D.cs) | Личное состояние и запрос освещения лампы | partial |
| [Runtime/World/Lighting/LocalLampAudio2D.cs](../../Assets/EmergencyShift/Runtime/World/Lighting/LocalLampAudio2D.cs) | Playback фонового гула и контактов лампы | reviewed_boundary |
| [Runtime/World/Lighting/LocalLampConfiguration2D.cs](../../Assets/EmergencyShift/Runtime/World/Lighting/LocalLampConfiguration2D.cs) | Настраиваемый authoring потолочной лампы | reviewed_boundary |
| [Runtime/World/Surfaces/GroundAudioSurface2D.cs](../../Assets/EmergencyShift/Runtime/World/Surfaces/GroundAudioSurface2D.cs) | Авторенная область материала пола | reviewed_boundary |
| [Runtime/World/Surfaces/GroundAudioSurfaceQuery2D.cs](../../Assets/EmergencyShift/Runtime/World/Surfaces/GroundAudioSurfaceQuery2D.cs) | Выбор материала из scene registry | reviewed_boundary |
| [Runtime/World/Surfaces/GroundSoundSurface.cs](../../Assets/EmergencyShift/Runtime/World/Surfaces/GroundSoundSurface.cs) | Семейства материала звуковой поверхности | reviewed_boundary |
| [Runtime/World/Surfaces/IGroundAudioRegion.cs](../../Assets/EmergencyShift/Runtime/World/Surfaces/IGroundAudioRegion.cs) | Контракт области звукового материала | reviewed_boundary |
| [Tests/EditMode/BloodPoolTests.cs](../../Assets/EmergencyShift/Tests/EditMode/BloodPoolTests.cs) | Регрессии BloodPool | partial |
| [Tests/EditMode/CeilingLightOcclusionTests.cs](../../Assets/EmergencyShift/Tests/EditMode/CeilingLightOcclusionTests.cs) | Регрессии CeilingLightOcclusion | reviewed_boundary |
| [Tests/EditMode/EnemyDeathAudioTests.cs](../../Assets/EmergencyShift/Tests/EditMode/EnemyDeathAudioTests.cs) | Регрессии EnemyDeathAudio | reviewed_boundary |
| [Tests/EditMode/EnvironmentPresentationTests.cs](../../Assets/EmergencyShift/Tests/EditMode/EnvironmentPresentationTests.cs) | Регрессии EnvironmentPresentation | reviewed_boundary |
| [Tests/EditMode/GoreCompositionTests.cs](../../Assets/EmergencyShift/Tests/EditMode/GoreCompositionTests.cs) | Регрессии GoreComposition | reviewed_boundary |
| [Tests/EditMode/GroundAudioSurfaceQueryTests.cs](../../Assets/EmergencyShift/Tests/EditMode/GroundAudioSurfaceQueryTests.cs) | Регрессии GroundAudioSurfaceQuery | reviewed_boundary |
| [Tests/EditMode/HealthModelTests.cs](../../Assets/EmergencyShift/Tests/EditMode/HealthModelTests.cs) | Регрессии HealthModel | reviewed_boundary |
| [Tests/EditMode/LocalLampAudioTests.cs](../../Assets/EmergencyShift/Tests/EditMode/LocalLampAudioTests.cs) | Регрессии LocalLampAudio | reviewed_boundary |
| [Tests/EditMode/LocalLampSceneSettingsTests.cs](../../Assets/EmergencyShift/Tests/EditMode/LocalLampSceneSettingsTests.cs) | Регрессии LocalLampSceneSettings | reviewed_boundary |
| [Tests/EditMode/SceneRegistryTests.cs](../../Assets/EmergencyShift/Tests/EditMode/SceneRegistryTests.cs) | Регрессии SceneRegistry | reviewed_boundary |
| [Tests/EditMode/WorkBudgetTests.cs](../../Assets/EmergencyShift/Tests/EditMode/WorkBudgetTests.cs) | Регрессии WorkBudget | reviewed_boundary |
| [Concepts/Unused/Visual/SleeperSpriteRig01/AuthoringDrafts/SleeperJawBakeSource.cs](../../Concepts/Unused/Visual/SleeperSpriteRig01/AuthoringDrafts/SleeperJawBakeSource.cs) | Исторический offline источник челюсти из native portraits | reviewed_boundary |
| [Concepts/Unused/Visual/SleeperSpriteRig01/AuthoringDrafts/SleeperSpriteBaker.cs](../../Concepts/Unused/Visual/SleeperSpriteRig01/AuthoringDrafts/SleeperSpriteBaker.cs) | Исторический bake шести клипов и numeric QA | reviewed_boundary |
| [Concepts/Unused/Visual/SleeperSpriteRig01/PausedAttempt/draft-SleeperJawBakeSource.cs](../../Concepts/Unused/Visual/SleeperSpriteRig01/PausedAttempt/draft-SleeperJawBakeSource.cs) | Архивный native-art jaw baker / идентичная копия | reviewed_boundary |
| [Concepts/Unused/Visual/SleeperSpriteRig01/PausedAttempt/draft-SleeperSpriteBaker.cs](../../Concepts/Unused/Visual/SleeperSpriteRig01/PausedAttempt/draft-SleeperSpriteBaker.cs) | Архивный bake/экспорт кадров с расширенной QA | reviewed_boundary |
| [Concepts/Unused/Visual/SleeperSpriteRig01/PausedAttempt/SleeperJawBakeSource.cs](../../Concepts/Unused/Visual/SleeperSpriteRig01/PausedAttempt/SleeperJawBakeSource.cs) | Архивный native-art jaw baker / идентичная копия | reviewed_boundary |
| [Concepts/Unused/Visual/SleeperSpriteRig01/PausedAttempt/SleeperSpriteBaker.cs](../../Concepts/Unused/Visual/SleeperSpriteRig01/PausedAttempt/SleeperSpriteBaker.cs) | Архивный bake/экспорт кадров с расширенной QA | reviewed_boundary |
| [Concepts/Used/Environment/CardDoorIndicators01/create.cs](../../Concepts/Used/Environment/CardDoorIndicators01/create.cs) | Исторический authoring индикаторов карточных дверей | partial |
| [Concepts/Used/Environment/RoomFinish01/create.cs](../../Concepts/Used/Environment/RoomFinish01/create.cs) | Исторический authoring плинтусов/влаги/слива | partial |
