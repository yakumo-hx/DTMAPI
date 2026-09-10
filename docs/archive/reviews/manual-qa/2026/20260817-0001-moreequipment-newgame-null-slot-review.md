# MoreEquipmentSlots 新建存档不显示 Product 三槽审查

- Review ID: `20260817-0001`
- Date: `2026-08-17`
- Status: `root cause confirmed; implementation not started`
- Scope: MoreEquipmentSlots 路线图/技术债权威定位，倒数第二次启动日志，新建存档首个会话的 UI 与保存边界
- Source: 用户手测反馈与本机倒数第二次 DTMAPI/Unity 启动日志
- Current lifecycle owner: [20260811-0001 MoreEquipmentSlots 1.0.0 direct replacement](../../../updates/2026/20260811-0001-moreequipment-slots-100-direct-replacement.md)
- Historical roadmap only: [20260802-0001 DTMAPI 0.6.0 authority roadmap](../../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)
- Related records: [20260806-0001 official slot growth review](20260806-0001-moreequipment-official-slot-growth-review.md), [ISSUE-021](../../../../debug/issues/ISSUE-021-20260805-moreequipment-native-placement-save-quarantine.md), [ISSUE-024](../../../../debug/issues/ISSUE-024-20260817-moreequipment-newgame-null-slot-ui.md)

本记录保持用户反馈顺序，并把用户观察、日志事实、源码事实和推论分开。本轮只读审查，没有启动游戏、获取 Runtime lock、修改安装环境、读写玩家存档或改动产品实现；因此不新增 smoke 行，也不创建实现 Update。

## 问题 1：新建存档不会出现三个额外饰品栏

### 原始反馈

- 阅读工作空间的 `AGENTS.md`、`PROJECT.md`。
- 定位本地有关更多饰品栏位的 Update、Debug，尤其是列为后续路线图实施的内容和部分技术债。
- 阅读倒数第二次启动日志；该次创建了一个新存档，新存档不会出现三个饰品栏；审查问题。
- 图片转写：无截图。

### 日志选择

- DTMAPI 当前日志 `D:\Steam\steamapps\common\Doloc Town\DTMAPI\logs\latest.log` 属于随后一次 `00:21` 开始的会话。
- 倒数第二次 DTMAPI 日志是 `D:\Steam\steamapps\common\Doloc Town\DTMAPI\logs\latest-20260816-162048769.log`，本地写入结束时间约为 `2026-08-17 00:20:48 +08:00`。
- 与它对应的 Unity 日志是 `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\Player-prev.log`，写入结束时间约为 `2026-08-17 00:20:50 +08:00`。

### 用户确认事实

- 该会话创建了一个新存档。
- 在新存档的首个会话中，打开装备界面后没有显示 MoreEquipmentSlots 拥有的三个 Product 槽。

### 日志事实

- DTMAPI 日志第 `16` 行显示重复来源仲裁选择启用的 Local MoreEquipmentSlots，忽略禁用的 Workshop 副本；不是同时加载两份。
- 第 `65–71` 行显示 Advanced receipt 被接受、五个 Product Hook 安装、`slotCount=3`、事务提交且 Entry 完成。
- 第 `310` 行记录 `SaveLoaded hook dispatched. slot/index=unknown isNewGame=True`；第 `317–324` 行继续保留 `slot=none`、`isNewGame=True`。
- 第 `330–331` 行显示 MoreEquipmentSlots 收到 `SaveLoaded` 后清理 UI，状态为 `clones=0`、`roots=0`、`hooks=5`。之后没有 Product UI rendered 或布局 fail-closed 记录。
- Unity `Player-prev.log` 第 `716–723` 行和 `734–737` 行显示 `EquipmentBarUiState` 在该新档中实际进入、退出两次，排除“没有打开装备界面”。
- DTMAPI 第 `346` 行的 `Feature.EquipmentSlots=ready` 是 GameBridge feature fanout 成功，不是 MoreEquipmentSlots Product 三槽已渲染的证据。

### 当前包与配置事实

