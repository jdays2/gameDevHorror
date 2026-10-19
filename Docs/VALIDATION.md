# Проверки проекта

Основная сцена — MechanicsSandbox. Ниже — индекс сохранённых свидетельств, а не результат нового общего прогона. Даты и состав проверенного состояния находятся внутри отчётов. Позднейшие ручные изменения требуют проверки затронутой области; успешная сборка не подтверждает художественное качество, готовность кампании или FPS.

## Актуальные области

| Область | Свидетельства и границы |
|---|---|
| Возврат крови | [BloodRestore01](Validation/BloodRestore01.json): возвращены 25 PNG крови, 25 нативных Sprite и 10 настроек импорта из версии до упрощения. Остальная новая графика сохранена. |
| Упрощённая графика | [SimplifiedArt01](Validation/SimplifiedArt01.json): аудит 143 PNG и 8 нативных текстур, 105 замен на дату прогона (25 изображений крови позднее возвращены), 163 спрайта, 536 сохранённых ссылок; 89/89 EditMode, смерть/останки обоих врагов в двух сценах, Windows64 build без ошибок/предупреждений. Отдельная ошибка индекса Unity Search в проверочной копии записана в отчёте. [Окружение](Validation/SimplifiedArt01/environment.png), [игровой кадр](Validation/SimplifiedArt01/native-living.png). |
| Диалог | [DialogueBranches](Validation/DialogueBranches.json), [DialoguePrefab](Validation/DialoguePrefab.json): две ветки, стабильные ID, ввод, сохранение фона и общий prefab. В проверочной копии было 80/80 EditMode и 37 Play-проверок. [Ответы](Validation/DialogueBranches/choices.png), [реплика](Validation/DialogueBranches/line.png). |
| HUD и карты | [GameplayHud03](Validation/GameplayHud03.json), [AccessCardsFlat02](Validation/AccessCardsFlat02.json): текущие иконки и карточки. [GameplayHud02](Validation/GameplayHud02.json) сохраняет проверки поведения HP/оружия; его иконки заменены. |
| Песочница | [Sandbox](Validation/Sandbox.json), [ящик патронов](Validation/SandboxAmmoSupply.json), [магазины оружия](Validation/WeaponMagazines.json). R начинает чистый тест; checkpoint-контроллеров в песочнице нет. |
| Полы и разметка | [HospitalFloors02](Validation/HospitalFloors02.json), [FloorMarkings01](Validation/FloorMarkings01.json), [SandboxFloorPreview](Validation/SandboxFloorPreview.json). |
| Окружение и тени | [EnvironmentLibraryScene](Validation/EnvironmentLibraryScene.json), [EnvironmentLibraryExpansion](Validation/EnvironmentLibraryExpansion.json), [StorageUtilitiesIndividuality](Validation/StorageUtilitiesIndividuality.json), [силуэтные тени](Validation/EnvironmentSilhouetteShadows.json). Незавершённые новые правки этим не покрыты. |
| Проба света на мебели | [FurnitureLightBenchmark](Validation/FurnitureLightBenchmark.json), [методика и выводы](Environment/FURNITURE_LIGHT_STUDY.md): A/B/C, три предмета и 60 runtime-копий, 6 повторов, фактические 1080p и 1920×1200. Физические высоты и 4K не измерялись. |
| Форма теней мебели | [FurnitureShadowShapes](Validation/FurnitureShadowShapes.json), [описание](Environment/FURNITURE_SHADOW_SHAPES.md): актуальные alpha-контуры 41 предмета, сохранённые pivot/PPU/GUID и prefab/сцены, 28 игровых ракурсов. C выбран владельцем и включается по умолчанию в комнате сравнения. |
| Пустая комната света | [LightingPlayground](Validation/LightingPlayground.json), [работа со сценой](Guides/LIGHTING_PLAYGROUND.md): пустая комната 24×16, игрок/фонарь, четыре стены и два отключённых тестовых источника. Play-проверка без сохранения временной мебели. |
| Морг: маршрут и пробная арена | [MorgueRouteTest](Validation/MorgueRouteTest.json), [управление и баланс](Guides/MORGUE_ROUTE_TEST.md): отдельный блокинг, физическое прохождение без боя, замки/карты/конечный лут, тени закрытой/открытой двери и укрытия, выстрелы/смерть и чистый перезапуск. Полный баланс арены не подтверждён. |
| Полы коридоров и помещений | [FloorTransitions01](Validation/FloorTransitions01.json): проба синего винила и светлой керамики в MorgueRouteTest, общая раскладка, 10 плоских матовых швов, игровые ракурсы с фонарём и проверка сохранения геометрии/механик. |
| Лампы | [HospitalLampSceneConfig03](Validation/HospitalLampSceneConfig03.json), [HospitalLampVisibility04](Validation/HospitalLampVisibility04.json), [HospitalLampAudio01](Validation/HospitalLampAudio01.json), [HospitalLampRuntime01](Validation/HospitalLampRuntime01.json), [корпуса 02](Validation/HospitalLampHousings02.json). |
| Стены | [WallFinishRemoval03](Validation/WallFinishRemoval03.json): отделка снята, геометрия/проёмы сохранены. Кадры отклонённых покрытий удалены. |
| Кровь | [BloodTraces05](Validation/BloodTraces05.json): 16 форм и 21 экземпляр статичного декора. [BloodPools01](Validation/BloodPools01.json), [порядок крови](Validation/BloodFloorOrder01.json): процедурные лужи под тушами. |
| Останки и глаза | [CreatureRemains04](Validation/CreatureRemains04.json), [FleshPalettes05](Validation/FleshPalettes05.json), [EyeSurface05](Validation/EyeSurface05.json), [SleeperBlood02](Validation/SleeperBlood02.json), [SleeperDeadEye03](Validation/SleeperDeadEye03.json). |
| Враги | [SleeperRuntime](Validation/SleeperRuntime.json), [StalkerLocomotion01](Validation/StalkerLocomotion01.json), [StalkerLargeEye01](Validation/StalkerLargeEye01.json). |
| Звук | [CreatureRupture06](Validation/CreatureRupture06.json), [RuptureVolume06](Validation/RuptureVolume06.json); FirearmMix02 закреплён в [acceptance.json](../Concepts/Used/Audio/FirearmMix02/acceptance.json). |

