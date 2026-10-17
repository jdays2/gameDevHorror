# Сталкер v09 — бледная плоть, рабочий набор

14 октября 2026. По запросу пользователя палитра стала серо-розовой, без костяной кремовой основы. Обновлены все активные состояния первого врага и останки; Unity использует этот набор.

## Файлы

- `walk-sheet.png`: 8 поз ходьбы.
- `recoil-sheet.png`: 4 позы реакции на свет.
- `attack-sheet.png`: 6 поз атаки; в Unity первая/последняя позиции используют Walk00.
- `death-sheet.png`: 4 позы гибели, источник 6 спрайтов кусков.
- `PaletteFrameLayout.json`: индивидуальные Rect, pivot и pixels-per-unit; активны 26 разных Sprite-ассетов тела/фрагментов (ходьба 8, реакция 4, атака 4, смерть 4, куски 6). Первая/последняя позиции атаки повторяют Walk00.
- `AlphaValidation.json`: проверка прозрачности и неизменности RGB при очистке.
- `palette-review.png`: четыре позы на нейтральном фоне для осмотра. Каждая подогнана под своё окно; сравнение игровых размеров выполнять в Unity.
- `previous-frames/`: копия прежних Sprite-ассетов и .meta до замены; это архив вне Unity Assets, не импортировать туда вместе с текущими GUID.
- `previous-preview.html`: прежний просмотр до обновления.

Рабочие текстуры находятся в `Assets/EmergencyShift/Art/Enemies/Stalker/PaleFlesh`. Sprite-ассеты сохраняют GUID, поэтому ссылки живого врага, трупов и GoreBurst остаются целыми. Предыдущие исходные листы в Art/Enemies/Stalker сохранены. Ранние неиспользуемые кадры без corrected относятся к архивной бордовой версии.

2026-10-17: поверх живых кадров подключён [увеличенный глаз LargeEye01](../LargeEye01/README.md). Эти исходные листы и 26 Sprite-ассетов тела/фрагментов не перерисовывались; PaleFlesh остаётся основой тела. Прежний нарисованный глаз закрывается новым органом с веками, а на смерти этот слой скрывается.

## Фон и разрешённая очистка

Палитра создана встроенным imagegen. Повторная background-extraction оставила низкоальфовую подложку в ходьбе/атаке. Пользователь явно разрешил программную очистку через Python 14 октября 2026.

Очистка выделяет непрозрачные связанные силуэты, сохраняет антиалиасинг в пределах двух пикселей рядом с контуром и обнуляет альфа-канал вне них. Отдельные части/капли смерти сохраняются. RGB, размеры холста и координаты рисунка при очистке не меняются. Rect/pivot затем измерены заново. Это обработка готовой графики, без программной перерисовки существ.

## Останки и кровь

Цветовой множитель останков: (0.58, 0.55, 0.60), alpha=1. Живые SortingGroup=1, мёртвые=0; Z=-0.03 отделяет останки от пола/крови при равном порядке. Затемняются тело и куски; лужи остаются отдельными.

Лужа создаётся синхронно при переходе в Dead; её масштаб варьируется на 0.85–1.18 от базового, seed меняется, внутри меняются пятна/капли. Куски запускаются через 0.45 с, после первых кадров разрыва. Повторный урон не дублирует эффект.

## Полные промпты imagegen

### Перекраска ходьбы

