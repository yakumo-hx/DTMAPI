# DolocTown Motor / Vehicle API Research

## 0.8.3 Implementation Result

DolocTown SMAPI 0.8.3 implements the recommended experimental boundary for new vehicle work:

- Official Workshop data mods still only support replacing the existing motor's icon/body/light-mask assets.
- SMAPI now exposes `helper.Experimental.Vehicles` for a second-motorcycle prototype: register a vehicle, bind the built-in motor, clone the built-in motor controller, attach an external instance, summon/dismiss, force dismount, set position, apply movement policy, set/recharge power, and receive mounted/dismounted/summoned/dismissed/position/power events.
- SMAPI now exposes `helper.Experimental.Inventory` and expanded `helper.Experimental.Resources` so vehicle-like mods can consume batteries/summon items and scan/apply tool hits to nearby resources.
- The Runtime can suppress vertical motor input through `VehicleMovementPolicy`, supports summon cost and per-second riding drain through `VehiclePowerPolicy`, and emits `VehiclePowerChanged`.
- This remains experimental: multi-vehicle save storage, cloned-controller hot unload, vehicle UI lists, and official content-pack vehicle definitions are not stable 1.0 features yet.

记录时间：2026-05-17

目标：确认“载具 mod”在当前官方 Workshop 能力和 DolocTown SMAPI 能力下分别能做到什么，并为下一版 SMAPI 的载具接口设计留下可接手依据。

## 官方 Workshop 文档结论

本地抓取文档：

- `E:\Python_project\DLK\research\创意工坊说明\飞书抓取_20260517\md\022_09载具_ApFewNFGeie7OYkImngcrxyQn2b.md`
- 图片资源：
  - `icon_vehicle_motor`
  - `sprite_vehicle_motor`
  - `sprite_vehicle_motor_light_mask`

官方文档当前开放的是“现有摩托外观替换”：

- 替换载具图标：`icon_vehicle_motor`
- 替换载具本体贴图：`sprite_vehicle_motor`
- 可选替换车灯遮罩：`sprite_vehicle_motor_light_mask`
- 摩托贴图需要同名 JSON 指定锚点，否则显示位置可能不正确。
- 车灯遮罩锚点需要与载具锚点一致。

这说明官方 Workshop 目前支持的是单一内置载具的皮肤层，不是新增载具类型、载具行为、载具物品、载具控制器或载具存档结构。

## 反编译事实

目标程序集：

- `D:\Steam\steamapps\common\Doloc Town\DolocTown_Data\Managed\Assembly-CSharp.dll`
- 当前 SHA256：`247ACDAA8D6ED0490A406C6FC8EB035F957D0EC1AE3F036F10E2092B8AC10367`

游戏内部确实有完整的摩托系统，核心类型如下。

### 存档与解锁

`DolocTown.GameData.MotorDataManager`

- 字段/属性：
  - `isUnlocked`
  - `roomId`
  - `dungeonName`
  - `position`
  - `CurrentRoom`
- 方法：
  - `UnlockMotor()`
  - `UpdateMotorRoom(Room room)`
  - `OnExitRoom()`
  - `OnEnterRoom()`
  - `AfterLoadData()`

`DolocTown.GameData.AgentArchiveData` 持有：

- `MotorDataManager motorData`

`DolocTown.GameData.ArchiveOperationGlobal` 暴露：

- `UnlockMotor(ArchiveDataHandle handle)`
- `UpdateMotorRoom(ArchiveDataHandle handle, Room room)`
- `IsMotorUnlocked(ArchiveDataHandle handle)`

### 全局 API

`DolocAPI` 已有摩托相关入口：

- `MotorController Motor { get; set; }`
- `bool IsMotor(Collider2D other)`
- `bool GetMotorDirToAgentInMap(out Vector2 dir)`
- `bool GetMotorPosInMap(out Vector2 pos)`
- `void UnlockMotor(float offset)`
- `void SetMotorPosition(Room room, Vector2 position)`
- `void ResetMotorStatus()`
- 控制台命令：
  - `Command_UnlockMotor(float yOffset)`
  - `Command_SetMotorPosition(float x, float y)`

`DolocAPI.UnlockMotor(float offset)` 做了三件事：

1. 如果已解锁则返回。
2. 写入存档解锁状态。
3. 显示摩托，并把摩托放在玩家当前位置加 `yOffset`。

`DolocAPI.SetMotorPosition(Room room, Vector2 position)` 做了：

1. 未解锁则返回。
2. 如果 room 为空，则使用当前房间。
3. 更新存档里的摩托所在房间。
4. `Motor.Reset()`。
5. 设置摩托世界坐标。
6. 只有摩托所在房间等于当前房间时才显示。

`DolocAPI.ResetMotorStatus()` 在数据已加载且摩托已解锁时调用 `AgentControllerState.GetOffIfRiding()`。

