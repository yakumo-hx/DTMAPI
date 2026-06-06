# DTMAPI 0.3.1 Manual Regression And New Content Mod Goal

This archived snapshot preserves the former root task-ledger content. It superseded the prior 0.3.0 Zoom/Y-console utility-mod ledger at the time, while preserving that work in `docs/updates/2026/20260606-0005-030-yconsole-newmods-goal.md` and `docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md`.

## Current Status

- Current controlled runtime baseline before this round: DTMAPI `0.3.0` partial.
- Latest completed broad implementation baseline: DTMAPI `0.2.9` in `docs/updates/2026/20260606-0004-029-readme-implementation.md`.
- Latest 0.3.0 utility-goal closure: Zoom in `docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md`, advanced Y-console closure in `docs/updates/2026/20260606-0011-030-advanced-yconsole-closure.md`, Chest Locator Enhancer in `docs/updates/2026/20260606-0009-030-chest-locator-enhancer.md`, and Strong Planting Gun in `docs/updates/2026/20260606-0010-030-strong-planting-gun.md`.
- Current review source: `docs/reviews/manual-qa/2026/20260606-0004-031-regression-new-content-review.md`.
- This round targets `0.3.1` because the workspace already contains a 0.3.0 version bump. Treat all old 0.2.4-0.2.9 smoke evidence as orientation only, not proof for this new manual-regression round.

## Manual Feedback Header

- Time: 2026-06-06 +08:00.
- Scope: new manual regression repair plus official-JSON/DTMAPI API implementation for Oil, Mine, and More Equipment Slots.
- Primary regression items: AnimalHusbandryProgress, AutoFishing/ActionSpeed, Y Console save/teleport, and SecondMotor.
- New content/API items: OilMod, MineMod/Machine API, and MoreEquipmentSlotsMod.
- Forbidden: do not copy old DLKsmapi or DLK_SecondMotor; do not mutate third-party/Workshop content files; do not put ordinary DTMAPI mods under `BepInEx/plugins`; do not use old smoke as the final proof for new feedback.
- Version requirement: bump controlled runtime/package sources from `0.3.0` to `0.3.1` and record old/new versions plus evidence.

## Implementation Tasks

### Task A: Confirm Current State And Reopen Manual Regressions

- Run `git status` before edits.
- Read the required project/debug/review/update/hook/API/reference docs.
- Preserve the new manual feedback as current facts in a durable review/debug/update record.
- Do not use old `0.2.3` through `0.2.9` smoke rows as final proof.
- Bump DTMAPI from `0.3.0` to `0.3.1` across controlled version sources, installer normalization, and official-local manifests/metadata that this round installs.

### Task B: AnimalHusbandryProgress Bell UI And Color Config

- Fix animal-switch flicker where the native `心情` row can appear before the hidden-produce row.
- Keep hidden produce as an independent native-like progress row with larger readable text.
- Ensure `+`/Custom color swatch shows the editable hex input immediately through pending-preview config visibility.
- Ensure ordinary preset swatches hide the hex input.
- Verify in the third save with animal-panel logs and screenshot evidence.

### Task C: AutoFishing And ActionSpeed Config/Function

- Keep second-level config controls aligned on the same row for AutoFishing and ActionSpeed auto-fill.
- Keep AutoFishing `AutoCompleteMiniGame` and `SkipMiniGame` as separate behavior.
- Verify `AutoCompleteMiniGame=true` and `SkipMiniGame=false` reaches the native minigame and completes it through `FishingGameScrollBar`.
- Verify `SkipMiniGame=true` routes directly to the pull/result path.
- Verify fishing animation speed affects cast and pull/reel phases.
- Verify ActionSpeed auto-fill second-level UI and no-key bottle fill behavior.

### Task D: Y Console Save And Teleport Audit

- Keep arbitrary-location save as a visible Y Console action, using native `DolocAPI.SaveGame`.
- Do not re-enable save-then-immediate-load in the player-facing path.
- Export current teleport destinations to CSV for manual screening.
- CSV must include internal id, map/room, coordinates, current display name, suggested name, category/source.
- Verify in third save with Y Console screenshot/logs and a fresh CSV artifact.

### Task E: SecondMotor True Independent Vehicle

- Restore/confirm the official example texture/source strategy without globally replacing the original motor.
- Ensure original and second motors can be visible together without texture/state replacement.
- Ensure riding the second motor across a map boundary does not bring the original motor to the new entry.
- Ensure map boundary transition does not leave the player stuck.
- Approximate native fly-to-player summon animation as far as safely possible; record any remaining animation limitation explicitly.
- Verify with third-save vehicle smoke; manual visual crossing remains useful evidence.

### Task F: OilMod Official JSON And Runtime Drop

- Prefer official content JSON for the oil item.
- Active item id should be content-facing (`crude_oil`), with old `dtmapi_oil` treated only as a legacy note if present.
- Oil must have icon, localization, salable/buyable metadata, Y Console source/category metadata, and fuel value above the current highest base fuel value.
- Coal mining should have a small chance to produce oil through the safest precise route; if official drop extension is too broad, keep the GameBridge coal-resource hook and document that boundary.
- Mine output pool should be able to include oil.

### Task G: MineMod And DTMAPI Machine API

- Add/maintain a separate Mine equipment via official JSON, reusing the water-well model at 2x display without replacing the well.
- Make it craftable through the intended workbench/research route.
- Support the current user target of hybrid fuel/electric behavior: large fuel capacity, pure-fuel faster fuel use, electric mode consumes 10 power and uses less fuel.
- Produce minerals by game-time periods and catch up after native time skips.
- Support base-game minerals and mod minerals, including default probabilities and config/compatibility overrides.
- Keep fragile reflection/Harmony/Unity behavior in GameBridge; public API stays experimental and type-safe.

### Task H: MoreEquipmentSlots Safe Extra Slots

- Extend player equipment UI with extra slots that are clickable and hoverable.
- Preserve vanilla/default visual equipment slot behavior.
- Extra slots provide attributes/effects only and must not change hat/cosmetic appearance.
- No-save exit must not persist extra-slot mutations or duplicate items.
- Native save must persist extra slots only after the game save transaction completes.
- Disabling/uninstalling the mod must recover extra-slot items to backpack or another safe path; no silent deletion.

### Task I: Validation And Documentation Closeout

- Release build and unit tests pass with 0 errors.
- Third-save game smoke covers AnimalHusbandryProgress, AutoFishing, ActionSpeed, Y Console save/teleport, SecondMotor, Oil, Mine, and MoreEquipmentSlots.
- Capture logs/screenshots/CSV/report evidence for each player-visible feature.
- Exit check shows no leftover `DolocTown.exe` and no fatal duplicate-instance popup.
- Update `docs/updates`, `docs/debug`, `docs/debug/regressions/smoke-matrix.md`, `docs/hook-map/README.md`, and `docs/api/public-api-matrix.md`.

## Completion Standard

Only mark the goal complete when Tasks A-I are player-visible and genuinely verified in the third save, with build, game smoke, logs/screenshots/CSV, exit cleanup, and documentation complete.

Do not mark complete if any of these remain unverified or blocked:

- Animal hidden-produce row still flashes the native mood row before correction.
- AutoFishing auto-complete and skip-minigame are not separated in real gameplay.
- Y Console save or teleport CSV is not player-visible and evidenced.
- SecondMotor independent instance/map transition is not proven.
- Oil official JSON/runtime metadata or coal drop cannot be verified.
- Mine hybrid machine behavior or official-content shell is unsafe/incomplete.
- MoreEquipmentSlots recovery/no-duplication behavior is unsafe.
