# 20260717-0002 Batch 4 Post-Closure Source Boundary Review

Status: recorded

Date: 2026-07-17

Reviewed branch/HEAD: `codex/major-update-batch0-20260713` at `8bf018505a0108235533de03bec46ab4b9e611fe`

Scope: independent post-G8 review of Batch 4 source ownership, optional-QA separation, ordinary-player behavior, package shape, route compliance, and admission to Batch 5

Owning follow-up Update: [20260717-0002 Batch 4 Source Boundary Reopen](../../../updates/2026/20260717-0002-batch4-source-boundary-reopen.md)

Historical Batch 4 Update: [20260715-0019 Batch 4 Optional QA Host Extraction](../../../updates/2026/20260715-0019-batch4-qa-host-extraction.md)

Frozen dependency map: [20260715-0011 Batch 4 QA Extraction Dependency Map](20260715-0011-batch4-qa-extraction-dependency-map.md)

Prior acceptance review: [20260716-0001 Batch 2/3 Supplement And Batch 4 Detailed Acceptance Review](20260716-0001-batch2-batch3-batch4-detailed-acceptance-review.md)

## Source Request

The user requested another detailed Batch 4 review after several interrupted implementation passes and asked that source, route, package, regression, and newly exposed boundary details be checked rather than accepting the latest completion record at face value.

This review is documentation and admission analysis. It does not change Runtime source, install to the game, stage QA in the shared Runtime, launch Doloc Town, edit Workshop subscriptions, or invalidate successful historical G8 runs.

## Verdict

There is no P0. The G8 implementation slice remains real and valuable: the main G4/G5/G6 case bodies, assertions, forced-GC scenario, settings, routing, and cleanup policy now live in the optional QA assembly; the old per-frame file poll is gone; production has no static project reference to QA; and the candidate player package contains the same five production DLLs with no QA payload.

Batch 4 as an overall route milestone is nevertheless **reopened**. Two P1 ownership violations remain in player assemblies, and the ordinary no-QA frame still traverses one P2 null relay. The current semantic inventory locks already classified text against drift, but its broad classifications bless these remaining production-owned QA behaviors instead of proving that they have a production owner. Batch 5 remains inadmissible until corrective G9 closes the two P1s and replays the exact ordinary-player paths that expose them.

| Priority | Finding | Admission effect |
| --- | --- | --- |
| P0 | None. | Do not roll back the valid G8 extraction or its lifecycle cleanup. |
| P1 | Ordinary Y-console, AnimalViewer, and EquipmentSlots production code still owns QA screenshot/evidence/file/status policy. | Reopen Batch 4; these behaviors contradict G4/G7 player-absence requirements. |
| P1 | QA-only or orphan mutation controls and synthetic-input adapters remain compiled into Core/GameBridge and alter production state machines. | Reopen Batch 4; renaming `ForSmoke` to `ForFixture` did not establish a production owner. |
| P2 | Every ordinary frame still enters the optional-host updater and several production Hook callbacks still relay QA observations before returning on a null participant. | Small bounded overhead, but the strict no-QA-updater target remains incomplete. |

## P1-1 - Player Product Paths Still Own QA Evidence Policy

### Y console

