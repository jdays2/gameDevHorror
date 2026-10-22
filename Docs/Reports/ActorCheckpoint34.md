# Предварительная проверка акторов · ActorCheckpoint34

2026-10-22, после c80ce59. BUG-030 воспроизведён: **0/12 EditMode**. Stalker checkpoint пропускал age/exposure/closure/escape/melee/patrol/visualRotation до PrepareRestore; direct Player restore принимал NaN/Infinity/overflow направления; pristine Sleeper возвращался до проверки версии. Поздний отказ мог возникнуть уже после перемещения/восстановления других сущностей.

ActorCheckpointValidation проверяет raw value-only DTO существующими чистыми Stalker/Sleeper/melee/patrol validators, границами здоровья и unit quaternion. Новые eye values проверяются до legacy clamping. Player direction должен иметь конечные компоненты и квадрат длины, нулевой legacy direction допустим. Stalker/Sleeper checkpoint выполняют эту проверку в общей preflight; прямой Restore использует тот же путь. Native Stalker/Sleeper RestoreState также проверяют raw snapshot до health/model mutation. Sleeper Restore всегда проверяет envelope, включая pristine early return.

Legacy eyeBurned-only, известные состояния Сталкера, pristine initialized=false, player formats 1/2/3 и модули сохранены; DTO/ключи/format/saveRevision не менялись. Remains разрешены только умершему и проверяются до изменения мира. Адаптеры остаются сериализационной Unity/JSON границей; игровые правила состояния принадлежат прежним чистым моделям, а не новым дублирующим FSM.

После исправления **1129/1129 EditMode**, включая все исходные 12 сценариев с проверкой неизменности мира/инвентаря. **60/60 PlayMode**, обычная Windows64 success; собственная Player assembly только EmergencyShift.Runtime. [Baseline/XML/source/meta hashes](../Validation/ActorCheckpoint34.json).

1805 защищённых ресурсов и 60 исходных dirty файлов сохранены; новые meta импортированы exclusive create. Сцены/профили/prefab не менялись. Отдельная dirty копия включает исходную паузу, не clean HEAD; чужая работа исключена из коммита. Нефатальный LicensingClient warning остаётся.

Дополнительные проверки выполняются при загрузке/прямом восстановлении, без новых Update или кеша. Общая загрузка дважды проходит validation намеренно: целиком до PrepareRestore и на публичном direct Restore. Это не транзакционный rollback после произвольной ошибки применения; проверенные ошибки данных обнаруживаются заранее. Session paths/state, другой SOD scope и авторская приёмка остаются открыты.
