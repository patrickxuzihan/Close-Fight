# Task T3: ground decals (craters, rubble piles, scorch)

Before starting, read `docs/art/codex/00-style-guide.md` and `docs/art/codex/terrain/00-terrain-guide.md` (decal rules in section 2).
Output folder: `ArtSource/codex/terrain/`

Transparent decals the game stamps onto the ground. 512 × 512 px each, 64 px = 1 m, centered, with edges that break up naturally into transparency.

| File | What | Size on the canvas |
|---|---|---|
| `decal_crater_small_1.png`, `decal_crater_small_2.png` | Mortar-round craters: a shallow pit with thrown-out earth and debris around it | About 2.5 m across (160 px) including the spray of earth |
| `decal_crater_large.png` | Artillery or bomb crater: deep dark pit, ring of thrown earth and broken bricks | About 7 m across (450 px) |
| `decal_rubble_1.png`, `decal_rubble_2.png`, `decal_rubble_3.png` | Piles of a collapsed building: heaps of bricks, plaster, beams and roof sheets, irregular outline | Filling most of the canvas (6–7.5 m), each a different shape |
| `decal_scorch.png` | Burn mark: blackened ground and soot where something burned | About 4 m across (256 px), soft edges |

Rules:
- Straight top-down, no cast shadows (contact darkening inside the pit and between bricks is fine).
- Different shapes for the numbered variants, so repeats are not obvious.
- No wreckage of vehicles, no bodies, no weapons.

Save a preview `decals_sheet.png` with all decals laid over the `ground_dirt.png` tile from task T1 (if it exists; otherwise over a plain dusty brown background).
