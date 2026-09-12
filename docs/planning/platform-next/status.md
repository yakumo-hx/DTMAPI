# DTMAPI 平台执行状态

- Lifecycle: active
- Role: 本路线唯一任务队列及阶段证据路由，不替代 implementation Update、API matrix、smoke 或发布 Catalog。
- Specification: [tasks](tasks.md)、[当前 SDK 完整迁移](execution-sdk-msbuild.md)、[roadmap](roadmap.md)、[acceptance](acceptance.md)；旧 SDK/M3 规格仅按需查阅。
- Planning revision: 最新 [0011：首次 SDK/CSV 更正及接续](../../updates/2026/20260910-0011-sdk-first-release-plan-correction.md)，承接方法验证与 SDK 完整计划。旧记录只作当时证据；CSV 恢复和旧 SDK 已发布的推论已撤销。

## 当前接手位置

**PN-041/042已完成，SDK D7/Runtime r6/多平台r2通过既定Windows Developer Preview技术验收。** [D7独立复核](../../debug/evidence/GAME-SMOKE/20260910-sdk-msbuild-070/repair-d7/independent-acceptance.md)重新验证准确包及原字段/工程根反例，并核对实际IDE/CLI/CI、完整Release和新字节Mono；[0012](../../updates/2026/20260910-0012-sdk-msbuild-first-release.md)已转verified，本批详细实现项全部收口。D6和所有失败证据保留，PN-042原范围有效。两个 Runtime 0.7.0 BOM 修复包已发布；SDK 0.7.0 于 2026-09-12 首次公开，含 Doctor BOM 修复和中文指南，见[交付记录](../../updates/2026/20260912-0001-sdk-publication-chinese-guides.md)。不因发布启动M4或新增通用迁移器；第一方临时CSV保持精确删除。

2026-09-10 PN-038、PN-039、PN-040.a 与 PN-031.a 的 Windows Developer Preview 技术验收完成；本轮独立 SDK 功能复核和指南修正见 [0007](../../updates/2026/20260910-0007-sdk-acceptance-distribution.md)。2026-09-11 两个 Runtime 0.7.0 分发已由用户公开发布并核实订阅，见[发布记录 0001](../../updates/2026/20260911-0001-runtime-070-upload-staging.md)；SDK 已独立公开，准确首发包与下载验收归[20260912-0001](../../updates/2026/20260912-0001-sdk-publication-chinese-guides.md)。准确产物由[候选选择](release-candidate.md)及各 Update 拥有，旧候选与失败证据保留。

[M4 实验](m4-experiments.md)已按当前 build 准备，本轮增加 [方法对照](method-validation.md)：先同档/侧车、官方内容/薄适配比较，再实现选中方案；没有新实测 PASS。PN-024 / PN-013 仍 ready，PN-041/042 完成不自动启动这些任务。Host 只依只读包输入，原生持久家族不强依 SaveData；设备、新平台、低 TFM 不因此晋级。

本轮 [SDK 独立验收](../../reviews/code/2026/20260910-0003-sdk-acceptance-distribution.md)接受功能，补正交付说明；SDK 走独立 GitHub 附件。**PN-031.multi done**：[0008](../../updates/2026/20260910-0008-multiplatform-070-candidate.md)已生成独立 0.7.0 多平台候选，Windows/WSL 安装、升级与恢复通过；实际 Steam Deck/Proton/CrossOver 游戏启动仍未验收；两个 Runtime 分发已发布，当前 BOM 修复包归[发布记录 0011](../../updates/2026/20260911-0011-runtime-y-hotfix-publication.md)。与 M4 实验独立。

用户决定：首发前内部 0.6.X；完成 M3 后最早公开 0.7.0，必要时吸收后续修复；随后 0.7.X，0.8.0 起逐族清退。详细版本/发布门由 roadmap 拥有。缺设备不阻塞手柄实验实现，但不记实体手柄 PASS。

