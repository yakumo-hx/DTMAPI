# Y 键控制台动物包目录、来源栏切换与传送旧辅助功能复核

## Review Header

- Date: `2026-08-31`
- Status: `recorded`
- Scope: Y 键控制台运行时动物目录、DTMAPI AnimalPack 来源归属、物品/怪物/动物来源栏切换，以及传送 CSV/当前位置旧 UI 债
- Source: 用户本轮文字反馈与三张游戏内截图
- User constraints:
  - 其他 Mod 物品进入控制台属于正常能力，不得因本轮动物修正而收窄。
  - 先研究最小实现与硬编码事实，再让本地 DTMAPI 动物拓展包的新动物进入控制台。
  - 取消点击怪物/动物后左侧来源栏重建为临时列表的行为；可把生物卡数量并入总体来源计数。
  - 删除传送地址 CSV 导出代码和按钮，删除玩家可见“当前位置”行。
- Owning Update: [20260831-0001-y-console-runtime-animal-catalog-and-teleport-cleanup](../../../../updates/2026/20260831-0001-y-console-runtime-animal-catalog-and-teleport-cleanup.md)
- Related records:
  - [Y 键控制台 1.1.2 手测接收、0.3.1 退役授权与旧城守护者静态审查](20260830-0001-y-console-112-manual-acceptance-retirement-and-space-ship-review.md)
  - [Y Console 1.1.0 Semantic UI and ProductNative Actions](../../../updates/2026/20260812-0001-y-console-semantic-ui-productnative.md)
  - [统一 AnimalPack 经济与小动物照料站](../../../../updates/2026/20260827-0003-unified-animal-pack-economy-and-care-station.md)

本 Review 按用户给出的两个问题顺序保存截图转写、代码事实、推断和验收门。
它是实施前根因记录，不以截图本身证明修复完成。

## 问题 1：AnimalPack 新动物未进入控制台，怪物/动物会替换左侧来源栏

### 原始反馈

- 其他 Mod 的物品可以由 Y 键控制台取得，这是正常设计。
- 本地已有基于 DTMAPI 的动物拓展包并添加了具体新动物，需要让控制台兼容。
- 点击动物界面（包括同类生物目录）后 UI 会切换一次，左侧栏位不应变化。
- 左栏最初只按物品构建，本体显示 `990`，原版物品表不包含生物；后来为生物目录另算数量并替换左栏。用户认为这层设计可以取消，或把生物数量直接并入整体。
- 图片转写：
  - 图一处于普通物品目录，左侧“来源”依次显示“本体 990”“模组 14”“Y键控制台 1”“缺氧动物包 13”；右侧分类含“怪物”“动物”。
  - 图二点击“怪物”后，左侧来源栏只剩“本体 26”，顶部也显示 `26 项 | 第 1/1 页`；原有“模组”“Y键控制台”“缺氧动物包”按钮全部消失，形成一次明显的栏位切换。

### 审查记录

- 用户确认事实：
  - 当前普通物品跨 Mod 发现与给予路径可用，应保留。
  - 缺氧动物包含四个真实新物种，而不只是物品。
  - 左侧来源栏的临时切换不是期望交互。
- 截图观察：
  - 来源栏确实从四行物品来源替换为单行“本体 26”；这不是同一来源模型下的筛选结果，而是整组控件数据被换掉。
- 代码事实：
  - `DebugConsoleNativeActions.Spawn.cs` 的动物目录由 `StableAnimalIds` 固定为 `slime/chicken/goat/marsh_pangolin` 四项；每项再展开 Child/Adult/Ready 三张状态卡。它没有枚举运行时 `TbAnimal`，因此 AnimalPack 的 `hatch/drecko/mole/oilfloater` 即使已经成功合并进原生表，也不会进入目录或 `animalProtos`。
  - 怪物目录已经从运行时 `assets.monsters.TotalProtos` 枚举，不是同类硬编码。
  - AnimalPack 的 `Content/animal_tbanimal.json` 定义四个物种；`Content/DTMAPI/custom-animals.json` 用相同 species ID 声明其 DTMAPI 自定义 Animator/AI/PNG 归属。运行时已有实际加载、渲染和声音隔离证据。
  - `BuildItemsTab` 对 Monster/Animal 使用 `virtualCategory` 分支：不再取得物品来源页，而是现场构造一个只含 `__base` 的来源组；点击这两个分类还强制把 `sourceFilter` 重置为 `__base`。这正是截图切换的直接原因。
  - 普通来源计数来自物品索引；动物状态卡当前没有来源字段，因此不能把 AnimalPack 的三种状态卡直接合并到对应来源。
- Codex 推断：
  - 最小生成修复不是新增 public CustomAnimal API，也不是从 AnimalPack 名称/固定四 ID 再建一张白名单；原生 `TbAnimal.DataList` 已是当前房间 `IAnimalHost.CreateAnimal` 使用的 proto 真源，动态枚举它即可让已加载动物包复用同一生成路径。
  - 来源归属只需要读取现有 DTMAPI `custom-animals.json` 的 species ID，并借当前内容索引的精确 Mod 根映射到既有来源 ID/显示名；这属于 DebugConsole ProductNative 目录元数据，不改变 Frozen `ICustomAnimalApi` 或 GameBridge 所有权。
  - 左栏应采用一个稳定的统一来源集合：本体计数加上原生怪物和原生动物状态卡；“模组”及具体来源计数加上其自定义动物状态卡。计数单位保持“当前控制台可选卡”，所以一种动物的 Child/Adult/Ready 计为三项。
