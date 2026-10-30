using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown.QA
{
    internal sealed partial class FishRoeTooltipObservationProbe
    {
        private static readonly Batch6HarmonyPatchTarget[] ProductHarmonyTargets =
        {
            new Batch6HarmonyPatchTarget("DolocTown.Item", "get_title", 0)
        };

        private readonly GameBridgeFixtureAccess access;
        private readonly bool productDisabled;
        private int queryCycles;
        private int clearCycles;
        private int environmentResetCountAtReadyQuery;
        private bool awaitingEnvironmentReset;

        internal FishRoeTooltipObservationProbe(GameBridgeFixtureAccess access, bool productDisabled = false)
        {
            this.access = access ?? throw new ArgumentNullException(nameof(access));
            this.productDisabled = productDisabled;
        }

        internal bool ShouldRunAfterSaveLoaded => !awaitingEnvironmentReset && queryCycles < 2;

        internal bool LifecycleComplete => queryCycles >= 2 && clearCycles >= 2 && !awaitingEnvironmentReset;

        internal string Run()
        {
            if (!ShouldRunAfterSaveLoaded)
                throw new InvalidOperationException("Fish roe ItemDisplayName lifecycle query was requested outside its next save cycle.");

            string details = QueryProductTitle();
            if (productDisabled) return details;
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
                "Yuuka.DTMAPI.FishBreedingAssistant.Native",
                ProductHarmonyTargets);
            if ((!productDisabled && (!owner.IsComplete || owner.ExactOwnerPatchCount != 1)) ||
                (productDisabled && (owner.ProductAssemblyLoaded ||
                    Batch6AdvancedHarmonyOwnerObserver.CountAllOwnerPatches("Yuuka.DTMAPI.FishBreedingAssistant.Native") != 0 ||
                    Batch6AdvancedHarmonyOwnerObserver.CountAllOwnerPatches("dtmapi.mod.yuuka.dtmapi.fishbreedingassistant") != 0 ||
                    access.Runtime.HasOwnerInstance("Yuuka.DTMAPI.FishBreedingAssistant"))))
                throw new InvalidOperationException("FishBreedingAssistant owner state did not match official enablement; disabled=" + productDisabled + "; " + owner.Details + ".");
            if (access.Runtime.CreateSnapshot().ConfigPages.Any(page => page.Manifest.UniqueID == "Yuuka.DTMAPI.FishBreedingAssistant"))
                throw new InvalidOperationException("The title-only FishBreedingAssistant must not register a configuration page.");

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

            return VerifyTitleSemantics(dolocApi, generateItem, roeItemId);
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

        internal static bool ContainsDecoration(string title, string expectedParent, string descriptionBefore, string descriptionAfter, string detailBefore, string detailAfter)
        {
            return !string.IsNullOrWhiteSpace(expectedParent) &&
                (title ?? string.Empty).EndsWith(" (" + expectedParent + ")", StringComparison.Ordinal) &&
                string.Equals(descriptionBefore, descriptionAfter, StringComparison.Ordinal) &&
                string.Equals(detailBefore, detailAfter, StringComparison.Ordinal);
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
