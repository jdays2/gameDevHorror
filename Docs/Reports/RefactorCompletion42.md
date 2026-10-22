# Завершение технических границ SOD

Срез от 2026-10-22 после PlayerLife41. [Manifest](../Validation/RefactorCompletion42.json) фиксирует исходные SHA, metadata, результаты и исключения. Финальная проверка чистого коммита и обычной Windows-сборки записывается отдельно; этот отчёт сам по себе её не утверждает.

## Правила и владельцы

- Игровые HP, доступ, предметы/фонарь, оружие/полёт, обе FSM, melee/patrol/path, encounter, run/objectives/checkpoint и audio mood/mix/heard deadlines имеют личные pure модели, команды, controller/factories и scoped DI. Предыдущие проверенные этапы — в [индексе](ValidationIndex.md).
- Завершён оставшийся tuning-pass: DoorRulesProfile и FixtureFlicker обязательны в world composition, проекции проверяются до любых bindings. Длительность двери и прежняя fixture-кривая сохраняются. Отдельный LocalLightProfile продолжает использовать свою кривую.
- Четыре существующих аудионабора владеют playback levels/cadence/ranges/variation; Stalker source envelope отделён от outer threat envelope. Старые authored differences имеют приоритет, флаг Use Authored сохраняет даже прежний default. Громкости/клипы принятых шагов, ручные сцены и порядок cosmetic random сохранены.
- Casing ejection — данные существующего ShotVisualProfile; ограниченные native коллекции используют bounded cursor. PrototypeHud собирает health/ammo текст при изменении чисел/фазы, не каждый кадр. Основной HUD сохраняет событийные подписки и существующий Canvas.
- Unity physics/Transform, геометрия света, AudioSource/DSP, ввод, native сцены/диск и renderer timelines остаются адаптерами/представлением. Им не добавлены пустые Model, context bags или фиктивные команды для каждого арифметического шага.

## Lifetimes и подтверждённые дефекты

- BUG-038: historical checkpoint probe отвергает текущую сцену до slot/player mutation и выдаёт понятный отказ. Удалённый FirstShift не восстанавливался; прежний отчёт8 volumes/44 IDs не является текущим прогоном.
- BUG-039: FurnitureLightStudy возвращает реальные материалы, MaterialPropertyBlock и изменённые light flags при disable/destroy. Повторный enable захватывает новый исходный снимок. Benchmark заменяет образцы через явный метод, без reflection в private cache; размер native snapshots согласован.
- BUG-040: measurement settings имеют native lease; прерывание закрывает recorders/работу и возвращает runInBackground/vSync/frame rate. Временные input devices/fixture/test profile/timeScale принадлежат probes и освобождаются. Отдельные замеры производительности не повторялись, цифры FPS не заявляются.
- BUG-041: persistence-authoring выполняет stable Edit/scene/run/player/definition preflight до каталогов/ID/компонентов. Sandbox и retired definitions явно исключены. Save payload/saveRevision не менялись.

## Сверка оставшихся файлов

Проверены все оставшиеся partial-границы, новые классы и затронутые consumers; актуальные SHA и конкретная область каждого вывода — [CodeAudit](CodeAudit.json). Общая transaction для once-only Editor recipe не является требованием runtime MVC. Генераторы сохранённых карт не запускались. AgentScripts/исторические Concepts не объявляются production domain code; старый составной drain остаётся отдельным художественным BUG-007.

У PauseMenuPresenter фактически два injected collaborator и authored view/music; прежняя запись о четырёх обязательных domain dependencies исправлена. Quit/fade остаётся native presentation. Незавершённое меню/музыка/сцены не включены в коммит. В его authoring-файле только три assignment chains развёрнуты справа налево с теми же setter/константами: [точный patch](../Validation/PreservedDraftStyle.patch), [hash manifest](../Validation/PreservedDraftStyle.json). Прочие59 исходных незавершённых файлов точны.

Roslyn scan644 own C# проверяет explicit blocks, compound declarations/assignments, nested ternaries и statements on one line. В активном коде нарушений этих правил не осталось; три bare using в некомпилируемых Concepts/Unused provenance snapshots сохранены как историческое исключение. Это дополнение к ручной сверке, не автоматическое доказательство всей архитектуры. Именованные .65/.12 технические интервалы path-cache допустимы по adopted config/readability rule; отдельный balance asset ради них не создавался.

## Проверки

Dirty disposable copy:1248/1248 EditMode(1244 production +3 authoring guards +1 read-only scene audit),71/71 PlayMode. Включены native study disable/re-enable/sample replacement, probe cancellation, конфиги/overrides, текущие AI/оружие/двери/камера/restore/room reload регрессии.

Unity нативно прочитал11 saved scenes и150 prefab:9343 components/66638 serialized references, без missing scripts, unresolved reference handles, disconnected prefab, duplicate/empty persistent ID или сохранённых temporary probes. Проверены и inactive children, и prefab external scene references. Все сцены и существующие prefab сохранены побайтно; в ProjectContext добавлены лишь две обязательные ссылки. PlayMode test assembly получила только ссылку на уже установленный Input System. Это проверка разрешаемых Unity-ссылок/fileID, не оценка всех намеренно optional-null полей или художественная приёмка.

Hierarchy: MechanicsSandbox разделён по corridor/трём тестовым комнатам и preview-зонам; MorgueRouteTest — текущий blockout по geometry/gameplay/dressing responsibility groups; Basement сохраняет legacy test groups; art-review сцены имеют отдельные room/rig. Повторные двери/враги/свет/props остаются prefab instances, position/patrol/local-light/ID overrides не применялись в base. Иерархия не перестраивалась ради формального шаблона. Нового уровня, кампании, height-shadow алгоритма, pooling/ECS или художественного восстановления врагов этот проход не добавляет.

Авторская оценка остаётся отдельной задачей владельца. Техническое завершение не означает, что BUG-006/007 по арту выполнены или что будущих дефектов не будет.
