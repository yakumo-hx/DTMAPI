# Custom Entities 0.4.0 Symbol-Level Native Responsibility Audit

- Audit: `20260607-0006-native-responsibility-method-audit`
- Scope: public Abstractions symbols in this volume; docs-only review, no implementation goal.
- Locator rule: `Symbol` is the stable key; `Declaration` line numbers are secondary and may drift.

## Top Risks

- `RequestSpawn`, `SpawnProjectile`, `ExecuteAttack`, `RequestSummon`, `Equip`, and `SetMode` are blocked runtime operations despite stable contracts.
- `RuntimeStatus`, `Snapshot`, `Handle`, `ActiveRuntimeInstanceCount`, and `Succeeded` fields can overpromise native runtime support.
- Contract stability and runtime adapter status must be split on every animal/monster/attack/drone block.
- Ordinary mods may register/query definitions, but must not depend on native entity creation until family adapters are verified.

## Audit Blocks

<a id="sym-0073"></a>
### DTMAPI.Abstractions.CustomEntityFamily

- Symbol: `DTMAPI.Abstractions.CustomEntityFamily`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:6`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0074"></a>
### DTMAPI.Abstractions.CustomEntityFamily.Animal

- Symbol: `DTMAPI.Abstractions.CustomEntityFamily.Animal`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:8`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:71`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:71
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0075"></a>
### DTMAPI.Abstractions.CustomEntityFamily.Monster

- Symbol: `DTMAPI.Abstractions.CustomEntityFamily.Monster`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:9`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:149`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:149
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0076"></a>
### DTMAPI.Abstractions.CustomEntityFamily.Attack

- Symbol: `DTMAPI.Abstractions.CustomEntityFamily.Attack`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:10`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:248`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:248
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0077"></a>
### DTMAPI.Abstractions.CustomEntityFamily.Drone

- Symbol: `DTMAPI.Abstractions.CustomEntityFamily.Drone`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:11`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:321`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:321
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0078"></a>
### DTMAPI.Abstractions.CustomEntityValidationSeverity

- Symbol: `DTMAPI.Abstractions.CustomEntityValidationSeverity`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:14`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0079"></a>
### DTMAPI.Abstractions.CustomEntityValidationSeverity.Info

- Symbol: `DTMAPI.Abstractions.CustomEntityValidationSeverity.Info`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:16`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0080"></a>
### DTMAPI.Abstractions.CustomEntityValidationSeverity.Warning

- Symbol: `DTMAPI.Abstractions.CustomEntityValidationSeverity.Warning`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:17`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:240`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:240
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0081"></a>
### DTMAPI.Abstractions.CustomEntityValidationSeverity.Error

- Symbol: `DTMAPI.Abstractions.CustomEntityValidationSeverity.Error`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:18`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:533`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:533
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0082"></a>
### DTMAPI.Abstractions.CustomEntityRuntimeStatus

- Symbol: `DTMAPI.Abstractions.CustomEntityRuntimeStatus`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:21`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0083"></a>
### DTMAPI.Abstractions.CustomEntityRuntimeStatus.Unknown

- Symbol: `DTMAPI.Abstractions.CustomEntityRuntimeStatus.Unknown`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:23`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:603`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:603
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0084"></a>
### DTMAPI.Abstractions.CustomEntityRuntimeStatus.Registered

- Symbol: `DTMAPI.Abstractions.CustomEntityRuntimeStatus.Registered`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:24`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:907`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:907
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0085"></a>
### DTMAPI.Abstractions.CustomEntityRuntimeStatus.ConfiguredNoRuntimeInstance

- Symbol: `DTMAPI.Abstractions.CustomEntityRuntimeStatus.ConfiguredNoRuntimeInstance`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:25`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:509`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:509
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0086"></a>
### DTMAPI.Abstractions.CustomEntityRuntimeStatus.RuntimeCreationBlocked

- Symbol: `DTMAPI.Abstractions.CustomEntityRuntimeStatus.RuntimeCreationBlocked`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:26`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:383`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:383
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0087"></a>
### DTMAPI.Abstractions.CustomEntityRuntimeStatus.Active

- Symbol: `DTMAPI.Abstractions.CustomEntityRuntimeStatus.Active`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:27`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:603`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:603
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0088"></a>
### DTMAPI.Abstractions.CustomEntityRuntimeStatus.Removing

- Symbol: `DTMAPI.Abstractions.CustomEntityRuntimeStatus.Removing`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:28`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0089"></a>
### DTMAPI.Abstractions.CustomEntityRuntimeStatus.Removed

- Symbol: `DTMAPI.Abstractions.CustomEntityRuntimeStatus.Removed`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:29`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:506`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:506
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0090"></a>
### DTMAPI.Abstractions.CustomEntityRuntimeStatus.Failed

- Symbol: `DTMAPI.Abstractions.CustomEntityRuntimeStatus.Failed`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:30`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:496`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:496
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0091"></a>
### DTMAPI.Abstractions.CustomEntityLifecycleKind

- Symbol: `DTMAPI.Abstractions.CustomEntityLifecycleKind`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:33`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0092"></a>
### DTMAPI.Abstractions.CustomEntityLifecycleKind.Registered

- Symbol: `DTMAPI.Abstractions.CustomEntityLifecycleKind.Registered`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:35`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:76`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:76
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0093"></a>
### DTMAPI.Abstractions.CustomEntityLifecycleKind.Unregistered

- Symbol: `DTMAPI.Abstractions.CustomEntityLifecycleKind.Unregistered`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:36`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:85`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:85
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0094"></a>
### DTMAPI.Abstractions.CustomEntityLifecycleKind.SaveLoaded

- Symbol: `DTMAPI.Abstractions.CustomEntityLifecycleKind.SaveLoaded`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:37`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0095"></a>
### DTMAPI.Abstractions.CustomEntityLifecycleKind.SaveSaving

- Symbol: `DTMAPI.Abstractions.CustomEntityLifecycleKind.SaveSaving`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:38`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0096"></a>
### DTMAPI.Abstractions.CustomEntityLifecycleKind.SaveSaved

- Symbol: `DTMAPI.Abstractions.CustomEntityLifecycleKind.SaveSaved`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:39`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0097"></a>
### DTMAPI.Abstractions.CustomEntityLifecycleKind.ReturnedToTitle

- Symbol: `DTMAPI.Abstractions.CustomEntityLifecycleKind.ReturnedToTitle`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:40`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0098"></a>
### DTMAPI.Abstractions.CustomEntityLifecycleKind.SpawnRequested

- Symbol: `DTMAPI.Abstractions.CustomEntityLifecycleKind.SpawnRequested`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:41`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:112`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:112
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0099"></a>
### DTMAPI.Abstractions.CustomEntityLifecycleKind.Spawned

- Symbol: `DTMAPI.Abstractions.CustomEntityLifecycleKind.Spawned`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:42`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0100"></a>
### DTMAPI.Abstractions.CustomEntityLifecycleKind.RemoveRequested

- Symbol: `DTMAPI.Abstractions.CustomEntityLifecycleKind.RemoveRequested`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:43`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0101"></a>
### DTMAPI.Abstractions.CustomEntityLifecycleKind.Removed

- Symbol: `DTMAPI.Abstractions.CustomEntityLifecycleKind.Removed`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:44`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:505`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:505
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0102"></a>
### DTMAPI.Abstractions.CustomEntityLifecycleKind.Tick

- Symbol: `DTMAPI.Abstractions.CustomEntityLifecycleKind.Tick`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:45`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0103"></a>
### DTMAPI.Abstractions.CustomEntityLifecycleKind.Damaged

- Symbol: `DTMAPI.Abstractions.CustomEntityLifecycleKind.Damaged`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:46`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0104"></a>
### DTMAPI.Abstractions.CustomEntityLifecycleKind.Died

- Symbol: `DTMAPI.Abstractions.CustomEntityLifecycleKind.Died`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:47`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0105"></a>
### DTMAPI.Abstractions.CustomEntityLifecycleKind.Expired

- Symbol: `DTMAPI.Abstractions.CustomEntityLifecycleKind.Expired`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:48`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0106"></a>
### DTMAPI.Abstractions.CustomEntityLifecycleKind.Failed

- Symbol: `DTMAPI.Abstractions.CustomEntityLifecycleKind.Failed`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:49`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0107"></a>
### DTMAPI.Abstractions.CustomEntityTickPolicyKind

- Symbol: `DTMAPI.Abstractions.CustomEntityTickPolicyKind`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:52`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0108"></a>
### DTMAPI.Abstractions.CustomEntityTickPolicyKind.Disabled

- Symbol: `DTMAPI.Abstractions.CustomEntityTickPolicyKind.Disabled`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:54`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0109"></a>
### DTMAPI.Abstractions.CustomEntityTickPolicyKind.OnGameUpdate

- Symbol: `DTMAPI.Abstractions.CustomEntityTickPolicyKind.OnGameUpdate`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:55`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0110"></a>
### DTMAPI.Abstractions.CustomEntityTickPolicyKind.FixedInterval

- Symbol: `DTMAPI.Abstractions.CustomEntityTickPolicyKind.FixedInterval`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:56`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0111"></a>
### DTMAPI.Abstractions.CustomEntityTickPolicyKind.OneSecond

- Symbol: `DTMAPI.Abstractions.CustomEntityTickPolicyKind.OneSecond`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:57`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:3587`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:3587
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0112"></a>
### DTMAPI.Abstractions.CustomEntityTickPolicyKind.SaveBoundaryOnly

- Symbol: `DTMAPI.Abstractions.CustomEntityTickPolicyKind.SaveBoundaryOnly`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:58`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0113"></a>
### DTMAPI.Abstractions.CustomEntityPersistenceKind

- Symbol: `DTMAPI.Abstractions.CustomEntityPersistenceKind`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:61`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0114"></a>
### DTMAPI.Abstractions.CustomEntityPersistenceKind.RuntimeOnly

- Symbol: `DTMAPI.Abstractions.CustomEntityPersistenceKind.RuntimeOnly`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:63`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:3464`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:3464
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0115"></a>
### DTMAPI.Abstractions.CustomEntityPersistenceKind.SaveScoped

- Symbol: `DTMAPI.Abstractions.CustomEntityPersistenceKind.SaveScoped`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:64`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:3575`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:3575
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0116"></a>
### DTMAPI.Abstractions.CustomEntityPersistenceKind.SaveAndRespawn

- Symbol: `DTMAPI.Abstractions.CustomEntityPersistenceKind.SaveAndRespawn`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:65`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0117"></a>
### DTMAPI.Abstractions.CustomEntityPersistenceKind.DefinitionOnly

- Symbol: `DTMAPI.Abstractions.CustomEntityPersistenceKind.DefinitionOnly`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:66`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0118"></a>
### DTMAPI.Abstractions.CustomEntityMovementKind

- Symbol: `DTMAPI.Abstractions.CustomEntityMovementKind`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:69`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0119"></a>
### DTMAPI.Abstractions.CustomEntityMovementKind.None

- Symbol: `DTMAPI.Abstractions.CustomEntityMovementKind.None`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:71`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0120"></a>
### DTMAPI.Abstractions.CustomEntityMovementKind.Stationary

- Symbol: `DTMAPI.Abstractions.CustomEntityMovementKind.Stationary`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:72`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0121"></a>
### DTMAPI.Abstractions.CustomEntityMovementKind.Wander

- Symbol: `DTMAPI.Abstractions.CustomEntityMovementKind.Wander`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:73`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:3480`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:3480
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0122"></a>
### DTMAPI.Abstractions.CustomEntityMovementKind.FollowTarget

- Symbol: `DTMAPI.Abstractions.CustomEntityMovementKind.FollowTarget`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:74`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:3462`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:3462
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0123"></a>
### DTMAPI.Abstractions.CustomEntityMovementKind.Patrol

- Symbol: `DTMAPI.Abstractions.CustomEntityMovementKind.Patrol`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:75`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0124"></a>
### DTMAPI.Abstractions.CustomEntityMovementKind.Flee

- Symbol: `DTMAPI.Abstractions.CustomEntityMovementKind.Flee`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:76`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0125"></a>
### DTMAPI.Abstractions.CustomEntityMovementKind.ProviderControlled

- Symbol: `DTMAPI.Abstractions.CustomEntityMovementKind.ProviderControlled`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:77`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0126"></a>
### DTMAPI.Abstractions.CustomEntityRelationKind

- Symbol: `DTMAPI.Abstractions.CustomEntityRelationKind`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:80`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0127"></a>
### DTMAPI.Abstractions.CustomEntityRelationKind.Neutral

- Symbol: `DTMAPI.Abstractions.CustomEntityRelationKind.Neutral`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:82`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0128"></a>
### DTMAPI.Abstractions.CustomEntityRelationKind.PlayerAlly

- Symbol: `DTMAPI.Abstractions.CustomEntityRelationKind.PlayerAlly`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:83`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0129"></a>
### DTMAPI.Abstractions.CustomEntityRelationKind.PlayerHostile

- Symbol: `DTMAPI.Abstractions.CustomEntityRelationKind.PlayerHostile`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:84`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0130"></a>
### DTMAPI.Abstractions.CustomEntityRelationKind.OwnerAlly

- Symbol: `DTMAPI.Abstractions.CustomEntityRelationKind.OwnerAlly`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:85`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:3458`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:3458
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0131"></a>
### DTMAPI.Abstractions.CustomEntityRelationKind.ProviderControlled

- Symbol: `DTMAPI.Abstractions.CustomEntityRelationKind.ProviderControlled`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:86`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0132"></a>
### DTMAPI.Abstractions.CustomHitboxShapeKind

- Symbol: `DTMAPI.Abstractions.CustomHitboxShapeKind`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:89`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0133"></a>
### DTMAPI.Abstractions.CustomHitboxShapeKind.Point

- Symbol: `DTMAPI.Abstractions.CustomHitboxShapeKind.Point`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:91`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0134"></a>
### DTMAPI.Abstractions.CustomHitboxShapeKind.Circle

- Symbol: `DTMAPI.Abstractions.CustomHitboxShapeKind.Circle`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:92`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:3461`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:3461
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0135"></a>
### DTMAPI.Abstractions.CustomHitboxShapeKind.Rectangle

- Symbol: `DTMAPI.Abstractions.CustomHitboxShapeKind.Rectangle`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:93`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0136"></a>
### DTMAPI.Abstractions.CustomHitboxShapeKind.Capsule

- Symbol: `DTMAPI.Abstractions.CustomHitboxShapeKind.Capsule`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:94`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0137"></a>
### DTMAPI.Abstractions.CustomHitboxShapeKind.ProviderControlled

- Symbol: `DTMAPI.Abstractions.CustomHitboxShapeKind.ProviderControlled`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:95`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0138"></a>
### DTMAPI.Abstractions.CustomAttackPatternKind

- Symbol: `DTMAPI.Abstractions.CustomAttackPatternKind`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:98`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0139"></a>
### DTMAPI.Abstractions.CustomAttackPatternKind.Instant

- Symbol: `DTMAPI.Abstractions.CustomAttackPatternKind.Instant`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:100`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0140"></a>
### DTMAPI.Abstractions.CustomAttackPatternKind.Projectile

- Symbol: `DTMAPI.Abstractions.CustomAttackPatternKind.Projectile`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:101`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:3463`
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:3463
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0141"></a>
### DTMAPI.Abstractions.CustomAttackPatternKind.Beam

- Symbol: `DTMAPI.Abstractions.CustomAttackPatternKind.Beam`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:102`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0142"></a>
### DTMAPI.Abstractions.CustomAttackPatternKind.Burst

- Symbol: `DTMAPI.Abstractions.CustomAttackPatternKind.Burst`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:103`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0143"></a>
### DTMAPI.Abstractions.CustomAttackPatternKind.Ring

- Symbol: `DTMAPI.Abstractions.CustomAttackPatternKind.Ring`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:104`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0144"></a>
### DTMAPI.Abstractions.CustomAttackPatternKind.Cone

- Symbol: `DTMAPI.Abstractions.CustomAttackPatternKind.Cone`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:105`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0145"></a>
### DTMAPI.Abstractions.CustomAttackPatternKind.Barrage

- Symbol: `DTMAPI.Abstractions.CustomAttackPatternKind.Barrage`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:106`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0146"></a>
### DTMAPI.Abstractions.CustomAttackPatternKind.ProviderControlled

- Symbol: `DTMAPI.Abstractions.CustomAttackPatternKind.ProviderControlled`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:107`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0147"></a>
### DTMAPI.Abstractions.CustomDroneBehaviorMode

- Symbol: `DTMAPI.Abstractions.CustomDroneBehaviorMode`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:110`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0148"></a>
### DTMAPI.Abstractions.CustomDroneBehaviorMode.Idle

- Symbol: `DTMAPI.Abstractions.CustomDroneBehaviorMode.Idle`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:112`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0149"></a>
### DTMAPI.Abstractions.CustomDroneBehaviorMode.Follow

- Symbol: `DTMAPI.Abstractions.CustomDroneBehaviorMode.Follow`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:113`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:404`
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:404
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0150"></a>
### DTMAPI.Abstractions.CustomDroneBehaviorMode.Guard

- Symbol: `DTMAPI.Abstractions.CustomDroneBehaviorMode.Guard`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:114`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:3497`
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:3497
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0151"></a>
### DTMAPI.Abstractions.CustomDroneBehaviorMode.Patrol

- Symbol: `DTMAPI.Abstractions.CustomDroneBehaviorMode.Patrol`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:115`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0152"></a>
### DTMAPI.Abstractions.CustomDroneBehaviorMode.Return

- Symbol: `DTMAPI.Abstractions.CustomDroneBehaviorMode.Return`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:116`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0153"></a>
### DTMAPI.Abstractions.CustomDroneBehaviorMode.Attack

- Symbol: `DTMAPI.Abstractions.CustomDroneBehaviorMode.Attack`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:117`
- Implementation: `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:3497`
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:3497
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0154"></a>
### DTMAPI.Abstractions.CustomDroneBehaviorMode.Support

- Symbol: `DTMAPI.Abstractions.CustomDroneBehaviorMode.Support`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:118`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0155"></a>
### DTMAPI.Abstractions.CustomDroneBehaviorMode.ProviderControlled

- Symbol: `DTMAPI.Abstractions.CustomDroneBehaviorMode.ProviderControlled`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:119`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0156"></a>
### DTMAPI.Abstractions.CustomEntityLocalizedText

