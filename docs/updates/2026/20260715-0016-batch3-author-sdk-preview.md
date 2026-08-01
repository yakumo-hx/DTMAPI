# 20260715-0016 Batch 3 Author SDK Preview

## Metadata

- Update ID: `20260715-0016`
- Date: 2026-07-15
- Lifecycle Status: `verified`
- Validation Level: `docs,source,unit,runtime`
- Runtime Validation: `passed`
- Related Issue State: `none`
- Area: author-sdk/doctor/templates/packaging/receipts/source-authority/reload
- Target: independent DTMAPI Author SDK `0.1.0`, targeting DTMAPI Runtime `0.5.5`
- Source: after closing and committing Batch 2, complete Batch 3 under the frozen SDK A1-I1 defaults
- Primary review: `docs/reviews/code/2026/20260715-0008-batch3-author-sdk-source-reload-boundary-review.md`
- Route: `docs/reviews/code/2026/20260712-0003-dtmapi-full-boundary-audit.md`

## Outcome

Batch 3 now implements the complete Author SDK `0.1.0` preview as three owned slices:

1. a self-contained Windows x64 `dtmapi-author` CLI, CodeMod and ContentPack templates, fixed offline compatibility payload, deterministic packaging and read-only PE Doctor;
2. dual receipt/external-journal deploy, update, withdraw and explicit recovery, plus Player Workshop, Local Development, Workshop Validation and Player Reproduction source modes;
3. an explicitly prepared short-lived author session with authenticated, bounded Runtime-thread requests and transactional last-good Audio replacement reload.

It does not publish/upload Workshop items, store Steam credentials, adopt/force unknown trees, add a public API, hot-reload DLLs, Custom Animals or native official JSON, close ISSUE-010/ISSUE-011, or provide GC evidence.

## Frozen Boundary And Safety

- The Author SDK is .NET 8 tooling; generated game-loaded CodeMods remain `netstandard2.0`.
- `manifest.json` owns identity, version, dependencies and minimum Runtime. `info.json` is a projection and receipts are package ownership proof only.
- SDK `0.1.0` validates new `MinimumDTMApiVersion` values as strict numeric `major.minor.patch`; values `<= 0.5.5` may be packaged/deployed, future, two-part, labeled and leading-zero forms fail. This is an Author SDK 0.1 authoring boundary and does not narrow Runtime compatibility parsing for historical manifests.
- The compatibility payload contains the fixed Release `DTMAPI.Abstractions.dll`, NETStandard.Library `2.0.3` references and pinned compiler assets. CodeMod builds never consume the player's Runtime DLL.
- The Doctor reads PE metadata and files only. It identifies misplaced BepInEx plugins, native/unknown assemblies and all four retained Lamp compatibility types, but never writes or repairs.
- Deploy/update/withdraw accept only a new target or an exact package-local receipt plus package-external journal match. Same-volume recovery material remains after commit; no force/adopt path exists.
- Local Development state stores one exact root and FileTree hash outside the package. Workshop Validation stores no invented offline root/hash: active author session plus `ModManager` native subscription/install-root authority select the source, and each request then binds and rehashes the current exact root.
- Ordinary player startup with no descriptor creates no author listener, watcher, timer or file poll. The explicitly prepared listener is short-lived, authenticated and bounded. The first native startup `ReturnHome` is retained only once when no save/current load/request has been observed, so the author can connect. A parsed request cancels that one-time exception, so the same or any later `ReturnedToTitle` closes it; expiry or shutdown also closes it.
- Audio replacement reload stages an owner generation, validates supported files, rehashes before commit and preserves the previous generation on invalid JSON or asset failure. CodeMod DLLs, Custom Animals, native JSON and unknown formats return deterministic `restart-required`.
- Lamp remains the Batch 2 retired compatibility shell. SDK source scanning now warns for `ILampControlApi`, `LampManualToggleOptions`, `LampManualToggleRegisterResult` and `LampManualToggleState` even when a Mod references only a DTO.
- SDK PE `ProductVersion` is fixed to `0.1.0` with source-revision injection disabled; the release checker asserts FileVersion/ProductVersion for all five SDK PE files, so committing no longer changes package bytes merely by changing HEAD.

## Native Owner And Hook Boundary

The required owner is `DolocTown.Config.ModManager`: `GetSubscribedMods()` supplies subscribed IDs and `GetSubscribedModDirectory(...)` supplies the installed root. `GetAllValidModInfos()` is optional enrichment; failure preserves the required ID/root snapshot with nullable native enablement and never invents a boolean. Initial capture is a one-shot existing PlayerLoop first-frame boundary after native `GameManager.Awake`, before Core manifest discovery and before Harmony initialization. The existing `ReloadMods()` Postfix recaptures first, then calls `NotifyWorkshopModListChanged`. No direct `SteamUGC` call, readiness loop or file poll was added. Canonical details live in `docs/hook-map/focused/WorkshopSourceAuthority.md`.

## Changed Files

This Update owns the following exact 97-file change surface.

