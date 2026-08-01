# 20260715-0009 Batch 2/3 Route Compliance And Batch 4 Entry Review

Status: recorded
Date: 2026-07-15
Scope: audit the claimed completion of Batch 2 and Batch 3 against the frozen major-update route, identify regression and release carry-forward gates, and decide whether physical Batch 4 QA extraction may begin
Related Update: `docs/updates/2026/20260715-0017-batch2-batch3-route-and-batch4-entry-review.md`
Primary route: `docs/reviews/code/2026/20260712-0003-dtmapi-full-boundary-audit.md`
Decision policy: `docs/reviews/code/2026/20260715-0007-decision-escalation-policy-and-product-nodes.md`
Batch 3 implementation review: `docs/reviews/code/2026/20260715-0008-batch3-author-sdk-source-reload-boundary-review.md`
Resolution: the Player Doctor gap identified here was closed by `docs/updates/2026/20260715-0018-batch3-player-doctor-closure.md`; the later Batch 4 completion claim was independently reopened by `docs/reviews/code/2026/20260716-0001-batch2-batch3-batch4-detailed-acceptance-review.md`.

## Source Request

The user stated that Batch 2 and Batch 3 had completed and requested a route-compliance and Batch 4 entry audit.

This review distinguishes an implemented slice from a closed route milestone. It is read-only with respect to Runtime, game, Workshop, subscriptions, player state and packages. It does not implement the newly found Batch 3 closure item or start physical Batch 4 extraction.

## Snapshot

- Branch: `codex/major-update-batch0-20260713`
- HEAD: `780d343c89f3d826d4449dfa86339013dd345a0e` (`docs: record flourishing flora teapot audit`)
- Worktree: clean before and after the automatic review gates
- Commit order: `7ee65e7c` Batch 2 -> `dd7492b3` Batch 3 -> `780d343c` unrelated documentation only
- Runtime lock: free; no `DolocTown.exe` process was observed

## Executive Verdict

| Milestone | Verdict | Entry consequence |
| --- | --- | --- |
| Batch 2 version/release authority | **Complete and route-compliant.** | No remaining Batch 2 P0/P1 blocks Batch 4. |
| Batch 3 Author SDK A1-I1 preview | **Complete and verified.** | The independent SDK/tooling/source/reload slice is not the problem. |
| Whole Batch 3 route milestone | **Not strictly closed: one P1 player-Doctor boundary is missing.** | Do not start physical QA extraction yet. |
| Batch 4 Checkpoint B read-only dependency map | **May start now.** | It creates no runtime dependency and can prepare the extraction graph. |
| Batch 4 code movement / QA host implementation | **Hold until the narrow Batch 3 Doctor closure passes or the user explicitly changes the frozen D1 promise.** | This preserves the agreed milestone order. |

The accurate current statement is:

> Batch 2 is complete. Batch 3's Author SDK implementation is complete, but Batch 3 as a product milestone still lacks the frozen player-facing read-only Doctor/misinstallation path. Batch 4 analysis may begin; physical extraction may not yet be called started.

## Batch 2 Closure Audit

Batch 2 closes at commit `7ee65e7ce72b8b58f815f700fb18321d977d0a3a`. Its exact code-bearing validation tree and final commit differ only by record/index files, not source behavior.

The required route gates are present:

- one Runtime `0.5.5` version authority projects five Runtime assemblies while preserving AssemblyVersion `0.5.3.0`;
- all eleven public products and Oil have current/retained/future version/minimum axes and an actual-artifact release contract;
- Oil remains current `0.3.1-dtmapi`, future `1.0.0`, official-JSON-led, DLL-free and without a DTMAPI minimum;
- `StopOnManualMove` and the exact four-type/92-signature Lamp family are restored; the strict retained ABI report has zero public deletions;
- Lamp is an obsolete Disabled owner-bound `retired-disabled` facade with no Lamp Hook, lifecycle root, gameplay state or recurring work;
- the exact retained AutoFishing DLL loaded without recompilation under Unity Mono and reached Entry, old setter, Configure and F6 enablement;
- stale Runtime rejection, injected transactional rollback, successful `0.5.5` update/status and both PowerShell host matrices passed;
- exact eleven-product enabled and disabled Steam lanes, final no-HookProbe foreground input/provenance, save/enablement restoration and clean process exit passed;
- all three prior Smoke P1s were corrected: continuous observed-state timing, exit-before-player-file restoration with fail-closed recovery, and complete Update 0013 change/rollback ownership.