- Local 官方目录与 Steam 订阅目录均是 `1.0.0`，八个文件逐字一致；Product DLL SHA-256 均为 `699E95BC05E79F67EE45D83C89D8119EB2BE723FF342CF2DA0CD0A2C7DBC8E31`。
- 当前配置 `Enabled=true`、`ExtraAttributeSlots=3`。
- 因而可排除禁用配置、旧 `0.3.1` 字节、Local/Workshop 内容漂移和错误发布包。

### 源码事实

- `SaveLoadedEventArgs` 同时公开 `SaveSlot` 和 `IsNewGame`；Core 也把两者一起派发。
- Product 的 `ModEntry.OnSaveLoaded` 只调用 `runtime.OnSaveLoaded(e.SaveSlot)`，丢弃 `e.IsNewGame`。
- `MoreEquipmentSlotsNativeRuntime.OnSaveLoaded(int? slot)` 遇到 `null` 或负数会把 `archiveIndex` 设为 `-1`，把 `document`、`scope` 清空，记录内部消息 `SaveLoaded without archive.` 后返回。
- `RenderAccessoriesBar` 在 `document == null` 时无日志直接返回；所以原生 `AccessoriesBar.RenderPassiveItems` Postfix 即使执行，也不会创建三个 Product clone。
- 当前物理/Unit fixture 只覆盖 `runtime.OnSaveLoaded(2)`，没有 `SaveLoaded(null, true)` 或原生 NewGame 首会话路径。

### 根因

这是确定性的生命周期/状态建模缺口：原生新建存档会合法地产生 `SaveLoaded(saveSlot: null, isNewGame: true)`，但 MoreEquipmentSlots 丢弃了 `IsNewGame`，把“新档尚未取得权威 archive scope”与“无效/未知档案”合并为同一状态。运行时因此清空 Product document；随后 UI Hook 因 `document == null` 静默退出，三个槽在新档第一次冷重载前不会出现。

后一次 `latest.log` 已把同一新档作为既有 `slot 4 / isNewGame=False` 加载，说明 null-slot 是新建会话特有边界；但后一次 Unity 日志没有再次打开装备栏，不能把它当作重载后 UI 已恢复的验收。

### 已有存档为何能正常得到三个 Product 槽

正常路径不要求该存档已经有 MoreEquipment sidecar，也不要求先使用饰品包：

1. 玩家从标题载入已有存档时，原生 `DolocAPI.LoadGame(index)` 经过 DTMAPI 的 LoadGame Hook，Core 先记住确定的零基 `index`。
2. 原生随后调用 `AfterLoadArchiveData(isNewGame: false)`；DTMAPI 据此派发 `SaveLoaded(saveSlot: index, isNewGame: false)`。
3. Product 虽然丢弃 `IsNewGame`，但这次 `e.SaveSlot` 有值，所以 `OnSaveLoaded(index)` 能读取当前 `archiveHandle` scope，并建立该档案的 sidecar 路径。
4. 有有效 Product-v3 sidecar 时加载/迁移它；完全没有 sidecar 时，`LoadOrMigrate` 也会建立一个带精确 scope、固定三个空槽的内存 document。缺少旧 sidecar 本身不会隐藏 UI。
5. 玩家打开装备界面时，原生按当前 `passiveItems.Length` 调用 `AccessoriesBar.RenderPassiveItems`。Product Postfix 此时看到 `document != null`，便在全部官方被动槽之后创建/复用三个 clone，并把它们追加进导航。
6. 空 document 的创建不等于提前写盘；只有玩家改变 Product 槽并经过受保护的 native `SaveGame` / `SaveSaved` 后，普通 gameplay 状态才会提交。

本机后一会话已经完成上述第 `1–4` 步：日志为 `LoadGame requested slot/index 4`、随后 `SaveLoaded slot/index=4 isNewGame=False`。由于该会话没有再次打开装备界面的日志，尚未观察第 `5` 步，但代码已不再处于本次 null-document 分支。

### “使用一次饰品包才解锁”辨析

