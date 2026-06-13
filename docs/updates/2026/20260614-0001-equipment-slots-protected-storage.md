# 20260614-0001 Equipment Slots Protected Storage

Status: implemented, basic game smoke passed, user manual QA passed except hot-disable visual cleanup

## Source Request

User asked for the most stable/reliable attempt to fix MoreEquipmentSlots uninstall protection and noted that a third-party paper-box expansion mod suggests saving expanded slots from the last slot backwards.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Features/EquipmentSlots/DolocTownExperimentalBridgeApi.EquipmentSlots.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/EquipmentSlots/EquipmentSlotProtectedStoragePolicy.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/AssemblyInfo.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/reviews/manual-qa/2026/20260614-0001-equipment-slots-protected-storage-review.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`

## Implementation

- Equipment-slot sidecar storage now writes under `config/protected-items/equipment-slots/slot-<archiveIndex>/equipment-slots-<owner>.json` instead of the former global owner file.
- Storage documents include schema version, storage scope, archive index, player/custom-player names, current scene, saved total game seconds, and per-entry `tailIndexFromEnd`.
- Storage reads require a loaded archive. Title-page API registration no longer reads equipment-slot sidecars.
- Per-save storage is guarded against clear archive/player/save-clock mismatches.
- Legacy global `equipment-slots-<owner>.json` files can be adopted for the current save and are archived after the next successful per-save persist.
- Missing-mod orphan recovery scans the current save's protected equipment-slot directory first, then legacy globals as migration input, and recovers stored entries from highest slot index to lowest.

## Validation

- `tools/scripts/build.ps1 -Configuration Release` passed on 2026-06-14 with 0 warnings/0 errors and `DTMAPI.UnitTests: OK`.
- `git diff --check` passed on 2026-06-14 with line-ending warnings only.
- `tools/scripts/test.ps1 -Configuration Release` passed on 2026-06-14 with 0 warnings/0 errors and `DTMAPI.UnitTests: OK`.
- Unit coverage was added for per-save scope keys, path-safe names, tail-first ordering, and identity mismatch rejection.
- Attempted third-save `-AutoExerciseNewContentApis` smoke `GAME-SMOKE/20260614-004621` was blocked before launch because `DolocTown.exe` was already running; no gameplay validation is claimed from that run.
- After installing the merged protected-storage branch locally, third-save `tools/scripts/run-game-smoke.ps1 -AutoExerciseNewContentApis -DisableSecondMotorForSmoke -AutoExitAfterSeconds 90` passed in `GAME-SMOKE/20260614-011222`. It verified DTMAPI startup, SaveLoaded, NewContent APIs, MoreEquipmentSlots registration, three rendered/interactive/hoverable extra slots, `grandmas_button` equip/recover, `straw_hat` attribute-only equip/recover while preserving the native `miner_helmet`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and no leftover `DolocTown.exe`. Logs also showed legacy global storage was adopted for save scope `slot-2` and marked dirty for next native SaveGame.
- User manual QA on 2026-06-14 confirmed extra-slot effects apply, extra-slot hats do not alter the character sprite, equip-save-exit-reenter does not duplicate items, other saves are not polluted, and disabling MoreEquipmentSlots returns protected items to the backpack.
- User manual QA also found one remaining lifecycle issue: disabling MoreEquipmentSlots and re-entering the save in the same game process still shows the three extra slot visuals. The slots are inert and item placement is blocked with red warning text; a full game restart removes them. This is tracked as current-process hot-disable UI-registration residue, not as protected-storage data failure.

Not yet run:

- Automated smoke coverage for the user-verified equip-save-reload, cross-save isolation, and disabled-mod backpack recovery flows.
- Disabled/unsubscribed Workshop removal recovery with mail overflow evidence.
- Current-process hot-disable UI cleanup that removes inert extra-slot visuals without requiring a full game restart.

## Related Records

- Manual QA review: `docs/reviews/manual-qa/2026/20260614-0001-equipment-slots-protected-storage-review.md`
- Prior transaction evidence: `docs/updates/2026/20260603-0015-equipment-slots-storage-recovery-smoke.md`
- Prior no-save/SaveGame transaction evidence: `docs/debug/regressions/smoke-matrix.md` rows `MANUALQA-029-README` and `NEWCONTENT-024-H`

## Rollback

Revert the protected storage path/policy changes to return to the old global `equipment-slots-<owner>.json` behavior. This rollback reopens the known cross-save pollution risk and should only be used if per-save storage blocks loading.

## Follow-Up

- Add a dedicated game smoke for MoreEquipmentSlots protected storage: equip, save, reload, no duplicate, cross-save isolation, disable/unsubscribe recovery, mail overflow, clean exit.
- Add a focused lifecycle follow-up for loaded-but-officially-disabled code mods: MoreEquipmentSlots should unregister or hide its reflected extra-slot UI in the current process without attempting DLL unload, while keeping protected item recovery active.
- Design a future generic protected-container API for tail-appended inventory expansions such as paper-box expansion mods. Do not claim third-party container protection until a registration contract and smoke evidence exist.
