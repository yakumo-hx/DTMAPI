# DebugConsole 1.00 原生责任与方法体审查

## 记录信息

- 日期：`2026-08-04`
- 状态：`recorded`
- 性质：DTMAPI 0.6.0 DebugConsole native responsibility / method-body / compatibility 审查
- Source：[DTMAPI 0.6.0 唯一权威路线图](../../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)要求先审查 `CostItemAt`、天气、输入、移动与科技点，再实施确定差异
- Implementation owner：[DTMAPI 0.6.0 唯一权威路线图](../../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)
- Native baseline：本地私有 `24456188_test_E861E0`，`Assembly-CSharp.dll` 长度 `6,384,128`，SHA-256 `E861E07E3CB82A6A21EEFA292456452F5AD12C25EC57972A59762AD3F3530923`
- Previous comparison baseline：本地私有 `23762374_public_C416D4`

本 Review 只保存原生 owner、方法体比较、必要修正与验收边界。产品实现生命周期、验证结果和发布状态仍由 owning Update 管理；不复制或分发官方程序集、反编译源码或资源。

## 1. 审查结论

1. `DolocAPI.CostItemAt` 已从产品安装器假设的二参数目标变为一个四参数方法；ProductNative 与冻结 ABI 使用的可选 Compatibility Host 都必须解析同一个四参数目标，19-Hook/15-creative 事务规模不变。
2. 天气按钮应只调用当前房间的官方私有静态二参数 `DolocAPI.Command_SetWeather(string,bool)`；产品不能自己重写 `SeasonGroupId`、`SetWeather`、`PatchWeather` 或 `CurrentWeatherKey` 逻辑，调用后只以 `LocalWeatherType` 回读核验。
3. `UseTool(bool)` 与 `UseItem(bool)` 在两个基线的方法体相同。`EnterUICheck` 只有原生菜单分支从直接写 `HorizontalMoveFactor=0` 改为 `ClearHorizontalMoveFactor()`；DebugConsole Prefix 只在自有 modal/Escape drain 激活时跳过整个 native menu owner，并返回已消费，不应复制该 native menu 分支。门不激活时原生方法完整执行。
4. `BodyController.MoveSpeed` 两版都由 native `MotionAbility.MoveSpeed` 加装备增量组成，仅成员命名大小写改变。产品 Postfix 乘最终 `__result`，没有读取、写入或恢复 `MoveScaler`，因此无需重放原生速度计算。
5. 科技按钮的产品语义是固定对 `NATURE`、`OPERATE`、`SCIENCE`、`ANIMAL` 依次各调用一次官方 `AddTechPoint(...,100)`；不枚举“第一个”类别，不加入 `BATTLE`/`FISHING`，某一类别失败也不能阻止其余类别被尝试。
6. 怪物、发电机、资源及 `targetRoom` 不进入本次修复。其 action helper 为冻结兼容调用保留，但 UI 无接线，Entry 不解析世界动作 native command。

## 2. `CostItemAt` 与事务所有权

当前精确签名是：

`bool DolocAPI.CostItemAt(int position, int count, bool useBox=false, bool shouldEqualAsItem=false)`

方法体把四个参数原样交给 `InventorySystem.CostAt`。DebugConsole 的 Creative/no-cost 安装器仍先解析全部十九个目标，随后以单一 ProductNative Harmony owner 原子安装；任何目标缺失都会回滚全部目标。可选 Compatibility Host 的 creative demand 仍是十五目标事务，冻结 0.3.1 公共 ABI 不变，只把其当前游戏原生目标签名同步为 `/4`。这避免产品和旧 Host 在 1.00 上分别观察不同或不存在的 method identity。

## 3. 天气 owner

正式版二参数官方命令自行：

1. 解析所选 weather ID；
2. 取得 `archiveHandle.currentRoom.RoomInfo.SeasonGroupId`；
3. 调用 `SetWeather(seasonGroupId, weather, shouldRender:true)`；
4. `patch=true` 时以当前 `DateNow.CurrentWeatherKey` 调用 `PatchWeather`。

同名三参数 overload 是全局区域命令，不是本产品按钮的 owner。产品因此用精确参数类型反射私有静态二参数 overload，传所选 ID 与 UI 的 `patchCurrentPeriod=true`，再比较调用前后的 `LocalWeatherType`。命令缺失、调用异常或回读不等于请求值都 fail-closed；产品不调用 `WeatherRegulator`，也不复制 group/key 写入。

天气页面的读路径也随原生 owner 更新：当前天气来自 `LocalWeatherType`，季节与当日预报按当前房间 `SeasonGroupId` 调用 `TimeArchiveData.GetSeasonInfo(string)` 和 `GetWeatherInfoOfDay(string,int)`。

## 4. 输入与移动方法体分类

`UseTool`/`UseItem` 的精确方法体比较为相同，因而不写“已确认漂移”或增加 replay。`EnterUICheck` 的 clear API 变化只属于 native `GlobalToggleMenu` 分支：DebugConsole 激活时 Prefix 的职责是阻止关闭自身 UI 的 Escape/Y 边沿继续打开 native menu；不激活时 Prefix 返回 `true`，新 native clear 方法照常执行。复制 clear 行会把 native menu 责任带入产品并可能在自有 modal close 时额外改变玩家状态，因此不实施。

移动路径继续在 `BodyController.get_MoveSpeed()` 最终结果上做 current-player-only Postfix。这样 native `MotionAbility`、terrain 与装备 Buff 先完成计算，DebugConsole 只叠乘自己的临时因子；显式 1x、存档载入、返回标题、停用和 owner cleanup 仍只清产品因子。

## 5. 策略与兼容历史

DebugConsole 是本轮实际改源码的产品，因此从当前 admission 切到 `doloctown-24456188-debugconsole-v1`；产品版本仍为 `1.0.0`，最低 DTMAPI 仍为 `0.5.5`，没有无权新增 `1.0.1` 或抬高最低版本。

旧 `doloctown-23762374-debugconsole-v1` policy 与 compiler surface 的精确字节分别以 SHA-256 `483B48BC...C165`、`BDAEE793...2F7D` 移入 inert `author-sdk/advanced-reference-policies/history/`。它们不再是当前唯一 UniqueID admission，也不进入 SDK/Doctor 分发；旧 package/receipt 与 Git 历史仍是审计材料。没有新建 receipt、schema、builder 或第二套 assurance 家族。

## 6. 可重放门与剩余验收

[`test-dtmapi-060-debugconsole-native-trace.ps1`](../../../../../tools/scripts/test-dtmapi-060-debugconsole-native-trace.ps1)绑定两个 reverse baseline、当前程序集 identity、原生方法体、产品/Host 目标、UI/Entry 排除项、当前 policy/Catalog/author binding 与旧 policy 精确历史哈希。Unit 另执行天气成功/回读不一致失败，以及科技点中途失败仍完成四类调用的反证。

当前结论是 `implemented, not player-verified`。仍需在共享 Runtime lock 下对 SDK 生成的当前包做最小聚焦游戏重放：产品 19-Hook 安装、Creative/no-cost 全有或全无、指定天气及回读、四类科技点、输入关闭/标题清理、移动因子与 clean exit。若冻结 0.3.1 DebugConsole 仍作为实际 retained consumer 进入兼容重放，则可选 Host 的 `/4` creative topology 也必须验证；不能用本 Review 或静态 trace 冒充玩家证据。
