# DolocTown ModManager / Workshop API Map

## Build

- Steam build: 23249387
- Branch: workshop
- Assembly-CSharp hash: 247ACDAA8D6ED0490A406C6FC8EB035F957D0EC1AE3F036F10E2092B8AC10367
- Research date: 2026-05-17
- Source status: Confirmed from local decompiled metadata; not yet Verified in-game unless noted.

## User-facing goals

- Reuse the official enabled-mod list as the SMAPI entry point.
- Preserve official local and Steam Workshop scanning semantics.
- Find stable postfix points for runtime refresh and diagnostics.

## Decompiled scope

- Matched types: 17
- Matched methods: 200
- Matched fields: 122
- Matched properties: 55
- Matched events: 0
- Matched call edges: 1032
- Matched strings: 128
- Raw indexes: `maps/index/ModManager_Workshop-*.csv`

## Decompiled types

| Type | Role | Stability | Notes |
| --- | --- | --- | --- |
| DolocTown.Config.ModManager | class | Confirmed | visibility=public; methods=57; fields=20 |
| DolocTown.Config.ModInfo | class | Confirmed | visibility=public; methods=35; fields=21 |
| DolocTown.Config.SteamWorkshopUploader | class | Confirmed | visibility=public; methods=15; fields=10 |
| DolocTown.Config.UI.ModMenuInfo | class | Confirmed | visibility=public; methods=17; fields=7 |
| DolocTown.Config.ModManifest | class | Confirmed | visibility=public; methods=7; fields=7 |
| DolocTown.Config.UI.TbModMenu | class | Confirmed | visibility=public; methods=8; fields=2 |
| DolocTown.Config.ModManager/<>c | class | Confirmed | visibility=private; methods=5; fields=4 |
| DolocTown.Config.WorkshopUploadResult | class | Confirmed | visibility=public; methods=4; fields=3 |
| DolocTown.Config.ModManager/<>c__DisplayClass82_0 | class | Confirmed | visibility=private; methods=2; fields=3 |
| DolocTown.Config.ModManager/<>c__DisplayClass85_0 | class | Confirmed | visibility=private; methods=2; fields=3 |
| DolocTown.Config.WorkshopUploadPlan | class | Confirmed | visibility=public; methods=3; fields=2 |
| DolocTown.Config.SteamWorkshopUploader/<>c__DisplayClass27_0 | class | Confirmed | visibility=private; methods=2; fields=2 |
| DolocTown.Config.SteamWorkshopUploader/WorkshopL10nText | struct | Confirmed | visibility=private; methods=1; fields=3 |
| DolocTown.Config.UI.ModMenuType | enum | Confirmed | visibility=public; methods=0; fields=4 |
| DolocTown.Config.ModManager/LocalModUploadPlanCache | class | Confirmed | visibility=private; methods=1; fields=2 |
| DolocTown.Config.WorkshopUploadMode | enum | Confirmed | visibility=public; methods=0; fields=3 |
| DolocTown.Config.ModWorkshopInfo | class | Confirmed | visibility=public; methods=1; fields=1 |

## Lifecycle hints

