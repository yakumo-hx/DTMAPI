# 20260710-0001 AutoFishing 输入、蓄力与首抛卡顿手测审查

## 手测记录头

- 时间：2026-07-10，Asia/Shanghai。
- 来源：用户第五存档手动测试反馈、附件 `codex-clipboard-c26b148c-227f-4c30-98d8-2887ad76bdc6.png`、当前工作区代码与近三天 AutoFishing/Input/GC 记录。
- 范围：根因审查并实施 `docs/goals/2026/20260710-0004-autofishing-input-charge-polish.md`；没有修改第三方小神增强包或 SMAPI 参考源码。
- 禁止事项：不复制第三方/SMAPI 实现；不把 F6 关闭接到共享 Harmony `UnpatchSelf`；不以自动 smoke 代替玩家可见的后摆、短按和捕获顺序验收。
- 审查记录：本文件是可跨上下文恢复的 durable manual-QA 记录。实现追踪见 `docs/updates/2026/20260710-0004-autofishing-input-charge-polish.md`。

## 逐条手测审查

### 问题 1：第五存档首次抛竿视觉卡顿

原始反馈：

- 第五档测试首次抛竿动作视觉掉帧严重，后续连续运行流畅；用户猜测是脚本为了截图检查锁定动作，并认为本项手测没有问题。
- 图片转写：截图中的上轮状态说明写明，第五存档曾发现产品迁移缺口：旧 smoke 在第一方产品注册前注入 F6，`FishingAutomation service was not registered`、`primitiveSessions=0`；随后调整测试顺序，改为观察第一方 session 的 WaitPlayable、BiteReady、小游戏和第二次抛竿。这是此前修复过程，不是用户本轮新增失败。

审查记录：

- 用户确认事实：首抛明显卡顿，后续循环流畅；正常手动使用未因此判失败。
- 截图/日志观察：附件只显示上轮 Codex 状态文字，没有新的游戏画面或帧时间数据。
- 代码/文档事实：旧 `AutoFishingSmokeCase` 在 Unity 主线程循环 40 次 `Thread.Sleep(125)`，单次可阻塞约 5 秒；移动取消另有 `Thread.Sleep(1100)`。这足以解释只在测试首段出现的视觉冻结。
- Codex 推断：这是 smoke 调度阻塞，不是截图锁、原生首抛着色器问题或产品循环本身的持续性能问题。
- 反证/未证实：没有当前构建的帧时间捕获，不能扩张为“所有首抛卡顿都已排除”。
- 归属：GameBridge smoke harness / 测试证据。
- 需要更新：goal、update、smoke matrix。
- 验收点：第五存档 smoke 首抛期间游戏主线程继续逐帧运行；`AutoFishingSmokeCase` 无阻塞 sleep；正常第二抛及后续循环不回归。
- blocker 判定：如果去掉 smoke sleep 后真实首抛仍冻结，必须保留为新的运行时/资源加载问题，不能以本根因关闭。

### 问题 2：现有自动钓鱼功能基线通过

原始反馈：

- 自动钓鱼手测通过；单功能基本通过、组合功能通过、热键更换通过。
- 图片转写：同问题 1，截图记录的是上轮 session/smoke 调整过程，没有推翻用户本轮通过结论。

审查记录：

- 用户确认事实：自动抛竿、等待、提竿、原生小游戏路径、循环，以及三个既有功能的单独/组合使用均可工作；换绑也能保存并生效。
- 截图/日志观察：没有本轮新增日志包；此前 2026-07-10 第五存档 DefaultLoop、FastAnimations、InstantBite、SkipMiniGame smoke 证据继续作为实施前基线。
- 代码/文档事实：第一方产品已拥有 enable/manual-cancel/recast/decision 状态机；GameBridge primitives 拥有单 session、原生交易、输入/动画 lease 与 Hook router；兼容 API 保留。
- Codex 推断：本轮应做最小增量，不能把已通过的产品循环重新塞回兼容服务。
- 反证/未证实：通过结论不覆盖新增蓄力 0/0.5/1，也不覆盖新的捕获顺序和快速短按。
- 归属：第一方 AutoFishing 产品 + GameBridge Fishing Primitives。
- 需要更新：goal、update、hook map、API matrix。
- 验收点：新增功能后 DefaultLoop、三个独立选项和组合场景仍走真实原生结果路径并能连续重抛。
- blocker 判定：任一既有单功能/组合循环回归都阻止本轮完成。

