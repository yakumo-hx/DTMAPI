# 工作空间结构与五条主要工作流审查

- Status: `recorded`
- Date: `2026-09-08`
- Source: 用户要求重新摸清整个工作空间，拆解 Mod 开发、测试、维护、玩家反馈修复和游戏本体解包，找出冗余及值得完善之处。
- Scope: 结构盘点、关键调用链和代表性案例审查。不是全仓逐行代码审计，也不证明所有历史问题已解决。
- Implementation: 审查建议尚未实施。本文件不是新增必读材料、测试门槛或平台任务状态；未来实施再由对应 Update 记录。

## 1. 总体判断

工作空间已经形成了 Runtime、产品、SDK、测试、参考材料、证据和发布物的基本分层。继续把目录全部搬一遍、压缩所有历史文档，收益有限。剩余的主要问题是：**少数入口承担过多工作，同一事实还在多个地方手写，常用操作缺少准确的环境前提，历史专项流程仍占据通用工具入口。**

前几轮已经完成根必读压缩、原地 NoNativeSave/专用槽规则、Update 月表自动投影、focus 防误用、解决方案构建合并和只读状态查询。这些不再列为待实施建议，见 [0007](../../../updates/2026/20260907-0007-product-maintenance-workflow-simplification.md)、[0008](../../../updates/2026/20260907-0008-astra-workflow-routing.md)、[本轮之前的构建精简](../../../updates/2026/20260908-0001-development-entrypoint-simplification.md)。

本审查区分：

- **确认重复**：源码调用链或字段维护方式可以直接证明。
- **结构性成本**：依赖、入口或职责导致扩大的工作量；未把它换算成未经测量的时间。
- **完善项**：缺少稳定衔接或失败处理，不等于已发生数据损坏。
- **必要区分**：不同事实、风险和交付阶段的记录/验证，即使形式相似也应保留。

## 2. 实际结构与职责

审查时 Git 跟踪 3,815 个文件；这个数字不包含现有未跟踪新增文件和本地忽略树。Git 中 references 有 1,363 个文件，docs 有 1,224 个文件。docs 下 Update/Review/Goal 三类目录合计 1,097 个文件，约占 docs 的 89.6%；其中包括索引和仍在使用的研究，不能把这些目录统称为可删除垃圾。

| 区域 | 实际作用 | 应拥有的事实 / 边界 |
| --- | --- | --- |
| [src](../../../../src/README.md) | 14 个实际 csproj，覆盖 Runtime、API、SDK、诊断、安装器、QA/Compatibility | 实现和项目依赖；产品身份与原生职责规范仍在 PROJECT |
| [products/first-party](../../../../products/first-party/README.md) | 第一方产品、原型和内容；并非每个目录都已发布 | 产品行为与自身源码；身份/角色/发布门查 Catalog |
| [author-sdk](../../../../author-sdk/README.md) | 作者项目、schema、API target、Advanced policy、样例和打包入口 | 编译目标、准入、receipt 和包生成 |
| [tests](../../../../tests/README.md) | 五个可执行源测试项目、ABI harness、native/Harmony 假件、QA Mod | 源码/契约/宿主检查；不能直接证明 Unity 游戏行为 |
| [tools/scripts](../../../../tools/scripts/README.md) | 138 个 PowerShell 脚本和 2 个 Python 脚本；另有 tools/release 与 portable 工具 | 构建、部署、进程、采证、安装/发布及支持操作 |
| [docs](../../../onboarding/current-state.md) | 规范、当前事实导航、架构、问题、历史与执行记录 | 规范与历史分层；一个事实一个主要维护位置 |
| [references](../../../../references/README.md) | 官方资料、本地逆向、第三方兼容参考 | 研究输入；不进入普通构建或分发内容 |
| dist / tmp / temp / evidence 等 | 包、临时产物、诊断与回滚证据 | 可重建输出、需保留证据、冻结字节是不同类别 |
| author-docs | 当前主要是 ContentPack 作者说明 | 用户教程；不重复内部准入和发布状态的详细历史 |
| wiki / .codex/wiki-maintenance | 另一个本地 Wiki 交付区及维护记录 | 与 DTMAPI 产品流程分离；Wiki 内容不是 Runtime 事实来源 |

