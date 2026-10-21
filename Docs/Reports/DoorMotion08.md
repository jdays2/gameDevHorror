# Двери · MVC/DI

Дата: 2026-10-21. База `6418452`. DoorMotionModel/commands/controller выделены из Door2D; геометрия, duration .3, openOffset, доступ по карте и события звука сохранены. Состояние и таймеры личные, часы внедрены. [Контракт](../Features/Doors.md).

**671/671 EditMode**: 16 новых случаев проверяют lock/busy и повторную валидацию команды, smoothstep открытия/закрытия, остановленные часы, confirmation deadline, отмену ожидания, остановку движения ровно один раз, бесшумное restore, изоляцию экземпляров, Dispose и недопустимые duration/delay. Старая checkpoint fixture теперь явно создаёт фабрику движения.

**28/28 PlayMode**: настоящий LevelContext и Door2D MechanicsSandbox. Действительно перемещаются leaf и её Collider; пауза замораживает положение, restore не повторяет события, disable завершает движение. Старый ClearWhileDoorMoves продолжает проверять запрет поздней записи сохранения. Первый новый прогон дал 27/28: его искусственная дверь создавалась без обязательной factory до Awake. Исправлена fixture, игровой fallback/service locator не добавлялся.

Обычная Windows64 success, собственная Player-сборка только EmergencyShift.Runtime; raw/hash и ресурсный guard — [DoorMotion08.json](../Validation/DoorMotion08.json). Тестовая копия содержит исходную незавершённую паузу; это не чистый HEAD. Build log содержит прежнее нефатальное сообщение LicensingClient окружения.

Стоимость: личные settings/model/controller на создание двери, прежняя coroutine на действие и события начала/конца. Постоянный Update не добавлен; новые физические запросы и массивы на tick отсутствуют. Controller применяет нормализованную позицию, Model считает smoothstep по scaled clock. Точный шаг начала/конца квантуется кадром; скриптовая запись Transform посторонним компонентом в середине движения не поддерживается. FPS не измерялся.

Совместимые поля duration/openOffset и IInteractable с concrete actor остаются на config/interaction-pass, поэтому Door2D ещё partial. Остальные игровые границы и итоговая авторская приёмка открыты.
