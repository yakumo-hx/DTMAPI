# MoreEquipmentSlots Post-Fix Compatibility Entry Audit

Date: 2026-07-30
Status: `recorded / production P0=0 P1=0 / test-causality P2=1 / publication blocked`
Audited HEAD: `206fbce76b70886eead1956d91de4b08141bfb8f`
Production correction commit: `5a6c59b4cc682a66b041f8ad4d9d577dd9f7ee87`
Owning implementation Update:
[`20260723-0008`](../../../updates/2026/20260723-0008-more-equipment-slots-eighth-advanced-product.md)

## Scope

This is a read-only post-fix audit of the six commits from `5a6c59b4` through
`206fbce7`. It reviews the three post-freeze P1 corrections, cold Compatibility
demand, focused test causality, the frozen MoreEquipmentSlots package and the
entry boundary for player compatibility testing.

No game, Runtime install, native save, complete Release, L0-L5, GC or long test
was run. This Review does not modify the implementation or reopen already
accepted U2 evidence beyond the paths actually executed.

## Finding

### P2: the late cross-process loser test does not identify its rejection cause

[`MoreEquipmentSlotsProductTests.cs`](../../../../tests/DTMAPI.UnitTests/MoreEquipmentSlotsProductTests.cs)
removes the optional per-hash claim before releasing the child process, but the
child still treats any `InvalidDataException` as the expected permanent-winner
rejection.

That is insufficient causal evidence. If permanent-winner matching regressed,
the already migrated-away global source or the deterministic archive could
still make a later source-state check throw `InvalidDataException`, leaving the
test green for the wrong reason.

Before claim crash/resume is accepted, make the child expose and assert the
specific permanent-winner rejection stage or reason. This is a focused
test-evidence correction; no production change, game run or complete Release is
required merely to close it.

The final source/package Review `20260730-0007` therefore overstates both:

- that the test proves the permanent winner alone caused rejection; and
- that no P2 remained.

Its wording that cold demand is recomputed at “each lifecycle boundary” is also
broader than production. The exact behavior is each relevant `SaveLoaded` cold
probe.

## Effective Production Corrections

No new production P0 or P1 was found. The audited code closes the prior three
P1 findings:

1. A deterministic `GlobalFlat` archive permits another save's empty state only
   when its filename hash matches its bytes and exactly one canonical Product
   authority has the corresponding `GlobalFlat` migration stamp. Missing,
   mismatched and ambiguous bindings remain fail-closed.
2. Optional per-hash evidence publication after a completed T0 winner and valid
   T1 Product now validates terminal authority instead of reapplying the stale
   strict T0 revision rule.
3. Product v3 read, previous fallback, write and pre-schema publication require
   non-negative stored and current native `TotalGameSeconds`.

The process-lifetime cold-storage cache is gone. Production calls the probe from
`SaveLoaded`; it does not poll files from `Update` or a per-frame path. Probe
failure is visible and retried on a later relevant load.

## Focused Validation

Independent checks at the audited HEAD:

```text
Release DTMAPI.UnitTests build = PASS
  errors = 0
  existing DebugConsole nullable warnings = 10
DTMAPI_UNIT_TEST_FOCUS=moreequipment-product = PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-cold-host = PASS
DTMAPI_UNIT_TEST_FOCUS=compatibility-host = PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-acceptance-routing = PASS
check-product-catalog.ps1 = PASS (27 / 11 / 22 / 48)
check-doc-governance.ps1 = PASS (6031 checks)
check-test-artifact-governance.ps1 = PASS
git diff --check = PASS
```

One shell timeout left duplicate focused-test processes briefly active, so the
cleanup script correctly refused to race them. They exited normally; the later
cleanup found zero candidates. This was orchestration noise, not a product or
test failure.

## Package And Runtime Boundary

The two independently built MoreEquipmentSlots ZIPs remain byte-identical:

```text
entries = 7
ZIP bytes = 65,237
ZIP SHA-256 =
  EDB7BF80240CE86C859CBD09BBBD9EE22131B38D9A865142EB3747C1767EEF70
Product DLL bytes = 168,448
Product DLL SHA-256 =
  03E3651C0E1F5258E4CE44EA38605F7AD047DD507C33B1E76DB0A29E5F639A60
Advanced reference receipt SHA-256 =
  1F7AB81DD6B521E03858DD2745E84BD47D1DDA65ED15EF1FE4C3654B7D22F6D9
```

The package declares Product `1.0.0`, minimum Runtime `0.5.5`, Advanced
CodeMod, `netstandard2.0` and tracked game build `23762374`. It remains
`RebuildBlocked` in the Catalog and is not a publication candidate.

The latest frozen Runtime candidate still records
`BuildCommit=493de436d2f7`. Mandatory GameBridge sources changed after that
boundary, including the audited EquipmentSlots cold-demand and lifecycle
corrections. The old Runtime bytes therefore must not be used as the final
compatibility or publication input.

## Verdict And Next Boundary

Publication is not authorized. MoreEquipmentSlots remains
`implemented/acceptance-open`; claim crash/resume, C0, U1, U3/U4 and the
migrated-save gameplay semantic spot check remain open.

Compatibility testing may begin after a focused current-HEAD Runtime 0.5.5
test candidate is rebuilt and frozen. A complete Release is not a prerequisite
for that rebuild or bounded matrix.

Recommended order:

1. correct and rerun the one late-loser causal assertion;
2. build and freeze a current-HEAD Runtime 0.5.5 test candidate;
3. run U1 and U3/U4 before Runtime R0;
4. run C0, claim crash/resume and the migrated-save semantic spot check before
   MoreEquipmentSlots R2;
5. after all player gates pass, promote exact final bytes, remove only the
   intended Catalog block, and enter the final complete Release and Workshop
   player-package audit.

Each failed matrix stage follows the existing focused-rerun rule; it does not
restart the whole sequence.
