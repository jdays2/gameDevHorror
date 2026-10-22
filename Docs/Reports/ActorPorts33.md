# Интерфейсы взаимодействия · ActorPorts33

2026-10-22, после b319fce. IInteractable/IAutoPickup принимают IInteractionActor вместо конкретного PlayerInteraction. Общий порт содержит reach и сообщения; отдельные IAccessCardRecipient/IFlashlightRecipient/IWeaponRecipient содержат только операции своей области и readonly значения. Они не возвращают MonoBehaviour, модели, контейнер или набор сервисов. Это граница native adapters; чистые selection/pickup/access commands по прежнему не получают Unity-компоненты.

Предметы запрашивают узкую возможность получателя и передают её существующим операциям/командам. Исчезли GetComponent из предмета карты и считывателя. PlayerInteraction кеширует свои компоненты при composition/Awake и делегирует существующим контроллерам. Проверка this != null учитывает уничтоженный Unity actor, сохранённый через interface. Перепроверка reach, выдачи, остатка патронов и one-shot consumption сохранена. Двери, диалог и бесконечный ящик используют общий порт без выбора типа предмета в центральном потребителе.

**1117/1117 EditMode**, включая шесть новых случаев с получателем без GameObject: недоступная/отклонённая выдача, фонарь/модуль, частичные патроны, доступ по карте и destroyed actor. **60/60 PlayMode**: реальные reentrant pickups, двери, диалог, оружие, lifetime/checkpoint. Обычная Windows64 success; собственная Player assembly только EmergencyShift.Runtime. [XML и hashes](../Validation/ActorPorts33.json). Новый дефект/red baseline этому этапу не приписывается.

1805 защищённых ресурсов и 60 исходных dirty файлов сохранены; сцены, prefab и serialized значения не менялись. Новые meta импортированы exclusive create. Проверена отдельная dirty копия с исходной паузой, не clean HEAD; чужая работа исключена из коммита. Имеющийся нефатальный LicensingClient warning остался.

Добавлены interface calls вместо component lookup, без нового Update, обхода сцены или аллокации на подбор. Проверенный scope — native interaction coupling и сохранение существующих command/model границ. Authored defaults/timing, прочая архитектура, tools/style и итоговая авторская приёмка ещё открыты.
