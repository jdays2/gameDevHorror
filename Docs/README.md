# Документация Emergency Shift

Начни с [текущего статуса Roadmap](Roadmap.md) и [активного этапа](Roadmap/00-ArchitectureMigration.md). Остальное ищи по теме; каталог не требует читать все ссылки. Reports/Archive — по конкретной проверке/регрессии, Changelog — дописать итог или найти по ключу.

## Рабочий паттерн

[Как владельцу и агенту вести работу](Guidelines/DOCUMENTATION.md) · [Когда какой skill использовать](Roadmap/Skills.md) · [Соглашения](Conventions.md). Требование имеет одно каноническое место; история проверки не назначает контракт заново.

## По задаче

| Документ | Назначение | Когда читать |
|---|---|---|
| [Roadmap](Roadmap.md) | Текущий приоритет и следующий шаг | Начало задачи, смена/закрытие шага |
| [SPEC](SPEC.md) | Принятые игровые требования и реализованный объём | Меняется игровое поведение/ограничение |
| [GAME_CONCEPT](GAME_CONCEPT.md) | Продуктовая рамка и предложения демо | Новый игровой этап; не путать план с реализацией |
| [ARCHITECTURE](ARCHITECTURE.md) | Фактические общие границы и статус миграции | Зависимости, слои, composition и сборки |
| [Conventions](Conventions.md) | Пути, C#, данные, UI и Git | Новый/изменяемый код, ассеты, документы |
| [Features](Features/README.md) | Контракты систем, владельцы, состояние и команды | По изменяемой фиче |
| [Bugs](Bugs.md) | Воспроизводимые незакрытые дефекты | Пересечение задачи/регрессия |
| [Backlog](Backlog.md) | Технический долг и будущие системы | Границы шага и остаточные отклонения |
| [Questions](Questions.md) | Не принятые решения | Нужен игровой выбор; перед повторным вопросом |
| [ADR-001](Decisions/001-sod-architecture.md) | Принятие SOD и полного прохода | Архитектура/политика миграции |
| [Шаблон ADR](Decisions/000-template.md) / [фичи](Features/_template.md) | Единый формат нового решения/контракта | Только при их создании |
| [VALIDATION](VALIDATION.md) | Как выбирать/повторять проверки | Код/сцена/сериализация/сборка |
| [Reports/ValidationIndex](Reports/ValidationIndex.md) | Результаты прежних прогонов и их границы | Сверка доказательства/регрессия |
| [Changelog](Changelog.md) | Краткие итоги по датам | Запись итога; поиск по ключу |
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

[MVP_PLAN](MVP_PLAN.md) и [ENGINEERING_REVIEW](ENGINEERING_REVIEW.md) сохраняют прежние точки входа; очередь теперь только в Roadmap, активный долг в Backlog. Игровые ассеты и ручные сцены документированием не меняются.
