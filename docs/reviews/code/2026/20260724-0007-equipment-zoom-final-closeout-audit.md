# MoreEquipmentSlots And Zoom Final Closeout Audit

**Review ID:** `20260724-0007`

**Date:** 2026-07-24

**Status:** recorded — physical migrations remain implemented, but the final
behavior closeout is not accepted

**Reviewed range:** `8ea0cad8..ea5951e1`

**Scope:** independent review of the corrections and final evidence used to
close MoreEquipmentSlots and Zoom after Review `20260724-0006`. This Review
does not reopen the physical ProductNative migrations, admit another product,
or authorize a complete Release, L0-L5, GC gradient, or long test.

## Verdict

The corrections are substantive: the wrong-root `NoNativeSave` path, disposable
fixture reparse-point gap, Zoom baseline compounding, exact attack binding, and
the previously absent real ProductNative operation routes were all addressed.
The final packages and the behavior actually exercised by
`202032`/`202146`/`202526` are bound to the final game-loaded implementation.

The combined `verified/closed` declaration is nevertheless premature:

- Zoom has one normal-player path that can leave product-derived camera state
  after the product returns to 1x or is disabled.
- The MoreEquipmentSlots no-save fixture performs the requested operations but
  returns its content to the same empty Committed baseline before title. It
  therefore does not prove rollback of an already committed shield or item.
- Two smaller fail-closed/tool-lifecycle gaps and current documentation drift
  remain.

MoreEquipmentSlots should return to `implemented/open/partial`, and Zoom should
return to `implemented/open`, until the bounded gates below pass. The first ten
physical ProductNative migrations remain implemented; the ten-product
behavioral baseline is not frozen by this closeout.

## Findings

### P1 — Zoom can leave scaled `CameraController.camSize` and room ranges after restoration

The product currently reads and writes only
`DolocAPI.mainCamera.orthographicSize`. That is insufficient when the player
changes resolution or fullscreen state while Zoom is active:

1. `MainMenuUiState` exposes `SettingPanelUiState` during ordinary gameplay.
2. `ScreenManager.RefreshResolutionBySetting` and
   `RefreshFullScreenBySetting` call
   `DolocAPI.cameraController.RefreshResolution()`.
3. `CameraController.SetResolution` derives and stores `camSize` from the
   current `mainCamera.orthographicSize * 2`, then recalculates room ranges.
4. `SetRoomRange`, `SetPosition`, and `Constraint` continue to consume that
   cached `camSize`.
5. `ZoomNativeRuntime` later restores only `orthographicSize`; it does not
   restore or natively refresh the derived controller state.

For example:

```text
native orthographicSize=16.875, camSize.y=33.75
-> Zoom 4x writes orthographicSize=67.5
-> resolution/fullscreen refresh writes camSize.y=135
-> Zoom returns to 1x or is disabled and restores orthographicSize=16.875
-> camSize.y and ranges remain based on 135 until another native refresh
```

This is an ordinary UI path, not a hypothetical second Mod owner. It can affect
camera following and room clamping after Zoom reports itself restored. The
current Hook map explicitly says that the product does not call
`CameraController.RefreshResolution`, and the Unit, QA, and `202032` oracles
observe only `orthographicSize`. They cannot detect this native-state residue.

Relevant paths:

- `products/first-party/Zoom/src/Native/ZoomNativeRuntime.cs`
- `docs/hook-map/focused/Camera.md`
- reviewed build `DolocTown/MainMenuUiState.cs`
- reviewed build `DolocTown/ScreenManager.cs`
- reviewed build `DolocTown/CameraController.cs`

### P1 acceptance gap — MoreEquipmentSlots no-save evidence is empty-to-empty

The owning Update requires real damage, break, equip, replace, and unequip
followed by no-save title/cold rollback, including exactly-one-item behavior.
The corrected fixture now executes every real operation, but its rollback
oracle is degenerate:

- it requires an empty Committed product slot;
- it gives and equips a new `grandmas_button`, replaces it with a newly given
  `box_hat`, executes real shield damage and break, then performs a post-break
  equip/unequip;
- before title it manually removes the QA-owned backpack item, leaving both
  Working content and Committed content empty;
