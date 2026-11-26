#!/usr/bin/env python3
"""Builds the Stalingrad campaign map (M16, D12, D55): 13-26 September 1942.

    python3 tools/campaign/make_stalingrad.py

Writes Assets/CloseFight/Data/Campaigns/stalingrad_center.json, adds the district names to
the localization table, and draws docs/images/m16_districts_draft.png for review.

The strategic map is 1600 x 1900 units, north at the top. The city lies on the west bank of
the Volga; across the river are the east bank and three islands, Soviet ground closed to the
Germans. The city centre is ringed by the factory district to the north, the western and
southern outskirts where the German supply points are, and the river to the east.

Districts are placed by hand. On each bank the outlines are the Voronoi cells of the label
points, and two districts are neighbours when their cells share a border; the river is
crossed only by the routes listed in RIVER_ROUTES, between the landing stages, the islands
and the east bank. So the drawing and the rules always agree. Move a label point and run
again to reshape the map.

Owners and regiments follow the situation on the evening of 13 September 1942 as far as the
secondary sources go. It is a draft to be checked. Needs Pillow.
"""
import json
import math
import os

from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
OUT = os.path.join(ROOT, "Assets", "CloseFight", "Data", "Campaigns", "stalingrad_center.json")
STRINGS = os.path.join(ROOT, "Assets", "CloseFight", "Data", "Localization", "strings.json")
SKETCH = os.path.join(ROOT, "docs", "images", "m16_districts_draft.png")

W, H = 1600, 1900
# The Volga's banks, north to south.
WEST_BANK = [(1130, 0), (1180, 300), (1230, 700), (1260, 1100), (1230, 1600), (1170, 1900)]
EAST_BANK = [(x + 170, y) for x, y in WEST_BANK]
CITY = [(0, 0)] + WEST_BANK + [(0, H)]
FAR_BANK = EAST_BANK + [(W, H), (W, 0)]

G, S = "German", "Soviet"

