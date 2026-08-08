# 0.6 发布阻断与 MoreSaves 本机包复查

- Review ID: `20260805-0003`
- Date: `2026-08-05`
- Status: `recorded`
- Scope: Author SDK tests, developer installer preflight, retired `<game>/Mods` projection, MoreSaves official-Local package identity, ChestLocator manual acceptance
- Source: user manual-test and code-review feedback
- Owning Update: [20260802-0001 DTMAPI 0.6.0 authority roadmap](../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)
- Related issues: [ISSUE-019](../../../debug/issues/ISSUE-019-20260804-moresaves-100-legacy-slot-migration.md), [ISSUE-020](../../../debug/issues/ISSUE-020-20260805-enabled-official-source-arbitration.md)
- Prior review: [20260805-0002 official source arbitration](20260805-0002-enabled-official-source-arbitration.md)

本记录按用户反馈顺序冻结本轮修改前事实和验收边界。完成状态、变更文件、测试结果和安装证据只写入 owning Update；本 Review 不冒充修复证明。

## 问题 1：完整 Author SDK Tests 当前失败

### 原始反馈

- README 已改成“仅旧恢复/撤回写 schema 3”，测试仍要求旧句子“所有新 deployment journal 都是 schema 3”。
- 因此路线图中“完整 Author SDK Tests 通过”的当前可复现性失效。
- 图片转写：无截图。

### 审查记录

- 用户确认事实：当前分支的完整 Author SDK Tests 失败，是 0.6 发布阻断。
- 代码/文档事实：`tests/DTMAPI.AuthorSdk.Tests/Program.cs:193-199` 精确查找已删除的 `New deployment journals and every journal write use schema 3.`；`author-sdk/README.md:68-70` 当前只承诺旧部署的 recovery/withdraw 可写 schema 3，公开新建/更新部署已由 `SDK003` 暂停。
- 复现事实：当前 `8d6fe5b8` 的完整测试在 `TestPublishedDeploymentJournalContract` 同一断言抛出 `InvalidOperationException`；受管测试会话随后正常清理。
- Codex 推断：README 是当前产品边界，测试契约陈旧；恢复旧 README 句子会重新声称一个已经暂停的新部署能力。
- 反证/未证实：这次失败发生在 README 契约断言，尚不能据此判断后续 Author SDK 测试是否还有第二个失败。
- 归属：Author SDK 发布契约与测试。
- 验收点：测试改为锁定“只有 legacy recovery/withdraw 的当前 journal write 使用 schema 3”以及 receipt/marker 仍为 schema 2；完整 Author SDK Tests 必须从头退出 `0`。
- blocker 判定：发布阻断，必须在本轮关闭。

## 问题 2：安装指南陈旧且暂停检查发生在 Runtime commit 之后

### 原始反馈

- `install-dev-preview.md` 仍指导开发者运行 `-InstallPublishedModsOnly` / `-InstallAllDevOfficialMods`，并声称 Advanced 产品继续由 SDK receipt 部署；这些入口现在会失败。
- 实际安装器先提交 Runtime，再检查暂停产品并报错，形成“命令退出失败，但 Runtime 已更新”的部分成功。
- 至少更新指南；更稳妥的是把确定性的暂停检查移到任何 Runtime 写入之前。
- 图片转写：无截图。

### 审查记录

