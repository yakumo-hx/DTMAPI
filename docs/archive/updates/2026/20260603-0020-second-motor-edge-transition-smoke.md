# 20260603-0020 - Second Motor Edge Transition Smoke

## Source Request / Goal

Continue the 0.2.4 manual-QA regression goal, especially Task E: the alternate flying motor must remain independent from the original motor when crossing a map boundary, must not drag the original motor to the new map entry, and must not leave the player stuck.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs`
- `testmods/SecondMotorMod/README.md`
- `readme.md`
- `docs/debug/INDEX.md`
- `docs/debug/issues/ISSUE-005-20260603-manual-qa-024-regressions.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`
- `docs/updates/INDEX.md`

## Implementation

- Added a `DolocAPI.EnterRoom` postfix hook so GameBridge can capture the target room/position when the player crosses a map boundary while riding a DTMAPI second motor.
- Added pending room-transition state to the second-motor runtime, then applies that state once `DolocAPI.CurrentRoom` matches the captured destination.
- Synchronizes the second-motor clone, body, Rigidbody2D, and controller positions to the destination while keeping the original motor archive snapshot restored and not visible in the new room.
- Mirrors the invisible original motor transform only while the player is actively riding the DTMAPI clone because native `DolocAPI.AgentPosition` reads the singleton `DolocAPI.Motor` during motor riding.
- Fixed original motor unlock-state reflection to read archive motor data instead of invoking an extension method as if it were an instance method.
- Tightened vehicle smoke so edge-transition success requires a room change, the second motor in the current room, near-destination position, original motor not visible at the new entry, and clean process exit.

## Version

The controlled project version remains `0.2.4`; the required patch bump from `0.2.3` to `0.2.4` is recorded in `20260603-0012-024-baseline-manual-regressions.md`. This record adds same-version regression evidence for the SecondMotor edge-transition slice.

## Validation

- Release build/unit: `powershell -ExecutionPolicy Bypass -File tools/scripts/build.ps1` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- Passing third-save vehicle smoke: `docs/debug/evidence/GAME-SMOKE/20260603-202948`.
- Result: `SaveLoaded=True`, `VehicleSecondMotor=True`, `NoFatalInstanceWindow=True`, `ProcessExited=True`, `ForcedClose=False`.
- Exit check: `docs/debug/evidence/GAME-SMOKE/20260603-202948/process-check.txt` says `No DolocTown.exe process found.`; `fatal-window-check.txt` says `No fatal instance popup found.`

## Evidence

- Hook readiness log: `Vehicle.MotorApi = experimental. Patched native motor key, riding, tuning, unlock, position, and room-entry touchpoints...`.
- Transition capture/apply logs: `Vehicle.SecondMotorMapTransition = experimental. Second motor room transition target captured room=city_郊区-上游丘陵1 x=0 y=0` and `Vehicle.SecondMotorMapTransition = experimental. Second motor room transition applied room=city_郊区-上游丘陵1 x=0 y=0`.
- Edge-transition smoke log: `Smoke exercise VehicleSecondMotor edge-transition OK destination=上游丘陵1-下端 ... changedRoom=True, secondStillRiding=True, secondInCurrentRoom=True, distanceToDestination=0.839, nearDestination=True, originalRoomAfterTransition=farm_type1-平地, originalVisibleAfterTransition=False, originalAtNewEntry=False ... noStuck=True`.
- Final vehicle summary includes `dualVisible=True`, `appearanceIsolated=True`, `originalScopedTint=0/10`, `secondScopedTint=5/10`, `speedMultiplier=2`, `baseMaxSpeed=25`, `effectiveMaxSpeed=50`, `outdoorRecovery=verified`, and `edgeTransition={...nearDestination=True...originalAtNewEntry=False...noStuck=True}`.

## Rejected Hypotheses / Failed Attempts

- `GAME-SMOKE/20260603-194216` showed that the edge probe could start before outdoor recovery had settled, so the smoke now waits after recovery teleport before starting the edge transition.
- `GAME-SMOKE/20260603-194923` and `GAME-SMOKE/20260603-195426` changed rooms but still let the original singleton appear at the new entry, proving `SetMotorPosition` redirection alone was not enough.
- `GAME-SMOKE/20260603-200032` passed the old loose stuck check while the player position remained stale, so the smoke now requires the rider to be near the destination.
- Later tightened failures proved native `DolocAPI.AgentPosition` follows the original singleton motor while riding. The fix mirrors only the invisible transform during active second-motor cross-room riding while preserving the original archive room/visibility snapshot.

## Related Records

- Debug issue: `docs/debug/issues/ISSUE-005-20260603-manual-qa-024-regressions.md`.
- Smoke matrix: `MANUALQA-024-E`, `VEHICLE-001`.
- Hook map: `Vehicle.MotorApi`.
- API matrix: `IMotorVehicleApi`.
- Prior records: `20260603-0008-second-motor-mail-status-smoke.md`, `20260603-0009-second-motor-dual-visible-inline-config.md`, `20260603-0010-original-motor-appearance-restore.md`, `20260603-0013-024-regression-fixes-evidence.md`.

## Rollback

Remove the `DolocAPI.EnterRoom` postfix, pending transition state, and AgentPosition proxy synchronization. Restore the prior smoke expectations only if a future game build exposes a safer native per-vehicle transition API. Keep `GAME-SMOKE/20260603-202948` and retained failed runs as the before/after evidence for singleton transition behavior.

## Follow-Up

- Manual visual player-driven crossing is still useful as user acceptance evidence, but the automated third-save edge-transition requirement is now covered.
- Native scoped sprite replacement for the official example texture remains a future visual polish item; current proof uses instance-scoped tint to avoid mutating the original motor.
- Later record `20260603-0021-mine-placement-equipment-ui-smoke.md` supersedes the Mine placement and More Equipment Slots player-UI evidence gaps that were still open when this record was written.
