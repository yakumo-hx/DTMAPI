# Runtime tools transaction access-denied player failures

- Status: `recorded`
- Time: `2026-08-07`
- Source: two player screenshots plus `D:\下载\DTMAPI-logs夜黑魔铃`
- Scope: durable manual-QA/root-cause review followed by an authorized standalone hotfix package
- User constraint: first ship a small BAT + PowerShell package that does not carry the Player Doctor executable; do not replace the normal Workshop package yet
- Related issue: [ISSUE-022](../../../../debug/issues/ISSUE-022-20260807-runtime-tools-move-access-denied.md)
- Owning update: [20260807-0001](../../../updates/2026/20260807-0001-runtime-no-player-doctor-hotfix-package.md)
- Current installer authority: [Runtime Workshop Installer Boundary](../../../../architecture/runtime-workshop-installer-boundary.md)

## 问题 1：两位玩家的 Runtime tools 目录提交持续被拒绝

原始反馈：

- 第一位玩家使用新游戏版本和新安装器仍无法安装；日志目录为 `D:\下载\DTMAPI-logs夜黑魔铃`。
- 第一位玩家截图显示 Windows PowerShell 5.1 主机探测通过，安装在 `Directory.Move(candidate\tools, live\tools)` 处以“访问被拒绝”退出。
- 第二位玩家的两次截图显示相同错误，并询问是否仍是安全软件问题。
- 用户随后要求先制作一个类似历史定制包的无 EXE 小包。
- 图片转写：第一位玩家包路径为 `E:\steam\steamapps\workshop\content\2285550\3743016467`，游戏目录为 `E:\steam\steamapps\common\Doloc Town`，失败脚本行为 `install-to-game.ps1:1399`；第二位玩家包和游戏目录位于 `F:\steam\...`，两次失败分别落在同一 `candidate\tools` 目录提交点 `install-to-game.ps1:1413`。两者均已进入 PowerShell 安装事务，不是旧 BAT 黑窗问题。

审查记录：

- 用户确认事实：两台不同机器均在 Runtime 工具目录提交阶段失败；第二台截图中的终端已是管理员窗口。
- 日志事实：第一位玩家保留了四份 `0.6.0` 失败状态，时间为 `18:58:26`、`19:03:16`、`19:06:20`、`19:09:18`。四次均在 `RuntimeTransactionPhase=PlacingTools` 调用 `Directory.Move` 时对各自 `candidate\tools` 返回访问被拒绝，且 `RuntimeRollbackSucceeded=true`、`RuntimeCommitSucceeded=false`。
- 代码事实：`candidate\tools` 除 PowerShell 支持脚本外还包含自包含、未签名的 `dtmapi-player-doctor.exe` 及两份 .NET notice；当前本地该 EXE 约 67 MB。Doctor 是只读诊断组件，不属于五个游戏加载 Runtime DLL，Core 对缺少 Doctor 的启动路径按非致命 `unavailable` 处理。
- 代码事实：当前安装器把 Doctor 三文件视为强制文件，并把整个 `candidate\tools` 通过一次无重试的同卷 `Directory.Move` 原子发布；因此不能让玩家简单删除 EXE，删除后会先在候选准备阶段失败。
- 已排除：这两次故障已通过 BAT/CMD 和 PowerShell host probe；第一位玩家日志还证明 JSON、便携 SHA-256 和 ZIP 能力正常。它们不是 ISSUE-018 的括号路径 CMD 解析、命令扩展或缺少 `Get-FileHash` 回归。
- Codex 推断：实时安全扫描器在新复制的自包含未签名 EXE 上持有句柄，是目前最强解释，也能说明只在部分机器发生和为什么目录级 `Move` 被拒绝；但现有证据没有安全软件事件日志或句柄快照，不能把具体安全软件归因为已证实。
- 仍可能：受控文件夹访问、第三方防护、索引/同步软件、目录 ACL 或其他短暂句柄占用也可能产生相同 `IOException`。
- 归属：Runtime Workshop 安装事务的可选诊断组件交付边界；不属于游戏 Runtime DLL、Mod 加载或 PowerShell 主机兼容层。
- 验收点：独立热修包不得包含任何项目自带 `.exe`；仍须保留五个 Runtime DLL、BepInEx 离线安装、事务回滚、状态检查、日志收集和 Runtime-only 卸载；必须在 Windows PowerShell 5.1、空格/中文/括号/`&` 路径完成安装→检查→收集→卸载。
- 诊断解释：若同一玩家用无 Doctor EXE 包安装成功，结果支持“Doctor/EXE 扫描锁”这一类原因，但不能单独识别具体防护产品；若仍在同一目录提交点失败，则应降低该假设权重并采集 ACL、Controlled Folder Access 和持有句柄证据。
- blocker 判定：包内仍有 EXE、状态检查把有意省略 Doctor 报成 Runtime 损坏、卸载无法清理其实际收据，或临时游戏矩阵失败时，不应交给玩家。

