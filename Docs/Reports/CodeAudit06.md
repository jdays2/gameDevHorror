# Пофайловый аудит SOD · враги · 06

Дата: 2026-10-21. База Git: `c86a235`; проверено текущее рабочее дерево. Это завершение чтения выбранной группы исходников, а не завершение полной миграции врагов. Исправления не выполнялись: игровой C#, сцены, prefab, SO, графика, звук, GUID и ручные параметры сохранены.

## Объём и результат

Полностью прочитаны **33 C# / 3918 строк**: 9 общих модулей врагов, 7 профилей, 9 видовых brain/audio/presentation-файлов, 2 checkpoint-адаптера, PersistentEntity2D, общий presentation-контракт, AudioThreatEmitter2D и 3 тестовых файла. Ответственность, зависимости, конкретное использование, замечания и SHA-256 каждого файла записаны в [CodeAudit.json](CodeAudit.json). Ранее существовавшие заключения для повторно проверенных файлов сохранены в `reviewHistory`.

Выборка: **19 reviewed_boundary / 14 partial**. Полное чтение с оставшимся архитектурным долгом получает partial; reviewed_boundary подтверждает только описанную границу исходников, а не всю фичу/сцену. Из очереди выведены 22 файла, ещё 11 ранее частично или гранично проверенных перечитаны. Общий инвентарь по-прежнему 269 C#: **90 reviewed_boundary / 49 partial / 130 queued**, `complete=false`.

Общий CreatureSpriteAnimator2D, GoreBurst2D, development probes и весь checkpoint-оркестратор этим проходом целиком не проверены. Для установления вызова валидации прочитаны связанные участки LevelCheckpointController2D.Apply и HealthModel; они не добавлены в счётчик полностью прочитанных файлов.

## Замечания SOD

1. **Доменные решения и Unity.** ShadowStalker2D (678 строк) и Sleeper2D (337) совмещают FSM, Time, физическую запись, компоненты представления и снимки. EnemyMeleeAttack2D хранит правила/таймеры атаки в MonoBehaviour, EnemyPatrol2D смешивает выбор маршрута с Physics2D. При будущей миграции нужны чистые Model/controller, команды внешних действий и явные environment-границы. Это замечание, не выполненный перенос.
2. **Время и случайность.** Поиск пути уже получает clock, budget и индивидуальный RNG; HP вынесено в HealthModel/команды. Но патруль выбирает точки/паузы через глобальный UnityEngine.Random, а brain/атака читают Time напрямую. Косметический Random/Time в аудио не приравнивается к авторитетной случайности AI.
3. **Оставшееся глобальное состояние.** AudioThreatEmitter2D содержит static mutable List. OnDisable и SubsystemRegistration очищают его, consumer фильтрует сцену/target, поэтому текущая утечка между сценами не заявляется воспроизведённой. Целевой scoped ownership здесь ещё не выполнен.
4. **Конфиги.** HP/aggro/таймеры не пишутся в общие SO. При этом EnemyDefinition/Patrol/StalkerBehaviour не имеют полной finite/cross-field валидации; SleeperBehaviourProfile.IsValid пропускает положительную Infinity. Min/Range Inspector не являются границей проверки загрузки. Часть общих аудионастроек Сталкера остаётся в большом компоненте StalkerAudio2D, который объединяет playback, микс, квоты голосов и отслеживание падения частей. Старые экземплярные значения при будущем переносе необходимо сохранить.
5. **Неподтверждённые integration-risks.** EnemyMotor2D.ClearDirection использует local shape.radius, хотя Radius/Follow учитывают lossyScale. ShadowStalker2D.Configure после Awake обновляет perception, но не motor mask. Это записано как требующее отдельного сценария; столкновения и смена масок в Play не проверялись, новые баги по одному подозрению не объявлены воспроизведёнными.

