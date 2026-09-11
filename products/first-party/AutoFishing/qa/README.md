# AutoFishing QA boundary

This directory is the product-owned QA authority for AutoFishing. It is deliberately outside the production `src/` tree. The standard product project disables default compile items and declares `Compile Include="src/**/*.cs"`; `dtmapi.author.json` selects that project. The production compilation therefore excludes this directory, while the QA projects explicitly link the required files below.

## Historical Batch 5 fixtures

The sources under `contracts/`, `performance/`, and `fifth-save/` preserve the former Batch 5 fifth-save control and performance policies. Their `not-wired-after-product-rehome` status describes that retired live route only. They remain linked into `DTMAPI.QaUnitTests` for regression coverage and must not be cited as new live evidence or restored to the generic `QaScenarioController`.

The old no-demand receipt's `FishingNativeFrameRefreshes` field is also historical. The generic Runtime snapshot no longer exposes ProductNative fishing refresh work, so no replacement receipt may synthesize a zero-valued product counter.

## Live Batch 6 pilot

`batch6/` owns the single live G6 case `Batch6AutoFishingPilot`. Those product-owned sources are linked at compile time into the optional `DTMAPI.GameBridge.DolocTown.QA` assembly. This gives the optional QA participant a live coordinator without moving ProductNative policy into the generic G6 scenario controller or adding a static `Yuuka.DTMAPI.AutoFishing` assembly reference. The runner, not the product or compatibility driver, owns the real `F6` input transitions.

For L1-L5, the pilot reflects the Core-resident `Yuuka.DTMAPI.AutoFishing.ModEntry`, observes its private `enabled`, `updateSubscribed`, `session`, and `nativeRuntime` state, and explicitly calls `primitives.EnableQaObservation()` before reading QA counters. Ordinary player sessions therefore do not allocate or update those counters unless the optional QA observer attaches. Reflection is intentional: the QA DLL must prove the real managed product instance while retaining no static Product AssemblyRef. The primary `AdvancedProduct` driver and the L0 compatibility driver each require their complete, fixed 22-patch inventory; the pilot never learns a weaker expected count from its first observation. Package, entry DLL, manifest, and tracked reference-policy hashes come from the deployed receipt/provenance chain and must match the expected candidate identity.

L0 keeps the product package and Core-resident product instance absent. After the authoritative fifth-save `SaveLoaded`, it uses the separately owned `CompatibilityNativeControl` driver over the frozen `IFishingAutomationApi` to run a real native fishing loop. The evidence must identify the QA driver owner, advance from real compatibility native-exit and auto-cast counters through 5 warmup fish plus 10 measured fish, and then verify exact-owner disable/deactivation with zero service, callback, hook, and owner resources. L0 expected hashes identify the candidate under comparison, but actual package, entry, manifest, and policy hashes stay empty because no product package is loaded; expected hashes must never be copied into actual fields.

After the runner's L4 disable F6, the reflected ModEntry updater and session remain off. A separate `QaProductNativeRecovery` owner then acquires `primitives` by reflection, drives one real cast/bite/skip-reel/PullExited recovery loop through the product-owned native hooks, releases the session, and proves its owner/session/input/animation roots returned to zero. The primary result remains `driverKind=AdvancedProduct`; `recoveryDriverKind=QaProductNativeRecovery` identifies this additional bounded recovery window.

L4 and L5 additionally publish handshake progress through the dedicated handshake status. For L5, the QA participant requests ReturnHome and the second authoritative fifth-save load through its existing lifecycle/save-load authority; the external runner supplies only the requested real F6 enable/disable transitions. L5 must observe a post-reload fish and final cleanup before passing.

Every Batch 6 pilot save window now begins with a strict native preflight: `DolocAPI.SelectedItem` must be a real `ItemFishingRod`, the current room must exist, and an exception-intolerant Unity scene scan must find at least one `FishingPool`. The product-owned official-command adapter records energy/spirit before refill, after-command readback, bounded maintenance receipts, and measurement-end readback. Formal acceptance additionally requires valid process metrics, native progress of at least 5 warm-up plus 10 measured exits, receipt-verified package identity for L1-L5, and exact L0/L4 cleanup zeros.

`Batch6AutoFishingPilot.Formal=true` is the only authoritative runtime contract and requires exactly 600 measurement seconds, 30-second sampling, 5 warm-up fish, and 10 measured fish. Ad-hoc runs remain useful diagnostics but serialize `authoritative=false`, `authority=non-authoritative`, and terminal `status=NonAuthoritativeCompleted`; they cannot be mistaken for a formal Batch 6 pass.

Exact hook status IDs are:

- `Smoke.Batch6.AutoFishingPilot`
- `Smoke.Batch6.AutoFishingPilot.Handshake`
- `Smoke.Batch6.AutoFishingPilot.Cleanup`

The strict cumulative evidence is written beneath the Runtime evidence root at:

```text
AUTO-FISHING-PERF/<runId>/auto-fishing-performance.json
```

Per-stage JSON files are co-located with that result. `productAssemblyReferenced=false` means the optional QA assembly has no static Product AssemblyRef; it does not mean the L1-L5 Core instance or its verified package provenance was absent.

The frozen `IFishingAutomationApi` compatibility smoke remains in the generic GameBridge QA assembly because it verifies the compatibility boundary. That older smoke is distinct from the product-owned Batch 6 L0 coordinator and does not own AutoFishing ProductNative behavior.