## Как повторить

В Unity: Window → General → Test Runner → EditMode → Run All. Тесты — `Assets/EmergencyShift/Tests/EditMode`.

В инструментированном development-player:

- `--sandbox-regression <absolute-report-json>` — маршрут песочницы.
- `--es-smoke-test --es-smoke-output <absolute-json-path>` — общие контракты.
- `--shotgun-regression <absolute-report-json>` — дробовик и оружейный payload.
- `--engineering-benchmark <absolute-report-json> <10|20>` — отдельный замер; не совмещать с другими тестами/сборками.

Обычный запуск не создаёт probes; в сохранённых сценах и release их быть не должно. Пользовательские checkpoint-слоты не использовать для тестов.

## Сохранённые исторические измерения

[CheckpointRegression](Validation/CheckpointRegression.json) относится к удалённому FirstShift. Старый fixture восстанавливается из Git; этот отчёт не доказывает работу сохранений в MechanicsSandbox. Политика системы — [CHECKPOINTS](Guides/CHECKPOINTS.md).

[CrowdBaseline](Validation/CrowdBaseline.json) и [EngineeringNative](Validation/EngineeringNative.json) сохранены как исходные измерения для [OPT-01](OPTIMIZATION_BACKLOG.md). Editor/native, одиночный прогон и синхронный участок Kill нельзя смешивать с временем полного кадра. Массовый пул остаётся отложенным.

[Аудит контента](CONTENT_AUDIT.md) проверяет зависимости и документацию отдельно от игровой регрессии. Удалённые промежуточные материалы доступны в Git; происхождение сохранено в отдельном реестре.
