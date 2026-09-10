# 20260713-0010 Batch 0 Boundary Catalog Baseline

## Metadata

- Update ID: `20260713-0010`
- Date: 2026-07-13
- Lifecycle Status: `verified`
- Validation Level: `docs,source,unit`
- Runtime Validation: `not-required`
- Related Issue State: `open`
- Area: release/catalog/identity/version/api-abi/workshop/compatibility/protected-behavior
- Source: user requested starting Batch 0 from the Full Boundary Audit and the 20260713-0014 retained/public/current/future baseline

## Source Request And Boundaries

The user requested that the major update use `docs/reviews/code/2026/20260712-0003-dtmapi-full-boundary-audit.md` as its construction map, read `docs/reviews/code/2026/20260713-0014-workshop-subscription-and-prerelease-baseline-review.md` as the current starting baseline, and begin Batch 0.

This Update owns only the Batch 0 freeze/contract baseline:

- one machine-readable multi-axis Catalog for Runtime, first-party products, planned/prototype/sample/QA/example/negative/compatibility/retired identities, version projections, frozen paths, and release eligibility;
- a dated read-only public Workshop metadata snapshot and explicit pending-live-verification fields where Steam does not expose a semantic version or account ownership proof;
- the five-production-DLL/no-separate-QA-assembly player-package invariant, without hiding the known embedded-Smoke debt;
- protected CustomAnimals JSON+PNG+WAV, Zoom, AutoFishing, and MoreSaves behavior contracts;
- API status/ABI compatibility identity freeze against the canonical public API matrix and the retained 0.5.2 player-artifact baseline;
- an offline fail-closed contract checker integrated into the tracked test path.

This Update does **not** upload or mutate Workshop items, modify the subscription cache, install/uninstall DTMAPI, launch Doloc Town, change Runtime or Mod versions, restore the known fishing ABI deletion, move products, change public API status, implement either P0 ownership fix, extract QA, or change player behavior.

## Required Safety Clause

> 先做本轮 API/domain 的 native owner 方法体审查；未找到 native owner 或状态持有者前，不得通过 mod 层补丁冒充 API 重做完成。

Batch 0 records API/status/behavior boundaries only. No gameplay or GameBridge API implementation is authorized by this record.

## Starting Baseline

- B-PUBLISHED: retained DTMAPI `0.5.2-alpha` plus all eleven known first-party Workshop products and recorded hashes.
- B-CURRENT: unpublished tested `0.5.3-alpha` / `0.5.3.0` source and local behavior evidence.
- B-055: selected DTMAPI `0.5.5` future release target; no final artifact exists.
- Public compatibility: fifteen retained non-Runtime DLLs reference `DTMAPI.Abstractions`, including eleven first-party and four external subscribed packages; the release-cutoff public ecosystem scan remains a distinct gate.
- Known ABI blocker: retained AutoFishing calls `FishingAutomationOptions.set_StopOnManualMove`, which current source has deleted and Batch 0 must record but not yet repair.

## Changed Files

- `tools/release/dtmapi-product-catalog.json`: authoritative multi-axis Catalog, release stop, three version boundaries, retained/public identity and path digests, five-DLL package invariant, API/ABI freeze, compatibility/retired/external rows, and the reserved AnimalPack identity;
- `tools/release/contracts/protected-behavior-contracts.json`: CustomAnimals, Zoom, AutoFishing, and MoreSaves protected-current contracts;
- `tools/release/baselines/workshop-public-metadata-20260713.json`: dated read-only public Steam metadata/search snapshot with a cross-PowerShell ISO-UTC digest;
- `tools/scripts/check-product-catalog.ps1`: PowerShell 5.1-compatible fail-closed checker for Catalog/source/release projections, manifests, frozen digests, protected contracts, API matrix, Workshop snapshot, and an optional staged Runtime artifact;
- `tools/scripts/test.ps1`: runs the checker and its Windows PowerShell syntax gate in the tracked Release test path;
- `tools/scripts/release-common.ps1`: removes the false `DeveloperOnly` label from three products with retained public identities; builder/install membership and versions are unchanged;
- `tools/release/dtmapi-mod-publish-zh.json`: marks all eleven retained public identities as `published`, classifies AutoHarvest as an API-demand sample, adds the retained Manbo identity, and states that this text projection grants no upload authority;
- `tools/scripts/install-to-game.ps1` and `testmods/README.md`: remove the false implication that every source under `testmods` is a dev-only product while preserving install behavior;
- this Update and `docs/updates/INDEX-2026-07.md`.

## Implemented Boundary

