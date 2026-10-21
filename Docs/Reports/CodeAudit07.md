# Пофайловый аудит SOD · оружие и боезапас · 07

Дата: 2026-10-21. База Git: `94616a6`; проверено текущее рабочее дерево, включая ранее незавершённые соседние изменения. Исправлений нет: игровой C#, тесты, сцены/prefab, SO, графика, звук, GUID и ручные значения не менялись. Это свидетельство чтения/анализа, не выполненная миграция и не новый релиз.

## Покрытие

Полностью прочитаны **33 C# / 3944 строки**: 5 combat-файлов, 9 definition/audio/visual profiles, 6 player-адаптеров (включая FlashlightUpgrades для пути восстановления), 3 визуальных адаптера, ScenePrefabFactory, WeaponLootPickup, 2 checkpoint-адаптера, HUD-строка, sandbox ammo supply, impact material adapter, editor authoring и 2 тестовых файла. По каждому в [CodeAudit.json](CodeAudit.json) сохранены ответственность, зависимости, использование, замечания, дата, хеш и история предыдущих заключений.

Выборка: **12 reviewed_boundary / 21 partial**. Из очереди выведены 28 файлов; перечитаны ещё 4 partial и 1 ранее проверенная граница. Весь реестр: **269 C#, 101 reviewed_boundary / 66 partial / 102 queued**, `complete=false`. Полностью прочитанный исходник с зафиксированным долгом остаётся partial; reviewed_boundary подтверждает только описанную границу, не готовность фичи/сцены.

Части PlayerInteraction/LevelCheckpointController2D и поисковые результаты смежных тестов прочитаны как контекст вызовов; полный аудит этих файлов не заявляется. Оставшиеся системы подбора/освещения/сохранения, полный HUD и development probes ещё требуют самостоятельного прохода.

## Основные выводы SOD

- **Циклы и инвентарь:** личные ammo/loaded/reserve/phase уже отделены от ввода и звука. Замена магазина атомарна, поштучная зарядка не создаёт патроны, Restore инвентаря сначала валидирует снимок. Но модели держат Unity SO и имеют внешние public mutations вместо принятых команд/Result. Явный аргумент `now` — положительная граница времени, его не следует путать с прямым чтением Time в Model. Цепочка WeaponCycle → MagazineWeaponCycle → PumpShotgunCycle имеет два перехода наследования и сама по себе разрешена SOD.
- **PlayerWeapon2D:** 445 строк совмещают input/controller, pose/physics, inventory, выдачу урона через projectile, VFX и событийный мост. Cycle operation binding снимается при смене/уничтожении; autoreload общий, без проверки конкретного вида оружия. Нужны Model/controller/environment/presentation-границы, а не механическое дробление файла или context bag.
- **Баллистика и конфиги:** WeaponDefinition/Magazine/Pump имеют хорошие finite/range проверки. ProjectileVisualProfile также валидирует числа, но его bulletSpeed реально определяет полёт — gameplay tuning смешан с оформлением. ShotVisualProfile не проверяет целостность вложенного projectile до расхода ammo; ошибки authoring могут проявиться поздно в Launch. WeaponCatalog.Find не заменяет полную проверку уникальных IDs/slots всего каталога. Эти ограничения не объявлены поломанными текущими assets.
- **Unity projectile/VFX:** BulletProjectile2D одновременно считает полёт/коллизии, доставляет damage и рисует trail/light. Общий Damageable2D сохраняет независимость от видов врагов. Raw Instantiate projectile/muzzle/impact пока не использует явное scene ownership как ScenePrefabFactory; нужен отдельный additive-сценарий, текущая ошибка выгрузки не заявляется воспроизведённой. 32-hit buffer и lifetime 3 с — текущие ограничения, не доказательство масштабируемости.
- **Жизненные циклы/ресурсы:** у audio/casing владельцев подписки парные. Существующий casing pool ограничен и явно принадлежит сцене; его расширение не выполнялось. PlayerAudio2D ограничивает память impact-volley до 64, но объединяет несколько playback-обязанностей и tuning. ShotEffect2D имеет static mutable registry, Play выделяет массивы/MPB. Стоимость не измерялась, оптимизация/новый пул не начинались. Cosmetic System.Random аудио отделён от Unity Random разброса; он не причина BUG-010.
- **View и авторинг:** HUD не проверяет класс оружия, cache ammo/phase уменьшает лишние текстовые обновления. Material switch в ImpactSurface2D/аудиоданных — выбор данных, не видовой gameplay-dispatch. PlayerAim — Unity input/camera adapter; Camera.main используется как разовый Awake fallback. PlayerShotShadow читался без изменений и не возвращает отклонённые новые тени окружения. ShotgunIntegrationAuthoring работает с существующим тиром по явной команде; не запускался.

