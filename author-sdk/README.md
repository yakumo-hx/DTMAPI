# DTMAPI Author SDK

`dtmapi-author` is the single authoring authority for SDK `0.1.0`. It compiles against the frozen DTMAPI `0.5.5` public API payload; an identity-bound Advanced policy may require a newer Runtime, currently no later than `0.6.1`. The distributed Windows x64 ZIP is self-contained on .NET 8; generated game-loaded CodeMods remain `netstandard2.0`.

SDK `0.1.0` exposes the ordinary **Strict CodeMod** lane and the policy-bound **Advanced CodeMod** machinery defined in [`PROJECT.md`](../PROJECT.md) and the [Batch 6 managed Mod identity contract](../docs/architecture/batch6-managed-mod-identity-contract.md). The tracked registry contains the G2 synthetic policy plus identity-specific policies for the exact admitted first-party products. This is not general Advanced admission: an arbitrary author/ID has no accepted native-reference policy, and copying an admitted product's declaration or receipt fails closed.

Strict projects continue to fail closed under `SDK160` when source or project inputs try to consume native game, Unity, Harmony, or BepInEx assemblies. Advanced intent must originate from schema-2 `dtmapi.author.json`; hand-editing only the Runtime manifest, inventing a reference policy, or hand-packaging a receipt is not an admitted path.

`DTMAPI.Abstractions` is a public assembly, not a promise that every public type is Stable. Before adopting a surface, check both stability and disposition in [`docs/api/public-api-matrix.md`](../docs/api/public-api-matrix.md). Diagnostic, Frozen, Disabled, DTMAPI-internal, and Proposed contracts are not ordinary new-mod dependencies.

## Commands

```text
dtmapi-author new codemod MyMod --id Author.MyMod --name "My Mod" --author Author
dtmapi-author new codemod DTMAPI.AdvancedFixture --id DTMAPI.AdvancedFixture --name "G2 Fixture" --author DTMAPI --code-mod-kind Advanced
dtmapi-author new contentpack MyPack --id Author.MyPack --name "My Pack" --author Author
dtmapi-author validate MyMod
dtmapi-author build MyMod
dtmapi-author pack MyMod
dtmapi-author hash MyMod/dist/Author.MyMod-0.1.0.zip
dtmapi-author deploy MyMod/dist/Author.MyMod-0.1.0.zip --game-root "D:\\Games\\DolocTown"  # paused: SDK003
dtmapi-author update MyMod/dist/Author.MyMod-0.2.0.zip --game-root "D:\\Games\\DolocTown"  # paused: SDK003
dtmapi-author withdraw Author.MyMod --game-root "D:\\Games\\DolocTown"
dtmapi-author recover Author.MyMod --game-root "D:\\Games\\DolocTown"
dtmapi-author deployment-status Author.MyMod --game-root "D:\\Games\\DolocTown"
dtmapi-author session prepare --game-root "D:\\Games\\DolocTown"
dtmapi-author session snapshot Author.MyMod "D:\\Games\\DolocTown\\Mods\\Author.MyMod" --game-root "D:\\Games\\DolocTown"
dtmapi-author session reload Author.MyMod "D:\\Games\\DolocTown\\Mods\\Author.MyMod" --game-root "D:\\Games\\DolocTown"
dtmapi-author session clear --game-root "D:\\Games\\DolocTown"
dtmapi-author doctor "D:\\Games\\DolocTown"
```

Add `--json` for a machine-readable report. Strict `build` compiles in-process against the SDK's hash-fixed compatibility payload, so it does not need an installed `dotnet`, a NuGet cache, the game directory, or the player's mutable Runtime DLL. Advanced `build` and `pack` additionally require an explicit `--game-root`; the CLI accepts only the tracked Steam build and exact reference path/length/SHA-256 inventory. It never searches ambient game installs, PATH, package caches, old workspaces, or temporary directories for native assemblies.

`pack` emits the official package shape:

```text
Package/
  info.json
  Content/DTMAPI/
    manifest.json
    Author.Mod.dll       # CodeMod only
    dtmapi-advanced-references.json # Advanced CodeMod only
    dtmapi-package.json  # metadata only; never an ownership receipt
    ...content files
```

