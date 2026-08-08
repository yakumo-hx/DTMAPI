# MoreEquipmentSlots Transition Fix And Test Audit

Date: 2026-07-30
Status: `recorded / production P0=0 P1=0 / acceptance-and-release-truth P1=4 / publication blocked`
Audited HEAD: `d450314ac6adc9b6e8ab9dab012d963661fb7ad2`
Audited commits: `6751157c`, `fa3283e6`, `d450314a`
Owning implementation Updates:

- [`20260723-0008`](../../../updates/2026/20260723-0008-more-equipment-slots-eighth-advanced-product.md)
- [`20260727-0001`](../../../updates/2026/20260727-0001-dtmapi-055-prerelease-route.md)

## Request And Scope

The user requested an independent review of the latest MoreEquipmentSlots fixes
and tests, and a decision on whether publication or further Mod compatibility
testing may begin.

This audit inspects the permanent-winner test correction, the replacement
Runtime package evidence, the U1/Prepare/U3Backpack/U3Mail/U4 QA route, the five
captured game runs, save isolation and current release/status documents.

It does not change production code, install another Runtime, start Doloc Town,
run a complete Release, L0-L5, GC or a long test. Existing player evidence is
reviewed rather than repeated.

## What Is Valid

### Permanent-winner causality is now specific

Commit `6751157c` no longer lets an arbitrary `InvalidDataException` satisfy the
late-loser child process. The child requires the exact permanent-winner
rejection reason, and the parent also verifies the winner, winning Product and
absence of global/loser claim authority. This closes the P2 test-causality
finding in Review `20260730-0008`; it does not modify production bytes.

### The replacement Runtime package evidence is internally consistent

The frozen test candidate under
`dist/prerelease-055-moreequipment-transition-candidate/DTMAPI` records
`BuildCommit=6751157c2d42`, 30 files and 71,552,124 bytes. Its manifest,
mandatory Runtime assemblies, optional Compatibility Host and Doctor identities
match the owning Update.

The player-like package audit
`tmp/test-runs/DTMAPI Workshop Audit 20260730-100109` reports no blocker.
PowerShell 5.1 parses all ten packaged scripts; missing, empty, valid install,
status, log collection and uninstall cases have the expected exit behavior.
The package root contains exactly the four numbered BAT entry points plus the
required payload.

### The five game runs are real and correctly isolated

The following evidence directories exist and report `RunStatus=Passed`:

| Phase | Evidence | Save classification |
| --- | --- | --- |
| U1 | `GAME-SMOKE/20260730-110543` | `NativeSaveExpected` |
| Prepare | `GAME-SMOKE/20260730-115306` | `NativeSaveExpected` |
| U3Backpack | `GAME-SMOKE/20260730-115439` | `NativeSaveExpected` |
| U3Mail | `GAME-SMOKE/20260730-115548` | `NativeSaveExpected` |
| U4 | `GAME-SMOKE/20260730-115656` | `NoNativeSave` |

The save phases use a real `SleepUiState` and runner-owned real Enter input,
then observe exactly one `SaveSaved`. They run on disposable save fixtures
isolated from Steam AutoCloud. U4 proves current/prev/bak archives and both
committed sidecar files have unchanged length, hash and mtime before cleanup.
No routine player-save backup, player archive writeback or post-run restoration
is used as acceptance evidence.

Logs show the intended old-consumer and cold-Host routes, ProductNative absence
in U3/U4, backpack recovery, mail fallback, title return and process exit. These
facts provide useful partial compatibility evidence and reveal no new
production-code fault.

## Findings

### P1-1: U1's native-item count assertion is self-referential

In
[`MoreEquipmentSlotsTransitionFixtureCase.cs`](../../../../src/DTMAPI.GameBridge.DolocTown.QA/Scenarios/Fixtures/MoreEquipmentSlotsTransitionFixtureCase.cs),
the U1 post-save path at lines 294-310 reads the current backpack and mail
counts and passes those same values as the expected counts to
`VerifyMoreEquipmentSlotsTransitionTerminal`.

Therefore any number of native duplicates passes. The run still proves the old
API, three slots, two applied entries, shield durability and sidecar shape, but
it does not prove U1's required per-item exactly-once invariant.

Use fixed phase expectations or deltas from a captured zero-item baseline. For
U1, both target items should remain in active committed slots and have zero
copies in native backpack and unaccepted mail.

### P1-2: the cross-domain conservation oracle omits shield mail and per-item sidecar identity

The terminal verifier at lines 377-443 counts:

- `box_hat` in the backpack;
- `grandmas_button` in the backpack;
- `grandmas_button` in unaccepted mail.

It never counts `box_hat` in unaccepted mail. A terminal state containing one
backpack shield plus a duplicate shield mail therefore passes U3/U4.

The sidecar verifier at lines 506-558 adds occurrences of both target item IDs
and compares only the combined occupied count. It does not establish the
identity-specific conservation equation required by the transition plan:

```text
backpack(i) + unaccepted-mail(i) + active-sidecar(i) = 1
```

The existing fixture should assert both item IDs independently in every domain:

- U1: shield sidecar `1`, button sidecar `1`, all native target counts `0`;
- U3Backpack: shield backpack `1`, button sidecar `1`, all other target counts
  `0`;
- U3Mail and U4: shield backpack `1`, button mail `1`, sidecar target counts
  `0`, all other target counts `0`.

