# 20260715-0006 Batch 2 Closure And Batch 3 Predecision Review

Status: recorded
Date: 2026-07-15
Scope: review the claimed Batch 2 completion, decompose the retained Lamp ABI decision, and print the still-open Batch 3 Author SDK decisions without reopening frozen product direction
Related Update: `docs/updates/2026/20260715-0014-batch2-closure-and-batch3-predecision-review.md`
Primary route: `docs/reviews/code/2026/20260712-0003-dtmapi-full-boundary-audit.md`
Decision closure: `docs/reviews/code/2026/20260713-0012-major-update-sixth-decision-docket.md`
Prior progress review: `docs/reviews/code/2026/20260715-0005-major-update-progress-and-decision-node-review.md`

## Source Request And Screenshot Transcription

The user stated that Batch 2 was complete, asked for an audit, asked that the Lamp decision be decomposed according to the current lightweight/compatibility direction, and asked for the remaining Batch 3 decisions so they could decide them in advance.

The supplied screenshot says:

- an exact retained Runtime 0.5.2 comparison found 92 candidate deletions, all under `ILampControlApi`, `LampManualToggleOptions`, `LampManualToggleRegisterResult`, and `LampManualToggleState`;
- none of the eleven retained first-party public product DLLs references those types, but unknown historical/external DLLs cannot be excluded;
- option 1 restores the ABI while explicitly disabling the behavior, option 2 explicitly retires/deletes it, and option 3 fully rebuilds Lamp behavior;
- Release/build/unit and PowerShell-focused gates were said to pass, while normal-Steam/no-HookProbe/final eleven-product runtime gates remained after the decision;
- three implementation P1s were already noticed in the Smoke gate: title stability timing, refusing player-file restoration while the game remains alive, and an incomplete Update 0013 rollback inventory.

This review is read-only with respect to Runtime, game, Workshop, local official packages, subscriptions, and player saves. It excludes the user's unrelated local analysis and third-party non-DLL Mod repair work.

## Snapshot And Executive Verdict

Branch: `codex/major-update-batch0-20260713`

HEAD: `a0bff2dbdd47f4186bf83d48d58600329f2a5200` (`test: make Oil release semantics hermetic`)

The worktree contained 54 status entries before this review record: 44 tracked modifications and 10 untracked Batch 2 paths. The Batch 2 implementation is not committed.

The accurate current statement is:

> Batch 2 功能主体已经写出，但闭环验证未完成，因此不能标为 complete/verified。

| Layer | Audit result |
| --- | --- |
| Source implementation | Unified Runtime version authority, eleven-product projections, real minimum preservation, release checks, staged Runtime upgrade transaction, restored `StopOnManualMove`, exact-ABI harness, and player-like Steam gate scaffolding are present. |
| Focused automatic evidence | Runtime transaction Update 0010 is verified; focused Build/Unit/Catalog/Release-contract checks have passing evidence. |
| Merged Release entry point | The first audit run reached `DTMAPI.UnitTests: OK` but failed because untracked Update 0013 was absent from the evidence-retention allowlist; concurrent UnitTests also caused output-lock retry warnings. After regenerating the allowlist and removing test concurrency, the exact entry point passed in 710.9 seconds with zero build warnings/errors, both transaction host matrices, Catalog `26/11/21/45`, and the Batch 2 Release contract. This satisfies Update 0011's stated merged-automatic condition but does not execute the opt-in strict ABI or Unity/Steam gates. |
| ABI | The strict exact-artifact gate intentionally fails with 92 deletions until the Lamp decision is implemented; `StopOnManualMove` itself binds and preserves its historical default. |
| Unity/Steam | Retained AutoFishing under Unity Mono, stale-installed-Runtime update/recovery, exact eleven-product enabled/disabled normal-Steam runs, and the no-HookProbe foreground-input run have not executed on this candidate. |
| Governance | Continuing Update 0004 and slice Updates 0011, 0012, and 0013 remain `in-progress`; Update 0013 omits part of its actual changed/rollback surface. |

Even after Batch 2 closes, this does not make DTMAPI 0.5.5 release-ready. QA extraction, hot-path/demand activation, the complete publicly obtainable external-ecosystem scan, and the independent AutoFishing/ActionSpeed active-gameplay GC gates remain later 0.5.5 release gates.

