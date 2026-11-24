# German rifleman art notes

## 2026-11-22 — Task 01: Standing reference frame

Created with the built-in imagegen tool, then processed with Python/Pillow.
Selected **candidate 3** as `de_rifleman_01_stand.png`: its black straps and ribbed gas-mask canister retain the clearest separation at game scale, with consistent matte field-grey clothing. The final reference is a byte-for-byte copy of candidate 3.

### Files
- `de_rifleman_01_stand_candidate_1.png` and `de_rifleman_01_stand_candidate_1_raw.png`
- `de_rifleman_01_stand_candidate_2.png` and `de_rifleman_01_stand_candidate_2_raw.png`
- `de_rifleman_01_stand_candidate_3.png` and `de_rifleman_01_stand_candidate_3_raw.png`
- `de_rifleman_01_stand_candidate_4.png` and `de_rifleman_01_stand_candidate_4_raw.png`
- `de_rifleman_01_stand.png` and `de_rifleman_01_stand_raw.png` (candidate 3 copies)
- `de_rifleman_01_stand_rejected_oblique_raw.png` (archived initial generation; not a usable sprite)
- `notes.md`

### Processing and measurements
Raw files are unchanged 1254 × 1254 RGBA tool outputs. Removed faint disconnected alpha artifacts; preserved antialiased silhouette edges and cleared RGB in fully transparent pixels. Trimmed the figure, uniformly scaled it, and used a supersampled premultiplied-alpha transform to center the annotated torso pivot exactly at (128, 128).

Helmet bounds and torso pivots were manually annotated by visual inspection in source pixels. Helmet widths are edge measurements, not a model-supplied size claim.

| Candidate | Source helmet x bounds | Source width | Source torso pivot | Final helmet width | Final torso pivot |
|---|---|---|---|---|---|
| 1 | 438–794 | 356 px | (616, 690) | 34 px | (128, 128) |
| 2 | 437–794 | 357 px | (616, 690) | 34 px | (128, 128) |
| 3 | 438–795 | 357 px | (617, 690) | 34 px | (128, 128) |
| 4 | 438–794 | 356 px | (616, 690) | 34 px | (128, 128) |

### Style-guide self-check
Programmatically checked dimensions, RGBA mode, PNG bit depth, fully transparent borders, alpha range, and identity of the selected copy. Inspected every final PNG visually. Helmet edge precision is approximately ±1 final pixel. Camera/pose and equipment checks are visual assessments of raster art, not a measured 3D camera.

| Final file | 256 × 256, 8-bit RGBA, transparency | Overhead / north | Helmet 34 ±2 px | Torso (128,128) | Uniform consistent | No cast shadow, ground, or symbols |
|---|---|---|---|---|---|---|
| de_rifleman_01_stand_candidate_1.png | Pass | Pass | Pass | Pass | Pass | Pass |
| de_rifleman_01_stand_candidate_2.png | Pass | Pass | Pass | Pass | Pass | Pass |
| de_rifleman_01_stand_candidate_3.png | Pass | Pass | Pass | Pass | Pass | Pass |
| de_rifleman_01_stand_candidate_4.png | Pass | Pass | Pass | Pass | Pass | Pass |
| de_rifleman_01_stand.png | Pass | Pass | Pass | Pass | Pass | Pass |

All four show the helmet dome, shoulder straps, rear canister, right bread bag/canteen, left entrenching tool, and a two-handed rifle pointing upper left. Feet are paired and face north. No earlier reference existed; candidate 1 established the design, and candidates 2–4 used it as an image reference. Fine equipment details are necessarily small at native sprite scale.

The initial generation showed too much back and long boot shafts and was rejected. It is retained only as a raw archive. Candidate 2 required a retry after a generation-service output rejection. No unresolved file-format or placement issues remain.

### Final prompt set
Shared production prompt:

```text
Use case: historical-scene. Asset: one production game sprite candidate for Close Fight, German rifleman at Stalingrad September 1942. Single full isolated figure on genuinely transparent alpha, square canvas.
CRITICAL CAMERA: strict orthographic 90-degree straight down from directly overhead, looking vertically down onto the CROWN of helmet, upper surfaces of square shoulders, and small glimpses of boots below. Soldier STANDING UPRIGHT, not lying down. Head/facing points to TOP of image (north). Highly foreshortened upright body from overhead; no face, no front or side view, no perspective or three-quarter view.
Pose: standing still, feet together, shoulders square, Kar98k wood-stock dark-metal bolt-action rifle in BOTH hands diagonally across chest, barrel/muzzle toward UPPER LEFT, butt toward lower right. Rifle silhouette clear. Average build.
Equipment: matte field-grey M1935 Stahlhelm with smooth dome and slightly flared rim, NO decals. Dusty worn field-grey wool M40 tunic and trousers, black leather jackboots. Black leather Y-straps visible over both shoulders and rear torso; black belt; two sets of black ammo pouches at front belt. Tan/grey bread bag and covered canteen at back RIGHT hip; entrenching tool at back LEFT hip. A fluted grey-green metal gas-mask cylinder slung across the BACK, visibly exposed. Arrange all gear physically plausibly and readable from directly overhead. No backpack.
Style: realistic slightly desaturated painterly-photographic military miniature photographed directly above; fine fabric and leather textures; soft even overcast daylight, no cast shadow or ambient shadow outside silhouette, no ground. No text, labels, borders, watermark, swastikas, flags or political symbols.
Proportions: shoulder width about 1.7 times helmet width; compact upright overhead silhouette, not a long prone silhouette. Body torso centered on image. Leave ample transparent space around entire rifle and figure. This will be rescaled to a 256x256 frame with helmet exactly 34 px wide and torso center (128,128).

```

Candidate 1 was regenerated from the rejected initial render using the shared prompt plus this camera correction:

