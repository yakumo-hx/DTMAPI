# 20260710-0004 AutoFishing Input and Charge Polish

## Status

- Source/unit implementation: complete.
- Current-build targeted runtime: passed for F7 DefaultLoop/charge 0/soak/cleanup; positive charge/Fast target path has partial runtime evidence.
- Player-visible rapid tap, Capture/Reset, charge-distance, and backswing validation: pending.
- Fixed target version: `0.5.2-alpha`; the workspace is already at this version, so this goal does not authorize another version bump.

## Source Review

Use `docs/reviews/manual-qa/2026/20260710-0001-autofishing-input-charge-polish-review.md` as the per-issue source of truth. Preserve the already-passing first-party product loop and the phase 1–3 Core/GameBridge split.

## Tasks

### A. Replace demand-local product toggle input

- Keep exactly one owner-bound Gameplay keybind registration for the product toggle.
- Update it in place on config save; preserve custom keys and explicit None.
- Ensure title scope samples no product toggle and failed Entry cannot retain a registration.
- Remove steady-state registered-key construction/delegate allocations and retain a warmed 10,000-frame Core allocation gate.

### B. Correct config capture/reset semantics

- Click Capture, then accept the next key; never bind a pre-click key or the Capture mouse edge.
- Show Reset=F6 for AutoFishing while retaining clear through Escape/Backspace/Delete.
- Add default-keybind support as a separate optional experimental interface; do not add an abstract member to `IDtmConfigMenuApi`.
- Keep capture candidate polling active only while a row is capturing.

### C. Add independent per-cast charge and preserve native Ready

- Expose charge `0..1` at `0.05` increments in the first-party product.
- Pass a scalar internal cast request before native `UseFishRod`/Ready entry.
- At zero, release synthetic use input immediately for exact minimum power and rely on native `_isAnimationDone`/`fishing_ready` gating for the backswing.
- Keep InstantBite, SkipMiniGame, FastAnimations, and charge independent. FastAnimations may speed positive charge timing after the backswing, Cast hook flight, and Pull, but must not change the charge target or speed the Ready animator.

### D. Fix fifth-save smoke truthfulness

- Remove AutoFishing main-thread sleep loops and do not double-call `UpdateRuntimeAutomation` from SmokeUpdate.
- Use the configured simple toggle key for internal smoke enable and cleanup.
- Write AutoFishing config for phase, external-hotkey, movement, and title-menu routes.
- When native `HorizontalMoveFactor` is available, use real external movement; use synthetic A only as an explicitly labeled fallback when the native value is unavailable.

### E. Validation and records

- Release build/unit, PowerShell 5.1 parse, JSON parse, and `git diff --check` must pass.
- Manual/runtime fifth-save gates: F6/F7 10× short taps, held-key single toggle, Capture ordering, Reset/clear, charge 0/0.5/1 with Fast off/on, independent/combined options, visible zero-charge backswing, non-blocking first smoke cast, cleanup, and clean process exit.
- Update ISSUE-010, API matrix, hook map, smoke matrix, update index/record. Do not mark ISSUE-010 solved.

## Blockers

- Source/unit evidence cannot complete the visual backswing, capture UX, or physical short-tap gates.
- If zero charge still lacks a visible backswing, capture native animation/state timing; do not add an arbitrary one-frame timer hold.
- If physical short taps fail before Core receives an edge, keep the goal pending and diagnose the Bootstrap backend.

## Rollback

- Roll back the first-party charge request/config as one slice without returning product ownership to `IFishingAutomationApi`.
- Preserve the cached registered key/direct evaluator and stable local snapshot watch state even if the product input route needs further backend work.
- Never restore Unity-main-thread sleeps or tie product disable to `UnpatchSelf`.