# id, (x, y), kind, owner, zh, en, flags. Flags: key, depot (side), crossing (supplies only at
# night), landing (river routes come ashore), map (hand-made battle map).
CITY_DISTRICTS = [
    # North: the factory district and the settlements
    ("orlovka", (330, 80), "Outskirts", S, "奥尔洛夫卡", "Orlovka", {}),
    ("workers", (590, 170), "Residential", S, "街垒厂工人村", "Barrikady workers' settlement", {}),
    ("barrikady", (800, 150), "Industrial", S, "街垒厂", "Barrikady factory", {}),
    ("tractor", (950, 50), "Industrial", S, "拖拉机厂", "Tractor factory", {}),
    ("spartanovka", (1085, 110), "Residential", S, "斯巴达诺夫卡", "Spartanovka", {"landing": True}),
    ("bannyi", (690, 330), "Hill", S, "班内冲沟", "Bannyi ravine", {}),
    ("red_october", (930, 330), "Industrial", S, "红十月厂", "Red October factory", {}),
    ("red_october_landing", (1110, 380), "Riverside", S, "红十月厂码头", "Red October landing", {"landing": True}),
    # West: the outskirts the Germans came through, and their supply points
    ("gorodishche", (110, 220), "Outskirts", G, "戈罗季谢", "Gorodishche", {"depot": G}),
    ("razgulyaevka", (420, 430), "Outskirts", G, "拉兹古利亚耶夫卡", "Razgulyayevka", {}),
    ("gumrak", (110, 620), "Outskirts", G, "古姆拉克（机场）", "Gumrak (airfield)", {"depot": G}),
    ("hill_146", (250, 860), "Hill", G, "146.1 高地", "Height 146.1", {}),
    ("sadovaya", (120, 1070), "Railway", G, "萨多瓦亚车站", "Sadovaya station", {}),
    ("upper_yelshanka", (130, 1360), "Outskirts", G, "上叶尔尚卡", "Verkhnyaya Yelshanka", {}),
    ("peschanka", (130, 1660), "Outskirts", G, "佩先卡", "Peschanka", {"depot": G}),
    # The city centre (the first draft's 21 districts, less the north-western supply point)
    ("mamayev", (720, 480), "Hill", S, "马马耶夫岗（102 高地）", "Mamayev Kurgan (Height 102.0)", {"key": True}),
    ("ravines", (1060, 560), "Riverside", S, "多尔吉与克鲁托伊冲沟", "Dolgiy and Krutoy ravines", {}),
    ("aviagorodok", (440, 750), "Outskirts", G, "航空城", "Aviagorodok", {}),
    ("red_barracks", (640, 700), "Residential", S, "红色兵营", "Red Barracks", {}),
    ("nail_factory", (870, 660), "Industrial", S, "制钉厂", "Nail factory", {}),
    ("specialists", (1110, 750), "Riverside", S, "专家楼", "Specialists' Houses", {}),
    ("hill_112", (590, 950), "Hill", S, "112.5 高地", "Height 112.5", {}),
    ("station", (810, 910), "Railway", S, "第一火车站", "Stalingrad-1 station", {"key": True, "map": "stalingrad_station"}),
    ("january9", (1010, 860), "Square", S, "1 月 9 日广场", "9 January Square", {}),
    ("red_square", (950, 1040), "Square", S, "阵亡战士广场", "Square of the Fallen Fighters", {"key": True}),
    ("landing", (1165, 990), "Riverside", S, "中央码头", "Central landing stage", {"key": True, "landing": True}),
    ("state_bank", (1130, 1180), "Riverside", S, "国家银行与老城", "State Bank and old town", {}),
    ("tsaritsa_west", (450, 1160), "Outskirts", G, "察里津河上游", "Upper Tsaritsa", {}),
    ("tsaritsa", (850, 1220), "Hill", S, "察里津河谷", "Tsaritsa gorge", {}),
    ("minina", (520, 1370), "Outskirts", G, "米宁诺郊区", "Minina suburb", {}),
    ("station2", (790, 1400), "Railway", S, "第二火车站", "Stalingrad-2 station", {}),
    ("elevator", (980, 1450), "Industrial", S, "粮仓", "Grain elevator", {"key": True}),
    ("cannery", (1150, 1360), "Riverside", S, "罐头厂与南部河岸", "Cannery and southern bank", {}),
    ("yelshanka", (590, 1580), "Outskirts", G, "叶尔尚卡", "Yelshanka", {}),
    # South: where the Germans reached the Volga on 10 September, and the 64th Army beyond
    ("voroponovo", (420, 1830), "Railway", G, "沃罗波诺沃车站", "Voroponovo station", {}),
    ("south_suburb", (780, 1760), "Residential", G, "南郊住宅区", "Southern suburb", {}),
    ("kuperosnoye", (1060, 1650), "Riverside", G, "库波罗斯诺耶", "Kuperosnoye", {}),
    ("beketovka", (1040, 1835), "Riverside", S, "别克托夫卡", "Beketovka", {"landing": True}),
]

FAR_DISTRICTS = [
    ("tumak", (1480, 250), "Outskirts", S, "图马克（对岸）", "Tumak (east bank)", {"depot": S, "crossing": True}),
    ("krasnaya_sloboda", (1500, 950), "Outskirts", S, "红斯洛博达（对岸）", "Krasnaya Sloboda (east bank)", {"depot": S, "crossing": True}),
    ("akhtuba", (1490, 1650), "Outskirts", S, "阿赫图巴（对岸）", "Akhtuba (east bank)", {"depot": S, "crossing": True}),
]

# Islands in the river: id, centre, half width, half height, zh, en
ISLANDS = [
    ("zaitsevsky", (1242, 170), 42, 120, "扎伊采夫斯基岛", "Zaitsevsky island"),
    ("golodny", (1337, 1220), 40, 140, "戈洛德内岛", "Golodny island"),
    ("sarpinsky", (1287, 1730), 48, 150, "萨尔平斯基岛", "Sarpinsky island"),
]

# The only ways across the Volga.
RIVER_ROUTES = [
    ("spartanovka", "zaitsevsky"), ("zaitsevsky", "tumak"),
    ("red_october_landing", "tumak"),
    ("landing", "krasnaya_sloboda"), ("landing", "golodny"), ("state_bank", "golodny"), ("golodny", "krasnaya_sloboda"),
    ("beketovka", "sarpinsky"), ("kuperosnoye", "sarpinsky"), ("sarpinsky", "akhtuba"),
]


