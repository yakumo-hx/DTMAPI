# 20260719-0009 - Batch 5 AutoFishing Performance Observer Effect

- Date: 2026-07-19
- Status: corrective implementation and formal replay required
- Severity: P1 performance-acceptance blocker; no product crash, save corruption, or source-state loss observed
- Owning Update: [20260718-0003 Batch 5 Event, Demand, Content Invalidation, Lifecycle And Performance Boundary](../../../updates/2026/20260718-0003-batch5-event-demand-content-lifecycle-performance.md)
- Affected formal root: `docs/debug/evidence/BATCH5-GC-LADDER/20260719-115313-0b93d47f`

## Observed Result

The fifth-save AutoFishing L0-L5 run completed all six 600-second stages. Its behavior, official-vitals, fifth-save byte restoration, OfficialLocal source transaction, title cleanup, no-fatal-window, and process-exit receipts remain valid.

The six resource-snapshot counters nevertheless increased by 35,946 to 36,010 builds per stage, approximately 60 builds per second. This is not a product-side zero-demand or fishing-lifecycle result. The QA fixture calls `CaptureAutoFishingPerformanceSnapshot()` on the frame path, and that capture reads both `runtime.ResourceLifecycleSnapshot` and `runtime.RuntimeMemoryOwnerRootCount`. The first read constructs, sorts, and publishes a complete resource diagnostic snapshot; the second constructs Input, Event, and multiple CustomEntity snapshots. `AutoFishingPerformanceFixtureSnapshot` is also a reference type allocated for every observation.

The 30-second trend sampler then reads another complete resource snapshot while trying to measure the resource snapshot-build counter. The sampled value therefore includes work caused by the measurement path itself.

## Root Cause

The QA boundary combined two different data classes in one per-frame transport object:

- action progress and primitive counters needed to recognize fishing state transitions on the frame path;
- diagnostic ownership/resource/event/input/demand snapshots needed only at the configured performance sample cadence.

No runner gate bounded the observer's own resource snapshot builds or required the top-level and trend counters to agree. Consequently the formal runner accepted a terminal behavior receipt even though its GC/memory observer materially changed the measured workload.

This is an observer-effect defect in QA and acceptance infrastructure. The retained evidence does not prove a product memory leak, and its Mono/GC trends cannot prove Batch 5 performance acceptance.

## Rejected Interpretations

- The roughly 60 Hz snapshot count is not evidence that ordinary AutoFishing rebuilds the resource ledger each frame; the call graph identifies the QA observer as the direct caller.
- Flat owner/event/input/demand trends do not make the memory measurements clean; a stable but repeatedly allocated diagnostic snapshot can still perturb GC and heap behavior.
- Successful six-level behavior and cleanup receipts do not upgrade the polluted performance trends into a formal GC pass.
- The evidence root must not be deleted or relabelled as wholly failed. It remains authoritative for the bounded behavior, fifth-save, official-command, restoration, and exit facts listed above.

## Required Correction

1. Expose the resource record count and snapshot-build count as locked, allocation-free scalar reads. A scalar read must not increment the build count; one explicit `GetSnapshot()` must increment it exactly once.
2. Remove resource, owner-root, and other diagnostic snapshot construction from the per-frame AutoFishing observation path. Keep per-frame transport value-typed and limited to action progress counters. Capture ownership/event/input/resource/hook/demand data only at `SampleSeconds` cadence.
3. Make ActionSpeed's sampled path use the same resource scalars so it does not increment the counter it measures.
4. Make the outer ladder fail closed when resource counter fields are absent, non-monotonic, inconsistent, under-sampled, or over budget. Persist observed delta, rate, budget, sample expectation, and pass/fail in each `stage.json`.
5. Add source, unit, and runner tests for 10,000 allocation-free scalar reads, a 60 Hz simulated observation window with cadence-bounded domain capture, forbidden hot-path snapshot calls, and polluted/missing/inconsistent counter fixtures.
6. Rebuild/install the corrected candidate and rerun AutoFishing L0-L5 from fifth-save snapshots. Preserve the `115313` root as the pre-correction diagnostic checkpoint.

## Replay Gate

A replacement formal root may be accepted only when each stage still satisfies its existing behavior/vitals/save/source/cleanup/exit contracts and additionally proves:

- the 600/30 run contains cadence-complete ordered memory samples (normally 21, bounded fail-closed against the actual elapsed duration);
- `ResourceRecordCount` and `ResourceSnapshotBuilds` are present from start through end;
- the top-level snapshot-build delta equals the trend delta and is non-negative;
- the observer budget is below 25 snapshot builds for the 600-second stage;
- no per-frame fixture source reads `ResourceLifecycleSnapshot` or `RuntimeMemoryOwnerRootCount`.

Until that replay passes, Batch 5 AutoFishing is behavior-verified but performance-in-progress. ISSUE-010 remains open, and no zero-allocation or leak-free conclusion is authorized.
