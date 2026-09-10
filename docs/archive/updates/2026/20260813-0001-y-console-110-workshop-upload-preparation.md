# Y Console 1.1.0 Three-Language Workshop Upload Preparation

## Metadata

- Update ID: `20260813-0001`
- Date: `2026-08-13`
- Lifecycle Status: `verified`
- Validation Level: `docs, source, unit, player`
- Runtime Validation: `not-required`
- Related Issue State: `open`
- Area: debugconsole/workshop/localization/release/package
- Source Request: after manual acceptance, prepare and print the existing Y-console Workshop upload directory with authoritative Simplified Chinese, Traditional Chinese and English descriptions; do not perform the Steam upload

## Source Request

The player reported that manual testing was basically green, accepted the current animal limits and requested the Y-console 1.1.0 upload directory. The supplied Simplified Chinese description adds the full feature list, the `0616`, `0617` and `0813` notes, and explicit warnings for locked-area teleport, Old City Guardian and post-boss saving. This is explicit authorization to prepare one existing Workshop update for item `3742714442`; it is not authorization for any other Workshop mutation and does not ask Codex to submit to Steam.

## Owning Review

- [Y 键控制台 1.1.0 旧城守护者熔断与发布限制复核](../../reviews/manual-qa/2026/20260813-0003-y-console-space-ship-release-limitation.md)
- [Y Console 1.1.0 Semantic UI and ProductNative Actions](20260812-0001-y-console-semantic-ui-productnative.md)

## Scope

- Preserve the user's Simplified Chinese text exactly as the primary public description, with only JSON newline encoding.
- Provide natural Traditional Chinese and English translations carrying the same feature and risk statements.
- Regenerate the Advanced package through the tracked Author SDK and exact `doloctown-24456188-debugconsole-v1` policy; do not hand-edit generated manifest, receipt, DLL or upload `info.json`.
- Preserve the existing Workshop identity/control file and prepare exactly one local official upload folder.
- Record one exact Catalog mutation entrypoint only after the final directory hash is known. Do not upload or edit the Steam subscription cache.

## Validation Plan

- Parse and compare the three descriptions across Author source, official-info projection and generated upload `info.json`.
- Re-run focused Author SDK, Product Catalog, native trace, Runtime floor, retained ABI and document checks affected by the metadata/authorization change. Record why the all-product dual-build release contract is not repeated for a description-only projection when the exact Y-console SDK package has already been regenerated and audited.
- Under the shared Runtime lock, atomically replace only the existing local official Y-console folder; verify exact file set/tree, preserved `workshop.json`, enabled Local source, disabled Workshop source, zero game process and clean lock release.
- Keep the Update `implemented` after preparation. Only a post-upload native Steam manifest and downloaded-subscription byte audit may close it as `verified`.

## Implemented Result

- The supplied Simplified Chinese description is the exact `schinese` authority in Author metadata, `official-info.json`, the Simplified publication projection and the generated upload `info.json`. Equivalent Traditional Chinese and English descriptions carry the same features, teleport warning, Old City Guardian limitation, Boss save warning and `0616` / `0617` / `0813` notes.
- The Author SDK regenerated Y-console `1.1.0` through compatibility target `0.5.5` and exact native policy `doloctown-24456188-debugconsole-v1`. The game-loaded DLL remains the already player-tested `netstandard2.0` candidate; this release preparation changes no Product code.
- Catalog grants one bounded existing-item update for `y-console / 3742714442 / 1.1.0` and binds it to the exact package and upload-tree hashes. New Workshop items, identity/folder moves, mass edits and every other existing-item update remain blocked.
- Under the shared Runtime lock, a same-volume validated stage atomically replaced only the local official `DTMAPI_YKeyConsole` folder. Its original `workshop.json` was preserved byte-for-byte. The local source remains enabled at priority `9`; the current Workshop source remains disabled at priority `-1`.
- Preparation result was `PreparedNotSubmitted`. After the user's manual submission, Steam installed the exact authorized tree under a new converged manifest; the post-publication result is recorded below. No Codex submit action ran and no player save or committed sidecar was touched.

## Changed Files

