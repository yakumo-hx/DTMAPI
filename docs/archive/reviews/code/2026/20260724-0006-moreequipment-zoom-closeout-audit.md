# MoreEquipmentSlots And Zoom Closeout Audit

**Review ID:** `20260724-0006`

**Date:** 2026-07-24

**Status:** superseded — the recorded findings remain historical; current
closeout status is owned by Review `20260724-0007` and the two owning Updates

**Scope:** independent closeout review of the MoreEquipmentSlots
Working/Committed correction, the `NoNativeSave` / `NativeSaveExpected`
workflow, and the tenth-product Zoom migration. This Review does not reopen
the already accepted physical ownership of either ProductNative package, and
does not authorize another product or a complete Release run.

## Verdict

The combined closeout was **not accepted at audit time**. The final resolution
at the end of this Review supersedes that lifecycle decision without rewriting
the findings or promoting the earlier failed runs.

- MoreEquipmentSlots has the intended Working/Candidate/Committed source
  structure, and the existing runtime evidence remains valid for the paths it
  actually exercised. Its declared ProductNative recovery matrix and reusable
  save-mode runner are nevertheless incomplete.
- Zoom is physically split out of mandatory Runtime, its exact ABI and
  dual-owner gates remain useful, and Loader cleanup reaches zero. Its normal
  camera behavior is not accepted: `GAME-SMOKE/20260724-180233` directly
  records a multiplied camera baseline rather than a restoration.
- The unaffected first seven products and StrongPlantingGun remain
  verified/closed. The ten-product physical aggregate is implemented, not a
  frozen behavioral baseline. Eleventh-or-later admission remains blocked.

## Findings

### P1 — Zoom recaptures its own scaled value as the vanilla baseline

`products/first-party/Zoom/src/Native/ZoomNativeRuntime.cs` has three related
normal-player failures:

1. `SetViewScale` stores the new scale before `ApplyCurrentScale`. When the new
   scale is `1`, `ApplyCurrentScale` overwrites `vanillaOrthographicSize` with
   the still-scaled current camera value. A `2x -> 1x` request can therefore
   report scale `1` while leaving the camera at `2x`.
2. `Configure` clamps the active scale to the new maximum, but calls
   `ApplyCurrentScale` only when the result remains greater than `1`.
   Reducing `MaxViewScale` to `1` does not restore the camera.
3. `OnEnvironmentReset` always reads the current camera size into
   `vanillaOrthographicSize` before reapplying the scale.

The reviewed game build's
`DolocAPI.SetEnvCamera(Vector2,Vector2,bool,bool,bool)` calls room-range,
optional position and background methods. It does not write
`mainCamera.orthographicSize`. The Zoom Postfix therefore reads the product's
already-scaled value, mistakes it for a new native baseline, and multiplies it
again.

The accepted-looking log is positive evidence of the defect:

```text
scale2=33.75; scale4=67.5; resetVanilla=67.5;
afterRealSetEnvCamera=270; configDisableRestore=67.5;
titlePendingSize=270
```

The original camera size was `16.875`. A real `SetEnvCamera` call left the
orthographic size unchanged at `67.5`; the ProductNative Postfix then changed
it to `270`, and config disable restored the polluted `67.5` baseline.
`GAME-SMOKE/20260724-180233` is therefore **non-acceptance**, not a Zoom
behavior PASS.

The Unit and QA oracles hid this fault:

- the Unit test manually changes its fake native size before
  `OnEnvironmentReset`, modeling a native reset that the real method does not
  perform;
- the runtime fixture accepts `resetVanillaSize * 4`;
- the title fixture compares the final size with the already-polluted internal
  baseline rather than the pre-zoom native value;
- no focused case covers minus-key/`Step` to `1`, `2x -> 1x`, or
  `MaxViewScale -> 1`.

### P1 — `NoNativeSave` can compare the wrong save root

`tools/scripts/run-game-smoke.ps1` lets
`Get-DolocTownPersistentRootForSmoke` prefer either
`-DisposableSaveFixtureRoot` or `DTMAPI_DOLOC_PERSISTENT_ROOT` in every mode.
The fixture validation and QA `LocalSave.dataDirPath` redirect run only for
non-`NoNativeSave` modes. The no-save metadata baseline later uses the
overridable helper even though the game still writes the live persistent root.

The validation-only command below incorrectly passes:

```powershell
tools/scripts/run-game-smoke.ps1 `
  -SaveTestMode NoNativeSave `
  -DisposableSaveFixtureRoot E:\definitely-not-live-fixture `
  -ValidateSaveTestModeOnly
