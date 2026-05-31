# AnimalHusbandryProgressMod Maintenance

This file is the handoff entry for the animal bell special-produce progress mod.

## Current State

- Mod name: `牧铃显示隐藏产物进度`
- SMAPI UniqueID: `Yuuka.AnimalHusbandryProgress`
- DLL: `DolocTownAnimalHusbandryProgress.dll`
- Version: `1.0.0`
- Minimum SMAPI API: `0.8.22`
- Source: `src/AnimalHusbandryProgressPlugin.cs`
- Build script: `build.ps1`
- Cover image: copied from the external screenshot path configured in `build.ps1` to both `icon.png` and `preview.png`.

The mod is intentionally small. It should stay on SMAPI's animal API and should not add private Harmony patches.

## Runtime Contract

The mod depends on SMAPI `helper.Experimental.Animals`:

- `ViewerRendering`
- `AnimalViewerRenderingEventArgs.HusbandryProgress`
- `AnimalViewerRenderingEventArgs.AddProgressBar(...)`
- `AnimalViewerRenderingEventArgs.AddProgressBar(..., Color fillColor)`

SMAPI owns all fragile game integration:

- mapping `AnimalFullInfoData` back to `Animal`
- reading private `Animal.husbandryValues`
- reading `TbHusbandry` thresholds
- cloning and positioning Runtime-managed `AnimalViewer` progress bars

The mod only chooses what to display.

## Display Rule

For each animal viewer render:

1. Read `e.HusbandryProgress`.
2. Keep entries with `Threshold > 0`.
3. Pick the entry with highest `Progress`, then highest `Current`.
4. Add one progress bar:

```text
特殊产物 current/threshold
```

No item name, feed source, probability, or extra detail is shown. The mod passes an orange fill color through the 0.8.22 color overload.

## Build

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\src\mods\AnimalHusbandryProgressMod\build.ps1
```

Build outputs:

- `dist/DolocTownAnimalHusbandryProgress.dll`
- `release/DolocTownAnimalHusbandryProgress-SMAPI-1.0.0.zip`
- `packages/workshop/WorkshopPackages/DLK_AnimalHusbandryProgress/Content/DolocSMAPI/plugins/DolocTownAnimalHusbandryProgress.dll`
- `packages/workshop/WorkshopPackages/DLK_AnimalHusbandryProgress/icon.png`
- `packages/workshop/WorkshopPackages/DLK_AnimalHusbandryProgress/preview.png`
- `%AppData%/LocalLow/RedSawGames/DolocTown/MODS/DLK_AnimalHusbandryProgress/Content/DolocSMAPI/plugins/DolocTownAnimalHusbandryProgress.dll`

The PowerShell build script constructs Chinese package text from Unicode escapes to avoid Windows PowerShell 5 UTF-8 parsing issues.

## Expected Logs

With SMAPI 0.8.22+, startup should include lines like:

```text
Started DolocTown SMAPI mod Yuuka.AnimalHusbandryProgress
Registered animal viewer handler: Yuuka.AnimalHusbandryProgress -> Animals.ViewerRendering.
```

## If The Bar Does Not Appear

Check in this order:

1. Runtime/API is at least `0.8.22`.
2. The mod is enabled in the official mod list.
3. SMAPI logs show the animal viewer handler registration.
4. The selected animal has configured husbandry data in `TbHusbandry`; if not, no bar is expected.
5. If the animal should have special produce but `HusbandryProgress` is empty, fix SMAPI's `TryBuildAnimalHusbandryProgress` path rather than patching this mod.

Observed failure on 2026-05-21:

```text
Skipping Yuuka.AnimalHusbandryProgress: requires DolocTown SMAPI 0.8.21 but runtime is 0.8.20.
```

Fix: reinstall/update the Runtime package so the game directory contains:

```text
BepInEx/plugins/DolocTownSMAPI/DolocTownSMAPI.Core.dll 0.8.22.0
BepInEx/plugins/DolocTownSMAPI/DolocTownSMAPI.SDK.dll  0.8.22.0
```

After updating the Runtime, fully restart the game. A running game process keeps the old loaded assemblies.

Color API note:

- SMAPI `0.8.21` can add animal viewer progress bars, but they inherit the cloned mood bar color.
- SMAPI `0.8.22` adds `AddProgressBar(label, progress, text, Color fillColor)` and Runtime applies the color to the cloned bar's `progressMask`.

Flicker fix note:

- Early 0.8.22 builds cloned `moodBar` while it was active, then changed title/progress/color. This could show `心情` for one visible tick on first creation.
- Runtime now hides cached extension bars in an `AnimalViewer.OnShow` prefix, instantiates clones inactive, updates title/progress/color while inactive, and only then activates them.
- If a fresh game launch enters the animal viewer and the third bar keeps the cloned `moodBar` title while showing the correct custom progress/color, check the cloned bar's `UILocalization` components. The first enable can run `UILocalization.Start()` and restore the cloned title. Runtime strips those localization components from cloned extension bars and reapplies title/progress/color after activation.

## Future Ideas

- Optional config to show output name, for debugging only.
- Optional config to display all special outputs if an animal ever gets multiple important tracks.
- Keep the default player-facing UI as one compact progress bar.