- the first-process evidence reports `committedOccupied=0` and
  `workingOccupied=0`;
- the cold observer proves that the empty Committed state reloads as empty.

This usefully proves that the operations mutate only Working state, do not
advance Committed state, and leave no candidate/journal. It does not prove:

- an already committed shield restores its previous durability after a no-save
  title return;
- an already committed broken shield returns exactly once;
- replacing or unequipping an already committed item rolls both the product
  slot and native backpack back to exactly one logical item.

No implementation-level corruption was found in the
Working/Candidate/Committed source path. This is a blocking save-safety
acceptance gap, not evidence that the untested rollback is broken.

Relevant evidence:

- `MoreEquipmentSlotsNoNativeSaveFixtureCase.cs`
- `GAME-SMOKE/20260724-202032/DTMAPI-latest.log`
- `GAME-SMOKE/20260724-202146/DTMAPI-latest.log`

### P2 — Zoom maximum-clamp failure is overwritten as success

`ZoomNativeRuntime.Configure` clamps `currentViewScale`, invokes
`ApplyCurrentScale`, ignores its Boolean result, and then unconditionally writes
`configured-product-native`.

If `MaxViewScale` changes from 4 to 1 while camera read/write fails, the live
camera can remain at 4x while the runtime records scale 1 and success. A later
`OnEnvironmentReset` then takes the 1x branch and can register the still-scaled
live value as a new vanilla baseline. Existing write-failure coverage exercises
`SetViewScale`, not this configuration path.

### P2 — Save runner leaves process environment redirected

For a non-`NoNativeSave` run, `run-game-smoke.ps1` assigns
`DTMAPI_DOLOC_PERSISTENT_ROOT` and `DTMAPI_STATE_DIR` after validation but does
not restore their previous values. Environment-provider writes survive a
script-scope invocation in the same PowerShell process. A later no-save run
fails closed, while other tools or a manual launch from that shell can continue
to observe the disposable root.

The focused validation-only tests return before those assignments and therefore
do not cover success or failure cleanup of the two environment values.

Completed disposable save fixtures also remain outside the existing managed
test-session lifecycle. Three current completed trees under
`temp/disposable-save-fixtures` total approximately 28.5 MB. They do not
invalidate `202526`, but successful bounded fixtures should be removed or use
the existing managed ownership/cleanup mechanism. Do not introduce a new
receipt family for this.

On Windows PowerShell 5.1, the new reparse negative test also fails during
junction cleanup at `test-game-smoke-save-modes.ps1:203` with a
`NullReferenceException`; the same test passes under PowerShell 7. This is a
focused test-host cleanup issue, not a player Runtime defect.

### P2 — Current closeout documents disagree and weight measurements are stale

The canonical Batch 6 contract simultaneously says:

- Review `20260724-0006` still finds MoreEquipmentSlots runtime coverage
  incomplete;
- MoreEquipmentSlots remains independently open;
- Zoom has only the historical `180233` non-acceptance baseline;
- later current-state rows declare both products verified/closed.

The implementation Review also absorbed a long completion narrative and
artifact ledger even though document governance assigns completion evidence to
the owning Updates and allows only a short resolution link in a code Review.
That duplication helped produce the contradictory current truth.

Final source changes also invalidated several weight snapshots. At current
HEAD:

| Boundary | Files | Physical | Non-empty |
|---|---:|---:|---:|
| MoreEquipmentSlots ProductNative `src` | 13 | 7,213 | 6,715 |
| Zoom ProductNative `src` | 9 | 1,434 | 1,339 |
| all ten ProductNative `src` trees | 91 | 23,270 | 21,453 |

The current Zoom DLL is 29,696 bytes and the Compatibility Host is 437,760
bytes. The Zoom Update, Batch 6 contract, roadmap, and older eight-product table
still contain earlier component counts. The aggregate product DLL total happens
to remain correct because the final MoreEquipmentSlots and Zoom DLL changes
offset each other, but the component and source explanations are stale.

## Accepted Parts

This audit does not invalidate:

- the physical MoreEquipmentSlots and Zoom ProductNative migrations;
- the real default-loaded mandatory Runtime reduction, with no claim about
  repository, download, install, total-shipped, or all-products-enabled size;
