# DTMAPI 长期平台路线与产品验收规划

## Metadata

- Update ID: `20260907-0005`
- Date: `2026-09-07`
- Lifecycle Status: `verified`
- Validation Level: `docs, source`
- Runtime Validation: `not-run`
- Related Issue State: `none`
- Source: 用户要求继续承担技术负责人职责，重新展开成熟类 SMAPI 平台的完整能力地图、近期连续实施规格、中期架构与验收、远期演进和 GPT-6 复盘节点；本轮规划、验证与文档，不做大规模生产实现。
- Review: [本轮独立规划复核](../../reviews/code/2026/20260907-0005-platform-maturity-baseline.md)。

## Scope

保留 PN-001–003 的实际源码验收，区分 task done、产品能力证明与发布事实。重排未开工任务，不受反射先行或旧阶段划分约束。用户补充的已发布 Runtime 0.6.1 与 SDK 冻结 API 0.5.5 基线仍成立；不修改其版本、载荷、发布 Catalog 或已有代码。

## Changed Files

本次共涉及以下 17 个工作区文档；它们的先前未提交改动继续保留，不把其他源码或 Wiki 改动归入本 Update。

- 根路由：`AGENTS.md`、`PROJECT.md`，只补长期路线及任务/产品/发布分离原则。
- 架构：`docs/architecture/README.md`、`platform-next.md`；新增 `platform-author-delivery.md`、`platform-runtime-contracts.md`、`platform-data-content.md`。
- 规划：`docs/planning/README.md`；`docs/planning/platform-next/README.md`、`roadmap.md`、`tasks.md`、`status.md`；新增同目录 `capability-map.md`、`acceptance.md`。
- 记录：[独立 Review](../../reviews/code/2026/20260907-0005-platform-maturity-baseline.md)、本 Update、`docs/updates/INDEX-2026-09.md`。

没有修改生产代码、当前公共 API matrix、Hook、smoke、发行 Catalog、SDK 冻结 payload 或原生参考字节。

## Decisions Delivered

1. 以 C01–C33 定义完整作者/平台能力面；M0–M6 展开从工具链到长期维护的主要阶段，35 个任务保留旧编号并重定未开工范围。
2. M1 改为 PN-015→004→016→017→008：构建一致性、官方 Local、真实生命周期、符号/调试/诊断、外部作者闭环；反射移 M3。
3. M2 公共服务以 owner/save/world 寿命、明确阶段和调度/命令为基础；保留旧 ABI、五程序集、已有 Entry/清理/事件机制。
4. M3 开放共享库与自助 Advanced；M4 分开事务 SaveData、ModContent/GameContent 和 Host/Pack；M5 按一个领域家族闭合原生效果与维护；M6 收敛兼容、发行、回归实验室和稳定子集。稳定核心不以全部 M5 完成为硬前置。
5. 新 SDK 先生成隔离 staging 的完整候选，真实外部消费后再冻结 target，旧冻结载荷不改。PN-007.a→020→R2→007.b 消除验收/冻结环。
6. 保存采用 sidecar 候选，但必须先证明 durable prepare、原生 commit 关联和失败窗口；没有证明则不开放 WriteSaveData。当前 SaveSaving 不启用 quarantine，是保留资产而非本轮发现的漏洞。
7. E01–E08 区分代码、实际能力与发布；R1–R6 仅复盘受影响契约，不要求 Sol 反复从头设计。

## Validation

| 验证 | 结果与边界 |
| --- | --- |
| 当前代码/原生/SMAPI 独立阅读 | 已完成，事实和推断分开保存在 Review；本地 SMAPI 现行与历史演进均使用实际路径，没有复制源码 |
| 文档治理 | `tools/scripts/check-doc-governance.ps1`：通过，7882 checks；最终收口复验见日志 |
| 本轮文档链接/结构/依赖 | 12 份主交付文档、142 个本地链接、35 个任务规格、36 个执行切片节点、33 项能力、8 组验收场景；零缺失链接、零显式依赖环，ready 前置已满足 |
| 独立交接审查 | 通过，无剩余执行阻塞。已修正世界切换状态、反射任务引用、M6 过度依赖、M2 SDK 候选循环和场景家族误用，并由独立审查者复读确认；结论沿 evidence report |
| 源码未改 | 前后 `rg --files src tests tools author-sdk` 的 728 个文件路径与 SHA-256 完全一致，delta=0 |
| diff 空白检查 | 本轮涉及目录 `git diff --check` 通过；Git 仅提示现有换行规范 LF/CRLF，不是内容失败 |
| build / .NET tests / Mono / game | 本轮未运行。生产源未改，沿风险做文档/源码检查；此前 PN-001–003 回归结果不冒称本轮重跑，也不证明新平台游戏能力 |

这次通过只表示规划交接资料经过核对；没有把 PN-004 及后续任务或任一未运行产品场景标 done/passed。

## Evidence

本地忽略的 `reports/platform-next/20260907-long-horizon/` 保存：

- `author-findings.md`、`runtime-findings.md`、`content-findings.md`：分工源码调查；接手必要事实已归入 tracked Review/架构。
- `final-plan-review.md`：独立最终交接审查及修正结论。
- `initial-status.txt`、`source-before.json`、`source-unchanged.json`：原状态及生产范围未改验证。
- `doc-governance.log`、`plan-check.json`：文档验证输出。
- `check-plan.ps1`：仅本次文档的非权威检查脚本，不是新平台 gate/receipt 或 tracked 工具。

## Rollback Notes

仅撤回本轮架构/规划与路由增量，保留 PN-001–003 实现、所有现有未提交工作和已发布事实；不需要恢复游戏、安装、配置或存档。

## Follow-up

从 [接手入口](../../planning/platform-next/README.md)和 [status](../../planning/platform-next/status.md)启动 PN-015；连续 M1 后 R1，M2 后 R2，再沿 M3–M6 已设计的能力分支推进。本记录不承载未来任务实现流水。本轮无需要用户先做出的阻塞性产品决定，正式对外支持窗口/扩平台/在线服务等留指定复盘节点。
