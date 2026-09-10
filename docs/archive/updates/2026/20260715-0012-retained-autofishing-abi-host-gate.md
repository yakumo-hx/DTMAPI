# 20260715-0012 Retained AutoFishing ABI Host Gate

## Metadata

- Update ID: `20260715-0012`
- Date: 2026-07-15
- Lifecycle Status: `verified`
- Validation Level: `docs,source,unit,runtime`
- Runtime Validation: `passed`
- Related Issue State: `open`
- Area: api/abi/autofishing/retained-artifact/dotnet-host/lamp/release-gate
- Source: user required the Batch 2 retained AutoFishing DLL/ABI gate after `docs/reviews/code/2026/20260715-0005-major-update-progress-and-decision-node-review.md`; compatibility owner: `docs/reviews/api/2026/20260712-0002-workshop-055-binary-compatibility-review.md`; continuing Batch 2 lifecycle owner: `docs/updates/2026/20260714-0004-batch2-version-release-authority.md`

## Scope

- build a tracked, small .NET 8 console harness without copying any retained DLL into the repository;
- compare the complete public/protected surface of the exact retained Runtime `0.5.2.0` Abstractions DLL with current Release Abstractions while excluding assembly version from member identity;
- verify the retained AutoFishing artifact hash and metadata reference to `FishingAutomationOptions.set_StopOnManualMove`;
- directly load the retained AutoFishing DLL in a collectible isolated `AssemblyLoadContext`, resolve every type, bind current candidate Abstractions and resolve the old setter token to the candidate member;
- verify the restored property has a public getter/setter, historical default `true`, a working round trip and `Obsolete(..., false)`;
- optionally inventory all eleven Catalog-owned retained public product DLLs for MemberRefs to the newly exposed Lamp deletion family;
- implement the user's selected `L-A1/L-B1/L-C1/L-D1/L-E1` boundary: restore the exact four Lamp types and 92 signatures with historical defaults, publish a non-null owner-bound facade from the original provider, and make every call a stateless deterministic `retired-disabled` result;
- keep the compatibility provider outside `IGameBridgeFeature`, Hook installation, native light mutation, persistence, session overrides, frame/lifecycle dispatch and file polling;
- keep the wrapper opt-in so the default Release suite builds the harness but never depends on a mutable local Workshop tree.

This slice does not copy or modify a retained DLL, write the game or Workshop directory, approve any public API deletion, restore Lamp gameplay behavior, run Unity Mono, prove movement cancellation, publish a package, or by itself clear the remaining Batch 2 runtime gates.

## Newly Exposed Retained-Binary Drift

- Retained Runtime Abstractions is `DTMAPI.Abstractions.dll`, SHA-256 `39A51683034FF0B7495BEB3DD1C50F76F591D4E24C63872B6502EA360EF8880B`, AssemblyVersion/FileVersion `0.5.2.0`.
- Retained AutoFishing is `Yuuka.DTMAPI.AutoFishing.dll`, SHA-256 `E573F8CA1989663B672AF481921E4C6131C061294402F654A4062844DC5CA7FA`, and references Abstractions `0.5.1.0`.
- The candidate has AssemblyVersion `0.5.3.0` and FileVersion `0.5.5.0`; the strict comparer ignores assembly version in API identity.
- Before the L-A1 restoration, the exact retained DLL contained four public types absent from the candidate: `DTMAPI.Abstractions.ILampControlApi`, `LampManualToggleOptions`, `LampManualToggleRegisterResult`, and `LampManualToggleState`. They accounted for all 92 remaining deleted signature records after `StopOnManualMove` restoration.
- The retained DLL's informational commit `8caf8403b45cf601c3c9791e778f2aeb859a77dc` does not contain those four types in tracked Abstractions source. Physical published bytes remain the compatibility input despite that build/source drift.
- All eleven retained first-party public product DLLs were scanned by Catalog identity and exact package DLL name. Their total MemberRef count whose parent is one of the four Lamp types is zero. Non-consumption narrows risk but does not authorize deletion.
- The earlier “exactly one deletion” review statement was incomplete for the exact physical retained artifact and is corrected in the compatibility Review and API matrix.

## Lamp Compatibility Decision And Boundary

