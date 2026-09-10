# 20260614-0005 Equipment Hat Table Diagnostic

## Summary

Added a read-only equipment hat table diagnostic to the NewContent/EquipmentSlots smoke path. The smoke now enumerates runtime `DolocConfig.Tables.TbHat.DataList` and cross-checks each hat against `DolocConfig.Tables.TbItem.DataList` rows whose function is `ItemFunctionHat` or `ItemFunctionHatShield`.

## Source Request

User requested the minimum reliable next step: output every runtime hat's `HatInfo.Id`, `Skill`, `Defense`, `Skill_Ref?.Id`, `Skill_Ref?.GearEntry`, `Skill_Ref?.Function.GetType().Name`, and cross-check `TbItem[itemId].Function.GetType().Name`, `ItemFunctionHatBase.HatId`, and whether `HatId_Ref` is resolved.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Smoke/ContentSmoke.cs`
- `docs/updates/INDEX.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`

## Behavior

- Writes `equipment-hat-table.json` and `equipment-hat-table.csv` under the runtime `DTMAPI/evidence/NEWCONTENT-025/<timestamp>` folder.
- Publishes `Smoke.EquipmentHatTable`.
- Adds the diagnostic summary to `Smoke.NewContentEquipmentSlots`.
- Does not change `IEquipmentSlotsApi`, extra-slot validation, protected storage, native stat application, or UI behavior.
- Keeps `IEquipmentSlotsApi` Experimental.

## Validation

- `git diff --check` passed with existing line-ending warnings only.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings/0 errors and `DTMAPI.UnitTests: OK`.
- Third-save Steam NewContent smoke passed: `docs/debug/evidence/GAME-SMOKE/20260614-091839`.
- Runtime diagnostic evidence: `Smoke.EquipmentHatTable = verified`, `hats=33`, `itemHatRows=33`, `hatsWithoutItems=0`, `itemRowsWithoutHatInfo=0`.
- Generated files:
  - `D:\Steam\steamapps\common\Doloc Town\DTMAPI\evidence\NEWCONTENT-025\20260614-091928\equipment-hat-table.json`
  - `D:\Steam\steamapps\common\Doloc Town\DTMAPI\evidence\NEWCONTENT-025\20260614-091928\equipment-hat-table.csv`
- Exit checks passed: no leftover `DolocTown.exe`, no fatal instance popup.

## Key Runtime Findings

- `box_hat` resolves as `ItemFunctionHatShield`, `Skill=shield`, `Skill_Ref.Function=AgentEquipmentFuncProtoShield`.
- `mushroom_hat` resolves as `ItemFunctionHat`, `Skill=""`, `Defense=1`, with `HatId_Ref=true`.
- The current loaded runtime content has no hat item rows missing `HatInfo` and no `HatInfo` rows missing a hat item row.

## Rollback

Remove `CaptureEquipmentHatTableForSmoke` and the `hatTable={...}` summary addition from `ContentSmoke.cs`. This is diagnostic-only and does not require storage or save migration.

## Follow-Up

Use the generated CSV/JSON as the factual source for deciding whether shield hats should be stored-only or safely adapted. Do not infer stable shield behavior from enumeration alone; `ItemFunctionHatShield` still has native stateful behavior that can affect vanilla hat ownership.
