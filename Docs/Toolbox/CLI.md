# CLI и локальные утилиты

**[← Старт](../../README.md)** · [MCP](MCP.md) · [Skills](SKILLS.md)

Снимок **17 октября 2026**. Здесь команды, относящиеся к разработке проекта, и известные локальные среды. Это не перечень всех системных EXE Windows. PATH проверен через `Get-Command`; наличие файла и успешный запуск различаются.

## Найдены в PATH

| Команда | Назначение | Результат проверки |
|---|---|---|
| `git` | История, коммиты и push | Запуск подтверждён: 2.49.0.windows.1 |
| `codex` | Codex CLI | Версия получена: 0.163.0-alpha.10.2; при проверке были предупреждения доступа к временным PATH aliases |
| `node` | JavaScript и инструменты | Запуск подтверждён: v22.14.0 |
| `npm`, `npx` | Пакеты и команды Node | Найдены; запуск не проверялся |
| `uv`, `uvx` | Python-среды и запуск инструментов | Найдены; успешный запуск здесь не подтверждён |
| `rg` | Быстрый поиск по файлам | Использован для поиска файлов в этой задаче |
| `pwsh` | PowerShell | Текущая командная среда |
| `unity` | Командный launcher Unity | Найден в AppData; это не путь Editor проекта |
| `python`, `python3` | Windows App Execution Alias | Найдены в WindowsApps; рабочий Python этим не подтверждён |

## Проверенные пути вне PATH

| Инструмент | Где находится | Статус |
|---|---|---|
| Unity Editor 6000.6.5f1 | `C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Unity.exe` | Файл найден; Editor из этой задачи не запускался |
| Комплектный Python | `C:/Users/Mark/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe` | Успешный запуск: 3.12.14 |
| Python среды Unity MCP | `work/mcp-tools/unity-server/Scripts/python.exe` от корня рабочего каталога | Файл найден; версия и соединение с Editor не проверялись |
| Среда обработки контента | `work/creature-tools/Scripts` от корня рабочего каталога | Каталог найден; состав и команды проверяются по конкретной задаче |

Каталог `work` находится рядом с `outputs`, вне Git-репозитория игры. Пути локального компьютера не являются переносимой конфигурацией проекта.

## Не найдены в PATH

`gh`, `py`, `dotnet`, `ffmpeg`, `ffprobe`. Это результат поиска в PATH, а не доказательство отсутствия в локальных средах. Устанавливать их для работы с навигатором не требуется.

## Утилиты проекта

**План:** `Открыть план.cmd` запускает [локальный просмотрщик](../Guides/PLAN_VIEWER.md) с галочками; `Tools/PlanViewer/server.py` работает на стандартной библиотеке Python.

| Файл | Назначение |
|---|---|
| [prepare_creature_rupture_06.py](../../Tools/Audio/prepare_creature_rupture_06.py) | Подготовка набора звуков разрыва |
| [generate_stalker_membrane_v04.py](../../Tools/Audio/generate_stalker_membrane_v04.py) | Генерация мембранного звука |
| [stalker_membrane_dsp.py](../../Tools/Audio/stalker_membrane_dsp.py) | DSP-модуль мембраны |
| [prepare_stalker_tissue.py](../../Tools/Audio/prepare_stalker_tissue.py) | Подготовка звуков ткани |
| [prepare_firearm_mix.py](../../Tools/Audio/prepare_firearm_mix.py) | Парный микс оружия |
| [prepare_pistol_runtime.py](../../Tools/Audio/prepare_pistol_runtime.py) | Подготовка пистолетного звука |
| [prepare_shotgun_concepts.py](../../Tools/Audio/prepare_shotgun_concepts.py) | Пробы дробовика |
| [prepare_shotgun_gameplay.py](../../Tools/Audio/prepare_shotgun_gameplay.py) | Игровой набор дробовика |
| [prepare_world_foley.py](../../Tools/Audio/prepare_world_foley.py) | Звуки предметов и взаимодействий |
| [requirements.txt](../../Tools/Audio/requirements.txt) | Зависимости аудиоутилит |
| [Проверки Unity](../VALIDATION.md) | Актуальные точки входа и результаты тестов/сборок |
| [Настройка MCP](../MCP_SETUP.md) | Подключение редактора и границы исторических проверок |

Список аудиоутилит сверен с Git-каталогом Tools/Audio; обработка и перезапись звука в этой задаче не запускались. Аргументы конкретного скрипта проверяются по его исходнику перед применением.

## Как обновить список

В PowerShell: `Get-Command git,codex,node,npm,npx,uv,uvx,rg,pwsh,unity,python,python3,gh,py,dotnet,ffmpeg,ffprobe -ErrorAction SilentlyContinue`.

Агент обновляет дату, пути и результаты; наличие файла помечает отдельно от подтверждённого запуска. Команды сборки и изменения сцены выполняет только в рамках соответствующей задачи.
