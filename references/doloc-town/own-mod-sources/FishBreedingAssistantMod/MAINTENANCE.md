# FishBreedingAssistant Maintenance Notes

This file is the handoff entry for future work after context compaction. It records the current state, update steps, and the fish roe display lessons learned from the SMAPI debugging run.

## Current State

- Project: `src/mods/FishBreedingAssistantMod`
- Mod name: `鱼卵信息显示`
- SMAPI UniqueID: `Yuuka.FishBreedingAssistant`
- Entry type: `Dlk.DolocFishBreedingAssistant.FishBreedingAssistantMod`
- Current mod version: `1.1.3`
- Current minimum Runtime/API: `0.8.20`
- Runtime public Workshop version paired with this state: `0.2.0`
- Active local install path:
  `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DLK_FishBreedingAssistant`
- Workshop package path:
  `packages/workshop/WorkshopPackages/DLK_FishBreedingAssistant`

The active behavior is intentionally small:

- Title: keep the vanilla title and append `（孵化鱼名）`, for example `鱼卵（黑夜骨架鱼）`.
- Description/detail: keep the vanilla description, then append exactly one line:
  `孵化：黑夜骨架鱼；孵化时间：12 小时；成长时间：1 天`
- Do not show parent-source lists.
- Do not show the wiki calculator URL.
- Do not change save data or the fish roe item itself.

## Why It Works Now

The hidden fish identity was always stored by the game:

- Fish tank products are converted to generic roe items.
- If the product item is `DolocTown.ItemFishRoe`, the game calls `SetFishName(product)`.
- `ItemFishRoe.fishName` is persisted on the item.
- The official incubator can identify the roe because it calls `Incubate()` / `ToFry()`, which use that hidden fish name.

The old failure was in SMAPI, not in the fish roe mod:

- The helper could read `ItemFishRoe.fishName`, but validation used the wrong table path.
- The correct farm fish table is `DolocTown.Config.DolocConfig.Tables.TbFarmFish`.
- Backpack/container hover often renders from `DolocTown.UI.ItemData`, bypassing simple `Item.get_title` / `GetDetailInfo` hooks.
- Directly patching the `ItemData` value-type constructor caused Harmony IL failures and should not be repeated.

SMAPI `0.8.20` fixed the chain:

- `TryGetFishRoeIdentity` reads `ItemFishRoe.fishName` and validates through `TbFarmFish`.
- The final hover path rebuilds or updates the rendered `ItemData`.
- `TitleRendering` and `DescriptionRendering` are raised and written back before the hover box is shown.

Related detailed debugging log:

- `src/mods/FishBreedingAssistantMod/reports/fish_roe_display_debugging_notes.md`
- `docs/releases/DolocTownSMAPI-0.8.20-public-0.2.0.md`

## Important Files

- `src/FishBreedingAssistantPlugin.cs`
  - Native SMAPI mod entry.
  - Subscribes to `helper.Experimental.Items.TitleRendering`, `DescriptionRendering`, and `DetailRendering`.
  - `AppendDetail(...)` owns the visible appended text.
- `src/FishBreedingLookup.g.cs`
  - Generated fish lookup: fish id, Chinese display name, roe item, hatch time, growth time, parent summary data.
  - The current mod only uses fish title, hatch time, and growth time.
- `tools/build_fish_outputs.py`
  - Regenerates `FishBreedingLookup.g.cs`, CSV/PNG reports, and calculator data from extracted tables.
- `build.ps1`
  - Builds `dist/DolocTownFishBreedingAssistant.dll`.
- `scripts/tools/package_split_workshop_mods.ps1`
  - Creates/updates the official-mod package directories under `packages/workshop/WorkshopPackages` and local `MODS`.
  - Contains FishBreedingAssistant package metadata, version, minimum SMAPI version, and descriptions.
- `scripts/tools/build_smapi_release.ps1`
  - Full release build and validation script.
  - Contains version assertions that must be updated whenever this mod version changes.

## Local Build And Install

Build only this mod:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\src\mods\FishBreedingAssistantMod\build.ps1
```

Install the built DLL into the active local official-mod package:

```powershell
Copy-Item -LiteralPath ".\src\mods\FishBreedingAssistantMod\dist\DolocTownFishBreedingAssistant.dll" `
  -Destination "$env:USERPROFILE\AppData\LocalLow\RedSawGames\DolocTown\MODS\DLK_FishBreedingAssistant\Content\DolocSMAPI\plugins\DolocTownFishBreedingAssistant.dll" `
  -Force
```

The game must be restarted after replacing the DLL. Already-loaded DLLs do not hot-reload.

Regenerate split Workshop/local packages after metadata changes:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\tools\package_split_workshop_mods.ps1
```

