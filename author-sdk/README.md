# DTMAPI Author SDK

当前 0.7.0 候选的任意作者 Advanced 工作流见 [本机原生引用](NATIVE-REFERENCES.md)。新项目使用 NativeContractVersion=2；旧 V1 与 receipt 包保留各自校验路径。

`dtmapi-author` is the single authoring authority for SDK `0.7.0`. Complete M3 API `0.7.0` is the default and requires Runtime 0.7.0; retained targets keep their own catalog ranges. Available means buildable from the exact committed source recipe, not published: release acceptance is still in progress and the published Runtime remains `0.6.1`. The Windows x64 ZIP is self-contained on .NET 8; generated game-loaded CodeMods remain `netstandard2.0`.

Internal `0.6.3` adds [package dependencies and shared DLLs](PACKAGE-DEPENDENCIES.md). New projects select author schema 3; older projects require explicit migration.

## API target selection

[target-catalog.json](target-catalog.json) defines the default and available API targets, each payload's immutable contract, and its supported package Runtime range. `new --api-target 0.5.5` selects the retained target explicitly; omission uses `0.7.0`. Available means buildable, not published. `build` and `pack` use the project's `targetDtmApiVersion`; they do not infer it from the installed game or Runtime version. Schema-1 projects keep their original `targetRuntimeVersion=0.5.5` field without automatic rewriting.

The retained internal `0.6.2` contract combines the Experimental [platform services](PLATFORM-SERVICES.md) and [reflection](REFLECTION.md). Save-bound data remains outside the current API. Earlier unpublished M2 and reflection candidates remain repository history and are not distributed SDK targets. Old hashes and packages are never relabeled.

New packages must declare a minimum Runtime within the selected target's catalog range; promising a version below the compiled API target is now an error. Readers retain the lower-minimum behavior of previously issued SDK 0.1.0 / API 0.5.5 Strict and ContentPack packages, including existing receipt recovery; Advanced policy requirements remain enforced. This does not alter frozen payload bytes. An explicit `--compatibility-root` or `DTMAPI_AUTHOR_COMPAT_ROOT` is authoritative: missing, mismatched or modified payloads fail instead of silently falling back to another directory. The executable embeds the catalog and original compatibility contract; editing the distributed JSON cannot authorize another target.

`session prepare --api-target <version>` requests that same available API target while negotiating the session protocol independently. Existing Advanced reference policies remain bound to their original target and receipts.

Use **Strict CodeMod** for public DTMAPI/BCL and supported managed dependencies. Any valid author ID can use the current **Advanced CodeMod** native-contract path with explicit local game references. The older receipt path remains identity-specific; copying another product's policy or receipt fails closed. Neither path permits redistributing game/platform DLLs.

Strict project assembly references and their actual PE dependency closure reject host dependencies with `SDK160`. Source names bind through Roslyn against the frozen references: comments, strings and same-named user types are valid; unresolved host types produce compiler diagnostics. Undeclared inputs and unsupported targets/options produce `SDK180`; undeclared bundled DLL inputs produce `SDK161`. Schema 3 supports declared libraries, resources, locked packages and `managedReferences`, subject to complete package closure validation. See [projects and restore](PROJECTS-AND-RESTORE.md) for the supported grammar. Native V1/V2 use schema 3 and generated local provenance; hand-editing only the Runtime manifest does not authorize native references.

`DTMAPI.Abstractions` is a public assembly, not a promise that every public type is Stable. Before adopting a surface, check both stability and disposition in the bundled [API status](API-STATUS.md), generated from the repository's authoritative matrix. Diagnostic, Frozen, Disabled, DTMAPI-internal, and Proposed contracts are not ordinary new-mod dependencies. See [migration](MIGRATION.md), including the historical CSV removal exception.

Existing frozen consumers can follow [the migration guide](MIGRATION.md). It distinguishes player-product alternatives, unavailable capabilities and retained ABI/data, without announcing a removal date.

## Commands

New CodeMod templates delegate normal IDE/MSBuild Build to the same self-contained CLI. Set `DTMAPI_AUTHOR_SDK_ROOT` to the extracted SDK directory; design-time uses its frozen references and C# 12. `build MyMod --configuration Debug` writes `bin/dtmapi-author/Debug`; the default Release writes `bin/dtmapi-author/Release`. By default Debug defines DEBUG and TRACE, and Release defines TRACE. Mod projects use checked arithmetic, nullable enabled, AnyCPU, no unsafe and deterministic portable symbols; their assembly versions come from manifest.json. The current SDK also supports author-defined constants, XML documentation and ordinary library projects with their own supported settings; see [projects and restore](PROJECTS-AND-RESTORE.md).