```text

CAMERA CORRECTION from input reference: input is too oblique, showing an entire long back and boot shafts. Render this SAME character but move camera to EXACTLY VERTICAL above his head. Foreshorten the body dramatically. Helmet crown sits centrally between shoulder tops; chest, lower back and long legs are hidden under the helmet/shoulder mass. Only small boot TOE tips are visible beside the forward body, pointing north, never long boot shafts or complete legs. Overall BODY footprint excluding rifle about as wide as tall, helmet width around 45 percent of shoulder-body width. Hips and rear equipment appear just behind/below shoulder mass. Do not stretch body down the page. This must be an actual 90-degree orthographic aerial game sprite. Preserve the same clothing, gear, north facing direction, feet together, rifle held diagonally muzzle upper left, transparent background.
```

Candidates 2–4 used candidate 1 as the reference image and the shared production prompt plus these additions:

Candidate 2:

```text

Input image is the CHARACTER and POSE reference. Produce candidate 2 of this SAME soldier. Preserve exact uniform colors, identical equipment layout, pose, proportions, rifle direction, compact overhead body outline and NORTH-facing paired boot toes. Camera exactly 90-degree vertical overhead, orthographic; see helmet crown and shoulder TOPS, NO long legs or boot shafts, NO face. Keep torso short and foreshortened. Shoulders anatomically 1.7 times helmet width excluding bent arms. Only small rendering variation: slightly softer fine wool shading. Transparent background and absolutely no cast shadows. Do not change design or add gear. Single isolated figure.
```

Candidate 3:

```text

Input image is the CHARACTER and POSE reference. Produce candidate 3 of this SAME soldier. Preserve exact uniform colors, identical equipment layout, pose, proportions, rifle direction, compact overhead body outline and NORTH-facing paired boot toes. Camera exactly 90-degree vertical overhead, orthographic; see helmet crown and shoulder TOPS, NO long legs or boot shafts, NO face. Keep torso short and foreshortened. Shoulders anatomically 1.7 times helmet width excluding bent arms. Only small rendering variation: subtly clearer fine leather and ribbed canister texture. Transparent background and absolutely no cast shadows. Do not change design or add gear. Single isolated figure.
```

Candidate 4:

```text

Input image is the CHARACTER and POSE reference. Produce candidate 4 of this SAME soldier. Preserve exact uniform colors, identical equipment layout, pose, proportions, rifle direction, compact overhead body outline and NORTH-facing paired boot toes. Camera exactly 90-degree vertical overhead, orthographic; see helmet crown and shoulder TOPS, NO long legs or boot shafts, NO face. Keep torso short and foreshortened. Shoulders anatomically 1.7 times helmet width excluding bent arms. Only small rendering variation: slightly more even matte helmet rendering. Transparent background and absolutely no cast shadows. Do not change design or add gear. Single isolated figure.
```


## 2026-11-22 — Task 02: Two-frame walk cycle

Created with built-in imagegen using `de_rifleman_01_stand.png` as the visual reference for both generation attempts; processed with Python/Pillow. The first attempt was rejected by the generation service at output. The second returned both frames together.

### Files
- `de_rifleman_02_walk_a.png`: left foot forward, right foot back.
- `de_rifleman_03_walk_b.png`: right foot forward, left foot back.
- `de_rifleman_02_walk_a_raw.png` and `de_rifleman_03_walk_b_raw.png`: identical, unchanged copies of the complete paired raw generation, retained for reprocessing.
- `de_rifleman_walk_sheet.png`: final 512 × 256 paired sheet, A left/B right.
- `de_rifleman_walk_preview.png`: 2× nearest-neighbor inspection preview.

### Processing and measurements
The service returned 1774 × 887 RGBA rather than the requested 512 × 256. Both poses were generated together in the same image. Final frames and paired sheet have the requested dimensions. Extracted each frame's generated lower limbs and scaled them by 34/125 from the approximately 125 px generated helmet. Reused the exact standing reference upper body to preserve its uniform detail and remove generation drift in the helmet, torso, gear, hands and rifle. Removed only the old paired standing boot toes with an antialiased polygon mask, then composited the moving limbs behind the stationary body. No changes were made to the standing reference file.

The reference helmet retains its measured 34 px width (raw reference bounds x438–795, scale 34/357). Reference torso pivot stays at (128,128) in both frames. Left/right limb centers use x117/x139, swapped in frame B. Front crops start at y84 and rear crops at y156; visually annotated boot centers are approximately 77–79 px apart longitudinally. Some feet are naturally occluded by the rifle and rear equipment. Removed alpha below 8/255 and cleared RGB for transparent pixels; preserved antialiased edges with premultiplied-alpha resizing.

### Self-check
| Final file | 256 × 256, 8-bit RGBA, transparent background | Strict overhead, north | Helmet 34 ±2 px | Torso pivot (128,128) | Reference uniform | No cast shadows, ground, symbols |
|---|---|---|---|---|---|---|
| de_rifleman_02_walk_a.png | Pass | Pass | Pass (34 px) | Pass | Pass | Pass |
| de_rifleman_03_walk_b.png | Pass | Pass | Pass (34 px) | Pass | Pass | Pass |

Programmatically verified dimensions, RGBA mode, PNG bit depth/color type, transparent borders, and equality of all 4,741 opaque reference upper-body pixels across both frames. Visually inspected the final pair at 2×. Camera, pose, uniform and symbol checks are visual raster assessments. Helmet measurement tolerance approximately ±1 px. No unresolved final format or pivot issues. The raw generation resolution is the deviation noted above.

### Final generation prompt
```text
Use case: precise-object-edit. Input image is the character reference for a historical strategy game animation. Produce a transparent 512x256 sprite sheet containing two 256x256 frames side by side. Repeat this exact north-facing soldier, preserving his overhead camera, uniform, colors, equipment, held rifle, helmet, shoulders, hands and torso. Change only the legs to a gentle walking stride.
Left cell A: left boot forward/north, right boot back/south. Right cell B: right boot forward/north, left boot back/south. Both boot toes point toward top of canvas. Short stride about 77 pixels between front and back boot tips. Strict 90-degree straight-down orthographic view of an upright standing/walking person. Only foreshortened lower legs and boots visible beyond torso. No face visible.
Torso center at (128,128) in A and (384,128) in B. Helmet width 34 pixels each, same scale as reference; keep figure small with generous empty space. The torso, helmet, arms, hands, rifle and equipment are identically positioned in both frames, only feet and lower legs animate. Reference's realistic desaturated miniature texture and soft even lighting. True transparent background with clean alpha edges. No ground, cast shadows, lettering, insignia, decorative symbols, borders or watermark.
```


