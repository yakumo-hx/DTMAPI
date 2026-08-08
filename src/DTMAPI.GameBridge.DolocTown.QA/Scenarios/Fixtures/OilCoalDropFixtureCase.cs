using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using DTMAPI.Abstractions;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class QaScenarioController
    {
        private const int OilNativeDropMaxAttemptsForFixture = 256;
        private bool oilOnlyFixtureMode;

        private string TryExerciseOilItemMetadataForFixture(Type dolocApi)
        {
            if (!TryGetNativeItemProto(dolocApi, "crude_oil", out object? proto, out string nativeProbe))
            {
                if (oilOnlyFixtureMode)
                    throw new InvalidOperationException("crude_oil is not present in the native item table at Oil-only cold start. Runtime reload fallback is disabled for this acceptance mode. probe={" + nativeProbe + "}");

                string reloadDetail;
                if (!TryReloadOfficialModsAndConfig("Smoke.NewContentOilItemMetadata", "missing crude_oil before Oil metadata smoke", out reloadDetail))
                    throw new InvalidOperationException("crude_oil is not present in the native item table before Oil metadata smoke, and official reload failed. probe={" + nativeProbe + "}, reload={" + reloadDetail + "}");

                if (!TryGetNativeItemProto(dolocApi, "crude_oil", out proto, out nativeProbe))
                    throw new InvalidOperationException("crude_oil is not present in the native item table after official reload. probe={" + nativeProbe + "}, reload={" + reloadDetail + "}");
            }

            if (proto == null)
                throw new InvalidOperationException("crude_oil native item query returned no proto. probe={" + nativeProbe + "}");

            InventoryDebugPage page = inventoryDebugApi.GetItems(new InventoryDebugQuery
            {
                SearchText = "crude_oil",
                IncludeUnavailable = true,
                PageSize = 50
            });
            InventoryDebugItem? item = page.Items.FirstOrDefault(i => i.Id.Equals("crude_oil", StringComparison.OrdinalIgnoreCase));
            if (item == null)
                throw new InvalidOperationException("IInventoryDebugApi did not return crude_oil. status=" + page.Status + ", total=" + page.TotalItems + ".");

            InventoryDebugPage sourcePage = inventoryDebugApi.GetItems(new InventoryDebugQuery
            {
                SourceId = item.SourceId,
                IncludeUnavailable = true,
                PageSize = 50
            });
            IContentItemInfo? sourceInfo = runtime.GetIndexedContentItem("crude_oil");
            bool sourceFilterIncludesOil = sourcePage.Items.Any(i => i.Id.Equals("crude_oil", StringComparison.OrdinalIgnoreCase));
            InventoryDebugSourceGroup? sourceGroup = page.Sources.FirstOrDefault(g => g.Id.Equals(item.SourceId, StringComparison.OrdinalIgnoreCase));
            string effectiveCategory = FirstNonEmpty(item.SubCategory, item.Category);
            bool categoryListed = !string.IsNullOrWhiteSpace(effectiveCategory) &&
                page.Categories.Any(c => c.Equals(effectiveCategory, StringComparison.OrdinalIgnoreCase));
            string[] tags = item.Tags?.ToArray() ?? Array.Empty<string>();
            string[] nativeSources = ReadStringValues(ReadAnyMember(proto, "Source", "source")).ToArray();

            string title = FirstNonEmpty(ReadAnyStringMember(proto, string.Empty, "Title", "title"), item.DisplayName, item.ChineseName, item.Id);
            string description = ReadAnyStringMember(proto, string.Empty, "DescriptionBasic", "description_basic");
            bool salable = ReadAnyBoolMember(proto, false, "Salable", "salable");
            bool viewable = ReadAnyBoolMember(proto, false, "Viewable", "viewable");
            int sellingPrice = ReadAnyIntMember(proto, -1, "SellingPrice", "selling_price");
            int buyingPrice = ReadAnyIntMember(proto, -1, "BuyingPrice", "buying_price");
            int electricEnergy = ReadAnyIntMember(proto, -1, "ElectricEnergy", "electric_energy");
            int overlay = ReadAnyIntMember(proto, -1, "Overlay", "overlay");
            string nativeIcon = ReadAnyMember(proto, "UiSpriteAsset", "ui_sprite_asset")?.ToString() ?? string.Empty;
            string indexedIcon = sourceInfo?.IconAssetKey ?? string.Empty;
            string nativeSubType = ReadAnyStringMember(proto, string.Empty, "SubType", "sub_type");
            string functionType = ReadAnyMember(proto, "Function", "function")?.GetType().Name ?? "none";
            int highestBaseFuel = FindHighestNativeFuelEnergyExcept("crude_oil", out string highestBaseFuelItem);

            var failures = new List<string>();
            if (!item.RuntimeLoaded)
                failures.Add("not-runtime-loaded");
            if (!item.CanSpawn || !item.CanGive)
                failures.Add("not-giveable:" + item.CannotGiveReason);
            if (!item.IsModItem)
                failures.Add("not-marked-mod-item");
            if (!item.SourceKind.Equals("DTMAPI", StringComparison.OrdinalIgnoreCase))
                failures.Add("source-kind=" + item.SourceKind);
            if (!item.SourceId.Equals("Local.DTMAPI_Oil", StringComparison.OrdinalIgnoreCase))
                failures.Add("source-id=" + item.SourceId);
            if (sourceGroup == null || sourceGroup.Count <= 0)
                failures.Add("missing-source-group");
            if (!sourceFilterIncludesOil)
                failures.Add("source-filter-misses-oil");
            if (string.IsNullOrWhiteSpace(effectiveCategory))
                failures.Add("missing-category");
            if (!categoryListed)
                failures.Add("category-not-listed:" + effectiveCategory);
            if (!ContainsAny(tags, "material_ore", "mining", "processing") &&
                !ContainsAny(nativeSources, "material_ore", "mining", "processing") &&
                !ContainsIgnoreCase(item.SearchText, "material_ore"))
                failures.Add("missing-mining-category-tags");
            if (!item.HasIcon || string.IsNullOrWhiteSpace(nativeIcon) || !ContainsIgnoreCase(indexedIcon, "icon_item_coal"))
                failures.Add("missing-icon:hasIcon=" + item.HasIcon + ", item=" + item.IconAssetKey + ", native=" + nativeIcon + ", indexed=" + indexedIcon);
            if (!ContainsIgnoreCase(title, "原油") && !ContainsIgnoreCase(title, "石油") && !ContainsIgnoreCase(title, "Oil") && !ContainsIgnoreCase(item.DisplayName, "原油") && !ContainsIgnoreCase(item.DisplayName, "Oil"))
                failures.Add("missing-localized-title:" + title + "/" + item.DisplayName);
            if (!ContainsIgnoreCase(description, "燃料") && !ContainsIgnoreCase(description, "fuel"))
                failures.Add("missing-fuel-description");
            if (!salable || sellingPrice <= 0)
                failures.Add("not-salable:sale=" + salable + ", price=" + sellingPrice);
            if (buyingPrice <= 0)
                failures.Add("missing-buy-price:" + buyingPrice);
            if (!viewable)
                failures.Add("not-viewable");
            if (overlay <= 0)
                failures.Add("not-stackable-overlay:" + overlay);
            if (electricEnergy <= highestBaseFuel)
                failures.Add("fuel-not-above-base-highest:" + electricEnergy + "<=" + highestBaseFuel + "(" + highestBaseFuelItem + ")");

            string summary = "id=" + item.Id +
                ", display=" + FirstNonEmpty(item.DisplayName, title, item.Id) +
                ", sourceKind=" + item.SourceKind +
                ", sourceId=" + item.SourceId +
                ", sourceTitle=" + item.SourceModTitle +
                ", sourceGroup=" + (sourceGroup == null ? "missing" : sourceGroup.DisplayName + "/" + sourceGroup.Count) +
                ", sourceFilterIncludesOil=" + sourceFilterIncludesOil +
                ", category=" + effectiveCategory +
                ", categoryListed=" + categoryListed +
                ", tags=" + (tags.Length == 0 ? "none" : string.Join("|", tags)) +
                ", nativeSources=" + (nativeSources.Length == 0 ? "none" : string.Join("|", nativeSources)) +
                ", salable=" + salable +
                ", sellingPrice=" + sellingPrice +
                ", buyingPrice=" + buyingPrice +
                ", fuelEnergy=" + electricEnergy +
                ", baseHighestFuel=" + highestBaseFuelItem + ":" + highestBaseFuel +
                ", icon=" + FirstNonEmpty(item.IconAssetKey, nativeIcon) +
                ", indexedIcon=" + indexedIcon +
                ", title=" + title +
                ", subType=" + nativeSubType +
                ", function=" + functionType +
                ", nativeProbe={" + nativeProbe + "}";
            if (failures.Count > 0)
                throw new InvalidOperationException("Oil item metadata smoke failed: " + string.Join(", ", failures) + ". " + summary);

            runtime.RuntimeMonitor.Log("Smoke exercise NewContentOilItemMetadata OK " + summary);
            runtime.SetHookStatus("Smoke.NewContentOilItemMetadata", "verified", "IInventoryDebugApi.GetItems + DolocAPI.QueryItemProto", summary);
            return summary;
        }

        private string TryExerciseOilCoalDropForFixture(Type dolocApi, object room)
        {
            if (!TryQueryNativeItemProto(dolocApi, "crude_oil", out string oilProbe))
            {
                if (oilOnlyFixtureMode)
                    throw new InvalidOperationException("crude_oil is not present in the native item table at Oil-only native-drop cold start. Runtime reload fallback is disabled for this acceptance mode. probe={" + oilProbe + "}");

                string reloadDetail;
                if (!TryReloadOfficialModsAndConfig("Smoke.NewContentOilCoalDrop", "missing crude_oil before official native-drop smoke", out reloadDetail))
                    throw new InvalidOperationException("crude_oil is not present in the native item table before official native-drop smoke, and official reload failed. probe={" + oilProbe + "}, reload={" + reloadDetail + "}");

                if (!TryQueryNativeItemProto(dolocApi, "crude_oil", out oilProbe))
                    throw new InvalidOperationException("crude_oil is not present in the native item table after official reload. probe={" + oilProbe + "}, reload={" + reloadDetail + "}");
            }

            string mergedLutSummary = ValidateOilCoalDropLutForFixture(expectOil: true);

            Type? toolColliderType = patcher?.ResolveType("DolocTown.ToolCollider, Assembly-CSharp");
            Type? resourceRendererType = patcher?.ResolveType("DolocTown.DungeonResourceRenderer, Assembly-CSharp");
            if (toolColliderType == null || resourceRendererType == null)
                throw new MissingMemberException("ToolCollider or DungeonResourceRenderer was not visible.");

            MethodInfo? handleTools = FindMethod(toolColliderType, "HandleTools", 1);
            MethodInfo? resetTool = FindMethod(toolColliderType, "ResetTool", 1);
            MethodInfo? resetChopCounter = FindMethod(toolColliderType, "ResetChopCounter", 1);
            if (handleTools == null || resetTool == null)
                throw new MissingMethodException("ToolCollider.HandleTools/ResetTool path was not found for official native-drop smoke.");

            object? toolCollider = FindToolColliderForFixture(dolocApi, toolColliderType);
            if (toolCollider == null)
                throw new InvalidOperationException("No ToolCollider instance was available for official native-drop smoke.");

            object? tool = GenerateItemForFixture(dolocApi, "steel_pickaxe") ?? GenerateItemForFixture(dolocApi, "iron_pickaxe") ?? GenerateItemForFixture(dolocApi, "old_pickaxe");
            if (tool == null)
                throw new InvalidOperationException("Could not generate a pickaxe for official native-drop smoke.");

            string toolName = ReadStringMember(tool, "name", tool.GetType().Name);
            int toolDamage = ReadIntMember(tool, "ChopNumber", 0);
            if (toolDamage <= 0)
                throw new InvalidOperationException("Generated pickaxe has no native ChopNumber. tool=" + toolName + ".");

            List<string> attempts = new List<string>();
            string lastAttempt = "none";

            for (int attempt = 1; attempt <= OilNativeDropMaxAttemptsForFixture; attempt++)
            {
                object? transientResource = null;
                List<object> baselineDrops = SnapshotRoomWorldDropsForOilSmoke(room);
                bool observedOil = false;
                string observedSummary = string.Empty;
                string dropCleanupSummary = "not-run";
                string resourceCleanupSummary = "not-run";
                bool dropCleanupOk = false;
                bool resourceCleanupOk = false;
                Exception? attemptFailure = null;

                try
                {
                    if (!TryCreateTransientCoalResourceForOilSmoke(room, out transientResource, out string createSummary) || transientResource == null)
                        throw new InvalidOperationException("Could not create a transient coal_mine for official native-drop smoke. " + createSummary);

                    TryRenderAllResourcesForFixture(room);
                    object? renderer = FindUnityObjects(resourceRendererType, includeInactive: true)
                        .FirstOrDefault(candidate => ReferenceEquals(ReadMember(candidate, "DungeonResource"), transientResource));
                    if (renderer == null)
                    {
                        object? attachedRenderer = ReadMember(transientResource, "Renderer");
                        if (attachedRenderer != null && resourceRendererType.IsInstanceOfType(attachedRenderer))
                            renderer = attachedRenderer;
                    }
                    if (renderer == null)
                        throw new InvalidOperationException("Transient coal_mine did not receive a DungeonResourceRenderer. create={" + createSummary + "}");

                    object? collider = ReadMember(renderer, "PolygonCollider");
                    if (collider == null)
                        throw new InvalidOperationException("Transient coal_mine renderer has no PolygonCollider. create={" + createSummary + "}");

                    string resourceName = ReadStringMember(transientResource, "ResourceName", transientResource.GetType().Name);
                    string resourceClass = ReadResourceClass(transientResource);
                    if (!IsCoalResourceNameForFixture(resourceName))
                        throw new InvalidOperationException("Transient resource was not coal_mine. resource=" + resourceName + ", create={" + createSummary + "}");

                    int healthBefore = ReadIntMember(transientResource, "currentHealth", 0);
                    if (healthBefore <= 0)
                        throw new InvalidOperationException("Transient coal_mine has no positive native health. resource=" + resourceName + ", health=" + healthBefore + ".");

                    int seededHealth = Math.Max(1, Math.Min(healthBefore, toolDamage));
                    if (!WriteIntMember(transientResource, "currentHealth", seededHealth) || ReadIntMember(transientResource, "currentHealth", 0) != seededHealth)
                        throw new InvalidOperationException("Could not seed transient coal_mine health for one native pickaxe hit. health=" + healthBefore + ", target=" + seededHealth + ".");

                    resetTool.Invoke(toolCollider, new object[] { tool });
                    resetChopCounter?.Invoke(toolCollider, new object[] { 999 });
                    handleTools.Invoke(toolCollider, new object[] { collider });

                    int healthAfter = ReadIntMember(transientResource, "currentHealth", 0);
                    bool removedAfter = IsRemoved(transientResource);
                    if (!removedAfter)
                        throw new InvalidOperationException("Native ToolCollider hit did not remove transient coal_mine. health=" + healthBefore + "->" + healthAfter + ", seeded=" + seededHealth + ".");

                    List<object> newWorldDrops = FindNewRoomWorldDropsForOilSmoke(room, baselineDrops);
                    string worldDropNames = DescribeWorldDropsForOilSmoke(newWorldDrops);
                    int oilWorldDrops = newWorldDrops.Count(drop => ReadAnyStringMember(drop, string.Empty, "ItemName", "itemName").Equals("crude_oil", StringComparison.OrdinalIgnoreCase));
                    observedOil = oilWorldDrops > 0;
                    observedSummary = "attempt=" + attempt + "/" + OilNativeDropMaxAttemptsForFixture +
                        ", resource=" + resourceName +
                        ", class=" + resourceClass +
                        ", tool=" + toolName +
                        ", toolDamage=" + toolDamage +
                        ", health=" + healthBefore + "->" + seededHealth + "->" + healthAfter +
                        ", removed=" + removedAfter +
                        ", worldOwner=CurrentRoom.DM_dropitem.AllDatas" +
                        ", newWorldDrops=" + newWorldDrops.Count +
                        ", oilWorldDrops=" + oilWorldDrops +
                        ", dropItems=" + worldDropNames +
                        ", create={" + createSummary + "}";
                    lastAttempt = observedSummary;
                    if (attempts.Count < 12)
                        attempts.Add(observedSummary);
                }
                catch (Exception ex)
                {
                    attemptFailure = ex;
                }
                finally
                {
                    dropCleanupOk = TryRemoveNewRoomWorldDropsForOilSmoke(room, baselineDrops, out dropCleanupSummary);
                    resourceCleanupOk = TryRemoveTransientCoalResourceForOilSmoke(room, transientResource, out resourceCleanupSummary);
                }

                if (!dropCleanupOk || !resourceCleanupOk)
                    throw new InvalidOperationException("Oil native-drop smoke cleanup failed. drops={" + dropCleanupSummary + "}, resource={" + resourceCleanupSummary + "}, observation={" + observedSummary + "}, primary={" + (attemptFailure == null ? "none" : attemptFailure.GetType().Name + ": " + attemptFailure.Message) + "}");
                if (attemptFailure != null)
                    ExceptionDispatchInfo.Capture(attemptFailure).Throw();

                if (!observedOil)
                    continue;

                string summary = "mergedLut={" + mergedLutSummary + "}, nativeHit={" + observedSummary + "}, cleanup={drops:" + dropCleanupSummary + ", resource:" + resourceCleanupSummary + "}, naturalRetryLimit=" + OilNativeDropMaxAttemptsForFixture + ", oilProbe={" + oilProbe + "}";
                runtime.RuntimeMonitor.Log("Smoke exercise NewContentOilCoalDrop OK " + summary);
                runtime.SetHookStatus("Smoke.NewContentOilCoalDrop", "verified", "DolocConfig.Tables.TbItemSpawn merged LUT -> ToolCollider.HandleTools -> CurrentRoom.DM_dropitem world drops", summary);
                return summary;
            }

            throw new InvalidOperationException("No transient coal_mine produced a native crude_oil world drop within the natural retry limit. maxAttempts=" + OilNativeDropMaxAttemptsForFixture + ", mergedLut={" + mergedLutSummary + "}, samples=" + (attempts.Count == 0 ? "none" : string.Join(" ; ", attempts)) + ", last={" + lastAttempt + "}");
        }

        private string VerifyOilAbsentForFixture(Type dolocApi)
        {
            if (TryQueryNativeItemProto(dolocApi, "crude_oil", out string nativeProbe))
                throw new InvalidOperationException("crude_oil unexpectedly exists in the native item table during the Oil-absent cold-start smoke. probe={" + nativeProbe + "}");

            IContentItemInfo? indexedOil = runtime.GetIndexedContentItem("crude_oil");
            if (indexedOil != null)
                throw new InvalidOperationException("crude_oil unexpectedly exists in the active DTMAPI content index during the Oil-absent smoke. source=" + indexedOil.SourceId + ".");

            string baseLutSummary = ValidateOilCoalDropLutForFixture(expectOil: false);
            string[] retiredHooks = runtime.Diagnostics.GetHookStatuses()
                .Where(status => status.HookId.Equals("Resources.OilCoalDrop", StringComparison.OrdinalIgnoreCase) ||
                    status.HookId.Equals("OilMod.MiningDrop", StringComparison.OrdinalIgnoreCase))
                .Select(status => status.HookId + "=" + status.Status)
                .ToArray();
            string[] retiredFeatures = runtime.Diagnostics.GetFeatureStatuses()
                .Where(status => status.FeatureId.Equals("OilCoalDrop", StringComparison.OrdinalIgnoreCase) ||
                    status.FeatureId.Equals("Feature.OilCoalDrop", StringComparison.OrdinalIgnoreCase))
                .Select(status => status.FeatureId + "=" + status.Status)
                .ToArray();
            if (retiredHooks.Length > 0 || retiredFeatures.Length > 0)
                throw new InvalidOperationException("Retired Oil GameBridge diagnostics unexpectedly exist. hooks=" + (retiredHooks.Length == 0 ? "none" : string.Join("|", retiredHooks)) + ", features=" + (retiredFeatures.Length == 0 ? "none" : string.Join("|", retiredFeatures)) + ".");

            return "nativeItem=absent, activeContentIndex=absent, " + baseLutSummary + ", retiredHooks=none, retiredFeatures=none, nativeProbe={" + nativeProbe + "}";
        }

        private string ValidateOilCoalDropLutForFixture(bool expectOil)
        {
            object? spawnInfo = GetDolocConfigDataMapValueForFixture("TbItemSpawn", "coal_mine_drop");
            if (spawnInfo == null)
                throw new InvalidOperationException("DolocConfig.Tables.TbItemSpawn does not contain coal_mine_drop after official content merge.");

            List<object> rows = EnumerateObjects(ReadAnyMember(spawnInfo, "SpawnDatas", "spawn_datas")).ToList();
            string[] expectedNames = expectOil ? new[] { "coal", "amber_ore", "crude_oil" } : new[] { "coal", "amber_ore" };
            double[] expectedWeights = expectOil ? new[] { 990d, 10d, 25d } : new[] { 990d, 10d };
            int[] expectedMinimums = expectOil ? new[] { 0, 0, 0 } : new[] { 0, 0 };
            int[] expectedMaximums = expectOil ? new[] { 0, 1, 0 } : new[] { 0, 1 };
            bool[] expectedUnlimited = expectOil ? new[] { true, false, true } : new[] { true, false };
            var failures = new List<string>();
            var rowSummaries = new List<string>();

            if (rows.Count != expectedNames.Length)
                failures.Add("entry-count=" + rows.Count + " expected=" + expectedNames.Length);

            for (int index = 0; index < rows.Count; index++)
            {
                object row = rows[index];
                string itemName = ReadAnyStringMember(row, string.Empty, "ItemName", "item_name");
                object? rawWeight = ReadAnyMember(row, "SpawnWeight", "spawn_weight");
                double weight = rawWeight == null ? double.NaN : Convert.ToDouble(rawWeight, CultureInfo.InvariantCulture);
                int minimum = ReadAnyIntMember(row, int.MinValue, "MinCount", "min_count");
                int maximum = ReadAnyIntMember(row, int.MinValue, "MaxCount", "max_count");
                bool unlimited = ReadAnyBoolMember(row, false, "Unlimited", "unlimited");
                rowSummaries.Add(index + ":" + itemName + "/weight=" + weight.ToString("0.###", CultureInfo.InvariantCulture) + "/min=" + minimum + "/max=" + maximum + "/unlimited=" + unlimited);

                if (index >= expectedNames.Length)
                {
                    failures.Add("unexpected-entry@" + index + "=" + itemName);
                    continue;
                }
                if (!itemName.Equals(expectedNames[index], StringComparison.OrdinalIgnoreCase))
                    failures.Add("order@" + index + "=" + itemName + " expected=" + expectedNames[index]);
                if (double.IsNaN(weight) || Math.Abs(weight - expectedWeights[index]) > 0.001d)
                    failures.Add("weight@" + index + "=" + weight.ToString("0.###", CultureInfo.InvariantCulture) + " expected=" + expectedWeights[index].ToString("0.###", CultureInfo.InvariantCulture));
                if (minimum != expectedMinimums[index])
                    failures.Add("min@" + index + "=" + minimum + " expected=" + expectedMinimums[index]);
                if (maximum != expectedMaximums[index])
                    failures.Add("max@" + index + "=" + maximum + " expected=" + expectedMaximums[index]);
                if (unlimited != expectedUnlimited[index])
                    failures.Add("unlimited@" + index + "=" + unlimited + " expected=" + expectedUnlimited[index]);
            }

            string expectedSummary = expectOil
                ? "coal:990:unlimited->amber_ore:10:max1->crude_oil:25:unlimited"
                : "coal:990:unlimited->amber_ore:10:max1";
            string summary = "owner=DolocConfig.Tables.TbItemSpawn[coal_mine_drop].SpawnDatas, order=" + (rowSummaries.Count == 0 ? "none" : string.Join("|", rowSummaries)) + ", expected=" + expectedSummary;
            if (failures.Count > 0)
                throw new InvalidOperationException("coal_mine_drop LUT does not match the expected " + (expectOil ? "append-only Oil" : "Oil-absent native base") + " boundary: " + string.Join(", ", failures) + ". " + summary);

            return summary;
        }

        private bool TryCreateTransientCoalResourceForOilSmoke(object room, out object? resource, out string summary)
        {
            resource = null;
            Type? hostType = patcher?.ResolveType("DolocTown.IDungeonResourceHost, Assembly-CSharp");
            MethodInfo? createResource = FindMethod(hostType, "CreateDungeonResource", 1);
            if (hostType == null || createResource == null || !hostType.IsInstanceOfType(room))
            {
                summary = "Current room is not an IDungeonResourceHost or CreateDungeonResource is unavailable. room=" + DescribeRoomForFixture(room);
                return false;
            }

            int attempted = 0;
            foreach (object proto in FindOneActionResourceProtosForFixture("Ore", "coal"))
            {
                attempted++;
                object? created = createResource.Invoke(room, new object[] { proto });
                if (created == null)
                    continue;
                string resourceName = ReadStringMember(created, "ResourceName", created.GetType().Name);
                if (!IsCoalResourceNameForFixture(resourceName))
                {
                    TryRemoveTransientDungeonResourceForFixture(room, created);
                    continue;
                }

                resource = created;
                summary = "created transient coal resource through IDungeonResourceHost.CreateDungeonResource, resource=" + resourceName + ", class=" + ReadResourceClass(created) + ", attemptedProtos=" + attempted + ", room={" + DescribeRoomForFixture(room) + "}";
                return true;
            }

            summary = "No coal_mine proto could be created through IDungeonResourceHost.CreateDungeonResource. attemptedProtos=" + attempted + ", room={" + DescribeRoomForFixture(room) + "}";
            return false;
        }

        private static List<object> SnapshotRoomWorldDropsForOilSmoke(object room)
        {
            object? manager = ReadMember(room, "DM_dropitem");
            if (manager == null)
                throw new InvalidOperationException("Current room does not expose DM_dropitem for native world-drop observation.");
            object? allDatas = ReadMember(manager, "AllDatas");
            if (allDatas == null)
                throw new InvalidOperationException("Current room DM_dropitem does not expose AllDatas for native world-drop observation.");
            return EnumerateObjects(allDatas).Where(item => !IsRemoved(item)).ToList();
        }

        private static List<object> FindNewRoomWorldDropsForOilSmoke(object room, IReadOnlyList<object> baseline)
        {
            return SnapshotRoomWorldDropsForOilSmoke(room)
                .Where(item => !baseline.Any(existing => ReferenceEquals(existing, item)))
                .ToList();
        }

        private static string DescribeWorldDropsForOilSmoke(IEnumerable<object> drops)
        {
            string[] names = drops
                .Select(drop => ReadAnyStringMember(drop, drop.GetType().Name, "ItemName", "itemName"))
                .ToArray();
            return names.Length == 0 ? "none" : string.Join("|", names);
        }

        private bool TryRemoveNewRoomWorldDropsForOilSmoke(object room, IReadOnlyList<object> baseline, out string summary)
        {
            try
            {
                Type? hostType = patcher?.ResolveType("DolocTown.IDropItemHost, Assembly-CSharp");
                MethodInfo? removeDropItem = FindMethod(hostType, "RemoveDropItem", 1);
                if (hostType == null || removeDropItem == null || !hostType.IsInstanceOfType(room))
                {
                    summary = "Current room is not an IDropItemHost or RemoveDropItem(DropItemBase) is unavailable.";
                    return false;
                }

                List<object> created = FindNewRoomWorldDropsForOilSmoke(room, baseline);
                int removeAccepted = 0;
                foreach (object drop in created)
                {
                    object? result = removeDropItem.Invoke(room, new[] { drop });
                    if (result is bool accepted && accepted)
                        removeAccepted++;
                }

                List<object> remaining = FindNewRoomWorldDropsForOilSmoke(room, baseline);
                summary = "owner=IDropItemHost.RemoveDropItem, created=" + created.Count + ", removeAccepted=" + removeAccepted + ", remainingInDM=" + remaining.Count + ", items=" + DescribeWorldDropsForOilSmoke(created);
                return remaining.Count == 0;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke transient Oil world-drop cleanup failed.", ex.ToString());
                summary = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }

        private bool TryRemoveTransientCoalResourceForOilSmoke(object room, object? resource, out string summary)
        {
            if (resource == null)
            {
                summary = "not-created";
                return true;
            }

            object? manager = ReadMember(room, "DM_dungeonResource");
            object? allResources = manager == null ? null : ReadMember(manager, "AllDungeonResources");
            if (manager == null || allResources == null)
            {
                summary = "Current room does not expose DM_dungeonResource.AllDungeonResources for transient-resource cleanup verification.";
                return false;
            }

            bool registeredBefore = EnumerateObjects(allResources).Any(candidate => ReferenceEquals(candidate, resource));
            bool removedBefore = IsRemoved(resource);
            if (registeredBefore || !removedBefore)
                TryRemoveTransientDungeonResourceForFixture(room, resource);

            object? resourcesAfter = ReadMember(manager, "AllDungeonResources");
            if (resourcesAfter == null)
            {
                summary = "DM_dungeonResource.AllDungeonResources disappeared after transient-resource cleanup.";
                return false;
            }

            bool stillRegistered = EnumerateObjects(resourcesAfter).Any(candidate => ReferenceEquals(candidate, resource));
            bool removedAfter = IsRemoved(resource);
            bool removed = !stillRegistered;
            summary = "owner=IDungeonResourceHost.RemoveDungeonResource, removed=" + removed + ", registeredBefore=" + registeredBefore + ", isRemovedBefore=" + removedBefore + ", isRemovedAfter=" + removedAfter + ", stillRegistered=" + stillRegistered + ", resource=" + ReadStringMember(resource, "ResourceName", resource.GetType().Name);
            return removed;
        }
    }
}