`manifest.json` remains authoritative for Mod identity, Mod version, dependencies, and minimum Runtime. `dtmapi.author.json` is an SDK-only pre-1.0 build/publish input and does not replace the Runtime manifest. The package's `info.json` is generated from both inputs and must not be hand-maintained in the author project.

For every accepted Advanced identity, the receipt binds its exact tracked policy, game build, official reference paths/lengths/hashes, manifest bytes, entry DLL bytes, `netstandard2.0`, canonical Harmony owner, and policy-specific minimum Runtime. The live Author SDK registry is the only selectable authoring set. Core and Doctor separately embed a byte-exact historical acceptance registry for already-issued packages; those historical rows can never be selected by `new`, `validate`, `build`, or `pack`. G2 remains the synthetic proof; real-product acceptance is identity-specific and owned by the Batch 6 contract. The package contains only the Mod entry DLL: `Assembly-CSharp.dll`, Unity, Harmony, and BepInEx files are never copied into it. Because the current game assembly names a newer transitive netstandard facade, compilation uses a separately hash-fixed, policy-limited reference surface after the real game DLL has passed exact verification; emitted Mods are still checked as `netstandard2.0` and must carry exactly the declared native AssemblyRefs.

The SDK has no upload, credential, force, or adopt operation. DTMAPI-managed Strict or Advanced CodeMods must never be created, built, packaged, or deployed under `BepInEx/plugins`; third-party plugins placed there are External BepInEx Plugins outside DTMAPI ownership.

## Legacy receipt-bound deployment recovery

DTMAPI 0.6.0 no longer discovers the historical `game/Mods/<UniqueID>` development root. Because Author SDK `0.1.0` has not been released with an official-`MODS` transaction, new `deploy`, `update`, `install-local`, and `source local select` mutations are paused and return `SDK003` before reading a package or changing the game/Author state. Do not use that directory as player-load or release evidence.

Existing receipt-bound deployments remain recoverable. `deployment-status` and `install-local-status` are read-only; `recover` restores an interrupted prepared transaction; `withdraw` moves an exactly verified committed package into retained same-volume recovery; and `source local clear` may remove a stale historical selection without moving package files. These compatibility commands do not make `game/Mods` a Runtime source.

The historical package-local `.dtmapi-author-receipt.json`, package-external journal, and complete deployment inventory must agree exactly:

- the package-local receipt;
- the package-external committed journal;
- the complete current deployment inventory, including unknown files and empty directories.

Each game root has an exclusive operation lock. Historical staging, destination, failed, and recovery directories stay on the `Mods` volume so an already prepared transaction can be recovered with its original atomic-move semantics. A mismatch preserves evidence and fails closed. Missing, forged, wrong-root, wrong-path, wrong-ID, wrong-kind, drifted, concurrent, receipt-only, and journal-only states are never adopted.

Current journal writes performed by legacy recovery/withdraw use schema 3. Schema 3 retains the schema-2 deployment identity and inventory fields and adds the nullable `localInstall` transaction which owns the exact journal/source-state preimage and SDK-owned paths of an already prepared compound local install. Package-local receipts and package markers remain schema 2.

The reader retains narrow upgrade paths for exact SDK `0.1.0` schema-1 and prior schema-2 journals already present on a player's machine. A historical schema-1 `CodeMod` is always interpreted as `Strict`, while a historical `ContentPack` remains code-free. Its journal, package-local receipt, legacy package marker, identity, destination, package hash, payload hash and complete inventory must all agree. `deployment-status` is read-only and does not rewrite historical state; `recover` and `withdraw` may write a schema-3 journal while preserving the old receipt authority. New update publication is paused. Schema 1 can never declare or upgrade itself into `Advanced`, and unknown or mixed-version fields fail closed.

`DTMAPI_AUTHOR_STATE_ROOT` may relocate package-external test/CI state. Without it, state is stored under `%LOCALAPPDATA%/DTMAPI/AuthorSdk/state/installations/<game-root-key>`. The key is the lowercase first 16 bytes of SHA-256 over the canonical, uppercase game-root path.

## Source modes

