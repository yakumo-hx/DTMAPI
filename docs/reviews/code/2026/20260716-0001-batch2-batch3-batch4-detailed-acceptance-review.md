# 20260716-0001 Batch 2/3 Supplement And Batch 4 Detailed Acceptance Review

Status: recorded
Date: 2026-07-16
Scope: review the supplemental Batch 2/3 closure and the interrupted Batch 4 implementation against the frozen major-update route, source boundaries, release evidence, player package, and next-batch admission gate
Related Update: `docs/updates/2026/20260716-0002-batch234-detailed-acceptance-correction.md`
Primary route: `docs/reviews/code/2026/20260712-0003-dtmapi-full-boundary-audit.md`
Batch 4 dependency map: `docs/reviews/code/2026/20260715-0011-batch4-qa-extraction-dependency-map.md`
Implementation owners: `docs/updates/2026/20260714-0004-batch2-version-release-authority.md`, `docs/updates/2026/20260715-0016-batch3-author-sdk-preview.md`, `docs/updates/2026/20260715-0018-batch3-player-doctor-closure.md`, `docs/updates/2026/20260715-0019-batch4-qa-host-extraction.md`

## Source Request

The user requested a detailed review of the supplemental Batch 2/3 work and the newly completed Batch 4 because implementation had been interrupted several times and the later model did not independently use parallel sub-agent review.

This review preserves the successful build, package, transaction and runtime evidence while independently checking whether each claimed milestone satisfies its architectural completion gate. It does not treat passing tests as proof that code is in the correct assembly.

## Executive Verdict

- Reviewed branch/HEAD: `codex/major-update-batch0-20260713` at `7e62d050` (`Complete Batch 4 optional QA host extraction`), initially clean.
- Batch 2 remains complete and route-compliant.
- Batch 3 remains complete after the Author SDK preview and the separate player Doctor closure.
- Batch 4 is **not complete**. It has a valid optional-host skeleton, no recurring `smoke-settings.json` poll, correct project-reference direction, and a QA-free five-DLL package, but most executable scenario bodies remain compiled into the player GameBridge.
- No confirmed P0 product defect was found. Four P1 acceptance/boundary findings block entry into Batch 5. P2 evidence and tooling debts do not reopen Batch 2 or Batch 3.
- Current placement is Batch 4 corrective closure, equivalent to a new G8 after the intended G7. Do not start Batch 5 recurring-work/demand activation until G8 passes.

## Route Status

| Batch | Route result | Evidence-backed position |
| --- | --- | --- |
| Batch 2 — version/release authority | complete | One Runtime authority projects `0.5.5` / `0.5.5.0` / compatibility AssemblyVersion `0.5.3.0`; eleven product projections, retained ABI, Runtime transactions and player matrices are closed. |
| Batch 3 — Author SDK/player Doctor | complete | Author build/deploy/repair authority is in the independent SDK; the player gets only the read-only Doctor and bounded startup/export integration; the five game-loaded DLL invariant remains. |
| Batch 4 — optional QA host/staged extraction | in progress | Host activation, lifecycle, probes, settings/result ownership and package exclusion exist, but production still owns QA cases, scenario state and test-only mutation/evidence logic. |
| Batch 5 — recurring work/demand activation | blocked | The route explicitly starts this only after QA is physically moved. Current GameBridge measurements still include the retained QA case body. |

## Batch 2 Detailed Review

### Confirmed completion

- `tools/release/dtmapi-runtime-version.props` is the single Runtime authority: release `0.5.5`, binary/file version `0.5.5.0`, compatibility AssemblyVersion `0.5.3.0`. `Directory.Build.props` projects those values only to the five Runtime projects.
- The Catalog/release contract keeps current source version, retained published minimum and future `1.0.0`/`0.5.5` targets as separate axes instead of bulk rewriting historical artifacts.
- The retained Lamp family is restored as four public types and 92 signatures. All four types use `Obsolete(..., false)`; the implementation is owner-bound, deterministic `retired-disabled`, and adds no Lamp Hook or recurring gameplay owner.
- `FishingAutomationOptions.StopOnManualMove` is retained, and the exact old AutoFishing DLL loaded without recompilation under Unity Mono through Entry, the old setter, Configure and F6 activation. Movement cancellation and GC were correctly left outside the ABI claim.
- Runtime replacement is a staged directory/state transaction rather than five live DLL overwrites. Stale-version rejection, injected failures, rollback/recovery and successful `0.5.5` status passed under PowerShell 7 and Windows PowerShell 5.1.
- Exact eleven-public-product enabled/disabled and normal Steam/no-HookProbe player-input lanes passed before Batch 3/4. These are compatibility baselines, not publication or GC permission.

