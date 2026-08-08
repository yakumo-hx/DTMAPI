# MoreEquipmentSlots Transition And Release Closeout Reaudit

Date: 2026-07-30
Status: `recorded / production P0=0 P1=0 / acceptance P1=1 / release-provenance P1=1 / publication blocked`
Audited HEAD: `be21b033789e1d75c6ec3467a285ed10f29a15b8`
Audited commits: `3a77b93c`, `96b95c8e`, `f96c9cc6`, `be21b033`

Owning implementation Updates:

- [`20260723-0008`](../../../updates/2026/20260723-0008-more-equipment-slots-eighth-advanced-product.md)
- [`20260727-0001`](../../../updates/2026/20260727-0001-dtmapi-055-prerelease-route.md)

Prior audit:

- [`20260730-0009`](20260730-0009-moreequipment-transition-fix-and-test-audit.md)

## Scope

The user requested an independent review of the new MoreEquipmentSlots fixes
and tests. This review checks the corrected U1/U3/U4 oracle, retained Workshop
provenance, C0, the migrated no-save/save/cold chain, final candidate records
and complete Release evidence.

This is a read-only source/evidence audit apart from this Review. It does not
change production code, install a Runtime, start Doloc Town, rerun a game
matrix, run a complete Release, L0-L5, GC or a long test.

## Accepted Corrections

The four P1 findings in Review `20260730-0009` were materially addressed:

- U1 now uses fixed expected locations instead of feeding observed counts back
  as expectations.
- The transition terminal oracle counts `box_hat` and `grandmas_button`
  independently across backpack, unaccepted mail and active sidecar, and
  requires each three-domain sum to equal one.
- U1 preflights Workshop `3744059735` before QA staging or profile mutation and
  binds nine files, 539,565 bytes and tree SHA-256
  `e0854cee94969d98b916a3f6085fd03773c67bcd35c7bc83dc2894f8156e0ca6`.
- The omitted player claim-kill gate is explicitly withdrawn rather than
  silently counted as passed. Deterministic cross-process fault tests own the
  filesystem transaction windows; game runs own Unity and native-save
  integration.

The corrected U1
[`132349`](../../../debug/evidence/GAME-SMOKE/20260730-132349/), Prepare
[`132150`](../../../debug/evidence/GAME-SMOKE/20260730-132150/), U3Backpack
[`132527`](../../../debug/evidence/GAME-SMOKE/20260730-132527/), U3Mail
[`132623`](../../../debug/evidence/GAME-SMOKE/20260730-132623/) and U4
[`132723`](../../../debug/evidence/GAME-SMOKE/20260730-132723/) evidence is
genuine. The runs use disposable,
Steam-AutoCloud-isolated fixtures. U4 is a valid `NoNativeSave` run and proves
the selected archives and committed sidecars unchanged before cleanup. No
routine live-save backup or writeback is used as acceptance evidence.

The migrated chain also proves useful real behavior:

- `134155` creates the migrated Product-v3 baseline;
- `134315` damages Working state without advancing Committed state;
- `134412` cold-loads the prior committed durability;
- `134949` uses real damage, native `SleepUiState`/Enter and exactly one
  `SaveSaved` to commit the new durability;
- [`135057`](../../../debug/evidence/GAME-SMOKE/20260730-135057/) cold-loads
  that committed document under `NoNativeSave`.

C0 `133359` contains the expected old-Runtime warning and no Product entry:
Runtime `0.5.2-alpha` rejects a Product requiring `>=0.5.5`, exits, and leaves
the protected archives and committed sidecars unchanged. Its generic
`RunStatus=Failed` is not a current-Runtime health PASS and must remain
described as a targeted negative/manual acceptance.

No production Runtime or Product source changed in the audited commit range.
The new changes are QA, release-contract tests and documentation. No new
production-code P0/P1/P2 was found.

## Findings

### P1-1: the final migrated cold-start oracle omits unaccepted mail

`MoreEquipmentSlotsNoNativeSaveColdObserver` verifies the committed and Working
Product documents and counts both target items in the native backpack. It does
not count either target item in unaccepted mail.

The preceding `134949` process proves that both items were each present exactly
once immediately after the native save. It cannot prove that the subsequent
cold load did not also produce a native-mail or backpack duplicate. Therefore
`135057` proves committed durability and transaction cleanup, but not the
durable statement that both items remain exactly once across all three domains
after cold startup.

This is an acceptance-oracle gap, not evidence of a production duplication
bug. Reuse the existing per-item equation:

```text
backpack(i) + unaccepted-mail(i) + active-sidecar(i) = 1
```

for both `box_hat` and `grandmas_button` in the cold observer, then rerun only
the final `NoNativeSave` cold observation against the already committed
disposable fixture. U1, Prepare, U3, the no-save damage run, the normal-save
run, the complete Release suite and game ladders do not need repetition.

### P1-2: the final Release documentation gate is not reproducible from clean `f96c9cc6`

Review `20260730-0009` is still untracked, but committed Updates and smoke
records link to it and the committed evidence-retention allowlist includes it
among its 486 source files.

