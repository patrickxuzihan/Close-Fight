# R5-03 — Soviet platoon headquarters
Date: 2026-11-26 (Australia/Melbourne)

## Deliverables
- unit_su_hq.png — final, 1536 × 1024, 8-bit RGBA, transparent background; original composition and canvas retained.
- unit_su_hq_raw.png — unchanged built-in image-generation output.
- unit_su_hq_preview.png — reference and final on RGB (153, 158, 117) olive tiles, each at 76 × 54 and a nearest-neighbour 3× enlargement (228 × 162).
- unit_su_hq_notes.md — this record.

## Method
Read 00-style-guide.md, the R5-03 task and 00-squad-guide.md. The squad guide explicitly replaces the sprite camera, canvas, scale and pivot requirements.
Used the built-in image generation tool with de_rifle_4_raw.png as the rendering, lighting, camera and framing reference.
The paired ArtSource/codex/roster/squads/unit_de_hq.png did not exist at the reference check, so no paired-composition comparison was possible.
Two successful built-in image generations: initial composition, then a targeted helmet correction to remove the crown strips and German-like high side vents and give all three helmets the plain rounded Soviet profile. No refusals; no refusal request IDs. The final raw file is the unchanged second generation. The first generation remains preserved in the tool-managed cache at /Users/xuzihan/.codex/generated_images/01a21759-67b7-7b01-8457-c5681c9b2ca7/exec-7abb1a8f-c47b-4397-b6b7-5131950e5e72.png.
Only the four named task deliverables were written to the project, all inside ArtSource/codex/roster/squads/. No code, Assets, documentation or shared roster notes were changed.

## Processing and measured checks
The revised service output supplied real alpha (0–254), with residual low-alpha background haze. Cleared alpha ≤32 to zero, restored almost-opaque alpha ≥247 to 255, and cleared hidden RGB where alpha is zero. Other antialiased edge alpha was preserved. No RGB recolouring, rescaling or final cropping.
- Final PNG: 1536 × 1024, RGBA; alpha range 0–255.
- Figure coverage at alpha >160: 78.33% (requirement ≥70%).
- Bottom row occupied at alpha >160: 79.04% (requirement ≥33.33%).
- Fully transparent pixels: 21.30%.
- Foreground bounding box: (3, 1, 1533, 1024); reaches bottom, with no feet visible.
- Helmet crowns visually within the top 3%.
- Preview mirrors tools/roster/process.py fill(), including the 304 × 216 intermediate before downsampling to 76 × 54. Crop boxes: {'reference': (18, 0, 1459, 1024), 'soviet_hq': (29, 0, 1470, 1024)}.
- Revised raw SHA-256: 5490bda38ff8c5124672fc990e44454d80e536bf1984e6057dfc39c9f7a1b19a.

## Visual checklist
- Exactly three standing men, left to right: messenger turning toward the leader; leader studying a map held in both hands with slung PPSh-41; observer holding binoculars to his eyes.
- Different faces, ages, builds and poses; khaki-brown uniforms, green helmets and rolled greatcoats.
- No visible insignia, political symbols, text, badges, flags or watermark.
- Visible complete Mosin-Nagant with spike bayonet and PPSh-41 with round drum; both supported by slings. No curved magazines.
- Helmets close to top; legs cut by bottom edge; no prone figures or feet; coverage thresholds pass.
- Fine realistic rendering, soft lighting and tight framing follow the reference.
- Final 76 × 54 olive-tile readability: pending visual preview inspection.
- No hard ground shadow or opaque backdrop.
- No game build was run: this task only delivers source artwork.

## Prompt (verbatim)
Built-in image generation, transparent_background=true.
Reference input: ArtSource/codex/roster/squad_test/de_rifle_4_raw.png.

