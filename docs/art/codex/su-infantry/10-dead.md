# Task 10: Dead (2 frames) — Soviet rifleman

Before starting, read `docs/art/codex/00-style-guide.md` and `docs/art/codex/su-infantry/00-character.md`.
Use `ArtSource/codex/su_infantry/su_rifleman_01_stand.png` as the visual reference for every generation in this task.
If that reference image does not exist, stop and say so.

## Frames

- `su_rifleman_14_dead_a.png`: dead A: lying **face down**, limbs splayed, head roughly north. The rifle lies a short distance away. No gore, no blood pools.
- `su_rifleman_15_dead_b.png`: dead B: a **different** pose: lying twisted on his side, one leg bent, one arm under the body. The rifle lies at a different angle. No gore, no blood pools.

## Notes

- These two are variants, not an animation, so they **should look different**. Generate them together in one 512 × 256 image anyway, for a consistent uniform and scale.
- Pivot: hips at (128, 128) in both.

## Output

Save into `ArtSource/codex/su_infantry/`:
- `su_rifleman_14_dead_a.png` (and the raw generation next to it as `su_rifleman_14_dead_a_raw.png`)
- `su_rifleman_15_dead_b.png` (and the raw generation next to it as `su_rifleman_15_dead_b_raw.png`)
- Append an entry to `ArtSource/codex/su_infantry/notes.md`.

Finish by reporting the style guide's self-check for each file.