- `L-A1`: candidate Abstractions restores all four retained types, all 92 signature records, and the reflected historical defaults: options `Enabled=true`, empty equipment IDs and `VerboseLogging=false`; all result/state strings and arrays empty, booleans false and counts zero.
- `L-B1`: `DTMAPI.GameBridge.DolocTown` registers a non-null raw provider plus a stable consumer-scoped facade. The facade binds mutation to the canonical consumer manifest and rejects cross-owner string queries.
- `L-C1`: valid calls return `retired-disabled`; registration returns `Success=false`, `Enabled=false`, `HookInstalled=false`, and no registered equipment. The provider is stateless, is not an `IGameBridgeFeature`, and has no Hook/native/persistence/session/frame/lifecycle/file-polling route.
- `L-D1`: the interface and all three DTOs use `Obsolete(..., false)` plus `DtmApiStatus.Disabled`. Author SDK/Doctor warning and migration scans remain later work.
- `L-E1`: no old Lamp behavior is restored. A future real Lamp feature requires a separate native-owner/API Review and implementation project.
- Physical deletion is not a 0.5.5 decision. It may be reconsidered no earlier than 0.6.0 after a published warning cycle and renewed ecosystem/author-supplied scans; discovery of a real consumer pauses retirement.

## Changed Files

- `tests/DTMAPI.AbiCompatibilityHarness/DTMAPI.AbiCompatibilityHarness.csproj` and `Program.cs`
  - implement deterministic public surface signatures, hash/version checks, PE metadata MemberRef inspection, isolated candidate binding, all-type/signature resolution, setter token resolution and restored-property assertions;
  - optionally scan the exact eleven retained public product DLLs selected by the wrapper;
  - validate all four Lamp type-level `Obsolete(false)` and `DtmApiStatus.Disabled` attributes plus every historical DTO default;
  - emit a complete machine-readable JSON report and fail the overall gate when any deletion remains;
  - state `UnityMonoRuntimeValidation=False` explicitly in success and failure output.
- `tools/scripts/test-retained-autofishing-abi.ps1`
  - requires explicit retained baseline, candidate and AutoFishing paths;
  - pins the retained Runtime Abstractions and AutoFishing hashes plus expected `0.5.2.0` baseline, `0.5.1.0` consumer reference and `0.5.3.0` candidate identities;
  - optionally resolves the exact eleven public product DLLs from the tracked Catalog and an explicitly supplied Workshop root;
  - supports spaces and non-ASCII paths through literal-path resolution and argument arrays.
- `tools/scripts/build.ps1`
  - builds the net8 harness on the default tracked build path without executing it or requiring private/local artifacts.
- `tools/release/baselines/retained-runtime-052-public-api-audit-20260715.json`
  - records exact hashes, versions, API counts, zero remaining deletions, all eleven product DLL hashes, zero Lamp MemberRefs and the four-type/default/attribute result without storing local absolute paths or binaries.
- `src/DTMAPI.Abstractions/RetiredLampControl.cs`
  - restores the exact retained public shape and historical DTO defaults while marking every type Disabled and obsolete-with-warning.
- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
  - restores `FishingAutomationOptions.StopOnManualMove` with its historical `true` default and warning-only obsolete contract.
- `src/DTMAPI.GameBridge.DolocTown/Compatibility/Lamp/RetiredLampControlApi.cs`
  - implements the stateless deterministic `retired-disabled` raw provider without implementing the feature lifecycle contract.
- `src/DTMAPI.GameBridge.DolocTown/Compatibility/FishingAutomation/LegacyFishingAutomationService.cs`
  - preserves the restored option when configuring the compatibility service and keeps the owner-once retirement warning.
- `src/DTMAPI.GameBridge.DolocTown/OwnerBoundGameBridgeApis.cs` and `DolocTownGameBridge.cs`
  - register the historical provider identity and return a stable owner-bound facade without adding Lamp to feature dispatch.
- `tests/DTMAPI.UnitTests/Program.cs`
  - verifies non-null raw/facade lookup, canonical ownership, anti-spoofing, stable retired results, zero GameBridge/lifecycle roots and no Lamp Hook status.
- `docs/reviews/api/2026/20260712-0002-workshop-055-binary-compatibility-review.md` and `docs/api/public-api-matrix.md`
  - record the selected compatibility-shell boundary, zero-deletion host result, Disabled status and remaining Unity Mono gate.
