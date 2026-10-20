# Пофайловый аудит SOD · диалоги, пауза, индикация урона

Дата: 2026-10-21. База Git: `09175d7`; проверено текущее рабочее дерево, включая ранее незакоммиченные файлы паузы/ввода. Игровые исходники, сцены, prefab, настройки и GUID этим проходом не изменялись. Это результат аудита, не реализация MVC-миграции и не новый релиз.

## Покрытие и значение статусов

Полностью прочитаны 45 C# выбранной области: 11 диалоговых файлов, 14 файлов паузы, 11 файлов индикации урона и 9 связанных файлов ввода/модального контекста/композиции/игрока. Для каждого в [реестре](CodeAudit.json) записаны ответственность, зависимости, конкретное использование, замечания, дата и `reviewedSha256`.

В этой выборке 35 `reviewed_boundary` и 10 `partial`. Первый статус означает, что проверена указанная граница исходников; он не сертифицирует всю фичу, графику, производительность или сценовый сценарий. `partial` после этого прохода означает прочитанный файл с конкретным архитектурным долгом; старые partial могут обозначать только частичное чтение — смотреть `reviewScope` и заметки. `queued` означает отсутствие семантического заключения.

Реестр расширен с 225 до 269 собственных C#: 250 под `Assets/EmergencyShift`, 11 выполняемых по запросу утилит `AgentScripts`, 8 сохранённых C#-исходников/черновиков `Concepts`. Последние две группы не компилируются как игровой runtime. Они учтены отдельно и не объявлены неиспользуемыми. Общий результат: **74 reviewed_boundary / 43 partial / 152 queued**, `complete=false`. 25 новых Unity-файлов покрыты этим проходом; 19 файлов вне Assets добавлены в очередь. Четыре изменённых старых файла были сначала лишены устаревшей отметки проверки, затем прочитаны заново.

## Найденные границы и дефект

