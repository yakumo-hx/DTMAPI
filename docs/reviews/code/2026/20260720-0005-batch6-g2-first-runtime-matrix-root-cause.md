# Batch 6 G2 首轮 Runtime Matrix 根因审查

## Metadata

- 日期：`2026-07-20`
- 状态：`recorded`
- 性质：G2 synthetic 首轮真实运行的 Harmony/QA 生命周期根因审查；不是通过记录
- 证据：`docs/debug/evidence/GAME-SMOKE/20260720-203217`
- Owning Update：[20260720-0007 Batch 6 G2 Advanced Synthetic Vertical Slice](../../../updates/2026/20260720-0007-batch6-g2-advanced-synthetic-vertical-slice.md)

## 观察 1：额外 Manager MVP 场景使 QA Host fail-safe 保留暂存树

### 事实

首轮运行同时请求了 `QaObserveSaveLoaded` 与 `AutoOpenTitleSettingsManagerMvp`。`20:33:08.368`，QA owner 已请求 `manager-status-page.png`，而三秒后的第三存档加载在同一生命周期窗口隐藏标题页并关闭了 QA-owned Manager overlay；下一帧的 exact-session 校验因此以 `The exact Manager Status overlay session changed before screenshot completion` 失败。清理回执期望 `{manager-status-page.png, manager-logs-page.png}`，实际只有前者，所以 `ExactQaEvidence=false`，并按设计保留完整 stage。

### 判断与排除

- Advanced fixture 的 manifest、receipt、Entry、canonical owner、patch postfix 与 native query 在此之前已成功；这次清理失败不是 Advanced package/classifier/loader 缺陷。
- 该运行也不能作为 G2 正向通过，因为无 `result.json`，且后述 Harmony 误报是独立真实阻断。
- G2 合同不要求 Manager 截图与自动存档加载并发。后续 G2 case 只启用 `StageQaHost + QaObserveSaveLoaded`；Doctor/Manager identity projection 继续由独立 package/Doctor/unit gate 验证，不把无关 UI 场景塞进 G2 每次 cold run。
- 已证明 `DolocTown.exe` 不存在，并依据 stage/cleanup 中的长度与 SHA-256 只删除本轮 4 个精确文件以及清空后的拥有目录；没有删除未知 sibling。

## 观察 2：正常 Advanced owner 被平台 Harmony sibling 的晚安装误报

### 事实

同一运行中，`DTMAPI.AdvancedFixture` 以 canonical owner `dtmapi.mod.dtmapi.advancedfixture` 完成 Entry，`PatchPostfix result=False` 与 `NativeQuery result=False` 一致。随后 `AdvancedHarmonySupervisor.AuditActiveOwners()` 把 GameBridge owner `dtmapi.gamebridge.doloctown` 的 16 个 patch 作为 `unattributed` addition，并发布：

`owner=DTMAPI.AdvancedHarmony; code=advanced-harmony-late-owner-drift`

这些 patch 的 owner 和 patch-module MVID 都不属于 synthetic Advanced entry。当前代码虽不会因空 owner 停用 fixture，也不会 unpatch sibling，但仍把正常平台启动记为 Advanced error；因此正常 G2 不能通过，Doctor/Manager/导出报告也会出现错误事实。

### 根因

`AdvancedHarmonySupervisor` 的 late audit 正确地用 canonical owner 或唯一 entry-module MVID 归因 Advanced additions，却又把所有无法归因的全局 additions 作为 Advanced violation 返回。全局 Harmony snapshot 会包含在 Advanced Entry 之后合法安装的 Platform、Strict 或 External sibling；“晚于 Advanced baseline”本身不证明属于 Advanced owner。

### 最小修正

- 只对 canonical owner 命中或唯一 Advanced entry-module MVID 命中的 addition 生成 `advanced-harmony-late-owner-drift`。
- 无法归因到任何 active Advanced owner 的 patch 保持 sibling-owned，并从 Advanced issue 集合忽略；不得记录 Advanced error、不得停用 Advanced owner、不得 unpatch sibling。
- 保留 fail-closed 行为：canonical owner late patch、同一 Advanced entry module 使用错误 owner、wrong owner during Entry、duplicate、snapshot unavailable 与 owner-scoped cleanup 语义不变。
- 单元测试把“未知 sibling 产生空-owner violation”改为“零 Advanced issue”，同时继续断言没有 sibling cleanup。

该保证是可归因边界，不是安全沙箱：canonical owner 始终可归因；错误 owner 只有在 patch method 的 module MVID 唯一命中 active Advanced entry 时可归因。错误 owner 加匿名/外部动态 module 与真实 sibling 在全局 Harmony snapshot 中不可区分，因此必须保持 sibling-owned，否则会违反隔离。若未来要封闭该残余风险，需要独立的 Harmony 调用拦截或 owner execution-context 设计，不属于本次最小修正。

### 被拒绝的替代方案

- 仅在 Runtime 层把空 owner error 降级：仍会让监督器输出虚假的 Advanced violation，其他调用方可继续误用。
- 把 GameBridge owner 加白名单：会把一个通用归因错误固化成产品名单，并遗漏 Strict/External sibling。
- 将任意晚 patch 归给唯一 active Advanced owner：会错误停用并尝试清理无关 Mod，违反 sibling isolation。

## 观察 3：`-AutoReloadMods` 尚未接入 QA 原生 reload 路径

