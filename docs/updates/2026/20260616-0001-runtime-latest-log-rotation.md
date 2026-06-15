# 20260616-0001 Runtime Latest Log Rotation

## Status

Verified local package sync.

## Source Request

User asked whether the player report "logs seem to keep growing and are never cleared; every startup writes into the same log" is real, then approved fixing it on the current branch with the most recent 10 logs retained, committed, and written to the local official upload directory.

## Changed Files

- `src/DTMAPI.Core/Logging/LatestLogRotator.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `tools/scripts/collect-logs.ps1`
- `tools/scripts/README.md`
- `docs/updates/INDEX.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/api/public-api-matrix.md`

## Implementation

- Confirmed the local installed `DTMAPI/logs/latest.log` had multiple `DTMAPI runtime starting.` markers, because `FileMonitor` appends to a fixed path and runtime startup did not rotate or clear it.
- Added `LatestLogRotator` in Core logging. On runtime startup it moves a non-empty previous `latest.log` to `latest-yyyyMMdd-HHmmssfff.log`, keeps the 10 newest `latest-*.log` history files, and lets the current run write a fresh `latest.log`.
- Kept startup robust: rotation errors are logged as a warning after `FileMonitor` is created instead of blocking DTMAPI startup.
- Updated in-game diagnostics export to include retained `DTMAPI-history/latest-*.log` files in addition to current `DTMAPI-latest.log`.
- Updated offline `collect-logs.ps1` so `4_collect_dtmapi_logs.bat` packages the 10 newest `latest-*.log` history files under `DTMAPI-log-history`.

## Validation

- `tools/scripts/build.ps1 -Configuration Release`: passed with 0 warnings / 0 errors and `DTMAPI.UnitTests: OK`.
- `tools/scripts/test.ps1 -Configuration Release`: passed with 0 warnings / 0 errors and `DTMAPI.UnitTests: OK`.
- Unit coverage `RuntimeRotatesLatestLogAndRetainsHistory` verifies old `latest.log` content does not append into the fresh log, exactly 10 history files remain, the oldest history is trimmed, the previous latest is retained, and diagnostic report export includes that retained history log.
- Runtime package rebuilt with `tools/scripts/build-release-workshop-packages.ps1 -Configuration Release -SkipBuild -RuntimeOnly`.
- Official local upload folder synced to `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI` while preserving `workshop.json`.
- Source/build, installed runtime, dist package, and local upload package `DTMAPI.Core.dll` hashes match: SHA256 `BC8C94A5796E9675A105F907B6D8BCBF09C70AB516145E02A76C326A6495090B`, length `180736`.
- `collect-logs.ps1` dist/local upload byte hashes match: SHA256 `743CD7C01764C19C40C4C0A7ACD83DA11C640B4FAE1859CB55E8E94D650F9B11`, length `6942`. Source/dist normalized text matches; byte hash differs because release packaging writes scripts as UTF-8 BOM/Windows line endings.
- `tools/scripts/collect-logs.ps1 -CaseId LOG-ROTATION-CHECK -OutputDirectory tmp/collect-log-rotation-check` copied retained history file `latest-20260615-210026331.log` under `DTMAPI-log-history`; the temporary output directory was removed after verification.
- Startup smoke after runtime install passed: Steam/title startup saw a fresh `DTMAPI runtime starting.` log, `GameLaunched`, clean exit, no fatal window, and no leftover `DolocTown.exe`.
- `git diff --check`: passed with line-ending warnings only.

## Evidence

- Local upload path: `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI`
- Staging path: `E:\Python_project\DTMAPI\dist\workshop-packages\DTMAPI`
- Smoke evidence: `GAME-SMOKE/20260616-054507` recorded in `docs/debug/regressions/smoke-matrix.md`.

## Rollback

Remove `LatestLogRotator`, remove the runtime startup call, remove history inclusion from `DiagnosticsService.ExportLogs()` and `collect-logs.ps1`, rebuild the runtime package, and resync the official local upload folder.

## Follow-Up

Manual Workshop upload is still required from the official Mod UI. If future crash reports need longer history, raise the retained history count deliberately rather than making `latest.log` append forever again.
