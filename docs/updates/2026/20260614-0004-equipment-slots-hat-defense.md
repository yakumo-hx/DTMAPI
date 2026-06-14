# 20260614-0004 Equipment Slots Hat Defense

Status: verified

## Source Request

User reported that `mushroom_hat` could not be placed into MoreEquipmentSlots and asked whether this was a whitelist or hardcoded rule. After root-cause inspection, the user clarified that mushroom hat has a `+1` effect and should inject that effect while still not changing the character sprite.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Features/EquipmentSlots/DolocTownExperimentalBridgeApi.EquipmentSlots.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/ContentSmoke.cs`
- `docs/reviews/manual-qa/2026/20260614-0002-equipment-slots-mushroom-hat-review.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`

## Implementation

- Fixed the extra-slot validator so registered official hats are not rejected solely because `HatInfo.Skill` is empty.
- Split hat handling into native semantic paths:
  - hats/passives with `Skill` still use `AgentEquipmentFunction.CreateAgentEquipmentFunction`;
  - hats with `HatInfo.Defense` are applied after native `AgentEquipmentManager.ReloadParams` by adding the extra-slot hat defense to `AgentEquipmentAbility`;
  - hats with neither skill nor defense can still be stored, displayed, and recovered without changing vanilla visuals.
- Kept extra hats out of native `hatItem`, so the character sprite remains owned by the official hat slot.
- Extended NewContent equipment-slot smoke to give/equip/recover `mushroom_hat`, assert the native visual hat slot is unchanged, and assert `DolocAPI.AgentEquipmentParams.defence` increases while the extra-slot mushroom hat is equipped.

## Validation

- `tools/scripts/build.ps1 -Configuration Release` passed on 2026-06-14 with 0 warnings/0 errors and `DTMAPI.UnitTests: OK`.
- `git diff --check` passed on 2026-06-14 with line-ending warnings only.
- `tools/scripts/test.ps1 -Configuration Release` passed on 2026-06-14 with 0 warnings/0 errors and `DTMAPI.UnitTests: OK`.
- Third-save `tools/scripts/run-game-smoke.ps1 -AutoExerciseNewContentApis -DisableSecondMotorForSmoke -AutoExitAfterSecondsOverride 90 -TimeoutSeconds 220` passed in `GAME-SMOKE/20260614-082151`. Evidence records `NewContentEquipmentSlots=Passed`, `NewContentApis=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- Equipment-slot smoke evidence includes `mushroomHatGive=mushroom_hat 0->1`, `mushroomHatBackpack=1->0`, `mushroomHatDefence=0->1->0`, `mushroomHatRecover=1`, `mushroomHatRecoverBackpack=0->1`, `recoveredStored=0`, and unchanged native visual hat evidence `nativeHat=miner_helmet->miner_helmet->miner_helmet`.

Not yet run:

- Manual QA recheck for mushroom hat placement/effect/save/recovery.

## Related Records

- Manual QA review: `docs/reviews/manual-qa/2026/20260614-0002-equipment-slots-mushroom-hat-review.md`
- Protected storage baseline: `docs/updates/2026/20260614-0001-equipment-slots-protected-storage.md`
- API native-owner review: `docs/reviews/api/native-owner-domains/08-hats-accessories-equipment-slots.md`

## Rollback

Revert the validator and post-`ReloadParams` defense merge to return to the previous skill-only extra-slot behavior. That rollback will again reject `mushroom_hat` and any official hat whose effect is represented by `HatInfo.Defense` rather than `HatInfo.Skill`.

## Follow-Up

- Run the new third-save NewContent smoke and capture evidence.
- Reinstall the updated DTMAPI/MoreEquipmentSlots local package before Workshop upload if manual QA passes.