- 官方新档的 `AgentEquipmentManager` 把 `passiveItems` 初始化为长度 `1`。这表示一个**官方被动饰品槽**；加上帽子和主动饰品，初始画面有三个官方装备位置。它与 MoreEquipment 固定拥有的三个 Product 槽不是同一组“三区”。
- `use_accessory_bag` 对话只执行 `add_accessory_slot`。该命令读取 `passiveItems.Length` 并调用 `SetPassiveSlotCount(length + 1)`；第一次使用是把官方被动槽从 `1` 扩为 `2`，保留旧物品并重算官方装备参数。
- 这条命令没有调用 `LoadGame`、`SaveGame`、`SaveLoaded`，也没有创建 MoreEquipment document。因此饰品包不是 Product 三槽的解锁条件。
- 对**已经正确载入 document 的已有档**，饰品包改变官方槽数后会引起原生 UI 重绘；Product 的 Render Postfix 会随之把既有三个 clone 移到新的官方尾槽之后。这个可见变化可能被误解为“饰品包解锁了 Mod”。
- 对本次**新建档 null-document** 会话，同样的重绘仍会在 `document == null` 处静默返回。即使随后在同一会话触发保存，当前 `OnSaveSaving` / `OnSaveSaved` 也都要求已有匹配 slot 和非空 document，不能借此补建 Product 状态。
- 真正能改变本次状态的是：原生先成功保存出档案，返回标题后再把它作为已有档载入。此时 LoadGame Hook 得到确定 index，Product 才走前述正常路径。若所谓“使用一次饰品包后出现三槽”的过程包含睡觉保存、返回标题或重启，那么真正起作用的是后续已有档加载，不是饰品包。

若能确认在当前 `1.0.0`、同一个新建档首会话中，既没有保存/返回标题/重载，也没有切换产品状态，却在使用饰品包后立即出现恰好三个 Product 槽，那么它与当前确定代码路径矛盾，应保留那一次的 DTMAPI 日志和 `Player.log` 作为另一条独立复现，不应据传闻改写本 Issue 的根因。

### 被否决的候选原因

- **Mod 未启用：** 配置为 true，Local 来源被启用并成功加载。
- **仍在运行旧包或错误包：** Local 与 Workshop 都是相同的当前 `1.0.0` 精确字节。
- **Hook 安装失败：** 日志明确记录五个 Hook 安装和 Entry 成功。
- **官方被动槽达到六个而触发有意 fail-closed：** 新档原生被动槽从一开始，且日志没有布局拒绝；代码在布局判断之前已经因 null document 返回。
- **玩家没有打开装备栏：** Unity 日志证明实际打开两次。
- **ISSUE-021 复发：** ISSUE-021 管的是放置结果、保存隔离和 cold recovery；本次尚未进入 Product UI/document，更没有物品放置事务。
- **当前游戏 build Drift 破坏 Hook：** exact 目标已解析并安装，当前症状由可直接到达的 Product 分支解释；没有 Drift 导致目标缺失的日志。
- **必须先使用一次饰品包才能解锁 Product 三槽：** 饰品包只增加一个官方被动槽并触发官方 UI 重绘；它不建立 Product document。包含保存/重载的观察把时间相关误当成了因果。

## 路线图与技术债定位

### 当前权威

- 当前生命周期权威是 `20260811-0001`，状态 `verified`；它已经完成并发布 MoreEquipmentSlots `1.0.0`，支持官方被动槽总数 `1–5` 后追加三个 Product 槽。
- `20260802-0001` 曾把动态原生前缀、布局/导航和 MoreEquipmentSlots 1.0 publication 列为后续路线，但该记录已冻结为历史执行材料；这些路线项已由 `20260811-0001` 实现并关闭。不能把旧路线图里的 pending 文本当作当前状态。
- `20260730-0016` 的五项 P1（严格邮件证据、incoming `CostItem` 观察、native-save fingerprint、cold recovery、延迟 count-only reconciliation）也已被后续 Branch B 和 `20260811-0001` 关闭，不是本次仍开放的五项债务。

