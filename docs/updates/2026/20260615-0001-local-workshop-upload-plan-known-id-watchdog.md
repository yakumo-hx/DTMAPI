# 20260615-0001 Local Workshop Upload Plan Known-Id Watchdog

## Status

Verified after owner-account reboot/update retest.

## Source Request

After the `20260614-0011` busy fallback, the user retested official local Workshop updates and reported that clicking Update again had no visible response.

## Root Cause Notes

- This is a different layer from the final Steam `k_EResultTimeout`.
- `ModUiState.UploadMod` calls `ModManager.ResolveLocalModUploadPlan` before it can show the confirm question or call `UploadLocalMod`.
- The latest `Player.log` at `2026-06-15 00:25` showed `PendingUiState` entering and returning to `HomePageUiState`, but no `QuestionUiState`, no `Update local mod`, and no `Update workshop mod`.
- Steam `workshop_log.txt` showed fresh `GetDetails request 0xc/0xd`, but no `Upload starting`.
- That means the official button event happened, but native `SteamWorkshopUploader.ResolveUploadPlan` did not deliver the callback needed to release `ModManager.resolvingLocalModUploadPlanKey`.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `docs/updates/INDEX.md`
- `docs/hook-map/README.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`

## Implementation

- Kept the display-only `ModData..ctor` patch from `20260614-0010`.
- Narrowed the `SteamWorkshopUploader.ResolveUploadPlan` prefix so it only invokes the callback immediately when the native uploader is already busy.
- Added a postfix/watchdog for DTMAPI-generated official-local packages with a nonzero `workshop.json` id:
  - native Steam `GetDetails` is allowed to run first;
  - DTMAPI records the exact uploader callback only if it is still the active native `resolveUploadPlanCallback`;
  - after 4 seconds, if the same callback is still unresolved for the same workshop id, DTMAPI clears the native pending callback/id and invokes the callback with a native `WorkshopUploadPlan(Update, workshopId)`;
  - actual upload execution remains native `SteamWorkshopUploader.UploadMod`.
- Non-DTMAPI local mods, zero ids, missing DTMAPI package markers, successful native callbacks, and ordinary cached upload plans keep native behavior.

## Validation

- `tools/scripts/build.ps1 -Configuration Release`: passed with 0 warnings/errors and `DTMAPI.UnitTests: OK`.
- `tools/scripts/test.ps1 -Configuration Release`: passed with 0 warnings/errors and `DTMAPI.UnitTests: OK`.
- `git diff --check`: passed with line-ending warnings only.
- `tools/scripts/build-release-workshop-packages.ps1 -Configuration Release -SkipBuild`: passed.
- `tools/scripts/install-to-game.ps1 -Configuration Release -SkipBuild`: passed.
- DTMAPI runtime hash after manual sync into the official local upload package:
  - `DTMAPI.GameBridge.DolocTown.dll` source/install/dist/local package SHA256 `00AE49CB0F88D13B44207541E81DC6A4E87DDBE4BBB5C3FD64C6400A24C678CA`.
- Steam title official Mod UI smoke `GAME-SMOKE/20260615-010503`:
  - passed `OfficialModUi`, `ProcessExited`, and `NoFatalInstanceWindow`;
  - aggregate `RunStatus` failed only because `OfficialModUiScreenshotFile` reported false after evidence capture;
  - log loaded `Workshop.LocalUploadPlanKnownIdFallback = experimental`;
  - log observed `Workshop.LocalUploadPlanKnownIdFallback = watching` for `Local.DTMAPI` and `workshopId=3743016467`.
- Manual owner-account retest after reboot:
  - user confirmed the official update flow completed after restarting the computer;
  - Steam `workshop_log.txt` records `Upload finished for workshop item 3743016467 : OK` at `2026-06-15 07:02:54`, `07:02:55`, and `07:02:56`;
  - Steam `workshop_log.txt` records `Upload finished for workshop item 3744059735 : OK` at `2026-06-15 07:03:29`, `07:03:30`, and `07:03:31`;
  - resubscribe/download evidence then detected updated cached manifests for both items.

## Evidence

- Pre-fix `Player.log`:
  - `2026-06-15 00:25:22-00:25:55` repeated `Workshop.LocalUploadPlan = verified` display-only lines.
  - Update click entered `PendingUiState`, then returned to `HomePageUiState`.
  - No `QuestionUiState`, `Update local mod`, or `Update workshop mod` lines appeared.
- Pre-fix Steam `workshop_log.txt`:
  - `2026-06-15 00:25:22` `GetDetails request 0xc`.
  - `2026-06-15 00:25:23` `GetDetails request 0xd`.
  - No corresponding `Upload starting` lines.
- Post-fix title smoke:
  - `docs/debug/evidence/GAME-SMOKE/20260615-010503`.
  - `DTMAPI-latest.log` contains `Workshop.LocalUploadPlanKnownIdFallback = experimental`, then `watching` for `Local.DTMAPI`.
  - `result.json` records `OfficialModUi=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`.
- Post-reboot owner-account update:
  - `D:\Steam\logs\workshop_log.txt` at `2026-06-15 07:02:54-07:02:56` records successful DTMAPI update results for item `3743016467`.
  - `D:\Steam\logs\workshop_log.txt` at `2026-06-15 07:03:29-07:03:31` records successful MoreEquipmentSlots update results for item `3744059735`.

## Rollback

Remove the `SteamWorkshopUploader.ResolveUploadPlan` postfix/watchdog and keep the display-only patch plus busy-only prefix. This may reintroduce the latest no-confirm/no-upload inert-click case when Steam details queries are delayed or aborted without a native callback.

## Follow-Up

If the click reaches upload but still ends at `k_EResultTimeout`, continue with the separate final-submit investigation around native `UploadUpdate` / `OnSubmitItem`; this update does not claim to solve Steam final submission timeouts.
