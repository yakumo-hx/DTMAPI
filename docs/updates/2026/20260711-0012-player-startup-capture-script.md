# 20260711-0012 Player Startup Capture Script

## Metadata

- Update ID: `20260711-0012`
- Date: 2026-07-11
- Lifecycle Status: `verified`
- Validation Level: `docs, source, unit`
- Runtime Validation: `not-run`
- Related Issue State: `mitigated`
- Source request: replace the multi-step player Process Monitor procedure with a foolproof double-click script after full payload hashes, Steam options/branch, patchers, persistent disable variables, and live Doorstop proxy loading were confirmed clean.
- Related review: `docs/reviews/manual-qa/2026/20260711-0001-player-title-settings-runtime-missing.md`
- Related issue: `docs/debug/issues/ISSUE-012-20260711-player-title-settings-runtime-missing.md`

## Scope And Result

- Added player-facing standalone probe entry `5_capture_dtmapi_startup.bat`. The player closes the game, double-clicks once, approves elevation, and presses Enter after the explicit Sysinternals EULA notice; the probe then performs the rest automatically. It is intentionally absent from the Runtime Workshop package.
- The capture script prepares a Microsoft-signed Process Monitor instance, starts a destructive process-name filter for `DolocTown.exe`, launches the game through the Windows shell/Steam URI, waits through the title-startup window, stops capture, exports a readable CSV plus important-event summary, invokes the existing bounded log collector, classifies the Doorstop/Preloader/log boundary, and creates one Desktop ZIP for support.
- Do not bundle or redistribute Process Monitor binaries; download from the official Microsoft Sysinternals endpoint at runtime and require a valid Microsoft Authenticode signature before execution.
- Signature validation also requires the Sysinternals Procmon product identity, so an unrelated Microsoft-signed executable is rejected.
- A generated 150-byte Process Monitor configuration enables destructive `Process Name is DolocTown.exe / Include` filtering. A five-second fake-process run produced a roughly 1.5 MB PML instead of the roughly 176 MB seen in an unfiltered five-second research capture.
- Kept the new route separate from install, uninstall, ordinary static status checks, and the published Runtime package.
- Built an immediate standalone player helper ZIP under the ISSUE-012 evidence root; it contains no Process Monitor executable.
- Player feedback exposed a launcher-only defect before any capture logic ran: the UTF-8 BAT had LF-only line endings, and Windows `cmd.exe` label seeking resumed in the middle of command text. The package builder now normalizes root BAT files to CRLF, and the corrected exact helper reaches the consent prompt.
- Elevation now occurs in the standalone BAT before invoking the capture script. Any unhandled pre-capture PowerShell failure writes `DTMAPI-startup-capture-last-error.txt` to the Desktop (or explicit test output root), while the elevated BAT keeps the result visible.
- A maintainer self-test showed the first classifier could mislabel a successful startup when BepInEx exclusively held `LogOutput.log`: the same payload contained a fresh DTMAPI log with runtime start, title UI initialization, and title-button visibility. Fresh DTMAPI now wins classification, and the probe closes the verified captured game PID after log collection before retrying freshness snapshots.
- Replaced label-based UTF-8 BAT flow with standalone ASCII wrappers to eliminate the final partial-command corruption; added root `6_remove_startup_capture_probe_data.bat` with exact `DELETE` confirmation.
- Cleanup removes only the Desktop capture directory/fallback error and `%LOCALAPPDATA%\DTMAPI\tools\ProcessMonitor`; it does not remove the game, DTMAPI Runtime, BepInEx, mods, configs, saves, or the extracted probe folder.
- Probe v2 adds five bounded automatic evidence files for the leading process-launch/security hypotheses: parent chain plus protected command lines and known Loader variables; IFEO/AppCompat/process mitigation; registered antivirus and selected Defender state; relevant capture-window Defender/Code Integrity/AppLocker events; critical Loader ACL/Zone summaries; and aggregated Procmon Loader results.
- Privacy boundary: v2 does not read the complete Steam/DolocTown environment block, export full environment variables, Defender exclusions, complete Windows event logs, or Zone source/referrer URLs. Suspected token/password/secret variable values are redacted.

## Changed Files

- `tools/release/startup-capture-probe/5_capture_dtmapi_startup.bat`
- `tools/release/startup-capture-probe/6_remove_startup_capture_probe_data.bat`
- `tools/scripts/capture-startup-trace.ps1`
- `tools/scripts/remove-startup-capture-probe-data.ps1`
- `tools/scripts/build-startup-capture-probe.ps1`
- `tools/scripts/build-release-workshop-packages.ps1`
- `tools/scripts/README.md`
- `docs/debug/issues/ISSUE-012-20260711-player-title-settings-runtime-missing.md`
- `docs/reviews/manual-qa/2026/20260711-0001-player-title-settings-runtime-missing.md`
- `docs/updates/2026/20260711-0011-player-title-settings-log-analysis.md`
- `docs/updates/2026/20260711-0012-player-startup-capture-script.md`
- `docs/updates/INDEX-2026-07.md`
- local-only player helper and validation evidence under `docs/debug/evidence/player-logs/20260711-yuyuyu-title-settings-window/`

