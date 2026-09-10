# 20260707-0002 - Hotkey Edge Follow-Up Review

## Source Feedback

1. Chord short taps are still not native enough.

Analysis: `DtmKeybind.IsPressed` still required every chord member to be currently down. That misses the native-feeling case where a modifier such as `Ctrl` is down, the main key such as `F6` has `PressedEdge=true`, but the main key is already up by the sampled frame. Unity Input System-style actions can still trigger from that edge.

Fix direction: chord pressed logic must require at least one member with `PressedEdge`, while allowing the remaining members to be down, pressed, or released in the same frame.

2. `KeybindReleased` has not consumed `ReleasedEdge`.

Analysis: Core only released a keybind when aggregate down-state became false after previously being down. A same-frame short tap with `PressedEdge=true`, `ReleasedEdge=true`, and `IsDownNow=false` could publish `KeybindPressed` but not `KeybindReleased`, leaving release-waiting consumers at risk.

Fix direction: typed keybind release dispatch must consume `ReleasedEdge` directly, while legacy `ButtonReleased` remains conservative and is not synthesized for keys DTMAPI never tracked as down.

3. Win32 fallback cannot guarantee native-feeling rapid retap recovery.

Analysis: the guarded Win32 fallback intentionally prevents held-key repeated toggles by requiring local previous state to be up before accepting the transition/current state as a pressed edge. That avoids the AutoFishing F6 repeat regression but cannot reconstruct every missed release-plus-retap case if final state is still down.

Fix direction: keep Win32 as a degraded fallback and rely on Unity Input System `wasPressedThisFrame` / `wasReleasedThisFrame` for native-feeling hotkeys when available.

4. Hot-path allocation pressure is reduced but not fully cleaned up.

Analysis: the large per-frame dictionary had already been removed, but active registration iteration, inactive keybind cleanup, keybind list state checks, suppression checks, and trigger lookup still had LINQ or temporary arrays in ordinary frame paths.

Fix direction: replace those paths with cached registration buffers, reusable cleanup lists, and manual loops. Dirty-path cached button array rebuilds are acceptable.

5. Owner-bound helper state query is not strictly owner-bound.

Analysis: `OwnerBoundInputHelper.WasKeybindPressed` and `IsKeybindDown` called global lookup by id. If two owners registered the same keybind id, a helper could read the first matching owner or global state rather than its own registration.

Fix direction: owner-bound helpers must resolve exact `ownerId + keybindId` keys for transient keybind state, while global helper behavior remains compatible for older callers.

## Verification Notes

- Added console tests for chord short tap pressed+released ordering, release-edge cleanup, same-id owner isolation, rapid retap behavior, and legacy tracked-button compatibility.
- Release build and console unit runner passed after the fix.
- Runtime closure was completed on 2026-07-08 after adding smoke-only typed-frame dispatch: user manual retest passed AutoFishing, Y console, Zoom, AutoFishing F6, movement cancel, and return-to-title; short functional smoke `GAME-SMOKE/20260708-021606` passed the same feature set with A as the AutoFishing movement-cancel key.
