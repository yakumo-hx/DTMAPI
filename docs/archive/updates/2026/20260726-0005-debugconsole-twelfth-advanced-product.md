# DebugConsole Twelfth Advanced Product

## Metadata

- Update ID: `20260726-0005`
- Date: `2026-07-26`
- Lifecycle Status: `verified`
- Validation Level: `docs,source,unit,runtime,player`
- Runtime Validation: `passed`
- Related Issue State: `verified`

## Source Request And Authority

After separately closing and committing Mine, complete the independent
DebugConsole admission and physical split. Admission authority is Review
[`20260726-0002`](../../reviews/api/2026/20260726-0002-debugconsole-admission-prerequisite-review.md).

The implementation boundary is behavior-equivalent extraction only:

- DebugConsole remains the published optional Diagnostic product
  `DTMAPI.DebugConsoleMod`;
- the SDK-generated Advanced product owns typed Y/Escape control, UI,
  allowlisted native actions, three input-isolation Prefixes and reversible
  leases;
- the exact retained 0.3.1 ABI reuses the existing dormant-shipped
  Compatibility component;
- mandatory Runtime retains only Platform input/modal/lifecycle coordination
  and thin frozen-ABI routing;
- no public command parser, arbitrary native command exposure, new Host,
  SharedNative gameplay owner or UI redesign is authorized.

## Implemented Atomic Switch

1. `DTMAPI.DebugConsoleMod` is now an SDK-generated
   `netstandard2.0` Advanced product with exact Harmony owner
   `dtmapi.mod.dtmapi.debugconsolemod`.
2. The product owns typed SaveLoaded `Y`/`Escape`, the owner-bound Core modal
   token, the Unity Canvas, three input-isolation Prefixes, fifteen bounded
   creative/action patches, allowlisted native actions and reversible
   movement/time/creative leases.
3. The new product consumes none of the frozen `IDebugConsoleApi` or public
   Diagnostic action APIs. Its action ports are private ProductNative
   interfaces and it exposes no command parser or arbitrary native command
   lane.
4. The exact retained 0.3.1 provider/UI and all seven frozen action executors,
   including their creative/input compatibility Hooks, live under separate
   Harmony owner `dtmapi.compatibility.debugconsole.legacy` in the existing
   optional Compatibility component. Mandatory GameBridge retains only a thin
   reflected broker plus the unrelated mail owner; it contains no old action
   body and does not load the Host until an old ABI call occurs.
5. Mandatory Core retains only the generic owner-bound custom-menu seam and
   typed input/lifecycle cleanup. Bootstrap no longer contains the DebugConsole
   Canvas; GameBridge no longer installs or dispatches DebugConsole-native
   Prefixes.
6. Canvas visibility is applied synchronously in the product's main-thread
   modal-acquisition callback. This closes the modal-frame suppression gap
   without restoring an unconditional platform UI pump.
7. Product-owned English and Simplified Chinese UI dictionaries cover every
   current UI key; a missing translation now falls back to player text instead
   of exposing `debug.*` identifiers.
8. QA observes the exact owner-bound `DTMAPI.DebugConsoleMod` modal rather than
   the frozen Compatibility API, so its screenshot cannot pass against the
   wrong UI owner.
9. ReturnedToTitle releases the complete product UI owner graph immediately;
   Canvas, EventSystem, Button, InputField, ScrollRect, listeners, dynamic
   binders and the root are all zero before Loader deactivation.
10. Movement, time-scale and creative changes use exact original-value leases.
    Cleanup protects foreign movement writes, retries retained restore
    failures, propagates cleanup failure, and never substitutes `MoveScaler=0`
    or `timeScale=1` for an unknown native original.
11. `Save here` is a two-click, eight-second confirmation. The first click
    cannot enter native `SaveGame`; native rejection is surfaced and clears
    the confirmation, and the next attempt must be confirmed again.
12. The Compatibility Hook owner now separates desired demand from installed
    topology. Repeated stable-frame writes are no-ops, modal-to-drain handoff
    preserves the existing three-Prefix topology, and the duplicate
    Compatibility action update was removed.
13. Compatibility input and creative installation/removal are transactional:
    partial acquisition is rolled back, failed removal reconstructs the exact
    prior topology when possible, and a failed UI open releases the modal,
    desired suppression and visible UI before propagating the failure.
14. ProductNative and Compatibility inspect and reject the other exact Harmony
    owner before physical installation. A frozen Diagnostic consumer can no
    longer create a dual DebugConsole owner after the product has loaded, and
    the reverse order is rejected as well.
