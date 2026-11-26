#!/usr/bin/env python3
"""Turn chosen roster candidates into game pictures.

    python3 tools/roster/process.py ArtSource/roster/candidates/unit_de_rifle/zimage_5001.png ...
    python3 tools/roster/process.py ArtSource/codex/roster/unit_stug3f.png ...
    python3 tools/roster/process.py --preview

A candidate's folder names its unit key (local candidates); otherwise its file name does
(Codex pictures, unit_<key>.png). A picture with a transparent background keeps it; one on
white is cut out of the white. Then it is cropped, padded to the roster tile's shape (76 x 54, standing on the bottom
edge) and saved at 4x as Assets/CloseFight/Resources/Art/Roster/<key>.png. An original from
outside ArtSource/ is kept as ArtSource/roster/<key>.png; one inside it (Codex's) stays
where it is, and an older kept copy is removed. --preview (also run after processing)
draws every unit picture on roster tiles at 1x and 3x into ArtSource/roster/preview.png.
Needs Pillow and numpy.
"""
import argparse
import os
import shutil

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
GAME = os.path.join(ROOT, "Assets", "CloseFight", "Resources", "Art", "Roster")
PICKED = os.path.join(ROOT, "ArtSource", "roster")
TILE = (76, 54)
SCALE = 4
# How dark the model's floor shadow stays after the white is removed (0 = gone, 1 = as drawn).
SHADOW = 0.6


def has_alpha(image):
    """True if the picture already has a transparent background."""
    if image.mode not in ("RGBA", "LA") and "transparency" not in image.info:
        return False
    # A close-up may be cut off by the edges, so look at the whole picture, not just its border.
    a = np.asarray(image.convert("RGBA"))[:, :, 3]
    return (a < 16).mean() > 0.05


def cutout(image):
    """The picture with its white background (and white pockets between legs and arms) made transparent.

    The model often draws a soft grey floor shadow under the feet. Everything pale and neutral that
    is reached from the border (the white and that shadow) is turned into translucent black by
    "colour to alpha" against white, so the shadow stays as a faint dark shadow instead of a white
    smear on the tile."""
    if has_alpha(image):
        return image.convert("RGBA")
    rgb = image.convert("RGB")
    a = np.asarray(rgb).astype(np.int16)
    lo, hi = a.min(axis=2), a.max(axis=2)
    neutral = (hi - lo) < 22
    pale = neutral & (lo > 140)
    pure = (lo > 232) & (hi - lo < 18)
    mask = Image.fromarray(np.where(pale, 255, 0).astype(np.uint8)).copy()
    w, h = mask.size
    border = [(x, y) for x in range(0, w, 8) for y in (0, h - 1)] + [(x, y) for y in range(0, h, 8) for x in (0, w - 1)]
    for p in border:
        if mask.getpixel(p) == 255:
            ImageDraw.floodfill(mask, p, 128)
    m = np.asarray(mask)
    background = m == 128
    # Pale pixels not reached from the border: pockets between legs and arms, if really white.
    # Specks (highlights) are kept.
    pockets = Image.fromarray(np.where((m == 255) & pure, 255, 0).astype(np.uint8))
    pockets = pockets.filter(ImageFilter.MinFilter(5)).filter(ImageFilter.MaxFilter(5))
    pockets = np.asarray(pockets) > 0
    # The figure: pull its edge in by a pixel to drop the white fringe, then soften it.
    figure = Image.fromarray(np.where(background | pockets, 0, 255).astype(np.uint8))
    figure = np.asarray(figure.filter(ImageFilter.MinFilter(3)).filter(ImageFilter.GaussianBlur(0.7))).astype(np.float32)
    # The background and the floor shadow: colour to alpha against white, softened a little.
    shadow = np.clip((246.0 - lo) / 246.0, 0, 1) * 255 * SHADOW
    alpha = np.where(background, np.maximum(shadow, figure * (figure > 250)), figure)
    colour = np.where(background[:, :, None], 0, a).astype(np.uint8)
    out = Image.fromarray(colour, "RGB")
    out.putalpha(Image.fromarray(np.clip(alpha, 0, 255).astype(np.uint8)))
    return out


