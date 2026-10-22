# Безопасный preflight authoring-команд · AuthoringSafety39

2026-10-22, после 5f8a191. BUG-036: AssignPlayerInput сохранял prefab раньше проверки dirty Basement; InspectChangesAt открывал/закрывал уже открытую сцену; hospital builder перезаписывал существующие prefabs до полной проверки recipes.

Два первых случая воспроизведены **только в disposable Unity-копии**: baseline **0/2**. Для первого тест подготовил rig без controls и открыл dirty Basement с несохранённым sentinel. Прежняя команда отказала, но успела заменить prefab bytes. Прежняя инспекция завершилась вместо раннего отказа; её finally закрывал target. Исходные ресурсы владельца не использовались как fixture.

AssignPlayerInput теперь до LoadPrefabContents/Save проверяет stopped/compiled state, закрытый target и существование сцены. InspectChangesAt отклоняет loaded target до OpenScene, закрывает только собственную disk copy и проверяет valid/loaded предыдущую active scene. Hospital builder до Directory/import/save проверяет полный массив recipes: IDs/дубли, PPU, размер canvas/bounds, collider kind, наличие art и отсутствие prefab/meta outputs. Высота из старого JSON не используется как runtime shadow rule.

После исправления **3/3 guard-сценария** прошли; существующие family prefabs и art metadata не меняются, dirty scene/sentinel остаются. Затем **1182/1182 EditMode =1179 production +3 временных guard cases**. AgentScripts намеренно находятся вне Unity assemblies; для этой проверки их точные копии и отдельный harness временно компилировались только в Editor/Test assembly копии. Harness проверяет свой абсолютный путь и восстанавливает rig bytes в finally. Сам harness и реальный recipe учтены хешами в [манифесте](../Validation/AuthoringSafety39.json).

Все Runtime C# совпадают с проверенным NativeLight38. Его 69 PlayMode/Windows64 — предыдущее свидетельство для неизменного runtime, **новая сборка этапа39 не выполнялась**. 2011 защищённых ресурсов и 60 исходных dirty файлов сохранены; новых production C#/meta/assets нет. Генераторы на исходном проекте не запускались, пользовательские сцены не сохранялись.

Это cold authoring/environment adapters, им не нужна фиктивная игровая Model. Ранние отказы не обещают полную транзакцию создания нового семейства при произвольной IO-ошибке. Остальные tools/style/config/player границы и итоговый проход ещё открыты.
