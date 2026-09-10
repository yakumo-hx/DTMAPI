# 20260714-0004 Batch 2 Version And Release Authority

## Metadata

- Update ID: `20260714-0004`
- Date: 2026-07-14
- Lifecycle Status: `verified`
- Validation Level: `docs,source,unit,runtime`
- Runtime Validation: `passed`
- Related Issue State: `open`
- Area: release/version/catalog/oil/workshop/compatibility
- Source: user requested continuation into Batch 2 after the pre-Batch 2 regression and exact-commit gates

## Scope

This Update is the continuing lifecycle owner for Batch 2 version and release authority. Its first independently reviewable slice will:

- select Oil's existing `0.3.1-dtmapi` source manifest value as the current prototype version without promoting the planned `1.0.0` target;
- align Oil's native `info.json` source and publish-text projection with that current version;
- fail closed when a ContentOnly developer package's manifest and native info versions drift;
- prove the installed Oil package, Catalog source version, source/staged manifest, staged info and release receipt agree while the package remains DLL-free and has no DTMAPI minimum;
- prove a Workshop-only CodeMod requiring DTMAPI `0.5.5` is visibly classified `api-too-new` on the current `0.5.3-alpha` Runtime before `Assembly.LoadFrom`, Entry, owner roots or restart state.

The first slice does not create or publish DTMAPI `0.5.5`, change Runtime/MSBuild/assembly versions, authorize Oil for Workshop, reconcile every product version, silently rewrite minimum versions, restore the frozen AutoFishing ABI, or claim the complete stale-Runtime update/recovery matrix. This Update remains `in-progress` after that slice until the later Batch 2 gates are closed.

## Known Facts And Rejected Hypotheses Before Change

- Oil currently declares `0.3.1-dtmapi` in `manifest.json` but `1.0.0` in `official-info.json` and the publish-text row. The Catalog separately records `0.3.1-dtmapi` as current source and `1.0.0` as a blocked future target.
- The developer installer preserves both values, so successful gameplay does not make the package metadata coherent.
- The current Runtime checks `MinimumDTMApiVersion` before dependency resolution and `LoadCodeMod`; existing future-version tests use ContentPacks and therefore do not prove a CodeMod DLL was never loaded.
- A source/unit Workshop-shaped fixture can prove block classification and the pre-assembly boundary. It cannot replace the later retained-package install/update/recovery game matrix.

## Changed Files

- `testmods/OilMod/official-info.json`
  - projects the existing current `0.3.1-dtmapi` source version into native official metadata while leaving the Catalog's future `1.0.0` target untouched.
- `tools/release/dtmapi-mod-publish-zh.json`
  - aligns Oil's non-authoritative publish-text current-version field with the current source version.
- `tools/scripts/release-common.ps1`
  - adds a shared exact, non-empty manifest/native-info version parity assertion.
- `tools/scripts/install-to-game.ps1`
  - invokes the parity assertion for ContentOnly packages after reading metadata and before creating a staging package; explicit drift now fails closed.
- `tools/scripts/check-product-catalog.ps1`
  - checks Oil's Catalog current/future axes, release eligibility, publish scope/path/version and native-info version without widening the assertion to undecided CodeMod products.
- `tools/scripts/test-developer-official-local-install-transaction.ps1`
  - retains the published-package failure matrix and adds a fresh all-non-QA-developer install under both PowerShell hosts;
  - checks source/staged manifest, source/staged info, Catalog, publish text and release receipt versions, plus ContentOnly/DLL/minimum boundaries;
  - directly proves a divergent manifest/info pair is rejected.
- `tests/DTMAPI.UnitTests/Program.cs`
  - checks Oil source metadata parity and the installer/catalog gates;
  - adds an enabled Workshop-only valid CodeMod requiring `0.5.5` and proves current `0.5.3-alpha` rejects it before construction, Entry, load checkpoint, assembly-owner publication, transaction, owner roots or restart state while exposing `api-too-new` and update guidance.
- `docs/workflows/workshop-package-subscription-test-matrix.md`
  - records the ContentOnly current-version projection lane and its non-release boundary.

## Validation

