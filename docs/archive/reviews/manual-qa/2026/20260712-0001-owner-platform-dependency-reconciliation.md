# 20260712-0001 Owner Platform Dependency Reconciliation Review

Status: recorded
Date: 2026-07-12
Scope: manual-QA regression review for ordinary Mod dependency reconciliation after the common owner-lifetime refactor
Related Update: `docs/updates/2026/20260712-0001-owner-platform-dependency-reconciliation.md`
Related Issue: `docs/debug/issues/ISSUE-013-20260712-owner-platform-dependency-reconciliation.md`

## Source Feedback

The user reported `mod一个都没有识别到，查一下是什么原因。` and attached a title-settings screenshot. The screenshot showed only the developer ConfigMenu example on the Config tab. The matching runtime log, inspected separately, showed that the same run first discovered 29 packages and loaded six code Mods, then a later title/Workshop refresh deactivated three already-entered Mods even though their DTMAPI platform dependencies were still present.

This record preserves the feedback as a pre-implementation review. It does not claim a fix or validation result.

## Issue 1 - Platform-dependent owners are deactivated after a source refresh

### Screenshot transcription

The screenshot-visible state was transcribed as:

- the DTMAPI Settings window reported version `0.5.3-alpha`;
- the active tab was `配置`, not the `Mod` inventory tab;
- under `已启用的 DTMAPI Mod`, the only visible page was `配置菜单示例（开发者） (DTMAPI.ConfigMenuExample)`;
- no discovery count, loaded count, or deactivated owner ID was visible in the screenshot itself.

No raw screenshot file was archived in the repository. The `29 -> 6 -> 3` counts and affected owner IDs below come from the same run's local `latest.log`, not from screenshot-only inference.

### User-confirmed symptom

The user confirmed the player-visible symptom that the Config tab appeared to recognize none of the expected product Mods. Log and code inspection then established that three product Mods had completed Entry before a later reconciliation pass treated their still-loaded platform services as missing dependencies. This is not an acceptable SMAPI-like owner boundary: a refresh with unchanged provider availability must not convert an active owner to restart-required.

### Runtime observations

The failing run records:

- `rows=29; loadedRows=6; ... dependencyErrors=0; dependencyWarnings=0` after initial load;
- `Mod Entry completed` for all three affected owners;
- at `2026-07-12 00:03:18`, required-dependency invalidation for:
  - `DTMAPI.DebugConsoleMod -> DTMAPI.DebugConsoleHost`;
  - `DTMAPI.ZoomMod -> DTMAPI.GameBridge.DolocTown`;
  - `Yuuka.DTMAPI.AutoFishing -> DTMAPI.ModConfigMenu`;
- owner status later reporting those three IDs, `recentFailures=3`, `needsRestart=3`, and `cleanupFailures=0`;
- subsequent registry snapshots retaining `rows=29` while falling to `loadedRows=3`.

The surviving loaded owners were the three without those required platform-provider declarations in this fixture: `DTMAPI.ConfigMenuExample`, `DTMAPI.HelloDtmMod`, and `DTMAPI.HookProbeMod`.

### Code and manifest facts

- `DTMAPI.DebugConsoleMod` declares required dependencies on `DTMAPI.DebugConsoleHost` and `DTMAPI.GameBridge.DolocTown`.
- `DTMAPI.ZoomMod` declares a required dependency on `DTMAPI.GameBridge.DolocTown` and an optional dependency on `DTMAPI.ModConfigMenu`.
- `Yuuka.DTMAPI.AutoFishing` declares required dependencies on `DTMAPI.ModConfigMenu` and `DTMAPI.GameBridge.DolocTown`.
- `DTMAPI.ConfigMenuExample` declares no corresponding dependency, which explains why it is not a counterexample to the observed split.
- Core, ConfigMenu, GameBridge, and bootstrap-host APIs are registered as process-lifetime providers in the authoritative Mod registry. They are not ordinary package sources in the refreshed discovery map.
- The reviewed reconciliation path required a dependency to be both present in the loaded registry and present/enabled in the ordinary `discoveredById` map. That second condition cannot be satisfied by registry-only platform providers.

