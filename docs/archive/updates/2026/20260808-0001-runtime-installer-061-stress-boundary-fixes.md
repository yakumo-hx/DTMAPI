# Runtime Workshop 0.6.1 stress-boundary fixes

- Update ID: `20260808-0001`
- Date: `2026-08-08`
- Lifecycle Status: `implemented`
- Validation Level: `docs, source, unit, runtime`
- Runtime Validation: `passed`
- Related Issue State: `mitigated`
- Area: `release/workshop/installer/0.6.1/host-proof/mutation-lock/uninstall/log-integrity`
- Source Request: 在独立 V2 候选继续开发时，并行小修现有 0.6.1，不进行安装器重写
- Source Review: [20260807-0002](../../reviews/code/2026/20260807-0002-runtime-installer-three-commit-stress-audit.md)
- Debug Issue: [ISSUE-023](../../../debug/issues/ISSUE-023-20260808-runtime-installer-stress-boundaries.md)

## Scope

This Update changes only the existing 0.6.1 installer boundary and its focused
tests. After the source commit and exact-package audit passed, the same package
was synchronized to the local official upload directory and installed into the
real Doloc Town game directory under the shared Runtime lock. It did not edit
or package the cached V2 candidate, update Steam, launch the game, or touch a
save.

The bounded fixes are:

1. require a nonce-bound result file before CMD accepts a PowerShell candidate,
   and allocate it inside an atomically claimed per-invocation directory so
   identical `%RANDOM%` sequences cannot collide across CMD processes;
2. serialize install and uninstall for one normalized game directory with a
   process-lifetime named mutex;
3. make uninstall fail closed before mutation when a Runtime recovery
   transaction is pending;
4. give uninstall receipts and log bundles collision-resistant names and report
   a truthful no-op uninstall;
5. publish desktop log output only after staging completes, and copy the newest
   ten DTMAPI logs in full with stability and SHA-256 verification.

The Runtime directory transaction, receipt model, BepInEx source/rollback
contract, exact Runtime assembly validation, four public actions and zero-EXE
package boundary remain intact. A proposed `9_` complete-uninstall entry remains
part of the unpromoted V2 workspace cache; this focused correction does not add
a new destructive public action to 0.6.1.

## V2 Exclusion Boundary

- This update owns only existing `tools/release/runtime-workshop/**`, selected
  existing `tools/scripts/**`, and the 0.6.1 focused package matrix.
- Unpromoted V2 source, design, Review, Update and generated artifacts remain
  untracked workspace cache. They are excluded from this commit, package,
  official local upload and live installation.

## Changed Files

- `tools/release/runtime-workshop/invoke-dtmapi-action.cmd`
- `tools/scripts/common.ps1`
- `tools/scripts/probe-powershell-host.ps1`
- `tools/scripts/install-to-game.ps1`
- `tools/scripts/uninstall-dtmapi.ps1`
- `tools/scripts/collect-logs.ps1`
- `tools/scripts/test-runtime-workshop-installer-061.ps1`
- `tools/scripts/test-player-runtime-only-uninstall.ps1`
- `tools/scripts/README.md`
- `docs/architecture/runtime-workshop-installer-boundary.md`
- `docs/workflows/workshop-package-subscription-test-matrix.md`
- `docs/reviews/code/2026/20260807-0002-runtime-installer-three-commit-stress-audit.md`
- this Update and linked Debug navigation/issue records

The cached V2 files are intentionally absent from this changed-file set.

## Validation

- Windows PowerShell 5.1 AST parse of all changed/player scripts: passed.
- Disposable Runtime-only package build with
  `build-release-workshop-packages.ps1 -SkipBuild -RuntimeOnly`: passed.
