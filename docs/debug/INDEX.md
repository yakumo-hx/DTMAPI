# DTMAPI Debug Index

This file is a compact router. Detailed symptoms, attempts, evidence, and rejected hypotheses belong in their canonical records, not in this index.

## Current Routes

- [Issue ledger](issues/README.md): current issue states and links to one file per recurring symptom.
- [Protocols](protocols/README.md): repeatable evidence-collection procedures.
- [Evidence retention protocol](protocols/evidence-retention.md): bounded current-run collection, generated preservation allowlist, and cleanup safety boundary.
- [Test artifact and process dump retention](protocols/test-artifact-retention.md): process-owned test temp, failed-session bounds, dump handoff receipts, and stale recovery.
- [Evidence retention allowlist](evidence-retention-allowlist.json): generated identities referenced by project Markdown; preservation input only, not deletion authorization.
- [Active smoke matrix](regressions/smoke-matrix.md): runtime validation recorded after the 2026-07-11 cutoff.
- [Historical smoke matrix through 2026-07-11](regressions/smoke-matrix-history-through-20260711.md): full pre-cutoff runtime history.
- [Smoke history for 2026-07-12](regressions/smoke-matrix-history-20260712.md): completed owner/platform and YConsole acceptance slice.
- [Superseded Batch 4 smoke attempts, 2026-07-15 through 2026-07-17](regressions/smoke-matrix-history-batch4-superseded-20260715-through-20260717.md): cutoff rows moved from the active router after later acceptance superseded them.
- [Lessons learned](lessons.md): rejected directions and durable engineering lessons.
- `evidence/`: local-only raw logs and reports; do not recursively scan or copy by default.
- [Native-owner domain library](../reviews/api/native-owner-domains/INDEX.md): API/GameBridge discovery evidence.

## Historical Index Snapshot

- [Detailed Debug Index through 2026-07-11](INDEX-history-through-20260711.md)

The snapshot preserves the former long-form index. Do not append new issue narratives to it.

## Rule

Before changing runtime lifecycle, shutdown, BepInEx, Harmony patches, event dispatch, input, config menu, Workshop loading, or mod loading:

1. read the relevant issue and protocol rather than the full history;
2. record new runtime facts in the issue file;
3. add a smoke row only when a game/runtime validation actually ran;
4. link the implementation Update instead of copying its full summary here.
