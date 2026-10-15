# Проверки и границы

Исторические промежуточные JSON удалены из актуальной документации; восстановимы из Git/резервной копии. Здесь только свидетельства текущего состояния.

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

Window → General → Test Runner → EditMode → Run All запускает независимые тесты из Assets/EmergencyShift/Tests/EditMode. FirstLevelProbe/AdaptiveAudioProbe/CrowdReadinessProbe добавляются временно для сценарных проверок, не сохраняются на сцене.

Native smoke работает в инструментированной development-сборке с `--es-smoke-test --es-smoke-output <absolute-json-path>`. Обычный запуск игры не затрагивается. Проверять также release-сборку: редакторская проверка не доказывает исключение служебных сборок из игрока.

Пройденный сценарий не заменяет оценку страха/темпа человеком и профилирование целевого устройства. OPT-01 массовой гибели остаётся открытым.

`CheckpointRegressionProbe` запускается в инструментированном player с `--checkpoint-regression <absolute-report-json>`; временный slot располагается рядом с отчётом и очищается в конце. `EngineeringBenchmark2D`: `--engineering-benchmark <absolute-report-json> <10|20>`. Не запускать performance-прогоны одновременно с другими тестами/сборками. Development probes никогда не сохраняются в сцене.
