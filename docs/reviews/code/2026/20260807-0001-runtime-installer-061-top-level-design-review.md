# DTMAPI 0.6.1 Runtime Workshop 安装器顶层设计与代码审查

- 日期：`2026-08-07`
- 状态：`recorded`
- 性质：安装器控制流、PowerShell 5.1 兼容性、BepInEx 来源、Player Doctor 交付边界与复杂度审查；不是修复完成证明
- Source：用户确认无 EXE 小包能够安装，要求解释安装程序每一步、BepInEx 安装来源与 EXE 职责，并在 0.6.1 前重做顶层设计、审查冗余/过度设计/过度防御和 PowerShell 问题
- 相关 Manual QA：[Runtime tools transaction access-denied player failures](../../manual-qa/2026/20260807-0001-runtime-tools-move-access-denied.md)
- 相关 Debug：[ISSUE-022](../../../debug/issues/ISSUE-022-20260807-runtime-tools-move-access-denied.md)
- 当前设计 authority：[Runtime Workshop Installer Boundary](../../../architecture/runtime-workshop-installer-boundary.md)
- 后续实现：实施开始后由单独的 `0.6.1` Update 负责

本次没有安装到真实游戏、没有启动游戏、没有修改 Steam 订阅目录、官方 `MODS` 或存档。审查对象是当前仓库源码、当前生成的 `0.6.0` Runtime 包、已有玩家日志和无 EXE 小包反馈。

## 一、审查结论

当前安装器的主要问题不是“BAT + PowerShell 天生不可靠”，也不是现有脚本存在普遍的 PowerShell 5.1 语法错误。真正的结构性问题是三类职责逐步叠加到了同一玩家链：

1. 普通玩家 Runtime 安装、检查、卸载；
2. 仓库开发者构建、Git/worktree/runtime-lock、官方本地产品发布；
3. 深度诊断、Player Doctor、启动证据、Unity dump 与大体积历史日志收集。

结果是普通玩家双击一个 BAT 时，需要解析和携带大量不会执行的开发函数；一个非 Runtime 必需的 65 MB 自包含 EXE 又被放进强制工具目录事务。前者放大 PowerShell 版本/语法回归面，后者已经把可选诊断能力变成真实安装阻断。

`0.6.1` 应以“玩家安装链最小化”为主线，而不是再增加更多探测层：

- Player Doctor 从普通 Runtime 包、安装事务和“Runtime 是否完整”的判定中移出；保留为开发/作者/按需支持工具。
- 四个 BAT 继续只做动作选择和返回码保留；共享 CMD 只做主机选择，不再用分号拼接脚本路径。
- Windows PowerShell 5.1 作为 Windows 内置首选主机；探测失败时才回退到 PowerShell 7，任何变更动作只执行一次。
- 玩家包只携带玩家执行所需的 PowerShell；开发发布、Git、存档测试、运行锁和无人入口预检不再属于玩家包。
- BepInEx 对玩家只有“包内固定哈希 ZIP”与“固定官方 URL 下载”两个有效安装来源；完整现有安装直接跳过。
- 保留五个 Runtime DLL 的候选验证、哈希/版本收据、同卷目录交换和路径绑定回滚。
- 普通日志收集必须有小体积上限；dump 和完整历史只在显式深度模式收集。

## 二、Player Doctor EXE 是什么

`dtmapi-player-doctor.exe` 不是安装器、不是 BepInEx、不是 DTMAPI 游戏 Runtime，也不参与五个 Runtime DLL 的加载。它是一个只读离线诊断器：

- 扫描 `BepInEx/plugins` 与 `Mods`；
- 读取 PE/CLR 元数据而不加载待检查程序集；
- 区分 DTMAPI Runtime、Strict/Advanced CodeMod、External BepInEx Plugin、原生/损坏/未知 DLL；
- 检查错放、最低 Runtime 版本、目标框架、Advanced receipt/policy 与版本收据；
- 输出 JSON、文本和一行摘要；不安装、移动、删除或修复任何玩家文件。

它使用 `.NET 8 win-x64 self-contained single-file` 发布，因此玩家不需要预装 .NET，但 EXE 本身约 65 MB 且未做商业代码签名。当前普通 Runtime 包约 71.6 MB；去掉 Doctor 三文件后约 3.9 MB，Doctor 占绝大多数体积。

当前接入有三处：