Full SMAPI release validation:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\tools\build_smapi_release.ps1
```

## Version Update Checklist

When changing the fish roe display mod version, update all of these together:

1. `src/mods/FishBreedingAssistantMod/src/FishBreedingAssistantPlugin.cs`
   - `[assembly: AssemblyVersion(...)]`
   - `[assembly: AssemblyFileVersion(...)]`
   - `PluginVersion`
2. `scripts/tools/package_split_workshop_mods.ps1`
   - FishBreedingAssistant `VersionEscaped`
   - `SmapiMinimumApiVersion` if the Runtime dependency changes
   - Chinese/traditional/English description text
3. `scripts/tools/build_smapi_release.ps1`
   - FishBreedingAssistant expected manifest version
   - FishBreedingAssistant expected minimum API version
   - FishBreedingAssistant assembly version assertion
   - FishBreedingAssistant release zip filename
4. Rebuild and repackage:
   - `src/mods/FishBreedingAssistantMod/build.ps1`
   - `scripts/tools/package_split_workshop_mods.ps1`
5. Confirm the generated package files:
   - `packages/workshop/WorkshopPackages/DLK_FishBreedingAssistant/info.json`
   - `packages/workshop/WorkshopPackages/DLK_FishBreedingAssistant/Content/DolocSMAPI/manifest.json`
   - local `MODS/DLK_FishBreedingAssistant/info.json`
   - local `MODS/DLK_FishBreedingAssistant/Content/DolocSMAPI/manifest.json`

Quick DLL verification:

```powershell
[System.Reflection.AssemblyName]::GetAssemblyName(".\src\mods\FishBreedingAssistantMod\dist\DolocTownFishBreedingAssistant.dll")
Get-FileHash -Algorithm SHA256 ".\src\mods\FishBreedingAssistantMod\dist\DolocTownFishBreedingAssistant.dll"
```

## In-Game Verification

Use a fresh restart after installing:

1. Enable `DolocTown SMAPI Runtime` installation in the game directory, and enable `鱼卵信息显示` in official mods.
2. Confirm the Runtime is at least internal/API `0.8.20`.
3. Hover a newly collected fish roe in the backpack.
4. Hover a stored fish roe in a container.
5. Expected:
   - Title includes `（鱼名）`.
   - Vanilla unknown-roe text is still present.
   - Only one appended line is present: hatch fish, hatch time, growth time.
   - No parent list and no calculator link.

If the old long text appears, the game is still loading an old DLL or stale package metadata. Restart the game and verify the DLL hash in the active local `MODS` package.

If no text appears:

- Confirm `MinimumApiVersion` is not higher than the installed Runtime/API version.
- Confirm the Runtime log says the mod loaded:
  `Started DolocTown SMAPI mod Yuuka.FishBreedingAssistant`
- Confirm the old BepInEx Harmony plugin with GUID `com.dlk.doloctown.fishbreedingassistant` is not enabled at the same time.
- Confirm the item is a real `ItemFishRoe`; generic items without hidden `fishName` cannot be identified.

## Data Regeneration

The fish lookup is generated from game data, not hand-written.

Run from repo root when game data changes:

```powershell
python .\src\wiki\FishFarmingAnalyzer\tools\extract_fish_breeding_data.py
python .\src\wiki\FishFarmingAnalyzer\tools\build_fish_outputs.py
python .\src\wiki\FishFarmingAnalyzer\tools\audit_fish_breeding_data.py
```

Then rebuild the mod. `build_fish_outputs.py` lives in the wiki project because it also regenerates tables and calculator assets, but it still writes the generated C# lookup back to `src/mods/FishBreedingAssistantMod/src/FishBreedingLookup.g.cs`. The in-game fish roe display currently only needs:

- fish id
- Chinese fish title
- hatch time text
- growth time text

## Guidance For The Next Hidden-Info Mod

Start with the same pattern:

1. Identify where the game stores the hidden state on the real item/object.
2. Confirm how the official game consumes that state.
3. Add a small SMAPI helper only if the hidden state is generally useful to mods.
4. Display through `helper.Experimental.Items` or another shared SMAPI event instead of per-mod UI patches.
5. Avoid patching `DolocTown.UI.ItemData` constructors directly.
6. Prefer one concise visible line over dumping all derived data into the tooltip.

For diagnostics, first prove three facts separately:

- the hidden field exists on the real runtime object;
- the value survives clone, stack, container, save/load, or day transition if relevant;
- the final UI path receives the object or a reliable identity tag.

Only after those are confirmed should the mod decide what to show to the player.
