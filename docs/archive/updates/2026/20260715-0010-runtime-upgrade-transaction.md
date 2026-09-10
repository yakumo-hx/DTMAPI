# 20260715-0010 Runtime Upgrade Transaction

## Metadata

- Update ID: `20260715-0010`
- Date: 2026-07-15
- Lifecycle Status: `verified`
- Validation Level: `docs,source,unit,runtime`
- Runtime Validation: `passed`
- Related Issue State: `none`
- Area: installer/runtime/upgrade/transaction/rollback/powershell/fault-injection
- Source: user requested the Batch 2 old-Runtime update, failure-recovery and PowerShell 5.1 gate after `docs/reviews/code/2026/20260715-0005-major-update-progress-and-decision-node-review.md` identified sequential Runtime replacement as a release risk.

## Scope

- replace the installer's five independent live-DLL overwrites with a staged Runtime-directory switch;
- validate the complete candidate before touching the installed Runtime: exact five-DLL set, nonempty managed assemblies, expected assembly names, binary file version, copy hashes, version authority, required installed tools, and candidate JSON state;
- switch the Runtime directory, installed `DTMAPI/tools`, `release-manifest.json`, and `install-state.json` as one rollback-capable operation while leaving unrelated `DTMAPI/config`, reports, logs, evidence, and backups untouched;
- keep candidate/recovery DLLs in a game-root transaction directory outside `BepInEx/plugins`, persist a path-validated `transaction.json` before and after each move, automatically roll an interrupted prior transaction back on the next install, and restore the complete byte-identical previous Runtime/tools/state set on any catchable failure at candidate preparation, old-directory movement, candidate placement, tools commit, release-manifest commit, or install-state commit;
- commit the validated Runtime and its base receipts before publishing developer official-local products, so a later package/enablement failure retains the new Runtime and writes an explicit bounded recovery receipt instead of producing old Runtime plus newly published products;
- add an isolated fake-game matrix under temporary paths containing spaces and non-ASCII characters, execute the copied package-like `Tools` tree there, and run it under PowerShell 7 and Windows PowerShell 5.1 without resolving or writing the real game directory.

## Known Facts And Rejected Paths

- The prior installer copied the five assemblies directly over the live directory and only later wrote tools and state. A failure in the middle could leave assemblies from two versions or new assemblies paired with old state.
- Per-file temporary replacement would still expose a mixed five-DLL set between moves. The Runtime payload is therefore prepared as a sibling directory and switched by same-volume directory moves.
- The entire `DTMAPI` state directory cannot be replaced: it also owns player config, reports, logs, evidence and backups. Only installer-owned `tools`, `release-manifest.json`, and `install-state.json` participate in the state switch.
- The developer official-local package/enablement transaction remains separate. This Runtime transaction neither widens that transaction's ownership nor rolls back third-party or official-local package state.
- The default developer path now commits Runtime before official-local package publication. Each product transaction still owns only its current package/enablement rollback; if a later product fails, earlier completed products remain installed and are enumerated in `FilesInstalledBeforeFailure` and `OfficialLocalAttempts`. This is an explicit recovery boundary rather than silent cross-transaction destructive authority.
- ISSUE-010 and ISSUE-011 remain open and unchanged. This source/fake-directory gate is not GC, gameplay, Steam-launch, or current-session injection evidence and creates no smoke-matrix row or Debug issue update.

## Changed Files

- `tools/scripts/install-to-game.ps1`
  - stages and validates the exact Runtime, icon, version authority and installed helper-tool candidate before switching live paths;
  - records assembly length, SHA-256 and managed `AssemblyFileVersion` in install/release state; byte-based managed metadata reading avoids the empty `FileVersionInfo` result observed on a long Unicode Windows path and leaves staged DLLs unlocked;
  - backs up and moves the old Runtime directory and the three installer-owned state entries, validates the committed projection, and restores every old entry in reverse order after a catchable failure;
  - clears each move flag immediately after its rollback action succeeds, treats missing recovery sources as errors, and permits the script-level trap to safely retry a partially failed rollback;
  - keeps candidate and old-recovery DLL directories outside the BepInEx scan tree, writes an atomic durable receipt around every move, validates all receipt paths against the current fake/live roots, automatically restores an interrupted uncommitted transaction, and fails closed on unowned state-only residue;
  - commits Runtime before developer product publication and records Runtime commit/rollback phase plus the bounded Runtime/product recovery ownership in `install-state.failed-*.json`;
  - exposes six guarded test-only fault points that require `DTMAPI_INSTALL_TRANSACTION_TEST_MODE=1`.
- `tools/scripts/test-runtime-upgrade-transaction.ps1`
  - creates only fake game, state, persistent and package roots below the system temporary directory;
  - covers six injected forward phases, a one-time failure inside rollback followed by script-trap retry, restart recovery from an interrupted `InstallStateCommitted` receipt, missing and corrupt candidate assemblies, and one successful upgrade;
  - asserts byte-identical rollback of the old Runtime, tools and both state files, preservation of unrelated state, exact new hashes on success, no mixed DLL set, and no transaction residue;
  - executes a copied package-like installer/tools tree from the non-ASCII temporary root and runs the eleven-case child matrix under both PowerShell 7 and Windows PowerShell 5.1 using ASCII test source that constructs its non-ASCII path segment at runtime.
