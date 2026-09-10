# Machines, Equipment, Save, Vehicle, Camera, Farming Symbol-Level Native Responsibility Audit

- Audit: `20260607-0006-native-responsibility-method-audit`
- Scope: public Abstractions symbols in this volume; docs-only review, no implementation goal.
- Locator rule: `Symbol` is the stable key; `Declaration` line numbers are secondary and may drift.

## Top Risks

- `IMachineProductionApi` is the highest-risk ordinary-mod surface: production loop/state are not fully native-owned.
- `IMotorVehicleApi` mixes original motor singleton and DTMAPI second-motor clone, creating shared-state and cross-room risks.
- `ICameraZoomApi` currently changes camera size without full background/depth-fog/camera-controller ownership.
- `IEquipmentSlotsApi` exposes native-like UI but persists DTMAPI sidecar state after native SaveGame only.
- `ISaveSlotsApi` reaches official save UI count but still lacks full create/load/delete/copy/restart matrix.

## Audit Blocks

<a id="sym-0852"></a>
### DTMAPI.Abstractions.IMotorVehicleApi

- Symbol: `DTMAPI.Abstractions.IMotorVehicleApi`
- Current marker: `experimental`
- Review advice: 保持 experimental；拆分 original native motor 与 second/custom clone 语义
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:116`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:17`
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:84 `IMotorVehicleApi` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:17
- Result: Gap
- Recommendation: 普通 mod 依赖会污染原生 motor singleton 与 DTMAPI clone 状态；风险是跨房间失效、原车/副车共享状态污染、外观或骑乘状态残留。

<a id="sym-0853"></a>
### DTMAPI.Abstractions.IMotorVehicleApi.VehicleChanged

- Symbol: `DTMAPI.Abstractions.IMotorVehicleApi.VehicleChanged`
- Current marker: `experimental`
- Review advice: 保持 experimental；拆分 original native motor 与 second/custom clone 语义
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:118`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:127`
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:84 `IMotorVehicleApi` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:127
- Result: Gap
- Recommendation: 普通 mod 依赖会污染原生 motor singleton 与 DTMAPI clone 状态；风险是跨房间失效、原车/副车共享状态污染、外观或骑乘状态残留。

<a id="sym-0854"></a>
### DTMAPI.Abstractions.IMotorVehicleApi.GetOriginalMotorState()

- Symbol: `DTMAPI.Abstractions.IMotorVehicleApi.GetOriginalMotorState()`
- Current marker: `experimental`
- Review advice: 保持 experimental；拆分 original native motor 与 second/custom clone 语义
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:120`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4813`
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:84 `IMotorVehicleApi` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4813
- Result: Gap
- Recommendation: 普通 mod 依赖会污染原生 motor singleton 与 DTMAPI clone 状态；风险是跨房间失效、原车/副车共享状态污染、外观或骑乘状态残留。

<a id="sym-0855"></a>
### DTMAPI.Abstractions.IMotorVehicleApi.GetVehicleState(string vehicleId)

- Symbol: `DTMAPI.Abstractions.IMotorVehicleApi.GetVehicleState(string vehicleId)`
- Current marker: `experimental`
- Review advice: 保持 experimental；拆分 original native motor 与 second/custom clone 语义
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:121`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4818`
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:84 `IMotorVehicleApi` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4818
- Result: Gap
- Recommendation: 普通 mod 依赖会污染原生 motor singleton 与 DTMAPI clone 状态；风险是跨房间失效、原车/副车共享状态污染、外观或骑乘状态残留。

<a id="sym-0856"></a>
### DTMAPI.Abstractions.IMotorVehicleApi.GetVehicles()

- Symbol: `DTMAPI.Abstractions.IMotorVehicleApi.GetVehicles()`
- Current marker: `experimental`
- Review advice: 保持 experimental；拆分 original native motor 与 second/custom clone 语义
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:122`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4835`
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:84 `IMotorVehicleApi` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4835
- Result: Gap
- Recommendation: 普通 mod 依赖会污染原生 motor singleton 与 DTMAPI clone 状态；风险是跨房间失效、原车/副车共享状态污染、外观或骑乘状态残留。

<a id="sym-0857"></a>
### DTMAPI.Abstractions.IMotorVehicleApi.RegisterSecondMotor(IManifest owner, SecondMotorOptions options)

- Symbol: `DTMAPI.Abstractions.IMotorVehicleApi.RegisterSecondMotor(IManifest owner, SecondMotorOptions options)`
- Current marker: `experimental`
- Review advice: 保持 experimental；拆分 original native motor 与 second/custom clone 语义
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:123`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4885`
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:84 `IMotorVehicleApi` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4885
- Result: Gap
- Recommendation: 普通 mod 依赖会污染原生 motor singleton 与 DTMAPI clone 状态；风险是跨房间失效、原车/副车共享状态污染、外观或骑乘状态残留。

<a id="sym-0858"></a>
### DTMAPI.Abstractions.IMotorVehicleApi.UnlockOriginalMotor(IManifest owner, double yOffset)

- Symbol: `DTMAPI.Abstractions.IMotorVehicleApi.UnlockOriginalMotor(IManifest owner, double yOffset)`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:124`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4890`
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:84 `IMotorVehicleApi` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4890
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0859"></a>
### DTMAPI.Abstractions.IMotorVehicleApi.SummonOriginalMotor(IManifest owner)

- Symbol: `DTMAPI.Abstractions.IMotorVehicleApi.SummonOriginalMotor(IManifest owner)`
- Current marker: `experimental`
- Review advice: 保持 experimental；拆分 original native motor 与 second/custom clone 语义
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:125`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4923`
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:84 `IMotorVehicleApi` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4923
- Result: Gap
- Recommendation: 普通 mod 依赖会污染原生 motor singleton 与 DTMAPI clone 状态；风险是跨房间失效、原车/副车共享状态污染、外观或骑乘状态残留。

<a id="sym-0860"></a>
### DTMAPI.Abstractions.IMotorVehicleApi.SummonVehicle(IManifest owner, string vehicleId)

- Symbol: `DTMAPI.Abstractions.IMotorVehicleApi.SummonVehicle(IManifest owner, string vehicleId)`
- Current marker: `experimental`
- Review advice: 保持 experimental；拆分 original native motor 与 second/custom clone 语义
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:126`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4975`
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:84 `IMotorVehicleApi` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:4975
- Result: Gap
- Recommendation: 普通 mod 依赖会污染原生 motor singleton 与 DTMAPI clone 状态；风险是跨房间失效、原车/副车共享状态污染、外观或骑乘状态残留。

<a id="sym-0861"></a>
### DTMAPI.Abstractions.IMotorVehicleApi.RideVehicle(IManifest owner, string vehicleId)

- Symbol: `DTMAPI.Abstractions.IMotorVehicleApi.RideVehicle(IManifest owner, string vehicleId)`
- Current marker: `experimental`
- Review advice: 保持 experimental；拆分 original native motor 与 second/custom clone 语义
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:127`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:5034`
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:84 `IMotorVehicleApi` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:5034
- Result: Gap
- Recommendation: 普通 mod 依赖会污染原生 motor singleton 与 DTMAPI clone 状态；风险是跨房间失效、原车/副车共享状态污染、外观或骑乘状态残留。

<a id="sym-0862"></a>
### DTMAPI.Abstractions.IMotorVehicleApi.DismountVehicle(IManifest owner, string reason)

- Symbol: `DTMAPI.Abstractions.IMotorVehicleApi.DismountVehicle(IManifest owner, string reason)`
- Current marker: `experimental`
- Review advice: 保持 experimental；拆分 original native motor 与 second/custom clone 语义
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:128`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:5078`
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:84 `IMotorVehicleApi` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:5078
- Result: Gap
- Recommendation: 普通 mod 依赖会污染原生 motor singleton 与 DTMAPI clone 状态；风险是跨房间失效、原车/副车共享状态污染、外观或骑乘状态残留。

<a id="sym-0863"></a>
### DTMAPI.Abstractions.IMotorVehicleApi.GetStatus(string uniqueId)

- Symbol: `DTMAPI.Abstractions.IMotorVehicleApi.GetStatus(string uniqueId)`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:129`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:5088`
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:84 `IMotorVehicleApi` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:5088
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0864"></a>
### DTMAPI.Abstractions.IMachineProductionApi

- Symbol: `DTMAPI.Abstractions.IMachineProductionApi`
- Current marker: `experimental`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:133`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:17`
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:85 `IMachineProductionApi` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:17
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-0865"></a>
### DTMAPI.Abstractions.IMachineProductionApi.RegisterMachine(IManifest owner, MachineDefinition definition)

- Symbol: `DTMAPI.Abstractions.IMachineProductionApi.RegisterMachine(IManifest owner, MachineDefinition definition)`
- Current marker: `experimental`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:135`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:568`
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:85 `IMachineProductionApi` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:568
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-0866"></a>
### DTMAPI.Abstractions.IMachineProductionApi.GetMachines(string uniqueId)

- Symbol: `DTMAPI.Abstractions.IMachineProductionApi.GetMachines(string uniqueId)`
- Current marker: `experimental`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:136`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1174`
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:85 `IMachineProductionApi` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1174
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-0867"></a>
### DTMAPI.Abstractions.IMachineProductionApi.GetState(string uniqueId)

- Symbol: `DTMAPI.Abstractions.IMachineProductionApi.GetState(string uniqueId)`
- Current marker: `experimental`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:137`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1181`
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:85 `IMachineProductionApi` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1181
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-0868"></a>
### DTMAPI.Abstractions.IMachineProductionApi.GetStatus(string uniqueId)

- Symbol: `DTMAPI.Abstractions.IMachineProductionApi.GetStatus(string uniqueId)`
- Current marker: `experimental`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:138`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1194`
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:85 `IMachineProductionApi` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1194
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-0869"></a>
### DTMAPI.Abstractions.IEquipmentSlotsApi

- Symbol: `DTMAPI.Abstractions.IEquipmentSlotsApi`
- Current marker: `experimental`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:142`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:17`
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:86 `IEquipmentSlotsApi` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:17
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-0870"></a>
### DTMAPI.Abstractions.IEquipmentSlotsApi.RegisterSlots(IManifest owner, EquipmentSlotsOptions options)

- Symbol: `DTMAPI.Abstractions.IEquipmentSlotsApi.RegisterSlots(IManifest owner, EquipmentSlotsOptions options)`
- Current marker: `experimental`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:144`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1201`
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:86 `IEquipmentSlotsApi` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1201
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-0871"></a>
### DTMAPI.Abstractions.IEquipmentSlotsApi.GetSlots(string uniqueId)

- Symbol: `DTMAPI.Abstractions.IEquipmentSlotsApi.GetSlots(string uniqueId)`
- Current marker: `experimental`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:145`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2628`
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:86 `IEquipmentSlotsApi` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2628
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-0872"></a>
### DTMAPI.Abstractions.IEquipmentSlotsApi.EquipExtraSlot(IManifest owner, string slotId, string itemId)

- Symbol: `DTMAPI.Abstractions.IEquipmentSlotsApi.EquipExtraSlot(IManifest owner, string slotId, string itemId)`
- Current marker: `experimental`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:146`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2713`
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:86 `IEquipmentSlotsApi` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2713
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-0873"></a>
### DTMAPI.Abstractions.IEquipmentSlotsApi.UnequipExtraSlot(IManifest owner, string slotId, string reason)

- Symbol: `DTMAPI.Abstractions.IEquipmentSlotsApi.UnequipExtraSlot(IManifest owner, string slotId, string reason)`
- Current marker: `experimental`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:147`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2756`
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:86 `IEquipmentSlotsApi` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2756
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-0874"></a>
### DTMAPI.Abstractions.IEquipmentSlotsApi.GetState(string uniqueId)

- Symbol: `DTMAPI.Abstractions.IEquipmentSlotsApi.GetState(string uniqueId)`
- Current marker: `experimental`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:148`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2761`
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:86 `IEquipmentSlotsApi` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2761
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-0875"></a>
### DTMAPI.Abstractions.IEquipmentSlotsApi.RecoverExtraSlotItems(IManifest owner, string reason)

- Symbol: `DTMAPI.Abstractions.IEquipmentSlotsApi.RecoverExtraSlotItems(IManifest owner, string reason)`
- Current marker: `experimental`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:149`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2785`
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:86 `IEquipmentSlotsApi` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2785
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-0876"></a>
### DTMAPI.Abstractions.IEquipmentSlotsApi.GetStatus(string uniqueId)

- Symbol: `DTMAPI.Abstractions.IEquipmentSlotsApi.GetStatus(string uniqueId)`
- Current marker: `experimental`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:150`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2801`
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:86 `IEquipmentSlotsApi` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2801
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-0877"></a>
### DTMAPI.Abstractions.ISaveSlotsApi

- Symbol: `DTMAPI.Abstractions.ISaveSlotsApi`
- Current marker: `experimental`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:154`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:17`
- Native owner: DolocAPI.gameManager.archiveFileCount and official GameDataPanel save-slot rendering.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:87 `ISaveSlotsApi` (experimental)
  - update record: docs/updates/2026/20260606-0004-029-readme-implementation.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-051653
  - hook-map entry: docs/hook-map/README.md#hook-savemoreslotsapi
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:17
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0878"></a>
### DTMAPI.Abstractions.ISaveSlotsApi.RegisterSlots(IManifest owner, SaveSlotsOptions options)

- Symbol: `DTMAPI.Abstractions.ISaveSlotsApi.RegisterSlots(IManifest owner, SaveSlotsOptions options)`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:156`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1201`
- Native owner: DolocAPI.gameManager.archiveFileCount and official GameDataPanel save-slot rendering.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:87 `ISaveSlotsApi` (experimental)
  - update record: docs/updates/2026/20260606-0004-029-readme-implementation.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-051653
  - hook-map entry: docs/hook-map/README.md#hook-savemoreslotsapi
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1201
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0879"></a>
### DTMAPI.Abstractions.ISaveSlotsApi.GetState(string uniqueId)

