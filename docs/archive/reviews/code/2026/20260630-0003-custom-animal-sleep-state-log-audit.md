# Custom Animal Sleep State Log Audit

Date: 2026-06-30

Scope: Hatch/Shell Crab diagnostic log volume and the remaining first-entry barn observation where Shell Crab logged `sleep=true` with `aiState=Goat_FreeTimeState`.

Sources reviewed:

- `src/DTMAPI.GameBridge.DolocTown/Features/CustomAnimals/CustomAnimalAnimatorBridgeService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `references/doloc-town/reverse/builds/23762374_public_C416D4/metadata/methods.csv`
- `references/doloc-town/reverse/builds/23762374_public_C416D4/metadata/calls.csv`
- Runtime log from the 2026-06-30 slot 7 manual run.

## Findings

### 1. Hatch sprite success status was too chatty

`TryResolvePngSpriteOverride` runs from native `SpriteOverrideHandler.TryGetModOverrideSprite`, so it can execute for every animated frame. `MarkPngSpriteVerified` already wrote the direct runtime monitor line only once per species, but still called `runtime.SetHookStatus(CustomAnimals.PngSpriteBridge.<species>, verified, ...)` on every mapped frame. HookProbe records status changes, so a short three-minute run generated many Hatch frame mapping lines.

Resolution applied: per-species PNG verified hook status now publishes only on the first successful mapping. Missing sprites and degraded states still publish immediately.

### 2. `Animal.OnRender` diagnostics were useful but too broad

The `Animal.OnRender` postfix was recording every custom animal render refresh. Native metadata shows room entry and pass-time paths can refresh renderers repeatedly: `AnimalSystem.SetCurrentRoom` calls `Animal.RefreshRenderer`, and `Animal.OnRender` applies `_GetCurrentAnimation()` plus `set_ShowSleepEffects(isSleep)`. That made render diagnostics noisy even when state was normal.

Resolution applied: `Animal.OnRender` diagnostics now log only suspicious render snapshots: currently the important one is `sleep=true` while AI state is neither `Normal_SleepState` nor the empty/no-state sentinel. Repeated identical suspicious signatures are suppressed per runtime animal instance, and suspicious lines include `animal=` identity details (`ref`, `index`, `dataIdx`, custom name/title candidates, position cell/world-position candidates, and renderer ref/position when available). A follow-up renderer-state field records animator controller, known current animator state, movement/eating/jump flags, facing, and current sprite when reflection can read them, so the next run can tell whether the visible Shell Crab was actually in `idle` or already playing `sleep`.

### 3. The first-entry Shell Crab observation is not a proven wake call

The manual log showed Shell Crab at night with `sleep=true` and `aiState=Goat_FreeTimeState`, without `Animal.WakeUp`, `AnimalRenderer.OnFell`, or `Animal.CallToRoom` in the same window. A later slot 7 log showed three Shell Crab `OnRender` snapshots at the 0:00 barn boundary and then `Normal_SleepState` snapshots by 0:10, while the player visually observed only one Shell Crab standing. A subsequent 0:00 retest after a debug time skip showed all three Shell Crabs plus Hatch in the first-render mismatch window at 00:05. The old species-only log format cannot identify which visible animals appeared standing or which animation state was active, so the diagnostic was extended with runtime animal identity, position labels, and renderer animator-state labels. Native metadata supports the current interpretation:

- `AnimalAI/AnimalAIState.MakeDecision_Sleep` schedules `Animal.Sleep` through a linear task.
- `Goat_FreeTimeState.GetNextState` and `Chicken_FreeTimeState.GetNextState` both query `ShouldSleepNow` and `animal.isSleep`, then can route to `Normal_SleepState`.
- `Normal_SleepState.OnExit` is the path that calls `Animal.WakeUp`.
- `Animal.__OnDayChanged` refreshes the AI controller, which can recreate the template default FreeTime state while preserving `animal.isSleep`; the next AI update should route back to `Normal_SleepState`.
- `AnimalRenderer.OnFell` also calls `Animal.WakeUp`, which matched the later sickle/tool wakeup logs.

So the first-entry case is best classified as a transient sleep flag vs AI/render/animator lifecycle mismatch. It is not evidence that DTMAPI or the native game called `WakeUp`.

## Rejected Hypotheses

- Missing Hatch PNG frames: rejected for the current run. Adult `eat_4`, `eat_5`, and `eat_6` all map successfully.
- Tool collision for the first-entry observation: rejected for that specific event because no `AnimalRenderer.OnFell` log appeared. Tool collision remains confirmed for later intentional wakeups.
- `CallToRoom` wakeup: rejected for the current first-entry observation because no `Animal.CallToRoom` log appeared.
- A failed AI template registration: rejected. Runtime logs show `shell_crab -> goat` and `hatch -> chicken` AI template mappings verified.

## Risk Assessment

No behavior fix is applied in this pass. The observed mismatch has not produced confirmed product, growth, feeding, sleep persistence, or morning wake regressions. A behavior patch at this stage would risk fighting the native state machine without proving which transition is late.

## Recommended Next Diagnostics

- Keep `Sleep/WakeUp/OnFell/CallToRoom` low-frequency logs.
- Keep only suspicious `OnRender` state-mismatch logs, but keep the new runtime animal identity/position/renderer-state fields so one Shell Crab can be followed across subsequent events in the same run.
- If the visual stand-up remains user-visible after log throttling, first compare `rendererState.animState` and `rendererState.sprite`; add a short-lived diagnostic around the native state-machine transition owner only if renderer-state evidence is ambiguous. Candidate owners are `AnimalAI/AnimalAIState.MakeDecision_Sleep`, `Goat_FreeTimeState.GetNextState`, `Chicken_FreeTimeState.GetNextState`, and `Normal_SleepState.GetNextState/OnExit`.

## 2026-06-30 Addendum

After a later user request to avoid narrow behavior fixes, a temporary force-sleep renderer stabilizer idea was discarded. The follow-up implementation is diagnostic-only: `AnimalRenderer.PlayAnimation("sleep")` records the immediate post-call renderer state for registered custom animals, and `AnimalRenderer.FixedUpdate` records one delayed state only for renderers marked by a suspicious sleeping `OnRender`. This is intended to prove whether the mismatch is a one-frame pooled sprite/Animator sampling artifact or a persistent idle-state problem before any behavior patch is considered.

## 2026-06-30 Root-Cause Recheck

The later manual clarification invalidates the "mostly first-frame artifact" interpretation for the full symptom. The player-visible issue can be sustained and may include short movement. Re-reading `Player-prev.log` plus native code shows a stronger root-cause chain:

- Runtime evidence: line 2400 of `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\Player-prev.log` records Hatch at 00:05 with `sleep=true`, `period=Night`, `aiState=Normal_SleepState`, and `task=DolocTown.AnimalMove`. The other three Shell Crabs in the same room-entry sample have `task=none`. This matches the player report that only one custom animal can remain visibly awake/moving.
- This is still not a wake call. The same repro window has no `Animal.WakeUp`, `AnimalRenderer.OnFell`, or `Animal.CallToRoom` source.
- Native chain: `Animal.__OnDayChanged()` calls `RefreshAI()` and recreates the `AnimalController`; `DecisionMaker.Update()` calls `OnUpdate(dt)` then immediately creates a task when `CurrentTask == null`; `RedSaw.AI.StateMachine.StateMachine.Update()` sets the default state on the first `currentState == null` tick and returns without running `GetNextState()`; template `Chicken_FreeTimeState` / `Goat_FreeTimeState` can therefore run `MakeDecision_FreeTime()` before the night/sleep guard transitions to `Normal_SleepState`.
- `MakeDecision_FreeTime()` has no `animal.isSleep` guard and can return `WanderEx()`. `WanderEx()` is random and often returns idle, but can return a journey/move task when pathing is available.
- State transition back to `Normal_SleepState` does not automatically break an existing task. `DecisionMaker.Update()` keeps executing `CurrentTask` until it succeeds, fails, or a breaker triggers.
- `Animal.Move()` has no `isSleep` guard and will call `Renderer.MoveTo(...)` plus `Renderer.PlayAnimation("move")` if an `AnimalMove` task executes.

Current classification: custom animal sleep flag and AI/current-task lifecycle can desynchronize after midnight/day-change/room-entry. Renderer pooled sprite state is a secondary visual amplifier, not the root owner of sustained stand-up/movement.

Rejected or narrowed:

- Pure PNG / AssetBundle animation issue: rejected because the same task chain applies to both Hatch PNG override and Shell Crab AssetBundle.
- Pure renderer first-frame artifact: rejected for the sustained/move symptom, though it remains true that same-tick `PlayAnimation("sleep")` sampling can show stale `idle`/sprite state.
- Native `WakeUp` source: rejected for the no-tool/no-call repro windows.

Recommended fix direction: do not patch renderer sleep as the primary fix. Evaluate a custom-species-only sleep/task gate that prevents FreeTime decisions from producing wander/move while `animal.isSleep == true` and current period is Night, or replaces/breaks custom-animal movement tasks while asleep. The hook point should be reviewed separately to avoid changing vanilla animals or normal morning `Normal_SleepState.OnExit -> WakeUp`.
