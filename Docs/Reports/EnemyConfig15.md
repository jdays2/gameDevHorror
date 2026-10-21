# Общие конфиги врагов

Дата: 2026-10-21. База `511ff76`. EnemyDefinition, EnemyPatrolProfile, SleeperBehaviourProfile и StalkerBehaviourProfile используют private SerializeField с прежними именами. C# получает readonly getters либо immutable settings. Health, movement и обе behaviour settings проверяются до создания brain; NaN/Infinity не проходят через Sleeper.IsValid. Нулевая скорость преследования остаётся допустимой для неподвижных экземпляров.

HP, движение, melee timeline, патруль, реакция на свет и cooldown обоих видов сохранены. SandboxSceneBuilder записывает прежние значения через SerializedObject и проверяет definition до CreateAsset; генератор не запускался. Родные asset/prefab/сцены и GUID не перезаписывались.

**823/823 EditMode**, 21 новая проверка: недопустимые общие параметры блокируют обе brain settings, прежние поля десериализуются, authored profiles всех четырёх типов валидны без mutation. **32/32 PlayMode**, включая настоящий patrol/melee, FSM обоих видов и checkpoint/lifecycle. Обычная Windows64 success; собственная Player-сборка только EmergencyShift.Runtime.

Source parity, raw hashes и 1794 неизменённых ресурса — [EnemyConfig15.json](../Validation/EnemyConfig15.json). Первая компиляция выявила пропущенную запись displayName в Editor builder; она исправлена до успешных прогонов. Копия включает исходную незавершённую паузу и не является clean HEAD. LicensingClient остаётся нефатальным сообщением окружения; художественная/слуховая приёмка и FPS не заявляются.

Новых Update, источников звука или физических запросов нет. Валидация выполняется при построении настроек: один дополнительный временный melee snapshot на brain composition, без новых аллокаций на тик. Общие SO не хранят личное состояние. Прежние authoring transaction, Development probe и concrete checkpoint adapter остаются partial; этот результат не завершает полный SOD-перенос.