The production Bootstrap UI arms evidence capture on the first ordinary console open through `screenshotPending = !screenshotRecorded` in [`ReflectedDebugConsoleUi.cs`](../../../../src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs#L214). Its ordinary update then prepares a synthetic hover specimen and invokes screenshot capture in the open console path at [lines 546-552](../../../../src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs#L546).

The capture method creates `DEBUG-CONSOLE-UI/<timestamp>`, requests a Unity screenshot, writes `summary.txt`, selects a QA-specific item specimen, and publishes `Smoke.DebugConsoleScreenshot`/`Smoke.DebugConsoleModItemUi` at [lines 1773-1812](../../../../src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs#L1773). The sample selector even gives special treatment to Workshop item `3722791728` and `mod_butter` at [lines 1885-1893](../../../../src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs#L1885). This is QA evidence policy, not the production responsibility to render and operate the Y console.

### AnimalViewer

Production Harmony callbacks invoke `RecordAnimalViewerUiEvidence` and `RecordAnimalPanelUiEvidence` without an active-QA condition in [`DolocTownHookCallbacks.cs`](../../../../src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs#L368). Once an ordinary configured AnimalViewer has renderable progress rows, the production service creates `ANIMAL-001/<timestamp>`, requests a screenshot, writes a summary, and publishes both product and `Smoke.*` statuses in [`AnimalViewerService.cs`](../../../../src/DTMAPI.GameBridge.DolocTown/Features/AnimalViewer/AnimalViewerService.cs#L277). A second delayed screenshot/summary append remains in the same player service at [line 330](../../../../src/DTMAPI.GameBridge.DolocTown/Features/AnimalViewer/AnimalViewerService.cs#L330).

### EquipmentSlots

The production equipment service still contains a QA-only evidence adapter which creates `EQUIPMENT-SLOTS-UI/<timestamp>`, captures a screenshot, writes a summary, and publishes `Smoke.NewContentEquipmentSlotsUi` in [`DolocTownExperimentalBridgeApi.EquipmentSlots.cs`](../../../../src/DTMAPI.GameBridge.DolocTown/Features/EquipmentSlots/DolocTownExperimentalBridgeApi.EquipmentSlots.cs#L1750). Current repository callers are QA scenarios, so the adapter has no demonstrated ordinary product owner.

### Route conflict

The dependency map requires G4 to move screenshots, debug-console QA driving, AnimalViewer evidence, and UI/content observation behind the active QA host while preserving only real UI behavior ([G4 lines 160-168](20260715-0011-batch4-qa-extraction-dependency-map.md#L160)). G7 then requires the player package to have no QA test root/listener/updater and the ordinary no-receipt run to show no QA activity ([lines 188-194](20260715-0011-batch4-qa-extraction-dependency-map.md#L188)). These production-owned evidence writers therefore remain P1 even though they are bounded and do not recreate the old every-frame file poll.

## P1-2 - QA-Only Mutation And Synthetic Input Remain In Player Assemblies

The optional QA assembly now owns the case sequencing, but several scenario-only controls still live inside production state machines:

- `ActionSpeedService.SuppressActionSpeedAutoFillForFixture` is production state at [`ActionSpeedService.cs:40`](../../../../src/DTMAPI.GameBridge.DolocTown/Features/ActionSpeed/ActionSpeedService.cs#L40) and directly bypasses the normal auto-fill path at [line 246](../../../../src/DTMAPI.GameBridge.DolocTown/Features/ActionSpeed/ActionSpeedService.cs#L246). The observed non-test setter is the QA ActionSpeed case at [`ActionSpeedFixtureCase.cs:694`](../../../../src/DTMAPI.GameBridge.DolocTown.QA/Scenarios/Fixtures/ActionSpeedFixtureCase.cs#L694).
- `DolocTownExperimentalBridgeApi.ForceMachineProductionDueForFixture` remains production state at [`DolocTownExperimentalBridgeApi.cs:106`](../../../../src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs#L106) and makes the machine loop treat production as due independently of native TU progression at [`DolocTownExperimentalBridgeApi.MachineProduction.cs:708`](../../../../src/DTMAPI.GameBridge.DolocTown/Features/MachineProduction/DolocTownExperimentalBridgeApi.MachineProduction.cs#L708).
- Legacy fishing retains `SuppressFishingAutoCastForFixture`, forced no-water/no-rod/fish/native-bite flags, and a pool override at [`LegacyFishingAutomationService.cs:121-131`](../../../../src/DTMAPI.GameBridge.DolocTown/Compatibility/FishingAutomation/LegacyFishingAutomationService.cs#L121). Those fields alter real product branches at [lines 320](../../../../src/DTMAPI.GameBridge.DolocTown/Compatibility/FishingAutomation/LegacyFishingAutomationService.cs#L320), [390](../../../../src/DTMAPI.GameBridge.DolocTown/Compatibility/FishingAutomation/LegacyFishingAutomationService.cs#L390), and [707](../../../../src/DTMAPI.GameBridge.DolocTown/Compatibility/FishingAutomation/LegacyFishingAutomationService.cs#L707).
- Core still owns `RecordSyntheticInputFrameForFixture` and `RecordSyntheticInputTapForFixture` at [`DtmApiRuntime.cs:566-581`](../../../../src/DTMAPI.Core/Runtime/DtmApiRuntime.cs#L566). They clear and inject the real input frame rather than exposing a read-only native observation.

The dependency map explicitly says feature helpers must move with their test-only cases and only real production repair or ordinary-Mod operations may survive ([lines 95-114](20260715-0011-batch4-qa-extraction-dependency-map.md#L95)). G7 requires all former `ForSmoke` surfaces to be audited, with proven orphans deleted and surviving production operations renamed only after ownership is established ([lines 188-194](20260715-0011-batch4-qa-extraction-dependency-map.md#L188)). The current controls are internal rather than public, which avoids a public ABI leak, but internal visibility does not make scenario mutation a player responsibility.

## P2 - Ordinary Frames Still Traverse A QA Null Relay

Every production frame unconditionally calls `UpdateQaHostParticipant()` in [`DolocTownGameBridge.Update.cs:5-10`](../../../../src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.Update.cs#L5). With no activation receipt, the method returns after reading a null participant at [`DolocTownGameBridge.QaHost.cs:121-125`](../../../../src/DTMAPI.GameBridge.DolocTown/QaHost/DolocTownGameBridge.QaHost.cs#L121); it does not call the optional assembly, allocate a scenario, or touch the filesystem.

Several existing production UI Hook callbacks also relay `NotifyQaHostUiObservation` before the same null check, for example [`DolocTownHookCallbacks.cs:171-276`](../../../../src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs#L171). This is far smaller than the deleted `SmokeUpdate` and is not a GC conclusion, but it remains inconsistent with the strongest G7 wording that an ordinary player has no QA updater. It should be removed or converted to an activation-installed delegate after the P1 ownership fixes.

## Correctly Completed G8 Boundaries

- Production `QaHost/**` is four files and 608 physical lines; the optional QA project is 42 C# files and 15,950 physical lines on the reviewed tree.
- `QaScenarioController`, G4/G5/G6 cases, case assertions, forced `GC.Collect`, performance probes, native-continuation policy, and scenario cleanup are physically owned by `DTMAPI.GameBridge.DolocTown.QA`.
- Production projects contain no static project reference to the QA project. QA references Core and GameBridge inward through friend/internal access.
- QA activation is a once-at-start receipt/hash/version check. The old `smoke-settings.json` per-frame file probe is absent.
- The earlier public Core fixture methods were removed. Remaining fixture members found in public containing assemblies are internal; no new public Abstractions QA API was found.
- Run-scoped Harmony cleanup and G6 owner/Camera cleanup from the final G8 lifecycle correction remain valid historical evidence and are not reopened by these source-ownership findings.
- The inspected Runtime candidate contains exactly five production assemblies and no forbidden QA payload.

## Why Existing Gates And Runtime Evidence Missed This

The schema-3 semantic inventory in [`batch4-production-qa-semantic-inventory.json`](../../../../tools/release/batch4-production-qa-semantic-inventory.json) hashes lines matched by a vocabulary pattern and restricts the four-file neutral host, but it explicitly classifies the whole DebugConsole and AnimalViewer evidence surfaces as product-owned. The gate therefore proves that the approved classifications have not drifted; it does not prove that the classification decision was correct. Production mutation helpers similarly pass as frozen compatibility or product-native primitives even when their only behavioral purpose is a QA case.

The final no-QA lane `GAME-SMOKE/20260717-003431` proved no receipt/root/load attempt, slot-3 load, eleven owners, ordinary hotbar input, profile restoration, and process exit. It did not open Y console, enter a configured AnimalViewer, or exercise EquipmentSlots UI. It therefore could not reveal automatic evidence creation in those paths. The package and Workshop audits correctly prove artifact shape and installer behavior, but a five-DLL/no-QA package cannot prove that those five DLLs contain no QA policy.

## Required G9 Regression Matrix

| Lane | Required acceptance |
| --- | --- |
| Source/IL ownership | Reject production screenshot/evidence-directory/summary/`Smoke.*` composition that exists only for QA; reject QA-only `ForFixture` mutation and synthetic-input controls. Preserve explicitly justified production diagnostics separately. |
| No-QA Y console | No QA receipt or DLL; open, use, and close the real Y console; UI remains functional and no new `DEBUG-CONSOLE-UI` QA evidence tree is created. |
| No-QA AnimalViewer | Load the third save with the real configured product, open/switch the animal viewer, preserve progress UI behavior, and create no `ANIMAL-001` QA evidence tree. |
| No-QA EquipmentSlots | Exercise the real equipment UI and recovery behavior with no QA receipt, no screenshot/summary evidence, and no loss of orphan-recovery behavior. |
| Staged QA UI evidence | Explicitly stage the receipt-bound QA host and prove the migrated DebugConsole/AnimalViewer/EquipmentSlots evidence still reaches the run-owned QA evidence root. |
| Staged QA mutation | Replay ActionSpeed, machine, fishing, input, owner, save/load, and cleanup cases after production mutation flags are removed or replaced with reviewed neutral native operations. |
| Player/package closure | Full Release, semantic source/IL gate, exact five-DLL/no-QA package, Workshop audit, exact eleven-product enabled/disabled lanes, stable exit, and exact external-state restoration. |

These short architecture runs do not replace the independent AutoFishing/ActionSpeed GC ladders. ISSUE-010 and ISSUE-011 remain open and outside the proof offered by G9.

## Recommended G9A-G9E Order

1. **G9A - reclassify residue:** extend the machine-readable inventory to distinguish production diagnostics from QA evidence policy and enumerate every remaining `ForFixture` mutation/synthetic-input member with an owner decision.
2. **G9B - move evidence ownership:** move Y-console, AnimalViewer, and EquipmentSlots screenshot/directory/summary/status composition into the optional QA assembly while preserving ordinary UI behavior.
3. **G9C - remove scenario mutation from player state:** move or replace ActionSpeed, machine, fishing, synthetic-input, and other orphan fixture controls; retain only narrow operations with a documented production/native owner.
4. **G9D - close ordinary-player residue:** remove the no-QA per-frame null updater where feasible and tighten source/IL tests so broad classification cannot whitelist new QA policy.
5. **G9E - replay admission:** execute the regression matrix above and update Catalog only after both P1s and the exact ordinary-player UI paths pass.

## Release-Package Hygiene Note

The current temporary candidate is useful evidence, not a publication authority. It reports commit `8bf018505a01`, DTMAPI `0.5.5`, 30 files, 70,918,209 bytes, and zero forbidden QA entries. The Workshop stress matrix passed with no blockers in a path containing spaces and non-ASCII characters; see [stress-summary.md](../../../../tmp/Batch4%20Workshop%20%E5%AE%A1%E8%AE%A1%E8%AF%81%E6%8D%AE/DTMAPI%20Workshop%20Audit%2020260717-135413/Results/stress-summary.md).

Any stale `dist` tree and the current `tmp` candidate must still be regenerated from the exact clean publication commit. That is release hygiene, not a Batch 4 source blocker, and does not weaken the package audit result.

## Admission Decision

Keep G8 as a completed implementation and evidence checkpoint, but withdraw the conclusion that Batch 4 overall is accepted. Continue under G9 Update `20260717-0002`; do not enter Batch 5 until both P1 ownership findings are removed, the semantic gate enforces the corrected ownership, and the no-QA plus staged-QA regression matrix passes.

## Resolution

Resolved by the G9 implementation and validation owned by [Update 20260717-0002](../../../updates/2026/20260717-0002-batch4-source-boundary-reopen.md). This Review remains the frozen source-boundary diagnosis; completion details and evidence belong to that Update, the focused Hook map, and the active smoke matrix.
