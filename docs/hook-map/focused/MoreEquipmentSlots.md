# MoreEquipmentSlots Protected Equipment

Status: `Branch B storage/recovery implemented and cold acceptance passed / current dynamic native-slot UI conflicts / Product 1.0 publication blocked pending UI correction`

## Native Boundary

- Current tracked game reference: `24456188_test_E861E0`.
- Exact ProductNative owner:
  `dtmapi.mod.dtmapi.moreequipmentslotsmod`.
- Atomic four-target set:
  `AgentEquipmentManager.ReloadParams()` Postfix,
  `AgentEquipmentManager.TryGetShieldItem(out IAgentEquipmentShieldItem)`
  Postfix,
  `AccessoriesBar.__Init()` Postfix and
  `AccessoriesBar.OnStartShow()` Postfix.
- The retained Compatibility owner continues to use the current
  `BodyController.OnAttacked(float,bool,Vector2,AttackProperties,out bool)`
  Prefix for frozen `0.3.1-dtmapi` consumers. It is not part of the Product
  four-target set.
- The product owns exactly three extra-slot records, protected
  prepared/committed journal state, clone/listener lifecycle and product
  configuration.
- Native equipment functions, backpack/mail storage, vanilla shield priority,
  the official `BodyController.OnAttacked` tail, native `hatItem` appearance
  and the base AccessoriesBar remain game-owned. The Product supplies one
  policy-limited typed `IAgentEquipmentShieldItem` adapter only when the
  official manager reports no native shield.

## Install And Lifecycle

- All four targets are pre-resolved and installed as one transaction. Any
  target failure rolls back the exact product owner; any residual exact or
  conflicting owner fails closed.
- Product-first and frozen-compatibility-first load orders both reconcile
  before installation. The compatibility owner is
  `dtmapi.gamebridge.doloctown.equipmentslots.compatibility`.
- Configuration disable, failed Entry and Loader deactivation independently
  attempt native-state restore, callback detach and exact-owner unpatch.
  Successful cleanup requires clone, listener, function, callback, Hook and
  product root counts all to reach zero.
- Save/title/environment boundaries clear or reconcile scoped state without
  claiming that the native save and sidecar are one atomic store.

## Storage And Compatibility

Official `TryPlaceInBackpack(..., true)` can send overflow mail and still
return `false`. The ProductNative path therefore performs and observes
backpack placement, explicit mail delivery and true failure as three distinct
outcomes. A save-identity-bound durable journal records prepared and committed
states across native save, committed sidecar and journal cleanup; ambiguous or
conflicting state remains fail-closed.

Production placement now requires readable backpack and mail authority before
the first native call. After a native call begins, unreadable, excess or mixed
count evidence is `NativeMutationOutcomeUnknown`, never ordinary failure.
Product gameplay and the legacy Host keep an in-memory no-retry guard; durable
owner/orphan recovery keeps its persisted attempted escrow. `SaveSaving`
requires the unchanged native-save fingerprint plus exact zero, backpack-only
or mail-only evidence before retaining or releasing sidecar authority. Title
may discard the in-process gameplay guard because native no-save rollback owns
that boundary.

The journal must distinguish ordinary `GameplayMutation` from explicit
`OwnerRecovery`/`OrphanRecovery`. Ordinary shield/equip/replace/unequip state
may become committed only after the matching native save succeeds, normally
through `SaveSaved` or after an interrupted notification window proves that
exact native commit; management recovery may persist and retry to prevent
protected items becoming inaccessible.

`IEquipmentSlotsApi` plus its six DTOs remain
Experimental/Deprecated/Frozen. The mandatory GameBridge retains a thin proxy
and demand route; the old arbitrary-owner `0..24` executor and cold recovery
live in the existing single Compatibility Host. The new fixed-three product
does not consume the frozen API. No SharedNative component is introduced.

## Evidence

- The current physical Harmony fixture resolves and installs both exact
  four-target owners. Product tests prove official native-shield priority,
  `ShieldDefend`, full block, exact depletion, residual/critical/Thunder,
  death/drone escape, nonfatal invincibility/fishing/hit/hitback and
  Working-only shield persistence. Retained-Host tests exercise the same
  current five-argument native tail, delegating positive residual damage to
  the official method and covering its bounded exact-depletion path.