- Symbol: `DTMAPI.Abstractions.CustomEntityLocalizedText`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:122`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0157"></a>
### DTMAPI.Abstractions.CustomEntityLocalizedText.Default

- Symbol: `DTMAPI.Abstractions.CustomEntityLocalizedText.Default`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:124`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0158"></a>
### DTMAPI.Abstractions.CustomEntityLocalizedText.English

- Symbol: `DTMAPI.Abstractions.CustomEntityLocalizedText.English`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:125`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0159"></a>
### DTMAPI.Abstractions.CustomEntityLocalizedText.SimplifiedChinese

- Symbol: `DTMAPI.Abstractions.CustomEntityLocalizedText.SimplifiedChinese`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:126`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0160"></a>
### DTMAPI.Abstractions.CustomEntityLocalizedText.Translations

- Symbol: `DTMAPI.Abstractions.CustomEntityLocalizedText.Translations`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:127`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0161"></a>
### DTMAPI.Abstractions.CustomEntityAssetHandle

- Symbol: `DTMAPI.Abstractions.CustomEntityAssetHandle`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:130`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0162"></a>
### DTMAPI.Abstractions.CustomEntityAssetHandle.AssetId

- Symbol: `DTMAPI.Abstractions.CustomEntityAssetHandle.AssetId`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:132`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0163"></a>
### DTMAPI.Abstractions.CustomEntityAssetHandle.ContentType

- Symbol: `DTMAPI.Abstractions.CustomEntityAssetHandle.ContentType`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:133`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0164"></a>
### DTMAPI.Abstractions.CustomEntityAssetHandle.RelativePath

- Symbol: `DTMAPI.Abstractions.CustomEntityAssetHandle.RelativePath`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:134`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0165"></a>
### DTMAPI.Abstractions.CustomEntityAssetHandle.VariantId

- Symbol: `DTMAPI.Abstractions.CustomEntityAssetHandle.VariantId`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:135`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0166"></a>
### DTMAPI.Abstractions.CustomEntityAssetHandle.Tags

- Symbol: `DTMAPI.Abstractions.CustomEntityAssetHandle.Tags`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:136`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0167"></a>
### DTMAPI.Abstractions.CustomEntityHandle

- Symbol: `DTMAPI.Abstractions.CustomEntityHandle`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:139`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0168"></a>
### DTMAPI.Abstractions.CustomEntityHandle.Family

- Symbol: `DTMAPI.Abstractions.CustomEntityHandle.Family`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:141`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0169"></a>
### DTMAPI.Abstractions.CustomEntityHandle.OwnerUniqueId

- Symbol: `DTMAPI.Abstractions.CustomEntityHandle.OwnerUniqueId`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:142`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0170"></a>
### DTMAPI.Abstractions.CustomEntityHandle.DefinitionId

- Symbol: `DTMAPI.Abstractions.CustomEntityHandle.DefinitionId`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:143`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0171"></a>
### DTMAPI.Abstractions.CustomEntityHandle.RuntimeId

- Symbol: `DTMAPI.Abstractions.CustomEntityHandle.RuntimeId`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:144`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0172"></a>
### DTMAPI.Abstractions.CustomEntityHandle.SaveSlot

- Symbol: `DTMAPI.Abstractions.CustomEntityHandle.SaveSlot`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:145`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0173"></a>
### DTMAPI.Abstractions.CustomEntityHandle.IsEmpty

- Symbol: `DTMAPI.Abstractions.CustomEntityHandle.IsEmpty`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:147`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0174"></a>
### DTMAPI.Abstractions.CustomEntityHandle.ToString()

- Symbol: `DTMAPI.Abstractions.CustomEntityHandle.ToString()`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:149`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0175"></a>
### DTMAPI.Abstractions.CustomEntityVector2

- Symbol: `DTMAPI.Abstractions.CustomEntityVector2`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:155`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0176"></a>
### DTMAPI.Abstractions.CustomEntityVector2.X

- Symbol: `DTMAPI.Abstractions.CustomEntityVector2.X`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:157`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0177"></a>
### DTMAPI.Abstractions.CustomEntityVector2.Y

- Symbol: `DTMAPI.Abstractions.CustomEntityVector2.Y`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:158`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0178"></a>
### DTMAPI.Abstractions.CustomEntityGridPosition

- Symbol: `DTMAPI.Abstractions.CustomEntityGridPosition`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:161`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0179"></a>
### DTMAPI.Abstractions.CustomEntityGridPosition.RoomId

- Symbol: `DTMAPI.Abstractions.CustomEntityGridPosition.RoomId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:163`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0180"></a>
### DTMAPI.Abstractions.CustomEntityGridPosition.X

- Symbol: `DTMAPI.Abstractions.CustomEntityGridPosition.X`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:164`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0181"></a>
### DTMAPI.Abstractions.CustomEntityGridPosition.Y

- Symbol: `DTMAPI.Abstractions.CustomEntityGridPosition.Y`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:165`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0182"></a>
### DTMAPI.Abstractions.CustomEntityGridPosition.Layer

- Symbol: `DTMAPI.Abstractions.CustomEntityGridPosition.Layer`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:166`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0183"></a>
### DTMAPI.Abstractions.CustomEntityValidationMessage

- Symbol: `DTMAPI.Abstractions.CustomEntityValidationMessage`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:169`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0184"></a>
### DTMAPI.Abstractions.CustomEntityValidationMessage.CustomEntityValidationMessage()

- Symbol: `DTMAPI.Abstractions.CustomEntityValidationMessage.CustomEntityValidationMessage()`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:171`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0185"></a>
### DTMAPI.Abstractions.CustomEntityValidationMessage.CustomEntityValidationMessage(CustomEntityValidationSeverity severity, string code, string message)

- Symbol: `DTMAPI.Abstractions.CustomEntityValidationMessage.CustomEntityValidationMessage(CustomEntityValidationSeverity severity, string code, string message)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:175`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0186"></a>
### DTMAPI.Abstractions.CustomEntityValidationMessage.Severity

- Symbol: `DTMAPI.Abstractions.CustomEntityValidationMessage.Severity`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:182`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0187"></a>
### DTMAPI.Abstractions.CustomEntityValidationMessage.Code

- Symbol: `DTMAPI.Abstractions.CustomEntityValidationMessage.Code`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:183`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0188"></a>
### DTMAPI.Abstractions.CustomEntityValidationMessage.Message

- Symbol: `DTMAPI.Abstractions.CustomEntityValidationMessage.Message`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:184`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0189"></a>
### DTMAPI.Abstractions.CustomEntityValidationMessage.Field

- Symbol: `DTMAPI.Abstractions.CustomEntityValidationMessage.Field`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:185`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0190"></a>
### DTMAPI.Abstractions.CustomEntityCapabilityStatus

- Symbol: `DTMAPI.Abstractions.CustomEntityCapabilityStatus`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:188`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0191"></a>
### DTMAPI.Abstractions.CustomEntityCapabilityStatus.Family

- Symbol: `DTMAPI.Abstractions.CustomEntityCapabilityStatus.Family`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:190`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0192"></a>
### DTMAPI.Abstractions.CustomEntityCapabilityStatus.Status

- Symbol: `DTMAPI.Abstractions.CustomEntityCapabilityStatus.Status`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:191`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0193"></a>
### DTMAPI.Abstractions.CustomEntityCapabilityStatus.FailureReason

- Symbol: `DTMAPI.Abstractions.CustomEntityCapabilityStatus.FailureReason`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:192`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0194"></a>
### DTMAPI.Abstractions.CustomEntityCapabilityStatus.Details

- Symbol: `DTMAPI.Abstractions.CustomEntityCapabilityStatus.Details`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:193`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0195"></a>
### DTMAPI.Abstractions.CustomEntityCapabilityStatus.RegisteredDefinitionCount

- Symbol: `DTMAPI.Abstractions.CustomEntityCapabilityStatus.RegisteredDefinitionCount`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:194`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0196"></a>
### DTMAPI.Abstractions.CustomEntityCapabilityStatus.ActiveRuntimeInstanceCount

- Symbol: `DTMAPI.Abstractions.CustomEntityCapabilityStatus.ActiveRuntimeInstanceCount`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:195`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0197"></a>
### DTMAPI.Abstractions.CustomEntityCapabilityStatus.Messages

- Symbol: `DTMAPI.Abstractions.CustomEntityCapabilityStatus.Messages`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:196`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0198"></a>
### DTMAPI.Abstractions.CustomEntityFamilySnapshot

- Symbol: `DTMAPI.Abstractions.CustomEntityFamilySnapshot`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:199`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0199"></a>
### DTMAPI.Abstractions.CustomEntityFamilySnapshot.Family

- Symbol: `DTMAPI.Abstractions.CustomEntityFamilySnapshot.Family`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:201`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0200"></a>
### DTMAPI.Abstractions.CustomEntityFamilySnapshot.OwnerUniqueId

- Symbol: `DTMAPI.Abstractions.CustomEntityFamilySnapshot.OwnerUniqueId`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:202`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0201"></a>
### DTMAPI.Abstractions.CustomEntityFamilySnapshot.RegisteredDefinitionCount

- Symbol: `DTMAPI.Abstractions.CustomEntityFamilySnapshot.RegisteredDefinitionCount`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:203`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0202"></a>
### DTMAPI.Abstractions.CustomEntityFamilySnapshot.ActiveRuntimeInstanceCount

- Symbol: `DTMAPI.Abstractions.CustomEntityFamilySnapshot.ActiveRuntimeInstanceCount`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:204`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0203"></a>
### DTMAPI.Abstractions.CustomEntityFamilySnapshot.SaveStateRecordCount

- Symbol: `DTMAPI.Abstractions.CustomEntityFamilySnapshot.SaveStateRecordCount`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:205`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0204"></a>
### DTMAPI.Abstractions.CustomEntityFamilySnapshot.RuntimeStatus

- Symbol: `DTMAPI.Abstractions.CustomEntityFamilySnapshot.RuntimeStatus`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:206`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0205"></a>
### DTMAPI.Abstractions.CustomEntityFamilySnapshot.StatusDetails

- Symbol: `DTMAPI.Abstractions.CustomEntityFamilySnapshot.StatusDetails`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:207`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0206"></a>
### DTMAPI.Abstractions.CustomEntityFamilySnapshot.DefinitionIds

- Symbol: `DTMAPI.Abstractions.CustomEntityFamilySnapshot.DefinitionIds`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:208`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0207"></a>
### DTMAPI.Abstractions.CustomEntityFamilySnapshot.RuntimeHandles

- Symbol: `DTMAPI.Abstractions.CustomEntityFamilySnapshot.RuntimeHandles`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:209`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0208"></a>
### DTMAPI.Abstractions.CustomEntityLifecycleEventArgs

- Symbol: `DTMAPI.Abstractions.CustomEntityLifecycleEventArgs`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:212`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0209"></a>
### DTMAPI.Abstractions.CustomEntityLifecycleEventArgs.Family

- Symbol: `DTMAPI.Abstractions.CustomEntityLifecycleEventArgs.Family`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:214`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0210"></a>
### DTMAPI.Abstractions.CustomEntityLifecycleEventArgs.Kind

- Symbol: `DTMAPI.Abstractions.CustomEntityLifecycleEventArgs.Kind`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:215`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0211"></a>
### DTMAPI.Abstractions.CustomEntityLifecycleEventArgs.OwnerUniqueId

- Symbol: `DTMAPI.Abstractions.CustomEntityLifecycleEventArgs.OwnerUniqueId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:216`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0212"></a>
### DTMAPI.Abstractions.CustomEntityLifecycleEventArgs.DefinitionId

- Symbol: `DTMAPI.Abstractions.CustomEntityLifecycleEventArgs.DefinitionId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:217`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0213"></a>
### DTMAPI.Abstractions.CustomEntityLifecycleEventArgs.Handle

- Symbol: `DTMAPI.Abstractions.CustomEntityLifecycleEventArgs.Handle`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:218`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0214"></a>
### DTMAPI.Abstractions.CustomEntityLifecycleEventArgs.Reason

- Symbol: `DTMAPI.Abstractions.CustomEntityLifecycleEventArgs.Reason`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:219`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0215"></a>
### DTMAPI.Abstractions.CustomEntityLifecycleEventArgs.Time

- Symbol: `DTMAPI.Abstractions.CustomEntityLifecycleEventArgs.Time`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:220`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0216"></a>
### DTMAPI.Abstractions.CustomEntityRegistrationResult

- Symbol: `DTMAPI.Abstractions.CustomEntityRegistrationResult`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:223`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0217"></a>
### DTMAPI.Abstractions.CustomEntityRegistrationResult.Succeeded

- Symbol: `DTMAPI.Abstractions.CustomEntityRegistrationResult.Succeeded`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:225`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0218"></a>
### DTMAPI.Abstractions.CustomEntityRegistrationResult.Family

- Symbol: `DTMAPI.Abstractions.CustomEntityRegistrationResult.Family`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:226`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0219"></a>
### DTMAPI.Abstractions.CustomEntityRegistrationResult.OwnerUniqueId

- Symbol: `DTMAPI.Abstractions.CustomEntityRegistrationResult.OwnerUniqueId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:227`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0220"></a>
### DTMAPI.Abstractions.CustomEntityRegistrationResult.DefinitionId

- Symbol: `DTMAPI.Abstractions.CustomEntityRegistrationResult.DefinitionId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:228`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0221"></a>
### DTMAPI.Abstractions.CustomEntityRegistrationResult.FailureReason

- Symbol: `DTMAPI.Abstractions.CustomEntityRegistrationResult.FailureReason`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:229`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0222"></a>
### DTMAPI.Abstractions.CustomEntityRegistrationResult.RuntimeStatus

- Symbol: `DTMAPI.Abstractions.CustomEntityRegistrationResult.RuntimeStatus`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:230`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0223"></a>
### DTMAPI.Abstractions.CustomEntityRegistrationResult.Messages

- Symbol: `DTMAPI.Abstractions.CustomEntityRegistrationResult.Messages`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:231`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0224"></a>
### DTMAPI.Abstractions.CustomEntityUnregisterResult

- Symbol: `DTMAPI.Abstractions.CustomEntityUnregisterResult`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:234`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0225"></a>
### DTMAPI.Abstractions.CustomEntityUnregisterResult.Succeeded

- Symbol: `DTMAPI.Abstractions.CustomEntityUnregisterResult.Succeeded`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:236`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0226"></a>
### DTMAPI.Abstractions.CustomEntityUnregisterResult.Family

- Symbol: `DTMAPI.Abstractions.CustomEntityUnregisterResult.Family`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:237`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0227"></a>
### DTMAPI.Abstractions.CustomEntityUnregisterResult.OwnerUniqueId

- Symbol: `DTMAPI.Abstractions.CustomEntityUnregisterResult.OwnerUniqueId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:238`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0228"></a>
### DTMAPI.Abstractions.CustomEntityUnregisterResult.DefinitionId

- Symbol: `DTMAPI.Abstractions.CustomEntityUnregisterResult.DefinitionId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:239`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0229"></a>
### DTMAPI.Abstractions.CustomEntityUnregisterResult.RemovedRuntimeInstanceCount

- Symbol: `DTMAPI.Abstractions.CustomEntityUnregisterResult.RemovedRuntimeInstanceCount`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:240`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0230"></a>
### DTMAPI.Abstractions.CustomEntityUnregisterResult.FailureReason

- Symbol: `DTMAPI.Abstractions.CustomEntityUnregisterResult.FailureReason`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:241`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0231"></a>
### DTMAPI.Abstractions.CustomEntityUnregisterResult.Messages

- Symbol: `DTMAPI.Abstractions.CustomEntityUnregisterResult.Messages`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:242`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0232"></a>
### DTMAPI.Abstractions.CustomEntityRequestResult

- Symbol: `DTMAPI.Abstractions.CustomEntityRequestResult`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:245`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0233"></a>
### DTMAPI.Abstractions.CustomEntityRequestResult.Succeeded

- Symbol: `DTMAPI.Abstractions.CustomEntityRequestResult.Succeeded`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:247`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0234"></a>
### DTMAPI.Abstractions.CustomEntityRequestResult.Family

- Symbol: `DTMAPI.Abstractions.CustomEntityRequestResult.Family`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:248`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0235"></a>
### DTMAPI.Abstractions.CustomEntityRequestResult.OwnerUniqueId

- Symbol: `DTMAPI.Abstractions.CustomEntityRequestResult.OwnerUniqueId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:249`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0236"></a>
### DTMAPI.Abstractions.CustomEntityRequestResult.DefinitionId

- Symbol: `DTMAPI.Abstractions.CustomEntityRequestResult.DefinitionId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:250`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0237"></a>
### DTMAPI.Abstractions.CustomEntityRequestResult.Handle

- Symbol: `DTMAPI.Abstractions.CustomEntityRequestResult.Handle`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:251`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0238"></a>
### DTMAPI.Abstractions.CustomEntityRequestResult.FailureReason

- Symbol: `DTMAPI.Abstractions.CustomEntityRequestResult.FailureReason`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:252`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0239"></a>
### DTMAPI.Abstractions.CustomEntityRequestResult.Details

- Symbol: `DTMAPI.Abstractions.CustomEntityRequestResult.Details`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:253`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0240"></a>
### DTMAPI.Abstractions.CustomEntityRequestResult.RuntimeStatus

- Symbol: `DTMAPI.Abstractions.CustomEntityRequestResult.RuntimeStatus`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:254`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0241"></a>
### DTMAPI.Abstractions.CustomEntityRequestResult.Messages

- Symbol: `DTMAPI.Abstractions.CustomEntityRequestResult.Messages`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:255`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0242"></a>
### DTMAPI.Abstractions.CustomEntitySaveDataKey

- Symbol: `DTMAPI.Abstractions.CustomEntitySaveDataKey`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:258`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0243"></a>
### DTMAPI.Abstractions.CustomEntitySaveDataKey.Key

- Symbol: `DTMAPI.Abstractions.CustomEntitySaveDataKey.Key`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:260`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0244"></a>
### DTMAPI.Abstractions.CustomEntitySaveDataKey.Version

- Symbol: `DTMAPI.Abstractions.CustomEntitySaveDataKey.Version`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:261`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0245"></a>
### DTMAPI.Abstractions.CustomEntitySaveDataKey.Description

- Symbol: `DTMAPI.Abstractions.CustomEntitySaveDataKey.Description`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:262`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0246"></a>
### DTMAPI.Abstractions.CustomEntitySaveMigrationContext

