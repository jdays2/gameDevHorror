# Здоровье

`HealthModel` — чистый C# экземпляр текущего HP, предела и защиты после попадания. `IGameClock` передаёт время извне; `HealthSettings` — неизменяемая проекция авторенных значений. `HealthModelFactory` разделяет часы, но создаёт отдельную модель для каждой особи. Общего изменяемого HP нет.

`DamageHealthCommand` повторно проверяет состояние при Execute, отвергает неположительный урон, мёртвую и защищённую цель. Смертельный удар выдаёт один Died, обычный — Damaged. `SetHealthCapacityCommand` меняет только экземпляр и не воскрешает погибшего. Доверенное восстановление checkpoint выполняется без повторных эффектов смерти.

`EnemyHealth2D` сохраняет прежний GUID и внешний контракт, передаёт события модели. `PlayerLifeController` применяет damage/capacity/restore/restart через команды. Его три явных зависимости — HealthModel, IPlayerLifeEnvironment и IPlayerLifeView; Unity-объектов и coroutine в нём нет. При поражении модель уже мертва до остановки физики и уведомления UI. Порядок Damaged → HealthChanged сохранён, отказ не вызывает native эффектов.

`PlayerLifeEnvironment2D` кеширует компоненты из явного actor root и управляет defeat/reload: motor/interaction/weapon, фонарь, Rigidbody и checkpoint/SceneManager. `PlayerLife2D` сохраняет GUID/сериализованные поля и публичный фасад Damageable; он отображает цвет/flash и передаёт события существующему UI. Disable отменяет coroutine и возвращает нормальный цвет живого спрайта. Отключение картинки не авторизует или запрещает урон: правило остаётся у модели/команды.

Общий [PlayerRules](PlayerRules.md) задаёт здоровье/защиту и параметры native hit presentation; сценовые overrides сохранены. MaximumHealth принадлежит модели; временная ёмкость не перезаписывает authored поле. RestorePlayerHealthCommand сохраняет прежний clamp1..Maximum, снимает защиту, не повторяет смерть/урон и просит environment восстановить actor. RestartPlayerRoomCommand повторно проверяет поражение перед native reload. Формат checkpoint/ID не менялся.

Проверки: независимые HealthModelTests, EnemySystemsTests/PersistenceTests и интеграционный запуск уровня. Формат checkpoint этим шагом не изменён. Статус общего прохода — [этап 00](https://app.notion.com/p/3ff8d6dc06b681b6950bd8115222669c?pvs=204).
