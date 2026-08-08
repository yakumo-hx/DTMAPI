# DTMAPI 0.6.0 第二组五切片并行代码审查

## 记录信息

- 日期：`2026-08-04`
- 状态：`recorded`
- 性质：0.6.0 第二组五个独立功能切片后的并行代码审查
- Source：用户要求每完成五个独立功能切片执行一次并行子智能体审核；本轮覆盖 G2 closure、ChestLocator 静态语义、DebugConsole 1.00 修正、Runtime candidate/published 元数据边界与 Author SDK exact-reference 夹具
- Implementation owner：[DTMAPI 0.6.0 唯一权威路线图](../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)
- 审查基线：`6804156f`（`test(author-sdk): bind advanced fixtures to exact policy bytes`）

本 Review 只保存第二轮并行审查发现、根因和有界关闭条件。修复生命周期、changed files、验证结果与 0.6 发布状态仍由 owning Update 维护；本轮发现及其有界修复属于同一审核周期，不计入下一组五个功能切片。

## 1. 审查范围与方法

三名独立审查者分别检查：

1. G2 历史 closure 与 ChestLocator exact native/caller/transaction 证据；
2. DebugConsole 1.00 native owner、产品/Host 事务、策略分发和测试制品边界；
3. Runtime candidate/published 双 owner、evidence allowlist 与 Author SDK exact-reference/受管会话。

审查只读取仓库、Git 历史和本地 reverse authority；未安装 Runtime、未启动游戏、未写玩家订阅目录或存档，也未获取 Runtime lock。

## 2. 发布阻断发现

### R1：passed G2 receipt override 可绕开历史 Git closure（P1）

永久 G2 门允许 `OwnershipReceiptPath` / `RuntimeReceiptPath` 指向外部副本。Ownership 的 historical commit/blob、单父提交与 closure diff 只在 effective receipt 可由 `git ls-files` 找到时执行，失败时没有 fail-closed；Runtime 也可验证外部 receipt，而 contract-owned 默认 receipt 只证明 tracked/clean。后续若默认 receipt 被改写，调用者可以用外部旧副本重放并跳过真正的历史 closure 身份。

有界修复：`passed` 状态的两个 effective path 必须解析为 contract-owned 默认路径，默认文件必须 tracked/clean；historical ownership receipt 不可追溯时明确失败。外部 ownership、外部 Runtime、二者同时覆盖都必须为负例。`in-progress` 开发候选仍可使用显式覆盖，但不能形成永久 PASS。

### R2：默认 Release 可在缺少私有旧策略输入时跳过 Author SDK Advanced 矩阵并整体报 OK（P1）

Author SDK Tests 找不到本地 `23762374` Assembly 时打印 `SKIP` 并返回，默认 Release driver 只看最终进程退出码。干净环境因此可完全跳过 G2/AutoFishing old-policy build、pack/deploy、receipt tamper 与 native-byte exclusion，却仍输出整个测试集 `OK`。

有界修复：增加显式 exact-reference required 模式并由默认 Release 强制开启；缺输入、缺文件或 hash 不符必须非零退出且不能输出最终 OK。非 Release 的独立开发运行可保留带“不是 release acceptance”标记的 SKIP。每次 staged Assembly/Harmony 和每个派生 game fixture 继续按长度/SHA-256 复核，防止复制窗口漂移。

## 3. Assurance 与一致性发现

### R3：Runtime info 双 owner 只有源码字符串断言，没有 hostile package 负例（P2）

当前 checker 实现本身 fail-closed，但 Unit 只硬编码 Catalog 值并检查脚本标签文本。真正判断若被反转、旁路或变成不可达，只要标签仍在，Unit 仍可能通过。

有界修复：对一次 exact `0.6.0` Runtime package 运行正控和 hostile matrix；缺失/错误 version、合法 JSON 字节漂移、缺 `info.json`、缺/非空 `localized_name` 全部失败；另变异 Catalog 的 candidate/published info hash，证明两个 owner 不能互换。

