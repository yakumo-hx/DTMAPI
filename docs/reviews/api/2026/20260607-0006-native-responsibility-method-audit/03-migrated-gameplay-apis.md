# Migrated Gameplay APIs Symbol-Level Native Responsibility Audit

- Audit: `20260607-0006-native-responsibility-method-audit`
- Scope: public Abstractions symbols in this volume; docs-only review, no implementation goal.
- Locator rule: `Symbol` is the stable key; `Declaration` line numbers are secondary and may drift.

## Top Risks

- `IFishingAutomationApi` spans multiple fishing state owners; partial smoke success must not become stable automation semantics.
- `IAnimalViewerApi` is UI data only; it must not be documented as animal production lifecycle control.
- `IActionSpeedApi` and options expose broad timing promises; each path needs its native responsibility function kept separate.
- `IItemTooltipApi` decorates UI text only and can conflict with multiple tooltip providers.

## Audit Blocks

<a id="sym-0795"></a>
### DTMAPI.Abstractions.IActionCompletionApi

- Symbol: `DTMAPI.Abstractions.IActionCompletionApi`
- Current marker: `experimental`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:7`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:17`
- Native owner: ToolCollider.HandleTools, DungeonResourceRenderer, Equipment.DecoratedInteract, and VegetationRenderer exception path.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:69 `IActionCompletionApi.Configure` (experimental)
  - update record: docs/updates/2026/20260531-0004-oneaction-resource-hit-smoke.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260531-032318
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#oneaction-001
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:17
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0796"></a>
### DTMAPI.Abstractions.IActionCompletionApi.Configure(IManifest owner, ActionCompletionOptions options)

- Symbol: `DTMAPI.Abstractions.IActionCompletionApi.Configure(IManifest owner, ActionCompletionOptions options)`
- Current marker: `experimental`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:9`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:408`
- Native owner: ToolCollider.HandleTools, DungeonResourceRenderer, Equipment.DecoratedInteract, and VegetationRenderer exception path.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:69 `IActionCompletionApi.Configure` (experimental)
  - update record: docs/updates/2026/20260531-0004-oneaction-resource-hit-smoke.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260531-032318
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#oneaction-001
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:408
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0797"></a>
### DTMAPI.Abstractions.IActionCompletionApi.GetStatus(string uniqueId)

- Symbol: `DTMAPI.Abstractions.IActionCompletionApi.GetStatus(string uniqueId)`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:10`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:416`
- Native owner: ToolCollider.HandleTools, DungeonResourceRenderer, Equipment.DecoratedInteract, and VegetationRenderer exception path.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:69 `IActionCompletionApi.Configure` (experimental)
  - update record: docs/updates/2026/20260531-0004-oneaction-resource-hit-smoke.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260531-032318
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#oneaction-001
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:416
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0798"></a>
### DTMAPI.Abstractions.IFishingAutomationApi

- Symbol: `DTMAPI.Abstractions.IFishingAutomationApi`
- Current marker: `experimental`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:14`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:17`
- Native owner: BodyController.UseFishRod, AgentStateFishingReady/Cast/Wait/Pull, and FishingGameScrollBar.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:71 `IFishingAutomationApi.Configure/SetEnabled` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150631
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#autofish-001
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:17
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0799"></a>
### DTMAPI.Abstractions.IFishingAutomationApi.Configure(IManifest owner, FishingAutomationOptions options)

- Symbol: `DTMAPI.Abstractions.IFishingAutomationApi.Configure(IManifest owner, FishingAutomationOptions options)`
- Current marker: `experimental`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:16`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:408`
- Native owner: BodyController.UseFishRod, AgentStateFishingReady/Cast/Wait/Pull, and FishingGameScrollBar.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:71 `IFishingAutomationApi.Configure/SetEnabled` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150631
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#autofish-001
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:408
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0800"></a>
### DTMAPI.Abstractions.IFishingAutomationApi.SetEnabled(IManifest owner, bool enabled, string reason)

- Symbol: `DTMAPI.Abstractions.IFishingAutomationApi.SetEnabled(IManifest owner, bool enabled, string reason)`
- Current marker: `experimental`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:17`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:476`
- Native owner: BodyController.UseFishRod, AgentStateFishingReady/Cast/Wait/Pull, and FishingGameScrollBar.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:71 `IFishingAutomationApi.Configure/SetEnabled` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150631
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#autofish-001
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:476
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0801"></a>
### DTMAPI.Abstractions.IFishingAutomationApi.GetState(string uniqueId)

