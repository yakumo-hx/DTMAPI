# Doloc Town 1.00.00 / DTMAPI 0.6.0 Compatibility Root-Cause Review

Status: `recorded`

Date: 2026-08-01

Scope: pre-implementation audit of the current local game, the DTMAPI 0.5.5 player log, the 0.5.5 Advanced-product policy set, and first-party ProductNative/Compatibility native targets. This Review establishes the bounded 0.6.0 compatibility work; it does not claim that implementation, packaging, or game acceptance has started.

## Source Request And Release Decision

The user reported that the newest locally unpacked/decompiled game differs substantially from the game targeted by DTMAPI 0.5.5, that almost every Mod reports an error, and directed that the next DTMAPI version is `0.6.0` with formal-version compatibility as its main purpose.

This is an audit-only turn. No Runtime, Mod source, manifest, policy, receipt, Catalog row, package, installed game file, or save was changed. No game was launched. The existing 0.5.5 player run and the accepted reverse baselines are read-only evidence.

The exact local target is the accepted **1.00.00 final-test** baseline, not yet a captured public-branch release:

| Boundary | DTMAPI 0.5.5 reference | Current local target |
| --- | --- | --- |
| Reverse baseline | `23762374_public_C416D4` | `24456188_test_E861E0` |
| Steam branch | `public` | `test` |
| Player version | pre-1.00 baseline | `1.00.00` |
| `Assembly-CSharp.dll` length | `5,993,984` | `6,384,128` |
| SHA-256 | `C416D461C2559DDE8FB34D6B279BA84330E1403D18AB2D32A0224C6760D06404` | `E861E07E3CB82A6A21EEFA292456452F5AD12C25EC57972A59762AD3F3530923` |

The current baseline README explicitly records `BetaKey=test`. Therefore 0.6.0 may use build `24456188` as the present implementation target, but it must not call that identity public-release proof. If the public Steam 1.00.00 bytes differ, capture them as another exact baseline and generate another exact policy family before release.

## Required Native-Owner Safety Boundary

先做本轮 API/domain 的 native owner 方法体审查；未找到 native owner 或状态持有者前，不得通过 mod 层补丁冒充 API 重做完成。

No new public API is justified by this compatibility pass. The changed DebugConsole and AutoFishing behavior remains ProductNative. The retained MoreEquipmentSlots executor remains a bounded frozen-ABI Compatibility Host responsibility; its failure does not authorize a general equipment/storage API redesign.

## What The Player Log Actually Proves

The 2026-08-01 22:28:35–22:29:24 local run used Runtime `0.5.5` on the current `6,384,128`-byte game assembly.

The log contains no `Error` or `Fatal` record. Its 41 warning records consist of:

- 36 Advanced-reference mismatches: the same 18 paths were diagnosed once at startup and once after the official Mod-page refresh;
- four duplicate-source warnings: MoreEquipmentSlots and Manbo each had an enabled Workshop source and a disabled OfficialLocal source;
- one real native compatibility refusal from MoreEquipmentSlots.

The first 18 discovery failures are not 18 independently crashing Mods. They are nine unique Advanced products, each present in both OfficialLocal and Workshop form:

- ChestLocatorEnhancer;
- MoreSaves;
- DebugConsole / Y Key Console;
- Zoom;
- ActionSpeed;
- AnimalHusbandryProgress;
- AutoFishing;
- FishBreedingAssistant;
- OneActionComplete.

Every one was rejected before Mod Entry with the same fail-closed reason:

`advanced-reference-hash-mismatch: reference Assembly-CSharp length mismatch; expected=5993984; actual=6384128`

This is the dominant reason that “almost every Mod” currently appears broken. It is a DTMAPI policy/package compatibility stop, not evidence that all nine Mod implementations threw after loading.

