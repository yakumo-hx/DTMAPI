using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using DTMAPI.Abstractions;
using DTMAPI.Core.Json;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;

namespace DTMAPI.Core.Services
{
    internal sealed class WorkshopService : IWorkshopHelper
    {
        private readonly List<IWorkshopModInfo> mods = new List<IWorkshopModInfo>();

        public void SetMods(IEnumerable<DiscoveredMod> discovered)
        {
            mods.Clear();
            mods.AddRange(discovered.Select(m => new WorkshopModInfo(m)));
        }

        public IReadOnlyList<IWorkshopModInfo> GetOfficialMods() => mods.ToArray();
        public IReadOnlyList<IWorkshopModInfo> GetDtmApiMods() => mods.Where(m => !string.IsNullOrWhiteSpace(m.UniqueID)).ToArray();
        public bool IsOfficialEnablementManaged(IWorkshopModInfo mod) => !mod.CanDTMApiToggle || mod.Source.Equals("Workshop", StringComparison.OrdinalIgnoreCase);
        public string GetEnablementHint(IWorkshopModInfo mod)
        {
            if (IsOfficialEnablementManaged(mod))
                return "请通过 Doloc Town 官方 Mod 界面或 Steam 创意工坊启用/禁用。";
            return "本地非创意工坊 Mod；DTMAPI 可使用本地禁用标记。";
        }
    }

    internal sealed class ContentQueryService : IContentQueryHelper
    {
        private readonly RuntimePaths paths;
        private readonly List<ContentAssetInfo> assets = new List<ContentAssetInfo>();
        private readonly List<ContentItemInfo> indexedItems = new List<ContentItemInfo>();
        private readonly Dictionary<string, ContentItemInfo> indexedByItemId = new Dictionary<string, ContentItemInfo>(StringComparer.OrdinalIgnoreCase);

        public ContentQueryService(RuntimePaths paths)
        {
            this.paths = paths;
        }

        public int IndexedItemCount => indexedItems.Count;
        public int IndexedItemSourceCount => indexedItems.Select(i => i.SourceId).Distinct(StringComparer.OrdinalIgnoreCase).Count();