Use case: historical-scene.
Asset type: Close Fight Soviet platoon headquarters roster portrait for a tiny 76 x 54 game tile; create a 1536 x 1024 landscape PNG with genuine transparent alpha.
Input image 1 is the REQUIRED visual style and framing reference, not the subject to copy. Match its realistic finely detailed 3D rendering, soft upper-front daylight, dusty cloth and leather, camera at chest height slightly from front left, and tightly packed helmet-to-knee framing.

Exactly THREE different adult Soviet soldiers of September 1942, standing shoulder to shoulder with slight natural overlaps. Left to right:
1. Young, slim messenger, torso facing viewer slightly right, turning his head right toward the leader to listen. A complete wooden-stock Mosin-Nagant bolt-action rifle with long straight spike bayonet is slung on his outside shoulder; clear leather sling; not aiming. Keep its muzzle and bayonet visible within the canvas by angling the rifle diagonally, butt by the outer thigh.
2. Broader middle-aged platoon leader studying an open pale paper map, held visibly in BOTH hands around waist height. His head tilts down toward the map. A complete recognizable PPSh-41 with round drum magazine and wooden stock is slung diagonally across his chest ABOVE the map, fully readable with its barrel and round drum exposed; the sling supports the weapon while both hands hold the map.
3. Older binocular observer, both hands holding binoculars to his eyes, looking toward viewer's right. Elbows give him a distinct silhouette. No extra weapon is needed.

All wear plain slightly worn khaki-brown tunics and trousers, rounded olive-green SSh-40 steel helmets, simple brown leather belts and pouches, and a rolled khaki greatcoat worn diagonally over one shoulder. Each man has a different face, age, build and pose. Use Soviet shapes and equipment rather than the German uniforms in the reference.

Composition is critical: three broad closely packed figures fill all three columns from left edge to right edge. Helmet crowns within the top 1–3 percent of the image. The bottom edge cuts across their legs at about the knees: NO feet and no empty margin below them. Figures cover at least 70 percent of the whole canvas, with almost no empty gaps between bodies; solid legs extend to bottom across well over one-third of the width. The map and binoculars must remain legible at tiny tile size. Keep every important head and role-defining prop within the centered 76:54 crop.
Transparent empty space only; no opaque scene or backdrop, no terrain or ground, no cast shadows.
NO insignia of any kind: no stars, eagles, badges, medals, ribbons, armbands, flags, political symbols, swastikas, logos, text, letters or watermark. Map may show subtle unlabeled contour lines but NO writing or symbols. No curved magazines. No extra people or duplicated limbs.


## Targeted revision prompt (verbatim)
Built-in image generation, transparent_background=true. Edit input: the initial cleaned unit_su_hq.png.

Use case: precise-object-edit.
Edit the attached Soviet headquarters portrait. Change ONLY the three steel helmets. Keep absolutely everything else unchanged: the exact three people, faces, skin, poses, garments, greatcoat rolls, hands, map, complete Mosin rifle and spike bayonet, PPSh with drum magazine, binoculars, lighting, scale, crop, 1536 x 1024 canvas, and true transparent background.

Replace the three helmets with historically recognizable plain olive-green Soviet SSh-40 helmets: smooth rounded bowl dome, simple shallow continuous rolled rim, short almost horizontal front visor, modest skirt at the sides and rear. Correct the silhouette to the simple round Soviet SSh-40. REMOVE the conspicuous straps or raised strips currently running over each crown; the crown must be uninterrupted smooth steel. REMOVE the high prominent side air-vent stud inherited from the German helmet. SSh-40 helmet liner fastening uses small inconspicuous low rivets, no high ventilation lug. The helmets should be slightly dusty matte olive green, matching the soft realistic 3D material detail of the rest of the image. No star, no insignia, no symbols, no writing, no added markings.

Preserve helmet crown heights within the top 3% of frame, preserve each man's head angle and do not change his face or the binocular placement. Preserve the excellent tightly packed chest-height front-left composition, kneecropped standing legs, shoulder-to-shoulder overlaps, and exact positions of all existing items. Background must remain fully transparent PNG alpha; do not add a backdrop, ground, shadows, text or additional people.
