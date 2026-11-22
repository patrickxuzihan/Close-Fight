# Task 06: Crawl cycle (2 frames) — German rifleman

Before starting, read `docs/art/codex/00-style-guide.md` and `docs/art/codex/de-infantry/00-character.md`.
Use `ArtSource/codex/de_infantry/de_rifleman_01_stand.png` as the visual reference for every generation in this task.
If that reference image does not exist, stop and say so.

## Frames

- `de_rifleman_08_crawl_a.png`: crawl A: crawling north on the stomach. **Left knee drawn up** toward the hip, **right arm reaching forward**. The rifle is cradled across the forearms.
- `de_rifleman_09_crawl_b.png`: crawl B: mirror of A. **Right knee drawn up**, **left arm reaching forward**.

## Notes

- Generate frames A and B together in one 512 × 256 image (A left, B right), then slice and post-process them as the style guide describes.
- Apart from the limbs that the frame descriptions say move, A and B must be identical: same torso position, same helmet position, same rifle, same colors.
- The pivot (hips) is at exactly (128, 128) in both frames.
- If `de_rifleman_07_prone.png` exists, also use it as a reference: the crawl frames are the prone pose with limbs moved.

## Output

Save into `ArtSource/codex/de_infantry/`:
- `de_rifleman_08_crawl_a.png` (and the raw generation next to it as `de_rifleman_08_crawl_a_raw.png`)
- `de_rifleman_09_crawl_b.png` (and the raw generation next to it as `de_rifleman_09_crawl_b_raw.png`)
- Append an entry to `ArtSource/codex/de_infantry/notes.md`.

Finish by reporting the style guide's self-check for each file.
