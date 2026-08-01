# Sixth Product Admission And MoreSaves Commit-Range Audit

**Review ID:** `20260723-0003`
**Date:** 2026-07-23
**Status:** recorded — keep the MoreSaves extraction, but NO-GO for freezing the six-product baseline as verified or admitting a seventh product until the focused findings below close
**Scope:** commits `c42fc8b4..19cb81c5`, the tracked source and documentation at `19cb81c5`, focused source/unit/package/evidence review only; no new implementation, game launch, complete Release, L0-L5, GC ladder or long test

## Source Request

The user requested an audit of the commits made after
[`20260723-0001`](20260723-0001-four-task-closeout-and-next-route-audit.md).
The reviewed range contains the Animal refresh correction, the existing
Compatibility Host prerequisites, MoreSaves admission, the atomic sixth-product
extraction, and its corrected bounded game acceptance.

The unrelated untracked portable reverse-capture Update, tool directory and
builder are outside this Review.

## Verdict

The MoreSaves extraction is a real ProductNative split and should not be
reverted:

- the new product owns only fixed enabled `12` / disabled `6` policy,
  configuration, lifecycle reconciliation and direct
  `GameManager.archiveFileCount` read/write;
- it installs no Harmony patch and does not take ownership of official save
  files, discovery, load/save/delete/copy or UI rendering;
- the frozen old ABI executor is physically in the one optional Compatibility
  Host, while mandatory GameBridge retains a thin demand proxy;
- `GAME-SMOKE/20260723-150215` is valid evidence for the new product path,
  including initial and post-title twelve-slot official panels, third-save load,
  real Loader deactivation to native six, zero product Harmony patches, exact
  restoration of 36 archive paths and clean exit;
- the measured default-loaded source reduction is real: the admitted SaveSlots
  boundary changed from `887 / 781` physical/non-empty lines to `136 / 111`,
  removing `751 / 670` from the mandatory path. This is not a claim that total
  repository, package or download size fell.

The range nevertheless does not satisfy its own `verified/closed` claim. One
installation P1 from `20260723-0001` remains open, the frozen ABI has behavioral
regressions, the new product adds an avoidable permanent frame-event
allocation source, and release/document authorities contradict the admitted
state. MoreSaves may remain the sixth implemented product, but Update
`20260723-0004` should be treated as `implemented/open` until the focused
corrections below pass. A seventh product remains blocked.

## P1 — Current 0.5.5 Can Be Doctor-Green Without Its Required Host

`InstalledRuntimeVersionProbe.ValidateOptionalComponents` returns immediately
when both current receipts omit `OptionalComponents`. The positive five-DLL test
constructs exactly that installation and expects `Consistent`.
`check-dtmapi-status.ps1` has the same early return.

This installation cannot satisfy the current Runtime topology: the first frozen
ABI call reaches the broker, which requires the Catalog-owned Compatibility
Host receipt and bytes and must reject the installation. Therefore Doctor and
Status can still report green for a topology the Runtime rejects.

The correction is version-aware and small:

- current `0.5.5` receipts must both contain exactly the one Catalog-owned Host
  row;
- explicitly supported older receipt versions may retain their historical
  no-component shape;
- add one negative Doctor/Status case in which both current receipts omit the
  Host.

This does not require another Host schema, receipt family, game run or complete
Release suite.

## P1 — Frozen `ISaveSlotsApi` Behavior Regressed During Extraction

The old public ABI signatures and provider identity remain present, but three
observable behaviors changed:

1. `GetState` for an unregistered owner calls the common `BuildState`, which
   always sets `IsConfigured=true`; the previous executor returned
   `IsConfigured=false` and `not-configured`.
2. A cold disabled registration with no current compatibility lease reads the
   native value and returns success without normalizing it to six. The previous
   implementation executed the native six-slot write for a disabled request,
   matching the frozen matrix wording “disabled requests normalize to native
   six.”
3. `UpdateStates` assigns the same status to every owner. In a mixed enabled and
   disabled owner set, the disabled owner's state is therefore reported as
   configured/enabled-path status rather than preserving its disabled state.

MemberRef/hash checks cannot catch these semantic regressions. Add focused
behavior tests for unregistered state, cold disabled registration, and mixed
enabled/disabled owners, then restore the previous fixed-six/twelve
observables. No old DLL or game launch is needed for this correction.

## P1 — MoreSaves Creates A Permanent Frame-Event Allocation Source

`MoreSaves.ModEntry` subscribes to `UpdateTicked` unconditionally for the entire
loaded owner lifetime. Core creates a new `UpdateTickedEventArgs` whenever the
slot has a listener, while `MoreSavesNativeRuntime.Update` immediately returns
on almost every healthy frame because no retry is pending.

