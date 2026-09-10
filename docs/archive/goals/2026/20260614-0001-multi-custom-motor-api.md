# 2026-06-14 Multi Custom Motor API Rebuild Goal

## Objective

Rebuild the motor/vehicle API and `SecondMotorMod` toward stable multi custom motorcycles: the first custom motor should mirror the official flying motor's player-visible behavior while remaining fully independent from the native original motor.

This goal is an API rebuild. Native-owner method-body review must happen before runtime changes. If the native owner cannot support a stable multi-motor contract, report blocker facts and keep the API Experimental or split the contract rather than claiming stability from a working mod demo.

## Target Version

Target DTMAPI version: from `0.5.1-alpha` / `0.5.1.0` to `0.5.2-alpha` / `0.5.2.0`.

If the code is already at `0.5.2-alpha`, do not bump again. If it is not `0.5.1-alpha` or `0.5.2-alpha`, stop and report a version blocker.

## Branch / Worktree

Implementation work is in `E:\Python_project\DTMAPI-multi-motor` on branch `codex/multi-custom-motor-api-20260614`, created from `Refactor` HEAD. Do not use the dirty `E:\Python_project\DTMAPI` worktree for this goal.

## Required Reading

- `AGENTS.md`
- `PROJECT.md`
- `docs/planning/DolocTownModdingAPI.md`
- `docs/planning/Debug.md`
- `references/README.md`
- `docs/workflows/codex-feedback-to-goal.md`
- `docs/workflows/codex-api-rebuild.md`
- `docs/goals/README.md`
- `docs/reviews/README.md`
- `docs/reviews/manual-qa/2026/20260614-0002-multi-custom-motor-api-review.md`
- `docs/reviews/api/native-owner-domains/06-flying-motor-vehicle-types.md`
- `docs/reviews/api/2026/20260607-0009-native-owner-special-audits/06-vehicle-motor-api.md`
- `references/doloc-town/research-notes/research-DolocTown-Motor-Vehicle-API.md`
- `docs/debug/issues/ISSUE-005-20260603-manual-qa-024-regressions.md`
- `docs/debug/issues/ISSUE-006-20260604-critical-manual-qa-025.md`
- `docs/updates/2026/20260603-0010-original-motor-appearance-restore.md`
- `docs/updates/2026/20260603-0020-second-motor-edge-transition-smoke.md`
- `docs/updates/2026/20260604-0002-025-critical-manual-qa-fixes.md`
- `docs/updates/2026/20260606-0007-031-regression-new-content-round.md`
- `docs/updates/INDEX.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`
- relevant decompiled build files under `references/doloc-town/reverse/builds`

## Known Historical Facts

- Official Workshop vehicle content only replaces the built-in motor's icon/body/light-mask assets. It does not provide a native vehicle registry, save slot, UI list, or behavior registry.
- Existing `IMotorVehicleApi` is Experimental because native state revolves around `DolocAPI.Motor`, `MotorDataManager`, and `AgentControllerState.motorController`.
- Past regressions include global motor sprite replacement, original motor following the second motor across room boundaries, failed summon residue crossing saves, and disabled SecondMotor sending empty mail.
- The old `SecondMotorMod` delivers the key through mail. This goal must remove that player path in favor of official JSON content and phone-booth sale.
- Previous vehicle smokes used third save and sometimes recovery teleports. This goal must use the user's eighth and ninth save fixtures for player-visible vehicle validation.

## API Status Goal

Target shape:

- Keep or migrate old `IMotorVehicleApi` compatibility surface as Experimental/obsolete compatibility unless it is rebuilt cleanly.
- Introduce or reshape the public contract around custom motor definitions, key binding, per-save vehicle state, summon/ride/dismount, transition lifecycle, and appearance leases.
- Definition/content/key registration may become StableCandidate only if it is DTMAPI-owned, namespaced, and does not imply native runtime success.
- Runtime multi-motor behavior may be promoted only if method-body review and eighth/ninth-save evidence prove save/load, transition, disable/re-enable, cleanup, and original-motor isolation. Otherwise it must remain Experimental with explicit blocker notes.
- Do not expose raw Unity, Harmony, BepInEx, or decompiled Doloc Town types.

## Native Owner Questions

Before implementation, answer in an API review/update section:

- Which native method equips or binds `ItemMotorKey` to the equipment/vehicle slot?
- Which state holder records the player's official motor unlock, equipped vehicle, motor room, motor position, and current riding controller?
- Does the phone booth shop load official JSON items by table, store id, or content source id, and can a custom key be sold there without runtime injection?
- Can a custom key route to a DTMAPI vehicle without mutating the original `DolocAPI.Motor` persisted state?
- Which native methods read `DolocAPI.Motor` while riding, crossing maps, entering gates, refreshing camera/body/drone state, or showing the motor bar?
- Which renderers and asset keys can be scoped to a clone so official `sprite_vehicle_motor*` remains untouched?
- What happens when multiple keys for one custom vehicle are present? Which API layer enforces single vehicle identity?
- What happens if the custom-motor mod is disabled or missing after a save has a custom motor state?

## Task A - Baseline, Branch, Version, Review

