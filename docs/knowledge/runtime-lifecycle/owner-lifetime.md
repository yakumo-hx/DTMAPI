# Runtime owner 生命周期的历史演进

当前规则归 [PROJECT](../../../PROJECT.md)和 [Mod Owner Lifetime Contract](../../design/mod-owner-lifetime-contract.md)；本页合并历史演进与失败教训，不复制一份契约。旧记录中的版本号、安装目录和 PASS 均只代表当时样本。

2026-05-31 的增量加载已用 UniqueID 防止官方刷新重复执行 Entry，但停用已加载 DLL 时主要表现为配置页锁定、提示重启。这是后续 owner 事务和统一撤销之前的历史能力，不能把“当时不卸载 DLL”解释为允许停用后继续保留全部活动根。

当前契约的关键区分是 Core owner 状态与成功加载 Mono assembly 这一进程事实。Entry 及其注册属于同一事务，发布失败也走撤销；成功 Assembly.LoadFrom 后，即便已知根归零仍需重启，不能再次 Entry。没有成功加载 assembly 的来源，只有全部参与者证实零根、零清理失败，才允许同进程重新发布。关闭、停用、来源切换、必需依赖失效共享幂等清理，保存/回标题只清瞬态，不删除仍活跃 Mod 的进程级注册。

平台 provider 与普通来源有不同的发现边界：平台以 canonical registry row 为准，普通来源还需当前 discovered/enabled source；磁盘更新版本不能冒充进程中已加载版本。清理失败须保留有界剩余根或 tombstone 以便重试，不能假报零，也不能重入 Mod。诊断只是观察者，不能因日志 sink 失败阻断权威发布和撤销；计数也不能来自另一份不断增长的诊断图。

2026-05-30 的 [ISSUE-002](../../debug/issues/ISSUE-002-hookprobe-blocks-hotkeys.md)提供了另一种“Mod 已加载但快捷键无响应”的已知解释：HookProbe 在 SaveLoaded 自动打开多页 DTMAPI UI，IsOpen 因而阻挡普通热键。该记录为 mitigated，并明确真实人工热键证据未补齐。测试助手需要显式加入；不能把正常游戏留有 HookProbe 后的输入失效直接归因于 Hotkey 实现。

7 月初 phase 2–6 是逐层落地的历史：先只观察 lifecycle，再按 process/content/save generation 记录资源，再把 Hook 安装与事件发布移回 runtime thread，随后补 owner/Entry 事务。资源代际只在真正内容签名变化或存档边界变化时前进；标题检查不应反复加载同一音频或动物定义，borrowed native controller/asset 不因诊断归零就可销毁。

Hook readiness 分成 core、feature、诊断/QA，避免缺一个 optional Hook 就让全局 Timer 和 AssemblyLoad 订阅一直活着。Timer/AssemblyLoad 请求工作，实际 Harmony 安装由 runtime thread 执行；可排队的生命周期/状态事件与必须拒绝的 off-thread Update/Input 分开。阶段开关只是当时迁移手段，不能把旧关闭开关当现行受支持模式。

初期事务只包 Entry，config preview 仍调用真实 setter 再恢复，quarantine 先移出热路径；这些当时保留的风险由后来当前 owner 契约继续收紧。账本记录成功注册历史并非最终设计，后续已改有界计数和权威根状态。不能因一次 phase smoke 的 activeTransactions=0 就推导所有发布失败点已事务化。

phase 7 的 EveryFrame/250ms/LifecycleOnly 桶曾是调度方案，7 月 28 日已经因生产路径改为 demand routing 而退休；旧 bucket 配置只作迁移输入。历史健康收据保留其当时代码的事实，不能用来要求当前 Runtime 重新暴露一个已无调用者的 scheduler。

7 月 4 日全局生命周期审查补上真实缺口：标题 UI reset 通过既有 config transaction 取消 pending edits，销毁自己从本地素材创建的 icon Sprite/Texture；DebugConsole 在 shutdown 清自建 root/modal/binder；GameBridge 清 Timer、AssemblyLoad、自有 SaveLoaded listener 与 static Runtime/Bridge 引用。这些具体对象有 DTMAPI 所有权，不能推广为对原生 controller、bundle 或音频对象一律 Destroy。

