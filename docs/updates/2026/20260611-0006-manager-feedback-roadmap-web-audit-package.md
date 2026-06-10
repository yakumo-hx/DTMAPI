# 20260611-0006 Manager Feedback Roadmap Web Audit Package

## Summary

Merged the manager/feedback/roadmap follow-up branches back to `Refactor`, ran final build/test and smoke validation, narrowed the compact web package default evidence to the final current smoke set, and prepared the final web-only audit package route.

## Source Request

User requested the mid/long next-round implementation plan: Manager view-model counters, diagnostics report-export result, Manager runtime skeleton, product roadmap/community loop, CameraView manual handoff, and final compact web audit package only.

## Changed Files

- `tools/scripts/update-audit-package.ps1`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/hook-map/focused/Camera.md`
- `docs/reviews/manual-qa/2026/20260610-0006-cameraview-manual-play-gate.md`
- `docs/goals/2026/20260611-0001-cameraview-manual-play-handoff.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`

## Branches Merged

- `codex/manager-viewmodel-summary-counters`
- `codex/test-diagnostics-report-export-result`
- `codex/manager-ui-runtime-skeleton`
- `codex/docs-product-roadmap-community-loop`
- `codex/qa-camera-view-manual-play-handoff`

## Details

- Final package generation remains web-only for this route; no full package or full zip is generated.
- `tools/scripts/update-audit-package.ps1` now defaults the web package evidence set to current final smokes only:
  - `GAME-SMOKE/20260611-031502` Camera diagnostics report-export smoke
  - `GAME-SMOKE/20260611-031721` ActionSpeed diagnostics report-export smoke
  - `GAME-SMOKE/20260611-031838` AutoFishing behavior and report-export smoke
  - `GAME-SMOKE/20260611-031954` HookProbe UI/runtime smoke
- Historical evidence remains referenced in docs but is no longer copied as default compact package payload.
- CameraView remains pending manual/user confirmation and Experimental.
- No public API members, hook/status IDs, smoke behavior schemas, or Stable/StableCandidate statuses changed.

## Validation

- Passed:
  - `git diff --check`
  - `tools/scripts/build.ps1 -Configuration Release`
  - `tools/scripts/test.ps1 -Configuration Release`
- Passed final game smoke:
  - Camera: `GAME-SMOKE/20260611-031502`
  - ActionSpeed: `GAME-SMOKE/20260611-031721`
  - AutoFishing: `GAME-SMOKE/20260611-031838`
  - HookProbe: `GAME-SMOKE/20260611-031954`
- Final process check: no leftover `DolocTown.exe`.

## Evidence

- Camera `result.json`: `RunStatus=Passed`, `Zoom=Passed`, `DiagnosticsReportExport=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`; report `dtmapi-report-20260611-031644.zip`.
- ActionSpeed `result.json`: `ActionSpeedTool=Passed`, `ActionSpeedConfigApply=Passed`, `ActionSpeedInteraction=Passed`, `DiagnosticsReportExport=Passed`; report `dtmapi-report-20260611-031801.zip`.
- AutoFishing `result.json`: `AutoFishingInputLog=Passed`, `AutoFishingHotkey=Passed`, `AutoFishingMovementCancel=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, `AutoFishingReportExport=Passed`, `DiagnosticsReportExport=Passed`; report `dtmapi-report-20260611-031919.zip`.
- HookProbe `result.json`: `HookProbe=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`.

## Package Notes

- Generate only compact web package:
  - `tools/scripts/update-audit-package.ps1 -WebOnly`
  - `tools/scripts/update-audit-package.ps1 -SelfAuditOnly -WebOnly`
- The web package preserves source/docs/key logs/result files and omits oversized screenshot-heavy evidence and report zip payloads by design.

## Rollback Notes

Revert this update record and restore the previous default evidence list in `tools/scripts/update-audit-package.ps1`. Runtime code, public APIs, and smoke behavior are unaffected by the package default evidence change.
