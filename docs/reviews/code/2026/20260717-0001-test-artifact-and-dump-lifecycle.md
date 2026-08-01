# Test Artifact And Dump Lifecycle Review

- Review Status: recorded
- Date: 2026-07-17
- Source Request: user-requested cleanup implementation after the C/E storage audit, including the secondary dump-lifecycle findings
- Owning Update: [20260717-0001-test-artifact-and-dump-governance](../../../updates/2026/20260717-0001-test-artifact-and-dump-governance.md)

## 1. Unit And QA Test Artifact Accumulation

### Observed Facts

- `%TEMP%\DTMAPI-tests` held 23.122 GiB and `%TEMP%\DTMAPI-tests-persistent` held 0.351 GiB.
- The accumulated roots contained 6,104 report ZIPs (15.181 GiB) and 9,779 copied DLLs (7.304 GiB), plus GUID game trees, logs, Mods directories, and persistent-root fixtures.
- `DTMAPI.UnitTests` creates GUID directories through `Path.GetTempPath()` and restores environment variables after individual tests without deleting most owning GUID roots.
- `DTMAPI.QaUnitTests` independently creates additional GUID game directories under the system temp root.
- The directory names do not identify a test process, worktree, completion state, expiry, or active owner.

### Ownership And Root Cause

The test process is the missing lifecycle owner. Individual fixtures create child paths, but no process-level session encloses all fixture paths and guarantees cleanup. Per-test cleanup is incomplete and cannot recover artifacts after a killed process.

Moving the existing names to another drive without adding a process receipt, exclusive lease, exit cleanup, and stale-session scavenger would only relocate the leak.

### Acceptance Gates

- Every Unit/QA test process owns one receipt-bearing session and redirects its `TEMP`/`TMP` children into that session.
- A successful process removes its session before returning whenever handles allow it; the next process or explicit cleaner must recover any unlocked cleanup-pending session.
- A normal failed process keeps only a compact failure receipt unless the caller explicitly opts into retaining the full fixture tree.
- Full retained failures expire after 24 hours and share a 5 GiB cap; compact receipts expire after seven days.
- Parallel processes hold independent exclusive leases and cannot delete one another.
- The repository test entry point defaults the session base to ignored repository-local `tmp/test-runs`, while direct executable launches retain a safe system-temp fallback.

## 2. Diagnostics Report Reads Real Historical Unity Crashes

### Observed Facts

- A sample UnitTests report ZIP contained the same six real historical Unity crash directories.
- Production `DiagnosticsService.ExportLogs` intentionally scans the current user's Unity crash roots.
- Unit report tests inherit that production discovery scope, so thousands of otherwise disposable test reports repeatedly packaged the same `crash.dmp` and `Player.log` files.

### Ownership And Root Cause

The report code's player behavior is valid, but the test process did not isolate the Windows temp roots. A process-level temp session can make production discovery see only session-local synthetic crash roots without changing player behavior or adding a public runtime switch.

### Acceptance Gates

- Unit reports see only crash fixtures created under the current test session.
- A UnitTests report must not contain any pre-existing user crash identity.
- Production runtime discovery remains unchanged outside the test process.

## 3. Fatal-Window Process Dump Lifecycle

### Observed Facts

- `run-game-smoke.ps1` already defaults `FatalWindowProcessDumpMode` to `None` and captures only after a fatal-window detection when explicitly enabled.
- Capture writes a full temporary dump under `%TEMP%\DTMAPI-Dumps`, copies it to `GAME-SMOKE/<run-id>/Process-Dumps`, but does not compare source/destination length and SHA-256 or delete the temporary source in `finally`.
- Several approximately 4.9 GB dumps were retained without debugger analysis. Repeated full dumps with no signature or analysis state have low incremental value.
- `%TEMP%\DTMAPI-Dumps` was deleted as a whole at 2026-07-17 08:41:52 without a project cleanup receipt; the executing process cannot be attributed from current project evidence.

### Ownership And Root Cause

Dump capture lacks an independent run receipt and treats copy success as lifecycle completion. The temporary source and formal evidence copy therefore have no verified handoff boundary.

### Acceptance Gates

- Capture remains opt-in and uses one run-id temp directory with an exclusive lease and receipt.
- Copy completion requires equal non-zero length and equal SHA-256.
- A verified temporary source is deleted in `finally`; a failed handoff is retained with an explicit receipt for at most 24 hours.
- Temporary dump sessions older than two hours are reported and unlocked sessions older than 24 hours are recoverable by the cleanup tool.
- Formal `Process-Dumps` evidence includes a machine-readable hash/size/status receipt and remains governed by Markdown references and the evidence-retention allowlist.
- Duplicate formal dumps are not silently deleted. Same-signature/hash consolidation requires one canonical referenced dump plus a reviewed cleanup manifest.

## Rejected Directions

- Do not globally change Windows `TEMP`/`TMP` outside the test process.
- Do not move legacy GUID trees into Git or formal evidence.
- Do not make full dump capture a routine Smoke default.
- Do not delete formal process dumps merely because no debugger is installed.
- Do not recursively delete wildcard `DTMAPI-*` temp paths without a recognized receipt or an explicit legacy-cleanup mode.