| Member | Use | Risk |
| --- | --- | --- |
| DolocAPI.FastButton_ReloadMods | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Localization.StaticTextInfo.get_UiModOpenWorkshop | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Localization.StaticTextInfo.get_UiModOpenWorkshop_l10n_key | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Localization.StaticTextInfo.set_UiModOpenWorkshop | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Localization.TbStaticText.get_UiModOpenWorkshop | candidate lifecycle or hook point | Medium |
| DolocTown.Config.ModInfo.Init | candidate lifecycle or hook point | Medium |
| DolocTown.Config.ModInfo.LoadConfigs | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.ModInfo.LoadSprites | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.ModInfo.TryLoadWorkshopInfo | candidate lifecycle or hook point | Medium |
| DolocTown.Config.ModInfo.UpdateCache | candidate lifecycle or hook point | Medium |
| DolocTown.Config.ModInfo.UpdateFlags | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.ModInfo.UpdatePlayerFlags | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.ModManager.CacheLocalModUploadPlan | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.ModManager.GetLocalModUploadPlanKey | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.ModManager.LoadSpriteFromFile | candidate lifecycle or hook point | Medium |
| DolocTown.Config.ModManager.LoadWithMods | candidate lifecycle or hook point | Medium |
| DolocTown.Config.ModManager.ProcessNextLocalModUploadPlanRequest | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.ModManager.RefreshPriority | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.ModManager.ReloadMods | candidate lifecycle or hook point | Medium |
| DolocTown.Config.ModManager.ResolveLocalModUploadPlan | candidate lifecycle or hook point | Medium |
| DolocTown.Config.ModManager.TryGetCachedLocalModUploadPlan | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.ModManager.TryGetResolvedLocalModUploadPlan | candidate lifecycle or hook point | Medium |
| DolocTown.Config.ModManager.TryLoadMod | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.ModManager.UpdateCache | candidate lifecycle or hook point | Medium |
| DolocTown.Config.ModManager.UpdateFlags | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.ModManager.UploadLocalMod | candidate lifecycle or hook point | Medium |
| DolocTown.Config.ModManager/<>c__DisplayClass82_0.<ProcessNextLocalModUploadPlanRequest>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.ModManager/<>c__DisplayClass85_0.<UploadLocalMod>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.ModManifest.GetUploadL10nIds | candidate lifecycle or hook point | Medium |
| DolocTown.Config.SteamWorkshopUploader.BuildTextsToUpload | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.SteamWorkshopUploader.CompleteUpload | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.SteamWorkshopUploader.get_IsResolvingUploadPlan | candidate lifecycle or hook point | Medium |
| DolocTown.Config.SteamWorkshopUploader.get_IsUploading | candidate lifecycle or hook point | Medium |
| DolocTown.Config.SteamWorkshopUploader.ResolveUploadPlan | candidate lifecycle or hook point | Medium |
| DolocTown.Config.SteamWorkshopUploader.UploadMod | candidate lifecycle or hook point | Medium |
| DolocTown.Config.SteamWorkshopUploader.UploadUpdate | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.GameData.DataPersistenceManager.LoadModManager | candidate lifecycle or hook point | Medium |
| DolocTown.GameData.DataPersistenceManager.SaveModManager | candidate lifecycle or hook point | Medium |
| DolocTown.GameData.IFileDataHandler.LoadModManager | candidate lifecycle or hook point | Medium |
| DolocTown.GameData.IFileDataHandler.SaveModManager | candidate lifecycle or hook point | Medium |
| DolocTown.GameData.LocalSave.LoadModManager | candidate lifecycle or hook point | Medium |
| DolocTown.GameData.LocalSave.SaveModManager | candidate lifecycle or hook point | Medium |
| DolocTown.ModUiState.OpenWorkshop | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.ModUiState.ShowUploadResultMessage | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.ModUiState/<>c.<RefreshData>b__33_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.ModUiState/<>c.<RefreshData>b__33_1 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.ModUiState/<>c__DisplayClass31_0.<RefreshCurrentModUploadPlan>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.ModUiState/<>c__DisplayClass43_0.<UploadMod>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.ModUiState/<>c__DisplayClass43_0.<UploadMod>b__2 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.UserSettings.UpdateCachedData | candidate lifecycle or hook point | Medium |

## Public members