7 月 8 日资源账本交叉审查分清两种成本：AutoFishing 每条鱼按 native 对象身份产生记录，release 后仍保留在当前 save generation，后续快照随之增大；音频和动物服务的 ID 稳定，却在未变化、甚至跳过刷新时仍每帧建立完整快照和摘要。前者需要聚合高流量已释放记录，后者需要未变化就不发布、不建快照。固定上限与没有日志输出都不能证明热路径没有分配；诊断应直接记录 publish、skip、snapshotBuilds、当前及已释放数量。该源码观察本身不证明原生 GC 崩溃根因。

7 月 11 日通用 owner 审查逐个给出可在 Unit 中复现的失败轴：Entry 返回后、发布中或最终日志失败仍需清 loadedMods/modInstances；停用要撤掉平台根；ConfigPage 和 migration 不能借另一个 manifest 冒名注册；同 owner/contract API 不能默默替换；quarantine 不能为诊断继续持有捕获委托；LogOnce 和诊断 key 必须有界。历史实现 owner 是 Update 20260711-0010，现行行为回到上方契约。不能重复开启一轮原生方法调查来解决这些已知 Core 自有状态，也不能通过回标题清空全部服务逃避 owner 撤销。

该实现的 owner 清理证据有效，但没有覆盖“已活跃普通 Mod 依赖仅存在于 registry 的平台 provider，再做无变化刷新”。随后用户反馈配置页几乎全部消失；截图只显示一个配置页，29 discovered、6 loaded 降到 3 与具体 owner 身份来自日志。根因是把平台 provider 强行要求出现在普通来源发现表，清理本身反而正常。这个已验证回归的状态归 [ISSUE-013](../../debug/issues/ISSUE-013-20260712-owner-platform-dependency-reconciliation.md)，不要误指 Camera 租约释放为起因。

该次人工反馈审查的编号保留为：1 平台依赖误停用；2 同 UniqueID 来源切换却残留旧程序集和根；3 SaveLoaded 未清 Core 瞬态输入；4 日志 sink 中断权威 reconciliation；5 平台 manifest/API 非原子发布且动态 ID 未保留；6 未成功加载程序集的 owner 也被永久要求重启；7 quarantine overflow 被算成无法按 owner 清理的当前根；8 config preview aggregate 重复留存任意 detail。后续实现和反例在原 Update；本页只合并共同知识。真实程序集加载是重启边界，历史 trimmed 不是当前根，日志和诊断不能支配正确性。

紧接着的用户三项审查依次为：1 磁盘 manifest 1.0→2.0 冒充旧进程已加载 2.0；2 行数有界但字符串字节无界；3 干净 Release 暴露 CS8602，原零警告表述需修正。最终诊断在构造 key、DTO 和外来 snapshot 投影之前裁剪字段，固定 hash 区分截断文本，报告截断量，且总摘要也有预算。整数行数上限不能替代字节和外部对象图边界；历史有界值属于其代码版本，修改集中 sanitizer 后应检查当前实现。

[ISSUE-016](../../debug/issues/ISSUE-016-20260731-audio-hook-idempotent-status-republish.md)当前保留 mitigated：用户四功能手测通过，之后 Animal Bell 首次名称查询触发全局 Hook review；音频四个物理 guard 已避免重复安装，却再次把 verified 写回 experimental，字符串启发式将其当重复安装。最小修复只在初次或物理 tuple 真变化时发布，并保留 unavailable→installed 重试。两条 Unit 反例通过，玩家相同延迟需求序列仍缺补测；无完整 Release、GC 或无关功能矩阵要求。全局 scheduler 范围和结构化安装状态的后续设计归[音频路线图](../../archive/planning/2026/20260731-audio-replacement-bridge-roadmap.md)，不借本页实现第二套状态机。

7 月 31 日四切片按 C1、重复 SaveLoaded 假诊断删除、F1 相对驱动健康、G1 Lite 原顺序保留于[当前查询/生命周期路线图](../../planning/20260731-runtime-query-lifecycle-driver-logging-roadmap.md)。所有驱动一起停是全局暂停，只有 sibling 前进而 InputSystem 独停才恢复；Timer 只观察，不分发 Mod Update。Lite 减连续对象图，不能隐藏 Warning/Error、根异常、inner stack 与因果上下文。实施 Update 的 runtime 仍 not-run，不将之前用户 AutoFishing PASS 转为修正后二进制 PASS。F3 删除 InputSystem 驱动角色、G2 真日志等级和官方最终表查询仍按各自 owner 处理。
