# 20260716-0002 Batch 2/3/4 Detailed Acceptance Correction

- Lifecycle Status: `verified`
- Validation Level: `docs,source,unit`
- Runtime Validation: `not-run`
- Related Issue State: `open`
- Area: review/major-update/batch2/batch3/batch4/qa-boundary/catalog/release
- Source: user requested a detailed post-interruption review of supplemental Batch 2/3 and newly completed Batch 4
- Primary review: `docs/reviews/code/2026/20260716-0001-batch2-batch3-batch4-detailed-acceptance-review.md`
- Primary route: `docs/reviews/code/2026/20260712-0003-dtmapi-full-boundary-audit.md`

## Objective

Preserve the verified Batch 2/3 results, independently audit Batch 4 source and player-package boundaries, correct any lifecycle/Catalog overclaim, and leave a durable admission gate before Batch 5.

## Changed Files

- Added the detailed code review above.
- Reopened `docs/updates/2026/20260715-0019-batch4-qa-host-extraction.md` as `in-progress`, appended the post-completion acceptance correction and replaced the false final completion conclusion with corrective G8 follow-up.
- Changed `tools/release/dtmapi-product-catalog.json` from completed/no-debt to `PartialOptionalHostWithPlayerFixtureDebt` with the measured production scenario debt and missing final no-receipt gate.
- Updated `tools/scripts/check-product-catalog.ps1` to assert the truthful interim state rather than mechanically enforcing the prior completion string.
- Updated the July Update ledger and added a short resolution link to the earlier Batch 2/3 entry review.

## Findings Applied

- Batch 2 and Batch 3 remain complete.
- Batch 4 has a correct optional-host loader, dependency direction, removed file poll and QA-free five-DLL package.
- Production GameBridge nevertheless contains 25 QA host/fixture files and 12,733 physical lines, 91.1% of the old 13,981-line embedded Smoke baseline. The optional QA source contains 15 files and 3,227 lines.
- `DtmApiRuntime` exposes two public fixture controls, including owner-root suppression that removes input/event/config roots.
- Final G7 staged-QA runs do not replace a final-HEAD no-QA/no-receipt player run.
- Existing Catalog/package gates exclude QA artifacts and reverse project references but do not detect executable QA case bodies inside GameBridge.

## Validation

- Before the documentation/Catalog correction, clean committed HEAD `7e62d050` passed `tools/scripts/test.ps1 -Configuration Release` in 582.1 seconds with zero build warnings/errors and all Unit/QA Unit/ABI/Doctor/Author SDK/Catalog/release/transaction/evidence/document gates passing.
- A fresh Runtime-only package from those outputs passed the DTMAPI Workshop release-audit workflow under Windows PowerShell 5.1 from space/non-ASCII temporary paths. Ten scripts parsed and the eight-case install/check/collect/uninstall matrix reported `Blockers: 0`.
- The candidate package contained exactly five production DLLs and no QA artifact. This validates package exclusion, not semantic removal from GameBridge.
- After the correction, `check-product-catalog.ps1` passed under PowerShell 7 and Windows PowerShell 5.1 (`26/11/21/46`), document governance passed 5,114 checks, and `git diff --check` passed with line-ending notices only.
- No game/runtime run was performed; the final post-G7 no-QA/no-receipt Steam gate remains pending.

## Rollback

Revert this Update, the new Review, Update 0019 lifecycle/addendum, Catalog state/checker assertion, July ledger row and the old-review resolution link together. Do not revert the Batch 2/3 code or the successful Batch 4 optional-host infrastructure/evidence merely to restore the former completion wording.

## Follow-Up

Continue Update 0019 with corrective G8A-G8E from the primary Review. Do not enter Batch 5 until production retains only narrow neutral native seams, the public Core fixture controls are removed, semantic source/IL absence gates pass, and final no-QA plus QA-enabled runtime matrices are recorded.

## Resolution

Resolved at the G8 decision point by the 2026-07-17 corrective closure in Update `20260715-0019`. The interim Catalog value `PartialOptionalHostWithPlayerFixtureDebt` remains recorded above as the truthful review-time state; the then-current Catalog state was `CompletedOptionalHostSemanticBoundaryVerified` after the four-file/608-line semantic boundary, public-Core cleanup, full Release and package audit, final no-QA player lane `GAME-SMOKE/20260717-003431`, and staged QA/eleven-product lanes `004127`, `004235`, `004342`, and `004504` all passed.

The later G9 closure is owned by [Update 20260717-0002](20260717-0002-batch4-source-boundary-reopen.md), which supersedes that G8-era current-state sentence with schema-5 ownership enforcement and the exact no-QA/staged-QA UI matrix. Batch 5 was not entered; ISSUE-010/ISSUE-011 and the independent GC ladders remain open.