- Symbol: `DTMAPI.Abstractions.CustomEntitySaveMigrationContext`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:265`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0247"></a>
### DTMAPI.Abstractions.CustomEntitySaveMigrationContext.Family

- Symbol: `DTMAPI.Abstractions.CustomEntitySaveMigrationContext.Family`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:267`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0248"></a>
### DTMAPI.Abstractions.CustomEntitySaveMigrationContext.OwnerUniqueId

- Symbol: `DTMAPI.Abstractions.CustomEntitySaveMigrationContext.OwnerUniqueId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:268`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0249"></a>
### DTMAPI.Abstractions.CustomEntitySaveMigrationContext.DefinitionId

- Symbol: `DTMAPI.Abstractions.CustomEntitySaveMigrationContext.DefinitionId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:269`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0250"></a>
### DTMAPI.Abstractions.CustomEntitySaveMigrationContext.FromVersion

- Symbol: `DTMAPI.Abstractions.CustomEntitySaveMigrationContext.FromVersion`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:270`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0251"></a>
### DTMAPI.Abstractions.CustomEntitySaveMigrationContext.ToVersion

- Symbol: `DTMAPI.Abstractions.CustomEntitySaveMigrationContext.ToVersion`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:271`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0252"></a>
### DTMAPI.Abstractions.CustomEntitySaveMigrationContext.SaveState

- Symbol: `DTMAPI.Abstractions.CustomEntitySaveMigrationContext.SaveState`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:272`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0253"></a>
### DTMAPI.Abstractions.CustomEntityTickPolicy

- Symbol: `DTMAPI.Abstractions.CustomEntityTickPolicy`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:275`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0254"></a>
### DTMAPI.Abstractions.CustomEntityTickPolicy.Kind

- Symbol: `DTMAPI.Abstractions.CustomEntityTickPolicy.Kind`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:277`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0255"></a>
### DTMAPI.Abstractions.CustomEntityTickPolicy.IntervalSeconds

- Symbol: `DTMAPI.Abstractions.CustomEntityTickPolicy.IntervalSeconds`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:278`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0256"></a>
### DTMAPI.Abstractions.CustomEntityTickPolicy.DeterministicOrder

- Symbol: `DTMAPI.Abstractions.CustomEntityTickPolicy.DeterministicOrder`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:279`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0257"></a>
### DTMAPI.Abstractions.CustomEntityTickPolicy.Order

- Symbol: `DTMAPI.Abstractions.CustomEntityTickPolicy.Order`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:280`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0258"></a>
### DTMAPI.Abstractions.CustomEntityPersistencePolicy

- Symbol: `DTMAPI.Abstractions.CustomEntityPersistencePolicy`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:283`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0259"></a>
### DTMAPI.Abstractions.CustomEntityPersistencePolicy.Kind

- Symbol: `DTMAPI.Abstractions.CustomEntityPersistencePolicy.Kind`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:285`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0260"></a>
### DTMAPI.Abstractions.CustomEntityPersistencePolicy.SchemaVersion

- Symbol: `DTMAPI.Abstractions.CustomEntityPersistencePolicy.SchemaVersion`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:286`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0261"></a>
### DTMAPI.Abstractions.CustomEntityPersistencePolicy.RemoveInstancesWhenOwnerMissing

- Symbol: `DTMAPI.Abstractions.CustomEntityPersistencePolicy.RemoveInstancesWhenOwnerMissing`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:287`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0262"></a>
### DTMAPI.Abstractions.CustomEntityPersistencePolicy.RestoreRuntimeInstancesOnSaveLoad

- Symbol: `DTMAPI.Abstractions.CustomEntityPersistencePolicy.RestoreRuntimeInstancesOnSaveLoad`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:288`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0263"></a>
### DTMAPI.Abstractions.CustomEntityPersistencePolicy.SaveKeys

- Symbol: `DTMAPI.Abstractions.CustomEntityPersistencePolicy.SaveKeys`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:289`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0264"></a>
### DTMAPI.Abstractions.CustomEntityBehaviorContext

- Symbol: `DTMAPI.Abstractions.CustomEntityBehaviorContext`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:292`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0265"></a>
### DTMAPI.Abstractions.CustomEntityBehaviorContext.Family

- Symbol: `DTMAPI.Abstractions.CustomEntityBehaviorContext.Family`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:294`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0266"></a>
### DTMAPI.Abstractions.CustomEntityBehaviorContext.OwnerUniqueId

- Symbol: `DTMAPI.Abstractions.CustomEntityBehaviorContext.OwnerUniqueId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:295`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0267"></a>
### DTMAPI.Abstractions.CustomEntityBehaviorContext.DefinitionId

- Symbol: `DTMAPI.Abstractions.CustomEntityBehaviorContext.DefinitionId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:296`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0268"></a>
### DTMAPI.Abstractions.CustomEntityBehaviorContext.Handle

- Symbol: `DTMAPI.Abstractions.CustomEntityBehaviorContext.Handle`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:297`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0269"></a>
### DTMAPI.Abstractions.CustomEntityBehaviorContext.UpdateTick

- Symbol: `DTMAPI.Abstractions.CustomEntityBehaviorContext.UpdateTick`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:298`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0270"></a>
### DTMAPI.Abstractions.CustomEntityBehaviorContext.SaveSlot

- Symbol: `DTMAPI.Abstractions.CustomEntityBehaviorContext.SaveSlot`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:299`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0271"></a>
### DTMAPI.Abstractions.CustomEntityBehaviorContext.RoomId

- Symbol: `DTMAPI.Abstractions.CustomEntityBehaviorContext.RoomId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:300`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0272"></a>
### DTMAPI.Abstractions.CustomEntityBehaviorContext.State

- Symbol: `DTMAPI.Abstractions.CustomEntityBehaviorContext.State`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:301`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0273"></a>
### DTMAPI.Abstractions.CustomEntityBehaviorResult

- Symbol: `DTMAPI.Abstractions.CustomEntityBehaviorResult`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:304`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0274"></a>
### DTMAPI.Abstractions.CustomEntityBehaviorResult.Succeeded

- Symbol: `DTMAPI.Abstractions.CustomEntityBehaviorResult.Succeeded`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:306`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0275"></a>
### DTMAPI.Abstractions.CustomEntityBehaviorResult.FailureReason

- Symbol: `DTMAPI.Abstractions.CustomEntityBehaviorResult.FailureReason`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:307`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0276"></a>
### DTMAPI.Abstractions.CustomEntityBehaviorResult.UpdatedState

- Symbol: `DTMAPI.Abstractions.CustomEntityBehaviorResult.UpdatedState`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:308`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0277"></a>
### DTMAPI.Abstractions.ICustomEntityBehaviorProvider

- Symbol: `DTMAPI.Abstractions.ICustomEntityBehaviorProvider`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:311`
- Implementation: No runtime implementation; public type is a data/contract surface or enum.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0278"></a>
### DTMAPI.Abstractions.ICustomEntityBehaviorProvider.OnRegistered(CustomEntityBehaviorContext context)

- Symbol: `DTMAPI.Abstractions.ICustomEntityBehaviorProvider.OnRegistered(CustomEntityBehaviorContext context)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:313`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:75`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:75
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0279"></a>
### DTMAPI.Abstractions.ICustomEntityBehaviorProvider.OnUnregistered(CustomEntityBehaviorContext context)

- Symbol: `DTMAPI.Abstractions.ICustomEntityBehaviorProvider.OnUnregistered(CustomEntityBehaviorContext context)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:314`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:84`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:84
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0280"></a>
### DTMAPI.Abstractions.ICustomEntityBehaviorProvider.MigrateSaveState(CustomEntitySaveMigrationContext context)

- Symbol: `DTMAPI.Abstractions.ICustomEntityBehaviorProvider.MigrateSaveState(CustomEntitySaveMigrationContext context)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:315`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0281"></a>
### DTMAPI.Abstractions.ICustomAnimalApi

- Symbol: `DTMAPI.Abstractions.ICustomAnimalApi`
- Current marker: `stable`
- Review advice: 保持
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:319`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9`
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:46 `ICustomAnimalApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9
- Result: OK
- Recommendation: Keep current public contract; no implementation goal is created by this audit.

<a id="sym-0282"></a>
### DTMAPI.Abstractions.ICustomAnimalApi.LifecycleChanged

- Symbol: `DTMAPI.Abstractions.ICustomAnimalApi.LifecycleChanged`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:321`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:36`
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:46 `ICustomAnimalApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:36
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0283"></a>
### DTMAPI.Abstractions.ICustomAnimalApi.RegisterSpecies(IManifest owner, CustomAnimalSpeciesDefinition definition)

- Symbol: `DTMAPI.Abstractions.ICustomAnimalApi.RegisterSpecies(IManifest owner, CustomAnimalSpeciesDefinition definition)`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:322`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:60`
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:46 `ICustomAnimalApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:60
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0284"></a>
### DTMAPI.Abstractions.ICustomAnimalApi.UnregisterSpecies(IManifest owner, string speciesId)

- Symbol: `DTMAPI.Abstractions.ICustomAnimalApi.UnregisterSpecies(IManifest owner, string speciesId)`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:323`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:80`
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:46 `ICustomAnimalApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:80
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0285"></a>
### DTMAPI.Abstractions.ICustomAnimalApi.GetSpeciesDefinitions(string? ownerUniqueId = null)

- Symbol: `DTMAPI.Abstractions.ICustomAnimalApi.GetSpeciesDefinitions(string? ownerUniqueId = null)`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:324`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:89`
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:46 `ICustomAnimalApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:89
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0286"></a>
### DTMAPI.Abstractions.ICustomAnimalApi.GetSpeciesDefinition(string speciesId)

- Symbol: `DTMAPI.Abstractions.ICustomAnimalApi.GetSpeciesDefinition(string speciesId)`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:325`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:95`
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:46 `ICustomAnimalApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:95
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0287"></a>
### DTMAPI.Abstractions.ICustomAnimalApi.RequestSpawn(IManifest owner, CustomAnimalSpawnRequest request)

- Symbol: `DTMAPI.Abstractions.ICustomAnimalApi.RequestSpawn(IManifest owner, CustomAnimalSpawnRequest request)`
- Current marker: `stable`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:326`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:101`
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:46 `ICustomAnimalApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:101
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0288"></a>
### DTMAPI.Abstractions.ICustomAnimalApi.RequestRemove(IManifest owner, CustomEntityHandle animalHandle, string reason)

- Symbol: `DTMAPI.Abstractions.ICustomAnimalApi.RequestRemove(IManifest owner, CustomEntityHandle animalHandle, string reason)`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 禁止依赖
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:327`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:116`
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:46 `ICustomAnimalApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:116
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0289"></a>
### DTMAPI.Abstractions.ICustomAnimalApi.GetAnimalInstances(string? ownerUniqueId = null)

- Symbol: `DTMAPI.Abstractions.ICustomAnimalApi.GetAnimalInstances(string? ownerUniqueId = null)`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:328`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:121`
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:46 `ICustomAnimalApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:121
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0290"></a>
### DTMAPI.Abstractions.ICustomAnimalApi.GetAnimalInstance(CustomEntityHandle handle)

- Symbol: `DTMAPI.Abstractions.ICustomAnimalApi.GetAnimalInstance(CustomEntityHandle handle)`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:329`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:127`
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:46 `ICustomAnimalApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:127
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0291"></a>
### DTMAPI.Abstractions.ICustomAnimalApi.GetSnapshot(string? ownerUniqueId = null)

- Symbol: `DTMAPI.Abstractions.ICustomAnimalApi.GetSnapshot(string? ownerUniqueId = null)`
- Current marker: `stable`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:330`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:135`
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:46 `ICustomAnimalApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:135
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0292"></a>
### DTMAPI.Abstractions.ICustomAnimalApi.GetStatus(string? ownerUniqueId = null)

- Symbol: `DTMAPI.Abstractions.ICustomAnimalApi.GetStatus(string? ownerUniqueId = null)`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:331`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:136`
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:46 `ICustomAnimalApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:136
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0293"></a>
### DTMAPI.Abstractions.ICustomMonsterApi

- Symbol: `DTMAPI.Abstractions.ICustomMonsterApi`
- Current marker: `stable`
- Review advice: 保持
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:335`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9`
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:47 `ICustomMonsterApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9
- Result: OK
- Recommendation: Keep current public contract; no implementation goal is created by this audit.

<a id="sym-0294"></a>
### DTMAPI.Abstractions.ICustomMonsterApi.LifecycleChanged

- Symbol: `DTMAPI.Abstractions.ICustomMonsterApi.LifecycleChanged`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:337`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:42`
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:47 `ICustomMonsterApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:42
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0295"></a>
### DTMAPI.Abstractions.ICustomMonsterApi.RegisterMonster(IManifest owner, CustomMonsterDefinition definition)

- Symbol: `DTMAPI.Abstractions.ICustomMonsterApi.RegisterMonster(IManifest owner, CustomMonsterDefinition definition)`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:338`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:138`
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:47 `ICustomMonsterApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:138
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0296"></a>
### DTMAPI.Abstractions.ICustomMonsterApi.RegisterSpawnTable(IManifest owner, CustomMonsterSpawnTableDefinition spawnTable)

- Symbol: `DTMAPI.Abstractions.ICustomMonsterApi.RegisterSpawnTable(IManifest owner, CustomMonsterSpawnTableDefinition spawnTable)`
- Current marker: `stable`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:339`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:158`
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:47 `ICustomMonsterApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:158
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0297"></a>
### DTMAPI.Abstractions.ICustomMonsterApi.UnregisterMonster(IManifest owner, string monsterId)

- Symbol: `DTMAPI.Abstractions.ICustomMonsterApi.UnregisterMonster(IManifest owner, string monsterId)`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:340`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:177`
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:47 `ICustomMonsterApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:177
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0298"></a>
### DTMAPI.Abstractions.ICustomMonsterApi.GetMonsterDefinitions(string? ownerUniqueId = null)

- Symbol: `DTMAPI.Abstractions.ICustomMonsterApi.GetMonsterDefinitions(string? ownerUniqueId = null)`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:341`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:186`
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:47 `ICustomMonsterApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:186
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0299"></a>
### DTMAPI.Abstractions.ICustomMonsterApi.GetMonsterDefinition(string monsterId)

- Symbol: `DTMAPI.Abstractions.ICustomMonsterApi.GetMonsterDefinition(string monsterId)`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:342`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:192`
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:47 `ICustomMonsterApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:192
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0300"></a>
### DTMAPI.Abstractions.ICustomMonsterApi.RequestSpawn(IManifest owner, CustomMonsterSpawnRequest request)

- Symbol: `DTMAPI.Abstractions.ICustomMonsterApi.RequestSpawn(IManifest owner, CustomMonsterSpawnRequest request)`
- Current marker: `stable`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:343`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:101`
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:47 `ICustomMonsterApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:101
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0301"></a>
### DTMAPI.Abstractions.ICustomMonsterApi.RequestDespawn(IManifest owner, CustomEntityHandle monsterHandle, string reason)

- Symbol: `DTMAPI.Abstractions.ICustomMonsterApi.RequestDespawn(IManifest owner, CustomEntityHandle monsterHandle, string reason)`
- Current marker: `stable`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:344`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:213`
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:47 `ICustomMonsterApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:213
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0302"></a>
### DTMAPI.Abstractions.ICustomMonsterApi.GetMonsterInstances(string? ownerUniqueId = null)

- Symbol: `DTMAPI.Abstractions.ICustomMonsterApi.GetMonsterInstances(string? ownerUniqueId = null)`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:345`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:218`
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:47 `ICustomMonsterApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:218
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0303"></a>
### DTMAPI.Abstractions.ICustomMonsterApi.GetMonsterInstance(CustomEntityHandle handle)

- Symbol: `DTMAPI.Abstractions.ICustomMonsterApi.GetMonsterInstance(CustomEntityHandle handle)`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:346`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:224`
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:47 `ICustomMonsterApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:224
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0304"></a>
### DTMAPI.Abstractions.ICustomMonsterApi.GetSnapshot(string? ownerUniqueId = null)

- Symbol: `DTMAPI.Abstractions.ICustomMonsterApi.GetSnapshot(string? ownerUniqueId = null)`
- Current marker: `stable`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:347`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:232`
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:47 `ICustomMonsterApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:232
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0305"></a>
### DTMAPI.Abstractions.ICustomMonsterApi.GetStatus(string? ownerUniqueId = null)

- Symbol: `DTMAPI.Abstractions.ICustomMonsterApi.GetStatus(string? ownerUniqueId = null)`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:348`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:233`
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:47 `ICustomMonsterApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:233
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0306"></a>
### DTMAPI.Abstractions.ICustomAttackApi

- Symbol: `DTMAPI.Abstractions.ICustomAttackApi`
- Current marker: `stable`
- Review advice: 保持
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:352`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9`
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:48 `ICustomAttackApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9
- Result: OK
- Recommendation: Keep current public contract; no implementation goal is created by this audit.

<a id="sym-0307"></a>
### DTMAPI.Abstractions.ICustomAttackApi.LifecycleChanged

- Symbol: `DTMAPI.Abstractions.ICustomAttackApi.LifecycleChanged`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:354`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:48`
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:48 `ICustomAttackApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:48
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0308"></a>
### DTMAPI.Abstractions.ICustomAttackApi.RegisterAttack(IManifest owner, CustomAttackDefinition definition)

- Symbol: `DTMAPI.Abstractions.ICustomAttackApi.RegisterAttack(IManifest owner, CustomAttackDefinition definition)`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:355`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:235`
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:48 `ICustomAttackApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:235
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0309"></a>
### DTMAPI.Abstractions.ICustomAttackApi.UnregisterAttack(IManifest owner, string attackId)

- Symbol: `DTMAPI.Abstractions.ICustomAttackApi.UnregisterAttack(IManifest owner, string attackId)`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:356`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:257`
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:48 `ICustomAttackApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:257
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0310"></a>
### DTMAPI.Abstractions.ICustomAttackApi.GetAttackDefinitions(string? ownerUniqueId = null)

- Symbol: `DTMAPI.Abstractions.ICustomAttackApi.GetAttackDefinitions(string? ownerUniqueId = null)`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:357`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:266`
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:48 `ICustomAttackApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:266
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0311"></a>
### DTMAPI.Abstractions.ICustomAttackApi.GetAttackDefinition(string attackId)

- Symbol: `DTMAPI.Abstractions.ICustomAttackApi.GetAttackDefinition(string attackId)`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:358`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:272`
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:48 `ICustomAttackApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:272
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0312"></a>
### DTMAPI.Abstractions.ICustomAttackApi.SpawnProjectile(IManifest owner, CustomAttackSpawnRequest request)

