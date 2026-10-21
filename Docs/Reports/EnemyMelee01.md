# Общая атака врагов · MVC/DI

Дата: 2026-10-21. База `6dc54a6`. Перенос общего melee-модуля обоих видов; их FSM, параметры, art и расстановка не менялись.

`EnemyMeleeModel` хранит личные struck/lunged и readyAt, читает IGameClock и immutable EnemyMeleeSettings. Begin/Finish/Interrupt/Restore проходят ICommand/CommandResult; собственный расчёт Step продвигает одноразовые флаги. У Model две зависимости (settings/clock), у Controller две (model/environment); нет MonoBehaviour, SO-ссылки, Time, Physics2D или видового enum в модели.

`EnemyMeleeEnvironment2D` разрешает Unity perception/motor/receiver. Controller сначала проверяет данные цели/дистанцию/дугу, затем LOS и Damageable; урон окончательно проверяется самим получателем. Pure observation хранит значения, не Transform/health component. Lunge/Resolved остаются событиями представления; они не авторизуют урон. Прежняя последовательность lunge → движение → strike сохранена, включая повторный вход обработчика без второго удара.

`EnemyMeleeAttack2D` сохраняет публичный API и Snapshot { struck, lunged, cooldown }, выступает адаптером событий/DTO. Configure проверяет новую конфигурацию до замены, сохраняет прежние флаги/cooldown и снимает старые handlers; Destroy освобождает свои handlers. Начальная доступность не настроенного модуля остаётся false. EnemyInstaller/factory подключены к LevelContext, модели личные для актёров.

`EnemyDefinition.ToMeleeSettings` копирует параметры и проверяет finite/ranges/order lungeStart ≤ strikeAt ≤ lungeEnd. Все существующие definitions дали корректные настройки без изменения assets. Заданные damage/distance/duration/speed/cooldown/arc не переписаны. Общая валидация остальных полей EnemyDefinition и приватизация legacy config API ещё остаются в проходе configs.

**572/572 EditMode**, включая 27 новых случаев: one-shot/events/reentry, контакты/дуга/преграда/смерть/защита, время/restore, экземпляры, malformed settings и authored definitions. **21/21 PlayMode**: новая проверка через настоящий LevelContext/MechanicsSandbox создаёт оба существующих prefab, отключает только их brain для явного управления timeline, перемещает игрока в изолированную зону и проверяет стену/однократный фактический урон. Это общий модуль на реальных компонентах, не полный игровой цикл двух FSM. Обычный Windows64 Player — [EnemyMelee01.json](../Validation/EnemyMelee01.json).

Первая новая unit-fixture ошибочно требовала точного совпадения float-квадрата порога. Она заменена проверкой точек непосредственно по обе стороны; рабочая формула squared-distance не менялась для подгонки. Начальный отказ сохранён как ошибка новой проверки, не прежний игровой баг. Проверяется копия dirty working tree с прежней принятой паузой; GUID/ресурсы сохранены, слоты игрока не используются. Авторская приёмка, полный маршрут, патруль/brains и FPS не заявляются.

Цена: личные model/controller/environment, immutable snapshot настроек при Configure; команды при переходах/restore, не каждый кадр. Step возвращает value-данные без нового Update и без отдельного таймера. LOS вызывается после геометрических условий, не чаще прежнего; движение использует тот же мотор. Это описание структуры/числа операций, не Profiler-замер.
