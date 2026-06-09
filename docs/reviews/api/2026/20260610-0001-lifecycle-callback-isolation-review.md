# Lifecycle Callback Isolation Review

- Date: 2026-06-10
- Scope: `codex/fix-lifecycle-callback-isolation`
- Type: pre-implementation hook/lifecycle review

## Known Facts

- `DolocTownHookCallbacks.AfterLoadArchiveDataPostfix`, `SaveGamePrefix`, `SaveGamePostfix`, `ReturnHomePostfix`, `AgentStateToolExitPostfix`, `AgentStateInteractExitPostfix`, `AgentStateBaseExitPostfix`, and `FishingPullExitPostfix` previously called multiple runtime/GameBridge cleanup and notification steps directly.
- A thrown exception in any early callback could prevent later restore, cleanup, smoke mark, or runtime event dispatch in the same native boundary.
- Fishing automation and ActionSpeed both restore animator-speed snapshots on lifecycle or state-exit boundaries. ActionCompletion fuel/feed shares `AgentStateInteract.OnExit`.
- Existing feature-host dispatch already isolates `IGameBridgeFeature` operations, but these low-level hook callbacks still sat outside that feature-host isolation layer.

## Rejected Directions

- Do not change hook targets, hook IDs, hook status text, smoke result fields, or gameplay service behavior.
- Do not add public API surface for lifecycle isolation.
- Do not collapse all callback failures into a single outer try/catch because that would still allow one callback to skip the rest of the boundary chain.
- Do not treat build success as enough; SaveGame, ReturnedToTitle, AutoFishing, ActionSpeed, and ActionCompletion need smoke coverage.

## Recommended Change

- Add a private `SafeCallback(operation, action)` inside `DolocTownHookCallbacks`.
- Catch and record exceptions under `DTMAPI.GameBridge.Lifecycle`, and write a runtime error log.
- Wrap each boundary step independently.
- Keep `FishingPullExitPostfix` restore in a `finally`-equivalent path so `NotifyFishingPhase("Cooldown")` cannot block `RestoreExperimentalAnimatorSpeeds("AgentStateFishingPull.OnExit")`.

## Acceptance

- Release build/test pass.
- AutoFishing, ActionSpeed, OneAction, InstantSave, and TitleButtonLifecycle smokes pass on local save slot 3.
- Logs contain no `Lifecycle callback failed` entries in the passing smokes.
- Evidence and update records cite the exact smoke IDs.

