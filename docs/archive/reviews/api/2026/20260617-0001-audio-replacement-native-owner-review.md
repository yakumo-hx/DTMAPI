# Audio Replacement Native Owner Review

## Scope

This review feeds `docs/goals/2026/20260617-0001-audio-replacement-cardboard.md`.

## Findings

1. `DungeonResourceModelPaperBox.OnInteract()` is the native owner for pressing `E` on a wild paper box.

   It owns particle effects, item drops, completion/removal, and finally posts `SoundEvents.PLAY_RESOURCE_PAPER_BOX`. DTMAPI must not replace this method body or duplicate its drop/removal behavior.

2. `WwiseSoundManager.InternalPostSoundEvent(...)` is the native event-to-Wwise bridge.

   A GameBridge hook at the sound manager layer can replace a narrow native sound event without touching resource gameplay state. This is less invasive than patching paper-box interaction directly.

3. The replacement event must be the narrow paper-box event.

   `PLAY_RESOURCE_PAPER_BOX` is the target. `PLAY_ITEM_BOX`, `PLAY_GARBAGE_COLLECT`, and `PLAY_GARBAGE_RECYCLING` are separate responsibilities and must not be replaced by this mod.

4. Audio replacement must fail open.

   Historical monster-barrage replacement could suppress native Wwise before local audio was actually playable, producing silence. The new API must allow the native event through unless local replacement playback succeeds.

5. API status must remain Experimental.

   The implementation depends on Harmony, Unity audio reflection, and Wwise event names. It is useful as a first audio bridge but is not yet a stable content API.

