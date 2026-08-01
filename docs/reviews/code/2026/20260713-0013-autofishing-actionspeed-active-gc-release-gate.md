# 20260713-0013 AutoFishing And ActionSpeed Active-Gameplay GC Release Gate

Status: recorded / implementation and runtime evidence pending / ISSUE-010 open
Date: 2026-07-13
Scope: DTMAPI 0.5.5 active-gameplay GC priority, AutoFishing and ActionSpeed parallel speed ladders, evidence boundary and release claims
Related Update: `docs/updates/2026/20260713-0007-sixth-round-closure-and-active-gc-gate.md`
Direction Refinement Update: `docs/updates/2026/20260713-0008-autofishing-actionspeed-parallel-gc-direction.md`
Related Issue: `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
Decision Sources: `D:/下载/第六轮正式.md` and the user's subsequent GC direction correction

## Source Request

The user corrected the major-update GC priority: MoreEquipment is principally a product/save/recovery risk, while active AutoFishing and ActionSpeed/animation acceleration should receive the first gameplay investigation before DTMAPI 0.5.5. The formal sixth-round response initially proposed a joint A-H matrix.

The user then refined the direction: AutoFishing and ActionSpeed are parallel first-priority domains, not a presumed conflict pair. AutoFishing advances Fishing Ready/Cast/Pull; ActionSpeed advances Tool/Interact/Eat/Continuous-use. Their common risk is that faster animation time and action-state progression may independently amplify the same class of Unity/Mono GC pressure. Each domain must therefore receive the same speed ladder and per-action/per-minute comparison before any combined-conflict theory is considered.

This Review preserves that direction while separating player correlation from source/runtime proof. It does not declare either Mod a Fatal GC root cause, change code, install a package, run the game or close ISSUE-010.

## 1. Priority Correction

The selected 0.5.5 order is:

```text
first active-gameplay gate
  AutoFishing Fishing Ready/Cast/Pull speed ladder
  ActionSpeed Tool/Interact/Eat/Continuous-use speed ladder
  compare each domain independently at the same acceleration/lifecycle levels

later product gates
  every other Mod receives its own disabled/idle/active/title/long-run profile
  before that Mod's 1.0.0 release
