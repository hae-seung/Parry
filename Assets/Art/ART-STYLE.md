# Character art direction

Established 2026-10-28. Applies to subsequent player and monster art.

## Reference

Primary visual reference: `Characters/Player/Source/Hero-Spritesheet-v1.png`.
Keep this reference image attached when generating later character sheets. Text descriptions alone do not guarantee identical styling.

## Shared style

- Simple side-view pixel art for readable combat poses; prefer clear anticipation, contact and recovery silhouettes over detail.
- Compact mechanical characters, segmented armor and dark joints. Player carries one sword.
- Dark navy outlines, slate steel and warm off-white armor, cyan highlights, muted rust-red cloth accents.
- Small restrained palette, hard pixel clusters and simple shading. Avoid soft painterly shading, gradients and incompatible pixel densities.
- Right-facing source frames; consistent proportions, weapon length and lighting across animations.
- Monsters should use matching outlines, pixel scale, shading and material treatment, with distinct silhouettes. Enemy warning colors must remain distinguishable from player cyan effects.
- Future characters should use this sheet as a visual reference and match the chosen in-game pixel scale after it is validated. Exact logical resolution is not yet finalized.

## First player sheet

8 columns × 8 rows, ordered left to right, top to bottom:

1. Idle
2. Run
3. Jump / apex / fall / land (no aerial normal attack)
4. Ground combo strike 1
5. Ground combo strike 2
6. Ground combo strike 3
7. Parry
8. Dodge roll

Charge/dash skill, airborne rotation/plunge skill, hit reaction and death need supplementary animation sheets using the same character reference.

## Integration status

AI-generated source artwork, not yet sliced or connected to an Animator. Cell containment, pixel consistency, grounded baseline, pivots and loop continuity require inspection during animation integration. Some slash effects approach or cross nominal cell borders, so an equal-grid slice is not yet validated. Preserve this source and make corrected versions separately.

Generated with the built-in image generation tool. Full generation prompt: `Hero-Spritesheet-v1.prompt.txt` beside the source image.
