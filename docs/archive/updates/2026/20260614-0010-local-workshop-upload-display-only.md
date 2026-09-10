# 20260614-0010 Local Workshop Upload Display-Only Fix

## Status

Verified-display / live-upload-pending. Build/test and official Mod UI display validation passed; live Workshop update click requires manual owner-account validation.

## Source Request

After the `20260614-0009` fix, the official local mod page button changed from `上传模组` to `更新模组`, but updating DTMAPI-owned local packages failed. User relayed a workaround where removing the id from `workshop.json`, saving, writing it back, saving, and restarting made the game behave normally.

## Root Cause

The `20260614-0009` prefix fixed the UI by skipping `ModManager.ResolveLocalModUploadPlan` and directly supplying `WorkshopUploadPlan(Update, workshopId)` for DTMAPI-generated local packages.

That was too invasive. Native `SteamWorkshopUploader.ResolveUploadPlan` is not only a text decision; it performs a Steam item-details query before upload. Recent failure evidence:

- `Player.log` showed `Update local mod DTMAPI` / `Update local mod DTMAPI 更多装备栏位`, then `[MOD] Workshop upload failed: k_EResultTimeout`.
- Steam `workshop_log.txt` showed uploads started, content and preview were sent, then Steam finished with `Timeout` and reverted the content.
- Successful earlier uploads had a preceding Steam `GetDetails request`; the prefix path produced no new details query before `SubmitItemUpdate`.

The user workaround likely works because editing `workshop.json` and restarting pushes the official path through its native details-resolution flow again.

## Changes

- Removed the `ModManager.ResolveLocalModUploadPlan` prefix from the installed hook path.
- Added a display-only Harmony postfix on `DolocTown.UI.ModData` constructor.
- For DTMAPI-generated official-local packages with nonzero `workshopId`, the postfix sets only `ModData.canUpdateWorkshopItem = true`.
- Native `ModManager.ResolveLocalModUploadPlan` and `SteamWorkshopUploader.UploadMod` now remain the sole owner of upload execution and Steam item-detail resolution.
- Non-DTMAPI local packages are not changed.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260614-0009-local-workshop-upload-plan.md`
- `docs/hook-map/README.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`

## Validation

- `tools/scripts/build.ps1 -Configuration Release`: passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- `tools/scripts/test.ps1 -Configuration Release`: passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- `git diff --check`: passed with line-ending warnings only.
- `tools/scripts/build-release-workshop-packages.ps1 -Configuration Release -SkipBuild`: refreshed `dist/workshop-packages`.
- `tools/scripts/install-to-game.ps1 -Configuration Release -SkipBuild`: refreshed the installed BepInEx runtime and official-local functional packages.
- Manually mirrored `dist/workshop-packages/DTMAPI` to official local `MODS/DTMAPI` while preserving `workshop.json`.
- Hashes match for `DTMAPI.GameBridge.DolocTown.dll` across source Release output, installed game plugin, dist runtime package, and official local runtime upload package: `57115B88FD1A816C1D651033694DED81B1B1899609A16A61E34E1444F31E446F`.
- Steam title official Mod UI smoke `GAME-SMOKE/20260614-232339`:
  - `OfficialModUi=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, `ForcedClose=Passed`.
  - `Workshop.LocalUploadPlan = verified` logs say DTMAPI-generated local packages display Update while native Steam `ResolveLocalModUploadPlan` still owns upload execution.
  - The run aggregate is retained as failed only because `OfficialModUiScreenshotFile` reported false; the referenced screenshot exists at `D:\Steam\steamapps\common\Doloc Town\DTMAPI\evidence\OFFICIAL-001\20260614-232424\official-mod-ui.png`.
- Game validation target: launch through Steam, open official Mod UI, select a DTMAPI-generated local package with `workshop.json`, verify the button displays `更新模组`, click update manually, and confirm `workshop_log.txt` shows a fresh `GetDetails request` before upload and finishes `OK` instead of `Timeout`.

## Evidence

- Failure evidence before fix:
  - `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\Player.log`.
  - `D:\Steam\logs\workshop_log.txt`.
  - Affected workshop ids observed: `3743016467` (`DTMAPI`) and `3744059735` (`DTMAPI 更多装备栏位`).

## Rollback

Reverting this change restores the `ResolveLocalModUploadPlan` prefix and the visual Update button, but reopens the upload-timeout risk because native Steam item-details resolution is skipped.

## Follow-Up

After installing this build, manually test one live update from the owner account. If it still fails, keep the display-only patch and collect the exact `EResult` plus the preceding Steam `GetDetails` and `Upload starting/finished` lines.
