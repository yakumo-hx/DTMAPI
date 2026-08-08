# 20260712-0002 Y Console Close Double Toggle Review

## Manual QA Record Header

- Date: 2026-07-12
- Status: `recorded`
- Source: user manual QA after the typed hotkey/input rebuild
- Scope: Y-key console close path only; preserve console features, Escape/button close, public Input APIs, and unrelated Mod hotkeys
- Related records: `docs/updates/2026/20260707-0004-hotkey-rebuild.md`, `docs/updates/2026/20260710-0005-input-frame-gc-ready-animation.md`, `docs/debug/issues/ISSUE-007-20260605-mine-yconsole-026.md`, `docs/debug/issues/ISSUE-014-20260712-y-console-close-double-toggle.md`

## 问题 1：Y 打开正常，但 Y 关闭连续触发两次

原始反馈：

- “重做热键系统后，Y键控制台打开是正常的，关闭按Y会连续触发两次以至于无法关闭，只能点击关闭按钮或其他方式关闭。”
- 图片转写：无截图。

审查记录：

- 用户确认事实：一次 Y 可以打开控制台；控制台打开后再按 Y 会发生两次切换，最终仍保持打开；关闭按钮或其他关闭路径可用。
- 代码事实：`DebugConsoleMod` 在热键重做后注册 owner-bound `debug-console.toggle=Y`，其 `KeybindPressed` 处理器调用 `IDebugConsoleApi.Toggle`。
- 代码事实：Bootstrap `ReflectedDebugConsoleUi.Update` 仍保留 2026-06-05 旧 UI host 的原生 `GetKeyDown("Y") -> Close(..., "Y")` 路径。Bootstrap 同一更新链先运行 UI host，再采样/派发 DTMAPI typed input。
- 代码事实：现有 `suppressYCloseUntilReleased` 只保护打开控制台的同一物理 Y 周期；文本输入焦点门也只阻止输入框中的 Y 关闭。它们没有解决一次新的关闭边沿被 UI host 和普通 Mod keybind 两个 owner 同时处理。
- 代码事实：`DebugConsoleSmoke` 的 Y-close 断言直接调用 `debugConsoleApi.Close(owner, "Y")`；十次短按循环在关闭分支也直接调用 `Close`，因此没有验证真实 `debug-console.toggle` 关闭路径。
- Git 历史事实：UI host 的直接 Y 关闭来自 typed keybind 重做之前；热键重做迁移普通 Mod 后没有移除该旧消费 owner。
- Codex 推断：用户观察到的“连续触发两次”与 `UI host Close -> owner-bound keybind Toggle/Open` 的双所有者序列一致。无需假设全局 Input edge 又产生了两个 `KeybindPressed`；当前证据首先指向已确认存在的两条消费路径。
- 已排除方向：不对全局 Y 或全部 keybind 增加时间防抖；不吞掉正常 release/下一次 press；不移除文本框焦点保护；不改变 Escape、按钮关闭或公开 Input API。
- 归属：Y toggle 归普通 `DTMAPI.DebugConsoleMod` owner-bound keybind；Bootstrap UI host 只拥有 UI 焦点、开场同一物理周期保护和原生 Escape fallback。
- 验收点：关闭状态按 Y 只打开一次；打开状态按 Y 只关闭一次且保持关闭；按住开场 Y 不闪烁；输入框聚焦时 Y 不关闭；Escape/关闭按钮仍可关闭；快速释放后下一次 Y 仍有效。
- blocker 判定：自动 smoke 可证明 typed open/close/短按/hold 状态机，但外部物理按键注入不稳定，最终玩家可见关闭仍需用户复测确认。

## Resolution Link

Implementation and validation are owned by `docs/updates/2026/20260712-0006-y-console-close-edge-owner.md`.

## 2026-07-12 玩家复测

