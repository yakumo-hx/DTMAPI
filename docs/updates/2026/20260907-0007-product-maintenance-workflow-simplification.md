# 20260907-0007: Product maintenance workflow simplification

## Metadata

- Update ID: `20260907-0007`
- Date: `2026-09-07`
- Lifecycle Status: `verified`
- Validation Level: `docs, source, unit`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: 用户要求压缩实际强制读取量，明确产品修复测试前提、触发条件与结束条件，减少不必要隔离，再处理 Catalog 和记录重复投影；不优化快速治理检查耗时。
- Review: [workflow review](../../reviews/code/2026/20260907-0007-product-maintenance-workflow-review.md)

## Summary

Ordinary product fixes now read compact common guidance and a bounded validation checklist. Tests start with a resolved product/profile/slot/save entry and finish when affected acceptance, disk-state evidence and clean exit pass. Existing environments and explicitly disposable slots are valid direct-test routes; isolation has named triggers. Catalog candidate checks use source relationships instead of per-version constants, and monthly status is projected from the owning Update.

## Changed Files

- Common guidance: [AGENTS](../../../AGENTS.md), [PROJECT](../../../PROJECT.md), [current-state](../../onboarding/current-state.md).
- Task-specific guidance: [product validation](../../workflows/product-change-validation.md), [API workflow](../../workflows/codex-api-rebuild.md), [feedback workflow](../../workflows/codex-feedback-to-goal.md), [Review router](../../reviews/README.md), [Runtime package matrix](../../workflows/workshop-package-subscription-test-matrix.md).
- Records: [governance](../../workflows/document-governance.md), [Update template](../README.md), this Update, its linked Review and [monthly row](../INDEX-2026-09.md).
- Catalog: [checker](../../../tools/scripts/check-product-catalog.ps1), [Catalog freeze normalization](../../../tools/release/dtmapi-product-catalog.json), [admission generator](../../../tools/scripts/generate-managed-product-admission-registry.ps1) and its [generated document](../../architecture/managed-product-admission-registry.md).
- Maintenance tools: [ledger synchronization](../../../tools/scripts/sync-update-ledger.ps1), [focused regressions](../../../tools/scripts/test-product-maintenance-tools.ps1).

## Validation

- PASS: `tools/scripts/test-product-maintenance-tools.ps1` under PowerShell 7.6.3. Ledger fixtures cover new/existing rows, unchanged human text and neighboring rows, LF/CRLF and BOM preservation, no-op mtime/hash, read-only stale detection, invalid metadata/date/path and duplicate IDs/rows. Catalog fixtures cover date-only acceptance and unchanged admission output, source-version mismatch without an identity-freeze error, and rejection of candidate/identity/retained-evidence/release-stop drift.
- PASS: `tools/scripts/test-product-maintenance-tools.ps1 -LedgerOnly` under Windows PowerShell 5.1, using process-local `-ExecutionPolicy Bypass` as required by that host's script policy. No machine/user policy was changed.
- PASS: `check-product-catalog.ps1 -Quiet`, `generate-managed-product-admission-registry.ps1 -Check -Quiet`, PowerShell parsing, `check-doc-governance.ps1 -Quiet`, changed-document local links and `git diff --check`.
- During fixture validation, corrected PowerShell's null-string binding for atomic `File.Replace`; the completed reruns above pass. Fixtures were removed from the script-owned repository temp child.
- Runtime/full Release/game validation: not required; no runtime code or package behavior changed. No game smoke row was created.

## Evidence

- Prior analysis, screenshot transcription, historic repair commit and official OpenAI guidance are in the linked Review. The governance checker was not modified or performance-optimized.
- Reading measurements use Unicode character counts with CRLF normalized to LF; these are not token counts or a promised time saving:

| Guidance | Before | After |
| --- | ---: | ---: |
| AGENTS | 10,236 | 3,061 |
| PROJECT | 9,615 | 4,439 |
| current-state | 4,419 | 2,797 |
| Common required total | 24,270 | 10,297 |

Common reading falls 57.6%; AGENTS falls 70.1%. Including the new product checklist and compact Update instructions, an ordinary implementation's generic reading is 19,596 characters versus the previous common-plus-mandatory-governance minimum of 34,319 (42.9% less). Both figures exclude task-specific code/tests/evidence. The eight rewritten guides plus the new product checklist total 32,357 characters versus 68,336 in the original eight; this is not merely moving text to another mandatory file.

- PROJECT now explicitly states native-save commit semantics, ordinary in-place NoNativeSave testing and user-designated disposable-slot use. Fault injection, multi-save/startup mutation and uncontrolled writes retain isolated fixtures. Existing specialist runner assertions remain intact; direct game evidence is a separate valid route.
- Catalog normalization drops only mutable source version/minimum from the identity digest; this authorized normalization change recomputes its digest once. Identity/path fields, published versions, retained artifacts, ABI/reference-policy checks, subscription observations and release authorization remain protected. Catalog/source/target fields are retained for existing consumers; this change removes checker constants and date-only admission projection rather than replacing the Catalog schema.
- New record flow edits one Update, then synchronizes its monthly status. Issue, Hook, API, smoke and release records change only when their owned facts change; no parallel status store was added.

## Rollback Notes

- Revert only the files/changes listed above; retain the pre-existing Wiki changes and artifacts. Revert Catalog normalization, checker digest and admission generator/output together. No player or published package bytes are changed.

## Follow-Up

- None required for this bounded change. A later real product fix can establish actual wall-clock savings; this documentation/tool validation does not claim a measured reduction of the historical 1h 47m repair.
