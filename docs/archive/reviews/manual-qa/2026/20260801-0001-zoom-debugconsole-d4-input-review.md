# Zoom、Y 键控制台与 D4 输入所有权复核

- Review ID: `20260801-0001`
- Date: `2026-08-01`
- Status: `recorded`
- Scope: Zoom camera ownership, DebugConsole modal refresh, incomplete world-action UI, Core input audience
- Source: user manual QA after the twelve-item `0.5.5` profile run
- Owning Update: [20260801-0001-zoom-debugconsole-d4-correction](../../../updates/2026/20260801-0001-zoom-debugconsole-d4-correction.md)

This Review freezes the pre-implementation observations and root-cause boundaries. Implementation and validation facts belong to the owning Update.

## 1. Zoom 4x flickers and follows the map centre instead of the player

### Feedback

At 4x the view flickers and becomes fixed around the map centre. The user confirmed that the split product no longer preserves the older Zoom meaning: Zoom should alter the visible orthographic extent while leaving the game's native camera follow and room calculations under native ownership.

### Code facts

- The ProductNative scale and restore paths write `Camera.orthographicSize` and then actively invoke `CameraController.RefreshResolution()`.
- Native `RefreshResolution()` derives controller fields such as `camSize` and room range from the current orthographic size. Calling it while the camera is already scaled therefore promotes the temporary Zoom value into native controller state.
- The existing `SetEnvCamera` postfix is sufficient to reapply the selected multiplier after a scene camera transition, but it cannot undo controller-state drift introduced by the active refresh call.

### Root cause and ownership

Zoom accidentally assumed ownership of native derived camera state. The correct product boundary is narrower: it owns only the temporary `orthographicSize` multiplier. Native refresh must run against the unscaled baseline, and the product may reapply its presentation value only after the native call finishes.

### Acceptance boundary

- 1x/2x/4x changes never invoke native refresh.
- A genuine native refresh observes the 1x orthographic baseline, and the selected multiplier is restored on both normal return and exception propagation.
- Zoom never writes native `follow`, `enabled`, `camSize`, range, position, or other controller fields.
- `4x -> native refresh -> 2x`, scene transition, title return, disable and Loader cleanup preserve native follow while restoring the exact product-owned value.

## 2. DebugConsole source/category/page clicks redraw only after closing and reopening Y

### Feedback

Clicking the item-source or category list changes the underlying selection but does not redraw the list until the Y console is closed and reopened.

### Code facts

- DebugConsole records a dirty UI state from click handlers and rebuilds it during its ordinary `GameLoop.UpdateTicked` handler.
- Core currently suppresses all Mod `UpdateTicked` delivery while a modal overlay is open through `UI.BlocksModUpdates`.
- The Y overlay does not pause native world time. Native gameplay and direct Harmony/Unity work may continue while DTMAPI event-driven state machines are frozen.

### Root cause and ownership

This is not a missing product click callback. It is a Core event/input boundary defect: one modal flag conflates UI input ownership with the right of every Mod to advance background state. A product-private polling bypass would fix only this screen and preserve the inconsistent global clock.

### Acceptance boundary

- Every valid Runtime frame continues to dispatch `UpdateTicked`, including while an owner or platform modal is open.
- The console's existing dirty/update path redraws on the next UI tick without a private pump.
- Modal isolation applies only to DTMAPI input delivery, not to the update clock.

## 3. Generator, monster and resource actions are not player-ready

### Feedback

The Chinese UI exposes three buttons whose selection, scope, quantity, availability and result semantics are not complete. Their adjacent English/native-owner status text is also player-ambiguous.

### Code facts

The ProductNative actions, translation keys, frozen ABI and Compatibility executors are still required by existing diagnostic consumers. Removing them would expand this UI correction into an ABI and compatibility change.

### Decision boundary

Hide only the three player-facing buttons and the adjacent status line. Keep the executors and compatibility surface dormant. Before re-enabling them, the product needs a separate world-mutation and save-semantics review covering selection/search/count, current-scene availability, effect scope, confirmation, rollback and result verification.

