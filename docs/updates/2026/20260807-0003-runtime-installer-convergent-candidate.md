# Runtime Workshop convergent installer isolated candidate

- Update ID: `20260807-0003`
- Date: `2026-08-07`
- Lifecycle Status: `implemented`
- Validation Level: `docs, source, runtime`
- Runtime Validation: `passed`
- Related Issue State: `open`
- Area: `release/workshop/installer/v2/isolated-candidate/convergence/full-uninstall/log-integrity`
- Source Request: 保持现有安装器目录与现有 0.6.1 候选不变，沿用历史独立热修包流程重写安装器，再把历史故障转为回归矩阵并压力测试
- Source Review: [20260807-0002](../../reviews/code/2026/20260807-0002-runtime-installer-three-commit-stress-audit.md)
- Candidate Architecture: [Runtime Workshop Installer V2 Candidate](../../architecture/runtime-workshop-installer-v2-candidate.md)
- Historical Regression Index: [Runtime Installer History And Regression Index](../../workflows/runtime-installer-history-regression-index.md)

## Scope

This Update owns an isolated V2 installer overlay, builder, regression index and
disposable player-package pressure matrix. It does not modify or promote the
current canonical Runtime Workshop installer.

The source and artifact boundaries are:

- tracked isolated source: `tools/release/runtime-workshop-v2-candidate`;
- immutable base input: `dist/workshop-packages-0.6.1/DTMAPI`;
- generated candidate: `dist/runtime-installer-v2-candidate/DTMAPI`;
- disposable/evidence roots: `tmp/test-runs/runtime-installer-v2-*`.

## User Constraints

- Do not recommend a fixed path such as `C:\DTMAPI`; say only to copy the whole
  folder to a path containing English letters and numbers.
- Continue using wildcard-safe PowerShell literal-path operations; verify from
  project history and regression tests that this was not the earlier path bug.
- Do not add Steam-library relocation guidance.
- Do not use an undefined path-depth heuristic. Reject only a drive root and
  protect exact allowlisted child targets.
- Keep normal Runtime-only uninstall and add a clearly separate `9_` complete
  uninstall that removes DTMAPI, BepInEx and residual DTMAPI/BepInEx logs after
  explicit confirmation.
- Export the newest ten DTMAPI logs in full, without size limits, and never
  publish an incomplete copied file or a partially built bundle.

## Historical Isolated-Package Precedent

- `20260618-0003`: `dist/player-hotfix-20260618-powershell-host`;
- `20260703-0001`: `dist/player-hotfix-20260703-pwsh-diagnostics`;
- `20260703-0005`: `dist/player-hotfix-20260703-root-file-probe`;
- `20260807-0001`: `dist/player-hotfix-20260807-runtime-only-no-doctor`.

The last precedent explicitly changed copied package scripts while leaving the
canonical source and normal Workshop package unchanged. V2 follows that model
and adds a tracked overlay so the copied implementation is reviewable.

## Implementation

- Added a tracked isolated overlay and atomic copied-package builder. The
  builder removes only the old installer files in its staging copy, applies the
  V2 overlay, verifies Runtime payload and bundled BepInEx tree hashes, and
  publishes only below `dist/runtime-installer-v2-candidate`.
- Added five public BAT actions: install, normal uninstall, lightweight static
  check, full log collection and separately confirmed `9_` complete uninstall.
- Reduced the player chain to BAT, one CMD dispatcher, one nonce-bound host
  probe and one PowerShell action. Windows PowerShell 5.1 is tried first and
  PowerShell 7 is a fixed-path fallback.
- Replaced receipt recovery with deterministic normal-state convergence. An
  old production-shaped Runtime transaction is inert during normal actions and
  removable only through complete uninstall.
- Kept normal uninstall scoped to DTMAPI Runtime while complete uninstall owns
  the full DTMAPI/BepInEx/Doorstop boundary requested by the user.
- Added the historical incident/update router and mapped its live invariants to
  named executable cases without treating retired features as current design.

## Changed Files

- `docs/architecture/runtime-workshop-installer-v2-candidate.md`
- `docs/workflows/runtime-installer-history-regression-index.md`
- `docs/workflows/workshop-package-subscription-test-matrix.md`
- `docs/updates/2026/20260807-0003-runtime-installer-convergent-candidate.md`
- `docs/updates/INDEX-2026-08.md`
- `tools/release/runtime-workshop-v2-candidate/**`

## Validation

- Windows PowerShell `5.1.26100.8875` parsed all eight tracked V2 PowerShell
  files. The built candidate contains `25` files, five BATs, six PowerShell
  files and zero EXEs.
- The frozen full matrix passed `20/20` with zero failures under both Windows
  PowerShell 5.1 and PowerShell 7 host probes. Evidence:
  `tmp/test-runs/runtime-installer-v2-20260808-002511-973-6775e73e`.
- The real BAT chain passed from a package root containing spaces,
  `Program Files (x86)`, Chinese, ampersand, semicolon and square brackets under
  outer `cmd /e:off`; the marker-valid top-level game directory `G` also passed.
- Installation, static check, ten full logs, normal uninstall, convergence
  reinstall, partial-BepInEx repair and complete uninstall passed. Foreign
  plugin/configuration sentinels and unrelated game-root bytes were preserved
  at their applicable boundaries.
- Missing payload, corrupt bundled ZIP and invalid explicit game target all
  failed before Runtime/BepInEx mutation and produced their stable guidance.
- A false `cmd.exe` host could not forge nonce proof. Four externally locked
  installs failed before mutation; four release-time contending installs
  converged to one healthy state.
- A locked selected log published no directory or staging residue. Two parallel
  collectors then published two unique complete directories. The newest ten
  policy copied a `6 MiB + 137` byte log with matching length and SHA-256.
- Complete uninstall refused a BepInEx junction, rejected drive-root use and
  removed a production-shaped historical transaction only as a verified direct
  game child.
- Before/after byte manifests proved that
  `tools/release/runtime-workshop` and
  `dist/workshop-packages-0.6.1/DTMAPI` did not change during the V2 matrix.
- No real Doloc Town directory, local official upload folder, Steam
  subscription or running game was touched. Runtime validation here means the
  disposable player-package process matrix, not a game smoke.

Two retained diagnostic runs preceded the frozen pass:

- `runtime-installer-v2-20260808-001213-842-ae175e00` exposed a nondeterministic
  contention-fixture assumption;
- `runtime-installer-v2-20260808-001326-347-ebaf84f2` exposed that concurrent
  CMD processes can generate identical `%RANDOM%` sequences and collide on the
  same probe result path. The dispatcher now claims a unique probe directory
  atomically before accepting a nonce result.

- Document governance passed `6,428` checks. All links in the new candidate
  architecture, Update and history/regression index resolved; candidate source
  had no trailing whitespace; tracked documentation diff hygiene passed.

## Rollback

Delete only the new tracked V2 candidate folder, its generated ignored candidate
and its managed test evidence. Do not edit or replace the current installer,
current 0.6.1 package, local upload tree or Steam subscription as rollback.

## Follow-up

- Keep Lifecycle at `implemented`: V2 is not the current authority and has no
  affected-player, Steam publication or real-game injection acceptance.
- Promotion requires a separate authorization and independent review of the
  complete-uninstall ownership boundary.
- ISSUE-022 remains externally open; removing the EXE/tools move eliminates the
  implicated design surface but does not prove which security component caused
  the player access denial.
