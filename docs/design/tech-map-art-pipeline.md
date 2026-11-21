# 技术路线：引擎、地图与美术

> 状态：引擎已定为 **Unity（C#）**（D18），画风已定为 **CC 式写实航拍风**（D20）。
> 第 4 节的"画面实现方式"等第 4 轮 Q20 确认。

---

## 1. 关键认识：交互靠数据，不靠建模

CC 的战斗地图其实是**一张画好的大图，加上几层看不见的数据**：
- 画面层：玩家看到的房子、树、路。
- 数据层：同样大小的网格，每格记着地形类型、高度、能否挡子弹和视线、通行速度。

士兵进房子、视线被挡、坦克过桥，都是在**查数据层**，和画面怎么画无关。

## 2. 地图 = 矢量数据 → 同时生成画面和玩法网格

| 物体 | 形状 | 属性（举例） |
|---|---|---|
| 房子 | 多边形 | 类型（木屋 / 砖楼 / 混凝土楼 / 厂房）、层数（决定视野高度，D5）、耐久、掩护值 |
| 街道、铁路 | 线 | 宽度、路面 |
| 河流、水域 | 多边形 | 不可通行（浅滩可涉水） |
| 桥 | 线 | 承重、耐久（能被炸断） |
| 冲沟、高地 | 高程图 | 高度、坡度 |
| 墙、篱笆、弹坑、瓦砾 | 线或多边形 | 掩护值、是否挡视线 |

程序读这份数据，生成：
1. **画面**：地面、建筑、阴影、瓦砾。
2. **玩法网格**：通行代价、掩护、视线遮挡、高度。

房子被炸塌，就把类型改成"瓦砾"，画面和玩法一起更新。

## 3. 地图来源与做图流程（D19）

**参考资料**：
- 1942 年德军总参谋部的《斯大林格勒城市地图》：1:20,000，依据 1942 年 6 月的航拍绘制，美国国会图书馆有藏本。
- 1942 年 9 月到 1943 年 2 月的德国空军航拍照片。
- 公开的卫星高程数据（马马耶夫岗、冲沟、河岸这些自然地形基本没变）。

**流程（我做工具，你摆关卡）**：

```
历史地图作底图（编辑器里半透明垫在下面）
  └─▶ 描主干：河岸、冲沟、主干道、铁路、地标建筑
        └─▶ 普通街区程序填充：标成"木屋区 / 砖楼区 / 工厂区"，自动铺满房子
              └─▶ 手动微调：胜利点、部署区、关键火力点
                    └─▶ 一键生成，进游戏测试
```

编辑器做成 **Unity 编辑器扩展**：在 Unity 里多出一个"Close Fight 关卡编辑器"窗口，你在里面画就行。

## 4. 画面实现方式（推荐方案，待 Q20 确认）

**推荐：固定俯视视角 + 3D 渲染。**

- **玩家看到的和 CC 一样**：正上方俯视、不能转视角，就是 2D 俯视游戏（D6 不变）。
- **底下用 3D 来画**：Unity 的强项都能用上，而且大部分东西不需要美术手绘。

| 内容 | 做法 | 谁来做 |
|---|---|---|
| 地面 | 高程网格加**照片扫描材质**（草地、泥土、碎石、沥青、瓦砾）。ambientCG、Poly Haven 有大量免费、可商用（CC0）的材质，天然就是"航拍照片感" | 我 |
| 建筑 | 根据地图里的房子多边形**自动拉伸成 3D 楼**，贴上屋顶和墙面材质。太阳光自动投出阴影，楼高一眼看得出来，这正是 CC 地图的质感 | 我（程序生成） |
| 废墟、瓦砾、弹坑 | 程序生成破损的墙体、碎石堆，以及弹坑贴花 | 我（程序生成） |
| 树木、灌木 | 免费素材，或者简单模型 | 我来找、来接入 |
| **士兵** | **2D 帧动画**（D27）：Codex 画正俯视的动作帧（例如匍匐时左腿、右腿两帧交替），我再加上轻微摆动。正俯视下一张图旋转就能朝向任何方向，**不需要画 8 个方向**。武器不单独建模（D26） | Codex 画帧 → 我切图、对齐、做动画 |
| **坦克、火炮** | 车体和炮塔**分开**画，炮塔要能单独转。先让 Codex 直接画俯视图；效果不够再用 Meshy 建 3D，由我在 Unity 里渲染成俯视图 | Codex，必要时加 Meshy |