## 4. Independent D4 review and completed design corrections

### Review result

The proposed D4 direction is correct: world/Mod updates continue, while Core freezes an input audience for each sampled frame. The original outline needed the following explicit safety conditions before implementation:

- distinguish `Normal`, owner-modal and platform-modal audiences and freeze owner, effective scope and overlay session until `Input.ClearFrame()`;
- target owner-modal button and keybind delivery to the modal owner instead of broadcasting and relying on helper-side filtering;
- make both string and `DtmButton` queries owner-aware, without creating arbitrary local snapshot watches in a modal;
- record the exact owners that received Pressed and deliver their settlement Release even after scope, modal, official UI or configuration eligibility changes;
- require physical neutral before any newly eligible Pressed after those boundaries;
- preserve the old keybind snapshot until an outstanding press/release cycle closes;
- sample held buttons, settlement/rearm buttons and chord members even when they are not currently eligible;
- retain the exact legacy modal route for the frozen old DebugConsole ABI.

### Additional source finding

`Suppress()` currently removes the button from the physical `down` set. A physically held key can consequently appear as a fresh press on the next frame. D4 must split physical truth from owner-visible state: suppression hides only the current DTMAPI-visible frame and cannot delete physical edges, cancel an owed Release, or synthesize a later Pressed.

### Promise boundary

D4 isolates only DTMAPI-delivered input. It does not claim to stop a third-party Mod that directly reads Unity input, owns a Harmony patch, or performs a native game action. Such behavior remains author-managed.

## Rejected shortcuts

- Do not give only the modal owner `UpdateTicked`; that retains a clock split for every other Mod.
- Do not broadcast all modal input and rely solely on helper queries; event handlers can act before querying.
- Do not hide lifecycle warnings or special-case DebugConsole/Zoom in diagnostics.
- Do not delete legacy action executors or change public ABI in this correction.

## Resolution

Implementation and actual evidence are owned by [Update 20260801-0001](../../../updates/2026/20260801-0001-zoom-debugconsole-d4-correction.md). The Update must remain `implemented/manual-pending` until the user's combined player test completes.

## 2026-08-01 post-implementation manual observation (append-only)

The following observations were reported after the combined twelve-Mod hand test. They are kept in the user's issue order and do not change the earlier pre-implementation facts.

### 1. Opening the official Mod UI leaves five DTMAPI warnings

#### Feedback and screenshot facts

- The corrected Y-console refresh and hidden unfinished actions passed manual testing, and the combined Mod profile was otherwise functional.
- After opening the official Mod UI once, Manager showed `Errors (0)` and `Warnings (5)`.
- The five retained rows were two lifecycle-boundary diagnostics and three resource-lifecycle diagnostics, all in `ReturnedToTitle`.

#### Runtime evidence

- The player log spans `06:54:35` through `06:58:46`, contains 1,064 Info rows, five Warn rows, zero Error rows, and is 380,944 bytes.
- The official UI caused two native `ModManager.ReloadMods` completions at `06:58:25.843` and `06:58:27.336`. Each completion called the full `NotifyWorkshopModListChanged -> DiscoverMods -> LoadMods` path.
- Both passes found the same 15 DTMAPI-capable folders and 11 loaded owners, loaded zero new Mods, and published zero shadow-registry diffs.
- The second pass therefore produced two `Shadow registry refresh repeated` warnings (`DiscoverMods` and `LoadMods hot loadedNow=0`) and one repeated `ContentQuery` rebuild warning. The generation drain then produced the remaining repeated `CustomAnimals` and `AudioReplacement` rebuild warnings. Both domains contained zero active content entries in this run.
- Health snapshots still report zero failed features, zero event-handler failures, zero owner-cleanup failures, and zero resource errors.

#### Root cause

