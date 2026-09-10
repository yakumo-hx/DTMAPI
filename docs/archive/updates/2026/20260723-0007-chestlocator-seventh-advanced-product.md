# ChestLocatorEnhancer Seventh Advanced Product

## Metadata

- Update ID: `20260723-0007`
- Date: `2026-07-23`
- Lifecycle Status: `verified`
- Validation Level: `docs,source,unit,runtime,player`
- Runtime Validation: `passed`
- Related Issue State: `closed`

## Source Request And Authority

After the separately committed Phase 1/4 closeout and seventh-product
admission, implement ChestLocatorEnhancer as the only admitted seventh
Advanced ProductNative mod. Preserve its product, Workshop, config and package
identity; move the frozen `IChestLocatorEnhancerApi` executor into the existing
single dormant Compatibility Host; prove both product/compatibility load
orders fail closed; and add no Host, receipt family, public API or SharedNative
capability.

Admission authority:
[`20260723-0005`](../../reviews/code/2026/20260723-0005-seventh-product-chestlocator-admission-review.md).
That Review admits no other product and does not itself claim implementation
or acceptance.

The native responsibility owner is
`ArchiveDataHandle.GetAvailableInventories(Vector2Int, Vector2Int, bool)`.
Authoritative item state stays in the native room/building equipment graph and
the `LinearInventory` instances owned by `Case`, `StorageShelf` and `ItemBox`.
The product may own only its widening policy, configuration, diagnostics,
single Postfix and exact-owner lifecycle.

## Approved Implementation Boundary

- Rehome `DTMAPI.ChestLocatorEnhancerMod` as an SDK-generated
  `netstandard2.0` Advanced ProductNative mod at version `1.0.0`, minimum
  DTMAPI `0.5.5`.
- Preserve Workshop item `3742765514`, package DLL
  `DTMAPI.ChestLocatorEnhancer.dll`, official folder
  `DTMAPI_ChestLocatorEnhancer` and canonical config
  `DTMAPI/config/DTMAPI.ChestLocatorEnhancerMod.json`.
- Install one exact ProductNative Postfix under
  `dtmapi.mod.dtmapi.chestlocatorenhancermod`; install atomically and unpatch
  the exact owner during disable, failed Entry and Loader owner deactivation.
  SaveLoaded and ReturnedToTitle reset only product observation state; the
  enabled process-lifetime Hook remains installed for clean re-entry.
- Preserve native inventory instances, deduplicate additions, and leave
  CountItem/CostItem, persistence and UI owned by the game.
- Keep the frozen public ABI provider in mandatory GameBridge as a thin
  on-demand proxy. Move its heavy executor into the existing Compatibility
  Host and preserve provider identity, DTO members, merge policy and
  observable state.
- Prove product-first and compatibility-first ordering cannot install two
  widening Postfixes, including exception-safe rollback.
- Reuse the existing Catalog-driven SDK, package, Doctor/Manager,
  Compatibility Host and release-contract mechanisms. Do not create a
  product-specific assurance family.

## Changed Files

