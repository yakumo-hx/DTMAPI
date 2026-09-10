# Runtime Workshop 0.6.1 receipt reliability and player-message update

- Update ID: `20260820-0001`
- Date: `2026-08-20`
- Lifecycle Status: `verified`
- Validation Level: `docs, source, unit, player`
- Runtime Validation: `not-required`
- Related Issue State: `mitigated`
- Area: `release/workshop/installer/0.6.1/transaction/retry/path-resolution/messages`
- Source Request: optimize only the current 0.6.1 PowerShell installer, preserving versions, supported Mods, four public actions, and uninstall ownership
- Source Review: [20260820-0001](../../reviews/code/2026/20260820-0001-runtime-installer-receiptless-residue-root-cause.md)
- Debug Issue: [ISSUE-025](../../../debug/issues/ISSUE-025-20260820-runtime-installer-receiptless-residue.md)

## Scope

This Update owns a bounded correction to the current PowerShell-based 0.6.1
installer. It will:

1. establish and validate the first Runtime transaction receipt before any
   candidate/state mutation, with bounded transient retry;
2. give install, uninstall, and status one shared read-only transaction
   classifier and safe sterile-shell cleanup rule;
3. remove registry-wide and `libraryfolders.vdf` Steam-library discovery;
4. add stable Chinese-first/English-second message codes and final summaries;
5. extend the sequential Windows PowerShell 5.1 and PowerShell 7 fault/retry
   matrix.

It does not enable or edit the isolated V2 candidate, rebuild Runtime or Mods,
change `0.6.1`, `0.6.1.0`, `0.5.3.0`, change supported game/Mod policy, alter
the four BAT actions, edit log collection behavior, launch Doloc Town, touch a
save, or have Codex upload Steam. After the candidate passed package validation,
the user separately authorized loading only this installer delta into the local Steam
subscription for manual test. After manual acceptance, the user authorized
staging the same accepted delta in the local official upload directory and
restoring the subscription directory to its pre-test bytes. The user then
performed the upload; this Update records the resulting Steam subscription
observation and exact post-publish audit.

## Changed files

- `tools/scripts/common.ps1`
- `tools/scripts/install-to-game.ps1`
- `tools/scripts/install-bepinex.ps1`
- `tools/scripts/uninstall-dtmapi.ps1`
- `tools/scripts/check-dtmapi-status.ps1`
- `tools/release/runtime-workshop/invoke-dtmapi-action.cmd`
- `tools/scripts/test-runtime-upgrade-transaction.ps1`
- `tools/scripts/test-runtime-workshop-installer-061.ps1`
- `tools/scripts/test-installer-invalid-target-failure.ps1`
- `docs/architecture/runtime-workshop-installer-boundary.md`
- `docs/architecture/managed-product-admission-registry.md` (generated Catalog
  status-date projection only)
- `docs/workflows/workshop-package-subscription-test-matrix.md`
- `tools/release/current-subscription-manifest.json`
- `tools/release/dtmapi-product-catalog.json` (current Runtime publication
  observation and consumed-upload note only)
- `tools/scripts/check-product-catalog.ps1` (matching current Runtime publication
  assertions only)
- this Update, its root-cause Review, ISSUE-025, and their existing ledgers

The existing dirty DebugConsole, Runtime source, unrelated Catalog product rows,
API, smoke, and unrelated documentation changes are outside this Update and
remain untouched.

## Validation

- Windows PowerShell 5.1 parser checks for every packaged player script;
- the full transaction child matrix independently under Windows PowerShell
  5.1 and PowerShell 7, including receipt genesis write/publish retry,
  exhaustion, cleanup, restart, valid recovery, unsafe residue, uninstall, and
  read-only status;
- the exact 0.6.1 Workshop package matrix from special-character player-like
  paths;
- source assertions proving global Steam registry/`libraryfolders.vdf`
  discovery is absent;
- frozen-package parity for five Runtime DLLs, Compatibility Host, release and
  binary versions, assembly compatibility, and supported-version text;