1. 包生成器把 EXE 与两份 .NET notice 放入 `Content/DTMAPIInstaller/tools/player-doctor`；
2. 安装器把三文件作为强制候选，随整个 `DTMAPI/tools` 目录原子移动；状态检查也把缺少它判成当前 Runtime 不完整；
3. 游戏 Runtime 启动后尝试运行一次，预算 5 秒，失败/缺失只记 `unavailable` 警告，游戏继续加载。

第三点证明它在运行时本来就是可选诊断；前两点却把它升级成强制安装组件，职责不一致。两位玩家的错误都落在包含该 EXE 的 `candidate/tools -> DTMAPI/tools` 目录移动；正常包在关闭防护后能装，无 EXE 小包也已有成功反馈，但因为小包测试时的防护状态未报告，仍不能声称 EXE 已被单变量证明为唯一触发物。能确定的是：普通 Runtime 不应再因这个可选 EXE 失败。

## 三、当前安装程序逐步控制流

### 3.1 BAT 与 PowerShell 主机选择

1. 玩家双击四个根 BAT 之一。
2. BAT 强制 `cmd.exe /d /e:on /v:off`，关闭 AutoRun、打开命令扩展、关闭延迟展开，随后 `call` 共享 `invoke-dtmapi-action.cmd`。这一层现在只有 11 行，是合理的薄入口。
3. 共享 CMD 将动作映射为 `install`、`uninstall`、`status` 或 `collect`，并确定目标 `.ps1`。
4. CMD 为候选 PowerShell 主机逐个运行 `probe-powershell-host.ps1`。探测会检查 FullLanguage、JSON round-trip、便携 SHA-256、安装时的 ZIP 支持，并用 AST 解析本动作所需脚本。
5. 第一个探测通过的主机只执行一次真实动作。变更开始后不会换另一个主机重试，这是必须保留的安全边界。
6. 安装动作调用 `install-to-game.ps1 -InstallBepInEx`；日志动作调用 `collect-logs.ps1 -CaseId PLAYER-CRASH -DesktopTimestampOutput`；CMD 保留退出码并 `pause`。

### 3.2 游戏目录解析

当前 PowerShell 按以下顺序找游戏：

1. `DTMAPI_GAME_DIR`；若设置但无效，立即报错，不静默回退。
2. `local.settings.json` 的 `GameDir`。
3. Steam 注册表根、`libraryfolders.vdf` 与 `appmanifest_2285550.acf`，再假定目录名为 `steamapps/common/Doloc Town`。

当前缺口：没有利用“订阅包自身已经位于某个 Steam library 的 workshop 路径”这一强证据，也没有从 appmanifest 读取 `installdir`；HKLM 也只读取 `SteamPath`。这不会解释已知 tools access-denied，但会制造一部分“包在某盘、游戏在同盘却找不到”的兼容风险。

### 3.3 包与候选文件校验

1. 安装器发现同级 `Payload` 后进入玩家包模式，禁止构建与官方本地 Mod 发布。
2. 读取 `Content/DTMAPI/release-manifest.json`，校验 schema、DTMAPI 版本、binary version、`PackageKind=workshop-runtime` 和 Git build commit 格式。
3. 要求 Runtime payload 恰好是五个生产 DLL，不允许多 DLL、QA host、普通 Mod 或测试设置混入。
4. 对五个 DLL 校验非空、程序集名、FileVersion、长度与 SHA-256 收据。
5. 对 dormant compatibility component 校验目录、程序集身份、`netstandard2.0`、版本、长度与哈希。
6. 对安装链脚本做严格语法检查；支持/诊断脚本在构包阶段原本是 warning-only，但主机探测仍会解析当前动作涉及的脚本。
7. 当前版本还强制校验 Player Doctor 三文件、EXE ProductVersion/FileVersion 与哈希。

### 3.4 BepInEx 安装

1. 先验证现有 BepInEx 的 16 个必需文件、非空状态、Doorstop enabled 与目标程序集。
2. 若完整，直接跳过，不覆盖。
3. 若缺失/不完整且入口传入 `-InstallBepInEx`，调用 `install-bepinex.ps1`。
4. 固定 BepInEx 版本 `5.4.23.5`，所有来源都必须匹配固定 SHA-256。
5. 使用自有 .NET `ZipArchive` 解压，并校验每个 entry 的目标路径没有逃出解压目录。
6. 备份现有目标，再复制到游戏根，最后重新验证 16 个文件与 Doorstop 配置。

### 3.5 Runtime 事务提交

