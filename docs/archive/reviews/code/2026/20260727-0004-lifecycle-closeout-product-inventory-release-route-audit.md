# Lifecycle Closeout, Product Inventory And Release Route Audit

**Date:** 2026-07-27
**Status:** recorded — Mine and DebugConsole independent acceptance passed;
exact twelve-product baseline frozen
**Reviewed HEAD:** `aec0130e`
**Reviewed commits:** `d7db2577`, `aec0130e`
**Owning Updates:** `20260726-0004-mine-eleventh-advanced-product.md`,
`20260726-0005-debugconsole-twelfth-advanced-product.md`

## Scope And Decision

This is the requested independent audit after the DebugConsole lifecycle-owner
closeout and Mine deployable-byte correction. It also inventories current
first-party products, measures the remaining mandatory Runtime boundary, and
separates pre-release implementation from the final Release/Workshop audit.

The lifecycle correction is accepted. No P0/P1/P2 remains in the reviewed
Mine/DebugConsole admission scope:

- the Compatibility Host exposes one shared `DebugActions` instance to both
  construction orders, while GameBridge owns its one action lifecycle pump;
- ProductNative `SaveLoaded` restores any old lease before publishing a new
  active session and fails closed when restoration fails;
- Compatibility Hook acquisition, rollback, cleanup debt and later retry are
  transactional;
- UI close failure restores the visible UI, Core modal token and Hook demand
  instead of losing the cleanup handle;
- the Mine SDK package contains the corrected current source bytes.

Mine and DebugConsole may therefore move from `implemented/open` to
`verified/closed`. This freezes exactly twelve admitted ProductNative products.
It does not admit a thirteenth product, implement Content Host G7, make Mine
publishable, or make Runtime 0.5.5 releasable.

No game process, complete Release, L0-L5, GC ladder or long-duration test ran
for this audit.

## Lifecycle Ownership Findings

### One Compatibility action owner

`CompatibilityHostFactory.cs:75-98` creates both the `DebugConsole` UI service
and `DebugActions` service from one stored action instance.
`CompatibilityHostBroker.cs:90-166` registers the nested instance as the
`DebugActions` lifecycle alias when the UI service is constructed first.
`DebugConsoleCompatibilityService.cs:85-102` no longer duplicates action
update, title or shutdown delivery.

GameBridge remains the one action update/save/title/shutdown pump:

- `DolocTownGameBridge.cs:256-281,313-318`;
- `DolocTownGameBridge.Hooks.cs:248-256`.

`tests/DTMAPI.UnitTests/Program.cs:5951-6037` covers both service-construction
orders and exactly-once lifecycle delivery.

### SaveLoaded fail-closed

`products/first-party/DebugConsole/src/DebugConsoleSaveSessionGate.cs:7-25`
restores the old session, publishes the new UI boundary, and only then marks
the new session active. `ModEntry.cs:112-128` uses that gate.
`Program.cs:6047-6087` proves a failed restore does not publish a new session
and that a later retry can succeed.

### Hook and UI transactions

`CompatibilityDebugConsoleInputHooks.cs:206-421,448-672` separates desired
from installed topology, retains `cleanupPending` after a combined patch and
rollback-unpatch failure, and proves physical owner state before accepting
cleanup. `Program.cs:5861-5886` covers the combined failure and later exact
cleanup retry.

`products/first-party/DebugConsole/src/Ui/DebugConsoleUi.cs:274-365` commits
`IsOpen=false` only after modal, Hook and Core release succeeds. A failure
reacquires the prior UI/modal/input state so a later close can retry.
`Program.cs:5888-5928` covers this path.

The retained 0.3.1 run
`GAME-SMOKE/20260727-132103` binds the exact `d7db2577` Runtime and
Compatibility Host. Across 4,886 action lifecycle updates it records zero
owner-wide Harmony cleanup operations, then reaches title/Loader zero while
preserving `NoNativeSave` data. This existing runtime evidence did not need to
be repeated after the documentation-only acceptance commit.

## Mine Deployable Bytes

The current package
`temp/batch6-mine-advanced-pilot/DTMAPI-Mine-advanced-pilot.zip` has SHA-256:

`848154FBB6816F0F0D18CBEF7A574539400FA6540C29A9B0A50F52BD3DECCE2B`

Its entry DLL and
`products/first-party/Mine/bin/dtmapi-author/DTMAPI.Mine.dll` are both
86,528 bytes with SHA-256:

`E2EEEA2BECE5A71B779A32BE4A4637D2C56C42E602CB7A0D08B61D4C0D4FCCBE`

