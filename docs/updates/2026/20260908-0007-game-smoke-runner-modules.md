# 20260908-0007: 游戏测试运行器职责拆分与 MoreSaves 场景首迁

## Metadata

- Update ID: `20260908-0007`
- Date: `2026-09-08`
- Lifecycle Status: `verified`
- Validation Level: `docs, source, unit, runtime`
- Runtime Validation: `passed`
- Related Issue State: `none`
- Source: 工作空间建设获批工程包；既有保存、恢复、截止时间和 MoreSaves 三阶段契约保留，移除维护上的集中和重复投影。

## Summary

将 `run-game-smoke.ps1` 的共享会话、保存策略、部署恢复和证据职责移到专用模块，兼容现有 CLI 与结果字段。入口从 12,395 行减至 280 行；MoreSaves 三阶段先迁入按请求加载的场景模块。普通 NoNativeSave 的选定产品恢复/迁移前提，以及 MoreSaves Entry 的旧档迁移异常，在启动前区分。为已批准的真实邮件验收增加明确的 `WaitForManualExit` 人工观察入口，人工截止时间单独判定。实现、离线验证、状态已确认的原位普通会话、MoreSaves 三阶段和最终人工邮件会话通过；此前三次原位运行因既有产品加载写入而整体失败，首次人工邮件会话到期退出且生命周期失败，均保留原证据。

## Changed Files

- `tools/scripts/run-game-smoke.ps1` 与 `tools/scripts/game-smoke/`。
- `tools/scripts/run-moresaves-fixed12-acceptance.ps1`、受影响脚本测试及运行器工作流说明。
- QA `QaHostSettings`、`QaHostParticipant` 与 `DTMAPI.QaUnitTests`：人工观察 bool 及直接行为测试；未改 Runtime ABI 或生产程序集源码。
- `tools/scripts/fixtures/candidate11-product-identities-20260805.json`：冻结历史九源/两保留事务的最小身份输入，注明来源提交；不再从当前 Catalog 推断旧事务集合。
- 本 Update；公共月表由主建设流程同步。

## Validation

要求：参数/字段/退出与 evidence marker 兼容；保存、来源、恢复边界的直接行为测试；前提失败不启动游戏；过期输入与进程退出/恢复失败不误报成功；三阶段共用 fixture 和有效候选。

实际 Windows PowerShell 5.1 离线结果：

- 模块行为 71 项通过，覆盖按需场景、三阶段参数、实际包相对 EntryDll、MoreSaves 启动迁移、选定 MoreEquipment 活跃/未加载 owner 的状态、截止时间、退出、来源和恢复失败。
- 真实子进程边界 17 项通过：投影/错误前提必须停止；人工模式必须显式绑定 StageQaHost、QaObserveSaveLoaded 和有效 UI 槽位；私有 runner 副本将部署与操作系统边界设为陷阱，证明阶段失败、finally 与失败结果不假报 NoNativeSave/恢复成功。另用真实、受本测试拥有的工作进程证明提前正常退出通过，而到期后再正常退出仍保留 `ManualExitTimeout` 失败，进程清理通过不能替代人工退出通过。
- QA `--manual-exit-only` 聚焦行为通过：省略新字段时仍自动 RequestQuit；人工模式仍产生真实 SaveLoaded verified，完成后 Continue，正常 Close 幂等；非法 QA settings 拒绝。QA Release 单项目构建成功，0 警告/0 错误；只重新构建 QA 测试项目验证新增行为，未重复全平台构建/测试。
- `test-game-smoke-save-modes.ps1` 与 `test-noqa-deadline.ps1` 通过；旧入口实现文本断言替换为模块行为，保留保存家族、来源和产品路由约束。
- 受影响的 Batch5 no-demand、Batch5 GC ladder、ActionSpeed Advanced、Candidate11 冻结事务回放、Runtime evidence retention 五份兼容测试全部通过。前两类适配仅调整已迁移函数/阶段的读取位置；Candidate11 保留原九源/两保留断言，匿名 Catalog 传递被冻结的 selector 字段，匿名 ISSUE-011 收据补齐当前消费者所需 tooltip/search-kind 字段。没有反写当前 Catalog 为旧状态。
- 最终测试链复核将 Batch5 GC/no-demand 和 Batch6 AutoFishing GC/behavior/退役 Manager 的源码断言分别定位到实际模块，移除把模块重新拼成大字符串的适配方式；跨阶段顺序检查入口调用。五个合成脚本在 Windows PowerShell 5.1 依次通过（合计 10.184 秒），日志和输入 SHA 在 `docs/debug/evidence/WORKSPACE-CONSTRUCTION/20260908/runner-test-adaptation/validation.json`。Batch5 的旧范围断言曾误命中其他参数的 `0–500`，现精确检查自身工作量的 `1–500`。没有改变生产 runner 或复跑游戏。
- 完整入口的产物治理检查也已适配实际 owner：Unit 通过注册图核对共享 Suite.props / SuiteEntry 会话，dump 源码与 AST 从 diagnostics 读取，CLI 默认仍查入口。Windows PowerShell 清理夹具通过（1.767 秒，`runner-test-adaptation/06-test-artifact-governance.log`）：持锁会话保留、释锁清理、无捕获不建目录，以及长度/SHA 核对后交接均实际执行；未放宽原约束。
- 首次机械迁移复核：原 CLI 参数块保持一致；原 99 个函数定义中 95 个正文保持一致，另四处为被后定义覆盖的重复哈希函数删除、无限默认日期的整型溢出修复、进程退出检查可注入测试依赖、恢复前先验证备份。随后明确追加一个兼容默认值的 `WaitForManualExit` 参数及 QA stage settings 传递；新增前提和人工行为另以样本验证。改动 PowerShell 解析通过。

