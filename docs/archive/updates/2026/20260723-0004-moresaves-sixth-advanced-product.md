# MoreSaves Sixth Advanced Product

## Metadata

- Update ID: `20260723-0004`
- Date: `2026-07-23`
- Lifecycle Status: `verified`
- Validation Level: `docs,source,unit,runtime,player`
- Runtime Validation: `passed`
- Related Issue State: `closed`

## Source Request And Authority

After the separately committed admission prerequisites, implement and commit
MoreSaves as the sixth independent Advanced ProductNative mod. The implementation
must preserve `DTMAPI.MoreSavesMod`, Workshop item `3742763050`, the canonical
config path, the fixed twelve/six behavior and native save ownership. It must
install zero Harmony patches, keep the frozen `ISaveSlotsApi` ABI in the one
dormant-shipped Compatibility Host, and pass only focused checks plus one short
third-save acceptance. MoreEquipmentSlots remains a separate candidate.

The admission authority is
[`20260723-0002`](../../reviews/code/2026/20260723-0002-moresaves-sixth-product-admission-review.md);
the closed prerequisites are
[`20260723-0003`](20260723-0003-moresaves-admission-prerequisites.md).
Commit-range audit
[`20260723-0003`](../../reviews/code/2026/20260723-0003-sixth-product-commit-range-audit.md)
temporarily returned this Update to `implemented/open`; the focused correction
below closes those findings without replacing the retained `150215` game
evidence.

先做本轮 API/domain 的 native owner 方法体审查；未找到 native owner 或状态持有者前，不得通过 mod 层补丁冒充 API 重做完成。

Build `23762374` confirms that `GameManager.archiveFileCount` is the only
product-written state. `LocalSave`, `DataPersistenceManager`,
`GameDataUiState` and `GameDataPanel` remain the official file, discovery,
mutation and UI owners.

## Implemented Boundary

- `products/first-party/MoreSaves` is an SDK-built `netstandard2.0` Advanced
  CodeMod with preserved UniqueID, Workshop/package identity and canonical
  config. Its policy references only exact build `23762374`
  `Assembly-CSharp`; it has no Harmony reference and reports zero installed
  patches.
- ProductNative owns the fixed enabled `12` / disabled `6` policy, the direct
  `archiveFileCount` write/readback, a 750ms missing-manager retry, save/title
  reconciliation, configuration and final owner restoration.
- The internal one-writer coordinator rejects Product-first and
  Compatibility-first conflicts. A failed final native-six restoration retains
  the exact product lease and reports the failure instead of exposing a second
  writer. Because Loader removes owner events during deactivation, it waits for
  restart or a later explicit cleanup pass rather than claiming an automatic
  post-deactivation retry.
- Healthy MoreSaves has no `UpdateTicked` listener. Missing-manager work
  demand-activates the listener only while the owner remains active and removes
  it immediately after success; this avoids permanent Core event-argument
  allocation. Its status reports pending work separately from a live retry
  scheduler, so a retained final-cleanup failure does not imply automatic event
  delivery.
- The mandatory GameBridge deletes the historical save-panel paging/Unity UI
  body and Show/Select callbacks. Its `SaveSlotsService` is now a thin frozen
  ABI proxy that activates the existing Compatibility Host only on an old ABI
  call; compatibility installs no save UI Hook.
- Catalog, release definitions, SDK policy registry and focused product checks
  now use the existing Catalog-driven Advanced builder. The SDK compiler path
  accepts a tracked Advanced policy without `0Harmony`, which is required for
  a genuine zero-Hook product.
- The QA owner-deactivation seam recognizes MoreSaves, observes native twelve
  and zero real Harmony-owner patches before Loader deactivation, then requires
  native six, zero owner roots and zero real patches afterward.
- The default-loaded SaveSlots feature/proxy boundary is now 136 physical /
  111 non-empty lines, down from 887 / 781 at the admitted implementation
  baseline: 751 physical / 670 non-empty lines removed from default load.
  Because the frozen executor remains in the optional Host and the product DLL
  is separately shipped, this is not a download-package or total-shipped-size
  reduction claim.

## Changed Files

- `products/first-party/MoreSaves/*`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `author-sdk/advanced-reference-policies/doloctown-23762374-moresaves-v1*`
- `author-sdk/advanced-reference-policies/registry.json`
- `author-sdk/compatibility/0.5.5/compatibility.contract.json`
- `src/DTMAPI.AuthorSdk/AdvancedCompilationReferences.cs`
- `src/DTMAPI.GameBridge.DolocTown/CompatibilityHost/SaveSlotsServiceProxy.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/SaveSlots/*`
- `src/DTMAPI.GameBridge.DolocTown/Compatibility/SaveSlots/*`
- `src/DTMAPI.GameBridge.DolocTown.QA/*`
- `tests/DTMAPI.UnitTests/MoreSavesProductTests.cs`
- `tools/release/dtmapi-product-catalog.json`
- `tools/release/contracts/protected-behavior-contracts.json`
- `tools/release/contracts/batch6-phase0-domain-contract.json`
- `tools/release/dtmapi-mod-publish-zh.json`
- `tools/release/batch4-production-qa-semantic-inventory.json`
- `tools/scripts/build-batch6-phase0-baseline.ps1`
- `tools/scripts/build-batch6-moresaves-advanced-pilot.ps1`
- `tools/scripts/check-release-contract.ps1`
- `tools/scripts/test-batch6-moresaves-advanced-product.ps1`
- `tools/scripts/test-batch6-phase0-contract.ps1`
- existing generic build/install/smoke projections and this Update

