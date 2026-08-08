# ISSUE-004: Steam Launch Stuck Before DolocTown.exe Creation

## Current Status

- Status: mitigated by Steam client restart; keep open for recurrence tracking
- Last observed: 2026-05-31
- Last clean recheck: 2026-07-15 19:14
- Last external observer validation: 2026-05-31 19:14
- Severity: high for automated game smoke verification
- Regression risk: medium

## Symptom

After several successful Steam-launched smoke runs, later `steam://rungameid/2285550` launches can stop before creating `DolocTown.exe`.

The smoke result then shows:

```text
StartupLog=false
GameLaunched=false
SaveLoaded=false
No DolocTown.exe process found.
No fatal instance popup found.
```

## Known Facts

- Direct `DolocTown.exe` launch remains a rejected smoke path because `ISSUE-001` can leave a fatal "Another instance is already running" popup.
- Successful runs before the blocker created and removed a real game process normally, including ActionSpeed, AutoFishing, UI lifecycle, and instant-save smoke runs.
- Steam `console_log.txt` shows the 2026-05-31 11:52 launch reaching `LaunchApp changed task to CheckShaderDepotManifest`.
- Later 2026-05-31 11:57, 12:05, and 12:09 attempts show `ExecuteSteamURL: "steam://rungameid/2285550"` but no new `CreatingProcess`, `Game process added`, or `App Running` lines.
- Evidence `docs/debug/evidence/GAME-SMOKE/20260531-115258`, `20260531-115734`, and `20260531-120507` all failed before DTMAPI startup: no `DTMAPI/logs/latest.log`, no `DolocTown.exe`, and no fatal popup.
- Evidence `docs/debug/evidence/GAME-SMOKE/20260531-123258` reproduced the same pre-process failure after the 0.1.13 follow-up build: no `DolocTown.exe`, no fatal popup, and no fresh DTMAPI startup log.
- A stale visible Steam launch window titled `启动中...` was closed via `CloseMainWindow`; subsequent visible-window enumeration found no `启动中...`, Doloc, Steam launch, or fatal popup window.
- A graceful Steam client restart using `Steam.exe -shutdown` followed by relaunch restored normal Steam game creation. Clean follow-up runs reached DTMAPI startup, third-save load, hook evidence, and clean exit.
- Later post-restart smokes stayed clean, including `GAME-SMOKE/20260531-160900` and title smoke `GAME-SMOKE/20260531-161357` with collected logs `GAME-SMOKE/20260531-160943` and `GAME-SMOKE/20260531-161545`.
- Latest normal DTMAPI segment samples are sub-second: `Bootstrap.Awake totalMs=610` in `GAME-SMOKE/20260531-160943` and `Bootstrap.Awake totalMs=636` in `GAME-SMOKE/20260531-161545`.
- `analyze-startup-evidence.ps1` now classifies startup evidence folders and reports whether a sample is normal DTMAPI startup, true DTMAPI slow startup, or Steam/pre-process launch blocking.
- Batch 3 defers native Workshop capture and Core Runtime start from BepInEx `Awake` to a one-shot first native-ready PlayerLoop frame because `DolocAPI.dataPersistenceManager`/`modManager` is not ready before the game's `GameManager.Awake` completes. Current evidence therefore measures the complete DTMAPI runtime segment with `Bootstrap.StartRuntime totalMs`; `analyze-startup-evidence.ps1` prefers that metric and falls back to historical `Bootstrap.Awake totalMs` evidence. The retained `BootstrapAwakeMs` report field/column is a compatibility label for this preferred-current/fallback-historical value, not a claim that current Core startup still runs inside BepInEx `Awake`.
- Final normal-Steam Batch 3 recheck `GAME-SMOKE/20260715-191256` was classified `NormalDtmapiStartup`: `LaunchToProcessMs=3146`, `LaunchToStartupPatternMs=26771`, `Bootstrap.StartRuntime totalMs=658`, `DiscoverMods totalMs=292`, `ModLoad elapsedMs=196`, and `Bootstrap.HarmonyInitialize elapsedMs=653`. The game reached slot 3, completed the native ReloadMods recapture and title lifecycle, then exited with no fatal popup or residual `DolocTown.exe`. The longer launch-to-startup wall clock with a sub-second DTMAPI runtime segment is not a true DTMAPI slow sample and did not reproduce the pre-process Steam blocker.
- `collect-logs.ps1` now includes `steam-appmanifest-2285550.acf`, `steam-info.txt`, and Steam log tails so the next blocked launch has Steam-side evidence without manual digging.
- `run-game-smoke.ps1` now writes collected logs, `startup-timeline.json`, and startup analysis into the same primary smoke evidence folder, so launch wall-clock timing, `result.json`, and startup segment evidence can be audited together.
- Latest integrated timeline smoke `GAME-SMOKE/20260531-164214` reached `DolocTown.exe` at `LaunchToProcessMs=4141`, reached `DTMAPI runtime starting.` at `LaunchToStartupPatternMs=6167`, and was classified as `NormalDtmapiStartup` with `Bootstrap.Awake totalMs=649`.
- `run-startup-samples.ps1` now runs repeated title-startup smokes and writes a `STARTUP-SAMPLES` aggregate with each primary `GAME-SMOKE` evidence folder plus analyzer output.
- Latest repeated sample batch `STARTUP-SAMPLES/20260531-165252` captured two consecutive clean Steam launches: `GAME-SMOKE/20260531-165253` and `GAME-SMOKE/20260531-165447`, both classified as `NormalDtmapiStartup`.
- Extended baseline batch `STARTUP-SAMPLES/20260531-170717` captured five consecutive clean Steam launches. Launch-to-startup wall-clock varied from 9173ms to 12251ms, but DTMAPI `Bootstrap.Awake totalMs` stayed between 624ms and 648ms.
- Fast pure-startup sampling now supports `-NoTitleSettingsMenu -AutoExitAfterSeconds 20`, reducing sample duration to 31-37 seconds in `STARTUP-SAMPLES/20260531-171630` while still collecting startup timeline, process, fatal-window, Steam, and DTMAPI logs.
- `run-startup-samples.ps1` now also writes `startup-sample-summary.json/md` with threshold counts and min/max/average ranges. Validation `STARTUP-SAMPLES/20260531-172605` reported `SlowLaunchCount=0`, `SlowRuntimeCount=0`, `LaunchToStartupPatternMs=9176-9192`, and `Bootstrap.Awake totalMs=586-601`.
- `run-startup-samples.ps1 -StopOnSlowSample` now stops immediately when either configured threshold is reached. Validation `STARTUP-SAMPLES/20260531-173318` used an artificial `SlowLaunchThresholdMs=1` to prove the stop path, not to claim a real slow launch.
- Real-threshold capture run `STARTUP-SAMPLES/20260531-174136` used default `SlowLaunchThresholdMs=30000`, default `SlowRuntimeThresholdMs=30000`, and `-StopOnSlowSample`; it captured six consecutive normal samples with `SlowLaunchCount=0`, `SlowRuntimeCount=0`, `LaunchToStartupPatternMs=6136-6165`, and `Bootstrap.Awake totalMs=587-615`.
- `run-startup-monitor.ps1` now wraps repeated `STARTUP-SAMPLES` batches for longer unattended capture. It stops on slow launch/runtime, pre-runtime delay, Steam blocked launch, needs-review classifications, sample failures, or leftover `DolocTown.exe`, and writes a `STARTUP-MONITOR` aggregate pointing to the triggering batch evidence.
- First monitor validation `STARTUP-MONITOR/20260531-175200` exposed a script bug in single-object PowerShell `.Count`/dynamic-property handling after two normal startup samples; the game evidence was clean and no `DolocTown.exe` remained. The script was fixed before accepting monitor validation.
- Normal monitor validation `STARTUP-MONITOR/20260531-175403` captured one two-sample batch with `Triggered=False`, `NormalDtmapiStartup=2`, `SlowLaunchCount=0`, `SlowRuntimeCount=0`, no blocked/needs-review classifications, and no leftover process.
- Artificial trigger validation `STARTUP-MONITOR/20260531-175538` used `SlowLaunchThresholdMs=1` and stopped on batch 1 with reason `slow-launch-threshold`; this proves the monitor stop path only and is not real slow-start evidence.
- Real-threshold monitor run `STARTUP-MONITOR/20260531-180222` captured two batches / six samples with default `30000ms` thresholds, `Triggered=False`, `NormalDtmapiStartup=6`, `SlowLaunchCount=0`, `SlowRuntimeCount=0`, no blocked/needs-review classifications, and no leftover process. The largest launch-to-startup wall-clock sample was `7172ms`, while `Bootstrap.Awake totalMs` stayed at `604ms` for that sample.
- Follow-up real-threshold monitor run `STARTUP-MONITOR/20260531-181048` captured three batches / nine samples with default `30000ms` thresholds, `Triggered=False`, `NormalDtmapiStartup=9`, `SlowLaunchCount=0`, `SlowRuntimeCount=0`, no blocked/needs-review classifications, and no leftover process. The largest launch-to-startup wall-clock sample was `7170ms`, while `Bootstrap.Awake totalMs` stayed at `612ms` or lower in that batch.
- Title-settings monitor attempt `STARTUP-MONITOR/20260531-182655` exposed that child smoke failures could be obscured by PowerShell native-command error wrapping, causing the sample parser to keep `Evidence: :String) [], RemoteException` instead of the real primary smoke path.
- `run-startup-samples.ps1` and `run-startup-monitor.ps1` now temporarily relax `$ErrorActionPreference` while running child PowerShell processes and then restore it, so child smoke failures are preserved as sample data instead of aborting the aggregate writer.
- `run-startup-samples.ps1` now only accepts existing `GAME-SMOKE` evidence paths when parsing child output, avoiding false evidence paths from PowerShell error wrapper text.
- Failure-capture validation `STARTUP-MONITOR/20260531-182957` stopped with `sample-failure-count-1` for the title-settings path, but the linked sample still classified primary smoke `GAME-SMOKE/20260531-182958` as `NormalDtmapiStartup`: `LaunchToStartupPatternMs=6153`, `Bootstrap.Awake totalMs=657`, no fatal popup, and no leftover process.
- Post-fix no-title recheck `STARTUP-SAMPLES/20260531-183210` captured one clean normal startup sample: `LaunchToStartupPatternMs=6159`, `Bootstrap.Awake totalMs=610`, `SlowLaunchCount=0`, `SlowRuntimeCount=0`, no fatal popup, and no leftover process.
- Longer real-threshold monitor run `STARTUP-MONITOR/20260531-183958` captured four batches / twelve samples with default `30000ms` thresholds, `Triggered=False`, `NormalDtmapiStartup=12`, `SlowLaunchCount=0`, `SlowRuntimeCount=0`, no blocked/needs-review classifications, and no leftover process. Across all samples `LaunchToStartupPatternMs=6132-6172`, `Bootstrap.Awake totalMs=584-638`, `DiscoverMods totalMs=53-64`, `ModLoad elapsedMs=387-431`, and `Bootstrap.HarmonyInitialize elapsedMs=528-578`.
- Follow-up longer real-threshold monitor run `STARTUP-MONITOR/20260531-185341` captured five batches / twenty samples with default `30000ms` thresholds, `Triggered=False`, `NormalDtmapiStartup=20`, `SlowLaunchCount=0`, `SlowRuntimeCount=0`, no blocked/needs-review classifications, and no leftover process. Across all samples `LaunchToStartupPatternMs=6133-7181`, `Bootstrap.Awake totalMs=586-617`, `DiscoverMods totalMs=53-64`, `ModLoad elapsedMs=388-408`, and `Bootstrap.HarmonyInitialize elapsedMs=530-558`.
- `run-startup-observer.ps1` now captures externally triggered launches without launching the game itself. It clears stale startup logs by default when the game is not already running, waits for `DolocTown.exe` and a fresh `DTMAPI runtime starting.` line, writes `STARTUP-OBSERVE/startup-timeline.json`, then collects/analyzes logs through the same startup analyzer.
- Timeout validation `STARTUP-OBSERVE/20260531-191241` ran with no external launch and did not misclassify stale logs: `StartupFound=false`, no process observed, no fresh DTMAPI log, and analyzer classification `SteamLaunchBlockedBeforeProcessOrNoFreshRuntimeLog`.
- External Steam URL validation `STARTUP-OBSERVE/20260531-191352` observed a game launch that was triggered outside the observer script. It recorded `LaunchToProcessMs=6208`, `LaunchToStartupPatternMs=7236`, `Bootstrap.Awake totalMs=230`, `DiscoverMods totalMs=53`, `ModLoad elapsedMs=37`, and `Bootstrap.HarmonyInitialize elapsedMs=173`. The game did not auto-exit under this observer-only path, so Codex closed the launched game window afterward and wrote `process-check-after-cleanup.txt` with no remaining `DolocTown.exe`.
- `compare-startup-evidence.ps1` now produces `STARTUP-COMPARE/startup-comparison.*` for explicit normal-vs-candidate startup comparison. Validation `STARTUP-COMPARE/20260531-192226` compared normal external-launch evidence `STARTUP-OBSERVE/20260531-191352` with blocked/no-launch evidence `STARTUP-OBSERVE/20260531-191241` and reported `OnlyPreRuntimeOrBlockedAbnormalEvidence`, confirming that blocked/no-fresh-log evidence is not enough to satisfy the true DTMAPI runtime slow comparison.
- Goal audit `docs/updates/2026/20260531-0032-goal-completion-audit.md` records A-E as evidenced and keeps F incomplete because no sample has both a fresh DTMAPI runtime log and a 30-second DTMAPI runtime segment.