- Symbol: `DTMAPI.Abstractions.IFishingAutomationApi.GetState(string uniqueId)`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:18`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:493`
- Native owner: BodyController.UseFishRod, AgentStateFishingReady/Cast/Wait/Pull, and FishingGameScrollBar.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:71 `IFishingAutomationApi.Configure/SetEnabled` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150631
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#autofish-001
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:493
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0802"></a>
### DTMAPI.Abstractions.IFishingAutomationApi.GetStatus(string uniqueId)

- Symbol: `DTMAPI.Abstractions.IFishingAutomationApi.GetStatus(string uniqueId)`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:19`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:515`
- Native owner: BodyController.UseFishRod, AgentStateFishingReady/Cast/Wait/Pull, and FishingGameScrollBar.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:71 `IFishingAutomationApi.Configure/SetEnabled` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150631
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#autofish-001
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:515
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0803"></a>
### DTMAPI.Abstractions.IActionSpeedApi

- Symbol: `DTMAPI.Abstractions.IActionSpeedApi`
- Current marker: `experimental`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:23`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:17`
- Native owner: AgentStateTool/Interact/Eat, BodyController/tool animators, and item use/fill paths.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:70 `IActionSpeedApi.Configure/GetStatus` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150543
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#actionspeed-002
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:17
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0804"></a>
### DTMAPI.Abstractions.IActionSpeedApi.Configure(IManifest owner, ActionSpeedOptions options)

- Symbol: `DTMAPI.Abstractions.IActionSpeedApi.Configure(IManifest owner, ActionSpeedOptions options)`
- Current marker: `experimental`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:25`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:408`
- Native owner: AgentStateTool/Interact/Eat, BodyController/tool animators, and item use/fill paths.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:70 `IActionSpeedApi.Configure/GetStatus` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150543
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#actionspeed-002
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:408
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0805"></a>
### DTMAPI.Abstractions.IActionSpeedApi.GetStatus(string uniqueId)

- Symbol: `DTMAPI.Abstractions.IActionSpeedApi.GetStatus(string uniqueId)`
- Current marker: `experimental`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:26`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:431`
- Native owner: AgentStateTool/Interact/Eat, BodyController/tool animators, and item use/fill paths.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:70 `IActionSpeedApi.Configure/GetStatus` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150543
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#actionspeed-002
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:431
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0806"></a>
### DTMAPI.Abstractions.IItemTooltipApi

- Symbol: `DTMAPI.Abstractions.IItemTooltipApi`
- Current marker: `experimental`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:30`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:17`
- Native owner: Item title/description/detail UI getters only; no native item data owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:72 `IItemTooltipApi.ConfigureFishRoeProvider` (experimental)
  - update record: docs/updates/2026/20260601-0002-y-console-021-migrated-mod-polish.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-150808
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#fishroe-001
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:17
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0807"></a>
### DTMAPI.Abstractions.IItemTooltipApi.ConfigureFishRoeProvider(IManifest owner, FishRoeTooltipOptions options, Func<string, FishRoeDisplayInfo?> lookup)

- Symbol: `DTMAPI.Abstractions.IItemTooltipApi.ConfigureFishRoeProvider(IManifest owner, FishRoeTooltipOptions options, Func<string, FishRoeDisplayInfo?> lookup)`
- Current marker: `experimental`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:32`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:536`
- Native owner: Item title/description/detail UI getters only; no native item data owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:72 `IItemTooltipApi.ConfigureFishRoeProvider` (experimental)
  - update record: docs/updates/2026/20260601-0002-y-console-021-migrated-mod-polish.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-150808
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#fishroe-001
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:536
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0808"></a>
### DTMAPI.Abstractions.IItemTooltipApi.GetStatus(string uniqueId)

- Symbol: `DTMAPI.Abstractions.IItemTooltipApi.GetStatus(string uniqueId)`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:33`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:546`
- Native owner: Item title/description/detail UI getters only; no native item data owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:72 `IItemTooltipApi.ConfigureFishRoeProvider` (experimental)
  - update record: docs/updates/2026/20260601-0002-y-console-021-migrated-mod-polish.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-150808
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#fishroe-001
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:546
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0809"></a>
### DTMAPI.Abstractions.IAnimalViewerApi

