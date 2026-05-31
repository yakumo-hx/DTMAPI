# 20260531-0001: Player-visible hotload and F8 cleanup

## Source Request

- Goal: review and fix DTMAPI 0.1.12 player-visible issues without redoing completed official local packaging, base localization, or fish roe display.
- Focus for this update: official enablement reload experience, formal-install sample mod cleanup, and F8 diagnostics feedback.

## Changes

- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
  - `NotifyWorkshopModListChanged` now discovers and incrementally loads newly enabled, not-yet-loaded mods.
  - Already-loaded `UniqueID`s are skipped on later reloads, so repeated official reloads do not duplicate `Entry` or event handlers.
  - If an already loaded source-managed mod becomes officially disabled, DTMAPI does not attempt DLL unload; its config page is locked with a restart-required reason.
- `src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs`
  - F8 diagnostics hotkey is disabled by default.
  - Developers can opt in with `DTMAPI_ENABLE_DIAGNOSTICS_HOTKEY=1` or `DTMAPI_DIAGNOSTICS_HOTKEY=<key>`.
- `src/DTMAPI.BepInExBootstrap/DtmUiText.cs`
  - Added Chinese-first text for loaded-but-disabled restart state and default-disabled diagnostics hotkey.
- `src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs`
  - Mod list shows `已加载，停用需重启` when official disable happens after a code mod has already loaded.
  - Status page explains that F8 diagnostics overlay is disabled by default and players should use the title DTMAPI Settings entry.
- `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs`
  - Removed the default F8 conflict reservation because the diagnostics overlay is no longer a player-facing default binding.
- `tools/scripts/install-to-game.ps1`
  - Formal installs now move stale `DTMAPI.HelloDtmMod` and `DTMAPI.ConfigMenuExample` out of game `Mods` unless `-IncludeTestMods` is passed.
- `tests/DTMAPI.UnitTests/Program.cs`
  - Added isolated persistent-root setup so unit tests do not read the user's real LocalLow official mods.
  - Added official-reload hotload coverage: disabled-at-start code mod becomes enabled, hot-loads once, repeated reload does not duplicate, then disable-after-load locks config and requires restart.

## Validation

- `powershell -NoProfile -ExecutionPolicy Bypass -File tools/scripts/build.ps1`
  - Passed 2026-05-31.
  - `DTMAPI.UnitTests: OK`.
- Formal install check:
  - `tools/scripts/install-to-game.ps1 -SkipBuild` moved stale sample mods to `D:\steam\steamapps\common\Doloc Town\DTMAPI\backups\disabled-testmods-20260531-011134`.
  - Game `Mods` directory had no `DTMAPI.HelloDtmMod`, `DTMAPI.ConfigMenuExample`, or HookProbe after formal install.
- Player-state smoke:
  - `docs/debug/evidence/GAME-SMOKE/20260531-011150/result.json`
  - Collected logs: `docs/debug/evidence/GAME-SMOKE/20260531-011353/DTMAPI-latest.log`
  - Verified Steam launch, no fatal instance window, clean process exit, five official enabled DTMAPI packages loaded, and log line `DTMAPI diagnostics hotkey is disabled by default`.
- Official hotload smoke:
  - Temporary state/evidence: `docs/debug/evidence/OFFICIAL-HOTLOAD/20260531-011650`
  - Game smoke result: `docs/debug/evidence/GAME-SMOKE/20260531-011527/result.json`
  - Collected logs: `docs/debug/evidence/GAME-SMOKE/20260531-011745/DTMAPI-latest.log`
  - Evidence lines:
    - Startup skipped disabled ActionSpeed: `Skipping Yuuka.DTMAPI.ActionSpeed`.
    - Background enable job completed: `enable-job.txt` contains `enabled-after-skip`.
    - Official reload hot-loaded the mod: `Workshop ModListChanged hook dispatched. discoveredMods=5 hotLoaded=1`.
    - Repeated reload did not duplicate it: later `hotLoaded=0`.
    - Exit check: `process-check.txt` says no `DolocTown.exe` process found.
  - `mod_infos.json` was restored from `mod_infos.before.json`; `Compare-Object` found no difference.

## Related Records

- Smoke matrix:
  - `WORKSHOP-002`
  - `INSTALL-001`
  - `UI-005`
- Hook map:
  - `Workshop.ReloadMods`
- Debug issue:
  - `ISSUE-003-hotkey-openconfig-no-overlay`

## Not Fully Verified

- This update does not complete the remaining gameplay vertical slices:
  - Animal bell hidden produce must still get a real UI screenshot/evidence.
  - ActionSpeed real animation impact remains experimental unless separately verified.
  - AutoFishing real fishing phase automation remains pending.
  - OneAction real resource/tool hit remains pending.

## Rollback

- Revert the listed source changes to return official reload to discover-only behavior.
- Remove `INSTALL-001`, `WORKSHOP-002`, and `UI-005` smoke rows if rolling back.
- Restore sample mods from the timestamped backup only for dev testing; do not put them back into formal player installs.
