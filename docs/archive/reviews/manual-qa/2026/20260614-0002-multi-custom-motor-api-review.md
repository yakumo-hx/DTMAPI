# 2026-06-14 Multi Custom Motor API Rebuild Review

时间：2026-06-14 03:40:16 +08:00

来源：用户手测规划与两张背包/装备界面截图：`codex-clipboard-be72680b-e5f3-405b-a923-2d327bfd38b8.png`、`codex-clipboard-47d13b3b-65d4-4cf4-8e5b-e5fcfca2f6d2.png`。

范围：为“稳定多辆自定义摩托 API + 第二辆载具 mod 重做”创建实现前审查和目标约束。当前记录不证明实现完成。

禁止事项：不复制旧 DLKsmapi / DLK_SecondMotor；不把普通 DTMAPI mod 放入 `BepInEx/plugins`；不通过邮件发放新摩托钥匙；未完成 native-owner 方法体审查前，不得用 mod 层补丁冒充稳定多车 API；不污染当前 `Refactor` 主工作树未提交改动。

审查记录：本文件为 durable manual-QA/API rebuild review，供 `docs/goals/2026/20260614-0001-multi-custom-motor-api.md` 使用。

## 问题 1：稳定多辆自定义摩托 API 与第二辆摩托 mod 重做

原始反馈：
- 接下来要重做 API、重做 mod，尝试给出稳定的多辆自定义摩托。
- 创建全新分支或工作树实现。
- 第一个 mod 完全复刻官方原生摩托行为，但必须完全独立。
- 钥匙优先走官方 JSON 添加，在官方电话亭售卖，不再走邮件发放。
- 就算多个钥匙，也绑定同一辆摩托。
- 与第一辆摩托不同的是，使用官方拓展 mod 里面的摩托贴图独立替换这个 mod。
- 未来拓展方向包括：完全不同碰撞箱、完全不同贴图模式（例如大型机甲）、不同运动形式（地面行走、低高度差爬升、飞行改成长按蓄力高跳）、额外功能（类似星露谷拖拉机范围采集资源、不同攻击方式和攻击动画）。
- 测试用第八存档：当前停留在农场比较左侧、清空杂物，向左运动 3 秒左右跨图。进入存档手格子为空、主动道具为空、身上没有携带官方摩托钥匙；原地给予钥匙并左键装上、再左键召唤、E 键上车即可。过去 hook 脚本容易传送到难观察的位置或直接卡住。
- 第九存档未解锁载具。其他条件与第八存档基本一致，只是不在地图最左侧，在中间位置。
- 额外注意背包：当前背包很可能反映了官方强绑定摩托与角色。

图片转写：
- 图 1：角色装备界面显示玩家在后世 32 年 01 月 22 日，金钱 210696G。无人机栏显示“未装备无人机”，载具栏显示“飞行摩托 / 多洛可河谷”。左下背包第一格为空，背包里可见工具、矿物/瓶装物品、材料以及一个类似摩托/载具图标的物品。画面符合“第八存档已有官方载具状态、但测试入口需要手格子/主动道具为空”的说明。
- 图 2：角色装备界面显示玩家在后世 31 年 01 月 01 日，金钱 300G。无人机栏显示“未装备无人机”，载具栏显示“未解锁载具”。角色装备有普通物品，画面符合“第九存档未解锁载具，用于验证自定义摩托不依赖官方已解锁载具”的说明。