第一次真实 Windows PowerShell 5.1 投影检查发现：dot-source 阶段中的 `exit` 只结束阶段，外层在输出 ValidateSaveTestModeOnly 结果后继续，意外执行了一次本地构建；随后既有游戏目录未解析门拒绝，未安装、启动游戏或接触玩家数据。解析通过不能证明此控制流兼容。现改为阶段显式返回退出码、由 CLI 每阶段立即停止，并在会话准备前再次拒绝投影请求；上述真实子进程测试已验证零构建、零部署、零启动。

## Evidence

启动前真实包复核发现，新 MoreSaves migration 预检把源 manifest 模板的短 `EntryDll` 当成部署格式；实际官方包已投影为从包根解析的 `Content/DTMAPI/DTMAPI.MoreSaves.dll`。现按照既有包装器的包根相对路径边界解析并绑定唯一预期 DLL，拒绝遍历/根外路径和未投影的短模板；合成样本使用实际部署结构并补两种负例。此差异在主流程启动游戏前修正，没有靠重复游戏启动定位。

- [模块行为结果](../../debug/evidence/WORKSPACE-CONSTRUCTION/20260908/runner-initial/modules-result.json)、[子进程边界结果](../../debug/evidence/WORKSPACE-CONSTRUCTION/20260908/runner-initial/process-boundaries-result.json)、[首次函数迁移复核](../../debug/evidence/WORKSPACE-CONSTRUCTION/20260908/runner-initial/initial-function-move-audit.json)、[QA 聚焦测试](../../debug/evidence/WORKSPACE-CONSTRUCTION/20260908/runner-initial/qa-manual-exit-focused-test.log)、[QA Release 构建](../../debug/evidence/WORKSPACE-CONSTRUCTION/20260908/runner-initial/qa-manual-exit-release-build.log)。最终引用的五份结果已按原字节提升到本次长期证据目录，`provenance.json` 保留原路径与 SHA；这些离线报告不代表游戏玩法 PASS。
- 兼容测试日志位于 `tmp/workspace-construction/test-<name>-runner-tail.log`，对应 `batch5-no-demand-profile`、`batch5-gc-ladder`、`batch6-actionspeed-advanced-product`、`candidate11-source-transaction`、`runtime-evidence-retention`。
- [20260908-102732 原位运行](../../debug/evidence/GAME-SMOKE/20260908-102732/result.json)：原生存档未变、QA 清理和进程退出通过；选定 native2 的既有 Product v3 journal 在 SaveLoaded 恢复时写入，committed-sidecar 比对失败。原/观察文件保留在同证据目录；generation 11→12、attempt 标志与指纹清空，两个 escrow 项未丢失。此为既有产品恢复行为，不归因于 CLI 拆分。
- [20260908-103741 原位运行](../../debug/evidence/GAME-SMOKE/20260908-103741/result.json)：原生存档未变、QA 清理和进程退出通过；native5 的旧 scoped flat schema 3 被 Product 正常迁移，generation 3→4，因此同样未通过 committed-sidecar 比对。原件由产品既有 `.legacy-migrations` 保存，日志记录精确来源；数字 schema 3 本身不能证明是 Product v3。
- [20260908-104616 CoreOnly 原位运行](../../debug/evidence/GAME-SMOKE/20260908-104616/result.json)：原生存档未变；禁用 MoreEquipment 后，既有 Compatibility Host 在 native2 检出 cold demand，把两个 journal escrow 项归还到内存背包并写入 current/previous。CoreOnly 不能单凭 owner 禁用跳过旧数据恢复前提。此事务由本测试主动变更 owner profile 引出；主流程保留观察后的两份侧车，再用已保存的原件恢复到 104616 前的精确 SHA/mtime（`21C6…` / `3BDD…`），没有写回 native archive，失败状态保留。见同目录的 `orphan-recovery-after-0/1` 和[恢复收据](../../debug/evidence/GAME-SMOKE/20260908-104616/deliberate-profile-sidecar-restore.json)。
- [20260908-105706 最终普通原位运行](../../debug/evidence/GAME-SMOKE/20260908-105706/result.json)：主流程先确认 UI6/native5 的当前真实 Product v3 无待恢复项目，再使用 Current 配置与本次 QA 构建运行。整体 PASS，原生存档/committed sidecar 未变、QA 清理和正常退出通过。
- [MoreSaves 三阶段汇总](../../debug/evidence/WORKSPACE-CONSTRUCTION/20260908/more-saves-runtime/moresaves-fixed12-acceptance.json)：同一保留 fixture 的 EnabledLifecycle、DisabledCold、ReenabledCold 均 PASS，分别关联 `105907`、`110000`、`110042`。只有第一阶段请求 ArchiveMutation；后两阶段 NoNativeSave。官方产品候选身份匹配、live 产品树与 profile 未变；包装器已完成并显式保留 fixture。
- [20260908-110211 首次人工邮件会话](../../debug/evidence/GAME-SMOKE/20260908-110211/result.json)：修复件 SaveLoaded、原生存档/sidecar 未变、QA staging 清理和进程退出通过；到期后由 CloseMainWindow 收尾，SaveFixtureIsolationCleanup、QaHostLifecycle、OwnerLifetimeCloseCleanup 失败，整体仍为 Failed。未打开邮件，未验收原生任务接受。该会话使用人工超时单独字段加入前的 runner，保留原始结果，不反填新字段或改为玩法通过。
- [20260908-112115 最终人工邮件会话](../../debug/evidence/GAME-SMOKE/20260908-112115/result.json)：复用原 player-mail fixture，用户打开原生邮箱，主流程观察并留存[目标邮件](../../debug/evidence/GAME-SMOKE/20260908-112115/player-recovery-mail.jpg)与[原生任务](../../debug/evidence/GAME-SMOKE/20260908-112115/player-ruinedcity-quest.jpg)画面，后者显示“旧城市废墟／和奥兰多聊聊”。经正常菜单返回主页后结束会话，整体 Passed，`ManualExit=Passed`、`ManualExitReason=ExitObservedBeforeDeadline`、`ManualExitTimeout=false`；SaveLoaded、原生存档与 committed sidecar 未变、fixture/QA/OwnerLifetime 清理及 profile 恢复均通过。邮件玩法来自独立操作与画面证据，未由 SaveLoaded 或人工等待字段推断。

