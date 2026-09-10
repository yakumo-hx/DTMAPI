# Runtime Workshop 安装器入口与主机兼容层重做

- Update ID: `20260803-0001`
- Date: `2026-08-03`
- Lifecycle Status: `implemented`
- Validation Level: `docs, source, unit, runtime`
- Runtime Validation: `passed`
- Related Issue State: `open`
- Area: `release/workshop/installer/bat/cmd/powershell-5.1/capabilities/package-matrix`
- Source Request: 梳理四个脚本的历史变动与 bug 原因，重新做顶层设计，并在保留 BAT + PowerShell 的前提下部分重做安装器
- Root-cause Review: [20260803-0001](../../reviews/manual-qa/2026/20260803-0001-runtime-workshop-installer-entry-regressions.md)
- Architecture: [Runtime Workshop Installer Boundary](../../../architecture/runtime-workshop-installer-boundary.md)
- Debug issue: [ISSUE-018](../../../debug/issues/ISSUE-018-20260803-runtime-installer-entry-host-compat.md)

## Scope

This Update owns the post-0.5.5 source correction for:

- four label-free public BAT shims;
- one clean-CMD shared action dispatcher;
- safe CMD compound-block behavior for parenthesized Steam library paths;
- capability-aware PowerShell host probing;
- shared .NET SHA-256, ZIP extraction and download primitives;
- removal of direct player-script `Get-FileHash`/`Expand-Archive` dependencies;
- package and focused test coverage for exact `Program Files (x86)` subscription paths, disabled CMD extensions and forced Windows PowerShell 5.1.

It preserves the existing Runtime install transaction, rollback, install-state/release receipts, Runtime-only player uninstall, Player Doctor and support-collector semantics. It does not touch the real game, local official `MODS`, Steam subscription bytes, Workshop upload state or player saves.

## Pre-change Evidence

- Exact Steam package normal matrix: ten Windows PowerShell parser passes, valid temp install/check/collect/uninstall, zero blockers.
- Affected-player environment correction: package root `E:\Program Files (x86)\steam\steamapps\workshop\content\2285550\3743016467`; Windows PowerShell `5.1.26100.8875`, working `Get-FileHash`, all execution policies `Undefined`, and failure retained under `/e:on`; copying to a parenthesis-free path resolves it.
- Exact old-package reproduction under a temporary `Program Files (x86)\steam\steamapps\workshop\content\2285550\3743016467`: all four BATs exit `255` under `/e:on` because the unquoted probe path expanded inside an `if (...)` block is parsed as CMD grammar.
- Exact Steam package with outer CMD `/e:off`: all four BAT files fail before PowerShell with syntax/label errors.
- Exact Steam package under fixed Windows PowerShell `5.1.26100.8875`: real install exits `1` at packaged `install-to-game.ps1:1539` because `Get-FileHash` is unavailable.
- Full evidence root: `tmp/test-runs/installer-redesign-baseline/DTMAPI Workshop Audit 20260803-223344`.

The `/e:off` and missing-`Get-FileHash` reproductions are independent defects found during review; the confirmed affected-player cause is the parenthesized path expansion.

## Changed Files

- `tools/release/runtime-workshop/0_probe_dtmapi_install.bat`
- `tools/release/runtime-workshop/1_install_dtmapi.bat`
- `tools/release/runtime-workshop/2_uninstall_dtmapi.bat`
- `tools/release/runtime-workshop/3_check_dtmapi_status.bat`
- `tools/release/runtime-workshop/4_collect_dtmapi_logs.bat`
- `tools/release/runtime-workshop/invoke-dtmapi-action.cmd`
- `tools/scripts/common.ps1`
- `tools/scripts/install-to-game.ps1`
- `tools/scripts/install-bepinex.ps1`
- `tools/scripts/check-dtmapi-status.ps1`
- `tools/scripts/collect-logs.ps1`
- `tools/scripts/probe-install-preflight.ps1`
- `tools/scripts/probe-powershell-host.ps1`
- `tools/scripts/build-release-workshop-packages.ps1`
- `tools/scripts/test-player-doctor-packaged-entrypoints.ps1`
- `tools/scripts/README.md`
- `PROJECT.md`
- `AGENTS.md`
- `README.md`
- `docs/architecture/README.md`
- `docs/architecture/batch6-managed-mod-identity-contract.md`
- `docs/architecture/runtime-workshop-installer-boundary.md`
- `docs/debug/issues/README.md`
- `docs/debug/issues/ISSUE-018-20260803-runtime-installer-entry-host-compat.md`
- `docs/onboarding/current-state.md`
- `docs/reviews/manual-qa/2026/20260803-0001-runtime-workshop-installer-entry-regressions.md`
- `docs/workflows/installer-workshop-phase-summary-20260706.md`
- `docs/workflows/workshop-package-subscription-test-matrix.md`
- this Update and `docs/updates/INDEX-2026-08.md`

