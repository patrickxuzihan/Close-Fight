# Task 05: Prone (1 frame) — Soviet rifleman


> **Camera check (added 2026-11-24 after a rejected delivery):** the soldier lies **face down** and we look at his **back** from directly above: helmet top, back of the tunic or coat, the pack, the backs of the legs, boot soles toward the bottom edge. **No face, chest, buttons or belt buckle may be visible.** A front view of a standing figure is not a prone pose.

Before starting, read `docs/art/codex/00-style-guide.md` and `docs/art/codex/su-infantry/00-character.md`.
Use `ArtSource/codex/su_infantry/su_rifleman_01_stand.png` as the visual reference for every generation in this task.
If that reference image does not exist, stop and say so.

## Frames

- `su_rifleman_07_prone.png`: lying flat on the stomach, head north, legs straight and slightly apart. Both hands hold the rifle forward, muzzle pointing north.

## Notes

- Pivot: hips at (128, 128). Helmet to boot tips is about 230 px.
- This frame is the base for the crawl task, so keep the body straight and symmetrical.

## Output

Save into `ArtSource/codex/su_infantry/`:
- `su_rifleman_07_prone.png` (and the raw generation next to it as `su_rifleman_07_prone_raw.png`)
- Append an entry to `ArtSource/codex/su_infantry/notes.md`.

Finish by reporting the style guide's self-check for each file.
