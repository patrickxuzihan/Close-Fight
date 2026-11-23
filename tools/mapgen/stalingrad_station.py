"""
Generates the first draft of the Stalingrad-1 station map (M8, D48) and its
scenario. Hand-placed key features (yard, station, square, fountain, blocks)
plus a seeded fill for the private sector west of the tracks. The user then
refines the layout in Unity with the map editor (D19).

Run:  python3 tools/mapgen/stalingrad_station.py
Writes Assets/CloseFight/Data/Maps/stalingrad_station.json and
       Assets/CloseFight/Data/Scenarios/stalingrad_station_sept14.json
"""
import json
import math
import os
import random

ROOT = os.path.join(os.path.dirname(__file__), "..", "..")
W, H = 360.0, 280.0
rng = random.Random(19420914)


def rect(x0, y0, x1, y1):
    return [{"x": float(x0), "y": float(y0)}, {"x": float(x1), "y": float(y0)},
            {"x": float(x1), "y": float(y1)}, {"x": float(x0), "y": float(y1)}]


def poly(points):
    return [{"x": round(float(x), 2), "y": round(float(y), 2)} for x, y in points]


def ngon(cx, cy, r, n=8, phase=0.0):
    return poly([(cx + r * math.cos(phase + i * 2 * math.pi / n), cy + r * math.sin(phase + i * 2 * math.pi / n)) for i in range(n)])


areas, buildings, walls, props = [], [], [], []
next_id = [1]
occupied = []  # rectangles kept clear of the private-sector fill


def area(terrain, polygon):
    areas.append({"terrain": terrain, "polygon": polygon})


def building(x0, y0, x1, y1, floors, material="Brick", name=""):
    buildings.append({"id": next_id[0], "nameKey": name, "material": material, "floors": floors, "polygon": rect(x0, y0, x1, y1)})
    next_id[0] += 1
    occupied.append((x0 - 2, y0 - 2, x1 + 2, y1 + 2))


def prop(kind, polygon, height, cover):
    props.append({"kind": kind, "polygon": polygon, "height": height, "cover": cover})


def wall(points, height=2.2, blocks=True, crushable=False):
    walls.append({"points": poly(points), "height": height, "blocksMovement": blocks, "crushable": crushable})


def free(x0, y0, x1, y1):
    for (a, b, c, d) in occupied:
        if x0 < c and x1 > a and y0 < d and y1 > b:
            return False
    return True


# --- roads and ground
area("Road", rect(0, 58, W, 66))          # southern avenue (StuG approach)
area("Road", rect(0, 196, W, 204))        # northern street
area("Road", rect(44, 0, 50, H))          # lane through the private sector
area("Road", rect(266, 0, 274, H))        # eastern street
area("Rail", rect(92, 0, 142, H))         # railway yard
area("Road", rect(204, 92, 262, 172))     # station square
occupied += [(0, 56, W, 68), (0, 194, W, 206), (42, 0, 52, H), (90, 0, 144, H)]

# --- railway yard: strings of freight wagons (3 x 11 m) on several tracks
for track_x, spans in [
    (100, [(18, 29), (31, 42), (78, 89), (148, 159), (161, 172), (228, 239)]),
    (112, [(46, 57), (59, 70), (108, 119), (210, 221), (223, 234)]),
    (124, [(24, 35), (84, 95), (97, 108), (168, 179), (250, 261)]),
    (136, [(36, 47), (128, 139), (141, 152), (238, 249)]),
]:
    for y0, y1 in spans:
        if y1 < 56 or y0 > 68:  # keep the avenue crossing clear
            if y1 < 194 or y0 > 206:
                prop("wagon", rect(track_x - 1.5, y0, track_x + 1.5, y1), 3.2, 75)
building(93, 176, 99, 182, 4, "Brick", "")  # water tower

# --- Stalingrad-1 station: clock tower first (it claims its cells), then the main hall
building(168, 120, 182, 140, 3, "Brick", "building.station_tower")
building(146, 108, 204, 152, 2, "Brick", "building.station")
# platform shelter walls along the yard side
wall([(144, 104), (144, 156)], height=1.0)

