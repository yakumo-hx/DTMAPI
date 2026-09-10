# 20260728-0004 DTMAPI 0.5.5 发布前路线完成度独立审计

**Date:** 2026-07-28  
**Status:** recorded — `READY-FOR-FINAL-RELEASE` 暂不接受  
**Reviewed HEAD:** `1f51eaf61fff64e223c2888e152a553ff5138e21`  
**Owning Update:** `docs/updates/2026/20260727-0001-dtmapi-055-prerelease-route.md`  
**Roadmap:** `docs/planning/20260727-dtmapi-055-prerelease-roadmap.md`

## 范围与结论

本审计复核发布前路线步骤 1–7、冻结候选、回滚输入、Author SDK、
Manbo/no-demand/active-GC 证据、发布波次和下一阶段入口。它没有运行完整
Release、Workshop 玩家包终审、Steam 上传、L0–L5、长测或新的游戏进程。

结论为：

- `P0=0`；
- `P1=3`；
- `P2=4`；
- Runtime 和十个后续发布产品的冻结字节没有发现漂移；
- Manbo、no-demand 和 active-GC 的既有运行证据仍可用；
- 但当前不能维持 `verified / READY-FOR-FINAL-RELEASE`，应在最小修正及独立
  复核前回到 `implemented / provisional`；
- 现在启动完整 Release 会遇到一个已知、可预先修掉的确定性门禁失败。

三个 P1 分别是 Author SDK 损坏快照可被当成有效恢复前态、证据保留
allowlist 已过期，以及三份公开外部消费者仍没有完成已冻结的真实加载门。

## P1-1：Author SDK 会把语义无效的快照写回并报告恢复成功

`src/DTMAPI.AuthorSdk/DeploymentService.cs:953-992` 对 schema-3
`localInstall` 的两份前态快照只执行 Base64 解码。
`ExactFileSnapshot.FromBase64`（同文件 `1555-1561`）允许
`Existed=true` 配空字节，也不解析解码结果是否为对应 game root、UniqueID、
destination、previous 和 source selection 的有效旧权威。

恢复随后在同文件 `1418-1422` 和 `1438-1442` 写回这些字节，并只检查
“当前文件等于刚写入的字节”。因此字节相等被错误地当成了权威语义正确。

独立复现使用冻结 Author SDK 自己生成临时 ContentPack 和受管临时测试目录：

1. seed `Audit.LocalInstall`；
2. 在 `crash:install-local.after-prepare` 留下合法
   `recovery-required` marker；
3. 保持 `sourceStateBeforeExisted=true`，只把
   `sourceStateBeforeBase64` 改为空串；
4. 另一个命令执行 `recover`。

实际结果为：

```text
recover exit=0
success=true
recovered=true
source-state.json length=0
subsequent source status exit=1: JSON contains no token
```

对 `journalBeforeBase64` 做相同变造时，恢复会先把 deployment journal
覆盖成零字节；命令最终虽然失败，但原权威已经被破坏。

现有
`tests/DTMAPI.AuthorSdk.Tests/Program.cs:1312-1323`
只覆盖损坏 `phase` 和整个 marker 为非法 JSON。发布 schema 也允许空快照
字符串。因此 Author SDK 完整 Unit、PowerShell 7/5.1 release check、包结构和
schema parity 全绿仍会漏过该问题。这直接反驳了路线中“损坏但可反序列化的
schema-3 marker 会 fail-closed”的完成结论。

最小修正不需要新 schema、receipt 或恢复体系。在任何目录移动或权威写回前：

- 校验 `Existed` 与空/非空字节一致；
- 严格解析 journal 快照并核对 game root、UniqueID、destination 和
  `previous`；
- 严格解析 source-state 快照并核对 schema、game root 和 selections；
- 增加空串、Base64 `{}`、错误 root、错误 product 和无效 selection 回归；
- 重新构建唯一受影响的 Author SDK ZIP，并复跑现有聚焦 Unit、双 PowerShell
  release check 和本地安装事务门。

该修正不要求重建 Runtime 或十个产品，也不要求重跑 Manbo、GC 或游戏。

## P1-2：当前完整 Release 会在证据保留门确定性失败

`docs/debug/evidence-retention-allowlist.json` 尚未吸收 2026-07-28 的
Manbo、no-demand 和 active-GC 新证据引用。独立执行：

```powershell
tools/scripts/build-evidence-retention-allowlist.ps1 -Check
```

当前 HEAD 返回：

```text
Evidence retention allowlist is stale.
```

`tools/scripts/test.ps1:76-83` 在完整 Release 中固定执行相同的 `-Check`。
所以此处不是“完整 Release 可能发现问题”，而是发布前已经知道完整 Release
必然中止。

不能只机械重建当前 allowlist：

- 新 no-demand 根在 Update/Smoke Matrix 中写成裸
  `BATCH5-NO-DEMAND/...`，而当前 durable-root parser 只从带
  `docs/debug/evidence/` 前缀的引用提取根；
