# Camera Hook Map

Last updated: 2026-08-01

## Scope

This focused map covers the playable world-camera orthographic-size path, the
Zoom ProductNative hook, and the frozen CameraView/CameraZoom compatibility
boundary. It does not claim ownership of panorama/photo cameras, background or
fog compensation, scanner refresh, UI scaling, camera follow/range rules, or
room construction.

## Current Ownership

- `DTMAPI.ZoomMod` is the only current gameplay product consumer. Its managed
  Advanced ProductNative assembly owns scale/config/input state and exactly
  three Harmony patches under owner `dtmapi.mod.dtmapi.zoommod`: the
  five-parameter `DolocAPI.SetEnvCamera` Postfix plus a Prefix and Finalizer on
  `CameraController.RefreshResolution()`.
- `products/first-party/Zoom/src/Native/ZoomNativeRuntime.cs` captures and
  restores one live playable-camera baseline. Update `20260801-0001` returns
  ownership to `orthographicSize` only: product scale changes do not invoke
  native refresh and never write controller follow, enabled, camSize, range or
  position fields. A genuine native refresh temporarily sees the 1x baseline;
  the Finalizer restores the selected presentation multiplier on normal return
  or exception without swallowing the native exception.
- `ZoomHookInstaller` installs its three-patch set atomically. A partial install,
  another live owner, a residual owner, or a restore failure is fail-closed;
  exact-owner unpatch still runs.
- `src/DTMAPI.GameBridge.DolocTown/Features/Camera/CameraFeature.cs` is now a
  thin registration/proxy/demand coordinator. Mandatory GameBridge no longer
  contains the CameraView/CameraZoom state executors or the deleted
  `CameraDiagnosticsService`.
- Frozen `ICameraViewApi` and obsolete `ICameraZoomApi` execution live only in
  the existing dormant-shipped Compatibility Host under
  `src/DTMAPI.GameBridge.DolocTown/Compatibility/Camera/`. The retained
  `0.4.2-dtmapi` Zoom binary is their only real product consumer.
- ProductNative and compatibility owners are mutually exclusive in both load
  orders. Product-first rejects a later compatibility demand; compatibility-
  first rejects product Hook installation. Neither path can silently become a
  second native owner.
- ItemDisplayName still uses the independent SharedNative
  `EnvironmentResetHookBridge` route to invalidate its cache after
  `SetEnvCamera`. Sharing a native callback point does not merge its lifecycle
  or demand with Zoom ProductNative.

## Hooks: Zoom ProductNative camera presentation

- Status: `verified` by Updates `20260801-0001` and `20260801-0002`. The
  corrected ProductNative candidate passed the combined player test and is
  authorized only through its frozen existing-Workshop update tree.
- Native targets: the exact five-parameter `DolocAPI.SetEnvCamera` overload and
  zero-argument instance `CameraController.RefreshResolution()`.
- Patch type/count: one Postfix plus one Prefix and one Finalizer; exactly three,
  all-or-none.
- Product behavior: 1x through 4x orthographic scaling with configured key
  bindings. Scale transactions only write `mainCamera.orthographicSize` and do
  not call `RefreshResolution`. When the game itself performs a real refresh,
  the Prefix exposes the retained 1x baseline so all native derived state stays
  game-owned, and the Finalizer reapplies the current multiplier. It does not
  own positioning, follow, enabled, camSize, room ranges, scanner, background,
  fog or panorama policy.
- Reset behavior: the reviewed native `SetEnvCamera` method does not change
  orthographic size. If the Postfix observes the product's last applied value,
  it reapplies from the retained true baseline without recapturing; a genuinely
  different native value can establish a new baseline.
- Cleanup: title, disable and real Loader owner deactivation restore the original
  native orthographic size and remove all three exact-owner patches. No derived
  controller field is written as part of cleanup. A failed product-owned write
  remains failed and cannot publish a false restoration result.

## Frozen Public ABI

- `ICameraViewApi`, `ICameraViewLease`, `CameraViewRequest`,
  `CameraViewResult`, and `CameraViewState` remain Frozen/Obsolete compatibility
  surface. The exact retained Zoom DLL resolves 35 CameraView MemberRefs; that
  complete set is locked by the retained-binary ABI gate.
- `ICameraZoomApi`, `CameraZoomOptions`, `CameraZoomRegisterResult`,
  `CameraZoomResult`, and `CameraZoomState` remain obsolete compatibility
  surface.