- Symbol: `DTMAPI.Abstractions.ISaveSlotsApi.GetState(string uniqueId)`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:157`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1213`
- Native owner: DolocAPI.gameManager.archiveFileCount and official GameDataPanel save-slot rendering.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:87 `ISaveSlotsApi` (experimental)
  - update record: docs/updates/2026/20260606-0004-029-readme-implementation.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-051653
  - hook-map entry: docs/hook-map/README.md#hook-savemoreslotsapi
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1213
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0880"></a>
### DTMAPI.Abstractions.ISaveSlotsApi.GetStatus(string uniqueId)

- Symbol: `DTMAPI.Abstractions.ISaveSlotsApi.GetStatus(string uniqueId)`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:158`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1236`
- Native owner: DolocAPI.gameManager.archiveFileCount and official GameDataPanel save-slot rendering.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:87 `ISaveSlotsApi` (experimental)
  - update record: docs/updates/2026/20260606-0004-029-readme-implementation.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-051653
  - hook-map entry: docs/hook-map/README.md#hook-savemoreslotsapi
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1236
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0881"></a>
### DTMAPI.Abstractions.ICameraZoomApi

- Symbol: `DTMAPI.Abstractions.ICameraZoomApi`
- Current marker: `experimental`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:162`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:17`
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:88 `ICameraZoomApi` (experimental)
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:17
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-0882"></a>
### DTMAPI.Abstractions.ICameraZoomApi.Register(IManifest owner, CameraZoomOptions options)

- Symbol: `DTMAPI.Abstractions.ICameraZoomApi.Register(IManifest owner, CameraZoomOptions options)`
- Current marker: `experimental`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:164`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1242`
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:88 `ICameraZoomApi` (experimental)
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1242
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-0883"></a>
### DTMAPI.Abstractions.ICameraZoomApi.SetViewScale(IManifest owner, double viewScale, string reason)

- Symbol: `DTMAPI.Abstractions.ICameraZoomApi.SetViewScale(IManifest owner, double viewScale, string reason)`
- Current marker: `experimental`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:165`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1274`
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:88 `ICameraZoomApi` (experimental)
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1274
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-0884"></a>
### DTMAPI.Abstractions.ICameraZoomApi.StepViewScale(IManifest owner, int direction, string reason)

- Symbol: `DTMAPI.Abstractions.ICameraZoomApi.StepViewScale(IManifest owner, int direction, string reason)`
- Current marker: `experimental`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:166`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1304`
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:88 `ICameraZoomApi` (experimental)
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1304
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-0885"></a>
### DTMAPI.Abstractions.ICameraZoomApi.ResetViewScale(IManifest owner, string reason)

- Symbol: `DTMAPI.Abstractions.ICameraZoomApi.ResetViewScale(IManifest owner, string reason)`
- Current marker: `experimental`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:167`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1316`
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:88 `ICameraZoomApi` (experimental)
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1316
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-0886"></a>
### DTMAPI.Abstractions.ICameraZoomApi.GetState(string uniqueId)

- Symbol: `DTMAPI.Abstractions.ICameraZoomApi.GetState(string uniqueId)`
- Current marker: `experimental`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:168`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1321`
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:88 `ICameraZoomApi` (experimental)
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1321
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-0887"></a>
### DTMAPI.Abstractions.ICameraZoomApi.GetStatus(string uniqueId)

- Symbol: `DTMAPI.Abstractions.ICameraZoomApi.GetStatus(string uniqueId)`
- Current marker: `experimental`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:169`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1326`
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:88 `ICameraZoomApi` (experimental)
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1326
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-0888"></a>
### DTMAPI.Abstractions.IChestLocatorEnhancerApi

- Symbol: `DTMAPI.Abstractions.IChestLocatorEnhancerApi`
- Current marker: `experimental`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:173`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:17`
- Native owner: ArchiveDataHandle.GetAvailableInventories and native LinearInventory Count/Cost consumers.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:89 `IChestLocatorEnhancerApi` (experimental)
  - update record: docs/updates/2026/20260606-0009-030-chest-locator-enhancer.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-163226
  - hook-map entry: docs/hook-map/README.md#hook-inventorychestlocatorenhancer
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:17
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0889"></a>
### DTMAPI.Abstractions.IChestLocatorEnhancerApi.Register(IManifest owner, ChestLocatorEnhancerOptions options)

- Symbol: `DTMAPI.Abstractions.IChestLocatorEnhancerApi.Register(IManifest owner, ChestLocatorEnhancerOptions options)`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:175`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1242`
- Native owner: ArchiveDataHandle.GetAvailableInventories and native LinearInventory Count/Cost consumers.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:89 `IChestLocatorEnhancerApi` (experimental)
  - update record: docs/updates/2026/20260606-0009-030-chest-locator-enhancer.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-163226
  - hook-map entry: docs/hook-map/README.md#hook-inventorychestlocatorenhancer
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1242
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0890"></a>
### DTMAPI.Abstractions.IChestLocatorEnhancerApi.GetState(string uniqueId)

- Symbol: `DTMAPI.Abstractions.IChestLocatorEnhancerApi.GetState(string uniqueId)`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:176`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1362`
- Native owner: ArchiveDataHandle.GetAvailableInventories and native LinearInventory Count/Cost consumers.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:89 `IChestLocatorEnhancerApi` (experimental)
  - update record: docs/updates/2026/20260606-0009-030-chest-locator-enhancer.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-163226
  - hook-map entry: docs/hook-map/README.md#hook-inventorychestlocatorenhancer
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1362
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0891"></a>
### DTMAPI.Abstractions.IChestLocatorEnhancerApi.GetStatus(string uniqueId)

- Symbol: `DTMAPI.Abstractions.IChestLocatorEnhancerApi.GetStatus(string uniqueId)`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:177`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1367`
- Native owner: ArchiveDataHandle.GetAvailableInventories and native LinearInventory Count/Cost consumers.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:89 `IChestLocatorEnhancerApi` (experimental)
  - update record: docs/updates/2026/20260606-0009-030-chest-locator-enhancer.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-163226
  - hook-map entry: docs/hook-map/README.md#hook-inventorychestlocatorenhancer
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1367
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0892"></a>
### DTMAPI.Abstractions.IStrongPlantingGunApi

- Symbol: `DTMAPI.Abstractions.IStrongPlantingGunApi`
- Current marker: `experimental`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:181`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:17`
- Native owner: ItemFarmingGun construction/use/UI transfer and PlantBasin native seed/film/fertilizer checks.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:90 `IStrongPlantingGunApi` (experimental)
  - update record: docs/updates/2026/20260606-0010-030-strong-planting-gun.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-170334
  - hook-map entry: docs/hook-map/README.md#hook-farmingstrongplantinggun
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:17
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0893"></a>
### DTMAPI.Abstractions.IStrongPlantingGunApi.Register(IManifest owner, StrongPlantingGunOptions options)

- Symbol: `DTMAPI.Abstractions.IStrongPlantingGunApi.Register(IManifest owner, StrongPlantingGunOptions options)`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:183`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1242`
- Native owner: ItemFarmingGun construction/use/UI transfer and PlantBasin native seed/film/fertilizer checks.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:90 `IStrongPlantingGunApi` (experimental)
  - update record: docs/updates/2026/20260606-0010-030-strong-planting-gun.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-170334
  - hook-map entry: docs/hook-map/README.md#hook-farmingstrongplantinggun
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1242
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0894"></a>
### DTMAPI.Abstractions.IStrongPlantingGunApi.GetState(string uniqueId)

- Symbol: `DTMAPI.Abstractions.IStrongPlantingGunApi.GetState(string uniqueId)`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:184`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1409`
- Native owner: ItemFarmingGun construction/use/UI transfer and PlantBasin native seed/film/fertilizer checks.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:90 `IStrongPlantingGunApi` (experimental)
  - update record: docs/updates/2026/20260606-0010-030-strong-planting-gun.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-170334
  - hook-map entry: docs/hook-map/README.md#hook-farmingstrongplantinggun
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1409
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0895"></a>
### DTMAPI.Abstractions.IStrongPlantingGunApi.GetStatus(string uniqueId)

- Symbol: `DTMAPI.Abstractions.IStrongPlantingGunApi.GetStatus(string uniqueId)`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:185`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1414`
- Native owner: ItemFarmingGun construction/use/UI transfer and PlantBasin native seed/film/fertilizer checks.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:90 `IStrongPlantingGunApi` (experimental)
  - update record: docs/updates/2026/20260606-0010-030-strong-planting-gun.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-170334
  - hook-map entry: docs/hook-map/README.md#hook-farmingstrongplantinggun
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1414
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1178"></a>
### DTMAPI.Abstractions.SecondMotorOptions

- Symbol: `DTMAPI.Abstractions.SecondMotorOptions`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；拆分 original native motor 与 second/custom clone 语义
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:564`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会污染原生 motor singleton 与 DTMAPI clone 状态；风险是跨房间失效、原车/副车共享状态污染、外观或骑乘状态残留。

<a id="sym-1179"></a>
### DTMAPI.Abstractions.SecondMotorOptions.VehicleId

- Symbol: `DTMAPI.Abstractions.SecondMotorOptions.VehicleId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；拆分 original native motor 与 second/custom clone 语义
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:566`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会污染原生 motor singleton 与 DTMAPI clone 状态；风险是跨房间失效、原车/副车共享状态污染、外观或骑乘状态残留。

<a id="sym-1180"></a>
### DTMAPI.Abstractions.SecondMotorOptions.DisplayName

- Symbol: `DTMAPI.Abstractions.SecondMotorOptions.DisplayName`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:567`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1181"></a>
### DTMAPI.Abstractions.SecondMotorOptions.KeyItemId

- Symbol: `DTMAPI.Abstractions.SecondMotorOptions.KeyItemId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:568`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1182"></a>
### DTMAPI.Abstractions.SecondMotorOptions.SpeedMultiplier

- Symbol: `DTMAPI.Abstractions.SecondMotorOptions.SpeedMultiplier`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；拆分 original native motor 与 second/custom clone 语义
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:569`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会污染原生 motor singleton 与 DTMAPI clone 状态；风险是跨房间失效、原车/副车共享状态污染、外观或骑乘状态残留。

<a id="sym-1183"></a>
### DTMAPI.Abstractions.SecondMotorOptions.UseOriginalMotorVisuals

- Symbol: `DTMAPI.Abstractions.SecondMotorOptions.UseOriginalMotorVisuals`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:570`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1184"></a>
### DTMAPI.Abstractions.SecondMotorOptions.TextureSourceNote

- Symbol: `DTMAPI.Abstractions.SecondMotorOptions.TextureSourceNote`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:571`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1185"></a>
### DTMAPI.Abstractions.SecondMotorOptions.VerboseLogging

- Symbol: `DTMAPI.Abstractions.SecondMotorOptions.VerboseLogging`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:572`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1186"></a>
### DTMAPI.Abstractions.MotorVehicleState

- Symbol: `DTMAPI.Abstractions.MotorVehicleState`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；拆分 original native motor 与 second/custom clone 语义
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:575`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会污染原生 motor singleton 与 DTMAPI clone 状态；风险是跨房间失效、原车/副车共享状态污染、外观或骑乘状态残留。

<a id="sym-1187"></a>
### DTMAPI.Abstractions.MotorVehicleState.VehicleId

- Symbol: `DTMAPI.Abstractions.MotorVehicleState.VehicleId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；拆分 original native motor 与 second/custom clone 语义
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:577`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会污染原生 motor singleton 与 DTMAPI clone 状态；风险是跨房间失效、原车/副车共享状态污染、外观或骑乘状态残留。

<a id="sym-1188"></a>
### DTMAPI.Abstractions.MotorVehicleState.OwnerUniqueId

- Symbol: `DTMAPI.Abstractions.MotorVehicleState.OwnerUniqueId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:578`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1189"></a>
### DTMAPI.Abstractions.MotorVehicleState.DisplayName

- Symbol: `DTMAPI.Abstractions.MotorVehicleState.DisplayName`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:579`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1190"></a>
### DTMAPI.Abstractions.MotorVehicleState.IsOriginalMotor

- Symbol: `DTMAPI.Abstractions.MotorVehicleState.IsOriginalMotor`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:580`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1191"></a>
### DTMAPI.Abstractions.MotorVehicleState.IsRegistered

- Symbol: `DTMAPI.Abstractions.MotorVehicleState.IsRegistered`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:581`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1192"></a>
### DTMAPI.Abstractions.MotorVehicleState.IsUnlocked

- Symbol: `DTMAPI.Abstractions.MotorVehicleState.IsUnlocked`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:582`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1193"></a>
### DTMAPI.Abstractions.MotorVehicleState.IsVisible

- Symbol: `DTMAPI.Abstractions.MotorVehicleState.IsVisible`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:583`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1194"></a>
### DTMAPI.Abstractions.MotorVehicleState.IsRiding

- Symbol: `DTMAPI.Abstractions.MotorVehicleState.IsRiding`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:584`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1195"></a>
### DTMAPI.Abstractions.MotorVehicleState.IsAvailableInCurrentRoom

- Symbol: `DTMAPI.Abstractions.MotorVehicleState.IsAvailableInCurrentRoom`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:585`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1196"></a>
### DTMAPI.Abstractions.MotorVehicleState.RoomId

- Symbol: `DTMAPI.Abstractions.MotorVehicleState.RoomId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:586`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1197"></a>
### DTMAPI.Abstractions.MotorVehicleState.RoomTitle

- Symbol: `DTMAPI.Abstractions.MotorVehicleState.RoomTitle`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:587`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1198"></a>
### DTMAPI.Abstractions.MotorVehicleState.X

- Symbol: `DTMAPI.Abstractions.MotorVehicleState.X`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:588`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1199"></a>
### DTMAPI.Abstractions.MotorVehicleState.Y

