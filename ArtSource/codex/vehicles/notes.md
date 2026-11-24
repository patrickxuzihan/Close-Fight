
## 2026-11-22 — Task V1: StuG III Ausf. F

- Created `veh_stug3f_hull.png` and preserved the original generation as `veh_stug3f_hull_raw.png`. Built-in imagegen used.
- Final: 512 × 1024, 8-bit RGBA, transparent background. Hull including small end attachments measured 692 px long × 378 px wide at alpha ≥128; requested 691 × 378 px. Hull bounds: (67, 166, 445, 858) (right/bottom exclusive).
- Hull pivot: (256, 512). Subpixel placement established at 2× resolution before final downsampling. Source hull anchor: x=119..768, y=449..1702; source canvas 887 × 1774.
- Barrel projection shortened during post-processing: muzzle near y=59, front plate near y=187, about 128 px beyond the front plate. Barrel lies slightly right of centerline and remains part of the hull PNG.
- Removed faint alpha haze and normalized opaque vehicle surfaces; antialiasing retained only at silhouette edges.
- Visual check: strict north-facing orthographic overhead view, dark grey worn finish, box mantlet, no muzzle brake, closed hatches, engine hatches and side grilles, no side skirts, crew, markings, symbols, ground, or external cast shadow. Wheel counts cannot be reliably confirmed from the strict overhead view. No separate reference image was specified.
- Limitations: generated mechanical details are illustrative; individual wheel count has not been certified. No turret image required or produced.

Generation prompt:

```text
Use case: historical-scene.
Asset type: a single production sprite for Close Fight, a realistic top-down WWII tactics game, Stalingrad September 1942.
Primary request: one German StuG III Ausf. F turretless assault gun, entire hull and integrated long 7.5 cm StuK 40 L/43 gun visible, pointing straight NORTH at top of image.
Scene/backdrop: genuinely transparent alpha background, isolated vehicle only.
Camera: STRICT ORTHOGRAPHIC PLAN VIEW, camera exactly 90 degrees vertically overhead. Zero perspective, zero isometric tilt. Vehicle axis perfectly vertical. Do not show vertical front/rear/side armor faces or side-on wheel discs.
Composition: portrait 1:2 canvas, preferably 1024 x 2048. Hull length excluding projecting gun 1382 pixels, overall width over tracks 756 pixels, hull center at (512,1024); hull begins near y333 and ends near y1715. Long straight narrow barrel extends 256 pixels beyond front hull edge, to about y77, slightly RIGHT of centerline. Preserve generous transparent margins. Hull aspect ratio length/width 5.40/2.95.
Subject details: low box-like angular early StuG F casemate covering forward two thirds, flat casemate roof, closed commander hatch and small periscope at rear left of casemate roof, closed loader hatch at rear right. Rear third is engine deck with two large rectangular hatches and louvered side air intakes. Large angular BOX mantlet, NOT curved saukopf. Gun barrel WITHOUT ANY muzzle brake per the requested asset specification: straight simple cylindrical muzzle, no bulb or side ports. No rotating turret, no turret ring, no machinegun turret or tall cupola. Full-length exposed narrow segmented tracks either side, subtle six small road-wheel outer edges partly discernible beside hull only as top-view permits, no side skirts. Spare track links and small stowed tools on fenders. Closed hatches, NO crew.
Style/medium: realistic slightly desaturated painterly-photographic military scale miniature seen exactly from overhead; crisp natural small mechanical detail with restrained weathering.
Color/materials: dark charcoal Panzergrau grey steel; subtle dusty tracks and lower hull, fine scuffed exposed metal edges, dark grilles. No camouflage.
Lighting: soft even overcast ambient illumination, only small intrinsic crevice shading. Absolutely NO cast/drop shadow, NO ground plane, NO smoky halo, no background haze.
Avoid: text, labels, borders, watermarks, markings, numbers, crosses, flags, swastikas, red stars, all political symbols, people, scenery, extra items, perspective, diagonal orientation, split panels. One clean silhouette only.
```


## V3 — T-34/76 model 1942, STZ — 2026-11-22

Created veh_t34_76_hull.png, veh_t34_76_turret.png and their corresponding _raw.png originals using the built-in image generator. Postprocessed with Pillow: removed alpha values <= 8, normalized dimensions with landmark-based resampling, and placed on transparent 8-bit RGBA canvases.

- Hull canvas: 512 × 1024. Outline: 384 × 758 px, bounds (64,133)–(448,891), centered at (256,512). 758 px / 128 = 5.921875 m.
- Hull ring center: (256,397), 115 px north of hull center. Raw visual rim center measured at approximately (587,523); mapped to the specified position, with approximately circular rim preserved. Visual measurement precision is about 1–2 final pixels.
- Turret canvas: 1024 × 1024. Body including mantlet: 256 × 282 px, x384–640, y410–692. Barrel runs north from the mantlet front y410 to muzzle y13: 397 px, no muzzle brake.
- Turret rotation pivot is defined at (512,512), on the barrel centerline, 102 px behind the mantlet front. The invisible ring pivot is intentionally forward of the body bounding-box center to accommodate the rear bustle and fit the full barrel with margin.
- Visual checks: overhead north-facing view, worn desaturated protective green, closed turret hatch, no crew, no markings or political symbols, no ground or external cast shadows. Wheels remain hidden beneath tracks in the strict overhead view; individual five-wheel counts and unrubbered wheel construction cannot be verified in these sprites. AI-generated mechanical details are illustrative, not a certified historical reconstruction.
- The soldier-specific 256 × 256 canvas and helmet checks do not apply; vehicle-guide dimensions supersede them.

