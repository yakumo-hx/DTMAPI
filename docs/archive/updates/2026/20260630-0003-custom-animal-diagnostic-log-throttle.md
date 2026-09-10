# 20260630-0003 Custom Animal Diagnostic Log Throttle

## Summary

Reduced Hatch/Shell Crab custom animal diagnostic log volume after manual testing showed a short run could generate very large exported logs. The change keeps low-frequency sleep/wake source evidence while suppressing per-frame success noise, and it adds instance-level labels for suspicious Shell Crab render snapshots so one animal can be followed across later logs.

## Source Request

User confirmed Hatch PNG direction and eating are fixed, then asked whether the extra diagnostics can be closed down so player log exports are not huge. User also requested a code-level review of the remaining unclear first-entry barn issue where Shell Crab appeared awake while logs showed no explicit wake call.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Features/CustomAnimals/CustomAnimalAnimatorBridgeService.cs`
- `docs/reviews/code/2026/20260630-0003-custom-animal-sleep-state-log-audit.md`
- `docs/reviews/manual-qa/2026/20260630-0002-hatch-shellcrab-eat-sleep-review.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`

## Implementation Notes

- `CustomAnimals.PngSpriteBridge.<species>` verified status now publishes only on the first successful mapped PNG sprite per species. Missing mapped sprites and degraded states still publish immediately.
- `CustomAnimals.SleepWakeDiagnostics` status now publishes only once per species/event name, instead of repeating for every identical diagnostic event.
- `Animal.OnRender` diagnostics now log only suspicious custom animal render snapshots, currently `sleep=true` with a non-sleep AI state. Repeated identical suspicious signatures are suppressed per runtime animal instance rather than per species; the signature now includes renderer animation state so an `idle -> sleep` visual correction is not hidden by AI-only de-dupe.
- Suspicious snapshots now include an `animal=` identity block with runtime object ref, native index/dataIdx/name/title candidates, position cell/world-position candidates, and renderer ref/position when available. Rendered suspicious snapshots also include `rendererState` with animator controller, current known animator state, movement/eating/jump flags, facing, and current sprite when reflection can read them. This is intentionally diagnostic-only and does not persist a new gameplay id.
- `Sleep`, `WakeUp`, `CallToRoom`, and `AnimalRenderer.OnFell` diagnostics remain available because they are low-frequency and identify the concrete wake source.

## Code-Level Review

Review record: `docs/reviews/code/2026/20260630-0003-custom-animal-sleep-state-log-audit.md`.

Conclusion: the first-entry Shell Crab observation is not a proven wake call. Runtime logs showed `sleep=true` with `aiState=Goat_FreeTimeState` and no `Animal.WakeUp`, `AnimalRenderer.OnFell`, or `Animal.CallToRoom`. The latest slot 7 log after a debug time skip from 18:00 to 00:00 showed all three Shell Crabs plus Hatch entering the same first-render mismatch window at 00:05. Native metadata indicates day change refreshes animal AI, template FreeTime states route back to `Normal_SleepState` when `ShouldSleepNow || animal.isSleep`, and `Normal_SleepState.OnExit` owns `Animal.WakeUp`. The remaining issue is best treated as a transient sleep flag vs AI/render/animator lifecycle mismatch until renderer-state evidence proves whether the visible sprite was actually `idle` or already on `sleep`.

## Validation

- Passed: `tools/scripts/test.ps1 -Configuration Release` with `DTMAPI.UnitTests: OK` after the log-throttle change and again after adding renderer-state fields.
- Passed: `git diff --check`; only existing CRLF normalization warnings were reported.
- Installed current Release runtime to the local Doloc Town directory under runtime lock with `tools/scripts/install-to-game.ps1 -Configuration Release -SkipBuild -SkipOfficialLocalMods`, then released the lock. Repeated after the renderer-state diagnostic addition.
- Passed: `tools/scripts/check-dtmapi-status.ps1` reported required DTMAPI install files present and no legacy DLK/SMAPI items detected. Existing `Local.DTMAPI_ShellCrab` and `Local.DTMAPI_HatchAssets` entries remain enabled in `SAVE/mod_infos.json`.
- Warnings: restricted-network `NU1900` package vulnerability index warnings while querying NuGet metadata.
- Not run: local game smoke after the throttle change. This pass changes diagnostic publication only and does not alter gameplay behavior or custom animal routing.

## Rollback

Move the `PngSpriteBridge` verified `SetHookStatus` call back outside the first-success branch and remove the suspicious-only `OnRender` gate if fuller frame-by-frame evidence is needed again. Prefer doing that only in a short-lived diagnostic branch because it is intentionally noisy.

## Follow-Up

- Next manual run should confirm exported logs no longer contain per-frame Hatch `PngSpriteBridge.hatch=verified` spam.
- If the first-entry Shell Crab visual mismatch remains visible, first compare `rendererState.animState`/`sprite` for the visually standing animal. Add short-lived state-machine transition diagnostics around `AnimalAIState.MakeDecision_Sleep`, `Goat_FreeTimeState.GetNextState`, `Chicken_FreeTimeState.GetNextState`, and `Normal_SleepState.GetNextState/OnExit` only if renderer-state evidence is still ambiguous.
