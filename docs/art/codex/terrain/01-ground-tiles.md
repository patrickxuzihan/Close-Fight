# Task T1: ground tiles

Before starting, read `docs/art/codex/00-style-guide.md` and `docs/art/codex/terrain/00-terrain-guide.md`.
Output folder: `ArtSource/codex/terrain/`

Six seamless ground tiles, 512 × 512 px each (8 m × 8 m), for the map's terrain types. All in the same light, color range and level of detail, so they sit well next to each other. Late summer 1942, a city under months of bombing: dry, dusty, littered.

| File | Ground | Details |
|---|---|---|
| `ground_dirt.png` | Bare packed earth (the default ground between buildings) | Dry light brown-grey soil, footprints and wheel ruts barely visible, scattered pebbles, brick crumbs and splinters of wood. Dusty. |
| `ground_grass.png` | Neglected yards and verges | Patchy dry, yellowed late-summer grass with bare soil showing through, some weeds. Not lush, not green lawn. |
| `ground_road.png` | Cobbled city street | Grey granite setts (cobbles) in rows running north–south across the tile, worn, some missing, dust and fine debris in the joints. |
| `ground_rubble.png` | Brick and plaster rubble spread over the ground | Broken red and yellow bricks, chunks of plaster and concrete, splintered timber, a little twisted metal. Lumpy but flat enough to read as ground (it is a texture, not a pile). |
| `ground_rail.png` | Railway ballast (the game draws the rails itself) | Dark grey crushed-stone ballast with cinders and oil stains, a few scattered wooden sleeper fragments. **No rails and no complete sleepers.** |
| `ground_water.png` | Still, murky water (a pond or flooded crater) | Dull grey-brown water seen from above, slight surface texture, a little floating debris. No reflections of sky or objects. |

Also save the six 2 × 2 tiled previews (`ground_*_tiled_preview.png`) and one side-by-side sheet `ground_tiles_sheet.png` with all six tiles next to each other, so their colors can be compared.

Do not make craters, puddles or objects that stand out inside the tiles; those are decals (task T3).
