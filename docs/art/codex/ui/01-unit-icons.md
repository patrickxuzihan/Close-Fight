# Task U1: unit symbols (NATO style, hand-drawn ink)

Before starting, read `docs/art/codex/00-style-guide.md` (only its rules on forbidden symbols, keeping raw files, `notes.md` and reporting apply here; this is not a top-down sprite).
Output folder: `ArtSource/codex/ui/`

Unit symbols for the team cards at the bottom of the screen and, later, the 1942-style campaign map (design decision D42: **NATO standard military symbols, drawn by hand in ink**, to suit a paper map).

## Style

- Black ink on a **transparent background**, as if drawn with a dip pen on paper: slightly uneven line weight, a little ink wobble, crisp but not computer-perfect.
- Each symbol is the standard NATO land-unit **rectangle frame** (ratio 1.5 : 1) with the function symbol inside.
- **256 × 256 px**, the frame about 200 × 134 px, centered. Line about 7–9 px thick.
- Plain black ink only. The game colors the symbols per side, so do not add color.
- No text, no national symbols.

## Symbols

| File | Unit | Inside the frame |
|---|---|---|
| `icon_infantry.png` | Rifle squad | The infantry cross: two diagonals corner to corner (X) |
| `icon_machinegun.png` | Machine-gun team | Infantry X, plus a small machine-gun weapon mark below the frame (an upward arrow with two short crossbars) |
| `icon_mortar.png` | Mortar team | Infantry X, plus a mortar weapon mark below the frame (an upward arrow standing on a small circle) |
| `icon_antitank.png` | Anti-tank gun | An inverted V (the anti-armour mark) from the bottom corners to the top center |
| `icon_headquarters.png` | Company headquarters | Infantry X, with the headquarters staff: a vertical line down from the frame's bottom-left corner, about half the frame height long |
| `icon_armor.png` | Tank | The armour mark: a horizontal oval (track) in the middle of the frame |
| `icon_assaultgun.png` | Assault gun | The armour oval, plus a short horizontal gun line from the oval's right side to the frame |

Also save a preview `icons_sheet.png`: all seven symbols in a row on an off-white paper-colored background, so the ink style can be judged.

Self-check: 256 × 256 RGBA, transparent background, black ink only, frames the same size and line weight across all seven, symbols readable when shrunk to 48 px (check a downscaled copy).
