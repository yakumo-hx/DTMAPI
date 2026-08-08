# 20260710-0002 AutoFishing 逐帧输入、GC 与 Ready 动画审查

## 手测记录头

- 时间：2026-07-10，Asia/Shanghai。
- 来源：用户在第五存档完成蓄力、热键捕获与重置手测后的追加反馈；当前 `latest.log`、DTMAPI/SMAPI/Generic Mod Config Menu 源码与当前游戏 `Assembly-CSharp.dll` 只读反编译研究。
- 范围：先记录根因，再实施 `docs/goals/2026/20260710-0005-input-frame-gc-ready-animation.md`；不修改第三方小神增强包、SMAPI 或 Generic Mod Config Menu 文件。
- 禁止事项：不把普通 Mod 热键写进游戏原生 ActionMap/官方按键存储；不让 250ms TimerFallback 分发普通 Mod Update；不复制 SMAPI/GMCM 实现；不改变 0/0.5/1 蓄力目标；不在 F6/F7 关闭时卸载共享 Harmony owner。
- 审查记录：本文件是可跨上下文恢复的 durable manual-QA/root-cause 记录。上一轮事实见 `20260710-0001-autofishing-input-charge-polish-review.md`。

## 逐条手测审查

### 问题 1：逐帧输入驱动与短按边沿

原始反馈：

- 用户确认 F6/F7 连续快速短按仍不如原生 B 键丝滑，并同意先修逐帧输入驱动和边沿 latch。
- 图片转写：本轮无新截图。

审查记录：

- 用户确认事实：长按或重按通常可触发；快速短按可能丢失；原生 B 键打开背包明显更及时。
- 截图/日志观察：2026-07-10 手测进程先记录 `Unity frame callback ... Coroutine`，随后记录 fallback pump 通过 Unity `SynchronizationContext` 派发；对象图持续为 `updateCallbackSeen=False`。F6/F7 press/release 基本落在 250ms 网格上。
- 代码/文档事实：`BootstrapPlugin` 的 fallback timer 周期为 250ms；InputSystem backend 读取 `isPressed`、`wasPressedThisFrame`、`wasReleasedThisFrame` 后立即返回，不与 Win32 transition latch 合并。当前游戏原生 B 对应 `ToggleBackpack` InputAction，使用 `Tap` interaction，并由 `DolocUserInput.GlobalToggleBackpack -> InputAction.triggered` 在同一输入帧消费。
- Codex 推断：根因不是 AutoFishing `OnKeybindPressed` 慢，而是正常 Unity `Update`/Coroutine 驱动在启动后失活，Gameplay 输入长期由 4Hz fallback 采样。短按若完全发生在两次采样之间，InputSystem 的帧边沿已经清除；长按能依靠 `isPressed` 被下一次采样发现。
- 反证/未证实：尚未证明 Bootstrap MonoBehaviour/Coroutine 失活的最终 Unity/BepInEx 原因；不能仅靠延长按键或把 Win32 放到第一优先级掩盖主时钟故障。
- 归属：DTMAPI.BepInExBootstrap 输入时钟与边沿采集；DTMAPI.Core 共享输入帧/owner scope。
- 需要更新：本轮 goal、ISSUE-010、smoke matrix、hook map、API matrix、update record。
- 验收点：Gameplay 中 F6 和 F7 各连续 10 次约 40ms 短按逐次触发；长按只触发一次；输入诊断证明逐帧主线程驱动持续存活、fallback 不承担普通输入；标题空闲不采样 Gameplay 热键。
- blocker 判定：若边沿已经由逐帧后端捕获但没有在同一/下一安全帧分发，保持未完成；若新的驱动跨线程或重复分发普通 Update，也保持未完成。

### 问题 2：配置审计无界增长与 AutoFishing 热路径分配

原始反馈：

- 用户同意消除配置审计无界增长及 AutoFishing 热路径字符串。
- 图片转写：本轮无新截图。

审查记录：

