# Bite02 — игровое сравнение D/E/F

[Общий ролик D → E → F](DEF-native-gameplay.mp4), около23s. В правом верхнем углу буква текущей пробы; в каждом эпизоде четыре настоящих принятых попадания автономного Сталкера с интервалом≈2.04s, **ноль выстрелов**. [Только главный микс WAV](DEF-native-main-mix.wav), [manifest сравнения](comparison.json).

| Проба | Отдельное видео | Главный микс WAV | Peak | Clipped samples |
|---|---|---|---:|---:|
| D | [Видео](D/native-gameplay.mp4) | [WAV](D/native-main-mix.wav) | .499527 | 0 |
| E | [Видео](E/native-gameplay.mp4) | [WAV](E/native-main-mix.wav) | .453885 | 0 |
| F | [Видео](F/native-gameplay.mp4) | [WAV](F/native-main-mix.wav) | .565741 | 0 |

Каждый материал пока один исходник, повторяемый на четырёх реальных ударах; это не готовые банки вариаций. Пробы из Concepts/Unused/Audio/PlayerHitTask009/Bite02/Exports назначались **только в memory-copy профиля**; production B, volume.8/pitch±.012 и world route сохранены. Обычные голоса/атаки врага не приглушались. Player временно обездвижен на исходном spawn со100HP, остальные враги отключены только в Play; после четвёртого попадания атаки остановлены с хвостом1.2s. Тайминги/урон/баланс не редактировались.

Записаны native AudioRenderer float32 и GameView1280×720. Индивидуальные WAV/видео без нормализации или подмены аудио. В общем видео добавлены только буквы D/E/F и склейка; звук взят из соответствующих PCM24 WAV с прежним gain, AAC encode. На границе concat может дополнить менее одного кадра тишины. Raw PNG/mix.f32/capture.json сохранены в project-root work/task009-player-hit/capture-{D,E,F}-hits-only-live/.

До записи прочитан свежий TASK008 manifest и исправленный PlayerHitAudioPresenter с lifetime parenting (BUG063), прежний MoveGameObjectToScene не восстановлен. После записи165 текущих imported inputs без изменений; все25 SHA TASK008 и2 expectedAbsent paths совпали. [Сохранность](preservation.json), [TASK008 SHA](task008-preservation.json), [Editor после Stop](editor-stopped.json), [консоль](console-after.json). Production-настройки восстановлены, временных audition clips и runtime presenters после Stop0; одна чистая MechanicsSandbox, entry=null, time1/capture0/listener1/pausefalse, новых warnings/errors0.

Нового runtime/asset изменения, сборки или повторного suite в этом capture-only окне нет. Предыдущие1478 EditMode/117 PlayMode и Windows64 принадлежат TASK008. Итоговый контроль авторинга неприменим: авторские объекты не изменялись. Выбор материала и внедрение полного банка остаются отдельным следующим шагом после оценки владельца.