Old projects are unchanged until `dtmapi-author migrate-build MyMod`. Migration retains the exact original csproj in a named backup and replaces its Build delegation; custom unsupported inputs must be resolved explicitly. Frozen target props are never rewritten. The supported IDE lane is the Windows SDK template. SDK 0.6.5 adds explicitly declared ProjectReference, resources, generated source inputs and locked PackageReference replay; see [projects and restore](PROJECTS-AND-RESTORE.md). Arbitrary MSBuild and generator execution remain unsupported.

`pack MyMod --configuration Debug` includes matching PDB bytes. Release packages omit symbols unless `--symbols true`; local build output always retains them. DLL/PDB hashes, normalized source paths, bound references and input identity are recorded in JSON and `<DLL>.build.json`. Legacy source paths remain relative to sourceDirectory; explicit build settings use project-relative paths so generated sources and resources have stable identities. Symbol emission alone does not prove Mono source-line or breakpoint support.

```text
dtmapi-author new codemod MyMod --id Author.MyMod --name "My Mod" --author Author
dtmapi-author new codemod NativeObserver --id Pine.NativeObserver --name "Native Observer" --author Pine --code-mod-kind Advanced --game-root "D:\Games\Doloc Town"
dtmapi-author new contentpack MyPack --id Author.MyPack --name "My Pack" --author Author
dtmapi-author validate MyMod
dtmapi-author build MyMod
dtmapi-author pack MyMod
dtmapi-author hash MyMod/dist/Author.MyMod-0.1.0.zip
dtmapi-author deploy MyMod/dist/Author.MyMod-0.1.0.zip --game-root "D:\\Games\\DolocTown"
dtmapi-author update MyMod/dist/Author.MyMod-0.2.0.zip --game-root "D:\\Games\\DolocTown"
dtmapi-author withdraw Author.MyMod --game-root "D:\\Games\\DolocTown"
dtmapi-author recover Author.MyMod --game-root "D:\\Games\\DolocTown"
dtmapi-author deployment-status Author.MyMod --game-root "D:\\Games\\DolocTown"
dtmapi-author session prepare --game-root "D:\\Games\\DolocTown"
dtmapi-author session snapshot Author.MyMod "%USERPROFILE%\\AppData\\LocalLow\\RedSawGames\\DolocTown\\MODS\\Author.MyMod" --game-root "D:\\Games\\DolocTown"
dtmapi-author session reload Author.MyMod "%USERPROFILE%\\AppData\\LocalLow\\RedSawGames\\DolocTown\\MODS\\Author.MyMod" --game-root "D:\\Games\\DolocTown"
dtmapi-author session clear --game-root "D:\\Games\\DolocTown"
dtmapi-author doctor "D:\\Games\\DolocTown"
```

Add `--json` for a machine-readable report. A minimal Strict `build` compiles in-process against the SDK's hash-fixed compatibility payload without an installed `dotnet` or game directory; projects with package dependencies additionally need their verified restore inputs. Advanced uses the explicit local game root saved by `new --game-root` or supplied to the command. The current native-contract path records the exact host identities, paths and hashes and derives required signatures from the compiled output; legacy receipt projects retain their tracked policy/build restrictions. The CLI does not search ambient game installs or unrelated workspaces for native assemblies. Tested game compatibility and remaining limitations are described in [native references](NATIVE-REFERENCES.md).

`pack` already compiles a CodeMod once. Use `pack MyMod --build-output path/to/compiled --json` when the calling build also needs that DLL and PDB; do not precede it with a duplicate `build`. The report keeps `outputPath`/`sha256` for the ZIP and adds `values.buildOutputPath`, `buildOutputSha256`, `buildInputSha256` and `buildInputIdentity` for the actual compilation. ContentPack does not accept `--build-output`.

Repository product wrappers use `prepare-author-sdk.ps1` once, then reuse its explicit output through `-AuthorSdkRoot`. Preparation checks the SDK source/dependency graph, compiler, build properties and packaging inputs; a normal product-source change does not rebuild the SDK. `preflight-workspace.ps1 -Operation BuildProduct -CatalogId more-saves` reports missing setup and the separate compile-reference/test-game paths without creating output. Formal release verification still packs twice for determinism.