Видовые PresentationSource/AudioThreat-адаптеры находятся на правильной границе: они преобразуют enum своего brain в общие факты; общий renderer/директор не обязан импортировать видовой state. EnemyPerception2D и физический motor имеют допустимую роль Unity environment-adapter — само наличие MonoBehaviour здесь не является дефектом. Подписки health/melee/audio/gore/частей в прочитанных владельцах имеют парные отключения. Летальный HealthModel.ApplyDamage отправляет Died, а не предварительный Damaged/Wake. Урон укуса рассчитывается один раз по struck/timing, а не разрешается animation event.

## BUG-008: недостаточная проверка состояния Сталкера

[Карточка](https://app.notion.com/p/4008d6dc06b6814aa9d7c1ecd0afb886) — «Нужно уточнить»: причина подтверждена чтением, настоящий JsonUtility/EditMode-прогон ещё требуется.

```json
{"format":1,"initialized":true,"hasRemains":false,"actor":{"health":1,"state":999,"melee":{"struck":false,"lunged":false,"cooldown":0}}}
```

В StalkerCheckpoint2D.ValidateCheckpoint нет Enum.IsDefined. При health=1/state=999 существующие условия отказа не срабатывают. PersistentEntity2D действительно вызывает participant.ValidateCheckpoint, а LevelCheckpointController2D.Apply сначала проверяет все записи, затем PrepareRestore/Restore. Пропущенное неизвестное состояние напрямую присваивается ShadowStalker2D.RestoreState и не имеет ветки в Tick switch: живой враг может остаться без поведения. Проверка payload на внешнем уровне не компенсирует отсутствующий guard внутри адаптера.

У SleeperCheckpoint2D уже есть enum/HP/aggro/time и presence-проверки; у Stalker также нужно отдельно сверить остальные времена, верхнюю HP-границу и присутствие вложенного payload. Исправление и изменение save-формата не выполнялись. При исправлении: отвергать контрпример до любой мутации мира; сохранить корректные состояния и поддержку старого eyeBurned; проверить отказ Apply без частичного восстановления.

## Два сообщения владельца

- [BUG-006](https://app.notion.com/p/4008d6dc06b6812d98a1fc3691581245): восстановить индивидуальный дизайн **обоих** типов врагов до общего упрощения. Статус «Новая». Это отдельное требование к Stalker/Sleeper, не откат всей графики проекта. Нужны прежние версии, проверка анимационных частей/связанных состояний и визуальная приёмка владельца.
- [BUG-007](https://app.notion.com/p/4008d6dc06b681b5950cc578367fadb5): заменить составной сливной люк пола морга единым визуальным ассетом. Статус «Новая». Сборка старых деталей под одним корнем сама по себе не решает требование. Сценовая расстановка/соседний пол должны сохраниться.

Обе карточки созданы после проверки дубликатов, с источником «сообщение владельца» и критериями будущей проверки. Их визуальное воспроизведение и исправление этим кодовым аудитом не подменяются.

## Проверка свидетельства

Проверены полнота 33 записей и их хеши, согласованность счётчиков всего реестра, существование новых локальных ссылок и состав отдельного commit. Прочитаны 19 существующих test cases: EnemySystemsTests — 4, SleeperTests — 9, StalkerEyeTests — 6. Это проверка содержания тестов, не новый успешный запуск. Проверки неизвестного enum Сталкера в прочитанных тестах нет; у Спящего такой случай уже существует.

`unity command editor_status` с явным project-path снова вернул отсутствие Pipeline instance. Editor не запускался, новые EditMode/Play/build и замеры производительности не выполнялись. Текущие staged docs проверяются отдельно от прежних незавершённых сценовых/игровых изменений. Статический YAML-граф CodeAudit — подсказка сериализованного использования; не новая AssetDatabase/fileID-проверка и не доказательство неиспользуемого кода.

Закрывается только текущая выборка исходников. Общий аудит/MVC-миграция и все BUG-006/007/008 остаются открыты в [Notion](https://app.notion.com/p/3ff8d6dc06b6819fbbc3c56bd63e6b5e). Исправления и художественное одобрение не отмечались выполненными.

## Указатель файлов

Полные выводы и зависимости по каждому файлу — в CodeAudit.json. Ниже сокращённый указатель.

| Файл | Ответственность | Результат |
|---|---|---|
| [Runtime/Enemies/Common/CrowdAgent2D.cs](../../Assets/EmergencyShift/Runtime/Enemies/Common/CrowdAgent2D.cs) | Локальное разведение движущихся тел | reviewed_boundary |
| [Runtime/Enemies/Common/EnemyChasePath2D.cs](../../Assets/EmergencyShift/Runtime/Enemies/Common/EnemyChasePath2D.cs) | Ограниченный A* и кэш маршрута одного врага | partial |
| [Runtime/Enemies/Common/EnemyChasePathFactory.cs](../../Assets/EmergencyShift/Runtime/Enemies/Common/EnemyChasePathFactory.cs) | Создание индивидуального пути | reviewed_boundary |
| [Runtime/Enemies/Common/EnemyHealth2D.cs](../../Assets/EmergencyShift/Runtime/Enemies/Common/EnemyHealth2D.cs) | Unity-адаптер здоровья и однократной смерти | reviewed_boundary |
| [Runtime/Enemies/Common/EnemyMeleeAttack2D.cs](../../Assets/EmergencyShift/Runtime/Enemies/Common/EnemyMeleeAttack2D.cs) | Таймлайн выпада, геометрия укуса и доставка урона | partial |
| [Runtime/Enemies/Common/EnemyMotor2D.cs](../../Assets/EmergencyShift/Runtime/Enemies/Common/EnemyMotor2D.cs) | Unity-физика, steering и использование пути | partial |
| [Runtime/Enemies/Common/EnemyPatrol2D.cs](../../Assets/EmergencyShift/Runtime/Enemies/Common/EnemyPatrol2D.cs) | Личный мирный маршрут и авторенная область | partial |
| [Runtime/Enemies/Common/EnemyPerception2D.cs](../../Assets/EmergencyShift/Runtime/Enemies/Common/EnemyPerception2D.cs) | Unity-запросы LOS, расстояния и направленного света | reviewed_boundary |
| [Runtime/Enemies/Common/NavigationWorkBudget2D.cs](../../Assets/EmergencyShift/Runtime/Enemies/Common/NavigationWorkBudget2D.cs) | World-scoped квота построения путей | reviewed_boundary |
| [Runtime/Definitions/EnemyDefinition.cs](../../Assets/EmergencyShift/Runtime/Definitions/EnemyDefinition.cs) | Общий баланс HP, преследования и укуса | partial |
| [Runtime/Definitions/EnemyPatrolProfile.cs](../../Assets/EmergencyShift/Runtime/Definitions/EnemyPatrolProfile.cs) | Общий темп мирного патруля | partial |
| [Runtime/Definitions/SleeperBehaviourProfile.cs](../../Assets/EmergencyShift/Runtime/Definitions/SleeperBehaviourProfile.cs) | Дозировка света, пробуждение и recovery | partial |
| [Runtime/Definitions/StalkerBehaviourProfile.cs](../../Assets/EmergencyShift/Runtime/Definitions/StalkerBehaviourProfile.cs) | Реакция на свет, отход и накопление ожога глаза | partial |
| [Runtime/Definitions/SleeperAudioProfile.cs](../../Assets/EmergencyShift/Runtime/Definitions/SleeperAudioProfile.cs) | Общие клипы и микс Спящего | reviewed_boundary |
| [Runtime/Definitions/StalkerAudioSet.cs](../../Assets/EmergencyShift/Runtime/Definitions/StalkerAudioSet.cs) | Банк клипов, cue и лимиты голосов | reviewed_boundary |
| [Runtime/Definitions/StalkerLocomotionProfile.cs](../../Assets/EmergencyShift/Runtime/Definitions/StalkerLocomotionProfile.cs) | Косметический темп походки и сглаживание позы | reviewed_boundary |
| [Runtime/Enemies/Sleeper/Sleeper2D.cs](../../Assets/EmergencyShift/Runtime/Enemies/Sleeper/Sleeper2D.cs) | Видовая FSM сна, пробуждения, преследования и выпада | partial |
| [Runtime/Enemies/Sleeper/SleeperAudio2D.cs](../../Assets/EmergencyShift/Runtime/Enemies/Sleeper/SleeperAudio2D.cs) | Видовая аудиопрезентация сна, реакций и контактов | reviewed_boundary |
| [Runtime/Enemies/Sleeper/SleeperAudioThreat2D.cs](../../Assets/EmergencyShift/Runtime/Enemies/Sleeper/SleeperAudioThreat2D.cs) | Проекция фактов Спящего в аудиоугрозу | reviewed_boundary |
| [Runtime/Enemies/Sleeper/SleeperPresentationSource2D.cs](../../Assets/EmergencyShift/Runtime/Enemies/Sleeper/SleeperPresentationSource2D.cs) | Проекция FSM Спящего в общие визуальные факты | reviewed_boundary |
| [Runtime/Enemies/Stalker/ShadowStalker2D.cs](../../Assets/EmergencyShift/Runtime/Enemies/Stalker/ShadowStalker2D.cs) | Видовая FSM агрессии, света, отхода, укуса и ожога | partial |
| [Runtime/Enemies/Stalker/StalkerAudio2D.cs](../../Assets/EmergencyShift/Runtime/Enemies/Stalker/StalkerAudio2D.cs) | Голоса, контакты, occlusion, сценовые квоты и foley частей | partial |
| [Runtime/Enemies/Stalker/StalkerAudioThreat2D.cs](../../Assets/EmergencyShift/Runtime/Enemies/Stalker/StalkerAudioThreat2D.cs) | Проекция фактов Сталкера в аудиоугрозу | reviewed_boundary |
| [Runtime/Enemies/Stalker/StalkerPresentationSource2D.cs](../../Assets/EmergencyShift/Runtime/Enemies/Stalker/StalkerPresentationSource2D.cs) | Проекция FSM Сталкера в визуальные факты | reviewed_boundary |
| [Runtime/Enemies/Stalker/StalkerSpriteAnimator.cs](../../Assets/EmergencyShift/Runtime/Enemies/Stalker/StalkerSpriteAnimator.cs) | Совместимая оболочка общего renderer | reviewed_boundary |
| [Runtime/Persistence/SleeperCheckpoint2D.cs](../../Assets/EmergencyShift/Runtime/Persistence/SleeperCheckpoint2D.cs) | Сериализация/валидация/восстановление Спящего | partial |
| [Runtime/Persistence/StalkerCheckpoint2D.cs](../../Assets/EmergencyShift/Runtime/Persistence/StalkerCheckpoint2D.cs) | Сериализация/валидация/восстановление Сталкера | partial |
| [Runtime/Persistence/PersistentEntity2D.cs](../../Assets/EmergencyShift/Runtime/Persistence/PersistentEntity2D.cs) | Unity-граница стабильного ID и checkpoint participants | reviewed_boundary |
| [Runtime/Core/Contracts/EnemyPresentationSource2D.cs](../../Assets/EmergencyShift/Runtime/Core/Contracts/EnemyPresentationSource2D.cs) | Общий Unity-контракт визуальных фактов врага | reviewed_boundary |
| [Runtime/Presentation/Audio/AudioThreatEmitter2D.cs](../../Assets/EmergencyShift/Runtime/Presentation/Audio/AudioThreatEmitter2D.cs) | Факты слышимости и учёт источников угроз | partial |
| [Tests/EditMode/EnemySystemsTests.cs](../../Assets/EmergencyShift/Tests/EditMode/EnemySystemsTests.cs) | Проверки общих здоровья, LOS и тесного патруля | reviewed_boundary |
| [Tests/EditMode/SleeperTests.cs](../../Assets/EmergencyShift/Tests/EditMode/SleeperTests.cs) | Проверки checkpoint payload Спящего | reviewed_boundary |
| [Tests/EditMode/StalkerEyeTests.cs](../../Assets/EmergencyShift/Tests/EditMode/StalkerEyeTests.cs) | Проверки checkpoint-диапазона повреждения глаза | reviewed_boundary |
