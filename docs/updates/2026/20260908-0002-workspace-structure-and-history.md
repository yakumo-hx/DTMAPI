# 20260908-0002: 工作空间结构与历史整理

## Metadata

- Update ID: `20260908-0002`
- Date: `2026-09-08`
- Lifecycle Status: `verified`
- Validation Level: `docs, source`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: 用户批准完整工作空间建设计划：先结构与迁移，再全量逐篇历史审阅、知识提炼及工程流程重构。依据 [工作空间审查](../../reviews/code/2026/20260908-0002-workspace-structure-and-workflow-review.md)。

## Summary

统一历史入口和现行知识归属，保留历史原件及发布身份，减少日常导航和重复记录。本文拥有结构、迁移和全量历史审阅；独立工程工作包的 Update 在实施后链接于此，不另设重复状态表。

实施顺序：原件基线 → 现行入口/迁移机制 → 已退役材料迁移 → 分主题完整阅读及提炼 → 构建/Catalog → 测试/runner → 支持/解包 → 输出清理及整体验收。每批通过对应检查后继续。

## Changed Files

- `tools/workspace-history/`：本次整理的原件快照、可回退迁移、显式阅读决策汇入和验证工具。
- `docs/archive/migrations/20260908-workspace.json`：逐项位置、原始指纹、阅读覆盖和知识归属；不拥有项目或发布状态。
- `docs/README.md`、`docs/archive/README.md`、`docs/knowledge/README.md`、current-state 及 src/tests/author-docs/references 入口：区分当前职责、研究知识和历史，补齐玩家支持与完整解包路由。
- `docs/workflows/player-support.md`、`document-governance.md`：支持入口及迁移职责；修档实现由独立工程记录接续。
- `docs/archive/goals/2026/`、`docs/archive/architecture/`、`docs/archive/releases/`：首批 83 份退役正文；另将根目录《构想》迁入 `docs/archive/planning/2026/`。五份冻结文本未搬动、未改字节。
- `tools/scripts/document-paths.ps1`、`sync-update-ledger.ps1`、`check-doc-governance.ps1`、`update-audit-package.ps1`：正文位置适配，旧 ID 保持，月表直接指向归档正文。
- `.gitattributes`：固定五份冻结原件的检出字节；其中 6 月 1 日原始规划保留原有混合换行，以原始字节入 Git，不重算冻结预期值。
- `docs/knowledge/*`：全文审阅后按实际领域合并提炼。总迁移清单持有全部逐篇处置字段；13 份临时阅读卡核对完全重复后移除，原件仍可从 Git 检查点恢复。
- 后续迁移：969 份已审阅退役正文进入 `docs/archive`，87 个冻结/结构化身份入链保留短入口；39 份本地对话归入忽略的 conversations；8 份作者交付文本与 31 个附件成组归档。7 个当前函数地图文件迁到 `tools/native-function-map/workbench`，原始基线另存。
- API matrix、动物作者指南、派生值验证指南和 Manager 设计分别保留原始正文后整理现行内容。API matrix 从 85872 字符缩到 53936 字符；契约状态未删。

## Validation

- 已通过 28 项迁移/链接工具检查：中文、空格、锚点、代码原文、带代码格式的链接标题、冲突、重复身份、中断恢复、后续修改保护、显式阅读决策、混合索引例外、附件、私有历史、当前工具及测试源码搬迁引用、冻结身份。重算当前指纹也不能使改写过的历史正文通过。
- 已通过月表工具检查：原格式、字节与其他行保留、状态同步、旧路径定位、归档正文、入口页排除及跨目录重复 ID。
- 首批文档治理通过；原件及迁移后位置/正文指纹检查通过。
- 已通过审计包正文/附件字节、跨仓库路径缓存及路径越界检查；证据保留测试通过，原有证据集合无丢失。
- 必读字符数：建设前 `10336`，本批 `10023`；未扩大必读范围。
- 全量阅读已完成：1244 份公开历史/负责文本中，1232 份全文已读，12 份为人工内容全读并核对生成部分的混合索引；39 份私有对话全文已读。不是用打开文件、摘要或哈希代替阅读。
- 后续正文与地图迁移检查通过：1099 个公开对象已换到新位置，37 个附件有原始指纹。归档正文独立从原件计算机械链接结果，五份冻结文本保持原始字节。链接标题反引号的解析遗漏已修复；新增断链为 0，12 条原有历史断链单列，不冒称本次新增。
- 知识不按审阅分工和月份建目录：26 份临时/混合主题页合入实际领域，增加 6 个必要专题；71 页收为 51 页，518178 字节降为 400641 字节。全部历史去向同步到总清单，来源和未确认边界继续保留。
- 当前 smoke 入口只保留本月行；建设前完整表通过原件工具保存到 `smoke-matrix-pre-construction-20260907.md`，包括当时的失败与历史链接，journal `20260908T025730199951Z`。没有删除旧证据或改写归档判断。
- 输出限定清理、全部工程包的本地源码/运行验收已完成：完整 Release 及公开源码入口均从候选 `e1321953` 从头通过，五份实际游戏结果按未变行为输入复用。实际审计包从 `db4ed0fd` 生成并通过独立自检及 ZIP 内容核验；源码/文档快照、阅读覆盖、验证报告和五份游戏证据完整交付，细节由 0008 拥有。
- 构建、游戏及发布验证由各工程工作包按改变范围执行；文档整理不创建游戏 smoke。