The canonical Batch 2 Updates `20260714-0004` and `20260715-0010` through `0013` are now `verified`. ISSUE-010/011 and the independent AutoFishing/ActionSpeed GC ladders remain later release gates, not missing Batch 2 implementation.

## Batch 3 Author SDK Audit

Commit `dd7492b31b625f912fd17f4a9bb35ac10aac04ef` implements the selected SDK A1-I1 defaults without prematurely entering Batch 4/5:

- self-contained Windows x64 .NET 8 `dtmapi-author` `0.1.0`; generated CodeMods remain `netstandard2.0`;
- Runtime target `0.5.5`, fixed/hash-checked offline Abstractions/compiler/reference payload and no dependency on the player's installed Runtime DLL;
- minimal CodeMod and ContentPack scaffolds, manifest authority, validation, offline CodeMod build and byte-deterministic packaging;
- metadata-only read-only Author Doctor, with no `Assembly.Load` and no tree mutation;
- dual package receipt plus external journal deployment/update/withdraw/recovery, with no force/adopt;
- Player/Workshop, Local Development, Workshop Validation and Player Reproduction source modes with explicit selected/shadowed state;
- ordinary player Runtime creates no author listener, watcher, timer or file poll; an explicitly prepared bounded author session owns the transport;
- only Audio supports transactional valid-invalid-valid reload with last-good retention; DLLs, Custom Animals, native official JSON and unknown formats remain restart-required;
- no Workshop upload, Steam credentials, public API addition, QA extraction or general Batch 5 demand/hot-path work.

The recorded SDK ZIP SHA-256 still matches the current artifact: `12C246EFF17557A1536357677B50B87AA83455AA47712F1930F7D72749A8929C`. Existing runtime evidence `185255`, `190725` and `191256` covers native Workshop source authority, scoped author-session Audio rollback/commit and a final ordinary-player no-author-poll run.

## P1 Batch 3 Closure Gap: Player Doctor

This is a route mismatch, not a preference question.

The frozen second-round D1 decision says:

- Author SDK owns templates, schemas, validation, packaging, local deployment, receipts, reload and author documentation;
- the player package retains read-only Doctor/error/minimum-version/misinstallation diagnostics;
- direct BepInEx plugins remain external and are never moved, deleted or adopted.

The third-round closed inputs make the acceptance boundary explicit:

- when DTMAPI loads, the player has an in-game Doctor summary/export;
- when DTMAPI does not load, the player still has an offline read-only check/log path;
- full validation, packaging and repair remain Author SDK work.

Current implementation does not close that player boundary:

- `DTMAPI.InstallDoctor` and `DTMAPI.Tooling.Metadata` target .NET 8 and are referenced by `DTMAPI.AuthorSdk`, not by any of the five game-loaded player assemblies;
- the Author Doctor correctly identifies external BepInEx plugins and misplaced DTMAPI CodeMods, but it is distributed through the Author SDK;
- player `3_check` and `4_collect` provide general install/version/status/log diagnostics but do not perform the Doctor PE classification or identify an ordinary DTMAPI CodeMod placed directly under BepInEx;
- Runtime/Manager has no corresponding metadata-only player Doctor summary/export path;
- Product Catalog external item `3759797170` still has `releaseGate: DoctorAndPluginPlacementPending`.

Update `20260715-0016` remains truthful and verified for the **Author SDK preview** it owns. It cannot by itself close the broader Batch 3 route milestone.

### Required narrow closure

Before physical Batch 4 extraction, a focused Batch 3 closure must prove both player paths without widening authority:

1. **Offline:** a player-packaged, read-only check/report still works when the Runtime cannot load and diagnoses misplaced DTMAPI CodeMods/external BepInEx plugins without executing or changing them.
2. **In game:** a bounded lifecycle-triggered metadata-only summary/export is available when DTMAPI loads; it performs no ordinary every-frame scan or file polling.
3. **Safety:** neither path moves, deletes, adopts, enables, disables, patches or loads an unknown DLL; it exposes actionable errors/minimum-version/misinstallation guidance only.
4. **Packaging:** the five game-loaded production-DLL invariant remains intact and the player package does not acquire Author SDK build/deploy/repair authority.
5. **Contract:** the Catalog `DoctorAndPluginPlacementPending` gate is resolved from actual evidence rather than renamed away.