## 2026-11-23 — Task 08: Cowering (1 frame)

Created `de_rifleman_12_cower.png` and preserved the unchanged generation as `de_rifleman_12_cower_raw.png`. Used built-in imagegen with `de_rifleman_01_stand.png` as the character reference. Both hands cover the helmet; elbows and knees are tucked into a cowering pose, with the rifle dropped separately beside him.

Processing: removed disconnected faint alpha artifacts, retained the figure and rifle, normalized nearly opaque alpha, and used premultiplied-alpha resampling. Visually annotated source helmet bounds x548–708 (160 px) and hip pivot (625,755); uniform scale 0.2125 produces a 34 px helmet and maps the hips to (128,128). Helmet measurement is approximate because the hands obscure part of its rim (within ±2 final pixels).

Self-check for `de_rifleman_12_cower.png`: **Pass** — 256 × 256, 8-bit RGBA, transparent background/borders, overhead north-facing view, 34 ±2 px helmet, centered hips, reference-matched uniform, no cast shadow, ground, or symbols. Format/alpha/pivot checks are programmatic; pose, uniform, and camera checks are visual assessments. Raw file: original 1254 × 1254 RGBA, intentionally unprocessed and not a game-ready frame. No unresolved final-file limitations beyond visual landmark measurement precision.

<details>
<summary>Generation prompt (built-in imagegen)</summary>

```text
Use case: historical-scene. Asset: a single production top-down German rifleman cowering sprite for Close Fight, Stalingrad September 1942. Input image 1 is the mandatory CHARACTER reference: preserve its exact subdued field-grey palette, matte M1935 Stahlhelm, dusty M40 wool tunic and trousers, black leather Y-straps and belt, ammunition pouches, black jackboots, rear-right bread bag/canteen, rear-left entrenching tool, and fluted grey-green gas-mask canister. Create a DIFFERENT POSE of this same soldier.
POSE CRITICAL: curled tightly on the ground in terror, resting on BOTH KNEES AND BOTH ELBOWS, knees tucked under abdomen, hips low, shoulders hunched, head bowed toward the TOP of the image. BOTH bare hands are visibly pressed flat on TOP of the helmet crown, one from each side, protecting his head; fingers distinct and hands not holding anything. Upper arms lead to elbows on the ground to either side of bowed head, forearms fold up and inward toward helmet. Clear compact fetal-like crouch, visibly broken morale rather than fighting or crawling. A SINGLE dropped wood-and-dark-metal Kar98k rifle lies separately beside him with a small visible transparent gap, within the same frame. No extra weapons.
CAMERA CRITICAL: strictly orthographic 90-degree vertically overhead, no perspective and no three-quarter view. See helmet crown beneath hands, tops of bent arms, upper back and tightly tucked legs. No face. Figure faces NORTH toward top edge. Shoulders about 1.7 helmet widths, natural average human proportions. Hips are the placement anchor, at image center. Preserve compact pose; generous empty space around figure and entire rifle. Square frame intended for final 256x256, helmet steel rim width 34px at final scale; DO NOT stretch the small curled pose to fill the canvas.
Style: realistic slightly desaturated painterly-photographic miniature matching reference, textured wool/leather/metal, soft even overcast daylight. Truly transparent alpha background, crisp clean silhouette. NO ground, NO cast shadow, NO ambient haze outside the silhouette, no text, labels, borders or watermark. Absolutely no swastikas, flags, political symbols or helmet decals.
```

</details>


## 2026-11-23 — Task 03: Two-frame run cycle

Created with built-in imagegen using `de_rifleman_01_stand.png` as the visual reference, followed by Python/Pillow post-processing.

- `de_rifleman_04_run_a.png`: left foot forward, right foot back.
- `de_rifleman_05_run_b.png`: right foot forward, left foot back.
- Matching `*_raw.png` files: unchanged copies of the complete paired generation.
- `de_rifleman_run_sheet.png`: final 512 × 256 sheet, A left/B right.
- `de_rifleman_run_preview.png`: 2× inspection image on grey; final sprites retain transparency.

The service returned 1774 × 887 instead of the requested 512 × 256; both poses were generated together and then sliced. Annotated source helmet bounds x398–504 give a uniform scale of 34/106. The source torso pivot (449,444) maps to (128,128). Both frames reuse frame A's upper-body layer to prevent jitter; 5,418 opaque upper-body pixels match exactly. Generated legs are placed behind it, with boot centers approximately (116,70)/(140,198) in A and (140,70)/(116,198) in B: 128 px longitudinal separation. Removed disconnected alpha noise, made near-opaque interiors opaque, preserved antialiased edges, and cleared RGB in transparent pixels.

| Final file | 256 × 256, 8-bit RGBA, transparent | Overhead / north | Helmet 34 ±2 px | Torso (128,128) | Reference uniform | No cast shadows, ground, symbols |
|---|---|---|---|---|---|---|
| de_rifleman_04_run_a.png | Pass | Pass | Pass | Pass | Pass | Pass |
| de_rifleman_05_run_b.png | Pass | Pass | Pass | Pass | Pass | Pass |

Checked PNG headers, alpha, transparent borders, single connected silhouettes, shared opaque pixels, and identical raw archive hashes programmatically; inspected the final pair visually. Helmet edge/pivot annotations and camera/pose checks are visual raster assessments, with approximately ±1 px helmet-edge precision. No unresolved final-format issues; the generated raw resolution differs as noted above.

<details>
<summary>Final built-in imagegen prompt</summary>

