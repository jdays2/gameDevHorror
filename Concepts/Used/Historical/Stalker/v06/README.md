# Сталкер: атака и гибель

Источник — выбранный шестиногий ползун. Пасть отделена от центрального глаза: раскрывается из-под переднего края корпуса при атаке. Кровь на полу и разлетающиеся куски предполагаются отдельными игровыми эффектами, чтобы переиспользовать их для других существ.

Генерация встроенным imagegen. Листы требуют проверки масштаба, анатомии и нарезки до принятия в игру.

## attack

```text
Use case: precise-object-edit. Input image is the DESIGN REFERENCE of an approved SIX-LEGGED alien stalker; preserve its exact identity and art style. Small maroon triangular fleshy central body, ONE circular pale dorsal eye, SIX long dark-plum legs with one bent knee each and pale needle-like tips, three leg roots on each side. No normal spider abdomen, no humanoid features, no additional head, no different species.
Camera MUST remain strictly 90-degree directly overhead, all parts in the floor plane. Every living frame faces screen UP. Use the same line weight, same purple/maroon/bone palette, same clean compact shaded 2D game sprite rendering. No photorealism or painterly texture.
Create a genuine TRANSPARENT PNG animation sheet. No background, no visible grid lines, no captions or numbers, no drop shadows, no player, no room or weapon. Cells have generous transparent margins of at least 55 pixels. Every sprite and fragment must stay well INSIDE its own cell; no clipped tips or pieces from another frame. The central eye/body pivot sits at the precise geometric CENTER of every cell, and the body has identical scale and orientation across frames. Not a concept board.
Output layout: ideally 1536x1024, exactly THREE equal columns by TWO equal rows, SIX consecutive ATTACK animation frames in row-major order.
Biology: the eye is only a sensory organ. A separate feeding mouth is normally folded under the FRONT/UPPER edge of the central body, BETWEEN the front leg roots and ABOVE the dorsal eye. During attack this lip UNFOLDS FORWARD into a small round fleshy feeding funnel with a dark throat and FOUR short inward-pointing radial teeth. The eye remains visible behind the mouth, NOT inside the mouth. Mouth is clearly a tooth-lined opening, NOT a second eye, not a human jaw or a separate long head.
Frame 1: normal six-leg ready stance, mouth hidden as a short seam at the upper body edge, eye open.
Frame 2: crouched anticipation, front pair bends closer, four remaining legs brace, upper feeding lip starts unfolding.
Frame 3: strike, front pair reaches up/forward to grip prey; the feeding funnel opens clearly above the unchanged central eye. SAME six legs, same body pivot.
Frame 4: bite, front tips curl inward, mouth wide with dark throat and short radial teeth; rear and middle pairs brace. No prey shown.
Frame 5: release, lip folds back partially and front pair retracts.
Frame 6: recovery into the original ready stance, mouth hidden again and all six legs match frame 1.
Keep changes small and coherent; stable body scale, no body jumping, no detached limbs, no extra legs appearing, no iris changing size. It should read as pinning with front limbs then biting with a concealed underside mouth, while preserving the approved creature.
```

## death

```text
Use case: precise-object-edit. Input image is the DESIGN REFERENCE of an approved SIX-LEGGED alien stalker; preserve its exact identity and art style. Small maroon triangular fleshy central body, ONE circular pale dorsal eye, SIX long dark-plum legs with one bent knee each and pale needle-like tips, three leg roots on each side. No normal spider abdomen, no humanoid features, no additional head, no different species.
Camera MUST remain strictly 90-degree directly overhead, all parts in the floor plane. Every living frame faces screen UP. Use the same line weight, same purple/maroon/bone palette, same clean compact shaded 2D game sprite rendering. No photorealism or painterly texture.
Create a genuine TRANSPARENT PNG animation sheet. No background, no visible grid lines, no captions or numbers, no drop shadows, no player, no room or weapon. Cells have generous transparent margins of at least 55 pixels. Every sprite and fragment must stay well INSIDE its own cell; no clipped tips or pieces from another frame. The central eye/body pivot sits at the precise geometric CENTER of every cell, and the body has identical scale and orientation across frames. Not a concept board.
Output layout: ideally 1024x1024, exactly TWO equal columns by TWO equal rows, FOUR consecutive DEATH animation frames in row-major order.
Death is violent alien body rupture, readable in a tiny sprite. Blood pools and flying gore will be separate runtime effects; do NOT bake a giant blood puddle or a full radial splatter across these cells.
Frame 1 upper-left: wounded creature, all six legs still attached, dorsal eye CLOSED under a maroon lid, central body tense with a short red crack.
Frame 2 upper-right: central triangular mass splits open around the CLOSED eye organ, raw red interior shows, all SIX leg roots still identifiable, legs splayed sharply.
Frame 3 lower-left: body collapses into two major fleshy pieces near the same pivot; three severed proximal leg remnants close to the core, other three limbs pull apart toward the immediate perimeter of this same cell. Keep all fragments INSIDE the cell with 55px margin, no blood outside. No intact copy behind the fragments.
Frame 4 lower-right: final corpse is ONLY two low irregular maroon core remnants and the crushed CLOSED eye organ lying at the same pivot, a few tiny nearby scraps. No complete live creature, no six intact extended legs; detached legs will be spawned separately by the game. No visible open pupil and no huge baked blood pool.
Preserve the approved purple flesh and pale tips in the first two frames; later pieces must clearly derive from its anatomy. Simple large shapes, no microscopic gore texture. No human body parts. This is a sprite rupture animation, not a cinematic poster.
```


## Коррекция пропорций атаки

```text
Use case: precise-object-edit.
Image 1 is the ATTACK SHEET TO EDIT, six poses in a 3-column by 2-row grid. Image 2 is the WALK SHEET DESIGN/SCALE REFERENCE only.
Change only the oversized CENTRAL FLESHY BODY and EYE of each attack pose in image 1. Keep the six attack poses, same outline/color rendering, same SIX legs, their knee and tip positions, the grid layout and transparency. Preserve the concealed feeding mouth and its opening/closing sequence.
The central triangular torso in attack sheet is too wide compared with the approved walking creature. Reduce the WIDTH and HEIGHT of the central triangular torso and its dorsal eye by approximately 25% in all six poses, centered on the same eye pivot. The core should look small and triangular as in image 2; approximately 125 pixels wide per 512px cell, eye approximately 35 pixels wide. Keep the overall leg-span at approximately 400 pixels per cell. Extend/reconnect the inner leg segments cleanly to the smaller core. Do not shorten all legs or shrink the whole creature.
The feeding mouth remains a small ventral funnel unfolding at the upper/front body edge, above and distinct from the central eye. No separate humanoid head.
All six sprites must remain entirely inside their own cells with clean empty gaps between frames; no detached tips or neighboring fragments. Keep camera strictly overhead and creature facing UP. True transparent background, including gaps between legs. No text or borders. Preserve the style and identity; this is a proportion correction, not a redesign.
```