```text
dtmapi-author source local select Author.MyMod <game/Mods/Author.MyMod> --game-root <game>  # paused: SDK003
dtmapi-author source local clear Author.MyMod --game-root <game>
dtmapi-author source workshop prepare Author.MyMod --game-root <game>
dtmapi-author source workshop clear Author.MyMod --game-root <game>
dtmapi-author source reproduction begin --game-root <game>
dtmapi-author source reproduction restore <snapshotId> --game-root <game>
dtmapi-author source status [Author.MyMod] --game-root <game>
```

Historical Local Development state records an exact `DTMAPI-FileTree-SHA256-v1` digest in external `source-state.json`; it is recovery-only in 0.6.0 and grants neither file ownership nor player source selection. New local selection is paused, while status and clear remain available. Player Reproduction first writes an external snapshot, then atomically clears every override and refuses new selections until the exact snapshot is explicitly restored. Workshop Validation only prepares state and reports Runtime-captured native snapshot availability. Offline directory presence is never reported as native Workshop success.

## Explicit Runtime session

`session prepare` must run before the user starts the game. It writes the Core-compatible one-shot `author-session.json` startup descriptor and a separate protected `author-session-client.json` credential under the same installation-state root. Both bind schema 1, Runtime `0.5.5`, canonical game root, GUID-N session ID, a derived `dtmapi-author-<game-root-key>-<session-id>` pipe, and a random 256-bit base64url token to a ten-minute UTC lifetime. On Windows, both files have protected ACLs granting only the current user; Unix hosts use mode `0600`.

Runtime atomically consumes the startup descriptor once. The client credential remains available for explicit `snapshot` and `reload` requests until expiry or `session clear`. Expired matching state is cleared on the next prepare/request; malformed or mismatched state is preserved and requires explicit clear. Tokens are sent only inside authenticated JSONL pipe requests and are redacted from human/JSON reports, Runtime messages, and response values.

Each request includes the exact UniqueID, absolute selected root, a fresh request ID, and the current `DTMAPI-FileTree-SHA256-v1` hash. Response protocol/session/request/operation/owner identities must match exactly. Transport failures report stable codes such as `pipe-connect-timeout`, `pipe-response-timeout`, and `pipe-response-identity-mismatch`; Runtime outcomes remain `ok`, `rejected`, `restart-required`, or `error`. A valid `restart-required` response is a successful authenticated boundary with an explicit warning, not a hot-reload claim.

The CLI never launches the game, installs a watcher, polls for Runtime startup, uploads DLL/native data, or sends a request without an unexpired protected token. Custom Animals, CodeMod DLLs, official-native JSON, and unknown formats remain restart-required; Runtime owns the only reviewed reload implementation.

## Read-only Doctor

`dtmapi-author doctor <path>` prints a human report; add `--json` for the Doctor report schema. Doctor is a `0.1.0` support library inside the single Author SDK CLI, not a second apphost. It uses `PEReader` and shared-read file streams, never `Assembly.Load`, and never moves, deletes, adopts, enables, disables, or executes inspected binaries.

## Fixed compatibility payload

The release pipeline stages these files next to the self-contained CLI under `compatibility/0.5.5`:

- `compatibility.json`, containing an exact relative-path/SHA-256 inventory;
- the DTMAPI-authored `DTMAPI.Abstractions.dll` built for Runtime 0.5.5, not copied from a player install;
- `DTMAPI.Author.props`;
- the `NETStandard.Library 2.0.3` `netstandard2.0` reference assemblies and their license/notice files.

The CLI embeds `compatibility.contract.json` as its independent trust anchor. It verifies the fixed Abstractions, props, license, notice, exact 114-file reference inventory, every release-manifest hash, all required kinds, and the complete no-extras tree. Replacing both `compatibility.json` and its payload cannot replace the embedded contract. A missing or modified compatibility payload fails closed.

Runtime source may advance while this 0.5.5 authoring payload remains frozen. The release builder therefore accepts an explicit `-FrozenAbstractionsDll`/`DTMAPI_AUTHOR_SDK_FROZEN_ABSTRACTIONS_DLL`, then checks only bounded staged-package and sibling `DTMAPI-retained-artifacts/release-candidates` authorities before falling back to the current build. Every candidate must match the contract SHA-256 exactly; the builder does not search a player game, arbitrary workspaces, or temporary directories.
