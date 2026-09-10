# Manual QA Review: 0.3.1 Regression And New Content Round

- 时间：2026-06-06 +08:00
- 来源：用户 `/goal`，要求把下一版做成手测回归修复 + 石油、矿井、更多装备栏位研发轮次。
- 范围：记录本轮实现前提、代码/文档事实、验收点和 blocker；本轮后续实现需要更新独立 goal 文件、debug/update/smoke/hook/API 记录。
- 禁止事项：不 reset/revert 既有未提交改动；不复制旧 DLKsmapi / DLK_SecondMotor；不改写第三方/Workshop 内容；普通 DTMAPI mod 不放 `BepInEx/plugins`。
- 审查记录：`docs/reviews/manual-qa/2026/20260606-0004-031-regression-new-content-review.md`

## 当前基线事实

- 当前工作树已经包含未提交的 0.3.0 Zoom slice；受控 runtime 版本为 `0.3.0`。
- 0.2.4-0.2.9 文档记录显示本轮大部分功能曾有 smoke evidence，但用户要求把四条手测反馈作为新的 debug/update 事实记录，不能用旧 smoke 覆盖。
- 因此本轮目标应作为新的 patch 轮次处理：从当前 `0.3.0` bump 到 `0.3.1`，并重新跑第三存档验证。

## 问题 1：牧铃隐藏产物 UI 与颜色配置

原始反馈：
- 修复切换动物先闪“心情”的问题。
- 恢复隐藏产物行大字号。
- `+` 自定义颜色色块必须显示输入框；普通预设不显示输入框。

审查记录：
- 用户确认事实：牧铃 UI 的第一帧/切换生命周期仍是重点，不允许只验证最终状态。
- 代码/文档事实：旧 0.2.9 smoke 记录了 `independent cloned ProgressBar prefilled rows=1`，但当前 GameBridge 仍有 `AnimalViewer.Show` postfix 后渲染路径；若原生 `Show` 先刷新 mood row，再由 DTMAPI 追加行，就可能出现肉眼可见的“心情”过渡。
- Codex 推断：本轮应把清理/预填克隆行前移到 `AnimalViewer.Show` prefix，并在无隐藏产物或不可见动物时清掉旧克隆行，避免切换污染。
- 反证/未证实：未证明旧 0.2.9 screenshot 能覆盖用户新反馈；需要新第三存档截图/日志。
- 归属：DTMAPI GameBridge AnimalViewer API、AnimalHusbandryProgressMod、ConfigMenu pending preview。
- 验收点：第三存档连续切换动物，日志显示 `moodOverride=False` / `stateDescriptionOverride=False`，隐藏产物行从打开/切换后即为独立大字号行；标题配置页普通色块无输入框，`+`/Custom 有输入框。
- blocker 判定：如果无法消除第一帧原生 mood 暴露，目标不能 complete，只能报告 native UI 时序限制和证据。

## 问题 2：自动钓鱼与动作加速配置/功能

原始反馈：
- 二级 UI 同行对齐。
- 自动完成小游戏和跳过小游戏必须分离。
- 动画加速必须作用于抛竿/收竿。
- 自动装水二级 UI 同样修正。

审查记录：
- 用户确认事实：配置 UI 和真实钓鱼阶段都要验收，不能只看 config 保存。
- 代码/文档事实：当前 AutoFishing mod 使用 `AddInlineBoolBoolOption` 分离 `AutoCompleteMiniGame` 和 `SkipMiniGame`；GameBridge 已有 `FishingCastEnterPostfix`、`FishingPullEnterPostfix` 和 `FishingGameScrollBar.UpdateGame`。
- Codex 推断：重点是重新证明 skip=false 会进入原生 mini-game 并自动成功，skip=true 才从 Wait 进入 Pull；Cast/Pull 动画均要有日志样本。
- 反证/未证实：当前本轮尚未跑新 smoke；旧 0.2.4/0.2.8 evidence 只能作代码方向参考。
- 归属：AutoFishingMod、ActionSpeedMod、ConfigMenu inline row renderer、GameBridge fishing/action-speed hooks。
- 验收点：第三存档/标题配置 smoke 覆盖 AutoFishing、ActionSpeed 页面截图；日志包含 Cast 和 Pull 动画加速；skip=false 看到/记录 `FishingGameScrollBar`，skip=true 记录 `AgentStateFishingPull`。
- blocker 判定：若真实鱼类 roll 不稳定，需要保留 smoke-only force-fish gate，但必须明确普通玩家路径没有强制改 fish/trash 结果。

