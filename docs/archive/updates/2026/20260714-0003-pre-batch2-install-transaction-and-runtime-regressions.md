# 20260714-0003 Pre-Batch 2 Install Transaction And Runtime Regressions

## Metadata

- Update ID: `20260714-0003`
- Date: 2026-07-14
- Lifecycle Status: `verified`
- Validation Level: `docs,source,unit,runtime`
- Runtime Validation: `passed`
- Related Issue State: `none`
- Area: installer/official-local/transaction/recovery/regression/oil/product-refresh/git
- Source: user requested the pre-Batch 2 regression gates identified by review `20260714-0001`, exact reviewable commits, and continuation into Batch 2

## Scope

This Update owns the final entrance checkpoint before Batch 2:

- make one newly published developer official-local package and its `mod_infos.json` enablement mutation fail or succeed as one recoverable per-package operation;
- add isolated malformed-JSON, enablement-write-failure, rollback, retry, and pre-existing-destination tests under both supported PowerShell hosts;
- rerun an ordinary non-`CoreOnly` ActionSpeed/AutoHarvest/CropHarvestingQA owner-refresh profile with exact load counts and cleanup checks;
- rerun the finalized Oil plus OneAction smoke profile end to end;
- validate the exact committed tree with the full Release test path and diff checks.

The player Runtime-only uninstaller boundary remains unchanged. A package created by the current invocation may be rolled back only through that invocation's transient publication ownership; legacy metadata, markers, failure-state files, and pre-existing directories grant no overwrite or deletion authority.

This Update does not implement the future Author SDK receipt lifecycle, publish a Workshop item, claim long-run GC evidence, extract QA from the player runtime, or change the selected Batch 2 version/ABI policy.

## Known Facts And Rejected Hypotheses Before Change

- `OWNER-PLATFORM-NOOP-REFRESH-20260712-011924` and `OWNER-VERSION-AUTHORITY-NOOP-20260712-101106` already proved an unchanged refresh can keep `loadedNow=0`, one Entry per loaded code Mod, and zero dependency errors. They did not cover the three newly corrected GameBridge dependency declarations together.
- `OIL-ONEACTION-20260713-221610` already proved the native Oil JSON route and generic OneAction coexistence. The missing evidence was a run after the final profile/restoration script hardening, not a new gameplay implementation.
- `ISSUE-010` remains open for independent active AutoFishing and ActionSpeed speed ladders. Both acceptance runs here are shorter than one minute, and the Oil run keeps AutoFishing inactive; neither is GC evidence.
- No current fact justified a new frame-driver Debug issue. A 2026-07-15 progress review corrected the earlier wording which said both runs contained no frame-driver stall: each run actually contains one recovered `fallback pump detected a missing frame callback` warning and two recovered `DTMAPI Input System frame driver stalled` warnings. Neither run contains `Fatal GC`, `Crash!!!`, a failed fatal window, or residual process evidence. The repeated warning shape predates this Update and remains a focused normal-player input classification gate rather than a newly proven Runtime regression.
- A controlled process exception can be made recoverable per package, but a process kill or power loss between directory publication and enablement commit still needs the future durable Author SDK receipt. This Update does not claim crash consistency.

## Changed Files

- `tools/scripts/install-to-game.ps1`
  - validates `mod_infos.json` before any official-local publication;
  - writes enablement through a unique temporary file;
  - records per-package publication/rollback diagnostics in failure state;
  - rolls back only a package published by the current invocation and only while both pre-move and post-move tree fingerprints still match;
  - restores and preserves unknown post-fingerprint changes instead of deleting them;
  - keeps logging outside the enablement correctness transaction and uses unique failure/backup stamps.
- `tools/scripts/test-developer-official-local-install-transaction.ps1`
  - runs the complete matrix under PowerShell 7 and Windows PowerShell 5.1;
  - covers malformed and wrong-shape JSON, locked writes, byte-identical retry, post-fingerprint drift, foreign pre-existing destinations, operator recovery, backups, and residue checks.