状态词：ready 可领取；waiting 等列出的输入或细化；in-progress 正在实施；done 满足该切片定义的验收；blocked 有具体外部阻碍。实验交付 done 不表示设备/全平台已验证；真正产品证据另列。直接前置列只表示实施依赖，最终 PN-020/R2 证据不反写为 M2 子卡的前置，防止循环。

## 任务队列

| 任务 | 阶段 | 状态 | 直接前置/进入条件 | Implementation Update / 说明 |
| --- | --- | --- | --- | --- |
| PN-001 版本投影 | M0 | done | 无 | [0002](../../updates/2026/20260907-0002-platform-version-projection-baseline.md) |
| PN-002 会话协议 | M0 | done | PN-001 | [0003](../../updates/2026/20260907-0003-platform-author-session-handshake.md) |
| PN-003 target 骨架 | M0 | done | PN-002 | [0004](../../updates/2026/20260907-0004-platform-sdk-target-catalog.md) |
| PN-014.config-correctness | M1 | done | PN-001 | [0010](../../updates/2026/20260908-0010-platform-config-correctness.md) |
| PN-015 构建一致性 | M1 | done | PN-003 | [0011](../../updates/2026/20260908-0011-platform-build-plan.md)：A2/A3 返修、旧模板/IDE 等价与失败保全 |
| PN-004 官方 Local 安装 | M1 | done | PN-015 | [0012](../../updates/2026/20260908-0012-platform-official-local.md) |
| PN-016 生命周期事实 | M1 | done | PN-004 | [0013](../../updates/2026/20260908-0013-platform-lifecycle-evidence.md)：原生空档分支失败限制保留 |
| PN-017 调试与诊断 | M1 | done | PN-016 | [0014](../../updates/2026/20260908-0014-platform-author-debugging.md)：A1、resident/disk 负例；断点仍未证 |
| PN-008 作者闭环验收 | M1 | done | PN-017、PN-014.config-correctness | [0015](../../updates/2026/20260908-0015-platform-author-journey.md)及 R1 接受有界 M1 |
| PN-005 可选服务/旧 ABI | M2 | done | PN-008 | [0002](../../updates/2026/20260909-0002-platform-optional-services.md)：原 retained DLL Mono 调用 |
| PN-009 context/调度/命令 | M2 | done | PN-005 | [0004](../../updates/2026/20260909-0004-platform-context-scheduler.md) |
| PN-018 own-file/global data | M2 | done | PN-005 | [0003](../../updates/2026/20260909-0003-platform-owner-global-data.md) |
| PN-019 配置/输入/翻译 | M2 | done | PN-009、PN-018 | [0005](../../updates/2026/20260909-0005-platform-config-input-i18n.md)：设备/typing-focus 限制不扩大 |
| PN-007.a M2 target 候选 | M2 | done | PN-009、PN-018、PN-019 | [0006](../../updates/2026/20260909-0006-platform-sdk-candidate.md) |
| PN-020 M2 产品验收 | M2 | done | PN-007.a | [0007](../../updates/2026/20260909-0007-platform-m2-author-validation.md)：三个最终 runner；R2 有界 GO |
| PN-007.b M2 内部 target 冻结 | M2 | done | PN-020、R2 | [0006](../../updates/2026/20260909-0006-platform-sdk-candidate.md)：原字节验收保留，未来公开版本坐标由 PN-036 归位 |
| PN-006 内部反射 | M3 | done | PN-020、PN-007.b | [0008](../../updates/2026/20260909-0008-platform-reflection-core.md) |
| PN-021 公开反射候选 | M3 | done | PN-006 | [0009](../../updates/2026/20260909-0009-platform-public-reflection.md)：原隔离 API0.8 候选未发布；新组合由 PN-036/023 重验 |
| PN-036 内部版本归位 | M3 前置 | done | PN-007.b、PN-021、用户版本决定 | [0011](../../updates/2026/20260909-0011-platform-internal-version-realignment.md)：新 0.6.2，历史原字节保留；组合 Mono、会话、调度/反射关闭与旧 ABI Passed |
| PN-011 契约/依赖 DLL | M3 | done | PN-036 | [0012](../../updates/2026/20260909-0012-platform-package-dependencies.md)、[R3.shared GO](../../reviews/code/2026/20260909-0005-platform-r3-shared.md)：P01–P04、共享 Mono 调用/冷冲突/关闭与旧 reader 回归通过 |
| PN-010 自助 Advanced | M3 | done | PN-011、R3.shared | [0013](../../updates/2026/20260909-0013-platform-open-advanced.md)：P05；最终 SDK 0.6.4 与 Mono 任意 ID/旧 reader/故障隔离接受；[R3.native](../../reviews/code/2026/20260909-0006-platform-r3-native.md) GO |
| PN-022 工程/库/CI | M3 | done | PN-011 | [0014](../../updates/2026/20260909-0014-platform-author-projects.md)：P06；多工程、资源、显式锁定 restore |
| PN-023 M3 生态验收 | M3 | done | PN-010、PN-022、PN-021、PN-036 | [0015](../../updates/2026/20260909-0015-platform-m3-composition.md)：完整 0.7.0 target、作者闭环、准确 r3 Mono/共享/命令/旧 ABI/关闭与恢复通过；Experimental、未发布 |
| PN-033.a 弃用与迁移准备 | 0.7 首发前 | done | PN-020 | [0016](../../updates/2026/20260909-0016-platform-deprecation-preparation.md)：旧 ABI、去重诊断、离线扫描及同 ID 迁移 Mono 通过；不删 ABI/数据，无新公告 |
| PN-038 委托/原生泛型 | 0.7 首发修正 | done | V2/委托源码、R-Generic 补正、完整 Release 与 Mono 通过 | [0003](../../updates/2026/20260910-0003-sdk-delegates-native-generics.md)；保留 r4 owner 失败与 JSON 驻留限制 |
| PN-039 作者工程选择 | 0.7 首发增量 | done | 普通库/常量/XML/传递包及外部 CLI/IDE/Mono 通过 | [0004](../../updates/2026/20260910-0004-sdk-project-options-libraries.md) |
| PN-040.a 所选资产判定 | 0.7 首发增量 | done | 资产正反例、准确包与 Mono 通过 | [0005](../../updates/2026/20260910-0005-sdk-selected-package-assets.md)；R05 整体未完成 |
| PN-031.a 首发候选/升级/支持 | 0.7 首发前 | done | 本批新候选完整 Release、外部作者、实机与恢复通过 | [0019](../../updates/2026/20260909-0019-platform-release-preparation.md)；Windows Developer Preview 技术接受，未上传 |
| PN-040.b 运行资产兼容 | 后续 0.7.X | waiting | PN-041；真实低 TFM/facade 与 Mono/BCL 输入，R-Assets | 不同 ref/lib 已吸入 PN-041；剩余运行能力扩展不是 M4 前置 |
| PN-041 标准构建完整迁移 | 0.7.0 首发前 | done | a–f、F1–F4及D7独立复核通过 | [0012](../../updates/2026/20260910-0012-sdk-msbuild-first-release.md)：准确D7/外部反例与正常作者、实际IDE/CI、完整Release和Mono支持技术接受；D6失败保留；SDK首次公开归[20260912-0001](../../updates/2026/20260912-0001-sdk-publication-chinese-guides.md) |
| PN-042 现有 Mod/接入兼容 | 0.7.0 首发前 | done | a–c、C01–C10 与 PN-041 合并接受 | [0012](../../updates/2026/20260910-0012-sdk-msbuild-first-release.md)：准确0.6.1/原DLL/既有来源，真实旧marker退化修正，r6/r2安装恢复及Steam组合；CSV精确20项删除保留 |
| PN-037.a 实验控制器绑定 | 0.7 系列 | done | PN-019 | [0017](../../updates/2026/20260909-0017-platform-controller-bindings.md)：代码/无设备/键盘入口配置/冷启动/恢复通过；实体设备 pending-player |
| PN-037.b 菜单方向导航 | 0.7 系列 | done | PN-037.a；验收 A1 | [0018](../../updates/2026/20260909-0018-platform-config-navigation.md)：原生方向入口、Enter/Tab/事务、可选入口冷启撤回、动态分辨率及恢复通过；实体设备归 PN-037.c |
| PN-037.c 设备反馈与晋级 | 0.7 系列 | waiting | PN-037.a、PN-037.b、实际设备反馈 | D09/E07.input；按设备/输入路径分别晋级，不是本批连续实施的硬件阻塞 |
| PN-024 保存方法/身份/窗口 | M4 | ready | 当前 build 输入、同档容器与替代方法/故障矩阵已准备 | [R4a](m4-experiments.md#r4a保存身份与提交窗口)；先方法比较，选中后端再完整故障证明；未执行 |
| PN-012 事务 SaveData | M4 | waiting | PN-020、PN-024、PN-018、R4a | 真保存/未保存/故障/冷恢复；规划目标 0.7.2 |
| PN-013 ModContent/GameContent | M4 | ready | a 只读包/资源；b 官方/薄适配/受限编辑方法输入 ready | [R4b](m4-experiments.md#r4b一项展示表字段与一种静态图标)；a 可独立供 Host，b 未实测/未选方法 |
| PN-025 Host/Pack | M4 | waiting | PN-011、PN-013.a 只读包输入/owner | [V-Host](method-validation.md#v-host)；先普通 Host＋两 Pack；编辑/额外保存才加 013.b/012 |
| PN-026 M4 数据内容验收 | M4 | waiting | PN-012、PN-013.b 所选内容、PN-025、PN-023 | 真提交/内容效果与共存；不阻塞只读 Host 单独交付 |
| PN-027 领域身份/查询 | M5 | waiting | PN-020；选定家族两消费者 | V-Domain 先原生操作对照；仅实际数据/编辑加 012/013 |
| PN-028 通用 UI/输入/音频 | M5 | waiting | 所用 PN-013.a 资源及 PN-027 查询 | V-UI 先原生 UI/现有菜单适配比较；PN-037 不重做 |
| PN-029 首个持久实体 Host | M5 | waiting | PN-025、PN-027、PN-023 和所用内容；额外随档状态才依 012 | V-Entity 先官方创建/原生保存；完整家族/缺包/升级，R5 |
| PN-030 M5 领域验收 | M5 | waiting | PN-028、PN-029 | 只对实测家族成立 |
| PN-031.multi 多平台 0.7.0 候选 | 首发分发补齐 | done | PN-031.a 准确 Runtime r5 | [0008](../../updates/2026/20260910-0008-multiplatform-070-candidate.md)：新旧来源/reader、准确两端 host 与候选、Windows/WSL 生命周期和升级恢复通过；设备游戏启动不在本切片 |
| PN-031.b 持续发行/支持 | M6 | waiting | PN-031.a、各已承诺领域出口 | 后续升级/数据/内容恢复矩阵；复用现有基础设施 |
| PN-032 回归实验室/性能 | M6 | waiting | PN-020；选定负载/游戏矩阵 | 已有测试图/SDK prepare/公开 CI 不重建；首发风险子项在 PN-031.a |
| PN-033.b 公告/移除预览 | 0.7 系列 | waiting | PN-033.a、实际公开公告 | 具体版本与日期、逐族替代/预览/旧 DLL；R-Compat 输入 |
| PN-033.c 正式清退 | 0.8 起 | waiting | PN-033.b、R-Compat、已公告日期/版本 | 逐族清退；不自动删稳定 helper/保存 reader/所有旧 target |
| PN-035 作者材料/贡献 | 连续/M6 | waiting | PN-008；相应域实际产物 | 每域同批更新；本批材料随 PN-023/031.a，不独立重做 Wiki |
| PN-034 稳定候选验收 | M6 | waiting | PN-026、PN-031.b、PN-032、PN-033.a、PN-033.b、PN-035 | R6；承诺 M5 另需 PN-030，不依赖先执行 0.8 删除 |
| PN-014 有界内部裁剪 | 连续 | waiting | 已命名的具体 delta | 配置正确性已完成；不领取无边界全局清理 |

## 本次连续执行的终点

旧 PN-038/039/040.a/031.a 与 M4 输入准备已完成，保留事实。本次连续终点 PN-041.a–f＋PN-042.a–c 已达到：一个标准后端、完整 SDK、准确新作者/旧包/现有入口、IDE/Mono/发行双门接受并统一交回，不上传。实际验收归0012；未来方法实验不一律阻塞 0.7.0。

先 focused 排错，已知问题处理后对最终输入执行一次完整 Release；Stage/StartAt 仅诊断。R-AuthorBuild 支持后同任务继续，前卡可保持 implemented / 产品证据 pending，最终集中关闭，避免循环。真实外部条件不足时先做独立项，再准确列明缺口；没有未经批准的新长期承诺就不重复询问设计方向。

## 阶段产品证明

| 阶段 | 任务与产品证据 | 当前发布解释 |
| --- | --- | --- |
| M0 | 原 PN-001–003 source/package 验收 | 未据此新增公开版本 |
| M1 | [作者闭环](../../updates/2026/20260908-0015-platform-author-journey.md)及最终 R1；A1–A3 修复保留 | 日志/源行出口接受，断点未证；历史启用恢复声明已有纠正 |
| M2 | [PN-020](../../updates/2026/20260909-0007-platform-m2-author-validation.md)/R2 有界接受 | 原内部冻结未发布；PN-036 归位，不否定原候选事实 |
| M3 | [PN-023](../../updates/2026/20260909-0015-platform-m3-composition.md)证明既有组合；[PN-031.a](../../updates/2026/20260909-0019-platform-release-preparation.md)拥有原候选实测；[PN-041/042](../../updates/2026/20260910-0012-sdk-msbuild-first-release.md)拥有标准 SDK 与兼容最终验收 | 原子集接受保留；完整 0.7.0 Windows Developer Preview 技术门接受；Runtime 与 SDK 的公开结果分别归当前发布记录 |
| M4 | native 保存与内容生效两分支 pending | 没有新增通用保存/内容承诺 |
| M5 | 所选领域全旅程 pending | Frozen/blocked 壳不算平台能力 |
| M6 | 两代维护/支持/稳定证明 pending | 0.7 准备和 0.8 清退有路线，尚无新公开公告/删除事实 |
| 控制器 | 按钮绑定、原生标题方向入口、面板键盘事务及可选入口/动态分辨率证明接受 | 绑定/导航分别实验；实体控制器、Steam Input 和重连仍 pending-player |

## 接续和证据规则

按本卡 Update 和必要前置证据开工；完成后改行状态并解锁后卡。PN-007.a/b 共用原 Update；PN-031.a 继续原记录，新 PN-038/039/040.a 各自一份有界实施 Update。不为每个子步骤再开审查，R-Generic 只处理新的格式/根因；支持结论后同任务继续。Update 拥有实际改动/验收，候选页只放选择/剩余门/链接，本页只维护阶段和下一动作。

游戏/存档/锁/测试/文档沿 PROJECT 与现行流程，相关输入未变才复用证据。实际运行由各 Update 的 smoke 证据拥有，不能把 focused .NET 或计划文档改动写成新的 smoke PASS。下一候选实际 DLL/hash 变化按影响补验；无需把所有历史命令重新跑一遍。
