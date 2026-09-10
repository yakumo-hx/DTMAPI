# 2026-06-28 Crash Risk Problem Map For Ecosystem Comparison

## Purpose

This is a structured problem map derived from `docs/reviews/code/2026/20260628-0001-crash-log-full-code-audit.md`.

It is designed for two follow-up uses:

- compare DTMAPI's current architecture with mature modding ecosystems such as Stardew Valley SMAPI;
- send a compact, structured package to an external reviewer or web research thread without losing code ownership, risk, and evidence context.

This file is not an implementation goal and does not prove any single crash root cause. It turns each audit finding into a repeatable question set:

- What does this code do?
- Which game or DTMAPI behavior depends on it?
- Why is it probably written this way?
- What are the benefits?
- What are the costs or failure modes?
- What would a mature architecture usually try to do instead?
- What evidence would distinguish a real root cause from a plausible risk?

## Source Scope

Primary source:

- `docs/reviews/code/2026/20260628-0001-crash-log-full-code-audit.md`

Relevant code areas named by the audit:

- `src/DTMAPI.BepInExBootstrap/`
- `src/DTMAPI.Core/`
- `src/DTMAPI.GameBridge.DolocTown/`
- `src/DTMAPI.ModConfigMenu/`
- `testmods/`

Related debug issues:

- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/debug/issues/ISSUE-011-20260623-short-run-native-crash.md`

## How To Use This Map

When comparing against SMAPI or another mature framework, do not ask only "does it have the same class". Ask whether it has the same responsibility boundary:

- hook installation phase and retry policy;
- main-thread ownership of game/Harmony/native operations;
- required vs optional feature readiness;
- owner-scoped registration and cleanup;
- input registration lifecycle;
- event handler lifecycle;
- UI object lifecycle;
- native-object reference lifecycle;
- diagnostics/logging policy;
- third-party mod isolation.

## Problem Index

| ID | Audit item | Risk shape | Main comparison target |
| --- | --- | --- | --- |
| PM-01 | `System.Threading.Timer` retries `InstallHarmonyHooks()` every 2 seconds | off-main-thread hook/status/event fanout | hook install lifecycle |
| PM-02 | `SetHookStatus()` immediately dispatches `HookStatusChanged` and writes diagnostics | synchronous status fanout and possible thread crossing | diagnostics/event bus |
| PM-03 | `AllHookTargetsReady` includes many functional/experimental hooks | optional hooks can keep retry alive | capability readiness model |
| PM-04 | `EnvironmentReset` / `Feature.Update` high-frequency fanout | long-run managed/native lifecycle pressure | per-feature scheduler |
| PM-05 | static callback roots, timer, assembly-load subscription, native-object dictionaries | long-lived managed roots | shutdown/dispose model |
| PM-06 | global ownerless input registration | stale hotkeys and noisy movement dispatch | input ownership |
| PM-07 | DebugConsole item give/search/hover UI churn | short-run UI/object churn | dev tool UI lifecycle |
| PM-08 | MoreEquipmentSlots cloned UI/native equipment bridge | deep native UI/state integration risk | extension-slot storage model |
| PM-09 | AutoFishing native state dictionaries and long loops | long AFK lifecycle pressure | automation state machine |
| PM-10 | Qiuzy/NoWeeds third-party BepInEx variables | external Harmony/plugin collision | third-party isolation |
| PM-11 | EventManager disables failing handlers but retains delegates | captured roots remain alive | event subscription cleanup |
| PM-12 | failed code mod cleanup lacks input/third-party Harmony cleanup | partial load leaves residue | owner cleanup contract |
| PM-13 | config preview applies real setters during render | preview can mutate runtime state | pure config preview |
| PM-14 | file logging/export synchronous pressure | diagnostic IO and lock pressure | logging pipeline |
| PM-15 | bootstrap error accounting and shutdown cleanup gaps | native crash diagnosis lacks final counters | crash diagnostics |
| PM-16 | SaveSlots historic official UI state errors | old pager/state pollution risk | UI lifecycle proof |
| PM-17 | local mod correctness risks | small risks can amplify in player sets | release hygiene |

## PM-01: Hook Retry Timer Calls `InstallHarmonyHooks()` Every 2 Seconds

### What This Code Does

`DolocTownGameBridge` creates a `System.Threading.Timer` during initialization. The timer retries hook installation every two seconds until the bridge believes all hook targets are ready.

The target operation includes Harmony patch attempts, reflection-heavy game type lookup, hook-status updates, and feature hook registration.

### User-Visible Or DTMAPI Behavior

This is why DTMAPI can start even when some Doloc Town types are not immediately available. It can patch hooks later after assemblies load or native UI/game types become discoverable.

Affected behavior includes:

- DTMAPI startup status;
- `Hook` tab statuses;
- GameBridge feature availability;
- smoke probes that wait for hook readiness;
- features whose native hook targets are discovered late.

### Why It Was Probably Written This Way

Inference: this is a resilience mechanism. During early boot, not every game type or method is always ready at the same moment. A retry loop avoids hard failing DTMAPI if one target is temporarily unavailable.

It also reduces the need to perfectly model every Unity/BepInEx/Doloc Town load phase.

### Benefits

- Startup is tolerant of load-order uncertainty.
- Late-loaded targets can still become available.
- Hook status can show progress instead of a single fatal failure.
- Early development moves faster because new feature hooks can be added without a full loader-phase redesign.

### Costs And Risks

- `System.Threading.Timer` runs on a ThreadPool thread, not guaranteed Unity main thread.
- Harmony patching, reflection state, diagnostics events, and feature status changes may run off the main thread.
- If readiness includes optional or broken targets, the retry loop can remain alive for the entire process.
- A lock may prevent simultaneous hook installs, but it does not prove Unity/Harmony/native work is safe from that thread.
- This failure shape matches native Mono/Unity crash families better than a normal managed exception.

### Mature Scheme To Compare

Ask SMAPI or another mature framework:

- Are Harmony patches installed only in a deterministic loader phase?
- If hook retry exists, is it marshalled to the game/main thread?
- Are late retries bounded by count, time, or required capability set?
- Are optional hooks allowed to keep a global retry loop alive?
- Is there a "mod loaded but optional capability unavailable" status distinct from "framework not ready"?

Likely mature direction:

- main-thread owned hook install;
- explicit loader phases;
- bounded retry;
- required/optional capability separation;
- failed optional hooks degrade one feature, not the entire hook retry lifecycle.

### Evidence Needed

- Thread ID and main-thread flag for every hook retry.
- Retry count per hook target.
- List of unresolved required vs optional targets.
- Whether timer remains alive during a long player session.
- Long-run comparison before and after moving retries to main thread.

## PM-02: `SetHookStatus()` Immediately Dispatches `HookStatusChanged` And Writes Logs

### What This Code Does

`DtmApiRuntime.SetHookStatus()` updates retained diagnostic status, dispatches `HookStatusChanged`, and writes status/log records when status changes or when callers force detail updates.

### User-Visible Or DTMAPI Behavior

This powers:

- DTMAPI Settings `Hook` tab;
- status/debug records shown in the UI;
- smoke harness evidence;
- mod diagnostics;
- exported reports.

### Why It Was Probably Written This Way

Inference: hook status is both a diagnostic channel and a lightweight event stream. Immediate dispatch makes UI/status pages reactive and makes smoke tests easier to observe.

### Benefits

- Hook status appears quickly.
- Smoke harness can wait for real status transitions.
- Debugging is simple because every status writer uses one path.
- Mod authors can subscribe to diagnostics without polling.

### Costs And Risks

- If called from hook retry timer, status dispatch can cross from ThreadPool into DTMAPI event handlers.
- Event handlers may assume game/main thread.
- Status logging can become part of high-frequency runtime paths.
- If handler add/remove is not fully thread-safe, off-thread dispatch can create rare list/snapshot hazards.
- Status text can be mistaken for native gameplay proof.

### Mature Scheme To Compare

Ask:

- Are diagnostics events queued and drained on the main thread?
- Is logging separated from event dispatch?
- Are status updates coalesced or rate-limited?
- Does the framework distinguish internal hook health from gameplay validation evidence?
- Can status subscribers mutate game state?

Likely mature direction:

- diagnostics writes are cheap and thread-safe;
- event dispatch has a known thread policy;
- UI subscribers are main-thread only;
- high-frequency status paths use aggregation, not immediate rich text.

### Evidence Needed

- Thread ID at every `SetHookStatus()` call.
- Count and source of status events per minute.
- Subscriber count and owner list for `HookStatusChanged`.
- Whether any subscriber touches Unity objects or mod state.

## PM-03: `AllHookTargetsReady` Includes Functional And Experimental Hooks

### What This Code Does

`AllHookTargetsReady` decides whether the global hook retry timer can stop. The audit notes that readiness covers many feature hooks, including experimental or functional hooks.

### User-Visible Or DTMAPI Behavior

This affects whether DTMAPI reports a fully ready hook surface and whether retry keeps running after startup.

If one optional target is absent, unrelated systems may still be repeatedly rechecked.

### Why It Was Probably Written This Way

Inference: one readiness flag is simpler. During early DTMAPI development, a single "everything is ready" condition made HookProbe and smoke checks straightforward.

### Benefits

- Easy to understand.
- Easy to expose in status pages.
- Smoke checks can ask for one global readiness condition.
- Missing hook targets remain visible.

### Costs And Risks

- Optional features can keep infrastructure retry alive.
- Experimental hook drift becomes a runtime infrastructure problem.
- A single readiness flag blurs "DTMAPI can run" vs "all experimental features are patched".
- Failed optional hooks may produce repeated reflection/status work for no player-visible benefit.

### Mature Scheme To Compare

Ask:

- Does the framework separate core readiness, feature readiness, and optional compatibility probes?
- Can one optional feature keep global startup retry alive?
- How are "unavailable but safe" hooks represented?
- Are experimental features isolated behind per-feature lifecycle services?

Likely mature direction:

- `CoreReady`: required runtime works.
- `FeatureReady`: per-feature native owner works.
- `OptionalProbe`: diagnostic only; never blocks global readiness.

### Evidence Needed

- Current unresolved hook target list after a normal session.
- Which unresolved targets are required for enabled mods.
- Which unresolved targets are smoke-only or experimental.
- Whether global retry stops in ordinary player sessions.

## PM-04: `EnvironmentReset` / `Feature.Update` High-Frequency Fanout

### What This Code Does

GameBridge dispatches repeated lifecycle operations across registered features, including update loops and environment reset events.

### User-Visible Or DTMAPI Behavior

This powers many features:

- camera/view leases after room transitions;
- fishing automation;
- action speed;
- equipment slot lifecycle;
- animal viewer lifecycle;
- save/title cleanup;
- repeated feature status refresh.

### Why It Was Probably Written This Way

Inference: DTMAPI acts as a mini runtime inside Doloc Town. Central feature fanout is an easy way to keep multiple migrated mods coordinated without each mod patching native update loops independently.

### Benefits

- Centralizes lifecycle calls.
- Avoids many mods individually patching `Update`.
- Makes feature enable/disable and diagnostics easier.
- Allows cross-feature cleanup on save/title/environment transitions.

### Costs And Risks

- Long sessions produce thousands of bridge calls.
- A single central fanout can hide which feature is doing expensive work.
- If one feature retains native objects, the global loop keeps touching it.
- Native crash evidence may show only cumulative pressure, not the responsible owner.

### Mature Scheme To Compare

Ask:

- Are update subscriptions owner-scoped and removable?
- Are high-frequency updates opt-in and paused when inactive?
- Does each feature have counters and time budget?
- Are game-loop events typed by phase and thread?

Likely mature direction:

- owner-scoped update subscriptions;
- separate one-shot lifecycle events from per-frame update;
- per-owner diagnostics;
- inactive features do not receive high-frequency calls.

### Evidence Needed

- Feature update/reset counts by owner.
- Count deltas before crash or long soak exit.
- Which features receive update calls while disabled or inactive.
- Time spent per feature if feasible.

## PM-05: Long-Lived Managed Roots And Native Object Dictionaries

### What This Code Does

The audit identifies static callbacks, the hook retry timer, assembly-load subscriptions, feature dictionaries keyed by native objects, UI binders, Unity event delegates, and input registrations as long-lived roots.

### User-Visible Or DTMAPI Behavior

These are the backbone of DTMAPI's runtime:

- native hooks call back into DTMAPI;
- features remember native state across frames;
- UI clones and binders stay interactive;
- input/hotkey systems remain active.

### Why It Was Probably Written This Way

Inference: native hooks require static callback entry points. Many Unity objects do not have stable public identifiers, so the bridge stores object references directly. This is common in experimental Unity modding because it works before stable adapters exist.

### Benefits

- Direct mapping from native object to feature state.
- Easier to implement migrated mods quickly.
- Static callbacks are Harmony-friendly.
- UI and native object integration can be precise.

### Costs And Risks

- Native Unity objects can remain rooted after room/title/save changes.
- Static callbacks outlive save sessions unless explicitly cleared.
- Object-reference dictionaries need reliable cleanup on every lifecycle boundary.
- Native Mono/Unity GC crashes can happen without a managed exception if roots and native state diverge.

### Mature Scheme To Compare

Ask:

- Does the mature framework expose stable IDs instead of raw native object references?
- Are native object references weak, scoped, or cleared by owner/session?
- Is there a per-save/session disposal model?
- Are static callback roots reset on unload/shutdown?

Likely mature direction:

- stable DTO/adapters at public API boundary;
- explicit session-scoped containers;
- owner disposables;
- object-count diagnostics;
- weak references or ID-based lookup where safe.

### Evidence Needed

- Counts of every native-object-key dictionary.
- Counts before save load, after save load, after title return, and after quit.
- Static callback fields at shutdown.
- Timer and assembly-load subscription state at shutdown.

## PM-06: Global Ownerless Input Registration

### What This Code Does

`InputService` stores registered buttons globally. Mods register hotkeys or movement keys. The audit notes that failed-mod cleanup does not remove input registrations by owner.

### User-Visible Or DTMAPI Behavior

This powers:

- Y console;
- AutoFishing toggle and movement cancel;
- ActionSpeed / other migrated mod hotkeys;
- config keybind testing.

### Why It Was Probably Written This Way

Inference: early API needed a simple key-state helper. A global key set is easy to query and avoids per-mod input routing complexity.

### Benefits

- Simple API for migrated mods.
- Fast lookup.
- Easy unit tests.
- Works for ordinary hotkeys.

### Costs And Risks

- Failed or disabled mods can leave stale key registrations.
- Movement keys become DTMAPI event traffic.
- Multiple mods can collide without owner policy.
- UI focus bugs can cause open/close in the same frame if input is not phase-guarded.

### Mature Scheme To Compare

Ask:

- Are input registrations owner-scoped?
- Does disabling/unloading a mod remove all hotkeys?
- Are gamepad/keyboard/rebind actions represented by stable action names?
- Are UI focus and text input separated from global hotkeys?

Likely mature direction:

- owner token on registration;
- automatic cleanup on owner unload/failure;
- named action binding layer;
- per-frame edge suppression and UI focus guards.

### Evidence Needed

- Registered key list by owner.
- Stale key list after failed mod load.
- Dispatch count by key, especially movement keys.
- UI focus state at the time of hotkey dispatch.

## PM-07: DebugConsole UI Churn

### What This Code Does

The debug console builds item/search UI, gives items, shows hover tooltips, and rebuilds panels/cells/binders when dirty.

### User-Visible Or DTMAPI Behavior

This is a developer/player diagnostic tool:

- open console with hotkey;
- search items;
- give items;
- inspect tooltips.

### Why It Was Probably Written This Way

Inference: the debug console is a bootstrap UI built without a full native UI framework wrapper. Rebuilding is simpler than maintaining a pooled virtualized list.

### Benefits

- Easy to implement and debug.
- State is refreshed from source of truth after each action.
- Less risk of stale visual cells in short sessions.

### Costs And Risks

- Destroy/recreate cycles can churn Unity objects and event binders.
- Item give + hover + MoreEquipmentSlots can produce dense UI/native activity.
- Short-run crash samples include debug-console item give activity.

### Mature Scheme To Compare

Ask:

- Does the framework provide a reusable UI toolkit or virtualized list?
- Are debug tools isolated from normal player release?
- Are item-give actions batched separately from UI rebuild?
- Are UI event handlers owner-scoped and disposed?

Likely mature direction:

- cell pooling or virtualization;
- explicit UI panel lifecycle;
- batch refresh;
- debug UI disabled unless enabled by developer mode.

### Evidence Needed

- Rebuild count.
- Live cell count.
- Event binder count.
- Tooltip object create/destroy count.
- Item give count and UI dirty count.

## PM-08: MoreEquipmentSlots Cloned UI And Native Equipment Bridge

### What This Code Does

MoreEquipmentSlots adds extra equipment/accessory slots through GameBridge support. It clones/extends official UI, handles hover/click, stores sidecar slot data, restores items, and injects equipment effects.

### User-Visible Or DTMAPI Behavior

This is visible in the player equipment UI:

- extra equipment slots appear;
- hats/special accessories can be placed;
- effects apply without changing official character icon display;
- disabled mod items return to backpack/mail through protection logic.

### Why It Was Probably Written This Way

Inference: Doloc Town has fixed native equipment slots. To add extra slots without corrupting official save fields, DTMAPI must own sidecar storage and bridge native UI/effect responsibilities.

### Benefits

- Avoids directly changing official save equipment fields.
- Allows uninstall protection.
- Uses official hover/click preview where possible.
- Keeps public API from exposing raw native types.

### Costs And Risks

- Deep Unity UI clone lifecycle.
- Native item/function objects are used as bridge keys.
- Equipment effects must avoid double-counting and visual pollution.
- Storage must be save-scoped and owner-scoped.
- This feature appears in a short-run crash sample, so it needs isolation evidence even if current cleanup is improved.

### Mature Scheme To Compare

Ask:

- Does the mature framework support extra inventory/equipment slots as a first-class API?
- If not, how do mods protect items on uninstall?
- Are extra slots stored per save with explicit owner identity?
- Are UI clones pooled, tagged, and destroyed on every UI close/save/title?
- Are equipment effects applied through official stat recalculation phases?

Likely mature direction:

- protected sidecar storage;
- owner/session-scoped slots;
- explicit orphan recovery;
- native stat owner integration;
- UI clone lifecycle counters.

### Evidence Needed

- Active clone count.
- UI binder count.
- storage owner count.
- native equipment function count.
- save/load/title cleanup count.
- orphan recovery evidence.

## PM-09: AutoFishing Native State And Long AFK Loops

### What This Code Does

FishingAutomation drives native fishing stages: cast, wait, bite, reel, visible minigame automation, pull/collect, and recast loop. It also stores native phase state, animator speeds, minigame handles, hook physics, watchdog state, and diagnostics.

### User-Visible Or DTMAPI Behavior

This powers the AutoFishing mod:

- F6 toggles automation;
- default loop fishes repeatedly;
- optional instant bite;
- optional skip minigame;
- optional animation speed/charge changes;
- long AFK fishing sessions.

### Why It Was Probably Written This Way

Inference: the rewritten AutoFishing intentionally follows native responsibility functions instead of faking rewards. That requires observing and manipulating many native stage objects.

### Benefits

- Better gameplay fidelity than direct reward injection.
- Native success/failure paths remain meaningful.
- Visible minigame can be automated through native input decisions.
- Existing smoke evidence maps to real fifth-save behavior.

### Costs And Risks

- Long AFK sessions can run hundreds or thousands of cycles.
- Native object dictionaries/sets must be cleared perfectly.
- Diagnostics can become noisy if every catch/status is logged.
- Animation speed changes must restore on every exit path.

### Mature Scheme To Compare

Ask:

- Does the mature framework expose fishing stages as stable events?
- Are automation mods allowed to drive native input/state, or do they call higher-level APIs?
- How are long-running automation loops throttled and cancelled?
- Are state references cleared by save/session lifecycle?

Likely mature direction:

- explicit phase events;
- owner-scoped automation state;
- periodic compact counters;
- hard lifecycle cleanup on save/title/disable.

### Evidence Needed

- Counts of every FishingAutomation dictionary/hashset.
- Catch count, loop count, watchdog count.
- Cleanup count on title/save/load/disable.
- Long soak with AutoFishing only vs disabled.

## PM-10: Third-Party BepInEx Plugin And NoWeeds Isolation Variables

### What This Code Does

This is not DTMAPI code. The audit notes a third-party BepInEx plugin and NoWeeds Harmony failure in one short-run crash package.

### User-Visible Or DTMAPI Behavior

Player sessions may include:

- DTMAPI;
- DTMAPI ordinary mods;
- non-DTMAPI BepInEx plugins;
- legacy or failed Harmony patches.

### Why It Matters

DTMAPI can clean up only what it owns. A third-party BepInEx plugin can patch the same native game methods and create crash conditions that DTMAPI logs but cannot control.

### Benefits Of Current Visibility

- Logs can identify third-party plugins.
- DTMAPI can report its own mod inventory.
- The audit can separate long-run and short-run crash families.

### Costs And Risks

- Users see "DTMAPI crash" even when another BepInEx plugin participates.
- Hook collisions may not surface as managed exceptions.
- Partial Harmony failures can leave a complex runtime state.

### Mature Scheme To Compare

Ask:

- How does the mature ecosystem report non-framework plugins?
- Is there a conflict/isolation mode?
- Can the framework list all patches on a target method?
- Are third-party Harmony failures quarantined or merely logged?

Likely mature direction:

- plugin inventory in every report;
- patch-owner inventory for suspect methods;
- safe mode / core-only run instructions;
- compatibility matrix.

### Evidence Needed

- BepInEx plugin inventory per report.
- Harmony patch owner list for suspect native methods.
- Reproduction with third-party plugin absent/present.
- Reproduction with DTMAPI local mods absent/present.

## PM-11: EventManager Disables Failing Handlers But Retains Delegates

### What This Code Does

EventManager can disable repeatedly failing handlers, especially for high-frequency events, but the handler record may remain in the list.

### User-Visible Or DTMAPI Behavior

This protects gameplay from repeated exceptions in mod event callbacks while keeping DTMAPI running.

### Why It Was Probably Written This Way

Inference: fail-soft behavior is friendlier than crashing the whole runtime because one mod handler throws.

### Benefits

- Bad handlers stop executing.
- Runtime survives mod callback failures.
- Diagnostics can report disabled handlers.

### Costs And Risks

- Disabled delegates can still hold captured objects.
- Owner cleanup must remove them later, or they remain rooted.
- Concurrent dispatch/add/remove risk matters if diagnostics events can fire off-main-thread.

### Mature Scheme To Compare

Ask:

- Are failed handlers removed, disabled, or quarantined?
- Are handlers owner-scoped?
- Can a mod unload remove all delegates?
- Is event dispatch thread-confined?

Likely mature direction:

- owner-scoped subscriptions;
- automatic unsubscribe on owner unload/failure;
- disabled-handler count diagnostics;
- main-thread event dispatch for game events.

### Evidence Needed

- Active, disabled, removed handler counts by event type.
- Owner list for disabled handlers.
- Handler cleanup count after failed mod load.

## PM-12: Failed Code Mod Cleanup Lacks Input And Third-Party Harmony Cleanup

### What This Code Does

DTMAPI cleanup can remove several DTMAPI-owned registrations after a code mod fails, but the audit notes input cleanup and third-party Harmony cleanup are unsupported.

### User-Visible Or DTMAPI Behavior

This affects what remains after:

- a mod fails to load;
- a mod throws during registration;
- a user disables/re-enables mods;
- a partial load occurs.

### Why It Was Probably Written This Way

Inference: DTMAPI first cleaned the registries it directly owned and could safely identify. Input and arbitrary Harmony ownership need stronger owner metadata.

### Benefits

- Some failed-mod residue is cleaned.
- Cleanup avoids pretending to remove patches it does not own.
- Lower chance of destructive cleanup.

### Costs And Risks

- Stale input keys can remain.
- Partial Harmony patches can remain.
- Users may see a mod "disabled" while its earlier registration still affects runtime.

### Mature Scheme To Compare

Ask:

- Does every registration API require an owner token?
- Does the loader track all resources created during mod entry?
- Are failed mods rolled back transactionally?
- Are third-party patches outside the framework explicitly marked unmanaged?

Likely mature direction:

- owner/resource ledger;
- transactional mod load;
- cleanup by owner;
- clear unmanaged-patch warning.

### Evidence Needed

- Resource ledger before and after failed mod load.
- Input keys by owner.
- Patch owners by mod.
- Failed-load cleanup report with counts.

## PM-13: Config Preview Applies Real Setters During Render

### What This Code Does

The config menu preview can apply pending values through real setter paths while rendering, then roll them back.

### User-Visible Or DTMAPI Behavior

This makes config UI responsive:

- pending slider/toggle values can preview effects;
- reset/save/cancel can appear accurate;
- title settings can display live state.

### Why It Was Probably Written This Way

Inference: using real setters avoids duplicating config display logic and keeps UI behavior aligned with runtime behavior.

### Benefits

- Less duplicated code.
- Accurate preview for simple values.
- Easy for mod authors to reason about small config changes.

### Costs And Risks

- Setters may register input, create UI, patch hooks, mutate native state, or write logs.
- Render-time preview becomes a runtime side-effect path.
- Cancel/rollback may not undo external side effects.

### Mature Scheme To Compare

Ask:

- Are config previews pure?
- Are side-effectful setters delayed until save/apply?
- Is there a transaction or dry-run mode?
- Can a mod mark a config option as requiring restart/apply?

Likely mature direction:

- pure pending-value model;
- explicit apply phase;
- restart-required marker;
- preview mode blocks side effects.

### Evidence Needed

- Audit setters registered in current mods.
- Detect input/hook/UI changes during preview.
- Compare runtime resource count before/after opening/canceling config.

## PM-14: File Logging And Diagnostics Export Synchronous Pressure

### What This Code Does

DTMAPI writes logs, monitors log files, exports diagnostics, and collects crash/report artifacts.

### User-Visible Or DTMAPI Behavior

This powers:

- `Logs` tab;
- `Export Report`;
- `4_collect_dtmapi_logs.bat`;
- player support packages.

### Why It Was Probably Written This Way

Inference: supportability mattered immediately because players report screenshots and crashes. Synchronous file operations are simpler and more reliable for small log sets.

### Benefits

- Reports are easy to collect.
- Recent logs are available without external tools.
- Player support instructions are clearer.

### Costs And Risks

- IO during gameplay can create stutter if done at the wrong time.
- Per-instance locks may not serialize same-path writes across monitors.
- Report export can become expensive if done while the game is stressed.
- Log volume can obscure signal.

### Mature Scheme To Compare

Ask:

- Is logging asynchronous or centralized?
- Are logs bounded and rotated?
- Are exports only user-triggered and outside hot loops?
- Does the report include counters instead of raw spam?

Likely mature direction:

- bounded log retention;
- central writer or per-path lock;
- compact counters for high-frequency events;
- export-time collection, not gameplay-time scanning.

### Evidence Needed

- File write count per minute.
- Export duration.
- Log size after 1 hour AFK fishing.
- Whether any diagnostics collection runs during hot gameplay loops.

## PM-15: Bootstrap Error Accounting And Shutdown Cleanup Gaps

### What This Code Does

Bootstrap records startup/update errors and logs lifecycle transitions. The audit says recurring errors may be counted only once and shutdown cleanup evidence is not enough for native-crash diagnosis.

### User-Visible Or DTMAPI Behavior

This affects:

- whether players see fatal windows;
- what exported reports contain;
- whether DTMAPI can prove clean quit;
- whether repeated errors are visible.

### Why It Was Probably Written This Way

Inference: suppressing duplicate errors prevents log floods and makes first failure visible.

### Benefits

- Logs stay readable.
- One bad component does not flood every frame.
- Users see a simpler error story.

### Costs And Risks

- Recurring error cadence can be lost.
- Shutdown may not prove every static root/timer/subscription cleared.
- Native crash reports need final counters, not just first exception.

### Mature Scheme To Compare

Ask:

- Does the framework track first error, last error, and count?
- Does shutdown dispose every service in a known order?
- Is there a final runtime snapshot on quit?
- Are native crashes linked to previous lifecycle counters?

Likely mature direction:

- first/last/count error records;
- explicit runtime dispose graph;
- shutdown counter snapshot;
- crash report bundles include recent lifecycle summaries.

### Evidence Needed

- Repeated component error counters.
- `OnApplicationQuit` cleanup counts.
- Timer disposed flag.
- static callback cleared flag.
- event/input/resource counts at exit.

## PM-16: SaveSlots Historic Official UI State Errors

### What This Code Does

SaveSlots extends official save UI capacity and paging behavior. Historic logs showed `SaveSlots.OfficialUi.SelectPaging` `IndexOutOfRangeException`; current code appears to include cleanup and fixed 12-slot behavior.

### User-Visible Or DTMAPI Behavior

This affects:

- official save slot screen;
- extra save slots;
- page/pager display;
- selecting/loading additional slots.

### Why It Was Probably Written This Way

Inference: the original goal was to fit extra saves into the official UI with minimal visible divergence from native menus.

### Benefits

- User sees official save UI.
- Extra slots feel native.
- Current fixed slot count simplifies support.

### Costs And Risks

- Official UI has native state assumptions.
- Pager/index state can drift across title/save transitions.
- Old crash logs prove this area once failed in a visible way.

### Mature Scheme To Compare

Ask:

- Does the framework provide official UI extension points?
- Are extra slots modeled as data first, UI second?
- Are UI states recreated per open rather than carried globally?
- Are pager indices validated against current data every frame/open?

Likely mature direction:

- data-driven model;
- per-open UI state;
- hard index bounds;
- smoke test for page transitions after repeated title/save cycles.

### Evidence Needed

- official UI state count.
- pager object count.
- binder count.
- current page/index values during navigation.
- repeated open/close and save/load smoke.

## PM-17: Local Mod Correctness Risks

### What This Code Does

The audit lists smaller risks in migrated/local mods, including Mine enabled state, Zoom lease release, Manbo audio defaults, ActionSpeed `None` key, QA fixtures, and experimental StrongPlantingGun behavior.

### User-Visible Or DTMAPI Behavior

These affect ordinary Workshop/local mods that players enable:

- mines;
- zoom;
- audio replacement;
- action speed;
- harvesting QA;
- planting gun.

### Why It Was Probably Written This Way

Inference: these mods are both feature examples and migrated functionality. Some carry test/experimental history and may not have the same release hygiene as the core runtime.

### Benefits

- Real mods exercise DTMAPI APIs.
- Faster discovery of missing GameBridge capabilities.
- Player-visible features validate the ecosystem.

### Costs And Risks

- Small mod-specific bugs can look like DTMAPI runtime bugs.
- Experimental examples can ship accidentally.
- Default verbose logging can hide real issues.
- Config values like `"None"` can leak into input handling.

### Mature Scheme To Compare

Ask:

- Does the mature ecosystem distinguish sample mods, test mods, and release mods?
- Are release packages linted for QA-only mods and verbose defaults?
- Are experimental APIs clearly gated?
- Are ordinary mods forbidden from bootstrap/plugin folders?

Likely mature direction:

- release package linting;
- mod manifest validation;
- separate samples from player packages;
- stable/experimental API compatibility gates.

### Evidence Needed

- Release package manifest inventory.
- Default config audit.
- Experimental API usage audit.
- Per-mod smoke matrix.

## Cross-Cutting Comparison Questions

Use this section as the short checklist for SMAPI or external research.

### Hook Lifecycle

- When are hooks installed?
- Is hook installation always main-thread or loader-thread confined?
- Are retries bounded?
- Are optional hooks separated from core readiness?
- Can one feature's hook failure keep global retry alive?

### Diagnostics And Events

- Are diagnostic status updates events, logs, or both?
- Are status events thread-confined?
- Are high-frequency statuses aggregated?
- Is "hook status" distinct from "gameplay proof"?

### Owner And Resource Cleanup

- Does every registration have an owner?
- Can failed mod load roll back input, events, UI, Harmony patches, and storage?
- Are disabled handlers removed or retained?
- Is shutdown cleanup measurable?

### Input

- Are hotkeys bound to framework action names or raw keys?
- Does text input suppress hotkeys?
- Are movement keys treated differently from low-frequency hotkeys?
- Can stale keys survive mod failure?

### UI

- Does the framework offer pooled/virtualized UI primitives?
- Are cloned native UI objects tagged and counted?
- Are event binders owner-scoped?
- Is UI state recreated per open?

### Native Object State

- Are raw native objects used as dictionary keys?
- Are references weak, scoped, or ID-based?
- Are counts emitted on save/load/title/quit?
- How are destroyed Unity objects detected?

### Third-Party Isolation

- Can reports list all plugins and Harmony patch owners?
- Is there a safe mode?
- Can non-framework plugins be isolated from framework mod bugs?

### Release Hygiene

- Are sample/test mods excluded from player packages?
- Are package manifests linted?
- Are experimental APIs gated and labeled?
- Are verbose logs off by default?

## Prioritized Mature-Architecture Candidates For DTMAPI

This is not an implementation plan, but these are the architecture ideas most likely to survive comparison with mature systems.

### Candidate 1: Main-Thread Hook Retry Queue

Move hook retry work out of `System.Threading.Timer` callbacks and into a main-thread queue drained from the bootstrap/game update path.

Expected benefit:

- removes off-main-thread Harmony/status/event fanout risk.

Verification:

- hook retry thread ID is always the main/game thread;
- retry count bounded;
- optional hooks cannot keep global retry alive.

### Candidate 2: Required vs Optional Hook Readiness

Split hook status into core runtime readiness, enabled-feature readiness, optional diagnostics readiness, and smoke-only targets.

Expected benefit:

- a missing experimental hook no longer keeps infrastructure retry alive.

Verification:

- `CoreReady` can pass while optional features report unavailable;
- enabled mods report their own missing capability clearly.

### Candidate 3: Owner-Scoped Runtime Resource Ledger

Every mod-owned registration should record owner, kind, creation phase, and cleanup action.

Expected benefit:

- failed mod load can roll back input/events/UI/config/diagnostics consistently.

Verification:

- failed-load cleanup reports counts by resource kind;
- no stale input/event registrations after a simulated failure.

### Candidate 4: Compact Long-Run Counters

Replace high-frequency log lines with periodic owner counters for AutoFishing, EquipmentSlots, DebugConsole, EventManager, input, and feature updates.

Expected benefit:

- long AFK sessions remain diagnosable without giant logs.

Verification:

- 1000-catch AutoFishing run has bounded log growth;
- crash reports include last counter snapshot.

### Candidate 5: Native Object Lifecycle Audits

For every feature storing native/Unity object references, emit counts on save load, title return, feature disable, and quit.

Expected benefit:

- turns "maybe leaked native object" into measurable state.

Verification:

- counts return to zero where expected;
- destroyed Unity objects are not retained.

## Things Not To Conclude From This Map

- Do not conclude that `System.Threading.Timer` is proven to be the crash root cause. It is a high-priority candidate because the shape matches native crash risk.
- Do not conclude that log volume alone caused the crash. Log volume is a signal and pressure source, not enough evidence by itself.
- Do not conclude that every local mod risk listed here is a current regression. Some are release-hygiene or evidence-gap risks.
- Do not copy SMAPI code or third-party implementation. Use mature ecosystems for architecture comparison only.
- Do not treat Hook status as gameplay proof. It is diagnostic state unless backed by native-owner/manual/smoke evidence.

## One-Page Summary For External Review

DTMAPI's current crash-risk shape is less like a single managed exception and more like cumulative lifecycle pressure:

- hook retry can call Harmony/status/event paths from a timer thread;
- hook readiness mixes required and optional targets;
- diagnostics status is immediate and eventful;
- feature update/reset fanout is central and high-frequency;
- several systems store native/Unity object references across frames;
- input and event registrations need stronger owner cleanup;
- DebugConsole, MoreEquipmentSlots, and AutoFishing are the highest-value isolation targets;
- third-party BepInEx plugins must be recorded as separate variables.

The mature comparison question is therefore:

> How does a mature modding framework keep hooks, events, input, UI objects, native references, diagnostics, and failed mod cleanup owner-scoped, main-thread-safe, bounded, and observable over long player sessions?