1. 在游戏 `DTMAPI/.runtime-install-transaction-<id>` 创建候选区、恢复区与 `transaction.json`。
2. 先完整写入候选五 DLL、icon、optional component、helper scripts、version authority、release/install receipts；当前版本还写入 Doctor。
3. 若发现上次未提交事务，先逐项校验 receipt 中所有绝对路径仍属于本次游戏/state/plugin 边界，再做回滚；不会相信任意 JSON 路径。
4. 提交顺序为：旧 Runtime plugin 到 recovery、候选 plugin 到 live；旧 components 到 recovery、候选 components 到 live；旧 tools 到 recovery、候选 tools 到 live；随后交换 release manifest 与 install state。
5. 每个阶段更新事务状态。所有 live 文件再做一次版本、哈希、精确集合与收据验证。
6. 全部通过才标记 committed 并删除 recovery；任何异常由 trap 尝试回滚并写 `install-state.failed-*.json`。

`candidate/tools -> live/tools` 正是玩家 access-denied 的阶段。把 EXE、支持脚本和 Runtime DLL 放在同一个“全成或全败”的安装事务中，扩大了必需 Runtime 的失败面。

### 3.6 检查、收集与卸载

- 状态检查验证游戏路径、BepInEx、五 DLL、QA residue、安装/发布收据、版本 authority、optional component、installed helper syntax；当前还强制验证 Doctor 三文件并运行 Doctor。
- 日志收集尝试运行 Doctor，然后复制当前 DTMAPI/BepInEx/Unity 日志、最多 10 个 DTMAPI 历史日志、安装失败/卸载状态、Steam 尾部日志与 Unity crash tree。默认允许单个 dump 384 MB、含 dump 总计 512 MB。
- 卸载只移动 DTMAPI plugin、components、tools 和收据到备份；默认保留玩家 Mods、报告、配置和备份。只有 install-state 明确记录由 DTMAPI 安装 BepInEx 时，显式参数才允许移除 BepInEx。

## 四、BepInEx 到底有几个安装路径

代码里有三个“取 ZIP 分支”，但普通玩家包实际只有两个有效来源：

| 层次 | 来源 | 普通玩家包是否存在 | 说明 |
| --- | --- | --- | --- |
| 1 | `Content/.tools/bepinex/BepInEx_win_x64_5.4.23.5.zip` 缓存 | 是 | 构包时复制进去，离线首选 |
| 2 | `<repo>/tools/release/bootstrap/...zip` 源树 fallback | 否 | 只在仓库开发树存在；普通包不带这个目录 |
| 3 | BepInEx GitHub 固定 release URL | 是 | 本地包不存在/损坏时的网络 fallback，下载后仍校验固定哈希 |

此外，“现有 BepInEx 已完整，直接跳过”是一个结果，不是第四个安装来源；“解压失败后重试一次”也是重试，不是新来源。

因此玩家视角的准确答案是：**两条安装来源——包内离线 ZIP、官方固定 URL 下载；另有一条只服务仓库开发树的源码 fallback。** 当前测试曾人为把 ZIP 放到源码 fallback 位置，这会让测试看起来覆盖了玩家并不存在的第三条路，0.6.1 应把开发来源与玩家来源明确拆开。

## 五、代码审查发现

### P0：可选 EXE 被错误提升为 Runtime 强依赖

- Doctor 约占包体积 95%，不参与游戏 Runtime 加载。
- Core 已把缺失/失败定义为非致命，却由构包、安装、状态、事务把它定义为强制。
- 它使整个 `tools` 目录更容易被实时扫描/行为防护占用；已有两台玩家机器在这个提交边界失败。
- 0.6.1 必须从普通包和 Runtime 完整性判定移出，而不是只增加 `Directory.Move` 重试。重试可以缓解瞬时锁，但不能修正错误依赖关系。

### P0：普通日志收集没有普通玩家体积边界

- `D:\下载\DTMAPI-logs友利奈绪` 与第二份重采集各约 `30.35 MiB`；主要是一个 7.3 MB 当前日志与多个 1.5–7.2 MB 历史日志被原样复制。
- 代码还允许默认复制 384 MB dump、总计 512 MB；对普通 QQ/工单发送不现实。
- 0.6.1 默认应输出有上限的文本尾部与少量历史，dump 只记录路径并由显式深度参数收集。

### P1：主机探测用分号序列化绝对路径

- CMD 将脚本绝对路径用 `;` 拼成一个 `ProbeList`，PowerShell 再 `-split ';'`。
- 分号是合法 Windows 文件名字符，因此合法 Steam/library 路径含 `;` 时会被拆成虚假脚本路径。
- 这不是 PowerShell 语法错误，而是跨进程参数协议错误。应只传 `ToolsRoot + Action`，由 probe 在 PowerShell 内生成固定脚本列表。

