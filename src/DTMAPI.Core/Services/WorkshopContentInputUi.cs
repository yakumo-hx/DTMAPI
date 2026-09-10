using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using DTMAPI.Abstractions;
using DTMAPI.Core.Json;
using DTMAPI.Core.Manager;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;

namespace DTMAPI.Core.Services
{
    internal sealed class WorkshopService : IWorkshopHelper
    {
        private IWorkshopModInfo[] publication = Array.Empty<IWorkshopModInfo>();

        public void SetMods(IEnumerable<DiscoveredMod> discovered)
        {
            IWorkshopModInfo[] candidate = (discovered ?? Array.Empty<DiscoveredMod>())
                .Select(m => (IWorkshopModInfo)new WorkshopModInfo(m))
                .ToArray();
            Volatile.Write(ref publication, candidate);
        }

        public IReadOnlyList<IWorkshopModInfo> GetOfficialMods() => Volatile.Read(ref publication).ToArray();
        public IReadOnlyList<IWorkshopModInfo> GetDtmApiMods() => Volatile.Read(ref publication).Where(m => !string.IsNullOrWhiteSpace(m.UniqueID)).ToArray();
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
        private const int MaxOfficialInputDiagnosticSamples = 8;
        private const int MaxOfficialInputDiagnosticChars = 512;
        private readonly RuntimePaths paths;
        private ContentQueryPublication publication = ContentQueryPublication.Empty;
        private int candidateBuildCount;

        private ContentQueryPublication CurrentPublication => Volatile.Read(ref publication);

        public ContentQueryService(RuntimePaths paths)
        {
            this.paths = paths;
        }

        public int IndexedItemCount => CurrentPublication.IndexedItems.Length;
        public int IndexedItemSourceCount => CurrentPublication.IndexedItemSourceCount;
        internal long LastGoodGeneration => CurrentPublication.Generation;
        internal int CandidateBuildCountForTest => candidateBuildCount;
        internal Action<ContentRefreshDirtyBatch>? BeforeGenerationCompleteForTest { get; set; }
        internal Action<string>? PreCommitFaultForTest { get; set; }
        internal Action<string>? PostCommitFaultForTest { get; set; }

        public ContentQueryRebuildResult RebuildCandidate(IEnumerable<DiscoveredMod> activeOwners, long dirtyGeneration, string reason)
        {
            ContentQueryPublication previous = CurrentPublication;
            candidateBuildCount++;
            try
            {
                // DTMAPI-owned files are a published platform root, so this input must be
                // the authoritative loaded-owner set rather than discovery candidates.
                var candidateAssets = new List<ContentAssetInfo>();
                var candidateItems = new List<ContentItemInfo>();
                var officialInputDiagnostics = new List<string>();
                int skippedOfficialInputCount = 0;
                var activeOwnerByRoot = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (DiscoveredMod mod in activeOwners ?? Array.Empty<DiscoveredMod>())
                {
                    if (!Directory.Exists(mod.RootPath))
                    {
                        throw new DirectoryNotFoundException(
                            "Active content owner '" + mod.Manifest.UniqueID + "' root is unavailable: " + mod.RootPath);
                    }
                    activeOwnerByRoot[NormalizeRootPath(mod.RootPath)] = mod.Manifest.UniqueID;
                    foreach (string path in Directory.GetFiles(mod.RootPath, "*.*", SearchOption.AllDirectories))
                    {
                        string extension = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
                        if (extension != "json" && extension != "png" && extension != "txt" && extension != "csv")
                            continue;
                        string relative = path.Substring(mod.RootPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        candidateAssets.Add(new ContentAssetInfo(extension, relative, mod.Manifest.UniqueID, path));
                    }
                }

                RebuildOfficialContentItemIndex(
                    activeOwnerByRoot,
                    candidateItems,
                    officialInputDiagnostics,
                    ref skippedOfficialInputCount);
                ContentQueryPublication candidate = ContentQueryPublication.Create(dirtyGeneration, candidateAssets, candidateItems);
                return ContentQueryRebuildResult.Prepared(
                    previous.Generation,
                    candidate.Generation,
                    candidate.Assets.Length,
                    candidate.IndexedItems.Length,
                    skippedOfficialInputCount,
                    officialInputDiagnostics,
                    reason,
                    () => Volatile.Write(ref publication, candidate));
            }
            catch (Exception ex)
            {
                return ContentQueryRebuildResult.Rejected(
                    previous.Generation,
                    previous.Assets.Length,
                    previous.IndexedItems.Length,
                    reason,
                    ex.GetType().Name + ": " + ex.Message);
            }
        }

        public int RemoveOwner(string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId))
                return 0;

            ContentQueryPublication previous = CurrentPublication;
            var nextAssets = new List<ContentAssetInfo>(previous.Assets.Length);
            int removed = 0;
            foreach (ContentAssetInfo asset in previous.Assets)
            {
                if (asset.SourceModId.Equals(ownerId, StringComparison.OrdinalIgnoreCase))
                    removed++;
                else
                    nextAssets.Add(asset);
            }

            var nextItems = new List<ContentItemInfo>(previous.IndexedItems.Length);
            foreach (ContentItemInfo current in previous.IndexedItems)
            {
                ContentItemInfo item = CloneItem(current);
                if (item.Enabled && item.OwnerId.Equals(ownerId, StringComparison.OrdinalIgnoreCase))
                {
                    item.Enabled = false;
                    removed++;
                }
                nextItems.Add(item);
            }

            if (removed > 0)
                Volatile.Write(ref publication, ContentQueryPublication.Create(previous.Generation, nextAssets, nextItems));
            return removed;
        }

