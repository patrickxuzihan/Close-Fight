# Task R3: roster pictures of the four guns and their crews

Read `docs/art/codex/roster/00-picture-guide.md` first.

**Output folder:** `ArtSource/codex/roster/`

Four pictures. Each shows the gun with its crew around it, at full strength.

- **Every man does something different**, the way the Close Combat unit pictures do; the task lists each man's job. No two men in the same pose.
- **Crews wear plain uniforms with no insignia of any kind:** no eagle, no badges, no medals, no stars, no armbands.
  - German, 1942: field-grey tunic and trousers, M35 steel helmet, leather belt and boots.
  - Soviet, 1942: khaki gymnastyorka tunic, SSh-40 steel helmet, belt, boots or puttees.
- The gun is the main subject: it is large and in front, and the men stand around and behind it.

## If German soldiers are refused

Codex's image service refused German soldiers in an earlier task (R1, four times, `moderation_blocked`, `abuse`). So:

1. Make the two **Soviet** pictures first.
2. For each **German** gun, try once with its crew.
3. If that is refused:
   - draw that gun **alone, without men**, as `unit_<key>.png`;
   - also make the matching Soviet picture without men, as `unit_m1937_45_nocrew.png` or `unit_bm37_nocrew.png`, so the two sides can match;
   - write in `notes.md` which were refused, with the request IDs.

## `unit_pak38.png`: 5 cm PaK 38 (German anti-tank gun) and 4 crew

Shape reference: `ArtSource/codex/guns/gun_pak38.png`.

**Must show (gun):**

- A low gun with a long barrel ending in a two-chamber muzzle brake.
- A shield of two spaced, angled steel plates, with the barrel through the middle.
- Two pressed-steel wheels with solid rubber tyres, beside the shield.
- The two trail legs opened in a V behind the gun.
- Dark grey paint.

**Crew, left to right:**

1. The gunner crouches behind the left of the shield, his eye at the sight.
2. The loader kneels beside the breech, holding a shell in both hands.
3. The gun commander kneels at the right edge of the shield, looking through binoculars.
4. The fourth man carries an ammunition case.

**Must not:** tracks, a hull or a turret (the local model turned this gun into a tracked tank destroyer); a single flat shield.

## `unit_m1937_45.png`: 45 mm M1937 (Soviet anti-tank gun) and 4 crew

Shape reference: `ArtSource/codex/guns/gun_m1937_45.png`.

**Must show (gun):**

- A small, light gun with a thin, long barrel and no muzzle brake.
- A flat steel shield with a straight top edge.
- Two spoked wheels with rubber tyres.
- The two trail legs opened in a V behind the gun.
- Protective green paint.

**Crew, left to right:**

1. The gunner crouches at the sight.
2. The loader kneels with a shell ready.
3. The gun commander, half bent, points forward.
4. The fourth man carries an ammunition box.

**Must not:** tracks, a hull, a muzzle brake.

## `unit_grw34.png`: 8 cm Granatwerfer 34 (German mortar) and 3 crew

Shape reference: `ArtSource/codex/guns/gun_grw34.png`.

**Must show (mortar):**

- A smooth tube about 1.1 m long, leaning forward steeply, at about 60°.
- A bipod with the sight on its left leg.
- A roughly rectangular base plate.
- Dark grey paint.

**Crew:**

1. One man kneels and holds a bomb over the muzzle, about to drop it in.
2. The gunner kneels at the bipod, adjusting the sight.
3. The third man crouches beside an open case of mortar bombs, handing one up.

**Must not:** wheels, a long barrel like a gun, a round base plate.

## `unit_bm37.png`: 82 mm BM-37 (Soviet mortar) and 3 crew

Shape reference: `ArtSource/codex/guns/gun_bm37.png`.

**Must show (mortar):**

- The same layout as the German mortar: the tube leaning forward steeply on a bipod.
- A **round** base plate.
- Protective green paint.

**Crew:** the same three jobs as the German mortar team, in different poses from that picture.

**Must not:** wheels, a long barrel.

## Order

Soviet pictures first, then the German ones (see above), then the preview `r3_preview.png`.