实现后的特殊路径矩阵又暴露了同一边界的第二个 Windows 原生参数陷阱：`%~dp0` 固定以反斜杠结尾，把它直接作为带引号的 `-ToolsRoot "...\"` 参数传给 `powershell.exe` 时，闭合引号会被末尾反斜杠吞并，后续 `-Action` 被并入同一参数并表现为“缺少 mandatory Action”。这与目录中的分号无关；CMD 必须传递语义相同但不以反斜杠结尾的 `tools\.`，专项矩阵必须从真实 BAT 入口持续保护该行为。

### P1：玩家脚本解析了大量无关开发代码

当前物理规模：

- `common.ps1`：1611 行；
- `release-common.ps1`：1059 行；
- `install-to-game.ps1`：2484 行；
- `check-dtmapi-status.ps1`：874 行；
- `collect-logs.ps1`：1474 行；
- `probe-install-preflight.ps1`：1054 行。

`common.ps1` 同时包含 Git/worktree/runtime lock、.NET SDK 获取、存档快照、游戏 smoke 与玩家 hash/ZIP/path 函数；`release-common.ps1` 同时包含产品 catalog/官方发布与玩家 receipt 函数；`install-to-game.ps1` 同时包含玩家 Runtime 安装和官方本地/Author SDK 产品发布。

包模式虽然跳过后半段，但 PowerShell 必须先解析整个文件。于是任何与玩家无关的开发函数语法/StrictMode 回归都可能让四个玩家入口在执行前失败。0.6.1 至少应停止打包无人入口的 `probe-install-preflight.ps1`，并建立 player-only library/entry 的迁移边界；若一次性抽取风险过高，可以先兼容拆分、后删除旧开发入口，但不能继续把新增开发逻辑写进玩家共享库。

### P1：BepInEx 恢复策略会放大占用与体积

- 解压第一次失败时，即使 ZIP 哈希正确，也会删除唯一离线缓存，再走重建/网络逻辑。应先对同一已验证 ZIP 清空临时目录后重试；只有哈希或 archive integrity 证明确实坏了才替换源。
- 当前按 ZIP 顶层项备份；BepInEx ZIP 顶层包含整个 `BepInEx` 目录，因此会递归复制玩家的第三方 plugins/config/cache，可能很大且更容易遇到锁。应只备份安装包实际将覆盖的相对文件，不复制未知第三方内容。

### P1：游戏目录解析没有使用最近、最强的本地证据

- 订阅包路径本身可反推当前 Steam library；当前实现忽略它。
- appmanifest 存在时仍硬编码 `Doloc Town`，没有读取 `installdir`。
- 0.6.1 应优先从包根推断同库 `steamapps/common/<installdir>`，再走注册表与 VDF，并保留显式环境变量最高优先级。

### P2：状态与卸载存在不必要或误导代码

- 状态脚本对 Doctor 的校验比五 Runtime DLL 的 receipt 校验更强，优先级倒置；Doctor 移出后应聚焦 Runtime、BepInEx、版本与收据一致性。
- 状态脚本递归扫描整个 state tree 查 QA residue，可能触碰大型/锁定的报告与备份；应只扫描定义过的 live 目录。
- 卸载参数 `KeepReports`、`KeepConfigs`、`KeepBackups` 实际始终保留，`RemoveOfficialLocalPackages` 已退役，`$errors` 也没有被填充；应删除或明确兼容弃用，避免伪选项。

## 六、PowerShell 5.1 审查

本次在真实 `Windows PowerShell 5.1.26100.8875` 下用 AST 解析了当前玩家包涉及的十个 `.ps1`，全部通过；当前脚本也没有直接调用 `Get-FileHash` 或 `Expand-Archive`，SHA-256 与 ZIP 已使用 .NET 便携实现。因此当前未发现一个“标准 5.1 必现的语法错误”。

风险来自以下方面：

- 巨型共享文件导致无关代码也必须解析；
- CMD 到 PowerShell 的分号路径协议；
- PowerShell 7 优先使玩家环境差异变多；
- StrictMode + 多角色脚本让数组展开、缺属性、空结果等运行时差异难以穷尽；
- 测试有时构造了生产包不存在的源码 fallback，掩盖真实分支数。

下列防御不是过度设计，应保留：

