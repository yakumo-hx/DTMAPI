# 20260907-0006：正式版 public 1.00.07 全量逆向捕获与公告核对

## Metadata

- Update ID: `20260907-0006`
- Date: `2026-09-07`
- Lifecycle Status: `verified`
- Validation Level: `docs, source`
- Runtime Validation: `not-required`
- Related Issue State: `open`
- Issue note: 官方静态修复已找到，玩家运行验收仍开放。
- Source: 用户要求捕获最新游戏内容，并核对官方公告中的 14 项 BUG 修复。
- Relevant Reviews:
  - `docs/reviews/manual-qa/2026/20260827-0001-official-hope-seed-wake-save-race.md`
  - `docs/reviews/manual-qa/2026/20260823-0003-evernight-newyear-warm-reload-partial-init.md`

## Scope

- 冻结当前 Steam `public` payload，执行 AssetRipper 全量 Unity 工程恢复和 ILSpy 主程序集/firstpass 反编译。
- 独立复核 raw snapshot、AssetRipper export 与两棵反编译树的完整清单和 SHA-256。
- 以最近的 public reverse 观察头 `24966367_public_958EAF / 1.00.06` 为直接对照，分离 Unity 重打包噪声与真实代码、配置、剧情和资源变化。
- 逐项映射用户提供的 14 项官方修复，明确静态证据能力边界与 DTMAPI 影响。
- 官方字节、恢复资源和反编译源码只写入 Git 忽略的本地 reverse 目录，不提交、不打包、不发布。

## Start Identity

- Steam app: `2285550`
- Steam build: `25163613`
- Branch: `public`；`UserConfig` 与 `MountedConfig` 均为 `public`，无 pending branch switch。
- Source manifest SHA-256: `28FA7046432DD91F97C9897ADA10184B95853E1F3024EBEBFA37B2E81177D0B8`
- PlayerSettings/Application version slot: `1.00.07`
- `Assembly-CSharp.dll`: 6,401,024 bytes；SHA-256 `60489873C645886C5A523FD0D17C4D451A7D68DF133F552C6667501245110AC6`
- 身份探测时没有运行中的 `DolocTown.exe`，无需主动关闭游戏。

## Changed Files

- `docs/updates/2026/20260907-0006-public-10007-full-reverse-capture.md`
- `docs/updates/INDEX-2026-09.md`
- `docs/reviews/manual-qa/2026/20260827-0001-official-hope-seed-wake-save-race.md`
- `docs/reviews/manual-qa/2026/20260823-0003-evernight-newyear-warm-reload-partial-init.md`
- ignored local capture under `references/doloc-town/reverse/builds/25163613_public_604898/`

## Capture Result

| Layer | Result |
| --- | --- |
| Frozen raw snapshot | 542 files / 1,881,784,447 bytes；542 个 payload 文件与冻结源逐文件长度/SHA-256 exact，含 manifest 的 543 项 source parity exact |
| AssetRipper 1.3.14 | 52,089 files / 4,456,223,241 bytes；90 scenes、195 config tables、8 bundles、181 managed assemblies |
| ILSpy 9.1.0.7988 main | 3,671 files / 9,507,853 bytes |
| ILSpy firstpass | 29 files / 137,684 bytes |
| Known AssetRipper limitation | 93 errors；仍为 90 个逐 scene Cubemap size mismatch 与 3 个 custom-version `globalgamemanagers` setting-object read error，没有新增错误类别 |

全量捕获退出码为 `0`。source-start、frozen、source-end 的 build、branch、manifest 和主程序集身份一致；游戏安装内容在任务期间只读，捕获结束后共享 Runtime lock 已正常释放。

## Difference From 1.00.06

### Packaging and stable boundaries

