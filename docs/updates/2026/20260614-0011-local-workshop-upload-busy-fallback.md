# 20260614-0011 Local Workshop Upload Busy Fallback

## Status

Verified local UI / live upload pending.

## Source Request

User reported that DTMAPI and MoreEquipmentSlots still failed to update, and other local DTMAPI mods such as MoreSaves had an Update button that did nothing after failed update attempts.

## Root Cause Notes

- Native `ModUiState.UploadMod` always calls `ModManager.ResolveLocalModUploadPlan` again before upload.
- Native `ModManager` serializes local upload-plan resolution with `resolvingLocalModUploadPlanKey`.
- Native `SteamWorkshopUploader.ResolveUploadPlan` returns early without invoking the supplied callback when the uploader is already busy.
- If a resolve request reaches that busy path, `ModManager` can retain a stale resolving key, leaving later local upload-plan requests stuck. This explains the "click Update and nothing happens" behavior after failed or overlapping update attempts.
- The Steam `k_EResultTimeout` result remains a separate live-upload issue. Logs show current code reaches Steam item details and content/preview upload before timeout.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `docs/updates/INDEX.md`
- `docs/hook-map/README.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`

## Implementation

- Added a narrow Harmony prefix on `DolocTown.Config.SteamWorkshopUploader.ResolveUploadPlan`.
- The prefix only handles DTMAPI-generated official-local packages with a nonzero `workshop.json` id and DTMAPI package markers.
- When the native uploader is busy, DTMAPI invokes the pending callback with an Update plan for the current package id and skips the native busy return, preventing the native `ModManager` queue from stalling.
- Normal non-busy resolution remains native-owned and still performs the Steam details query.
- Actual upload execution remains native `SteamWorkshopUploader.UploadMod`.

## Validation

- `tools/scripts/build.ps1 -Configuration Release`: passed, 0 warnings/errors, `DTMAPI.UnitTests: OK`.
- `tools/scripts/test.ps1 -Configuration Release`: passed, 0 warnings/errors, `DTMAPI.UnitTests: OK`.
- `git diff --check`: passed with line-ending warnings only.
- Rebuilt release Workshop packages and installed local packages.
- DTMAPI GameBridge SHA256 matched source Release, installed BepInEx runtime, dist runtime package, and official local DTMAPI upload package: `E8015A76C6A71665A5E5A1F2A15EB14EAE3EAEBBE8E8523609FE3EB0F533DAA7`.
- MoreEquipmentSlots DLL SHA256 matched source Release and official local upload package: `092807CC5C5DB359B40D5325EDD6EBC6860238E51F7F65014F5B39FBC0CF71ED`.
- Steam official Mod UI smoke `GAME-SMOKE/20260614-235540` loaded the new hook (`Workshop.LocalUploadPlanBusyFallback = experimental`), passed `OfficialModUi`, `ProcessExited`, and `NoFatalInstanceWindow`, and exited with no `DolocTown.exe` left running. The aggregate remains failed only because `OfficialModUiScreenshotFile` reported failed despite `Smoke.OfficialModUiScreenshot = verified` in the log.
- 2026-06-15 manual owner-account retest: MoreSaves, DTMAPI, and MoreEquipmentSlots Update clicks all reached the native upload path instead of becoming inert. `Player.log` recorded `Update workshop mod` followed by `Workshop upload failed: k_EResultTimeout` for `DTMAPI`, `DTMAPI 更多装备栏位`, and `DTMAPI 更多存档`. `D:\Steam\logs\workshop_log.txt` recorded fresh `GetDetails request` entries and content/preview uploads before `Timeout`, including new manifest ids for DTMAPI (`6363828491129135539`), MoreEquipmentSlots (`2782529992353271677`), and MoreSaves (`126075062394699117`). This verifies the no-callback/inert-click layer is closed, while the final Steam `SubmitItemUpdate` timeout remains unresolved.

## Rollback

Remove the `SteamWorkshopUploader.ResolveUploadPlan` prefix and keep the display-only `ModData..ctor` patch from `20260614-0010`. This may reintroduce native resolve-queue stalls after busy upload attempts.

## Follow-Up

- Owner-account live upload still needs manual validation. Expected Steam log: fresh `GetDetails request`, upload start, and final `OK` rather than `Timeout`.
- If `Timeout` remains, inspect Steam submission state separately; this update only prevents no-callback local resolve queue stalls. Next useful diagnostic is around native `SteamWorkshopUploader.UploadUpdate` / `OnSubmitItem`, logging the first upload language, title/description length, tag list, `ioFailure`, `m_eResult`, and legal-agreement flag without replacing native upload execution.
