# CLI проекта

**[Проект](../../README.md)** · [План демо](../MVP_PLAN.md) · [MCP](MCP.md) · [Skills](SKILLS.md)

| Инструмент | Для чего нужен проекту |
|---|---|
| Unity Editor 6000.6.5f1 | Редактирование сцен/профилей, Play, тесты и Windows-сборки |
| Git | История, отдельные коммиты задач и сохранение ручных правок |
| Python 3.12 | Подготовка контента, экспорт схем и локальный просмотр плана |
| FFmpeg | Нарезка/конвертация и проверка аудио; путь передаётся обработчикам явно |

Unity Editor этого компьютера: `C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Unity.exe`. Для Python используется комплектная среда Codex или среда конкретного обработчика; WindowsApps alias не является рабочей установкой.

## Что запускаем

| Задача | Вход |
|---|---|
| Тестировать механику и смотреть профили | Unity → [MechanicsSandbox](../SANDBOX.md) |
| Проверять изменение перед завершением | [Тесты и сборки проекта](../VALIDATION.md) |
| Готовить звук оружия | [prepare_firearm_mix.py](../../Tools/Audio/prepare_firearm_mix.py) |
| Готовить мембрану Сталкера | [generate_stalker_membrane_v04.py](../../Tools/Audio/generate_stalker_membrane_v04.py) |
| Готовить общий разрыв | [prepare_creature_rupture_06.py](../../Tools/Audio/prepare_creature_rupture_06.py) |
| Работать со схемой демо | [Редактируемая схема 03](../../Concepts/Unused/Levels/MorgueDemo03/README.md); ручные draw.io/SVG не перезаписывать экспортом |
| Открыть план с галочками | `Открыть план.cmd` → [инструкция](../Guides/PLAN_VIEWER.md) |

Аудиообработчики используют [свои зависимости](../../Tools/Audio/requirements.txt) и источники из Concepts. Принятый звук не перегенерируем без соответствующего поручения.
