# Изученные архитектурные источники

История оценки источников, перенесена 2026-10-19. Действующий стандарт — [ADR-001](../Decisions/001-sod-architecture.md), активные отклонения — [Backlog](../Backlog.md). Это не альтернативный набор правил.

## Изученные открытые примеры

Это первичные репозитории авторов, не знак качества или официальная рекомендация Unity. Из них взяты направления для оценки; текст/код целиком не копировались. Ссылки закреплены на просмотренных версиях.

| Пример | Полезно для нас | Что не переносим автоматически |
|---|---|---|
| [alpershin · unity-code-architecture](https://github.com/alpershin/claude-skill-unity-code-architecture/blob/e1e8c6f1975b7428203c874125cc680f79d8576c/unity-code-architecture/SKILL.md) | Проверка причин для паттерна, границ и запахов кода | Жёсткие лимиты параметров, запрет null/flag-параметров как универсальное правило, центральный ticker по одному числу объектов. Лицензия не определена API GitHub; материалы не импортированы |
| [tianzhiying · unity-performance](https://github.com/tianzhiying/Unity-perf/blob/12454db9979754b2c632a6e4974b04587825659c/skills/unity-performance/SKILL.md), [code-standards](https://github.com/tianzhiying/Unity-perf/blob/12454db9979754b2c632a6e4974b04587825659c/skills/code-standards/SKILL.md) · MIT | Сопоставимые замеры до/после, проверка реальной сборки, владение отложенной работой | Мобильные целевые метрики, тотальный zero-GC, обязательный многoагентный аудит |
| [AUN-PN · unity-agent-workflows](https://github.com/AUN-PN/unity-agent-workflows/blob/4193d84176e80fc38ee0252b0fa327119ef96d01/skills/unity-agent-workflows/SKILL.md) · MIT | Найти настоящий runtime-источник изменения, проверить путь состояния до результата | Большой обязательный протокол координат/IoU для несвязанных задач и правила автоматической остановки |
| [Nice-Wolf-Studio · unity-data-driven](https://github.com/Nice-Wolf-Studio/unity-claude-skills/blob/a149d3e995420432f52b0899092daa4b069f441f/skills/unity-data-driven/SKILL.md) · MIT | Разделение конфигов, авторинга и версионирования данных | Автоматическое внедрение чужого save-фреймворка/пакетов и обобщение примеров на все сериализаторы |

Таблица выше отражает прежнюю оценку этих источников. С 2026-10-19 отказ от лимитов constructor dependencies и общий запрет строгих архитектурных ориентиров заменены явным решением владельца принять SOD. Остальные непринятые механики/инструменты не вводятся автоматически.

Дополнительные источники: [Unity: форматирование C#](https://unity.com/how-to/formatting-best-practices-c-scripting-unity), [Unity: ScriptableObject data containers](https://unity.com/how-to/separate-game-data-logic-scriptable-objects). Правила хранения именно наших туш, агрессии и чекпоинтов остаются проектными решениями.