审查记录：
- 用户确认事实：用户要求从 API 和 mod 双层重做；第一辆自定义摩托要复刻官方摩托行为但完全独立；钥匙发放要改为官方 JSON + 电话亭售卖；第八存档和第九存档分别作为已解锁/未解锁载具状态下的真实测试夹具。
- 截图/日志观察：第八存档可见官方载具状态，第九存档明确显示未解锁载具；两张截图都能作为后续烟测脚本选择存档和解释失败原因的人工基线。
- 代码/文档事实：既有 `IMotorVehicleApi` 仍在 `docs/api/public-api-matrix.md` 标为 `Experimental`；`docs/reviews/api/2026/20260607-0009-native-owner-special-audits/06-vehicle-motor-api.md` 说明当前第二摩托是围绕 `DolocAPI.Motor` 单例的 DTMAPI clone/routing；`docs/debug/issues/ISSUE-005-20260603-manual-qa-024-regressions.md` 与 `ISSUE-006-20260604-critical-manual-qa-025.md` 记录过贴图污染、跨地图状态泄漏、跨存档残留和禁用后空邮件问题。
- Codex 推断：稳定多车 API 的难点不在“复制一个 `MotorController` 能跑”，而在把官方单例摩托的状态、钥匙、装备栏、召唤、骑乘、过图、贴图、禁用/取消订阅、存档隔离拆成 DTMAPI 可拥有的 per-vehicle lease/runtime/state。第九存档是关键反证：如果自定义摩托仍依赖官方摩托解锁或角色装备槽绑定，就无法视为独立。
- 反证/未证实：尚未重新审查当前反编译 build 的 `MotorController`、`MotorDataManager`、`ItemMotorKey.OnUse`、`AgentControllerState.GetOnMotor/GetOffMotor`、`DolocAPI.SetMotorPosition/EnterRoom`、装备栏载具槽和电话亭商店加载路径的方法体。尚未证明官方 JSON 能把自定义钥匙稳定放入电话亭且不触发原生单例摩托绑定。尚未证明多个钥匙不会生成多个车辆实例。
- 归属：GameBridge MotorVehicle API、DTMAPI public abstractions、SecondMotorMod ordinary mod、official-local content packaging、smoke harness 第八/第九存档夹具。
- 需要更新：API native-owner review 或 goal 内 method-body section、`docs/api/public-api-matrix.md`、`docs/hook-map/README.md`、`docs/debug/regressions/smoke-matrix.md`、`docs/updates/INDEX.md` 与本轮 update record；如果 API 状态变化，还需要开发者说明/迁移说明。
- 验收点：第八存档原地给新钥匙、左键装上/使用、左键召唤、E 上车、向左跨图约 3 秒，证明自定义摩托可召唤、可骑、可跨图、原生摩托状态不被拖动或贴图污染；第九存档未解锁官方载具时仍可购买/给予自定义钥匙、绑定同一辆自定义摩托、召唤/骑乘/跨图，不解锁或污染官方摩托。
- blocker 判定：如果找不到官方电话亭售卖 content owner、装备栏载具槽与钥匙绑定 owner、或无法隔离 `DolocAPI.Motor` 单例状态，则不得宣称 stable multi-motor；应保留或降级为 experimental/internal，并报告具体 native-owner blocker。

## 测试反馈理解

- 用户确认事实：第八存档是左侧农场跨图真实测试夹具；第九存档是未解锁载具夹具；过去 hook 脚本因为传送位置不好或卡住而不能作为可靠验收。
- 截图/日志观察：第八存档有官方飞行摩托状态，第九存档没有官方载具解锁状态。
- Codex 推断：本轮烟测必须禁止把车辆脚本传送到“方便但不真实”的点位；应使用真实输入路径和当前位置。第九存档应成为“官方摩托未解锁也不阻塞自定义摩托”的强验证。

## 问题分组

- API：从 `IMotorVehicleApi` 实验 clone API 重做为清晰分层的 registration/content/key/summon/ride/transition/runtime lease。
- Hook/GameBridge：审查并重建原生钥匙、装备栏、召唤、骑乘、过图、位置、渲染和清理边界。
- 官方/工坊兼容：新钥匙和售卖优先使用官方 JSON；不通过邮件；官方拓展 mod 摩托贴图只作用于自定义摩托。
- 测试/证据：第八、第九存档替代旧传送式车辆烟测；必须记录截图/日志/退出残留。
- 文档：API matrix、hook map、smoke matrix、update record 必须同步。

## 边界约束

必须做：
- 新分支或 worktree 实现。
- native-owner 方法体审查先于 runtime 改动。
- 第一个自定义摩托 mod 复刻官方摩托行为并与官方摩托完全独立。
- 钥匙走官方 JSON/电话亭售卖路径，不走邮件。
- 多个钥匙绑定同一辆自定义摩托，不能生成多辆重复车。
- 自定义贴图只替换自定义摩托，不替换官方摩托。
- 第八和第九存档真实路径验证。

禁止做：
- 不能复制旧 DLKsmapi / DLK_SecondMotor。
- 不能把官方全局 `sprite_vehicle_motor*` 替换当成自定义摩托贴图方案。
- 不能只用烟测 helper 或 debug teleport 证明玩家可见路径。
- 不能在未证明单例隔离前提升为 Stable。

可选做：
- 为未来机甲/地面车/高跳/拖拉机式采集/攻击动作保留 DTO 扩展点，但本轮不实现这些未来功能。

blocker 判定：
- 原生 owner 不存在或无法安全接管时，目标必须降级并报告 blocker。
- 第八或第九存档夹具状态不符时，烟测应标记 blocked，而不是传送绕过。
