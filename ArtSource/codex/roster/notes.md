
## R1 — German roster figures — 2026-11-24

- Read `00-style-guide.md`, `roster/01-german-figures.md`, and `roster/00-roster-guide.md`; the roster camera, sizing, and checklist override the top-down sprite rules.
- Attempted the rifleman reference first using the built-in image generation tool. Both calls returned HTTP 400 `moderation_blocked` at the output stage (`safety_violations=[abuse]`). No image was returned.
- Prompts requested one unmarked 1942 field-grey infantry miniature, full-length front/15-degree view, diagonal Kar98k at port arms, clean transparent background, no insignia, no shadow, and proportions for the 160 × 368 roster canvas. The second attempt simplified the description to a plain museum miniature.
- Service request IDs: `1a894ae5-0fb7-4180-9c1e-1fcb3c626432` and `d0b2ba9a-e176-449b-9526-77bdebdaf172`.
- Files made: this `notes.md` only. No final figures, raw generations, or previews were available to save.
- Status: incomplete; all nine figures and both previews remain outstanding. The size, alpha, alignment, scale, thumbnail readability, and visual-content checks could not be performed.
- No game code, Assets, or docs changed.

### R1 retry requested by user — 2026-11-24

- Retried the rifleman with the built-in image generation tool, retaining the requested 1942 regular-army equipment, neutral miniature presentation, transparent background, and explicit exclusion of all insignia and political symbols.
- The service again returned HTTP 400 `moderation_blocked` at the output stage (`safety_violations=[abuse]`), without a specific content explanation. Request ID: `4cf0a279-4320-4ff0-8ef9-7ec09652fd41`.
- No image returned; no raw, final, or preview PNG could be saved. All nine figures and both previews remain outstanding; visual and technical checklist checks remain unperformed.

### R1 flat-silhouette rifleman attempt — 2026-11-24

- Re-read the updated `roster/00-roster-guide.md` and `roster/01-german-figures.md`. The current target is a flat RGB (20, 20, 16) silhouette on transparent alpha, walking right, with a 160 × 368 canvas, helmet about 44 px wide, and soles at y = 362.
- Made exactly one built-in image generation attempt as requested. The prompt specified a side-on rifleman with a flared helmet skirt, tall jackboots, a diagonal rifle separated from the torso by negative space, no shading or texture, and no insignia or symbols.
- Result: HTTP 400 `moderation_blocked`, output stage, `safety_violations=[abuse]`; the service supplied no specific content explanation. Request ID: `1173c402-ab3a-4095-801c-7e9b5500501b`.
- No image was returned, so no raw PNG, final PNG, or preview was created. The RGBA/tone, scale/alignment, thumbnail readability, and insignia checklist could not be verified.
- Stopped after this single blocked silhouette attempt, as instructed. Only this notes file was updated.


## R2 — Vehicle roster pictures — 2026-11-26