- Symbol: `DTMAPI.Abstractions.IAnimalViewerApi`
- Current marker: `experimental`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:37`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:17`
- Native owner: AnimalViewer.Show / AnimalFullInfoData UI data; no animal production lifecycle owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:73 `IAnimalViewerApi.ConfigureSpecialProduceProgress` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150721
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#animal-001
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:17
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0810"></a>
### DTMAPI.Abstractions.IAnimalViewerApi.ConfigureSpecialProduceProgress(IManifest owner, AnimalHusbandryProgressOptions options)

- Symbol: `DTMAPI.Abstractions.IAnimalViewerApi.ConfigureSpecialProduceProgress(IManifest owner, AnimalHusbandryProgressOptions options)`
- Current marker: `experimental`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:39`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:553`
- Native owner: AnimalViewer.Show / AnimalFullInfoData UI data; no animal production lifecycle owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:73 `IAnimalViewerApi.ConfigureSpecialProduceProgress` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150721
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#animal-001
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:553
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0811"></a>
### DTMAPI.Abstractions.IAnimalViewerApi.GetStatus(string uniqueId)

- Symbol: `DTMAPI.Abstractions.IAnimalViewerApi.GetStatus(string uniqueId)`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:40`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:561`
- Native owner: AnimalViewer.Show / AnimalFullInfoData UI data; no animal production lifecycle owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:73 `IAnimalViewerApi.ConfigureSpecialProduceProgress` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150721
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#animal-001
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:561
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1501"></a>
### DTMAPI.Abstractions.ActionCompletionOptions

- Symbol: `DTMAPI.Abstractions.ActionCompletionOptions`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:977`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: ToolCollider.HandleTools, DungeonResourceRenderer, Equipment.DecoratedInteract, and VegetationRenderer exception path.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260531-0004-oneaction-resource-hit-smoke.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260531-032318
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#oneaction-001
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1502"></a>
### DTMAPI.Abstractions.ActionCompletionOptions.Enabled

- Symbol: `DTMAPI.Abstractions.ActionCompletionOptions.Enabled`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:979`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ToolCollider.HandleTools, DungeonResourceRenderer, Equipment.DecoratedInteract, and VegetationRenderer exception path.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260531-0004-oneaction-resource-hit-smoke.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260531-032318
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#oneaction-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1503"></a>
### DTMAPI.Abstractions.ActionCompletionOptions.CompleteTrees

- Symbol: `DTMAPI.Abstractions.ActionCompletionOptions.CompleteTrees`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:980`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ToolCollider.HandleTools, DungeonResourceRenderer, Equipment.DecoratedInteract, and VegetationRenderer exception path.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260531-0004-oneaction-resource-hit-smoke.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260531-032318
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#oneaction-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1504"></a>
### DTMAPI.Abstractions.ActionCompletionOptions.CompleteOres

- Symbol: `DTMAPI.Abstractions.ActionCompletionOptions.CompleteOres`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:981`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ToolCollider.HandleTools, DungeonResourceRenderer, Equipment.DecoratedInteract, and VegetationRenderer exception path.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260531-0004-oneaction-resource-hit-smoke.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260531-032318
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#oneaction-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1505"></a>
### DTMAPI.Abstractions.ActionCompletionOptions.CompleteGarbage

- Symbol: `DTMAPI.Abstractions.ActionCompletionOptions.CompleteGarbage`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:982`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ToolCollider.HandleTools, DungeonResourceRenderer, Equipment.DecoratedInteract, and VegetationRenderer exception path.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260531-0004-oneaction-resource-hit-smoke.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260531-032318
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#oneaction-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1506"></a>
### DTMAPI.Abstractions.ActionCompletionOptions.CompleteWeeds

- Symbol: `DTMAPI.Abstractions.ActionCompletionOptions.CompleteWeeds`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:983`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ToolCollider.HandleTools, DungeonResourceRenderer, Equipment.DecoratedInteract, and VegetationRenderer exception path.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260531-0004-oneaction-resource-hit-smoke.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260531-032318
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#oneaction-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1507"></a>
### DTMAPI.Abstractions.ActionCompletionOptions.CompleteMachineFuel

- Symbol: `DTMAPI.Abstractions.ActionCompletionOptions.CompleteMachineFuel`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:984`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ToolCollider.HandleTools, DungeonResourceRenderer, Equipment.DecoratedInteract, and VegetationRenderer exception path.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260531-0004-oneaction-resource-hit-smoke.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260531-032318
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#oneaction-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1508"></a>
### DTMAPI.Abstractions.ActionCompletionOptions.CompleteFeeder

- Symbol: `DTMAPI.Abstractions.ActionCompletionOptions.CompleteFeeder`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:985`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ToolCollider.HandleTools, DungeonResourceRenderer, Equipment.DecoratedInteract, and VegetationRenderer exception path.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260531-0004-oneaction-resource-hit-smoke.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260531-032318
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#oneaction-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1509"></a>
### DTMAPI.Abstractions.ActionCompletionOptions.VerboseLogging

