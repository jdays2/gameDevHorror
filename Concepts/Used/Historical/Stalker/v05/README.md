# Шестиногий ползун — пробные спрайты v05

## Текущий результат — 14 октября 2026

В просмотр добавлен снимок `creature-shadow-preview.png` из настоящего Game View v0.4.4: два сталкера с чёткими контактными и короткими направленными тенями полного силуэта, включая шесть ног. Canvas-последовательности ниже остаются просмотром кадров/зрачка; тени работают отдельным компонентом в Unity. Первая версия сталкера принята пользователем 14 октября 2026 как ориентир следующих анимаций.

Просмотр обновлён до v09: четыре последовательности в серо-розовой палитре и очищенная прозрачность; новая раскладка в `../v09/PaletteFrameLayout.json`. Unity использует тот же набор. Видео v0.4 показывает прежнюю бордовую палитру. Полные новые промпты и очистка — `../v09/README.md`; текст о ранней генерации ниже исторический.

Выбранный сталкер уже импортирован и работает в Unity: восемь кадров ходьбы, четыре сжатия, шесть атаки и четыре смерти. Зрачок отдельно следит за игроком; при атаке раскрывается пасть, при гибели возникают независимые кровь и куски. Эталон — [ANIMATION_REFERENCE.md](../../EmergencyShift/Docs/ANIMATION_REFERENCE.md).

Исправление артефактов выполнено индивидуальными прямоугольниками/центрами в `preview.html` и Sprite-ассетах Unity. Равную сетку исходного листа ходьбы больше не использовать: один кончик пересекает её границу. Исходные PNG сохранены. Первый/последний кадры атаки используют настоящую готовую позу ходьбы; масштаб атаки/сжатия/смерти согласован.

Просмотр теперь содержит четыре последовательности и видео `stalker-demo-v04.mp4`: 40 секунд из Game View, 1280×720, 24 кадра/с. Осмотр/бой/гибель в записи временно подсвечены; движение и фонарь задаёт сценарий, AI/здоровье/кровь настоящие. Смерть врага вызвана тестовым уроном, оружия ещё нет. Остальной текст ниже сохраняет историю первоначальной генерации; утверждения «ещё не в Unity» относятся к ранней итерации.

Основа — выбранный пользователем угловатый ползун. Вид строго сверху. Предложены шесть ног, небольшой треугольный корпус и один глаз. Под светом существо сжимается и закрывает глаз; это реакция на свет, а не утверждение, что фонарь наносит урон здоровью.

Одна ориентация — вверх. Направление в игре можно задавать вращением спрайта. Движение и реакция на свет — отдельные последовательности. Изображения созданы встроенным imagegen; нарезка и анимированный просмотр — упаковка полученных кадров, без дорисовки. Это черновик для оценки, не принятый игровой ассет.

## Промпт движения

```text
Use case: precise-object-edit / game sprite design.
Input image is a DESIGN REFERENCE only, not a composition to preserve. Redesign the selected triangular alien stalker into a SIX-LEGGED version, then draw a clean animation sprite sheet. Preserve its small triangular fleshy core, dark plum/purple angular jointed legs and pale pointed leg tips. The central pale circular organ is now clearly ONE EYE with a dark pupil, viewed on the TOP surface of the body. Keep it alien, NOT a conventional spider: no separate head/thorax/round abdomen, no Earth spider anatomy.
Output: a transparent PNG sprite sheet, ideally 1536x1024, EXACTLY FOUR EQUAL COLUMNS by TWO EQUAL ROWS, eight evenly spaced sprites total. No visible grid lines, NO text, no labels, no background, no player, no floor, no shadows outside the creature, no flashlight. Each cell contains one COMPLETE full creature with ample clear margin; nothing crosses cell boundaries. Identical body size, identical central body pivot and framing in all eight cells. Central core sits at the EXACT geometric CENTER of every cell.
Camera: STRICT 90-degree directly overhead orthographic for every frame, with all six legs splayed in the floor plane. Never show standing legs under a side-view body, no isometric foreshortening, no front-view creature. Creature faces screen UP in every frame; do not rotate the body between frames.
Anatomy constraints: EXACTLY SIX LEGS in EVERY frame. Three leg roots down each side of the small triangular core: front pair points diagonally up and out, middle pair points sideways, rear pair diagonally down and out. Each leg has TWO broad slender segments, ONE main bent knee, ONE pale tapered terminal tip. NO extra small limbs, no fingers, no tentacles, no spikes. The same six identifiable limbs must persist through the entire animation.
Animation: eight consecutive frames of ONE SLOW CREEPING WALK LOOP, read left to right first row then left to right second row. Alternate two tripod groups: left-front/right-middle/left-rear against right-front/left-middle/right-rear. Subtle progressive bends at the same knees and modest forward/back toe shifts; not eight random poses. Body orientation, shape, eye, scale and pivot stay fixed. Small controlled motion across frames; frame eight naturally leads back into frame one. All tips visible.
Visual style: simplified finished-looking 2D game sprite illustration, crisp clear silhouette, restrained thick outline, large clean color shapes, 3 flat tones per part, barely any muscle lines. NO mottled painterly texture, no glossy realistic rendering, no grunge. Palette dark-plum legs, maroon small body, pale gray-pink eye rim, black pupil, bone pale tips. Uniform line weight and anatomy across frames. Transparent exterior INCLUDING spaces between legs.
This is a preliminary walking sprite sheet for previewing motion, not a multi-view concept board.
```