```

Analysis immediately following issue 1: this is a release-risk priority, not causal attribution. The shared concern is a common pressure mechanism across separate gameplay domains, not competition for one Animator. MoreEquipment remains high risk for product semantics, save compatibility, orphan recovery and rollback, but current evidence does not make it the primary GC suspect.

## 2. Current Evidence Boundary

### Proven or currently accepted

- Unity/Mono `Unexpected mark stack overflow` is a real long-run crash class tracked by ISSUE-010.
- Known July Fatal profiles reproduced while AutoFishing was disabled. AutoFishing is therefore not necessary for all observed Fatal GC cases.
- The current `InactiveNoConsumer` 30-minute trend had zero fishing activity and zero AutoFishing domain-structure growth. The inactive sustained-retention suspicion is cleared for this build and should not receive another identical long run.
- AutoFishing previously had high-frequency diagnostics/reflection/temporary-allocation pressure. Demand activation, aggregation, native caches and lifecycle cleanup have corrected specific paths; historical defects must not be silently treated as current defects.
- AutoFishing has short soak, single-fish and title-cleanup evidence, but no accepted active 10-minute or 10-20-fish memory trend.
- ActionSpeed has functional and Animator-restore evidence. Its current action application still constructs temporary lists/arrays/strings/status summaries, and active AutoFill performs reflective queries; these are pressure candidates, not retained-leak proof.
- Current source routes AutoFishing animation control through fishing `Ready`, `Cast` and `Pull`. ActionSpeed targets `Tool`, `Interact`, `Eat`, `UseItemContinues` and `InteractContinues`, and its accelerated-tool filter does not include a fishing rod.
- Both domains may traverse common native lifecycle infrastructure, but current source does not prove simultaneous writes to the same Animator. Shared infrastructure is not evidence of an owner conflict.
- Unity Mono's current-thread allocation counter is nonfunctional in the current environment. Historical zero-byte readings are invalid and future unavailable values must be `Blocked`/`null`, not zero.

### Not proven

- AutoFishing or ActionSpeed as the sole or necessary cause of the global Fatal GC class;
- a normal-flow double owner of the same fishing Animator;
- a per-fish or per-action retained leak;
- that any short run without a Fatal closes a long-window intermittent issue;
- that DTMAPI-owned bounded roots alone prove Unity native state is bounded.

Analysis immediately following issue 2: ISSUE-010 now has two parallel evidence tracks which must not overwrite one another. The title-idle/LoadGame track has historical Fatal evidence with AutoFishing disabled; after the input-pressure mitigation, the tested one-hour-title plus ten-save-load `FullKnown` route passed and is accepted as the current title-idle baseline, while recurrence and broader native room/terrain/dungeon loading remain tracked. The active-gameplay track is prioritized by severity, long-session field correlation and release value, not by prevalence: the user characterizes reports as affecting a minority of players, and controlled single-variable evidence is still required.

## 3. Corrected Common-Pressure Hypotheses

The `GC-H*` prefix avoids collision with the earlier H1 release-policy decision.

### GC-H1 - action throughput amplifies short-lived allocation

Faster animation/state progression may increase completed casts, reels, tool actions, interactions, state transitions, diagnostics and events per real minute while keeping cost per action bounded. Compare both per real minute and per completed domain action so a throughput increase is not mislabeled as a leak.

### GC-H2 - animation events and completion callbacks allocate more work

Each accelerated animation event, completion callback, state handoff, Tween/Coroutine or result publication may create short-lived managed/native work. The ladder must determine whether per-action cost stays stable while per-minute cost rises, or whether high speed also changes the per-action cost.

### GC-H3 - small per-action residual is magnified by acceleration

Each fish/action may leave a small callback, context, lease, handler, Animator snapshot, Unity object or native/managed reference. Normal speed may hide the slope; common/high multipliers may reveal a staircase. Exactly-once completion counters remain part of this test, but historical AutoFishing defects are not presumed current.

### GC-H4 - high-speed Animator/native state transitions create Unity/Mono pressure

Even when DTMAPI-owned references return to baseline, rapid Animator events and native state enter/exit cycles may stress Unity native objects, Mono root registration or engine-side allocation paths. This must be tested independently in Fishing and Tool/Interact/Eat/Continuous-use workloads.

### Conditional owner-overlap branch

Present source does not show ActionSpeed targeting fishing states. Same-Animator double ownership is therefore not a standing GC hypothesis. Instrumentation may record owner identity, but an arbitration design is opened only if a real overlapping Animator/state is observed or a future product scope creates one.

Analysis immediately following issue 3: the four primary questions now match the user's requested evidence order—throughput, event/callback allocation, per-action residual and high-speed native/Mono pressure. “Two systems conflict” is explicitly excluded from the default explanation.

## 4. Parallel Per-Domain Speed Ladders

Run the same ladder independently for AutoFishing and ActionSpeed. Do not alternate the two products inside the primary classification run.

| Level | AutoFishing workload | ActionSpeed workload | Purpose |
| --- | --- | --- | --- |
| L0 - native 1x | AutoFishing off; fixed manual fishing workload at native timing | ActionSpeed off; fixed representative native action workload | Production-Runtime/native reference. |
| L1 - enabled, no acceleration | AutoFishing active with FastAnimation off | ActionSpeed owner/features active at effective multiplier 1 | Separate product/automation cost from speed amplification. |
| L2 - common acceleration | AutoFishing active at the reviewed common player multiplier | Each ActionSpeed state family at its reviewed common player multiplier | Measure ordinary accelerated use. Record the exact multiplier; do not infer it from labels. |
| L3 - high multiplier | AutoFishing at the current supported high multiplier | Each ActionSpeed state family at the current supported high multiplier | Stress fast Animator/state transitions without mixing gameplay domains. |
| L4 - disable recovery | Disable after L2/L3, repeat a native action and observe cleanup | Disable after each representative L2/L3 action family, repeat native action and observe cleanup | Prove speed/state/owner restoration. |
| L5 - title cycle | L2/L3 -> title -> reload -> re-enable/disable -> exit | Repeat separately for representative ActionSpeed state families | Prove no old action/Animator/native state crosses title or process exit. |

For ActionSpeed, `Tool`, `Interact`, `Eat` and `Continuous-use` are separate subcases; a single tool loop cannot certify the whole product. For AutoFishing, automation itself may change completed actions per minute even at L1, which is why the native L0 and per-action denominator are both required.

After both independent ladders are classified, one ordinary installed-coexistence/lifecycle smoke may confirm that neither product changes the other's inactive cost. It is an integration check, not the main GC causal matrix and not evidence of an Animator conflict.

Every ladder level records all applicable denominators:

```text
per real minute
per fish / cast-reel cycle
per ActionSpeed action
```

Without both real-time and unit-work denominators, the result cannot distinguish throughput amplification from a growing per-cycle cost.

Controls must hold save, room, other-Mod profile, log level and warm-up constant. Trend measurement must not call `GC.Collect()` as a product workaround or use forced collection to manufacture a flat result.

## 5. Required Measurements

- Mono used/heap, Unity allocated/reserved, Windows private/working set and process Gen0/1/2;
- owner roots, Event/Input/API-facade roots, ResourceLifecycle records/snapshots/publications and Hook demand/status counts;
- action/animation-event/completion-callback counts and any measurable allocation associated with them;
- Fishing session, callback/context, animation lease/snapshot, Hook physics, pending cast, minigame handle, accepted reel and completion/reward counts;
- ActionSpeed owner options, Animator snapshots, pending animal marker, application/continuous/AutoFill counts, retained log-key cardinality and status-publication counts;
- completed fish and representative ActionSpeed action counts;
- safe Tween/Coroutine/Animator/Unity-object counts where the native owner allows observation without retaining or destroying unknown objects;
- current profile, final lifecycle boundary, clean process exit and Unity crash artifacts if a Fatal occurs.

Process memory may have warm-up waves and recoverable peaks. The gate evaluates stable/trailing low-water behavior and structural stair-steps; it does not require the final process byte count to equal startup.

## 6. Release Gates

### DTMAPI 0.5.5 RC gate

- no-consumer AutoFishing remains demand-inactive;
- no-consumer ActionSpeed has no dedicated every-frame work or dedicated Hook; a shared Hook required by another feature may remain, but the ActionSpeed callback must return without product work;
- both independent L0-L5 ladders show bounded DTMAPI-owned structural counts and truthful per-action/per-minute trends;
- unavailable allocation metrics are explicit `Blocked`/`null`;
- the chosen test windows have no Fatal; a Fatal requires a captured Unity crash, exact profile, per-unit counters and last lifecycle boundary;
- this is a Runtime integration gate and does not by itself publish either product as 1.0.0.

### AutoFishing 1.0.0 gate

- L1-L3 complete approximately ten minutes or 10-20 fish of active trend evidence, followed by L4-L5 cleanup;
- session, callback/context, lease, Animator snapshot, Hook physics and other transients return to baseline after each fish and at title;
- reel, energy, completion and reward effects remain exactly once;
- Fast toggle, disable, title and reload restore native state.

### ActionSpeed release gate

- L1-L3 cover representative Tool/Interact/Eat/Continuous-use native state families, followed by L4-L5 cleanup;
- Animator snapshots and pending markers return to zero after each action and at lifecycle boundaries;
- log keys, status publications, owner roots and application bookkeeping remain bounded by owners/action classes rather than action count;
- disabling or returning through title does not inherit or restore an incorrect speed inside any state family.

### Safe conclusion language

If the available evidence proves only DTMAPI-owned state and unit-work trends, use:

> AutoFishing 与 ActionSpeed 已分别完成各自玩法域的速度阶梯；其 DTMAPI-owned 状态、单位工作量趋势和生命周期通过本次门禁，测试窗口内未复现 Fatal GC。更广泛的 Unity/Mono 长期崩溃类别仍由 ISSUE-010 跟踪。

The user's stronger wording about DTMAPI-owned “allocation” may be used only after a working allocation/profiler route actually measures it. Never state that all Unity GC problems are solved.

## 7. Staged Per-Mod Certification

The selected staged strategy is:

```text
before DTMAPI 0.5.5
  production Runtime baseline
  AutoFishing and ActionSpeed parallel active-gameplay ladders
  CustomAnimals + Audio current hot paths
  known high-risk combinations
  common player profiles