- `products/first-party/ChestLocatorEnhancer/*`
- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/ChestLocatorEnhancer/*`
- `src/DTMAPI.GameBridge.DolocTown/Compatibility/ChestLocatorEnhancer/*`
- `src/DTMAPI.GameBridge.DolocTown.Compatibility/*`
- `src/DTMAPI.GameBridge.DolocTown.QA/*ChestLocator*` and the generic
  requested-owner deactivation fixture
- `tests/DTMAPI.UnitTests/ChestLocator*`
- `tests/DTMAPI.AbiCompatibilityHarness/Program.cs`
- `tests/DTMAPI.QaUnitTests/Program.cs`
- `author-sdk/advanced-reference-policies/doloctown-23762374-chestlocator-v1*`
  and the existing policy registry
- the existing Catalog, publish, compatibility, Phase 0, build,
  release-contract and smoke projections
- `docs/api/public-api-matrix.md`
- `docs/hook-map/focused/ChestLocatorEnhancer.md`
- `docs/debug/regressions/smoke-matrix.md`
- the Batch 6 identity contract, lightweight roadmap, monthly ledger and this
  Update

The historical Strict fixture under `testmods/ChestLocatorEnhancerMod` is
removed. Product source now has one authority under
`products/first-party/ChestLocatorEnhancer`; the retained published binary is
evidence for the frozen ABI, not a second source implementation.

## Validation

Completed focused checks:

- Release builds for Abstractions, mandatory GameBridge, Compatibility Host,
  optional QA and Unit: PASS with zero warnings/errors;
- Unit focuses `chestlocator-product`, `batch5-gamebridge-demand`,
  `compatibility-host` and `api-metadata`: PASS;
- product-first and compatibility-first arbitration, pending-demand
  reconciliation, atomic pre-resolution/rollback, exact-owner residual
  rejection, disable/re-enable and title/save cleanup: PASS;
- Catalog/source/zero-leftover, tracked Advanced policy, Doctor, QA Unit,
  retained ABI and Phase 0 current-vs-historical contract checks: PASS;
- the retained ABI gate resolves the exact old DLL hash
  `125704135FFF47778993B268F89A90E911B7757D14938B731AB91CCC7BCDE2CE`,
  reports zero public removals, no mandatory Host AssemblyRef and no mandatory
  heavy compatibility markers;
- Catalog-driven Author SDK validate/build/pack: PASS. Candidate package
  `temp/batch6-chest-locator-enhancer-advanced-pilot/DTMAPI-ChestLocatorEnhancer-advanced-pilot.zip`
  is 17,088 bytes with SHA-256
  `D43FB74329C7434726BAEB829E0FAE4D82D4CBA3D7661D1C44CCD44A24005F77`;
  its 32,768-byte entry DLL has SHA-256
  `FB0EF1504ADE1503901CE6F889FCA0A498EE3EADA76AC823A2109FD17DB63F57`;
- focused release-contract execution builds all seven admitted products twice
  from one current Author SDK tool build and reports deterministic pairs:
  PASS.

The first launchable attempt, `GAME-SMOKE/20260723-202456`, failed closed in
the optional QA settings allowlist before product Entry and is non-acceptance
evidence. The allowlist and its QA Unit projection were corrected without
changing product behavior.

Authoritative bounded acceptance:
`docs/debug/evidence/GAME-SMOKE/20260723-203127`, PASS. Current DLLs and
HookProbe loaded the third save (native slot index `2`). The ProductNative
route installed exactly one Postfix on the reviewed native target and proved
native inventory behavior with `case_locator` counts
`baseline=0 -> afterPlace=3 -> afterCost=1`; CountItem/CostItem remained
native-owned. Title return and the title-button lifecycle passed. Real Loader
deactivation observed
`actual1+callback1 -> instance0+actual0+callback0+roots0`, then QA/profile/
Author-source cleanup, exact three-file player-save restoration and final
no-`DolocTown.exe` exit all passed.

Complementary same-process re-entry acceptance:
`docs/debug/evidence/GAME-SMOKE/20260723-210424`, PASS. The current product
installed its exact Postfix once with count `1`, then completed two real
third-save cycles with coordinator totals
`requests=2; nativeEnter=2; nativeReturn=2; saveLoaded=2`. Its
ReturnedToTitle/SaveLoaded observation resets ran across both cycles without
duplicate installation or event-handler failure; startup, HookProbe, title
boundaries, fatal-window check, exact save/profile/Author-source/QA restoration
and final process exit all passed.

`GAME-SMOKE/20260723-205205` and `20260723-210101` are non-acceptance
orchestration conflicts: their combined QA routes returned to title before a
save-load request. Both runners still restored their scoped state exactly;
neither run is used as product evidence.

No complete Release, L0-L5, GC ladder or long test was run.

### 2026-07-23 Commit-Range Audit Correction

Commit-range Review
[`20260723-0007`](../../reviews/code/2026/20260723-0007-seven-product-commit-range-audit.md)
reopened this Update because the earlier focused evidence did not execute both
physical Harmony owner orders, two transaction tests still hard-coded three
Advanced products, and the ProductNative query path retained avoidable boxing,
reflection/allocation and log amplification.

The correction is implemented and focused checks now pass:

- a `net48` focused fixture uses the repository-provisioned real HarmonyX
  runtime to patch one exact test target in both owner orders, execute the
  Postfix, fail the second owner closed, preserve the unrelated owner during
  cleanup, disable/re-enable/restart, and retain installed/count `1` after a
  simulated failed exact-owner unpatch;
- executable Unit object graphs cover shared Case,
  StorageShelf/ItemBox, native `autoUseBox`, both option-off paths and
  reference-identity dedup instead of source-token assertions;
- the Postfix no longer binds or boxes its unused `Vector2Int` arguments;
  traversal scratch and immutable reflection metadata are reused, overlapping
  roots are visited once, diagnostics format lazily, and stable repeated
  results log once unless verbose mode is enabled;
- the warmed 32-call allocation gate stays within 262,144 bytes and the
  repeated-observation gate records only first observation, transition and
  explicit verbose output;
- developer official-local transaction and player Runtime-only uninstall
  tests exact-match the Catalog-derived seven-product Advanced set: PASS;
- Product Catalog passes under both PowerShell 7 and Windows PowerShell 5.1
  after replacing the unsupported `String.Contains` overload;
- current Catalog-driven product build/pack: PASS. Package is 17,734 bytes
  with SHA-256
  `D747977D306B3CC6C7C947AED1D7445F7324434B8CEB59B2780EF9259A2026A2`;
  entry DLL SHA-256 is
  `29C2FD2EDA5618C6814C851913F64E77245CE9A31703FF6FD70C1B802D0DA9C1`.

Final committed-candidate acceptance:
`docs/debug/evidence/GAME-SMOKE/20260723-224200`, PASS. The installed Runtime
reports `BuildCommit=7ace68260cb1`; the installed 34,304-byte product entry DLL
has SHA-256
`29C2FD2EDA5618C6814C851913F64E77245CE9A31703FF6FD70C1B802D0DA9C1`,
matching the corrected Catalog-driven package. Current DLL startup,
HookProbe, third-save load, native Chest placement/CostItem behavior,
title-button recovery and real Loader owner deactivation all pass. The exact
product owner transitions
`actual1+callback1 -> instance0+actual0+callback0+roots0`; protected saves,
official profile, Author source and QA staging restore exactly, and no
`DolocTown.exe` remains. This closes the commit-range correction without
replaying the complete Release, L0-L5, GC ladder or a long test.

The `224200` game behavior, owner cleanup and restoration evidence remains
valid, but its bundled Player Doctor was stale and reported findings with exit
code `2` because it did not yet recognize the Chest Advanced policy. No game
rerun was needed for that tooling-only mismatch. The current Player Doctor was
rebuilt into `dist/player-doctor/doctor-policy-closure` (EXE SHA-256
`9B542C44E0DCE1816DF0D5DB49CE914FB0B6AE95E5163CCB2138D6AFDF252A28`)
and inspected the same installed game tree read-only with exit code `0`:
`artifacts=58; errors=0; warnings=0; misplaced=0; minimumBlocked=0;
runtime=0.5.5`. Its exact Advanced policy set contained the seven admitted
products, including ChestLocatorEnhancer. The focused InstallDoctor suite now
also compares the Doctor's embedded policy resources and embedded registry
against the Catalog-derived Advanced product set; all twelve tests pass.

## Measurement

Admission baseline:

- mandatory ChestLocator GameBridge boundary: 542 physical / 475 non-empty
  lines;
- heavy service executor within it: 426 physical / 378 non-empty lines;
- old product entry source: 101 physical / 89 non-empty lines.

Final current-tree measurements:

- mandatory ChestLocator GameBridge boundary:
  542 physical / 475 non-empty -> 243 / 206, a reduction of 299 physical /
  269 non-empty lines;
- mandatory GameBridge DLL:
  966,656 -> 937,984 bytes, a reduction of 28,672 bytes (about 2.97%);
- Compatibility Host Chest executor:
  540 physical / 481 non-empty lines; Host DLL grows from 184,832 to 209,408
  bytes;
- ProductNative after the focused hot-path correction:
  eight source files / 1,672 physical / 1,524 non-empty lines; product DLL
  34,304 bytes.

This is a real reduction in default-loaded Runtime volume only. The frozen
executor remains shipped in the optional Host and the product DLL is also
shipped, so no download-package, installed-footprint, combined-binary,
total-source or total-repository reduction is claimed.

## Seven-Product Comparison

All seven products use the same Catalog-driven Advanced SDK validate/build/
pack path, package/Doctor/Manager projections and live zero-leftover
classification. ChestLocatorEnhancer also uses the existing owner-scoped
ConfigMenu and Loader lifecycle conventions, but its inventory traversal,
configuration, diagnostics and Harmony owner remain ProductNative.

No other admitted product consumes
`ArchiveDataHandle.GetAvailableInventories(Vector2Int, Vector2Int, bool)` or
shares the widening/restore invariant. The frozen compatibility executor uses
the same native owner only to preserve an old ABI and therefore is not an
independent new-product consumer. No SharedNative capability, new Host, new
public API or product-specific receipt/checker family is justified.

## Rollback

Revert the final implementation as one unit while retaining the frozen public
ABI declarations and retained-consumer evidence. A rollback must restore the
previous exact provider behavior and must not modify player inventories,
save files or native UI.

## Follow-up

The earlier independent final Review
[`20260723-0006`](../../reviews/code/2026/20260723-0006-chestlocator-seventh-product-final-review.md)
is supplemented by commit-range Review `20260723-0007` and the final `224200`
committed-candidate acceptance. The seven-product baseline is frozen
`verified/closed`. An eighth implementation, G7, 0.5.5 publication and
general Advanced authoring remain blocked.
