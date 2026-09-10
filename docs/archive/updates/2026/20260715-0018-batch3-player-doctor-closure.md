# 20260715-0018 Batch 3 Player Doctor Closure

## Metadata

- Update ID: `20260715-0018`
- Date: 2026-07-15
- Lifecycle Status: `verified`
- Validation Level: `docs,source,unit,runtime`
- Runtime Validation: `passed`
- Related Issue State: `open`
- Area: batch3/player-doctor/diagnostics/minimum-version/misinstallation/installer/package/runtime
- Source: user requested the missing D1 player Doctor offline and in-game closure before Batch 4
- Primary review: `docs/reviews/code/2026/20260715-0010-batch3-player-doctor-closure-review.md`
- Route review: `docs/reviews/code/2026/20260715-0009-batch2-batch3-route-and-batch4-entry-review.md`

## Scope

- ship an independent read-only self-contained Player Doctor without Author SDK build/deploy/repair authority;
- diagnose Runtime/receipt/minimum-version errors, external BepInEx ownership and ordinary CodeMod misinstallation without loading or mutating scanned DLLs;
- retain an offline `3_check`/`4_collect` path when Runtime cannot load;
- add one bounded startup-only Runtime summary and include Doctor artifacts in normal report export;
- install/update/rollback/uninstall the helper through the existing DTMAPI-owned Runtime tools transaction;
- preserve the exact five game-loaded production DLL invariant and resolve the Catalog plugin-placement gate from executable evidence;
- complete and commit this closure before creating the Batch 4 implementation lifecycle.

## Current Change Surface

- Doctor/CLI: `src/DTMAPI.InstallDoctor/**`; `src/DTMAPI.PlayerDoctor/**`; `tests/DTMAPI.InstallDoctor.Tests/**`; `author-sdk/schemas/doctor-report.schema.json`; `tests/DTMAPI.AuthorSdk.Tests/Program.cs`.
- Runtime summary/export: `src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs`; `PlayerDoctorRuntimeService.cs`; `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`; `RuntimePaths.cs`; `tests/DTMAPI.UnitTests/Program.cs`.
- Build/package/install/offline tools: `DTMAPI.sln`; `tools/scripts/build.ps1`; `build-player-doctor.ps1`; `check-player-doctor-release.ps1`; `test-player-doctor-portable.ps1`; `test-player-doctor-packaged-entrypoints.ps1`; `build-release-workshop-packages.ps1`; `install-to-game.ps1`; `check-dtmapi-status.ps1`; `collect-logs.ps1`; `probe-install-preflight.ps1`; `test-runtime-upgrade-transaction.ps1`; `run-game-smoke.ps1`; `test.ps1`.
- Release contract: `tools/release/dtmapi-product-catalog.json`; `tools/scripts/check-product-catalog.ps1`; `check-release-contract.ps1`.
- Player/workflow documentation: `docs/guides/install-dev-preview.md`; `docs/workflows/workshop-package-subscription-test-matrix.md`; `tools/scripts/README.md`.
- Governance/evidence: Review 0010, this Update, `docs/updates/INDEX-2026-07.md`, and `docs/debug/regressions/smoke-matrix.md`.

## Validation