        public int CountOwnerResources(string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId))
                return 0;
            ContentQueryPublication current = CurrentPublication;
            return current.Assets.Count(asset => asset.SourceModId.Equals(ownerId, StringComparison.OrdinalIgnoreCase)) +
                current.IndexedItems.Count(item => item.Enabled && item.OwnerId.Equals(ownerId, StringComparison.OrdinalIgnoreCase));
        }

        public IReadOnlyList<IContentAssetInfo> FindAssets(string contentType)
        {
            ContentQueryPublication current = CurrentPublication;
            return current.Assets.Where(a => a.ContentType.Equals(contentType, StringComparison.OrdinalIgnoreCase)).Cast<IContentAssetInfo>().ToArray();
        }

        public IReadOnlyList<string> GetKnownContentTypes()
        {
            ContentQueryPublication current = CurrentPublication;
            return current.Assets.Select(a => a.ContentType).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(v => v, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        public bool TryReadTextAsset(string relativePath, out string text)
        {
            ContentQueryPublication current = CurrentPublication;
            ContentAssetInfo? asset = current.Assets.FirstOrDefault(a => a.RelativePath.Equals(relativePath, StringComparison.OrdinalIgnoreCase));
            if (asset != null && File.Exists(asset.SourcePath))
            {
                text = File.ReadAllText(asset.SourcePath);
                return true;
            }
            text = string.Empty;
            return false;
        }

        public IReadOnlyList<IContentItemInfo> GetIndexedItems()
        {
            ContentQueryPublication current = CurrentPublication;
            return current.IndexedItems.Where(i => i.Enabled).Cast<IContentItemInfo>().ToArray();
        }

        public IReadOnlyList<IContentItemInfo> GetAllIndexedItems()
        {
            ContentQueryPublication current = CurrentPublication;
            return current.IndexedItems.Cast<IContentItemInfo>().ToArray();
        }

        public IContentItemInfo? GetIndexedItem(string itemId)
        {
            ContentQueryPublication current = CurrentPublication;
            return !string.IsNullOrWhiteSpace(itemId) && current.IndexedByItemId.TryGetValue(itemId, out ContentItemInfo item)
                ? item
                : null;
        }

        public IContentItemInfo? GetAnyIndexedItem(string itemId)
        {
            ContentQueryPublication current = CurrentPublication;
            return !string.IsNullOrWhiteSpace(itemId) && current.AllIndexedByItemId.TryGetValue(itemId, out ContentItemInfo item)
                ? item
                : null;
        }

        private void RebuildOfficialContentItemIndex(
            IReadOnlyDictionary<string, string> activeOwnerByRoot,
            List<ContentItemInfo> candidateItems,
            List<string> officialInputDiagnostics,
            ref int skippedOfficialInputCount)
        {
            OfficialModEnablementIndex enablement = OfficialModEnablementIndex.Load();
            var seenRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int scanOrder = 0;

            if (Directory.Exists(enablement.LocalModsRoot))
                AddOfficialContentRoot(enablement.LocalModsRoot, isWorkshopRoot: false, enablement, activeOwnerByRoot, seenRoots, candidateItems, officialInputDiagnostics, ref skippedOfficialInputCount, ref scanOrder);

            string workshopRoot = Path.Combine(paths.GamePath, "steamapps", "workshop", "content", "2285550");
            if (Directory.Exists(workshopRoot))
                AddOfficialContentRoot(workshopRoot, isWorkshopRoot: true, enablement, activeOwnerByRoot, seenRoots, candidateItems, officialInputDiagnostics, ref skippedOfficialInputCount, ref scanOrder);

            string siblingWorkshopRoot = Path.GetFullPath(Path.Combine(paths.GamePath, "..", "..", "workshop", "content", "2285550"));
            if (Directory.Exists(siblingWorkshopRoot))
                AddOfficialContentRoot(siblingWorkshopRoot, isWorkshopRoot: true, enablement, activeOwnerByRoot, seenRoots, candidateItems, officialInputDiagnostics, ref skippedOfficialInputCount, ref scanOrder);
        }

        private void AddOfficialContentRoot(
            string root,
            bool isWorkshopRoot,
            OfficialModEnablementIndex enablement,
            IReadOnlyDictionary<string, string> activeOwnerByRoot,
            HashSet<string> seenRoots,
            List<ContentItemInfo> candidateItems,
            List<string> officialInputDiagnostics,
            ref int skippedOfficialInputCount,
            ref int scanOrder)
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
                try
                {
                    AddOfficialContentDirectory(
                        dir,
                        parsedWorkshopId,
                        enablement,
                        activeOwnerByRoot,
                        candidateItems,
                        officialInputDiagnostics,
                        ref skippedOfficialInputCount,
                        scanOrder++);
                }
                catch (Exception ex)
                {
                    bool activeDtmApiOwner = HasDtmApiMarker(dir) && activeOwnerByRoot.ContainsKey(NormalizeRootPath(dir));
                    if (activeDtmApiOwner)
                        throw;

                    skippedOfficialInputCount++;
                    AddOfficialInputDiagnostic(
                        officialInputDiagnostics,
                        "source=" + (parsedWorkshopId.HasValue ? "Workshop." + parsedWorkshopId.Value : "Local." + Path.GetFileName(dir)) +
                        "; path=" + dir +
                        "; error=" + ex.GetType().Name + ": " + ex.Message);
                }
            }
        }

        private void AddOfficialContentDirectory(
            string root,
            ulong? workshopId,
            OfficialModEnablementIndex enablement,
            IReadOnlyDictionary<string, string> activeOwnerByRoot,
            List<ContentItemInfo> candidateItems,
            List<string> officialInputDiagnostics,
            ref int skippedOfficialInputCount,
            int scanOrder)
        {
            string contentRoot = Path.Combine(root, "Content");
            if (!Directory.Exists(contentRoot))
                return;

            string officialId = workshopId.HasValue ? "Workshop." + workshopId.Value : "Local." + Path.GetFileName(root);
            bool enablementKnown = enablement.TryGetState(officialId, out OfficialModInfoState state);
            bool sourceEnabled = enablementKnown && state.Enabled;
            if (!sourceEnabled)
                return;

            bool isDtmApiContent = HasDtmApiMarker(root);
            string ownerId = string.Empty;
            bool ownerActive = activeOwnerByRoot.TryGetValue(NormalizeRootPath(root), out ownerId);
            if (isDtmApiContent && !ownerActive)
                return;

            int loadOrder = enablementKnown ? state.Priority ?? -1 : -1;
            OfficialContentModInfoJson info = ReadOfficialInfo(root, isDtmApiContent);
            Dictionary<string, string> localizedName = info.LocalizedName ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string title = FirstText(
                enablementKnown ? state.Title : string.Empty,
                localizedName.TryGetValue("schinese", out string chineseTitle) ? chineseTitle : string.Empty,
                localizedName.TryGetValue("english", out string englishTitle) ? englishTitle : string.Empty,
                info.Name,
                info.Title,
                Path.GetFileName(root));
            // Reaching this point proves both official enablement and, for DTMAPI
            // content, an atomically activated owner. Disabled/unknown sources retain
            // package-level status elsewhere and are never parsed into item rows.
            bool enabled = true;
            string sourceKind = isDtmApiContent ? "DTMAPI" : (workshopId.HasValue ? "Workshop" : "OfficialLocal");
            Dictionary<string, string> pngByKey = BuildPngIndex(contentRoot);

            foreach (string itemFile in Directory.GetFiles(contentRoot, "item_tbitem.json", SearchOption.AllDirectories))
            {
                OfficialContentItemJson[] items;
                try
                {
                    items = isDtmApiContent
                        ? JsonFile.Read<OfficialContentItemJson[]>(itemFile) ?? Array.Empty<OfficialContentItemJson>()
                        : OfficialJsonCompatReader.Read<OfficialContentItemJson[]>(itemFile) ?? Array.Empty<OfficialContentItemJson>();
                }
                catch (Exception ex)
                {
                    if (!isDtmApiContent)
                    {
                        skippedOfficialInputCount++;
                        AddOfficialInputDiagnostic(
                            officialInputDiagnostics,
                            "source=" + officialId +
                            "; path=" + itemFile +
                            "; error=" + ex.GetType().Name + ": " + ex.Message);
                        continue;
                    }
                    throw new InvalidDataException("Failed to parse official content item candidate " + itemFile + ".", ex);
                }

                string relativeContentPath = itemFile.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                foreach (OfficialContentItemJson jsonItem in items)
                {
                    if (jsonItem == null)
                        continue;
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

                    candidateItems.Add(new ContentItemInfo
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
                        OwnerId = isDtmApiContent && ownerActive ? ownerId : string.Empty,
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

        private static OfficialContentModInfoJson ReadOfficialInfo(string root, bool isDtmApiContent)
        {
            string path = Path.Combine(root, "info.json");
            if (!File.Exists(path))
                return new OfficialContentModInfoJson();
            try
            {
                return isDtmApiContent
                    ? JsonFile.Read<OfficialContentModInfoJson>(path) ?? new OfficialContentModInfoJson()
                    : OfficialJsonCompatReader.Read<OfficialContentModInfoJson>(path) ?? new OfficialContentModInfoJson();
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

        private static void AddOfficialInputDiagnostic(List<string> diagnostics, string value)
        {
            if (diagnostics.Count >= MaxOfficialInputDiagnosticSamples)
                return;
            string normalized = (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ').Trim();
            if (normalized.Length > MaxOfficialInputDiagnosticChars)
                normalized = normalized.Substring(0, MaxOfficialInputDiagnosticChars - 3) + "...";
            diagnostics.Add(normalized);
        }

        private static string NormalizeRootPath(string root)
        {
            return Path.GetFullPath(root)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
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

        private static ContentItemInfo CloneItem(ContentItemInfo value)
        {
            return new ContentItemInfo
            {
                ItemId = value.ItemId,
                ChineseName = value.ChineseName,
                EnglishName = value.EnglishName,
                Category = value.Category,
                Tags = value.Tags.ToArray(),
                IconAssetKey = value.IconAssetKey,
                IconPath = value.IconPath,
                SourceKind = value.SourceKind,
                SourceModTitle = value.SourceModTitle,
                SourceId = value.SourceId,
                OwnerId = value.OwnerId,
                WorkshopId = value.WorkshopId,
                Enabled = value.Enabled,
                EnablementKnown = value.EnablementKnown,
                IsDtmApiContent = value.IsDtmApiContent,
                RootPath = value.RootPath,
                ContentPath = value.ContentPath,
                LoadOrder = value.LoadOrder
            };
        }

        private sealed class ContentQueryPublication
        {
            private ContentQueryPublication(
                long generation,
                ContentAssetInfo[] assets,
                ContentItemInfo[] indexedItems,
                Dictionary<string, ContentItemInfo> indexedByItemId,
                Dictionary<string, ContentItemInfo> allIndexedByItemId)
            {
                Generation = generation;
                Assets = assets;
                IndexedItems = indexedItems;
                IndexedByItemId = indexedByItemId;
                AllIndexedByItemId = allIndexedByItemId;
                IndexedItemSourceCount = indexedItems.Select(item => item.SourceId).Distinct(StringComparer.OrdinalIgnoreCase).Count();
            }

            public static ContentQueryPublication Empty { get; } = Create(0, Array.Empty<ContentAssetInfo>(), Array.Empty<ContentItemInfo>());

            public long Generation { get; }
            public ContentAssetInfo[] Assets { get; }
            public ContentItemInfo[] IndexedItems { get; }
            public Dictionary<string, ContentItemInfo> IndexedByItemId { get; }
            public Dictionary<string, ContentItemInfo> AllIndexedByItemId { get; }
            public int IndexedItemSourceCount { get; }

            public static ContentQueryPublication Create(long generation, IEnumerable<ContentAssetInfo> assets, IEnumerable<ContentItemInfo> indexedItems)
            {
                ContentAssetInfo[] assetSnapshot = (assets ?? Array.Empty<ContentAssetInfo>()).ToArray();
                ContentItemInfo[] itemSnapshot = (indexedItems ?? Array.Empty<ContentItemInfo>()).ToArray();
                var all = new Dictionary<string, ContentItemInfo>(StringComparer.OrdinalIgnoreCase);
                var enabled = new Dictionary<string, ContentItemInfo>(StringComparer.OrdinalIgnoreCase);

                foreach (ContentItemInfo item in itemSnapshot
                    .GroupBy(i => i.ItemId, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.OrderByDescending(i => i.Enabled)
                        .ThenBy(i => i.LoadOrder < 0 ? int.MaxValue : i.LoadOrder)
                        .ThenBy(i => i.SourceKind, StringComparer.OrdinalIgnoreCase)
                        .First()))
                {
                    all[item.ItemId] = item;
                }

                foreach (ContentItemInfo item in itemSnapshot
                    .Where(i => i.Enabled)
                    .GroupBy(i => i.ItemId, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.OrderBy(i => i.LoadOrder < 0 ? int.MaxValue : i.LoadOrder)
                        .ThenBy(i => i.SourceKind, StringComparer.OrdinalIgnoreCase)
                        .First()))
                {
                    enabled[item.ItemId] = item;
                }

                return new ContentQueryPublication(generation, assetSnapshot, itemSnapshot, enabled, all);
            }
        }
    }

    internal readonly struct ContentQueryRebuildResult
    {
        private ContentQueryRebuildResult(
            bool success,
            long previousGeneration,
            long currentGeneration,
            int assetCount,
            int indexedItemCount,
            int skippedOfficialInputCount,
            IReadOnlyList<string>? officialInputDiagnostics,
            bool retainedPreviousGeneration,
            string reason,
            string failure,
            Action? publishPreparedSnapshot)
        {
            Success = success;
            PreviousGeneration = previousGeneration;
            CurrentGeneration = currentGeneration;
            AssetCount = assetCount;
            IndexedItemCount = indexedItemCount;
            SkippedOfficialInputCount = skippedOfficialInputCount;
            OfficialInputDiagnostics = (officialInputDiagnostics ?? Array.Empty<string>()).ToArray();
            RetainedPreviousGeneration = retainedPreviousGeneration;
            Reason = reason ?? string.Empty;
            Failure = failure ?? string.Empty;
            PublishPreparedSnapshot = publishPreparedSnapshot;
        }

        public bool Success { get; }
        public long PreviousGeneration { get; }
        public long CurrentGeneration { get; }
        public int AssetCount { get; }
        public int IndexedItemCount { get; }
        public int SkippedOfficialInputCount { get; }
        public IReadOnlyList<string> OfficialInputDiagnostics { get; }
        public bool RetainedPreviousGeneration { get; }
        public string Reason { get; }
        public string Failure { get; }
        public Action? PublishPreparedSnapshot { get; }

        public static ContentQueryRebuildResult Prepared(
            long previousGeneration,
            long currentGeneration,
            int assetCount,
            int indexedItemCount,
            int skippedOfficialInputCount,
            IReadOnlyList<string> officialInputDiagnostics,
            string reason,
            Action publishPreparedSnapshot)
        {
            return new ContentQueryRebuildResult(true, previousGeneration, currentGeneration, assetCount, indexedItemCount, skippedOfficialInputCount, officialInputDiagnostics, false, reason, string.Empty, publishPreparedSnapshot);
        }

        public static ContentQueryRebuildResult Rejected(long lastGoodGeneration, int assetCount, int indexedItemCount, string reason, string failure)
        {
            return new ContentQueryRebuildResult(false, lastGoodGeneration, lastGoodGeneration, assetCount, indexedItemCount, 0, Array.Empty<string>(), lastGoodGeneration > 0, reason, failure, null);
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
        internal string OwnerId { get; set; } = string.Empty;
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

    internal readonly struct InputButtonSample
    {
        public InputButtonSample(string button, bool isDownNow, bool pressedEdge, bool releasedEdge)
        {
            Button = DtmButton.Normalize(button);
            IsDownNow = isDownNow;
            PressedEdge = pressedEdge;
            ReleasedEdge = releasedEdge;
        }

        public string Button { get; }
        public bool IsDownNow { get; }
        public bool PressedEdge { get; }
        public bool ReleasedEdge { get; }
    }

    internal enum InputAudienceMode
    {
        Normal,
        OwnerModal,
        PlatformModal
    }

    internal readonly struct InputAudienceSnapshot : IEquatable<InputAudienceSnapshot>
    {
        public InputAudienceSnapshot(InputAudienceMode mode, string ownerId, DtmInputScope effectiveScope, long overlaySessionSequence)
        {
            Mode = mode;
            OwnerId = string.IsNullOrWhiteSpace(ownerId) ? string.Empty : ownerId.Trim();
            EffectiveScope = effectiveScope;
            OverlaySessionSequence = overlaySessionSequence;
        }

        public InputAudienceMode Mode { get; }
        public string OwnerId { get; }
        public DtmInputScope EffectiveScope { get; }
        public long OverlaySessionSequence { get; }

        public bool Equals(InputAudienceSnapshot other) =>
            Mode == other.Mode &&
            EffectiveScope == other.EffectiveScope &&
            OverlaySessionSequence == other.OverlaySessionSequence &&
            OwnerId.Equals(other.OwnerId, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object? obj) => obj is InputAudienceSnapshot other && Equals(other);
        public override int GetHashCode() =>
            ((int)Mode * 397) ^
            ((int)EffectiveScope * 31) ^
            OverlaySessionSequence.GetHashCode() ^
            StringComparer.OrdinalIgnoreCase.GetHashCode(OwnerId);

        public static InputAudienceSnapshot Normal(DtmInputScope scope) =>
            new InputAudienceSnapshot(InputAudienceMode.Normal, string.Empty, scope, 0);
    }

    internal readonly struct InputButtonDispatch
    {
        public InputButtonDispatch(string button, string targetOwnerId)
        {
            Button = button;
            TargetOwnerId = targetOwnerId ?? string.Empty;
        }

        public string Button { get; }
        public string TargetOwnerId { get; }
        public bool IsBroadcast => TargetOwnerId.Length == 0;
    }

    internal readonly struct InputReleaseDispatch
    {
        public InputReleaseDispatch(string button, IReadOnlyList<string> recipientOwnerIds, string targetOwnerId)
        {
            Button = button;
            RecipientOwnerIds = recipientOwnerIds ?? Array.Empty<string>();
            TargetOwnerId = targetOwnerId ?? string.Empty;
        }

        public string Button { get; }
        public IReadOnlyList<string> RecipientOwnerIds { get; }
        public string TargetOwnerId { get; }
        public bool IsSettlement => RecipientOwnerIds.Count > 0;
        public bool IsBroadcast => !IsSettlement && TargetOwnerId.Length == 0;
    }

    internal readonly struct InputKeybindReleaseDispatch
    {
        public InputKeybindReleaseDispatch(InputKeybindDispatch keybind, IReadOnlyList<string> recipientOwnerIds, string targetOwnerId)
        {
            Keybind = keybind;
            RecipientOwnerIds = recipientOwnerIds ?? Array.Empty<string>();
            TargetOwnerId = targetOwnerId ?? string.Empty;
        }

        public InputKeybindDispatch Keybind { get; }
        public IReadOnlyList<string> RecipientOwnerIds { get; }
        public string TargetOwnerId { get; }
        public bool IsSettlement => RecipientOwnerIds.Count > 0;
    }

    internal sealed partial class InputService : IInputHelper
    {
        private const string GlobalOwnerId = "DTMAPI.Legacy.GlobalInput";
        private const int LocalSnapshotDormantFrameRetention = 2;
        private readonly Func<bool> ownerBoundInputEnabled;
        private readonly Action<string, string, string, string>? recordOwnerRegistration;
        private readonly Action<string, string, int, string>? recordOwnerCleanup;
        private readonly Dictionary<string, InputRegistrationState> registrationsByKey = new Dictionary<string, InputRegistrationState>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, HashSet<string>> registrationKeysByOwner = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, HashSet<string>> registrationKeysByButton = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, LocalSnapshotOwnerState> localSnapshotOwners = new Dictionary<string, LocalSnapshotOwnerState>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> activeLocalSnapshotOwnerCountsByButton = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> down = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> pressed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> released = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> suppressed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> suppressedByOwner = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> keybindDown = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> keybindPressed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> keybindReleased = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, EventDeliveryReceipt> buttonPressLedgers = new Dictionary<string, EventDeliveryReceipt>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, KeybindPressLedger> keybindPressLedgers = new Dictionary<string, KeybindPressLedger>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> settlementReleasedByOwnerButton = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> settlementReleasedButtons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> settlementReleasedKeybinds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, HashSet<string>> requiresNeutralByOwner = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> keybindRequiresNeutral = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private InputAudienceSnapshot? frameAudience;
        private InputAudienceSnapshot? lastCompletedAudience;
        private int registryVersion;
        private int cachedButtonsVersion = -1;
        private DtmInputScope cachedButtonsScope = DtmInputScope.Gameplay;
        private string[] cachedButtons = Array.Empty<string>();
        private int cachedActiveRegistrationsVersion = -1;
        private DtmInputScope cachedActiveRegistrationsScope = DtmInputScope.Gameplay;
        private readonly List<InputRegistrationState> cachedActiveRegistrations = new List<InputRegistrationState>();
        private readonly List<string> sampleButtonsBuffer = new List<string>();
        private readonly HashSet<string> sampleButtonsSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> sampledFrameButtons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> activeRegistrationKeysFrame = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> inactiveRegistrationKeysFrame = new List<string>();
        private readonly List<LocalSnapshotPruneEntry> localSnapshotPruneEntries = new List<LocalSnapshotPruneEntry>();
        private readonly List<string> localSnapshotEmptyOwners = new List<string>();
        private readonly List<string> localSnapshotButtonsBuffer = new List<string>();
        private readonly Func<InputButtonDispatch, EventDeliveryReceipt> legacyButtonPressedAdapter;
        private readonly Action<InputReleaseDispatch> legacyButtonReleasedAdapter;
        private readonly Func<InputKeybindDispatch, string, EventDeliveryReceipt> legacyKeybindPressedAdapter;
        private readonly Action<InputKeybindReleaseDispatch> legacyKeybindReleasedAdapter;
        private string[] cachedLocalSnapshotButtons = Array.Empty<string>();
        private bool localSnapshotButtonsCacheDirty;
        private long inputFrameGeneration;
        private int localSnapshotVersion;
        private int cachedButtonsLocalSnapshotVersion = -1;
        private int localSnapshotCacheRebuildCount;
        private int expiredLocalSnapshotWatchCount;
        private Action<string>? currentLegacyButtonPressed;
        private Action<string>? currentLegacyButtonReleased;
        private Action<InputKeybindDispatch>? currentLegacyKeybindPressed;
        private Action<InputKeybindDispatch>? currentLegacyKeybindReleased;
        private bool legacyDispatchActive;

        public InputService(
            Func<bool>? ownerBoundInputEnabled = null,
            Action<string, string, string, string>? recordOwnerRegistration = null,
            Action<string, string, int, string>? recordOwnerCleanup = null)
        {
            this.ownerBoundInputEnabled = ownerBoundInputEnabled ?? (() => false);
            this.recordOwnerRegistration = recordOwnerRegistration;
            this.recordOwnerCleanup = recordOwnerCleanup;
            legacyButtonPressedAdapter = DispatchLegacyButtonPressed;
            legacyButtonReleasedAdapter = DispatchLegacyButtonReleased;
            legacyKeybindPressedAdapter = DispatchLegacyKeybindPressed;
            legacyKeybindReleasedAdapter = DispatchLegacyKeybindReleased;
        }

        public IInputHelper CreateOwnerBound(string ownerId, Action? ensureOwnerActive = null)
        {
            return new OwnerBoundInputHelper(this, ownerId, ensureOwnerActive ?? (() => { }));
        }

        public void RegisterButton(string button)
        {
            RegisterButtonForOwner(GlobalOwnerId, button);
        }

        public void UnregisterButton(string button)
        {
            UnregisterButtonForOwner(GlobalOwnerId, button);
        }

        public IInputRegistration RegisterKeybind(string id, string keybindText, DtmInputScope scope = DtmInputScope.Gameplay)
        {
            return RegisterKeybindForOwner(GlobalOwnerId, id, DtmKeybindList.Parse(keybindText), scope);
        }

        public IInputRegistration RegisterKeybind(string id, DtmKeybindList keybinds, DtmInputScope scope = DtmInputScope.Gameplay)
        {
            return RegisterKeybindForOwner(GlobalOwnerId, id, keybinds, scope);
        }

        public IReadOnlyList<string> GetRegisteredButtons()
        {
            return registrationsByKey.Values
                .SelectMany(registration => registration.Keybinds.GetButtonIds())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        internal bool HasOwnerLegacyButtonRegistration(string ownerId, string button)
        {
            string normalizedOwner = NormalizeOwner(ownerId);
            string normalizedButton = NormalizeButton(button);
            if (normalizedButton.Length == 0)
                return false;

            string key = MakeRegistrationKey(normalizedOwner, "legacy:" + normalizedButton);
            return registrationsByKey.TryGetValue(key, out InputRegistrationState registration) &&
                !registration.Disposed &&
                !registration.DispatchKeybindEvents &&
                registration.ButtonIds.Any(candidate => candidate.Equals(normalizedButton, StringComparison.OrdinalIgnoreCase));
        }

        internal bool HasOwnerTypedKeybindForButton(string ownerId, string button)
        {
            string normalizedOwner = NormalizeOwner(ownerId);
            string normalizedButton = NormalizeButton(button);
            if (normalizedButton.Length == 0 || !registrationKeysByOwner.TryGetValue(normalizedOwner, out HashSet<string> ownerKeys))
                return false;

            foreach (string key in ownerKeys)
            {
                if (registrationsByKey.TryGetValue(key, out InputRegistrationState registration) &&
                    !registration.Disposed &&
                    registration.DispatchKeybindEvents &&
                    registration.ButtonIds.Any(candidate => candidate.Equals(normalizedButton, StringComparison.OrdinalIgnoreCase)))
                    return true;
            }
            return false;
        }

        public IReadOnlyList<string> GetButtonsToSample(DtmInputScope currentScope)
        {
            // This overload is retained for the internal fixture surface. Production
            // freezes an explicit InputAudienceSnapshot through the overload below.
            frameAudience = null;
            IReadOnlyList<string> result = GetButtonsToSample(InputAudienceSnapshot.Normal(currentScope));
            frameAudience = null;
            return result;
        }

        public IReadOnlyList<string> GetButtonsToSample(InputAudienceSnapshot requestedAudience)
        {
            InputAudienceSnapshot audience = FreezeAudience(requestedAudience);
            string[] activeButtons = audience.Mode == InputAudienceMode.Normal
                ? GetCachedActiveButtons(audience.EffectiveScope)
                : Array.Empty<string>();

            sampleButtonsBuffer.Clear();
            sampleButtonsSet.Clear();
            foreach (string button in down)
            {
                if (sampleButtonsSet.Add(button))
                    sampleButtonsBuffer.Add(button);
            }

            if (audience.Mode == InputAudienceMode.Normal)
            {
                foreach (string button in activeButtons)
                {
                    if (sampleButtonsSet.Add(button))
                        sampleButtonsBuffer.Add(button);
                }
            }
            else if (audience.Mode == InputAudienceMode.OwnerModal)
            {
                foreach (InputRegistrationState registration in registrationsByKey.Values)
                {
                    if (!IsRegistrationEligible(registration, audience, legacyDispatchAllowed: null))
                        continue;
                    AddButtonsToSample(registration.ButtonIds);
                }
            }

            foreach (HashSet<string> ownerButtons in requiresNeutralByOwner.Values)
            {
                foreach (string button in ownerButtons)
                {
                    if (sampleButtonsSet.Add(button))
                        sampleButtonsBuffer.Add(button);
                }
            }

            foreach (KeybindPressLedger ledger in keybindPressLedgers.Values)
                AddButtonsToSample(ledger.ButtonIds);

            foreach (string button in buttonPressLedgers.Keys)
            {
                if (sampleButtonsSet.Add(button))
                    sampleButtonsBuffer.Add(button);
            }

            sampleButtonsBuffer.Sort(StringComparer.OrdinalIgnoreCase);
            return sampleButtonsBuffer;
        }

        public InputFrameResult RecordFrame(
            DtmInputScope currentScope,
            IReadOnlyList<InputButtonSample> buttonSamples,
            Func<DtmInputScope, bool> dispatchAllowed,
            Action<string> dispatchButtonPressed,
            Action<string> dispatchButtonReleased,
            Action<InputKeybindDispatch> dispatchKeybindPressed,
            Action<InputKeybindDispatch> dispatchKeybindReleased)
        {
            InputAudienceSnapshot legacyAudience = InputAudienceSnapshot.Normal(currentScope);
            if (frameAudience.HasValue && !frameAudience.Value.Equals(legacyAudience))
                frameAudience = null;
            if (legacyDispatchActive)
                throw new InvalidOperationException("Legacy input dispatch cannot be re-entered.");

            legacyDispatchActive = true;
            currentLegacyButtonPressed = dispatchButtonPressed;
            currentLegacyButtonReleased = dispatchButtonReleased;
            currentLegacyKeybindPressed = dispatchKeybindPressed;
            currentLegacyKeybindReleased = dispatchKeybindReleased;
            try
            {
                return RecordFrame(
                    legacyAudience,
                    buttonSamples,
                    dispatchAllowed,
                    legacyButtonPressedAdapter,
                    legacyButtonReleasedAdapter,
                    legacyKeybindPressedAdapter,
                    legacyKeybindReleasedAdapter);
            }
            finally
            {
                currentLegacyButtonPressed = null;
                currentLegacyButtonReleased = null;
                currentLegacyKeybindPressed = null;
                currentLegacyKeybindReleased = null;
                legacyDispatchActive = false;
            }
        }

        private EventDeliveryReceipt DispatchLegacyButtonPressed(InputButtonDispatch dispatch)
        {
            currentLegacyButtonPressed?.Invoke(dispatch.Button);
            return EventDeliveryReceipt.Empty;
        }

        private void DispatchLegacyButtonReleased(InputReleaseDispatch dispatch) =>
            currentLegacyButtonReleased?.Invoke(dispatch.Button);

        private EventDeliveryReceipt DispatchLegacyKeybindPressed(InputKeybindDispatch dispatch, string targetOwnerId)
        {
            currentLegacyKeybindPressed?.Invoke(dispatch);
            return EventDeliveryReceipt.Empty;
        }

        private void DispatchLegacyKeybindReleased(InputKeybindReleaseDispatch dispatch) =>
            currentLegacyKeybindReleased?.Invoke(dispatch.Keybind);

        public InputFrameResult RecordFrame(
            InputAudienceSnapshot requestedAudience,
            IReadOnlyList<InputButtonSample> buttonSamples,
            Func<InputButtonDispatch, EventDeliveryReceipt> dispatchButtonPressed,
            Action<InputReleaseDispatch> dispatchButtonReleased,
            Func<InputKeybindDispatch, string, EventDeliveryReceipt> dispatchKeybindPressed,
            Action<InputKeybindReleaseDispatch> dispatchKeybindReleased)
        {
            return RecordFrame(
                requestedAudience,
                buttonSamples,
                legacyDispatchAllowed: null,
                dispatchButtonPressed,
                dispatchButtonReleased,
                dispatchKeybindPressed,
                dispatchKeybindReleased);
        }

        private InputFrameResult RecordFrame(
            InputAudienceSnapshot requestedAudience,
            IReadOnlyList<InputButtonSample> buttonSamples,
            Func<DtmInputScope, bool>? legacyDispatchAllowed,
            Func<InputButtonDispatch, EventDeliveryReceipt> dispatchButtonPressed,
            Action<InputReleaseDispatch> dispatchButtonReleased,
            Func<InputKeybindDispatch, string, EventDeliveryReceipt> dispatchKeybindPressed,
            Action<InputKeybindReleaseDispatch> dispatchKeybindReleased)
        {
            buttonSamples ??= Array.Empty<InputButtonSample>();
            InputAudienceSnapshot audience = FreezeAudience(requestedAudience);
            int sampled = 0;
            int pressedCount = 0;
            int releasedCount = 0;
            int keybindPressedCount = 0;
            int keybindReleasedCount = 0;
            sampledFrameButtons.Clear();

            for (int sampleIndex = 0; sampleIndex < buttonSamples.Count; sampleIndex++)
            {
                InputButtonSample sample = buttonSamples[sampleIndex];
                string button = sample.Button;
                if (button.Length == 0 || !sampledFrameButtons.Add(button))
                    continue;

                sampled++;
                bool isDownNow = sample.IsDownNow;
                bool wasDown = down.Contains(button);
                bool pressedNow = sample.PressedEdge || (isDownNow && !wasDown);
                bool releasedNow = sample.ReleasedEdge || (!isDownNow && wasDown);

                if (isDownNow)
                    down.Add(button);
                else
                    down.Remove(button);

                if (pressedNow)
                {
                    pressed.Add(button);
                    if (TryResolveButtonDispatch(button, audience, legacyDispatchAllowed, out string targetOwnerId) &&
                        !IsButtonSuppressedForDispatch(button, audience, targetOwnerId))
                    {
                        EventDeliveryReceipt receipt = dispatchButtonPressed != null
                            ? dispatchButtonPressed(new InputButtonDispatch(button, targetOwnerId))
                            : EventDeliveryReceipt.Empty;
                        if (receipt.HasRecipients)
                            buttonPressLedgers[button] = receipt;
                        pressedCount++;
                    }
                }

                if (releasedNow)
                {
                    released.Add(button);
                    ClearNeutralRequirementsForReleasedButton(button);
                }

                if (releasedNow && buttonPressLedgers.TryGetValue(button, out EventDeliveryReceipt buttonReceipt))
                {
                    dispatchButtonReleased?.Invoke(new InputReleaseDispatch(button, buttonReceipt.OwnerIds, string.Empty));
                    for (int ownerIndex = 0; ownerIndex < buttonReceipt.OwnerIds.Count; ownerIndex++)
                        settlementReleasedByOwnerButton.Add(MakeOwnerButtonKey(buttonReceipt.OwnerIds[ownerIndex], button));
                    settlementReleasedButtons.Add(button);
                    buttonPressLedgers.Remove(button);
                    releasedCount++;
                }
                else if (releasedNow && wasDown &&
                    TryResolveButtonDispatch(button, audience, legacyDispatchAllowed, out string releaseTargetOwnerId) &&
                    !IsButtonSuppressedForDispatch(button, audience, releaseTargetOwnerId))
                {
                    dispatchButtonReleased?.Invoke(new InputReleaseDispatch(button, Array.Empty<string>(), releaseTargetOwnerId));
                    releasedCount++;
                }
            }

            inactiveRegistrationKeysFrame.Clear();
            foreach (KeyValuePair<string, KeybindPressLedger> pair in keybindPressLedgers)
            {
                EvaluateKeybindState(pair.Value.Keybinds, out bool ledgerDown, out _, out _);
                if (ledgerDown)
                    continue;
                dispatchKeybindReleased?.Invoke(new InputKeybindReleaseDispatch(
                    pair.Value.ToDispatch(),
                    pair.Value.Receipt.OwnerIds,
                    string.Empty));
                settlementReleasedKeybinds.Add(pair.Key);
                inactiveRegistrationKeysFrame.Add(pair.Key);
                keybindReleasedCount++;
            }
            foreach (string settledKey in inactiveRegistrationKeysFrame)
                keybindPressLedgers.Remove(settledKey);

            activeRegistrationKeysFrame.Clear();
            foreach (InputRegistrationState registration in registrationsByKey.Values)
            {
                bool eligible = IsRegistrationEligible(registration, audience, legacyDispatchAllowed);
                if (!eligible)
                {
                    if (keybindDown.Remove(registration.Key))
                        MarkRegistrationRequiresNeutral(registration);
                    continue;
                }

                activeRegistrationKeysFrame.Add(registration.Key);
                if (!registration.Keybinds.IsBound)
                    continue;

                EvaluateKeybindState(registration.Keybinds, out bool isDownNow, out bool isPressedNow, out bool isReleasedNow);
                bool requiresNeutral = keybindRequiresNeutral.Contains(registration.Key);
                if (requiresNeutral && !isDownNow)
                {
                    keybindRequiresNeutral.Remove(registration.Key);
                    ClearOwnerNeutralRequirements(registration.OwnerId, registration.Keybinds);
                    requiresNeutral = false;
                }

                bool wasDown = keybindDown.Contains(registration.Key);
                bool visibleDown = isDownNow && !requiresNeutral;
                if (visibleDown)
                    keybindDown.Add(registration.Key);
                else
                    keybindDown.Remove(registration.Key);

                bool pressedDispatched = false;
                if (!requiresNeutral &&
                    (isPressedNow || (isDownNow && !wasDown)) &&
                    registration.DispatchKeybindEvents &&
                    !IsSuppressedForOwner(registration.OwnerId, registration.Keybinds, audience))
                {
                    keybindPressed.Add(registration.Key);
                    var keybindDispatch = new InputKeybindDispatch(
                        registration.OwnerId,
                        registration.Id,
                        registration.Keybinds,
                        FindTriggerButton(registration.Keybinds));
                    string targetOwnerId = audience.Mode == InputAudienceMode.OwnerModal ? audience.OwnerId : string.Empty;
                    EventDeliveryReceipt receipt = dispatchKeybindPressed != null
                        ? dispatchKeybindPressed(keybindDispatch, targetOwnerId)
                        : EventDeliveryReceipt.Empty;
                    if (receipt.HasRecipients)
                    {
                        keybindPressLedgers[registration.Key] = new KeybindPressLedger(
                            registration.OwnerId,
                            registration.Id,
                            registration.Keybinds,
                            keybindDispatch.TriggerButton,
                            receipt);
                    }
                    keybindPressedCount++;
                    pressedDispatched = true;
                }

                if (!isDownNow && (isReleasedNow || wasDown) && (wasDown || pressedDispatched) &&
                    !keybindPressLedgers.ContainsKey(registration.Key) &&
                    !settlementReleasedKeybinds.Contains(registration.Key))
                {
                    keybindReleased.Add(registration.Key);
                    if (registration.DispatchKeybindEvents && !IsSuppressedForOwner(registration.OwnerId, registration.Keybinds, audience))
                    {
                        string targetOwnerId = audience.Mode == InputAudienceMode.OwnerModal ? audience.OwnerId : string.Empty;
                        dispatchKeybindReleased?.Invoke(new InputKeybindReleaseDispatch(new InputKeybindDispatch(
                            registration.OwnerId,
                            registration.Id,
                            registration.Keybinds,
                            FindTriggerButton(registration.Keybinds)), Array.Empty<string>(), targetOwnerId));
                        keybindReleasedCount++;
                    }
                }
            }

            inactiveRegistrationKeysFrame.Clear();
            foreach (string key in keybindDown)
            {
                if (!activeRegistrationKeysFrame.Contains(key))
                    inactiveRegistrationKeysFrame.Add(key);
            }

            foreach (string inactiveKey in inactiveRegistrationKeysFrame)
            {
                keybindDown.Remove(inactiveKey);
            }

            return new InputFrameResult(sampled, pressedCount, releasedCount, keybindPressedCount, keybindReleasedCount);
        }

        public int RemoveOwner(string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId))
                return 0;

            ownerId = ownerId.Trim();
            registrationKeysByOwner.TryGetValue(ownerId, out HashSet<string> registrationKeys);

            string[] removedKeys = registrationKeys?.ToArray() ?? Array.Empty<string>();
            foreach (string registrationKey in removedKeys)
                DisposeRegistration(registrationKey, recordCleanup: false);

            if (removedKeys.Length > 0)
                recordOwnerCleanup?.Invoke(ownerId, "InputButton", removedKeys.Length, "Owner-bound input cleanup removed mod-owned keybind registrations.");
            int removedSnapshotButtons = RemoveLocalSnapshotOwner(ownerId);
            if (removedSnapshotButtons > 0)
                recordOwnerCleanup?.Invoke(ownerId, "InputSnapshotButton", removedSnapshotButtons, "Owner-bound input cleanup removed local snapshot button watches.");
            DropOwnerFrameState(ownerId);
            return removedKeys.Length + removedSnapshotButtons;
        }

        public InputOwnerSnapshot GetOwnerSnapshot()
        {
            int buttonCount = GetRegisteredButtons().Count;
            string[] ownerIds = registrationKeysByOwner.Keys
                .Concat(localSnapshotOwners.Keys)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var byOwner = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (string ownerId in ownerIds)
            {
                int registeredButtons = registrationKeysByOwner.TryGetValue(ownerId, out HashSet<string> registrationKeys)
                    ? CountOwnerButtons(registrationKeys)
                    : 0;
                int localButtons = localSnapshotOwners.TryGetValue(ownerId, out LocalSnapshotOwnerState localOwner)
                    ? localOwner.Buttons.Count
                    : 0;
                byOwner[ownerId] = registeredButtons + localButtons;
            }
            int ownerRegistrations = registrationKeysByOwner.Values.Sum(v => v.Count) + localSnapshotOwners.Values.Sum(owner => owner.Buttons.Count);
            return new InputOwnerSnapshot(true, byOwner.Count, buttonCount, ownerRegistrations, byOwner);
        }

        internal int CountOwnerResources(string ownerId)
        {
            ownerId = NormalizeOwner(ownerId);
            int registrations = registrationKeysByOwner.TryGetValue(ownerId, out HashSet<string> registrationKeys)
                ? registrationKeys.Count
                : 0;
            int localWatches = localSnapshotOwners.TryGetValue(ownerId, out LocalSnapshotOwnerState localOwner)
                ? localOwner.Buttons.Count
                : 0;
            return registrations + localWatches;
        }

        public void ClearFrame()
        {
            if (frameAudience.HasValue)
                lastCompletedAudience = frameAudience.Value;
            pressed.Clear();
            released.Clear();
            suppressed.Clear();
            suppressedByOwner.Clear();
            keybindPressed.Clear();
            keybindReleased.Clear();
            settlementReleasedByOwnerButton.Clear();
            settlementReleasedButtons.Clear();
            settlementReleasedKeybinds.Clear();
            frameAudience = null;
            CommitLocalSnapshotRequests();
        }

        public void ClearTransientState()
        {
            MarkAllRegistrationsRequiresNeutral();
            pressed.Clear();
            released.Clear();
            suppressed.Clear();
            suppressedByOwner.Clear();
            keybindDown.Clear();
            keybindPressed.Clear();
            keybindReleased.Clear();
            settlementReleasedByOwnerButton.Clear();
            settlementReleasedButtons.Clear();
            settlementReleasedKeybinds.Clear();
            frameAudience = null;
            lastCompletedAudience = null;
            ClearLocalSnapshotState();
        }

        internal InputLocalSnapshotDiagnostics GetLocalSnapshotDiagnostics()
        {
            int watchCount = 0;
            foreach (LocalSnapshotOwnerState owner in localSnapshotOwners.Values)
                watchCount += owner.Buttons.Count;
            return new InputLocalSnapshotDiagnostics(
                localSnapshotOwners.Count,
                watchCount,
                activeLocalSnapshotOwnerCountsByButton.Count,
                localSnapshotCacheRebuildCount,
                expiredLocalSnapshotWatchCount,
                inputFrameGeneration);
        }

        public DtmButtonState GetState(DtmButton button) => GetStateForOwner(GlobalOwnerId, button);
        public bool IsDown(string button) => IsDownForOwner(GlobalOwnerId, DtmButton.Parse(button));
        public bool IsDown(DtmButton button) => IsDownForOwner(GlobalOwnerId, button);
        public bool WasPressed(string button) => WasPressedForOwner(GlobalOwnerId, DtmButton.Parse(button));
        public bool WasPressed(DtmButton button) => WasPressedForOwner(GlobalOwnerId, button);
        public bool WasReleased(string button) => WasReleasedForOwner(GlobalOwnerId, DtmButton.Parse(button));
        public bool WasReleased(DtmButton button) => WasReleasedForOwner(GlobalOwnerId, button);
        public bool IsKeybindDown(string id) => TryResolveRegistrationKey(id, out string key) && keybindDown.Contains(key);
        public bool WasKeybindPressed(string id) => TryResolveRegistrationKey(id, out string key) && keybindPressed.Contains(key);
        private bool IsKeybindDownForOwner(string ownerId, string id) => keybindDown.Contains(MakeRegistrationKey(ownerId, id));
        private bool WasKeybindPressedForOwner(string ownerId, string id) => keybindPressed.Contains(MakeRegistrationKey(ownerId, id));
        public void Suppress(string button) => SuppressForOwner(GlobalOwnerId, button);

        private void SuppressForOwner(string ownerId, string button)
        {
            string normalized = NormalizeButton(button);
            if (normalized.Length == 0)
                return;
            InputAudienceSnapshot audience = GetQueryAudience();
            if (audience.Mode == InputAudienceMode.PlatformModal)
                return;
            if (audience.Mode == InputAudienceMode.OwnerModal)
            {
                if (!audience.OwnerId.Equals(NormalizeOwner(ownerId), StringComparison.OrdinalIgnoreCase))
                    return;
                suppressedByOwner.Add(MakeOwnerButtonKey(ownerId, normalized));
                return;
            }
            suppressed.Add(normalized);
        }

        internal bool IsSuppressed(string button)
        {
            string normalized = NormalizeButton(button);
            InputAudienceSnapshot audience = GetQueryAudience();
            return suppressed.Contains(normalized) ||
                (audience.Mode == InputAudienceMode.OwnerModal &&
                 suppressedByOwner.Contains(MakeOwnerButtonKey(audience.OwnerId, normalized)));
        }

        private void EvaluateKeybindState(DtmKeybindList keybinds, out bool isDownNow, out bool isPressedNow, out bool isReleasedNow)
        {
            isDownNow = false;
            isPressedNow = false;
            isReleasedNow = false;
            for (int keybindIndex = 0; keybindIndex < keybinds.KeybindCount; keybindIndex++)
            {
                DtmKeybind keybind = keybinds.GetKeybindAt(keybindIndex);
                bool chordDown = true;
                bool chordPressedEligible = true;
                bool chordReleasedEligible = true;
                bool chordPressed = false;
                bool chordReleased = false;
                for (int index = 0; index < keybind.ButtonCount; index++)
                {
                    string button = keybind.GetButtonAt(index).Id;
                    bool buttonDown = ContainsPhysicalState(down, button);
                    bool buttonPressed = ContainsPhysicalState(pressed, button);
                    bool buttonReleased = ContainsPhysicalState(released, button);
                    chordDown &= buttonDown;
                    chordPressed |= buttonPressed;
                    chordReleased |= buttonReleased;
                    if (!buttonPressed && !buttonDown && !buttonReleased)
                        chordPressedEligible = false;
                    if (!buttonReleased && !buttonDown && !buttonPressed)
                        chordReleasedEligible = false;
                }

                isDownNow |= chordDown;
                isPressedNow |= chordPressed && chordPressedEligible;
                isReleasedNow |= chordReleased && chordReleasedEligible;
            }
        }

        private static bool ContainsPhysicalState(HashSet<string> states, string button)
        {
            if (states.Contains(button))
                return true;
            if (button.Equals("Control", StringComparison.OrdinalIgnoreCase))
                return states.Contains("LeftControl") || states.Contains("RightControl");
            if (button.Equals("Shift", StringComparison.OrdinalIgnoreCase))
                return states.Contains("LeftShift") || states.Contains("RightShift");
            if (button.Equals("Alt", StringComparison.OrdinalIgnoreCase))
                return states.Contains("LeftAlt") || states.Contains("RightAlt");
            return false;
        }

        public IReadOnlyList<string> GetSuppressedButtons() => GetSuppressedButtonsForOwner(GlobalOwnerId);

        private IReadOnlyList<string> GetSuppressedButtonsForOwner(string ownerId)
        {
            InputAudienceSnapshot audience = GetQueryAudience();
            if (audience.Mode != InputAudienceMode.OwnerModal ||
                !audience.OwnerId.Equals(NormalizeOwner(ownerId), StringComparison.OrdinalIgnoreCase))
                return suppressed.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray();

            string prefix = NormalizeOwner(ownerId) + "\n";
            return suppressed
                .Concat(suppressedByOwner
                    .Where(key => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    .Select(key => key.Substring(prefix.Length)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static string NormalizeButton(string button)
        {
            return DtmButton.Normalize(button);
        }

        private void RegisterButtonForOwner(string ownerId, string button)
        {
            string normalized = NormalizeButton(button);
            ownerId = NormalizeOwner(ownerId);
            if (normalized.Length == 0)
                return;

            RegisterKeybindForOwner(ownerId, "legacy:" + normalized, new DtmKeybindList(new[] { new DtmKeybind(new[] { DtmButton.Parse(normalized) }) }), DtmInputScope.Gameplay, dispatchKeybindEvents: false);
        }

        private void UnregisterButtonForOwner(string ownerId, string button)
        {
            string normalized = NormalizeButton(button);
            ownerId = NormalizeOwner(ownerId);
            if (normalized.Length == 0)
                return;

            DisposeRegistration(MakeRegistrationKey(ownerId, "legacy:" + normalized), recordCleanup: true);
        }

        private DtmButtonState GetStateForOwner(string ownerId, DtmButton button)
        {
            ObserveLocalSnapshotButtonForOwnerIfAllowed(ownerId, button);
            return new DtmButtonState(
                button,
                IsOwnerVisibleButtonState(ownerId, button, down, includeSettlementRelease: false),
                IsOwnerVisibleButtonState(ownerId, button, pressed, includeSettlementRelease: false),
                IsOwnerVisibleButtonState(ownerId, button, released, includeSettlementRelease: true));
        }

        private bool IsDownForOwner(string ownerId, DtmButton button)
        {
            ObserveLocalSnapshotButtonForOwnerIfAllowed(ownerId, button);
            return IsOwnerVisibleButtonState(ownerId, button, down, includeSettlementRelease: false);
        }

        private bool WasPressedForOwner(string ownerId, DtmButton button)
        {
            ObserveLocalSnapshotButtonForOwnerIfAllowed(ownerId, button);
            return IsOwnerVisibleButtonState(ownerId, button, pressed, includeSettlementRelease: false);
        }

        private bool WasReleasedForOwner(string ownerId, DtmButton button)
        {
            ObserveLocalSnapshotButtonForOwnerIfAllowed(ownerId, button);
            return IsOwnerVisibleButtonState(ownerId, button, released, includeSettlementRelease: true);
        }

        private bool IsOwnerVisibleButtonState(string ownerId, DtmButton button, HashSet<string> physicalState, bool includeSettlementRelease)
        {
            if (!button.IsBound)
                return false;
            ownerId = NormalizeOwner(ownerId);
            if (includeSettlementRelease && ContainsPhysicalState(settlementReleasedButtons, button.Id))
            {
                foreach (string releasedButton in settlementReleasedButtons)
                {
                    if (PhysicalButtonsConflict(button.Id, releasedButton) &&
                        settlementReleasedByOwnerButton.Contains(MakeOwnerButtonKey(ownerId, releasedButton)))
                        return true;
                }
                return false;
            }

            InputAudienceSnapshot audience = GetQueryAudience();
            if (!IsButtonVisibleToOwner(ownerId, button.Id, audience))
                return false;
            if (RequiresNeutral(ownerId, button.Id))
                return false;
            if (IsSuppressedForOwner(ownerId, button.Id, audience))
                return false;
            return ContainsPhysicalState(physicalState, button.Id);
        }

        private void ObserveLocalSnapshotButtonForOwnerIfAllowed(string ownerId, DtmButton button)
        {
            InputAudienceSnapshot audience = GetQueryAudience();
            if (audience.Mode != InputAudienceMode.Normal)
                return;
            if (OwnerHasRegistrationForButton(ownerId, button.Id))
                return;
            ObserveLocalSnapshotButtonForOwner(ownerId, button);
        }

        private bool OwnerHasRegistrationForButton(string ownerId, string button)
        {
            ownerId = NormalizeOwner(ownerId);
            if (!registrationKeysByOwner.TryGetValue(ownerId, out HashSet<string> ownerKeys))
                return false;
            foreach (string registrationKey in ownerKeys)
            {
                if (registrationsByKey.TryGetValue(registrationKey, out InputRegistrationState registration) &&
                    !registration.Disposed && RegistrationContainsPhysicalButton(registration, button))
                    return true;
            }
            return false;
        }

        private void ObserveLocalSnapshotButtonForOwner(string ownerId, DtmButton button)
        {
            if (!button.IsBound)
                return;

            ownerId = NormalizeOwner(ownerId);
            if (!localSnapshotOwners.TryGetValue(ownerId, out LocalSnapshotOwnerState owner))
            {
                owner = new LocalSnapshotOwnerState();
                localSnapshotOwners[ownerId] = owner;
            }

            if (!owner.Buttons.TryGetValue(button.Id, out LocalSnapshotButtonWatch watch))
            {
                watch = new LocalSnapshotButtonWatch();
                owner.Buttons[button.Id] = watch;
            }
            watch.LastObservedGeneration = inputFrameGeneration;
        }

        private string[] GetLocalSnapshotButtons()
        {
            if (!localSnapshotButtonsCacheDirty)
                return cachedLocalSnapshotButtons;

            localSnapshotButtonsBuffer.Clear();
            foreach (KeyValuePair<string, int> pair in activeLocalSnapshotOwnerCountsByButton)
            {
                if (pair.Value > 0)
                    localSnapshotButtonsBuffer.Add(pair.Key);
            }
            localSnapshotButtonsBuffer.Sort(StringComparer.OrdinalIgnoreCase);
            cachedLocalSnapshotButtons = localSnapshotButtonsBuffer.Count == 0
                ? Array.Empty<string>()
                : localSnapshotButtonsBuffer.ToArray();
            localSnapshotButtonsCacheDirty = false;
            localSnapshotCacheRebuildCount++;
            return cachedLocalSnapshotButtons;
        }

        private void CommitLocalSnapshotRequests()
        {
            long observedGeneration = inputFrameGeneration;
            long nextGeneration = observedGeneration + 1;
            localSnapshotPruneEntries.Clear();
            localSnapshotEmptyOwners.Clear();

            foreach (KeyValuePair<string, LocalSnapshotOwnerState> ownerPair in localSnapshotOwners)
            {
                foreach (KeyValuePair<string, LocalSnapshotButtonWatch> buttonPair in ownerPair.Value.Buttons)
                {
                    LocalSnapshotButtonWatch watch = buttonPair.Value;
                    bool shouldBeActive = watch.LastObservedGeneration == observedGeneration;
                    if (watch.Active != shouldBeActive)
                    {
                        watch.Active = shouldBeActive;
                        AdjustActiveLocalSnapshotButton(buttonPair.Key, shouldBeActive ? 1 : -1);
                    }

                    if (!shouldBeActive && nextGeneration - watch.LastObservedGeneration > LocalSnapshotDormantFrameRetention)
                        localSnapshotPruneEntries.Add(new LocalSnapshotPruneEntry(ownerPair.Key, buttonPair.Key));
                }
            }

            foreach (LocalSnapshotPruneEntry entry in localSnapshotPruneEntries)
            {
                if (!localSnapshotOwners.TryGetValue(entry.OwnerId, out LocalSnapshotOwnerState owner) ||
                    !owner.Buttons.TryGetValue(entry.Button, out LocalSnapshotButtonWatch watch) ||
                    watch.Active ||
                    nextGeneration - watch.LastObservedGeneration <= LocalSnapshotDormantFrameRetention)
                    continue;

                owner.Buttons.Remove(entry.Button);
                expiredLocalSnapshotWatchCount++;
                if (owner.Buttons.Count == 0)
                    localSnapshotEmptyOwners.Add(entry.OwnerId);
            }

            foreach (string ownerId in localSnapshotEmptyOwners)
            {
                if (localSnapshotOwners.TryGetValue(ownerId, out LocalSnapshotOwnerState owner) && owner.Buttons.Count == 0)
                    localSnapshotOwners.Remove(ownerId);
            }

            inputFrameGeneration = nextGeneration;
        }

        private int RemoveLocalSnapshotOwner(string ownerId)
        {
            ownerId = NormalizeOwner(ownerId);
            if (!localSnapshotOwners.TryGetValue(ownerId, out LocalSnapshotOwnerState owner))
                return 0;

            int removed = owner.Buttons.Count;
            foreach (KeyValuePair<string, LocalSnapshotButtonWatch> pair in owner.Buttons)
            {
                if (pair.Value.Active)
                    AdjustActiveLocalSnapshotButton(pair.Key, -1);
            }
            localSnapshotOwners.Remove(ownerId);
            return removed;
        }

        private void AdjustActiveLocalSnapshotButton(string button, int delta)
        {
            activeLocalSnapshotOwnerCountsByButton.TryGetValue(button, out int current);
            int updated = current + delta;
            bool membershipChanged = current <= 0 || updated <= 0;
            if (updated <= 0)
                activeLocalSnapshotOwnerCountsByButton.Remove(button);
            else
                activeLocalSnapshotOwnerCountsByButton[button] = updated;

            if (membershipChanged)
            {
                localSnapshotVersion++;
                localSnapshotButtonsCacheDirty = true;
            }
        }

        private void ClearLocalSnapshotState()
        {
            bool hadActiveButtons = activeLocalSnapshotOwnerCountsByButton.Count > 0;
            localSnapshotOwners.Clear();
            activeLocalSnapshotOwnerCountsByButton.Clear();
            localSnapshotPruneEntries.Clear();
            localSnapshotEmptyOwners.Clear();
            cachedLocalSnapshotButtons = Array.Empty<string>();
            localSnapshotButtonsCacheDirty = false;
            inputFrameGeneration++;
            if (hadActiveButtons)
                localSnapshotVersion++;
        }

        private IInputRegistration RegisterKeybindForOwner(string ownerId, string id, DtmKeybindList keybinds, DtmInputScope scope, bool dispatchKeybindEvents = true, Action? ensureOwnerActive = null)
        {
            ownerId = NormalizeOwner(ownerId);
            id = NormalizeRegistrationId(id);
            string key = MakeRegistrationKey(ownerId, id);
            if (!registrationsByKey.TryGetValue(key, out InputRegistrationState registration))
            {
                registration = new InputRegistrationState(this, ownerId, id, ensureOwnerActive ?? (() => { }));
                registrationsByKey[key] = registration;
            }
            else if (ensureOwnerActive != null)
            {
                registration.EnsureOwnerActive = ensureOwnerActive;
            }

            registration.DispatchKeybindEvents = dispatchKeybindEvents;
            UpdateRegistration(registration, keybinds ?? DtmKeybindList.None, scope);
            return registration.Handle;
        }

        private void UpdateRegistration(InputRegistrationState registration, DtmKeybindList keybinds, DtmInputScope scope)
        {
            keybinds ??= DtmKeybindList.None;
            bool wasRegistered = !registration.Disposed && registration.Keybinds.IsBound;
            string[] previousButtonIds = registration.ButtonIds;
            bool unchanged = !registration.Disposed &&
                registration.Scope == scope &&
                registration.Keybinds.Equals(keybinds);
            if (unchanged)
            {
                recordOwnerRegistration?.Invoke(registration.OwnerId, "InputButton", registration.Id, "Owner-bound input keybind registration unchanged scope=" + registration.Scope + " buttons=" + registration.Keybinds + ".");
                return;
            }

            IReadOnlyList<string> computedButtonIds = keybinds.GetButtonIds();
            string[] newButtonIds = computedButtonIds as string[] ?? computedButtonIds.ToArray();
            RemoveIndexes(registration);
            registration.Keybinds = keybinds ?? DtmKeybindList.None;
            registration.ButtonIds = newButtonIds;
            registration.Scope = scope;
            registration.Disposed = false;
            keybindDown.Remove(registration.Key);
            keybindPressed.Remove(registration.Key);
            keybindReleased.Remove(registration.Key);
            if (wasRegistered)
            {
                MarkOwnerRequiresNeutral(registration.OwnerId, previousButtonIds);
                MarkOwnerRequiresNeutral(registration.OwnerId, newButtonIds);
                keybindRequiresNeutral.Add(registration.Key);
            }
            AddIndexes(registration);
            registryVersion++;
            recordOwnerRegistration?.Invoke(registration.OwnerId, "InputButton", registration.Id, "Owner-bound input keybind registration scope=" + registration.Scope + " buttons=" + registration.Keybinds + ".");
        }

        private void DisposeRegistration(string registrationKey, bool recordCleanup)
        {
            if (!registrationsByKey.TryGetValue(registrationKey, out InputRegistrationState registration))
                return;

            RemoveIndexes(registration);
            registrationsByKey.Remove(registrationKey);
            keybindDown.Remove(registrationKey);
            keybindPressed.Remove(registrationKey);
            keybindReleased.Remove(registrationKey);
            keybindRequiresNeutral.Remove(registrationKey);
            keybindPressLedgers.Remove(registrationKey);
            registryVersion++;
            registration.Disposed = true;
            if (recordCleanup)
                recordOwnerCleanup?.Invoke(registration.OwnerId, "InputButton", 1, "Owner-bound input unregister removed " + registration.Id + ".");
        }

        private void AddIndexes(InputRegistrationState registration)
        {
            if (!registrationKeysByOwner.TryGetValue(registration.OwnerId, out HashSet<string> ownerKeys))
            {
                ownerKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                registrationKeysByOwner[registration.OwnerId] = ownerKeys;
            }
            ownerKeys.Add(registration.Key);

            foreach (string button in registration.ButtonIds)
            {
                if (!registrationKeysByButton.TryGetValue(button, out HashSet<string> buttonKeys))
                {
                    buttonKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    registrationKeysByButton[button] = buttonKeys;
                }
                buttonKeys.Add(registration.Key);
            }
        }

        private void RemoveIndexes(InputRegistrationState registration)
        {
            if (registrationKeysByOwner.TryGetValue(registration.OwnerId, out HashSet<string> ownerKeys))
            {
                ownerKeys.Remove(registration.Key);
                if (ownerKeys.Count == 0)
                    registrationKeysByOwner.Remove(registration.OwnerId);
            }

            foreach (string button in registration.ButtonIds)
            {
                    if (registrationKeysByButton.TryGetValue(button, out HashSet<string> buttonKeys))
                {
                    buttonKeys.Remove(registration.Key);
                    if (buttonKeys.Count == 0)
                    {
                        registrationKeysByButton.Remove(button);
                    }
                }
            }
        }

        private string[] GetCachedActiveButtons(DtmInputScope currentScope)
        {
            if (cachedButtonsVersion == registryVersion &&
                cachedButtonsLocalSnapshotVersion == localSnapshotVersion &&
                cachedButtonsScope == currentScope)
                return cachedButtons;

            cachedButtonsVersion = registryVersion;
            cachedButtonsLocalSnapshotVersion = localSnapshotVersion;
            cachedButtonsScope = currentScope;
            sampleButtonsBuffer.Clear();
            sampleButtonsSet.Clear();
            foreach (InputRegistrationState registration in GetCachedActiveRegistrations(currentScope))
            {
                foreach (string button in registration.ButtonIds)
                {
                    if (sampleButtonsSet.Add(button))
                        sampleButtonsBuffer.Add(button);
                }
            }

            foreach (string button in GetLocalSnapshotButtons())
            {
                if (sampleButtonsSet.Add(button))
                    sampleButtonsBuffer.Add(button);
            }

            sampleButtonsBuffer.Sort(StringComparer.OrdinalIgnoreCase);
            cachedButtons = sampleButtonsBuffer.ToArray();
            return cachedButtons;
        }

        private sealed class LocalSnapshotOwnerState
        {
            public Dictionary<string, LocalSnapshotButtonWatch> Buttons { get; } = new Dictionary<string, LocalSnapshotButtonWatch>(StringComparer.OrdinalIgnoreCase);
        }

        private sealed class LocalSnapshotButtonWatch
        {
            public long LastObservedGeneration { get; set; }

            public bool Active { get; set; }
        }

        private readonly struct LocalSnapshotPruneEntry
        {
            public LocalSnapshotPruneEntry(string ownerId, string button)
            {
                OwnerId = ownerId;
                Button = button;
            }

            public string OwnerId { get; }

            public string Button { get; }
        }

        private List<InputRegistrationState> GetCachedActiveRegistrations(DtmInputScope currentScope)
        {
            if (cachedActiveRegistrationsVersion == registryVersion && cachedActiveRegistrationsScope == currentScope)
                return cachedActiveRegistrations;

            cachedActiveRegistrationsVersion = registryVersion;
            cachedActiveRegistrationsScope = currentScope;
            cachedActiveRegistrations.Clear();
            foreach (InputRegistrationState registration in registrationsByKey.Values)
            {
                if (!registration.Disposed && ScopeMatches(registration.Scope, currentScope))
                    cachedActiveRegistrations.Add(registration);
            }
            return cachedActiveRegistrations;
        }

        private InputAudienceSnapshot FreezeAudience(InputAudienceSnapshot requestedAudience)
        {
            if (frameAudience.HasValue)
                return frameAudience.Value;

            ApplyAudienceEligibilityBoundary(requestedAudience);
            frameAudience = requestedAudience;
            return requestedAudience;
        }

        private InputAudienceSnapshot GetQueryAudience()
        {
            if (frameAudience.HasValue)
                return frameAudience.GetValueOrDefault();
            if (lastCompletedAudience.HasValue)
                return lastCompletedAudience.GetValueOrDefault();
            return InputAudienceSnapshot.Normal(DtmInputScope.Gameplay);
        }

        private void ApplyAudienceEligibilityBoundary(InputAudienceSnapshot audience)
        {
            if (!lastCompletedAudience.HasValue)
                return;

            InputAudienceSnapshot previous = lastCompletedAudience.Value;
            foreach (InputRegistrationState registration in registrationsByKey.Values)
            {
                bool wasEligible = IsRegistrationEligible(registration, previous, legacyDispatchAllowed: null);
                bool isEligible = IsRegistrationEligible(registration, audience, legacyDispatchAllowed: null);
                if (wasEligible && !isEligible)
                    MarkRegistrationRequiresNeutral(registration);
            }

            if (previous.Mode == InputAudienceMode.Normal && audience.Mode != InputAudienceMode.Normal)
            {
                foreach (KeyValuePair<string, LocalSnapshotOwnerState> ownerPair in localSnapshotOwners)
                {
                    foreach (string button in ownerPair.Value.Buttons.Keys)
                        MarkOwnerRequiresNeutral(ownerPair.Key, button);
                }
            }
        }

        private bool IsRegistrationEligible(
            InputRegistrationState registration,
            InputAudienceSnapshot audience,
            Func<DtmInputScope, bool>? legacyDispatchAllowed)
        {
            if (registration.Disposed)
                return false;
            if (legacyDispatchAllowed != null && !legacyDispatchAllowed(registration.Scope))
                return false;
            if (audience.Mode == InputAudienceMode.PlatformModal)
                return false;
            if (audience.Mode == InputAudienceMode.OwnerModal &&
                !registration.OwnerId.Equals(audience.OwnerId, StringComparison.OrdinalIgnoreCase))
                return false;
            if (audience.Mode == InputAudienceMode.OwnerModal && registration.Scope == DtmInputScope.Gameplay)
                return false;
            return ScopeMatches(registration.Scope, audience.EffectiveScope);
        }

        private bool TryResolveButtonDispatch(
            string button,
            InputAudienceSnapshot audience,
            Func<DtmInputScope, bool>? legacyDispatchAllowed,
            out string targetOwnerId)
        {
            targetOwnerId = string.Empty;
            if (audience.Mode == InputAudienceMode.PlatformModal)
                return false;

            foreach (InputRegistrationState registration in registrationsByKey.Values)
            {
                if (!IsRegistrationEligible(registration, audience, legacyDispatchAllowed) ||
                    !RegistrationContainsPhysicalButton(registration, button) ||
                    RequiresNeutral(registration.OwnerId, button))
                    continue;
                if (audience.Mode == InputAudienceMode.OwnerModal)
                    targetOwnerId = audience.OwnerId;
                return true;
            }
            return false;
        }

        private bool IsButtonVisibleToOwner(string ownerId, string button, InputAudienceSnapshot audience)
        {
            if (audience.Mode == InputAudienceMode.PlatformModal)
                return false;
            if (audience.Mode == InputAudienceMode.OwnerModal &&
                !audience.OwnerId.Equals(ownerId, StringComparison.OrdinalIgnoreCase))
                return false;

            if (registrationKeysByOwner.TryGetValue(ownerId, out HashSet<string> ownerKeys))
            {
                foreach (string registrationKey in ownerKeys)
                {
                    if (registrationsByKey.TryGetValue(registrationKey, out InputRegistrationState registration) &&
                        IsRegistrationEligible(registration, audience, legacyDispatchAllowed: null) &&
                        RegistrationContainsPhysicalButton(registration, button))
                        return true;
                }
            }

            if (audience.Mode != InputAudienceMode.Normal ||
                !localSnapshotOwners.TryGetValue(ownerId, out LocalSnapshotOwnerState localOwner) ||
                !localOwner.Buttons.TryGetValue(button, out LocalSnapshotButtonWatch watch))
                return false;
            return watch.Active || watch.LastObservedGeneration == inputFrameGeneration;
        }

        private static bool RegistrationContainsPhysicalButton(InputRegistrationState registration, string physicalButton)
        {
            foreach (string registeredButton in registration.ButtonIds)
            {
                if (PhysicalButtonsConflict(registeredButton, physicalButton))
                    return true;
            }
            return false;
        }

        private void AddButtonsToSample(IReadOnlyList<string> buttonIds)
        {
            for (int index = 0; index < buttonIds.Count; index++)
            {
                string button = buttonIds[index];
                if (sampleButtonsSet.Add(button))
                    sampleButtonsBuffer.Add(button);
            }
        }

        private bool IsButtonSuppressedForDispatch(string button, InputAudienceSnapshot audience, string targetOwnerId)
        {
            if (suppressed.Contains(button))
                return true;
            return audience.Mode == InputAudienceMode.OwnerModal &&
                targetOwnerId.Length > 0 &&
                suppressedByOwner.Contains(MakeOwnerButtonKey(targetOwnerId, button));
        }

        private bool IsSuppressedForOwner(string ownerId, string button, InputAudienceSnapshot audience)
        {
            if (suppressed.Contains(button))
                return true;
            return audience.Mode == InputAudienceMode.OwnerModal &&
                suppressedByOwner.Contains(MakeOwnerButtonKey(ownerId, button));
        }

        private bool IsSuppressedForOwner(string ownerId, DtmKeybindList keybinds, InputAudienceSnapshot audience)
        {
            foreach (DtmKeybind keybind in keybinds.Keybinds)
            {
                foreach (DtmButton button in keybind.Buttons)
                {
                    if (DtmButton.MatchesPhysicalState(button.Id, candidate => IsSuppressedForOwner(ownerId, candidate, audience)))
                        return true;
                }
            }
            return false;
        }

        private void MarkRegistrationRequiresNeutral(InputRegistrationState registration)
        {
            keybindRequiresNeutral.Add(registration.Key);
            MarkOwnerRequiresNeutral(registration.OwnerId, registration.ButtonIds);
        }

        private void MarkAllRegistrationsRequiresNeutral()
        {
            foreach (InputRegistrationState registration in registrationsByKey.Values)
                MarkRegistrationRequiresNeutral(registration);
            foreach (KeyValuePair<string, LocalSnapshotOwnerState> ownerPair in localSnapshotOwners)
            {
                foreach (string button in ownerPair.Value.Buttons.Keys)
                    MarkOwnerRequiresNeutral(ownerPair.Key, button);
            }
        }

        private void MarkOwnerRequiresNeutral(string ownerId, IReadOnlyList<string> buttonIds)
        {
            for (int index = 0; index < buttonIds.Count; index++)
                MarkOwnerRequiresNeutral(ownerId, buttonIds[index]);
        }

        private void MarkOwnerRequiresNeutral(string ownerId, string button)
        {
            ownerId = NormalizeOwner(ownerId);
            button = NormalizeButton(button);
            if (button.Length == 0)
                return;
            if (!requiresNeutralByOwner.TryGetValue(ownerId, out HashSet<string> ownerButtons))
            {
                ownerButtons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                requiresNeutralByOwner[ownerId] = ownerButtons;
            }
            ownerButtons.Add(button);
        }

        private bool RequiresNeutral(string ownerId, string button)
        {
            ownerId = NormalizeOwner(ownerId);
            if (!requiresNeutralByOwner.TryGetValue(ownerId, out HashSet<string> ownerButtons))
                return false;
            foreach (string requiredButton in ownerButtons)
            {
                if (PhysicalButtonsConflict(requiredButton, button))
                    return true;
            }
            return false;
        }

        private void ClearNeutralRequirementsForReleasedButton(string releasedButton)
        {
            localSnapshotEmptyOwners.Clear();
            foreach (KeyValuePair<string, HashSet<string>> ownerPair in requiresNeutralByOwner)
            {
                inactiveRegistrationKeysFrame.Clear();
                foreach (string requiredButton in ownerPair.Value)
                {
                    if (PhysicalButtonsConflict(requiredButton, releasedButton) &&
                        !ContainsPhysicalState(down, requiredButton))
                        inactiveRegistrationKeysFrame.Add(requiredButton);
                }
                foreach (string neutralButton in inactiveRegistrationKeysFrame)
                    ownerPair.Value.Remove(neutralButton);
                if (ownerPair.Value.Count == 0)
                    localSnapshotEmptyOwners.Add(ownerPair.Key);
            }
            foreach (string emptyOwner in localSnapshotEmptyOwners)
                requiresNeutralByOwner.Remove(emptyOwner);

            inactiveRegistrationKeysFrame.Clear();
            foreach (string registrationKey in keybindRequiresNeutral)
            {
                if (!registrationsByKey.TryGetValue(registrationKey, out InputRegistrationState registration))
                {
                    inactiveRegistrationKeysFrame.Add(registrationKey);
                    continue;
                }

                bool anyDown = false;
                for (int index = 0; index < registration.ButtonIds.Length; index++)
                {
                    if (!ContainsPhysicalState(down, registration.ButtonIds[index]))
                        continue;
                    anyDown = true;
                    break;
                }
                if (!anyDown)
                    inactiveRegistrationKeysFrame.Add(registrationKey);
            }
            foreach (string neutralRegistration in inactiveRegistrationKeysFrame)
                keybindRequiresNeutral.Remove(neutralRegistration);
        }

        private void ClearOwnerNeutralRequirements(string ownerId, DtmKeybindList keybinds)
        {
            ownerId = NormalizeOwner(ownerId);
            if (!requiresNeutralByOwner.TryGetValue(ownerId, out HashSet<string> ownerButtons))
                return;
            foreach (string button in keybinds.GetButtonIds())
            {
                if (!ContainsPhysicalState(down, button))
                    ownerButtons.Remove(button);
            }
            if (ownerButtons.Count == 0)
                requiresNeutralByOwner.Remove(ownerId);
        }

        private void DropOwnerFrameState(string ownerId)
        {
            ownerId = NormalizeOwner(ownerId);
            requiresNeutralByOwner.Remove(ownerId);
            suppressedByOwner.RemoveWhere(key => key.StartsWith(ownerId + "\n", StringComparison.OrdinalIgnoreCase));
            settlementReleasedByOwnerButton.RemoveWhere(key => key.StartsWith(ownerId + "\n", StringComparison.OrdinalIgnoreCase));

            foreach (string button in buttonPressLedgers.Keys.ToArray())
            {
                EventDeliveryReceipt filtered = FilterReceipt(buttonPressLedgers[button], ownerId);
                if (filtered.HasRecipients)
                    buttonPressLedgers[button] = filtered;
                else
                    buttonPressLedgers.Remove(button);
            }
            foreach (string key in keybindPressLedgers.Keys.ToArray())
            {
                KeybindPressLedger ledger = keybindPressLedgers[key];
                if (ledger.OwnerId.Equals(ownerId, StringComparison.OrdinalIgnoreCase))
                {
                    keybindPressLedgers.Remove(key);
                    continue;
                }
                EventDeliveryReceipt filtered = FilterReceipt(ledger.Receipt, ownerId);
                if (filtered.HasRecipients)
                    keybindPressLedgers[key] = ledger.WithReceipt(filtered);
                else
                    keybindPressLedgers.Remove(key);
            }
        }

        private static EventDeliveryReceipt FilterReceipt(EventDeliveryReceipt receipt, string removedOwnerId)
        {
            string[] remaining = receipt.OwnerIds
                .Where(owner => !owner.Equals(removedOwnerId, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            return remaining.Length == 0 ? EventDeliveryReceipt.Empty : new EventDeliveryReceipt(remaining);
        }

        private static string MakeOwnerButtonKey(string ownerId, string button) =>
            NormalizeOwner(ownerId) + "\n" + NormalizeButton(button);

        private static bool PhysicalButtonsConflict(string left, string right)
        {
            if (left.Equals(right, StringComparison.OrdinalIgnoreCase))
                return true;
            return BelongsToModifierFamily(left, right, "Control", "LeftControl", "RightControl") ||
                BelongsToModifierFamily(left, right, "Shift", "LeftShift", "RightShift") ||
                BelongsToModifierFamily(left, right, "Alt", "LeftAlt", "RightAlt");
        }

        private static bool BelongsToModifierFamily(
            string left,
            string right,
            string generic,
            string leftPhysical,
            string rightPhysical)
        {
            bool leftGeneric = left.Equals(generic, StringComparison.OrdinalIgnoreCase);
            bool rightGeneric = right.Equals(generic, StringComparison.OrdinalIgnoreCase);
            if (leftGeneric)
            {
                return rightGeneric ||
                    right.Equals(leftPhysical, StringComparison.OrdinalIgnoreCase) ||
                    right.Equals(rightPhysical, StringComparison.OrdinalIgnoreCase);
            }
            if (!rightGeneric)
                return false;
            return left.Equals(leftPhysical, StringComparison.OrdinalIgnoreCase) ||
                left.Equals(rightPhysical, StringComparison.OrdinalIgnoreCase);
        }

        private string FindTriggerButton(DtmKeybindList keybinds)
        {
            string fallback = string.Empty;
            foreach (DtmKeybind keybind in keybinds.Keybinds)
            {
                foreach (DtmButton button in keybind.Buttons)
                {
                    if (fallback.Length == 0)
                        fallback = button.Id;
                    if (DtmButton.MatchesPhysicalState(button.Id, pressed.Contains) ||
                        DtmButton.MatchesPhysicalState(button.Id, released.Contains))
                        return button.Id;
                }
            }
            return fallback;
        }

        private bool TryResolveRegistrationKey(string id, out string key)
        {
            id = NormalizeRegistrationId(id);
            key = MakeRegistrationKey(GlobalOwnerId, id);
            if (registrationsByKey.ContainsKey(key) || keybindDown.Contains(key) || keybindPressed.Contains(key))
                return true;
            foreach (InputRegistrationState registration in registrationsByKey.Values)
            {
                if (registration.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
                {
                    key = registration.Key;
                    return true;
                }
            }
            return false;
        }

        private int CountOwnerButtons(HashSet<string> registrationKeys)
        {
            return registrationKeys
                .Select(key => registrationsByKey.TryGetValue(key, out InputRegistrationState registration) ? registration : null)
                .Where(registration => registration != null)
                .SelectMany(registration => registration!.Keybinds.GetButtonIds())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
        }

        private static bool ScopeMatches(DtmInputScope registrationScope, DtmInputScope currentScope)
        {
            if (registrationScope == DtmInputScope.Always)
                return true;
            if (registrationScope == DtmInputScope.SaveLoaded)
                return currentScope == DtmInputScope.SaveLoaded || currentScope == DtmInputScope.Gameplay;
            return registrationScope == currentScope;
        }

        private static string MakeRegistrationKey(string ownerId, string id)
        {
            return NormalizeOwner(ownerId) + "\n" + NormalizeRegistrationId(id);
        }

        private static string NormalizeRegistrationId(string id)
        {
            return string.IsNullOrWhiteSpace(id) ? "default" : id.Trim();
        }

        private static string NormalizeOwner(string ownerId)
        {
            return string.IsNullOrWhiteSpace(ownerId) ? "unknown" : ownerId.Trim();
        }

        private sealed class KeybindPressLedger
        {
            public KeybindPressLedger(
                string ownerId,
                string keybindId,
                DtmKeybindList keybinds,
                string triggerButton,
                EventDeliveryReceipt receipt)
            {
                OwnerId = ownerId;
                KeybindId = keybindId;
                Keybinds = keybinds;
                TriggerButton = triggerButton;
                Receipt = receipt;
                IReadOnlyList<string> buttonIds = keybinds.GetButtonIds();
                ButtonIds = buttonIds as string[] ?? buttonIds.ToArray();
            }

            public string OwnerId { get; }
            public string KeybindId { get; }
            public DtmKeybindList Keybinds { get; }
            public string TriggerButton { get; }
            public EventDeliveryReceipt Receipt { get; }
            public string[] ButtonIds { get; }

            public InputKeybindDispatch ToDispatch() =>
                new InputKeybindDispatch(OwnerId, KeybindId, Keybinds, TriggerButton);

            public KeybindPressLedger WithReceipt(EventDeliveryReceipt receipt) =>
                new KeybindPressLedger(OwnerId, KeybindId, Keybinds, TriggerButton, receipt);
        }

        private sealed class InputRegistrationState
        {
            public InputRegistrationState(InputService input, string ownerId, string id, Action ensureOwnerActive)
            {
                OwnerId = ownerId;
                Id = id;
                Key = MakeRegistrationKey(ownerId, id);
                EnsureOwnerActive = ensureOwnerActive ?? (() => { });
                Handle = new InputRegistrationHandle(input, this);
            }

            public string OwnerId { get; }
            public string Id { get; }
            public string Key { get; }
            public DtmKeybindList Keybinds { get; set; } = DtmKeybindList.None;
            public string[] ButtonIds { get; set; } = Array.Empty<string>();
            public DtmInputScope Scope { get; set; } = DtmInputScope.Gameplay;
            public bool DispatchKeybindEvents { get; set; } = true;
            public bool Disposed { get; set; }
            public Action EnsureOwnerActive { get; set; }
            public InputRegistrationHandle Handle { get; }
        }

        private sealed class InputRegistrationHandle : IInputRegistration
        {
            private readonly InputService input;
            private readonly InputRegistrationState state;

            public InputRegistrationHandle(InputService input, InputRegistrationState state)
            {
                this.input = input;
                this.state = state;
            }

            public string Id => state.Id;
            public string OwnerId => state.OwnerId;
            public DtmKeybindList Keybinds => state.Keybinds;
            public DtmInputScope Scope => state.Scope;
            public bool IsDisposed => state.Disposed;
            public void Update(string keybindText, DtmInputScope scope = DtmInputScope.Gameplay) => Update(DtmKeybindList.Parse(keybindText), scope);
            public void Update(DtmKeybindList keybinds, DtmInputScope scope = DtmInputScope.Gameplay)
            {
                if (state.Disposed)
                    throw new ObjectDisposedException("InputRegistration");
                state.EnsureOwnerActive();
                input.UpdateRegistration(state, keybinds ?? DtmKeybindList.None, scope);
            }

            public void Dispose()
            {
                if (state.Disposed)
                    return;
                input.DisposeRegistration(state.Key, recordCleanup: true);
            }
        }

        private sealed class OwnerBoundInputHelper : IInputHelper
        {
            private readonly InputService input;
            private readonly string ownerId;
            private readonly Action ensureOwnerActive;

            public OwnerBoundInputHelper(InputService input, string ownerId, Action ensureOwnerActive)
            {
                this.input = input;
                this.ownerId = NormalizeOwner(ownerId);
                this.ensureOwnerActive = ensureOwnerActive;
            }

            public void RegisterButton(string button) { ensureOwnerActive(); input.RegisterButtonForOwner(ownerId, button); }
            public void UnregisterButton(string button) { ensureOwnerActive(); input.UnregisterButtonForOwner(ownerId, button); }
            public IInputRegistration RegisterKeybind(string id, string keybindText, DtmInputScope scope = DtmInputScope.Gameplay) { ensureOwnerActive(); return input.RegisterKeybindForOwner(ownerId, id, DtmKeybindList.Parse(keybindText), scope, ensureOwnerActive: ensureOwnerActive); }
            public IInputRegistration RegisterKeybind(string id, DtmKeybindList keybinds, DtmInputScope scope = DtmInputScope.Gameplay) { ensureOwnerActive(); return input.RegisterKeybindForOwner(ownerId, id, keybinds, scope, ensureOwnerActive: ensureOwnerActive); }
            public IReadOnlyList<string> GetRegisteredButtons() { ensureOwnerActive(); return input.GetRegisteredButtons(); }
            public DtmButtonState GetState(DtmButton button) { ensureOwnerActive(); return input.GetStateForOwner(ownerId, button); }
            public bool IsDown(string button) { ensureOwnerActive(); return input.IsDownForOwner(ownerId, DtmButton.Parse(button)); }
            public bool IsDown(DtmButton button) { ensureOwnerActive(); return input.IsDownForOwner(ownerId, button); }
            public bool WasPressed(string button) { ensureOwnerActive(); return input.WasPressedForOwner(ownerId, DtmButton.Parse(button)); }
            public bool WasPressed(DtmButton button) { ensureOwnerActive(); return input.WasPressedForOwner(ownerId, button); }
            public bool WasReleased(string button) { ensureOwnerActive(); return input.WasReleasedForOwner(ownerId, DtmButton.Parse(button)); }
            public bool WasReleased(DtmButton button) { ensureOwnerActive(); return input.WasReleasedForOwner(ownerId, button); }
            public bool IsKeybindDown(string id) { ensureOwnerActive(); return input.IsKeybindDownForOwner(ownerId, id); }
            public bool WasKeybindPressed(string id) { ensureOwnerActive(); return input.WasKeybindPressedForOwner(ownerId, id); }
            public void Suppress(string button) { ensureOwnerActive(); input.SuppressForOwner(ownerId, button); }
            public IReadOnlyList<string> GetSuppressedButtons() { ensureOwnerActive(); return input.GetSuppressedButtonsForOwner(ownerId); }
        }
    }

    internal readonly struct InputFrameResult
    {
        public InputFrameResult(int sampledButtons, int pressedEvents, int releasedEvents, int keybindPressedEvents, int keybindReleasedEvents)
        {
            SampledButtons = sampledButtons;
            PressedEvents = pressedEvents;
            ReleasedEvents = releasedEvents;
            KeybindPressedEvents = keybindPressedEvents;
            KeybindReleasedEvents = keybindReleasedEvents;
        }

        public int SampledButtons { get; }
        public int PressedEvents { get; }
        public int ReleasedEvents { get; }
        public int KeybindPressedEvents { get; }
        public int KeybindReleasedEvents { get; }
    }

    internal sealed class InputLocalSnapshotDiagnostics
    {
        public InputLocalSnapshotDiagnostics(
            int ownerCount,
            int watchCount,
            int activeButtonCount,
            int cacheRebuildCount,
            int expiredWatchCount,
            long frameGeneration)
        {
            OwnerCount = ownerCount;
            WatchCount = watchCount;
            ActiveButtonCount = activeButtonCount;
            CacheRebuildCount = cacheRebuildCount;
            ExpiredWatchCount = expiredWatchCount;
            FrameGeneration = frameGeneration;
        }

        public int OwnerCount { get; }

        public int WatchCount { get; }

        public int ActiveButtonCount { get; }

        public int CacheRebuildCount { get; }

        public int ExpiredWatchCount { get; }

        public long FrameGeneration { get; }

        public string FormatSummary()
        {
            return "owners=" + OwnerCount.ToString(CultureInfo.InvariantCulture) +
                ", watches=" + WatchCount.ToString(CultureInfo.InvariantCulture) +
                ", activeButtons=" + ActiveButtonCount.ToString(CultureInfo.InvariantCulture) +
                ", cacheRebuilds=" + CacheRebuildCount.ToString(CultureInfo.InvariantCulture) +
                ", expiredWatches=" + ExpiredWatchCount.ToString(CultureInfo.InvariantCulture) +
                ", frameGeneration=" + FrameGeneration.ToString(CultureInfo.InvariantCulture);
        }
    }

    internal sealed class InputKeybindDispatch
    {
        public InputKeybindDispatch(string ownerId, string keybindId, DtmKeybindList keybinds, string triggerButton)
        {
            OwnerId = ownerId;
            KeybindId = keybindId;
            Keybinds = keybinds;
            TriggerButton = triggerButton;
        }

        public string OwnerId { get; }
        public string KeybindId { get; }
        public DtmKeybindList Keybinds { get; }
        public string TriggerButton { get; }
    }

    internal sealed class InputOwnerSnapshot
    {
        public InputOwnerSnapshot(bool enabled, int ownerCount, int buttonCount, int ownerRegistrations, IReadOnlyDictionary<string, int>? buttonsByOwner = null)
        {
            Enabled = enabled;
            OwnerCount = ownerCount;
            ButtonCount = buttonCount;
            OwnerRegistrations = ownerRegistrations;
            ButtonsByOwner = buttonsByOwner ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        public bool Enabled { get; }
        public int OwnerCount { get; }
        public int ButtonCount { get; }
        public int OwnerRegistrations { get; }
        public IReadOnlyDictionary<string, int> ButtonsByOwner { get; }

        public string FormatSummary()
        {
            return Enabled
                ? "status=ok; owners=" + OwnerCount + "; buttons=" + ButtonCount + "; ownerRegistrations=" + OwnerRegistrations + "; byOwner={" + FormatByOwner() + "}"
                : "status=disabled; owner-bound input disabled";
        }

        private string FormatByOwner()
        {
            string value = string.Join("; ", ButtonsByOwner
                .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Take(32)
                .Select(pair => SanitizeMetricKey(pair.Key) + "=" + pair.Value.ToString(CultureInfo.InvariantCulture)));
            return string.IsNullOrWhiteSpace(value) ? "none" : value;
        }

        private static string SanitizeMetricKey(string value)
        {
            string text = string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();
            var builder = new System.Text.StringBuilder(text.Length);
            bool previousUnderscore = false;
            foreach (char ch in text)
            {
                if (char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' || ch == '.')
                {
                    builder.Append(ch);
                    previousUnderscore = false;
                    continue;
                }

                if (!previousUnderscore)
                {
                    builder.Append('_');
                    previousUnderscore = true;
                }
            }

            return builder.ToString().Trim('_');
        }
    }

    public enum DtmOverlayPage
    {
        Status,
        Mods,
        Config,
        Errors,
        Hooks,
        Features,
        Logs
    }

    public sealed class UiRuntimeService : IUiHelper
    {
        private readonly Func<string> exportLogs;
        private readonly Action<string> opened;
        private readonly Action<string> closed;
        private readonly Action<string, string, string>? recordError;
        private readonly Action<string, LogLevel>? log;

        public UiRuntimeService(
            Func<string> exportLogs,
            Action<string> opened,
            Action<string> closed,
            Action<string, string, string>? recordError = null,
            Action<string, LogLevel>? log = null)
        {
            this.exportLogs = exportLogs;
            this.opened = opened;
            this.closed = closed;
            this.recordError = recordError;
            this.log = log;
        }

        internal IUiHelper CreateOwnerBound(string ownerId, Action ensureRuntimeThread, Action ensureOwnerActive)
        {
            if (string.IsNullOrWhiteSpace(ownerId))
                throw new ArgumentException("Owner id is required.", nameof(ownerId));
            return new OwnerBoundUiHelper(
                this,
                ownerId,
                ensureRuntimeThread ?? throw new ArgumentNullException(nameof(ensureRuntimeThread)),
                ensureOwnerActive ?? throw new ArgumentNullException(nameof(ensureOwnerActive)));
        }

        public bool IsOpen { get; private set; }
        public DtmOverlayPage CurrentPage { get; private set; } = DtmOverlayPage.Status;
        public string ActiveMenuId { get; private set; } = string.Empty;
        internal string ActiveMenuOwnerId { get; private set; } = string.Empty;
        internal long OverlaySessionSequence { get; private set; }
        public string LastExportPath { get; private set; } = string.Empty;
        public string? RequestedConfigUniqueId { get; private set; }
        public string InputContext { get; private set; } = "Unknown";
        public bool CanDrawOverlay { get; private set; } = true;
        public bool GameplayHotkeysAllowed { get; private set; } = true;
        public string DrawBoundaryReason { get; private set; } = string.Empty;
        public bool BlocksGameplayHotkeys => IsOpen || !GameplayHotkeysAllowed;
        internal DtmManagerRuntimeModelProvider? ManagerModelProvider { get; set; }
        internal DtmManagerViewModel? CurrentManagerModel { get; private set; }
        internal DtmManagerReportExportResult? LastManagerReportExport { get; private set; }
        internal DtmManagerCopySummaryResult? LastManagerSummaryCopy { get; private set; }
        internal string LastManagerRefreshError { get; private set; } = string.Empty;

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
            ActiveMenuOwnerId = string.Empty;
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
        public void OpenFeatureStatusPage() => Open(DtmOverlayPage.Features);
        public void OpenLogsPage() => Open(DtmOverlayPage.Logs);
        public void SetPage(DtmOverlayPage page) => Open(page);

        public void OpenCustomMenu(string menuId)
        {
            bool wasOpen = IsOpen;
            OverlaySessionSequence++;
            ActiveMenuId = string.IsNullOrWhiteSpace(menuId) ? "DTMAPI.Custom" : menuId.Trim();
            ActiveMenuOwnerId = string.Empty;
            IsOpen = true;
            if (!wasOpen)
                opened(ActiveMenuId);
        }

        internal bool TryOpenOwnerBoundCustomMenu(string menuId, string ownerId)
        {
            string normalizedMenuId =
                string.IsNullOrWhiteSpace(menuId)
                    ? "DTMAPI.Custom"
                    : menuId.Trim();
            string normalizedOwnerId =
                string.IsNullOrWhiteSpace(ownerId)
                    ? string.Empty
                    : ownerId.Trim();
            if (normalizedOwnerId.Length == 0)
                return false;
            if (IsOpen)
            {
                return ActiveMenuId.Equals(
                           normalizedMenuId,
                           StringComparison.OrdinalIgnoreCase) &&
                    ActiveMenuOwnerId.Equals(
                        normalizedOwnerId,
                        StringComparison.OrdinalIgnoreCase);
            }
            bool wasOpen = IsOpen;
            OverlaySessionSequence++;
            ActiveMenuId = normalizedMenuId;
            ActiveMenuOwnerId = normalizedOwnerId;
            IsOpen = true;
            if (!wasOpen)
                opened(ActiveMenuId);
            return true;
        }

        internal void OpenOwnerBoundCustomMenu(
            string menuId,
            string ownerId)
        {
            if (!TryOpenOwnerBoundCustomMenu(menuId, ownerId))
            {
                throw new InvalidOperationException(
                    "Another owner already holds the DTMAPI modal token.");
            }
        }

        internal bool IsOwnerBoundCustomMenuOpen(
            string menuId,
            string ownerId)
        {
            return IsOpen &&
                ActiveMenuId.Equals(
                    (menuId ?? string.Empty).Trim(),
                    StringComparison.OrdinalIgnoreCase) &&
                ActiveMenuOwnerId.Equals(
                    (ownerId ?? string.Empty).Trim(),
                    StringComparison.OrdinalIgnoreCase);
        }

        internal bool CloseOwnerBoundCustomMenu(
            string menuId,
            string ownerId)
        {
            if (!IsOwnerBoundCustomMenuOpen(menuId, ownerId))
                return false;
            Close();
            return true;
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
            try
            {
                if (ManagerModelProvider != null)
                {
                    LastManagerReportExport = ManagerModelProvider.ExportReportAndRefresh();
                    CurrentManagerModel = LastManagerReportExport.RefreshedModel;
                    LastExportPath = LastManagerReportExport.ExportedReportPath;
                }
                else
                {
                    LastExportPath = exportLogs() ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                LastExportPath = string.Empty;
                LastManagerReportExport = DtmManagerReportExportResult.FromFailure(ex, CurrentManagerModel);
                RecordManagerUiFailure("ExportLogs", ex);
            }

            CurrentPage = DtmOverlayPage.Logs;
            Open(DtmOverlayPage.Logs);
            return LastExportPath;
        }

        private void Open(DtmOverlayPage page)
        {
            bool wasOpen = IsOpen;
            OverlaySessionSequence++;
            CurrentPage = page;
            ActiveMenuId = "DTMAPI." + page;
            ActiveMenuOwnerId = string.Empty;
            IsOpen = true;
            RefreshDtmManagerModel();
            if (!wasOpen)
                opened(ActiveMenuId);
        }

        internal void RefreshDtmManagerModel()
        {
            if (ManagerModelProvider == null)
                return;

            try
            {
                CurrentManagerModel = ManagerModelProvider.GetCurrentModel();
                LastManagerRefreshError = string.Empty;
            }
            catch (Exception ex)
            {
                LastManagerRefreshError = ex.GetType().Name + ": " + ex.Message;
                RecordManagerUiFailure("RefreshDtmManagerModel", ex);
            }
        }

        internal DtmManagerCopySummaryResult CopyManagerSummary(Func<string, bool> copyText)
        {
            string summary = ManagerPageRowFormatter.FormatStatusSummary(CurrentManagerModel, LastManagerRefreshError, LastManagerReportExport);
            try
            {
                bool copied = copyText != null && copyText(summary);
                LastManagerSummaryCopy = copied
                    ? DtmManagerCopySummaryResult.FromCopied(summary)
                    : DtmManagerCopySummaryResult.FromUnavailable(summary, "Clipboard unavailable.");
            }
            catch (Exception ex)
            {
                LastManagerSummaryCopy = DtmManagerCopySummaryResult.FromUnavailable(summary, ex.GetType().Name + ": " + ex.Message);
            }

            if (LastManagerSummaryCopy.Copied)
                log?.Invoke("Manager UI Copy Summary copied.", LogLevel.Info);
            else
                log?.Invoke("Manager UI Copy Summary unavailable; summary text retained in runtime log. " + LastManagerSummaryCopy.Text, LogLevel.Warn);

            return LastManagerSummaryCopy;
        }

        private void RecordManagerUiFailure(string operation, Exception exception)
        {
            string message = "Manager UI " + operation + " failed.";
            recordError?.Invoke("DTMAPI.ManagerUI", message, exception.ToString());
            log?.Invoke(message + " " + exception.GetType().Name + ": " + exception.Message, LogLevel.Warn);
        }

        private sealed class OwnerBoundUiHelper :
            IUiHelper
        {
            private readonly UiRuntimeService inner;
            private readonly string ownerId;
            private readonly Action ensureRuntimeThread;
            private readonly Action ensureOwnerActive;

            public OwnerBoundUiHelper(
                UiRuntimeService inner,
                string ownerId,
                Action ensureRuntimeThread,
                Action ensureOwnerActive)
            {
                this.inner = inner;
                this.ownerId = ownerId;
                this.ensureRuntimeThread = ensureRuntimeThread;
                this.ensureOwnerActive = ensureOwnerActive;
            }

            public bool TryOpenModal(string menuId)
            {
                ensureRuntimeThread();
                ensureOwnerActive();
                return inner.TryOpenOwnerBoundCustomMenu(
                    menuId,
                    ownerId);
            }

            public bool IsModalOpen(string menuId)
            {
                ensureRuntimeThread();
                ensureOwnerActive();
                return inner.IsOwnerBoundCustomMenuOpen(
                    menuId,
                    ownerId);
            }

            public bool CloseModal(string menuId)
            {
                ensureRuntimeThread();
                ensureOwnerActive();
                return inner.CloseOwnerBoundCustomMenu(
                    menuId,
                    ownerId);
            }

            public void OpenDtmApiStatusPage()
            {
                ensureRuntimeThread();
                ensureOwnerActive();
                inner.OpenDtmApiStatusPage();
            }

            public void OpenModListPage()
            {
                ensureRuntimeThread();
                ensureOwnerActive();
                inner.OpenModListPage();
            }

            public void OpenConfigPage(string? uniqueId = null)
            {
                ensureRuntimeThread();
                ensureOwnerActive();
                inner.OpenConfigPage(uniqueId);
            }

            public void OpenErrorPage()
            {
                ensureRuntimeThread();
                ensureOwnerActive();
                inner.OpenErrorPage();
            }

            public void OpenHookStatusPage()
            {
                ensureRuntimeThread();
                ensureOwnerActive();
                inner.OpenHookStatusPage();
            }

            public string ExportLogs()
            {
                ensureRuntimeThread();
                ensureOwnerActive();
                return inner.ExportLogs();
            }
        }
    }
}
