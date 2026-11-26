# R5-04 — German heavy machine gun team

Date: 2026-11-26 (Australia/Melbourne)

## Result

Blocked by two image-service refusals. The task's shared squad guide explicitly requires stopping after the second refusal; no further generation was attempted.

Read `docs/art/codex/00-style-guide.md` first, then `docs/art/codex/roster/squads/04-de-hmg.md` and `00-squad-guide.md`. Applied the squad guide's chest-height, close helmet-to-knee portrait rules; the general sprite camera, canvas, scale and pivot rules do not apply.

Used the built-in image generation tool (not the CLI). Inspected and supplied `ArtSource/codex/roster/squad_test/de_rifle_4_raw.png` as the rendering, lighting, camera and framing reference for both requests. The counterpart `ArtSource/codex/roster/squads/unit_su_hmg.png` did not exist when checked, so no counterpart was supplied.

## Files

Created only `ArtSource/codex/roster/squads/unit_de_hmg_notes.md`.

No `unit_de_hmg.png`, `unit_de_hmg_raw.png` or `unit_de_hmg_preview.png` was created: neither request returned an image. No other project files were modified. No game build was run because this is an art-only task and generation was refused.

## Refusals

1. Initial detailed request: HTTP 400, `moderation_blocked`, `safety_violations=[abuse]`, moderation stage `output`, category `other`.
   Request ID: `b55b085b-a7cc-41dd-86f1-65c40653e705`.
2. One retry with simpler wording, as permitted by the squad guide: HTTP 400, `moderation_blocked`, `safety_violations=[abuse]`, moderation stage `output`, category `other`.
   Request ID: `9ebed442-9d34-469d-905f-1a70f9d6d935`.

Both responses said: “Your request was rejected by the safety system.” No more specific explanation or image was provided.

## Prompt 1

```text
Use case: historical-scene.
Asset type: one transparent German heavy machine-gun team roster portrait for the WWII tactics game Close Fight, September 1942. Requested canvas 1536 x 1024, landscape, RGBA.
Input image 1 is a REFERENCE for the realistic 3D rendering, worn cloth and metal materials, soft upper-front light, camera, dense close framing and level of detail. Make a new four-man HMG team. The four roles below replace the reference roles.

Exactly FOUR distinct adult men, standing and moving forward together, shoulder to shoulder and slightly overlapping, facing the viewer and slightly to image-right. Camera at chest height from slightly front-left, same as the reference. Unique faces, ages, builds and different purposeful carrying poses; natural human anatomy and separate correctly gripping hands. The left-to-right order is:
1. Gunner: carrying a COMPLETE MG34 over his shoulder, one hand supporting its grip. Recognizable long cylindrical PERFORATED barrel jacket, receiver, wooden buttstock and folded bipod, entire gun inside the picture. The gun runs roughly across his shoulder, its barrel angled slightly to the right, not upward past the top edge.
2. Assistant: carrying a FOLDED heavy MG tripod on his BACK, one hand firmly holding its shoulder strap across his chest. Make the folded metal tripod visibly identifiable behind one shoulder with its compact folded three-leg framework, mounting head and hardware. It is carried securely with straps; not erected, not a weapon, not held loose in front.
3. Ammunition bearer: carrying TWO metal ammunition boxes, one gripped by its handle in EACH hand, visible on opposite sides of his legs. Broad, older face and stockier build.
4. Ammunition bearer: holding ONE metal ammunition box by its handle in one hand, and a COMPLETE wooden-stock Kar98k bolt-action rifle in the OTHER hand, upright at a slight diagonal, with visible muzzle, action and stock and secure grip. His pose differs from man 3.

All four wear plain slightly dusty field-grey wool tunics and trousers, grey-green M35 steel helmets with flared rims, plain leather belts, Y-straps and ammunition pouches. Absolutely NO insignia of any kind: blank helmets, blank collars, blank shoulder straps and blank buckles; no eagles, badges, medals, ribbons, stars, armbands, flags, text or political symbols. No swastikas. No modern weapons, no curved magazines, no MP40, no binoculars.
Close framing is essential: highest helmets only 0–3% below top edge, no tall equipment above the helmets; shoulders fill the image edge to edge with minimal gaps; bottom edge cuts through the men's trousered legs around the knees, no feet. Legs intersect the bottom edge across at least one third of the image width. The men and their carried equipment occupy at least 70% of the entire canvas. Keep all four heads and the distinguishing MG34, folded tripod, three ammunition boxes and Kar98k inside the central 76:54 crop. Box bodies may end at the lower crop but their handles and recognizable box shapes must show clearly.
Fine detailed realistic 3D render like the reference, desaturated natural field-grey palette, even soft upper-front lighting, no hard cast shadows. GENUINELY FULLY TRANSPARENT background with clean alpha edges, including gaps around arms and equipment. No scenery, no ground plane, no painted backdrop, no checkerboard, no cast shadows, no labels, no border, no watermark. This is a quiet equipment-carrying portrait, not combat or firing.
```

## Prompt 2 — single simplified retry

```text
Create a transparent-background historical game roster illustration of exactly four German infantrymen in 1942, using the supplied rifle-squad image as the reference for realistic 3D rendering, soft lighting, chest-height front-left camera and tight framing.

They stand shoulder to shoulder, walking forward and looking slightly right. Left to right: an MG34 gunner carrying the complete machine gun over his shoulder; a man carrying a folded machine-gun tripod on his back and holding its strap; a man carrying a metal ammunition box in each hand; and a man carrying one ammunition box in one hand and a wooden-stock Kar98k rifle in the other. Give them four different faces and carrying poses. Show the folded tripod clearly.

Plain dusty field-grey uniforms, grey-green M35 helmets, leather belts and straps. No insignia, symbols, flags or text. All equipment is carried, with no firing or combat.

Landscape 1536 x 1024. Helmets almost touch the top edge. The four men fill the width and at least 70% of the image; the bottom edge cuts their legs around the knees, with no feet. Keep heads and equipment inside the central 76:54 crop. Fully transparent background including the gaps between the men, no ground, no cast shadows, no border. Match the reference's materials, detail and close framing.
```

For both calls: `transparent_background=true`, with the reference image path stated above.

## Self-check

All image checks are **not completed / unverifiable**, because no output image was returned:

- Four distinct men, each performing the specified role: not verifiable.
- No insignia or symbols: not verifiable.
- Correct, complete, properly held weapons, with no curved magazines: not verifiable.
- Close helmet-to-knee framing, no prone men: not verifiable.
- At least 70% frame coverage and sufficient bottom-edge contact: not verifiable.
- Rendering, light and framing matching the reference: not verifiable.
- Separate men and identifiable HMG team on the 76 × 54 olive tile: not verifiable.
- RGBA transparency and 1×/3× preview: not verifiable / not produced.

Remaining work: obtain an accepted generation, inspect it, preserve the raw if cleaned, produce the final transparent PNG and olive comparison preview, and complete the full checklist. No finished asset is claimed.