15. Product disposal is retryable. A failed native restoration no longer marks
    the instance disposed, so Core's retained second cleanup pass can restore
    the exact original before disposal completes.
16. Movement, time-scale and creative leases now stage original, attempted,
    observed and uncertain state before a native write. A write-success plus
    readback/rollback failure retains an exact-original ledger and blocks a new
    mutation until a later cleanup proves restoration.
17. The disposable-save acceptance now covers both missing persistence rows:
    a rejected save leaves the prior committed money exact after cold reload,
    while a successful save cold-reloads the exact new value.
18. The Compatibility broker registers the nested `DebugActions` instance as
    the single action-lifecycle alias even when `DebugConsole` is constructed
    first. The UI service never pumps or resets that instance itself, so both
    construction orders receive exactly one frame, save, title and shutdown
    callback.
19. Product SaveLoaded is fail-closed: retained movement/time/creative state
    must restore exactly before the new save session is published active.
20. A failed partial-Hook rollback retains an explicit cleanup tombstone.
    Later reconciliation or shutdown observes and removes the exact owner
    topology without adding per-frame Harmony queries.
21. Compatibility UI close is transactional. If modal/Hook/Core release
    fails, the complete visible open state and Core token are restored for a
    later retry; the UI cannot become invisibly closed while retaining modal
    ownership.

## Changed Files

- Product and authoring authority:
  `products/first-party/DebugConsole/DTMAPI.DebugConsole.csproj`,
  `dtmapi.author.json`, `manifest.json`, `official-info.json`, `README.md`,
  `i18n/*`, and `src/**`;
  `author-sdk/advanced-reference-policies/doloctown-23762374-debugconsole-v1*`
  plus the tracked policy registry.
- Platform/Compatibility split:
  `src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs` and deleted
  `ReflectedDebugConsoleUi.cs`;
  `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs`;
  the lazy proxy in
  `src/DTMAPI.GameBridge.DolocTown/CompatibilityHost/`;
  `src/DTMAPI.GameBridge.DolocTown.Compatibility/DebugConsole/**` and its
  project/factory wiring;
  removal of the old mandatory GameBridge DebugConsole demand/callback routes.
- QA and gates:
  `src/DTMAPI.GameBridge.DolocTown.QA/**`,
  `tests/DTMAPI.UnitTests/**`, `tests/DTMAPI.QaUnitTests/Program.cs`,
  Catalog/release/install/build scripts and the game-smoke runner.
- 2026-07-27 correction:
  `HarmonyReflectionPatcher.cs`,
  `CompatibilityDebugConsoleInputHooks.cs`,
  `CompatibilityDebugConsoleRuntimeAdapter.cs`,
  `DebugConsoleCompatibilityService.cs`,
  `CompatibilityHostBroker.cs`,
  `DebugConsoleSaveSessionGate.cs`,
  ProductNative Hook/UI/action lease sources, two focused Unit fixture
  projects, and the G5 disposable-save fixture/runner route.
- Authorities/evidence:
  this Update, the monthly ledger, Batch 6 contract, public API matrix,
  focused Hook map, ISSUE-014, ISSUE-015 and the active smoke matrix.

## Validation

- `DTMAPI.UnitTests: OK (debugconsole-product)`; focused tests cover 240
  warmed frames with zero Hook/topology/status change, modal-to-drain
  handoff, partial input/creative acquisition rollback, failed-removal prior
  topology reconstruction, both owner-installation orders, retryable disposal,
  and movement/time/creative write-success plus readback/rollback failures.
  The fixture uses real Harmony patch metadata for both-order owner exclusion;
  the injected backend covers transition faults deterministically.
- The QA project builds in Release with zero warnings/errors, and
  `test-game-smoke-save-modes.ps1` passes the four new phase/mode/isolation
  routing assertions.
- The retained ABI gate passes under current PowerShell and Windows PowerShell:
  eleven exact retained DLLs resolve across ten frozen compatibility
  consumers, the old Y-console MemberRefs match exactly, mandatory GameBridge
  has zero heavy-executor markers and the optional Host contains the executor
  implementation.
- Catalog, build, packaging and SDK policy gates pass. The corrected SDK
  package is
  `temp/debugconsole-fix-candidate/DTMAPI-YKeyConsole-advanced-pilot.zip`
  with SHA-256
  `094F033F9CDE8E9270679A5F3DDDAC32B775DC5E1B9611397A897FA3984C9C58`;
  entry DLL SHA-256 is
  `BFD30F44FE3A861B22B1524E5BC106EB9B753964A744FB4019764ADFAC5432B8`;
  Advanced reference receipt SHA-256 is
  `250C3A760A61BE6ACCF443EAB5CE1FB6946A0BF3F664F7B3320F7AC8D75D025A`.