## Batch 2 Findings

### P0 compatibility stop: 92 retained Lamp signatures are absent

The exact retained `DTMAPI.Abstractions.dll` has SHA-256 `39A51683034FF0B7495BEB3DD1C50F76F591D4E24C63872B6502EA360EF8880B`, Assembly/File version `0.5.2.0`, and four Lamp public types which are absent from the candidate. The retained binary contains 92 corresponding type/method/property/base/kind signature records.

The retained information commit does not contain those types in tracked Abstractions source. That establishes a published-binary/source drift, not permission to remove the published binary contract.

No Lamp `MemberRef` was found in:

- the eleven retained first-party public product DLLs;
- the five DLLs in the four currently visible external subscription packages inspected during this review.

This narrows known risk but does not constitute a complete historical/public ecosystem scan and does not authorize deletion under frozen U1/I1 or the 0.5.5 zero-deletion gate.

### P1: title stability is measured from the transition request, not continuous observation

Several Smoke paths start a timer when `ReturnHome` is requested and later treat “now minus request time >= 1.5 seconds” as proof that `HomePageUiState` itself has stayed stable for 1.5 seconds. If the transition consumes the whole interval, the first observed HomePage frame can pass immediately.

Affected paths include the product refresh cleanup around `SmokeHarness.cs:1770`, external-player-input cleanup around `SmokeHarness.cs:1948`, and TitleButtonLifecycle around `SmokeHarness.cs:2114`. `EnsureSmokeAutoReloadCompletedForSmoke` around `SmokeHarness.cs:1842` is the correct local model because it starts a separate observation timer only after HomePage is actually seen and resets it when the state is lost.

Acceptance: each lifecycle gate owns a separate observed-state timestamp, resets it whenever the expected state is absent, and passes only after a continuous stable interval. Source-string assertions are insufficient; add focused logic tests and then normal-Steam evidence.

### P1: player files can be restored while DolocTown is still alive

`run-game-smoke.ps1:3508-3519` attempts normal close and then force close, but `$leftover` may still contain a live process. The outer `finally` beginning around line 3582 then restores selected save files, `mod_infos.json`, smoke settings, and temporary product configs without requiring confirmed process exit.

This can race the still-running game and overwrite player state. If process exit cannot be proved, the runner must preserve backups/receipts and fail closed without writing any player-owned file. Restoration may be retried only after an operator or later recovery command proves the game is gone.

### P1: Update 0013 does not own its complete implementation/rollback surface

Update 0013 lists only `run-game-smoke.ps1`, `test.ps1`, and the July index. Its actual slice also changes:

- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`;
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`;
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`;
- `tests/DTMAPI.UnitTests/Program.cs`.

The documented rollback would therefore leave half of the handshake/reload runtime path behind. Update 0013 must list and roll back the full slice, or explicitly split mixed changes into their actual owning Updates before implementation closure.

### Resolved review hygiene: merged Release allowlist was stale

The 2026-07-15 merged Release run reached build/unit success and then stopped at `build-evidence-retention-allowlist.ps1 -Check`. A read-only generated comparison shows the only missing source entry is:

`docs/updates/2026/20260715-0013-batch2-steam-player-input-and-public-product-gates.md`

The tracked allowlist was regenerated after the new Review/Update files were finalized. A no-concurrency replay of the complete Release entry point then passed in 710.9 seconds with zero build warnings/errors. The original failure remains useful evidence that every new evidence-bearing Update must synchronize the generated allowlist before claiming a merged pass.

## Lamp Decision Decomposed

The screenshot's option 1 should be treated as five orthogonal decisions instead of one vague “restore Lamp” choice.

### L-A: binary shape

**Recommended L-A1:** restore all four types, all 92 signatures, and historical defaults in 0.5.5.

- `LampManualToggleOptions`: `Enabled=true`, `EquipmentIds=[]`, `VerboseLogging=false`.
- Result/state DTOs: strings/arrays empty, booleans false, counts zero.

Rejecting L-A1 means reopening the already selected U1/I1 and 0.5.5 zero-old-ABI-removal policy; it is not a local cleanup decision.

### L-B: provider behavior

**Recommended L-B1:** the historical GameBridge provider returns a non-null, owner-bound `ILampControlApi` compatibility facade.

Restoring declarations while letting `GetApi<ILampControlApi>` return null preserves metadata but creates an avoidable null-reference trap for an unknown old consumer. Registration must bind the supplied manifest to the canonical requesting owner; string queries must not reopen arbitrary cross-owner reads.

### L-C: runtime behavior

**Recommended L-C1:** every operation deterministically fails closed with stable `retired-disabled` semantics.

- `RegisterManualToggle` returns `Success=false`, `Enabled=false`, and `HookInstalled=false`;
- query methods return an explicit disabled/unavailable status;
- no Lamp Hook, native light mutation, gameplay state, session override, per-frame/lifecycle feature dispatch, or file polling is restored;
- emit at most one owner-scoped warning on actual use, so zero consumers create zero work.

### L-D: lifecycle and warning policy

**Recommended L-D1:** mark the interface and all three DTOs `[Obsolete(..., false)]`, classify the API as `Disabled` / `Retired compatibility shell`, and teach Author SDK/Doctor to report deprecated use and migration guidance.

This is a status downgrade and warning cycle, not an ABI deletion. Physical removal may be reconsidered only after a published warning-bearing cycle, renewed ecosystem/author-supplied scans, and an explicit breaking minor boundary no earlier than 0.6.0. Discovery of a real consumer automatically pauses retirement.

### L-E: future Lamp feature

**Recommended L-E1:** do not rebuild Lamp gameplay in Batch 2. Any future real demand gets its own native-owner/API project.

The retained old implementation is not a trivial DTO shell: it contains equipment interaction, room/light refresh, session override, and built-in lamp behavior. Current source has no matching native-owner review or Hook Map proof. The old binary may inform compatibility research but must not be copied or treated as current clean-rebuild authority.

Recommended Lamp answer set:

```text
L-A1 / L-B1 / L-C1 / L-D1 / L-E1
```

Resolution, 2026-07-15: the user selected the complete recommended set and additionally froze deletion as a breaking-version decision no earlier than 0.6.0 after a warning cycle and renewed scans. Implementation and validation are owned by `docs/updates/2026/20260715-0012-retained-autofishing-abi-host-gate.md`; this Review's earlier snapshot remains the pre-decision audit record.

## Batch 3 Frozen Boundaries

Do not reopen these in the Batch 3 vote:

- D1: one independent, versioned Author SDK; player package retains only read-only Doctor/error guidance;
- A2: only SDK/internal deployment receipts plus matching external state may authorize update/withdrawal; `dtmapi-package.json` is permanently non-destructive;
- B0/B1/B2/B3: player Runtime is lifecycle-driven with zero polling, SDK supplies explicit content reload first, a dev watcher may come later, and ordinary-player polling is rejected;
- CodeMod DLLs are never hot reloaded and source changes after load require restart;
- G1: Workshop is player authority; SDK owns the four explicit source modes and per-Mod local overrides;
- ordinary DTMAPI Mods never go under `BepInEx/plugins`; external plugins are metadata-only/read-only diagnostics and are never moved, deleted, adopted, enabled, or disabled by DTMAPI;
- generated game-loaded CodeMods remain `netstandard2.0`;
- current manifest/custom-animal/audio shapes are frozen inputs; Batch 3 must not silently add a wrapper or `Format` field, invent AnimalPack economy/content IDs, or include first-party/third-party assets in templates.

QA extraction remains Batch 4 and must not be folded into the Author SDK implementation.

## Batch 3 Decisions For Advance Selection

Follow-up: `20260715-0007-decision-escalation-policy-and-product-nodes.md` reclassifies the recommended A-I set below as evidence-gated engineering defaults, not a still-open user ballot. The alternatives remain useful as reversal paths if a recorded validation gate fails.

The option labels below are local to Batch 3. In replies and later records, prefix them with `SDK-` (for example `SDK-A1`) so they cannot be confused with the already frozen first/second-round A-I decisions.

### A. SDK distribution and host

| Choice | Meaning | Assessment |
| --- | --- | --- |
| **A1 recommended** | Independent `DTMAPI Author SDK 0.1.0`, Windows x64 self-contained .NET 8 portable ZIP, one `dtmapi-author` CLI; BAT/PS are launchers only. | No author-side .NET prerequisite; strongest single implementation authority; larger download. Tool host may be .NET 8 while generated CodeMods stay netstandard2.0. |
| A2 | Framework-dependent .NET 8 tool. | Smaller artifact, but authors must install the correct host. |
| A3 | PowerShell-only script collection. | Fastest start but repeats the fragmented-script problem and is weak for metadata, schema, and transaction work. |

### B. First SDK compatibility target

| Choice | Meaning | Assessment |
| --- | --- | --- |
| **B1 recommended** | SDK 0.1.x generates/builds only for DTMAPI 0.5.5; Doctor may inspect older Mods, but no 0.5.2/0.5.5 multi-target template. | Keeps one truthful author baseline; SDK version remains independent from Runtime and Mod versions. |
| B2 | Start the SDK itself at 1.0.0, still targeting 0.5.5. | Strong stability signal before the workflow is field-tested. |
| B3 | Maintain 0.5.2 and 0.5.5 templates/references from the first release. | Doubles the compatibility matrix and carries a stale author route forward. |

### C. Delivery slicing

| Choice | Meaning | Assessment |
| --- | --- | --- |
| **C1 recommended** | 3.1 CLI/schema/templates/validate/pack/read-only Doctor; 3.2 source modes plus receipt deployment/update/withdrawal; 3.3 explicit B1 content reload. Publish one preview only after all three pass. | Small reversible implementation slices without publishing a half-authoritative tool. |
| C2 | Publish after 3.1, add deployment/source/reload later. | Earlier feedback, but users may mistake an incomplete path for the supported workflow. |
| C3 | One large implementation. | Weak regression attribution and rollback. |

### D. Abstractions reference source

| Choice | Meaning | Assessment |
| --- | --- | --- |
| **D1 recommended** | SDK bundles a hash/version-fixed 0.5.5 Abstractions reference plus shared MSBuild props; projects do not read the game install or require public NuGet. | Offline, reproducible, and tied to the SDK compatibility manifest. A NuGet mirror can be added later. |
| D2 | NuGet-only reference. | Smaller SDK, but network and package publication become build prerequisites. |
| D3 | Reference the player's installed `BepInEx/plugins/DTMAPI` DLL. | Couples author builds to a mutable manual install and should be rejected. |

### E. Templates and metadata authority

| Choice | Meaning | Assessment |
| --- | --- | --- |
| **E1 recommended** | Two minimal templates: CodeMod and ContentPack. `manifest.json` owns Runtime identity/version/minimum/dependencies; SDK-only project data owns build/publish inputs and generates/validates official info plus Workshop-shaped output. Complex examples live in a cookbook. | Avoids another hand-maintained version triplet while keeping the Runtime's actual manifest as truth. |
| E2 | Add a new `dtmapi-project.json` as the single source which generates manifest/info. | Internally tidy, but creates a new DTMAPI-only author format before the SDK is proven. |
| E3 | Authors manually align manifest, official info, and csproj. | Recreates the Batch 2 drift and should be rejected. |

### F. Existing-directory adoption and force

| Choice | Meaning | Assessment |
| --- | --- | --- |
| **F1 recommended** | No `force` or `adopt`. Deploy only to a new destination or update/withdraw when package receipt and external state match exactly; unknown drift refuses. Withdrawal moves to SDK recovery/backup first. | Direct continuation of A2 and the ownership P0. |
| F2 | Explicit adopt after full inventory, backup, and identity confirmation. | Useful later, but substantially expands the first release's ownership and recovery surface. |
| F3 | Force overwrite/delete. | Contradicts frozen ownership rules and should be rejected. |

### G. Doctor mutation authority

| Choice | Meaning | Assessment |
| --- | --- | --- |
| **G1 recommended** | `doctor` is always read-only and exports human text plus machine JSON. Mutations are separately named `deploy`, `undeploy`, `source`, and `reload` operations limited by SDK receipts. | Keeps diagnosis separate from authorization and matches the player Doctor boundary. |
| G2 | SDK Doctor offers interactive one-click repair. | Even if player Doctor stays read-only, it blurs diagnostic and destructive authority. |
| G3 | Player Doctor repairs/moves DLLs. | Already rejected by D1 and the BepInEx boundary. |

### H. Developer source-override persistence

| Choice | Meaning | Assessment |
| --- | --- | --- |
| **H1 recommended** | Store overrides outside packages, keyed by game install plus `UniqueID`; show them clearly. Player Reproduction snapshots and clears all overrides, then requires explicit restore. Override state is not an ownership receipt. | Reproducible across restarts without contaminating packages or granting file authority. |
| H2 | Session-only overrides. | Fewer persistent files, but poor restart/reproduction consistency. |
| H3 | Local directory presence automatically overrides Workshop. | Already rejected by G1. |

### I. Workshop authority in Batch 3

| Choice | Meaning | Assessment |
| --- | --- | --- |
| **I1 recommended** | Deterministic pack/validate/hash/report only. Do not create Workshop items, store credentials, or upload. Workshop Validation only verifies an already downloaded Steam tree. | Keeps account/network/publishing failure semantics outside the authoring baseline. |
| I2 | Add an explicit upload command. | Requires a separate Steam/account/failure-recovery project. |
| I3 | Replace the official uploader. | Far beyond Batch 3. |

Recommended Batch 3 answer set:

```text
SDK-A1 / SDK-B1 / SDK-C1 / SDK-D1 / SDK-E1 / SDK-F1 / SDK-G1 / SDK-H1 / SDK-I1
```

## Facts To Determine, Not Preference Votes

1. Whether every official-JSON or mixed package can execute completely from local `game/Mods`, or whether some native content requires the official-local `MODS`/Workshop lane, must be established from native `ModManager` ownership plus runtime evidence. The SDK should select deployment lane by validated package capability, not author preference.
2. The B1 reload transport is an implementation review. It must be zero-file-polling, per-`UniqueID`, transactional, last-good preserving, and incapable of DLL reload; whether that uses explicit in-game invocation or demand-only IPC follows from the smallest proven lifecycle design.
3. Doctor must classify unknown DLLs from metadata without loading or executing them. Error/warning thresholds follow the already selected four-way Runtime/CodeMod/ContentPack/external-BepInEx classification.

## Closure Order And Acceptance

Batch 2 closure order:

```text
repair the three Smoke P1s
-> synchronize the evidence-retention allowlist
-> decide and implement the Lamp compatibility shell
-> exact retained ABI deletion count = 0
-> complete Release entry point passes without concurrent locks/warnings
-> retained AutoFishing Unity Mono load/behavior
-> stale installed Runtime block/update/recovery
-> normal-Steam eleven enabled + eleven disabled + no-HookProbe input gates
-> byte-verified player state restoration, clean exit, evidence records
-> Updates 0004/0011/0012/0013 truthfully close and Batch 2 is committed
```

Batch 3 completion must prove clean new-to-build-to-validate-to-deterministic-package for both templates, metadata-only Doctor classification, no ordinary Mod under BepInEx, fail-closed receipt matrices, all four source modes, owner-bound deployment/disable/cleanup/restart behavior, and B1 content reload with invalid input retaining the last good generation.

## Validation Boundary

This review cross-checked current Git state, Batch 2 Updates, the exact retained ABI report/harness, current candidate source, Product Catalog, public API matrix, second/third/sixth decision dockets, full-boundary work order, Smoke implementation and tests, and current visible first-party/external binary MemberRefs. The first merged Release attempt exposed the stale allowlist and concurrent output lock; after synchronization and a no-concurrency replay, `tools/scripts/test.ps1 -Configuration Release` passed in 710.9 seconds with zero build warnings/errors. The opt-in strict ABI test remains red by design with 92 Lamp deletions. No final candidate Workshop package exists, so the package-level subscription stress matrix was not run against the old 0.5.2 package as a substitute. This review did not acquire the Runtime lock, install or launch Doloc Town, modify subscriptions, decide an option for the user, or implement Batch 2/3 behavior.
