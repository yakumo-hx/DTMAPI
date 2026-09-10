# 20260729-0001 DTMAPI 0.5.5 发布前修正独立复核

**Date:** 2026-07-29
**Status:** recorded — `READY-FOR-FINAL-RELEASE` 仍不接受
**Reviewed HEAD:** `fb0d132e3a3d4d2fc79bfc2a8c5a08f2af623446`
**Reviewed range:** `1f51eaf6..fb0d132e`
**Source audit:** `docs/reviews/code/2026/20260728-0004-dtmapi-055-prerelease-route-completion-audit.md`
**Owning Update:** `docs/updates/2026/20260727-0001-dtmapi-055-prerelease-route.md`
**Roadmap:** `docs/planning/20260727-dtmapi-055-prerelease-roadmap.md`

## 范围与结论

本审计独立复核完成度审计所列三个 P1、四个 P2 的修正，以及修正后重新
冻结的 Runtime、Author SDK、外部消费者、Manbo、no-demand 和 active-GC
证据。本轮没有运行完整 Release、Workshop 玩家包终审、Steam 操作、历史
L0–L5、长测或新的游戏进程。

结论为：

- `P0=0`；
- `P1=1`；
- `P2=4`；
- 原 P1-1 的损坏快照语义校验、原 P1-2 的证据保留和原 P1-3 的三份外部
  消费者真实加载均已直接修复；
- 原 P2-2、P2-3、P2-4 已关闭，原 P2-1 的写后对账已修复；
- 但 Author SDK 的 schema-3 恢复仍会覆盖 marker 形成后另一产品合法产生的
  `source-state` 变更，因此候选必须保持 `implemented / provisional`；
- 该 P1 只影响 Author SDK。当前 Runtime、十个产品包以及
  `c7e1ec2f3697` 上的四组聚焦游戏证据不需要重建或重跑。

## P1：pending localInstall 没有形成 game-root 级 source mutation barrier

### 代码事实

`src/DTMAPI.AuthorSdk/SourceStateService.cs:21-38` 的 `source` 命令只在本次
进程内取得 `GameOperationLock`。锁在命令结束后释放，不表示某个
`localInstall` 的持久事务仍在占用全局 `source-state.json`。

当产品 A 的 `localInstall` 留下 schema-3 `recovery-required` marker 后：

- `SourceStateService` 的 local/workshop/reproduction 写命令不会检查其他
  deployment journal 中的 pending `localInstall`；
- `DeploymentService.InstallLocal` 在
  `src/DTMAPI.AuthorSdk/DeploymentService.cs:198-205` 只拒绝当前目标产品的
  pending marker，另一产品仍可开始新的 composite transaction；
- `RecoverLocalInstall` 最终通过
  `RollbackLocalInstall`（同文件 `1591-1615`）无条件恢复整份
  `sourceBefore`，没有证明当前 `source-state` 仍是该事务允许覆盖的前态或
  精确中间态。

因此，内存锁保证了“同时只有一条命令”，却没有保证“未恢复事务期间没有
后续合法写入”。后续写入可以成功，随后又被旧事务恢复静默抹除。

### 冻结 SDK 实际复现

复现直接使用当前冻结包：

- 路径：
  `temp/prerelease-step5-author-sdk/DTMAPI-Author-SDK-0.1.0-win-x64.zip`；
- 文件数：389；
- 长度：126,954,799；
- SHA-256：
  `723f89f5a1a966ab60dd2f63b649c774ea0d5db7a07129bde0561262046115c9`。

在受管临时 fake-game、独立 `DTMAPI_AUTHOR_STATE_ROOT` 和 SDK 自建
ContentPack 中执行：

```text
seed install-local Tests.Deployment          -> success
source workshop prepare Tests.Other          -> success
crash:install-local.after-prepare             -> recovery-required
source workshop prepare Tests.Later           -> success / WorkshopValidation
recover Tests.Deployment                      -> success / recovered=true
source status Tests.Later                     -> PlayerWorkshop
```

`Tests.Later` 在 recover 前确实为 `WorkshopValidation`，recover 后却回到
`PlayerWorkshop`；整份 source-state hash 也从
`0EFE2744...1EF92` 变回 `70BB5472...E857`。这证明发生了真实的跨产品合法
状态丢失，而不是损坏输入被 fail-closed。

临时 fixture 已按 test-artifact retention 协议清理；没有操作游戏目录、玩家
存档或 Steam。

### 最小修正边界

复用现有 deployment journal，不新建 lease、receipt、schema 或第二套恢复
权威：

1. 在同一 game-root lock 内扫描并语义验证全部 deployment journal；
2. 任一 `localInstall` pending 时，允许 `source status`、
   `install-local-status` 和正确目标的显式 recover；