- Symbol: `DTMAPI.Abstractions.MotorVehicleState.Y`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:589`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1200"></a>
### DTMAPI.Abstractions.MotorVehicleState.Z

- Symbol: `DTMAPI.Abstractions.MotorVehicleState.Z`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:590`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1201"></a>
### DTMAPI.Abstractions.MotorVehicleState.EnduranceProgress

- Symbol: `DTMAPI.Abstractions.MotorVehicleState.EnduranceProgress`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:591`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1202"></a>
### DTMAPI.Abstractions.MotorVehicleState.BaseMaxSpeed

- Symbol: `DTMAPI.Abstractions.MotorVehicleState.BaseMaxSpeed`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；拆分 original native motor 与 second/custom clone 语义
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:592`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会污染原生 motor singleton 与 DTMAPI clone 状态；风险是跨房间失效、原车/副车共享状态污染、外观或骑乘状态残留。

<a id="sym-1203"></a>
### DTMAPI.Abstractions.MotorVehicleState.EffectiveMaxSpeed

- Symbol: `DTMAPI.Abstractions.MotorVehicleState.EffectiveMaxSpeed`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；拆分 original native motor 与 second/custom clone 语义
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:593`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会污染原生 motor singleton 与 DTMAPI clone 状态；风险是跨房间失效、原车/副车共享状态污染、外观或骑乘状态残留。

<a id="sym-1204"></a>
### DTMAPI.Abstractions.MotorVehicleState.SpeedMultiplier

- Symbol: `DTMAPI.Abstractions.MotorVehicleState.SpeedMultiplier`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；拆分 original native motor 与 second/custom clone 语义
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:594`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会污染原生 motor singleton 与 DTMAPI clone 状态；风险是跨房间失效、原车/副车共享状态污染、外观或骑乘状态残留。

<a id="sym-1205"></a>
### DTMAPI.Abstractions.MotorVehicleState.KeyItemId

- Symbol: `DTMAPI.Abstractions.MotorVehicleState.KeyItemId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:595`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1206"></a>
### DTMAPI.Abstractions.MotorVehicleState.LastFailureReason

- Symbol: `DTMAPI.Abstractions.MotorVehicleState.LastFailureReason`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:596`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1207"></a>
### DTMAPI.Abstractions.MotorVehicleState.LastMessage

- Symbol: `DTMAPI.Abstractions.MotorVehicleState.LastMessage`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:597`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1208"></a>
### DTMAPI.Abstractions.MotorVehicleRegisterResult

- Symbol: `DTMAPI.Abstractions.MotorVehicleRegisterResult`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；拆分 original native motor 与 second/custom clone 语义
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:600`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会污染原生 motor singleton 与 DTMAPI clone 状态；风险是跨房间失效、原车/副车共享状态污染、外观或骑乘状态残留。

<a id="sym-1209"></a>
### DTMAPI.Abstractions.MotorVehicleRegisterResult.Success

- Symbol: `DTMAPI.Abstractions.MotorVehicleRegisterResult.Success`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:602`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1210"></a>
### DTMAPI.Abstractions.MotorVehicleRegisterResult.VehicleId

- Symbol: `DTMAPI.Abstractions.MotorVehicleRegisterResult.VehicleId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；拆分 original native motor 与 second/custom clone 语义
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:603`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会污染原生 motor singleton 与 DTMAPI clone 状态；风险是跨房间失效、原车/副车共享状态污染、外观或骑乘状态残留。

<a id="sym-1211"></a>
### DTMAPI.Abstractions.MotorVehicleRegisterResult.KeyItemId

- Symbol: `DTMAPI.Abstractions.MotorVehicleRegisterResult.KeyItemId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:604`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1212"></a>
### DTMAPI.Abstractions.MotorVehicleRegisterResult.State

- Symbol: `DTMAPI.Abstractions.MotorVehicleRegisterResult.State`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；拆分 original native motor 与 second/custom clone 语义
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:605`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会污染原生 motor singleton 与 DTMAPI clone 状态；风险是跨房间失效、原车/副车共享状态污染、外观或骑乘状态残留。

<a id="sym-1213"></a>
### DTMAPI.Abstractions.MotorVehicleRegisterResult.FailureReason

- Symbol: `DTMAPI.Abstractions.MotorVehicleRegisterResult.FailureReason`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:606`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1214"></a>
### DTMAPI.Abstractions.MotorVehicleRegisterResult.Message

- Symbol: `DTMAPI.Abstractions.MotorVehicleRegisterResult.Message`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:607`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1215"></a>
### DTMAPI.Abstractions.MotorVehicleSummonResult

- Symbol: `DTMAPI.Abstractions.MotorVehicleSummonResult`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；拆分 original native motor 与 second/custom clone 语义
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:610`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会污染原生 motor singleton 与 DTMAPI clone 状态；风险是跨房间失效、原车/副车共享状态污染、外观或骑乘状态残留。

<a id="sym-1216"></a>
### DTMAPI.Abstractions.MotorVehicleSummonResult.Success

- Symbol: `DTMAPI.Abstractions.MotorVehicleSummonResult.Success`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:612`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1217"></a>
### DTMAPI.Abstractions.MotorVehicleSummonResult.VehicleId

- Symbol: `DTMAPI.Abstractions.MotorVehicleSummonResult.VehicleId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；拆分 original native motor 与 second/custom clone 语义
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:613`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会污染原生 motor singleton 与 DTMAPI clone 状态；风险是跨房间失效、原车/副车共享状态污染、外观或骑乘状态残留。

<a id="sym-1218"></a>
### DTMAPI.Abstractions.MotorVehicleSummonResult.DisplayName

- Symbol: `DTMAPI.Abstractions.MotorVehicleSummonResult.DisplayName`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:614`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1219"></a>
### DTMAPI.Abstractions.MotorVehicleSummonResult.Before

- Symbol: `DTMAPI.Abstractions.MotorVehicleSummonResult.Before`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:615`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1220"></a>
### DTMAPI.Abstractions.MotorVehicleSummonResult.After

- Symbol: `DTMAPI.Abstractions.MotorVehicleSummonResult.After`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:616`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1221"></a>
### DTMAPI.Abstractions.MotorVehicleSummonResult.FailureReason

- Symbol: `DTMAPI.Abstractions.MotorVehicleSummonResult.FailureReason`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:617`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1222"></a>
### DTMAPI.Abstractions.MotorVehicleSummonResult.Message

- Symbol: `DTMAPI.Abstractions.MotorVehicleSummonResult.Message`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:618`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1223"></a>
### DTMAPI.Abstractions.MotorVehicleRideResult

- Symbol: `DTMAPI.Abstractions.MotorVehicleRideResult`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；拆分 original native motor 与 second/custom clone 语义
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:621`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会污染原生 motor singleton 与 DTMAPI clone 状态；风险是跨房间失效、原车/副车共享状态污染、外观或骑乘状态残留。

<a id="sym-1224"></a>
### DTMAPI.Abstractions.MotorVehicleRideResult.Success

- Symbol: `DTMAPI.Abstractions.MotorVehicleRideResult.Success`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:623`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1225"></a>
### DTMAPI.Abstractions.MotorVehicleRideResult.VehicleId

- Symbol: `DTMAPI.Abstractions.MotorVehicleRideResult.VehicleId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；拆分 original native motor 与 second/custom clone 语义
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:624`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会污染原生 motor singleton 与 DTMAPI clone 状态；风险是跨房间失效、原车/副车共享状态污染、外观或骑乘状态残留。

<a id="sym-1226"></a>
### DTMAPI.Abstractions.MotorVehicleRideResult.DisplayName

- Symbol: `DTMAPI.Abstractions.MotorVehicleRideResult.DisplayName`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:625`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1227"></a>
### DTMAPI.Abstractions.MotorVehicleRideResult.Action

- Symbol: `DTMAPI.Abstractions.MotorVehicleRideResult.Action`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:626`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1228"></a>
### DTMAPI.Abstractions.MotorVehicleRideResult.Before

- Symbol: `DTMAPI.Abstractions.MotorVehicleRideResult.Before`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:627`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1229"></a>
### DTMAPI.Abstractions.MotorVehicleRideResult.After

- Symbol: `DTMAPI.Abstractions.MotorVehicleRideResult.After`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:628`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1230"></a>
### DTMAPI.Abstractions.MotorVehicleRideResult.FailureReason

- Symbol: `DTMAPI.Abstractions.MotorVehicleRideResult.FailureReason`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:629`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1231"></a>
### DTMAPI.Abstractions.MotorVehicleRideResult.Message

- Symbol: `DTMAPI.Abstractions.MotorVehicleRideResult.Message`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:630`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1232"></a>
### DTMAPI.Abstractions.MotorVehicleEventArgs

- Symbol: `DTMAPI.Abstractions.MotorVehicleEventArgs`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；拆分 original native motor 与 second/custom clone 语义
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:633`
- Implementation: No runtime implementation; public type is a data/contract surface or enum.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会污染原生 motor singleton 与 DTMAPI clone 状态；风险是跨房间失效、原车/副车共享状态污染、外观或骑乘状态残留。

<a id="sym-1233"></a>
### DTMAPI.Abstractions.MotorVehicleEventArgs.EventType

- Symbol: `DTMAPI.Abstractions.MotorVehicleEventArgs.EventType`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:635`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1234"></a>
### DTMAPI.Abstractions.MotorVehicleEventArgs.VehicleId

- Symbol: `DTMAPI.Abstractions.MotorVehicleEventArgs.VehicleId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；拆分 original native motor 与 second/custom clone 语义
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:636`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会污染原生 motor singleton 与 DTMAPI clone 状态；风险是跨房间失效、原车/副车共享状态污染、外观或骑乘状态残留。

<a id="sym-1235"></a>
### DTMAPI.Abstractions.MotorVehicleEventArgs.State

- Symbol: `DTMAPI.Abstractions.MotorVehicleEventArgs.State`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；拆分 original native motor 与 second/custom clone 语义
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:637`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会污染原生 motor singleton 与 DTMAPI clone 状态；风险是跨房间失效、原车/副车共享状态污染、外观或骑乘状态残留。

<a id="sym-1236"></a>
### DTMAPI.Abstractions.MotorVehicleEventArgs.Message

- Symbol: `DTMAPI.Abstractions.MotorVehicleEventArgs.Message`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:638`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: original DolocAPI.Motor/MotorController singleton plus DTMAPI second-motor clone/routing state.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150357
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#vehicle-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1237"></a>
### DTMAPI.Abstractions.MachineDefinition

- Symbol: `DTMAPI.Abstractions.MachineDefinition`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:641`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1238"></a>
### DTMAPI.Abstractions.MachineDefinition.MachineId

- Symbol: `DTMAPI.Abstractions.MachineDefinition.MachineId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:643`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1239"></a>
### DTMAPI.Abstractions.MachineDefinition.DisplayName

- Symbol: `DTMAPI.Abstractions.MachineDefinition.DisplayName`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:644`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1240"></a>
### DTMAPI.Abstractions.MachineDefinition.ItemId

- Symbol: `DTMAPI.Abstractions.MachineDefinition.ItemId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:645`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1241"></a>
### DTMAPI.Abstractions.MachineDefinition.EquipmentId

- Symbol: `DTMAPI.Abstractions.MachineDefinition.EquipmentId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:646`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1242"></a>
### DTMAPI.Abstractions.MachineDefinition.RecipeId

- Symbol: `DTMAPI.Abstractions.MachineDefinition.RecipeId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:647`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1243"></a>
### DTMAPI.Abstractions.MachineDefinition.RecipeGroupId

- Symbol: `DTMAPI.Abstractions.MachineDefinition.RecipeGroupId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:648`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1244"></a>
### DTMAPI.Abstractions.MachineDefinition.VisualScale

- Symbol: `DTMAPI.Abstractions.MachineDefinition.VisualScale`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:649`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1245"></a>
### DTMAPI.Abstractions.MachineDefinition.AllowFuelMode

- Symbol: `DTMAPI.Abstractions.MachineDefinition.AllowFuelMode`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:650`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1246"></a>
### DTMAPI.Abstractions.MachineDefinition.AllowElectricMode

- Symbol: `DTMAPI.Abstractions.MachineDefinition.AllowElectricMode`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:651`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1247"></a>
### DTMAPI.Abstractions.MachineDefinition.DefaultMode

- Symbol: `DTMAPI.Abstractions.MachineDefinition.DefaultMode`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:652`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1248"></a>
### DTMAPI.Abstractions.MachineDefinition.NativeTechTreeId

- Symbol: `DTMAPI.Abstractions.MachineDefinition.NativeTechTreeId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:653`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1249"></a>
### DTMAPI.Abstractions.MachineDefinition.NativeTechNodeId

- Symbol: `DTMAPI.Abstractions.MachineDefinition.NativeTechNodeId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:654`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1250"></a>
### DTMAPI.Abstractions.MachineDefinition.NativeTechNodeTitle

- Symbol: `DTMAPI.Abstractions.MachineDefinition.NativeTechNodeTitle`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:655`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1251"></a>
### DTMAPI.Abstractions.MachineDefinition.NativeTechNodeDescription

- Symbol: `DTMAPI.Abstractions.MachineDefinition.NativeTechNodeDescription`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:656`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1252"></a>
### DTMAPI.Abstractions.MachineDefinition.NativeTechNodeParentId

- Symbol: `DTMAPI.Abstractions.MachineDefinition.NativeTechNodeParentId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:657`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1253"></a>
### DTMAPI.Abstractions.MachineDefinition.NativeTechNodeAboveTitleContains

- Symbol: `DTMAPI.Abstractions.MachineDefinition.NativeTechNodeAboveTitleContains`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:658`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1254"></a>
### DTMAPI.Abstractions.MachineDefinition.FuelCapacity

- Symbol: `DTMAPI.Abstractions.MachineDefinition.FuelCapacity`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:659`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1255"></a>
### DTMAPI.Abstractions.MachineDefinition.FuelOnlyFuelCostPerCycle