### Generation prompts (built-in imagegen)

Hull:

Use case: historical-scene. Asset type: production top-down WWII strategy game tank hull sprite, isolated with true transparent alpha.
Create ONE Soviet T-34/76 model 1942 Stalingrad Tractor Factory (STZ) tank HULL ONLY, with turret completely removed and a circular open turret ring visible. Strict orthographic camera pointing vertically down at 90 degrees; front north/up, rear south/down. No perspective, no visible vertical side faces. Realistic slightly desaturated painterly photographic miniature with crisp detailed mechanical surfaces, worn 4BO protective green, dust on steel tracks, subtle bare metal at edges. Soft even overcast lighting, NO cast shadow.
Geometry matters: full hull silhouette length to width ratio 758:384, including tracks. Center the hull on the image. The circular turret-ring opening is centered on the longitudinal centerline, 15.17 percent of hull length FORWARD of the hull midpoint, that is 34.83 percent down from its front. Opening is a round dark recessed mechanical cavity with metal circular rim, no crew. The intended final canvas is 512 wide by 1024 tall, hull bbox x64..448 y133..891, ring center x256 y397. Keep generous transparent margin all around.
Distinctive T34 sloped angular glacis at FRONT TOP, closed driver's hatch on front LEFT and hull machine-gun ball mount on front RIGHT. Long rear engine deck below ring with historically plausible grilles, rectangular covers and a big round-cornered transmission access hatch at very rear bottom. External rectangular period fuel tanks on hull sides, wide steel tracks on both sides, hints of five plain steel road wheel positions under each track only to extent visible directly overhead. No suspended side view wheels. Symmetrical tracked silhouette, realistic weathering, clear grilles, tools and track links. 
NO turret, NO main gun, NO crew, NO red stars, NO slogans, NO numbers, NO lettering, NO flags, NO political symbols, NO swastikas, NO watermark, NO labels, NO ground, NO background, NO drop shadow, NO halo. Preserve genuine transparent background.

Turret:

Use case: historical-scene. Asset type: isolated transparent production top-down WWII strategy game tank TURRET sprite.
ONE T-34/76 model 1942 Stalingrad Tractor Factory Soviet cast rounded turret with F-34 76.2mm cannon pointing perfectly NORTH/up. TURRET AND BARREL ONLY, no hull or tracks. Strict orthographic directly overhead at 90 degrees, NO perspective, NO visible vertical walls, no gun elevation. Realistic slightly desaturated painterly photographic military miniature finish, worn 4BO protective olive green with dusty crevices, subtle bare metal edge wear. Soft even overcast lighting, no cast shadows.
Canvas square 1024x1024 transparent. Turret ring rotational pivot exactly at (512,512). Rounded compact turret body 256 px wide and 282 px front-to-back, approximately x384..640 and y371..653; the gun mantlet at the front/top, its front face at y397. F-34 straight narrow barrel extends exactly 397 px north from mantlet front to muzzle at y0; to avoid cropping leave extra raw top transparent margin and final placement will be handled in postprocessing. Alternatively provide a larger raw canvas margin preserving these proportions. Proportions: barrel length from mantlet front = 1.41 times turret body length; turret body is 1.10 times longer than wide. The ring is below the turret and not visible but the rotational axis is the geometric center of the rounded body. One large CLOSED single hatch on turret roof toward rear/bottom, small plausible periscope details, CAST rounded gun mantlet, no later hexagonal turret and no twin hatch layout. Long thin cylindrical F-34 gun has simple open muzzle, NO muzzle brake. Barrel perfectly straight aligned to central axis. 
Entire isolated turret and full barrel must fit with transparent margin on all sides. No crew, no hull, no tracks, no red stars, no slogans, no numbers, no text, no flags, no symbols, no swastikas, no ground, no background, no labels, no watermark, NO shadow or halo. Genuine clean transparent alpha.

## Task V2 — Panzer III Ausf. J — 2026-11-22

Created with the built-in image_gen tool, followed by Pillow alpha cleanup and dimensional normalization.

Files: veh_pz3j_hull.png, veh_pz3j_turret.png, veh_pz3j_hull_raw.png, veh_pz3j_turret_raw.png, veh_pz3j_assembly_preview.png.

Measurements (pixel coordinates use the top-left origin):
- Hull: 512 × 1024, 8-bit RGBA. Nonzero-alpha bounds [67,156,445,868), giving exactly 378 px width × 712 px length; hull center (256,512).
- Hull ring: visually measured raw center approximately (483,702), mapped to (256,499), 13 px north of hull center. Circular feature center is visually estimated to within about 1–2 px. Deck bands were rescaled around the ring to correct its position while preserving its circular outline.
- Turret: 1024 × 1024, 8-bit RGBA; designated rotation pivot (512,512). This is the hidden ring center beneath the main roof, not the rear cupola center. Place it at hull (256,499).
- Turret body: nominal 243 × 269 px resampling rectangle; visible alpha approximately 242 × 269 px including rear bin. Mantlet front y=397; visible muzzle begins y=14, approximately 383 px of visible barrel (384 px allotted).
- Assembly preview aligns those pivots and is for inspection only.

