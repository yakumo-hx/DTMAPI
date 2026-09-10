# 20260714-0001 Major Update Progress, Route, And Regression Review

Status: recorded
Date: 2026-07-14
Scope: review the current major-update worktree against the closed Batch 0 through Batch 8 route, identify the actual completed layer, rank regression work, and separate new findings from known deferred debt
Related Update: `docs/updates/2026/20260714-0002-major-update-progress-route-regression-review.md`
Primary route: `docs/reviews/code/2026/20260712-0003-dtmapi-full-boundary-audit.md`
Decision closure: `docs/reviews/code/2026/20260713-0012-major-update-sixth-decision-docket.md`
GC gate: `docs/reviews/code/2026/20260713-0013-autofishing-actionspeed-active-gc-release-gate.md`

## Source Request And Exclusion

The user requested a current-progress review answering, in order:

1. which implementation layer the project has reached;
2. whether work followed the agreed route;
3. which behavior deserves regression testing;
4. whether new problems appeared.

The user also said that a separate local analysis of other issues may be ignored. This review therefore excludes the 2026-07-14 JSON-derived-value/manual-QA analysis from its technical conclusions. Those files remain independent dirty-worktree material and must not be silently folded into a major-update checkpoint.

This is a review and handoff record. It does not change runtime, manifests, packages, public APIs, versions, game files, Workshop state, or debug issue classifications.

## Snapshot And Executive Verdict

Snapshot branch: `codex/major-update-batch0-20260713`

Snapshot HEAD: `c826d2617218b362594f589f6d707e9180058094` (`docs: record major update boundaries and decisions`)

There are no commits after that baseline. The reviewed worktree contains 63 status entries: 50 tracked changes/deletions and 13 untracked paths. The three main implementation records `20260713-0010`, `20260713-0011`, and `20260713-0012` are themselves untracked at the snapshot.

The accurate two-axis verdict is:

```text
Implementation and evidence:
  Batch 0 verified
  -> player uninstaller ownership P0 verified
  -> Oil/OneAction ownership P0 verified
  -> standing at the Batch 2 entrance

Git durability:
  still at the pre-implementation baseline commit
```

The route is being followed. The current work is not a 0.5.5 release candidate, and neither lightweight-runtime work nor the active-gameplay GC gate has started. The largest immediate project risk is that three independently described rollback boundaries plus unrelated local analysis are stacked in one uncommitted worktree.

## 1. Current Progress Layer

| Route layer | Current state | Evidence and boundary |
| --- | --- | --- |
| Decision and identity direction | committed | The full boundary audit and six decision rounds are in `c826d261`. |
| Batch 0: Catalog, identities, API/behavior baselines | verified in worktree, uncommitted | `docs/updates/2026/20260713-0010-batch0-boundary-catalog-baseline.md`; current checker reports 26 products, 11 public products, 21 retained Workshop items, and 45 API rows. Public upload remains stopped and 0.5.5 remains `NoArtifact`. |
| P0-A: player uninstall ownership | verified in worktree, uncommitted | `docs/updates/2026/20260713-0011-player-runtime-only-uninstall-ownership-p0.md`; the player uninstaller is Runtime-only and `dtmapi-package.json` has no delete/overwrite authority. A2 Author-SDK receipts remain deferred. |
| P0-B: Oil and OneAction ownership | verified in worktree, uncommitted | `docs/updates/2026/20260713-0012-oil-official-json-oneaction-decoupling-p0.md`; Oil is a DLL-free official-JSON ContentPack, the GameBridge Oil service/Prefix/direct grant and OneAction callback are removed, and two focused third-save runs passed. |
| Batch 2: version and release authority | not started | Runtime is still hand-authored as `0.5.3-alpha` / `0.5.3.0` in `Directory.Build.props`, `DtmApiRuntime`, and `release-common.ps1`. The Catalog records future 0.5.5 but does not yet drive all release/build/install projections. |
| Batch 3: Author SDK and Doctor | not started | No independent SDK receipt lane, transactional local deployment, real CodeMod scaffold, or BepInEx-misplacement Doctor exists. The new fail-closed installer behavior makes this an operational prerequisite before sustained product iteration. |
| Batch 4: QA extraction | not started | Smoke remains compiled into the player GameBridge, the constructor still calls `LoadSmokeSettings()`, and every frame still calls `SmokeUpdate()`. The Oil P0 added roughly 242 net Smoke lines for evidence, so this batch has not reduced player QA weight. |
| Batch 5: hot paths and demand activation | not started | `LoadedMods` still allocates with `ToArray()`, AudioReplacement and CustomAnimals still build content signatures in recurring paths, and no-consumer feature hosts still receive EveryFrame dispatch. |
| Batch 6: functional Mod splits | only an enabling Oil boundary | Oil ownership is corrected, but this is not completion of the later OneAction/product structural split. No other product migration should be counted as done. |
| Batch 7: compatibility/API governance | not started | The known `FishingAutomationOptions.StopOnManualMove` ABI deletion remains an open 0.5.5 blocker. |
| Batch 8: UI and future features | not started | Manager/GMCM, MoreSaves, Y-console, AnimalPack, audio/BGM, and future platform work remain correctly deferred. |

