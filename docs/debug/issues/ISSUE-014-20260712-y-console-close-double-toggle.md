# ISSUE-014: Y Console Close Double Toggle

## Current Status

- Status: verified
- Opened: 2026-07-12 +08:00
- Verified: 2026-07-12 +08:00
- Re-verified: 2026-07-27 +08:00
- Severity: medium
- Regression risk: input/UI lifecycle
- Related Review: `docs/reviews/manual-qa/2026/20260712-0002-y-console-close-double-toggle.md`
- Owning Updates: `docs/updates/2026/20260712-0006-y-console-close-edge-owner.md`; `docs/updates/2026/20260715-0013-batch2-steam-player-input-and-public-product-gates.md`; `docs/updates/2026/20260726-0005-debugconsole-twelfth-advanced-product.md`

## Symptom

After the typed hotkey rebuild, Y opens the debug console normally. Pressing Y while it is open triggers two state changes and leaves the console open, so the player must use the close button, Escape, or another close route.

## Minimum Reproduction

1. Clean-start DTMAPI and enter the third save.
2. Press Y once; confirm the console opens.
3. Release Y completely.
4. Press Y once while the console is open.
5. Observe whether it closes and remains closed.

## Known Facts And Rejected Directions

- The ordinary Mod owns typed `debug-console.toggle=Y` and calls `Toggle` on `KeybindPressed`.
- The Bootstrap UI host independently owns a legacy raw-Y `Close` branch from before the typed hotkey rebuild.
- The Bootstrap update order can expose one physical edge to the UI host before Core typed dispatch.
- Existing smoke bypasses the close keybind by directly calling `Close`, so its prior pass does not disprove the user regression.
- Do not add global debounce, weaken all keybinds, or suppress a valid later Y press.
- Do not remove opener-cycle or focused-text-input protection.

## Acceptance Criteria

- Y open and Y close each produce one final state transition.
- Held opener Y, focused text input, rapid release/retap, Escape, and button close remain correct.
- Focused unit/source tests and Release validation pass.
- Third-save DebugConsole smoke uses the actual typed Y close route and passes with clean process/fatal checks.
- Keep this issue open until the user manually confirms physical Y close on the fixed build.

## Attempts

### 2026-07-12 typed-owner correction

- Removed the legacy host-owned ordinary Y close and retained only opener-cycle/text-focus consumption.
- Corrected `SaveLoaded` scoped dispatch through an open in-save DTMAPI UI while keeping `Gameplay` dispatch blocked.
- Cleared prior one-frame synthetic edges between smoke samples and routed every smoke close through typed input.
- Focused Release build and direct unit execution passed with zero warnings/errors and `DTMAPI.UnitTests: OK`.
- `GAME-SMOKE/20260712-164518` is an incomplete attempt. It exposed the modal `SaveLoaded` dispatch gate and stale synthetic edge; it is not acceptance evidence.
- `GAME-SMOKE/20260712-170328` passed the third-save acceptance route: Y open, Escape close, Y reopen, Y close, ten short taps, held-Y no-flicker, HookProbe, SaveLoaded, no-fatal, and clean process exit. The typed result was `openCount=8; escapeCloseCount=1; yCloseCount=6; shortTaps=10; holdNoFlicker=True`.
- Automated status is verified. The issue remains open solely for the requested player-visible physical Y close confirmation.

### 2026-07-12 player verification

- The user confirmed the installed fixed build passes manual testing.
- Y point-press toggles the console normally; both isolated short presses and consecutive short presses are recognized reliably.
- This satisfies the final physical-input acceptance gate. ISSUE-014 is verified.

### 2026-07-15 Batch 2 integration re-verification

- The ordinary Steam/no-HookProbe gate exposed two integration boundaries beyond the original typed sole-owner fix: a delayed raw Escape could enter the native menu owner after closing the console, and the retained subscribed DebugConsole DLL still uses legacy `RegisterButton("Y")` / owner-bound `ButtonPressed` rather than the current typed keybind.
- The focused Review records the rejected host-direct close and the final boundary: Escape uses a bounded two-clean-frame drain that keeps native input Prefix isolation active, while old modal Y is dispatched only to the active custom-menu owner when that owner has the legacy registration and no typed registration. Ordinary scope and broadcast rules are unchanged.
- `GAME-SMOKE/20260715-145818` passed normal Steam foreground `SendInput` with HookProbe absent: six modal Y closes matched six owner-targeted legacy dispatches, six retained-DLL toggle closes and six compatibility markers, with zero typed modal-Y dispatches; two Escape closes passed and `NativeMenuLeakDetected=false`. Ten 40 ms taps, the held-key route, title cleanup, enablement/three-file save restoration, no-fatal and process exit all passed.
- `GAME-SMOKE/20260715-153336` is the final runner replay: it retained every input/cleanup result above and additionally required the exact `3742714442` tree, retained DLL hash, and one unique Core Workshop load-source record. This closes the final provenance false-positive found during code review.
- ISSUE-014 remains verified. The recovered one missing frame and two frame-driver stalls do not change this scoped result and are not GC evidence.

### 2026-07-27 Advanced ProductNative re-verification

- The twelfth-product split removes normal DebugConsole ownership from
  Bootstrap/mandatory GameBridge. `DTMAPI.DebugConsoleMod` now solely owns the
  typed SaveLoaded Y/Escape registrations, owner-bound modal/Canvas and the
  three ProductNative input Prefixes. The old 0.3.1 UI/input behavior remains a
  separate lazy Compatibility route under owner
  `dtmapi.compatibility.debugconsole.legacy`.
- Non-acceptance runs `20260727-000352` and `002900` showed that opening the
  owner-bound modal suppresses the next ordinary product frame. Applying Canvas
  visibility synchronously in the same main-thread modal-acquisition callback
  closes that timing gap without restoring platform UI ownership.
- `GAME-SMOKE/20260727-083919` is the corrected current-product result. Its
  foreground ProductNative matrix retains the owner-exact Y/Escape behavior
  and records the complete title UI graph and Loader roots at zero.
- Exact old 0.3.1/current-Host `GAME-SMOKE/20260727-085929` supersedes the
  2026-07-15 retained-DLL route for the current candidate. It verifies the
  frozen modal/input behavior with strict 120 ms single-send taps, all seven
  action groups under `nativeOwner=Compatibility`, and title UI graph zero.
- Isolated `NativeSaveExpected` `090702` separately covers Save here and does
  not broaden this input issue's acceptance.
- ISSUE-014 remains verified. The parent DebugConsole admission Update remains
  `implemented/open` until independent acceptance.