- Symbol: `DTMAPI.Abstractions.ICustomAttackApi.SpawnProjectile(IManifest owner, CustomAttackSpawnRequest request)`
- Current marker: `stable`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:359`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:278`
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:48 `ICustomAttackApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:278
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0313"></a>
### DTMAPI.Abstractions.ICustomAttackApi.ExecuteAttack(IManifest owner, CustomAttackSpawnRequest request)

- Symbol: `DTMAPI.Abstractions.ICustomAttackApi.ExecuteAttack(IManifest owner, CustomAttackSpawnRequest request)`
- Current marker: `stable`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:360`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:283`
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:48 `ICustomAttackApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:283
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0314"></a>
### DTMAPI.Abstractions.ICustomAttackApi.RequestExpire(IManifest owner, CustomEntityHandle attackHandle, string reason)

- Symbol: `DTMAPI.Abstractions.ICustomAttackApi.RequestExpire(IManifest owner, CustomEntityHandle attackHandle, string reason)`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 禁止依赖
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:361`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:288`
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:48 `ICustomAttackApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:288
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0315"></a>
### DTMAPI.Abstractions.ICustomAttackApi.GetActiveAttacks(string? ownerUniqueId = null)

- Symbol: `DTMAPI.Abstractions.ICustomAttackApi.GetActiveAttacks(string? ownerUniqueId = null)`
- Current marker: `stable`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:362`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:293`
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:48 `ICustomAttackApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:293
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0316"></a>
### DTMAPI.Abstractions.ICustomAttackApi.GetAttackInstance(CustomEntityHandle handle)

- Symbol: `DTMAPI.Abstractions.ICustomAttackApi.GetAttackInstance(CustomEntityHandle handle)`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:363`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:299`
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:48 `ICustomAttackApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:299
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0317"></a>
### DTMAPI.Abstractions.ICustomAttackApi.GetSnapshot(string? ownerUniqueId = null)

- Symbol: `DTMAPI.Abstractions.ICustomAttackApi.GetSnapshot(string? ownerUniqueId = null)`
- Current marker: `stable`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:364`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:307`
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:48 `ICustomAttackApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:307
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0318"></a>
### DTMAPI.Abstractions.ICustomAttackApi.GetStatus(string? ownerUniqueId = null)

- Symbol: `DTMAPI.Abstractions.ICustomAttackApi.GetStatus(string? ownerUniqueId = null)`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:365`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:308`
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:48 `ICustomAttackApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:308
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0319"></a>
### DTMAPI.Abstractions.ICustomDroneApi

- Symbol: `DTMAPI.Abstractions.ICustomDroneApi`
- Current marker: `stable`
- Review advice: 保持
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:369`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9`
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:49 `ICustomDroneApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9
- Result: OK
- Recommendation: Keep current public contract; no implementation goal is created by this audit.

<a id="sym-0320"></a>
### DTMAPI.Abstractions.ICustomDroneApi.LifecycleChanged

- Symbol: `DTMAPI.Abstractions.ICustomDroneApi.LifecycleChanged`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:371`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:54`
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:49 `ICustomDroneApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:54
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0321"></a>
### DTMAPI.Abstractions.ICustomDroneApi.RegisterDrone(IManifest owner, CustomDroneDefinition definition)

- Symbol: `DTMAPI.Abstractions.ICustomDroneApi.RegisterDrone(IManifest owner, CustomDroneDefinition definition)`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:372`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:310`
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:49 `ICustomDroneApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:310
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0322"></a>
### DTMAPI.Abstractions.ICustomDroneApi.UnregisterDrone(IManifest owner, string droneId)

- Symbol: `DTMAPI.Abstractions.ICustomDroneApi.UnregisterDrone(IManifest owner, string droneId)`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:373`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:330`
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:49 `ICustomDroneApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:330
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0323"></a>
### DTMAPI.Abstractions.ICustomDroneApi.GetDroneDefinitions(string? ownerUniqueId = null)

- Symbol: `DTMAPI.Abstractions.ICustomDroneApi.GetDroneDefinitions(string? ownerUniqueId = null)`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:374`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:339`
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:49 `ICustomDroneApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:339
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0324"></a>
### DTMAPI.Abstractions.ICustomDroneApi.GetDroneDefinition(string droneId)

- Symbol: `DTMAPI.Abstractions.ICustomDroneApi.GetDroneDefinition(string droneId)`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:375`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:345`
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:49 `ICustomDroneApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:345
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0325"></a>
### DTMAPI.Abstractions.ICustomDroneApi.RequestSummon(IManifest owner, CustomDroneSummonRequest request)

- Symbol: `DTMAPI.Abstractions.ICustomDroneApi.RequestSummon(IManifest owner, CustomDroneSummonRequest request)`
- Current marker: `stable`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:376`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:351`
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:49 `ICustomDroneApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:351
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0326"></a>
### DTMAPI.Abstractions.ICustomDroneApi.RequestDismiss(IManifest owner, CustomEntityHandle droneHandle, string reason)

- Symbol: `DTMAPI.Abstractions.ICustomDroneApi.RequestDismiss(IManifest owner, CustomEntityHandle droneHandle, string reason)`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:377`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:366`
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:49 `ICustomDroneApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:366
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0327"></a>
### DTMAPI.Abstractions.ICustomDroneApi.Equip(IManifest owner, CustomEntityHandle droneHandle, CustomDroneEquipmentRequest request)

- Symbol: `DTMAPI.Abstractions.ICustomDroneApi.Equip(IManifest owner, CustomEntityHandle droneHandle, CustomDroneEquipmentRequest request)`
- Current marker: `stable`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:378`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:371`
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:49 `ICustomDroneApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:371
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0328"></a>
### DTMAPI.Abstractions.ICustomDroneApi.SetMode(IManifest owner, CustomEntityHandle droneHandle, CustomDroneCommandRequest request)

- Symbol: `DTMAPI.Abstractions.ICustomDroneApi.SetMode(IManifest owner, CustomEntityHandle droneHandle, CustomDroneCommandRequest request)`
- Current marker: `stable`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:379`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:390`
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:49 `ICustomDroneApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:390
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0329"></a>
### DTMAPI.Abstractions.ICustomDroneApi.GetDroneInstances(string? ownerUniqueId = null)

- Symbol: `DTMAPI.Abstractions.ICustomDroneApi.GetDroneInstances(string? ownerUniqueId = null)`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:380`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:408`
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:49 `ICustomDroneApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:408
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0330"></a>
### DTMAPI.Abstractions.ICustomDroneApi.GetDroneInstance(CustomEntityHandle handle)

- Symbol: `DTMAPI.Abstractions.ICustomDroneApi.GetDroneInstance(CustomEntityHandle handle)`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:381`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:414`
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:49 `ICustomDroneApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:414
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0331"></a>
### DTMAPI.Abstractions.ICustomDroneApi.GetSnapshot(string? ownerUniqueId = null)

- Symbol: `DTMAPI.Abstractions.ICustomDroneApi.GetSnapshot(string? ownerUniqueId = null)`
- Current marker: `stable`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:382`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:422`
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:49 `ICustomDroneApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:422
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0332"></a>
### DTMAPI.Abstractions.ICustomDroneApi.GetStatus(string? ownerUniqueId = null)

- Symbol: `DTMAPI.Abstractions.ICustomDroneApi.GetStatus(string? ownerUniqueId = null)`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:383`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:423`
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:49 `ICustomDroneApi` (stable)
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:423
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0333"></a>
### DTMAPI.Abstractions.ICustomAnimalBehaviorProvider

- Symbol: `DTMAPI.Abstractions.ICustomAnimalBehaviorProvider`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:386`
- Implementation: No runtime implementation; public type is a data/contract surface or enum.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0334"></a>
### DTMAPI.Abstractions.ICustomAnimalBehaviorProvider.OnTick(CustomAnimalBehaviorContext context)

- Symbol: `DTMAPI.Abstractions.ICustomAnimalBehaviorProvider.OnTick(CustomAnimalBehaviorContext context)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:388`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0335"></a>
### DTMAPI.Abstractions.ICustomAnimalBehaviorProvider.OnFeedRequested(CustomAnimalBehaviorContext context, string itemId, int amount)

- Symbol: `DTMAPI.Abstractions.ICustomAnimalBehaviorProvider.OnFeedRequested(CustomAnimalBehaviorContext context, string itemId, int amount)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:389`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0336"></a>
### DTMAPI.Abstractions.ICustomAnimalBehaviorProvider.OnExcrementProduced(CustomAnimalLifecycleEventArgs args)

- Symbol: `DTMAPI.Abstractions.ICustomAnimalBehaviorProvider.OnExcrementProduced(CustomAnimalLifecycleEventArgs args)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:390`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0337"></a>
### DTMAPI.Abstractions.ICustomAnimalBehaviorProvider.OnBreedingEvent(CustomAnimalLifecycleEventArgs args)

- Symbol: `DTMAPI.Abstractions.ICustomAnimalBehaviorProvider.OnBreedingEvent(CustomAnimalLifecycleEventArgs args)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:391`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0338"></a>
### DTMAPI.Abstractions.ICustomAnimalBehaviorProvider.OnHiddenProductChanged(CustomAnimalLifecycleEventArgs args)

- Symbol: `DTMAPI.Abstractions.ICustomAnimalBehaviorProvider.OnHiddenProductChanged(CustomAnimalLifecycleEventArgs args)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:392`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0339"></a>
### DTMAPI.Abstractions.CustomAnimalSpeciesDefinition

- Symbol: `DTMAPI.Abstractions.CustomAnimalSpeciesDefinition`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:395`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0340"></a>
### DTMAPI.Abstractions.CustomAnimalSpeciesDefinition.SpeciesId

- Symbol: `DTMAPI.Abstractions.CustomAnimalSpeciesDefinition.SpeciesId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:397`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0341"></a>
### DTMAPI.Abstractions.CustomAnimalSpeciesDefinition.VariantIds

- Symbol: `DTMAPI.Abstractions.CustomAnimalSpeciesDefinition.VariantIds`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:398`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0342"></a>
### DTMAPI.Abstractions.CustomAnimalSpeciesDefinition.DisplayName

- Symbol: `DTMAPI.Abstractions.CustomAnimalSpeciesDefinition.DisplayName`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:399`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0343"></a>
### DTMAPI.Abstractions.CustomAnimalSpeciesDefinition.Description

- Symbol: `DTMAPI.Abstractions.CustomAnimalSpeciesDefinition.Description`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:400`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0344"></a>
### DTMAPI.Abstractions.CustomAnimalSpeciesDefinition.Icon

- Symbol: `DTMAPI.Abstractions.CustomAnimalSpeciesDefinition.Icon`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:401`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0345"></a>
### DTMAPI.Abstractions.CustomAnimalSpeciesDefinition.Sprite

- Symbol: `DTMAPI.Abstractions.CustomAnimalSpeciesDefinition.Sprite`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:402`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0346"></a>
### DTMAPI.Abstractions.CustomAnimalSpeciesDefinition.LifeStages

- Symbol: `DTMAPI.Abstractions.CustomAnimalSpeciesDefinition.LifeStages`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:403`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0347"></a>
### DTMAPI.Abstractions.CustomAnimalSpeciesDefinition.Habitat

- Symbol: `DTMAPI.Abstractions.CustomAnimalSpeciesDefinition.Habitat`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:404`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0348"></a>
### DTMAPI.Abstractions.CustomAnimalSpeciesDefinition.Diet

- Symbol: `DTMAPI.Abstractions.CustomAnimalSpeciesDefinition.Diet`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:405`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0349"></a>
### DTMAPI.Abstractions.CustomAnimalSpeciesDefinition.Consumption

- Symbol: `DTMAPI.Abstractions.CustomAnimalSpeciesDefinition.Consumption`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:406`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0350"></a>
### DTMAPI.Abstractions.CustomAnimalSpeciesDefinition.Excrement

- Symbol: `DTMAPI.Abstractions.CustomAnimalSpeciesDefinition.Excrement`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:407`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0351"></a>
### DTMAPI.Abstractions.CustomAnimalSpeciesDefinition.Breeding

- Symbol: `DTMAPI.Abstractions.CustomAnimalSpeciesDefinition.Breeding`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:408`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0352"></a>
### DTMAPI.Abstractions.CustomAnimalSpeciesDefinition.HiddenProducts

- Symbol: `DTMAPI.Abstractions.CustomAnimalSpeciesDefinition.HiddenProducts`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:409`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0353"></a>
### DTMAPI.Abstractions.CustomAnimalSpeciesDefinition.ProduceRules

- Symbol: `DTMAPI.Abstractions.CustomAnimalSpeciesDefinition.ProduceRules`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:410`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0354"></a>
### DTMAPI.Abstractions.CustomAnimalSpeciesDefinition.Stats

- Symbol: `DTMAPI.Abstractions.CustomAnimalSpeciesDefinition.Stats`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:411`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0355"></a>
### DTMAPI.Abstractions.CustomAnimalSpeciesDefinition.Persistence

- Symbol: `DTMAPI.Abstractions.CustomAnimalSpeciesDefinition.Persistence`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:412`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0356"></a>
### DTMAPI.Abstractions.CustomAnimalSpeciesDefinition.TickPolicy

- Symbol: `DTMAPI.Abstractions.CustomAnimalSpeciesDefinition.TickPolicy`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:413`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0357"></a>
### DTMAPI.Abstractions.CustomAnimalSpeciesDefinition.Provider

- Symbol: `DTMAPI.Abstractions.CustomAnimalSpeciesDefinition.Provider`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:414`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0358"></a>
### DTMAPI.Abstractions.CustomAnimalLifeStageDefinition

- Symbol: `DTMAPI.Abstractions.CustomAnimalLifeStageDefinition`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:417`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0359"></a>
### DTMAPI.Abstractions.CustomAnimalLifeStageDefinition.StageId

- Symbol: `DTMAPI.Abstractions.CustomAnimalLifeStageDefinition.StageId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:419`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0360"></a>
### DTMAPI.Abstractions.CustomAnimalLifeStageDefinition.MinimumAgeDays

- Symbol: `DTMAPI.Abstractions.CustomAnimalLifeStageDefinition.MinimumAgeDays`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:420`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0361"></a>
### DTMAPI.Abstractions.CustomAnimalLifeStageDefinition.MaximumAgeDays

- Symbol: `DTMAPI.Abstractions.CustomAnimalLifeStageDefinition.MaximumAgeDays`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:421`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0362"></a>
### DTMAPI.Abstractions.CustomAnimalLifeStageDefinition.DisplayName

- Symbol: `DTMAPI.Abstractions.CustomAnimalLifeStageDefinition.DisplayName`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:422`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0363"></a>
### DTMAPI.Abstractions.CustomAnimalLifeStageDefinition.Sprite

- Symbol: `DTMAPI.Abstractions.CustomAnimalLifeStageDefinition.Sprite`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:423`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0364"></a>
### DTMAPI.Abstractions.CustomAnimalHabitatPolicy

- Symbol: `DTMAPI.Abstractions.CustomAnimalHabitatPolicy`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:426`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0365"></a>
### DTMAPI.Abstractions.CustomAnimalHabitatPolicy.AllowedRoomIds

- Symbol: `DTMAPI.Abstractions.CustomAnimalHabitatPolicy.AllowedRoomIds`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:428`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0366"></a>
### DTMAPI.Abstractions.CustomAnimalHabitatPolicy.AllowedBuildingTags

- Symbol: `DTMAPI.Abstractions.CustomAnimalHabitatPolicy.AllowedBuildingTags`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:429`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0367"></a>
### DTMAPI.Abstractions.CustomAnimalHabitatPolicy.AllowedHabitatTags

- Symbol: `DTMAPI.Abstractions.CustomAnimalHabitatPolicy.AllowedHabitatTags`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:430`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0368"></a>
### DTMAPI.Abstractions.CustomAnimalHabitatPolicy.PopulationLimitPerRoom

- Symbol: `DTMAPI.Abstractions.CustomAnimalHabitatPolicy.PopulationLimitPerRoom`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:431`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0369"></a>
### DTMAPI.Abstractions.CustomAnimalHabitatPolicy.RequiresShelter

- Symbol: `DTMAPI.Abstractions.CustomAnimalHabitatPolicy.RequiresShelter`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:432`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0370"></a>
### DTMAPI.Abstractions.CustomAnimalDietPolicy

- Symbol: `DTMAPI.Abstractions.CustomAnimalDietPolicy`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:435`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0371"></a>
### DTMAPI.Abstractions.CustomAnimalDietPolicy.AcceptedItemIds

- Symbol: `DTMAPI.Abstractions.CustomAnimalDietPolicy.AcceptedItemIds`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:437`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0372"></a>
### DTMAPI.Abstractions.CustomAnimalDietPolicy.AcceptedItemTags

- Symbol: `DTMAPI.Abstractions.CustomAnimalDietPolicy.AcceptedItemTags`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:438`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0373"></a>
### DTMAPI.Abstractions.CustomAnimalDietPolicy.UnitsPerFeeding

- Symbol: `DTMAPI.Abstractions.CustomAnimalDietPolicy.UnitsPerFeeding`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:439`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0374"></a>
### DTMAPI.Abstractions.CustomAnimalDietPolicy.CanGraze

- Symbol: `DTMAPI.Abstractions.CustomAnimalDietPolicy.CanGraze`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:440`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0375"></a>
### DTMAPI.Abstractions.CustomAnimalConsumptionPolicy

- Symbol: `DTMAPI.Abstractions.CustomAnimalConsumptionPolicy`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:443`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0376"></a>
### DTMAPI.Abstractions.CustomAnimalConsumptionPolicy.HungerIntervalHours

- Symbol: `DTMAPI.Abstractions.CustomAnimalConsumptionPolicy.HungerIntervalHours`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:445`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0377"></a>
### DTMAPI.Abstractions.CustomAnimalConsumptionPolicy.MaxFeedCapacity

- Symbol: `DTMAPI.Abstractions.CustomAnimalConsumptionPolicy.MaxFeedCapacity`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:446`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0378"></a>
### DTMAPI.Abstractions.CustomAnimalConsumptionPolicy.TrackFedState

