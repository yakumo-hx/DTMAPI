# MoreEquipmentSlots Mushroom Hat Review

Date: 2026-06-14
Source: user screenshot and follow-up question after MoreEquipmentSlots protected-storage QA.

## 1. Mushroom hat cannot be placed in an extra equipment slot

User feedback:

- The mushroom hat (`mushroom_hat`, `菌菇帽`) should be accepted by MoreEquipmentSlots because it is a registered official hat.
- The extra equipment slots should accept hats plus the four known special passive accessories and future similarly tagged official items.
- Screenshot showed the DTMAPI extra slot rejected the item with red text: `Hat item mushroom_hat has no equipment skill.`
- User clarified that the mushroom hat has a `+1` equipment effect, so it should not merely be stored as a cosmetic/no-effect hat.

Analysis:

- This is not an item-id whitelist failure. The DTMAPI GameBridge validator was hardcoded to accept only `ItemPassive` / `ItemHat` items and then required every accepted item to expose a non-empty equipment `Skill`.
- Native hat semantics are split across two responsibility paths:
  - `AgentEquipmentManager.GetSkillFromItem` reads `ItemFunctionHatBase.HatId_Ref.Skill` and creates an `AgentEquipmentFunction` only when a hat has an explicit skill.
  - `AgentEquipmentManager.ReloadParams` separately reads the vanilla `hatItem`'s `ItemFunctionHatBase.HatId_Ref.Defense` and seeds `AgentEquipmentParams(defense)` from that value.
- The mushroom hat is an official `ItemFunctionHat`, but its useful stat comes from `HatInfo.Defense`, not from a non-empty skill string. Requiring `Skill` caused the observed rejection.
- DTMAPI extra slots deliberately must not write the stored hat into native `hatItem`, because doing so would change the character sprite and pollute the vanilla equipment slot. Therefore the correct bridge behavior is to mirror the native defense path after `ReloadParams`, while preserving vanilla visual ownership.

Decision:

- Keep `IEquipmentSlotsApi` Experimental.
- Accept registered official hats even when `HatInfo.Skill` is empty.
- For hats with `Skill`, continue injecting the native `AgentEquipmentFunction`.
- For hats with `Defense`, add the extra hat defense to the post-`ReloadParams` `AgentEquipmentAbility` without writing `hatItem`.
- For hats with neither skill nor defense, allow storage/recovery/hover display but apply no stat effect.

## Validation Required

- Build/test Release.
- Extend NewContent equipment-slot smoke to equip `mushroom_hat`, verify the native visual hat slot remains unchanged, and verify `DolocAPI.AgentEquipmentParams.defence` increases while the item is in the extra slot.
- Manual QA should re-check mushroom hat placement, tooltip/hover, effect presence, save/reload, and disable recovery.