## 问题 3：Y 控制台任意位置存档与传送核对

原始反馈：
- 把任意位置存档做成 Y 控制台功能。
- 导出当前传送地点 CSV，包含内部 ID、地图、坐标、当前显示名、建议名、来源，供用户人工筛选。

审查记录：
- 用户确认事实：这是 player-visible Y 控制台功能，不应停留在 smoke-only API。
- 代码/文档事实：当前 `ReflectedDebugConsoleUi` 已有 `Save here` 和 `Export CSV` 按钮；`ITeleportDebugApi.ExportDestinationsCsv` 写出 `teleport-destinations.csv`。
- Codex 推断：本轮主要是重新验证按钮可见、调用原生 `DolocAPI.SaveGame`、CSV 字段完整，并把新 CSV 作为本轮证据。
- 反证/未证实：旧 CSV 行数/字段不能代替本轮导出。
- 归属：Bootstrap Y Console UI、GameBridge save/teleport debug APIs。
- 验收点：第三存档打开 Y 控制台，`Save here` 成功并记录 before/after room/position；导出 CSV 字段包含 internal id、map/room、x/y、current display、suggested display、category/source。
- blocker 判定：如果任意位置 native save 被拒绝，必须报告当前 room/slot 状态，不得写存档文件绕过原生保存。

## 问题 4：第二辆摩托真实独立修复

原始反馈：
- 恢复/确认官方示例贴图来源。
- 双摩托不互相替换。
- 骑第二摩托换图不把原摩托带到入口。
- 不导致地图边界卡死。
- 召唤动画尽量 clone 原版飞到角色位置体验。

审查记录：
- 用户确认事实：第二摩托是 vehicle/lifecycle 高风险项，旧 evidence 不能覆盖新手测。
- 代码/文档事实：当前 GameBridge 已有 `IMotorVehicleApi`、`ItemMotorKey.OnUse`、`DolocAPI.EnterRoom`、`SetMotorPosition`、AgentPosition proxy 同步和 appearance isolation；installer 记录曾移除全局 official example motor sprite replacement。
- Codex 推断：本轮需要重跑 vehicle smoke，尤其是原始/第二摩托同场、appearance isolation、边界切图、原摩托不跟到入口、noStuck。
- 反证/未证实：召唤动画“原版飞到角色位置体验”仍偏手感，需要日志/视频或明确 blocker；自动 smoke 只能证明 clone/position/transition。
- 归属：GameBridge Motor API、SecondMotorMod、installer official-local asset packaging。
- 验收点：第三存档同场双摩托、骑第二摩托切图、原摩托留在旧房间、不在新入口可见、玩家不被边界卡死；日志记录 official sprite/clone strategy。
- blocker 判定：若原版飞行动画无法安全复用，只能报告最大可实现的 clone/summon 行为，不能声称完整手感完成。

## 问题 5：石油 Mod

原始反馈：
- 优先走官方内容 JSON。
- 新增石油物品，煤矿采集小概率出，矿井可产出。
- 石油用于研究矿井、合成矿井、高燃料值，高于当前最高燃料值。
- 售卖、图标、本地化、Y 控制台分类都正常。

审查记录：
- 代码/文档事实：官方 `item_tbitem.json` 支持 `electric_energy`、售卖、图标、本地化、来源；研究笔记确认 base highest fuel 为 pumpkin 1200。当前 OilMod 已使用 `crude_oil`，GameBridge 煤矿掉落通过 native backpack placement。
- Codex 推断：本轮应确认 `crude_oil` 是 active id，旧 `dtmapi_oil` 只出现在 legacy docs；Y 控制台分类/图标/售卖/燃料值要重测。
- 验收点：第三存档查询/give `crude_oil`，煤矿命中可掉落，fuelEnergy > 当前最高基础燃料，Mine output pool 含 oil，CSV/log/source index 正确。
- blocker 判定：如果官方 drop extension 无法精确煤矿，保留 GameBridge exact coal hook，但必须记录它不是纯 JSON。

