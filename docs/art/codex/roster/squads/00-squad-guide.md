# R5: infantry team pictures, shared rules (read before each R5 task)

Each R5 task makes **one** picture and runs in its own conversation. Several may run at the same time. This file holds what they share. The task file says which picture to make and what the men do.

Before starting, also read `docs/art/codex/00-style-guide.md`. Only these rules from it apply:

- forbidden symbols;
- only creating files inside `ArtSource/`;
- keeping raw files;
- reporting.

Its camera, scale, canvas and pivot rules do **not** apply: these are not top-down sprites.

**Output folder:** `ArtSource/codex/roster/squads/`

## The reference

In task R4 you made `ArtSource/codex/roster/squad_test/de_rifle_4_raw.png`, the German rifle squad. It is now in the game, and it is exactly what we want. **Use it as the reference image for every R5 picture:**

- the same rendering, light and level of detail;
- the same close framing (below);
- the same camera height and angle.

Only the men, their jobs and their weapons change.

## What the pictures are

The game's unit list shows a small picture for each team, **76 × 54 px in game**, like the unit pictures in the later Close Combat games. One picture is shared by every team of that type on that side.

## Framing (the most important part)

- The men stand or kneel **shoulder to shoulder**, overlapping a little, and fill the frame from edge to edge.
- Their **helmets nearly touch the top edge**: a margin of about 3 % at most.
- The **bottom edge cuts across the men's legs**: standing men at about the knees, kneeling men at the thighs. There are no feet in the picture.
- The men must reach the bottom edge across **at least a third of its width**, so that the game recognises the close-up and crops it to fill the tile.
- Very little background shows between the men. The men cover **at least 70 %** of the frame.
- **Nobody lies down.** A lying man would fall below the crop.
- The camera is at chest height, slightly from the front left. The men face the viewer and a little to the right.

The game crops each picture to the tile's shape like this, so keep everything that matters inside that area:

- top: 2 % of the picture height above the highest helmet;
- bottom: the bottom edge;
- width: the height × 76 / 54, centred on the men.

## Look

- **Rendering:** a realistic 3D render with fine detail, like the reference.
- **Light:** soft and even, from the upper front. No hard shadows.
- **Background:** fully transparent, like the reference.
- **Every man is different:**
  - different faces, ages and builds;
  - different poses: no two men in the same pose;
  - each does the job the task lists for him.
- **Uniforms are plain and a little worn and dusty.** No insignia of any kind: no eagle, no badges, no medals, no ribbons, no stars, no armbands, no flags, no text.

### German, 1942

- Field-grey wool tunics and trousers, grey-green M35 steel helmets with flared rims, leather belts, Y-straps and ammunition pouches.
- Only these weapons:
  - wooden-stocked Kar98k bolt-action rifles (a sniper's has a telescopic sight);
  - MP40 submachine guns with a straight magazine;
  - the MG34 machine gun: a long gun with a perforated barrel jacket.

### Soviet, 1942

- Khaki-brown tunics and trousers, rounded green SSh-40 steel helmets, a rolled greatcoat worn diagonally over the shoulder.
- Only these weapons:
  - wooden-stocked Mosin-Nagant bolt-action rifles with long spike bayonets (a sniper's has a telescopic sight and no bayonet);
  - PPSh-41 submachine guns with a round drum magazine;
  - the DP-27 machine gun with a large, flat, round pan magazine on top;
  - the Maxim M1910 heavy machine gun: a water-cooled gun with a thick, ribbed barrel jacket.
- **Nobody has a curved magazine.**

## Matching the other side

Most pictures come in pairs, German and Soviet. The task names the other side's file. If that file already exists in the output folder, look at it too: the two should match in composition, each with its own side's uniforms and weapons. If it does not exist yet, go on without it.

## If the picture is refused

The image service may refuse a picture. R4 had one refusal, the six-man version.

- After a refusal, try **once more**, with simpler wording.
- If it is refused again, stop. Write both refusals and their request IDs in your notes file and report them.

## Files

Write **only your own picture's files**. Other R5 conversations may be writing their files in the same folder at the same time.

- `unit_<key>.png`: the final picture, 1536 × 1024 (or the size the tool gives), RGBA, transparent background, framed as above. No further cropping: the game crops it.
- `unit_<key>_raw.png`: the raw generation, if you cleaned it afterwards.
- `unit_<key>_preview.png`, on an olive tile, RGB (153, 158, 117):
  - the reference `de_rifle_4_raw.png` and your picture, each cropped the way the game crops it;
  - each at 76 × 54 and again at 3× (228 × 162).
- `unit_<key>_notes.md`: the task, the date, the files you made, the prompts you used, any refusals with their request IDs, and anything you could not achieve.
- **Do not edit** `ArtSource/codex/roster/notes.md`, because several conversations may run at once.

## Self-check

Confirm:

- [ ] The right number of men, each doing his listed job, in a different pose from the others.
- [ ] There are no insignia or symbols of any kind.
- [ ] Each weapon is one of the listed ones, complete, and correctly held. No curved magazines.
- [ ] Helmets nearly touch the top edge, the legs are cut by the bottom edge, and nobody lies down.
- [ ] The men cover at least 70 % of the frame.
- [ ] The rendering, light and framing match the reference.
- [ ] On the olive tile at 76 × 54, you can see separate men and tell what the team is.

Report the result of this checklist in your final reply.
