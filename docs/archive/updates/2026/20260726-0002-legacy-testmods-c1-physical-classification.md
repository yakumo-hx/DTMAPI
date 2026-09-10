# Legacy testmods C1 Physical Classification

## Metadata

- Update ID: `20260726-0002`
- Date: `2026-07-26`
- Lifecycle Status: `verified`
- Validation Level: `docs,source,unit`
- Runtime Validation: `not-required`
- Related Issue State: `closed`

## Source Request And Authority

Complete the physical classification of legacy `testmods` content at the C1
endpoint without expanding the player Runtime.

Review `docs/reviews/code/2026/20260713-0001-major-update-second-decision-docket.md`
remains authoritative: C2 was the migration process, while the final physical
endpoint is C1. The Catalog remains the role and release authority. Physical
placement does not publish a Mod, grant Advanced admission, or move product
logic into mandatory Runtime.

## Implemented Boundary

The legacy root is gone. Its tracked contents now have one physical role:

| Legacy source | C1 endpoint | Role |
| --- | --- | --- |
| `DebugConsoleMod`, `ManboCardboardAudioMod`, `MineMod`, `OilMod` | `products/first-party/*` | published product or bounded prototype, as recorded by Catalog |
| `AutoHarvestMod` | `author-sdk/samples/api-demand/AutoHarvest` | `ApiDemandSample` |
| `ConfigMenuExample`, `HelloDtmMod` | `author-sdk/examples/*` | `Example` |
| `CropHarvestingQaMod`, `HookProbeMod`, `DTMAPI.AdvancedFixture` | `tests/mod-fixtures/qa/*` | `QaFixture` |
| `BrokenManifestMod` | `tests/mod-fixtures/negative/BrokenManifest` | `NegativeFixture` |
| AnimalHusbandry/FishBreeding artwork and MoreSaves legacy data | `archive/legacy-product-assets/*` | non-buildable audit/reference assets |

Build, developer-install, official-local, Workshop-package, Catalog,
publish-text, Batch 4 semantic and Batch 6 contract paths now use the classified
roots. Release definitions must declare `SourceRoot`; the old implicit
`testmods/<Project>` fallback fails closed.

`check-product-catalog.ps1` now enforces the role-to-root mapping and requires
the physical `testmods` root to remain absent. Historical Phase 0 and G2
receipts keep their original `testmods/...` observations in explicit baseline
fields; current source scans do not treat those paths as live authority.

## Player Runtime Boundary

No file under `src/` changed and no product, sample, example, QA fixture,
negative fixture, or archived asset entered the player Runtime definition.
The generated Runtime-only Workshop candidate still contains exactly five DLLs
under `BepInEx/plugins/DTMAPI`:

```text
DTMAPI.Abstractions.dll
DTMAPI.BepInExBootstrap.dll
DTMAPI.Core.dll
DTMAPI.GameBridge.DolocTown.dll
DTMAPI.ModConfigMenu.dll
```

The existing optional Compatibility component remains outside that mandatory
five-DLL root. Package eligibility and the eleven-product published set are
unchanged.

## Changed Files

- classified source roots under `products/first-party/`,
  `author-sdk/examples/`, `author-sdk/samples/api-demand/`,
  `tests/mod-fixtures/`, and `archive/legacy-product-assets/`;
- project references for the moved buildable Mods;
- `tools/release/dtmapi-product-catalog.json`,
  `tools/release/dtmapi-mod-publish-zh.json`, current Batch 4/6 contracts and
  semantic inventory;
- build, install, package, Catalog, audit-package and focused Batch 6 scripts;
- role-root README files;
- this Update and `docs/updates/INDEX-2026-07.md`.

## Validation

Passed on the implementation candidate:

- `tools/scripts/check-product-catalog.ps1`: `products=27`, `public=11`,
  `workshop-items=21`, `api-rows=48`;
- `tools/scripts/test-batch6-phase0-contract.ps1`: 23 domains, 115 historical
  Batch 5 files and the Phase 0 receipt reproduce exactly while live consumer
  roots point at the C1 destinations;
- `tools/scripts/test-batch4-qa-semantic-inventory.ps1`: all 12 semantic
  meta-negative cases, the Catalog projection negative case and six receipt-set
  validation cases pass;
- `tools/scripts/build.ps1 -Release -SkipTests`: every Runtime/tooling project
  and every moved buildable Mod/fixture builds with zero warnings and errors;
- Runtime-only Workshop staging plus Catalog package audit passes, and the
  mandatory plugin root contains exactly the five DLLs listed above.

The independent Batch 4 semantic boundary runner reached an existing
`DolocTownGameBridge.Hooks.cs` lifecycle-order drift involving
`animalFullInfoDataPatched`; this task changes neither that source nor its
inventory token order, so it is recorded as an unrelated broad-gate failure.

The G2 receipt and ownership candidate generated successfully. Its pre-commit
run correctly rejected the deliberately dirty
`test-batch6-g2-advanced-synthetic.ps1` authority file. The post-commit
clean-authority rerun passed the nine-case runtime receipt, exact ownership
receipt and complete synthetic contract while keeping all non-admitted products
blocked.

No game launch or native save was required for source/path classification.

### 2026-07-26 audit correction

The post-commit audit found two current-authority leaks that the original C1
validation did not exercise:

- active Unit cases for AutoHarvest, DebugConsole, Oil and CropHarvesting still
  opened deleted `testmods/...` paths;
- `tests/README.md` still directed new test assets to `testmods`, while a
  release-tool copy with stale paths remained physically under
  `tools/release/`.

The active Unit inputs now use their classified sample, product and QA-fixture
roots. The Compatibility Host fixture is staged explicitly before the three
camera compatibility cases, so its temporary missing-manifest failure no longer
masks later path assertions. `tests/README.md` now routes generated QA/negative
fixtures to `tests/mod-fixtures` and samples to `author-sdk`. The release copy
was removed from the live tool tree and replaced by a non-authoritative archive
receipt under `archive/release-projections`; its exact old bytes remain
recoverable by the recorded Git blob rather than by a second live projection.

Final correction validation passed:

- `DTMAPI.UnitTests` complete Release run;
- `check-product-catalog.ps1`, including physical C1 endpoints and the
  historical-exclusion projection;
- `check-test-artifact-governance.ps1 -RunCleanupFixture`.

The managed Unit session was cleaned by the tracked artifact cleaner. No game
launch or player Runtime expansion was required for these C1 corrections.

## Rollback

Revert the physical moves, Catalog roots/digest, explicit release `SourceRoot`
projections, script paths, role-root guards and this record together. Do not
restore only the legacy directory or reintroduce an implicit
`testmods/<Project>` package fallback.

## Follow-Up

Preserve historical receipt paths as baseline facts and keep all future source
roots role-classified.