### Remaining release gates, not Batch 2 gaps

- The publicly obtainable external ecosystem scan remains bounded by what can actually be obtained; one real subscription sample is not the whole ecosystem.
- ISSUE-010/011 and the independent AutoFishing/ActionSpeed GC ladders remain open.
- No Runtime or Mod has been publicly released from this branch merely because Batch 2 passed.

## Batch 3 Detailed Review

### Confirmed completion

- `DTMAPI.AuthorSdk` is an independently versioned `net8.0` tool (`0.1.0`) containing scaffold, schema, validation, packaging, local deploy/withdraw/recovery and explicit author-session authority. The Unity Mono Runtime remains `netstandard2.0` and has no SDK project reference.
- The SDK uses fixed/hash-checked offline compiler/reference payloads and does not depend on a player's installed Runtime DLL. Ordinary players gain no author watcher, listener, timer or file poll.
- `DTMAPI.PlayerDoctor` is a self-contained player tool versioned from Runtime `0.5.5`. It is installed under `DTMAPI/tools/player-doctor`, not `BepInEx/plugins`, and the exact package is three files: executable plus the two .NET notice/license files.
- Player Doctor performs metadata-only/read-only classification. It diagnoses Runtime coherence, minimum versions, external BepInEx plugins and misplaced ordinary CodeMods without loading, moving, deleting or adopting them.
- Core invokes the installed Doctor once during startup with a bounded timeout and publishes one summary/export surface. It is not called from `Update`, Manager refresh, title refresh or a timer.
- Real package, PowerShell 5.1, transaction, installed-game, normal Steam/no-HookProbe and Manager export evidence closed the prior D1 gap. Catalog `PlayerDoctorAndPluginPlacementVerified` describes the tested product shape and sampled external artifact; it is not a universal third-party compatibility certification.

### P2 residual: repository-source status invocation

`tools/scripts/check-dtmapi-status.ps1` resolves the executable only as a sibling `player-doctor/dtmapi-player-doctor.exe`. That is correct inside the packaged/installed player tool tree and all product-path gates pass. Directly invoking this source copy from `tools/scripts` reports the helper missing even when the built or installed helper exists.

This remains P2 while repository-source invocation is not a documented player/CI contract. If documentation or automation promises that entry, either stage the sibling helper first or add an explicit developer-source resolution mode; do not weaken packaged-player sibling ownership.

## Batch 4 Findings

### Completed and retained work

- `QaHostActivationLoader` performs one startup activation-file check. When a receipt exists it validates schema, protocol, run ID, Runtime versions, fixed path containment, length and SHA-256, then loads the exact validated bytes and checks assembly/factory identity. Missing receipt returns immediately; there is no per-frame file poll.
- The optional QA project references production GameBridge, while the five production projects have no project reference to QA. GameBridge exposes an internal friend boundary to the QA assembly.
- The old `SmokeUpdate`, `LoadSmokeSettings`, production `SmokeSettingsPath` and recurring absent-file probe are gone.
- The four performance-probe files and their DTO/orchestration dependencies moved to the optional QA project.
- The player candidate package contains exactly the five production DLLs and no QA DLL/PDB/settings/activation root. The isolated Workshop install/check/collect/uninstall audit has zero blockers.
- QA lifecycle activation/attach/start/update/close, runner staging and exact recovery have substantial runtime coverage. Existing scenario results remain useful behavior/regression evidence.

### P1-1 — QA scenario bodies remain in the player GameBridge

The frozen dependency map counted the old `Smoke/**/*.cs` surface as 18 files / 13,981 physical lines and required scenario state, case orchestration, assertions, evidence writing and test-only process control to move into the optional QA assembly.

Current source, excluding `bin/obj`, contains:

| Surface | Files | Physical lines | Release DLL size |
| --- | ---: | ---: | ---: |
| `src/DTMAPI.GameBridge.DolocTown/QaHost/**/*.cs` | 25 | 12,733 | GameBridge: 1,474,048 bytes |
| `src/DTMAPI.GameBridge.DolocTown.QA/**/*.cs` | 15 | 3,227 | optional QA: 150,016 bytes |

The production QA-named body is 91.1% of the old embedded Smoke physical-line baseline. Line count is not itself a quality metric, but here the retained files contain the exact responsibilities the route assigned to QA:

