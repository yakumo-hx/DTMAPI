# DTMAPI 0.6.1 Runtime Workshop 安装器三提交设计与压力审查

- 日期：`2026-08-07`
- 状态：`recorded`
- 性质：对最近三次安装器相关提交的语义冻结、代码审查与隔离压力测试；不是修复完成或发布批准
- Source：用户确认此前 GC 日志来自低版本，要求先确定安装器“安装 / 卸载 / 检查 / 导出日志到桌面”四项语义，再审查三次提交及执行压力测试
- 审查提交：`30fc6db6712c549e081072163d30b0c7c0542034`、`eed21df7eaec3be168667f2e70939c2eddc202fe`、`905dccc398f125cd830325d0ee934bb3d87f8529`
- 前置设计 Review：[20260807-0001 Runtime installer 0.6.1 top-level design](20260807-0001-runtime-installer-061-top-level-design-review.md)
- 当前设计 authority：[Runtime Workshop Installer Boundary](../../../../architecture/runtime-workshop-installer-boundary.md)
- 当前测试 authority：[Workshop Package Subscription Test Matrix](../../../../workflows/workshop-package-subscription-test-matrix.md)
- 实现 Update：[20260807-0002 Runtime installer 0.6.1 simplification](../../../updates/2026/20260807-0002-runtime-installer-061-simplification.md)
- 相关 Debug：[ISSUE-022 Runtime tools move access denied](../../../../debug/issues/ISSUE-022-20260807-runtime-tools-move-access-denied.md)

本次只在仓库生成包副本和临时假游戏目录中测试，没有安装到真实 Doloc Town，没有启动游戏，没有改动 Steam 订阅目录、官方 `MODS` 或玩家存档。依据 Runtime Workshop 审计规则，真实游戏测试不在本次授权范围内。

## 一、结论

四入口设计本身可以保留：一个薄 BAT 对应一个明确动作，共享 CMD 只选择主机，PowerShell 动作脚本拥有语义。`0.6.1` 去掉普通包中的 Player Doctor EXE、保留固定哈希 BepInEx 来源、缩小日志默认收集量、继续使用五 DLL 收据和同卷事务，方向正确。

但当前候选不能作为“安装器已验证完成”或发布批准，存在以下结论：

1. **P1：PowerShell 主机探测存在确定的假阳性。** CMD 只依据候选进程退出码 `0` 判定探测成功。显式把主机指向 `cmd.exe` 时，探测脚本完全没有执行，安装和检查却都能打印成功；普通候选回退也会被一个返回 `0` 的伪 PowerShell 截断。这直接违反“无效显式主机必须在变更前失败”和“无效普通候选必须继续回退”的当前架构语义。
2. **P2：同一游戏目录没有跨进程安装锁。** 四个并发修复实例中只有一个成功，另三个分别读取到尚未写完/正被占用的事务收据或与另一个实例的目录移动冲突。最终状态仍健康且无事务残留，但各实例会把另一个仍活跃的事务当作“中断事务”，原子性依赖调度时序。
3. **P2：卸载不处理合法的未完成 Runtime 事务。** 在与现有事务恢复测试相同的合法 `transaction.json` 状态下，公共卸载返回 `0`，却保留游戏根下的完整旧五 DLL、`tools`、组件和收据恢复副本。卸载不能一边宣称完成，一边留下未来安装会自动恢复的 DTMAPI-owned Runtime 状态。
4. **P2：桌面日志目录名只有秒级精度。** 两次同秒导出会使用同一目录并通过 `New-Item -Force` 复用，可能混合或覆盖一次支持证据。
5. **发布门槛仍未满足。** `30fc6db6` 改了游戏加载的 `DTMAPI.Core` 启动诊断分支，但本候选没有真实游戏启动日志；ISSUE-022 也仍缺受影响玩家在正常防护下的验收。Update 保持 `implemented` 是正确的，不能提升为 `verified` 或授权发布；其头部 `Runtime Validation: passed` 只能解释为“隔离 Runtime 安装矩阵通过”，否则会与正文“没有启动真实游戏”冲突。

顺序与故障路径的结果是好的：五轮共 30 次真实 BAT 动作全部得到预期返回码；被占用的 `DTMAPI\tools` 会失败、完整回滚、保持健康并可重试；损坏的包内 BepInEx ZIP 会拒绝后走固定官方 URL，安装及检查通过；第三方 BepInEx plugin/config 哨兵未改变。

## 二、冻结的四项玩家语义