Runtime itself reached native-ready startup, initialized the reflected title settings UI, installed the exact four native layout hooks, installed save/load/title/Workshop observation targets at signature level, completed a real ReturnHome/title lifecycle, and completed the official Mod-page preview/candidate/native-save/deferred-commit transaction. Save/load hooks were not exercised by a save load in this run. The final GameBridge title snapshot reported zero feature failures.

Two legacy native compatibility products passed discovery:

- `Yuuka.DTMAPI.ManboCardboardAudio` registered its stable DTMAPI audio replacement, loaded the WAV backend, and caused the native Wwise/paper-box hook route to report installed. This is positive load/hook evidence, not yet a paper-box audible-behavior acceptance.
- the retained public MoreEquipmentSlots `0.3.1-dtmapi` DLL loaded and called the frozen `IEquipmentSlotsApi`, but the Compatibility Host returned `success=False` because its exact native target set was unavailable. This is an independent true compatibility break.

## Root Cause 1: Every Advanced Policy Is Bound To Build 23762374

All 13 rows in `author-sdk/advanced-reference-policies/registry.json` currently belong to the `doloctown-23762374-...-v1` family. The product policies embed the old assembly hash and length, product `dtmapi.author.json` files bind those policy IDs, and the Catalog, SDK, Core/Loader validation, Doctor fixtures, package gates, and focused tests repeat those exact authorities.

The current rejection is intentional and correct: accepting an arbitrary changed `Assembly-CSharp.dll`, or weakening the exact length/hash check, would silently allow native code compiled/reviewed for another game body. The 0.6.0 fix is not to bypass the guard.

The Platform/release work is:

1. Add a distinct build-24456188 policy family while retaining the historical 23762374 policies; do not overwrite their identities.
2. Generate the policy/reference surfaces and registry entries through the tracked SDK/policy workflow.
3. Rebind each admitted 0.6.0 product source and Catalog row to its exact new policy only after its native-owner review closes.
4. Update Core/Loader embedded policy validation, Install Doctor/Player Doctor, Manager/package checks, Catalog assertions, focused product gates, and release contracts that own the live policy set.
5. Rebuild and reissue product packages and receipts through the Author SDK. Do not hand-author an Advanced manifest value, receipt, or package.
6. Set the rebuilt packages' minimum DTMAPI version to the final 0.6.0 decision and verify that old packages remain diagnosed rather than misclassified.

This Platform work is required for all nine currently published Advanced products even when their ProductNative source needs no semantic edit.

## Root Cause 2: Confirmed Native Contract Changes

### DebugConsole — ProductNative source changes required (`P1`)

The Advanced reference guard currently prevents Entry. After that guard is updated, the current source still has several deterministic failures.

#### Atomic Hook inventory

`DebugConsoleHookInstaller` pre-resolves all 19 targets and currently asks for `DolocAPI.CostItemAt` with exactly two parameters. The native signature changed:

- old: `CostItemAt(int position, int count)`;
- new: `CostItemAt(int position, int count, bool useBox = false, bool shouldEqualAsItem = false)`.

The other 18 DebugConsole hook targets remain present with their tracked parameter counts. Because installation is atomic, this single missing two-parameter target rejects the entire 19-patch product. The source must resolve the exact current four-parameter owner and keep exact-type/owner collision and rollback tests. A name-only or first-overload lookup is not sufficient.

#### Weather/state owner

The formal weather implementation is no longer one global current-weather value. It is keyed by season/weather group:

| 0.5.5-era source assumption | 1.00.00 native owner |
| --- | --- |
| `ArchiveDataHandle.CurrentWeatherType` | removed; local room projection is `LocalWeatherType` |
| `TimeArchiveData.SeasonProto` | removed; use `GetSeasonInfo(seasonGroupId)` / `TryGetSeasonInfo` |
| `GetWeatherInfoOfDay(int)` | `GetWeatherInfoOfDay(string seasonGroupId, int dayOffset)` |
| `SetWeather(WeatherType, bool)` | `SetWeather(string seasonGroupId, WeatherType, bool)` |
| `PatchWeather(WeatherType)` | `PatchWeather(string seasonGroupId, WeatherType, Vector2Int weatherKey)` |

