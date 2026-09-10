# 20260630-0006 Custom Animal Diagnostic Follow-Up Log Trim

## Summary

Trimmed the custom animal sleep/wake diagnostic follow-up logs after the Hatch/Shell Crab sleep task boundary passed manual five-night testing. Stable sleeping renderers now clear their diagnostic follow-up immediately, repeated follow-up signatures ignore animator normalized time, and unresolved follow-ups are capped at 60 fixed frames instead of 180.

## Source Request

User reported that five manual nights passed: debug time skips and post-midnight barn entry kept Hatch/Shell Crab sleeping. User asked to review the logs, optimize the extra diagnostics so exported logs are not huge, and commit the result.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Features/CustomAnimals/CustomAnimalAnimatorBridgeService.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/reviews/manual-qa/2026/20260630-0002-hatch-shellcrab-eat-sleep-review.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260630-0006-custom-animal-diagnostic-followup-log-trim.md`

## Log Review

- Latest manual `Player.log` showed `CustomAnimals.SleepTaskBoundary=verified`, `animalAIMakeDecisionFreeTime=True`, and `animalControllerOnUpdate=True`.
- No residual custom-animal `sleep=true period=Night task=DolocTown.AnimalMove` / `AnimalJump` / `AnimalEnterRoom` was found.
- At 00:00, expected `CustomAnimals.SleepTaskBoundary event=FreeTimeDecisionGuard ... replacement=WaitFrames(5)` lines appeared for Shell Crab and Hatch.
- Tool wakeups were normal `AnimalRenderer.OnFell -> Animal.WakeUp`, and 6:00 wakeups were normal morning `Animal.WakeUp`.
- The remaining log volume came from `AnimalRenderer.FixedUpdateFollowUp`: the renderer was already sleeping and the task was native wait, but `animState=sleep@normalizedTime` changed every frame and was treated as a new signature.

## Implementation Notes

- Kept the behavior fix unchanged: `CustomAnimals.SleepTaskBoundary` still owns the FreeTime guard and stale movement-task cleanup.
- `AnimalRenderer.FixedUpdate` diagnostics now remove follow-up context immediately when a custom animal is still `isSleep=true`, AI is `Normal_SleepState` or no-state, the current task is native wait/none, and the renderer is already playing `sleep`.
- Remaining follow-up diagnostic signatures normalize renderer state by dropping the `@normalizedTime` suffix from `animState=...`.
- Reduced the unresolved follow-up cap from 180 fixed frames to 60 fixed frames.
- Added unit coverage for stable sleep follow-up cleanup, normalized-time-insensitive signatures, and keeping diagnostics active for movement-task, FreeTime-state, or non-sleep-renderer cases.

## Validation

- Passed: `tools/scripts/test.ps1 -Configuration Release` with `DTMAPI.UnitTests: OK`.
- Warnings: restricted-network `NU1900` package vulnerability index warnings while querying NuGet metadata.
- Passed: `git diff --check`; only CRLF normalization warnings were reported.
- Passed: installed current Release runtime to the local Doloc Town directory under runtime lock, then ran `tools/scripts/run-game-smoke.ps1 -DirectExe -IncludeHookProbe -SaveSlot 7 -TimeoutSeconds 240 -SkipBuild`.
- Passed: slot 7 smoke evidence `docs/debug/evidence/GAME-SMOKE/20260630-230749` reports clean startup/save-load/hook probe/process exit.
- Passed: `docs/debug/evidence/GAME-SMOKE/20260630-230749/process-check.txt` reports no `DolocTown.exe`.
- Verified: `Unity-Player.log` from the smoke shows `CustomAnimals.SleepTaskBoundary=verified`, `animalRendererFixedUpdate=True`, and zero `AnimalRenderer.FixedUpdateFollowUp` lines.

## Evidence

- Manual QA review: `docs/reviews/manual-qa/2026/20260630-0002-hatch-shellcrab-eat-sleep-review.md`.
- Hook map: `docs/hook-map/README.md` under `CustomAnimals.SleepWakeDiagnostics` and `CustomAnimals.SleepTaskBoundary`.
- Regression matrix: `docs/debug/regressions/smoke-matrix.md`.
- Runtime smoke evidence: `docs/debug/evidence/GAME-SMOKE/20260630-230749`.

## Rollback

Restore the prior `SleepRenderFollowUpMaxFrames` and `AnimalRenderer.FixedUpdate` follow-up signature behavior in `CustomAnimalAnimatorBridgeService`. The behavior-level sleep/task boundary can remain enabled independently.

## Follow-Up

No behavior follow-up is required from this log-trim pass. If a future manual repro shows a custom animal standing or moving at night again, keep the diagnostic follow-up active only for non-wait tasks, non-sleep renderer states, or FreeTime AI states.
