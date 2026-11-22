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
| `de-infantry/00-character.md` | 德军步枪兵的外观：军服、装备、武器，以及 17 帧的编号表 |
| `de-infantry/01-…12-*.md` | 每个动画一个任务文件 |

## 顺序

1. **必须先做 `01-stand-reference.md`**。它会生成 4 张候选的站姿图，选一张当作这个兵的**参考图**，后面所有动作都照着它画。我们俩一起确认参考图之后，再做下一步。
2. 然后按编号顺序做：02 行走 → 03 奔跑 → 04 跪射 → 05 卧倒 → 06 匍匐 → 07 投弹 → 08 畏缩 → 09 重伤 → 10 阵亡 → 11 机枪手 → 12 迫击炮手。
   - 06 匍匐会参考 05 卧倒，所以这两个按顺序做。其余的确认参考图之后，可以同时开好几个对话。

## 出图位置

所有图片都放在 `ArtSource/codex/de_infantry/`。这个目录在 Unity 的 `Assets/` 之外，草稿不会被直接导入游戏。我检查合格后，再处理成游戏用的格式，挪进项目。

## 以后的批次

苏军步兵、车辆、火炮会按同样的结构，各开一个子文件夹（比如 `su-infantry/`），共用同一份 `00-style-guide.md`。
