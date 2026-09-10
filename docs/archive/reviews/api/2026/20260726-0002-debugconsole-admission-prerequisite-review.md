# DebugConsole Independent Admission Prerequisite Review

**Review ID:** `20260726-0002`

**Date:** 2026-07-26

**Status:** recorded — behavior-equivalent extraction plan ready; DebugConsole admission remains blocked

**Scope:** DebugConsole native-owner, input, modal, command, lifecycle,
Compatibility, owner-cleanup and later UI-rewrite planning only; no
eleventh-product admission, implementation, Advanced package, public command
service, game launch, Release suite or 0.5.5 publication

## Source Request

Prepare the independent DebugConsole admission prerequisite by directly
completing:

- native owner, input, modal, command and lifecycle review;
- a behavior-equivalent extraction plan;
- Compatibility, Owner cleanup and UI rewrite acceptance matrices.

The required order is **equivalent ownership extraction first, visual/UX
rewrite second**. This Review does not combine or authorize those two
implementation Updates.

## Admission Result

**NO-GO for admitting the current source. GO for the bounded extraction plan
below.**

DebugConsole is a published optional Diagnostic product, not a built-in
Runtime feature:

- Catalog identity: `DTMAPI.DebugConsoleMod`;
- Workshop item: `3742714442`;
- retained public version: `0.3.1-dtmapi`;
- target product version: `1.0.0`, minimum DTMAPI `0.5.5`;
- current Catalog state: `RebuildBlocked`;
- current Batch 6 contract: `ProductNative+PlatformSeam / split-decided`.

The optional product currently owns only 148 tracked source lines for identity,
configuration, Y/Escape hotkeys and API binding. The actual 2,335-line UI
executor lives in mandatory `DTMAPI.BepInExBootstrap`, is constructed during
every Bootstrap `Awake`, is registered as process-lifetime provider
`DTMAPI.DebugConsoleHost`, participates in lifecycle reports and receives an
`Update()` call on every selected Runtime frame. Even with no product owner,
the product implementation remains in every player's normal load path.

That physical shape makes disable/unsubscribe misleading and is the admission
blocker. The new product must own its UI, allowlisted debug actions and
product-specific native patches. Mandatory Runtime may retain only genuine
Platform coordination and thin frozen-ABI compatibility routing.

## Evidence Inspected

- `PROJECT.md`, `docs/workflows/codex-api-rebuild.md`,
  `docs/architecture/batch6-managed-mod-identity-contract.md` and
  `docs/api/public-api-matrix.md`;
- prior boundary Review `20260713-0007` and the current Batch 6 Phase 0
  contract/baseline;
- current Product Catalog DebugConsole row and retained-artifact facts;
- current product source/manifest/README under
  `products/first-party/DebugConsole`;
- `BootstrapPlugin`, `ReflectedDebugConsoleUi`, Core input/UI services,
  GameBridge Diagnostic implementations, input Hook callbacks, owner cleanup
  and DebugConsole QA fixtures;
- native-owner special audits for save, time, teleport, inventory and the
  remaining Y-console APIs;
- `DebugConsoleInput.md`, ISSUE-014 and final Batch 2 Update/evidence;
- tracked decompile
  `references/doloc-town/reverse/builds/24256979_test_7A1907/decompiled/Assembly-CSharp`,
  including `DevHelper`, `DolocUserInput`, `AgentControllerState` and official
  command declarations;
- `src/DTMAPI.ConsoleCommands/README.md` and repository-wide consumer scans.

## 1. Native Owners

DebugConsole is not one native-owner domain. It is a product UI coordinating a
fixed set of unrelated Diagnostic operations:

| Concern/action | Native or authoritative state owner | Current reach | Target physical owner |
| --- | --- | --- | --- |
| Canvas, layout, filters, paging, hover, buttons, localization | DebugConsole-owned Unity object graph | reflected Bootstrap UI | DebugConsole ProductNative |
| Y/Escape binding and DTMAPI event dispatch | owner-bound Core input registry | reached; current source uses typed `SaveLoaded` keybinds | Platform service consumed by product |
| DTMAPI modal arbitration | Core `UiRuntimeService` active menu/owner state | reached through Bootstrap-internal owner-bound call | minimal Platform modal token; no product renderer |
| Native menu/tool/item isolation | `AgentControllerState.EnterUICheck`, `UseTool`, `UseItem` | three verified GameBridge Prefixes | product-owned three-Prefix compatibility behavior |
| Full native input-map suspension | `DolocUserInput.DisableAllInput(includeGlobal)` / `ResumeCurrentInput` | official console reaches it; DTMAPI Y console does not | later modal rewrite candidate only |
| Item list/give | native `TbItem`, query/count/capacity and backpack placement plus DTMAPI source index | reached/debug-only | DebugConsole ProductNative allowlist |
| Weather read/write | `TbWeather`, archive forecast/current weather, `ArchiveDataHandle.SetWeather/PatchWeather` | reached/debug-only | DebugConsole ProductNative allowlist |
| Teleport | native mark/station tables and `DolocAPI.DoTransport` | request owner reached; completion is not | DebugConsole ProductNative allowlist |
| Time period/day/week/month | archive date plus `ArchiveDataHandle.PassTimeNoControl` and `DolocAPI.OnWakeUp` | partial/watch | DebugConsole ProductNative allowlist |
| Instant save | native `DolocAPI.SaveGame` | save-only reached; reload deliberately blocked | DebugConsole ProductNative Diagnostic action |
| Movement multiplier | player `MotionAbility.SetMoveScaler` | reached, current owner lease | DebugConsole ProductNative reversible lease |
| Time scale | `DolocAPI.SetTimeScale/RevertTimeScale` | reached, current owner lease | DebugConsole ProductNative reversible lease |
| Money/tech/progression | native command/add-point functions or direct archive collections | mixed reached/debug-only | DebugConsole ProductNative fixed actions |
| Crop maturity | `PlantBasin.Crop.DEBUG_SetLevel` family | reached only for reviewed basin traversal | DebugConsole ProductNative fixed action |
| Creative mode | native debug flags plus multiple cost/time Harmony bypasses | multi-owner partial/watch | DebugConsole ProductNative transactional lease |
| Monster/resource spawn | native room host plus official command/resource creation paths | room-gated/debug-only | DebugConsole ProductNative fixed action |

There is only one real product consumer of these combined policies. Raw native
types, debug commands and cheat verbs must not become ordinary public APIs.
The existing Diagnostic interfaces remain compatibility contracts for the
retained binary, not evidence for SharedNative.

The official `DevHelper` console is reference evidence, not the target UI. It
uses F1, a RedSaw `GameConsole`, and broad native input disable/restore. The
game contains hundreds of `[Command]` entries, including arbitrary script,
load, clear, refresh and progression/story mutations. DTMAPI must not expose
that parser or command table wholesale.

## 2. Input Review

### Current source owner

The tracked product registers:

- `debug-console.toggle=Y`, `DtmInputScope.SaveLoaded`;
- `debug-console.close=Escape`, `DtmInputScope.SaveLoaded`.

Only owner-matching `KeybindPressed` events are accepted. Y before a save is
ignored. Y toggles only after `SaveLoaded`; Escape closes only while open.
Core owner deactivation removes input registrations and events.

### Published compatibility owner

The exact retained Workshop package differs from the current source behavior:
the final Batch 2 gate proves the retained binary still uses legacy
`RegisterButton("Y")`/`ButtonPressed`. While its modal is open, Core dispatches
that Y only to the active custom-menu owner, only if that owner has the legacy
registration, and only if no typed registration shadows it. This is a narrow
frozen compatibility exception, not a general modal broadcast.

Final retained evidence:

- tree: 10 files, 303,759 bytes,
  `c4e6eda7f131fcffebe14b1784b9b487f3ff07edb8ad7f600fea0e32063b7d60`;
- DLL SHA-256:
  `E5A34963C0B66D6168104AF27DB849D707EE644F07917D8274868F8B8299B41E`;
- `GAME-SMOKE/20260715-153336`: exact Workshop source, six
  owner-targeted legacy Y closes, zero typed modal-Y dispatches, two Escape
  closes, no native-menu leak and clean title/exit.

### Required new-product input controller

The 1.0 product uses one typed owner for Y and Escape. It must preserve:

- one physical Y press produces one final transition;
- opener Y cannot immediately close until released;
- held Y never flickers;
- focused text input receives Y instead of closing;
- rapid release/retap remains valid;
- Escape closes once and holds native isolation until the physical close edge
  is fully drained;
- button close and owner deactivation do not create a synthetic Y/Escape edge;
- title, save-slot change and re-entry clear all latches.

