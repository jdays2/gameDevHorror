# TASK-009 · отклик принятого урона

**Текущее решение владельца: отложить задачу, на этом этапе звуки не добавлять.** Reaction04 отклонены как мужской стон: возраст и гендер героя не определены. Требуется глубокое исследование перед возвращением к задаче; новые генерации/интеграции прекращены. Ранее подключённый B отключён сохранённым Volume=0 в PlayerHitAudio.asset. [Проверка принятого урона с нулевым gain](../Validation/PlayerHitTask009/Deferred-off/README.md); остальные разделы — история, не актуальная приёмка.

2026-11-04. Первоначально выбранный **B — плоть и ткань** технически подключён, но затем отклонён владельцем по характеру звука. D/E/F Bite02 после игрового сравнения также отклонены: чужеродный ретро-характер; ассоциация следующего направления с укусом яблока неприемлема. Новый материал художественно не принят. [Контракт](../Features/PlayerHitAudio.md), [используемая семья и hashes](../../Concepts/Used/Audio/PlayerHit01/integration.json), [A/B/C и происхождение](../../Concepts/Unused/Audio/PlayerHitTask009/README.md).

## Предыдущий этап: референсы и пробы Reaction04

Последующее поручение владельца: подготовлены [Reaction04 J/K/L отдельно](../../Concepts/Unused/Audio/PlayerHitTask009/Reaction04/README.md) и [в игре без стрельбы](../Validation/PlayerHitTask009/Reaction04-native/README.md). Самостоятельное прослушивание YouTube и новых файлов недоступно, об этом сообщено до генерации; сходство с источниками слухом не объявляется проверенным. Заданные направления — короткая боль, сорванный выдох, реакция с телесным контактом. Три новых независимых источника, по одному на направление; это пробы, художественного выбора нет.

Отдельное сравнение12.4s: по два повтора J/K/L. Клипы.770/.600/.250s, уровни−25.22/−25.21/−25.19 LUFS, clipping0; только highpass65, silence trim/DC/fades/static gain, без компрессии/lowpass/pitch shift. Native ролик≈23s с буквами: по4 настоящих принятых попадания и0 выстрелов, интервалы≈2.02–2.06s; главный микс не нормализован/не подменён. Временные профили восстановлены, production B не заменён.149 imported inputs hash-identical;736 authored objects/scene snapshots равны, Editor stopped/clean, entrynull/time1/capture0/listener1/pausefalse, временных clips/presenters0.14 новых retained errors классифицированы как CLI status polling (8 unsupported editor status/6 timeout), gameplay exceptions нет; ground-truth после Stop errors/warnings0. Подробные evidence/пределы — по ссылке выше. Нового runtime/Assets изменения/сборки/suite нет; authoring gate неприменим.

По прямому поручению владельца подготовлена [подборка источников](../Research/PlayerDamageAudioReferencesTask009.md): целевой hurt-ролик Resident Evil 4 Remake, вокализации Айзека из Dead Space 2, официальный разбор дыхания Dead Space с 05:11. Subterrain и Alien Shooter 2 Reloaded из нашей концепции представлены записями без комментариев для общего микса; точные моменты урона в них пока не подтверждены. Заголовки/авторы/доступность ссылок проверены oEmbed, описание/главы — по индексированным карточкам; само звучание инструментами не прослушано. Выбор владельца не приписывается.

Ранее отправленные Organic03 G/H/I отложены без скачивания/монтажа результатов; [промпты и task IDs](../../Concepts/Unused/Audio/PlayerHitTask009/Organic03/provenance.json) сохранены. В исследовательском проходе нет новых генераций после смены направления, работы Unity, imported изменений или нового производственного банка. Задача остаётся в работе.

## Новые D/E/F в игровом эпизоде

По прямому поручению владельца записан [общий ролик D → E → F](../Validation/PlayerHitTask009/Bite02-native/DEF-native-gameplay.mp4), около23s. Вариант подписан буквой сверху справа. В каждом четыре настоящих попадания Сталкера, **ноль выстрелов**, интервалы≈2.04s; [индивидуальные видео/WAV и manifest](../Validation/PlayerHitTask009/Bite02-native/README.md). Native peak D/E/F .499527/.453885/.565741, clipping0. Нативный главный микс не нормализован и не подменён; только подписи и склейка общего видео. Просмотрен кадр общего видео в сегменте E: подпись читается, HP/реакция игрока присутствуют, оружейных сигналов нет.

