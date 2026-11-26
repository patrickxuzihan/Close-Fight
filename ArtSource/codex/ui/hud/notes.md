# U3 — Battle HUD visual directions

Date: **2026-11-25, Australia/Melbourne**. Scope: concepts and component specifications only.

## Deliverables and how to read them

| Direction | Battle screen, 1600 × 1000 | Measured component sheet, 2000 × 1300 |
| --- | --- | --- |
| A — Field Slate | `concept_a.png` | `components_a.png` |
| B — Field Ledger | `concept_b.png` | `components_b.png` |
| C — Night Signal | `concept_c.png` | `components_c.png` |

The battle screens explore the finish on a busy ruined-city map. The component sheets are rendered at **exactly 2 image pixels per logical pixel**; their dimension labels are **1× logical dimensions**. For example, a 72 × 26 button occupies 144 × 52 image pixels, a 76 × 54 portrait occupies 152 × 108, and a 4 px meter is 8 image pixels tall. Sheet headings and annotations are presentation furniture, not part of the battle HUD.

**Implementation authority:** use the dimensions and colour tokens here and the measured component sheets. Generated battle screens and generated raw component boards are visual references, not atlases or pixel-accurate layout specifications. Generated lettering, tiny head counts, portrait proportions and some stroke treatments remain approximate. The final component sheets use actual font glyphs, ten discrete head icons, exact flat fills and measured geometry. A later task should supply final production icons, portraits and any chosen texture; do not cut interactive labels out of the concept screens.

Built-in image generation produced the three battle directions and exploratory component boards, followed by targeted image edits for retreat controls and symbol removal. `concept_*_raw.png` are unmodified selected generations. `components_*_raw.png` retain the generated style boards. `raw_iterations/` retains the initial, superseded battle generations, including rejected details; it is an archive, not approved art. `prompts.json` contains every generation and revision prompt. `render_components.swift` and `manifest.json` reproduce the measured boards from the retained art and installed font.

## Shared layout and behaviour

All coordinates and sizes below are **1× at 1600 × 1000**. At Retina, multiply positions, dimensions, font sizes and strokes by 2. Keep the same logical layout. Rasterize text and icons at the active scale rather than enlarging a 1× screenshot. Snap borders and meter edges to device pixels.

| Element | Position / dimensions | Layout rule |
| --- | --- | --- |
| Top strip | x 0, y 0, w 1600, h 36 | Full width; contents vertically centred; 12 px outer horizontal inset |
| Platoon column | x 12, y 48, w 224 | Five visible rows; row 54 high; gap 4 |
| Open platoon's squad column | x 244, y 48, w 224 | Four visible rows; same row geometry; 8 px between columns |
| Roster row | 224 × 54 | Portrait 76 × 54; right region 148 × 54 |
| Name / state bar | 148 × 24 | Name at x +6, 12 px; clip with ellipsis before adjacent edge |
| Head / status band | 148 × 30 | Dark in all three directions; one 6 × 8 head per man, 2 px gaps |
| Stance tag | 28 × 16 | On portrait's top-left corner; two Chinese characters at 11 px |
| Selected-squad panel | x 1252, y 48, w 336, h 620 | Only one selected squad; 10 visible personnel rows; no scroll at this size |
| Panel header block | h 96 | Title, casualties/morale/state, delegation and two action buttons |
| Personnel row | h 50, horizontal inset 10 | Rank/name and mental state; weapon/ammo; separate red and blue meters |
| Context menu | example x 760, y 300, w 256, h 434 | Open at click; clamp to map viewport; height grows only if localization requires it |
| Menu header | h 32 | 13 px bold; 10 px horizontal inset |
| Menu order row | h 24 | 16 × 16 icon at x +10; label at x +34; right-aligned hotkey, right inset 10 |
| Menu separator | 9 high | 1 px line centred vertically; 10 px inset each end |
| Menu bottom inset | 9 | 32 + 16 × 24 + 9 + 9 = 434 total height |
| Toast | x 500, y 52, w 690, h 32 | 10 px text inset; 2 px leading accent rule |
| Cursor hint | 224 × 26 | Cursor offset +12,+16; clamp to viewport; 8 px horizontal inset |
| Standard top-strip button | h 26 | 72 px base width; 6 px gaps; 12 px text |
| Small delegation button | 44 × 20 | Two buttons, 6 px gap; 11 px label |
| Roster retreat button | 38 × 18 | Chinese 准撤 / 驳回; 4 px gap; 11 px label |
| Thin meters | h 4 | Width 86 in personnel row; 176 in standalone specimen; separate tracks |

