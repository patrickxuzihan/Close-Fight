"""Roster thumbnails of vehicles and guns (M18).

The roster shows each vehicle or gun team with its equipment facing right. The
Codex top-down art faces up on a padded canvas, so this script composites the
turret onto the hull at its ring, crops to the visible pixels, turns the image to
face right and writes Resources/Art/Roster/equip_<id>.png. Run it again after a
new Codex delivery of vehicle or gun art:

    python3 tools/roster/make_equipment_thumbs.py
"""
import glob
import os
import sys

from PIL import Image

ROOT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "CloseFight", "Resources", "Art")
OUT = os.path.join(ROOT, "Roster")
MAX_LENGTH = 256

# Turret ring centres on the 512 x 1024 hull canvases, pixels from the top-left
# (the same table as Game/View/EquipmentArt.cs).
RINGS = {"pz3j": (256, 499), "t34_76": (256, 397), "t60": (211, 474)}


def finish(image, name):
    box = image.getchannel("A").getbbox()
    image = image.crop(box).rotate(-90, expand=True)  # front (up) now points right
    scale = min(1.0, MAX_LENGTH / max(image.size))
    if scale < 1.0:
        image = image.resize((round(image.width * scale), round(image.height * scale)), Image.LANCZOS)
    path = os.path.join(OUT, "equip_" + name + ".png")
    image.save(path)
    print(path, image.size)


def vehicles():
    for hull_path in sorted(glob.glob(os.path.join(ROOT, "Vehicles", "veh_*_hull.png"))):
        vid = os.path.basename(hull_path)[len("veh_"):-len("_hull.png")]
        hull = Image.open(hull_path).convert("RGBA")
        turret_path = os.path.join(ROOT, "Vehicles", "veh_" + vid + "_turret.png")
        if os.path.exists(turret_path) and vid in RINGS:
            turret = Image.open(turret_path).convert("RGBA")
            # A canvas big enough for the turret canvas centred on the ring.
            pad = turret.width
            canvas = Image.new("RGBA", (hull.width + 2 * pad, hull.height + 2 * pad))
            canvas.alpha_composite(hull, (pad, pad))
            rx, ry = RINGS[vid]
            canvas.alpha_composite(turret, (pad + rx - turret.width // 2, pad + ry - turret.height // 2))
            hull = canvas
        finish(hull, vid)


def guns():
    for gun_path in sorted(glob.glob(os.path.join(ROOT, "Guns", "gun_*.png"))):
        gid = os.path.basename(gun_path)[len("gun_"):-len(".png")]
        finish(Image.open(gun_path).convert("RGBA"), gid)


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    vehicles()
    guns()
    sys.exit(0)
