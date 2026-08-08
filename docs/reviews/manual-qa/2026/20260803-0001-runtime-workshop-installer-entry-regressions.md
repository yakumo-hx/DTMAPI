# Runtime Workshop 安装器入口与主机兼容回归复核

- Status: `recorded`
- Time: `2026-08-03`
- Source: 玩家截图、玩家环境复核、用户对四个 BAT 与 PowerShell 5.1 兼容性的补充判断，以及对安装器历史/顶层设计重做的请求
- Scope: durable manual-QA/root-cause review followed by an authorized implementation
- User constraint: 保留 `BAT + PowerShell` 交付形态，不改为 EXE
- Related issue: [ISSUE-018](../../../debug/issues/ISSUE-018-20260803-runtime-installer-entry-host-compat.md)
- Owning update: [20260803-0001](../../../updates/2026/20260803-0001-runtime-workshop-installer-entry-redesign.md)

## 问题 1：四个根 BAT 双击后黑窗消失，入口没有进入 PowerShell

原始反馈：

- 玩家表示双击 `1_install_dtmapi.bat` 后只出现黑色窗口，随后窗口消失。
- 玩家进一步确认 `1` 到 `4` 四个 BAT 都是同样的问题。
- 图片转写：资源管理器位于 `steamapps/workshop/content/2285550/3743016467`，可见 `1_install_dtmapi.bat`、`2_uninstall_dtmapi.bat`、`3_check_dtmapi_status.bat`、`4_collect_dtmapi_logs.bat`；聊天截图文字为“我双击安装就显示个黑框就没了”“1-4不管双击哪个都是一样的问题”。
- 后续玩家环境复核确认订阅根为 `E:\Program Files (x86)\steam\steamapps\workshop\content\2285550\3743016467`；复制到不含括号的位置后可运行。

审查记录：

- 用户确认事实：四个公开入口在该玩家环境下共同失效；完整订阅路径包含 `Program Files (x86)`，复制到无括号路径后恢复。
- 用户确认事实：玩家的 Windows PowerShell 为 `5.1.26100.8875`，`Get-FileHash` 存在且可用，所有执行策略均为 `Undefined`；强制 `/e:on` 后仍失败。因此命令扩展关闭、缺少 `Get-FileHash` 和执行策略拦截均不是该玩家的根因。
- 代码事实：四个 BAT 从 2026-06-20 起复制同一套约 50 行的主机发现/标签子程序；当前入口第一条状态命令只是普通 `setlocal`，随后使用 `%~dp0`、`call :dtmapi_try_powershell`、`if defined` 和标签跳转。这些语义依赖 CMD 命令扩展。
- 代码事实：包构建器曾因“UTF-8/LF-only 下标签寻找不可靠”而专门把 BAT 重写为 CRLF，但没有把 `EnableExtensions` 或 `/e:on` 纳入入口契约；历史独立启动捕获 BAT 也曾真实出现 LF/标签寻找失败。
- 玩家根因代码点：旧 BAT 的 `if not exist "%DTMAPI_PS_HOST_PROBE%" (...)` 复合命令内部还包含未引用的 `echo ... %DTMAPI_PS_HOST_PROBE%`。CMD 在判断 `if` 真伪前会先展开并解析整个括号块；展开后的路径中 `(x86)` 的右括号被当成命令块结束符，所以即使 probe 文件实际存在也会在 PowerShell 前语法失败。
- 精确同构复现：把原 Steam 0.5.5 订阅包复制到临时 `Program Files (x86)\steam\steamapps\workshop\content\2285550\3743016467` 后，以 `cmd.exe /d /e:on /v:off` 运行四个 BAT，四者均退出 `255`，共同报 `...probe-powershell-host.ps1 was unexpected at this time.`。证据根为 `tmp/test-runs/installer-parentheses-old-c064209572c7494f90d7ea69cb038ff4`。
- 独立兼容缺口：同一旧包复制到普通空格/中文路径后，在 `cmd.exe /d /e:off /v:off` 下四入口均退出 `1` 并出现语法/标签错误。该复现仍证明旧入口错误依赖 ambient command extensions，但用户的新证据明确排除了它作为这位玩家的根因。
- 对照：同一精确包在普通 CMD/PowerShell 环境中的完整临时游戏矩阵通过，Windows PowerShell 5.1 文件解析 `10/10`，安装/检查/收集/卸载语义正确，`Blockers: 0`。证据在 `tmp/test-runs/installer-redesign-baseline/DTMAPI Workshop Audit 20260803-223344/Results/stress-summary.md`。
- 纠正此前推断：`/e:off` 是审查中发现的另一个真实缺口，不是该玩家的故障归因。该玩家的已确认根因是旧 BAT 在括号代码块中不安全地展开了含括号的安装路径。
- 反证：不是 PowerShell 版本、`Get-FileHash`、执行策略或某一个动作脚本损坏，因为失败发生在 host probe 之前、四入口一致且只改变父路径即可恢复。
- 归属：Runtime Workshop 根入口与 CMD 启动契约。
- 验收点：四个公开 BAT 必须是无标签的薄入口；路径型输出必须安全引用；每个入口先显式启用扩展，再通过 `%ComSpec% /d /e:on /v:off` 进入唯一共享调度器；生成包必须在精确同构 `Program Files (x86)\steam\steamapps\workshop\content\2285550\3743016467`、外层 `cmd /e:off` 以及空格/中文路径下完成安装→检查→收集→卸载链。
- blocker 判定：若任一入口仍在复合 CMD 块中不安全展开路径、复制主机发现子程序、依赖外层扩展状态或在 PowerShell 前只闪退，本问题保持阻断。