## 2. Route Compliance

The implementation order matches the final W1 decision:

```text
Batch 0
-> player uninstaller ownership P0
-> Oil/OneAction ownership P0
-> version and release authority next
```

The full audit originally labeled Oil as 1A and installer ownership as 1B, but the later binding decision explicitly placed the installer P0 first. The observed order is therefore a refinement, not a deviation.

Positive route-control evidence:

- no public 0.5.5 artifact or bulk functional-Mod 1.0.0 promotion was created;
- no directory-wide QA/product move, public API deletion, UI rewrite, or animal-detail work was pulled forward;
- the Oil change began from the native drop owner and preserved official JSON ownership instead of stabilizing a Mod-layer workaround;
- the player uninstaller lost package ownership before any new author receipt system was introduced;
- retained subscription artifacts were treated as comparison evidence, not rewritten release inputs.

The material governance exception is Git durability. Each Update describes an independent rollback, but no commit currently makes those boundaries independently recoverable. Entering Batch 2 on top of this stack would turn a correct logical order into a difficult practical rollback and review history.

## 3. Regression Work Worth Running

### Immediate checkpoint gates, before Batch 2 implementation

1. Separate the explicitly excluded local analysis from the three mainline batches, then create reviewed Git checkpoints. Rerun the full Release test and `git diff --check` on the exact checkpoint tree.
2. Add an isolated developer-installer failure matrix for malformed `mod_infos.json` and enablement write failure. Prove that package publication and enablement either complete together or leave a deterministic, recoverable state.
3. Run one ordinary third-save product/owner refresh profile, not only `CoreOnly`, covering the three newly declared GameBridge dependencies (ActionSpeed, AutoHarvest, CropHarvestingQA), exact Entry counts, dependency resolution, no-op refresh, and cleanup.
4. Run the current finalized Oil profile script once end to end. The final profile/restoration control-flow hardening was source-tested and replayed after the two game runs, but the final script itself has not yet launched the game.

### Batch 2 and 0.5.5 compatibility gates

1. Test the intended player case directly: a Workshop Mod has updated while the manually installed Runtime is stale. Validate visible block, update guidance, and recovery without copying the Workshop Mod into OfficialLocal.
2. Make every player-visible/source/package version projection agree by policy, and fail closed on drift. Include Runtime manifest/DLL/status, Mod manifest, native `info.json`, minimum DTMAPI, Catalog, and published list.
3. Restore the obsolete-compatible `StopOnManualMove` member, diff the retained 0.5.2 public surface against the candidate, and load retained 0.5.1/0.5.2 consumer DLLs without recompilation under Unity Mono. The retained Workshop AutoFishing DLL is the first required consumer.
4. Exercise all four externally evidenced consumers and all 11 public product identities through Doctor/load classification before the release cutoff.
5. Before RC, run the Steam subscription/player startup chain and a no-HookProbe ordinary-input path. The two Oil P0 runs were DirectExe + HookProbe and cannot cover these paths.

### Later structural and GC gates

1. After QA extraction, rerun Oil-off, Oil-on, the four OneAction paths, profile restoration, and clean exit to prove that evidence capability moved without changing player behavior.
2. Compare player builds before/after demand activation with no Smoke settings and with CustomAnimals, AudioReplacement, AutoFishing, and ActionSpeed. Source or DLL size reduction is not a GC result.
3. Run AutoFishing and ActionSpeed independently through the already approved `1x`, enabled/no acceleration, common speed, high speed, disabled recovery, and title-loop ladders. Measure per-action and per-minute pressure; add dual-Animator arbitration only if a real owner overlap is observed.
4. Keep ISSUE-011 open until a current common-product run exceeds its historical short-crash window. The sub-minute Oil runs do not cover it.

