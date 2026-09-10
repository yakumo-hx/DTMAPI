# 本地 Wiki 传送快捷键无动作排障审查

## Review Header

- Time: `2026-08-23`
- Status: `recorded`
- Source: 用户连续手测反馈与当前 `BepInEx/LogOutput.log`。
- Scope: 只诊断本地、不发布的 `LocalWikiTeleport` 辅助插件；不修改存档，不改变 DTMAPI 产品或公开 API。
- Related records:
  - [AutoFishing 玩家按 F6 无可见反应排障审查](20260822-0001-autofishing-player-f6-no-visible-response.md)
  - [Hotkey OpenConfig No Overlay](../../../../debug/issues/ISSUE-003-hotkey-openconfig-no-overlay.md)
- Files inspected:
  - `temp/local-wiki-teleport/Plugin.cs`
  - `products/first-party/AutoFishing/src/ModEntry.cs`
  - `src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs`
  - `src/DTMAPI.BepInExBootstrap/ReflectedUnityInput.cs`
  - 当前游戏的 `BepInEx/LogOutput.log`

## Issue Review

### Issue 1: F8/F9/F10 插件已加载但没有发生传送

Original feedback:

- “没反应，试试F8/F9/F10”
- “好像还是没有触发，找一下自动钓鱼的F6怎么触发的”
- “为啥还没有传送”

Log transcription:

- `Local Wiki Teleport 0.1.1` 已由 BepInEx 加载。
- 插件启动日志确认当前游戏存在兼容的 `DolocAPI.DoTransport` 重载，并称 `InputDriver=InputSystem.onAfterUpdate`。
- 本次玩家在存档内多次按 F10；DTMAPI 在 `00:07:22` 至 `00:07:28` 记录了五次 F10 pressed/released，证明键盘、游戏焦点和当前 Input System 均正常。
- 插件没有记录预期的 `InputSystem frame callback observed`、`F10 pressed` 或任何 transport accepted/rejected/failed 行，证明调用尚未到达传送层。
- DTMAPI Bootstrap 在原生就绪首帧完成 Runtime 启动后，于 `00:06:11.824` 才订阅 `InputSystem.onAfterUpdate`；该订阅随后正常采到 F10。

Code facts and ownership:

- AutoFishing 通过 `helper.Input.RegisterKeybind(..., F6, Gameplay)` 注册，再由 owner-bound `KeybindPressed` 事件切换状态。
- DTMAPI 的底层采样不是只依赖普通 `MonoBehaviour.Update()`：Bootstrap 先安装 `Unity PlayerLoop.Update` 驱动，等原生就绪首帧完成 Runtime 启动，再订阅 `InputSystem.onAfterUpdate` 并读取 `wasPressedThisFrame`。
- `LocalWikiTeleport 0.1.1` 在 BepInEx Chainloader 阶段立即订阅，比 DTMAPI 的原生就绪边界早约两秒。订阅 API 返回成功，但其回调从未发生；当前证据说明这条早期订阅没有留在随后工作的 Input System 回调链中。
- 本地辅助插件与坐标/传送调用归调用方所有；本问题的当前失败层是辅助插件输入生命周期，不是 DTMAPI F6 注册，也不是游戏传送数据。

Root cause and rejected hypotheses:

- 根因：辅助插件在原生 Input System 就绪之前绑定帧回调，绑定时机错误；“订阅成功”被误当成“回调已存活”。
- 已否定键盘 F10 未被游戏识别：DTMAPI 有五组明确的 pressed/released 记录。
- 已否定新版 DLL 未部署：安装文件 hash 与 `0.1.1` 构建一致，日志也加载了 `0.1.1`。
- 尚未检验 `DoTransport` 是否接受三个目标，因为按键从未到达该调用。

Implementation boundary:

- 本地辅助插件应在 Awake 仅安装一个最小 `Unity PlayerLoop.Update` 首帧驱动；到原生就绪首帧后移除早期订阅并重新订阅 `InputSystem.onAfterUpdate`。
- 保留逐层日志：PlayerLoop 已进入、Input System 回调已进入、物理键已按下、传送 accepted/rejected/failed。
- 不改 DTMAPI Runtime、AutoFishing、官方 Mod 启用状态或存档；插件仍不得调用 `SaveGame`。

Acceptance checks:

1. 新进程加载修订后的本地插件并记录 native-ready PlayerLoop 首帧及延迟 Input System 订阅。
2. 载入存档后按一次 F8、F9 或 F10，日志依次出现帧回调、对应按键和传送结果。
3. 游戏实际进入对应 room；若按键已记录但 `DoTransport` 拒绝，则将后续调查限定在传送重载、调用参数或游戏当前过渡状态。
4. 未发生插件触发的原生保存，退出后没有遗留 `DolocTown.exe`。

Blocker conditions:

- 若修订后 PlayerLoop 首帧仍未进入，不能再修改键位或坐标，应改用已验证的 DTMAPI owner-bound 输入宿主或在现有 DebugConsole 产品边界内增加一次性本地入口。
- 若传送 accepted 但房间未变化，需要获取完整原生过渡日志后再判断，不能继续把问题归因于输入。

## Follow-up Manual Evidence — 0.1.2

- 用户复测反馈：“无反应”。
- 新进程明确加载 `Local Wiki Teleport 0.1.2`，并载入存档槽位 11。
- DTMAPI 在 `00:18:18` 至 `00:18:22` 记录六组 F10 pressed/released；外部辅助插件仍没有 `Native-ready PlayerLoop frame observed`、Input System 回调、物理键或传送结果日志。
- Acceptance check 1 失败：外部插件自行插入的 PlayerLoop 节点也没有进入；继续调整键位、坐标或同类自建帧回调没有依据。
- DTMAPI 自身的 `BootstrapPlugin.OnInputSystemAfterUpdate` 已在相同进程内持续工作。下一次本地实现限定为挂接这个已验证帧入口，并由该入口调用辅助插件的按键采样；不再建立并行输入驱动。

## Follow-up Manual Evidence — 0.1.3

- 用户复测反馈：“没用”。
- 新进程加载 `Local Wiki Teleport 0.1.3`，启动时 Harmony 报告成功挂接 `BootstrapPlugin.OnInputSystemAfterUpdate`；该 postfix 仍没有任何回调日志。
- 相同进程中 DTMAPI 继续收到五组 F10 pressed/released，且唯一明确的活跃帧来源日志为 `Unity frame callback observed by DTMAPI bootstrap: PlayerLoop`。
- 修正上一轮未证实的假设：`InputSystem.onAfterUpdate` 的“订阅成功”日志不等于该回调在本机实际运行；当前 F10 由 DTMAPI 的 PlayerLoop/反射输入回退链采样。
- 外部 BepInEx 插件路径至此被否定。下一实现改为普通 DTMAPI 本地 CodeMod，直接调用 `helper.Input.RegisterKeybind` 并接收 owner-bound `KeybindPressed`，与已验证的 AutoFishing F6 路径同构；本地模组只在事件处理器内反射调用当前游戏的 `DoTransport`。

## Follow-up Manual Decision — 停用并保留

- 用户确认本次 Wiki 图片补充已经提交，不再需要临时传送辅助保持启用。
- 用户要求移除当前运行时副本、保持不启用，但保留实现；后续可将其整理成一个简单的传送附属 Mod，用于定位少量不涉及剧情、仅位于隐藏房间内的伊甸果。
- 当前输入/传送链没有通过玩家验收；停用决定不应解释为 `0.2.0` 行为已修复或已被接受。
- 当前部署停用与保留位置由 [Update 20260823-0001](../../../updates/2026/20260823-0001-local-wiki-teleport-disable-and-retain.md) 记录。任何后续产品化必须从新的有界审查开始，不得由此临时包反向获得产品准入、发布授权或默认启用状态。
