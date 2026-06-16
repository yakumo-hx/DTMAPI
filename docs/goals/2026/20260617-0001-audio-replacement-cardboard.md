# 2026-06-17 Audio Replacement Cardboard Goal

## Source Request

Build an experimental audio replacement API from native responsibility functions, then implement a test mod named `曼波音频替换开纸箱子`.

The concrete target is to replace the native wild cardboard/paper-box open sound with `D:\下载\manbo.wav`. Save slot 10 starts beside a wild paper box; entering the save and pressing `E` should open the box and play the replacement sound instead of the normal paper-box sound.

## Native Owner Findings

- Native owner for paper-box interaction: `DolocTown.DungeonResourceModelPaperBox.OnInteract()`.
- Native sound event from that owner: `SoundEvents.PLAY_RESOURCE_PAPER_BOX`.
- Native sound manager path: `DolocAPI.Sound.PostSoundEvent(...)` -> `WwiseSoundManager.InternalPostSoundEvent(...)` -> `AkSoundEngine.PostEvent(...)`.
- The previous monster-barrage audio experiment failed the important safety rule: if local audio is not loaded/played successfully, native Wwise must not be suppressed.

## Implementation Requirements

- Add an experimental public API that accepts native sound event names as strings and does not expose Wwise, Unity, or decompiled Doloc Town types.
- Fragile Harmony/reflection/Unity audio loading logic must live in `DTMAPI.GameBridge.DolocTown`.
- Replacements must be owner-bound by manifest and fail open: disabled, missing, unloaded, invalid, or failed local audio must allow the native sound event through.
- For the cardboard mod, register only `PLAY_RESOURCE_PAPER_BOX`.
- Include a local WAV asset copied from `D:\下载\manbo.wav` into the mod package.
- Record logs that distinguish registration, preload readiness, event interception, replacement playback, and fail-open fallback.

## Validation Requirements

- `git diff --check`
- Release build and tests.
- Install DTMAPI and the new mod into the local game.
- Use save slot 10; press `E` beside the paper box.
- Evidence must show:
  - DTMAPI startup and GameBridge hook status.
  - `PLAY_RESOURCE_PAPER_BOX` interception.
  - replacement audio played successfully.
  - native event suppressed only after local audio was playable.
  - no leftover `DolocTown.exe` after exit.

## Review Requirements

After a working stage, run parallel code-review subagents from multiple angles:

- API/native-owner reliability review.
- Hook and Unity audio lifecycle review.
- Smoke/install/package review.
- One reviewer must be explicitly adversarial and try to find hidden failure modes such as silent suppression, global event overmatching, leaked Unity objects, or unverified smoke evidence.

Iterate on actionable review findings before completion.

