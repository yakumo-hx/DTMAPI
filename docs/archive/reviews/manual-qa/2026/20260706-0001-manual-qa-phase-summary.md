# Manual QA Phase Summary - 2026-07-06

Status: docs-only synthesis

Sources:

- `docs/reviews/manual-qa/README.md`
- `docs/reviews/manual-qa/2026/*.md`
- `docs/debug/phase-summary-20260705.md`
- recent update records through `20260705-0012`

## Core Rule

Manual QA records are not optional commentary. They preserve user-observed behavior, root-cause analysis, rejected hypotheses, and evidence gaps that should feed a future goal file before implementation.

## Phase Map

| Phase | Date range | Main feedback families | Current lesson |
| --- | --- | --- | --- |
| First feature correction wave | 2026-06-05 to 2026-06-06 | Mine, Animal, Y console, ConfigMenu, MoreEquipmentSlots, MoreSaves | Newer manual QA can invalidate older smoke success. Preserve the user's issue order. |
| Console/native baseline wave | 2026-06-06 | Official console comparison, Y console input isolation/layout, new mods | Native behavior should be used as baseline when available. |
| Camera and playable-view wave | 2026-06-07 to 2026-06-10 | CameraZoom failure, CameraView gate | Visual/playable APIs need manual movement/background/sync checks. |
| Refactor and animal UI wave | 2026-06-11 | Animal flicker, UI state, code review gates | Repeated UI flicker needs root-cause review before new implementation goals. |
| Fishing/save/title wave | 2026-06-12 to 2026-06-13 | AutoFishing, SaveSlots, title/load behavior | Smoke can help but must be tied to native loop and clean lifecycle evidence. |
| Equipment/mushroom/shield/vehicle wave | 2026-06-14 to 2026-06-15 | Equipment storage, custom motor, multi motor behavior | Vehicle path was retired; equipment needs native storage and cross-save checks. |
| ActionSpeed/player diagnostics wave | 2026-06-16 to 2026-06-17 | Movement speed, ActionSpeed, Y console responsiveness, collect-log failure | Edge interactions need manual third-save repetition and installer diagnostics need player-like scripts. |
| Custom animal behavior wave | 2026-06-29 to 2026-07-01 | Shell Crab/Hatch eat/sleep/voice behavior | Overnight/sleep behavior and native voice leakage need per-template manual evidence. |

## Recurring Bug Families

| Family | What the bug is | Current state | Technical debt |
| --- | --- | --- | --- |
| Animal UI and state | Panels or progress flicker, hidden product fixture mismatch, animal sleep/eat/voice inconsistencies | Some later animal/content-pack paths are verified, but family remains sensitive | Need per-animal/template manual evidence and avoid declaring UI success from smoke only. |
| Y console input | Open/toggle/right-click/key input can diverge from native console behavior or human input | Improved with input fallback and diagnostics, still a diagnostic/tooling surface | External smoke injection is flaky; manual input remains important. |
| Mine/machine behavior | Preview, production timing, electricity, recipe, energy, and persistent placed-machine behavior | Earlier forced smoke was not enough | Needs native pass-time/lifecycle coverage for placed machines. |
| MoreEquipmentSlots/storage | Too many config states, cross-save pollution, duplicate risks, storage alignment | Experimental/high risk | Needs native storage, unsaved re-entry, save/load, hat/accessory/shield checks. |
| Camera | Live zoom path failed; CameraView is more plausible but still experimental | CameraZoom retired/failed; CameraView gated | Needs playable third-save movement/background/sync evidence. |
| AutoFishing | Native loop/animation/toggle behavior needed repeated review | Improved by later lifecycle/soak evidence | Continue to separate smoke helper success from public API stability. |
| ActionSpeed | Some native interaction speedups work, but wells/planting/animal connector cases were intermittent | Experimental | Needs repeated manual gate for low-row seed/fertilizer/film, already-fertilized/crop-film, animal doors/connectors, sprinklers, grow lights. |
| Installer/player logs | Player package failures involved PowerShell host, parser, and collector behavior | Many temp/package matrices were added | Still needs real player environment caution and package parity checks. |

## Handoff Rules

- Keep the user's numbered issue order when converting feedback into a review or goal.
- Translate screenshot-only evidence into text.
- Attach analysis immediately after each issue so a compacted context can resume safely.
- For repeated lifecycle, UI flicker, stale-state, hook, input, save/load, vehicle, machine, or official-content issues, write or update a review record before implementation.
- Do not claim a manual issue is solved without clean restart, game evidence, relevant logs, and regression-matrix updates when applicable.

## Next Use

Before writing a goal from manual feedback, read:

1. the newest manual QA review for that feature family
2. `docs/workflows/codex-feedback-to-goal.md`
3. `docs/goals/README.md`
4. `docs/reviews/README.md`
5. the matching debug/API/Hook Map record when the issue touches runtime, API, or hooks