If the product no longer wants a player Doctor, that is a change to frozen D1/third-round behavior and must be explicitly decided; Update 0016 cannot silently redefine it as Author-SDK-only.

## Batch 4 Baseline And Allowed Preparation

The current tree is at the expected pre-Batch-4 debt boundary:

- no separate `DTMAPI.GameBridge.DolocTown.QA` project exists;
- `src/DTMAPI.GameBridge.DolocTown/Smoke` contains 18 files and 13,981 source lines;
- `ForSmoke` occurs 697 times across 29 production source files;
- `DolocTownGameBridge.Update` still calls `SmokeUpdate()` every frame and the constructor still calls `LoadSmokeSettings()`;
- the Catalog correctly says `separateQaAssemblies=[]`, `qaExtractionState=NotYetImplemented`, and player QA debt is still embedded.

The allowed preparatory work is a focused Checkpoint B Review and read-only dependency map covering partial/private/static seams, Hooks, Core, Bootstrap, runner protocols, production repair versus instrumentation and exact rollback groups. Do not create the Batch 4 implementation Update or move code until the Doctor closure is complete.

After that closure, the selected Batch 4 order remains:

```text
optional netstandard2.0 QA host skeleton and explicit runner staging
-> DTO/result/report and four probe groups
-> low-coupling diagnostic/content/save groups
-> Bootstrap/UI/content/audio/camera, separating real UI repair first
-> world-changing ActionSpeed/ActionCompletion/Crop/Equipment
-> Fishing/owner-lifetime/save-load/long-trend last
-> remove SmokeUpdate/polling/Hook markers/root isolation/ForSmoke seams only after each replacement passes
```

Production assemblies must never reference QA; QA may depend inward through narrow internal/friend seams. The player package remains free of QA DLLs/settings/types/roots/listeners/polls. Batch 4 must not add public Abstractions APIs, expose Unity/Harmony/Doloc types, perform Batch 5 demand activation, remove Equipment orphan recovery, remove genuine UI repair, migrate product identities or call package-size reduction a GC fix.

## Regression And Release Carry-Forward

These do not block the narrow Doctor closure or Checkpoint B mapping, but must remain visible:

1. Batch 3 changed `ManifestReader`, `DtmApiRuntime`, native Workshop source snapshots and `ReloadMods`. Its final normal-player smoke passed, but the exact eleven-product enabled/disabled matrices were not replayed on post-Batch-3 HEAD. Run them as a pre-extraction baseline or the first Batch 4 regression group.
2. The default Release entry builds the ABI harness but does not consume the private retained DLLs. The explicit physical old-DLL wrapper remains a release-candidate gate.
3. The zero-blocker Workshop subscription audit is for the final Batch 2 package and predates Batch 3 Runtime changes. It validates Batch 2, not a current-HEAD `0.5.5` release candidate; rebuild and re-audit the final RC later.
4. Existing one-frame fallback/recovered-stall/resubscription warnings in the final Batch 3 normal-player run are the migration baseline, not evidence caused by Batch 4.
5. ISSUE-010/011, the complete publicly obtainable external ecosystem scan and independent AutoFishing/ActionSpeed GC ladders remain 0.5.5 publication gates.
6. Update `20260715-0003-official-tutorial-real-environment-audit` remains in progress and names `run-game-smoke.ps1`, UnitTests and the active Smoke matrix in its change surface. Current Git is clean, but file ownership must be handed off before Batch 4 edits those shared paths.

## Validation Boundary

- Current branch, HEAD, commit ancestry, clean worktree, Update metadata, Catalog, source/project references and current package artifacts were inspected.
- `tools/scripts/test.ps1 -Configuration Release` passed on current HEAD in 732.3 seconds. It completed tracked Release builds with zero reported build warnings/errors, Unit/Author/Doctor gates, dual-host Runtime and developer-install transaction matrices, Catalog `26/11/21/46`, Batch 2 release contract, Author SDK deterministic/portable checks, evidence retention and document governance.
- Focused Doctor and Author SDK Release tests also passed with zero warnings/errors.
- The existing final Batch 2 Workshop package audit was reviewed through the Workshop release-audit workflow: PowerShell 5.1 parsed all ten packaged scripts and the missing/empty/valid/check/log/uninstall matrix had zero blockers. It was not rerun because the package is a Batch 2 artifact, not the current-HEAD RC.
- No game was launched and no new runtime evidence was produced. Existing runtime records were cross-checked only.