- Read the general style guide, roster picture guide, and R2 brief; used the roster-specific front three-quarter camera and landscape canvas.
- Created `unit_stug3f.png`, `unit_pz3j.png`, `unit_t34_76.png`, and `unit_t60.png`, plus each matching `_raw.png`, `r2_preview.png`, and the discarded first StuG generation `unit_stug3f_attempt01_raw.png`.
- Used built-in image_gen, with the rifle-squad rendering reference and all named vehicle shape references. The first StuG casemate was too tall; a second generation lowered it and enlarged the projecting box mantlet. The brief's two-hatch hexagonal T-34 turret took precedence over the older single-hatch sprite reference.
- Prompt set: unmarked, closed-hatch 1942 vehicles, front facing lower left, common approximately 18-degree elevated camera, soft upper-front light, weathered Panzergrau or protective green, transparent background. Vehicle prompts specified the listed gun, turret/casemate, hatch, and running-gear details. Exact generation/edit prompts are embedded in each final PNG's `Generation prompts` text metadata.
- Post-processing: normalized generated alpha (removed values at or below 8, made values at or above 248 opaque); resized with premultiplied-alpha Lanczos; centered each cutout at 1382 px wide on a 1536 × 1024 RGBA canvas with a 51 px bottom margin. Raw copies are unchanged.
- Technical checks passed for all four: 8-bit RGBA, fully transparent outer margins, full vehicle inside frame, 89.97% width, common bottom alignment, embedded prompts, and byte-identical raw copies.
- Visual checks passed: no markings, people, open hatches, ground, or cast shadows; matching angle/light; distinct silhouettes at 76 × 54. The preview shows each on RGB (153, 158, 117) tiles at 76 × 54 and 228 × 162, preserving aspect ratio.
- Visible geometry checked: StuG has a low broad turretless casemate, box mantlet, long L/43 with single-chamber brake, closed rear-left hatch/periscope, six small road wheels and three return rollers; Panzer III has a central box turret, side hatch, rear cupola/storage box, long thin unbraked gun, six small wheels and three rollers; T-34 has sloped armor, forward hex turret, two round closed hatches, unbraked F-34, five large wheels and no return rollers; T-60 has an offset small turret, short thin 20 mm gun, narrow tracks, four wheels and three rollers. No side skirts.
- Limitation: far-side running gear and the Panzer III's opposite side hatch are naturally occluded by the required camera and cannot be independently counted/inspected in these single-view pictures.
- Project writes were confined to `ArtSource/codex/roster/`; game code, Assets, and docs were not changed.

## R3 — Gun roster pictures and full crews — 2026-11-26

- Completed the two Soviet pictures first, then the German pictures, using the built-in image-generation tool and the specified rifle-squad style / gun shape references. Both German crew requests succeeded on their first attempts; no refusals, request IDs, or crewless fallbacks.
- Finals: `unit_m1937_45.png`, `unit_bm37.png`, `unit_pak38.png`, `unit_grw34.png`. All are 1536 × 1024, 8-bit RGBA PNGs. Preserved their four matching `_raw.png` files and the earlier `unit_m1937_45_initial_raw.png` / `unit_pak38_initial_raw.png`.
- Corrected the M1937 crew's left-to-right ordering and the PaK 38's rearward V trails. Cleaned residual alpha (≤16 to zero, ≥240 to opaque, remapped intermediate alpha), then fitted each subject to about 90% canvas width with its bottom at y=988. No source reference changed.
- Created `r3_preview.png`: all four finals at 76 × 54 and 228 × 162 on exact RGB (153, 158, 117) olive tiles, with keys beside them.
- Checklist passed by visual inspection: requested crew counts and distinct jobs; unbraked thin M1937 barrel, straight shield, spoked wheels and V trails; PaK 38 two-chamber brake, spaced shield plates, pressed-steel wheels and corrected V trails; BM-37 round plate / GrW 34 rectangular plate, steep tubes and bipods. No prohibited tracks, hulls, turrets, mortar wheels, markings or insignia. Consistent front-left three-quarter presentation and soft light; clean transparency with no ground or cast shadows; each type distinguishable at game size.
- Automated checks passed for dimensions, 8-bit RGBA, alpha range 0–255, transparent corners/margins, retained raw images, and exact preview tile color. All deliverables are inside ArtSource. No game build was run because this task creates standalone art and the task prohibits outputs elsewhere. No outstanding requested deliverables.

<details>
<summary>Reproducible prompt set — built-in image generation</summary>

### m1937_45