- Symbol: `DTMAPI.Abstractions.MachineDefinition.FuelOnlyFuelCostPerCycle`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:660`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1256"></a>
### DTMAPI.Abstractions.MachineDefinition.ElectricModeFuelCostPerCycle

- Symbol: `DTMAPI.Abstractions.MachineDefinition.ElectricModeFuelCostPerCycle`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:661`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1257"></a>
### DTMAPI.Abstractions.MachineDefinition.ElectricModePowerCostPerCycle

- Symbol: `DTMAPI.Abstractions.MachineDefinition.ElectricModePowerCostPerCycle`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:662`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1258"></a>
### DTMAPI.Abstractions.MachineDefinition.CycleMinutes

- Symbol: `DTMAPI.Abstractions.MachineDefinition.CycleMinutes`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:663`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1259"></a>
### DTMAPI.Abstractions.MachineDefinition.RecipeInputs

- Symbol: `DTMAPI.Abstractions.MachineDefinition.RecipeInputs`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:664`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1260"></a>
### DTMAPI.Abstractions.MachineDefinition.IncludeRuntimeModMinerals

- Symbol: `DTMAPI.Abstractions.MachineDefinition.IncludeRuntimeModMinerals`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:665`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1261"></a>
### DTMAPI.Abstractions.MachineDefinition.OutputRules

- Symbol: `DTMAPI.Abstractions.MachineDefinition.OutputRules`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:666`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1262"></a>
### DTMAPI.Abstractions.MachineDefinition.ProbabilityOverrides

- Symbol: `DTMAPI.Abstractions.MachineDefinition.ProbabilityOverrides`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:667`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1263"></a>
### DTMAPI.Abstractions.MachineDefinition.VerboseLogging

- Symbol: `DTMAPI.Abstractions.MachineDefinition.VerboseLogging`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:668`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1264"></a>
### DTMAPI.Abstractions.MachineRecipeInput

- Symbol: `DTMAPI.Abstractions.MachineRecipeInput`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:671`
- Implementation: No runtime implementation; public type is a data/contract surface or enum.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1265"></a>
### DTMAPI.Abstractions.MachineRecipeInput.ItemId

- Symbol: `DTMAPI.Abstractions.MachineRecipeInput.ItemId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:673`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1266"></a>
### DTMAPI.Abstractions.MachineRecipeInput.Count

- Symbol: `DTMAPI.Abstractions.MachineRecipeInput.Count`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:674`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1267"></a>
### DTMAPI.Abstractions.MachineOutputRule

- Symbol: `DTMAPI.Abstractions.MachineOutputRule`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:677`
- Implementation: No runtime implementation; public type is a data/contract surface or enum.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1268"></a>
### DTMAPI.Abstractions.MachineOutputRule.ItemId

- Symbol: `DTMAPI.Abstractions.MachineOutputRule.ItemId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:679`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1269"></a>
### DTMAPI.Abstractions.MachineOutputRule.DisplayName

- Symbol: `DTMAPI.Abstractions.MachineOutputRule.DisplayName`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:680`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1270"></a>
### DTMAPI.Abstractions.MachineOutputRule.Weight

- Symbol: `DTMAPI.Abstractions.MachineOutputRule.Weight`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:681`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1271"></a>
### DTMAPI.Abstractions.MachineOutputRule.MinCount

- Symbol: `DTMAPI.Abstractions.MachineOutputRule.MinCount`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:682`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1272"></a>
### DTMAPI.Abstractions.MachineOutputRule.MaxCount

- Symbol: `DTMAPI.Abstractions.MachineOutputRule.MaxCount`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:683`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1273"></a>
### DTMAPI.Abstractions.MachineOutputRule.Source

- Symbol: `DTMAPI.Abstractions.MachineOutputRule.Source`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:684`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1274"></a>
### DTMAPI.Abstractions.MachineOutputRule.AllowProbabilityOverride

- Symbol: `DTMAPI.Abstractions.MachineOutputRule.AllowProbabilityOverride`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:685`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1275"></a>
### DTMAPI.Abstractions.MachineRegisterResult

- Symbol: `DTMAPI.Abstractions.MachineRegisterResult`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:688`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1276"></a>
### DTMAPI.Abstractions.MachineRegisterResult.Success

- Symbol: `DTMAPI.Abstractions.MachineRegisterResult.Success`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:690`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1277"></a>
### DTMAPI.Abstractions.MachineRegisterResult.OwnerId

- Symbol: `DTMAPI.Abstractions.MachineRegisterResult.OwnerId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:691`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1278"></a>
### DTMAPI.Abstractions.MachineRegisterResult.MachineId

- Symbol: `DTMAPI.Abstractions.MachineRegisterResult.MachineId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:692`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1279"></a>
### DTMAPI.Abstractions.MachineRegisterResult.Definition

- Symbol: `DTMAPI.Abstractions.MachineRegisterResult.Definition`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:693`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1280"></a>
### DTMAPI.Abstractions.MachineRegisterResult.FailureReason

- Symbol: `DTMAPI.Abstractions.MachineRegisterResult.FailureReason`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:694`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1281"></a>
### DTMAPI.Abstractions.MachineRegisterResult.Message

- Symbol: `DTMAPI.Abstractions.MachineRegisterResult.Message`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:695`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1282"></a>
### DTMAPI.Abstractions.MachineProductionState

- Symbol: `DTMAPI.Abstractions.MachineProductionState`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:698`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1283"></a>
### DTMAPI.Abstractions.MachineProductionState.OwnerId

- Symbol: `DTMAPI.Abstractions.MachineProductionState.OwnerId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:700`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1284"></a>
### DTMAPI.Abstractions.MachineProductionState.IsConfigured

- Symbol: `DTMAPI.Abstractions.MachineProductionState.IsConfigured`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:701`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1285"></a>
### DTMAPI.Abstractions.MachineProductionState.RegisteredMachineCount

- Symbol: `DTMAPI.Abstractions.MachineProductionState.RegisteredMachineCount`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:702`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1286"></a>
### DTMAPI.Abstractions.MachineProductionState.RuntimeHookInstalled

- Symbol: `DTMAPI.Abstractions.MachineProductionState.RuntimeHookInstalled`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:703`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1287"></a>
### DTMAPI.Abstractions.MachineProductionState.MachineId

- Symbol: `DTMAPI.Abstractions.MachineProductionState.MachineId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:704`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1288"></a>
### DTMAPI.Abstractions.MachineProductionState.DisplayName

- Symbol: `DTMAPI.Abstractions.MachineProductionState.DisplayName`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:705`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1289"></a>
### DTMAPI.Abstractions.MachineProductionState.ItemId

- Symbol: `DTMAPI.Abstractions.MachineProductionState.ItemId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:706`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1290"></a>
### DTMAPI.Abstractions.MachineProductionState.EquipmentId

- Symbol: `DTMAPI.Abstractions.MachineProductionState.EquipmentId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:707`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1291"></a>
### DTMAPI.Abstractions.MachineProductionState.RecipeId

- Symbol: `DTMAPI.Abstractions.MachineProductionState.RecipeId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:708`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1292"></a>
### DTMAPI.Abstractions.MachineProductionState.RecipeGroupId

- Symbol: `DTMAPI.Abstractions.MachineProductionState.RecipeGroupId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:709`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1293"></a>
### DTMAPI.Abstractions.MachineProductionState.VisualScale

- Symbol: `DTMAPI.Abstractions.MachineProductionState.VisualScale`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:710`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1294"></a>
### DTMAPI.Abstractions.MachineProductionState.AllowFuelMode

- Symbol: `DTMAPI.Abstractions.MachineProductionState.AllowFuelMode`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:711`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1295"></a>
### DTMAPI.Abstractions.MachineProductionState.AllowElectricMode

- Symbol: `DTMAPI.Abstractions.MachineProductionState.AllowElectricMode`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:712`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1296"></a>
### DTMAPI.Abstractions.MachineProductionState.DefaultMode

- Symbol: `DTMAPI.Abstractions.MachineProductionState.DefaultMode`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:713`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1297"></a>
### DTMAPI.Abstractions.MachineProductionState.FuelCapacity

- Symbol: `DTMAPI.Abstractions.MachineProductionState.FuelCapacity`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:714`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1298"></a>
### DTMAPI.Abstractions.MachineProductionState.RemainingFuel

- Symbol: `DTMAPI.Abstractions.MachineProductionState.RemainingFuel`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:715`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1299"></a>
### DTMAPI.Abstractions.MachineProductionState.FuelOnlyFuelCostPerCycle

- Symbol: `DTMAPI.Abstractions.MachineProductionState.FuelOnlyFuelCostPerCycle`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:716`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1300"></a>
### DTMAPI.Abstractions.MachineProductionState.ElectricModeFuelCostPerCycle

- Symbol: `DTMAPI.Abstractions.MachineProductionState.ElectricModeFuelCostPerCycle`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:717`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1301"></a>
### DTMAPI.Abstractions.MachineProductionState.ElectricModePowerCostPerCycle

- Symbol: `DTMAPI.Abstractions.MachineProductionState.ElectricModePowerCostPerCycle`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:718`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1302"></a>
### DTMAPI.Abstractions.MachineProductionState.CycleMinutes

- Symbol: `DTMAPI.Abstractions.MachineProductionState.CycleMinutes`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:719`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1303"></a>
### DTMAPI.Abstractions.MachineProductionState.CycleTUs

- Symbol: `DTMAPI.Abstractions.MachineProductionState.CycleTUs`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:720`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1304"></a>
### DTMAPI.Abstractions.MachineProductionState.NextDueTotalTUs

- Symbol: `DTMAPI.Abstractions.MachineProductionState.NextDueTotalTUs`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:721`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1305"></a>
### DTMAPI.Abstractions.MachineProductionState.PlacedMachineCount

- Symbol: `DTMAPI.Abstractions.MachineProductionState.PlacedMachineCount`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:722`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1306"></a>
### DTMAPI.Abstractions.MachineProductionState.ProductionCycleCount

- Symbol: `DTMAPI.Abstractions.MachineProductionState.ProductionCycleCount`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:723`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1307"></a>
### DTMAPI.Abstractions.MachineProductionState.LastOutputItemId

- Symbol: `DTMAPI.Abstractions.MachineProductionState.LastOutputItemId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:724`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1308"></a>
### DTMAPI.Abstractions.MachineProductionState.LastOutputDisplayName

- Symbol: `DTMAPI.Abstractions.MachineProductionState.LastOutputDisplayName`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:725`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1309"></a>
### DTMAPI.Abstractions.MachineProductionState.LastOutputCount

- Symbol: `DTMAPI.Abstractions.MachineProductionState.LastOutputCount`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:726`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1310"></a>
### DTMAPI.Abstractions.MachineProductionState.LastMachineKey

- Symbol: `DTMAPI.Abstractions.MachineProductionState.LastMachineKey`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:727`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1311"></a>
### DTMAPI.Abstractions.MachineProductionState.LastMode

- Symbol: `DTMAPI.Abstractions.MachineProductionState.LastMode`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:728`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1312"></a>
### DTMAPI.Abstractions.MachineProductionState.LastFuelCost

- Symbol: `DTMAPI.Abstractions.MachineProductionState.LastFuelCost`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:729`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1313"></a>
### DTMAPI.Abstractions.MachineProductionState.LastElectricPowerCost

- Symbol: `DTMAPI.Abstractions.MachineProductionState.LastElectricPowerCost`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:730`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1314"></a>
### DTMAPI.Abstractions.MachineProductionState.LastObservedTotalTUs

- Symbol: `DTMAPI.Abstractions.MachineProductionState.LastObservedTotalTUs`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:731`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1315"></a>
### DTMAPI.Abstractions.MachineProductionState.LastOutputTarget

- Symbol: `DTMAPI.Abstractions.MachineProductionState.LastOutputTarget`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:732`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1316"></a>
### DTMAPI.Abstractions.MachineProductionState.LastStorageFilledSlots

- Symbol: `DTMAPI.Abstractions.MachineProductionState.LastStorageFilledSlots`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:733`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1317"></a>
### DTMAPI.Abstractions.MachineProductionState.LastStorageCapacity

- Symbol: `DTMAPI.Abstractions.MachineProductionState.LastStorageCapacity`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:734`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1318"></a>
### DTMAPI.Abstractions.MachineProductionState.LastStorageLineCapacity

- Symbol: `DTMAPI.Abstractions.MachineProductionState.LastStorageLineCapacity`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:735`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1319"></a>
### DTMAPI.Abstractions.MachineProductionState.NativeTechTreeSummary

- Symbol: `DTMAPI.Abstractions.MachineProductionState.NativeTechTreeSummary`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:736`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1320"></a>
### DTMAPI.Abstractions.MachineProductionState.Status

- Symbol: `DTMAPI.Abstractions.MachineProductionState.Status`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:737`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1321"></a>
### DTMAPI.Abstractions.MachineProductionState.LastMessage

- Symbol: `DTMAPI.Abstractions.MachineProductionState.LastMessage`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；不得升 stable，优先重审 native production owner
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:738`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: DolocConfig recipe/tech/item tables and native electronic component Launch(); production state is still partly DTMAPI runtime-owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-machineproductionruntimeloop
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI runtime loop 当作原生机器生命周期；风险是存档/离线房间/电力/输出状态不同步，UI 或 telemetry 成功但 native worker owner 未完全接管。

<a id="sym-1322"></a>
### DTMAPI.Abstractions.EquipmentSlotsOptions

- Symbol: `DTMAPI.Abstractions.EquipmentSlotsOptions`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:741`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1323"></a>
### DTMAPI.Abstractions.EquipmentSlotsOptions.Enabled

- Symbol: `DTMAPI.Abstractions.EquipmentSlotsOptions.Enabled`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:743`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1324"></a>
### DTMAPI.Abstractions.EquipmentSlotsOptions.ExtraAttributeSlots

