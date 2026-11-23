# Task V1: StuG III Ausf. F assault gun (German, 1942)

Before starting, read `docs/art/codex/00-style-guide.md` and `docs/art/codex/vehicles/00-vehicle-guide.md`.
Output folder: `ArtSource/codex/vehicles/`

## What it is

A low, turretless assault gun on the Panzer III chassis. The long 7.5 cm StuK 40 L/43 gun sits in a box-like casemate (superstructure) and does not turn independently, so the gun is **part of the hull image**.

## Images

- `veh_stug3f_hull.png`: 512 × 1024 canvas, with the hull and gun pointing north.
  - Hull length without the gun: **5.40 m = 691 px**. Width over the tracks: **2.95 m = 378 px**.
  - The gun barrel sticks out about 1.0 m (128 px) beyond the front plate, slightly right of the center line. It has a large boxy mantlet and **no muzzle brake** on the L/43.
  - The casemate covers the front two thirds of the hull: a flat roof with the commander's hatch and periscope on the rear left, and the loader's hatch on the rear right. The engine deck (rear third) has two large hatches and side air intakes.
  - The tracks run the full length on both sides, with 6 small road wheels a side partly visible beside the hull. There are no side skirts in 1942.
  - The pivot is the hull center at canvas (256, 512).
- There is no turret image for this vehicle.

## Revision 2: shape corrections (the first version looked like a different vehicle)

- **The casemate is wide and low.** It fills almost the whole width between the tracks: it sits on the fenders, with flat sloped side plates. It is **not** a narrow box in the middle of the hull.
- **The gun sits in a big, boxy, slab-sided mantlet** set into the casemate front, **offset to the right of the center line** by about 0.2 m (26 px). The mantlet is wide (about 0.9 m = 115 px), not a small round collar.
- **On the casemate roof:**
  - the commander's split hatch with a periscope at the rear left;
  - the loader's hatch at the rear right;
  - the gunner's sight opening at the front left.
- **The front hull ahead of the casemate** is a short, flat plate with two track-brake access hatches.
- **The engine deck** is the rear third, with two big hatches, air intakes on both sides, and a towing cable and tools on the fenders.
- The tracks are visible along both sides, slightly wider than the fenders.

## Checks

The hull is 691 px long (±2 %) and centered on the canvas, the gun is included, no crew is visible, and there are no swastikas.
