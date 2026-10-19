# Игровой HUD

Статус на начало архитектурного прохода 2026-10-19: uGUI-HUD реализован; визуальная приёмка и дальнейшая MVC-сверка ведутся отдельно.

Архитектурная цель — [ADR-001](../Decisions/001-sod-architecture.md); фактическая карта ниже перенесена из ARCHITECTURE без изменения поведения. При кодовой миграции обновлять её по исходникам.

## Игровой HUD

GameplayHudView — представление под существующим Canvas. Читает PlayerLife2D (HealthChanged / отдельное Damaged только для принятого урона), PlayerWeapon2D, Flashlight2D и необязательный PlayerAccessCards2D. HudWeaponRow показывает WeaponDefinition и переставляется между двумя редактируемыми anchors; правила владения/выбора остаются у оружия. GameplayHudStyle содержит цвета, длительности и необязательные hit clips; таймеры и потерянный участок HP принадлежат экземпляру. Отключение Canvas удаляет подписки/отклик; включение заново читает состояние. Старый PrototypeHud сохраняет контекст, подборы и поражение. [Настройка](../UI/HUD_DESIGN_SYSTEM.md).

## Направленная кровь по краям

Общий GameplayHud01 содержит вложенный prefab DirectionalDamage. Он слушает только принятый PlayerLife2D.Damaged: направление полёта удара инвертируется и проецируется камерой на край экрана. Отклонённый защитой удар не создаёт пятно; неизвестное направление не выдумывается. Существующие здоровье, покраснение игрока и реакция полосы HP не меняются.

Features/DamageFeedback разделён по SOD: чистая DirectionalDamageModel хранит восемь независимых затухающих пятен, ShowDamagePulseCommand проверяет вход, Controller передаёт состояние IDirectionalDamageView. View отвечает за камеру, события и uGUI. LevelContext устанавливает фабрику без состояния через DamageFeedbackInstaller; каждый HUD получает собственную модель и копию материала. Общий DirectionalDamageStyle неизменяем в Play. Восстановление здоровья и отключение объекта очищают отклик; подписки и материал освобождаются владельцем. Canvas скрывает эффект вместе с HUD.

Повторное использование на новом уровне требует общего HUD и обычного LevelContext, без копирования кода или ручного добавления пятен. Цвет, размер и время меняются в [профиле](../../Assets/EmergencyShift/Data/UI/Feedback/DirectionalDamage01.asset). [Проверки и игровые кадры](../Reports/DirectionalDamage01.md).

## Проверки и связанные документы

Контракт и авторинг: [Игровой HUD](../UI/HUD_DESIGN_SYSTEM.md). Проверки выбирать по [VALIDATION](../VALIDATION.md); прежние результаты — [Reports/ValidationIndex](../Reports/ValidationIndex.md). Полная архитектурная сверка — текущий этап [Roadmap](../Roadmap/00-ArchitectureMigration.md).