- Symbol: `DTMAPI.Abstractions.EquipmentSlotsOptions.ExtraAttributeSlots`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:744`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1325"></a>
### DTMAPI.Abstractions.EquipmentSlotsOptions.SlotIdPrefix

- Symbol: `DTMAPI.Abstractions.EquipmentSlotsOptions.SlotIdPrefix`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:745`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1326"></a>
### DTMAPI.Abstractions.EquipmentSlotsOptions.PreserveVanillaVisualSlots

- Symbol: `DTMAPI.Abstractions.EquipmentSlotsOptions.PreserveVanillaVisualSlots`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:746`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1327"></a>
### DTMAPI.Abstractions.EquipmentSlotsOptions.ExtraSlotsAffectVisuals

- Symbol: `DTMAPI.Abstractions.EquipmentSlotsOptions.ExtraSlotsAffectVisuals`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:747`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1328"></a>
### DTMAPI.Abstractions.EquipmentSlotsOptions.SafeUnequipOnDisable

- Symbol: `DTMAPI.Abstractions.EquipmentSlotsOptions.SafeUnequipOnDisable`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:748`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1329"></a>
### DTMAPI.Abstractions.EquipmentSlotsOptions.AutoRecoverOnMissingMod

- Symbol: `DTMAPI.Abstractions.EquipmentSlotsOptions.AutoRecoverOnMissingMod`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:749`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1330"></a>
### DTMAPI.Abstractions.EquipmentSlotsOptions.VerboseLogging

- Symbol: `DTMAPI.Abstractions.EquipmentSlotsOptions.VerboseLogging`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:750`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1331"></a>
### DTMAPI.Abstractions.SaveSlotsOptions

- Symbol: `DTMAPI.Abstractions.SaveSlotsOptions`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:753`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: DolocAPI.gameManager.archiveFileCount and official GameDataPanel save-slot rendering.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0004-029-readme-implementation.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-051653
  - hook-map entry: docs/hook-map/README.md#hook-savemoreslotsapi
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1332"></a>
### DTMAPI.Abstractions.SaveSlotsOptions.Enabled

- Symbol: `DTMAPI.Abstractions.SaveSlotsOptions.Enabled`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:755`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.gameManager.archiveFileCount and official GameDataPanel save-slot rendering.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0004-029-readme-implementation.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-051653
  - hook-map entry: docs/hook-map/README.md#hook-savemoreslotsapi
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1333"></a>
### DTMAPI.Abstractions.SaveSlotsOptions.SlotCount

- Symbol: `DTMAPI.Abstractions.SaveSlotsOptions.SlotCount`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:756`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.gameManager.archiveFileCount and official GameDataPanel save-slot rendering.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0004-029-readme-implementation.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-051653
  - hook-map entry: docs/hook-map/README.md#hook-savemoreslotsapi
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1334"></a>
### DTMAPI.Abstractions.SaveSlotsOptions.VerboseLogging

- Symbol: `DTMAPI.Abstractions.SaveSlotsOptions.VerboseLogging`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:757`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.gameManager.archiveFileCount and official GameDataPanel save-slot rendering.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0004-029-readme-implementation.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-051653
  - hook-map entry: docs/hook-map/README.md#hook-savemoreslotsapi
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1335"></a>
### DTMAPI.Abstractions.SaveSlotsRegisterResult

- Symbol: `DTMAPI.Abstractions.SaveSlotsRegisterResult`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:760`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: DolocAPI.gameManager.archiveFileCount and official GameDataPanel save-slot rendering.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0004-029-readme-implementation.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-051653
  - hook-map entry: docs/hook-map/README.md#hook-savemoreslotsapi
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1336"></a>
### DTMAPI.Abstractions.SaveSlotsRegisterResult.Success

- Symbol: `DTMAPI.Abstractions.SaveSlotsRegisterResult.Success`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:762`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.gameManager.archiveFileCount and official GameDataPanel save-slot rendering.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0004-029-readme-implementation.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-051653
  - hook-map entry: docs/hook-map/README.md#hook-savemoreslotsapi
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1337"></a>
### DTMAPI.Abstractions.SaveSlotsRegisterResult.OwnerId

- Symbol: `DTMAPI.Abstractions.SaveSlotsRegisterResult.OwnerId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:763`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.gameManager.archiveFileCount and official GameDataPanel save-slot rendering.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0004-029-readme-implementation.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-051653
  - hook-map entry: docs/hook-map/README.md#hook-savemoreslotsapi
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1338"></a>
### DTMAPI.Abstractions.SaveSlotsRegisterResult.PreviousSlotCount

- Symbol: `DTMAPI.Abstractions.SaveSlotsRegisterResult.PreviousSlotCount`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:764`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.gameManager.archiveFileCount and official GameDataPanel save-slot rendering.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0004-029-readme-implementation.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-051653
  - hook-map entry: docs/hook-map/README.md#hook-savemoreslotsapi
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1339"></a>
### DTMAPI.Abstractions.SaveSlotsRegisterResult.RequestedSlotCount

- Symbol: `DTMAPI.Abstractions.SaveSlotsRegisterResult.RequestedSlotCount`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:765`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.gameManager.archiveFileCount and official GameDataPanel save-slot rendering.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0004-029-readme-implementation.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-051653
  - hook-map entry: docs/hook-map/README.md#hook-savemoreslotsapi
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1340"></a>
### DTMAPI.Abstractions.SaveSlotsRegisterResult.AppliedSlotCount

- Symbol: `DTMAPI.Abstractions.SaveSlotsRegisterResult.AppliedSlotCount`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:766`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.gameManager.archiveFileCount and official GameDataPanel save-slot rendering.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0004-029-readme-implementation.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-051653
  - hook-map entry: docs/hook-map/README.md#hook-savemoreslotsapi
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1341"></a>
### DTMAPI.Abstractions.SaveSlotsRegisterResult.FailureReason

- Symbol: `DTMAPI.Abstractions.SaveSlotsRegisterResult.FailureReason`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:767`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.gameManager.archiveFileCount and official GameDataPanel save-slot rendering.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0004-029-readme-implementation.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-051653
  - hook-map entry: docs/hook-map/README.md#hook-savemoreslotsapi
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1342"></a>
### DTMAPI.Abstractions.SaveSlotsRegisterResult.Message

- Symbol: `DTMAPI.Abstractions.SaveSlotsRegisterResult.Message`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:768`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.gameManager.archiveFileCount and official GameDataPanel save-slot rendering.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0004-029-readme-implementation.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-051653
  - hook-map entry: docs/hook-map/README.md#hook-savemoreslotsapi
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1343"></a>
### DTMAPI.Abstractions.SaveSlotsState

- Symbol: `DTMAPI.Abstractions.SaveSlotsState`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:771`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: DolocAPI.gameManager.archiveFileCount and official GameDataPanel save-slot rendering.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0004-029-readme-implementation.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-051653
  - hook-map entry: docs/hook-map/README.md#hook-savemoreslotsapi
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1344"></a>
### DTMAPI.Abstractions.SaveSlotsState.OwnerId

- Symbol: `DTMAPI.Abstractions.SaveSlotsState.OwnerId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:773`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.gameManager.archiveFileCount and official GameDataPanel save-slot rendering.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0004-029-readme-implementation.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-051653
  - hook-map entry: docs/hook-map/README.md#hook-savemoreslotsapi
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1345"></a>
### DTMAPI.Abstractions.SaveSlotsState.IsConfigured

- Symbol: `DTMAPI.Abstractions.SaveSlotsState.IsConfigured`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:774`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.gameManager.archiveFileCount and official GameDataPanel save-slot rendering.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0004-029-readme-implementation.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-051653
  - hook-map entry: docs/hook-map/README.md#hook-savemoreslotsapi
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1346"></a>
### DTMAPI.Abstractions.SaveSlotsState.Enabled

- Symbol: `DTMAPI.Abstractions.SaveSlotsState.Enabled`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:775`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.gameManager.archiveFileCount and official GameDataPanel save-slot rendering.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0004-029-readme-implementation.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-051653
  - hook-map entry: docs/hook-map/README.md#hook-savemoreslotsapi
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1347"></a>
### DTMAPI.Abstractions.SaveSlotsState.NativeSlotCount

- Symbol: `DTMAPI.Abstractions.SaveSlotsState.NativeSlotCount`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:776`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.gameManager.archiveFileCount and official GameDataPanel save-slot rendering.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0004-029-readme-implementation.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-051653
  - hook-map entry: docs/hook-map/README.md#hook-savemoreslotsapi
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1348"></a>
### DTMAPI.Abstractions.SaveSlotsState.RequestedSlotCount

- Symbol: `DTMAPI.Abstractions.SaveSlotsState.RequestedSlotCount`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:777`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.gameManager.archiveFileCount and official GameDataPanel save-slot rendering.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0004-029-readme-implementation.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-051653
  - hook-map entry: docs/hook-map/README.md#hook-savemoreslotsapi
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1349"></a>
### DTMAPI.Abstractions.SaveSlotsState.AppliedSlotCount

- Symbol: `DTMAPI.Abstractions.SaveSlotsState.AppliedSlotCount`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:778`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.gameManager.archiveFileCount and official GameDataPanel save-slot rendering.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0004-029-readme-implementation.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-051653
  - hook-map entry: docs/hook-map/README.md#hook-savemoreslotsapi
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1350"></a>
### DTMAPI.Abstractions.SaveSlotsState.Status

- Symbol: `DTMAPI.Abstractions.SaveSlotsState.Status`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:779`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.gameManager.archiveFileCount and official GameDataPanel save-slot rendering.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0004-029-readme-implementation.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-051653
  - hook-map entry: docs/hook-map/README.md#hook-savemoreslotsapi
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1351"></a>
### DTMAPI.Abstractions.SaveSlotsState.LastMessage

- Symbol: `DTMAPI.Abstractions.SaveSlotsState.LastMessage`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:780`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.gameManager.archiveFileCount and official GameDataPanel save-slot rendering.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0004-029-readme-implementation.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-051653
  - hook-map entry: docs/hook-map/README.md#hook-savemoreslotsapi
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1352"></a>
### DTMAPI.Abstractions.CameraZoomOptions

- Symbol: `DTMAPI.Abstractions.CameraZoomOptions`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:783`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-1353"></a>
### DTMAPI.Abstractions.CameraZoomOptions.Enabled

- Symbol: `DTMAPI.Abstractions.CameraZoomOptions.Enabled`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:785`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-1354"></a>
### DTMAPI.Abstractions.CameraZoomOptions.MinViewScale

- Symbol: `DTMAPI.Abstractions.CameraZoomOptions.MinViewScale`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:786`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-1355"></a>
### DTMAPI.Abstractions.CameraZoomOptions.MaxViewScale

- Symbol: `DTMAPI.Abstractions.CameraZoomOptions.MaxViewScale`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:787`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-1356"></a>
### DTMAPI.Abstractions.CameraZoomOptions.Step

- Symbol: `DTMAPI.Abstractions.CameraZoomOptions.Step`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:788`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-1357"></a>
### DTMAPI.Abstractions.CameraZoomOptions.VerboseLogging

- Symbol: `DTMAPI.Abstractions.CameraZoomOptions.VerboseLogging`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:789`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-1358"></a>
### DTMAPI.Abstractions.CameraZoomRegisterResult

- Symbol: `DTMAPI.Abstractions.CameraZoomRegisterResult`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:792`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-1359"></a>
### DTMAPI.Abstractions.CameraZoomRegisterResult.Success

- Symbol: `DTMAPI.Abstractions.CameraZoomRegisterResult.Success`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:794`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-1360"></a>
### DTMAPI.Abstractions.CameraZoomRegisterResult.OwnerId

- Symbol: `DTMAPI.Abstractions.CameraZoomRegisterResult.OwnerId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:795`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-1361"></a>
### DTMAPI.Abstractions.CameraZoomRegisterResult.MinViewScale

- Symbol: `DTMAPI.Abstractions.CameraZoomRegisterResult.MinViewScale`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:796`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-1362"></a>
### DTMAPI.Abstractions.CameraZoomRegisterResult.MaxViewScale

- Symbol: `DTMAPI.Abstractions.CameraZoomRegisterResult.MaxViewScale`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:797`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-1363"></a>
### DTMAPI.Abstractions.CameraZoomRegisterResult.CurrentViewScale

- Symbol: `DTMAPI.Abstractions.CameraZoomRegisterResult.CurrentViewScale`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:798`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-1364"></a>
### DTMAPI.Abstractions.CameraZoomRegisterResult.FailureReason

- Symbol: `DTMAPI.Abstractions.CameraZoomRegisterResult.FailureReason`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:799`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-1365"></a>
### DTMAPI.Abstractions.CameraZoomRegisterResult.Message

- Symbol: `DTMAPI.Abstractions.CameraZoomRegisterResult.Message`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:800`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-1366"></a>
### DTMAPI.Abstractions.CameraZoomResult

- Symbol: `DTMAPI.Abstractions.CameraZoomResult`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:803`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-1367"></a>
### DTMAPI.Abstractions.CameraZoomResult.Success

- Symbol: `DTMAPI.Abstractions.CameraZoomResult.Success`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:805`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-1368"></a>
### DTMAPI.Abstractions.CameraZoomResult.OwnerId

- Symbol: `DTMAPI.Abstractions.CameraZoomResult.OwnerId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:806`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-1369"></a>
### DTMAPI.Abstractions.CameraZoomResult.RequestedViewScale

- Symbol: `DTMAPI.Abstractions.CameraZoomResult.RequestedViewScale`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:807`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-1370"></a>
### DTMAPI.Abstractions.CameraZoomResult.BeforeViewScale

- Symbol: `DTMAPI.Abstractions.CameraZoomResult.BeforeViewScale`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:808`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-1371"></a>
### DTMAPI.Abstractions.CameraZoomResult.AfterViewScale

- Symbol: `DTMAPI.Abstractions.CameraZoomResult.AfterViewScale`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:809`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-1372"></a>
### DTMAPI.Abstractions.CameraZoomResult.VanillaOrthographicSize