- 用户确认事实：本轮在标题设置页反复调整 AutoFishing 选项，并询问当前自动钓鱼是否仍有 GC/累计日志压力。
- 截图/日志观察：本次约 27 分钟 `latest.log` 为 2,591,272 bytes、2,157 行，其中 526 行含 AutoFishing、616 行为 Hook status。`ConfigPreview` 从 290 增到 370 约用 72 秒；AutoFishing owner 一度有 285 条 ledger record。关闭/返回标题后 fishing owner options/states、primitive sessions/leases、native transients、animators 和 hook physics 均回到 0。
- 代码/文档事实：配置页每次重绘对全部选项 apply pending/restore，各写一条成功审计；每条审计追加到无上限 `ModOwnerLedgerService.entries`，随后 `entries.ToArray()` 并执行多组 LINQ/字符串汇总和六组状态发布。AutoFishing 每个 Update 格式化 phase 和未发生移动时的 movement reason；Fast+正蓄力每个 `AgentStateFishingReady.OnPlay` 新建 `List<string>` 并在节流判断前构造完整摘要。Bootstrap InputSystem 反射 getter 还会装箱三个 bool。
- Codex 推断：此前 local snapshot/registered-key Core 集合分配已修复，但端到端并非零分配。配置预览 ledger 是真实 retained-growth 和放大型 Gen0 路径；AutoFishing session 本身没有短测泄漏，剩余主要是热路径临时字符串/集合/反射装箱和诊断 I/O。
- 反证/未证实：短测资源回零不能关闭 ISSUE-010；当前仍无 100/500-loop 与完整 Bootstrap/Mono allocation profile。
- 归属：DTMAPI.ModConfigMenu、DTMAPI.Core diagnostics/owner ledger、Bootstrap input adapter、第一方 AutoFishing、GameBridge fishing diagnostics。
- 需要更新：本轮 goal、ISSUE-010、smoke matrix、update record。
- 验收点：配置页反复重绘/滑动后 owner ledger 条目数保持有界，成功 preview 只累计计数而不保留逐项历史；失败详情有界保留；稳定页面重绘不再按条复制全 ledger。AutoFishing warmed idle/active frames不格式化状态字符串，Fast Ready 每 tick 不创建诊断 List/摘要；日志只在转换、失败或到期汇总时产生。
- blocker 判定：若为追求零分配而删除失败诊断、owner cleanup 证据或真实配置预览语义，保持未完成；若 ledger 仍随每次重绘线性增长，保持未完成。

### 问题 3：Ready 后摆与蓄力计时加入 animation lease

原始反馈：

- 用户确认 Fast 开启时后摆动画加速可能不稳定或没有加速，并要求蓄力动画一并加入动画倍速范围；随后同意最后把 Ready 动画正式加入 animation lease。
- 图片转写：本轮无新截图。

审查记录：

- 用户确认事实：0/0.5/1 实际抛距正确；当前需要 Fast 同时加速后摆、蓄力、抛竿/鱼钩飞行和收竿。
- 截图/日志观察：实机日志能证明 FastReadyCharge timer、Cast hook physics 与 Pull，但不能证明第一方 Ready animator 已加速。
- 代码/文档事实：当前第一方 `FishingAnimationLeaseRequest` 只有 CastHook/Pull；primitive request 分支对 `Ready` 明确返回 false。原生 `AgentStateFishingReady.OnEnter` 同时播放 body 和 rod 的 `fishing_ready`；`OnPlay` 只在 `_isAnimationDone` 后推进 `_castTimer`；`NextState` 在输入释放且动画完成后进入 Cast。
- Codex 推断：当前第一方后摆实际上没有纳入倍率；玩家感觉到的快慢来自 Ready 后的 timer、hook flight 与 Pull。正确实现是 animation lease 显式携带 Ready multiplier，Ready enter 对 body/rod animator 做 scoped snapshot/write/restore，timer 继续按同倍率推进，蓄力 target 保持独立。
- 反证/未证实：自动 smoke 只能证明倍率/恢复和目标不漂移，最终视觉仍需玩家手测；不能用多 hold 一帧伪造后摆。
- 归属：内部 first-party fishing primitive contract、GameBridge Ready adapter/Hook lifecycle、第一方 AutoFishing 配置文案。
- 需要更新：本轮 goal、hook map、API matrix、smoke matrix、update record。
- 验收点：Fast=off 保持原生 Ready；Fast=2/3/4 时 body 与 rod 后摆、后摆后的蓄力计时、hook flight、Pull 同倍率缩短；charge 0/0.5/1 距离不变；Ready exit、disable、save/title、异常都恢复原速并清零 lease/native snapshots。
- blocker 判定：若只能通过直接写结果 power、跳过 `_isAnimationDone` 或保留全局 animator speed 实现，保持未完成。

## 问题分组