- Oil manifest, native info, publish-text and Catalog JSON files parsed successfully.
- PowerShell syntax validation passed for the changed common release helper, installer, Catalog checker and transaction matrix under PowerShell 7 and Windows PowerShell 5.1.
- `tools/scripts/check-product-catalog.ps1` passed with `products=26`, `public=11`, `workshop-items=21`, and `api-rows=45`.
- `tools/scripts/build.ps1 -Configuration Release` passed with zero warnings, zero errors and `DTMAPI.UnitTests: OK`.
- The complete tracked `tools/scripts/test.ps1 -Configuration Release` suite passed from the working tree with zero build warnings/errors, `DTMAPI.UnitTests: OK`, runtime-evidence retention and allowlist checks, the player Runtime-only uninstall matrix, the dual-host developer install transaction matrix, the product Catalog checker and documentation governance all green.
- The dual-host developer install transaction matrix passed under both hosts (`hosts=2`): the existing eight-package malformed/write/drift/foreign recovery matrix remained green, and each host also installed all fourteen non-QA developer definitions with `oil-version=aligned`.
- The Oil package check found one exact `0.3.1-dtmapi` value across source manifest, source native info, Catalog source row, publish-text row, staged manifest, staged native info and `BundledMods` receipt. The Catalog still records `targetVersion=1.0.0` and `PrototypeBlocked`; staged Oil remained ContentOnly, DLL-free and without `MinimumDTMApiVersion`.
- The Workshop-only CodeMod unit fixture retained `MinimumDTMApiVersion=0.5.5`, produced zero constructor/Entry/checkpoint/assembly-owner/transaction/root/restart state on `0.5.3-alpha`, and exposed required/installed versions, `api-too-new`, and `Run 1_install_dtmapi.bat` guidance.
- 2026-07-15 exact-HEAD replay at commit `866021b5`: a detached Git-only worktree compiled with zero warnings/errors but failed UnitTests because the current Oil native-semantics test directly reads ignored private reverse data. Mounting the configured workspace `references/doloc-town/reverse` root into the exact same worktree made the complete `tools/scripts/test.ps1 -Configuration Release` suite pass in 220.4 seconds, including UnitTests, both installer ownership/transaction lanes and Catalog `26/11/21/45`. The first slice therefore passes under the documented DTMAPI workspace reference condition, while commit-only/hermetic reproducibility remains open for an explicit preflight or tracked synthetic semantic fixture. No official reverse source may be copied into the repository to close that tooling gap.
- The continuing Batch 2 lifecycle stays open for the later version authority, minimum policy, ABI and package/update/recovery gates.

## Evidence

The initial Oil/version slice required no game launch. Batch 2 closure is owned by the focused records rather than duplicated here:

- `20260715-0010`: stale installed Runtime block, catchable live update rollback, successful `0.5.5` install/status and player-like candidate package audit;
- `20260715-0011`: single Runtime/product/minimum authority plus the exact-commit Release suite;
- `20260715-0012`: zero retained public deletions, Lamp retired compatibility shell and exact old AutoFishing Unity Mono binding/Entry/configuration;
- `20260715-0013`: exact eleven-product enabled/disabled Steam matrices and final strict no-HookProbe player-input/provenance gate.

Current runtime rows are `GAME-SMOKE/20260715-125327`, `131518`, `131727`, `132321`, and final input acceptance `153336`; `145818` remains the superseded pre-provenance-hardening input run. The clean exact code-bearing commit `f2f0f3ffef26e4040c26d920b3bc0a554a318180` passed `tools/scripts/test.ps1 -Configuration Release` in 677.3 seconds with zero build warnings/errors, current Catalog `26/11/21/46`, Release contract and documentation governance green.

ISSUE-010 and ISSUE-011 remain open. The minute-scale Smoke runs are not AutoFishing or ActionSpeed GC evidence; both independent speed/disable/title ladders remain deliberately post-Batch 2.

## Rollback

Revert this Update together with its Oil metadata projection, ContentOnly package assertions and Workshop stale-Runtime unit test. Do not revert the preceding Oil ownership P0 or installer transaction checkpoint.

## Follow-Up

- Begin Batch 3 Author SDK from the frozen A1-I1 defaults and keep its receipt/source/reload authority separate from the player Runtime installer.
- Run the independent AutoFishing and ActionSpeed GC ladders later; do not reinterpret Batch 2 Smoke as GC closure.