The current DebugConsole reads the removed properties and resolves the old method shapes. Its state/forecast display becomes empty and `SetWeather` returns `missing-setweather`. The source must bind the current room's native season group (`ArchiveDataHandle.currentSeasonGroupId` or the reviewed `RoomInfo.SeasonGroupId` owner), use `LocalWeatherType`, and supply the exact group to forecast/set operations.

For “patch current period”, the native owner uses `DateNow.CurrentWeatherKey`. The implementation must derive that exact native key. It must not invent a `Vector2Int`; if the owner/key cannot be proven, the patch-current-period action must remain disabled/fail-closed.

#### Monster generation

The official private command changed from `Command_GenerateMonster(string, int)` to `Command_GenerateMonster(string, int, string targetRoom = null)`. The current exact two-parameter reflection lookup returns null. The product must resolve the exact three-parameter method and deliberately pass the reviewed current/target-room argument.

Inventory placement, `SaveGame(int)`, five-parameter `DoTransport`, `PassTimeNoControl`, `SetTimeScale(float,bool)`, money, tech-point, and resource-replacement shapes used by the product remain present. `AgentControllerState.EnterUICheck`, `UseTool`, and `BodyController.MoveSpeed` bodies changed internally, so the input/creative/movement behavior matrix must be replayed even though those exact hook signatures remain available.

### AutoFishing — ProductNative source and behavior review required (`P1`)

All 21 exact native methods behind AutoFishing's 22 patches remain present with the tracked parameter counts. This means a build-24456188 package is expected to pass static hook pre-resolution. It does not prove the state machine remains behaviorally correct.

One direct reflection dependency was removed:

- old state holder: `AgentPhysicalStatus.HorizontalMoveFactor`;
- new state holder: `AgentPhysicalStatus.MoveModifier.inputMultiplier`, with `SetHorizontalMoveFactorByInput` and `ClearHorizontalMoveFactor`; velocity now also includes `MoveModifier` multipliers/offsets.

`FishingNativeStateCache` still builds only a `HorizontalMoveFactor` getter. On 1.00.00 it marks the accessor unavailable and falls back to the legacy key snapshot. That fallback is explicitly weaker and invalidates the existing fifth-save/manual-movement cancellation acceptance.

The source must read the current native movement state first and retain the old property only as an exact older-build compatibility fallback if that dual-build lane is deliberately supported. The QA/performance contracts and messages must stop treating `HorizontalMoveFactor` as the sole native owner.

The fishing owner bodies also changed materially:

- `AgentStateFishingWait.NextState/OnPlay` now consult `MoveModifier.inputMultiplier` and `VelocityX`;
- Wait timers use `AgentEquipmentParams.HookingTimeMultiplier`;
- Ready/Wait/Pull add current base-state calls and/or `CurrentTool` lifecycle work;
- `FishRodRenderer.Play(string toolName, string behaviourName)` became `Play(string behaviourName)`, although the present product does not reflect or invoke that method.

The existing forced bite/reel, ready-charge, pending-cast, energy, animation, input override, and restoration paths require method-body review against those new native invariants, followed by the authoritative fifth-save acceptance. Static presence of all 21 targets is not enough.

### MoreEquipmentSlots — Compatibility Host change required (`P1`); new ProductNative remains deferred

The live failure maps exactly to this signature change:

- old: `BodyController.OnAttacked(float, bool, Vector2, out bool)`;
- new: `BodyController.OnAttacked(float, bool, Vector2, AttackProperties, out bool)`.

The retained public `0.3.1-dtmapi` Mod DLL only calls the frozen DTMAPI API and does not itself need to be rebuilt to explain this failure. The DTMAPI optional Compatibility Host is the broken physical native owner. The unreleased/deferred MoreEquipmentSlots 1.0 ProductNative source has the same old target assumption and would also fail if admitted unchanged.

