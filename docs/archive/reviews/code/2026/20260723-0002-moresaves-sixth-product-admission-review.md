# MoreSaves Sixth Product Admission Review

**Review ID:** `20260723-0002`

**Date:** 2026-07-23

**Status:** recorded — admission-time NO-GO until the listed prerequisites;
conditional GO only after those prerequisites close; this Review owns no
implementation or acceptance lifecycle
**Scope:** independent read-only admission decision for MoreSaves identity,
native owner, migration value and minimum acceptance; MoreEquipmentSlots remains
a separate candidate

## Source Request

After the Animal ProductNative refresh optimization and the G1 Oil/Mine
authority projection, decide whether MoreSaves may become the sixth Advanced
product. The Review must not bundle MoreEquipmentSlots or implement either
product before an explicit admission pass.

## Independent Verdict

**Admission-time atomic verdict: NO-GO until the blockers below close.**

MoreSaves is a strong next extraction candidate: its fixed-six/twelve policy has
one real consumer and a precise native state holder, and the fixed-twelve path
does not need a Harmony UI Hook. The current Runtime/Compatibility baseline is
not qualified to add another frozen executor, however. Therefore this Review
does not add an Advanced policy row, product assembly, package, receipt, Hook,
Catalog admission or game run.

An independent sub-agent source review reached the same verdict and the same
fixed-twelve no-Hook conclusion.

## Identity To Preserve

| Fact | Decision |
| --- | --- |
| Product identity | `DTMAPI.MoreSavesMod` |
| Workshop item | `3742763050` |
| Current public version | `0.3.1-dtmapi` |
| Future admitted target | `1.0.0`, minimum DTMAPI `0.5.5` |
| Canonical config | `DTMAPI/config/DTMAPI.MoreSavesMod.json` |
| Target product DLL | existing Catalog name `DTMAPI.MoreSaves.dll` |
| Canonical Harmony namespace | `dtmapi.mod.dtmapi.moresavesmod` |
| Product promise | fixed twelve when enabled, native six when disabled |

The UniqueID, Workshop item, config path and protected player data behavior must
not be renamed during migration. This Review does not invent an Advanced
manifest value, policy receipt or package. If the admission blockers close, the
existing tracked policy registry and Author SDK must generate those facts.

## Native Responsibility And Physical Owner

The current build `23762374` makes the native chain explicit:

```text
GameManager.archiveFileCount
  -> LocalSave.dataFileCount / GetAllArchiveInfo
  -> GameDataUiState.Show
  -> GameDataPanel.Render
  -> GameDataPanel.SetCapacity
```

`GameDataUiState.Show` already requests all archive rows and calls
`GameDataPanel.Render`. `GameDataPanel.Render` calls its native `SetCapacity`,
which derives the official layout capacity for twelve slots. The current
`GameDataUiState.Show` Postfix therefore adds no required fixed-twelve behavior:
for `slots.Length <= 12`, it only repeats official panel restoration. The
unreachable 18+/24+ pager, Unity button, navigation and child-visibility body is
historical QA/future residue, not a MoreSaves 1.0.0 requirement.

The conditional target is consequently:

- **ProductNative:** fixed 6/12 policy, writing and observing
  `archiveFileCount`, bounded retry while `gameManager` is unavailable,
  configuration, title/owner restoration and product status;
- **native game:** archive files and paths, discovery, create/save/load,
  duplicate/delete, metadata repair, UI rows and navigation;
- **Platform:** generic Loader, owner cleanup transaction, SDK/package,
  Catalog, Doctor/Manager and ordinary save lifecycle events;
- **Compatibility:** the frozen `ISaveSlotsApi` executor in the existing single
  dormant-shipped Compatibility Host;
- **SharedNative:** none. There is no second independent real archive-count
  product and no common owner with MoreEquipmentSlots.

A future fixed-twelve product should install **zero Harmony patches**. Naming,
configurable counts above twelve, scrolling and pager UI remain separate future
ProductNative reviews.

## Migration Value

The Phase 0 baseline assigns the current SaveSlots GameBridge feature two files
and 885 physical lines. The current product shell is 93 physical lines. Moving
the fixed policy and native write to the product, deleting the unnecessary
paging/UI body, and leaving only thin frozen-ABI activation/coordination can
remove most of those 885 lines from the default-loaded GameBridge.

This is a credible default-load reduction, not yet an accepted measurement. The
old `ISaveSlotsApi` executor must remain shipped for the 0.5.5 compatibility
window, so the existing optional Host will grow. No future Update may describe
this migration as a download-package or total-shipped-size reduction unless a
separate measured result proves it.

## Admission Blockers

### P1 — Existing Compatibility Host Is Not Yet A Safe Expansion Base

The current broker still calls `Assembly.Load(bytes)` before validating the
actual assembly simple name, AssemblyVersion and TargetFramework. A coherent
wrong receipt and payload can therefore leave rejected bytes resident in the
Mono AppDomain.

