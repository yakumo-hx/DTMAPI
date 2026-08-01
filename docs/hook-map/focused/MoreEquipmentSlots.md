# MoreEquipmentSlots Protected Equipment

Status: `implemented/acceptance-open; production mail outcome-unknown transaction corrected in source, replacement bytes and independent acceptance pending`

## Native Boundary

- Game build: `23762374_public_C416D4`.
- Exact ProductNative owner:
  `dtmapi.mod.dtmapi.moreequipmentslotsmod`.
- Atomic four-target set:
  `AgentEquipmentManager.ReloadParams()` Postfix,
  `BodyController.OnAttacked(float,bool,Vector2,out bool)` Prefix,
  `AccessoriesBar.__Init()` Postfix and
  `AccessoriesBar.OnStartShow()` Postfix.
- The product owns exactly three extra-slot records, protected
  prepared/committed journal state, clone/listener lifecycle and product
  configuration.
- Native equipment functions, backpack/mail storage, vanilla shield priority,
  native `hatItem` appearance and the base AccessoriesBar remain game-owned.

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

## Rollback

Revert the ProductNative/Catalog/Host switch as one unit while preserving the
frozen public ABI and exact retained consumer. Never delete protected sidecars
or journals during rollback; keep cold recovery available until every prepared
record reaches a terminal reconciled state.