This is not a safe parameter-count-only repair. The current native owner additionally:

- rejects attacks while `_attackableType` is `Unattackable`;
- carries `AttackProperties`, including special Thunder damage-tip behavior;
- calls `droneController.EscapeCombat()` on death;
- disables attackability after a surviving hit and restores it after `InvincibilityDuration`;
- retains native shield priority before the ordinary health tail.

Both current DTMAPI implementations manually reproduce part of the post-shield attack tail. Simply adding an ignored fifth argument would bypass new 1.00.00 invariants. The Compatibility Host must be redesigned around the reviewed current owner/seam so extra-slot shield handling preserves `AttackProperties`, death/drone behavior, hit invincibility, fishing interruption, hitback, and native shield priority. The protected-item save/no-save/mail/recovery matrix remains separately mandatory.

Publishing MoreEquipmentSlots 1.0 is still deferred by the current API/Catalog authority. DTMAPI 0.6.0 formal compatibility requires the retained 0.3.1 Host path unless a separate bounded authority explicitly replaces that public product.

## Published Product Classification

| Public product | Current 1.00.00 finding | 0.6.0 action |
| --- | --- | --- |
| DebugConsole / Y Key Console | policy-rejected; one hard Hook signature break plus weather and monster reflection breaks | ProductNative source changes, new exact policy/package, focused actions/input/save tests |
| AutoFishing | policy-rejected; all Hook signatures present, but removed movement state and changed fishing bodies | ProductNative source/body review, new exact policy/package, fifth-save acceptance |
| MoreEquipmentSlots 0.3.1 | DLL loads; frozen API Host refuses exact Hook set | change Compatibility Host; retain old DLL ABI; do not smuggle deferred 1.0 publication into 0.6.0 |
| Zoom | policy-rejected; `SetEnvCamera` and `RefreshResolution` signatures and reviewed bodies remain compatible | no source edit currently indicated; rebuild and re-run camera/native-refresh visual matrix |
| MoreSaves | policy-rejected; no Harmony targets; `DolocAPI.gameManager` and native `archiveFileCount = 6` remain | no source edit currently indicated; rebuild and re-run save UI/count/title restoration |
| ActionSpeed | policy-rejected; all nine targets remain; native Tool exit now clears `CurrentTool` | no source edit currently indicated; rebuild and re-run tool/interact/eat/continuous-use restoration |
| OneActionComplete | policy-rejected; both targets and reviewed bodies remain | no source edit currently indicated; rebuild and re-run resource/fuel/feed transactions |
| FishBreedingAssistant | policy-rejected; `Item.get_title` target/body remains | no source edit currently indicated; rebuild and re-run localized roe-title/cleanup route |
| AnimalHusbandryProgress | policy-rejected; all three targets/bodies and reflected animal/progress members remain | no source edit currently indicated; rebuild and re-run viewer/render/close cleanup |
| ChestLocatorEnhancer | policy-rejected; exact inventory-enumeration signature remains and decompiled method text is unchanged | no source edit currently indicated; rebuild and re-run native inventory transaction |
| ManboCardboardAudio | loads and registers; shared native audio hook installs | no product source edit indicated; audible replacement/fallback/cleanup smoke still required |

“No source edit currently indicated” means targeted reverse comparison found no incompatible symbol/body dependency. It is not a runtime pass. Those Advanced products did not load in the observed run and still require SDK-generated 0.6.0 packages plus focused gameplay evidence.

## Planned/Local Products Outside The Initial Public Compatibility Set

- StrongPlantingGun's five exact targets remain present and its relevant FarmingGun source text is unchanged. If it is included later, it needs a build-24456188 policy/package and focused acceptance, but this audit does not promote the planned local product.
- Mine's three target signatures remain, but `EquipmentBuilder.CreateIndicator` changed internally. It needs a method-body re-review before any new package. Mine remains planned/local and outside the initial public 0.6.0 compatibility set unless its existing authority changes.
- The current log contains no DTMAPI error attributable to official JSON/content-only products. The new baseline changes 44/195 config tables and 19/133 Yarn files, so official-content validation remains a release test concern, not a confirmed first-party Mod-code break from this evidence.