Checks passed: specified vehicle canvas sizes; RGBA; transparent outer margins; cleaned isolated alpha noise; north-facing strict overhead view; matching worn dark Panzergrau; separate turret and empty hull ring; closed hatches; rear cupola and storage box; thin long barrel without muzzle brake; no crew, ground, cast shadows, text, or symbols. Infantry helmet and 256 × 256 checks do not apply under the vehicle guide.

Limitations: six road wheels per side cannot be counted in the strictly overhead view because the fenders/tracks conceal them. Ring and hidden pivot placement are art alignment measurements, not CAD-derived geometry. No game integration or code changes were made.

Generation prompts:

Hull:
Use case: historical-scene. Asset: realistic top-down WWII tactics game sprite, Panzer III Ausf J hull ONLY, turret completely removed. Strict orthographic camera vertically overhead 90 degrees, north/up is front, no perspective, no visible vertical front or side faces. Isolated on actual transparent alpha, no ground, no cast shadow, no text, no symbols, no crew. Realistic painterly-photographic military miniature texture, desaturated dark Panzergrau, subtle worn edges, dusty tracks, soft even overcast illumination. Long rectangular German Panzer III hull length/width ratio exactly 712:378, centered, symmetrical tracks. Bare circular turret ring opening at centerline 1.8 percent of hull length FORWARD/up from geometric hull center. Circular ring visible, empty dark interior. Front roof above ring has driver and radio operator closed hatches; rear engine deck below ring has two large rectangular access hatches, grilles, tools along fenders. Panzer III tracks six road wheels per side although wheels hidden by overhead view; realistic steel track links visible at sides. No turret, no gun, no muzzle, no hull superstructure resembling turret. Entire hull visible with generous transparent margin, portrait composition. Save output only inside /Users/xuzihan/Desktop/Personal Projects/Close Fight/ArtSource/codex/vehicles if file saving available.

Turret:
Use case: historical-scene. Asset: isolated turret sprite for realistic WWII top-down tactics game. Panzer III Ausf J long 5 cm KwK 39 L/60 turret ONLY with attached gun pointing straight NORTH/up. NO hull, NO tracks. Strict 90 degree orthographic overhead, absolutely no perspective or visible vertical side faces. Dark desaturated Panzergrau, worn steel edges, lightly dusty, painterly-photographic military miniature detail. Soft even overcast light, no cast shadow. Actual transparent background, no ground. Closed flat turret roof, commander's circular cupola at REAR bottom, side hatch seams just at lateral outlines, rectangular storage bin on turret rear/bottom. Historically accurate angular Panzer III three-man turret outline. Turret body width 243 units, length 269 units including rear bin; thin straight round gun barrel length 384 units FROM mantlet front to muzzle, no muzzle brake. The barrel must be visibly about 1.43 times longer than the entire turret body. Turret ring rotation center on turret centerline roughly middle of main roof, excluding rear bin, should sit just below canvas center so entire long gun fits with clear margin. Gun aligned with central vertical axis. All of the barrel tip and rear bin visible. No crew, no markings of any kind, no text, no numbers, no flags, no political symbols. Single turret only, portrait arrangement within square transparent image. Save output only inside /Users/xuzihan/Desktop/Personal Projects/Close Fight/ArtSource/codex/vehicles if file saving available.

## 2026-11-22 — Task V4: T-60 light tank

- Created `veh_t60_hull.png`, `veh_t60_turret.png`, corresponding `_raw.png` files, and `veh_t60_assembled_preview.png` for alignment QA. Built-in imagegen used.
- Hull: 512 × 1024, 8-bit RGBA PNG. Alpha ≥128 bounds (109, 250, 403, 774), right/bottom exclusive: **524 px long × 294 px wide**. Length error versus 525 px: −0.19%, within ±2%. Continuous placement span 525 px, centered at (256, 512); antialiasing accounts for opaque-bound difference.
- Hull ring: source outer-rim center measured approximately (336.5, 911), mapped to **(211, 474)**. Final rim approximately 130 px across, visually circular and centered within about 1 px. Piecewise resampling moved the ring forward while retaining hull center and dimensions.
- Turret: 1024 × 1024, 8-bit RGBA PNG. Rotation anchor **(512, 512)**, defined at center of generated shell's hidden base, source (625, 885.5). Opaque shell width **178 px** (179 px continuous target). Overall opaque bounds including gun (423, 203, 601, 608). Exposed main barrel from mantlet y≈408.1 to muzzle y≈203.1: **205 px**. Forward barrel region lengthened separately from shell scaling.
- Alignment: turret translated (−301, −38) onto hull canvas, placing pivot exactly over ring. Inspected assembled preview; ring concealed correctly.
- Alpha: removed source alpha below 32; normalized metal alpha ≥240 to 255; retained edge antialiasing and cleared resampling fringe below 8.
- Visual checklist: north-facing strict overhead view; matching worn desaturated 4BO olive-green finish; turretless hull with exposed ring, front-center raised closed driver hatch, right/rear grilles and tracks; angular turret with one closed roof hatch and thin gun. No crew, red stars, numbers, flags, political symbols, text, ground, or external cast shadows. No reference image specified. Helmet/256px soldier checks replaced by vehicle guide.
- Limitation: four road wheels per side cannot be reliably counted from the strict overhead silhouette. Generated mechanical details are illustrative.