- BAT 强制 `/d /e:on /v:off` 与所有路径引号；
- 主机在变更前完成能力/语法探测，变更后不跨主机重试；
- 使用 `-LiteralPath` 和 `.NET Path.GetFullPath`；
- BepInEx 固定版本、固定 SHA-256 与 ZIP-slip containment；
- 五 DLL 精确集合、程序集身份、FileVersion、长度和哈希；
- 事务 receipt 的路径绑定校验、同卷目录交换和回滚；
- 安装期间拒绝游戏进程正在运行。

## 七、0.6.1 目标顶层设计

```text
4 个 BAT（动作名、返回码、pause）
        |
共享 CMD（WinPS 5.1 首选；PS7 fallback；只探测，不安装）
        |
PowerShell player action
        |-- resolve：游戏路径 + 包身份
        |-- verify：五 DLL / manifest / hash / version / BepInEx ZIP
        |-- prepare：同卷候选目录，不碰 live
        |-- optional prerequisite：BepInEx 离线 ZIP，必要时固定 URL
        |-- commit：Runtime / component / 小型 helper / receipts
        `-- verify + rollback receipt

Player Doctor / 完整 dump / 作者策略诊断
        `-- 独立、按需支持工具，不参与 Runtime 安装成败
```

### 普通包必须包含

- 四个薄 BAT；
- 一个共享 CMD 和一个 host probe；
- 玩家 install/status/collect/uninstall 所需脚本；
- 五个 Runtime DLL、一个 dormant compatibility component、release manifest、icon；
- 固定哈希的 BepInEx ZIP。

### 普通包不得包含

- `dtmapi-player-doctor.exe` 与其 self-contained notices；
- 无公开入口的 `probe-install-preflight.ps1`；
- QA host、测试设置与普通 Mod DLL；
- Git/worktree/runtime-lock、存档测试、游戏 smoke、官方本地产品/Author SDK 发布功能的新增依赖。

### 0.6.1 分层责任

- BAT：只承载双击体验，不判断文件状态。
- CMD：只映射动作、选择一个已探测主机、显示最终退出码。
- PowerShell player layer：路径发现、校验、事务、状态、收集和卸载。
- release/developer layer：构建、catalog、官方产品发布、完整 Doctor 验证；只在仓库执行。
- Runtime：只加载五个生产 DLL；不能依赖外部诊断 EXE 才算健康。

## 八、0.6.1 验收门

1. 正常 Runtime ZIP 内 `.exe=0`、Player Doctor entries=`0`；五 Runtime DLL 精确且版本为 `0.6.1.0`。
2. Windows PowerShell 5.1 解析全部打包脚本；安装链不得直接依赖 `Get-FileHash`/`Expand-Archive`。
3. 包路径与游戏路径分别覆盖空格、中文、括号、`&` 和分号；四个 BAT 都必须到达正确 PowerShell 动作。
4. 临时有效游戏完成安装→状态→有界收集→卸载；无效目标友好失败；卸载后状态不得误报成功。
5. BepInEx 覆盖：已有完整跳过、包内离线安装、损坏缓存由包内源恢复、无本地源时网络 fallback 失败信息明确、第三方 plugin/config 不被整树备份或删除。
6. Runtime 事务 fault matrix 覆盖 plugin/components/tools/receipts 各阶段中断与恢复；Doctor 不再出现在 transaction receipt。
7. 默认日志包对普通文本与 crash dump 有硬上限；现有 30.35 MiB 样本应显著缩小，并留下截断/未复制 dump 的说明。
8. 状态检查在无 Doctor 情况下不得警告 Runtime 损坏；如单独提供 Doctor，只能显示为可选补充结果。
9. 正常包通过订阅包审计矩阵，并输出包路径、PowerShell 5.1 版本、每项退出码和证据目录。

## 九、实现顺序

1. 先把 Player Doctor 从普通包与强制 install/status receipt 中移出，并修正 catalog/文档 authority。
2. 修复 host probe 参数协议与主机优先级，加入分号路径回归。
3. 收紧日志默认体积与 dump 策略。
4. 修正 BepInEx 有效来源、重试与最小覆盖备份。
5. 清除无人入口预检与卸载伪参数；建立 player-only library 的不再增长边界。
6. 最后才考虑进一步抽取 `common.ps1`/`release-common.ps1`/`install-to-game.ps1`。抽取必须由行为矩阵保护，不能为了行数好看而复制出第二套漂移实现。

本 Review 只冻结问题、目标设计和验收边界。具体改动、测试结果、0.6.1 构包与提交状态由后续 Update 记录。