以下语义应先于具体脚本实现，后续修复不得用“脚本执行结束”代替玩家动作成功。

### 2.1 安装

公共入口是 `1_install_dtmapi.bat`，其语义为：

1. 在任何游戏目录变更前选择一个真正执行并通过能力探测的 PowerShell 主机；Windows PowerShell 5.1 优先，PowerShell 7 只在探测阶段回退。
2. 显式无效 `DTMAPI_GAME_DIR`、无效包、游戏运行中、能力不足、事务失败或提交后校验不一致均返回非零。
3. 已完整的 BepInEx 可以复用；否则先使用包内固定哈希 ZIP，再使用固定官方 URL，并在解压前校验同一固定哈希。未知第三方 plugin/config 不属于 DTMAPI。
4. 只安装五个 Runtime DLL、当前可选 Compatibility Host、玩家工具和双收据；不安装或管理功能 Mod，不把 Player Doctor EXE 放进普通 Runtime，不启动游戏。
5. DTMAPI-owned Runtime 通过同卷事务提交，失败必须恢复提交前的完整旧状态。只有提交后状态检查通过才可返回 `0` 和显示“安装成功”。
6. 同一游戏目录的并发调用必须串行化或让后来的调用在变更前明确返回“已有安装正在进行”；不得把仍活跃的事务当成崩溃恢复对象。

### 2.2 卸载

公共入口是 `2_uninstall_dtmapi.bat`，其语义为：

1. 只卸载 DTMAPI Runtime 所有权范围：`BepInEx\plugins\DTMAPI`、Compatibility Host、安装工具、release/install receipts，以及属于同一 Runtime 的待恢复事务状态。
2. 默认保留 BepInEx、External BepInEx Plugin、官方 Local/Workshop Mods、`mod_infos.json`、报告、配置和备份。
3. 未安装、部分安装或损坏收据时仍应幂等；若存在可验证的中断事务，应先按既有恢复规则协调到一个稳定状态再卸载，或明确返回非零并说明保留了什么，不能静默忽略。
4. “从未安装”返回 `0` 可以接受；若为了支持审计写一份 `uninstall-state`，输出必须说“未发现可卸载 Runtime”，不能声称实际删除了文件。
5. 卸载成功不等于游戏健康，卸载后检查应因缺少 Runtime 返回非零。

### 2.3 检查

公共入口是 `3_check_dtmapi_status.bat`，其语义为：

1. 完全只读，不修复、不安装、不启动游戏。
2. 校验游戏目录、BepInEx、精确五 DLL、版本/哈希/来源收据、Compatibility Host 和必要工具；Player Doctor 缺失只提供信息。
3. 当前完整健康状态返回 `0`；缺失、损坏或无法证明的状态返回 `1`；已定义的版本漂移返回 `2`。
4. 卸载后或从未安装不能打印“检查通过”。主机探测自身也必须证明目标 `.ps1` 确实执行过，而不能只继承任意进程的退出码。

### 2.4 导出日志到桌面

公共入口是 `4_collect_dtmapi_logs.bat`，其语义为：

1. 尽力收集而不是严格安装：游戏、Runtime、某类日志或可选诊断器缺失，不应阻断其他证据。
2. 默认写入 `Desktop\DTMAPI-logs\<唯一时间目录>`；桌面不可写时回退到临时目录，并明确打印实际路径。
3. 默认当前文本日志尾部最多 4 MiB、最新三份历史日志各最多 2 MiB、Player/Unity 文本日志有界；除非显式 `-IncludeCrashDumps`，不复制 `.dmp`。
4. 对游戏目录和 Runtime 状态只读，只写支持输出。每次调用必须得到独立目录，即使同秒或并发启动也不能复用另一份证据目录。

## 三、三次提交逐项审查

### 3.1 `30fc6db6` — `fix(installer): simplify runtime workshop 0.6.1`

这次提交包含 45 个文件、`+1313/-2167` 行，是实质重做：

- 普通包从五入口减为四入口，删除无人使用的预检 BAT/脚本；
- Player Doctor 从普通包、安装收据、状态硬要求和默认启动外部进程中移出；
- 四 BAT 汇入一个 CMD，改为 Windows PowerShell 5.1 优先；
- BepInEx 改为玩家两来源、固定哈希、逐文件备份/回滚；
- 默认日志收集增加数量和字节上限；
- 保留五 DLL、Compatibility Host、双收据、同卷事务和路径绑定恢复；
- 新增专项包矩阵并更新 `0.6.1` 版本 authority。

