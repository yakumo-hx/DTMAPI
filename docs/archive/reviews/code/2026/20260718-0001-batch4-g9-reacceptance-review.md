# 20260718-0001 Batch 4 G9 Reacceptance Review

Status: recorded

Date: 2026-07-18

Reviewed branch/HEAD: `codex/major-update-batch0-20260713` at `d01c2ca7ee47461b3d876b7d192cb5e692d49c52`

Scope: independent re-audit of the G9 source boundary, executable semantic gate, current-tree runtime evidence, player package, route compliance, and admission to Batch 5

Owning G9 Update: [20260717-0002 Batch 4 G9 Source Boundary Closure](../../../updates/2026/20260717-0002-batch4-source-boundary-reopen.md)

Audit documentation Update: [20260718-0001 Batch 4 G9 Reacceptance Audit](../../../updates/2026/20260718-0001-batch4-g9-reacceptance-audit.md)

Prior diagnosis: [20260717-0002 Batch 4 Post-Closure Source Boundary Review](20260717-0002-batch4-post-closure-source-boundary-review.md)

Frozen route: [20260712-0003 Full Boundary Audit](20260712-0003-dtmapi-full-boundary-audit.md)

## Source Request And Boundary

The user requested a fresh decision on whether Batch 4 is actually complete and Batch 5 may begin. This review treats the final committed tree as new evidence rather than inheriting either the earlier G8 completion claim or the G9 implementation record's conclusion.

The only pre-existing working-tree change is `docs/reviews/api/native-owner-domains/03-animal-husbandry-behavior.md`. It is unrelated parallel animal analysis, was excluded from the Batch 4 diff, and was not modified by this audit. No game, Steam subscription, Workshop upload, or shared local Runtime was changed.

## Verdict

**Batch 4 is complete through G9 and Batch 5 may begin.** No P0, P1, or P2 admission blocker remains in the reviewed tree. The two P1 findings from the prior review are removed in source and enforced by executable rejection tests; the former P2 per-frame participant traversal is reduced to an activation-bound nullable delegate check and no longer enters a QA updater when QA is absent.

Batch 5 must start under its own Update. This verdict does not close ISSUE-010 or ISSUE-011, does not turn minute-scale G9 runs into GC evidence, and does not authorize publication of a temporary or historical package.

| Gate | Result | Basis |
| --- | --- | --- |
| Player source owns no UI evidence policy | passed | DebugConsole, AnimalViewer, and EquipmentSlots production roots contain no legacy evidence paths, screenshots, summaries, or scenario `Smoke.*` verdicts. |
| Player state machines own no QA-only mutation | passed | ActionSpeed, Machine, legacy Fishing, and Core synthetic-input fixture controls identified in the prior review have zero production-source matches. |
| No-QA recurring QA traversal | passed | The update loop performs only `qaHostFrameUpdate?.Invoke()`; attachment installs the delegate and close clears frame/save/workshop/UI delegates before participant teardown. |
| Semantic absence is executable | passed | Schema 5 reports `25/25` contracts, `149/149` forbidden patterns, `20` lifecycle contracts, `49` negative samples, and `4` boundary IL artifacts. Meta-negative execution rejects twelve source mutations, one Catalog projection mutation, and six receipt-set errors. |
| Staged-QA and ordinary no-QA behavior | passed | Actual retained result receipts cover all three UI paths, exact participant lifecycle/cleanup, no-QA absence, exact five Runtime DLLs, exact public owner set, restoration, and stable exit. |
| Exact eleven-product combinations | passed | Enabled and disabled current-tree runs pass exact owner/artifact/lifecycle/cleanup assertions. |
| Final-commit player package | passed | A fresh `d01c2ca7ee47` Runtime-only candidate has exactly five receipt-bound player DLLs, zero forbidden QA payload, PowerShell 5.1 parser success, and zero subscription-audit blockers. |
| Frozen route | passed | Work remains Batch 4 QA ownership/extraction only; no Batch 5 demand activation or GC conclusion was smuggled into G9. |

## Source Ownership Recheck

### UI evidence policy

- `ReflectedDebugConsoleUi` retains ordinary UI visibility, input, hover, filter, and close state only. Optional QA owns DebugConsole screenshot timing and verdict composition.
- `AnimalViewerHookBridge` owns constructor Postfix, `Show` Prefix/Postfix, and `AnimalPanelUiState.Unregister` Postfix. The former evidence-only `AnimalPanel.RefreshViewer` Hook is absent. Production publishes neutral visibility/close receipts and clears only DTMAPI-owned cloned rows and derived keys.
- EquipmentSlots retains neutral bind/render/interaction/close observation. Optional QA owns screenshot path, summary, and scenario result.