### Root cause

The dependency reconciler conflated two provider classes:

1. source-managed ordinary Mods, whose removal, disable, or refreshed version must participate in dependency cascades; and
2. process-lifetime platform providers, whose availability and version are authoritative in the runtime Mod registry and which intentionally have no ordinary discovery row.

Initial dependency validation accepted the registered platform providers, so all six Entries committed. The later source reconciliation added an ordinary-source-presence requirement and therefore manufactured a dependency loss for three consumers. The unified owner cleanup then correctly acted on that incorrect decision.

### Rejected hypotheses

- Not an Entry or transaction rollback failure: each affected owner committed once before the refresh.
- Not an initial manifest/dependency validation failure: the initial registry reported zero dependency errors and warnings.
- Not an actual source removal or official disable: discovery stayed at 29 rows.
- Not a platform-provider unload: the providers are process-lifetime registry services, outside ordinary source discovery.
- Not an owner cleanup failure: cleanup reported zero failures and removed the affected platform roots.
- Not a Camera lease bug: the Zoom lease release was a downstream consequence of erroneous owner deactivation.
- Not a title/save policy requiring service cleanup: title/save boundaries must preserve committed process-lifetime Mod services.

### Required correction boundary

- Classify provider availability before reconciliation.
- For a process-lifetime platform provider, use the authoritative loaded registry row and its version; do not require an ordinary discovery row.
- For a source-managed provider, continue to require a present, enabled source and cascade true required-provider removal, disable, or version mismatch in reverse dependency order.
- Keep optional dependency changes warning-only.
- If a required platform provider is genuinely absent from the authoritative registry or has an incompatible version, deactivate the consumer through the existing owner cleanup path.
- Preserve same-process inactive/restart-required semantics for a real owner disable; do not re-run Entry.
- Keep public API signatures, `0.5.3-alpha`, Hook targets, native state, and content formats unchanged.

### Acceptance criteria

- A consumer of registered process-lifetime ConfigMenu, GameBridge, and DebugConsoleHost providers remains active across ReturnedToTitle and repeated Workshop/source refreshes, with Entry count still one and unchanged owner-root counts.
- The reproduced fixture remains `29 discovered / 6 loaded`, and the three affected owners do not enter `needsRestart`, when provider state has not changed.
- Removing a required platform provider from the authoritative registry, or lowering its registry version below the manifest minimum, deactivates the consumer and leaves zero owner roots.
- Removing, disabling, or downgrading a source-managed required provider still cascades to required dependants in reverse dependency order; optional dependants remain active with a bounded warning.
- Zoom's Camera lease remains live across title/save preservation and is released only when its owner is truly deactivated.
- A true same-process disable of a successfully loaded ordinary code Mod still leaves zero Event, Input, API, ConfigPage, migration, Content, and GameBridge roots, requires restart, and never repeats Entry.
- Release source/unit checks and a locked third-save game smoke pass before the issue is closed.

## Issue 2 - A duplicate-source identity handoff can keep the old assembly/root active

### Code-review fact

The same reconciliation pass originally compared an active owner only by `UniqueID` and current enabled state. If duplicate-source arbitration changed the selected package from one `Source + OfficialId + RootPath` identity to another with the same `UniqueID`, the old loaded `DiscoveredMod`, assembly, instance, and Content root could remain active even though diagnostics/discovery selected the replacement source.

### Risk and required boundary

Unity Mono cannot safely replace the loaded code assembly in-process. A selected source-identity change must therefore use unified owner deactivation, remove the old Content/platform roots, enter `InactiveRestartRequired`, publish neither old nor replacement active Content in that process, and load the selected replacement only in a clean runtime. In-place version-file changes retain the already-recorded `20260711-0010` version/dependency behavior; this finding is specifically about a changed source identity.

### Acceptance criteria

- an enabled OfficialLocal-to-Workshop duplicate handoff changes discovery to the new source but leaves no active owner/root or active Content in the current process;
- Entry remains once and repeated refresh is idempotent;
- a clean runtime loads only the newly selected source and executes its Entry once for that runtime.