正面评价：职责方向与上一份顶层设计 Review 一致；真实 PS5.1、特殊路径、离线 BepInEx、事务 fault matrix 和无 EXE 边界均有可复现证据。

主要问题：

- 共享 CMD 的主机证明协议只剩退出码，没有“该脚本确实由该主机执行”的返回凭据；新增专项矩阵又始终强制一个已知良好的 Windows PowerShell，未覆盖架构已经要求的无效 override 和普通回退。
- 安装事务没有跨进程 owner/lock，启动时直接枚举所有 `.dtmapi-runtime-install-*` 并当作中断事务恢复。
- 卸载目标仍只有五类 live 路径，没有中断事务协调。
- 玩家包仍需解析约 8,226 行 PowerShell；其中 `common.ps1` 1,659 行、`release-common.ps1` 1,059 行、`install-to-game.ps1` 2,435 行。删除无人入口与 Doctor 已显著减包，但上一 Review 要求的 player-only library 拆分还没有完成。这个是后续结构债，不应在本次紧急修复中盲目大拆，但不得继续增长玩家不使用的开发函数。
- 该提交同时修改了游戏加载的 `DtmApiRuntime.RunPlayerDoctorAtStartup` 路径，因此纯安装器假目录测试不能替代一次真实启动验证。

### 3.2 `eed21df7` — `fix(installer): preserve host probe arguments`

