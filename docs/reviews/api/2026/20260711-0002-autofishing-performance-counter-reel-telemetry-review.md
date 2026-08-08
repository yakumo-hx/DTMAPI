# AutoFishing Performance Counter and Visible-Reel Telemetry Review - 2026-07-11

## Decision

Treat the two Unity Mono `AllocatedBytes=0` values as invalid until the counter proves that a known same-thread allocation advances it. Persist visible-reel counters in `FishingPrimitivesService` across runtime detach. Keep the current 500 ms post-consumption rearm behavior unchanged and record its native-side-effect risk for a future injected test.

## 1. Unity Mono Allocation Counter Can Be a Nonfunctional Stub

### User evidence

- Both 600-second baselines report `AllocationCounterAvailable=true`, `AllocatedBytes=0`, and nine Gen0 collections.
- The probe currently treats method presence as counter availability and performs no behavioral calibration.

### Code and evidence facts

- `FishingPerformanceProbe.ResolveAllocatedBytesGetter()` accepts any public static `GC.GetAllocatedBytesForCurrentThread(): long` method.
- `Observe()` subtracts two calls and publishes zero as a valid measurement.
- Historical evidence remains immutable under `GAME-SMOKE/20260711-012851` and `GAME-SMOKE/20260711-024427`.

### Ownership and conclusion

- This is a GameBridge smoke/performance-probe validity bug, not evidence that AutoFishing allocates zero bytes on Unity Mono.
- The existing 0 cast/fish, stable accessor, no Fishing hot-log, native movement, and title-cleanup fields remain useful; only the thread-allocation conclusion is withdrawn. Nine Gen0 collections show process/runtime collection activity but do not identify the allocating thread or byte count.

### Acceptance

- At measurement start on the same thread, allocate and keep alive a 4096-byte array between counter reads.
- A delta below 4096 is `blocked-allocation-counter-nonfunctional`; invocation failure is `blocked-allocation-counter-error`.
- Calibration happens before allocation and Gen0 baselines. Blocked results serialize allocation/Gen0 measurement fields as null and the outer smoke result as Blocked, not Failed.

## 2. Visible-Reel Counters Are Lost With the Primitive Hook Runtime

### User evidence

- Queued/consumed/retry/timeout counters live only in `FishingPrimitiveHookRuntime`.
- After release, `FishingPrimitivesService.FormatDiagnosticsSummary()` reads a null runtime and emits empty counter values.

### Code facts

- Primitive release resets and detaches the hook runtime before the final service diagnostics string is built.
- `FishingVisibleReelInputState.Clear()` clears pending state but intentionally preserves its counters, so detach can snapshot them without changing input behavior.

### Ownership and conclusion

- Process-lifetime diagnostics belong in `FishingPrimitivesService`; the runtime should contribute a final snapshot exactly once when detached.
- Reads must return archived totals plus the active runtime's current counters so values remain monotonic during a session and after title return.

### Acceptance

- Release, owner cleanup, title return, throwing transition subscriber, and activation rollback cannot lose or double-count telemetry.
- Diagnostics always print numeric queued/consumed/retry/timeout values.
- Performance JSON records start/end/delta for all four counters; InactiveNoConsumer remains all zero.

## 3. Post-Consumption 500 ms Rearm Can Repeat a Native Edge

### User evidence

- `FishingVisibleReelInputState.OnNativeFrame()` rearms after 500 ms without phase confirmation.
- Current three-fish evidence did not reproduce duplication because the native state normally transitions synchronously and phase hooks clear pending first.

### Native-owner fact

- Build `23762374_public_C416D4` `AgentStateFishingWait.NextState()` deducts fishing energy when `NormalUseTool`, `NormalUseItem`, or `NormalFishing` is true, then returns Battle or Pull. Input-getter consumption is not proof that the returned state was committed.

### Current decision

- Do not change the retry timing or force native state in this goal.
- A future fault-injection test must delay phase confirmation while counting the native reel/state-transition side effect and energy delta. It must prove one consumed logical reel request cannot execute that native effect twice; getter-consumption counters alone are insufficient.

## Validation Boundary

- Automatic: injected allocation-counter states, nullable JSON, cumulative runtime telemetry, no double archive, Release/full units/parser/version/diff checks.
- Runtime: one fifth-save natural visible-minigame Mono gate and one short InactiveNoConsumer calibration gate.
- No replacement ten-minute baseline, 100/500 fish, soak, public API change, version bump, or AutoFishing behavior change.

## Implementation Result

- P1 confirmed at runtime: `GAME-SMOKE/20260711-070116` resolves the method but the kept-alive 4096-byte allocation leaves the counter at `0 -> 0`. The result is now `blocked-allocation-counter-nonfunctional`, allocation/Gen0 interval fields are null, outer smoke is Blocked, and title/process cleanup passes.
- P2 fixed and runtime-verified: `GAME-SMOKE/20260711-065929` retains queued=1 and consumed=1 after primitive runtime release, with retries=0, timeouts=0, numeric lifecycle diagnostics, and every title cleanup root zero.
- P3 remains deliberately unchanged. The native energy/reel side-effect test remains a future fault-injection requirement and is not claimed by the counter work.
- Release/full units/parser/version checks pass. No ten-minute, 100/500, soak, public API, version, or release action occurred.
