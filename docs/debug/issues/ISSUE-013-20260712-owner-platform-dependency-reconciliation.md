# ISSUE-013: Owner reconciliation treats platform providers as missing ordinary Mod sources

- State: `verified`
- Current boundary: Registry-only platform providers survive unchanged title/Workshop refresh; slot-3 owner and no-op reconciliation smokes retain `29/6`, one Entry per code Mod, and zero false restart/cleanup state.

Date: 2026-07-12
Area: Core owner lifecycle / dependency reconciliation / Workshop refresh
Related Review: `docs/reviews/manual-qa/2026/20260712-0001-owner-platform-dependency-reconciliation.md`
Related Update: `docs/updates/2026/20260712-0001-owner-platform-dependency-reconciliation.md`

## Summary

The original run falsely deactivated three of six entered code Mods after a title/Workshop refresh because registry-only platform providers were incorrectly required to appear as ordinary discovered sources. Reconciliation now separates canonical process-lifetime registry providers from source-managed ordinary providers, and locked slot-3 title/owner plus no-op Workshop smokes verify the corrected boundary.

Observed transition:

```text
discovered=29, loaded=6
  -> refresh with the same 29 discovered rows
  -> DebugConsoleMod, ZoomMod, AutoFishing deactivated
  -> discovered=29, loaded=3, needsRestart=3
```

## User-visible report and screenshot transcription

The user screenshot showed DTMAPI Settings `0.5.3-alpha` on the `配置` tab. Under `已启用的 DTMAPI Mod`, only `配置菜单示例（开发者） (DTMAPI.ConfigMenuExample)` was visible; the screenshot did not display discovery/load counters or deactivated owner IDs.

The matching runtime log, inspected separately, showed `29 discovered / 6 initially loaded / 3 later loaded` and identified the three platform-dependent owners deactivated/restart-required as:

- `DTMAPI.DebugConsoleMod`;
- `DTMAPI.ZoomMod`;
- `Yuuka.DTMAPI.AutoFishing`.

No raw screenshot is stored in the repository. The matching local runtime log confirms the counts and IDs below.

## Reproduction

1. Start the Owner Lifetime/first-party Zoom runtime with the same enabled package set.
2. Allow the six ordinary code Mods to complete Entry.
3. Confirm the initial content registry reports `rows=29; loadedRows=6` and no dependency errors or warnings.
4. Enter/return from the native title/save flow so the Workshop/source refresh invokes active-owner dependency reconciliation without changing the provider registry or official enable state.
5. Observe three `必需依赖已失效` warnings at the same refresh.
6. Observe later registry snapshots report `rows=29; loadedRows=3` and the owner ledger reports `needsRestart=3`.

## Failing log evidence

Local source inspected: `D:\Steam\steamapps\common\Doloc Town\DTMAPI\logs\latest.log`.

Initial successful publication:

```text
2026-07-12 00:02:43.303 [DTMAPI.DebugConsoleMod] Mod Entry completed.
2026-07-12 00:02:43.322 [DTMAPI.ZoomMod] Mod Entry completed.
2026-07-12 00:02:43.333 [Yuuka.DTMAPI.AutoFishing] Mod Entry completed.
2026-07-12 00:02:43.435 [DTMAPI] ... rows=29; loadedRows=6; ... dependencyErrors=0; dependencyWarnings=0 ...
```

False dependency invalidation:

```text
2026-07-12 00:03:18.497 [Warn] [DTMAPI] 必需依赖已失效，停用 owner=DTMAPI.DebugConsoleMod; dependency=DTMAPI.DebugConsoleHost。
2026-07-12 00:03:18.498 [Warn] [DTMAPI] 必需依赖已失效，停用 owner=DTMAPI.ZoomMod; dependency=DTMAPI.GameBridge.DolocTown。
2026-07-12 00:03:18.498 [Warn] [DTMAPI] 必需依赖已失效，停用 owner=Yuuka.DTMAPI.AutoFishing; dependency=DTMAPI.ModConfigMenu。
```

Cleanup/result state:

```text
2026-07-12 00:03:18.625 [DTMAPI] ... owners=DTMAPI.DebugConsoleMod|DTMAPI.ZoomMod|Yuuka.DTMAPI.AutoFishing; recentFailures=3; ... cleanupFailures=0; needsRestart=3 ...
2026-07-12 00:03:18.627 [DTMAPI] ... rollbackRecords=3; needsRestart=3 ...
2026-07-12 00:04:27.498 [DTMAPI] ... rows=29; loadedRows=3; ... dependencyErrors=0; dependencyWarnings=0 ...
```

