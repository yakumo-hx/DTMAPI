# Y 键控制台文本输入占用 Y 回归审查

## Review Header

- Time: `2026-08-23 +08:00`
- Status: `recorded`
- Source: 用户在当前本地游戏上的两次复现、所附 Steam 评论截图、两次运行的 `DTMAPI/logs/latest.log` 与 `BepInEx/LogOutput.log`。
- Scope: 诊断并界定两个已复现输入问题：Y 键控制台自身搜索框聚焦时按 Y 关闭，以及游戏原生箱子改名框聚焦时按 Y 打开控制台。
- User constraints: 当前完整反编译基线已经更新到实际运行游戏版本；文本输入正在编辑中时 Y 必须留给输入，不得打开或关闭控制台；修复可重构或精简，但不能增加过多性能负担。
- Related review/update/debug records:
  - [ISSUE-014: Y Console Close Double Toggle](../../../../debug/issues/ISSUE-014-20260712-y-console-close-double-toggle.md)
  - [DebugConsole Input Isolation Hook Map](../../../../hook-map/focused/DebugConsoleInput.md)
  - [Y Console Close Double Toggle Review](20260712-0002-y-console-close-double-toggle.md)
  - [DebugConsole Twelfth Advanced Product Update](../../../updates/2026/20260726-0005-debugconsole-twelfth-advanced-product.md)
  - [Y Console 1.1.0 Roadmap Review](20260812-0001-y-console-roadmap-native-owner-review.md)
  - [Public 1.00.05 Full Reverse Capture](../../../updates/2026/20260823-0002-public-10005-full-reverse-capture.md)
- Files/docs inspected:
  - `products/first-party/DebugConsole/src/ModEntry.cs`
  - `products/first-party/DebugConsole/src/Ui/DebugConsoleUi.cs`
  - `src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs`
  - Core 输入采样、帧记录、事件派发与现有 DebugConsole 单元/游戏测试记录
  - 当前 `InputNameUiState`、`RenamingUiState`、`InputNameBox`、`RenamingBox` 与 `DolocInputFiledComponent` 反编译代码
  - `references/doloc-town/reverse/builds/24650773_public_76C24E`
  - `references/doloc-town/reverse/builds/24788406_public_F06183`
  - 本次实际运行日志和已加载产品清单
- Not inspected: Steam 评论者各自的完整日志、存档和复现步骤；任何未由当前用户实际复现的崩溃路径。

## Issue Review

### Issue 1: 控制台搜索输入框聚焦时按 Y 仍触发关闭

Original feedback:

- “疑似新版本出现的问题。按照设计首先在Y键控制台输入界面就不应该会关闭。检查具体发生了什么问题。”
- 用户确认本地完整反编译版本已更新为当前运行游戏版本，并在刚才的实际测试中复现：Y 键控制台搜索界面按 Y 会关闭控制台。

Screenshot/log transcription:

- 截图中的 Steam 评论一称“搜索栏按y键游戏直接崩溃”。当前用户只复现了控制台关闭，没有复现或确认进程崩溃。
- 截图中的 Steam 评论二称，在游戏箱子改名时只要按到 Y 就会弹出控制台。该报告涉及游戏原生输入框，不等同于本次已复现的控制台自身搜索框问题。
- 当前运行于 `2026-08-23 01:58:07.503` 打开控制台，日志保留 `searchText=涂装`，证明搜索状态正在使用。
- `01:58:08.391` 同一时间戳依次记录：`Input Y pressed ... OwnerModal`、`debug-console.toggle pressed`、`Debug console close boundary reason=hotkey Y`。`01:58:12.129` 再次出现相同顺序。
- 本次日志没有出现焦点保护应写出的 `Debug console left Y with the focused text input instead of toggling the console.`。
- 本次进程没有相应 fatal/exception，退出后没有遗留 `DolocTown.exe`；当前证据支持“关闭回归”，不支持把本次复现写成“崩溃”。

Review record:

- User-confirmed facts:
  - 实际运行游戏为当前公开 `1.00.05` / Steam build `24788406`。
  - 用户在 Y 键控制台自己的搜索输入框中复现了按 Y 后控制台关闭。
  - 设计和现有 ISSUE-014 验收条件都要求：文本输入聚焦时 Y 不得切换控制台。
