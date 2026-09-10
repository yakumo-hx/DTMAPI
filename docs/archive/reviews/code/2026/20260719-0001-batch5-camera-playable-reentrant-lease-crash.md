# 20260719-0001 - Batch 5 CameraPlayable Reentrant Lease Crash Review

Status: recorded

Date: 2026-07-19 +08:00

Source: Batch 5 continuation requested after the user moved the third-save spawn beside the large barn and reported that the earlier Zoom route could place the player below the visible farm ground.

Owning implementation lifecycle: `docs/updates/2026/20260718-0003-batch5-event-demand-content-lifecycle-performance.md`

## Observation And Evidence

- The third local Zoom acceptance run, `GAME-SMOKE/20260719-024242`, passed the independent `ZoomOwnerLifetime` G6 case with exact `OfficialLocal` source selection and terminal owner cleanup.
- The following `CameraPlayable` G4 case captured only `before.png`. The process then exited unexpectedly before the 4x screenshot or a scenario terminal receipt.
- `D:\Steam\steamapps\common\Doloc Town\BepInEx\LogOutput.log` contains 585 `CameraView lease acquired` lines for the run-specific low/high QA owners between 02:43:23 and 02:43:25. After the first pair, the log alternates new low and high lease IDs without a release or a state transition receipt.
- Windows Application Error event 1000 records `DolocTown.exe` PID 55236 failing at 02:43:25 in `ntdll.dll` with exception code `0xc0000005`. Windows Error Reporting event 1001 records the same report ID. A Windows-created local crash dump exists at `C:\Users\Administrator\AppData\Local\CrashDumps\DolocTown.exe.55236.dmp`; it is not copied into repository evidence and is not required to establish the managed control-flow defect.
- The latest DTMAPI log stops inside the alternating acquisition sequence. It has no managed scenario failure, cleanup, or normal quit marker.

## Known Code Facts

- `CameraPlayableMovementFixtureScenario.Advance()` stage 1 acquires the low lease, acquires the high lease, calls `applyRuntimeAutomation()`, validates the active owner, requests `scale-4x.png`, and only then changes `stage` to 2.
- The production fixture supplies `() => UpdateRuntimeAutomation()` as `applyRuntimeAutomation`.
- Runtime automation can synchronously drive the QA participant again. Because stage 1 has not been committed, the nested `Advance()` call repeats both acquisitions. Every nested call invokes another runtime update before any caller reaches the stage assignment.
- `lowLease` and `highLease` are overwritten on every nested acquisition, so even a hypothetical unwind would retain handles only for the last pair and could not release the earlier roots authoritatively.
- `CameraViewService` correctly arbitrates each request it receives. This run does not show a production CameraView request multiplying on its own; the QA fixture explicitly submitted every logged acquisition.

## Root Cause

The G4 CameraPlayable state machine publishes its durable stage after invoking a synchronously reentrant runtime side effect. That violates the transaction rule that state identifying an operation as started must become visible before any callback capable of observing or re-entering it. Re-entry therefore sees stage 1 repeatedly, creates an unbounded recursive acquisition chain, overwrites cleanup handles, and ultimately crashes the Unity process.

Severity is P0 for the QA route because the defect caused a real process crash. It does not by itself establish a P0 defect in ordinary Zoom or the production CameraView API.

## Rejected Or Unproven Hypotheses

- The user-reported below-ground movement is not the cause. The crash occurs before movement starts; only `before.png` exists.
- The earlier Workshop/local product identity mismatch is not the cause. `ZoomOwnerLifetime` passed with the exact local product identity and root before CameraPlayable began.
- This is not classified as the open long-run Mono GC issue. The failure occurs immediately after a deterministic recursive lease flood. No evidence currently links it to forced GC or the Batch 5 GC ladder.
- The `ntdll.dll` fault alone does not prove the native owner. The managed state-machine defect and 585 explicit acquisitions are sufficient to require a fix, while dump-symbol analysis remains unnecessary unless the bounded fix still crashes.
- Production CameraView must not be changed to deduplicate arbitrary distinct leases merely to mask a QA caller that repeatedly submits new lease IDs.

## Required Implementation Boundary

- Commit a non-reentrant activation stage before the first lease acquisition or any runtime automation callback.
- Retain each acquired handle exactly once; repeated pending ticks or synchronous re-entry must not submit another acquisition.
- Preserve fail-closed cleanup when acquisition, automation, active-owner validation, or screenshot request fails.
- Keep the existing ground-safe rightward movement and independent agent/camera restoration checks.
- Make log waiting terminate when a run-start receipt exists and the game process has already exited, even if the polling loop missed the short-lived process.

## Acceptance Gates

1. A source/unit regression must synchronously re-enter the scenario from the automation callback and prove exactly two total acquisitions, no recursive growth, and terminal release/root-zero cleanup.
2. Windows PowerShell 5.1 parsing must pass for the smoke-runner helper changes.
3. Release build/tests must pass.
4. A fresh third-save `OfficialLocal` Zoom run must pass `ZoomOwnerLifetime` and `CameraPlayable`, create all required screenshots and telemetry, record exactly one low and one high QA acquisition, restore agent/camera/prior owner/vanilla scale, report root zero, exit cleanly, and restore the exact Author source/profile transaction.
5. The screenshots must show that the bounded movement remains on visible farm ground beside the large barn; the map-boundary mask must not obstruct the acceptance sequence.

## Downstream Facts To Update After Validation

- Append the implementation/evidence outcome to the owning Batch 5 Update record.
- Update `docs/debug/issues/ISSUE-009-20260608-camera-playable-dynamic-qa.md` with the dated regression and fresh bounded evidence.
- Update `docs/hook-map/focused/Camera.md` and the CAMERA-PLAYABLE smoke row only if the new runtime evidence passes.
- Keep `ISSUE-010` and `ISSUE-011` open; this deterministic QA crash does not close either broader crash class.

## Resolution Link

The focused implementation and runtime result are owned by [Batch 5 Update 20260718-0003](../../../updates/2026/20260718-0003-batch5-event-demand-content-lifecycle-performance.md). Fresh acceptance `GAME-SMOKE/20260719-031358` passed Zoom arbitration, real-owner lifetime cleanup, CameraPlayable evidence files, ground-safe movement, GameBridge Camera cleanup health, optional-QA lifecycle/cleanup, profile restoration, no-fatal and process-exit gates. The P0 classification remains limited to the deterministic optional-QA reentrant fixture failure in `20260719-024242`; no ordinary Zoom/CameraView P0 is inferred, and neither ISSUE-010 nor ISSUE-011 is closed by the bounded rerun.
