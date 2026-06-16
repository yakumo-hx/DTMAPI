# Goal: ActionSpeed Bottom-Layer Rebuild

## Objective

Rebuild the ActionSpeed native-owner layer instead of patching individual animation symptoms.
This goal is a future handoff created from the 2026-06-16 player-feedback review; it is not part of the current lightweight logging/console/movement/AnimalViewer fix.

## Source Facts

- Player feedback reports that well bottle-fill acceleration only works reliably when an empty bottle is available/selected, and planting acceleration can intermittently fall back to native speed.
- Current `IActionSpeedApi` is Experimental and spans many native states: tool, interact, eat/drink, continuous item use, bottle fill, planting, harvest, resin, and vegetation.
- Prior API review already warned that ActionSpeed reaches multiple native owners and must not be treated as one global animation-speed switch.
- Current review record: `docs/reviews/manual-qa/2026/20260616-0001-player-log-diagnostics-console-speed-animal-review.md`.

## Required Reading

- `AGENTS.md`
- `PROJECT.md`
- `docs/planning/DolocTownModdingAPI.md`
- `docs/planning/Debug.md`
- `references/README.md`
- `docs/workflows/codex-api-rebuild.md`
- `docs/reviews/api/2026/20260607-0005-native-responsibility-api-audit.md`
- `docs/reviews/api/2026/20260607-0006-native-responsibility-method-audit-index.md`
- `docs/reviews/manual-qa/2026/20260616-0001-player-log-diagnostics-console-speed-animal-review.md`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`

## Native-Owner Questions

- Which native state owns well interaction with and without a selected empty bottle?
- Which native responsibility owns bottle-fill completion, inventory cost/output, and animation duration?
- Which native state(s) own planting, seed/film/fertilizer placement, and repeated planting input?
- Which paths should be accelerated by animator speed, which by timer/tick speed, and which must remain native speed?
- How should multiple speed providers resolve conflicts without last-writer surprises?

## Intended API Result

- Keep `IActionSpeedApi` Experimental.
- Do not expose raw `AgentState*`, `BodyController`, `ItemBottle`, `PlantBasin`, Unity animator, or decompiled game types.
- Prefer a native-stage strategy model if the current option set cannot express reliable owner boundaries.

## Acceptance Gates

- Unit coverage for option normalization and provider precedence.
- Third-save smoke for well fill with selected empty bottle, well fill with empty bottle only in backpack, planting, harvest, and ordinary tool paths.
- Logs must prove the native owner used for each accelerated path.
- Clean exit evidence: no leftover `DolocTown.exe`, no fatal window, and no Steam waiting-for-exit regression.
- Update `docs/updates`, `docs/debug`, smoke matrix, hook map, and public API matrix.

## Non-Goals

- Do not fix this by broad per-frame animator writes.
- Do not promote `IActionSpeedApi` to Stable or StableCandidate.
- Do not include this work in the current player-log lightweight fix unless the user explicitly starts this goal.
