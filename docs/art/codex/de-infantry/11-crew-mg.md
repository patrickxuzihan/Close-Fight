# Task 11: Machine gunner (1 frame) — German rifleman

Before starting, read `docs/art/codex/00-style-guide.md` and `docs/art/codex/de-infantry/00-character.md`.
Use `ArtSource/codex/de_infantry/de_rifleman_01_stand.png` as the visual reference for every generation in this task.
If that reference image does not exist, stop and say so.

## Frames

- `de_rifleman_16_crew_mg.png`: lying prone behind an **MG 34 machine gun** on its bipod. The gun points north, the bipod legs are spread on the ground ahead, the right hand is on the grip, the left hand rests on the stock, and an ammunition belt feeds in from the left side.

## Notes

- Pivot: hips at (128, 128). The gun's muzzle may extend toward the top of the canvas but must stay inside it.
- Same soldier and uniform as the reference; his own rifle is slung on his back.

## Output

Save into `ArtSource/codex/de_infantry/`:
- `de_rifleman_16_crew_mg.png` (and the raw generation next to it as `de_rifleman_16_crew_mg_raw.png`)
- Append an entry to `ArtSource/codex/de_infantry/notes.md`.

Finish by reporting the style guide's self-check for each file.
