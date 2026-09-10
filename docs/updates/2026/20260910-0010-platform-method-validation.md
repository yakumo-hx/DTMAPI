# 20260910-0010: 后续方法验证与 0.7.0 兼容门修订

## Metadata

- Update ID: `20260910-0010`
- Date: `2026-09-10`
- Lifecycle Status: `verified`
- Validation Level: `docs, source`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: 用户要求将 SDK 的架构取舍教训用于长期路线，先验证替代方法再实施；0.7.0 可暂缓但必须保障现有 Mod 及接入方式。决定见 [Review](../../reviews/code/2026/20260910-0005-platform-method-validation.md)。

## Summary

将保存、内容、Host、领域操作、UI、持久家族等尚未实现的强预设改为已有 R 节点内的方法比较；保留作者目标和产品验收。SDK 标准后端方向保留，把薄集成/实际 IDE 的早期证伪前移；新增 PN-042 现有 Mod 与来源兼容门，与 SDK 最终候选集中验收。

本轮只修改计划与架构文档，不实现兼容修正或 SDK，不启动游戏，不发布。r5 及多平台 r1 原证据仍保留；新增实验未执行，0.7.0 暂不放行。

## Changed Files

- 新方法验证规格、现有 Mod 兼容执行包与本次 Review。
- 数据/内容和 Runtime 架构、M4 实验：替代方法前置，候选机制条件化。
- roadmap/tasks/status、SDK 执行包、acceptance、能力图及接手路由同步依赖、顺序和首发门。

## Validation

- 已核对当前源码/规划、保存问题历史及本地 SMAPI 对应责任；三项独立只读复核完成。
- PASS：两次独立交叉复核；已修正永久身份残留预设、同档已提交/外层失败分类、PN-026 内容前置、ref/lib 首发归属及无条件热刷新要求。既有 R4a 锚点保留。
- PASS：17 份本轮文档的本地链接/锚点检查、文档治理与 diff 检查；月度状态由 sync 投影。没有为规划改动运行生产构建或重复旧产品测试。
- 新方法的实际作者/IDE/Mono/保存/内容实验：not-run；规划完成不表示平台能力成立。

## Evidence

- [路线复盘](../../reviews/code/2026/20260910-0005-platform-method-validation.md)记录源码定位、历史遗漏与采纳边界。
- [方法比较](../../planning/platform-next/method-validation.md)与 [兼容执行包](../../planning/platform-next/execution-compatibility.md)拥有新规格；无新 smoke、候选或上传记录。
- 结果范围：verified 只指本次规划/源码复核和文档治理；新增方法、SDK 后端、CSV 兼容修正及首发 B/C 矩阵仍待各实施卡证明。

## Rollback Notes

可撤回本轮文档修订；保留先前 SDK 设计、工作区治理和其他未提交改动。没有改玩家资产、存档、工具链或生产代码，无需恢复游戏目录。

## Follow-Up

PN-041.a 与 PN-042.a 开始，在同一实施任务连续完成 SDK a–f、兼容 a–c 和必要返修；最终统一验收后交回，不自动上传。随后按 R4 方法选择推进 M4；本轮未创建或恢复其他任务。
