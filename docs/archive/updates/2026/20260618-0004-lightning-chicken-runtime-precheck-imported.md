# Update 20260618-0004: Lightning Chicken Runtime Precheck (Imported)

> Imported archival note (2026-07-08): this record was originally `20260618-0001` in `E:\Python_project\DTMAPI-animal`. It was renumbered on import because the latest DTMAPI branch already uses `20260618-0001` for installer work. It is historical branch evidence, not current mainline runtime support.

Date: 2026-06-18
Status: partial-runtime-evidence-build-passed

## Source Request

The active goal is the DTMAPI `0.5.3-alpha` Lightning Chicken MVP. This follow-up hardens the experimental GameBridge adapter after current reverse metadata review and records the first save-slot 11 runtime precheck without claiming final animal/animator acceptance.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Features/CustomAnimals/CustomAnimalsNativeTemplateAdapter.cs`
- `docs/api/public-api-matrix.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/2026/20260617-0010-lightning-chicken-mvp.md`
- `docs/updates/2026/20260618-0001-lightning-chicken-runtime-precheck.md`
- `docs/updates/INDEX.md`
- `docs/debug/evidence/GAME-SMOKE/20260618-010235/post-cleanup-status.txt`

## Native Facts And Changes

- Current reverse metadata `23762374_public_C416D4` names chicken free-time AI as nested `DolocTown.AnimalAI/Chicken_FreeTimeState`; the runtime resolver now uses `DolocTown.AnimalAI+Chicken_FreeTimeState` with the slash-form fallback.
- The runtime status text now names the nested AI type instead of the invalid top-level `DolocTown.Chicken_FreeTimeState`.
- AI mapping logs are emitted once per process instead of every repeated native `GetDefaultAnyState` query.
- The adapter clears cached runtime animator override-controller templates at save/title/environment reset so no old controller instance is reused across runtime boundaries.

## Runtime Evidence

- Acquired the shared runtime lock before install/launch.
- Ran `tools/scripts/run-game-smoke.ps1 -AutoExerciseLightningChicken -SaveSlot 11 -TimeoutSeconds 150`.
- Evidence directory: `docs/debug/evidence/GAME-SMOKE/20260618-010235`.
- Installed DTMAPI and the developer official local `DTMAPI_LightningChicken` package.
- `DTMAPI-latest.log` records:
  - `DTMAPI runtime starting`.
  - `Lightning Chicken content pack loaded. Species=dtmapi_lightning_chicken package=sack_dtmapi_lightning_chicken.`
  - `SaveLoaded hook dispatched. slot/index=10 isNewGame=False`.
  - `CustomAnimals AI template mapped species=dtmapi_lightning_chicken template=chicken state=DolocTown.AnimalAI+Chicken_FreeTimeState previous=DolocTown.AnimalAI+Normal_FreeTimeState.`
  - `Smoke exercise LightningChicken static checks OK ... animal=move:3.6/run:10.8/metabolism:2.083/adultPrice:2000 ... package=buy:2000/sell:1000 ... shop=animal_shop-append-preserved ... animator=pending-rendered-instance, animatorApplyCount=0`.

## Validation

- Passed: `E:\Python_project\DTMAPI\.tools\dotnet\dotnet.exe build DTMAPI.sln -c Release --nologo -nr:false` with `0` warnings and `0` errors.
- Passed: `E:\Python_project\DTMAPI\.tools\dotnet\dotnet.exe run --project tests\DTMAPI.UnitTests\DTMAPI.UnitTests.csproj -c Release --no-build` -> `DTMAPI.UnitTests: OK`.
- Passed: PowerShell parser checks for `run-game-smoke.ps1`, `install-to-game.ps1`, `release-common.ps1`, and `build.ps1`.
- Passed: parsed all `testmods/LightningChickenMod/**/*.json`.
- Passed: static contamination scan found no Lightning Chicken override of `store_tbstoreitemlist.json`, `chicken`, `sack_chicken`, or a vanilla `sack_chicken` shop append.
- Passed: version scan found no `0.5.2-alpha` / `0.5.2.0` in the controlled runtime/package/script paths and confirmed `0.5.3-alpha` / `0.5.3.0` in `Directory.Build.props`, `DtmApiRuntime`, release scripts, install script, publish metadata, and Lightning Chicken manifests.
- Passed: `git diff --check` with line-ending warnings only.
- Cleanup verified: `tools/scripts/runtime-lock-status.ps1` reported FREE and `Get-Process -Name DolocTown -ErrorAction SilentlyContinue` returned no process after manual cleanup.

## Incomplete Acceptance

- The smoke shell command timed out before `run-game-smoke.ps1` wrote `result.json`; this is not a passing smoke.
- No phone-booth purchase/release was completed.
- No rendered `dtmapi_lightning_chicken` existed, so independent `RuntimeAnimatorController` / cloned clip-path evidence remains pending.
- Movement timing, two-eggs-per-day production timing, vanilla chicken controller/speed/production non-pollution, chicken-nest/equipment interaction, save/reload, and scripted clean exit remain pending.

## Rollback Notes

Revert this follow-up by restoring the AI resolver/status/logging changes in `CustomAnimalsNativeTemplateAdapter.cs` and removing the 2026-06-18 evidence/doc entries. Do not remove the earlier Lightning Chicken implementation record unless the whole MVP branch is rolled back.

## Follow-Up

- Superseded by imported blocker update `20260618-0005`: two native store/release attempts showed the live `animal_shop` store cache does not expose `sack_dtmapi_lightning_chicken`, even after `DolocAPI.RefreshStore("animal_shop")`.
- Drive the real phone-booth animal-shop purchase path on save slot 11 or document a manual-test handoff if UI automation is not practical.
- Release `sack_dtmapi_lightning_chicken` on the farm and wait for `CustomAnimals animator isolation OK ... customController=DTMAPI.LightningChicken... clonedClips=...`.
- Compare ordinary chicken and Lightning Chicken controller identity, speed, metabolism/production, interaction, chicken-nest behavior, reload, and exit.