- `DolocTownGameBridge.G4Fixtures.cs` owns UI scenario fields, native UI state entry/exit and screenshot capture.
- `DolocTownGameBridge.G5Fixtures.cs` switches case IDs and dispatches twenty mutation scenarios.
- `DolocTownGameBridge.G6Fixtures.cs` owns lifecycle case routing, long-title/load/return decisions and status expectations.
- `Fixtures/ActionSpeedFixtureCase.cs` owns full action scenario construction, mutation, assertion and status publication.
- `Fixtures/ContentFixture.cs` owns Oil/Mine/equipment/tech-tree state machines, temporary world mutation and screenshots.
- `Fixtures/DolocTownGameBridge.FixtureSupport.cs` owns save/load cycles, forced `GC.Collect`, screenshots, official UI continuation, application quit, transient world objects and Smoke status output.

These are not narrow native adapters. They are receipt-gated and therefore normally inactive, which prevents this from being a P0 behavior regression, but they still inflate the player assembly and keep QA policy/test authority in production. Renaming `Smoke` to `QaHost/Fixtures` did not perform the required physical move.

Acceptance requires the optional QA assembly to own case IDs, scenario state, expected outcomes, assertions, result/status composition, screenshot/file/process/GC control and sequencing. Production may retain only small internal operations that describe real native responsibilities or neutral primitive actions. A source/IL gate must inspect semantic residue, not only project references and package filenames.

### P1-2 — test-only destructive controls leaked onto a public Core type

`DtmApiRuntime` is public and currently exposes:

- `ConfigureSaveLoadObjectSnapshotModeForFixture(...)`;
- `ApplySmokeOwnerRootIsolationProfileForFixture(...)`.

The second method removes input buttons, event handlers and config pages for selected owners and publishes `Smoke.OwnerRootIsolation`. Repository consumers are only the production G6 fixture path. This is test authority on a public production type and violates the route stop condition against creating a public QA seam.

Move scenario choice to QA. Let QA call an internal GameBridge seam, and let GameBridge use its existing Core friend access for any genuine owner-lifecycle primitive. Before public `0.5.5`, remove the unpublished fixture methods from the public surface and rerun the retained `0.5.2` ABI comparison to prove no published member was deleted.

### P1-3 — final G7 has no post-deletion no-QA/no-receipt player run

G1 `004919` and several G4 runs prove no-receipt/no-QA behavior on earlier trees. G7 subsequently changed production Bootstrap/GameBridge lifecycle and deleted the embedded scheduler. The final G7 Published11, Disabled11 and player-input runs (`194541`, `194653`, `195027`) all use the staged optional participant; the runner also requires `-StageQaHost` for those assertions.

Therefore the final candidate lacks the dependency-map gate: ordinary Steam startup on final source with no QA receipt and no physical QA payload, showing no QA load attempt/root/listener/update and normal title/save/input/exit behavior. This is an evidence gap, not proof of a current player failure.

### P1-4 — completion metadata and automated Catalog assertions overclaim closure

Before this review, Update 0019, the July ledger and Catalog said Batch 4 was complete, all scheduling/evidence lived in QA, and only narrow native fixture seams remained. The Catalog checker asserted that string and inspected project references and package filenames, but did not inspect case bodies compiled into GameBridge. Those statements could incorrectly authorize Batch 5 and hide the remaining code-weight target.

This review reopens Update 0019 as `in-progress`, changes the Catalog state to `PartialOptionalHostWithPlayerFixtureDebt`, and records the exact debt. Successful prior tests and runtime runs remain historical evidence; only the completion inference is withdrawn.

### P2 — validation provenance and runner robustness

- The earlier final Release claim was based on a frozen pre-commit candidate tree. This review closed that provenance gap by running the complete tracked Release suite on clean committed HEAD `7e62d050` for 582.1 seconds.
- Multiple G5-G7 runs exposed runner classification, continuation and timeout/recovery defects before later passing reruns. No un-restored player state or residual process was found, but future acceptance should put run ID, actual participant set, phase/heartbeat, soft versus hard timeout, final exit code, artifact directory and residual-process/lock result in one terminal summary.
- Review `20260715-0009` described the Player Doctor as still missing. That was true at its snapshot and is now resolved by Update 0018; it receives a short resolution link rather than being rewritten.

## Validation Performed By This Review

