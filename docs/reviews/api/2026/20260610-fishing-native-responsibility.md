# Fishing Native Responsibility Review - 2026-06-10

## Scope

Public symbol/domain: `IFishingAutomationApi`, `FishingAutomationOptions`, `FishingAutomationState`, `Fishing.Automation`, `Fishing.MiniGameUpdate`, and AutoFishing smoke paths.

Current matrix status: `Experimental`.

Recommended status after this review: keep `Experimental`. Do not split `FishingAutomationFeature` in this branch. The current bridge reaches important native fishing owners, but the automation policy, F6 enablement, movement cancel, smoke force-fish gate, and animator restore sidecar remain DTMAPI-owned and too stateful for ordinary-mod stability.

This review is docs-only. It does not change runtime, public API, mods, game files, Workshop files, official DLLs, or reverse/decompiled reference material.

## Files Read

- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/DolocTownExperimentalBridgeApi.FishingAutomation.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/AutoFishingSmoke.cs`
- `testmods/AutoFishingMod/ModEntry.cs`
- `testmods/AutoFishingMod/README.md`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/reviews/api/2026/20260607-0005-native-responsibility-api-audit.md`
- `docs/reviews/api/2026/20260607-0006-native-responsibility-method-audit-index.md`
- `docs/reviews/api/2026/20260607-0007-native-responsibility-code-review-index.md`
- `docs/reviews/api/2026/20260607-0010-native-owner-remaining-api-audit/04-migrated-gameplay-apis.md`
- `docs/reviews/api/2026/20260610-0001-lifecycle-callback-isolation-review.md`
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Fishing.md`
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Action_Interaction.md`
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Input.md`

## Current DTMAPI Path

```text
AutoFishingMod
  -> helper.ModRegistry.GetApi<IFishingAutomationApi>("DTMAPI.GameBridge.DolocTown")
  -> Configure(owner, FishingAutomationOptions)
  -> ButtonPressed F6 / movement keys
  -> SetEnabled(owner, enabled, reason)
  -> DolocTownExperimentalBridgeApi sidecar state
  -> Harmony fishing phase/minigame callbacks
  -> native BodyController.UseFishRod / AgentStateFishingWait.RollFish / StateManager.Overwrite / FishingGameScrollBar.currentGameStatus
