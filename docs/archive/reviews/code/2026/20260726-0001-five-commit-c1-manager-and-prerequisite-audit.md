# Five-Commit C1, Manager And Prerequisite Audit

**Review ID:** `20260726-0001`

**Date:** 2026-07-26

**Status:** recorded — Zoom accepted; C1 and Manager remain implemented/open;
Mine prerequisite requires one product decision; DebugConsole prerequisite is
accepted as planning authority only

**Reviewed range:** `9387e229..5aecad73`

**Scope:** independent review of the five commits after Review
`20260725-0001`: Zoom active-scale refresh coverage, legacy `testmods` physical
classification, Manager/GMCM player information architecture, Mine admission
prerequisite and DebugConsole extraction prerequisite. This Review does not
change source, admit an eleventh product, implement Mine or DebugConsole,
authorize Content Host G7, publish 0.5.5, launch the game, or run a complete
Release/GC/long-test ladder.

## Verdict

The five commits make real forward progress, but the combined current HEAD is
not fully green:

- `e7276b88` correctly closes the final Zoom P2 and may remain
  `verified/closed`.
- `83d01e41` reaches the intended C1 physical directory layout without adding
  player Runtime files. Its Update must return to `implemented/open` until the
  live Unit-test consumers of deleted `testmods` paths are migrated.
- `d97c6f6d` adds useful Manager paging, registry-backed dependency/restart
  projection and an Advanced diagnostics split without absorbing gameplay
  policy. It remains `implemented/open`: the current Catalog gate is red and
  the new player-facing interactions/localization were not actually accepted.
- `a5ebef1a` correctly rejects the current Mine prototype and correctly keeps
  Mine policy out of GameBridge, but it silently converts the scheduler into
  durable per-save sidecar state. That is an unresolved product/economy
  decision, not a native-owner consequence. The prerequisite must be narrowed
  before admission.
- `5aecad73` is a sound DebugConsole prerequisite: equivalent ownership
  extraction precedes visual rewrite, the old ABI reuses the existing
  Compatibility component, native actions stay private, and save-bound
  mutations follow native commit semantics. It authorizes planning only.

No P0 was found. Three P1 findings and two P2 findings remain.

## Findings

### P1 — current Catalog authority is red after the Manager source change

`check-product-catalog.ps1` fails at current HEAD:

```text
Batch 4 production source file count: expected '255', got '254'
Batch 4 currentDebt metric claim is stale or missing:
255 production source files
```

The Catalog still freezes `254` production source files in both
`semanticBoundary.productionSourceFiles` and the prose `currentDebt`. The
current semantic projection sees the additional Manager production source and
reports `255`. This is precisely the drift the Catalog gate is intended to
catch.

Relevant paths:

- `src/DTMAPI.Core/Manager/ManagerPagination.cs`
- `tools/release/dtmapi-product-catalog.json`
- `tools/release/batch4-production-qa-semantic-inventory.json`
- `tools/scripts/check-product-catalog.ps1`

This is a small source-authority correction, not a reason to rerun a game
acceptance. Recompute the existing projection and update its existing
authority; do not create another receipt or metric family.

### P1 — C1 moved the files but left executable Unit consumers on deleted roots

The physical `testmods` root is correctly absent, but the broad Unit source
still directly reads or builds from paths that no longer exist:

```text
testmods/AutoHarvestMod/ModEntry.cs
testmods/DebugConsoleMod/ModEntry.cs
testmods/OilMod/...
testmods/CropHarvestingQaMod/manifest.json
testmods/AutoHarvestMod/bin/.../AutoHarvestMod.dll
```

The active references occur in `tests/DTMAPI.UnitTests/Program.cs`, including
the AutoHarvest lifecycle, Batch 2 player-like smoke ordering, Oil ownership,
dependency and executable product-lifetime tests. The broad Unit run currently
fails earlier in the known Compatibility Host fixture because its temporary
release manifest is absent, so these later deterministic missing-file failures
are masked rather than disproved.

The C1 correction must:

1. map every live test to the Catalog-classified source root;
2. add `author-sdk/samples/api-demand` to any consumer scan that is meant to
   include API-demand samples;
3. keep old `testmods/...` strings only where a test explicitly asserts a
   retired path is absent or reproduces a historical receipt;
4. update `tests/README.md`, which still tells authors that game-process checks
   belong in `testmods`.

This is not a rollback of C1. The new physical classification is correct; its
remaining consumers are incomplete.

### P1 — Mine persistence scope was selected without a product decision

The Mine prerequisite correctly identifies current defects in configurable
power, `Enabled`, the unused mineral switch and native mutation restoration.
However, it then states that Mine **must** maintain per-save `Working` and
`Committed` scheduler state, possibly with a sidecar, archive fingerprints and
interrupted-promotion recovery.

Native ownership proves that inventory and appliance power are native
save-bound state. It does not prove that the Mod's next-due anchor must survive
title/restart exactly. Two valid product contracts remain:

1. **Session-derived scheduler — recommended for the first 1.0.0.**
   Clear product scheduler entries on `SaveLoaded`, title and owner cleanup;
   initialize the next due time from the newly loaded native clock. Persist no
   product scheduler sidecar. Native inventory and power are the only durable
   production results.
2. **Exact save-bound scheduler.**
   Preserve the due anchor/anti-reroll state through the existing save-commit
   infrastructure, with disposable native-save and interruption evidence.
   Choose this only if exact cross-session cadence is a deliberate economy
   promise.