The user's attribution to opening the official Mod UI is correct. This is not a failure of the five named features, but it is also not merely unrelated visual noise: the native UI emits two reload completions, Core performs two unchanged full scans/publications, and the diagnostics key repetition only by lifecycle phase/reason or phase/content generation. They have no reload transaction or unchanged-source fingerprint with which to distinguish a legitimate second native signal from a harmful rebuild loop.

#### Candidate corrections

1. Preferred: coalesce native reload completions into one trailing refresh transaction. Every native signal marks the source dirty, but Core runs `DiscoverMods/LoadMods` once after the short burst becomes quiet. This preserves a real source change while avoiding the duplicate scan, publication and warnings.
2. Smaller diagnostic-only correction: keep both scans, attach a reload transaction/snapshot identity, and classify a second zero-diff publication as Advanced/Info rather than Warning. This fixes the player-visible false alarm but retains the duplicate work and log volume.

Merely suppressing these five message strings is rejected because it would hide a future repeated rebuild with actual changing output.

### 2. Y-console movement multipliers cannot affect observable gameplay

#### Feedback

Selecting the Y-console movement multiplier appears to succeed, but player movement speed does not change.

#### Code and runtime facts

- The native mutation itself succeeds. The log records successful `2x` and `4x` movement leases, and the ProductNative code writes the native additive scaler as `multiplier - 1` through `MotionAbility.SetMoveScaler(float)`, then reads `MoveScaler` back before reporting success.
- While the console is open, its native `AgentControllerState.EnterUICheck` prefix forces the result to `true`. Native `OnUpdateNormal` returns before it copies movement input or advances `AgentMotion`, so the player cannot move while the changed scaler is active.
- Closing by Y, Escape, button, or the UI close path calls `RestoreAfterConsoleClose()`, which immediately calls `RestoreTransientState()` and restores the exact original movement scaler.
- The player log shows the complete contradiction: a `2x` lease at `06:56:11.667`, console close at `06:56:13.448`, and exact original restoration at `06:56:13.449`. The same acquire/close pattern repeats for later `2x` and `4x` selections.

#### Root cause

This is a lifetime/interaction design defect, not a missing native member and not a D4 update-pump regression. The movement lease exists only while the modal deliberately prevents movement, then is restored in the same transition that returns control to gameplay. There is no frame in which both normal player movement and the selected multiplier are active.

#### Candidate corrections

1. Minimal functional correction: make movement a save-session diagnostic lease instead of a console-open lease. Console close restores only truly modal-scoped state; movement remains active until the player selects `1x`, a new save loads, the title is reached, the product is disabled/unloaded, or owner cleanup runs. No sidecar is added, and the existing exact-original/foreign-mutation safeguards remain mandatory. Because the lease still writes the aggregate native `MoveScaler`, a focused Buff-concurrency test is a release condition rather than an optional follow-up.
2. Stronger ownership correction: stop writing aggregate `MoveScaler` and apply a ProductNative multiplier at the native player-speed read boundary while the diagnostic lease is active. Closing, `1x`, save/title and owner cleanup then clear only the product factor, leaving native Buff and equipment state untouched. This has cleaner ownership but adds a new Harmony target and requires an explicit choice between multiplying motion-only speed and total effective speed.
3. Command-style variant: selecting `2x/3x/4x` arms the value, closes the console, and acquires the movement lease only after modal input suppression has ended. The lease is then cleared by `1x` or the same save/title/owner boundaries. This makes the transition explicit and avoids mutating an unobservable state, at the cost of every speed selection closing the console; it must still choose either the aggregate-field or read-layer implementation above.
4. Release-minimal alternative: hide movement controls for `0.5.5` and reopen them only with the later Y-console interaction redesign. This is safest but removes an advertised diagnostic action.

Allowing ordinary player movement behind the open modal is rejected: it weakens D4 input ownership, makes search/UI keystrokes move the character, and leaves tool/gameplay input isolation ambiguous.

#### Required acceptance for either functional correction