- 用户原始反馈：“没问题，手测通过。Y键点按开关正常。且短按、连续短按开关都识别都比较稳定。”
- 图片转写：无截图。
- 用户确认事实：固定版中 Y 点按可以正常切换控制台；单次短按与连续短按均能稳定识别；此前“关闭后被第二次触发重新打开”的玩家可见症状未再出现。
- 验收结论：问题 1 的物理按键玩家门通过；该结论与自动 smoke 分开记录，不将自动输入替代为玩家证据。

## 2026-07-15 Batch 2 外部输入门禁复审

- 新证据：`GAME-SMOKE/20260715-133713` 使用普通 Steam 启动、无 HookProbe、前台窗口 `SendInput`。Y1 打开和 Escape 关闭均成功；Y2 在 `13:37:58.738` 打开后，同一次旧 Escape 的原始 UI 边沿又于 `13:37:58.754` 关闭控制台。随后 Y3 第一次实际重新打开，第二次才以 `reason=hotkey Y` 关闭。
- 代码事实：`DebugConsoleMod` 已注册 `debug-console.close=Escape` 为 `DtmInputScope.SaveLoaded`，而 Core 已明确允许这类 owner 在自己的 in-save DTMAPI modal UI 打开时收到关闭键。Bootstrap UI host 仍同时执行 `ReflectedUnityInput.GetKeyDown("Escape") -> Close`，因此 Escape 仍有两个消费 owner。
- 代码事实：Bootstrap 每帧先更新 title settings UI，再更新 debug console，最后采样 typed input。`ReflectedTitleMenuSettingsUi.ResetBoundaryState("title hidden")` 当前无条件调用共享 `runtime.UI.Close()`；因此 Y console 调用 `OpenCustomMenu` 后，下一帧会被不拥有该 menu 的 title host 清掉共享 modal 状态。失败日志随后持续显示 `menuOpen=False`，这会让 Gameplay scope 和普通 Mod 更新在控制台可见时错误恢复。
- 代码事实：`ReflectedUnityInput.GetKeyDown` 合并 Legacy Input、Input System 和 Win32 路径；本轮同时发生过可恢复 missing-frame/frame-driver stall。当前证据足以证明原始 Escape 边沿在后续 UI frame 再次可见，但不足以把 recovered stall 升格为新的崩溃或 GC Debug issue。
- 测试事实：Batch 2 runner 的 Y-close 断言错误等待 `reason=Y`，而普通 Mod 的真实稳定原因字符串始终是 `reason=hotkey Y`。这会把已经到达 typed owner 的关闭误报为失败并注入额外 Y，必须与运行时所有权修正一起纠正。
- 初次根因结论：Y 的 typed sole-owner 修复仍然成立；`20260715-133713` 证明 title host 跨 owner 关闭共享 modal 状态、runner 使用过时 close 原因字符串，同时同一 Escape 物理周期会被多输入后端在不同帧再次暴露。这里不重开或复制 ISSUE-014，而是在同一输入/UI 所有权问题下补齐集成边界。
- 反证尝试：移除 raw Escape 后的 `GAME-SMOKE/20260715-135559` 保持了 `DTMAPI.DebugConsole` modal 状态，但两次真实前台 40 ms Escape 均未进入 typed sampler；控制台直到失败清理的 `ReturnedToTitle` 才关闭。该运行证明 raw multi-backend Escape fallback 仍是当前普通玩家短按的必要兼容路径，不能按最初假设删除。
- 修正后的最小方向：Y 继续由普通 `DebugConsoleMod` typed toggle 单一拥有；Bootstrap 保留审查原定的 native Escape fallback，但在一次 Escape 关闭后跨 reopen 保留 release-cycle guard，吞掉同一物理周期从其他后端延迟出现的重复边沿。title host 只关闭 `DTMAPI.<CurrentPage>` 形式的自有 manager menu；runner 只接受 `reason=hotkey Y` 的 typed Y close，并分别证明 Y close 前 `menuOpen=True` 与 Escape close 前 shared modal owner 正确。

