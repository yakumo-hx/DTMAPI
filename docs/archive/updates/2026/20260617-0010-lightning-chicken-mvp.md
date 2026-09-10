# Update 20260617-0010: Lightning Chicken MVP

> Imported archival note (2026-07-08): this record was copied from `E:\Python_project\DTMAPI-animal` to preserve the abandoned Lightning Chicken experiment trail. It describes a superseded branch route and must not be read as current mainline release support.

Date: 2026-06-17
Status: implemented-build-passed-runtime-proof-pending

## Source Request

The user asked to implement the DTMAPI `0.5.3-alpha` Lightning Chicken MVP: a developer-only `DTMAPI.LightningChickenMod` package containing `dtmapi_lightning_chicken`, purchased as a native animal package from the animal shop, with chicken-like behavior, doubled movement and production pace, adult sale price `2000`, and no vanilla chicken pollution. The strict acceptance criterion is an independent native `RuntimeAnimatorController` or clip path.

## Changed Files

- `DTMAPI.sln`
- `Directory.Build.props`
- `docs/goals/2026/20260617-0002-lightning-chicken-mvp.md`
- `docs/goals/2026/20260617-0002-lightning-chicken-mvp.goal.txt`
- `docs/api/public-api-matrix.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/2026/20260617-0010-lightning-chicken-mvp.md`
- `docs/updates/INDEX.md`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.Hooks.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/CustomAnimals/*`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/DebugConsoleSmoke.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/LightningChickenSmokeCase.cs`
- `testmods/LightningChickenMod/*`
- `tests/DTMAPI.UnitTests/Program.cs`
- `tools/release/dtmapi-mod-publish-zh.json`
- `tools/scripts/build.ps1`
- `tools/scripts/install-to-game.ps1`
- `tools/scripts/release-common.ps1`
- `tools/scripts/run-game-smoke.ps1`

## Known Native Facts

- `mod_tbmodstoreextension` is the correct non-overwriting store extension path; `Tables.HandleModStoreExtension` removes only matching item IDs before appending extra rows.
- `AnimalAI.GetDefaultAnyState(string)` returns `Chicken_FreeTimeState` only for `chicken`; a narrow postfix can map `dtmapi_lightning_chicken` to the chicken state type.
- `Animal._HandleChickenNestBroken(AnimalEvent)` contains the hard-coded `protoName != "chicken"` guard, so chicken-template equipment behavior needs a narrow compatibility hook or a documented blocker.
- `Animal.OnRender` and `Animal.DEBUG_SetAdult` assign `AnimalRenderer.animatorController` from native `AnimalInfo` animator assets; this is the current strict-risk area.
- `AnimalRenderer.MoveTo(Vector2,float,Action)` clamps movement speed above `10`, so the `10.8` run-speed target may be limited by the native renderer.

## Validation

- Passed: `E:\Python_project\DTMAPI\.tools\dotnet\dotnet.exe build DTMAPI.sln -c Release --nologo -nr:false` with `0` warnings and `0` errors.
- Passed: `E:\Python_project\DTMAPI\.tools\dotnet\dotnet.exe run --project tests\DTMAPI.UnitTests\DTMAPI.UnitTests.csproj -c Release --no-build` -> `DTMAPI.UnitTests: OK`.
- Passed: PowerShell parser check for `tools/scripts/run-game-smoke.ps1`, `tools/scripts/install-to-game.ps1`, `tools/scripts/release-common.ps1`, and `tools/scripts/build.ps1`.
- Passed: `git diff --check` with line-ending warnings only.
- Passed: parsed all `testmods/LightningChickenMod/**/*.json`.
- Passed: static contamination scan found no `store_tbstoreitemlist.json`, no overriding `"id": "chicken"`, no overriding `"id": "sack_chicken"`, and no vanilla `sack_chicken` shop append inside `LightningChickenMod/Content`.
- Partial run: 2026-06-18 `GAME-SMOKE/20260618-010235` installed DTMAPI plus developer `DTMAPI_LightningChicken`, launched save slot 11/index 10, loaded the content pack, verified static native table/shop/package data after save load, and mapped AI to nested `DolocTown.AnimalAI+Chicken_FreeTimeState`.
- Incomplete run: `GAME-SMOKE/20260618-010235` timed out before final `result.json`; the log remained at `animator=pending-rendered-instance` because no real `dtmapi_lightning_chicken` had been bought/released/rendered. Logs were collected manually, the game process was closed, and the runtime lock was released.
- Not run/pending: phone-booth purchase/release, save/reload, movement timing, two-eggs-per-day production timing, ordinary chicken non-pollution runtime checks, and chicken-nest hardcoded-guard coverage.

## Evidence Links

- Goal: `docs/goals/2026/20260617-0002-lightning-chicken-mvp.md`
- Prior research: `docs/reviews/api/2026/20260617-0004-custom-animal-json-ai-animator-research.md`
- Reverse baseline: `references/doloc-town/reverse/builds/23762374_public_C416D4`
- Partial runtime precheck: `docs/debug/evidence/GAME-SMOKE/20260618-010235`
- Smoke matrix row: `docs/debug/regressions/smoke-matrix.md`
- Hook-map entry: `docs/hook-map/README.md`
- Follow-up update: `docs/updates/2026/20260618-0004-lightning-chicken-runtime-precheck-imported.md`

## Rollback Notes

Revert the `CustomAnimals` feature files/callbacks, remove `LightningChickenMod` and its release/install/smoke/test wiring, and restore the version constants/scripts from `0.5.3-alpha` / `0.5.3.0` to the previous baseline. Keep the research/update/goal records if they are needed to explain the stopped animator or manual-test boundary.

## Follow-Up

- Acquire the runtime lock and run or hand-test `tools/scripts/run-game-smoke.ps1 -AutoExerciseLightningChicken -SaveSlot 11`.
- In save slot 11, buy `sack_dtmapi_lightning_chicken` from the phone-booth animal shop and release it in the clean farm-space fixture.
- Confirm the log reaches `Smoke exercise LightningChicken OK` and `CustomAnimals animator isolation OK ... customController=DTMAPI.LightningChicken... clonedClips=...`.
- Compare vanilla chicken against lightning chicken for controller identity, movement, production cadence, save/reload, player interaction, and chicken-nest/equipment interaction.
- The hard-coded `_HandleChickenNestBroken` `protoName != "chicken"` path still needs runtime evidence or a narrow compatibility hook; implementing it by re-creating native `LinearTask` chains or broad IL rewriting is intentionally deferred.