Built-in generation prompts (before scale, pivot, and alpha processing):

Hull:

```text
Use case: historical-scene. Production game sprite, Close Fight WWII Stalingrad 1942.
Generate ONE Soviet T-60 light tank HULL WITHOUT TURRET, front pointing exactly north/up, strict orthographic plan view camera exactly 90 degrees overhead, no perspective and no visible vertical armor sides. Transparent RGBA background.
Portrait canvas 1:2. Hull silhouette length to width ratio 4.10:2.30. Center hull on canvas. For a 1024x2048 canvas hull bounds approximately x218..806 y499..1549 (588 wide,1050 long). The OPEN circular turret ring must have its center at (422,948), 90 px LEFT and 76 px FORWARD of hull center (512,1024). Circular ring diameter approximately 230 px. Ring opening is dark inside with a narrow worn metal rim, no turret whatsoever.
Distinctive small T-60 hull: raised box driver's compartment in FRONT CENTER ahead of ring, closed driver hatch; engine deck and grilles on RIGHT side and REAR. Angular sloped front hull, rectangular rear deck, narrow continuous segmented tracks both sides, four small road wheels per side only as top-down permits, fenders with fine tools. No gun or barrel anywhere, no turret, no crew. Do not invent a large extra hatch next to ring.
Realistic detailed miniature photographed from precisely overhead, slightly desaturated painterly photographic rendering. Soviet protective 4BO olive green paint, restrained dust and fine exposed metal scuffs, dark steel tracks. Even soft overcast lighting. No cast shadows, no ground, no haze. Crisp clean alpha edges. No text, red stars, numbers, flags, political symbols, logos, markings, borders, watermarks. Only the single hull. Preserve true vertical overhead view.
```

Turret:

```text
Use case: historical-scene. Single production sprite for Close Fight realistic WWII top-down tactics.
Generate only the detached TURRET of a Soviet T-60 light tank, September 1942. No hull, no tracks, no crew. Genuinely transparent background. Strict ORTHOGRAPHIC TOP PLAN VIEW with camera exactly vertically overhead, absolutely no perspective or side view. The thin straight 20mm TNSh autocannon points vertically NORTH to top of image.
Square canvas. Small angular faceted polygonal turret shell, about 1.4m wide. ONE closed rectangular roof hatch, low small hinge detail, no cupola. Worn desaturated Soviet 4BO olive green matching a dusty military miniature; fine steel scuffs, subdued brown grey dust. Thin dark steel autocannon barrel, exposed length from mantlet to muzzle 1.6m, so exposed gun is approximately 1.15 times the turret shell width. Small restrained mantlet on front. A tiny parallel coaxial machinegun stub permitted. NOT a large tank cannon, no huge muzzle brake.
Composition: turret rotation ring center at exact square canvas center (512,512) on a 1024 square. Turret body about 300px across, centered over that pivot with front at y362 rear at y662. Thin barrel extends north from front mantlet y362 to y18, about344px long, must remain fully in frame with transparent margin. Turret body faceted, with no long rear bustle. Shape is compact, ring under shell and hidden.
Painterly-photographic realistic detailed scale miniature photographed from precisely overhead, soft even overcast lighting, no cast shadow or drop shadow, no ground, no haze. No red star, numbers, text, labels, symbols, flags, political symbols, watermark or border. Crisp clean opaque metal silhouette against true alpha transparency.
```

## 2026-11-23 — Task V1: StuG III Ausf. F, Revision 2

