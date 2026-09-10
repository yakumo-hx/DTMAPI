# 测试运行器、证据与成本的历史教训

日常测试以 [产品修改验证流程](../../workflows/product-change-validation.md)为准；本页归并旧执行失败和信息增量，旧命令与阈值是历史证据。

2026-05-31 连续两轮启动监测分别增加 12 和 20 个正常样本，都没有捕获用户描述的 30 秒异常。样本中的 Bootstrap.Awake 约 0.58–0.64 秒，不能据此优化 Runtime 来解释全部启动等待，也不能凭更多正常样本否定间歇异常。应区分发起启动到进程出现、进程到启动日志、Runtime 内部分段，以及启动前阻塞；下一步需要真正异常窗口的证据。两份纯证据文档后来再次 build，只能视作当时成本，不能推广为每次记录更新的要求。

20260608-0004 的机械 Hook 文件拆分先 build 后 test，两者都重新 Release build；首次 smoke 又因没有游戏路径而在证据创建前失败，补 DTMAPI_GAME_DIR 才运行。配置和路径缺失应在启动前一次报明；保留这一失败，不把后续成功误写为首次执行即成功。机械搬文件能保留 namespace、类型、Hook ID 和日志文本；是否进游戏应由实际变更风险决定，不继承旧的每次完整路线。

20260607-0006 的 1,700 个 public symbol 清单来自生成；0007 明确只用它导航，再人工读方法体，且拒绝生成 1,013 行低风险属性模板来增加所谓覆盖。索引数量、字段完整性与代码行为审查是不同证据。旧审查末尾的“另建 goal 文件”是当时流程，不是当前任务的自动工作指令。

20260608-0001 将构建产物、游戏二进制、解包及第三方材料移出 Git 索引而保留本地原件；验证的是索引边界和文件仍存在，没有 build 或游戏 smoke。当前材料边界仍由 references/README.md 管理；本次知识提取没有复制本体或第三方实现。

20260610-0001 曾并行运行 build/test，因共用 obj 引发瞬态文件锁，再改成顺序重跑；这属于运行器资源冲突，不能归入游戏生命周期 bug。共享输出的构建不能用并行制造所谓提速。该轮还手动修正了五个 latest-report 指针，说明报告位置应由每次运行的确定输出统一提供，避免维护者追赶全局 latest。

20260610-0002 的审计包生成暴露 PowerShell 文本插值问题：哈希表对象名直接出现在文档，反引号还把 testmods/tools/bin 一类路径变成控制字符。生成器改用明确属性及格式化字符串、先 staging 后自检。full/web 有不同证据体积目标，不能把有意省略的 zip 当内容缺失；首次打包源码构建因 120 秒期限不足重跑，不是源码编译错误。文档包验证可以检查生成格式、范围和可构建性，不能自动触发游戏。

0019 又因并行 build/test 抢同一 UnitTests.dll 而重跑，说明它是重复出现的流程缺陷。0031/0032/0038、0044/0048 的 latest-report 仍指向上次报告；原记录正确地排除旧 zip，只引用本轮日志、结果和退出检查。后续 0053 要求独立 AutoFishingReportExport 字段，避免小游戏成功与证据导出成功混为一个结果。没有新报告并不使已收集的行为证据失效，但不能把全局 latest 伪装成本轮产物。

6 月 13 日鱼卵首轮 smoke 的 20 秒退出提前结束用例，后来热键五分钟计数也被同一默认退出抢断；这是启动前可判定的计划矛盾。6 月 15 日日志采集已改轻量默认、显式 IncludeRuntimeEvidence 才递归复制；7 月 live dump 达 4.9 GB 时日常包只留元数据说明。当前采集边界由专用工具负责，不能把这些历史“完整证据”逐层套用。

ISSUE-010 保留的长测工具失败包括：把 PowerShell 只读 PID 用作变量、nullable/null stdout/stderr 处理失败、子 PowerShell 缺 Get-FileHash 导致已抓到 dump 却没有 result/自动还原、Steam 不传入新环境变量、按错存档 fixture、外部按键未到窗口、在 owner 注册前执行隔离。每项都应在启动长等待前用短预检或合成输入覆盖；原生故障观察可保留，未完成的结果/还原则明确标缺失，不伪造全流程 PASS。

旧 fatal 检查在杀进程后才看窗口，曾把真实 Fatal 写成 PASS；后来改活进程采集。Unity crash 目录还必须按本轮基线区分 fresh/stale-only/missing，不能用上次 dump 的堆栈解释这次失败。DbgHelp 得到 dump 也不等于已分析：当时没有 debugger，只核对大小/hash，TopStackSummary 仍为 missing-debugger。