The compatibility controller and new typed controller are mutually exclusive.
Duplicate source/version selection must fail before either publishes input
roots.

## 3. Modal Review

### Current DTMAPI modal

`ReflectedDebugConsoleUi.Open` records an owner-bound Core custom menu, marks
the console open and activates the three native input Prefixes. Close clears
the modal-open flag. A raw Escape close starts a bounded drain that remains
active until two consecutive clean frames; this prevents the delayed same
Escape from opening native `MainMenuUiState`.

While open, Core permits the owning `SaveLoaded` registrations needed to close
the modal but blocks ordinary Gameplay registrations, broadcasts and updates.
The reflected host also owns Canvas/EventSystem creation, reuses a native
EventSystem when available, and destroys its fallback EventSystem on close.

### Native modal fact

The official console's stronger native owner is
`DolocUserInput.DisableAllInput(includeGlobal: true)` paired with
`ResumeCurrentInput()`. The latter re-enables global input and whichever
`DolocInputType` is current. It is not an owner-token or nested lease. Calling
it blindly can release another UI's input lock.

### Extraction decision

Behavior-equivalent extraction keeps the verified three Prefixes and
two-clean-frame Escape drain under the exact DebugConsole Harmony owner. It
does not switch to broad native `DisableAllInput` during ownership migration.

Core may expose/reuse one narrow owner-bound modal token for cross-owner DTMAPI
arbitration:

- acquire/release by exact product owner;
- only the owning `SaveLoaded` close keys remain dispatchable;
- ordinary Gameplay input/events/updates stay blocked;
- owner cleanup releases the token;
- no Unity renderer, layout, command policy or DebugConsole-specific state is
  added to Core.

The native three-Prefix suppression remains ProductNative because there is one
real consumer. A later UI/modal rewrite may consider the official native input
owner only after it has snapshot/stack/restore semantics, nested-modal conflict
tests and exact failure rollback. That change is not behavior-equivalent
extraction.

## 4. Command Review

### Current fact

The Y console is not a text-command service. Its buttons call a fixed set of
Diagnostic API methods directly. `src/DTMAPI.ConsoleCommands` contains only a
README; the repository has no implemented owner-bound command registry,
parser, permission service or command-history contract.

Therefore:

- do not claim that extraction implements `ICommandHelper` or the planned
  `dtmapi list/errors/dump-hooks` service;
- do not feed arbitrary text to the official RedSaw command parser;
- do not enumerate and expose all native `[Command]` methods;
- do not promote current cheat/debug APIs for ordinary mods.

### Behavior-equivalent command model

The extracted product uses private, typed allowlist descriptors. Each action
has:

- stable product-local action ID and localized label;
- explicit parameter bounds and room/save preconditions;
- one reviewed native responsibility function;
- before/after readback where available;
- success/failure result and structured owner log;
- mutation classification: transient lease, working save-bound mutation,
  native save request or read-only query.

The initial allowlist is exactly the behavior already exposed by the retained
UI. No command-line input, aliases, history or new native verb is added in the
equivalent phase.

If the later UI rewrite introduces a command palette, DebugConsole owns its
allowlisted actions and presentation. A future generic owner-bound command
registration/parser/log-output service is a separate Platform project with
duplicate-name policy, permission/scope, quoting, cancellation, thread,
owner-cleanup and bounded-output rules. It is not required to move the product
out of Bootstrap and must not be invented as part of that move.

### Save classification

- item give, money, tech, weather, time, crop, spawn and similar actions mutate
  native save-bound Working state; native save remains the only ordinary commit;
- movement, time scale and creative bypass are transient owner leases and must
  restore at close policy, title, disable, entry failure and shutdown;
- `Save here` intentionally invokes native `SaveGame` and must be visibly
  marked as a commit action;
- UI filters, language and optional future command history are
  configuration/diagnostics, not gameplay state.

Owner cleanup must never convert abandoned Working mutations into Committed
state. It also must not undo an intentionally native-saved item/money/tech
result. Transient leases and durable command effects require separate ledgers.

## 5. Lifecycle Review

### Current strengths

- one owner may bind `DTMAPI.DebugConsoleHost`; replacement by another owner is
  rejected;
- owner cleanup closes the menu, clears modal/drain flags, destroys root and
  fallback EventSystem, clears binders/inputs/APIs/manifest and reports zero
  host resources;
