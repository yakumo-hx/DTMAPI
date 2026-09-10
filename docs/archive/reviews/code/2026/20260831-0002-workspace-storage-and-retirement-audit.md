# DTMAPI 工作空间容量与历史资产退役审计

- Date: `2026-08-31`
- Status: `recorded`
- Area: `governance/storage/docs/tests/evidence/reverse-builds/worktrees`
- Source request: 用户要求审计工作空间总体积，识别纯粹无用文件，并研究历史文档、测试、历史解包文件及运行证据的彻底退役或清理方案
- Scope: 主工作树 `E:\Python_project\DTMAPI`、两个关联 Git worktree；不含真实 Steam 订阅目录、本地官方 `MODS`、游戏安装目录或 Codex 全局依赖目标
- Current authorities: [`PROJECT.md`](../../../../../PROJECT.md), [document governance](../../../../workflows/document-governance.md), [evidence retention](../../../../debug/protocols/evidence-retention.md), [test artifact retention](../../../../debug/protocols/test-artifact-retention.md), [reference boundary](../../../../../references/README.md)
- Resolution: 本记录只给出处置分类、容量区间和验收门；本轮未授权或执行删除。任何实施应由一个后续 Update 记录，并在正式证据删除前取得明确批准

## 1. 结论

主工作树当前有 `155,959,068,143 B / 145.248 GiB`、654,532 个普通文件。
两个关联 worktree 另有 4.641 GiB；三者合计 `149.890 GiB`。

这不是“代码仓库太大”。Git 跟踪内容约 0.206 GiB，约 99.76% 的主工作树
逻辑字节是 ignored 本地产物、正式证据或 local-only 参考资料。容量高度集中在：

1. `docs/debug/evidence`：58.867 GiB；
2. `references/doloc-town/reverse/builds`：46.204 GiB；
3. `temp`：31.719 GiB；
4. `tmp`：3.574 GiB；
5. `dist`：1.958 GiB。

历史 Markdown 不是容量问题。除正式 evidence 外，整个 `docs` 只有约 46.6 MiB；
批量删除 Goal、Review、Update、Issue、Hook 或 smoke 历史会破坏审计链，却几乎
不释放空间。文档治理应解决默认上下文和权威路由，原始字节治理应集中在
evidence、reverse capture、重复 package/staging 和遗留测试树。

当前可以直接归类为低风险可再生物的主要是 `bin/obj`，约 945.08 MiB。
大量旧 package/staging 很可能无用，但必须先排除仍活动任务、选出唯一规范副本并
保存小型 receipt/hash。正式 evidence 和 dirty worktree 不能凭年龄或“没有 Git 引用”
直接删除。

## 2. 计量口径

- 统计普通文件逻辑长度，`1 GiB = 2^30 B`；包含隐藏文件和 `.git`。
- 显式跳过 11 个 reparse point/junction，不跟随外部目标。若跟随，会虚增约
  1.48 GiB 和 38,966 个文件，并可能越出工作空间边界。
- 数值不含目录/MFT 开销，也不等于 NTFS 实际分配簇；压缩、稀疏文件和硬链接
  可能让物理占用不同。
- Git 分类因若干超长路径只能视为近似值；文件系统总量更可靠。
- 工作树存在并行未提交工作，因此这是扫描时点快照，不是冻结清单。

## 3. 容量快照

### 3.1 主工作树

| 一级目录 | 普通文件 | 逻辑大小 |
| --- | ---: | ---: |
| `docs` | 113,492 | 58.913 GiB |
| `references` | 450,581 | 46.395 GiB |
| `temp` | 55,434 | 31.719 GiB |
| `tmp` | 10,962 | 3.574 GiB |
| `dist` | 3,683 | 1.958 GiB |
| `.tools` | 6,272 | 1.293 GiB |
| `src` | 3,081 | 0.646 GiB |
| `tests` | 2,425 | 0.283 GiB |
| `.tmp` | 1,486 | 0.206 GiB |
| `.git` | 624 | 0.132 GiB |

Git 近似分类为：

