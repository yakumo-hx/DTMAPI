# 20260708-0005 - AutoFishing Ledger Lifecycle Boundary

Status: source-and-runtime-verified / diagnostics-pressure-reduced / issue-010-open

## Source Request

User requested the AutoFishing boundary split and Lifecycle Ledger pressure plan: reduce `ResourceLifecycleLedger` pressure first, retire misleading GameBridge movement-cancel semantics, lazily activate FishingAutomation, clarify AutoFishing as an optional first-party product mod, and add SMAPI-style input helper convenience methods.

## Changed Files

- `src/DTMAPI.Core/Runtime/ResourceLifecycleLedgerService.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.Abstractions/Helpers.cs`
- `src/DTMAPI.Abstractions/Input.cs`
- `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.Features.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.Hooks.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationService.cs`
- `first-party-mods/AutoFishingMod/AutoFishingMod.csproj`
- `first-party-mods/AutoFishingMod/ModEntry.cs`
- `first-party-mods/AutoFishingMod/README.md`
- `first-party-mods/AutoFishingMod/manifest.json`
- `first-party-mods/AutoFishingMod/official-info.json`
- `first-party-mods/AutoFishingMod/i18n/english.json`
- `first-party-mods/AutoFishingMod/i18n/schinese.json`
- `testmods/ZoomMod/ModEntry.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `tools/scripts/build.ps1`
- `tools/scripts/install-to-game.ps1`
- `tools/scripts/release-common.ps1`
- `tools/scripts/build-release-workshop-packages.ps1`
- `tools/scripts/run-game-smoke.ps1`
- `tools/scripts/README.md`
- `tools/release/dtmapi-mod-publish-zh.json`
- `tools/release/dtmapi-mod-publish-zh - 副本.md`
- `docs/api/public-api-matrix.md`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260708-0005-autofishing-ledger-lifecycle-boundary.md`

## Summary

Reduced diagnostics lifecycle pressure without changing fishing gameplay semantics.

- Ledger snapshots now expose internal/report counters for record count, current-save released records, snapshot builds, publish counts by area, skipped refresh calls, repeated skipped-refresh fast-path count, and AutoFishing record count.
- Repeated skipped refreshes with unchanged phase/area/result/resource count/content generation now return a no-publish/no-snapshot update.
- AutoFishing borrowed native `SaveLifetime` handles aggregate by high-churn kind/phase, preserving observed/released counts plus first/last sample ids instead of building one full ledger record per transient handle.
- High-churn release publication is sampled; failures, warnings, cleanup, and generation-boundary updates still publish immediately.
- `FishingAutomationFeature` registers a lightweight facade at startup and creates service/runtime state only after `Configure` or `SetEnabled(true)`.
- Fishing status vocabulary is now `inactive/no-consumer`, `configured`, `active`, and `disabled/consumer-present`.
- `FishingAutomationOptions.StopOnManualMove` was retired from the experimental GameBridge DTO. AutoFishingMod keeps F6, movement cancel, prompts, defaults, and config menu product text.
- Input helpers gained typed snapshot methods. AutoFishing now uses local `DtmKeybindList` snapshot checks for toggle/cancel and caches manual cancel buttons instead of parsing them every frame.
- ZoomMod was migrated as a small ordinary-mod template for SMAPI-style local keybind consumption.
- AutoFishing moved physically from `testmods/AutoFishingMod` to `first-party-mods/AutoFishingMod`; build/install/release scripts now include first-party product mods.
- AutoFishing docs and metadata now describe it as an optional first-party sample/product mod and GameBridge API consumer, not DTMAPI Core behavior.
- AutoFishing product config now forces no-charge casting (`CastChargeRatio=0`). The experimental GameBridge API still has `CastChargeRatio`, but the first-party product no longer exposes or smoke-requires player-facing charge.
- The smoke harness no longer treats generic `WaitingAction` as native fishing wait evidence. The retained failed FastAnimations smoke `GAME-SMOKE/20260708-144151` caught this false-success class; the fixed rerun `GAME-SMOKE/20260708-144556` proved native Wait/Bite/MiniGame/NextAutoCast without `FastReadyCharge`.