实际占位项也要识别：src 的 ContentPatcher、ConsoleCommands、TemplateMod 目录和 tests/DTMAPI.IntegrationTests 当前只有 README，没有相应项目。它们不能被文档描述成已经可执行的模块/测试入口。

Wiki 的 [README](../../../../wiki/README.md) 明确说明它通过 .git/info/exclude 排除、没有嵌套 Git；主仓库只跟踪相关维护资料。保留这个既有边界，但不能把主仓库提交当成 Wiki 本地交付源码的备份。

## 3. 五条工作流：最小完整闭环

### 3.1 开发或修改一个 Mod

当前可用链：

需求/已知问题 → Catalog 产品与源码 → 已有 native owner/API 事实 → 修改 → 相关源检查 → SDK 生成包 → 按变化验证实际游戏行为 → 必要目录同步和一个 Update。

应在开始时确定四件事：改什么行为、构建用什么参考目标、游戏验证用哪个实际环境、满足什么结果即结束。已有 owner 的小修不重做整个 API 架构审查；新 API/新 native 边界才进入对应设计路线。

编译迭代用 build；需要交付包用 pack。产品包必须经过 SDK 的身份、引用和 receipt 规则。普通 gameplay 采用 [产品验证流程](../../../workflows/product-change-validation.md) 的直接游戏路线；保存/故障窗口按 PROJECT 选择专用槽或隔离 fixture。

结束条件是本次改变的行为和必要回归被证明、交付物与证据对应、记录齐备。没有游戏运行就不新增 smoke；没有发布动作就不改变已发布事实。

### 3.2 测试

将测试按它能证明什么分开，而不是按脚本名字或历史批次分开：

| 检查层 | 何时需要 | 结束条件 |
| --- | --- | --- |
| 文档/元数据 | 对应文字、链接或机器投影改变 | 改动范围的格式、链接和投影一致 |
| 纯逻辑/事务 | 产品规则、计算、状态转换改变 | 相关行为及必要边界通过 |
| 契约/引用/文件系统 | Loader、SDK、路径、receipt、兼容行为改变 | 正常与相关失败输入均满足约定 |
| 独立 Harmony/兼容宿主 | Hook 所有权、清理和兼容假件改变 | 对应宿主断言通过；仍不等于实际 Unity Mono |
| 游戏操作 | 声称玩家可见/native 行为正确 | 目标行为、来源、日志、相关存档边界和干净退出 |
| 完整发布矩阵 | 明确的集成/发布边界 | 对该候选从规定入口完成所需全套检查 |

focus 是执行选择器，目前还不是独立编译边界。仅改记录不会使二进制证据失效；改变相关源码、依赖、包、游戏版本或场景输入才重新验证受影响部分。完整发布要求仍须完整通过，不能把几个 focused PASS 拼成完整 PASS。

### 3.3 日常维护与发布

分为三种变化：

- 产品版本/配置/翻译：走该产品、对应元数据和包，不扩展成 Runtime 安装器审计。
- Runtime/SDK/公共契约：走实际依赖闭包和相应兼容/安装门。
- 游戏本体更新：先确定新游戏身份和差异，再映射受影响 native owner/产品，决定重新编译还是只需兼容性实测。

构建参考版本、实际测试游戏版本、最新解包观察头必须分开。当前十个 PublishedProduct Advanced policy 中，六个仍绑定 build 23762374，四个绑定 24456188；最新 public 解包观察是 25163613。**这不是自动错误，也不证明十个产品不兼容；它说明“最新游戏目录”不能自动当成这些包的编译参考目录。** policy 不能靠修改版本号绕过。

包准备、授权范围内同步、Workshop 上传、下载订阅后核验是不同阶段。当前订阅 manifest/发布 Update 证明观察到的发布事实，Catalog releaseStop 管将来的上传范围。

### 3.4 玩家反馈与修复

先分流，避免每条反馈都变成代码修复：

| 反馈类型 | 首先需要 | 对应处理 |
| --- | --- | --- |
| 功能/操作理解差异 | 最小复现、设置、预期行为 | 解释或文档修正，确需改变行为才改代码 |
| Mod 缺陷 | 实际加载来源/版本、复现、相关日志 | 定位 owner，修复并做对应回归 |
| 安装/环境问题 | 路径、Runtime/Loader 状态、失败阶段 | Doctor/日志及匹配的安装器路线 |
| 游戏本体问题 | 游戏 build、原生事实、是否由 Mod 改变 | 核对原生差异和官方修复；不默认归因 Mod |
| 已损坏/缺状态的玩家存档 | 原件、目标槽、原生条件与明确修复范围 | 生成派生修复件、验证、交付和玩家确认 |