- Confirm `git status --short --branch` in `E:\Python_project\DTMAPI-multi-motor`.
- Confirm current controlled version is `0.5.1-alpha` or already `0.5.2-alpha`.
- Preserve `docs/reviews/manual-qa/2026/20260614-0002-multi-custom-motor-api-review.md`.
- Add or update a dedicated API review record if method-body review expands beyond this goal file.
- Do not edit runtime before the native-owner review notes are written.

## Task B - Native Owner Method-Body Review

- Inspect current decompiled build method bodies for:
  - `ItemMotorKey.OnUse`
  - `MotorController.__Init`, `Control`, `AutoFlyTo`, `FlyToTargetCoroutine`, `SetIsRiding`, `OnFixedUpdate`, endurance methods
  - `MotorDataManager.UnlockMotor`, `UpdateMotorRoom`, `AfterLoadData`
  - `AgentControllerState.GetOnMotor`, `GetOffMotor`, `OnUpdateRiding`
  - `MotorInteractable.OnInteract`
  - `DolocAPI.Motor`, `UnlockMotor`, `SetMotorPosition`, `EnterRoom`, `AgentPosition`
  - `ManagerGate.TryEnterOnMotor`, `TryQuitOnMotor`
  - native phone-booth/store/item JSON loaders and shop table owners
  - native equipment/vehicle slot UI and archive state holders
- Record known facts, rejected hypotheses, and risk points before changing code.

## Task C - API Contract Rebuild

- Split definition/content/key registration from runtime behavior.
- Add clear result objects and failure reasons for content missing, key source disabled, original motor state conflict, no runtime clone, room disallowed, native transition blocked, and cleanup failure.
- Add per-vehicle identity semantics so multiple keys for the same vehicle bind to one vehicle instance.
- Add per-save custom motor state with archive/save identity guards; do not use a global owner file that can pollute other saves.
- Keep future extension points explicit but inactive: collider profile, appearance profile, movement profile, harvest/attack abilities.
- Preserve compatibility or add migration notes for old `SecondMotorMod` and `IMotorVehicleApi` callers.

## Task D - GameBridge Runtime Rebuild

- Rebuild the custom motor runtime around reviewed native owners, not smoke-only helpers.
- Ensure original motor state is captured/restored only at safe ownership boundaries and is never permanently overwritten by custom motor actions.
- Ensure clone appearance is scoped to the custom vehicle and does not install global `sprite_vehicle_motor*` replacements.
- Ensure summon, ride, dismount, room transition, returned-to-title, save load/switch, disabled/missing mod, and failed summon all clean up DTMAPI-owned runtime objects.
- Ensure riding a custom motor through the eighth-save left edge keeps the custom motor with the player and does not move/show the original motor at the new map entry.
- Ensure ninth save works without native official motor unlock.

## Task E - SecondMotorMod Rebuild

- Rework `SecondMotorMod` to use the new custom motor API.
- Remove mail delivery as the player-facing key path.
- Add official-local JSON content for the custom key and phone-booth sale path where the native content owner supports it.
- The key should be bindable through the official left-click use path and summon the same registered custom motor even if the player owns multiple copies.
- Use the official extension mod's motor art as scoped custom motor art for this mod only.
- Keep config/status UI informative, but no config success may substitute for game validation.

## Task F - Smoke/Test Rewrite

- Keep global smoke defaults unchanged except vehicle scenarios.
- Add or rewrite vehicle smoke cases so custom motor validation explicitly uses:
  - Save slot 8: farm-left fixture, no teleport to hidden positions, real input path: give key, left-click equip/use, left-click summon, `E` ride, move left about 3 seconds across map boundary.
  - Save slot 9: official motor locked fixture, prove custom motor key/summon/ride does not depend on official motor unlock.
- If the save fixture does not match the user's described state, mark the scenario Blocked with clear reason instead of teleporting around it.
- Retain disabled/missing mod cleanup checks and clean exit checks.

## Task G - Validation

Required before completion:

- `git diff --check`
- Release build
- Release unit tests
- package/install updated DTMAPI and SecondMotor official-local content
- eighth-save custom motor smoke with screenshot/log evidence
- ninth-save locked-official-motor custom motor smoke with screenshot/log evidence
- disabled/missing SecondMotor cleanup and no empty mail check
- returned-to-title or save-switch cleanup check
- no leftover `DolocTown.exe`
- no Steam waiting-for-exit regression

## Task H - Documentation And Release Records

- Update `docs/updates/YYYY/...` and `docs/updates/INDEX.md`.
- Update `docs/debug/regressions/smoke-matrix.md`.
- Update `docs/hook-map/README.md`.
- Update `docs/api/public-api-matrix.md`.
- Update SecondMotor README/metadata and any developer-facing notes.
- Record old/new version and exact validation evidence paths.

## Completion Standard

Complete only if the player-visible requirements pass in real game:

- first custom motor replicates official motor behavior enough for key equip/use, summon, ride, dismount, and map transition;
- original motor remains independent and visually unmodified;
- custom motor art is scoped to the custom motor;
- key is official JSON/phone-booth sale path, not mail;
- multiple keys bind to the same custom motor;
- eighth and ninth save scenarios pass without teleporting around the fixture;
- cleanup/disable/save/title boundaries leave no residue or cross-save pollution;
- docs and matrices are updated.

If any required native owner is missing or any player-visible requirement cannot be proven, keep the goal incomplete and report the blocker, logs, verified facts, and next split.