- The new Zoom product consumes neither public API family. Mandatory Runtime
  can remain thin until a retained legacy consumer requests the Host.
- The older `CameraPlayable`/`ZoomOwnerLifetime` scenario is historical frozen
  compatibility evidence, not the current product acceptance route and not a
  production QA seam.

## Validation

- The 2026-08-01 focused Unit and physical Harmony-owner fixture prove exact
  three-patch installation/rollback/cleanup, no product-initiated native refresh,
  native refresh observation of the 1x baseline, 4x Finalizer restoration and
  exact native-exception propagation. The final player test additionally proves
  moving-player follow at 4x without the former fixed-center flicker, plus clean
  1x/2x/4x transitions in the combined twelve-item profile.

- Focused Unit/source gates pass for ProductNative behavior, both owner load
  orders, exact Harmony ownership, rollback, config disable, Loader
  deactivation, Compatibility Host activation, API metadata, and acceptance
  routing.
- Exact retained ABI validation passes with 35 CameraView MemberRefs, all 11
  retained public products, mandatory GameBridge, and the dormant
  Compatibility Host.
- Catalog, Author SDK, Advanced reference policy, package, Manager, Doctor, and
  zero-leftover checks pass for the exact ten-product set.
- `GAME-SMOKE/20260724-180233` is non-acceptance. It proves current product
  loading, ownership, Doctor, Loader cleanup and NoNativeSave preservation, but
  also records the defect: native `16.875` became `67.5` at 4x, the real
  no-size-change `SetEnvCamera` path compounded it to `270`, and config disable
  restored the polluted `67.5` baseline.
- `GAME-SMOKE/20260801-033908` is the current Z1 third-save `NoNativeSave`
  focus. It observes 1x `16.875`, 2x `33.75`, 4x `67.5`, then
  `4x -> native RefreshResolution -> 2x = 33.75`; two later `SetEnvCamera`
  callbacks preserve 4x `67.5`. The exact inventory is one SetEnvCamera
  Postfix plus the refresh Prefix/Finalizer. Title restores 1x and real Loader
  cleanup reaches zero instance, actual/derived patch, callback and owner-root
  counts while archives and committed sidecars remain unchanged. This numeric
  native-owner proof does not replace the user's moving-camera visual check.
- `GAME-SMOKE/20260724-202032` is the exact-final-candidate corrected
  third-save acceptance. It proves direct `2x -> 1x`, maximum-to-1 and config-
  disable restoration to native `16.875`; repeated real no-size-change
  `SetEnvCamera` callbacks preserve 4x `67.5` instead of compounding; title and
  Loader cleanup reach native 1x, zero Hook/callback/instance/roots, and the
  NoNativeSave metadata gates pass.
- `GAME-SMOKE/20260724-221902` is the final current-commit acceptance. It
  reproduces native resolution/fullscreen refresh at 2x and 4x, then proves
  direct 1x, maximum-to-1, configuration disable, title recovery and real
  Loader deactivation restore the original `16.875` orthographic size and
  exact initial `camSize`/x-range/y-range. Loader cleanup reaches zero product
  instance, callback, Hook and roots; the NoNativeSave metadata and process
  exit gates pass.
- Update `20260726-0001` closes the later focused P2 source/QA coverage gap for
  the direct `4x -> native resolution/fullscreen refresh -> 2x` sequence.
  `GAME-SMOKE/20260726-074143` binds implementation commit `1a358ccb035a`,
  observes `67.5 -> 33.75`, matches the complete captured 2x
  `camSize`/x-range/y-range snapshot, and passes title plus real Loader cleanup
  to zero instance/callback/Hook/roots with unchanged NoNativeSave evidence.
- `GAME-SMOKE/20260724-175944` is retained non-acceptance evidence: it exposed
  the post-`SaveLoaded` `CurrentRoom` readiness window and led to the pending-
  before-write retry correction.
- No complete Release, L0-L5, GC gradient, or long test was run for this
  migration.

## Related Records

- `docs/reviews/code/2026/20260724-0005-tenth-product-zoom-admission-review.md`
- `docs/updates/2026/20260724-0002-zoom-tenth-advanced-product.md`
- `docs/architecture/batch6-managed-mod-identity-contract.md`
- `docs/api/public-api-matrix.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/reviews/manual-qa/2026/20260610-0006-cameraview-manual-play-gate.md`
- `docs/updates/2026/20260722-0004-five-product-baseline-correction.md`