        public void Rebuild(IEnumerable<DiscoveredMod> discovered)
        {
            assets.Clear();
            foreach (DiscoveredMod mod in discovered)
            {
                if (!Directory.Exists(mod.RootPath))
                    continue;
                foreach (string path in Directory.GetFiles(mod.RootPath, "*.*", SearchOption.AllDirectories))
                {
                    string extension = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
                    if (extension != "json" && extension != "png" && extension != "txt" && extension != "csv")
                        continue;
                    string relative = path.Substring(mod.RootPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    assets.Add(new ContentAssetInfo(extension, relative, mod.Manifest.UniqueID, path));
                }
            }

            RebuildOfficialContentItemIndex();
        }

        public IReadOnlyList<IContentAssetInfo> FindAssets(string contentType)
        {
            return assets.Where(a => a.ContentType.Equals(contentType, StringComparison.OrdinalIgnoreCase)).Cast<IContentAssetInfo>().ToArray();
        }

        public IReadOnlyList<string> GetKnownContentTypes()
        {
            return assets.Select(a => a.ContentType).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(v => v, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        public bool TryReadTextAsset(string relativePath, out string text)
        {
            ContentAssetInfo? asset = assets.FirstOrDefault(a => a.RelativePath.Equals(relativePath, StringComparison.OrdinalIgnoreCase));
            if (asset != null && File.Exists(asset.SourcePath))
            {
                text = File.ReadAllText(asset.SourcePath);
                return true;
            }
            text = string.Empty;
            return false;
        }

        public IReadOnlyList<IContentItemInfo> GetIndexedItems() => indexedItems.Cast<IContentItemInfo>().ToArray();

        public IContentItemInfo? GetIndexedItem(string itemId)
        {
            return !string.IsNullOrWhiteSpace(itemId) && indexedByItemId.TryGetValue(itemId, out ContentItemInfo item)
                ? item
                : null;
        }

        private void RebuildOfficialContentItemIndex()
        {
            indexedItems.Clear();
            indexedByItemId.Clear();

            OfficialModEnablementIndex enablement = OfficialModEnablementIndex.Load();
            var seenRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int scanOrder = 0;

            if (Directory.Exists(enablement.LocalModsRoot))
                AddOfficialContentRoot(enablement.LocalModsRoot, isWorkshopRoot: false, enablement, seenRoots, ref scanOrder);

            string workshopRoot = Path.Combine(paths.GamePath, "steamapps", "workshop", "content", "2285550");
            if (Directory.Exists(workshopRoot))
                AddOfficialContentRoot(workshopRoot, isWorkshopRoot: true, enablement, seenRoots, ref scanOrder);

            string siblingWorkshopRoot = Path.GetFullPath(Path.Combine(paths.GamePath, "..", "..", "workshop", "content", "2285550"));
            if (Directory.Exists(siblingWorkshopRoot))
                AddOfficialContentRoot(siblingWorkshopRoot, isWorkshopRoot: true, enablement, seenRoots, ref scanOrder);

            foreach (ContentItemInfo item in indexedItems
                .GroupBy(i => i.ItemId, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderByDescending(i => i.Enabled)
                    .ThenBy(i => i.LoadOrder < 0 ? int.MaxValue : i.LoadOrder)
                    .ThenBy(i => i.SourceKind, StringComparer.OrdinalIgnoreCase)
                    .First()))
            {
                indexedByItemId[item.ItemId] = item;
            }
        }

        private void AddOfficialContentRoot(string root, bool isWorkshopRoot, OfficialModEnablementIndex enablement, HashSet<string> seenRoots, ref int scanOrder)
        {
            if (!Directory.Exists(root))
                return;

            foreach (string dir in Directory.GetDirectories(root))
            {
                string fullDir = Path.GetFullPath(dir);
                if (!seenRoots.Add(fullDir))
                    continue;

                ulong workshopId;
                ulong? parsedWorkshopId = isWorkshopRoot && ulong.TryParse(Path.GetFileName(dir), out workshopId) ? workshopId : (ulong?)null;
                AddOfficialContentDirectory(dir, parsedWorkshopId, enablement, scanOrder++);
            }
        }

        private void AddOfficialContentDirectory(string root, ulong? workshopId, OfficialModEnablementIndex enablement, int scanOrder)
        {
            string contentRoot = Path.Combine(root, "Content");
            if (!Directory.Exists(contentRoot))
                return;

            string officialId = workshopId.HasValue ? "Workshop." + workshopId.Value : "Local." + Path.GetFileName(root);
            bool enablementKnown = enablement.TryGetState(officialId, out OfficialModInfoState state);
            bool enabled = enablementKnown && state.Enabled;
            int loadOrder = enablementKnown ? state.Priority : -1;
            OfficialContentModInfoJson info = ReadOfficialInfo(root);
            Dictionary<string, string> localizedName = info.LocalizedName ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string title = FirstText(
                enablementKnown ? state.Title : string.Empty,
                localizedName.TryGetValue("schinese", out string chineseTitle) ? chineseTitle : string.Empty,
                localizedName.TryGetValue("english", out string englishTitle) ? englishTitle : string.Empty,
                info.Name,
                info.Title,
                Path.GetFileName(root));
            bool isDtmApiContent = HasDtmApiMarker(root);
            string sourceKind = isDtmApiContent ? "DTMAPI" : (workshopId.HasValue ? "Workshop" : "OfficialLocal");
            Dictionary<string, string> pngByKey = BuildPngIndex(contentRoot);

            foreach (string itemFile in Directory.GetFiles(contentRoot, "item_tbitem.json", SearchOption.AllDirectories))
            {
                OfficialContentItemJson[] items;
                try
                {
                    items = JsonFile.Read<OfficialContentItemJson[]>(itemFile) ?? Array.Empty<OfficialContentItemJson>();
                }
                catch
                {
                    continue;
                }

                string relativeContentPath = itemFile.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                foreach (OfficialContentItemJson jsonItem in items)
                {
                    string itemId = (jsonItem.Id ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(itemId))
                        continue;

                    string iconKey = FirstText(jsonItem.UiSpriteAsset?.Url ?? string.Empty, "icon_item_" + itemId);
                    string iconPath = ResolveIconPath(iconKey, itemId, pngByKey);
                    var tags = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
                    AddTag(tags, jsonItem.SubType);
                    foreach (string tag in jsonItem.Source ?? Array.Empty<string>())
                        AddTag(tags, tag);
                    AddTag(tags, sourceKind);
                    AddTag(tags, title);
                    if (workshopId.HasValue)
                        AddTag(tags, workshopId.Value.ToString());

                    indexedItems.Add(new ContentItemInfo
                    {
                        ItemId = itemId,
                        ChineseName = FirstText(jsonItem.Title?.Text ?? string.Empty, itemId),
                        EnglishName = FirstText(jsonItem.Title?.English ?? string.Empty, string.Empty),
                        Category = jsonItem.SubType ?? string.Empty,
                        Tags = tags.ToArray(),
                        IconAssetKey = iconKey,
                        IconPath = iconPath,
                        SourceKind = sourceKind,
                        SourceModTitle = title,
                        SourceId = officialId,
                        WorkshopId = workshopId,
                        Enabled = enabled,
                        EnablementKnown = enablementKnown,
                        IsDtmApiContent = isDtmApiContent,
                        RootPath = root,
                        ContentPath = relativeContentPath,
                        LoadOrder = loadOrder >= 0 ? loadOrder : scanOrder
                    });
                }
            }
        }

        private static OfficialContentModInfoJson ReadOfficialInfo(string root)
        {
            string path = Path.Combine(root, "info.json");
            if (!File.Exists(path))
                return new OfficialContentModInfoJson();
            try
            {
                return JsonFile.Read<OfficialContentModInfoJson>(path) ?? new OfficialContentModInfoJson();
            }
            catch
            {
                return new OfficialContentModInfoJson();
            }
        }

        private static bool HasDtmApiMarker(string root)
        {
            return File.Exists(Path.Combine(root, "dtmapi.manifest.json")) ||
                File.Exists(Path.Combine(root, "Content", "DTMAPI", "manifest.json")) ||
                File.Exists(Path.Combine(root, "Content", "DTMAPI", "dtmapi-package.json"));
        }

        private static Dictionary<string, string> BuildPngIndex(string contentRoot)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string path in Directory.GetFiles(contentRoot, "*.png", SearchOption.AllDirectories))
            {
                string key = Path.GetFileNameWithoutExtension(path);
                if (!result.ContainsKey(key))
                    result.Add(key, path);
            }
            return result;
        }

        private static string ResolveIconPath(string iconKey, string itemId, Dictionary<string, string> pngByKey)
        {
            if (!string.IsNullOrWhiteSpace(iconKey) && pngByKey.TryGetValue(iconKey, out string path))
                return path;
            string conventional = "icon_item_" + itemId;
            return pngByKey.TryGetValue(conventional, out path) ? path : string.Empty;
        }

        private static void AddTag(ISet<string> tags, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                tags.Add(value!.Trim());
        }

        private static string FirstText(params string[] values)
        {
            foreach (string value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }
            return string.Empty;
        }
    }