- Input/Runtime：问题 1。
- Config/Diagnostics/GC：问题 2。
- Hook/GameBridge/AutoFishing：问题 3。
- 测试/证据：三项都要求 source/unit 与第五存档真实运行证据；视觉 Ready 仍需玩家门。

## 边界约束

- 必须做：一份逐帧共享输入源、边沿 latch、scope/owner cleanup；有界 config preview 诊断；AutoFishing 热路径延迟格式化；Ready/charge/cast/pull 同一 Fast 倍率且 charge target 独立。
- 禁止做：原生 ActionMap 注入普通 Mod 热键；TimerFallback 普通 Update；每 keybind 独立平台状态；成功 preview 无界历史；直接写鱼竿结果 power；共享 Harmony `UnpatchSelf`。
- 可选做：若 typed Unity InputSystem 编译引用风险过高，可先用一次性生成的强类型 getter delegate；不得保留每帧 `PropertyInfo.GetValue` 装箱作为最终路径。
- blocker 判定：无真实第五存档短按/循环/清理证据时不得写 runtime complete；无玩家 Ready 视觉反馈时只能写 smoke/source verified。

## 实施中新增事实：首次逐帧烟测的同帧源抢占

- 保留失败样本：`GAME-SMOKE/20260710-171352`。它通过 `AutoFishingInputLog`、`AutoFishingHotkey`、`AutoFishingAnimationSpeed`、`AutoFishingCastCharge`、进程退出和无 fatal 窗口，但完整 phase/minigame/soak 因烟测移动把 `HorizontalMoveFactor` 置为 `-1` 后触发产品的原生移动取消而失败。
- 输入诊断揭示的独立实现缺陷：日志证明成功订阅 `InputSystem.onAfterUpdate`，但 22.8 秒内只分发 2 个输入帧；同一期间 GameBridge Coroutine 已分发约 2150 次 Update。初版仲裁允许 Coroutine 先写 `lastProcessedUnityFrame`，随后 InputSystem 回调在 latch 前以“同帧重复”返回；InputSystem 活跃标记又会压住后续 Coroutine，形成两个正确局部判断组合出的停更。
- 修正约束：一旦 InputSystem driver 订阅成功，它就是唯一普通帧源；Coroutine/Update 不能抢占 frame id。InputSystem 每次 after-update 必须先合并边沿，再对 Mod Update 做同帧去重。若该 driver 连续 2 秒无回调，Timer 只能在主线程执行健康检查、解除失活订阅并让 Unity callback 临时恢复，不能自己派发普通 Mod Update。
- 该失败样本同时证明 Ready/Cast 速度与 0.5 target 已生效、退出后 `readyChargeStates=0`、`animators=0`、`hookPhysics=0`；它不是完整循环或短按通过证据。
- 保留第二个失败样本：`GAME-SMOKE/20260710-172128`。修掉同帧抢占后，InputSystem 活跃窗口在 6.5 秒累计 2575 个输入帧、12873 个按钮样本、`latchedFrames=2574`，证明采集/排水不再停在 4Hz；但场景加载后 InputSystem callback 再次停止，旧 `SynchronizationContext` 也不再执行定时健康检查，三次外部 F6 均发生在无普通 Mod Update 的窗口。
- 第二次根因收紧：InputSystem after-update 适合作为边沿采集点，但不能独自承担 DTMAPI 生命周期主时钟；启动时捕获的 Unity `SynchronizationContext` 也不是跨场景恢复点。最终路线改为把 DTMAPI 排水回调安装进当前 Unity `PlayerLoop.Update`，InputSystem 只负责提前 latch；PlayerLoop 自身也直接采样一次作为边沿后备。Coroutine/MonoBehaviour Update/Timer 都不再与已安装 PlayerLoop 抢占普通帧。
- 保留第三个失败样本：`GAME-SMOKE/20260710-172756`。日志证明 PlayerLoop 节点安装成功、成为首个普通帧源，并在 6.5 秒内分发 1867 个输入帧；但原生 `LoadGame` 重建了当前 PlayerLoop，DTMAPI 的本地 `installed=true` 标记仍在而实际节点已被替换，SaveLoaded 后三次外部 F6 仍无排水。
- 第三次根因收紧：PlayerLoop 是正确的逐帧归属点，但安装不是一次性 process-lifetime 操作；它必须按原生 lifecycle 重建。现有 `DolocAPI.LoadGame` Postfix 已是确定的 native-return 边界，因此 Core 增加内部通知，Bootstrap 在 `SaveLoaded`、`LoadGameReturned` 和 `ReturnedToTitle` 重新检查/替换自己的单一 PlayerLoop 节点。该通知不扩展公共 API，也不增加 Harmony target。
- 保留第四个失败样本：`GAME-SMOKE/20260710-173218`。它在 `SaveLoaded`/`LoadGameReturned` 重新调用 `SetPlayerLoop` 并记录 `installed=True`，但在当前原生加载调用栈内改写 PlayerLoop 后，后续并没有新的 DTMAPI PlayerLoop callback。结论是“生命周期边界重装成功返回”不能当作“新循环已实际运行”的证明。
- 第四次根因收紧：逐帧 Gameplay drain 改为钩住本体稳定责任函数 `DolocTown.NormalGameState.OnUpdate(float)` 的 Postfix；Core 只提供内部 `NativeGameFrame` 通知，Bootstrap 仍统一拥有 input latch、Core/UI/event dispatch。PlayerLoop 保留为标题/非 NormalGameState 后备，不能在原生 Gameplay drain 活跃时重复采样或重复分发。
- 当前运行证据：`GAME-SMOKE/20260710-173812` 记录 `GameLoop.NativeFrameDrain = experimental`，存档内 6.8 秒完成 2394 个 Gameplay 输入帧（约 350.6 fps）、11968 个已注册按钮样本，`nativeGameFrameCallbackSeen=True`，进程退出与 fatal-window 检查通过。该轮三次 `SentExternalAutoFishingToggleAttempt*Failed` 都是在烟测脚本未能把按键发送给游戏窗口时失败，InputSystem/Win32 计数也为零，因此不能把它写成产品 F6 失败或短按通过证据。
- 去重结论：第五轮同时看到 native drain 与保留的 PlayerLoop latch，导致约双倍 `latchedFrames`。最终实现让 Native Gameplay drain 活跃后 PlayerLoop 仅保留健康计数、立即返回，不再重复采样；标题仍由 PlayerLoop 驱动。40ms 连续 F6/F7 的玩家视觉/手感门仍需复测。

