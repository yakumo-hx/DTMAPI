# Update 20260717-0002: Batch 4 G9 Source Boundary Closure

- Lifecycle Status: `verified`
- Validation Level: `docs,source,unit,runtime,player`
- Runtime Validation: `passed`
- Related Issue State: `open`
- Date: 2026-07-17
- Source Request: complete G9 by moving DebugConsole, AnimalViewer, and EquipmentSlots evidence policy out of player assemblies; remove orphan fixture mutation; replace the semantic allowlist with symbol, consumer, forbidden-behavior, lifecycle, and negative-sample gates; replay no-QA/staged-QA UI, eleven-product, Workshop, cleanup, and full Release admission
- Related Review: [Batch 4 Post-Closure Source Boundary Review](../../reviews/code/2026/20260717-0002-batch4-post-closure-source-boundary-review.md)
- Historical Implementation: [Batch 4 Optional QA Host Extraction](20260715-0019-batch4-qa-host-extraction.md)

## Scope And Outcome

This Update originally reopened Batch 4 after G8 because the five-DLL package boundary was correct but three product paths still owned QA evidence policy, several production state machines retained QA-only mutation, and the semantic inventory merely froze those classifications. G9A-G9E now close that admission debt:

1. production DebugConsole, AnimalViewer, and EquipmentSlots retain only neutral product UI/state/lifecycle behavior;
2. screenshot timing, evidence paths, summaries, specimen selection, and `Smoke.*` scenario verdicts compile only into the optional QA assembly or runner;
3. orphan fixture mutation and synthetic-input controls were removed from Core/GameBridge, while remaining QA behavior calls neutral owner-bound operations;
4. optional QA update/observation delegates are mounted only for an activated participant rather than traversed by every no-QA frame;
5. schema-5 semantic enforcement rejects forbidden behavior, missing symbols/consumers/lifecycle cleanup, and representative old-defect samples instead of accepting whole-file hashes;
6. final current-tree staged-QA, ordinary no-QA, exact eleven-product enabled/disabled, cleanup/restoration, and receipt-bound replacement Workshop package gates passed, followed by the frozen exact-tree full Release suite.

G9 source ownership, Runtime/player behavior, the corrected Workshop package, and the frozen exact-tree Release suite are accepted. This Update is `verified`, and Batch 4 is complete through G9. Batch 5 was not started. ISSUE-010, ISSUE-011, and the independent AutoFishing/ActionSpeed GC ladders remain open later release work; none of the minute-scale G9 runs are GC evidence.

## Changed Files

### Player production boundary