This is an acceptance-oracle correction, not evidence of a production
duplication bug.

### P1-3: U1 does not bind the actual Workshop source to the frozen retained artifact

`run-game-smoke.ps1` requires only enabled ID `Workshop.3744059735` for U1.
The run proves that one old consumer assembly loaded from that subscription
root and reports version `0.3.1-dtmapi`, but it does not preflight the Catalog's
retained artifact identity:

```text
files = 9
bytes = 539565
treeSha256 = e0854cee94969d98b916a3f6085fd03773c67bcd35c7bc83dc2894f8156e0ca6
```

The evidence's published-product artifact/combination gates are not requested.
Steam keeps the same Workshop ID across updates, so ID plus displayed version is
not exact-byte provenance. The durable wording “exact retained 0.3.1” exceeds
the current gate.

Add a read-only U1 preflight against the existing Catalog retained-artifact
authority. Do not create another receipt family.

### P1-4: the claim crash/resume player gate was silently omitted from the latest remaining-gate summary

The MoreEquipmentSlots Update at lines 1564-1577 explicitly says the focused
permanent-winner test does not close the broader claim crash/resume player gate.
Commit `fa3283e6` adds only U1, Prepare, U3Backpack, U3Mail and U4.

The later Follow-up at lines 1687-1692 and the active smoke row list only C0 and
the migrated-save gameplay spot check as remaining Product gates. That omission
can incorrectly authorize R2.

This gate must receive one explicit disposition before more test
infrastructure is added:

1. retain it and define one bounded player scenario with a precise failure
   boundary and acceptance result; or
2. formally retire it, explaining why the existing cross-process/file
   transaction tests are the authoritative evidence.

It must not disappear through summary wording alone.

### P2-1: current status authorities contradict the new evidence

The current-status line, current MoreEquipmentSlots section and summary line in
[`batch6-managed-mod-identity-contract.md`](../../../architecture/batch6-managed-mod-identity-contract.md)
still say U1/U3/U4 are open and RC preparation remains invalidated by them.
The top current-boundary section of Update `20260723-0008` says the same, while
later Update and smoke evidence says those runs passed.

The Batch 6 contract is the canonical current product/evidence authority.
Correct only its current-truth projection and the Update's current summary;
keep historical pre-run paragraphs unchanged. Until the P1 acceptance-oracle
repairs are rerun, describe U1/U3/U4 as executed with partial evidence, not
fully accepted or wholly unrun.

The monthly Update row also still says independent source reacceptance and
bounded player transition acceptance are both open even though source
reacceptance is closed and player execution is partial.

### P2-2: several durable descriptions overstate or blur the executed scope

- U1 did not execute an in-process Loader-disable route. Evidence proves title
  cleanup and process exit; wording such as “title/Loader cleanup” should be
  narrowed unless a focused deactivation gate is added.
- U3Backpack reached `equipmentJournals=0` and
  `equipmentGameplayCandidates=0`, with one committed button in the sidecar.
  “terminal journal/candidate” should say those transaction objects are absent.
- [`0.5.5-workshop-update-copy.md`](../../../releases/0.5.5-workshop-update-copy.md)
  still calls local player-like Workshop package acceptance pending. That local
  audit passed; complete Release, post-upload subscription verification and
  manual three-language paste/save/readback remain pending.

These wording issues do not invalidate the underlying captured files.

## Independent Focused Validation

Run against audited HEAD without starting the game:

```text
Release build: DTMAPI.UnitTests = PASS (10 existing DebugConsole nullable warnings)
Release build: DTMAPI.QaUnitTests = PASS (0 warnings, 0 errors)
DTMAPI_UNIT_TEST_FOCUS=moreequipment-product = PASS
DTMAPI.QaUnitTests = PASS
test-game-smoke-save-modes.ps1 = PASS
check-product-catalog.ps1 = PASS (27 / 11 / 22 / 48)
check-doc-governance.ps1 = PASS (6031)
check-test-artifact-governance.ps1 = PASS
git diff --check = PASS
Runtime lock = free
DolocTown.exe = absent
```

## Verdict And Minimal Next Route

The latest production corrections remain source/package accepted; this audit
finds no new production P0/P1/P2. The newly captured game runs are genuine and
valuable, but the U1/U3/U4 acceptance claim is not yet reliable because the
item-conservation oracle can pass duplicate states and the exact old Workshop
tree is not bound.

Publication is not authorized. Catalog `releaseStop=Active`,
`ExistingWorkshopUpdate` remains blocked and MoreEquipmentSlots remains
`RebuildBlocked`; a green test does not implicitly remove those authorities.

The smallest next route is:

1. repair the existing item-conservation assertions and add the read-only
   retained Workshop tree preflight;
2. rerun U1 and the smallest fresh Prepare -> U3Backpack -> U3Mail -> U4 chain
   only; do not run a complete Release for this correction;
3. reconcile current-truth documents and explicitly retain or retire the
   claim crash/resume player gate;
4. run C0 and the one migrated-save no-save/save semantic spot check before
   MoreEquipmentSlots R2;
5. freeze the final Product only after those gates pass.

For Runtime R0, the replacement candidate may continue through other focused
compatibility work. It must not be published until the acceptance-oracle
correction is resolved, one clean complete Release runs against the exact
current candidate, and the explicit release authority is changed. Steam upload
and post-upload subscription/manual-language verification remain separate
stages.