首次在全新 PowerShell 子进程运行该正控时，checker 暴露了此前被长套件前置 native 命令掩盖的同范围缺陷：严格模式下读取尚未初始化的 `$LASTEXITCODE`。修复不放宽 commit 绑定，而是直接要求两次 `git rev-parse` 输出分别满足有效短 token 与本地完整 commit token；命令失败或无效输出仍必然失败。

### R4：Chest trace 未把 ignored decompile 文本绑定到 exact DLL，且全文件 token 可产生方法体假阳性（P2）

脚本只校验相邻 raw Assembly hash，实际语义输入是 ignored、可修改的 ILSpy `.cs`；多数断言又在全文件找 token，无法证明 token 位于目标 owner/control flow。例如 `GetBackpackWithInsideBoxes(false)` 的绕过和 `TryCostInputItemsInInventory` 的 affordability-before-cost 顺序没有方法体边界。

有界修复：锁定现有 `Assembly-CSharp-decompiled-files.json` 的 `604943` 字节 / `3DE5EE...9EE7` identity 和 `3666` 行 exact tree，逐文件复核长度/hash；用 brace-aware 方法提取和 token 顺序锁住 inventory owner、四个 wrapper、FarmingGun false 分支、IRecipe transaction、RecipePanel/PackingPanel buffer 特例，并加入“token 位于 sibling method”反证。

### R5：DebugConsole Harmony owner fixture 越出受管测试会话（P2）

net48 子进程直接在系统临时目录创建固定 `DebugConsoleHarmonyOwnerFixture`，不清理且可能跨运行共享状态。本机审查时已留下空目录。

有界修复：子进程必须取得父级 `DTMAPI_TEST_SESSION_ROOT`，缺少就失败；所有运行根位于该受管会话的进程专属子目录，由父会话 receipt/lease/cleanup 统一处理。

### R6：Chest Review 漏记 RecipePanel dynamic buffer 特例（P3）

Review 原文只说 PackingPanel 把 buffer 纳入 max/首件语义。实际 RecipePanel dynamic dish 路径也对 `inventoriesAround.Append(dishItemBuffer)` 求 max，并在确认时从 `inventoriesAround` 扣 `count - 1`。当前 ChestLocator 追加集合仍连续参与，没有产品错误，但证据文字和 trace 覆盖不完整。

有界修复：Review 同时记录 RecipePanel 与 PackingPanel 的首件-buffer 特例；trace 在具体 `GetMaxCraftCountDefault`、`TryConfirmCraftDish`、`TryCostInputItemsInInventory` 方法体内锁住关系。

### R7：DebugConsole 天气能力诊断仍描述旧 mutation owner（P3）

实现已改为官方 `DolocAPI.Command_SetWeather(string,bool)` 并用 `LocalWeatherType` 回读，`GetStatus` 却仍称 `ArchiveDataHandle weather mutation`，会误导日志分析与后续审查。

有界修复：诊断同步为官方 room weather command + `LocalWeatherType` verification，不改变行为或公开 API。

## 4. 非发现与剩余边界

- Runtime `currentSourceBaseline=0.6.0 candidate` 与 `currentPublishedArtifact=0.5.5 Steam` 的当前内容正确，allowlist 刷新是 `+2` source / `+3` smoke 的纯增量。
- G2 当前历史对象正确：closure `9fb8d87d...`、直接父 `c79306df...`、contract blob `39cc17a4...`，后续 `runtime055Amendment` 未污染历史 blob。
- DebugConsole 未发现 P0/P1 或产品行为级新缺陷；24456188 policy、旧 policy inert history、隐藏 UI 和双 owner transaction 边界保持。
- ChestLocator 仍只完成静态审查，不能冒充 1.00 player verification；发布前人工农场/建筑行为确认仍 open。
- 本轮没有授权新 receipt/schema、公开 API breaking、旧 Compatibility 删除、产品范围扩张或游戏启动。

## 5. 关闭条件

R1–R7 完成上述有界修复并通过 PowerShell 5.1 syntax、双宿主 Chest trace、Author SDK required positive/negative、DebugConsole focused Unit、G2 authority path matrix、Runtime info hostile matrix、文档/制品治理后，本审核周期关闭。Review 保持 `recorded`；commit、完整验证和残余 player/release 风险只追加到 owning Update。
