# 20260721-0004: AutoFishing Product Slimming

## Metadata

- Update ID: `20260721-0004`
- Date: `2026-07-21`
- Lifecycle Status: `verified`
- Validation Level: `docs,source,unit,runtime,player`
- Runtime Validation: `passed`
- Related Issue State: `closed`
- Area: products/autofishing/product-native/slimming/qa-seam/hooks/lifecycle/config/package
- Source: User request to close AutoFishing D.5 before admitting one second real product, preserving player behavior, configuration, Workshop identity, frozen ABI, atomic Hook installation, fail-closed behavior, duplicate-settlement protection and native/input/animation restoration.
- Owning Review: [AutoFishing Product Weight And Config Reuse Review](../../reviews/code/2026/20260721-0004-autofishing-product-weight-and-config-reuse-review.md)
- Acceptance correction Review: [Two-Product Baseline Closeout Feedback](../../reviews/manual-qa/2026/20260722-0001-two-product-baseline-closeout-feedback.md)
- Route authority: [AutoFishing Product Slimming Route](20260721-0003-autofishing-product-slimming-route.md)
- Prior implementation: [Batch 6 AutoFishing Advanced Pilot](20260720-0008-batch6-autofishing-advanced-pilot.md)

## Scope

This Update owns Checkpoint D.5 inside the already admitted `Yuuka.DTMAPI.AutoFishing` Advanced product. It will:

- delete production code with no consumer and remove the retired Legacy/Shadow migration branches;
- move QA-only observation out of continuously maintained player state, or reduce it to an explicitly activated bounded seam;
- fold single-owner session/router/lease/transaction/cache layers only when their protected failure or restoration behavior remains explicit;
- preserve the current UniqueID, Workshop item, product version, manifest/package identity, config keys/defaults and unified `IDtmConfigMenuApi` registration;
- preserve all 22 product-owned Hook targets under canonical owner `dtmapi.mod.yuuka.dtmapi.autofishing`, full pre-resolution, atomic installation and fail-closed rollback;
- preserve exactly-once bite/reel/energy settlement, sequence guards, F6/manual-movement cancellation, title/save/owner cleanup and native/input/animation restoration;
- leave the frozen `IFishingAutomationApi` compatibility executor and public ABI in mandatory GameBridge unchanged for the 0.5.5 window.

This Update does not release 0.5.5, upload or alter Workshop/local official content, admit another real product, open general Advanced authoring, add a public API, create a new receipt family, or move ProductNative code back into mandatory Runtime.

## Native-owner boundary

The established native responsibility map remains unchanged:

- `AgentStateFishingReady`, `AgentStateFishingCast`, `AgentStateFishingWait`, `AgentStateFishingPull` own the fishing state transitions;
- `AgentStateFishingWait.RollFish` / `NextState` and native state overwrite own bite and reel progression;
- `FishingGameScrollBar.StartGame` / `UpdateGame` / `StopGame` own the visible minigame;
- `DolocUserInput` owns native fishing/tool/item input reads;
- `FishRodRenderer.CastHook` / `Pull` / `PullCancel` plus Hook body physics own animation and line restoration.

These are ProductNative adapters for one product. No SharedNative or public API promotion is implied.

先做本轮 API/domain 的 native owner 方法体审查；未找到 native owner 或状态持有者前，不得通过 mod 层补丁冒充 API 重做完成。

## Slices and validation

1. Dead code and migration scaffolding: product SDK build, focused AutoFishing decision/config unit checks, source/owner/ABI gates.
2. QA-only diagnostics and observation: focused QA compile/unit plus product package exclusion and reflection-observer checks.
3. Single-owner state/native folding: affected source/unit/package/owner checks after each responsibility-sized change.
4. Frozen final candidate: compare player source/file count, SDK DLL/package tree, inactive roots and short allocation authority.
5. Runtime: one short fifth-save enabled/F6-disabled/re-enabled/returned-to-title/re-entry/clean-exit run with native/input/animation restoration and no residual process.

The complete Release suite, L0-L5 ladder, 10/30-minute windows, 100/500 loops and long soak are intentionally excluded unless a focused short metric proves a regression that requires escalation.

## Current baseline

- Player source: 23 C# files, 6,266 physical lines.
- Optional product QA: 23 C# files, 8,348 physical lines; excluded from the player DLL/package.
- Current SDK entry DLL: 155,136 bytes at the review checkpoint.
- Frozen compatibility Runtime: separate 9-file/4,164-line GameBridge debt; unchanged by D.5.

## Changed files

- `products/first-party/AutoFishing/src`: removed `IFishingHookRuntime`, legacy/shadow decision branches, the unused lifecycle publication gate, resident diagnostics/telemetry and duplicate single-consumer state holders; folded session/router/lease/cache paths while retaining explicit settlement/restoration guards.
- `products/first-party/AutoFishing/qa`: changed the reflection observer and recovery driver to explicitly activate the bounded QA seam; the observer now fail-closed enumerates input override, pending visible-reel input, Ready targets/releases/current state, Animator speeds, Hook gravity/velocity and Pull-duration snapshots. The seam remains absent from ordinary packages.
- `tests/DTMAPI.UnitTests`, `tests/DTMAPI.QaUnitTests` and `tools/scripts/test-batch6-autofishing-behavior-matrix.ps1`: updated focused assertions for the reduced topology and on-demand diagnostics.
- `tools/scripts/build-batch6-advanced-product.ps1` and the thin AutoFishing wrapper: moved common Advanced validate/build/pack/package checks to the Catalog-driven two-consumer implementation.
- `docs/debug/regressions/smoke-matrix.md`, this Update and the roadmap/Batch 6 current-state records: recorded the bounded final result without copying the historical full-suite receipts.

