# 20260712-0001 Owner Version And Diagnostic Byte Boundary Follow-Up

Status: recorded
Date: 2026-07-12
Scope: follow-up code review of ordinary-provider version authority, retained diagnostic bytes, and Release warning accuracy
Related Update: `docs/updates/2026/20260712-0003-owner-version-diagnostic-byte-bounds.md`
Related baseline: `docs/reviews/code/2026/20260711-0001-general-owner-lifetime-boundary-audit.md`

## Source Request

The user reported three remaining findings after the owner-lifetime reconciliation commit and requested implementation plus a same-directory manifest-upgrade regression test.

## Finding 1 — P1: ordinary provider in-place upgrade can advertise a version newer than the resident DLL/API

Original feedback:

- dependency availability used the rediscovered manifest version;
- source identity did not include manifest version;
- a provider loaded as `1.0` could have its on-disk manifest edited to `2.0`, allowing a newly discovered consumer requiring `>=2.0` even though the process still hosted the `1.0` assembly/API.

Review:

- User-confirmed code fact: `TryGetAvailableDependencyVersion` returned `discoveredById[provider].Manifest.Version` for ordinary providers.
- Code fact: `ModRegistry.Get(provider)` retains the manifest committed with the actual loaded owner transaction and is therefore the process-resident version authority.
- Code fact: source identity compared `Source + OfficialId + RootPath`, so an in-place version edit did not deactivate the old owner.
- Decision: ordinary dependency checks must use the loaded registry manifest version. A version change at the same source identity is treated as an update-pending source handoff: deactivate the loaded owner and required dependents in reverse order; a successfully loaded assembly stays restart-required and the new version cannot enter until a clean process.
- Rejected direction: keeping the resident owner active while publishing the refreshed version. This preserves the exact false-version authority the finding identifies.
- Acceptance: provider `1.0` plus an active `>=1.0` dependent are deactivated when the same directory changes to `2.0`; a newly introduced `>=2.0` consumer does not enter; every old assembly executes Entry only once; a clean runtime may load `2.0` and its consumers.

## Finding 2 — P2: bounded diagnostic row counts still retain unbounded strings

Original feedback:

- the 64 owner-ledger failures can retain arbitrary-length details;
- diagnostic aggregate key/message/details can also retain arbitrary-length caller-controlled text;
- object count is bounded, but retained bytes are not.

Review:

- User-confirmed code fact: `ModOwnerLedgerService.AddFailureNoLock` copied every string field directly into retained entries.
- User-confirmed code fact: `DiagnosticsService.RecordDiagnosticCounter` built its dictionary key from unsanitized owner/message and retained unsanitized details in aggregate counters.
- Decision: route every retained scalar field through one byte-bounded sanitizer. Truncated values keep a stable hash suffix, and each service exposes a saturating `trimmedBytes` scalar so loss is observable without retaining the discarded content.
- Safety boundary: hashing/truncation must not create another long-lived object graph; caps must apply before dictionary-key construction and before recent-failure/aggregate storage.
- Acceptance: very large owner/key/message/details inputs produce fixed per-field retained limits, stable hash suffixes, positive trimmed-byte counters, bounded aggregate/row counts, and no unbounded copy in snapshots or formatted summaries.

## Finding 3 — P2: Release validation emitted CS8602

Original feedback:

- `DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs` emitted CS8602 near the owner-bound facade mismatch exception;
- prior documentation claiming zero warnings was therefore inaccurate.

Review:

- Code fact: `EnsureActive()` proves `owner != null` at runtime, and the condition already uses `owner!`; a second unannotated access in the exception message loses that flow fact.
- Decision: apply the minimal null-forgiving annotation to the second access without changing control flow, public API, target framework, or runtime behavior.
- Acceptance: the single project and complete Release script both report zero warnings and zero errors.

## Documentation Boundary

Implementation, validation, changed files, and final status belong to Update `20260712-0003`. This review remains the pre-implementation reasoning record. No public API stability or Hook fact changes.