Selected roster row: amber name bar `#C6A057`, dark ink `#161C18`, thin selection outline. Within its lower band, put the ten heads in the first 8 px line; put the status and approval/refusal controls on the line below. A withdrawal request is a decision state, not the yellow pinned state. Other squads retain their semantic state colours. Only 1. Gruppe has the selection outline. State words and meters remain visible so colour is never the only cue.

Top-strip horizontal reservations: timer x12/w228; score x252/w274; morale x538/w344; right controls start x1008. Widths: support 92, flares 88, cease-fire 124, language 86, pause 96; 6 px gaps. The 126 px flexible gap between morale and controls absorbs localization. Reserve the same cells in Chinese and English. Full English labels may use `Cease-fire`, `Flares 3`, `Support 12`, `Paused`; never shrink below 11 px.

In English, portrait tags may use `FLEX`, `DELAY`, `HOLD` in a 44 px wide tag at the same anchor. Roster retreat buttons use `OK` / `Deny`, with full action text in a tooltip. Full panel buttons use `Approve` / `Deny` in 58 × 20 cells, using the existing right-side reservation. Long personal names ellipsize before the mental-state column; weapon lines ellipsize before the meter column. Never change row height or hide the ten-man list when switching language.

Menu contents, in order: 移动 M; 快速移动 R; 匍匐 C; 开火 F; 区域压制 E; 烟雾 K; 防御 B; 伏击 N; 隐蔽 H; 突入建筑 G; 后撤 T; 倒车 X; separator; 托管：弹性防御 Z; 托管：迟滞; 托管：死守; 取消托管. Show 快速移动 hovered and 倒车 disabled. The list has **16 rows**: twelve direct orders and four delegation entries. Use a neutral three-node delegation icon, never a flag.

## Font — one family for all directions