### 摩托控制器

`DolocTown.MotorController : DolocObject`

关键字段：

- 物理/控制：
  - `Rigidbody2D rb`
  - `accelerationX`
  - `accelerationRX`
  - `decelerationX`
  - `maxSpeedX`
  - `gravity`
  - `maxDropSpeed`
  - `groundRayLength`
  - `naturalJumpAccelerationRange`
  - `naturalJumpThreshold`
  - `maxNatureJumpSpeed`
  - `pushJumpAcceleration`
  - `maxPushJumpSpeed`
- 耐力：
  - `enduranceDuration`
  - `enduranceRecoveryRate`
  - `currentEndurance`
  - `ProgressCircle enduranceProgressCircle`
- 状态：
  - `IsRiding`
  - `isFlying`
  - `isFollowing`
  - `currentVelocityX`
  - `currentVelocityY`
  - `currentInputX`
  - `currentInputY`
- 渲染/交互：
  - `MotorDriverRenderer driverRenderer`
  - `MotorRenderer motorRenderer`
  - `ScannerGate scannerGate`
  - `ScannerInteractableOfMotor scannerInteractable`
  - `MotorInteractable motorInteractable`
  - `MotorLight motorLight`

关键方法：

- `Control(float inputX, float inputY)`
- `SetIsRiding(bool value)`
- `AutoFlyTo(Func<Vector2> positionGetter, Action callback)`
- `StopFollowing()`
- `RecordVelocity()`
- `RestoreVelocity()`
- `PauseRigidbody()`
- `ResumeRigidbody()`
- `ClearVelocity()`
- `CheckVelocityAbs(float threshold)`
- `CheckInputAbs(float threshold)`
- `UpdateVelocityX(float deltaTime)`
- `UpdateVelocityY(float deltaTime)`
- `UpdateVelocityYWhileNoInput(float deltaTime)`
- `CostEndurance(float deltaTime)`
- `RecoveryEndurance(float deltaTime)`
- `GetDstToGround()`
- `SetVisible(bool visible)`
- `Reset()`
- `UpdatePerTU()`
- `OnFixedUpdate(float deltaTime)`
- `OnCollisionEnter2D(Collision2D collision)`

`MotorController.SetIsRiding(bool)` 会切换骑乘状态、切 layer、显示/隐藏扫描器和耐力 UI、切换驾驶员渲染、切 tag、开关隐藏触发器、清速度、重置旋转、同步灯光骑乘状态。

`MotorController.OnFixedUpdate(float)` 根据 `IsRiding` 更新速度，并把 `currentVelocityX/currentVelocityY` 写回 `Rigidbody2D.velocity`。

### 交互与玩家状态

`DolocTown.MotorInteractable : DolocObject`

- `OnInteract()` 调用：
  - `GameStateManager.agentController.GetOnMotor()`
- `OnTouch()` / `OnDisTouch()` 负责触碰提示/高亮。
- `OnSwordAttack()` 可处理对摩托的攻击交互。

`DolocTown.AgentControllerState`

- `GetOnMotor()`
- `GetOffMotor()`

`GetOnMotor()` 做了：

1. `IsRidingNow = true`
2. 清理 gate/building/room scanner buffer
3. 隐藏玩家 body
4. `motorController.SetIsRiding(true)`
5. 把玩家帽子渲染信息交给 `MotorDriverRenderer`
6. 摄像机跟随摩托
7. 无人机跟随摩托 `DroneFollowPoint`
8. 取消当前选中物品
9. 清理场景操作提示

`GetOffMotor()` 做了：

1. `IsRidingNow = false`
2. `motorController.SetIsRiding(false)`
3. 显示玩家 body
4. 把玩家位置设为摩托位置
5. 切入 `AgentStateDrop`
6. 隐藏工具渲染
7. 摄像机跟回玩家
8. 无人机跟回玩家
9. 更新存档里的摩托房间
10. 清理场景操作提示

### 物品入口

`DolocTown.ItemMotorKey : Item`

- `OnUseAsTool()`
- `OnUseAsItem()`
- `OnUse()`
- `GetMotorFlyPosition()`
- `_GetDirToAgent()`
- `IsSame(Item other)`

`OnUse()` 主要逻辑：

1. 未解锁时输出“摩托还未解锁”。
2. 当前房间为空则返回。
3. 在屋内或禁用摩托的房间中使用，会显示不能召唤摩托。
4. 播放召唤音效。
5. 如果摩托已经在当前房间且距离玩家小于约 `1.5`，则返回。
6. 根据玩家位置和屏幕边缘计算飞来目标点。
7. 如摩托已经在当前房间且目标点更近，则先设置摩托位置。
8. 调用 `MotorController.AutoFlyTo(GetMotorFlyPosition, null)`。
9. 更新存档里的摩托房间。