- Symbol: `DTMAPI.Abstractions.ActionCompletionOptions.VerboseLogging`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:986`
- Implementation: No runtime implementation; data contract member only.
- Native owner: ToolCollider.HandleTools, DungeonResourceRenderer, Equipment.DecoratedInteract, and VegetationRenderer exception path.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260531-0004-oneaction-resource-hit-smoke.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260531-032318
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#oneaction-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1510"></a>
### DTMAPI.Abstractions.FishingAutomationOptions

- Symbol: `DTMAPI.Abstractions.FishingAutomationOptions`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:989`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: BodyController.UseFishRod, AgentStateFishingReady/Cast/Wait/Pull, and FishingGameScrollBar.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150631
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#autofish-001
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1511"></a>
### DTMAPI.Abstractions.FishingAutomationOptions.AutoRecast

- Symbol: `DTMAPI.Abstractions.FishingAutomationOptions.AutoRecast`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:991`
- Implementation: No runtime implementation; data contract member only.
- Native owner: BodyController.UseFishRod, AgentStateFishingReady/Cast/Wait/Pull, and FishingGameScrollBar.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150631
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#autofish-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1512"></a>
### DTMAPI.Abstractions.FishingAutomationOptions.StopOnManualMove

- Symbol: `DTMAPI.Abstractions.FishingAutomationOptions.StopOnManualMove`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:992`
- Implementation: No runtime implementation; data contract member only.
- Native owner: BodyController.UseFishRod, AgentStateFishingReady/Cast/Wait/Pull, and FishingGameScrollBar.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150631
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#autofish-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1513"></a>
### DTMAPI.Abstractions.FishingAutomationOptions.RequireSelectedFishingRod

- Symbol: `DTMAPI.Abstractions.FishingAutomationOptions.RequireSelectedFishingRod`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:993`
- Implementation: No runtime implementation; data contract member only.
- Native owner: BodyController.UseFishRod, AgentStateFishingReady/Cast/Wait/Pull, and FishingGameScrollBar.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150631
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#autofish-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1514"></a>
### DTMAPI.Abstractions.FishingAutomationOptions.CastReleaseProgress

- Symbol: `DTMAPI.Abstractions.FishingAutomationOptions.CastReleaseProgress`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:994`
- Implementation: No runtime implementation; data contract member only.
- Native owner: BodyController.UseFishRod, AgentStateFishingReady/Cast/Wait/Pull, and FishingGameScrollBar.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150631
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#autofish-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1515"></a>
### DTMAPI.Abstractions.FishingAutomationOptions.RecastDelaySeconds

- Symbol: `DTMAPI.Abstractions.FishingAutomationOptions.RecastDelaySeconds`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:995`
- Implementation: No runtime implementation; data contract member only.
- Native owner: BodyController.UseFishRod, AgentStateFishingReady/Cast/Wait/Pull, and FishingGameScrollBar.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150631
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#autofish-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1516"></a>
### DTMAPI.Abstractions.FishingAutomationOptions.SkipMiniGame

- Symbol: `DTMAPI.Abstractions.FishingAutomationOptions.SkipMiniGame`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:996`
- Implementation: No runtime implementation; data contract member only.
- Native owner: BodyController.UseFishRod, AgentStateFishingReady/Cast/Wait/Pull, and FishingGameScrollBar.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150631
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#autofish-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1517"></a>
### DTMAPI.Abstractions.FishingAutomationOptions.AutoCompleteMiniGame

- Symbol: `DTMAPI.Abstractions.FishingAutomationOptions.AutoCompleteMiniGame`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:997`
- Implementation: No runtime implementation; data contract member only.
- Native owner: BodyController.UseFishRod, AgentStateFishingReady/Cast/Wait/Pull, and FishingGameScrollBar.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150631
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#autofish-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1518"></a>
### DTMAPI.Abstractions.FishingAutomationOptions.InstantBite

- Symbol: `DTMAPI.Abstractions.FishingAutomationOptions.InstantBite`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:998`
- Implementation: No runtime implementation; data contract member only.
- Native owner: BodyController.UseFishRod, AgentStateFishingReady/Cast/Wait/Pull, and FishingGameScrollBar.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150631
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#autofish-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1519"></a>
### DTMAPI.Abstractions.FishingAutomationOptions.FastAnimations