| Layer | Added | Removed | Changed | Same | Size delta |
| --- | ---: | ---: | ---: | ---: | ---: |
| Raw snapshot | 4 | 4 | 16 | 522 | +2,175 bytes |
| AssetRipper export | 10 | 10 | 45,882 | 6,197 | +30,180 bytes |
| ILSpy main | 1 | 1 | 12 | 3,658 | +2,580 bytes |
| ILSpy firstpass | 0 | 0 | 0 | 29 | 0 |

- Raw 的 4 added / 4 removed 全部是内容哈希进入文件名后的 bundle 替换；文件总数仍为 542。181 个 managed assemblies 中只有 `Assembly-CSharp.dll` 变化，另外 180 个逐字节相同。
- 90 个 built scene 的原始路径、build index 和导出映射完全一致，没有新增或删除地图/室内场景。90 个 scene 哈希都受 Unity 重打包影响，但只有 3 个导出 scene 的字节长度改变，语义检查也只在这 3 个 scene 中找到非 GUID/fileID 变化。
- 8 个 bundle 中 4 个换成新哈希名、4 个逐字节相同。三个 Wwise bundle 全部相同；本补丁没有新增或替换 BGM、CD 或其它 Wwise 音频字节。
- AssetRipper 的 45,882 个 changed 主要来自 Unity GUID/fileID 重写，不能解释成等量的实际内容修改。firstpass 完全不变。
- 成就图的序列化正文长度和 SHA-256 完全相同，没有新增或改变 Steam/游戏内成就节点。

### Native code

主程序集的真实差异为 13 个文件、111 insertions / 31 deletions：

- 保存/无人机：`CropGeneFunctionHope`、`AutomateBot`、`AutomateTaskGatherEquipment`。
- 对话/节日：`DolocConst`、`DialogueTask_TimeCheck`、`DialogueManager`、`DialogueState`、`CharacterRenderer`。
- 任务迁移：`VersionPatchFunctions`。
- 其它：`ItemSleepingBag`、`AgentEquipmentManager`、`CropGeneUtils`，以及 `CropGeneFunctionProtoInfertility` 到 `CropGeneFunctionInfertility` 的类名修正。

`UpdateTo10007` 是本次旧档补偿中心：它重放岛屿家具配方解锁、旧城发电站完成事件和奥兰多 schedule，补齐已击杀年兽的两个怪物图鉴节点，并迁移/补齐 NPC 档案记录。

### Declarative content and scenes

- 配置只有 3 张变化：
  - `item_tbitem.json`: 990 行不增不减；17 个怪物雕像的 `salable` 从 `false` 改为 `true`，`apple_pie` 从 `food_cooking` 改为 `food_baking`。
  - `recipe_tbrecipe.json`: 483 -> 500 行；只新增 17 条隐藏、默认锁定的怪物雕像配方，没有删除或修改旧配方。
  - `localization_tbtextmapperen.json`: 只修正 Panorama Screenshot 文本中 action tag 后的一处空格。
- Yarn 只有 7 个文件变化：`eden_main`、`trading_port_main`、`ruined_city`、`hult_favorability`、`villain_favorability`、`zenis_favorability`、`evernight_festival`。
- 任务图变化集中于：
  - `npc_favorability.asset` 新增 3 个 `MonthAndDay` 条件；
  - `ruinedcity_siren.asset` 的两个发电站 listener 都增加 `isGlobalCount: true`；
  - `schedule_sacco.asset` 只改变编辑器平移/缩放视图，不是玩法变化。
- 3 个有真实长度变化的 scene：
  - build index 30 `city_泽尼瑟酒馆`: -3,834 bytes；移除右侧酒馆椅和两把右侧吧台椅上的 3 个 `FESTIVAL.EvernightNewyear` 条件组件。
  - build index 62 `dungeon_山脊谷地`: +21,431 bytes；CompositeCollider 路径变化，主要 Tilemap 引用计数 10,336 -> 10,413，边界从 `(-121,-1,247,93)` 扩为 `(-121,-3,247,95)`。
  - build index 87 `dungeon_旧城市废墟`: +47 bytes；可移动平台 EdgeCollider 的 4 个中间点变化，`低处住宅隐藏` 与一个垃圾袋对象分别换位。