- 用户确认事实：公开说明和实际失败边界冲突；部分成功不可接受。
- 代码/文档事实：`install-to-game.ps1:2436-2444` 先完成 Runtime transaction；`2457-2459` 才选择产品并调用 `Assert-DtmApiNoPausedAuthorSdkInstall`。相同检查在 dry-run 路径已经发生于任何 Runtime write 之前，证明产品集合和暂停结论可提前确定。指南仍把两个会选择 managed Advanced 产品的模式写成可用安装路径。
- Codex 推断：应在产品定义读取完成后、BepInEx/Runtime candidate 或 live state 变化前计算一次计划并 fail-closed；提交后不能再次重选另一套集合。
- 反证/未证实：`-SkipOfficialLocalMods` 的 Runtime-only 安装不触发该暂停项，仍是有效开发刷新入口；显式 `-LegacyOfficialLocalOnly` 是维护/测试边界，不应被包装成 Advanced 产品安装替代品。
- 归属：Runtime installer transaction boundary、开发预览指南、installer focused tests。
- 验收点：两个暂停模式在临时有效 game-shaped 目录上退出非零，且 Runtime/BepInEx/state 树前后完全相同；Runtime-only 模式继续成功；指南不再建议暂停命令安装产品。
- blocker 判定：发布阻断，必须在本轮关闭。

## 问题 3：旧 `<game>/Mods` 语义仍由规范和 Runtime 投影

### 原始反馈

- `PROJECT.md` 仍写 Advanced CodeMod 从 DTMAPI `Mods/` 发现。
- Runtime 每次启动仍创建并记录 `<game>/Mods`；虽然它没有重新进入生产扫描，但仍制造“第三来源”的假象。
- 图片转写：无截图。

### 审查记录

- 用户确认事实：要求清理的是当前玩家语义，不是否认旧 recovery/withdraw 证据。
- 代码/文档事实：`PROJECT.md` 的 Advanced 定义仍使用旧发现位置；`RuntimePaths.Ensure()` 创建 `ModsPath`，`DtmApiRuntime.Start()` 输出 `ModsPath = ...`。生产 Bootstrap 不实现 `ILegacyDevelopmentModSourceTestHost`，因此 `ManifestReader` 只在明确 Unit fixture 中读取该路径。
- Codex 推断：无需删除 Author SDK 旧 journal 引擎；应删除普通启动的目录创建和日志，并把 Core 属性明确降为测试用 legacy path 或移除旧名称。规范改为官方 Local/当前订阅 Workshop 双来源。
- 反证/未证实：当前玩家症状不是第三根被加载；本轮不重新设计 test fixture 或 InstallDoctor 对旧错误部署的只读诊断。
- 归属：Core Runtime path projection、project identity contract、Unit source-boundary fixture。
- 验收点：普通 `RuntimePaths.Ensure()` 不创建 `<game>/Mods`，启动日志不再输出它，生产扫描仍保持双来源；test-only legacy fixture 仍可显式构造旧根反证。
- blocker 判定：当前语义一致性阻断，必须在本轮关闭。

## 问题 4：MoreSaves 显示 12 槽但读不到六个旧额外存档

### 原始反馈

- 进入游戏后 MoreSaves UI 已加载，但六个额外存档不可读。
- 用户询问是否没有把新版安装到官方上传目录。
- 图片转写：无截图。

### 审查记录

- 用户确认事实：本次真实冷启动能看到 MoreSaves UI 扩为 12，但额外六槽仍为空。
- 现场事实：官方 Local `MODS/DTMAPI_MoreSaves` 与 Workshop `3742763050` 都是旧 `1.0.0 / Minimum 0.5.5`，入口 DLL 均为 `17,408` bytes、SHA-256 `09FE3D96...B9A3F`。官方状态为 Local enabled/priority `8`，Workshop disabled/priority `-1`。
- 日志事实：Core 正确选择 Local 并忽略禁用 Workshop；旧产品只记录 `archiveFileCount 6->12`，没有任何 1.00 legacy-role migration 记录。
- 源码事实：当前产品 manifest 是 `1.0.1 / Minimum 0.6.0`；ISSUE-019 的已验证当前入口 DLL 为 `90EBC85A...C0EC2`。
- Codex 推断：本次现象由旧产品包被正确加载造成，不是 ISSUE-020 仲裁复发，也不是新版迁移器执行失败。新版确实没有安装到官方 Local 上传目录。
- 反证/未证实：没有运行当前 `1.0.1` 玩家进程，因此不能声称轻量迁移已经在真实 live SAVE 完成；旧 UI 成功不能验证新包。
- 归属：MoreSaves ProductNative package/deployment；ISSUE-019 保持 reopened/pending player revalidation。
- 数据分类与官方提交边界：这是启动时存档文件名迁移，属于 `ArchiveMutation`；Codex 不以 live Steam AutoCloud 启动作为自动验收。可在共享锁内安装精确官方 Local 包并保留可恢复的旧包备份，由用户随后冷启动手测。
- 验收点：构建/验证当前 `1.0.1` exact package；在不改变官方 enabled/priority 的前提下事务性替换 Local 包；安装后 manifest/DLL/hash/来源状态精确；下一次用户冷启动日志必须出现当前迁移器结果，UI #7--#12 显示并能加载旧额外槽。
- blocker 判定：包部署阻断已定位；源码是否通过真实玩家验收仍待用户下一轮确认。

