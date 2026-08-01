using System;
using System.Linq;
using System.Reflection;

namespace DTMAPI.GameBridge.DolocTown.QA
{
    internal sealed class NativeLoadContinuationProbe
    {
        private const string QaHarmonyOwnerPrefix = "dtmapi.gamebridge.doloctown.qa.native-continuation.";
        private readonly GameBridgeFixtureAccess access;
        private readonly HarmonyReflectionPatcher patcher;
        private bool afterLoadPrefixPatched;
        private bool afterLoadPostfixPatched;
        private bool loadAllPrefixPatched;
        private bool loadAllPostfixPatched;
        private bool loadBeyondPrefixPatched;
        private bool loadBeyondPostfixPatched;
        private bool mapInitPrefixPatched;
        private bool mapInitPostfixPatched;
        private DateTimeOffset nextAttemptUtc;
        private bool complete;
        private bool closed;

        internal NativeLoadContinuationProbe(GameBridgeFixtureAccess access)
        {
            this.access = access ?? throw new ArgumentNullException(nameof(access));
            patcher = new HarmonyReflectionPatcher(access.Runtime, QaHarmonyOwnerPrefix + access.RunId);
        }

        internal void Update(string operation)
        {
            if (closed || complete || DateTimeOffset.UtcNow < nextAttemptUtc)
                return;
            nextAttemptUtc = DateTimeOffset.UtcNow.AddSeconds(2);

            EnsurePatchPair(
                ref afterLoadPrefixPatched,
                ref afterLoadPostfixPatched,
                "DolocAPI, Assembly-CSharp",
                "AfterLoadArchiveData",
                nameof(DolocTownHookCallbacks.NativeContinuationAfterLoadArchiveDataPrefix),
                nameof(DolocTownHookCallbacks.NativeContinuationAfterLoadArchiveDataPostfix),
                1);
            EnsurePatchPair(
                ref loadAllPrefixPatched,
                ref loadAllPostfixPatched,
                "DolocTown.VersionPatcher, Assembly-CSharp",
                "LoadAllVersionPatches",
                nameof(DolocTownHookCallbacks.NativeContinuationVersionPatcherLoadAllPrefix),
                nameof(DolocTownHookCallbacks.NativeContinuationVersionPatcherLoadAllPostfix),
                0);
            EnsurePatchPair(
                ref loadBeyondPrefixPatched,
                ref loadBeyondPostfixPatched,
                "DolocTown.VersionPatcher, Assembly-CSharp",
                "LoadAllVersionPatchesBeyond",
                nameof(DolocTownHookCallbacks.NativeContinuationVersionPatcherLoadBeyondPrefix),
                nameof(DolocTownHookCallbacks.NativeContinuationVersionPatcherLoadBeyondPostfix),
                1);
            EnsurePatchPair(
                ref mapInitPrefixPatched,
                ref mapInitPostfixPatched,
                "DolocTown.MapManager, Assembly-CSharp",
                "Init",
                nameof(DolocTownHookCallbacks.NativeContinuationMapManagerInitPrefix),
                nameof(DolocTownHookCallbacks.NativeContinuationMapManagerInitPostfix),
                1);

            PublishStatus(operation);
            complete = afterLoadPrefixPatched && afterLoadPostfixPatched &&
                loadAllPrefixPatched && loadAllPostfixPatched &&
                loadBeyondPrefixPatched && loadBeyondPostfixPatched &&
                mapInitPrefixPatched && mapInitPostfixPatched;
        }

        internal bool Close(string reason)
        {
            if (closed)
                return true;

            string installedBefore =
                "afterLoad=" + FormatStatus(afterLoadPrefixPatched, afterLoadPostfixPatched) +
                "; loadAll=" + FormatStatus(loadAllPrefixPatched, loadAllPostfixPatched) +
                "; loadBeyond=" + FormatStatus(loadBeyondPrefixPatched, loadBeyondPostfixPatched) +
                "; mapInit=" + FormatStatus(mapInitPrefixPatched, mapInitPostfixPatched);
            bool unpatchSucceeded = patcher.TryUnpatchAllOwnedPatches();
            string details =
                "ownerId=" + patcher.OwnerId +
                "; unpatchSucceeded=" + unpatchSucceeded +
                "; installedBefore=" + installedBefore +
                "; reason=" + (reason ?? string.Empty) +
                "; owner=qa";
            if (!unpatchSucceeded)
            {
                access.Log("Native continuation probe close failed; " + details + ".");
                access.SetHookStatus(
                    "Smoke.NativeLoadContinuationProbe",
                    "cleanup-failed",
                    "optional QA G6 probe",
                    details);
                return false;
            }

            afterLoadPrefixPatched = false;
            afterLoadPostfixPatched = false;
            loadAllPrefixPatched = false;
            loadAllPostfixPatched = false;
            loadBeyondPrefixPatched = false;
            loadBeyondPostfixPatched = false;
            mapInitPrefixPatched = false;
            mapInitPostfixPatched = false;
            complete = false;
            closed = true;
            access.Log("Native continuation probe closed; " + details + ".");
            access.SetHookStatus(
                "Smoke.NativeLoadContinuationProbe",
                "closed",
                "optional QA G6 probe",
                details);
            return true;
        }