- PNG 只有两组真实像素变化：
  - `武器店.png` 仍为 442x211，2,794 个像素变化，范围为 x=0..105 / y=123..210；
  - `AtlasEquipments` 仍为 1024x1024，只有 38 个像素变化，范围为 x=832..837 / y=516..522。

### Native public-surface drift

这不是 DTMAPI 公共 API 变化，但对直接链接游戏内部类型的 CodeMod 属于真实 ABI 风险：

- 删除公共类/构造器名 `CropGeneFunctionProtoInfertility`，改为 `CropGeneFunctionInfertility`。
- `DialogueManager.UnhandledDialogueNodes` 的返回类型从 `Queue<DialogueNodeData>` 改为 `List<DialogueNodeData>`；对应 JSON 构造器参数也同步变更。
- 新增 `DialogueManager.IsFinalNode(string)` 和 `DolocConst.DialogueTag_IsFinalNode`。

对 DTMAPI `src`、`products`、`first-party-mods`、`testmods`、`tests` 做精确成员/类型扫描，没有发现对上述变更成员或旧不育类名的直接消费者。命中的 `MoveModifier.conveyorOffset` 仅为既有 AutoFishing QA 采样，不调用本次变化的 `ItemSleepingBag` 路径。

## Official Announcement Cross-Check

以下“直接”表示 1.00.06 -> 1.00.07 静态差异已定位到对应 owner；它证明实现进入 payload，不等于完成玩家运行验收。

