# 20260712-0006 Y Console Close Edge Owner

## Metadata

- Update ID: `20260712-0006`
- Date: 2026-07-12
- Lifecycle Status: `verified`
- Validation Level: `docs, source, unit, runtime, player`
- Runtime Validation: `passed`
- Related Issue State: `verified`
- Source: user reported that Y opens the rebuilt console normally but a Y close toggles twice and leaves it open.

## Scope

- make the ordinary DebugConsoleMod owner-bound typed keybind the sole Y toggle owner;
- retain Bootstrap UI-host consumption only for the opener's same physical key cycle and focused text input;
- preserve Escape/button close, console features, public Input APIs, runtime version, and other Mod hotkeys;
- replace direct-Close smoke shortcuts with the actual typed close route;
- add regression coverage and run third-save validation.

## Source Review

- `docs/reviews/manual-qa/2026/20260712-0002-y-console-close-double-toggle.md`

## Known Facts Before Change

- The user-visible sequence matches two confirmed Y consumers, not a missing open event.
- Legacy raw-Y close predates the typed keybind rebuild.
- Existing opener-cycle and text-focus guards are valid and must remain.
- Existing smoke directly closed the host and therefore did not exercise the failing owner-bound close path.

## Implementation

- Removed the Bootstrap host's ordinary raw-Y close mutation. The host now consumes Y only for the opening physical cycle and while a text input owns focus; every ordinary open/close transition is owned by `DTMAPI.DebugConsoleMod/debug-console.toggle`.
- Corrected scoped dispatch so `SaveLoaded` registrations remain active while an in-save DTMAPI UI is open, while `Gameplay` registrations and Mod update ticks remain blocked. This lets an overlay's ordinary Mod owner close itself without leaking gameplay actions through the modal boundary.
- Kept Escape and close-button behavior unchanged. The host's Escape fallback remains safe, and the ordinary close keybind still closes only when the console is open.
- Made each synthetic smoke input call represent one frame by clearing only prior one-frame edges before recording the next sample; held state is preserved. This prevents an earlier Y tap from retriggering during the following Escape sample.
- Reworked DebugConsole smoke so Y open, Escape close, Y reopen, Y close, ten rapid taps, and the held-Y check all traverse the typed keybind path.

## Changed Files

- `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/DebugConsoleSmoke.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/api/public-api-matrix.md`
- `docs/debug/evidence-retention-allowlist.json`
- `docs/debug/issues/ISSUE-014-20260712-y-console-close-double-toggle.md`
- `docs/debug/issues/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/reviews/manual-qa/2026/20260712-0002-y-console-close-double-toggle.md`
- `docs/updates/2026/20260712-0006-y-console-close-edge-owner.md`
- `docs/updates/INDEX-2026-07.md`

## Validation

- Focused Release build completed with `0` warnings and `0` errors; the direct unit executable reported `DTMAPI.UnitTests: OK`.
- Unit coverage confirms opener-cycle and text-focus consumption, ordinary raw-Y delegation, `SaveLoaded` dispatch through an open DTMAPI UI, continued `Gameplay` blocking, and distinct synthetic input frames.
- The first runtime attempt, `GAME-SMOKE/20260712-164518`, was incomplete and exposed the two follow-up test facts: modal dispatch still blocked the delegated `SaveLoaded` keybind, and consecutive synthetic calls retained the prior one-frame Y edge. It is retained as attempt evidence, not acceptance evidence.
- Locked third-save smoke `GAME-SMOKE/20260712-170328` passed `RunStatus`, HookProbe, SaveLoaded, Y open, Escape close, second Y open, Y close, no-fatal, and process-exit gates. `Smoke.DebugConsoleHotkey=verified` reports `openCount=8`, `escapeCloseCount=1`, `yCloseCount=6`, `shortTaps=10`, and `holdNoFlicker=True`.
- The accepted run exited normally, left no `DolocTown.exe`, and released the shared runtime lock.
- `tools/scripts/test.ps1 -Configuration Release` completed with exit code `0`: every build reported zero warnings/errors, `DTMAPI.UnitTests: OK`, runtime evidence retention tests passed, and the regenerated evidence allowlist passed with `370` source files, `689` smoke runs, and `62` runtime identities.
- The same tracked test command ran document governance successfully in quiet mode; `git diff --check` also completed with exit code `0`.
- Player manual retest passed on 2026-07-12: Y point-press toggles normally, and both isolated and consecutive short presses are recognized stably. This closes the physical-input evidence gap without treating automation as player proof.

## Rollback

Revert the host delegation, scoped dispatch correction, synthetic-frame cleanup, and typed smoke changes together. Restoring only the host-owned raw-Y close recreates two Y toggle owners. Do not roll back the typed Input framework or opener/text-focus safety gates.

## Follow-Up

- ISSUE-014 is verified after the player physical-input retest; no scoped acceptance gap remains.
- The public API signatures and `0.5.3-alpha` version are unchanged; Input remains Experimental.
