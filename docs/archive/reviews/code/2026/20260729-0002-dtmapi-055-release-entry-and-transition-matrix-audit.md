# 20260729-0002: DTMAPI 0.5.5 Release Entry And Transition Matrix Audit

Status: `open / complete Release entry rejected`

## Scope

The user asked for an independent review after the latest Author SDK recovery
correction, whether the project may start the one complete Release suite, what
old/new Runtime and AutoFishing transition states can be tested, whether a
failure would invalidate staged publication, whether the 0.5.5 player package
contains only the four intended root scripts, and what still needs manual
testing.

The reviewed source state is:

- branch `codex/major-update-batch0-20260713`;
- HEAD `e43ad55d`;
- clean tracked worktree before this Review;
- Runtime candidate BuildCommit `c7e1ec2f3697`;
- frozen Author SDK ZIP: 389 files, 126,959,919 bytes, SHA-256
  `1131840b940d9b366fbc24391449cf739078730655b7409b24720b1579c5f1c7`;
- frozen Runtime Workshop directory:
  `dist/prerelease-step5-candidate/DTMAPI`.

This audit did not run the complete Release suite, launch Doloc Town, mutate
Steam, install a candidate, or change a subscription or OfficialLocal tree.

## Result

`P0=0 / P1=2 / P2=0`.

The corrected game-root pending-local-install barrier and exact source-state
recovery preflight are accepted. The project is nevertheless not yet
`READY-FOR-FINAL-RELEASE`: one installer reconciliation path can accept a
pending recovery as a commit, and the frozen player package contradicts the
previously selected four-script upload layout.

Both corrections are bounded. Neither changes a game-loaded Runtime DLL or a
product DLL, so they do not invalidate Manbo, external-consumer, no-demand,
AutoFishing or ActionSpeed game/GC evidence. They do change the player package
tree and therefore require a new exact player-package freeze.

## P1-1: Corrupt Install Output Can Turn `RecoveryRequired` Into A False Commit

The new Author SDK behavior is internally coherent:

- `install-local-status` is read-only;
- a prepared `localInstall` returns process exit `0`, `success=true` and
  `values.status=RecoveryRequired`;
- an exact committed destination/journal/source selection returns
  `values.status=CommittedLocalDevelopment`.

The outer installer has not adopted that three-state meaning. When the
mutating `install-local` output is lost or cannot be parsed,
`tools/scripts/install-to-game.ps1` invokes `install-local-status`, but its
acceptance condition checks only the status process exit and top-level
`success`. It does not require `values.status=CommittedLocalDevelopment`.
It then overwrites the report with `operation=reconciled`, labels the
reconciliation committed and proceeds to register the product as installed.

The frozen SDK reproduced the rejected state:

```text
install-local with crash:install-local.after-prepare
  -> exit 1 / rollback=recovery-required

install-local-status
  -> exit 0 / success=true / status=RecoveryRequired
```

This means a lost/corrupt outer JSON response combined with a process-style
interruption can leave a durable recovery marker while the installer reports
success. No mutation is replayed, which is good, but the terminal state is
still misclassified.

The existing transaction matrix covers process-style interruption and corrupt
output after a committed install separately. It does not cover their
combination.

Required correction:

1. read-only reconciliation must require the exact
   `CommittedLocalDevelopment` status, not merely `success=true`;
2. it should also require the expected nonempty deployment and source-tree
   digests already returned by the committed status;
3. `RecoveryRequired` must fail closed with explicit recovery guidance and
   must not be relabeled or registered as installed;
4. add a focused corrupt-report plus
   `crash:install-local.after-prepare` case, and preferably the terminal
   source-selection interruption case; require the marker to remain
   retryable and explicit recovery to restore the exact state.

The Author SDK ZIP need not change if its current status contract is retained.
The packaged installer script does change, so the Runtime player-package tree
must be rebuilt and re-frozen even though the game-loaded Runtime DLL freeze
remains valid.

## P1-2: The Frozen Player Package Reintroduced The Removed Root Probe

