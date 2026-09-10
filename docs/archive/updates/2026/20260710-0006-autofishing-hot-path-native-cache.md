# 20260710-0006 AutoFishing Hot Path and Native Cache

## Status

- `source-and-unit-verified / runtime-matrix-pending / issue-010-open`
- No game process was launched and no 0/100/500-fish result is claimed by this update.

## Source Request

- Goal: `docs/goals/2026/20260710-0006-autofishing-hot-path-native-cache.md`
- Review: `docs/reviews/api/2026/20260710-0001-autofishing-hot-path-native-cache-review.md`
- User direction: keep Fishing Primitives first-party/internal, remove reflection and formatting from active fishing frames, preserve the single Gameplay toggle and independent product options, then scaffold a later isolated performance matrix.

## Changes

- Added `FishingNativeStateCache`; `NormalGameState.OnUpdate` refreshes it before Core native-frame dispatch, and its failure is isolated from ordinary Update.
- `FishingRuntimeSession.GetSnapshot()` now reads only session fields plus the cached scalar struct. It performs no pool scan, reflection, phase transition, diagnostic publication, or lifecycle mutation.
- Added fishing-local compiled typed accessors for private/static fields, properties, setters, and calls. Current `netstandard2.0` references do not expose a direct `System.Reflection.Emit.DynamicMethod` surface, so the implementation uses cached `Expression.Compile()` delegates without adding a new runtime package. Active frames never fall back to `FieldInfo.GetValue`, `PropertyInfo.GetValue`, or `MethodInfo.Invoke`; failed capabilities are latched unavailable and logged once.
- Added `FishingMiniGameNativeCache` for `currentGameStatus`, note spawner/current note/timing, `GetNote(int)`, native enum integers, and `Time.time`. Warmed minigame frames do not rebuild accessors, box enums, call enum `ToString()`, or allocate on the unit-test thread.
- `FishingAnimationController` now caches Ready `_castTimer`, `Progress`, `Tick(float)`, progress-bar writes, color transfer, and `Time.fixedDeltaTime`; warmed positive-charge Ready ticks no longer reflect, allocate argument arrays, or rebuild status strings.
- Added `FishingNativeTransactionCache` and routed first-party `RollFish`, bite field writes, `InvokeFishOnHookTip`, `NextState`, native skip flag restoration, and state-manager `Overwrite` through cached typed delegates. First-party bite/reel no longer calls the legacy service implementation.
- Cached the current native agent and selected rod. Cast uses the Hook-fed cache, a cold/environment-invalidated pool scan, and cached `UseFishRod`; ordinary snapshots never scan the scene.
- Replaced Ready Play, Wait Play, minigame Update prefix/postfix, native-frame cache refresh, and fishing input getter hot callbacks with direct fail-open `try/catch` routing. The native getter is allowed through when override code fails.
- Added a fixed-enum lifecycle publication gate. Repeated clean success increments observed/suppressed counters; boundaries, warnings/status changes, and a 30-second aggregate may format and publish. First-party phase/minigame paths no longer populate legacy minigame owner dictionaries or rebuild compatibility owner strings.
- Extracted `FishingInputOverride`, `FishingAnimationController`, `FishingHookRouter`, `FishingNativeStateCache`, `FishingNativeAdapter`, and `FishingAutomationCompatibilityAdapter`. `FishingAutomationService` no longer implements `IFishingAutomationApi`; the compatibility adapter is the only public Experimental implementation. First-party sessions keep legacy `fishingOptions` and `fishingStates` at zero and read charge/animation policy from the primitive session and scoped leases.
- Kept Fishing Primitives internal through existing `InternalsVisibleTo`; no public abstraction or stability promotion was added. Shared Harmony hooks remain install-on-first-consumer and are not unpatched by F6.
- Added a disabled performance probe and smoke route. Positive 100/500 targets force typed-frame toggle, InstantBite + SkipMiniGame + Fast x4 + charge 0, five-fish warm-up, current-thread allocation/Gen0 counters, log/snapshot/native-transient metrics, and ReturnedToTitle cleanup fields. Target 0 uses 60 seconds warm-up plus 600 seconds measurement and fails on any PullExit. Follow-up `20260711-0002` adds required behavioral calibration because method presence alone is insufficient on Unity Mono; unavailable or nonfunctional counters block and `GetTotalMemory` is not used as a substitute.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingNativeAccessors.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingNativeStateCache.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingMiniGameNativeCache.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingNativeTransactionCache.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingNativeAdapter.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingPrimitivesService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingRuntimeComponents.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingInputOverride.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAnimationController.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Compatibility/FishingAutomation/FishingCompatibilityController.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/AutoFishingSmokeCase.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `tools/scripts/run-game-smoke.ps1`
- `tools/scripts/README.md`
- `tests/DTMAPI.UnitTests/Program.cs`
- the linked goal/review/API/hook/smoke/debug records.