## 4. New Problems And Newly Concrete Risks

### P1 release risk: Oil currently materializes two current versions

`testmods/OilMod/manifest.json` declares `0.3.1-dtmapi`, while `testmods/OilMod/official-info.json` declares `1.0.0`. `Install-OfficialLocalDtmApiMod` preserves both because it only fills the native info version when the field is missing or blank. Batch 0 intentionally records multiple version axes, but the current developer package therefore exposes `1.0.0` to the native official-Mod UI while DTMAPI metadata still reports `0.3.1-dtmapi`.

This is not a new gameplay regression and Oil remains `PrototypeBlocked`. It is a newly concrete example of the Batch 2 version-authority problem. Batch 2 must decide which value is the player-visible current product version and add a package-level equality/projection assertion instead of allowing an unexplained split.

### P1 developer-install recovery risk: package publication is not one transaction with enablement

The developer installer builds outside `MODS` and atomically moves the package directory into place, then reads/writes `SAVE/mod_infos.json`. If the enablement JSON is malformed or the enablement write fails, the published package remains. A rerun then refuses the existing destination by design, potentially leaving a stranded package requiring manual recovery.

This does not reopen the player uninstaller ownership P0: refusing to overwrite an unreceipted directory is correct. It exposes a missing failure/recovery contract between the P0 fail-closed behavior and the deferred Author-SDK transaction lane. The temp ownership matrix currently does not cover malformed/read-only/write-failure enablement.

### Known debt now reconfirmed by runtime evidence

The two latest `CoreOnly` runs still dispatched FishingAutomation, ActionSpeed, AudioReplacement, Camera, and CustomAnimalAnimatorBridge every frame despite no active consumer. This is evidence that Batch 5 remains necessary, not a regression introduced by the P0s.

Both runs also logged a missing-frame fallback and one or two Input System frame-driver stall/re-subscription warnings, then recovered and passed final health/exit gates. Similar warnings predate these P0s. They are currently transition/recovery diagnostic noise to measure during the input/GC gates, not sufficient evidence for a new debug issue.

No new Failed/Blocked game result, Fatal GC, fresh crash dump, or leftover process was found.

## Claims Supported And Not Supported

Supported now:

- Batch 0's offline identity/catalog baseline is verified;
- the player uninstaller no longer owns official Mod deletion;
- Oil no longer has a GameBridge/OneAction product coupling;
- focused Oil-off and Oil+OneAction native behavior passed with restoration and exit checks.

Not supported now:

- 0.5.5 is an RC or public artifact;
- the runtime has become lightweight;
- long-session or active-gameplay GC is solved;
- AutoFishing/ActionSpeed, Steam startup, normal no-HookProbe input, or the common full product set has passed on this worktree;
- Author-SDK receipt cleanup exists;
- Oil economy, Mine integration, or Oil product promotion is final.

## Review Validation

Run against the reviewed dirty tree:

- `tools/scripts/test.ps1 -Configuration Release`: passed in 169.1 seconds; all projects built for their existing targets with zero warnings/errors and `DTMAPI.UnitTests: OK`;
- player Runtime-only uninstall ownership matrix: passed;
- product Catalog checker: passed (`products=26`, `public=11`, `workshop-items=21`, `api-rows=45`);
- `tools/scripts/check-doc-governance.ps1`: passed after adding this review, its Update, and the ledger entry, 4,534 checks;
- `git diff --check`: passed;
- runtime lock: free; no `DolocTown.exe` process was present.

No new game process was launched for this review. Runtime conclusions are bounded to the preserved `GAME-SMOKE/20260713-221443` and `GAME-SMOKE/20260713-221610` evidence and the active smoke/debug records.

## Handoff

Do not begin Batch 2 until the Batch 0/P0 stack is separated from unrelated local analysis and committed as reviewed checkpoints. Then close the two developer-package regression gaps above at the Batch 2 entrance and proceed with one version/release authority. Do not pull QA extraction, hot-path activation, product splits, UI work, or GC completion claims ahead of their planned gates.
