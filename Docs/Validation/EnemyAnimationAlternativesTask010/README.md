# TASK-010 · границы проверки

Исследование: [отчёт](../../Research/EnemyAnimationAlternativesTask010.md), [схемы и сравнение](../../../Concepts/Unused/Visual/EnemyAnimationAlternativesTask010/README.md).

- `references.json`: полный native Sprite rect/pivot/текстура двух контрольных рисунков, точное восстановление RGBA.
- `source-preservation.json`: 17 selected-input SHA; генератор проверяет совпадение, не меняет исходники.
- `numeric-checks.json`: 245 отсчётов условного манекена, длины/опоры/неподвижные контакты/пол/проекция root. Ни масса, ни skin, ни collision не симулировались.
- `rejected-attack-support-v1.json`: сохранённый провал опорного многоугольника. Не превращён задним числом в PASS.
- `rejected-attack-ground-v2.json`: сохранённый провал высоты колена при уже исправленных опорах.
- `sources.json`: первичные источники и способ их проверки; внешние примеры не объявляются проверкой на Сталкере.
- `verify_artifacts.py`, `artifact-checks.json`: целостность локальных ссылок, PNG/APNG, JS/data и выбранных входов; не browser/Unity тест.

Фактически осмотрены: исторический contact sheet Locomotion02, независимая схема скелета, оригинальный полный Sprite, статический обзор новых схем. Браузеров CUA нет (`iab unavailable`, inventory=[]), поэтому UI Play/Pause/select и реальное браузерное воспроизведение не проверены. Новые рисунки, реальный Blender mesh/rig/renderer, импорт/Play и художественная приёмка отсутствуют.

Итоговый контроль авторинга: неприменим — сцены и их объекты не менялись. TASK-007 и BUG-054 не закрываются результатами этого исследования.