## Evidence

- 迁移前 Git 检查点：`4572a6f6cc6082739f36ab972e0383c06df7a7c6`，收录此前已验收的工作流精简；Wiki 并行改动未纳入。
- 首批结构与迁移检查点：`65f8be2fc03b5d59e41c067f65b6c91c2fab3eac`；84 份迁移、冻结原件检出字节和对应工具检查独立可回退。
- 后续历史/构建投影/离线修档检查点：`64eafe5e4c36495037774f734d38e5c2a83de7ee`；工程中的测试拆分和 runner 另批提交，Wiki 并行内容继续保留。
- 测试拆分、runner、解包与知识整合检查点：`7af49dfa5f8779e8e0f2e79190cf536825253a63`。可重建兼容 SDK 与玩家真实修复验收检查点：`c07dcde2f8c12c9205e94567cfb624dca1e6998a`；最终集成发现的纠正另有独立提交，不重写前序检查点。
- 测试源码 22 条位置映射通过机械链接 journal `20260908T022143307218Z` 记录；当前/历史实际链接修正，历史命令中的路径保持原文。完整阅读、冻结字节及新增断链为 0 的检查仍通过。
- 全文后批次 journal：`20260908T013928482353Z`；标题链接修正 journal：`20260908T014206018342Z`，均位于原件目录同级 `migrations/`，保留逐文件 before/after 和整批回退验证。
- 原始工作树文本和私有对话快照：`docs/debug/evidence/WORKSPACE-CONSTRUCTION/20260908/originals`（本地忽略）。
- 公开覆盖清单：[migration manifest](../../archive/migrations/20260908-workspace.json)。读取文件或计算哈希不能自动标记全文阅读完成。
- 最终阅读与历史检查沿用未变历史输入的 [完整检查](../../debug/evidence/WORKSPACE-CONSTRUCTION/20260908/final-history-check.log)；[最终链接检查](../../debug/evidence/WORKSPACE-CONSTRUCTION/20260908/final-links.json)覆盖 1524 份 Markdown，新增断链 0，原有 12 条缺陷单列。日常必读仍为 10023 字符，未超过 10336 字符的建设基线。
- [完整审计交付及其验收](20260908-0008-workspace-output-retention-and-audit-delivery.md)绑定实际快照提交；后续关闭记录的文档提交不改写该交付身份。

## Rollback Notes

按单批路径清单和提交恢复；保留不属于本任务的工作树修改。迁移只修改 Markdown 链接，不改写历史观察和失败。固定发布身份及冻结原件继续保留。唯一原件、玩家材料和官方冻结基线不在清理集合内。

## Follow-Up

本次建设已完成，无剩余本地验收门。工程包均已验收：[玩家支持与修档](20260908-0003-player-support-and-save-repair.md)、[构建与产品投影](20260908-0004-workspace-build-and-product-projections.md)、[测试与 CI](20260908-0005-workspace-test-projects-and-ci.md)、[解包](20260908-0006-workspace-reverse-capture-and-function-map.md)、[runner](20260908-0007-game-smoke-runner-modules.md)、[输出清理](20260908-0008-workspace-output-retention-and-audit-delivery.md)。远端 CI 执行和正式发布继续单独记录，未执行的远端结果不借用本地 PASS。历史逐篇审阅及本次全量验收不进入日常 Mod 修复流程。
