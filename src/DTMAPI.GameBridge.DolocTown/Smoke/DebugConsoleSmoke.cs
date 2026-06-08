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
    public sealed partial class DolocTownGameBridge
    {
        private void TryExerciseInstantSaveForSmoke()
        {
            try
            {
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
            finally
            {
                if (smokeSettings?.AutoExitAfterSaveLoaded == true && !autoExitAttempted)
                {
                    if (IsDebugSmokeRequested())
                    {
                        TryQuitAfterDebugSmoke();
                    }
                    else
                    {
                        autoExitAttempted = true;
                        TryQuitApplication("smoke instant-save evidence captured");
                    }
                }
            }
        }

        private void TryExerciseDebugInventoryForSmoke()
        {
            try
            {
                if (experimentalApi == null)
                    throw new InvalidOperationException("Experimental bridge API is not available.");

                InventoryDebugItem? item = SelectInventorySmokeItem(experimentalApi);
                if (item == null)
                    throw new InvalidOperationException("No spawnable official item was found in TbItem.");

                InventoryGiveResult result = experimentalApi.GiveItem(CreateSmokeManifest(), item.Id, 1);
                if (!result.Success)
                    throw new InvalidOperationException(result.FailureReason + ": " + result.Message);

                string modSummary = "modItem=not-found";
                InventoryDebugItem? modItem = SelectModInventorySmokeItem(experimentalApi);
                if (modItem != null)
                {
                    InventoryGiveResult modResult = experimentalApi.GiveItem(CreateSmokeManifest(), modItem.Id, 1);
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
                    ", " + modSummary;
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

        private void TryExerciseDebugWeatherForSmoke()
        {
            try
            {
                if (experimentalApi == null)
                    throw new InvalidOperationException("Experimental bridge API is not available.");

                WeatherDebugState state = experimentalApi.GetState();
                IReadOnlyList<WeatherDebugOption> options = experimentalApi.GetAvailableWeathers();
                WeatherDebugOption? option = options.FirstOrDefault(w => !w.IsCurrent)
                    ?? options.FirstOrDefault();
                if (option == null)
                    throw new InvalidOperationException("No native weather options were available.");

                WeatherSetResult result = experimentalApi.SetWeather(CreateSmokeManifest(), option.Id, patchCurrentPeriod: true);
                if (!result.Success)
                    throw new InvalidOperationException(result.FailureReason + ": " + result.Message);

                string summary = "options=" + options.Count +
                    ", season=" + state.SeasonName +
                    ", date=" + state.Year + "-" + state.Month + "-" + state.Day + " " + state.Hour +
                    ", before=" + result.BeforeWeatherId +
                    ", after=" + result.AfterWeatherId +
                    ", display=" + FirstNonEmpty(result.DisplayName, option.DisplayName, option.Id) +
                    ", currentDayForecast=" + option.IsCurrentDayForecast;
                runtime.RuntimeMonitor.Log("Smoke exercise DebugWeather OK " + summary);
                runtime.SetHookStatus("Smoke.DebugWeather", "verified", "IWeatherDebugApi -> ArchiveDataHandle.SetWeather/PatchWeather", summary);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke debug weather exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.DebugWeather", "failed", "IWeatherDebugApi", ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void TryExerciseDebugTimeForSmoke()
        {
            try
            {
                if (experimentalApi == null)
                    throw new InvalidOperationException("Experimental bridge API is not available.");

                var steps = new List<string>();
                for (int i = 0; i < 3; i++)
                {
                    TimeSkipResult result = experimentalApi.SkipToNextWeatherPeriod(CreateSmokeManifest());
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

                string summary = "transitions=" + string.Join(" -> ", steps.ToArray());
                runtime.RuntimeMonitor.Log("Smoke exercise DebugTime OK " + summary);
                runtime.SetHookStatus("Smoke.DebugTime", "verified", "ITimeDebugApi -> ArchiveDataHandle.PassTimeNoControl", summary);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke debug time exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.DebugTime", "failed", "ITimeDebugApi", ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void TryExerciseDebugMovementForSmoke()
        {
            try
            {
                if (experimentalApi == null)
                    throw new InvalidOperationException("Experimental bridge API is not available.");

                double[] levels = { 1, 2, 3, 4 };
                var samples = new List<string>();
                foreach (double level in levels)
                {
                    MovementSpeedResult result = experimentalApi.SetSpeedMultiplier(CreateSmokeManifest(), level);
                    if (!result.Success)
                        throw new InvalidOperationException(result.FailureReason + ": " + result.Message);
                    samples.Add(level.ToString("0.#") + "x:" + FormatSmokeDouble(result.After.MoveSpeed));
                }
                MovementSpeedResult reset = experimentalApi.ResetSpeed(CreateSmokeManifest(), "smoke-restore");
                if (!reset.Success || !reset.After.IsDefault)
                    throw new InvalidOperationException("Reset failed: " + reset.FailureReason + " " + reset.Message);

                string summary = "levels=" + string.Join(",", samples.ToArray()) + ", restored=" + reset.After.IsDefault + ", finalSpeed=" + FormatSmokeDouble(reset.After.MoveSpeed);
                runtime.RuntimeMonitor.Log("Smoke exercise DebugMovement OK " + summary);
                runtime.SetHookStatus("Smoke.DebugMovement", "verified", "IMovementDebugApi -> MotionAbility.SetMoveScaler", summary);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke debug movement exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.DebugMovement", "failed", "IMovementDebugApi", ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void TryExerciseAdvancedDebugForSmoke()
        {
            try
            {
                if (experimentalApi == null)
                    throw new InvalidOperationException("Experimental bridge API is not available.");

                ManifestModel owner = CreateDebugConsoleSmokeManifest();
                var required = new List<string>();
                var optional = new List<string>();

                TimeSkipResult day = experimentalApi.AdvanceTime(owner, AdvancedTimeAdvanceKind.Day, 1);
                if (!day.Success)
                    throw new InvalidOperationException("Advance day failed: " + day.FailureReason + ": " + day.Message);
                required.Add("day{seconds=" + day.AdvancedSeconds + ", before=" + FormatTimeSnapshot(day.Before) + ", after=" + FormatTimeSnapshot(day.After) + "}");

                TimeScaleDebugResult scale = experimentalApi.SetTimeScale(owner, 4);
                if (!scale.Success)
                    throw new InvalidOperationException("Set time scale failed: " + scale.FailureReason + ": " + scale.Message);
                TimeScaleDebugResult resetScale = experimentalApi.ResetTimeScale(owner, "advanced-smoke");
                if (!resetScale.Success)
                    throw new InvalidOperationException("Reset time scale failed: " + resetScale.FailureReason + ": " + resetScale.Message);
                required.Add("scale{set=" + FormatSmokeDouble(scale.AfterMultiplier) + ", reset=" + FormatSmokeDouble(resetScale.AfterMultiplier) + "}");

                DebugValueResult money = experimentalApi.AddMoney(owner, 1);
                if (!money.Success)
                    throw new InvalidOperationException("Add money failed: " + money.FailureReason + ": " + money.Message);
                required.Add("money{" + money.BeforeValue + "->" + money.AfterValue + "}");

                TechPointDebugOption? techOption = experimentalApi.GetTechPointOptions().FirstOrDefault();
                if (techOption == null)
                    throw new InvalidOperationException("No tech point options were available.");
                DebugValueResult tech = experimentalApi.AddTechPoint(owner, techOption.Id, 1);
                if (!tech.Success)
                    throw new InvalidOperationException("Add tech point failed: " + tech.FailureReason + ": " + tech.Message);
                required.Add("tech{" + techOption.Id + ":" + tech.BeforeValue + "->" + tech.AfterValue + "}");

                CreativeModeResult creativeOn = experimentalApi.SetCreativeMode(owner, true);
                CreativeModeResult? creativeOff = null;
                string creativeSmoke;
                try
                {
                    if (!creativeOn.Success || !creativeOn.After.RuntimeHooksInstalled)
                        throw new InvalidOperationException("Creative enable failed: " + FirstNonEmpty(creativeOn.FailureReason, creativeOn.Message));
                    creativeSmoke = experimentalApi.VerifyAdvancedCreativeHooksForSmoke();
                }
                finally
                {
                    creativeOff = experimentalApi.SetCreativeMode(owner, false);
                }
                if (creativeOff == null || !creativeOff.Success)
                    throw new InvalidOperationException("Creative disable failed: " + (creativeOff == null ? "no result" : FirstNonEmpty(creativeOff.FailureReason, creativeOff.Message)));
                required.Add("creative{hooks=" + creativeOn.After.RuntimeHooksInstalled + ", generatorAvailable=" + creativeOn.After.GeneratorRuntimeAvailable + ", smoke=" + creativeSmoke + "}");

                DebugCommandResult unlock = experimentalApi.UnlockAllTechTrees(owner);
                optional.Add("unlockTech{success=" + unlock.Success + ", affected=" + unlock.AffectedCount + ", reason=" + FirstNonEmpty(unlock.FailureReason, "none") + "}");

                CropMaturityResult crops = experimentalApi.MatureAllCrops(owner);
                optional.Add("crops{success=" + crops.Success + ", matured=" + crops.CropsMatured + "/" + crops.PlantBasinsVisited + ", reason=" + FirstNonEmpty(crops.FailureReason, "none") + "}");

                InventoryGiveResult generator = experimentalApi.GiveCreativeGenerator(owner);
                if (!generator.Success)
                    throw new InvalidOperationException("Creative generator give failed: " + generator.FailureReason + ": " + generator.Message);
                required.Add("generator{success=True, id=" + FirstNonEmpty(generator.ItemId, "dtmapi_creative_generator") + ", before=" + generator.BeforeCount + ", after=" + generator.AfterCount + "}");

                SpawnDebugOption? monsterOption = experimentalApi.GetMonsterOptions().FirstOrDefault(o => o.IsAvailableInCurrentRoom);
                if (monsterOption == null)
                    throw new InvalidOperationException("No monster option was available in the current room.");
                else
                {
                    SpawnDebugResult monster = experimentalApi.SpawnMonster(owner, monsterOption.Id, 1);
                    if (!monster.Success)
                        throw new InvalidOperationException("Monster spawn failed: " + monster.FailureReason + ": " + monster.Message);
                    required.Add("monster{success=True, id=" + monsterOption.Id + ", count=" + monster.SpawnedCount + "}");
                }

                SpawnDebugOption? resourceOption = experimentalApi.GetResourceOptions().FirstOrDefault(o => o.IsAvailableInCurrentRoom);
                if (resourceOption == null)
                    throw new InvalidOperationException("No resource option was available in the current room.");
                else
                {
                    SpawnDebugResult resource = experimentalApi.SpawnResource(owner, resourceOption.Id, 1);
                    if (!resource.Success)
                        throw new InvalidOperationException("Resource spawn failed: " + resource.FailureReason + ": " + resource.Message);
                    required.Add("resource{success=True, id=" + resourceOption.Id + ", count=" + resource.SpawnedCount + "}");
                }

                string summary = "required=" + string.Join("; ", required.ToArray()) + "; optional=" + string.Join("; ", optional.ToArray());
                runtime.RuntimeMonitor.Log("Smoke exercise AdvancedDebug OK " + summary);
                runtime.SetHookStatus("Smoke.AdvancedDebug", "verified", "IAdvancedDebugApi whitelist", summary);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke advanced debug exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.AdvancedDebug", "failed", "IAdvancedDebugApi whitelist", ex.GetType().Name + ": " + ex.Message);
            }
        }

        private SmokeAttemptResult TryExerciseDebugTeleportForSmoke()
        {
            try
            {
                if (experimentalApi == null)
                    throw new InvalidOperationException("Experimental bridge API is not available.");

                TeleportSnapshot before = experimentalApi.GetCurrentSnapshot();
                IReadOnlyList<TeleportDestination> destinations = experimentalApi.GetDestinations();
                ManifestModel smokeManifest = CreateSmokeManifest();
                TeleportCsvExportResult csvExport = experimentalApi.ExportDestinationsCsv(smokeManifest);
                if (!csvExport.Success)
                    throw new InvalidOperationException("Teleport CSV export failed: " + csvExport.FailureReason + ": " + csvExport.Message);
                runtime.RuntimeMonitor.Log("Smoke exercise DebugTeleportCsv OK rows=" + csvExport.RowCount + " path=" + csvExport.Path);
                runtime.SetHookStatus("Smoke.DebugTeleportCsv", "verified", "ITeleportDebugApi.ExportDestinationsCsv", "rows=" + csvExport.RowCount + " path=" + csvExport.Path);

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
                TeleportResult result = experimentalApi.Teleport(smokeManifest, destination.Id);
                debugTeleportRequestResult = result;
                if (!result.Success)
                    throw new InvalidOperationException(result.FailureReason + ": " + result.Message);

                debugTeleportRequestedAt = DateTimeOffset.Now;
                string summary = "destination=" + FirstNonEmpty(destination.DisplayName, destination.Id) +
                    ", markPoint=" + destination.MarkPointId +
                    ", from={" + FormatTeleportSnapshot(before) + "}" +
                    ", requestAfter={" + FormatTeleportSnapshot(result.AfterRequest) + "}";
                runtime.RuntimeMonitor.Log("Smoke exercise DebugTeleport request OK " + summary);
                runtime.SetHookStatus("Smoke.DebugTeleport", "pending", "ITeleportDebugApi -> DolocAPI.DoTransport", summary);
                return SmokeAttemptResult.Pending;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke debug teleport exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.DebugTeleport", "failed", "ITeleportDebugApi", ex.GetType().Name + ": " + ex.Message);
                return SmokeAttemptResult.Failed;
            }
        }

        private void CompleteDebugTeleportForSmoke()
        {
            try
            {
                if (experimentalApi == null)
                    throw new InvalidOperationException("Experimental bridge API is not available.");

                TeleportSnapshot before = debugTeleportBeforeSnapshot ?? debugTeleportRequestResult?.Before ?? new TeleportSnapshot();
                TeleportSnapshot after = experimentalApi.GetCurrentSnapshot();
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

        private void TryQuitAfterDebugSmoke()
        {
            if (smokeSettings == null || !smokeSettings.Enabled || !smokeSettings.AutoExitAfterSaveLoaded || autoExitAttempted)
                return;
            if (!IsDebugSmokeRequested())
                return;
            if (smokeSettings.AutoExerciseDebugConsole && !autoExerciseDebugConsoleAttempted)
                return;
            if (smokeSettings.AutoExerciseDebugInventory && !autoExerciseDebugInventoryAttempted)
                return;
            if (smokeSettings.AutoExerciseDebugWeather && !autoExerciseDebugWeatherAttempted)
                return;
            if (smokeSettings.AutoExerciseDebugTime && !autoExerciseDebugTimeAttempted)
                return;
            if (smokeSettings.AutoExerciseDebugMovement && !autoExerciseDebugMovementAttempted)
                return;
            if (smokeSettings.AutoExerciseAdvancedDebug && !autoExerciseAdvancedDebugAttempted)
                return;
            if (smokeSettings.AutoExerciseInstantSave && !autoExerciseInstantSaveAttempted)
                return;
            if (smokeSettings.AutoExerciseVehicle && !autoExerciseVehicleAttempted)
                return;
            if (smokeSettings.AutoExerciseNewContentApis && !autoExerciseNewContentApisAttempted)
                return;
            if (smokeSettings.AutoExerciseMineContentApis && !autoExerciseMineContentApisAttempted)
                return;
            if (smokeSettings.AutoExerciseZoom && !autoExerciseZoomAttempted)
                return;
            if (smokeSettings.AutoExerciseChestLocatorEnhancer && !autoExerciseChestLocatorEnhancerAttempted)
                return;
            if (smokeSettings.AutoExerciseStrongPlantingGun && !autoExerciseStrongPlantingGunAttempted)
                return;
            if (smokeSettings.AutoExerciseCustomEntityApis && !autoExerciseCustomEntityApisAttempted)
                return;
            if (smokeSettings.AutoExerciseDebugTeleport && !debugTeleportVerificationCompleted)
                return;

            autoExitAttempted = true;
            TryQuitApplication("smoke debug console/API evidence captured");
        }

        private void TryExerciseDebugConsoleHotkeyForSmoke()
        {
            try
            {
                if (debugConsoleApi == null)
                    throw new InvalidOperationException("Debug console host API was not available to the GameBridge smoke runner.");

                ManifestModel owner = CreateDebugConsoleSmokeManifest();
                if (debugConsoleApi.IsOpen)
                    debugConsoleApi.Close(owner, "smoke-reset");

                int openCount = 0;
                int escapeCloseCount = 0;
                int yCloseCount = 0;

                DispatchSmokeInput("Y");
                if (!debugConsoleApi.IsOpen)
                    throw new InvalidOperationException("Y did not open the debug console through the DTMAPI input event path.");
                openCount++;

                debugConsoleApi.Close(owner, "Escape");
                if (debugConsoleApi.IsOpen)
                    throw new InvalidOperationException("Escape did not close the debug console host state.");
                escapeCloseCount++;

                DispatchSmokeInput("Y");
                if (!debugConsoleApi.IsOpen)
                    throw new InvalidOperationException("Second Y did not reopen the debug console through the DTMAPI input event path.");
                openCount++;

                debugConsoleApi.Close(owner, "Y");
                if (debugConsoleApi.IsOpen)
                    throw new InvalidOperationException("Y close did not close the debug console host state.");
                yCloseCount++;

                for (int tap = 1; tap <= 10; tap++)
                {
                    if (debugConsoleApi.IsOpen)
                    {
                        debugConsoleApi.Close(owner, "Y");
                        if (debugConsoleApi.IsOpen)
                            throw new InvalidOperationException("Short Y tap " + tap + " did not close the console.");
                        yCloseCount++;
                    }
                    else
                    {
                        DispatchSmokeInput("Y");
                        if (!debugConsoleApi.IsOpen)
                            throw new InvalidOperationException("Short Y tap " + tap + " did not open the console.");
                        openCount++;
                    }
                }

                int openBeforeHold = openCount;
                int yCloseBeforeHold = yCloseCount;
                DispatchSmokeInput("Y");
                if (!debugConsoleApi.IsOpen)
                    throw new InvalidOperationException("Held-Y smoke did not open the console before the no-flicker check.");
                openCount++;

                runtime.RecordInputPressed("Y");
                runtime.RecordInputReleased("Y");
                bool holdNoFlicker = debugConsoleApi.IsOpen && openCount == openBeforeHold + 1 && yCloseCount == yCloseBeforeHold;
                if (!holdNoFlicker)
                    throw new InvalidOperationException("Held-Y smoke changed the expected open/close counts.");

                bool keepOpenForMouseGive = smokeSettings?.AutoExerciseDebugConsoleMouseGive == true;
                if (!keepOpenForMouseGive && debugConsoleApi.IsOpen)
                    debugConsoleApi.Close(owner, "smoke-cleanup");

                string summary = "openCount=" + openCount +
                    ", escapeCloseCount=" + escapeCloseCount +
                    ", yCloseCount=" + yCloseCount +
                    ", shortTaps=10" +
                    ", holdNoFlicker=" + holdNoFlicker +
                    ", keepOpenForMouseGive=" + keepOpenForMouseGive;
                runtime.RuntimeMonitor.Log("Smoke exercise DebugConsoleHotkey OK " + summary);
                runtime.SetHookStatus("Smoke.DebugConsoleHotkey", "verified", "DtmApiRuntime.RecordInputPressed + IDebugConsoleApi", summary);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke debug console hotkey exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.DebugConsoleHotkey", "failed", "DtmApiRuntime.RecordInputPressed + IDebugConsoleApi", ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void DispatchSmokeInput(string button)
        {
            runtime.RecordInputPressed(button);
            runtime.RecordInputReleased(button);
        }

        private bool IsDebugSmokeRequested()
        {
            return smokeSettings != null && (smokeSettings.AutoExerciseDebugConsole || smokeSettings.AutoExerciseDebugInventory || smokeSettings.AutoExerciseDebugWeather || smokeSettings.AutoExerciseDebugTeleport || smokeSettings.AutoExerciseDebugTime || smokeSettings.AutoExerciseDebugMovement || smokeSettings.AutoExerciseAdvancedDebug || smokeSettings.AutoExerciseVehicle || smokeSettings.AutoExerciseNewContentApis || smokeSettings.AutoExerciseMineContentApis || smokeSettings.AutoExerciseZoom || smokeSettings.AutoExerciseChestLocatorEnhancer || smokeSettings.AutoExerciseStrongPlantingGun || smokeSettings.AutoExerciseCustomEntityApis);
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
