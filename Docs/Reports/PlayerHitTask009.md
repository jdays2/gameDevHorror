# TASK-009 · отклик принятого урона

2026-11-04. Владелец выбрал **B — плоть и ткань**. Четыре самостоятельно созданных коротких контакта подключены к существующему факту принятого урона игрока. Окончательная художественная оценка игрового микса остаётся владельцу. [Контракт](../Features/PlayerHitAudio.md), [используемая семья и hashes](../../Concepts/Used/Audio/PlayerHit01/integration.json), [A/B/C и происхождение](../../Concepts/Unused/Audio/PlayerHitTask009/README.md).

## Реализация

Новый PlayerHitAudioPresenter слушает PlayerLife2D.Damaged после принятой команды здоровья, включая последний удар. Он не наносит урон и не выбирает вид монстра. Общий профиль содержит четыре PCM16 mono48k WAV по.32s; volume.8, pitch±.012, overlap.018s. Два повторно используемых голоса направлены в WorldAudioBus; сумма source gain при overlap ограничена Volume. Выбор исключает предыдущий контакт, cosmetic RNG и история принадлежат игроку. Дополнительного death-клипа нет.

Installer добавлен нативно в существующий ProjectContext prefab; LevelContext создаёт и освобождает одно представление на явном player. HealthRestored передаёт существующий флаг restoration отдельно от HealthChanged: восстановление останавливает отклик, обычная смена ёмкости HP не вызывает звук и не обрезает реальное попадание. Disable/re-enable, listener pause и Dispose имеют явное владение. SO не хранит runtime-историю.

До интеграции живое принятое попадание снимало HP, у Damaged были только HUD/DirectionalDamage подписчики, отдельного player hurt источника не было. PlayerUtilityAudio2D/PlayerWorldAudioSet фактически содержат фонарь/сухой спуск/шаги; устаревшая строка AUDIO_GUIDE исправлена. Существующие успешные Bite Сталкера/Спящего и Grasp Скорпиона сохранены. UI, реакция модели, атаки, защита, баланс и авторские сцены не менялись.

## Прослушивание

В [Previews](../../Concepts/Unused/Audio/PlayerHitTask009/Previews/) сохранены A/B/C_single, series, context-offline и ABC_single-and-series.wav. A — сдержанный сухой контакт; B — плотный физический контакт без нового голоса; C — контакт с коротким выдохом. Рекомендация B дана до выбора владельца. Исходники, prompts, шесть generation task IDs и обработка сохранены; внешняя CC0-лицензия им не приписывается.

Базовые уровни −25.25/−25.21/−25.17 LUFS, четыре независимых B −25.21/−25.55/−25.05/−25.25 LUFS. Peak≤.68, highpass110/lowpass6500, мягкие границы, последние samples нулевые, DC<.00054. B2 отдельно сжат перед выравниванием. A/C используют по одному источнику и монтажные вариации. [Обработка](../../AgentScripts/Task009/prepare_audio.py), [измерения](../../Concepts/Unused/Audio/PlayerHitTask009/processing.json).

