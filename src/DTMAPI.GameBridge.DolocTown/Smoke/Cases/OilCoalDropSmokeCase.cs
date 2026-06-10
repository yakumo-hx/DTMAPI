using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;

namespace DTMAPI.GameBridge.DolocTown
{
    public sealed partial class DolocTownGameBridge
    {
        private string TryExerciseOilItemMetadataForSmoke(Type dolocApi)
        {
            if (experimentalApi == null)
                throw new InvalidOperationException("Experimental GameBridge API was not registered.");

            if (!TryGetNativeItemProto(dolocApi, "crude_oil", out object? proto, out string nativeProbe))
            {
                string reloadDetail;
                if (!TryReloadOfficialModsAndConfig("Smoke.NewContentOilItemMetadata", "missing crude_oil before Oil metadata smoke", out reloadDetail))
                    throw new InvalidOperationException("crude_oil is not present in the native item table before Oil metadata smoke, and official reload failed. probe={" + nativeProbe + "}, reload={" + reloadDetail + "}");

                if (!TryGetNativeItemProto(dolocApi, "crude_oil", out proto, out nativeProbe))
                    throw new InvalidOperationException("crude_oil is not present in the native item table after official reload. probe={" + nativeProbe + "}, reload={" + reloadDetail + "}");
            }

            if (proto == null)
                throw new InvalidOperationException("crude_oil native item query returned no proto. probe={" + nativeProbe + "}");

            InventoryDebugPage page = experimentalApi.GetItems(new InventoryDebugQuery
            {
                SearchText = "crude_oil",
                IncludeUnavailable = true,
                PageSize = 50
            });
            InventoryDebugItem? item = page.Items.FirstOrDefault(i => i.Id.Equals("crude_oil", StringComparison.OrdinalIgnoreCase));
            if (item == null)
                throw new InvalidOperationException("IInventoryDebugApi did not return crude_oil. status=" + page.Status + ", total=" + page.TotalItems + ".");

            InventoryDebugPage sourcePage = experimentalApi.GetItems(new InventoryDebugQuery
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

        private string TryExerciseOilCoalDropForSmoke(Type dolocApi, object room)
        {
            OilCoalDropService? oilCoalDropService = OilCoalDropService;
            if (oilCoalDropService == null)
                throw new InvalidOperationException("OilCoalDrop feature service is not available.");

            if (!TryQueryNativeItemProto(dolocApi, "crude_oil", out string oilProbe))
            {
                string reloadDetail;
                if (!TryReloadOfficialModsAndConfig("Smoke.NewContentOilCoalDrop", "missing crude_oil before OilMod smoke", out reloadDetail))
                    throw new InvalidOperationException("crude_oil is not present in the native item table before OilMod smoke, and official reload failed. probe={" + oilProbe + "}, reload={" + reloadDetail + "}");

                if (!TryQueryNativeItemProto(dolocApi, "crude_oil", out oilProbe))
                    throw new InvalidOperationException("crude_oil is not present in the native item table after official reload. probe={" + oilProbe + "}, reload={" + reloadDetail + "}");
            }

            Type? toolColliderType = patcher?.ResolveType("DolocTown.ToolCollider, Assembly-CSharp");
            Type? resourceRendererType = patcher?.ResolveType("DolocTown.DungeonResourceRenderer, Assembly-CSharp");
            if (toolColliderType == null || resourceRendererType == null)
                throw new MissingMemberException("ToolCollider or DungeonResourceRenderer was not visible.");

            if (!TryEnsureCoalResourceForOilSmoke(room, resourceRendererType, out string setupSummary))
                throw new InvalidOperationException("Could not prepare a coal resource for OilMod smoke. " + setupSummary);

            MethodInfo? handleTools = FindMethod(toolColliderType, "HandleTools", 1);
            MethodInfo? resetTool = FindMethod(toolColliderType, "ResetTool", 1);
            MethodInfo? resetChopCounter = FindMethod(toolColliderType, "ResetChopCounter", 1);
            if (handleTools == null || resetTool == null)
                throw new MissingMethodException("ToolCollider.HandleTools/ResetTool path was not found for OilMod smoke.");

            object? toolCollider = FindToolColliderForSmoke(dolocApi, toolColliderType);
            if (toolCollider == null)
                throw new InvalidOperationException("No ToolCollider instance was available for OilMod smoke.");

            object? tool = GenerateItemForSmoke(dolocApi, "steel_pickaxe") ?? GenerateItemForSmoke(dolocApi, "iron_pickaxe") ?? GenerateItemForSmoke(dolocApi, "old_pickaxe");
            if (tool == null)
                throw new InvalidOperationException("Could not generate a pickaxe for OilMod smoke.");

            string toolName = ReadStringMember(tool, "name", tool.GetType().Name);
            int toolDamage = ReadIntMember(tool, "ChopNumber", 0);
            int beforeDrops = oilCoalDropService.OilMiningDropCount;
            int rendererCount = 0;
            int coalCount = 0;
            int invokedCount = 0;
            List<string> samples = new List<string>();
            List<string> attempts = new List<string>();

            try
            {
                oilCoalDropService.ForceOilDropForSmoke = true;
                foreach (object renderer in FindUnityObjects(resourceRendererType))
                {
                    rendererCount++;
                    object? resource = ReadMember(renderer, "DungeonResource");
                    if (resource == null || IsRemoved(resource))
                        continue;

                    string resourceName = ReadStringMember(resource, "ResourceName", resource.GetType().Name);
                    string resourceClass = ReadResourceClass(resource);
                    int healthBefore = ReadIntMember(resource, "currentHealth", 0);
                    if (samples.Count < 8)
                        samples.Add(resourceName + "/class=" + resourceClass + "/health=" + healthBefore);
                    if (!IsCoalResourceNameForSmoke(resourceName) || healthBefore <= 0)
                        continue;

                    coalCount++;
                    object? collider = ReadMember(renderer, "PolygonCollider");
                    if (collider == null)
                        continue;

                    int seededHealth = Math.Max(1, toolDamage > 0 ? Math.Min(healthBefore, toolDamage) : 1);
                    WriteIntMember(resource, "currentHealth", seededHealth);
                    resetTool.Invoke(toolCollider, new object[] { tool });
                    resetChopCounter?.Invoke(toolCollider, new object[] { 999 });
                    handleTools.Invoke(toolCollider, new object[] { collider });
                    invokedCount++;

                    int healthAfter = ReadIntMember(resource, "currentHealth", 0);
                    bool removedAfter = IsRemoved(resource);
                    if (attempts.Count < 8)
                        attempts.Add(resourceName + "/tool=" + toolName + "/toolDamage=" + toolDamage + "/health=" + healthBefore + "->" + healthAfter + "/seeded=" + seededHealth + "/removed=" + removedAfter + "/drops=" + beforeDrops + "->" + oilCoalDropService.OilMiningDropCount + "/bridge={" + oilCoalDropService.LastOilMiningDropSummary + "}");
                    if (oilCoalDropService.OilMiningDropCount > beforeDrops)
                    {
                        string summary = "setup={" + setupSummary + "}, resource=" + resourceName + ", class=" + resourceClass + ", tool=" + toolName + ", toolDamage=" + toolDamage + ", healthBefore=" + healthBefore + ", seededHealth=" + seededHealth + ", healthAfter=" + healthAfter + ", removed=" + removedAfter + ", beforeDrops=" + beforeDrops + ", afterDrops=" + oilCoalDropService.OilMiningDropCount + ", bridge={" + oilCoalDropService.LastOilMiningDropSummary + "}";
                        runtime.RuntimeMonitor.Log("Smoke exercise NewContentOilCoalDrop OK " + summary);
                        runtime.SetHookStatus("Smoke.NewContentOilCoalDrop", "verified", "ToolCollider.HandleTools private path + OilMod.MiningDrop", summary);
                        return summary;
                    }
                }
            }
            finally
            {
                oilCoalDropService.ForceOilDropForSmoke = false;
            }

            throw new InvalidOperationException("No coal resource produced crude_oil during OilMod smoke. renderers=" + rendererCount + ", coalResources=" + coalCount + ", invoked=" + invokedCount + ", beforeDrops=" + beforeDrops + ", afterDrops=" + oilCoalDropService.OilMiningDropCount + ", setup={" + setupSummary + "}, attempts=" + (attempts.Count == 0 ? "none" : string.Join(" ; ", attempts)) + ", samples=" + string.Join(" ; ", samples));
        }

        private bool TryEnsureCoalResourceForOilSmoke(object room, Type resourceRendererType, out string summary)
        {
            TryRenderAllResourcesForSmoke(room);
            int existing = CountRenderedCoalResources(FindUnityObjects(resourceRendererType), out string existingSamples);
            if (existing > 0)
            {
                summary = "existingCoalResources=" + existing + ", samples=" + existingSamples;
                return true;
            }

            if (TryCreateTransientOneActionResourceForSmoke(room, "Ore", "coal", out string createSummary))
            {
                TryRenderAllResourcesForSmoke(room);
                int created = CountRenderedCoalResources(FindUnityObjects(resourceRendererType), out string createdSamples);
                summary = "createdCoalResources=" + created + ", create={" + createSummary + "}, samples=" + createdSamples;
                return created > 0;
            }

            summary = "existingCoalResources=0, samples=" + existingSamples + ", create={" + createSummary + "}";
            return false;
        }

        private static int CountRenderedCoalResources(IEnumerable<object> renderers, out string samplesText)
        {
            int count = 0;
            List<string> samples = new List<string>();
            foreach (object renderer in renderers)
            {
                object? resource = ReadMember(renderer, "DungeonResource");
                if (resource == null || IsRemoved(resource))
                    continue;
                string resourceName = ReadStringMember(resource, "ResourceName", resource.GetType().Name);
                string resourceClass = ReadResourceClass(resource);
                int health = ReadIntMember(resource, "currentHealth", 0);
                if (samples.Count < 8)
                    samples.Add(resourceName + "/class=" + resourceClass + "/health=" + health);
                if (IsCoalResourceNameForSmoke(resourceName))
                    count++;
            }

            samplesText = samples.Count == 0 ? "none" : string.Join(" ; ", samples);
            return count;
        }
    }
}