这是正确且边界清楚的一行功能修复：把以反斜杠结尾的 `%~dp0` 目录作为 `tools\.` 传给原生 PowerShell 参数解析，避免末尾 `\` 吞掉闭合引号并把后续 `-Action` 合并到同一参数。当前特殊路径矩阵已持续从真实 BAT 入口保护这个行为。

这个提交修复的是 native argument quoting，不修复本 Review 发现的“错误进程返回 0 即被认作 PowerShell”。两者必须分开，不能用 `eed21df7` 的特殊路径 PASS 推导主机身份验证也正确。

### 3.3 `905dccc3` — `docs(installer): record 0.6.1 candidate validation`

这是文档提交，没有改变候选包字节。它准确记录了：

- 包嵌入提交 `eed21df7eaec`，具体 ZIP 长度和 SHA-256 由实现 Update 的 Final Candidate 段持有；
- 27 文件、四 BAT、九 PS1、五 Runtime DLL、零 EXE；
- 没有真实游戏启动、没有发布、ISSUE-022 仍等待受影响玩家。

不足是验收记录没有实际覆盖架构 Required Package Tests 第 9 项；本 Review 已证明该项不是遗漏一条测试名称，而是能产生“未安装却报告安装成功 / 未检查却报告检查通过”的真实缺陷。因此 `Runtime Validation: passed` 不应被读作完整 Runtime/发布验收，后续实现 Update 必须补上该测试后再更新状态。

## 四、逐项发现与根因

### F1 — P1：主机探测只看退出码，可把非 PowerShell 认作成功

代码事实：

- `invoke-dtmapi-action.cmd` 的 `:dtmapi_try_powershell` 调用候选进程后，只把 `%ERRORLEVEL% == 0` 转成 `DTMAPI_POWERSHELL=<candidate>`。
- `probe-powershell-host.ps1` 最后打印 `[INFO] Probe OK.` 并退出 `0`，但 CMD 不验证这条输出，也没有一次性 challenge/nonce 或其他绑定结果。
- 选择后的真实动作同样只继承候选进程退出码，所以同一个错误进程可以连续让 probe 和 action 都“成功”。

压力证据：

- 显式 `DTMAPI_POWERSHELL_HOST=C:\Windows\System32\cmd.exe`：退出 `0`，输出 `[OK] DTMAPI install finished` 和 `Host used: cmd.exe`；假游戏目录只有原始 `DolocTown.exe`/`DolocTown_Data`，没有任何 DTMAPI 或 BepInEx 变更。
- 普通候选测试把首选 `...\WindowsPowerShell\v1.0\powershell.exe` 替换为 `cmd.exe`，并保留可用 PowerShell 7：错误候选仍被接受，检查返回 `0`，没有进入 PowerShell 7。
- 完整输出保存在 [prefix stress Results](../../../../../tmp/test-runs/installer-three-commit-audit/edge/edge-stress-9dbb1968b3b34f3ea279c9bffec50e40/Results/prefix-stress-summary.md)。

排除项：这不是 PowerShell 5.1 语法、分号路径或 `tools\` 末尾反斜杠问题；同一个候选包在真正 PS5.1 下的五轮矩阵全部通过。

验收门槛：

1. probe 返回值必须携带并由 CMD 验证“本次探测确实执行”的不可复用标记，且绑定当前候选、动作和工具根；不能只检查退出码。
2. 显式 `cmd.exe`、一个无输出但返回 `0` 的假主机、返回非零的假主机都必须在任何游戏变更前失败。
3. 普通候选遇到上述两类假主机后必须继续到真实 PowerShell 7，且动作只由最终主机执行一次。
4. 安装/检查的成功输出必须来自真实动作结果；失败主机不能打印公共成功语句。

### F2 — P2：并发安装会把活跃事务当作中断事务

代码事实：`install-to-game.ps1` 在创建本次事务前枚举游戏根全部 `.dtmapi-runtime-install-*`，读取其 `transaction.json` 并恢复；当前没有按游戏目录绑定的跨进程锁或事务 owner 存活判断。

压力证据：对一个已健康安装的假游戏同时启动四个 `1_install_dtmapi.bat`：

- 返回码为 `0,1,1,1`；
- 一个失败实例读到另一个仍写入中的收据并得到 sharing violation；
- 一个看到事务目录但收据尚未出现；
- 一个与另一个实例同时放置 candidate plugin，得到“文件已存在”；
- 最终检查返回 `0`，第三方哨兵未改，未留下事务目录。

这次调度没有破坏最终状态，但不能证明所有调度都安全；主动恢复另一个活进程的事务本身已经越过所有权边界。

验收门槛：在扫描旧事务之前取得按规范化游戏目录绑定的跨进程锁，并持有到提交/回滚/清理结束。四实例测试应只有一个执行变更，其他实例确定性等待或在变更前返回清晰的“安装正在进行”，不得读取、恢复或写 failure receipt 到另一个活事务。

### F3 — P2：卸载成功后仍保留完整 Runtime 恢复状态

代码事实：`uninstall-dtmapi.ps1` 只处理 live plugin、components、tools、release manifest 和 install state；没有读取游戏根 `.dtmapi-runtime-install-*` 或状态根 `.runtime-install-transaction-*`。安装脚本下一次运行却会自动恢复这些目录。

压力证据：使用跟踪事务矩阵同 schema、同路径绑定、阶段为 `InstallStateCommitted` 的合法中断事务，然后运行公共卸载：

- 卸载返回 `0`；
- live candidate 路径被卸载；
- 游戏根仍保留 `recovery-plugin` 中的完整五 DLL；
- `DTMAPI` 下仍保留旧 tools、Compatibility Host、release/install receipts；
- 证据见 [tail stress summary](../../../../../tmp/test-runs/installer-three-commit-audit/edge/edge-stress-1e7a3e40030c4b4ba7b7f1afacb31a4d/Results/edge-stress-summary.md) 与同目录 `pending-after-uninstall-tree.txt`。

验收门槛：对事务各阶段分别测试卸载。要么先用既有路径绑定逻辑恢复到稳定旧状态再做 Runtime-only 卸载；要么在无法安全判断时返回非零、列出保留路径，且绝不声称卸载完成。成功结果下不得留下可被下一次安装自动恢复的 Runtime DLL/tools/components/receipts。

### F4 — P2：桌面日志导出目录同秒冲突

代码事实：`collect-logs.ps1` 使用 `yyyyMMdd-HHmmss` 生成目录，并用 `New-Item -Force` 创建；没有毫秒、GUID、独占创建或冲突重试。

影响：玩家同秒双击、支持人员并发执行或前一次收集仍未结束时，两次调用会写入同一证据目录。即使单文件复制大多是原子覆盖，最终目录也不能再证明来自哪一次调用。

验收门槛：目录名加入毫秒和随机后缀或使用独占创建循环；并发两个公共日志 BAT 必须产生两个不同目录，两者各自有完整 summary，且安装/游戏目录保持只读。

### F5 — P3：从未安装时的卸载输出不够诚实

压力证据：对有效但从未安装 DTMAPI 的假游戏运行卸载，返回 `0` 并只创建 `DTMAPI\uninstall-state-<秒>.json`，随后仍输出“Uninstalled DTMAPI-owned runtime files”。

保留一份支持收据可以接受；问题是输出没有区分“删除了 Runtime”和“本来就没有 Runtime”，而秒级收据名也可能同秒覆盖。后续应输出 removed count/no-op 状态，并给收据名增加唯一后缀。

## 五、压力测试结果

| 测试 | 结果 | 关键证据 |
|---|---|---|
| 生成包结构与 PS5.1 parser | PASS | 27 文件、四 BAT、九 PS1、五 Runtime DLL、零 EXE；九脚本 PS5.1 parser 全过 |
| 标准订阅包矩阵 | PASS | 缺失/空目标按预期失败；安装、检查、收集、卸载通过；卸载后检查返回 1；零 blocker |
| 专项特殊路径矩阵 | PASS | `Program Files (x86);中文` 包路径到含括号、`&`、中文、分号的游戏路径；BepInEx rollback、离线安装、日志上限和第三方保持均通过 |
| Runtime 事务矩阵 | PASS | PowerShell 7 17 项 + Windows PowerShell 5.1 17 项，共 34 项 fault/recovery |
| 五轮公共 BAT 循环 | PASS | 30 个动作返回码全部符合语义；每轮 install/check/repair/check/uninstall 为 0，卸载后 check 为 1 |
| 真实 tools 文件占用 | PASS | repair 返回 1；旧安装检查仍为 0；释放后 retry/check 为 0；无事务残留 |
| 四进程并发 repair | DEGRADED | `0,1,1,1`；最终 check 0、无残留，但发生跨事务读取和目录移动冲突 |
| 显式无效主机 | FAIL | `cmd.exe` 未执行 probe/action，却返回安装成功 0；无安装文件 |
| 普通无效候选后回退 | FAIL | 假 `powershell.exe` 被当作成功，检查假成功 0，未到 PowerShell 7 |
| 损坏包内 BepInEx ZIP | PASS | 拒绝损坏 ZIP，固定官方网络 fallback，install 0、check 0、summary 为 `official-network-fallback` |
| 从未安装时卸载 | OBSERVED | exit 0，创建一份 uninstall-state；没有无关删除 |
| 合法中断事务时卸载 | SEMANTIC FINDING | exit 0，但两组 recovery 根和完整旧 Runtime 状态仍在 |
| 默认日志上限 | PASS | 当前日志不超过 4 MiB、历史仅三份且各不超过 2 MiB、默认无 dump |
| 真实游戏启动 / 受影响玩家 | NOT RUN | 本次未获真实游戏测试授权；ISSUE-022 保持 open/mitigated |

主要证据目录：

- [五轮、文件占用、并发与主机选择](../../../../../tmp/test-runs/installer-three-commit-audit/edge/edge-stress-9dbb1968b3b34f3ea279c9bffec50e40/Results/prefix-stress-summary.md)
- [网络 fallback、no-op 卸载与中断事务卸载](../../../../../tmp/test-runs/installer-three-commit-audit/edge/edge-stress-1e7a3e40030c4b4ba7b7f1afacb31a4d/Results/edge-stress-summary.md)
- [默认订阅包审计](../../../../../tmp/test-runs/installer-three-commit-audit/subscription/DTMAPI%20Workshop%20Audit%2020260807-224713/Results/stress-summary.md)
- 专项矩阵保留根：`tmp/test-runs/installer-three-commit-audit/focused/runtime-installer-061-7877cc7cc5bf4d9d9e1c799b6feaf6e9`

说明：扩展 runner 在完成五轮、文件占用、并发和两个主机案例后被外层 120 秒命令时限终止；这些案例的逐项输出此前已经落盘，`prefix-stress-summary` 是随后从保留输出和最终文件树重建的非权威汇总。网络/卸载 tail 在新的隔离根完整运行。两段结果只服务本次审查，不拼接为一次正式完整套件 PASS。

## 六、下一步边界

本 Review 不授权直接修改或发布。若开始实现，应创建一个安装器 Update，并按最小顺序处理：

1. 先修 P1 主机身份/探测成功证明，并把无效 override、返回 0 假主机、普通回退加入真实 BAT 包矩阵。
2. 再增加每游戏跨进程安装锁，补四实例并发矩阵。
3. 明确中断事务下的卸载策略，复用既有路径绑定和恢复代码，补各事务阶段卸载测试。
4. 给桌面导出和 uninstall-state 使用唯一目录/文件名，并修正 no-op 提示。
5. 重新构建候选，先跑受影响的 focused gates，再跑一次干净的完整安装器/订阅包矩阵。
6. 候选冻结后，另行取得一次真实游戏启动/第五存档证据，并由受影响玩家在普通防护下验收；这两项通过前不得关闭 ISSUE-022 或发布。

当前包可以继续作为未发布的诊断候选保存，但不能把本次局部 PASS 解释成最终安装器验收。
