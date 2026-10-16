# Проверки и границы

## MechanicsSandbox · 2026-10-16

Полный актуальный отчёт — [Sandbox.json](Validation/Sandbox.json). [Общий вид](Sandbox/Overview.png), [попадания и останки в тире, Play Mode](Sandbox/Range.png).

- 42/42 проверки физического маршрута в самостоятельном инструментированном Windows-player: карта/замок, входы комнат, 100 HP в тире и прежние 2/3 снаружи, настоящие пули, закреплённая цель, независимое HP, однократные останки, T, R при живом игроке и после смерти, обнаружение/страх света/постоянная агрессия обычного Сталкера.
- 13/13 общих native smoke проверок: смена сцены, реестры, один listener, принятые источники аудио игрока, другая цель и другой профиль оружия, стена, однократная активация группы.
- 15/15 EditMode тестов: здоровье, видимость, патруль, сохранение/повреждение/ID/версии и silent restore. Система checkpoint сохранена, но не подключена к песочнице.
- Development и Release: обе Succeeded, 0 ошибок и предупреждений. Настройки managed code восстановлены в Release. В release из проектных сборок есть только EmergencyShift.Runtime.dll; служебные сборки отсутствуют.
- Сохранённая сцена: missing scripts 0, Development probes 0; 14 редактируемых подписей; MechanicsSandbox первая в Build Settings. Финальная консоль: 0 ошибок/предупреждений.
- BasementPrototype побайтово совпадает с резервной копией перед задачей. GUID уникальны; сериализованных ссылок на удалённые FirstShift/LevelDefinition нет. Три project skills синхронизированы. Принятые арт/аудио и происхождение материалов сохранены.

Проверки ввода используют отдельный виртуальный Keyboard внутри Input System, чтобы события редактора/ОС не сбрасывали тестовые нажатия. Проверенный сценарий не является замером FPS или художественным одобрением.

## Запуск

Window → General → Test Runner → EditMode → Run All запускает независимые тесты Assets/EmergencyShift/Tests/EditMode.

В инструментированном development-player: `--sandbox-regression <absolute-report-json>` — маршрут песочницы; `--es-smoke-test --es-smoke-output <absolute-json-path>` — общие контракты. Обычный запуск не создаёт probes. В сценах они не сохраняются, в release не входят. Скриншоты маршрута пишутся рядом с отчётом. Пользовательские checkpoint-слоты не затрагиваются.

Старый CheckpointRegressionProbe рассчитан на удалённый FirstShift и не запускается на песочнице. Для повторения полного checkpoint-сценария потребуется восстановить прежний fixture из Git либо отдельно создать новый уровень для сохранения. EngineeringBenchmark2D запускается через `--engineering-benchmark <absolute-report-json> <10|20>`; performance-прогоны не совмещать с другими тестами/сборками. OPT-01 массовой гибели остаётся открытым.

## Исторические свидетельства до замены FirstShift

Следующие отчёты относятся к предыдущим сценам/миграциям и не заменяют актуальный Sandbox.json:

- [Migration.json](Validation/Migration.json): сравнение31 врага до/после — скорости/HP/тайминги/радиусы, позиции и индивидуальные границы/флаги патруля. Missing scripts0.
- [Checkpoint regression](Validation/CheckpointRegression.json):24 проверки смерти/диска/reload/состояния мира и отсутствия дубликатов в native-player.
- [Сериализованное состояние](Validation/EngineeringMigration.json): старые параметры/геометрия/патрули/свет и GUID сохранены.
- [Native до/после](Validation/EngineeringNative.json):10/20 особей, отдельный Kill dispatch и кадры, без гарантии FPS/ускорения.
- [Первый уровень](FirstLevel/Validation.json):47 проверок реального перемещения, подборов, боя, выхода, перезапуска/закрытых дверей. Основной маршрут без телепортации, god mode и увеличения урона.
- [EditMode](Validation/EditMode.json):15/15 — общие механики, запись/замена/версии/повреждение/ID, silent restore и неактивированные actor.
- [Development-сборка](Validation/DevelopmentBuild.json) и [Release-сборка](Validation/ReleaseBuild.json): обе Succeeded, 0 ошибок/предупреждений.
- [Самостоятельный игрок](Validation/StandaloneSmoke.json):18 проверок, включая смену сцены, реестры, полный звук игрока, другой тип цели и блокировку пули стеной.
- [Состав release](Validation/ReleaseAssemblies.json): из наших сборок присутствует только EmergencyShift.Runtime; Editor/Development/Tests отсутствуют.
- [Контент и skills](Validation/ContentAndSkills.json): сохранность источников, уникальность GUID и проверка трёх skills.