`pack` emits the official package shape:

```text
Package/
  info.json
  dtmapi-dependencies.json   # current dependency-contract packages
  dtmapi-native-build.json   # current Advanced V1/V2 packages
  Content/DTMAPI/
    manifest.json
    Author.Mod.dll       # CodeMod only
    dtmapi-package.json  # metadata only; never an ownership receipt
    ...content files
```

Generated dependency/native metadata paths are relative to the package root and are recorded in `dtmapi-package.json`. Legacy receipt packages retain their original layout; use the marker's actual path instead of moving generated files by hand.

`manifest.json` remains authoritative for Mod identity, Mod version, dependencies, and minimum Runtime. `dtmapi.author.json` is an SDK-only pre-1.0 build/publish input and does not replace the Runtime manifest. The package's `info.json` is generated from both inputs and must not be hand-maintained in the author project.

Current Advanced projects accept any valid author ID through the [native-contract workflow](NATIVE-REFERENCES.md); no first-party registry row is required. Their generated provenance binds the package, exact local host references, required signatures, `netstandard2.0`, Harmony owner and minimum Runtime. Supported private/shared managed libraries are described by the dependency inventory. Game, Unity, Harmony, BepInEx and DTMAPI platform DLLs are never bundled as author dependencies; generated metadata reference surfaces are compile-only inputs.

Legacy receipt-based Advanced projects keep their exact tracked policy and live authoring registry. Core and Doctor also retain the historical acceptance registry for already-issued receipts; those historical rows cannot authorize a new receipt. These identity-specific compatibility rules do not restrict the current self-service native-contract path.

The SDK has no upload, credential, force, or adopt operation. DTMAPI-managed Strict or Advanced CodeMods must never be created, built, packaged, or deployed under `BepInEx/plugins`; third-party plugins placed there are External BepInEx Plugins outside DTMAPI ownership.

## Legacy receipt-bound deployment recovery

Official installation derives the profile root using the same `DTMAPI_DOLOC_PERSISTENT_ROOT` override and Windows LocalLow default as Runtime. SDK never edits `SAVE/mod_infos.json`: enable/disable the Mod in the game's official Mod UI. `install-local` requires expected ID, version and ZIP SHA-256; `deploy` creates a new owned package and `update` requires an existing exact receipt. `deployment-status` reports disk inventory separately from official enabled state; selected source and resident DLL need a live `session snapshot` and are otherwise unavailable. A disk update never proves that a process loaded it.

Official journal schema 4 uses the existing transaction/inventory fields, no compound historical `localInstall`, and binds its receipt to `OfficialLocal/<UniqueID>`. Readers enforce the current resolved official root, retained complete inventory and exact prior transaction. Unknown files, wrong roots, access failures and mismatched journals are preserved and rejected. Staging and recovery remain on the official MODS volume. Windows cold mutations hold the SDK operation lock and an exclusive game executable handle through commit or rollback, rejecting a running or concurrently starting game. Restart after changing code. Non-Windows mutation is currently unsupported; no platform support is inferred from netstandard2.0.

DTMAPI 0.6.0 no longer discovers the historical `game/Mods/<UniqueID>` development root. The current source candidate routes `deploy`, `update` and `install-local` to official profile `MODS/<UniqueID>`, recognized as `Local.<UniqueID>`. It accepts any valid author ID without Catalog registration. `source local select` remains paused with `SDK003` for the historical source. Do not use that directory as player-load or release evidence.

Existing receipt-bound deployments remain recoverable. `deployment-status` and `install-local-status` are read-only; `recover` restores an interrupted prepared transaction; `withdraw` moves an exactly verified committed package into retained same-volume recovery; and `source local clear` may remove a stale historical selection without moving package files. These compatibility commands do not make `game/Mods` a Runtime source.

The historical package-local `.dtmapi-author-receipt.json`, package-external journal, and complete deployment inventory must agree exactly:

- the package-local receipt;
- the package-external committed journal;
- the complete current deployment inventory, including unknown files and empty directories.

