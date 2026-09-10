# 20260713-0007 Sixth-Round Closure And Active-Gameplay GC Gate

## Metadata

- Update ID: `20260713-0007`
- Date: 2026-07-13
- Lifecycle Status: `verified`
- Validation Level: `docs, source`
- Runtime Validation: `not-required`
- Related Issue State: `open`
- Source: user supplied `D:/下载/第六轮正式.md`, selected U1/V1 revised/W1, fixed the DTMAPI 0.5.5 release baseline, corrected GC priorities and asked whether any further DTMAPI-wide decisions remained

## User Decisions In Supplied Order

### 1. U1 And The Public Compatibility Boundary

Single-first-party-consumer gameplay capabilities default to first-party internal implementations. DTMAPI 0.5.5 keeps the old public ABI; I1 freeze, warning, public-consumer scan and conditional retirement govern later removal. Publicly obtainable Workshop Mods and author-supplied test builds are within the compatibility promise; unobtainable private binaries are not default release blockers, and direct BepInEx plugins receive Doctor diagnosis without DTMAPI ownership claims.

Analysis immediately following issue 1: `20260713-0012` now records the exact compatibility categories and clean-room future API-evolution research route. This decision changes governance, not any concrete API status in the current source.

### 2. V1 Revised And One Public Runtime Update

DTMAPI 0.5.5 may use multiple internal RCs but has one public player update. AutoFishing releases alone next, ActionSpeed follows only after its focused GC/native-owner gate, low-user products may share a publication window while retaining independent identity/rollback, and MoreEquipment releases alone. Manbo, OneAction, AutoFishing/Zoom and ActionSpeed keep distinct validation roles.

Analysis immediately following issue 2: this supersedes the proposal that the first completed low-risk AutoFishing-or-OneAction product would be chosen dynamically. AutoFishing is the compatibility/update Canary; OneAction is the structural split template. A publication window is not a physical package merge.

### 3. W1 Ends The Global Questionnaire

Future AnimalPack economy, ActionSpeed native scope, MoreEquipment rules, uninstaller implementation, UI and future-platform decisions belong to focused Reviews/Updates. Work starts with Batch 0, then the two independent P0 ownership corrections and the remainder of the 0.5.5 gate.

Analysis immediately following issue 3: no seventh global decision docket is needed on present evidence. A new top-level question is opened only if implementation discovers a release-contract contradiction which existing stop rules cannot resolve.

### 4. DTMAPI 0.5.5 Release Baseline

The release baseline contains ten gates: Catalog/identity/version projection; uninstaller ownership P0; Oil/OneAction P0; unified current/minimum/installed versions; public ecosystem scan; no 0.5.5 ABI deletion plus warnings; QA-free player package/no meaningless polling; demand activation; active AutoFishing/ActionSpeed GC evidence; and old-Runtime block/update/recovery messages.

Analysis immediately following issue 4: the ordered implementation batches and public release waves are different axes. Completing the early version-authority batch is not by itself permission to publish 0.5.5; every release-baseline gate must pass.

### 5. GC Priority, Hypotheses, Matrix And Claims

AutoFishing active play and ActionSpeed are the first gameplay GC gate. The formal throughput, retention, re-entry and double-owner concerns are retained, but source review corrects the last topology claim: the current products target different native state families, so same-Animator ownership is unproven and must be detected before arbitration is designed. The corrected A-H matrix measures per minute, per fish and per ActionSpeed action. MoreEquipment remains high-risk for save/product/recovery, not the current first GC target.

Analysis immediately following issue 5: `20260713-0013` owns the full evidence classes, corrected `GC-H1` through `GC-H4`, matrix and release language. ISSUE-010 remains open and keeps its title-idle/LoadGame Fatal track alongside the new active-gameplay track. No source-only or short-run result may be called a complete Unity GC fix.

### 6. Staged Per-Mod GC Certification

Before 0.5.5, test the production Runtime, active AutoFishing/ActionSpeed, current CustomAnimals/Audio hot paths, known high-risk combinations and common player profiles. Before each later 1.0.0 product, establish its disabled, idle, active, title/reload and extended-run profile.

Analysis immediately following issue 6: this is a staged certification program, not a demand to finish every product investigation before Batch 0 or the P0 fixes.

## Summary

Closed the sixth and final global decision round, recorded the exact 0.5.5 compatibility/release baseline, separated implementation order from public release waves, and created a focused AutoFishing/ActionSpeed active-gameplay GC gate with evidence-safe wording. No Runtime, API, product, manifest, package, game, save or Workshop behavior changed.

## Reviews And Issues

- `docs/reviews/code/2026/20260713-0012-major-update-sixth-decision-docket.md`
- `docs/reviews/code/2026/20260713-0013-autofishing-actionspeed-active-gc-release-gate.md`
- `docs/reviews/code/2026/20260712-0003-dtmapi-full-boundary-audit.md`
- `docs/reviews/api/2026/20260712-0002-workshop-055-binary-compatibility-review.md`
- `docs/api/public-api-matrix.md`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`

## Changed Files

- closed and refined the mainline sixth-round docket;
- added the focused active-gameplay GC Review;
- appended the ISSUE-010 two-track boundary;
- reconciled the full audit, relevant version/release/compatibility/API records and historical refinements;
- added this Update and its monthly ledger row.

## Validation

- formal decision order and screenshot-derived context are preserved in text;
- current AutoFishing/ActionSpeed Hook targets and restore boundaries were source-checked;
- historical ISSUE-010 Fatal-disabled and inactive-AutoFishing evidence was cross-checked;
- `tools/scripts/check-doc-governance.ps1`: passed (`4372` checks);
- `git diff --check`: passed (only existing working-copy LF-to-CRLF warnings were reported);
- focused new records have no trailing whitespace;
- no build/runtime validation is required because this Update changes no implementation or package behavior.

## Runtime Evidence

Not run. No game process was launched, no runtime lock was acquired, and no game, local `MODS`, install, upload or Workshop state was touched.

## Rollback

Revert this documentation-only closure, remove `20260713-0013`, restore `20260713-0012` to proposed status and remove this Update/monthly row. No source, package, runtime or save rollback is required.

## Follow-Up

Begin Batch 0 under its own in-progress Update. Then implement the player-uninstaller and Oil/OneAction P0 streams under independent Updates. The active-gameplay GC Review supplies the later 0.5.5 RC evidence gate; it does not block the initial documentation/catalog work.

## Subsequent GC Direction Refinement

`docs/updates/2026/20260713-0008-autofishing-actionspeed-parallel-gc-direction.md` keeps both products at first priority but replaces the combined-conflict/A-H emphasis with separate, comparable speed ladders for their distinct gameplay domains. All non-GC sixth-round decisions remain unchanged.