## 问题 2：审查中独立发现的退化 Windows PowerShell 5.1 兼容回归

原始反馈：

- 当前 0.5.5 包在“只有 Windows PowerShell 5.1、且 `Get-FileHash` 不可用”的环境中会真实安装失败。
- 用户指出这看起来像历史上修过、后来重新引入的兼容问题。
- 图片转写：本问题没有新的错误截图；错误事实由精确包执行和失败状态复现。
- 后续玩家环境复核确认该玩家的 `Get-FileHash` 正常，因此本节是审查过程中发现的独立兼容回归，不是该玩家黑窗的根因。

审查记录：

- 用户确认事实：目标产品仍应支持不安装 PowerShell 7 的普通 Windows 玩家。
- 代码/文档事实：Update `20260618-0002` 已明确把 `install-bepinex.ps1` 的 `Get-FileHash` 换成 .NET SHA-256，并验证“Windows PowerShell hosts without the cmdlet”仍能安装；这是一条既有兼容目标，不是本轮新发明。
- 代码事实：从 2026-07-14 的官方本地事务、2026-07-15 的 Runtime 事务/Player Doctor、2026-07-21 的 Advanced 安装、2026-07-23 的 Compatibility Host 到 2026-07-28 的 Author SDK 生命周期，`install-to-game.ps1`、状态、收集和 probe 又逐步加入直接 `Get-FileHash`。当前玩家包共有十五处直接调用。
- 精确包复现：Windows PowerShell `5.1.26100.8875` 对十个脚本解析均通过，但固定以该主机运行实际 `install-to-game.ps1 -InstallBepInEx -SkipOfficialLocalMods` 时退出 `1`；错误为 `Get-FileHash is not recognized`，调用点是打包脚本 `install-to-game.ps1:1539` 的 `Get-DtmApiRuntimeAssemblyMetadata`。
- 环境观察：该 Windows PowerShell 子进程能发现 inbox `Microsoft.PowerShell.Utility 3.1.0.0`，但继承的 `PSModulePath` 先包含 PowerShell 7 模块目录；`Get-FileHash`、`Expand-Archive` 和 `Compress-Archive` 均不可用，而 JSON cmdlet 可用。它证明模块阴影/损坏形态可真实发生；它不证明所有干净 Windows PowerShell 5.1 都必现。
- Codex 推断：根因不是 PowerShell 5.1 语法，而是入口只做 AST 解析、安装实现又把环境 cmdlet 当成默认能力；正常包矩阵优先选择本机 PowerShell 7，因此也掩盖了纯 5.1 执行链。
- 反证/未证实：不能把问题描述成“标准 5.1 一定没有 `Get-FileHash`”；正常 inbox 5.1 通常提供该命令。需要支持的是 cmdlet 缺失、模块阴影或精简系统下的既有降级边界。
- 归属：共享 PowerShell 兼容能力层、host runtime probe 和玩家包执行矩阵。
- 验收点：玩家脚本不得直接调用 `Get-FileHash` 或 `Expand-Archive`；SHA-256、ZIP 解压和固定文件下载由 `common.ps1` 的 .NET 实现统一拥有；host probe 除解析外还必须检查 FullLanguage、JSON 和共享 SHA-256/ZIP 能力；固定 Windows PowerShell 5.1 的包级安装→检查→收集→卸载链必须通过。
- blocker 判定：只做语法解析、只在 PowerShell 7 下通过，或仍存在玩家脚本直调上述 cmdlet，均不能关闭本问题。

