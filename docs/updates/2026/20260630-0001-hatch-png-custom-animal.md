# 20260630-0001 Hatch PNG Custom Animal

## Summary

Implemented the source side of a loose-PNG custom livestock path for the Hatch prototype. Hatch is registered as a chicken-template animal: native AI, schedule, movement, sleep, and production ownership stay on the chicken route, while DTMAPI returns the chicken RuntimeAnimatorController for Hatch animator keys and swaps individual renderer sprites through native `SpriteOverrideHandler`.

## Source Request

User requested implementing the Hatch PNG custom animal plan:

- Use `hatch` with `schedule_id/templateSpeciesId/aiTemplate = chicken`.
- Avoid Unity Editor/AssetBundle generation for Hatch.
- Add `animatorMode: "pngSpriteOverride"` and map chicken frame sprites to Hatch PNG sprites.
- Keep production on a custom `hatch_produce -> hatch_meat` path unless runtime proves custom products are unsupported.
- Do not patch `DolocAssetCache<T>`, mutate chicken/goat controllers, or reuse the untested `DTMAPI-animal` worktree.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Features/CustomAnimals/CustomAnimalAnimatorBridgeService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/CustomAnimals/CustomAnimalAnimatorBridgeHookBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/HarmonyReflectionPatcher.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `E:\DolocTownUnity\DolocTownMeta\prototypes\hatch\DTMAPI_HatchAssets\Content\DTMAPI\manifest.json`
- `E:\DolocTownUnity\DolocTownMeta\prototypes\hatch\DTMAPI_HatchAssets\info.json`
- `E:\DolocTownUnity\DolocTownMeta\prototypes\hatch\DTMAPI_HatchAssets\Content\DTMAPI\dtmapi-package.json`
- `E:\DolocTownUnity\DolocTownMeta\prototypes\hatch\DTMAPI_HatchAssets\Content\DTMAPI\custom-animals.json`
- `E:\DolocTownUnity\DolocTownMeta\prototypes\hatch\DTMAPI_HatchAssets\Content\animal_tbanimal.json`
- `E:\DolocTownUnity\DolocTownMeta\prototypes\hatch\DTMAPI_HatchAssets\Content\animal_tbanimaldocument.json`
- `E:\DolocTownUnity\DolocTownMeta\prototypes\hatch\DTMAPI_HatchAssets\Content\item_tbitem.json`
- `E:\DolocTownUnity\DolocTownMeta\prototypes\hatch\DTMAPI_HatchAssets\Content\item_tbitemspawn.json`
- `E:\DolocTownUnity\DolocTownMeta\prototypes\hatch\DTMAPI_HatchAssets\Content\mod_tbmodstoreextension.json`
- `E:\DolocTownUnity\DolocTownMeta\prototypes\hatch\DTMAPI_HatchAssets\Content\Sprites\hatch_frame_manifest.json`

## Implementation Notes

- `custom-animals.json` now accepts `animatorMode: "pngSpriteOverride"` with `frameManifest`, `templateSpritePrefix`, and `customSpritePrefix`.
- `AnimatorAsset.TryLoadAsset` returns the template chicken controller for Hatch adult/child animator keys instead of loading a bundle.
- `Animal.OnRender` and `Animal.DEBUG_SetAdult` attach or refresh native `DolocTown.SpriteOverrideHandler` only on registered PNG custom animal renderers.
- `SpriteOverrideHandler.TryGetModOverrideSprite` maps only registered renderer contexts from `anim_animal_chicken_*` to `anim_animal_hatch_*` and loads sprites through native `DolocAPI.modManager.LoadSpriteFromFile`.
- `AnimalRenderer.OnRecycle` clears the handler context so pooled renderers cannot leak Hatch mappings into another animal.
- `jump_ready` maps to Hatch `jump_0`; missing mapped PNGs mark `CustomAnimals.PngSpriteBridge.<species>` degraded and do not replace the frame.
- The Hatch prototype manifest now targets `MinimumDTMApiVersion: 0.5.2-alpha`, matching the current local runtime used for validation.
- The Hatch package root now includes official-local `info.json`. Without this file, Doloc Town's official local-mod refresh removed `Local.DTMAPI_HatchAssets` from `SAVE\mod_infos.json`, causing DTMAPI to skip the package with `未找到 Local.DTMAPI_HatchAssets 的官方启用状态`.

