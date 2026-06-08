# 20260608-0030 Core Loader Hardening 2

## Metadata

- Update ID: 20260608-0030
- Date: 2026-06-08
- Status: verified
- Source: Active goal: "DTMAPI Core loader hardening 2"
- Owner: Codex

## Summary

- Added manifest `EntryType` support for code-mod entry selection.
- Hardened `EntryDll` validation so code mods must point to a relative `.dll` file inside the mod root.
- Changed code-mod loading so a DLL with multiple concrete `DtmMod` subclasses is rejected unless `EntryType` selects exactly one entry type.
- Changed dependency-cycle handling so every mod involved in the cycle is marked blocked and does not enter the load path.
- Added an explicit `MinimumGameVersion` warning when the current host cannot detect the Doloc Town game version.
- Changed event unsubscription to owner-bound removal so one mod cannot remove another mod's handler by sharing the same delegate instance.
- Kept the implementation scoped to Core manifest/loading/event code and unit tests; no GameBridge, CameraView, or hook behavior was changed for this goal.

## Changed Files

- `src/DTMAPI.Core/Manifesting/ManifestModels.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.Core/Services/EventManager.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260608-0030-core-loader-hardening-2.md`
- `docs/updates/INDEX.md`

## Validation

- Passed: `tools/scripts/build.ps1 -Configuration Release`
  - Build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`
- Passed: `tools/scripts/test.ps1 -Configuration Release`
  - Build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`
- Unit coverage added for:
  - `EntryDll` non-`.dll` rejection.
  - `EntryType` manifest parsing and entry selection.
  - Missing `EntryType` rejection when a DLL contains multiple `DtmMod` subclasses.
  - Dependency-cycle involved mods not loading and receiving owner-specific blocked diagnostics.
  - `MinimumGameVersion` warning when game version detection is unavailable.
  - Owner-bound event handler removal.
- Game smoke was not run because this update does not change GameBridge, CameraView, Harmony hooks, smoke harness behavior, or installed game behavior.

## Evidence

- Release build/test terminal output from this Codex session.
- Debug regression row: `docs/debug/regressions/smoke-matrix.md` case `CORE-LOADER-HARDENING-002`.
- Debug index note: `docs/debug/INDEX.md`.

## Known Facts And Rejected Hypotheses

- Known fact: this is a Core manifest/loader/event-dispatch hardening pass, not a Doloc Town native hook pass.
- Known fact: the current runtime host does not expose a reliable game-version value.
- Rejected hypothesis: keep silently ignoring `MinimumGameVersion`; the loader now emits a warning when the field is present but cannot be checked.
- Rejected hypothesis: keep using the first discovered `DtmMod` type in a multi-entry DLL; the loader now requires `EntryType`.
- Rejected hypothesis: remove event handlers only by delegate identity; removal is now scoped to the subscribing owner.

## Related Records

- Previous Core runtime hardening: `docs/updates/2026/20260608-0003-core-runtime-hardening.md`
- Debug index: `docs/debug/INDEX.md`
- Regression matrix: `docs/debug/regressions/smoke-matrix.md`

## Rollback Notes

- Revert the Core manifest/runtime/event changes and the matching unit tests to restore the previous permissive loader behavior.
- If rolling back only `EntryType`, keep the `EntryDll` `.dll` validation and owner-bound event removal if they are still desired independently.

## Follow-Up

- If a future host-level game-version detector is added, replace the current `MinimumGameVersion` warning-only path with real version comparison tests.