- Re-read both current guides and task shape corrections. Created fresh with built-in imagegen.
- Before replacing, renamed existing final to `veh_stug3f_hull_v1.png` and original raw to `veh_stug3f_hull_v1_raw.png`. Preserved final SHA-256: 3bf1e5f899cd559aa865a4737a00e5e50233a580077621dd068f92d874aa28a2.
- Revision files: `veh_stug3f_hull.png` and untouched generation `veh_stug3f_hull_raw.png`.
- Verified 512 × 1024, 8-bit RGBA PNG, transparent margins. Hull alpha ≥128 bounds: (67, 166, 445, 857), right/bottom exclusive. Hull length 691 px, width 378 px; target 691 × 378 within ±2%.
- Pivot is (256,512), defined by continuous hull placement x67..445, y166.5..857.5 at 2× resolution before downsampling. Pixel-threshold bounds have a 0.5 px vertical midpoint difference due to the odd 691 px length.
- Source canvas 887 × 1774; hull source bounds x47..840, y340..1718. Horizontal landmarks source x=[47,300,408,511,629,720,840] map to final x=[67,187.5,224.5,282,339.5,388,445], normalizing box mantlet width to approximately 115 px and barrel axis to x282, 26 px right of hull center. Landmark accuracy approximately 1–2 final pixels.
- Barrel muzzle y≈47, front plate y≈175, projection approximately 128 px. Barrel integrated into hull. No turret asset required.
- Shape review: wide low casemate overlaps fenders, broad sloped side plates; large slab-sided mantlet; twin front brake hatches; rear-left split commander hatch and periscope; rear-right loader hatch; front-left gunner sight; rear engine deck twin hatches and side intakes; towing cable and tools; exposed tracks, no skirts.
- Finish: dark grey Panzergrau, heavy dusty/muddy tracks and lower edges, scuffed paint and restrained rust. Dark recesses around panels and hatch rims, lighter slopes and raised edges under diffuse direction-neutral light; no external cast shadow.
- Alpha haze removed, metal opacity normalized, silhouette antialiasing retained. North-facing overhead view, no crew, markings, symbols, text, ground, or background. Vehicle guide supersedes infantry canvas and helmet rules. No reference image specified.
- Limitation: six individual road wheels cannot be reliably counted beneath fenders from overhead; mechanical details are generated illustration.
- Only this task's PNGs and appended shared notes changed.

Generation prompt (built-in imagegen):

```text
Use case: historical-scene. Asset type: production top-down game sprite, Close Fight, Stalingrad September 1942.
Generate ONE German StuG III Ausf. F assault gun, integrated hull and gun, facing straight NORTH/up. Strict 90-degree ORTHOGRAPHIC PLAN VIEW directly vertically overhead, zero perspective and NO visible vertical front or side faces. Isolated on genuinely transparent alpha, no ground or cast shadow.
SHAPE IS CRITICAL: wide, low, angular casemate on Panzer III tracked chassis. The casemate extends nearly the whole width between outer tracks and sits OVER the fenders, with broad straight sloping side plates. It must read as a WIDE LOW SUPERSTRUCTURE, not a narrow central tower, not a turret, not a giant flat undifferentiated slab.
Layout using final 512x1024 coordinate proportions (generate at higher resolution if possible, preserve proportions): hull overall x67..445, y166.5..857.5, center256,512. Two track strips at outer sides, slightly wider than fenders. FRONT hull at TOP is a short flat glacis/deck from y167 to about245 with TWO closed rectangular track-brake access hatches side by side, clearly visible flanking the gun. Casemate begins abouty245 and endsy625; broad base x91..421, flat roof x116..396, continuous sloped side armor strips each side. Roof is clearly separated from lower front glacis and rear engine deck by seams and ambient occlusion.
Gun is fixed in a BIG BOX-SHAPED SLAB-SIDED rectangular mantlet, width115px, center x282, 26px RIGHT of hull center. Mantlet approximately x224.5..339.5,y230..325, inset into casemate front. Box mantlet is plainly wide: about one third of the full vehicle width. NOT a small cylinder/collar, NOT rounded Saukopf. Long 7.5cm StuK40 L/43 barrel centered x282, straight north from box, narrow cylindrical tube with simple bare muzzle WITHOUT muzzle brake as explicitly required. Muzzle near y39, about128px ahead of front hull. Barrel and box are part of the hull image, no separate turret.
Casemate roof: small gunner sight opening FRONT LEFT at about(160,340); commander SPLIT closed hatch with TWO leaves and small periscope REAR LEFT at about(173,548); loader closed hatch REAR RIGHT at about(340,550). No cupola. Rear third y625..858 is engine deck with TWO BIG rectangular engine hatches side by side and louvered air intakes either side. Towing cable secured along fender and small tools on fenders. Steel tracks full length both sides; hints of six small road wheel positions per side only as vertical overhead allows, never side-on discs. No skirts. All hatches CLOSED, no crew.
Style: realistic slightly desaturated painterly-photographic aerial military miniature, not CAD or clean plastic model. Dark charcoal Panzergrau grey paint. Heavily field-used: thick grey-brown dust and dried mud on tracks and lower hull, accumulated dirt in edges, irregular small steel paint scuffs on raised edges, a little restrained rust, subtle dusty variation on roof.
VOLUME WITHOUT DIRECTIONAL LIGHT: symmetric ambient occlusion, dark crevices between plates, under fenders, around hatch rims and mantlet; raised hatch rims, weld seams, worn edges catch diffuse ambient light. Sloped glacis and side plates a little lighter than flat roof. Strong readable separation of plates and height without any one-sided highlight. Soft omnidirectional overcast illumination. No cast/drop shadow, no ground plane, no haze.
Whole silhouette in portrait 1:2 canvas with transparent margins. NO text, labels, border, watermark, crosses, flags, numbers, markings, political symbols, swastikas, red stars, crew, extra objects. No visible vertical faces, no perspective, no turret. Prioritize exact wide casemate and large right-offset box mantlet silhouette with visible short front deck and twin brake hatches.
```


## V3 revision 2 — T-34/76 STZ — 2026-11-23

