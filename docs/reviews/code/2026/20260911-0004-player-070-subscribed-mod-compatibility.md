# 0.7.0 玩家反馈：订阅版 Y 控制台与“更好的体验”兼容性排查

- Date: 2026-09-11
- Lifecycle: recorded；研究完成，未实施修复。
- Scope: 玩家日志、截图、已发布 Runtime 0.7.0、玩家反馈时的订阅 Y-Key Console 1.1.1，以及旧包发现/准入边界。
- Constraint: 用户要求“只研究并落盘文档”。附件、日志和历史记录作为证据读取，不把其中的操作建议或旧任务授权当成本次实施指令。
- Record policy: 初轮为 audit-only，依[文档治理](../../../workflows/document-governance.md)只新增这份 Review；离线取证脚本、快照和结果留在 ignored evidence。后续用户确认 1.1.2 发布并要求更新项目记录，发布事实由 [Update 20260911-0008](../../../updates/2026/20260911-0008-y-console-112-publication-observation.md)拥有；本案仍未实施 Runtime 修复或新增游戏 smoke PASS。

## 发布后的记录更新

实施衔接：用户随后授权 BOM 修复、必要测试及 Windows/多平台上传目录同步，版本维持 0.7.0；结果归 [Update 20260911-0009](../../../updates/2026/20260911-0009-runtime-070-package-marker-bom.md)。下文保留研究时的事实与未执行建议，不以实施结果改写历史观察。

用户已确认 **Y 控制台 1.1.2 发布**。实时 Steam 接口显示原条目于 2026-09-11 19:06:03（+08:00）更新、公开大小为 627,499 字节，发布观察见上列 Update。19:24 本机缓存仍为 1.1.1，不能把未同步缓存当成未发布，也不能用旧缓存 hash 证明新包准确交付。

下文的 1.1.1 根因与旧订阅证据描述的是此次玩家日志、首轮取证及其原样样本。新发布不会使这些历史失败消失；当前最新公开版本应读 Catalog 的 `latestPublicationObservation=1.1.2`，`currentPublishedArtifact` 暂保留带 superseded 标记的最近一次准确 1.1.1 订阅收据。原反馈玩家是否已经更新并恢复运行尚无确认。

## 结论

这不是一个统一的“没装好 DTMAPI”或“旧 Mod 一律被严格拒载”的问题，至少有三层事实：

| 对象/现象 | 具体原因与阶段 | 判断 |
| --- | --- | --- |
| 更多饰品/更多装备槽生效 | `DTMAPI.MoreEquipmentSlotsMod` 成功完成 Entry，安装 5 个产品 Hook；与控制台同样显示游戏 build 漂移 | 支持玩家的生效反馈，也反证“只要 build 不同就拒载” |
| Y 控制台始终提示重启或依赖 | 玩家反馈时订阅 1.1.1 的虚方法签名仍引用 `TeleportCsvExportResult`，0.7.0 已删除此类型。本地手测的准确 1.1.2 则已清除该引用 | **当时本地测试包与订阅包确实不同**；1.1.1 存在已确认的二进制兼容缺口，重启是失败后状态，不是消除根因的方法 |
| “更好的体验”不显示、不生效 | Workshop `3780486383` 的 `dtmapi-package.json` 读取报 `package-marker-invalid` / unexpected character `ï`，尚未产生可显示的 discovered row | **发现阶段拒绝已确认**；UTF-8 BOM 导致同样拒绝的 Runtime 退化已离线复现。本包原始 marker 未取得，不能把具体文件编码定为最终事实 |
| 管理页容易被理解成“未依赖” | 重启和依赖阻断共用“需重启或检查依赖”；首行先放通用重启说明，真正异常追加在后并被截断 | **诊断展示问题已确认**；“未声明依赖”只表示 manifest 的依赖数组为空 |

因此不能继续以“0.7.0 完全不影响现有 Mod”概括当前结果。已有严格边界中有合理的拒绝，也存在会误拒旧包的读取退化；Y 控制台则是另一条真实 ABI 断裂，不能通过放宽 manifest 检查解决。

## 输入与证据身份

原反馈按原顺序保留，原文没有编号：

> “你好，关于创意工坊mod的一些事，我正常安装了DTMAPI，您上传的更多饰品的mod是实时生效的，但是y键控制台一直显示需要重启或者未依赖，然后另一个不是您制作的但是介绍说需要DTMAPI调控的mod（更好的体验）是不生效并且DTMAPI检测不到”

用户后续补充“本地测试 y 控制台应该没问题，是否版本不同”，并确认“更好的体验”在 **0.6.1 可以运行**；先前输入的 0.6.0 已由用户更正为 0.6.1。旧版可运行在本记录中属于用户确认事实，本次离线差分也一直使用准确公开 0.6.1，不把它记成本次重新跑过第三方游戏行为。