- `PRERELEASE-ACTIVE-GC` 不在
  `build-evidence-retention-allowlist.ps1:35-48,130`
  的 durable category 集合中，r14 证据根及
  `auto-fishing/final-validator-reevaluation.json` 因而不会被当作一个
  不可分割的保留根。

最小修正是把 no-demand 权威引用写为完整 evidence 路径，在现有 allowlist
机制中增加 `PRERELEASE-ACTIVE-GC` category 和聚焦 parser 测试，再重建并
提交 allowlist、执行一次 `-Check`。这仍是文档/证据索引修正，不需要完整
Release 或游戏复验，也不应新建第二套 receipt/保留体系。

## P1-3：三份公开外部消费者仍处于真实加载待办

`tools/release/dtmapi-product-catalog.json` 仍以
`docs/reviews/api/2026/20260712-0002-workshop-055-binary-compatibility-review.md`
作为兼容权威。该 Review 仍为 `release blocker open`，其 gate 5、7、12
要求现有外部消费者在 Unity Mono 下完成不重编加载及适用的行为门。

Catalog 中以下三项仍明确写为 `ActualLoadLanePending`：

- Workshop `3743621104`；
- Workshop `3743644065`；
- Workshop `3754869009`。

步骤 4/5 的 `23/23` MemberRef 解析只证明候选 ABI 可以静态解析这些引用；
步骤 6 运行的是 Manbo、no-demand、ActionSpeed 和 AutoFishing，没有加载上述
三份 DLL。默认完整 Release 的 private retained-ABI lane仍是 .NET metadata
一致性检查，也不替代 Unity Mono Entry/load 证据。

新路线没有明确撤销旧兼容承诺；用户要求增加 Manbo 验证，也不等于放弃其他
已经公开、已经把 DTMAPI 作为前置的消费者。因此不能把 Pending 字样保留在
Catalog，同时宣称所有发布前兼容门已经关闭。

推荐的最小收口是一轮第三存档 `NoNativeSave` 小型兼容 smoke：绑定三个精确
订阅 DLL/hash，证明 Loader 来源、Entry、其实际 provider/API 解析、无
`MissingMethodException` / `TypeLoadException` / `FileLoadException`、
标题/退出清理和存档不变。可以在一次进程中组合，但每个消费者必须有独立
加载结果。另一选项是用户明确缩减 0.5.5 的外部兼容承诺，并同步旧 Review、
Catalog 和公开措辞；不能静默把静态 MemberRef 当成真实加载。

## P2-1：prepare publication boundary 仍会产生孤儿或错误报告

Author SDK 在 durable `localInstall` marker 发布前已经创建 immutable input
和 staging。真实进程终止发生在这段窗口时，会留下无 journal 引用的
`install-inputs/<guid>.zip` 或 staging，目前没有有界发现/清理入口。

另外，prepare journal 已落盘但 `WriteJournal` 抛出写后异常时，
`localPrepared` 尚未置为 `true`，报告会写成“没有 durable mutation”，而下次
安装实际会要求显式 recover。

这不要求为 0.5.5 新建事务或收据家族。应补写后只读对账；prepare 前孤儿可用
现有 hidden-root 规则做有界清理债，或明确记录为 SDK 后续项。

## P2-2：Owning Update 的 Changed Files 仍漏四项

`docs/updates/2026/20260727-0001-dtmapi-055-prerelease-route.md` 的
Changed Files 清单没有列出本路线实际修改的：

- `docs/reviews/code/2026/20260728-0003-prerelease-no-demand-managed-product-isolation.md`；
- `tools/scripts/run-batch5-no-demand-profile.ps1`；
- `tools/scripts/test-batch5-no-demand-profile.ps1`；
- `tools/scripts/run-prerelease-manbo-compatibility.ps1`。

这只是追溯清单缺口，不影响候选字节和运行证据。补清单无需重建或重测。

## P2-3：完整 Release 的临时重建包不能替代冻结候选

`tools/scripts/test.ps1:26` 从当前 HEAD 重新构建，`test.ps1:62` 又在临时目录
重新生成 Runtime Workshop 包。它不会直接消费
`dist/prerelease-step5-candidate/DTMAPI`；临时包的 `BuildCommit` 也会是
完整 Release 当时的 HEAD，而不是冻结候选的 `3f5cb3268f3e`。

因此完整 Release 可作为当前源树的全套门，但其临时包 PASS 不能替代冻结
玩家包的 hash 权威。进入下一阶段时应：

1. 完整 Release 前后重哈希冻结候选，确认它未被覆盖；
2. 对 Runtime/产品候选范围做 source diff，确认没有相关漂移；
3. 随后的 Workshop 玩家包终审明确以冻结目录为输入；
4. 若决定采用完整 Release 临时重建包作为新候选，则必须重新冻结其精确
   provenance/hash，不能同时沿用旧候选身份。