```

This can produce a green comparison of an unrelated fixture while the live
save is unobserved. `NoNativeSave` must reject both overrides and bind its
metadata to `Get-DolocTownLivePersistentRootForSmoke`.

The retained `155216` and `155344` evidence files themselves name the real
`C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\SAVE` archive
paths and prove those paths unchanged. This runner defect does not retroactively
invalidate those two covered runs; it does prevent the workflow from being
declared closed.

### P1 — MoreEquipmentSlots runtime recovery coverage is narrower than declared

The Update requires no-save title/cold rollback and committed-save behavior for
shield damage, shield break, equip, replace and unequip.

The ProductNative game fixtures do not execute that matrix:

- the `NoNativeSave` case requires an empty slot and performs one
  `EquipFromBackpack`;
- the `NativeSaveExpected` case exercises equip followed by unequip;
- shield damage, shield break and replacement do not run through the real
  ProductNative game path;
- production unequip is not covered on the no-save path.

Unit and Compatibility Host coverage is valuable, but direct coordinator
projection and source-string checks are not substitutes for the already
declared bounded ProductNative behavior matrix. No source-level corruption was
found in the Working/Candidate/Committed flow; the finding is an acceptance
gap, not proof that those unexercised operations are broken.

### P2 — Disposable save fixtures do not reject reparse points

The runner and `QaSaveFixtureIsolation` compare lexical paths, required
directories and a self-declared marker. They do not reject a fixture root or
`SAVE` directory that is a junction or symbolic link into the live Steam
AutoCloud tree. Existing `161422`/`161536` ordinary-directory evidence remains
usable, but future `NativeSaveExpected` / `ArchiveMutation` runs must reject
reparse points before launch.

### P2 — The frozen CameraView exact set contains 35 MemberRefs, not 36

The ABI harness and retained-artifact report both contain exactly **35**
CameraView MemberRefs:

- `CameraViewRequest`: one constructor plus eight setters (`9`);
- `CameraViewResult`: five getters (`5`);
- `CameraViewState`: fourteen getters (`14`);
- `ICameraViewApi`: two calls (`2`);
- `ICameraViewLease`: five calls (`5`).

The exact-set gate is sound and no ABI member is missing. The documentation
must say `35`; it must not invent a thirty-sixth member.

### P2 — `180233` is artifact/content-bound, not final-commit-bound

The evidence `install-state.json` and `release-manifest.json` record parent
commit `02265fd75fcf`, while the implementation commit is `cbabf184`.
Package, entry-DLL and source-tree hashes bind the tested Zoom content, so this
metadata mismatch alone does not require another game launch. It does mean the
run must not be described as exact-final-commit evidence. The Zoom P1 already
requires one corrected bounded rerun.

### P2 — Independent acceptance was declared before it happened

The Zoom Update marked itself `verified/closed` and said an independent review
found no P0/P1/P2. The only earlier Zoom Review was explicitly admission-only.
This Review is the first independent implementation closeout, and it found the
blocking behavior above. Both affected Updates return to `implemented/open`
until their focused acceptance gates pass.

### P3 — Compatibility consumer count drift

The current Compatibility Host has nine exact retained consumers after Zoom,
not eight. This is a current-fact correction only; it does not alter the Host
design.

## Accepted Parts

The findings do not reverse the following results:

- MoreEquipmentSlots and Zoom are real ProductNative physical migrations;
- mandatory GameBridge remains smaller by the recorded default-loaded
  measurements; no repository/download/total-shipped reduction is inferred;
- MoreEquipmentSlots source separates Working, uncommitted gameplay candidate,
  Committed, OwnerRecovery and OrphanRecovery state;
- `155216`/`155344` prove the real third-save archive files and committed
  sidecars unchanged for their actual equip/title/cold path;
- `161422`/`161536` remain ordinary-directory isolated native-save evidence for
  their actual equip/unequip/promotion/cold path;
- Zoom's ProductNative package shape, one-Hook atomic installation, both
  owner orders, exact-owner cleanup, dormant Compatibility Host placement,
  SDK/package/Doctor gates and Loader zero-leftover checks remain structurally
  valid;
- all 35 observed CameraView MemberRefs remain protected by an exact gate.

## Smallest Reacceptance

### MoreEquipmentSlots and save-mode runner

1. Make `NoNativeSave` reject `-DisposableSaveFixtureRoot` and
   `DTMAPI_DOLOC_PERSISTENT_ROOT`, and add focused negative tests for both.
2. Reject reparse points on the disposable root, `SAVE`, `DTMAPI` and every
   native archive path used by the fixture.
3. Extend the bounded ProductNative fixtures to cover damage, break, equip,
   replace and unequip across no-save title/cold rollback and successful native
   save, preserving exactly one logical item.
4. Rerun only the focused runner tests, product/Host Units and the smallest
   affected game cases. No complete Release, L0-L5, GC gradient or long test is
   required.

### Zoom

1. Preserve one true native baseline across `2x -> 1x`, minus-key `Step`,
   config maximum reduction, config disable, title and Loader cleanup.
2. A real `SetEnvCamera` call that did not change orthographic size must not
   compound the current scale. If another native owner genuinely changes the
   orthographic size, rebaselining must be explicit and distinguishable from
   this product's last applied value.
3. Add focused tests for `1x -> 2x -> 1x`, `4x -> MaxViewScale=1`, repeated
   real no-size-change `SetEnvCamera`, genuine native baseline change,
   disable/title and Loader cleanup.
4. Run one corrected third-save `NoNativeSave` Zoom smoke after the focused
   gates pass. No complete Release, L0-L5, GC gradient or long test is
   required.

Do not admit an eleventh product until these two open closeouts are resolved.

## Validation Performed By This Audit

- inspected the current implementation commit and its parent;
- reviewed the current save runner, MoreEquipmentSlots ProductNative/Host
  fixtures, Zoom ProductNative/Unit/QA paths, retained ABI harness, evidence
  manifests and reverse build;
- reproduced the invalid `NoNativeSave + DisposableSaveFixtureRoot`
  validation-only PASS;
- `test-game-smoke-save-modes.ps1` passed, demonstrating that its current
  focused matrix does not detect that invalid combination;
- focused Unit Release build plus `zoom-product`,
  `moreequipment-product` and `compatibility-host` passed; the missing semantic
  cases above explain why those green results are insufficient.

No game process, complete Release, L0-L5, GC gradient or long test was run by
this audit.

## Resolution Disposition

The implementation/evidence response to this Review is recorded by Updates
`20260723-0008` and `20260724-0002`, not duplicated here. Later independent
Review `20260724-0007` retains the covered evidence but supersedes the final
closeout state with two smaller open acceptance gates.