    internal sealed class ContentAssetInfo : IContentAssetInfo
    {
        public ContentAssetInfo(string contentType, string relativePath, string sourceModId, string sourcePath)
        {
            ContentType = contentType;
            RelativePath = relativePath;
            SourceModId = sourceModId;
            SourcePath = sourcePath;
        }

        public string ContentType { get; }
        public string RelativePath { get; }
        public string SourceModId { get; }
        public string SourcePath { get; }
    }

    internal sealed class ContentItemInfo : IContentItemInfo
    {
        public string ItemId { get; set; } = string.Empty;
        public string ChineseName { get; set; } = string.Empty;
        public string EnglishName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();
        public string IconAssetKey { get; set; } = string.Empty;
        public string IconPath { get; set; } = string.Empty;
        public string SourceKind { get; set; } = "Vanilla";
        public string SourceModTitle { get; set; } = string.Empty;
        public string SourceId { get; set; } = string.Empty;
        public ulong? WorkshopId { get; set; }
        public bool Enabled { get; set; }
        public bool EnablementKnown { get; set; }
        public bool IsDtmApiContent { get; set; }
        public string RootPath { get; set; } = string.Empty;
        public string ContentPath { get; set; } = string.Empty;
        public int LoadOrder { get; set; } = -1;
    }

    [DataContract]
    internal sealed class OfficialContentModInfoJson
    {
        [DataMember(Name = "name")]
        public string Name { get; set; } = string.Empty;

        [DataMember(Name = "title")]
        public string Title { get; set; } = string.Empty;