3. 所有 source-state 写操作以及另一产品的 `install-local` 必须在任何写入、
   解包、移动或 orphan cleanup 前 fail-closed；
4. recover 写回 `sourceBefore` 前，还应证明当前 source state 是该事务的精确
   前态或可由 marker 推导的精确事务后态；未知漂移必须保留 marker 并拒绝
   覆盖；
5. 多个历史 pending marker 无法证明安全顺序时，不得猜测恢复顺序。

最低回归矩阵：

- `crash A -> source local/workshop/reproduction mutation B`：B 被拒绝且字节不变；
- `crash A -> install-local B`：B 在任何部署/source 变化前被拒绝；
- pending 期间各只读 status 可用；
- `recover A` 仍恢复精确前态并清除自己的 marker；
- 外部或旧工具制造未知 source-state 漂移后，recover 拒绝覆盖且 marker 可重试；
- pending A 的 staging 不会被下一次 orphan cleanup 删除。

该修正只需 Author SDK 聚焦 Unit、PowerShell 7/Windows PowerShell 5.1
release/事务检查和 SDK ZIP 重冻结。它不要求启动游戏、重跑 GC、Manbo、
外部消费者、no-demand 或完整 Release。

## P2-1：orphan cleanup 的当前记录不是实现真相

Owning Update 写成“删除同一事务产生的 bounded pre-marker orphan”，但
`CleanupUnreferencedLocalInstallArtifacts`
（`src/DTMAPI.AuthorSdk/DeploymentService.cs:981-1069`）实际在每次
`install-local` 开始时：

1. 先读取并验证全部 deployment journal；
2. 删除 SDK-owned `install-inputs` 下全部 32-hex `.zip`；
3. 删除 SDK-owned staging 下全部 32-hex、且未被任一有效
   `LocalInstall`、`Active` 或 `RecoveryArtifact` 引用的目录。

没有发现它误删当前恢复所需材料：有效 pending stage 会进入 protected set，
任一 journal 不可读或语义无效都会在删除循环前失败，恢复本身也不依赖
immutable input ZIP。因此这是文档真相和保护矩阵缺口，不是第二个 P1。

应把记录改为“在全 journal fail-closed preflight 后，清理 SDK-owned roots
内 transaction-shaped、unreferenced 的 pre-marker artifacts”，并补一项
pending stage 保留测试。

## P2-2：Core 嵌入了完整 Catalog，而不是最小派生投影

`src/DTMAPI.Core/DTMAPI.Core.csproj:14` 把完整的
`tools/release/dtmapi-product-catalog.json` 作为资源嵌入 Core：

- 当前 Catalog 为 77,430 字节；
- 三个 legacy admission 所需的最小数据约 1,091 字节；
- Core 相对前一候选增加 86,016 字节；
- Catalog 中其余产品的 QA、延期、source root、evidence 和发布字段均不参与
  Runtime 判定；
- 以后任何无关 Catalog 文本变化也会改写 Core 字节和候选 hash。

这不是每帧或游戏内热路径，也没有发现秘密或绝对私有路径；当前 Runtime
准入安全性不因此失效。为避免再次重冻 0.5.5 Runtime，本项可明确延期到
0.5.5 后：仍由唯一 canonical Catalog 生成并校验最小 Runtime projection，
不要手写第二份权威。

## P2-3：legacy native 例外的生命周期诊断需更精确

三个精确例外仍以 `Strict CodeMod` 显示。实际分类同时记录
`legacy-external-catalog-verified`、native risk 和 restart policy，且
`MarkManagedAssemblyLoadAttempt` 会在停用后阻止同进程重进，因此安全上是
fail-closed。

但 Runtime snapshot 会使用 `inactive / restart-required`。DTMAPI 只能证明
自己的 platform roots 已清理，不能证明旧 DLL 自行安装的 Harmony/native
效果已经卸载；这些效果可能持续到进程退出。后续版本应提供明确的
legacy-exact/native-compat 诊断，或在 restart 提示中直说这一限制，避免把
“DTMAPI owner inactive”理解成“第三方 native/Harmony 已热卸载”。

该诊断改动会改变 Core，因此不应为一个非阻断措辞项再次使 0.5.5 Runtime
候选和四组游戏证据失效；本轮先记录为已知限制。

## P2-4：Catalog 的 title/exit cleanup 措辞超过证据

三个 `legacyNativeAdmission` 行的 `gateEvidence` 写成
`title/exit cleanup`。`GAME-SMOKE/20260728-230231` 实际证明的是：

- 三个精确 Workshop 输入各加载一次并完成 Entry；
- 各自的 monitor、config read 或 GMCM registration 行为成立；
- 订阅树、玩家 current/prev/bak 与 committed sidecar 不变；
- 游戏返回标题并自然退出。

