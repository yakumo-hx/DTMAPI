# Test Artifact And Process Dump Retention

Use this protocol for Unit/QA fixture trees, generated diagnostic reports, and fatal-window process-dump capture. These artifact classes have different retention authority.

## Test Session Ownership

`DTMAPI.UnitTests` and `DTMAPI.QaUnitTests` each create one process session through `tests/Shared/DtmApiTestSession.cs`.

- `tools/scripts/test.ps1` defaults `DTMAPI_TEST_TEMP_ROOT` to ignored repository-local `tmp/test-runs`; callers may override it with `-TestTempRoot` or the environment variable.
- Direct executable launches fall back to `%TEMP%\DTMAPI-test-sessions`.
- The process redirects only its own `TEMP` and `TMP`, so existing fixture code and diagnostics crash discovery stay inside the session without changing the user or machine environment.
- Every session has `session.json` with owner `DTMAPI.TestSession` and an exclusive `active.lock`.
- Success deletes the session. Ordinary failure writes a compact receipt under `_failure-receipts` and deletes the fixture tree.
- `DTMAPI_KEEP_FAILED_TEST_TEMP=1` or `tools/scripts/test.ps1 -KeepFailedTestTemp` retains one failed tree for diagnosis. Retained/unexpected sessions expire after 24 hours and share a 5 GiB cap. Compact receipts expire after seven days and share a 500 MiB cap.
- A cleaner may remove only a recognized receipt-bearing, unlocked session. Reparse points and unknown directories are not deletion candidates.

This also isolates `DiagnosticsService.ExportLogs`: UnitTests reports can see only session-local synthetic Unity crash roots and must not package the user's historical `%TEMP%\RedSawGames\...\Crashes` directories.

## Routine Inspection And Cleanup

Preview managed expired sessions:

```powershell
tools/scripts/cleanup-test-artifacts.ps1
```

Apply the generated plan:

```powershell
tools/scripts/cleanup-test-artifacts.ps1 -Apply
```

Pre-governance roots are deliberately excluded. A one-time cleanup requires the explicit switch:

```powershell
tools/scripts/cleanup-test-artifacts.ps1 -IncludeLegacy
tools/scripts/cleanup-test-artifacts.ps1 -IncludeLegacy -Apply -ManifestPath <reviewed-path>
```

The legacy set is exact: `DTMAPI-tests`, `DTMAPI-tests-persistent`, `DTMAPI-tests-author-state`, and `DTMAPI-QA-Tests` under the selected system temp root. The script refuses apply mode while a DTMAPI Unit/QA test or game-smoke process is active.

## Fatal-Window Process Dumps

`run-game-smoke.ps1` keeps `FatalWindowProcessDumpMode=None` as the default. Enable full capture only for a new native crash, hang, or critical boundary change where logs and Unity crash reports are insufficient.

An enabled capture:

1. creates `%TEMP%\DTMAPI-Dumps\<run-id>` with `dump-session.json` owner `DTMAPI.DumpCapture` and an exclusive lease;
2. captures into that run directory;
3. copies to the current `GAME-SMOKE/<run-id>/Process-Dumps` directory;
4. verifies equal non-zero length and SHA-256;
5. writes `process-dump-receipt.json` owner `DTMAPI.ProcessDumpEvidence` beside the formal dump;
6. removes the temporary source after verified handoff.

If handoff fails, the run directory is retained for at most 24 hours. Sessions older than two hours generate a warning; the temp root generates a capacity warning at 6 GiB. Recognized unlocked sessions are recovered after 24 hours. Non-capture runs also invoke the scavenger and should leave no managed dump temp behind.

Formal dumps are evidence, not temp:

- add any required dump path to a durable Markdown record so `build-evidence-retention-allowlist.ps1` can preserve it;
- record analysis status in the receipt or linked analysis output;
- for the same reviewed crash signature or identical SHA-256, retain one canonical dump rather than every reproduction;
- never silently delete duplicate formal dumps: build the allowlist, select the canonical identity, generate a byte/file manifest, and obtain explicit cleanup approval.

## Validation

Run the session/cleanup fixture check when changing that mechanism. Check the evidence allowlist when changing retained-evidence references or its generator; document-only edits use the document check. These are separate triggers:

| Changed boundary | Check |
| --- | --- |
| Session ownership, cleanup or retention behavior | `tools/scripts/check-test-artifact-governance.ps1 -RunCleanupFixture` |
| Retained-evidence references or allowlist generator | `tools/scripts/build-evidence-retention-allowlist.ps1 -Check` |
| Protocol/record text | `tools/scripts/check-doc-governance.ps1` and changed links |

Ordinary product tests use the existing automatic session lifecycle. They do not require a manual cleanup pass, allowlist regeneration or a second test run merely to prove cleanup again. Inspect a specific session if cleanup reports a failure or a retained tree is needed for diagnosis.

When validating the session/cleanup mechanism itself, `tmp/test-runs` should contain no completed session after success; a second run must not accumulate old GUID trees. A failed retained session must print its exact path and be recoverable after its retention window.