### 问题 3：动画加速时原生后摆消失

原始反馈：

- 点按后原生过程应包含渔竿向后蓄力动作（即使蓄力为零也有），再抛竿；当前向后动作没有了。
- 图片转写：无对应游戏截图。

审查记录：

- 用户确认事实：当前玩家可见的后摆动作缺失。
- 截图/日志观察：无视觉证据文件；必须以新手测为最终门。
- 代码/文档事实：当前反编译构建中 `AgentStateFishingReady.NextState()` 先调用 `CheckAnimationDone()` 更新私有 `_isAnimationDone`，再读取 `NormalUseToolInProgress`，最后在 `_isAnimationDone=false` 时继续停留 Ready。`fishing_ready` 动画完成本身就是原生状态门。
- Codex 推断：零蓄力不应人为多 hold 一个 FixedUpdate；那会推进 `_castTimer`，使最终 power 非零且受帧率影响。正确边界是立即释放合成 use input，让原生 `_isAnimationDone` 保留后摆并保持精确 0。
- 反证/未证实：只读原生方法体能证明状态门，不能证明当前机器最终渲染观感；必须手测。
- 归属：GameBridge Ready 输入覆盖与原生状态机边界。
- 需要更新：hook map、smoke matrix、update。
- 验收点：charge=0、FastAnimations 开/关都能看到原生后摆，且 0 保持最短抛距；正向 charge 不改变目标。
- blocker 判定：如果原生 `_isAnimationDone` 门在实际游戏中仍无可见后摆，不能再靠一帧 timer hold 猜修；需采集动画状态/帧证据并重新审查。

### 问题 4：新增独立蓄力幅度 0–1

原始反馈：

- 调整蓄力幅度作为独立小功能，可设为 0–1。
- 图片转写：无截图。

审查记录：

- 用户确认事实：需要玩家可配置的独立 0–1 蓄力目标。
- 代码/文档事实：兼容 DTO 历史上已有 `CastChargeRatio`；第一方 cutover 曾临时固定为 0。原生责任仍是 Ready 状态的 use-input hold/release，不能直接写鱼竿结果 power。
- Codex 推断：应使用每次 `TryCast` 前传入的 scalar primitive request，避免把产品配置放入全局动画 lease 或重新扩张兼容 API。
- 反证/未证实：单位测试可证明 clamp/order，不能替代 0/0.5/1 的实际抛距与视觉验证。
- 归属：第一方产品配置 + internal Fishing Primitives + GameBridge Ready adapter。
- 需要更新：goal、public API matrix、hook map、smoke script/matrix、i18n/README。
- 验收点：0、0.5、1 都生效；InstantBite、SkipMiniGame、FastAnimations 与 charge 四个维度互不暗含。
- blocker 判定：任何目标值需要直接写 native result power，或 FastAnimations 改变目标值，都阻止完成。

### 问题 5：快速短按热键偶发漏识别

原始反馈：

- 热键仍需长按/重按，短快按下不一定识别；要求先研究小神增强包与 SMAPI，再决定；若二者也这样可接受。
- 图片转写：无对应截图。

审查记录：

- 用户确认事实：快速短按体验不可靠。
- 代码/文档事实：小神增强包钓鱼无热键，不能提供对照；SMAPI 使用一份共享输入帧，keybind 只读取帧状态。DTMAPI 原产品用 demand-local `JustPressed`：Bootstrap 在 `runtime.Update()` 之前采样，而首次产品查询发生在 Update 内，因此第一次查询只能为下一帧布防；标题/菜单后 watch 过期时，第一下短按可丢失。
- Codex 推断：持久产品动作应使用一条 owner-bound Gameplay keybind registration，而不是每帧 demand-local snapshot。该注册在采样前存在、标题范围不采样，并消费同帧 press/release edge。
- 反证/未证实：单测证明 Core edge 语义，不等于所有 Unity/Win32 backend 在实机上捕获任意极短输入。
- 归属：第一方 mod 输入选择 + DTMAPI Core/Bootstrap 输入帧。
- 需要更新：ISSUE-010、input API matrix、goal、update、smoke matrix。
- 验收点：进入存档后连续 10 次约 40ms 短按逐次切换；长按只切换一次；F6/F7 均通过；标题界面不采样 Gameplay 注册。
- blocker 判定：若 Core 已收到 edge 而产品不切换，修产品事件；若 Core 未收到，必须继续定位 Bootstrap backend，不能延长按键时间掩盖。

