
## G2 — 45 mm M1937 (53-K) — 2026-11-22

- Created `gun_m1937_45.png` and `gun_m1937_45_raw.png` using built-in imagegen.
- Final: 1024 × 1024, 8-bit RGBA PNG, transparent background. Removed faint exterior alpha specks (alpha below 8).
- Measured raw landmarks: muzzle y=244; forward shield face y=576; axle center (627,681); shield width 267 px. Calibrated the carriage and barrel, then continuously adjusted the trails below their hinges.
- Final barrel length: 269 px (2.10 m), shield face to muzzle. Axle center: (512,512). Shield width: approximately 205 px. Trails: approximately 256 px from hinge to spade center, approximately 50° opening. Landmark measurements are visual, with about 2 px edge uncertainty.
- Visual check: overhead view, barrel north, split trails south, worn Soviet green, no crew, markings, symbols, ground, or cast shadows. Helmet and uniform checks do not apply to gun assets. Task-specific 1024 × 1024 canvas supersedes the infantry canvas.
- Prompt: Strict orthographic overhead Soviet M1937 (53-K) 45 mm anti-tank gun, firing position, realistic painterly-photographic miniature, worn desaturated 4BO green, soft overcast light, transparent background; thin north-facing barrel without muzzle brake, 269 px from shield face; shield 205 px wide ahead of horizontal axle; two edge-on wheels; axle pivot centered; two 256 px south-facing trails spread 50°; no crew, text, markings, symbols, ground or shadows.
- Limitations: AI-generated historical detail; geometry checked against task dimensions rather than an engineering drawing.

## G3: 8 cm GrW 34 mortar — 2026-11-22

Created `gun_grw34.png` and `gun_grw34_raw.png` using built-in image generation. Final: 256 × 256, 8-bit RGBA, transparent background. Square baseplate approximately 64 × 64 px, bounding box (96,96)–(160,160), center/pivot (128,128). Northward tube projection from plate center to muzzle: approximately 77 px, muzzle y≈51. Raw plate bounds approximately x407–843, y660–1084. Plate scaled to square; forward assembly compressed vertically to match the specified foreshortening. Low-alpha background residue removed.

Visual checks: overhead view; tube north; worn dark grey; bipod and left sight present; no crew, ground, cast shadows, text or symbols. Helmet/uniform checks are not applicable under the vehicle guide. Physical tube length/elevation are represented through the requested projected length, not independently measurable in a 2D sprite. Prompt saved in `gun_grw34_prompt.txt`.
# Task G1 — 5 cm PaK 38 anti-tank gun

Date: 2026-11-22 (Australia/Melbourne)

Files: `gun_pak38.png`, `gun_pak38_raw.png`, `notes.md`.
Generated with the built-in imagegen tool; raw output retained unchanged. Final processed with Pillow: removed alpha values <=8, resampled geometry, and positioned on a transparent canvas. Only ArtSource project files were created. The generation tool also retains its automatic source in its managed cache.

## Measurements and verification

- Final: 1024 × 1024, 8-bit RGBA PNG. Fully transparent corners and 982,443 fully transparent pixels; no clipping.
- Axle midpoint: (512, 512), mapped from visually identified raw midpoint (627, 667).
- Barrel: 410 px from shield face to muzzle tip; final centerline endpoints approximately (512, 454.5) and (512, 44.5). Raster outline starts at y=44. This follows the task's specified barrel dimension.
- Shield: approximately 256 px wide, measured across the thin curved armor bar; double-layer edge visible.
- Split trails: approximately 293 px from hinge centers to spade centers, approximately 52° included angle. Landmark measurements are visual estimates, not CAD measurements.
- North/front scale: 410/570 vertically; horizontal scale: 256/378; rear vertical scale: 0.691. Both regions meet at the axle without a gap.
- Visual check: straight overhead, barrel north, trails south; double-baffle muzzle brake; two wheels; worn Panzergrau; no crew, ground, cast shadows, text, or political symbols. Clean alpha outline, with edge antialiasing retained.
- Helmet, uniform-reference and human-pose checks: not applicable; the vehicle/gun guide replaces the human scale rules.
- No unmet task requirements identified. Mechanical details are generated artwork and have not been validated against an engineering drawing.

## Generation prompt

Use case: historical-scene. Create a production game sprite, a single German 5 cm PaK 38 antitank gun in firing position. Strict 90-degree orthographic directly overhead plan view, never perspective or three-quarter. Realistic slightly desaturated painterly photographic military miniature, worn dark grey Panzergrau metal, subtle dust and exposed metal edge wear. Soft even overcast lighting, NO cast shadows. True transparent background, no ground, no crew, no symbols, no text, no markings. Square 1024 x 1024 canvas. Important exact layout at 128 pixels per meter: barrel points straight north on x=512. Axle midpoint at (512,512). Thin curved double-layer armor shield ahead of axle, approximately from x=384 to 640, center shield face near y=470; seen edge-on from above as a thin curved horizontal bar, NOT a broad face. Barrel projects 410 pixels from shield face to muzzle tip at y=60, with recognizable double-baffle muzzle brake. Two narrow rubber-tired disc wheels on either side of axle, seen strictly from above so tire tread is visible and wheel side discs are not facing camera. Breech and compact carriage centered behind shield. Two tubular split trails extend south from near axle, each 294 pixels long and splayed 25 degrees to each side of south, 50 degree included angle, ending with compact ground spades. Keep entire gun within image with transparent margins. Mechanical gun details must be realistic, symmetrical carriage geometry, no decorative extra parts. Long slender barrel, compact shield and rear carriage. This is an isolated sprite, not a diagram.

## G4: 82 mm BM-37 mortar — 2026-11-22

- Files: gun_bm37.png and gun_bm37_raw.png. Built-in imagegen generation.
- Final: 256 × 256, 8-bit RGBA PNG, transparent background.
- Raw circular plate: approximately x=434–817, y=599–982, diameter 383 px. Scale 64/383; final diameter 64 px; center/pivot (128,128).
- North-facing tube: projected heel-to-muzzle extent approximately 77 px, muzzle y=51. Exposed northern assembly shortened during post-processing to match foreshortening; circular plate preserved.
- Faint stray alpha removed, invisible RGB zeroed.
- Visual checklist: overhead view, round plate, forward bipod, left sight, worn Soviet green, no crew, markings, symbols, ground or cast shadows. Helmet/uniform checks not applicable.
- Prompt: Isolated WWII Soviet 82 mm BM-37 mortar, strict orthographic directly overhead view, north-leaning tube, round horizontal baseplate, forward bipod and left sight; realistic slightly desaturated painterly-photographic miniature, worn 4BO green and soft overcast light. Transparent background, no shadows, ground, crew, extra props, markings or text. Square composition with 64 px plate centered at (128,128), tube projected length 77 px on intended 256 × 256 canvas; high resolution generation with the same proportions.
- Limitation: generated visual depiction, not an engineering reconstruction.