## 问题 6：矿井 Mod 与 DTMAPI Machine API

原始反馈：
- 新增“矿井”，复用水井模型但 2 倍显示，不替换水井。
- 二级合成台制作。
- 燃料容量大；支持耗电模式耗电 10。
- 纯燃料模式消耗快，耗电模式燃料消耗小。
- 按游戏时间周期产矿；兼容本体矿物和模组矿物、概率、默认概率与配置覆盖。

审查记录：
- 代码/文档事实：当前 MineMod 已有 official JSON shell、2x scale containment、Machine API、electric component path；但近期 0.2.8/0.2.9 docs also say Mine was tightened to electric-only in a previous user follow-up.
- Codex 推断：本轮用户明确重新要求 hybrid fuel/electric mode；需要审慎恢复/确认 Machine API 是否支持 fuel+electric without regressing official electric integration. 独立 goal 文件必须记录此目标，不能沿用旧 electric-only ledger。
- 验收点：官方 JSON 不替换 well；placement/preview 2x；二级工作台 recipe；fuel-only 和 electric mode 都有明确消耗差异；electric uses official power cost 10；生产按 native time catch-up；output pool 包含 base minerals and mod minerals with resolved probabilities。
- blocker 判定：若 hybrid mode 与官方 electric component 无法同时安全工作，应保持未完成并报告 exact API/official component conflict。

## 问题 7：更多装备栏位 Mod

原始反馈：
- 扩展玩家装备 UI。
- 默认栏位保留外显装饰效果。
- 额外栏位只提供属性，不造成外观冲突。
- 禁用/卸载 mod 时把额外栏位装备安全卸下或恢复，不允许静默删除。

审查记录：
- 代码/文档事实：当前 `IEquipmentSlotsApi` 已支持 native-like cloned slots、attribute-only hats/passives、dirty state and save transaction flush；0.2.9 记录了 no-save dirty 不落盘、native save 后 persist。
- Codex 推断：本轮需要复测安全恢复，尤其是禁用/卸载后 orphan recovery；只验证 equip/recover 不够。
- 验收点：第三存档 extra slots 可点击/hover/equip/unequip；默认槽外观不受影响；额外帽子只给属性不换外观；不保存退出不复制；保存后重进存在；禁用/卸载恢复到背包/安全路径。
- blocker 判定：如果 mod 禁用后普通 mod 代码不加载而无法恢复，必须由 Core/GameBridge orphan recovery 兜底；否则目标不能 complete。

## 问题分组

- UI：牧铃首帧、ConfigMenu `+` 输入框、AutoFishing/ActionSpeed inline rows、Y console save/CSV、SecondMotor visual, equipment extra slots。
- GameBridge/Hook：AnimalViewer prefix/postfix、Fishing phase hooks、native save/teleport、Motor transition, Oil coal drop, Machine production/electric/fuel, Equipment slot save/recovery。
- 官方内容 JSON：Oil item, Mine equipment/recipe/workbench route; official JSON is source, GameBridge owns fragile behavior.
- 测试/证据：必须有本轮 Release build/unit、第三存档 smoke、logs/screenshots/CSV/process check。

## 边界与 blocker

- 必须做：使用独立 goal 文件指定的目标版本；更新 goal 文件、docs/updates、docs/debug、smoke matrix、hook map、API matrix；第三存档真实验证。注意：后续已确认 `0.3.1` 是 Codex 自动升版错误，不能作为版本规则模板。
- 禁止做：不把旧 smoke 当新完成；不复制旧实现；不把普通 mod 放进 `BepInEx/plugins`；不直接暴露反编译类型到 public API；不静默删除玩家物品。
- blocker：第二摩托独立实例/地图切换、石油 official content + coal hook、矿井 hybrid machine behavior、更多装备栏安全恢复任一无法验证，则 goal 保持未完成。
