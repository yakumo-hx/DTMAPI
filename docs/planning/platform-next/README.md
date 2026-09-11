# DTMAPI 平台实施与接手入口

- Lifecycle: `active`
- Role: 已接受的平台路线入口；未来设计不等于当前实现、产品验收或发布。
- Queue: [status](status.md)拥有下一任务、执行状态和证据路由；[tasks](tasks.md)只保存规格。
- Planning evidence: 从 status 所选任务定位仍适用的决策；历史规格和验收不作为新的派发指令。

## 接手范围

先复用已读的 PROJECT/current-state，按本次用户授权在 status 定位接续项，再读对应规格节、Update 和仍适用的决策。不通读任务册、所有架构或历史审查。ready 表示输入已就绪；本批终点已经达到时，不因队列还有 ready 项而自动扩展范围。

done 卡及其执行包只提供历史规格和结果；有新缺陷才按实际 delta 接续。旧规划的“本轮未实施”不是当前禁令，也不能把旧接手提示当成重新开工的依据。

| 问题 | 按需查阅 |
| --- | --- |
| 阶段目标、依赖与复盘节点 | [roadmap](roadmap.md) |
| 0.6.X 内部、0.7.X 公开版本和 0.8 清退 | [可发布版本](roadmap.md#可发布版本) |
| SDK 标准构建规格、失败语料和完成条件 | [execution-sdk-msbuild](execution-sdk-msbuild.md)；实施状态查 status |
| 0.7.0 现有 Mod 与接入兼容门 | [PN-042 执行包](execution-compatibility.md)；实施状态查 status |
| 当前候选、发布边界和验收入口 | [候选选择](release-candidate.md) |
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
5. 完成同一 Update 并同步月度状态；仅在任务/依赖或阶段产品证据变化时更新 status，契约与真实 smoke 回写原 owner。在本次授权终点内连续推进。

R 结论支持就继续，不逐卡等待“继续”，不制造前卡最终验收阻塞后卡源码的循环。具体接续和交回条件由 [roadmap](roadmap.md)及本次任务负责。

任务 done、阶段产品 proven、版本 published 分别由任务行、实际产品验收和发布权威证明。一个未齐的阶段验收不阻止与它无依赖的内部切片；未运行游戏不能声明 Mono/玩家能力。

## 验证与复盘

[脚本选择与 focused 示例](../../../tools/scripts/README.md#choose-validation)给出当前命令。单个源改动构建选中项目及依赖；完整 Release 和安装器矩阵只在对应集成/发布边界执行。共享 Runtime 锁、存档模式、专用槽和故障隔离沿 [PROJECT](../../../PROJECT.md)；不在本页复制另一套规则。

按 [roadmap 的复盘节点](roadmap.md#gpt-6-架构复盘节点)选本任务涉及的关口，模型沿用户指定及 roadmap 分工。材料只带对应 Update、候选、匹配证据、契约和待决点。已有结果的复用与模型切换按产品验证流程处理。

## 可复用接手提示

> 在当前工作区接续本次授权的工作。复用已读上下文，从 status 定位当前任务、Update、对应规格和未完成验收；已经 done 的任务不重领。先检查本次变化的关键输入和反例，再完成指定验收。保留其他改动，沿授权范围连续推进，到既定终点交回。