## 问题 5：ChestLocator 人工确认通过

### 原始反馈

- `ChestLocator` 人工确认通过；远端箱子可以识别并正常扣除物品。
- 图片转写：无截图。

### 审查记录

- 用户确认事实：玩家真实流程已经确认远端容器参与材料识别和实际扣料。
- 代码/文档事实：静态 native trace 已证明 Case/Shelf、count/max/actual-cost 使用同一扩展 inventory 数组并锁定非共享容器排除；此前只缺玩家人工门。
- Codex 推断：可以关闭路线图中 ChestLocator 的人工确认阻断，但玩家证据描述必须保持为用户实际说出的“远端箱子识别和真实扣料”；其余 caller/容器/最大数量/排除细节仍引用静态 trace，不伪造为逐项人工操作。
- 反证/未证实：本轮没有新的 Codex game smoke、截图或独立日志收据。
- 归属：用户 Manual QA 由本记录拥有；发布生命周期投影由 owning Update 拥有。
- 验收点：路线图将人工门标为 user-verified，并精确保留证据范围；不新增 smoke-matrix 行。
- blocker 判定：用户人工门已通过，不再阻断本轮。

## 2026-08-06 问题 4 后续观察（append-only）

- 玩家随后实际冷启动了已部署的 Local `1.0.1`。DTMAPI 日志明确选择
  enabled Local priority `8`、忽略 disabled Workshop，并从
  `MODS\DTMAPI_MoreSaves` 加载当前 entry DLL。
- 当前迁移器报告 `legacySources=16, moved=16,
  destinationsPreserved=0`，随后把官方 archive count 从 `6` 公布为
  `12`，产品事务与 Drift 激活均成功。
- Codex 只读复核迁移后文件状态：18 个旧角色名全部不存在；迁移前实际
  存在的 16 个角色都位于对应官方 current/`.prev0`/`.bak` 名称，长度与
  SHA-256 和既有迁移前回执逐项一致；原本缺失的 index-10 prev 与
  index-11 bak 仍缺失。异常 index-10 old bak 已按用户决定成为
  `doloc-save-10.data.bak`。
- 同一进程随后原生加载 `slot=7`，协调器以
  `nativeEnter/nativeReturn/saveLoaded=1/1/1`、零异常和零 timeout 关闭。
  因而此前“新版未装到上传目录、额外槽全部不可读”的根因和现场症状都已
  得到反证；至少一个真实额外槽已读入。
- 本次没有自动化 UI #7--#12 六槽逐一加载或 harness clean-exit/fatal-window
  回执。它是玩家持久环境的手测/日志证据，不替代 disposable
  `ArchiveMutation` 发布验收，也不新增 smoke-matrix 行。

## 2026-08-06 问题 6：Y 键控制台新包未持久发布到官方 Local（append-only）

### 原始反馈

- “Y键控制台新版本没有加到上传目录。现在显示报错。”
- 图片转写：无截图。

### 审查记录