Offline-контексты используют одинаковые существующие пистолет/шаги/windup/lunge/**успешный Bite**, включая выстрел за40ms до попадания. Peak A/B/C .709/.857/.728; clipping0. Это монтаж, без native falloff/pan/микшера.

Отдельные **нативные игровые записи**: [A](../Validation/PlayerHitTask009/A-native/native-gameplay.mp4), [выбранный B](../Validation/PlayerHitTask009/B-native/native-gameplay.mp4), [C](../Validation/PlayerHitTask009/C-native/native-gameplay.mp4). Для каждого рядом native-main-mix.wav и mix-analysis.json. По три настоящих попадания автономного Сталкера и три обычных пистолетных выстрела; HUD, blood edge и tint записаны из GameView1280×720. На просмотренном кадре первого попадания B видны HP99 и красная реакция игрока.

| Набор | Длительность | Peak главного микса | RMS | Clipped samples |
|---|---:|---:|---:|---:|
| A | 6.016s | .422198 | .056149 | 0 |
| B | 6.037s | .427373 | .056447 | 0 |
| C | 6.165s | .410211 | .057761 | 0 |

Условия записи: игрок временно обездвижен на исходном spawn и имеет100HP, другие враги отключены только в Play. A/C используют memory-only клипы/копию профиля; сохранённый B не менялся. Это эквивалентные эпизоды, не идентичные samples по времени. Native float32 DSP-микс экспортирован без gain/нормализации или замены аудио; WAV PCM24, видео AAC. Длительность кадров выведена из захваченных samples. Старый 4K work/task009-player-hit/capture-B/ с ошибочным темпом не является доставляемым доказательством. Правильные raw записи находятся в **корне проекта** work/task009-player-hit/capture-{A,B,C}-live/.

## Проверки

| Проверка | Результат и свидетельство |
|---|---|
| Профиль, диапазоны, NaN/Infinity, пустые/повторные ссылки, readonly bank | 12/12 EditMode, [editmode-profile.json](../Validation/PlayerHitTask009/editmode-profile.json) |
| Прежние команды здоровья/защиты/смерти/восстановления | 12/12 EditMode, [editmode-player-life.json](../Validation/PlayerHitTask009/editmode-player-life.json) |
| Реальный accepted/отказ, restore/capacity, listener pause, disable/re-enable, Dispose, последний hit | 2/2 PlayMode, [playmode-lifetime.json](../Validation/PlayerHitTask009/playmode-lifetime.json) |
| Независимые world gain leases и baseline | 1/1 PlayMode, [playmode-world-bus.json](../Validation/PlayerHitTask009/playmode-world-bus.json) |
| ShadowStalker, Sleeper, Scorpion, сохранённый ClawPrototype | [native-melee.json](../Validation/PlayerHitTask009/native-melee.json): по7 принятых cues, два коллайдера; windup/miss/protection/repeated Step молчат. Частые удары без соседнего повтора, максимум2 источника. Почти одновременная пара даёт1 принятый cue; всего29. Реальные prefab/common melee delivery, brains отключены и cooldown snapshots сброшены только в Play fixtures; это не полный автономный AI-прогон. |
| DSP-разделение мира/UI | [native-transitions.json](../Validation/PlayerHitTask009/native-transitions.json): при world gain0 новый принятый cue учитывается один раз, мировой RMS/peak0. Независимый временный UI probe слышен, RMS.002315. |
| Смерть при открытой/закрывающейся музыке паузы, fade, две Single reload | Там же: lethal delta1, музыка останавливается синхронно; через1.7s голоса завершены/мир заглушён. AudioListener1/pausefalse сохранён. Старые player/источники уничтожены, после reload gain1, один presenter, два голоса и0 ложных cues. Проверена аудиограница SetMenuOpen и native SceneManager.Single; UI intent flow принадлежит TASK-006. |
| Обычная Windows64 | [release-build.json](../Validation/PlayerHitTask009/release-build.json): Succeeded,115.1s; профиль/все4 клипа включены; из собственных DLL только Runtime, Development/Editor/Tests отсутствуют. Четыре явно заданных уровня, без изменения Build Settings/autoentry. |

Сборка выполнена из общей рабочей копии с сохранёнными чужими изменениями, не из изолированного чистого commit. В отчёте2 Error logs — **тайм-ауты Pipeline HTTP main-thread запросов во время синхронной сборки**, не ошибки C#/игры. Единственное Warning сообщает отсутствие RuntimePipelineConfig: управляющий Pipeline не включён в обычный Player. [Точные сообщения](../Validation/PlayerHitTask009/release-build-messages.json). До сборки свежая консоль имела0 errors/0 warnings. Повторная сборка не запускалась; результат взят из завершённого native BuildReport. Проверена компиляция/включение ресурсов, самостоятельное прохождение Player и слуховая приёмка не заявляются.

## Сохранность

HEAD перед интеграцией0342baaf — мгновенный фон/меню TASK-006 D03. Его visual entry0 и отдельный WorldAudio fade1.4s сохранены. MainMenu вторым слоем не загружается, playModeStartScene=null.

[preservation-result.json](../Validation/PlayerHitTask009/preservation-result.json):147 защищённых импортированных dirty-файлов без неожиданных изменений. Общие четыре границы просмотрены отдельно; PauseMenu-строки LevelContext/AUDIO_GUIDE и музыкальная ссылка Concepts/README не принадлежат TASK-009 и не включаются в его commit. [native-preservation.json](../Validation/PlayerHitTask009/native-preservation.json):736 объектов MechanicsSandbox с прежними GlobalObjectID/родителями/именами/local transforms/activeSelf; добавленных/удалённых/изменённых0, missing scripts0. До/после96 авторских AudioSource. Сцена чистая, Play остановлен, timeScale1/captureFramerate0/listener1/pausefalse. Временные probes не сохранялись.

**Итоговый контроль авторинга: неприменим** — авторские объекты сцены не добавлялись/не перемещались/не переподчинялись. Сохранность фактической MechanicsSandbox отдельно сверена в Editor. Единственное prefab-подключение — installer на существующем ProjectContext с прежними ссылками/IDs.

Offline-подготовка сохранена07518fa6; интеграция сохраняется отдельным целевым commit. История остановки [PAUSED.md](../Validation/PlayerHitTask009/PAUSED.md) сохранена; пауза завершилась явным возобновлением владельцем. Оценка B в итоговом игровом миксе остаётся открытой.
