# Shell Crab Sleep AI Template Review

Date: 2026-06-29

Scope: slot 7 Shell Crab prototype, DTMAPI custom animal animator bridge follow-up, native AnimalAI sleep exit.

## User Feedback

1. 第七档，小螃蟹过夜，sleep没有变成正常的其他状态。检查是动画机、AI还是DTMAPI问题。

   Analysis: This is not primarily an AnimatorController frame/state problem. The Shell Crab child/adult controllers include the expected shared animal state names, and native animal rendering directly calls `Animator.Play("sleep")`, `Animator.Play("idle")`, `Animator.Play("move")`, and related names. The root is AI template routing. Shell Crab keeps `schedule_id: goat`, so native `AnimalAI.Create(animal.proto.ScheduleId, animal)` builds the goat state machine. However `AnimalAIState` initializes `defaultAnyState` by calling `GetDefaultAnyState(animal.protoName)`. For `shell_crab`, vanilla code falls through to `Normal_FreeTimeState`; that state is not present in the goat machine. When `Normal_SleepState.GetNextState()` sees sleep time has ended, it returns this missing/null default state, the state machine does not transition, `Normal_SleepState.OnExit()` does not run, and `Animal.WakeUp()` is not called. DTMAPI therefore needs an internal custom-animal AI template bridge for enabled content packs: `shell_crab -> goat` should resolve to `AnimalAI+Goat_FreeTimeState` while unknown native species remain untouched.

## Decision

Implement a narrow GameBridge-owned bridge for `DolocTown.AnimalAI.GetDefaultAnyState(string)`:

- Read `aiTemplate` from enabled DTMAPI ContentPack `Content/DTMAPI/custom-animals.json`.
- Register only custom species mappings such as `shell_crab -> goat`.
- Harmony-postfix the private static native default-state resolver.
- Replace the returned state type only when the queried species is registered.
- Keep `schedule_id: goat`, Shell Crab animator URLs, goat/chicken native controllers, template `AnimalInfo`, renderer sprites, and live entities untouched.

## Validation

- Source validation: `tools/scripts/test.ps1 -Configuration Release` passed with `DTMAPI.UnitTests: OK`.
- Runtime validation: slot 7 smoke `GAME-SMOKE/20260629-164028` passed after runtime-locked install. Result fields include `RunStatus=Passed`, `HookProbe=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`.
- Runtime log evidence: `CustomAnimals.AiTemplateBridge = verified`, `CustomAnimals.AiTemplateBridge.shell_crab = verified`, and `Mapped custom animal species=shell_crab to aiTemplate=goat stateType=DolocTown.AnimalAI+Goat_FreeTimeState`.
- Manual overnight validation pending: the user still needs to observe a young Shell Crab exiting sleep after overnight time passes.

## Related Records

- Update: `docs/updates/2026/20260629-0001-shell-crab-ai-template-wake.md`
- Prior visual bridge update: `docs/updates/2026/20260628-0003-shell-crab-animator-bridge.md`
- Hook map: `CustomAnimals.AiTemplateBridge`
- Regression row: `SHELL-CRAB-AI-TEMPLATE-WAKE-20260629`