| # | 公告项 | 静态结论 | 1.00.07 实际证据 |
| ---: | --- | --- | --- |
| 1 | “希望”基因返种在睡醒时未及时保存 | 直接 | `CropGeneFunctionHope.DropSeed` 删除 `UniTask.Delay(200)`，在收获回调内同步克隆并 `CreateDropItem`，之后才播放粒子；同时移除重复的第二次 `base.AfterHarvest`。这与先前 Review 建议的“权威状态现在写入，表现随后处理”一致。 |
| 2 | 采集无人机偶发生成无法拾取的掉落物 | 直接 | 房间掉落改用 `currentRoom.IsRenderNow` 判断当前房间是否渲染；设备收集掉落改用 `_equipment.IsRender`，不再错误复用无人机自身的 `Bot.IsRenderNow`。 |
| 3 | 支线在年夜饭开始后触发导致节日异常 | 直接 | 对话队列由 Queue 改为可选择的 List，`is_final` 节点会等所有非 final 节点处理后再出队；好感图新增 3 条 4 月 28 日精确条件，其中澳柯玛 10 心、泽尼瑟 9 心用 `_invert: true` 排除该日，泽尼瑟 4 心无反转、只在该日成立。酒馆 scene 同时移除 3 个年夜饭座椅条件组件。 |
| 4 | 长时间动画可能跳过年夜饭 | 直接机制、运行待验 | 年夜饭与爆竹节点均加 `is_final`；年夜饭入口新增“酒馆 + 18..23 时 + 4 月 28 日”硬门，爆竹节点只在东部远郊清场/播动画；`CharacterRenderer.InternalWalkTo` 另加 transform-null 保护。精确长动画复现未运行。 |
| 5 | 帕依雅商店岛屿家具组配方不能解锁 | 直接 | 正常商埠修复剧情新增 `unlock_store_item paiea_shop recipe_islands_basics`；`UpdateTo10007` 又调用既有 `version_patch9601`，为已越过剧情的旧档补解锁并刷新商店。 |
| 6 | 怪物雕像不能丢弃与售卖 | 直接配置 | 17 个雕像全部从 `salable: false` 改为 `true`，并增加对应隐藏配方；其 `selling_price=-1` 会由 `ItemEquipment.GetSellingPrice` 按配方材料求值。它们原本已是 `disposable: true`；新增 `salable` 也放开按 Salable 过滤的设备回收/丢弃路径。 |
| 7 | 旧城可移动平台上能使用睡袋 | 直接 | `ItemSleepingBag.CheckCondition` 在 `MoveModifier.conveyorOffset.magnitude > 0.1` 时显示拒绝表情并返回 false。 |
| 8 | 非常规顺序修复旧城发电站导致任务卡住 | 直接 | 两个发电站任务 listener 都改为全局计数，不再依赖局部顺序；1.00.07 迁移为已完成工业发电站事件的旧档重新广播完成；旧城 boss gate 还允许环境改造器达到 6 级时恢复通过。 |
| 9 | 非常规顺序触发河谷封锁动画导致奥兰多失踪 | 直接 | `eden_main.yarn` 不再在封锁动画后禁用奥兰多和凯尔 schedule；1.00.07 迁移对已经完成 `drone_guide_anim` 的旧档重新启用奥兰多。 |
| 10 | 旧城市废墟地形卡死 | 直接资源证据 | build index 87 的可移动平台 EdgeCollider 4 个点发生实质变化，并调整低处住宅隐藏对象和垃圾袋的位置；未启动游戏验证实际碰撞。 |
| 11 | 山脊谷地地形卡死 | 直接资源证据 | build index 62 的 CompositeCollider 与 Tilemap 明确扩展，增加 77 个主要 tile 引用并向下扩两格；未启动游戏验证实际碰撞。 |
| 12 | 外部 Mod 不兼容导致角色档案未解锁 | 直接迁移 | `UpdateTo10007` 遍历当前 `TbNpcDocument.DataList`：若 NPC 记录错误留在通用 `collections` 而不在 `npcCollections`，则迁移并刷新；其余缺项补空记录。它是一次官方旧档迁移，不应外推成任意加载时序下的长期 Mod 兼容保证。 |
| 13 | 部分贴图问题 | 直接资源证据 | 实际 PNG 变化只落在 442x211 的 `武器店.png` 和 1024x1024 的 `AtlasEquipments`；前者 2,794 像素、后者 38 像素变化。 |
| 14 | 部分文本错误 | 直接 | Yarn 有 5 处中文文字修正：镇政厅笔记本 -> 商埠记事本、男爵涂装语序、包扎“地/得”、补出“洛维那”、删除“你你”重复字；英文 Panorama Screenshot 文本另去掉一处多余空格。 |

上一公告列出的 3 个“下个补丁修复”问题——希望返种、年夜饭支线冲突、帕依雅配方——本次均能找到对应静态实现。第一项精确采用先前 Review 推荐的同步状态提交方案；但三项都未做本机游戏运行验收。

## Additional Unannounced Changes

- `villain_favorability.yarn` 把不可能同时成立的 `hour >= 22 && hour < 10` 修为跨午夜的 `hour >= 22 || hour < 10`。
- `apple_pie` 分类从烹饪改为烘焙。
- 1.00.07 迁移为已经击杀年兽的旧档补 `nian_kill_1` / `nian_kill_2` 怪物图鉴节点。
- 不育基因运行类移除 `Proto` 命名残留；`CropGeneUtils` 删除一个无效果的空 HashSet 分支；主角装备技能日志增加物品名。
- Sacco schedule 资源仅有编辑器视图平移/缩放变化，不计为玩法修改。

## DTMAPI Boundary