- tracked：0.206 GiB；
- untracked：0.013 GiB；
- ignored，排除 junction：144.896 GiB；
- `.git` 本体：0.132 GiB。

`.tools` 不能按普通缓存整体删除。当前规则要求仓库本地 .NET 8 SDK/runtime，
AssetRipper、ILSpy 和离线依赖也有现存消费者。只能在各工具的解析器证明不再需要
某个版本后单独清理。

### 3.2 关联 worktree

| 路径 | 分支 | HEAD | 普通文件 | 逻辑大小 | 最新文件 |
| --- | --- | --- | ---: | ---: | --- |
| `E:\Python_project\DTMAPI-pet` | `codex/pet-20260617` | `8caf8403` | 8,850 | 1.421 GiB | 2026-06-18 |
| `E:\Python_project\DTMAPI-vehicle` | `codex/vehicle-20260617` | `8caf8403` | 14,511 | 3.220 GiB | 2026-06-18 |

两者都含 modified 和 untracked 工作。旧时间戳不是删除许可。应先逐项审查 diff、
保存未跟踪文件清单，按用户选择 commit、导出 patch/bundle 或确认舍弃，最后使用
Git worktree 命令移除；不得直接递归删除目录。

项目 Catalog 指向的仓库同级 retained-artifact authority 当前存在，约 0.218 GiB，
本表未计入。把文件单纯移到该目录只能缩小“工作空间”，不会释放磁盘；真正回收
空间仍需压缩/去重并验证后删除活副本。

## 4. “纯粹无用”的判定

一个文件只有同时满足以下条件，才应进入无需历史迁移的直接清理批次：

1. 可由当前跟踪源码和规定工具链重建，或为空目录；
2. 没有活动进程、lock、lease 或当前任务消费者；
3. 不是 Catalog、subscription manifest、latest release Update、正式 evidence
   allowlist、历史不可重取官方字节或 dirty worktree 的组成部分；
4. 没有 tracked 文档把它当作持久证据；
5. 删除目标是精确解析后的路径，且不穿过 reparse point。

`ignored`、`untracked`、时间久、没有 Markdown 引用都只是候选信号，不是充分条件。

按这个标准，本次确认的直接可再生候选为：

| 类别 | 大小 | 处置 |
| --- | ---: | --- |
| `src/**/{bin,obj}` | 655.62 MiB | 无构建进程时按精确目录清理 |
| `tests/**/{bin,obj}` | 285.95 MiB | 无测试进程时清理；不删测试源码 |
| `products/**/{bin,obj}` | 2.47 MiB | 同上 |
| `author-sdk/**/{bin,obj}` | 1.04 MiB | 同上 |
| `.git` 报告的 13 个临时 garbage object | 3.92 MiB | 仅通过 Git maintenance 处理，不手删 object |
| 空目录 | 可忽略 | 只改善整洁度 |

`artifacts/multiplatform-installer` 的两个宿主字节与当前 `dist` 副本哈希一致，
但当前构建器仍把前者作为默认输入，所以它们不是现阶段纯垃圾。应在构建器改为
单一 staging 后再清理，容量收益约 23.9 MiB。

## 5. `temp`、`tmp`、`dist` 与重复包

### 5.1 `temp`

`temp` 有 486 个一级条目、31.719 GiB：

- 2026-08 以前：21.182 GiB / 324 项；
- 2026-08-01 至 15：9.095 GiB / 138 项；
- 2026-08-16 至 31：1.441 GiB / 17 项；
- 空目录：7 项。

其中有 467 个 ZIP，共约 15.0 GiB。126 份同名同版本 Author SDK ZIP 合计约
14.9 GiB，另有成批约 242 MiB 的重复 SDK/runtime staging。它们是最强的
可重建缓存候选，但本次未对所有大文件重新计算内容哈希，不能仅凭相同名称和大小
断言逐字节相同。

清理前应：

1. 排除仍活动的 8 月下旬工作根；
2. 对 ZIP/staging 按树哈希分组；
3. 通过 Catalog、subscription manifest、latest release Update 和现有 retained
   artifact 选出唯一规范字节；