## Implementation

- Public BAT files now contain only extension enablement, safely quoted dispatcher-path validation, clean CMD re-entry and exit propagation.
- `invoke-dtmapi-action.cmd` is the single host/action owner and is emitted as ASCII CRLF into the package.
- Host selection now proves FullLanguage, JSON, shared SHA-256 and (for install/probe) ZIP capability after parsing required scripts.
- Shared .NET helpers replace direct player-path hash/archive cmdlets; BepInEx fallback download also uses the shared fixed-file downloader.
- The packaged-entrypoint regression copies the candidate under an exact temporary `Program Files (x86)\steam\steamapps\workshop\content\2285550\3743016467`, fixes the outer shell at `/e:off`, forces Windows PowerShell 5.1, rejects direct optional cmdlets by AST and exercises the full public action chain.

## Validation

- Top-level authority scan and governance repair passed: `PROJECT.md`, `AGENTS.md`, `README.md` and onboarding now route current installer decisions to the canonical architecture/matrix/Review/Issue/Update; the stale statement that the already-published `0.5.5` release remained blocked was removed. Dated Planning/Update records were preserved as historical audit material.
- PowerShell 7 and Windows PowerShell 5.1 syntax validation passed for every packaged installer script. Direct degraded Windows PowerShell `5.1.26100.8875` capability probing passed FullLanguage, JSON, portable SHA-256 and portable ZIP checks; packaged player scripts are also AST-gated against direct `Get-FileHash`, `Expand-Archive` and `Invoke-WebRequest` use.
- `build-release-workshop-packages.ps1 -SkipBuild -RuntimeOnly` passed and emitted the frozen source candidate under `tmp/test-runs/installer-redesign-candidate-final/DTMAPI`; Player Doctor release and product-catalog gates also passed.
- `test-player-doctor-packaged-entrypoints.ps1 -PackageRoot <candidate>` passed from an exact temporary `Program Files (x86)\steam\steamapps\workshop\content\2285550\3743016467` copy with outer `cmd /e:off`, a game path containing spaces, Chinese text, parentheses and `&`, and Windows PowerShell 5.1 selected. It rejected an unusable first PowerShell 7 candidate and fell back to 5.1 before action execution; replaced a corrupt cached BepInEx ZIP from the offline fallback; repaired a partial BepInEx core; allowed an intentionally invalid optional diagnostic script to warn without blocking install; repaired that helper on the next install; then passed clean status, semantic-invalid Doctor status, collect, Runtime-only uninstall and post-uninstall status while preserving locked external fixtures.
- `test-installer-invalid-target-failure.ps1` passed (`installer-target=2`, option conflict `=2`, status target `=2`), proving an invalid explicit path does not fall through to a real game.
- `test-runtime-upgrade-transaction.ps1` passed all 17 fault cases under both PowerShell 7 and Windows PowerShell 5.1. `test-player-runtime-only-uninstall.ps1` passed all ownership fixtures; `test-workshop-download-markers.ps1` passed expected Zone removal/unexpected ADS fail-before-mutation; `test-player-doctor-portable.ps1` passed its self-contained read-only gate.
- The final Workshop subscription audit against the rebuilt candidate passed all ten Windows PowerShell 5.1 parser rows and the missing/empty/valid/install/check/collect/uninstall matrix with `Blockers: 0`. Evidence: `tmp/test-runs/installer-redesign-final-skill-audit/DTMAPI Workshop Audit 20260804-001459/Results/stress-summary.md`.
- No real game directory, local official upload tree, Steam subscription tree or save was modified.

## Not Run

- No live Doloc Town launch/game smoke: this correction is confined to temporary installer/package boundaries and did not change game-loaded assemblies.
- No complete Release suite: focused package, transaction and compatibility gates cover the changed boundary; publication acceptance remains separate.
- No local-upload sync, Steam upload/resubscribe parity or affected-player retest.

## Rollback

Revert this Update's source/package/docs changes together and rebuild a Runtime candidate. No external Runtime or Workshop rollback is required because implementation and validation stay in temporary game/package roots.

## Follow-up

The implemented source correction cannot close ISSUE-018 by itself. Publication requires a separately frozen Runtime candidate, exact upload/subscription parity and affected-player or exact-equivalent external acceptance.