- `docs/reviews/code/2026/20260715-0006-batch2-closure-and-batch3-predecision-review.md`
  - adds only the allowed short decision-resolution link to this implementation owner.
- `docs/updates/INDEX-2026-07.md`
  - routes this in-progress Update from the monthly ledger.

## Validation

- `tools/scripts/build.ps1 -Configuration Release -SkipTests` used the repository-local .NET 8 resolver and built every tracked project, including the opt-in harness, with zero warnings and zero errors.
- The post-decision opt-in PowerShell 7 run verified the exact hashes/versions, one metadata setter reference, one resolved candidate setter, all eight AutoFishing types, and all restored-property assertions.
- The same run scanned all eleven retained public product DLLs and found zero Lamp MemberRefs.
- PowerShell 7 and Windows PowerShell 5.1 both parsed the wrapper and tracked build entrypoint without errors. Before the decision, Windows PowerShell 5.1 reproduced the intentional 92-deletion blocker. After L-A1 through L-E1, it executed the full host gate again using a report path containing spaces and Chinese characters and passed with the same zero-deletion/default/attribute/product results as PowerShell 7.
- The post-decision strict PowerShell 7 gate passed with candidate SHA-256 `45F10BF4049F0E7F85B70E328D7D52AADB5B463FFE10D44BDF2E52B54EC8579F`, `CandidatePublicApiCount=5251`, `RemovedPublicApiCount=0`, four Lamp types, four `Obsolete(false)` declarations, four Disabled status attributes and all historical defaults.
- The tracked schema-2 JSON contains the same zero-deletion result and eleven product hash/MemberRef rows as that live report.
- `tools/scripts/build.ps1 -Configuration Release -SkipTests` passed after the Lamp implementation with zero warnings and zero errors, including Abstractions, GameBridge, ABI harness and UnitTests compilation.
- Focused Release execution completed with `DTMAPI.UnitTests: OK`; the final strict ABI gate then passed again under PowerShell 7 and Windows PowerShell 5.1 with the same candidate hash, 5,251 public API records, zero removals, four Lamp types and eleven zero-Lamp-MemberRef public products.
- `GAME-SMOKE/20260715-131518` loaded the exact retained AutoFishing subscription DLL bytes, SHA-256 `E573F8CA1989663B672AF481921E4C6131C061294402F654A4062844DC5CA7FA`, without recompilation through an isolated Workshop canary. Under Unity Mono its old Entry ran, its `StopOnManualMove` setter resolved and was invoked, and external F6 enabled/configured the legacy compatibility service. Profile and `mod_infos.json` restoration plus clean process exit passed; selected-save restoration was not part of this canary.
- This runtime result proves retained-binary binding, Entry and the restored option/configuration path. It does not prove movement-triggered cancellation, a speed ladder, GC stability or Lamp gameplay behavior.
- `tools/scripts/check-doc-governance.ps1` passed all 4,971 checks, and `git diff --check` passed (line-ending notices only).

## Rollback

Revert the Lamp registration/facade/provider and restored declaration file together with its Unit/ABI assertions, machine-readable audit and focused API-matrix/Review resolution. If reverting the fishing compatibility slice, revert `ExperimentalGameBridge.cs` and `LegacyFishingAutomationService.cs` together with their ABI assertions; never remove only the declaration or only its value-preserving implementation. Never roll back only the Lamp provider or only the Lamp declarations: that would leave either an avoidable null provider or a missing binary shape. A deliberate future removal requires the no-earlier-than-0.6.0 warning/scan/breaking-version process, not this rollback.

## Follow-Up

- Keep movement-triggered cancellation, disable/title cycling and the independent AutoFishing speed/GC ladder as later behavior gates; the scoped retained-binary Unity Mono binding/Entry/configuration gate is complete, but does not imply those results.
- Add Author SDK/Doctor deprecated-contract scanning and migration guidance in its own Batch 3 slice without turning Doctor into a mutation authority.
- If real Lamp functionality is requested, open a separate native-owner/API Review; do not expand this compatibility shell or reuse historical hooks by convenience.
- Keep the later AutoFishing and ActionSpeed GC ladders separate; this host run is not GC evidence.