- Symbol: `DTMAPI.Abstractions.CustomAnimalConsumptionPolicy.TrackFedState`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:447`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0379"></a>
### DTMAPI.Abstractions.CustomAnimalConsumptionPolicy.RaiseFeedEvents

- Symbol: `DTMAPI.Abstractions.CustomAnimalConsumptionPolicy.RaiseFeedEvents`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:448`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0380"></a>
### DTMAPI.Abstractions.CustomAnimalExcrementPolicy

- Symbol: `DTMAPI.Abstractions.CustomAnimalExcrementPolicy`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:451`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0381"></a>
### DTMAPI.Abstractions.CustomAnimalExcrementPolicy.Enabled

- Symbol: `DTMAPI.Abstractions.CustomAnimalExcrementPolicy.Enabled`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:453`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0382"></a>
### DTMAPI.Abstractions.CustomAnimalExcrementPolicy.IntervalHours

- Symbol: `DTMAPI.Abstractions.CustomAnimalExcrementPolicy.IntervalHours`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:454`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0383"></a>
### DTMAPI.Abstractions.CustomAnimalExcrementPolicy.MaxPendingCount

- Symbol: `DTMAPI.Abstractions.CustomAnimalExcrementPolicy.MaxPendingCount`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:455`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0384"></a>
### DTMAPI.Abstractions.CustomAnimalExcrementPolicy.Outputs

- Symbol: `DTMAPI.Abstractions.CustomAnimalExcrementPolicy.Outputs`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:456`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0385"></a>
### DTMAPI.Abstractions.CustomAnimalExcrementPolicy.RequiresManualCleanup

- Symbol: `DTMAPI.Abstractions.CustomAnimalExcrementPolicy.RequiresManualCleanup`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:457`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0386"></a>
### DTMAPI.Abstractions.CustomAnimalBreedingPolicy

- Symbol: `DTMAPI.Abstractions.CustomAnimalBreedingPolicy`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:460`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0387"></a>
### DTMAPI.Abstractions.CustomAnimalBreedingPolicy.Enabled

- Symbol: `DTMAPI.Abstractions.CustomAnimalBreedingPolicy.Enabled`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:462`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0388"></a>
### DTMAPI.Abstractions.CustomAnimalBreedingPolicy.CompatibleSpeciesIds

- Symbol: `DTMAPI.Abstractions.CustomAnimalBreedingPolicy.CompatibleSpeciesIds`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:463`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0389"></a>
### DTMAPI.Abstractions.CustomAnimalBreedingPolicy.CooldownHours

- Symbol: `DTMAPI.Abstractions.CustomAnimalBreedingPolicy.CooldownHours`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:464`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0390"></a>
### DTMAPI.Abstractions.CustomAnimalBreedingPolicy.PregnancyOrIncubationHours

- Symbol: `DTMAPI.Abstractions.CustomAnimalBreedingPolicy.PregnancyOrIncubationHours`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:465`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0391"></a>
### DTMAPI.Abstractions.CustomAnimalBreedingPolicy.OffspringCount

- Symbol: `DTMAPI.Abstractions.CustomAnimalBreedingPolicy.OffspringCount`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:466`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0392"></a>
### DTMAPI.Abstractions.CustomAnimalBreedingPolicy.PopulationLimitPerOwner

- Symbol: `DTMAPI.Abstractions.CustomAnimalBreedingPolicy.PopulationLimitPerOwner`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:467`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0393"></a>
### DTMAPI.Abstractions.CustomAnimalProductRule

- Symbol: `DTMAPI.Abstractions.CustomAnimalProductRule`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:470`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0394"></a>
### DTMAPI.Abstractions.CustomAnimalProductRule.ProductId

- Symbol: `DTMAPI.Abstractions.CustomAnimalProductRule.ProductId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:472`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0395"></a>
### DTMAPI.Abstractions.CustomAnimalProductRule.DisplayName

- Symbol: `DTMAPI.Abstractions.CustomAnimalProductRule.DisplayName`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:473`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0396"></a>
### DTMAPI.Abstractions.CustomAnimalProductRule.HiddenUntilReady

- Symbol: `DTMAPI.Abstractions.CustomAnimalProductRule.HiddenUntilReady`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:474`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0397"></a>
### DTMAPI.Abstractions.CustomAnimalProductRule.ProgressPerGameHour

- Symbol: `DTMAPI.Abstractions.CustomAnimalProductRule.ProgressPerGameHour`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:475`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0398"></a>
### DTMAPI.Abstractions.CustomAnimalProductRule.RequiredProgress

- Symbol: `DTMAPI.Abstractions.CustomAnimalProductRule.RequiredProgress`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:476`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0399"></a>
### DTMAPI.Abstractions.CustomAnimalProductRule.Outputs

- Symbol: `DTMAPI.Abstractions.CustomAnimalProductRule.Outputs`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:477`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0400"></a>
### DTMAPI.Abstractions.CustomAnimalProductRule.RequiredStateTags

- Symbol: `DTMAPI.Abstractions.CustomAnimalProductRule.RequiredStateTags`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:478`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0401"></a>
### DTMAPI.Abstractions.CustomAnimalItemOutput

- Symbol: `DTMAPI.Abstractions.CustomAnimalItemOutput`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:481`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0402"></a>
### DTMAPI.Abstractions.CustomAnimalItemOutput.ItemId

- Symbol: `DTMAPI.Abstractions.CustomAnimalItemOutput.ItemId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:483`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0403"></a>
### DTMAPI.Abstractions.CustomAnimalItemOutput.MinStack

- Symbol: `DTMAPI.Abstractions.CustomAnimalItemOutput.MinStack`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:484`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0404"></a>
### DTMAPI.Abstractions.CustomAnimalItemOutput.MaxStack

- Symbol: `DTMAPI.Abstractions.CustomAnimalItemOutput.MaxStack`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:485`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0405"></a>
### DTMAPI.Abstractions.CustomAnimalItemOutput.Chance

- Symbol: `DTMAPI.Abstractions.CustomAnimalItemOutput.Chance`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:486`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0406"></a>
### DTMAPI.Abstractions.CustomAnimalStats

- Symbol: `DTMAPI.Abstractions.CustomAnimalStats`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:489`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0407"></a>
### DTMAPI.Abstractions.CustomAnimalStats.MaxHealth

- Symbol: `DTMAPI.Abstractions.CustomAnimalStats.MaxHealth`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:491`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0408"></a>
### DTMAPI.Abstractions.CustomAnimalStats.MaxMood

- Symbol: `DTMAPI.Abstractions.CustomAnimalStats.MaxMood`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:492`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0409"></a>
### DTMAPI.Abstractions.CustomAnimalStats.MaxFriendship

- Symbol: `DTMAPI.Abstractions.CustomAnimalStats.MaxFriendship`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:493`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0410"></a>
### DTMAPI.Abstractions.CustomAnimalStats.MoveSpeed

- Symbol: `DTMAPI.Abstractions.CustomAnimalStats.MoveSpeed`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:494`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0411"></a>
### DTMAPI.Abstractions.CustomAnimalStats.CustomValues

- Symbol: `DTMAPI.Abstractions.CustomAnimalStats.CustomValues`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:495`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0412"></a>
### DTMAPI.Abstractions.CustomAnimalSpawnRequest

- Symbol: `DTMAPI.Abstractions.CustomAnimalSpawnRequest`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:498`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0413"></a>
### DTMAPI.Abstractions.CustomAnimalSpawnRequest.SpeciesId

- Symbol: `DTMAPI.Abstractions.CustomAnimalSpawnRequest.SpeciesId`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:500`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0414"></a>
### DTMAPI.Abstractions.CustomAnimalSpawnRequest.VariantId

- Symbol: `DTMAPI.Abstractions.CustomAnimalSpawnRequest.VariantId`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:501`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0415"></a>
### DTMAPI.Abstractions.CustomAnimalSpawnRequest.Position

- Symbol: `DTMAPI.Abstractions.CustomAnimalSpawnRequest.Position`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:502`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0416"></a>
### DTMAPI.Abstractions.CustomAnimalSpawnRequest.InitialLifeStageId

- Symbol: `DTMAPI.Abstractions.CustomAnimalSpawnRequest.InitialLifeStageId`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:503`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0417"></a>
### DTMAPI.Abstractions.CustomAnimalSpawnRequest.InitialState

- Symbol: `DTMAPI.Abstractions.CustomAnimalSpawnRequest.InitialState`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:504`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0418"></a>
### DTMAPI.Abstractions.CustomAnimalInstanceSnapshot

- Symbol: `DTMAPI.Abstractions.CustomAnimalInstanceSnapshot`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:507`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0419"></a>
### DTMAPI.Abstractions.CustomAnimalInstanceSnapshot.Handle

- Symbol: `DTMAPI.Abstractions.CustomAnimalInstanceSnapshot.Handle`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:509`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0420"></a>
### DTMAPI.Abstractions.CustomAnimalInstanceSnapshot.SpeciesId

- Symbol: `DTMAPI.Abstractions.CustomAnimalInstanceSnapshot.SpeciesId`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:510`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0421"></a>
### DTMAPI.Abstractions.CustomAnimalInstanceSnapshot.VariantId

- Symbol: `DTMAPI.Abstractions.CustomAnimalInstanceSnapshot.VariantId`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:511`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0422"></a>
### DTMAPI.Abstractions.CustomAnimalInstanceSnapshot.Position

- Symbol: `DTMAPI.Abstractions.CustomAnimalInstanceSnapshot.Position`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:512`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0423"></a>
### DTMAPI.Abstractions.CustomAnimalInstanceSnapshot.RuntimeStatus

- Symbol: `DTMAPI.Abstractions.CustomAnimalInstanceSnapshot.RuntimeStatus`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:513`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0424"></a>
### DTMAPI.Abstractions.CustomAnimalInstanceSnapshot.AgeDays

- Symbol: `DTMAPI.Abstractions.CustomAnimalInstanceSnapshot.AgeDays`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:514`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0425"></a>
### DTMAPI.Abstractions.CustomAnimalInstanceSnapshot.IsHungry

- Symbol: `DTMAPI.Abstractions.CustomAnimalInstanceSnapshot.IsHungry`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:515`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0426"></a>
### DTMAPI.Abstractions.CustomAnimalInstanceSnapshot.NeedsExcrementCleanup

- Symbol: `DTMAPI.Abstractions.CustomAnimalInstanceSnapshot.NeedsExcrementCleanup`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:516`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0427"></a>
### DTMAPI.Abstractions.CustomAnimalInstanceSnapshot.HiddenProductProgress

- Symbol: `DTMAPI.Abstractions.CustomAnimalInstanceSnapshot.HiddenProductProgress`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:517`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0428"></a>
### DTMAPI.Abstractions.CustomAnimalInstanceSnapshot.State

- Symbol: `DTMAPI.Abstractions.CustomAnimalInstanceSnapshot.State`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:518`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0429"></a>
### DTMAPI.Abstractions.CustomAnimalBehaviorContext

- Symbol: `DTMAPI.Abstractions.CustomAnimalBehaviorContext`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:521`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0430"></a>
### DTMAPI.Abstractions.CustomAnimalBehaviorContext.Entity

- Symbol: `DTMAPI.Abstractions.CustomAnimalBehaviorContext.Entity`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:523`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0431"></a>
### DTMAPI.Abstractions.CustomAnimalBehaviorContext.Snapshot

- Symbol: `DTMAPI.Abstractions.CustomAnimalBehaviorContext.Snapshot`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:524`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0432"></a>
### DTMAPI.Abstractions.CustomAnimalLifecycleEventArgs

- Symbol: `DTMAPI.Abstractions.CustomAnimalLifecycleEventArgs`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:527`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0433"></a>
### DTMAPI.Abstractions.CustomAnimalLifecycleEventArgs.Entity

- Symbol: `DTMAPI.Abstractions.CustomAnimalLifecycleEventArgs.Entity`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:529`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0434"></a>
### DTMAPI.Abstractions.CustomAnimalLifecycleEventArgs.Snapshot

- Symbol: `DTMAPI.Abstractions.CustomAnimalLifecycleEventArgs.Snapshot`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:530`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0435"></a>
### DTMAPI.Abstractions.CustomAnimalLifecycleEventArgs.ItemId

- Symbol: `DTMAPI.Abstractions.CustomAnimalLifecycleEventArgs.ItemId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:531`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0436"></a>
### DTMAPI.Abstractions.CustomAnimalLifecycleEventArgs.Amount

- Symbol: `DTMAPI.Abstractions.CustomAnimalLifecycleEventArgs.Amount`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:532`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0437"></a>
### DTMAPI.Abstractions.CustomAnimalRegistrationResult

- Symbol: `DTMAPI.Abstractions.CustomAnimalRegistrationResult`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:535`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0438"></a>
### DTMAPI.Abstractions.CustomAnimalSpawnResult

- Symbol: `DTMAPI.Abstractions.CustomAnimalSpawnResult`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:539`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0439"></a>
### DTMAPI.Abstractions.CustomAnimalSpawnResult.Snapshot

- Symbol: `DTMAPI.Abstractions.CustomAnimalSpawnResult.Snapshot`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:541`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is Animal/AnimalManager/AnimalViewer/save data adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0440"></a>
### DTMAPI.Abstractions.ICustomMonsterBehaviorProvider

- Symbol: `DTMAPI.Abstractions.ICustomMonsterBehaviorProvider`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:544`
- Implementation: No runtime implementation; public type is a data/contract surface or enum.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0441"></a>
### DTMAPI.Abstractions.ICustomMonsterBehaviorProvider.OnTick(CustomMonsterBehaviorContext context)

- Symbol: `DTMAPI.Abstractions.ICustomMonsterBehaviorProvider.OnTick(CustomMonsterBehaviorContext context)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:546`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0442"></a>
### DTMAPI.Abstractions.ICustomMonsterBehaviorProvider.SelectAttack(CustomMonsterBehaviorContext context, IReadOnlyList<string> availableAttackIds)

- Symbol: `DTMAPI.Abstractions.ICustomMonsterBehaviorProvider.SelectAttack(CustomMonsterBehaviorContext context, IReadOnlyList<string> availableAttackIds)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:547`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0443"></a>
### DTMAPI.Abstractions.ICustomMonsterBehaviorProvider.SelectMovement(CustomMonsterBehaviorContext context)

- Symbol: `DTMAPI.Abstractions.ICustomMonsterBehaviorProvider.SelectMovement(CustomMonsterBehaviorContext context)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:548`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0444"></a>
### DTMAPI.Abstractions.ICustomMonsterBehaviorProvider.OnTargetChanged(CustomMonsterLifecycleEventArgs args)

- Symbol: `DTMAPI.Abstractions.ICustomMonsterBehaviorProvider.OnTargetChanged(CustomMonsterLifecycleEventArgs args)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:549`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0445"></a>
### DTMAPI.Abstractions.ICustomMonsterBehaviorProvider.OnDamaged(CustomMonsterLifecycleEventArgs args)

- Symbol: `DTMAPI.Abstractions.ICustomMonsterBehaviorProvider.OnDamaged(CustomMonsterLifecycleEventArgs args)`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:550`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0446"></a>
### DTMAPI.Abstractions.ICustomMonsterBehaviorProvider.OnDeath(CustomMonsterLifecycleEventArgs args)

- Symbol: `DTMAPI.Abstractions.ICustomMonsterBehaviorProvider.OnDeath(CustomMonsterLifecycleEventArgs args)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:551`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0447"></a>
### DTMAPI.Abstractions.CustomMonsterDefinition

- Symbol: `DTMAPI.Abstractions.CustomMonsterDefinition`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:554`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0448"></a>
### DTMAPI.Abstractions.CustomMonsterDefinition.MonsterId

- Symbol: `DTMAPI.Abstractions.CustomMonsterDefinition.MonsterId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:556`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0449"></a>
### DTMAPI.Abstractions.CustomMonsterDefinition.VariantIds

- Symbol: `DTMAPI.Abstractions.CustomMonsterDefinition.VariantIds`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:557`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0450"></a>
### DTMAPI.Abstractions.CustomMonsterDefinition.DisplayName

- Symbol: `DTMAPI.Abstractions.CustomMonsterDefinition.DisplayName`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:558`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0451"></a>
### DTMAPI.Abstractions.CustomMonsterDefinition.Description

- Symbol: `DTMAPI.Abstractions.CustomMonsterDefinition.Description`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:559`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0452"></a>
### DTMAPI.Abstractions.CustomMonsterDefinition.SpawnRules

- Symbol: `DTMAPI.Abstractions.CustomMonsterDefinition.SpawnRules`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:560`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0453"></a>
### DTMAPI.Abstractions.CustomMonsterDefinition.MaxCountPerRoom

- Symbol: `DTMAPI.Abstractions.CustomMonsterDefinition.MaxCountPerRoom`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:561`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0454"></a>
### DTMAPI.Abstractions.CustomMonsterDefinition.RelationToPlayer

- Symbol: `DTMAPI.Abstractions.CustomMonsterDefinition.RelationToPlayer`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:562`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0455"></a>
### DTMAPI.Abstractions.CustomMonsterDefinition.FactionId

- Symbol: `DTMAPI.Abstractions.CustomMonsterDefinition.FactionId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:563`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0456"></a>
### DTMAPI.Abstractions.CustomMonsterDefinition.Stats

- Symbol: `DTMAPI.Abstractions.CustomMonsterDefinition.Stats`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:564`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0457"></a>
### DTMAPI.Abstractions.CustomMonsterDefinition.Targeting

- Symbol: `DTMAPI.Abstractions.CustomMonsterDefinition.Targeting`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:565`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0458"></a>
### DTMAPI.Abstractions.CustomMonsterDefinition.Movement

- Symbol: `DTMAPI.Abstractions.CustomMonsterDefinition.Movement`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:566`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0459"></a>
### DTMAPI.Abstractions.CustomMonsterDefinition.AttackSlots

- Symbol: `DTMAPI.Abstractions.CustomMonsterDefinition.AttackSlots`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:567`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0460"></a>
### DTMAPI.Abstractions.CustomMonsterDefinition.Loot

- Symbol: `DTMAPI.Abstractions.CustomMonsterDefinition.Loot`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:568`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0461"></a>
### DTMAPI.Abstractions.CustomMonsterDefinition.Icon

- Symbol: `DTMAPI.Abstractions.CustomMonsterDefinition.Icon`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:569`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0462"></a>
### DTMAPI.Abstractions.CustomMonsterDefinition.Sprite