def infantry(prefix, number):
    """A German infantry regiment of 1942 as far as battles use it: staff, three rifle
    companies, a machine-gun company and the Panzerjäger company."""
    return [
        ("de_staff", f"Stab {prefix} {number}"),
        ("de_rifle", f"1./{prefix} {number}"),
        ("de_rifle", f"2./{prefix} {number}"),
        ("de_rifle", f"3./{prefix} {number}"),
        ("de_mg", f"4./{prefix} {number}"),
        ("de_antitank", f"14./{prefix} {number}"),
    ]


def soviet(name, rifle=3, mg=True, mortar=True, antitank=False, sapper=False):
    companies = [("su_staff", f"Shtab {name}")]
    companies += [("su_rifle", f"{i + 1}-ya rota, {name}") for i in range(rifle)]
    if mg:
        companies.append(("su_mg", f"Pulemyotnaya rota, {name}"))
    if mortar:
        companies.append(("su_mortar", f"Minomyotnaya rota, {name}"))
    if antitank:
        companies.append(("su_antitank", f"Batareya PTO, {name}"))
    if sapper:
        companies.append(("su_sapper", f"Sapyornaya rota, {name}"))
    return companies


def tank_brigade(name):
    return [("su_staff", f"Shtab {name}"), ("su_tank", "1-ya tankovaya rota"), ("su_light_tank", "2-ya tankovaya rota"),
            ("su_rifle", f"Motostrelkovaya rota, {name}")]


def regiment(rid, side, name, division, district, companies, experience=55, strength=0.6, morale=70):
    return {
        "id": rid, "side": side, "name": name, "division": division, "district": district,
        "experience": experience, "strength": strength, "morale": morale,
        "companies": [{"template": t, "name": n} for t, n in companies],
    }


