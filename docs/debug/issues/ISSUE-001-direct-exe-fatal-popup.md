# ISSUE-001: Direct EXE Smoke Launch Shows Fatal Instance Popup

- State: `open`
- Current boundary: DirectExe fatal popup; script mitigation exists.

## Current Status

- Previous status wording: open / mitigated in scripts
- Last observed: 2026-05-30
- Severity: high for automated smoke testing
- Regression risk: high

## Symptom

When Codex launches Doloc Town directly through `DolocTown.exe`, the game can exit before the title menu and leave a foreground dialog:

```text
Fatal error
Another instance is already running
```

After the game exits, `Get-Process -Name DolocTown` can be empty, so a process-only exit check misses the failure.

## Known Facts

- The failing run `docs/debug/evidence/GAME-SMOKE/20260530-133702` reached DTMAPI startup and `GameLaunched`, but did not reach `HookProbe SaveLoaded OK`.
- User observed the fatal popup after the automated launch; the popup remains after the game process exits.
- User clarified that the popup appears a few seconds after launch, before the title menu is visible; by the time the popup is available for screenshot, `DolocTown.exe` may already be gone.
- Doloc Town build `23465763_workshop_38581E` calls `SteamAPI.RestartAppIfNecessary(new AppId_t(2285550u))` during Steam initialization.
- Direct EXE launch can therefore race or delegate to Steam in a way that produces a second-instance popup before the main menu.

## Rejected Hypotheses

- Not a clean hook probe pass: no third-save `SaveLoaded` evidence was captured in the failing run.
- Not sufficient to check only `DolocTown.exe`: the dialog can remain visible after the process disappears.
- Not a DTMAPI mod dependency failure: `Yuuka.ActionSpeed`, HookProbe, HelloDtmMod, and ConfigMenuExample all reached `Entry` in the failing log.

## Mitigation

- `tools/scripts/run-game-smoke.ps1` now launches through Steam by default.
- Direct EXE launch is opt-in via `-DirectExe`.
- Smoke runs with HookProbe now require `HookProbe SaveLoaded OK`; startup/GameLaunched alone is not enough.
- Smoke scripts enumerate visible desktop windows and fail if a `Fatal error` / `Another instance is already running` popup is present, even when `DolocTown.exe` has already exited.
- Smoke settings are disabled at the end of the script to avoid affecting a later manual game launch.

## 2026-05-30 Follow-up Evidence

- Steam-launched smoke `docs/debug/evidence/GAME-SMOKE/20260530-143756` reached `SaveLoaded`, exited cleanly, and recorded `NoFatalInstanceWindow=true`.
- HookProbe `docs/debug/evidence/HOOK-PROBE/20260530-143833` passed with `NoFatalInstanceWindow=true` and `No DolocTown.exe process found.`
- Final Steam HookProbe `docs/debug/evidence/HOOK-PROBE/20260530-150808` passed all strict checks, including `NoFatalInstanceWindow=true` and exit without `DolocTown.exe`.
- Direct EXE launch remains invalid for automated smoke unless explicitly requested with `-DirectExe`.

## Acceptance Criteria

Do not mark solved until:

- A clean Steam-launched smoke run reaches the third save and logs `HookProbe SaveLoaded OK`.
- The run exits with no `DolocTown.exe` left.
- The user confirms no fatal instance popup remained after the run.
- The evidence path is recorded in `docs/debug/regressions/smoke-matrix.md`.
