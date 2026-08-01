# 20260715-0010 Batch 3 Player Doctor Closure Review

Status: recorded
Date: 2026-07-15
Scope: close the frozen D1 player-facing read-only Doctor boundary without moving Author SDK authority or adding a sixth game-loaded Runtime assembly
Related Update: `docs/updates/2026/20260715-0018-batch3-player-doctor-closure.md`
Route review: `docs/reviews/code/2026/20260715-0009-batch2-batch3-route-and-batch4-entry-review.md`
Primary route: `docs/reviews/code/2026/20260712-0003-dtmapi-full-boundary-audit.md`

## Source Request And Verdict

The user identified one remaining Batch 3 product gap: Doctor existed only inside the Author SDK, while the player `3_check` and `4_collect` paths could not diagnose an ordinary DTMAPI CodeMod misplaced under `BepInEx/plugins`. The frozen D1 and third-round inputs require both an offline path when Runtime cannot load and an in-game summary/export when it can.

The accepted narrow closure is an independent self-contained `dtmapi-player-doctor.exe` with read-only diagnostic authority only. It is packaged and transactionally installed under `DTMAPI/tools/player-doctor`, outside `BepInEx/plugins`; the five game-loaded production DLL set remains unchanged. Runtime invokes that trusted DTMAPI-owned helper at most once during startup with a hard timeout, records one bounded Manager/diagnostics summary, and includes its three bounded reports in normal export. It never calls the helper from `Update`, title refresh, Manager refresh, log export or a timer.

This closes the player surface without shipping `dtmapi-author`, templates, schemas, build, pack, deploy, withdraw, recover, Workshop upload or repair authority.

## Root Cause

- `DTMAPI.InstallDoctor` and `DTMAPI.Tooling.Metadata` were .NET 8 tooling libraries referenced only by `DTMAPI.AuthorSdk`.
- Runtime already reported manifest parse/load/minimum-version failures for discovered Mods, but it did not classify loose DLL placement under the shared BepInEx plugin tree.
- `check-dtmapi-status.ps1` checked five Runtime files, receipts and FileVersions only.
- `collect-logs.ps1` copied existing evidence only; a crash before Runtime startup left no PE-placement report.
- Catalog retained `DoctorAndPluginPlacementPending` for external Workshop item `3759797170`, which is a real third-party installer/plugin ownership surface.

The gap was distribution and lifecycle ownership, not a reason to embed .NET 8 metadata libraries in Unity Mono or to teach the five production DLLs Author SDK operations.

## Authority And Safety Boundary

`dtmapi-player-doctor` exposes only `inspect`, `help` and `version`. Unknown input is a usage failure. Its inspection engine uses PE/CLR metadata readers and file hashes only; it does not use `Assembly.Load`, `Assembly.LoadFrom`, reflection execution, Harmony or native game calls.

The engine and both player entry points follow these rules:

- never load, execute, patch, move, delete, adopt, install, uninstall, enable or disable a scanned DLL or package;
- installed-game scans are limited to `BepInEx/plugins` and `Mods`, skip reparse points, and do not recurse `DTMAPI/tools`, logs, reports or configuration;
- report files are written only to explicit caller-owned paths outside the scanned roots;
- Runtime may delete/replace only its three known `DTMAPI/reports/player-doctor-latest.*` outputs before the one-shot run;
- external `BaseUnityPlugin` assemblies under `BepInEx/plugins` remain externally owned and expected; an ordinary `DtmMod` DLL there is a misplaced error;
- package-artifact context does not claim a bundled external plugin payload is already installed in the wrong place. It reports placement as not applicable and leaves ownership unchanged.

The real `3759797170` artifact motivated the explicit scan-context split: package payload layout and installed shared-loader placement are different facts and cannot share one placement verdict.

## Minimum-Version Semantics

Player diagnostics deliberately reuse Runtime's historical compatibility behavior rather than Author SDK 0.1's stricter new-authoring grammar:

1. trim whitespace;
2. strip the first `-` or `+` suffix;
3. parse `Version` with two through four numeric components;
4. compare major/minor/build/revision with absent parts normalized to zero.

The player report distinguishes not checked, not declared, compatible, Runtime unavailable, Runtime version invalid, manifest minimum invalid and newer Runtime required. Offline inference does not silently pick one source: it cross-checks `install-state.json`, `release-manifest.json` and all five Runtime DLL FileVersions. A missing member, unreadable version or disagreement produces a structured error and no selected installed version.

## Player And Runtime Flows

### Offline status

`3_check_dtmapi_status.bat` still delegates to PowerShell. The packaged/installed status script requires the sibling self-contained helper and runs an installed-game inspection even when Runtime is incomplete. Exit `0` is clean, exit `2` means the Doctor completed and found placement/minimum/install errors, and any other exit is a tool failure. Doctor errors contribute to the existing required-invalid result; warnings remain visible but do not acquire repair authority.

