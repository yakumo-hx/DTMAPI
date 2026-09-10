# 20260804-0010 AutoFishing Fifth-Save Fixture Availability Review

- Date: 2026-08-04
- Status: `superseded / false fixture diagnosis withdrawn`
- Severity: no remaining fixture blocker; the failed preflight exposed a save-name resolver defect
- Owning Update: [`20260802-0001`](../../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)
- Failed matrix: `docs/debug/evidence/BATCH6-AUTOFISHING-BEHAVIOR-MATRIX/20260804-100952-70e21c42`
- Prior fifth-save authority: [`20260613-0005`](../../manual-qa/2026/20260613-0005-autofishing-native-loop-fifth-save-review.md)
- Current AutoFishing native review: [`20260804-0008`](20260804-0008-autofishing-100-native-body-and-af-d2-review.md)
- Superseding review: [`20260804-0001 MoreSaves 1.00 legacy-slot migration`](../../manual-qa/2026/20260804-0001-moresaves-100-legacy-slot-migration.md)
- Related issue: [`ISSUE-019`](../../../../debug/issues/ISSUE-019-20260804-moresaves-100-legacy-slot-migration.md)

## Supersession Correction

The failed matrix remains valid evidence that the smoke run stopped before a
game process was created, but this Review's conclusion about the player's
fifth-save fixture was wrong. The user confirmed, and a read-only filename
inventory verified, that Doloc Town 1.00 stores the first six current archives
as `doloc-save-0.data` through `doloc-save-5.data`; therefore UI #5/index 4 is
present as `doloc-save-4.data`, and the normal third-save fixture is present as
`doloc-save-2.data`.

`Get-DtmApiCurrentSaveArchiveFamilySnapshot` instead hard-coded the pre-1.00
name `ea-playtest-doloc-archive-{index}.data`. The matrix consequently asked
for an obsolete index-4 path and produced a false preflight blocker. The
read-only inventory in this Review saw only legacy indices 6 through 11
because it searched the wrong current-name family; it did not establish that
indices 0 through 5 were absent.

The local fifth-save blocker and all three resume conditions below are
withdrawn. They are retained only as the historical reasoning made from the
incorrect filename premise. The runner must resolve the current
`doloc-save-{index}.data` plus `.prevN/.bak` family before the formal
AutoFishing matrix is replayed.

## Scope

The first formal 0.6 AutoFishing behavior-matrix replay that reached the
`DefaultLoop` profile stopped before launching Doloc Town because the selected
current save archive was absent. This Review determines whether the runner may
map the fifth-save authority to another existing archive, use a backup, create
a save, or instead must preserve a local blocker.

It does not change a player save, inspect decrypted save contents, install or
modify a product, start the game, redefine the AutoFishing fixture, or claim a
game acceptance result.

## Observed Evidence

- The matrix was bound to clean candidate commit `dacf037a29e9`, Author SDK ZIP
  SHA-256 `6B6C5092...EF52C`, and AutoFishing package SHA-256
  `7A9C1A66...CA753`.
- SDK deployment succeeded as an `Advanced` CodeMod, so the preceding
  deployment-floor defect was closed for this path.
- `DefaultLoop/smoke-output.txt` stopped in the `NoNativeSave` preflight with:
  `The selected current save archive is missing; legacy backup names cannot
  satisfy NoNativeSave proof: ...archive-4.data`.
- Both per-profile process cleanup receipts and the matrix final cleanup receipt
  report no Doloc Town process. No profile reached game launch.
- The temporary Author SDK deployment was withdrawn, the exact recovery tree
  and journal were removed by the existing `AbsentNoJournal` restoration, the
  final status returned to `AbsentNoJournal`, and the Runtime lock was normally
  released.
- A read-only filename/length/mtime inventory found current archives only at
  indices 6 through 11. The exact index-4 family had zero current,
  `.data.prevN`, `.data.bak`, legacy `-prev.data`, or legacy `-bak.data` files.
  No save bytes were decrypted or modified for this inventory.

## Native Identity Facts

Current build `24456188_test_E861E0` preserves physical save-slot identity:

1. `LocalSave.GetAllArchiveInfo()` creates an array with
   `archiveFileCount` positions and calls `GetArchiveInfo(i)` for every exact
   index. Missing files remain null positions; existing saves are not compacted.
2. `GameDataUiState.GetDataIndexText(index)` renders `#(index + 1)` and
   `Show()` renders the complete array in that same order.
3. Selecting UI `#5` therefore sets `currentIndex=4`. `OnConfirm()` calls
   `Load(false, 4)` only when the exact index-4 archive info exists.
4. When index 4 is null, the official UI treats the slot as new and calls
   `DolocAPI.NewGame(4)`. It does not load the fifth non-empty archive.
5. The QA coordinator independently and intentionally translates human
   `SaveSlot` exactly once as `humanSlot - 1`; its existing tests lock this
   mapping.

The formal AutoFishing authority has repeatedly specified fifth save / UI #5 /
index 4. Existing index 10 may be the fifth currently non-empty file, but it is
UI #11 and is not the authorized fixture.

## Root Cause

The current player environment no longer contains the exact fifth-save fixture
that prior accepted evidence used. The new `NoNativeSave` preflight correctly
requires the current archive before a game process exists, because absence
cannot be proven safe by a legacy backup name and selecting the empty official
slot would enter a new-game/save-creation route.

This is not evidence that the AutoFishing product, Runtime, SDK deployment,
save-family comparison, or UI-to-index mapping is defective. It is a local
acceptance-fixture availability blocker.

## Rejected Actions And Hypotheses

- **Map `SaveSlot=5` to the fifth non-empty archive.** Rejected: native UI
  identity is positional and does not compact missing saves.
- **Use index 10 because it is the fifth current file.** Rejected: that is UI
  #11, has no fifth-save authority, and its fishing readiness was not inspected.
- **Use `.prevN`, `.bak`, `-prev.data`, or `-bak.data` without a current file.**
  Rejected by `NoNativeSave` and by native current-file load ownership.
- **Copy/rename another live archive to index 4.** Rejected: that mutates
  cloud-managed player save state, changes embedded archive identity, and is not
  accepted isolation or semantic evidence.
- **Launch UI #5 and let the game create the missing save.** Rejected for this
  run: it intentionally enters native new/save state and cannot be classified
  `NoNativeSave`.
- **Treat the preflight failure as a game/profile failure.** Rejected: no game
  process was created and no AutoFishing phase ran.

## Blocker And Resume Boundary

Formal fifth-save AutoFishing behavior/GC/Manager acceptance remains locally
blocked until one of these user-owned conditions is met:

1. the exact UI #5 / index-4 current archive is restored from a trusted player
   source; or
2. the user prepares a new fish-ready UI #5 through normal gameplay and a
   successful native save; or
3. the user explicitly changes the authoritative AutoFishing fixture through a
   new bounded decision, with corresponding Review/Update/test changes.

Codex must not synthesize condition 1 or 2 from another live save. When the
exact current archive exists, the same matrix must start from a clean committed
candidate, prove the complete current `.data` plus `.prevN/.bak` family
unchanged before any runner/external restoration, and then satisfy the existing
pond, selected rod, energy/spirit, phase, input, F6/F7, title/re-entry, cleanup,
and exit gates.

This blocker is local. MoreEquipment Branch B and this fifth-save availability
gate do not stop independent 0.6 source, package, retained-consumer, Manbo,
third-save, release-governance, or public-build conditional work.