- Symbol: `DTMAPI.Abstractions.CustomMonsterDefinition.Sprite`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:570`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0463"></a>
### DTMAPI.Abstractions.CustomMonsterDefinition.Audio

- Symbol: `DTMAPI.Abstractions.CustomMonsterDefinition.Audio`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:571`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0464"></a>
### DTMAPI.Abstractions.CustomMonsterDefinition.Persistence

- Symbol: `DTMAPI.Abstractions.CustomMonsterDefinition.Persistence`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:572`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0465"></a>
### DTMAPI.Abstractions.CustomMonsterDefinition.TickPolicy

- Symbol: `DTMAPI.Abstractions.CustomMonsterDefinition.TickPolicy`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:573`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0466"></a>
### DTMAPI.Abstractions.CustomMonsterDefinition.Provider

- Symbol: `DTMAPI.Abstractions.CustomMonsterDefinition.Provider`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:574`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0467"></a>
### DTMAPI.Abstractions.CustomMonsterSpawnRule

- Symbol: `DTMAPI.Abstractions.CustomMonsterSpawnRule`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:577`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0468"></a>
### DTMAPI.Abstractions.CustomMonsterSpawnRule.RuleId

- Symbol: `DTMAPI.Abstractions.CustomMonsterSpawnRule.RuleId`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:579`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0469"></a>
### DTMAPI.Abstractions.CustomMonsterSpawnRule.RoomIds

- Symbol: `DTMAPI.Abstractions.CustomMonsterSpawnRule.RoomIds`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:580`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0470"></a>
### DTMAPI.Abstractions.CustomMonsterSpawnRule.RoomTags

- Symbol: `DTMAPI.Abstractions.CustomMonsterSpawnRule.RoomTags`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:581`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0471"></a>
### DTMAPI.Abstractions.CustomMonsterSpawnRule.BiomeTags

- Symbol: `DTMAPI.Abstractions.CustomMonsterSpawnRule.BiomeTags`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:582`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0472"></a>
### DTMAPI.Abstractions.CustomMonsterSpawnRule.Seasons

- Symbol: `DTMAPI.Abstractions.CustomMonsterSpawnRule.Seasons`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:583`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0473"></a>
### DTMAPI.Abstractions.CustomMonsterSpawnRule.WeatherIds

- Symbol: `DTMAPI.Abstractions.CustomMonsterSpawnRule.WeatherIds`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:584`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0474"></a>
### DTMAPI.Abstractions.CustomMonsterSpawnRule.Probability

- Symbol: `DTMAPI.Abstractions.CustomMonsterSpawnRule.Probability`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:585`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0475"></a>
### DTMAPI.Abstractions.CustomMonsterSpawnRule.MinGroupSize

- Symbol: `DTMAPI.Abstractions.CustomMonsterSpawnRule.MinGroupSize`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:586`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0476"></a>
### DTMAPI.Abstractions.CustomMonsterSpawnRule.MaxGroupSize

- Symbol: `DTMAPI.Abstractions.CustomMonsterSpawnRule.MaxGroupSize`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:587`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0477"></a>
### DTMAPI.Abstractions.CustomMonsterSpawnRule.EarliestHour

- Symbol: `DTMAPI.Abstractions.CustomMonsterSpawnRule.EarliestHour`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:588`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0478"></a>
### DTMAPI.Abstractions.CustomMonsterSpawnRule.LatestHour

- Symbol: `DTMAPI.Abstractions.CustomMonsterSpawnRule.LatestHour`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:589`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0479"></a>
### DTMAPI.Abstractions.CustomMonsterSpawnTableDefinition

- Symbol: `DTMAPI.Abstractions.CustomMonsterSpawnTableDefinition`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:592`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0480"></a>
### DTMAPI.Abstractions.CustomMonsterSpawnTableDefinition.SpawnTableId

- Symbol: `DTMAPI.Abstractions.CustomMonsterSpawnTableDefinition.SpawnTableId`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:594`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0481"></a>
### DTMAPI.Abstractions.CustomMonsterSpawnTableDefinition.MonsterIds

- Symbol: `DTMAPI.Abstractions.CustomMonsterSpawnTableDefinition.MonsterIds`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:595`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0482"></a>
### DTMAPI.Abstractions.CustomMonsterSpawnTableDefinition.Rules

- Symbol: `DTMAPI.Abstractions.CustomMonsterSpawnTableDefinition.Rules`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:596`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0483"></a>
### DTMAPI.Abstractions.CustomMonsterStats

- Symbol: `DTMAPI.Abstractions.CustomMonsterStats`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:599`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0484"></a>
### DTMAPI.Abstractions.CustomMonsterStats.MaxHealth

- Symbol: `DTMAPI.Abstractions.CustomMonsterStats.MaxHealth`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:601`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0485"></a>
### DTMAPI.Abstractions.CustomMonsterStats.Armor

- Symbol: `DTMAPI.Abstractions.CustomMonsterStats.Armor`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:602`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0486"></a>
### DTMAPI.Abstractions.CustomMonsterStats.ContactDamage

- Symbol: `DTMAPI.Abstractions.CustomMonsterStats.ContactDamage`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:603`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0487"></a>
### DTMAPI.Abstractions.CustomMonsterStats.MoveSpeed

- Symbol: `DTMAPI.Abstractions.CustomMonsterStats.MoveSpeed`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:604`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0488"></a>
### DTMAPI.Abstractions.CustomMonsterStats.Resistances

- Symbol: `DTMAPI.Abstractions.CustomMonsterStats.Resistances`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:605`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0489"></a>
### DTMAPI.Abstractions.CustomMonsterStats.CustomValues

- Symbol: `DTMAPI.Abstractions.CustomMonsterStats.CustomValues`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:606`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0490"></a>
### DTMAPI.Abstractions.CustomMonsterTargetPolicy

- Symbol: `DTMAPI.Abstractions.CustomMonsterTargetPolicy`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:609`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0491"></a>
### DTMAPI.Abstractions.CustomMonsterTargetPolicy.TargetTags

- Symbol: `DTMAPI.Abstractions.CustomMonsterTargetPolicy.TargetTags`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:611`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0492"></a>
### DTMAPI.Abstractions.CustomMonsterTargetPolicy.AggroRange

- Symbol: `DTMAPI.Abstractions.CustomMonsterTargetPolicy.AggroRange`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:612`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0493"></a>
### DTMAPI.Abstractions.CustomMonsterTargetPolicy.RetargetWhenDamaged

- Symbol: `DTMAPI.Abstractions.CustomMonsterTargetPolicy.RetargetWhenDamaged`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:613`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0494"></a>
### DTMAPI.Abstractions.CustomMonsterTargetPolicy.ProviderCanOverride

- Symbol: `DTMAPI.Abstractions.CustomMonsterTargetPolicy.ProviderCanOverride`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:614`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0495"></a>
### DTMAPI.Abstractions.CustomMonsterMovementPolicy

- Symbol: `DTMAPI.Abstractions.CustomMonsterMovementPolicy`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:617`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0496"></a>
### DTMAPI.Abstractions.CustomMonsterMovementPolicy.Kind

- Symbol: `DTMAPI.Abstractions.CustomMonsterMovementPolicy.Kind`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:619`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0497"></a>
### DTMAPI.Abstractions.CustomMonsterMovementPolicy.PreferredDistance

- Symbol: `DTMAPI.Abstractions.CustomMonsterMovementPolicy.PreferredDistance`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:620`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0498"></a>
### DTMAPI.Abstractions.CustomMonsterMovementPolicy.PatrolRadius

- Symbol: `DTMAPI.Abstractions.CustomMonsterMovementPolicy.PatrolRadius`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:621`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0499"></a>
### DTMAPI.Abstractions.CustomMonsterMovementPolicy.ProviderCanOverride

- Symbol: `DTMAPI.Abstractions.CustomMonsterMovementPolicy.ProviderCanOverride`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:622`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0500"></a>
### DTMAPI.Abstractions.CustomMonsterAttackSlot

- Symbol: `DTMAPI.Abstractions.CustomMonsterAttackSlot`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:625`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0501"></a>
### DTMAPI.Abstractions.CustomMonsterAttackSlot.SlotId

- Symbol: `DTMAPI.Abstractions.CustomMonsterAttackSlot.SlotId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:627`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0502"></a>
### DTMAPI.Abstractions.CustomMonsterAttackSlot.AttackId

- Symbol: `DTMAPI.Abstractions.CustomMonsterAttackSlot.AttackId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:628`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0503"></a>
### DTMAPI.Abstractions.CustomMonsterAttackSlot.CooldownSeconds

- Symbol: `DTMAPI.Abstractions.CustomMonsterAttackSlot.CooldownSeconds`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:629`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0504"></a>
### DTMAPI.Abstractions.CustomMonsterAttackSlot.Range

- Symbol: `DTMAPI.Abstractions.CustomMonsterAttackSlot.Range`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:630`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0505"></a>
### DTMAPI.Abstractions.CustomMonsterAttackSlot.Priority

- Symbol: `DTMAPI.Abstractions.CustomMonsterAttackSlot.Priority`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:631`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0506"></a>
### DTMAPI.Abstractions.CustomMonsterLootRule

- Symbol: `DTMAPI.Abstractions.CustomMonsterLootRule`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:634`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0507"></a>
### DTMAPI.Abstractions.CustomMonsterLootRule.ItemId

- Symbol: `DTMAPI.Abstractions.CustomMonsterLootRule.ItemId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:636`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0508"></a>
### DTMAPI.Abstractions.CustomMonsterLootRule.MinStack

- Symbol: `DTMAPI.Abstractions.CustomMonsterLootRule.MinStack`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:637`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0509"></a>
### DTMAPI.Abstractions.CustomMonsterLootRule.MaxStack

- Symbol: `DTMAPI.Abstractions.CustomMonsterLootRule.MaxStack`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:638`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0510"></a>
### DTMAPI.Abstractions.CustomMonsterLootRule.Chance

- Symbol: `DTMAPI.Abstractions.CustomMonsterLootRule.Chance`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:639`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0511"></a>
### DTMAPI.Abstractions.CustomMonsterSpawnRequest

- Symbol: `DTMAPI.Abstractions.CustomMonsterSpawnRequest`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:642`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0512"></a>
### DTMAPI.Abstractions.CustomMonsterSpawnRequest.MonsterId

- Symbol: `DTMAPI.Abstractions.CustomMonsterSpawnRequest.MonsterId`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:644`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0513"></a>
### DTMAPI.Abstractions.CustomMonsterSpawnRequest.VariantId

- Symbol: `DTMAPI.Abstractions.CustomMonsterSpawnRequest.VariantId`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:645`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0514"></a>
### DTMAPI.Abstractions.CustomMonsterSpawnRequest.Position

- Symbol: `DTMAPI.Abstractions.CustomMonsterSpawnRequest.Position`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:646`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0515"></a>
### DTMAPI.Abstractions.CustomMonsterSpawnRequest.InitialState

- Symbol: `DTMAPI.Abstractions.CustomMonsterSpawnRequest.InitialState`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:647`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0516"></a>
### DTMAPI.Abstractions.CustomMonsterInstanceSnapshot

- Symbol: `DTMAPI.Abstractions.CustomMonsterInstanceSnapshot`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:650`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0517"></a>
### DTMAPI.Abstractions.CustomMonsterInstanceSnapshot.Handle

- Symbol: `DTMAPI.Abstractions.CustomMonsterInstanceSnapshot.Handle`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:652`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0518"></a>
### DTMAPI.Abstractions.CustomMonsterInstanceSnapshot.MonsterId

- Symbol: `DTMAPI.Abstractions.CustomMonsterInstanceSnapshot.MonsterId`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:653`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0519"></a>
### DTMAPI.Abstractions.CustomMonsterInstanceSnapshot.VariantId

- Symbol: `DTMAPI.Abstractions.CustomMonsterInstanceSnapshot.VariantId`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:654`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0520"></a>
### DTMAPI.Abstractions.CustomMonsterInstanceSnapshot.Position

- Symbol: `DTMAPI.Abstractions.CustomMonsterInstanceSnapshot.Position`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:655`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0521"></a>
### DTMAPI.Abstractions.CustomMonsterInstanceSnapshot.RuntimeStatus

- Symbol: `DTMAPI.Abstractions.CustomMonsterInstanceSnapshot.RuntimeStatus`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:656`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0522"></a>
### DTMAPI.Abstractions.CustomMonsterInstanceSnapshot.Health

- Symbol: `DTMAPI.Abstractions.CustomMonsterInstanceSnapshot.Health`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:657`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0523"></a>
### DTMAPI.Abstractions.CustomMonsterInstanceSnapshot.CurrentTargetId

- Symbol: `DTMAPI.Abstractions.CustomMonsterInstanceSnapshot.CurrentTargetId`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:658`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0524"></a>
### DTMAPI.Abstractions.CustomMonsterInstanceSnapshot.CurrentAttackId

- Symbol: `DTMAPI.Abstractions.CustomMonsterInstanceSnapshot.CurrentAttackId`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:659`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0525"></a>
### DTMAPI.Abstractions.CustomMonsterInstanceSnapshot.State

- Symbol: `DTMAPI.Abstractions.CustomMonsterInstanceSnapshot.State`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:660`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0526"></a>
### DTMAPI.Abstractions.CustomMonsterBehaviorContext

- Symbol: `DTMAPI.Abstractions.CustomMonsterBehaviorContext`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:663`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0527"></a>
### DTMAPI.Abstractions.CustomMonsterBehaviorContext.Entity

- Symbol: `DTMAPI.Abstractions.CustomMonsterBehaviorContext.Entity`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:665`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0528"></a>
### DTMAPI.Abstractions.CustomMonsterBehaviorContext.Snapshot

- Symbol: `DTMAPI.Abstractions.CustomMonsterBehaviorContext.Snapshot`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:666`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0529"></a>
### DTMAPI.Abstractions.CustomMonsterLifecycleEventArgs

- Symbol: `DTMAPI.Abstractions.CustomMonsterLifecycleEventArgs`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:669`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0530"></a>
### DTMAPI.Abstractions.CustomMonsterLifecycleEventArgs.Entity

- Symbol: `DTMAPI.Abstractions.CustomMonsterLifecycleEventArgs.Entity`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:671`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0531"></a>
### DTMAPI.Abstractions.CustomMonsterLifecycleEventArgs.Snapshot

- Symbol: `DTMAPI.Abstractions.CustomMonsterLifecycleEventArgs.Snapshot`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:672`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0532"></a>
### DTMAPI.Abstractions.CustomMonsterLifecycleEventArgs.TargetId

- Symbol: `DTMAPI.Abstractions.CustomMonsterLifecycleEventArgs.TargetId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:673`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0533"></a>
### DTMAPI.Abstractions.CustomMonsterLifecycleEventArgs.DamageAmount

- Symbol: `DTMAPI.Abstractions.CustomMonsterLifecycleEventArgs.DamageAmount`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:674`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0534"></a>
### DTMAPI.Abstractions.CustomMonsterLifecycleEventArgs.AttackId

- Symbol: `DTMAPI.Abstractions.CustomMonsterLifecycleEventArgs.AttackId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:675`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0535"></a>
### DTMAPI.Abstractions.CustomMonsterRegistrationResult

- Symbol: `DTMAPI.Abstractions.CustomMonsterRegistrationResult`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:678`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0536"></a>
### DTMAPI.Abstractions.CustomMonsterSpawnResult

- Symbol: `DTMAPI.Abstractions.CustomMonsterSpawnResult`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:682`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0537"></a>
### DTMAPI.Abstractions.CustomMonsterSpawnResult.Snapshot

- Symbol: `DTMAPI.Abstractions.CustomMonsterSpawnResult.Snapshot`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:684`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is MonsterController/MonsterGroupManager/AI/loot adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0538"></a>
### DTMAPI.Abstractions.ICustomAttackBehaviorProvider

- Symbol: `DTMAPI.Abstractions.ICustomAttackBehaviorProvider`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:687`
- Implementation: No runtime implementation; public type is a data/contract surface or enum.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0539"></a>
### DTMAPI.Abstractions.ICustomAttackBehaviorProvider.OnPatternTick(CustomAttackBehaviorContext context)

- Symbol: `DTMAPI.Abstractions.ICustomAttackBehaviorProvider.OnPatternTick(CustomAttackBehaviorContext context)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:689`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0540"></a>
### DTMAPI.Abstractions.ICustomAttackBehaviorProvider.OnCollision(CustomAttackLifecycleEventArgs args)

- Symbol: `DTMAPI.Abstractions.ICustomAttackBehaviorProvider.OnCollision(CustomAttackLifecycleEventArgs args)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:690`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0541"></a>
### DTMAPI.Abstractions.ICustomAttackBehaviorProvider.OnDamageApplied(CustomAttackLifecycleEventArgs args)

- Symbol: `DTMAPI.Abstractions.ICustomAttackBehaviorProvider.OnDamageApplied(CustomAttackLifecycleEventArgs args)`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:691`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0542"></a>
### DTMAPI.Abstractions.ICustomAttackBehaviorProvider.OnExpired(CustomAttackLifecycleEventArgs args)

- Symbol: `DTMAPI.Abstractions.ICustomAttackBehaviorProvider.OnExpired(CustomAttackLifecycleEventArgs args)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:692`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0543"></a>
### DTMAPI.Abstractions.CustomAttackDefinition

- Symbol: `DTMAPI.Abstractions.CustomAttackDefinition`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:695`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0544"></a>
### DTMAPI.Abstractions.CustomAttackDefinition.AttackId

- Symbol: `DTMAPI.Abstractions.CustomAttackDefinition.AttackId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:697`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0545"></a>
### DTMAPI.Abstractions.CustomAttackDefinition.FactionId

- Symbol: `DTMAPI.Abstractions.CustomAttackDefinition.FactionId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:698`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0546"></a>
### DTMAPI.Abstractions.CustomAttackDefinition.RelationToPlayer

- Symbol: `DTMAPI.Abstractions.CustomAttackDefinition.RelationToPlayer`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:699`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0547"></a>
### DTMAPI.Abstractions.CustomAttackDefinition.Damage

- Symbol: `DTMAPI.Abstractions.CustomAttackDefinition.Damage`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:700`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0548"></a>
### DTMAPI.Abstractions.CustomAttackDefinition.EffectTags

- Symbol: `DTMAPI.Abstractions.CustomAttackDefinition.EffectTags`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:701`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0549"></a>
### DTMAPI.Abstractions.CustomAttackDefinition.Hitbox

- Symbol: `DTMAPI.Abstractions.CustomAttackDefinition.Hitbox`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:702`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0550"></a>
### DTMAPI.Abstractions.CustomAttackDefinition.Trajectory

