# Own Mod Sources

Copied on 2026-05-30 from selected mod source folders under `E:\Python_project\DLK\src\mods`.

These files are migration inputs for DTMAPI compatibility work. They are not DTMAPI runtime source, and they must not be copied into `src/` unchanged. Use them to understand behavior, config needs, hook targets, and player-facing expectations.

## Included

- `AutoFishingMod`: automatic fishing, fishing information data, fishing phase/input/minigame hooks.
- `ActionSpeedMod`: action animation speed controls, bottle/eat/drink/machine/harvest interaction hooks.
- `OneActionCompleteMod`: one-action resource completion plus fuel machine and feeder completion helpers.
- `FishBreedingAssistantMod`: fish roe identity/display information using item title/description/detail rendering events.
- `AnimalHusbandryProgressMod`: animal bell hidden special-produce progress display.

`FishBreedingAssistantMod` and `AnimalHusbandryProgressMod` are separate mods. The former explains fish roe identity and hatch/growth data on item hover; the latter adds a special-produce progress bar in the animal bell viewer.

## Copied Files

Each mod keeps source-oriented files only:

- `src/`
- `README.md`
- `MAINTENANCE.md` when present
- `build.ps1`
- diagnostic `reports/` or `_mod_doctor_report/` when present

Old `dist/`, historical `release/` packages, extracted release folders, and binaries were intentionally not copied.

## Migration Rule

When migrating these mods, prefer adding stable DTMAPI APIs in `DTMAPI.Abstractions` and putting fragile Doloc Town/Harmony/reflection work in `DTMAPI.GameBridge.DolocTown`. The migrated mods should use DTMAPI APIs rather than re-owning broad Harmony patches where a stable bridge can exist.
