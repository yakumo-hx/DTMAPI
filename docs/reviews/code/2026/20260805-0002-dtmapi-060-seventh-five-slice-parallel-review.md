# DTMAPI 0.6.0 第七组五切片并行代码审查

## 记录信息

- 日期：`2026-08-05`
- 状态：`recorded`
- 性质：0.6.0 第七组五个独立功能切片后的并行代码审查
- Source：用户要求每完成五个独立功能切片执行一次并行子智能体审核；本轮覆盖 Author SDK 新部署暂停、官方双来源仲裁、MoreSaves 官方角色迁移、本机旧包诊断、旧 `<game>/Mods` Runtime 投影和安装器前置失败边界
- Implementation owner：[DTMAPI 0.6.0 唯一权威路线图](../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)
- 审查提交：`e1fd3525`

本 Review 只保存第七轮并行审查发现、根因和有界关闭条件。修复生命周期、changed files、验证、实际本机部署和 0.6 发布状态仍由 owning Update 维护；本轮发现及其有界修复属于同一审核周期，不计入下一组五个功能切片。

## 1. 审查范围与方法

三名独立审查者分别检查：

1. Core `RuntimePaths`、生产/测试发现边界、启动生命周期和实现程序集兼容；
2. PowerShell 安装事务、trap、双宿主空/非空产品计划和零变更失败；
3. Author SDK README/Tests/release checker、文档治理、MoreSaves 本机 package authority 与 ChestLocator 证据范围。

并行只读审查去重得到 `2×P1 + 1×P2 + 2×P3`，没有 P0 或全局阻断。审查者没有编辑仓库、安装 Runtime/产品、启动游戏、写玩家存档/Steam/官方 `MODS`，也没有获取共享 Runtime lock。

## 2. 发布阻断发现

### R1：Author SDK Tests 与 README 已收窄，正式 release checker 仍锁定旧句（P1）

`tests/DTMAPI.AuthorSdk.Tests/Program.cs` 已要求“只有 legacy recovery/withdraw 的当前 journal write 使用 schema 3”，但 `tools/scripts/check-author-sdk-release.ps1` 仍要求被删除的“所有新 deployment journal/write 使用 schema 3”。`build-author-sdk.ps1` 必然调用该 checker，故普通 Author SDK Tests 可以通过，而正式 SDK ZIP 和 MoreSaves builder 确定性失败。

有界关闭条件：checker 用动态 journal schema version 改绑 README 当前 legacy 句，错误文案同步；Author SDK Tests 同时锁住 README/checker 新句存在与旧句不存在；验收必须执行真实 Author SDK release checker 和 MoreSaves Advanced builder，不能只重复 Unit runner。

### R2：`SkipOfficialLocalMods` 空计划被 PowerShell 折叠成 `$null`（P1）

`install-to-game.ps1` 把 `$officialLocalMods` 赋为整个 `if` 的 success stream。即便 true branch 写了内部 `@()`，PowerShell 7 与 Windows PowerShell 5.1 都会把零输出折叠成 `$null`，随后 mandatory `Definitions` 参数绑定失败。package payload 会自动打开该开关，因此玩家 Runtime 包和指南推荐的 Runtime-only 刷新都在任何 Runtime 写入前退出 `1` 并写 failure state。新增的 paused-product 负例总选择非空 managed 计划，未覆盖这个空计划正例。

有界关闭条件：先显式初始化真正的空数组，只在未跳过产品时赋入选择结果；计划与暂停检查仍须位于 Runtime commit 前。完整双宿主 Runtime upgrade transaction 必须同时通过 source Runtime-only 和 package-payload 路径，开发官方目录事务继续通过 managed pause 零变更负例。

## 3. 发布边界与文档发现

### R3：本机 MoreSaves 安装计划与此前 disposable-first 后续门冲突（P2）

Manual QA 与 ISSUE-019 已把下一步路由到 exact Local 替换和用户冷启动，但 owning Update 仍把旧 focused package 明确标为非 candidate/release authority，并要求恢复后先做 disposable `ArchiveMutation`。最新用户已明确授权“安装最新 DTMAPI 后由用户手测”，所以可以只修订这个本机执行顺序，不能静默把旧验证包提升为发布件，或把持久安装/用户启动冒充 Codex isolated acceptance。

有界关闭条件：owning Update 明确一次 local Manual-QA deployment；同一 clean commit 重建 Runtime 和 SDK 生成的 `MoreSaves 1.0.1 / minimum 0.6.0` 包并通过真实 checker；共享锁、无游戏进程、Runtime-only 安装、官方 `MODS` 外同卷 staging、完整 before-image、collision-failing move 和失败恢复。两个现存 `mod_infos.json`、Workshop tree、SAVE/sidecar、其他 Local 产品及旧 `<game>/Mods` 前后必须零变化。Codex 不启动游戏；用户后续反馈只按真实范围进入 Manual QA/ISSUE，不形成 smoke/Release/publication 结论。

### R4：Author SDK README 首屏仍把暂停命令写成普通示例（P3）

README 的首个 Commands 代码块直接列出 `deploy` 与 `update`，直到后文才说明二者返回 `SDK003`。命令本身已 fail-before-mutation，但这仍会误导作者并与同文已标注的 `source local select` 不一致。

有界关闭条件：首屏示例原位标注 `paused: SDK003` 或移到 legacy recovery 边界；README contract test 要求每个首屏 deploy/update 示例都明确标注暂停。

### R5：Manual QA Review 多一个 EOF 空行（P3）

`docs/reviews/manual-qa/2026/20260805-0003-release-blockers-moresaves-live-package.md` 在提交中以两个 LF 结尾，`git diff --check e1fd3525^ e1fd3525` 报 `new blank line at EOF`。

有界关闭条件：只删除多余空行，并让最终 `git diff --check` 通过；不借格式修复改写 Review 的五项问题顺序或前实现事实。

## 4. 已核对的非问题

- `RuntimePaths` 属于 `DTMAPI.Core` 实现面，不是 `DTMAPI.Abstractions` 作者契约；仓库和已知受管产品没有 `ModsPath` 二进制消费者，共置 Runtime assembly 由同一事务更新，因此本轮重命名不是受支持公共 API breaking change。
- 生产 Host 不能启用 legacy-development test marker；既有测试也覆盖了预存 `<game>/Mods` 不能进入普通玩家发现。未发现新的官方 Local/Workshop 仲裁回归或启动日志句柄泄漏。
- managed-product pause 的选择、检查和 trap/failure-state suppression 在非空计划上位于共享写入之前；R2 只修正空计划形状，不授权恢复任何暂停产品部署入口。
- `install-dev-preview` 已清楚区分 Runtime-only、paused modes 与 legacy maintenance。ChestLocator 只陈述用户实际确认的远端箱子识别/真实扣料，MoreSaves 旧 `1.0.0` 冷启动也未被冒充为 `1.0.1` 玩家证据。

## 5. 关闭条件

R1--R5 的有界修复须通过完整 Author SDK Tests、真实 SDK release checker/MoreSaves builder、PowerShell 7 与 Windows PowerShell 5.1 Runtime upgrade/developer official transaction、完整 Unit、Catalog、test-artifact/document governance 和 `git diff --check`。实际本机 Runtime/MoreSaves 部署只能在修复后的 clean commit 和上述 R3 事务边界下进行。Review 始终保持 `recorded`；所有 PASS、package hash、安装前后回执和剩余阻断只写入 owning Update/对应 Issue。
