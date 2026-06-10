# 20260610-0051 DTMAPI Manager UI MVP Design

## Status

Verified.

## Source Request

User requested the post-`e255191` Fishing follow-up plan and included a Manager UI MVP design branch, explicitly without implementing UI.

## Summary

- Added `docs/design/dtmapi-manager-ui-mvp.md`.
- Defined the MVP information architecture for Mods, Errors/Warnings, Hooks, Features, Config, and Export Report.
- Fixed the MVP data sources to existing diagnostics/config/runtime surfaces such as `IDtmDiagnosticsApi.GetSnapshot()`, diagnostics helper report export, runtime API ownership data, and config registry/runtime state.
- Documented non-goals: no UI implementation, no public API additions, no stability promotion, no log parsing as the primary data source, and no official enablement management in MVP.
- Added implementation acceptance gates for future work, including report path verification and manual UI checks.

## Changed Files

- `docs/design/dtmapi-manager-ui-mvp.md`
- `docs/updates/2026/20260610-0051-dtmapi-manager-ui-mvp-design.md`
- `docs/updates/INDEX.md`

## Validation

- Passed: `git diff --check` (CRLF warnings only).
- Passed: `tools/scripts/build.ps1 -Configuration Release`.
- Passed: `tools/scripts/test.ps1 -Configuration Release`.
- No game smoke was run for this docs-only branch.

## Evidence

- Design document: `docs/design/dtmapi-manager-ui-mvp.md`.
- Build/test output from this branch showed 0 warnings and 0 errors, with `DTMAPI.UnitTests: OK`.

## Related Records

- `docs/api/public-api-matrix.md`
- `docs/reviews/api/2026/20260610-fishing-native-responsibility.md`
- `docs/reviews/api/2026/20260610-fishing-options-contract-review.md`
- `docs/updates/2026/20260610-0050-autofishing-smoke-report-export.md`

## Rollback

Remove the design document and this update record. No runtime, public API, hook, smoke, or package behavior depends on this document.

## Follow-Up

Future implementation should stay internal/Diagnostic first and must not promote `IDtmDiagnosticsApi`, UI helpers, or migrated gameplay APIs as part of the Manager MVP.