- Symbol: `DTMAPI.Abstractions.FishingAutomationOptions.FastAnimations`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:999`
- Implementation: No runtime implementation; data contract member only.
- Native owner: BodyController.UseFishRod, AgentStateFishingReady/Cast/Wait/Pull, and FishingGameScrollBar.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150631
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#autofish-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1520"></a>
### DTMAPI.Abstractions.FishingAutomationOptions.FastAnimationMultiplier

- Symbol: `DTMAPI.Abstractions.FishingAutomationOptions.FastAnimationMultiplier`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1000`
- Implementation: No runtime implementation; data contract member only.
- Native owner: BodyController.UseFishRod, AgentStateFishingReady/Cast/Wait/Pull, and FishingGameScrollBar.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150631
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#autofish-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1521"></a>
### DTMAPI.Abstractions.FishingAutomationOptions.VerboseLogging

- Symbol: `DTMAPI.Abstractions.FishingAutomationOptions.VerboseLogging`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1001`
- Implementation: No runtime implementation; data contract member only.
- Native owner: BodyController.UseFishRod, AgentStateFishingReady/Cast/Wait/Pull, and FishingGameScrollBar.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150631
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#autofish-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1522"></a>
### DTMAPI.Abstractions.ActionSpeedOptions

- Symbol: `DTMAPI.Abstractions.ActionSpeedOptions`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1004`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: AgentStateTool/Interact/Eat, BodyController/tool animators, and item use/fill paths.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150543
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#actionspeed-002
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1523"></a>
### DTMAPI.Abstractions.ActionSpeedOptions.Enabled

- Symbol: `DTMAPI.Abstractions.ActionSpeedOptions.Enabled`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1006`
- Implementation: No runtime implementation; data contract member only.
- Native owner: AgentStateTool/Interact/Eat, BodyController/tool animators, and item use/fill paths.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150543
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#actionspeed-002
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1524"></a>
### DTMAPI.Abstractions.ActionSpeedOptions.ToolSpeedEnabled

- Symbol: `DTMAPI.Abstractions.ActionSpeedOptions.ToolSpeedEnabled`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1007`
- Implementation: No runtime implementation; data contract member only.
- Native owner: AgentStateTool/Interact/Eat, BodyController/tool animators, and item use/fill paths.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150543
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#actionspeed-002
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1525"></a>
### DTMAPI.Abstractions.ActionSpeedOptions.ToolMultiplier

- Symbol: `DTMAPI.Abstractions.ActionSpeedOptions.ToolMultiplier`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1008`
- Implementation: No runtime implementation; data contract member only.
- Native owner: AgentStateTool/Interact/Eat, BodyController/tool animators, and item use/fill paths.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150543
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#actionspeed-002
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1526"></a>
### DTMAPI.Abstractions.ActionSpeedOptions.BottleFillSpeedEnabled

- Symbol: `DTMAPI.Abstractions.ActionSpeedOptions.BottleFillSpeedEnabled`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1009`
- Implementation: No runtime implementation; data contract member only.
- Native owner: AgentStateTool/Interact/Eat, BodyController/tool animators, and item use/fill paths.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150543
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#actionspeed-002
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1527"></a>
### DTMAPI.Abstractions.ActionSpeedOptions.BottleFillMultiplier

- Symbol: `DTMAPI.Abstractions.ActionSpeedOptions.BottleFillMultiplier`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1010`
- Implementation: No runtime implementation; data contract member only.
- Native owner: AgentStateTool/Interact/Eat, BodyController/tool animators, and item use/fill paths.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150543
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#actionspeed-002
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1528"></a>
### DTMAPI.Abstractions.ActionSpeedOptions.EatDrinkSpeedEnabled

- Symbol: `DTMAPI.Abstractions.ActionSpeedOptions.EatDrinkSpeedEnabled`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1011`
- Implementation: No runtime implementation; data contract member only.
- Native owner: AgentStateTool/Interact/Eat, BodyController/tool animators, and item use/fill paths.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150543
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#actionspeed-002
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1529"></a>
### DTMAPI.Abstractions.ActionSpeedOptions.EatDrinkMultiplier

