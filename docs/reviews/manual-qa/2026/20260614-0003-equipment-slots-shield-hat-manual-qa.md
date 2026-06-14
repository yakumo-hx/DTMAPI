# MoreEquipmentSlots Shield Hat Manual QA

Date: 2026-06-14
Source: user manual QA after `20260614-0006` shield-hat protection.

## 1. Vanilla shield hat plus extra-slot shield hat

User feedback:

- The official/vanilla hat slot held a paper-box shield hat.
- An extra MoreEquipmentSlots slot also held a paper-box shield hat.
- Taking damage consumed only the currently active paper-box shield.
- The official slot shield had priority over DTMAPI extra-slot shields.

Analysis:

- This matches the intended native-owner rule: `BodyController.OnAttacked` first defers to native `AgentEquipmentManager.TryGetShieldItem` when a vanilla shield exists.
- DTMAPI extra-slot shield handling should only run when the vanilla shield path is absent.
- No visual hat pollution was reported.

Acceptance status:

- Passed by user manual QA.

## 2. Extra-slot shield hat plus two other hats

User feedback:

- One extra equipment slot held a paper-box shield hat.
- The other two extra equipment slots held non-shield hats.
- Taking damage consumed only the paper-box shield hat.

Analysis:

- This matches the intended managed shield selection filter: only entries resolved as native `ItemFunctionHatShield` / `AgentEquipmentFuncProtoShield` should participate in shield blocking.
- Non-shield hats should continue to provide their own skill/defense effects without being consumed by shield damage.

Acceptance status:

- Passed by user manual QA.

## 3. Three extra-slot shield hats

User feedback:

- All three extra equipment slots held paper-box shield hats.
- Taking damage consumed only the current paper-box shield.
- The manual result is consistent with extra-slot shield hats stacking as multiple independent charges.

Analysis:

- This exercises the multi-shield case that automatic smoke had not covered.
- The implementation selects active DTMAPI-managed shield entries in tail-first order after native vanilla shield priority.
- No cross-slot double consumption was reported.

Acceptance status:

- Passed by user manual QA.

## 4. Mushroom hat and defense hats

User feedback:

- Mushroom hat and similar defense-value hats loaded their defense values successfully.

Analysis:

- This confirms the prior `mushroom_hat` fix in live manual play: extra registered hats without `HatInfo.Skill` but with `HatInfo.Defense` are accepted and mirrored through the DTMAPI post-`ReloadParams` defense bridge.

Acceptance status:

- Passed by user manual QA.

## 5. Extra-slot shield hats do not add the official yellow shield bar

User feedback:

- The official paper-box shield hat adds an extra yellow segment to the health bar.
- A paper-box shield hat in an extra equipment slot does not add that yellow bar.
- User explicitly said this does not need to be fixed now.

Analysis:

- This is a known visual parity gap, not a functional blocker for the current protection update.
- The current DTMAPI path intentionally avoids inserting extra-slot shield hats into native `AgentEquipmentManager.functions`; that is what prevents native shield break from clearing the vanilla visual hat slot.
- The yellow bar is therefore likely owned by the native shield status-bar/UI path that reads the vanilla `IAgentEquipmentShieldItem`.

Acceptance status:

- Accepted known difference for this update; no code fix requested.

## Validation Links

- Implementation/update record: `docs/updates/2026/20260614-0006-equipment-slots-shield-hat-protection.md`
- Automated smoke evidence: `docs/debug/evidence/GAME-SMOKE/20260614-120739`
- Regression row: `EQUIPMENT-SLOTS-SHIELD-HAT-PROTECTION-20260614`
