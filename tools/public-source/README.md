# 固定对象导出与贡献回流

`export-source.ps1` 读取完整提交 OID `S` 中的规则和工具源码。工具先构建该提交的实现，再从 `ls-tree -z` / `cat-file --batch` 获取原 blob 与 mode；工作树变化只进入未纳入报告。公开根文档及 Git 配置有可逆映射，代码与项目不做字符串替换。本地 `.gitignore` 和 `.gitattributes` 原样保留，公开副本只描述公开工程的生成物与必要字节规则。

0.7.5 的公开边界由 `export-policy.json` 执行：框架、SDK、可选模块、已分类独立 Mod、必要测试和作者文档可导出；内部 Catalog、发布文案、研究、计划、规则和验收记录留在内部 Git。Mod（含 SDK 中的 Mod 示例）的 Markdown/说明文件自动排除，新增未知组件仍会拒绝。代码、项目、清单和必要内容数据继续导出；许可证须单独保留。SDK 使用 `author-sdk/API-STATUS.md`；Core 按实际依赖加载包，不要求 SDK 生成的准入材料，公开测试不读取内部发布记录。可选媒体不因同目录源码公开而自动取得分发资格，现有未授权媒体继续排除。

Git push 发送提交及其祖先，不能按文件过滤。公开交付必须推送工具产生的纯导出/候选提交，不能把内部工作分支直接推到公开远端，也不能用 `.gitignore` 代替导出边界。普通合并后的文件删除不会抹除旧公开历史；本工具不改写历史。

```powershell
./tools/public-source/export-source.ps1 -Action Plan -SourceCommit $S -OutputRoot $newJob
./tools/public-source/export-source.ps1 -Action Export -SourceCommit $S -OutputRoot $anotherNewJob
./tools/public-source/export-source.ps1 -Action PreparePr -ExportDirectory $anotherNewJob -PublicRepository $publicClone -PublicMainCommit $P -Bootstrap
# 审阅 bootstrap-review.json，对每个覆盖/P-only 文件填写决定、准确 hash 与原因，再传 -DecisionsPath。
```

输出必须是与内部仓库、公开仓库及游戏/上传输入都不重叠的新目录；额外输入根可用 `-ProtectedPaths` 声明。没有覆盖或 Force 选项。失败留下本轮诊断，既有工作树不变。`objects.git` 是独立对象库；`tree` 是纯 N，`candidate` 是合并 M。

## E / P / N

`E` 是上次合入后确认的纯导出提交，ref 为 `refs/heads/codex/source-export`。`P` 是准确公开 main，`N` 是新纯导出。后续 Export 传 `-PublicRepository` 和 `-ExportBaseCommit E`，N 仅以 E 为父节点。PreparePr 使用明确 E 进行三方合并。冲突保留 Git stage 输出；不能把冲突 tree 当成已通过候选。

候选 U 以 P、N 为父，保留公开历史。最终 M 重新检查可信规则、路径、载荷、许可、链接与实际构建测试。首次 Bootstrap 必须逐项处理公开独有与覆盖项。修改规则需单独审阅，以新内部 S 重导；候选不能修改规则来批准自身。

`Finalize` 只接受已合入 main 的准确 merge commit H，并核对 P、U、N、M 和完整干净的 PublicWindows 报告；随后用比较并交换把 E 推进到 N。main 前进、未合入、不同树、squash/rebase 或 CAS 失败均拒绝。中断后相同 H/N 可幂等重试。工具不操作远端；发布 PR、保护分支要求与远端 ref 更新是维护者的独立交付步骤。

当前验收报告为 schema 3：按选定工程的 MSBuild 输入、选定脚本/数据和产物决定复用及运行中失效；无关文档、暂存状态或提交不使结果失效。报告分别记录构建来源与验证开始/结束的 Git 信息，相关输入或产物变化保留 `invalidated` 结果。`NoBuild` 必须匹配所选套件的构建收据。Finalize 兼容 schema 2/3，继续要求准确、干净且完整的候选与完整 PublicWindows 选区；执行项只能 `passed`，仅 `build-only` 项允许 `built`，输入／输出前后摘要必须一致。CI 的受影响选区报告不能冒充完整导出验收。

## 回流

`-Action Import -SourceCommit <内部基线> -PublicRepository <公开仓库> -PublicCommits <按序提交数组> -PullRequestUrl <PR URL> -OutputRoot <新目录>` 生成内部 scratch 候选。读取同一规则反向映射根文档，按 Git 三方处理修改、重命名、删除、mode 与二进制，保留 Author、AuthorDate 和 PR/原 commit 来源。内部 README 不会被公开 README 覆盖。映射变化或冲突阻断，维护者审阅与运行受影响测试后再取用候选。

## 验证

`check-public-source.ps1 -TreeCommit <候选> -PolicyCommit <可信规则提交>` 从可信提交构建检查器；CI 不使用 PR 自己修改的规则来放行内容。公共构建／测试由 [test-public-source.ps1](../scripts/test-public-source.ps1)执行，分类在 [suites.json](../../tests/DTMAPI.UnitTests/suites.json)。导出工具的 `self-test` 覆盖对象读取、边界与 Git 同步协议；实仓闭包和 Windows／WSL 成本记录属于具体交付。

首次引入工具、或明确修改规则时，维护者审阅准确 N 后设置仓库变量 `DTMAPI_EXPORT_POLICY_COMMIT` 为已取得的可信提交 OID。普通 PR 默认使用 base 的规则；没有该初始信任输入时 CI 拒绝，不能让首次 PR 自行宣称规则已获批准。变量若不再需要，删除后恢复 base 规则。此步骤属于实际远端 PR 交付，离线工具不会替维护者改仓库设置。
