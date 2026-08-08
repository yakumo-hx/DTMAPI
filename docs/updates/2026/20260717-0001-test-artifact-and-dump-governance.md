# Update 20260717-0001: Test Artifact And Dump Governance

- Lifecycle Status: `verified`
- Validation Level: `docs,source,unit`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Date: 2026-07-17
- Source Request: implement bounded cleanup for generated test artifacts and bring fatal-window process dumps into the same ownership and retention system
- Related Review: [Test Artifact And Dump Lifecycle Review](../../reviews/code/2026/20260717-0001-test-artifact-and-dump-lifecycle.md)

## Scope

- Add one process-owned session around Unit and QA test temporary content.
- Route repository test runs to ignored repository-local storage without hard-coding a drive letter.
- Add exclusive leases, receipts, success cleanup, opt-in failed-fixture retention, stale-session recovery, and bounded failure receipts.
- Isolate UnitTests diagnostics reports from the user's historical Unity crash directories.
- Make fatal-window dump handoff verifiable and remove the C-drive temporary source after a verified copy.
- Extend evidence-retention documentation and automated source checks for test sessions and dumps.
- Remove the pre-governance legacy Unit/QA temp roots only after confirming no relevant process is active, and record the exact deletion result.

## Changed Files

- `tests/Shared/DtmApiTestSession.cs`: added process receipts, exclusive leases, process-local `TEMP`/`TMP` isolation, success/failure cleanup, compact failure receipts, explicit full-failure retention, 24-hour scavenging, and 5 GiB/500 MiB caps.
- `tests/DTMAPI.UnitTests/DTMAPI.UnitTests.csproj`, `tests/DTMAPI.QaUnitTests/DTMAPI.QaUnitTests.csproj`: linked the shared session owner.
- `tests/DTMAPI.UnitTests/Program.cs`, `tests/DTMAPI.QaUnitTests/Program.cs`: bound each executable's outer lifecycle to the session and recorded success/failure before exit.
- `tools/scripts/test.ps1`: defaulted managed tests to ignored repository-local `tmp/test-runs`, exposed root/full-failure overrides, and applied an outer post-process cleanup for loaded-DLL cleanup-pending sessions.
- `tools/scripts/dump-governance.ps1`: added reusable non-empty file handoff with equal-length and SHA-256 verification.
- `tools/scripts/run-game-smoke.ps1`: retained opt-in dump capture, added run-id temp ownership/lease/receipt, verified formal handoff, deleted verified temporary sources, wrote formal evidence receipts, and scavenged/warned on stale/capacity-bound temp sessions.
- `tools/scripts/cleanup-test-artifacts.ps1`: added preview-first managed cleanup plus explicit legacy cleanup, active-process/lease protection, path containment, reparse rejection, and JSON manifests.
- `tools/scripts/check-test-artifact-governance.ps1`: added source invariants and synthetic success, active-lease, dump-session, no-capture, and verified-handoff checks.
- `AGENTS.md`, `docs/debug/INDEX.md`, `docs/debug/protocols/README.md`, `docs/debug/protocols/evidence-retention.md`, `docs/debug/protocols/test-artifact-retention.md`: made the lifecycle and dump evidence boundary durable.
- `docs/reviews/code/2026/20260717-0001-test-artifact-and-dump-lifecycle.md`: froze root causes, rejected directions, and acceptance gates.
- `docs/debug/evidence-retention-cleanups/20260717-legacy-test-temp.json`: recorded the exact four-root legacy deletion.
- `docs/updates/INDEX-2026-07.md`: registered this Update in the monthly ledger.

## Validation

- Full Release build with `tools/scripts/build.ps1 -Configuration Release -SkipTests`: passed with zero warnings and zero errors.
- Unit and QA test project builds: passed with zero warnings and zero errors.
- `DTMAPI.QaUnitTests`: passed and removed its process session immediately.
- `DTMAPI.UnitTests`: reached the current branch's unrelated QA activation check and failed at `QaHostActivationLoader.ValidateLoadedAssemblyIdentity` because the optional QA assembly FileVersion/ProductVersion did not match its activation receipt. The managed lifecycle wrote only a compact 18 KiB failure receipt; the outer cleaner removed the loaded-DLL cleanup-pending session and reported `REMAINING_SESSION_COUNT=0`. This pre-existing concurrent Batch 4 failure is not hidden as a pass and is outside this storage change.
- `check-test-artifact-governance.ps1 -RunCleanupFixture`: passed. It dynamically verified completed Unit and dump cleanup, active-lease preservation, cleanup after lease release, default `FatalWindowProcessDumpMode=None`, no dump temp creation in no-capture mode, non-empty length/SHA-256 handoff, and empty-source rejection.
- Windows PowerShell 5.1 parser checks for all changed PowerShell scripts: passed.
- Evidence allowlist rebuild/check: passed with 745 referenced Smoke runs, 62 canonical runtime identities, and the existing six process-dump identities; generic protocol examples did not become preservation entries.
- `check-doc-governance.ps1`: passed 5,138 checks.
- `git diff --check`: passed; only repository line-ending warnings were emitted.
- No game launch, install, runtime lock, or new full dump capture was required. Actual full-dump capture remains an explicit failure-only operation.

## Evidence

- Pre-implementation storage audit: `%TEMP%\DTMAPI-tests` 23.122 GiB; `%TEMP%\DTMAPI-tests-persistent` 0.351 GiB.
- Report content audit: 6,104 ZIPs / 15.181 GiB and 9,779 DLL copies / 7.304 GiB, with repeated historical Unity crash payloads.
- Executed cleanup manifest: `docs/debug/evidence-retention-cleanups/20260717-legacy-test-temp.json`, SHA-256 `C2E576901D3D8FF2D09FBB32FF4131E4FCC47E80BD127331A64E9DF37289BC21`.
- Deleted exactly four legacy roots: 105,046 files, 166,244 directories, and 25,208,281,312 bytes. All four results are `deleted` and post-checks report the paths absent.
- C-drive free space increased from 41.192 GiB immediately before cleanup to 64.837 GiB after cleanup and validation.
- `%TEMP%\DTMAPI-Dumps` is absent after validation. The repository default managed root has no completed session.

## Rollback

Revert the test-session integration and dump handoff changes. The legacy temp data deletion, once executed, is intentionally irreversible because those directories are reproducible fixtures rather than referenced evidence; its manifest will remain as an audit record.

## Follow-up

- Reassess the formal dump corpus after a debugger is available or stable crash signatures can be derived.
- Keep normal dump capture disabled unless a new native crash, hang, or critical boundary change justifies it.
- Resolve the concurrent Batch 4 QA activation FileVersion/ProductVersion test failure in its owning work before claiming a full repository UnitTests pass; it does not reopen this verified artifact lifecycle.
