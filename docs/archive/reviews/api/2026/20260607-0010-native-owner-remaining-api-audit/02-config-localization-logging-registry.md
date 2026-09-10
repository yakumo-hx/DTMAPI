# 02 - Config, ConfigMenu, Localization, Logging, And ModRegistry

## Scope

Audited the infrastructure APIs that do not need a Doloc Town native owner to be legitimate public contracts:

- `IConfigHelper.ReadConfig<T>`, `WriteConfig<T>`, `GetConfigPath`, `RegisterMigration<T>`
- `IDtmConfigMenuApi` and config-menu item/page/pending-preview DTOs
- `IDtmHelper.Translation`, `ITranslationHelper.Language`, `ITranslationHelper.Get`
- `IMonitor.Log`, `LogOnce`, `LogException`
- `IModRegistry.IsLoaded`, `Get`, `GetAll`, `GetApi<T>`, `RegisterApi<T>`

## Files read

- `docs/api/public-api-matrix.md`: lines 21-45.
- `src/DTMAPI.Abstractions/Helpers.cs`: lines 6-48 and 92-109.
- `src/DTMAPI.Abstractions/ConfigMenu.cs`: lines 6-88.
- `src/DTMAPI.Abstractions/Logging.cs`: lines 15-29.
- `src/DTMAPI.Core/Services/ConfigService.cs`: lines 20-57.
- `src/DTMAPI.Core/Services/TranslationService.cs`: lines 15-82.
- `src/DTMAPI.Core/Logging/FileMonitor.cs`: lines 24-54.
- `src/DTMAPI.Core/Services/RegistryAndHelpers.cs`: lines 9-71.
- `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs`: lines 9-194 and 226-334.
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`: lines 328-357 and 359-403.
- `testmods/ConfigMenuExample/ModEntry.cs`: lines 10-32.
- `testmods/ActionSpeedMod/ModEntry.cs`: lines 34-64.
- `testmods/AnimalHusbandryProgressMod/ModEntry.cs`: lines 29-65.
- `docs/hook-map/README.md`: lines 297-336.

## Functions read

- `DTMAPI.Abstractions.IConfigHelper.ReadConfig<T>`: `src/DTMAPI.Abstractions/Helpers.cs:33`.
- `DTMAPI.Abstractions.IConfigHelper.WriteConfig<T>`: `src/DTMAPI.Abstractions/Helpers.cs:34`.
- `DTMAPI.Abstractions.IConfigHelper.GetConfigPath`: `src/DTMAPI.Abstractions/Helpers.cs:35`.
- `DTMAPI.Abstractions.IConfigHelper.RegisterMigration<T>`: `src/DTMAPI.Abstractions/Helpers.cs:36`.
- `DTMAPI.Core.Services.ConfigService.ReadConfig<T>`: `src/DTMAPI.Core/Services/ConfigService.cs:20`.
- `DTMAPI.Core.Services.ConfigService.WriteConfig<T>`: `src/DTMAPI.Core/Services/ConfigService.cs:40`.
- `DTMAPI.Core.Services.ConfigService.GetConfigPath`: `src/DTMAPI.Core/Services/ConfigService.cs:48`.
- `DTMAPI.Core.Services.ConfigService.RegisterMigration<T>`: `src/DTMAPI.Core/Services/ConfigService.cs:54`.
- `DTMAPI.ModConfigMenu.ConfigMenuRegistry.Register`: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:13`.
- `DTMAPI.ModConfigMenu.ConfigMenuRegistry.Add*`: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:18-31`.
- `DTMAPI.ModConfigMenu.ConfigMenuRegistry.GetKeybindConflicts`: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:46`.
- `DTMAPI.ModConfigMenu.ConfigMenuPage.PreviewPendingValues`: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:157`.
- `DTMAPI.ModConfigMenu.ConfigMenuPage.Save`: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:175`.
- `DTMAPI.ModConfigMenu.ConfigMenuPage.Cancel`: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:189`.
- `DTMAPI.ModConfigMenu.ConfigMenuItemBase.TrySetPendingValue`: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:268`.
- `DTMAPI.Core.Services.TranslationService.Get`: `src/DTMAPI.Core/Services/TranslationService.cs:27`.
- `DTMAPI.Core.Services.TranslationService.DetectLanguage`: `src/DTMAPI.Core/Services/TranslationService.cs:38`.
- `DTMAPI.Core.Logging.FileMonitor.Log`: `src/DTMAPI.Core/Logging/FileMonitor.cs:24`.
- `DTMAPI.Core.Logging.FileMonitor.LogOnce`: `src/DTMAPI.Core/Logging/FileMonitor.cs:41`.
- `DTMAPI.Core.Logging.FileMonitor.LogException`: `src/DTMAPI.Core/Logging/FileMonitor.cs:51`.
- `DTMAPI.Core.Services.ModRegistryService.GetApi<T>`: `src/DTMAPI.Core/Services/RegistryAndHelpers.cs:21`.
- `DTMAPI.Core.Services.ModRegistryService.RegisterApi<T>`: `src/DTMAPI.Core/Services/RegistryAndHelpers.cs:26`.
- `DTMAPI.Core.Services.DtmHelper.ReadConfig<T>`: `src/DTMAPI.Core/Services/RegistryAndHelpers.cs:69`.
- `DTMAPI.Core.Services.DtmHelper.WriteConfig<T>`: `src/DTMAPI.Core/Services/RegistryAndHelpers.cs:70`.