# --- station square with the Barmaley fountain and street barricades
prop("fountain", ngon(236, 132, 6.0, 8, math.pi / 8), 1.2, 55)
for x, y, horizontal in [(222, 104, True), (250, 162, True), (214, 150, False), (256, 118, False)]:
    prop("barricade", rect(x - 3, y - 0.8, x + 3, y + 0.8) if horizontal else rect(x - 0.8, y - 3, x + 0.8, y + 3), 1.3, 65)

# --- around the station
building(150, 212, 200, 252, 1, "Concrete", "building.warehouse")
building(150, 16, 196, 50, 2, "Brick", "building.post_office")
area("Rubble", poly([(206, 18), (258, 20), (256, 52), (210, 50)]))
for x0, y0 in [(152, 162), (166, 164), (182, 160)]:
    building(x0, y0, x0 + 9, y0 + 8, 1, "Wood")

# --- eastern city blocks (Soviet rear, toward the Volga)
building(280, 76, 312, 122, 4, "Brick", "building.east_corner")
building(320, 76, 356, 118, 3, "Brick")
building(280, 134, 316, 186, 3, "Brick")
building(324, 132, 356, 188, 4, "Concrete")
building(280, 212, 314, 252, 2, "Brick")
building(322, 212, 356, 252, 2, "Brick")
building(280, 14, 318, 52, 3, "Brick")
area("Rubble", poly([(326, 18), (356, 16), (354, 50), (328, 48)]))
area("Rubble", poly([(312, 122), (320, 118), (320, 132), (312, 134)]))
wall([(316, 186), (324, 188)], height=2.4)  # courtyard wall between the blocks

# --- signal box (Stellwerk), the Soviet outpost west of the tracks
building(72, 122, 86, 138, 2, "Brick", "building.signal_box")

# --- engine shed north-west
building(58, 214, 88, 250, 1, "Concrete", "building.engine_shed")

# --- private sector west of the tracks: wooden houses, many burned out on 23 August
for row_y in list(range(8, 50, 15)) + list(range(72, 190, 15)) + list(range(210, 270, 15)):
    for col_x in list(range(4, 42, 12)) + list(range(54, 88, 12)):
        x0 = col_x + rng.uniform(0, 3)
        y0 = row_y + rng.uniform(0, 3)
        w = rng.uniform(6.5, 9)
        h = rng.uniform(6, 8)
        if not free(x0, y0, x0 + w, y0 + h):
            continue
        roll = rng.random()
        if roll < 0.40:
            # burned: a rubble heap where the house stood
            area("Rubble", poly([(x0, y0), (x0 + w, y0 + rng.uniform(-1, 1)), (x0 + w + rng.uniform(-1, 1), y0 + h), (x0 + rng.uniform(-1, 1), y0 + h)]))
            occupied.append((x0 - 1, y0 - 1, x0 + w + 1, y0 + h + 1))
        elif roll < 0.88:
            building(round(x0, 1), round(y0, 1), round(x0 + w, 1), round(y0 + h, 1), 1, "Wood")
        else:
            building(round(x0, 1), round(y0, 1), round(x0 + w, 1), round(y0 + h, 1), 2, "Brick")
        # garden fences around some plots
        if rng.random() < 0.3:
            fx0, fy0, fx1, fy1 = x0 - 1.5, y0 - 1.5, x0 + w + 2.5, y0 + h + 2.5
            wall([(fx0 + 2, fy0), (fx1, fy0), (fx1, fy1), (fx0, fy1), (fx0, fy0 + 2)], height=1.0, blocks=False, crushable=True)

# gardens
for x0, y0, x1, y1 in [(4, 190, 40, 194), (54, 52, 88, 56), (4, 52, 40, 56), (54, 190, 88, 194)]:
    area("Grass", rect(x0, y0, x1, y1))

# --- shell craters everywhere
for _ in range(45):
    cx, cy = rng.uniform(8, W - 8), rng.uniform(8, H - 8)
    r = rng.uniform(1.5, 3.5)
    area("Crater", ngon(cx, cy, r, 7, rng.uniform(0, 1)))

mapdef = {
    "id": "stalingrad_station", "nameKey": "map.stalingrad_station.name",
    "width": W, "height": H, "cellSize": 1.0, "baseTerrain": "Open",
    "areas": areas, "buildings": buildings, "walls": walls, "props": props,
}


