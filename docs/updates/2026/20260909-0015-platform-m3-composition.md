# 20260909-0015: PN-023 M3 生态组合验收

## Metadata

- Update ID: `20260909-0015`
- Date: `2026-09-09`
- Lifecycle Status: `verified`
- Validation Level: `unit, runtime`
- Runtime Validation: `passed`
- Related Issue State: `none`
- Source: 连续执行授权及 [PN-023](../../planning/platform-next/execution-next.md#pn-023m3-合成验收和首个公开-target-候选)；组合中新增的预加载来源问题由 [专项 Review](../../reviews/code/2026/20260909-0007-platform-native-preloader-origin.md) 收口。

## Summary

独立作者的 Advanced Provider、Strict Consumer、共享契约、私有库、资源和 Control 已完成 M3 组合采用闭环，完整 API/Runtime/SDK 0.7.0 候选验收通过。新 target 的作者流程、同源完整 Release、准确 r3 实机组合、一小时标题后读档、公开命令、owner 关闭和真实恢复均已完成；新能力保持 Experimental，尚未发布。

## Changed Files

仓库外 PN023 组合语料、本记录及状态路由。组合发现 Unity 内存预加载 Location 为空；Bootstrap/Core 增加预加载器原始来源绑定，见 [原因与边界](../../reviews/code/2026/20260909-0007-platform-native-preloader-origin.md)。SDK/target 原冻结字节不变，新 Runtime 修复字节另存 candidate-runtime-b。

## Validation

内部阶段公开 SDK 已生成 Provider、Consumer、Control、Provider 故障和更新包。原生来源回归、Core 154 入口、Runtime 构建及 Catalog/文档检查通过。172128/172443 原始失败保留；修复后 173304 组合及旧 ABI 实机通过。Provider、共享依赖和 native provenance 故障均证明独立 Control 存活，故障输入已恢复。

真实官方 Local 列表已实际启用三个测试包，冷启动共享调用、reflection、公共命令及来源快照通过；官方停用 Provider 后 Consumer 正确关闭，Control 仍可调用，第二次冷启动再次验证依赖阻断。原生界面清理了不在真实目录中的旧 Visitor 记录，撤回时已恢复完整原始选择文件和 Runtime；30 存档与 5 sidecar 内容/长度/mtime/数量不变。

Gameplay 文字焦点尝试 20260909-180154 自动 H/Y/Escape 无可观察送达，计数始终零；该次不能冒称 typing-focus Passed，见[早期证据](../../debug/evidence/GAME-SMOKE/20260909-platform-pn023-runtime/README.md)。后续 194252 用户实体 H 在真实 Y 控制台模态前后各触发一次；模态输入期间计数保持 1，关闭后增至 2，建立有效正对照。该范围的文字输入隔离通过，详见[重试证据](../../debug/evidence/GAME-SMOKE/20260909-platform-input-retry/README.md)。该内部阶段测试资产恢复、35 个存档/sidecar 文件不变及锁释放通过；完整 0.7.0 的后续结果见下。

## Evidence

固定输入来自 PN-010/022 已冻结 SDK 与 Runtime；本卡新证据记录于 artifacts/pn023-* 及独立实机目录。

## Rollback Notes

公开撤回测试 Mod、恢复有意改变的非存档资产；NoNativeSave 不写回存档。

## Follow-Up

PN-033.a 已完成；PN-037.a 的有界实验接受，b 的原生标题方向入口和面板键盘事务已通过，新增默认关闭/重启生效及动态分辨率也有准确候选证据。有效控制台模态输入对照、最终组合和真实恢复均已完成，实体设备单列 PN-037.c pending-player；本批未上传。

内部 0.6.5 / SDK 0.6.6 增量已确认原生方向入口、共享 Type/Assembly 身份、session command、旧 ABI 和关闭顺序；[该轮证据](../../debug/evidence/GAME-SMOKE/20260909-platform-native-entry-065/README.md)记录正常退出与完整非存档资产恢复。旧故障矩阵只复用于未变边界。

完整 0.7.0 的独立作者 new/build/pack/Doctor/symbols、CLI/IDE 字节比较与 API 精确冻结检查通过，Runtime r3 来自 `9fb6f018`，SDK r2 ZIP 与该来源完整 Release 重建字节一致。2026-09-10 的最终 r3 加载七个 managed code 探针及既存 AnimalPack，共八个 Mod；至少 3646.446 秒连续标题停留后读档，Provider/Consumer 在 Entry 和 SaveLoaded 的共享 Type/Assembly=true、值 71，Provider 的真实 agent/资源/Hook 及独立 Control 通过；原 unrecompiled retained ABI 返回 73、写入 1 次。

Consumer 先关闭且此时 Provider 仍可用，随后缓存 API 被拒、Provider ownHook=False；八个 owner 的 remaining/failures=0。三条公开命令在独立短冷启动的有效会话内通过，退出时 processed=3、pending/inFlight/replayEntries/handlerFailures=0。长测中的已关闭会话请求失败单列保留，不冒充成功。两次 runner 均正常退出；最终测试资产、真实选择和原安装恢复，35 个原生存档/sidecar 及 16 个配置文件未变、锁释放，见[最终组合证据](../../debug/evidence/GAME-SMOKE/20260909-platform-release-070/README.md)。原故障矩阵只复用于源码/native 边界未变部分；本次不扩大保存、实体手柄或多平台保证。