- Current-product third-save `NoNativeSave`
  `GAME-SMOKE/20260727-083919` executes inventory, weather, teleport, time,
  movement and Advanced actions through `nativeOwner=ProductNative`. It proves
  exact movement/time/creative restoration, title-time zero UI graph, exact
  Loader deactivation, unchanged player save/committed sidecars, no fatal
  window and clean process/QA exit.
- Exact retained 0.3.1/current-Host third-save `NoNativeSave`
  `GAME-SMOKE/20260727-085929` verifies the retained Workshop DLL SHA-256
  `E5A34963C0B66D6168104AF27DB849D707EE644F07917D8274868F8B8299B41E`,
  old Y modal behavior and all seven action groups through
  `nativeOwner=Compatibility`. ReturnedToTitle records the legacy owner UI
  graph at zero before Loader cleanup; save/sidecar, provenance, process and
  QA cleanup checks pass. This is historical successful-path evidence from
  before the edge-triggered Hook correction; its 804 cleanup entries are the
  reproduction for ISSUE-015, not proof of the corrected steady state.
- AutoCloud-isolated `NativeSaveExpected`
  `GAME-SMOKE/20260727-090702` proves first click makes zero native save calls,
  injected native failure is propagated, confirmation clears, a fresh
  first-click again makes zero calls, and the confirmed retry completes one
  real native save. The disposable fixture is removed and final health,
  isolation, process, QA and no-fatal gates pass.
- The old-DLL input runner uses 120 ms single-send taps: a diagnostic 40 ms run
  showed that the retained 201-button rebuild can miss a short physical tap.
  This changes only the external acceptance sender duration, not the frozen
  old-DLL behavior or Compatibility implementation.
- Disposable `NativeSaveExpected`
  `GAME-SMOKE/20260727-104118` mutates money
  `210726 -> 210863`, rejects native save, and retains the old committed
  expectation `210726`. Fresh `NoNativeSave`
  `GAME-SMOKE/20260727-104214` cold-observes exact `210726` with zero native
  save calls and unchanged archive/committed-sidecar metadata.
- Disposable `NativeSaveExpected`
  `GAME-SMOKE/20260727-104302` mutates and successfully saves
  `210726 -> 211019`. Fresh `NoNativeSave`
  `GAME-SMOKE/20260727-104359` cold-observes exact `211019` with zero native
  save calls and unchanged archive/committed-sidecar metadata. The isolated
  fixture is deleted only after this terminal observation.
- Old-Compatibility attempts `104513` and `104654` are explicitly
  non-acceptance. The first selected the LocalDevelopment product instead of
  the Workshop source. The second reached external-input readiness but its
  runner was externally timed out before the matrix began; the game then
  exited normally, player archive triples were proven byte/metadata unchanged
  before cleanup, and the exact profile/QA/source state was restored. Neither
  attempt is used to claim current-byte Compatibility player acceptance.

## Lifecycle Ownership Correction 2026-07-27

Independent Review
[`20260727-0003`](../../reviews/code/2026/20260727-0003-mine-debugconsole-transaction-fix-audit.md)
found four remaining transaction gaps plus one stale full-Unit assertion. The
source correction now:

- gives the nested action executor one observable broker alias and proves
  exactly-once lifecycle callbacks for both service construction orders;
- retries retained ProductNative restoration before a new SaveLoaded session
  can become active;
- retains `cleanupPending` after combined patch/rollback-unpatch failure and
  proves later shutdown removes the exact owner patch;
- keeps Compatibility UI, Hooks and Core modal ownership coherently open when
  close fails, then proves a retry reaches zero; and
- replaces the removed `RemoveOwnerResources` test contract with the current
  action/console proxy cleanup boundary.

The repository-local complete Unit entry point and focused
`debugconsole-product` entry point pass. The QA Release build passes with zero
warnings/errors; Product Catalog and Batch 6 G0 identity gates pass. The
current SDK candidate is
`temp/debugconsole-lifecycle-candidate/DTMAPI-YKeyConsole-advanced-pilot.zip`
with package SHA-256
`858FF9F40236A68FE64970BF7860FC6A20D254FD1F296A08EDADF51906012916`,
entry DLL SHA-256
`3BAB7F65EB03BBAD7CBA2FF4F47C0ACB0E956B00C358DCA311F2E20170E4E2D2`
and Advanced receipt SHA-256
`D81618FCD930F329192A4C8688DF9661BF9AEB7BE9489392FACB071C2A7A2D04`.

