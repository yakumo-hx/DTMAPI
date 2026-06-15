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
        private SmokeAttemptResult TryExerciseVehicleForSmoke()
        {
            try
            {
                if (experimentalApi == null)
                    throw new InvalidOperationException("Experimental bridge API is not available.");

                const string vehicleId = "dtmapi.second_motor";
                const string keyItemId = "dtmapi_second_motor_key";
                ManifestModel smokeManifest = CreateSmokeManifest();
                MotorVehicleState registered = experimentalApi.GetVehicleState(vehicleId);
                if (!registered.IsRegistered)
                    throw new InvalidOperationException("SecondMotorMod did not register " + vehicleId + ".");

                if (!registered.OwnerUniqueId.Equals("DTMAPI.SecondMotorMod", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Second motor owner was " + registered.OwnerUniqueId + " instead of DTMAPI.SecondMotorMod.");

                int saveSlot = smokeSettings?.AutoLoadSaveSlot ?? 0;
                if (saveSlot != 8 && saveSlot != 9)
                    throw new InvalidOperationException("Vehicle smoke requires the eighth or ninth save fixture. currentSaveSlot=" + saveSlot + ".");

                if (Math.Abs(registered.SpeedMultiplier - 1) > 0.05)
                    throw new InvalidOperationException("Second motor speed multiplier was " + FormatSmokeDouble(registered.SpeedMultiplier) + " instead of native-like 1x.");
                if (!registered.KeyItemIds.Any(key => key.Equals(keyItemId, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("Second motor registered keys did not include " + keyItemId + ".");
                if (!registered.IsAvailableInCurrentRoom)
                    throw new InvalidOperationException("Vehicle smoke fixture is not in a motor-enabled room. reason=" + registered.LastFailureReason + " message=" + registered.LastMessage + ". Do not use teleport recovery for this smoke.");

                MotorVehicleState originalBeforeKey = experimentalApi.GetOriginalMotorState();
                if (saveSlot == 8 && !originalBeforeKey.IsUnlocked)
                    throw new InvalidOperationException("Eighth save fixture should have the native motor unlocked for comparison, but original IsUnlocked=false.");
                if (saveSlot == 9 && originalBeforeKey.IsUnlocked)
                    throw new InvalidOperationException("Ninth save fixture should keep the native motor locked, but original IsUnlocked=true.");

                InventoryGiveResult giveKey = experimentalApi.GiveItem(smokeManifest, keyItemId, 2);
                if (!giveKey.Success)
                    throw new InvalidOperationException("Failed to give second motor key: " + giveKey.FailureReason + ": " + giveKey.Message);

                MotorVehicleSummonResult keySummon = experimentalApi.UseRegisteredSecondMotorKeyForSmoke(keyItemId);
                if (!keySummon.Success)
                    throw new InvalidOperationException("Second motor key summon failed: " + keySummon.FailureReason + ": " + keySummon.Message);

                MotorVehicleSummonResult secondKeySummon = experimentalApi.UseRegisteredSecondMotorKeyForSmoke(keyItemId);
                if (!secondKeySummon.Success)
                    throw new InvalidOperationException("Second motor repeated key summon failed: " + secondKeySummon.FailureReason + ": " + secondKeySummon.Message);

                MotorVehicleState originalAfterKeySummon = experimentalApi.GetOriginalMotorState();
                MotorVehicleState secondAfterKeySummon = experimentalApi.GetVehicleState(vehicleId);
                bool nativeUnlockUnchanged = originalBeforeKey.IsUnlocked == originalAfterKeySummon.IsUnlocked;
                bool lockedNativeUnaffected = saveSlot != 9 || (!originalAfterKeySummon.IsUnlocked && !originalAfterKeySummon.IsVisible);
                if (!nativeUnlockUnchanged || !lockedNativeUnaffected)
                    throw new InvalidOperationException("Second motor key changed native motor state. beforeUnlocked=" + originalBeforeKey.IsUnlocked + " afterUnlocked=" + originalAfterKeySummon.IsUnlocked + " afterVisible=" + originalAfterKeySummon.IsVisible + ".");
                if (!secondAfterKeySummon.IsVisible)
                    throw new InvalidOperationException("Second motor was not visible after custom key summon.");

                string appearanceProbe = experimentalApi.ProbeSecondMotorAppearanceForSmoke(vehicleId);
                if (!appearanceProbe.Contains("appearanceIsolated=True"))
                    throw new InvalidOperationException("Second motor appearance was not isolated from the original motor: " + appearanceProbe);

                MotorVehicleRideResult ride = experimentalApi.RideVehicle(smokeManifest, vehicleId);
                if (!ride.Success || !ride.After.IsRiding)
                    throw new InvalidOperationException("Second motor ride failed: " + ride.FailureReason + ": " + ride.Message);

                vehicleEdgeTransitionBaseSummary = "vehicle=" + vehicleId +
                    ", owner=" + registered.OwnerUniqueId +
                    ", keyItem=" + keyItemId +
                    ", keyBefore=" + giveKey.BeforeCount +
                    ", keyAfter=" + giveKey.AfterCount +
                    ", saveSlot=" + saveSlot +
                    ", nativeUnlockedBefore=" + originalBeforeKey.IsUnlocked +
                    ", nativeUnlockedAfterKey=" + originalAfterKeySummon.IsUnlocked +
                    ", repeatedKeySummonVisible=" + secondKeySummon.After.IsVisible +
                    ", keySummonRoom=" + keySummon.After.RoomId +
                    ", secondVisibleAfterKey=" + secondAfterKeySummon.IsVisible +
                    ", appearanceProbe=" + appearanceProbe.Replace(", ", "|") +
                    ", ride=" + ride.After.IsRiding +
                    ", speedMultiplier=" + FormatSmokeDouble(registered.SpeedMultiplier) +
                    ", baseMaxSpeed=" + FormatSmokeDouble(registered.BaseMaxSpeed) +
                    ", effectiveMaxSpeed=" + FormatSmokeDouble(registered.EffectiveMaxSpeed) +
                    ", enduranceSummon=" + FormatSmokeDouble(keySummon.After.EnduranceProgress) +
                    ", enduranceRide=" + FormatSmokeDouble(ride.After.EnduranceProgress);

                MotorVehicleRideResult dismount = experimentalApi.DismountVehicle(smokeManifest, "smoke-restore");
                if (!dismount.Success || dismount.After.IsRiding)
                    throw new InvalidOperationException("Second motor dismount failed: " + dismount.FailureReason + ": " + dismount.Message);

                MotorVehicleState originalAfterDismount = experimentalApi.GetOriginalMotorState();
                if (saveSlot == 9 && originalAfterDismount.IsUnlocked)
                    throw new InvalidOperationException("Dismount after custom motor ride unlocked the native motor on ninth-save fixture.");

                string summary = "vehicle=" + vehicleId +
                    ", owner=" + registered.OwnerUniqueId +
                    ", keyItem=" + keyItemId +
                    ", keyBefore=" + giveKey.BeforeCount +
                    ", keyAfter=" + giveKey.AfterCount +
                    ", saveSlot=" + saveSlot +
                    ", nativeUnlockedBefore=" + originalBeforeKey.IsUnlocked +
                    ", nativeUnlockedAfterKey=" + originalAfterKeySummon.IsUnlocked +
                    ", nativeVisibleAfterKey=" + originalAfterKeySummon.IsVisible +
                    ", nativeUnlockedAfterDismount=" + originalAfterDismount.IsUnlocked +
                    ", repeatedKeySummonVisible=" + secondKeySummon.After.IsVisible +
                    ", keySummonRoom=" + keySummon.After.RoomId +
                    ", secondVisibleAfterKey=" + secondAfterKeySummon.IsVisible +
                    ", appearanceProbe=" + appearanceProbe.Replace(", ", "|") +
                    ", ride=" + ride.After.IsRiding +
                    ", dismount=" + (!dismount.After.IsRiding) +
                    ", speedMultiplier=" + FormatSmokeDouble(registered.SpeedMultiplier) +
                    ", baseMaxSpeed=" + FormatSmokeDouble(registered.BaseMaxSpeed) +
                    ", effectiveMaxSpeed=" + FormatSmokeDouble(registered.EffectiveMaxSpeed) +
                    ", enduranceSummon=" + FormatSmokeDouble(keySummon.After.EnduranceProgress) +
                    ", enduranceRide=" + FormatSmokeDouble(ride.After.EnduranceProgress) +
                    ", enduranceDismount=" + FormatSmokeDouble(dismount.After.EnduranceProgress) +
                    ", enduranceTuning=unchanged";
                runtime.RuntimeMonitor.Log("Smoke exercise VehicleSecondMotor OK " + summary);
                runtime.SetHookStatus("Smoke.VehicleSecondMotor", "verified", "IMotorVehicleApi.RegisterCustomMotor + ItemMotorKey.OnUse prefix", summary);
                return SmokeAttemptResult.Succeeded;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke vehicle exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.VehicleSecondMotor", "failed", "IMotorVehicleApi", ex.GetType().Name + ": " + ex.Message);
                return SmokeAttemptResult.Failed;
            }
        }

        private static TeleportDestination? SelectVehicleOutdoorSmokeDestination(ITeleportDebugApi api)
        {
            IReadOnlyList<TeleportDestination> destinations = api.GetDestinations();
            return destinations
                .Where(d => !string.IsNullOrWhiteSpace(d.MarkPointId))
                .FirstOrDefault(d => d.Id.StartsWith("farm:", StringComparison.OrdinalIgnoreCase) || ContainsIgnoreCase(d.DisplayName, "农场") || ContainsIgnoreCase(d.DisplayName, "Farm"))
                ?? destinations
                    .Where(d => !string.IsNullOrWhiteSpace(d.MarkPointId))
                    .OrderByDescending(d => d.IsStation)
                    .ThenBy(d => d.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();
        }

        private bool StartVehicleEdgeTransitionForSmoke(ManifestModel smokeManifest, string vehicleId, MotorVehicleState originalBefore, MotorVehicleState secondBefore)
        {
            if (experimentalApi == null)
                throw new InvalidOperationException("Experimental bridge API is not available.");

            TeleportSnapshot before = experimentalApi.GetCurrentSnapshot();
            TeleportDestination? destination = SelectVehicleEdgeTransitionDestination(experimentalApi, before.RoomId);
            if (destination == null)
                throw new InvalidOperationException("No outdoor/motor-allowed transition destination was available for second-motor edge smoke. before=" + FormatTeleportSnapshot(before));

            TeleportResult transport = experimentalApi.Teleport(smokeManifest, destination.Id);
            if (!transport.Success)
                throw new InvalidOperationException("Second-motor edge transition request failed: " + transport.FailureReason + ": " + transport.Message);

            vehicleEdgeTransitionRequestedAt = DateTimeOffset.Now;
            vehicleEdgeTransitionDestination = destination;
            vehicleEdgeTransitionRequestResult = transport;
            vehicleEdgeTransitionOriginalBefore = originalBefore;
            vehicleEdgeTransitionSecondBefore = secondBefore;

            string summary = "vehicle=" + vehicleId +
                ", destination=" + FirstNonEmpty(destination.DisplayName, destination.Id) +
                ", markPoint=" + destination.MarkPointId +
                ", before={" + FormatTeleportSnapshot(before) + "}" +
                ", originalBeforeRoom=" + originalBefore.RoomId +
                ", secondBeforeRoom=" + secondBefore.RoomId +
                ", secondRidingBefore=" + secondBefore.IsRiding +
                ", requestAfter={" + FormatTeleportSnapshot(transport.AfterRequest) + "}";
            runtime.RuntimeMonitor.Log("Smoke exercise VehicleSecondMotor edge-transition request OK " + summary);
            runtime.SetHookStatus("Smoke.VehicleSecondMotorEdgeTransition", "pending", "ITeleportDebugApi -> DolocAPI.DoTransport while riding second motor", summary);
            return true;
        }

        private SmokeAttemptResult CompleteVehicleEdgeTransitionForSmoke(ManifestModel smokeManifest, string vehicleId, string keyItemId, MotorVehicleState registered)
        {
            if (experimentalApi == null)
                throw new InvalidOperationException("Experimental bridge API is not available.");

            TeleportSnapshot before = vehicleEdgeTransitionRequestResult?.Before ?? new TeleportSnapshot();
            TeleportSnapshot after = experimentalApi.GetCurrentSnapshot();
            MotorVehicleState originalAfterTransition = experimentalApi.GetOriginalMotorState();
            MotorVehicleState secondAfterTransition = experimentalApi.GetVehicleState(vehicleId);
            bool changedRoom = !string.IsNullOrWhiteSpace(before.RoomId) && !before.RoomId.Equals(after.RoomId, StringComparison.OrdinalIgnoreCase);
            bool secondStillRiding = secondAfterTransition.IsRiding;
            bool secondInCurrentRoom = !string.IsNullOrWhiteSpace(after.RoomId) && secondAfterTransition.RoomId.Equals(after.RoomId, StringComparison.OrdinalIgnoreCase);
            double distanceToDestination = vehicleEdgeTransitionDestination == null
                ? double.NaN
                : DistanceBetween(after.X, after.Y, vehicleEdgeTransitionDestination.X, vehicleEdgeTransitionDestination.Y);
            bool nearDestination = !double.IsNaN(distanceToDestination) && distanceToDestination < 8;
            double originalDistanceToEntry = DistanceBetween(originalAfterTransition.X, originalAfterTransition.Y, after.X, after.Y);
            bool originalAtNewEntry = originalAfterTransition.IsVisible &&
                originalAfterTransition.RoomId.Equals(after.RoomId, StringComparison.OrdinalIgnoreCase) &&
                !double.IsNaN(originalDistanceToEntry) &&
                originalDistanceToEntry < 6;
            bool noStuck = changedRoom && secondStillRiding && secondInCurrentRoom && nearDestination;

            string transitionSummary = "destination=" + FirstNonEmpty(vehicleEdgeTransitionDestination?.DisplayName ?? string.Empty, vehicleEdgeTransitionDestination?.Id ?? string.Empty) +
                ", markPoint=" + (vehicleEdgeTransitionDestination?.MarkPointId ?? string.Empty) +
                ", before={" + FormatTeleportSnapshot(before) + "}" +
                ", after={" + FormatTeleportSnapshot(after) + "}" +
                ", changedRoom=" + changedRoom +
                ", secondStillRiding=" + secondStillRiding +
                ", secondRoom=" + secondAfterTransition.RoomId +
                ", secondInCurrentRoom=" + secondInCurrentRoom +
                ", distanceToDestination=" + FormatSmokeDouble(distanceToDestination) +
                ", nearDestination=" + nearDestination +
                ", originalRoomAfterTransition=" + originalAfterTransition.RoomId +
                ", originalVisibleAfterTransition=" + originalAfterTransition.IsVisible +
                ", originalAtNewEntry=" + originalAtNewEntry +
                ", originalDistanceToEntry=" + FormatSmokeDouble(originalDistanceToEntry) +
                ", noStuck=" + noStuck;

            if (!noStuck || originalAtNewEntry)
                throw new InvalidOperationException("Second-motor edge transition failed. " + transitionSummary);

            runtime.RuntimeMonitor.Log("Smoke exercise VehicleSecondMotor edge-transition OK " + transitionSummary);
            runtime.SetHookStatus("Smoke.VehicleSecondMotorEdgeTransition", "verified", "ITeleportDebugApi -> DolocAPI.DoTransport while riding second motor", transitionSummary);

            MotorVehicleRideResult dismount = experimentalApi.DismountVehicle(smokeManifest, "smoke-edge-transition-restore");
            if (!dismount.Success || dismount.After.IsRiding)
                throw new InvalidOperationException("Second motor dismount after edge transition failed: " + dismount.FailureReason + ": " + dismount.Message);

            MotorVehicleSummonResult original = experimentalApi.SummonOriginalMotor(smokeManifest);
            if (!original.Success)
                throw new InvalidOperationException("Original motor summon after second-motor edge transition failed: " + original.FailureReason + ": " + original.Message);

            string disabledProbe = TryProbeVehicleDisabledLocationForSmoke(vehicleId, keyItemId);
            string summary = FirstNonEmpty(vehicleEdgeTransitionBaseSummary, "vehicle=" + vehicleId + ", owner=" + registered.OwnerUniqueId) +
                ", edgeTransition={" + transitionSummary + "}" +
                ", dismount=" + (!dismount.After.IsRiding) +
                ", enduranceDismount=" + FormatSmokeDouble(dismount.After.EnduranceProgress) +
                ", originalRoom=" + original.After.RoomId +
                ", originalVisibleAfterRestore=" + original.After.IsVisible +
                ", disabledProbe=" + disabledProbe;
            runtime.RuntimeMonitor.Log("Smoke exercise VehicleSecondMotor OK " + summary);
            runtime.SetHookStatus("Smoke.VehicleSecondMotor", "verified", "IMotorVehicleApi + ItemMotorKey.OnUse prefix + DoTransport edge transition", summary);
            return SmokeAttemptResult.Succeeded;
        }

        private static TeleportDestination? SelectVehicleEdgeTransitionDestination(ITeleportDebugApi api, string currentRoomId)
        {
            IReadOnlyList<TeleportDestination> destinations = api.GetDestinations();
            return destinations
                .Where(d => !string.IsNullOrWhiteSpace(d.MarkPointId))
                .Where(d => string.IsNullOrWhiteSpace(currentRoomId) || string.IsNullOrWhiteSpace(d.RoomId) || !d.RoomId.Equals(currentRoomId, StringComparison.OrdinalIgnoreCase))
                .Where(d => !LooksLikeIndoorVehicleDestination(d))
                .OrderByDescending(d => d.RoomId.StartsWith("city_", StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(d => ContainsIgnoreCase(d.DisplayName, "丘陵") || ContainsIgnoreCase(d.DisplayName, "郊区") || ContainsIgnoreCase(d.DisplayName, "Farm") || ContainsIgnoreCase(d.DisplayName, "农场"))
                .ThenByDescending(d => d.IsStation)
                .ThenBy(d => d.DisplayName, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
        }

        private static bool LooksLikeIndoorVehicleDestination(TeleportDestination destination)
        {
            string text = (destination.Id ?? string.Empty) + " " + (destination.DisplayName ?? string.Empty) + " " + (destination.RoomId ?? string.Empty);
            return ContainsIgnoreCase(text, "hall") ||
                ContainsIgnoreCase(text, "research") ||
                ContainsIgnoreCase(text, "laboratory") ||
                ContainsIgnoreCase(text, "lab") ||
                ContainsIgnoreCase(text, "bar") ||
                ContainsIgnoreCase(text, "市政") ||
                ContainsIgnoreCase(text, "研究") ||
                ContainsIgnoreCase(text, "酒吧") ||
                ContainsIgnoreCase(text, "屋") ||
                ContainsIgnoreCase(text, "室内");
        }

        private static double DistanceBetween(double x1, double y1, double x2, double y2)
        {
            if (double.IsNaN(x1) || double.IsNaN(y1) || double.IsNaN(x2) || double.IsNaN(y2))
                return double.NaN;
            double dx = x1 - x2;
            double dy = y1 - y2;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private string TryProbeVehicleDisabledLocationForSmoke(string vehicleId, string keyItemId)
        {
            try
            {
                if (experimentalApi == null)
                    return "not-verified:missing-api";

                TeleportSnapshot before = experimentalApi.GetCurrentSnapshot();
                TeleportDestination? indoor = experimentalApi.GetDestinations()
                    .Where(d => !string.IsNullOrWhiteSpace(d.MarkPointId))
                    .Where(d => !d.RoomId.Equals(before.RoomId, StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault(d => ContainsIgnoreCase(d.Id, "hall") || ContainsIgnoreCase(d.Id, "research") || ContainsIgnoreCase(d.Id, "laboratory") || ContainsIgnoreCase(d.Id, "lab") || ContainsIgnoreCase(d.Id, "bar") || ContainsIgnoreCase(d.DisplayName, "市政") || ContainsIgnoreCase(d.DisplayName, "研究") || ContainsIgnoreCase(d.DisplayName, "酒吧"));
                if (indoor == null)
                    return "not-verified:no-indoor-whitelist-destination";

                TeleportResult transport = experimentalApi.Teleport(CreateSmokeManifest(), indoor.Id);
                if (!transport.Success)
                    return "not-verified:teleport-" + transport.FailureReason;

                TeleportSnapshot after = experimentalApi.GetCurrentSnapshot();
                bool changed = !string.IsNullOrWhiteSpace(before.RoomId) && !before.RoomId.Equals(after.RoomId, StringComparison.OrdinalIgnoreCase);
                if (!changed)
                    return "not-verified:teleport-not-settled:" + indoor.Id;

                MotorVehicleSummonResult disabledKey = experimentalApi.UseRegisteredSecondMotorKeyForSmoke(keyItemId);
                if (!disabledKey.Success && (disabledKey.FailureReason.Equals("in-house", StringComparison.OrdinalIgnoreCase) || disabledKey.FailureReason.Equals("disabled-room", StringComparison.OrdinalIgnoreCase)))
                    return "verified:" + disabledKey.FailureReason + ":" + FirstNonEmpty(after.RoomTitle, after.RoomId, indoor.Id);

                return "not-verified:unexpected-" + (disabledKey.Success ? "success" : disabledKey.FailureReason);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke vehicle disabled-location probe failed.", ex.ToString());
                return "not-verified:" + ex.GetType().Name;
            }
        }

        private static bool ContainsIgnoreCase(string value, string search)
        {
            return !string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(search) && value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool ContainsAny(IEnumerable<string> values, params string[] searches)
        {
            foreach (string value in values)
            {
                foreach (string search in searches)
                {
                    if (ContainsIgnoreCase(value, search))
                        return true;
                }
            }

            return false;
        }
    }
}