## 2026-07-10 18:46 玩家复测失败：一次 F6 被分发两次

### 问题 1：表面上整个 AutoFishing 未启用

原始反馈：

- “手测失败了，感觉整个mod没有启用。无自动钓鱼功能。蓄力硬编码调整无效，所有小功能无效。”
- 图片转写：本轮无截图。

审查记录：

- 直接日志反证“Mod 未加载”：`Yuuka.DTMAPI.AutoFishing` 已完成 Entry，F6 注册存在，`Input F6 pressed`、产品 `Keybind ... pressed`、primitive session acquire、Ready body/rod `1->3`、charge target 和 Ready timer Hook 全部执行。
- 真正失败序列：`18:46:01.738` 第一次 F6 press 开启产品并取得 session；`18:46:01.755` 在没有 release 的情况下第二次发布同一个 F6 press，产品立即执行 `SetAutomation(false)`；真实 release 到 `18:46:01.889` 才发布。开启窗口只有约 17ms，因此玩家观察完全等同于“没有启用”，所有子选项也来不及产生可见效果。
- 根因：新的多源 frame driver 允许 InputSystem/Native/PlayerLoop 在相邻 drain 中重复读取同一个仍为 `wasPressedThisFrame=true` 的物理周期。Latch 在第一次 Core consumption 后清掉 pending edge，但保留 `IsDownNow=true`；第二次采集仍无条件接受 backend `PressedEdge`，于是同一 held cycle 被重新入队。最终 PlayerLoop 站下只能减少一个来源，不能证明 InputSystem 与 native drain 不会重采同一边沿。
- 产品层回归：旧 AutoFishing 曾有“按下后必须等 release 才接受下一次 toggle”的防御；第一方 primitive cutover 后该 guard 没有保留，使平台重复边沿直接变成双切换。
- 修正约束：平台每个 Unity frame 只允许一次 registered-button latch；同一已按下周期里，若没有观察到 release，不得把重复 backend pressed flag 当作新物理按下。若一个样本同时含 release+press（快速重按并最终保持 down），仍应接受新 press。AutoFishing 同时恢复 release guard 作为产品安全带，但平台修复必须覆盖其他 Mod。
- 验收点：单测模拟 `press(down) -> consume -> duplicate press(down, no release)`，第二次必须无 press；随后 `release -> press` 必须正常。第五存档一次 F6 只能出现一次产品 toggle，保持开启并进入 native loop；F6 release 后下一次 press 才允许关闭。