- Clean committed HEAD `7e62d050`: `tools/scripts/test.ps1 -Configuration Release` passed in 582.1 seconds.
- All tracked builds reported zero warnings and zero errors.
- `DTMAPI.UnitTests`, `DTMAPI.QaUnitTests`, InstallDoctor tests, Author SDK tests, retained ABI checks, product Catalog `26/11/21/46`, Batch 2 release contract, evidence retention, document governance and dual PowerShell-host install/upgrade transactions passed.
- A fresh Runtime-only candidate package was built from the reviewed Release outputs in a temporary path containing spaces.
- The Workshop release-audit workflow copied it into a space/non-ASCII subscription-shaped path and passed Windows PowerShell 5.1 parsing for ten scripts plus missing/empty/valid/install/check/collect/uninstall/post-uninstall cases with `Blockers: 0`.
- The installed fake-game payload contained the exact five Runtime DLLs; GameBridge was 1,474,048 bytes; no QA artifact was present.
- No real game, shared Runtime, Workshop subscription, upload folder or player state was changed by this review. The final post-G7 no-QA game run remains pending.

## Corrective G8 Order And Acceptance

1. **G8A — freeze semantic residue:** add a machine-readable inventory of every production `QaHost/Fixture/Smoke` field, case dispatcher, status key and test-only mutation; classify real native primitive versus QA policy.
2. **G8B — move G4/G5 bodies:** move UI observation and world-mutation scenario state/expected results/sequencing into QA; keep only internal native UI/world primitive operations in GameBridge.
3. **G8C — move G6 bodies:** move fishing/owner/save-load/forced-GC/process-control scenario ownership into QA; remove public Core fixture methods and route neutral primitives through internal friend seams.
4. **G8D — enforce absence:** extend source/IL gates so a five-DLL package cannot pass merely because the QA DLL and project reference are absent. Reject production scenario case classes, test-only process/GC/file controls and public fixture APIs.
5. **G8E — replay closure:** run full Release, Workshop package audit, final no-QA/no-receipt Steam title + third-save + input + exit lane, then QA-enabled migrated scenario groups and exact eleven-product enabled/disabled lanes.

The independent AutoFishing/ActionSpeed GC ladders stay after architectural closure and before public `0.5.5`; they are not required to prove the G8 code move itself.

## Admission Decision

Do not enter Batch 5. Continue under Update 0019 with a corrective G8, preserving `68b941e` as the old G6 rollback boundary and `7e62d050` as the current behavior/evidence baseline. Batch 5 becomes admissible only when production contains narrow neutral seams, the public fixture authority is gone, the semantic absence gate passes, and the final no-QA player run is recorded.

## 2026-07-17 Resolution

Update `20260715-0019` completed corrective G8A-G8E and closed every P1 in this review:

- production `QaHost/**` is four neutral host/native-facade files totaling 608 physical lines; G4/G5/G6 routing, state, assertions, evidence/process/GC policy and fixture cases now compile only into the optional QA assembly;
- the two public Core fixture methods were removed, the frozen Abstractions DLL stayed byte-identical, and the retained old AutoFishing ABI gate reported zero removed public APIs;
- the tracked semantic source/IL gate now rejects production scenario residue and public fixture authority in addition to rejecting QA package artifacts/references;
- final-source Steam run `GAME-SMOKE/20260717-003431` passed title, third archive (`slot=2`), ordinary player hotbar input, eleven products, no HookProbe, no QA receipt/root/load and stable exit;
- staged-QA runs `004127`, `004235`, `004342`, and `004504` passed exact enabled/disabled eleven-product gates and the migrated G4/G5/G6 scenario groups with exact cleanup and restoration.

The full Release suite, zero-blocker Workshop candidate audit and Catalog closure all passed. This review remains the historical reason for G8; its rejection text is not rewritten. Batch 4 is now admissible as complete, Batch 5 was not started, and the independent AutoFishing/ActionSpeed GC ladders plus ISSUE-010/ISSUE-011 remain later release work.

## 2026-07-17 Post-Closure Reaudit Note

The Resolution above is retained as the historical G8 acceptance decision. A later source-level review found that the semantic allowlist had frozen additional production evidence policy and fixture mutation seams rather than proving their ownership.

[Review 20260717-0002](20260717-0002-batch4-post-closure-source-boundary-review.md) supersedes the admission sentence only: Batch 4 is reopened under [Update 20260717-0002](../../../updates/2026/20260717-0002-batch4-source-boundary-reopen.md), and Batch 5 remains inadmissible until G9 closes both P1s and replays the missing no-QA UI paths.

## 2026-07-17 G9 Resolution Link

The later ownership debt is resolved by [Update 20260717-0002](../../../updates/2026/20260717-0002-batch4-source-boundary-reopen.md). The G8 findings and evidence above remain historical; the G9 Update owns the corrected schema-5 source/IL boundary, no-QA/staged-QA UI replay and final Batch 4 admission result.