- Symbol: `DTMAPI.Abstractions.CameraZoomResult.VanillaOrthographicSize`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:810`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-1373"></a>
### DTMAPI.Abstractions.CameraZoomResult.AppliedOrthographicSize

- Symbol: `DTMAPI.Abstractions.CameraZoomResult.AppliedOrthographicSize`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:811`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-1374"></a>
### DTMAPI.Abstractions.CameraZoomResult.FailureReason

- Symbol: `DTMAPI.Abstractions.CameraZoomResult.FailureReason`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:812`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-1375"></a>
### DTMAPI.Abstractions.CameraZoomResult.Message

- Symbol: `DTMAPI.Abstractions.CameraZoomResult.Message`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:813`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-1376"></a>
### DTMAPI.Abstractions.CameraZoomState

- Symbol: `DTMAPI.Abstractions.CameraZoomState`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:816`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-1377"></a>
### DTMAPI.Abstractions.CameraZoomState.OwnerId

- Symbol: `DTMAPI.Abstractions.CameraZoomState.OwnerId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:818`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-1378"></a>
### DTMAPI.Abstractions.CameraZoomState.IsConfigured

- Symbol: `DTMAPI.Abstractions.CameraZoomState.IsConfigured`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:819`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-1379"></a>
### DTMAPI.Abstractions.CameraZoomState.Enabled

- Symbol: `DTMAPI.Abstractions.CameraZoomState.Enabled`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:820`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-1380"></a>
### DTMAPI.Abstractions.CameraZoomState.MinViewScale

- Symbol: `DTMAPI.Abstractions.CameraZoomState.MinViewScale`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:821`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-1381"></a>
### DTMAPI.Abstractions.CameraZoomState.MaxViewScale

- Symbol: `DTMAPI.Abstractions.CameraZoomState.MaxViewScale`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:822`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-1382"></a>
### DTMAPI.Abstractions.CameraZoomState.Step

- Symbol: `DTMAPI.Abstractions.CameraZoomState.Step`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:823`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-1383"></a>
### DTMAPI.Abstractions.CameraZoomState.CurrentViewScale

- Symbol: `DTMAPI.Abstractions.CameraZoomState.CurrentViewScale`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:824`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-1384"></a>
### DTMAPI.Abstractions.CameraZoomState.VanillaOrthographicSize

- Symbol: `DTMAPI.Abstractions.CameraZoomState.VanillaOrthographicSize`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:825`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-1385"></a>
### DTMAPI.Abstractions.CameraZoomState.AppliedOrthographicSize

- Symbol: `DTMAPI.Abstractions.CameraZoomState.AppliedOrthographicSize`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:826`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-1386"></a>
### DTMAPI.Abstractions.CameraZoomState.CameraAvailable

- Symbol: `DTMAPI.Abstractions.CameraZoomState.CameraAvailable`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:827`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-1387"></a>
### DTMAPI.Abstractions.CameraZoomState.Status

- Symbol: `DTMAPI.Abstractions.CameraZoomState.Status`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:828`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-1388"></a>
### DTMAPI.Abstractions.CameraZoomState.LastMessage

- Symbol: `DTMAPI.Abstractions.CameraZoomState.LastMessage`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；补 CameraController/background/depth-fog native owner 后再升级
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:829`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Partial: DolocAPI.mainCamera.orthographicSize only; CameraController.camSize/background/depth-fog are not fully owned.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-134042
  - hook-map entry: docs/hook-map/README.md#hook-camerazoomapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会只改 camera size；风险是背景/depth fog/room bounds 未同步，出现只视觉成功但 camera native owner 未完整接管。

<a id="sym-1389"></a>
### DTMAPI.Abstractions.ChestLocatorEnhancerOptions

- Symbol: `DTMAPI.Abstractions.ChestLocatorEnhancerOptions`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:832`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: ArchiveDataHandle.GetAvailableInventories and native LinearInventory Count/Cost consumers.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0009-030-chest-locator-enhancer.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-163226
  - hook-map entry: docs/hook-map/README.md#hook-inventorychestlocatorenhancer
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1390"></a>
### DTMAPI.Abstractions.ChestLocatorEnhancerOptions.Enabled

- Symbol: `DTMAPI.Abstractions.ChestLocatorEnhancerOptions.Enabled`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:834`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.GetAvailableInventories and native LinearInventory Count/Cost consumers.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0009-030-chest-locator-enhancer.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-163226
  - hook-map entry: docs/hook-map/README.md#hook-inventorychestlocatorenhancer
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1391"></a>
### DTMAPI.Abstractions.ChestLocatorEnhancerOptions.IncludeSharedCases

- Symbol: `DTMAPI.Abstractions.ChestLocatorEnhancerOptions.IncludeSharedCases`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:835`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.GetAvailableInventories and native LinearInventory Count/Cost consumers.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0009-030-chest-locator-enhancer.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-163226
  - hook-map entry: docs/hook-map/README.md#hook-inventorychestlocatorenhancer
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1392"></a>
### DTMAPI.Abstractions.ChestLocatorEnhancerOptions.IncludeSharedStorageShelfBoxes

- Symbol: `DTMAPI.Abstractions.ChestLocatorEnhancerOptions.IncludeSharedStorageShelfBoxes`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:836`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.GetAvailableInventories and native LinearInventory Count/Cost consumers.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0009-030-chest-locator-enhancer.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-163226
  - hook-map entry: docs/hook-map/README.md#hook-inventorychestlocatorenhancer
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1393"></a>
### DTMAPI.Abstractions.ChestLocatorEnhancerOptions.RespectNativeAutoUseBoxSetting

- Symbol: `DTMAPI.Abstractions.ChestLocatorEnhancerOptions.RespectNativeAutoUseBoxSetting`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:837`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.GetAvailableInventories and native LinearInventory Count/Cost consumers.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0009-030-chest-locator-enhancer.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-163226
  - hook-map entry: docs/hook-map/README.md#hook-inventorychestlocatorenhancer
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1394"></a>
### DTMAPI.Abstractions.ChestLocatorEnhancerOptions.VerboseLogging

- Symbol: `DTMAPI.Abstractions.ChestLocatorEnhancerOptions.VerboseLogging`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:838`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.GetAvailableInventories and native LinearInventory Count/Cost consumers.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0009-030-chest-locator-enhancer.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-163226
  - hook-map entry: docs/hook-map/README.md#hook-inventorychestlocatorenhancer
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1395"></a>
### DTMAPI.Abstractions.ChestLocatorEnhancerRegisterResult

- Symbol: `DTMAPI.Abstractions.ChestLocatorEnhancerRegisterResult`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:841`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: ArchiveDataHandle.GetAvailableInventories and native LinearInventory Count/Cost consumers.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0009-030-chest-locator-enhancer.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-163226
  - hook-map entry: docs/hook-map/README.md#hook-inventorychestlocatorenhancer
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1396"></a>
### DTMAPI.Abstractions.ChestLocatorEnhancerRegisterResult.Success

- Symbol: `DTMAPI.Abstractions.ChestLocatorEnhancerRegisterResult.Success`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:843`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.GetAvailableInventories and native LinearInventory Count/Cost consumers.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0009-030-chest-locator-enhancer.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-163226
  - hook-map entry: docs/hook-map/README.md#hook-inventorychestlocatorenhancer
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1397"></a>
### DTMAPI.Abstractions.ChestLocatorEnhancerRegisterResult.OwnerId

- Symbol: `DTMAPI.Abstractions.ChestLocatorEnhancerRegisterResult.OwnerId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:844`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.GetAvailableInventories and native LinearInventory Count/Cost consumers.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0009-030-chest-locator-enhancer.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-163226
  - hook-map entry: docs/hook-map/README.md#hook-inventorychestlocatorenhancer
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1398"></a>
### DTMAPI.Abstractions.ChestLocatorEnhancerRegisterResult.Enabled

- Symbol: `DTMAPI.Abstractions.ChestLocatorEnhancerRegisterResult.Enabled`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:845`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.GetAvailableInventories and native LinearInventory Count/Cost consumers.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0009-030-chest-locator-enhancer.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-163226
  - hook-map entry: docs/hook-map/README.md#hook-inventorychestlocatorenhancer
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1399"></a>
### DTMAPI.Abstractions.ChestLocatorEnhancerRegisterResult.HookInstalled

- Symbol: `DTMAPI.Abstractions.ChestLocatorEnhancerRegisterResult.HookInstalled`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:846`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.GetAvailableInventories and native LinearInventory Count/Cost consumers.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0009-030-chest-locator-enhancer.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-163226
  - hook-map entry: docs/hook-map/README.md#hook-inventorychestlocatorenhancer
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1400"></a>
### DTMAPI.Abstractions.ChestLocatorEnhancerRegisterResult.FailureReason

- Symbol: `DTMAPI.Abstractions.ChestLocatorEnhancerRegisterResult.FailureReason`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:847`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.GetAvailableInventories and native LinearInventory Count/Cost consumers.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0009-030-chest-locator-enhancer.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-163226
  - hook-map entry: docs/hook-map/README.md#hook-inventorychestlocatorenhancer
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1401"></a>
### DTMAPI.Abstractions.ChestLocatorEnhancerRegisterResult.Message

- Symbol: `DTMAPI.Abstractions.ChestLocatorEnhancerRegisterResult.Message`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:848`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.GetAvailableInventories and native LinearInventory Count/Cost consumers.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0009-030-chest-locator-enhancer.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-163226
  - hook-map entry: docs/hook-map/README.md#hook-inventorychestlocatorenhancer
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1402"></a>
### DTMAPI.Abstractions.ChestLocatorEnhancerState

- Symbol: `DTMAPI.Abstractions.ChestLocatorEnhancerState`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:851`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: ArchiveDataHandle.GetAvailableInventories and native LinearInventory Count/Cost consumers.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0009-030-chest-locator-enhancer.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-163226
  - hook-map entry: docs/hook-map/README.md#hook-inventorychestlocatorenhancer
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1403"></a>
### DTMAPI.Abstractions.ChestLocatorEnhancerState.OwnerId

- Symbol: `DTMAPI.Abstractions.ChestLocatorEnhancerState.OwnerId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:853`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.GetAvailableInventories and native LinearInventory Count/Cost consumers.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0009-030-chest-locator-enhancer.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-163226
  - hook-map entry: docs/hook-map/README.md#hook-inventorychestlocatorenhancer
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1404"></a>
### DTMAPI.Abstractions.ChestLocatorEnhancerState.IsConfigured

- Symbol: `DTMAPI.Abstractions.ChestLocatorEnhancerState.IsConfigured`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:854`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.GetAvailableInventories and native LinearInventory Count/Cost consumers.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0009-030-chest-locator-enhancer.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-163226
  - hook-map entry: docs/hook-map/README.md#hook-inventorychestlocatorenhancer
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1405"></a>
### DTMAPI.Abstractions.ChestLocatorEnhancerState.Enabled

- Symbol: `DTMAPI.Abstractions.ChestLocatorEnhancerState.Enabled`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:855`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.GetAvailableInventories and native LinearInventory Count/Cost consumers.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0009-030-chest-locator-enhancer.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-163226
  - hook-map entry: docs/hook-map/README.md#hook-inventorychestlocatorenhancer
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1406"></a>
### DTMAPI.Abstractions.ChestLocatorEnhancerState.HookInstalled

- Symbol: `DTMAPI.Abstractions.ChestLocatorEnhancerState.HookInstalled`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:856`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.GetAvailableInventories and native LinearInventory Count/Cost consumers.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0009-030-chest-locator-enhancer.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-163226
  - hook-map entry: docs/hook-map/README.md#hook-inventorychestlocatorenhancer
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1407"></a>
### DTMAPI.Abstractions.ChestLocatorEnhancerState.ExtensionApplications

- Symbol: `DTMAPI.Abstractions.ChestLocatorEnhancerState.ExtensionApplications`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:857`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.GetAvailableInventories and native LinearInventory Count/Cost consumers.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0009-030-chest-locator-enhancer.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-163226
  - hook-map entry: docs/hook-map/README.md#hook-inventorychestlocatorenhancer
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1408"></a>
### DTMAPI.Abstractions.ChestLocatorEnhancerState.LastBaseInventoryCount

- Symbol: `DTMAPI.Abstractions.ChestLocatorEnhancerState.LastBaseInventoryCount`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:858`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.GetAvailableInventories and native LinearInventory Count/Cost consumers.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0009-030-chest-locator-enhancer.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-163226
  - hook-map entry: docs/hook-map/README.md#hook-inventorychestlocatorenhancer
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1409"></a>
### DTMAPI.Abstractions.ChestLocatorEnhancerState.LastAppendedInventoryCount

- Symbol: `DTMAPI.Abstractions.ChestLocatorEnhancerState.LastAppendedInventoryCount`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:859`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.GetAvailableInventories and native LinearInventory Count/Cost consumers.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0009-030-chest-locator-enhancer.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-163226
  - hook-map entry: docs/hook-map/README.md#hook-inventorychestlocatorenhancer
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1410"></a>
### DTMAPI.Abstractions.ChestLocatorEnhancerState.LastScannedRootCount

- Symbol: `DTMAPI.Abstractions.ChestLocatorEnhancerState.LastScannedRootCount`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:860`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.GetAvailableInventories and native LinearInventory Count/Cost consumers.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0009-030-chest-locator-enhancer.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-163226
  - hook-map entry: docs/hook-map/README.md#hook-inventorychestlocatorenhancer
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1411"></a>
### DTMAPI.Abstractions.ChestLocatorEnhancerState.LastScannedEquipmentCount

- Symbol: `DTMAPI.Abstractions.ChestLocatorEnhancerState.LastScannedEquipmentCount`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:861`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.GetAvailableInventories and native LinearInventory Count/Cost consumers.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0009-030-chest-locator-enhancer.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-163226
  - hook-map entry: docs/hook-map/README.md#hook-inventorychestlocatorenhancer
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1412"></a>
### DTMAPI.Abstractions.ChestLocatorEnhancerState.LastSharedCaseCount

- Symbol: `DTMAPI.Abstractions.ChestLocatorEnhancerState.LastSharedCaseCount`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:862`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.GetAvailableInventories and native LinearInventory Count/Cost consumers.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0009-030-chest-locator-enhancer.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-163226
  - hook-map entry: docs/hook-map/README.md#hook-inventorychestlocatorenhancer
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1413"></a>
### DTMAPI.Abstractions.ChestLocatorEnhancerState.LastSharedStorageBoxCount