一般先使用日志/Doctor；需要保存数据时才选保存采集器。现有 [collect-logs](../../../../tools/scripts/collect-logs.ps1) 和 [保存/崩溃采集器](../../../../tools/release/player-save-crash-collector/collect-save-and-crash-logs.ps1) 目的不同，后者会复制发现的整个 SAVE 树，不能作为所有反馈的默认入口。

玩家存档修复与普通 NoNativeSave 测试不同：修复会真实改变需要交付的数据，原件保护、准确的槽位/输入哈希、重复执行拒绝和回滚说明有实际用途。先生成派生文件，不直接覆盖唯一原件；按明确范围比较变化，并使用交付文件本身验证可加载。若玩家后来继续保存，旧修复包不能无条件替换新进度。

重复症状进入已有 Issue；未明确的根因进入 Review；实施进入一个 Update。交付文件可加载、玩家确认问题解决是两条事实，后者未回到系统时保留相应状态。

### 3.5 游戏解包与版本研究

已有完整链是：

游戏 build/branch/manifest 身份 → 冻结 raw snapshot → AssetRipper → ILSpy → 文件清单与工具回执 → 分层差异 → native/内容事实 → 受影响产品与必要验证。

完整入口已在 [portable full capture](../../../../tools/portable-reverse-capture/run-doloctown-full-capture.ps1)，它复用 [底层 capture](../../../../tools/scripts/capture-doloctown-reverse-baseline.ps1)，不是两套独立实现。references README 目前主要路由到底层入口，容易漏掉完整链和其复用参数。

建议明确三个任务档位：

- 查某个函数/机制：选已验证基线中的相关符号，不重新解包。
- 比较代码更新：先比较身份、程序集及相关依赖，按变化反编译和分析。
- 建立完整基线、研究场景/资源或用户要求全量：执行完整 AssetRipper/ILSpy 链并保留冻结身份。

现有 InventoryOnly 会重新计算 snapshot/export 清单并核对源字节，不是轻量“查看状态”。ReuseSnapshot/ReuseExport/ReuseDecompile 已存在；需要完善其完成状态和输入绑定，见 F07。

## 4. 发现与建议

### F01 / 优先：产品打包链确定存在重复编译和 SDK 准备

证据：[通用产品构建器](../../../../tools/scripts/build-batch6-advanced-product.ps1) 先 validate、再 build、再 pack；[DeterministicPackager](../../../../src/DTMAPI.AuthorSdk/DeterministicPackager.cs) 的 pack 又调用 CodeModBuilder.Build。外层 build 的输出主要写入 summary，未成为 pack 的输入。未传 AuthorSdkRoot 时，还会在产品输出目录重新执行 SDK 自包含 publish/打包。

建议：日常编译只 build，交付只走一次完整 pack；通过 pack 已有结果记录 DLL/receipt 身份。复用明确匹配且输入未变的 SDK；源码/target/policy 或 SDK 本身改变时才重建对应部分。旧产品包装脚本保持兼容转发，不另建一套构建权威。

验收：单次普通打包只有一次产品编译；SDK 未改变时不重建 SDK；错 policy/篡改输入仍拒绝；包与 receipt 校验继续通过。正式确定性检查仍独立打两次包，不能把必要的双构建删除。此项尚未实测节省多少秒。

### F02 / 优先：开发环境准备缺少准确、无副作用的入口

证据：[Unit csproj](../../../../tests/DTMAPI.UnitTests/DTMAPI.UnitTests.csproj) 的独立 net48 假件使用 [固定 .tools/BepInEx 路径](../../../../tests/DTMAPI.UnitTests/Fixtures/EquipmentSlotsHarmonyOwnerFixture/EquipmentSlotsHarmonyOwnerFixture.csproj)。build 入口解决 .NET 8，但未发现配套的源码测试依赖准备入口；[install-bepinex](../../../../tools/scripts/install-bepinex.ps1) 则面向真实游戏安装，不能用来冒充只准备开发依赖。现有缓存下成功不等于新检出已可复现。

