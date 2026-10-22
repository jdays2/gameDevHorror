# Активация размещённой группы

`Features/Encounter/Logic/EncounterModel` хранит личный флаг Activated. `ActivateEncounterCommand` отклоняет повторный запуск; Controller после успешной команды вызывает `IEncounterView.ActivateActors`. `RestoreEncounterCommand` меняет только флаг: загрузка не активирует и не сбрасывает врагов повторно. Состояния самих авторенных акторов восстанавливают их checkpoint-адаптеры.

`EncounterActivation2D` — trigger/scene environment adapter. Список акторов, положение и размер области остаются в сцене/prefab. `EncounterControllerFactory` внедряется из уровня, каждый экземпляр получает отдельную модель. Повторная композиция сохраняет флаг; Awake только настраивает trigger и не выключает существующий бой.

Формат `EncounterCheckpoint2D` и persistent IDs не менялись. Это одноразовая активация готовых объектов, без динамического спавнера, волн и кампании. Модели проверяются без сцены; native adapter и silent restore — отдельными checkpoint-тестами. Результаты — [индекс проверок](../Reports/ValidationIndex.md).