This makes a rare 750 ms missing-manager/restore retry keep the high-frequency
event route live permanently. It conflicts directly with the project's known
long-session Unity/Mono GC concern and the established demand-activation
direction. The bounded product smoke is not a long-session allocation proof and
does not invalidate this source finding.

Subscribe only while `RetryPending` is true, or use an existing low-frequency
event with equivalent bounded behavior. Add a focused steady-state assertion
that healthy MoreSaves retains no frame listener/demand. This source/lifecycle
correction does not require a GC ladder or long test; a new short game smoke is
needed only if the native owner/deactivation behavior itself changes.

## P1 — Release Version Authority Cannot Represent The Migrated Product

The Catalog correctly records three different facts for MoreSaves:

- current source: `1.0.0`;
- retained published Workshop version: `0.3.1-dtmapi`;
- migration target: `1.0.0`.

`check-release-contract.ps1` still requires every public product's
`publishedVersion == sourceVersion` and also requires
`sourceVersion != targetVersion`. Those assertions cannot pass for a product
that has completed the agreed 1.0.0 source reset while its last public Workshop
version remains retained separately. The publish text row also remains
`0.3.1-dtmapi` while the current manifest and official-info are `1.0.0`.

The same commit changed the unrelated ChestLocatorEnhancer publish text version
to `1.0.0`, while its Catalog, manifest and official-info all remain
`0.3.1-dtmapi`. That is an accidental cross-product release projection change,
not a ChestLocator migration.

Keep the three version axes distinct:

- validate current source artifacts and candidate publish text against
  `sourceVersion`;
- preserve the already-public Workshop baseline against
  `publishedVersion`;
- use `targetVersion` as a migration target only until the product reaches it;
- restore ChestLocator's publish row unless a separate migration authority
  changes its source.

The focused release-contract runner also requires explicit Author SDK artifact
roots; a direct no-artifact invocation exits before producing a contract
verdict. No complete Release was run in this audit.

## P1 — Current Route Authorities Contradict Each Other

The canonical lightweight roadmap still says that exactly five products are
admitted, MoreSaves is unselected, and the sixth product is blocked. The Batch 6
identity contract and MoreSaves Update say six products are admitted.

Two additional current-fact projections are stale:

- the Phase 0 machine contract still assigns MoreSaves slot/page patches,
  migration-pending wording and the deleted `testmods/MoreSavesMod` source;
- the managed identity contract still lists MoreSaves among manifests that omit
  `Type`, although the new manifest explicitly declares
  `Type=CodeMod` and `CodeModKind=Advanced`.

`20260723-0001` also ordered the remaining Phase 1/Phase 4 tails before a sixth
product. The later admission Review does not explicitly supersede that route or
explain why those tails stopped being admission blockers. The sixth
implementation need not be reverted, but the route must be reconciled before
work is selected for a seventh product.

## P2 — The Exact Component-Move Recovery Windows Are Still Untested

The upgraded transaction matrix now carries real Host bytes, one old component
sentinel and `OptionalComponents` receipts through success, later rollback,
fully committed interruption recovery and uninstall under PowerShell 7 and
Windows PowerShell 5.1. That repairs the empty-component test from
`20260723-0001`.

Its injected phase list still omits `MovingOldComponents`,
`PlacingComponents` and `ComponentsCommitted`. The installer's special
inference branches for a moved directory whose boolean receipt was not yet
updated are therefore not exercised. Update `20260723-0003` overstates the
result as “every rollback phase.”

Add the missing component move/place interrupted fixtures to the existing
matrix. This is a focused script correction, not a reason to rerun the complete
suite.

## P2 — Final-Deactivation Retry Evidence Does Not Match Loader Scheduling

The ProductNative runtime correctly fails closed after a native-six restoration
failure: it retains the exact lease and prevents a second writer. The Unit then
manually calls `MoreSavesNativeRuntime.Update()` on the deactivated object and
describes that as the due retry.

In the real Loader path, Core removes the owner's event registrations even when
`Dispose` fails. It retains the failed disposable instance so a later explicit
owner-deactivation pass can retry `Dispose`, but there is no surviving
`UpdateTicked` scheduler for the Unit's manual call. Therefore “one automatic
retry demand after final owner deactivation” is not proven for the product.

The low-complexity contract is:

- while the owner remains active, manager-late/config-disable retries may use
  the bounded scheduler;
- a final deactivation restoration failure retains the lease, fails cleanup
  visibly and requires restart or a later explicit cleanup pass.

If automatic post-deactivation retry is truly required, it needs an existing
platform cleanup participant; the isolated product Unit must not manufacture
that scheduler.

## P2 — Negative Doctor Coverage Does Not Protect The Actual-Bytes Check