- The current `moreequipment-product` focus passes. The Author SDK exact
  reference suite passes with both tracked builds available, and the
  Catalog-driven MoreEquipment builder produces an SDK-authored Advanced
  package from policy `doloctown-24456188-moreequipmentslots-v1`. The
  compiler surface exposes only empty `DolocAPI` plus the exact shield
  interface to this one product; unrelated native types/members and reuse by
  another Advanced policy are negative tests.
- Focused ProductNative and Compatibility Units cover the three placement
  outcomes, three canonical crash windows, fixed-three versus legacy `0..24`,
  both real-Harmony owner orders, residue, rollback, disable/restart, exact
  deactivation and zero-leftover lifecycle. The real Host fixture also covers
  ordinary Equip/Unequip, shield damage, SaveSaving/SaveSaved,
  title/cold-restart rollback, native-success-before-promotion, tombstone
  cleanup and typed management recovery.
- Commit `10e74ed6` adds production malformed-mail preflight, post-send
  unreadable evidence, SaveSaving rejection/no replay, exact `+1`
  reconciliation, Product and real-Host Working/durable recovery, Product
  no-save title rollback and owner-resource cleanup coverage. The
  `moreequipment-product`, `moreequipment-cold-host` and
  `moreequipment-acceptance-routing` focuses pass against that commit.
- Review `20260730-0015` keeps the product and affected Runtime candidate open.
  No replacement package, game run or complete Release was performed for this
  correction; all game evidence below remains scoped historical evidence for
  its exact earlier bytes.
- The current-head `moreequipment-product` and `compatibility-host` focused
  Units and Catalog check pass. The final source candidate also passed its
  focused Catalog, Phase 0, Author SDK/package, Doctor policy-set and QA route
  checks.
- The clean complete Release PASS belongs to the earlier `d38af17a`
  candidate. The corrected `034ea5e6` candidate ran from the beginning to the
  stale evidence-allowlist stop; after correcting the allowlist, the exact
  previously unreached tail passed. The complete suite was not rerun, and the
  original product admission did not require it.
- `GAME-SMOKE/20260724-053248` proved the final corrected enabled product:
  four exact Hooks, fixed-three behavior, one logical protected item, title
  recovery, real Loader deactivation to zero
  patches/callbacks/clones/listeners/functions/roots, current Doctor,
  save/config/source restoration and clean exit.
- That run did not grow the current native `passiveItems[]` collection and is
  not evidence for official accessory-slot progression. The current Unit
  `AccessoriesBar` fixture exposes only `__Init()` and `OnStartShow()`; it has
  no `slotRoot`, passive pool, selectable array or variable native-count model.
- `GAME-SMOKE/20260724-053342` proved product-disabled, exact
  ProductNative-v3 cold Host recovery with no product Entry, plus current
  Doctor, restoration and clean exit.
- `GAME-SMOKE/20260724-031003` disabled the receipt-bound product deployment,
  loaded the existing Host only after a protected orphan demand, recovered one
  item through the native backpack and restored the save/config/profile/source
  transaction with no remaining process. It is legacy-ABI orphan-recovery
  evidence, not the final ProductNative-v3 cold-recovery authority.
- Independent commit-range and final eight/nine closeout Reviews passed the
  physical ownership and tested lifecycle/recovery paths.
- `GAME-SMOKE/20260724-155216` and `155344` prove the third-save
  `NoNativeSave` equip/title/cold route: their evidence names the live archive
  paths, archive and committed-sidecar metadata were unchanged before cleanup,
  Working rolled back to Committed, and the green path created no backup or
  writeback.
- `GAME-SMOKE/20260724-161422` and `161536` pass the disposable
  Steam-AutoCloud-isolated native-save/promotion and cold-read contract with
  one logical item and no remaining journal/candidate.
- `GAME-SMOKE/20260724-202032` and `202146` close the real ProductNative
  no-save/cold matrix: replacement, damage, break, post-break equip/unequip,
  Working rollback to Committed and unchanged live archive/sidecar metadata
  before cleanup.
- `GAME-SMOKE/20260724-202526` closes the corrected disposable
  NativeSaveExpected matrix with two real `SaveSaved` boundaries, one logical
  item, empty committed product sidecar and journal, four exact targets,
  fixture isolation/cleanup and clean process exit.