建议：复用现有工具解析/固定包能力，提供按操作选择的只读 preflight，以及确实缺失时才运行的开发依赖准备。摘要显示产品、SDK/API target、参考 policy/build、测试游戏和输出目录；明确区分 reference root 与 live game root。缺少哪一项就报告哪一项，避免开始长构建后才报目录错误。

验收：干净开发目录能按声明输入构建相关项目；缺依赖时在游戏启动/长编译前失败并给准确路径；准备开发依赖不安装到游戏，不修改玩家环境。审查未移动本机 .tools 来模拟空环境，结论来自依赖与入口代码。

### F03 / 优先：Unit 的编译和责任范围需要拆开

证据：Unit Program 约 18,180 行、1.25 MB；csproj 有 17 个直接 ProjectReference、65 个链接 Compile 项，还额外调用多个独立 net48 宿主。执行一个产品 focus 会先处理这整条编译依赖。tests/README 与 Unit README 仍称其为纯 manifest/config 等单元测试。

建议：先按现有边界拆分公共纯逻辑/契约、产品测试、兼容宿主；复用现有测试函数和会话管理，不先迁移整个测试框架。纯规则检查不应为了执行而编译无关 Harmony 产品。focus/default 执行共用同一组案例；提供可列举的实际入口，避免从一万多行 Program 中猜名称。

验收：选定一个产品的构建图不再包含无关产品宿主；默认执行覆盖保留；独立宿主仍在需要时运行。工作与既有 [PN-032](../../../planning/platform-next/tasks.md) 回归实验室方向衔接，不另开平行规划系统。

### F04 / 优先：游戏 runner 已成为过大的组合开关入口

证据：[run-game-smoke](../../../../tools/scripts/run-game-smoke.ps1) 有 12,394 行、170 个顶层参数，混合通用进程操作、特定版本/产品验收、已退役入口拒绝和大量参数组合规则。MoreSaves/装备恢复专用 runner 已有 PlanOnly/ValidateOnly，说明场景边界可以独立表达。

建议：公共部分只负责锁、实际路径、启动/退出、日志和基础保存策略；产品验收放回现有产品 QA/场景模块；历史命令保留薄转发或明确退役提示。普通操作优先直接游戏步骤或少量明确参数，只有选中专项才校验它的复杂前提。不能把 170 个开关原封不动搬到另一个新总入口。

验收：普通场景不加载无关产品的前提；选定场景在启动前列清实际输入/结束条件；已有专项的存档、干净退出和真实行为断言不减弱。仅改变文件拆分不应改变游戏执行事实。

### F05 / 优先：Catalog 的手写投影仍未收拢

证据：[release-common](../../../../tools/scripts/release-common.ps1) 的 Get-DtmApiPublishedModDefinitions 仍手写 OfficialFolder、SourceRoot、DLL、UniqueID、包名等；这些字段同时存在于 [Product Catalog](../../../../tools/release/dtmapi-product-catalog.json)。[发布文案](../../../../tools/release/dtmapi-mod-publish-zh.json) 和产品 README 还维护部分路径、身份和发布叙述。上一轮生成了准入 registry，并未解决全部发布投影。

建议：产品身份/路径从 Catalog 生成或由唯一读取器解析；发布文案保留人工语言内容，用 catalogId 关联。玩家包需要自包含数据时，在打包阶段生成冻结投影，不能要求玩家机器读取开发仓库 Catalog。

验收：改一个身份/路径字段不需手改多张清单，生成检查能指出遗漏；发布物、旧 receipt 与已订阅字节仍按各自权威保留。sourceVersion/publishedVersion、API target/Runtime version 是不同事实，不应强行合并。

### F06 / 优先：玩家支持有成熟单例，但缺少稳定的通用衔接

证据：已有日志收集、PlayerDoctor、保存/崩溃采集器及 [slot0 恢复包](../../../../tools/scripts/build-player-slot0-recovery.ps1)。[近期定点修复](../../../updates/2026/20260905-0001-xingchen-ruined-city-save-repair.md) 复用了已知 native 修复边界，但具体脚本和报告仍放在该次私有 evidence 目录。根当前路由没有专门的玩家支持/存档修复分流。

建议：把已确认且会复用的“只读分析、准入条件、派生修复、允许的差异、重复拒绝、交付验证”抽为可跟踪的小组件；每位玩家只提供 case 参数和私有输入。保留原生规则的来源，不做万能存档编辑器。恢复包推广到其他槽前显式解决槽位/身份契约，不能简单去掉 slot0 的保护。