- Symbol: `DTMAPI.Abstractions.ChestLocatorEnhancerState.LastSharedStorageBoxCount`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:863`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.GetAvailableInventories and native LinearInventory Count/Cost consumers.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0009-030-chest-locator-enhancer.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-163226
  - hook-map entry: docs/hook-map/README.md#hook-inventorychestlocatorenhancer
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1414"></a>
### DTMAPI.Abstractions.ChestLocatorEnhancerState.Status

- Symbol: `DTMAPI.Abstractions.ChestLocatorEnhancerState.Status`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:864`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.GetAvailableInventories and native LinearInventory Count/Cost consumers.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0009-030-chest-locator-enhancer.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-163226
  - hook-map entry: docs/hook-map/README.md#hook-inventorychestlocatorenhancer
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1415"></a>
### DTMAPI.Abstractions.ChestLocatorEnhancerState.LastMessage

- Symbol: `DTMAPI.Abstractions.ChestLocatorEnhancerState.LastMessage`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:865`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ArchiveDataHandle.GetAvailableInventories and native LinearInventory Count/Cost consumers.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0009-030-chest-locator-enhancer.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-163226
  - hook-map entry: docs/hook-map/README.md#hook-inventorychestlocatorenhancer
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1416"></a>
### DTMAPI.Abstractions.StrongPlantingGunOptions

- Symbol: `DTMAPI.Abstractions.StrongPlantingGunOptions`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:868`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: ItemFarmingGun construction/use/UI transfer and PlantBasin native seed/film/fertilizer checks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0010-030-strong-planting-gun.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-170334
  - hook-map entry: docs/hook-map/README.md#hook-farmingstrongplantinggun
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1417"></a>
### DTMAPI.Abstractions.StrongPlantingGunOptions.Enabled

- Symbol: `DTMAPI.Abstractions.StrongPlantingGunOptions.Enabled`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:870`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ItemFarmingGun construction/use/UI transfer and PlantBasin native seed/film/fertilizer checks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0010-030-strong-planting-gun.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-170334
  - hook-map entry: docs/hook-map/README.md#hook-farmingstrongplantinggun
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1418"></a>
### DTMAPI.Abstractions.StrongPlantingGunOptions.SlotCount

- Symbol: `DTMAPI.Abstractions.StrongPlantingGunOptions.SlotCount`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:871`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ItemFarmingGun construction/use/UI transfer and PlantBasin native seed/film/fertilizer checks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0010-030-strong-planting-gun.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-170334
  - hook-map entry: docs/hook-map/README.md#hook-farmingstrongplantinggun
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1419"></a>
### DTMAPI.Abstractions.StrongPlantingGunOptions.IncludeSeeds

- Symbol: `DTMAPI.Abstractions.StrongPlantingGunOptions.IncludeSeeds`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:872`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ItemFarmingGun construction/use/UI transfer and PlantBasin native seed/film/fertilizer checks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0010-030-strong-planting-gun.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-170334
  - hook-map entry: docs/hook-map/README.md#hook-farmingstrongplantinggun
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1420"></a>
### DTMAPI.Abstractions.StrongPlantingGunOptions.IncludeFilms

- Symbol: `DTMAPI.Abstractions.StrongPlantingGunOptions.IncludeFilms`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:873`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ItemFarmingGun construction/use/UI transfer and PlantBasin native seed/film/fertilizer checks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0010-030-strong-planting-gun.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-170334
  - hook-map entry: docs/hook-map/README.md#hook-farmingstrongplantinggun
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1421"></a>
### DTMAPI.Abstractions.StrongPlantingGunOptions.IncludeFertilizers

- Symbol: `DTMAPI.Abstractions.StrongPlantingGunOptions.IncludeFertilizers`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:874`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ItemFarmingGun construction/use/UI transfer and PlantBasin native seed/film/fertilizer checks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0010-030-strong-planting-gun.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-170334
  - hook-map entry: docs/hook-map/README.md#hook-farmingstrongplantinggun
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1422"></a>
### DTMAPI.Abstractions.StrongPlantingGunOptions.IncludeWater

- Symbol: `DTMAPI.Abstractions.StrongPlantingGunOptions.IncludeWater`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:875`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ItemFarmingGun construction/use/UI transfer and PlantBasin native seed/film/fertilizer checks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0010-030-strong-planting-gun.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-170334
  - hook-map entry: docs/hook-map/README.md#hook-farmingstrongplantinggun
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1423"></a>
### DTMAPI.Abstractions.StrongPlantingGunOptions.VerboseLogging

- Symbol: `DTMAPI.Abstractions.StrongPlantingGunOptions.VerboseLogging`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:876`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ItemFarmingGun construction/use/UI transfer and PlantBasin native seed/film/fertilizer checks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0010-030-strong-planting-gun.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-170334
  - hook-map entry: docs/hook-map/README.md#hook-farmingstrongplantinggun
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1424"></a>
### DTMAPI.Abstractions.StrongPlantingGunRegisterResult

- Symbol: `DTMAPI.Abstractions.StrongPlantingGunRegisterResult`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:879`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: ItemFarmingGun construction/use/UI transfer and PlantBasin native seed/film/fertilizer checks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0010-030-strong-planting-gun.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-170334
  - hook-map entry: docs/hook-map/README.md#hook-farmingstrongplantinggun
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1425"></a>
### DTMAPI.Abstractions.StrongPlantingGunRegisterResult.Success

- Symbol: `DTMAPI.Abstractions.StrongPlantingGunRegisterResult.Success`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:881`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ItemFarmingGun construction/use/UI transfer and PlantBasin native seed/film/fertilizer checks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0010-030-strong-planting-gun.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-170334
  - hook-map entry: docs/hook-map/README.md#hook-farmingstrongplantinggun
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1426"></a>
### DTMAPI.Abstractions.StrongPlantingGunRegisterResult.OwnerId

- Symbol: `DTMAPI.Abstractions.StrongPlantingGunRegisterResult.OwnerId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:882`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ItemFarmingGun construction/use/UI transfer and PlantBasin native seed/film/fertilizer checks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0010-030-strong-planting-gun.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-170334
  - hook-map entry: docs/hook-map/README.md#hook-farmingstrongplantinggun
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1427"></a>
### DTMAPI.Abstractions.StrongPlantingGunRegisterResult.Enabled

- Symbol: `DTMAPI.Abstractions.StrongPlantingGunRegisterResult.Enabled`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:883`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ItemFarmingGun construction/use/UI transfer and PlantBasin native seed/film/fertilizer checks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0010-030-strong-planting-gun.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-170334
  - hook-map entry: docs/hook-map/README.md#hook-farmingstrongplantinggun
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1428"></a>
### DTMAPI.Abstractions.StrongPlantingGunRegisterResult.SlotCount

- Symbol: `DTMAPI.Abstractions.StrongPlantingGunRegisterResult.SlotCount`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:884`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ItemFarmingGun construction/use/UI transfer and PlantBasin native seed/film/fertilizer checks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0010-030-strong-planting-gun.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-170334
  - hook-map entry: docs/hook-map/README.md#hook-farmingstrongplantinggun
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1429"></a>
### DTMAPI.Abstractions.StrongPlantingGunRegisterResult.ToolHookInstalled

- Symbol: `DTMAPI.Abstractions.StrongPlantingGunRegisterResult.ToolHookInstalled`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:885`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ItemFarmingGun construction/use/UI transfer and PlantBasin native seed/film/fertilizer checks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0010-030-strong-planting-gun.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-170334
  - hook-map entry: docs/hook-map/README.md#hook-farmingstrongplantinggun
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1430"></a>
### DTMAPI.Abstractions.StrongPlantingGunRegisterResult.UiHookInstalled

- Symbol: `DTMAPI.Abstractions.StrongPlantingGunRegisterResult.UiHookInstalled`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:886`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ItemFarmingGun construction/use/UI transfer and PlantBasin native seed/film/fertilizer checks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0010-030-strong-planting-gun.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-170334
  - hook-map entry: docs/hook-map/README.md#hook-farmingstrongplantinggun
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1431"></a>
### DTMAPI.Abstractions.StrongPlantingGunRegisterResult.FailureReason

- Symbol: `DTMAPI.Abstractions.StrongPlantingGunRegisterResult.FailureReason`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:887`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ItemFarmingGun construction/use/UI transfer and PlantBasin native seed/film/fertilizer checks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0010-030-strong-planting-gun.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-170334
  - hook-map entry: docs/hook-map/README.md#hook-farmingstrongplantinggun
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1432"></a>
### DTMAPI.Abstractions.StrongPlantingGunRegisterResult.Message

- Symbol: `DTMAPI.Abstractions.StrongPlantingGunRegisterResult.Message`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:888`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ItemFarmingGun construction/use/UI transfer and PlantBasin native seed/film/fertilizer checks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0010-030-strong-planting-gun.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-170334
  - hook-map entry: docs/hook-map/README.md#hook-farmingstrongplantinggun
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1433"></a>
### DTMAPI.Abstractions.StrongPlantingGunState

- Symbol: `DTMAPI.Abstractions.StrongPlantingGunState`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:891`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: ItemFarmingGun construction/use/UI transfer and PlantBasin native seed/film/fertilizer checks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0010-030-strong-planting-gun.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-170334
  - hook-map entry: docs/hook-map/README.md#hook-farmingstrongplantinggun
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1434"></a>
### DTMAPI.Abstractions.StrongPlantingGunState.OwnerId

- Symbol: `DTMAPI.Abstractions.StrongPlantingGunState.OwnerId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:893`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ItemFarmingGun construction/use/UI transfer and PlantBasin native seed/film/fertilizer checks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0010-030-strong-planting-gun.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-170334
  - hook-map entry: docs/hook-map/README.md#hook-farmingstrongplantinggun
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1435"></a>
### DTMAPI.Abstractions.StrongPlantingGunState.IsConfigured

- Symbol: `DTMAPI.Abstractions.StrongPlantingGunState.IsConfigured`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:894`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ItemFarmingGun construction/use/UI transfer and PlantBasin native seed/film/fertilizer checks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0010-030-strong-planting-gun.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-170334
  - hook-map entry: docs/hook-map/README.md#hook-farmingstrongplantinggun
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1436"></a>
### DTMAPI.Abstractions.StrongPlantingGunState.Enabled

- Symbol: `DTMAPI.Abstractions.StrongPlantingGunState.Enabled`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:895`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ItemFarmingGun construction/use/UI transfer and PlantBasin native seed/film/fertilizer checks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0010-030-strong-planting-gun.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-170334
  - hook-map entry: docs/hook-map/README.md#hook-farmingstrongplantinggun
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1437"></a>
### DTMAPI.Abstractions.StrongPlantingGunState.SlotCount

- Symbol: `DTMAPI.Abstractions.StrongPlantingGunState.SlotCount`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:896`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ItemFarmingGun construction/use/UI transfer and PlantBasin native seed/film/fertilizer checks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0010-030-strong-planting-gun.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-170334
  - hook-map entry: docs/hook-map/README.md#hook-farmingstrongplantinggun
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1438"></a>
### DTMAPI.Abstractions.StrongPlantingGunState.ToolHookInstalled

- Symbol: `DTMAPI.Abstractions.StrongPlantingGunState.ToolHookInstalled`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:897`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ItemFarmingGun construction/use/UI transfer and PlantBasin native seed/film/fertilizer checks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0010-030-strong-planting-gun.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-170334
  - hook-map entry: docs/hook-map/README.md#hook-farmingstrongplantinggun
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1439"></a>
### DTMAPI.Abstractions.StrongPlantingGunState.UiHookInstalled

- Symbol: `DTMAPI.Abstractions.StrongPlantingGunState.UiHookInstalled`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:898`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ItemFarmingGun construction/use/UI transfer and PlantBasin native seed/film/fertilizer checks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0010-030-strong-planting-gun.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-170334
  - hook-map entry: docs/hook-map/README.md#hook-farmingstrongplantinggun
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1440"></a>
### DTMAPI.Abstractions.StrongPlantingGunState.ExpandedGunCount

- Symbol: `DTMAPI.Abstractions.StrongPlantingGunState.ExpandedGunCount`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:899`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ItemFarmingGun construction/use/UI transfer and PlantBasin native seed/film/fertilizer checks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0010-030-strong-planting-gun.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-170334
  - hook-map entry: docs/hook-map/README.md#hook-farmingstrongplantinggun
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1441"></a>
### DTMAPI.Abstractions.StrongPlantingGunState.LastVisitedEquipmentCount

- Symbol: `DTMAPI.Abstractions.StrongPlantingGunState.LastVisitedEquipmentCount`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:900`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ItemFarmingGun construction/use/UI transfer and PlantBasin native seed/film/fertilizer checks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0010-030-strong-planting-gun.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-170334
  - hook-map entry: docs/hook-map/README.md#hook-farmingstrongplantinggun
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1442"></a>
### DTMAPI.Abstractions.StrongPlantingGunState.LastSeedActions

- Symbol: `DTMAPI.Abstractions.StrongPlantingGunState.LastSeedActions`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:901`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ItemFarmingGun construction/use/UI transfer and PlantBasin native seed/film/fertilizer checks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0010-030-strong-planting-gun.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-170334
  - hook-map entry: docs/hook-map/README.md#hook-farmingstrongplantinggun
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1443"></a>
### DTMAPI.Abstractions.StrongPlantingGunState.LastFilmActions

