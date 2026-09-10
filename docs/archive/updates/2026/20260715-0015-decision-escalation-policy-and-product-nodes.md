# 20260715-0015 Decision Escalation Policy And Product Nodes

## Metadata

- Update ID: `20260715-0015`
- Date: 2026-07-15
- Lifecycle Status: `verified`
- Validation Level: `docs`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Area: review/major-update/decisions/author-sdk/products/roadmap
- Source: user supplied a subtask return which reclassified SDK A-I as evidence-gated engineering defaults and authorized durable filing if the classification was reasonable

## Scope

- define a durable threshold for escalating implementation work into a user/product decision;
- adopt SDK A1-I1 as provisional engineering defaults with explicit validation and reversal triggers instead of a pending ballot;
- preserve the canonical Batch 0-8 implementation order while mapping the later genuine product decision nodes;
- qualify B1 reload as a capability-gated DTMAPI content operation rather than an unproven promise for every official-native JSON lane;
- route current implementers away from unnecessary global decision rounds without freezing unverified SDK 0.1.x details as long-term public promises.

This is a documentation/project-direction update only. It does not implement Batch 2 or Batch 3, alter public API or Runtime behavior, acquire the Runtime lock, launch the game, or modify local/Workshop packages.

## Changed Files

- `docs/reviews/code/2026/20260715-0007-decision-escalation-policy-and-product-nodes.md`: owns the decision-escalation rule, SDK defaults/gates, true decision nodes and route interpretation.
- `docs/reviews/code/2026/20260715-0006-batch2-closure-and-batch3-predecision-review.md`: receives a narrow follow-up pointer so its former SDK ballot is not mistaken for a still-open user vote.
- `docs/updates/2026/20260715-0014-batch2-closure-and-batch3-predecision-review.md`: records that the SDK-vote follow-up was superseded by the engineering-default policy.
- `docs/updates/2026/20260715-0015-decision-escalation-policy-and-product-nodes.md`: owns this documentation lifecycle.
- `docs/updates/INDEX-2026-07.md`: routes this Update from the monthly ledger.
- `docs/debug/evidence-retention-allowlist.json`: regenerated so the evidence-source inventory includes this Review and Update.

## Validation

- The supplied subtask return was compared with the canonical full-boundary work order, the latest progress/decision-node review and the Batch 2 closure/Batch 3 predecision review.
- The adopted classification preserves all frozen A2/B0-B3/D1/G1/I1, ownership, BepInEx, ABI, official-JSON, native-owner and AnimalPack boundaries.
- It also preserves the fifth-round Q1/R1/S1/T0 audio boundary: reviewed JSON `SoundKey -> WAV`, Manbo identity continuity, layered contract/backend maturity, C# API retirement and no current BGM promise.
- The decision timetable is explicitly non-ordering, and B1 reload is limited to content capabilities proven reloadable.
- `tools/scripts/check-doc-governance.ps1`: passed after the record and ledger update.
- The evidence-retention allowlist was regenerated and its check passed after these records were added.
- `git diff --check`: passed; any emitted line-ending messages were warnings rather than whitespace errors.
- No source build or runtime test was required because this update changes documentation/project direction only.

## Rollback

Remove Review 0007, this Update and its July-ledger row; remove the narrow follow-up notes from Review 0006 and Update 0014; then regenerate the evidence-retention allowlist. Rollback does not alter any Batch 2/3 source implementation.

## Follow-Up

- Current threads may continue with SDK A1-I1 as engineering defaults once Batch 2 closure permits Batch 3 to start.
- Any Update implementing a provisional default must record its assumption, validation gate, rollback and escalation trigger.
- The next user decision should normally wait for evidence at ActionSpeed 1.0.0 or Mine 1.0.0; AnimalPack remains a later dedicated product round.
