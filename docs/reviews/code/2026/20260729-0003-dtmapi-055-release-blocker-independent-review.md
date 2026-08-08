# 20260729-0003: DTMAPI 0.5.5 Release Blocker Independent Review

Status: `recorded / accepted for complete Release entry`

## Scope

This is the requested small independent acceptance review for the two P1
findings in
`docs/reviews/code/2026/20260729-0002-dtmapi-055-release-entry-and-transition-matrix-audit.md`.
It reviews:

1. whether a lost or corrupt `install-local` JSON report can still let
   `RecoveryRequired` be registered by the outer installer as a successful
   managed product install; and
2. whether the frozen 0.5.5 player package now exposes exactly root BAT
   entrypoints `1` through `4`, while retaining the probe only at its separate
   diagnostic boundary and as a dormant internal helper.

The reviewed implementation commits are:

- `cc6b872d` — exact committed Author SDK reconciliation and combined
  crash/corrupt-report coverage;
- `493de436` — exact four-BAT player-package root and packaged-layout
  assertion.

The reviewed branch is `codex/major-update-batch0-20260713`, with source HEAD
`493de436d2f7`. This audit did not change implementation, create an Update,
run the complete Release suite, launch Doloc Town, touch Steam, or install
into the shared game Runtime.

## Result

`P0=0 / P1=0 / P2=0`.

Both findings from Review `20260729-0002` are resolved within their bounded
scope. The exact frozen package and focused transaction/package evidence are
accepted. The one from-start complete Release suite may begin; this Review
does not claim that suite has already passed.

## Finding 1: RecoveryRequired Cannot Cross The Product Commit Boundary

The corrected outer reconciliation path in
`tools/scripts/install-to-game.ps1` now requires all of the following before
it changes the unknown mutating result into a committed reconciliation:

- `install-local-status` exits successfully and returns `success=true`;
- `values.status` is ordinal-exact `CommittedLocalDevelopment`;
- the top-level deployment tree SHA-256 is exactly 64 hexadecimal
  characters; and
- `values.sourceTreeSha256` is exactly 64 hexadecimal characters.

The frozen Author SDK's `install-local-status` implementation derives that
committed status only after validating the exact journal, committed package
hash, manifest identity/version, installed inventory, and Local Development
source selection. A pending composite transaction instead returns
`success=true` with `values.status=RecoveryRequired` and no committed tree
digests.

The outer installer now treats that state as an error before it can set
`Phase=AuthorSdkAtomicLocalInstallCommitted`, `PublishSucceeded=true`, append
the product to `BundledMods`/installed files, or emit the installed result.
It gives explicit SDK recovery guidance and does not replay the mutation. The
old fallback that could set `$deployExit=0` from a status value after the main
check has also been removed.

The focused managed transaction test uses the frozen real Author SDK and
combines corrupt outer output with both process-style interruption points:

- `crash:install-local.after-prepare`;
- `crash:install-local.after-source-selection`.

For each point it requires a nonzero installer exit, explicit
`RecoveryRequired` rather than `CommittedLocalDevelopment`, no installed
message, a failed product attempt still at
`AuthorSdkAtomicLocalInstall`, `PublishSucceeded=false`, and no
`ReadOnlyReconciliation` commit marker. It then independently queries the
pending SDK authority, requires the still-retryable `RecoveryRequired`
marker, runs explicit `recover`, and proves the exact pre-transaction
destination tree, deployment journal, and source-state hashes are restored.

The product-specific failed-attempt assertions are the correct outer
registration proof. The base Runtime receipt may legitimately be refreshed
before product publication, so requiring the entire outer release-manifest
file to remain byte-identical would test an unrelated condition.

Observed focused evidence:

- the managed official-local transaction matrix passed under both available
  PowerShell hosts (`hosts=2`);
- all four changed PowerShell scripts passed the Windows PowerShell syntax
  check.

Conclusion: accepted. `RecoveryRequired` remains a visible, retryable
recovery state and cannot be relabeled or registered as a committed product
install through this lost/corrupt-report path.

## Finding 2: Player Root Is Exactly BAT 1 Through 4

`tools/scripts/build-release-workshop-packages.ps1` now clears the Runtime
package directory and copies exactly:

```text
1_install_dtmapi.bat
2_uninstall_dtmapi.bat
3_check_dtmapi_status.bat
4_collect_dtmapi_logs.bat
```

