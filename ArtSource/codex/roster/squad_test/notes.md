# R4 (test) — Rifle squad pictures, close framing — 2026-11-26

- Read `docs/art/codex/00-style-guide.md` first, then `roster/04-squad-test.md`; used the task's front three-quarter, close helmet-to-knee framing rules.
- Used the built-in image generation tool, one attempt per German picture, in the requested order.
- `de_rifle_4`: generation succeeded. Saved the unchanged service output as `de_rifle_4_raw.png` (1536 × 1024, RGBA). Alpha ranges from 0 to 254; background samples have alpha 0. The backdrop seen in the tool's inline rendering is not an opaque background in the saved PNG.
- `de_rifle_6`: refused with HTTP 400, `moderation_blocked`, `safety_violations=[abuse]`, moderation stage `output`, category `other`. Request ID: `a477cd4d-aa0e-4721-9a47-e0c17beb6521`. The service supplied no more specific explanation and returned no image.
- Stopped the whole task immediately after this refusal. No retry, no Soviet requests, no further image processing.
- Files made: `de_rifle_4_raw.png` and this `notes.md`. No final 608 × 432 PNGs or `squad_test_preview.png` were made; no raw image exists for the refused request.
- Preliminary visual inspection of the successful raw: four distinct men and roles, blank uniforms/helmets without visible insignia, straight MP40 magazine, identifiable perforated-jacket MG34 with folded bipod, two ammunition boxes, and a Kar98k. Helmets are close to the top and the legs end near the knees. Weapon completeness/crop suitability, exact 70% figure coverage and 76 × 54 olive-tile readability were not fully verified.
- Final checklist: **not completed**. There are no final pictures to certify; do not treat the raw as a finished asset.
- Project writes were confined to `ArtSource/codex/roster/squad_test/`. No build was run: this was an art-only task, halted at the explicit refusal condition.

## Prompt set

- **de_rifle_4:** The existing German rifle-squad image was supplied only as a material/rendering reference. Requested exactly four plain, unmarked 1942 German infantrymen, realistic studio render, true transparent alpha, chest-height front-left view, shoulder-to-shoulder helmet-to-knee crop, at least 70% coverage. Left to right: binocular leader with slung straight-magazine MP40; MG34 gunner carrying it on his right shoulder; assistant with two metal ammunition boxes; Kar98k rifleman. Explicitly excluded all insignia, political symbols, curved magazines, ground and background shadows.
- **de_rifle_6:** The successful four-man raw was the reference. Requested the same front four with their identities, equipment, order, scale and light preserved, plus exactly two partly hidden rear Kar98k riflemen with distinct readable heads, shoulders and rifles. Repeated the tight framing, true transparency, permitted weapons and exclusions. This single attempt was refused.
