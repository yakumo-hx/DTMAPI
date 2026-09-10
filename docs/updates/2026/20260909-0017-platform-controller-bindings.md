# 20260909-0017: PN-037.a 实验性控制器绑定

## Metadata

- Update ID: `20260909-0017`
- Date: `2026-09-09`
- Lifecycle Status: `verified`
- Validation Level: `unit, runtime`
- Runtime Validation: `partial`
- Related Issue State: `none`
- Source: [PN-037.a](../../planning/platform-next/execution-next.md#pn-037a实验性控制器按键绑定) 与 D09 已定边界，无新增公共设备契约决定。

## Summary

沿现有帧采样增加控制器按钮和可修复的配置编辑；数字 JoystickButton 保留 raw 含义，轴/扳机未支持不冒充按钮。无设备保持配置，连接/焦点变化后先 neutral。

## Changed Files

GameBridge 输入适配、Bootstrap 配置行及真实入口 action；AutoFishing 继续消费既有 toggle 注册。

## Validation

七项定向入口已通过设备快照/边沿/组合/序列化/owner 清理、无设备分配和未知绑定提示。Mono 已验证无设备页面、鼠标保存/取消/重置、冷启动值保留及未知按键完整保存/可修复提示。后续实体键盘已证明 F8 入口、保存为 F9 后关闭重开，以及 Gameplay H 的有效触发与控制台模态隔离；最终恢复通过。实体设备、实际产品 toggle 与 Steam Input 仍 pending-player，不由模拟或日志探针晋级。

## Evidence

重试由[本轮证据](../../debug/evidence/GAME-SMOKE/20260909-platform-input-retry/README.md)与[帧内入口复盘](../../reviews/code/2026/20260909-0008-platform-title-input-edge.md)维护：实体 F8 已送达，但处理晚于 ClearFrame；另修复保存入口按键时丢失 Title scope。修复后的七项定向回归及用户键盘复验通过。

[早期实机证据与失败修复](../../debug/evidence/GAME-SMOKE/20260909-platform-pn037-runtime/README.md)保留当时键盘未送达的限制，后续结论查重试证据。匹配的游戏 25163613 本地输入类型用于只读签名与所有权分析。

## Rollback Notes

撤回适配和界面增量；旧键盘配置字符串保持兼容，未知字符串保留供修复。

## Follow-Up

有界代码/无设备/键鼠出口已满足；首发受影响增量以及实体手柄反馈仍分列。用户询问同键关闭后同意保留现状：入口 action 仅打开，菜单已开时忽略；关闭使用菜单 Cancel 或“关闭”按钮，不新增 toggle 行为。