4. 把必要 build report、tree hash、receipt 和人工交付件迁入耐久位置；
5. 对当前 Author SDK 与 Runtime/Workshop 候选至少执行一次聚焦重建。

### 5.2 `tmp/test-runs`

该目录约 2.309 GiB，有 151 个直接顶层目录：116 个没有 tracked Markdown 引用，
35 个仍被 22 个历史 Issue、Review、Update 或 workflow 引用。

现有 [`cleanup-test-artifacts.ps1`](../../../../../tools/scripts/cleanup-test-artifacts.ps1)
只识别 `<suite>/<session>/session.json`、owner 为 `DTMAPI.TestSession`、schema 1 的
会话。8 个 Runtime Installer v2 stress 根把 receipt 放在顶层并使用另一 owner，
其中 6 个还含 junction，因此全部永久漏扫；`-IncludeLegacy` 又只覆盖系统临时目录
下的四种旧根，不覆盖 repo-local 遗留树。

不要扩大通用 cleaner 去删除任意子目录。应创建一次性的精确候选 manifest，逐项
记录绝对路径、解析后边界、文件数、字节、年龄、owner/receipt、Markdown 引用、
reparse 目标和活动进程。先处理 116 个无引用根；35 个有引用根先把真正需要的
summary/hash/receipt 迁入 canonical evidence，或明确登记证据已经过期。含 junction
的项目必须 fail-closed。

后续应修正 Runtime Installer stress 生产器，使新运行进入现有受管 TestSession
生命周期或有显式兼容适配；还应禁止 tracked Markdown 把 `tmp/test-runs`、`temp`
或 `dist` 路径当作唯一耐久证据。

### 5.3 `dist`、`.tmp` 与交付输出

`dist` 为 1.958 GiB，其中当前多平台目录只有约 0.028 GiB，其余包含多组
0.5.5、0.6.0、Batch 5、prerelease、quarantine、local/source 候选，每组约
68–71 MiB。旧 `dist` 字节不是当前发布权威。选出当前/latest 候选并核对 retained
artifact 后，预计可回收 1.4–1.8 GiB。

`.tmp` 为 0.206 GiB，主要是旧 inventory-loss audit 副本、旧 dotnet-tools 和
collector smoke；关单并保存必要摘要后，预计可回收 0.15–0.20 GiB。

`output/outputs` 合计约 16 MiB，含用户交付和预览，不得整目录按垃圾删除。

排除正式 evidence 和 reverse build 后，`temp`、旧 `dist`、旧 `tmp` 与 `.tmp`
的高置信清理池约 33–36 GiB。

## 6. reverse build 与历史解包退役

`references/doloc-town/reverse/builds` 有 10 个 build、46.204 GiB：

- 8 套 AssetRipper Unity project：32.184 GiB；
- 8 套 raw snapshot：13.323 GiB；
- maps：0.261 GiB；
- metadata：0.221 GiB；
- 10 套 decompiled source：0.087 GiB；
- full-baseline inventory：0.086 GiB。

完整 build 大多为 5.45–5.92 GiB。Wwise/MUSIC 等大文件在 raw、AssetRipper 和
相邻 build 间高度疑似重复，但本次没有读取数十 GiB 做全量哈希，不能仅凭长度
直接去重。

### 6.1 仍有消费者的最小集合

| Build | 必须保留的消费者边界 |
| --- | --- |
| `23465763` | native function-map 的 metadata/maps 默认输入 |
| `23762374` | Advanced fixture 所需精确 DLL、多个 native trace 所需 decompiled/Scripts、Steam identity appmanifest |
| `24256979` | Steam appmanifest identity fixture |
| `24456188` | Advanced fixture 所需精确 DLL、多个产品 trace 的 decompiled/Scripts |
| `24966367` | AnimalPack 静态测试所需两份配置、当前 DebugConsole/native trace；可暂作唯一完整基准 capture |

