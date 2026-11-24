# Task T2: roof tiles

Before starting, read `docs/art/codex/00-style-guide.md` and `docs/art/codex/terrain/00-terrain-guide.md`.
Output folder: `ArtSource/codex/terrain/`

Four seamless roof textures, 512 × 512 px each (8 m × 8 m), seen from directly above. The game covers each building's footprint with one of them. Stalingrad houses and blocks in 1942: weathered, dusty, some damage, but these are the *intact* roofs.

| File | Roof | Details |
|---|---|---|
| `roof_iron.png` | Painted sheet-iron roof (very common on Russian houses) | Long sheets with standing seams running **north–south** across the tile, faded dark red or grey-green paint, rust streaks, dust. |
| `roof_tile.png` | Clay tile roof | Rows of orange-red clay tiles running east–west, faded and dirty, a few cracked or missing tiles. |
| `roof_flat.png` | Flat roof of a concrete or brick block | Tar-paper or bitumen surface, grey-black, patched, with dust and small debris. |
| `roof_wood.png` | Wooden roof of a shed or small house | Weathered grey planks or shingles running north–south, some gaps. |

Rules:
- Seams and ridges run straight along the axes given above (the game rotates the texture to match each building).
- **No chimneys, skylights, antennas or other objects** (they would repeat on every tile).
- No cast shadows, no perspective.

Save the 2 × 2 tiled previews (`roof_*_tiled_preview.png`) and a comparison sheet `roof_tiles_sheet.png`.