- The Catalog contains 26 current/planned/prototype/sample/QA/example/negative product rows plus separately typed compatibility components, retired research, external ABI consumers, and pending local classifications.
- The Runtime plus eleven current first-party public product identities are frozen with exact Workshop ID, UniqueID, folder, package/DLL, config/sidecar paths, published version/minimum, retained size/tree hash, and release lane. A complete identity/path digest prevents synchronized drift between the Catalog and legacy projections.
- B-PUBLISHED remains `0.5.2-alpha`; B-CURRENT remains unpublished `0.5.3-alpha` / `0.5.3.0`; B-055 remains a no-artifact `0.5.5` target with selected assembly compatibility identity `0.5.3.0`.
- The retained subscription source is explicitly `SteamManagedMutableSubscriptionCache`; only this dated record and its digests are frozen, and no immutable artifact archive is claimed.
- `DTMAPI.AnimalPack` / `DTMAPI_AnimalPack` / `DTMAPI-AnimalPack` is reserved as `FrozenReservedNoArtifact`; the four animal prototype inputs remain protected inputs rather than independently publishable products.
- The player Runtime invariant is exactly five production DLLs. Smoke/QA remains embedded in `DTMAPI.GameBridge.DolocTown.dll` as recorded debt; Batch 0 does not claim that QA extraction is complete.
- The complete protected-contract file is digest-frozen in addition to typed field checks, so reversing a safety statement or changing a lifecycle guarantee fails the offline checker.
- The canonical public API status projection remains 45 rows, with fifteen retained non-Runtime consumers and the deleted `FishingAutomationOptions.StopOnManualMove` setter recorded as an open ABI blocker rather than repaired here.

## Validation

- All four JSON files parsed successfully in the current PowerShell host.
- `tools/scripts/check-product-catalog.ps1` passed in both PowerShell 7 and Windows PowerShell 5.1: 26 products, 11 first-party public products, 21 public Workshop snapshot items, and 45 API rows.
- A unique temporary staging root under the system temp directory was built with `build-release-workshop-packages.ps1 -Configuration Release -SkipBuild -RuntimeOnly`. The optional artifact audit passed in both PowerShell hosts and found exactly the five production DLLs, no ordinary-Mod manifest, and no smoke settings in the Runtime payload. The temporary root was verified and removed after the audit.
- `tools/scripts/test.ps1 -Configuration Release` passed in 108.2 seconds. Runtime and Mod projects built for their tracked targets with zero warnings/errors; `DTMAPI.UnitTests`, Runtime evidence retention, the evidence allowlist, the Windows PowerShell syntax gate, the Batch 0 checker, and document governance all passed.
- `tools/scripts/check-doc-governance.ps1` passed with 4441 checks.
- `git diff --check` passed; only expected Git line-ending notices were emitted.

No game install, uninstall, launch, third-save load, local official `MODS` write, subscription write, or Workshop mutation was performed. The shared Runtime lock was therefore not required. Runtime Validation remains `not-required` because this batch changes only source/release metadata, documentation, and offline/static validation.

## Evidence

- The [dated public Workshop snapshot](../../../../tools/release/baselines/workshop-public-metadata-20260713.json) records metadata captured at `2026-07-13T06:27:27Z` and search results captured at `2026-07-13T06:02:22Z`. Its 21 normalized rows hash to `3a570e9813c9886b5da4868eaf8b640910e92aac1329cf5c5ac382c15b0b7d64`.
- Public metadata returned result `1`, app `2285550`, public visibility, and creator `76561198946111933` for the Runtime and all eleven current first-party products. Thirteen query results share that creator because the retired DolocTown SMAPI Runtime is also present; the active DTMAPI set is twelve items including Runtime.
- A read-only recheck of the Steam-managed subscription trees matched the [0014 baseline review](../../reviews/code/2026/20260713-0014-workshop-subscription-and-prerelease-baseline-review.md) for all 12/12 Runtime/product file-count, byte-count, and tree-hash facts. Those mutable source trees are evidence inputs, not an immutable archive.
- Steam public metadata does not expose these packages' semantic manifest versions and does not prove current account control. Semantic versions therefore remain sourced from the retained subscription artifacts, and account control remains `PendingAccountControlVerification`.
- Frozen digests: retained artifact facts `2b5d9974a7e3d321c70442fd25457da9d1a91b548360e0f1cd7ffa2dd1c937ff`; complete public identity/path rows `e1250058f0b1f848df0562bd4b521331ee07dbf833ae2d449e35ab4aaf5df5d6`; protected contracts `2e1096c7dabeb69856960de98f31c633edbbfe48a7a052bc20a57b354be70a4a`; canonical public API status rows `d6a59e89fcc423a1568f86d1fed5660c4dc3d8733785d8b145ca7c6203890635`.

## Rollback

Remove the Batch 0 Catalog/snapshot/checker, remove its test-path invocation, revert only the classification fields changed by this Update, and remove this Update plus its monthly row. No Runtime, game, Workshop, subscription, package, API, save, or player-behavior rollback is required.

## Follow-Up

Batch 0 is verified as a source/offline boundary freeze. Next, implement the player-uninstaller receipt-ownership P0 and the Oil/OneAction ownership P0 under separate Updates. Version/release authority, ABI restoration, QA extraction, recurring-work reduction, demand activation, release-cutoff public ecosystem scanning, account-control verification, and Runtime publication remain later independently validated work.