- 用户确认事实：当前手测环境中的 Y 键控制台显示错误；用户指出新的产品包没有留在官方上传目录。
- 日志观察：`2026-08-06 13:49:39` 的当前 `BepInEx/LogOutput.log` 明确从启用的 `Local.DTMAPI_YKeyConsole`、priority `7` 和 `MODS/DTMAPI_YKeyConsole` 加载。其旧 DLL 在 `DebugConsoleHookInstaller.ResolveTargets()` 中抛出 `System.InvalidOperationException: Sequence contains no matching element`；DTMAPI 随后完整回滚该 owner，记录 `Failed to load DTMAPI.DebugConsoleMod` 与 `restartRequired=True`。
- 包字节事实：现场 Local 树为 `11` 文件 / `478,488` bytes，入口 DLL `175,616` bytes / SHA-256 `102E616C...6180FF`，reference policy 为 `doloctown-23762374-debugconsole-v1`，minimum Runtime `0.5.5`，并带 Workshop-only `workshop.json`。当前冻结 `566467f0-local11` 候选树为 `10` 文件 / `479,345` bytes，入口 DLL `176,128` bytes / SHA-256 `2869BFD9...A252F`，reference policy 为 `doloctown-24456188-debugconsole-v1`，minimum Runtime `0.6.0`，不含 `workshop.json`。
- 当前环境事实：游戏 build 为 `24585411`，DTMAPI Runtime `0.6.0` 的 Player Doctor 启动状态为 `5 / 0 / 0`；官方状态已正确选择 Local 并忽略禁用 Workshop，所以这不是 Runtime 安装失败或 ISSUE-020 enabled-first 仲裁复发。
- Codex 推断：最终 Candidate11 r6 为了证明恢复合同，在成功 smoke 后按设计把原 Local 产品树恢复为运行前字节；此前持久手测准备只单独更新过 MoreSaves，没有把当前 DebugConsole 候选留在上传目录。本次报错由旧产品包与当前原生 Hook 面不兼容直接造成，不是新候选已经加载后失败。
- 反证/未证实：当前 `24456188` 候选曾在 r6 exact Local11 中通过，但该运行早于本次观测到的 game build `24585411`；静态包身份不能代替用户下一次冷启动对新 build 的真实 Hook 安装与 Y 控制台行为确认。
- 归属：Y-Key Console ProductNative 官方 Local package/deployment；现有 Runtime、双来源选择规则和存档不需要修改。
- 验收点：在共享 Runtime lock 内，把 exact `566467f0-local11/DTMAPI-YKeyConsole` 事务性发布为 `MODS/DTMAPI_YKeyConsole`；旧树保留到独立 backup；`SAVE/mod_infos.json`、完整 SAVE 树、其他 Local 产品和 Workshop 订阅均不变。安装后逐文件与候选 exact，Local 继续 enabled/priority `7`。下一次用户冷启动必须从该 exact Local 根加载、`Entry`/Hook 事务成功、无上述 `ResolveTargets` 异常，存档内 Y 可打开并正常关闭。
- blocker 判定：本机手测部署阻断已定位，可通过有界包替换关闭；当前 game build `24585411` 上的产品行为仍待用户冷启动确认。

## 2026-08-06 问题 6 后续确认（append-only）

- 用户在 exact 新 Local 包安装后的冷启动中确认 `DebugConsole` 手测正常；
  Y 控制台可按当前产品语义使用，旧包的 `ResolveTargets` 报错不再是当前
  现场症状。
- 同轮用户也确认 `MoreSaves` 与 `AutoFishing` 正常。完整反馈顺序、五项
  沿用 smoke 的产品以及 MoreEquipmentSlots 新发现由
  [20260806-0001](20260806-0001-moreequipment-official-slot-growth-review.md)
  统一保存。
- 该结果关闭本 Review 问题 6 的玩家冷启动门；它仍是用户 Manual QA，
  不新增自动 smoke 行，也不改变旧 Local 包故障的历史证据。