REGIMENTS = [
    # LI Army Corps: 389., 295. and 71. ID, with the 76. ID behind them
    regiment("de_ir544", G, "IR 544", "389. ID", "gorodishche", infantry("IR", 544)),
    regiment("de_ir546", G, "IR 546", "389. ID", "gorodishche", infantry("IR", 546) + [("de_pioneer", "Pi.-Btl. 389")]),
    regiment("de_ir516", G, "IR 516", "295. ID", "razgulyaevka", infantry("IR", 516) + [("de_pioneer", "Pi.-Btl. 295")]),
    regiment("de_ir518", G, "IR 518", "295. ID", "razgulyaevka", infantry("IR", 518) + [("de_stug", "StuG-Abt. 245")]),
    regiment("de_ir203", G, "IR 203", "76. ID", "gumrak", infantry("IR", 203)),
    regiment("de_ir194", G, "IR 194", "71. ID", "aviagorodok", infantry("IR", 194) + [("de_stug", "StuG-Abt. 244")]),
    regiment("de_ir191", G, "IR 191", "71. ID", "aviagorodok", infantry("IR", 191) + [("de_pioneer", "Pi.-Btl. 171")]),
    regiment("de_ir211", G, "IR 211", "71. ID", "tsaritsa_west", infantry("IR", 211)),
    # XXXXVIII Panzer Corps: 24. and 14. PD, 94. ID, 29. ID (mot.)
    regiment("de_sr21", G, "SR 21", "24. PD", "tsaritsa_west", infantry("SR", 21) + [("de_panzer", "Pz.-Rgt. 24")], experience=60),
    regiment("de_sr26", G, "SR 26", "24. PD", "minina", infantry("SR", 26) + [("de_panzer", "Pz.-Rgt. 24")], experience=60),
    regiment("de_ir267", G, "IR 267", "94. ID", "minina", infantry("IR", 267)),
    regiment("de_ir276", G, "IR 276", "94. ID", "yelshanka", infantry("IR", 276) + [("de_pioneer", "Pi.-Btl. 194")]),
    regiment("de_ir71", G, "IR 71 (mot.)", "29. ID (mot.)", "yelshanka", infantry("IR", 71) + [("de_panzer", "Pz.-Abt. 129")]),
    regiment("de_ir15", G, "IR 15 (mot.)", "29. ID (mot.)", "south_suburb", infantry("IR", 15)),
    regiment("de_sr103", G, "SR 103", "14. PD", "kuperosnoye", infantry("SR", 103) + [("de_panzer", "Pz.-Rgt. 36")], experience=60),
    regiment("de_sr108", G, "SR 108", "14. PD", "voroponovo", infantry("SR", 108), experience=60),
    # 62nd Army, north to south
    regiment("su_115osbr", S, "115-ya osbr", "115-ya osbr", "orlovka", soviet("115-ya osbr", antitank=True), experience=45, strength=0.55),
    regiment("su_124osbr", S, "124-ya osbr", "Gruppa Gorokhova", "spartanovka", soviet("124-ya osbr", antitank=True), experience=50, strength=0.65),
    regiment("su_149osbr", S, "149-ya osbr", "Gruppa Gorokhova", "tractor", soviet("149-ya osbr"), experience=50, strength=0.6),
    regiment("su_189tbr", S, "189-ya tbr", "23-y tk", "barrikady", tank_brigade("189-ya tbr"), experience=50),
    regiment("su_282nkvd", S, "282-y sp NKVD", "10-ya sd NKVD", "workers", soviet("282-y sp NKVD"), experience=50),
    regiment("su_269nkvd", S, "269-y sp NKVD", "10-ya sd NKVD", "mamayev", soviet("269-y sp NKVD"), experience=50),
    regiment("su_416sp", S, "416-y sp", "112-ya sd", "red_barracks", soviet("416-y sp", rifle=2), experience=45, strength=0.4),
    regiment("su_524sp", S, "524-y sp", "112-ya sd", "nail_factory", soviet("524-y sp", rifle=2), experience=45, strength=0.4),
    regiment("su_42osbr", S, "42-ya osbr", "42-ya osbr", "hill_112", soviet("42-ya osbr", antitank=True), experience=50, strength=0.5),
    regiment("su_272nkvd", S, "272-y sp NKVD", "10-ya sd NKVD", "station", soviet("272-y sp NKVD", antitank=True, sapper=True), experience=50),
    regiment("su_6gvtbr", S, "6-ya gv. tbr", "6-ya gv. tbr", "red_square", tank_brigade("6-y gv. tbr"), experience=55),
    regiment("su_907sp", S, "907-y sp", "244-ya sd", "tsaritsa", soviet("907-y sp", rifle=2), experience=45, strength=0.35),
    regiment("su_100gvsp", S, "100-y gv. sp", "35-ya gv. sd", "station2", soviet("100-y gv. sp", antitank=True), experience=55, strength=0.45),
    regiment("su_101gvsp", S, "101-y gv. sp", "35-ya gv. sd", "elevator", soviet("101-y gv. sp", antitank=True), experience=55, strength=0.45),
    regiment("su_10osbr", S, "10-ya osbr", "10-ya osbr", "cannery", soviet("10-ya osbr"), experience=45, strength=0.5),
    # 64th Army south of the German wedge at Kuperosnoye
    regiment("su_106gvsp", S, "106-y gv. sp", "36-ya gv. sd", "beketovka", soviet("106-y gv. sp", antitank=True), experience=55, strength=0.6),
    regiment("su_550sp", S, "550-y sp", "126-ya sd", "beketovka", soviet("550-y sp"), experience=45, strength=0.5),
]