修复与复测：

- `ReflectedUnityInput` 现在用 Unity frame token 拒绝同帧第二采集源；Latch 还会在已 down 且没有 release 的周期内抑制重复 backend pressed flag。一个样本若同时包含 release+press，仍可表示真实快速重按。诊断新增 `duplicateLatchFrames` 与 `suppressedRepeatedPressedEdges`。
- 第一方 AutoFishing 恢复产品级 release guard：接受一次 toggle press 后必须先收到匹配的 `KeybindReleased`，才允许下一次切换；若平台再次重复 press，只记录一次警告并忽略。
- 单测新增 `InputNativeEdgeLatchRejectsDuplicateSourcesAndHeldPress`，覆盖同帧双源、跨帧 held-repeat、release 和下一次合法 press。Release 构建 0 警告/0 错误，`DTMAPI.UnitTests: OK`。
- 第五存档通过样本 `GAME-SMOKE/20260710-185323`：玩家手动 F6 后，DTMAPI 只在 `18:54:12.678` 发布一次 press，`18:54:12.751` 发布 release；AutoFishing 保持开启，完成 3 次真实可见小游戏/收竿并进入第 4 次抛竿，直到玩家第二次 F6 才关闭。结果 `RunStatus`、输入、热键、phase、小游戏完成、soak、报告、fatal-window、进程退出全部 Passed。诊断显示每帧第二来源被计入 `duplicateLatchFrames` 而没有变成第二 press；本样本不证明 smoke sender。
- 组合功能部分样本 `GAME-SMOKE/20260710-185537`：单次 F6 保持开启；target=1、Ready/Cast animator `1->3`、Ready timer、Cast hook velocity/gravity 均通过。第五存档烟测站位下满距离越过水面，17 次 Cast 都在 Wait 前 Interrupted，因此 Instant/Skip 无法进入；这是夹具/落点限制，不是启用回归。
- 组合功能行为通过样本 `GAME-SMOKE/20260710-185857`：外部 sender 结果门失败，所以总 `RunStatus=Failed`；但游戏内 typed F6 后只开启一次，并记录 `AutoFishingLoop OK`，`instantBite=True`、`skip=True`、Fast Ready/Cast、两次 native skip reel、PullExit 和 next auto-cast。保留为 behavior-pass / external-sender-gate-fail 证据，不写成完整烟测通过。

## 2026-07-10 最终玩家复测与证据来源纠正

### 问题 1：当前功能手测结果

原始反馈：

- “手测全通过。自动钓鱼功能有，单独测配置有，蓄力也 ok。”
- 图片转写：本轮无截图。

审查记录：

- 玩家确认当前安装版能正常启用自动钓鱼；各独立设置可观察到效果；蓄力调整有效。结合“全通过”表述，本 goal 的短按/启用、独立功能、组合功能、Ready/Fast 视觉与蓄力 0..1 玩家门记为通过。
- 自动证据仍保持分层：`185323` 证明玩家按键之后的平台分发、产品保持开启、完整 DefaultLoop、清理与退出；`185537`/`185857` 分别补充满蓄力/Fast 与 Instant/Skip 运行路径。玩家确认才是最终视觉/手感结论。
- ISSUE-010 不因此关闭：100/500-loop 和任意长时 Gameplay/原生 GC 路线仍未完成。

### 问题 2：`GAME-SMOKE/20260710-185323` 的 F6 来源

原始反馈：

- “smoke 测试 F6 其实没输入，是我手动点的。”

审查记录：

- 更正：`185323` 中打开和关闭 AutoFishing 的 F6 都应归因于玩家在烟测运行期间手动按键，不能写成烟测脚本/外部 sender 成功注入。
- 保留的证明范围：该样本仍真实经过 DTMAPI InputSystem/native drain、KeybindPressed/Released、第一方产品、primitive session 和完整钓鱼循环，因此可作为“manual-assisted runtime pass”；它不能证明 `Send-DolocTownNamedKey`、窗口聚焦或自动 40ms 注入可靠。
- `result.json` 的 `AutoFishingInputLog=Passed` 只说明日志门观察到 F6，不表达输入来源。未来记录必须同时查看 `summary.txt` sender 字段和玩家说明，不能从 result field 反推为外部自动注入。