The correction also treats Catalog `sourceVersion`, retained
`publishedVersion`, and migration `targetVersion` as independent axes. Current
publish text mirrors source `1.0.0`; the retained published baseline remains
`0.3.1-dtmapi`. The unrelated ChestLocator publish projection is restored to
its current `0.3.1-dtmapi` source.

## Validation

Completed:

- focused MoreSaves ProductNative Unit: PASS;
- mandatory Runtime and optional QA builds: PASS, zero warnings;
- Catalog live zero-leftover/source-root projection: PASS;
- MoreSaves source/identity/native-owner check: PASS;
- Catalog-driven Author SDK validate/build/pack: PASS;
- candidate package:
  `temp/batch6-more-saves-advanced-pilot/DTMAPI-MoreSaves-advanced-pilot.zip`,
  SHA-256
  `306E8D10C814D0A61B36A631354BDCADDD0091780886F32BC8C0741F47D01D99`.
- independent final review: PASS after one P2 evidence correction. The review
  found that `141154` did not reopen the official panel after title recovery and
  did not prove byte preservation for all twelve archive indexes. It found no
  P0/P1 identity, ownership, lifecycle, package or compatibility defect.
- the correction makes the combined MoreSaves/real-owner-deactivation QA route
  require a second post-title official-panel receipt. It also snapshots all
  archive indexes `0..11`, including each main/`-prev`/`-bak` path, restores the
  exact baseline and fails closed if protected extra indexes `6..11` changed
  before restoration.
- non-acceptance `GAME-SMOKE/20260723-144659` exposed one QA-only ordering bug:
  `MoreSavesPostTitlePanel` was not classified as a post-title requirement, so
  the participant never requested ReturnHome after SaveLoaded. The hung exact
  process was stopped; all 36 save paths already matched their baseline hashes,
  profile/source state was restored, QA staging was removed, and the shared
  runtime lock was released. The post-title classification now has a focused
  regression check.
- authoritative save-slot-3 current-DLL acceptance:
  `docs/debug/evidence/GAME-SMOKE/20260723-150215`, PASS. It loaded
  `DTMAPI.MoreSavesMod` with HookProbe, verified the initial official
  twelve-slot panel, loaded slot index `2`, returned to a continuous title,
  reopened the official panel and again observed exactly twelve slots, then
  exercised real Loader owner deactivation:
  `native12+actual0 -> instance0+native6+actual0+roots0`.
- both panel screenshots, title-button lifecycle, save-load boundary, owner
  cleanup, QA cleanup, official Mod profile restore, Author SDK source-state
  restore and final no-`DolocTown.exe` process checks: PASS.
- player data protection: 36/36 archive paths restored exactly; all 18 main,
  `-prev` and `-bak` paths for protected extra indexes `6..11` were already
  byte-identical before restoration.
- post-audit focused MoreSaves and Compatibility Host Unit: PASS, including
  unregistered/cold-disabled/mixed frozen states, demand-driven retry removal,
  and final-deactivation lease retention with later explicit cleanup.
- post-audit Catalog-driven primary/repeat product packages are deterministic:
  MoreSaves SHA-256
  `5E9BC4A2B4995520DCEB9669AF1A9C0C24C41BDEB56F4B1FB3E0B91105062EA8`.
- Install Doctor, Status negative topology case, Catalog, Phase 0 contract,
  release contract and Runtime transaction matrix on both PowerShell hosts
  (17 cases each): PASS.
- document governance: the MoreSaves records/index are coherent; the workspace
  gate remains red only because the separately untracked portable-capture
  Update `20260720-0006` is intentionally not registered in the monthly ledger.
- no additional game run was required; none was performed. `150215` remains
  authoritative because these corrections do not change successful native
  owner/deactivation behavior.

No complete Release, L0-L5, GC ladder or long test is authorized or planned.

## Rollback

Revert this Update as one unit and restore the old Catalog source projection.
Do not remove the frozen `ISaveSlotsApi` ABI, retained consumer evidence,
Compatibility Host executor or its one-writer guard. Rollback must not delete
any `doloc-archive-{n}.data` file or rewrite player selection/config state.

## Follow-up

The sixth-product baseline is frozen as `verified / runtime-smoke / closed`.
MoreEquipmentSlots remains a separate candidate and is neither admitted nor
implemented by this authority. A seventh product remains blocked until the
reordered Phase 1/4 tails receive their own bounded disposition.