三次运行已证明普通“不触发原生保存”仍可能遇到产品或兼容层的既有加载写入；不因此修产品或逐槽重试。最小预检只读取 Catalog 绑定的当前槽侧车及实际 owner 选择：活跃 Product 已确认的 `origin=3/phase=0/attemptStarted=true` 报 `PendingProductRecovery`，已确认的 scoped flat schema 3 报 `PendingProductMigration`；owner 不会加载时，规范 Product v3 的非空 committed itemId 或已知 owner/orphan journal 项报 `PendingProductColdRecovery`，对应 Host 已有的 journal 处理或 occupied slots 准备入口。别的槽/owner、单独 generation/escrow 数量不泛化为同一条件，显式恢复场景由自身门负责。后续普通样本先确认当前实际 Product v3 无待恢复项目，不再猜槽；临时非保存设置按收据恢复，不增加 MODS 树隔离。日志通过的子项均不冒充 UI 视觉通过。

人工入口在 CLI 前提、QA settings 和 participant 中绑定。它只在要求完成后继续等待，保留原有 SaveLoaded 检查、fixture 保护和超时退出；不会生成邮件/任务 PASS。运行器以 `manual-exit.json` 和结果中的 `ManualExit`、`ManualExitReason`、`ManualExitTimeout` 区分截止时间前观察到退出与到期后收尾；后者即使 CloseMainWindow 成功、ForcedClose 为 false，整体也必须失败。异常收尾保留相同归因，实际 QA/owner/fixture 生命周期门继续独立判定，不由操作系统关闭补成通过。未设置人工模式时保留原自动退出语义。实际邮件阅读及原生任务出现由人工玩法证据确认，不借用无关 ExternalPlayerInput gate。

MoreSaves 包装器首次调用前，主流程仍持有上一普通测试的锁，导致包装器等待约一分钟。正常释放旧锁后，包装器在同一进程、同一 fixture 继续取得自己的锁；未重启游戏，该等待仅记作流程耗时。说明已明确包装器自行取得/释放锁，调用前不能另持同 worktree 的非重入锁；未修改锁实现。

## Rollback Notes

回退入口与模块引用须同步，保留其他场景的 CLI 行为。离线工具测试仅使用本轮独占临时目录，不接触共享游戏、官方 MODS、上传目录或私人玩家档。102732、103741 两次 Current 运行中的产品自然恢复/迁移未回退；104616 主动变更 owner profile 引出的侧车事务，保留观察证据后按上述精确收据恢复。没有因此备份或写回 native archive。

## Follow-Up

本工程包验收完成：普通原位、同一 fixture 的 MoreSaves 三阶段、0003 修复件人工邮件验收及人工退出门均通过，完整 Release/公开源码入口也已通过。对比游戏验收检查点 `c07dcde2` 与最终源码候选 `e1321953`，产品、QA、runner 和共享构建的已跟踪行为输入没有变化，[复用证明](../../debug/evidence/WORKSPACE-CONSTRUCTION/20260908/final-game-evidence-reuse.json)绑定五份实际结果；保留各自真正加载的程序集身份，不声称后续每份重编译字节都进过游戏。没有剩余运行门。
