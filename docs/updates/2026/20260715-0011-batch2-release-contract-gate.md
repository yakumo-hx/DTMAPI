# 20260715-0011 Batch 2 Release Contract Gate

## Metadata

- Update ID: `20260715-0011`
- Date: 2026-07-15
- Lifecycle Status: `verified`
- Validation Level: `docs,source,unit`
- Runtime Validation: `not-required`
- Related Issue State: `open`
- Area: release/version/catalog/msbuild/products/oil/artifacts/minimums
- Source: user requested the Batch 2 single-version/minimum projection gate for Runtime, all eleven public products and Oil; related review: `docs/reviews/code/2026/20260715-0005-major-update-progress-and-decision-node-review.md`; continuing lifecycle owner: `docs/updates/2026/20260714-0004-batch2-version-release-authority.md`

## Scope

- project the new tracked Runtime version authority into the current Catalog baseline without changing the retained Steam subscription baseline or the `0.5.3.0` assembly compatibility identity;
- teach the Batch 0 Catalog checker to validate the imported authority and conditional five-project MSBuild projection instead of assuming one literal version PropertyGroup;
- resolve the recorded three-product published-builder omission once the shared release definitions contain all eleven public identities;
- project the completed retained-binary ABI result: `FishingAutomationOptions.StopOnManualMove` is restored and the exact old AutoFishing DLL binds, enters and configures under Unity Mono, while movement-cancel/GC remain separate behavior evidence;
- add one independent Release contract checker for actual Runtime and product build metadata, current manifests, native info, publish text, Catalog versions and current minimums;
- keep current source minimums, retained published minimums and future `0.5.5` target minimums as separate facts so the gate cannot legitimize a silent minimum rewrite;
- keep Oil at current `0.3.1-dtmapi`, blocked future `1.0.0`, ContentPack, DLL-free and without any DTMAPI minimum.

This slice does not publish an artifact, authorize a Workshop mutation, close the old-DLL ABI gate, validate a stale Runtime update transaction, launch Doloc Town, or provide AutoFishing/ActionSpeed GC evidence.

## Known Facts And Rejected Paths

- `tools/release/dtmapi-runtime-version.props` is the selected Runtime authority: release `0.5.5`, binary file version `0.5.5.0`, assembly compatibility identity `0.5.3.0`.
- `Directory.Build.props` now contains an ordinary shared PropertyGroup plus one conditional Runtime version PropertyGroup. Reading the first PropertyGroup or searching for literal version values is no longer a valid projection check.
- The eleven current product versions are not a bulk future `1.0.0` promotion. Their source manifest, native info, publish text, explicit project metadata and actual DLL ProductVersion must agree with Catalog `sourceVersion`.
- A product's current source minimum and retained published minimum are different historical axes for several products. The Release contract therefore requires manifest minimum equals Catalog `sourceMinimumDtmApiVersion`, preserves the retained published field independently, and keeps the future target minimum at `0.5.5`.
- Restoring the ABI member removed the source-deletion blocker; `GAME-SMOKE/20260715-131518` subsequently proved exact-byte no-recompile Unity Mono binding, Entry, setter, Configure and F6 enablement. It did not prove movement cancellation or GC.

## Changed Files

- `tools/release/dtmapi-product-catalog.json`
  - moves the current Runtime source baseline to `0.5.5` / `0.5.5.0` while preserving assembly identity `0.5.3.0`;
  - records the source-restored / Unity-Mono-pending AutoFishing ABI state;
  - moves AutoFishing, MoreEquipment and Manbo into the current `PublishedBuilder` lane, clears their obsolete omission markers and refreshes the normalized digest while leaving every public identity, path, version and retained artifact unchanged.
- `tools/scripts/check-product-catalog.ps1`
  - validates the XML authority, release-common projection, conditional MSBuild projection and generated Core constant route;
  - expects and cross-checks all eleven current published definitions and their current Catalog lanes;
  - keeps the completed old-DLL Unity Mono binding result and the still-unproven movement/GC behavior boundary explicit.
- `tools/scripts/check-release-contract.ps1`
  - validates all five Runtime DLLs have AssemblyVersion `0.5.3.0`, FileVersion `0.5.5.0` and Product/InformationalVersion `0.5.5`;
  - validates all eleven public current versions across manifest, native info, publish text, Catalog, explicit csproj metadata and actual Release DLL metadata;
  - validates each current manifest minimum against the current Catalog source minimum and keeps the retained/future minimum axes separate;
  - validates Oil's current/future split and ContentOnly/no-DLL/no-minimum boundary.
- `tools/scripts/test.ps1`
  - includes the new checker in the Windows PowerShell compatibility parse list and executes it after the tracked Release build and Catalog gate.
- `docs/updates/INDEX-2026-07.md`
  - routes this Update from the monthly ledger.

## Validation

- `tools/scripts/build.ps1 -Configuration Release -SkipTests` completed through the repository-local .NET 8 resolver with zero warnings and zero errors for every tracked Runtime, product, fixture and Unit project.
- `tools/scripts/check-release-contract.ps1 -Configuration Release` passed under PowerShell 7 and Windows PowerShell 5.1 after that fresh build.
- The checker read actual Release outputs and passed all five Runtime assemblies plus all eleven public product assemblies, including exact AssemblyVersion, FileVersion and Product/InformationalVersion values.
- The same gate passed all eleven current manifest/native-info/publish/Catalog/csproj projections and their current minimum declarations.
- Oil passed as current `0.3.1-dtmapi`, future `1.0.0`, `ContentPack`, with zero code/DLL source files and no `EntryDll`, dependencies or `MinimumDTMApiVersion`.
- Before the Lamp compatibility row was restored, `tools/scripts/check-product-catalog.ps1` passed under PowerShell 7 and Windows PowerShell 5.1 with `products=26`, `public=11`, `workshop-items=21`, and `api-rows=45`; that count is retained only as historical focused evidence.
- PowerShell parsing passed for the new checker under PowerShell 7 and Windows PowerShell 5.1; the modified Catalog checker and tracked test entrypoint also parse under PowerShell 7.
- The clean exact commit `f2f0f3ffef26e4040c26d920b3bc0a554a318180` passed `tools/scripts/test.ps1 -Configuration Release` in 677.3 seconds with zero build warnings/errors and `DTMAPI.UnitTests: OK`. Evidence retention (`385` source files / `704` smoke runs / `62` runtime identities), player Runtime-only uninstall ownership, both eleven-case Runtime transaction hosts, both developer installer transaction hosts, current Catalog `26/11/21/46`, the Batch 2 release contract and document governance all passed.
- No game path was written, no runtime lock was required, and no runtime/smoke evidence was created.

## Rollback

Revert the Catalog current-source status, both contract-check scripts, the test entrypoint and this Update together. Do not revert the retained published baseline, public identity/path freeze, restored ABI member, product version projections, or Oil ownership P0.

## Follow-Up

- Keep movement cancellation and the independent AutoFishing/ActionSpeed GC ladders outside this release-contract slice; do not downgrade the completed retained-binary binding result.
- A later upload operation must audit the actually downloaded Steam Runtime subscription separately; the current player-like candidate-package audit does not claim post-upload bytes.
