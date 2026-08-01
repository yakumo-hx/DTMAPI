# Issues

Create one file per recurring bug or symptom.

Name format:

```text
ISSUE-001-steam-stopping.md
ISSUE-002-save-load-stutter.md
```

Each issue should include state, reproduction, evidence, rejected hypotheses, attempts, and acceptance criteria.

Issue state vocabulary: `open`, `monitoring`, `mitigated`, `verified`, `closed`, or `deferred`. A verified Update does not automatically close a broader Issue.

## Current Issues

| Issue | State | Current boundary |
| --- | --- | --- |
| [ISSUE-001](ISSUE-001-direct-exe-fatal-popup.md) | open | DirectExe fatal popup; script mitigation exists. |
| [ISSUE-002](ISSUE-002-hookprobe-blocks-hotkeys.md) | mitigated | HookProbe/input interaction mitigation retained. |
| [ISSUE-003](ISSUE-003-hotkey-openconfig-no-overlay.md) | deferred | Old overlay path is not active product UI. |
| [ISSUE-004](ISSUE-004-steam-launch-stuck.md) | monitoring | Steam restart mitigation; keep recurrence evidence. |
| [ISSUE-005](ISSUE-005-20260603-manual-qa-024-regressions.md) | open | Historical manual-QA batch retains unresolved boundaries. |
| [ISSUE-006](ISSUE-006-20260604-critical-manual-qa-025.md) | verified | Smoke-backed; manual visual recheck remains useful. |
| [ISSUE-007](ISSUE-007-20260605-mine-yconsole-026.md) | verified | Current record is smoke-verified. |
| [ISSUE-008](ISSUE-008-20260605-manual-qa-027-root-cause.md) | verified | Current record is smoke-verified. |
| [ISSUE-009](ISSUE-009-20260608-camera-playable-dynamic-qa.md) | verified | Dynamic CameraView smoke evidence recorded. |
| [ISSUE-010](ISSUE-010-20260620-long-run-mono-gc-crash.md) | open | Title-input pressure mitigated; long gameplay/native GC class remains open. |
| [ISSUE-011](ISSUE-011-20260623-short-run-native-crash.md) | open | Evidence improved; game validation pending. |
| [ISSUE-012](ISSUE-012-20260711-player-title-settings-runtime-missing.md) | mitigated | Restart restored BepInEx/DTMAPI and the player-visible title button; exact transient native trigger remains unproven. |
| [ISSUE-013](ISSUE-013-20260712-owner-platform-dependency-reconciliation.md) | verified | Registry-only platform providers survive unchanged title/Workshop refresh; slot-3 owner and no-op reconciliation smokes retain `29/6`, one Entry per code Mod, and zero false restart/cleanup state. |
| [ISSUE-014](ISSUE-014-20260712-y-console-close-double-toggle.md) | verified | Player physical testing remains passed; normal Steam/no-HookProbe automation also re-verifies the retained old DebugConsole DLL through owner-targeted legacy modal dispatch, bounded Escape drain, stable short/held Y input, and no native-menu leak. |
| [ISSUE-015](ISSUE-015-20260727-debugconsole-hook-transactions.md) | mitigated | Compatibility Hook topology is edge-triggered/transactional, ProductNative cleanup and exact-original leases are retryable, and dual owners fail closed; current-byte old-route player reacceptance remains pending. |
| [ISSUE-016](ISSUE-016-20260731-audio-hook-idempotent-status-republish.md) | mitigated | Physical audio status publication is transition-based and focused/full Unit gates pass; corrected Manbo plus delayed ItemDisplayName player recheck remains open. |
| [ISSUE-017](ISSUE-017-20260801-official-mod-ui-source-transaction.md) | verified | Official Mod-page opening is preview-only; final player QA proves one successful completed native close queues one next-frame source refresh with zero new warning. |
