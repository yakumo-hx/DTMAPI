# 20260909-0019: PN-031.a 首发材料与候选门

## Metadata

- Update ID: `20260909-0019`
- Date: `2026-09-09`
- Lifecycle Status: `verified`
- Validation Level: `source, unit, runtime, docs`
- Runtime Validation: `passed`
- Related Issue State: `none`
- Source: [连续执行卡 PN-031.a](../../planning/platform-next/execution-next.md#pn-031a首发候选和维护基础)及验收 A2；不改发布授权。

## Summary

本次 SDK 审计增量已完成并统一接受：选用 Runtime r5 / SDK r4（均未发布），准确来源、旧包差异、外部作者、Mono、恢复与最终完整 Release 见[新候选证据](../../debug/evidence/GAME-SMOKE/20260910-sdk-unified-070/README.md)。PN-038/039/040.a 的实现各归其 Update；[M4 两支实验输入](../../planning/platform-next/m4-experiments.md)已完成只读准备，保存/编辑实验仍未执行。以下旧 r3/r2 叙述保留历史时点。

完整 Runtime/API/SDK 0.7.0 的 Windows 候选验收完成：Runtime r3 来自 `9fb6f01820560150d530bdbfd533bb040b3dbb12`，API source recipe 来自 `a5d0e8c`。作者闭环、准确安装矩阵、真实升级/撤回/恢复、同源完整 Release r9、一小时标题后读档及三条公开命令均通过。原本机 0.6.1 安装和测试资产已恢复，存档未变，运行锁已释放；候选没有发布。

2026-09-10 的[独立发布审查](../../reviews/code/2026/20260910-0001-release-070-compatibility-acceptance.md)复验准确字节、旧 ABI、SDK 包和来源门通过；另发现准确 SDK 内的当前版本/default target/必需阅读路径，以及 CSV 历史删除例外的发布表述需要修正。下列 Runtime Validation / PASS 均属于原候选，整体交付回到 implemented。

随后 [SDK 作者路径审计采纳](../../reviews/code/2026/20260910-0002-sdk-author-path-plan.md)新增 R01/R02 的实现缺陷/格式能力缺口，以及 R03/R05 作者工程限制；用户当前流程修正还已改变 TemplateCreator 的执行字节。后续必须按 [execution-sdk](../../planning/platform-next/execution-sdk.md)完成 PN-038/039/040.a，再沿本 Update 生成准确新 Runtime/SDK、修 R04 并验收。原“只改文档、全部非文档字节相同”的收尾前提已失效，以下历史结果不改写为新候选通过。

## Changed Files

A2 修改 `runtime-build-source.ps1`、Workshop builder、build/test 入口及独立 Git fixture 测试：用 MSBuild 评估 Compile/EmbeddedResource/ProjectReference 和预处理后的真实导入，合并当前与 HEAD 源归档的输入集合以覆盖 glob 删除；只检查该闭包的 Git 状态。正常发行强制真实 Rebuild，构建前后校验提交、集合与 hash；无可信输出来源的 Runtime SkipBuild 明确拒绝，不新建收据。无关 Wiki 改动允许保留；玩家 installer 不携带此 Git/MSBuild 检查。

完整候选还修正 installer 的写死版本成功提示和两处过时 managed Mod 安装提示；实际安装版本由 manifest 提供，SDK 官方 Local 路径保持既有所有权。SDK/Phase 0/G2 的旧测试默认值和负例 fixture 跟随当前已实现边界；QA inventory 只补登记拆出的 `Program.AuthorSettingsTests.cs` 单元文件，不扩大生产允许范围。API matrix 的两个当前版本投影同步 0.7.0。

[首发候选说明](../../planning/platform-next/release-candidate.md)汇集能力边界、准确产物、作者命令、支持与恢复。历史 SDK、retained DLL、M2 同号 0.7.0 原字节和 r2 产物均保留。

收尾时 smoke-matrix 超过既有 16 KiB 路由上限；2026-09-08 的历史行原文移入同目录历史卷，活动索引保留入口，不改旧结果或证据链接。

## Validation

最终完整 Release：命令 tools/scripts/test.ps1 -Configuration Release，FromStart/All，exit 0，24.56 分钟；源码与文档输入 hash 无漂移。准确 r5 安装器和五组符号、真实发布 0.6.1 升级/撤回/原本机恢复通过；SDK r4 与本轮完整测试重建 ZIP 同 SHA，执行载荷与已实测 r3 相同。实际 V2 Unity/作者泛型、普通/所选委托库、宿主 JSON、旧格式/ABI、命令与 owner 清理通过；私带 Newtonsoft 冲突保留，第一次 r4 owner 分支失败及修复分别归 0003/新 evidence。30 档、5 sidecar 和真实配置/启用选择未变，锁已释放。

R04：API 状态由权威矩阵投影，解压 ZIP 本地链接/锚点门通过，Advanced game-root 命令实际执行；当前版本、普通库/常量/XML、资产限制、JSON 驻留与 CSV 历史删除例外已随包说明。M4 当前 build 的 native 窗口/身份/表/图标消费者调查完成；不把只读准备写成 R4a/R4b 或公共写入产品已通过。

本批集中验收前的中间记录：新 SDK r3 已通过包检查及包内指南链接/锚点，仓库外默认/0.5.5 Strict、普通库/SDK shared library、资源/XML、CLI/IDE 与换目录同字节、Newtonsoft 13.0.3 在线/离线构建打包通过。Runtime r4 首次构建被 Catalog 门拒绝：已提交 API matrix 的 status date 为 2026-09-10，Catalog apiFreeze 仍为 2026-09-09；仅同步该派生日期后重试，发布授权和订阅事实不变。`artifacts/pn031-runtime-070-build-r4-attempt1.log` 保留失败；以下旧 r3/r9 结果不替代本批集中实测。

- A2 隔离 Git fixture：clean 实际构建、无关 Wiki 文档允许、Shared/版本/embedded/外链产品修改、glob 新增/删除拒绝、clean 新提交搭配旧 output 的 SkipBuild 拒绝、构建中输入集合变化拒绝全部通过。失败门保留原包。
- 完整 API 0.7.0 的已提交 source recipe 精确重建和缓存篡改 13 项通过。仓库外五个独立 SDK 工程完成 new/build/pack/Doctor/symbols；Provider、QuickStart 的 CLI/IDE 字节比较通过。SDK r2 ZIP 与完整 Release r9 重建 ZIP 同 SHA，复用原作者验收有准确字节依据。
- Runtime r3 构建、准确 Windows installer 矩阵和五组 portable PDB 配对通过；Abstractions 的 Release 配置明确不输出 PDB。真实已发布 0.6.1 正常升级至 r3，五个安装 DLL、components 和凭据与准确候选相符，原 Mod 数据未变。r2 与 r3 的 Runtime 源码和构建选项未变；r3 独立构建保留新的来源身份。
- **完整 Release r9 从 FromStart 串行通过，exit 0**：UTC `2026-09-09T15:49:25.6243516Z` 至 `16:38:56.1184906Z`，约 49 分 30 秒。执行期间无游戏进程，源码和文档引用冻结。覆盖单元、源码闭包、准确包、完整 SDK/便携包、两个 PowerShell 宿主各 28 项升级事务、官方 Local 事务、Catalog、Phase 0/G2 与来源负例、原生夹具、产品重复打包、QA 边界/篡改、生命周期、旧 ABI 和文档门。历史 SDK EXE 未提供，legacy wire 矩阵与 retained DLL 门实际执行，不能称为旧 SDK EXE 实测。
- PN-037 新选项在 r2 的默认关闭、开启保存/重启出现、实体方向/Enter/Tab 和动态分辨率已通过；r3 冷启动读取用户保存的 false，标题恢复原生五项，独立图标仍在。
- 2026-09-10 的 `GAME-SMOKE/20260910-004000` 通过：实际可见标题观察的保守上界为 00:41:19.532，首次原生读档为 01:42:05.978，连续停留至少 **3646.446 秒（60 分 46 秒）**。只读入一次 native index 0，01:42:08.442 原生返回 true，随后 Unity Application.quitting 正常退出。Provider/Consumer 共享值 71、Type/Assembly 身份、资源/native agent/Hook、独立 Control、原 retained ABI 73/1 和 owner 逆序关闭均通过；八个加载 owner 的 remaining/failures 均为零。无 Error/Fatal，runner 的读档、非强制退出、QA 与隔离清理、存档保护均 Passed。

长测启动会话的 Provider 命令成功；随后 Consumer 请求遇到 SDK601。日志明确会话先在 `ReturnedToTitle` 关闭，源码已有该生命周期；失败保留。既有初次标题边界只在未读档、没有认证请求时保留 startup session，本次提前请求使其正常关闭。独立短冷启动 `GAME-SMOKE/20260910-014255` 等待会话正常保留后，三条公开命令全部成功，apiTarget/hostVersion=0.7.0；随后再次读档并正常退出，session processed=3、pending/inFlight/replayEntries/handlerFailures=0。该短测不贡献一小时计时，也没有为补验改 Runtime。

真实候选撤回 → 已发布 0.6.1 恢复 → 原本机 0.6.1 owned 文件恢复通过。30 个原生存档、5 个 sidecar 和 16 个真实配置文件的 hash/长度/mtime/数量未变，真实启用选择未变；测试包、测试配置及 fixture 选择已恢复，既存 rejected staging 保留，QA 激活已清除、游戏退出且锁释放。没有原生存档备份写回。

## Evidence

准确身份、真实安装和实机结果由[最终候选证据](../../debug/evidence/GAME-SMOKE/20260909-platform-release-070/README.md)统一拥有；`final-acceptance-result.json` 汇总完成门。`candidate-identity.json`、`r3-maintenance-result.json`、`full-release-r9-result.json`、`full-release-sdk-parity.json` 保留来源与 hash；`long-soak-result.json`、`composition-result.json`、`cold-command-result.json`、`restore-result.json`、`final-protection-result.json` 保留各自时点的实际结论。

| 输入/检查 | 准确证据 |
| --- | --- |
| A2 输入闭包 | `artifacts/pn031-build-source-tests.log`、`artifacts/pn031-runtime-source-inputs.txt` |
| API 与作者 | `artifacts/pn031-frozen-070.log`、`artifacts/pn031/author-070/complete-result.json` |
| Runtime r3 | `artifacts/pn031-runtime-070-build-r3.log`、`artifacts/pn031-runtime-070-installer-r3.log`、`artifacts/pn031-runtime-070-symbols-r3.log` |
| 完整 Release | `artifacts/pn031-full-release-070-r9.log`；SHA-256 `53662291933b0eecf2f0dbd667bcb4436ae321068578f9f1febb6207b4a2728f` |

早期失败保留，不与 r9 拼接为 PASS：r5 的成功提示仍写死 0.6.1；r6 与实机进程互斥；r7 运行中新增文档引用使索引过期；r8 的 SDK 默认 target 断言过时。SDK 单组续验还发现 locked-restore 旧 fixture 未显式锁 target、程序集名负例的 JSON/csproj 不一致，以及一次临时目录访问拒绝。前两项只修 fixture；访问拒绝保留失败并原输入重试通过。Phase 0/G2 跟随条件 schema、Catalog 当前来源和 SDK160 真实 owner；QA 单元文件补登记后完整门通过。

对应日志：`artifacts/pn031-full-release-070-r5.log`、`artifacts/pn031-full-release-070-r6.log`、`artifacts/pn031-full-release-070-r7.log`、`artifacts/pn031-full-release-070-r8.log`、`artifacts/pn031-sdk-suite-070-r4.log`、`artifacts/pn031-phase0-contract-070-r3.log`、`artifacts/pn031-release-after-phase0-r1.log`、`artifacts/pn031-release-after-phase0-r2.log`、`artifacts/pn031-release-after-phase0-r3.log`、`artifacts/pn031-release-from-semantic-r1.log`。诊断尾段自身不是完整 Release。

[PN-023](../../debug/evidence/GAME-SMOKE/20260909-platform-pn023-runtime/README.md)、[PN-033](../../debug/evidence/GAME-SMOKE/20260909-platform-pn033-runtime/README.md)、[输入重试](../../debug/evidence/GAME-SMOKE/20260909-platform-input-retry/README.md)和[原生入口](../../debug/evidence/GAME-SMOKE/20260909-platform-native-entry-065/README.md)保有早期版本、实体输入正对照及故障矩阵的有界证据。未变化边界按产品工作流复用，实体手柄和新多平台注入不记 PASS。

## Rollback Notes

Catalog 仍为 ActiveNoUploadAuthorization，发布身份和 live upload 未动。真实 Runtime 按既有事务撤回候选、恢复已发布版本，再恢复原本机 0.6.1 开发安装的准确 owned 文件。原生存档只校验、不备份写回；配置和产品数据不随包卸载。

## Follow-Up

后续独立验收 [20260910-0007](20260910-0007-sdk-acceptance-distribution.md)接受 SDK 功能并纠正指南；当前 SDK 选用该记录的 r5，Runtime 保持本记录的 r5，原 SDK r4/完整 Release/Mono 事实保留。SDK 走独立附件；0.7.0 多平台分发仍需 PN-031.multi，不能从本记录的 Windows 通过推导完成。

本批 execution-sdk 所列工作已用尽：PN-038/039/040.a、准确新 PN-031.a 和 M4 输入准备完成。技术接受限 Windows Developer Preview；发布仍需另行明确指令，实际公开 Runtime 仍 0.6.1。PN-024/013 可领取有界实验，PN-040.b/041、设备/新平台保持各自后续条件。下段保留原连续执行边界供溯源。

保留原 r2 ZIP、Runtime r3 和全部有效证据；PN-038/039/040.a 各有自己的实施 Update，本记录继续拥有 R04/准确新候选与集中发行验收。按 execution-sdk 的影响范围补证，不能继承旧候选的全部 PASS；发布、实体设备和新平台仍有各自条件。此后同任务准备 M4 保存/内容的实验输入，不在本批实现公共写入，也不自动上传、晋级 API 稳定性或关闭广义 ISSUE-010。
