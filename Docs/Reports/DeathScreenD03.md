# TASK-006 · BUG-058 · мгновенный экран смерти D03

2026-11-04. По последнему указанию владельца «убери затухание, пусть просто показывается сразу меню и все» отменена анимация входа D02. [Контракт](../Features/DeathScreen.md).

В существующем Transition.asset World Fade и Content Delay изменены с0.32 на0; Content Fade уже0. Поэтому в том же HealthChanged фактические Model/View дают ClosureProgress1, WorldOpacity1, ContentOpacity1 и видимое меню. Ввод сохраняет прежний барьер отпускания удержанной кнопки. Runtime C#, prefab, иерархия, сцены, их GUID/IDs и музыка не менялись; отдельные world pause/audio1.4 s и exit.35 s сохранены.

Проверка обычным Play на MechanicsSandbox: [native результат](../Validation/DeathScreenD03/native.json), [первый настоящий кадр](../Validation/DeathScreenD03/first-frame.png). При самой смерти, до следующего tick, View.IsVisible=true и реальные content/shade alpha1; в первом отрисованном кадре world samples справа точно RGB0. Проверен [Single retry](../Validation/DeathScreenD03/retry.json): старый scope уничтожен, новый живой, Hidden/timeScale1, дубликат отвергнут. Прежняя world pause завершается отдельно. [Профиль до/после](../Validation/DeathScreenD03/profile.json), [финальное состояние](../Validation/DeathScreenD03/final-state.json).

Новых тестов и сборки для обратимой правки двух SO-значений не запускалось; результаты D02 остаются [историей](DeathScreenD02.md). Временные проверки не добавляли объекты и подписки в сохранённые карты. Итоговый контроль авторинга: неприменим, объекты сцен не добавлялись/не перемещались. [Сохранность](../Validation/DeathScreenD03/source-preservation.json): карты/meta и чужой dirty state защищены входным снимком.

Короткое окно game получено явно у TASK-009 по согласованию менеджера; собственные git/files освобождаются перед явным возвратом ему. Editor возвращён на clean MechanicsSandbox, Play stopped, entry=null. Коммит локальный, без push. Художественная приёмка владельцем не объявляется.