- Symbol: `DTMAPI.Abstractions.ActionSpeedOptions.EatDrinkMultiplier`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1012`
- Implementation: No runtime implementation; data contract member only.
- Native owner: AgentStateTool/Interact/Eat, BodyController/tool animators, and item use/fill paths.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150543
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#actionspeed-002
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1530"></a>
### DTMAPI.Abstractions.ActionSpeedOptions.MachineAddSpeedEnabled

- Symbol: `DTMAPI.Abstractions.ActionSpeedOptions.MachineAddSpeedEnabled`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1013`
- Implementation: No runtime implementation; data contract member only.
- Native owner: AgentStateTool/Interact/Eat, BodyController/tool animators, and item use/fill paths.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150543
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#actionspeed-002
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1531"></a>
### DTMAPI.Abstractions.ActionSpeedOptions.MachineAddMultiplier

- Symbol: `DTMAPI.Abstractions.ActionSpeedOptions.MachineAddMultiplier`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1014`
- Implementation: No runtime implementation; data contract member only.
- Native owner: AgentStateTool/Interact/Eat, BodyController/tool animators, and item use/fill paths.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150543
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#actionspeed-002
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1532"></a>
### DTMAPI.Abstractions.ActionSpeedOptions.HarvestSpeedEnabled

- Symbol: `DTMAPI.Abstractions.ActionSpeedOptions.HarvestSpeedEnabled`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1015`
- Implementation: No runtime implementation; data contract member only.
- Native owner: AgentStateTool/Interact/Eat, BodyController/tool animators, and item use/fill paths.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150543
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#actionspeed-002
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1533"></a>
### DTMAPI.Abstractions.ActionSpeedOptions.HarvestMultiplier

- Symbol: `DTMAPI.Abstractions.ActionSpeedOptions.HarvestMultiplier`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1016`
- Implementation: No runtime implementation; data contract member only.
- Native owner: AgentStateTool/Interact/Eat, BodyController/tool animators, and item use/fill paths.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150543
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#actionspeed-002
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1534"></a>
### DTMAPI.Abstractions.ActionSpeedOptions.PlantSpeedEnabled

- Symbol: `DTMAPI.Abstractions.ActionSpeedOptions.PlantSpeedEnabled`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1017`
- Implementation: No runtime implementation; data contract member only.
- Native owner: AgentStateTool/Interact/Eat, BodyController/tool animators, and item use/fill paths.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150543
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#actionspeed-002
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1535"></a>
### DTMAPI.Abstractions.ActionSpeedOptions.PlantMultiplier

- Symbol: `DTMAPI.Abstractions.ActionSpeedOptions.PlantMultiplier`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1018`
- Implementation: No runtime implementation; data contract member only.
- Native owner: AgentStateTool/Interact/Eat, BodyController/tool animators, and item use/fill paths.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150543
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#actionspeed-002
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1536"></a>
### DTMAPI.Abstractions.ActionSpeedOptions.AutoFillBottle

- Symbol: `DTMAPI.Abstractions.ActionSpeedOptions.AutoFillBottle`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1019`
- Implementation: No runtime implementation; data contract member only.
- Native owner: AgentStateTool/Interact/Eat, BodyController/tool animators, and item use/fill paths.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150543
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#actionspeed-002
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1537"></a>
### DTMAPI.Abstractions.ActionSpeedOptions.AutoFillStrong

- Symbol: `DTMAPI.Abstractions.ActionSpeedOptions.AutoFillStrong`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1020`
- Implementation: No runtime implementation; data contract member only.
- Native owner: AgentStateTool/Interact/Eat, BodyController/tool animators, and item use/fill paths.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150543
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#actionspeed-002
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1538"></a>
### DTMAPI.Abstractions.ActionSpeedOptions.AutoFillCooldownSeconds

- Symbol: `DTMAPI.Abstractions.ActionSpeedOptions.AutoFillCooldownSeconds`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1021`
- Implementation: No runtime implementation; data contract member only.
- Native owner: AgentStateTool/Interact/Eat, BodyController/tool animators, and item use/fill paths.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150543
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#actionspeed-002
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1539"></a>
### DTMAPI.Abstractions.ActionSpeedOptions.AutoFillStrongCooldownSeconds

- Symbol: `DTMAPI.Abstractions.ActionSpeedOptions.AutoFillStrongCooldownSeconds`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1022`
- Implementation: No runtime implementation; data contract member only.
- Native owner: AgentStateTool/Interact/Eat, BodyController/tool animators, and item use/fill paths.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150543
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#actionspeed-002
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1540"></a>
### DTMAPI.Abstractions.ActionSpeedOptions.ContinuousDrinkWithRightClick

- Symbol: `DTMAPI.Abstractions.ActionSpeedOptions.ContinuousDrinkWithRightClick`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1023`
- Implementation: No runtime implementation; data contract member only.
- Native owner: AgentStateTool/Interact/Eat, BodyController/tool animators, and item use/fill paths.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150543
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#actionspeed-002
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1541"></a>
### DTMAPI.Abstractions.ActionSpeedOptions.VerboseLogging

