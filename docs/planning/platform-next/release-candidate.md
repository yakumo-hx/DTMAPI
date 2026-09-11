# 0.7.0 候选选择与发布边界

当前玩家发布（2026-09-11）：Windows 和多平台均已更新为保持 0.7.0 的 BOM 修复包，准确订阅与已测试候选、上传目录一致，见 [Update 0011](../../updates/2026/20260911-0011-runtime-y-hotfix-publication.md)；修复与同版本更新方式见 [0009](../../updates/2026/20260911-0009-runtime-070-package-marker-bom.md)。原候选验收保留为当时输入的历史证据。

- Role: 仅维护候选选择、发布边界、剩余范围和入口；任务状态归 [status](status.md)。
- Disposition: **Windows 0.7.0 Developer Preview 技术验收通过，选择 D7/r6/r2；两个 Runtime 分发已由用户公开发布并核实订阅，SDK 独立交付。**
- Evidence owner: [PN-041/042 Update 0012](../../updates/2026/20260910-0012-sdk-msbuild-first-release.md)及[统一验收索引](../../debug/evidence/GAME-SMOKE/20260910-sdk-msbuild-070/README.md)拥有逐项结果、命令、hash、失败和复用范围；[D7 独立复核](../../debug/evidence/GAME-SMOKE/20260910-sdk-msbuild-070/repair-d7/independent-acceptance.md)拥有返修接受结论。本页不复制这些明细。

## 候选选择

| 对象 | 当前选择与用途 |
| --- | --- |
| 已发布 | Windows / 多平台均为 0.7.0 BOM 修复包；准确订阅与已测试候选、上传目录一致，归[发布记录 0011](../../updates/2026/20260911-0011-runtime-y-hotfix-publication.md)与 Catalog。原 r6/r2 发布保留在 0001 |
| 当前本地上传叶 | Windows / 多平台均为 [0009 BOM 修复包](../../updates/2026/20260911-0009-runtime-070-package-marker-bom.md)，保持 0.7.0，已发布并核实订阅。工坊绑定和描述不变；多平台同版本更新需先卸载 Runtime 再安装 |
| 原 Windows 验收包 | `artifacts/pn041/runtime-070-candidate-r6/DTMAPI-0.7.0-candidate-r6.zip` |
| 原多平台验收包 | `artifacts/pn041/multiplatform-070-candidate-r2/DTMAPI-MultiPlatform-0.7.0-candidate-r2.zip`；载荷为 r6，两个 host 沿用[PN-031.multi](../../updates/2026/20260910-0008-multiplatform-070-candidate.md)的准确构建 |
| 首发 SDK | `artifacts/pn041/sdk-msbuild-distribution-d7/DTMAPI-Author-SDK-0.7.0-win-x64.zip`；D6 留作历史反例基线 |
| 冻结 API / 兼容范围 | 0.7.0 Abstractions 原字节及 0.5.5、0.6.2–0.6.5 target 各自身份保留；准确范围归 0012 和 API 契约，不构造兼容超集 |
| 历史控制组 | Runtime r5、多平台 r1、SDK r5、D1–D6 等原字节与当时结果保留；入口归[迁移前 0019](../../updates/2026/20260909-0019-platform-release-preparation.md)及 0012，不重新标成当前候选 |

SDK 沿[独立附件流程](../../workflows/author-sdk-release.md)交付作者，不进入玩家 Runtime 包。旧 SDK 从未公开；首次交付及 CSV 精确删除范围由[计划更正 0011](../../updates/2026/20260910-0011-sdk-first-release-plan-correction.md)负责，不新增历史客户端支持或清退授权。

## 技术门与接受范围

[PN-041 B01–B12](execution-sdk-msbuild.md)和[PN-042 C01–C10](execution-compatibility.md)是验收定义；实际逐项结果只查 0012 及证据索引。已通过的完整 Release、外部作者、实际 IDE/CI/Mono 和安装恢复证据不在本页再维护一份。旧失败、标题长测与实际玩法的不同范围保持原记录解释。

## 能力限制与后续

- Windows Developer Preview 不承诺 shipping Mono 的断点、单步、受管栈/locals 或独立 debugger 环境；实际 IDE/日志证据的范围见 0012。
- 低 TFM/facade 留 PN-040.b；游戏驻留 JSON 与 Strict 私带版本冲突仍拒绝，不承诺任意库共存。标准 targets、生成器、普通库、不同 ref/lib 已完成，不再作为首发延期项。
- 实体控制器、Steam Input/重连和 Steam Deck/Proton/CrossOver 游戏启动仍需实测；能力稳定性归 [API matrix](../../api/public-api-matrix.md)。内容样本不代表全部动物/音效/外插件 Hook，广义 ISSUE-010 未关闭。
- [M4 实验](m4-experiments.md)另行进入；本批不交付 SaveData、通用 GameContent、Host/Pack，也不授权公开发布。

作者操作从 [SDK README](../../../author-sdk/README.md)进入，本页不复制操作步骤。