```text
Use case: precise-object-edit. Asset: production animation sprite for Close Fight.
Input image 1 is the mandatory CHARACTER REFERENCE: the German rifleman. Preserve its exact field-grey matte Stahlhelm helmet, worn grey-green wool uniform, black Y straps and belt, tan bread bag and canteen at his back right hip, entrenching tool at back left hip, grey-green ribbed gas mask canister across the back, wood-stock dark-metal Kar98k held in both hands across the chest with muzzle toward upper left. Match the reference's realistic slightly desaturated painterly photographic miniature rendering, equipment, anatomy, colors, and soft even overcast illumination.
Produce ONE transparent 512 x 256 sprite sheet: two 256 x 256 square cells, frame A at left and frame B at right, no visible divisions. Both show this same rifleman RUNNING NORTH, head and toes pointing toward the TOP edge. Use strict orthographic 90-degree straight-down overhead view, like camera vertically above a running miniature. We see helmet CROWN, shoulder tops and foreshortened back and limbs; no face or side/front view.
Frame A: anatomical LEFT foot (image left) reaches forward/north, right foot reaches back/south. Frame B: anatomical RIGHT foot (image right) reaches forward/north, left foot reaches back/south. Vigorous running stride, roughly 1 meter / 128 pixels from front boot center to rear boot center, with bent knees and natural foreshortened legs. The torso leans forward so helmet center sits slightly NORTH of the shoulders. Do not make a walking stride or a prone person. Keep rifle and both arms held steadily across chest.
CRITICAL INVARIANTS: same torso, same helmet position, same hands, same rifle, identical upper body, colors and equipment in BOTH frames. Only the legs move between A and B. Do not mirror the whole character: keep rifle and side-specific equipment the same.
Layout at final 512x256 resolution: torso center (128,128) in left cell and (384,128) in right cell. Helmet width exactly 34 px each, shoulders approximately 58 px wide excluding arms; helmet center approximately y105, shoulder axis about y119. Front boot approximately y69, rear boot approximately y197. Leave generous TRANSPARENT padding around the small figures; do not enlarge them to fill cells. No body part or rifle crosses cell edge.
Actual alpha transparency; isolated figures without ground or painted checkerboard. Clean edges without halos or haze. Absolutely NO cast shadows, text, labels, border, watermark, decals, flags, swastikas, political symbols or insignia. Generate A and B together as one coherent paired sheet.
```

</details>


## 2026-11-23 — Task 04: Kneeling fire

Created with built-in imagegen using `de_rifleman_01_stand.png` as the visual reference for every attempt; post-processed with Python/Pillow.

- Final: `de_rifleman_06_kneel_fire.png`.
- Unchanged selected generation: `de_rifleman_06_kneel_fire_raw.png` (1254 × 1254 RGBA).
- Archived rejected generation: `de_rifleman_06_kneel_fire_rejected_oblique_raw.png`; it showed an oblique back and the wrong leg forward and is not a usable sprite. An earlier service-level output rejection produced no image.
- Selected source helmet bounds: x503–711 (208 px); uniform scale 34/208. Visually annotated torso pivot (610,745) maps exactly to (128,128). Helmet measurement precision is approximately ±1 final pixel.
- Removed alpha below 8/255 and disconnected specks, made near-opaque interiors opaque, trimmed, then used a 4× supersampled premultiplied-alpha transform. Preserved antialiased edges and cleared RGB in fully transparent pixels.
- Final self-check: 256 × 256, 8-bit RGBA, transparent background/borders; straight overhead and north-facing; helmet 34 px; torso pivot (128,128); reference uniform/gear; no cast shadows, ground, symbols or muzzle flash. Pose: right knee down, left foot forward, raised elbows, rifle shouldered and vertical north beyond helmet. Format/alpha were checked programmatically; camera, anatomy, uniform and symbols were checked visually.
- No outstanding final-file issues. The raw generation resolution differs from the final sprite size by design; raw archives are not game-ready frames.

<details>
<summary>Exact final generation prompt</summary>

```text
Use case: historical-scene. Make one non-graphic historical strategy-game sprite on genuine transparent alpha. Reference image 1 supplies the EXACT uniform, equipment, muted colors, miniature texture and STRICT OVERHEAD CAMERA to preserve.
Repose this same unmarked German WWII rifleman into a compact KNEELING rifle-aiming pose. Soldier faces image TOP, which is north. Character's LEFT equals image LEFT. Character's RIGHT equals image RIGHT.
CRITICAL LEG PLACEMENT: LEFT knee raised and LEFT foot planted FORWARD at upper-left of torso (image left), left boot toe points TOP. RIGHT knee is DOWN on image RIGHT, right shin folds BACK toward lower-right with right boot behind. Only right knee is down. Do not mirror these legs.
Rifle wood stock braced at RIGHT shoulder on image RIGHT. Right hand at trigger, left hand supporting fore-end; elbows raised and bent. Rifle bore runs EXACTLY VERTICAL north, slightly to image RIGHT of helmet, parallel to canvas side, with muzzle above helmet. No muzzle flash or smoke.
CRITICAL CAMERA: an orthographic technical plan view from exactly vertically overhead, directly above the soldier's helmet. Match the input sprite's camera. Helmet crown is a broad round oval, nearly centered inside the shoulder mass. Only TOP horizontal surfaces are visible. NO view of face, NO rear portrait, NO long vertical back, NO tall boot shafts. The upright torso is extremely foreshortened: collar to back belt is shorter than one helmet length, with rear equipment immediately below helmet/shoulders. Silhouette of body excluding rifle has width close to its length, with bent legs forming a compact asymmetric shape. Never draw a full long back hanging below helmet.
Identical dusty field-grey tunic and trousers, matte M1935 steel helmet, black leather Y-straps and belt, black jackboots, belt pouches, grey-green fluted gas-mask cylinder across lower back, tan-grey bag/canteen at rear image-right and entrenching tool at rear image-left. No insignia or symbols.
Realistic slightly desaturated painterly-photographic miniature, soft even daylight. Single full isolated figure, no shadow, no ground, no background, no text. Square canvas with ample transparent margins. Intended 256x256 frame, 34px helmet width and torso center (128,128). Render at high resolution for later downsampling. Entire muzzle and boots inside image.
```
</details>


## 2026-11-24 — Task 07: Grenade throw (two frames)

Created with built-in imagegen, using `de_rifleman_01_stand.png` for every generation, then processed with Pillow. A shows the bent right-arm wind-up; B shows the forward release with a detached grenade.

