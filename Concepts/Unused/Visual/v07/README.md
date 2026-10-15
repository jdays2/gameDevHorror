# Второй тип врага — концепты v07

Дата: 14 октября 2026. Только концепты для обсуждения; Unity, сцена и игровое поведение не изменены. Названия временные, выбор ещё не сделан.

## Требования пользователя

- Негуманоидное существо в той же визуальной семье, что и сталкер: бордовая плоть, тёмные контуры, костяные зубы, читаемые крупные формы.
- Строгий вид сверху. Без шерсти и обычных звериных лап; звериная угроза выражена глазами, пастью и зубами.
- Обычно спит, свёрнуто или закрыто. Попадание в свет фонаря вызывает агрессию: пробуждение → движение к игроку → атака.
- Для будущей временной версии атака не наносит урон, после неё враг исчезает. Это заглушка до появления оружия, не нынешняя реализация.
- Свет неприятен обоим видам: сталкер отступает, новый враг бросается на источник угрозы.
- Пока не создавать анимационные листы и не добавлять нового врага в игру.

## Вариант 1: Капкан

Файл: [trap-concept.png](trap-concept.png). Три состояния слева направо: сон → пробуждение → атака.

Широкое компактное ядро, две толстые мясистые створки-челюсти, четыре коротких крюка. Во сне створки сложены, зубы спрятаны в шве, глаза закрыты. После света створки расходятся и раскрывают большую пасть. Глаза отделены от рта.

Удобен для первого цикла анимации: силуэт меняется главным образом движением двух челюстей и коротких опор. До спрайтовой реализации выверить единое положение глаз, точное число зубов и размеры корпуса; это концепт, не готовые кадры.

## Вариант 2: Свёртень

Файл: [coil-concept.png](coil-concept.png). Три состояния слева направо: сон → пробуждение → атака.

Толстое сегментированное тело с клиновидной головой, двумя глазами и передней зубчатой пастью. Спит плотным клубком; при пробуждении разворачивается и сокращениями тела движется вперёд. Короткие костяные крюки служат опорами.

Более заметная смена силуэта, но разворачивание тела сложнее удержать без изменения объёма между спрайтами. Для анимации понадобится отдельно выверить ось тела и движение сегментов. Никаких поз с боковой/изометрической камерой в рабочем листе.

## Генерация

Использован встроенный imagegen, два независимых запроса. Фон непрозрачный, тёмно-серый: это презентационные концепт-листы. Промпты ниже сохранены полностью. Изображения не нарезаны и не импортированы в Unity.

### Промпт Свёртня

```text
Use case: stylized-concept.
Asset type: top-down 2D enemy concept board for an indie survival horror, ONE species in three states.
Primary request: design a new nonhumanoid alien predator that SLEEPS curled up until a flashlight touches it, then wakes angrily and charges. This is concept art only, not a production animation sheet.
Style: clean readable 2D hand-drawn game sprite illustration; dark consistent outlines, broad maroon flesh and plum colored folds, pale ivory teeth, three controlled shading tones per form. Match the restrained angular alien sprite language of a six-legged maroon triangular eyed stalker, but give this NEW enemy a very different elongated curled silhouette. Avoid painterly AI fantasy rendering, glossy 3D, busy microtexture.
Subject: a compact muscular alien with FOUR large fleshy body sections and a blunt wedge-shaped anterior head. Hairless, no mammalian paws, no conventional spider legs. It travels through contractions of its segmented underside, with only TWO pairs of short backward-facing bony anchoring hooks; hooks fold close to the body. TWO small pale dorsal eyes with black pupils, exactly the same eyes in all poses. A wide hinged feeding mouth at the forward edge of the head with two rows of large uneven triangular ivory teeth; mouth opens toward the top of the image in the plane of the floor, not facing the viewer. Its broad head and heavy jaws look predatory and disturbing. The eye is never also the mouth. Not cute, not a familiar snake, not a humanoid.
Composition: a wide landscape board, exactly THREE equal spacious columns on a flat charcoal gray background, no text, captions, arrows, UI or extra figures. ONLY three depictions of the SAME creature, left to right:
LEFT sleep: curled tightly into a kidney-shaped coil, anterior head tucked inward, both eyes sealed, jaw shut, recognizable folded fleshy sections and tucked hooks.
CENTER awakened by light: same body partially uncoiling, head faces UP, two eyes now open and narrowed, mouth partially open, pale sharp teeth visible.
RIGHT attack: same creature uncoiled into a broad low muscular crescent, head pointing UP, the exact same jaws spread for a forward bite, teeth clearly threatening, four short anchoring hooks supporting its forward contraction. Keep all body sections, hook count, palette and head size consistent across poses. No tentacles growing during attack.
CRITICAL camera: STRICT 90-degree overhead orthographic view in ALL THREE poses, viewing the creature's dorsal back, all anatomy spreads in the flat floor plane. Absolutely no isometric, side view, upright neck, eye-level face or three-quarter angle. Mouth opening is visible from overhead because its upper jaw plates splay in the floor plane. Same world scale across the three poses; do not enlarge head in the attack. Large clear complete silhouettes, no clipping or overlap. Uniform neutral sprite shading, no painted flashlight beams, no scenery or location theme. Keep anatomy simple enough to animate with a small number of sprites.
```

