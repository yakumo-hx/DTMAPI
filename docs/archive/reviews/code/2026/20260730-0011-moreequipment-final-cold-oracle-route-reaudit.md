# MoreEquipmentSlots Final Cold Oracle Route Reaudit

Date: 2026-07-30
Status: `recorded / production P0=0 P1=0 P2=0 / current acceptance closed / QA-route P2=1 / publication blocked`
Audited HEAD: `94ab5ff510b097d6814c150f4d39a7932c617dbc`
Audited commits: `523fa6df`, `94ab5ff5`

Owning implementation Updates:

- [`20260723-0008`](../../../updates/2026/20260723-0008-more-equipment-slots-eighth-advanced-product.md)
- [`20260727-0001`](../../../updates/2026/20260727-0001-dtmapi-055-prerelease-route.md)

Prior audit:

- [`20260730-0010`](20260730-0010-moreequipment-transition-release-closeout-reaudit.md)

## Scope

The user requested an independent review of the correction and evidence shown
for the final MoreEquipmentSlots migrated cold observation. This review checks
commit `523fa6df`, `GAME-SMOKE/20260730-161355`, the documentation/provenance
closeout in `94ab5ff5`, and the existing generic cold-observer contract.

This review did not install a Runtime, launch Doloc Town, run a complete
Release, L0-L5, GC or a long test.

## Accepted Result

The previous final-cold mail omission is closed for the executed migrated
two-item state.

`GAME-SMOKE/20260730-161355` uses QA DLL SHA-256
`5D3F8A8345C72ED91500B754752A7BD512A8B14316CB279B223A379EC2408EF8`
at 1,039,872 bytes and the `523fa6df` runner. It proves:

```text
box_hat          = 0 backpack + 0 unaccepted mail + 1 sidecar = 1
grandmas_button  = 0 backpack + 0 unaccepted mail + 1 sidecar = 1
```

Working equals Committed at generation 11, shield durability is 25/80, and
dirty/journal/candidate are absent. The run is `NoNativeSave`; current, prev
and bak archives plus the committed sidecar and `.previous` retain identical
length, SHA-256 and mtime before cleanup. No routine player-save backup,
writeback or restoration occurred. Title return, QA cleanup, profile and Author
source restoration, process exit and the fatal-window check passed.

Mine and StrongPlantingGun were also loaded in the process, so this is not a
claim that only MoreEquipmentSlots existed in the process. Neither consumes the
two target items, pending mail or the EquipmentSlots owner; the four
MoreEquipmentSlots Hooks, callback and owner are checked separately. This does
not invalidate the bounded cold result.

Commit `94ab5ff5` also closes the prior documentation provenance defect:

- Reviews `0009` and `0010` are tracked;
- the evidence allowlist contains both Reviews and passes from clean HEAD;
- C0 is described as a targeted title-startup rejection, not a generic PASS or
  save-load/title-return run;
- the three QA generations are recorded separately;
- the `f96c9cc6` artifact/complete Release plus later source/QA/document
  closeout is stated without pretending clean `f96c9cc6` was self-contained.

No Runtime or Product production source, packaged Runtime bytes or complete
Release orchestrator changed. The existing complete Release does not need to
be repeated for this accepted bounded correction.

## Finding

### P2-1: the generic cold observer was specialized to the final two-item state

The cold observer accepts exact prior-state inputs:

- expected backpack baseline;
- expected committed generation;
- expected committed occupied count;
- expected committed slot description.

It was previously used for several legitimate Product states. Commit
`523fa6df` now unconditionally requires both target-item logical totals to be
one, and the runner unconditionally requires both items to be
`0 backpack + 0 mail + 1 sidecar`.

That is correct for `161355`, but it rejects other valid inputs which the
settings and routing contracts still accept:

- `GAME-SMOKE/20260724-202146` has one button in the backpack and an empty
  Committed document, so the expected shield total is zero;
- `GAME-SMOKE/20260724-222516` has three buttons in the backpack and only one
  shield in the sidecar;
- the active QA Unit/routing fixture accepts backpack baseline two and a
  one-button committed document.

The routing-only tests remain green because they do not execute the reflected
fixture or the final log matcher. A real rerun of those valid shapes would now
fail before evaluating their intended no-save persistence boundary.

This is a QA reuse regression, not a production fault and not a false PASS for
`161355`. It does not reopen the accepted migrated two-item evidence or require
another game run.

The smallest correction must not add another mode, receipt or gate. Derive the
expected totals from the existing inputs:

```text
expected button total =
    ExpectedMoreEquipmentSlotsBackpackBaseline
    + expectedCommittedButtonCount

expected shield total =
    expectedCommittedShieldCount

expected pending mail for this read-only Product observer = 0 for both items
```

The runner should validate the same dynamic distribution instead of a fixed
two-sidecar pattern. Cover the existing empty-Committed, shield-only and
two-item projections in the focused source/routing test. While editing the
same helper, an unreadable archive/farm/email/emails chain should fail closed
instead of being reported as zero mail; historical U3Mail positive evidence
means this robustness tail does not invalidate `161355`.

After that source-only correction, QA build, QA Unit and the save-mode focused
test are sufficient. Do not rerun the game, complete Release, GC or a ladder.

## Independent Focused Validation

The following passed at audited HEAD:

```text
DTMAPI.GameBridge.DolocTown.QA Release build: PASS
DTMAPI.QaUnitTests Release build/run: PASS
DTMAPI.UnitTests Release build: PASS (10 existing DebugConsole nullable warnings)
DTMAPI_UNIT_TEST_FOCUS=moreequipment-product: PASS
test-game-smoke-save-modes.ps1: PASS
test-batch4-qa-semantic-inventory.ps1: PASS
check-product-catalog.ps1: PASS (27 / 11 / 22 / 48)
build-evidence-retention-allowlist.ps1 -Check: PASS (488 / 896 / 62 / 19)
check-doc-governance.ps1: PASS (6031)
check-test-artifact-governance.ps1: PASS
git diff --check: PASS
```

The retained Workshop `3744059735` tree was independently read with hidden
entries enabled and still equals nine files, 539,565 bytes, tree SHA-256
`e0854cee94969d98b916a3f6085fd03773c67bcd35c7bc83dc2894f8156e0ca6`,
with zero hidden entries and zero reparse points.

## Verdict

The screenshot's substantive claim about `161355` is correct: the prior
final-cold acceptance P1 is closed, and no new player Runtime or Product bug
was found. The broader `P2=0` claim is not correct because the same change
regressed the reusable generic cold-observer route.

This P2 does not require another complete Release and does not block the
already frozen Runtime 0.5.5 candidate. It should be corrected before the
MoreEquipmentSlots QA route is reused or the product's later Workshop update.
Steam publication remains independently unauthorized because Catalog
`releaseStop` is active and MoreEquipmentSlots remains `RebuildBlocked`.
This audit Review is currently untracked; include it and the refreshed existing
evidence allowlist in the eventual correction commit before another allowlist
acceptance or full Release run.

## Resolution

Resolved after the bounded follow-up and accepted by
[Review `20260730-0013`](20260730-0013-moreequipment-mail-observer-final-acceptance.md);
the current lifecycle result is owned by
[Update `20260723-0008`](../../../updates/2026/20260723-0008-more-equipment-slots-eighth-advanced-product.md).
