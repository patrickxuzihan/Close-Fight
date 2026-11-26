# Task R4 (test): rifle squad pictures, close framing

Before starting, read `docs/art/codex/00-style-guide.md`. Only these rules from it apply:

- forbidden symbols;
- only creating files inside `ArtSource/`;
- keeping raw files;
- `notes.md`;
- reporting.

Its camera, scale, canvas and pivot rules do **not** apply: these are not top-down sprites.

**Output folder:** `ArtSource/codex/roster/squad_test/`

## Why this is a test

This is a small trial to see two things:

1. whether the image service will draw these men at all;
2. whether the close framing below looks right in the game's tile.

An earlier task (R1, see `ArtSource/codex/roster/notes.md`) was refused four times with `moderation_blocked` (`abuse`) for a single German soldier. So:

- **Do the German pictures first, one attempt each.** The men are ordinary soldiers in plain uniforms with no insignia of any kind: no eagle, no badges, no flags, no symbols.
- **If a German picture is refused, stop the whole task.** Do not retry and do not make the Soviet pictures, because the two sides must come from the same source to match. Write the refusal and its request ID in `notes.md` and report it.

## What the pictures are

The game has a unit list along the edge of the screen. Each unit has a small picture there, **76 × 54 px in game**, like the unit pictures in the later Close Combat games. One picture is shared by every unit of that type on that side.

The look that matters most is the **framing**:

- A few men stand **shoulder to shoulder**, overlapping a little, filling the frame from edge to edge.
- Their **helmets nearly touch the top edge**: a margin of about 3 % at most.
- The **bottom edge cuts the men at the knees**: there are no feet in the picture.
- Very little background shows between the men. The men cover **at least 70 %** of the frame.
- The men are **big and chunky**, seen close up: shoulders, chests, faces, helmets and weapons read clearly even at 76 × 54.

The camera is at chest height, slightly from the front left (front three-quarter view). The men face the viewer and a little to the right, as if moving forward together.

Our current pictures are on the right track for detail but wrong in framing: they show the whole squad from helmet to boots, so each man is tiny. For the uniforms and weapons, `ArtSource/roster/unit_de_rifle.png` and `ArtSource/roster/unit_su_rifle.png` are good references. **Do not copy their framing.**

## Look

- **Rendering:** a realistic 3D render with fine detail, like a high-quality figure photographed in a studio. Not a painting, not a cartoon.
- **Light:** soft and even, from the upper front. No hard shadows.
- **Background:** fully transparent. If the tool cannot give a clean transparent background, use plain pure white (RGB 255, 255, 255).
- **Every man is different:**
  - different faces, ages and builds;
  - different poses: no two men in the same pose;
  - each does his own job (see below).
- **Uniforms are plain and a little worn and dusty.** No insignia of any kind: no eagle, no badges, no medals, no ribbons, no stars, no armbands, no flags, no text.

### German, 1942

- Field-grey wool tunics and trousers, grey-green M35 steel helmets with flared rims, leather belts, Y-straps and ammunition pouches.
- Only these weapons:
  - wooden-stocked Kar98k bolt-action rifles;
  - MP40 submachine guns with a straight magazine;
  - an MG34 machine gun: a long gun with a perforated barrel jacket and a bipod.

### Soviet, 1942

- Khaki-brown tunics and trousers, rounded green SSh-40 steel helmets, a rolled greatcoat worn diagonally over the shoulder.
- Only these weapons:
  - wooden-stocked Mosin-Nagant bolt-action rifles with long spike bayonets;
  - PPSh-41 submachine guns with a round drum magazine;
  - a DP-27 machine gun with a large, flat, round pan magazine on top.
- **Nobody has a curved magazine.**

## The four pictures

Each side gets two versions, so we can compare how many men read best at 76 × 54.

### `de_rifle_4.png`: German rifle squad, 4 men in one row

From left to right:

1. The squad leader, looking through binoculars held to his eyes, an MP40 slung on his chest.
2. The machine gunner, carrying the MG34 over his right shoulder.
3. His assistant, a metal ammunition box in each hand.
4. A rifleman holding his Kar98k at the ready, across his body.

### `de_rifle_6.png`: German rifle squad, 6 men

The same four men in front, plus two riflemen just behind them, partly hidden, only their heads, shoulders and rifles showing.

### `su_rifle_4.png`: Soviet rifle section, 4 men in one row

From left to right:

1. The section leader, pointing forward with his outstretched left arm, a PPSh-41 in his right hand.
2. The machine gunner, carrying the DP-27 over his right shoulder.
3. A soldier with a PPSh-41 held at the hip, leaning forward.
4. A rifleman holding his Mosin-Nagant with its bayonet fixed, at the ready.

### `su_rifle_6.png`: Soviet rifle section, 6 men

The same four men in front, plus two riflemen just behind them, partly hidden, only their heads, shoulders and bayonets showing.

## Canvas

- Generate landscape images, for example 1536 × 1024.
- Crop each final picture to the tile's shape: **608 × 432 px** (8 × the 76 × 54 tile), RGBA.
- The crop follows the framing rules above: helmets at the top, knees at the bottom edge, men filling the width.
- Keep each uncropped generation as `<name>_raw.png`.

## Preview

Make `squad_test_preview.png`:

- every final picture on an olive tile, RGB (153, 158, 117);
- each at 76 × 54 and again at 3× (228 × 162);
- with its file name written beside it.

## Self-check

For every picture, confirm:

- [ ] There are no insignia or symbols of any kind.
- [ ] Each weapon is one of the listed ones, complete, and correctly held. No curved magazines.
- [ ] Helmets nearly touch the top edge, and the knees are at the bottom edge.
- [ ] The men cover at least 70 % of the frame.
- [ ] On the olive tile at 76 × 54, you can see separate men and tell the machine gunner from the others.

Report the result of this checklist in your final reply. If a picture was refused, say which one and stop.
