# 安装链、支持工具与失败归属

本文保存安装器历史的原因与修复边界，不拥有当前安装命令、跨平台合同或已发字节。当前任务入口查 [current-state](../../onboarding/current-state.md)，安装职责查 [Runtime installer boundary](../../architecture/runtime-workshop-installer-boundary.md)，身份与保存语义查 [PROJECT](../../../PROJECT.md)。原文与快照 hash 在 [阅读清单](../../archive/migrations/20260908-workspace.json)。

## 校验真正执行的 host 与包字节

[June 12 Windows PowerShell 修复](../../archive/updates/2026/20260612-0014-installer-windows-powershell-compat.md) 从真实 subscribed 0.5.0-alpha 重现中文 parser error；只在临时复制品加 UTF-8 BOM 就能解析，确定是脚本编码与 Windows PowerShell 5.1 的边界。生成的 `.ps1` 用 BOM，JSON 仍有自己的编码合同；不能全仓库机械统一一种编码，也不能用本机 PowerShell 7 通过来代替玩家 host。该改动没启动游戏。

本地 upload 新字节通过不等于 Steam 已发。该记录明确 source/staged 0.5.1-alpha 已修，而 subscribed 旧包仍无 BOM。更新准备、上传、订阅下载与安装后的字节需要分别识别，避免把目录错位当作产品修复无效。

## 离线依赖与部分安装

[June 17 offline package](../../archive/updates/2026/20260617-0005-runtime-offline-bepinex-package.md) 取消“先问 GitHub latest 才能用本地 cache”，固定 BepInEx 5.4.23.5 x64 包 SHA `82F9878551030F54657792C0740D9D51A09500EEAE1FBA21106B0C441E6732C4`，先 package-local，再 source fallback，最后固定网络 URL。这个 hash 是该历史 Windows 产物身份，不能套给其他平台或未来 bootstrap。

[corrupt zip correction](../../archive/updates/2026/20260617-0006-installer-corrupt-zip-and-api-version-message.md) 说明 zip central-directory 错误是包损坏/不完整，不是 DTMAPI DLL 错误。此前失败可以留下 plugin DLL，却没有 Doorstop、BepInEx core 或 state；只见单个 DLL 不能报已安装。历史修复对损坏 cache 替换并有界重试一次，保留原始及重试失败原因；无需为此验证游戏功能。

同记录把玩家文案改为“DTMAPI 前置版本过旧”，保留机器 status `api-too-new`。机器原因与玩家行动应分别维护，文案变动不应顺带创造兼容状态码。

## 可选支持工具不得阻断主安装

[June 18 validation split](../../archive/updates/2026/20260618-0001-installer-helper-validation-split.md) 的真实失败是日志分析 helper parser error 阻断 `1_install_dtmapi.bat`。它将当时的 common/release-common/install-to-game/install-bepinex 列为 strict install chain，status/collect/analyzer 等支持工具仅 warning；损坏的 strict install 文件仍非零退出。警告必须可见，不能同时打印 helper 全部 OK。

这四个旧脚本名不是当前永久白名单；可复用的是按实际执行责任划分必需和可选。临时目录、损坏/缺失 helper、空 settings、部分 BepInEx footprint 的聚焦测试已能验证安装层变化，无需启动游戏、复制玩家存档或做产品全套回归。

默认卸载保留 BepInEx 在该历史案例有明确 receipt：安装前已有、DTMAPI 没安装它；它不是卸载失败。当前更严格的 Runtime-only 删除权由 installer contract 与后续 ownership Update 拥有，旧记录中的可选扩大删除提议不自动生效。

[June 18 collector/bat correction](../../archive/updates/2026/20260618-0002-runtime-collector-bat-subscription-matrix.md) 固定了显式坏 game path 静默回落真实 Steam 的危险：用户/测试明确指定的路径无效应原样失败。CMD 嵌套时间戳 quoting 则搬到 PowerShell 做，并互斥 DesktopTimestampOutput 与显式 OutputDirectory。路径/host 问题用包含空格和中文的 package-local bat 实际入口验证，单独脚本 parser 通过不够。

[July 3 hotfix](../../archive/updates/2026/20260703-0001-pwsh-diagnostics-hotfix-package.md) 改为探测可用 host 并打印版本、被检脚本和 exit code，且旧失败 receipt 早于新成功时只作历史 INFO。这一独立 zip 当时没有同步 official upload/subscription；host 优先顺序和具体 wrapper 名称以后继续变化，应以当前 installer contract 为准。其核心经验是按真实结果时序报告状态，不让旧失败永久遮住新成功。
## 玩家入口错误的归因与精确环境夹具