## P2-4：no-demand 的“内容域不激活”措辞超过了证据

路线图步骤 6 把 no-demand 门写成“Compatibility/内容域不激活”。现有日志
实际证明的是：

- 可选 Compatibility Host 保持 dormant；
- 没有可选功能 Hook、目录枚举、反射查找、retained callback、native
  updater 或逐帧内容工作；
- mandatory GameBridge 仍会完成空 feature 注册和生命周期 refresh。

这不推翻 no-demand 验收，但应改成“可选 Compatibility Host 不激活；内容
功能无 demand 时不安装 Hook、不进入逐帧工作”，不能把平台自身的空
lifecycle refresh 也描述为不存在。

## 已接受的部分

独立复验没有推翻以下结果：

- 当前工作树在审计开始及聚焦复验后均为 clean；
- Runtime/product candidate source commit 为
  `3f5cb3268f3e786a13594fae44e542c0c9d2657e`，其后 mandatory Runtime、
  Compatibility Host、十产品、Catalog/publish 和版本投影没有候选范围源码
  漂移；
- Runtime 候选为 31 文件、71,447,151 字节，tree SHA-256
  `7c5148d1247cdd8faac63148e014801c1f0389fd210e677be18b3f2037cb8fab`；
- release manifest 和七个 Runtime/Doctor 组件 hash 与 owning Update 一致；
- 十个 Advanced ZIP 的长度/hash、`1.0.0` manifest 和最低 Runtime `0.5.5`
  投影一致；
- Runtime `0.5.2-alpha` 和十个产品的私有只读回滚输入分别通过
  `-VerifyOnly`；
- Catalog、Batch 6 Phase 0、prerelease-step1/step2/step5 Unit、文档治理、
  `git diff --check` 均通过；
- Author SDK ZIP 仍精确为 126,949,679 字节、SHA-256
  `f1960dbbfca4fcbaa2f8e19b9fcc9c7b6114b8db88e6f272cc3f91dbbd5aa439`，
  且现有双宿主 release check 通过；这证明包未漂移，但不关闭 P1-1；
- Manbo、no-demand 和 active-GC 收据根存在，候选/hash、`NoNativeSave`、
  进程退出和恢复摘要仍为 Passed；
- 发布波次仍准确为 Runtime → AutoFishing → MoreEquipmentSlots → 其余八
  产品；三语言文案仍是唯一手工 Steam 文字权威；
- G7、CustomAnimals/AudioReplacement 基座化、AnimalPack、Oil、Mine 发布、
  ShellCrab 和 BGM 仍被正确延期。

## 重新接受路线的最小门

按风险和依赖顺序：

1. 修正 Author SDK 两份快照的语义 preflight 和 prepare 写后对账，补聚焦
   Unit，重建 SDK，并只复跑 SDK 受影响门；
2. 补全 no-demand 路径和 `PRERELEASE-ACTIVE-GC` 的现有 allowlist
   category/聚焦测试，重建 allowlist 并让 `-Check` 通过；
3. 完成三份 `ActualLoadLanePending` 消费者的小型 Unity `NoNativeSave`
   兼容门，或取得并落盘明确的兼容承诺缩减决策；
4. 补齐 Changed Files、收窄 no-demand 措辞，并明确完整 Release 临时包与
   冻结玩家包的关系；
5. 对上述修正做一次独立聚焦复核；通过后才恢复
   `verified / READY-FOR-FINAL-RELEASE`；
6. 然后从头运行一次完整 Release。失败时仍遵守既定的聚焦失败门、未达诊断
   尾部、批量修正和最终一次 clean from-start 规则；
7. 完整 Release 通过后，再对精确冻结玩家包运行 PowerShell 5.1/标准主机、
   空格/非 ASCII、离线安装、check/status、uninstall 和 collect-logs
   终审。

前三项修正都不要求重跑既有 Manbo、AutoFishing/ActionSpeed GC、L0–L5 或
长测；只有改动 Runtime/product 候选字节或对应游戏行为时，原运行证据才失效。

## 验证边界

本审计运行了聚焦 source/unit/contract/package checks 和受管临时 Author SDK
复现。没有获取 Runtime lock、启动 Doloc Town、写玩家存档、修改订阅目录、
运行完整 Release、执行 Workshop 玩家包终审或操作 Steam。

## Resolution

审计修正由 owning Update `20260727-0001` 统一记录。Author SDK
快照/prepare 事务修正在 `ed85e11a`，三份冻结外部消费者的精确准入和 Catalog
真实加载门在 `9beefe78` / `c7e1ec2f`，证据保留契约升级为 schema 5；最终
Runtime-bound 外部消费者、Manbo、no-demand 和 active-GC 聚焦门均已针对
BuildCommit `c7e1ec2f3697` 重跑。修正集尚待独立聚焦复核，因此路线仍保持
`implemented / provisional`，本 Resolution 不把它提升为
`READY-FOR-FINAL-RELEASE`。
