# 20260719-0002 - Batch 5 GC Ladder SaveLoaded And Evidence-Path Runner Failure

- Date: 2026-07-19
- Status: corrective implementation required before replay
- Severity: P1 acceptance-infrastructure blocker; no product crash, save corruption, or source-state loss observed
- Owning Update: [20260718-0003 Batch 5 Event, Demand, Content Invalidation, Lifecycle And Performance Boundary](../../../updates/2026/20260718-0003-batch5-event-demand-content-lifecycle-performance.md)
- Failed ladder: `docs/debug/evidence/BATCH5-GC-LADDER/20260719-041457-94325d1a`
- Failed smoke: `docs/debug/evidence/GAME-SMOKE/20260719-041458`

## Observed Result

The first formal `Both`, 600-second Batch 5 GC stage, `ActionSpeed-L0-Tool`, completed its real third-save workload and wrote a schema-2 runtime metric receipt with `Status=completed`, `WorkloadCompleted=true`, `CompletedUnits=10`, `ActiveDurationSeconds=600.0092886`, `ActiveWindowSatisfied=true`, `BehaviorReceiptKind=native-control-active-window`, all eleven required metric categories available, and `ForcedGc=false`.

The outer smoke nevertheless returned exit code `1`. Its `result.json` contains only two top-level failed values: `RunStatus=Failed` and `SaveLoaded=Failed`. `ProcessExited`, `ForcedClose`, `NoFatalInstanceWindow`, `QaHostLifecycle`, `QaHostCleanup`, `OfficialModProfileGate`, `LocalAuthorSourceState`, and `PlayerSaveRestored` all passed. The Runtime log independently records `SaveLoaded hook dispatched` and the native load continuation for the third save (`slot=2`, the zero-based native slot).

After the smoke failure, the GC ladder attempted to discover the evidence directory from human-readable error output. It accepted a whitespace-only `Evidence:` match and called `System.IO.Path.GetFullPath` with an empty trimmed value. That secondary exception replaced the authoritative stage failure in `ladder-plan.json` and left `SmokeEvidencePath`, `RuntimeMetricsPath`, and the local-source load receipt unset even though the evidence and runtime metric files existed.

The ladder stopped before the remaining 29 stages, released the shared Runtime lock, and ran its `finally` restoration. The ActionSpeed Author source-state receipt reports `RestoredExact=true`, `AppliedStateUnchanged=true`, `SourceTreeUnchanged=true`, and `Passed=true`. Post-failure checks found no `DolocTown.exe` process and reproduced the frozen ActionSpeed and AutoFishing tree digests exactly.

## Root Cause

`run-game-smoke.ps1` includes `$batch5GcLadderEnabled` in `saveLoadedRequested`, so the final result requires a successful SaveLoaded observation. Its ordered SaveLoaded wait chain has branches for the older smoke scenarios but no branch for a Batch 5 GC ladder stage. The optional QA participant can therefore load the save, run the entire workload, close, and exit successfully while the outer `$saveLoadedOk` value remains its initial `false` value.

`run-batch5-gc-ladder.ps1` has a second contract defect. It scrapes the display-oriented `Evidence:` suffix from merged child stdout/stderr without requiring a non-whitespace capture, an existing directory, or a machine-readable marker. PowerShell error formatting may split the displayed message at `Evidence:`, making the regex match whitespace and hiding the primary smoke result behind an empty-path exception.

These are runner/receipt projection defects. The retained evidence does not show an ActionSpeed L0 behavior failure, a GC crash, or an Author transaction failure. It also is not accepted as a formal ladder pass because the authoritative outer stage never bound and validated the runtime receipt.

## Required Correction

1. Add a distinct Batch 5 GC SaveLoaded wait branch which observes the neutral Runtime `SaveLoaded hook dispatched` receipt before stage-specific terminal processing.
2. Make `run-game-smoke.ps1` emit one machine-readable evidence-path marker after writing its result and before either success or failure exit.
3. Make the ladder prefer that marker, reject empty/nonexistent/out-of-bound paths, retain the child smoke exit code, and report an explicit evidence-discovery failure without calling `GetFullPath` on an empty value. A legacy human-readable fallback may remain only if it has the same validation.
4. Add source and executable parser tests covering a failed smoke with a valid marker, a split/whitespace-only `Evidence:` line, missing marker/path, and the Batch 5 SaveLoaded routing branch.
5. Re-run the formal ladder from stage one. Do not promote the retained `041458` runtime metric into a passed ladder result.

## Replay Gate

Before replay, Windows PowerShell 5.1 parsing, focused runner tests, the GC transaction/parser test script, UnitTests, and `git diff --check` must pass. Replay must start with a free Runtime lock, no game process, the two externally frozen tree digests, and the exact restored Author source state. A passing stage must bind its `GAME-SMOKE` evidence path, runtime metric path, OfficialLocal load receipt, terminal behavior receipt, all required metric categories, exact source restore receipt, and clean process/profile/save state.
