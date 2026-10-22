# Архитектура

## Принятый стандарт SOD и текущий код

С 2026-10-19 по поручению владельца для новых фич и миграций принят [sod-unity-architecture](../.agents/skills/sod-unity-architecture/SKILL.md): чистая C# Model, Controller/команды, View и отдельный Unity-мотор, неизменяемые SO-конфиги, стабильные ID, Zenject/Extenject и независимые EditMode-тесты. [Источник и разрешение конфликтов](Research/SOD_SKILLS_ADOPTION.md).

Extenject установлен по закреплённому commit официального репозитория. Выделены личные модели/команды/controller здоровья, диалогов, оружия, фонаря, доступа, обеих AI FSM, патруля/атаки/пути и [движения игрока](Features/PlayerMovement.md); [Input Actions и composition scopes](Features/PlayerInput.md) управляют зависимостями и сроками жизни. Точные границы каждой фичи — в [Features](Features/README.md). Технический проход текущих игровых границ, общего tuning и native lifetimes завершён: [RefactorCompletion42](Reports/RefactorCompletion42.md), [проверка опубликованного состава](Reports/FinalVerification44.md). CodeAudit явно различает629 committed C# и15 сохранённых draft sources, исторические исключения и авторскую приёмку. Существование MonoBehaviour само по себе не долг: Unity physics, input и presentation адаптеры остаются компонентами. При дальнейшей миграции сохранять ручной авторинг, GUID, игровые контракты и формат сохранений. Кампания/сеть и чужая политика паузы этим решением не добавляются.

## Границы

Основная песочница — [MechanicsSandbox](SANDBOX.md); отдельная маршрутная проба — [MorgueRouteTest](Guides/MORGUE_ROUTE_TEST.md). FirstShift и его профиль удалены; Campaign.asset пока пуст. Системы целей/чекпоинтов сохранены, но в песочнице отсутствуют их контроллеры/слоты.

Код разделён по назначению, настройки — по типу контента, сценические значения остаются на экземплярах. При переносе исходники/ресурсы перемещены с .meta, Unity GUID сохранены. Общее namespace EmergencyShift не мешает разделению файлов.

```text
Assets/EmergencyShift/
  Runtime/
    Core/Contracts/       урон, интеракции, защитный свет, факты для теней
    Features/            чистые модели/команды/controller и feature installers перенесённых систем
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
  Tests/PlayMode/        изолированные проверки Unity lifecycle/корутин
  Data/                  Audio, Enemies, Weapons, Gore, Levels, Rendering
  Prefabs/               Player, Levels, Enemies, World, Loot, Combat
  Scenes/                авторенные тестовые сцены и ArtReview; включая MorgueRouteTest
  Art/ Audio/ Materials/ Shaders/
Concepts/Used/ Unused/    за пределами Assets, не импортируются
```

Сборки: Runtime, Development, Editor, EditModeTests и PlayModeTests. Runtime не зависит от Editor/Development/Tests. Development ограничена UNITY_EDITOR или UNITY_INCLUDE_INSTRUMENTATION; smoke запускается только в инструментированном development-игроке с явным аргументом. Тестовые сборки отсутствуют в обычном Player.

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

Проверка и обновление всей собственной кодовой базы прямо поручены владельцем 2026-10-19. Технический результат и отдельная авторская приёмка отражены в [Notion](https://app.notion.com/p/3ff8d6dc06b681b6950bd8115222669c?pvs=204). Итог по файлам и проверенная ревизия фиксируются в Reports; само принятие правил не считается переносом. Целевые слои/DI — [ADR-001](Decisions/001-sod-architecture.md), пути и стиль — [Conventions](Conventions.md).

## Оставшиеся ограничения

StalkerAudio2D остаётся видовым адаптером. Баланс оружия теперь только в WeaponDefinition; ручные дублирующие damage/rate/range/ammo поля удалены после сравнения значений. ShotVisualProfile задаёт вспышку/попадание и ссылается на ProjectileVisualProfile для полёта. Пистолет и дробовик разделяют CommonProjectile, не второй источник урона.

Следующий вид ещё надо проверить реальным контентом. Есть небольшой инвентарь оружия; нет общего инвентаря предметов, автоматического переноса между уровнями, динамического спавнера с сохранением новых runtime-ID и пула actor. Сохранение поддерживает авторенные объекты данной сцены. Изменив контракт/библиотеку частей, увеличивайте saveRevision и явно начинайте новую попытку либо пишите мигратор.

Локальный путь/раздвигание не рассчитаны на тысячи агентов. Сравнительный native-замер10/20 выполнен; большой пул/ECS и центральный Update не вводились; DI связывает world/level/player lifetimes; дальнейшие фичи подключаются через эти scopes. Постоянные следы не удаляются ради цифр. [Аудит](https://app.notion.com/p/a1b30c6e0f8343168ea91ae5990ee9fb), [границы измерений](https://app.notion.com/p/3ff8d6dc06b68151a93cc3b27f535ab8?pvs=204).