## 顶层处理边界

本轮只制作一个明确标识的独立救援包，不修改正常 Workshop 包的默认契约，也不撤销 Player Doctor 的既有诊断能力。热修包把 Doctor 省略视为有意降级：游戏 Runtime 可运行，离线检查和日志收集继续工作，但不再提供 PE 放置与最低版本的 Doctor 扫描结果。

## 2026-08-07 玩家环境补充

原始反馈：

- 玩家尚未运行无 EXE 小包。
- 玩家尝试“添加信任”后，正常订阅包仍安装失败。
- 玩家同时关闭防火墙和电脑管家后，正常 Steam 订阅包安装成功。
- 本次没有新增截图、拦截记录或安全软件事件日志。

审查记录：

- 用户确认事实：同一机器、同一正常订阅包在改变安全防护运行状态后能够完成安装。这是外部玩家环境中支持“终端安全软件干预”这一故障类别的直接证据，也明显降低了永久 ACL 错误、包损坏、PowerShell 主机不兼容和安装事务必现缺陷的可能性。
- 不能据此区分防火墙与电脑管家，因为玩家同时关闭了两者。正常安装全部使用包内离线文件，原失败又是本地 `candidate\tools` 目录移动被拒绝，因此纯网络防火墙本身与故障形态不太一致；电脑管家的实时文件/行为防护是当前更强推断，但尚未由单变量测试或产品日志证实。
- “添加信任”无效并不反证安全软件干预：信任范围可能只覆盖 Workshop 源目录，没有覆盖游戏目录中的 `DTMAPI\.runtime-install-transaction-*`、`DTMAPI\tools` 或 `BepInEx\plugins\DTMAPI`；产品的行为防护也可能不服从普通文件白名单。现有证据不支持要求玩家广泛信任 `powershell.exe`。
- 玩家没有运行无 EXE 小包，因此不能把本次成功记作该包的外部验证，也不能据此确认 Player Doctor EXE 是唯一触发物。无 EXE 包在防护开启状态下的受影响玩家测试仍是区分“EXE 扫描锁”与“更广泛拦截 PowerShell/目录事务”的有效验收点。
- 安全边界：玩家应恢复防火墙和电脑管家，不应把长期关闭安全防护当作安装方案。正常包已成功安装的玩家无需仅为诊断而卸载重装；可由后续受影响玩家完成单变量验证。

## 2026-08-07 无 EXE 小包玩家结果

原始反馈：

- 用户确认“小包安装成了”。
- 本次反馈没有说明安装时防火墙、电脑管家或其他实时防护是否重新开启，也没有提供新的安全软件事件日志。

审查记录：

- 用户确认事实：独立 `0.6.0` Runtime-only 无 EXE 小包至少在一台外部玩家机器完成了安装，证明该小包不是仅在本地临时游戏矩阵中可用。
- 证据边界：由于安装时的防护状态没有报告，不能把这个结果当成“在相同防护开启状态下只删除 Doctor EXE”的单变量实验，也不能据此断言 Player Doctor 是唯一触发物。
- 设计结论：Player Doctor 本来就不是五 DLL Runtime 的必需组件；即使具体拦截机制仍未单变量闭合，普通 Runtime 安装也不应继续因可选自包含 EXE 的扫描、锁定或信誉判断而失败。该反馈支持把无 EXE 交付作为 `0.6.1` 正常包设计，而不是只保留临时救援包。
- 后续实现与本地验收由 [20260807-0002](../../../updates/2026/20260807-0002-runtime-installer-061-simplification.md) 负责；本 Review 继续只拥有原始玩家事实和归因边界。
