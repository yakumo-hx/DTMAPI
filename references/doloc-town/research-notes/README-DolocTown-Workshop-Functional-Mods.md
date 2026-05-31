# Doloc Town Workshop Functional Mods

> Status: historical bridge note. The current mainline runtime is now
> `DolocTown SMAPI`; see `docs/README-DolocTown-SMAPI.md`. The old
> `DLKWorkshopBridge` prototype is kept only as a reference and fallback.

## Current Findings

- The workshop branch adds `DolocTown.Config.ModManager`, `ModInfo`, `ModManifest`, `SteamWorkshopUploader`, and mod menu UI types.
- Official local mods are read from `Application.persistentDataPath\MODS`, normally:

```text
C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS
```

- Steam subscribed mods are resolved through `SteamUGC.GetSubscribedItems()` and `SteamUGC.GetItemInstallInfo()`, so workshop content under `D:\Steam\steamapps\workshop\content\2285550\<id>` is loaded by the same `ModManager`.
- Each mod root must contain `info.json`, `icon.png`, `preview.png`, and optional `Content\...`.
- `ModManager.UpdateCache()` builds `EnabledMods`. Official enable/disable is persisted by `DataPersistenceManager.SaveModManager()` to:

```text
C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\SAVE\mod_infos.json
```

- Official loading currently supports JSON config merge and PNG sprite replacement. It does not call `Assembly.Load`, BepInEx, Harmony, or any managed-code plugin lifecycle for workshop content.

## Why Official Functional Mods Are Not Supported Yet

The official loader is data-driven. It scans `Content` for `*.json` and `*.png`, then merges config tables like `item_tbitem`, `recipe_tbrecipe`, `mod_tbmodstoreextension`, `mod_tbmodfishingpoolextension`, and localization tables. That can add or replace content the game already knows how to instantiate.

Functional mods need runtime code: patching state machines, input getters, private fields, object behavior, or new systems. New vehicles have the same problem at a larger scale: they need code, controllers, physics/input/state, rendering hooks, save/load behavior, and usually UI integration. The current official schema does not expose those extension points.

## Bridge Prototype

`src\loaders\WorkshopBridgeMod` is a BepInEx plugin that patches the official `ModManager` and loads managed plugins only from official enabled mods:

```text
Content\DLKPlugins\*.dll
Content\BepInEx\plugins\*.dll
```

This lets a workshop package carry DLL mods while still using the official mod menu as the source of truth for whether the package should be loaded.

Limitation: once a .NET assembly and Harmony patches are loaded, they cannot be completely unloaded from the same Unity process. Disabling a functional mod after it has already loaded should be treated as requiring a restart unless that specific plugin implements disable hooks.

## Functional Loader Package

`DLK_Functional_Mod_Loader` is the upload-ready front component package. It is not an auto-running workshop mod; it is a workshop-distributed installer package. Players must open the downloaded/local item folder and run:

```text
1_install_loader.bat
```

The installer copies the known-working BepInEx 5 runtime files and `DLKWorkshopBridge.dll` into the Doloc Town game folder. It also writes:

```text
BepInEx\plugins\DLKWorkshopBridge\DLKFunctionalModLoader.install.json
```

It also copies a backup copy of the management scripts to:

```text
BepInEx\DLKFunctionalModLoader
```

This keeps the uninstaller available even if the player unsubscribes from the loader workshop item later.

Uninstall follows the SMAPI-style model: run the bundled uninstaller instead of expecting Steam unsubscribe to clean the game folder:

```text
2_uninstall_loader.bat
```

`3_check_loader_status.bat` verifies that Doorstop, BepInEx, Harmony, and the bridge are present.

Upload order for public testing:

1. Upload `DLK_Functional_Mod_Loader`.
2. Upload or update `DLK_Fishing_TestMod`.
3. Keep the fishing test dependency warning: only subscribing to the fishing mod is not enough.

## Build And Package

Build and install the bridge:

```powershell
.\src\loaders\WorkshopBridgeMod\build.ps1
```

Build existing DLL mods and create the combined legacy official local workshop package:

```powershell
.\scripts\tools\package_workshop_functional_mods.ps1 -BuildExistingMods
```

Build the split public/local packages:

```powershell
.\src\mods\FishingTestMod\build.ps1
.\scripts\tools\package_split_workshop_mods.ps1
```

Build the front loader package:

```powershell
.\scripts\tools\package_functional_mod_loader.ps1
```

The upload-ready loader package is created in both:

```text
E:\Python_project\DLK\packages\workshop\WorkshopPackages\DLK_Functional_Mod_Loader
C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DLK_Functional_Mod_Loader
```

The upload-ready fishing test package is created in both:

```text
E:\Python_project\DLK\packages\workshop\WorkshopPackages\DLK_Fishing_TestMod
C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DLK_Fishing_TestMod
```
