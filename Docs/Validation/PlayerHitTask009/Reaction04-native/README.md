# Reaction04 · игровые пробы без стрельбы

[Общий ролик J → K → L](JKL-native-gameplay.mp4), около23s. Буква варианта видна сверху справа. В каждом четыре настоящих принятых попадания автономного Сталкера, **0 выстрелов**, интервалы≈2.02–2.06s. [Главный микс WAV](JKL-native-main-mix.wav), [manifest](comparison.json), [отдельные пробы](../../../../Concepts/Unused/Audio/PlayerHitTask009/Reaction04/README.md).

| Проба | Видео | Главный микс WAV | Длительность | Peak | Clipped samples |
|---|---|---|---:|---:|---:|
| J | [Видео](J/native-gameplay.mp4) | [WAV](J/native-main-mix.wav) |7.659s|.402955|0|
| K | [Видео](K/native-gameplay.mp4) | [WAV](K/native-main-mix.wav) |7.680s|.420485|0|
| L | [Видео](L/native-gameplay.mp4) | [WAV](L/native-main-mix.wav) |7.659s|.426576|0|

Native AudioRenderer float32 и GameView1280×720; WAV без нормализации или замены звука, видео AAC. В общем ролике только буквы и склейка с исходными PCM24 WAV. Обычные голоса/атаки врага сохранены. Каждый вариант — один исходник, повторяемый четыре раза с прежними volume.8/pitch±.012/world route; назначение только в memory-copy профиля. Player временно обездвижен на исходном spawn со100HP, остальные враги отключены только в Play. После четвёртого попадания атаки остановлены с хвостом1.2s; баланс и authored assets не менялись. Raw PNG/mix.f32/capture.json сохранены в `work/task009-player-hit/capture-{J,K,L}-hits-only-live/`.

[Сохранность](preservation.json):149 текущих imported inputs, включая сцену/профиль/ProjectContext, hash-identical. [До](editor-before.json) и [после Stop](editor-stopped.json): одна чистая MechanicsSandbox,736 authored objects, missing scripts0; scene snapshots равны. Entry=null/time1/capture0/listener1/pausefalse; временных audition clips/runtime presenters0. На просмотренном [кадре K](review-frame.png) подпись читается, HP98 и красная реакция видимы. Авторские объекты не добавлялись/перемещались; итоговый контроль авторинга неприменим.

[Console qualification](review.json): retained error counter вырос11→25, warnings147→147. Все14 новых записей — инфраструктура CLI `status --until-ready` при первом запуске Play:8 сообщений об отсутствии команды `editor status` и6 timeout4000ms. Прямой `editor_status` работал; для K/L этот status polling не повторялся. В новых retained entries нет gameplay exceptions, после Stop ground-truth console errors/warnings0. Полные [счётчики](console-after.json) и [записи](console-warnings.json) сохранены; общий счётчик ошибок не объявляется нулевым.

Capture/inspection scripts скомпилированы in memory без диагностики, все три capture assertions прошли. Production runtime/Assets не менялись; новой сборки и повторного test suite нет. Художественное качество/сходство с референсами слухом не проверено, выбор владельца остаётся открытым.