- Three-language public metadata: `products/first-party/DebugConsole/dtmapi.author.json`, `products/first-party/DebugConsole/official-info.json`, and the Y-console row in `tools/release/dtmapi-mod-publish-zh.json`.
- Release authority and enforcement: `tools/release/dtmapi-product-catalog.json`, `tools/release/current-subscription-manifest.json`, the mechanically regenerated `docs/architecture/managed-product-admission-registry.md`, and `tools/scripts/check-product-catalog.ps1`.
- Durable review/lifecycle records: this Update, `docs/updates/INDEX-2026-08.md`, and `docs/reviews/manual-qa/2026/20260813-0003-y-console-space-ship-release-limitation.md`.
- External prepared artifact: `%USERPROFILE%\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI_YKeyConsole`; its recoverable preimage is retained under `%USERPROFILE%\AppData\LocalLow\RedSawGames\DolocTown\.dtmapi-official-backups`.

## Validation

- Three-language source/projection audit: PASS. `schinese`, `tchinese` and `english` are the exact and only upload-description keys; Author and official projections match generated `info.json` byte-for-byte per language. Text SHA-256 values are respectively `31B76475ECFA40983BF8C383D5F78E66DB42CEE034589540738F9DD03874A2A0`, `C7823A156CB3E7EF2C54F683785D029FA287E6B208BE5F162C5B6410A6D20F9E` and `BB6F3EF80C57FE6ADF9DB0750E9B958C8FF485417A3BB133041D9872CEBCA2C3`.
- Focused Author SDK build: PASS. ZIP SHA-256 is `E5AC82C723FD8EA5371725DC28DE2E53D54C5145F46E234935EB932AD714A955`; it contains the unchanged `220160`-byte DLL SHA-256 `DBC0540A0187BB9B620AAE504BBF1F5569C23796686939A085B4328FF39B3BE3`, exact Advanced receipt SHA-256 `52FA9D72D8B6B90532634BA00EE95DE020F954D9B53A7A5153036D7940C6166A`, version `1.1.0`, minimum Runtime `0.6.1`, nine UI locales and all ten official content sidecars.
- Prepared-folder audit: PASS. Exact path is `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI_YKeyConsole`; it contains `27` files, `608799` bytes, one DLL, zero reparse points and zero alternate streams. Its `DTMAPI-Retained-SHA256SUMS-v1` tree is `B1C384A6ECF8AEA23ABB3684EA4B326C1F1350CE183798528C2793C9F3B3259C`; `info.json` is `07EF7207983C7FF20DA3E83554BB34CE40A074D10AF282350B9A945790A60333`; preserved `workshop.json` is `514C3829DA8FABDA78EC0B3934408F9B73D3D40089FAEC7DB6E6B011A953FA6F`.
- Native enablement audit: PASS against the authoritative `SAVE/mod_infos.json` (`0247C4C4B7527CA96DA6EB7BFCB546746CA74A96D13E08233F4B53A51E7A909D`). `Local.DTMAPI_YKeyConsole` is enabled at priority `9`; `Workshop.3742714442` is disabled at priority `-1`.
- `check-product-catalog.ps1`: PASS (`27` products, `11` public, `22` Workshop items, `48` API rows). Before upload the checker required the exact one-entry Y-console exception and rejected incidental authorization under any other target; after closeout it binds the exact observed public artifact and requires zero remaining upload authorization.
- `test-dtmapi-060-debugconsole-native-trace.ps1` and `test-dtmapi-060-runtime-floor-compatibility.ps1`: PASS. Current public monster/animal native-owner bytes still match the fixed policy and the product floor remains `0.6.1`.
- `test-synthetic-retained-abi.ps1 -NoBuild`, `test-retained-abi-input-resolution.ps1`, and `test-retained-release-abi.ps1 -NoBuild`: PASS. Removed public API count is zero and all `463/463` retained public-product member references resolve.
- The broad `check-release-contract.ps1` all-product deterministic branch was not repeated because it requires two complete exact artifact roots for all nine release products. This metadata-only preparation instead rebuilt the affected Y-console package, proved its exact package/tree/receipt/DLL identities, and ran the shared Catalog and ABI gates; no other Product artifact was changed.
- No new game launch was required: the entry DLL is byte-identical to the candidate already accepted by the player's manual test and the locked `NoNativeSave` run recorded by Update `20260812-0001`. The known `space_ship` limitation remains open and is disclosed rather than represented as fixed.
- `check-doc-governance.ps1` and `git diff --check`: PASS after final lifecycle normalization.

## Post-Publication Subscription Closeout