## Bounded 0.6.0 Work Order

1. Freeze `24456188_test_E861E0` as the current implementation identity and create the 0.6.0 compatibility Update. Capture a public baseline later if its bytes differ.
2. Add the new exact policy family and all existing Platform/SDK/Loader/Doctor/Catalog validation without weakening 23762374 history or opening general Advanced authoring.
3. Correct DebugConsole's exact Hook, grouped-weather owner, forecast/patch key, and monster-generation route.
4. Correct AutoFishing's movement-state owner and re-review the changed Ready/Wait/Pull bodies.
5. Redesign the retained MoreEquipmentSlots Compatibility Host attack seam. Keep MoreEquipmentSlots 1.0 deferred unless separately authorized.
6. SDK-build/package the nine public Advanced products against the new policy after each focused source gate passes; do not hand-edit receipts.
7. Run focused product and package gates first. Then use the smallest relevant game acceptances: third save by default, fifth save for AutoFishing, and disposable save fixtures for intentional native-save or recovery tests.
8. At the final integration boundary, verify startup has zero Advanced-reference mismatch for the selected 0.6.0 packages, all nine Advanced Entries load, the retained MoreEquipmentSlots Host registers, Manbo retains native/fallback behavior, core title/Workshop/save boundaries remain clean, and no `DolocTown.exe` remains.

## Acceptance Blockers

0.6.0 formal-version compatibility remains incomplete if any of the following is true:

- a package is admitted by disabling or widening the assembly identity guard;
- any new policy/manifest/receipt/package is hand-authored outside the tracked SDK/policy workflow;
- DebugConsole merely installs while weather state, current-period patching, or monster generation still uses old signatures;
- AutoFishing passes target lookup but manual movement cancellation still uses only the legacy key fallback;
- MoreEquipmentSlots accepts the fifth parameter while skipping the new native attack/death/invincibility semantics;
- title-only logs are cited as save/gameplay acceptance;
- the public Steam 1.00.00 assembly differs from `24456188_test_E861E0` but no exact public policy/baseline is created;
- game-loaded DTMAPI or Mod assemblies are retargeted away from `netstandard2.0` without a separate toolchain review.

## Evidence And Related Authorities

- Current baseline: `references/doloc-town/reverse/builds/24456188_test_E861E0/README.md`
- 0.5.5 baseline: `references/doloc-town/reverse/builds/23762374_public_C416D4/README.md`
- Current baseline acceptance: [20260801-0002 current game vs 24256979 audit](20260801-0002-current-game-vs-24256979-reverse-baseline-audit.md)
- Earlier 23762374 API baseline: [20260617-0003 reverse baseline audit](../../api/2026/20260617-0003-reverse-baseline-23762374-audit.md)
- Advanced ownership/identity authority: [Batch 6 managed Mod identity contract](../../../architecture/batch6-managed-mod-identity-contract.md)
- Public API dispositions: [public API matrix](../../../api/public-api-matrix.md)
- Hook authorities: [Hook map index](../../../hook-map/README.md)
- Runtime regression authority: [smoke matrix](../../../debug/regressions/smoke-matrix.md)

Local player-log evidence is deliberately described rather than copied into the repository. It remains machine-local and records Runtime 0.5.5, the 18 first-scan mismatch paths, the MoreEquipmentSlots refusal, the Manbo load/hook route, and the title/Workshop lifecycle summarized above.

## Audit Validation

- Reverse-source and product-source inspection: completed read-only.
- Player/BepInEx log classification: completed read-only; no new game run.
- Build, Unit, package, Doctor, game smoke, save matrix, and Release suite: not run because this Review makes no implementation change.
- Implementation owner: pending a 0.6.0 Update; this Review must remain `recorded`, not `verified`.
