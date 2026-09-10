# DTMAPI 0.6.1 Runtime Workshop installer simplification

- Update ID: `20260807-0002`
- Date: `2026-08-07`
- Lifecycle Status: `implemented`
- Validation Level: `docs, source, unit, runtime`
- Runtime Validation: `passed`
- Related Issue State: `mitigated`
- Area: `release/workshop/installer/0.6.1/player-boundary/no-exe/powershell-5.1/bepinex/log-bounds`
- Source Request: 在无 EXE 小包已能安装后，解释并重做普通 Runtime 安装器顶层设计，审查控制流、BepInEx 来源、PowerShell 兼容性与冗余，并进行 0.6.1 修复
- Root-cause/code Review: [20260807-0001](../../reviews/code/2026/20260807-0001-runtime-installer-061-top-level-design-review.md)
- Manual QA: [20260807-0001](../../reviews/manual-qa/2026/20260807-0001-runtime-tools-move-access-denied.md)
- Debug issue: [ISSUE-022](../../../debug/issues/ISSUE-022-20260807-runtime-tools-move-access-denied.md)

## Scope

This Update owns the canonical `0.6.1` Runtime Workshop installer correction. The intended boundary is:

- zero project-owned EXEs in the normal Runtime subscription package;
- Player Doctor remains a separate read-only developer/opt-in support tool and is not a Runtime install requirement;
- four thin BAT entries and one pre-mutation PowerShell host probe remain;
- Windows PowerShell 5.1 is the canonical first candidate, with PowerShell 7 fallback before mutation only;
- host-probe arguments remain valid for every legal Windows path character, including semicolon;
- the player-facing BepInEx route exposes the bundled fixed-hash ZIP first and fixed official URL fallback second;
- ordinary log collection is bounded and does not copy crash dumps unless explicitly requested;
- exact five-DLL validation, package receipts, same-volume Runtime transaction and path-bound rollback remain strict.

This Update does not authorize Steam upload or publication. It also does not make the separate Player Doctor release obsolete; it changes only whether the normal Runtime package and game startup require or invoke that helper by default.

## Planned Changed Files

- Runtime version authority and generated assembly versions;
- Runtime Workshop BAT/CMD/PowerShell packaging and player actions;
- BepInEx installer source/retry/backup behavior;
- status, collection, install transaction and optional Doctor startup handling;
- package Catalog and release/package gates;
- focused installer/transaction/package tests;
- installer architecture, subscription test matrix, Debug issue and documentation ledgers.

## Implemented Changes

- Version authority, catalog, public API matrix and install guide now project Runtime `0.6.1` / binary `0.6.1.0` while retaining assembly identity `0.5.3.0` and existing product minimum floors.
- The normal Runtime package contains no project-owned EXE. Player Doctor remains available only as a separately built opt-in diagnostic and is neither installed nor invoked by default.
- Four BATs dispatch through one CMD; CMD probes Windows PowerShell 5.1 first, PowerShell 7 second, and executes one action once. The probe receives `ToolsRoot + Action`, derives its fixed script set internally and receives the trailing-slash directory as `tools\.` to avoid native argument quote absorption.
- The dormant root preflight BAT/script and package-coupled Doctor entry-point test were removed.
- Steam game discovery first uses the current Workshop library, reads appmanifest `installdir`, then falls back to registry/VDF discovery.
- Player BepInEx installation prefers the bundled fixed-hash ZIP and falls back to the fixed official URL. The source-tree ZIP is development-only. Copy/rollback is per owned file and preserves unknown third-party plugins/config instead of backing up the whole BepInEx tree.
- Runtime candidate validation, exact five-DLL receipts, same-volume directory transaction and path-bound rollback remain strict.
- Default collection caps current text logs at 4 MiB, three history logs at 2 MiB each, and excludes crash dumps unless `-IncludeCrashDumps` is explicit.

## Validation Plan

