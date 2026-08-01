# Zoom Active-Scale Refresh Coherence

## Metadata

- Update ID: `20260726-0001`
- Date: `2026-07-26`
- Lifecycle Status: `verified`
- Validation Level: `docs,source,unit,runtime,player`
- Runtime Validation: `passed`
- Related Issue State: `closed`

## Source Request And Authority

Close the remaining Zoom P2 coverage debt for the exact ordinary-player
sequence:

```text
4x -> native resolution/fullscreen refresh -> 2x
```

The bounded authority is the non-blocking P2 recorded by Review
`20260725-0001`. Review `20260724-0007` and Update `20260724-0002` remain the
native-owner and restoration authorities. This Update does not reopen the
tenth-product admission, add a public API, add a Hook, admit another product,
or authorize a complete Release/GC/long-test ladder.

## Implemented Boundary

- `ZoomNativeRuntime.ApplyScale` now treats
  `CameraController.RefreshResolution()` as part of every active-scale
  transaction, not only the return-to-1x transaction.
- A `4x -> native refresh -> 2x` transition therefore writes the retained
  native baseline at 2x and rebuilds `camSize` plus room x/y ranges for 2x.
- If the native derived-state refresh fails, the runtime writes the prior
  camera size back, refreshes the prior derived state, leaves the previous
  published scale intact, and reports `failed-closed`.
- The focused Unit fixture reproduces the exact sequence and requires both the
  orthographic size and derived camera height to reach 2x.
- The real-game QA fixture records the native 2x `camSize`/x-range/y-range
  snapshot, performs the exact 4x-refresh-2x sequence, and requires the full
  derived-state snapshot to match before continuing through the existing
  SetEnvCamera, 1x, disable, title, and Loader-owner gates.

The physical owner remains `DTMAPI.ZoomMod`; the exact Harmony set remains one
Postfix on the five-parameter `DolocAPI.SetEnvCamera` overload. No player
Runtime component or public API gained product behavior.

## Changed Files

- `products/first-party/Zoom/src/Native/ZoomNativeRuntime.cs`
- `tests/DTMAPI.UnitTests/ZoomProductTests.cs`
- `src/DTMAPI.GameBridge.DolocTown.QA/Scenarios/Fixtures/ZoomProductNativeFixtureCase.cs`
- `docs/hook-map/focused/Camera.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260726-0001-zoom-active-scale-refresh-coherence.md`
- `docs/updates/INDEX-2026-07.md`

## Validation

Passed before the player run:

- Release builds for Unit, QaUnit, product/runtime dependencies and the
  physical Zoom Harmony fixture: zero warnings and zero errors;
- focused `DTMAPI_UNIT_TEST_FOCUS=zoom-product`;
- Release `DTMAPI.QaUnitTests`;
- `git diff --check`.

The broad Unit execution reached an unrelated Compatibility Host fixture whose
temporary release manifest was absent. The focused Zoom runner passes and is
the relevant source-level authority; the unrelated failure is not reclassified
as Zoom evidence.

`GAME-SMOKE/20260726-074143` is the exact implementation-commit
(`BuildCommit=1a358ccb035a`) third-save `NoNativeSave` acceptance. It reports:

```text
scale2=33.75
direct1=16.875
scale4=67.5
refresh4to2=33.75
resetVanilla=16.875
afterRealSetEnvCameraTwice=67.5
maxTo1Restore=16.875
configDisableRestore=16.875
camHeight1=33.75
camHeight2=67.5
camHeight4=135
```

The fixture matched the full 2x `camSize`/x-range/y-range snapshot after the
4x native refresh, retained exactly one product Hook and zero Compatibility
Hooks, restored title state, and then passed real Loader deactivation:

```text
title4to1+native1+derived1+actual1+callback1
-> instance0+actual0+callback0+roots0
```

Player Doctor reported zero errors/warnings/misplacements. All three selected
player archives and both relevant committed-sidecar paths were unchanged by
length/hash/mtime before any runner or external restoration; no routine byte
backup or archive writeback occurred. The QA Host/profile/Author-source state
was restored, no `DolocTown.exe` remained, and the shared runtime lock was
released.

Two prelaunch attempts are non-acceptance operational history: the first was
interrupted during build/package preparation by the caller's output timeout;
the second failed closed on that incomplete Zoom Author SDK package. Neither
started `DolocTown.exe`. The final run rebuilt the tracked SDK package and
passed normally.

No complete Release, L0-L5, GC gradient, native save, or long test was run.

## Rollback

Revert the active-scale refresh transaction, the exact Unit/QA assertions, and
this record together. Do not change the one-Hook owner boundary or restore only
the documentation.

## Follow-Up

Keep this exact active-scale sequence in the focused Zoom fixture. Do not turn
it into a broad release gate or infer authority for another product.