| 输入 | 身份、范围及限制 |
| --- | --- |
| 玩家原件 | `D:\下载\DTMAPI-logs (1).rar`，198,737 bytes，SHA-256 `BE038CF88AF1A9AC338295C3934738C182019300192589EE19EF7003DCDE6044`；原件未修改 |
| 玩家采集 | 2026-09-11 16:48 +08:00；9 份历史 DTMAPI 日志及 latest，共 10 次进程启动记录；含 BepInEx、Unity、Steam tail 和安装状态，**不含第三方 Mod 的 manifest/marker/DLL 原件** |
| 玩家当前环境 | BepInEx `5.4.23.5`；Bootstrap `0.7.0.0`；游戏 build `25163613`。latest 的 Core MVID 为 `124a33771cd9451190f28f86a01ead08` |
| Runtime 对照 | 使用保留的准确 0.7.0 r6 玩家载荷，来源 `1002ae052dee`；玩家 `release-manifest.json` 中五个安装 DLL 的 SHA-256 全部与它一致。该收据比对证明记录中的安装载荷一致，不替代玩家当前五 DLL 的重新采集 |
| Y 控制台对照 | 从本机 Steam 内容缓存只读复制当前 `3742714442`，manifest 版本 `1.1.1`、最低 Runtime `0.6.1`、Advanced 旧 receipt、`Dependencies=[]`；27 文件 / 610,335 bytes，入口 SHA-256 `784FD83E3174F80773926DE937171B1294D8F3E8D21240762492EC5E93B0E050`，与 Catalog 的 `currentPublishedArtifact` 一致。复制前后源文件 hash/长度/mtime 不变 |
| 订阅/发布权威 | [subscription manifest](../../../../tools/release/current-subscription-manifest.json) 中 Y 项 installed manifest 为 `6693520158465470410`；[Catalog](../../../../tools/release/dtmapi-product-catalog.json) 原 `currentPublishedArtifact.releaseVersion=1.1.1` 是已核验旧订阅实物，源码 1.1.2 当时不等于已公开。后续 1.1.2 发布另记 `latestPublicationObservation`，新订阅字节尚待同步。顶层历史 `publishedVersion=0.3.1-dtmapi` 也不能当作当前订阅版本 |
| 旧 Runtime 对照 | PN-031 保留的准确公开 0.6.1；Abstractions SHA-256 `BD732DE274A628258B1370D5F2CD7FD5B01673AD8D297F861EE7EF91EB42F227`。不是更早的 retained 0.5.2，也不是本次重编 |
| 第三方身份 | 玩家 Steam tail 记录 15:25:52 新订阅 `3780486383`，服务端 manifest `235757713548563082`；[该 Workshop 页面](https://steamcommunity.com/sharedfiles/filedetails/?id=3780486383)的英文名为 Quality of Doloc，列 DTMAPI 为必需项，说明通过 DTMAPI 配置。其评论也使用“更好的体验”名称。本机未订阅此包，不能用另一个 `Codex.DolocTownQoL` 冒充 |

原始日志与截图均保留在[本案证据目录](../../../debug/evidence/PLAYER-SUPPORT-20260911-070-YCONSOLE)。没有启动游戏、改官方启停、安装、重编产品、改订阅包或上传。共享 Runtime 锁当时由地图任务持有，本次未取得、未复用、未释放他人锁，也未读取/改动共享游戏安装、官方 MODS 或 live upload；研究使用工作区保留载荷和只读 Steam 内容缓存。

## 一、更多装备槽成功加载意味着什么

玩家称其在游戏中生效。latest 16:45:31.641–31.783 记录：

- `DTMAPI.MoreEquipmentSlotsMod`，Workshop `3744059735`，Advanced receipt 验证通过。
- `gameCompatibility=Drift; compiledBuild=24456188; installedBuild=25163613; references=1/2`。
- 产品 Hook `count=5`、`slotCount=3`、`CommitTransaction` 和该 owner 的 `Mod Entry completed.`。

15:39、15:41 两次历史进程也有其 Entry 完成记录。这证明 Runtime 已能发现并加载受管产品；**不等于所有 Mod 都兼容，也不把玩家的“实时生效”扩张成未知原生 Mod 可安全热卸载/重载的承诺**。本次没有亲自进档复验装备效果。

## 二、Y 控制台：先 ABI 失败，再进入重启状态

### 玩家日志与状态链

latest 的可复核位置：

| 行/时刻 | 观察 |
| --- | --- |
| L21 | 原生订阅快照可用，5 项；来源选择得到 2 个 DTMAPI Mod |
| L24–25，16:45:31.466 | 开始控制台装载事务；`sdk-reference-receipt-verified`，Workshop 来源已接受 |
| L26–31 | `EntryFailed` → 回滚；`assemblyLoaded=True`、`restartRequired=True`、`remaining=0`；原始异常为 `System.TypeLoadException: Could not resolve the signature of a virtual method` |
| L38 | 随后更多装备槽仍完成 Entry，异常被隔离 |
| L214，16:47:37.987 | ModListChanged 重扫后，控制台被“本进程已停用，需要重启”门拦住 |

[逐进程摘要](../../../debug/evidence/PLAYER-SUPPORT-20260911-070-YCONSOLE/log-session-summary.json)记录 10 次启动中 9 次出现相同控制台装载失败，零次控制台 Entry 完成；最早失败在 15:13:00.558，早于“更好的体验”15:25:52 的订阅。另一进程未记录控制台加载失败，不把它算成成功。

由此排除：仅缺少一次重启、第三方 Mod 先破坏控制台、控制台已经正常运行只是状态未刷新。依赖诊断为 0 error / 0 warning，控制台 manifest 本来也没有声明其他 Mod 依赖。

本次也不复用 [ISSUE-013](../../../debug/issues/ISSUE-013-20260712-owner-platform-dependency-reconciliation.md)的旧根因：该案是 Entry 已成功后误判平台依赖失效，本案的控制台从未完成 Entry。玩家官方 Local 目录计数为 0、作者来源覆盖为 0，日志选择的确是目标 Workshop 项，也没有 Local/Workshop 重复来源争抢的证据。

### 当前订阅 DLL 的离线差分

对**同一份原样 1.1.1 DLL**，使用 .NET `8.0.27` 仅解析元数据及反射签名，不执行 Mod 构造函数、Entry、CSV 导出或游戏代码：

| 核对项 | 准确 0.6.1 | 准确 0.7.0 |
| --- | --- | --- |
| `DTMAPI.Abstractions.TeleportCsvExportResult` 类型 | 存在 | 已删除 |
| `DTMAPI.DebugConsole.ITeleportActions.ExportDestinationsCsv(IManifest)` 返回类型解析 | 成功 | TypeLoadException，直接点名缺失的 DTO |
| `DebugConsoleNativeActions.ExportDestinationsCsv(IManifest)` 虚方法签名解析 | 成功 | 同上 |
| 1.1.1 引用的 50 个 Abstractions TypeRef | 全部存在 | 只有该 DTO 缺失 |

原 DLL 中该方法具有 `public final newslot virtual` 标志，返回上述 DTO。它仍在实现类型/接口的签名中，因此**即使玩家从未点击 CSV 按钮，也可能在创建/准备动作类型时使整个控制台 Entry 失败**。

准确结果：[0.6.1](../../../debug/evidence/PLAYER-SUPPORT-20260911-070-YCONSOLE/abi-061.json)、[0.7.0](../../../debug/evidence/PLAYER-SUPPORT-20260911-070-YCONSOLE/abi-070.json)。这不是仅凭异常文案猜测。玩家日志未携带入口 DLL hash，但其 Workshop ID、截图版本、已验证 receipt 和本机准确公开订阅样本吻合；本次仍不冒称做过该玩家机器的逐文件重新取证或 Mono 实机复现。

### 本地测试通过与订阅版失败为何能同时成立

[20260831-0006 的实际部署和玩家手测记录](../../../archive/updates/2026/20260831-0006-y-console-runtime-lightweighting.md)明确：本地 `MODS/DTMAPI_YKeyConsole` 已从 1.1.1 换成 1.1.2；Local 启用、Workshop 禁用；用户测试的是准备好的 1.1.2。这个操作只准备本地手测/上传源，没有提交 Steam，订阅缓存仍是 1.1.1。

本次进一步核验 9 月 9 日保存的原样 Local 包快照，其 marker 绑定的 DLL hash 与上述手测记录完全一致；不是仅比较源码 manifest 的版本号。

| 对照 | 本地手测包 | 当前 Steam 订阅包 |
| --- | --- | --- |
| 产品版本 | 1.1.2 | 1.1.1 |
| DLL SHA-256 | `28B91DE1CBD8F01308BC3162D988D22C2ABCDA0BE6E812A48358C810BF065600` | `784FD83E3174F80773926DE937171B1294D8F3E8D21240762492EC5E93B0E050` |
| CSV 导出虚方法/已删 DTO 引用 | 两个目标类型均已去掉该方法，不引用该 DTO | 两个目标类型仍有此方法，返回类型引用该 DTO |
| 在准确 0.6.1 上离线解析 | 50 个 Abstractions TypeRef 均可解析，两个目标类型均可解析 | 50 个 TypeRef 均存在，两个 CSV 签名可解析 |
| 在准确 0.7.0 上离线解析 | 50 个 TypeRef 均可解析，两个目标类型均可解析 | 缺失该 DTO，两个目标签名解析失败 |

本地包结果：[身份核对](../../../debug/evidence/PLAYER-SUPPORT-20260911-070-YCONSOLE/local-yconsole-112-identity.json)、[0.6.1 解析](../../../debug/evidence/PLAYER-SUPPORT-20260911-070-YCONSOLE/abi-061-local112.json)、[0.7.0 解析](../../../debug/evidence/PLAYER-SUPPORT-20260911-070-YCONSOLE/abi-070-local112.json)。这里的“50 个 TypeRef 均存在”不扩张成全功能验收；本轮未重新读取被其他测试占用的 live Local 目录，也没有新增 0.7.0 游戏实测。

**用户关于版本不同的判断成立。** 本地 1.1.2 没有本次订阅 1.1.1 的 CSV 类型绑定问题；本地成功不能覆盖玩家拿到的另一份旧 DLL。后续解决控制台问题的直接方向是使实际发行产品与 Runtime 配对，不能仅让玩家重启、重装同一组字节或放宽准入。

### 补充核对：1.1.2 是否通过发布门槛

用户随后要求先查文档，并提醒“0.7.0 测试包含这个 Mod”。进一步查验完整 Release 原始日志后确认：**1.1.2 当时的产品级验收已经接收，后续 0.7.0 的完整 Release 也包含 Y 控制台专项与 SDK 封包检查且通过；不能说 Y 没参加 0.7.0 测试，或把 8 月 31 日的旧 Unit 阻断当成当前仍未通过。** 初轮核对时尚无 1.1.2 公开发布记录；后续用户发布及实时接口观察已在本文顶部更新。是否发布与准确最终 r6 下是否重跑其完整 UI 是不同事实。

| 所问层次 | 文档事实 |
| --- | --- |
| 1.1.2 当时是否验收通过 | [最终 Update 20260831-0006](../../../archive/updates/2026/20260831-0006-y-console-runtime-lightweighting.md)记为 `verified`、`Runtime Validation: passed`、问题 `closed`。准确 `28B91DE1...5600` 候选通过产品专项、SDK validate/build/pack、真实 Y/Escape/鼠标给物及六组 G5；用户随后手测未发现显著问题，Follow-Up 明确 current 1.1.2 behavior is accepted |
| 未完成维护是否仍阻塞 1.1.2 | [后继路线 20260831-0007](../../../updates/2026/20260831-0007-y-console-deferred-roadmap-normalization.md)明确十五项技术债“都不是已接收 1.1.2 的阻塞项”；量化分配/profiler 也是后续可选证据。不能把这些路线项重新挂成当前发布前置条件 |
| 是否完整全仓 Release / Unit 全绿 | **后续 0.7.0 完整 Release 已通过。** `full-release-r5` 和最终 `full-release-repair-r2` 均从 `FromStart / All` 执行并 exit 0；两者都含 Y 的 22 个专项入口和 Advanced SDK package PASS。8 月 31 日的旧 Unit / Candidate11 失败保留为历史事实，不改成当时 PASS，也不据此否定后续完整 PASS |
| 是否参加过平台实机测试 | **参加过。** `GAME-SMOKE/20260909-194252` 在 Runtime 开发候选 0.6.4 上记录 Y Entry、SaveLoaded、按 Y 打开和按钮关闭；玩家配合 H 正对照证明控制台 modal 输入隔离，该范围被后续平台验收引用。不能把这轮写成最终 0.7.0 r6 原字节重跑，也不能说 Y 只在 8 月 31 日测过 |
| 最终 0.7.0 r6 是否重新跑过该 Y 包 UI | 所查 `20260910-200318`、D7 的 `20260911-001047` 没有 Y Entry；早期 0.7.0 长标题轮 `20260910-004000` 明确跳过禁用的 Workshop Y。这是具体运行覆盖边界，不等于 0.7.0 完整 Release 未通过；是否需要补跑应按未变化边界的证据复用规则对账 |
| 是否已公开上传 1.1.2 | **后续已发布。** 8 月 31 日 Update 只拥有本地上传源准备，不能改写成当时已发布；9 月 11 日用户确认和实时 Steam 观察归新发布 Update。产品 README 已更新，Catalog 的最新发布观察与尚未同步的新订阅收据分开记录 |

Catalog 里的 `releaseEligibility=RebuildBlocked` 不能单独解读成“1.1.2 功能验收失败”：[Catalog checker](../../../../tools/scripts/check-product-catalog.ps1)对所有 `PublishedProduct` 都要求该值，实际发布事实由各项 `currentPublishedArtifact` 保存；顶层 `ActiveNoUploadAuthorization` 表示没有后续上传授权，也不是测试结果。本轮只核对这些记录，不将历史手测/上传源准备当成本次上传指令。

### 0.7.0 既有 Y 测试覆盖的原始对账

- [最终完整 Release 原始日志](../../../../artifacts/pn041/full-release-repair-r2.log) L171–172：`Unit suite debugconsole: 22 registered entrypoints`，随后 `DTMAPI.UnitTests: OK (debugconsole)`；L421、427：`Batch 6 Catalog Advanced SDK package (y-console): PASS`，两次包 SHA-256 都为 `5453A24C5BF7894F0C8A09F3DA4CF4594A5E1C1E996FDC9C4731959B5E216EF6`。[退出收据](../../../../artifacts/pn041/full-release-repair-r2-result.json)记录 2026-09-10 23:27 至 09-11 00:09，`FromStart / All`、exit 0、前后 SDK 输入摘要相同。该封包检查使用当前源码候选及受控原生引用夹具，不是把旧手测 DLL 或当前订阅 DLL 原样执行一遍。
- [9 月 9 日输入验收](../../../debug/evidence/GAME-SMOKE/20260909-platform-input-retry/README.md)明确 real Y console 作为输入隔离正对照；[运行日志](../../../debug/evidence/GAME-SMOKE/20260909-194252/DTMAPI-latest.log) L5 是 `hostVersion=0.6.4`，L39 为 Y Entry 完成，L1081 为 `reason=hotkey Y`，L1093 为按钮关闭。后续验收对未变化输入边界有明确复用路线，不另造“必须所有 Mod 在每个 Runtime 候选全量重测”的门。
- 完整 Release 的 `RetainedPublicProductBinding=y-console` 仍是 `DebugConsoleMod@0.5.2.0` 历史消费者，34 个成员引用全部解析。这个 retained 输入的 PASS 不覆盖当前 Steam 1.1.1 的 `784FD83E...E050`。最终 r6 的实机旧 receipt 正例实际是 Zoom；不能仅凭总测试通过就推定所有现行订阅 DLL 都已运行。

本次只读提取的[既有覆盖摘要](../../../debug/evidence/PLAYER-SUPPORT-20260911-070-YCONSOLE/y-console-070-existing-coverage.json)保存原日志 hash、行号、两个完整 Release 结果和四轮运行的 Y Entry/打开计数，没有新增游戏测试。

因此应回答：**Y 已参加 0.7.0 筹备与完整 Release 测试，专项和封包门通过；本案漏掉的是准确当前订阅 1.1.1 的兼容覆盖，不能归因为 Y 完全未测。** 1.1.2 的既有产品接收与后续完整 PASS 应予保留；最终 r6 的具体 UI 覆盖缺口按实际变更和证据复用对账，不以“未找到一次重跑”直接宣布整套发布门未通过，更不把后续技术债变为发布阻塞。

### 原因归属与既有验收缺口

[20260831-0001](../../../updates/2026/20260831-0001-y-console-runtime-animal-catalog-and-teleport-cleanup.md)清理源码 UI、CSV 实现以及公共接口/DTO；[20260910-0011](../../../updates/2026/20260910-0011-sdk-first-release-plan-correction.md)再次明确保留该删除。删除授权是真实历史事实，本次不擅自恢复 API。

但“删除已授权”与“当前订阅者没有二进制引用”是两件事。本案证实当前 **1.1.1** 仍有引用，不能套用已退役的 **0.3.1-dtmapi** 的无用户结论，也不能用仅存在于源码/本地测试中的 **1.1.2** 替代 Steam 发布。

[ABI harness](../../../../tests/DTMAPI.AbiCompatibilityHarness/Program.cs)把 CSV 的 20 项删除列入允许集合，并且已知 retained Y consumer 固定为 `E5A34963…` 的 0.3.1 输入；[准确 0.6.1 surface 结果](../../../../artifacts/pn041/compatibility-evidence/exact-061-surface-r5.json)的批量 public consumer 数为 0。它们原范围内的 PASS 不证明 `784FD83E…` 的当前订阅版 1.1.1 可运行。[最终 C01–C10 记录](../../../debug/evidence/GAME-SMOKE/20260910-sdk-msbuild-070/README.md)实际覆盖的原 receipt 示例是 Workshop Zoom，同样不自动覆盖所有当前订阅产品。

责任边界是 **Runtime ABI 清理与当前产品发布版本没有衔接，以及准确当前订阅消费者覆盖不足**。游戏 build 漂移不是此次已找到的缺失类型来源；receipt 已通过，放宽 Advanced 准入也无法补出被删除的 CLR 类型。

## 三、“更好的体验”：发现阶段的 marker 读取失败

latest L15、L205 附近的首因是：

```text
.../workshop/content/2285550/3780486383/Content/DTMAPI/manifest.json:
package-marker-invalid: Package marker could not be read:
Encountered an unexpected character 'ï' in JSON.
```

错误前缀展示的是 manifest 路径，但实际抛错的是同包的 **`Content/DTMAPI/dtmapi-package.json`**。不能据此让玩家去删改 manifest 或补装一项虚构的依赖。

[`ManagedModClassifier.Classify`](../../../../src/DTMAPI.Core/Manifesting/ManagedModClassification.cs)在非 Advanced 分支调用 `AuthorPackageMarker.ValidateIfPresent`；该调用位于程序集加载之前。[`ModScanner`](../../../../src/DTMAPI.Core/Manifesting/ManifestReader.cs)只有分类成功后才 `mods.Add(...)`，异常只进入扫描诊断。因此本案是“已经找到包，但分类失败而未进入列表”，不是 DTMAPI 完全没有扫描到 Workshop，也不是 DLL Entry 执行后没效果。

### 0.6.1 → 0.7.0 具体改了哪里

| 版本/提交 | 对旧包路径的实际变化 |
| --- | --- |
| 公开 0.6.1，来源 `db5e518a6d7f` | 非 Advanced 分类分支直接进入历史兼容匹配/程序集边界检查；**不调用通用 `AuthorPackageMarker.ValidateIfPresent`**。旧的说明性 `dtmapi-package.json` 不成为此路径的准入前置条件。Advanced 自身的 receipt/marker 校验是另一个分支，不能混为“0.6.1 什么都不校验” |
| `8cfa8ac0`，2026-09-07 | 新增共享 `AuthorPackageMarker`，并在所有非 Advanced 候选前调用。原意是阻止删掉 CodeModKind 绕过 SDK marker；同时把已有旧 CodeMod/ContentPack 的同名说明文件纳入了验证。读取方式从新增之初就是 `ReadAllBytes` → `CreateJsonReader`，没有处理 BOM |
| `c03e4282`，2026-09-09 | 为显式 DependencyContract 增加专门 bundle reader 分支；没有修正普通旧 marker 的字节读取 |
| `1002ae05`，2026-09-10，最终 0.7.0 r6 | 补充 `IsLegacyMetadata`，允许无 schema 的已知旧说明字段；该判断位于 `XElement.Load(reader)` **之后**。BOM 在进入字段判断前就导致异常，故此修正没有覆盖本案编码边界 |

这解释了用户确认的“0.6.1 能运行、0.7.0 检测不到”：**Mod 字节即使完全不变，也会因 Runtime 新增读取此前不作为此路径准入条件的文件而失败。** “需要 DTMAPI”的依赖描述没有变化，失败也早于第三方 DLL/Hook 的执行。

变化定位有准确提交及发行二进制两类证据：[新增校验 diff](../../../debug/evidence/PLAYER-SUPPORT-20260911-070-YCONSOLE/marker-check-introduced-8cfa8ac0.patch)、[r6 旧字段补丁](../../../debug/evidence/PLAYER-SUPPORT-20260911-070-YCONSOLE/legacy-fields-added-1002ae05.patch)、[0.6.1 原分类器](../../../debug/evidence/PLAYER-SUPPORT-20260911-070-YCONSOLE/managed-classification-published061.cs.txt)，以及下表真实发行 Core 的差分结果。没有用当前源码重编一个“0.6.1”来代替原发行包。

### 已复现的编码退化

[`AuthorPackageMarker`](../../../../src/Shared/AuthorPackageMarker.cs)直接把 `File.ReadAllBytes` 送入 `JsonReaderWriterFactory.CreateJsonReader`，未消除 UTF-8 BOM。其旧格式字段兼容分支在 XML/JSON 读取完成后才执行，故无法挽救前面的编码错误。相比之下，[`JsonFile`](../../../../src/DTMAPI.Core/Json/JsonFile.cs)使用文本读取，并显式处理开头 `U+FEFF`。

使用两个准确发行 Core 的真实 `ManifestReader` / `ManagedModClassifier`，对自控离线夹具做差分：同一个合法、无 schema 的 `uniqueId/version` marker，只改变是否有开头 `EF BB BF`。CodeMod 夹具省略 CodeModKind，使用自有 DLL 作为静态包输入；没有执行它，也没有伪装成真实第三方包或 Workshop 来源。

| 合成输入 | 0.6.1 分类 | 0.7.0 分类 |
| --- | --- | --- |
| 旧 CodeMod，UTF-8 无 BOM | 历史原生兼容 CodeMod | 历史原生兼容 CodeMod |
| 相同旧 CodeMod，UTF-8 有 BOM | 接受 | `package-marker-invalid`，unexpected character `ï` |
| 旧 ContentPack，UTF-8 无 BOM | ContentPack | ContentPack |
| 相同旧 ContentPack，UTF-8 有 BOM | 接受 | 同上 |
| 显式非法新 `schemaVersion=999`，无 BOM | 旧 reader 不检查此新标记 | 拒绝未知 schema，符合新格式边界 |

结果保存在证据目录的 `marker-061/070-CodeMod/ContentPack-plain/bom/invalid-schema.json`；[研究脚本](../../../debug/evidence/PLAYER-SUPPORT-20260911-070-YCONSOLE/run-research.ps1)及[只读探针](../../../debug/evidence/PLAYER-SUPPORT-20260911-070-YCONSOLE/probe/Program.cs)可复查。

这确认了一个**由新读取路径引入、可误拒合法历史元数据的兼容退化**，受影响范围由文件格式/编码决定，不由 Mod 新旧版本号、作者是否第一方决定。新格式的结构、绑定、来源等严格检查仍有必要，不能把“兼容 BOM”扩大为“所有坏 marker 一律忽略”。

[20260910 marker 冲突 Review](20260910-0006-existing-package-marker-collision.md)解决的是无 schema 旧元数据被误认成 SDK marker；r6 已包含该字段分支。本次不是把已修问题重复归因给“没有 schema”，而是其前面的编码读取遗漏。原 16 包样本中没有 `3780486383`；样本通过不能证明所有旧 marker 编码都覆盖。

### 为什么引入这条准入

引入点是 2026-09-07 的 `8cfa8ac0`，对应 [PN-003 / Update 20260907-0004](../../../updates/2026/20260907-0004-platform-sdk-target-catalog.md)。当时有两个具体问题：

1. SDK、Runtime、Doctor 与内容重载各自读取包目标/marker，规则分散；Packager 已写 schema 2，而内容重载还只接受 schema 1。共享 reader 用于让生成、发现、诊断与重载对同一包作出一致判断。
2. 某个 SDK CodeMod 明确声明 `CodeModKind=Strict`，marker 又绑定该身份、目标版本、manifest 与入口 DLL；若只按修改后的 manifest 决定是否验证，删除 `CodeModKind` 就可能落入旧原生兼容通道，跳过原包的目标/字节一致性检查。因此代码新增“存在 SDK marker 就检查”的规则，调用放在非 Advanced 分支进入旧通道之前。原[跨端矩阵](../../../../tests/Shared/PlatformPackageTargetMatrix.cs)的 `deleted-kind-cannot-bypass-*` 正是该反例，不是因为第三方没有进入第一方 Catalog 而拒载。

问题在于当时把 `dtmapi-package.json` 当成了可以保留给 SDK 的文件名，实际它早已被安装脚本、内容生成器和第三方拿来存说明信息。r6 补回已观察旧字段的兼容，但没有补上字节编码兼容。

### 引入是否有必要：目的与适用范围应分开判断

| 范围 | 本次判断 |
| --- | --- |
| 明确声明 SDK 契约的包，其目标、身份、manifest/DLL hash 是否一致 | **有必要保留。** 能发现打包错配、发布了旧 DLL、修改清单却没有更新绑定等错误，避免不同入口对同一包给出矛盾结果 |
| 带 SDK 契约痕迹的坏包，是否能靠删一个 kind/schema 字段变回旧包 | **不应静默降级。** 继续在执行前报告具体契约错误，不能把“旧格式兼容”写成所有坏 marker 都忽略 |
| 旧包仅有同名说明元数据，是否因此必须满足新 SDK 格式、重新生成 receipt 或获得新许可 | **没有这种必要。** 该文件不授予原生权限、安装所有权或受信来源；旧包既有 manifest、来源与程序集边界仍负责实际加载。BOM 不改变这些语义，不应成为拒载理由 |
| 这是否是一条必须拦住所有旧文件才能成立的安全边界 | **不是。** 无 marker 的 legacy 通道仍开放，marker 本身也不是数字签名或代码沙箱；这条规则提供已声明包契约的一致性，不能夸大为阻止任意恶意作者重写/移除整套声明的保证 |

因此，统一读取和检查显式契约的目的合理；把所有同名旧元数据都纳入严格的新格式准入，范围过宽。当前字段白名单也只是已经实现的兼容策略，不能从“已实现”反推成旧包加载的天然必要条件。若今后调整旧元数据识别，应以是否声明 SDK 契约为边界另作有界修改；本次修复 BOM 不需要连带重做整套识别策略。

### 最简单的修复建议（未实施）

**仅在 `AuthorPackageMarker.ValidateIfPresent` 的 JSON 读取入口兼容一个开头 UTF-8 BOM，之后继续执行原来的字段与绑定校验。** 保留原始 65,536 字节限制和 `MaxDepth=4` 等读取配额；不改磁盘文件，不对 manifest/DLL 重编码，不改变其原始 hash。

建议改动形状如下，位置为 `ReadAllBytes` 和原大小检查之后：

```csharp
int offset = bytes.Length >= 3 && bytes[0] == 0xEF &&
    bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;
using (XmlDictionaryReader reader = JsonReaderWriterFactory.CreateJsonReader(
    bytes, offset, bytes.Length - offset,
    new XmlDictionaryReaderQuotas {
        MaxDepth = 4, MaxStringContentLength = 65536, MaxArrayLength = 65536
    }))
{
    // 保留现有 XElement.Load、旧格式识别及全部 schema/绑定检查。
}
```

这不是引入新的容错框架：[现有 `PackageDependencyContract.ReadJson`](../../../../src/Shared/PackageDependencyContract.cs) L184 已使用相同三字节偏移处理；新 Dependency/Native 路径经该 reader 读取，漏掉的是普通旧 marker 路径。这里采用相同处理方式即可；直接替换为那个 helper 会顺带改变深度/长度配额及诊断前缀，反而扩大改动。Core 与 Doctor 编译同一份 `AuthorPackageMarker.cs`，GameBridge 内容重载调用 Core 的该入口，因此只需修这一处共享源码，避免只修设置页或某一个调用者。

带 offset/count 的 reader 重载由 [.NET 官方文档](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.serialization.json.jsonreaderwriterfactory)提供；[RFC 8259 §8.1](https://www.rfc-editor.org/rfc/rfc8259#section-8.1)允许解析器为互操作性忽略开头 BOM。这里修的是读取兼容，不是让损坏的 SDK 包获得豁免。

本案既有双 Runtime 差分已经证明：同一旧 CodeMod/ContentPack 只多三个 BOM 字节，0.7.0 就由接受变拒绝；无 BOM 内容本身能通过。尚未实施上述 Runtime 改动，也没有把建议代码记作游戏 PASS。

后续实施的最小回归范围：复用现有跨 Core/Doctor/GameBridge 矩阵，加入旧 CodeMod/ContentPack 的 BOM/无 BOM 对照；对合法 schema 1/2 做同样读取对照，并确认带 BOM 的坏 schema、重复字段、身份/版本不符、manifest/DLL hash 错误、删 kind/schema 降级反例仍被拒绝。只跳过开头一个准确 BOM；字面乱码 `ï»¿`、非法 JSON 或正文中的字符不自动清洗。不需要全包重签、全平台准入重写或为此恢复已退役 API。

### 本包仍缺的证据

包内原始 marker、manifest、DLL 和订阅 ACF 不在玩家附件里，本机也没有该订阅。当前可确定的是发现拒绝和报错位置；**本包是标准 UTF-8 BOM、字面乱码 `ï»¿`，还是另有损坏，需要原始字节确认**。BOM 是已有差分支持的首要解释，不把它写成未经核验的包内容事实。

下一步若处理此案，只需取得该 item 的原始 manifest、marker、DLL 文件清单/哈希及来源版本；不需要玩家存档。即使解决 marker，是否还有 Entry、Hook、游戏 build 或与其他 Mod 的玩法冲突，仍需后续实际加载验证。本次没有运行其第三方代码，不能承诺去 BOM 后所有功能都正常。

## 四、Y 设置页为何掩盖首因

截图显示的是 DTMAPI 设置页的 Mod 标签，不是成功打开的 Y 控制台界面。

1. [`RuntimeSnapshotFactory.CreateModStatusSnapshot`](../../../../src/DTMAPI.Core/Runtime/RuntimeSnapshotFactory.cs)优先判断 `restartRequiredOwners`，把状态置为 `inactive/restart-required`，先放通用英文“platform roots were deactivated”；真实异常在 `Prior diagnostics` 后面追加。
2. [`ReflectedTitleMenuSettingsUi`](../../../../src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs)把 `restart-required`、`dependency-blocked`、`blocked` 合并翻译成“需重启或检查依赖”，而详情行又截到 136 个字符，截图正好在通用说明中途结束。
3. `Dependencies=[]` 被准确显示为“未声明依赖”。这不是“缺少 DTMAPI”，也不是一条 required-missing 诊断。
4. 底部“重启后才能再次运行”的意思是允许新的装载尝试。类型/包文件未改变时，下一次启动仍会在相同位置失败；当前展示没有把这一点说清。

重启保护本身符合已加载 Mono 程序集的进程生命周期；本案不能据此要求解除重入保护。需要改进的是**先显示装载失败的具体原因、分开重启状态与依赖状态、保留完整可查看的错误**。

## 五、对“是否更严格、是否误拒其他旧 Mod”的回答

| 边界 | 本次判断 |
| --- | --- |
| 省略 CodeModKind 的历史原生 Mod | 兼容通道仍在；不要求所有第三方补第一方 Catalog 行、Advanced receipt 或重新用 SDK 编译。无 BOM 正例已通过 |
| 显式 Strict / Advanced / NativeContract / DependencyContract | 按所声明通道校验。不能通过删字段将错误新格式降级为 legacy；这类严格边界不应因本案全部撤销 |
| 旧 `dtmapi-package.json` 编码 | 新 reader 会误拒有 BOM 的旧元数据，CodeMod 与 ContentPack 均已复现。其他合法旧字段组合是否遗漏，需要具体原包差分，不能承诺没有风险 |
| 游戏 build/原生引用漂移 | 旧 receipt 通道记录 Drift 后仍尝试装载；两个第一方产品同样漂移，一成一败。不是统一按 build/hash 不一致拒绝 |
| API 删除 | 不属于准入“更严”，但同样会让旧二进制无法加载。当前订阅 Y 1.1.1 是已确认实例；任何仍引用该已删类型的消费者也有绑定风险，不能无证据推广成所有旧 Mod 都坏 |
| 未发现、已发现但拒绝、Entry 失败、需要重启 | 四个阶段必须分别诊断。本案第三方在分类失败，控制台在类型绑定失败，重启提示属于其后续状态 |

## 后续处理范围与验收条件（建议，未执行）

- **控制台产品与发行配对**：核对当前可发布的产品是否已清理所有已删类型引用，并对准确最终订阅/候选字节与 0.7.0 做签名解析、冷启动 Entry、进档 Y 打开/关闭验证。版本号为 1.1.2 或本地能用都不代替准确发行验证。既有 CSV 删除决策维持；若要改变 API 退役策略，应依据这份新消费者证据另作明确决定，本次不恢复 DTO/stub/UI。
- **旧包 reader**：确认原 marker 字节，围绕编码处理做最小修正；保证原样旧包可读，同时重复/未知字段、未来 schema、非法 binding 等负例仍被拒绝。不替作者重签或把未知代码搬到 `BepInEx/plugins`。
- **管理页诊断**：让 `TypeLoadException` / `package-marker-invalid` 等首因可见；把“缺依赖”“装载失败”“本进程需重启”分别表述。保留装载失败条目的路径/身份线索，减少“完全检测不到”的误解。
- **验收输入**：将当前 `currentPublishedArtifact` 的准确 DLL 与冻结历史消费者分开覆盖；保留 0.3.1 历史 PASS，同时增加 1.1.1 的已证失败，不能改写旧记录或用另一个样本的 PASS 关闭本案。旧 marker 验证增加 BOM 维度。

以上均是后续边界，本次没有修复或发布，也没有给玩家发送消息。只做离线元数据/分类探针，不建立游戏运行 PASS。首轮研究脚本的最终汇总遇到 StrictMode 下可选 `error` 字段不存在，探针结果此前已全部落盘；仅修正汇总访问并读取已有结果，没有重跑游戏或产品测试。

研究交付包含：玩家原始证据副本、10 次进程摘要、当前订阅包静态快照、五 DLL 安装收据对照、准确双 Runtime ABI 差分及旧 marker 编码差分。对第三方具体原文件编码和修复后的游戏行为保留明确缺口。

文档收尾：研究探针经 tracked `build.ps1 -SkipTests -Projects` 和 `Get-DotNetExe` 构建，使用 .NET 8；研究 PowerShell 脚本语法无错误，Review 空白检查没有报告空白错误，RAR 原件 hash 未变。`check-doc-governance.ps1` 本次报告两项失败，均为并行地图任务 `20260911-0006` 的月度行未同步到 Update 的 `unit,runtime` / `partial` 状态；本 Review 未产生链接或治理报错。没有代改另一任务的 Update 或月度索引，不把全仓治理结果写成 PASS。