- `test-runtime-workshop-installer-061.ps1`: passed under forced Windows
  PowerShell 5.1 from the exact
  `Program Files (x86);中文/.../workshop/content/2285550/3743016467` shape and a
  game path containing parentheses, `&`, semicolon and non-ASCII text. This
  covered silent-zero `cmd.exe` rejection, held-lock rejection, BepInEx
  rollback/repair, install/check, rejection of a locked selected log without a
  published partial directory, two unique full newest-ten log bundles,
  pending-transaction uninstall refusal, normal uninstall and repeated no-op.
- Follow-up real-dispatcher pressure reproduced identical `%RANDOM%` sequences
  across concurrent CMD processes. The final dispatcher therefore claims a
  unique probe-session directory before launching PowerShell, and the focused
  matrix starts 24 mixed status/collect/install/uninstall BAT processes at
  once. Every process must select the real host; only action state or the held
  per-game lock may then determine its exit code.
- `test-runtime-upgrade-transaction.ps1 -HostMatrixChild`: passed 17/17 cases
  under PowerShell 7 and 17/17 under Windows PowerShell 5.1.
- `test-player-runtime-only-uninstall.ps1`: passed. Its stale hard-coded
  `0.6.0` source assertion now derives the current release authority (`0.6.1`).
- `test-installer-invalid-target-failure.ps1`: passed (`2/2/2`).
- Clean committed package:
  `dist/workshop-packages-061-final/DTMAPI`, version `0.6.1`, embedded
  `BuildCommit=db5e518a6d7f`, 27 package files before the local-only
  `workshop.json` identity file.
- Independent subscription-package audit: `Blockers: 0`; summary at
  `tmp/test-runs/workshop-audit-061-final/DTMAPI Workshop Audit
  20260808-075907/Results/stress-summary.md`.
- Document governance passed 6,406 checks in a clean detached snapshot of
  commit `db5e518a`; cached untracked V2 files were not part of that snapshot.
- Under the shared Runtime lock, the exact audited package replaced the local
  official upload at
  `C:/Users/Administrator/AppData/LocalLow/RedSawGames/DolocTown/MODS/DTMAPI`.
  Existing `workshop.json` bytes and Workshop ID `3743016467` were preserved;
  every other file was byte/hash-equal to the final package.
- The synchronized local upload itself then passed the independent subscription
  audit with `Blockers: 0`; summary at
  `tmp/test-runs/workshop-audit-061-upload/DTMAPI Workshop Audit
  20260808-080529/Results/stress-summary.md`. No upload exchange transient
  remained afterward.
- The local upload's public `1_install_dtmapi.bat` and
  `3_check_dtmapi_status.bat` both passed through Windows PowerShell
  `5.1.26100.8875`. The real game directory
  `D:/steam/steamapps/common/Doloc Town` reports installed Runtime `0.6.1`,
  binary `0.6.1.0`, matching provenance `db5e518a6d7f`, and the exact five
  production DLLs. Final package, upload payload and installed DLL SHA-256
  values match for all five assemblies.
- Deployment evidence is retained at
  `tmp/test-runs/runtime-installer-061-deployment-20260808-080418-2a0ff434`;
  it includes install/status output, `deployment-summary.json`, and a copy of
  the previous 0.6.0 local upload. Doloc Town was not launched.

An initial dual-host transaction run exposed that suppressing every pre-lock
failure receipt would break the established package-preflight diagnostic
contract. The final design acquires the per-game lock before package preflight:
lock contention writes no competing state, while a preflight failure after
ownership is acquired still writes the existing diagnostic receipt. Both host
matrices then passed.

## Follow-up

- Steam publication/redownload parity and affected-player acceptance remain
  separate release work.
- The isolated V2 candidate may later replace this boundary, but it must retain
  these regression cases or explicitly supersede them in its own authority.

## Rollback

Revert only the files listed by this Update and rebuild the prior package. For
the local machine, restore the retained `previous-upload` evidence under the
shared Runtime lock, preserve its `workshop.json`, and run that package's
installer to converge the live Runtime. Do not delete or change cached V2 work,
a Steam subscription, BepInEx as a whole, official/content Mods, or saves.
