# Движение двери

Личный `DoorMotionModel` хранит open/moving/awaitingAccess и нормализованное положение. Неизменяемые settings задают длительность; `IGameClock` даёт scaled-время. При паузе часы не движутся, створка остаётся на месте. Ожидание подтверждения предшествует первому открытию; состояние open меняется в начале движения. Повторный toggle занятой или запертой двери отклоняется командой. Закрыть открытую дверь можно без карты.

`ToggleDoorCommand`, `RestoreDoorMotionCommand` и `StopDoorMotionCommand` проверяют внешний переход. Controller связывает модель с `IDoorMotionView`, освобождает обе подписки при Dispose. Restore бесшумен и отменяет старую работу; disable отменяет ожидание, а движущуюся створку доводит до выбранного конца и выдаёт один MotionStopped.

`Door2D` сохраняет GUID, IInteractable, RestoreState/Toggle, ссылки на leaf и существующие события аудио. Он выполняет Transform-запись и повторно проверяет доступность актору. Coroutine вызывает controller только во время ожидания/движения; постоянного Update на каждую дверь нет. Collider и ShadowCaster остаются детьми створки. Factory/installer внедряются через LevelContext до Awake.

`duration`, `openOffset` и дальность взаимодействия пока остаются прежним авторенным compatibility bridge; общий config-pass и перенос интерфейса взаимодействия открыты. Публичный DoorMotionModel не зависит от Unity-компонентов, доступа по карточке или аудио. Состояние замка имеет отдельного владельца в AccessControl. Формат door.v1 не меняется.

Сценарии проверки и ограничения — [DoorMotion08](../Reports/DoorMotion08.md). Общие связи мира — [World](World.md).

`DoorAccessFeedbackProfile` настраивается в Inspector и доступен C#-потребителям только для чтения. Прежние serialized names сохранены. Помимо clips/delay/cooldown/volume, он задаёт FullVolumeRadius, HearingDistance, PanDistance и MaximumPan. HearingDistance должен превышать FullVolumeRadius, PanDistance положителен; числа конечные, громкость/предел панорамы в 0–1. Reader проверяет профиль в Awake и до замены через ConfigureFeedback. Null-профиль сохраняет старый мгновенный старт; клипы могут отсутствовать. Модель доступа копирует настройки cooldown, аудио читает общий неизменяемый профиль. [Проверка config-pass](../Reports/DoorFeedbackConfig12.md).