before each Mod 1.0.0
  disabled
  idle
  active representative work
  title/reload cycles
  product-appropriate extended run
```

Source review identifies candidates; a complete conclusion needs controlled runtime evidence, lifecycle repetition, combination isolation and a product-appropriate long window.

## Evidence References

- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md` - canonical long-run symptom, Fatal profiles, inactive-AutoFishing clearance and issue state;
- `docs/reviews/code/2026/20260708-0001-autofishing-longplay-gc-research.md` - AutoFishing long-play source/evidence analysis;
- `docs/reviews/manual-qa/2026/20260710-0002-autofishing-input-gc-ready-review.md` - current player-facing AutoFishing/input/GC-ready feedback boundary;
- `docs/reviews/manual-qa/2026/20260616-0002-actionspeed-native-interaction-followup.md` - ActionSpeed native interaction and restore follow-up;
- `src/DTMAPI.GameBridge.DolocTown/Features/ActionSpeed/ActionSpeedHookBridge.cs` and `ActionSpeedService.cs` - current ActionSpeed state targets, Animator snapshots and cleanup;
- `first-party-mods/AutoFishingMod/README.md` plus current Fishing Primitives source - current Ready/Cast/Pull ownership and product policy.

## Validation Boundary

This Review cross-checked the formal sixth-round decision, current AutoFishing and ActionSpeed source targets/lifecycle fields, prior manual/code/API reviews, current ISSUE-010 evidence and the existing runtime-memory limitations. No build or game run was performed, no runtime lock was acquired, and no current Hook/API/product/package behavior changed.