### 事实与根因

G2 的 post-load disable 要求文件 marker 变化后触发原生 `DolocAPI.modManager.ReloadMods()`/`DolocConfig.Reload()`，才能验证 owner deactivation、canonical cleanup 与 restart-required。Author session reload 只处理启动时冻结的 source decision；对已加载 CodeMod 返回 restart-required，不会替代 native discovery/reconciliation。

`run-game-smoke.ps1` 已暴露 `-AutoReloadMods` 并写入 summary，但 `$qaG4ProductOwnerRefreshEnabled` 当前只由 `AssertProductOwnerRefresh` 或 `AssertPublishedProductCombination` 打开。前者会强制 ActionSpeed 等非 G2 产品，不能用于 synthetic-only 验证。

### 最小修正

仅在 `StageQaHost` 已启用时，把 `AutoReloadMods` 纳入现有 `ProductOwnerRefreshEnabled` 开关。复用已审查的三秒后原生 reload 路径，不增加新的 Runtime/产品实现，不改变未传该参数的 smoke。

## 观察 4：Installed Doctor 把嵌套 depot build 当成游戏 build

### 事实与根因

修正候选安装后，真实 `appmanifest_2285550.acf` 的顶层 `AppState.buildid` 为 `23762374`，实际 `Assembly-CSharp.dll` SHA-256 也精确匹配 C416D4 policy；但同一 VDF 的嵌套块还保留 `buildid=23519688`。`SteamGameBuildProbe` 逐行扫描并让最后一个同名 key 覆盖前值，所以 Doctor 错报 `advanced-game-build-incompatible`。

### 最小修正

探针只接受恰好一个顶层 `AppState.appid` 与 `AppState.buildid`，跟踪 VDF brace depth 并忽略 depot/其他嵌套块的同名 key；缺失、重复、未闭合或非数值继续 fail closed。InstallDoctor test 使用“顶层当前 build + 嵌套旧 depot build”回归，证明嵌套值不能覆盖权威游戏 build。

## 观察 5：cleanup machine code 在 participant 汇总边界丢失

### 事实与根因

四个负例的独立 cold run 已分别证明 owner mismatch、duplicate patch、Entry exception 与 late drift 的主失败语义。`AdvancedHarmonySupervisor.RemoveOwner()` 也在 participant result 的 `Details` 中生成 `code=advanced-harmony-cleanup-complete` 或 `code=advanced-harmony-cleanup-failed`。但是 `ModOwnerParticipantCleanupSummary.FormatSummary()` 只保留 `participantId:removed/remaining:ok|failed` 计数，完全丢弃 `Details`；对应四个导出报告 ZIP 的全部文本项都没有任一 cleanup machine code。WrongOwner 只能看到 `0/1:failed(1)`，其余三个只能看到 `2/0:ok` 或 `1/0:ok`。

这不是证据解析器问题。G2 合同把 `advanced-harmony-cleanup-failed` 列为必需失败码，计数不能替代机器码；若让 receipt 只匹配计数，就会把实现与合同的实际偏差固化成假通过。

### 最小修正

- `ModOwnerParticipantCleanupSummary` 在保留原有聚合计数的同时，把每个 participant 的受界、单行 `Details` 写入同一权威 Runtime 日志；换行与 participant 分隔符必须归一化，单项长度必须受限。
- 单元回归从 supervisor result 贯穿到 summary，断言 `advanced-harmony-cleanup-failed` 不再在汇总边界丢失。
- 正式 runtime matrix 必须使用修正后的 Runtime 重跑 WrongOwner，并从收集日志与报告验证 cleanup failure code；cleanup-complete 也作为其余 cleanup 路径的正向可观测性断言。

## 验收门

修正后必须满足：

1. 单元回归同时覆盖 Advanced late drift 与合法 sibling late patch 零误报。
2. runner source test 证明 `AutoReloadMods` 真实接线且仍受 `StageQaHost` 约束。
3. 每个 G2 模式独立 cold process；第三存档内部 index 为 `slot=2`，HookProbe Strict sibling 的 Entry/GameLaunched/OneSecond/SaveLoaded 均通过。
4. Normal 不出现任何 G2 violation；WrongOwner、DuplicatePatch、EntryFailure、LateOwnerDrift 只出现该 case 的精确失败语义。
5. WrongOwner 报告必须同时暴露 `advanced-harmony-owner-mismatch` 与 `advanced-harmony-cleanup-failed`；成功移除 canonical patch 的负例必须暴露 `advanced-harmony-cleanup-complete`，不得用聚合计数代替合同机器码。
6. disabled cold、post-load disable/restart、SDK v1→v2 update/session restart 与最终 v2 clean restart 均有独立解析回执。
7. 每次退出证明无 Fatal、无 forced close、无残留 `DolocTown.exe`；QA stage、官方 profile、config/marker、SDK transaction/journal 只按精确回执恢复。

在上述 runtime matrix 完成前，G2 保持 `in-progress`，AutoFishing 与其他真实产品继续 blocked。

## Resolution

四项修正及其替换运行证据已由 [20260720-0007 Update](../../../updates/2026/20260720-0007-batch6-g2-advanced-synthetic-vertical-slice.md) 收口；被拒绝的 `GAME-SMOKE/20260720-203217` 仍只作根因证据，正式验收使用 Update 所列九例 committed runtime receipt。
