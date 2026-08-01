# Frozen Fishing Automation Compatibility Hook Map

## Boundary

- Public surface: frozen `IFishingAutomationApi` and its existing DTOs. The provider ID, signatures and owner behavior are unchanged.
- Physical implementation owner: dormant-shipped `DTMAPI.GameBridge.DolocTown.Compatibility.dll`; the default-loaded GameBridge holds only the typed proxy and receipt-validating broker.
- Harmony owner: `dtmapi.gamebridge.doloctown.fishingcompatibility`.
- Product exclusion owner: the exact Catalog-derived `Yuuka.DTMAPI.AutoFishing` Harmony owner only. An unrelated `dtmapi.mod.*` owner on a shared native target is not an AutoFishing collision.
- Activation: no consumer means no Host load and no compatibility Hook. The first valid frozen-ABI call loads the exact package component; enable installs the full inventory atomically, and any missing target or exact AutoFishing ownership fails closed.

## Native Inventory

The inventory is 22 patches across 21 exact methods: Postfixes on Fishing Ready `OnEnter`/`OnPlay`, Cast `OnEnter`, Wait `OnEnter`/`OnPlay`/`NextState`, minigame `StartGame`/`UpdateGame`/`StopGame`, rod `CastHook`/`Pull`/`PullCancel`, Pull `OnEnter`/`OnExit` and base-state `OnExit`; Prefixes on minigame `UpdateGame` plus the six normal-use/fishing input getters. Native types and parameter counts remain the tracked `FishingCompatibilityHookBridge.OwnedInventory` contract against `Assembly-CSharp`.

## Lifecycle And Failure Behavior

- Product-first and old-consumer-first orders are mutually exclusive against the exact AutoFishing owner. Hook installation is all-or-nothing.
- Disable, save/title/environment boundaries and native exits independently restore/clear input override, Ready holders, animator speed, gravity/velocity, Pull duration, minigame handles and pending-cast state before exact-owner unpatch.
- A loaded Host remains process-resident under Unity Mono. `resident` is not an unload claim; the required post-disable invariant is zero owner/demand/callback/Hook/native service state.
- The frozen compatibility path is independent of the ProductNative AutoFishing 22-Hook implementation and must not be cited as product behavior proof.

## Current Evidence

- Focused Host Unit verifies exact five-consumer/provider membership, no mandatory Host reference/heavy markers, first-call receipt validation, fail-closed errors, typed warmed calls and zero allocation over 10,000 ready probes.
- `GAME-SMOKE/20260723-083503` observed `Compatibility.Host=dormant; loaded=false; services=0; demand=0; callbacks=0; hooks=0`; the short runner itself was non-acceptance because generic terminal health publication was not reached.
- `GAME-SMOKE/20260723-084331` is superseded pre-review evidence: it passed dormant-to-resident activation and transient cleanup but did not inspect the process-resident backend owner dictionaries.
- Post-review `GAME-SMOKE/20260723-092718` passed third-save startup/HookProbe, dormant-to-resident first call, legacy configure/enable/disable, exact owner cleanup, enumerated transient state zero and direct resident-backend `ownerOptions=0; ownerStates=0`, followed by title/source/profile restoration and clean exit. `092433` preserves the exact ProductNative AutoFishing-first fail-closed branch; `083828` preserves the corrected over-broad shared-target collision diagnostic.
- Owning implementation lifecycle: [Update 20260722-0004](../../updates/2026/20260722-0004-five-product-baseline-correction.md). Consumer/topology input: [Review 20260722-0010](../../reviews/code/2026/20260722-0010-frozen-abi-consumer-and-compatibility-host-review.md).
