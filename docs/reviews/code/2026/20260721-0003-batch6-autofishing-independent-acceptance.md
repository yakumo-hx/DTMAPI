# Batch 6 AutoFishing Independent Acceptance

Status: `recorded`

Date: 2026-07-21

Owning Update: [Batch 6 AutoFishing Advanced Pilot](../../../updates/2026/20260720-0008-batch6-autofishing-advanced-pilot.md)

## Decision

Accept the AutoFishing Advanced pilot and promote its owning Update from `implemented` to `verified`.

The acceptance target is the source/tooling integration commit `4da7f97d`, together with the docs-only closeout at `5e159f94`. The later retry-governance commit `193d6abe` changes no Batch 6 product, mandatory Runtime, Author SDK, tests, scripts or release authority. No P0 or P1 finding was found.

This remains a bounded proof for `Yuuka.DTMAPI.AutoFishing`. It does not admit another real product, open general Advanced authoring, implement Content Host G7, or make 0.5.5 releasable.

## Evidence Reviewed

- The consolidated migration authority passes both its nine-case mutation self-test and an exact `-Check` against implementation ref `4bc05990668e219409f1d08be4485e67aa34add6`; mandatory ProductNative additions are zero and known residue is empty.
- The canonical SDK package, entry, manifest and policy hashes are identical across the cited L0-L5 evidence.
- L0-L3 from the unchanged-package ladder, formal L4-only and L5-only, all eight behavior profiles, and all three Manager lifecycle phases pass. Every cited parent root records process cleanup, source/profile/save restoration and Runtime-lock release.
- Catalog, the synthetic retained-ABI fixture and the locally subscribed exact retained AutoFishing binary pass focused checks. The retained binary has zero public API removals and `StopOnManualMove` MemberRef/resolution `1/1`.
- The compatibility smoke proves selected Workshop item `3743799721` loads and completes configure/enable/disable. Exact-byte identity is supplied by the retained ABI authority, not by a hash field in that smoke root.
- The implementation Update records a clean detached-worktree complete Release pass at `4da7f97d`. That run did not retain a standalone terminal transcript or machine result file, so the historical exit code is an implementation attestation rather than independently reconstructable repository evidence. The exact commit and command are recorded, focused authorities were independently rechecked, and no relevant path has changed; this limitation does not justify another full run or a new receipt system.

## Corrections Made During Acceptance

The owning Update now distinguishes the audited pre-migration source ref `9fb8d87d...` from the later `ff0f5fc3` commit that freezes the snapshot artifact, calls the 21-row set a Workshop snapshot/query union rather than 21 products, and separates retained exact-byte authority from the current selected-item smoke.

## Non-Blocking Finding

`tools/scripts/run-batch6-autofishing-gc-ladder.ps1` writes a successful plan status before its final restore/lock-release block. If that final block were to fail, the command would exit non-zero but the plan could still say `completed`; `RuntimeLockRelease` would be the only local warning. All evidence roots accepted here completed final restoration and lock release, so this ordering defect does not invalidate them. Repair it with the existing focused runner test when that script is next changed; do not reopen the pilot or rerun the game matrix solely for this P2.

## Validation Performed

- `tools/scripts/test-batch6-autofishing-gc-ladder.ps1`: passed, including artifact validation, complete PlanOnly and single-level `L4` PlanOnly.
- `tools/scripts/test-batch6-autofishing-behavior-matrix.ps1`: passed.
- `tools/scripts/test-batch6-autofishing-manager-lifecycle.ps1`: passed.
- `tools/scripts/build-batch6-autofishing-migration-evidence.ps1 -SelfTest`: passed, nine mutations.
- Exact migration-evidence `-Check` at implementation ref `4bc05990668e219409f1d08be4485e67aa34add6`: passed.
- `tools/scripts/check-product-catalog.ps1 -Quiet`: passed.
- Synthetic and locally subscribed retained AutoFishing ABI checks: passed.
- `tools/scripts/check-doc-governance.ps1` and scoped `git diff --check`: passed.
- Complete Release and game smoke were intentionally not rerun; the accepted product/Runtime/package inputs did not change.
