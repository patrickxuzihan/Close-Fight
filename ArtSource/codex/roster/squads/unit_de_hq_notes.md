# R5-02: German platoon headquarters

Date: 2026-11-26 (Australia/Melbourne)

## Deliverables

- unit_de_hq.png: final 1536 x 1024, 8-bit RGBA transparent roster portrait.
- unit_de_hq_raw.png: byte-for-byte original accepted generation, preserved before alpha cleanup.
- unit_de_hq_preview.png: RGB olive background (153, 158, 117), R4 reference and final each at 76 x 54 and 228 x 162 with the specified game crop.
- unit_de_hq_notes.md: this record.

Only these four task-owned files were written. Game code, Assets, documentation, shared notes, and other tasks' files were not modified.

## References and applicable rules

Read docs/art/codex/00-style-guide.md first, followed by roster/squads/02-de-hq.md and 00-squad-guide.md. R5's front-facing roster portrait rules supersede the general top-down sprite, scale, 256 x 256 canvas and pivot rules.

Image generation used ArtSource/codex/roster/squad_test/de_rifle_4_raw.png as the visual reference. The Soviet counterpart was absent when generation began, but appeared before final QA. It was inspected at ArtSource/codex/roster/squads/unit_su_hq.png: matching three-man standing arrangement, messenger left, map-reading leader center, binocular observer right, close knee crop and shoulder overlap. Each retains its own uniform and equipment.

## Method

Built-in image_gen tool, transparent_background=true, local R4 reference supplied. No CLI/API fallback.

The accepted generation already contained a usable alpha mask; its background color was retained in zero-alpha pixels and foreground alpha peaked at 254. Cleanup remapped alpha <=4 to zero and >=252 to 255, with a linear antialiased transition in between; RGB was zeroed only where resulting alpha was zero. No subject repainting, rescaling, cropping, repositioning or canvas changes were made to the final. Preview compositing and resizing used Pillow.

Preview crop: top is 2% of source height above highest subject/helmet, clamped at zero; bottom is source bottom; crop width is height x 76/54, centered on the alpha >127 subject bounds. Both highest points are helmet tops, so this follows the specified crop. Crop boxes (left, top, right, bottom): {'R4 reference': (47, 0, 1488, 1024), 'German headquarters': (44, 0, 1485, 1024)}.

## Refusal log

Attempt 1 was rejected by the image service: HTTP 400 moderation_blocked, output moderation category 'other', safety_violations=[abuse]. Request ID: 2e82ced5-b984-4a22-ae6b-619f4de222bf. No image was returned. Per the task guide, exactly one simpler retry was made; it succeeded. There was no second refusal. The successful tool response did not expose a request ID.

## Prompt 1 (refused)

Use case: historical-scene.
Create a single roster portrait for a WWII tactical game, German platoon headquarters, September 1942. Use the supplied German four-man rifle-team image as the STYLE AND CAMERA REFERENCE: match its realistic highly detailed 3D rendering, field-grey wool, worn steel helmets, muted palette, natural faces, soft even upper-front light and tightly packed knee-cropped composition. It is a reference, not an edit target; make a NEW THREE-MAN picture.

Landscape canvas 1536 x 1024. Exactly THREE adult men standing shoulder to shoulder, slightly overlapping, filling the width edge to edge. All three helmet crowns almost touch the top, within 1–3% margin; bottom border cuts across their knees with NO feet visible. Broad contiguous silhouettes, bodies occupy at least 70% of the image, legs run off bottom across at least half the width. Camera at chest height slightly from front left; bodies face viewer and a little right. No top-down or overhead view. Composition must remain legible as a tiny 76 x 54 game tile.

LEFT TO RIGHT:
1. Young slim messenger, Kar98k wooden-stock bolt-action rifle visibly slung upright on his outer shoulder with leather sling, its complete muzzle below the top edge and wooden butt within frame. His head turns toward the center leader, listening. One hand rests on sling, other lowered.
2. Middle-aged slightly sturdier platoon leader, head tilted down studying a large open folded pale paper map, holding its two side edges with both hands at waist level. A recognizable MP40 hangs diagonally across his upper chest by a sling, clearly visible above and to the side of the map, with a straight box magazine. The paper shows subtle unlabeled cartographic lines, no readable text.
3. Older lean observer with a different face, BOTH hands correctly holding a pair of black binoculars directly to his eyes, elbows raised, looking a little to viewer's right.

