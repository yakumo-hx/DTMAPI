# 20260610-0035 DTMAPI Version Compatibility Policy

## Status

Verified.

## Source Request

User requested the post-review midterm follow-up route, sixth branch `codex/api-version-compatibility-policy`, to record the current DTMAPI API version compatibility policy without changing loader behavior.

## Summary

- Added `docs/reviews/api/2026/20260610-dtmapi-version-compatibility-policy.md`.
- Documented that `MinimumDTMApiVersion` and dependency minimums remain numeric compatibility checks: prerelease/build suffixes are trimmed before `System.Version` comparison.
- Documented that `0.5.0-alpha` is the current dev baseline label, not a strict SemVer prerelease blocker for the numeric `0.5.0` surface.
- Documented why BepInEx plugin metadata must keep numeric `DtmApiRuntime.BinaryVersion = 0.5.0.0`.
- Did not add `AllowPrerelease`, did not change runtime loader behavior, and did not change public API surface.

## Changed Files

- `docs/reviews/api/2026/20260610-dtmapi-version-compatibility-policy.md`
- `docs/api/public-api-matrix.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260610-0035-dtmapi-version-compatibility-policy.md`

## Validation

- `git diff --check` passed with CRLF warnings only.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- No game smoke was run for this docs-only branch because runtime, loader behavior, public API members, packages, and installed game behavior were unchanged.

## Evidence Links

- API review: `docs/reviews/api/2026/20260610-dtmapi-version-compatibility-policy.md`
- Public API matrix: `docs/api/public-api-matrix.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- Prior retained runtime evidence:
  - failed BepInEx prerelease metadata attempt: `docs/debug/evidence/GAME-SMOKE/20260610-120641`
  - passed Camera alpha-baseline smoke: `docs/debug/evidence/GAME-SMOKE/20260610-121420`
  - passed ActionSpeed alpha-baseline smoke: `docs/debug/evidence/GAME-SMOKE/20260610-121631`

## Rollback

- Remove the policy review and matrix/update references. No runtime rollback is needed because this branch does not change code.

## Follow-Up

- If DTMAPI later wants strict SemVer prerelease behavior, design it as a separate manifest compatibility change with migration notes and new smoke coverage.