Install Doctor still accepts a freely supplied optional-component ID/path/name,
checks only that the path is under `DTMAPI/components`, and compares the actual
managed name and FileVersion without freezing the Catalog component identity,
actual AssemblyVersion and actual TargetFramework to the broker contract.

The focused Runtime upgrade transaction fixture still emits only
`IncludedAssemblies`; it does not move a real optional component and receipt
through commit, rollback, interrupted recovery and uninstall.

These are three existing Host acceptance gaps, not MoreSaves implementation
work. They must pass focused Host/Doctor/transaction checks before a sixth
product may expand the Host.

### P1 — SaveSlots Is Outside The Frozen Host Authority

The current retained-consumer hash/MemberRef authority and Host factory cover
exactly five migrated compatibility families. They do not cover
`ISaveSlotsApi`, `SaveSlotsOptions`, `SaveSlotsState` or the retained MoreSaves
Workshop DLL.

Before product migration, the existing authority must be extended in place to:

- bind the retained `DTMAPI.MoreSavesMod` artifact/hash and exact
  `ISaveSlotsApi` MemberRefs;
- preserve the provider ID `DTMAPI.GameBridge.DolocTown`;
- add a fixed-six/twelve executor to the existing single Host;
- retain Frozen/Experimental API warnings without deleting ABI.

Do not create a second Host, receipt schema, builder or checker family.

### P1 — The Global Native Writer Needs Both-Order Coordination

The product and frozen API executor would otherwise both write the same global
`archiveFileCount`. Product-first then old-ABI-call and old-ABI-call-first then
product must both fail closed or transfer one exact lease without allowing two
writers. Owner removal must not restore six while another admitted owner still
requires twelve.

Focused tests must cover both load orders, pending-manager restoration and
restore/unpatch exceptions. This coordination is a thin owner invariant, not a
reason to retain the full SaveSlots engine in mandatory Runtime.

## Minimum Future Acceptance

If a later correction closes every P1 and changes this Review to GO, the
smallest implementation acceptance is:

1. Focused source/Unit checks for preserved identity/config, SDK/policy/package,
   6/12 normalization, bounded missing-manager retry, zero Show/Select Hooks,
   both-order Product/Compatibility arbitration, exception-safe restoration,
   frozen Host demand and live zero-leftover checks for the old SaveSlots body.
2. Existing generic Catalog, package, Doctor/Manager and retained-ABI checks;
   no new product-specific toolchain family.
3. One short save-slot-3 game acceptance with the current Advanced DLL and
   HookProbe:
   - product identity accepted and actual Harmony patch count remains zero;
   - official title save panel shows twelve native slots;
   - the third save loads, returns to title and the panel still shows twelve;
   - real Loader owner deactivation restores native six and leaves no product,
     demand, callback or UI roots;
   - save files are byte-preserved, deployment/config/official selection is
     restored and the process exits cleanly.

That bounded migration acceptance does not replace the protected release matrix
for slots 7-12 create/save/reload/restart/copy/delete and non-destructive
disable/re-enable. No complete Release, L0-L5, GC ladder or long test is needed
for the bounded admission implementation.

## Separate MoreEquipmentSlots Candidate

MoreEquipmentSlots owns equipment sidecars, storage/UI clones and a different
native lifecycle. It has no common archive-count state holder, Hook or player
data boundary with MoreSaves. It remains an independent future admission and
must not be used to manufacture a second consumer or combined sixth-product
package.

## Disposition

- **Conditional GO:** only after a separate Update closes the existing Host
  preload/Doctor/transaction gaps, freezes the retained MoreSaves ABI consumer,
  adds the fixed-six/twelve executor to the one Compatibility Host, and proves
  both native-writer orders plus pending/restoration failure handling.
- **NO-GO:** MoreEquipmentSlots bundling, SharedNative save UI, a new Host,
  arbitrary-count API expansion, save naming/scrolling, 0.5.5 publication, or
  any download-size reduction claim from this Review.

## Later Resolution

This admission-time reasoning is intentionally frozen. Implementation and
acceptance outcomes belong to Updates
[`20260723-0003`](../../../updates/2026/20260723-0003-moresaves-admission-prerequisites.md)
and
[`20260723-0004`](../../../updates/2026/20260723-0004-moresaves-sixth-advanced-product.md).
The later commit-range findings and their focused closure are recorded in
Review
[`20260723-0003`](20260723-0003-sixth-product-commit-range-audit.md);
they do not rewrite this Review into an implementation ledger.

Related authorities:

- `tools/release/dtmapi-product-catalog.json`
- `tools/release/contracts/batch6-phase0-domain-contract.json`
- `tools/release/contracts/protected-behavior-contracts.json`
- `docs/reviews/api/2026/20260713-0002-saveslots-fixed12-product-boundary-review.md`
- `docs/reviews/api/2026/20260610-saveslots-native-responsibility.md`
- `docs/reviews/manual-qa/2026/20260713-0001-moresaves-long-term-player-baseline-review.md`
- `src/DTMAPI.GameBridge.DolocTown/Features/SaveSlots`
- `references/doloc-town/reverse/builds/23762374_public_C416D4`
