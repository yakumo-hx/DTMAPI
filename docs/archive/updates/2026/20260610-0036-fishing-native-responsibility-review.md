# 20260610-0036 Fishing Native Responsibility Review

## Status

Verified.

## Source Request

User requested the post-review midterm follow-up route, seventh branch `codex/review-fishing-native-responsibility`, to add a Fishing native responsibility review without splitting `FishingAutomationFeature` yet.

## Summary

- Added `docs/reviews/api/2026/20260610-fishing-native-responsibility.md`.
- Mapped Ready, Cast, Wait, MiniGame, Pull, and Cooldown responsibilities to native owners and current DTMAPI bridge behavior.
- Separated observation hooks from intervention hooks so a later feature split can avoid mixing status tracking with policy-driven automation.
- Recorded auto-cast, movement cancel, minigame completion, fast animation restore, save/title cleanup, and smoke force-fish evidence boundaries.
- Kept `IFishingAutomationApi` Experimental and did not change runtime, public API, hook IDs, smoke schema, mods, or package layout.

## Changed Files

- `docs/reviews/api/2026/20260610-fishing-native-responsibility.md`
- `docs/api/public-api-matrix.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260610-0036-fishing-native-responsibility-review.md`

## Validation

- `git diff --check` passed with CRLF warnings only.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- No game smoke was run for this docs-only branch because runtime, hooks, public API members, smoke harness, mods, packages, and installed game behavior were unchanged.

## Evidence Links

- API review: `docs/reviews/api/2026/20260610-fishing-native-responsibility.md`
- Hook map: `docs/hook-map/README.md`
- Public API matrix: `docs/api/public-api-matrix.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- Prior retained runtime evidence:
  - `docs/debug/evidence/GAME-SMOKE/20260608-080848`
  - `docs/debug/evidence/GAME-SMOKE/20260609-170646`
  - `docs/debug/evidence/GAME-SMOKE/20260610-012052`
  - `docs/debug/evidence/GAME-SMOKE/20260610-035752`

## Rollback

- Remove the Fishing native responsibility review and matrix/hook-map/update references. No runtime rollback is needed because this branch does not change code.

## Follow-Up

- Use this review as the precondition for a later mechanical `FishingAutomationFeature` split. That split should keep observation hooks separate from intervention callbacks and must preserve existing smoke/result semantics.