原记录中 repeated F6 曾先被解释为测试 sender 噪声，用户复测证明其实有产品重复边沿；之后另一条 PASS 的两次 F6 又被用户纠正为人工按下。保存观察来源及撤回过的归因。typed consumer 需要真正三状态 input frame；只注入旧 RecordInputPressed 不能证明 KeybindPressed。

6 月 collector 安全修正的上限计算针对“考虑过的文件”，不是只数成功复制，否则大量过大文件仍能无限遍历。两个 Temp 路径各保留最新 crash；新无 dump 样本不能被旧大 dump 挤掉，接受 dump 后也要留小文本预算。假树、空间/中文路径、桌面不可写和 game path 未解析都已用无游戏测试覆盖。日志头尾共同保留早期加载失败，MOD-RUNTIME-DIAGNOSTIC 与安装失败分开。

7 月 phase 2/4 的纸箱自动交互失败已有同一用户的人工行为确认，生命周期层可复用该行为证据，把 sender/fixture 单独留给运行器修复。阶段诊断 status=ok 与游戏 Fatal 可同时成立；只有健康字段通过就关闭总问题，会丢失真正退出与原生窗口事实。

7 月 7 日标题输入计数的五分钟与两分钟场景，子计数已 PASS，却因默认强制 SaveLoad gate 让外层失败；测试计划应按场景声明 gate，不能标题观察自动附带一次读写存档。50 个虚拟键与原 9 键有重合，实际 union 为 57；20 分钟复现后明确停止小时阶梯。7 月 10 日连续输入修复又保留了 InputSystem callback、PlayerLoop 被原生加载替换、栈内重新安装仍不运行等失败，2394 帧/6.8 秒只证明调度节奏，不证明外部按键送达。

可见收杆确认以 native NextState 真正转换为准；Unit 在确认后推进假时钟 750 ms，验证只扣一次能量、没有重发边沿，比等待实机概率触发重复更直接。线程分配计数不可用只阻塞该子探针，不能让完整内存趋势场景中止或把进程 Gen0 证据改 null。实际运行 gate 与探针能力各自报告，既不误报零分配，也不让一个平台缺失计数器吞掉其他有效证据。

owner 验收还发生过三类纯运行器误判：合成 owner 在预读档回标题时被提前结束；无程序集合成 owner 被错误归为重启；字符串 failedSteps=none 被当作失败。7 月 12 日 45 个请求 gate 已通过的测试又因未请求 Zoom owner exercise 却强制 Zoom cleanup health 而总失败。最小回归应测试请求、未请求、缺失通用 snapshot、通用 snapshot 不健康四个逻辑分支，不能让可选 skip 绕过通用健康检查。计划语义可以先用假收据验证，之后实机只补真实边界。

7 月 19 日首个正式 600 秒 ActionSpeed 阶段已完成真实 workload、保存已加载且恢复/退出全过，外层却没有 ladder 专用 SaveLoaded 分支，初始化 false 保持到最后。父进程再从人读错误文本抓 Evidence:，得到空白后 GetFullPath 异常覆盖主失败。应在结果落盘后始终输出机器可读 evidence marker，先校验非空、存在和范围，独立保留 child exit 与证据发现错误；不要靠人读 stdout 决定收据身份。那次重跑规定保留为历史，不把已知投影错误泛化为以后不可离线完成有效证据。

统一 deadline 不能只包最外层。旧 UI 尾部仍做十次 taps、固定四/五秒等待、A/E 间 sleep，甚至 Max(1, remaining) 延长已过期预算。每次输入、等待和迭代前都检查同一个绝对期限，完成最后行为/清理后再记录 completed-before-deadline；过期入口的假时钟测试应保证零输入。活跃多阶段测试冻结 runner 输入，不能中途改脚本制造混合版本；这与日常修改后只重跑受影响检查不冲突。

## 前提互斥和结束门必须属于所选场景

Fishing mechanical split 三次带 HookProbe 超时，原因是其配置页 reset 把 InstantBite 改回 false；FishRoe tooltip 首败来自 disabled provider。产品缺席时临时 fallback 可以验证 native decoration，但不能补成真实产品能力。纸箱六次 E 窗口未触 native 后，玩家同进程 OnInteract/played/suppressed 已接受，机器 result 仍红只代表 sender 缺口。已知前提错误不会因延长 timeout、换启动方式或槽位而成为新证据。

