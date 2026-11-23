# Task 01: Standing reference frame — Soviet rifleman

Before starting, read `docs/art/codex/00-style-guide.md` and `docs/art/codex/su-infantry/00-character.md`.

**This task creates the reference image for the whole character.** No reference exists yet. Every later frame will be drawn to match this one, so getting it right matters more than speed.

## Frames

- `su_rifleman_01_stand.png`: standing still, facing north. The rifle (Mosin-Nagant with bayonet) is held diagonally across the chest in both hands, muzzle toward the upper left. Feet are together and the shoulders are square. Every piece of equipment listed in `00-character.md` is visible from above:
  - the helmet dome
  - the rolled greatcoat over the left shoulder
  - the duffel bag on the back
  - the gas mask bag and entrenching tool at the left hip

## Method

1. Generate **four candidates**. Each one follows the character description and the style guide exactly; they differ only in small rendering details.
2. Post-process each candidate as the style guide describes (helmet 34 px wide, center of the torso at (128, 128), 256 × 256, transparent).
3. Save them as `su_rifleman_01_stand_candidate_1.png` … `_4.png`.
4. Pick the candidate that best matches the style guide and save a copy of it as `su_rifleman_01_stand.png`. In `notes.md`, write which one you picked and why.

The project owner may later swap in a different candidate, so keep all four.

## Output

Save into `ArtSource/codex/su_infantry/`:
- `su_rifleman_01_stand_candidate_1.png` … `su_rifleman_01_stand_candidate_4.png`, each with its `_raw.png`
- `su_rifleman_01_stand.png` (a copy of the chosen candidate)
- `notes.md`: create it, and add this task's entry

Finish by reporting the style guide's self-check for each file.