Exact-HEAD Runtime/Host evidence
`GAME-SMOKE/20260727-132103` now passes the retained 0.3.1 third-save
`NoNativeSave` route:

- installed Runtime provenance is `BuildCommit=d7db257747a2`; the optional
  Compatibility Host is 630,272 bytes with SHA-256
  `9920410DCF153D6FACB287A9F48E159FC8A9E4BE2D08159AB5352F454612DCE9`;
- the selected Workshop DLL is the exact retained
  `E5A34963C0B66D6168104AF27DB849D707EE644F07917D8274868F8B8299B41E`;
- all Y/Escape, ten short-tap, long-hold, post-hold close and seven bounded
  action gates pass through `nativeOwner=Compatibility`;
- eight open/close cycles publish exactly sixteen input-topology edges; the
  one additional transition clears creative demand. Across 4,886 action
  lifecycle updates there are zero old owner-wide Harmony cleanup entries and
  no unchanged-frame topology publication;
- ReturnedToTitle records Canvas, EventSystem, Button, InputField, ScrollRect,
  listeners, binders, root and exact Compatibility patches at zero; QA,
  profile, source, process and Loader cleanup pass; and
- player archives and committed sidecars are unchanged before cleanup, with no
  routine byte backup or archive writeback.

`130236` is non-acceptance because `WorkshopValidation` correctly rejected an
offline run without an active author session. Its retained QA stage was
preserved under that evidence root before exact external-state recovery.
`131832` used the corrected `PlayerWorkshop` route and proved the UI-only
steady state, but remained red solely because the existing title gate requires
an executed `nativeOwner=Compatibility` action; `132103` supersedes it without
changing the gate. Attempts `104513` and `104654` also remain non-acceptance.

## Evidence

- Current product `NoNativeSave`:
  `docs/debug/evidence/GAME-SMOKE/20260727-083919/`.
- Corrected exact-current Host/old 0.3.1 `NoNativeSave`:
  `docs/debug/evidence/GAME-SMOKE/20260727-132103/`.
- Historical old 0.3.1 Hook-churn reproduction:
  `docs/debug/evidence/GAME-SMOKE/20260727-085929/`.
- Isolated `NativeSaveExpected`:
  `docs/debug/evidence/GAME-SMOKE/20260727-090702/`.
- Failed-save/cold-old-committed pair:
  `docs/debug/evidence/GAME-SMOKE/20260727-104118/` and
  `docs/debug/evidence/GAME-SMOKE/20260727-104214/`.
- Successful-save/cold-new-committed pair:
  `docs/debug/evidence/GAME-SMOKE/20260727-104302/` and
  `docs/debug/evidence/GAME-SMOKE/20260727-104359/`.
- Diagnostic runs `083018`, `083456`, `084507`, `085127`, `085600` and
  `090305`, plus non-acceptance `104513`/`104654`/`130236`/`131832`, exposed
  QA owner selection, Host resource ownership, release
  manifest drift, short-tap loss, runner provenance/owner false negatives and
  pre-bootstrap disposable-profile/source-selection ordering. They are
  root-cause or recovery evidence, not acceptance claims.

## Rollback

Revert the final DebugConsole implementation commit as one atomic unit. Do not
restore only the old Bootstrap UI or only the old GameBridge executors; that
would create dual or missing UI/native ownership.

## Follow-Up

Independent Review
[`20260727-0004`](../../reviews/code/2026/20260727-0004-lifecycle-closeout-product-inventory-release-route-audit.md)
accepted the lifecycle alias, SaveLoaded gate, Hook cleanup debt, transactional
UI close and exact-current retained route with no remaining P0/P1/P2.
DebugConsole is therefore `verified` as the twelfth ProductNative product.
A future visual/UX redesign remains a separate bounded
Review/Update; this implementation only makes the behavior-equivalent product
surface coherent and localized. Thirteenth products, G7 and the 0.5.5 release
remain blocked. The four-run disposable save matrix and `132103` are the
current player evidence.

The install-to-game flow currently moves the selected managed product
destination before its source preflight/update; this run recovered with an
exact guarded backup and transactional reinstall, but installer ordering
remains a separate follow-up rather than part of DebugConsole admission.
