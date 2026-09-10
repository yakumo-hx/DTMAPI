# 0.7.0 候选选择与发布边界

- Owner: [PN-031.a Update](../../updates/2026/20260909-0019-platform-release-preparation.md)拥有候选身份、实际改动和验收；本页只维护选择、剩余门及证据路由。
- Disposition: **0.7.0 暂不放行：SDK 完整迁移和现有 Mod／接入方式兼容均待验收**。原 Windows Developer Preview 子集的技术接受保留；PN-041 标准后端及 PN-042 兼容门必须按准确最终产物完成，不能用旧 r5 PASS 替代。仍未发布，不授权上传。
- Execution: [execution-sdk-msbuild](execution-sdk-msbuild.md)拥有 PN-041.a–f，[execution-compatibility](execution-compatibility.md)拥有 PN-042.a–c；队列查 [status](status.md)。两者合并最终 Release/游戏验收，不重复建设发布流程。[旧执行包](execution-sdk.md)已完成，[M4 实验输入](m4-experiments.md)已准备。

## 候选选择

| 对象 | 当前选择与用途 |
| --- | --- |
| 已发布 Runtime | 仍为 0.6.1；发布事实归 Catalog，未改订阅项或 live upload |
| SDK 公开事实 | 用户确认旧 SDK 从未发布；r2–r5 和更早包均为内部证据。0.7.0 是 SDK 首次公开交付目标，无旧 SDK 用户迁移/客户端承诺 |
| 当前 Runtime 基线 | `artifacts/pn031/runtime-070-candidate-r5/DTMAPI`；准确来源/安装/实机见[原证据](../../debug/evidence/GAME-SMOKE/20260910-sdk-unified-070/README.md)。CSV 保持删除，不预设 Runtime 修正；只有真实新退化/共享代码变化才生成新候选 |
| 多平台 Runtime 基线候选 | `artifacts/pn031/multiplatform-070-candidate-r1/DTMAPI-MultiPlatform`；准确导入 r5，Windows/WSL 安装升级恢复接受，尚未发布；[PN-031.multi / 0008](../../updates/2026/20260910-0008-multiplatform-070-candidate.md)拥有两端 host 与全部证据。Runtime 更新时两种玩家投影同步导入新载荷，不能继续拿此 r1 代表新 DLL |
| SDK 迁移前基线 | `artifacts/pn031/sdk-070-candidate-r5/DTMAPI-Author-SDK-0.7.0-win-x64.zip`；[独立验收与指南修正](../../updates/2026/20260910-0007-sdk-acceptance-distribution.md)拥有准确来源/字节对照；保留作为当前子集与回退控制组 |
| 最终 Runtime / SDK 首发选择 | 尚未选定；待 PN-041 完整标准构建/离线/IDE/Mono/内部转换与 PN-042 真实现有包/入口兼容通过；Runtime 未变可继续采用准确 r5，不覆盖/重标旧候选 |
| 冻结 API 0.7.0 基线 | 保留 PN-023/031.a 原 recipe/payload 作准确证据，无 CSV 恢复/兼容超集实验。内部 SDK 未发布不等于可以破坏现有 Mod；确需候选变更时保留旧字节和独立来源 |
| 旧 Runtime / SDK | r3 / r2 原字节、旧 recipe 与[旧验收](../../debug/evidence/GAME-SMOKE/20260909-platform-release-070/README.md)保留；中间 r4 Runtime 的 V2 owner 失败也保留，不作为选用候选 |

## 技术门结果与来源

