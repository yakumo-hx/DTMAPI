# Player Y Console And Collect-Log Review

Date: 2026-06-17

Source request: player supplied `D:\下载\DTMAPI-logs` for a Y-key console that appears unresponsive, plus `D:\下载\dtmapi-report-20260616-213923.zip` and screenshots showing `3_check_dtmapi_status.bat` OK while `4_collect_dtmapi_logs.bat` failed.

## 1. Y console appears to do nothing

Player-visible report:

- Only Y-key console and MoreEquipmentSlots were expected to be active.
- Gamepad play was active, but the physical keyboard Y key also failed to open the console.
- The official mod UI showed the Y-key console enabled, and DTMAPI Settings listed `DTMAPI.DebugConsoleMod`.

Analysis:

- `D:\下载\DTMAPI-logs\20260616-212130\DTMAPI-latest.log` proves the mod loaded and the host was bound: `Debug console host bound owner=DTMAPI.DebugConsoleMod...` and `Y键控制台已加载；进入存档后按 Y 打开。`
- The input layer received Y repeatedly. The latest log contains 25 `Input Y pressed dispatched to DTMAPI mods` entries and 25 matching releases.
- Each Y press opened the console and then the reflected UI host immediately closed it with the same Y press. The key sequence at lines around 287 is `Input Y pressed` -> `Debug console opened ... reason=hotkey Y` -> `Debug console closed reason=Y` -> `Input Y released`.
- The player therefore saw "no response", but the real failure was an opener-key edge being read twice: once by `DebugConsoleMod.OnButtonPressed` to open, then by `ReflectedDebugConsoleUi.Update()` through `ReflectedUnityInput.GetKeyDown("Y")` to close before the Canvas became visible.
- The `None`/`无` hotkey visible in the settings screenshot is the DTMAPI diagnostics hotkey state, not the Y console hotkey. `DebugConsoleMod` currently hard-registers `Y` and `Escape`.
- The log package also contains unrelated third-party BepInEx plugin noise (`EasyFishing_DolocTown`, `com.user.dolocnoweeds`, Configuration Manager). It does not explain this Y failure because DTMAPI received and dispatched the key normally.

Conclusion:

- This is a DTMAPI reflected Y-console lifecycle bug, not a missing install or disabled-mod issue.
- The fix should suppress closing from the same physical Y press that opened the console until that key is released.
- Smoke evidence should require the Canvas-visible line for at least the first real external Y open path, because an `opened` log alone can be followed by same-frame close.

## 2. First launch black screen / crash, while in-game report export works

Player-visible report:

- The game sometimes black-screens or hangs on the first launch after startup, then enters normally on a later launch.
- `3_check_dtmapi_status.bat` reports required files OK.
- `4_collect_dtmapi_logs.bat` failed with `PropertyNotFoundStrict` on `$recentRuntimeEvidence.Count`, but the in-game report export succeeded.

Analysis:

- `dtmapi-report-20260616-213923.zip` contains `DTMAPI-latest.log`, `BepInEx-LogOutput.log`, `Unity-Player.log`, `dtmapi-summary.txt`, `install-state.json`, and `release-manifest.json`.
- The exported report runtime is `0.5.1-alpha` / binary `0.5.1.0`, not the current local `0.5.2-alpha` runtime.
- The 9 MB `DTMAPI-latest.log` still contains multiple `DTMAPI runtime starting.` markers in one file, which means this report was produced before the current latest-log rotation was installed or before it took effect in that game folder.
- The summary errors are dominated by old best-effort `DolocGridUI<T>.ResetLayoutSize/SetCapacity` diagnostic patch failures plus version gating for `Yuuka.DTMAPI.AnimalHusbandryProgress` requiring `0.5.2-alpha` while the installed runtime was `0.5.1-alpha`. These are not proven crash causes.
- The available logs show DTMAPI reaching `GameLaunched`, hook installation, return-to-title, and title settings UI/report export. They do not include a fatal window, process snapshot, Steam log tail, or a terminal crash stack for the first failed launch.

Conclusion:

- The provided report is useful but insufficient to identify the first-launch black-screen root cause.
- The offline collector failure is itself a blocker for future crash diagnosis because it prevented `process-check.txt`, `fatal-window-check.txt`, Steam tails, and startup analysis from being captured.

## 3. `4_collect_dtmapi_logs.bat` fails on `.Count`

Observed script error:

- `collect-logs.ps1:147` failed at `if ($recentRuntimeEvidence.Count -eq 0)` with `PropertyNotFoundStrict`.

Analysis:

- `tools/scripts/common.ps1` enables strict mode.
- In PowerShell, a pipeline result with exactly one item can become a scalar `DirectoryInfo` rather than an array.
- `DirectoryInfo` has no `Count` property under strict mode.
- The existing history-log code already uses `@(...)`; the runtime-evidence summary code did not.

Conclusion:

- Wrap the `Get-ChildItem ... | Select-Object -First 20` runtime evidence query in `@(...)`.
- Verify 0/1/many runtime-evidence child folder cases through the packaged collector path before asking players to use `4_collect_dtmapi_logs.bat` again.