- prove native base speed, `MoveScaler`, effective `MoveSpeed`, and measured displacement for `1x/2x/3x/4x` after the modal closes;
- prove exact original restoration at explicit `1x`, `SaveLoaded`, `ReturnedToTitle`, disable and owner cleanup;
- prove a native Buff mutation is not overwritten or later restored to a stale value;
- prove no save or sidecar changes are introduced by the diagnostic lease.

## 2026-08-01 authority and movement-semantics correction (append-only)

This section corrects two assumptions in the immediately preceding observation after the user identified the official UI transaction boundary and clarified the historical player-facing movement meaning. The earlier W1/W2 and aggregate-field alternatives remain as audit history but are superseded below.

### 1. Official Mod UI open is preview preparation; close is the commit path

#### User correction

The first `ModManager.ReloadMods()` occurs when the official Mod page opens, before the player changes enablement or ordering. The second occurs while the page closes and is followed by persistence. DTMAPI should not treat both calls as equivalent committed source changes.

#### Native and DTMAPI facts

- In the deployed `23762374` game build, `ModUiState.Register()` records the prior global Mod state and calls `ReloadMods()` before registering page controls. This refresh populates the editable official list.
- Toggle and ordering controls mutate the in-memory `ModManager` rows while the page remains open.
- `ModUiState.Hide()` schedules a delayed close transaction: it calls `ReloadMods()`, then `SaveModManager(modManager)`, then reloads official configuration/caches/localization before dismissing the pending box.
- DTMAPI currently patches generic `ModManager.ReloadMods` and immediately performs both native-subscription capture and `NotifyWorkshopModListChanged()`. Core consequently runs `DiscoverMods()` and `LoadMods(false)` for both preview preparation and the pre-save close reload.
- Native snapshot enrichment can expose the edited in-memory enablement value before `SaveModManager` succeeds. Therefore the second generic postfix is closer to the intended commit than the first, but it still cannot prove persistence success.

#### Corrected conclusion

The user's model is correct. W1 burst coalescing and W2 diagnostic reclassification address symptoms while preserving the wrong authority boundary. They are no longer recommended.

The correct design is an official Mod-UI transaction:

- opening `ReloadMods` may update a preview/diagnostic snapshot, but must not activate, deactivate or republish DTMAPI Mods;
- close `ReloadMods` prepares the candidate native snapshot;
- only successful `SaveModManager` promotes that candidate to committed source authority;
- DTMAPI performs one deferred refresh after the native close transaction has completed its configuration/cache reload;
- failed persistence discards the candidate and retains the last committed DTMAPI source state.

A shortcut that merely ignores “the first ReloadMods” is insufficient because it depends on call order, still acts before save success, and does not cover constructor/debug/upload call sites safely.

### 2. `MoveScaler` is native Buff aggregate state, not a DebugConsole-owned multiplier slot

#### Historical attribution

- The player-facing meaning is the user's M2 meaning: `2x/3x/4x` multiplies the game's final effective player movement speed without taking ownership of native Buff, terrain or equipment calculations.
- The historical implementation did not faithfully implement that meaning. Before ProductNative admission it already wrote `MotionAbility.SetMoveScaler(multiplier - 1)` and periodically reapplied it; reset wrote the raw `1x` scaler value of zero.
- Commit `2f11115e` introduced both the ProductNative native-input gate and `RestoreAfterConsoleClose()`. This created the newly observed “active only while movement is blocked, restored when movement resumes” contradiction. D4 commit `772409eb` did not modify either path.
- Later exact-original lease work removed the unconditional zero restore and added failure/foreign-mutation retention. That hardened failure handling but did not make the shared aggregate field product-owned.

#### Native ownership map

- `MotionAbility.MoveScaler` is `_modifier.moveSpeedModifier.x`.
- Native effective motion speed is `base.MoveSpeed * (1 + MoveScaler) + MoveAdder`, followed by terrain moderation.
- `BuffManager.ComposeMoveScaler()` and `BuffComponentBasic.Apply/Remove()` both read the current aggregate, add or subtract their contribution, and call `SetMoveScaler()` again.
- `MotionAbility.Clear()` resets the same aggregate along with the other native motion modifiers.
- Equipment movement addition is applied later by `BodyController.MoveSpeed`, outside `MotionAbility.MoveSpeed`.
- All ordinary player movement states in the current build (`Move`, `Jump`, `ClimbJump`, `Drop`, and trampoline movement) consume `BodyController.MoveSpeed`.

