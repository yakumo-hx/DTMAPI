# 20260908-0006：解包阶段复用、基线差异入口与函数地图迁移

## Metadata

- Update ID: `20260908-0006`
- Date: `2026-09-08`
- Lifecycle Status: `verified`
- Validation Level: `docs, source, unit`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: 用户授权工作空间结构建设、完整历史审阅后去冗余；本轮为解包与研究工具工程包，不改变 Runtime/native 产品边界，无需另建 Review。

## Summary

现有捕获的 InventoryOnly 会全树重哈希，ReuseExport/ReuseDecompile 只判断目录存在，旧快照续跑还依赖当前安装源。改为只读状态查询、记录明确输入/工具/参数的阶段回执及受验证的续跑；代码处理可跳过资源导出。正式差异入口保存原始 GUID/文件差异，并把稳定实体和引用变化与重打包噪声分开。函数地图迁入工具目录，展示实际生成数据身份，拒绝把旧图冒称当前覆盖。

## Changed Files

- `tools/scripts/reverse-capture-state.ps1`、`capture-doloctown-reverse-baseline.ps1`、`test-reverse-capture-stages.ps1`：阶段身份、输出完整性和中断续跑。完成输出才产生可复用回执；损坏输出保留到本基线的 `.stage-failures`，只重跑受影响阶段。
- `tools/portable-reverse-capture/run-doloctown-full-capture.ps1`、`README.zh-CN.md`、`tools/scripts/build-portable-reverse-capture-package.ps1`：只读 `Status`、离线 `Resume`、跳过资源的 `CodeOnly`、显式跨基线复用；工具仅在执行相应缺失阶段时解析/安装。兼容旧 Reuse 参数，但不再以目录存在为通过条件。
- `tools/reverse-capture/` 与 `tools/scripts/compare-doloctown-reverse-baselines.ps1`：已有基线的正式代码/资源差异入口，默认只比较代码，不启动解包。
- `tools/native-function-map/` 与 `tools/scripts/build-native-function-map-data.ps1`：地图工具迁移到 `workbench`，显示实际生成基线；显式输入身份和新生成数据输出 SHA 验证，拒绝混合图数据。原有三个数据 JSON 未重建或篡改身份。
- 本 Update；月表和共享路由由工作空间建设主任务统一更新。

## Validation

- PASS：7 个相关 PowerShell 脚本 AST 解析；`test-reverse-capture-stages.ps1` 分别在 Windows PowerShell 5.1 和 PowerShell 7 执行。微型自编 fixture 覆盖只读 status、输入/工具/参数改变、同长度同时间内容损坏、中断、额外输出、路径越界、仅相关阶段重跑、离线/跨基线复用、portable 文件布局、新快照和同源修复。
- PASS：`python -m unittest discover -s tools/reverse-capture -p test_compare_baselines.py -v`，7 项；涵盖 GUID/fileID 改写、真实引用变化、未知引用和重复实体不假报相等、保留原始 diff、相同程序集不访问代码/资源树，以及地图输入缺失拒绝和实际身份记录。
- PASS：`node tools/native-function-map/test_workbench.cjs`；读取保留的真实 `23465763 / workshop / 38581EE024D3808D4D73098E10E5A122F93AC31BEDE9961D709A0B68571D7228` 数据，确认 42,925 个方法、74,488 条内部边及显示身份；新回执输出 SHA 和混合数据拒绝也通过。该旧图仍是其生成时点的研究覆盖。
- PASS：已有 `24966367_public_958EAF` → `25163613_public_604898` 实际代码比较，main 得到 1 added / 1 removed / 12 changed / 3,658 same，firstpass 直接按相同 DLL 复用。仅对选定 `recipe_tbrecipe.json` 执行资源语义比较，保留 raw diff 并识别真实变化。
- PASS：真实 Windows PowerShell 5.1 同进程读取上述两基线 firstpass 引用，均为 87 个依赖、不含 main，依赖指纹相同。修复了 PowerShell 5 JSON 数组包装差异与重复 reflection-only 加载降级；同名不同 SHA 则保守失效，不沿用旧引用。
- 未运行：真实游戏、完整 AssetRipper/ILSpy 捕获、新 portable ZIP 发布、11 基线全量重哈希。现有输入足以验证本次工具改造；不据此新增游戏行为、API 准入或完整捕获 PASS。

## Evidence

- 历史依据：[研究基线与差异分析](../../knowledge/reverse/research-baselines.md)。当前工具使用规则见 [capture](../../../tools/portable-reverse-capture/README.zh-CN.md) 和 [compare](../../../tools/reverse-capture/README.md)。
- 地图迁移原件：`docs/debug/evidence/WORKSPACE-CONSTRUCTION/20260908/originals/docs/reviews/api/native-function-map/`，另由 `64eafe5e4c36495037774f734d38e5c2a83de7ee` 固定迁移后位置。
- 本地证据根：`references/doloc-town/reverse/workspace-construction/20260908-0006/`。内含 `stage-tests-ps5.log`、`stage-tests-ps7.log`、`firstpass-dependencies.json`、`compare-10006-10007/comparison-summary.json`、`resource-recipe-10006-10007/comparison-summary.json` 和原始差异；不发布官方源码/资源正文。

## Rollback Notes

按本 Update 的明确文件范围恢复工具代码；地图原件保存在建设快照和 Git 检查点。阶段输出只写专用 ignored/external baseline，失败产物保留；既有基线本次只读，差异和验证输出独立存放。不修改官方安装、存档和发布目录。

## Follow-Up

本工程验证完成，主任务统一同步月表、任务路由和集成检查。下一次确需捕获新基线时再实际执行对应阶段；缺少旧工具/输出证明的历史目录仍为未证明，语义比较遇到不确定引用仍保留 unresolved。