Regenerated both sprites using built-in imagegen with the existing finals as geometry references. Added ambient-occlusion shading in recesses, under fenders and around hatches; lighter sloped armor; heavier track mud and lower-hull dust; scuffed paint, exposed metal edges and restrained rust. No directional key light or external cast shadows.

Archived existing finals as veh_t34_76_hull_v1.png and veh_t34_76_turret_v1.png before replacement. Preserved old raw images as veh_t34_76_hull_raw_v1.png and veh_t34_76_turret_raw_v1.png. New finals and raw images use the original filenames.

Measurements: hull 384 × 758 px, bounds (64,133)–(448,891), 512 × 1024 RGBA canvas; hull center (256,512). Ring center unchanged at (256,397), 115 px forward of center (visual measurement precision approximately 1–2 px). Turret 1024 × 1024 RGBA canvas, body 256 × 282 px at (384,410)–(640,692), rotation pivot unchanged at (512,512). Barrel length 397 px from mantlet front y410 to muzzle y13. Normalized generated landmarks with Pillow, removing near-transparent stray pixels (alpha <=8) and preserving clean antialiased edges.

Checks: 8-bit RGBA, transparent background and margins; strict overhead north-facing view; green finish consistent across parts; no crew, markings, ground, political symbols or external cast shadows. Vehicle guide overrides soldier dimensions/helmet checks. Five road wheels remain obscured by tracks in this overhead view; exact wheel construction is not verifiable. Historical mechanical details remain an AI illustration.

### Revision 2 prompts

Hull:
Edit the supplied game sprite for revision 2. Preserve EXACT orthographic straight-down camera, north heading, silhouette, mechanical geometry, every hatch and fixture position, composition and transparent canvas. Change ONLY surface appearance, weathering and nondirectional volume shading. Realistic slightly desaturated painterly-photographic WWII Stalingrad combat vehicle, worn 4BO protective green. Strong AMBIENT OCCLUSION in recesses and seams, darker plate gaps and under fenders, around hatch bases. Raised edges, hatch rims and weld seams catch soft ambient light on ALL sides evenly. Sloped glacis and sloped armor surfaces slightly lighter than flat roofs so volume reads. NO single directional key light, no lateral light gradient, NO external cast shadow. Heavy dusty grime, caked dried earth mud on tracks and lower hull/below fenders, scuffed/chipped green paint on edges exposing dark steel, restrained scattered rust. Combat worn NOT clean plastic/CAD. Retain visible green painted armor, do not turn the entire object brown. Sharp clean alpha silhouette; true transparency, no ground or added shadow. No crew, numbers, text, stars, symbols, flags or watermark. Preserve all transparent canvas margins and don't crop or enlarge the subject.
HULL ONLY. Preserve 512x1024 canvas, hull bounds x64..448 and y133..891; turret-ring center (256,397), hull center (256,512). Keep open round ring, no turret. Heavy dust and mud especially tracks; darken grille recesses and ring interior.

Turret:
Edit the supplied game sprite for revision 2. Preserve EXACT orthographic straight-down camera, north heading, silhouette, mechanical geometry, every hatch and fixture position, composition and transparent canvas. Change ONLY surface appearance, weathering and nondirectional volume shading. Realistic slightly desaturated painterly-photographic WWII Stalingrad combat vehicle, worn 4BO protective green. Strong AMBIENT OCCLUSION in recesses and seams, darker plate gaps and under fenders, around hatch bases. Raised edges, hatch rims and weld seams catch soft ambient light on ALL sides evenly. Sloped glacis and sloped armor surfaces slightly lighter than flat roofs so volume reads. NO single directional key light, no lateral light gradient, NO external cast shadow. Heavy dusty grime, caked dried earth mud on tracks and lower hull/below fenders, scuffed/chipped green paint on edges exposing dark steel, restrained scattered rust. Combat worn NOT clean plastic/CAD. Retain visible green painted armor, do not turn the entire object brown. Sharp clean alpha silhouette; true transparency, no ground or added shadow. No crew, numbers, text, stars, symbols, flags or watermark. Preserve all transparent canvas margins and don't crop or enlarge the subject.
TURRET ONLY with entire thin F-34 barrel, no muzzle brake. Preserve 1024x1024 canvas, body x384..640 y410..692, barrel tip y13 and mantlet front y410, rotation pivot (512,512). Retain single large closed rear hatch. Strong ambient recess shading around hatch and cast mantlet, chipped rims, accumulated dust and modest rust; avoid thick mud over turret roof.

## 2026-11-23 — Task V2 Panzer III Ausf. J — Revision 2

Re-read both style guides and task. Used built-in imagegen edits of the revision 1 final sprites, followed by Pillow alpha cleanup and landmark-based resampling. Added deeper ambient occlusion in recesses, plate joints and hatch rims; brighter raised edges and sloped plates; heavy dusty/muddy tracks and lower hull; chipped paint and restrained rust. Even ambient lighting, no directional key light or exterior cast shadow.

Before replacement, renamed this task's previous hull, turret, raw sources and assembly preview to their respective _v1.png names. Other vehicle files untouched. New files: veh_pz3j_hull.png, veh_pz3j_turret.png, veh_pz3j_hull_raw.png, veh_pz3j_turret_raw.png, veh_pz3j_assembly_preview.png.