## 历史变动与为什么反复回归

| 时间 | 变动 | 当时解决的问题 | 遗留/新盲区 |
| --- | --- | --- | --- |
| 2026-06-11/12 | 建立 `1` 安装、`2` 卸载、`3` 状态 BAT 与 PowerShell 主体；随后用 UTF-8 BOM 兼容中文 WinPS 5.1 解析 | 初版发布卫生、中文脚本解析 | BAT 仍依赖外层 CMD 默认；验证集中在 PS 解析 |
| 2026-06-15/18 | 增加 `4` 收集；修复嵌套 CMD 时间戳引用、离线 BepInEx、坏 ZIP、严格安装链/宽松支持链；哈希改为 .NET fallback | 离线、损坏缓存、收集器路径、缺少 `Get-FileHash` | 四个 BAT 开始复制相同兼容逻辑 |
| 2026-06-20 | 四 BAT 加入 Windows PowerShell/`pwsh` 搜索和 `:dtmapi_try_powershell` | 找到可用主机并保留退出码 | 依赖标签、`if defined`、`%~dp0`，没有强制 extensions |
| 2026-07-03 | PowerShell 7 固定路径优先；长 inline/encoded probe 先被 file validator、再被 `probe-powershell-host.ps1 -File` 替代 | 真实玩家 encoded-command/inline parser 兼容失败 | probe 仍只证明“能运行解析器并解析脚本”，不证明运行期能力；逻辑仍复制五份 |
| 2026-07-14 至 07-28 | Runtime 原子事务、Player Doctor、Compatibility Host、Advanced/Author SDK 生命周期不断增强 | 所有权、哈希收据、回滚和恢复 | 功能提交逐处复制 `Get-FileHash`，绕过 6 月兼容 helper；矩阵没有 degraded-cmdlet gate |
| 2026-08-01 | 0.5.5 冻结包在普通 CMD、优先 PowerShell 7 的玩家矩阵中 0 blocker | 正常路径发布证据 | 没有精确 `Program Files (x86)` 订阅根、`/e:off` 外层或强制纯 WinPS 5.1/缺 cmdlet 链 |

共同设计缺陷不是“命令行天生不稳定”，而是兼容责任没有唯一 owner：四个 BAT 各自拥有 host discovery，并在复合块中展开路径；多个 PowerShell 脚本各自选择哈希/压缩能力；测试又分别证明语法、正常包和事务，却没有一个入口合同同时冻结含括号安装路径、外层 CMD、主机能力和动作链。

## 顶层设计结论

保留 BAT + PowerShell，但重做分层：

```text
四个无标签薄 BAT
  -> 强制 cmd /d /e:on /v:off
  -> 唯一 invoke-dtmapi-action.cmd
  -> probe-powershell-host.ps1（语法 + 运行能力）
  -> 现有 install/check/collect/uninstall 动作脚本
  -> common.ps1（SHA-256 / ZIP / 下载等兼容原语）
  -> 现有 Runtime 原子事务和 Runtime-only 卸载边界
```

本轮不重写已经验收的 Runtime 事务、回滚和所有权语义；重做的是玩家入口、host/capability 选择与共享兼容层。当前设计由 [Runtime Workshop Installer Boundary](../../../architecture/runtime-workshop-installer-boundary.md) 持有。
