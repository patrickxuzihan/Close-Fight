# Task 07: Grenade throw (2 frames) — Soviet rifleman

Before starting, read `docs/art/codex/00-style-guide.md` and `docs/art/codex/su-infantry/00-character.md`.
Use `ArtSource/codex/su_infantry/su_rifleman_01_stand.png` as the visual reference for every generation in this task.
If that reference image does not exist, stop and say so.

## Frames

- `su_rifleman_10_throw_a.png`: throw A: standing, facing north. The **right arm is raised behind the head** holding an RGD-33 stick grenade. The left arm points forward for balance. The rifle is slung on the back.
- `su_rifleman_11_throw_b.png`: throw B: the right arm has **swung forward over the head**, and the grenade has just left the hand (draw the grenade a little ahead of the hand, north). The torso is rotated slightly forward.

## Notes

- Generate frames A and B together in one 512 × 256 image (A left, B right), then slice and post-process them as the style guide describes.
- Apart from the limbs that the frame descriptions say move, A and B must be identical: same torso position, same helmet position, same slung rifle, same colors.
- The pivot (center of the torso) is at exactly (128, 128) in both frames.

## Output

Save into `ArtSource/codex/su_infantry/`:
- `su_rifleman_10_throw_a.png` (and the raw generation next to it as `su_rifleman_10_throw_a_raw.png`)
- `su_rifleman_11_throw_b.png` (and the raw generation next to it as `su_rifleman_11_throw_b_raw.png`)
- Append an entry to `ArtSource/codex/su_infantry/notes.md`.

Finish by reporting the style guide's self-check for each file.
