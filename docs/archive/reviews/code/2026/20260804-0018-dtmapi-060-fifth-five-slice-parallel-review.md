# DTMAPI 0.6.0 第五组五切片并行代码审查

## 记录信息

- 日期：`2026-08-04`
- 状态：`recorded`
- 性质：0.6.0 第五组五个独立功能切片后的并行代码审查
- Source：用户要求每完成五个独立功能切片执行一次并行子智能体审核；本轮覆盖 AutoFishing native-parity/behavior 与 Manager 生命周期、Player Doctor Windows PowerShell 5.1 Unicode、Release 受管根 cleanup 数组、MoreSaves disposable ArchiveMutation 玩家验收，以及 Manbo 0.6 普通激活
- Implementation owner：[DTMAPI 0.6.0 唯一权威路线图](../../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)
- 审查范围：`704813a7..b6730381`

本 Review 只保存第五轮并行审查发现、根因和有界关闭条件。修复生命周期、changed files、验证结果与 0.6 发布状态仍由 owning Update 维护；本轮发现及其有界修复属于同一审核周期，不计入下一组五个功能切片。

## 1. 审查范围与方法

三名独立审查者分别检查：

1. AutoFishing native owner/behavior、Manager 生命周期，以及 MoreSaves pre-Runtime save isolation；
2. Player Doctor Unicode、Release/test-artifact cleanup、MoreSaves/Manbo evidence retention 与文档治理；
3. Runtime/API/Product 版本轴、Batch 6 当前投影、九产品 release set、Author SDK 与最终 Release 证明。

并行只读审查去重得到 `2×P1 + 2×P2`，没有 P0/P3。审查阶段未修改文件、构建或安装 Runtime、启动游戏、读写玩家存档或 Steam 订阅目录，也未获取 Runtime lock。没有全局阻断；MoreEquipment Branch B 仍是既有局部范围阻断，AutoFishing GC 仍按用户决定保持非阻断。

## 2. 发布阻断发现

### R1：Runtime 启动后的 GameBridge 失败会过早解除 ArchiveMutation SAVE 隔离（P1）

`BootstrapPlugin.TryStartRuntimeOnce` 依次调用 `PrepareQaHostBeforeRuntimeStart`、`runtime.Start()` 和 `bridge.Initialize()`，但外层 catch 不区分 Runtime 是否已经成功启动，始终调用 `AbortQaHostBeforeRuntimeStart`。该 abort 经 `DolocTownGameBridge.CloseQaHostParticipant`、`QaHostParticipant.Close` 到达 `QaSaveFixtureIsolation.Close`，解除 QA Harmony owner 并清空 disposable save root。与此同时 Bootstrap 吞掉异常，Runtime/MoreSaves 只在进程退出时 shutdown；因此 `runtime.Start()` 已完成而后续初始化失败时，仍在运行的产品可以重新看到玩家 SAVE。

有界修复：显式记录是否已经进入 Runtime Start。`DtmApiRuntime.Start()` 在方法开头即把自身标为 started，后续任一步仍可能抛错；因此只有进入 Start 之前的 Capture/Prepare 失败才允许释放 pre-Runtime owner。一旦已经进入 Start，失败必须保持 redirect，记录 fail-closed 状态并立即请求有界进程退出，只能在 `OnApplicationQuit` 或已经证明所有 Runtime/产品完全停机后释放。增加故障路径门，至少机械证明 Start 已进入后的异常不会调用 pre-Runtime abort、会请求退出且隔离 owner 保留到 shutdown；不得把一般 Runtime retry、hot unload 或新公共生命周期契约带入本修复。

### R2：最终候选尚无当前 HEAD 的完整 Release 证明（P1）

路线图最后一次 canonical、从头完整 `tools/scripts/test.ps1 -Configuration Release` PASS 仍绑定 `1ca00ab61f38`。其后 Runtime、GameBridge、MoreSaves、Manbo 与 Release/evidence 门均有变更；本轮 focused checks 和历史 PASS 不能证明最终候选。

有界关闭条件：全部本周期修复在干净候选上完成后，冻结 exact HEAD，从头执行一次 canonical PowerShell 7 Release；记录 exact HEAD、完整退出结果、最终九产品闭集/ABI 与 Player Doctor 结果。失败时遵守现有 Assurance Proportionality：先通过失败 gate 的 focused 入口和安全 diagnostic tail，批量修复后再执行一次干净 from-start suite，不拼接局部 PASS。