Uniforms: plain dusty field-grey wool tunics and trousers, plain grey-green M35 steel helmets with flared rims, leather belts and Y-straps, period ammunition pouches. Distinct faces, ages, builds and poses. Authentic restrained materials, no caricature.
Background: GENUINE FULLY TRANSPARENT ALPHA, no scene, no ground, no studio backdrop, no fake checkerboard, no cast shadows or floating haze.
Absolutely no insignia of any kind: no eagles, badges, medals, ribbons, stars, armbands, flags, political or national symbols, swastikas, text, logos or watermark. Plain collars and helmet sides. Only the specified Kar98k and straight-magazine MP40, no extra weapons, no curved magazines.
Match the reference's close framing and fine rendering while delivering exactly the three requested roles and a truly transparent cutout.

## Prompt 2 (accepted)

Create a realistic 3D-rendered historical game roster image of a three-man German platoon headquarters from 1942, using the supplied image only as a reference for rendering, lighting, clothing and camera angle.
Exactly three standing men, tightly shoulder to shoulder, with different faces and poses. Left: a messenger with a wooden-stock Kar98k slung on his shoulder, turning toward the leader to listen. Center: a leader studying an open paper map held in both hands, with an MP40 and its straight magazine slung visibly across his chest. Right: an observer holding binoculars to his eyes.
Plain worn field-grey wool uniforms, plain grey-green M35 helmets, leather belts, Y-straps and ammunition pouches. All clothing and equipment is unmarked, without insignia or emblems.
Soft even front light, realistic fine material detail matching the reference. Chest-height camera slightly from front left, men facing forward and slightly right. Landscape 1536 x 1024, helmets within 3% of the top, bodies filling the width and at least 70% of the frame, bottom edge cutting across their knees, no feet. Fully transparent background with clean alpha edges, no ground, no background scenery, no cast shadows, no text. Ensure the map, binoculars and three separate men read clearly at 76 x 54 pixels.

## Self-check

- PASS: exactly three distinct standing men in the requested left-to-right roles. Messenger turns toward leader with Kar98k slung; leader holds open map with both hands and looks down; observer holds binoculars to eyes.
- PASS: plain uniforms and helmets with no insignia, symbols, badges, flags or text visible.
- PASS with visibility limitation: recognizable Kar98k and MP40 with a straight box magazine; correctly slung, no curved magazines. The MP40's forward section is naturally occluded by the map, so the entire weapon cannot be visually inspected.
- PASS: close composition, legs run off bottom, no feet or prone figures. Highest helmet starts at y=5 (0.5%); observer at about y=20 (2.0%); messenger at about y=34 (3.3%), approximately the guide's 3% margin.
- PASS: solid subject coverage (alpha >127) 72.75%; bottom-edge coverage 70.05%, exceeding 70% frame and one-third bottom-edge requirements.
- PASS: 1536 x 1024, 8-bit RGBA PNG, alpha 0–255, genuinely transparent background, no ground or cast shadows.
- PASS: rendering, light, close crop and materials visually match the R4 reference; paired Soviet composition inspected.
- PASS: final preview visually inspected at 76 x 54 and 3x. Three separate men remain visible; the light map and raised binocular gesture identify headquarters at tile scale. Olive background is clean with no backdrop or visible halo. Final PNG and preview decode/verify successfully; PNG IHDR confirms 8-bit RGBA final and RGB preview. Raw SHA-256 matches the accepted source generation exactly.

## Limitations

Messenger helmet margin is 3.3% (roughly 3 pixels beyond a strict 3% line); no post-generation crop was applied, as required. The map occludes part of the slung MP40. Fine map lines and weapon detail naturally simplify at 76 x 54. Final preview QA completed; no other known unmet requirements.