def team(key, side, typ, name, x, y, facing, exp=50, order=None, equipment=None):
    d = {"key": key, "side": side, "type": typ, "displayName": name, "size": 0,
         "position": {"x": x, "y": y}, "facing": facing, "experience": exp}
    if order:
        d["initialOrder"] = order
    if equipment:
        d["equipment"] = equipment
    return d


def objective(id, key, x, y, r, v):
    return {"id": id, "nameKey": key, "position": {"x": x, "y": y}, "radius": r, "value": v, "owner": "Soviet"}


scenario = {
    "id": "stalingrad_station_sept14", "mapId": "stalingrad_station", "seed": 19420914,
    "playerSide": "German", "timeLimitMinutes": 45,
    "sides": [{"side": "German", "retreatEdge": "West"}, {"side": "Soviet", "retreatEdge": "East"}],
    "teams": [
        # 71st Infantry Division, assault on the central station, 14 September 1942
        team("de_hq", "German", "Headquarters", "Kompanietrupp", 14, 140, 90, 60),
        team("de_1", "German", "Rifle", "1. Gruppe", 18, 32, 90),
        team("de_2", "German", "Rifle", "2. Gruppe", 18, 96, 90),
        team("de_3", "German", "Rifle", "3. Gruppe", 18, 170, 90),
        team("de_4", "German", "Rifle", "4. Gruppe", 18, 236, 90),
        team("de_mg1", "German", "MachineGun", "MG-Trupp 1", 24, 118, 90, 55),
        team("de_mg2", "German", "MachineGun", "MG-Trupp 2", 24, 214, 90, 55),
        team("de_mortar", "German", "Mortar", "Granatwerfer", 6, 152, 90, 50, equipment="grw34"),
        team("de_pak", "German", "AntiTankGun", "PaK 38", 12, 186, 90, 50, equipment="pak38"),
        team("de_stug", "German", "Vehicle", "Sturmgeschütz", 14, 62, 90, 55, equipment="stug3f"),
        # 13th Guards Rifle Division and NKVD troops holding the station
        team("su_1", "Soviet", "Rifle", "1-ye otdeleniye", 79, 130, 270, 45, order="Ambush"),
        team("su_2", "Soviet", "Rifle", "2-ye otdeleniye", 156, 145, 270, 50, order="Ambush"),
        team("su_3", "Soviet", "Rifle", "3-ye otdeleniye", 156, 114, 270, 50, order="Ambush"),
        team("su_mg1", "Soviet", "MachineGun", "Pulemyot 1", 175, 130, 270, 50, order="Ambush"),
        team("su_mg2", "Soviet", "MachineGun", "Pulemyot 2", 286, 160, 270, 45, order="Ambush"),
        team("su_at", "Soviet", "AntiTankGun", "Sorokapyatka", 208, 132, 270, 50, order="Ambush", equipment="m1937_45"),
        team("su_4", "Soviet", "Rifle", "4-ye otdeleniye", 290, 100, 270, 45, order="Ambush"),
        team("su_5", "Soviet", "Rifle", "5-ye otdeleniye", 330, 150, 270, 45, order="Ambush"),
        team("su_hq", "Soviet", "Headquarters", "Shtab roty", 340, 100, 270, 55),
        team("su_mortar", "Soviet", "Mortar", "Minomyot", 345, 200, 270, 45, equipment="bm37"),
        team("su_t60", "Soviet", "Vehicle", "T-60", 270, 62, 270, 45, order="Ambush", equipment="t60"),
    ],
    "objectives": [
        objective("signal_box", "objective.signal_box", 79, 130, 10, 1),
        objective("station", "objective.station", 175, 130, 18, 3),
        objective("square", "objective.square", 236, 132, 14, 2),
        objective("east_corner", "objective.east_corner", 296, 99, 12, 1),
    ],
}

with open(os.path.join(ROOT, "Assets/CloseFight/Data/Maps/stalingrad_station.json"), "w", encoding="utf-8") as f:
    json.dump(mapdef, f, indent=1, ensure_ascii=False)
with open(os.path.join(ROOT, "Assets/CloseFight/Data/Scenarios/stalingrad_station_sept14.json"), "w", encoding="utf-8") as f:
    json.dump(scenario, f, indent=2, ensure_ascii=False)
print(f"buildings {len(buildings)}, areas {len(areas)}, walls {len(walls)}, props {len(props)}")
