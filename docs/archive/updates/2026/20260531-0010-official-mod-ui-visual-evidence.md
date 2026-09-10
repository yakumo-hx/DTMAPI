# 20260531-0010 Official Mod UI Visual Evidence

## Source Request / Goal

- Active goal: review and fix DTMAPI 0.1.12 player-visible issues.
- Acceptance target: the official Doloc Town Mod UI must visibly recognize the five official-local DTMAPI packages, while DTMAPI remains status/config only and does not take over official enable/disable duties.
- Follow-up from `OFFICIAL-001`: package structure was already verified, but the visual official Mod UI list still needed evidence.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs`
- `src/DTMAPI.BepInExBootstrap/DtmUiText.cs`
- `tools/scripts/run-game-smoke.ps1`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`

## Known Facts And Rejected Hypotheses

- Known: official-local package structure and official enablement gating were already verified; this update should not move ordinary DTMAPI mods back under `BepInEx/plugins`.
- Known: official IDs in the rendered Mod UI use forms such as `Local.Yuuka_DTMAPI_ActionSpeed`, so a raw `Yuuka_DTMAPI_*` ID-only filter was too narrow.
- Known: `HomePageUiState` can remain active under official title-page panels; relying on that state alone allowed the DTMAPI title button to float over the official Mod UI.
- Rejected: treating DTMAPI's own scanner or files on disk as proof that the official UI displays the packages correctly.
- Rejected: hiding the issue by moving the DTMAPI button position; DTMAPI should hide when Doloc Town owns the visible title-page UI.

## Implementation

- Added `AutoOpenOfficialModUi` smoke automation that opens official `ModUiState`, selects the all-mods tab, identifies DTMAPI official-local packages through official UI data, selects a package, checks icon/preview assets, and captures `official-mod-ui.png`.
- Expanded official UI package detection to accept official `Local.Yuuka_DTMAPI_*` IDs and package metadata/root-path signals.
- Changed Doloc Town UI context detection to prefer blocking title-page panels (`ModUiState`, `GameDataUiState`, settings/confirm/menu states) before the underlying `HomePageUiState`.
- Updated DTMAPI title-entry status text to describe the unobstructed title homepage instead of any retained `HomePageUiState`.

## Validation

- `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\build.ps1`
  - Passed with `DTMAPI.UnitTests: OK`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\run-game-smoke.ps1 -SaveSlot 0 -TimeoutSeconds 180 -AutoOpenOfficialModUi -SkipBuild`
  - Passed.
  - Result: `docs/debug/evidence/GAME-SMOKE/20260531-051234/result.json`
  - Collected logs/screenshots: `docs/debug/evidence/GAME-SMOKE/20260531-051312`

## Evidence

- Official UI log: `Official Mod UI evidence OK dtmapiMods=5, visibleRows=5, selected=Local.Yuuka_DTMAPI_ActionSpeed, title=动作加速（DTMAPI）`.
- Screenshot: `docs/debug/evidence/GAME-SMOKE/20260531-051312/DTMAPI-evidence/OFFICIAL-001/20260531-051312/official-mod-ui.png`.
- Screenshot summary: `iconLoaded=True, iconFile=True, previewFile=True, ScreenshotRequested=True`.
- Visual inspection: the screenshot shows five DTMAPI packages with icons, Chinese names/descriptions, enabled toggles, and the detail pane; the DTMAPI title button is no longer drawn over the official Mod UI.
- Exit check: `docs/debug/evidence/GAME-SMOKE/20260531-051234/process-check.txt` says no `DolocTown.exe`.
- Fatal popup check: `docs/debug/evidence/GAME-SMOKE/20260531-051234/fatal-window-check.txt` says no fatal instance popup.
- Earlier failed attempt retained: `docs/debug/evidence/GAME-SMOKE/20260531-050211` / `docs/debug/evidence/GAME-SMOKE/20260531-050557` failed because the first official UI filter did not recognize `Local.Yuuka_DTMAPI_*` IDs.

## Related Records

- `docs/debug/regressions/smoke-matrix.md`: `OFFICIAL-001`, `UI-003`
- `docs/hook-map/README.md`: `UI.TitleSettingsEntry`
- `docs/updates/2026/20260530-0003-official-local-mods-localization.md`
- `docs/updates/2026/20260531-0007-title-ui-screenshot-evidence.md`

## Rollback Notes

- Remove `AutoOpenOfficialModUi` from the smoke settings and `run-game-smoke.ps1` if official UI automation becomes unstable.
- Restore the previous UI context order only if a future Doloc Town build stops keeping `HomePageUiState` beneath official title-page panels; if reverted, keep `OFFICIAL-001` pending until a no-overlap screenshot is captured again.

## Follow-Up

- This verifies official visual listing and DTMAPI title-button hiding only.
- DTMAPI still does not hot-unload already loaded DLLs when official UI disables a mod; loaded-disabled state remains restart-required by design.
- Remaining migrated feature work stays tracked separately: non-tool ActionSpeed paths, full AutoFishing automation, and OneAction fuel/feeder paths remain pending.
