# ActionSpeed Manual Watering Animation Review

- Review ID: `20260806-0002`
- Date: `2026-08-06`
- Status: `recorded`
- Scope: ActionSpeed ProductNative Hook/path review for manual watering
- Source: user manual feedback
- Related Update: [20260806-0002](../../../updates/2026/20260806-0002-actionspeed-manual-watering-tool-speed.md)

## 问题 1：用水壶手动加水没有加速动画

原始反馈：

- “用水壶手动加水没有加速动画。”
- 该行为应归入现有“工具动画加速”设置，不新增独立设置。
- 图片转写：无截图。

审查记录：

- 用户确认事实：当前游戏内手动使用水壶浇水时，ActionSpeed 没有加速角色/水壶动画。
- 代码事实：当前 ProductNative 只补丁 `AgentStateTool.OnEnter/OnExit`，其工具分类只接受 `AXE`、`PICKAXE`、`SICKLE`。
- 原生事实：当前 `24585411` 中，`ItemWaterCan` 经 `AgentControllerState.Water` 和 `BodyController._Water` 进入独立的 `AgentStateWater`。该状态在 `OnEnter` 启动角色 `water` 动画和 ToolRenderer 水壶动画，并在 `OnExit` 清理 CurrentTool/可见性；它不经过 `AgentStateTool`。
- 版本事实：`AgentStateWater.OnEnter/OnExit` 在受跟踪 `23762374` 权威中已存在；当前 `24456188`、`24567135`、`24585411` 三份反编译文件一致。当前版本新增的 `CurrentTool`/`IHasToolAnimation` 接线不改变独立状态边界。
- 根因：这不是配置读取或倍率失效，而是 Product Hook inventory 没有覆盖手动浇水的原生状态。
- 归属：ActionSpeed 单产品动画策略，属于 `Yuuka.DTMAPI.ActionSpeed` ProductNative；不进入 mandatory GameBridge，也不改变 frozen `IActionSpeedApi` Compatibility Host。
- 最小实现：在同一 exact Harmony owner 下原子增加 `AgentStateWater.OnEnter/OnExit` 两个 Postfix；Enter 复用现有 `ToolSpeedEnabled/ToolMultiplier` 和 Animator 快照逻辑，Exit 复用精确恢复。动物原生 owner 标记应像 `AgentStateTool.OnExit` 一样跨过 `AgentStateWater.OnExit`，避免状态替换时提前清除。
- 已排除：无需新增配置字段、公共 API、持续计时 Hook、存档/sidecar 写入、Transpiler 或 Compatibility Host 行为。
- 验收点：工具加速开启且倍率大于 `1x` 时，装有水的水壶手动浇地会明显加速角色和水壶动画；关闭工具加速时保持原生速度；斧/镐/镰刀和动物交互不回归；退出状态后 Animator 精确恢复；日志/QA 观察到 11 个 exact-owner patches，owner deactivation 后为零。
- blocker 判定：源码和聚焦测试可以关闭实现边界；玩家可见行为仍需当前正式版冷启动手测或等价有界 game smoke 后才能标 runtime verified。