## Validation

- Windows PowerShell `5.1.26100.8655` parsed every packaged PowerShell script, including `capture-startup-trace.ps1`, with exit code `0`.
- Fake game and output paths with spaces and Chinese characters passed under Windows PowerShell 5.1.
- Official runtime download passed: the helper downloaded from `https://download.sysinternals.com/files/ProcessMonitor.zip`, verified the Microsoft signer plus `Sysinternals Procmon` / `Process Monitor` product identity, cached the executable, captured, exported, classified, and zipped successfully.
- Rejection passed: an unrelated Microsoft-signed `timeout.exe` renamed to the requested Procmon path was rejected before capture.
- Destructive-filter proof passed: the exported CSV contained `245` events from only `DolocTown.exe`; the five-second PML was `1,592,815` bytes in the first bounded run.
- Full packaged flow with the existing log collector passed and produced one `3,397,602`-byte ZIP containing PML, full CSV, important-event CSV, JSON/text summary, and bounded DTMAPI logs.
- Root BAT path/host probing and explicit cancellation returned exit code `2` without launching the game.
- The originally supplied exact helper reproduced malformed partial commands (`lePath` and `ST`) from LF-only label seeking. After CRLF normalization, that same helper path reached the consent prompt and explicit cancellation returned exit code `2`.
- A forced pre-capture invalid-game-path failure under Windows PowerShell 5.1 returned exit code `1` and persisted `DTMAPI-startup-capture-last-error.txt` with the timestamp, host version, and exception text.
- Rebuilt Workshop staging package contains root BAT `5_capture_dtmapi_startup.bat` and packaged `capture-startup-trace.ps1`.
- Required Workshop matrix passed with zero blockers: missing install `1`, empty install `1`, empty check `1`, valid install `0`, post-install check `0`, collector `0`, uninstall `0`, post-uninstall check `1`.
- `tools/scripts/test.ps1` compiled all projects and `DTMAPI.UnitTests: OK`; its first governance pass rejected this Update's temporary `Validation Level: pending`, which was corrected before the final standalone governance pass.
- No live Doloc Town game run was performed. Player execution remains the acceptance boundary for ISSUE-012.
- Post-fix Workshop matrix passed with zero blockers, including packaged Windows PowerShell 5.1 parsing and package paths with spaces/non-ASCII characters.
- Fake captured-process validation passed: the probe observed a game-shaped `DolocTown.exe`, completed a five-second trace, recorded `ForcedExit`, and left no process behind.
- Cleanup validation passed against isolated exact-name paths: output, Procmon cache, and fallback error were removed; a sibling DTMAPI sentinel was preserved. Capture and cleanup cancellation both returned exit code `2` without malformed trailing commands.
- The rebuilt Runtime package contains neither root `5_capture_dtmapi_startup.bat` nor packaged `capture-startup-trace.ps1`; its required subscription matrix passed with zero blockers.
- Probe v2 final package: all five PowerShell files parse in Windows PowerShell 5.1; BATs are CRLF/ASCII-only; capture source/package hashes match; no DLL/EXE/Procmon/Runtime payload is present. A Chinese/space-path fake run generated all five new context files and exited cleanly.
- Probe v2 field-boundary validation recorded ten ACL owners and `ZoneId=3`, retained a synthetic `DOORSTOP_DISABLE=1`, redacted synthetic `DOORSTOP_API_TOKEN` to `[REDACTED]`, emitted no empty ASR rule, and left no process.
- Affected-player acceptance passed for collection mechanics: the returned helper generated the full v2 evidence set, captured 208,933 process-filtered events, produced all bounded launch/security/ACL/result summaries, included logs, closed only the captured PID with `GracefulExit`, and returned a roughly 10 MB support ZIP.
- Affected-player diagnostic result: local Doorstop proxy and configuration access are proven, but the sole Preloader event is metadata-only `QueryOpen`; no Preloader file read or fresh BepInEx/DTMAPI log occurs. The existing classifier label is acknowledged as broader than the underlying operation.
- Independent CSV recheck confirmed `PreloaderCount=1`, operation `QueryOpen`, result `SUCCESS`, zero `LogOutput.log` events, zero relevant Loader `ACCESS DENIED`/`SHARING VIOLATION`/`BAD IMAGE`, ten present critical files, and zero Zone streams or explicit deny ACEs. Document governance passed `3,923` checks.
- Post-reboot affected-player A/B recovered healthy injection in both launch conditions. The cold-Steam run created DolocTown about 71 seconds after capture began—outside the probe's 60-second wait—but its later history proves DTMAPI start and the button-visible milestone. The warm-Steam run captured Preloader `CreateFile` plus a complete 43,008-byte `ReadFile`, fresh BepInEx/DTMAPI logs, Chainloader completion, title-button visibility, and graceful probe close.
- Player evidence exposes a bounded probe defect: Steam cold start can exceed `LaunchTimeoutSeconds=60`; the probe then stops Procmon, packages an eventual success as `GameProcessNotObserved`, and cannot close the late unverified PID. This turn records the diagnosis only; no probe implementation was changed.
- Player visual acceptance passed: the affected player confirms the DTMAPI settings button was actually visible after reboot, matching the two recovered runtime visibility milestones. ISSUE-012 is recorded as restart-resolved/mitigated; exact native trigger attribution is not pursued for this incident.
- Final pre-commit validation on 2026-07-12 rebuilt the standalone ZIP as 11 entries / 37,606 bytes / SHA-256 `F1FA5D05DA625A29ADDA1B6FB1B125F280A3CD0E8131D09098B832223815AD8B`; no EXE, DLL, Process Monitor ZIP, or Procmon binary was present. Packaged PowerShell parsed under Windows PowerShell `5.1.26100.8655`, both BATs were ASCII with CRLF-only line endings, the temporary Runtime subscription matrix passed with zero blockers, and document governance passed 4,093 checks.