### 问题 6：换绑 UI 应为 Capture 后识别下一键，并提供 Reset

原始反馈：

- 当前显示为“当前热键 / 捕获 / 无”；理论上第二控制应为重置。
- 目前是先按某键、再点击捕获，捕获了之前的键；希望点击捕获后识别下一个按键。
- 用户担心标题界面 DTMAPI 持续监控并造成 GC 压力。
- 图片转写：无设置页截图；附件只显示上轮 Codex 文字状态。

审查记录：

- 用户确认事实：捕获顺序反了，且希望 `Reset` 恢复默认 F6。
- 代码/文档事实：旧点击回调直接设置 `capturingKeybindItemId`，同一 Unity 帧的 frame-latched key/mouse edge 仍可能被下一次 UI Update 读取。标题页空闲时并不扫描整套 capture candidates；全量扫描只在 `capturingKeybindItemId != null` 时发生。真正的标题持续轮询问题是 AutoFishing 的 local toggle query，现改为 Gameplay registration 后标题排除。
- Codex 推断：Capture 点击时应清掉/prime Win32 transition 状态并记录 Unity frame；跳过点击帧，从下一帧开始扫描所有 backend。默认值能力应放到新的可选 config-menu interface，避免给现有 `IDtmConfigMenuApi` 增加抽象成员而破坏第三方实现者。
- 反证/未证实：当前没有自动 UI 点击/键盘捕获测试，最终顺序、Reset 布局和清除操作必须手测。
- 归属：Bootstrap reflected title settings UI + ModConfigMenu optional API + 第一方 config。
- 需要更新：API matrix、goal、update、smoke matrix。
- 验收点：先按 F7 再点 Capture 不会绑定 F7；点 Capture 再按 F8 会绑定 F8；Reset 恢复 F6；Capture 中 Escape/Backspace/Delete 清为 None；标题空闲无 AutoFishing Gameplay 轮询。
- blocker 判定：若 capture backend 在点击后的下一帧仍读到 Mouse0/旧键，或新增 API 破坏既有 interface 实现，保持未完成。

## 问题分组

- UI/Config：问题 6。
- Input/GC：问题 5、问题 6 的标题范围担忧。
- Hook/GameBridge：问题 3、问题 4。
- 产品回归：问题 2。
- 测试/证据：问题 1，以及所有新增手测门。

## 边界约束

- 必须做：保留完整自动循环；四个产品设置独立；0–1 蓄力；原生后摆；可靠短按；Capture 后取下一键；Reset=F6；owner/title/failed-entry cleanup 有界。
- 禁止做：复制小神/SMAPI 源码；直接写鱼竿结果 power；在 F6 关闭时卸载共享 Harmony；为每个 keybind 复制一份平台状态；以合成 A 快照冒充 `HorizontalMoveFactor` 证据。
- 可选做：未来把 hook scope 分组；本轮不以反复安装/卸载 Harmony 为目标。
- blocker 判定：构建通过但没有真实第五存档视觉/短按/捕获证据时，只能写 source-and-unit-verified，不能写 user/runtime complete。

## 实施后证据（不改写原始手测事实）

- 最终 Release build/unit 与静态检查通过。
- 第五存档 `GAME-SMOKE/20260710-135136` 通过 F7、charge=0 DefaultLoop、四次抛竿、三次真实 Wait/Bite/MiniGame/Pull、一次 soak、关闭清理、报告、进程和 fatal-window 门。
- `GAME-SMOKE/20260710-134805` 仅作为正向 charge=0.5 在 progress=0.52 release、Fast Hook 和 clean-exit 的部分证据；该落点未进入当前 pond fixture，不作为完整循环通过。
- 玩家可见后摆、0/0.5/1 距离、快速短按、Capture/Reset/clear 仍待手测，因此本 review 的相关 blocker 未关闭。