GUARDS = dict(antitank=True, sapper=True)
ARRIVALS = [
    # Turn 4 is the night of 14/15 September: the 13th Guards Rifle Division crosses to the central landing.
    (4, regiment("su_42gvsp", S, "42-y gv. sp", "13-ya gv. sd", "landing", soviet("42-y gv. sp", **GUARDS), experience=60, strength=0.85, morale=85)),
    (4, regiment("su_39gvsp", S, "39-y gv. sp", "13-ya gv. sd", "landing", soviet("39-y gv. sp", **GUARDS), experience=60, strength=0.85, morale=85)),
    (6, regiment("su_34gvsp", S, "34-y gv. sp", "13-ya gv. sd", "krasnaya_sloboda", soviet("34-y gv. sp", **GUARDS), experience=60, strength=0.85, morale=85)),
    (10, regiment("su_92osbr", S, "92-ya osbr", "92-ya osbr (morskaya pekhota)", "akhtuba", soviet("92-ya osbr", antitank=True), experience=55, strength=0.8, morale=80)),
    (14, regiment("su_90sp", S, "90-y sp", "95-ya sd", "tumak", soviet("90-y sp", antitank=True), experience=50, strength=0.75, morale=75)),
    (14, regiment("su_161sp", S, "161-y sp", "95-ya sd", "tumak", soviet("161-y sp", antitank=True), experience=50, strength=0.75, morale=75)),
    (22, regiment("su_1043sp", S, "1043-y sp", "284-ya sd", "krasnaya_sloboda", soviet("1043-y sp", sapper=True), experience=50, strength=0.8, morale=75)),
    (22, regiment("su_1045sp", S, "1045-y sp", "284-ya sd", "krasnaya_sloboda", soviet("1045-y sp", sapper=True), experience=50, strength=0.8, morale=75)),
    (25, regiment("de_jr54", G, "Jäger-Rgt. 54", "100. JD", "gumrak", infantry("JR", 54), experience=55, strength=0.8, morale=75)),
    (25, regiment("de_jr227", G, "Jäger-Rgt. 227", "100. JD", "gumrak", infantry("JR", 227), experience=55, strength=0.8, morale=75)),
]

PURCHASES = [
    {"side": G, "template": "de_rifle", "name": "Marsch-Kompanie", "cost": 160},
    {"side": G, "template": "de_pioneer", "name": "Pionier-Kompanie", "cost": 140},
    {"side": G, "template": "de_stug", "name": "StuG-Batterie", "cost": 240},
    {"side": G, "template": "de_panzer", "name": "Panzer-Kompanie", "cost": 260},
    {"side": S, "template": "su_rifle", "name": "Marshevaya rota", "cost": 140},
    {"side": S, "template": "su_sapper", "name": "Shturmovaya gruppa", "cost": 120},
    {"side": S, "template": "su_antitank", "name": "Batareya PTO", "cost": 110},
    {"side": S, "template": "su_tank", "name": "Tankovaya rota", "cost": 240},
]


def clip(polygon, a, b, c):
    """Keeps the part of a polygon where a*x + b*y <= c (Sutherland-Hodgman against one half-plane)."""
    out = []
    for i, p in enumerate(polygon):
        q = polygon[(i + 1) % len(polygon)]
        dp, dq = a * p[0] + b * p[1] - c, a * q[0] + b * q[1] - c
        if dp <= 0:
            out.append(p)
        if dp * dq < 0:
            t = dp / (dp - dq)
            out.append((p[0] + t * (q[0] - p[0]), p[1] + t * (q[1] - p[1])))
    return out


def bisector(si, sj):
    a, b = sj[0] - si[0], sj[1] - si[1]
    return a, b, (sj[0] ** 2 + sj[1] ** 2 - si[0] ** 2 - si[1] ** 2) / 2


def voronoi(sites, region):
    cells = []
    for i, si in enumerate(sites):
        poly = list(region)
        for j, sj in enumerate(sites):
            if i != j:
                poly = clip(poly, *bisector(si, sj))
        cells.append(poly)
    return cells


def shared_border(cell, si, sj):
    """Length of a cell's border lying on the bisector with another site."""
    a, b, c = bisector(si, sj)
    norm = math.hypot(a, b)
    total = 0.0
    for k, p in enumerate(cell):
        q = cell[(k + 1) % len(cell)]
        if abs(a * p[0] + b * p[1] - c) / norm < 0.5 and abs(a * q[0] + b * q[1] - c) / norm < 0.5:
            total += math.hypot(q[0] - p[0], q[1] - p[1])
    return total


def island_outline(cx, cy, rx, ry, n=16):
    return [(cx + rx * math.sin(2 * math.pi * k / n), cy - ry * math.cos(2 * math.pi * k / n)) for k in range(n)]


