# Аудиоисточники уровня

Дата: 2026-10-22. База `eedd61b`. Статический AudioThreatEmitter2D.Active заменён SceneRegistry своего LevelContext. Унаследованная Zenject-инъекция подключает адаптеры обоих видов; enable/disable регистрируют и удаляют источник, повторный bind не дублирует его. Disable также очищает личные heard/urgent deadlines как прежде. Director перебирает только свой реестр и сохраняет дополнительную проверку Scene/Target; development smoke читает диагностический RegisteredThreatCount.

**905/905 EditMode, 48/48 PlayMode**, включая две новые native-проверки: наследуемая инъекция/повторный bind/disable/re-enable и выгрузка сцены при TimeScale=0 с сохранением соседнего источника. Прежняя полная аудиопроба прошла. Во время компиляции обнаружен оставшийся consumer Active в DevelopmentBuildSmoke; он обновлён до тестов. Обычная Windows64 success, собственная Player assembly только EmergencyShift.Runtime. [Хеши исходников, raw XML и 1794 неизменённых ресурса](../Validation/AudioThreatScopes23.json).

Одна membership collection на уровень и ссылка у источника; Register/Remove выполняются на lifecycle. Director использует indexed loop. Новых Update, голосов, клипов, физических запросов и коллекций на кадр нет. FPS и слуховая приёмка не заявляются. Копия содержит исходную незавершённую паузу, это не clean HEAD. Нефатальное сообщение LicensingClient остаётся.

Смена настроения/память угроз и таймеры пока принадлежат прежним MonoBehaviour; перенос этой логики и остальная MVC/config/style-работа открыты. Это проверка архитектурной границы, не заявление о новом найденном игровом баге: прежний director уже фильтровал чужую сцену. Partial-статусы этих файлов сохранены.
