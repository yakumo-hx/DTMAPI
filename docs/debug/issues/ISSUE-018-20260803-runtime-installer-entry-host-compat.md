# ISSUE-018: Runtime Workshop installer entry and host compatibility

- State: `open`
- Current boundary: Old BAT compound blocks break on `Program Files (x86)` paths; ambient CMD-extension and degraded WinPS capability gaps are separate. Source redesign passes the exact path, while publication/player retest remains open.

## Status

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

## 2026-08-10 App Control / mixed-language-mode player evidence

- A new screenshot from the published `0.6.1` subscription shows Windows PowerShell 5.1 rejecting
  `probe-powershell-host.ps1` with
  `FullyQualifiedErrorId=DotSourceNotSupported` because the command and imported content were defined
  in different language modes. Both displayed attempts are the same Windows PowerShell executable;
  no PowerShell 7 candidate passed or was available.
- The current Steam item is not corrupt: excluding Steam's `workshop.json`, all 27 subscription files
  are length/SHA-256 identical to the frozen `db5e518a6d7f` candidate. The exact subscription bytes also
  passed a fresh FullLanguage Windows PowerShell 5.1 fake-game package matrix with zero blockers at
  `tmp/test-runs/player-installer-language-mode-20260810/DTMAPI Workshop Audit
  20260810-194418/Results/stress-summary.md`.
- This is a different host boundary from the original parenthesized-path incident. The failure occurs
  during read-only host selection before an action or Runtime mutation. It is consistent with
  WDAC/App Control, AppLocker or an equivalent security sandbox assigning different trust/language
  modes to the script scopes. Current architecture deliberately requires `FullLanguage` and does not
  promise to bypass application control.
- The package nevertheless has a diagnostic defect: after the visible language-mode rejection it says
  to repair or install PowerShell, even though PowerShell exists and system policy is the relevant
  blocker; it also probes the same resolved Windows PowerShell path twice. Removing only the probe's
  dot-source would not add compatibility because every action imports the same helpers and uses
  FullLanguage-only .NET capabilities.
- Exact screenshot transcription, artifact hashes, ownership split, player guidance and any future
  acceptance gate are owned by manual-QA Review
  `docs/reviews/manual-qa/2026/20260803-0001-runtime-workshop-installer-entry-regressions.md`, Issue 3.
- State remains `open`. A future bounded correction may deduplicate candidates and report application
  control/FullLanguage failure accurately, but true locked-environment support requires a separate
  trusted-delivery/signing decision and must not weaken or bypass the player's security policy.

Follow-up policy evidence from the same personal PC narrows the environment owner without changing the
package classification:

- Windows Security opens as a blank white shell. `CiTool.exe -lp` on Windows `10.0.26200.8875` shows
  both `VerifiedAndReputableDesktop` and `VerifiedAndReputableDesktopEvaluation` present on disk but not
  enforced or authorized, so Smart App Control is not the active enforcement path shown by this capture.
- An unsigned, non-platform policy named `WindowsWorks`, version `10.3.0.4`, is on disk, authorized and
  enforced. Its PolicyId and BasePolicyId are both the reserved single-policy-format ID
  `{A244370E-44C9-4C06-B551-F6016E563076}`. This is the strongest candidate for the observed system App
  Control lockdown and PowerShell `ConstrainedLanguage`, but the friendly name alone does not establish
  who installed it. CodeIntegrity activation/block events or a separately authorized removal/reboot
  comparison are still needed for exact owner proof.
- Windows Security's blank UI remains a separate unresolved system symptom. It can reflect a broken
  `Microsoft.SecHealthUI`/SecurityHealth service chain or an App Control side effect; no current event
  evidence proves either. The support order is collect policy and event evidence, repair/reset the app,
  run DISM then SFC if needed, and only then let the personal-device owner decide whether to remove an
  unrecognized App Control policy through Microsoft's documented process.
- This evidence strengthens the environment-block classification. It does not authorize DTMAPI to
  weaken the policy and does not turn manual file copying into installer acceptance.

## 2026-08-10 Malformed Steam library candidate evidence

- A separate player ran the published installer from
  `E:\steam\steamapps\workshop\content\2285550\3743016467` under `FullLanguage` Windows
  PowerShell. Host, JSON, portable SHA-256 and ZIP probes passed, but install failed before resolving a
  game folder because `Test-Path -LiteralPath $path` reported an illegal-character path. Status repeated
  the same resolver failure.
- The resolver reads every quoted `path` from Steam `steamapps\libraryfolders.vdf` and currently calls
  `Test-Path` without isolating per-candidate argument failures. A malformed or stale auto-discovered
  entry can therefore abort discovery before a later valid library is considered. A normal drive colon
  is not illegal, and no Runtime mutation had begun at this boundary.
- The user moved this player to manual installation and requested a small correction in the next
  installer version. Manual success is a workaround, not packaged-entry acceptance.
- The correction boundary is narrow: catch and warn for malformed **auto-discovered** Steam library
  candidates, then continue enumeration. Explicit `DTMAPI_GAME_DIR` or local configured targets remain
  strict and fail closed. Package acceptance must place one malformed VDF candidate before a valid
  library and exercise at least install and status while retaining the existing path matrix.
- Exact screenshot transcription, rejected hypotheses and player outcome are owned by manual-QA Review
  `docs/reviews/manual-qa/2026/20260803-0001-runtime-workshop-installer-entry-regressions.md`, Issue 4.
  State remains `open`; no source correction was made in this evidence-only update.

## Acceptance Criteria

- Four public BAT files contain no labels or host-discovery copies and explicitly survive both an exact `Program Files (x86)\steam\steamapps\workshop\content\2285550\3743016467` package root and an outer `/e:off` shell.
- One shared dispatcher owns host ordering, probing, action mapping, pause behavior and exit propagation.
- Packaged player scripts contain no direct `Get-FileHash` or `Expand-Archive` command invocation.
- Host selection checks runtime capability, not AST parsing alone.
- The exact parenthesized package root passes install, check, collect and uninstall with Windows PowerShell 5.1 forced; the separate space/non-ASCII and ordinary subscription matrices remain green.
- The issue remains open until corrected bytes are published and the affected player or an exact-equivalent external environment reaches PowerShell and completes the intended action.
