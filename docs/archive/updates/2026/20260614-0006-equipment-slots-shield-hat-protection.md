# 20260614-0006 Equipment Slots Shield Hat Protection

## Summary

Added managed shield-hat protection for DTMAPI extra equipment slots. Extra slots now recognize registered hats whose native hat function/skill resolves to the paper-box shield path, store shield charge in the protected per-save sidecar, and handle player hits without letting the native shield break path clear the vanilla visual hat slot.

## Source Request

User asked to support shield hats through the same protected extra-slot path, including mod hats that register the native shield-hat effect, so players can equip multiple shield hats in extra slots without sprite pollution or item duplication.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Features/EquipmentSlots/DolocTownExperimentalBridgeApi.EquipmentSlots.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/EquipmentSlots/EquipmentSlotShieldPolicy.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/ContentSmoke.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`
- `docs/updates/INDEX.md`

## Behavior

- `TryValidateExtraEquipmentSlotItem` now reads hat traits as a single native metadata snapshot: display name, skill id, defense, shield max value, shield defend, native item function type, and native skill function type.
- Shield hats are not inserted into `AgentEquipmentManager.functions`, avoiding native `AgentEquipmentFunctionShield.TryBlockAttack` calling `DolocAPI.EquipHat(string.Empty)` when a shield breaks.
- `BodyController.OnAttacked` prefix first checks native `AgentEquipmentManager.TryGetShieldItem`; if the vanilla hat slot already owns a shield, DTMAPI returns control to the game.
- If the vanilla shield path is absent, DTMAPI consumes the highest-index active extra-slot shield first, mirrors native shield damage semantics, preserves the vanilla visual hat slot, updates the player hit tail path, and marks protected sidecar storage dirty for the next native SaveGame.
- Old stored shield hats without shield-charge fields initialize from native `ItemFunctionHatShield.MaxShieldValue` on load/apply.
- Extra hat defense only counts enabled owners and slots still inside the currently enabled extra-slot range, so a disabled/recovery-failed owner cannot keep contributing defense through stale runtime entries.
- `box_hat` smoke coverage was added to NewContent/EquipmentSlots smoke.
- `IEquipmentSlotsApi` remains Experimental.

## Validation

- `git diff --check` passed with line-ending warnings only.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings/0 errors and `DTMAPI.UnitTests: OK`.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings/0 errors and `DTMAPI.UnitTests: OK`.
- Third-save Steam NewContent smoke `GAME-SMOKE/20260614-120739` passed `NewContentApis`, `NewContentEquipmentSlots`, `ProcessExited`, and `NoFatalInstanceWindow`.
- `GAME-SMOKE/20260614-120739` logs `Player.EquipmentSlotsShield = verified` and `Smoke.NewContentEquipmentSlotsShield = verified` with `box_hat`, `damage=1`, `blocked=True`, `blockedDamage=1`, and `shield=80->79/80`.
- The same smoke keeps the vanilla visual hat stable (`nativeHat=miner_helmet->miner_helmet->miner_helmet`), recovers `box_hat` back to backpack, leaves `recoveredStored=0`, and writes updated hat table evidence under `DTMAPI/evidence/NEWCONTENT-025/20260614-120844/equipment-hat-table.json` and `.csv`.
- User manual QA passed the missing multi-shield cases: official slot paper-box plus extra-slot paper-box prioritizes the official slot; one extra-slot paper-box plus two other hats consumes only the paper-box shield; three extra-slot paper-box hats consume only the current shield. Manual QA also confirmed mushroom-hat/defense-hat values load successfully.
- Known accepted visual difference: vanilla paper-box hats add the official yellow shield bar on the health UI, while DTMAPI extra-slot paper-box hats do not. User explicitly said not to fix this now.
- Independent read-only subagent review found no high-risk hardcoding/save/visual-pollution/double-consumption issue. It flagged the player-hit tail replay as Experimental/native-version-sensitive and noted the stale disabled-owner defense edge, which was fixed before the final build/smoke/upload sync.

## Rollback

Remove the `BodyController.OnAttacked` prefix, `EquipmentSlotShieldPolicy`, shield trait fields in storage/runtime entries, and the `box_hat` smoke step. Stored sidecar shield fields are additive and can be ignored by older code, but shield hats would revert to the unsafe native-function path unless validation rejects them.

## Follow-Up

- Yellow shield-bar parity for DTMAPI extra-slot shield hats remains a visual follow-up only if later requested.
- Same-process official disable still has the known inert extra-slot visual residue until restart; this update does not change that hot-disable UI lifecycle.
- Future custom shield effects need a deliberate API shape; this update only trusts native `ItemFunctionHatShield` / `AgentEquipmentFuncProtoShield` metadata.