验收：第二个同类案例无需重写加解密/复制/验证脚本；错误原档、已修复档、玩家继续保存后的新档均正确处理；只采集该问题必要的资料。普通未保存游戏测试仍不做例行备份；真实修复交付继续保护原件。

### F07 / 优先：解包复用需要证明阶段完成和输入一致

证据：底层 capture 的 ReuseExport 路径主要以目录存在决定跳过导出；portable 的 ReuseDecompile 类似。随后重算现有输出清单，但未见在这些跳过分支把输出与先前成功阶段的输入 hash、工具版本和完成状态逐项核对。raw snapshot 与源游戏身份/parity 检查已有，应保留。

风险判断：中断留下的半成品或错配导出目录可能被当成复用对象。这是源码审查发现的判据缺口，本次没有人为损坏现有基线来复现，也不据此否定过去已独立核验的基线。

建议：扩展现有 tool/summary/inventory 回执，使阶段输入、工具、参数、完成状态与输出绑定；完成后再发布阶段结果，未完成只重跑该阶段。复用检查读对应证明，必要时核验相关文件，不在每次查询时重算几十万份文件。

验收：完整且同输入阶段可复用；中断、输入改变、工具改变或篡改输出被拒绝/准确重跑；已知 AssetRipper 限制以明确状态保留，不能把“无错误日志”当成唯一完成标准。

### F08 / 后续：解包应增加按任务选择的差异链

证据：9 份已有 summary 记录的 raw snapshot + AssetRipper export 合计约 51.41 GiB、460,062 个导出文件。这是已有清单中的累计值，不是本次逐盘实测，也不是可删除容量。最近 [1.00.07 比较](../../../updates/2026/20260907-0006-public-10007-full-reverse-capture.md) 中，原始 542 个文件有 522 个相同；导出层却有 45,882 个 changed，主要是 GUID/fileID 重写噪声。

建议：查询已有事实、代码更新分析、完整资源捕获分档；从已有清单筛选真正变化的程序集/配置/资源，输出受影响 native owner/产品建议。语义比较按稳定实体键/引用关系进行，不能把全部 GUID 简单删除而丢失对象连接变化。完整冻结和差异报告共用已有基线身份。

验收：查函数不启动 AssetRipper；不变程序集不重新反编译；完整捕获按需可重现。旧基线是否归档/去重另按引用和保留用途决定，不自动删除，也不把 Steam 活动文件硬链接成“冻结副本”。

### F09 / 后续：当前导航还需准确描述实际能力

证据：tests 的 IntegrationTests 占位被写成测试项目；src README 列出未来占位模块却遗漏实际 SDK/QA/Compatibility/Doctor 等；author-docs README 仍提 testmods 发布路径；当前路由缺完整解包和玩家支持入口。装备 README 的开头混合当前功能、发布状态及多次历史验收。

建议：当前目录 README 只讲实际组件、命令、边界及最近事实入口；planned 项目明确标注。产品 README 保留行为/配置/构建/必要限制，把长验收历史交回 Issue/Update。为支持和解包补当前路由行，不增加全局必读文件。链接历史仍保留，避免批量移动破坏证据。

验收：按五条任务路线能找到真实存在且参数正确的入口；新读者能区分现有、计划和历史。无需为修导航运行游戏。

### F10 / 后续：Issue 和产品文档仍有重复状态维护

证据：Issue README 手写 State 和较长 Current boundary，Issue 文件再维护 State/状态叙述。[ISSUE-028](../../../debug/issues/ISSUE-028-20260824-moreequipment-save-slot-reuse-v3-sidecar.md) 顶部 Current published lifecycle 仍指旧的 1.0.0 发布记录，后文已记录 1.0.1 修正，产品 README 则指向较新的发布 Update。这是当前指针陈旧，不是判定 Issue 应关闭的理由。

建议：Issue 文件拥有状态及当前未解决条件，索引自动投影 ID/短标题/State；历史发布证据标为历史，当前发布统一路由 Catalog/subscription 的 owner。复用现有 ledger 工具模式，不新增第二套进度台账。

验收：状态变更维护一处；索引不会复制根因全文；实现 verified、Issue mitigated、玩家未确认可以正确并存。

### F11 / 后续：临时输出需要统一新写入规则，不宜全面搬迁

