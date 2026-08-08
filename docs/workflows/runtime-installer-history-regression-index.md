# Runtime Installer History And Regression Index

Status: active routing and regression map. This file does not own current
installer behavior and does not turn a historical Update into current design
authority.

## Authorities And Use

- Current published-source design authority:
  [Runtime Workshop Installer Boundary](../architecture/runtime-workshop-installer-boundary.md).
- Current-package acceptance authority:
  [Workshop Package And Subscription Test Matrix](workshop-package-subscription-test-matrix.md).
- Isolated, unpromoted redesign:
  [Runtime Workshop Installer V2 Candidate](../architecture/runtime-workshop-installer-v2-candidate.md).
- V2 implementation lifecycle:
  [Update 20260807-0003](../updates/2026/20260807-0003-runtime-installer-convergent-candidate.md).
- V2 executable matrix:
  [`test-candidate.ps1`](../../tools/release/runtime-workshop-v2-candidate/test-candidate.ps1).

Every compact symptom below links to the record that owns the full facts. When
the current and V2 designs differ, run only the rows applicable to the design
being changed. Do not restore a retired transaction, Doctor or probe feature
merely because its historical test once existed.

## Durable Incident Routes

| Incident | Owned facts | Current disposition |
| --- | --- | --- |
| [ISSUE-012](../debug/issues/ISSUE-012-20260711-player-title-settings-runtime-missing.md) | Static files and an old clean status did not prove that the current game process loaded Doorstop/BepInEx/DTMAPI; the player-visible symptom recovered after reboot. The same investigation also found a separate LF/label/truncated-command defect in a standalone startup helper. | Static status must identify itself as static. Current-session injection remains a runtime/game-start evidence gate, not an installer-file claim. The standalone Procmon helper is not part of V2. |
| [ISSUE-018](../debug/issues/ISSUE-018-20260803-runtime-installer-entry-host-compat.md) and [manual QA](../reviews/manual-qa/2026/20260803-0001-runtime-workshop-installer-entry-regressions.md) | Parentheses expanded inside a CMD compound block broke all BATs before PowerShell; disabled ambient command extensions and missing optional WinPS cmdlets were separate defects. | Exact parenthesized Workshop path, outer `/e:off`, WinPS 5.1, optional-cmdlet absence and literal punctuation paths remain mandatory package tests. |
| [ISSUE-022](../debug/issues/ISSUE-022-20260807-runtime-tools-move-access-denied.md) and [manual QA](../reviews/manual-qa/2026/20260807-0001-runtime-tools-move-access-denied.md) | Two players received access denied while the Runtime transaction published `candidate\tools`; endpoint-security interference is supported but the exact producer is not proven. | V2 has no Player Doctor EXE and does not transaction-move a support-tools directory. This removes the implicated package boundary but does not claim to close the external security issue. |
| [ISSUE-023](../debug/issues/ISSUE-023-20260808-runtime-installer-stress-boundaries.md) and [stress review](../reviews/code/2026/20260807-0002-runtime-installer-three-commit-stress-audit.md) | Exit-code-only host acceptance, missing cross-process mutation ownership, pending-transaction uninstall, output-name collisions and misleading no-op uninstall. | V2 requires nonce proof, locks per game, treats old transactions as inert, gives full uninstall a bounded old-residue cleanup, publishes logs atomically under unique names and reports truthful no-op state. |

## Historical Update Families