| Member | Use | Risk |
| --- | --- | --- |
| DolocAPI.get_modManager() | direct call candidate | Medium |
| DolocTown.Config.Localization.StaticTextInfo.get_UiModOpenWorkshop() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_UiModOpenWorkshop_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_UiModSourceWorkshop() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_UiModSourceWorkshop_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.TbStaticText.get_UiModOpenWorkshop() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.TbStaticText.get_UiModSourceWorkshop() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModInfo.ClearCache() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModInfo.get_author() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModInfo.get_description() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModInfo.get_HasPlayerBodyOverride() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModInfo.get_HasPlayerHairOverride() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModInfo.get_HasPlayerOverride() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModInfo.get_icon() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModInfo.get_iconPath() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModInfo.get_manifest() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModInfo.get_previewPath() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModInfo.get_rootPath() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModInfo.get_tags() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModInfo.get_title() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModInfo.get_workshopId() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModInfo.Init(System.String rootPath; DolocTown.Config.ModManifest manifest; ModSourceType source; System.UInt64 workshopId) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModInfo.SetWorkshopId(System.UInt64 workshopId) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModInfo.ToggleEnabled() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModInfo.TryLoadWorkshopInfo(System.String rootPath; System.UInt64& workshopId) | direct call candidate | Medium |
| DolocTown.Config.ModInfo.UpdateCache() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModManager.CanMoveNext(DolocTown.Config.ModInfo modInfo) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModManager.CanMovePrev(DolocTown.Config.ModInfo modInfo) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModManager.get_CachedConfigs() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModManager.get_CachedSprites() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModManager.get_EnabledMods() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModManager.get_HasEnabledMods() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModManager.get_HasPlayerBodyOverride() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModManager.get_HasPlayerHairOverride() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModManager.get_HasPlayerOverride() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModManager.get_ModsRoot() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModManager.get_SortedModInfos() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModManager.GetAllEnabledModInfos() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModManager.GetAllValidModInfos() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModManager.GetSubscribedMods() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModManager.LoadSpriteFromFile(System.String address; UnityEngine.Sprite& asset) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModManager.LoadWithMods(System.String file; SimpleJSON.JSONNode baseJson) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModManager.MoveNext(DolocTown.Config.ModInfo modInfo) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModManager.MovePrev(DolocTown.Config.ModInfo modInfo) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModManager.QuerySubscribedMods() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModManager.ReloadMods() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModManager.ResolveLocalModUploadPlan(DolocTown.Config.ModInfo modInfo; System.Action`1<DolocTown.Config.WorkshopUploadPlan> onResolved) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModManager.SetModEnabled(System.String id; System.Boolean enabled) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModManager.ToggleModEnabled(DolocTown.Config.ModInfo modInfo) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModManager.TryGetResolvedLocalModUploadPlan(DolocTown.Config.ModInfo modInfo; DolocTown.Config.WorkshopUploadPlan& plan) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModManager.UpdateCache() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModManager.UploadLocalMod(DolocTown.Config.ModInfo modInfo; DolocTown.Config.WorkshopUploadPlan plan; System.Action`1<DolocTown.Config.WorkshopUploadResult> onCompleted) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModManifest.get_Description() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModManifest.get_Title() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModManifest.GetL10nIdDescription(System.String l10nId) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModManifest.GetL10nIdTitle(System.String l10nId) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModManifest.GetUploadL10nIds() | direct call candidate | Medium |
| DolocTown.Config.SteamWorkshopUploader.get_IsResolvingUploadPlan() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.SteamWorkshopUploader.get_IsUploading() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.SteamWorkshopUploader.ResolveUploadPlan(DolocTown.Config.ModInfo mod; System.Action`1<DolocTown.Config.WorkshopUploadPlan> onResolved) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.SteamWorkshopUploader.UploadMod(DolocTown.Config.ModInfo mod; DolocTown.Config.WorkshopUploadPlan plan; System.Action`1<DolocTown.Config.WorkshopUploadResult> onCompleted) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Tables.get_TbModMenu() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.UI.ModMenuInfo.DeserializeModMenuInfo(SimpleJSON.JSONNode _json) | direct call candidate | Medium |
| DolocTown.Config.UI.ModMenuInfo.get_IconAsset() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.UI.ModMenuInfo.get_Id() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.UI.ModMenuInfo.get_MenuTitle() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.UI.ModMenuInfo.get_MenuTitle_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.UI.ModMenuInfo.get_Title() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.UI.ModMenuInfo.get_Title_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.UI.ModMenuInfo.GetTypeId() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.UI.ModMenuInfo.Resolve(System.Collections.Generic.Dictionary`2<System.String,System.Object> _tables) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.UI.ModMenuInfo.ToString() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.UI.ModMenuInfo.TranslateText(System.Func`3<System.String,System.String,System.String> translator) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.UI.TbModMenu.Get(DolocTown.Config.UI.ModMenuType key) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.UI.TbModMenu.get_DataList() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.UI.TbModMenu.get_DataMap() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.UI.TbModMenu.get_Item(DolocTown.Config.UI.ModMenuType key) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.UI.TbModMenu.GetOrDefault(DolocTown.Config.UI.ModMenuType key) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.UI.TbModMenu.Resolve(System.Collections.Generic.Dictionary`2<System.String,System.Object> _tables) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.UI.TbModMenu.TranslateText(System.Func`3<System.String,System.String,System.String> translator) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.WorkshopUploadPlan.get_mode() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.WorkshopUploadPlan.get_workshopId() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.WorkshopUploadResult.get_mode() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.WorkshopUploadResult.get_success() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.WorkshopUploadResult.get_workshopId() | direct call candidate | Medium; needs instance source |
| DolocTown.GameData.DataPersistenceManager.get_modManager() | direct call candidate | Medium; needs instance source |
| DolocTown.GameData.DataPersistenceManager.LoadModManager() | direct call candidate | Medium; needs instance source |
| DolocTown.GameData.DataPersistenceManager.SaveModManager(DolocTown.Config.ModManager modManager) | direct call candidate | Medium; needs instance source |
| DolocTown.GameData.IFileDataHandler.LoadModManager(DolocTown.Config.ModManager& settings) | direct call candidate | Medium; needs instance source |
| DolocTown.GameData.IFileDataHandler.SaveModManager(DolocTown.Config.ModManager settings) | direct call candidate | Medium; needs instance source |
| DolocTown.GameData.LocalSave.LoadModManager(DolocTown.Config.ModManager& modManager) | direct call candidate | Medium; needs instance source |
| DolocTown.GameData.LocalSave.SaveModManager(DolocTown.Config.ModManager modManager) | direct call candidate | Medium; needs instance source |
| DolocTown.UserSettings.UpdateCachedData() | direct call candidate | Medium; needs instance source |