        private void EnsurePatchPair(
            ref bool prefixPatched,
            ref bool postfixPatched,
            string targetTypeName,
            string methodName,
            string prefixName,
            string postfixName,
            int parameterCount)
        {
            MethodInfo? prefix = typeof(DolocTownHookCallbacks).GetMethod(prefixName, BindingFlags.Public | BindingFlags.Static);
            MethodInfo? postfix = typeof(DolocTownHookCallbacks).GetMethod(postfixName, BindingFlags.Public | BindingFlags.Static);
            if (!prefixPatched)
                prefixPatched = patcher.TryPatchPrefix(targetTypeName, methodName, prefix, parameterCount);
            if (!postfixPatched)
                postfixPatched = patcher.TryPatchPostfix(targetTypeName, methodName, postfix, parameterCount);
        }

        private void PublishStatus(string operation)
        {
            string afterLoadStatus = FormatStatus(afterLoadPrefixPatched, afterLoadPostfixPatched);
            string loadAllStatus = FormatStatus(loadAllPrefixPatched, loadAllPostfixPatched);
            string loadBeyondStatus = FormatStatus(loadBeyondPrefixPatched, loadBeyondPostfixPatched);
            string mapInitStatus = FormatStatus(mapInitPrefixPatched, mapInitPostfixPatched);
            string[] supported = new[]
            {
                afterLoadStatus == "installed" ? "DolocAPI.AfterLoadArchiveData" : string.Empty,
                loadAllStatus == "installed" ? "DolocTown.VersionPatcher.LoadAllVersionPatches" : string.Empty,
                loadBeyondStatus == "installed" ? "DolocTown.VersionPatcher.LoadAllVersionPatchesBeyond" : string.Empty,
                mapInitStatus == "installed" ? "DolocTown.MapManager.Init" : string.Empty
            }.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
            string[] unsupported = new[]
            {
                afterLoadStatus == "installed" ? string.Empty : "DolocAPI.AfterLoadArchiveData(" + afterLoadStatus + ")",
                loadAllStatus == "installed" ? string.Empty : "DolocTown.VersionPatcher.LoadAllVersionPatches(" + loadAllStatus + ")",
                loadBeyondStatus == "installed" ? string.Empty : "DolocTown.VersionPatcher.LoadAllVersionPatchesBeyond(" + loadBeyondStatus + ")",
                mapInitStatus == "installed" ? string.Empty : "DolocTown.MapManager.Init(" + mapInitStatus + ")",
                "DolocTown.TextureUtils.DrawArea(skipped-high-frequency)"
            }.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
            string details =
                "SmokeNativeLoadContinuationProbe=VersionPatcher; " +
                "AfterLoadArchiveDataHook=" + afterLoadStatus + "; " +
                "VersionPatcherLoadAllHook=" + loadAllStatus + "; " +
                "VersionPatcherLoadBeyondHook=" + loadBeyondStatus + "; " +
                "MapManagerInitHook=" + mapInitStatus + "; " +
                "TextureUtilsDrawAreaHook=unsupported-skipped-high-frequency; " +
                "Supported=" + (supported.Length == 0 ? "none" : string.Join("|", supported)) + "; " +
                "Unsupported=" + (unsupported.Length == 0 ? "none" : string.Join("|", unsupported)) + "; " +
                "operation=" + (operation ?? string.Empty) + "; ownerId=" + patcher.OwnerId + "; owner=qa";
            access.Log(details + ".");
            access.SetHookStatus(
                "Smoke.NativeLoadContinuationProbe",
                supported.Length >= 3 ? "active" : "partial",
                "optional QA G6 probe",
                details);
        }

        private static string FormatStatus(bool prefixPatched, bool postfixPatched)
        {
            if (prefixPatched && postfixPatched)
                return "installed";
            if (prefixPatched || postfixPatched)
                return "partial";
            return "unsupported";
        }
    }
}