## Rejected Hypotheses

- Not a DTMAPI 30s startup regression: no DTMAPI startup log exists in the failed runs.
- Not a hook failure: the game process was never created, so BepInEx and DTMAPI hooks never ran.
- Not the known fatal direct-EXE popup: fatal-window checks were negative for the failed Steam runs.
- Not a leftover game process: process checks reported no `DolocTown.exe`.
- Not enough evidence to optimize DTMAPI startup: no captured run currently has both `StartupLog=true` and a 30s DTMAPI startup segment.

## Attempts

- Closed only the visible `steamwebhelper` launch window whose title was `启动中...`.
- Re-ran `run-game-smoke.ps1 -SaveSlot 3 -TimeoutSeconds 240 -AutoExerciseAutoFishingPhase -AutoPressAutoFishingHotkey -SkipBuild`.
- Checked process state, fatal-window state, visible desktop windows, and Steam logs after the failed run.
- 2026-05-31 12:32:58: reproduced with `run-game-smoke.ps1 -SaveSlot 3 -TimeoutSeconds 240 -AutoExerciseActionSpeedTool -SkipBuild`; evidence `GAME-SMOKE/20260531-123258` again had no process creation.
- 2026-05-31 12:43:15: ran `Steam.exe -shutdown`, waited for Steam processes to exit, relaunched Steam, then reran the same smoke successfully: `GAME-SMOKE/20260531-124346`, collected logs `GAME-SMOKE/20260531-124429`.
- 2026-05-31 12:53:56 and 12:55:29: additional F6 and OneAction smokes also succeeded after the Steam restart, with no leftover `DolocTown.exe`.
- 2026-05-31 16:09 and 16:14: later OneAction vegetation and title-menu smokes also succeeded, with startup segments at 610ms and 636ms respectively.
- 2026-05-31 16:27: added and ran startup evidence analysis. `STARTUP-COMPARE/20260531-162725` classified two normal DTMAPI startup samples, zero true DTMAPI slow samples, and one Steam/pre-process blocked sample.
- 2026-05-31 16:27: ran `collect-logs.ps1 -CaseId STARTUP-COLLECT-TEST`; evidence `STARTUP-COLLECT-TEST/20260531-162737` includes Steam appmanifest, Steam log tails, and a generated startup analysis.
- 2026-05-31 16:33: ran title smoke after integrating collection into `run-game-smoke`; evidence `GAME-SMOKE/20260531-163330` contains result JSON, runtime logs, Steam appmanifest/log tails, startup analysis, process check, and fatal-window check in one folder. The analyzer classified it as normal DTMAPI startup with `Bootstrap.Awake totalMs=659`.
- 2026-05-31 16:42: ran title smoke after adding launch wall-clock timeline capture; evidence `GAME-SMOKE/20260531-164214` contains `startup-timeline.json` with `LaunchMode=Steam`, `LaunchToProcessMs=4141`, `LaunchToDtmapiLogFileMs=6165`, `LaunchToStartupPatternMs=6167`, and `TimedOut=false`. The analyzer classified it as normal DTMAPI startup with no leftover process and no fatal popup.
- 2026-05-31 16:52-16:56: ran `run-startup-samples.ps1 -Count 2 -TimeoutSeconds 120 -DelaySeconds 5 -SkipBuild`; aggregate evidence `STARTUP-SAMPLES/20260531-165252` classified both samples as normal DTMAPI startup with no leftover process and no fatal popup. The two samples recorded `LaunchToProcessMs=4128/4134`, `LaunchToStartupPatternMs=6155/6163`, and `Bootstrap.Awake totalMs=644/632`.
- 2026-05-31 17:07-17:12: ran `run-startup-samples.ps1 -Count 5 -TimeoutSeconds 60 -DelaySeconds 5 -SkipBuild`; aggregate evidence `STARTUP-SAMPLES/20260531-170717` classified all five samples as normal DTMAPI startup with no leftover process and no fatal popup. The samples recorded `LaunchToProcessMs=6147-10232`, `LaunchToStartupPatternMs=9173-12251`, and `Bootstrap.Awake totalMs=624-648`.
- 2026-05-31 17:16-17:19: added fast pure-startup exit override and ran `run-startup-samples.ps1 -Count 5 -TimeoutSeconds 60 -DelaySeconds 3 -AutoExitAfterSeconds 20 -NoTitleSettingsMenu -SkipBuild`; aggregate evidence `STARTUP-SAMPLES/20260531-171630` classified all five samples as normal DTMAPI startup with no leftover process and no fatal popup. The samples recorded `LaunchToProcessMs=7166-12244`, `LaunchToStartupPatternMs=9205-14277`, and `Bootstrap.Awake totalMs=596-612`.
- 2026-05-31 17:26-17:27: added threshold summary output and ran `run-startup-samples.ps1 -Count 2 -TimeoutSeconds 60 -DelaySeconds 3 -AutoExitAfterSeconds 20 -NoTitleSettingsMenu -SkipBuild`; aggregate evidence `STARTUP-SAMPLES/20260531-172605` generated `startup-sample-summary.md` and classified both samples as normal DTMAPI startup with no leftover process and no fatal popup.
- 2026-05-31 17:33: added slow-sample stop support and validated it with `run-startup-samples.ps1 -Count 3 -TimeoutSeconds 60 -DelaySeconds 3 -AutoExitAfterSeconds 20 -NoTitleSettingsMenu -SkipBuild -StopOnSlowSample -SlowLaunchThresholdMs 1`; aggregate evidence `STARTUP-SAMPLES/20260531-173318` stopped after sample 1 with `StoppedOnSlowSample=1`, `StoppedOnSlowLaunch=True`, and `StoppedOnSlowRuntime=False`.
- 2026-05-31 17:41-17:44: ran real-threshold stop-on-slow sampling with `run-startup-samples.ps1 -Count 6 -TimeoutSeconds 70 -DelaySeconds 5 -AutoExitAfterSeconds 20 -NoTitleSettingsMenu -SkipBuild -StopOnSlowSample`; aggregate evidence `STARTUP-SAMPLES/20260531-174136` captured all six requested samples without hitting a 30000ms launch/runtime threshold. All samples were classified as `NormalDtmapiStartup`, with no leftover process and no fatal popup.
- 2026-05-31 17:52-17:53: first `run-startup-monitor.ps1` validation hit a monitor script bug after creating `STARTUP-MONITOR/20260531-175200` and `STARTUP-SAMPLES/20260531-175201`; the underlying two startup samples were normal and there was no leftover process. Fixed the monitor property/count handling before proceeding.
- 2026-05-31 17:54-17:55: reran monitor validation with `run-startup-monitor.ps1 -MaxBatches 1 -BatchSize 2 -TimeoutSeconds 70 -DelaySeconds 3 -AutoExitAfterSeconds 20 -DelaySecondsBetweenBatches 3 -SkipBuild`; aggregate evidence `STARTUP-MONITOR/20260531-175403` completed with `Triggered=False`, linked `STARTUP-SAMPLES/20260531-175403`, and captured two normal DTMAPI startup samples.
- 2026-05-31 17:55-17:56: ran artificial trigger validation with `run-startup-monitor.ps1 -MaxBatches 3 -BatchSize 2 -TimeoutSeconds 70 -DelaySeconds 3 -AutoExitAfterSeconds 20 -DelaySecondsBetweenBatches 3 -SlowLaunchThresholdMs 1 -SkipBuild`; aggregate evidence `STARTUP-MONITOR/20260531-175538` stopped after batch 1 with reason `slow-launch-threshold`, linked `STARTUP-SAMPLES/20260531-175538`, and left no `DolocTown.exe`.
- 2026-05-31 18:02-18:05: ran real-threshold monitor capture with `run-startup-monitor.ps1 -MaxBatches 2 -BatchSize 3 -TimeoutSeconds 70 -DelaySeconds 3 -AutoExitAfterSeconds 20 -DelaySecondsBetweenBatches 10 -SkipBuild`; aggregate evidence `STARTUP-MONITOR/20260531-180222` completed all two batches with `Triggered=False`, linked `STARTUP-SAMPLES/20260531-180222` and `STARTUP-SAMPLES/20260531-180406`, and captured six normal DTMAPI startup samples.
- 2026-05-31 18:10-18:15: ran another real-threshold monitor capture with `run-startup-monitor.ps1 -MaxBatches 3 -BatchSize 3 -TimeoutSeconds 70 -DelaySeconds 3 -AutoExitAfterSeconds 20 -DelaySecondsBetweenBatches 10 -SkipBuild`; aggregate evidence `STARTUP-MONITOR/20260531-181048` completed all three batches with `Triggered=False`, linked `STARTUP-SAMPLES/20260531-181048`, `STARTUP-SAMPLES/20260531-181230`, and `STARTUP-SAMPLES/20260531-181413`, and captured nine normal DTMAPI startup samples.
- 2026-05-31 18:21-18:28: tried title-settings startup monitor with `-IncludeTitleSettingsMenu`; first attempt `STARTUP-MONITOR/20260531-182126` aborted before aggregate output, and second attempt `STARTUP-MONITOR/20260531-182655` proved the need to filter wrapped PowerShell error output when parsing primary evidence paths.
- 2026-05-31 18:29-18:31: reran the title-settings monitor after fixing child process capture and evidence-path parsing; aggregate evidence `STARTUP-MONITOR/20260531-182957` linked `STARTUP-SAMPLES/20260531-182958`, stopped on `sample-failure-count-1`, and still preserved normal DTMAPI runtime timing for primary smoke `GAME-SMOKE/20260531-182958`.
- 2026-05-31 18:32: ran no-title post-fix sample `run-startup-samples.ps1 -Count 1 -TimeoutSeconds 70 -DelaySeconds 5 -AutoExitAfterSeconds 20 -NoTitleSettingsMenu -SkipBuild`; evidence `STARTUP-SAMPLES/20260531-183210` and primary smoke `GAME-SMOKE/20260531-183211` were normal and left no process.
- 2026-05-31 18:39-18:46: ran a longer real-threshold monitor capture with `run-startup-monitor.ps1 -MaxBatches 4 -BatchSize 3 -TimeoutSeconds 70 -DelaySeconds 3 -AutoExitAfterSeconds 20 -DelaySecondsBetweenBatches 10 -SkipBuild`; aggregate evidence `STARTUP-MONITOR/20260531-183958` completed all four batches with `Triggered=False`, linked `STARTUP-SAMPLES/20260531-183958`, `STARTUP-SAMPLES/20260531-184141`, `STARTUP-SAMPLES/20260531-184325`, and `STARTUP-SAMPLES/20260531-184508`, and captured twelve normal DTMAPI startup samples.
- 2026-05-31 18:53-19:04: ran another longer real-threshold monitor capture with `run-startup-monitor.ps1 -MaxBatches 5 -BatchSize 4 -TimeoutSeconds 70 -DelaySeconds 2 -AutoExitAfterSeconds 20 -DelaySecondsBetweenBatches 8 -SkipBuild`; aggregate evidence `STARTUP-MONITOR/20260531-185341` completed all five batches with `Triggered=False`, linked `STARTUP-SAMPLES/20260531-185341`, `STARTUP-SAMPLES/20260531-185551`, `STARTUP-SAMPLES/20260531-185759`, `STARTUP-SAMPLES/20260531-190010`, and `STARTUP-SAMPLES/20260531-190217`, and captured twenty normal DTMAPI startup samples.
- 2026-05-31 19:12: added `run-startup-observer.ps1` and validated its no-launch timeout path with `run-startup-observer.ps1 -TimeoutSeconds 3`; evidence `STARTUP-OBSERVE/20260531-191241` timed out without stale-log false positives.
- 2026-05-31 19:13-19:14: validated the observer success path by starting `run-startup-observer.ps1 -TimeoutSeconds 90 -WaitForExitSeconds 45`, then triggering `steam://rungameid/2285550` from a separate process. Evidence `STARTUP-OBSERVE/20260531-191352` captured the external launch and normal DTMAPI startup segment timings. The observer does not force game exit, so Codex closed the launched window afterward and confirmed no leftover process.
- 2026-05-31 19:22: added and validated `compare-startup-evidence.ps1` with normal evidence `STARTUP-OBSERVE/20260531-191352` and blocked candidate `STARTUP-OBSERVE/20260531-191241`; evidence `STARTUP-COMPARE/20260531-192226` reported `RuntimeSlowCount=0`, `BlockedCount=1`, and status `OnlyPreRuntimeOrBlockedAbnormalEvidence`.
- 2026-05-31 19:30: completed a goal-level audit in `docs/updates/2026/20260531-0032-goal-completion-audit.md`. Result: A-E have sufficient evidence; F remains pending/blocked on the first true DTMAPI runtime slow sample and must not be closed from blocked/pre-runtime evidence alone.

