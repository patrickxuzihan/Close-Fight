# Task R1: the roster's head marker

Before starting, read `docs/art/codex/00-style-guide.md`. Only its rules on forbidden symbols, keeping raw files, `notes.md` and reporting apply.

**Output folder:** `ArtSource/codex/roster/`

## What it is

Each row of the roster shows a unit's picture and name. Beside the name, the game draws one small head per man and tints each head by that man's state:

- green: unhurt;
- yellow: lightly wounded;
- light red: badly wounded;
- dark red: dead;
- grey: gone.

The unit pictures themselves are colour renders made elsewhere (see `docs/art/roster-pictures.md`). This task is only the head.

## The file

`head.png`:

- 40 × 52 px, RGBA.
- **Pure white and flat** (no shading) on a transparent background, so the game can tint it any colour.
- A simple head-and-shoulders silhouette wearing a steel helmet that could belong to either side:
  - a rounded dome;
  - no German flare;
  - no Soviet brim.
- It must still read as a little soldier at **8 × 11 px**, the size the roster draws it.

## Steps

1. Generate it, then clean it to pure white on transparent, with soft alpha only at the edges.
2. Make `head_preview.png` on dark grey (RGB 51, 51, 48):
   - eleven heads in a row at 8 × 11 px, tinted green, yellow, light red, dark red and grey;
   - the same row again at 16 × 22 px.
3. Self-check: is the head pure white, and does it read as a soldier at 8 × 11?