- Screenshot/log observations:
  - 运行时为 DTMAPI Bootstrap `0.6.1.0`；加载的本地 `DTMAPI.DebugConsole.dll` 是发布的 `1.1.0`，长度 `220160`，SHA-256 `DBC0540A0187BB9B620AAE504BBF1F5569C23796686939A085B4328FF39B3BE3`。
  - 产品来源为本地启用副本，当前安装 build `24788406`；产品编译基线 `24456188`，因此 Loader 正确报告 `gameCompatibility=Drift`，不是误载 Workshop 旧副本。
  - ProductNative 安装了预期全部 `19` 个补丁；输入 Prefix 缺失不是当前失败层。
  - 开启边缘保护仍工作：较早日志写出“ignored opener Y close until the opening key press is released”。失败只发生在后续聚焦输入时的独立 Y 边缘。
- Code/doc facts inspected:
  - `DebugConsoleUi.Update()` 会读取 raw Y、调用 `IsAnyTextInputFocused()`，命中时调用 `runtime.Input.Suppress("Y")`；焦点检测同时检查受管 `InputField.isFocused` 和 EventSystem 当前选中对象。
  - `ModEntry.OnKeybindPressed()` 收到 `debug-console.toggle` 后立即调用 `ui.Toggle(...)`，本身没有先查询文本焦点。
  - 当前 Bootstrap 每帧先调用 `SampleDtmInputFrame()`，该路径同步记录并派发 typed `KeybindPressed`；之后才调用 `runtime.Update()`，而产品的 `OnUpdateTicked -> ui.Update()` 位于这个后段。
  - 因而真实顺序是：`Sample Y -> Core 派发 typed keybind -> ModEntry.Toggle 关闭 -> runtime.Update -> UI 焦点检查`。后段的 `Suppress("Y")` 无法追溯撤销已经派发并完成的切换。
  - 2026-07-27 ProductNative 迁移前，Bootstrap 直接托管的 DebugConsole UI 更新位于 `SampleDtmInputFrame()` 之前；迁移后普通产品 UI 改由较晚的 `UpdateTicked` 驱动。焦点保护代码被保留，但它依赖的“派发前执行”位置没有随所有权迁移一起保留。
  - `24650773` 与 `24788406` 的 `Unity.InputSystem.dll`、`UnityEngine.InputLegacyModule.dll`、`UnityEngine.UI.dll`、`UnityPlayer.dll` 以及相关 `GameLoop`、`AgentControllerState`、`DolocInputSource`、输入框组件反编译内容均未出现与本症状相关的变化。`DolocUserInput` 的变化只是增加一个 Confirm 查询属性。
- Codex inference:
  - 这是 2026-07-27 DebugConsole ProductNative 迁移留下的帧顺序回归，1.1.0 的真实聚焦输入操作现在把它暴露出来；现有证据不支持归因于游戏 `1.00.05` 更新。
  - 日志中保存的 `searchText=涂装` 与用户即时复现共同支持“输入框确实处于使用场景”；根因不需要假设焦点检测 API 在新版本失效，因为切换事件已在焦点检测获得执行机会之前完成。
- Ownership:
  - 普通 DebugConsole 的 UI、typed Y 注册和关闭动作仍由 ProductNative `DTMAPI.DebugConsoleMod` 所有。
  - 当前只证明产品自身的派发顺序有缺口，不构成把普通 Mod hotkey 或输入框焦点提升为 SharedNative/GameBridge 权威的依据。
- Root-cause hypotheses:
  - Confirmed: 产品 UI 的 raw-Y 焦点抑制运行在 Core typed keybind 派发之后，保护时点过晚。
  - Repair boundary for a later implementation: 应在 typed toggle 执行前由产品同步作出“当前文本输入是否占用 Y”的决定；若确需平台前置采样接口，必须另行证明产品内同步门无法满足所有者边界。
- Rejected/unproven hypotheses:
  - Rejected: 当前游戏版本替换了相关 Unity 输入/UI 模块或改变了相关原生输入框路径。
  - Rejected: 三个 ProductNative 输入 Prefix 未安装或产品 DLL/来源错误。
  - Rejected: 焦点保护代码被删除；它存在且有隔离单元测试，只是运行得太晚。
  - Unproven: Steam 评论所称进程“崩溃”；本次日志只证明控制台关闭，进程干净退出。
  - Confirmed later in Issue 2: 原生箱子改名输入框中按 Y 会打开控制台；它与控制台搜索框问题共享“typed toggle 执行前没有统一文本输入占用门”的缺口。
- Required downstream updates:
  - 将 ISSUE-014 从 `verified` 改为 `regressed`，保留此前普通 Y 开关、Escape drain 和 opener-edge 验证的历史效力，只撤销“聚焦文本输入已受保护”的当前结论。
  - 更新 DebugConsole 输入 Hook map，明确补丁拓扑仍完整，而 typed 产品派发前置焦点门当前失效。
  - 用户后续授权研究并修复两个场景；创建一个有界实现 Update，链接本 Review 与 ISSUE-014。
  - 不向 smoke matrix 添加本次行：这是玩家手测与日志证据，不是仓库运行器产生的 `GAME-SMOKE` 接受包。