| Period | Historical records | What must survive; what is retired |
| --- | --- | --- |
| 2026-06-11 to 06-12 | [installer preview](../updates/2026/20260611-0019-release-hygiene-installer-preview.md), [hardening](../updates/2026/20260612-0001-release-hygiene-hardening-2.md), [WinPS compatibility](../updates/2026/20260612-0014-installer-windows-powershell-compat.md), [status output](../updates/2026/20260612-0015-check-status-ok-missing-output.md) | Preserve Windows PowerShell 5.1 parsing, UTF-8 BOM/CRLF package bytes, visible status classes and safe uninstall boundaries. Old selected-Mod and receipt breadth is not V2 player scope. |
| 2026-06-13 to 06-15 | [title icon asset](../updates/2026/20260613-0001-installer-autofishing-ui-diagnostics.md), [root log collector](../updates/2026/20260615-0002-dtmapi-runtime-description-support-note.md) | Preserve the installed title icon and a public offline support entry. |
| 2026-06-17 | [single-directory collector fix](../updates/2026/20260617-0001-debug-console-y-edge-collect-logs.md), [offline BepInEx](../updates/2026/20260617-0005-runtime-offline-bepinex-package.md), [corrupt ZIP recovery](../updates/2026/20260617-0006-installer-corrupt-zip-and-api-version-message.md) | Preserve array-safe one-log behavior, package-local BepInEx and corrupt/incomplete archive rejection. V2 deliberately removes download/cache recovery: bad bundled bytes fail before mutation and tell the player to resubscribe. |
| 2026-06-18 | [strict/support split](../updates/2026/20260618-0001-installer-helper-validation-split.md), [collector/package matrix](../updates/2026/20260618-0002-runtime-collector-bat-subscription-matrix.md), [host fallback hotfix](../updates/2026/20260618-0003-player-hotfix-powershell-host-fallback.md) | Preserve early invalid-target failure, root-BAT path testing, a checker that can still explain failure, and host fallback. V2 does not install optional support helpers, so their warning-only parser tier disappears. |
| 2026-07-03 to 07-08 | [pwsh diagnostics hotfix](../updates/2026/20260703-0001-pwsh-diagnostics-hotfix-package.md), [preflight probe](../updates/2026/20260703-0002-install-preflight-probe.md), [file validator](../updates/2026/20260703-0003-powershell-file-validator.md), [host diagnostics](../updates/2026/20260703-0004-powershell-host-self-diagnostics.md), [root file probe](../updates/2026/20260703-0005-root-bat-file-host-probe.md), [public probe removal](../updates/2026/20260708-0001-local-upload-remove-probe-entry.md) | Preserve file-based, invocation-bound host proof and visible failure. Long inline/encoded commands and a public probe BAT are retired. |
| 2026-07-11 | [player runtime analysis](../updates/2026/20260711-0011-player-title-settings-log-analysis.md), [startup capture helper](../updates/2026/20260711-0012-player-startup-capture-script.md) | Preserve the distinction between static install and a fresh loaded runtime. The heavyweight capture helper, download and Procmon flow stay outside the ordinary player installer. |
| 2026-07-13 | [Runtime-only uninstall ownership](../updates/2026/20260713-0011-player-runtime-only-uninstall-ownership-p0.md) | Preserve third-party Mod/BepInEx data during normal uninstall. Marker/receipt-driven official-package cleanup is outside V2. |
| 2026-07-15 | [Runtime upgrade transaction](../updates/2026/20260715-0010-runtime-upgrade-transaction.md), [Player Doctor](../updates/2026/20260715-0018-batch3-player-doctor-closure.md) | V2 replaces rollback/receipt recovery with deterministic reinstall convergence under the stated normal-player assumptions. Player Doctor remains a separate support/developer product and is absent from the candidate. |
| 2026-08-03 | [entry redesign](../updates/2026/20260803-0001-runtime-workshop-installer-entry-redesign.md) | Preserve exact `Program Files (x86)`, outer extensions-off, WinPS 5.1 and no direct `Get-FileHash`/`Expand-Archive`. Also preserve the later `tools\.` fix for the native trailing-backslash argument boundary. |
| 2026-08-07 | [no-Doctor hotfix](../updates/2026/20260807-0001-runtime-no-player-doctor-hotfix-package.md), [0.6.1 simplification](../updates/2026/20260807-0002-runtime-installer-061-simplification.md), [top-level review](../reviews/code/2026/20260807-0001-runtime-installer-061-top-level-design-review.md) | Preserve zero EXEs, bundled-only player prerequisites, a thin public entry boundary and unknown BepInEx data protection. V2 follows the isolated copied-package precedent and remains unpromoted. |
| 2026-08-08 | [focused current-installer fix](../updates/2026/20260808-0001-runtime-installer-061-stress-boundary-fixes.md), [isolated V2 Update](../updates/2026/20260807-0003-runtime-installer-convergent-candidate.md) | Keep current 0.6.1 correction and V2 implementation separate. Neither tree is a fallback source for the other. |

## V2 Historical Regression Mapping

The case names below are machine-owned by `test-candidate.ps1`. `Required`
means the case must stay green for any V2 change. `Replaced` means the old
implementation is intentionally gone, but the simpler target invariant is
tested. `External` means fake-directory installer tests cannot prove the fact.