### 当前仍存在的边界/债务

1. **本次新发现的 NewGame 状态债务：** Product 没有表达“新档已进入 Gameplay，但权威 archive scope 尚未建立”的状态，也没有消费 `IsNewGame`。
2. **测试覆盖债务：** 现有测试和第三存档 UI smoke 都从已有 slot 开始，未覆盖新建存档、首次打开装备栏和首次 native save 绑定。
3. **诊断债务：** `document == null` 使 Product UI 静默返回；宽泛的 `Feature.EquipmentSlots=ready` 容易被误读为 Product UI 成功。日志应区分 `new-game-pending-scope` 与真正的 invalid/no-archive。
4. **跨产品知识未传播：** 历史 MoreSaves 工作已经记录 NewGame 不经过 `LoadGame` Prefix、事件 slot 可能为 null，并改用原生 `archiveHandle.archiveIndex`；MoreEquipment 的生命周期设计和回归矩阵没有吸收这个事实。
5. **有意保留的兼容边界：** 官方被动槽总数六及以上仍明确 fail-closed；旧 `0.3.1` ABI/Compatibility Host 仍为迁移和回滚保留；244 exact Author policy 在当前 246 上按已批准 Drift 激活。这三项是当前受控边界，不是本次缺失三槽的根因。

## 所有权与修正约束

- 该问题属于 MoreEquipmentSlots 单产品的 ProductNative 生命周期、UI 和 sidecar 状态机，不应搬入 GameBridge SharedNative，也不应扩张冻结的 public `IEquipmentSlotsApi`。
- 新档未绑定权威 archive identity 前，可以建立仅内存的空 Working 三槽以渲染 UI；不得猜“下一个空槽”，不得提前创建/提交普通 gameplay sidecar，也不得让 Product 状态领先于最后一次成功的原生 `SaveGame`。
- 首次成功 native save 只能依据精确原生 archive holder/index/identity 绑定 scope，并在 `SaveSaved` 后提交 Product 状态；通知中断窗口仍需遵守 `PROJECT.md` 的 native-commit proof 规则。
- `SaveLoaded(null, false)` 或无法证明 NewGame 的 unknown slot 应继续 fail-closed，不能被新档兼容路径误接纳。

## 后续验收边界

1. source/Unit：`SaveLoaded(null, true)` 建立仅内存的空 Working 会话；原生被动槽数 `1` 时 `RenderPassiveItems` 产生三个 Product clone，导航是完整原生前缀加三槽；`SaveLoaded(null, false)` 仍 fail-closed 并输出可区分诊断。
2. 新建档必须使用与 Steam AutoCloud 隔离的 disposable `ArchiveMutation` fixture；创建后、任何首次 cold reload 之前打开装备栏，验证基础原生槽加三个 Product 槽可见、五 Hook 仍在、无布局拒绝。
3. 建立新档基线后执行 `NoNativeSave`：修改 Product Working 状态并返回标题/冷载，必须恢复先前状态；在任何清理前证明 current/prev/bak 和 committed sidecar 不变。
4. 正常 native save：首次精确绑定 archive identity，只在 `SaveSaved` 后提交；冷载后物品恰好保留一次。
5. `SaveSaving -> native SaveGame -> SaveSaved` 周围的失败窗口：保留前一 committed 状态，或凭精确 native-commit proof 对已成功原生提交做无丢失、无重复的 reconciliation。
6. 回归已有档的官方被动槽 `1–5 + Product 3`、禁用清理、装备/卸下和 ISSUE-021 隔离语义；官方槽 `6+` 继续明确 fail-closed。

## Implementation Record Decision

本轮是 audit-only，创建本 Review 和 `ISSUE-024`，不创建 Update、不修改实现、不启动游戏。后续若开始修正，应新建一个有界 Update：`20260811-0001` 的发布生命周期已经关闭，历史 `20260802-0001` 也不能重新作为当前实现权威；任何未来候选与上传授权必须由新的精确生命周期记录拥有。