```text
Use case: precise-object-edit. Input image 1 is the edit target, an existing production 2D sprite sheet. Change ONLY its palette and a small amount of localized blood staining. Preserve all anatomy, poses, centers, margins, silhouettes, dark contour geometry, transparent alpha, frame layout and the exact canvas dimensions. Do not reposition, scale, rotate, enlarge or redraw the creature. This must remain the SAME existing stalker and SAME animation frames.
The previous pale ivory attempt looked like exposed bone. The NEW palette must look like SOFT LIVING FLESH: pale desaturated dusty rose / cool pinkish mauve skin, predominantly light #C6B6BC and #D4BBC5, soft pink-gray highlights #E0CAD0, midtones #A6879B, shadows #796D86. Apply it to the triangular outer torso and the ENTIRE six limbs, not just the torso. Gently pinker joints. No maroon-purple outer skin, no yellow cream, beige, ivory, exposed skeleton, armor or white bony shaft appearance. Skin retains the existing smooth fleshy bulges and wrinkles. The pointed ends and mouth teeth should be pale COOL GRAY #C8C3C4 with subdued highlights, not warm yellow tusks; only these small tips may read as hard material.
Blood remains dark crimson #82182A, with restrained fresher red on the same THREE limb tips: upper LEFT tip, middle RIGHT tip, lower LEFT tip. Keep stains small and consistent across frames, leave most skin pale. In death poses preserve all wounds and blood as saturated reds.
ONE central dorsal eye and black pupil when open, six angular jointed legs, same small triangular body. No glow or emissive parts. Keep clean outlined 2D game sprite style, broad controlled shading, no noisy texture or painterly grunge, no CGI.
STRICT 90-degree overhead orthographic camera, every pose faces UP in the XY floor plane. No isometric, perspective, environment, writing, borders, grid lines or external shadow. Preserve true transparency INCLUDING spaces between legs.
Specific sheet invariants: Keep EXACTLY EIGHT walking poses in the existing FOUR columns by TWO rows, facing up. Preserve each pose's leg positions and eye center. No extra creatures.
```

### Попытка удаления подложки 1

```text
Use case: background-extraction. Input image 1 is the EDIT TARGET.
Remove ALL backdrop, blurred pink-gray haze, cloudy gradients, external shadow, stray magenta dots and colored edge contamination from this existing sprite sheet. Create actual fully transparent alpha outside the creature outlines, INCLUDING holes and every space BETWEEN limbs and BETWEEN separate poses. Transparent background means alpha ZERO there, not a painted black background or faint low-alpha haze. Remove dark shadows painted OUTSIDE the drawn outline. Clean antialiased outline edges, no colored fringe, no halo.
Preserve EXACTLY all actual creature pixels' illustration, pale dusty pink living-flesh palette, restrained lilac shadows, blood stains, eye/eyelid, mouths, all limb and tip anatomy, pose positions, sizes, frame spacing, layout and source canvas dimensions. Do not repaint, brighten, smooth the skin, change palette or rearrange anything. This is solely clean background removal for an existing 2D animation sprite atlas.
Do not draw any checkerboard, gray background, vignette, texture, labels, additional objects or fog. Keep original image dimensions and every creature's original placement.
This atlas is walk. Keep each pose whole; don't add detached pieces.
```

### Перекраска сжатия

```text
Use case: precise-object-edit. Input image 1 is the edit target, an existing production 2D sprite sheet. Change ONLY its palette and a small amount of localized blood staining. Preserve all anatomy, poses, centers, margins, silhouettes, dark contour geometry, transparent alpha, frame layout and the exact canvas dimensions. Do not reposition, scale, rotate, enlarge or redraw the creature. This must remain the SAME existing stalker and SAME animation frames.
The previous pale ivory attempt looked like exposed bone. The NEW palette must look like SOFT LIVING FLESH: pale desaturated dusty rose / cool pinkish mauve skin, predominantly light #C6B6BC and #D4BBC5, soft pink-gray highlights #E0CAD0, midtones #A6879B, shadows #796D86. Apply it to the triangular outer torso and the ENTIRE six limbs, not just the torso. Gently pinker joints. No maroon-purple outer skin, no yellow cream, beige, ivory, exposed skeleton, armor or white bony shaft appearance. Skin retains the existing smooth fleshy bulges and wrinkles. The pointed ends and mouth teeth should be pale COOL GRAY #C8C3C4 with subdued highlights, not warm yellow tusks; only these small tips may read as hard material.
Blood remains dark crimson #82182A, with restrained fresher red on the same THREE limb tips: upper LEFT tip, middle RIGHT tip, lower LEFT tip. Keep stains small and consistent across frames, leave most skin pale. In death poses preserve all wounds and blood as saturated reds.
ONE central dorsal eye and black pupil when open, six angular jointed legs, same small triangular body. No glow or emissive parts. Keep clean outlined 2D game sprite style, broad controlled shading, no noisy texture or painterly grunge, no CGI.
STRICT 90-degree overhead orthographic camera, every pose faces UP in the XY floor plane. No isometric, perspective, environment, writing, borders, grid lines or external shadow. Preserve true transparency INCLUDING spaces between legs.
Specific sheet invariants: Keep EXACTLY FOUR light-reaction poses in TWO columns by TWO rows: eye open, partly closed, nearly shut, fully sealed. Preserve the exact limb contraction and closed eyelid of each pose; DO NOT open closed eyes.
```

