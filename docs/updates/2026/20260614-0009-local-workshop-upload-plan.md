# 20260614-0009 Local Workshop Upload Plan

## Status

Superseded by `20260614-0010`.

## Source Request

User reported that after restarting Doloc Town, the official local mod page for `DTMAPI 更多装备栏位` again showed `上传模组` instead of `更新模组`, even though the local upload folder still contained `workshop.json`.

## Root Cause

The official button text is not determined directly by `workshop.json`. `ModUiState` renders the local upload button from `WorkshopUploadPlan`: `Update` shows update, while a missing or `Create` plan shows upload. Native `ModManager.ResolveLocalModUploadPlan` calls Steam item-detail ownership resolution; if Steam is not initialized, the query is stale/busy, or the callback does not resolve, official UI can keep or fall back to an upload/create plan even when the local package has a nonzero `workshop_id`.

## Changes

- Added `Workshop.LocalUploadPlan` hook status.
- Added a Harmony prefix on `DolocTown.Config.ModManager.ResolveLocalModUploadPlan`.
- The prefix only intercepts official-local packages when all of these are true:
  - `ModInfo.source == Local`.
  - `ModInfo.workshopId != 0`.
  - The package root contains DTMAPI's generated marker: ordinary functional packages use `Content/DTMAPI/dtmapi-package.json`, while the DTMAPI runtime package uses `Content/DTMAPI/release-manifest.json`.
- For those packages, DTMAPI constructs the game's native `WorkshopUploadPlan(Update, workshopId)`, invokes the original callback, and skips the native Steam plan query.
- Any non-DTMAPI package, missing marker, zero workshop id, or reflection failure falls back to the native resolver.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `docs/updates/INDEX.md`
- `docs/hook-map/README.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`

## Validation

- `git diff --check`: passed; line-ending warnings only.
- `tools/scripts/build.ps1 -Configuration Release`: passed with 0 warnings and 0 errors.
- `tools/scripts/test.ps1 -Configuration Release`: passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- `tools/scripts/install-to-game.ps1 -Configuration Release -SkipBuild`: installed runtime and official-local DTMAPI packages.
- Steam title official Mod UI smoke with temporary MoreEquipment priority boost:
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260614-223311`.
  - `Workshop.LocalUploadPlan = verified` for `Local.DTMAPI_MoreEquipmentSlots` with `workshopId=3744059735`.
  - `Smoke.OfficialModUi = verified`, selected `Local.DTMAPI_MoreEquipmentSlots`, title `DTMAPI 更多装备栏位`, icon and preview files loaded.
  - Process and fatal checks passed: no leftover `DolocTown.exe`, no fatal instance popup.
  - The harness result is retained as `RunStatus=Failed` only because `OfficialModUiScreenshotFile` collection reported false; the referenced screenshot file exists and was copied to `docs/debug/evidence/WORKSHOP-LOCAL-UPLOAD-PLAN/20260614-2230/official-mod-ui-moreequipment.png`.
- The temporary `SAVE/mod_infos.json` priority change was restored; before/after SHA256 hashes match.
- Rebuilt `dist/workshop-packages`, mirrored the DTMAPI runtime upload package to official local `MODS/DTMAPI` while preserving `workshop.json`, and verified `DTMAPI.GameBridge.DolocTown.dll` hashes match across source Release output, installed game plugin, dist staging, and local upload package.

## Evidence

- Local MoreEquipmentSlots upload folder retained `workshop.json` with `workshop_id=3744059735`.
- The subscribed Steam folder did not contain `workshop.json`, which is expected for subscribed Workshop content.
- Steam Workshop log showed no newer MoreEquipmentSlots upload after the older cloud manifest, so the current symptom was an upload-plan display/resolve problem, not a missing local metadata file.
- Targeted proof log: `docs/debug/evidence/WORKSHOP-LOCAL-UPLOAD-PLAN/20260614-2230/verified-log-lines.txt`.

## Rollback

Remove the `Workshop.LocalUploadPlan` prefix and let `ModManager.ResolveLocalModUploadPlan` use the original Steam detail query. This reopens the observed transient Upload/Create display for DTMAPI-generated official-local packages.

## Follow-Up

Superseded follow-up on 2026-06-14: manual upload attempts after this change showed the official button text was corrected, but updates for DTMAPI and `DTMAPI 更多装备栏位` failed with `k_EResultTimeout`. `Player.log` showed `Update local mod ...` followed by `[MOD] Workshop upload failed: k_EResultTimeout`; Steam `workshop_log.txt` showed content and preview upload started and then reverted with `Timeout`. Successful earlier uploads had a preceding Steam `GetDetails request`, while this prefix skipped native `ResolveLocalModUploadPlan` entirely. The replacement change is `docs/updates/2026/20260614-0010-local-workshop-upload-display-only.md`: do not supply a fake `WorkshopUploadPlan`; only mark `ModData.canUpdateWorkshopItem` for display and leave the official Steam details query/upload path intact.
