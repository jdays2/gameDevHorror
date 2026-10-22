# Главное меню

`Scenes/MainMenu.unity` — временная стартовая сцена для тестирования. Последнее указание владельца: чёрно-белый вид, без фонарика, декоративных линий и пояснений под пунктами. Финальная художественная приёмка не выполнена.

`Prefabs/UI/MainMenu/MainMenuUI.prefab` редактируется в Unity. Barlow Condensed Bold — узкий заголовок, Noto Sans Medium — текст с кириллицей; TTF и OFL находятся в `Fonts/MainMenu`. Цвета — `Data/UI/MainMenu/NightShift.asset`. CanvasScaler Expand сохраняет доступность элементов при смене пропорций окна.

## Поведение

- Новая игра подтверждает запуск чистой MechanicsSandbox; старые чекпоинты не удаляются. Кампании и переноса прогресса нет.
- Сохранения показывают пустое состояние: у доступных тестовых сцен сохранений пока нет. Демонстрационные слоты не выдаются за настоящие.
- Настройки: общая громкость через AudioListener.volume и fullscreen Player. PlayerPrefs `EmergencyShift.MenuPreferences.v1` хранит версионированные значения. В Editor fullscreen сохраняется для Player без изменения окна редактора.
- Выход подтверждается и ждёт затухания музыки. В Editor возвращает домой, не закрывая редактор.
- Выбор сцены и F8 доступны в Editor/development build. `Data/UI/MainMenu/SceneCatalog.asset` содержит стабильный ID, название и полный путь каждой сцены. Добавить сцену можно данными и включением в Build Settings; отсутствующая в сборке строка блокируется. Сейчас включены MechanicsSandbox, MorgueRouteTest, BasementPrototype и LevelTemplate.
- Мышь/клавиатура используют один активный пункт, выделенный светлым текстом. Esc возвращает домой. F8 в игре возвращает в меню, а в меню открывает каталог.

## Владельцы и освобождение

MainMenuWorldInstaller дополняет существующий ProjectContext: неизменяемый снимок каталога, настройки и MenuNavigationAdapter живут в world scope. Model/commands/controller отдельного MainMenuContext принадлежат сцене; Presenter/View — Unity-представление. Команды повторно проверяют раздел, доступность сцены и блокируют повторную загрузку. Ошибка восстанавливает исходный раздел с сообщением.

LoadSceneAsync Single выполняется после отпускания подтверждения и fade-out. Переход освобождает старый scene scope, UI, камеру, музыку и EventSystem; нажатие запуска не становится выстрелом. Игровые карты не переписываются ради возврата. Собственные InputAction освобождаются адаптерами; controller снимает подписки при закрытии scope. Runtime не меняет общие SO.

## Музыка

Повторно используются существующие PauseMenuMusic и `Data/Audio/PauseMenuAudio.asset`: Below the Ward 02, вступление 42.95 с, петля 21.333333 с, перекрытие 0.25 с, громкость 0.4, fade-in 1.5 с и fade-out 0.8 с. Главное меню имеет собственные два AudioSource, общие WAV и алгоритм с паузой. Навигация между разделами не перезапускает трек. Загрузка/выход ждут fade-out; возврат домой начинает вступление заново. Выгрузка уничтожает источники.

Новых генераций, обработки и копий WAV нет. [Происхождение](../../Concepts/Used/Audio/PauseMenuMusic01/provenance.json), [проверки](../Reports/MainMenu01.md).

## Авторинг

`AgentScripts/InstallMainMenu.cs` создаёт новые ресурсы через MainMenuAuthoring и подключает существующую музыку через MainMenuAudioAuthoring. Генерация поверх существующего меню запрещена: редактировать prefab/конфиги. Команды `Emergency Shift/UI/Use main menu as Play entry` и `Use current scene as Play entry` переключают редакторскую точку запуска. В Build Settings меню первое. `MainMenuTests` проверяют команды, каталог, настройки и disposal; `AgentScripts/VerifyMainMenu.cs` — реальные кнопки/переходы, Esc/F8, музыку и возврат из паузы с восстановлением временных InputSettings.
