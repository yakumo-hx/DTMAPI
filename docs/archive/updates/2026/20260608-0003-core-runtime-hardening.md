# 20260608-0003 Core Runtime Hardening

## Metadata

- Update ID: 20260608-0003
- Date: 2026-06-08
- Status: verified
- Source: Active goal: "DTMAPI Core runtime hardening only"
- Owner: Codex

## Summary

- Hardened Core manifest/dependency loading, API registration ownership, high-frequency event isolation, config recovery, and off-thread fallback update handling.
- Kept the implementation scoped to `DTMAPI.Core`, necessary `DTMAPI.Abstractions` interface surface, unit tests, and this update record.

## User-Visible Impact

- Bad manifests, dependency version mismatches, API version mismatches, circular dependencies, unsafe `EntryDll` paths, broken config JSON, and repeatedly failing update handlers now fail with clearer diagnostics instead of weakening the runtime loop.
- Ordinary mods can register APIs only through their helper-bound owner identity.

## Changed Files

- `src/DTMAPI.Abstractions/Helpers.cs`
- `src/DTMAPI.Core/Manifesting/ManifestModels.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.Core/Services/ConfigService.cs`
- `src/DTMAPI.Core/Services/EventManager.cs`
- `src/DTMAPI.Core/Services/RegistryAndHelpers.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/updates/2026/20260608-0003-core-runtime-hardening.md`
- `docs/updates/INDEX.md`

## Validation

- Passed: `tools/scripts/build.ps1 -Configuration Release`
  - Built Abstractions, Core, ModConfigMenu, BepInExBootstrap, GameBridge, all testmods, and `DTMAPI.UnitTests`.
  - `DTMAPI.UnitTests: OK`
  - 0 warnings, 0 errors for the final run.
- Scope check: `git diff -- src/DTMAPI.GameBridge.DolocTown testmods/ZoomMod src/DTMAPI.BepInExBootstrap` produced no diff.
- Game smoke was not run because this branch intentionally did not modify GameBridge, hooks, CameraZoom, or installed game behavior.

## Evidence

- Terminal validation output from the final Release build/unit run in this Codex session.

## Related Records

- Debug: `docs/debug/INDEX.md`
- Hook map: not changed; no hook work in this update.
- Smoke matrix: not changed; no game smoke in this Core-only update.
- API matrix: not changed; public surface change is limited to owner-bound `IModRegistry.RegisterApi`.
- Roadmap: `docs/architecture/20260608-runtime-hardening-branch-roadmap.md`

## Rollback Notes

- Revert the changed Core/Abstractions/test files and this update record to restore the previous permissive runtime behavior.
- If rolling back only the owner-bound registry interface, also restore any mod/test code compiled against `RegisterApi(owner, api)`.

## Follow-Up

- If future work changes the Bootstrap timer callback itself, keep it separate from this Core-only branch and verify that `TimerFallback` does not call ordinary Unity/game update paths.