### 2026-07-15 14:10 最终门禁反证与根因收敛

- 新证据：安装最新候选后的 `GAME-SMOKE/20260715-141043` 中，Y1 于 `14:11:27.522` 只分发一次并打开控制台；Escape 于 `14:11:28.554` 以 `modalOpen=True activeMenu=DTMAPI.DebugConsole` 只关闭一次。随后 runner 的 Y2 四次前台 `SendInput` 均无 DTMAPI Y 分发，终端清理明确报告运行上下文已成为 `MainMenuUiState`。这不是 Y 去抖误吞，也不是 HookProbe 干扰；本轮 `HookProbeAbsent=true`。
- 代码时序事实：raw Escape 分支先把 `suppressEscapeCloseUntilReleased=true`，随后 `Close` 立即把 `DolocTownHookCallbacks.DebugConsoleModalOpen=false`。`AgentControllerState.EnterUICheck` 的 prefix 只在该布尔值为真时跳过 native owner；因此若 native `EnterUICheck` 在同一帧或下一输入帧晚于 Bootstrap close 执行，同一枚 `GlobalToggleMenu` Escape 会继续进入 `MainMenuUiState`。
- 代码生命周期事实：现有 release-cycle guard 只在 `IsOpen` 分支内采样和清除。控制台关闭期间它既不维持 native 输入隔离，也不主动抽干多后端 Escape 边沿；这同时解释了 `20260715-140416` 中边沿为何一直延迟到 Y2 reopen 后才再次可见。
- native-owner 证据：当前公开构建 reverse metadata 显示 `AgentControllerState.EnterUICheck` 直接读取 `DolocUserInput.GlobalToggleMenu` 并进入 `MainMenuUiState`。因此修复边界应放在既有 `EnterUICheck` 隔离上，而不是让 runner 额外按一次 Escape 来掩盖暂停菜单。
- 收敛后的最小方向：Escape close 启动一个有界 release drain；关闭期间继续轮询 raw Escape、要求连续 clean frame 后才结束，并让 `EnterUICheck/UseTool/UseItem` 隔离覆盖 `console open OR close-drain active`。drain 只抑制 Escape/native gameplay 输入，不丢弃同帧合法 Y；owner 解绑、标题返回和 shutdown 必须清空 drain。最终仍以普通 Steam、无 HookProbe 的完整外部输入序列作为验收。

### 2026-07-15 14:35 旧订阅 DLL 兼容路径

- 修复验证：`GAME-SMOKE/20260715-143510` 中 Escape 于 `14:35:54.941` 关闭控制台，随后记录重复边沿被 drain 抑制及连续 clean frame 完成；`NativeMenuLeakDetected=false`，Y2 于 `14:35:56.644` 在 `context=Gameplay menuOpen=False` 正常分发并重新打开。该运行证明上一节的 Escape/native owner 根因已修复。
- 新兼容事实：本轮实际选中的 Workshop `3742714442` 旧 DLL 仍调用历史 `RegisterButton("Y")` 并订阅 `ButtonPressed`，而不是当前源码的 `RegisterKeybind(..., DtmInputScope.SaveLoaded)`。历史 `RegisterButton` 被 Runtime 映射为 Gameplay scope；控制台打开后当前 scope 降为 SaveLoaded，所以 Y3 虽由前台 `SendInput` 成功发送，却不会进入旧 handler。
- 边界结论：不能为此恢复全局 raw Y owner，也不能在 modal 打开时放开所有 legacy `ButtonPressed`；两者都会重建 2026-07-12 已修复的双 toggle 或让无关 Mod 热键穿透 modal。
- 最小兼容方向：Input registry 只向 UI host 暴露只读判定——当前 DebugConsole owner 是否有 legacy Y registration、且没有 typed Y keybind。仅在这个精确条件下，host 以 `hotkey Y` 关闭自有 modal，并对同一帧执行 `runtime.Input.Suppress("Y")`，防止视觉关闭后 Gameplay scope 立即把旧 `ButtonPressed` 再派发并重新打开。新 Mod 继续保持 typed sole-owner；其他 owner、其他按钮和其他 modal 不获得旁路。
- 门禁调整：Y-close 的权威计数改为 `modalOpen=True activeMenu=DTMAPI.DebugConsole` 的 close boundary，并分别记录 typed dispatch 与 legacy compatibility route；两条 route 总数必须与六次 Y close boundary 精确一致，不能伪造普通 Input dispatch 日志。