- Finals: `de_rifleman_10_throw_a.png`, `de_rifleman_11_throw_b.png`.
- Matching `_raw.png` files retain unchanged copies of the accepted complete paired generation. Earlier renders are `de_rifleman_throw_attempt_01_raw.png` and `de_rifleman_throw_attempt_02_raw.png`.
- `de_rifleman_throw_sheet.png`: final 512 × 256 pair, with the full final generation prompt and measurements in PNG text metadata.
- Removed colored edge speckles and disconnected noise; trimmed and rescaled with premultiplied alpha. Annotated source helmet bounds approximately x384–524 (140 px), scaled by 34/140; measurement precision approximately ±1 final pixel. Source torso pivot (454,509) maps to (128,128).
- Reused B's stationary body in both frames, compositing A's generated right arm with a 9 source-pixel horizontal alignment. Preserved 4,759 identical visible pixels and the shared helmet. Rifle visibility changes where the moving arm covers it. The reference was not changed.
- Verified dimensions, RGBA mode, PNG bit depth, transparent borders, zero RGB beneath zero alpha, stationary-pixel equality, and one connected silhouette in A versus body plus detached grenade in B. Visually inspected the final pair.

| Final | 256 × 256, 8-bit RGBA, transparent | Overhead / north | Helmet 34 ±2 px | Torso pivot (128,128) | Reference uniform | No cast shadows, ground, symbols |
|---|---|---|---|---|---|---|
| de_rifleman_10_throw_a.png | Pass | Pass | Pass | Pass | Pass | Pass |
| de_rifleman_11_throw_b.png | Pass | Pass | Pass | Pass | Pass | Pass |

Limitations: the service returned 1774 × 887 raw sheets; final frames/sheet have the requested dimensions. The torso remains fixed to avoid jitter; follow-through is conveyed by the arm without a separate torso rotation. Camera, pose and uniform checks are visual assessments. One initial request was rejected by the service; three subsequent paired renders were produced.


## 2026-11-24 — Task 12: Mortar crewman (1 frame)

Created `de_rifleman_17_crew_mortar.png` and preserved the unchanged generation as `de_rifleman_17_crew_mortar_raw.png`. Used built-in imagegen with `de_rifleman_01_stand.png` as the mandatory character reference, followed by Python/Pillow post-processing. Crouched north-facing soldier behind-left of the mortar, both hands holding one round at the muzzle, rifle slung on his back; north-leaning tube, bipod and square baseplate remain fully inside the frame.

Processing: removed low-alpha background haze (alpha <=96), normalized opaque surfaces (alpha >=200), and preserved the antialiased transition. Trimmed the cutout and applied a uniform 34/214 scale with premultiplied-alpha, 4x supersampled resampling. Visually measured helmet bounds x440–654 (214 px) and torso pivot (530,800) map to a 34 px helmet and (128,128) pivot. Removed one isolated pixel, cleared invisible RGB, and retained clean edges. Final occupied bounds: (80,27)–(221,181).

| File | 256 × 256, 8-bit RGBA, transparent | Overhead / north | Helmet 34 ±2 px | Torso (128,128) | Reference uniform | No shadows, ground, symbols |
|---|---|---|---|---|---|---|
| de_rifleman_17_crew_mortar.png | Pass | Pass | Pass (34 px) | Pass | Pass | Pass |
| de_rifleman_17_crew_mortar_raw.png | Original 1254 × 1254 RGBA; unprocessed low-alpha haze retained | Pass | Original 214 px; normalization not applied | Source (530,800) | Pass | Low-alpha backdrop retained in raw only; no symbols |

Final dimensions, PNG header, alpha, transparent border and transform verified programmatically; camera, equipment, pose, uniform and absence of symbols checked visually. Helmet and torso landmarks are visual raster measurements (helmet precision approximately ±1 final pixel). The raw is an archival source, not a game-ready frame. No unresolved final-frame issues.

<details>
<summary>Generation prompt (built-in imagegen)</summary>

```text
Use case: historical-scene. Asset: one production game sprite of a mortar crewman for Close Fight, Stalingrad September 1942. Input image 1 is the mandatory CHARACTER REFERENCE. Preserve the same soldier, muted field-grey colors, helmet, uniform, equipment, proportions, realistic painterly-photographic military-miniature appearance, and strictly vertical overhead camera.

Create a single crouched German crewman loading an 8 cm GrW 34 mortar. He faces NORTH, image TOP; crouched compactly just behind and to the LEFT of the mortar. Both bare hands hold ONE finned mortar round together directly above the open muzzle, poised to lower it tail-first. Show the round clearly as one object between his hands, its tail aligned with the muzzle. His wood-and-dark-metal Kar98k rifle is SLUNG on his back, not held. One soldier, one mortar, one round.

MORTAR: dark desaturated grey-green steel tube leaning north, visible open muzzle at its north end, believable bipod with two spread legs and a small adjustment assembly, and a SQUARE ribbed baseplate at its south end. All three mortar parts clearly readable from directly ABOVE. Mortar is ahead/north and a little east/right of the crewman, within comfortable arm's reach. The square baseplate is just right of the soldier, bipod further north, muzzle at upper-right. Accurate scale relative to a 34px-wide helmet; compact but recognizable mortar silhouette, all hardware within the frame.

CAMERA CRITICAL: strict orthographic plan view from exactly 90 degrees vertically above, matching the input. See round helmet CROWN, tops of shoulders, upper back, bent knees and arms, TOP of square baseplate and tube. No perspective, no front/side portrait, no visible face. Torso compressed by overhead foreshortening; do NOT draw a long vertical rear view. Head points image TOP.

Suggested composition at final 256x256 scale: soldier's TORSO CENTER at (128,128), helmet 34px wide, head about (128,108); crouched knees and boots close below torso. Mortar muzzle about (163,74), square baseplate about (182,127), bipod footprint spread around (145,98) and (211,99). Hands reach forward-right to muzzle. Keep natural anatomy, no stretched arms. These are layout targets, not text to render. Final whole asset intended for 256x256; render high-resolution square with plenty of transparent margins and nothing clipped.

Same matte M1935 Stahlhelm without decals, dusty grey-green M40 wool tunic and trousers, black leather Y-straps and belt, black pouches, black jackboots, tan-grey bread bag and canteen at back-right, entrenching tool at back-left, grey-green fluted gas mask canister across back. Average build, reference-matched equipment and hues.

Soft even overcast lighting, realistic slightly desaturated painterly-photographic detail. GENUINE FULLY TRANSPARENT ALPHA background, clean cutout edges. NO cast shadows, NO ground, NO background, NO haze, no text, no labels, no borders or watermark. Absolutely NO swastikas, flags, political symbols, insignia or helmet decals.
```

