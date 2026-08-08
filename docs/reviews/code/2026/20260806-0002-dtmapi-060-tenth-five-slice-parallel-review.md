# DTMAPI 0.6 第十次五切片并行审查

Date: 2026-08-06
Status: `recorded`
Audited commit: `132436f6`

Owning Update:

- [`20260802-0001`](../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)

## Scope

按用户规则，在第九次审核后的五个独立功能/发布切片完成后，同时启动三路
只读审查：

1. canonical Release 与废弃 `<game>/Mods` no-demand 隔离清理；
2. Candidate11 构建、Workshop 安装审计和本机 Runtime 安装；
3. 九项 ProductNative 候选与当前 Steam subscription 的上传差异；
4. Runtime 候选与当前 Steam publication 的差异及 published-tree authority；
5. Author SDK “新部署暂停、旧恢复保留”的实际分发 ZIP gate。

三路分别核对 Core/包语义、治理/权威边界和 runner/制品证据。审核期间没有
安装 Runtime、启动游戏、修改官方 `MODS`、启用状态、玩家存档或侧车。发现
及其有界修复属于本审核周期，不计入下一组功能切片。

## Findings

本轮没有 P0、P1 或 P3，合并后有一项 P2。

### R1 — P2：packaged Author SDK 暂停负例未进入可重复 Release 门

当前 ZIP 上执行 `deploy`、`update`、`install-local` 和
`source local select` 的四项黑盒调用，确实都会在读取 bogus package 或修改
fake game / Author state 前以 `SDK003` 退出 `1`；但该结论还只是本轮一次性
artifact evidence。`DTMAPI.AuthorSdk.Tests` 的暂停命令测试通过进程内
`RunPublic` 调用当前源码；绑定的外部 self-contained executable 只执行
Doctor/new/build。`test-author-sdk-portable.ps1` 也只覆盖 version、help、
new、validate、build、pack 和 Doctor；release structure checker 不执行 CLI。

因此源码实现与当前 ZIP 都正确，完整 Release 却不能防止未来误装陈旧
Author DLL 后仍通过。最小修复是在既有 portable gate 解包 ZIP 后直接执行四项
暂停命令，逐项要求 exit `1`、JSON diagnostic `SDK003`，并比较 fake game、
Author state、bogus package 的完整目录/文件身份不变。该门应复用现有临时根、
隔离环境和安全清理，不新增 receipt/schema 或第二套发布 authority。

## No-Finding Boundary

Core/包与治理两路没有发现：普通 Runtime 不创建或扫描旧 `<game>/Mods`，测试
seam 与旧部署 recover/withdraw 边界仍明确；安装器暂停检查早于 Runtime
transaction 创建和提交；官方来源仲裁仍是启用优先、唯一最高 priority
消歧，订阅快照只证明 Workshop 根。Candidate source9/local11、九产品
`74` 路径 / `32` 项变化 / 八项重传、Runtime strict-Ordinal published tree、
Author SDK ZIP 的文件/字节/hash 均由独立复算重现。路线图继续把 fixed-12、
ISSUE-011、实际 Steam publication 和 post-publication identity 保留为 pending。

## Resolution Boundary

本 Review 只保存独立审查发现。portable gate 的有界修复、验证和提交由 owning
Update `20260802-0001` 记录；R1 的修复不形成新的五切片周期。