```

`DolocTownGameBridge` still registers `IFishingAutomationApi` through `DolocTownExperimentalBridgeApi`; there is no dedicated `FishingAutomationFeature`/service yet. This branch intentionally records the boundary only.

## Native Owner Map

| Responsibility | Native owner / state holder | Current DTMAPI relation | Review result |
| --- | --- | --- | --- |
| Fishing Ready phase | `AgentStateFishingReady.OnEnter/OnExit` and `AgentStateFishing` base state | DTMAPI observes `OnEnter` and records `Phase=Ready`; no direct native mutation in Ready. | Observation hook; native-owned. |
| Cast phase | `AgentStateFishingCast.OnEnter/OnPlay` plus body/fishing-rod renderer animators | DTMAPI observes `OnEnter` and may raise animator speeds through sidecar snapshots when fast animations are enabled. | Mixed: observation plus DTMAPI animator intervention. |
| Wait phase | `AgentStateFishingWait.OnEnter/OnPlay`, internal wait fields, `RollFish`, body `FishingCache` | DTMAPI observes `OnEnter`, then `OnPlay` can call native `RollFish`, write wait fields, refresh renderer, charge energy, and overwrite state. | Intervention hook; native state reached but policy is DTMAPI-owned. |
| Native auto-cast | `BodyController.UseFishRod(ItemFishingRod)` and selected/backpack rod lookup | DTMAPI throttles auto-cast, checks normal/busy state, fishable water, selected/backpack rod, then invokes native `UseFishRod`. | Native method reached; trigger/policy remains DTMAPI-owned. |
| Minigame creation | `FishingGameScrollBar.StartGame/UpdateGame/StopGame` and `currentGameStatus` | DTMAPI observes start/stop; `UpdateGame` may set `currentGameStatus=Success` after visible-time gating when `AutoCompleteMiniGame` is enabled and `SkipMiniGame=false`. | Intervention hook; completion is not a stable ordinary-mod minigame API. |
| Pull phase | `AgentStateFishingPull.OnEnter/OnPlay/OnExit`, `IsFailed`, native catch/result path | DTMAPI observes `OnEnter`, can fast-forward wait to Pull when skip is enabled, and restores animator speeds on `OnExit`. | Mixed: native pull state reached; restore must remain lifecycle-safe. |
| Cooldown / exit boundary | `AgentStateFishingPull.OnExit`, shared agent-state exits, save/title boundaries | DTMAPI records `Phase=Cooldown` and restores experimental animator speeds through finally-equivalent lifecycle isolation; `SaveLoaded` and `ReturnedToTitle` also restore. | Cleanup boundary; DTMAPI-owned sidecar restore. |
| Movement cancel | DTMAPI input events in `AutoFishingMod`, not a native fishing state owner | The mod disables automation on movement/manual cancel keys when `StopOnManualMove` is enabled. | DTMAPI policy; not native proof. |
| Smoke force-fish gate | Smoke-only `ForceFishingFishForSmoke` loop before skip-false minigame evidence | Used only to make a native fish minigame reachable for regression proof. | Smoke helper only; not player behavior. |

## Observation Hooks Versus Intervention Hooks

Observation-oriented hooks:

- `AgentStateFishingReady.OnEnter` -> `NotifyFishingPhase("Ready")`
- `AgentStateFishingCast.OnEnter` -> `NotifyFishingPhase("Cast")`
- `AgentStateFishingWait.OnEnter` -> `NotifyFishingPhase("Wait")`
- `AgentStateFishingPull.OnEnter` -> `NotifyFishingPhase("Pull")`
- `FishingGameScrollBar.StartGame/StopGame` -> minigame phase tracking

Intervention-oriented hooks or callbacks:

- `AgentStateFishingWait.OnPlay` -> `ApplyFishingWaitAutomation(...)`
- `FishingGameScrollBar.UpdateGame` -> delayed `currentGameStatus=Success`
- `UpdateFishingAutoCast()` -> reflected native `BodyController.UseFishRod(...)`
- `TryApplyFishingAnimationSpeed(...)` -> body and fishing-rod renderer animator speed writes
- `AgentStateFishingPull.OnExit`, `AgentStateBase.OnExit`, `SaveLoaded`, `ReturnedToTitle` -> DTMAPI sidecar animator-speed restore

The future feature split should preserve this distinction. Observation hooks may become a shared lifecycle/status bridge; intervention hooks should remain behind explicit `FishingAutomationOptions` policy and safety checks.

## Current Evidence

- `GAME-SMOKE/20260608-080848`: mechanical feature split baseline for fishing automation code, with auto-cast, wait-phase `InstantBite`, skip-false minigame completion, movement cancel, clean exit, and no fatal popup.
- `GAME-SMOKE/20260609-170646`: animator restore evidence; logs include fast Pull animation application and `Experimental animator speeds restored reason=AgentStateFishingPull.OnExit restored=2`.
- `GAME-SMOKE/20260610-012052`: lifecycle callback isolation evidence; AutoFishing hotkey/input/movement cancel/phase/minigame-complete passed and `FishingPullExitPostfix` kept animator restore in a finally-equivalent path.
- `GAME-SMOKE/20260610-035752`: safe fallback wrapper regression for AutoFishing phase/minigame paths.

These evidence rows prove scoped AutoFishing behavior for the migrated official-local mod on local save slot 3. They do not prove a stable public fishing controller API for arbitrary mods.

## Unverified Points

- Multiple mods enabling `IFishingAutomationApi` at the same time and competing for wait/minigame/animation policy.
- Manual fishing with automation disabled immediately after a save/title restore boundary.
- Every fish/trash outcome route after forced and non-forced rolls.
- Native catch reward, collection, stamina/energy, and event side effects across skip and non-skip paths.
- Full disabled/re-enabled lifecycle after config changes, title return, reload, and game restart.
- UI/fishing camera behavior when fast animations and minigame auto-complete are active.
- Interaction with future ordinary fishing state-query APIs, if any are added.

## Future Fishing Feature Split Requirements

Before splitting a real `FishingAutomationFeature`, require:

- A dedicated feature/service/hook bridge that registers `IFishingAutomationApi` outside `DolocTownExperimentalBridgeApi`.
- No change to public API members, `Fishing.Automation`, `Fishing.MiniGameUpdate`, or smoke result fields in the mechanical split branch.
- Hook ownership that separates phase observation from intervention callbacks.
- Explicit lifecycle restore tests or smoke evidence for `AgentStateFishingPull.OnExit`, `AgentStateBase.OnExit`, `SaveLoaded`, and `ReturnedToTitle`.
- Third-save smokes covering F6 enable, movement cancel, native auto-cast, wait-phase `InstantBite`, skip=true Pull route, skip=false visible minigame completion, fast animation restore, clean exit, and no fatal popup.
- A policy decision for multiple enabled owners before the API is broadened beyond migrated DTMAPI mods.

## Ordinary Mod Usability

`IFishingAutomationApi` should remain documented as Experimental and intended for the migrated AutoFishing route. Ordinary mods should not treat it as a general fishing state machine, catch API, minigame API, or stable fish/reward mutation API.