- Acceptance checks:
  1. 增加真实顺序测试：控制台打开、owner modal 活跃且搜索输入聚焦时，在 Bootstrap/Core 采样 Y 后不得调用 toggle，UI 保持打开，Y 可进入文本字段。
  2. 同一测试证明未聚焦时一个物理 Y 边缘仍只关闭一次；opener hold、快速释放再按、Escape 两个干净帧和按钮关闭不回归。
  3. 游戏内实际聚焦搜索框输入含 `y/Y` 的文本，日志必须出现焦点占用证据且不得出现相同边缘的 `debug-console.toggle pressed`/hotkey close。
  4. 游戏内箱子改名框聚焦时输入含 `y/Y` 的中英文名称，字符和候选选择保持正常，控制台不得打开；离开文本输入后 Y 仍能正常打开控制台。
  5. 完整启动、第三存档、无 fatal、标题清理和无遗留进程证据通过后，才可再次把 ISSUE-014 标为 verified。
- Blocker conditions:
  - 当前没有技术阻塞；实施仍不得把单元或源码检查写成玩家验收。
  - 若后续同步焦点门在 typed 回调时无法读取可靠焦点，停止叠加 raw 轮询补丁，先记录真实 Unity/EventSystem 时序，再评审是否需要最小的产品所有者前置输入阶段。

### Issue 2: 游戏原生箱子改名输入框中按 Y 打开控制台

Original feedback:

- “刚才运行游戏并复现‘箱子改名时弹出控制台’。能输入和选择有关中英文，但也确实会打开Y键控制台。”
- 用户要求检查日志，研究两个场景的共同修复；允许重构或精简，但不接受明显的持续性能负担。无日志的崩溃报告暂不处理。

Screenshot/log transcription:

- 新运行的 `latest.log` 长度为 `154932`，最后写入时间 `2026-08-23T02:28:47.1024603+08:00`，SHA-256 为 `179F82BF9036E562C9B7B3F4BB51E24B90D2D1416B60D5C80A06EF95E82A2`。
- 在控制台关闭期间，日志多次记录 `Input Y pressed ... audience=Normal owner=broadcast`，紧接 `debug-console.toggle pressed` 和 `Debug console opened ... reason=hotkey Y`；例如 `02:28:35.838`。这与用户在原生改名框中的即时复现相符。
- 整次运行的 `debug-console-y-input-focus` 和“left Y with the focused text input”计数均为零；DTMAPI 没有在派发前识别原生文本输入占用。
- 该进程随后正常返回并退出，没有与此操作对应的 fatal/exception；无日志的崩溃报告不纳入本实现。

Review record:

- User-confirmed facts:
  - 箱子改名框可以正常输入并选择中英文内容，说明原生输入控件仍在工作。
  - 在该输入状态按物理 Y 会同时打开 DebugConsole。
- Code/doc facts inspected:
  - 当前游戏的 `RenamingBox` 继承 `InputNameBox`；后者持有 `DolocInputFiledComponent`，而该组件继承标准 `UnityEngine.UI.InputField`。
  - `InputNameBox.OnFinishShow()` 清空 EventSystem 选择后调用 `inputField.Select()`；编辑结束则选择确认按钮。因此 `EventSystem.current.currentSelectedGameObject` 上的 `InputField.isFocused` 是与实际文本编辑同生命周期的窄判据。
  - DebugConsole 的 typed keybind 仅声明 `DtmInputScope.SaveLoaded`。Core 当前把原生改名 UI 下的 Y 视为普通 `Gameplay` 广播；产品 `OnKeybindPressed` 不检查任何 Unity 文本焦点，因而直接打开控制台。
- Codex inference:
  - Issue 1 和 Issue 2 不需要两套修补：都应在 `ModEntry.OnKeybindPressed` 调用 `ui.Toggle` 之前同步查询“任意受支持文本输入是否真正聚焦”。
  - 判定应兼容标准 `UnityEngine.UI.InputField` 和 `TMPro.TMP_InputField`，同时保留对控制台自身已跟踪输入框的直接检查；不绑定 `RenamingUiState` 名称，也不添加游戏方法 Hook。
- Ownership:
  - 这是 DebugConsole 自己是否响应 Y 的产品策略，仍属于 ProductNative。当前证据不要求改变所有 Mod 的平台级 hotkey 语义或公开 API。