def build():
    entries = []

    def add(group, region):
        sites = [d[1] for d in group]
        cells = voronoi(sites, region)
        for i, (did, (x, y), kind, owner, zh, en, flags) in enumerate(group):
            neighbours = [group[j][0] for j in range(len(group)) if j != i and shared_border(cells[i], sites[i], sites[j]) > 25]
            entries.append((did, (x, y), kind, owner, zh, en, flags, cells[i], neighbours))

    add(CITY_DISTRICTS, CITY)
    add(FAR_DISTRICTS, FAR_BANK)
    for did, (x, y), rx, ry, zh, en in ISLANDS:
        entries.append((did, (x, y), "Riverside", S, zh, en, {}, island_outline(x, y, rx, ry), []))

    by_id = {e[0]: e for e in entries}
    for a, b in RIVER_ROUTES:
        by_id[a][8].append(b)
        by_id[b][8].append(a)

    east = {d[0] for d in FAR_DISTRICTS} | {i[0] for i in ISLANDS}
    districts = []
    for did, (x, y), kind, owner, zh, en, flags, poly, neighbours in entries:
        entry = {
            "id": did, "nameKey": f"district.{did}", "kind": kind, "owner": owner, "neighbours": neighbours,
            "key": bool(flags.get("key")), "crossing": bool(flags.get("crossing")), "landing": bool(flags.get("landing")),
            "mapId": flags.get("map", ""),
            "outline": [{"x": round(px, 1), "y": round(py, 1)} for px, py in poly],
            "label": {"x": x, "y": y},
        }
        if "depot" in flags:
            entry["depotOf"] = flags["depot"]
        if did in east:
            entry["closedTo"] = G
        districts.append(entry)

    campaign = {
        "id": "stalingrad_center",
        "nameKey": "campaign.stalingrad_center",
        "startDate": "1942-09-13",
        "turns": 28,
        "seed": 1942,
        "playerSide": G,
        "attacker": G,
        "districts": districts,
        "regiments": REGIMENTS,
        "arrivals": [{"turn": t, "regiment": r} for t, r in ARRIVALS],
        "purchases": PURCHASES,
    }
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "w") as f:
        json.dump(campaign, f, ensure_ascii=False, indent=2)
        f.write("\n")

    strings = json.load(open(STRINGS))
    strings = {k: v for k, v in strings.items() if not k.startswith("district.")}
    strings["campaign.stalingrad_center"] = {"zh": "斯大林格勒，1942 年 9 月 13–26 日", "en": "Stalingrad, 13–26 September 1942"}
    for did, _, _, _, zh, en, _, _, _ in entries:
        strings[f"district.{did}"] = {"zh": zh, "en": en}
    with open(STRINGS, "w") as f:
        f.write(json.dumps(strings, ensure_ascii=False, indent=2) + "\n")
    return campaign, entries


