# TASK-006 · внедрение D без музыки

2026-11-03, Unity6000.6.5f1. По прямому поручению владельца направление D подключено к пяти игровым сценам. [Контракт](../Features/DeathScreen.md), [источники](../../Concepts/Used/UI/DeathScreenD01/README.md), [TASK-006](https://app.notion.com/p/40a8d6dc06b68181a6b1f162b37c9ab2). Направление выбрано владельцем; runtime-художественная/слуховая приёмка отдельно, статус «На проверке».

## Результат

Отдельный prefab «Death Screen» с Noto Sans Medium, затемнением текущего кадра, левой колонкой, плоскими кнопками и красным штрихом. Новых аудиофайлов, AudioSource и генерации арта нет. Смерть немедленно забирает ввод и закрывает паузу/диалог, после1.4 s затухания останавливает мир. R/кнопка — единый intent с ожиданием отпускания, Single reload и стартовым input fence. Повтор не создаёт checkpoint-контроллеров в картах. Выход подтверждается с «Остаться» по умолчанию; Esc не возобновляет погибшего.

Чистые Model/Controller/command и immutable Settings отделены от Unity-адаптера загрузки/окон. Собственные leases/подписки освобождаются при отмене/уничтожении; чужие захваты не снимаются. Существующий legacy dead-R ограничен свободным GameplayInputBlock. Для отмены выхода собственный InputAction; runtime D не требует незавершённого расширения общего IPlayerInput из чужого черновика паузы.

## Подтверждённые проверки

| Проверка | Результат / свидетельство |
|---|---|
| EditMode всего | 1445/1445, failed/skipped0,5.92 s; [полный JSON](../Validation/DeathScreenD01/editmode-all.json) |
| Целевые правила D | 22/22; [JSON](../Validation/DeathScreenD01/editmode-unit.json); повторены после финального изменения Escape |
| Sleeper PlayMode | 2/2, в том числе PartialWakeTargetDeathFinishesTheBodyMovementWithoutSnappingToWalk; [JSON](../Validation/DeathScreenD01/playmode-sleeper.json) |
| Native input/окна | 8 случаев: смерть из открытой/закрывающейся паузы, отмена pending quit, диалог, внешние аренды, pointer raycast/Enter/Esc, Editor quit rejection, реальный удержанный R через reload и повторная смерть; [JSON](../Validation/DeathScreenD01/native-interactions.json) |
| Настоящие карты | MechanicsSandbox проверен полными interaction/DSP-прогонами; MorgueRouteTest, BasementPrototype, LevelTemplate, DoorCrushTest — смерть → Ready/time0 → Single reload → новый живой игрок/time1; [морг](../Validation/DeathScreenD01/scene-MorgueRouteTest.json), [подвал](../Validation/DeathScreenD01/scene-BasementPrototype.json), [шаблон](../Validation/DeathScreenD01/scene-LevelTemplate.json), [двери](../Validation/DeathScreenD01/scene-DoorCrushTest.json) |
| Native layout | 30/30, пять фактических размеров GameView1280×720/1920×1080/2560×1440/2560×1080/1024×768, текст100/125%, Ready/Confirm/Error. Переполнений/пересечений0; custom sizes7→7, selection6→6; [JSON](../Validation/DeathScreenD01/native-layout.json) |
| Совместный звук BUG-053 | При готовом D/timeScale0 world RMS/peak0/0, UI probe RMS.009251258/peak.013081476. Master1/pausefalse восстановлены, временный probe удалён; [DSP](../Validation/DeathScreenD01/native-dsp.json) |
| Настоящий checkpoint | 7 случаев: полная Single-загрузка сохранённых HP1/позиции[2,0,0], corrupt/missing/incompatible без удаления/подмены, repair/retry, startup-restore error с блокировкой живого default player, восстановление после repair; [JSON](../Validation/DeathScreenD01/native-checkpoint.json) |
| Обычная Win64 | Succeeded,0errors,1 ожидаемое предупреждение Pipeline disabled в обычном Player; [сборка](../Validation/DeathScreenD01/build-release.json) |
| Запуск Player | 12 s headless smoke, процесс работал до остановки только собственного экземпляра, exceptions/errors0; из проектных assemblies только EmergencyShift.Runtime.dll. Это проверка старта, не ручная оценка quit/визуала; [JSON](../Validation/DeathScreenD01/player-startup.json) |

Layout менял реальный viewport через GameView API, а не только масштаб PNG. Текст125% — временная runtime-проба; настройка доступности не добавлена. Кадры с alpha.97 просмотрены: [D720p](../Validation/DeathScreenD01/ready-1280x720-100.png), [текст125%](../Validation/DeathScreenD01/ready-1280x720-125.png), [выход](../Validation/DeathScreenD01/confirmation-1280x720-100.png), [морг в Play](../Validation/DeathScreenD01/morgue-runtime.png). Большой заголовок ошибки startup сокращён до «ОШИБКА», реальная recovery-проверка подтверждает его.

## Авторинг и сохранность

Фактические MechanicsSandbox/MorgueRouteTest/BasementPrototype/LevelTemplate/DoorCrushTest проверены в Unity, включая неактивные объекты. У каждого один новый отдельный корень «Death Screen» с prefab-ссылкой и локальными override HUD/прицела/контекста; world transform0/identity/1. Missing scripts0, по одному context/camera/listener, новых AudioSource/сохранённых runtime mixer-представлений0. Постоянные ID уникальны и не менялись. [Итоговый native review](../Validation/DeathScreenD01/authoring-review.json).

Снимок существовавших компонентов до/после установки совпал (1892/2631/1386/166/692 компонентов). Отдельное сравнение исходной YAML обнаружило автоматическую пересериализацию override трёх ламп MechanicsSandbox. Через PrefabUtility и реальные Light2D возвращены исходные override, enabled/intensity; повторное сравнение старых блоков оставило только ссылку deathScreen и SceneRoots. Профили/поведение ламп не редактировались. Это исправление сохранности, а не переработка освещения.

Checkpoint проверялся в временной копии LevelTemplate с отдельными Definition/Catalog/службами и отсутствующим в legacy-шаблоне participant карточек. Пользовательские карты и слоты не использованы. Временная fixture удалена, исходные13 BuildSettings entries восстановлены, исходные карты fixture не сохраняла; [cleanup](../Validation/DeathScreenD01/checkpoint-fixture-cleanup.json).

Работа выполнена после явной передачи общего writer-слота от задачи двери/менеджера, исходный HEAD eab662cf. Прежние dirty/untracked изменения защищены исходными хешами и резервной копией. В собственный коммит попадают только ресурсы D и его точечные изменения общих файлов; чужие сцены/пауза/ввод/документы остаются отдельным diff. Сборка проверяет общую рабочую копию с уже существовавшими черновиками, не опубликованный релиз.

Итоговое сравнение812 защищённых файлов:15 ожидаемых общих файлов изменены нашей задачей, посторонних изменений0; собственные Markdown-ссылки без разрывов. Изолированный Git index добавляет только UI/ссылку в контексте/SceneRoots; две необходимые stripped-ссылки морга на уже существующие компоненты игрока включены без чужой перестройки. [Сохранность и разрешение ссылок](../Validation/DeathScreenD01/source-preservation.json). Editor оставлен на одной clean MechanicsSandbox, Play stopped, startScene=null, timeScale1, listener1/pausefalse, mixer0dB, fixture/сохранённых death sources0; [final-state](../Validation/DeathScreenD01/final-state.json).

Обычный Play остаётся из открытого уровня, startScene=null; MainMenu не добавлялся и не открывался поверх карты. Иерархии, world transforms, коллизии, сортировка, световые/аудиопрофили и authored маршруты не перестраивались. Полное прохождение, производительность, слуховая естественность fade и художественное одобрение runtime не заявлены. Действительный quit в графическом Player остаётся ручной проверкой владельца.

## Воспроизведение

`AgentScripts/InstallDeathScreenD.cs` создаёт отсутствующие собственные assets и добавляет prefab только в пять явных clean-сцен; существующий prefab не регенерирует. `Review` проверяет фактические сохранённые сцены. Остальные `*DeathScreen*.cs` в AgentScripts — явные Editor-only probes вне Assets, в Player не компилируются. Interaction-проба рассчитана на текущую карту с установленной паузой; checkpoint-fixture имеет marker/проверку абсолютного собственного пути и парный Cleanup. Исходные пользовательские слоты не трогать. Не запускать authoring/fixture поверх dirty-сцен или во время чужого writer-слота.