Use **Noto Sans CJK SC**, the Simplified Chinese CJK distribution of Noto Sans (the brief's Noto Sans SC direction). It includes Chinese and Latin characters. Use actual **Regular 400, Medium 500, Bold 700** font files; no synthetic weight, stencil fallback or second heading family. The installed OTF files used for the measured sheets are `NotoSansCJKsc-Regular.otf`, `NotoSansCJKsc-Medium.otf`, and `NotoSansCJKsc-Bold.otf`.

The font is free/open under [SIL Open Font License 1.1, official Noto CJK licence](https://github.com/notofonts/noto-cjk/blob/main/Sans/LICENSE). Include its copyright and licence with distributed font files. Fonts have not been copied into the game or this art package.

| UI role | Size at 1× | Weight | Line box |
| --- | --- | --- | --- |
| Selected-squad title | 14 px | 700 | 20 px |
| Context-menu header | 13 px | 700 | 20 px |
| Top-strip status, buttons | 12 px | 500 | 18 px |
| Roster name | 12 px | 500; selected name 700 | 18 px |
| Command names | 12 px | 400 | 18 px |
| Hotkeys | 12 px | 500 | 18 px; right aligned |
| Soldier rank/name | 12 px | 500 | 18 px |
| Weapons, ammunition, state | 11 px | 400 | 16 px |
| Head-band status / tiny buttons / tags | 11 px | 400 / 500 / 500 | 16 px |
| Toast | 12 px | 500 | 18 px |
| Cursor hint | 12 px | 400 | 18 px |

## A — Field Slate

Clean, contemporary military interface: opaque olive-slate surfaces, quiet warm selection, crisp boundaries. This is the most neutral starting point for a general day/night implementation.

| Token | Hex | Use |
| --- | --- | --- |
| Panel | `#18221F` | Primary fill, 96% alpha in battle |
| Secondary | `#26332D` | Buttons, meter tracks, secondary strips |
| Border | `#52645A` | Normal 1 px edges and dividers |
| Text | `#E3E8DA` | Primary text |
| Secondary text | `#AFBCAF` | Weapon/ammo, supporting data |
| Disabled | `#6E7B70` | Disabled text and icons |
| Selection | `#C9B478` | Selected outline, pressed edge, toast rule |
| Hover | `#35473C` | Full hovered row / button fill |
| Pressed / dark head band | `#111A16` | Pressed controls and personnel band |
| Good | `#49694B` | Green name bar |
| Pinned | `#B9A75F` | Yellow name bar |
| Shaken | `#B17A43` | Orange name bar |
| Broken | `#96554E` | Red name bar |
| Gone | `#646963` | Grey name bar |
| Suppression | `#C87569` | Red meter |
| Morale | `#7EACBF` | Blue meter |
| Force morale | `#7BA76C` | Top-strip morale fill |

Typography: Noto Sans CJK SC 400/500/700, 11/12/13/14 px according to the shared table. Spacing: 4 px base unit, 8 px group gaps, 10 px panel content inset, 12 px viewport margin. Rows: roster 54, menu 24, personnel 50; buttons 26. Borders 1 px; pressed border 2 px; radius **2 px**, while state bars and meters stay square. Use no UI texture. Flat fills, code-drawn outlines and small white masks tinted to text colour are sufficient. No 9-slice required.

## B — Field Ledger

Warm light paper and charcoal ink, recalling a carefully typeset field record. Bold sans-serif headers and ruled sections supply the period character without stencils or a second typeface. Roster head bands stay dark for continuity with the existing roster structure.

| Token | Hex | Use |
| --- | --- | --- |
| Paper panel | `#D1C7AE` | Primary fill, 98% alpha in battle |
| Secondary | `#BDB49C` | Secondary strips, normal controls, meter tracks |
| Rule | `#817B67` | 1 px borders and separators |
| Ink | `#262C26` | Primary text and icons |
| Secondary ink | `#4D564A` | Supporting data |
| Disabled ink | `#757466` | Disabled text and icons |
| Selection | `#536047` | Olive selection edge, pressed edge, toast rule |
| Hover | `#E3D8BE` | Lighter button / menu-row fill |
| Pressed | `#ABA185` | Depressed fill |
| Dark head band | `#30372E` | Personnel/status band; pale head glyphs |
| Good | `#647B58` | Green name bar |
| Pinned | `#C5B266` | Yellow name bar |
| Shaken | `#B9814E` | Orange name bar |
| Broken | `#A96558` | Red name bar |
| Gone | `#858980` | Grey name bar |
| Suppression | `#A84F42` | Red meter |
| Morale | `#476F85` | Blue meter |
| Force morale | `#647B58` | Top-strip morale fill |

Typography: Noto Sans CJK SC 400/500/700, 11/12/13/14 px from the shared table; 700 headings create the printed emphasis. Spacing: 4 px base, 8 px group gaps, 10 px panel inset, 12 px viewport margin. Rows: 54/24/50; buttons 26. Borders 1 px, pressed border 2 px, radius **0 px**. No texture is required. Optional later asset: seamless **64 × 64 grayscale paper fibre**, no text or symbols, applied to headers only at **3% alpha maximum**, neutral soft-light equivalent optional but ordinary alpha overlay sufficient. Keep body/text regions flat; never use torn edges, parchment stains or rivets. The measured sheet deliberately demonstrates the texture-free build.

## C — Night Signal

Cool smoked slate panels with a warm brass interaction cue. Transparency is ordinary alpha blending over the scene: no blur, bloom, glass shaders or glossy gradients. The muted semantic state colours and red/blue meters are the only exceptions to the brass interaction accent.

| Token | Hex | Use |
| --- | --- | --- |
| Panel | `#111A20` | Primary fill, 90% alpha |
| Secondary | `#1D2A32` | Secondary fill / controls, 94% alpha |
| Border | `#4E6066` | Normal 1 px edges and rules |
| Text | `#E4E7DF` | Primary text |
| Secondary text | `#ADB9BE` | Supporting data |
| Disabled | `#708087` | Disabled text and icons |
| Brass accent | `#D2AB68` | Selection, pressed border, toast rule |
| Hover | `#344044` | Hovered controls, opaque for legibility |
| Pressed / dark head band | `#0C1419` | Pressed fill and status band |
| Good | `#526D5B` | Green name bar |
| Pinned | `#AF9C55` | Yellow name bar |
| Shaken | `#A77746` | Orange name bar |
| Broken | `#965A57` | Red name bar |
| Gone | `#626C70` | Grey name bar |
| Suppression | `#CB776F` | Red meter |
| Morale | `#7CA6BD` | Blue meter |
| Force morale | `#6F9E78` | Top-strip morale fill |

Typography: Noto Sans CJK SC 400/500/700, 11/12/13/14 px from the shared table. Spacing: 4 px base, 8 px group gaps, 10 px panel inset, 12 px viewport margin. Rows: 54/24/50; buttons 26. Borders 1 px, pressed border 2 px, selection rail 2 px, radius **3 px**. State bars and meters remain square. No textures or 9-slices required. Make menu and tiny head bands at least 96% opaque when they cover bright flare-lit ground. The component board shows uncomposited RGB tokens; apply these alpha values in battle, keeping text and icons 100% opaque.

## Shared colour and asset rules

- Roster request amber is `#C6A057`, distinct from the pinned yellow; its text is `#161C18`.
- Use `#F0F1E8` on green, red and grey state bars; `#161C18` on yellow and orange. Head glyphs are `#E4E7DF` on all dark bands.
- Selection is a thin outline or leading rule, not a large luminous fill. Hover fills the existing row without changing its bounds. Pressed fill darkens and receives a 2 px edge. Disabled state dims both text and icon and does not respond to hover.
- Icon production target: 16 × 16 at 1×, 32 × 32 at 2×, white alpha mask tinted in code. Head silhouette: 6 × 8 at 1×, 12 × 16 at 2×. These sheets contain direction specimens, not the later full order-icon set.
- All final assets avoid flags, stars, eagles, iron crosses, swastikas and national/political decoration. Round objectives and neutral abstract order symbols replace faction emblems. Tiny portrait details remain concept art, not approved uniform insignia.
- Panel/button skins can be drawn directly. If the engine requires a 9-slice, bake a flat 16 × 16 plate at 1× with 4 px fixed corners; export 32 × 32 at 2× with 8 px corners. Keep all text and state indicators outside the texture.

## Review / verification

- Three clearly different finishes: olive flat, warm light ledger, blue-black night overlay.
- Each final battle screen visibly includes top strip, platoon/squad roster, open order menu, one selected squad panel with ten personnel rows, retreat toast and cursor hint.
- The selected row has an amber request bar and approval/refusal controls. Five semantic state colours appear in each measured sheet. The open map remains dominant, with no bottom HUD frame.
- Every measured component sheet includes four button states, normal/hover/disabled menu rows, header, separator, panel, all five name bars, ten heads, red/blue meters, retreat toast and three corner tags.
- Final battle PNG dimensions checked at 1600 × 1000; component boards checked at 2000 × 1300 and generated from 1000 × 650 logical coordinates at 2×.
- Final images visually reviewed for clipping, missing controls and forbidden motifs. The initial paper draft's generic flag icons were removed from the selected final; the initial remains only in the raw archive.
- Generated screens have illustrative text/head-count details, as described above. Exact component geometry and glyphs are provided in the measured boards; real-time layout, localization and day/night contrast still need an engine check when a direction is implemented.
- No game source, scene, Assets content or documentation was modified. No game build or scene test was run: U3 is an art-only design task, and this checkout has no `docs/ui_style_guide.md` or Godot build target matching the generic skill. The U3 brief and its supplied screenshots were the local authority.

To rerender measured sheets on macOS with the named Noto fonts installed, run `swift render_components.swift .` from this folder. All source art paths in `manifest.json` are local to this package.
