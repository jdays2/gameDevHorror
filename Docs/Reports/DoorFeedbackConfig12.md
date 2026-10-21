# Конфиг считывателя двери · readonly и validation

Дата: 2026-10-21. База `e6e4475`. DoorAccessFeedbackProfile хранит private SerializeField с прежними именами; C#-потребители читают PascalCase getters. Ссылки denied/granted и исходные значения delay/cooldown/volume/hearingDistance сохраняются без переименования Unity-полей, GUID или перезаписи asset. Радиус полной громкости 2, дистанция панорамы 6 и предел .75 перенесены из кода в профиль с теми же defaults.

Validate отвергает NaN/Infinity, недопустимые диапазоны и обратный falloff (слышимость должна быть дальше радиуса полной громкости). DoorAccess2D проверяет авторенный профиль в Awake, заменяемый — до присваивания/перенастройки controller. Optional null-профиль/клипы сохраняют прежнюю политику; отсутствие clip само по себе не объявлено ошибкой конфигурации. Микс и тайминги действительных текущих profiles не меняются. [Контракт](../Features/Doors.md).

**711/711 EditMode**, 21 новая проверка: некорректные числа/отношения, старый plain JSON через runtime JsonUtility, все авторенные profiles/аудиоссылки без mutation и отказ invalid replacement до смены текущего профиля. Первая новая fixture дала 710/711: короткий plain JSON через EditorJsonUtility не перезаписал значения; исправлен её API, runtime не менялся ради теста. Узкие 21/21 и повторный полный прогон прошли.

**32/32 PlayMode**, включая composition/доступ/движение дверей и загрузку MorgueRouteTest с карточными считывателями. Обычная Windows64 success; собственная Player-сборка только EmergencyShift.Runtime. Source hashes и 1794 protected resources unchanged — [DoorFeedbackConfig12.json](../Validation/DoorFeedbackConfig12.json). Копия включает исходную незавершённую паузу; сообщение LicensingClient окружения остаётся нефатальным.

Валидация выполняется при создании/замене настройки, не в Mix на каждом кадре. Getters возвращают прежние значения; новых Update, объектов состояния, запросов физики, голосов или runtime-реестров нет. FPS и новая слуховая приёмка не заявляются. Это один проверенный config; остальные общие конфиги/ID/payload и полная MVC/DI-задача остаются открыты.