- stale facades reject calls after deactivation;
- inactive Bootstrap frames do not recreate an owner graph;
- movement reset is explicitly requested on return to title and is also
  covered by GameBridge owner cleanup.

### Current blockers

1. Bootstrap always constructs the UI host/provider and calls its Update even
   when the optional product is absent.
2. The three input-isolation Hooks are process-lifetime GameBridge
   infrastructure rather than exact DebugConsole-owned, demand-installed
   roots.
3. `OnReturnedToTitle` explicitly resets movement but not DebugConsole-owned
   time scale or creative mode. Owner deactivation can restore them, but mods
   remain loaded across title; title restoration is not equivalent to owner
   deactivation.
4. Current advanced actions combine reversible leases and durable gameplay
   mutations in one broad GameBridge service.
5. The current UI root is normally hidden rather than fully released on an
   ordinary close; only owner/title/shutdown cleanup proves the full graph
   zero.
6. `Open`/`Close`, raw input sampling, Core frame sampling and native Prefix
   state are coupled by Bootstrap ordering. Moving only the file without an
   explicit product controller would reintroduce double toggles or native-menu
   leaks.
7. The source manifest omits `Type`/`CodeModKind` and depends on both
   `DTMAPI.DebugConsoleHost` and GameBridge. It is a legacy Strict
   compatibility input, not an Advanced package.

## Behavior-Equivalent Extraction Plan

### E0 — Freeze the old product

Before source movement:

- capture the exact retained DLL's complete MemberRefs for
  `IDebugConsoleApi` and every consumed Diagnostic interface/DTO;
- freeze provider IDs, versions, manifest/dependency behavior and the final
  retained tree/DLL hashes above;
- convert the current runtime matrix into executable behavior fixtures:
  typed and legacy input, focus, Escape drain, right-click cell give,
  item/source/category paging, weather/teleport/time/movement/advanced actions,
  EventSystem fallback, title and owner cleanup;
- classify every action as read-only, transient lease, Working mutation or
  native save request.

No API signature is removed and no UI is redesigned in E0.

### E1 — Build the new hidden ProductNative implementation

After a separate admission Review authorizes the exact identity:

- create the SDK/policy-generated netstandard2.0 Advanced product;
- keep identity, Workshop ID, config path, language behavior and content
  sidecars;
- make the product own its Unity UI, typed input controller, private action
  allowlist, native executors, reversible leases and exact Harmony owner;
- remove the new product's dependency on `DTMAPI.DebugConsoleHost` and on
  Diagnostic GameBridge APIs;
- use only generic Platform input/config/log/modal/owner-lifecycle seams;
- keep the implementation hidden behind the existing Catalog state until
  behavior and cleanup fixtures pass.

Product native types stay private. No new public cheat/debug surface is
introduced.

### E2 — Move the retained ABI to the existing compatibility component

Reuse the one Catalog-owned dormant-shipped
`DTMAPI.GameBridge.DolocTown.Compatibility.dll`; do not create a second host or
receipt family.

- move the old reflected UI executor and old Diagnostic executors needed by the
  exact retained binary into that component;
- keep thin provider/proxy coordination for `DTMAPI.DebugConsoleHost` and the
  frozen Diagnostic interfaces in mandatory Runtime;
- pass generic Runtime/input/UI/log callbacks into the host instead of adding a
  Compatibility-to-Bootstrap assembly reference;
- load the host only on the first real old-ABI bind/call or explicit optional
  QA demand;
- warn once that the old API is Diagnostic/Frozen;
- after owner cleanup the assembly may remain Mono process-resident but every
  service, UI object, callback, demand and Hook is resident-dormant/zero.

The new 1.0 product must not activate this host. New-product and legacy-host
native patches are mutually exclusive for the same identity.

### E3 — Atomic ownership switch

Only after E1 and E2 are green:

- atomically switch the Catalog product identity/policy/package to the
  SDK-generated Advanced candidate;
- remove `ReflectedDebugConsoleUi`, DebugConsole construction, provider body,
  lifecycle/reset/report branches, per-frame Update and product-specific input
  consumption from Bootstrap;
- remove active DebugConsole execution bodies from mandatory GameBridge,
  retaining only justified thin Compatibility/Platform coordination;
- prove the five default-loaded Runtime assemblies have no hidden
  DebugConsole UI/action executor and that the no-product process does no
  DebugConsole frame work;