```text
Use case: historical-scene.
Asset type: Close Fight roster unit portrait, single landscape 1536 x 1024 RGBA PNG with fully transparent background.
Create one realistic finely detailed studio-rendered military miniature vignette of the Soviet 45 mm M1937 light anti-tank gun with exactly FOUR crewmen in September 1942, neutral historical game art.
Input image 1 is STYLE REFERENCE ONLY: match realistic rendering, soft upper-front lighting, dusty fabric and equipment, subdued colors and level of detail. Do not copy its weapons, composition, white background or insignia.
Input image 2 is the GUN SHAPE AND PAINT REFERENCE ONLY. Reinterpret that top-down sprite in the camera specified below.
Camera: front left three-quarter view, looking down only 15–20 degrees. The gun muzzle faces diagonally toward the LOWER LEFT of the picture; the rear trails extend to upper right. NOT a top-down view.
The gun is large and prominent across the foreground, the four men grouped around and behind it. The full group fills about 90% of image width with a small bottom margin; every boot, wheel, barrel and trail fits inside the canvas.
Gun must be small and light: thin long unbraked barrel with plain muzzle, flat steel shield with straight top edge, TWO open SPOKED wheels with rubber tyres, TWO separate trail legs spread in a clear V behind, protective muted 4BO green paint. Show visible spokes and both V trail legs, don't let figures hide their defining shapes. No muzzle brake, no tracks, hull or turret.
Exactly four different actions, left to right: 1 gunner crouches at the sight behind the left shield, eye at sight; 2 loader kneels beside breech holding a single small shell horizontally in both hands; 3 commander half bent at waist pointing forward toward lower left; 4 ammunition carrier walking with a rectangular ammunition box held at his side.
Crew wear plain khaki gymnastyorka tunics and trousers, SSh-40 steel helmets, plain leather belts, dark boots or puttees. Uniforms entirely unmarked: blank collars, sleeves, helmet, chest and belt. No epaulettes, badges, medals, eagles, stars, armbands or any insignia.
Realistic scale-model photography, natural anatomy, matte dusty metal, scuffed edges and a little dried mud on tyres, soft even lighting, no hard shadows.
No ground, no base/plinth, no floor or floor shadow, no cast shadows, no scenery. Actual transparent alpha including spaces between figures and spokes. No white rectangle. No crosses, stars, numbers, slogans, flags, text, political symbols or swastikas anywhere.
```

### bm37

```text
Use case: historical-scene.
Asset type: Close Fight roster portrait, single landscape 1536 x 1024 PNG, true transparent background.
Create a realistic finely detailed scale-model studio render of a Soviet 82 mm BM-37 mortar and exactly THREE adult crewmen, September 1942, neutral historical game artwork.
Input 1 is a STYLE reference only: realistic fabric, metal, skin, lighting and detail. Ignore its rifles, insignia and white background. Input 2 is a SHAPE and COLOR reference of the mortar viewed from above: reproduce its ROUND base plate and muted protective green paint, but use the new camera below.
Camera: front-left three-quarter, 15–20 degrees looking down. The mortar's firing direction is toward LOWER LEFT, not toward the viewer's right. Smooth short mortar tube about 1.2 metres long inclines steeply at 60 degrees above horizontal, supported in front by TWO bipod legs; tube bottom seats in a clearly visible ROUND metal base plate at rear-right. Small optical sight at left bipod side. Protective 4BO green paint, lightly scuffed/dusty. No wheels, no gun carriage, no long horizontal barrel.
Mortar is the main subject, large unobstructed in front, crew clustered around and behind. Every man in a different pose, all full bodies contained in frame:
LEFT / slightly behind the tube: loader kneels on his left knee, right knee raised, BOTH hands holding ONE finned mortar bomb vertically over and aligned with the open tube muzzle, just about to drop it tail first into the tube.
CENTER / behind-right of bipod: gunner kneels low on both knees, torso leaning forward, one hand adjusting the small optical sight and the other resting on the adjustment crank. Do not put his face in front of the muzzle.
RIGHT: ammunition man deeply crouches beside an OPEN rectangular wooden case visibly containing several finned mortar bombs; he extends one bomb in his right hand toward the loader, left hand on the case edge.
Exactly three men, three distinct jobs and silhouettes. Plain khaki gymnastyorka tunics and trousers, SSh-40 helmets, plain leather belts, worn dark boots or puttees. Uniforms and equipment completely unmarked: no eagles, badges, medals, stars, armbands, epaulettes or insignia of any kind.
Same scale and lighting as a high-quality realistic military miniature: matte natural materials, small scuffs and a little dried mud; soft even upper-front light. The entire group occupies 90% width, near bottom with small safety margins.
Transparent alpha everywhere except the subjects, including between limbs and bipod. Absolutely no background color, colored glow, backdrop, grey floor, scenery, ground, plinth, cast shadows or floor/contact shadows. No text, symbols, flags, crosses, numbers, swastikas, or watermark.
```