The Zoom cleanup additionally released its Camera lease with `reason=lease release owner cleanup Unload`; that release is evidence of downstream cleanup after false owner deactivation, not evidence that Camera initiated the failure.

## Root cause

The failing active-owner reconciliation used the refreshed ordinary discovery map as a universal dependency authority. Availability effectively required:

```text
provider is loaded in ModRegistry
AND provider has an enabled discoveredById row
AND provider is not already being deactivated
AND provider version satisfies the minimum
```

That is correct for source-managed ordinary Mod providers, but incorrect for DTMAPI platform providers registered directly into the authoritative Mod registry. ConfigMenu, GameBridge, and DebugConsoleHost intentionally have no ordinary `discoveredById` row. Their absence from that map was therefore misclassified as provider loss.

The initial load path accepted their registry rows, which explains why Entry committed before the later reconciliation failed.

## Rejected hypotheses

- **Entry failure:** rejected; each affected owner completed and committed Entry once.
- **Startup dependency/version mismatch:** rejected; initial publication reported zero dependency errors/warnings.
- **Actual package removal or disable:** rejected for this run; discovery stayed at 29 rows and no provider state change preceded the warnings.
- **Provider unload:** rejected; the three dependency IDs are process-lifetime platform registry services.
- **Owner cleanup malfunction:** rejected as root cause; `cleanupFailures=0` and affected Core/GameBridge roots were removed.
- **ReturnedToTitle should clear Mod services:** rejected by the owner lifetime contract; title/save only clear transient state.
- **Camera lease arbitration:** rejected; Camera was cleaned only after Zoom had already been selected for deactivation.
- **Native Hook regression:** no evidence; the failure is entirely inside Core dependency classification and owner reconciliation.

## Original impact

- Ordinary Mods using first-party platform APIs become inactive despite valid providers.
- Loaded count falls from six to three while discovery remains unchanged.
- DebugConsole UI/services, Zoom input/events/config/Camera lease, and AutoFishing services are cleaned unexpectedly.
- Mono assemblies cannot be unloaded, so the correct cleanup path marks all three restart-required and same-process re-enable cannot restore them.
- The unified owner cleanup contract behaves correctly but is invoked for an invalid reason.

## Acceptance criteria

- Registry-only process-lifetime providers remain valid across ReturnedToTitle and repeated Workshop/source refreshes when registered and version-compatible.
- The reproduced package set remains at `29 discovered / 6 loaded`; DebugConsoleMod, ZoomMod, and AutoFishing remain active, keep one Entry each, and are absent from `needsRestart`.
- Required platform-provider removal from the authoritative registry and platform-provider version mismatch still deactivate consumers and leave zero roots.
- Ordinary source removal, official disable, and version downgrade still cascade through required dependency chains in reverse order.
- Optional dependency loss remains warning-only and bounded.
- True same-process owner disable remains idempotent, releases Zoom's Camera lease and every other platform root, requires restart, and never repeats Entry.
- SaveLoaded/ReturnedToTitle preserve active ordinary Mod Event, Input registration, API, ConfigPage, migration, Content, and Camera lease services while clearing only transient input/save/native state.
- Focused tests, Release source/unit validation, document governance, diff checks, and a locked third-save game smoke pass with normal exit and no residual process.

## Resolution

- Process-lifetime provider IDs are recorded when their canonical manifest/API transaction commits. Dependency reconciliation uses the authoritative registry version and does not require an ordinary discovery row.
- Source-managed providers remain governed by refreshed source identity and enablement, while the canonical loaded registry manifest supplies the resident version. An in-place disk version change is update-pending/source-handoff and cannot advertise a newer DLL/API; real required-dependency loss still cascades in reverse order and optional changes remain warning-only.
- Runtime provider manifest/API publication is atomic, dynamic provider IDs are reserved, a late process provider cannot claim an active ordinary owner, file/host/log-error diagnostic sinks cannot interrupt correctness, and no-assembly zero-root owners are no longer permanently restart-required.
- SaveLoaded clears only transient Core Input state; process-lifetime Mod services and Camera lease requests remain until actual owner deactivation.
- Named quarantine metadata remains an authoritative removable current root, overflow is historical `trimmed` metadata rather than a current root, and Config preview aggregates retain counts only.
- The public API, `0.5.3-alpha`, Hook targets, native state contracts, and content formats are unchanged.

