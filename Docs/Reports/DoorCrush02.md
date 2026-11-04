# Раздавливание в первом уровне и игровой показ · 2026-11-04

В рабочем MorgueRouteTest раздавливание действительно не было подключено: DoorCrush2D находился только у двух CrushingDoor отдельного DoorCrushTest. После сообщения владельца получено прямое решение: **«Всем подъёмным дверям первого уровня»**. Существующая [карточка без кода](https://app.notion.com/p/4018d6dc06b681988121ca6de195b509) продолжается без дубликата/перенумерации.

## Что подключено

Нативным Undo.AddComponent добавлено 11 включённых DoorCrush2D к существующим экземплярам MorgueRouteTest. Базовый Door, CrushingDoor, прочие карты, runtime-правило, общий damage/death/gore/audio и профили не изменены.

| Двери | Экземпляры |
|---|---|
| Обычные | S-C0, A-B, H-ER, G-R, ER-ED |
| Синие | S-W, H-O1, H-O2, H-O3 |
| Зелёные | R-H, H-P |

Поддерживаемая геометрия проверена до добавления: solid XY BoxCollider2D без skew/Edge Radius, прежний локальный openOffset вверх. Сохраняются duration/useAuthoredDuration, collider/leaf, world pose, карточный доступ и экземплярные значения. Это выбранные компоненты уровня; в другой сцене возможность по-прежнему включается отдельно. [Контракт](../Features/Doors.md#раздавливание-монстров), [инструкция уровня](../Guides/MORGUE_ROUTE_TEST.md#раздавливание-дверями).

## Новая проверка в настоящем Play

На каждой из 11 сохранённых дверей уровня создана временная цель сначала ShadowStalker, затем Sleeper. Для изоляции контакта locomotion остановлена; обычные health/body/collider/death/gore/audio сохранены. Слушатель перемещён рядом, чтобы звук не отсекался штатной слышимостью. Использован настоящий Door2D.Toggle и прежний scaled-таймлайн; новое runtime-правило/фиктивный Kill в probe не подменялись.

**22/22:** health=0, Died=1, death sound=1, gore=true, створка завершила закрытие. [Сталкер: 11 дверей](../Validation/DoorCrush02/first-level-shadowstalker.json), [Спящий: 11 дверей](../Validation/DoorCrush02/first-level-sleeper.json). Все временные цели/останки удалены обычным завершением Play; Спящий в авторенную карту не добавлен. В первом уровне остаётся исходный состав Сталкеров.

Затем отдельно записаны **две настоящие погони** в DoorCrushTest: действующие brains дошли в проём, открывание/закрывание шло через Door.Interact с PlayerInteraction. Обоим временно противопоставлен игрок с 100 HP. Спящий пробуждён нефатальным попаданием, затем действовал сам. Дверь закончила ход, оба остались Dead, по одному звуку/gore; останки осели. Спящий успел укусить до разрыва (99 HP), Сталкер — нет (100 HP). Это контролируемый показ, не авторская приёмка баланса.

| Вид | Погоня и контакт | До | Контакт | После |
|---|---|---|---|---|
| Сталкер | [5.66 s](../Validation/DoorCrush02/stalker-live.webp) | [Кадр](../Validation/DoorCrush02/stalker-before.jpg) | [Кадр](../Validation/DoorCrush02/stalker-contact.jpg) | [Кадр](../Validation/DoorCrush02/stalker-after.jpg) |
| Спящий | [6.36 s](../Validation/DoorCrush02/sleeper-live.webp) | [Кадр](../Validation/DoorCrush02/sleeper-before.jpg) | [Кадр](../Validation/DoorCrush02/sleeper-contact.jpg) | [Кадр](../Validation/DoorCrush02/sleeper-after.jpg) |

[Метрики Сталкера](../Validation/DoorCrush02/stalker-live.json), [метрики Спящего](../Validation/DoorCrush02/sleeper-live.json). Кадры взяты самим Unity ScreenCapture из GameView 1280×720. Animated WebP уменьшен до 960 px без обрезки/перерисовки, timing основан на измеренном времени кадров; последний кадр удерживается для осмотра. Превью не содержит звук; его однократность измерена в живом audio View. Сырые PNG и данные кадров — `work/door-crush02`, экспорт — `AgentScripts/EncodeDoorCrush02.py`. Старые показанные во время ожидания фрагменты от 2 ноября не выдаются за новую проверку.

Свежий профильный [EditMode Door](../Validation/DoorCrush02/editmode.json) прошёл **78/78**. Дополнительный framework PlayMode-прогон был отменён **до выполнения**: Unity показал save-scene modal после Play. Его результат не заявляется зелёным. Сравнение 3718 объектов памяти/сохранённой сцены не нашло missing/extra и выявило только 13 известных производных значений: выключение лампы/рассеивателя при Stop и anchors/размеры driven UI layout. Диалог отменён, посторонние значения не сохранялись; после проверки восстановлен текущий сохранённый уровень. [Ограничение прогона](../Validation/DoorCrush02/testrunner-playmode.json). Старые 9 PlayMode результатов DoorCrush01 относятся к прежней ревизии.

## Сохранность и итоговый контроль авторинга

База окна — `a05f2ab7`, включая чужой незавершённый dirty/untracked. Проверена фактическая сцена, включая неактивные объекты: 11 существующих дверных сборок остались у прежних room/corridor Gameplay, prefab-связи/замки/геометрия сохранены; missing scripts=0. Новых иерархических объектов, перемещений или переименований нет. При добавлении все **2706 прежних компонентов** сохранили сериализованные значения.

Строгая YAML-сверка разрешает только 11 списков added components, 11 новых DoorCrush2D и 5 материализованных Unity ссылок на inherited root (stripped GameObject и m_GameObject у старого Door2D). Удалённых блоков нет. **3829/3830** защищённых файлов Assets/Packages/ProjectSettings совпали побайтно; единственное изменённое содержимое — целевой MorgueRouteTest. В сверку входят также ранее неотслеживаемые импортированные файлы. [Authoring gate](../Validation/DoorCrush02/first-level-authoring.json), [сверка](../Validation/DoorCrush02/preservation.json).

Нормальная Windows64 со сценой MorgueRouteTest: **Succeeded, 0 errors, 1 ожидаемое Pipeline warning**, 52.108 s. Служебный Pipeline выключен в Player; из собственных assembly только EmergencyShift.Runtime.dll. Собственный Player прошёл startup без исключений и остановлен. [Build](../Validation/DoorCrush02/build.json), [startup](../Validation/DoorCrush02/player-startup.json). Сборка текущей общей рабочей копии — `Builds/DoorCrush02/EmergencyShift.exe`; чужие изменения этой копии в наш коммит не включаются.

На диске сохраняются прежние пользовательские правки сцены; в index попадает только отдельный нативный component patch. Исходный Editor setup восстановлен: одна чистая MechanicsSandbox, Play stopped, entry=null. Настройка GameView после записи возвращена; собственные временные процессы/операции закончены. MainMenu, каталог и Build Settings не менялись. Художественная оценка момента разрыва/останков и удобства ловушки остаётся владельцу.
