# Reference Copy Manifest

Copied on 2026-05-30 from private local reference folders.

This manifest records categories and destination boundaries for public-source hygiene. It intentionally avoids requiring or publishing the original absolute local paths.

## Copied From Private Doloc Town Workspace

- Official Workshop/modding PDFs
  -> `references/doloc-town/official-workshop-docs/pdf`
- Official Workshop documentation crawl from 2026-05-17
  -> `references/doloc-town/official-workshop-docs/feishu-crawl-20260517`
- Doloc Town Workshop update notes for `0.96.05` and `0.96.06`
  -> `references/doloc-town/official-workshop-docs/update-notes`
- Doloc Town Modding API and functional Workshop research notes
  -> `references/doloc-town/research-notes`
- Doloc Town motor/vehicle API research notes
  -> `references/doloc-town/research-notes`
- Reverse research builds `23249387_workshop_247ACD` and `23465763_workshop_38581E`
  -> `references/doloc-town/reverse/builds`
- Selected own mod source copies for migration research:
  - `AutoFishingMod`
  - `ActionSpeedMod`
  - `OneActionCompleteMod`
  - `FishBreedingAssistantMod`
  - `AnimalHusbandryProgressMod`
  -> `references/doloc-town/own-mod-sources`

The five own mod source copies include `src/`, README/maintenance docs, build scripts, and diagnostic reports when present. Old `dist/`, historical `release/` packages, extracted release folders, and binaries were intentionally not copied.

## Explicitly Not Copied From Private Doloc Town Workspace

- Old DLKsmapi runtime/framework source, except the five selected own mod source folders listed above.
- Package caches.
- Archive folders.
- Temporary folders.
- Old generated DTMAPI/DLKsmapi runtime folders.
- Old DLKsmapi binaries, runtime packages, source backups, and existing packaged mod binaries.

## Copied From Stardew Valley SMAPI Install

- Installed SMAPI runtime files
  -> `references/stardew-smapi/installed`
- SMAPI internal files
  -> `references/stardew-smapi/installed/smapi-internal`
- SMAPI bundled `ConsoleCommands`
  -> `references/stardew-smapi/bundled-mods`
- SMAPI bundled `SaveBackup`
  -> `references/stardew-smapi/bundled-mods`

Other Stardew Valley user mods were not copied.

## Moved Existing DTMAPI Workspace Samples

Existing third-party Doloc Town mod archives/extracted folders from the DTMAPI workspace root were moved to `references/third-party-mods`.