- Root/build integration: `DTMAPI.sln`; `Directory.Build.props`.
- Author assets: `author-sdk/README.md`; `author-sdk/THIRD-PARTY-NOTICES.md`; `author-sdk/compatibility/0.5.5/DTMAPI.Author.props`; `author-sdk/compatibility/0.5.5/compatibility.contract.json`.
- Author schemas: `author-sdk/schemas/author-session-descriptor.schema.json`; `author-sdk/schemas/author-session-wire.schema.json`; `author-sdk/schemas/deployment-journal.schema.json`; `author-sdk/schemas/deployment-receipt.schema.json`; `author-sdk/schemas/doctor-report.schema.json`; `author-sdk/schemas/dtmapi-author.schema.json`; `author-sdk/schemas/manifest.schema.json`; `author-sdk/schemas/package-report.schema.json`; `author-sdk/schemas/source-state.schema.json`.
- CodeMod template: `author-sdk/templates/codemod/__UNIQUE_ID__.csproj.template`; `author-sdk/templates/codemod/content/README.txt.template`; `author-sdk/templates/codemod/dtmapi.author.json.template`; `author-sdk/templates/codemod/manifest.json.template`; `author-sdk/templates/codemod/src/ModEntry.cs.template`.
- ContentPack template: `author-sdk/templates/contentpack/content/README.txt.template`; `author-sdk/templates/contentpack/dtmapi.author.json.template`; `author-sdk/templates/contentpack/manifest.json.template`.
- CLI: `src/DTMAPI.AuthorSdk/AuthorApplication.cs`; `AuthorFileTreeDigest.cs`; `AuthorSessionService.cs`; `AuthorStateInfrastructure.cs`; `CodeModBuilder.cs`; `CommandLine.cs`; `CompatibilityAssets.cs`; `DTMAPI.AuthorSdk.csproj`; `DeploymentPackage.cs`; `DeploymentService.cs`; `DeterministicPackager.cs`; `DoctorCommandService.cs`; `GlobalUsings.cs`; `JsonSupport.cs`; `PathSafety.cs`; `Program.cs`; `ProjectValidator.cs`; `SourceStateService.cs`; `TemplateCreator.cs`.
- Shared author contracts: `src/DTMAPI.Authoring.Contracts/AuthorContracts.cs`; `DTMAPI.Authoring.Contracts.csproj`; `GlobalUsings.cs`.
- Doctor: `src/DTMAPI.InstallDoctor/DTMAPI.InstallDoctor.csproj`; `DeprecatedApiGuidance.cs`; `DoctorEngine.cs`; `DoctorModels.cs`; `DoctorReportFormatter.cs`; `ManifestProbe.cs`.
- Tooling metadata: `src/DTMAPI.Tooling.Metadata/DTMAPI.Tooling.Metadata.csproj`; `MetadataModels.cs`; `PeMetadataInspector.cs`; `ReadOnlyFiles.cs`; `TreeHasher.cs`.
- Core source/session additions: `src/DTMAPI.Core/Runtime/AuthorFileTreeDigest.cs`; `AuthorSessionContracts.cs`; `AuthorSessionDescriptorStore.cs`; `AuthorSessionHost.cs`; `AuthorSessionWire.cs`; `AuthorSourceState.cs`.
- Existing Runtime integration: `src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs`; `src/DTMAPI.Core/Manifesting/ManifestModels.cs`; `src/DTMAPI.Core/Manifesting/ManifestReader.cs`; `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`.
- GameBridge: `src/DTMAPI.GameBridge.DolocTown/AuthorSessionReloadBridge.cs`; `WorkshopSubscriptionSnapshotBridge.cs`; `DolocTownGameBridge.cs`; `Features/AudioReplacement/AudioReplacementFeature.cs`; `Features/AudioReplacement/AudioReplacementService.cs`; `Hooking/DolocTownHookCallbacks.cs`.
- Author tests: `tests/DTMAPI.AuthorSdk.Tests/DTMAPI.AuthorSdk.Tests.csproj`; `GlobalUsings.cs`; `Program.cs`.
- Doctor tests: `tests/DTMAPI.InstallDoctor.Tests/DTMAPI.InstallDoctor.Tests.csproj`; `Fixtures/BepInExPluginFixture/BepInExPluginFixture.csproj`; `Fixtures/BepInExPluginFixture/FixturePlugin.cs`; `Fixtures/CodeModFixture/CodeModFixture.csproj`; `Fixtures/CodeModFixture/FixtureMod.cs`; `Program.cs`.
- Runtime tests: `tests/DTMAPI.UnitTests/Program.cs`.
- Release scripts: `tools/scripts/author-sdk-release-common.ps1`; `build-author-sdk.ps1`; `check-author-sdk-release.ps1`; `test-author-sdk-portable.ps1`; plus modified `analyze-startup-evidence.ps1`; `build.ps1`; `test.ps1`.
- Governance: `docs/reviews/code/2026/20260715-0008-batch3-author-sdk-source-reload-boundary-review.md`; this Update; `docs/updates/INDEX-2026-07.md`; `docs/hook-map/focused/WorkshopSourceAuthority.md`; `docs/hook-map/README.md`; `docs/debug/regressions/smoke-matrix.md`; `docs/debug/issues/ISSUE-004-steam-launch-stuck.md`; `docs/debug/evidence-retention-allowlist.json`.

