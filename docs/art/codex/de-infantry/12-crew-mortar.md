# Task 12: Mortar crewman (1 frame) — German rifleman

Before starting, read `docs/art/codex/00-style-guide.md` and `docs/art/codex/de-infantry/00-character.md`.
Use `ArtSource/codex/de_infantry/de_rifleman_01_stand.png` as the visual reference for every generation in this task.
If that reference image does not exist, stop and say so.

## Frames

- `de_rifleman_17_crew_mortar.png`: crouched just behind and to the left of an **8 cm mortar (8 cm GrW 34)**: tube, bipod and square baseplate. The tube leans north. Both his hands hold a mortar round at the muzzle, about to drop it in.

## Notes

- Pivot: center of the soldier's torso at (128, 128). The mortar sits ahead of him (north and slightly east) and must stay inside the canvas.
- Same soldier and uniform as the reference; his rifle is slung on his back.

## Output

Save into `ArtSource/codex/de_infantry/`:
- `de_rifleman_17_crew_mortar.png` (and the raw generation next to it as `de_rifleman_17_crew_mortar_raw.png`)
- Append an entry to `ArtSource/codex/de_infantry/notes.md`.

Finish by reporting the style guide's self-check for each file.