| ID | Historical failure or invariant | Disposition | V2 case/evidence |
| --- | --- | --- | --- |
| `RI-001` | WinPS 5.1 rejected or misread package scripts without compatible encoding. | Required | `package-layout-and-static-boundary`; `powershell-winps51-probe` |
| `RI-002` | Status/check output self-failed or blurred missing/present state. | Required | `common-special-path-chain`; `normal-uninstall-ownership`; explicit static-only success text |
| `RI-003` | Runtime title icon asset was omitted during install. | Required | `common-special-path-chain` asserts `assets\branding\dtmapi-icon.png` |
| `RI-004` | Exactly one evidence/log directory became a scalar and strict `.Count` failed. | Required | `log-all-or-nothing-and-concurrency` succeeds with one selected Runtime log |
| `RI-005` | Root collector timestamp/nested quoting broke on package paths with spaces. | Required | `common-special-path-chain`; `ten-complete-unbounded-logs` |
| `RI-006` | An invalid explicit game target could fall through toward another discovered install. | Required | `invalid-explicit-game-target` |
| `RI-007` | Corrupt/incomplete BepInEx ZIPs caused partial installation. | Replaced | `corrupt-bundled-bepinex-fails-before-copy`; no network/cache branch |
| `RI-008` | Partial BepInEx repair damaged or copied unknown plugin/config trees. | Required | `convergent-repair` with foreign plugin and config sentinels |
| `RI-009` | WinPS/pwsh discovery and fallback varied by PATH or broken first candidate. | Required | `powershell-winps51-probe`; `powershell-pwsh7-probe`; fixed host paths |
| `RI-010` | Nested `-EncodedCommand`/long inline `-Command` failed independently of valid script syntax. | Replaced | `package-layout-and-static-boundary` rejects both CMD protocols; probe uses `-File` |
| `RI-011` | Extra public probes and support helpers expanded the player execution surface. | Replaced | exact five BAT/six PS1/zero EXE package layout; checker is one of the five actions |
| `RI-012` | Normal uninstall inferred ownership from markers/receipts and risked unrelated content. | Required | `normal-uninstall-ownership`; `truthful-never-installed-uninstall` |
| `RI-013` | Durable Runtime transaction/receipt recovery grew into a second package manager. | Replaced | `historical-runtime-transaction-is-inert`; `convergent-repair`; `separate-complete-uninstall` |
| `RI-014` | Static install could be green while the current launch never loaded DTMAPI. | External | checker explicitly says “static file check”; fresh game log/player UI remains a separate runtime acceptance |
| `RI-015` | BAT compound blocks failed on `Program Files (x86)`; ambient extensions-off and the trailing `"...\"` native argument were separate failures. | Required | `common-special-path-chain` runs the real BAT under `/e:off` with parentheses, `&`, semicolon, Chinese and `[...]`; dispatcher passes `tools\.` |
| `RI-016` | Direct `Get-FileHash`/`Expand-Archive` calls regressed the degraded-WinPS compatibility target. | Required | static rejection plus real WinPS 5.1 action probes; .NET SHA/ZIP implementation |
| `RI-017` | A non-PowerShell executable returned zero and was accepted as a working host. | Required | `nonce-bound-false-host-rejection` |
| `RI-018` | Concurrent installers recovered or changed another process's live transaction. | Required | `per-game-cross-process-lock` with four blocked and four contending processes |
| `RI-019` | Runtime-only uninstall left an authoritative recovery tree that a later install restored. | Replaced | `historical-runtime-transaction-is-inert`; `separate-complete-uninstall` removes only production-shaped old roots |
| `RI-020` | Same-second log exports collided or exposed partially copied files. | Required | `log-all-or-nothing-and-concurrency` |
| `RI-021` | Log collection truncated/size-capped files contrary to the current candidate requirement. | Required | `ten-complete-unbounded-logs` copies newest ten; a 6 MiB+137 file matches length and SHA-256 |
| `RI-022` | Empty uninstall claimed files were removed. | Required | `truthful-never-installed-uninstall` |
| `RI-023` | Destructive cleanup could escape the intended game tree or rely on an undefined path-depth rule. | Required | `reparse-and-outside-tree-safety`; `drive-root-without-depth-heuristic`; one-letter top-level game `G` succeeds |
| `RI-024` | A copied-package experiment accidentally changed canonical source/base bytes. | Required | `existing-installer-and-base-immutability` |
| `RI-025` | A missing dispatcher/action script produced an unhelpful black window. | Required | `cmd-only-checker-fallback`; `incomplete-package-guidance` |
| `RI-026` | Endpoint scanners could lock a bundled unsigned support EXE and the tools-tree publish boundary. | Replaced, issue still open | `package-layout-and-static-boundary` asserts zero EXEs; V2 has no tools-directory transaction move |

## Candidate-Only Policy Differences

These are explicit V2 choices and must not silently rewrite the current 0.6.1
authority:

- newest ten DTMAPI logs are copied whole with no size cap;
- BepInEx has one bundled source and no network/cache fallback;
- package completeness is file-presence based; arbitrary player edits and old
  receipt reconstruction are not supported;
- repair is the same convergence operation as install;
- normal uninstall keeps BepInEx and logs;
- `9_` complete uninstall removes DTMAPI, BepInEx, their logs, fixed Doorstop
  files and production-named historical Runtime transaction roots only after
  explicit confirmation;
- no generic directory-depth rule exists: drive root is rejected, while a
  marker-valid top-level game directory is allowed.

## Evidence And Remaining Gates

Latest frozen V2 matrix evidence:
`tmp/test-runs/runtime-installer-v2-20260808-002511-973-6775e73e`.

This evidence is a fake-game package/install stress run under Windows
PowerShell 5.1 and PowerShell 7. It is not Steam publication, an affected-player
acceptance, a real game injection test, a code-signing decision, or permission
to promote V2 over the current installer.
