# DTMAPI 平台实施与接手入口

- Lifecycle: `active`
- Role: 已接受的平台路线入口；未来设计不等于当前实现、产品验收或发布。
- Queue: [status](status.md)拥有下一任务、执行状态和证据路由；[tasks](tasks.md)只保存规格。
- Planning evidence: 最新 [0011：SDK 首发与 CSV 更正](../../updates/2026/20260910-0011-sdk-first-release-plan-correction.md)，承接方法/SDK 架构 Review；旧推论按更正范围失效，历史实测保留。

## 接手范围

用户已授权路线内可逆实施及必要测试。先读 PROJECT/current-state，再查 status 的 ready 行、该任务规格节和它引用的决策。后续任务复用未变化的上下文。不要通读整个任务册、所有架构或历史审查；旧规划交付中的“本轮未实施”不是继续实施的禁令。

M1–M3、输入切片和 SDK 委托/泛型/普通库增量已有有界证据，M4 两条实验输入与多平台候选也已准备。r5 新对照研究促使我们调整长期构建方向：**先按 [SDK 标准 MSBuild 完整执行包](execution-sdk-msbuild.md)完成 PN-041.a–f，贯通整条作者链后一次切换正式后端**。原 [execution-sdk](execution-sdk.md)和 [execution-next](execution-next.md)只保留已完成批次规格；不重领旧卡、不继续扩充私有工程语法。现行简化测试和文档流程保留。

本次确认旧 SDK 未发布、CSV 为第一方已授权清退临时接口。首发取消通用历史 SDK 迁移/旧客户端支持及 CSV 恢复，只转换当前实际内部输入；PN-042 仍验证现有 Mod/入口，有新退化才修。两卡同批共用 Update/最终验证。未来方法路线保留；具体下一动作查 status。

| 问题 | 按需查阅 |
| --- | --- |
| 阶段目标、依赖与复盘节点 | [roadmap](roadmap.md) |
| 0.6.X 内部、0.7.X 公开版本和 0.8 清退 | [可发布版本](roadmap.md#可发布版本) |
| 本批具体切片、失败语料和完成条件 | [execution-sdk-msbuild](execution-sdk-msbuild.md)；旧规格按需查阅 |
| 0.7.0 现有 Mod 与接入兼容门 | [PN-042 完整执行包](execution-compatibility.md)，与 SDK 最终候选合并验收 |
| 哪些方法先试、如何选择及退出 | [method-validation](method-validation.md)，复用现有 R 节点；实际 M4 步骤在 [m4-experiments](m4-experiments.md) |
| 依赖/程序集/Advanced 格式与构建输入 | [包契约 P01–P08](../../architecture/platform-package-contracts.md) |
| 能力缺口 | [capability-map](capability-map.md) |
| 跨领域边界 | [主架构](../../architecture/platform-next.md) |
| SDK/工程/调试/发行 | [标准构建 AB](../../architecture/platform-sdk-build.md)、[作者交付](../../architecture/platform-author-delivery.md) |
| 生命周期/线程/owner/命令 | [Runtime 契约](../../architecture/platform-runtime-contracts.md) |
| 数据/内容/Host | [数据内容](../../architecture/platform-data-content.md) |
| 当前任务的验收场景 | [acceptance](acceptance.md)对应 E 节；只选本卡涉及子项 |

这些文档不替代 PROJECT、API/Hook、Catalog 和实际验收记录。新公共契约、格式和兼容承诺按任务与指定 R 节点冻结；不因内部实验提前开放，也不重复全平台架构审查。

## 实施循环

1. 核对 Git 当前改动。保留其他工作，测试范围根据本任务 delta 确定。
2. 接续 status 指向的 Update；未开工才创建。读取当前任务及必要依赖的结果，不重跑已经满足且输入未变的前置。
3. 实现本卡行为和所需契约、作者说明、失败语义。新增公共 API 同时处理 owner、异常、线程、SDK 和旧消费者。
4. 按[产品验证流程](../../workflows/product-change-validation.md)和本卡选择验证；不存在的未来 focus/fixture 不能当已有命令。未完成的必要验收继续保留缺口。
5. 完成同一 Update 并同步月度状态；仅在任务/依赖或阶段产品证据变化时更新 status，契约与真实 smoke 回写原 owner。按已授权路线继续下一个 ready 项。

按用户要求同任务连续执行。本批 PN-041.a–f＋PN-042.a–c：先薄集成/IDE 骨架与旧包基线，接着标准后端和最小兼容修正，最终同一准确候选集中关闭 B/C 矩阵。R 结论支持就继续，不逐卡等待“继续”，不制造前卡最终验收阻塞后卡源码的循环。只有新长期承诺、必要外部条件在独立工作完成后仍阻碍，或细化任务及返修完成才统一交回。本批不自动扩成 M4 或上传；未来 M4 先方法比较，选中路径再实现，旧实验准备不冒充方法已证明。

任务 done、阶段产品 proven、版本 published 分别由任务行、实际产品验收和发布权威证明。一个未齐的阶段验收不阻止与它无依赖的内部切片；未运行游戏不能声明 Mono/玩家能力。

## 验证与复盘

[脚本选择与 focused 示例](../../../tools/scripts/README.md#choose-validation)给出当前命令。单个源改动构建选中项目及依赖；完整 Release 和安装器矩阵只在对应集成/发布边界执行。共享 Runtime 锁、存档模式、专用槽和故障隔离沿 [PROJECT](../../../PROJECT.md)；不在本页复制另一套规则。

按 roadmap 的 R1–R6、R3.shared/native、R-Generic、R4a/b、R-AuthorBuild / R-Assets 和 R-Compat 做有界复盘。默认 Sol 实施、GPT-6 复盘；用户指定 GPT-6 实施时可在同任务完成，无须为模型切换另设关卡。材料只带对应 Update、固定候选、实际外部工程/Mono 证据、契约和待决点。已有授权的内部工作继续推进；0.8 清退不需要等所有 M5 领域完成。

## 可复用接手提示

> 在当前 DTMAPI 工作区继续 0.7.0 SDK 首次公开交付与现有 Mod 兼容实施。先读 status、Update 0011、execution-sdk-msbuild 和 execution-compatibility；CSV 已获授权删除，不恢复；旧 SDK 从未发布，不做通用旧工程迁移器/旧客户支持。PN-041.a＋042.a 开始，在同一实施 Update 连续完成标准后端、当前内部工程转换、完整 SDK、IDE/CI/Mono；042.b 仅修真实新增退化。最终一次完整 Release/游戏组合关闭 B/C，旧 Mod 不要求重编/重签/改来源。保留其他改动，不逐卡等继续；本批细化任务和返修结束后统一交回，不自动 M4 或上传。