- Runtime-only uninstall preservation for BepInEx, External plugins,
  configuration, logs, reports, official/content packages, and enablement.

## Implementation

- The first schema-1 receipt now receives six total write/publish attempts with
  delays of `200/400/800/1200/1600 ms`, a fresh temp/backup name on every
  attempt, read-back through the shared schema/path validator, and
  best-effort scratch cleanup that cannot mask the primary exception.
- Candidate/recovery and state-transaction directories are created only after
  that first receipt validates. Exhaustion emits `DTM-E1301`; the failure path
  reclassifies the root and removes it only when it is still a sterile shell.
- `common.ps1` owns the read-only `Clean`, `RecoverableReceipt`,
  `SterileNoReceipt`, unsafe/invalid receipt, and `OrphanState`
  classifications. Install, uninstall, and status consume the same result but
  preserve their distinct mutation authority.
- Game discovery is bounded to the explicit environment setting, local
  setting, package-colocated game, or the current Workshop package library's
  one appmanifest. Registry roots and global Steam library enumeration are no
  longer read.
- Player PowerShell output now uses stable Chinese-first/English-second
  `DTM-E*`, `DTM-W*`, and `DTM-S*` messages. Local file/security-software
  interference is distinct from the network-only fallback, and each action
  ends in one authoritative summary. The root CMD remains ASCII-safe for
  legacy parser/code-page compatibility and points to the bilingual action
  summary instead of inventing a conflicting result.

## Evidence

- `tools/scripts/test-runtime-upgrade-transaction.ps1`: passed the complete
  sequential matrix under Windows PowerShell `5.1.26100.9168` and PowerShell
  `7.6.3`, `28` cases on each host. This includes write fail twice/third
  success, publish fail twice/third success, six-attempt exhaustion, cleanup
  failure, clean next retry, every sterile/unsafe/invalid/orphan fixture,
  valid old recovery, uninstall refusal, read-only status, all earlier commit
  faults, rollback-failure retry, and successful upgrade.
  Exhausted first-receipt creation produced neither transaction residue nor an
  ordinary `install-state.failed-*` file before the clean retry.
- `tools/scripts/test-runtime-workshop-installer-061.ps1 -PackageRoot
  tmp/test-runs/runtime-installer-061-candidate-20260820-015928-065`: passed
  the full 0.6.1 package matrix from temporary Workshop/game paths containing
  parentheses, Chinese, spaces, `&`, and `;`, including invalid explicit
  paths and message/exit-code assertions.
- `tools/scripts/test-installer-invalid-target-failure.ps1`: passed
  independently under both supported hosts (`2` invalid installer targets,
  `2` option conflicts, and `2` invalid status targets per host).
- `tools/scripts/test-player-runtime-only-uninstall.ps1 -Quiet`: passed;
  BepInEx, third-party plugins, configuration, logs, reports, official/content
  packages, and enablement state remained outside Runtime uninstall ownership.
- The `dtmapi-workshop-release-audit` standalone package audit passed with
  `0` blockers. Its retained summary is
  `tmp/test-runs/runtime-installer-061-subscription-audit-final/DTMAPI Workshop Audit 20260820-023223/Results/stress-summary.md`.
- The audited candidate was loaded into the locally resolved subscription at
  `D:\Steam\steamapps\workshop\content\2285550\3743016467` for the user's
  manual test. Pre/post tree hashes proved exactly the seven installer files
  changed; the resulting package matches the candidate byte-for-byte except
  for Steam-owned `workshop.json`. The prior six replaceable files are retained
  under
  `tmp/test-runs/runtime-installer-061-subscription-sync-20260820-074335-650/before`;
  the root dispatcher was newly present in the candidate and therefore had no
  prior subscription byte to back up. Protected Runtime, Compatibility,
  manifest, and version files still match the audited candidate.
- A second `dtmapi-workshop-release-audit` run against the actual post-sync
  subscription passed with `0` blockers. Its retained summary is
  `tmp/test-runs/runtime-installer-061-subscription-post-sync-audit/DTMAPI Workshop Audit 20260820-074444/Results/stress-summary.md`.
