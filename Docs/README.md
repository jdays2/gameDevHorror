# Документация Emergency Shift

Начни с [рабочего пространства Notion](https://app.notion.com/p/3ff8d6dc06b6819caa69ece0495375a3), текущего плана и затронутых карточек, затем прочитай соответствующую локальную спеку. [Обязательный порядок Notion](NOTION_WORKFLOW.md), ID/карта — [.agents/notion-project.json](../.agents/notion-project.json). Все планы, задачи, баги, вопросы и ченджлог ведутся там; прежние локальные файлы помечены как снимки. Reports/Archive читать по конкретной проверке/регрессии.

## Рабочий паттерн

[Как владельцу и агенту вести работу](Guidelines/DOCUMENTATION.md) · [Когда какой skill использовать](Roadmap/Skills.md) · [Соглашения](Conventions.md). Требование имеет одно каноническое место; история проверки не назначает контракт заново.

## По задаче

| Документ | Назначение | Когда читать |
|---|---|---|
| [Планы Notion](https://app.notion.com/p/c4b84aeca5c34ae1a9b3199d0bfd3e04) | Текущий приоритет, этапы и следующий шаг | Начало задачи, смена/закрытие шага |
| [SPEC](SPEC.md) | Принятые игровые требования и реализованный объём | Меняется игровое поведение/ограничение |
| [GAME_CONCEPT](GAME_CONCEPT.md) | Продуктовая рамка и предложения демо | Новый игровой этап; не путать план с реализацией |
| [ARCHITECTURE](ARCHITECTURE.md) | Фактические общие границы и статус миграции | Зависимости, слои, composition и сборки |
| [Conventions](Conventions.md) | Пути, C#, данные, UI и Git | Новый/изменяемый код, ассеты, документы |
| [Features](Features/README.md) | Контракты систем, владельцы, состояние и команды | По изменяемой фиче |
| [Баги Notion](https://app.notion.com/p/169cd5b853c84ee8a28ef529fc8292da) | Сообщения, воспроизведение и исправления | Регистрация, начало исправления, проверка |
| [Задачи Notion](https://app.notion.com/p/a1b30c6e0f8343168ea91ae5990ee9fb) | Технический долг BL, задачи и идеи | Границы шага и остаточные отклонения |
| [Вопросы в задачах Notion](https://app.notion.com/p/a1b30c6e0f8343168ea91ae5990ee9fb) | Карточки типа «Вопрос», Q-NNN | Нужен игровой выбор; перед повторным вопросом |
| [ADR-001](Decisions/001-sod-architecture.md) | Принятие SOD и полного прохода | Архитектура/политика миграции |
| [Шаблон ADR](Decisions/000-template.md) / [фичи](Features/_template.md) | Единый формат нового решения/контракта | Только при их создании |
| [VALIDATION](VALIDATION.md) | Как выбирать/повторять проверки | Код/сцена/сериализация/сборка |
| [Reports/ValidationIndex](Reports/ValidationIndex.md) | Результаты прежних прогонов и их границы | Сверка доказательства/регрессия |
| [Изменения и релизы Notion](https://app.notion.com/p/74792a1164a743d88ff1062c0ca9aab0) | Краткие итоги, версии и состав | Запись итога; поиск по ключу |
| [FILE_MAP](FILE_MAP.md) | Карта реальных файлов | Поиск владельца; обновлять из исходников |
| [MCP_SETUP](MCP_SETUP.md) | Каналы связи с Editor | Подключение/сбой Pipeline или MCP |
| [SOD adoption](Research/SOD_SKILLS_ADOPTION.md) | Источник skills, разрешённые конфликты, хеши | Обновление/проверка переноса |

## Игровые и авторские документы

| Область | Документы | Когда читать |
|---|---|---|
| Песочница/маршрут | [SANDBOX](SANDBOX.md), [MorgueRouteTest](Guides/MORGUE_ROUTE_TEST.md), [разбор уровня](Levels/FIRST_LEVEL.md) | Расстановка, тестовая сцена и принятый маршрут |
| Враги | [Сталкер](Enemies/STALKER.md), [Спящий](Enemies/SLEEPER.md), [разрыв](Enemies/RUPTURE_CONCEPT.md) | AI, световая реакция, смерть/останки |
| Оружие | [Индекс](Weapons/README.md), [пистолет](Weapons/PISTOL.md), [дробовик](Weapons/SHOTGUN.md) | Правила оружия/баланс, не общий roadmap |
| UI | [План UI](UI/UI_PLAN.md), [HUD](UI/HUD_DESIGN_SYSTEM.md), [диалог](UI/DIALOGUE_DESIGN_SYSTEM.md) | Текущий внешний контракт и будущие экраны |
| Авторинг/сохранение | [LEVEL_AUTHORING](Guides/LEVEL_AUTHORING.md), [CHECKPOINTS](Guides/CHECKPOINTS.md), [C# для владельца](Guides/CSHARP.md) | Редактирование, ID/restore, понимание системы |
| Арт/окружение | [ART_DIRECTION](ART_DIRECTION.md), [библиотека](Guides/ENVIRONMENT_LIBRARY.md), [идеи объектов](Environment/OBJECT_BACKLOG.md), [фотореференсы](Environment/VISUAL_REFERENCES.md) | Новые/заменяемые материалы и оценка |
| Materials | [Карта материалов](Guides/MATERIALS_LIBRARY.md) | Тематические папки, общие материалы/шейдеры/маски, безопасный перенос |
| Свет/тени | [Лампы](Guides/HOSPITAL_LAMPS.md), [стены](Guides/INTERIOR_WALL_FINISHES.md), [форма теней](Environment/FURNITURE_SHADOW_SHAPES.md), [проба мебели](Environment/FURNITURE_LIGHT_STUDY.md), [пустая комната](Guides/LIGHTING_PLAYGROUND.md) | Соответствующий источник/авторинг/регрессия |
| Пол/следы | [Разметка](Guides/FLOOR_MARKINGS.md), [износ](Guides/FLOOR_WEAR.md), [исследование крови](Research/BLOOD_AND_BATTLE_AFTERMATH.md) | Составление пола, художественные пробы |
| Аудио | [AUDIO_GUIDE](Audio/AUDIO_GUIDE.md) | Маршрутизация, микс, источники и согласованный звук |
| Контент/масштабирование | [CONTENT_AUDIT](CONTENT_AUDIT.md), [OPT-01](OPTIMIZATION_BACKLOG.md), [Concepts](../Concepts/README.md) | Использование, provenance и измеренные ограничения |
| Референсы/прошлые варианты | [LEVEL_DESIGN_REFERENCES](Research/LEVEL_DESIGN_REFERENCES.md), [схема 03](../Concepts/Unused/Levels/MorgueDemo03/README.md), [старый план](Archive/MVP_PLAN-before-SOD.md) | Поиск причины/истории, не текущая очередь |

[MVP_PLAN](MVP_PLAN.md) и [ENGINEERING_REVIEW](ENGINEERING_REVIEW.md) сохраняют прежние точки входа; очередь и активный долг теперь только в Notion; локальные версии сохранены как снимки. Игровые ассеты и ручные сцены документированием не меняются.