- Final current-commit `GAME-SMOKE/20260724-222231` normally saves one
  durability-80 `box_hat` shield. `222329` performs real no-save damage to
  Working durability 40; `222423` first proves the prior rollback to 80, then
  performs unsaved unequip/re-equip, replacement/replacement-unequip and shield
  break; `222516` cold-loads the exact committed durability-80 shield with zero
  native copies, one committed/logical item and no candidate/journal. Archive
  and committed-sidecar metadata are unchanged before cleanup in every
  NoNativeSave run, and the disposable fixture is deleted successfully.
- `154906`, `155811` and `161030` remain non-acceptance diagnostics.

## Final Save-Commit Acceptance

Shield hit/break and ordinary equip/replace/unequip now mutate only Working
memory. `SaveSaving` may persist an explicitly uncommitted gameplay candidate,
but only successful native-save evidence promotes it to Committed. Title,
shutdown or cold restart without that evidence discards it. Explicit
OwnerRecovery/OrphanRecovery remains persistently retryable and separate.

This also removes the prior synchronous JSON plus `Flush(true)` work from the
shield-hit Prefix. Review `20260724-0006` closes after the real ProductNative
damage, break, replace and no-save unequip route passed, `NoNativeSave`
rejected both root overrides, and disposable fixtures rejected reparse
points. No complete Release, L0-L5, GC gradient or long test was run.

Review `20260724-0007` retained those covered paths but found the accepted
no-save pair degenerate because it began and ended with empty Committed state.
The `222231`/`222329`/`222423`/`222516` sequence supplies the required
non-empty proof and closes that finding without changing the fixed-three
ProductNative or legacy `0..24` Host boundaries.

## 2026-08-06 Dynamic Native-Slot UI Finding

Read-only inspection of the installed game build `24585411` confirms that the
official equipment manager now owns a variable `passiveItems[]` collection.
`add_accessory_slot` calls `SetPassiveSlotCount(current + 1)`, and
`AccessoriesBar.RenderPassiveItems` resizes an official `slotRoot` object pool
to that exact count. `allSelectablesArray` includes the native pool controls.

The Product's two UI Hooks still call `RenderAccessoriesBar`, which probes the
removed `passiveItem2/passiveItem1` fields, necessarily falls back to
`positiveItem`, and appends three clones under the top AccessoriesBar grid.
Those clones are neither children of the official pool nor members of
`allSelectablesArray`.

The current live Addressables layout was parsed read-only. Its root grid uses
`112`-pixel cells with `12`-pixel spacing. The official passive container takes
one root cell and internally uses the same horizontal spacing. When official
progression changes the native passive count from one to two, the second native
control and the first Product clone occupy the same horizontal position. The
geometry collision and missing Product navigation are therefore static current
findings, not hypothetical save corruption.

Native resizing does not overwrite the Product's three Product-v3 sidecar
records, so the completed save/no-save/cold-recovery work remains valid. The
new blocker is bounded to UI composition and focus/navigation: Product slots
must follow the variable native prefix through a layout-safe owner after native
pool rendering, without incrementing the official count or deleting the frozen
compatibility/recovery path. A focused `1 -> 2` native-count UI test and a
current-game UI acceptance are required before Product 1.0 publication.

## Relations

- Admission Review:
  `docs/reviews/code/2026/20260723-0009-eighth-product-more-equipment-slots-admission-review.md`.
- Conditional audit:
  `docs/reviews/code/2026/20260723-0010-eighth-product-admission-runtime-weight-and-api-reuse-audit.md`.
- Update:
  `docs/updates/2026/20260723-0008-more-equipment-slots-eighth-advanced-product.md`.
- Final eight/nine closeout:
  `docs/reviews/code/2026/20260724-0004-eighth-ninth-product-split-closeout-audit.md`.
- Save-commit regression:
  `docs/reviews/manual-qa/2026/20260724-0001-moreequipment-unsaved-save-commit-regression.md`.
- Independent closeout correction:
  `docs/reviews/code/2026/20260724-0006-moreequipment-zoom-closeout-audit.md`.
- Production mail transaction audit:
  `docs/reviews/code/2026/20260730-0015-moreequipment-production-mail-transaction-audit.md`.
- Current native slot-growth review:
  `docs/reviews/manual-qa/2026/20260806-0001-moreequipment-official-slot-growth-review.md`.

## Rollback

Revert the ProductNative/Catalog/Host switch as one unit while preserving the
frozen public ABI and exact retained consumer. Never delete protected sidecars
or journals during rollback; keep cold recovery available until every prepared
record reaches a terminal reconciled state.