08-03 玩家四 BAT 黑窗的确证根因是 `Program Files (x86)` 路径在 CMD 括号块中被不安全展开：CMD 先解析整块，即便 if 条件为假仍会语法失败。外层 extensions 关闭、缺 Get-FileHash 是审查另发现的真实问题，玩家已证实不属于本次原因。薄 BAT → 唯一 CMD dispatcher → host/capability probe → 共享 .NET 原语的重构消除复制兼容逻辑；PS AST 解析通过不代表退化 PS5.1 的 hash/ZIP 运行能力通过。实际括号/中文/& 路径、外层 /e:off、强制 PS5.1 的包动作链是有针对性的测试，无需游戏。

同一 Review 后续新增两条玩家反馈，不能漏在初版结论之后：08-10 `DotSourceNotSupported` 在 probe 前期发生，符合应用控制跨信任语言模式限制；具体 WindowsWorks 策略是强候选、非凭友好名定因，安全中心白屏也未证同根。FullLanguage 包 PASS 不等于该设备可安装，PS7/Bypass 不是解决系统策略的保证。当前边界与诊断修正回 installer architecture/ISSUE-018，不复制旧系统排障操作为开发规则。

另一玩家自动发现因 VDF 单个非法库候选在 Test-Path 抛错而停止。正确边界只跳过自动发现坏候选并保留警告，显式配置/用户路径仍应严格失败；手动复制成功不是 install/check/uninstall/collect 全链验收。两例均在 Runtime 写入前停止，不能推断半安装或存档变化；后续 08-20 等 owner 接管修正。

来源：`docs/reviews/manual-qa/2026/20260803-0001-runtime-workshop-installer-entry-regressions.md` 与同日 Update `0001`。
## 0.6.1 减包成功，但原型删掉完整性证明的方案未获采用

08-07 的临时无 EXE 包保留 0.6.0 游戏 DLL，只改复制包中的安装/状态脚本，是隔离可选诊断依赖的实验，并未单变量证明防护软件就是唯一原因。随后 canonical 0.6.1 将约 65 MB Player Doctor 从普通 Runtime 事务和默认启动移出，保留为独立只读工具；普通玩家不应因可选诊断 EXE 而安装失败。玩家脚本仍携带开发共享代码的解析成本，是结构债；不能用复制另一套 resolver/validator 消除行数。

同期 V2 曾将约 8,679 行玩家链缩到 1,177 行，但测试发现零字节 BepInEx 被视为完整、0.5.5/零字节 Runtime 与缺 Compatibility 仍返回成功，且安装和检查使用两套游戏定位器。这些并非冗余校验，而是用户所需版本识别和重跑修复语义。原型的独占 probe 目录和集中的路径防护可吸收，存在即健康的判定及第五个完整卸载入口没有整体晋升。原型早期记录了完整卸载授权，后续比较又按四入口收束；当前普通卸载边界只回 [installer authority](../../architecture/runtime-workshop-installer-boundary.md)，不能从旧原型恢复破坏性入口。

## 返回零、顺序 PASS 与真实并发是不同证据

08-07 压力审查发现 `cmd.exe` 根本未执行 probe/action，也能以 exit 0 伪装 PowerShell 成功。nonce/action 证明修复了假阳性；但直接以三个 CMD RANDOM 值命名临时文件，仍被并发同种子碰撞破坏。24 路真实 dispatcher 暴露 10 路错误的“无 PowerShell”，后改成先原子占有独立目录再接受 proof。外部持锁测试不覆盖主机选择并发，顺序三十动作通过也不能替代这一组合。

按规范化游戏目录的进程期 mutex 在扫描恢复事务前取得；活进程事务不能当中断恢复。0.6.1 对 pending transaction 的卸载选择变更前拒绝，而非静默保留恢复树后宣称完成。no-op 卸载须诚实输出；日志与卸载收据需唯一名字。08-08 Update 0001 已实现并完成 source/package 与真实目录安装/检查；当时未启动游戏或证明受影响玩家环境，后续发布/外部验收仍各有自己的 owner。

## 日志策略的后续覆盖与真实开放问题

