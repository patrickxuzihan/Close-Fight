# Soviet rifleman art notes

## 2026-11-23 — Task 01: Standing reference frame

Created four candidates with the built-in imagegen tool, then post-processed with Python/Pillow. Selected **candidate 4** as `su_rifleman_01_stand.png`: its even matte olive helmet reads cleanly at game scale while the left-shoulder coat roll, gathered duffel, and left-hip equipment remain distinct. The chosen reference is a byte-for-byte copy of candidate 4.

### Files

- `su_rifleman_01_stand_candidate_1.png` through `su_rifleman_01_stand_candidate_4.png`, each with an unchanged `_raw.png` generation.
- `su_rifleman_01_stand.png` and `su_rifleman_01_stand_raw.png`: copies of candidate 4 and its raw.
- `su_rifleman_01_stand_preview.png`: review-only 2× enlargement, candidates 1–4 from left to right; not a game frame.
- `su_rifleman_01_stand_rejected_layout_raw.png`: archived first attempt, rejected for mirrored equipment and a short rifle; not a usable sprite.
- `notes.md`.

### Processing and measurement

All accepted raw generations are 1254 × 1254 RGBA. Removed alpha below 8/255 and disconnected specks, retaining the full connected figure/rifle silhouette. Trimmed and uniformly rescaled with premultiplied-alpha interpolation. Cleared RGB in fully transparent pixels. No painted equipment or clothing was added during processing.

Helmet rims were visually annotated at source x506–744 (238 px) in each candidate, with approximately ±1 final-pixel measurement precision. Applied scale 34/238 = 1/7. Anatomical torso pivot was annotated at source (625,710), then mapped to exactly (128,128) with a supersampled affine transform. All four final silhouettes occupy x42–185, y51–175, with transparent margins and no clipping. Anatomical shoulder width is approximately 58 px, excluding arms and carried kit.

### Self-check for every final frame

| File | 256×256, 8-bit RGBA, transparent | Vertical overhead / north | Helmet 34 ±2 px | Torso (128,128) | Consistent uniform | No cast shadow, ground, symbols |
|---|---|---|---|---|---|---|
| su_rifleman_01_stand_candidate_1.png | Pass | Pass | Pass (34 px) | Pass | Pass | Pass |
| su_rifleman_01_stand_candidate_2.png | Pass | Pass | Pass (34 px) | Pass | Pass | Pass |
| su_rifleman_01_stand_candidate_3.png | Pass | Pass | Pass (34 px) | Pass | Pass | Pass |
| su_rifleman_01_stand_candidate_4.png | Pass | Pass | Pass (34 px) | Pass | Pass | Pass |
| su_rifleman_01_stand.png | Pass | Pass | Pass (34 px) | Pass | Pass | Pass |

Dimensions, PNG bit depth/color type, transparent borders, alpha range, and selected-file identity were checked programmatically. Camera, pose, equipment, colors and absence of symbols were assessed visually in every raw and final. A raster image does not encode a measurable 3D camera. The round unmarked helmet dome, left-shoulder rolled greatcoat, rear drawstring duffel, and separate gas-mask bag and covered entrenching tool at the left hip are visible in all candidates. Feet are paired, shoulders square, and the rifle is held in both hands with its fixed spike bayonet toward upper left.

There was no prior Soviet reference. The existing German candidate 4 raw supplied only the initial camera/rendering reference; Soviet clothing and gear were generated from the character brief. Candidates 2–4 use corrected Soviet candidate 1 as their visual reference. Fine buckle, collar and pouch details remain small or partly occluded in the overhead pose. The prompt's approximate six-helmet rifle-length target was not attained exactly; the final diagonal rifle/bayonet is about 165 px long and remains visibly longer than the existing German rifle. The mandatory helmet scale and frame/pivot requirements take precedence over that supplemental prompt target. One correction request was rejected by the image service; a retry clarifying the nonviolent miniature context succeeded.

### Final generation prompt set

Candidate 1 was corrected from the archived initial render using:

```text
This is a nonviolent historical model miniature for a strategy-game asset, standing at rest, with no combat, harm, or injury depicted.
Use case: precise-object-edit. Correct this Soviet rifleman sprite to make the definitive candidate 1. Preserve exact 90-degree vertical orthographic overhead camera, upright standing pose with feet together facing TOP, realistic desaturated miniature textures, same khaki-brown tunic, matte olive round SSh-40 dome, dark boots, drawstring duffel.
Three REQUIRED corrections:
1. The figure faces UP, so HIS LEFT is IMAGE LEFT. Put the thick grey-brown greatcoat roll OVER THE IMAGE-LEFT SHOULDER. It curves as a hoop diagonally across his back to IMAGE-RIGHT HIP. Do NOT put the upper coil on image right as in input.
2. Move the khaki gas-mask bag AND canvas-covered entrenching shovel together to IMAGE-LEFT HIP, visibly exposed at lower left. Keep drawstring duffel at central back, with two straps over shoulders. Two brown front ammo pouches, brown belt and brass buckle. No extra bags.
3. Correct the LONG Mosin-Nagant 1891/30 rifle silhouette and true relative length. Wood butt at LOWER RIGHT; receiver and hands across front of torso; long wood fore-end then exposed dark barrel continuing toward UPPER LEFT, then a long fine straight SPIKE BAYONET further upper left. The complete rifle plus bayonet length must be SIX times helmet WIDTH. Do not compress the weapon. Forearm supports stock, right hand at grip. Straight weapon, round bolt knob, a thin spike not a broad triangular blade.
COMPOSITION: greatly reduce figure size relative to square canvas to fit that longer rifle fully with transparent margins. Helmet WIDTH about 15 percent of square image, shoulders 26 percent, entire compact foreshortened BODY footprint about 40 percent. Helmet crown and shoulders mostly conceal torso and legs. Center torso in canvas. Paired north-pointing boot toes visible forward of body. DO NOT elongate back or legs. Rifle total length about 90 percent of canvas, diagonal pointing upper left, fully inside frame.
No face, no perspective, no three-quarter angle. True transparent background, clean alpha. Soft even light. No cast shadows, ground, haze, letters, symbols, stars, insignia, flags or watermarks. One figure only.
```

Candidates 2–4 used candidate 1 as the image reference and this shared prompt:

```text
Use case: identity-preserve. This is a nonviolent historical miniature standing at rest, for a game sprite. The input is the approved SOVIET RIFLEMAN character and pose reference. Render one subtly different candidate of this EXACT figure.
Preserve camera, silhouette, pose, proportions, framing, colors and every piece of equipment exactly. Strict orthographic 90-degree vertical overhead; see helmet crown and shoulder tops; no face; compact foreshortened upright body; no long legs. Facing UP/NORTH, feet together, two black boot toes forward of torso. Both hands hold same long wooden Mosin-Nagant 1891/30 with thin spike bayonet diagonally, bayonet toward UPPER LEFT.
Keep matte olive round SSh-40 helmet, faded dusty khaki-brown tunic/trousers, brown-grey greatcoat roll over IMAGE-LEFT shoulder looping to RIGHT hip, central rear drawstring duffel with two shoulder straps, gas-mask canvas bag and covered entrenching shovel at IMAGE-LEFT HIP, brown belt and two ammunition pouches. Keep same dimensions and all relative placements. No extra gear. Maintain full rifle and bayonet in frame. Keep exact desaturated realistic painterly-photographic miniature style and soft even overcast lighting.
Only the small rendering variation stated below may differ. Do not redesign or move equipment. Transparent alpha background, clean edges, no ground, no cast shadow or haze. No text, symbols, insignia, stars, flags, borders or watermark. One figure only.
```

Texture-only variations:

1. Candidate 2: Slightly softer fine cloth grain on the tunic; keep its exact color.
2. Candidate 3: Subtly clearer cloth folds and seam definition on the coat roll and gathered duffel; same colors and geometry.
3. Candidate 4: Slightly more even matte paint texture on the helmet dome; keep its exact outline and olive color.