Until that choice is made, admission must not inherit the second option by
default. The current “failure after every individual mutation” and entire
matrix also must not become one game-test atom: source/unit fault injection
should cover the mutation ladder, followed by the smallest real-game
activation, production, restoration and save-mode checks.

### P2 — Manager structure is implemented, but player UI acceptance is incomplete

The structural part is good:

- pagination clamps empty, stale and partial pages;
- dependencies come from the Content/Manifest Registry rather than a second
  resolver;
- Errors and Warnings no longer stack two windows;
- the compatibility-preserved Features route is presented as Advanced;
- no public API, gameplay patch or product command was added.

The final game route opened each page and asserted internal model rows, but it
captured only Status and Logs screenshots. It did not click or prove:

- Mod row selection and detail replacement;
- next/previous page behavior with enough rows;
- Errors/Warnings category switching;
- Registry/Feature switching under Advanced;
- empty, partial-last-page and stale-page behavior in the reflected Unity UI.

The two retained screenshots also show Simplified Chinese UI mixed with
hard-coded English. New strings such as `Selected Mod details`,
`Dependency issues`, `restart required`, `Registry evidence`, page labels and
empty messages bypass `DtmUiText`. The Status page still leads with technical
health counters, paths and English diagnostics even in the Chinese locale.

Therefore Update `20260726-0003` proves runtime wiring, NoNativeSave safety and
clean exit, but not the declared complete player-centred information
architecture. A bounded UI reacceptance should localize the new strings and
exercise the new controls at one normal resolution plus one constrained
window. It does not need another broad Release or long test.

### P2 — live documentation still exposes two obsolete C1 authorities

- `tests/README.md` still routes game-process checks to `testmods`.
- `tools/release/dtmapi-mod-publish-zh - 副本.md` remains a tracked historical
  copy under the live release-tool root and contains old `testmods` paths.

The Catalog already lists the latter as historically excluded, so it is not an
upload authority. Move it to an explicit archive location or add an
unmistakable historical header; do not leave it looking like a second current
release projection.

## Commit-by-Commit Disposition

| Commit | Result | Required follow-up |
| --- | --- | --- |
| `e7276b88` Zoom active-scale coverage | accepted | preserve the focused sequence; no broad gate |
| `83d01e41` C1 physical classification | implemented/open | migrate live Unit paths and stale C1 docs |
| `d97c6f6d` Manager player navigation | implemented/open | repair Catalog projection; localize and interactively accept new UI controls |
| `a5ebef1a` Mine prerequisite | correction facts accepted; admission plan conditional | choose session-derived versus exact save-bound scheduler, then shrink the validation atom |
| `5aecad73` DebugConsole prerequisite | accepted as plan only | later independent admission; execute E0-E4 as bounded phases |

## DebugConsole Scope Check

The DebugConsole Review does not repeat Mine's persistence mistake:

- ordinary give/money/weather/time/progression changes remain native Working
  state and are not written to a sidecar before native save;
- `Save here` is explicitly a native commit action and requires an isolated
  disposable fixture;
- movement, time scale and creative flags are reversible leases;
- the current 2,335-line UI executor is explicitly identified as mandatory
  Bootstrap product debt;
- the exact retained ABI is routed to the one existing Compatibility
  component rather than a second Host.

Its 500-line planning record is large because the product itself spans input,
modal UI and many native debug actions. Implementation must nevertheless keep
E0 freeze, E1 hidden ProductNative, E2 Compatibility relocation, E3 atomic
switch and E4 independent acceptance as separate bounded work. “Atomic switch”
must not be interpreted as one enormous source commit or one all-purpose game
run.

## Validation Performed

Passed:

- Release build of `DTMAPI.UnitTests`: zero warnings and errors;
- focused Unit `zoom-product`;
- focused Unit `manager-ui`;
- Release build and execution of `DTMAPI.QaUnitTests`;
- Batch 6 Phase 0 historical contract reproduction.

Failed:

- `check-product-catalog.ps1`: two current semantic-count/currentDebt
  projection failures;
- broad `DTMAPI.UnitTests`: known Compatibility Host fixture has no temporary
  `DTMAPI/release-manifest.json`; this failure occurs before the deterministic
  stale C1 path reads.

Not run:

- game launch;
- complete Release;
- L0-L5, GC gradient or long test;
- Mine or DebugConsole runtime acceptance, because neither is admitted.

## Route After Correction

1. Repair the existing Catalog projection and all live C1 Unit/doc paths.
2. Localize and perform one bounded interaction/visual acceptance for the
   Manager additions. Keep the implementation in Platform.
3. Decide Mine scheduler persistence. If session-derived is selected, remove
   the sidecar/native-commit ladder from the admission prerequisite.
4. Then choose exactly one next-product admission. The DebugConsole
   prerequisite is ready for a separate admission Review; Mine is not ready
   until step 3.
5. Keep 0.5.5 publication, broad ecosystem scan, final Release and active
   AutoFishing/ActionSpeed GC validation at their existing later boundary.

## Disposition

Do not revert Zoom, C1 physical moves or the Manager structural implementation.
Do not call the latest HEAD fully verified until the two red source authorities
and Manager UI acceptance are corrected. Do not admit Mine before the scheduler
persistence decision. The DebugConsole plan may proceed only to its separate,
exact admission decision.
