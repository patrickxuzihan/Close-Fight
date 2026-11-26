# Task U3b: direction A, compact and less generic (revision of U3)

The user chose **direction A (Field Slate)** from U3. They gave two notes on it:

1. **It takes too much of the map.** The roster, the squad panel and the menu are big, with a lot of empty padding. The same information must fit in roughly **half the area**.
2. **It feels "AI-made".** It reads like a web admin dashboard or a UI kit, not like a war game.

Concretely, these are the causes:

- every element sits in its own bordered, rounded box;
- the padding is generous and even everywhere, so nothing stands out;
- labels are repeated: "压制 18%" and "士气 82%" with bars *and* percentages on every soldier's row, and normal states like "镇定" or "良好" are spelled out;
- the palette is a generic dark-plus-accent one;
- everything is perfectly symmetrical and uniform.

Real tactics-game HUDs, the recent Close Combat titles for instance, look different:

- they are dense and tight;
- they rely on colour, position and small icons;
- they show words only when something is wrong.

**Do not generate any images in this task.**

- Draw the HUD with code, with the real font, the way you made `render_components.swift`.
- Draw it over our real game screenshot: `ArtSource/codex/ui/hud/game_backdrop_day.png`, 1600 × 1000. It is a real frame of the game with the HUD switched off. The flags on the objectives are the game's own markers: leave them as they are.
- The roster pictures are the game's real ones: `Assets/CloseFight/Resources/Art/Roster/unit_de_rifle.png` (and `unit_su_rifle.png`). Use `unit_de_rifle.png` for every infantry tile. Where a platoon has no picture of its own yet, use a plain dark tile.
- For reference, `ArtSource/codex/ui/hud/game_hud_current.png` is the game's current HUD.

Keep direction A's palette, font (Noto Sans CJK SC 400/500/700, installed on this Mac) and state colours from `notes.md`, except where the rules below change them.

## Rules against the generic look

1. **One flat plate per block, no boxes inside it.**
   - Each block (top bar, roster columns, squad panel, menu, toast) is a single flat plate: `#18221F` at 90 % alpha.
   - Inside a plate there are no bordered sub-boxes and no outline around each row. Separate rows with spacing, or at most a 1 px rule at 35 % alpha.
2. **Square corners** (0 px radius) everywhere.
3. **No redundant words.**
   - No "压制" / "士气" labels and no percentages on soldier rows: the two bars are enough.
   - No "状态：" prefixes.
   - Normal states ("良好", "镇定") are not printed: silence means fine. A state word appears only when something is wrong, in its colour.
4. **One emphasis per block.** One item is bold or coloured (the title, or the thing that needs attention); everything else is the muted secondary colour.
5. **Small icons instead of words where they are unambiguous:**
   - the weapon class on each soldier's row (rifle, submachine gun, machine gun, pistol);
   - pause.
   Orders keep their Chinese names next to their icons. Icons are simple, consistent 14 px line or solid glyphs, drawn by code, white and tinted.
6. **Tight, uneven spacing:** 4 px inner padding, 2 px between rows, tabular figures for numbers (time, ammo, counts).
7. **Period character in one restrained detail at most.** For example, the German unit names in small caps, or a 2 px khaki edge along the top of each plate like a field-notebook binding. No stencils, paper, rivets or emblems. If nothing works, leave it out.

## Size budget (1× logical pixels at 1600 × 1000)

| Element | Before (U3 A) | Now |
|---|---|---|
| Top bar | 36 high, boxed buttons | **22 high**. Items are plain text with thin dividers, no button boxes; hover brightens the text. Right side: 支援 12 · 照明弹 3 · 停火 · EN · pause icon |
| Roster row | 224 × 54 (picture 76 × 54) | **178 × 44**: picture 62 × 44; name bar 116 × 18 (12 px); band 116 × 26 with heads 6 × 8, then a status word (only when not normal) or the two answer buttons |
| Roster | two columns, 456 wide | two columns of 178, 3 px apart: **359 wide** |
| Stance tag | 28 × 16 | 22 × 13, 10 px |
| Squad panel | 336 × 620 | **252 wide, about 230 high** |
| Order menu | 256 × 434 | **168 wide**: header 18 (11 px, muted, e.g. "建筑 · 3 层"); rows 20 (icon 14, name 12 px, hotkey 11 px muted, right); separator 5; disabled rows at 35 % opacity |
| Toast | 690 × 32 | **22 high, as wide as its text** plus 24, below the top bar; amber 2 px rule on the left |
| Cursor hint | 224 × 26 | 20 high, as wide as its text |

The squad panel in detail:

- **Header, about 40 high:**
  - line 1: "1. Gruppe" in bold, then "步兵班" muted;
  - line 2 (11 px): "伤亡 0% · 士气 68%", then the state word only if not normal, then "托管·弹性" and the two small buttons 准撤 / 驳回 on the right.
- **Ten soldier rows, 18 high each.** From left to right:
  1. a 6 px state dot (morale colour);
  2. rank abbreviation and surname (11 px; "下士 Wagner", not the full name);
  3. weapon icon;
  4. ammunition, right-aligned;
  5. two stacked 2 px bars, 44 wide (suppression red, morale blue);
  6. the state word only when not normal (e.g. 被压制, 溃逃).
- Full name and exact numbers can go in a tooltip on hover. Show one hover tooltip in the image.

## Scene to draw

Draw the same scene as U3 so the two can be compared:

- **Roster:** two columns. The left column shows 5 platoons; the right column shows the 4 squads of 1. Zug, with 1. Gruppe asking to pull back (amber name bar, 准撤 / 驳回).
- **Selection:** 1. Gruppe is selected, with its panel top right.
- **Order menu:** open on a building in the middle of the map (快速移动 hovered, 倒车 disabled, the delegation rows below the separator).
- **Toast and hint:** the toast, and the cursor hint "移动 — 左键点目标".

## Deliverables

All go in `ArtSource/codex/ui/hud/`:

- `compact_a.png`: the full 1600 × 1000 screen.
- `compact_a_detail.png`: the roster, the squad panel and the menu at 2×, side by side, to check the small type.
- `compare_a.png`: `concept_a.png` and `compact_a.png` side by side. Under each, give the share of the screen covered by HUD plates (without the open menu); the new one should be about half the old.
- `notes_compact.md`: every size, colour and alpha that changed from `notes.md`, and the icon list with how each icon is drawn. Write it so the programmer can build from it directly.
- The script that renders it, so it can be re-rendered.

## Self-check

- Nothing generated.
- No boxes inside the plates.
- No per-soldier labels or percentages, and no printed normal states.
- The HUD covers about half the area it did in U3 A.
- All information from U3 is still there.
- Type is at least 11 px and readable over the map.
