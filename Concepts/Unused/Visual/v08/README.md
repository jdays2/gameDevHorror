# Сталкер: пробная светлая палитра v08

Дата: 14 октября 2026. Пример перекраски первого врага по просьбе пользователя, создан встроенным imagegen. Это отдельный концепт для оценки палитры, не замена полного набора анимаций. Исходные PNG и рабочая сцена сохранены.

Файл: [stalker-pale-example.png](stalker-pale-example.png).

## Цвет и форма

- Сохранены треугольный корпус, один глаз, шесть суставчатых ног и вид строго сверху.
- Основной цвет: серо-костяной, светлая плоть; суставы приглушённо розоватые, тени холодные серо-фиолетовые.
- Концы ног цвета слоновой кости; локальная тёмно-красная кровь на трёх концах и рядом с суставами. Большая часть тела остаётся светлой.
- Самосвечение не предполагается. В игре такой материал должен читаться в луче, оставаясь скрытым в неосвещённом пространстве.
- Изображение на прозрачном фоне, одна поза. Это не лист анимации: для переноса палитры потребуются согласованные ходьба, сжатие, атака, гибель и фрагменты. Новую нарезку сверить с эталоном, не подставлять одиночную позу во все кадры.

## Полный промпт

```text
Use case: precise-object-edit.
Asset type: one standalone top-down palette test of an existing 2D game monster, not an animation sheet.
Input image 1 is the EDIT TARGET / exact existing creature design: a walking sprite sheet. Extract ONLY the UPPER LEFT creature as a single large complete sprite, preserving that exact pose and shape; do not recreate the whole sheet.
Primary request: recolor this existing stalker from maroon/plum to a pale sickly bone-flesh palette, with sparse blood stains. Keep the SAME nonhumanoid triangular small fleshy central body, SINGLE central dorsal circular eye with black pupil, EXACTLY SIX angular jointed legs, two segments and one bend per leg, pointed pale ends. Same thin limbs, knee positions, small triangular core proportions, leg roots, overhead pose facing UP. This is a color change, not a species redesign.
Palette: body and the whole length of all six legs predominantly LIGHT muted gray-bone #D2CFC2; muscles and joints softly desaturated pink-gray #BFA6A3; controlled shadow shapes cool gray-violet #777785; sharp leg ends ivory #E7DDC5. Retain visible dark slate contours and broad 2D shading. The creature should read as PALE overall, no plum legs or large red body areas. Eye rim pale gray-pink, one dark pupil, no glowing eyes or emissive parts.
Blood: a few localized dark crimson #780F20 stains with a small amount of fresh red on THREE sharp leg tips and the adjacent end segments, subtle stains near one knee; clearly distinguish blood as a surface smear on the light flesh. Leave most skin clean and pale. No blood puddles, no detached limbs, no blood splashes outside the silhouette, no human gore.
Composition: ONE creature only, centered, fills most of a square canvas with generous transparent margins around every complete pointed foot. True transparent exterior AND spaces between the legs. No backdrop, ground shadow, lighting vignette, text, title, UI, flashlight cone or additional figures.
Style must remain the reference's clean outlined hand-drawn 2D sprite illustration, limited 3-tone shading, readable at small scale. No painterly grunge, no glossy CGI, no photorealistic texture, no fur, no armor. Keep small central body and long thin angular legs exactly as reference; no extra features or teeth added to the eye.
CRITICAL camera: STRICT 90-degree overhead orthographic, dorsal view, anatomy entirely in the floor plane. No isometric or side view. Preserve the accepted monster silhouette and six-leg anatomy.
```

