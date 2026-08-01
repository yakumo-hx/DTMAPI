# MoreEquipmentSlots Generic Cold Oracle Fix Reaudit

Date: 2026-07-30
Status: `recorded / implementation accepted with one residual QA P2 / no P0 or P1 / publication blocked`
Audited HEAD: `bb14f75bfe30971873f0d8b6529fe734b9033018`
Audited commits: `c2c215e4`, `bb14f75b`

Owning implementation Update:

- [`20260723-0008`](../../../updates/2026/20260723-0008-more-equipment-slots-eighth-advanced-product.md)

Prior audit:

- [`20260730-0011`](20260730-0011-moreequipment-final-cold-oracle-route-reaudit.md)

## Scope

The user requested an independent review of the correction shown for the
MoreEquipmentSlots generic cold-observer route. This review checks the C# QA
oracle, PowerShell runner matcher, focused tests, Update/provenance closeout and
whether the prior QA-route P2 is fully closed.

This audit did not install or freeze a Runtime, launch Doloc Town, run a
complete Release, L0-L5, GC or a long test.

## Accepted Correction

Commit `c2c215e4` correctly removes the fixed two-item oracle:

- the expected button total is the supplied backpack baseline plus the number
  of `grandmas_button` entries in the expected Committed slot description;
- the expected shield total is the number of `box_hat` entries in that
  description;
- the observer compares the actual backpack, mail and sidecar distribution
  against those values;
- the runner derives the same sidecar and total values instead of matching
  both items as fixed `0 backpack + 0 mail + 1 sidecar`;
- QA Unit directly executes the shared helper for an empty Committed document
  with a nonzero backpack baseline, a shield-only document and the accepted
  two-item document. These are executable assertions, not only source-text
  scans.

The C# and PowerShell slot-description parsers use the same item-id field,
ordinal case-sensitive comparison and multiplicity rule. No current mismatch
was found between the two implementations.

The existing `GAME-SMOKE/20260730-161355` result remains valid evidence for its
executed two-item state. This source-only generalization neither changes nor
reinterprets that retained evidence.

Commit `bb14f75b` truthfully records that Runtime/Product source, frozen
candidate bytes and save data did not change, and that no game or complete
Release run was performed. Catalog still blocks existing Workshop updates and
MoreEquipmentSlots remains `RebuildBlocked`; the commit does not authorize
publication.

## Findings

### P2-1: unreadable pending-mail authority still fails open as zero

Review `20260730-0011` required the same correction to stop treating an
unreadable native mail chain as an observed zero. That tail was not
implemented.

`CountPendingMoreEquipmentSlotsMail` still returns zero when any of these
authorities cannot be read:

- `archiveHandle`;
- `farmData`;
- `emailManager`;
- `emails`.

The cold observer then accepts that value as proof that pending mail is empty.
A native field drift or failed reflection can therefore be reported as
`mailButton=0` / `mailShield=0` instead of failing the QA run.

This is a QA evidence-integrity defect, not a Runtime or Product gameplay bug.
It does not invalidate `161355`, because that run and prior positive mail
evidence exercised the current native chain. It does prevent the reusable QA
route from being described as P2-clean.

The smallest correction is to throw when the authority chain is unreadable and
return zero only after successfully enumerating the native mail collection.
Add independent negative checks for unreadable mail authority, nonzero button
mail and nonzero shield mail. The present negative helper case combines
nonzero button mail with a duplicate logical total, so it does not isolate both
failure reasons.

### P2-2: independent-acceptance lifecycle order was not preserved

The owning Update remained `verified` and `closed` while `c2c215e4` was waiting
for this requested independent audit. The active governance rule requires an
Update to remain `implemented` until a planned independent acceptance passes.

This is a workflow-order defect, not a code or release-evidence defect. Because
this audit still found P2-1, the QA correction is not yet independently closed.
The next correction should keep the Update at `implemented` until its focused
re-audit accepts the final state.

### P3-1: the prior Review lacks a short resolution link

Review `20260730-0011` correctly preserves the state of its audited
`94ab5ff5` HEAD, including `QA-route P2=1` and the fact that the Review was then
untracked. After `c2c215e4`, those historical statements are easy to read as
current truth.

Do not rewrite the historical analysis. A short resolution link from Review
`0011` to the owning Update and this re-audit is sufficient after the remaining
P2 is closed.

The screenshot also assigns Review `0011` to `bb14f75b`, but the Review and
refreshed evidence allowlist were actually committed by `c2c215e4`;
`bb14f75b` changed only the owning Update.

## Independent Focused Validation

The following passed at audited HEAD:

```text
DTMAPI.GameBridge.DolocTown.QA Release build: PASS
DTMAPI.QaUnitTests Release build/run: PASS
DTMAPI Unit focuses:
  moreequipment-product: PASS
  moreequipment-cold-host: PASS
  moreequipment-acceptance-routing: PASS
test-game-smoke-save-modes.ps1: PASS
Windows PowerShell 5.1 ParseFile:
  run-game-smoke.ps1: PASS
  test-game-smoke-save-modes.ps1: PASS
test-batch4-qa-semantic-inventory.ps1: PASS
check-product-catalog.ps1: PASS (27 / 11 / 22 / 48)
build-evidence-retention-allowlist.ps1 -Check: PASS (489 / 896 / 62 / 19)
check-doc-governance.ps1: PASS (6031)
check-test-artifact-governance.ps1: PASS
git diff --check: PASS
```

The save-mode script executes the C# helper through QA Unit, but its runner
matcher assertion remains a source-shape check rather than an executable
parameterized matcher test. No error was found in the current runner
calculation. Extracting or exposing a small pure matcher builder for
parameterized no-game testing would strengthen this route without adding a new
receipt or gate.

The worktree was clean before this audit Review was created. This new Review
must be committed with the next focused correction and the existing evidence
allowlist refreshed then.

## Verdict

The main claim in the screenshot is substantially correct: the generic
empty-Committed, shield-only and two-item baseline calculations are fixed and
their focused tests are real. No P0/P1 or player-facing regression was found.

The broader claim that no P2 remains is not accepted. The native pending-mail
reader can still turn an unreadable authority into a false zero, and the
independent-acceptance lifecycle order was not followed.

Fix those bounded QA/document tails and rerun only the affected QA Unit,
save-mode, PowerShell parser, governance and allowlist checks. Do not launch the
game, refreeze Runtime/Product bytes or run a complete Release for this
correction.