- At `2026-08-13T01:53:07Z`, the native Steam manifest at `<SteamLibrary>/steamapps/workshop/appworkshop_2285550.acf` reported `NeedsUpdate=0`, `NeedsDownload=0`, and identical installed/latest manifest `6025676757483117589` for Workshop `3742714442`. Its item size is `608799` bytes and its update epoch `1786585323` resolves to `2026-08-13T01:42:03Z`.
- The complete native ACF observation contains `51` installed and `51` detail members, reports `83618300` bytes on disk, and has SHA-256 `7B8B3843497C0A7E7BDB86A6FFDFB2B02CC48655FB0FEED9BE161B0DE70693C`. Installed and latest manifest/time values agree for Runtime plus all eleven Catalog `PublishedProduct` identities; no pending Workshop state remains.
- The downloaded player artifact at `<SteamLibrary>/steamapps/workshop/content/2285550/3742714442` contains exactly `27` files and `608799` bytes. It matches the prepared upload directory path-for-path, length-for-length and SHA-256-for-SHA-256 with zero extra or missing files. Under the authorization ordering its tree is `B1C384A6ECF8AEA23ABB3684EA4B326C1F1350CE183798528C2793C9F3B3259C`; under `DTMAPI-Published-SHA256SUMS-v1` ordinal ordering its current-public tree is `520C4F7DFF6AC484B0990BCF572AC45A05D01A1FC5BCC8479A5A728465A18DDB`. The different aggregate values are ordering semantics, not byte drift.
- The subscription manifest is `DTMAPI.DebugConsoleMod / 1.1.0 / minimum Runtime 0.6.1 / Advanced`. It binds one `220160`-byte entry DLL at SHA-256 `DBC0540A0187BB9B620AAE504BBF1F5569C23796686939A085B4328FF39B3BE3`, target API `0.5.5`, exact policy `doloctown-24456188-debugconsole-v1` and policy build `24456188`.
- The tree contains nine UI locale files, nine official content localization sidecars plus the item sidecar, and Workshop descriptions for exactly `english`, `schinese` and `tchinese`. `info.json`, preserved `workshop.json` and infinite-fuel item JSON are respectively `07EF7207…0333`, `514C3829…FA6F` and `A23C9BBF…7671`; no reparse point, alternate stream, unexpected executable/script, duplicate DLL or legacy Y-console entry is present.
- This was a read-only player-artifact audit. The skill's Runtime installer BAT/PowerShell stress matrix is not applicable to this Advanced product package, which contains no installer entrypoints. No game launch, Runtime lock, official-folder write, save mutation or Steam subscription-cache write was required.
- Catalog now records the exact observed `1.1.0` current artifact, the current subscription manifest names this Update as latest release authority, and the one-shot upload exception is consumed. The old retained `0.3.1-dtmapi` artifact remains rollback evidence only and does not describe current subscription bytes.
- The final Catalog checker passed under both PowerShell 7 and Windows PowerShell 5.1; generated admission-registry check, document governance (`6745` checks), JSON parsing and `git diff --check` also passed. A second independent read-only subscription snapshot reproduced the same ACF, package, manifest, tree and zero-authorization facts.

## Evidence

- [Old City Guardian release-limitation Review](../../reviews/manual-qa/2026/20260813-0003-y-console-space-ship-release-limitation.md) owns the exact log diagnosis and the decision not to weaken rollback/circuit safety for publication.
- [Y-console 1.1.0 implementation Update](20260812-0001-y-console-semantic-ui-productnative.md) owns Product source, player/manual observations, the final runtime smoke and the unchanged entry-DLL identity.
- The prepared upload folder and retained preimage remain local external evidence. Catalog now owns the exact current published artifact and zero-upload state; `tools/release/current-subscription-manifest.json` owns the converged native Steam observation.

## Rollback Notes

- The retained pre-publication local backup remains recoverable for developer-only comparison, but restoring it cannot change Steam publication facts and must never be copied into the Steam-managed subscription cache.
- A public rollback requires a new user-authorized existing-item update, a new bounded lifecycle Update and a subsequent downloaded-subscription audit. Catalog and current-subscription authority must continue to describe `1.1.0` until Steam actually distributes different bytes.
- A source rollback must not rewrite this observed publication, remove the stable `dtmapi_creative_generator` ID, or claim that Old City Guardian was repaired.

## Follow-Up

- Publication lifecycle is closed. Catalog is back at `ActiveNoUploadAuthorization`; any future Y-console upload requires a new bounded Update and exact existing-item exception.
- Future Old City Guardian work starts with more granular ProductNative host/count/containment diagnostics and an isolated save fixture; do not bypass the current hard circuit in this release.
