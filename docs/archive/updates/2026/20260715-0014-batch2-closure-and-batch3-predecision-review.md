# 20260715-0014 Batch 2 Closure And Batch 3 Predecision Review

## Metadata

- Update ID: `20260715-0014`
- Date: 2026-07-15
- Lifecycle Status: `verified`
- Validation Level: `docs,source`
- Runtime Validation: `not-required`
- Related Issue State: `open`
- Area: review/major-update/batch2/abi/lamp/smoke/author-sdk/doctor/decisions
- Source: user stated Batch 2 was complete, requested a closure audit and Lamp-decision decomposition, and requested the remaining Batch 3 decisions for advance selection

## Scope

- record the current Batch 2 implementation, automatic, ABI, Unity/Steam, governance, and release layers separately;
- preserve the supplied screenshot's Lamp/Smoke findings as text;
- verify and record the three Smoke implementation P1s plus the merged evidence-retention failure;
- split the retained Lamp question into binary shape, provider behavior, runtime effect, warning lifecycle, and future feature decisions;
- print only the still-open Batch 3 Author SDK choices while preserving already frozen A2/B0-B3/D1/G1/I1 boundaries and later AnimalPack/QA/product work.

This is a review/documentation update. It does not implement or select any Lamp/SDK option, alter Runtime/API behavior, acquire the Runtime lock, launch the game, write local official/Workshop packages, or close Batch 2.

## Changed Files

- `docs/reviews/code/2026/20260715-0006-batch2-closure-and-batch3-predecision-review.md`: owns the closure audit, actionable findings, Lamp decomposition, Batch 3 option set, fact-first questions, and acceptance gates.
- `docs/updates/2026/20260715-0014-batch2-closure-and-batch3-predecision-review.md`: owns this documentation lifecycle.
- `docs/updates/INDEX-2026-07.md`: routes this Update from the monthly ledger.
- `docs/debug/evidence-retention-allowlist.json`: regenerated after the new Update/Review records so the tracked evidence-source inventory remains current.

## Validation

- Current Git branch/HEAD/status and all Batch 2 Update metadata were inspected.
- The exact retained ABI JSON, current candidate, retained defaults and first-party/current-visible external MemberRefs were inspected without copying retained DLLs into Git.
- The three Smoke P1s were confirmed in current source and their acceptance boundaries were recorded.
- The first `tools/scripts/test.ps1 -Configuration Release` audit run reached `DTMAPI.UnitTests: OK` but failed because Update 0013 was absent from the tracked evidence-retention allowlist; concurrent UnitTests also caused transient output-lock retry warnings. The failure is retained as a Batch 2 closure finding.
- The allowlist was regenerated after this Review/Update was added; its check passed with 379 source files, 694 smoke identities and 62 runtime identities.
- A second no-concurrency `tools/scripts/test.ps1 -Configuration Release` replay passed in 710.9 seconds with zero build warnings/errors, `DTMAPI.UnitTests: OK`, both eleven-case Runtime transaction hosts, both developer-install transaction hosts, Catalog `26/11/21/45`, the five-Runtime/eleven-product/Oil Release contract, and document governance.
- The default Release entry point does not execute the opt-in exact retained ABI test and cannot clear its 92 Lamp deletions; it also creates no Unity/Steam/player evidence.
- No final 0.5.5 candidate Workshop package exists yet, so the subscription-package stress matrix required by the Workshop release-audit workflow was not run against the unrelated old 0.5.2 subscription artifact.
- `tools/scripts/check-doc-governance.ps1` passed 4,948 checks.
- `git diff --check` passed; output contained only existing LF-to-CRLF working-copy warnings.
- No game/runtime/Workshop path was changed and no smoke-matrix row was created.

## Rollback

Remove this Review, this Update and its July ledger row, then regenerate the evidence-retention allowlist. Rollback does not change any Batch 2 Runtime/API/test implementation or its open Updates.

## Follow-Up

- The user may select the recommended Lamp bundle `L-A1/L-B1/L-C1/L-D1/L-E1` or explicitly reopen the incompatible 0.5.5 deletion/rebuild direction.
- The user may answer the Batch 3 set with `SDK-A1/SDK-B1/SDK-C1/SDK-D1/SDK-E1/SDK-F1/SDK-G1/SDK-H1/SDK-I1` or choose individual alternatives; the prefix prevents collision with earlier frozen A-I labels.
- Batch 2 implementers must repair the recorded P1s, implement the selected Lamp boundary, rerun exact ABI and clean merged Release gates, then acquire the Runtime lock for the retained AutoFishing and normal-Steam player matrices before closing Updates 0004/0011/0012/0013.
- Follow-up: Review `20260715-0007-decision-escalation-policy-and-product-nodes.md` adopts SDK A1-I1 as evidence-gated engineering defaults, so the former SDK ballot no longer requires a user reply; alternatives are retained only as reversal paths when validation fails.