- Windows PowerShell 5.1 AST parse for every packaged `.ps1`;
- exact special-path matrix including spaces, Chinese, parentheses, ampersand and semicolon;
- disposable subscription install/status/bounded-collect/uninstall matrix;
- Runtime transaction fault/recovery matrix without Doctor receipts;
- BepInEx existing/offline/corrupt-cache/fallback/third-party-preservation cases;
- package inspection proving zero EXEs, exact Runtime DLL set and 0.6.1 version receipts;
- focused build/unit/release-contract gates proportional to changed Runtime/package source.

## Current State

Implementation and local package validation are complete. This is an implemented, unpublished candidate rather than affected-player or live-game acceptance. Current evidence:

- all `126` tracked tool scripts parse with the Windows PowerShell 5.1 AST;
- Release source build passes with zero errors; the existing ten nullable warnings remain isolated to DebugConsole sources;
- InstallDoctor, Author SDK and Core unit suites pass;
- the Runtime upgrade transaction matrix passes `17` cases on both PowerShell 7 and Windows PowerShell 5.1;
- product Catalog and the historical Batch 6 Phase 0 contract pass after the 0.6.1 authority update;
- built-package inspection, the special-path/BepInEx/log-bound matrix and the default subscription audit pass;
- no live game was installed or launched, and Steam publication remains unauthorized.

The first built-package matrix run correctly rejected the candidate before mutation: native argument parsing consumed `-Action` because the quoted `%~dp0` `ToolsRoot` ended in a backslash. The dispatcher now passes the same directory as `tools\.` so the quoted argument has no terminal backslash; the rebuilt package then passed the complete rerun.

## Final Candidate And Validation

- Source commit embedded in the package: `eed21df7eaec`.
- Package directory: `dist/workshop-packages-0.6.1/DTMAPI`.
- Handoff ZIP: `dist/DTMAPI-0.6.1-runtime-workshop-candidate-eed21df7.zip`.
- ZIP bytes: `1,676,415`.
- ZIP SHA-256: `E359B9B41BC8CA6F1CE2FB0A594EAD361E8AC7EBC39BC39C087AB64BA4FEFD3C`.
- Package structure: `27` files, `3,808,646` uncompressed bytes, four root BATs, nine PowerShell scripts, five Runtime assemblies and zero EXEs. `info.json` is `9,192` bytes with SHA-256 `5D7E4538D34621EFFFA91B190535E2E274E237C97070AEAF2F46EA2AB17F1171`.
- Windows PowerShell AST: all `126` tracked tool scripts pass; all nine packaged scripts also pass an external Windows PowerShell `5.1.26100.8875` parser run.
- Release source build passes; InstallDoctor, Author SDK and Core Unit suites pass. The existing ten DebugConsole nullable warnings remain unrelated.
- Runtime upgrade transaction matrix passes `17` cases on PowerShell 7 and `17` cases on Windows PowerShell 5.1.
- Special-path package matrix passes from `Program Files (x86);中文` Workshop path into `Game (x86) & 中文; path`, including controlled BepInEx rollback, offline install, status, bounded large-log collection and uninstall with third-party sentinels unchanged.
- Workshop audit evidence: `tmp/test-runs/workshop-audit-061-absolute/DTMAPI Workshop Audit 20260807-213545/Results/stress-summary.md`; zero blockers. Expected exit codes were missing install `1`, empty install `1`, empty status `1`, valid install `0`, installed status `0`, collect `0`, uninstall `0`, post-uninstall status `1`.
- Catalog/package gates and Batch 6 Phase 0 authority checks pass.
- No real Doloc Town installation, game launch, Steam subscription replacement or Steam upload was performed. Affected-player acceptance remains required before closing ISSUE-022 or describing the endpoint-security cause as isolated.

## Rollback

Before publication, revert the files owned by this Update and discard generated `0.6.1` package/test artifacts. Do not alter the already delivered standalone `0.6.0` no-EXE hotfix evidence.

## Follow-up

Have an affected player test the corrected normal package with ordinary protections restored. Steam upload, live-game startup evidence and ISSUE-022 closure remain separate follow-ups; do not treat local disposable tests as those approvals.
