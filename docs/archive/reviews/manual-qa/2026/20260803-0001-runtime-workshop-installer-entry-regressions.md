# Runtime Workshop 安装器入口与主机兼容回归复核

- Status: `recorded`
- Time: `2026-08-03`
- Source: 玩家截图、玩家环境复核、用户对四个 BAT 与 PowerShell 5.1 兼容性的补充判断，以及对安装器历史/顶层设计重做的请求
- Scope: durable manual-QA/root-cause review followed by an authorized implementation
- User constraint: 保留 `BAT + PowerShell` 交付形态，不改为 EXE
- Related issue: [ISSUE-018](../../../../debug/issues/ISSUE-018-20260803-runtime-installer-entry-host-compat.md)
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

## 问题 3：0.6.1 订阅安装器被 PowerShell 应用控制语言模式拦截

原始反馈：

- 2026-08-10 用户提供安装器错误截图并询问“这是啥问题，安装器”。
- 图片原件：`D:\桌面\c55d4852bacd0af5d04ab23a5c5f5a0b.png`，104,219 bytes，
  SHA-256 `8555DE080C72A4842B8913BE0D4C000AA3CF56AE96426EE3FD21C49C2F12E35E`。
- 图片转写：订阅路径为
  `D:\steam\steamapps\workshop\content\2285550\3743016467`。调度器两次探测同一个
  `C:\WINDOWS\System32\WindowsPowerShell\v1.0\powershell.exe`；两次均在
  `probe-powershell-host.ps1` 报“无法使用点 `.` 获得此命令来源，因为该命令是在不同
  语言模式下定义的”，`FullyQualifiedErrorId=DotSourceNotSupported`、exit `1`。最后打印
  `No PowerShell host passed`，并误导性建议修复或安装 PowerShell。

审查记录：

- 截图/代码事实：失败发生在共享 CMD 的只读主机探测阶段，尚未选择动作主机，也没有进入
  `install-to-game.ps1`；因此这次调用没有发现半安装、游戏目录变更或 Runtime 事务窗口。
- Artifact fact：当前 Steam 订阅包是 `0.6.1`、
  `BuildCommit=db5e518a6d7f`。除 Steam 自有 `workshop.json` 外，它的 27 个文件与冻结
  `dist/workshop-packages-061-final/DTMAPI` 逐路径 length/SHA-256 全部相同；订阅、官方本地
  上传和冻结包中的 `probe-powershell-host.ps1` 均为 5,585 bytes、SHA-256
  `77E7696C6C4E821D8CF84125F047C2D12CED82EA103DB4D938B4455142645B7A`。这不是 Steam 下载
  损坏或旧安装器字节。
- Exact code point：探针在 `probe-powershell-host.ps1:122` 以点调用导入 `common.ps1`；
  install/check/collect/uninstall 的动作脚本也都点调用 `common.ps1` 或
  `release-common.ps1`。所以即使只让探针跳过该行，真实动作仍会在同一个跨语言模式边界
  失败；这不是改一行 probe 就能获得的 ConstrainedLanguage 兼容。