- `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs` and `BootstrapPlugin.cs`: removed screenshot/evidence directory, summary, specimen, and `Smoke.*` policy from the ordinary Y-console path; retained neutral visibility and interaction state.
- `src/DTMAPI.GameBridge.DolocTown/Features/AnimalViewer/**` plus `Hooking/DolocTownHookCallbacks.cs`: removed `ANIMAL-001`, screenshot, summary, and evidence-only `AnimalPanel.RefreshViewer` behavior; added a native `AnimalPanelUiState.Unregister` close seam which clears only DTMAPI-owned cloned rows and derived data keys.
- `src/DTMAPI.GameBridge.DolocTown/Features/EquipmentSlots/**`: removed the production screenshot/evidence adapter and retained neutral render, interaction, and close observations.
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`, `Features/ActionSpeed/ActionSpeedService.cs`, `Features/MachineProduction/**`, and `Compatibility/FishingAutomation/LegacyFishingAutomationService.cs`: removed orphan synthetic input, suppress/force, and legacy fixture override state from player state machines.
- `src/DTMAPI.GameBridge.DolocTown/QaHost/**`, `DolocTownGameBridge.Update.cs`, and related bridge files: changed optional-QA update/observation dispatch to activation-bound delegates and kept only receipt/hash/version validation plus neutral native facades in production.

### Optional QA ownership and lifecycle

- `src/DTMAPI.GameBridge.DolocTown.QA/**`: owns DebugConsole/AnimalViewer/EquipmentSlots screenshots, evidence directories, summaries, scenario mutation, assertions, and `Smoke.*` verdicts; converts ActionSpeed/Machine/Fishing scenarios to neutral operations and real-frame pending transactions.
- `Scenarios/Fixtures/ContentFixture.cs`: keeps transient Mine ownership in a retryable QA transaction, waits for the normal Runtime entry and 0.5-second machine poll across real Unity frames before and after native time advance, verifies world-cell removal, and retries close cleanup instead of sleeping or forcing the production poll.
- `Scenarios/Fixtures/AutoFishingFixtureCase.cs`: owns synthetic input samples inside QA, clears terminal frames/state deterministically, and preserves the current AutoFishing movement-cancel -> `InstantSkip` phase sequence without restoring Core fixture controls.
- `Scenarios/DolocTownGameBridge.G6Fixtures.cs`: marks retained `LegacyFishingCompatibility` as SaveLoaded-dependent so the old Workshop DLL/ABI case cannot execute during initial title loading; QA Unit coverage locks this routing and the protocol-7 contract.
- `NativeLoadContinuationProbe`, `QaHostParticipant`, and G6 controller/cases retain the run-scoped Harmony owner, exact owner-only unpatch, synthetic-owner/Camera failure-close rollback, and idempotent cleanup from the earlier P1/P2 correction.
- Protocol version is `7`; participant close performs native-probe and scenario cleanup before committing the closed receipt.

### Enforced gates and records

- `tools/release/batch4-production-qa-semantic-inventory.json`: schema 5 behavior/symbol/consumer/lifecycle contracts and old-defect negative samples; no whole-file semantic hash allowlist.
- `tools/scripts/check-batch4-qa-semantic-boundary.ps1` and new `test-batch4-qa-semantic-inventory.ps1`: full player-source enforcement, four-artifact Runtime/optional-QA IL enforcement, twelve named semantic mutations, one Catalog-projection mutation, and six receipt-set validator cases; wired into `tools/scripts/test.ps1`.
- `tools/scripts/run-game-smoke.ps1`: strict staged/no-QA UI routes, exact five-DLL and no-QA pre/postflight, causal AnimalViewer receipts, native-close ordering, eleven-product selection/artifact gates, and exact save/profile/QA cleanup.
- `tools/release/dtmapi-product-catalog.json` and `tools/scripts/check-product-catalog.ps1`: completed G9 state with remaining GC/debug debt stated separately.
- `tools/scripts/install-to-game.ps1`, `check-dtmapi-status.ps1`, and `release-common.ps1`: initialize installer failure state before game-path resolution, convert missing/empty target checks to friendly diagnostics, and project the packaged release `BuildCommit` into both the installed manifest and install-state provenance.
- new `tools/scripts/test-installer-invalid-target-failure.ps1`, wired through `tools/scripts/test.ps1`: rejects raw unset-variable/`InvalidOperation` failures for two invalid installer targets, two conflicting option pairs, and two invalid status targets.
- `tools/scripts/test-runtime-upgrade-transaction.ps1`: expands the upgrade/recovery transaction matrix to 15 cases per host, including packaged commit provenance, explicit synthetic-payload compatibility, exact assembly receipts, and missing/damaged/mismatched packaged-manifest failure before mutation.
- `docs/debug/evidence-retention-allowlist.json`: regenerated from tracked and unignored Markdown references; the delta contains only twelve G9 `GAME-SMOKE` run identities and the three new G9 Hook-map/Review/Update source files, with no new artifact, Runtime-evidence, dump, absolute, TEMP, private-reverse, or worktree-external path.
- this Update, the July ledger, focused Hook maps, historical cross-references, and `docs/debug/regressions/smoke-matrix.md`: durable ownership and runtime evidence.
- ordinary and QA Unit suites cover the removed surfaces, neutral operations, native close/data-key release, fail-closed cleanup, and semantic gate behavior.

## Implementation Details

### G9A/G9B — evidence policy moved

- Opening Y no longer arms a screenshot, creates `DEBUG-CONSOLE-UI`, chooses a QA specimen, writes a summary, or publishes `Smoke.*`. The staged DebugConsole fixture owns those actions after an explicit QA activation.
- AnimalViewer production publishes neutral `Animals.ViewerProgressUi`, `Animals.ViewerRenderingLifecycle`, and `Animals.ViewerNativeLifecycle` receipts only. A new data-object identity/sequence receipt coalesces duplicate `Show` callbacks for the same native data object, while a real animal switch produces a new causal receipt.
- Native `AnimalPanelUiState.Unregister` is the close owner. It publishes native close first, releases cloned objects and derived `AnimalFullInfoData` keys, publishes `session-cleared` without the former throttle gap, and finally publishes the neutral closed state.
- EquipmentSlots production publishes neutral bind/render/interaction/close state. QA owns screenshot timing, output path, summary, and pass/fail composition.

### G9C — scenario mutation removed

- `SuppressActionSpeedAutoFillForFixture`, `ForceMachineProductionDueForFixture`, legacy fishing suppress/force/pool fields, Core synthetic frame/tap injection, and the title fixture click delegate were deleted from production.
- QA scenarios use normal UI/input or narrow neutral owner-bound operations. Real-frame pending transactions replace sleeps in Machine and ActionSpeed cases.
- Evidence-root/config mutation and the creative mutation probe moved to QA. G6 failure-close still independently releases both synthetic owners and both Camera leases.

### G9D — absence is enforced

The schema-5 gate scans all player C# and relevant runner PowerShell rather than hashing selected lines. Its canonical selector is the Catalog's exact `PublishedProduct` + `PublicWorkshop` eleven-product set under policy `published-product-player-evidence-policy`; the production roots are the five Runtime roots plus those eleven product roots, and the consumer roots are `src`, `tests`, plus the same eleven product roots. All symbol contracts scan public product roots unconditionally, public roots cannot appear in consumer `allowedRoots`, and neutral-input references are checked through direct, whitespace/comment-separated, method-group, and local-alias forms. Its final result is:

- production source files: `164`;
- neutral QA-host files/physical lines: `4 / 597`;
- contract coverage: `25/25` (`14` forbidden-behavior, `5` symbol, `6` consumer);
- forbidden-pattern coverage: `149/149`;
- lifecycle contracts: `20`;
- negative samples: `49`;
- built IL artifacts: `4` (only the Runtime/optional-QA boundary artifacts, not a claim that all eleven product DLLs receive IL scanning);
- actual-execution receipts: `12/12` named semantic cases, `1/1` Catalog-projection case, and `6/6` receipt-set validator rejection cases.

The gate explicitly covers DebugConsole/AnimalViewer/EquipmentSlots evidence behavior, ActionSpeed/Machine/Fishing/Core orphan mutation, optional participant activation, native continuation cleanup, G6 failure-close cleanup, AnimalViewer native close/order/data release, runner postflight, and exact no-QA/staged ownership.

## Validation

### Source and unit

- Schema-5 semantic boundary, Catalog projection, and meta-negative tests passed under PowerShell 7 and Windows PowerShell 5.1 with the counts above. PS7 timings were semantic `23.7s`, Catalog `1.3s`, meta `91.3s`; PS5.1 timings were `27.1s`, `1.5s`, and `86.8s`.
- Targeted Release build of Unit dependencies and `DTMAPI.UnitTests` passed with zero warnings and zero errors; temporary managed-test artifacts were subsequently absent.
- Targeted Release `DTMAPI.QaUnitTests` passed after the ContentFixture real-frame/cleanup, AutoFishing QA-input/state-reset, and Legacy G6 SaveLoaded-routing corrections.
- AnimalViewer global readiness now requires constructor Postfix, `Show` Prefix, `Show` Postfix, and `Unregister` Postfix together. Summary totals are fishing `34` and no-fishing `25`; Unit coverage proves the old constructor + Show-Postfix pair cannot report ready and the complete set contributes exactly four ready targets.
- The no-QA runner's exact `PSBoundParameters` whitelist rejects QA, AutoSave, smoke-root isolation, extra products, and other automation; its legacy-evidence receipt compares recursive directory sets as well as file path/length/SHA-256, so a new empty directory also fails. The valid and negative source routes passed `8/8` under both PowerShell hosts.
- `test-installer-invalid-target-failure.ps1` passed under `pwsh` and Windows PowerShell 5.1 with `installer-target=2`, `installer-option-conflict=2`, and `status-target=2`. `test-runtime-upgrade-transaction.ps1` passed its full `15`-case child matrix under each host, including non-empty/equal package provenance, exact receipt validation, same-FileVersion/different-content rejection, and fail-before-mutation package-manifest cases.
- The first final-Release attempt stopped at the stale generated evidence-retention allowlist. Regeneration added only twelve G9 run identities and three G9 Markdown source files; artifacts remained `225`, Runtime identities `62`, process dumps `6`, and an explicit path audit found no absolute, TEMP, private-reverse, or worktree-external entry. The regenerated `398`-source / `758`-run allowlist then passed its deterministic `-Check` gate.
- The frozen source tree's `tools/scripts/test.ps1 -Configuration Release` run passed on 2026-07-18 with exit code `0` in `886.4s` (`14m46.4s`). Every reported managed build completed with zero warnings and zero errors; Unit, QA Unit, Install Doctor, Player Doctor, Author SDK, dual-host installer/upgrade transactions, Catalog, Batch 2 release contract, G9 semantic/meta-negative, and synthetic ABI gates passed. The tracked synthetic ABI fixture remained the default Release gate. The optional private retained-binary consistency layer was skipped because its explicit artifact environment variables were not provided, and is not implied by the synthetic result.

### Staged-QA positive UI lanes

- `GAME-SMOKE/20260717-235930`: current-tree staged DebugConsole screenshot/evidence route passed with two-phase input/close, exactly one participant lifecycle, and exact QA/profile/process cleanup; missing-frame/stall `1/1` recovered.
- `GAME-SMOKE/20260718-000107`: current-tree staged EquipmentSlots route passed real render, screenshot/summary, move-only hover, Escape close, one participant lifecycle, and exact cleanup; missing-frame/stall `1/1` recovered.
- `GAME-SMOKE/20260718-000232`: current-tree staged AnimalViewer route passed the four-Hook readiness boundary, valid screenshot, neutral render-before-evidence, native close before `session-cleared`, one participant lifecycle, and exact cleanup; missing-frame/stall `1/1` recovered.

### Ordinary no-QA negative UI lane

`GAME-SMOKE/20260718-005546` used normal Steam, third save, Published11, no HookProbe, no QA activation, and exact five Runtime DLLs. It passed:

- repeated short/held Y-console input, Escape recovery, and no new screenshot/directory/summary/`Smoke.*` product evidence; the three historical legacy evidence trees remained recursively identical;
- EquipmentSlots open, move-only hover, Escape close, and no QA evidence;
- exactly two causal `Animals.ViewerProgressUi=visible` receipts from normal player open/switch input;
- runner-owned Escape followed by native `AnimalPanelUiState.Unregister`, then `session-cleared`, in strict order;
- no QA DLL, activation receipt, QA root, HookProbe, or published QA artifact before or after the run;
- exact five-DLL postflight, exact eleven published owners with Entry/Begin/Commit `1/1/1`, legacy-evidence recursive tree unchanged, stable exit, byte-identical `mod_infos.json`, and byte-identical three-file third-save restoration;
- QA marker counts `0`, absent activation/root/QA DLL before and after, one recovered missing-frame warning, one recovered frame-driver stall, no Fatal GC/fresh crash, and no residual process.

The earlier current-tree attempt `20260718-003040` loaded the save only after the runner's first timeout and is rejected evidence; its failure path restored the official profile and exited cleanly. Formal no-QA acceptance is only `005546`.

### Exact eleven-product matrix

- `GAME-SMOKE/20260718-000350`: current-tree Published11/Enabled11 passed all `11/11` product-owner gates with Entry/Begin/Commit `1/1/1`, zero owner failures, exact product artifacts, lifecycle/participant close, and exact save/profile/QA-stage/process cleanup. Missing-frame/stall `1/2` recovered; no Fatal GC or fresh crash was attributed to the run.
- `GAME-SMOKE/20260718-000521`: current-tree CoreOnly/Disabled11 passed all `11/11` disabled gates with `0/0/0`, the exact empty public-owner boundary, lifecycle/participant close, and exact save/profile/QA-stage/process cleanup. Missing-frame/stall `1/1` recovered; no Fatal GC or fresh crash was attributed to the run.

Both runs restored the three third-save files byte-for-byte, removed the exact staged QA tree after validation, restored `mod_infos.json`, and left no residual process. Earlier `224520`/`224634` and `213631`/`213808` remain historical passes but are superseded as the final eleven-product matrix by these current-tree replays.

### Migrated G5/G6 continuation matrix

- `GAME-SMOKE/20260717-221222`: final G5 ActionSpeed plus Mine passed. Mine official JSON, tech-tree UI and production were observed after real-frame Runtime discovery and native time advance; the pending transient equipment was removed, G5 config/external state and the third-save files were restored, QA closed cleanly, and missing-frame/stall `1/1` recovered.
- `GAME-SMOKE/20260717-222930`: current AutoFishing `InstantSkip` passed the QA-owned input tap, movement cancellation, native-visible skip, phase/lifecycle, input-log and report-export sequence. QA/profile/process cleanup passed; `PlayerSaveRestored=Skipped`; missing-frame/stall `1/1` recovered.
- `GAME-SMOKE/20260717-224027`: Workshop item `3743799721` supplied the retained 0.5.1-era AutoFishing DLL instead of the disabled official-local copy. The DLL completed Entry, loaded the third archive (`slot=2`), and passed the legacy configure/enable/disable compatibility facade with zero transient owner resources after cleanup. QA/profile/process cleanup passed; `PlayerSaveRestored=Skipped`; missing-frame/stall `1/1` recovered.
- `GAME-SMOKE/20260717-224136`: CoreOnly completed one third-save load/title/load cycle (`cycles=1`, runner save slot `3`) after the G6 initial-load correction. QA/profile/process cleanup passed; `PlayerSaveRestored=Skipped`; missing-frame/stall `1/1` recovered.

These four short functional/lifecycle runs are G9 continuation evidence only. They are not the independent AutoFishing or ActionSpeed GC ladders.

### Workshop player package — receipt-bound replacement audit passed

- The `2140` candidate is rejected because invalid targets exposed raw initialization failures. The later `2252` candidate is also superseded: its `IncludedAssemblies` held five strings rather than the required receipt objects and it predated the final GameBridge/readiness tree, so it cannot prove payload identity.
- The final pre-commit candidate is `tmp/Batch4 G9 Workshop Candidate Final3 中文 20260717-235728`. It contains twelve packages: the Runtime plus the exact eleven Catalog-selected public products. The Runtime manifest records `PackageKind=workshop-runtime`, hex `BuildCommit=157d7642fb51`, and exactly five `{FileName,Length,Sha256,FileVersion}` receipts; no QA/Smoke/Test assembly or QA settings artifact is present.
- Receipt hashes are: Bootstrap `ea7f566e...dddc8`, Abstractions `1773527a...7d44`, Core `00ec4bf7...5872`, GameBridge `af4a3137...3f3d`, and ModConfigMenu `6ead244c...a84`. The package installer validates schema/version/kind/commit and the exact unique receipt set before transaction/recovery/mutation; a same-FileVersion but different DLL fails before mutation.
- The subscription-style audit at `tmp/Batch4 G9 Workshop Audit Final3 Evidence 中文 20260717-235728/DTMAPI Workshop Audit 20260717-235803/Results/stress-summary.md` passed Windows PowerShell 5.1 parsing for all ten packaged scripts and the eight-case missing/empty/valid install, check, collect, uninstall, and post-uninstall matrix with `Blockers: 0` on paths containing spaces and Chinese text.
- A real local package install under the shared runtime lock projected `BuildCommit=157d7642fb51` through package, installed release manifest, and install-state `SourceRepoCommit`; its exact five live DLL receipts matched the candidate. This is frozen pre-commit payload evidence. A post-commit `-SkipBuild` package must preserve all five binary receipts while changing provenance to the final commit before publication use.
- The 2026-07-18 independent admission re-audit completed that post-commit check without touching the shared game Runtime. Runtime-only candidate `tmp/Batch4 G9 独立复核 RuntimeOnly d01c2ca7ee47 20260718-022734987 中文 With Spaces/DTMAPI` records final G9 commit `d01c2ca7ee47`, preserves the same five receipt hashes, and contains no QA/Smoke/Test/HookProbe assembly or QA settings/activation artifact. Its Windows PowerShell 5.1 subscription audit at `tmp/Batch4 G9 独立复核 Workshop Audit 20260718-022801450 中文 With Spaces/DTMAPI Workshop Audit 20260718-022801/Results/stress-summary.md` passed all ten parser checks and the eight-case install/check/collect/uninstall matrix with `Blockers: 0`. This closes the G9 provenance caveat for admission evidence; it is still a temporary audit candidate, not authority to publish after later Batch 5 changes.

### Recovery and rejected evidence

- Diagnostic no-QA attempts `203839`, `204607`, and `210845` exposed input/close/receipt weaknesses and are not acceptance evidence. The `210845` interrupted route was recovered from exact save/profile receipts before the next run.
- `212214` executed the final staged Animal behavior but its outer collector was interrupted by an incorrectly short tool timeout. After the game exited, all three save hashes were unchanged; the exact stage artifacts/tree and profile backup were verified before cleanup. Formal acceptance is the later fully collected `212720` run.
- `220629` is a failed Mine diagnostic superseded by `221222`; ActionSpeed had passed, but Mine exposed the removed forced-poll/real-frame discovery gap.
- `221940` is a failed current-AutoFishing movement/phase diagnostic. `222402` then reached passing InstantSkip movement/phase/lifecycle receipts but retained an overall failed result on the older animation-speed gate. Neither is acceptance; `222930` supersedes both.
- `223136` and `223308` are failed retained-DLL diagnostics which ran the Legacy G6 case before a valid SaveLoaded/initial-load boundary; `223308` also lacked a normal participant-close receipt. They are superseded by `224027` and must not be described as passed runs.
- `20260718-000640` was physically interrupted before ordinary-player input and `003040` loaded the save after its first timeout; both restored profile/save/process state and are rejected rather than acceptance evidence. `005546` is the only final current-tree no-QA acceptance.
- No accepted G9 run produced a gameplay failure, Fatal GC, fresh crash dump, or residual `DolocTown.exe`. Existing missing-frame/frame-driver warnings recovered and do not change ISSUE-010/ISSUE-011 state.

## Rollback

Revert the G9 source, optional-QA cases, schema-5 inventory/checker/meta-tests, runner gates, Catalog/checker, installer/check/provenance correction and its regression test, Hook maps, smoke ledger, historical resolution links, and this Update as one reviewable unit. A partial rollback which restores production evidence writers or fixture mutation while retaining the completed Catalog state is invalid.

Runtime rollback must occur only under the shared runtime lock after confirming `DolocTown.exe` is absent. Rebuild and install the chosen prior commit, then rerun exact profile/save/QA cleanup checks; never copy QA DLLs into the five-DLL player Runtime or restore evidence-only `AnimalPanel.RefreshViewer` behavior.

## Follow-Up

The frozen exact-tree Release suite passed, so this Update is `verified` and Batch 4 is complete through G9. Batch 5 remains unstarted by this Update. A later Update may enter it only as a separate lifecycle record; it must not reinterpret this architectural/UI evidence as either deferred GC ladder. ISSUE-010 and ISSUE-011 stay open, and AutoFishing plus ActionSpeed still require independent `1x -> enabled/no acceleration -> common multiplier -> high multiplier -> disabled recovery -> title cycle` GC ladders before the relevant public release decision.
