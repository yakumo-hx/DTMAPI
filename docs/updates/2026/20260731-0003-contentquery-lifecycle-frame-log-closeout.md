# 20260731-0003 ContentQuery、生命周期诊断、帧驱动与轻量日志收口

## Metadata

- Update ID: `20260731-0003`
- Date: 2026-07-31
- Lifecycle Status: `implemented`
- Validation Level: `docs,source,unit`
- Runtime Validation: `not-run`
- Related Issue State: `open`
- Area: Core/ContentQuery/official-content/lifecycle/Bootstrap/frame-driver/logging/diagnostics
- Source: user selected C1, lifecycle diagnostic-only minimum repair, F1 and G1; selected thin official ContentQuery, simple lifecycle decisions, F3 and G2 for the next Runtime version

## Scope

This Update owns four bounded current-version changes and one roadmap:

1. scan only enabled official inputs, accept official line comments for
   third-party content and isolate their malformed files;
2. remove the internal `SecondSaveLoaded` diagnostic classification without
   changing public lifecycle dispatch or product behavior;
3. distinguish a global Unity callback pause from an isolated InputSystem
   stall using relative callback progress;
4. make Lite continuous object-graph evidence the player default while
   preserving complete Error evidence;
5. freeze the next-version direction for a thin native official query layer,
   individually decided lifecycle simplification, F3 and G2.

Manager information architecture, ProductNative state machines, save
transactions, owner cleanup, public ABI changes, game installation and package
freezing are out of scope.

## Source Authorities

- [`PROJECT.md`](../../../PROJECT.md)
- [`20260731-0001 manual QA review`](../../reviews/manual-qa/2026/20260731-0001-autofishing-manager-player-ui-and-loop-stall.md)
- [`ISSUE-010`](../../debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md)
- [`public API matrix`](../../api/public-api-matrix.md)
- [`runtime simplification roadmap`](../../planning/20260731-runtime-query-lifecycle-driver-logging-roadmap.md)

## Implemented Behavior

### ContentQuery C1

- Disabled and unknown official sources exit before info, image or item reads.
- Third-party official/Workshop JSON uses a dedicated reader that removes only
  line comments outside strings. DTMAPI authority JSON remains strict.
- An unreadable third-party enabled file increments a bounded skipped-input
  diagnostic and does not reject other valid enabled sources.
- Active DTMAPI ContentPack input remains strict and can still reject the whole
  candidate so its prior last-good publication stays authoritative.
- Disabled package status remains available through Mod diagnostics; item-level
  disabled rows are no longer constructed.

### Lifecycle minimum repair

- Every normal load is internally classified as `SaveLoaded`.
- `SecondSaveLoaded`, its policy and its before/repeat diagnostics are removed.
- Save generation opening, public event dispatch, hooks, coordinator and product
  callbacks are unchanged.

### Frame-driver F1

- The health pump captures callback counts before posting to Unity and compares
  them on execution.
- Global callback pauses leave subscriptions untouched and produce no Warning.
- Only sibling progress plus an unchanged stale InputSystem count selects
  InputSystem recovery. Successful recovery is Info; failed recovery is Warning.
- Missing subscription retry keeps the existing bounded cadence.

### Logging G1

- Ordinary Runtime defaults object-graph snapshots to Lite; QA may explicitly
  select Full.
- Lifecycle contract summaries are written only for a phase/title boundary,
  status transition or new diagnostic instead of every Hook observation.
- Lite never filters Warning/Error. Unit evidence requires Error log and
  retained diagnostics to contain the causal context, outer/inner exception and
  captured stack without Full snapshots.

## Changed Files

- `src/DTMAPI.Core/Json/OfficialJsonCompatReader.cs`
- `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.Core/Runtime/LifecycleBoundaryContractService.cs`
- `src/DTMAPI.BepInExBootstrap/FrameDriverHealthPolicy.cs`
- `src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs`
- `src/DTMAPI.Abstractions/Helpers.cs`
- `tests/DTMAPI.UnitTests/Batch5ContentGenerationTests.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/api/public-api-matrix.md`
- `docs/design/mod-owner-lifetime-contract.md`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/planning/README.md`
- `docs/planning/20260731-runtime-query-lifecycle-driver-logging-roadmap.md`
- `docs/reviews/manual-qa/2026/20260731-0001-autofishing-manager-player-ui-and-loop-stall.md`
- `docs/updates/2026/20260731-0003-contentquery-lifecycle-frame-log-closeout.md`
- `docs/updates/INDEX-2026-07.md`
- `tools/release/dtmapi-product-catalog.json`

## Validation

- `tools/scripts/build.ps1 -Configuration Release -SkipTests`: PASS, zero
  warnings/errors for game-loaded Runtime projects.
- Complete `DTMAPI.UnitTests`: PASS. New coverage proves enabled comment JSON,
  `//` inside strings, disabled/unknown no-read behavior, one bad enabled native
  source isolated beside two valid sources, five repeated load/title phases,
  five resource save generations, global-pause versus isolated-stall decisions,
  bounded missing-subscription retry and Lite Error evidence.
- `DTMAPI.QaUnitTests`: PASS.
- `tools/scripts/check-product-catalog.ps1`: PASS (`products=27`,
  `public=11`, `workshop-items=22`, `api-rows=48`).
- `tools/scripts/check-doc-governance.ps1`: PASS (`6101` checks).
- `tools/scripts/check-test-artifact-governance.ps1`: PASS after managed
  test-session cleanup.
- `git diff --check`: PASS apart from the repository's existing line-ending
  normalization notices. No game has been launched by this Update yet.

## Runtime Evidence Boundary

The preceding user manual run proves AutoFishing behavior under the prior
candidate and supplied the warning/log evidence used by the Review. It does
not prove this corrected binary. The current worktree retains the shared manual
test lock; installation and player hand test remain the next runtime step.

## Rollback

The four slices are source-only and do not write player saves. Rollback may
restore the former official file scan, second-load diagnostic label, wall-clock
health test or Full default independently. Restoring the C1 scan intentionally
reintroduces disabled parsing and whole-candidate rejection; restoring the old
health test intentionally reintroduces the startup false warnings.

## Follow-Up

- Run the bounded player checks listed in the roadmap against the installed
  committed candidate.
- In the next Runtime version, do not extend the current ContentQuery scanner;
  design the thin official final-table/winning-source layer first.
- Discuss each lifecycle problem separately before implementation; no L3-style
  parallel state machine is authorized.
- Replace F1 with F3 only after input/focus/title coverage proves InputSystem's
  driver role redundant.
- Implement G2 as a separate log-level audit while preserving the Error evidence
  invariant.
