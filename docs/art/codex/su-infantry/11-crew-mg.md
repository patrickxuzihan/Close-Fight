# Task 11: Machine gunner (1 frame) — Soviet rifleman

Before starting, read `docs/art/codex/00-style-guide.md` and `docs/art/codex/su-infantry/00-character.md`.
Use `ArtSource/codex/su_infantry/su_rifleman_01_stand.png` as the visual reference for every generation in this task.
If that reference image does not exist, stop and say so.

## Frames

- `su_rifleman_16_crew_mg.png`: lying prone behind a **DP-28 light machine gun** on its bipod. The gun points north, the bipod legs are spread on the ground ahead, the flat round pan magazine sits on top of the gun (clearly visible from above), the right hand is on the grip and the left hand rests on the stock.

## Notes

- Pivot: hips at (128, 128). The gun's muzzle may extend toward the top of the canvas but must stay inside it.
- Same soldier and uniform as the reference; his own rifle is slung on his back.

## Output

Save into `ArtSource/codex/su_infantry/`:
- `su_rifleman_16_crew_mg.png` (and the raw generation next to it as `su_rifleman_16_crew_mg_raw.png`)
- Append an entry to `ArtSource/codex/su_infantry/notes.md`.

Finish by reporting the style guide's self-check for each file.