- Symbol: `DTMAPI.Abstractions.ActionSpeedOptions.VerboseLogging`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1024`
- Implementation: No runtime implementation; data contract member only.
- Native owner: AgentStateTool/Interact/Eat, BodyController/tool animators, and item use/fill paths.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150543
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#actionspeed-002
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1542"></a>
### DTMAPI.Abstractions.FishingAutomationState

- Symbol: `DTMAPI.Abstractions.FishingAutomationState`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1027`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: BodyController.UseFishRod, AgentStateFishingReady/Cast/Wait/Pull, and FishingGameScrollBar.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150631
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#autofish-001
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1543"></a>
### DTMAPI.Abstractions.FishingAutomationState.Enabled

- Symbol: `DTMAPI.Abstractions.FishingAutomationState.Enabled`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1029`
- Implementation: No runtime implementation; data contract member only.
- Native owner: BodyController.UseFishRod, AgentStateFishingReady/Cast/Wait/Pull, and FishingGameScrollBar.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150631
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#autofish-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1544"></a>
### DTMAPI.Abstractions.FishingAutomationState.Phase

- Symbol: `DTMAPI.Abstractions.FishingAutomationState.Phase`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1030`
- Implementation: No runtime implementation; data contract member only.
- Native owner: BodyController.UseFishRod, AgentStateFishingReady/Cast/Wait/Pull, and FishingGameScrollBar.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150631
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#autofish-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1545"></a>
### DTMAPI.Abstractions.FishingAutomationState.LastReason

- Symbol: `DTMAPI.Abstractions.FishingAutomationState.LastReason`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1031`
- Implementation: No runtime implementation; data contract member only.
- Native owner: BodyController.UseFishRod, AgentStateFishingReady/Cast/Wait/Pull, and FishingGameScrollBar.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150631
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#autofish-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1546"></a>
### DTMAPI.Abstractions.FishRoeTooltipOptions

- Symbol: `DTMAPI.Abstractions.FishRoeTooltipOptions`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1034`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: Item title/description/detail UI getters only; no native item data owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260601-0002-y-console-021-migrated-mod-polish.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-150808
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#fishroe-001
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1547"></a>
### DTMAPI.Abstractions.FishRoeTooltipOptions.Enabled

- Symbol: `DTMAPI.Abstractions.FishRoeTooltipOptions.Enabled`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1036`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Item title/description/detail UI getters only; no native item data owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260601-0002-y-console-021-migrated-mod-polish.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-150808
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#fishroe-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1548"></a>
### DTMAPI.Abstractions.FishRoeTooltipOptions.LabelFishRoeTitle

- Symbol: `DTMAPI.Abstractions.FishRoeTooltipOptions.LabelFishRoeTitle`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1037`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Item title/description/detail UI getters only; no native item data owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260601-0002-y-console-021-migrated-mod-polish.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-150808
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#fishroe-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1549"></a>
### DTMAPI.Abstractions.FishRoeTooltipOptions.LabelFishRoeDetails

- Symbol: `DTMAPI.Abstractions.FishRoeTooltipOptions.LabelFishRoeDetails`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1038`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Item title/description/detail UI getters only; no native item data owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260601-0002-y-console-021-migrated-mod-polish.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-150808
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#fishroe-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1550"></a>
### DTMAPI.Abstractions.FishRoeTooltipOptions.CacheSeconds

- Symbol: `DTMAPI.Abstractions.FishRoeTooltipOptions.CacheSeconds`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1039`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Item title/description/detail UI getters only; no native item data owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260601-0002-y-console-021-migrated-mod-polish.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-150808
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#fishroe-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1551"></a>
### DTMAPI.Abstractions.FishRoeTooltipOptions.VerboseLogging

- Symbol: `DTMAPI.Abstractions.FishRoeTooltipOptions.VerboseLogging`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1040`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Item title/description/detail UI getters only; no native item data owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260601-0002-y-console-021-migrated-mod-polish.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-150808
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#fishroe-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1552"></a>
### DTMAPI.Abstractions.FishRoeDisplayInfo

- Symbol: `DTMAPI.Abstractions.FishRoeDisplayInfo`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1043`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: Item title/description/detail UI getters only; no native item data owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260601-0002-y-console-021-migrated-mod-polish.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-150808
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#fishroe-001
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1553"></a>
### DTMAPI.Abstractions.FishRoeDisplayInfo.FishId