The Advanced receipt SHA-256 is:

`01489C5DC760B627A1B36A5FB21470798EE406C0F024A774FEB5B491DF4DDC69`

These values match the owning Mine Update. The earlier accepted Mine game
matrix was not repeated because the correction changes restoration retention
and deployable provenance, not the already tested gameplay behavior.

Mine remains `RebuildBlocked` for public release because its borrowed well art
and runtime 2x presentation are prototype debt. Technical ProductNative
acceptance must not be described as Mine publication readiness.

## First-Party Product Inventory

“All admitted native products are split” and “all project-owned products are
finished” are different claims.

| Group | Current result |
| --- | --- |
| Admitted Advanced products | Twelve ProductNative products now have independent `products/first-party/*/src` ownership and are verified/closed |
| Public Workshop products | Ten current code products are in the twelve-product Advanced set; `ManboCardboardAudio` remains the one published first-party product on its old Strict C# route |
| Oil | Already a DLL-free official-JSON ContentPack; no C# split is required |
| AnimalPack | Catalog identity only; no canonical product source, pending the optional G7 content-host route |
| Hatch/Mole/Drecko/OilFloater | `NeverPublish` prototype inputs, not four admitted products |
| ShellCrab | `PrototypeBlocked`, outside this release wave |

`ManboCardboardAudio` is therefore the concrete unfinished published product.
Its target is a data-only audio ContentPack, but that conversion belongs with
the later Content Host/audio boundary rather than a thirteenth Advanced
admission in this release closeout.

## Runtime Weight And Remaining Boundary

At the reviewed tree, the five mandatory Runtime projects compile about:

- 148 source files;
- 60,403 physical lines;
- 54,093 non-empty lines;
- 1,938,944 bytes across the five built DLLs.

The AutoFishing pre-migration mandatory baseline was 80,097 physical lines.
The current mandatory tree is therefore lower by 19,694 physical lines
(24.59%). Relative to the ten-product mandatory snapshot
65,534 / 58,710 / 2,111,488, it is lower by 5,131 physical lines, 4,617
non-empty lines and 172,544 built bytes.

This is real default-loaded Runtime reduction. It is not repository, download
package or all-products-enabled reduction: the twelve ProductNative `src`
trees contain about 33,744 physical / 31,272 non-empty lines, and the optional
Compatibility Host retains old published ABI executors.

No current implementation of the twelve admitted products was found hidden in
mandatory Runtime. The largest remaining architectural candidates are:

- `Features/CustomAnimals`: about 3,807 physical lines;
- `Features/AudioReplacement`: about 3,450 physical lines.

Together they are about 31.65% of current mandatory GameBridge. Their intended
destination is the optional G7 Content Host/content products, not a larger
general C# Runtime API. They remain outside a “publish current optimizations,
do not redesign Mods” scope.

Other retained code is not presently safe to delete:

- CropHarvesting is a small frozen compatibility/API surface; AutoHarvest is a
  `NeverPublish` SDK demand sample, not a future first-party product;
- the Compatibility broker/proxies and old ABI executors protect already
  published consumers;
- Core CustomEntity registry and Lamp facades are frozen compatibility debt;
- Catalog, SDK, Doctor, Manager, ConfigMenu and lifecycle cleanup are Platform
  responsibilities rather than product residue.

## New Non-Blocking P2 Findings

### P2 — old update-bucket diagnostics have drifted from real scheduling

`DolocTownGameBridge.Features.cs:93` defines `UpdateGameBridgeFeatures`, but no
current caller uses it; `DolocTownGameBridge.Update.cs:5-8` invokes the
demand-routed `UpdateRuntimeAutomation` path instead.
`DolocTownGameBridge.Features.cs:93` and
`RefactorScaffoldOptions.cs:71` still expose bucketed/every-frame descriptions.
This is not evidence of current per-frame CustomAnimals/Audio file polling,
but it can mislead performance diagnosis and future maintenance.

Before the release candidate is frozen, remove the dead route or make the
diagnostic/configuration truth match the demand router. Focused source/unit
checks are sufficient unless the correction changes live scheduling.

### P2 — AutoHarvest authority wording is inconsistent

Catalog and the Author SDK correctly classify AutoHarvest as a
`NeverPublish` API-demand sample, while the current Phase 0 domain contract
still gives it a target `Yuuka.DTMAPI.AutoHarvest Advanced CodeMod`; the
matching dated ownership baseline preserves the same old projection. Correct
the live contract without rewriting the historical baseline so another task
cannot accidentally start a thirteenth product. Do not remove its frozen API
or create a first-party AutoHarvest Mod.

