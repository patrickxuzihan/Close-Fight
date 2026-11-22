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
