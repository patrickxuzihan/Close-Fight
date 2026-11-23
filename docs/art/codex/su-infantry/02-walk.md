# Task 02: Walk cycle (2 frames) — Soviet rifleman

Before starting, read `docs/art/codex/00-style-guide.md` and `docs/art/codex/su-infantry/00-character.md`.
Use `ArtSource/codex/su_infantry/su_rifleman_01_stand.png` as the visual reference for every generation in this task.
If that reference image does not exist, stop and say so.

## Frames

- `su_rifleman_02_walk_a.png`: walk A: walking north at a normal pace, **left foot forward**, right foot back. The rifle is held diagonally across the chest in both hands, the same way as in the reference.
- `su_rifleman_03_walk_b.png`: walk B: the same, **right foot forward**, left foot back.

## Notes

- Generate frames A and B together in one 512 × 256 image (A left, B right), then slice and post-process them as the style guide describes.
- Apart from the limbs that the frame descriptions say move, A and B must be identical: same torso position, same helmet position, same rifle, same colors.
- The pivot (center of the torso) is at exactly (128, 128) in both frames.
- Seen from above, only the feet and lower legs show ahead of and behind the torso. The stride is short: about 0.6 m between the feet (about 77 px).

## Output

Save into `ArtSource/codex/su_infantry/`:
- `su_rifleman_02_walk_a.png` (and the raw generation next to it as `su_rifleman_02_walk_a_raw.png`)
- `su_rifleman_03_walk_b.png` (and the raw generation next to it as `su_rifleman_03_walk_b_raw.png`)
- Append an entry to `ArtSource/codex/su_infantry/notes.md`.

Finish by reporting the style guide's self-check for each file.