- Platform fact：Microsoft 的 PowerShell 应用控制文档说明，App Control for Business
  （WDAC）或 AppLocker 会按策略信任度让获准脚本运行于 `FullLanguage`、其他脚本运行于
  `ConstrainedLanguage`，并明确禁止不同信任/语言模式的父子脚本通过 dot-source 共享作用
  域。`ExecutionPolicy Bypass` 不是禁用系统应用控制的开关。参考
  [How App Control works with PowerShell](https://learn.microsoft.com/en-us/powershell/scripting/security/app-control/how-app-control-works?view=powershell-7.6)
  与
  [about_Language_Modes](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_language_modes?view=powershell-7.6)。
- Codex inference：截图精确符合应用控制/安全产品给入口脚本与其共享 helper 不同语言模式或
  信任等级后的跨边界拒绝。常见 owner 是 WDAC/App Control、AppLocker、单位/学校设备策略
  或施加同类锁定的安全沙箱；仅凭截图不能进一步断言是哪一个策略产品或哪条规则。
- Package counter-evidence：同一份实际 Steam 订阅包在本机 FullLanguage Windows
  PowerShell `5.1.26100.8875` 下重新通过完整假游戏订阅矩阵：九个 PS1 parser 全过，
  missing/empty target 正确失败，install/check/collect/uninstall 与卸载后非健康检查均符合
  预期，`Blockers: 0`。证据为
  `tmp/test-runs/player-installer-language-mode-20260810/DTMAPI Workshop Audit 20260810-194418/Results/stress-summary.md`。
- 归属：主要阻断属于玩家系统的应用控制/PowerShell 信任策略，位于现行安装器明确要求的
  `FullLanguage` 支持边界之外；当前包不承诺绕过企业应用控制。安装器同时拥有一个真实但
  次级的诊断缺口：它没有把 `DotSourceNotSupported` 分类成 App Control/语言模式拒绝，
  却建议“修复或安装 PowerShell”；候选枚举也未去重同一个 `powershell.exe`，导致相同错误
  打印两遍。
- 反证/未证实：这不是 PowerShell 5.1 版本太低、脚本语法错误、订阅包损坏、游戏目录
  错误或安装事务失败。安装 PowerShell 7 也不是可靠修复，因为系统应用控制策略可以作用于
  任意 PowerShell 版本。不得建议玩家关闭安全策略来换取安装成功。
- 玩家侧处理：先让玩家/设备管理员确认 PowerShell LanguageMode 与 WDAC/AppLocker/安全
  软件策略；受管理设备应由管理员决定是否允许这组 Workshop 脚本。若策略不能或不应改变，
  当前 BAT + 未签名 PowerShell 交付无法安装。个人设备则检查 Windows 应用控制和安全软件
  的脚本沙箱/白名单，不要反复重装 PowerShell 或覆盖游戏目录。
- 验收点：短期安装器改进只能做准确分类和指导——候选主机去重；出现
  `DotSourceNotSupported`、非 FullLanguage 或应用控制拒绝时，明确说明“PowerShell 存在但
  被系统策略限制，安装尚未开始”，不再建议笼统重装 PowerShell。若产品决定真正支持这种
  锁定环境，需要单独审查可信签名/管理员部署/其他交付载体，并重新决策当前零 EXE 边界；
  不能把规避安全策略当作兼容修复。
- blocker 判定：本次玩家环境对现行安装器仍是环境阻断；普通 FullLanguage 包矩阵继续
  通过不等于该设备已可安装。没有管理员许可或新的受信交付设计时，不应要求玩家重复运行。

2026-08-10 玩家系统策略复核：

- 图片转写：该个人电脑为 Windows `10.0.26200.8875`；Windows 安全中心窗口能够启动，
  但内容区完全白屏。管理员 `CiTool.exe -lp` 显示 `VerifiedAndReputableDesktop` 与
  `VerifiedAndReputableDesktopEvaluation` 均为“当前强制执行: false、授权: false”，
  因而当前没有证据表明 Smart App Control 正处于开启或评估强制状态。
- 同一输出显示 `WindowsWorks` 策略 ID/基础策略 ID 均为
  `{A244370E-44C9-4C06-B551-F6016E563076}`，版本 `10.3.0.4`，平台策略 `false`、策略已签名
  `false`、磁盘上有文件 `true`、当前强制执行 `true`、授权 `true`。其余截图中的活动策略
  为 Microsoft Cross Certificates audit、Driver 与 Endpoint Security 等平台签名策略；
  Lockdown、VBS 及 Smart App Control 相关策略均未强制执行。
- Platform fact：Microsoft 把上述 GUID 定义为 App Control 单策略格式的系统保留 ID；该策略
  可能以 `SiPolicy.p7b` 或同 GUID `.cip` 存在于系统卷/EFI。友好名并不能证明创建者，故不能
  仅凭 `WindowsWorks` 名称断言恶意软件、某款安全软件或 Windows 自带功能。
- Codex inference：系统确有用户模式 App Control 锁定，这与此前 PowerShell
  `ConstrainedLanguage` 和跨信任边界 `DotSourceNotSupported` 高度一致；当前最强候选是这条
  活动、未签名、非平台的 `WindowsWorks` 策略。若要把它定为唯一 owner，仍需 CodeIntegrity
  Operational 激活/阻止事件、策略来源，或经设备所有者授权移除并重启后的对照。
- 白屏归因仍分开：Windows 安全中心前端依赖 `Microsoft.SecHealthUI`、
  `SecurityHealthService` 与 `wscsvc`。白屏可能是应用包/系统组件损坏，也可能是同一策略阻断
  的连带表现；现有截图没有异常栈或阻止事件，不能宣称与 `WindowsWorks` 已证实同根。
- 玩家侧下一步：不需要再进入 Smart App Control 页面，也不应重装 PowerShell。先导出
  `CiTool -lp -json`、CodeIntegrity Operational 与 AppLocker MSI and Script 日志，并查询
  两个 Windows Security 服务；随后按 Windows 的应用“修复→重置”、DISM→SFC 顺序处理白屏。
  若玩家不认识 `WindowsWorks` 且确认不需要该安全策略，再按 Microsoft 官方 App Control
  删除流程处理；不得随意删除系统卷/EFI 文件或把关闭安全策略包装成安装器修复。

## 问题 4：Steam 非法库候选中断游戏目录自动发现

原始反馈：

- 2026-08-10 另一位玩家从
  `E:\steam\steamapps\workshop\content\2285550\3743016467` 运行 0.6.1 安装器；
  PowerShell 为 `FullLanguage`，JSON、portable SHA-256 与 ZIP 能力探测均为 `OK`，但安装和
  状态检查都在解析游戏目录时失败。
- 图片转写：`Test-Path` 报“路径中具有非法字符”，调用点为
  `Content\DTMAPIInstaller\tools\common.ps1` 的 `Test-Path -LiteralPath $path`；随后显示
  `Doloc Town game folder could not be resolved or validated`。错误参数中出现了来自剪贴板或
  Steam 配置的乱码路径片段，但正常盘符写法 `C:`、`E:` 本身合法。
- 用户确认玩家已改用手动复制安装，本轮不再要求其重复运行现行安装器；下一个版本做小修。

审查记录：

- 代码事实：自动发现会读取 Steam `steamapps\libraryfolders.vdf` 中每个 `"path"` 值，
  随后直接调用 `Test-Path -LiteralPath $path`。一个含 Windows 非法字符或控制字符的陈旧候选
  会抛出终止性错误，使枚举无法继续到后面的有效 Steam 库。
- 归因：这是 Runtime Workshop 安装器的自动发现健壮性缺口，不是 PowerShell 主机、冒号、
  Workshop 包损坏或游戏目录本身必然非法。失败发生在解析目标目录和任何 Runtime 写入之前，
  当前证据不支持半安装或游戏目录被修改。
- 语义边界：仅对 **Steam 自动发现得到的候选** 做逐项验证、记录警告并跳过；用户显式设置的
  `DTMAPI_GAME_DIR` 或本地配置仍须严格失败，不能悄悄改投另一个游戏目录。
- 临时处置：本次玩家已按用户指示手动复制安装。该结果只证明手动路径可用，不构成自动安装、
  检查、卸载或日志导出的验收证据。
- 下版本验收点：用一个非法 `libraryfolders.vdf` 候选排在有效库之前做精确回归；安装与状态检查
  应打印可理解的“已跳过无效 Steam 库候选”警告并继续找到有效 `DolocTown.exe`；显式非法目标
  仍应失败关闭；普通、空格、中文、括号、`&` 路径及当前订阅包矩阵不得退化。
- blocker 判定：该缺口不要求重写安装事务，也不阻止已手动安装的玩家继续使用；但在上述包级
  回归通过前，不能宣称自动发现能容忍 Steam 库配置中的单个脏候选。

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

本轮不重写已经验收的 Runtime 事务、回滚和所有权语义；重做的是玩家入口、host/capability 选择与共享兼容层。当前设计由 [Runtime Workshop Installer Boundary](../../../../architecture/runtime-workshop-installer-boundary.md) 持有。