### m1937_45_revision

```text
Use case: precise-object-edit. Edit this Soviet 45 mm M1937 crew roster picture. Keep the realistic scale-model rendering, the gun's geometry and green paint, exactly FOUR khaki-uniformed unmarked crew, front-left three-quarter camera and lighting. Make just this composition correction: MOVE the pointing commander from the far left to behind the kneeling loader, between the loader and the ammunition carrier, in the center-right. The left-to-right order of heads must be: crouched gunner at sight, kneeling loader holding a shell in both hands, half-bent commander pointing toward lower left, standing/walking case carrier at far right. Give them four clearly distinct silhouettes. Do not add another figure.
Keep the gun fully visible, with a long thin plain barrel (no muzzle brake) projecting toward lower-left, one flat straight-topped shield, two SPOKED rubber-tyred wheels and two V-spread trails. No tracks, hull or turret.
Transparent background, NO backdrop or floor, no ground, no cast or floor shadow, no glow or haze. Completely blank plain uniform collars, shoulders and sleeves: no insignia, badges, stars, emblems, text, crosses or other symbols anywhere. Full group centered in 1536x1024 landscape canvas, 90% width and a small margin at bottom; nothing cropped.
```

### pak38

```text
Use case: historical-scene.
Asset type: Close Fight roster unit portrait, single 1536 x 1024 landscape PNG with transparent alpha.
Create a neutral historical game miniature of the German 5 cm PaK 38 anti-tank gun and exactly FOUR crewmen in plain unmarked 1942 field-grey uniforms.
Input image 1: realistic rendering and lighting reference only; do not copy its Soviet uniforms, insignia or weapons. Input image 2: the PaK 38 SHAPE AND DARK-GREY PAINT reference from above, to reinterpret at the camera below.
Camera and composition: front-left three-quarter view, viewed from 15–20 degrees above horizontal. Long barrel points toward LOWER LEFT. Gun large across the foreground, crew clustered behind and around it. Entire group fills 90% canvas width, near bottom, all parts inside a small safety margin.
Specific PaK 38 geometry is essential: a low, light TOWED gun, a long thin barrel ending in a conspicuous TWO-CHAMBER muzzle brake (two sequential side vent chambers); a shield consisting of TWO SPACED slightly angled steel plates, one immediately behind the other with a visible air gap at top and side edges, barrel through the middle. Shield should have the broad curved/angled profile from reference, NOT a single flat slab. TWO pressed-steel wheels with solid rubber tyres (solid metal wheel faces, not open spokes). TWO long trail legs spread in a clear V behind, with spade ends. Dark Panzergrau paint, dusty rubber, scuffed edges and a little dried mud. No tracks, chassis hull, turret or tank parts.
Crew left-to-right in exactly this order, four separate distinct poses and actions:
1 gunner CROUCHES behind left edge of shield, his eye at optical sight.
2 loader KNEELS beside breech with a 5cm shell held horizontally in BOTH hands.
3 gun commander KNEELS at the right shield edge, holding BINOCULARS to his eyes looking along the gun direction.
4 ammunition carrier upright with a rectangular AMMUNITION CASE held at his side.
Keep the gun outline readable; don't hide the wheels, twin shield edges or V trails with figures. Plain field-grey tunics and trousers, M35 steel helmets, plain leather belts and boots. No insignia of ANY kind: absolutely blank collars, helmet, chest and sleeves; no eagles, badges, medals, stars, armbands, rank markings or political symbols. Neutral concentrated expressions, no salutes.
Realistic 3D scale-model photograph matching style reference: matte worn steel and cloth, natural human anatomy, fine detail, softly desaturated, soft even upper-front daylight. No dramatic action or muzzle flash.
Fully transparent background including holes around gun and between crew. NO ground, base, floor, cast shadows, floor shadows, haze, glow or scenery. No crosses including Balkenkreuz, no stars, numbers, flags, text, slogans, swastikas or watermark.
```

### grw34

