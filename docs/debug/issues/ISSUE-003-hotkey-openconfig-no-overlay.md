# ISSUE-003: Hotkey opens DTMAPI UI state but overlay is not visible

## Status

- Status: deferred
- Last updated: 2026-05-30
- Severity: high
- Area: input / UI overlay / config menu
- Current owner: none for the current title-entry goal; `ReflectedImGuiOverlay` remains a diagnostic fallback only.

## Symptom

In a loaded save, pressing F10 logs `ActionSpeed hotkey OpenConfig OK key=F10`, but no DTMAPI config or diagnostics overlay appears on screen.

## 2026-05-30 Goal Decision

This issue is no longer blocking the player-facing config route for DTMAPI 0.1.11. The current goal intentionally skips F8/F10 temporary overlay repair and uses a formal title-homepage DTMAPI Settings button instead.

Verified replacement evidence:

- `GAME-SMOKE/20260530-202152`: title smoke passed with `TitleSettingsButton=true`, `TitleSettingsMenu=true`, `NoFatalInstanceWindow=true`, and `ProcessExited=true`.
- `GAME-SMOKE/20260530-202310`: log lines include `DTMAPI title settings UI created an EventSystem for button input.`, `DTMAPI title settings button visible on HomePageUiState.`, `DTMAPI title settings button clicked.`, and `DTMAPI title settings menu opened.`
- `HOOK-PROBE/20260530-202404`: third-save smoke passed after title UI fixes; `OneSecondUpdateTicked`, SaveLoaded, UI pages, log export, and migrated config save/cancel/reset evidence all passed.

## 2026-05-31 Player-visible Mitigation

The F8 diagnostics overlay is no longer a default player-facing binding. `BootstrapPlugin` now leaves diagnostics hotkey disabled unless a developer explicitly opts in with `DTMAPI_ENABLE_DIAGNOSTICS_HOTKEY=1` or `DTMAPI_DIAGNOSTICS_HOTKEY=<key>`.

Evidence:

- Build/unit passed 2026-05-31.
- `GAME-SMOKE/20260531-011353`: formal Steam smoke log includes `DTMAPI diagnostics hotkey is disabled by default; use the title-page DTMAPI Settings entry.`
- `UI-005` added to `docs/debug/regressions/smoke-matrix.md`.

This does not solve the historical IMGUI rendering bug. It prevents the unresolved overlay from presenting as a silent player control.

## Known Facts

- HookProbe was removed from the local game `Mods` folder before this test.
- The current game launch discovered 7 DTMAPI-capable mod folders, not 8, confirming HookProbe was not loaded.
- The first input implementation only polled legacy `UnityEngine.Input.GetKeyDown(KeyCode)` and did not observe user F-key presses.
- Doloc Town uses Unity's new Input System (`Unity.InputSystem.dll`, `DolocInputSource`, `InputAction`), so DTMAPI added a reflected `Keyboard.current.*Key.wasPressedThisFrame` fallback.
- After the Input System fallback, DTMAPI observed F10 but initially blocked it as `context=HomePageUiState`.
- `HomePageUiState` remains cached in `DolocAPI.gameUiStates` after save load, so checking `HasState<HomePageUiState>()` was not a valid active-context test.
- DTMAPI now prioritizes `DolocAPI.IsNormalState` before cached UI states; after that, F10 reached the mod event path.
- User confirmed that even after the F10 event reached `helper.UI.OpenConfigPage`, no visible DTMAPI UI appeared.

## Evidence

- Runtime launch: `2026-05-30 18:40:19.281 +08:00 [Info] [DTMAPI] DTMAPI runtime starting.`
- Mod scan: `2026-05-30 18:40:19.333 +08:00 [Info] [DTMAPI] Discovered 7 DTMAPI-capable mod folder(s).`
- Save loaded: `2026-05-30 18:40:56.261 +08:00 [Info] [DTMAPI] SaveLoaded hook dispatched. slot/index=4 isNewGame=False`
- Hotkey success: `2026-05-30 18:41:19.790 +08:00 [Info] [Yuuka.ActionSpeed] ActionSpeed hotkey OpenConfig OK key=F10`
- Manual observation: user reported the overlay did not appear after F10.

Evidence folder: `docs/debug/evidence/ISSUE-003/20260530-hotkey-openconfig-no-overlay`.

## Changes Made During Investigation

- `src/DTMAPI.BepInExBootstrap/ReflectedUnityInput.cs`
  - Added reflected Unity Input System fallback for keyboard and mouse controls.
  - Legacy `UnityEngine.Input` polling remains as a fallback.
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
  - Added `DolocAPI.IsNormalState` priority check for gameplay hotkey context.
- `src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs`
  - Added low-frequency F8 diagnostics logging.
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
  - Added `LogOnce` evidence when registered hotkeys are blocked by UI/context.
- `tools/scripts/install-to-game.ps1`
  - HookProbe is no longer installed by generic `-IncludeTestMods`; it requires explicit `-IncludeHookProbe`.
- `tools/scripts/run-game-smoke.ps1`
  - Passes explicit `-IncludeHookProbe` for HookProbe smoke tests.

## Rejected Hypotheses

- Not caused by missing mod load: all five Yuuka migrated mods reached `Entry`, `UpdateTicked`, and `SaveLoaded`.
- Not caused by HookProbe after 18:40 restart: local game discovered 7 DTMAPI mod folders and `DTMAPI.HookProbeMod` was absent.
- Not caused by Unity input being fully unreadable after the Input System fallback: F10 reached `ActionSpeed hotkey OpenConfig OK`.
- Not solely caused by gameplay-context blocking after the `DolocAPI.IsNormalState` fix: F10 reached the mod handler.

## Deferred Debug Target

Only resume this if F8/F10 diagnostic overlay support is explicitly needed again. Focus on the overlay rendering path, not input:

1. Add evidence logs in `UiRuntimeService.Open` and `Close` to confirm `IsOpen`, `CurrentPage`, and requested config ID after F10.
2. Add once-per-open logs in `BootstrapPlugin.OnGUI` to confirm Unity calls `OnGUI` while `runtime.UI.IsOpen` is true.
3. Add `ReflectedImGuiOverlay.Render` diagnostics for `CanDrawOverlay`, GUI type resolution, and first successful `GUI.Box`/`GUI.Label`.
4. If `OnGUI` is not called, investigate whether BepInEx plugin `OnGUI` works in this Unity/player path or whether overlay must render through a runtime-created `MonoBehaviour`/Canvas instead.
5. If `OnGUI` is called but nothing is visible, inspect GUI skin/color/depth/scaling and whether another camera/UI layer covers IMGUI.

## Historical Acceptance Criteria

Do not mark this overlay-specific issue solved until all are true:

- Pressing F10 in a loaded save visibly opens the ActionSpeed config page.
- Pressing F8 visibly toggles the DTMAPI diagnostics overlay.
- When the overlay is open, ordinary mod gameplay hotkeys are blocked and logged only at low frequency.
- When the overlay is closed, F6 toggles AutoFishing and logs its state change.
- Evidence is added to `smoke-matrix.md`.
