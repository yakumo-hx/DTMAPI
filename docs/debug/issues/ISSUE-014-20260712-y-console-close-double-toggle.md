# ISSUE-014: Y Console Close Double Toggle

- State: `verified`
- Current boundary: Player physical testing remains passed; normal Steam/no-HookProbe automation also re-verifies the retained old DebugConsole DLL through owner-targeted legacy modal dispatch, bounded Escape drain, stable short/held Y input, and no native-menu leak.

## Current Status

- Opened: 2026-07-12 +08:00
- Verified: 2026-07-12 +08:00
- Re-verified: 2026-07-27 +08:00
- Regressed: 2026-08-23 +08:00
- Re-verified: 2026-08-23 +08:00
- Severity: medium
- Regression risk: input/UI lifecycle
- Related Review: `docs/reviews/manual-qa/2026/20260712-0002-y-console-close-double-toggle.md`
- Regression Review: `docs/reviews/manual-qa/2026/20260823-0002-y-console-focused-y-dispatch-order-regression.md`
- Owning Updates: `docs/updates/2026/20260712-0006-y-console-close-edge-owner.md`; `docs/updates/2026/20260715-0013-batch2-steam-player-input-and-public-product-gates.md`; `docs/updates/2026/20260726-0005-debugconsole-twelfth-advanced-product.md`; `docs/updates/2026/20260823-0003-y-console-text-input-hotkey-guard.md`

## Symptom

After the typed hotkey rebuild, Y opens the debug console normally. Pressing Y while it is open triggers two state changes and leaves the console open, so the player must use the close button, Escape, or another close route.

The current 2026-08-23 regression is narrower: while a text field owns input,
Y can still reach the product's typed toggle. It closes the console from its own
search field or opens the console from the game's chest-renaming field.

## Minimum Reproduction

1. Clean-start DTMAPI and enter UI save slot 10 (`-SaveSlot 10`, native archive index `9`).
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
- Tenth-save DebugConsole smoke uses the actual typed Y close route and passes with clean process/fatal checks.
- Keep this issue regressed until the user independently confirms both focused
  search input and native chest rename on the fixed build, followed by one
  ordinary unfocused Y toggle.

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

### 2026-08-23 focused-search regression on public 1.00.05

- The user reproduced that pressing physical Y while the current 1.1.0
  console search field is in use closes the console. The run loaded the exact
  local 1.1.0 DLL (`220160` bytes, SHA-256
  `DBC0540A0187BB9B620AAE504BBF1F5569C23796686939A085B4328FF39B3BE3`)
  on public build `24788406`; ProductNative reported all nineteen patches.
- At `01:58:08.391` and again at `01:58:12.129`, the log records owner-modal Y
  dispatch, typed `debug-console.toggle pressed`, and immediate
  `close boundary reason=hotkey Y`. It never records the
  `debug-console-y-input-focus` protection marker. The saved UI state retained
  `searchText=涂装` across the first close/reopen.
- Source inspection identifies an ordering regression introduced by the
  ProductNative move: Bootstrap calls `SampleDtmInputFrame()` and synchronously
  dispatches the typed toggle before `runtime.Update()` reaches the product's
  `OnUpdateTicked -> DebugConsoleUi.Update`. The UI's raw-Y focus check and
  `Input.Suppress("Y")` therefore run after `ModEntry.OnKeybindPressed` has
  already closed the console.
- A comparison between complete reverse builds `24650773` and `24788406`
  found no relevant change in Unity Input System, legacy input, Unity UI,
  UnityPlayer or the inspected native input-field paths. This is not currently
  attributed to the 1.00.05 game update, a missing patch, or a wrong product
  package.
- Existing tests exercised the focus helper and raw-Y consumer in isolation;
  they did not execute the real Bootstrap order. The prior ordinary Y/Escape,
  opener-edge and lifecycle evidence remains valid, but focused-text acceptance
  is withdrawn. ISSUE-014 is `regressed` until a product-owned pre-dispatch
  focus gate, an integrated ordering test and a real focused-field game run
  pass.
- The screenshot's separate reports of a crash and Y opening the console while
  renaming a chest are not reproduced by this run and remain outside this
  confirmed root cause. No implementation or new `GAME-SMOKE` was performed in
  this diagnostic review.