- preserve a small live zero-leftover source/artifact scan for known old
  Bootstrap/GameBridge product bodies.

This switch still does not rewrite the UI.

### E4 — Independent equivalent acceptance

Run the exact behavior, Compatibility, owner cleanup and save-mode matrices
below against the frozen candidate. Keep the implementation Update at
`implemented` until an independent Review accepts it. Only then may a separate
UI-rewrite Review/Update begin.

## Compatibility Acceptance Matrix

| Case | Required result |
| --- | --- |
| No DebugConsole product, no old consumer | compatibility component dormant; no UI object, product executor, frame callback, input Hook/demand or owner root |
| Exact retained Workshop 0.3.1 binary | same provider IDs/MemberRefs bind; legacy Y/Escape/focus/modal/UI/actions work; one frozen warning |
| New 1.0 Advanced product | no `IDebugConsoleApi`/Diagnostic API consumption; compatibility component remains dormant |
| Duplicate loose + official/Workshop source | authoritative loader conflict/restart diagnostic; never two input/UI/native owners |
| Missing or mismatched compatibility bytes with old binary | fail closed before service/UI/Hook publication; Manager/Doctor gives a player-readable repair |
| Old owner disabled/unsubscribed | UI/lease/callback/Hook roots zero; provider/component truthfully process-resident dormant if previously loaded |
| New owner disabled/unsubscribed | exact ProductNative Harmony owner, input registrations, UI graph and leases zero; no compatibility fallback activation |
| Source/version handoff in one process | loaded code cannot re-enter as another version; restart required |
| ABI gate | complete retained MemberRef set, DTO accessors, provider identity and minimum-version resolution unchanged |
| Package gate | reuse existing optional component manifest/install-state authority; no second Host or hand-authored Advanced manifest/receipt |

## Owner Cleanup Acceptance Matrix

| Boundary | Required zero/restoration proof |
| --- | --- |
| Failed Entry before UI | no modal token, input registration, event, patch, native lease, Canvas/EventSystem or product root |
| Partial UI construction failure | destroy every created object/listener/binder in reverse order; leave native input enabled and no modal |
| Open then Y close | one final close; opener latch cleared; no modal; no native drain after release |
| Open then Escape close | modal closes once; three Prefixes remain active only through two clean frames; no `MainMenuUiState` leak |
| Button close | modal/EventSystem/input state released without synthetic hotkey edge |
| Search focus | Y stays in focused field; focus and selected object clear on close/title |
| SaveLoaded same slot/new slot | no duplicate registrations/patches; filters reset only by the frozen boundary semantics |
| Returned to title | UI graph, modal/drain, movement, time scale, creative flags/patches and transient command state restored; no product frame work |
| Loader deactivate | exact Harmony owner unpatched; typed/legacy registrations, events, modal token, Canvas, EventSystem, binders, APIs, caches and owner roots zero |
| Compatibility deactivate | host service graph zero; process-resident assembly reported dormant rather than unloaded |
| Native transition during open | close/fail safely before teleport/load/title settles; no orphan EventSystem or input lock |
| Shutdown | best-effort reverse cleanup with explicit failure diagnostics; no `DolocTown.exe` or Steam waiting-for-exit regression |
| Saved command result | cleanup does not delete or duplicate an intentionally native-committed item/money/tech result |
| Unsaved command result | return-to-title/cold reload follows native rollback; cleanup does not promote it through a sidecar |
| Foreign modal/input owner conflict | acquisition fails without closing or resuming the other owner; no global `ResumeCurrentInput` |

## Behavior And Save-Mode Matrix

