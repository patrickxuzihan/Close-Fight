# Roster pictures: shared rules (tasks R2 and R3)

Before starting, read `docs/art/codex/00-style-guide.md`. Only these rules from it apply:

- forbidden symbols;
- only creating files inside `ArtSource/`;
- keeping raw files;
- `notes.md`;
- reporting.

Its camera, scale, canvas and pivot rules do **not** apply: these pictures are not top-down sprites.

## What they are

The roster is the unit list along the edge of the screen. Each unit has a small picture there, **76 × 54 px in game**, in the style of the unit pictures in the later Close Combat games: a colour, realistic rendered picture of the unit, seen from the front three-quarter.

- The infantry pictures are made elsewhere with a local image model.
- The local model cannot draw the vehicles and guns correctly. It turned the PaK 38 into a tracked tank destroyer and gave the turretless StuG a turret. So Codex makes these eight.

**Style reference:** `ArtSource/roster/unit_su_rifle.png`, the Soviet rifle squad picture now in the game. Match its rendering, light and level of detail.

**Shape references:** the game's top-down sprites in `ArtSource/codex/vehicles/` and `ArtSource/codex/guns/` show the shapes and paint the game already uses. The pictures must look like the same vehicles and guns. Each task names the sprite to use.

## Look

- **Rendering:** a realistic 3D render with fine detail, like a high-quality scale model photographed in a studio. Not a painting, not a cartoon, not a clean CAD or plastic look.
- **Camera:** the same for all eight pictures.
  - Front three-quarter view: the front of the vehicle or gun faces the **lower left** of the picture.
  - Seen from slightly above, about 15–20° down, so the front, the left side and a little of the top all show.
- **Light:** soft and even, from the upper front. No hard shadows.
- **No ground, no cast shadow, no floor shadow, no scenery.**
- **Background:** fully transparent. If the tool cannot give a clean transparent background, use plain pure white (RGB 255, 255, 255) instead, with no grey floor.
- **Paint:**
  - German: dark grey (Panzergrau).
  - Soviet: protective green (4BO).
- **Wear:** this is Stalingrad in 1942, not a museum.
  - Dust on the lower hull, wheels and tracks.
  - Scuffed paint on the edges, a little mud.
- **Markings: none at all.** No crosses (no Balkenkreuz either), no stars, no numbers, no slogans, no flags, no text, no swastikas.

## Canvas

- Landscape PNG, 1536 × 1024 (or the nearest landscape size the tool gives), RGBA.
- The subject fills the frame: about 90 % of the width, standing near the bottom edge with a small margin.
- The game shrinks the picture to 76 × 54, so small details vanish. **The outline must say which vehicle or gun it is.** If the shape is wrong, regenerate: the shape matters more than anything else.

## Files

All go in `ArtSource/codex/roster/`.

- `unit_<key>.png`: the final picture (the task gives each key).
- `unit_<key>_raw.png`: the raw generation, if you cleaned it afterwards.
- `<task>_preview.png` (`r2_preview.png` or `r3_preview.png`):
  - every final picture of the task on an olive tile, RGB (153, 158, 117);
  - at 76 × 54 and again at 3× (228 × 162), with its key written beside it.
- An entry in `notes.md` for the task.

## Self-check

For every picture, confirm:

- [ ] Every point in the task's "Must show" list is there, and nothing from its "Must not" list.
- [ ] There are no markings or symbols.
- [ ] The angle and light match the other pictures in the task.
- [ ] The background is transparent or pure white, with no ground or shadow.
- [ ] On the olive tile at 76 × 54, you can tell which vehicle or gun it is.

Report the result of this checklist in your final reply.