The durable 2026-07-08 product decision says that the official upload package
exposes only:

```text
1_install_dtmapi.bat
2_uninstall_dtmapi.bat
3_check_dtmapi_status.bat
4_collect_dtmapi_logs.bat
```

`0_probe_dtmapi_install.bat` remains available only through the separate probe
package. The current subscribed 0.5.2 directory follows that layout.

The current generator instead copies `0_probe` plus `1` through `4`, and the
frozen 0.5.5 candidate therefore contains five root BAT files. Its current
identity is 31 files and 71,533,167 bytes. The existing OfficialLocal upload
directory also contains `0_probe`, but it is a stale 0.5.5 package with
BuildCommit `c93c460e5b7a` and is not an upload input. The subscribed package is
still `0.5.2-alpha`, BuildCommit `8caf8403b45c`, and is a compatibility input,
not the new candidate.

Required correction:

1. make the Runtime package generator copy exactly root BAT `1` through `4`;
2. add one small assertion to the existing packaged-entrypoint/layout test:
   exact root BAT set `1..4`, with root `0_probe` absent;
3. keep `Content/DTMAPIInstaller/tools/probe-install-preflight.ps1` dormant in
   the package for reuse by the separate probe ZIP; none of the four player
   entrypoints calls it and Runtime does not load it;
4. rebuild and re-freeze the player directory. With only root `0_probe`
   removed from the current bytes, the expected preliminary shape is 30 files
   and 71,530,288 bytes, but the new freeze must calculate rather than copy
   that expectation.

No new gate, receipt family or game run is justified for this correction.

## Accepted Author SDK Correction

Code review and focused checks accept the correction introduced through
`c32a274f`:

- every source-state mutation and every other `install-local` performs the
  game-root pending-marker preflight under the existing operation lock;
- all direct deployment journals are parsed and semantically validated before
  source mutation or SDK-owned orphan cleanup;
- source/deployment/local-install status operations remain read-only;
- local recovery accepts exactly one validated matching pending transaction;
- recovery changes deployment state only after the current source state is
  proven equal to either the exact pre-transaction state or the exact derived
  committed state;
- unknown external source-state drift is preserved and recovery stays
  retryable;
- pending staging/input artifacts remain protected.

Focused evidence passed:

- `DTMAPI.AuthorSdk.Tests` Release;
- frozen Author SDK PowerShell 7 release check;
- Windows PowerShell 5.1 structure/contract check;
- frozen ZIP `dtmapi-author.dll` equals the current win-x64 Release build.

The large dual-host transaction matrix was attempted but exceeded this audit's
bounded execution window and is not counted as fresh evidence. Its previously
recorded pass remains historical evidence for its exact earlier input.

## Complete Release Decision

Do not start the acceptance complete Release suite on HEAD `e43ad55d`.

Running it now would:

- build the wrong five-root-script player package again; and
- lack the exact combined interruption/corrupt-report assertion that exposes
  P1-1, so a green suite would not refute that finding.

After the two P1 corrections:

1. run only Author SDK/installer transaction focus and the existing packaged
   entrypoint/layout check;
2. rebuild and freeze the exact four-script player package and update only its
   package/tree/hash authorities;
3. perform one small independent recheck of the two findings;
4. hash the frozen candidate before and after one from-start complete Release;
5. if it passes, run the player-like Workshop package audit against the exact
   frozen directory, including Windows PowerShell 5.1, a standard PowerShell
   host, spaces/non-ASCII paths, offline installation, status/check,
   uninstall and collect-logs.

The complete Release is a source/build/contract suite. It does not replace the
separate short Unity compatibility runs below.

## Runtime And Product Transition Matrix