它没有证明三个旧 DLL 自行安装的 Harmony patch 被逐 owner unpatch。当前
准入本身仍成立，但后续维护 Catalog 时应收窄为“title return + natural
process exit”，并明确不主张 legacy Harmony cleanup。为避免仅因内部证据
文字改变嵌入资源和候选 hash，本项与 P2-2/P2-3 一并排入 0.5.5 后的最小
projection/诊断整理。

## 已接受的修正与独立验证

### Author SDK 已成立的部分

- 两份恢复快照现在会经过严格 UTF-8、精确 JSON 字段、game root、UniqueID、
  package kind、previous record 和 source selection 语义校验；
- 空快照、`{}`、错误 root/product、非法 selection 都会在任何移动或写回前
  fail-closed；
- prepare 写后异常会只读对账，不再把已发布 marker 报成“无 durable
  mutation”；
- `DTMAPI.AuthorSdk.Tests` 通过；
- PowerShell 7 release check 与 Windows PowerShell 5.1
  contract/structure check 通过。

这些结果关闭原 P1-1 的直接缺陷，但普通 green suite 没有在 crash marker
与 recover 之间插入另一产品的 source mutation，所以没有覆盖本轮新 P1。

### 证据保留与路线真相

- schema 5 已纳入 `PRERELEASE-ACTIVE-GC`；
- no-demand r3/r4 与 active-GC r14/r15 均进入 durable roots；
- allowlist `-Check` 与聚焦 retention test 通过：
  483 source files、878 smoke runs、62 Runtime identities、19 durable roots；
- PowerShell 7 和 Windows PowerShell 5.1 retention 聚焦测试通过；
- 文档治理 6,031 项通过；
- Changed Files、no-demand 证据边界和完整 Release 临时包/冻结候选关系已
  纠正。

### 三份外部消费者与 Runtime 候选

精确 legacy admission 是 Catalog 数据驱动，不是在 Runtime 方法体中硬编码
三个 ID。它要求 native-verified Workshop 来源、精确 Workshop ID、UniqueID、
省略 `CodeModKind`、入口相对路径和入口 SHA；加载前后再次验证
SHA/MVID/实际位置。Local、未验证来源、错误 ID/path/hash、显式 Strict 和
额外 native DLL 均保持拒绝。

`GAME-SMOKE/20260728-230231` 的三个消费者各有一条 Workshop load source、
一次 Entry 和一项 scoped provider 行为，订阅树与 NoNativeSave 输入不变。
Catalog checker、相关 Unit、runner ValidateOnly 及候选 DLL/证据逐 hash 比对
通过。

Runtime 冻结目录独立重算仍为：

- 31 files / 71,533,167 bytes；
- retained-artifact tree
  `f0dd2c5651b8e76e7da207f938aaa4c32f629f8a075806f9ceeac50d8cbf81b2`；
- `DTMAPI-FileTree-SHA256-v1`
  `3f634965ac15af0563abb16202a3141a3d3a4ab01c2c3b9649d8e73ec285f154`；
- `DTMAPI-CandidateStructure-SHA256-v1`
  `c032f22c8f31475a027b24a98622870049b1e2c64a77ef12e0383dc4e1a93a02`；
- manifest BuildCommit `c7e1ec2f3697`。

Catalog check、evidence-retention test、allowlist `-Check`、文档治理和当前
`git diff --check` 均通过。

## 下一入口

当前不要运行完整 Release，也不要重跑游戏矩阵。下一实现只应：

1. 修正 Author SDK 的 pending-localInstall 全局 source mutation barrier 和
   recover 写回前状态证明；
2. 补本 Review 的跨产品/未知漂移/pending-stage 聚焦矩阵；
3. 修正 orphan cleanup 记录；
4. 通过 Author SDK Unit、双 PowerShell host 检查并重冻唯一 SDK ZIP；
5. 再对该小修正做一次独立聚焦复核。

若该复核为 `P0=0 / P1=0`，可以保持现有 Runtime、产品包和游戏证据不变，把
owning Update 提升为 `verified / READY-FOR-FINAL-RELEASE`。随后才进入一次
完整 Release 和精确冻结玩家包终审；Steam 上传仍需用户另行授权。

## Resolution

- 2026-07-29：`c32a274f` 已实现本 Review 限定的 game-root 写入屏障、
  recovery 前精确状态证明和聚焦 Unit；Author SDK 已经双 PowerShell host
  检查并单独重冻。实现与证据记录在
  [20260727-0001 Update](../../../updates/2026/20260727-0001-dtmapi-055-prerelease-route.md)。
  本 Review 要求的独立聚焦复核尚未执行，因此 owning Update 仍为
  `implemented / provisional`。
