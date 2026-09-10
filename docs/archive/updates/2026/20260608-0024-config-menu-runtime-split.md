# 20260608-0024 Config Menu Runtime Split

## Metadata

- Update ID: 20260608-0024
- Date: 2026-06-08
- Status: verified
- Source: Active goal: split `IDtmConfigMenuApi` from internal `IConfigMenuRuntime`
- Owner: Codex

## Summary

- Kept ordinary mod-facing `IDtmConfigMenuApi` focused on config registration, display names, option adders, and keybind conflict queries.
- Added friend-internal `IConfigMenuRuntime` for page enumeration, page lookup, begin/save/reset/cancel, page locks, keybind conflict reads, and pending preview.
- Removed editing verbs from public `IConfigMenuPage`; page objects now expose state/items only.
- Updated DTMAPI Core runtime snapshots and config-page lock refresh to use `IConfigMenuRuntime`.
- Updated reflected title settings UI and diagnostic overlay to use `IConfigMenuRuntime` instead of the ordinary mod API.
- Updated GameBridge smoke helper call sites to use the runtime interface for staged config screenshots/config-save smoke, without changing hook implementations.
- Reduced HookProbe's config-menu probe to public API checks; testmods no longer call runtime page editing methods through `IDtmConfigMenuApi`.

## Changed Files

- `src/DTMAPI.Abstractions/AssemblyInfo.cs`
- `src/DTMAPI.Abstractions/ConfigMenu.cs`
- `src/DTMAPI.Core/AssemblyInfo.cs`
- `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedImGuiOverlay.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/DolocTownGameBridge.Smoke.cs`
- `testmods/HookProbeMod/ModEntry.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/api/public-api-matrix.md`
- `docs/debug/INDEX.md`
- `docs/updates/2026/20260608-0024-config-menu-runtime-split.md`
- `docs/updates/INDEX.md`

## Validation

- Passed: `rg` check found no testmod calls to `GetPage`, `GetPages`, `BeginEditing`, `Save`, `Reset`, `Cancel`, `SetPageLock`, `IConfigMenuPage`, `IConfigMenuPendingPreview`, or `TrySetPendingValue`.
- Passed: `tools/scripts/build.ps1`
  - Release build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`
- Not run: Doloc Town game smoke. This was a public/internal API boundary refactor with no GameBridge hook edits; title/UI game smoke should be run before claiming new player-facing ConfigMenu behavior.

## Evidence

- `IDtmConfigMenuApi` no longer declares `GetPages`, `GetPage`, `BeginEditing`, `Save`, `Reset`, `Cancel`, or `SetPageLock`.
- `IConfigMenuPage` no longer declares `BeginEditing`, `Save`, `Reset`, or `Cancel`.
- `ConfigMenuRegistry` implements runtime methods explicitly through internal `IConfigMenuRuntime`.
- DTMAPI Core and Bootstrap UI fields now use `IConfigMenuRuntime` for page runtime operations.
- HookProbe only calls public `IDtmConfigMenuApi.GetKeybindConflicts` after retrieving the public API.
- No GameBridge Hooking/hook implementation files were edited for this goal; only smoke helper calls were adapted to the internal runtime boundary. The Refactor worktree still contains earlier GameBridge hook split changes, so do not interpret the broader dirty hook status as part of this update.

## Related Records

- API matrix: `docs/api/public-api-matrix.md`
- Debug index: `docs/debug/INDEX.md`
- Native owner audit: `docs/reviews/api/2026/20260607-0010-native-owner-remaining-api-audit-index.md`
- Code review ledger: `docs/reviews/api/2026/20260607-0007-native-responsibility-code-review-index.md`

## Rollback Notes

- Revert `ConfigMenu.cs`, `ConfigMenuRegistry.cs`, Core runtime, Bootstrap UI, HookProbe, and unit-test changes together if a downstream consumer still relies on public page editing.
- Do not re-add runtime page editing to `IDtmConfigMenuApi` without a new public API review and matrix update.

## Follow-Up

- Run a focused title ConfigMenu smoke before using this refactor as player-facing UI evidence.
- Consider moving any remaining page/item edit staging primitives behind internal facades in a later breaking API cleanup once runtime snapshot consumers have a complete replacement.