## Промпт реакции на свет

```text
Use case: precise-object-edit / game animation sprite sheet.
Input is the walking sprite sheet of the selected alien. Preserve EXACTLY its visual design: small maroon triangular central body, ONE central pale circular eye with dark pupil, SIX dark-plum legs, pale pointed tips. Use this same creature, not a new species.
Create a SEPARATE animation sequence of its REACTION TO FLASHLIGHT. Output a true transparent PNG, ideally 1024x1024, EXACTLY TWO EQUAL COLUMNS and TWO EQUAL ROWS, FOUR sprites total, row-major order. No grid lines, text, labels, background, scenery, shadows outside the body, player or light beam. ALL exterior pixels and spaces between legs transparent.
Camera STRICT 90-degree directly overhead, all legs in the floor plane, creature faces UP in all frames. Identical body center/pivot at exact geometric center of every square cell, same body scale and anatomy, generous margin.
Every frame has EXACTLY SIX identifiable legs, three roots per side of triangular body, each two segments and one bend, with the same pale tips as reference. No new limbs or rearranged anatomy.
Four progressive poses:
1 upper-left: initial walking/rest pose with all six legs moderately spread and central eye fully OPEN.
2 upper-right: early flinch, all six knees folding inward around the body, central eye HALF-CLOSED by a simple fleshy eyelid.
3 lower-left: stronger protective contraction, the six limbs tuck toward the perimeter of the core without hiding body shape completely; eye CLOSED as a narrow dark slit with a fleshy lid, NO visible pupil.
4 lower-right: fully compact protective huddle, same six legs tucked in a tight ring around triangular core, tips angled around rather than impaling the eye. Eye completely SEALED under a simple maroon closed lid. No visible iris or pupil.
The creature does not die, split, grow armor or get burned. It reacts to bright light by curling inward to hide its sensitive eye. Sprite sheet should support playing forward to close and backward to reopen.
Style: same clean outlined simplified 2D sprite rendering as input, broad controlled shapes, only 3 tones per part. No glossy CGI, no painterly texture, no added microscopic eye detail. Keep consistent art and anatomy across all four frames.
```

## Просмотр и файлы

- [Анимированный просмотр](preview.html): движение циклом; сжатие, удержание и раскрытие. Скорость 3–12 кадров/с, пауза и выбор кадра. Просмотр выравнивает кадры по измеренным центрам глаза; PNG не перерисованы.
- [Движение](walk-sheet.png): 1536 × 1024, сетка 4 × 2, ячейка 384 × 512. Порядок слева направо, сверху вниз.
- [Реакция на свет](light-reaction-sheet.png): фактический размер 1254 × 1254, четыре позы в раскладке 2 × 2. Генерация не сохранила идеально равные безопасные поля: в просмотре граница рядов поставлена на y=700, чтобы не обрезать ноги верхней позы. Прямоугольники и центры для просмотра сохранены в `preview.html`. При реализации состояние закрытого глаза удерживать отдельно от обратного раскрытия.

Проверено: файлы RGBA, наличие полностью прозрачных пикселей, восемь поз движения и четыре позы сжатия/закрытия глаза при визуальном просмотре. Это не проверка в Unity. Кадры имеют различия в деталях и форме ног; финальную анатомию, опору стоп и плавность нужно выверить вручную. Исходные листы отличаются масштабом; в просмотре реакция уменьшена до сопоставимого масштаба. Генератор не обеспечил единый центр кадров; для импорта понадобятся проверенные pivots/отступы. Unity-проект в этой итерации не изменялся.


Проверка просмотра в браузере: обе последовательности загрузились и отображаются, номера кадров меняются, в конечной позе реакции глаз закрыт. Локальный просмотр открыт в браузере Codex. Для повторного открытия без сервера можно открыть `preview.html` в обычном браузере рядом с обоими PNG.

## Слежение зрачка — уточнение пользователя

Когда существо крадётся, зрачок следит за игроком. В HTML-превью добавлена жёлтая метка игрока: она движется автоматически или следует за указателем внутри картинки. Зрачок рисуется кодом поверх глаза и смещается в направлении метки с ограничением радиусом радужки. При сжатии зрачок скрыт закрывающимся веком. Исходные PNG не перерисованы: перед игровым импортом нужно отделить исходный зрачок от рисунка глаза. В Unity это поведение ещё не реализовано.

Проверка обновлённого просмотра: визуально наблюдалось смещение зрачка к метке внизу и справа; в сжатой позе зрачок не отображается. Страница успешно загружается локально. Это проверка HTML-превью, не игрового поведения Unity.