- Performance boundary:
  - 只在 DebugConsole 收到 Y pressed 事件、以及控制台打开时已经存在的 raw-Y 边缘检查上查询一次；控制台关闭时不恢复 `UpdateTicked` 常驻订阅。
  - Unity Type、PropertyInfo 和 `GameObject.GetComponent(Type)` MethodInfo 只解析一次并缓存；每次 Y 最多检查当前选中对象的两个输入组件，不使用 `FindObjectsOfType`、全场景扫描、逐帧反射、Expression 编译或新 Harmony Hook。
- Acceptance checks:
  1. 源码/单元证明 typed toggle 在 `ui.Toggle` 前调用统一焦点门，聚焦时返回且不改变 UI 状态。
  2. 标准 InputField、TMP_InputField、控制台自有输入框均使用缓存元数据；无焦点或选中普通按钮时不误吞 Y。
  3. 游戏中分别验证控制台搜索框和箱子改名框；Y 留给输入，控制台状态不变。
  4. 离开文本输入后，普通 Y 开/关、开启键释放保护、Escape drain、按钮关闭与标题清理继续通过。
- Blocker conditions:
  - 如果当前选中对象不是实际输入组件，或者 `isFocused` 在 typed 回调时已失真，不能扩大为全场景组件扫描；应先捕获 EventSystem 选中对象和原生 UI 状态证据。

## Cross-Issue Summary

- Confirmed user facts: 当前游戏和反编译基线一致；控制台搜索框中按 Y 会关闭，原生箱子改名框中按 Y 会打开控制台；两处中英文输入本身均可工作。
- Screenshot/log facts: 两次运行分别确认 owner-modal typed Y 关闭和 normal-broadcast typed Y 打开；没有确认截图所称崩溃。
- Code-path findings: ProductNative 迁移后，控制台自身焦点抑制晚于 typed 派发；控制台关闭时又没有任何原生 InputField 焦点门。两个问题可在产品 typed toggle 前统一解决。
- Risks: 若只在后段继续增加 suppress/debounce，会保留同一根因并可能破坏普通 Y、快速重按或旧 ABI 路径。
- Suggested implementation scope: 在产品 typed toggle 边界增加一次按键边缘触发、缓存反射元数据的通用 InputField/TMP_InputField 焦点门；复用既有控制台焦点判定，保留 opener/Escape/legacy 所有权，并分别验收两种输入框。
- Items that should not be carried forward: “1.00.05 改坏 Unity 输入”“当前运行发生崩溃”“需要逐帧扫描所有输入组件”“输入 Prefix 未安装”。

## Implementation Record Decision

- Create/update an implementation update record: yes；用户已授权研究并修复两个已复现文本输入场景，由 `20260823-0003` 跟踪实现与验证。
- Additional debug/API/hook/smoke records required: yes；更新 ISSUE-014 与 focused Hook map。公共 API 未变化，不更新 API 矩阵；没有运行器游戏验收，不更新 smoke matrix。
- Suggested task title: `Y 键控制台聚焦文本输入的派发前抑制修复`。
- Completion standard: 产品内派发前门、集成顺序回归测试、聚焦搜索框实际游戏证据、普通 Y/Escape/opener 回归和干净生命周期全部通过；独立确认后 ISSUE-014 才恢复 `verified`。

## Manual Acceptance Outcome

- Time: `2026-08-23 +08:00`
- Exact candidate: local DebugConsole `1.1.1`, entry DLL SHA-256 `784FD83E3174F80773926DE937171B1294D8F3E8D21240762492EC5E93B0E050`.
- User-confirmed facts, preserving the reported order:
  1. 在控制台搜索栏输入 Y，控制台保持打开。
  2. 在箱子改名输入框中输入 Y，不会触发 Y 键控制台。
  3. 离开输入框后，Y 能正常打开和关闭控制台。
- Runtime log observation: the final `latest.log` is `155868` bytes, last written at `2026-08-23T07:02:10.7486247+08:00`, SHA-256 `F1B3B72A8BCBCF1F786E8E7B716294649FA45BCE8816E484DC70D5CF0380AF32`. At `07:01:54.893`, Core dispatched an owner-modal typed Y and the product logged `Debug console left Y with the focused text input instead of toggling the console.` without a close boundary. Later unfocused Y edges at `07:02:05.477`, `07:02:06.362` and `07:02:10.013` closed, reopened and closed the console normally; the reopened lifecycle retained `searchText=伊萨多`.
- Acceptance result: both focused-input defects and the ordinary unfocused toggle regression gate are user verified. This closes the Review's acceptance conditions; implementation lifecycle and durable issue state are owned by Update `20260823-0003` and ISSUE-014.
- The screenshot-only crash claim remains unverified and outside this result because no supporting crash log or dump was supplied.
