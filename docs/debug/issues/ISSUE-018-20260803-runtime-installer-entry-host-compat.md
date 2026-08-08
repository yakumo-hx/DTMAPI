# ISSUE-018: Runtime Workshop installer entry and host compatibility

## Status

- State: `open`
- Opened: `2026-08-03`
- Severity: high
- Area: Runtime Workshop BAT / CMD / PowerShell host / installer compatibility
- Related review: `docs/reviews/manual-qa/2026/20260803-0001-runtime-workshop-installer-entry-regressions.md`
- Owning update: `docs/updates/2026/20260803-0001-runtime-workshop-installer-entry-redesign.md`

## Symptom

The affected player double-clicked any of the four public Runtime BAT files, briefly saw a black window, and never reached PowerShell diagnostics when the subscription lived under `E:\Program Files (x86)\steam\...`. Copying the same package to a path without parentheses resolved the launch. A separate degraded Windows PowerShell 5.1 path can parse every package script yet fail the real install because `Get-FileHash` is unavailable.

## Current Classification

Three compatibility gaps are now separated against the exact Steam `0.5.5` subscription package:

1. **Affected-player root cause:** the old BAT files expand `%DTMAPI_PS_HOST_PROBE%` without quoting inside an `if (...)` compound block. CMD eagerly expands and parses the whole block, so `(x86)` in the subscription path closes the block early. All four entries fail before PowerShell even with `/e:on`.
2. **Independent CMD gap:** with outer command extensions disabled, all four BAT files also fail before PowerShell because they rely on `%~dp0`, labels/subroutine calls and `if defined` without enabling extensions. The player explicitly disproved this as their cause by reproducing failure after forcing `/e:on`.
3. **Independent host gap:** with Windows PowerShell `5.1.26100.8875` fixed as the only host in a degraded module environment, install fails at packaged `install-to-game.ps1:1539` because runtime transaction code reintroduced direct `Get-FileHash` calls after the June .NET fallback decision. The affected player's `Get-FileHash` is present and usable, so this is not their cause.

The affected player also confirmed every execution-policy scope is `Undefined`, so policy interception is rejected for this incident.

## Evidence

- Normal exact-subscription baseline: `tmp/test-runs/installer-redesign-baseline/DTMAPI Workshop Audit 20260803-223344/Results/stress-summary.md`; ten WinPS parser passes and zero blockers.
- Parenthesized-path reproduction: exact old subscription bytes copied under temporary `Program Files (x86)\steam\steamapps\workshop\content\2285550\3743016467`; all four BATs exit `255` under `/e:on` with `probe-powershell-host.ps1 was unexpected at this time`. Evidence root: `tmp/test-runs/installer-parentheses-old-c064209572c7494f90d7ea69cb038ff4`.
- CMD degraded reproduction: all four copied root BATs run through `cmd.exe /d /e:off /v:off`, exit `1`, print CMD syntax errors and fail label lookup before PowerShell.
- WinPS runtime reproduction: `Pure Windows PowerShell 5.1 Repro 中文/DTMAPI/install-state.failed-20260803-223612-171-95a526b9.json`; install exits `1` at `Get-DtmApiRuntimeAssemblyMetadata` with missing `Get-FileHash`.
- Historical authority: Update `20260618-0002` explicitly supports Windows PowerShell hosts without `Get-FileHash` for BepInEx verification.

## 2026-08-03/04 Source Correction And Pressure Evidence

- The four public BAT files are now label-free action shims. Each explicitly enables command extensions and launches one packaged `invoke-dtmapi-action.cmd` through `%ComSpec% /d /e:on /v:off`.
- The shared dispatcher owns action mapping, host order/override, capability probing, pause behavior and exact child exit propagation. It never retries a mutating action under a second host.
- Every path emitted from a CMD compound block is quoted; the generated package completed its full chain from an exact temporary `Program Files (x86)\steam\steamapps\workshop\content\2285550\3743016467` copy.
- `common.ps1` now owns .NET-based SHA-256, safe ZIP extraction and fixed-file download. Packaged install/status/collect/preflight scripts no longer invoke `Get-FileHash` or `Expand-Archive` directly.
- Direct capability probe passed on the reproduced degraded Windows PowerShell `5.1.26100.8875`: FullLanguage, JSON, portable SHA-256 and portable ZIP all reported `OK`.
- `test-player-doctor-packaged-entrypoints.ps1` passed the exact parenthesized package root with an outer `cmd /e:off`, a forced Windows PowerShell 5.1 host and a game path containing spaces, Chinese text, parentheses and `&`, across install, status, collect and uninstall.
- The same packaged test rejected a deliberately unusable fixed PowerShell 7 candidate, selected Windows PowerShell 5.1 before running the action, replaced a corrupt cached BepInEx ZIP from its offline fallback, repaired a partial BepInEx core, and proved that a parser-broken optional diagnostic helper warns without blocking install before a clean repair install restores status.
- Runtime transaction fault injection passed all 17 cases under both PowerShell 7 and Windows PowerShell 5.1. Invalid explicit targets, Runtime-only uninstall ownership, unexpected download markers and the portable Player Doctor gates also passed.
- The final generated-package audit passed all ten Windows PowerShell 5.1 parser rows and the full missing/empty/valid/install/check/collect/uninstall matrix with `Blockers: 0`: `tmp/test-runs/installer-redesign-final-skill-audit/DTMAPI Workshop Audit 20260804-001459/Results/stress-summary.md`.

The source acceptance criteria are satisfied. State remains `open` because the Steam subscription still contains the old 0.5.5 bytes and the affected player has not retested corrected published bytes.

## Rejected Or Unproven Hypotheses

- Rejected as a universal package failure: the same exact package passes its normal temp install/status/collect/uninstall matrix.
- Rejected as PowerShell syntax failure for the independent WinPS case: all ten scripts parse under the exact failing host.
- Rejected for the affected player: disabled command extensions, Windows PowerShell version, missing `Get-FileHash`, execution policy and action-script-specific failure.
- Unproven and no longer required to explain the incident: antivirus blocking, permissions, corrupt subscription bytes or a broken `.bat` association.

## Acceptance Criteria

- Four public BAT files contain no labels or host-discovery copies and explicitly survive both an exact `Program Files (x86)\steam\steamapps\workshop\content\2285550\3743016467` package root and an outer `/e:off` shell.
- One shared dispatcher owns host ordering, probing, action mapping, pause behavior and exit propagation.
- Packaged player scripts contain no direct `Get-FileHash` or `Expand-Archive` command invocation.
- Host selection checks runtime capability, not AST parsing alone.
- The exact parenthesized package root passes install, check, collect and uninstall with Windows PowerShell 5.1 forced; the separate space/non-ASCII and ordinary subscription matrices remain green.
- The issue remains open until corrected bytes are published and the affected player or an exact-equivalent external environment reaches PowerShell and completes the intended action.
