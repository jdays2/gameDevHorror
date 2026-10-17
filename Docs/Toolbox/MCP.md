# MCP и подключения

**[← Старт](../../README.md)** · [CLI](CLI.md) · [Skills](SKILLS.md) · [Настройка Unity MCP](../MCP_SETUP.md)

Снимок **17 октября 2026**, текущий чат. «Инструмент доступен» означает наличие вызываемой команды в сеансе, а не проверенную авторизацию или работоспособность внешнего сервиса. Для конкретного действия агент проверяет соединение по месту.

## Пользовательские MCP

Источник — пользовательский `config.toml`; секреты, переменные окружения и аргументы запуска сюда не копируются.

| Сервер | Найден в настройках | В этом чате |
|---|---|---|
| unityMCP | Да, текущая запись HTTP | Прямые Unity-инструменты не объявлены; прежняя проверка и локальный мост описаны в [MCP_SETUP](../MCP_SETUP.md) |
| context7 | Да | Доступны resolve_library_id и query_docs; запрос к сервису в этой задаче не выполнялся |
| node_repl | Да | JavaScript-инструменты объявлены; выполнение в этой задаче не проверялось |

Наличие настройки Unity MCP не доказывает соединение с открытым Editor. Подтверждение — фактическое чтение текущей сцены и консоли. Эта задача документации не управляла Unity.

## Инструменты, объявленные в сеансе

Подключения приложения приходят также через плагины и не обязаны находиться в пользовательском `config.toml`.

| Группа | Назначение | Число команд |
|---|---|---:|
| [artifact_session](MCP_TOOLS.md#artifact-session) | Редактирование таблиц и презентаций | 3 |
| [code_review](MCP_TOOLS.md#code-review) | Проверки CI pull request | 1 |
| [codex_app](MCP_TOOLS.md#codex-app) | Файлы, чаты, проекты и интерфейс Codex | 47 |
| [chatgpt_space](MCP_TOOLS.md#chatgpt-space) | Pages и Spaces | 35 |
| [codex_document_control](MCP_TOOLS.md#codex-document-control) | Подключённые сессии документов | 3 |
| [gmail](MCP_TOOLS.md#gmail) | Почта Gmail | 21 |
| [google_calendar](MCP_TOOLS.md#google-calendar) | Календарь Google | 15 |
| [google_contacts](MCP_TOOLS.md#google-contacts) | Контакты Google | 3 |
| [google_drive](MCP_TOOLS.md#google-drive) | Drive, Docs, Sheets и Slides | 45 |
| [hotline](MCP_TOOLS.md#hotline) | Поиск местных линий помощи | 1 |
| [pets](MCP_TOOLS.md#pets) | Анимированные питомцы ChatGPT | 11 |
| [plugin_management](MCP_TOOLS.md#plugin-management) | Плагины и разрешения | 6 |
| [runway](MCP_TOOLS.md#runway) | Генерация изображений, видео, музыки и звука | 33 |
| [safety_settings](MCP_TOOLS.md#safety-settings) | Семейные настройки ChatGPT | 5 |
| [search_service](MCP_TOOLS.md#search-service) | Поиск в интернете | 1 |
| [sites](MCP_TOOLS.md#sites) | Создание и размещение сайтов | 25 |
| [context7](MCP_TOOLS.md#context7) | Документация библиотек | 2 |
| [node_repl](MCP_TOOLS.md#node-repl) | JavaScript-среда агента | 3 |

**[Открыть полный список команд →](MCP_TOOLS.md)**

Дополнительно в текущем окружении объявлены браузерная автоматизация `cua_repl`, генератор `image_gen`, web-поиск, shell и файловые операции. Они перечислены отдельно от пользовательских MCP, поскольку способ предоставления отличается.

## Обновление каталога

При изменении подключений агент сверяет имена серверов в настройках и фактические инструменты текущего сеанса, обновляет этот файл и полный список с новой датой. Не копирует токены/URL с подписью и не объявляет подключение исправным по одному наличию записи.

Навыки и MCP — разные части окружения: [официальное описание skills](https://developers.openai.com/plugins/concepts/skills).