- The exact pre-commit tree passed `tools/scripts/test.ps1 -Configuration Release`, including builds and unit suites, Player Doctor release/portable/packaged-entrypoint gates, evidence retention, Author SDK release/portable tests, Runtime and developer installer transaction matrices under PowerShell 7 and Windows PowerShell 5.1, Catalog, and the Batch 2 release contract.
- Player Doctor Release build passed with FileVersion `0.5.5.0`, ProductVersion `0.5.5`, exact three-file output and executable SHA-256 `B7A46D2ED65D3F0C77E91B6450B48734A90D6164297D11920C0E6D8D4BAC967F`.
- `DTMAPI.InstallDoctor.Tests` passed all seven metadata/read-only/context/minimum/identity/CLI groups; `DTMAPI.AuthorSdk.Tests` passed including real output against the bundled schema-v2 file; `DTMAPI.UnitTests` passed with stale/latest/pending cleanup and three-file ZIP export coverage. The relevant Release builds reported zero warnings and errors.
- The self-contained helper passed with empty `PATH` and empty `DOTNET_ROOT*` from a path containing spaces and Chinese characters while a misplaced CodeMod DLL was locked read-only; plugin/Mods hashes remained unchanged.
- Windows PowerShell 5.1 parsed the changed installer, status, collector, transaction, Doctor build/release/portable/entrypoint and runner scripts.
- Runtime transaction matrices passed all eleven fault/success/status cases under PowerShell 7 and Windows PowerShell 5.1. Committed Doctor files, versions and hashes were exact; clean, tampered-license and missing-notice status cases produced the intended outcomes.
- Candidate package `dist/workshop-packages-doctor-closure-20260715/DTMAPI` passed Catalog `26/11/21/46`, the exact five game-loaded DLL invariant, and the player subscription audit with `Blockers: 0`. Audit summary: `dist/workshop-packages-doctor-closure-20260715/audit/DTMAPI Workshop Audit 20260715-232255/Results/stress-summary.md`.
- The packaged offline matrix installed into a fake Chinese/space game path, locked a misplaced ordinary CodeMod, and passed `3_check=1`, `4_collect=0`, three Doctor exports, `PlayerDoctorExit=2`, unchanged plugin/Mods trees and uninstall preservation of the unknown DLL.
- The real subscribed `3759797170` PackageArtifact scan exited `0` with two artifacts, zero errors, and one non-ownership warning. Exact DLL hashes and classifications are owned by Review 0010 and the Catalog gate evidence.
- The installed real-game `3_check` returned `0`: exact five Runtime DLL versions, exact installed Doctor files/version, all three unique receipt hashes, and the read-only `9/0/0` artifact/error/warning report passed.
- `GAME-SMOKE/20260715-232802` passed normal Steam/no-HookProbe slot-3 title lifecycle with Doctor `ready`, Runtime `0.5.5`, profile restoration and no residual process.
- `GAME-SMOKE/20260715-233115` passed normal Steam/no-HookProbe Manager Status/Logs/export. The generated ZIP SHA-256 `A08C2C2E1E3A47873813A5E603BB8EC1A1FFAD5E5E5E224317CA868E61FACEE5` contained `PlayerDoctor/player-doctor.json` (`5680` bytes), text (`1452`) and summary (`86`); source collection captured the same reports with empty child stdout/stderr. Missing-frame/stall counts were `1/1`, recovered, and match the known baseline rather than a new Debug issue.
- The preceding `232447` attempt is retained as failed evidence: Doctor and Manager export passed, but the runner falsely required a dedicated save coordinator line in a no-save title-only lane and source failure collection searched only a sibling helper. The runner now accepts only a zero-request/zero-duplicate final-health coordinator record for no-save lanes, the collector resolves the installed helper, and `233115` is the passing replay.
- Runtime lock was released after every game run; the final status was free, official enablement was restored, `DolocTown.exe` was absent, and no new Fatal GC, crash dump or gameplay failure appeared.
- ISSUE-010 and ISSUE-011 remain open. These minute-scale Doctor/Smoke runs are not AutoFishing or ActionSpeed GC evidence.

## Rollback

Revert this Update's complete code/script/Catalog surface as one Batch 3 closure. The installer transaction must roll the Doctor tool directory back together with Runtime tools and receipts; do not delete or move any unknown/external DLL or ordinary Mod. If runtime validation has staged a deliberately misplaced fixture, restore its exact pre-test path/bytes before releasing the Runtime lock.

## Follow-Up

- After this verified closure is committed as a reviewable rollback boundary, create Update `20260715-0019` for Batch 4.
- Use Review `20260715-0011` as the Batch 4 dependency map and preserve its G0-G7 atomic migration/rollback order.