**为什么不用纯 2D 贴图**：纯 2D 需要把每个士兵的每个动作、8 个朝向都画成图，没有美术的话最难做。CC 当年其实也是先建 3D 模型，再渲染成贴图的。

**"建模"怎么分工（回答 Q15）**：

```
Codex：按我写的《美术需求清单》生成多视角参考图（前、侧、后、俯视，统一风格）
  └─▶ 你（或 Codex）：把参考图喂给 AI 建模工具（Meshy / Tripo 等），导出 .glb 模型
        └─▶ 我：导入 Unity，调比例、材质、骨骼和动作，接进游戏，截图检查效果
```

- 我**不能直接生成图片，也不能从图片生成 3D 模型**。但这些模型进了 Unity 之后的所有工作都由我负责。
- 建筑、地面、废墟这些量最大的部分，**全部程序生成，不需要建模**。
- **更新（v2）**：单位改为 Codex 画的 2D 图，Meshy 退居备选，只在 Codex 的车辆图不够好时使用。明细见 [art-asset-list.md](art-asset-list.md)。

**AI 建模工具的授权（Q22）**：
- **Meshy**：免费版只能个人或评估使用，商用要付费；带自动绑骨骼和预设动作。
- **Tripo**：速度快，有评测称免费额度的产出按 CC BY 4.0 授权（需要署名，以官网条款为准）。
- **混元 3D（腾讯）**：可以本地部署，但许可证限制较多（部分版本禁止商用，还有地区限制），不推荐。
- 作品集阶段用免费额度通常够用；如果准备商业发布，要么付费，要么重新生成。

## 5. 我怎么接进 Unity

1. **直接写代码**：C# 游戏逻辑、编辑器扩展、自动化脚本，都是文本文件，我直接写。
2. **命令行**：在后台运行 Unity，执行测试、自动生成场景和资源、打包。
3. **MCP 连接编辑器**：Unity 官方在 2026 年推出了面向 AI 助手的 MCP 接入（Beta，Unity 6），社区也有成熟的开源方案（如 CoplayDev 的 MCP for Unity）。接上以后，我可以直接操作你开着的 Unity 编辑器：建物体、改场景、读报错、进入游戏截图，**自己看效果**。
   - 注意：Unity 的纯后台模式不渲染画面，所以截图要通过开着的编辑器来做。
4. **更新（2026-11-22）：Unity 官方命令行工具 `unity`**。装 Hub 时已经自动装好了（`~/.unity/bin/unity`，版本 1.0.0）。它能做到的事情，基本覆盖了上面 2 和 3：
   - 装编辑器和模块、创建项目、打包、跑测试。
   - **操作开着的编辑器**：执行编辑器命令、重新编译并汇报编译错误、查看编辑器状态。
   - **运行游戏并截图**。
   - 自带 MCP 配置（`unity mcp`）和给 AI 助手用的说明（`unity skill`）。
   - **结论：优先用它，社区 MCP 插件只作为备选。**

**性能**：团级规模用 Unity 的 Jobs 和 Burst（多线程、高性能编译）来跑视线和寻路计算。视线按小组算，不按每个士兵两两算；寻路用流场。

## 6. 需要安装的东西

| 东西 | 说明 | 谁来做 |
|---|---|---|
| Unity Hub | Unity 的启动器 | 我可以用 Homebrew 帮你装 |
| Unity 账号登录 | 个人版免费 | **你**（账号登录我不能代劳） |
| Unity 6 最新 LTS 编辑器 | 加装 Mac、Windows、WebGL 打包模块，约 10–15 GB | 你登录后，我通过 Hub 安装，或者你在 Hub 里点一下 |
| MCP 插件 | 让我连上编辑器 | 我来配置 |
| Git LFS | 模型和贴图这类大文件需要它来版本管理 | 我来配置 |

