# Экран смерти D

TASK-006 · [карточка](https://app.notion.com/p/40a8d6dc06b68181a6b1f162b37c9ab2). По отзыву владельца 2026-11-04 исправлен [BUG-058](https://app.notion.com/p/40e8d6dc06b681a5a6e3f131cf7fea58): быстрый чёрный переход D02 технически проверен; оценка темпа/вида остаётся владельцу. [Текущие проверки](../Reports/DeathScreenD02.md), [исходное внедрение](../Reports/DeathScreenD01.md), [источники](../../Concepts/Used/UI/DeathScreenD01/README.md).

## Назначение и контракт

Смерть немедленно запускает сгущение чёрной тьмы от краёв к центру. После полного закрытия на чёрном фоне сразу появляется D: левая колонка «ВЫ ПОГИБЛИ», красный штрих, плоские кнопки повтора и выхода. Музыки и новых UI-звуков нет. Фонарь ради подложки не включается. HUD/прицел скрываются, курсор освобождается, игровой ввод захватывается сразу. Пауза закрывается вместе с её pending quit, музыкой и собственными арендами; диалог отменяется. Внешние захваты сохраняются.

Чёрные края видны уже на первом кадре смерти; видимая область быстро сжимается к центру за0.32 s. Радиус меняется линейно, мягкая граница является пространственным краем маски. Текст/действия появляются сразу после полного чёрного, без fade-in; даже ранний Content Delay не может показать их раньше закрытия. Время unscaled. Остановка мира отделена от визуала и сохраняет прежние1.4 s через WorldMotionPause, чтобы не обрывать TargetDead/закрытие тела Спящего. Этот таймер не сбрасывается при Ready/подтверждении выхода. Аудио плавно затухает независимо через [WorldAudio](WorldAudio.md), без нового AudioListener mute. Ввод меню разрешён после закрытия и отпускания удержанного подтверждения.

Кнопка и R выполняют один Retry intent. На текущих пяти картах нет активного checkpoint-контроллера, поэтому повтор загружает текущую сцену заново через Single. Карты не получают новые сохранения. Для явно подключённой сцены с активным LevelCheckpointController2D и resumeOnStart доступный слот предварительно проверяется на чтение, level/revision и world entities; затем полный reload запускает штатное восстановление. Повреждённый/исчезнувший выбранный слот показывает ошибку и сохраняет файл; скрытого чистого запуска нет. Ошибка автоматического restore при старте также закрывает доступ к игре до повтора/выхода. Формат snapshot не меняется.

Loading отвергает повторный intent. Перед переходом .35 s чёрного fade; команда ждёт отдельный tick и отпускание подтверждения. Новый scope блокирует удержанное подтверждение при старте, чтобы R/клик не прошёл в игру. Ошибка оставляет действия «Попробовать снова»/«Выйти из игры». Выход требует подтверждения; по умолчанию выделено «Остаться». Esc отменяет только подтверждение выхода, мёртвого игрока не возобновляет. В Editor выход возвращает понятную ошибку; Application.Quit вызывается в Player.

## Реализация и зависимости

| Слой | Тип | Владелец |
|---|---|---|
| Данные | DeathScreenProfile → immutable DeathScreenSettings | Общий Transition.asset, отдельное состояние каждого scope |
| Model/Logic | DeathScreenModel, DeathScreenPhase | Состояние/альфы/ошибка одного экрана |
| Controller/команды | DeathScreenController, DeathScreenCommand/Intent | Проверка intent в Execute, таймеры и только свои IDisposable input/motion leases |
| Unity-адаптер | DeathScreenEnvironment | Игрок, закрытие окон, preflight и Single load, quit; не владеет мировым звуком |
| Представление | DeathScreenPresenter/View/Iris/Button/GameplayOverlay | Подписки, unscaled tick, чёрная uGUI-геометрия, EventSystem/focus, курсор/HUD и их восстановление |
| Composition | DeathScreenInstaller из LevelContext2D | Level scope, только при authored deathScreen; холодное связывание собственной сцены |

Runtime лежит в `Runtime/Features/DeathScreen`. Model/Controller не ищут объекты сцены и не зависят от MonoBehaviour. Существующий legacy R в PlayerLife2D допускается только при свободном GameplayInputBlock; в подключённых сценах его заменяет единый intent D. Эта граница позволяет менять представление/тайминги без изменения здоровья, AI или checkpoint-формата.

## Авторинг и данные

`Prefabs/UI/DeathScreenUI.prefab` — отдельный корень «Death Screen» в MechanicsSandbox, MorgueRouteTest, BasementPrototype, LevelTemplate, DoorCrushTest. В LevelContext назначен environment, в GameplayOverlay — собственные Canvas HUD и прицел. Новых checkpoint/objective-контроллеров нет. Prefab не содержит Camera, AudioListener или AudioSource. Fallback EventSystem активируется только при отсутствии действующего; после закрытия освобождается только собственный.

Canvas Overlay order500, Expand1280×720; колонка сохраняется внутри центрального виртуального прямоугольника на ultrawide. Существующий Noto Sans Medium/SIL OFL1.1, без новых растров/генерации. World Veil использует DeathScreenIris/CanvasRenderer вместо полупрозрачного Image: сетка чёрных вершин с закрывающейся овальной апертурой, после закрытия — сплошной непрозрачный quad. Нового shader/material нет. В `Data/UI/DeathScreen/Transition.asset`: World Fade.32, Content Delay.32, Content Fade0, Exit Fade.35, World Pause Delay1.4 s; Iris Feather.18, безразмерная величина в(0,1]. Длительности конечные неотрицательные; полный вход =max(World Fade,Content Delay)+Content Fade. Настройки не меняются в Play.

Обычный Play запускает открытую сцену, playModeStartScene=null. MainMenu остаётся отложенным, его каталог/Canvas не требуется. Рецепт InstallDeathScreenD не заменяет существующий prefab и отказывается от dirty-сцен. Проверка authoring читает и неактивные объекты; существующие размещения/ID/ссылки сохранены.

## Проверки и ограничения

D02:30/30 EditMode,12 кадров настоящей смерти в GameView,817 точно чёрных world samples после закрытия; независимая белая подложка подтверждает рост чёрной области и921600/921600 чёрных пикселей на финале. Пройдены Single retry/защита от дубля и8 native UI/input случаев; ссылки/ID/иерархия проверены в пяти реальных сценах. Подробности — [D02](../Reports/DeathScreenD02.md). Прежние1445 EditMode/2 Sleeper PlayMode/30 layout/DSP/checkpoint прогоны относятся к [D01](../Reports/DeathScreenD01.md), новым прогоном они не объявляются. Увеличение текста было тестовой runtime-пробой, отдельная настройка доступности не добавлена. Кампания/динамические checkpoint ID не реализованы. Художественная и слуховая приёмка, полное прохождение и оценка производительности не заявлены.
