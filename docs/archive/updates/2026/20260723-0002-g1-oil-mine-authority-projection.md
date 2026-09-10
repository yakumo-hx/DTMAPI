# G1 Oil/Mine Authority Projection

## Metadata

- Update ID: `20260723-0002`
- Date: `2026-07-23`
- Lifecycle Status: `verified`
- Validation Level: `docs,source`
- Runtime Validation: `not-required`
- Related Issue State: `closed`

## Source Request

Project the existing G1 Review conclusion into current authorities:

- Oil remains a pure official-JSON ContentPack;
- Mine static content remains official JSON, while runtime cycle and economy
  belong to a future Mine ProductNative product;
- do not add a shared machine API, Content Host or other assembly.

This Update does not admit Mine or any sixth product, implement product code,
change prototype economy values, run the game or publish 0.5.5.

## Owning Review

- [Batch 6 G1 Unresolved Native Owner And Oil/Mine Design Review](../../reviews/api/2026/20260722-0002-batch6-g1-unresolved-native-owner-and-oil-mine-design-review.md)

## Projection

The existing `oil-drop` row remains `ContentPack` /
`content-only-decided`. Native official drop JSON owns the data and native
spawn chain; no managed Hook, cache, API or assembly is introduced.

The existing `mine-machine` row becomes `ProductNative+ContentOwner` /
`split-decided`:

- `ContentPack` owns static item, equipment, recipe and group rows;
- a future separately admitted Mine `Advanced CodeMod` owns scheduler, RNG,
  archive time, power/economy configuration, recipe/tech restoration,
  cache/renderer lifetime and all product policy;
- there is still one real consumer, so no Platform, SharedNative, Content Host
  or public machine-DTO boundary is justified.

The Update reuses the existing Phase 0 contract, checker and reproducible
ownership baseline receipt. It does not create another receipt or gate family.

## Changed Files

- `tools/release/contracts/batch6-phase0-domain-contract.json`
- `tools/scripts/test-batch6-phase0-contract.ps1`
- `tools/release/baselines/batch6-phase0-ownership-baseline-20260720.json`
- `docs/reviews/api/2026/20260722-0002-batch6-g1-unresolved-native-owner-and-oil-mine-design-review.md`
- `docs/architecture/batch6-managed-mod-identity-contract.md`
- `docs/planning/20260712-dtmapi-lightweight-functional-mod-roadmap.md`
- this Update and the July ledger

## Validation

- `test-batch6-phase0-contract.ps1`: PASS. The existing schema-2 ownership
  baseline reproduces all 23 domains and 115 Batch 5 classified files with Mine
  `split-decided` and Oil `content-only-decided`.
- `git diff --check`: PASS.
- Document governance/link validation is recorded with the final task checks.
  The unrelated user-owned portable-capture Update remains intentionally
  outside this Update and is not registered here.

Runtime validation is not required because this is an authority-only projection
with no Runtime, product package or content change.

## Rollback

Revert the Mine row, expected focused checker facts and regenerated existing
baseline receipt together. Oil remains pure official JSON either way. Do not
roll back by adding a host or by moving Mine policy into mandatory Runtime.

## Follow-up

The separate sixth-product admission Review may cite this decided owner map but
cannot treat Mine as admitted. MoreSaves and MoreEquipmentSlots remain separate
candidates.