- On `2026-08-20`, the user manually exercised the locally loaded subscription
  through a normal install and an install attempt while `DolocTown.exe`
  remained running. The normal success summary and the `DTM-E1001`
  game-process interlock prompt were both reported correct. This is
  user-verified player acceptance, but it does not yet prove post-upload
  subscription parity.
- After that acceptance, the exact seven-file installer delta was staged at
  `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI`.
  Full-tree comparison matches the accepted candidate byte-for-byte while
  retaining only the upload directory's existing `workshop.json`. The six
  replaced upload bytes are retained at
  `tmp/test-runs/runtime-installer-061-upload-stage-20260820-080858-820/upload-before`;
  the root dispatcher was newly added and remains recoverable from the
  accepted candidate.
- A `dtmapi-workshop-release-audit` run against that actual upload directory
  passed with `0` blockers. Its retained summary is
  `tmp/test-runs/runtime-installer-061-upload-directory-audit/DTMAPI Workshop Audit 20260820-080908/Results/stress-summary.md`.
- The local Steam subscription was then restored by reinstating its exact six
  saved installer bytes and removing only the root dispatcher that had been
  absent before the manual-test sync. Hash checks confirmed the seven-item
  revert set and no unrelated subscription change.
- SHA-256 comparison between the frozen package and the one-time candidate
  matched for all five Runtime DLLs, the dormant Compatibility Host,
  `release-manifest.json`, and `dtmapi-runtime-version.props`. The candidate
  therefore retains `0.6.1`, `0.6.1.0`, assembly compatibility `0.5.3.0`, and
  the frozen supported-game fields.
- Source assertions passed: no Registry Steam-root or `libraryfolders.vdf`
  lookup remains, and the current-library `appmanifest_2285550.acf` path does.
- Windows PowerShell 5.1 parser checks passed for all changed packaged `.ps1`
  scripts. `git diff --check` and final documentation/link checks are recorded
  at handoff.
- After the user uploaded the installer-only update, Steam converged the Runtime
  subscription to manifest `918505309011394484` (`NeedsUpdate=0`,
  `NeedsDownload=0`). The Steam-delivered tree contains `29` files and
  `3,866,890` bytes with normalized SHA-256
  `846665a979e17aada210b3960441312a403c72b3c198a0fba91af08972801f88`;
  excluding Steam-owned `workshop.json`, the player payload contains `28` files
  and `3,866,857` bytes with normalized SHA-256
  `b4ec6a441b4930b5174d4caed8799748fb4e4701ac0d8e72fc6bf6bd48eee4aa`.
  All `28` player-payload files match the accepted candidate byte-for-byte, with
  no missing or extra payload files. A post-publish audit of the actual
  subscription passed with `0` blockers; its retained summary is
  `tmp/test-runs/runtime-installer-061-post-publish-subscription-audit/DTMAPI Workshop Audit 20260820-082642/Results/stress-summary.md`.

Codex did not launch Doloc Town; the user's manual interlock test intentionally
kept `DolocTown.exe` running. The frozen package was not overwritten. The local
official upload directory contains the accepted installer-only candidate, and
the Steam-managed subscription now contains the exact published player payload.
The one-time candidate, sync/upload backups, and audit roots are
non-authoritative local evidence only.

## Rollback Notes

Revert only the files listed by this Update. Do not delete ambiguous Runtime
transaction/recovery paths, restore or overwrite unrelated dirty work, alter
the frozen 0.6.1 package, remove BepInEx as a whole, or touch player saves.
The pre-stage local upload bytes are retained under the Evidence path above;
the accepted candidate remains the source for the newly added root dispatcher.

## Follow-Up

Keep ISSUE-025 at `mitigated` until one previously affected player removes the
external file blocker and proves that an ordinary install retry succeeds
without producing another receiptless poison root. That player observation is
not required to retain the local implementation, but it is required to close
the external compatibility issue. The local prompt acceptance, upload-directory
audit, Steam manifest convergence, exact downloaded subscription parity, and
post-publish audit are now recorded; no further lifecycle action is pending for
this installer-only release.
