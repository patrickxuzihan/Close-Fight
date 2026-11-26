# R5-01: Soviet rifle section

Date: 2026-11-26 (Australia/Melbourne)

## Deliverables

- unit_su_rifle.png — final 1536 × 1024, 8-bit RGBA PNG; no crop or resize.
- unit_su_rifle_raw.png — unchanged selected raw generation, before alpha cleanup.
- unit_su_rifle_preview.png — RGB comparison with de_rifle_4_raw.png, on olive RGB (153, 158, 117), at 76 × 54 and 228 × 162.
- unit_su_rifle_notes.md — this record.

Generated with the built-in image_gen tool using the German squad as the visual reference. A second image-tool pass adjusted the leader's PPSh stock and the rightmost rifle/bayonet to improve their framing. The final retains all four men and their distinct actions. Only this task's four deliverables were written in the project, all under ArtSource/codex/roster/squads/. The image tool also automatically retained its source generations in its own generated_images cache.

## Processing and validation

The generation contained a real alpha channel. Its hidden RGB background can appear in viewers that ignore alpha. For the final, alpha values at or below 12 were cleared, values at or above 245 made fully opaque, and intervening edge alpha linearly remapped. RGB under fully transparent pixels was zeroed. No geometry was changed and the final was not cropped.

The preview follows the shared guide: top is 2% of image height above the highest visible helmet (clamped to the image); bottom is the original bottom; width is cropped height × 76 / 54, centered on the visible group. Each crop is composited over the specified olive after Lanczos resampling.

Measurements:
- Final: 1536 × 1024, RGBA, 8-bit channels.
- Figure coverage (alpha ≥ 128): 78.73%.
- Alpha-weighted coverage: 78.73%.
- Bottom-edge figure coverage: 69.99%.
- Highest helmet top margin: 1 px (0.1%).
- Fully transparent pixels: 328804.
- Fully opaque pixels: 1229995.
- Raw SHA-256: b20851ec5e50a104fbf8f8c83ff4803e41fa850976c11726b6eae3c03c7bfed0.
- Preview crop rectangles (left, top, right, bottom): {"reference": [47, 0, 1488, 1024], "soviet": [37, 0, 1479, 1024]}.

## Visual self-check

- PASS: exactly four standing men, distinct faces/builds and poses; leader points with his left arm and holds PPSh in his right; gunner carries DP-27 over his right shoulder; third man leans forward with PPSh at hip; fourth holds the Mosin-Nagant at the ready.
- PASS: plain dusty khaki-brown Soviet clothing, rounded green helmets and diagonal rolled greatcoats; no insignia, stars, badges, medals, ribbons, armbands, flags, text or political symbols.
- PASS: two recognizable PPSh-41s with round drum magazines, DP-27 with flat top pan, and wooden-stocked Mosin-Nagant with fixed spike bayonet; no curved magazines. Weapons are present in the full final image and hand contacts were visually checked.
- PASS: close chest-height framing, helmets near top, knees/legs cut by bottom, no feet or prone figures.
- PASS: more than 70% frame coverage and more than one-third bottom-edge coverage.
- PASS: realistic detailed rendering, subdued color, soft upper-front light and tight composition match the reference.
- PASS: four separate helmeted figures and the rifle-section equipment read at 76 × 54 on olive; the preview also provides 3× views.

Limitations: at native tile size, fine mechanical weapon details and fingers are too small to verify; the large source was used for that inspection. The centered game crop trims a few pixels from the extreme left tip of the leader's wooden buttstock, without losing the PPSh silhouette or drum. No other requested feature remains unachieved. The general top-down sprite size, scale and pivot requirements do not apply to R5, per 00-squad-guide.md.

Refusals: none. No refusal request IDs.

## Prompt 1 — initial generation

