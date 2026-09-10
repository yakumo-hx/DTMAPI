# Issues

Create one file per recurring bug or symptom.

Name format:

```text
ISSUE-001-steam-stopping.md
ISSUE-002-save-load-stutter.md
```

Each issue owns one `- State:` and one `- Current boundary:` field, plus reproduction, evidence, rejected hypotheses, attempts, and acceptance criteria. Run `tools/scripts/sync-issue-index.ps1` after changing these fields; the table below is generated.

Issue state vocabulary: `open`, `monitoring`, `mitigated`, `verified`, `closed`, or `deferred`. A verified Update does not automatically close a broader Issue.

## Current Issues

<!-- generated: issue-index:start -->
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
| [ISSUE-011](ISSUE-011-20260623-short-run-native-crash.md) | verified | Exact 0.6 Local11 candidate passed title/Y-console/Input-System/crash-package/Steam-exit acceptance; the dump-less historical 0.5.2 native owner remains unproven and a fresh recurrence reopens diagnosis. |
| [ISSUE-012](ISSUE-012-20260711-player-title-settings-runtime-missing.md) | mitigated | Restart restored BepInEx/DTMAPI and the player-visible title button; exact transient native trigger remains unproven. |
| [ISSUE-013](ISSUE-013-20260712-owner-platform-dependency-reconciliation.md) | verified | Registry-only platform providers survive unchanged title/Workshop refresh; slot-3 owner and no-op reconciliation smokes retain `29/6`, one Entry per code Mod, and zero false restart/cleanup state. |
| [ISSUE-014](ISSUE-014-20260712-y-console-close-double-toggle.md) | verified | Player physical testing remains passed; normal Steam/no-HookProbe automation also re-verifies the retained old DebugConsole DLL through owner-targeted legacy modal dispatch, bounded Escape drain, stable short/held Y input, and no native-menu leak. |
| [ISSUE-015](ISSUE-015-20260727-debugconsole-hook-transactions.md) | verified | Compatibility Hook topology is edge-triggered/transactional, ProductNative cleanup and exact-original leases are retryable, dual owners fail closed, and the exact-current retained 0.3.1 route passed its final player reacceptance. The user later confirmed that old product has no remaining users; physical retirement is separate work. |
| [ISSUE-016](ISSUE-016-20260731-audio-hook-idempotent-status-republish.md) | mitigated | Physical audio status publication is transition-based and focused/full Unit gates pass; corrected Manbo plus delayed ItemDisplayName player recheck remains open. |
| [ISSUE-017](ISSUE-017-20260801-official-mod-ui-source-transaction.md) | verified | Official Mod-page opening is preview-only; final player QA proves one successful completed native close queues one next-frame source refresh with zero new warning. |
| [ISSUE-018](ISSUE-018-20260803-runtime-installer-entry-host-compat.md) | open | Old BAT compound blocks break on `Program Files (x86)` paths; ambient CMD-extension and degraded WinPS capability gaps are separate. Source redesign passes the exact path, while publication/player retest remains open. |
| [ISSUE-019](ISSUE-019-20260804-moresaves-100-legacy-slot-migration.md) | verified | Corrected Local 1.0.1 passed isolated 16-role migration, populated official #7--#12 UI, native index 6 load, cold `0/0` idempotence, native index 11 load and clean exit without player archive writeback. |
| [ISSUE-020](ISSUE-020-20260805-enabled-official-source-arbitration.md) | open | Enabled-first official Local selection is source-verified. The old Y-console cold-start subcondition was superseded by later exact 1.1.1 publication/acceptance; this issue remains open only for the broader multi-product source-arbitration closeout. |
| [ISSUE-021](ISSUE-021-20260805-moreequipment-native-placement-save-quarantine.md) | verified | Exact Product-disabled cold recovery passed disposable native prepare/commit/NoNativeSave-observe processes with one recovered item, save quarantine/commit, zero replay, unchanged final archives/sidecars and whole-fixture cleanup. |
| [ISSUE-022](ISSUE-022-20260807-runtime-tools-move-access-denied.md) | open | Two players reach the Runtime transaction but receive access denied while publishing `candidate\tools`; an EXE-free standalone package is the current isolation/workaround attempt. |
| [ISSUE-023](ISSUE-023-20260808-runtime-installer-stress-boundaries.md) | mitigated | The 0.6.1 source/package matrix now freezes host proof, per-game mutation ownership, pending-transaction uninstall, unique complete log publication and truthful no-op receipts; publication/player acceptance remains pending. |
| [ISSUE-024](ISSUE-024-20260817-moreequipment-newgame-null-slot-ui.md) | mitigated | MoreEquipmentSlots 1.0.1 resolves the initialized native index and creates an empty pending document at NewGame; first-save/cold-load runtime acceptance passed, while first-session UI screenshot and Product-absent coverage remain. |
| [ISSUE-025](ISSUE-025-20260820-runtime-installer-receiptless-residue.md) | mitigated | First-receipt retry/readback and shared fail-closed classification passed the dual-host and package matrices; affected-player convergence remains pending. |
| [ISSUE-026](ISSUE-026-20260820-moreequipment-legacy-sidecar-player-mismatch.md) | open | Archive 0 found a scoped flat legacy MoreEquipment sidecar whose embedded player identity differs from the current save; migration safely stops before cross-save attachment, while player artifact classification/recovery remains pending. |
| [ISSUE-027](ISSUE-027-20260823-config-mod-list-pager-snapback.md) | mitigated | Config selection following is separated from explicit paging; a real 16-page Next/Mod-selection/Previous title smoke passed with enlarged non-overlapping controls and zero QA-owner residue. Lower-resolution player usability confirmation remains open. |
| [ISSUE-028](ISSUE-028-20260824-moreequipment-save-slot-reuse-v3-sidecar.md) | mitigated | MoreEquipmentSlots 1.0.1 deletes the exact stale Product directory only at a proven NewGame boundary and passed first-save/cold-load acceptance; absence across both deletion and NewGame remains explicit. |
<!-- generated: issue-index:end -->