## Call graph

```text
Config helper
  ordinary mod helper.ReadConfig/WriteConfig
    DtmHelper.ReadConfig/WriteConfig
      ConfigService.ReadConfig/WriteConfig
        config path = Paths.ConfigPath/SafeUniqueId.json
        optional registered migration callback
        System.Text.Json read/write

Config menu
  mod helper.ModRegistry.GetApi<IDtmConfigMenuApi>("DTMAPI.ModConfigMenu")
    ConfigMenuRegistry.Register/Add*
      ConfigMenuPage items and pending values
      title settings UI reads pages/items
      PreviewPendingValues temporarily applies pending setters for visibility/editability checks

Localization
  DtmHelper.Translation
    TranslationService reads i18n/<language>.json and i18n/english.json
    Get(primary, english, fallback, key)

Logging
  mod helper.Monitor.Log/LogOnce/LogException
    FileMonitor writes DTMAPI latest log
    forwards to host logger

ModRegistry
  Runtime.RegisterRuntimeApi / ModRegistryService.RegisterApi
  ordinary mod helper.ModRegistry.GetApi<T>
    returns DTMAPI API object by unique id + interface type
```

## Function body findings

- `ConfigService.ReadConfig<T>` creates a default object when no file exists, applies a registered migration callback, and writes the result back to disk (`src/DTMAPI.Core/Services/ConfigService.cs:20-38`).
- `ConfigService.WriteConfig<T>` serializes indented JSON and applies a newline formatting replacement before writing to the per-mod config path (`src/DTMAPI.Core/Services/ConfigService.cs:40-46`).
- `GetConfigPath` derives the path from `paths.ConfigPath` and `SafeUniqueID`, which makes it DTMAPI-owned and independent of Doloc Town save files (`src/DTMAPI.Core/Services/ConfigService.cs:48-52`).
- `ConfigMenuRegistry` stores pages by owner id; `Register` creates a `ConfigMenuPage`, and all `Add*` calls append local item descriptors. No native settings menu object is owned here (`src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:9-38`).
- `GetKeybindConflicts` scans config-menu keybind items across pages and returns duplicate key ownership. It is useful DTMAPI metadata, not a native input binding conflict resolver (`src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:46-76`).
- `PreviewPendingValues` temporarily writes pending values into target config objects, lets the title UI evaluate conditional rows, then restores previous values on dispose. This is correct for title UI preview, but callback setters can have side effects if a mod does more than assign a field (`src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:157-163`, `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:226-234`).
- `ConfigMenuPage.Save` refuses locked pages/keybind conflicts, applies pending values, invokes `SaveConfig`, captures committed values, and intentionally leaves `IsEditing=true` after save (`src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:175-187`).
- `TranslationService` chooses language from an environment override or .NET culture, loads a primary and English dictionary, and falls back to the provided fallback or key. It does not read the game's own language setting (`src/DTMAPI.Core/Services/TranslationService.cs:15-82`).
- `FileMonitor.Log` formats timestamped DTMAPI log lines, writes to the latest log, and forwards to the BepInEx host logger (`src/DTMAPI.Core/Logging/FileMonitor.cs:24-39`).
- `ModRegistryService` has two dictionaries: loaded manifests by unique id, and registered API objects by unique id + interface type. This is the DTMAPI runtime registry only (`src/DTMAPI.Core/Services/RegistryAndHelpers.cs:9-28`).
- `DtmApiRuntime.RefreshConfigPageLocks` locks config pages for official-local disabled or not-yet-loaded mods, showing that official enablement is consulted, but not owned, by DTMAPI (`src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:328-357`).

