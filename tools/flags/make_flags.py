"""Builds the objective flag atlas from the public-domain source images.

Sources (Wikimedia Commons, public domain), downloaded into ArtSource/flags/source/:
  - Flag_of_the_Soviet_Union.svg  (rendered 1280 px)  -> Soviet flag
  - Balkenkreuz.svg               (rendered 1280 px)  -> German flag: the Wehrmacht's
    identification cross on a field-grey cloth. No Nazi symbols are used.

Output: Assets/CloseFight/Resources/Art/Flags/flags_atlas.png, 512 x 512:
the Soviet flag in the upper half, the German flag in the lower half. Each is a
flat, still 448 x 224 cloth with a thin dark edge, centred in a 512 x 256 cell.
Plus a preview in ArtSource/flags.

Run: python3 -I tools/flags/make_flags.py   (from the project root)
"""
import os

from PIL import Image, ImageDraw

ROOT = os.getcwd()
SRC = os.path.join(ROOT, "ArtSource", "flags", "source")
OUT = os.path.join(ROOT, "Assets", "CloseFight", "Resources", "Art", "Flags")
CELL_W, CELL_H = 512, 256
CLOTH_W, CLOTH_H = 448, 224
FIELD_GREY = (96, 102, 88)
EDGE = (30, 28, 24, 255)


def soviet_cloth():
    src = Image.open(os.path.join(SRC, "Flag_of_the_Soviet_Union_1280.png")).convert("RGBA")
    return src.resize((CLOTH_W, CLOTH_H), Image.LANCZOS)


def german_cloth():
    cloth = Image.new("RGBA", (CLOTH_W, CLOTH_H), FIELD_GREY + (255,))
    cross = Image.open(os.path.join(SRC, "Balkenkreuz_1280.png")).convert("LA").convert("RGBA")
    size = int(CLOTH_H * 0.72)
    cross = cross.resize((size, size), Image.LANCZOS)
    cloth.alpha_composite(cross, ((CLOTH_W - size) // 2, (CLOTH_H - size) // 2))
    return cloth


def cell(cloth):
    """The cloth flat and still, with a thin dark edge so it reads on any ground."""
    img = Image.new("RGBA", (CELL_W, CELL_H), (0, 0, 0, 0))
    x, y = (CELL_W - CLOTH_W) // 2, (CELL_H - CLOTH_H) // 2
    img.alpha_composite(cloth, (x, y))
    ImageDraw.Draw(img).rectangle([x - 1, y - 1, x + CLOTH_W, y + CLOTH_H], outline=EDGE, width=4)
    return img


def main():
    os.makedirs(OUT, exist_ok=True)
    atlas = Image.new("RGBA", (CELL_W, CELL_H * 2), (0, 0, 0, 0))
    atlas.alpha_composite(cell(soviet_cloth()), (0, 0))
    atlas.alpha_composite(cell(german_cloth()), (0, CELL_H))
    path = os.path.join(OUT, "flags_atlas.png")
    atlas.save(path, optimize=True)
    preview = Image.new("RGBA", atlas.size, (150, 140, 120, 255))
    preview.alpha_composite(atlas)
    preview.convert("RGB").save(os.path.join(ROOT, "ArtSource", "flags", "flags_preview.png"))
    print("wrote", path, atlas.size)


if __name__ == "__main__":
    main()