08-07 第一轮简化采用 4 MiB 当前、三份 2 MiB 历史的截断策略，但同日用户随后要求最新十份 DTMAPI 日志完整复制，08-08 canonical 实现已经采用。不能从较早设计重建截断默认。选中源需稳定且副本长度/哈希一致，全部完成后 staging rename；失败不发布看似完整的半包，dump 仍按需。现行具体规则由 collector 与 installer authority 拥有。

ISSUE-018 把括号路径的 CMD 解析、extensions disabled、缺 Get-FileHash、App Control 混合语言模式和异常 VDF 候选明确分开。FullLanguage 本机矩阵不能证明玩家 App Control 环境可用；修改诊断不等于绕过策略。ISSUE-023 的 source/package 修复不能自动关闭 ISSUE-022 的玩家 tools access-denied。ISSUE-025 另有“首份 receipt 未落盘即留下空壳，污染所有后续重试”的确认根因，不是网络或防火墙问题。各 Issue 保留活跃归属，历史审查 pending 不另建重复待办。

来源：08-07 code Reviews 0001/0002、Updates 0001/0002/0003；08-08 code Review 0001 与 Update 0001；ISSUE-018、ISSUE-023、ISSUE-025。以上完整正文均已审阅，具体玩家验收状态仍回对应 Issue 的后续记录。
## 首收据毒化重试已经修复，外部环境验收另有归属

08-20 Review/Update 将 receiptless 根分成可证明的空壳与真正未知恢复状态：首份 receipt 必须写入、读回并验证后，才能创建 candidate/state；只有无 reparse、无配对 state、无未知内容的确切直接子目录才可清理。先写内存 transaction 再无条件 rollback 的旧链会在首次发布失败后留下永久阻断。共享只读分类器由 install/uninstall/status 共用，三者仍有不同修改权限；临时重试与清理不能掩盖第一条错误。

该次已经通过两 host 各 28 项矩阵、用户安装/游戏运行中拒绝的手测、官方上传和 Steam 下载精确一致性；因此“该 installer-only release 尚未发布/待本机接受”已过期。ISSUE-025 仍只等待原受影响环境移除外部阻挡后的普通重试，不应重开已经通过的发布闭环。全局 Registry/Steam libraryfolders 扫描也在这一轮删除；早期补强该扫描的建议被 bounded resolver 取代。

## 多平台分发复用一个 Runtime 身份

08-29 的路线研究随后被实施 Update 扩展为 Steam Deck 优先且支持 native Windows 的 sibling，采用两个 trimmed/self-contained .NET 8 host。早期 NativeAOT 数字只是已删除临时 spike 的下限，不能写成实际发布技术或可重建 benchmark。两个 installer engine 暂时并存是明确选择：已接受的 Windows PowerShell 包保留，新的 C# engine 由 Linux/Windows hosts 共用；不要再增加按动作/系统各一套实现。

已发布 Windows item `3743016467` 与多平台 item `3792681186` 均投影 `DTMAPI.Runtime`；distributionId 不进入产品准入。多平台必须导入已发布 0.6.1 的共享字节，不能将当前源码重编后以同版本冒充。两个 host 不进入游戏目录；普通 Windows 零 EXE 规则不被 sibling 的允许列表削弱。版本、host artifact、source commit、已发布 content 与本地 successor 分轴，最新值只链接 Catalog/current-subscription。

文件健康、启动配置、新鲜 Runtime 观察三者分开。Linux/Deck 的 Proton `winhttp` 参数和 CrossOver 现有 Bottle 的 Native-then-Builtin 是不同操作；首次下载一致性不证明注入。08-30 已观察首次发布，后来的真实 item 路径文案仍是本地 successor，未再次上传；Steam Deck/Linux/CrossOver 玩家运行接受仍归 08-29 Update 0003。原设计的“尚未创建 item”“native Windows 不支持”已被后续明确覆盖。

当前事务、锁、host 验证、日志路径与双引擎 fail-closed 规则只由 [installer boundary](../../architecture/runtime-workshop-installer-boundary.md) 和现行 focused matrix 拥有。V2 candidate 文档仅作未晋升历史原型；不可误作本多平台 engine 的当前架构。

来源：08-20 Review/Update 0001，08-29 Review 0002/Update 0003，完整 installer boundary 与 V2 candidate 快照。

## 先用最小宿主输入区分自检与安装失败