## Private members

| Member | Use | Risk | Fallback |
| --- | --- | --- | --- |
| DolocTown.BuildingManager.updateCache | System.Collections.Generic.HashSet`1<DolocTown.Room> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiModOpenWorkshop_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiModOpenWorkshop>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiModSourceWorkshop_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiModSourceWorkshop>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModInfo.<HasPlayerBodyOverride>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModInfo.<HasPlayerHairOverride>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModInfo.<HasPlayerOverride>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModInfo.<icon>k__BackingField | UnityEngine.Sprite | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModInfo.<iconPath>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModInfo.<manifest>k__BackingField | DolocTown.Config.ModManifest | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModInfo.<previewPath>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModInfo.<rootPath>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModInfo.<workshopId>k__BackingField | System.UInt64 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModInfo.IconImageFileName | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModInfo.IgnoreFolderName | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModInfo.ManifestFileName | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModInfo.PreviewImageFileName | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModInfo.WorkshopInfoFileName | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModManager._configTableTypesByFile | System.Collections.Generic.Dictionary`2<System.String,System.Type> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModManager.<CachedConfigs>k__BackingField | System.Collections.Generic.Dictionary`2<System.String,System.Collections.Generic.List`1<DolocTown.Config.ModInfo>> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModManager.<CachedSprites>k__BackingField | System.Collections.Generic.Dictionary`2<System.String,DolocTown.Config.ModInfo> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModManager.<EnabledMods>k__BackingField | DolocTown.Config.ModInfo[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModManager.<HasPlayerBodyOverride>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModManager.<HasPlayerHairOverride>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModManager.<HasPlayerOverride>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModManager.jsonSettings | Newtonsoft.Json.JsonSerializerSettings | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModManager.localModUploadPlanCache | System.Collections.Generic.Dictionary`2<System.String,DolocTown.Config.ModManager/LocalModUploadPlanCache> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModManager.ModFolderName | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModManager.ModInfoFileName | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModManager.modInfos | System.Collections.Generic.Dictionary`2<System.String,DolocTown.Config.ModInfo> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModManager.pendingLocalModUploadPlanCallbacks | System.Collections.Generic.Dictionary`2<System.String,System.Collections.Generic.List`1<System.Action`1<DolocTown.Config.WorkshopUploadPlan>>> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModManager.pendingLocalModUploadPlanQueue | System.Collections.Generic.Queue`1<DolocTown.Config.ModInfo> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModManager.queryCallback | Steamworks.Callback`1<Steamworks.SteamUGCQueryCompleted_t> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModManager.queuedLocalModUploadPlanKeys | System.Collections.Generic.HashSet`1<System.String> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModManager.resolvingLocalModUploadPlanKey | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModManager.sortedModInfos | System.Collections.Generic.List`1<DolocTown.Config.ModInfo> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModManager.uploader | DolocTown.Config.SteamWorkshopUploader | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModManager.WorkshopInfoFileName | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.SteamWorkshopUploader.createItemResult | Steamworks.CallResult`1<Steamworks.CreateItemResult_t> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.SteamWorkshopUploader.isSubmittingBaseUpdate | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.SteamWorkshopUploader.pendingL10nTexts | System.Collections.Generic.Queue`1<DolocTown.Config.SteamWorkshopUploader/WorkshopL10nText> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.SteamWorkshopUploader.pendingMod | DolocTown.Config.ModInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.SteamWorkshopUploader.pendingUploadMode | DolocTown.Config.WorkshopUploadMode | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.SteamWorkshopUploader.pendingWorkshopId | System.UInt64 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.SteamWorkshopUploader.queryItemDetailsResult | Steamworks.CallResult`1<Steamworks.SteamUGCQueryCompleted_t> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.SteamWorkshopUploader.resolveUploadPlanCallback | System.Action`1<DolocTown.Config.WorkshopUploadPlan> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.SteamWorkshopUploader.submitItemResult | Steamworks.CallResult`1<Steamworks.SubmitItemUpdateResult_t> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.SteamWorkshopUploader.uploadCompletedCallback | System.Action`1<DolocTown.Config.WorkshopUploadResult> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Tables.<TbModMenu>k__BackingField | DolocTown.Config.UI.TbModMenu | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.UI.ModMenuInfo.<IconAsset>k__BackingField | DolocTown.SpriteAsset | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.UI.ModMenuInfo.<Id>k__BackingField | DolocTown.Config.UI.ModMenuType | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.UI.ModMenuInfo.<MenuTitle_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.UI.ModMenuInfo.<MenuTitle>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.UI.ModMenuInfo.<Title_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.UI.ModMenuInfo.<Title>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.UI.TbModMenu._dataList | System.Collections.Generic.List`1<DolocTown.Config.UI.ModMenuInfo> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.UI.TbModMenu._dataMap | System.Collections.Generic.Dictionary`2<DolocTown.Config.UI.ModMenuType,DolocTown.Config.UI.ModMenuInfo> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.WorkshopUploadPlan.<mode>k__BackingField | DolocTown.Config.WorkshopUploadMode | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.WorkshopUploadPlan.<workshopId>k__BackingField | System.UInt64 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.WorkshopUploadResult.<mode>k__BackingField | DolocTown.Config.WorkshopUploadMode | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.WorkshopUploadResult.<success>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.WorkshopUploadResult.<workshopId>k__BackingField | System.UInt64 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.GameData.DataPersistenceManager.<modManager>k__BackingField | DolocTown.Config.ModManager | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.ModUiState.currentModInfo | DolocTown.Config.ModInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.ModUiState.currentModList | System.Collections.Generic.List`1<DolocTown.Config.ModInfo> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.UI.ModSlot.workshopIcon | UnityEngine.Transform | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.UI.ModViewer.workshopButtonGroup | DolocTown.UI.ModFunctionButtonGroup | Risky: reflection/private field | Disable dependent feature if missing |

## Events worth exposing

| Event | Trigger | Data | Risk |
| --- | --- | --- | --- |
| DolocAPI.FastButton_ReloadMods | Harmony patch candidate |  | Risky |
| DolocTown.Config.ModInfo.LoadConfigs | Harmony patch candidate |  | Risky |
| DolocTown.Config.ModInfo.LoadSprites | Harmony patch candidate |  | Risky |
| DolocTown.Config.ModInfo.TryLoadWorkshopInfo | Harmony patch candidate | System.String rootPath; System.UInt64& workshopId | Medium |
| DolocTown.Config.ModInfo.UpdateCache | Harmony patch candidate |  | Medium |
| DolocTown.Config.ModInfo.UpdateFlags | Harmony patch candidate |  | Risky |
| DolocTown.Config.ModInfo.UpdatePlayerFlags | Harmony patch candidate |  | Risky |
| DolocTown.Config.ModManager.CacheLocalModUploadPlan | Harmony patch candidate | DolocTown.Config.ModInfo modInfo; DolocTown.Config.WorkshopUploadPlan plan | Risky |
| DolocTown.Config.ModManager.GetLocalModUploadPlanKey | Harmony patch candidate | DolocTown.Config.ModInfo modInfo | Risky |
| DolocTown.Config.ModManager.LoadSpriteFromFile | Harmony patch candidate | System.String address; UnityEngine.Sprite& asset | Medium |
| DolocTown.Config.ModManager.LoadWithMods | Harmony patch candidate | System.String file; SimpleJSON.JSONNode baseJson | Medium |
| DolocTown.Config.ModManager.ProcessNextLocalModUploadPlanRequest | Harmony patch candidate |  | Risky |
| DolocTown.Config.ModManager.RefreshPriority | Harmony patch candidate |  | Risky |
| DolocTown.Config.ModManager.ReloadMods | Harmony patch candidate |  | Medium |
| DolocTown.Config.ModManager.ResolveLocalModUploadPlan | Harmony patch candidate | DolocTown.Config.ModInfo modInfo; System.Action`1<DolocTown.Config.WorkshopUploadPlan> onResolved | Medium |
| DolocTown.Config.ModManager.TryGetCachedLocalModUploadPlan | Harmony patch candidate | DolocTown.Config.ModInfo modInfo; DolocTown.Config.WorkshopUploadPlan& plan | Risky |
| DolocTown.Config.ModManager.TryGetResolvedLocalModUploadPlan | Harmony patch candidate | DolocTown.Config.ModInfo modInfo; DolocTown.Config.WorkshopUploadPlan& plan | Medium |
| DolocTown.Config.ModManager.TryLoadMod | Harmony patch candidate | System.String dir; System.String overrideId; ModSourceType source; DolocTown.Config.ModInfo& info | Risky |
| DolocTown.Config.ModManager.UpdateCache | Harmony patch candidate |  | Medium |
| DolocTown.Config.ModManager.UpdateFlags | Harmony patch candidate |  | Risky |
| DolocTown.Config.ModManager.UploadLocalMod | Harmony patch candidate | DolocTown.Config.ModInfo modInfo; DolocTown.Config.WorkshopUploadPlan plan; System.Action`1<DolocTown.Config.WorkshopUploadResult> onCompleted | Medium |
| DolocTown.Config.ModManager/<>c__DisplayClass82_0.<ProcessNextLocalModUploadPlanRequest>b__0 | Harmony patch candidate | DolocTown.Config.WorkshopUploadPlan plan | Risky |
| DolocTown.Config.ModManager/<>c__DisplayClass85_0.<UploadLocalMod>b__0 | Harmony patch candidate | DolocTown.Config.WorkshopUploadResult result | Risky |
| DolocTown.Config.ModManifest.GetUploadL10nIds | Harmony patch candidate |  | Medium |
| DolocTown.Config.SteamWorkshopUploader.BuildTextsToUpload | Harmony patch candidate | DolocTown.Config.ModInfo mod | Risky |

## Helpers worth exposing

| Helper | Methods | Stability |
| --- | --- | --- |
| `IWorkshopHelper / IModRegistry` | Query first; mutate only after two mod validations | Experimental |

## Content pack candidates

| Content type | Data source | Can add? | Can edit? |
| --- | --- | --- | --- |
| DolocTown.Config.ModInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.ModManager | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.ModManager/<>c | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.ModManager/<>c__DisplayClass82_0 | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.ModManager/<>c__DisplayClass85_0 | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.ModManager/LocalModUploadPlanCache | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.ModManifest | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.ModWorkshopInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.SteamWorkshopUploader | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.SteamWorkshopUploader/<>c__DisplayClass27_0 | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.SteamWorkshopUploader/WorkshopL10nText | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.UI.ModMenuInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.UI.ModMenuType | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.UI.TbModMenu | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.WorkshopUploadMode | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.WorkshopUploadPlan | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.WorkshopUploadResult | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |

## Verified mods

| Mod | Feature | Result |
| --- | --- | --- |
| _pending_ | Needs in-game run after API draft | Not verified |

## API proposal

```csharp
// Draft only. Keep under DolocTown.SMAPI.Experimental until two real mods verify it.
public interface IWorkshopHelper
{
    // Query methods should be stabilized before mutation methods.
}
```

## Open questions

- Which matched types are actually alive in the current scene lifecycle?
- Which private fields survive the next game update?
- Which official JSON/PNG content formats should be reused before SMAPI invents its own format?
- Existing public docs already confirm the official path does not load managed Workshop DLLs.