The broker's pre-load PE inspection is correct and the wrong name/version/TFM
fixtures prove no rejected assembly becomes resident.

The Doctor negative tests, however, primarily mutate one receipt. They do not
replace the disk DLL while coherently changing both receipts, so they can fail
at receipt disagreement before exercising actual PE identity/version/framework
validation. Add one dual-receipt substituted-byte case so a later removal of the
PE check cannot pass unnoticed.

## P2 — Review And Historical Authority Lifecycle Drifted

The admission Review `20260723-0002` began as a conditional admission decision,
then was edited to carry implementation details, independent acceptance and
`verified/closed` state. Review governance freezes substantive Review content
after implementation starts; lifecycle completion belongs to Update
`20260723-0004`.

Restore the Review as the admission-time decision and add only a short
resolution link. Keep implementation and `150215` evidence in the Update.

The source audit `20260723-0001` also remains untracked even though the new
commits claim to fix it. Commit that historical Review with a short resolution
link or explicitly supersede it; do not rewrite its original NO-GO finding.
The unrelated portable reverse-capture files must remain isolated.

## Findings That Did Close

- AnimalHusbandryProgress no longer performs the former repeated 80 ms full
  refresh in healthy state; its focused source/unit evidence matches the
  ProductNative boundary.
- The Compatibility Host broker validates actual simple name, assembly version
  and target framework before `Assembly.Load`; wrong candidates remain absent
  from the AppDomain.
- The existing Host is the only optional framework component. No second Host,
  adjacent receipt family or parallel assurance system was added.
- MoreSaves uses the existing Catalog, SDK, policy, package and owner-cleanup
  paths and retains zero Harmony patches.
- The real `150215` evidence supports the product path and exact player-save
  restoration. It does not prove all old retained consumers executed in the new
  Host under Unity, and the documents should not make that broader claim.

## Validation Performed

- Catalog checker: PASS, `products=27`, `public=11`, `workshop-items=21`,
  `api-rows=48`.
- MoreSaves source/identity/zero-Hook checker: PASS.
- focused Unit `compatibility-host`: PASS.
- focused Unit `moresaves-product`: PASS.
- focused Unit `moresaves-acceptance-routing`: PASS.
- focused Unit `api-metadata`: PASS.
- Install Doctor tests: PASS, 11 cases; one positive case preserves the
  no-Host false-green behavior described above.
- Runtime upgrade transaction matrix: the two supported PowerShell hosts pass
  the current 15-case matrix; the omitted exact phases remain untested.
- `git diff --check`: PASS before this Review was added.
- document governance: blocked only by the isolated untracked portable Update
  lacking its monthly index row; no MoreSaves Update/index mismatch was found.
- no game, complete Release, L0-L5, GC ladder or long test ran during this
  audit.

Passing the existing focused tests does not close findings for behaviors they
do not assert.

## Next Route

Use small reversible corrections and the existing test families:

1. require the one Host projection for current 0.5.5 Doctor/Status receipts and
   add the dual-omission/substituted-byte negative cases;
2. restore frozen SaveSlots behavior and demand-activate the MoreSaves retry
   event; correct final-deactivation retry wording/tests;
3. separate source/published/target version checks and undo the unrelated
   ChestLocator projection drift;
4. add the two exact component move/place interruption fixtures;
5. reconcile roadmap, Phase 0 contract, identity contract, Review lifecycle and
   the untracked historical audit, then return Updates `0003`/`0004` to verified
   only after their focused gates pass;
6. complete or explicitly re-order the still-open Phase 1/Phase 4 tails before
   selecting a seventh product.

These corrections need focused build/unit/Doctor/Catalog/release-contract/
transaction checks only. Preserve `GAME-SMOKE/20260723-150215`; do not rerun
MoreSaves game acceptance, a GC ladder or a long test unless the native
owner/deactivation behavior changes. The full slots 7–12 lifecycle matrix
remains a later 0.5.5 release gate rather than a prerequisite for these source
corrections.

## Resolution

The findings above remain the audit record and are not rewritten as
implementation history. Existing Updates
[`20260723-0003`](../../updates/2026/20260723-0003-moresaves-admission-prerequisites.md)
and
[`20260723-0004`](../../updates/2026/20260723-0004-moresaves-sixth-advanced-product.md)
own the corrections. Focused MoreSaves/Compatibility Unit, Install Doctor,
Status, Phase 0, Catalog, release-contract and the Runtime transaction matrix
on both PowerShell hosts (17 cases each) close the listed P1/P2 source/tooling
gaps without another game run;
`GAME-SMOKE/20260723-150215` remains the bounded player evidence. The six-product
baseline may return to `verified/closed`; a seventh product remains blocked by
its own admission and the explicitly reordered Phase 1/4 tails.