Production-source scans found no `DEBUG-CONSOLE-UI`, `ANIMAL-001`, `EQUIPMENT-SLOTS-UI`, evidence-capture helper, or corresponding `Smoke.*` UI verdict symbol in the five Runtime roots.

### Fixture mutation and input

The prior `SuppressActionSpeedAutoFillForFixture`, `ForceMachineProductionDueForFixture`, legacy Fishing suppress/force/pool overrides, and Core synthetic frame/tap injection are absent from production. QA now advances Mine and ActionSpeed work across real frames through neutral operations and owns synthetic fishing input locally.

Some read-only internal methods still end in `ForFixture`, including state summaries and native containment/policy observations. They do not mutate product state and fit the accepted neutral facade. Their names should be reconsidered during the planned whole-code readability pass, not treated as a Batch 4 or Batch 5 admission defect.

### Optional participant dispatch

`DolocTownGameBridge.Update` still pays one nullable delegate guard per frame. This is not the old P2: no-QA startup never installs the delegate, so the frame does not enter `UpdateQaHostParticipant`; close nulls the delegates before participant cleanup. A dedicated branch in the hot loop could remove even the guard, but that micro-optimization belongs to measured Batch 5 work and is not an unfinished QA ownership boundary.

## Evidence Recheck

The retained `result.json` receipts, not only the smoke ledger summaries, were checked:

- staged QA: `GAME-SMOKE/20260717-235930` DebugConsole, `20260718-000107` EquipmentSlots, and `20260718-000232` AnimalViewer;
- ordinary no-QA: `GAME-SMOKE/20260718-005546`, including real Y, EquipmentSlots hover/close, two causal AnimalViewer renders, native-close ordering, unchanged legacy evidence, zero QA markers, exact save/profile restoration, and stable exit;
- exact products: `GAME-SMOKE/20260718-000350` enabled eleven and `20260718-000521` disabled eleven;
- migrated continuation: `GAME-SMOKE/20260717-221222`, `222930`, `224027`, and `224136`.

These are functional/lifecycle acceptance only. The independent AutoFishing and ActionSpeed `1x -> enabled without acceleration -> common multiplier -> high multiplier -> disabled recovery -> title cycle` ladders remain later 0.5.5 release gates.

## Package Recheck

The earlier pre-commit candidate had correct binary receipts but stale Git provenance. This audit rebuilt a Runtime-only candidate from the final G9 commit without installing it to the real game:

- candidate: `tmp/Batch4 G9 独立复核 RuntimeOnly d01c2ca7ee47 20260718-022734987 中文 With Spaces/DTMAPI`;
- identity: Runtime `0.5.5`, binary `0.5.5.0`, `PackageKind=workshop-runtime`, `BuildCommit=d01c2ca7ee47`;
- payload: exactly Abstractions, Bootstrap, Core, GameBridge, and ModConfigMenu DLL receipts; no QA/Smoke/Test/HookProbe DLL, activation receipt, or QA settings;
- audit: `tmp/Batch4 G9 独立复核 Workshop Audit 20260718-022801450 中文 With Spaces/DTMAPI Workshop Audit 20260718-022801/Results/stress-summary.md` passed ten Windows PowerShell 5.1 parser checks and the eight-case matrix with `Blockers: 0`.

`dist/workshop-packages` remains historical. The new ignored `tmp` tree is audit evidence, not a durable publication folder; any package after Batch 5 must be rebuilt and audited from that later exact clean commit.

## New Finding And Correction

One non-runtime documentation drift was found: the public API matrix still described the removed `AnimalPanel.RefreshViewer` Hook and 2026-06-12 evidence as the current AnimalViewer implementation. Update `20260718-0001` corrects the row to the G9 four-patch boundary and current staged/no-QA evidence. No public API signature or stability status changes; `IAnimalViewerApi` remains Experimental.

## Batch 5 Admission Boundary

Create a separate in-progress Batch 5 Update and keep its scope to the frozen recurring-work/demand-activation sequence:

1. replace per-frame content filesystem signatures with lifecycle/change-driven invalidation or true pre-work throttles;
2. remove unnecessary `LoadedMods.ToArray` and entry snapshots from hot paths;
3. measure feature-dispatch and diagnostics costs before changing cadence;
4. introduce the demand catalog/coordinator for API consumer, content definition, mandatory safety repair, and explicit QA demand;
5. apply it first to low-risk Hook-only products, then Camera/AnimalViewer, then Audio/CustomAnimals;
6. do not demand-disable title layout repair or Equipment orphan recovery until those mandatory owners are separated and proven.

The first Batch 5 baseline must preserve schema-5/meta-negative/Catalog success, exact five-DLL/no-QA package shape, ordinary no-QA UI behavior, staged-QA positive behavior, and the exact eleven-product enabled/disabled matrix. GC ladders remain independent release evidence rather than Batch 5 completion shorthand.