## Validation

- Passed: PowerShell `ConvertFrom-Json` for all Hatch content JSON files under `E:\DolocTownUnity\DolocTownMeta\prototypes\hatch\DTMAPI_HatchAssets\Content`.
- Passed: `tools/scripts/test.ps1 -Configuration Release` with `DTMAPI.UnitTests: OK`; only restricted-network `NU1900` package vulnerability index warnings were emitted.
- Passed: slot 7 DirectExe smoke under shared runtime lock after installing current DTMAPI and copying `DTMAPI_HatchAssets` to LocalLow `MODS`.
- Evidence `GAME-SMOKE/20260630-182610`: retained diagnostic run where DTMAPI launched cleanly but skipped Hatch because the prototype manifest required `DTMAPI >= 0.5.3-alpha` while the local runtime was `0.5.2-alpha`.
- Evidence `GAME-SMOKE/20260630-183043`: retained early pass after a manual `mod_infos.json` enablement edit; later manual testing showed the official local-mod refresh removed the Hatch entry because the package root lacked `info.json`.
- Evidence `GAME-SMOKE/20260630-184600`: passed `RunStatus`, `StartupLog`, `GameLaunched`, `HookProbe`, `SaveLoaded`, `ProcessExited`, `NoFatalInstanceWindow`, and `ForcedClose`; logs show `DTMAPI.HatchAssets` indexed, Hatch adult/child animator keys registered, `hatch->chicken` AI template registered, `pngSpriteOverrides=hatch`, and `CustomAnimals.PngSpriteBridge=verified`. Post-smoke `SAVE\mod_infos.json` still contains `Local.DTMAPI_HatchAssets enabled=True`, proving the official local metadata fix persists across startup refresh.
- Pending: manual/native debug-console Hatch release through `sack_hatch`.
- Pending: live Hatch renderer frame evidence (`CustomAnimals.PngSpriteBridge.hatch`) because the automatic smoke does not release a Hatch animal.
- Pending: `hatch_meat` custom product verification; fallback to native `meat` is not applied unless runtime evidence shows the custom product row is unsupported.

## Evidence Links

- Hook map: `docs/hook-map/README.md`
- Smoke matrix row: `HATCH-PNG-CUSTOM-ANIMAL-20260630` in `docs/debug/regressions/smoke-matrix.md`
- Hatch content package: `E:\DolocTownUnity\DolocTownMeta\prototypes\hatch\DTMAPI_HatchAssets\Content`
- Runtime smoke: `docs/debug/evidence/GAME-SMOKE/20260630-183043`
- Stable runtime smoke after root `info.json` fix: `docs/debug/evidence/GAME-SMOKE/20260630-184600`
- Diagnostic skipped-pack smoke: `docs/debug/evidence/GAME-SMOKE/20260630-182610`

## Rollback

Revert the `pngSpriteOverride` additions in the CustomAnimals GameBridge files and remove the Hatch content package JSON if the renderer override path pollutes native renderers or fails slot 7 validation. The existing Shell Crab AssetBundle animator bridge is independent and should remain unless its tests regress.

## Follow-Up

- Give or buy `sack_hatch`, release Hatch, confirm Hatch PNGs render instead of chicken, and verify product routing.
- If `hatch_meat` is not queryable/produceable in the native product route, change `hatch_produce` to native `meat`, rerun slot 7 validation, and record the unsupported custom-product evidence.