```text
Use case: historical-scene.
Asset type: Close Fight unit roster portrait, 1536x1024 landscape RGBA PNG with actual transparent background.
Create one neutral historical miniature render of a German 8 cm Granatwerfer 34 mortar with exactly THREE crewmen wearing plain unmarked 1942 field-grey uniforms.
Input image 1 is realistic rendering / soft lighting reference only, not its weapons or uniform insignia. Input image 2 is SHAPE AND PAINT reference for the mortar: preserve its RECTANGULAR ribbed base plate and dark grey paint, but change the overhead reference view to the camera below.
Camera: front-left three-quarter, 15–20 degrees looking down. Mortar firing azimuth toward LOWER LEFT. Its smooth tube roughly 1.1 metres long leans steeply forward at about 60 degrees, muzzle at upper-left, heel at lower-right on a clearly visible roughly RECTANGULAR steel base plate with corners. Two narrow bipod legs, optical sight on the left leg. Dark grey Panzergrau paint, scuffed steel, modest dust. Distinct tube, bipod and plate silhouettes.
Mortar large and foremost in foreground with men around and behind; group fills about 90% image width, full bodies, bombs and feet all inside small margins, bottom aligned.
Exactly three different crew actions, also DIFFERENT poses from a conventional line of three kneeling men:
LEFT/rear of muzzle: loader kneels on his RIGHT knee with LEFT leg planted forward, torso slightly twisted, holds ONE small finned 8cm mortar bomb in both hands over the muzzle, about to drop it tail-first. Bomb is small enough to fit in the 80mm bore. His hands raised but below the top edge.
CENTER behind bipod: gunner kneels on his LEFT knee, RIGHT knee raised out to the side, head angled toward the small optical sight on the bipod's left side, left hand on sight and right hand adjusting a crank. His lower pose differs from the loader.
RIGHT: ammunition man in a low squat with both heels on the same level, beside an open dark wooden case of finned mortar bombs in front of him. One hand lifts and offers a single bomb upward toward loader, the other hand reaching into case; head looking down at remaining bombs.
Plain field-grey tunics and trousers, M35 helmets, plain leather belts, leather boots. Absolutely no uniform insignia: blank collars, chest, sleeves and helmets; no eagles, badges, medals, stars, armbands, rank marks or political symbols.
High-quality realistic scale-model photograph, matte natural fabric, subtle wear, natural hands and anatomy, same soft even upper-front light as reference, desaturated palette.
No wheels, no long horizontal gun barrel, no ROUND base plate. No ground, floor, scenery, cast shadows, floor shadows, glow, haze or plinth. Transparent background including holes among legs and equipment. No crosses, flags, stars, numbers, text, slogans, swastikas or watermarks anywhere.
```

### pak38_revision

```text
Use case: precise-object-edit.
Edit this PaK 38 roster portrait to correct ONLY the gun's TWO TRAIL LEGS. Everything else stays: the four unmarked field-grey crew in their current distinct poses and left-to-right order, two-layer spaced angled shield, long barrel with two-chamber brake pointing lower-left, two pressed-steel wheels, dark-grey paint, soft light, front-left three-quarter camera, realistic miniature style.
The current image incorrectly shows a spade-ended leg sticking out to the far LEFT of the left wheel like a sideways outrigger. REMOVE that left-side outrigger completely. A PaK 38 has a SPLIT TRAIL opening behind the breech, not a transverse stabilizer bar.
Both separate trail legs must visibly originate on the rear carriage behind the axle/breech and extend REARWARD toward the RIGHT of the image, spreading into a V on the plane under the crew: one far trail goes back to UPPER-RIGHT, the other near trail goes back to LOWER-RIGHT. Both end in spades. Show the fork and separated diagonal tubes as clearly as the crew allows, do not join them into one straight bar. The commander can kneel in the opening between those two rearward trails while retaining his binocular pose. There must be NO trail end or spade protruding on the far-left side of the gun. Preserve the crew and gun silhouette otherwise.
Keep everything fully inside a 1536x1024 landscape canvas with safe margins. Absolutely transparent alpha background; no floor, ground, shadows, backdrop, glow or haze. Blank unmarked uniforms and equipment: no insignia, eagles, medals, crosses, stars, numbers, text, political symbols, swastikas or flags.
```

</details>
