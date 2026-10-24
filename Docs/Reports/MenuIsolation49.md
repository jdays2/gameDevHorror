# BUG-049 · Откат обязательного входа через меню

Владелец отклонил решение: «найди задачу в которой это было доблено и откати, задача wont do, решение с двумя сценами меня не устраивает. Займемся этим когда придем к сборке билда работоспособного». Последнее решение заменяет прежний обязательный вход через MainMenu. BUG-048 — Won't do; подготовленные ассеты и история TASK-UI-02 сохранены. BUG-049 фиксирует диагностику и выполнение целевого отката.

## Подтверждённый источник

Схему обязательного входа добавил BUG-048, локальный `5fe0ffa`. Его `VerifyUniversalMenu.Finish` открывал MainMenu Additive поверх уже открытой авторской сцены и оставлял её активной. Editor сохранял такой multiscene setup; после Stop и перезапуска он восстанавливался. Это ошибка завершения проверки, а не сохранение UI главного меню внутри карты и не runtime additive-переход.

[Исходный снимок состава](../Validation/MenuIsolation49/edit-before.json): Edit Mode, загружены MorgueRouteTest и MainMenu, обе clean. MainMenuUI enabled=true, Overlay, `Neutral background` RGBA(0.055,0.055,0.055,1), 1280×720; две камеры и два AudioListener. [Scene View до исправления](../Validation/MenuIsolation49/before.png). В сохранённых .unity ссылка на prefab MainMenuUI найдена только в MainMenu.unity. В Play MorgueRouteTest загружался один, MainMenuView отсутствовал, камера/AudioListener по одному. Старый несохранённый в Git Pause Canvas принадлежит карте, disabled и не был источником перекрытия.

## Целевой откат

- Удалены MainMenuEntryPolicy с InitializeOnLoad/Play callbacks и исключением Test Runner, MainMenuSceneRegistry с автокаталогизацией/перезаписью Build Settings, VerifyUniversalMenu с ошибочным Finish. Удалены проверки отменённой политики, ссылка Editor assembly на UnityEditor.TestRunner.
- SandboxSceneBuilder возвращён к собственному прежнему авторингу Build Settings без синхронизации меню. MainMenuAuthoring больше не назначает playModeStartScene при создании/изменении стиля. Menu item принудительной entry удалён.
- ReviewWeaponFx, PickupGlintReview, PickupGlintTiming возвращены к версии до `5fe0ffa`; они больше не требуют проходить меню.
- Обычный Play снова из открытой карты: override снят, чистая отдельная MainMenu закрыта без сохранения. Карты, ручные изменения, пауза, сценарии, глаз/блики не переписаны. Сцена/каталог/prefab/шрифты/музыка/концепты меню и Single runtime-переходы сохранены для будущего билда. MainMenuEditorNavigation остаётся необязательным помощником отдельной проверки меню, без callbacks и автозапуска.
- Первая попытка исправить только завершение Workspace была прекращена после нового решения владельца. В итоговом составе нет нового Workspace helper и его незавершённых тестов; система обязательного входа не заменена другой схемой.
- Оба AGENTS и контракт MainMenu отражают новое решение. Менеджерский чат обновил Notion: BUG-048 Won't do, TASK-UI-02 сохраняет историческую подготовку, правила обязательной entry отменены. Project/installed skills не менялись и не требовали синхронизации.

## Проверки

Компиляция итогового отката пройдена, ошибок нет. Дважды выполнен обычный Play → Stop прямо из открытого MorgueRouteTest, второй раз после принудительного domain reload. В обоих запусках: entry пустая, одна сцена MorgueRouteTest, MainMenuView=0, камера=1, AudioListener=1, EventSystem=0 (штатное состояние этой карты), сохранены HUD и disabled Pause Canvas. Никаких переходов через каталог для этих проб.

[Первый Play](../Validation/MenuIsolation49/direct-play-1.json), [Stop](../Validation/MenuIsolation49/stop-1.json), [повторный Play после reload](../Validation/MenuIsolation49/direct-play-2-after-reload.json). Итоговый снимок Editor, проверки тестов/хешей и сухая компиляция старых probes добавлены в том же каталоге Validation. Полный процесс Editor повторно не перезапускался; domain reload и два обычных Play покрывают удалённые startup/play callbacks. Новая сборка не нужна: изменения Editor-only, runtime/assembly включения Player не менялись.

Итоговые EditMode проверки сохранённого меню: 10/10, ошибок нет. Три восстановленных probe проходят dry-run компиляцию без диагностики. SHA256 четырёх .unity в корне Scenes совпадает, включая MainMenu и MechanicsSandbox. MorgueRouteTest на диске изменился между снимками (mtime 16:45:39 UTC); причина этой параллельной записи не установлена, совпадение всех хешей не заявляется. Проверочные скрипты этой задачи не вызывают SaveScene для карты. Изменённая карта сохранена как есть и полностью исключена из целевого коммита. [Финальное редактирование](../Validation/MenuIsolation49/final-edit.json), [Scene View после отката](../Validation/MenuIsolation49/final.png), [тесты](../Validation/MenuIsolation49/edit-tests.json), [хеши](../Validation/MenuIsolation49/scene-hashes.json), [компиляция probes](../Validation/MenuIsolation49/probes-compile.json). Исходный скрин владельца также сохранён в Validation.

Датированные отчёты прежних проверок сохраняют историю и не являются разрешением возвращать обязательный вход. [Текущий контракт](../Features/MainMenu.md).