def fit(picture):
    """Cropped to its content and padded to the tile's shape, standing on the bottom edge.

    A close-up whose men are cut off by the bottom edge (the Close Combat framing: helmets at
    the top, knees at the bottom) is instead cropped to fill the tile, with no padding."""
    a = np.asarray(picture.getchannel("A")) > 160
    if a[-1].mean() > 0.1:
        return fill(picture, a)
    box = picture.getchannel("A").point(lambda v: 255 if v > 160 else 0).getbbox()
    picture = picture.crop(box)
    w, h = picture.size
    aspect = TILE[0] / TILE[1]
    margin = 0.03
    cw, ch = w * (1 + 2 * margin), h * (1 + margin)
    if cw / ch > aspect:
        ch = cw / aspect
    else:
        cw = ch * aspect
    canvas = Image.new("RGBA", (round(cw), round(ch)), (0, 0, 0, 0))
    canvas.alpha_composite(picture, (round((cw - w) / 2), round(ch - h - h * margin * 0.3)))
    return canvas.resize((TILE[0] * SCALE, TILE[1] * SCALE), Image.LANCZOS)


def fill(picture, a):
    """A close-up cropped to the tile's shape from just above the helmets to the bottom edge,
    centred on the men; transparent padding only where the picture is too narrow."""
    ys, xs = np.where(a)
    top = max(0.0, ys.min() - 0.02 * picture.height)
    h = picture.height - top
    w = h * TILE[0] / TILE[1]
    left = xs.mean() - w / 2
    left = min(max(left, 0.0), picture.width - w) if w <= picture.width else (picture.width - w) / 2
    canvas = Image.new("RGBA", (round(w), round(h)), (0, 0, 0, 0))
    canvas.alpha_composite(picture.convert("RGBA").crop((round(left), round(top), round(left + w), picture.height)), (0, 0))
    return canvas.resize((TILE[0] * SCALE, TILE[1] * SCALE), Image.LANCZOS)


def preview():
    keys = sorted(f[:-4] for f in os.listdir(GAME) if f.startswith(("unit_", "platoon_")) and f.endswith(".png"))
    if not keys:
        print("no unit pictures yet")
        return
    tiles = {"olive": (153, 158, 117), "slate": (58, 62, 66)}
    zoom = 3
    cell_w, cell_h = TILE[0] * (1 + zoom) + 30, TILE[1] * zoom + 26
    sheet = Image.new("RGB", (len(tiles) * cell_w + 10, len(keys) * cell_h + 10), (40, 40, 38))
    d = ImageDraw.Draw(sheet)
    for row, key in enumerate(keys):
        pic = Image.open(os.path.join(GAME, key + ".png")).convert("RGBA")
        small = pic.resize(TILE, Image.LANCZOS)
        for col, (name, colour) in enumerate(tiles.items()):
            x, y = 10 + col * cell_w, 10 + row * cell_h
            d.text((x, y), f"{key} on {name}", fill=(220, 220, 210))
            tile = Image.new("RGBA", TILE, colour + (255,))
            tile.alpha_composite(small)
            sheet.paste(tile.convert("RGB"), (x, y + 14))
            sheet.paste(tile.convert("RGB").resize((TILE[0] * zoom, TILE[1] * zoom), Image.NEAREST), (x + TILE[0] + 10, y + 14))
    os.makedirs(PICKED, exist_ok=True)
    path = os.path.join(PICKED, "preview.png")
    sheet.save(path)
    print(os.path.relpath(path, ROOT))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("candidates", nargs="*")
    ap.add_argument("--preview", action="store_true")
    args = ap.parse_args()
    os.makedirs(GAME, exist_ok=True)
    os.makedirs(PICKED, exist_ok=True)
    for path in args.candidates:
        key = os.path.basename(os.path.dirname(os.path.abspath(path)))
        if not key.startswith(("unit_", "platoon_")):
            key = os.path.splitext(os.path.basename(path))[0]
        if not key.startswith(("unit_", "platoon_")):
            raise SystemExit(f"{path}: its folder or file must be named after the unit key")
        fit(cutout(Image.open(path))).save(os.path.join(GAME, key + ".png"))
        # Keep a copy of the original only if it lives nowhere else in the project (a local
        # candidate, which git ignores); Codex pictures already stay in ArtSource/codex/.
        kept = os.path.join(PICKED, key + ".png")
        if os.path.abspath(path).startswith(os.path.join(ROOT, "ArtSource") + os.sep):
            if os.path.exists(kept):
                os.remove(kept)
        else:
            shutil.copyfile(path, kept)
        print(f"{key}  <-  {os.path.relpath(os.path.abspath(path), ROOT)}")
    if args.candidates or args.preview:
        preview()


if __name__ == "__main__":
    main()