证据：根目录同时存在 .artifacts、.tmp、artifacts、tmp、temp、output、outputs、dist、reports；temp 有 491 个直接子项，tmp 有 152 个。旧 first-party-mods 仍残留 AutoFishingMod/ZoomMod 目录，但已无当前 csproj。目录数只证明布局分散，不证明全部内容无用。

建议：新的一次性操作统一进入受控 tmp 会话；可交付包归 dist；需要长期解释问题的私有证据归既有 evidence owner；工具缓存归 .tools，官方冻结研究归 references。已有脚本的 temp 约束逐个迁移并留兼容路径，不一次移动所有历史产物。对需要清理的目录先确定用途、活跃性、保留引用和边界。

验收：普通成功运行只留下所需交付物/紧凑结果；失败可定位自己的输出；下一次运行不覆盖仍被引用的证据。不新增每次任务全盘清扫步骤。

### F12 / 后续：源码文本断言与本地测试调度值得整理

证据：Unit Program 有 127 处 File.ReadAllText，包含针对 PowerShell 内部变量、分支表达式、完整提示语的 Contains 断言；例如外部输入/退出回归附近直接匹配具体 runner 语句。全文的 1,168 个 Contains 并不等于 1,168 个冗余测试，其中也有正常结果断言，不能据此批量删除。仓库未发现 GitHub/GitLab/Azure/Jenkins 的跟踪 CI 入口；外部自动化是否存在未调查。

建议：把重要行为改为执行真实小函数/受控假件的输入输出检查；语法/引用边界使用结构化解析；确实属于 ABI、来源禁区、固定历史字节的检查保留。通过现有 PN-032 渐进建立短的源码/契约检查和按边界运行的完整套件；真实游戏检查独立调度。

验收：内部改名或格式调整不会无故击穿行为验收；真正破坏来源、保存/恢复或失败传播仍失败。基础环境可复现之后再接 CI，不让每次文字修改排队完整游戏/发布流程。

## 5. 建议实施顺序

| 顺序 | 工作包 | 主要收益与退出条件 |
| --- | --- | --- |
| 1 | F01 产品 pack/SDK 复用 + F02 按目标 preflight + F09 导航小修 | 先消除每次都会发生的重复准备和晚期目录错误；相关包/错误输入与命令路线验证通过即结束 |
| 2 | F05 Catalog 投影 + F10 Issue 索引 | 减少同事实多处手改；生成物一致，历史/发布事实不被改写 |
| 3 | F03 Unit 编译边界 + F04 runner 场景边界 | 从一个产品开始迁移，证明构建依赖缩小、原覆盖保留，再扩到下一产品 |
| 4 | F06 支持修复组件 + F07/F08 解包阶段与差异链 | 以一个同类支持案例、一个已有基线/中断假件验证复用；不以真实玩家数据制造故障 |
| 持续 | F11 输出归属 + F12 检查质量/调度 | 随触及的工具整理；不建立新的全仓强制清扫或反复检查任务 |

这是建议顺序，不修改 platform-next 的任务状态、架构复盘节点或上传授权。必要的专项门继续由现有权威规定，不以本审查新增日常打卡项。

## 6. 明确保留的内容

- PROJECT 的原生保存提交语义、共享运行环境锁和实际退出证据。
- 玩家修复的原件/身份保护，与普通 NoNativeSave 的免例行备份分开。
- SDK 的引用准入、包/receipt 绑定，以及正式确定性双打包。
- 旧 DLL/包的真实兼容检查；QA、独立 Harmony 宿主与真实 Unity 行为的区别。
- Catalog、subscription manifest、API target catalog、历史 policy 的不同事实边界。
- 已有 Review/Issue/Update 和冻结逆向基线的审计用途。
- 约两秒的文档治理检查，不将扫描条目数当作工作成果。

## 7. 本次审查的验证范围

已核对 Git/目录清单、主要 README/路由、五条工作流入口、PowerShell 参数/直接脚本调用、SDK pack 的实际编译调用、选定项目依赖、代表性玩家修复及最新解包记录；逆向体量读取已有 summary，未重算全量资产哈希。

未运行游戏、重新解包、修复玩家数据、构建新的 Runtime/SDK 包或全量测试。除本审查记录外不修改实施文件。收尾仅检查本文件链接、格式和文档治理。后续实施时验证对应改变，不把本次静态结论当运行验收。