7 月长闲置首轮尚未跨 ModChange 确认到 SaveLoaded，不能算 GC 重现；另两次 Fatal 用上一轮累计 SaveLoaded>0 把最新请求误标 closed。ledger 应绑定 active/latest 请求，nativeEnter>nativeReturn 和新请求无 SaveLoaded 不能被历史成功覆盖。title-only Doctor/输入计数不应索取一次 SaveLoad；健康计数为零和未请求业务必须分别解释。

结果 schema 区分 Passed/Failed/Skipped/Blocked；未请求不等于失败。历史 `ForcedClose=Passed` 表示没有强退检查通过，不能读成实际已强退。外层 completed 应在必要退出/清理完成后记录；看到会话结束后的 lock free 不能倒填到更早回执。进程还活着时不得恢复它使用的文件；普通游戏无保存不因此要求复制 archive。

来源：[历史 smoke 原卷](../../archive/regressions/2026/smoke-matrix-history-through-20260711.md)、[首阶段真实/脚本证据分层](../../archive/updates/2026/20260702-0001-first-stage-refactor-scaffold.md)、[请求边界对象图](../../archive/reviews/code/2026/20260704-0001-issue010-title-return-next-load-object-graph-audit.md)、[保存输入与 completed 时序](../../archive/reviews/code/2026/20260806-0001-dtmapi-060-ninth-five-slice-parallel-review.md)。

## 复制历史证据才曾是大额成本

7 月容量审查约 495 GiB GAME-SMOKE 中，456.5 GiB 来自反复复制历史 runtime evidence。三轮明确清理分别约 50.5、150.7、248.5 GiB；数字是当时不同集合，不是本次释放量。当前机制按运行开始以来的直接 case/timestamp 选取，整批预检数量/总量/单文件上限，保留唯一 canonical identity 及原始 run/明确工件。外层协调 receipt 可归既有 GAME-SMOKE，不为每项验收再造 durable root/schema。

旧 retention 的 62 identity 先报 194、后报 182 唯一文件，原件口径和原缺失集合保留；仅凭数字差不能推断丢失。Catalog 也可能持有唯一正式证据引用，文档去重不能切断其保留关系。当前范围查[证据保留协议](../../debug/protocols/evidence-retention.md)，本页不授予额外删除权。

Unit 曾反复采集用户历史 crash，形成 6,104 ZIP/9,779 DLL；进程 session、独占 lease、仅进程级 TEMP/TMP、成功清理/失败紧凑 receipt 解决测试产物归属。它不是每次游戏行为隔离存档的理由。普通测试不抓 full dump；需要时先非空、长度/hash 完成正式交接，再清自己的临时源，不能用 Git 回退记录假装恢复 ignored 数据。相关机制更改才跑 fixture，普通产品测试不为 cleanup 再跑一轮。

来源：[容量审查](../../archive/reviews/code/2026/20260712-0002-smoke-evidence-retention-boundary.md)、[有界采集实施](../../archive/updates/2026/20260712-0005-smoke-evidence-retention-bounds.md)、[产物与 dump 治理](../../archive/updates/2026/20260717-0001-test-artifact-and-dump-governance.md)。

## 停止和重试以未证明事项为界

7 月 21 日已接受“失败焦点→未到达尾段诊断→冻结候选一次完整验证”；局部与尾段不能拼成正式完整 PASS，也不要求每修一处立即重跑全套。仅 provenance、文档链接或判据解释改变时，保留未变 raw/stage，追加更正或重评；不改写旧收据。原件提出的隔离矩阵若被后继用户/游戏版本证据消解，应停止，不能把提案当必须偿还的测试债。

保存专项中的每进程一次 Enter 没见 SaveSaving，不应固定五秒再敲到可能已变的 UI；缺事件保留 non-acceptance，下一独立运行再试。这里确实验 native save，普通 Hook/UI 不继承三进程。MoreSaves Entry 迁移例外见[存档槽知识](../persistence/save-slot-migration.md)，长期 GC 与无效计数器见[测量边界](measurement-and-test-gates.md)，不将它们再抄成 runner 默认门。

来源：[重试比例](../../archive/updates/2026/20260721-0002-full-suite-retry-proportionality.md)、[大茶壶末尾关闭](../../archive/reviews/manual-qa/2026/20260715-0003-flourishing-flora-large-teapot-zero-time.md)、[第九组保存证据](../../archive/reviews/code/2026/20260806-0001-dtmapi-060-ninth-five-slice-parallel-review.md)。
