# Экран смерти D

TASK-006 · [карточка](https://app.notion.com/p/40a8d6dc06b68181a6b1f162b37c9ab2). Реализация технически проверена 2026-11-03; направление D выбрано владельцем, оценка игрового результата остаётся ему. [Проверки](../Reports/DeathScreenD01.md), [источники](../../Concepts/Used/UI/DeathScreenD01/README.md).

## Назначение и контракт

После смерти появляется D: почти непрозрачный серо-зелёный игровой кадр, левая колонка «ВЫ ПОГИБЛИ», красный штрих, плоские кнопки повтора и выхода. Музыки и новых UI-звуков нет. Фонарь ради подложки не включается. HUD/прицел скрываются, курсор освобождается, игровой ввод захватывается сразу. Пауза закрывается вместе с её pending quit, музыкой и собственными арендами; диалог отменяется. Внешние захваты сохраняются.

Мир затемняется за 1.4 s; текст появляется с 0.7 s за 0.5 s. Время unscaled. Остановка через WorldMotionPause происходит после 1.4 s: существующая реакция врага TargetDead и закрытие тела Спящего успевают завершиться. Аудио плавно затухает независимо через [WorldAudio](WorldAudio.md), без нового AudioListener mute. Ввод меню разрешён после перехода и отпускания удержанного подтверждения.

Кнопка и R выполняют один Retry intent. На текущих пяти картах нет активного checkpoint-контроллера, поэтому повтор загружает текущую сцену заново через Single. Карты не получают новые сохранения. Для явно подключённой сцены с активным LevelCheckpointController2D и resumeOnStart доступный слот предварительно проверяется на чтение, level/revision и world entities; затем полный reload запускает штатное восстановление. Повреждённый/исчезнувший выбранный слот показывает ошибку и сохраняет файл; скрытого чистого запуска нет. Ошибка автоматического restore при старте также закрывает доступ к игре до повтора/выхода. Формат snapshot не меняется.

Loading отвергает повторный intent. Перед переходом .35 s чёрного fade; команда ждёт отдельный tick и отпускание подтверждения. Новый scope блокирует удержанное подтверждение при старте, чтобы R/клик не прошёл в игру. Ошибка оставляет действия «Попробовать снова»/«Выйти из игры». Выход требует подтверждения; по умолчанию выделено «Остаться». Esc отменяет только подтверждение выхода, мёртвого игрока не возобновляет. В Editor выход возвращает понятную ошибку; Application.Quit вызывается в Player.

## Реализация и зависимости

| Слой | Тип | Владелец |
|---|---|---|
| Данные | DeathScreenProfile → immutable DeathScreenSettings | Общий Transition.asset, отдельное состояние каждого scope |
| Model/Logic | DeathScreenModel, DeathScreenPhase | Состояние/альфы/ошибка одного экрана |
| Controller/команды | DeathScreenController, DeathScreenCommand/Intent | Проверка intent в Execute, таймеры и только свои IDisposable input/motion leases |
| Unity-адаптер | DeathScreenEnvironment | Игрок, закрытие окон, preflight и Single load, quit; не владеет мировым звуком |
| Представление | DeathScreenPresenter/View/Button/GameplayOverlay | Подписки, unscaled tick, EventSystem/focus, курсор/HUD и их восстановление |
| Composition | DeathScreenInstaller из LevelContext2D | Level scope, только при authored deathScreen; холодное связывание собственной сцены |

Runtime лежит в `Runtime/Features/DeathScreen`. Model/Controller не ищут объекты сцены и не зависят от MonoBehaviour. Существующий legacy R в PlayerLife2D допускается только при свободном GameplayInputBlock; в подключённых сценах его заменяет единый intent D. Эта граница позволяет менять представление/тайминги без изменения здоровья, AI или checkpoint-формата.

## Авторинг и данные

`Prefabs/UI/DeathScreenUI.prefab` — отдельный корень «Death Screen» в MechanicsSandbox, MorgueRouteTest, BasementPrototype, LevelTemplate, DoorCrushTest. В LevelContext назначен environment, в GameplayOverlay — собственные Canvas HUD и прицел. Новых checkpoint/objective-контроллеров нет. Prefab не содержит Camera, AudioListener или AudioSource. Fallback EventSystem активируется только при отсутствии действующего; после закрытия освобождается только собственный.

Canvas Overlay order500, Expand 1280×720; колонка сохраняется внутри центрального виртуального прямоугольника на ultrawide. Существующий Noto Sans Medium/SIL OFL1.1, без новых растров/генерации. World Veil rgba(4/255,6/255,6/255,.97); .97 проверено в реальном линейном рендере Unity. Времена в `Data/UI/DeathScreen/Transition.asset`: World Fade1.4, Content Delay.7, Content Fade.5, Exit Fade.35 s; конечные неотрицательные величины. Настройки не меняются в Play.

Обычный Play запускает открытую сцену, playModeStartScene=null. MainMenu остаётся отложенным, его каталог/Canvas не требуется. Рецепт InstallDeathScreenD не заменяет существующий prefab и отказывается от dirty-сцен. Проверка authoring читает и неактивные объекты; существующие размещения/ID/ссылки сохранены.

## Проверки и ограничения

22 целевых и 1445 всех EditMode, 2 Sleeper PlayMode; native переходы/внешние аренды/удержанный R, 30 сочетаний actual GameView размеров/состояний/текста125%, DSP и реальный checkpoint reload. Точные результаты и сборка — в [отчёте](../Reports/DeathScreenD01.md). Увеличение текста — тестовая runtime-проба, отдельная пользовательская настройка не добавлена. Кампания/динамические checkpoint ID не реализованы. Художественная и слуховая приёмка, полное прохождение и оценка производительности не заявлены.