The allowlist builder intentionally enumerates both tracked and unignored
Markdown with:

```text
git ls-files --cached --others --exclude-standard
```

Consequently the successful complete Release actually tested
`f96c9cc6 + untracked Review 0009`. A clean checkout of `f96c9cc6` lacks that
file while its committed allowlist still names it, so the allowlist `-Check`
cannot pass from that commit alone.

This does not invalidate the candidate Runtime bytes or the other complete
Release gates: the same read-only Review content was present and scanned
during the run, and later tracked changes are documentation-only. The
proportional correction is:

1. retain Review `0009` unchanged as the recorded historical audit and include
   it together with this new audit;
2. rebuild the existing evidence allowlist so it includes both Reviews, commit
   the two Reviews and allowlist together, then run the allowlist check,
   document governance and `git diff --check` from the resulting clean tree;
3. describe the authority as the frozen `f96c9cc6` artifact/complete Release
   plus the later documentation-only closeout, rather than as a clean
   `f96c9cc6` checkout being self-contained.

No Runtime refreeze or complete Release rerun is required unless production
source, packaged bytes, the release runner, or another release-contract input
changes.

### P2-1: captured QA provenance is described too broadly

The corrected U1/U3/U4 runs used QA assembly SHA-256
`DF0A93078B63AAE6B5EA69C4BEF013C9B041FAF10640F30BD9ACA12A8C3A69B2`
at 1,035,264 bytes. `134949` and `135057` used
`979E2156E4537AB0F1CC80BCD2EDDFB2037EF7EA8D4DF147405875F8272A7465`
at 1,038,848 bytes after the migrated-save phase was added.

The evidence remains usable, but the route Update's claim that the corrected
transition and migrated-save runs all used one unchanged QA/runner working tree
is false. Record the two fixture generations separately; do not rerun the game
for this wording correction.

### P2-2: current-truth and C0 wording still contain historical drift

- The Batch 6 contract still contains an unqualified older paragraph saying
  MoreEquipmentSlots remains open for migration/cold acceptance, while its
  current summary says the product is verified/closed.
- The MoreEquipmentSlots Update's section titled `Current correction boundary`
  still projects `implemented/open/partial` and all player gates open, despite
  its verified/closed metadata and later corrected-acceptance section.
- C0 did not load a save and return to title. It remained on the title flow,
  proved startup-time minimum-version rejection, and exited.
- The C0 generic aggregate is `Failed`; only the bounded negative expectation
  is accepted. A future runner edit may add a small expected-rejection result
  field, but another C0 game run is not required for the present manually
  inspected evidence.

Correct the current projection and wording without rewriting historical
snapshots.

### P2-3: two small runner diagnostics remain

- The generic Workshop tree enumerator lacks `-Force` and does not reject
  reparse points as strictly as the freeze tool. A separate `-Force` inspection
  of the actual retained subscription still finds the expected nine files and
  539,565 bytes, so existing U1 evidence is not invalidated.
- The invalid transition-phase error text omits the allowed `MigratedSave`
  value.

These can be repaired with the next focused runner/QA edit and require only
source checks.

## Focused Validation

Independent checks against the audited tree passed:

```text
Release build: DTMAPI.UnitTests = PASS
Release build: DTMAPI.GameBridge.DolocTown.QA = PASS
Release build: DTMAPI.QaUnitTests = PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-product = PASS
DTMAPI.QaUnitTests = PASS
test-game-smoke-save-modes.ps1 = PASS
test-batch4-qa-semantic-inventory.ps1 = PASS
check-product-catalog.ps1 = PASS (27 / 11 / 22 / 48)
check-doc-governance.ps1 = PASS (6031)
check-test-artifact-governance.ps1 = PASS
build-evidence-retention-allowlist.ps1 -Check = PASS before this Review was added,
  and only with untracked Review 0009 present
git diff --check = PASS
```

The retained Workshop directory was independently enumerated with hidden files
included and still matched the frozen identity. No game process was started by
this review.

## Verdict

The new fixes are effective and the corrected U1/U3/U4 evidence remains
accepted. C0 and the migrated no-save/save sequence reveal no production
fault. MoreEquipmentSlots must nevertheless return to
`implemented / acceptance-open` until the final cold-start per-item oracle and
one bounded cold rerun pass.

The Runtime candidate does not need another complete Release merely for these
QA/document corrections. Publication is still not authorized:
Catalog `releaseStop` is `Active`, MoreEquipmentSlots is
`RebuildBlocked`, Review `0009` is untracked, and the final migrated cold-start
claim is incomplete.

The minimal route is:

1. repair the cold observer plus the two small diagnostics;
2. run focused build/unit/save-mode checks and only one final migrated
   `NoNativeSave` cold observation;
3. reconcile the provenance/current-truth wording, rebuild the existing
   allowlist, and commit Reviews `0009`/`0010` plus that allowlist;
4. run clean allowlist/document/diff checks;
5. independently reaccept that bounded result, then change Catalog release
   authority in the separately authorized publication step.