### Промпт Капкана

```text
Use case: stylized-concept.
Asset type: top-down 2D enemy concept board for an indie survival horror, ONE species in three states.
Primary request: second distinct concept for a dormant alien that becomes AGGRESSIVE when exposed to a flashlight. Show sleep, awakening, attack, not walking animation frames.
Style: clean readable outlined 2D game sprite illustration, maroon flesh, plum joints, pale ivory teeth, muted pale small eyes with black pupils. Dark crisp contour, controlled broad flat shaded shapes, three tones per part. Same visual family as a maroon triangular eyed stalker, but no spider silhouette. Not painterly fantasy, no glossy 3D, no mottled grunge, no photorealism, no fuzzy fur.
Subject: a broad, low, bilaterally symmetrical alien predator resembling a living muscular bear trap rather than a plant or a machine. An oval compact rear core with TWO thick hinged fleshy jaw lobes projecting forward. Each jaw lobe carries six large irregular inward-facing ivory teeth along the INNER edge; together the lobes form a single huge feeding mouth. Two small eyes set on the dorsal rear core BEHIND the jaw hinges, separate from the mouth. EXACTLY FOUR short, thick jointed hook limbs, two per side, tucked below the core during sleep; sharp bone points instead of furry feet or mammal paws. The whole creature is hairless and fleshy. An animal-like toothy predator with alien anatomy, not a humanoid, not a crab, not a spider, not a flower. Minimal parts support practical sprite animation: one core, two jaw lobes, four hooks.
Composition: wide landscape board with exactly THREE equal spacious columns, flat charcoal gray background, no writing, labels, arrows, UI, scenery, flashlight beams or players. Three complete depictions of ONE consistent creature at identical scale:
LEFT sleeping: lobes folded tightly together over the front and top of the core like closed fleshy shutters; teeth concealed inside their seam, eyes closed, four short hooks tucked against its sides. A compact dormant shape that could hide among clutter.
CENTER alarmed by light: front-facing UP, same pair of lobes part, small eyes open in angry squint behind hinges, hooks push outward, visible inner seam and a few teeth; no glow.
RIGHT charging attack: head direction UP, both jaws unfold outward into a U-shaped trap mouth in the plane of the floor; tooth rows face each other, dark central feeding cavity, tense core behind them, all FOUR short jointed hooks out bracing for a rush. Eye and limb counts remain exactly the same; mouth does not become a giant eye. Show controlled deformation rather than magically adding body mass.
CRITICAL: perfectly STRICT 90-degree overhead orthographic camera for ALL poses. See only the dorsal plan view; jaw lobes open sideways in XY floor plane so teeth can be seen from directly above. Absolutely no isometric camera, no frontal standing monster, no side-view face, no 3D perspective or upright vertical jaws. Same head/core dimensions across poses, clear spacing, complete tips with margins. Make it wild, threatening, and readable even when small; avoid excessive details that would be impossible to animate.
```