- `tools/scripts/test.ps1`
  - adds the Runtime upgrade matrix to the Release test path and the PowerShell syntax gate while preserving the parallel Batch 2 release-contract additions.
- `docs/updates/INDEX-2026-07.md`
  - routes this lifecycle record from the July ledger.

## Validation

- The final unified dual-host Runtime entry point passed both eleven-case children in 119.8 seconds: six injected forward rollback boundaries, one deliberately failed/retried rollback, one persisted interrupted-transaction restart recovery, missing candidate DLL, corrupt managed DLL, and successful upgrade.
- Each injected case retained the byte-identical old five-DLL directory (including old-only files), old tools, old release manifest and old install state; each failure receipt recorded the exact failed phase and `RuntimeRollbackSucceeded=true`.
- The interrupted-restart case began from a simulated `InstallStateCommitted` receipt and mixed live/recovery layout, restored the byte-identical old set automatically, then failed the intentionally invalid new candidate without leaving transaction residue.
- The success case installed exactly five candidate DLLs with payload-identical SHA-256 values, projected Runtime `0.5.5` / binary `0.5.5.0`, replaced old-only Runtime/tools files, preserved unrelated report/config sentinels, and left no candidate, recovery, or state-transaction directory.
- `GAME-SMOKE/20260715-125327` exercised the player-visible stale-Runtime boundary before the update: an installed `0.5.3` Runtime rejected a Workshop CodeMod requiring `0.5.5` before Entry and emitted explicit update guidance. The run restored its profile and `mod_infos.json`, exited cleanly, and recorded one recovered missing frame plus one recovered frame-driver stall; selected-save restoration was not part of this gate.
- The live candidate install then injected a failure at `InstallStateCommitted`. The installer restored the exact previous Runtime, tools, release manifest and install-state hashes and left no transaction residue. A following ordinary install upgraded the same local Runtime to `0.5.5`; `check-dtmapi-status.ps1` passed the required-file, version and state projections.
- These live checks used the final-freeze working-tree candidate and do not replace the later exact-commit Release suite. They changed no Workshop subscription tree.
- The Runtime-only candidate package at `dist/workshop-packages-batch2-055-20260715/DTMAPI` passed the player-like subscription audit with zero blockers. Evidence is `docs/debug/evidence/WORKSHOP-SUBSCRIPTION-AUDIT/20260715-batch2-055-candidate/DTMAPI Workshop Audit 20260715-153835/Results/stress-summary.md`: Windows PowerShell `5.1.26100.8875` parsed all ten packaged scripts; install correctly rejected missing and empty game roots; install/status/log collection/uninstall passed in the valid game-shaped path with spaces and Chinese characters; post-uninstall status returned the expected nonzero missing-Runtime result without parser or strict-mode errors. The package contained 27 files / 3,622,318 bytes before the audit copy. This temporary matrix did not touch the real game or subscription tree.
- The existing developer official-local install transaction matrix passed after the final Runtime-before-product ordering under PowerShell 7 in 104.1 seconds and Windows PowerShell 5.1 in 73.0 seconds, preserving malformed JSON, invalid-shape, write-failure, drift, foreign-destination and recovery behavior.
- The developer matrix also exposed and closed two first-install/path portability defects before final validation: missing `BepInEx/plugins` parent creation and `FileVersionInfo` returning an empty version on a long Unicode candidate path.
- `Test-DtmApiWindowsPowerShellSyntax` passed for the installer, new matrix and merged Release test entry under Windows PowerShell 5.1 with PowerShell 7 fallback coverage.
- Focused `git diff --check` passed for the installer and test scripts with only existing line-ending normalization notices.
- No runtime lock was acquired because validation never installed to, uninstalled from, launched, or otherwise wrote the real Doloc Town environment.
- The exact merged-tree full Release suite remains the encompassing Batch 2 gate owned by `20260715-0011`; it was not duplicated as this focused transaction record's completion claim.

## Rollback

Revert the Runtime transaction functions and restore the prior direct-copy/state-write block together, then remove the dedicated matrix and its `test.ps1` invocation. Do not keep the state switch without its rollback matrix, and do not keep the matrix's test-only environment variables in a non-transactional installer.

The verified guarantee covers ordinary PowerShell, validation and filesystem exceptions that return control to the installer, including a partially failed first rollback followed by trap retry, plus restart recovery from a synthetically interrupted move sequence with a complete durable receipt. It does not claim power-loss atomicity if the filesystem corrupts or loses the receipt itself, nor automatic recovery for an abruptly terminated post-product two-file receipt refresh; those paths retain bounded recovery material and require operator inspection rather than destructive guessing.

## Follow-Up

- Run the exact merged-tree Release suite after the parallel Batch 2 version/product contract work settles.
- Preserve the stale-installed-Runtime block and injected-failure evidence when packaging the release candidate; the scoped local package/update/recovery gate is complete, while a post-upload Steam subscription check remains a release-operation concern.
- If Batch 2 later requires automatic continuation after a product failure, add a separate fingerprinted product-publication receipt; do not widen Runtime rollback authority to delete previously completed products.
- Keep normal Steam launch, no-HookProbe player input, eleven-product combination loading, and the independent AutoFishing/ActionSpeed GC ladders under their existing Batch 2 and later-GC gates.
