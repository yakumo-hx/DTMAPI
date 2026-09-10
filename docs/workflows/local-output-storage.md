# 本地产物与保留

这份说明用于调整输出脚本、专项整理或诊断磁盘占用。普通 Mod 修改仍按[产品验证](product-change-validation.md)完成，不增加全盘扫描。

| 位置 | 用途和默认入口 | 保留方式 |
| --- | --- | --- |
| `src/**/bin,obj`、`products/**/bin,obj`、`tests/**/bin,obj` | MSBuild 的项目输出 | 随输入重新构建；运行 `-NoBuild` 前保留本次已编译结果 |
| `.tools/` | 固定工具链、缺项安装、可复用 SDK | `prepare-workspace.ps1` 按需准备；`prepare-author-sdk.ps1` 校验输入及产物后复用 |
| `tmp/test-runs/` | 有 owner、receipt 和 lease 的测试会话 | 现有 `cleanup-test-artifacts.ps1` 只清理已完成或过期且未占用的会话；失败摘要保留 |
| `temp/` | 明确工作包的临时构建、编译参考 fixture、一次性调查 | 使用独立子目录；只有已确认可重建或有等字节副本的对象可清理。历史失败中的唯一材料保留 |
| `dist/` | SDK、产品、玩家包和可交付工具包 | 包装脚本的显式输出；发布冻结包和仍被验收使用的候选不能当临时文件清掉 |
| `dist/audit-packages/` | 新审计交付的默认位置 | `update-audit-package.ps1`；旧 `-OutputRoot` 参数可继续指定独立路径 |
| `docs/debug/evidence/` | 实际运行、玩家案件、无法重建的调查证据和本次迁移原件 | 按现有引用/证据保留规则管理；唯一玩家原件与冻结基线保留 |
| `references/doloc-town/reverse/` | 带游戏基线的私有完整捕获及派生资料 | 按捕获阶段的输入、工具与输出证明复用；不纳入普通临时清理 |

`temp` 和 `tmp` 的既有目录不为了统一名字再搬一遍。新脚本优先使用对应入口；调整旧脚本时逐项改变默认值，保留必要参数兼容。函数地图静态工具在 `tools/native-function-map/`，其数据必须展示自己的真实游戏基线。

## 专项清理

先列出具体文件、字节数、最后修改时间、引用和保留理由。递归删除前必须重新核对绝对目标在声明的工作目录内、没有重解析点、没有活跃进程/lease、内容未在审查后变化。不能确认用途的目录继续保留。

重复副本以文件内容及树结构证明，清单写明 canonical 路径和恢复方式；保留原报告，不改写报告中当时使用的命令或路径。可重建产物需要明确原输入及生成入口。只删除清单内对象，不把一次清理扩为每次任务的收尾步骤。

## 审计包

审计包里的源码快照保留完整 `docs/` 和 `tools/`，历史正文、附件及模块依赖随其真实位置交付。`audit/docs/README.md` 和 `audit/tools/scripts/README.md` 只导航，不再复制第二套内容。

独立 Wiki 工作区 `.codex/wiki-maintenance/` 不随工程审计包复制；其中的并行改动继续由 Wiki 自己的交付流程维护。

默认打包本次明确选定的 `-EvidenceIds` 和 `-ReportZipPaths`；没有选定证据就是源码/文档审计包，不声称做过游戏测试。旧固定证据集合与上一包的研究摘录仅在显式 `-LegacyEvidenceSet` 时沿用；单独选用研究摘录可传 `-ReverseSnippetsSource`。

构建器拒绝源码与输出重叠、越界、重解析点及冲突文件。先完成临时目录/ZIP，再替换交付物；替换失败恢复上一份。已有交付保留为 `.bak`，中断的 staging 保留诊断，二者都需按上面的具体清单复核后清理。打包本身不触发构建、测试或游戏启动。

本次建设的实际清理范围、结果与证据由[输出工作包](../updates/2026/20260908-0008-workspace-output-retention-and-audit-delivery.md)维护。