- the exact 35-member retained CameraView ABI gate;
- the one-Hook Zoom and four-Hook MoreEquipmentSlots ownership sets, dual-owner
  arbitration, atomic installation, exact-owner cleanup, SDK/package/Doctor
  gates, or Loader zero-leftover evidence for the observed state;
- corrected Zoom `2x -> 1x`, `MaxViewScale -> 1`, repeated no-size-change
  `SetEnvCamera`, configuration-disable, title, and Loader behavior on the
  covered path;
- real MoreEquipmentSlots replacement, attack, break, equip/unequip, Working
  dirtiness, unchanged Committed state, native-save promotion, and
  exactly-one-item evidence for the covered empty-baseline/save routes;
- `202032`/`202146` as `NoNativeSave`, `202526` as an AutoCloud-isolated
  `NativeSaveExpected` run, their exact `25403595da3e` game-loaded source
  binding, process/Doctor results, and unchanged-before-cleanup receipts;
- the classification of `202324` as non-acceptance and complete Release count
  zero.

## Smallest Reacceptance

### Zoom

1. Decide and encode the relationship between product scale and the native
   `CameraController.RefreshResolution/camSize/range` state. Restoration must
   leave both the camera size and the native derived state at a coherent 1x
   baseline.
2. Add a focused fixture that changes resolution/fullscreen while at 4x, then
   verifies 1x, config disable, title, and Loader cleanup of both
   `orthographicSize` and `CamSize`/ranges.
3. Make configuration reapplication propagate failure and preserve a coherent
   in-memory/live scale; add read- and write-failure cases for
   `MaxViewScale -> 1`.
4. After focused build/Unit/QA gates pass, run one smallest corrected Zoom game
   smoke. No complete Release or broad ladder is required.

### MoreEquipmentSlots

1. Use an AutoCloud-isolated disposable fixture to establish and normally save
   one committed shield.
2. In a later no-native-save process, damage it, return to title/reload, and
   prove the committed durability returns.
3. Break the committed shield in a no-native-save process, return to
   title/reload, and prove the same shield returns exactly once.
4. Cover committed replace/unequip with the same exact slot/native-item oracle.
   Reuse the existing runner, journal/store harness, and evidence format; do not
   create a new assurance authority or run a complete Release.

### Runner and documents

1. Restore both persistent-root environment values in one outer cleanup path
   and give disposable fixtures bounded existing-lifecycle cleanup.
2. Keep Windows PowerShell 5.1 junction deletion from turning a successful
   negative test into residue/failure.
3. Reopen and later close the two existing owning Updates. Correct the current
   contract, Hook/smoke state, and measurements once. Keep historical evidence
   historical; do not append another completion ledger to Review `0006`.

## Validation Performed

- inspected `8ea0cad8..ea5951e1`, current source, QA routes, reverse build,
  package/manifest identity, and `202032`/`202146`/`202526` evidence;
- current Release build of `DTMAPI.UnitTests`: zero warnings, zero errors;
- focused `zoom-product`, `zoom-acceptance-routing`,
  `moreequipment-product`, `moreequipment-acceptance-routing`, and
  `compatibility-host`: PASS;
- `test-game-smoke-save-modes.ps1`: PASS under PowerShell 7; junction-cleanup
  failure under Windows PowerShell 5.1;
- current product Catalog: PASS;
- `git diff --check`: PASS;
- document governance: 5,875 checks with only the two pre-existing failures
  caused by the unrelated user-owned untracked portable reverse-capture Update
  and its absent monthly row.

No game process, complete Release, L0-L5, GC gradient, or long test was run.

## Disposition

The findings above remain the historical independent-review record. Commit
`136a7278ed79614d7dffe259d21789b02329d869` implements the bounded fixes.
The owning Updates contain the final acceptance facts:

- Zoom `GAME-SMOKE/20260724-221902` closes native resolution/fullscreen-derived
  state and maximum-to-1 failure propagation;
- MoreEquipmentSlots `222231`/`222329`/`222423`/`222516` close the normally
  saved non-empty shield rollback and runner cleanup gaps.

Both products are therefore `verified/closed`; this Review does not admit an
eleventh product or create another receipt, gate, or audit lifecycle.
