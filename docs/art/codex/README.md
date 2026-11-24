# 给 Codex 的出图需求

每个需求是一个单独的 md 文件，**一个动画开一个 Codex 对话**。

## 怎么用

1. 开一个新的 Codex 对话，让它能读到这个项目文件夹。
2. 把下面的固定提示词贴进去，只需要把**任务文件名**换掉。
3. Codex 做完后告诉我。我检查出图，合格就进入下一个；不合格我给你一句返工提示词，在同一个对话里让它改。

**固定提示词**（换掉最后的任务文件路径即可）：

```text
Read docs/art/codex/00-style-guide.md first, then complete the task described in the file below. Only create files inside ArtSource/. Task file: docs/art/codex/de-infantry/01-stand-reference.md
```

## 文件结构

| 文件 | 内容 |
|---|---|
| `00-style-guide.md` | 所有任务共用的规则：正俯视、比例（每米 128 像素，钢盔宽 34 像素）、透明背景、无阴影、禁用标志、后期处理、自检清单 |
| `de-infantry/` | 德军步枪兵：`00-character.md` 外观设定，`01–12` 每个动画一个任务 |
| `su-infantry/` | 苏军步枪兵：同样的结构和编号 |
| `vehicles/00-vehicle-guide.md` | 载具和火炮的比例与中心点规则（替代样式规范第 2 节） |
| `vehicles/01–04` | StuG III F、三号 J、T-34/76、T-60：车体和炮塔分开画 |
| `guns/01–04` | PaK 38、45 毫米炮、8 厘米和 82 毫米迫击炮 |
| `terrain/00-terrain-guide.md` | 地面、屋顶贴图和地面贴片的规则（替代样式规范第 1、2、5 节）：每米 64 像素、无缝拼接、透明边缘 |
| `terrain/01–03` | T1 地面贴图 6 种、T2 屋顶贴图 4 种、T3 弹坑 / 瓦砾堆 / 焦痕贴片 |
| `ui/01-unit-icons.md` | U1 北约风格的手绘墨水单位符号 7 个（名单卡片和以后的战略地图用，D42） |
| `ui/02-briefing-background.md` | U2 简报背景插画（火车站一带，斜俯视，无任何标志） |

## 顺序和依赖

- **德军步兵**：01 参考图 ✅、02 行走 ✅；03–12 可以同时开，只有 06 匍匐要等 05 卧倒。
- **苏军步兵**：**先做 01 参考图**（4 张候选），我们确认后再做 02–12（同样 06 等 05）。
- **载具和火炮**：8 个任务彼此独立，随时可以同时开。
- **载具第二版**：四辆车按 `00-vehicle-guide.md` 第 3 节新加的「明暗和做旧」要求重画，StuG 还要按 `01-stug3f.md` 改外形。四个对话可以同时开。
- **第二批（2026-11-23）**：T1、T2、U1、U2 彼此独立，可以同时开；T3 最好等 T1 做完（预览图要铺在 T1 的地面上）。

## 出图位置

| 批次 | 文件夹 |
|---|---|
| 德军步兵 | `ArtSource/codex/de_infantry/` |
| 苏军步兵 | `ArtSource/codex/su_infantry/` |
| 载具 | `ArtSource/codex/vehicles/` |
| 地面、屋顶、贴片 | `ArtSource/codex/terrain/` |
| 单位符号、简报背景 | `ArtSource/codex/ui/` |
| 火炮 | `ArtSource/codex/guns/` |

这些目录都在 Unity 的 `Assets/` 之外，草稿不会被直接导入游戏。我检查合格后，再处理成游戏用的格式，挪进项目。

## 进游戏

- 合格的成品图（不带 `_raw` 的那张）复制到 `Assets/CloseFight/Resources/Art/` 下对应的文件夹：载具放 `Vehicles/`，火炮放 `Guns/`。
- 导入设置是自动的（`Editor/ArtImportSettings.cs`）：每米 128 像素、中心点在画布中心、开 mipmap。
- 游戏按装备代号找图：车体 `veh_<代号>_hull`，炮塔 `veh_<代号>_turret`，火炮 `gun_<代号>`。没有图的装备继续画占位方块。
- 炮塔转轴在车体上的位置写在 `Game/View/EquipmentArt.cs` 里，数字来自 Codex 的 notes.md。重画车体后如果转轴位置变了，要同步改这里。
- 返工的图直接覆盖同名文件即可。
- 2026-11-23：四辆车和四门炮的第一版已经进游戏。
- 2026-11-24（评审见 [review-2026-11-24.md](review-2026-11-24.md)）：
  - 已进游戏：四辆车第二版。
  - 地面贴图放 `Resources/Art/Terrain/`，由着色器 `Resources/Shaders/TerrainTiles.shader` 按地形混合。
  - 屋顶贴图放同一个文件夹，按建筑材质选用，纹理顺着建筑长边。
  - 单位符号显示在名单卡片上，简报背景换成了新插画。这两类放 `Resources/Art/UI/`，背景存成 JPG 以减小体积。
  - 步兵等苏军那套齐了再一起接。
- 地面、屋顶、界面图片的导入规则也在 `ArtImportSettings.cs` 里：地面和屋顶贴图设为可平铺，界面图片作为普通贴图。返工后直接覆盖同名文件。
