# Движение игрока

`PlayerMovementModel` хранит личное направление и считает скорость по неизменяемому `PlayerMovementSettings`. `SetMovementIntentCommand` проверяет конечность компонентов и длины вектора до изменения состояния. Ненулевой ввод нормализуется: диагональ и частичное отклонение стика сохраняют прежнюю полную скорость. Игровые правила движения этим переносом не меняются.

`PlayerMovementController` читает `IPlayerInput` и проверяет `GameplayInputBlock`. Блокировка проверяется повторно при чтении скорости для физического шага: открывшееся между Update и FixedUpdate окно сразу останавливает игрока. Некорректный ввод возвращает отказ и очищает личное направление. `PlayerMovementControllerFactory` создаёт отдельную модель на каждый motor; `PlayerMovementInstaller` подключён в LevelContext.

`PlayerMotor2D` остаётся Unity environment: кеширует Rigidbody2D, запрашивает ввод в Update и записывает скорость в FixedUpdate. Disable очищает направление и немедленно обнуляет скорость тела. Подписки и дополнительный общий Update-менеджер не нужны.

`PlayerRules.asset` задаёт общую скорость; старое `moveSpeed` сохраняет authored override по явной [политике совместимости](PlayerRules.md). Фабрика проецирует неизменяемые settings при composition, пользовательские значения и GUID сохранены. Изменение поля в Inspector после построения controller не меняет уже созданные settings.

Проверки и ограничения — [PlayerMovement07](../Reports/PlayerMovement07.md). Ввод и сроки жизни — [PlayerInput](PlayerInput.md).