No public abstraction changed, so `docs/api/public-api-matrix.md` is intentionally unchanged.

## Validation

### Source, unit and artifact gates

- Release builds for UnitTests and AuthorSdk.Tests completed with zero warnings/errors; `DTMAPI.UnitTests: OK` and `DTMAPI Author SDK tests: OK`.
- Tests cover both templates, deterministic pack/build, hostile paths/references, exact manifest/info projection, lower/future/malformed minimums, DTO-only Lamp scan, Doctor damaged/native/unknown/misplaced/deprecated cases, FileTree vectors, dual receipt/journal happy/drift/fault/concurrency/multi-root recovery, all four source modes, session authentication/replay/concurrency/expiry/title/shutdown and valid-invalid-valid Audio generations.
- Two independent self-contained publishes under different space/non-ASCII output roots produced the same 359-entry canonical ZIP. Both sidecars and release checks passed; the second package also passed the isolated empty-PATH/DOTNET_ROOT/USERPROFILE/HOME/NUGET portable gate.
- Final Author SDK SHA-256: `12c246eff17557a1536357677b50b87aa83455aa47712f1930f7d72749a8929c`.
- Fixed compatibility `DTMAPI.Abstractions.dll` SHA-256: `1773527ab8d28a904c6c185fd8bc4a1502746cb7fa228e27ab80d15370357d44`.
- `tools/scripts/test.ps1 -Configuration Release` passed after implementation and the generated evidence-retention allowlist reached their intended pre-commit state, including source, Unit, Doctor, Author SDK, package/release, ABI/catalog and governance gates. The later record-only closure was rechecked with diff and governance gates before commit.

### Runtime gates

- `GAME-SMOKE/20260715-185255` proved the corrected `PlayerLoop.FirstFrame` initial boundary: native source authority was `True/43` before manifest discovery, slot 3/title/process gates passed and `Bootstrap.StartRuntime totalMs=1128`.
- `GAME-SMOKE/20260715-190725/author-session-matrix.md` proved snapshot A, invalid JSON rejection with `retainedPreviousGeneration=True` at generation 1, valid B commit at generation 2, exact disk restoration to A/hash `5CECA3EBE312D15D987C585ADA04E34B5FE429830E0520271471C4E6AF2F8C95`, slot 3, real post-save title closure (`processed=3`, `handlerFailures=0`) and no residual process. Its generic aggregate is Failed only because the deliberate generation mutations violate the unrelated `TitleIdleResourceGrowth` no-mutation premise; the scoped Author matrix, SaveLoaded and SaveLoadCycle gates passed.
- `GAME-SMOKE/20260715-191256` is the final normal-player gate: Steam, no HookProbe, no descriptor/listener/watcher/timer/file poll, native `True/43` at startup and after `ModManager.ReloadMods.Postfix`, no-op owner refresh, slot 3/title lifecycle and process exit all passed. It recorded one missing-frame fallback warning, one recovered frame-driver stall and two resubscriptions; no new Debug issue, fatal popup, crash dump or residual process was found.
- `182252` remains a retained superseded root-cause row because it uniquely captures the too-early `Awake` boundary and native `False/0`. The `184017`, `184816` and `185821` startup-window attempts are unretained diagnostics superseded by `190725`; `185600` passed generic slot/title behavior but processed no session request and is likewise not an acceptance or retention anchor.
- The test Author package was restored to its original bytes before exact receipt-bound withdrawal. Source selection and session credentials were cleared. The three pre-existing loose user test Mods were moved back from the verified backup, smoke settings were absent, no game process remained and the shared Runtime lock was released.

These minute-scale runs are not GC evidence. ISSUE-010 and ISSUE-011 remain open. Known per-frame no-consumer dispatch and Smoke/GameBridge polling debts remain Batch 4/5 work.

## Rollback

Revert this exact file inventory as one Batch 3 unit. Runtime source-selection rollback must remove external author state, descriptor/session transport and scanner priority together. Workshop rollback must revert the Bootstrap native-ready capture, GameBridge scalar projection and Core consumption together. Audio rollback must revert staging, last-good generation and reload bridge together. Do not delete/move a user, Workshop or external package as rollback; Author deployment recovery is receipt/journal-owned and retains same-volume recovery material.

## Follow-up

- Batch 4/5 own no-consumer dispatch and polling/hot-path debt.
- AutoFishing and ActionSpeed GC ladders remain independent future evidence work; no Batch 3 smoke substitutes for them.
- DLL/Custom Animals/native official JSON reload remains restart-required unless a later native-owner/API project proves a safe transaction.
- A future SDK release may revisit its authoring-version grammar, but Runtime historical manifest compatibility must not be silently narrowed.