`24567135`、`24585411`、`24650773`、`24788406` 未发现代码或工具直接消费完整树，
只有历史文档引用，可作为第一批薄化对象。历史文档指向一个 build 身份，不等于
必须永久保留数 GiB raw/AssetRipper 副本。

### 6.2 目标形态

每个历史 build 在工作树中只保留：

- README、Steam build/depot identity；
- compact inventory、树哈希、与相邻 build 的差异摘要；
- 被聚焦测试精确消费的 DLL、appmanifest、少量配置、metadata/maps 或
  decompiled source；
- 对不可重取官方字节的离线归档位置与校验信息。

先把四个中间 full build 薄化，预计可回收约 23.1 GiB。再把所有历史 build 改为
薄引用视图、只留一个完整基准，并去重 Wwise/StreamingAssets，reverse 总体预计
可回收约 35–44 GiB。

raw snapshot 可能无法从 Steam 重取，应先压缩/去重到 local-only 冷存储并验证
恢复，不能直接删除。AssetRipper project 是派生物，但只有在保留 raw、工具身份、
inventory 并完成一次独立重建后，才能宣告可再生。任何官方或第三方字节都不得
提交或再发布。

focused 验证至少包括：Advanced reference-game fixture builder、Steam appmanifest
identity test、native function-map generator、AnimalPack static test 和相关 trace
检查；不需要启动游戏或运行完整 Release suite。

## 7. 正式运行证据

`docs/debug/evidence` 为 58.867 GiB，其中 `GAME-SMOKE` 55.280 GiB。按扩展名：

- 1,848 个 `.dmp`：30.041 GiB；
- 21,320 个 `.log`：12.878 GiB；
- 452 个 `.zip`：6.608 GiB；
- 9,811 个 `.png`：6.455 GiB；
- 911 个 `.data`：1.387 GiB。

六份大型进程转储共 27.423 GiB。当前 allowlist 精确保护其中四份，共
18.341 GiB；另两份没有 tracked Markdown 或 allowlist 的精确引用，合计
9.082 GiB，文件名为：

- `DolocTown-24776-fatal-live-dbghelp.dmp`；
- `DolocTown-8448-fatal-live-dbghelp.dmp`。

它们是高收益复核候选，但仍不是自动删除授权。当前
[`evidence-retention-allowlist.json`](../../../../debug/evidence-retention-allowlist.json)
已落后于 dirty 文档引用；`build-evidence-retention-allowlist.ps1 -Check` 因新增
14 个 smoke run 身份而失败。在工作树稳定、allowlist 重生成并通过检查以前，
任何正式 evidence 清理都处于阻塞状态。

默认的 `test-runtime-evidence-retention.ps1` 还把 Batch 5/6、0.5.5 prerelease、
Candidate11 和 Workshop audit 的精确历史 evidence root 集合作为永久门。只重生成
allowlist 并不能释放这些字节。应拆成：

1. 默认门：动态 schema、路径边界、reparse 安全、allowlist 一致性和少量仍会
   回归的 live invariant；
2. 手动历史门：冻结里程碑 root 集合的审计重放。

正式 evidence 的后续处置必须复用现有 allowlist 和 cleanup manifest，不新建第二套
证据权威。删除前须哈希规范副本、输出 dry-run 文件/字节清单、核对 receipt 与分析
状态，并取得明确删除批准。失败、唯一 crash signature、发布验收、存档语义或仍开放
Issue 的原始证据，应优先保留；例行成功运行可以在规则迁移后只保留 compact summary、
hash 和必要代表样本。

## 8. 历史文档治理

### 8.1 不应为省空间批量删除

- `docs/goals/2026` 的 80 个历史文件合计仅约 0.25 MiB，且治理检查显式锁定数量；
- `docs/updates` 约 4 MiB；
- `docs/reviews` 的 Markdown 约 8 MiB；
- planning、Hook、smoke、Issue 历史合计仍只有约 1–2 MiB。

这些文件保存需求、根因、验收与发布审计链。应保持 frozen/retired 原位路由，避免
进入默认上下文，不应靠删除换取微小容量。

