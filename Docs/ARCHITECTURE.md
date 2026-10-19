# Архитектура

## Принятый стандарт SOD и текущий код

С 2026-10-19 по поручению владельца для новых фич и миграций принят [sod-unity-architecture](../.agents/skills/sod-unity-architecture/SKILL.md): чистая C# Model, Controller/команды, View и отдельный Unity-мотор, неизменяемые SO-конфиги, стабильные ID, Zenject/Extenject и независимые EditMode-тесты. [Источник и разрешение конфликтов](Research/SOD_SKILLS_ADOPTION.md).

Это целевое правило, а не отчёт о выполненном рефакторинге. Ниже описана фактическая структура с существующими MonoBehaviour и Configure-проводкой. Zenject пока не установлен. При следующей реализации явно выделять границу переноса, сохранять ручной авторинг, GUID, игровые контракты и формат сохранений. Кампания/сеть и чужая политика паузы этим решением не добавляются.

## Границы

Основная песочница — [MechanicsSandbox](SANDBOX.md); отдельная маршрутная проба — [MorgueRouteTest](Guides/MORGUE_ROUTE_TEST.md). FirstShift и его профиль удалены; Campaign.asset пока пуст. Системы целей/чекпоинтов сохранены, но в песочнице отсутствуют их контроллеры/слоты.

Код разделён по назначению, настройки — по типу контента, сценические значения остаются на экземплярах. При переносе исходники/ресурсы перемещены с .meta, Unity GUID сохранены. Общее namespace EmergencyShift не мешает разделению файлов.

```text
Assets/EmergencyShift/
  Runtime/
    Core/Contracts/       урон, интеракции, защитный свет, факты для теней
    Definitions/          типы ScriptableObject
    Enemies/Common/      здоровье, восприятие, мотор, атака, патруль, навигация
    Enemies/Stalker/     решения вида, его звук/адаптеры
    Enemies/Sleeper/     сон/пробуждение/погоня/выпад, звук/адаптеры
    Player/              ввод, движение, прицел, оружие, состояние игрока
    World/               свет, двери, лут, поверхности
    Combat/Weapons/      пуля, инвентарь и индивидуальные циклы оружия
    Levels/              контекст, цели и жизненный цикл уровня
    Persistence/         ID, снимки, файловый слот, адаптеры чекпоинтов
    Presentation/        эффекты и аудиорежиссёр
    UI/                  отображение существующего Canvas
    Development/         временные проверки, исключаемые из release
    Sandbox/             управление тестом, временное HP, закреплённые цели
  Editor/                авторинг/инспекторы, отсутствуют в игроке
  Tests/EditMode/        независимые регрессии общих систем
  Data/                  Audio, Enemies, Weapons, Gore, Levels, Rendering
  Prefabs/               Player, Levels, Enemies, World, Loot, Combat
  Scenes/                авторенные тестовые сцены и ArtReview; включая MorgueRouteTest
  Art/ Audio/ Materials/ Shaders/
Concepts/Used/ Unused/    за пределами Assets, не импортируются
```

Сборки: Runtime, Development, Editor, EditModeTests. Runtime не зависит от Editor/Development. Development ограничена UNITY_EDITOR или UNITY_INCLUDE_INSTRUMENTATION; smoke запускается только в инструментированном development-игроке с явным аргументом. Служебные MonoBehaviour не перенесены в Editor-папку, где их временное добавление в Play могло бы сломаться.

## Карта фич

| Область | Фактический контракт |
|---|---|
| Враги и останки | [Features/Enemies](Features/Enemies.md) |
| Мир, фонарь, камера и интеракции | [Features/World](Features/World.md) |
| Уровень, цели и сохранение | [Features/Persistence](Features/Persistence.md) |
| Оружие | [Features/Weapons](Features/Weapons.md) |
| Диалоги | [Features/Dialogue](Features/Dialogue.md) |
| Песочница | [Features/Sandbox](Features/Sandbox.md) |
| Тени окружения | [Features/Lighting](Features/Lighting.md) |
| Игровой HUD | [Features/Hud](Features/Hud.md) |

## Статус приведения к SOD

Проверка и обновление всей собственной кодовой базы прямо поручены владельцем 2026-10-19. Этап активен в [Roadmap](Roadmap/00-ArchitectureMigration.md), а не отложен до неизвестной будущей задачи. Принятие правил не закрывает задачи; итог по файлам и проверки фиксируются в Reports. Целевые слои/DI — [ADR-001](Decisions/001-sod-architecture.md), пути и стиль — [Conventions](Conventions.md).

## Оставшиеся ограничения

StalkerAudio2D остаётся видовым адаптером. Баланс оружия теперь только в WeaponDefinition; ручные дублирующие damage/rate/range/ammo поля удалены после сравнения значений. ShotVisualProfile задаёт вспышку/попадание и ссылается на ProjectileVisualProfile для полёта. Пистолет и дробовик разделяют CommonProjectile, не второй источник урона.

Следующий вид ещё надо проверить реальным контентом. Есть небольшой инвентарь оружия; нет общего инвентаря предметов, автоматического переноса между уровнями, динамического спавнера с сохранением новых runtime-ID и пула actor. Сохранение поддерживает авторенные объекты данной сцены. Изменив контракт/библиотеку частей, увеличивайте saveRevision и явно начинайте новую попытку либо пишите мигратор.

Локальный путь/раздвигание не рассчитаны на тысячи агентов. Сравнительный native-замер10/20 выполнен; большой пул/ECS и центральный Update не вводились; DI выбран целевым стандартом и ещё требует внедрения. Постоянные следы не удаляются ради цифр. [Аудит](ENGINEERING_REVIEW.md), [границы измерений](OPTIMIZATION_BACKLOG.md).

## Совместимость прежних ссылок

### Сталкер и повторное использование

Перенесено в [Features/Enemies](Features/Enemies.md).

### Три вида данных

Перенесено в [Features/Enemies](Features/Enemies.md).

### Останки и профили

Перенесено в [Features/Enemies](Features/Enemies.md).

### Остальные связи

Перенесено в [Features/World](Features/World.md).

### Уровень и сохранение

Перенесено в [Features/Persistence](Features/Persistence.md).

### Оружие: данные, наследование и композиция

Перенесено в [Features/Weapons](Features/Weapons.md).

### Пробный диалог

Перенесено в [Features/Dialogue](Features/Dialogue.md).

### Песочница

Перенесено в [Features/Sandbox](Features/Sandbox.md).

### Контуры теней окружения

Перенесено в [Features/Lighting](Features/Lighting.md).

### Игровой HUD

Перенесено в [Features/Hud](Features/Hud.md).
