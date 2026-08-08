# 20260628-0001 Enabled Duplicate Mod Source

## Summary

Adjusted DTMAPI mod discovery duplicate resolution so an enabled source wins over a disabled or stale duplicate with the same `UniqueID`. This fixes the case where a disabled higher-version `OfficialLocal` upload package could shadow an enabled Steam Workshop subscription package and make DTMAPI report the enabled subscription as blocked by a newer API requirement.

This does not change `MinimumDTMApiVersion` comparison. `0.5.2-alpha` continues to satisfy `0.5.2-alpha` and older requirements such as `0.5.1-alpha`; the problem was source selection before version/load checks.

## Source Request

User request: prefer the official enabled source when duplicate DTMAPI mod sources exist, keep version comparison behavior unchanged, add tests, then rebuild/sync the upload package and local runtime.

## Changed Files

- `src/DTMAPI.Core/Manifesting/ManifestReader.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260628-0001-enabled-duplicate-mod-source.md`

## Behavior

- Duplicate `UniqueID` groups now sort by enabled state first, then by the existing source priority: `OfficialLocal` > `Workshop` > `Local`, then by `RootPath` for deterministic tie-breaking.
- A disabled `OfficialLocal` package no longer shadows an enabled `Workshop` package with the same `UniqueID`.
- If multiple candidates have the same enabled state, the previous source priority remains intact.
- Duplicate warnings now include selected and ignored source, enabled state, and root path, making cases like `disabled OfficialLocal ignored, enabled Workshop selected` visible in diagnostics.

## Validation

- `git diff --check` passed 2026-06-28 with line-ending warnings only.
- `tools/scripts/test.ps1 -Configuration Release` passed 2026-06-28 with `DTMAPI.UnitTests: OK`; restore/build emitted only restricted-network NU1900 vulnerability-index warnings for `https://api.nuget.org/v3/index.json`.

Local upload package sync and installed runtime hash checks are performed after this source commit under the runtime lock. No game smoke is claimed in this update record; the intended game-side check is opening DTMAPI Settings and confirming enabled Workshop packages are loaded instead of being shadowed by disabled OfficialLocal duplicates.

## Evidence Links

- Related player-facing symptom: DTMAPI Settings showed `DTMAPI 前置版本过旧` for an enabled subscribed mod while the official mod UI showed the subscribed Workshop package enabled and the stale local package disabled.
- Relevant diagnostic area: `DTMAPI.ModScanner` duplicate warnings.

## Rollback Notes

If an enabled developer-local duplicate unexpectedly overrides a deliberately disabled official package in a testing setup, restore the old `SourcePriority`-first ordering or narrow enabled-state priority to official-managed sources only. The current behavior intentionally treats enabled state as the stronger signal across all discovered duplicate sources.

## Follow-Up

After package sync, retest the local environment where disabled 0.5.3 `OfficialLocal` packages coexist with enabled 0.5.1/0.5.2 Workshop subscriptions. Verify AutoFishing, Fish Roe Info, and MoreEquipmentSlots show as selected from Workshop when their Workshop entries are enabled.
