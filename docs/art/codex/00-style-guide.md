# Close Fight sprite style guide (read before every task)

You are producing 2D sprites for **Close Fight**, a top-down WWII tactics game set in the Battle of Stalingrad, September 1942. The game has a realistic aerial look.

**You only make images.** Do not create, edit or delete anything outside the `ArtSource/` folder. Never touch game code, `Assets/` or `docs/`.

## 1. Camera

- **Strict orthographic top-down view.** The camera points straight down at 90 degrees.
- No perspective, no 3/4 view, no visible faces. We see the top of the helmet, the shoulders, the back and the limbs, as from a drone directly overhead.
- **The figure always faces the TOP of the image (north).** The head is toward the top edge. The game rotates the sprite to any heading.

## 2. Scale and placement (all frames must match)

- **128 pixels = 1 meter.**
- Every frame is a **256 × 256 px** PNG (2 m × 2 m).
- **Scale anchor: the steel helmet is 34 px wide in every frame**, whatever the pose. After generating, measure the helmet and rescale the figure so it matches.
- Reference sizes:
  - A prone soldier measures about 230 px from helmet to boot tips.
  - A standing soldier's shoulders are about 58 px wide.
- **Do not enlarge small poses to fill the canvas.** A standing soldier looks small in a 256 px frame, and that is correct.
- **Pivot:**
  - Standing, walking, running, kneeling and throwing poses: the center of the torso sits at the canvas center (128, 128).
  - Lying poses: the hips sit at the canvas center.
  - In a two-frame animation, the pivot stays at exactly the same pixel in both frames, so the sprite does not jitter when the game alternates them.

## 3. Look

- **Style:** realistic, slightly desaturated, painterly-photographic, like a high-detail military miniature photographed from directly above.
- **Lighting:** soft, even overcast daylight.
- **NO cast shadows, NO ground, NO background.** The game adds shadows itself.
- **Fully transparent background** (PNG with alpha). Clean edges, no white or colored halo, no semi-transparent haze around the figure.
- No text, no labels, no borders, no watermark.
- **Absolutely NO swastikas, NO flags, NO political symbols** anywhere.

## 4. Consistency

- Every task for a character starts from that character's **reference image** (named in its `00-character.md`). Use it as the visual reference for each generation, so the uniform, colors, equipment and proportions stay identical.
- **For a two-frame animation, generate both frames together** in a single 512 × 256 image (frame A on the left, frame B on the right), then slice it. Both frames come from the same generation and match.
- If a frame comes out at a different angle, scale or style, regenerate it. Do not deliver it.

## 5. Post-processing (do this with code, e.g. Python + Pillow)

1. Remove any leftover background, so the alpha is clean.
2. Trim, then scale the figure so the helmet is 34 px wide.
3. Place it on a 256 × 256 transparent canvas, with the pivot at (128, 128) as defined in section 2.
4. Save as 8-bit RGBA PNG.

## 6. Output

- Save into the folder named in the task, using the exact file names it gives.
- Keep each raw generation as `<file name>_raw.png` next to the final file, so it can be reprocessed later.
- Append a short entry to `notes.md` in the same folder: the task name, the date, which files you made, and anything you could not achieve.

## 7. Self-check before finishing

For every final PNG, confirm:

- [ ] It is 256 × 256, RGBA, with a transparent background.
- [ ] The view is straight top-down, and the head points up.
- [ ] The helmet is 34 px wide (±2 px).
- [ ] The pivot is at the canvas center as defined in section 2.
- [ ] The uniform matches the reference image.
- [ ] There are no shadows, no ground and no symbols.

Report the result of this checklist in your final reply.
