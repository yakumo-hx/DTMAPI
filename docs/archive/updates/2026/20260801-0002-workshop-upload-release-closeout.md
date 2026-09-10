# 0.5.5 与功能 Mod 上传目录发布收口

- Update ID: `20260801-0002`
- Date: `2026-08-01`
- Lifecycle Status: `verified`
- Validation Level: `source, unit, runtime, player`
- Runtime Validation: `passed`
- Related Issue State: `closed`
- Area: `release/workshop/upload/manual-acceptance/runtime/0.5.5/advanced-products/rollback`
- Source Request: inspect the final hand-test logs, mark the accepted candidate publishable, restore Steam subscriptions to their pre-test state, and place the exact tested Runtime and Mod bytes in the official upload directory
- Owning implementation Update: [20260801-0001 Zoom Z1、DebugConsole 与 D4 输入所有权修正](20260801-0001-zoom-debugconsole-d4-correction.md)
- Root-cause Review: [20260801-0001 Zoom、Y 键控制台与 D4 输入所有权复核](../../reviews/manual-qa/2026/20260801-0001-zoom-debugconsole-d4-input-review.md)

## Result

The exact player-tested Runtime plus ten product packages are **authorized for
their existing Workshop item updates**. This is a byte-bounded authorization,
not a general Advanced authoring or upload lane:

- new Workshop items, mass version edits, UniqueID moves, WorkshopID moves and
  official-folder moves remain blocked;
- only the eleven `publicMutationEntrypoints` frozen in the Product Catalog are
  authorized;
- MoreEquipmentSlots ProductNative `1.0.0` remains publication-deferred.
  Workshop `3744059735` stays on the exact retained `0.3.1-dtmapi` package;
- no Steam upload was performed by this Update. The local official upload
  directories are ready for the user's upload action.

## Final player acceptance

The user accepted the final focused matrix in this order:

1. selecting DebugConsole movement `2x`, `3x` and `4x` remained effective after
   closing the Y console;
2. selecting `1x` restored normal movement while preserving the native movement
   Buff;
3. opening and closing the official Mod page once produced no new DTMAPI
   warning.

The two latest DTMAPI sessions corroborate that verdict:

- `latest-20260801-073139100.log`, `15:28:45–15:31:39`, contains 885 lines and
  307,722 bytes: 4 Debug, 881 Info, 0 Warning and 0 Error/Fatal;
- `latest.log`, `15:32:44–15:34:08`, contains 425 Info lines and 145,622 bytes:
  0 Warning and 0 Error/Fatal;
- the matching current/previous Unity player logs and current BepInEx log
  contain zero warning, error, exception, crash, assertion, stack-overflow or
  out-of-memory records;
- the official page path recorded exactly one open preview, one close
  candidate, one successful native save/close commit and one deferred DTMAPI
  refresh. No resource-ledger or lifecycle warning followed it;
- both DTMAPI logs total 1,310 lines / 453,344 bytes over 257 seconds, about
  5.1 lines and 1.7 KiB per second. The most repeated exact messages are the
  user's Y press/release observations (25 each), not an idle or failure loop.

## Exact accepted candidate

Before restoring Steam-managed directories, the twelve actually loaded trees
were copied read-only to:

`E:/Python_project/DTMAPI-retained-artifacts/release-candidates/20260801-d389da0f-manual-pass`

The archive contains 130 package files, 74,393,945 bytes and zero reparse
points, plus `candidate.json` and `SHA256SUMS`. Runtime provenance is
`d389da0fe89b`; the documentation-only `56fa284f` commit did not alter package
bytes. The accepted publication set is Runtime plus:

- AutoFishing;
- ActionSpeed;
- ManboCardboardAudio;
- FishBreedingAssistant;
- AnimalHusbandryProgress;
- Zoom;
- DebugConsole;
- MoreSaves;
- OneActionComplete;
- ChestLocatorEnhancer.

All ten product Catalog `sourceRoot` directories exist. Nine Advanced packages
pass their SDK reference-receipt, manifest, entry-DLL, tracked policy,
`netstandard2.0` and game-build `23762374` hash checks. Manbo remains the
existing legacy-compatible product lane and its projected manifest plus exact
tested DLL are source-linked. The retained MoreEquipmentSlots `0.3.1` binary is
deliberately not represented as rebuildable current ProductNative source; it is
excluded from this publication authorization rather than being rebuilt from the
new migration implementation.

## Subscription rollback

The actual Steam subscription root was restored to the pre-test read-only
snapshot at:

`E:/Python_project/DTMAPI-retained-artifacts/subscriptions/workshop-2285550-20260731-004038`

Post-restore verification covers the complete root, not only the selected
items: 44 directories, 4,897 files, 78,638,015 bytes, zero reparse points and
every per-file SHA-256 in the retained `SHA256SUMS` passes. The retained backup
tree authority is
`0644A44850D29C9F11E9F6AAB147ACD81F78AA4A9F9AF9F934700B2EAEB176AD`.
No test-candidate directory remains under the subscription root.