It no longer copies `0_probe_dtmapi_install.bat` to the player package root.
The generator still copies
`Content/DTMAPIInstaller/tools/probe-install-preflight.ps1`, so the internal
diagnostic implementation remains available without becoming a fifth
player-facing entrypoint.

`tools/scripts/test-player-doctor-packaged-entrypoints.ps1` now compares the
complete root `*.bat` set against the exact four expected names, explicitly
rejects root `0_probe`, and requires the dormant internal probe script. The
freshly generated-package focus passed, and this independent review reran
the same offline entrypoint matrix directly against the frozen candidate;
it passed install, expected placement-invalid status, log collection, and
uninstall behavior in the isolated fake-game fixture.

The retained standalone diagnostic ZIP
`dist/DTMAPI-player-probe-20260703-install-preflight-v3.zip` remains outside
the player package. Read-only inspection found one root
`0_probe_dtmapi_install.bat`, zero `1_install_dtmapi.bat`, and one internal
`probe-install-preflight.ps1`, preserving the previously selected diagnostic
boundary.

Conclusion: accepted. The package generator can no longer regenerate the
fifth root BAT into the official player package.

## Frozen Player Package Recheck

The independently rehashed directory is
`dist/prerelease-step5-candidate/DTMAPI`.

| Property | Accepted value |
| --- | --- |
| Files / bytes | `30 / 71,532,156` |
| release/retained-artifact tree | `9661b96fc106ee7372de26c79e0fd2a9700295d72ec1b8695c4c7d4bb6e38034` |
| `DTMAPI-FileTree-SHA256-v1` | `3e0e34703ab43fcdb136075ac3e49ec3d0fa5470d132fc6e87d76009561cb477` |
| `DTMAPI-CandidateStructure-SHA256-v1` | `6cf56275cfe2cc913c38b2cba717e2eeeb074e5743b3bc4849f6fdb180153418` |
| release-manifest SHA-256 | `4d8e54f02d7ef7ae3d43c5ddd032704a7d491511c65aa8200744d61c006e0408` |
| release-manifest `BuildCommit` | `493de436d2f7` |
| Root BAT set | exact `1`, `2`, `3`, `4` |
| Root `0_probe` | absent |
| Internal probe script | present |

The two implementation commits changed only installer/package PowerShell
sources and tests. A direct byte/hash comparison confirmed that the six
game-loaded or dormant-shipped Runtime/Compatibility assemblies are
unchanged from the accepted prior freeze:

| Assembly | Bytes | SHA-256 |
| --- | ---: | --- |
| `DTMAPI.BepInExBootstrap.dll` | 161,280 | `42831b4a76e6b7285a16192eea1fb2a399e3984c01fbacb100d2a627ad0fee38` |
| `DTMAPI.Abstractions.dll` | 242,688 | `3cd0eee2815e5b8254c4d9c2b32d101c77364b55eca462759f2244b94add1295` |
| `DTMAPI.Core.dll` | 922,112 | `a7ad62a34a2001c077a25e0684fb48f5e4ce90882c1a0bbb25fcbd4078fb7185` |
| `DTMAPI.GameBridge.DolocTown.dll` | 651,776 | `a18e7949c9a2e45a36ddab68a150f7fa2f762c3d3dd3b8751de09c7cc29d1e1a` |
| `DTMAPI.ModConfigMenu.dll` | 40,448 | `926e020d052d051f048e49dcf06944b6cee1db5601f9495e6fcae387a1f288ba` |
| `DTMAPI.GameBridge.DolocTown.Compatibility.dll` | 630,272 | `f8962000aa62a2b199dc9fbd0655749c8f0458eb9b956134a1129b7909321e4a` |

## Release Entry Decision

The resolution gate in Review `20260729-0002` is satisfied:

- non-`CommittedLocalDevelopment` reconciliation fails closed;
- both combined crash/corrupt-report cases pass and recover the exact
  pre-state;
- the generated and frozen player roots contain exactly BAT `1` through `4`;
- the packaged entrypoint/layout focus passes;
- this independent review reports `P0=0 / P1=0 / P2=0`.

Proceed with one clean, from-start `tools/scripts/test.ps1 -Configuration
Release` run against the current source state. Rehash the exact frozen player
directory after the suite; the complete Release's temporary package is not a
replacement for this frozen candidate.
