# Останки · DTO и Unity-адаптер

Дата: 2026-10-21. База `44850cb`. Из RemainsSpriteState извлечены методы Capture/Apply SpriteRenderer в RemainsSpriteAdapter. RemainsSpriteState и RemainsPartState находятся в отдельных файлах Logic, RemainsSnapshot сохраняет прежний путь/GUID. Их прежний namespace, имена/порядок полей, типы и presence flags сохранены; миграция payload и новая версия не нужны.

DTO теперь содержит только значения и вложенные DTO. Unity Transform/color/enabled читает и записывает отдельный View-адаптер. Extension-вызовы сохраняют исходный синтаксис потребителей; данные не знают ни адаптера, ни SpriteRenderer. RemainsSnapshotValidation по-прежнему выполняет preflight всего снимка перед изменением сцены; правила смерти/генерации/владения этим извлечением не меняются.

**671/671 EditMode**, включая JsonRoundTrip трёх форм крови, legacy без bloodVariantId, тихое восстановление ShadowStalker/Sleeper без новых GibMotion и изменения профилей, отклонение повреждённых вложенных состояний до mutation. Использованы существующие поведенческие регрессии; зеркальные тесты перестановки методов не добавлялись. **28/28 PlayMode** и обычная Windows64 success подтверждают composition/lifecycle и компиляцию runtime-адаптера. Собственная Player-сборка только EmergencyShift.Runtime.

Поля сравнены с исходником базы, source hashes совпадают с тестовой копией, 1794 защищённых ресурса не изменились. Подробности — [RemainsDto09.json](../Validation/RemainsDto09.json). Тестовая копия включает исходную незавершённую паузу; build log сохраняет нефатальное сообщение LicensingClient. Это не clean-HEAD, художественная приёмка или проверка всех авторенных маршрутов.

Новых Update, объектов состояния, запросов физики или выделений на Capture/Apply нет: те же действия выполняет статический адаптер. DTO создаются в прежних точках. FPS не измерялся. RemainsRecord/Gore генерация и остальные model/controller/config границы остаются открыты; отделение DTO не объявляется завершением всей системы останков.