| Gate | Action | Required proof | Save mode |
| --- | --- | --- | --- |
| Typed input | Y open/close, Escape, button, ten short taps, hold, focus, retap | exact 1:1 transitions, no flicker or native menu/tool/item leak | `NoNativeSave`, third slot |
| Legacy input | exact retained DLL sequence | owner-targeted legacy close only, zero typed modal-Y, exact source provenance | `NoNativeSave`, third slot |
| Item UI | search/source/category/item pages, hover, left x1/right x10 | no stale rectangle hit; source-disabled/runtime-missing errors truthful | `NoNativeSave`, third slot |
| Read-only status | weather/time/location/item enumeration | same bounded rows and localization; no mutation | `NoNativeSave`, third slot |
| Reversible leases | movement, time scale, creative enable then close/title/deactivate | exact original/native state and patch/root zero | `NoNativeSave`, third slot |
| Working mutations | give, weather, money, tech, crop, spawn, time advance, then title without saving | target action readback in-session; prior committed state after cold reload; archive triple unchanged before cleanup | `NoNativeSave`, third slot |
| Teleport | whitelisted request and post-transition observation | request/complete states distinguished; modal/UI closes safely | `NoNativeSave`, third slot |
| Save here | explicit UI confirmation, one native save, cold reload | native commit success and exact committed result; no forged `SaveSaved` | `NativeSaveExpected`, disposable AutoCloud-isolated fixture |
| Save failure | fail native save after Working mutation | previous committed state; no false success or sidecar promotion | `NativeSaveExpected`, disposable fixture |
| Owner cleanup | close/title/re-entry/disable/Loader/shutdown | matrices above plus clean process exit | `NoNativeSave`, third slot |

## Later UI Rewrite Acceptance Matrix

The UI rewrite is a separate task after E4. It may change layout, navigation,
search/history and visual technology, but must preserve the accepted action and
lifecycle contracts:

| Area | Rewrite gate |
| --- | --- |
| Information architecture | separate item browser, world/time, movement, progression, spawn and dangerous/save actions; dangerous actions cannot be accidental adjacent clicks |
| Scaling/layout | 16:9, ultrawide, small window, fullscreen/resolution refresh and UI scale remain readable with no clipped controls |
| Paging/search | item/source/category/weather/teleport results have deterministic paging, bounded rows and visible empty/error states |
| Keyboard/focus | Y/Escape/hold/retap/focused-input matrix remains exact; Tab/arrows/Enter do not leak to gameplay |
| Mouse | hover follows the active cell; right-click x10 is cell `PointerDown` only; no stale screen-rectangle fallback |
| Modal | product never opens over a foreign modal; EventSystem reuse/fallback/cleanup and Escape drain remain verified |
| Command palette, if added | private allowlist only; parsed parameters bounded; dangerous native commands absent; history bounded and non-gameplay |
| Feedback | every mutation shows requested/applied values, native owner/result and failure reason; save action is visibly a native commit |
| Localization | Chinese/English/Auto cover labels, errors, action results and confirmation text without fixed-width truncation |
| Accessibility/safety | destructive or permanent actions require clear confirmation; no color-only state; creative/time-scale active state is persistent and obvious while leased |
| Performance | closed product performs zero UI work; open steady state does not rebuild unchanged object graphs or emit per-frame logs/allocations |
| Regression | full Compatibility and Owner cleanup matrices still pass; visual success cannot replace native command readback or save evidence |

## Public API And Platform Boundary

Implementation may later mark the exact retained DebugConsole and Diagnostic
families as warning-bearing `Diagnostic/Frozen` without changing binary
signatures. The new product consumes none of them.

Platform may own:

- typed owner-bound input registration and cleanup;
- generic modal arbitration/token lifetime;
- generic diagnostics/log output and future command registration semantics;
- Loader, Catalog, SDK/package, Doctor/Manager and owner cleanup coordination.

DebugConsole ProductNative owns:

- Y/Escape product policy and UI;
- action descriptors, parameter limits and native debug executors;
- the three behavior-equivalent input Prefixes and Escape drain;
- transient movement/time-scale/creative leases;
- product Canvas/EventSystem, filters, paging, localization and later command
  palette/history.

Manager remains the platform management UI. DebugConsole must not absorb Mod
enablement, dependency management, install repair or generic advanced
diagnostics already owned by Manager.

## Required Safety Clause

先做本轮 API/domain 的 native owner 方法体审查；未找到 native owner 或状态持有者前，不得通过 mod 层补丁冒充 API 重做完成。

The bounded action owners above are sufficient to plan extraction. They do not
make the official command parser, broad creative/progression verbs or modal
input ownership safe for ordinary mods.

## Disposition

**GO** to use this plan as the prerequisite for a later exact DebugConsole
admission Review.

**NO-GO** to admit the current Bootstrap-hosted product, move the file without
its input/modal lifecycle, expose the official command parser, create a second
Compatibility Host, or combine equivalent extraction with the UI rewrite.

**NO-GO** to infer an eleventh-product admission, general Advanced authoring,
complete Release, G7 or 0.5.5 publication from this Review.
