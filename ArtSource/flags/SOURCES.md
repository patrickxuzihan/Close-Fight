# Objective flag sources

Both images are public domain (Wikimedia Commons), downloaded 2026-11-23 as 1280 px renders of the SVGs into `source/`:

| File | Commons page | License | Used as |
|---|---|---|---|
| `source/Flag_of_the_Soviet_Union_1280.png` | https://commons.wikimedia.org/wiki/File:Flag_of_the_Soviet_Union.svg | Public domain | Soviet objective flag |
| `source/Balkenkreuz_1280.png` | https://commons.wikimedia.org/wiki/File:Balkenkreuz.svg | Public domain | German objective flag: the Wehrmacht identification cross on a field-grey cloth |

No Nazi symbols are used (the German flag of the period carried a swastika, so the game uses the military identification cross instead).

Commons notes that communist symbols are restricted in some countries; worth keeping in mind for distribution.

`tools/flags/make_flags.py` builds `Assets/CloseFight/Resources/Art/Flags/flags_atlas.png` (one flat, still flag per side) and `flags_preview.png` from these.