## 7. 费用与学生优惠（2026 年 11 月调研，以各官网为准）

| 工具 | 普通价格 | 学生优惠 | 免费版限制 |
|---|---|---|---|
| **Unity** | 个人版免费 | **Unity Student 计划免费**：Pro 版编辑器，加 Odin 插件教育授权、Synty 素材包，还附带 GitHub 学生开发包。需要学生身份验证，每年续一次 | 个人版功能足够做这个游戏 |
| **Meshy** | Pro 每月 20 美元（1000 点数），按年付约每月 16 美元 | **教育计划：每月 5.99 美元，共 13 个月**（有来源说首月免费），需要用学校邮箱申请，只适用于 Pro | 免费版只能个人或评估使用，不能商用 |
| **Tripo** | Pro 每月 20 美元（3000 点数，约 200 个模型），可商用；按年付更便宜（各来源说法不一，约每月 12–13 美元） | 没查到学生优惠 | 免费版生成的模型是公开的，不能商用 |

**建议的用法**：
1. 先用 Meshy 和 Tripo 的免费额度，各试做一辆 T-34 和一个步兵，看哪家的二战题材效果更好。
2. 选定后集中订阅，把雏形需要的约 20–25 个模型生成完。
3. 如果 Meshy 的学生价申请下来，优先用 Meshy，最便宜。
4. 订阅期间生成的模型，退订之后能不能继续商用，要在订阅前看清条款。

**再次更新（v2）**：士兵改用 2D 帧动画，武器不建模，Meshy 只作为车辆的备选方案。**先不开会员**，等 Codex 第一批图出来再决定。下面的估算是按原来全部 3D 的方案算的，仅供参考。

**更新（2026-11-21）**：Meshy 免费版不支持多视图转 3D。你决定**等真正需要建模时，直接开 Meshy Pro**。

**额度估算**（以 Meshy 官方文档为准）：
- 单图或多图生成一个带贴图的模型，约 30 点（模型 20 点，加贴图 10 点）。
- 网页版绑骨骼免费，基础动作免费。
- Pro 每月 1000 点，约 33 次生成。**点数按月清零，不能累积。**
- AI 生成一般要试 3–5 次才能得到满意的结果；坦克的车体和炮塔要分开生成。
- 雏形约 20–25 个模型（含武器等小件），算下来约 1800–2250 点，也就是 **2 个月左右的 Pro 额度**。
- 一个季度约 3000 点，够做完雏形。
- 首个完整版本约 60–75 个模型，约 6 个月的额度，学生价更划算。明细见 [art-asset-list.md](art-asset-list.md)。

**注意**：
- 等《美术需求清单》和 Codex 的参考图都准备好了再开通，避免点数在等待中清零。
- 开通前先看一下学生价（每月 5.99 美元），可能比按季度付 Pro 更划算。

## 8. Unity 安装指引（Apple Silicon Mac）

1. 打开 **https://unity.com/download**，下载 **Unity Hub（Mac 版）**，把它拖进"应用程序"文件夹。
2. 打开 Unity Hub，用**个人邮箱**注册或登录 Unity 账号。毕业后学校邮箱会失效，所以账号邮箱用长期的；学校邮箱只在验证学生身份时使用。
3. 许可证先选 **Personal（个人版，免费）**，马上就能用。
4. （可选）去 https://unity.com/products/unity-student 申请学生计划。学生身份由第三方 **SheerID** 审核，最多需要 5 个工作日。
   - 学校列表里搜不到，就走 SheerID 的"学校不在列表"流程，或者发邮件给 customerservice@sheerid.com 请他们添加，可能要等几周。
   - **学生计划只是锦上添花**，个人版完全够做这个项目，审核不顺利可以不管。
   - **实测结果（2026-11-21）**：你的学校不在 Unity 学生计划的合格名单上（资格由 Unity 决定）。**结论：用个人版。**