### 2026-07-15 旧 ABI owner-bound 复审

- 历史源码与实际 DLL 一致：旧 `OnButtonPressed(Y)` 通过 owner-bound `ButtonPressed` handler 调用 `consoleApi.Toggle(helper.ModManifest, "hotkey Y")` 并写入 `toggle requested ... open=False`。因此 host 直接 `Close` 只能复现视觉结果，不能证明旧 DLL 的事件 ABI 仍可执行。
- 反证结论：上一节提出的 host-direct fallback 不作为最终实现。它只把 legacy registration 当作布尔 feature flag；即使旧 handler 未订阅、已被 owner cleanup 移除或执行失败，门禁仍会伪通过，并把 Y mutation 重新放回 Bootstrap host。
- 最小安全边界：正常 `Gameplay` / `SaveLoaded` scope 与普通广播路径保持不变；Core 记录当前 custom modal 的 owner。仅当当前 UI 是该 owner 的 DebugConsole modal、owner 仍有 legacy Y registration、没有 typed Y keybind、Y 未被抑制且运行在 runtime thread 时，Runtime 才将 `ButtonPressed(Y)` 定向派发给该 owner 的 handler。无关 owner 不接收该事件。
- 关闭所有权：旧 DLL handler 自己调用 `Toggle(..., "hotkey Y")` 完成关闭；host 只在定向派发成功后执行 Y suppress-until-release，防止关闭后的同一物理边沿进入普通 Gameplay 广播。新 typed DLL 不进入该兼容 lane。
- 最终门禁：每次 legacy Y 关闭必须同时出现 owner-targeted route marker、旧 Mod `toggle requested ... open=False` 与 `modalOpen=True activeMenu=DTMAPI.DebugConsole` close boundary，三者严格 1:1；仅有 host close 不合格。

### 2026-07-15 Batch 2 最终自动验收

- `GAME-SMOKE/20260715-145818` 使用普通 Steam 启动、无 HookProbe、前台窗口 `SendInput`，16 个输入标签均一次成功且没有 `PostMessage` fallback。
- 六次 Y modal close 与六次 legacy compatibility marker、六次 owner-targeted `ButtonPressed`、六次旧 DLL `toggle requested ... open=False` 严格 1:1；typed modal-Y dispatch 为零，因此验收的是实际旧 ABI handler，而不是 host 伪造关闭。
- Escape close 为两次，十次 40 ms 短按与一次 1,800 ms hold 均通过；`NativeMenuLeakDetected=false`，ordered lifecycle、modal isolation、ReturnHome cleanup、HookProbe absence、三份 slot-3 文件与 `mod_infos.json` 恢复、无 Fatal 和进程退出均通过。
- 该自动集成结果补强但不替换 2026-07-12 的玩家物理手测。分钟级运行中恢复的一次 missing-frame 和两次 frame-driver stall 不属于 GC 证据。
- 最终 provenance 加固后的 `GAME-SMOKE/20260715-153336` 再次通过相同输入矩阵，并额外证明 `3742714442` 精确树、旧 DLL 哈希以及唯一 `source=Workshop` 的 Core load-source 记录；因此 loose/local 同 ID 不能再替代旧订阅 DLL 让门禁伪通过。