- **Диалоги:** `DialogueRun` хранит ссылку на `DialogueSequence` (ScriptableObject), отдаёт mutable `Line` и принимает внешние переходы без принятой границы ICommand/Result. Прогресс при этом действительно индивидуален, и запись в asset в текущем прохождении не найдена. `DialogueSession` одновременно владеет переходами, вводом, Time.frameCount, паузой, HUD и курсором; это не только делегирующий координатор. Выделение чистого графа, controller/команд и Unity-адаптера относится к существующей MVC-задаче, без добавления игровых последствий/сохранений.
- **BUG-004:** `DialogueInteraction2D.IsAvailable` не проверяет активность `session`, `DialogueSession.TryBegin` также допускает отключённый компонент. Если отключить только Session, активный маркер может начать разговор и получить input/motion leases. Update отключённого Session не работает, View-подписки сняты — подтверждение не освобождает контекст. [Карточка бага](https://app.notion.com/p/3ff8d6dc06b681b5beeaf9c860dd4138) содержит шаги и критерии проверки. Причина подтверждена исходниками; Play-воспроизведение ещё требуется, статус «Нужно уточнить».
- **Пауза:** чистые Model/Controller и проверяемые команды уже выделены. Контроллер имеет две зависимости, собственные аренды и rollback при ошибке AcquirePause. `PauseMenuPresenter` имеет четыре collaborator и собственную последовательность quit/fade; при дальнейшей миграции требуется отделить платформенный выход, не прятать зависимости в context bag. Три зависимости `PauseMenuEnvironment` имеют узкое обоснование: проверка живого игрока и управление двумя видами захвата. Прежнее состояние AudioListener восстанавливается, но это не общий сервис нескольких параллельных audio-пауз.
- **Индикация урона:** модель/команда/контроллер независимы от Unity-объектов; Vector2/Mathf допускаются SOD. Настройки копируются в readonly struct, состояние ограничено восемью импульсами, личный material принадлежит View. Адаптер View получает камеру через конкретный `Flashlight2D`; нужен узкий контракт проекции/камеры. Порядок `PlayerLife2D.Damaged → HealthChanged` проверен в коде и согласован с `damagePending`.
- **Смежный долг:** `PlayerLife2D` остаётся объединением health-adapter, выключения actor-компонентов, coroutine и restart/checkpoint. `AimReticle2D` читает legacy weapon/aim напрямую; в Ring есть объединённые объявления, в PauseMenuAuthoring — цепочки присваиваний. Это замечания к границам/читаемости, не основание автоматически переписывать игровое поведение или форматировать сцены.

## Проверки и ограничения

`Tools/Maintenance/audit_code.py` обновляет только механический инвентарь: SHA-256, типы/лексические подсказки, сохранённые внешние GUID-ссылки и их транзитивное достижение из сцен. Старая AssetDatabase-проверка не выдаётся за текущий запуск: новый `referenceMethod` прямо описывает текстовый YAML-граф. Это не проверка локальных fileID, обязательных null, reflection или действительного вызова метода. Лексическое совпадение вложенного типа не доказывает использование.

По умолчанию утилита только проверяет согласованность реестра; новые/удалённые/изменённые файлы или устаревший reviewedSha256 дают ненулевой exit code. `--refresh --date YYYY-MM-DD` обновляет метаданные, добавляет queued и переводит изменённые ранее проверенные записи в partial, сохраняя старый reviewedSha256. Утилита никогда автоматически не присваивает reviewed_boundary. Семантические заметки этого прохода внесены после чтения исходников.

Проверены 13 точных имён `FindAction` по текущему JSON Input Actions, полнота 269 путей/хешей и отсутствие пропущенных файлов выбранной области. Прочитаны 14 диалоговых data test cases, 3 prefab-теста, 15 тестов контроллера паузы, 5 environment test cases, 3 input-теста и 16 feedback test cases. Это сверка содержания тестов, **не их новый успешный запуск**.

Глобальная статическая проверка контента нашла одну прежнюю битую ссылку в историческом README HospitalService01; она зарегистрирована отдельно как [BUG-005](https://app.notion.com/p/3ff8d6dc06b681cb821bd7122c617455), файл не менялся. Её 232 GUID вне сканируемой карты Assets не интерпретируются как отсутствующие ресурсы без AssetDatabase/package resolution. В новых ссылках этого отчёта ошибок нет. Проверка whitespace ограничена изменениями этого аудита: существующие пробелы в незавершённых сценовых правках не исправлялись.

`unity command editor_status` с явным project-path вернул отсутствие экземпляра Pipeline. Новый Unity Editor автоматически не запускался. EditMode, Play, сборка, прослушивание музыки и визуальная приёмка не выполнялись; старые результаты находятся в профильных отчётах. Отсутствующий Play-прогон не мешает записать замечания к исходникам, но не позволяет закрыть BUG-004 как исправленный.

Общие задачи инвентаризации, MVC-миграции и финальных проверок остаются открыты. Активные результаты/статусы и следующий шаг находятся в [Notion](https://app.notion.com/p/3ff8d6dc06b6819fbbc3c56bd63e6b5e), этот файл хранит проверяемое свидетельство.

## Файлы этого прохода

Полные зависимости и выводы по каждому файлу — поля `responsibility`, `dependencies`, `usageEvidence`, `reviewScope`, `notes` в CodeAudit.json. Ниже сокращённый указатель.

| Файл | Ответственность | Результат |
|---|---|---|
| [Editor/Authoring/DialoguePrefabAuthoring.cs](../../Assets/EmergencyShift/Editor/Authoring/DialoguePrefabAuthoring.cs) | Экспорт и установка общего UI диалога | reviewed_boundary |
| [Editor/Authoring/DialogueSandboxAuthoring.cs](../../Assets/EmergencyShift/Editor/Authoring/DialogueSandboxAuthoring.cs) | Авторинг тестового графа и маркера | reviewed_boundary |
| [Runtime/Dialogue/DialogueInteraction2D.cs](../../Assets/EmergencyShift/Runtime/Dialogue/DialogueInteraction2D.cs) | Адаптер взаимодействия E с точкой разговора | partial |
| [Runtime/Dialogue/DialogueRun.cs](../../Assets/EmergencyShift/Runtime/Dialogue/DialogueRun.cs) | Позиция одного прохождения графа и уведомления переходов | partial |
| [Runtime/Dialogue/DialogueSequence.cs](../../Assets/EmergencyShift/Runtime/Dialogue/DialogueSequence.cs) | Общий граф реплик и валидация ID/переходов | partial |
| [Runtime/Dialogue/DialogueSession.cs](../../Assets/EmergencyShift/Runtime/Dialogue/DialogueSession.cs) | Модальный сеанс, ввод, UI и аренды паузы | partial |
| [Runtime/Dialogue/DialogueStyle.cs](../../Assets/EmergencyShift/Runtime/Dialogue/DialogueStyle.cs) | Общие визуальные настройки диалога | reviewed_boundary |
| [Runtime/UI/DialogueFadeGraphic.cs](../../Assets/EmergencyShift/Runtime/UI/DialogueFadeGraphic.cs) | Градиентная сетка UI | reviewed_boundary |
| [Runtime/UI/DialogueView.cs](../../Assets/EmergencyShift/Runtime/UI/DialogueView.cs) | Отображение текста, портрета и ответов | reviewed_boundary |
| [Tests/EditMode/DialogueDataTests.cs](../../Assets/EmergencyShift/Tests/EditMode/DialogueDataTests.cs) | Проверки графа и владельцев модального контекста | reviewed_boundary |
| [Tests/EditMode/DialoguePrefabTests.cs](../../Assets/EmergencyShift/Tests/EditMode/DialoguePrefabTests.cs) | Контракт prefab и привязок двух сцен | reviewed_boundary |
| [Editor/Authoring/PauseMenuAuthoring.cs](../../Assets/EmergencyShift/Editor/Authoring/PauseMenuAuthoring.cs) | Создание prefab паузы, PCM импорта и установка в две сцены | partial |
| [Runtime/Features/PauseMenu/PauseMenuInstaller.cs](../../Assets/EmergencyShift/Runtime/Features/PauseMenu/PauseMenuInstaller.cs) | Композиция модели, environment и контроллера | reviewed_boundary |
| [Runtime/Features/PauseMenu/Config/PauseMenuAudioConfig.cs](../../Assets/EmergencyShift/Runtime/Features/PauseMenu/Config/PauseMenuAudioConfig.cs) | Настройки клипов, громкости и переходов | reviewed_boundary |
| [Runtime/Features/PauseMenu/Logic/IPauseMenuEnvironment.cs](../../Assets/EmergencyShift/Runtime/Features/PauseMenu/Logic/IPauseMenuEnvironment.cs) | Контракт разрешения и эффектов паузы | reviewed_boundary |
| [Runtime/Features/PauseMenu/Logic/OpenPauseMenuCommand.cs](../../Assets/EmergencyShift/Runtime/Features/PauseMenu/Logic/OpenPauseMenuCommand.cs) | Проверяемый внешний запрос открытия | reviewed_boundary |
| [Runtime/Features/PauseMenu/Logic/ResumePauseMenuCommand.cs](../../Assets/EmergencyShift/Runtime/Features/PauseMenu/Logic/ResumePauseMenuCommand.cs) | Проверяемый внешний запрос возврата | reviewed_boundary |
| [Runtime/Features/PauseMenu/Logic/PauseMenuModel.cs](../../Assets/EmergencyShift/Runtime/Features/PauseMenu/Logic/PauseMenuModel.cs) | Состояние IsOpen одного меню | reviewed_boundary |
| [Runtime/Features/PauseMenu/Logic/PauseMenuController.cs](../../Assets/EmergencyShift/Runtime/Features/PauseMenu/Logic/PauseMenuController.cs) | Команды, арендованные ресурсы и release fence | reviewed_boundary |
| [Runtime/Features/PauseMenu/View/PauseMenuEnvironment.cs](../../Assets/EmergencyShift/Runtime/Features/PauseMenu/View/PauseMenuEnvironment.cs) | Unity-мост владения паузой мира/аудио/ввода | reviewed_boundary |
| [Runtime/Features/PauseMenu/View/PauseMenuMusic.cs](../../Assets/EmergencyShift/Runtime/Features/PauseMenu/View/PauseMenuMusic.cs) | Косметическое вступление/петля и затухание | reviewed_boundary |
| [Runtime/Features/PauseMenu/View/PauseMenuPresenter.cs](../../Assets/EmergencyShift/Runtime/Features/PauseMenu/View/PauseMenuPresenter.cs) | Привязка intent, отображения и выхода из приложения | partial |
| [Runtime/Features/PauseMenu/View/PauseMenuView.cs](../../Assets/EmergencyShift/Runtime/Features/PauseMenu/View/PauseMenuView.cs) | Canvas, кнопки, курсор, HUD и UI selection | reviewed_boundary |
| [Tests/EditMode/PauseMenuTests.cs](../../Assets/EmergencyShift/Tests/EditMode/PauseMenuTests.cs) | Независимые проверки model/command/controller lifecycle | reviewed_boundary |
| [Tests/EditMode/PauseMenuEnvironmentTests.cs](../../Assets/EmergencyShift/Tests/EditMode/PauseMenuEnvironmentTests.cs) | Проверки Unity паузы и вложенных motion leases | reviewed_boundary |
| [Runtime/Features/PlayerInput/Logic/IPlayerInput.cs](../../Assets/EmergencyShift/Runtime/Features/PlayerInput/Logic/IPlayerInput.cs) | Контракт локального намерения игрока | reviewed_boundary |
| [Runtime/Features/PlayerInput/View/InputActionPlayerInput.cs](../../Assets/EmergencyShift/Runtime/Features/PlayerInput/View/InputActionPlayerInput.cs) | Локальный экземпляр Input Actions и чтение intent | reviewed_boundary |
| [Runtime/Features/PlayerInput/PlayerInputInstaller.cs](../../Assets/EmergencyShift/Runtime/Features/PlayerInput/PlayerInputInstaller.cs) | Композиция локальных input и block | reviewed_boundary |
| [Tests/EditMode/PlayerInputTests.cs](../../Assets/EmergencyShift/Tests/EditMode/PlayerInputTests.cs) | Проверка intent и изоляции action clones | reviewed_boundary |
| [Runtime/Core/GameplayInputBlock.cs](../../Assets/EmergencyShift/Runtime/Core/GameplayInputBlock.cs) | Reference-counted личная блокировка ввода | reviewed_boundary |
| [Runtime/Core/WorldMotionPause.cs](../../Assets/EmergencyShift/Runtime/Core/WorldMotionPause.cs) | Scoped остановка масштабируемого мира | reviewed_boundary |
| [Runtime/Levels/LevelContext2D.cs](../../Assets/EmergencyShift/Runtime/Levels/LevelContext2D.cs) | Composition root уровня/локального игрока и lifetimes | partial |
| [Runtime/UI/AimReticle2D.cs](../../Assets/EmergencyShift/Runtime/UI/AimReticle2D.cs) | Положение и сетка прицела/визуализация перезарядки | partial |
| [Runtime/Player/PlayerLife2D.cs](../../Assets/EmergencyShift/Runtime/Player/PlayerLife2D.cs) | Health Unity-adapter, смерть, hit flash и restart | partial |
| [Runtime/Features/DamageFeedback/DamageFeedbackInstaller.cs](../../Assets/EmergencyShift/Runtime/Features/DamageFeedback/DamageFeedbackInstaller.cs) | Композиция фабрики локального HUD feedback | reviewed_boundary |
| [Runtime/Features/DamageFeedback/Config/DamageFeedbackSettings.cs](../../Assets/EmergencyShift/Runtime/Features/DamageFeedback/Config/DamageFeedbackSettings.cs) | Immutable числовые параметры импульса | reviewed_boundary |
| [Runtime/Features/DamageFeedback/Config/DirectionalDamageStyle.cs](../../Assets/EmergencyShift/Runtime/Features/DamageFeedback/Config/DirectionalDamageStyle.cs) | Unity-оформление и проекция числового конфига | reviewed_boundary |
| [Runtime/Features/DamageFeedback/Logic/DamagePulse.cs](../../Assets/EmergencyShift/Runtime/Features/DamageFeedback/Logic/DamagePulse.cs) | Immutable визуальный импульс | reviewed_boundary |
| [Runtime/Features/DamageFeedback/Logic/DirectionalDamageModel.cs](../../Assets/EmergencyShift/Runtime/Features/DamageFeedback/Logic/DirectionalDamageModel.cs) | Ограниченный набор импульсов и их fade | reviewed_boundary |
| [Runtime/Features/DamageFeedback/Logic/DirectionalDamageController.cs](../../Assets/EmergencyShift/Runtime/Features/DamageFeedback/Logic/DirectionalDamageController.cs) | Командная граница и показ состояния | reviewed_boundary |
| [Runtime/Features/DamageFeedback/Logic/DirectionalDamageFactory.cs](../../Assets/EmergencyShift/Runtime/Features/DamageFeedback/Logic/DirectionalDamageFactory.cs) | Создание независимых модели/контроллера HUD | reviewed_boundary |
| [Runtime/Features/DamageFeedback/Logic/IDirectionalDamageView.cs](../../Assets/EmergencyShift/Runtime/Features/DamageFeedback/Logic/IDirectionalDamageView.cs) | Контракт представления импульсов | reviewed_boundary |
| [Runtime/Features/DamageFeedback/Logic/ShowDamagePulseCommand.cs](../../Assets/EmergencyShift/Runtime/Features/DamageFeedback/Logic/ShowDamagePulseCommand.cs) | Проверяемое отображение принятого урона | reviewed_boundary |
| [Runtime/Features/DamageFeedback/View/DirectionalDamageFeedbackView.cs](../../Assets/EmergencyShift/Runtime/Features/DamageFeedback/View/DirectionalDamageFeedbackView.cs) | Unity-адаптер событий здоровья, камеры и GPU material | partial |
| [Tests/EditMode/DirectionalDamageTests.cs](../../Assets/EmergencyShift/Tests/EditMode/DirectionalDamageTests.cs) | Чистые регрессии импульсов, направления и времени | reviewed_boundary |
