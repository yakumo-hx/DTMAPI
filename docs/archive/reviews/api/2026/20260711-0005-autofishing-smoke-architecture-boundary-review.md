# AutoFishing smoke 与架构边界审查

- Date: 2026-07-11 (Asia/Shanghai)
- Source: user checkpoint after the 30-minute inactive memory trend
- Scope: source-only smoke separation, architecture gates, and input contract documentation

## Runtime decision

- Stop all further `InactiveNoConsumer` memory runs.
- Do not extend the 30-minute run and do not run platform-layer baselines.
- Do not run active fishing, 100/500-fish, soak, or another game process in this task.
- Preserve the existing 30-minute evidence. Its conclusion is limited to closing the suspicion that an inactive AutoFishing consumer continuously retains DTMAPI-owned state.
- ISSUE-010 remains open. Active fishing-loop retention is still unverified.
- The recovered private-memory wave peaking near 4.97 GB is recorded as a common game/Unity/platform transient, not an AutoFishing leak.

## Confirmed smoke coupling

Production AutoFishing already acquires the internal first-party primitive session without creating `LegacyFishingAutomationService`. The smoke harness does not yet prove that boundary:

- `AutoFishingSmokeCase.cs` contains both primitive and legacy branches.
- primitive progress summaries add legacy counters and inspect `FishingAutomationFeature.Service`.
- primitive cleanup can therefore pass through implementation knowledge belonging to the compatibility island.
- the frozen compatibility API has unit coverage, but no independently named smoke case.

The next source boundary is:

```text
AutoFishingPrimitiveSmokeCase
  -> first-party product input
  -> internal primitives/session/runtime telemetry
  -> neutral result + cleanup assertions

LegacyFishingAutomationCompatibilitySmokeCase
  -> obsolete IFishingAutomationApi only
  -> legacy activation/disable/owner cleanup
  -> neutral result + cleanup assertions
```

The primitive case must not reference `IFishingAutomationApi`, `LegacyFishingAutomationService`, or `FishingAutomationFeature.Service`.

## Architecture gates

Add source-level gates for these durable rules:

1. AutoFishing product code does not reference Harmony, reflection, raw Doloc Town fishing state types, or the frozen legacy API.
2. DTMAPI Core contains no AutoFishing/Fishing product names.
3. `IFirstPartyFishingPrimitivesApi` remains internal and is friend-visible only to the first-party AutoFishing assembly and framework/test assemblies; ordinary mods cannot request it.
4. primitive session activation cannot construct or access the legacy service.
5. the legacy service constructor is owned by `FishingAutomationFeature`, and the activating call path is reachable only from the obsolete compatibility adapter's `SetEnabled(true)` flow.

## Input contract

The owner-bound Gameplay registration in AutoFishing is intentional and remains unchanged. Demand-local `DtmKeybindList.JustPressed(helper.Input)` is the default for ordinary mods such as Zoom because it avoids a persistent registration root. AutoFishing is the bounded exception: it owns exactly one persistent toggle registration because the product is always present and must not miss the first very short edge after a watch expires.

This is compatible with the SMAPI-like design goal: one shared sampled input frame, local keybind configuration objects by default, and an explicit owner-bound registration only where reliable first-edge delivery is part of the product contract.

## Rejected work

- no local-query migration for AutoFishing F6;
- no new runtime memory instrumentation;
- no platform-layer baseline;
- no legacy deletion, version/manifest changes, or formal release;
- no active fishing validation before the release checkpoint.

## Completion decision

- Source separation is complete: primitive smoke has no legacy facade/service/feature-service dependency, and compatibility smoke owns the old API path independently.
- Static gates make the product/Core/internal API/activation/input distinction durable.
- Release and complete unit validation passed; no runtime evidence was requested or produced.