| 门 | 当前依据 |
| --- | --- |
| PN-041 / R-AuthorBuild | **待实施/未验收**：[新决定](../../reviews/code/2026/20260910-0004-sdk-msbuild-architecture.md)、[完整执行与 B01–B12](execution-sdk-msbuild.md)。是首发新增门，下面各行保留其原输入范围 |
| PN-042 / R-Compatibility | **待验收**：[C01–C10](execution-compatibility.md)。准确公开 0.6.1、支持范围内原包/实现者、真实入口与安装恢复；CSV 精确删除允许，旧 SDK 未发布。只有新退化才实施 b，不用重编旧源码冒充原包 |
| R01/R02、PN-038 | [0003](../../updates/2026/20260910-0003-sdk-delegates-native-generics.md)：合法委托、三端 V2 泛型语料、实际 Unity/作者类型、旧格式/ABI、owner 清理；首次 r4 supervision 遗漏已修并重验 |
| PN-039 | [0004](../../updates/2026/20260910-0004-sdk-project-options-libraries.md)：普通库、常量/XML、标准构建、CLI/IDE 和外部 Mono 组合 |
| PN-040.a | [0005](../../updates/2026/20260910-0005-sdk-selected-package-assets.md)：所选资产正反例与 Mono；R05 整体仍含 PN-040.b 后续范围 |
| R04 与统一发行 | [0019](../../updates/2026/20260909-0019-platform-release-preparation.md)拥有原统一候选/完整 Release/Mono；[0007](../../updates/2026/20260910-0007-sdk-acceptance-distribution.md)补独立 SDK 功能验收及 r5 指南修正，执行与契约文件不变 |
| 未变化的 UI/帧/长测 | 按 execution-sdk 复用旧 r3 有界证据；旧长测是标题一小时后首次读档/正常退出，不是连续玩法一小时。新 r5 短组合不冒充一小时 |
| 兼容说明 | 原 API/现有包 reader 与历史证据保留；CSV 为第一方临时接口，用户确认删除有效，不恢复、不阻塞首发。其他真实退化须修；设备/平台未测不记 PASS |

此前子集技术门完成不等于新的 SDK 架构与兼容目标完成，更不等于公开发布。PN-041/042 新门必须验收；用户允许推迟首发，现有合法包无需玩家重打包/重签/改 manifest，受支持接入方式继续工作。实际发布仍沿既有授权与流程。本次规划没有上传、移动 live upload 或更改 Catalog 发布授权。

SDK 按 [独立附件流程](../../workflows/author-sdk-release.md)交付给作者，不进入玩家 Runtime 安装包。本轮 [独立验收](../../reviews/code/2026/20260910-0003-sdk-acceptance-distribution.md)确认 SDK 功能，当时发现 **0.7.0 多平台包尚未生成，builder 和 host 仍绑定旧 0.6.1**。这一反例现由 [PN-031.multi / 0008](../../updates/2026/20260910-0008-multiplatform-070-candidate.md)解决：独立多平台候选、Windows/WSL 安装/升级/恢复均已接受，旧发布事实保留。Steam Deck/Proton/CrossOver 的实际游戏启动仍独立待验。

## 能力限制与后续

目标限 **Windows 0.7.0 Developer Preview**。新服务按 [API matrix](../../api/public-api-matrix.md)的实际稳定性，绑定与方向导航仍实验；实体控制器、Steam Input/重连及新平台注入需要各自实测。广义 ISSUE-010 未因本批完成而关闭。

Newtonsoft.Json 13.0.3 的恢复/构建误拒已修；当前游戏与私带该库的 Strict 混装仍因驻留冲突被拒。SDK 提供已验证的 Advanced 宿主 JSON 引用，不承诺任意 JSON 库共存。当前 r5 尚不提供标准 targets/生成器/不同 ref-lib；这些已纳入 PN-041 首发必做，不能原样带入新 SDK 的延期清单。低 TFM/facade 等运行支持留 PN-040.b，未支持的 Native 签名形式仍按实际格式边界说明。

用户明确 `ITeleportDebugApi.ExportDestinationsCsv(IManifest)` / `TeleportCsvExportResult` 仅用于第一方临时功能，[历史清退](../../updates/2026/20260831-0001-y-console-runtime-animal-catalog-and-teleport-cleanup.md)有效。[0011](../../updates/2026/20260910-0011-sdk-first-release-plan-correction.md)撤销上轮恢复推论：保持精确删除集合，不恢复 ABI/DTO/stub/UI，不等待 0.8，不作为本候选缺陷。其他仍支持接口和现有 Mod 路径照常验收；该例外不扩大删除授权，未来清退仍按实际承诺和窗口。

作者操作和恢复步骤随 SDK 的 [README](../../../author-sdk/README.md)、[工程/恢复](../../../author-sdk/PROJECTS-AND-RESTORE.md)、[Advanced](../../../author-sdk/NATIVE-REFERENCES.md)、[迁移](../../../author-sdk/MIGRATION.md)交付。PN-024/013 的 [M4 实验说明](m4-experiments.md)已具备当前原生输入与实验步骤；R4a/R4b 仍待实际实验，SaveData、通用 GameContent 和 Host/Pack 未成为本候选承诺。