| Transition | Current evidence | Correct acceptance |
| --- | --- | --- |
| installed old Runtime `0.5.2` + new AutoFishing `1.0.0` | Exact local artifacts exist; historical stale-Runtime canary proves pre-assembly `api-too-new`, game continuation and no fatal popup. Exact 0.5.2 plus exact new ZIP has not run in Unity. | Run a short exact-byte canary before R1. New AutoFishing must not reach Assembly/Entry; Manager/Errors must visibly say the Runtime is too old and direct the player to `1_install_dtmapi.bat`; title/save entry and exit must remain usable. |
| final Runtime `c7e1ec2f` + retained old AutoFishing | Final retained ABI has zero removals; the old DLL ran on an earlier Runtime candidate. It has not run against final `c7`. | Fifth save: exact old subscription DLL, F6/config, one real Ready/Cast/Pull/fish cycle, disable and return to title. |
| final Runtime `c7e1ec2f` + new AutoFishing `1.0.0` | Final-candidate active-GC r15 already passed new AutoFishing L1/L3/L4/L5. | No upload-before rerun. After R1, bind the downloaded subscription tree/hash and do one short fish/config user-path check. |
| final Runtime `c7e1ec2f` + other old Workshop products | Final-candidate Manbo and three exact external consumers passed. The other retained first-party set has final static ABI plus older combination runs, not one final-candidate Published11 run. | Before R0 upload, one slot-3 `NoNativeSave` final-candidate Published11 short run: eleven exact load sources and Entry once, zero load/provider/fatal failures, title/Loader cleanup, subscriptions and saves unchanged. This proves loading/lifecycle, not every gameplay feature. |

The Workshop DTMAPI item is a downloaded installer package. Steam updating the
subscription does not replace the already installed game-directory Runtime;
the player still has to run `1_install_dtmapi.bat`.

There is no current automatic update modal for old Runtime plus new
AutoFishing. The existing behavior is a visible DTMAPI Manager/error/log
`api-too-new` result and a skipped DLL load. Because the new DLL is rejected
before `Assembly.LoadFrom`, it cannot display its own update dialog. A literal
automatic popup is a separate product/UX decision and must not be claimed by
this release test.

Real Steam download timing, Workshop provenance and the language-page text can
only be accepted after upload. Local exact-byte tests cannot substitute those
facts.

## Staged Versus Simultaneous Publication

Keep the selected staged order:

```text
R0 Runtime 0.5.5
R1 AutoFishing 1.0.0
R2 MoreEquipmentSlots 1.0.0
R3 the remaining eight products
```

Steam updates to separate items are not atomic, and the Runtime requires a
manual installation step. Publishing every product together cannot eliminate
old/new transition combinations; it increases simultaneous variables and
rollback scope.

If one retained old product fails under final 0.5.5, R0 is blocked until the
Runtime restores backward compatibility or that exact product receives a
bounded coordinated correction. That does not imply that all products should
be released together. Only a common Loader/ABI failure across multiple
products would justify reopening the overall wave design.

## Minimum Manual Checks

Before Runtime upload:

1. inspect the old-Runtime/new-AutoFishing Manager and error wording with human
   eyes; do not expect a popup;
2. run final 0.5.5 plus exact Published11 once on slot 3;
3. run final 0.5.5 plus exact old AutoFishing for one real fish cycle on slot
   5;
4. spot-check retained MoreEquipmentSlots because it is the next large
   subscription/save-risk product: open the extra slots, equip an item and
   verify no-save return restores the committed state.

After R0 upload:

1. redownload Workshop item `3743016467`;
2. bind its exact hash/layout to the frozen package;
3. run `1_install`, `3_check`, then the short retained-product startup;
4. confirm the official page's three language texts were manually pasted and
   read back.

After R1 upload:

1. redownload Workshop item `3743799721`;
2. bind the subscription tree/hash to the frozen AutoFishing product;
3. run one fifth-save fish cycle and a config save/reload check.

The remaining products can use their existing focused acceptance plus the
Published11 load/lifecycle run unless an individual update changes its
artifact or known risk boundary.

## Resolution Gate

This Review can close when:

- P1-1 rejects every non-`CommittedLocalDevelopment` reconciliation and the
  combined crash/corrupt-report test passes;
- P1-2 produces and freezes an exact four-root-script player package;
- focused package/transaction checks pass;
- an independent recheck reports `P0=0 / P1=0`;
- then, and only then, the one complete Release suite may start.

