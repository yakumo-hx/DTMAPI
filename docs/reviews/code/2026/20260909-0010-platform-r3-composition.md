# M3 综合 R3：完整 target 的准入复盘

- Lifecycle: accepted
- Scope: PN-023 的 P01–P07 组合；源输入截至 c03e4282，内部 Runtime/API 0.6.5、SDK 0.6.6。
- Implementation: [Update 0015](../../../updates/2026/20260909-0015-platform-m3-composition.md)。

结论：支持从已提交的 DTMAPI 自有源码生成完整 0.7.0 target 和匹配 SDK，继续发行验收。此结论不等于新版本字节实机通过或已发布。

P01–P04 的严格 reader、依赖计划、共享 CLR 身份和拒绝闭包沿 [R3.shared](20260909-0005-platform-r3-shared.md)。P05 的任意作者 Advanced 生成、来源/签名验证与 owner 清理沿 [R3.native](20260909-0006-platform-r3-native.md)，预加载来源修复沿 [Review 0007](20260909-0007-platform-native-preloader-origin.md)。没有证据要求改变既有包协议或 native owner 边界。

P06 的仓库外工程、私有库、嵌入/包内资源、显式锁定 restore 和 CLI/IDE 共同输入由 [PN-022](../../../updates/2026/20260909-0014-platform-author-projects.md)证明。P07 的组合采用由 [PN-023](../../../updates/2026/20260909-0015-platform-m3-composition.md)证明：官方启停、冷更新、Provider/shared/native 故障闭包与独立 Control；实体 H 的控制台模态对照只支持该具体输入边界。

[最终内部增量](../../../debug/evidence/GAME-SMOKE/20260909-platform-native-entry-065/README.md)覆盖新增原生方向入口、共享调用 71、Type/Assembly identity、旧未重编译 ABI 返回 73，以及 Consumer 先于 Provider 关闭、缓存 API 拒绝和 ownHook=False。原生入口由用户只用实体键盘验收；自动按键未送达不记通过。正常退出后 35 个 archive/sidecar 未变，测试资产已恢复。

[A1/A2 返修](20260909-0009-platform-m3-input-release-acceptance.md)已补原生标题动作与真实 MSBuild 输入闭包。来源测试证明 Shared/版本/资源/外链源变更和旧输出拒绝；平台输入已精确提交，Wiki 维护改动未纳入。实际最终 builder 仍须以新提交重建并检查不漂移。

后续必要门：0.7.0 的精确 source recipe 与篡改测试、匹配新 SDK 的仓库外作者流程和旧 reader 抽验、新 Runtime Mono 冷启，以及 PN-031 的完整 Release、准确安装器候选、升级恢复和固定最终字节一小时标题 idle 后读档。实体手柄、其他宿主平台和上传仍不在本轮通过范围；不因这些外部项停止已授权的内部验收。

2026-09-10 接续：上述 Windows 候选门由 [PN-023](../../../updates/2026/20260909-0015-platform-m3-composition.md)和 [PN-031.a](../../../updates/2026/20260909-0019-platform-release-preparation.md)完成，准确 r3、完整 Release r9、至少 60 分 46 秒标题后读档、公开命令及真实恢复均通过。本 Review 原内部输入与当时决定保持不变，最终产物身份由后续验收记录拥有。