`DolocTown.Config.Item.ItemFunctionMotorKey` 表明道具系统中存在 `MotorKey` 类型函数。

## 当前能做什么

### 1. 官方 Workshop 原生能力

可以做：

- 摩托图标替换。
- 摩托本体贴图替换。
- 摩托灯光遮罩替换。
- 通过锚点 JSON 调整贴图显示位置。

不能直接做：

- 新增第二种载具。
- 给载具增加新数值字段。
- 新增载具控制器。
- 新增骑乘状态机。
- 新增载具存档槽。
- 新增载具 UI 列表。
- 新增载具物品和解锁链路。

### 2. 当前 DolocTown SMAPI 可做的载具类 mod

可以稳定尝试的方向：

- 解锁/召唤现有摩托。
- 调整现有摩托速度、加速度、重力、耐力、恢复速度等字段。
- 拦截 `MotorController.Control` 或 `OnFixedUpdate` 改控制手感。
- 拦截 `ItemMotorKey.OnUse` 改召唤规则。
- 在 UI 中显示当前摩托位置、房间、是否骑乘、耐力等信息。
- 给现有摩托做“皮肤 + 数值包”的混合 mod。

谨慎尝试的方向：

- 复制现有 `game_entity_motor` GameObject 作为第二辆摩托。
- 复用 `MotorController` 但给它不同贴图/数值。
- 通过自定义存档 JSON 保存额外载具位置。

高风险方向：

- 真正注册新的载具类型。
- 修改官方存档结构以支持多个载具。
- 替换 `DolocAPI.Motor` 单例语义。
- 让官方 UI `MotorBar` 显示多辆载具。
- 在不同房间/地牢间维护多载具生命周期。

## 建议的 SMAPI 0.4 载具路线

先不要把接口命名为泛化的 `IVehicleHelper`，因为当前反编译事实显示游戏内部是单一 `Motor` 系统。建议第一阶段命名为 `IMotorHelper`，等确认可稳定支持多载具后再提升为 `IVehicleRegistry`。

### 0.4 第一阶段：Motor API

SDK 草案：

```csharp
public interface IMotorHelper
{
    bool IsUnlocked { get; }
    bool IsRiding { get; }
    string? RoomId { get; }
    Vector2? Position { get; }

    void Unlock(float yOffset = 0f);
    bool TrySummonToCurrentRoom(Vector2 position);
    bool TryGetController(out object controller);
    bool TrySetTuning(MotorTuning tuning);
}

public sealed class MotorTuning
{
    public float? MaxSpeedX { get; set; }
    public float? AccelerationX { get; set; }
    public float? DecelerationX { get; set; }
    public float? Gravity { get; set; }
    public float? EnduranceDuration { get; set; }
    public float? EnduranceRecoveryRate { get; set; }
}
```

Runtime 实现可以先用反射包住：

- `DolocAPI.UnlockMotor(float)`
- `DolocAPI.SetMotorPosition(Room, Vector2)`
- `DolocAPI.Motor`
- `MotorController.IsRiding`
- `MotorController` tuning 字段
- `ArchiveDataHandle.farmData.agentData.motorData`

### 0.4 第二阶段：Motor events

增加事件：

- `MotorRidingStarted`
- `MotorRidingStopped`
- `MotorSummoned`
- `MotorPositionChanged`
- `MotorTuningApplying`

可通过 Harmony patch：

- `AgentControllerState.GetOnMotor`
- `AgentControllerState.GetOffMotor`
- `DolocAPI.SetMotorPosition`
- `ItemMotorKey.OnUse`

### 0.5+：Vehicle Registry 研究

只有满足以下条件后，才适合承诺“新增一种载具”：

- 能稳定复制或加载 `game_entity_motor` prefab。
- 能稳定替换 `MotorRenderer` / `MotorDriverRenderer` / `MotorLight` 资源。
- 有自定义存档系统保存多辆载具。
- 有选择/召唤多辆载具的 UI。
- 明确如何处理 `DolocAPI.Motor` 单例。
- 明确如何处理摄像机、无人机、玩家 body、碰撞层和 tag。
- 禁用 mod 后不会把玩家留在无效骑乘状态。

## 结论

“多洛可有载具”是对的，而且底层比官方 Workshop 文档开放的内容多很多。  

当前最靠谱的推进方式不是直接承诺“新增载具”，而是先把现有飞行摩托做成 SMAPI 的第一个领域 API：

1. `IMotorHelper`：解锁、召唤、位置、状态、数值调节。
2. `Motor events`：上车、下车、召唤、位置变化。
3. `Motor tuning mod` 示例：证明开发者可以用 SMAPI 改现有载具行为。
4. 再研究 `VehicleRegistry`：复制/注册第二辆载具、独立存档、UI 选择。

这样路线更像 Stardew 生态：SMAPI 先提供稳定入口和生命周期，复杂领域功能再由专门框架或高级 mod 承担。