## Work Before The Frozen Release Candidate

These are implementation, fact-refresh or focused acceptance tasks. They are
not the complete Release suite:

1. synchronize the twelve-product/current-roadmap truth and close the two P2
   wording/scheduler drifts above;
2. perform the already-decided bounded private/internal naming and readability
   review without renaming public IDs, frozen ABI or serialized keys;
3. refresh the publicly obtainable Workshop/DTMAPI consumer inventory at the
   RC cutoff and add any newly found binary to the retained ABI matrix;
4. correct or explicitly focused-accept the recorded Author SDK
   install-to-game source-preflight ordering tail;
5. select the exact Runtime/product release wave, project each selected
   functional product to the decided 1.0.0 epoch, and rebuild the exact Runtime,
   Compatibility Host, SDK and selected product packages;
6. pass focused Catalog, ABI, Doctor, package/hash, no-QA and ownership gates;
7. run the current-candidate AutoFishing and ActionSpeed active-gameplay GC
   gate. The safe claim is bounded trend/lifecycle evidence, not “Unity GC is
   cured.”

Mine art/economy, Manbo/G7 conversion, AnimalPack, Oil economy, DebugConsole UX
rewrite and MoreSaves UI additions are product follow-ups unless the user
explicitly adds them to the release wave.

## Final Release And Workshop Audit

After the exact candidate is frozen and all focused gates are green, run one:

```powershell
tools/scripts/test.ps1 -Configuration Release
```

That suite builds Runtime/products/QA/Doctor/SDK and exercises the repository
Unit, QA, installer, uninstaller, upgrade, Catalog, Advanced deterministic
package, ABI, Doctor, artifact and document-governance gates. It does not
launch Doloc Town, run the real GC ladders, upload Steam, or prove the bytes a
subscriber receives.

The following remain release-operation audit work after the complete suite:

- create and audit the exact player Workshop package;
- exercise PowerShell 5.1/7, BAT wrappers, spaces/non-ASCII paths,
  install/check/status/uninstall and collect-logs behavior in a temporary
  player-like directory;
- upload the frozen package;
- re-download the Steam subscription artifact and compare exact hashes,
  layout and install behavior;
- run the smallest final subscription/runtime smoke required by the selected
  wave.

If the complete suite fails, use its focused failed gate and the unreached
diagnostic tail, batch corrections, then run one clean from-start suite on the
new frozen candidate. Do not create cached-pass receipts or restart the whole
suite after every repair.

## User Decisions At Audit Time

The existing A-W decisions remain in force. Only these current choices need
product authority:

1. whether to lift the 0.5.5 pause and enter RC preparation; this does not
   authorize Steam upload;
2. the release wave roster. The default low-risk route is Runtime 0.5.5 first,
   then the rebuilt 1.0.0 Advanced Workshop products in bounded waves, while
   excluding Mine, Manbo, Oil and AnimalPack until their own blockers close;
3. whether the release claim needs stronger GC evidence than the existing
   bounded current-candidate gate. A claim of quantified allocation budgets or
   “GC solved” would require stronger profiling/longer evidence.

The scheduler/document cleanup, naming review, external-consumer refresh,
package rebuild and focused gates are engineering defaults, not additional
product votes.

### Resolution On 2026-07-27

The user authorized bounded work from the two P2 truth corrections through
final-candidate freeze, but not the complete Release, Workshop package
acceptance or Steam upload. Runtime `0.5.5` goes first; the later Advanced
waves are AutoFishing, MoreEquipmentSlots, then the remaining eight public
Advanced products. The retained Manbo subscription package must run without
load or registration errors on the final Runtime candidate. The selected
public GC sentence and the post-0.5.5 CustomAnimals/AudioReplacement/AnimalPack
direction are recorded without implementing G7 in the
[0.5.5 prerelease route](../../../planning/2026/20260727-dtmapi-055-prerelease-roadmap.md)
and its [lifecycle Update](../../../updates/2026/20260727-0001-dtmapi-055-prerelease-route.md).

## Validation

Passed on the reviewed tree:

- `debugconsole-product`, `compatibility-host` and `mine-product` focused Unit;
- complete `DTMAPI.UnitTests`;
- Product Catalog: 27 products / 11 public / 21 Workshop items / 48 API rows;
- Batch 6 Phase 0;
- document governance: 6,007 checked items;
- `git diff --check`.

The complete Unit build retains ten existing nullable warnings in the
reflection-heavy DebugConsole UI. They are non-blocking compile debt and are
not a lifecycle-ownership regression.
