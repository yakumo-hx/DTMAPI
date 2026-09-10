# AutoFishing Manager 无关 EquipmentSlots 清理扇出审查

## 记录信息

- 日期：`2026-08-04`
- 状态：`recorded / root cause confirmed / unrelated-owner no-op correction authorized`
- 性质：AutoFishing 正式 Manager lifecycle 的专项 receipt 已通过，但通用 ModOwnerLifecycle 因无关 EquipmentSlots Host 清理失败而拒绝整场 smoke 的根因审查
- Audited HEAD：`f39c4c83c89a76bb8417e5d601a72dc590a78c95`
- Implementation owner：[DTMAPI 0.6.0 唯一权威路线图](../../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)
- 上游范围权威：[MoreEquipmentSlots 1.00 Branch B Review](20260804-0007-moreequipment-100-overlap-decision-gate.md)
- 失败 outer：`docs/debug/evidence/BATCH6-AUTOFISHING-MANAGER-LIFECYCLE/20260804-204034-fa6e7198`
- 失败 game smoke：`docs/debug/evidence/GAME-SMOKE/20260804-204036`

本 Review 只冻结失败事实、跨域扇出根因和有界修复授权。实现、changed files、验证与最终发布状态只由 owning Update 维护。它不改变 MoreEquipmentSlots Branch B 的范围决定，也不授权修复或发布其 1.00 ProductNative。

## 1. 已确认事实

1. 正式 Manager runner 使用第五存档、`NoNativeSave`、Steam 启动路径和 exact AutoFishing package `23AEA43A2B367775578E4457B2F6C9992DF240C6E83F17D2A615ACFA1D93BF36`。第一次尝试只被此前主动中止 GC 场留下的 exact QA stage 拒绝；该 stage 的 run ID、三个文件长度/哈希和目录成员与 `GAME-SMOKE/20260804-202514/qa-host-stage.json` 完全一致，在无游戏进程且持有共享锁时按 runner 契约非递归清除。它没有进入产品验收。
2. 第二次尝试实际完成 `SameProcessDisable`。`auto-fishing-manager-lifecycle.json` 为 `Passed`：产品产生至少一次 native cast，两次真实 `ModManager.ReloadMods` receipt、marker 建立/移除、第一次 reload 后产品实例/Core owner roots/callback/22 patches 全部归零，第二次 reload 仍禁止同进程重入并保持 `restart-required`。
3. 同场 `Batch6AutoFishingManagerLifecycle`、专项 cleanup、marker restore、NoNativeSave archive/committed sidecar、Fatal、进程退出、QA Host cleanup 和官方 profile restore 都通过。整场 `RunStatus=Failed` 的唯一真实失败门是通用 `ModOwnerLifecycle`。
4. 通用 owner participant 在移除 `Yuuka.DTMAPI.AutoFishing` 时扇出到所有 GameBridge 服务。EquipmentSlots Host 对该 owner 的 options、states、entries、storage owner、journal、candidate、guard 和 generation 计数全部为零，却记录：`Player.EquipmentSlotsCompatibilityOwner = cleanup-unproven; reason=no enabled frozen compatibility owners; unpatchCall=True`。
5. `EquipmentSlotsCompatibilityService.RemoveOwner` 已先计算 `hasEquipmentState`，但 `finally` 无条件调用 `ReleaseCompatibilityHooksIfUnused()`。因此完全无关的 owner 也会触发全局 EquipmentSlots exact-target 解析和 unpatch 证明。
6. 当前 1.00 `BodyController.OnAttacked` 是五参数，而冻结 Host 仍按四参数查找。该缺口和当前 archive-family 变化已经由 Branch B Review `20260804-0007` 记录并停止实施；它应在真实 EquipmentSlots consumer/owner 路径继续 fail-closed，不能借 AutoFishing 验收顺手放宽。

## 2. 根因

AutoFishing 的 owner cleanup 没有 EquipmentSlots 资源需要释放。失败来自 EquipmentSlots cleanup 的跨 owner 全局副作用：方法虽然知道 `hasEquipmentState=false`，却仍在 `finally` 运行“最后一个 frozen compatibility owner 离开时才需要”的 Hook release。当前 Branch B target 无法解析使这次无关调用返回失败，随后被统一 participant 归因成 AutoFishing cleanup failure。

这不是 AutoFishing 产品残留，也不是已有 Manager 专项 gate 过宽；是无关 owner 的 EquipmentSlots no-op 语义被破坏。真实 EquipmentSlots owner 若拥有任一资源，仍必须执行恢复、状态删除和 exact Hook release，并在当前 Branch B 未解决时保持失败。

## 3. 被拒绝方案

- **把本场专项 receipt 直接当完整 Manager matrix PASS。** 拒绝；外层通用 lifecycle 门真实失败，不能拼接或忽略。
- **在 smoke runner 中豁免 `ModOwnerLifecycle` warning。** 拒绝；会隐藏其他产品真实 cleanup failure。
- **把五参数 target 改回可解析或只修 attack seam。** 拒绝；Branch B 同时包含 native archive/save authority，现有范围明确禁止 attack-only 实施。
- **让 EquipmentSlots 对所有 owner 的 target-unavailable 都返回成功。** 拒绝；会掩盖真实 retained consumer 的 Hook/回调残留风险。
- **卸载或删除 frozen Compatibility Host。** 拒绝；公开 `0.3.1-dtmapi` 仍是 frozen ABI 的真实 retained consumer。

## 4. 有界修复授权

只授权调整 `EquipmentSlotsCompatibilityService.RemoveOwner` 的无关-owner no-op：

1. `hasEquipmentState` 必须覆盖 `CountOwnerResources` 所拥有的全部 per-owner state，包含 `equipmentSlotStorageGenerations`。
2. 只有 `hasEquipmentState=true` 时，`finally` 才调用 `ReleaseCompatibilityHooksIfUnused()` 并要求 exact-owner Hook 释放证明。
3. 即使无 owner state，既有逐字典 `Remove` 与 UI lifecycle 清理可保持幂等；不得创建 Host、安装 Hook、改变全局 options 或把 target-unavailable 标为成功。
4. 自动测试必须证明无关 owner 在当前无资源时不调用/不依赖 compatibility target cleanup；同时锁住有 owner state 的路径仍执行 release 并在无法证明时失败。
5. 不改 `BodyController.OnAttacked` target、Compatibility Host attack body、save fingerprint、公开 ABI、MoreEquipment ProductNative、Catalog、包或发布集合。

## 5. 验收边界

- 先通过 EquipmentSlots/owner-cleanup 聚焦 Unit、源码边界、Catalog 和双 PowerShell 静态门（若对应脚本受影响）。
- 刷新 0.6 Runtime 候选后，用同一个 AutoFishing exact package 从 `SameProcessDisable` 开始重跑完整三进程 Manager matrix；失败场专项 receipt 不能与后两场拼接。
- 新矩阵必须同时通过专项 receipt、通用 `ModOwnerLifecycle`、NoNativeSave、进程/Fatal、profile/source/deployment 和共享锁收尾。
- MoreEquipmentSlots Branch B 继续作为局部阻断；本修复的 PASS 不能冒充 retained `0.3.1-dtmapi` 或延期 ProductNative 的 1.00 行为/保存验收。