- Symbol: `DTMAPI.Abstractions.FishRoeDisplayInfo.FishId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1045`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Item title/description/detail UI getters only; no native item data owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260601-0002-y-console-021-migrated-mod-polish.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-150808
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#fishroe-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1554"></a>
### DTMAPI.Abstractions.FishRoeDisplayInfo.FishTitle

- Symbol: `DTMAPI.Abstractions.FishRoeDisplayInfo.FishTitle`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1046`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Item title/description/detail UI getters only; no native item data owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260601-0002-y-console-021-migrated-mod-polish.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-150808
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#fishroe-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1555"></a>
### DTMAPI.Abstractions.FishRoeDisplayInfo.RoeTitle

- Symbol: `DTMAPI.Abstractions.FishRoeDisplayInfo.RoeTitle`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1047`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Item title/description/detail UI getters only; no native item data owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260601-0002-y-console-021-migrated-mod-polish.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-150808
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#fishroe-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1556"></a>
### DTMAPI.Abstractions.FishRoeDisplayInfo.IncubateText

- Symbol: `DTMAPI.Abstractions.FishRoeDisplayInfo.IncubateText`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1048`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Item title/description/detail UI getters only; no native item data owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260601-0002-y-console-021-migrated-mod-polish.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-150808
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#fishroe-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1557"></a>
### DTMAPI.Abstractions.FishRoeDisplayInfo.GrowText

- Symbol: `DTMAPI.Abstractions.FishRoeDisplayInfo.GrowText`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1049`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Item title/description/detail UI getters only; no native item data owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260601-0002-y-console-021-migrated-mod-polish.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-150808
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#fishroe-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1558"></a>
### DTMAPI.Abstractions.FishRoeDisplayInfo.ParentSummary

- Symbol: `DTMAPI.Abstractions.FishRoeDisplayInfo.ParentSummary`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1050`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Item title/description/detail UI getters only; no native item data owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260601-0002-y-console-021-migrated-mod-polish.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-150808
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#fishroe-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1559"></a>
### DTMAPI.Abstractions.AnimalHusbandryProgressOptions

- Symbol: `DTMAPI.Abstractions.AnimalHusbandryProgressOptions`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1053`
- Implementation: No runtime implementation; DTO consumed by the owning API implementation.
- Native owner: AnimalViewer.Show / AnimalFullInfoData UI data; no animal production lifecycle owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150721
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#animal-001
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1560"></a>
### DTMAPI.Abstractions.AnimalHusbandryProgressOptions.Enabled

- Symbol: `DTMAPI.Abstractions.AnimalHusbandryProgressOptions.Enabled`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1055`
- Implementation: No runtime implementation; data contract member only.
- Native owner: AnimalViewer.Show / AnimalFullInfoData UI data; no animal production lifecycle owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150721
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#animal-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1561"></a>
### DTMAPI.Abstractions.AnimalHusbandryProgressOptions.ProgressColor

- Symbol: `DTMAPI.Abstractions.AnimalHusbandryProgressOptions.ProgressColor`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1056`
- Implementation: No runtime implementation; data contract member only.
- Native owner: AnimalViewer.Show / AnimalFullInfoData UI data; no animal production lifecycle owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150721
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#animal-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1562"></a>
### DTMAPI.Abstractions.AnimalHusbandryProgressOptions.CacheSeconds

- Symbol: `DTMAPI.Abstractions.AnimalHusbandryProgressOptions.CacheSeconds`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1057`
- Implementation: No runtime implementation; data contract member only.
- Native owner: AnimalViewer.Show / AnimalFullInfoData UI data; no animal production lifecycle owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150721
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#animal-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1563"></a>
### DTMAPI.Abstractions.AnimalHusbandryProgressOptions.VerboseLogging

- Symbol: `DTMAPI.Abstractions.AnimalHusbandryProgressOptions.VerboseLogging`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 仅 DTMAPI 自家 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:1058`
- Implementation: No runtime implementation; data contract member only.
- Native owner: AnimalViewer.Show / AnimalFullInfoData UI data; no animal production lifecycle owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150721
  - hook-map entry: docs/debug/regressions/smoke-matrix.md#animal-001
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.
