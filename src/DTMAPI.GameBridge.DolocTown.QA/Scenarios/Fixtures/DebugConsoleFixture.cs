using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class QaScenarioController
    {
        private static int debugConsoleSaveProbeCalls;
        private static bool debugConsoleSaveProbeReject;

        private void TryExerciseInstantSaveForFixture()
        {
            try
            {
                if (DebugConsoleActions.ProductEntry != null)
                {
                    ExerciseProductSaveHereConfirmationForFixture(
                        DebugConsoleActions.ProductEntry);
                    return;
                }

                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                MethodInfo? saveGame = FindMethod(dolocApi, "SaveGame", 1);
                if (dolocApi == null || saveGame == null)
                    throw new MissingMethodException("DolocAPI.SaveGame(int) was not found.");

                InstantSaveSnapshot before = CaptureInstantSaveSnapshot(dolocApi);
                int gameIndex = before.ArchiveIndex ?? pendingAutoLoadGameIndex ?? 0;

                runtime.RuntimeMonitor.Log("DTMAPI debug instant save requested slot/index=" + gameIndex + " before=" + before.ToLogString() + ".");
                runtime.SetHookStatus("Smoke.InstantSave", "pending", "DolocAPI.SaveGame", "Requested native save from current scene: " + before.ToLogString());
                object? saveResult = saveGame.Invoke(null, new object[] { gameIndex });
                if (saveResult is bool saved && !saved)
                {
                    runtime.SetHookStatus("Smoke.InstantSave", "failed", "DolocAPI.SaveGame", "SaveGame returned false for slot/index " + gameIndex + ".");
                    return;
                }

                InstantSaveSnapshot afterSave = CaptureInstantSaveSnapshot(dolocApi);
                bool sameRoom = before.RoomId.Equals(afterSave.RoomId, StringComparison.OrdinalIgnoreCase);
                double distance = before.DistanceTo(afterSave);
                string summary =
                    "slot/index=" + gameIndex +
                    ", before={" + before.ToLogString() + "}" +
                    ", afterSave={" + afterSave.ToLogString() + "}" +
                    ", sameRoom=" + sameRoom +
                    ", distance=" + (double.IsNaN(distance) ? "unknown" : distance.ToString("0.###", CultureInfo.InvariantCulture)) +
                    ", reloadDisabled=True";

                runtime.RuntimeMonitor.Log("Smoke exercise InstantSave OK " + summary);
                runtime.SetHookStatus("Smoke.InstantSave", "verified", "DolocAPI.SaveGame", summary);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke instant-save exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.InstantSave", "failed", "DolocAPI.SaveGame", ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void ExerciseProductSaveHereConfirmationForFixture(
            object productEntry)
        {
            object ui =
                productEntry.GetType().GetField(
                        "ui",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic)
                    ?.GetValue(productEntry) ??
                throw new MissingFieldException(
                    productEntry.GetType().FullName,
                    "ui");
            MethodInfo saveHere =
                ui.GetType().GetMethod(
                    "SaveHere",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic) ??
                throw new MissingMethodException(
                    ui.GetType().FullName,
                    "SaveHere");
            FieldInfo confirmation =
                ui.GetType().GetField(
                    "saveConfirmationArmed",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic) ??
                throw new MissingFieldException(
                    ui.GetType().FullName,
                    "saveConfirmationArmed");
            FieldInfo status =
                ui.GetType().GetField(
                    "statusMessage",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic) ??
                throw new MissingFieldException(
                    ui.GetType().FullName,
                    "statusMessage");
            var failurePatcher = new HarmonyReflectionPatcher(
                runtime,
                "dtmapi.qa.debugconsole-save-failure." +
                access.RunId);
            MethodInfo probe =
                typeof(QaScenarioController).GetMethod(
                    nameof(DebugConsoleSaveGameProbePrefix),
                    BindingFlags.Static |
                    BindingFlags.NonPublic) ??
                throw new MissingMethodException(
                    typeof(QaScenarioController).FullName,
                    nameof(DebugConsoleSaveGameProbePrefix));
            Interlocked.Exchange(
                ref debugConsoleSaveProbeCalls,
                0);
            debugConsoleSaveProbeReject = false;
            bool unpatched = false;
            try
            {
                if (!failurePatcher.TryPatchPrefix(
                        "DolocAPI",
                        "SaveGame",
                        probe,
                        parameterCount: 1))
                {
                    throw new InvalidOperationException(
                        "Could not install the QA-only SaveGame confirmation/failure probe.");
                }

                saveHere.Invoke(ui, null);
                if (!(confirmation.GetValue(ui) is bool armed) ||
                    !armed ||
                    Volatile.Read(
                        ref debugConsoleSaveProbeCalls) != 0)
                {
                    throw new InvalidOperationException(
                        "The first Save here click did not remain confirmation-only.");
                }

                debugConsoleSaveProbeReject = true;
                saveHere.Invoke(ui, null);
                string failedStatus =
                    status.GetValue(ui)?.ToString() ??
                    string.Empty;
                if (Volatile.Read(
                        ref debugConsoleSaveProbeCalls) != 1 ||
                    failedStatus.IndexOf(
                        "native-rejected",
                        StringComparison.OrdinalIgnoreCase) < 0 ||
                    (confirmation.GetValue(ui) is bool failedArmed &&
                     failedArmed))
                {
                    throw new InvalidOperationException(
                        "The confirmed failure path did not propagate native-rejected and clear the confirmation lease. status=" +
                        failedStatus + ".");
                }

                debugConsoleSaveProbeReject = false;
                saveHere.Invoke(ui, null);
                if (!(confirmation.GetValue(ui) is bool successArmed) ||
                    !successArmed ||
                    Volatile.Read(
                        ref debugConsoleSaveProbeCalls) != 1)
                {
                    throw new InvalidOperationException(
                        "The success retry first click did not arm without another SaveGame call.");
                }
                saveHere.Invoke(ui, null);
                string successStatus =
                    status.GetValue(ui)?.ToString() ??
                    string.Empty;
                if (Volatile.Read(
                        ref debugConsoleSaveProbeCalls) != 2 ||
                    successStatus.IndexOf(
                        "native-rejected",
                        StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (confirmation.GetValue(ui) is bool finalArmed &&
                     finalArmed))
                {
                    throw new InvalidOperationException(
                        "The confirmed success path did not call native SaveGame exactly once and clear confirmation. status=" +
                        successStatus + ".");
                }

                string summary =
                    "owner=ProductNative" +
                    ", firstClickSaveCalls=0" +
                    ", injectedFailureCalls=1" +
                    ", successCalls=1" +
                    ", failurePropagated=true" +
                    ", confirmationCleared=true" +
                    ", saveMode=NativeSaveExpected" +
                    ", disposableRedirect=true";
                runtime.RuntimeMonitor.Log(
                    "Smoke exercise InstantSave OK " +
                    summary);
                runtime.SetHookStatus(
                    "Smoke.InstantSave",
                    "verified",
                    "DebugConsole Save here two-click UI + QA-only SaveGame failure injection",
                    summary);
            }
            finally
            {
                debugConsoleSaveProbeReject = false;
                Interlocked.Exchange(
                    ref debugConsoleSaveProbeCalls,
                    0);
                unpatched =
                    failurePatcher.TryUnpatchAllOwnedPatches();
                if (!unpatched)
                {
                    throw new InvalidOperationException(
                        "The QA-only DebugConsole SaveGame failure probe did not unpatch.");
                }
            }
        }

        private static bool DebugConsoleSaveGameProbePrefix(
            ref bool __result)
        {
            Interlocked.Increment(
                ref debugConsoleSaveProbeCalls);
            if (!debugConsoleSaveProbeReject)
                return true;
            __result = false;
            return false;
        }

        private void
            TryExerciseDebugConsoleSaveAcceptanceForFixture()
        {
            const string hookId =
                "Smoke.DebugConsoleSaveAcceptance";
            try
            {
                if (DebugConsoleActions.ProductEntry == null)
                {
                    throw new InvalidOperationException(
                        "The save-commit acceptance requires the admitted DebugConsole ProductNative owner.");
                }
                string phase =
                    debugConsoleSaveAcceptancePhase;
                int before = ReadCurrentMoneyForFixture();
                if (phase.Equals(
                        "ColdObserve",
                        StringComparison.Ordinal))
                {
                    if (before != expectedDebugConsoleMoney)
                    {
                        throw new InvalidOperationException(
                            "Cold reload money mismatch: expected=" +
                            expectedDebugConsoleMoney +
                            " actual=" + before + ".");
                    }
                    string coldSummary =
                        "phase=ColdObserve" +
                        ", exactMoney=" + before +
                        ", expectedMoney=" +
                        expectedDebugConsoleMoney +
                        ", saveMode=NoNativeSave" +
                        ", disposableRedirect=true" +
                        ", nativeSaveCalls=0";
                    runtime.RuntimeMonitor.Log(
                        "Smoke exercise DebugConsoleSaveAcceptance OK " +
                        coldSummary);
                    runtime.SetHookStatus(
                        hookId,
                        "verified",
                        "fresh process + isolated native archive read",
                        coldSummary);
                    return;
                }

                if (expectedDebugConsoleMoney >= 0 &&
                    before != expectedDebugConsoleMoney)
                {
                    throw new InvalidOperationException(
                        "Mutation phase did not start from the exact expected committed money: expected=" +
                        expectedDebugConsoleMoney +
                        " actual=" + before + ".");
                }
                int delta =
                    phase.Equals(
                        "FailedMutation",
                        StringComparison.Ordinal)
                        ? 137
                        : 293;
                DebugValueResult mutation =
                    DebugConsoleActions.AddMoney(
                        CreateFixtureManifest(),
                        delta);
                if (!mutation.Success ||
                    mutation.BeforeValue != before ||
                    mutation.AfterValue != before + delta ||
                    ReadCurrentMoneyForFixture() !=
                        mutation.AfterValue)
                {
                    throw new InvalidOperationException(
                        "Working money mutation was not exact. " +
                        mutation.Message);
                }

                if (phase.Equals(
                        "FailedMutation",
                        StringComparison.Ordinal))
                {
                    ExerciseRejectedSaveForWorkingMutation(
                        before,
                        mutation.AfterValue);
                    return;
                }
                if (!phase.Equals(
                        "SuccessfulMutation",
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Unsupported DebugConsole save acceptance phase: " +
                        phase + ".");
                }

                InstantSaveDebugResult saved =
                    DebugConsoleActions.Save(
                        CreateFixtureManifest(),
                        reloadAfterSave: false);
                if (!saved.Success)
                {
                    throw new InvalidOperationException(
                        "Native save rejected the successful-mutation phase: " +
                        saved.FailureReason + ": " +
                        saved.Message);
                }
                int committed = ReadCurrentMoneyForFixture();
                if (committed != mutation.AfterValue)
                {
                    throw new InvalidOperationException(
                        "The successful native save changed the exact Working money unexpectedly: expected=" +
                        mutation.AfterValue +
                        " actual=" + committed + ".");
                }
                string successSummary =
                    "phase=SuccessfulMutation" +
                    ", priorCommittedMoney=" + before +
                    ", workingMoney=" + mutation.AfterValue +
                    ", expectedColdMoney=" + committed +
                    ", nativeSaveSuccess=true" +
                    ", saveMode=NativeSaveExpected" +
                    ", disposableRedirect=true";
                runtime.RuntimeMonitor.Log(
                    "Smoke exercise DebugConsoleSaveAcceptance OK " +
                    successSummary);
                runtime.SetHookStatus(
                    hookId,
                    "verified",
                    "ProductNative AddMoney -> native SaveGame",
                    successSummary);
            }
            catch (Exception error)
            {
                runtime.Diagnostics.RecordError(
                    "DTMAPI.GameBridge",
                    "DebugConsole save-commit acceptance failed.",
                    error.ToString());
                runtime.SetHookStatus(
                    hookId,
                    "failed",
                    "isolated ProductNative save-commit fixture",
                    error.GetType().Name + ": " +
                    error.Message);
            }
        }

        private void ExerciseRejectedSaveForWorkingMutation(
            int priorCommitted,
            int working)
        {
            var failurePatcher =
                new HarmonyReflectionPatcher(
                    runtime,
                    "dtmapi.qa.debugconsole-working-save-failure." +
                    access.RunId);
            MethodInfo probe =
                typeof(QaScenarioController).GetMethod(
                    nameof(DebugConsoleSaveGameProbePrefix),
                    BindingFlags.Static |
                    BindingFlags.NonPublic) ??
                throw new MissingMethodException(
                    typeof(QaScenarioController).FullName,
                    nameof(DebugConsoleSaveGameProbePrefix));
            Interlocked.Exchange(
                ref debugConsoleSaveProbeCalls,
                0);
            debugConsoleSaveProbeReject = true;
            try
            {
                if (!failurePatcher.TryPatchPrefix(
                        "DolocAPI",
                        "SaveGame",
                        probe,
                        parameterCount: 1))
                {
                    throw new InvalidOperationException(
                        "Could not install the QA-only Working-save failure probe.");
                }
                InstantSaveDebugResult failed =
                    DebugConsoleActions.Save(
                        CreateFixtureManifest(),
                        reloadAfterSave: false);
                if (failed.Success ||
                    Volatile.Read(
                        ref debugConsoleSaveProbeCalls) != 1 ||
                    failed.FailureReason.IndexOf(
                        "native-rejected",
                        StringComparison.OrdinalIgnoreCase) < 0 ||
                    ReadCurrentMoneyForFixture() != working)
                {
                    throw new InvalidOperationException(
                        "Rejected Working save did not preserve the unsaved in-memory mutation and propagate native-rejected. result=" +
                        failed.FailureReason + ": " +
                        failed.Message + ".");
                }
                string failedSummary =
                    "phase=FailedMutation" +
                    ", priorCommittedMoney=" +
                    priorCommitted +
                    ", workingMoney=" + working +
                    ", expectedColdMoney=" +
                    priorCommitted +
                    ", nativeSaveRejected=true" +
                    ", saveMode=NativeSaveExpected" +
                    ", disposableRedirect=true";
                runtime.RuntimeMonitor.Log(
                    "Smoke exercise DebugConsoleSaveAcceptance OK " +
                    failedSummary);
                runtime.SetHookStatus(
                    "Smoke.DebugConsoleSaveAcceptance",
                    "verified",
                    "ProductNative AddMoney -> QA-rejected native SaveGame",
                    failedSummary);
            }
            finally
            {
                debugConsoleSaveProbeReject = false;
                Interlocked.Exchange(
                    ref debugConsoleSaveProbeCalls,
                    0);
                if (!failurePatcher.TryUnpatchAllOwnedPatches())
                {
                    throw new InvalidOperationException(
                        "The QA-only Working-save failure probe did not unpatch.");
                }
            }
        }

        private int ReadCurrentMoneyForFixture()
        {
            patcher ??=
                new HarmonyReflectionPatcher(runtime);
            Type? dolocApi =
                patcher.ResolveType(
                    "DolocAPI, Assembly-CSharp");
            object? archive =
                ReadStaticMember(
                    dolocApi ??
                    throw new TypeLoadException(
                        "DolocAPI was not found."),
                    "archiveHandle");
            if (archive == null)
            {
                throw new InvalidOperationException(
                    "DolocAPI.archiveHandle is unavailable.");
            }
            return ReadIntMember(
                archive,
                "CurrentMoney",
                int.MinValue) is int money &&
                money != int.MinValue
                ? money
                : throw new MissingMemberException(
                    archive.GetType().FullName,
                    "CurrentMoney");
        }

        private void TryExerciseDebugInventoryForFixture()
        {
            try
            {
                InventoryDebugItem? item = SelectInventorySmokeItem(inventoryDebugApi);
                if (item == null)
                    throw new InvalidOperationException("No spawnable official item was found in TbItem.");

                InventoryGiveResult result = inventoryDebugApi.GiveItem(CreateFixtureManifest(), item.Id, 1);
                if (!result.Success)
                    throw new InvalidOperationException(result.FailureReason + ": " + result.Message);

                string modSummary = "modItem=not-found";
                InventoryDebugItem? modItem = SelectModInventorySmokeItem(inventoryDebugApi);
                if (modItem != null)
                {
                    InventoryGiveResult modResult = inventoryDebugApi.GiveItem(CreateFixtureManifest(), modItem.Id, 1);
                    if (!modResult.Success)
                        throw new InvalidOperationException("Mod item give failed " + modItem.Id + ": " + modResult.FailureReason + ": " + modResult.Message);

                    modSummary = "modItem=" + modResult.ItemId +
                        ", modDisplay=" + FirstNonEmpty(modResult.DisplayName, modItem.DisplayName, modItem.Id) +
                        ", sourceKind=" + modItem.SourceKind +
                        ", sourceTitle=" + modItem.SourceModTitle +
                        ", sourceId=" + modItem.SourceId +
                        ", workshopId=" + (modItem.WorkshopId.HasValue ? modItem.WorkshopId.Value.ToString() : "none") +
                        ", runtimeLoaded=" + modItem.RuntimeLoaded +
                        ", before=" + modResult.BeforeCount +
                        ", after=" + modResult.AfterCount +
                        ", given=" + modResult.GivenCount +
                        (modItem.SourceKind.Equals("Workshop", StringComparison.OrdinalIgnoreCase) ? ", workshopRuntimeItem=verified" : ", workshopRuntimeItem=not-found");
                }

                string summary = "item=" + result.ItemId +
                    ", display=" + FirstNonEmpty(result.DisplayName, item.DisplayName, item.Id) +
                    ", before=" + result.BeforeCount +
                    ", after=" + result.AfterCount +
                    ", given=" + result.GivenCount +
                    ", " + modSummary +
                    ", nativeOwner=" + DebugConsoleActions.OwnerKind;
                DebugConsoleActions.ValidateOwnerBoundaryAfterAction();
                runtime.RuntimeMonitor.Log("Smoke exercise DebugInventory OK " + summary);
                runtime.SetHookStatus("Smoke.DebugInventory", "verified", "IInventoryDebugApi -> DolocAPI.TryPlaceInBackpack", summary);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke debug inventory exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.DebugInventory", "failed", "IInventoryDebugApi", ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static InventoryDebugItem? SelectInventorySmokeItem(IInventoryDebugApi api)
        {
            string[] preferredIds = { "wood", "stone", "roughage_feed", "seed_endyam" };
            foreach (string preferredId in preferredIds)
            {
                InventoryDebugPage page = api.GetItems(new InventoryDebugQuery { SearchText = preferredId, PageSize = 50 });
                InventoryDebugItem? exact = page.Items.FirstOrDefault(i => i.CanSpawn && i.Id.Equals(preferredId, StringComparison.OrdinalIgnoreCase));
                if (exact != null)
                    return exact;

                InventoryDebugItem? partial = page.Items.FirstOrDefault(i => i.CanSpawn && i.Id.IndexOf(preferredId, StringComparison.OrdinalIgnoreCase) >= 0);
                if (partial != null)
                    return partial;
            }

            return api.GetItems(new InventoryDebugQuery { PageSize = 50 }).Items.FirstOrDefault(i => i.CanSpawn);
        }

        private static InventoryDebugItem? SelectModInventorySmokeItem(IInventoryDebugApi api)
        {
            string? requiredId = Environment.GetEnvironmentVariable("DTMAPI_QA_DEBUG_INVENTORY_ITEM_ID");
            if (!string.IsNullOrWhiteSpace(requiredId))
            {
                InventoryDebugPage requiredPage = api.GetItems(new InventoryDebugQuery { SearchText = requiredId, ModItemsOnly = true, IncludeUnavailable = true, PageSize = 200 });
                return requiredPage.Items.FirstOrDefault(i => i.CanGive && i.RuntimeLoaded && i.IsModItem && i.Id.Equals(requiredId, StringComparison.Ordinal))
                    ?? throw new InvalidOperationException("Required inventory fixture item is unavailable: " + requiredId);
            }

            InventoryDebugPage page = api.GetItems(new InventoryDebugQuery { ModItemsOnly = true, IncludeUnavailable = true, PageSize = 200 });
            InventoryDebugItem? preferredButter = page.Items
                .Where(i => i.CanGive && i.RuntimeLoaded && i.IsModItem)
                .Where(i => i.SourceKind.Equals("Workshop", StringComparison.OrdinalIgnoreCase))
                .Where(i => i.WorkshopId == 3722791728UL || i.SourceId.Equals("Workshop.3722791728", StringComparison.OrdinalIgnoreCase))
                .Where(i => i.Id.Equals("mod_butter", StringComparison.OrdinalIgnoreCase) || ContainsIgnoreCase(i.DisplayName, "黄油") || ContainsIgnoreCase(i.SearchText, "butter"))
                .OrderBy(i => i.Id.Equals("mod_butter", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(i => i.RuntimeOrder)
                .ThenBy(i => i.Id, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
            if (preferredButter != null)
                return preferredButter;

            InventoryDebugItem? workshop = page.Items
                .Where(i => i.CanGive && i.RuntimeLoaded && i.IsModItem)
                .Where(i => i.SourceKind.Equals("Workshop", StringComparison.OrdinalIgnoreCase))
                .OrderBy(i => i.RuntimeOrder)
                .ThenBy(i => i.Id, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
            if (workshop != null)
                return workshop;

            return page.Items
                .Where(i => i.CanGive && i.RuntimeLoaded && i.IsModItem)
                .OrderBy(i => i.RuntimeOrder)
                .ThenBy(i => i.Id, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
        }

        private void TryExerciseDebugWeatherForFixture()
        {
            try
            {
                WeatherDebugState state = weatherDebugApi.GetState();
                IReadOnlyList<WeatherDebugOption> options = weatherDebugApi.GetAvailableWeathers();
                WeatherDebugOption? option = options.FirstOrDefault(w => !w.IsCurrent)
                    ?? options.FirstOrDefault();
                if (option == null)
                    throw new InvalidOperationException("No native weather options were available.");

                WeatherSetResult result = weatherDebugApi.SetWeather(CreateFixtureManifest(), option.Id, patchCurrentPeriod: true);
                if (!result.Success)
                    throw new InvalidOperationException(result.FailureReason + ": " + result.Message);

                string summary = "options=" + options.Count +
                    ", season=" + state.SeasonName +
                    ", date=" + state.Year + "-" + state.Month + "-" + state.Day + " " + state.Hour +
                    ", before=" + result.BeforeWeatherId +
                    ", after=" + result.AfterWeatherId +
                    ", display=" + FirstNonEmpty(result.DisplayName, option.DisplayName, option.Id) +
                    ", currentDayForecast=" + option.IsCurrentDayForecast +
                    ", nativeOwner=" + DebugConsoleActions.OwnerKind;
                DebugConsoleActions.ValidateOwnerBoundaryAfterAction();
                runtime.RuntimeMonitor.Log("Smoke exercise DebugWeather OK " + summary);
                runtime.SetHookStatus("Smoke.DebugWeather", "verified", "IWeatherDebugApi -> ArchiveDataHandle.SetWeather/PatchWeather", summary);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke debug weather exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.DebugWeather", "failed", "IWeatherDebugApi", ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void TryExerciseDebugTimeForFixture()
        {
            try
            {
                var steps = new List<string>();
                for (int i = 0; i < 3; i++)
                {
                    TimeSkipResult result = timeDebugApi.SkipToNextWeatherPeriod(CreateFixtureManifest());
                    if (!result.Success)
                        throw new InvalidOperationException(result.FailureReason + ": " + result.Message);

                    steps.Add("step" + (i + 1) +
                        "{targetHour=" + result.TargetHour +
                        ", advancedMinutes=" + result.AdvancedGameMinutes +
                        ", advancedSeconds=" + result.AdvancedSeconds +
                        ", before=" + FormatTimeSnapshot(result.Before) +
                        ", after=" + FormatTimeSnapshot(result.After) +
                        "}");
                    runtime.RuntimeMonitor.Log("Smoke exercise DebugTime step OK " + steps[steps.Count - 1]);
                }

                string summary = "transitions=" +
                    string.Join(" -> ", steps.ToArray()) +
                    ", nativeOwner=" + DebugConsoleActions.OwnerKind;
                DebugConsoleActions.ValidateOwnerBoundaryAfterAction();
                runtime.RuntimeMonitor.Log("Smoke exercise DebugTime OK " + summary);
                runtime.SetHookStatus("Smoke.DebugTime", "verified", "ITimeDebugApi -> ArchiveDataHandle.PassTimeNoControl", summary);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke debug time exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.DebugTime", "failed", "ITimeDebugApi", ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void TryExerciseDebugMovementForFixture()
        {
            try
            {
                double[] levels = { 1, 2, 3, 4 };
                var samples = new List<string>();
                foreach (double level in levels)
                {
                    MovementSpeedResult result = movementDebugApi.SetSpeedMultiplier(CreateFixtureManifest(), level);
                    if (!result.Success)
                        throw new InvalidOperationException(result.FailureReason + ": " + result.Message);
                    samples.Add(level.ToString("0.#") + "x:" + FormatSmokeDouble(result.After.MoveSpeed));
                }
                MovementSpeedResult reset = movementDebugApi.ResetSpeed(CreateFixtureManifest(), "smoke-restore");
                if (!reset.Success || !reset.After.IsDefault)
                    throw new InvalidOperationException("Reset failed: " + reset.FailureReason + " " + reset.Message);

                string summary = "levels=" +
                    string.Join(",", samples.ToArray()) +
                    ", restored=" + reset.After.IsDefault +
                    ", originalScaler=" +
                    FormatSmokeDouble(reset.Before.MoveSpeed) +
                    ", finalSpeed=" +
                    FormatSmokeDouble(reset.After.MoveSpeed) +
                    ", nativeOwner=" +
                    DebugConsoleActions.OwnerKind;
                DebugConsoleActions.ValidateOwnerBoundaryAfterAction();
                runtime.RuntimeMonitor.Log("Smoke exercise DebugMovement OK " + summary);
                runtime.SetHookStatus("Smoke.DebugMovement", "verified", "IMovementDebugApi -> player BodyController.MoveSpeed Postfix", summary);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke debug movement exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.DebugMovement", "failed", "IMovementDebugApi", ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void TryExerciseAdvancedDebugForFixture()
        {
            try
            {
                ManifestModel owner = CreateDebugConsoleSmokeManifest();
                var required = new List<string>();
                var optional = new List<string>();

                IReadOnlyList<SpawnDebugOption> monsterOptions =
                    advancedDebugApi.GetMonsterOptions();
                SpawnDebugOption? monsterOption = monsterOptions.FirstOrDefault(option =>
                        option.IsAvailableInCurrentRoom &&
                        option.Id.Equals(
                            "space_ship",
                            StringComparison.OrdinalIgnoreCase)) ??
                    monsterOptions.FirstOrDefault(option =>
                        option.IsAvailableInCurrentRoom);
                SpawnDebugOption? resourceOption = advancedDebugApi.GetResourceOptions().FirstOrDefault(option => option.IsAvailableInCurrentRoom);
                if (monsterOption == null || resourceOption == null)
                {
                    runtime.SetHookStatus(
                        "Smoke.AdvancedDebug",
                        "pending",
                        "CurrentRoom IMonsterHost + IDungeonResourceHost preflight",
                        "Waiting for the loaded save to publish a room that supports the reviewed monster/resource spawn probes before any AdvancedDebug mutation. monsterReady=" + (monsterOption != null) + ", resourceReady=" + (resourceOption != null) + ".");
                    return;
                }

                TimeSkipResult day = advancedDebugApi.AdvanceTime(owner, AdvancedTimeAdvanceKind.Day, 1);
                if (!day.Success)
                    throw new InvalidOperationException("Advance day failed: " + day.FailureReason + ": " + day.Message);
                required.Add("day{seconds=" + day.AdvancedSeconds + ", before=" + FormatTimeSnapshot(day.Before) + ", after=" + FormatTimeSnapshot(day.After) + "}");

                TimeScaleDebugResult scale = advancedDebugApi.SetTimeScale(owner, 4);
                if (!scale.Success)
                    throw new InvalidOperationException("Set time scale failed: " + scale.FailureReason + ": " + scale.Message);
                TimeScaleDebugResult resetScale = advancedDebugApi.ResetTimeScale(owner, "advanced-smoke");
                if (!resetScale.Success)
                    throw new InvalidOperationException("Reset time scale failed: " + resetScale.FailureReason + ": " + resetScale.Message);
                required.Add("scale{set=" + FormatSmokeDouble(scale.AfterMultiplier) + ", reset=" + FormatSmokeDouble(resetScale.AfterMultiplier) + "}");

                DebugValueResult money = advancedDebugApi.AddMoney(owner, 1);
                if (!money.Success)
                    throw new InvalidOperationException("Add money failed: " + money.FailureReason + ": " + money.Message);
                required.Add("money{" + money.BeforeValue + "->" + money.AfterValue + "}");

                TechPointDebugOption? techOption = advancedDebugApi.GetTechPointOptions().FirstOrDefault();
                if (techOption == null)
                    throw new InvalidOperationException("No tech point options were available.");
                DebugValueResult tech = advancedDebugApi.AddTechPoint(owner, techOption.Id, 1);
                if (!tech.Success)
                    throw new InvalidOperationException("Add tech point failed: " + tech.FailureReason + ": " + tech.Message);
                required.Add("tech{" + techOption.Id + ":" + tech.BeforeValue + "->" + tech.AfterValue + "}");

                CreativeModeResult creativeOn = advancedDebugApi.SetCreativeMode(owner, true);
                CreativeModeResult? creativeOff = null;
                string creativeSmoke;
                try
                {
                    if (!creativeOn.Success || !creativeOn.After.RuntimeHooksInstalled)
                        throw new InvalidOperationException("Creative enable failed: " + FirstNonEmpty(creativeOn.FailureReason, creativeOn.Message));
                    creativeSmoke = VerifyAdvancedCreativeHooksForFixture();
                }
                finally
                {
                    creativeOff = advancedDebugApi.SetCreativeMode(owner, false);
                }
                if (creativeOff == null || !creativeOff.Success)
                    throw new InvalidOperationException("Creative disable failed: " + (creativeOff == null ? "no result" : FirstNonEmpty(creativeOff.FailureReason, creativeOff.Message)));
                required.Add("creative{hooks=" + creativeOn.After.RuntimeHooksInstalled + ", generatorAvailable=" + creativeOn.After.GeneratorRuntimeAvailable + ", smoke=" + creativeSmoke + "}");

                DebugCommandResult unlock = advancedDebugApi.UnlockAllTechTrees(owner);
                optional.Add("unlockTech{success=" + unlock.Success + ", affected=" + unlock.AffectedCount + ", reason=" + FirstNonEmpty(unlock.FailureReason, "none") + "}");

                CropMaturityResult crops = advancedDebugApi.MatureAllCrops(owner);
                optional.Add("crops{success=" + crops.Success + ", matured=" + crops.CropsMatured + "/" + crops.PlantBasinsVisited + ", reason=" + FirstNonEmpty(crops.FailureReason, "none") + "}");

                InventoryGiveResult generator = advancedDebugApi.GiveCreativeGenerator(owner);
                optional.Add(
                    "generator{success=" + generator.Success +
                    ", id=" + FirstNonEmpty(generator.ItemId, "dtmapi_creative_generator") +
                    ", before=" + generator.BeforeCount +
                    ", after=" + generator.AfterCount +
                    ", reason=" + FirstNonEmpty(generator.FailureReason, "none") +
                    "}");

                SpawnDebugResult monsterOne = advancedDebugApi.SpawnMonster(owner, monsterOption.Id, 1);
                if (!monsterOne.Success || monsterOne.SpawnedCount != 1)
                    throw new InvalidOperationException("Monster 1-batch failed: " + monsterOne.FailureReason + ": " + monsterOne.Message);
                SpawnDebugResult monsterTen = advancedDebugApi.SpawnMonster(owner, monsterOption.Id, 10);
                if (!monsterTen.Success || monsterTen.SpawnedCount != 10)
                    throw new InvalidOperationException("Monster 10-batch failed: " + monsterTen.FailureReason + ": " + monsterTen.Message);
                if (monsterOption.Id.Equals("space_ship", StringComparison.OrdinalIgnoreCase) &&
                    (monsterOne.Message.IndexOf("requestedRoots=1", StringComparison.Ordinal) < 0 ||
                     monsterOne.Message.IndexOf("succeededRoots=1", StringComparison.Ordinal) < 0 ||
                     monsterOne.Message.IndexOf("addedEntities=3", StringComparison.Ordinal) < 0 ||
                     monsterTen.Message.IndexOf("requestedRoots=10", StringComparison.Ordinal) < 0 ||
                     monsterTen.Message.IndexOf("succeededRoots=10", StringComparison.Ordinal) < 0 ||
                     monsterTen.Message.IndexOf("addedEntities=30", StringComparison.Ordinal) < 0))
                {
                    throw new InvalidOperationException(
                        "Old City Guardian composite counts were not observable in the ProductNative results. one=" +
                        monsterOne.Message + "; ten=" + monsterTen.Message);
                }
                required.Add(
                    "monster{success=True, id=" + monsterOption.Id +
                    ", batches=" + monsterOne.SpawnedCount + "+" +
                    monsterTen.SpawnedCount +
                    ", one=" + monsterOne.Message +
                    ", ten=" + monsterTen.Message + "}");

                SpawnDebugResult resource = advancedDebugApi.SpawnResource(owner, resourceOption.Id, 1);
                if (!resource.Success)
                    throw new InvalidOperationException("Resource spawn failed: " + resource.FailureReason + ": " + resource.Message);
                required.Add("resource{success=True, id=" + resourceOption.Id + ", count=" + resource.SpawnedCount + "}");

                string summary = "required=" +
                    string.Join("; ", required.ToArray()) +
                    "; optional=" +
                    string.Join("; ", optional.ToArray()) +
                    "; nativeOwner=" +
                    DebugConsoleActions.OwnerKind;
                DebugConsoleActions.ValidateOwnerBoundaryAfterAction();
                runtime.RuntimeMonitor.Log("Smoke exercise AdvancedDebug OK " + summary);
                runtime.SetHookStatus("Smoke.AdvancedDebug", "verified", "IAdvancedDebugApi whitelist", summary);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke advanced debug exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.AdvancedDebug", "failed", "IAdvancedDebugApi whitelist", ex.GetType().Name + ": " + ex.Message);
            }
        }

        private string VerifyAdvancedCreativeHooksForFixture()
        {
            CreativeModeState state = advancedDebugApi.GetCreativeModeState();
            if (!state.Enabled)
                throw new InvalidOperationException("Creative mode is not enabled.");
            if (!state.RuntimeHooksInstalled)
                throw new InvalidOperationException("Creative no-cost/no-time hooks are not installed.");

            patcher ??= new HarmonyReflectionPatcher(runtime);
            Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
            object? gameManager = ReadStaticMember(dolocApi, "gameManager");
            object? config = gameManager == null ? null : ReadMember(gameManager, "gameInitConfig");
            if (dolocApi == null || config == null)
                throw new InvalidOperationException("Creative native DolocAPI/GameInitConfig state is unavailable.");

            bool ignoreMaterialCost = ReadBoolMember(config, "ignoreMaterialCost", false);
            bool skipMoneyVerifyInShop = ReadBoolMember(config, "skipMoneyVerifyInShop", false);
            bool ignoreSpiritCost = ReadBoolMember(config, "ignoreSpiritCost", false);
            if (!ignoreMaterialCost || !skipMoneyVerifyInShop || !ignoreSpiritCost)
                throw new InvalidOperationException("Creative native GameInitConfig flags are not active. ignoreMaterialCost=" + ignoreMaterialCost + ", skipMoneyVerifyInShop=" + skipMoneyVerifyInShop + ", ignoreSpiritCost=" + ignoreSpiritCost + ".");

            MethodInfo? canAffordMoney = dolocApi.GetMethod("CanAffordMoney", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(int) }, null);
            object? canAffordResult = canAffordMoney?.Invoke(null, new object?[] { int.MaxValue });
            if (!(canAffordResult is bool canAfford) || !canAfford)
                throw new InvalidOperationException("Creative CanAffordMoney(int.MaxValue) did not return true.");

            int? energyBefore = ReadAgentEnergyForFixture(dolocApi);
            MethodInfo? costEnergy = dolocApi.GetMethod("CostEnergy", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(int) }, null);
            object? costEnergyResult = costEnergy?.Invoke(null, new object?[] { 1 });
            int? energyAfter = ReadAgentEnergyForFixture(dolocApi);
            if (!(costEnergyResult is bool costOk) || !costOk)
                throw new InvalidOperationException("Creative CostEnergy(1) did not return true.");
            if (energyBefore.HasValue && energyAfter.HasValue && energyBefore.Value != energyAfter.Value)
                throw new InvalidOperationException("Creative CostEnergy(1) changed energy " + energyBefore.Value + "->" + energyAfter.Value + ".");

            return "nativeFlags{ignoreMaterialCost=" + ignoreMaterialCost + ",skipMoneyVerifyInShop=" + skipMoneyVerifyInShop + ",ignoreSpiritCost=" + ignoreSpiritCost + "}, canAffordMoneyIntMax=True, costEnergyNoChange=" + (energyBefore.HasValue && energyAfter.HasValue ? (energyBefore.Value == energyAfter.Value).ToString() : "unknown") + ", noTimeHookInstalled=" + state.RuntimeHooksInstalled + ", generatorAvailable=" + state.GeneratorRuntimeAvailable;
        }

        private static int? ReadAgentEnergyForFixture(Type dolocApi)
        {
            try
            {
                object? archive = ReadStaticMember(dolocApi, "archiveHandle");
                object? farmData = archive == null ? null : ReadMember(archive, "farmData");
                object? agentData = farmData == null ? null : ReadMember(farmData, "agentData");
                object? value = agentData == null ? null : ReadMember(agentData, "energy");
                return value == null ? (int?)null : Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return null;
            }
        }

        private FixtureAttemptResult TryExerciseDebugTeleportForFixture()
        {
            try
            {
                TeleportSnapshot before = teleportDebugApi.GetCurrentSnapshot();
                IReadOnlyList<TeleportDestination> destinations = teleportDebugApi.GetDestinations();
                ManifestModel smokeManifest = CreateFixtureManifest();
                TeleportDestination? destination = destinations
                    .Where(d => !string.IsNullOrWhiteSpace(d.MarkPointId))
                    .Where(d => string.IsNullOrWhiteSpace(before.RoomId) || !d.RoomId.Equals(before.RoomId, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(d => d.IsStation)
                    .ThenBy(d => d.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault()
                    ?? destinations.FirstOrDefault(d => !string.IsNullOrWhiteSpace(d.MarkPointId));
                if (destination == null)
                    throw new InvalidOperationException("No whitelisted teleport destination was available.");

                debugTeleportBeforeSnapshot = before;
                debugTeleportDestination = destination;
                TeleportResult result = teleportDebugApi.Teleport(smokeManifest, destination.Id);
                debugTeleportRequestResult = result;
                if (!result.Success)
                    throw new InvalidOperationException(result.FailureReason + ": " + result.Message);

                debugTeleportRequestedAt = DateTimeOffset.Now;
                string summary = "destination=" + FirstNonEmpty(destination.DisplayName, destination.Id) +
                    ", markPoint=" + destination.MarkPointId +
                    ", from={" + FormatTeleportSnapshot(before) + "}" +
                    ", requestAfter={" + FormatTeleportSnapshot(result.AfterRequest) + "}" +
                    ", nativeOwner=" + DebugConsoleActions.OwnerKind;
                DebugConsoleActions.ValidateOwnerBoundaryAfterAction();
                runtime.RuntimeMonitor.Log("Smoke exercise DebugTeleport request OK " + summary);
                runtime.SetHookStatus("Smoke.DebugTeleport", "pending", "ITeleportDebugApi -> DolocAPI.DoTransport", summary);
                return FixtureAttemptResult.Pending;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke debug teleport exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.DebugTeleport", "failed", "ITeleportDebugApi", ex.GetType().Name + ": " + ex.Message);
                return FixtureAttemptResult.Failed;
            }
        }

        private void CompleteDebugTeleportForFixture()
        {
            try
            {
                TeleportSnapshot before = debugTeleportBeforeSnapshot ?? debugTeleportRequestResult?.Before ?? new TeleportSnapshot();
                TeleportSnapshot after = teleportDebugApi.GetCurrentSnapshot();
                bool changedRoom = !string.IsNullOrWhiteSpace(before.RoomId) && !before.RoomId.Equals(after.RoomId, StringComparison.OrdinalIgnoreCase);
                double distance = DistanceBetween(before, after);
                bool moved = changedRoom || (!double.IsNaN(distance) && distance > 1);
                string summary = "destination=" + FirstNonEmpty(debugTeleportDestination?.DisplayName ?? string.Empty, debugTeleportDestination?.Id ?? string.Empty, debugTeleportRequestResult?.DestinationId ?? string.Empty) +
                    ", markPoint=" + FirstNonEmpty(debugTeleportDestination?.MarkPointId ?? string.Empty, debugTeleportRequestResult?.MarkPointId ?? string.Empty) +
                    ", before={" + FormatTeleportSnapshot(before) + "}" +
                    ", after={" + FormatTeleportSnapshot(after) + "}" +
                    ", changedRoom=" + changedRoom +
                    ", distance=" + FormatSmokeDouble(distance);

                if (!moved)
                    throw new InvalidOperationException("Teleport request did not change the observed room/position within the smoke verification window. " + summary);

                runtime.RuntimeMonitor.Log("Smoke exercise DebugTeleport OK " + summary);
                runtime.SetHookStatus("Smoke.DebugTeleport", "verified", "ITeleportDebugApi -> DolocAPI.DoTransport", summary);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke debug teleport completion failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.DebugTeleport", "failed", "ITeleportDebugApi -> DolocAPI.DoTransport", ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static ManifestModel CreateDebugConsoleSmokeManifest()
        {
            return new ManifestModel
            {
                Name = "Y-Key Console",
                Author = "DTMAPI",
                Version = DtmApiRuntime.ApiVersion,
                UniqueID = "DTMAPI.DebugConsoleMod",
                Type = "Smoke"
            };
        }

        private static double DistanceBetween(TeleportSnapshot before, TeleportSnapshot after)
        {
            if (before == null || after == null || double.IsNaN(before.X) || double.IsNaN(before.Y) || double.IsNaN(after.X) || double.IsNaN(after.Y))
                return double.NaN;
            double dx = before.X - after.X;
            double dy = before.Y - after.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static string FormatTeleportSnapshot(TeleportSnapshot snapshot)
        {
            if (snapshot == null)
                return "unknown";
            return "roomId=" + snapshot.RoomId +
                ", roomTitle=" + snapshot.RoomTitle +
                ", roomType=" + snapshot.RoomType +
                ", position=" + FormatSmokeDouble(snapshot.X) + "," + FormatSmokeDouble(snapshot.Y) + "," + FormatSmokeDouble(snapshot.Z);
        }

        private static string FormatTimeSnapshot(TimeDebugState snapshot)
        {
            if (snapshot == null)
                return "unknown";
            return snapshot.Year + "-" + snapshot.Month + "-" + snapshot.Day + " " + snapshot.Hour.ToString("00") + ":" + snapshot.Minute.ToString("00") +
                ", weather=" + FirstNonEmpty(snapshot.CurrentWeatherName, snapshot.CurrentWeatherId) +
                ", period=" + snapshot.Period;
        }

        private static string FormatSmokeDouble(double value)
        {
            return double.IsNaN(value) ? "unknown" : value.ToString("0.###");
        }

        private static InstantSaveSnapshot CaptureInstantSaveSnapshot(Type dolocApi)
        {
            object? archive = ReadStaticMember(dolocApi, "archiveHandle");
            object? room = ReadStaticMember(dolocApi, "CurrentRoom");
            object? position = ReadStaticMember(dolocApi, "AgentPosition");
            object? timeData = archive == null ? null : ReadMember(archive, "timeData");
            object? dateNow = timeData == null ? null : ReadMember(timeData, "dateNow");
            int archiveIndex = archive == null ? -1 : ReadIntMember(archive, "archiveIndex", -1);

            return new InstantSaveSnapshot
            {
                ArchiveIndex = archiveIndex >= 0 ? archiveIndex : null,
                RoomId = room == null ? "unknown" : FirstNonEmpty(ReadStringMember(room, "RoomId", string.Empty), ReadStringMember(room, "roomId", string.Empty), room.GetType().Name),
                RoomTitle = room == null ? string.Empty : ReadStringMember(room, "Title", string.Empty),
                RoomType = room == null ? "unknown" : (ReadMember(room, "Type")?.ToString() ?? room.GetType().Name),
                X = ReadDoubleMember(position, "x", double.NaN),
                Y = ReadDoubleMember(position, "y", double.NaN),
                Z = ReadDoubleMember(position, "z", double.NaN),
                TimeText = dateNow?.ToString() ?? string.Empty
            };
        }

        private sealed class InstantSaveSnapshot
        {
            public int? ArchiveIndex { get; set; }
            public string RoomId { get; set; } = "unknown";
            public string RoomTitle { get; set; } = string.Empty;
            public string RoomType { get; set; } = "unknown";
            public double X { get; set; } = double.NaN;
            public double Y { get; set; } = double.NaN;
            public double Z { get; set; } = double.NaN;
            public string TimeText { get; set; } = string.Empty;

            public double DistanceTo(InstantSaveSnapshot other)
            {
                if (other == null || double.IsNaN(X) || double.IsNaN(Y) || double.IsNaN(other.X) || double.IsNaN(other.Y))
                    return double.NaN;
                double dx = X - other.X;
                double dy = Y - other.Y;
                return Math.Sqrt(dx * dx + dy * dy);
            }

            public string ToLogString()
            {
                return "archiveIndex=" + (ArchiveIndex?.ToString() ?? "unknown") +
                    ", roomId=" + RoomId +
                    ", roomTitle=" + RoomTitle +
                    ", roomType=" + RoomType +
                    ", position=" + FormatDouble(X) + "," + FormatDouble(Y) + "," + FormatDouble(Z) +
                    ", time=" + TimeText;
            }

            private static string FormatDouble(double value)
            {
                return double.IsNaN(value) ? "unknown" : value.ToString("0.###");
            }
        }
    }
}