- Symbol: `DTMAPI.Abstractions.StrongPlantingGunState.LastFilmActions`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:902`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ItemFarmingGun construction/use/UI transfer and PlantBasin native seed/film/fertilizer checks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0010-030-strong-planting-gun.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-170334
  - hook-map entry: docs/hook-map/README.md#hook-farmingstrongplantinggun
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1444"></a>
### DTMAPI.Abstractions.StrongPlantingGunState.LastFertilizerActions

- Symbol: `DTMAPI.Abstractions.StrongPlantingGunState.LastFertilizerActions`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:903`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ItemFarmingGun construction/use/UI transfer and PlantBasin native seed/film/fertilizer checks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0010-030-strong-planting-gun.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-170334
  - hook-map entry: docs/hook-map/README.md#hook-farmingstrongplantinggun
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1445"></a>
### DTMAPI.Abstractions.StrongPlantingGunState.LastWaterActions

- Symbol: `DTMAPI.Abstractions.StrongPlantingGunState.LastWaterActions`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:904`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ItemFarmingGun construction/use/UI transfer and PlantBasin native seed/film/fertilizer checks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0010-030-strong-planting-gun.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-170334
  - hook-map entry: docs/hook-map/README.md#hook-farmingstrongplantinggun
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1446"></a>
### DTMAPI.Abstractions.StrongPlantingGunState.LastConsumedItemCount

- Symbol: `DTMAPI.Abstractions.StrongPlantingGunState.LastConsumedItemCount`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:905`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ItemFarmingGun construction/use/UI transfer and PlantBasin native seed/film/fertilizer checks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0010-030-strong-planting-gun.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-170334
  - hook-map entry: docs/hook-map/README.md#hook-farmingstrongplantinggun
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1447"></a>
### DTMAPI.Abstractions.StrongPlantingGunState.Status

- Symbol: `DTMAPI.Abstractions.StrongPlantingGunState.Status`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:906`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ItemFarmingGun construction/use/UI transfer and PlantBasin native seed/film/fertilizer checks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0010-030-strong-planting-gun.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-170334
  - hook-map entry: docs/hook-map/README.md#hook-farmingstrongplantinggun
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1448"></a>
### DTMAPI.Abstractions.StrongPlantingGunState.LastMessage

- Symbol: `DTMAPI.Abstractions.StrongPlantingGunState.LastMessage`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:907`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ItemFarmingGun construction/use/UI transfer and PlantBasin native seed/film/fertilizer checks.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0010-030-strong-planting-gun.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-170334
  - hook-map entry: docs/hook-map/README.md#hook-farmingstrongplantinggun
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1449"></a>
### DTMAPI.Abstractions.EquipmentSlotsRegisterResult

- Symbol: `DTMAPI.Abstractions.EquipmentSlotsRegisterResult`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:910`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1450"></a>
### DTMAPI.Abstractions.EquipmentSlotsRegisterResult.Success

- Symbol: `DTMAPI.Abstractions.EquipmentSlotsRegisterResult.Success`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:912`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1451"></a>
### DTMAPI.Abstractions.EquipmentSlotsRegisterResult.OwnerId

- Symbol: `DTMAPI.Abstractions.EquipmentSlotsRegisterResult.OwnerId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:913`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1452"></a>
### DTMAPI.Abstractions.EquipmentSlotsRegisterResult.ExtraAttributeSlots

- Symbol: `DTMAPI.Abstractions.EquipmentSlotsRegisterResult.ExtraAttributeSlots`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:914`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1453"></a>
### DTMAPI.Abstractions.EquipmentSlotsRegisterResult.FailureReason

- Symbol: `DTMAPI.Abstractions.EquipmentSlotsRegisterResult.FailureReason`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:915`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1454"></a>
### DTMAPI.Abstractions.EquipmentSlotsRegisterResult.Message

- Symbol: `DTMAPI.Abstractions.EquipmentSlotsRegisterResult.Message`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:916`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1455"></a>
### DTMAPI.Abstractions.EquipmentSlotsState

- Symbol: `DTMAPI.Abstractions.EquipmentSlotsState`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:919`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1456"></a>
### DTMAPI.Abstractions.EquipmentSlotsState.OwnerId

- Symbol: `DTMAPI.Abstractions.EquipmentSlotsState.OwnerId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:921`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1457"></a>
### DTMAPI.Abstractions.EquipmentSlotsState.IsConfigured

- Symbol: `DTMAPI.Abstractions.EquipmentSlotsState.IsConfigured`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:922`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1458"></a>
### DTMAPI.Abstractions.EquipmentSlotsState.RuntimeUiHookInstalled

- Symbol: `DTMAPI.Abstractions.EquipmentSlotsState.RuntimeUiHookInstalled`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:923`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1459"></a>
### DTMAPI.Abstractions.EquipmentSlotsState.RuntimeStatsHookInstalled

- Symbol: `DTMAPI.Abstractions.EquipmentSlotsState.RuntimeStatsHookInstalled`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:924`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1460"></a>
### DTMAPI.Abstractions.EquipmentSlotsState.ExtraAttributeSlots

- Symbol: `DTMAPI.Abstractions.EquipmentSlotsState.ExtraAttributeSlots`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:925`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1461"></a>
### DTMAPI.Abstractions.EquipmentSlotsState.StoredItemCount

- Symbol: `DTMAPI.Abstractions.EquipmentSlotsState.StoredItemCount`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:926`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1462"></a>
### DTMAPI.Abstractions.EquipmentSlotsState.AppliedItemCount

- Symbol: `DTMAPI.Abstractions.EquipmentSlotsState.AppliedItemCount`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:927`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1463"></a>
### DTMAPI.Abstractions.EquipmentSlotsState.PreserveVanillaVisualSlots

- Symbol: `DTMAPI.Abstractions.EquipmentSlotsState.PreserveVanillaVisualSlots`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:928`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1464"></a>
### DTMAPI.Abstractions.EquipmentSlotsState.ExtraSlotsAffectVisuals

- Symbol: `DTMAPI.Abstractions.EquipmentSlotsState.ExtraSlotsAffectVisuals`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:929`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1465"></a>
### DTMAPI.Abstractions.EquipmentSlotsState.SafeUnequipOnDisable

- Symbol: `DTMAPI.Abstractions.EquipmentSlotsState.SafeUnequipOnDisable`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:930`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1466"></a>
### DTMAPI.Abstractions.EquipmentSlotsState.StatsRefreshCount

- Symbol: `DTMAPI.Abstractions.EquipmentSlotsState.StatsRefreshCount`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:931`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1467"></a>
### DTMAPI.Abstractions.EquipmentSlotsState.PendingRecoveryCount

- Symbol: `DTMAPI.Abstractions.EquipmentSlotsState.PendingRecoveryCount`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:932`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1468"></a>
### DTMAPI.Abstractions.EquipmentSlotsState.LastEquippedSlotId

- Symbol: `DTMAPI.Abstractions.EquipmentSlotsState.LastEquippedSlotId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:933`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1469"></a>
### DTMAPI.Abstractions.EquipmentSlotsState.LastEquippedItemId

- Symbol: `DTMAPI.Abstractions.EquipmentSlotsState.LastEquippedItemId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:934`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1470"></a>
### DTMAPI.Abstractions.EquipmentSlotsState.Status

- Symbol: `DTMAPI.Abstractions.EquipmentSlotsState.Status`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:935`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1471"></a>
### DTMAPI.Abstractions.EquipmentSlotsState.LastRecoveryMessage

- Symbol: `DTMAPI.Abstractions.EquipmentSlotsState.LastRecoveryMessage`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:936`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1472"></a>
### DTMAPI.Abstractions.EquipmentSlotInfo

- Symbol: `DTMAPI.Abstractions.EquipmentSlotInfo`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:939`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1473"></a>
### DTMAPI.Abstractions.EquipmentSlotInfo.OwnerId

- Symbol: `DTMAPI.Abstractions.EquipmentSlotInfo.OwnerId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:941`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1474"></a>
### DTMAPI.Abstractions.EquipmentSlotInfo.SlotId

- Symbol: `DTMAPI.Abstractions.EquipmentSlotInfo.SlotId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:942`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1475"></a>
### DTMAPI.Abstractions.EquipmentSlotInfo.Index

- Symbol: `DTMAPI.Abstractions.EquipmentSlotInfo.Index`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:943`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1476"></a>
### DTMAPI.Abstractions.EquipmentSlotInfo.ItemId

- Symbol: `DTMAPI.Abstractions.EquipmentSlotInfo.ItemId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:944`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1477"></a>
### DTMAPI.Abstractions.EquipmentSlotInfo.DisplayName

- Symbol: `DTMAPI.Abstractions.EquipmentSlotInfo.DisplayName`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:945`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1478"></a>
### DTMAPI.Abstractions.EquipmentSlotInfo.IsOccupied

- Symbol: `DTMAPI.Abstractions.EquipmentSlotInfo.IsOccupied`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:946`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1479"></a>
### DTMAPI.Abstractions.EquipmentSlotInfo.IsApplied

- Symbol: `DTMAPI.Abstractions.EquipmentSlotInfo.IsApplied`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:947`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1480"></a>
### DTMAPI.Abstractions.EquipmentSlotInfo.IsRecoverable

- Symbol: `DTMAPI.Abstractions.EquipmentSlotInfo.IsRecoverable`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:948`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1481"></a>
### DTMAPI.Abstractions.EquipmentSlotInfo.AttributeOnly

- Symbol: `DTMAPI.Abstractions.EquipmentSlotInfo.AttributeOnly`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:949`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1482"></a>
### DTMAPI.Abstractions.EquipmentSlotInfo.AffectsVisuals

- Symbol: `DTMAPI.Abstractions.EquipmentSlotInfo.AffectsVisuals`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:950`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1483"></a>
### DTMAPI.Abstractions.EquipmentSlotInfo.LastMessage

- Symbol: `DTMAPI.Abstractions.EquipmentSlotInfo.LastMessage`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:951`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1484"></a>
### DTMAPI.Abstractions.EquipmentSlotEquipResult

- Symbol: `DTMAPI.Abstractions.EquipmentSlotEquipResult`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:954`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1485"></a>
### DTMAPI.Abstractions.EquipmentSlotEquipResult.Success

- Symbol: `DTMAPI.Abstractions.EquipmentSlotEquipResult.Success`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:956`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1486"></a>
### DTMAPI.Abstractions.EquipmentSlotEquipResult.OwnerId

- Symbol: `DTMAPI.Abstractions.EquipmentSlotEquipResult.OwnerId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:957`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1487"></a>
### DTMAPI.Abstractions.EquipmentSlotEquipResult.SlotId

- Symbol: `DTMAPI.Abstractions.EquipmentSlotEquipResult.SlotId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:958`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1488"></a>
### DTMAPI.Abstractions.EquipmentSlotEquipResult.ItemId

- Symbol: `DTMAPI.Abstractions.EquipmentSlotEquipResult.ItemId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:959`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1489"></a>
### DTMAPI.Abstractions.EquipmentSlotEquipResult.DisplayName

- Symbol: `DTMAPI.Abstractions.EquipmentSlotEquipResult.DisplayName`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:960`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1490"></a>
### DTMAPI.Abstractions.EquipmentSlotEquipResult.BeforeBackpackCount

- Symbol: `DTMAPI.Abstractions.EquipmentSlotEquipResult.BeforeBackpackCount`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:961`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1491"></a>
### DTMAPI.Abstractions.EquipmentSlotEquipResult.AfterBackpackCount

- Symbol: `DTMAPI.Abstractions.EquipmentSlotEquipResult.AfterBackpackCount`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:962`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1492"></a>
### DTMAPI.Abstractions.EquipmentSlotEquipResult.RecoveredCount

- Symbol: `DTMAPI.Abstractions.EquipmentSlotEquipResult.RecoveredCount`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:963`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1493"></a>
### DTMAPI.Abstractions.EquipmentSlotEquipResult.FailureReason

- Symbol: `DTMAPI.Abstractions.EquipmentSlotEquipResult.FailureReason`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:964`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1494"></a>
### DTMAPI.Abstractions.EquipmentSlotEquipResult.Message

- Symbol: `DTMAPI.Abstractions.EquipmentSlotEquipResult.Message`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:965`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1495"></a>
### DTMAPI.Abstractions.EquipmentSlotsRecoveryResult

- Symbol: `DTMAPI.Abstractions.EquipmentSlotsRecoveryResult`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:968`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1496"></a>
### DTMAPI.Abstractions.EquipmentSlotsRecoveryResult.Success

- Symbol: `DTMAPI.Abstractions.EquipmentSlotsRecoveryResult.Success`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:970`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1497"></a>
### DTMAPI.Abstractions.EquipmentSlotsRecoveryResult.OwnerId

- Symbol: `DTMAPI.Abstractions.EquipmentSlotsRecoveryResult.OwnerId`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:971`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1498"></a>
### DTMAPI.Abstractions.EquipmentSlotsRecoveryResult.RecoveredCount

- Symbol: `DTMAPI.Abstractions.EquipmentSlotsRecoveryResult.RecoveredCount`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:972`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1499"></a>
### DTMAPI.Abstractions.EquipmentSlotsRecoveryResult.FailureReason

- Symbol: `DTMAPI.Abstractions.EquipmentSlotsRecoveryResult.FailureReason`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:973`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。

<a id="sym-1500"></a>
### DTMAPI.Abstractions.EquipmentSlotsRecoveryResult.Message

- Symbol: `DTMAPI.Abstractions.EquipmentSlotsRecoveryResult.Message`
- Current marker: `not-in-matrix`
- Review advice: 保持 experimental；文档写清 DTMAPI sidecar 与 native slot 的边界
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:974`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Mixed: AgentEquipmentManager/AccessoriesBar/native backpack placement plus DTMAPI sidecar storage.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150834
  - hook-map entry: docs/hook-map/README.md#hook-playerequipmentslotsapi
  - code path: No direct evidence
- Result: Gap
- Recommendation: 普通 mod 依赖会把 DTMAPI sidecar slot 当作 native equipment slot；风险是存档事务、恢复、跨存档清理和 UI clone 状态不同步。