        [DataMember(Name = "localized_name")]
        public Dictionary<string, string> LocalizedName { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    [DataContract]
    internal sealed class OfficialContentItemJson
    {
        [DataMember(Name = "id")]
        public string Id { get; set; } = string.Empty;

        [DataMember(Name = "sub_type")]
        public string SubType { get; set; } = string.Empty;

        [DataMember(Name = "source")]
        public string[] Source { get; set; } = Array.Empty<string>();

        [DataMember(Name = "overlay")]
        public int Overlay { get; set; }

        [DataMember(Name = "title")]
        public OfficialContentTextJson? Title { get; set; }

        [DataMember(Name = "ui_sprite_asset")]
        public OfficialContentSpriteAssetJson? UiSpriteAsset { get; set; }
    }

    [DataContract]
    internal sealed class OfficialContentTextJson
    {
        [DataMember(Name = "text")]
        public string Text { get; set; } = string.Empty;

        [DataMember(Name = "english")]
        public string English { get; set; } = string.Empty;
    }

    [DataContract]
    internal sealed class OfficialContentSpriteAssetJson
    {
        [DataMember(Name = "url")]
        public string Url { get; set; } = string.Empty;
    }

    internal sealed class InputService : IInputHelper
    {
        private readonly HashSet<string> registered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> down = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> pressed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> suppressed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public void RegisterButton(string button)
        {
            if (!string.IsNullOrWhiteSpace(button) && !button.Equals("None", StringComparison.OrdinalIgnoreCase))
                registered.Add(button.Trim());
        }

        public void UnregisterButton(string button)
        {
            if (!string.IsNullOrWhiteSpace(button))
                registered.Remove(button.Trim());
        }

        public IReadOnlyList<string> GetRegisteredButtons() => registered.OrderBy(v => v, StringComparer.OrdinalIgnoreCase).ToArray();

        public void SetPressed(string button)
        {
            down.Add(button);
            pressed.Add(button);
        }

        public void SetReleased(string button)
        {
            down.Remove(button);
        }

        public void ClearFrame()
        {
            pressed.Clear();
            suppressed.Clear();
        }
        public bool IsDown(string button) => down.Contains(button);
        public bool WasPressed(string button) => pressed.Contains(button);
        public void Suppress(string button) => suppressed.Add(button);
        public IReadOnlyList<string> GetSuppressedButtons() => suppressed.OrderBy(v => v, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public enum DtmOverlayPage
    {
        Status,
        Mods,
        Config,
        Errors,
        Hooks,
        Logs
    }

    public sealed class UiRuntimeService : IUiHelper
    {
        private readonly Func<string> exportLogs;
        private readonly Action<string> opened;
        private readonly Action<string> closed;

        public UiRuntimeService(Func<string> exportLogs, Action<string> opened, Action<string> closed)
        {
            this.exportLogs = exportLogs;
            this.opened = opened;
            this.closed = closed;
        }

        public bool IsOpen { get; private set; }
        public DtmOverlayPage CurrentPage { get; private set; } = DtmOverlayPage.Status;
        public string ActiveMenuId { get; private set; } = string.Empty;
        public string LastExportPath { get; private set; } = string.Empty;
        public string? RequestedConfigUniqueId { get; private set; }
        public string InputContext { get; private set; } = "Unknown";
        public bool CanDrawOverlay { get; private set; } = true;
        public bool GameplayHotkeysAllowed { get; private set; } = true;
        public string DrawBoundaryReason { get; private set; } = string.Empty;
        public bool BlocksGameplayHotkeys => IsOpen || !GameplayHotkeysAllowed;
        public bool BlocksModUpdates => IsOpen;

        public void Toggle()
        {
            if (IsOpen)
                Close();
            else
                Open(DtmOverlayPage.Status);
        }

        public void Close()
        {
            if (!IsOpen)
                return;
            string menuId = string.IsNullOrWhiteSpace(ActiveMenuId) ? "DTMAPI." + CurrentPage : ActiveMenuId;
            IsOpen = false;
            ActiveMenuId = string.Empty;
            closed(menuId);
        }

        public void OpenDtmApiStatusPage() => Open(DtmOverlayPage.Status);
        public void OpenModListPage() => Open(DtmOverlayPage.Mods);
        public void OpenConfigPage(string? uniqueId = null)
        {
            RequestedConfigUniqueId = uniqueId;
            Open(DtmOverlayPage.Config);
        }
        public void OpenErrorPage() => Open(DtmOverlayPage.Errors);
        public void OpenHookStatusPage() => Open(DtmOverlayPage.Hooks);
        public void OpenLogsPage() => Open(DtmOverlayPage.Logs);
        public void SetPage(DtmOverlayPage page) => Open(page);

        public void OpenCustomMenu(string menuId)
        {
            bool wasOpen = IsOpen;
            ActiveMenuId = string.IsNullOrWhiteSpace(menuId) ? "DTMAPI.Custom" : menuId.Trim();
            IsOpen = true;
            if (!wasOpen)
                opened(ActiveMenuId);
        }

        public void SetUiContext(string inputContext, bool canDrawOverlay, bool gameplayHotkeysAllowed, string reason)
        {
            InputContext = string.IsNullOrWhiteSpace(inputContext) ? "Unknown" : inputContext;
            CanDrawOverlay = canDrawOverlay;
            GameplayHotkeysAllowed = gameplayHotkeysAllowed;
            DrawBoundaryReason = reason ?? string.Empty;
        }

        public string ExportLogs()
        {
            LastExportPath = exportLogs();
            CurrentPage = DtmOverlayPage.Logs;
            Open(DtmOverlayPage.Logs);
            return LastExportPath;
        }

        private void Open(DtmOverlayPage page)
        {
            bool wasOpen = IsOpen;
            CurrentPage = page;
            ActiveMenuId = "DTMAPI." + page;
            IsOpen = true;
            if (!wasOpen)
                opened(ActiveMenuId);
        }
    }
}