## Native owner verdict

| API group | Verdict | Native owner reached |
| --- | --- | --- |
| Config helper | DTMAPI-only | No native owner required; files are DTMAPI config files, not save data. |
| ConfigMenu | DTMAPI-only / Partial UI | DTMAPI registry plus reflected title settings UI; no native settings-menu contract is exposed. |
| Localization | DTMAPI-only | DTMAPI `i18n` files; no confirmed official language owner. |
| Logging | DTMAPI-only | DTMAPI log file and BepInEx host logger. |
| ModRegistry | DTMAPI-only / registry-only | DTMAPI-loaded mods and DTMAPI API objects only; official ModManager remains separate. |

## Ordinary mod usability

- Config helper: 普通 mod 可用; `ReadConfig`, `WriteConfig`, and `GetConfigPath` can remain `stable`. `RegisterMigration<T>` should remain `experimental` until migration ordering/versioning is documented.
- ConfigMenu: 普通 mod 可用; keep `experimental` because it is title-settings DTMAPI UI, not an official options-menu extension point.
- Localization: 普通 mod 可用; keep `experimental` because language source is DTMAPI-controlled rather than official settings.
- Logging: 普通 mod 可用; keep `stable`.
- ModRegistry: 普通 mod 可用 as `registry-only`; keep stable for DTMAPI API lookup but document that it is not an official enablement/load-order source of truth.

## Concrete failure modes

1. A config-menu `isVisible` or `canEdit` callback that triggers IO or registers APIs can run during `PreviewPendingValues` and observe temporary unsaved values, producing side effects that outlive preview.
2. A mod that treats `ConfigMenuRegistry.GetKeybindConflicts` as native input conflict detection can miss non-DTMAPI bindings because it only scans DTMAPI config-menu keybind items.
3. A mod using `ITranslationHelper.Language` as the game's official language can display the wrong language if Doloc Town settings differ from DTMAPI env/culture detection.
4. A mod that treats `IModRegistry.IsLoaded` as official package enablement can be wrong for official-local or Workshop mods that are indexed but skipped, disabled after load, or loaded by native systems outside DTMAPI.
5. A mod that assumes `GetApi<T>` means the underlying native owner is ready can call an experimental GameBridge API whose status is still pending or blocked.

## Minimal rebuild direction

- Keep config/logging/registry helpers in the stable public docs as DTMAPI-owned infrastructure.
- Add config-menu developer guidance: setters used by pending preview must be pure assignments; expensive side effects belong in save callbacks.
- Add a `LanguageSource` note or future property if official game language is later discoverable.
- Keep `ModRegistry` wording strict: DTMAPI loaded mod registry and DTMAPI API registry only.
- For future official settings integration, add a new bridge contract rather than expanding `IDtmConfigMenuApi` into native UI ownership.

## Evidence gaps

- No native official settings/menu owner was audited for ConfigMenu; current evidence is title settings UI and DTMAPI registry behavior only.
- No direct game-language owner was found; `TranslationService` uses env/culture plus DTMAPI files.
- No smoke was run in this pass. Existing evidence is public matrix lines 21-45 and hook-map config/title evidence lines 297-336.