### Попытка удаления подложки 2

```text
Use case: background-extraction. Input image 1 is the EDIT TARGET.
Remove ALL backdrop, blurred pink-gray haze, cloudy gradients, external shadow, stray magenta dots and colored edge contamination from this existing sprite sheet. Create actual fully transparent alpha outside the creature outlines, INCLUDING holes and every space BETWEEN limbs and BETWEEN separate poses. Transparent background means alpha ZERO there, not a painted black background or faint low-alpha haze. Remove dark shadows painted OUTSIDE the drawn outline. Clean antialiased outline edges, no colored fringe, no halo.
Preserve EXACTLY all actual creature pixels' illustration, pale dusty pink living-flesh palette, restrained lilac shadows, blood stains, eye/eyelid, mouths, all limb and tip anatomy, pose positions, sizes, frame spacing, layout and source canvas dimensions. Do not repaint, brighten, smooth the skin, change palette or rearrange anything. This is solely clean background removal for an existing 2D animation sprite atlas.
Do not draw any checkerboard, gray background, vignette, texture, labels, additional objects or fog. Keep original image dimensions and every creature's original placement.
This atlas is recoil. Keep each pose whole; don't add detached pieces.
```

### Перекраска атаки

```text
Use case: precise-object-edit. Input image 1 is the edit target, an existing production 2D sprite sheet. Change ONLY its palette and a small amount of localized blood staining. Preserve all anatomy, poses, centers, margins, silhouettes, dark contour geometry, transparent alpha, frame layout and the exact canvas dimensions. Do not reposition, scale, rotate, enlarge or redraw the creature. This must remain the SAME existing stalker and SAME animation frames.
The previous pale ivory attempt looked like exposed bone. The NEW palette must look like SOFT LIVING FLESH: pale desaturated dusty rose / cool pinkish mauve skin, predominantly light #C6B6BC and #D4BBC5, soft pink-gray highlights #E0CAD0, midtones #A6879B, shadows #796D86. Apply it to the triangular outer torso and the ENTIRE six limbs, not just the torso. Gently pinker joints. No maroon-purple outer skin, no yellow cream, beige, ivory, exposed skeleton, armor or white bony shaft appearance. Skin retains the existing smooth fleshy bulges and wrinkles. The pointed ends and mouth teeth should be pale COOL GRAY #C8C3C4 with subdued highlights, not warm yellow tusks; only these small tips may read as hard material.
Blood remains dark crimson #82182A, with restrained fresher red on the same THREE limb tips: upper LEFT tip, middle RIGHT tip, lower LEFT tip. Keep stains small and consistent across frames, leave most skin pale. In death poses preserve all wounds and blood as saturated reds.
ONE central dorsal eye and black pupil when open, six angular jointed legs, same small triangular body. No glow or emissive parts. Keep clean outlined 2D game sprite style, broad controlled shading, no noisy texture or painterly grunge, no CGI.
STRICT 90-degree overhead orthographic camera, every pose faces UP in the XY floor plane. No isometric, perspective, environment, writing, borders, grid lines or external shadow. Preserve true transparency INCLUDING spaces between legs.
Specific sheet invariants: Keep EXACTLY SIX attack poses in THREE columns by TWO rows. The central eye stays separate from the forward feeding funnel. Preserve the existing mouth opening, teeth, recoil/windup and leg placement in every pose.
```

### Попытка удаления подложки 3