## Validation

Focused results:

- Author SDK Release validate/build/pack: PASS; final player package contains one product DLL, no native dependencies and no QA assembly.
- `DTMAPI.UnitTests`: PASS after the topology reduction.
- `DTMAPI.QaUnitTests`: PASS with explicit QA activation and no static product AssemblyRef.
- `test-batch6-autofishing-behavior-matrix.ps1`: PASS for decision/config, exactly-once settlement, 22-target/owner, fail-closed and restoration source contracts.
- Deep-state QA unit: PASS after seeding every named deep holder; the aggregate detected all ten seeded states in addition to the four existing transient states.
- Player source changed from 23 files / 6,266 physical lines / 5,765 non-empty lines to 22 files / 5,393 physical lines / 4,964 non-empty lines: `-873` physical lines (`13.93%`) and `-801` non-empty lines (`13.89%`). Optional QA remains package-excluded. Entry DLL changed from 155,136 to 131,072 bytes, a reduction of about `15.5%`.
- Final package SHA-256 `D29768A1FB2E3D0BA0AFBACE214B006A2B0D509E54197215B3F3AD1CA10AB409`; entry DLL SHA-256 `9B97BAEA2FCD05788CDC91D90F60878C88B0A17EC43CF433D478DB8D09233B05`.
- Catalog-driven Advanced build/package validation: PASS with the unchanged final AutoFishing package and DLL hashes; the generic live zero-leftover gate covers both admitted products.
- Windows PowerShell 5.1 parsing for the modified Advanced/release/smoke scripts: PASS; document governance: PASS (`5583` checks); `git diff --check`: PASS.
- One exploratory direct invocation of `test-retained-autofishing-abi.ps1` was rejected before testing because its mandatory artifact arguments were omitted. This was not an ABI failure; private retained inputs were absent and the canonical suite would skip that optional exact-artifact route. Public ABI declarations and existing unit gates remained unchanged and passed.
- No complete Release suite, L0-L5 ladder, 10/30-minute window, 100/500 loop or soak was run.

## Evidence

- Corrected deep-state short regression: `docs/debug/evidence/AUTOFISHING-SLIMMING/20260722-deep-state-closeout/regression-summary.json` (`Status=Passed`, slot 5, receipt-bound package).
- Game smoke root: `docs/debug/evidence/GAME-SMOKE/20260722-074938`.
- One actual process loaded the fifth save, enabled/disabled through four physical F6 inputs, returned to title, loaded the fifth save again and ended disabled. Final state retained the exact 22 installed inactive callbacks with updater/session absent. `nativeTransientCount=0` and `deepNativeTransientCount=0`; input override and pending visible-reel input were false; Ready targets/releases/current state, Animator-speed snapshots, Hook gravity/velocity snapshots and Pull-duration snapshots were each zero.
- This proves that every enumerated AutoFishing product-held override/snapshot container is empty at the cleanup boundary and that the native behavior recovered. It does not claim a byte-for-byte census of every game-owned native object.
- Save, official profile, local source/deployment, fatal-window and process-exit restoration gates passed; the runtime lock was released and no `DolocTown.exe` remained.
- The receipt authority is `non-authoritative-short-deep-state-regression`: it is intentionally a short behavior/recovery proof, not an L5/GC or Release authority.
- The earlier `GAME-SMOKE/20260722-004352` run remains the preliminary D.5 short regression; after the user identified the shallow observer it no longer serves as the final deep-state acceptance.
- An earlier invocation at `docs/debug/evidence/AUTOFISHING-SLIMMING/20260721-d5-short-4b4b540d` was rejected by long-run preflight before SaveLoaded/product behavior because the parameters accidentally selected an L5 contract without its required behavior declaration. It launched no valid functional case and is not counted as the reserved final run.

## Related records

- Hook map: update only if target, signature, owner, lifecycle or evidence changes.
- Smoke matrix: add one row only after the final fifth-save runtime run.
- Public API matrix: no change expected; the frozen ABI is out of scope.
- Debug issues: update only if runtime evidence changes ISSUE-010/011 facts.

## Rollback

Revert the D.5 product-source and focused test/tooling changes while keeping the accepted Batch 6 Advanced policy, product identity/package authority and frozen Compatibility path. No rollback may reintroduce product execution into mandatory GameBridge.

## Follow-up

D.5 is closed with the corrected metrics and bounded deep-state recovery proof. The separate [OneActionComplete admission Review](../../reviews/code/2026/20260721-0005-oneactioncomplete-second-product-admission-review.md) accepted exactly one second real product; its implementation is owned by Update `20260721-0005`. Historical L0-L5/full Release evidence remains frozen, and 0.5.5 publication stays paused.