异常是 [`native-function-map/data`](../../../../../tools/native-function-map/workbench/README.md)：
`methods.json` 与 `links.json` 合计约 32.5 MiB，是由 local reverse metadata 生成的
旧 build 缓存，README 已声明它落后于当前 reverse reference。建议在一个独立 Update
中把大 JSON 改为 ignored/on-demand 产物，只跟踪生成器、workbench 壳和小型 summary；
迁移前先让前端能够处理“数据未生成”状态。

### 8.2 下一批上下文退役候选

- [`docs/planning/Debug.md`](../../../../planning/Debug.md)：与已退役原始规划同型的旧问答式
  长文，仍被 `AGENTS.md` 强制读取；建议冻结全文、原路径改成 compact handoff，并把
  当前事实转交 `PROJECT.md`、Debug Index/Issue/protocol 和 document governance。
- `docs/planning/20260603-flow-out-new-mods-official-json-dtmapi-research.md`：无 lifecycle、
  几乎无入站消费者，可审查后冻结归档。
- 两个 20260712/20260727 roadmap：先解除 Batch 6/G2/allowlist 测试耦合，再冻结。
- 两个 20260731 roadmap：仍被 Issue/Review/Update 当作 deferred next-version authority，
  暂不可退役；先逐项确认 debt 是否仍开放。
- 根目录 `构想.md`：13 KiB 的早期联机 Mod 研究/对话底稿，没有入站引用；具有研究
  来源价值但没有当前权威地位。可由用户选择冻结到 planning archive 或删除，不能仅凭
  文件名判为纯垃圾。

旧 official docs crawl 共约 137 MiB。较早 crawl 约 52 MiB；在覆盖率比较后可只保留
manifest、链接、规范化正文和差异摘要，预计回收约 45–50 MiB。网页原始内容可能消失，
因此应先留离线哈希/归档，收益也远低于 evidence、reverse 和 temp。

## 9. 历史测试退役

[`tools/scripts/test.ps1`](../../../../../tools/scripts/test.ps1) 当前直接运行 33 个脚本，
其中仍有多组历史 milestone replay：

- Batch 6 Phase 0 historical contract；
- G2 synthetic/authority/receipt closure；
- Batch 4 inventory/QA 历史边界；
- Batch 5 GC ladder 与 no-demand profile；
- evidence retention 中精确冻结的历史 root 集合。

Phase 0 和 G2 脚本硬锁旧 commit、旧 hash、旧 roadmap 文句及里程碑数量，应移出默认
suite，保留为手动 frozen audit replay。Batch 4 QA、Batch 5 GC/no-demand 是混合门：
先把当前仍可能回归的最小 live invariant 收敛到 Catalog、通用 source checker 或
unit tests，再把历史库存、receipt 和终态提交重放移出默认路径。

不能按 `Batch6` 文件名前缀批量删除。当前产品 source/policy、AutoFishing behavior/GC、
Catalog 和 retained ABI 仍保护活约束。AutoFishing manager lifecycle 的小型负面测试
仍保护旧 `<game>/Mods` lane fail-closed，可保留但改成中性名称，并消除重复执行。

推荐顺序：

1. 为每个旧 gate 写出仍有效的最小当前不变量；
2. 在现行通用门中补足这些不变量；
3. 新旧 gate 并跑一次，证明替代覆盖；
4. 从默认 `test.ps1` 移除历史重放；
5. 把旧脚本、contract、baseline 标成 manual/frozen；
6. 再冻结依赖它们的 roadmap 和证据集合。

这项治理主要减少默认测试时间和维护耦合，不会显著减少源码磁盘占用。测试目录的
实际容量主要来自 ignored `bin/obj`。

## 10. 分批实施方案

### Batch A：可再生物与包缓存

- 冻结活动写入并确认无构建、测试、打包进程或 lease；
- 清理精确 `bin/obj`；
- 对 `temp`/`tmp`/`dist` 按 hash 分组，保留 current/latest 规范件及小型 receipt；
- 排除 8 月下旬仍活动的 guardian/package 工作根；
- 执行当前 Author SDK 与 Runtime/Workshop 聚焦重建；
- 预计回收：主工作树约 34–37 GiB，含约 945 MiB build 输出；
- 不涉及正式 evidence 或 reverse raw snapshot。

