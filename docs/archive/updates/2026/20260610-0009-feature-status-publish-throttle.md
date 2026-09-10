# 20260610-0009 Feature Status Publish Throttle

## Metadata

- Update ID: 20260610-0009
- Date: 2026-06-10
- Status: verified
- Source: User requested the midterm Refactor hardening route, including feature status publish throttling.
- Owner: Codex

## Scope

- Throttle repeated successful `Feature.<Id>` / diagnostics feature-status publication for high-frequency `Update` dispatches.
- Keep the internal GameBridge feature-status model intact.
- Do not add public-like members to `IGameBridgeFeature`.
- Do not change hook IDs, hook status meanings, smoke result schema, public API contracts, or feature service behavior.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `docs/api/public-api-matrix.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260610-0009-feature-status-publish-throttle.md`

## Summary

- Split feature status recording from feature status publication.
- The internal `GameBridgeFeatureStatus` row still records every dispatch result.
- External hook/diagnostics publication now occurs only when:
  - the feature publishes for the first time,
  - status changes between ready/failed,
  - a failure is recorded or its count/error changes,
  - a non-`Update` lifecycle/host operation completes,
  - a successful `Update` heartbeat is due after 10 seconds.
- This keeps `Feature.Camera`, `Feature.ActionSpeed`, and other feature rows readable while preventing every-frame successful `Update` from rewriting status rows and logs.

## Validation

- `git diff --check` passed with only existing line-ending warnings.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors.
- DirectExe third-save Camera smoke:
  - `GAME-SMOKE/20260610-040940`
  - `Zoom=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, `ForcedClose=Passed`
  - `Smoke.DiagnosticsSnapshot = verified`
  - `Feature.Camera = ready`
  - Diagnostics summary: `loadedMods=14`, `mods=14`, `errors=0`, `warnings=0`, `hooks=60`, `features=5`
- DirectExe third-save ActionSpeed smoke:
  - `GAME-SMOKE/20260610-041156`
  - `ActionSpeedTool=Passed`, `ActionSpeedConfigApply=Passed`, `ActionSpeedInteraction=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, `ForcedClose=Passed`
  - `Smoke.DiagnosticsSnapshot = verified`
  - `Feature.ActionSpeed = ready`
  - Diagnostics summary: `loadedMods=14`, `mods=14`, `errors=0`, `warnings=0`, `hooks=64`, `features=5`
- Log check:
  - Camera smoke produced 10 `Safe feature host dispatch completed Update` status log lines across five features, matching two 10-second heartbeats during the longer camera run.
  - ActionSpeed smoke produced 5 `Safe feature host dispatch completed Update` status log lines across five features, matching one heartbeat during the shorter ActionSpeed run.
  - This confirms successful Update status publication is no longer every-frame.
- Final process check found no leftover `DolocTown.exe`.

## Evidence

- `docs/debug/evidence/GAME-SMOKE/20260610-040940`
- `docs/debug/evidence/GAME-SMOKE/20260610-041156`
- Runtime report zips:
  - `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-041125.zip`
  - `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-041239.zip`

## Related Records

- `docs/updates/2026/20260609-0020-feature-status-model.md`
- `docs/updates/2026/20260609-0030-api-diagnostics-snapshot.md`
- `docs/updates/2026/20260610-0004-diagnostics-snapshot-mod-status.md`

## Rollback Notes

- Revert the publish-throttle fields and `PublishGameBridgeFeatureStatusIfNeeded(...)` helper to return to publishing successful feature status after every dispatch.
- Keep the existing feature-status model unless explicitly rolling back the earlier `20260609-0020` work.

## Follow-Up

- Continue the midterm route with ChestLocator merged policy.