Each game root has an exclusive operation lock. Historical staging, destination, failed, and recovery directories stay on the `Mods` volume so an already prepared transaction can be recovered with its original atomic-move semantics. A mismatch preserves evidence and fails closed. Missing, forged, wrong-root, wrong-path, wrong-ID, wrong-kind, drifted, concurrent, receipt-only, and journal-only states are never adopted.

Current journal writes performed by legacy recovery/withdraw use schema 3. Schema 3 retains the schema-2 deployment identity and inventory fields and adds the nullable `localInstall` transaction which owns the exact journal/source-state preimage and SDK-owned paths of an already prepared compound local install. Package-local receipts and package markers remain schema 2.

The reader retains narrow upgrade paths for exact SDK `0.1.0` schema-1 and prior schema-2 journals already present on a player's machine. A historical schema-1 `CodeMod` is always interpreted as `Strict`, while a historical `ContentPack` remains code-free. Its journal, package-local receipt, legacy package marker, identity, destination, package hash, payload hash and complete inventory must all agree. `deployment-status` is read-only and does not rewrite historical state; `recover` and `withdraw` may write a schema-3 journal while preserving the old receipt authority. Historical journals cannot authorize official updates: recover/withdraw the old deployment first. Schema 1 can never declare or upgrade itself into `Advanced`, and unknown or mixed-version fields fail closed.

`DTMAPI_AUTHOR_STATE_ROOT` may relocate package-external test/CI state. Without it, state is stored under `%LOCALAPPDATA%/DTMAPI/AuthorSdk/state/installations/<game-root-key>`. The key is the lowercase first 16 bytes of SHA-256 over the canonical, uppercase game-root path.

## Source modes

```text
dtmapi-author source local select Author.MyMod <game/Mods/Author.MyMod> --game-root <game>
dtmapi-author source local clear Author.MyMod --game-root <game>
dtmapi-author source workshop prepare Author.MyMod --game-root <game>
dtmapi-author source workshop clear Author.MyMod --game-root <game>
dtmapi-author source reproduction begin --game-root <game>
dtmapi-author source reproduction restore <snapshotId> --game-root <game>
dtmapi-author source status [Author.MyMod] --game-root <game>
```

Historical Local Development state records an exact `DTMAPI-FileTree-SHA256-v1` digest in external `source-state.json`; it is recovery-only in 0.6.0 and grants neither file ownership nor player source selection. New local selection is paused, while status and clear remain available. Player Reproduction first writes an external snapshot, then atomically clears every override and refuses new selections until the exact snapshot is explicitly restored. Workshop Validation only prepares state and reports Runtime-captured native snapshot availability. Offline directory presence is never reported as native Workshop success.

## Explicit Runtime session

`session prepare` must run before the user starts the game. Current source writes a one-shot schema 2 `author-session.json` and a protected `author-session-client.json`, then negotiates protocol/capabilities through authenticated `hello`. The API compilation target is independent of the actual Host version returned by hello. The bounded schema 1 adapter retains the known old SDK wire value. See the [session protocol](SESSION-PROTOCOL.md) for fields, compatibility, lifecycle and unavailable/timeout diagnostics; published availability remains with the release records.

Runtime atomically consumes the startup descriptor once. The client credential remains available for explicit `snapshot` and `reload` requests until expiry or `session clear`. Expired matching state is cleared on the next prepare/request; malformed or mismatched state is preserved and requires explicit clear. Tokens are sent only inside authenticated JSONL pipe requests and are redacted from human/JSON reports, Runtime messages, and response values.

Each business request includes the exact UniqueID, absolute selected root, a fresh request ID, and the current `DTMAPI-FileTree-SHA256-v1` hash. Response protocol/Host/session/request/operation/owner identities must match the negotiated session. Transport failures report stable codes such as `host-unavailable`, `handshake-timeout`, `pipe-response-timeout`, and `pipe-response-identity-mismatch`; absence alone never implies an upgrade requirement. Runtime outcomes remain `ok`, `rejected`, `restart-required`, or `error`. A valid `restart-required` response carries an explicit warning and still requires a game restart.

The CLI never launches the game, installs a watcher, polls for Runtime startup, uploads DLL/native data, or sends a request without an unexpired protected token. Custom Animals, CodeMod DLLs, official-native JSON, and unknown formats remain restart-required; Runtime owns the only reviewed reload implementation.

## Read-only Doctor