### Batch B：旧 worktree 收口

- 审查两棵 worktree 的 tracked diff 与 untracked 文件；
- 由用户选择 commit、patch/bundle、迁回主线或舍弃；
- 保存分支后用 Git worktree 机制移除；
- 预计回收：主工作树外 4.641 GiB。

### Batch C：reverse 薄化

- 生成一次性 exact consumer/candidate manifest，不新增长期 receipt 家族；
- 先薄化四个无完整树消费者的中间 build；
- 在独立临时副本模拟保留集并通过聚焦验证；
- 对不可重取 raw snapshot 先做 local-only 压缩/去重归档及恢复校验；
- 预计回收：保守约 23.1 GiB；全面薄化约 35–44 GiB。

### Batch D：测试和文档解耦

- 把历史 milestone replay 从默认 suite 分离；
- 迁移当前 live invariant；
- 退役 `Debug.md` 和已关闭 roadmap 的默认上下文角色；
- 把 native function-map 大 JSON 改成按需生成；
- 以文档治理和聚焦 source/unit checks 验证，不运行游戏。

### Batch E：正式 evidence

- 先稳定 dirty 文档并重生成 allowlist；
- 拆分默认 live retention check 与手动 milestone replay；
- 生成 allowlist-aware dry-run manifest，核对 hash、receipt、Issue 和 reparse；
- 先复核两份合计 9.082 GiB 的孤立大型转储，再处理 routine nested duplicates；
- 取得用户明确删除批准后才执行；
- 每批保存删除前清单、删除后容量差异和可恢复性说明。

## 11. 回收量区间

下列区间互不重复时才可相加：

| 方案 | 预计回收 | 条件 |
| --- | ---: | --- |
| build `bin/obj` | 0.923 GiB | 无活动构建/测试 |
| 旧 package/staging/`tmp`/`dist`/`.tmp` | 33–36 GiB | 规范件、receipt、当前任务排除完成 |
| 两个旧 dirty worktree | 4.641 GiB | 先保存或明确舍弃未提交工作；位于主根外 |
| 四个中间 reverse full build 薄化 | 约 23.1 GiB | consumer manifest 与聚焦验证通过 |
| reverse 全面薄化 | 35–44 GiB | 包含上一行，不能重复相加；冷存储恢复验证通过 |
| 两份孤立大型 process dump | 9.082 GiB | 当前阻塞；allowlist/receipt/显式批准完成 |
| native function-map 大 JSON | 约 32.5 MiB | 前端支持按需生成 |
| 旧 official docs crawl | 约 45–50 MiB | coverage/diff/offline archive 完成 |

不碰正式 evidence 时，高置信 package/temp 清理约 33–36 GiB；加四个中间 reverse
build 薄化约 56–59 GiB。若将整个 reverse 体系改成薄引用集，总回收约 68–80 GiB，
另可单列 build 输出和旧 worktree。正式 evidence 的广泛回收量不能在 allowlist
失配时诚实估算，本记录只确认两份 9.082 GiB 的高收益复核候选。

## 12. 验证结果与限制

- 只读文件系统扫描完成，跳过所有 reparse point；
- Git tracked/untracked/ignored、object 和 worktree 状态已检查；
- `check-test-artifact-governance.ps1` 通过；
- evidence allowlist `-Check` 失败，原因是当前 dirty 文档新增 14 个 run 身份；
- 没有运行会写 manifest 的 cleanup preview；
- 没有启动游戏、获取 Runtime lock、修改 Steam/官方 `MODS`、移动或删除文件；
- 没有为相似大文件完成全量内容哈希，所以相同名称/长度仅作为候选信号；
- 本审计新增一份 Review，不创建 Update；后续若实施清理或治理变更，必须由单一
  owning Update 记录 changed files、dry-run manifest、验证、回滚和最终回收量。
