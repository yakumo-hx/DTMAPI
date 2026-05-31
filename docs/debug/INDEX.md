# DTMAPI Debug Index

DTMAPI debug records must be durable and evidence-based. Do not rely on chat memory for repeated bugs.

## Required Debug Assets

- `issues/`: one recurring symptom per issue file.
- `protocols/`: repeatable evidence collection procedures.
- `regressions/smoke-matrix.md`: build/game/hook/exit regression matrix.
- `evidence/`: raw logs and collected reports, grouped by issue and date.
- `lessons.md`: failed directions and engineering lessons that future Codex sessions must not forget.

## First Known High-Risk Issues

- Direct `DolocTown.exe` smoke launch can exit before title and leave `Fatal error: Another instance is already running`; use Steam launch for smoke unless explicitly testing direct EXE behavior.
- Steam can get stuck before process creation after a previous launch action, with `steam://rungameid/2285550` logged but no new `DolocTown.exe` or DTMAPI startup log; track this as a Steam launch-blocking issue, not as DTMAPI startup slowness.
- Startup evidence should compare `startup-timeline.json` launch wall-clock fields against DTMAPI startup segment logs; a slow `LaunchToStartupPatternMs` with normal `Bootstrap.Awake totalMs` is pre-runtime launch delay, not DTMAPI runtime startup cost. Use `tools/scripts/run-startup-samples.ps1` for repeated normal/slow/blocked launch sampling; use `-NoTitleSettingsMenu -AutoExitAfterSeconds 20` for faster pure-startup sampling, `-StopOnSlowSample` for capture-and-stop runs, and read `startup-sample-summary.md` for threshold counts and min/max/average ranges. Use `tools/scripts/run-startup-monitor.ps1` when the goal is a longer unattended capture loop that stops on slow, blocked, needs-review, failed, or leftover-process evidence and preserves the triggering `STARTUP-SAMPLES` batch. Use `tools/scripts/run-startup-observer.ps1` when the launch is triggered outside the smoke harness, such as manual Steam UI launches. Use `tools/scripts/compare-startup-evidence.ps1` before claiming the normal-vs-abnormal requirement is satisfied; it must report `RuntimeSlowComparisonReady` for a true DTMAPI runtime slow comparison. Failed child smokes or batches should still leave primary evidence and aggregate summaries; treat `sample-failure-count-*` separately from true DTMAPI runtime slowness.
- Steam shows Doloc Town as still exiting after the game window closes.
- Stutter/performance regressions after save load or inventory/menu interactions.
- Config menu input boundaries and title-screen-only UI entry.
- HookProbe is test-only; leaving it installed during normal manual play can open DTMAPI UI after save load and block gameplay hotkeys.
- F-key input can reach migrated mods through Unity Input System, but the DTMAPI overlay still has an open rendering bug tracked in `issues/ISSUE-003-hotkey-openconfig-no-overlay.md`.
- Decal equipment such as `resin_collector` must be created or selected through the game's decal host/slot path; ordinary `CreateEquipment(..., null, -1)` is expected to fail with `建造贴纸设备需要传入decalHost和decalSlot参数`.
- ActionSpeed harvest classification cannot rely only on static/current equipment scanner state. Wild vegetation harvest is reached through `InteractableManagerEx` and a `VegetationRenderer` wrapper, and stale scanner selections must be cleared before collecting water/vegetation evidence.
- OneAction vegetation/dandelion must not be treated as a `DungeonResourceRenderer` resource. It uses `VegetationRenderer.OnFell -> Vegetation.CheckToolConstraints`, so DTMAPI records it as a native exception path and verifies wrong/correct tools without applying one-action damage.
- Hook regressions after Doloc Town build updates.

## Rule

Before changing runtime lifecycle, shutdown, BepInEx, Harmony patches, event dispatch, input, config menu, Workshop loading, or mod loading, check this folder and update it after the attempt.
