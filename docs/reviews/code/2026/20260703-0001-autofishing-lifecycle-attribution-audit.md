# 20260703-0001 AutoFishing Lifecycle Attribution Audit

## Scope

Reviewed `FishingAutomationService`, `FishingAutomationFeature`, `FishingAutomationHookBridge`, `AutoFishingMod`, the fifth-save AutoFishing smoke path, and the fourth-stage Hook/Event scheduler impact. This is an attribution audit, not an AutoFishing gameplay rewrite.

## Native-Key Collections

Must be transient and zero after destructive boundaries:

- `fishingMiniGameStartedAt`
- `fishingMiniGameBonusTaps`
- `fishingMiniGameInputStats`
- `fishingMiniGameCompletedHandles`
- `fishingReadyChargeAppliedStates`
- `fishingReadyChargeTargetStates`
- `fishingReadyChargeReleasedStates`
- `currentFishingReadyChargeState`
- `fishingMiniGameInputOverride`
- `originalAnimatorSpeeds`
- `originalHookGravityScales`
- pending auto-cast marker fields
- short auto-cast backoff after destructive boundaries

Allowed long-lived owner/config state:

- `fishingOptions`
- `fishingStates`
- `fishingHooksInstalled`
- cumulative counters and diagnostics used as process-level evidence, except reset-only smoke overrides.

## Boundary Findings

- `SaveLoaded` and `ReturnedToTitle` already call `ResetFishingRuntimeState`, which clears mini-game handles, ready-charge sets, input overrides, pending cast markers, smoke overrides, failure throttle dictionaries, diagnostic throttle dictionaries, and restores animator/rigidbody snapshots.
- `SetEnabled(false)` already calls `ClearFishingAutomationTransientState`, which clears DTMAPI-held native object keys and pending cast state while retaining owner policy/state.
- `FishingGameScrollBar.StopGame` removes the concrete mini-game handle and clears the input override. It is a mini-game handle boundary, not a full destructive boundary for PullExit-owned animator/physics snapshots.
- `AgentStateFishingPull.OnExit` already restores experimental animator/physics state; phase 4.5 adds a no-behavior-change lifecycle assertion after restore.
- Leaving the native `Ready` phase now releases DTMAPI's Ready charge state keys so a zero-charge/default run cannot carry Ready state handles through the rest of the fishing loop.
- The pending-cast watchdog releases the pending marker and enters a short backoff. This is not proof of a leak, but remains a diagnostic risk signal.
- `EnvironmentReset` should remain non-destructive: it is too close to `DolocAPI.SetEnvCamera` and other high-frequency refresh paths, so clearing AutoFishing there could cancel legitimate in-progress fishing. Save/load/title/native exits own cleanup instead.

## AutoFishingMod Owner Review

- Input registrations are owner/config state keyed by string, not native object handles.
- `ReturnedToTitle` disables automation through the existing API path and therefore reaches transient cleanup.
- Config save normalizes and persists owner preferences, then calls `ConfigureBridge`; this retains owner config but does not retain native objects.
- The config path passes `CastChargeRatio` through to `FishingAutomationOptions`; source and smoke harness defaults use `0`.

## Stage 4 Hook/Event Impact

- AutoFishing remains a feature Hook group. The fourth-stage scheduler changes when Hook status/log events are published, not which fishing Harmony targets are patched.
- `Fishing.Automation` readiness semantics remain experimental. New `Fishing.Automation.Lifecycle` diagnostics are internal/report-only and do not become mod API.

## Charge-Ratio Check

Source does not show a hard-coded nonzero cast charge:

- `AutoFishingMod` passes `config.CastChargeRatio`.
- `run-game-smoke.ps1` defaults `-AutoFishingCastChargeRatio 0` and writes that to `Yuuka.DTMAPI.AutoFishing.json`.
- `TryOverrideFishingReadyChargeInput` releases immediately when target is `0`.
- Local runtime config `D:\Steam\steamapps\common\Doloc Town\DTMAPI\config\Yuuka.DTMAPI.AutoFishing.json` had `CastChargeRatio=0`.
- No matching `Yuuka.DTMAPI.AutoFishing.json` was found in the local Steam Workshop subscription cache for app `2285550`.

Remaining plausible explanation: with fast animations enabled, Ready-phase `_castTimer` can tick before the release override is observed, so a tiny visible charge may appear even though the configured target is zero. Phase 4.5 exposes `charge=...`, `fast=...`, and multiplier in the lifecycle summary so future evidence can distinguish stale config/package state from a real code behavior problem.

## ISSUE-010 Attribution

AutoFishing remains a reasonable suspect only in the narrow sense that it owns native-object-keyed transient state and long-loop automation. Current evidence does not prove it caused the long title-idle `Fatal error in GC / Unexpected mark stack overflow`:

- The latest reproducible long-idle failures occur during title idle and post-idle save load, before active fishing gameplay.
- Stage 3/4 evidence found no title-idle registry, Hook, WAV, custom-animal, or Hook/Event queue growth loop.
- AutoFishing is not active at title unless enabled after entering gameplay.

The next useful evidence is a fifth-save short soak with lifecycle summary, not a full matrix or gameplay rewrite.

## Runtime Evidence

Fifth-save short soak `GAME-SMOKE/20260703-194001` passed:

- `RunStatus=Passed`
- `AutoFishingPhase=Passed`
- `AutoFishingMiniGameComplete=Passed`
- `AutoFishingLifecycle=Passed`
- `AutoFishingSoak=Passed`
- `ProcessExited=Passed`
- `NoFatalInstanceWindow=Passed`

The lifecycle summary reported `charge=0;fast=False;mult=3`, `nativeTransientHandles=0`, `boundaryClearCount=0`, `miniGameHandles=0`, `readyChargeStates=0`, `animators=0`, `hookPhysics=0`, and `pendingCast=False` after three requested soak loops.

Conclusion: AutoFishing should remain on the watch list for native-key lifecycle regressions, but this evidence does not justify treating it as the current ISSUE-010 Fatal GC cause or starting a full FSM rewrite immediately.