- 本补丁没有触碰 DTMAPI 当前畜牧、音频、地图数量或 firstpass owner；不要求因 1.00.07 自动重做动物接口、音频桥或地图 API。
- 对话队列类型和不育基因类名是直接使用官方内部 ABI 的外部 CodeMod 风险，但当前 DTMAPI 生产/第一方源码没有直接消费者，因此本任务不修改 Runtime 或产品代码。
- NPC 档案变化是 `VersionPatch("1.00.07")` 的一次性原生迁移。它是对 Mod 相关坏档的积极信号，但不能替代 DTMAPI Content Host 的加载顺序、刷新和持续兼容契约。
- 当前硬编码使用 `24966367_public_958EAF` 的邮件条件快照、AnimalPack 经济校验及特定 QA trace 都保持原样；捕获新观察头不自动重生成产品数据或改变已验收基线。
- 捕获只把 `25163613_public_604898` 推进为最新本地 public reverse 观察头；Author exact policy、compatibility receipt、产品 admission、订阅清单和发布授权均不随静态捕获自动变化。

## Validation

- Full capture exited `0`；source-start / frozen / source-end Steam identity 一致，raw source parity 为 exact。
- 独立重新枚举并 SHA-256 校验四棵树：raw `542/542`、AssetRipper `52,089/52,089`、main `3,671/3,671`、firstpass `29/29`；均为 0 missing、0 extra、0 length mismatch、0 hash mismatch。
- 分别对 raw、AssetRipper export、managed assemblies、built scenes、bundles、config、Yarn、ILSpy main/firstpass、任务序列化图、3 个实质 scene 和 PNG 像素做稳定路径/语义比较。
- 对 DTMAPI `src`、`products`、`first-party-mods`、`testmods`、`tests` 做变化 owner 消费者扫描。
- 文档治理通过 7,913 项检查；本 Update、月度索引和两份 Review 的目标 `git diff --check` 通过。
- 最终确认 `DolocTown.exe` 未运行、共享 Runtime lock 为 free；新 reverse 目录仍由 `.gitignore` 隔离。
- 不启动游戏，不执行存档、玩家行为、完整 Release 或 Workshop 发布测试；静态证据充分用于捕获与差异解释，但不把公告的运行结果记为 smoke PASS。

## Evidence

- Capture root: `references/doloc-town/reverse/builds/25163613_public_604898/`.
- Capture summary: `full-baseline-inventory/summary.json` and `portable-full-capture-summary.json`.
- Source parity: `full-baseline-inventory/snapshot-source-parity.json`.
- Full inventories: `raw-snapshot-files.json`, `asset-ripper-export-files.json`, `Assembly-CSharp-decompiled-files.json`, `Assembly-CSharp-firstpass-decompiled-files.json`.
- Built scene identity: `full-baseline-inventory/built-scenes.json`.

## Related Records

- Previous public observation head: `docs/updates/2026/20260827-0002-public-10006-full-reverse-capture.md`.
- Hope save-race review: `docs/reviews/manual-qa/2026/20260827-0001-official-hope-seed-wake-save-race.md`.
- Evernight warm-reload review: `docs/reviews/manual-qa/2026/20260823-0003-evernight-newyear-warm-reload-partial-init.md`.

## Rollback Notes

- 删除本 Update 的月度索引行和新增的 ignored 本地捕获目录即可回滚本次捕获记录；不要覆盖或删除任何较早 reverse build。
- 两份 Manual QA Review 的 2026-09-07 追加段为新静态事实，可随本 Update 一并回滚；其原始用户观察和历史根因段不得改写。
- 本任务没有修改 DTMAPI Runtime、官方游戏安装、`MODS`、Workshop 订阅或存档。

## Follow-Up

- 后续逆向/原生责任研究以 `25163613_public_604898` 作为最新 public 观察头，并保留 `24966367_public_958EAF` 作为直接差异基线。
- 如需把 1.00.07 纳入正式兼容权威，另开 bounded compatibility lifecycle，只测试实际受影响的 Hook、产品与 ABI；本次静态捕获不代替该步骤。
- “希望”返种需要 disposable、AutoCloud 隔离的 cold-save A/B 才能从静态修复提升为运行验收。年夜饭旧 Review 中的灯男钓鱼绳对象池与 `FestivalState` 初始化 owner 在 1.00.07 仍逐字节不变，不能因这次触发排序修复而关闭原半初始化问题。
