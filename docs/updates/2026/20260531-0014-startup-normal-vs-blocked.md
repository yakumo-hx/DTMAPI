# 20260531-0014 Startup Normal Vs Blocked Evidence

## Source Request / Goal

- Continue the DTMAPI 0.1.13 follow-up without redoing already completed gameplay/UI work.
- Address the occasional 30-second startup concern with segment evidence, not guessed optimization.
- Keep direct `DolocTown.exe` launch rejected for smoke unless explicitly testing the known fatal-instance path.

## Changed Files

- `docs/debug/regressions/smoke-matrix.md`
- `docs/debug/issues/ISSUE-004-steam-launch-stuck.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260531-0014-startup-normal-vs-blocked.md`

## Known Facts And Rejected Hypotheses

- Known: DTMAPI startup segment logging exists for Bootstrap, BepInEx init, path/log setup, manifest scan, official MODS scan, Workshop scan, content index, mod load, Harmony initialize, icon load, and total Bootstrap Awake time.
- Known: latest normal samples are sub-second DTMAPI startup runs, not 30-second DTMAPI runs.
- Known: the reproduced blocked Steam samples did not create `DolocTown.exe` and did not create a fresh DTMAPI log.
- Rejected: optimizing DTMAPI startup based on the blocked Steam samples. They are pre-process launcher failures, not DTMAPI runtime segments.
- Rejected: marking the 30-second comparison complete. There is still no captured run with `StartupLog=true` and a 30-second DTMAPI segment.

## Implementation

- No runtime code was changed in this record.
- Updated the smoke matrix to distinguish current normal DTMAPI startup segment samples from the Steam launch-blocking samples.
- Updated `ISSUE-004` with the latest clean post-restart normal samples and the explicit limit that no 30-second DTMAPI segment exists yet.

## Validation

- No new game run was started for this documentation-only comparison.
- Existing successful smoke evidence was inspected:
  - `docs/debug/evidence/GAME-SMOKE/20260531-160900`, collected logs `docs/debug/evidence/GAME-SMOKE/20260531-160943`
  - `docs/debug/evidence/GAME-SMOKE/20260531-161357`, collected logs/screenshots `docs/debug/evidence/GAME-SMOKE/20260531-161545`
- Existing blocked Steam evidence was inspected:
  - `docs/debug/evidence/GAME-SMOKE/20260531-123258`

## Evidence

- Normal sample `GAME-SMOKE/20260531-160943`:
  - `DiscoverMods totalMs=56`
  - `ModLoad elapsedMs=402 totalMs=461`
  - `Bootstrap.RuntimeStart elapsedMs=468`
  - `Bootstrap.HarmonyInitialize elapsedMs=544`
  - `IconLoad elapsedMs=14`
  - `Bootstrap.Awake totalMs=610`
- Normal sample `GAME-SMOKE/20260531-161545`:
  - `DiscoverMods totalMs=56`
  - `ModLoad elapsedMs=432 totalMs=490`
  - `Bootstrap.RuntimeStart elapsedMs=498`
  - `Bootstrap.HarmonyInitialize elapsedMs=573`
  - `IconLoad elapsedMs=10`
  - `Bootstrap.Awake totalMs=636`
- Blocked Steam sample `GAME-SMOKE/20260531-123258`:
  - `StartupLog=false`
  - `GameLaunched=false`
  - `SaveLoaded=false`
  - `process-check.txt`: `No DolocTown.exe process found.`
  - `fatal-window-check.txt`: `No fatal instance popup found.`
  - No `Startup segment` lines exist in the blocked evidence.

## Related Records

- `docs/debug/regressions/smoke-matrix.md`: `STARTUP-001`, `SMOKE-002`
- `docs/debug/issues/ISSUE-004-steam-launch-stuck.md`
- Previous runtime/startup instrumentation record: `docs/updates/2026/20260531-0011-runtime-behavior-013.md`

## Rollback Notes

- Documentation-only update. Revert the smoke-matrix and issue text if a future run captures a real 30-second DTMAPI startup segment and replaces this pre-process-launch classification.

## Follow-Up

- If the user observes another slow launch, capture whether `DolocTown.exe` exists, whether `DTMAPI/logs/latest.log` has new `Startup segment` lines, and whether `Bootstrap.Awake totalMs` is near 30000ms.
- A true 30-second DTMAPI startup sample should be compared segment-by-segment before changing scanner, icon, config, Harmony, or mod-load code.