## Attempts

1. The original player-correlated run reproduced `29 discovered / 6 loaded -> 3 loaded`, with three false dependency warnings and `needsRestart=3`; its log remains the failure evidence above.
2. Locked slot-3 `GAME-SMOKE/20260712-010905` proved the corrected runtime behavior: all `45` requested platform gates passed, six code Mods entered once, registry state stayed `29/6`, `needsRestart=0`, OwnerLifetime reached `8 -> 8 -> 0`, Camera cleanup and `remaining=0` passed, and the process exited cleanly. The outer `RunStatus=Failed` came only from the harness requiring optional Zoom cleanup health when Zoom owner exercise was not requested. This attempt is a superseded harness-classification result, not a valid runtime failure, and is intentionally absent from the smoke matrix.
3. `tools/scripts/run-game-smoke.ps1` was corrected so unrequested Zoom cleanup health uses skip semantics while requested Zoom owner exercise retains the full health gate; a UnitTests source assertion protects that expression.
4. Final planned locked slot-3 `GAME-SMOKE/20260712-011551` passed `RunStatus`, startup, GameLaunched, HookProbe, SaveLoaded, TitleButtonLifecycle, OwnerLifetime, final health, no-fatal, process exit, and forced-close checks. It retained six one-time Entries, `29/6`, `needsRestart=0`, and `8 -> 8 -> 0` with Camera released and all Core/GameBridge remaining roots zero.
5. Focused locked no-op refresh `GAME-SMOKE/20260712-011924` passed with `DiscoverMods` and `LoadMods hot` both at `rows=29; loadedRows=6`, `loadedNow=0`, zero dependency errors/warnings, six one-time Entries, HookProbe Workshop count `29`, no false restart/cleanup state, and clean exit.
6. Final `tools/scripts/test.ps1 -Configuration Release` after the last code/test review passed in approximately `108s` with exit code `0`, all projects at zero warnings/errors, and `DTMAPI.UnitTests: OK`; the preceding `63.6s` and `85.4s` full runs also passed. Coverage includes ordinary-first provider collision, missing/BadImage same-process repair, named quarantine root counting, participant retry under failed log sinks, and final-health truth cases. Document governance passed `3952` checks, and pre-closure `git diff --check` exited `0`.
7. Final combined locked route `GAME-SMOKE/20260712-080130` ran title/save, OwnerLifetime, and `AutoReloadMods` together. All requested runtime/lifecycle/final-health/fatal/process gates passed; initial and hot-refresh state remained `29/6` with `loadedNow=0` and zero dependency errors/warnings; all six code Mods retained one Entry each. OwnerLifetime preserved `8` roots across SaveLoaded, then reached zero control/cleanup remainder with Camera released, zero Core/participant failures, `restartRequired=False`, and no residual `DolocTown.exe`.
8. Final follow-up Release validation for `20260712-0003` passed in approximately `99.5s`: every project reported zero warnings/errors and `DTMAPI.UnitTests: OK`. Focused tests prove same-directory `1.0 -> 2.0` cannot authorize a new `>=2.0` consumer in the old process; retained DTO/snapshot/report/ledger strings and inputs are bounded with streaming stable hashes, isolated overflow buckets, total report budgets, and same-export trimmed-byte counters.
9. Locked final-binary slot-3 no-op refresh `GAME-SMOKE/20260712-101106` passed all requested startup, HookProbe, SaveLoaded, dependency, owner-transaction, final-health, fatal, forced-close, and process gates. Hot refresh stayed `loadedNow=0; rows=29; loadedRows=6`, dependency errors/warnings stayed zero, all six code Mods retained one Entry, Workshop count was `29`, and no `DolocTown.exe` remained.

The issue is verified for unchanged refresh, title/save preservation, zero-root owner cleanup, the focused no-op Workshop path, and the final combined route exercising those boundaries together. Broader native/Mono lifetime risk remains outside this issue.

## Related records

- `docs/reviews/code/2026/20260711-0001-general-owner-lifetime-boundary-audit.md`
- `docs/updates/2026/20260711-0010-general-owner-lifetime-refactor.md`
- `docs/updates/2026/20260712-0001-owner-platform-dependency-reconciliation.md`
- `docs/updates/2026/20260712-0003-owner-version-diagnostic-byte-bounds.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/design/mod-owner-lifetime-contract.md`