```text
Use case: background-extraction. Input image 1 is the EDIT TARGET.
Remove ALL backdrop, blurred pink-gray haze, cloudy gradients, external shadow, stray magenta dots and colored edge contamination from this existing sprite sheet. Create actual fully transparent alpha outside the creature outlines, INCLUDING holes and every space BETWEEN limbs and BETWEEN separate poses. Transparent background means alpha ZERO there, not a painted black background or faint low-alpha haze. Remove dark shadows painted OUTSIDE the drawn outline. Clean antialiased outline edges, no colored fringe, no halo.
Preserve EXACTLY all actual creature pixels' illustration, pale dusty pink living-flesh palette, restrained lilac shadows, blood stains, eye/eyelid, mouths, all limb and tip anatomy, pose positions, sizes, frame spacing, layout and source canvas dimensions. Do not repaint, brighten, smooth the skin, change palette or rearrange anything. This is solely clean background removal for an existing 2D animation sprite atlas.
Do not draw any checkerboard, gray background, vignette, texture, labels, additional objects or fog. Keep original image dimensions and every creature's original placement.
This atlas is attack. Keep each pose whole; don't add detached pieces.
```

### Перекраска гибели

```text
Use case: precise-object-edit. Input image 1 is the edit target, an existing production 2D sprite sheet. Change ONLY its palette and a small amount of localized blood staining. Preserve all anatomy, poses, centers, margins, silhouettes, dark contour geometry, transparent alpha, frame layout and the exact canvas dimensions. Do not reposition, scale, rotate, enlarge or redraw the creature. This must remain the SAME existing stalker and SAME animation frames.
The previous pale ivory attempt looked like exposed bone. The NEW palette must look like SOFT LIVING FLESH: pale desaturated dusty rose / cool pinkish mauve skin, predominantly light #C6B6BC and #D4BBC5, soft pink-gray highlights #E0CAD0, midtones #A6879B, shadows #796D86. Apply it to the triangular outer torso and the ENTIRE six limbs, not just the torso. Gently pinker joints. No maroon-purple outer skin, no yellow cream, beige, ivory, exposed skeleton, armor or white bony shaft appearance. Skin retains the existing smooth fleshy bulges and wrinkles. The pointed ends and mouth teeth should be pale COOL GRAY #C8C3C4 with subdued highlights, not warm yellow tusks; only these small tips may read as hard material.
Blood remains dark crimson #82182A, with restrained fresher red on the same THREE limb tips: upper LEFT tip, middle RIGHT tip, lower LEFT tip. Keep stains small and consistent across frames, leave most skin pale. In death poses preserve all wounds and blood as saturated reds.
ONE central dorsal eye and black pupil when open, six angular jointed legs, same small triangular body. No glow or emissive parts. Keep clean outlined 2D game sprite style, broad controlled shading, no noisy texture or painterly grunge, no CGI.
STRICT 90-degree overhead orthographic camera, every pose faces UP in the XY floor plane. No isometric, perspective, environment, writing, borders, grid lines or external shadow. Preserve true transparency INCLUDING spaces between legs.
Specific sheet invariants: Keep EXACTLY FOUR death poses in TWO columns by TWO rows: damaged body, ripped body, severed legs/body, final dismembered core. Keep ALL detached fragments in their exact positions, shape, blood and exposed deep-red interior. Closed eye stays closed. Do not recolor internal wounds/blood to pale skin. ALL severed legs' outer skin must match the new living flesh color.
```

### Попытка удаления подложки 4

```text
Use case: background-extraction. Input image 1 is the EDIT TARGET.
Remove ALL backdrop, blurred pink-gray haze, cloudy gradients, external shadow, stray magenta dots and colored edge contamination from this existing sprite sheet. Create actual fully transparent alpha outside the creature outlines, INCLUDING holes and every space BETWEEN limbs and BETWEEN separate poses. Transparent background means alpha ZERO there, not a painted black background or faint low-alpha haze. Remove dark shadows painted OUTSIDE the drawn outline. Clean antialiased outline edges, no colored fringe, no halo.
Preserve EXACTLY all actual creature pixels' illustration, pale dusty pink living-flesh palette, restrained lilac shadows, blood stains, eye/eyelid, mouths, all limb and tip anatomy, pose positions, sizes, frame spacing, layout and source canvas dimensions. Do not repaint, brighten, smooth the skin, change palette or rearrange anything. This is solely clean background removal for an existing 2D animation sprite atlas.
Do not draw any checkerboard, gray background, vignette, texture, labels, additional objects or fog. Keep original image dimensions and every creature's original placement.
This atlas is death. Keep all intentional detached flesh, blood droplets and severed limbs as foreground pieces; only remove halos behind them.
```