## BUG-009: nested payload модулей не проверяется до восстановления игрока

[Карточка](https://app.notion.com/p/4008d6dc06b6814287c5e10e2b136c0f), «Нужно уточнить». В корректный PlayerCheckpoint format3 подставить два modules с одинаковым id и конечными bonus, moduleCount=2. PlayerCheckpoint2D.ValidateCheckpoint проверяет inventory/ownership, но не modules. RestoreCheckpoint уже меняет здоровье, карты, оружие, затем FlashlightUpgrades.Restore очищает installed и вызывает Dictionary.Add для каждой записи. Дубль вызывает исключение после частичной мутации.

```json
{"modules":[{"id":"lens","bonus":{"x":0.1,"y":0}},{"id":"lens","bonus":{"x":0.1,"y":0}}],"moduleCount":2}
```

Это фрагмент для изменения полного валидного snapshot, не самодостаточный save. Также нужны проверки null/ID/finite bonus и корректная политика счётчика повторяемых модулей: число установок не обязано равняться количеству уникальных ID. LevelCheckpointController.Apply действительно рассчитывает на полную Validate до PrepareRestore всех объектов. При исправлении нужен настоящий JsonUtility/EditMode-контрпример и отказ Apply без частичного изменения мира; поддержка player format1/2/3 и повторяемых линз должна сохраниться. Исправление/изменение save-схемы не выполнялось.

## BUG-010: gameplay-разброс и cosmetic draws делят RNG

[Карточка](https://app.notion.com/p/4008d6dc06b6817984b8ffd452fd345b), «Нужно уточнить». PumpShotgunDefinition.ProjectileDirection использует глобальный UnityEngine.Random.value. В PlayerWeapon.TryFire до веера выполняется ShotEffect.Play, а между направлениями дробин — BulletProjectile.Launch; оба потребляют тот же Unity RNG для косметики. Например, наличие plume добавляет cosmetic seed draw. Поэтому дополнительное потребление в оформлении сдвигает последовательность авторитетных направлений при том же исходном gameplay state.

Нужен отдельный per-instance/shot gameplay RNG и отдельная косметическая последовательность без изменения текущего распределения/баланса. Изолированный тест PelletDirectionsStayInSpread проверяет диапазон и отсутствие упорядоченного веера, но не interleaving с VFX. При исправлении сравнить направления полного залпа при фиксированном gameplay seed и разных cosmetic draws/configs, затем изоляцию двух оружий. Реальное Unity-сравнение ещё не выполнялось; replay/save-детерминизм не обещается одним выделением RNG.

## Проверки и границы

Прочитаны **16 существующих тестов**: MagazineWeaponTests — 6, WeaponCycleTests — 10. Проверены источники сценариев ammo conservation, reload/cancel/pump/смены, legacy migration, malformed snapshots и instance isolation. Это не новый успешный прогон. Новые тесты не добавлялись.

Проверены полнота 33 записей, актуальные SHA-256, согласованность счётчиков и ссылки отчёта. Механический audit_code.py сверяет 269 путей/хешей и не присваивает semantic verdict автоматически. YAML reference graph/лексические usages остаются подсказками, не свежей AssetDatabase/local-fileID/unused-code сертификацией.

`unity command editor_status` с явным project-path снова сообщил отсутствие Pipeline instance. Editor не запускался; EditMode/Play/build, аудиоприёмка, FX/физические/additive-проверки и Profiler не выполнялись. Общий SOD-аудит и миграция открыты; закрывается только данная выборка исходников. Оба новых бага остаются неподтверждёнными настоящим runtime-тестом, исправления/авторское одобрение не отмечались выполненными. Рабочие статусы и ченджлог — в [Notion](https://app.notion.com/p/3ff8d6dc06b6819fbbc3c56bd63e6b5e).

## Указатель файлов

Полные зависимости/использование/выводы по каждому файлу — в CodeAudit.json.

| Файл | Ответственность | Результат |
|---|---|---|
| [Runtime/Combat/Weapons/WeaponCycle.cs](../../Assets/EmergencyShift/Runtime/Combat/Weapons/WeaponCycle.cs) | Боезапас и cooldown одного оружия, DTO фаз/операций | partial |
| [Runtime/Combat/Weapons/MagazineWeaponCycle.cs](../../Assets/EmergencyShift/Runtime/Combat/Weapons/MagazineWeaponCycle.cs) | Заряженный магазин и атомарная замена | partial |
| [Runtime/Combat/Weapons/PumpShotgunCycle.cs](../../Assets/EmergencyShift/Runtime/Combat/Weapons/PumpShotgunCycle.cs) | Помпа и прерываемая поштучная зарядка | partial |
| [Runtime/Combat/Weapons/WeaponInventory.cs](../../Assets/EmergencyShift/Runtime/Combat/Weapons/WeaponInventory.cs) | Владение, активное оружие, подбор/смена и snapshot | partial |
| [Runtime/Combat/Weapons/BulletProjectile2D.cs](../../Assets/EmergencyShift/Runtime/Combat/Weapons/BulletProjectile2D.cs) | Полёт, segment hit, урон и визуализация projectile | partial |
| [Runtime/Definitions/WeaponDefinition.cs](../../Assets/EmergencyShift/Runtime/Definitions/WeaponDefinition.cs) | Общий баланс, идентичность и factory оружия | partial |
| [Runtime/Definitions/MagazineWeaponDefinition.cs](../../Assets/EmergencyShift/Runtime/Definitions/MagazineWeaponDefinition.cs) | Баланс сменного магазина и создание cycle | partial |
| [Runtime/Definitions/PumpShotgunDefinition.cs](../../Assets/EmergencyShift/Runtime/Definitions/PumpShotgunDefinition.cs) | Баланс помпы и авторитетный веер дробин | partial |
| [Runtime/Definitions/WeaponCatalog.cs](../../Assets/EmergencyShift/Runtime/Definitions/WeaponCatalog.cs) | Разрешение стабильного contentId в definition | partial |
| [Runtime/Definitions/WeaponAudioProfile.cs](../../Assets/EmergencyShift/Runtime/Definitions/WeaponAudioProfile.cs) | Общие weapon clips, gain и внешний вид гильзы | reviewed_boundary |
| [Runtime/Definitions/ProjectileVisualProfile.cs](../../Assets/EmergencyShift/Runtime/Definitions/ProjectileVisualProfile.cs) | Общее оформление projectile и текущая скорость полёта | partial |
| [Runtime/Definitions/ShotVisualProfile.cs](../../Assets/EmergencyShift/Runtime/Definitions/ShotVisualProfile.cs) | Оформление вспышки/impact и ссылка на projectile profile | partial |
| [Runtime/Definitions/PlayerAudioSet.cs](../../Assets/EmergencyShift/Runtime/Definitions/PlayerAudioSet.cs) | Общие выборки pickup/shot/impact/casing по данным | reviewed_boundary |
| [Runtime/Definitions/PlayerWorldAudioSet.cs](../../Assets/EmergencyShift/Runtime/Definitions/PlayerWorldAudioSet.cs) | Общие world foley/switch/empty clips и gain | reviewed_boundary |
| [Runtime/Player/PlayerWeapon2D.cs](../../Assets/EmergencyShift/Runtime/Player/PlayerWeapon2D.cs) | Unity-мост input, inventory, прицеливания и выдачи выстрела | partial |
| [Runtime/Player/PlayerCasingEjector2D.cs](../../Assets/EmergencyShift/Runtime/Player/PlayerCasingEjector2D.cs) | Один spent casing по событию и bounded collection | partial |
| [Runtime/Player/PlayerAim2D.cs](../../Assets/EmergencyShift/Runtime/Player/PlayerAim2D.cs) | Проекция pointer/scripted target на XY | reviewed_boundary |
| [Runtime/Player/PlayerAudio2D.cs](../../Assets/EmergencyShift/Runtime/Player/PlayerAudio2D.cs) | Ограниченный playback pickup/shot/mechanism/impact/casing | partial |
| [Runtime/Player/PlayerUtilityAudio2D.cs](../../Assets/EmergencyShift/Runtime/Player/PlayerUtilityAudio2D.cs) | Звук фонаря/пустого спуска в одном канале | reviewed_boundary |
| [Runtime/Player/FlashlightUpgrades.cs](../../Assets/EmergencyShift/Runtime/Player/FlashlightUpgrades.cs) | Личные установленные модули, лимиты и пересчёт оптики | partial |
| [Runtime/Presentation/Visuals/ShellCasing2D.cs](../../Assets/EmergencyShift/Runtime/Presentation/Visuals/ShellCasing2D.cs) | Косметическая XY/virtual-height физика, контакт и вид | partial |
| [Runtime/Presentation/Visuals/ShotEffect2D.cs](../../Assets/EmergencyShift/Runtime/Presentation/Visuals/ShotEffect2D.cs) | Вспышка/плюм/impact, локальный свет и запрос видимости | partial |
| [Runtime/Presentation/Visuals/PlayerShotShadow2D.cs](../../Assets/EmergencyShift/Runtime/Presentation/Visuals/PlayerShotShadow2D.cs) | Косметическая проекция игрока от света дула | partial |
| [Runtime/Core/Composition/ScenePrefabFactory.cs](../../Assets/EmergencyShift/Runtime/Core/Composition/ScenePrefabFactory.cs) | Создание экземпляра в своём scope до Awake | reviewed_boundary |
| [Runtime/World/Loot/WeaponLootPickup.cs](../../Assets/EmergencyShift/Runtime/World/Loot/WeaponLootPickup.cs) | Авто-подбор оружия/боезапаса и остаток коробки | partial |
| [Runtime/Persistence/PlayerCheckpoint2D.cs](../../Assets/EmergencyShift/Runtime/Persistence/PlayerCheckpoint2D.cs) | Player DTO format1/2/3, Validate и восстановление | partial |
| [Runtime/Persistence/WeaponLootCheckpoint2D.cs](../../Assets/EmergencyShift/Runtime/Persistence/WeaponLootCheckpoint2D.cs) | Checkpoint остатка/collected оружейного лута | partial |
| [Runtime/UI/HudWeaponRow.cs](../../Assets/EmergencyShift/Runtime/UI/HudWeaponRow.cs) | Выбранная/запасная строка и ammo/phase presentation | reviewed_boundary |
| [Runtime/Sandbox/SandboxAmmoSupply2D.cs](../../Assets/EmergencyShift/Runtime/Sandbox/SandboxAmmoSupply2D.cs) | Повторяемое пополнение активного резерва в тире | reviewed_boundary |
| [Runtime/World/Surfaces/ImpactSurface2D.cs](../../Assets/EmergencyShift/Runtime/World/Surfaces/ImpactSurface2D.cs) | Материал попадания по collider/parent | reviewed_boundary |
| [Editor/Authoring/ShotgunIntegrationAuthoring.cs](../../Assets/EmergencyShift/Editor/Authoring/ShotgunIntegrationAuthoring.cs) | Установка готового дробовика/патронов в существующий тир | reviewed_boundary |
| [Tests/EditMode/MagazineWeaponTests.cs](../../Assets/EmergencyShift/Tests/EditMode/MagazineWeaponTests.cs) | Регрессии магазина, отмены и legacy reload snapshot | reviewed_boundary |
| [Tests/EditMode/WeaponCycleTests.cs](../../Assets/EmergencyShift/Tests/EditMode/WeaponCycleTests.cs) | Регрессии помпы, инвентаря, snapshots и диапазона веера | reviewed_boundary |
