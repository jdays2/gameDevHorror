# Native-свет и активация группы · NativeLight38

2026-10-22, после 0ff7c92. BUG-035 подтверждён двумя настоящими PlayMode отказами: отключённая вспышка оставляла native Light2D и спрайты без затухания; cleanup потолочной маски повторно включал уже отключённую лампу. Baseline native-light-before.xml: **0/2**.

ShotEffect закрывает все свои native каналы и playback marker при disable. Повторное enable не повторяет выстрел; явный Play сохраняет подготовку inactive fixtures, используемых оружейной регрессией. LocalLamp.ApplyPower учитывает enable владельца, сохраняя личный флаг питания. Длительности, клипы, спрайты, профиль и порядок cosmetic random draws не менялись.

FullHeightLightBlocker больше не содержит static mutable registry: членство принадлежит уровневому SceneRegistry. CeilingLightOcclusion получает его через DI и фильтрует сцену также у explicit blockers. Без контейнера standalone Editor/Prefab Mode использует собственную загруженную сцену и повторно используемые buffers; незагруженный prefab scene не перечисляется. Hash учитывает состав, Transform/size/offset/enable. Прежние пересечения прямоугольников, 512 лучей, 128² cookie и Destroy native ресурсов сохранены. Height/длину теней мебели не меняли.

EncounterActivation стал trigger/environment View: личный Activated хранится в чистом EncounterModel, Activate/Restore проходят команды и Controller. Акторы включаются только после успешной первой Activate; Restore меняет только флаг, rebind сохраняет его. Список и положение объектов, checkpoint JSON/ID прежние; волн/динамического спавнера нет.

**1179/1179 EditMode**, **69/69 PlayMode**, обычная **Windows64 success**. Проверены исходные disable/cleanup, повторный Play, scene registry/enable, standalone preview, личная one-shot модель, native activation/rebind и старые bool payload; прежние оружейные volley/inactive fixture сценарии также прошли. Собственная Player assembly только EmergencyShift.Runtime. [Манифест и хеши](../Validation/NativeLight38.json).

Сохранены 2002 защищённых ресурса и 60 исходных dirty файлов; добавлены только семь C# и девять meta. Сцены/prefab/art/shared assets не менялись. Тестировалась dirty копия с исходной паузой, не clean HEAD; сторонние правки исключены из коммита. LicensingClient warning нефатален.

Сбор текущих blocker-участников добавляет O(B) на лампу к прежнему geometry hash; ray/cookie rebuild остаётся по изменению. Редакторский fallback также проходит свою hierarchy. Это описание стоимости, не замер FPS; общий updater/пул и OPT-01 не добавлялись. Открыты legacy tuning/audio/player/native UI, tools/style, итоговая сериализация и авторская приёмка.