- Symbol: `DTMAPI.Abstractions.CustomAttackDefinition.Trajectory`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:703`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0551"></a>
### DTMAPI.Abstractions.CustomAttackDefinition.Pattern

- Symbol: `DTMAPI.Abstractions.CustomAttackDefinition.Pattern`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:704`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0552"></a>
### DTMAPI.Abstractions.CustomAttackDefinition.LifetimeSeconds

- Symbol: `DTMAPI.Abstractions.CustomAttackDefinition.LifetimeSeconds`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:705`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0553"></a>
### DTMAPI.Abstractions.CustomAttackDefinition.PierceCount

- Symbol: `DTMAPI.Abstractions.CustomAttackDefinition.PierceCount`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:706`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0554"></a>
### DTMAPI.Abstractions.CustomAttackDefinition.BounceCount

- Symbol: `DTMAPI.Abstractions.CustomAttackDefinition.BounceCount`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:707`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0555"></a>
### DTMAPI.Abstractions.CustomAttackDefinition.Homing

- Symbol: `DTMAPI.Abstractions.CustomAttackDefinition.Homing`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:708`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0556"></a>
### DTMAPI.Abstractions.CustomAttackDefinition.FriendlyFire

- Symbol: `DTMAPI.Abstractions.CustomAttackDefinition.FriendlyFire`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:709`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0557"></a>
### DTMAPI.Abstractions.CustomAttackDefinition.Visual

- Symbol: `DTMAPI.Abstractions.CustomAttackDefinition.Visual`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:710`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0558"></a>
### DTMAPI.Abstractions.CustomAttackDefinition.Audio

- Symbol: `DTMAPI.Abstractions.CustomAttackDefinition.Audio`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:711`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0559"></a>
### DTMAPI.Abstractions.CustomAttackDefinition.Persistence

- Symbol: `DTMAPI.Abstractions.CustomAttackDefinition.Persistence`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:712`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0560"></a>
### DTMAPI.Abstractions.CustomAttackDefinition.TickPolicy

- Symbol: `DTMAPI.Abstractions.CustomAttackDefinition.TickPolicy`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:713`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0561"></a>
### DTMAPI.Abstractions.CustomAttackDefinition.Provider

- Symbol: `DTMAPI.Abstractions.CustomAttackDefinition.Provider`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:714`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0562"></a>
### DTMAPI.Abstractions.CustomDamagePayload

- Symbol: `DTMAPI.Abstractions.CustomDamagePayload`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:717`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0563"></a>
### DTMAPI.Abstractions.CustomDamagePayload.Amount

- Symbol: `DTMAPI.Abstractions.CustomDamagePayload.Amount`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:719`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0564"></a>
### DTMAPI.Abstractions.CustomDamagePayload.DamageType

- Symbol: `DTMAPI.Abstractions.CustomDamagePayload.DamageType`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:720`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0565"></a>
### DTMAPI.Abstractions.CustomDamagePayload.Knockback

- Symbol: `DTMAPI.Abstractions.CustomDamagePayload.Knockback`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:721`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0566"></a>
### DTMAPI.Abstractions.CustomDamagePayload.Scaling

- Symbol: `DTMAPI.Abstractions.CustomDamagePayload.Scaling`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:722`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0567"></a>
### DTMAPI.Abstractions.CustomHitboxDefinition

- Symbol: `DTMAPI.Abstractions.CustomHitboxDefinition`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:725`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0568"></a>
### DTMAPI.Abstractions.CustomHitboxDefinition.Shape

- Symbol: `DTMAPI.Abstractions.CustomHitboxDefinition.Shape`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:727`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0569"></a>
### DTMAPI.Abstractions.CustomHitboxDefinition.Radius

- Symbol: `DTMAPI.Abstractions.CustomHitboxDefinition.Radius`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:728`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0570"></a>
### DTMAPI.Abstractions.CustomHitboxDefinition.Width

- Symbol: `DTMAPI.Abstractions.CustomHitboxDefinition.Width`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:729`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0571"></a>
### DTMAPI.Abstractions.CustomHitboxDefinition.Height

- Symbol: `DTMAPI.Abstractions.CustomHitboxDefinition.Height`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:730`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0572"></a>
### DTMAPI.Abstractions.CustomHitboxDefinition.ProviderCanOverride

- Symbol: `DTMAPI.Abstractions.CustomHitboxDefinition.ProviderCanOverride`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:731`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0573"></a>
### DTMAPI.Abstractions.CustomTrajectoryDefinition

- Symbol: `DTMAPI.Abstractions.CustomTrajectoryDefinition`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:734`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0574"></a>
### DTMAPI.Abstractions.CustomTrajectoryDefinition.Kind

- Symbol: `DTMAPI.Abstractions.CustomTrajectoryDefinition.Kind`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:736`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0575"></a>
### DTMAPI.Abstractions.CustomTrajectoryDefinition.Speed

- Symbol: `DTMAPI.Abstractions.CustomTrajectoryDefinition.Speed`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:737`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0576"></a>
### DTMAPI.Abstractions.CustomTrajectoryDefinition.Acceleration

- Symbol: `DTMAPI.Abstractions.CustomTrajectoryDefinition.Acceleration`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:738`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0577"></a>
### DTMAPI.Abstractions.CustomTrajectoryDefinition.TurnRateDegreesPerSecond

- Symbol: `DTMAPI.Abstractions.CustomTrajectoryDefinition.TurnRateDegreesPerSecond`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:739`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0578"></a>
### DTMAPI.Abstractions.CustomTrajectoryDefinition.ProviderCanOverride

- Symbol: `DTMAPI.Abstractions.CustomTrajectoryDefinition.ProviderCanOverride`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:740`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0579"></a>
### DTMAPI.Abstractions.CustomBarragePatternDefinition

- Symbol: `DTMAPI.Abstractions.CustomBarragePatternDefinition`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:743`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0580"></a>
### DTMAPI.Abstractions.CustomBarragePatternDefinition.Kind

- Symbol: `DTMAPI.Abstractions.CustomBarragePatternDefinition.Kind`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:745`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0581"></a>
### DTMAPI.Abstractions.CustomBarragePatternDefinition.ProjectileCount

- Symbol: `DTMAPI.Abstractions.CustomBarragePatternDefinition.ProjectileCount`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:746`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0582"></a>
### DTMAPI.Abstractions.CustomBarragePatternDefinition.ArcDegrees

- Symbol: `DTMAPI.Abstractions.CustomBarragePatternDefinition.ArcDegrees`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:747`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0583"></a>
### DTMAPI.Abstractions.CustomBarragePatternDefinition.IntervalSeconds

- Symbol: `DTMAPI.Abstractions.CustomBarragePatternDefinition.IntervalSeconds`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:748`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0584"></a>
### DTMAPI.Abstractions.CustomBarragePatternDefinition.RepeatCount

- Symbol: `DTMAPI.Abstractions.CustomBarragePatternDefinition.RepeatCount`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:749`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0585"></a>
### DTMAPI.Abstractions.CustomBarragePatternDefinition.DeterministicRandomSeed

- Symbol: `DTMAPI.Abstractions.CustomBarragePatternDefinition.DeterministicRandomSeed`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:750`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0586"></a>
### DTMAPI.Abstractions.CustomAttackSpawnRequest

- Symbol: `DTMAPI.Abstractions.CustomAttackSpawnRequest`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:753`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0587"></a>
### DTMAPI.Abstractions.CustomAttackSpawnRequest.AttackId

- Symbol: `DTMAPI.Abstractions.CustomAttackSpawnRequest.AttackId`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:755`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0588"></a>
### DTMAPI.Abstractions.CustomAttackSpawnRequest.Source

- Symbol: `DTMAPI.Abstractions.CustomAttackSpawnRequest.Source`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:756`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0589"></a>
### DTMAPI.Abstractions.CustomAttackSpawnRequest.Target

- Symbol: `DTMAPI.Abstractions.CustomAttackSpawnRequest.Target`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:757`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0590"></a>
### DTMAPI.Abstractions.CustomAttackSpawnRequest.Origin

- Symbol: `DTMAPI.Abstractions.CustomAttackSpawnRequest.Origin`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:758`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0591"></a>
### DTMAPI.Abstractions.CustomAttackSpawnRequest.Direction

- Symbol: `DTMAPI.Abstractions.CustomAttackSpawnRequest.Direction`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:759`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0592"></a>
### DTMAPI.Abstractions.CustomAttackSpawnRequest.InitialState

- Symbol: `DTMAPI.Abstractions.CustomAttackSpawnRequest.InitialState`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:760`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0593"></a>
### DTMAPI.Abstractions.CustomAttackInstanceSnapshot

- Symbol: `DTMAPI.Abstractions.CustomAttackInstanceSnapshot`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:763`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0594"></a>
### DTMAPI.Abstractions.CustomAttackInstanceSnapshot.Handle

- Symbol: `DTMAPI.Abstractions.CustomAttackInstanceSnapshot.Handle`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:765`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0595"></a>
### DTMAPI.Abstractions.CustomAttackInstanceSnapshot.AttackId

- Symbol: `DTMAPI.Abstractions.CustomAttackInstanceSnapshot.AttackId`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:766`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0596"></a>
### DTMAPI.Abstractions.CustomAttackInstanceSnapshot.Source

- Symbol: `DTMAPI.Abstractions.CustomAttackInstanceSnapshot.Source`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:767`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0597"></a>
### DTMAPI.Abstractions.CustomAttackInstanceSnapshot.Position

- Symbol: `DTMAPI.Abstractions.CustomAttackInstanceSnapshot.Position`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:768`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0598"></a>
### DTMAPI.Abstractions.CustomAttackInstanceSnapshot.RuntimeStatus

- Symbol: `DTMAPI.Abstractions.CustomAttackInstanceSnapshot.RuntimeStatus`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:769`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0599"></a>
### DTMAPI.Abstractions.CustomAttackInstanceSnapshot.AgeSeconds

- Symbol: `DTMAPI.Abstractions.CustomAttackInstanceSnapshot.AgeSeconds`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:770`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0600"></a>
### DTMAPI.Abstractions.CustomAttackInstanceSnapshot.HitCount

- Symbol: `DTMAPI.Abstractions.CustomAttackInstanceSnapshot.HitCount`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:771`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0601"></a>
### DTMAPI.Abstractions.CustomAttackInstanceSnapshot.State

- Symbol: `DTMAPI.Abstractions.CustomAttackInstanceSnapshot.State`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:772`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0602"></a>
### DTMAPI.Abstractions.CustomAttackBehaviorContext

- Symbol: `DTMAPI.Abstractions.CustomAttackBehaviorContext`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:775`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0603"></a>
### DTMAPI.Abstractions.CustomAttackBehaviorContext.Entity

- Symbol: `DTMAPI.Abstractions.CustomAttackBehaviorContext.Entity`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:777`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0604"></a>
### DTMAPI.Abstractions.CustomAttackBehaviorContext.Snapshot

- Symbol: `DTMAPI.Abstractions.CustomAttackBehaviorContext.Snapshot`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:778`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0605"></a>
### DTMAPI.Abstractions.CustomAttackLifecycleEventArgs

- Symbol: `DTMAPI.Abstractions.CustomAttackLifecycleEventArgs`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:781`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0606"></a>
### DTMAPI.Abstractions.CustomAttackLifecycleEventArgs.Entity

- Symbol: `DTMAPI.Abstractions.CustomAttackLifecycleEventArgs.Entity`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:783`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0607"></a>
### DTMAPI.Abstractions.CustomAttackLifecycleEventArgs.Snapshot

- Symbol: `DTMAPI.Abstractions.CustomAttackLifecycleEventArgs.Snapshot`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:784`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0608"></a>
### DTMAPI.Abstractions.CustomAttackLifecycleEventArgs.HitTarget

- Symbol: `DTMAPI.Abstractions.CustomAttackLifecycleEventArgs.HitTarget`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:785`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0609"></a>
### DTMAPI.Abstractions.CustomAttackLifecycleEventArgs.DamageAmount

- Symbol: `DTMAPI.Abstractions.CustomAttackLifecycleEventArgs.DamageAmount`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:786`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0610"></a>
### DTMAPI.Abstractions.CustomAttackRegistrationResult

- Symbol: `DTMAPI.Abstractions.CustomAttackRegistrationResult`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:789`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0611"></a>
### DTMAPI.Abstractions.CustomAttackSpawnResult

- Symbol: `DTMAPI.Abstractions.CustomAttackSpawnResult`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:793`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0612"></a>
### DTMAPI.Abstractions.CustomAttackSpawnResult.Snapshot

- Symbol: `DTMAPI.Abstractions.CustomAttackSpawnResult.Snapshot`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:795`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is BulletFactory/BulletManager/collision/damage adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0613"></a>
### DTMAPI.Abstractions.ICustomDroneBehaviorProvider

- Symbol: `DTMAPI.Abstractions.ICustomDroneBehaviorProvider`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:798`
- Implementation: No runtime implementation; public type is a data/contract surface or enum.
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0614"></a>
### DTMAPI.Abstractions.ICustomDroneBehaviorProvider.OnTick(CustomDroneBehaviorContext context)

- Symbol: `DTMAPI.Abstractions.ICustomDroneBehaviorProvider.OnTick(CustomDroneBehaviorContext context)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:800`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0615"></a>
### DTMAPI.Abstractions.ICustomDroneBehaviorProvider.SelectMode(CustomDroneBehaviorContext context)

- Symbol: `DTMAPI.Abstractions.ICustomDroneBehaviorProvider.SelectMode(CustomDroneBehaviorContext context)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:801`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0616"></a>
### DTMAPI.Abstractions.ICustomDroneBehaviorProvider.SelectMovement(CustomDroneBehaviorContext context)

- Symbol: `DTMAPI.Abstractions.ICustomDroneBehaviorProvider.SelectMovement(CustomDroneBehaviorContext context)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:802`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0617"></a>
### DTMAPI.Abstractions.ICustomDroneBehaviorProvider.SelectAttack(CustomDroneBehaviorContext context, IReadOnlyList<string> availableAttackIds)

- Symbol: `DTMAPI.Abstractions.ICustomDroneBehaviorProvider.SelectAttack(CustomDroneBehaviorContext context, IReadOnlyList<string> availableAttackIds)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:803`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0618"></a>
### DTMAPI.Abstractions.ICustomDroneBehaviorProvider.OnDamaged(CustomDroneLifecycleEventArgs args)

- Symbol: `DTMAPI.Abstractions.ICustomDroneBehaviorProvider.OnDamaged(CustomDroneLifecycleEventArgs args)`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:804`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0619"></a>
### DTMAPI.Abstractions.ICustomDroneBehaviorProvider.OnDestroyed(CustomDroneLifecycleEventArgs args)

- Symbol: `DTMAPI.Abstractions.ICustomDroneBehaviorProvider.OnDestroyed(CustomDroneLifecycleEventArgs args)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:805`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0620"></a>
### DTMAPI.Abstractions.ICustomDroneBehaviorProvider.OnRepaired(CustomDroneLifecycleEventArgs args)

- Symbol: `DTMAPI.Abstractions.ICustomDroneBehaviorProvider.OnRepaired(CustomDroneLifecycleEventArgs args)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:806`
- Implementation: `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9`
- Native owner: DTMAPI Core registry/status contract; no native runtime owner is attached unless a family adapter says verified.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: src/DTMAPI.Core/Services/CustomEntityRegistryService.cs:9
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0621"></a>
### DTMAPI.Abstractions.CustomDroneDefinition

- Symbol: `DTMAPI.Abstractions.CustomDroneDefinition`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:809`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0622"></a>
### DTMAPI.Abstractions.CustomDroneDefinition.DroneId

- Symbol: `DTMAPI.Abstractions.CustomDroneDefinition.DroneId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:811`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0623"></a>
### DTMAPI.Abstractions.CustomDroneDefinition.VariantIds

- Symbol: `DTMAPI.Abstractions.CustomDroneDefinition.VariantIds`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:812`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0624"></a>
### DTMAPI.Abstractions.CustomDroneDefinition.DisplayName

- Symbol: `DTMAPI.Abstractions.CustomDroneDefinition.DisplayName`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:813`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0625"></a>
### DTMAPI.Abstractions.CustomDroneDefinition.Description

- Symbol: `DTMAPI.Abstractions.CustomDroneDefinition.Description`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:814`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0626"></a>
### DTMAPI.Abstractions.CustomDroneDefinition.OwnerBinding

- Symbol: `DTMAPI.Abstractions.CustomDroneDefinition.OwnerBinding`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:815`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0627"></a>
### DTMAPI.Abstractions.CustomDroneDefinition.SupportedModes

- Symbol: `DTMAPI.Abstractions.CustomDroneDefinition.SupportedModes`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:816`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0628"></a>
### DTMAPI.Abstractions.CustomDroneDefinition.EquipmentSlots

- Symbol: `DTMAPI.Abstractions.CustomDroneDefinition.EquipmentSlots`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:817`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0629"></a>
### DTMAPI.Abstractions.CustomDroneDefinition.ModuleSlots

- Symbol: `DTMAPI.Abstractions.CustomDroneDefinition.ModuleSlots`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:818`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0630"></a>
### DTMAPI.Abstractions.CustomDroneDefinition.AttackIds

- Symbol: `DTMAPI.Abstractions.CustomDroneDefinition.AttackIds`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:819`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0631"></a>
### DTMAPI.Abstractions.CustomDroneDefinition.Stats

- Symbol: `DTMAPI.Abstractions.CustomDroneDefinition.Stats`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:820`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0632"></a>
### DTMAPI.Abstractions.CustomDroneDefinition.Energy

- Symbol: `DTMAPI.Abstractions.CustomDroneDefinition.Energy`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:821`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0633"></a>
### DTMAPI.Abstractions.CustomDroneDefinition.Movement

- Symbol: `DTMAPI.Abstractions.CustomDroneDefinition.Movement`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:822`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0634"></a>
### DTMAPI.Abstractions.CustomDroneDefinition.Repair

- Symbol: `DTMAPI.Abstractions.CustomDroneDefinition.Repair`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:823`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0635"></a>
### DTMAPI.Abstractions.CustomDroneDefinition.Summon

- Symbol: `DTMAPI.Abstractions.CustomDroneDefinition.Summon`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:824`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0636"></a>
### DTMAPI.Abstractions.CustomDroneDefinition.Icon

- Symbol: `DTMAPI.Abstractions.CustomDroneDefinition.Icon`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:825`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0637"></a>
### DTMAPI.Abstractions.CustomDroneDefinition.Sprite