- Hull: 512 × 1024, 8-bit RGBA; alpha bounds (67,156)–(445,868), right/bottom exclusive: exactly 378 × 712 px. Hull center unchanged at (256,512).
- Hull ring center unchanged at (256,499). Raw visual ring center approximately (620,602), mapped to this anchor with the circular middle band preserved. Visual center accuracy approximately 1–2 pixels.
- Turret: 1024 × 1024, 8-bit RGBA. Hidden ring rotation pivot unchanged at (512,512), using the same roof-relative anchor as v1. Body occupies nominal 243 × 269 px at (391,397)–(634,666). Thin barrel extends 384 px from mantlet front y397 to muzzle y13; no muzzle brake.
- Assembly preview places the turret pivot exactly on the hull ring. Visually inspected final hull and assembled tank.
- Validation passed: canvas sizes, RGBA mode, fully transparent outer margins, cleaned alpha specks, north-facing strict overhead view, matching dark Panzergrau with stronger weathering and local volume, closed hatches, no crew, symbols, text, ground or external cast shadows. Vehicle dimensions replace infantry helmet and 256 px checks.
- Limitation retained: six road wheels per side cannot be counted beneath fenders/tracks from a strict overhead view; mechanical details are illustrative. Hidden pivot is an assigned art anchor, not directly visible geometry.

Built-in edit prompts:

Hull:
Edit the provided Panzer III Ausf J hull sprite. Preserve EXACT overhead orthographic 90-degree camera, north orientation, geometry, silhouette, hatches, tools, empty circular ring, and image placement. This is a material and shading revision ONLY. Canvas 512x1024; hull 378x712 at center (256,512); turret ring center MUST stay (256,499). No turret, no barrel. Revision 2: substantial ambient occlusion shading in recesses, hatch gaps, plate joints, under fenders, ring recess and grilles; brighter raised rims and welds; sloped glacis and sloped plates slightly lighter than flat roofs. NO single directional key light, use uniform hemispheric overcast lighting with rich local volume. Heavy Stalingrad combat weathering: packed earthy mud in track links and lower hull, heavy grey-brown dust below fenders, deposits in corners, chipped/scuffed paint at exposed edges, restrained rust spots. Keep dark grey Panzergrau visible, realistic desaturated painterly photographic military miniature, no clean plastic CAD appearance. Tracks and mechanical detail must stay readable. No cast shadow anywhere, no ground, no background, no haze. Preserve real alpha transparency. No crew, no markings, no text, no symbols. Keep ALL proportions, scale, ring position unchanged.

Turret:
Edit supplied isolated Panzer III Ausf J long 5cm L/60 turret sprite, material and local shading revision ONLY. Preserve EXACT original geometry, silhouette, top-down 90-degree orthographic camera, north-pointing thin long barrel with NO muzzle brake, closed rear cupola, rear storage bin, side hatch outlines, mantlet and all details. Preserve original placement on transparent 1024x1024 canvas: hidden ring pivot (512,512); turret body approximately x391..634 y397..666, barrel from y397 to y13; DO NOT recenter visible silhouette or enlarge turret to fill canvas. Revision 2: stronger ambient occlusion in hatch seams, cupola rim, plate gaps and under mantlet; raised rims, welds and edges catching soft light uniformly; sloped side plates somewhat lighter than flat roof. NO directional key light, no side-lit gradients, even hemispheric overcast lighting but convincing local volume. Matching heavily field-worn Stalingrad 1942 dark grey Panzergrau: accumulated grey-brown dust in seams and on roof, paint scuffs and exposed metal on edges, some restrained rusty streaks/bolts and dried dirt on lower outer edges. Realistic desaturated painterly-photographic textured metal, not clean plastic/CAD. No hull, no tracks, no crew, no text, no markings or political symbols. NO cast shadow or drop shadow, no ground, no haze. Actual alpha transparent background. Keep shape and anchor placement unchanged.

## 2026-11-23 — Task V4: T-60 light tank — Revision 2

