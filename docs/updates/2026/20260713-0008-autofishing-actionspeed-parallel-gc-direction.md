# 20260713-0008 AutoFishing And ActionSpeed Parallel GC Direction

## Metadata

- Update ID: `20260713-0008`
- Date: 2026-07-13
- Lifecycle Status: `verified`
- Validation Level: `docs, source`
- Runtime Validation: `not-required`
- Related Issue State: `open`
- Source: user refined only the GC direction after the formal sixth-round closure and explicitly left every other decision unchanged

## User Direction In Supplied Order

### 1. First Priority And Common Risk

AutoFishing and ActionSpeed remain the first GC priority. They operate in separate gameplay domains rather than competing for one Animator: AutoFishing advances Fishing Ready/Cast/Pull; ActionSpeed advances Tool/Interact/Eat/Continuous-use. The common risk is that each independently shortens animation time and accelerates action-state progression, potentially amplifying the same class of Unity/Mono GC pressure.

Analysis immediately following issue 1: the focused Review now uses two parallel single-domain ladders. “Two systems conflict” is removed as a default causal hypothesis; current source has no same-Animator evidence.

### 2. Comparable Speed And Lifecycle Levels

Each domain is compared independently at native 1x, enabled without acceleration, common player acceleration, high multiplier, disable recovery and title-cycle levels. Results use both per-action and per-real-minute denominators.

Analysis immediately following issue 2: native 1x separates game/Runtime cost; enabled-without-acceleration separates product/automation ownership from speed; common/high multipliers establish the pressure curve; disable/title levels establish restoration. ActionSpeed repeats the ladder for Tool, Interact, Eat and Continuous-use subdomains rather than treating one tool as representative of the whole product.

### 3. Primary Questions

The campaign tests whether action throughput amplifies short-lived allocation, whether animation events/completion callbacks add work, whether each action leaves a small residual magnified by speed, and whether high-speed Animator/native state transitions increase Unity native or Mono pressure.

Analysis immediately following issue 3: this replaces the former A-H combined-workload emphasis. Per-minute growth without per-action growth is classified as throughput amplification; per-action growth or structural stair-steps require a retained-state/root investigation. Unavailable Unity Mono allocation counters remain `Blocked`/`null` rather than zero.

### 4. Conditional Animator Arbitration Only

Same-Animator ownership is observed as topology data only. An arbitration design is added only if a real overlapping Animator/state is detected.

Analysis immediately following issue 4: installed coexistence may receive one final lifecycle integration smoke after both independent ladders are classified, but it cannot be used to predeclare a shared Animator conflict.

## Unchanged Decisions

U1, V1 revised, W1, the ten-gate DTMAPI 0.5.5 baseline, external compatibility scope, release waves, Product/Author SDK/QA boundaries and all non-GC specialist routing remain unchanged.

## Summary

Replaced the combined-conflict framing with two source-aligned, comparable speed ladders while retaining AutoFishing and ActionSpeed as the first gameplay GC priority. No Runtime, Hook, API, Mod, package, game, save or Workshop behavior changed.

## Reviews And Issue

- `docs/reviews/code/2026/20260713-0013-autofishing-actionspeed-active-gc-release-gate.md`
- `docs/reviews/code/2026/20260713-0012-major-update-sixth-decision-docket.md`
- `docs/reviews/code/2026/20260712-0003-dtmapi-full-boundary-audit.md`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`

## Changed Files

- revised the focused GC Review hypotheses, matrix and release gates;
- synchronized the sixth-round GC summary and full-audit campaign route;
- corrected the ISSUE-010 active-gameplay track;
- linked the prior closure Update to this refinement;
- added this Update and its monthly ledger row.

## Validation

- user issue order and analysis are preserved in text;
- AutoFishing and ActionSpeed source domains remain correctly separated;
- `tools/scripts/check-doc-governance.ps1`: passed (`4395` checks);
- `git diff --check`: passed (only existing working-copy LF-to-CRLF warnings were reported);
- focused GC records have no trailing whitespace;
- no build/runtime validation is required because this is a documentation-only direction correction.

## Runtime Evidence

Not run. No game process was launched, no runtime lock was acquired, and no game, local `MODS`, install, upload or Workshop state was touched.

## Rollback

Remove this Update/monthly row and revert only the parallel-ladder wording in the focused GC Review, ISSUE-010, sixth-round summary and full audit. No source, runtime, package or save rollback is required.

## Follow-Up

When the GC implementation begins, create its own in-progress Update and instrument/run the two independent ladders before any coexistence smoke. Do not add Animator arbitration without observed owner overlap.