def sketch(campaign, entries):
    ox, oy = 20, 70
    img = Image.new("RGB", (W + 420, H + 100), (238, 232, 214))
    d = ImageDraw.Draw(img)
    font = ImageFont.truetype("/System/Library/Fonts/Hiragino Sans GB.ttc", 19)
    big = ImageFont.truetype("/System/Library/Fonts/Hiragino Sans GB.ttc", 28)
    small = ImageFont.truetype("/System/Library/Fonts/Supplemental/Arial Unicode.ttf", 13)
    tiny = ImageFont.truetype("/System/Library/Fonts/Hiragino Sans GB.ttc", 13)

    def P(points):
        return [(x + ox, y + oy) for x, y in points]

    d.polygon(P(WEST_BANK + EAST_BANK[::-1]), fill=(150, 182, 204))
    colours = {G: (176, 180, 190), S: (214, 170, 160)}
    far = (222, 196, 186)
    east = {e[0] for e in FAR_DISTRICTS} | {i[0] for i in ISLANDS}
    for did, _, _, owner, _, _, _, poly, _ in entries:
        d.polygon(P(poly), fill=far if did in east and owner == S else colours[owner], outline=(60, 55, 50))

    centres = {e[0]: e[1] for e in entries}
    for a, b in RIVER_ROUTES:
        (x1, y1), (x2, y2) = centres[a], centres[b]
        n = 14
        for k in range(n):
            if k % 2 == 0:
                t0, t1 = k / n, (k + 1) / n
                d.line(P([(x1 + (x2 - x1) * t0, y1 + (y2 - y1) * t0), (x1 + (x2 - x1) * t1, y1 + (y2 - y1) * t1)]), fill=(30, 60, 110), width=3)

    d.text((1270 + ox, 560 + oy), "伏\n尔\n加\n河", fill=(40, 70, 100), font=big)
    for did, (x, y), _, owner, zh, _, flags, _, _ in entries:
        x, y = x + ox, y + oy
        mark = ("★ " if flags.get("key") else "") + zh
        if did in {i[0] for i in ISLANDS}:
            d.text((x - d.textlength(zh[:-1], font=tiny) / 2, y - 30), "\n".join(zh), fill=(30, 28, 25), font=tiny)
            continue
        d.text((x - d.textlength(mark, font=font) / 2, y - 26), mark, fill=(30, 28, 25), font=font)
        tags = []
        if flags.get("depot"):
            tags.append("补给点" + ("（夜间过河）" if flags.get("crossing") else ""))
        if flags.get("landing"):
            tags.append("码头")
        if flags.get("map"):
            tags.append("现有地图")
        if tags:
            t = " · ".join(tags)
            d.text((x - d.textlength(t, font=tiny) / 2, y - 3), t, fill=(110, 70, 20), font=tiny)
        regs = [r for r in campaign["regiments"] if r["district"] == did]
        for k, r in enumerate(regs):
            label = r["name"]
            bw = d.textlength(label, font=small) + 10
            bx, by = x - bw / 2, y + 15 + k * 21
            d.rectangle([bx, by, bx + bw, by + 18], fill=(70, 80, 95) if r["side"] == G else (150, 40, 35))
            d.text((bx + 5, by + 1), label, fill=(255, 255, 255), font=small)

    d.text((ox, 18), f"M16 区块划分草图（第二版）：{len(entries)} 个区块，1942 年 9 月 13 日晚", fill=(30, 28, 25), font=big)
    lx = W + 50
    legend = [((176, 180, 190), "德军控制"), ((214, 170, 160), "苏军控制"), (far, "东岸和河心岛（德军进不去）"),
              (None, "★ 关键区块"), (None, "虚线：过河路线")]
    for k, (c, t) in enumerate(legend):
        y = 100 + k * 34
        if c:
            d.rectangle([lx, y, lx + 26, y + 22], fill=c, outline=(60, 55, 50))
        d.text((lx + 36, y), t, fill=(30, 28, 25), font=font)
    y = 100 + len(legend) * 34 + 10
    d.text((lx, y), "方块是团（棋子）", fill=(30, 28, 25), font=font)
    y += 30
    d.rectangle([lx, y, lx + 60, y + 18], fill=(70, 80, 95)); d.text((lx + 70, y - 2), "德军", fill=(30, 28, 25), font=font)
    y += 26
    d.rectangle([lx, y, lx + 60, y + 18], fill=(150, 40, 35)); d.text((lx + 70, y - 2), "苏军", fill=(30, 28, 25), font=font)
    y += 50
    d.text((lx, y), "援军（按史实日期）", fill=(30, 28, 25), font=font)
    names = {e[0]: e[4] for e in entries}
    for t, r in ARRIVALS:
        y += 24
        day = 13 + (t - 1) // 2
        half = "夜" if t % 2 == 0 else "昼"
        d.text((lx, y), f"9月{day}日{half}  {r['name']} → {names[r['district']]}", fill=(150, 40, 35) if r["side"] == S else (70, 80, 95), font=small)
    y += 50
    de = sum(1 for r in campaign["regiments"] if r["side"] == G)
    su = len(campaign["regiments"]) - de
    d.text((lx, y), f"开局：德军 {de} 个团，苏军 {su} 个团", fill=(30, 28, 25), font=font)
    img.save(SKETCH)


if __name__ == "__main__":
    campaign, entries = build()
    sketch(campaign, entries)
    for dist in campaign["districts"]:
        print(f"{dist['id']:20s} {dist['owner']:7s} -> {', '.join(dist['neighbours'])}")
    print(len(campaign["districts"]), "districts;", os.path.relpath(OUT, ROOT), os.path.relpath(SKETCH, ROOT))
