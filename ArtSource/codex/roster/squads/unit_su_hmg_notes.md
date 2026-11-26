# R5-05: Soviet heavy machine gun team

Date: 2026-11-26 (Australia/Melbourne).

## Files
- `unit_su_hmg.png`: final 1536 × 1024, 8-bit RGBA, transparent background.
- `unit_su_hmg_raw.png`: unchanged built-in image generation output, retained before alpha cleanup.
- `unit_su_hmg_preview.png`: RGB olive (153, 158, 117) comparison with the required German rifle reference; both at native 76 × 54 and 3× (228 × 162).
- `unit_su_hmg_notes.md`: this record.

## References and method
Used `ArtSource/codex/roster/squad_test/de_rifle_4_raw.png` as the input reference for rendering, soft light, camera, and framing. The German HMG counterpart `unit_de_hmg.png` was absent when work began.

Generated with the built-in imagegen tool, transparent background requested. No CLI fallback. One generation, no refusals. Generated image identifier: `exec-071e86f0-cb26-4c76-aef6-e8b7560cdf90`.

The squad guide overrides the general guide's top-down camera, 256 × 256 canvas, helmet scale and pivot requirements. This is a knee-up roster portrait, not a battlefield sprite. Final image was not cropped, resized, repainted or recomposed. Pillow/NumPy cleanup mapped alpha ≤8 to 0 and ≥245 to 255, retaining a smooth edge ramp between them; fully transparent RGB was cleared. This removes residual low-alpha haze and makes cloth, faces and equipment opaque. The untouched raw generation is retained.

Preview follows the squad guide's crop: top 2% of image height above the first solid foreground row (clamped at image top), bottom at the image bottom, width = remaining height × 76 / 54, horizontally centred on the foreground. Both scales use Lanczos downsampling and alpha composition on the required olive colour.

## Automated checks
```json
{
  "dimensions": [
    1536,
    1024
  ],
  "mode": "RGBA",
  "foreground_coverage_percent": 81.74,
  "alpha_weighted_coverage_percent": 81.74,
  "top_opaque_pixel": 3,
  "top_margin_percent": 0.29,
  "bottom_edge_coverage_percent": 60.03,
  "fully_transparent_pixels": 280620,
  "fully_opaque_pixels": 1277825,
  "antialiased_edge_pixels": 14419,
  "frame_contact": {
    "left": true,
    "right": false,
    "bottom": true
  }
}
```

## Visual self-check
- Exactly four Soviet men, each with a different face, build and carrying posture.
- Men 1 and 2 carry the same complete Maxim together at waist height.
- Man 3 carries a visibly separate steel shield on his back and one ammunition box.
- Man 4 carries two ammunition boxes, one in each hand.
- Plain khaki-brown Soviet clothing, rounded green steel helmets and diagonal rolled greatcoats; no insignia, text, badges, flags, stars or political symbols.
- One thick ribbed water-cooled Maxim; no other weapons or curved magazines.
- All men stand/walk; helmets near the top, legs cut at the lower edge, no feet.
- Foreground coverage exceeds 70%; bottom contact exceeds one third.
- Realistic detail, subdued colour, camera and even lighting match the reference.
- No hard cast shadows or ground. Background removed through alpha.
- Small olive-tile preview: passed visual inspection at 76 × 54 and 228 × 162. Four separate heads remain visible; the thick horizontal Maxim, the shield and ammunition boxes distinguish this team. The game crop trims the outside of the far-right ammunition box, but its separate handle and box remain identifiable. The un-cropped final preserves the complete box.

## Limitations
The counterpart could not be matched because it did not exist. No game-code or UI changes were requested or made; build checks are not applicable to these standalone PNGs. All requested squad self-checks passed visual and image-metadata review. PNG decoding and 8-bit output were verified, and the saved raw file is byte-identical to the generated source.

## Generation prompt
Use case: historical-scene.
Create one polished game roster portrait for Close Fight, a 1942 Soviet heavy machine gun team. The supplied German rifle squad image is a STYLE, LIGHTING, CAMERA AND CLOSE-FRAMING reference only. Change the people, uniforms and equipment to the exact Soviet team specified below. Match its realistic finely detailed 3D rendering, subtly dusty worn cloth and steel, lifelike individual faces, soft even upper-front illumination, and tight knee-up portrait composition. Request landscape 1536 x 1024 PNG with real transparent background.

EXACTLY FOUR Soviet men STANDING AND WALKING TOGETHER, close shoulder to shoulder, slight overlaps, facing the viewer and a little to the viewer's right. Chest-height camera slightly from the front left. Keep all four faces and helmets clearly separate, different ages, faces and builds, and different convincing carrying poses. Their rounded green SSh-40 helmets almost touch the top edge (0 to 3% margin). The men fill the entire width and more than 70% of the whole image, and the bottom edge cuts through all their legs around their knees, with the legs occupying more than one third of the bottom edge. No feet. No kneeling or lying figures. NO extra border or empty space above or below.

LEFT TO RIGHT, each man has exactly this job:
1. A sturdy older soldier and
2. a younger lean soldier together carry ONE complete detached Maxim M1910 gun assembly horizontally BETWEEN THEM at WAIST HEIGHT by its carrying handles. One carrier securely grips the rear carrying handle near the receiver, the other securely grips the forward carrying handle near the front, both visibly supporting the same heavy gun. Make the water-cooled Maxim unmistakable: long thick ribbed cylindrical barrel jacket, short muzzle, heavy rectangular receiver and rear spade grips, dark worn steel. Keep the whole gun inside the frame, laid horizontally across the left two men's waists, muzzle pointing to image right. It is a carried load, nobody is aiming or firing. The gun's shield is removed and carried by man 3; do not attach the shield to the gun. No tripod or wheels.
3. A broad middle-aged man carries the detached Maxim STEEL GUN SHIELD strapped onto his BACK. Show an unmistakably broad flat dark olive steel shield with rounded upper corners and central gun aperture projecting visibly behind one shoulder while his head remains fully visible. He carries ONE olive metal ammunition box by its handle in one hand, lifted enough that the box is visible above the knee crop.
4. A younger slim ammunition bearer carries TWO separate olive metal ammunition boxes, ONE IN EACH HAND, handles gripped naturally, boxes lifted enough that both boxes and both supporting hands are visible above the knee crop.

All four men wear plain khaki-brown 1942 Soviet tunics and trousers, dark leather belts, rounded green SSh-40 helmets, and rolled brown-grey greatcoats worn diagonally over a shoulder. Different natural walking stances while keeping shoulder overlap and dense framing. Small natural wear and dust. Plain clothing and plain equipment with ABSOLUTELY NO insignia, badges, medals, stars, ribbons, armbands, flags, logos, readable text or political symbols. No other guns, no modern equipment, no curved magazines. No background, ground, floor, cast shadow, scenery, smoke, vignette or haze. True transparent alpha everywhere outside the men and their equipment. Preserve crisp natural edges. This is one group portrait, not four panels, no labels, no borders.
