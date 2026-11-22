# Task 01: Standing reference frame — German rifleman

Before starting, read `docs/art/codex/00-style-guide.md` and `docs/art/codex/de-infantry/00-character.md`.

**This task creates the reference image for the whole character.** No reference exists yet. Every later frame will be drawn to match this one, so getting it right matters more than speed.

## Frames

- `de_rifleman_01_stand.png`: standing still, facing north. The rifle (Kar98k) is held diagonally across the chest in both hands, muzzle toward the upper left. Feet are together and the shoulders are square. Every piece of equipment listed in `00-character.md` is visible from above:
  - the helmet dome
  - the Y-straps over the shoulders
  - the gas mask canister on the back
  - the bread bag, canteen and entrenching tool at the hips

## Method

1. Generate **four candidates**. Each one follows the character description and the style guide exactly; they differ only in small rendering details.
2. Post-process each candidate as the style guide describes (helmet 34 px wide, center of the torso at (128, 128), 256 × 256, transparent).
3. Save them as `de_rifleman_01_stand_candidate_1.png` … `_4.png`.
4. Pick the candidate that best matches the style guide and save a copy of it as `de_rifleman_01_stand.png`. In `notes.md`, write which one you picked and why.

The project owner may later swap in a different candidate, so keep all four.

## Output

Save into `ArtSource/codex/de_infantry/`:
- `de_rifleman_01_stand_candidate_1.png` … `de_rifleman_01_stand_candidate_4.png`, each with its `_raw.png`
- `de_rifleman_01_stand.png` (a copy of the chosen candidate)
- `notes.md`: create it, and add this task's entry

Finish by reporting the style guide's self-check for each file.