7 月 3 日玩家全部包脚本 exit=-1/no output，但普通脚本能运行。无写入 probe 分 package/host/parser/path/payload，再比较 Command/File/EncodedCommand，玩家最后证明连最小 EncodedCommand 都失败，而 File ParseInput/ParseFile 成功。真实修复使用 UTF-8 BOM 临时 File 入口及包内 BAT helper，消除长 inline Command；本机两路成功不能代替玩家 v3 报告。独立 payload 检查继续，依赖失败标 SKIPPED，失败仍生成报告、不创建安装目录。

来源：[预检](../../archive/updates/2026/20260703-0002-install-preflight-probe.md)、[File validator](../../archive/updates/2026/20260703-0003-powershell-file-validator.md)、[玩家最小实证](../../archive/updates/2026/20260703-0004-powershell-host-self-diagnostics.md)、[BAT 后继](../../archive/updates/2026/20260703-0005-root-bat-file-host-probe.md)。probe 后来移至独立支持包，不成为永久第五入口。进程已启动但无 Runtime 的另一路见[启动诊断](../runtime-lifecycle/startup-diagnostics.md)。

## 作者标记没有覆盖和删除权

7 月 13 日 P0 来自作者可创建的 dtmapi-package.json 同时被当身份、构建来源、覆盖和删除凭据。玩家卸载最终只管 Runtime；SDK/内部部署须包内 receipt 与包外 install state 匹配。临时矩阵中的第三方、复制 owner、坏 JSON、伪 receipt、未知增补保持原字节，mod_infos 不改。开发安装在官方扫描根外同卷 staging 组装，既有或竞态目的地拒绝覆盖；这种文件事务隔离不成为普通游戏测试前提。

7 月 14 日又发现包已移动到 MODS，随后 enablement JSON 失败，留下以后拒绝覆盖的孤立包。malformed/locked-write/retry 和 fingerprint 后插入未知文件的双宿主反例已补；只回滚本次创建且指纹仍匹配的包。受控异常恢复不等于断电/被杀恢复，后者由 SDK journal 独立证明。来源：[事务缺口](../../archive/reviews/code/2026/20260714-0001-major-update-progress-route-regression-review.md)、[有界后继](../../archive/updates/2026/20260714-0003-pre-batch2-install-transaction-and-runtime-regressions.md)。

[Runtime upgrade](../../archive/updates/2026/20260715-0010-runtime-upgrade-transaction.md) 后将逐 DLL 覆盖改为完整目录/installer-owned tools/state 事务；候选和备份不进入 BepInEx 扫描树，反向重试保留未知资产，不删除已完成产品。原件实机证据与“未触真实环境、未取锁”的旧否定句矛盾原样保留，不能据后者证明正确用锁。

Candidate11 确实会交换 11 个 MODS 产品和 Runtime，因此 child 参数、Runtime、SDK/entry/receipt 绑定在变更前检查；同卷 sibling staging 验成才交换，游戏仍活着或恢复不全不能释放共享锁。这些针对发布事务的门，不要求普通 Mod 修复重造候选/全目录备份。来源：[Candidate11 审查](../../archive/reviews/code/2026/20260804-0022-dtmapi-060-sixth-five-slice-parallel-review.md)。

## Player Doctor 是只读补充，不是产品可用证明

[Batch3 关闭](../../archive/reviews/code/2026/20260715-0010-batch3-player-doctor-closure-review.md) 补了离线 check/collect 和一次启动摘要；metadata-only 不 Assembly.Load。PackageArtifact 中内附插件与 InstalledGame 错放是不同语境；DtmMod 实装到 BepInEx 的确切错误也不授权工具搬动它。退出 2 表示完成诊断且有发现，不能让收集器把其它日志也判失败；Doctor 损坏或超时不阻断其余收集。作者新模板严语法不能否认 Runtime 可加载的历史 manifest。

8 月玩家 tools Directory.Move 被拒已越过 host/parser，四次回滚成功；同时关闭两类防护后原包成功支持 endpoint-security 类，但未单变量证明具体 EXE/ACL/句柄。0.6.1 去掉普通包 Doctor EXE 是职责修复，不能推广为关防护方案，也不能用本机四 BAT 矩阵关闭 [ISSUE-022](../../debug/issues/ISSUE-022-20260807-runtime-tools-move-access-denied.md) 的外部环境尾项。后续只围绕新现场补证，避免让已成功玩家重做安装。
