#!/usr/bin/env python3
"""Draws exported district battle maps (M16 step 4) as a preview sheet.

    Unity -batchmode -quit -projectPath . -executeMethod CloseFight.EditorTools.CampaignMapExport.RunFromCommandLine -exportDir <dir>
    python3 tools/campaign/preview_maps.py <dir> <out.png> [district ...]

Each map is drawn from above, north up: roads, rail, grass, rubble, craters and water;
buildings shaded by floors (wood brown, brick red-brown, concrete grey); walls and fences;
wagons and props. The attacker's deployment strip (west) is red, the defender's ground blue,
objectives are yellow rings. Needs Pillow.
"""
import json
import os
import sys

from PIL import Image, ImageDraw, ImageFont

TERRAIN = {"Open": (196, 186, 160), "Road": (150, 142, 128), "Rubble": (160, 140, 118), "Rail": (120, 110, 100),
           "Grass": (150, 165, 110), "Water": (110, 150, 180), "Crater": (120, 105, 85), "BuildingFloor": (180, 170, 150)}
MATERIAL = {"Wood": (150, 110, 70), "Brick": (150, 85, 70), "Concrete": (140, 140, 135)}
NAMES = {"Residential": "住宅区", "Industrial": "工厂", "Railway": "铁路", "Riverside": "河岸", "Hill": "高地", "Square": "广场", "Outskirts": "郊区"}


def draw(map_json, field_json, scale):
    m = json.load(open(map_json))
    f = json.load(open(field_json))
    w, h = int(m["width"] * scale), int(m["height"] * scale)
    img = Image.new("RGB", (w, h), TERRAIN.get(m["baseTerrain"], (196, 186, 160)))
    d = ImageDraw.Draw(img, "RGBA")

    def P(points):
        return [(p["x"] * scale, h - p["y"] * scale) for p in points]

    for area in m["areas"]:
        d.polygon(P(area["polygon"]), fill=TERRAIN.get(area["terrain"], (200, 0, 200)))
    for b in m["buildings"]:
        base = MATERIAL[b["material"]]
        k = max(0.55, 1.05 - 0.1 * b["floors"])
        d.polygon(P(b["polygon"]), fill=tuple(int(c * k) for c in base), outline=(40, 35, 30))
    for wall in m["walls"]:
        colour = (60, 55, 50) if wall.get("blocksMovement", True) else (110, 95, 70)
        d.line(P(wall["points"]), fill=colour, width=max(1, int(scale * 1.2)))
    for prop in m["props"]:
        d.polygon(P(prop["polygon"]), fill=(80, 70, 60) if prop["kind"] == "wagon" else (170, 170, 175))
    d.polygon(P(f["attackerZone"]), fill=(200, 40, 30, 45), outline=(200, 40, 30))
    d.polygon(P(f["defenderZone"]), fill=(40, 80, 200, 25), outline=(40, 80, 200))
    for o in f["objectives"]:
        x, y = o["position"]["x"] * scale, h - o["position"]["y"] * scale
        r = o["radius"] * scale
        d.ellipse([x - r, y - r, x + r, y + r], outline=(250, 210, 40), width=3)
    return img, f["kind"]


def main():
    src, out = sys.argv[1], sys.argv[2]
    ids = sys.argv[3:] or sorted({n.split(".")[0] for n in os.listdir(src) if n.endswith(".map.json")})
    scale = 0.9
    font = ImageFont.truetype("/System/Library/Fonts/Hiragino Sans GB.ttc", 24)
    tiles = []
    for i in ids:
        img, kind = draw(os.path.join(src, i + ".map.json"), os.path.join(src, i + ".field.json"), scale)
        tiles.append((i, kind, img))
    cols = 3
    cw = max(t[2].width for t in tiles) + 20
    ch = max(t[2].height for t in tiles) + 50
    rows = (len(tiles) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * cw + 20, rows * ch + 20), (40, 40, 38))
    d = ImageDraw.Draw(sheet)
    for k, (i, kind, img) in enumerate(tiles):
        x, y = 20 + (k % cols) * cw, 20 + (k // cols) * ch
        d.text((x, y), f"{i}（{NAMES.get(kind, kind)}）", fill=(240, 236, 220), font=font)
        sheet.paste(img, (x, y + 36))
    sheet.save(out)
    print(out, sheet.size)


if __name__ == "__main__":
    main()