### Offline log collection

`4_collect_dtmapi_logs.bat` still delegates to the broad diagnostic collector. The collector writes `player-doctor.json`, `player-doctor.txt` and a compact summary into the new evidence directory. Doctor exit `2` is successful evidence collection and does not make log collection fail. A missing/crashed helper becomes a collection warning, so unrelated crash evidence remains obtainable.

### In-game summary/export

After normal manifest discovery/load and before `GameLaunched`, Core starts the installed helper once with explicit current `ApiVersion`, installed-game context and three DTMAPI-owned report paths. The child has a five-second budget; timeout/failure degrades the Doctor feature but does not block Runtime or Mods. The result is exposed as `Diagnostics.PlayerDoctor` in the existing Manager feature snapshot, logged once, added to runtime report context, and included in exported ZIPs. No public Abstractions API, Bootstrap poll, watcher, listener or recurring task is added.

## Packaging And Transaction Boundary

- the helper is a self-contained Windows x64 single-file .NET 8 executable plus the two .NET license/notice files;
- its FileVersion/ProductVersion project from the Runtime `0.5.5` authority;
- the Runtime Workshop package keeps exactly five DLLs under `Payload/BepInEx/plugins/DTMAPI` and places Doctor under `Content/DTMAPIInstaller/tools/player-doctor`;
- installer candidate preparation validates the exact three-file Doctor set and versions, copies it into candidate tools, records hashes in install receipts, and commits/rolls back it with the existing Runtime/state tools transaction;
- player uninstall removes the DTMAPI-owned tools directory while preserving reports/config/backups and all external/ordinary packages;
- package preflight and Catalog gates reject missing/wrong Doctor files and still reject extra Runtime/QA/test DLLs, ordinary manifests and smoke settings.

Catalog gate `DoctorAndPluginPlacementPending` may be changed to `PlayerDoctorAndPluginPlacementVerified` only with passing metadata/portable/package/offline tests. The name change alone is not evidence.

## Closure Audit Hardening

The first independent closure review found six integration defects before commit; all were fixed and regression-covered:

1. the Author SDK still bundled a schema-v1 Doctor contract while emitting schema v2, so the bundled schema was upgraded and the Author test now validates a real `doctor --json` document against the actual copied schema;
2. an installed external BepInEx plugin's generic/Thunderstore `manifest.json` could be mistaken for a DTMAPI CodeMod, so InstalledGame now requires a DTMAPI path or strong DTMAPI declaration marker while PackageArtifact retains full-package interpretation;
3. five correctly named, same-version substitute DLLs could impersonate Runtime, so the offline probe now checks each internal PE AssemblyName against the exact five identities;
4. package-side `3_check` could run its own helper while the installed game-side helper was missing or tampered, so current `0.5.5` status now requires the installed exact three-file set, exact executable version, and three unique install-state SHA-256 receipts;
5. `4_collect` could be aborted or hung by a damaged helper, so the child now has a 30-second bound, redirected bounded evidence, targeted termination, and exception isolation; exit `2` remains successful finding evidence;
6. missing or failed startup helpers could leave stale complete or partial reports for a later export, so Runtime clears only its owned latest/pending names and publishes pending outputs atomically only after a completed exit `0` or `2`.

The installer committed-state gate now verifies all recorded tool hashes, the exact Doctor records/files and version. Source-tree collection resolves a helper from its package sibling first, then the installed game tools, then the repository build artifact; this keeps packaged player authority unchanged while allowing the developer runner to collect the installed player report.

The real subscribed `3759797170` tree was then scanned in PackageArtifact context, not merely represented by a synthetic fixture. The scan exited `0`: `com.mxx.doloc.itemlimiter.installer` was `DtmApiCodeMod/Expected`, `Mxx_DolocTownMod_Plugins.dll` was `ExternalBepInExPlugin/NotApplicable`, and there were zero errors. The inspected DLL hashes were:

- installer: `F2E92A2A1310194EFE2E4C05E393B9FA30BEE4941F095CAEEBA5E715E7AADE2D`;
- external plugin: `43005B5CE96CCEFC17EB150B765F4200BB9B51BB9EC9C83D7EDA25F00D9D1E76`.

This evidence is what resolves the Catalog gate; it does not transfer ownership of either DLL to DTMAPI.

## Validation And Remaining Boundary

The acceptance matrix is owned by Update 0018. It must include focused Doctor/CLI/Core tests, empty-PATH space/non-ASCII portable execution, PowerShell parser checks, Runtime install rollback, exact five-DLL package construction, player-package audit, an in-game one-shot summary/export run, and clean process exit/restoration.

This closure does not provide GC evidence, close ISSUE-010/ISSUE-011, migrate embedded Smoke, add the Batch 4 QA host, or scan/repair every disabled Workshop package. Runtime-discovered manifest/minimum errors and the installed-game PE placement report remain complementary facts.
