# Питание и часы ламп · LampPower30

Этап после `bbe4f24`, 2026-10-22. Перенесены питание и чтение часов; остальные lamp/audio/config/style границы остаются в общем SOD-проходе.

`LampPowerModel` хранит личное питание, получает `IGameClock` и рассчитывает фазовый clock из значений phase/position. `SetLampPowerCommand` валидирует модель и меняет питание до вывода; повторный SetPower по прежнему обновляет базовый вид. `LampPowerController` имеет две зависимости: model и view. Scoped installer/factory подключены в LevelRegistriesInstaller; каждый экземпляр имеет отдельную модель. Rebind сохраняет питание и заменяет clock, не оставляя прежнего tick. Отказ отсутствующей зависимости происходит до снятия регистрации.

LocalLamp2D остаётся Light2D/Transform/Physics2D-адаптером и представлением. Оно применяет прежние формулы мерцания и color/intensity/bulb alpha: scene settings → SO profile → legacy fields. Сохранён точный порядок float-арифметики `time + phase + x*.37 + y*.19`, границы интервалов, разные пороги bulb, full-power Apply до следующего tick и сохранение питания при disable/re-enable. Native powered-поле удалено; Time.time поступает через внедрённый clock. Audio-компонент по прежнему читает фактический свет. Геометрия/алгоритмы теней, профили и звуки не менялись.

Standalone editor/development preview имеет явный fallback с UnityGameClock до scoped injection, чтобы прежний авторинг и одноразовые визуальные probes работали без LevelContext. Он не регистрируется в gameplay illumination query самостоятельно. При инъекции fallback controller заменяется с сохранением питания. Это ограниченное сохранение существующего preview-контракта, не глобальный gameplay service locator. Подробный config-pass legacy SceneSettings с hardcoded flicker остаётся открытым; LocalLamp2D сохраняет partial в реестре до общего config/style-прохода.

## Проверки

**1070/1070 EditMode** (12 новых случаев): command/view order, off без чтения clock, точная фаза/пауза, отказы NaN/Infinity/overflow, изоляция экземпляров, repeated refresh и missing model. Прогретые 1000 Tick+SetPower — **0 managed bytes** в выделенной модельной петле; это не измерение всей лампы/кадра.

**58/58 PlayMode** (3 новых): реальные Light2D/колба для scene/profile/legacy ветвей, dropout/alpha/color/radius, power/disable/re-enable, смена clock и отказ invalid rebind без потери membership. Прежние light scope/shot/frozen unload и остальные native-проверки прошли. Для прямой проверки Light2D в PlayModeTests.asmdef добавлена только тестовая ссылка Unity.RenderPipelines.Universal.2D.Runtime; GUID и runtime assemblies не менялись.

Обычная Windows64 success, собственная Player assembly только EmergencyShift.Runtime. [XML/hash/provenance](../Validation/LampPower30.json). **1805** прежних защищённых ресурсов и все source hashes предыдущего LevelProgress29 неизменны. 9 новых meta импортированы из копии с exclusive create. Сцены/SO/prefab и производственные параметры не менялись. Проверки на dirty изолированной копии с прежней незавершённой паузой, не clean HEAD; пауза не включена в коммит. Имеющийся нефатальный LicensingClient warning остался.

## Стоимость

Для scoped лампы — по model/controller и ссылка на общий clock; временный standalone preview создаёт собственный bootstrap до инъекции. Нового Update, реестра, глобального менеджера и фонового поиска нет. Существующий Update выполняет одну передачу значений/clock/view; выключенная модель не читает часы и не обновляет мерцание. Новые runtime-команды — readonly structs. Новых Physics2D-запросов/аудиоголосов/геометрии нет. FPS и массовая производительность не заявляются.