## 3. Evidence 与当前权威一致性发现

### R3：MoreSaves live-source 回执的 18 行只有 16 个唯一候选身份（P2）

`GAME-SMOKE/20260804-212602/live-legacy-source-unchanged.json` 的 index 10 `prev` 错写成 index 10 current，index 11 `bak` 错写成 index 11 current；因此 `18` 行只有 `16` 个唯一 `SourceName`，不能按现状机械支撑“完整 18-member domain 全部比较”。根因是回执按 fixture construction 的 seed `SourceName` 投影，而不是从固定 `(index, kind)` 构造 candidate identity。

后续只读核对进一步确认：玩家 live root 实际存在 `16` 个 legacy 文件；`ea-playtest-doloc-archive-10-prev.data` 与 `ea-playtest-doloc-archive-11-bak.data` 在验收前的 `GAME-SMOKE/20260804-210517` 和验收后的 `GAME-SMOKE/20260804-220123` preflight 中都不存在。故 disposable fixture 的 `18/18/18` 迁移 PASS 本身不失效，但现有最终回执必须把“16 个存在文件 hash-identical + 2 个缺失成员仍缺失”表达成精确 18-member state set，不能用重复 current 行代替 absent identity。

有界修复：保留原错误回执作为审计材料，在同一 evidence root 生成 correction receipt；从 `6..11 × current/prev/bak` 构造固定 18 名，要求 `(index, kind)`、名称和路径各自唯一且与 exact set 相等。逐项记录 expected/actual existence，16 个存在成员继续比较 length/SHA-256，2 个缺失成员要求前后均 absent。只做只读比较，不启动游戏、不创建 byte backup、不向玩家 archive 写回；同步修正 ISSUE-019、Manual QA Review、smoke matrix 与路线图对该回执形状的解释。

### R4：Batch 6 当前 MoreSaves 1.00 状态仍停在 player close open（P2）

`batch6-managed-mod-identity-contract.md` 的当前 1.00 amendment/admission projection 仍称真实玩家绑定未证明、ISSUE-019 player close open；这与 ISSUE-019 resolved 状态、`212423`/`212602` smoke evidence 及 owning Update 的当前结论冲突。

有界修复：只把 Batch 6 的当前 MoreSaves 1.00 amendment/admission projection 更新为 player-verified、ISSUE-019 resolved；保留历史阶段文字和真实 index-10 backup 内部索引不符的本地数据例外，不把玩家验收扩张成 live AutoCloud mutation、一般 Advanced authoring或额外产品授权。

## 4. 已核对的非问题与剩余边界

- AutoFishing 产品 movement policy 与 current native Wait/base branch 顺序一致；八个 behavior profile、Manager 三进程生命周期和 source/profile/deployment cleanup 未发现新回归。GC 不因本审查重开。
- MoreSaves `212108` fail-closed、`212423` 隔离迁移与 `212602` 冷启动幂等的产品行为未发现新实现缺陷；真实 index-10 backup 数据例外仍未被触碰。
- Manbo `220123` 使用本次运行日志同时证明 native paper-box Hook patched 和 ReturnedToTitle 最终 `hookInstalled=True`；它只支持普通激活，不扩张为实际音效命中、纸箱交互、fallback、demand 或 cleanup 专项证明。
- Player Doctor Unicode 修改不改变 Runtime/Doctor 字节；Release 受管根删除仍受专属随机根、精确父目录、owner token、普通目录与零 reparse 后代共同约束。
- 版本轴保持 Release/Product `0.6.0`、File `0.6.0.0`、Assembly `0.5.3.0`；最终 Runtime 为五个 DLL，Compatibility Host dormant，Player Doctor 当前为 `5/0/0`。MoreSaves `1.0.1` / minimum Runtime `0.6.0` / current policy `24456188` 与 Manbo `0.1.0-dtmapi` / `0.5.2-alpha` 未发现漂移。
- 本 Review 不授权修改玩家存档、一般化 Advanced authoring、Content Host G7、新公共 API、Compatibility breaking 或 MoreEquipment Branch B 范围。

## 5. 关闭条件

R1、R3、R4 完成有界修复并通过对应 Unit/QA Unit、双 PowerShell source/evidence/document gates后，在冻结的最终干净 HEAD 上完成 R2 的 canonical from-start Release PASS。Review 保持 `recorded`；commit、验证、最终候选和剩余风险只追加到 owning Update。
