# 20260628-0003 Shell Crab Custom Animal Animator Bridge

## Status

verified / slot-7 runtime smoke and manual release passed

## Source Request

User confirmed the Shell Crab content chain can place an animal in save slot 7, but it still renders as the goat fallback. The requested route is a generic DTMAPI custom animal animator bridge in `DTMAPI.GameBridge.DolocTown`: read enabled ContentPack `Content/DTMAPI/custom-animals.json`, register only DTMAPI-owned animator keys, return independent package controllers for those keys only, and keep goat/chicken native controllers plus template `AnimalInfo` untouched.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Hooking/HarmonyReflectionPatcher.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/CustomAnimals/CustomAnimalAnimatorBridgeFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/CustomAnimals/CustomAnimalAnimatorBridgeHookBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/CustomAnimals/CustomAnimalAnimatorBridgeService.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/updates/INDEX.md`
- `docs/hook-map/README.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- External content package: `E:\DolocTownUnity\DolocTownMeta\prototypes\shell_crab\DTMAPI_ShellCrab`
- External Unity study project: `D:\Unity\Projects\DolocTownMCP\Assets\Editor\ShellCrabStudyEditor.cs`

## Summary

- Added a generic custom animal animator bridge feature owned by GameBridge.
- Added Harmony reflection support for the non-generic native owner hook `DolocTown.AnimatorAsset.TryLoadAsset(string,out RuntimeAnimatorController)` without referencing Unity assemblies at compile time.
- The bridge scans enabled DTMAPI ContentPacks for `Content/DTMAPI/custom-animals.json`, supports `animatorMode: "assetBundle"`, and registers adult/child animator keys to package-local AssetBundle controller assets.
- Unknown resource keys return `unhandled` and continue through original Doloc Town `AnimatorAsset` / asset cache logic.
- Registered keys load independent `RuntimeAnimatorController` assets from a package AssetBundle. If the bundle or asset cannot load, the bridge marks the key `degraded`, logs diagnostics, and falls back to the template animator key instead of mutating the template controller.
- Shell Crab metadata now points its own child/adult animator URLs to `dtmapi_anim_animal_shell_crab_child` / `dtmapi_anim_animal_shell_crab`; `schedule_id: goat` remains unchanged.
- The Shell Crab package is now a non-code `ContentPack`; it does not place ordinary mods under `BepInEx/plugins`.
- Unity batch/menu `ShellCrabStudyEditor.BuildShellCrabAnimatorBundleBatch` builds `Content/DTMAPI/assets/shell-crab/shell_crab_animators.bundle` from the existing `shell_crab_adult.controller` and `shell_crab_young.controller`, then validates both simple asset names load from the bundle.
- Rejected after runtime evidence: patching closed `DolocAssetCache.GetAsset<RuntimeAnimatorController>` / `CheckAsset<RuntimeAnimatorController>` looked precise in source but polluted Unity/Mono's shared generic method body at runtime. It caused unrelated prefab/weather resources to be queried as `RuntimeAnimatorController`, so Shell Crab now hooks only `AnimatorAsset.TryLoadAsset`.

## Validation

- Passed: `tools/scripts/test.ps1 -Configuration Release` (`DTMAPI.UnitTests: OK`; `NU1900` package-vulnerability index warnings only).
- Passed: unit coverage for custom animal metadata parse, adult/child key registration, unknown-key passthrough, and missing bundle degraded status.
- Passed: Unity batch AssetBundle build with `D:\Unity\Editors\2021.3.45f2\Editor\Unity.exe -batchmode -quit -projectPath D:\Unity\Projects\DolocTownMCP -executeMethod ShellCrabStudyEditor.BuildShellCrabAnimatorBundleBatch`.
- Passed: Unity post-build validation loaded `shell_crab_adult` and `shell_crab_young` from `E:\DolocTownUnity\DolocTownMeta\prototypes\shell_crab\DTMAPI_ShellCrab\Content\DTMAPI\assets\shell-crab\shell_crab_animators.bundle`.
- Passed: installed rebuilt DTMAPI and the Shell Crab ContentPack to the local runtime under the shared runtime lock.
- Passed: `tools/scripts/run-game-smoke.ps1 -SaveSlot 7 -IncludeHookProbe -AutoExitAfterSecondsOverride 120 -TimeoutSeconds 180`; evidence `docs/debug/evidence/GAME-SMOKE/20260628-205615` records `SaveLoaded` slot/index `6`, HookProbe OK, Shell Crab ContentPack indexed, two animator keys registered, no fatal popup, and no leftover `DolocTown.exe`.
- Passed: manual slot 7 empty-barn release. The Y debug console gave `sack_shell_crab`, the native animal bag flow opened the naming dialog and spawned a Shell Crab visual instead of a goat. Runtime log at 2026-06-28 21:06:23 records `CustomAnimals.AnimatorBridge verified key=dtmapi_anim_animal_shell_crab_child species=shell_crab stage=child ... asset=shell_crab_young`.
- Passed: temporary runtime smoke settings were removed after manual testing, `DolocTown.exe` was no longer running, and the shared runtime lock was released after validation.

## Evidence Notes

- Relevant native owner path remains `Animal.OnRender -> AnimatorAsset.Asset -> AnimatorAsset.TryLoadAsset -> DolocAPI.GetAsset<RuntimeAnimatorController> -> DolocAssetCache.GetAsset<T>`.
- Runtime evidence from `docs/debug/evidence/GAME-SMOKE/20260628-201917`, `20260628-202858`, `20260628-203345`, and `20260628-203832` rejected the closed generic cache hook: startup weather resources and the save UI `GameDataPanel` prefab failed through `RuntimeAnimatorController` lookups even when Shell Crab was disabled. That failure mode is outside Shell Crab content and is why this update moved to the non-generic `AnimatorAsset.TryLoadAsset` hook.
- Passing runtime evidence from `docs/debug/evidence/GAME-SMOKE/20260628-205615` verifies the replacement hook without the earlier weather or `GameDataPanel` generic-cache failures.
- Passing manual evidence is in the live runtime `Player.log`: `Inventory debug give ... item=sack_shell_crab ... success=True`, then `CustomAnimals.AnimatorBridge verified key=dtmapi_anim_animal_shell_crab_child species=shell_crab stage=child ... asset=shell_crab_young`.
- This change deliberately does not implement PNG runtime controller generation, renderer late sprite override, vehicle-like runtime entity replacement, or `ICustomAnimalApi.RequestSpawn`.
- This change does not claim custom animal native spawn is complete. It only verifies the animator resource bridge path for custom animal table URLs.

## Rollback

Remove `CustomAnimalAnimatorBridgeFeature`, remove the `AnimatorAsset.TryLoadAsset` Harmony prefix helper, restore Shell Crab `animal_tbanimal.json` animator URLs to goat fallback, and remove the Shell Crab AssetBundle fields from `custom-animals.json`.

## Follow-Up

- Tune Shell Crab controller scale/bounds in Unity if the current child visual size should be smaller in the barn.
- Exercise the adult `dtmapi_anim_animal_shell_crab` key after growth or with a dedicated adult fixture; this run verified the child key because the native bag release spawned the young stage.
- If a future bundle fails in runtime, inspect Unity AssetBundle target/platform or asset-name lookup before considering a PNG runtime-controller fallback.