## Validation

- `dotnet build DTMAPI.sln -c Release`: passed with 0 warnings and 0 errors.
- `DOTNET_ROLL_FORWARD=Major dotnet run --project tests/DTMAPI.UnitTests/DTMAPI.UnitTests.csproj -c Release --no-build`: passed (`DTMAPI.UnitTests: OK`). The machine has .NET 6 and 9 but not .NET 8, so the net8 test runner used supported major roll-forward to .NET 9.
- Allocation micro-tests passed after warm-up:
  - On the .NET 8 unit-test runtime, 10,000 pure snapshots report zero thread allocation, unchanged sequence, and unchanged native-frame read count;
  - On the .NET 8 unit-test runtime, 10,000 minigame frame reads report zero thread allocation with no accessor rebuild/fallback;
  - On the .NET 8 unit-test runtime, 10,000 native bite transaction reads report zero thread allocation with no accessor rebuild/fallback;
  - On the .NET 8 unit-test runtime, 10,000 positive-charge Ready animation ticks report zero thread allocation after accessor warm-up;
  - On the .NET 8 unit-test runtime, the scheduler not-due loop reports zero thread allocation. These source microtests do not establish Unity Mono allocation counts.
- Simulated tests cover single-owner arbitration, Ready/Wait/Bite/MiniGame/Pull exit, animation/input lease restore, owner cleanup, disabled cleanup, SaveLoaded/ReturnedToTitle reset contracts, and one-publication-plus-999-suppression lifecycle behavior.
- PowerShell parser check for `tools/scripts/run-game-smoke.ps1`: passed.
- `git diff --check`: passed (only repository line-ending conversion warnings were printed); no runtime installation or smoke was run.

## Runtime Evidence

- None in this update by design.
- The existing player manual pass predates this refactor and is functional baseline only; it is not evidence for the new cache/accessor implementation.
- `auto-fishing-performance.json` support is source/unit verified but has not produced a current-build game result.

## Related Records

- Hook map: `docs/hook-map/README.md` / `Fishing.Automation` and native frame drain.
- Regression matrix: `AUTOFISHING-HOT-PATH-NATIVE-CACHE-20260710`.
- API matrix: internal Fishing Primitives and Experimental compatibility rows.
- Debug: `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md` remains open.

## Rollback

- Revert this update as one unit: cache refresh ordering, primitive transaction routing, compatibility ownership, and the tests are coupled.
- Do not selectively restore snapshot-time reflection or first-party calls into `FishingAutomationService`; that would reintroduce the measured boundary violation while leaving the new session assumptions in place.
- The performance probe is disabled by default and can be omitted from a runtime command without changing player behavior.

## Follow-up

- Under the shared runtime lock, run isolated fifth-save 0/100/500 processes and retain `auto-fishing-performance.json`, startup/game logs, clean exit, and returned-title zero evidence.
- Establish a baseline before defining numeric allocation/log thresholds. The hard gate remains no retained state or success-log growth proportional to fish count and zero DTMAPI fishing transients after title return.
- Keep ISSUE-010 open until the long gameplay/native GC class has appropriate runtime evidence.
