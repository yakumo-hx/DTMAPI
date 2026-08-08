using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown.QA
{
    internal sealed class FishRoeTooltipObservationProbe
    {
        private static readonly Batch6HarmonyPatchTarget[] ProductHarmonyTargets =
        {
            new Batch6HarmonyPatchTarget("DolocTown.Item", "get_title", 0)
        };

        private readonly GameBridgeFixtureAccess access;
        private int queryCycles;
        private int clearCycles;
        private int environmentResetCountAtReadyQuery;
        private bool awaitingEnvironmentReset;

        internal FishRoeTooltipObservationProbe(GameBridgeFixtureAccess access)
        {
            this.access = access ?? throw new ArgumentNullException(nameof(access));
        }

        internal bool ShouldRunAfterSaveLoaded => !awaitingEnvironmentReset && queryCycles < 2;

        internal bool LifecycleComplete => queryCycles >= 2 && clearCycles >= 2 && !awaitingEnvironmentReset;

        internal string Run()
        {
            if (!ShouldRunAfterSaveLoaded)
                throw new InvalidOperationException("Fish roe ItemDisplayName lifecycle query was requested outside its next save cycle.");

            string details = QueryProductTitle();
            queryCycles++;
            TryArmEnvironmentResetProof(repeatQueryAfterHookReady: false);
            return details + "; itemDisplayNameQueryCycle=" + queryCycles + "; environmentResetProof=" +
                (awaitingEnvironmentReset ? "armed" : "waiting-hook-ready");
        }

        internal void UpdateLifecycle()
        {
            if (LifecycleComplete || awaitingEnvironmentReset || queryCycles <= clearCycles)
                return;
            TryArmEnvironmentResetProof(repeatQueryAfterHookReady: true);
        }

        internal void OnReturnedToTitle()
        {
            if (queryCycles <= clearCycles)
                return;
            if (!awaitingEnvironmentReset)
                throw new InvalidOperationException("ReturnedToTitle arrived before the demanded SharedNative.EnvironmentReset Hook became ready.");
            if (access.Bridge.EnvironmentResetCountForTests <= environmentResetCountAtReadyQuery)
                throw new InvalidOperationException("ReturnedToTitle did not observe a real DolocAPI.SetEnvCamera environment-reset callback after the cached ItemDisplayName query.");

            RuntimeCapabilityDemandSnapshot route = GetItemDisplayNameRoute();
            if (route.TotalDemand != 0 || access.Bridge.HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.ItemDisplayNameEnvironmentReset))
                throw new InvalidOperationException("The real environment-reset callback did not release the ItemDisplayName demand and retained callback.");

            IHookStatusInfo cache = GetHookStatus("SharedNative.ItemDisplayNameEnvironmentReset")
                ?? throw new InvalidOperationException("ItemDisplayName environment-reset clear status was not published.");
            if (!cache.Status.Equals("cleared", StringComparison.OrdinalIgnoreCase) ||
                cache.Source.IndexOf("EnvironmentReset DolocAPI.SetEnvCamera", StringComparison.OrdinalIgnoreCase) < 0 ||
                cache.Details.IndexOf("demandReleased=true", StringComparison.OrdinalIgnoreCase) < 0)
            {
                throw new InvalidOperationException("ItemDisplayName did not retain the real SetEnvCamera clear status across later valid queries: status=" +
                    cache.Status + "; source=" + cache.Source + "; details=" + cache.Details + ".");
            }

            clearCycles++;
            awaitingEnvironmentReset = false;
            string details = "queries=" + queryCycles + "; realSetEnvCameraClears=" + clearCycles +
                "; cache=0; demand=0; retainedCallback=false; titleRecovered=true; owner=qa; fallback=false";
            access.SetHookStatus(
                "Smoke.ItemDisplayNameLifecycle",
                LifecycleComplete ? "verified" : "pending",
                "Fish/Animal product queries -> SharedNative.EnvironmentReset -> DolocAPI.SetEnvCamera",
                details);
            access.Log("ItemDisplayName lifecycle boundary observed " + details + ".");
        }

        private string QueryProductTitle()
        {
            Batch6HarmonyOwnerInventory owner = Batch6AdvancedHarmonyOwnerObserver.Observe(
                "Yuuka.DTMAPI.FishBreedingAssistant",
                "dtmapi.mod.yuuka.dtmapi.fishbreedingassistant",
                ProductHarmonyTargets);
            if (!owner.IsComplete || owner.ExactOwnerPatchCount != 1)
                throw new InvalidOperationException("FishBreedingAssistant product owner did not own exactly one Item.get_title patch: " + owner.Details + ".");

            Type dolocApi = ResolveType("DolocAPI", "Assembly-CSharp")
                ?? throw new MissingMemberException("DolocAPI was not visible.");
            string roeItemId = FindFishRoeItemId()
                ?? throw new MissingMemberException("Could not find an ItemFunctionFishRoe item id.");
            MethodInfo generateItem = dolocApi.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method =>
                {
                    ParameterInfo[] parameters = method.GetParameters();
                    return method.Name == "GenerateItem" && parameters.Length >= 1 && parameters.Length <= 2 &&
                        parameters[0].ParameterType == typeof(string) &&
                        (parameters.Length == 1 || parameters[1].ParameterType == typeof(int));
                }) ?? throw new MissingMethodException("DolocAPI.GenerateItem(string, int) was not found.");

            object item = generateItem.GetParameters().Length == 1
                ? generateItem.Invoke(null, new object[] { roeItemId })
                : generateItem.Invoke(null, new object[] { roeItemId, 1 });
            if (item == null)
                throw new InvalidOperationException("DolocAPI.GenerateItem returned null for " + roeItemId + ".");

            item.GetType().GetMethod("SetFishName", BindingFlags.Public | BindingFlags.Instance)?.Invoke(item, new object[] { "fish" });
            string title = item.GetType().GetProperty("title", BindingFlags.Public | BindingFlags.Instance)?.GetValue(item) as string ?? string.Empty;
            string description = item.GetType().GetProperty("description", BindingFlags.Public | BindingFlags.Instance)?.GetValue(item) as string ?? string.Empty;
            string detail = item.GetType().GetMethod("GetDetailInfo", BindingFlags.Public | BindingFlags.Instance)?.Invoke(item, null) as string ?? string.Empty;
            if (!ContainsDecoration(title, description, detail))
                throw new InvalidOperationException("Fish roe tooltip hooks did not append expected text.");

            return "item=" + roeItemId + "; title=" + title + "; detail=" + detail.Replace(Environment.NewLine, " | ") +
                "; productOwnerPatches=1; productOwnerTargets=1; transientOnly=true; owner=qa; fallback=false";
        }

        private void TryArmEnvironmentResetProof(bool repeatQueryAfterHookReady)
        {
            RuntimeCapabilityDemandSnapshot route = GetItemDisplayNameRoute();
            if (route.TotalDemand <= 0 || !access.Bridge.HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.ItemDisplayNameEnvironmentReset))
                throw new InvalidOperationException("Fish product query did not synchronously demand the ItemDisplayName environment-reset route.");

            IHookStatusInfo? environmentReset = GetHookStatus("SharedNative.EnvironmentReset");
            bool hookReady = environmentReset != null &&
                environmentReset.Status.Equals("experimental", StringComparison.OrdinalIgnoreCase) &&
                environmentReset.Source.IndexOf("DolocAPI.SetEnvCamera", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!hookReady)
            {
                access.SetHookStatus(
                    "Smoke.ItemDisplayNameLifecycle",
                    "pending",
                    "Fish/Animal product queries -> SharedNative.EnvironmentReset",
                    "queryCycle=" + queryCycles + "; demand=" + route.TotalDemand + "; waitingForHookReady=true; owner=qa; fallback=false");
                return;
            }

            if (repeatQueryAfterHookReady)
                QueryProductTitle();
            IHookStatusInfo cache = GetHookStatus("SharedNative.ItemDisplayNameCache")
                ?? throw new InvalidOperationException("A hook-ready ItemDisplayName product query did not publish cache activity.");
            if (!cache.Status.Equals("active", StringComparison.OrdinalIgnoreCase) ||
                cache.Details.IndexOf("hookReady=true", StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException("ItemDisplayName cache was not active after the shared physical Hook became ready.");

            environmentResetCountAtReadyQuery = access.Bridge.EnvironmentResetCountForTests;
            awaitingEnvironmentReset = true;
            access.SetHookStatus(
                "Smoke.ItemDisplayNameLifecycle",
                "pending",
                "Fish/Animal product queries -> SharedNative.EnvironmentReset -> DolocAPI.SetEnvCamera",
                "queryCycle=" + queryCycles + "; demand=" + route.TotalDemand + "; hookReady=true; cacheActive=true; waitingForRealSetEnvCamera=true; owner=qa; fallback=false");
        }

        private RuntimeCapabilityDemandSnapshot GetItemDisplayNameRoute() =>
            access.Runtime.RuntimeDemandSnapshot.Capabilities.Single(item =>
                item.CapabilityId.Equals(GameBridgeDemandRoutes.ItemDisplayNameEnvironmentReset, StringComparison.OrdinalIgnoreCase));

        private IHookStatusInfo? GetHookStatus(string hookId) =>
            access.Runtime.Diagnostics.GetHookStatuses().LastOrDefault(item =>
                item.HookId.Equals(hookId, StringComparison.OrdinalIgnoreCase));

        internal static bool ContainsDecoration(string title, string description, string detail)
        {
            return (title ?? string.Empty).IndexOf(" (", StringComparison.Ordinal) >= 0 ||
                (description ?? string.Empty).IndexOf("Hatches:", StringComparison.Ordinal) >= 0 ||
                (detail ?? string.Empty).IndexOf("Hatches:", StringComparison.Ordinal) >= 0;
        }

        private static string? FindFishRoeItemId()
        {
            Type? dolocConfig = ResolveType("DolocTown.Config.DolocConfig", "Assembly-CSharp");
            object? tables = dolocConfig?.GetProperty("Tables", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            object? tbItem = tables?.GetType().GetProperty("TbItem", BindingFlags.Public | BindingFlags.Instance)?.GetValue(tables);
            object? dataMap = tbItem?.GetType().GetProperty("DataMap", BindingFlags.Public | BindingFlags.Instance)?.GetValue(tbItem);
            if (!(dataMap is IEnumerable entries))
                return null;

            foreach (object entry in entries)
            {
                Type entryType = entry.GetType();
                object? key = entryType.GetProperty("Key")?.GetValue(entry);
                object? itemInfo = entryType.GetProperty("Value")?.GetValue(entry);
                object? function = itemInfo?.GetType().GetProperty("Function", BindingFlags.Public | BindingFlags.Instance)?.GetValue(itemInfo);
                if (function?.GetType().FullName != "DolocTown.Config.Item.ItemFunctionFishRoe")
                    continue;
                object? id = itemInfo?.GetType().GetProperty("Id", BindingFlags.Public | BindingFlags.Instance)?.GetValue(itemInfo);
                return id as string ?? key?.ToString();
            }
            return null;
        }

        private static Type? ResolveType(string fullName, string assemblyName)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => string.Equals(assembly.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase))
                .Select(assembly => assembly.GetType(fullName, throwOnError: false, ignoreCase: false))
                .FirstOrDefault(type => type != null);
        }
    }
}