#### Current risk

Merely opening and closing Y with no multiplier selected does not write native movement state. After a multiplier is selected, however, the current code replaces the whole aggregate:

- an already-active native `MoveScaler=0.2` is replaced by `1.0` when the user selects `2x`, so the Buff contribution is suppressed while the lease is active;
- if a Buff applies or expires during the lease, its read-modify-write starts from DTMAPI's replacement value, not the native-only aggregate;
- the current foreign-mutation check can refuse a later stale restore, but then the product lease and altered aggregate remain unresolved;
- value equality cannot detect an ABA sequence in which native mutations leave the same numeric value with different contribution ownership, so an “exact original” snapshot is not proof that an old aggregate may safely be restored.

The usual immediate close path restores the captured value and therefore does not prove that a Buff was permanently cleared in this manual run. Nevertheless, selecting a multiplier demonstrably suppresses existing aggregate Buff state during its active window and can leave incorrect state when native contributions change. This is sufficient to reject the field as the product mechanism.

#### DTMAPI impact map

- ProductNative `DebugConsoleNativeActions` is the only current DTMAPI source body that directly reflects `MoveScaler/SetMoveScaler`.
- The optional Compatibility Host links the same action source for the frozen `IMovementDebugApi`, so the defect also affects a legacy consumer when that backend is resident.
- Mandatory GameBridge contains only the reflected compatibility proxy; it does not own a separate movement executor.
- Current Unit tests assert raw `MoveScaler` replacement, exact snapshot restoration and foreign-mutation refusal. The game fixture reads `MotionAbility.MoveSpeed`; neither layer measures final `BodyController.MoveSpeed`, real displacement, or native Buff apply/remove behavior. Passing evidence therefore proves the old mechanism, not the promised result.

#### Corrected decision: retire the `MoveScaler` path

DebugConsole should not use `MoveScaler` for its player-facing multiplier. The ProductNative implementation should own only an in-memory `debugMovementMultiplier` and apply it as a guarded postfix to the current player's final `BodyController.get_MoveSpeed` result. At `1x`, save/title boundaries, disable and owner cleanup it clears that factor; it never snapshots, writes or restores native movement modifiers.

The same implementation must serve the frozen Compatibility action backend under the existing ProductNative/Compatibility owner exclusion. Public API and DTO signatures remain unchanged, while hook/status text and tests stop claiming `SetMoveScaler` ownership.

Required acceptance is correspondingly replaced by:

- final effective speed and measured displacement are exactly `native final speed * 1/2/3/4` after the modal closes;
- pre-existing, newly applied and expiring native MoveScaler Buffs remain numerically native-correct under and after the product factor;
- equipment addition and terrain moderation occur before the product factor;
- `1x`, `SaveLoaded`, `ReturnedToTitle`, disable, Loader cleanup and Compatibility owner cleanup clear only the product factor;
- ProductNative and Compatibility cannot install two effective final-speed multipliers;
- no save, sidecar or native movement field changes occur.

## Resolution

The owning implementation Update is now `verified`. The final player test
confirmed persistent 2x/3x/4x movement after Y closes, exact 1x restoration with
the native movement Buff intact, and zero new DTMAPI warning after one official
Mod-page open/close. The two latest DTMAPI logs and their BepInEx/Unity
companions contain no hidden warning, error or crash record.

Exact package freezing, subscription rollback, upload-tree hashes and the
byte-bounded existing-Workshop-update authorization are recorded by
[Update 20260801-0002](../../../updates/2026/20260801-0002-workshop-upload-release-closeout.md).
This resolution does not rewrite the rejected W1/W2 or MoveScaler alternatives
above; they remain the preserved root-cause history.
