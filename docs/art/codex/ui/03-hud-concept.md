# Task U3: the battle HUD's visual design (concepts only)

Before starting, read `docs/art/codex/00-style-guide.md`. Only its rules on forbidden symbols, keeping raw files, `notes.md` and reporting apply. This task is UI design, not a sprite.

**Output folder:** `ArtSource/codex/ui/hud/`

## Why

The game is a top-down WWII tactics game in the manner of the recent Close Combat titles (The Bloody First, Last Stand Arnhem). Its battle screen has a heads-up display (HUD) drawn over a busy map of ruined city blocks.

Most of the HUD still uses the engine's default widgets: grey bevelled buttons, plain labels and boxes. The user says it looks like an old Windows (x86) program: crude and disjointed. Only the roster (the unit column in the top-left corner) was designed. The user likes its structure but not the rest.

We need a **visual direction** for the whole HUD that looks like a modern, polished strategy game, and one specification that the programmer can build every element from.

Look at these screenshots first:

- `docs/images/m15_delegation.png`: the roster as it is now.
  - Keep its structure: a column of platoons, and beside it the open platoon's squads.
  - Each row has a 76 × 54 colour picture of the unit, a name bar coloured by the unit's state (green good, yellow pinned, orange shaken, red broken, grey gone), and a dark band with one small head per man plus a status word.
  - Its exact colours and finish may change.
- `docs/images/m10_attack_ai.png` and `docs/images/m14_night_flares.png`: the map the HUD sits on, by day and by night.

## What the HUD contains

Everything is in Chinese; English must also fit, with the same layout.

1. **Top bar**, full width and thin. It shows:
   - battle time "战斗时间 05:12 / 45:00";
   - objectives score "目标点 德军 2 · 5 苏军";
   - force morale, as a bar with a %;
   - buttons: "支援 12" (off-map support), "照明弹 3" (flares), "请求停火" (cease-fire), "English" (language), and the pause state.
2. **Roster**, top-left: see above.
3. **Order menu**, which opens where the player right-clicks:
   - **Header:** what was clicked, e.g. "目标：建筑（3 层）".
   - **Orders**, about 15 rows, each with a small icon, a name and its hotkey right-aligned:
     - 移动 M, 快速移动 R, 匍匐 C, 开火 F, 区域压制 E, 烟雾 K, 防御 B, 伏击 N, 隐蔽 H, 突入建筑 G, 后撤 T, 倒车 X.
     - One row disabled (greyed out), and one row under the mouse (hover).
   - **Separator**, then 3 rows "托管：弹性防御 Z", "托管：迟滞", "托管：死守", and "取消托管".
4. **Selected-squad panel**, top-right:
   - **Title:** "1. Gruppe — 步兵班".
   - **Summary line:** casualties %, morale, state.
   - **Delegation line:** "托管：弹性防御", with two small buttons "准撤" (approve) and "驳回" (refuse).
   - **Ten rows, one per man:**
     - rank and name, e.g. "下士班长 Friedrich Wagner";
     - weapon and ammo, e.g. "MP40 冲锋枪 · 192";
     - mental state, e.g. "镇定", "被压制" or "溃逃";
     - two thin bars: suppression (red) and morale (blue).
5. **Toast**: a short message near the top centre, e.g. "1. Zug · 1. Gruppe 请求撤退：20 秒内不驳回就自动撤".
6. **Hint**: a hint that follows the cursor while choosing a target, e.g. "移动 — 左键点目标".

## Constraints

- **Compact:** the map matters most. The HUD covers as little of it as possible, with no big window frames.
- **Readable over a busy map:** text 11–14 px at 1× (1600 × 1000), high contrast. It is drawn at 1× and 2× (Retina).
- **Buildable from flat colours plus a few small textures:**
  - solid or translucent panels;
  - thin borders or 1–2 px highlight lines;
  - optional 9-slice textures for panels and buttons;
  - small white icons tinted in code.
  - No glows, blurs or heavy gradients the engine cannot draw.
- **Period flavour without kitsch:**
  - A muted military palette (olive, khaki, field grey, dark slate, a warm accent) suits the 1942 setting.
  - A hint of paper, canvas or stencil type is welcome.
  - Not a pastiche: no fake rivets, wood frames or torn paper.
- **No national symbols:** no swastikas, eagles, iron crosses, red stars or flags anywhere, including as decoration.
- **One font family for everything:**
  - free and open licence;
  - must cover Chinese;
  - for example Noto Sans SC / Source Han Sans, or Noto Serif SC for headings.
  - Name the font and the weights.

## Deliverables

1. **Three different directions:** `concept_a.png`, `concept_b.png`, `concept_c.png`.
   - Each is a full battle screen, 1600 × 1000 px, with every element above on a ruined top-down city map.
   - The order menu is open.
   - Exactly one squad is selected, with its panel shown.
   - One squad in the roster is asking to pull back: its name bar is amber and "准撤 / 驳回" show in its band.
   - Make the three genuinely different, for example:
     - (a) clean, flat and modern;
     - (b) wartime print and paper, with stencil headings;
     - (c) dark translucent glass with a single warm accent.
2. **For each direction, a component sheet** `components_a.png` (and `_b`, `_c`), at 2× scale:
   - buttons: normal, hover, pressed and disabled;
   - a menu row with icon and hotkey, and its hover;
   - the menu header and separator;
   - a panel, a name bar in all five state colours, the head row;
   - the suppression and morale bars;
   - a toast;
   - the small stance tag ("弹性" / "迟滞" / "死守") shown on a roster picture's corner.
3. **`notes.md`:** for each direction:
   - the palette as hex codes, each with what it is for;
   - the font and weights, and the font sizes at 1×;
   - spacing, row heights, border widths and corner radius;
   - which textures (if any) the programmer would need.

Image generation may garble Chinese text. That is acceptable in the concepts, but keep the layout and the text lengths realistic.

## Self-check

- Three clearly different directions.
- Every element listed above appears in each concept.
- The map stays visible and the HUD stays compact.
- No national symbols.
- Each `notes.md` section is complete enough to build from.

After the user picks a direction, a later task will produce the order icons and any textures.