</details>


## 2026-11-24 — Task 09: Wounded (1 frame)

Created `de_rifleman_13_wounded.png` with built-in imagegen, using `de_rifleman_01_stand.png` as the character reference. Preserved the unchanged 1254 × 1254 RGBA generation as `de_rifleman_13_wounded_raw.png`. The soldier lies supine with his head north, front tunic/webbing and ammunition pouches visible, one hand clutching his side, the other arm out, slightly bent knees, and a separate dropped Kar98k. Helmet covers the face; no blood or gore.

Processing: removed faint disconnected alpha artifacts, retained soldier and rifle, normalized near-opaque alpha, trimmed, and resampled in premultiplied alpha at 4× before downsampling. Source helmet half-opacity bounds x524–699 (175 px) give uniform scale 34/175. Visually annotated hip midpoint (605,615) maps to (128,128). Final helmet width independently measured at 34 px; sprite bounds (38,11)–(201,251), with transparent borders. Cleared RGB from fully transparent pixels.

Self-check for `de_rifleman_13_wounded.png`: **Pass** — 256 × 256, 8-bit RGBA, transparent background; straight overhead/head north; helmet 34 ±2 px; hips centered at (128,128); reference-consistent uniform; no cast shadow, ground, or symbols. Format, alpha, two connected components (soldier/rifle), helmet width and placement were checked programmatically. Camera, pose, clothing and symbols were checked visually. Helmet/hip landmarks are visual raster measurements with approximately ±1 final-pixel precision. Body footprint is about 240 px high at the required helmet scale (the guide's prone example is approximately 230 px); nothing is clipped. Raw archive is intentionally unprocessed and is not a 256 × 256 game frame. No other unresolved limitations.

<details>
<summary>Final generation prompt — built-in imagegen</summary>

```text
Use case: historical-scene. Asset: one production top-down wounded infantry sprite for Close Fight, Stalingrad September 1942.
Input image 1 is the mandatory CHARACTER REFERENCE. Preserve this exact German rifleman's subdued field-grey palette, matte M1935 Stahlhelm, dusty grey-green M40 wool tunic and trousers, black leather straps/belt and ammunition pouches, black jackboots, tan-grey bread bag/canteen at rear right hip, entrenching tool at rear left hip, fluted grey-green gas mask canister at back, and wood-stock dark-metal Kar98k. Use the reference for appearance, NOT its standing pose.
NEW POSE: the same full-body soldier lies flat ON HIS BACK, alive but wounded, head toward image TOP/north and feet toward BOTTOM/south. Chest, tunic front/buttons, belt buckle and front ammunition pouches FACE THE CAMERA. One bare hand clutches the side of his abdomen; other arm is thrown outward, relaxed and bent slightly at elbow. Both knees slightly bent, with a gentle asymmetric bend in legs. Only one soldier, two arms, two legs. A SINGLE Kar98k rifle is DROPPED beside him, separate from both hands, entire rifle visible with a transparent gap from body. No gore, no blood or blood pools.
CAMERA: strict orthographic 90-degree straight down, as a military miniature lying on a transparent horizontal plane viewed directly from above. No perspective, no three-quarter angle. Front torso visible because he is supine. Helmet has slipped forward to cover the face: show steel helmet crown/dome and rim with NO visible facial features, no face portrait. Rear gear remains behind/at sides of torso, not on chest.
SCALE AND COMPOSITION: square transparent canvas, full figure including boots and extended hand entirely inside it. Normal average adult build. Helmet width one-seventh of full head-to-boot length; shoulders about 1.7 helmet widths excluding arms. Do not make oversized helmet or stubby body. Hip joint midpoint is at exact canvas CENTER. Head north, body long axis vertical. At intended final 256x256 size, helmet width 34px, body total length approximately 220-230px, hips (128,128); at high resolution keep these proportions with enough margins.
STYLE: realistic slightly desaturated painterly-photographic high-detail miniature matching reference, textured worn wool and leather, soft even overcast daylight. Genuine transparent alpha background, crisp clean silhouette with no haze or halo. NO ground, NO cast shadows, no drawn checkerboard, no text, labels, border, watermark, flags, swastikas, political symbols, decals, or insignia. One frame only.
```

</details>

## 2026-11-24 — Task 10: Dead (two variants)

- Created with built-in imagegen using de_rifleman_01_stand.png; both poses came from one paired generation.
- Finals: de_rifleman_14_dead_a.png (face down, limbs splayed) and de_rifleman_15_dead_b.png (twisted onto side, one bent leg, one arm occluded). Detached rifles at different angles; no blood or gore.
- Raw archives: de_rifleman_14_dead_a_raw.png and de_rifleman_15_dead_b_raw.png are identical untouched copies of the full 1774 × 887 generation. de_rifleman_dead_sheet.png is the normalized 512 × 256 paired sheet.
- Visually measured source helmet widths: approximately 126 px (A), 138 px (B); scales 34/126 and 34/138. Annotated source hip landmarks (440,445) and (1410,430) map exactly to (128,128).
- Cleaned faint/disconnected alpha specks, made interiors opaque, trimmed, then used a 4× premultiplied-alpha transform and Lanczos reduction; retained edge antialiasing and cleared invisible RGB.
- Self-check for EACH final: 256 × 256, 8-bit RGBA; fully transparent background and borders; strict overhead view, heads north; measured helmets 34 px (A) / 34 px (B); hips (128,128); reference uniform/gear; no cast shadows, ground, symbols or visible faces. Format/alpha/size/components checked by code; camera, pose, uniform and exclusions checked visually.
- Raw resolution exceeds the requested sheet resolution; the normalized sheet and final frames meet their requested dimensions. No outstanding final-file issues.

<details>
<summary>Final generation prompt (built-in imagegen)</summary>

```text
Use case: historical-scene. Asset type: non-graphic realistic overhead WWII strategy game sprite sheet.
Input image 1 is the mandatory character visual reference. Preserve the exact soldier identity, dusty muted field-grey wool tunic and trousers, matte grey M1935 helmet shape and color, black leather Y-straps and belt, black jackboots, belt pouches, grey-green fluted gas-mask cylinder across back, tan-grey bread bag and canteen at back right hip, entrenching tool at back left hip. Preserve its realistic slightly desaturated painterly-photographic miniature texture.
Generate TWO DIFFERENT DEAD BODY POSE VARIANTS together in ONE WIDE 512x256 image, two equal square 256x256 cells side by side, with real transparent alpha. Can render larger at precisely the same 2:1 aspect ratio for later downsampling. No dividing line, no text.
CAMERA MUST BE STRICT ORTHOGRAPHIC DIRECTLY OVERHEAD, 90 degrees straight down, both bodies lying on the horizontal plane. Not oblique, not a three-quarter camera. Heads toward image TOP/north in BOTH cells. No visible face.
LEFT CELL A: soldier lying fully FACE DOWN, back up, head pointing north, limp arms asymmetrically splayed away from torso, both legs extended downward and somewhat splayed in a relaxed dead posture. Not actively crawling or holding a weapon. Helmet stays on. One detached wood-stock Kar98k rifle lies a short distance from his right hand at image-right, roughly vertical with slight diagonal.
RIGHT CELL B: visibly DIFFERENT pose of the same soldier lying twisted on his SIDE, head north, face turned down and hidden. One arm underneath torso and mostly occluded, other arm limp forward/left. One leg extended toward bottom, other bent at knee and displaced to image-right, asymmetrical resting pose. Helmet stays on, crown seen from above. One detached wood-stock Kar98k rifle lies at a DIFFERENT diagonal angle to the body's left, clearly separate from the hand. Exactly two legs, two arms per soldier, anatomically plausible.
Scale and layout at target 512x256: each helmet 34px WIDE, about 7.5 helmet widths per cell width. A helmet-to-boot length approximately 220-230px. HIPS are at each cell center: A (128,128), B (384,128), not chest. Head around y30-40, feet around y240. Allow transparent padding all around. Entire figures and detached rifles comfortably inside their own square cell. Match physical proportions and helmet size across poses. No pose enlargement to fill canvas.
Soft even overcast daylight; self-shading only on objects. Clean crisp cutout edges. Genuinely transparent backdrop. NO cast shadows whatsoever, NO ground or floor, NO white or colored halo, NO semi-transparent surrounding haze. No gore, no wounds, no blood or blood pools, no smoke. Absolutely NO swastikas, flags, political symbols, insignia, helmet decals, text, labels, borders or watermarks.
```
</details>



## 2026-11-24 — Task 06: Two-frame crawl cycle

Created with built-in imagegen using `de_rifleman_01_stand.png` as the mandatory character reference for both generations. The optional `de_rifleman_07_prone.png` was absent. Generated both poses together, then processed with Python/Pillow.

- `de_rifleman_08_crawl_a.png`: left knee drawn up, right arm reaching forward.
- `de_rifleman_09_crawl_b.png`: right knee drawn up, left arm reaching forward.
- Matching `*_raw.png` files: identical, unchanged copies of the complete selected paired generation, so either archive retains both original frames.
- `de_rifleman_crawl_sheet.png`: final transparent 512 × 256 sheet, A left/B right.
- `de_rifleman_crawl_attempt_01_raw.png`: archived first generation, rejected because both frames reached with the left arm and it introduced extra shapes near the helmet.

Processing: sliced the selected paired raw, removed disconnected alpha noise and alpha below 8/255, normalized nearly opaque interiors, and retained antialiased edges. Shared frame A's stationary helmet/torso/hip-equipment layer and exposed rifle end sections with B while retaining the generated alternating limbs; natural arm occlusion of the rifle varies. Trimmed to the pair's common bounds, uniformly scaled by 34/118 using 4× supersampled premultiplied-alpha resampling, and mapped the visually annotated hip pivot (448,447) to exactly (128,128) in both frames. Source helmet x bounds 389–507 give 118 px width, hence 34 px final width. Copied the stationary helmet patch once more after resampling to eliminate 1-level edge-rounding differences. Cleared RGB in all fully transparent pixels.

| Final file | 256 × 256, 8-bit RGBA, transparent | Straight overhead / north | Helmet 34 ±2 px | Hips (128,128) | Reference uniform | No cast shadow, ground, symbols |
|---|---|---|---|---|---|---|
| de_rifleman_08_crawl_a.png | Pass | Pass | Pass (34 px) | Pass | Pass | Pass |
| de_rifleman_09_crawl_b.png | Pass | Pass | Pass (34 px) | Pass | Pass | Pass |

Verified PNG headers, transparent borders, one connected silhouette per frame, zero RGB outside alpha, raw archive hashes, and exact pixel equality in stationary helmet (924 px), torso (1,128 px), and hip-equipment (918 px) inspection regions. Visually inspected both final poses at 2×. Helmet bounds and hip landmarks are manually annotated raster measurements (approximately ±1 final pixel); camera, anatomy, uniform and absence of symbols are visual assessments. Final silhouettes fit without clipping. The image service returned 1774 × 887 rather than the requested 512 × 256; the final paired sheet is exactly 512 × 256. No other unresolved final-file issues.

<details>
<summary>Final prompt (built-in imagegen)</summary>

```text
Use case: historical-scene. Make a two-frame sprite sheet for a top-down WWII tactics game. Reference image 1 defines this SAME German rifleman's uniform, helmet, gear, muted colors and realistic miniature style. The reference is standing; create a NEW prone belly-crawl pose. Do not copy the standing feet in front of the head.
ONE 512x256 TRANSPARENT image, two equal square cells side by side, animation A left and B right. Both figures lie FLAT ON THEIR STOMACHS crawling north. Strict orthographic view DIRECTLY VERTICALLY ABOVE THEIR BACKS, head at TOP, feet at BOTTOM, no face, no perspective. All body parts fit in their cell.
LEFT CELL A: LEFT KNEE bends outward and upward toward left hip. RIGHT LEG stretches back straight toward bottom. RIGHT ARM (image RIGHT) reaches STRAIGHT FORWARD past head, while LEFT ARM (image LEFT) is BENT BACK, elbow alongside ribs. Clearly reach with RIGHT hand, NOT left.
RIGHT CELL B: RIGHT KNEE bends outward and upward toward right hip. LEFT LEG stretches back toward bottom. LEFT ARM (image LEFT) reaches FORWARD past head, while RIGHT ARM is BENT BACK with elbow alongside ribs. Arms must visibly ALTERNATE: left-cell right hand forward, right-cell left hand forward. This is a low crawl, not rifle aiming. A SINGLE wood-stock Kar98k lies CRADLED ACROSS THE FOREARMS, roughly horizontally in front of shoulders; both frames have the identical rifle at the identical angle, muzzle toward image LEFT. The reaching hand is free to pull forward while forearms support rifle.
Locked between both cells: SAME exact helmet position, torso, hips, rifle, colors and back equipment. Do not mirror whole soldier. Only four limbs move. Rear RIGHT bread bag/canteen, rear LEFT entrenching tool, grey-green ribbed gas-mask canister across lower back, black Y straps, dusty field-grey wool, black jackboots, matte plain field-grey Stahlhelm. No additional objects or loose equipment. Exactly one head, two arms, two legs per cell. Only transparent space in front of helmet except hands, forearms and single rifle. No binoculars, no extra boots near head.
Body proportions: helmet width 34px at final 256px cell scale, shoulders 58px, roughly 225px from top of helmet to extended boot. Hip joint midpoint exactly cell (128,128); helmet and hips same in both cells. Generous clean transparent margins. Realistic desaturated painterly-photographic miniature detail, soft even overcast light. True alpha transparency. NO background, ground, shadow, haze, text, grid, border, symbols, swastikas, flags, decals or watermark.
```
</details>


## 2026-11-24 — Task 05: Prone (one frame)

Created with built-in imagegen using `de_rifleman_01_stand.png` for every generation. Its matching high-resolution raw reference was added to clarify materials and gear. Processed with Python/Pillow; no CLI/API generation was used.

- Final: `de_rifleman_07_prone.png`.
- Unchanged accepted generation: `de_rifleman_07_prone_raw.png` (1254 × 1254 RGBA).
- Archived first returned image: `de_rifleman_prone_attempt_01_raw.png` (1024 × 1536); rejected for a long forward arm/rifle reach and surrounding haze.
- Inspection image: `de_rifleman_prone_preview.png` (3× nearest-neighbor preview on a neutral background; not a game sprite).
- Source helmet bounds approximately x540–706 (166 px); uniform scale 34/166. Visually annotated hip-joint pivot (619,630) maps to (128,128). Helmet edge precision is approximately ±1 final pixel.
- Removed alpha below 8/255, disconnected artifacts and colored edge specks; normalized near-opaque interior alpha. Trimmed and rescaled with a 4× supersampled premultiplied-alpha transform, then removed two disconnected one-pixel resampling specks. Transparent pixels have zero RGB.
- Pose: flat on stomach, straight north-facing body, extended slightly separated legs, both hands holding one rifle with its muzzle north. The stock rests beside the ribs to fit the complete rifle inside the frame.
- Verified PNG dimensions, 8-bit RGBA color type, transparent borders, alpha range, zero RGB under zero alpha, a single connected final silhouette, and byte identity of the raw archive. Camera, pose, uniform and absence of ground/shadows/symbols were checked visually.

| Final file | 256 × 256, RGBA, transparent | Overhead / head north | Helmet 34 ±2 px | Hip pivot (128,128) | Reference uniform | No cast shadows, ground or symbols |
|---|---|---|---|---|---|---|
| de_rifleman_07_prone.png | Pass | Pass | Pass (34 px) | Pass | Pass | Pass |

Limitations: helmet-to-boot length is approximately 239 px versus the task's approximate 230 px; retained uniform scaling and the required helmet scale. Raw/preview images are supporting assets and do not use the final-frame dimensions. Four earlier service-level output rejections returned no images.

<details>
<summary>Exact final generation prompt (also stored in final PNG metadata)</summary>

```text
Use case: precise-object-edit. Make ONE SQUARE transparent top-down prone infantry game sprite. Image 1 is REQUIRED character reference. Image 2 is its high-resolution detail reference. Image 3 is a pose draft requiring correction. Keep the same field-grey uniform, helmet, equipment and realistic desaturated rendering.
Correct image 3: BEND THE ELBOWS and draw hands BACK alongside the helmet/upper chest. Do not extend the arms above his head. Move the whole rifle DOWN to run beside the RIGHT side of the body: butt beside his right ribs/waist, right hand gripping near chest, left hand supporting forward near helmet. Rifle barrel points straight UP/north parallel to spine. Muzzle only slightly ABOVE helmet crown, not far above it. It is a low ready resting hold, not shouldered aiming. Both hands hold this one rifle. Elbows at either side of shoulders, bent outward symmetrically as possible.
Keep him flat on his stomach with spine straight vertical, head north, back visible, legs extended straight and slightly apart, no bent knees. Use BLACK PLAIN PULL-ON JACKBOOTS without buckles, straps or laces. Exact orthographic 90-degree overhead camera; no visible face, no perspective. Normal human proportions.
Composition is a SQUARE 256x256 logical sprite frame, rendered larger if desired. Hip JOINTS center at (128,128). Helmet width 34px, top at about y22, boots reach about y250, so body length about 228px. Rifle muzzle near y6 and rifle butt near y122; full weapon visible INSIDE canvas. Keep body and all equipment inside all edges.
REMOVE ALL surrounding colored glow, halo, floor, cast shadow and haze from image 3. Transparent pixels immediately outside the figure. Soft even daylight texture only ON the figure. Genuine alpha transparency, clean tight antialiased edges. No ground or background and no text, flags, emblems, insignia or symbols.
```
</details>