`dtmapi-author symbols <DLL> [--pdb <path>] --json` verifies the portable PDB identity against the DLL's CodeView GUID/stamp and reports both hashes, resident-comparable module MVID and source document names. Missing or mismatched symbols return SDK191. This proves a matching artifact pair, not debugger attach. Source paths in SDK-built stacks are relative to the configured source directory (normally `src`); keep the exact source/build report with the DLL and PDB. Debug packages include symbols; for Release use `--symbols true` when packing.

In the current Windows shipping Mono candidate, the tested Entry exception identified the exact `ModEntry.cs` throw line. The tested event exception identified its callback's closing line instead of the throw line, despite matching symbols. Use the owner, event name, error message and paired source together; exact event throw-line accuracy is not promised. Event errors are retained by `helper.Diagnostics.GetErrors()` and the existing `ExportLogs()` report; they are not necessarily repeated in the text log. Inspect the Errors page or export while that process is still running. Exported reports are local and can contain identifiable machine paths, historical logs and crash dumps with memory contents; inspect the ZIP before sharing. Reports are not automatically uploaded. No archive, complete Mod configuration or session credential file entries were found in the tested export; this is not a privacy guarantee for dump contents. Breakpoint attach is a separate host capability and is not promised by PDB generation.

Live `session snapshot` separates `officialEnabled`, `ownerActive`, `diskEntrySha256`, `diskEntryMvid` and `residentEntryMvid`. `residentMatchesDiskMvid` compares module identities, not a hash of Mono memory; an unavailable resident observation is reported explicitly. A failed Entry can leave a resident assembly even though its owner is inactive. Code updates and symbol replacement require a restart.

Packages include all current native upload localization fields in `info.json` so native discovery does not migrate the file and invalidate exact receipts. If a pre-fix candidate was already rewritten by the game, status/update correctly refuses the drift; do not force/adopt it. Preserve evidence and restore the exact known test asset only within its original authorization before recovery. Newly generated packages need no such repair.

`dtmapi-author doctor <path>` prints a human report; add `--json` for the Doctor report schema. Doctor is a `0.1.0` support library inside the single Author SDK CLI, not a second apphost. It uses `PEReader` and shared-read file streams, never `Assembly.Load`, and never moves, deletes, adopts, enables, disables, or executes inspected binaries.

## Fixed compatibility payload

The release pipeline stages these files next to the self-contained CLI under `compatibility/0.5.5`:

- `compatibility.json`, containing an exact relative-path/SHA-256 inventory;
- the DTMAPI-authored `DTMAPI.Abstractions.dll` built for Runtime 0.5.5, not copied from a player install;
- `DTMAPI.Author.props`;
- the `NETStandard.Library 2.0.3` `netstandard2.0` reference assemblies and their license/notice files.

The CLI embeds `compatibility.contract.json` as its independent trust anchor. It verifies the fixed Abstractions, props, license, notice, exact 114-file reference inventory, every release-manifest hash, all required kinds, and the complete no-extras tree. Replacing both `compatibility.json` and its payload cannot replace the embedded contract. A missing or modified compatibility payload fails closed.

Runtime source may advance while this 0.5.5 authoring payload remains frozen. Run `tools/scripts/prepare-author-sdk-compatibility.ps1` to build its exact DLL from the frozen DTMAPI-owned source inputs in the repository (`author-sdk/compatibility/0.5.5/source-build.json`) and prepare the complete payload under `.tools/author-sdk-compatibility/0.5.5`. A shallow checkout or source archive is sufficient. The recipe fixes the original 16 compilation inputs, Release settings, PathMap and exact .NET SDK `8.0.421`; every resulting DLL must match the existing contract SHA-256. It does not compile the current Runtime interfaces as the old target.

The full SDK release builder calls this same preparation step and reuses a verified payload. An explicit `-FrozenAbstractionsDll`/`DTMAPI_AUTHOR_SDK_FROZEN_ABSTRACTIONS_DLL` remains available, with exact SHA verification; there is no implicit search of `dist`, sibling retained artifacts or player installations. `-Check` verifies preparation without building or downloading. For focused source tests, prepare this payload first; `DTMAPI_AUTHOR_COMPAT_ROOT` may select another explicit output root. NETStandard.Library `2.0.3` is obtained from the public NuGet source into the local cache and checked against the existing reference/license/notice hashes. No third-party binary or game material is tracked in the frozen source directory.
