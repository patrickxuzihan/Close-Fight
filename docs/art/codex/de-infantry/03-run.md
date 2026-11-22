# Task 03: Run cycle (2 frames) — German rifleman

Before starting, read `docs/art/codex/00-style-guide.md` and `docs/art/codex/de-infantry/00-character.md`.
Use `ArtSource/codex/de_infantry/de_rifleman_01_stand.png` as the visual reference for every generation in this task.
If that reference image does not exist, stop and say so.

## Frames

- `de_rifleman_04_run_a.png`: run A: running north, **left foot forward**, torso leaning forward, rifle held across the chest.
- `de_rifleman_05_run_b.png`: run B: the same, **right foot forward**.

## Notes

- Generate frames A and B together in one 512 × 256 image (A left, B right), then slice and post-process them as the style guide describes.
- Apart from the limbs that the frame descriptions say move, A and B must be identical: same torso position, same helmet position, same rifle, same colors.
- The pivot (center of the torso) is at exactly (128, 128) in both frames.
- The stride is longer than walking: about 1.0 m between the feet (about 128 px). The torso leans forward, so the helmet sits slightly ahead (north) of the shoulders.

## Output

Save into `ArtSource/codex/de_infantry/`:
- `de_rifleman_04_run_a.png` (and the raw generation next to it as `de_rifleman_04_run_a_raw.png`)
- `de_rifleman_05_run_b.png` (and the raw generation next to it as `de_rifleman_05_run_b_raw.png`)
- Append an entry to `ArtSource/codex/de_infantry/notes.md`.

Finish by reporting the style guide's self-check for each file.
