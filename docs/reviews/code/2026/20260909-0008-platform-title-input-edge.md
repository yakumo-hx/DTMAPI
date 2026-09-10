# PN-037 — 标题入口的帧内按键状态

- Date: `2026-09-09`
- Status: `keyboard regressions verified; physical controller pending-player`
- Owner: [PN-037.a Update](../../../updates/2026/20260909-0017-platform-controller-bindings.md)

## 观察与原因

用户要求重试后，193045 候选中实体方向键可以移动原生标题菜单，实体 F8 却不能打开配置。19:32:24 起日志记录 F8 pressed 与 `DTMAPI.TitleSettings/open-config` 事件，证明该次输入已经送达；不能继续把 F8 失败归为自动输入问题。自动方向键没有移动原生菜单仍是另一项工具观察。

Bootstrap 在 `runtime.Update()` 返回后调用 `ProcessEntryAction()`，而 Core 的 `Update` 在 finally 中执行 `Input.ClearFrame()`，导致入口永远读不到已清空的 pressed 集合。修复只把入口调用移到 `RecordInputFrame` 完成之后、`runtime.Update` 之前，不更改 Core 的帧末清理契约。

补充回归还证实：输入配置的 Save 回调调用 registration.Update 时省略 scope，会采用 Gameplay 默认值，使标题入口保存后失效。显式传入 Title；不改公共 Update 的默认语义。

## 验证与限制

`artifacts/pn037-entry-save-regression-before.log` 在“保存后无重启再次打开”断言失败；显式 Title 修复后的 `artifacts/pn037-entry-save-regression-after.log` 七项通过。用真实 Core 采样、帧结束、配置保存与 UI 状态验证首次打开、清空后不重放和保存后再开。

首个顺序修复候选在 19:36:43 记录 F8 绑定触发及下一次 UI 更新打开菜单；该候选尚未包含 scope 修复。最终实机、导航和 Gameplay typing-focus 结论由[本轮证据](../../../debug/evidence/GAME-SMOKE/20260909-platform-input-retry/README.md)维护。无新增 native Hook 或公共 API 边界。

用户随后确认 F8、方向键与 Enter 可用，但单次 Tab 跳 2–3 项；长按方向键连续移动正常。Tab 原来调用 GetKeyDown 的 legacy/InputSystem/Win32 短路 OR；上游返回 true 时 Win32 不被轮询，后续帧可能把其同一物理按键状态重新当作新边沿。导航 Tab 改用已有 SampleButtonCached 的单后端采样，避免这条重复路径；方向键重复算法不变。代码分析解释重复风险，最终因果仍须用轻按 Tab 的实机反馈确认。`artifacts/pn037-tab-final-tests.log` 七项通过。

最终候选用户复验通过：轻按 Tab 每次一项；保存入口 F9 后关闭可重开；修正独立作者探针的字段别名后，Enabled 取消、保存、关闭重开和冷启动保持一致。Gameplay H 在真实 Y 控制台模态前后各触发一次，模态输入期间未增加计数。精确运行与计数见本轮证据，不扩大为所有输入框或实体手柄验收。20:07:40 完成原 Runtime/测试资产恢复、35 个存档及 sidecar 文件不变核对、锁释放。已观察到的三项键盘缺陷关闭。