- Symbol: `DTMAPI.Abstractions.CustomDroneDefinition.Sprite`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:826`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0638"></a>
### DTMAPI.Abstractions.CustomDroneDefinition.Audio

- Symbol: `DTMAPI.Abstractions.CustomDroneDefinition.Audio`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:827`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0639"></a>
### DTMAPI.Abstractions.CustomDroneDefinition.Persistence

- Symbol: `DTMAPI.Abstractions.CustomDroneDefinition.Persistence`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:828`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0640"></a>
### DTMAPI.Abstractions.CustomDroneDefinition.TickPolicy

- Symbol: `DTMAPI.Abstractions.CustomDroneDefinition.TickPolicy`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:829`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0641"></a>
### DTMAPI.Abstractions.CustomDroneDefinition.Provider

- Symbol: `DTMAPI.Abstractions.CustomDroneDefinition.Provider`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:830`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0642"></a>
### DTMAPI.Abstractions.CustomDroneOwnerBindingPolicy

- Symbol: `DTMAPI.Abstractions.CustomDroneOwnerBindingPolicy`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:833`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0643"></a>
### DTMAPI.Abstractions.CustomDroneOwnerBindingPolicy.BindToPlayer

- Symbol: `DTMAPI.Abstractions.CustomDroneOwnerBindingPolicy.BindToPlayer`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:835`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0644"></a>
### DTMAPI.Abstractions.CustomDroneOwnerBindingPolicy.BindToOwnerMod

- Symbol: `DTMAPI.Abstractions.CustomDroneOwnerBindingPolicy.BindToOwnerMod`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:836`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0645"></a>
### DTMAPI.Abstractions.CustomDroneOwnerBindingPolicy.PermissionTags

- Symbol: `DTMAPI.Abstractions.CustomDroneOwnerBindingPolicy.PermissionTags`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:837`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0646"></a>
### DTMAPI.Abstractions.CustomDroneEquipmentSlotDefinition

- Symbol: `DTMAPI.Abstractions.CustomDroneEquipmentSlotDefinition`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:840`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0647"></a>
### DTMAPI.Abstractions.CustomDroneEquipmentSlotDefinition.SlotId

- Symbol: `DTMAPI.Abstractions.CustomDroneEquipmentSlotDefinition.SlotId`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:842`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0648"></a>
### DTMAPI.Abstractions.CustomDroneEquipmentSlotDefinition.DisplayName

- Symbol: `DTMAPI.Abstractions.CustomDroneEquipmentSlotDefinition.DisplayName`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:843`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0649"></a>
### DTMAPI.Abstractions.CustomDroneEquipmentSlotDefinition.AllowedItemIds

- Symbol: `DTMAPI.Abstractions.CustomDroneEquipmentSlotDefinition.AllowedItemIds`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:844`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0650"></a>
### DTMAPI.Abstractions.CustomDroneEquipmentSlotDefinition.AllowedItemTags

- Symbol: `DTMAPI.Abstractions.CustomDroneEquipmentSlotDefinition.AllowedItemTags`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:845`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0651"></a>
### DTMAPI.Abstractions.CustomDroneEquipmentSlotDefinition.Required

- Symbol: `DTMAPI.Abstractions.CustomDroneEquipmentSlotDefinition.Required`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:846`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0652"></a>
### DTMAPI.Abstractions.CustomDroneStats

- Symbol: `DTMAPI.Abstractions.CustomDroneStats`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:849`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0653"></a>
### DTMAPI.Abstractions.CustomDroneStats.MaxHealth

- Symbol: `DTMAPI.Abstractions.CustomDroneStats.MaxHealth`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:851`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0654"></a>
### DTMAPI.Abstractions.CustomDroneStats.MaxShield

- Symbol: `DTMAPI.Abstractions.CustomDroneStats.MaxShield`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:852`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0655"></a>
### DTMAPI.Abstractions.CustomDroneStats.Armor

- Symbol: `DTMAPI.Abstractions.CustomDroneStats.Armor`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:853`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0656"></a>
### DTMAPI.Abstractions.CustomDroneStats.ContactDamage

- Symbol: `DTMAPI.Abstractions.CustomDroneStats.ContactDamage`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:854`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0657"></a>
### DTMAPI.Abstractions.CustomDroneStats.MoveSpeed

- Symbol: `DTMAPI.Abstractions.CustomDroneStats.MoveSpeed`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:855`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0658"></a>
### DTMAPI.Abstractions.CustomDroneStats.Resistances

- Symbol: `DTMAPI.Abstractions.CustomDroneStats.Resistances`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:856`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0659"></a>
### DTMAPI.Abstractions.CustomDroneEnergyPolicy

- Symbol: `DTMAPI.Abstractions.CustomDroneEnergyPolicy`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:859`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0660"></a>
### DTMAPI.Abstractions.CustomDroneEnergyPolicy.MaxEnergy

- Symbol: `DTMAPI.Abstractions.CustomDroneEnergyPolicy.MaxEnergy`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:861`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0661"></a>
### DTMAPI.Abstractions.CustomDroneEnergyPolicy.EnergyPerSecond

- Symbol: `DTMAPI.Abstractions.CustomDroneEnergyPolicy.EnergyPerSecond`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:862`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0662"></a>
### DTMAPI.Abstractions.CustomDroneEnergyPolicy.AttackEnergyCost

- Symbol: `DTMAPI.Abstractions.CustomDroneEnergyPolicy.AttackEnergyCost`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:863`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0663"></a>
### DTMAPI.Abstractions.CustomDroneEnergyPolicy.AcceptedFuelItemIds

- Symbol: `DTMAPI.Abstractions.CustomDroneEnergyPolicy.AcceptedFuelItemIds`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:864`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0664"></a>
### DTMAPI.Abstractions.CustomDroneMovementPolicy

- Symbol: `DTMAPI.Abstractions.CustomDroneMovementPolicy`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:867`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0665"></a>
### DTMAPI.Abstractions.CustomDroneMovementPolicy.Kind

- Symbol: `DTMAPI.Abstractions.CustomDroneMovementPolicy.Kind`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:869`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0666"></a>
### DTMAPI.Abstractions.CustomDroneMovementPolicy.FollowDistance

- Symbol: `DTMAPI.Abstractions.CustomDroneMovementPolicy.FollowDistance`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:870`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0667"></a>
### DTMAPI.Abstractions.CustomDroneMovementPolicy.PatrolRadius

- Symbol: `DTMAPI.Abstractions.CustomDroneMovementPolicy.PatrolRadius`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:871`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0668"></a>
### DTMAPI.Abstractions.CustomDroneMovementPolicy.Layer

- Symbol: `DTMAPI.Abstractions.CustomDroneMovementPolicy.Layer`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:872`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0669"></a>
### DTMAPI.Abstractions.CustomDroneMovementPolicy.CollidesWithWorld

- Symbol: `DTMAPI.Abstractions.CustomDroneMovementPolicy.CollidesWithWorld`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:873`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0670"></a>
### DTMAPI.Abstractions.CustomDroneMovementPolicy.ProviderCanOverride

- Symbol: `DTMAPI.Abstractions.CustomDroneMovementPolicy.ProviderCanOverride`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:874`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0671"></a>
### DTMAPI.Abstractions.CustomDroneRepairPolicy

- Symbol: `DTMAPI.Abstractions.CustomDroneRepairPolicy`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:877`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0672"></a>
### DTMAPI.Abstractions.CustomDroneRepairPolicy.CanRepair

- Symbol: `DTMAPI.Abstractions.CustomDroneRepairPolicy.CanRepair`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:879`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0673"></a>
### DTMAPI.Abstractions.CustomDroneRepairPolicy.RepairAmountPerItem

- Symbol: `DTMAPI.Abstractions.CustomDroneRepairPolicy.RepairAmountPerItem`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:880`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0674"></a>
### DTMAPI.Abstractions.CustomDroneRepairPolicy.RepairItemIds

- Symbol: `DTMAPI.Abstractions.CustomDroneRepairPolicy.RepairItemIds`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:881`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0675"></a>
### DTMAPI.Abstractions.CustomDroneSummonPolicy

- Symbol: `DTMAPI.Abstractions.CustomDroneSummonPolicy`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:884`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0676"></a>
### DTMAPI.Abstractions.CustomDroneSummonPolicy.CanSummonAnywhere

- Symbol: `DTMAPI.Abstractions.CustomDroneSummonPolicy.CanSummonAnywhere`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:886`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0677"></a>
### DTMAPI.Abstractions.CustomDroneSummonPolicy.CooldownSeconds

- Symbol: `DTMAPI.Abstractions.CustomDroneSummonPolicy.CooldownSeconds`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:887`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0678"></a>
### DTMAPI.Abstractions.CustomDroneSummonPolicy.MaxActiveInstances

- Symbol: `DTMAPI.Abstractions.CustomDroneSummonPolicy.MaxActiveInstances`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:888`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0679"></a>
### DTMAPI.Abstractions.CustomDroneSummonRequest

- Symbol: `DTMAPI.Abstractions.CustomDroneSummonRequest`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:891`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0680"></a>
### DTMAPI.Abstractions.CustomDroneSummonRequest.DroneId

- Symbol: `DTMAPI.Abstractions.CustomDroneSummonRequest.DroneId`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:893`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0681"></a>
### DTMAPI.Abstractions.CustomDroneSummonRequest.VariantId

- Symbol: `DTMAPI.Abstractions.CustomDroneSummonRequest.VariantId`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:894`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0682"></a>
### DTMAPI.Abstractions.CustomDroneSummonRequest.Position

- Symbol: `DTMAPI.Abstractions.CustomDroneSummonRequest.Position`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:895`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0683"></a>
### DTMAPI.Abstractions.CustomDroneSummonRequest.InitialState

- Symbol: `DTMAPI.Abstractions.CustomDroneSummonRequest.InitialState`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:896`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0684"></a>
### DTMAPI.Abstractions.CustomDroneEquipmentRequest

- Symbol: `DTMAPI.Abstractions.CustomDroneEquipmentRequest`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:899`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0685"></a>
### DTMAPI.Abstractions.CustomDroneEquipmentRequest.SlotId

- Symbol: `DTMAPI.Abstractions.CustomDroneEquipmentRequest.SlotId`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:901`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0686"></a>
### DTMAPI.Abstractions.CustomDroneEquipmentRequest.ItemId

- Symbol: `DTMAPI.Abstractions.CustomDroneEquipmentRequest.ItemId`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:902`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0687"></a>
### DTMAPI.Abstractions.CustomDroneEquipmentRequest.Stack

- Symbol: `DTMAPI.Abstractions.CustomDroneEquipmentRequest.Stack`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:903`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0688"></a>
### DTMAPI.Abstractions.CustomDroneEquipmentRequest.Unequip

- Symbol: `DTMAPI.Abstractions.CustomDroneEquipmentRequest.Unequip`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:904`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0689"></a>
### DTMAPI.Abstractions.CustomDroneCommandRequest

- Symbol: `DTMAPI.Abstractions.CustomDroneCommandRequest`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:907`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0690"></a>
### DTMAPI.Abstractions.CustomDroneCommandRequest.Mode

- Symbol: `DTMAPI.Abstractions.CustomDroneCommandRequest.Mode`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:909`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0691"></a>
### DTMAPI.Abstractions.CustomDroneCommandRequest.Target

- Symbol: `DTMAPI.Abstractions.CustomDroneCommandRequest.Target`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:910`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0692"></a>
### DTMAPI.Abstractions.CustomDroneCommandRequest.Destination

- Symbol: `DTMAPI.Abstractions.CustomDroneCommandRequest.Destination`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:911`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0693"></a>
### DTMAPI.Abstractions.CustomDroneCommandRequest.Reason

- Symbol: `DTMAPI.Abstractions.CustomDroneCommandRequest.Reason`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:912`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0694"></a>
### DTMAPI.Abstractions.CustomDroneInstanceSnapshot

- Symbol: `DTMAPI.Abstractions.CustomDroneInstanceSnapshot`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:915`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0695"></a>
### DTMAPI.Abstractions.CustomDroneInstanceSnapshot.Handle

- Symbol: `DTMAPI.Abstractions.CustomDroneInstanceSnapshot.Handle`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:917`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0696"></a>
### DTMAPI.Abstractions.CustomDroneInstanceSnapshot.DroneId

- Symbol: `DTMAPI.Abstractions.CustomDroneInstanceSnapshot.DroneId`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:918`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0697"></a>
### DTMAPI.Abstractions.CustomDroneInstanceSnapshot.VariantId

- Symbol: `DTMAPI.Abstractions.CustomDroneInstanceSnapshot.VariantId`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:919`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0698"></a>
### DTMAPI.Abstractions.CustomDroneInstanceSnapshot.Position

- Symbol: `DTMAPI.Abstractions.CustomDroneInstanceSnapshot.Position`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:920`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0699"></a>
### DTMAPI.Abstractions.CustomDroneInstanceSnapshot.RuntimeStatus

- Symbol: `DTMAPI.Abstractions.CustomDroneInstanceSnapshot.RuntimeStatus`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:921`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0700"></a>
### DTMAPI.Abstractions.CustomDroneInstanceSnapshot.Mode

- Symbol: `DTMAPI.Abstractions.CustomDroneInstanceSnapshot.Mode`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:922`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0701"></a>
### DTMAPI.Abstractions.CustomDroneInstanceSnapshot.Health

- Symbol: `DTMAPI.Abstractions.CustomDroneInstanceSnapshot.Health`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:923`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0702"></a>
### DTMAPI.Abstractions.CustomDroneInstanceSnapshot.Shield

- Symbol: `DTMAPI.Abstractions.CustomDroneInstanceSnapshot.Shield`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:924`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0703"></a>
### DTMAPI.Abstractions.CustomDroneInstanceSnapshot.Energy

- Symbol: `DTMAPI.Abstractions.CustomDroneInstanceSnapshot.Energy`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:925`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0704"></a>
### DTMAPI.Abstractions.CustomDroneInstanceSnapshot.Equipment

- Symbol: `DTMAPI.Abstractions.CustomDroneInstanceSnapshot.Equipment`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:926`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0705"></a>
### DTMAPI.Abstractions.CustomDroneInstanceSnapshot.State

- Symbol: `DTMAPI.Abstractions.CustomDroneInstanceSnapshot.State`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:927`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0706"></a>
### DTMAPI.Abstractions.CustomDroneBehaviorContext

- Symbol: `DTMAPI.Abstractions.CustomDroneBehaviorContext`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:930`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0707"></a>
### DTMAPI.Abstractions.CustomDroneBehaviorContext.Entity

- Symbol: `DTMAPI.Abstractions.CustomDroneBehaviorContext.Entity`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:932`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0708"></a>
### DTMAPI.Abstractions.CustomDroneBehaviorContext.Snapshot

- Symbol: `DTMAPI.Abstractions.CustomDroneBehaviorContext.Snapshot`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:933`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0709"></a>
### DTMAPI.Abstractions.CustomDroneLifecycleEventArgs

- Symbol: `DTMAPI.Abstractions.CustomDroneLifecycleEventArgs`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:936`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0710"></a>
### DTMAPI.Abstractions.CustomDroneLifecycleEventArgs.Entity

- Symbol: `DTMAPI.Abstractions.CustomDroneLifecycleEventArgs.Entity`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:938`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0711"></a>
### DTMAPI.Abstractions.CustomDroneLifecycleEventArgs.Snapshot

- Symbol: `DTMAPI.Abstractions.CustomDroneLifecycleEventArgs.Snapshot`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:939`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0712"></a>
### DTMAPI.Abstractions.CustomDroneLifecycleEventArgs.DamageAmount

- Symbol: `DTMAPI.Abstractions.CustomDroneLifecycleEventArgs.DamageAmount`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:940`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0713"></a>
### DTMAPI.Abstractions.CustomDroneLifecycleEventArgs.RepairAmount

- Symbol: `DTMAPI.Abstractions.CustomDroneLifecycleEventArgs.RepairAmount`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:941`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0714"></a>
### DTMAPI.Abstractions.CustomDroneLifecycleEventArgs.EquipmentSlotId

- Symbol: `DTMAPI.Abstractions.CustomDroneLifecycleEventArgs.EquipmentSlotId`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:942`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0715"></a>
### DTMAPI.Abstractions.CustomDroneRegistrationResult

- Symbol: `DTMAPI.Abstractions.CustomDroneRegistrationResult`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:945`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0716"></a>
### DTMAPI.Abstractions.CustomDroneSummonResult

- Symbol: `DTMAPI.Abstractions.CustomDroneSummonResult`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:949`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0717"></a>
### DTMAPI.Abstractions.CustomDroneSummonResult.Snapshot

- Symbol: `DTMAPI.Abstractions.CustomDroneSummonResult.Snapshot`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:951`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0718"></a>
### DTMAPI.Abstractions.CustomDroneEquipmentResult

- Symbol: `DTMAPI.Abstractions.CustomDroneEquipmentResult`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 禁止依赖
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:954`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0719"></a>
### DTMAPI.Abstractions.CustomDroneEquipmentResult.SlotId

- Symbol: `DTMAPI.Abstractions.CustomDroneEquipmentResult.SlotId`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:956`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0720"></a>
### DTMAPI.Abstractions.CustomDroneEquipmentResult.ItemId

- Symbol: `DTMAPI.Abstractions.CustomDroneEquipmentResult.ItemId`
- Current marker: `not-in-matrix`
- Review advice: 拆成 stable contract + experimental runtime
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:957`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Blocked
- Recommendation: 普通 mod 依赖会把 DTMAPI registry/status 误认为 native entity runtime；风险是只注册成功但未接 native owner，spawn/summon/execute/equip 返回 runtime-creation-blocked，导致作者以为对象已出现在游戏世界。

<a id="sym-0721"></a>
### DTMAPI.Abstractions.CustomDroneCommandResult

- Symbol: `DTMAPI.Abstractions.CustomDroneCommandResult`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:960`
- Implementation: No runtime implementation; DTO/contract consumed by src/DTMAPI.Core/Services/CustomEntityRegistryService.cs and GameBridge status paths.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0722"></a>
### DTMAPI.Abstractions.CustomDroneCommandResult.Mode

- Symbol: `DTMAPI.Abstractions.CustomDroneCommandResult.Mode`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/CustomEntities.cs:962`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Core registry now; future native owner is DroneController/weapon/equipment/save adapters.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-191219
  - hook-map entry: docs/hook-map/README.md#hook-customentitiescoreregistry
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.