5. 左侧选 **Installs → Install Editor**，选标着 **LTS** 的最新版（目前是 **Unity 6.3 LTS，6000.3.x**；如果 Hub 里已经有更新的 LTS，就选更新的）。**实际安装的是 6000.6.10f1（Unity 6.6）**，6.7 LTS 正式发布后再升级。
6. **模块只勾这些，省空间**：
   - ✅ Mac Build Support（默认就有，保持勾选）
   - ✅ 简体中文语言包（可选，编辑器界面变成中文）
   - ❌ Android、iOS、visionOS、WebGL、Windows Build Support、Documentation 都先不勾，等打包时再加
7. 装好后告诉我，**项目由我用命令行创建**（渲染管线、目录结构、Git 配置都一次设好），你在 Hub 里打开就行。

空间占用：编辑器加默认模块大约 6–10 GB，项目缓存再占几 GB。

---

## 参考来源

- [David Rumsey：1942 年德军《斯大林格勒城市地图》](https://davidrumsey.com/luna/servlet/detail/RUMSEY~8~1~333227~90101389)
- [Wikimedia：1942 German map of Stalingrad（指向美国国会图书馆藏本）](https://download.osmand.net/wiki/File:1942_German_map_of_Stalingrad.jpg)
- [WW2 Aerial Recon Studies：斯大林格勒航拍](https://www.ww2aerialreconstudies.com/eastern-front/stalingrad%2C-september-1942---february-1943)
- [Unity 博客：Unity AI 与 MCP 入门](https://unity.com/blog/unity-ai-mcp-how-to-get-started)
- [Unity 文档：Unity MCP 概览](https://docs.unity3d.com/Packages/com.unity.ai.assistant@2.0/manual/unity-mcp-overview.html)
- [Unity MCP 指南（提到官方 MCP 可能迁移到 Unity CLI，待核实）](https://mcpplaygroundonline.com/blog/unity-mcp-server-guide.md)
- [Unity-MCP 截图方案示例](https://raw.githubusercontent.com/IvanMurzak/Unity-MCP/main/Unity-MCP-Plugin/.claude/skills/screenshot-camera/SKILL.md)
- [Unity 问题追踪：后台模式不渲染](https://issuetracker.unity3d.com/issues/batchmode-if-project-is-run-in-batchmode-unity-doesnt-render-to-texture2d-dot-readpixels)
- [Meshy 工具对比页](https://www.meshy.ai/zh/compare)
- [WaveSpeed：AI 3D 游戏资产生成器对比](https://wavespeed.ai/blog/3d-model-access/best-ai-tools-for-3d-game-assets/)
- [Sorceress：免费 AI 3D 生成器评测](https://sorceress.games/blog/best-free-ai-3d-model-generator-honest-2026-picks)
- [Unity Student 计划](https://unity.com/products/unity-student) · [Unity Student 与 GitHub 学生包](https://id.unity.com/en/notGithubStudent) · [密歇根大学：Unity Student 说明](https://teamdynamix.umich.edu/TDClient/76/Portal/KB/Article/5457/Unity-Student)
- [Unity 6 发布页](https://unity.com/releases/unity-6) · [Unity 6.3 LTS 发布公告](https://unity.com/blog/unity-6-3-lts-is-now-available) · [Unity 下载存档](https://unity.com/releases/editor/archive)
- [Meshy 价格（Render a House）](https://www.renderahouse.com/blog/meshy-pricing) · [Meshy 教育计划](https://aistudentdiscount.org/meshy-student-discount/) · [Meshy 学生优惠](https://aistudentdiscount.com/meshy-ai-student-education-plan/)
- [Tripo 官方价格页](https://www.tripo3d.ai/pricing) · [Voyager：Tripo 评测](https://voyager.so/blog/tripo-ai-review) · [AffMaven：Tripo 价格](https://affmaven.com/tripo-pricing/)