## Official upload directories

The official local upload root is:

`C:/Users/Administrator/AppData/LocalLow/RedSawGames/DolocTown/MODS`

Its original 23-package tree was restored first. Runtime and the ten authorized
products were then replaced by the exact accepted candidate bytes; only each
original upload folder's `workshop.json` was byte-preserved. The original
MoreEquipmentSlots upload folder remains unchanged at `0.3.1-dtmapi`.

The Product Catalog now retains the global stop while declaring eleven exact
existing-item exceptions, each bound to WorkshopID, version, official folder
and `DTMAPI-Retained-SHA256SUMS-v1` tree SHA-256. This prevents the release
decision from authorizing MoreEquipmentSlots `1.0.0`, a thirteenth product or a
different rebuild.

## Validation

- final manual player matrix: PASS;
- DTMAPI/BepInEx/Unity log audit: PASS, zero issue records;
- accepted-candidate copy and per-file parity: PASS;
- complete 44-item subscription rollback SHA-256 verification: PASS;
- upload/candidate exact parity excluding byte-preserved `workshop.json`: PASS
  for all eleven authorized items;
- nine Advanced package receipts and one legacy Manbo source projection: PASS;
- Runtime local-upload player package matrix under Windows PowerShell
  `5.1.26100.8875`: PASS, 0 blockers. Evidence:
  `temp/20260801-release-closeout/runtime-upload-matrix/DTMAPI Workshop Audit 20260801-155026/Results/stress-summary.md`;
- Product Catalog enforcement passes under PowerShell 7 and Windows PowerShell
  5.1; document governance passes 6,196 checks, test-artifact governance passes,
  and the Batch 4 semantic-boundary gate passes;
- upload/subscription evidence:
  `temp/20260801-release-closeout/evidence/release-state-audit.json`,
  `product-upload-receipt-audit.json` and `upload-tree-authority.json`;
- a final read-only audit at `2026-08-01 16:06 +08:00` reconfirmed all eleven
  upload trees and the restored subscription root after documentation changes;
- the current source/unit suite and focused Product/Catalog gates were already
  green before the manual run. The complete Release suite was not repeated
  after the final narrow corrections, as explicitly selected for this hand-test
  route; authorization is therefore limited to these frozen, manually accepted
  trees.

## Changed files and external state

- `tools/release/dtmapi-product-catalog.json` — exact existing-item release
  exceptions and tree hashes;
- `tools/scripts/check-product-catalog.ps1` — machine enforcement for the exact
  exception set and MoreEquipmentSlots exclusion;
- this Update, the owning/prerelease Updates, Batch 6 contract, Hook/API maps,
  release-copy checklist, issue records and monthly ledger — final status and
  evidence links;
- Steam subscription root — restored to its exact pre-test snapshot;
- official local `MODS` root — restored and populated with the accepted upload
  trees.
- shared Runtime environment — `DolocTown.exe` absent and the worktree-owned
  Runtime lock released after the final read-only audit.

## Rollback

- The accepted tested trees remain under the retained release-candidate path.
- The full pre-test subscription tree remains under the retained subscription
  backup with per-file hashes.
- The original local upload tree remains under the manual-test `Original/MODS`
  snapshot.
- Reverting this Update's Catalog/checker commit removes upload authorization;
  it does not delete any retained package.

## Follow-up

- Steam publication itself remains a user action.
- MoreEquipmentSlots `1.0.0`, Mine artwork/presentation, Content Host G7 and a
  general Advanced authoring lane remain outside this release.
- The next Runtime version owns the deferred ContentQuery, lifecycle and logging
  redesign work already recorded by Update `20260731-0006`.

## Post-closeout compatibility validation

The later player test of old Runtime with the frozen new Mod candidates, followed
by a Runtime-only upgrade to `0.5.5` while retaining MoreEquipmentSlots
`0.3.1-dtmapi`, is recorded in
[Manual QA Review 20260801-0002](../../reviews/manual-qa/2026/20260801-0002-old-runtime-newmods-upgrade-compatibility.md).
The first pre-isolation run is explicitly excluded because disabled OfficialLocal
duplicates shadowed enabled Workshop packages. The valid Workshop-only rerun and
the Runtime-only upgrade passed their bounded goals. A post-test audit also found
and corrected one 90-byte `info.json` upload-tree residue before reconfirming the
complete subscription, upload, Runtime and profile authorities.

The later Steam publication delivered the official-game-normalized Runtime
metadata. Its exact post-publication authority is recorded separately by
[Update 20260801-0003](20260801-0003-runtime-published-metadata-authority.md),
without rewriting this Update's pre-upload authorization or player-test facts.
