# QA 拆分为何多次重新打开

当前普通产品测试归 [产品验证流程](../../workflows/product-change-validation.md)，Runtime 故障状态归对应 Issue，版本和安装权限归 [PROJECT](../../../PROJECT.md)。以下是 Batch 4 的历史结构与失败，G0–G9 是当时一次架构迁移，不能成为每个 Mod 小修的必跑矩阵。

拆分前 Smoke 18 文件、约 1.4 万行；构造器和每帧 Update 都找设置，缺少 smoke-settings.json 时仍每帧 File.Exists。这是已证实的无消费者成本，不能据此宣称找到原生 GC 崩溃原因。四个性能探针、DTO、序列化和 AutoFishing 正目标编排必须一起迁移，否则会留下生产→QA 依赖，或只移动字段而仍不生成正目标性能结果。原 100/500 参数实际未接探针的缺口应由合成正目标测试直接发现。

G7 删除旧文件名和调度器后，25 文件、12,733 行场景正文仍在生产 QaHost/Fixtures，optional QA 仅 3,227 行。五个玩家 DLL、不引用 QA、包内没有 QA.dll 都通过，并未证明场景逻辑离开玩家程序集。源码审查撤回的是完成推论，既有行为与恢复 PASS 仍有效。G8 才移动 case ID、场景状态、断言、截图/文件/退出/强制 GC 策略，移除公共 Core fixture 控制；后来的 G9 又发现被 allowlist 承认的证据策略与孤儿变更入口，故单纯名称、行数或全文件 hash 同样不足。

权限方向是 optional QA 依赖窄内部生产操作；生产不解释测试 profile，不掌握预期结果，也不引入公共测试 API。保留真正原生 UI 修复、装备孤儿回收、owner 清理，不能凭 ForSmoke 后缀删除。无 QA 场景应验证这些实际玩家路径；较早无 QA PASS 不能覆盖之后改动的启动和 UI 路径。但也不需要因为只改记录就重做一次无 QA 游戏。

激活是一次性、显式、runId/版本/hash 绑定；加载经过验证的同一字节；一个 participant、一个 writer。必需回执是 validated→activated→attached→started→updated→closed，并非可能被同 HookId 合并的普通日志。close 失败不能记成功，也不能吞退出请求。可重试原生 unpatch 使用该 run 自己的 Harmony owner，禁止全局 unpatch；先成功清理再 closed。部分注册失败、双 Camera lease 与双 owner 关闭、结果文件写失败应由 Unit 故障注入测试。

典型失败前提与运行器问题保留：构建 stdout 混入 PowerShell 返回对象，导致 stage.Artifacts 不存在；计数器文件路径与游戏截图根混淆，启动前就失败；调用已返回 false 的 load 请求却先消费 disposition；原生 mod-change 弹窗少 continuation；标题通知重复使 participant 提前 close；队列刷新后的旧状态被拿来判断真实输入顺序；owner 隔离 profile 未加载目标产品；AdvancedDebug 未等 CurrentRoom/native spawn host 就先变更。它们都能在短预检、假收据或明确 ready gate 层拦截，无需等待 420 秒或反复重放全产品矩阵。

真实状态应从观察开始计时：请求 ReturnHome 不等于已连续处于 HomePage，accepted load 不等于 SaveLoaded。外部 OS 输入、人工输入、synthetic 输入分开报告。Camera 测试遇 room clamp 导致距离零时，修正 QA fixture 与 settle 条件，不能为了测得预期移动去更改产品 Camera 契约。历史回放的某个 inner PASS 也不能抹掉同一 run 的 outer animation、close 或退出失败。

一次全套 Release 当时耗时约 9–12 分钟，其中包含 SDK 离线/便携打包、双 PowerShell 安装事务、Doctor、ABI 等。曾经这些全部通过，只在最后遇 evidence allowlist、路由大小或 Update/月表状态不同步，又把完整 Release 重跑。当前设计应复用已完成且输入未变的工程证据，只重跑失败检查；文档治理本身几秒与被它牵连的全套执行成本要分开统计。测试入口应输出各层耗时与原因，不能只报总绿灯或数千检查数。

Batch 2/3 还说明玩家 Doctor 和 Author SDK 不是同一承诺：SDK 已完成不等于 Runtime 加载失败时玩家有离线只读诊断。补齐后 Doctor 以 .NET 8 工具在玩家工具目录单次启动、metadata-only、不加载未知 DLL，游戏的五程序集仍 netstandard2.0。作者 watcher/session 也不能进入普通玩家每帧路径。这里的旧 milestone admission 顺序只解释当时暂停，不是今天新的审批要求。

G9 进一步把 YConsole、AnimalViewer、EquipmentSlots 的自动截图/summary/specimen 策略移到 QA；无 QA 测试必须实际打开这三个 UI 才能发现残留，而只有普通 hotbar 输入发现不了。最终 gate 用符号、真实消费者、禁用行为与负例约束所有生产根及相关公开产品源码；四个 IL 工件只覆盖 Runtime/QA 边界，不宣称扫描全部产品 DLL。动物关闭以 native Unregister 为 owner，先 native close，再释放自有 clone/derived key，再 session-cleared。

7 月 18 日独立再验收提供了正确复用范例：未改变源码，保留原约十五分钟完整 Release 证据，仅做改变的语义/文档与最终 commit 包来源校验。相同二进制仅更新包 provenance，也不要求重新进游戏。相反同 FileVersion 而不同 hash 不能当相同安装工件。临时候选包通过不构成后续源码版本的发布许可。

CameraPlayable 曾在提交 stage 之前获取两个 lease 并同步调用 Runtime，回入相同 stage，两秒内出现 585 次交替获取并真实崩溃。根因足够明确：进入阶段先提交，再做可能重入的副作用，每个 handle 只持有一次；Unit 同步重入即证明只获取两次。无需先做 dump 符号分析，也不应修改生产 CameraView 去去重各不相同的租约以掩盖 QA 缺陷。修复后一个有界真实 Zoom/ground-safe/cleanup 验收即可针对该问题，不能并入广泛 GC 根因。
