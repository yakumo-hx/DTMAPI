# 20260613-0021 AutoFishing InstantBite Energy Cost

Date: 2026-06-13
Status: implemented
Area: gamebridge/autofishing/energy

## Source

User feedback reported that AutoFishing stamina/energy consumption changed from the expected native 10 to 20. The request was to fix this small AutoFishing issue before returning to the larger equipment-slot API rebuild.

## Root Cause

`AgentStateFishingWait.OnEnter` runs inside the native `Cast -> Wait` transition. The native `AgentStateBase.Update` calls the target state's `OnEnter()` and then sets the current state to that target after `OnEnter` returns. AutoFishing's `InstantBite` path was allowed to force a bite and advance to Battle/Pull from the `Wait.OnEnter` postfix, but that overwrite could be clobbered by the native transition assigning current back to Wait. The later `Wait.OnPlay` postfix then advanced the same bite again, causing the mirrored native reel path to consume fishing energy twice.

## Changes

- `FishingAutomationService.ApplyFishingWaitAutomation` now allows `Wait.OnEnter` to prepare the bite state only.
- Actual reel/state advancement is deferred to `AgentStateFishingWait.OnPlay`, after the native state transition has completed.
- Added unit coverage for the regression: InstantBite `Wait.OnEnter` does not overwrite state or call `CostEnergy`; the following `Wait.OnPlay` advances to Battle and consumes native fishing energy exactly once.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationService.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/updates/INDEX.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`

## Validation

- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings / 0 errors and `DTMAPI.UnitTests: OK`.
- `git diff --check` passed with line-ending warnings only.
- `tools/scripts/install-to-game.ps1 -Configuration Release -SkipBuild` installed the updated Runtime/GameBridge and local official AutoFishing package.
- `tools/scripts/check-dtmapi-status.ps1` passed required install checks; installed DTMAPI version is `0.5.1-alpha`. Legacy DLK/Workshop detections remain detected-only warnings.
- No `DolocTown.exe` process was found before install verification.

## Evidence

- New unit: `FishingAutomationInstantBiteDefersReelUntilWaitPlay`.
- The fix is code/unit and local-install validated in this update. A fifth-save manual or smoke pass should still verify the visible in-game energy delta remains the native single `FishingEnergyCost`.

## Rollback

Revert the `Wait.OnEnter` deferral block and remove the new unit test if it blocks InstantBite from advancing. That rollback would re-open the double-cost risk on the native `Cast -> Wait` transition.

## Follow-up

Run fifth-save AutoFishing with `InstantBite` enabled and verify one completed hook/reel costs the same native amount as manual fishing.