Use case: historical-scene.
Asset type: transparent unit-roster illustration for Close Fight, Task R5-01 Soviet rifle section.
Input image 1 is the required German squad STYLE, CAMERA, FRAMING and COMPOSITION REFERENCE. Make its Soviet counterpart with exactly FOUR men, using the same realistic detailed 3D rendering, soft upper-front illumination, dusty cloth and aged leather textures, chest-height camera slightly from the front-left, and close shoulder-to-shoulder overlap. The men face the viewer and slightly to image-right. Use the reference's proportions and large faces, but replace the uniforms, weapons and poses with the Soviet descriptions below. Background must be genuinely transparent with alpha, not the reference's visible gray background.
Landscape 1536 x 1024. Very tight framing: highest helmet only 1–2% below top; figures fill both side edges and at least 70% of image area; legs cut by the bottom edge around the knees, no feet. Bodies and trousers continue through at least half of the bottom edge. Keep important weapon shapes, helmets and hands within the centered 76:54 game crop. Avoid extra headroom or surrounding empty space.
Exactly four DIFFERENT individual adult men with different ages, faces, builds and distinct actions, arranged LEFT TO RIGHT:
1. Section leader, sturdy middle-aged face, standing, pointing forward with his anatomically LEFT arm outstretched and slightly foreshortened toward image-right/front; his RIGHT hand holds one complete PPSh-41 low at his hip. PPSh has wood stock, perforated barrel shroud and ROUND DRUM magazine, barrel kept inside frame.
2. Machine gunner, tall younger man, standing, carrying a complete DP-27 over his anatomically RIGHT shoulder, gripping it securely. Its highly recognizable LARGE FLAT ROUND PAN MAGAZINE lies HORIZONTALLY ON TOP of the receiver and is conspicuously readable in silhouette. Correct DP-27 wood buttstock, long barrel and folded bipod; entire weapon fits within frame.
3. Stockier older soldier, standing but leaning forward, BOTH hands correctly supporting one complete PPSh-41 at his hip, with wooden stock and ROUND DRUM magazine, in a different pose from the leader. Barrel directed diagonally toward image-right with clear silhouette inside frame.
4. Slim young rifleman, standing alert, holding a complete wooden-stocked Mosin-Nagant at the ready diagonally upward across his body, right hand at the grip, left supporting fore-end. LONG STRAIGHT SPIKE BAYONET fixed on muzzle; rifle stock, barrel and bayonet all remain within frame.
Uniforms on ALL FOUR: plain khaki-brown 1942 Soviet tunics and trousers, rounded olive-green SSh-40 steel helmets, worn brown leather belts and pouches, a rolled grey-brown greatcoat worn DIAGONALLY OVER ONE SHOULDER across the body. No German helmet shapes or German equipment. Realistic muted khaki, olive and brown color palette. Fine dusty wear without obscuring silhouettes.
Absolutely NO insignia, stars, badges, eagles, medals, ribbons, armbands, flags, swastikas, symbols, text, labels, watermark, borders, ground, cast background shadows or haze. No curved magazines, no straight box magazines on PPSh, no extra weapons, no extra people, no lying figures. Soft self-shading on the figures is fine. Fully transparent background with clean cutout edges.

## Prompt 2 — targeted framing edit

Use case: precise-object-edit. This is the almost finished Soviet rifle squad roster illustration. Preserve EXACTLY the four faces, poses, anatomy, uniform colors, rounded olive SSh-40 helmets, diagonal rolled greatcoats, lighting, realistic 3D rendering, close shoulder-to-shoulder composition, head sizes, knee cutoffs, image size and TRANSPARENT ALPHA background.
Make ONLY two small weapon framing corrections:
1. On the leftmost leader, shift his held PPSh-41 and its supporting RIGHT hand slightly toward the center, so its ENTIRE wooden buttstock is visible with its leftmost tip at least 65 pixels inside the LEFT image edge. Keep his LEFT arm pointing exactly as it is, and keep the PPSh at his hip with its round drum magazine and complete barrel.
2. On the rightmost rifleman, adjust the rifle to a slightly more upright angle, gently repositioning his two hands to hold it naturally. Keep the ENTIRE Mosin-Nagant, including its wooden stock, barrel and full LONG SPIKE BAYONET, inside the image with the bayonet's rightmost tip at least 90 pixels inside the RIGHT edge, and its tip at least 40 pixels below the top edge. Retain its at-the-ready pose, right hand at grip and left hand supporting fore-end. Make the bayonet a thin straight spike.
All four people and their equipment otherwise stay unchanged; keep the DP-27 with its flat horizontal top pan magazine over the second man's right shoulder and the third man's drum-magazine PPSh at hip.
Output 1536 x 1024 RGBA with genuinely transparent background. No scene backdrop. No insignia, badges, stars, flags, symbols, text, labels, or watermark. Keep men filling at least 70 percent of frame, tallest helmets 1–2 percent from the top, and trousers running through the bottom edge.