## Validation

Source checks:

- `$env:DOTNET_ROLL_FORWARD='Major'; tools/scripts/test.ps1 -Configuration Release` passed with `DTMAPI.UnitTests: OK`. Roll-forward was required because this machine has .NET 6 and 9 installed but no .NET 8 runtime.
- `git diff --check` passed.

Runtime checks:

- Inactive/no-consumer baseline `GAME-SMOKE/20260708-132338` passed with AutoFishing disabled. `Fishing.Automation` reported `inactive/no-consumer`, lifecycle hook install signals did not require `Fishing.Automation`, and final resource lifecycle summary had `autoFishingRecords=0`.
- Fifth-save `DefaultLoop` passed in `GAME-SMOKE/20260708-143733`.
- Fifth-save `InstantBite` passed in `GAME-SMOKE/20260708-143905`.
- Fifth-save `SkipMiniGame` passed in `GAME-SMOKE/20260708-144029`.
- Fifth-save `FastAnimations` first failed in `GAME-SMOKE/20260708-144151` because the smoke gate could accept no-real-cast evidence and the Ready charge helper still ticked `_castTimer`; this was fixed by the no-charge Ready guard and stricter native Wait evidence.
- Fifth-save `FastAnimations` passed after the fix in `GAME-SMOKE/20260708-144556`, with real AutoCast -> Wait -> BiteReady -> Battle/Pull -> PullExit -> NextAutoCast flow, `FastCastHookPhysics`, Pull evidence, `readyCharges=0->0`, and no `FastReadyCharge`.
- Longer fifth-save AutoFishing soak passed in `GAME-SMOKE/20260708-144724`: `soakLoops=8/8`, `autoCast=0->10`, `miniGameComplete=0->9`, final fishing transients cleared, and final resource ledger stayed bounded at `recordCount=6`, `autoFishingRecords=4`, `snapshotBuilds=51`, `skippedRefreshCalls=380`, `skippedRefreshFastPath=374`.
- Product-level `AutoFishingCastCharge` is intentionally skipped after the no-charge product change. Positive `CastChargeRatio` remains covered as experimental GameBridge behavior by unit tests and historical `GAME-SMOKE/20260613-171405`, not by current first-party AutoFishing product smoke.

## Evidence Links

- API matrix: `docs/api/public-api-matrix.md`
- ISSUE-010 ledger note: `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- Regression matrix row: `AUTOFISHING-LEDGER-LIFECYCLE-PRESSURE-20260708`
- Inactive/no-consumer runtime evidence: `docs/debug/evidence/GAME-SMOKE/20260708-132338`
- Fifth-save AutoFishing runtime evidence: `GAME-SMOKE/20260708-143733`, `20260708-143905`, `20260708-144029`, `20260708-144151` retained failed false-success/Ready-charge finding, `20260708-144556`, and `20260708-144724`
- Prior lifecycle attribution: `docs/updates/2026/20260703-0006-autofishing-lifecycle-attribution-audit.md`
- Prior ResourceLifecycle generation ledger: `docs/updates/2026/20260702-0003-resource-lifecycle-generations.md`
- Input edge follow-up baseline: `docs/updates/2026/20260707-0007-hotkey-edge-followup.md`

## Rollback

Revert this update if the aggregation hides required failure evidence or if lazy activation breaks first-consumer FishingAutomation startup. Restore per-handle ledger records for AutoFishing native handles only as a diagnostic fallback, not as default runtime behavior.

## Follow-Up

- Run a fresh slot-3 typed input smoke after the physical first-party move for YConsole, Zoom, AutoFishing F6, and movement cancel if this branch needs independent input-route closure beyond the earlier `GAME-SMOKE/20260708-021606` edge-follow-up proof.
- Keep the positive `CastChargeRatio` path experimental GameBridge-only until there is a reviewed product reason to expose player-facing charge again.
- Keep ISSUE-010 open until active gameplay and long AutoFishing GC evidence improves; this update is diagnostics lifecycle pressure reduction, not a solved GC claim.
