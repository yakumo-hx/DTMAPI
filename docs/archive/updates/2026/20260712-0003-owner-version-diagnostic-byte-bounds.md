# 20260712-0003 Owner Version And Diagnostic Byte Bounds

## Metadata

- Update ID: `20260712-0003`
- Date: 2026-07-12
- Lifecycle Status: `verified`
- Validation Level: `docs, source, unit, runtime`
- Runtime Validation: `passed`
- Related Issue State: `verified`
- Source: user follow-up code review after `cd80111b` identified one P1 version-authority gap and two P2 bounded-diagnostic/build-warning gaps.

## Scope

- use the process-resident loaded manifest as the version authority for ordinary providers;
- treat an in-place ordinary manifest version change as an update-pending source handoff;
- prevent a refreshed disk manifest from authorizing consumers against an older resident DLL/API;
- bound every retained owner-failure and diagnostic-aggregate scalar field, append a stable hash when truncated, and report trimmed bytes;
- remove the ModConfigMenu CS8602 warning without behavior or API changes;
- add focused regressions for same-directory version upgrade, retained diagnostic bytes, and warning-free Release validation.

## Source Review

- `docs/reviews/code/2026/20260712-0001-owner-version-diagnostic-byte-boundary-follow-up.md`
- `docs/reviews/code/2026/20260711-0001-general-owner-lifetime-boundary-audit.md`

## Implementation

- Ordinary provider availability still requires a current discovered/enabled source, but its dependency version now comes only from the canonical manifest committed in `ModRegistry` with the resident owner transaction.
- `ReconcileActiveOwners` compares loaded/current manifest versions in addition to `Source + OfficialId + RootPath`. An in-place version change enters the existing update-pending/source-handoff path, deactivates required/transitive dependents before the provider, leaves optional consumers active with a warning, and keeps every successfully loaded assembly restart-required.
- A new consumer requiring the refreshed disk version cannot enter the old process after the resident provider is deactivated. A clean runtime may publish the updated canonical manifest and load compatible consumers.
- `BoundedDiagnosticScalar` centralizes retained scalar limits: owner `160`, identifier `192`, short text `256`, message `512`, and details `2048` UTF-16 code units. Truncated values keep a streaming SHA-256 suffix and contribute their net omitted UTF-16 bytes to a saturating counter.
- Diagnostics apply those limits before retaining errors/warnings, Hook/feature dictionary keys and values, diagnostic aggregate keys/owner/message/details, owner active transactions, Config preview aggregate keys, and every caller-controlled field in the 64-entry owner-failure window. Hook, feature, diagnostic, and Config-preview overflow buckets live outside caller key space. Fixed SHA-256 owner identities preserve long-manifest error association without retaining the full ID.
- Public diagnostic DTO construction is defensive, and `DtmDiagnosticsSnapshot` projects supplied interfaces into bounded scalar rows with fixed row caps instead of retaining a foreign object graph. Evidence summaries and exported runtime context each cap at `262144` UTF-16 code units; report summaries cap at `524288`; log/report paths, installer-state versions, and read failures use the same sanitizer. Reports expose same-export `DiagnosticsScalarTrimmedBytes` plus `DiagnosticsSummaryTrimmedBytes`; owner snapshots expose `TrimmedBytes` and `trimmedBytes=`.
- The ConfigMenu owner facade now uses the already-proven non-null owner annotation in its mismatch exception, removing CS8602 without changing behavior or API.

## Changed Files

- `src/DTMAPI.Core/Diagnostics/BoundedDiagnosticScalar.cs`
- `src/DTMAPI.Core/Diagnostics/DiagnosticsModels.cs`
- `src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.Core/Runtime/ModOwnerLedgerService.cs`
- `src/DTMAPI.Core/Runtime/RuntimeSnapshotFactory.cs`
- `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/reviews/code/2026/20260712-0001-owner-version-diagnostic-byte-boundary-follow-up.md`
- `docs/design/mod-owner-lifetime-contract.md`
- `docs/api/public-api-matrix.md`
- `docs/debug/issues/ISSUE-013-20260712-owner-platform-dependency-reconciliation.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260712-0001-owner-platform-dependency-reconciliation.md`
- this Update and its monthly ledger row

## Validation

- `tools/scripts/test.ps1 -Configuration Release`: final run passed in approximately `99.5s` after the defensive snapshot/report follow-up; every runtime, ModConfigMenu, first-party Mod, test Mod, Bootstrap, and UnitTests project compiled with `0 warnings` and `0 errors`; `DTMAPI.UnitTests: OK`.
- Version tests cover same-directory downgrade handoff, required/transitive reverse cleanup, optional warning-only preservation, repeated refresh idempotence, and exact `1.0 -> 2.0` upgrade behavior: the old process blocks a new `>=2.0` consumer before Entry with zero roots, while a clean runtime publishes `2.0` and loads both consumers.
- Diagnostic stress covers long error/warning owner/message/details, Hook/feature keys and values, caller/overflow-key separation, aggregate key distinction by stable hash, direct DTO construction, foreign snapshot rows and row caps, report/path/install-state output, the total summary budget, owner failure fields, preview aggregate keys, exact UTF-16 trimmed-byte accounting, and snapshot/report counters.
- Independent read-only reviews found no remaining P0/P1/P2 in the scoped version-authority or diagnostic-retention paths.
- Final `tools/scripts/check-doc-governance.ps1`: passed `3980` checks after evidence closure; final `git diff --check`: passed.

## Runtime Evidence

- Locked third-save command `tools/scripts/run-game-smoke.ps1 -SaveSlot 3 -IncludeHookProbe -AutoReloadMods -TimeoutSeconds 360 -SkipBuild` passed against the final binaries as `GAME-SMOKE/20260712-101106`.
- `RunStatus`, StartupLog, GameLaunched, HookProbe, SaveLoaded, DependencyCompatibility, ModOwnerLifecycle, ModLoadTransaction, GameBridgeFinalHealthSnapshot, NoFatalInstanceWindow, and ForcedClose passed.
- The unchanged refresh retained `rows=29; loadedRows=6`, reported `loadedNow=0`, kept dependency errors/warnings at zero, delivered `WorkshopModListChanged OK count=29`, and each of the six ordinary code Mods completed Entry exactly once.
- The game exited normally, `process-check.txt` reported no `DolocTown.exe`, and the shared runtime lock was released. The destructive in-place version-change branch is proven by the focused source/unit fixture rather than mutating installed player packages during smoke.

## Rollback

Revert this Update's task-scoped source, tests, and documentation. Do not revert the base owner transaction, provider classification, or first-party Zoom promotion.

## Follow-Up

No public API signature, stability level, `0.5.3-alpha` version, Hook target, native state, or content format changed. Reopen this boundary if an unchanged refresh deactivates owners, an in-place version edit authorizes a consumer against resident older code, or a retained diagnostic field exceeds its centralized limit.