## Issue 3 - SaveLoaded does not clear Core transient Input state

### Code-review fact

`ReturnedToTitle` cleared Core down/pressed/released/suppression state and demand-local snapshot watches, while `SaveLoaded` only invoked Bootstrap reflected-input cleanup. Persistent owner registrations correctly survived both, but Core-local transient state could cross a save transition despite the common owner contract requiring both boundaries to clear transient state.

### Required boundary and acceptance criteria

`SaveLoaded` must clear Core transient edges/down state and demand-local watches before ordinary SaveLoaded callbacks, while preserving persistent Input registrations plus Event, API/facade, ConfigPage, migration, Content, and Camera services. The same transient watches may be recreated by later gameplay queries without duplicating persistent registrations.

## Issue 4 - A logging sink exception can interrupt authoritative reconciliation

### Code-review fact

In the reviewed baseline, `FileMonitor.Log` synchronously created/appended the file and then called the host log sink. File IO, host logging, and the error-diagnostic callback could all throw. `ReconcileActiveOwners`, source discovery, activation, and deactivation call the monitor from correctness-critical paths without a general isolation boundary. In particular, a warning emitted after an owner was selected for deactivation could throw before the reverse-order cleanup loop ran, leaving discovery and active-owner state split.

Isolating only the final `Mod Entry completed` message is insufficient. Logging is an observation surface, so none of its sinks may decide whether publication, reconciliation, cleanup, or shutdown completes.

### Required boundary

- File append, host logging, and diagnostic recording must be attempted independently and must not propagate to runtime correctness paths.
- A sink failure may publish at most a bounded scalar diagnostic; failure reporting must not recursively depend on the failed sink.
- A successful owner transaction remains successful if its completion log fails.
- A real disable/dependency cascade and every cleanup participant still run when all configured log sinks throw.

### Acceptance criteria

- inject simultaneous file-path, host-log, and diagnostic-callback failures and prove initial activation still commits;
- under the same failures, disable a loaded source provider and prove required dependants deactivate in reverse order, all authoritative roots reach zero, and successfully loaded assemblies remain restart-required;
- no logging exception escapes the loader/reconciler, changes Entry count, or creates an unbounded retry/error graph.

## Issue 5 - Process-lifetime provider manifest and API publication is not atomic

### Code-review fact

The reviewed runtime provider path first published the loaded manifest and then registered the API contract. If API registration failed, a provider could remain visible through `Get/IsLoaded` without its promised contract. `AddLoaded` also replaced rows by `UniqueID`, so a later runtime registration could change the canonical provider manifest independently of previously published API contracts.

The reserved-owner check used a fixed list. A process provider registered under a valid dynamic ID was therefore authoritative for consumers but did not automatically reserve that ID against a discovered ordinary source.

### Required boundary

- Publish a process provider's canonical manifest and each API contract as one transaction: first-contract failure leaves neither row; later-contract failure preserves the existing canonical manifest and earlier contracts.
- The same canonical manifest may publish distinct contracts, but duplicate `(owner, contract)` and a different manifest for the same owner must throw without replacement.
- Canonical identity includes at least owner ID, name, author, version, and type for this internal registration boundary.
- A successfully published dynamic process-provider ID becomes reserved immediately and cannot be claimed, loaded, or cleaned by an ordinary discovered source.
- Dependency reconciliation must use the canonical registry manifest/version and must not manufacture an ordinary-source requirement for that dynamic provider.

### Acceptance criteria

- injected duplicate/factory/registration failures never expose a manifest-without-contract or replace an earlier object;
- one canonical owner can publish two different API contracts while a conflicting canonical manifest is rejected atomically;
- an ordinary package using the dynamic provider ID is rejected before `Assembly.LoadFrom`, while an ordinary consumer of that provider loads once and survives repeated refresh;
- logging/diagnostic failure after commit cannot roll back or partially expose the provider.

## Issue 6 - No-assembly and non-code owners are incorrectly made permanently restart-required

### Code-review fact