## Evidence

- Final Workshop audit: `docs/debug/evidence/player-logs/20260711-yuyuyu-title-settings-window/startup-capture-final-package-audit/DTMAPI Workshop Audit 20260711-211648/Results/stress-summary.md`
- Launcher-fix Workshop audit: `docs/debug/evidence/player-logs/20260711-yuyuyu-title-settings-window/startup-capture-launcher-fix-audit/DTMAPI Workshop Audit 20260711-212817/Results/stress-summary.md`
- Standalone-runtime audit: `docs/debug/evidence/player-logs/20260711-yuyuyu-title-settings-window/startup-capture-standalone-runtime-audit/DTMAPI Workshop Audit 20260711-215046/Results/stress-summary.md`
- Probe self-test analysis: `docs/debug/evidence/player-logs/20260711-yuyuyu-title-settings-window/probe-self-test-analysis.md`
- Probe v2 validation: `docs/debug/evidence/player-logs/20260711-yuyuyu-title-settings-window/probe-v2-validation.md`
- Standalone helper: `docs/debug/evidence/player-logs/20260711-yuyuyu-title-settings-window/player-helper/DTMAPI-startup-capture-probe.zip`
- Helper SHA-256: `045481743AF1291DA4FDFD34A0779D9F5CD4E7DB72EAA60AD1C269F997AD7872`
- Affected-player return: `docs/debug/evidence/player-logs/20260711-yuyuyu-title-settings-window/player-captures/DTMAPI-startup-capture-20260711-223703.zip`
- Affected-player analysis: `docs/debug/evidence/player-logs/20260711-yuyuyu-title-settings-window/player-captures/startup-capture-20260711-223703-analysis.md`
- Affected-player return SHA-256: `AED5A9B1B0A1C0ACF34A15CE4C484614E8D61BC5F8E888E0FE95AB8FB9C280FF`
- Post-reboot cold-Steam return: `docs/debug/evidence/player-logs/20260711-yuyuyu-title-settings-window/player-captures/DTMAPI-startup-capture-20260712-131334.zip`
- Post-reboot warm-Steam return: `docs/debug/evidence/player-logs/20260711-yuyuyu-title-settings-window/player-captures/DTMAPI-startup-capture-20260712-133519.zip`
- Reboot A/B analysis: `docs/debug/evidence/player-logs/20260711-yuyuyu-title-settings-window/player-captures/startup-capture-20260712-reboot-ab-analysis.md`

## Rollback

- Remove the standalone probe BATs, build script, capture/cleanup scripts, and generated probe ZIP. Existing Runtime install, uninstall, status, and log collector routes remain independent.

## Follow-up

- The affected-player return is complete; the player may run `6_remove_startup_capture_probe_data.bat`, type `DELETE`, then manually delete the extracted probe folder.
- The reboot comparison and player-visible confirmation are complete. Treat ISSUE-012 as restart-resolved; do not change DTMAPI runtime/UI or reinstall payloads from this incident.
- On an authorized probe follow-up, extend cold-Steam waiting (for example to 180 seconds), continue watching for the requested late PID before packaging, and close only a subsequently verified game process. Preserve the one-click interaction model.
- If the loader symptom recurs, capture before Steam exit/reboot and add privacy-bounded actual Steam/DolocTown `DOORSTOP_*` inspection plus an explicit native hook-status route. Do not reinstall first, because that destroys the transient comparison without testing the recovered boundary.
