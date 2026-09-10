# Scripts

This is a command reference, not a reading or execution checklist. Search for the script/scenario needed by the current task; do not read or run every entry.

## Choose validation

| Need | Entry |
| --- | --- |
| Docs/links | `check-doc-governance.ps1`; resolve links in the changed files |
| One product or test boundary | `test-unit.ps1 -Focus <name>`; use `-List` to inspect its actual project scope |
| Existing test outputs only | `test-unit.ps1 -Focus <name> -NoBuild`; unchanged relevant inputs are required |
| Default Unit/product source coverage | `test-unit.ps1`; builds only the default test graphs and their dependencies |
| Public source CI | `test-public-source.ps1`; source/redistributable inputs, without private game or Release package evidence |
| Compile the framework/example/test solution | `build.ps1 -Configuration Release -SkipTests`; no tests |
| Compile plus all source suites | `build.ps1 -Configuration Release`; broader than a focused fix |
| Complete Release/integration boundary | `test.ps1 -Configuration Release`; includes its build and package/ABI checks; do not prepend `build.ps1` |
| Release failure diagnosis | `test.ps1 -List`; `-Stage <name>` runs one stage, `-StartAt <name>` runs its tail; neither establishes full acceptance |
| Test-dispatch changes | `test-unit-routing.ps1`, `test-release-routing.ps1`; `test-test-focus-routing.ps1 -EntryGuardsOnly` checks full-entry guards |
| Ordinary game evidence | Direct game steps or a fitting smoke scenario under [product validation](../../docs/workflows/product-change-validation.md) |

From the repository root:

```powershell
./tools/scripts/test-unit.ps1 -List
./tools/scripts/test-unit.ps1 -Focus moresaves-product
./tools/scripts/test-unit.ps1 -Focus moreequipment-product -List
```

`-Focus` takes an existing focus or a suite ID with default tests. A focus spanning several owners runs those projects in separate processes; a product-only suite ID selects only that product graph. Unknown and blank explicit focuses fail before SDK lookup or building. An explicit focus overrides `DTMAPI_UNIT_TEST_FOCUS`; full build/Release/public-source entrypoints reject ambient focus filters.

`-NoBuild` executes existing DLLs directly and does not prepare dependencies or invoke MSBuild. The old `DTMAPI.UnitTests` binary is a thin compatibility dispatcher using this mode. On a clean tree, start with `test-unit.ps1`; building only the old launcher does not build new suites. A focused PASS is evidence for its selected boundary.

Release diagnostic stages reuse the prepared build and SDK outputs. Rebuild changed inputs with the relevant project/build entry first; `BuildAndUnit` includes its build. `PostRuntimeInstaller` remains an alias for the `ScriptContracts` tail. Use the supported selectors rather than copying/slicing the driver. Real runs check conflicting tests/game processes first. The full run checks evidence-retention freshness before building; diagnostic selections check it only when they include `ScriptContracts`. `-List` only prints the selection. Product/QA/ABI gates run before the long installer transaction matrices. Keep the candidate and evidence-bearing records unchanged during a complete run. Repair an affected stage first, then perform the required final complete run once.

## Script reference

Expected script surface:

- `preflight-workspace.ps1`: read-only checks selected by `BuildProduct`, `TestProduct` (`Unit` or `Game`), `SourceChecks`, or `CaptureGame`; reports missing prerequisites without installing or launching anything.
- `prepare-workspace.ps1 -Dependency DotNet,AuthorSdk`: prepare only the selected dependencies. The repository [global.json](../../global.json) owns SDK selection; `Get-DotNetExe` verifies actual SDK execution and runtime availability in that repository. `prepare-author-sdk.ps1 -OutputRoot <path>` explicitly selects/reuses an SDK output.
- `test-dotnet-toolchain.ps1`: real-host and mixed-version fixtures for SDK selection, stable patch boundaries, missing runtime and read-only refusal; no build or provisioning.
- `test-unit.ps1`: select independent compilation and execution graphs from `tests/DTMAPI.UnitTests/suites.json`; `-List`, `-Focus`, `-NoBuild`, and `-BuildOnly` use that same map.
- `test-public-source.ps1`: CI profile using tracked source, redistributable and public symbol-map fixtures; builds one solution filter, then runs all default Unit/product, QA, Doctor and installer source tests, the three public SDK focuses, and synthetic player support, smoke runner, reverse comparison and history-tool checks. Requires Windows, Python 3 and Node.js; Actions prepares these explicitly. Exact private game/ABI inputs and the full Release/package matrix remain separate evidence.
- `test-unit-routing.ps1`: Windows PowerShell 5.1 behavior tests for early focus rejection, explicit focus precedence, default product coverage, and `-NoBuild` without dependency preparation/building. Run when changing test dispatch, not after every product edit.
- `prepare-unit-test-dependencies.ps1`: stage selected public test fixture dependencies under `.tools`; the test entry invokes it only for selected graphs that need them and only when building.
- `prepare-author-sdk-compatibility.ps1`: rebuild the frozen API payload from its tracked DTMAPI source inputs. Public CI prepares this small payload before its SDK focuses; `-NoBuild` only checks the existing payload. Source provenance and byte identity live beside the frozen contract, without requiring old packages or Git history.
- `build-batch6-advanced-product.ps1`: ordinary Advanced product packaging uses validation followed by one `pack --build-output` compilation; `pack-report.json` owns compilation/package evidence.
- `build.ps1`: build `DTMAPI.sln` once and, unless `-SkipTests`, run the default split Unit suites plus QA, Doctor, installer and SDK tests. The solution owns ordinary project membership; Advanced product packages use the Catalog/Author SDK route. Non-Release runs also build the fixed Release compatibility assembly required by SDK tests.
- `test.ps1`: build once, run complete source/package/ABI validation, then document checks. `-StartAt PostRuntimeInstaller` is a diagnostic tail, never a full PASS.
- `install-to-game.ps1`: install local DTMAPI build into the configured Doloc Town game directory.
- `run-game-smoke.ps1`: launch through Steam by default, write `startup-timeline.json`, wait for startup/save evidence, collect only runtime evidence created or updated since this run began, verify process exit, and fail on the fatal instance popup even if the process has already exited. Automated fixture scenarios additionally require the receipt-bound optional QA host with `-StageQaHost`; direct game evidence does not. The runner never writes a player-side automation settings file. Direct `DolocTown.exe` launch is opt-in with `-DirectExe` because the game calls Steam restart logic and can show `Fatal error: Another instance is already running` before the title menu.
- Historical CameraView compatibility validation uses `-AutoExerciseZoom` and the QA-owned `CameraPlayable` fixture. Current Zoom ProductNative validation uses `-AutoExerciseZoomProductNative` plus `-AssertAdvancedProductOwnerDeactivation -AdvancedProductOwnerDeactivationOwnerIds DTMAPI.ZoomMod`; the old Strict `ZoomOwnerLifetime` seam has been removed.
- Y-console game routes are special: pass explicit `-SaveSlot 10` for the tenth slot shown in the game UI (native archive index `9`). `run-game-smoke.ps1` rejects Y-console routes that omit this value or still use the old third-slot fixture; unrelated game/Hook routes retain their own defaults.
- AutoFishing phase smokes are special: run them with explicit `-SaveSlot 5` for the real pond/fishing-rod fixture. `-AutoFishingCastChargeRatio 0..1` is written to the first-party product config and values above zero enable the `AutoFishingCastCharge` result gate; use 0/0.5/1 for minimum, midpoint, and full-charge coverage. Use `-AutoFishingToggleKey <key>` with `-AutoPressAutoFishingHotkey` when validating a rebound toggle such as `F7`.
- AutoFishing performance probing is disabled unless `-AutoFishingPerformance` is supplied with `-AutoExerciseAutoFishingPhase -SaveSlot 5` and target `0`, `100`, or `500`. Positive targets force the product config to InstantBite + SkipMiniGame + Fast x4 + charge 0 and use the typed synthetic Gameplay keybind frame; default warm-up is five PullExit results. Target 0 uses a 60-second warm-up and 600-second strict no-catch window and therefore requires placing the fixture in a genuinely non-fishable environment before enabling the run. The probe writes `AUTO-FISHING-PERF/<timestamp>/auto-fishing-performance.json`, returns through `DolocAPI.ReturnHome`, and records title cleanup. If exact per-thread allocation counters are unavailable, the result is blocked; it does not fall back to `GetTotalMemory`.
- `run-startup-samples.ps1`: run repeated title-startup smokes, keep each primary `GAME-SMOKE` evidence folder, and write a `STARTUP-SAMPLES` aggregate with `startup-samples.*`, `startup-sample-summary.*`, and analyzer output for normal/slow/blocked classification. Use `-NoTitleSettingsMenu -AutoExitAfterSeconds 20` for faster pure-startup sampling without UI screenshot evidence, and `-StopOnSlowSample` to stop as soon as a launch/runtime threshold is reached. Failed child smokes are still captured and linked when a `GAME-SMOKE` evidence folder exists.
- `run-startup-monitor.ps1`: run one or more `STARTUP-SAMPLES` batches, stop on slow launch/runtime, pre-runtime delay, Steam blocked launch, needs-review, sample failure, or leftover `DolocTown.exe`, and write a `STARTUP-MONITOR` aggregate that points to the triggering batch evidence. Failed child sample batches are preserved instead of aborting the monitor before it can write the aggregate.
- `run-startup-observer.ps1`: wait for an externally launched Doloc Town process and fresh DTMAPI startup log without launching the game itself, write `STARTUP-OBSERVE` evidence with `startup-timeline.json`, then collect/analyze logs. Use this when the 30-second startup appears only through manual Steam/UI launch paths.
- `run-hook-probe.ps1`: run HookProbe checks against the local third save when possible.
- `collect-logs.ps1`: collect BepInEx, DTMAPI and Unity text evidence plus Steam/appmanifest, process, fatal-window and hook facts. The player-facing `-DesktopTimestampOutput` mode requires the game to be closed, copies the newest ten DTMAPI current/history logs in full with stable-source length/SHA-256 verification, uses a millisecond-plus-GUID directory identity, and publishes only after a sibling staging directory is complete. Native crash dumps are skipped unless `-IncludeCrashDumps` is explicitly supplied. Internal callers can still merge bounded evidence into an existing folder with `-OutputDirectory`; that mode keeps the current tail caps and three-history default. A separately installed Player Doctor can contribute JSON/text/summary, but its absence or failure does not block unrelated evidence. By default the collector records the runtime `DTMAPI/evidence` root without copying it. `-RuntimeEvidenceSinceUtc` copies only `case/timestamp` directories created or updated in the current run, after a fail-closed 512 MiB total / 128 MiB file / 2000 file / 128 directory preflight; `-IncludeRuntimeEvidence` remains an explicit exceptional full-tree mode.
- `build-player-doctor.ps1`, `check-player-doctor-release.ps1`, and `test-player-doctor-portable.ps1`: optional support-tool workflow for the separate self-contained read-only Doctor. It is not invoked by the normal Runtime build/test/package contract and is never copied into the Workshop Runtime package.
- `build-evidence-retention-allowlist.ps1`: generate the tracked JSON allowlist of GAME-SMOKE runs/artifacts, canonical runtime-evidence identities, and process dumps referenced by Git-tracked or unignored Markdown. `-Check` fails when documentation references changed without regenerating the allowlist.
- `test-runtime-evidence-retention.ps1`: verify that current-run evidence is selected by time window and that over-limit payloads are rejected without partial copies.
- `capture-startup-trace.ps1`: source for the standalone player startup-capture probe built by `build-startup-capture-probe.ps1`; it is not included in the normal Runtime Workshop package. It requires explicit Sysinternals EULA consent, downloads only from the official Microsoft endpoint, verifies the Microsoft Authenticode signer and Procmon identity, filters to `DolocTown.exe`, launches and later closes the captured game process, records Doorstop/Preloader access plus module/log freshness, embeds the bounded collector, and creates one Desktop support ZIP. Probe v2 also adds a bounded parent-process/command-line chain, known Loader environment variables, IFEO/AppCompat/process-mitigation context, antivirus/selected Defender state, relevant Defender/Code Integrity/AppLocker events, critical loader ACL/Zone summaries, and aggregated Procmon loader results; it intentionally excludes full environment blocks, Defender exclusions, complete event logs, and Zone source/referrer URLs.
- `remove-startup-capture-probe-data.ps1`: opt-in cleanup for the standalone startup-capture probe. After exact `DELETE` confirmation it removes only the Desktop capture output/error file and `%LOCALAPPDATA%\DTMAPI\tools\ProcessMonitor`; it leaves the game, Runtime, BepInEx, mods, configs, saves, and the extracted probe folder untouched.
- `build-startup-capture-probe.ps1`: build `dist/DTMAPI-player-probe-20260711-startup-capture.zip` as a standalone probe containing root entries `5` (capture) and `6` (cleanup), without a Process Monitor binary or Runtime payload.
- `analyze-startup-evidence.ps1`: parse one or more evidence folders and classify normal DTMAPI startup segments, true 30s DTMAPI slow-start samples, pre-runtime launch delays, or Steam/pre-process launch blockers. Use `-Quiet` when another script embeds it.
- `compare-startup-evidence.ps1`: run startup analysis for normal plus candidate abnormal evidence, then write `startup-comparison.*` with normal ranges, runtime-slow ranges when present, and a status that says whether the requested normal-vs-abnormal comparison is actually ready.
- `status.ps1`: read existing toolchain/game/runtime/debug status without installing tools. Missing tools or an invalid configured game path are reported while the remaining status is still shown.
- `runtime-lock-status.ps1`, `wait-runtime-lock.ps1`, `acquire-runtime-lock.ps1`, `release-runtime-lock.ps1`, and `invoke-runtime-locked.ps1`: coordinate multiple Codex worktrees that share one local Doloc Town runtime. Acquire or wait for the lock before installing DTMAPI, launching the game, running smoke, or writing live local `MODS`/Workshop upload folders; release it when the shared-runtime operation ends. `invoke-runtime-locked.ps1` wraps a command and releases the lock in `finally`.
- `package-report.ps1`: zip the latest evidence bundle.
- `build-native-function-map-data.ps1`: generate the docs-only native function-map workbench data from local reverse metadata, map indexes, and DTMAPI-authored native-owner reports. It writes symbol/call graph JSON under `tools/native-function-map/workbench/data` and does not copy decompiled method bodies.
- `check-doc-governance.ps1`: validate normalized metadata for new Update records and monthly rows, exact row-to-record metadata agreement, unique monthly-index ownership, root/year routing, duplicate IDs, retired Goal history, compact router/month sizes, current-truth volatility, active-rule regressions, and governance-entry Markdown links. It is source/docs-only and never touches the shared game runtime.

Scripts must not hard-code the user's Steam path. Use local settings or environment variables.

Set `DTMAPI_RUNTIME_DIR` or `DTMAPI_STATE_DIR` to redirect DTMAPI runtime state (`logs`,
`reports`, `evidence`, and `config`) away from the game directory.

`SecondMotorMod` was archived on 2026-06-15, and the remaining MotorVehicle API/GameBridge
code was retired in cleanup Round 2. `run-game-smoke.ps1 -AutoExerciseVehicle` now returns
a blocked archived result, and `-DisableSecondMotorForSmoke` remains only as a no-op
compatibility flag for old commands.
