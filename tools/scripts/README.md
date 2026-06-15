# Scripts

Expected script surface:

- `build.ps1`: compile all DTMAPI projects.
- `test.ps1`: run unit/integration tests.
- `install-to-game.ps1`: install local DTMAPI build into the configured Doloc Town game directory.
- `run-game-smoke.ps1`: launch game through Steam by default, write `startup-timeline.json`, wait for startup/save evidence, collect logs with full runtime evidence payloads, verify process exit, and fail on the fatal instance popup even if the process has already exited. Direct `DolocTown.exe` launch is opt-in with `-DirectExe` because the game calls Steam restart logic and can show `Fatal error: Another instance is already running` before the title menu.
- AutoFishing phase smokes are special: run them with explicit `-SaveSlot 5` for the real pond/fishing-rod fixture. Use `-AutoFishingScenario FastAnimations -AutoFishingCastChargeRatio <0..1>` when validating configured Ready charge; the smoke must observe a matching `Smoke.AutoFishingCastCharge` target. Use `-AutoFishingToggleKey <key>` with `-AutoPressAutoFishingHotkey` when validating a rebound toggle key such as `F7`.
- `run-startup-samples.ps1`: run repeated title-startup smokes, keep each primary `GAME-SMOKE` evidence folder, and write a `STARTUP-SAMPLES` aggregate with `startup-samples.*`, `startup-sample-summary.*`, and analyzer output for normal/slow/blocked classification. Use `-NoTitleSettingsMenu -AutoExitAfterSeconds 20` for faster pure-startup sampling without UI screenshot evidence, and `-StopOnSlowSample` to stop as soon as a launch/runtime threshold is reached. Failed child smokes are still captured and linked when a `GAME-SMOKE` evidence folder exists.
- `run-startup-monitor.ps1`: run one or more `STARTUP-SAMPLES` batches, stop on slow launch/runtime, pre-runtime delay, Steam blocked launch, needs-review, sample failure, or leftover `DolocTown.exe`, and write a `STARTUP-MONITOR` aggregate that points to the triggering batch evidence. Failed child sample batches are preserved instead of aborting the monitor before it can write the aggregate.
- `run-startup-observer.ps1`: wait for an externally launched Doloc Town process and fresh DTMAPI startup log without launching the game itself, write `STARTUP-OBSERVE` evidence with `startup-timeline.json`, then collect/analyze logs. Use this when the 30-second startup appears only through manual Steam/UI launch paths.
- `run-hook-probe.ps1`: run HookProbe checks against the local third save when possible.
- `collect-logs.ps1`: collect BepInEx, current DTMAPI `latest.log`, the 10 most recent DTMAPI `latest-*.log` history files, Player, Steam/appmanifest, process, fatal-window, and hook logs. It can write into an existing evidence folder with `-OutputDirectory`. By default it records the runtime `DTMAPI/evidence` root and recent child folders without recursively copying the whole runtime evidence tree; use `-IncludeRuntimeEvidence` only when a full screenshot/runtime payload is required.
- `analyze-startup-evidence.ps1`: parse one or more evidence folders and classify normal DTMAPI startup segments, true 30s DTMAPI slow-start samples, pre-runtime launch delays, or Steam/pre-process launch blockers. Use `-Quiet` when another script embeds it.
- `compare-startup-evidence.ps1`: run startup analysis for normal plus candidate abnormal evidence, then write `startup-comparison.*` with normal ranges, runtime-slow ranges when present, and a status that says whether the requested normal-vs-abnormal comparison is actually ready.
- `disable-smoke-settings.ps1`: turn off smoke automation after an interrupted or invalid run.
- `status.ps1`: print local game/runtime/debug status.
- `package-report.ps1`: zip the latest evidence bundle.
- `build-native-function-map-data.ps1`: generate the docs-only native function-map workbench data from local reverse metadata, map indexes, and DTMAPI-authored native-owner reports. It writes symbol/call graph JSON under `docs/reviews/api/native-function-map/data` and does not copy decompiled method bodies.

Scripts must not hard-code the user's Steam path. Use local settings or environment variables.

Set `DTMAPI_RUNTIME_DIR` or `DTMAPI_STATE_DIR` to redirect DTMAPI runtime state (`logs`,
`reports`, `evidence`, `config`, `smoke-settings.json`) away from the game directory.

`SecondMotorMod` was archived on 2026-06-15, and the remaining MotorVehicle API/GameBridge
code was retired in cleanup Round 2. `run-game-smoke.ps1 -AutoExerciseVehicle` now returns
a blocked archived result, and `-DisableSecondMotorForSmoke` remains only as a no-op
compatibility flag for old commands.