### 2026-08-23 local 1.1.1 pre-toggle text-focus candidate

- The user then reproduced the second case directly: the native chest rename
  box accepted and selected Chinese/English text, but a physical Y also opened
  DebugConsole. The new run logged normal/broadcast Y followed immediately by
  `debug-console.toggle` and console open; it had no corresponding fatal or
  exception. `RenamingBox -> InputNameBox -> DolocInputFiledComponent` confirms
  that the active native control is a standard `UnityEngine.UI.InputField`.
- `ModEntry.OnKeybindPressed` now calls one product-owned
  `TryConsumeFocusedTextInputY` gate before `ui.Toggle`. The gate reuses the
  tracked console input list and also checks the EventSystem's selected object
  for a focused standard `InputField` or `TMP_InputField`. Type, property and
  `GetComponent(Type)` metadata are cached once. The query runs only on a Y
  pressed edge and the already-existing open-console raw-Y edge; there is no
  closed-state update subscription, per-frame reflection, object scan or new
  Harmony patch.
- Focused `debugconsole-product` Unit, Catalog, native trace, Runtime-floor,
  documentation governance and exact double Author SDK package checks pass.
  The two Author packages are byte-identical; package SHA-256 is
  `70FD50874B6227F4D865087206ECA5D2C19146745E4BB386AFFEF7E1D007EA73`
  and entry DLL SHA-256 is
  `784FD83E3174F80773926DE937171B1294D8F3E8D21240762492EC5E93B0E050`.
- Non-acceptance `GAME-SMOKE/20260823-025619` loaded that exact local 1.1.1
  candidate. Its Y open/close, Escape, reopen, ten-short-tap, held-Y,
  owner-bound input, SaveLoaded, no-fatal, NoNativeSave invariants and clean
  process exit subresults pass. With the console search visibly focused, a
  physical `y` entered the Chinese IME candidate flow and the console stayed
  open; the IME consumed that edge before Core emitted a typed-Y/focus marker,
  so this is visible-outcome evidence rather than causal guard acceptance.
- The overall eleven-product smoke is not a PASS because unrelated Animal
  Viewer/Equipment UI steps were not completed before its deadline. No smoke
  matrix row was added. The native chest rename case has not yet been repeated
  on 1.1.1. ISSUE-014 therefore remains `regressed`, and the owning Update
  remains `implemented`, until the user accepts both focused-input scenarios.
- The local 1.1.1 candidate is installed and selected through authoritative
  `SAVE/mod_infos.json`; Workshop `3742714442` is disabled without changing its
  subscribed bytes or published 1.1.0 authority. See
  `docs/updates/2026/20260823-0003-y-console-text-input-hotkey-guard.md` and
  `docs/reviews/manual-qa/2026/20260823-0002-y-console-focused-y-dispatch-order-regression.md`.

### 2026-08-23 independent player re-verification

- The user independently tested the exact local 1.1.1 candidate and confirmed
  all three gates in order: Y typed in the console search field keeps the
  console open; Y typed while renaming a chest does not open the console; and
  after leaving text input, ordinary Y opens and closes the console normally.
- The final runtime log is `155868` bytes with SHA-256
  `F1B3B72A8BCBCF1F786E8E7B716294649FA45BCE8816E484DC70D5CF0380AF32`.
  At `07:01:54.893`, an owner-modal typed Y emitted the exact
  `debug-console-y-input-focus` message and no close boundary. Later unfocused
  Y edges at `07:02:05.477`, `07:02:06.362` and `07:02:10.013` closed,
  reopened and closed normally, with the reopened lifecycle retaining
  `searchText=伊萨多`.
- This is user verification, not a retroactive PASS for the earlier
  non-acceptance eleven-product smoke. The scoped regression is nevertheless
  closed by its exact independent acceptance conditions, so ISSUE-014 returns
  to `verified`. The unlogged screenshot crash claim remains outside the
  issue.

### 2026-08-31 future fixture migration

- The user designated UI save slot 10 (native archive index `9`) as the
  authoritative fixture for all subsequent Y-console testing so the current
  third-slot accessory state cannot interfere with the product result.
- This changes future reproduction and smoke routing only. It does not alter
  or invalidate the immutable third-save evidence recorded in earlier
  attempts, and it does not classify the third archive as corrupt.
