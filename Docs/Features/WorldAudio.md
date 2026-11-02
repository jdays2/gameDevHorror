# Звук мира при смерти

BUG-053: смерть плавно уменьшает весь мировой звук до неслышимости, UI остаётся независимым. [Экран смерти D](DeathScreen.md) по решению TASK-006 работает **без музыки** и реализован отдельной UI-границей. Проверки аудиофичи — [DeathAudio53](../Reports/DeathAudio53.md), совместного перехода — [DeathScreenD01](../Reports/DeathScreenD01.md).

## Настройка

`Assets/EmergencyShift/Data/Audio/WorldDeathTransition.asset` — неизменяемый в Play профиль:

| Поле | Начальное значение | Назначение |
|---|---|---|
| Fade Out Seconds | 1.4 s | Переход от текущего gain к нулю после смерти |
| Restore Seconds | 0.35 s | Возврат к единице при восстановлении живого игрока |
| Curve | SmoothStep | Linear / SmoothStep / SmootherStep; кривая линейной амплитуды |
| World Output | WorldAudio / Master | Отдельный mixer мира |
| Attenuation Parameter | MyExposedParam | Native exposed Master Volume; при переименовании обновить ссылку |

Длительности конечные, не меньше .05 s. Эти начальные значения не означают авторской приёмки звучания. `WorldAudio.mixer` создан штатным Editor UI, с UnscaledTime; структура вручную не редактировалась. При gain=0 выход ограничен −80 dB, native mixer может приостановить неслышимый граф. WAV и authored уровни SFX сохранены.

## Владельцы

- `WorldAudioModel` — чистая C# огибающая игрока. `SetWorldAudioDefeatedCommand` проверяет внешний переход; повторное уведомление не перезапускает fade. Обратный переход начинается с текущего gain, без скачка/накопления множителей.
- `WorldAudioController` передаёт gain в `IWorldAudioGain`, время получает аргументом. `WorldAudioPresenter` слушает `PlayerLife2D.HealthChanged` и использует `Time.unscaledDeltaTime`. Restore живого здоровья запускает обратный переход. Disable/level Dispose снимают подписку и собственную аренду.
- `WorldAudioBus` живёт в world scope и владеет mixer attenuation. Он запоминает исходный mixer level; отдельные аренды уровней объединяются минимумом, поэтому один не снимает mute другого. Последняя аренда возвращает исходный уровень. `AudioListener.volume/pause`, `AudioSource.volume/mute` и SO не используются как временная переменная смерти.
- `WorldAudioInstaller` подключён в существующий `ProjectContext.prefab`. `LevelContext2D` создаёт только runtime-компонент представления на явном player. Новая карта с обычным LevelRig не требует нового authored scene-компонента. Изолированные EditMode-контейнеры могут намеренно не устанавливать presentation-фичу.

Native View маршрутизируют свои голоса через DI до первого проигрывания: PlayerAudio/Utility/Footsteps, Stalker/Sleeper/Scorpion, DoorAudio, DoorAccessFeedback, LocalLampAudio и LevelAudioDirector. Новый мировой звук вызывает `WorldAudioBus.Route` в собственном адаптере при композиции; вида в центральный список добавлять не требуется. Runtime prefab создаётся через существующий `LevelContext2D.CreateInstance`, его голоса сразу получают текущую мировую громкость. Произвольный AudioSource вне этого контракта автоматически мировым не считается.

HUD/UI и `PauseMenuMusic` в bus не направляются. Необязательная ссылка на сценового игрока у `PauseMenuMusic` синхронно останавливает оба голоса на смерти, включая хвост закрывающейся паузы. Открытие музыки для мёртвого отвергается. В отдельном MainMenu игрок отсутствует, прежнее поведение сохраняется.

Фоновые слои при смерти больше не ставятся на Pause только из-за timeScale=0; собственный death fade директора использует unscaled time. Внешняя AudioListener.pause сохраняется. Обычная пауза живого мира и диалог не изменены.

## TASK-006 и восстановление

Экран D владеет визуальным переходом, вводом, курсором и остановкой мира. Он деактивирует отдельный корень паузы; существующий OnDisable вызывает `PauseMenuController.Cancel` и освобождает **её** аренды. Аудиофича не закрывает окна и не освобождает чужие захваты. Неподвижный кадр использует действующий WorldMotionPause после 1.4 s перехода, без нового общего AudioListener mute; трека смерти нет. Совместная DSP-проверка на готовом D: world RMS/peak0/0, независимый временный UI probe RMS.009251; пробник удалён.

Состояние fade не входит в checkpoint. `PlayerCheckpoint2D.RestoreCheckpoint → RestoreHealth` восстанавливает аудио, новая сцена получает новый player scope. Save format и постоянные IDs не менялись.