Каждый материал пока один исходник, повторяемый четыре раза с действующим pitch±.012, volume.8 и world route. Подмена clips выполнялась на memory-copy профиля, не на production asset. Настройки актёров менялись только в Play fixture: player обездвижен/100HP, другие враги отключены, остановка атак после четвёртого удара и хвост1.2s. Это сравнение реакций, не художественная приёмка или установка нового банка.

Окно получено явно от TASK008 после его native-квалификации. Прочитан новый presenter lifetime-parenting fix BUG063; он сохранён.165 imported inputs hash-identical,25 SHA TASK008 и2 ожидаемо отсутствующих wrapper paths сохранены. После Stop: одна чистая MechanicsSandbox, entry=null, time1/capture0/listener1/pausefalse, временных clips/presenters0, новых warnings/errors0. В этом окне нет imported изменений/новой сборки/повторного suite; прежние1478 EditMode/117 PlayMode/Windows64 относятся к TASK008. Итоговый контроль авторинга неприменим.

## Материал B отклонён: новые пробы Bite02

После просмотра записи без выстрелов владелец отклонил сам характер B: слишком звонко, трение ткани/подошв и скольжение вместо повреждения тела при укусе. Техническое прохождение тестов и первоначальный выбор направления не равны приёмке этого банка.

[Общее прослушивание новых D/E/F](../../Concepts/Unused/Audio/PlayerHitTask009/Bite02/Previews/DEF_comparison.wav): по два повтора с паузами, без боевого микса. D — глухое сдавливание тела, E — влажное сжатие/укус, F — короткое повреждение/разрыв плоти. Три новых независимых источника; один на направление, без расширения в полный банк до выбора. [Параметры/происхождение](../../Concepts/Unused/Audio/PlayerHitTask009/Bite02/README.md). Источники не основаны на старом фрикционном материале; верх смягчён, короткие пики сжаты, уровни сопоставлены около−25.27 LUFS, peak≤.65, clipping0. Спектр и задания генерации не доказывают нужную физическую ассоциацию; оценка владельца открыта.

Это отдельная non-imported подготовка: Unity, Assets, gameplay и действующий микс не менялись; игровой доступ остаётся у TASK-008. Технические тесты/сборка ниже относятся к своим прежним ревизиям, не подтверждают художественную готовность. Материалы B и прежние записи сохранены как история отклонённого варианта.

## Исправленная демонстрация по отзыву владельца

Владелец отклонил первоначальный пример: синхронный выстрел при каждом попадании маскирует отклик урона. Это была ошибка сценария демонстрации; художественное одобрение по нему не получено.

**Основной пример для оценки:** [B без выстрелов](../Validation/PlayerHitTask009/B-hits-only-native/native-gameplay.mp4), [главный микс WAV](../Validation/PlayerHitTask009/B-hits-only-native/native-main-mix.wav). Четыре настоящих принятых удара, ноль выстрелов, интервалы2.042/2.041/2.040s; прозвучали все4 контакта B. Длительность7.595s, peak.461008, clipped samples0. [Проверка](../Validation/PlayerHitTask009/B-hits-only-native/review.json). [Отдельно только семейство B](../../Concepts/Unused/Audio/PlayerHitTask009/Previews/B_series.wav).

Обычный микс и атаки Сталкера сохранены. Только fixture: player обездвижен со100HP, другие враги отключены, после четвёртого попадания атаки прекращены с хвостом1.2s. Script больше не содержит TryFire или выдачи патронов. Нормализации/подмены аудио после записи нет. Свежий baseline713505a9; [149 импортированных inputs без изменений](../Validation/PlayerHitTask009/B-hits-only-native/preservation.json). [Editor](../Validation/PlayerHitTask009/B-hits-only-native/editor-stopped.json): Play остановлен, одна чистая MechanicsSandbox, entry=null, time1/capture0/listener1/pausefalse. Нового runtime/asset изменения нет; предыдущие технические test/build результаты остаются датированными свидетельствами. Итоговый контроль авторинга неприменим.

Прежние A/B/C с выстрелами ниже сохранены как технический контекстный стресс-пример, **не основной материал художественной оценки**.

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

Offline-подготовка сохранена07518fa6; интеграция сохраняется отдельным целевым commit. История остановки [PAUSED.md](../Validation/PlayerHitTask009/PAUSED.md) сохранена; пауза завершилась явным возобновлением владельцем. Материал B отклонён; новые пробы Bite02 ожидают выбора направления.