- Re-read style and vehicle guides, including revision-2 shading/weathering rules. Edited the existing sprites with built-in imagegen as geometry references.
- Before replacement, renamed previous hull/turret finals to `veh_t60_hull_v1.png` and `veh_t60_turret_v1.png`. Also retained previous raw generations and assembled preview with `_v1.png` suffixes. Backups verified byte-for-byte using SHA-256.
- New finals: `veh_t60_hull.png`, `veh_t60_turret.png`; new unprocessed generations: corresponding `_raw.png`; updated `veh_t60_assembled_preview.png`.
- New finish: stronger ambient occlusion in plate seams, hatch rims/hinges, engine grilles, turret ring and fender recesses; raised rims/welds catch diffuse light; sloped armor facets lighter than flat roofs. No single directional key light or external cast shadow. Added patchy accumulated dust, muddy tracks/lower hull, chipped edges and restrained rust; retained desaturated 4BO green.
- Hull canvas **512 × 1024**, 8-bit RGBA. Alpha ≥128 bounds **(109,250,403,774)**, right/bottom exclusive; **524 px length × 294 px width**, unchanged, within 0.19% of requested 525px length. Hull center **(256,512)**. Ring center retained at **(211,474)** by mapping visually measured raw ring center approximately (366,821) to that anchor (visual precision about ±1px).
- Turret canvas **1024 × 1024**, 8-bit RGBA. Hidden rotation-ring pivot **(512,512)** unchanged; body opaque width **178px**, nominal179px; exposed barrel **205px**, as in v1. Overall opaque bounds **(423,203,601,608)**. Raw pivot anchor approximately (627,627), mantlet y499.5, muzzle y249.
- Registered generated RGB using piecewise coordinate maps, resampled at 4× then downsampled. Reused each v1 alpha mask exactly, preserving silhouette, canvas, dimensions, and antialiased edges; nearest-interior RGB extension removed color noise at alpha edges. Final alpha arrays verified pixel-identical to v1. Ring is an internal visual landmark rather than an alpha cutout; hidden turret pivot is a placement convention, not a visible marker.
- Assembly uses turret offset **(−301,−38)**, putting turret pivot over hull ring. Visual QA confirms assembly alignment, north-facing orthographic overhead view, matching finish, closed hatches, no crew, red stars, numbers, flags, political symbols, ground or external shadows. Vehicle-specific sizes replace soldier helmet/256px checks.
- Limitation: individual four-road-wheel count is obscured by fenders/tracks in this strict overhead view; generated mechanics remain illustrative.

Built-in edit prompt — Hull:

```text
Use case: precise-object-edit. Revision 2 production sprite for Close Fight, Soviet T-60 tank HULL WITHOUT TURRET. The input image is the exact geometry and layout reference and edit target. Change ONLY surface shading and weathering. Preserve its strict orthographic 90-degree directly overhead camera, north-facing orientation, all mechanical parts, silhouette proportions, closed raised front-center driver box, right/rear grilles, tools, exposed empty circular turret ring offset to left and slightly forward. Keep the large transparent margins and object placement: canvas 512x1024, hull bounds x109..403 y249.5..774.5, hull center256,512, circular ring center211,474. Do not add a turret or gun.
NEW FINISH: realistic battle-worn Stalingrad September 1942 miniature, desaturated Soviet 4BO protective olive green. Strong localized AMBIENT OCCLUSION giving visible volume: dark gaps between plates, under fender lips, around hatch hinges and ring recess, deep dark engine grilles; hatch rims, weld seams and raised metal edges catch soft diffuse light. Sloped glacis and sloped driver-box plates are a little LIGHTER than flat horizontal roofs, symmetrically on all sides. Absolutely NO single directional key light, NO side-to-side lighting gradient. Make contrast substantial enough to read at small game sprite size while retaining subtle photographic texture.
Heavy dusty earth deposits on BOTH tracks and everything below fenders; caked dried dark brown mud packed BETWEEN track links with exposed worn metal tread ridges. Heavy lower-hull dust, scuffed chipped green paint along edges, small restrained rust spots at fasteners and worn seams. Roof green remains visible beneath finer patchy dust; avoid uniformly beige finish. A field-used fighting vehicle, not pristine museum model, plastic or CAD. No large external mud blobs changing silhouette.
Transparent alpha background preserved. No cast shadows, no ground, no halo, no crew, no numbers, no text, no red stars, flags or symbols. Keep all geometry and ring position unchanged.
```

Built-in edit prompt — Turret:

```text
Use case: precise-object-edit. Revision 2 production sprite for Close Fight. Input is exact geometry/layout reference and edit target: isolated Soviet T-60 small angular faceted turret with thin north-pointing 20mm TNSh autocannon, single CLOSED rectangular roof hatch, small coaxial stub, no cupola. Change ONLY surface shading and weathering, preserve mechanical layout, outline, long thin barrel and north-facing strict orthographic 90-degree overhead camera. Keep transparent margins, body position, size, and rotation pivot. On 1024x1024 canvas the HIDDEN TURRET RING PIVOT remains (512,512), body bounds about x422..602 y416..608; exposed barrel extends from y408 to203, 205 pixels. Do NOT center the whole silhouette including barrel on canvas.
NEW FINISH matches a heavily used Soviet 4BO green T60 in Stalingrad September1942: desaturated olive green with realistic painterly-photographic texture. Clearly stronger localized AMBIENT OCCLUSION for volume: deep narrow recesses around roof hatch, hinges, gun socket, seams and under overhangs, raised hatch lip and weld seams catch light evenly. ALL sloped polygonal armor facets a little LIGHTER than the flat roof, with subtle scuffed edges on every side, not a single key light. Absolutely NO directional lighting gradient, no single bright side or dark side. Cylindrical barrel gets symmetric soft ambient form shading, no hard directional highlight.
Weathering: patchy accumulated grey-brown dust, worn green paint with metallic edge chips and fine scratches; small restrained rust at hinges, bolts and seams. Dust at lower perimeter but retain visible green roof paint; do not coat entire roof beige. Battle-used steel, not pristine museum display, plastic model or CAD. Mechanically legible at small game scale.
Preserve genuinely TRANSPARENT alpha background. NO hull or tracks, NO ground or cast shadow or halo, NO crew, NO red stars, numbers, flags, text, logos or political symbols. Preserve exact silhouette and pivot.
```
