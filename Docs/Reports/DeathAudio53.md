# BUG-053 · мировой звук после смерти

2026-11-02. Исходный SHA `1fd99053`. Техническое исправление и проверки текущего сценария смерти; экран D и его визуальная интеграция остаются TASK-006. Экран смерти без музыки. Контракт — [WorldAudio](../Features/WorldAudio.md).

## Подтверждённая причина

В обычном Play MechanicsSandbox игрок поставлен рядом с авторенной Ward lamp — muted cyan; применён настоящий смертельный урон через PlayerLife2D. После смерти гул продолжал играть: RMS выходного микса на участке 2.8–3.9 s — **0.001887**, peak **0.005208**, включая timeScale=0. Предыдущий death fade охватывал только три атмосферных слоя, отдельные лампы/двери/голоса не управлялись смертью. У директора также была scaled-time ветка Pause, обрывавшая фон при прямом timeScale=0.

Добавлены отдельный native mixer мира, readonly профиль и чистая огибающая/команда/controller с player presentation и world-scoped gain leases. Мировые native View маршрутизируют собственные источники при DI; authored source.volume, общие SFX и пользовательский AudioListener.volume не меняются. Музыку открытой/закрывающейся паузы останавливает её собственный PauseMenuMusic. Чужой незавершённый PauseMenuPresenter не менялся.

## Что запускалось

- **56/56 EditMode**: 11 новых WorldAudioTests, 20 PauseMenu, 19 AudioLayerMix, 6 LevelComposition. Отдельный native mixer-тест перенесён из EditMode, где Unity не допускает SetFloat при редактировании snapshot, в **PlayMode: 1/1**. [Результаты](../Validation/DeathAudio53/).
- **Реальный AudioRenderer**, float32 stereo / 48 kHz, без нормализации/монтажа. В MechanicsSandbox маршрутизированы 93 мировых голоса, в MorgueRouteTest — 117; в каждой карте три независимых UI/music-голоса. Пропущенных мировых источников после итоговой сверки **0**. Шесть считывателей дверей обнаружены полным аудитом и подключены до финальной проверки.
- После fade при timeScale=0 RMS/peak мирового микса **0/0** в обеих картах. Новые события двери и разрыва runtime-созданного Сталкера не слышны. В морге реальный reader voice играет с volume=.32, итоговый микс остаётся нулевым. Затем независимый UI-probe даёт RMS **0.007909** при мировом gain=0. Это технический probe с существующим WAV, не новый UI/SFX-контент. [Анализ и хеши](../Validation/DeathAudio53/mix-analysis.json).
- Четыре native цикла: смерть при открытой паузе, при музыкальном хвосте закрытия, внешняя AudioListener.pause, фактический RestartRoom со сменой scope. Музыка останавливается синхронно, её новое открытие для мёртвого блокируется. В тесте закрытие окна выполняет существующий PauseMenuController.Cancel, как согласовано для будущего TASK-006. Пользовательская громкость .37 сохраняется; после возврата gain=1 и ненулевые samples. [Lifecycle](../Validation/DeathAudio53/lifecycle.json).
- Настоящий `PlayerCheckpoint2D` payload записан/прочитан из изолированного `work/bug053/player-payload.json` и восстановлен при timeScale=0. Временный participant/catalog/probe существуют только в Play; checkpoint-контроллер в песочницу не добавлялся, пользовательские слоты не использовались. Gain=1, source.volume=.25 сохранён, выходной peak **0.019071**. [Payload](../Validation/DeathAudio53/player-payload-restore.json).
- Настоящий перезапуск MorgueRouteTest уничтожил прежний scope; новый игрок жив, gain=1, listener pause=false/timeScale=1. [Unload](../Validation/DeathAudio53/morgue-unload.json).
- Обычная Windows64 текущей общей рабочей копии: [build.json](../Validation/DeathAudio53/build.json). Из проектных Player assemblies только EmergencyShift.Runtime; Editor/Development/Tests исключены. Сборка включает прежние локальные черновики рабочей копии, поэтому не выдаётся за проверку чистого опубликованного релиза. Build Settings и development-настройки не менялись.

## Сохранность и пределы

Сцены не сохранялись/регенерировались. GUID, IDs, положения, параметры, прочие dirty-файлы сверены по baseline; в общем LevelContext и AUDIO_GUIDE коммитятся только изменения этой задачи. ProjectContext получил один installer-компонент и ссылку; новые аудиоданные созданы через native Editor API/UI. Итоговый контроль авторинга: **неприменим** — authored объекты сцен не добавлялись/перемещались. Runtime-представление и диагностические probes не сохранены в сценах. [Сверка](../Validation/DeathAudio53/preservation.json).

Проверки — контролируемые переходы и DSP, не полное ручное прохождение и не субъективная слуховая/художественная приёмка. Время/кривая доступны владельцу для прослушивания и настройки. Новый экран TASK-006 ещё не внедрён; совместный визуальный переход проверяется при его реализации. MainMenu не открывался, playModeStartScene=null. Только локальный коммит, без push.

Сохранённые записи: [до](../Validation/DeathAudio53/before-world.wav), [после](../Validation/DeathAudio53/after-world.wav), [морг](../Validation/DeathAudio53/morgue-after-world.wav). `AgentScripts/VerifyDeathAudio53.cs` повторяет native сценарии, `AnalyzeDeathAudio53.cjs` проверяет/упаковывает неизменённые samples. Исходные .f32 оставлены в `work/bug053/raw-captures`, WAV содержит те же float32 samples.
