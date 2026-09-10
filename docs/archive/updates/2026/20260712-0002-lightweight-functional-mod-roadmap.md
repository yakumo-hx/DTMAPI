# 20260712-0002 DTMAPI 轻量化与功能性 Mod 拆分路线图

## Metadata

- Update ID: `20260712-0002`
- Date: `2026-07-12`
- Lifecycle Status: `verified`
- Validation Level: `docs, source`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Scope Tags: `planning/architecture/lightweight/gamebridge/qa/compatibility/first-party-mods`

## Summary

新增一份可执行路线图，明确 DTMAPI 下一阶段应先缩小普通玩家实际加载的 QA 与兼容面，再继续复制 Zoom/AutoFishing 的功能性 Mod 产品化模式。

路线图将轻量化拆成玩家运行时、inactive 生命周期和公共维护面三个维度，并明确源码行数只用于定位结构集中区，不作为 GC 或性能结论。

本文记录的是规划与基线，不表示 QA、Compatibility 或后续功能 Mod 已完成迁移。

## Decisions

1. 在进一步拆分前，先关闭加载 provider 版本、诊断字节上限和真实零警告三个正确性前置项。
2. 下一项主要结构工作是将 GameBridge Smoke/性能探针迁入可选 QA 程序集，普通玩家包不加载。
3. Compatibility 必须先做消费者审计；旧 fishing executor 只允许冻结、薄适配、可选装载或按版本退役，不再承载新逻辑。
4. 功能性 Mod 按 `OneActionComplete`、`AutoHarvest`、`ActionSpeed`、复杂 UI/存档功能的风险顺序推进。
5. 公共 API 先冻结扩张，再按真实消费者分类；第一方 primitives 默认保持 internal。
6. GameBridge 先实现按消费者激活，再根据实际装载收益决定是否继续物理拆 DLL。

## Baseline Recorded

- 当前运行时项目约 `80,348` 行 C# 物理源码。
- `GameBridge/Smoke` 约 `13,433` 行。
- `GameBridge/Compatibility` 约 `3,077` 行。
- 移出这两部分后的理论普通玩家运行时基线约 `63,838` 行，未扣除或加入未来装配代码。
- 本地 SMAPI 可比运行时区域约 `42,270` 行；该差异不作为性能排名。

## Changed Files

- `docs/planning/20260712-dtmapi-lightweight-functional-mod-roadmap.md`
- `docs/planning/README.md`
- `docs/updates/2026/20260712-0002-lightweight-functional-mod-roadmap.md`
- `docs/updates/INDEX-2026-07.md`

## Validation

- 文档治理：`tools/scripts/check-doc-governance.ps1` 通过，共 `3966` 项检查。
- 格式检查：`git diff --check` 通过；仅报告工作树既有文件的 LF/CRLF 转换提示。
- 运行时验证：不要求。本次只增加规划、索引和基线记录，未修改运行时代码、项目引用或发布包。

## Rollback

删除本 Update 与路线图，并从 `docs/planning/README.md`、`docs/updates/INDEX-2026-07.md` 移除对应入口即可。回滚不影响运行时、API、Mod 身份或玩家配置。

## Follow-up

1. 完成路线图 Checkpoint A 的正确性闭环。
2. 输出 `GameBridge/Smoke` 的 QA 依赖地图与分批迁移设计。
3. 在 QA 第一批迁移验证通过后，再启动 `OneActionComplete` 产品化模板。

## References

- [DTMAPI 轻量化与功能性 Mod 拆分路线图](../../planning/2026/20260712-dtmapi-lightweight-functional-mod-roadmap.md)
- [功能性 Mod 与 DTMAPI 边界审查](../../reviews/api/2026/20260705-0001-functional-mod-dtmapi-boundary-review.md)
- [Mod Owner Lifetime 设计契约](../../../design/mod-owner-lifetime-contract.md)
- [Owner 平台依赖协调更新](20260712-0001-owner-platform-dependency-reconciliation.md)
