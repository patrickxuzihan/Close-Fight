# Vehicle and gun sprite guide (read with the style guide)

Read `docs/art/codex/00-style-guide.md` first. Its rules on **camera, look, transparency, forbidden symbols, post-processing, output and self-check all apply here.**

This guide **replaces only section 2 (scale and placement)** of the style guide, because vehicles and guns are not people: there is no helmet to measure.

## 1. Scale

- **128 pixels = 1 meter**, the same as the soldiers, so everything matches in game.
- **The scale anchor is the hull (or carriage) length given in each task**, in pixels. After generating, measure the real outline and rescale until it matches the given length ±2 %.

## 2. Canvases and pivots

- **Hulls:** a 512 × 1024 px canvas (4 m × 8 m). The vehicle points north (front toward the top edge). The **center of the hull** sits at the canvas center (256, 512).
- **Turrets:** a 1024 × 1024 px canvas. The barrel points north. The **center of the turret ring** (the point the turret turns around) sits exactly at the canvas center (512, 512).
- **Towed guns:** a 1024 × 1024 px canvas. The barrel points north and the trails point south. The **axle center** sits at the canvas center.
- **Mortars:** a 256 × 256 px canvas. The tube leans north. The **baseplate center** sits at the canvas center.
- If a turret has to sit somewhere on the hull, the hull is drawn **without** the turret, with the turret ring visible as a circular opening. The task gives where the ring center must be; measure it afterwards and report it in `notes.md`.

## 3. Look

- **German vehicles and guns, 1942:** dark grey (Panzergrau), worn, with dust on the lower hull and tracks.
- **Soviet vehicles and guns:** protective green (4BO), worn, with dust and some bare metal on edges.
- Tracks, road wheels, the engine deck (grilles, hatches), tools and spare track links should read clearly from above.
- **Markings:** none visible from above, except an optional small black-and-white Balkenkreuz (straight-armed cross) on the rear plate of German vehicles. **No swastikas, no red stars, no slogans, no numbers.**
- No crew visible (hatches closed). No cast shadows, no ground, transparent background.
- **Volume without a light direction (revision 2, after the first batch looked flat):**
  - Use ambient-occlusion style shading: recesses, gaps between plates, the area under fenders and around hatches darker; raised edges, hatch rims and weld seams catching light.
  - Sloped plates (glacis, sloped sides) a little lighter than flat roofs, so slopes read.
  - **Do not use a single key light from one side.** Sprites rotate in game, so a baked light direction would be wrong half the time; the game adds the cast shadow itself.
- **Weathering (revision 2):** this is Stalingrad, not a museum. Add heavy dust on everything below the fenders, mud on tracks and lower hull, scuffed paint on edges, a little rust. Avoid the clean, flat, plastic-model or CAD look.

## 4. Output

Save into the folder named in the task, with the exact file names. Keep each raw generation as `<file name>_raw.png`, and append an entry to `notes.md` with the measured lengths and pivot positions.
