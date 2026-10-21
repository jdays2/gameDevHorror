# Контракты фич

Текущая реализация отделена от целевой SOD-архитектуры. При миграции обновлять владельцев/команды по реальному коду, оставляя результаты прогонов в Reports.

| Фича | Что здесь | Детали/настройка |
|---|---|---|
| [World](World.md) | Игрок/контекст, камера, фонарь, двери/подборы и локальный свет | [Авторинг](../Guides/LEVEL_AUTHORING.md), [лампы](../Guides/HOSPITAL_LAMPS.md) |
| [Enemies](Enemies.md) | Общие модули, решения видов, данные/останки | [Сталкер](../Enemies/STALKER.md), [Спящий](../Enemies/SLEEPER.md) |
| [Weapons](Weapons.md) | Циклы/инвентарь и Unity-мост | [Оружие](../Weapons/README.md) |
| [Dialogue](Dialogue.md) | Данные, сеанс, пауза/захват, UI-prefab | [Дизайн диалога](../UI/DIALOGUE_DESIGN_SYSTEM.md) |
| [PauseMenu](PauseMenu.md) | Esc, пауза мира/аудио, возврат без сквозного игрового ввода, Below the Ward | [План UI](https://app.notion.com/p/3ff8d6dc06b681b3a635cf10e8836b1c?pvs=204), [музыка](../Audio/AUDIO_GUIDE.md#музыка-меню-паузы) |
| [Hud](Hud.md) | Данные HUD и представление | [HUD](../UI/HUD_DESIGN_SYSTEM.md) |
| [Persistence](Persistence.md) | Цели, ID, snapshot/restore и дисковая запись | [Чекпоинты](../Guides/CHECKPOINTS.md) |
| [Sandbox](Sandbox.md) | Временные условия независимых проверок | [Песочница](../SANDBOX.md), [маршрут](../Guides/MORGUE_ROUTE_TEST.md) |
| [Lighting](Lighting.md) | Контуры/URP-тени и Editor-границы | [Форма теней](../Environment/FURNITURE_SHADOW_SHAPES.md) |

Новая фича — по [_template](_template.md), принятый стандарт — [ADR-001](../Decisions/001-sod-architecture.md). Наличие документа не означает полного покрытия системы тестами.

- [Здоровье](Health.md): чистая модель, команды, защита и адаптеры.
- [Ввод и composition scopes](PlayerInput.md): Input Actions, игрок/уровень/мир и сроки жизни.