- 反证/未证实：
  - 不能从 `custom-animals.json` 单独推断动物已进入游戏；最终目录仍必须以运行时 `TbAnimal` 中实际存在的 proto 为准。
  - 本轮没有发现或承诺通用自定义怪物来源协议；当前怪物目录仍归入本体来源。以后若存在真实 DTMAPI 自定义怪物内容宿主，应另行提供来源元数据，而不是猜测 JSON 文件名。
  - 其他 Mod 物品发现路径不是问题根因，不应改成 AnimalPack 专用扫描。
- 归属：
  - 动物 proto 枚举、来源投影、生成入口：Y 键控制台 ProductNative。
  - AnimalPack 的物种与资源：AnimalPack ContentPack；控制台只读取已经激活的内容元数据与原生表，不接管动物生命周期。
  - `TbAnimal` 合并和 `IAnimalHost.CreateAnimal`：官方/SharedNative 已有内容加载与原生责任路径，本轮不新增 Hook。
- 需要更新：本 Review、Owning Update；public API/Hook/Debug issue 当前事实不因动物目录接入而改变。
- 验收点：
  - 源码不再出现固定四动物目录；运行时表新增任意有效动物 proto 后，目录自动生成 Child/Adult/Ready 三张卡。
  - AnimalPack 四个 species 都映射到“缺氧动物包”来源；选“模组”或该具体来源后可见其状态卡，选“本体”只见原生动物。
  - 来源按钮集合与计数不因普通物品/怪物/动物分类切换而重建；选择来源和选择分类互不清空。
  - `hatch.child` 等自定义卡进入现有 `SpawnAnimal` proto 路径，不再返回 `unknown-animal-card`。
- blocker 判定：如果运行时 `TbAnimal` 不含 pack 的 species，则控制台必须不显示该动物，并把问题留在内容加载链；不得用 JSON 声明伪造可生成状态。

## 问题 2：删除传送 CSV 导出和“当前位置”显示

### 原始反馈

- 地址导出是过去为手工调整传送地址而做，现在不再需要；相关代码可以删除。
- “当前位置”毫无意义，也应删除。
- 图片转写：图三的传送区标题同行显示“当前位置：当前位置”和蓝色“导出 CSV”按钮，下方才是固定传送目的地按钮。

### 审查记录

- 用户确认事实：手工地址审定阶段已经结束，当前产品不再需要 CSV 产物或当前位置文案。
- 截图观察：“当前位置：当前位置”在无法取得友好房间名时重复占位文字，既不提供内部 ID，也不帮助选择目的地；CSV 按钮同时占用首行宽度。
- 代码事实：
  - 当前 ProductNative UI 每次构建传送区都会调用 `GetCurrentSnapshot()`，再经 `DisplaySafeLocation`/`LooksInternalLocationName` 生成该行。
  - `ExportTeleportCsv` 调用 `ExportDestinationsCsv`，在 DTMAPI evidence 下创建时间戳目录并写 `teleport-destinations.csv`；产品为此保留 `EvidencePath`、CSV 转义和五组九语言键。
  - `ITeleportDebugApi.ExportDestinationsCsv` 与 `TeleportCsvExportResult` 是旧 Diagnostic Compatibility 表面的一部分；当前 ProductNative 直接复用了结果 DTO，但不调用旧 provider。
  - 前一轮用户已确认精确旧 `0.3.1-dtmapi` Y 控制台无使用者，并解除其兼容保留限制；本轮又明确要求删除地址导出代码。因此可以把这一个旧成员和只为它存在的 DTO 作为有界 breaking cleanup 删除，而不需要继续保留伪入口。
  - `TeleportSnapshot` 仍被 `TeleportResult.Before/AfterRequest`、原生传送完成验证和 InstantSave 状态使用；删除玩家可见“当前位置”不等于删除动作校验快照。
- Codex 推断：
  - 应物理删除 ProductNative 按钮、动作、文件写入、内部端口、结果 DTO、旧 Compatibility proxy/facade/QA 调用和废弃本地化键，而不是只把按钮隐藏。
  - 应保留 `GetCurrentSnapshot` 的内部/Diagnostic 读路径，因为它继续证明传送前后实际变化，并不是截图中的无意义占位控件。
- 反证/未证实：
  - 固定传送目录、MarkPoint 白名单和 `DolocAPI.DoTransport` 不属于地址导出旧工具，不能随 CSV 一起删除。
  - 历史 Review/Update 中记录过的 CSV 验收是当时事实，应保持只读，不回写成“从未存在”。
- 归属：
  - 当前 UI/文件导出：DebugConsole ProductNative。
  - 被删除的 public Diagnostic member/DTO 与代理：Abstractions、GameBridge/Compatibility、QA/ABI 测试的有界同步修改。
- 需要更新：Owning Update、public API matrix；历史 Review、Hook Map 与 Debug issue 不改写。
- 验收点：
  - 传送区首行只保留标题；不存在 Current/当前位置文本对象和 Export CSV 按钮。
  - 当前产品、Abstractions、Compatibility proxy/facade 和 QA 源码均不再含 `ExportDestinationsCsv` 或 `TeleportCsvExportResult`。
  - 九语言文件删除仅供这两个控件使用的键并保持完全同构、合法 JSON。
  - 固定传送按钮、动作结果的 Before/AfterRequest 以及运行时位置变化验证继续存在。
- blocker 判定：如果删除旧成员会命中仍存在的真实消费者证据，则 public 删除必须停止；本轮现有 Catalog/本地扫描与用户最新事实均未给出该阻塞。

## Validation Classification

- 动物目录和来源合并：source/unit；如执行实际游戏验收，应使用已包含 AnimalPack 的隔离 `NoNativeSave` 夹具并获取 Runtime lock。
- 传送旧辅助删除：source/unit/API/ABI 静态检查；它不需要为删除一个控件单独触发原生传送或保存。
- 不修改 Steam subscription、官方上传目录、玩家存档或发布授权。