## Evidence

- Blocked smoke result: `docs/debug/evidence/GAME-SMOKE/20260531-120507/result.json`
- Blocked smoke summary: `docs/debug/evidence/GAME-SMOKE/20260531-120507/summary.txt`
- Process/fatal checks: `docs/debug/evidence/GAME-SMOKE/20260531-120507/process-check.txt`, `fatal-window-check.txt`
- Earlier blocked attempts: `docs/debug/evidence/GAME-SMOKE/20260531-115258`, `docs/debug/evidence/GAME-SMOKE/20260531-115734`
- Reproduced blocked attempt: `docs/debug/evidence/GAME-SMOKE/20260531-123258`
- Recovery evidence after Steam restart: `docs/debug/evidence/GAME-SMOKE/20260531-124346`, collected logs `docs/debug/evidence/GAME-SMOKE/20260531-124429`
- Additional clean post-restart evidence: `docs/debug/evidence/GAME-SMOKE/20260531-125356`, logs `docs/debug/evidence/GAME-SMOKE/20260531-125453`; `docs/debug/evidence/GAME-SMOKE/20260531-125529`, logs `docs/debug/evidence/GAME-SMOKE/20260531-125613`
- Latest clean normal startup samples: `docs/debug/evidence/GAME-SMOKE/20260531-160900`, logs `docs/debug/evidence/GAME-SMOKE/20260531-160943`; `docs/debug/evidence/GAME-SMOKE/20260531-161357`, logs/screenshots `docs/debug/evidence/GAME-SMOKE/20260531-161545`
- Startup comparison report: `docs/debug/evidence/STARTUP-COMPARE/20260531-162725/startup-analysis.md`
- Log collection validation: `docs/debug/evidence/STARTUP-COLLECT-TEST/20260531-162737`
- Integrated smoke evidence: `docs/debug/evidence/GAME-SMOKE/20260531-163330`
- Integrated timeline smoke evidence: `docs/debug/evidence/GAME-SMOKE/20260531-164214`
- Repeated startup sample evidence: `docs/debug/evidence/STARTUP-SAMPLES/20260531-165252`, primary smokes `docs/debug/evidence/GAME-SMOKE/20260531-165253` and `docs/debug/evidence/GAME-SMOKE/20260531-165447`
- Extended baseline sample evidence: `docs/debug/evidence/STARTUP-SAMPLES/20260531-170717`, primary smokes `docs/debug/evidence/GAME-SMOKE/20260531-170718`, `docs/debug/evidence/GAME-SMOKE/20260531-170815`, `docs/debug/evidence/GAME-SMOKE/20260531-170912`, `docs/debug/evidence/GAME-SMOKE/20260531-171007`, and `docs/debug/evidence/GAME-SMOKE/20260531-171105`
- Fast pure-startup sample evidence: `docs/debug/evidence/STARTUP-SAMPLES/20260531-171630`, primary smokes `docs/debug/evidence/GAME-SMOKE/20260531-171631`, `docs/debug/evidence/GAME-SMOKE/20260531-171705`, `docs/debug/evidence/GAME-SMOKE/20260531-171745`, `docs/debug/evidence/GAME-SMOKE/20260531-171821`, and `docs/debug/evidence/GAME-SMOKE/20260531-171859`
- Threshold summary validation evidence: `docs/debug/evidence/STARTUP-SAMPLES/20260531-172605`, primary smokes `docs/debug/evidence/GAME-SMOKE/20260531-172606` and `docs/debug/evidence/GAME-SMOKE/20260531-172640`
- Stop-on-slow validation evidence: `docs/debug/evidence/STARTUP-SAMPLES/20260531-173318`, primary smoke `docs/debug/evidence/GAME-SMOKE/20260531-173318`
- Real-threshold stop-on-slow baseline evidence: `docs/debug/evidence/STARTUP-SAMPLES/20260531-174136`, primary smokes `docs/debug/evidence/GAME-SMOKE/20260531-174136`, `docs/debug/evidence/GAME-SMOKE/20260531-174209`, `docs/debug/evidence/GAME-SMOKE/20260531-174243`, `docs/debug/evidence/GAME-SMOKE/20260531-174317`, `docs/debug/evidence/GAME-SMOKE/20260531-174351`, and `docs/debug/evidence/GAME-SMOKE/20260531-174425`
- Startup monitor failed validation evidence: `docs/debug/evidence/STARTUP-MONITOR/20260531-175200`, linked samples `docs/debug/evidence/STARTUP-SAMPLES/20260531-175201`
- Startup monitor normal validation evidence: `docs/debug/evidence/STARTUP-MONITOR/20260531-175403`, linked samples `docs/debug/evidence/STARTUP-SAMPLES/20260531-175403`
- Startup monitor artificial trigger evidence: `docs/debug/evidence/STARTUP-MONITOR/20260531-175538`, linked samples `docs/debug/evidence/STARTUP-SAMPLES/20260531-175538`
- Real-threshold monitor run evidence: `docs/debug/evidence/STARTUP-MONITOR/20260531-180222`, linked samples `docs/debug/evidence/STARTUP-SAMPLES/20260531-180222` and `docs/debug/evidence/STARTUP-SAMPLES/20260531-180406`
- Follow-up real-threshold monitor run evidence: `docs/debug/evidence/STARTUP-MONITOR/20260531-181048`, linked samples `docs/debug/evidence/STARTUP-SAMPLES/20260531-181048`, `docs/debug/evidence/STARTUP-SAMPLES/20260531-181230`, and `docs/debug/evidence/STARTUP-SAMPLES/20260531-181413`
- Failed title-settings monitor capture before path-parser fix: `docs/debug/evidence/STARTUP-MONITOR/20260531-182655`, linked partial samples `docs/debug/evidence/STARTUP-SAMPLES/20260531-182655`, primary smoke `docs/debug/evidence/GAME-SMOKE/20260531-182655`
- Title-settings failure-capture validation after parser fix: `docs/debug/evidence/STARTUP-MONITOR/20260531-182957`, linked samples `docs/debug/evidence/STARTUP-SAMPLES/20260531-182958`, primary smoke `docs/debug/evidence/GAME-SMOKE/20260531-182958`
- Post-fix no-title startup sample: `docs/debug/evidence/STARTUP-SAMPLES/20260531-183210`, primary smoke `docs/debug/evidence/GAME-SMOKE/20260531-183211`
- Longer real-threshold monitor evidence: `docs/debug/evidence/STARTUP-MONITOR/20260531-183958`, linked samples `docs/debug/evidence/STARTUP-SAMPLES/20260531-183958`, `docs/debug/evidence/STARTUP-SAMPLES/20260531-184141`, `docs/debug/evidence/STARTUP-SAMPLES/20260531-184325`, and `docs/debug/evidence/STARTUP-SAMPLES/20260531-184508`
- Follow-up longer real-threshold monitor evidence: `docs/debug/evidence/STARTUP-MONITOR/20260531-185341`, linked samples `docs/debug/evidence/STARTUP-SAMPLES/20260531-185341`, `docs/debug/evidence/STARTUP-SAMPLES/20260531-185551`, `docs/debug/evidence/STARTUP-SAMPLES/20260531-185759`, `docs/debug/evidence/STARTUP-SAMPLES/20260531-190010`, and `docs/debug/evidence/STARTUP-SAMPLES/20260531-190217`
- Startup observer timeout validation: `docs/debug/evidence/STARTUP-OBSERVE/20260531-191241`
- Startup observer external-launch validation: `docs/debug/evidence/STARTUP-OBSERVE/20260531-191352`
- Startup comparison validation: `docs/debug/evidence/STARTUP-COMPARE/20260531-192226`
- Final normal-Steam Batch 3 startup/source recheck: `docs/debug/evidence/GAME-SMOKE/20260715-191256`
- Related successful pre-blocker F6 evidence: `docs/debug/evidence/GAME-SMOKE/20260531-112959`

## Acceptance Criteria

Do not mark resolved until the recovery repeats without requiring a full Steam restart:

- Steam launches Doloc Town again through the default smoke path.
- A run reaches `DTMAPI runtime starting.`, `GameLaunched dispatched.`, and third-save `SaveLoaded hook dispatched.`.
- The run exits with no `DolocTown.exe` and no fatal instance popup.
- A smoke matrix note links the clean evidence and states whether Steam needed manual restart or another external action.
