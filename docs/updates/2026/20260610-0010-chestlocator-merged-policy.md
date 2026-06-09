# 20260610-0010 ChestLocator Merged Policy

## Metadata

- Update ID: 20260610-0010
- Date: 2026-06-10
- Status: verified
- Source: User requested the midterm Refactor hardening route, including a ChestLocator merged-owner policy instead of a short-term documentation-only limit.
- Owner: Codex

## Scope

- Merge all enabled `IChestLocatorEnhancerApi` owner policies into one effective runtime policy.
- Keep the existing public DTOs unchanged.
- Keep hook IDs, hook status meanings, smoke result schema, and native inventory scan behavior unchanged.
- Do not add cross-frame inventory caching.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Features/ChestLocatorEnhancer/ChestLocatorEnhancerService.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/api/public-api-matrix.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260610-0010-chestlocator-merged-policy.md`

## Summary

- Replaced the previous single effective ChestLocator owner policy with a merged enabled-owner policy.
- Effective rules:
  - `Enabled` is true when any owner is enabled.
  - `IncludeSharedCases`, `IncludeSharedStorageShelfBoxes`, and `VerboseLogging` are true when any enabled owner requests them.
  - `RespectNativeAutoUseBoxSetting` is true only when all enabled owners request it, so any owner can opt into forced shared box scanning.
- Existing `ChestLocatorEnhancerState.LastMessage` and runtime summaries now include `effectiveOwners` plus the merged booleans.
- Disabled owners are excluded from the effective policy but keep their existing disabled state.
- Inventory enumeration still scans live native objects per callback; no cross-frame inventory cache was added.

## Validation

- `git diff --check` passed with only existing line-ending warnings.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors.
- Unit test `ChestLocatorPoliciesMergeEnabledOwners` verifies two enabled owners merge with:
  - deterministic `effectiveOwners=DTMAPI.Tests.ChestPolicyA|DTMAPI.Tests.ChestPolicyB`
  - any-true `IncludeSharedCases`
  - any-true `IncludeSharedStorageShelfBoxes`
  - any-true `VerboseLogging`
  - all-true `RespectNativeAutoUseBoxSetting`
- DirectExe third-save ChestLocator smoke:
  - `GAME-SMOKE/20260610-042459`
  - `ChestLocatorEnhancer=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, `ForcedClose=Passed`
  - `Feature.ChestLocatorEnhancer = ready`
  - `Inventory.ChestLocatorEnhancer = verified`
  - `Smoke.ChestLocatorEnhancer = verified`
  - Runtime summary includes `effectiveOwners=DTMAPI.ChestLocatorEnhancerMod`, merged policy flags, `base=1`, `appended=5`, `roots=1`, `equipments=283`, `sharedCases=5`, and `sharedStorageBoxes=0`.
- Final process check found no leftover `DolocTown.exe`.

## Evidence

- `docs/debug/evidence/GAME-SMOKE/20260610-042459`
- The smoke produced DTMAPI latest log, BepInEx log, Player log, result JSON, process check, and fatal-window check.
- This ChestLocator-only smoke did not export a fresh DTMAPI report zip; `latest-report.txt` still points to the previous diagnostics report and is not cited as this branch's report evidence.

## Related Records

- `docs/updates/2026/20260610-0005-chestlocator-feature-split.md`
- `docs/updates/2026/20260610-0008-hook-callback-safe-fallbacks.md`

## Rollback Notes

- Revert `TryGetChestLocatorPolicy(...)` to selecting a single enabled owner if a future compatibility issue requires returning to previous behavior.
- Keep the feature-host split and safe callback fallback work unless explicitly rolling back those separate records.

## Follow-Up

- Continue the midterm route with diagnostics status codes.