- `tools/scripts/run-game-smoke.ps1`
  - adds `-AssertProductOwnerRefresh`, explicit CropHarvestingQA fixture installation, exact Entry/transaction gates, no-op refresh/dependency checks, cleanup checks, result fields, and a dedicated evidence JSON file.
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs` and `Smoke/SmokeHarness.cs`
  - add the smoke-only `ActionSpeed config apply -> ReturnHome -> stable HomePageUiState -> exit` orchestration so ReturnedToTitle cleanup is observed before process exit.
- `tests/DTMAPI.UnitTests/Program.cs`
  - preserves the source gate requiring that dedicated ReturnHome orchestration.
- `tools/scripts/test.ps1`
  - includes the transaction matrix in tracked Release validation and PowerShell syntax coverage.
- `docs/workflows/workshop-package-subscription-test-matrix.md`
  - records the developer package/enablement transaction lane.
- `docs/debug/regressions/smoke-matrix.md`
  - records the ordinary owner-refresh and final Oil/OneAction runtime evidence.

## Validation

- PowerShell parser checks passed for the changed installer, matrix, runner, and test scripts under both PowerShell 7 and Windows PowerShell 5.1.
- The complete developer transaction matrix passed under both hosts (`hosts=2`): eight published definitions, malformed/wrong-shape preflight rejection, locked-write rollback/retry, a real unknown-file injection after the first fingerprint, fail-closed foreign-directory handling, and zero temporary/staging residue.
- `tools/scripts/build.ps1 -Configuration Release` passed after the smoke orchestration change with `0` warnings, `0` errors, and `DTMAPI.UnitTests: OK`.
- Third-save ordinary profile `GAME-SMOKE/20260714-034644` passed with no HookProbe:
  - `RunStatus`, save load, product owner refresh, ReturnHome orchestration, dependency compatibility, cleanup, no-fatal, and process-exit gates all passed;
  - ActionSpeed, AutoHarvest, and CropHarvestingQA each recorded exactly one Entry, one BeginTransaction, one CommitTransaction, and zero load failures;
  - the unchanged refresh recorded one direct `Workshop ModListChanged ... hotLoaded=0`, `LoadMods hot loadedNow=0`, and `dependencyErrors=0`;
  - the CoreUi profile was restored.
- Final third-save Oil/OneAction profile `GAME-SMOKE/20260714-034949` passed: Oil-only metadata, final native LUT, a natural `crude_oil` world drop, native drop/resource cleanup, all four OneAction paths, HookProbe, profile restoration, no-fatal, and process-exit gates passed. Attempt `8/256` produced `coal|coal|crude_oil`; all three created drops were removed with `remainingInDM=0`.
- Both runtime rounds used independent receipts. `mod_infos.json` and all three third-save files were restored to their exact pre-run SHA-256 values; original package/runtime/loose-Mod directories were restored, originally absent paths stayed absent, no new `.dtmapi-staging-*` directory remained, and `DolocTown.exe` count was zero before releasing the shared lock.
- `git diff --check` passed for the scoped source files (Git emitted only line-ending conversion warnings).
- The tracked full Release gate and exact committed-tree replay are the final commit gate; their result is reported with the commit rather than treated as runtime evidence.

## Evidence

- [ordinary product-owner refresh result](../../../debug/evidence/GAME-SMOKE/20260714-034644/result.json)
- [ordinary product-owner refresh gate](../../../debug/evidence/GAME-SMOKE/20260714-034644/product-owner-refresh-gate.json)
- [ordinary runtime restoration verification](../../../debug/evidence/GAME-SMOKE/20260714-034644/runtime-restore-verification.json)
- [final Oil/OneAction result](../../../debug/evidence/GAME-SMOKE/20260714-034949/result.json)
- [final Oil/OneAction runtime restoration verification](../../../debug/evidence/GAME-SMOKE/20260714-034949/runtime-restore-verification.json)

## Rollback

Revert the transaction helper, its isolated test matrix, runner-side post-gates, and this Update together. Do not revert the already independent Batch 0, player-uninstaller P0, or Oil P0 commits.

## Follow-Up

After this checkpoint is committed and verified on its exact tree, begin Batch 2 with one authoritative version projection and the retained binary-compatibility gate. The AutoFishing and ActionSpeed GC ladders remain later independent work.
