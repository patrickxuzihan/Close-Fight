# Terrain texture guide (read with the style guide)

Read `docs/art/codex/00-style-guide.md` first. Its rules on **look (realistic, slightly desaturated, painterly-photographic, soft overcast light), forbidden symbols, keeping raw files, `notes.md` and the self-check report all apply here.**

This guide **replaces sections 1, 2 and 5 of the style guide** for terrain, because these are not characters: they are ground textures, roof textures and ground decals that the game lays over its map. The game is a straight top-down aerial view of Stalingrad in September 1942, in the spirit of Close Combat's aerial-photo maps: dusty, bombed, late summer.

## 1. Camera

- **Strict orthographic top-down**, like a vertical aerial photograph taken from directly above. No perspective, no horizon, no visible walls or vertical faces.
- **No cast shadows** from any object (the game draws its own shadows). Small contact darkening inside cracks, between stones or in ruts is fine.

## 2. Two kinds of image

### Tiles (ground and roofs)

- **512 × 512 px, covering 8 m × 8 m (64 px = 1 m).**
- **Seamless:** the right edge continues into the left edge and the top into the bottom. The game repeats each tile across large areas.
- **No obvious features that repeat visibly** when tiled: no single big stain, crater or object that would show up every 8 m. Keep variation fine and even.
- Fully opaque (no transparency).
- **Check seamlessness** by building a 2 × 2 tiled preview (1024 × 1024) and looking for seams and repeating blotches. Save it as `<file name>_tiled_preview.png`.

### Decals (craters, rubble piles, scorch marks)

- **512 × 512 px, 64 px = 1 m** (the canvas covers 8 m × 8 m), unless the task says otherwise.
- **Transparent background**; the edges fade or break up naturally into transparency, so the decal blends onto any ground. No hard square edge, no halo.
- The decal is centered on the canvas.

## 3. Post-processing (with code, e.g. Python + Pillow)

1. Tiles: make the texture seamless (for example offset by half a tile and blend or regenerate the seam area), then resize to exactly 512 × 512.
2. Decals: clean the alpha (no leftover background, no gray fringe), center, 512 × 512.
3. Save as 8-bit PNG (RGB for tiles, RGBA for decals).

## 4. Self-check for every final PNG

- [ ] Correct size (512 × 512) and mode (RGB tile / RGBA decal).
- [ ] Straight top-down, no perspective, no cast shadows.
- [ ] Tiles: no visible seam in the 2 × 2 preview, no obvious repeating blotch.
- [ ] Decals: clean transparent edges.
- [ ] No people, vehicles, text, symbols or flags.

Report this checklist in your final reply.