The reviewed lifecycle coordinator completed every non-shutdown deactivation as `InactiveRestartRequired`. It did not distinguish a successfully loaded Mono assembly from a non-code owner, a missing DLL, or an `Assembly.LoadFrom` failure. That conservatively blocks valid same-process publication even when no managed code ran and Core plus every cleanup participant prove `remaining=0`.

The irreversible process boundary begins when `Assembly.LoadFrom` succeeds, not merely when an owner transaction begins. After that point, entry-type resolution, construction, context attachment, Entry, or commit may still fail, but the assembly itself cannot be unloaded safely from Unity Mono.

### Required boundary

- Record successful Mono assembly load as a process-lifetime fact for that owner.
- A successfully loaded assembly remains restart-required after any non-shutdown deactivation, even when every DTMAPI-owned root reaches zero.
- A non-code owner, missing DLL, or failed `Assembly.LoadFrom` may have its lifecycle row removed once Core and all cleanup participants report zero remaining roots and zero cleanup failures; a corrected/re-enabled source may then publish in the same process.
- If a no-assembly owner still has a failed or remaining platform root, keep it inactive and retryable. Remove the lifecycle state only after a later cleanup proves zero; never re-enter while a root remains.
- Restart diagnostics must distinguish `LoadedAssembly` from `IncompleteOwnerCleanup` instead of implying unknown code side effects before any assembly loaded.

### Acceptance criteria

- missing-DLL and `Assembly.LoadFrom` failure fixtures leave no restart gate after zero-root cleanup and can load after the source is corrected;
- a non-code ContentPack can disable, reach zero roots, re-enable, and republish exactly once in the same process;
- a synthetic no-assembly owner with an initially failing participant stays blocked, then becomes eligible only after a retry proves zero;
- every checkpoint after successful assembly load remains restart-required and never repeats Entry, including entry-type/constructor/Entry/publication failures.

## Issue 7 - Quarantine overflow is reported as a synthetic current owner root

### Code-review fact

High-frequency quarantine releases the delegate, then retains per-event owner failure metadata. Once the 128 named-owner table filled, the reviewed implementation incremented `quarantinedOther` and included an `other` row in `QuarantinedHandlers`. That overflow has no owner identity, so `RemoveOwner` can never remove it. A historical trimmed event was therefore reported as a current removable root and could make current-root counts remain nonzero forever.

### Required boundary and acceptance criteria

Overflow beyond 128 named owners must increment a historical `trimmed` scalar only. It must not create `other`, contribute to current `QuarantinedHandlers`, or affect owner cleanup completion. Stress with more than 128 unique failing owners must prove delegates and captured objects are collectible, named metadata remains capped and individually cleanable, trimmed history remains bounded/scalar, and removing a trimmed owner reports zero current roots removed.

## Issue 8 - Config preview aggregates retain arbitrary detail strings

### Code-review fact

The preview aggregate key is bounded, but each retained aggregate also stored `LastDetails`. This keeps up to 256 arbitrary detail strings in addition to the shared 64-entry recent failure window. Aggregate rows need operation/kind counts and timestamps; they do not need a second longer-lived failure-detail store.

### Required boundary and acceptance criteria

Config preview aggregates must be counts-only: owner/item/kind/operation, observation/warning counts, and bounded timestamps are sufficient. `LastDetails` must remain empty if the internal snapshot shape is retained for compatibility. Arbitrary failure detail may appear only in the shared 64-entry recent scalar window; successful detail must not be retained. Stress with repeated and more than 255 unique aggregate keys must preserve total/overflow counts, cap aggregate rows at 256 including the stable overflow row, and retain no aggregate detail string.

## Downstream Ownership

- Implementation lifecycle and validation results belong in `docs/updates/2026/20260712-0001-owner-platform-dependency-reconciliation.md`.
- Reproduction evidence and open/closed state belong in `docs/debug/issues/ISSUE-013-20260712-owner-platform-dependency-reconciliation.md`.
- The original common-boundary findings remain in `docs/reviews/code/2026/20260711-0001-general-owner-lifetime-boundary-audit.md`.
- The completed base refactor remains owned by `docs/updates/2026/20260711-0010-general-owner-lifetime-refactor.md`; this regression does not rewrite that historical implementation record.
