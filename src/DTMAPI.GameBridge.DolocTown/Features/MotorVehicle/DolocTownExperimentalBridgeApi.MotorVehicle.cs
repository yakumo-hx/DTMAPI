using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DTMAPI.Abstractions;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class DolocTownExperimentalBridgeApi
    {
        internal void SetMotorVehicleHooksInstalled(bool installed)
        {
            motorVehicleHooksInstalled = installed;
        }

        public MotorVehicleState GetOriginalMotorState()
        {
            return BuildOriginalMotorState("query");
        }

        public MotorVehicleState GetVehicleState(string vehicleId)
        {
            if (string.IsNullOrWhiteSpace(vehicleId) ||
                vehicleId.Equals("original", StringComparison.OrdinalIgnoreCase) ||
                vehicleId.Equals("doloc.original_motor", StringComparison.OrdinalIgnoreCase))
                return GetOriginalMotorState();

            return secondMotors.TryGetValue(vehicleId, out SecondMotorRuntime vehicle)
                ? BuildSecondMotorState(vehicle, "query")
                : new MotorVehicleState
                {
                    VehicleId = vehicleId ?? string.Empty,
                    LastFailureReason = "not-registered",
                    LastMessage = "No DTMAPI second motor is registered with this id."
                };
        }

        public IReadOnlyList<MotorVehicleState> GetVehicles()
        {
            var result = new List<MotorVehicleState> { GetOriginalMotorState() };
            foreach (SecondMotorRuntime vehicle in secondMotors.Values.OrderBy(v => v.Options.VehicleId, StringComparer.OrdinalIgnoreCase))
                result.Add(BuildSecondMotorState(vehicle, "query"));
            return result.ToArray();
        }

        public MotorVehicleRegisterResult RegisterSecondMotor(IManifest owner, SecondMotorOptions options)
        {
            string ownerId = owner?.UniqueID ?? "unknown";
            options ??= new SecondMotorOptions();
            options.VehicleId = FirstText(options.VehicleId, ownerId + ".second_motor");
            options.KeyItemId = FirstText(options.KeyItemId, "dtmapi_second_motor_key");
            options.DisplayName = FirstText(options.DisplayName, "Second Motor");
            options.SpeedMultiplier = Math.Max(0.1, Math.Min(8, options.SpeedMultiplier <= 0 ? 2 : options.SpeedMultiplier));

            var result = new MotorVehicleRegisterResult
            {
                VehicleId = options.VehicleId,
                KeyItemId = options.KeyItemId
            };

            if (owner == null)
                return MotorVehicleRegisterFailed(result, "missing-owner", "Vehicle registration requires a mod manifest owner.");
            if (string.IsNullOrWhiteSpace(options.VehicleId) || string.IsNullOrWhiteSpace(options.KeyItemId))
                return MotorVehicleRegisterFailed(result, "invalid-options", "VehicleId and KeyItemId are required.");
            if (!IsOwnerOfficiallyEnabled(owner.UniqueID, out string enablementMessage))
                return MotorVehicleRegisterFailed(result, "source-disabled", enablementMessage);
            if (secondMotorsByKeyItemId.TryGetValue(options.KeyItemId, out SecondMotorRuntime existingByKey) &&
                !existingByKey.Options.VehicleId.Equals(options.VehicleId, StringComparison.OrdinalIgnoreCase))
                return MotorVehicleRegisterFailed(result, "duplicate-key", "Key item " + options.KeyItemId + " is already registered by " + existingByKey.OwnerUniqueId + ".");

            if (!secondMotors.TryGetValue(options.VehicleId, out SecondMotorRuntime vehicle))
            {
                vehicle = new SecondMotorRuntime(ownerId, options);
                secondMotors[options.VehicleId] = vehicle;
            }
            else
            {
                vehicle.OwnerUniqueId = ownerId;
                vehicle.Options = options;
            }
            secondMotorsByKeyItemId[options.KeyItemId] = vehicle;

            vehicle.LastMessage = "Registered DTMAPI second motor key=" + options.KeyItemId + " speedMultiplier=" + options.SpeedMultiplier.ToString("0.###") + ".";
            result.Success = true;
            result.State = BuildSecondMotorState(vehicle, "registered");
            result.Message = vehicle.LastMessage;
            runtime.RuntimeMonitor.Log("Motor vehicle registration owner=" + ownerId + " vehicle=" + options.VehicleId + " key=" + options.KeyItemId + " speedMultiplier=" + options.SpeedMultiplier.ToString("0.###") + " visuals=" + (options.UseOriginalMotorVisuals ? "original-runtime-clone" : "custom") + " note=" + options.TextureSourceNote);
            runtime.SetHookStatus("Vehicle.SecondMotorRegistration", "experimental", "IMotorVehicleApi.RegisterSecondMotor", result.Message);
            RaiseVehicleEvent("registered", vehicle.Options.VehicleId, result.State, result.Message);
            return result;
        }

        public MotorVehicleSummonResult UnlockOriginalMotor(IManifest owner, double yOffset)
        {
            string ownerId = owner?.UniqueID ?? "unknown";
            var result = new MotorVehicleSummonResult
            {
                VehicleId = "doloc.original_motor",
                DisplayName = "Original Motor",
                Before = GetOriginalMotorState()
            };

            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                MethodInfo? unlock = dolocApi?.GetMethod("UnlockMotor", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(float) }, null);
                if (unlock == null)
                    return MotorVehicleSummonFailed(result, "missing-unlock", "DolocAPI.UnlockMotor(float) was not found.");

                unlock.Invoke(null, new object[] { (float)yOffset });
                result.After = GetOriginalMotorState();
                result.Success = true;
                result.Message = "Original motor unlock requested owner=" + ownerId + " beforeUnlocked=" + result.Before.IsUnlocked + " afterUnlocked=" + result.After.IsUnlocked + ".";
                runtime.RuntimeMonitor.Log("Motor vehicle unlock OK " + result.Message);
                runtime.SetHookStatus("Vehicle.OriginalMotorUnlock", "experimental", "DolocAPI.UnlockMotor", result.Message);
                RaiseVehicleEvent("unlock", result.VehicleId, result.After, result.Message);
                return result;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Original motor unlock failed.", ex.ToString());
                return MotorVehicleSummonFailed(result, ex.GetType().Name, ex.Message);
            }
        }

        public MotorVehicleSummonResult SummonOriginalMotor(IManifest owner)
        {
            string ownerId = owner?.UniqueID ?? "unknown";
            var result = new MotorVehicleSummonResult
            {
                VehicleId = "doloc.original_motor",
                DisplayName = "Original Motor",
                Before = GetOriginalMotorState()
            };

            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                if (dolocApi == null)
                    return MotorVehicleSummonFailed(result, "missing-dolocapi", "DolocAPI is not available.");

                if (!result.Before.IsUnlocked)
                {
                    MethodInfo? unlock = dolocApi.GetMethod("UnlockMotor", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(float) }, null);
                    unlock?.Invoke(null, new object[] { 0f });
                }

                if (!CanCallMotorInCurrentRoom(dolocApi, out string reason, out string message))
                    return MotorVehicleSummonFailed(result, reason, message);

                object? currentRoom = ReadStaticMember(dolocApi, "CurrentRoom");
                object? target = BuildMotorSummonPositionNearAgent(0.95f);
                if (currentRoom == null || target == null)
                    return MotorVehicleSummonFailed(result, "missing-position", "Current room or agent position was not available.");

                MethodInfo? setMotorPosition = dolocApi.GetMethod("SetMotorPosition", BindingFlags.Public | BindingFlags.Static);
                if (setMotorPosition == null)
                    return MotorVehicleSummonFailed(result, "missing-set-position", "DolocAPI.SetMotorPosition(Room, Vector2) was not found.");

                setMotorPosition.Invoke(null, new[] { currentRoom, target });
                object? motor = ReadStaticMember(dolocApi, "Motor");
                TryInvokeAutoFlyToAgent(motor);
                result.After = GetOriginalMotorState();
                result.Success = true;
                result.Message = "Original motor summon requested owner=" + ownerId + " room=" + result.After.RoomId + " x=" + FormatRatio(result.After.X) + " y=" + FormatRatio(result.After.Y) + ".";
                runtime.RuntimeMonitor.Log("Motor vehicle original summon OK " + result.Message);
                runtime.SetHookStatus("Vehicle.OriginalMotorSummon", "experimental", "DolocAPI.SetMotorPosition + MotorController.AutoFlyTo", result.Message);
                RaiseVehicleEvent("summon", result.VehicleId, result.After, result.Message);
                return result;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Original motor summon failed.", ex.ToString());
                return MotorVehicleSummonFailed(result, ex.GetType().Name, ex.Message);
            }
        }

        public MotorVehicleSummonResult SummonVehicle(IManifest owner, string vehicleId)
        {
            if (string.IsNullOrWhiteSpace(vehicleId) ||
                vehicleId.Equals("original", StringComparison.OrdinalIgnoreCase) ||
                vehicleId.Equals("doloc.original_motor", StringComparison.OrdinalIgnoreCase))
                return SummonOriginalMotor(owner);

            if (!secondMotors.TryGetValue(vehicleId, out SecondMotorRuntime vehicle))
            {
                return MotorVehicleSummonFailed(new MotorVehicleSummonResult
                {
                    VehicleId = vehicleId ?? string.Empty,
                    Before = new MotorVehicleState { VehicleId = vehicleId ?? string.Empty }
                }, "not-registered", "No DTMAPI second motor is registered with this id.");
            }

            return SummonSecondMotor(owner, vehicle, "api");
        }

        public MotorVehicleRideResult RideVehicle(IManifest owner, string vehicleId)
        {
            if (string.IsNullOrWhiteSpace(vehicleId) ||
                vehicleId.Equals("original", StringComparison.OrdinalIgnoreCase) ||
                vehicleId.Equals("doloc.original_motor", StringComparison.OrdinalIgnoreCase))
            {
                return MotorVehicleRideFailed(new MotorVehicleRideResult
                {
                    VehicleId = "doloc.original_motor",
                    DisplayName = "Original Motor",
                    Action = "ride",
                    Before = GetOriginalMotorState()
                }, "native-only", "Programmatic riding currently supports DTMAPI-managed second motors only; use the game's native interaction for the original motor.");
            }

            if (!secondMotors.TryGetValue(vehicleId, out SecondMotorRuntime vehicle))
            {
                return MotorVehicleRideFailed(new MotorVehicleRideResult
                {
                    VehicleId = vehicleId ?? string.Empty,
                    Action = "ride",
                    Before = new MotorVehicleState { VehicleId = vehicleId ?? string.Empty }
                }, "not-registered", "No DTMAPI second motor is registered with this id.");
            }

            var result = new MotorVehicleRideResult
            {
                VehicleId = vehicle.Options.VehicleId,
                DisplayName = vehicle.Options.DisplayName,
                Action = "ride",
                Before = BuildSecondMotorState(vehicle, "before-ride")
            };

            bool success = TryStartSecondMotorRide(vehicle, "api:" + (owner?.UniqueID ?? "unknown"));
            result.After = BuildSecondMotorState(vehicle, "after-ride");
            if (!success || !result.After.IsRiding)
                return MotorVehicleRideFailed(result, FirstText(vehicle.LastFailureReason, "ride-failed"), FirstText(vehicle.LastMessage, "Second motor ride request did not enter riding state."));

            result.Success = true;
            result.Message = vehicle.LastMessage;
            runtime.SetHookStatus("Vehicle.SecondMotorRide", "experimental", "IMotorVehicleApi.RideVehicle -> AgentControllerState.GetOnMotor", result.Message);
            return result;
        }

        public MotorVehicleRideResult DismountVehicle(IManifest owner, string reason)
        {
            SecondMotorRuntime? dismountedSecondMotor = activeSecondMotor;
            string vehicleId = dismountedSecondMotor?.Options.VehicleId ?? "doloc.original_motor";
            string displayName = dismountedSecondMotor?.Options.DisplayName ?? "Original Motor";
            MotorVehicleState before = dismountedSecondMotor == null ? GetOriginalMotorState() : BuildSecondMotorState(dismountedSecondMotor, "before-dismount");
            var result = new MotorVehicleRideResult
            {
                VehicleId = vehicleId,
                DisplayName = displayName,
                Action = "dismount",
                Before = before
            };

            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                if (dolocApi == null)
                    return MotorVehicleRideFailed(result, "missing-dolocapi", "DolocAPI is not available.");

                if (!ReadStaticBoolMember(dolocApi, "IsAgentRiding", false) && activeSecondMotor == null)
                {
                    result.After = before;
                    result.Success = true;
                    result.Message = "No active motor ride was observed; dismount treated as a no-op owner=" + (owner?.UniqueID ?? "unknown") + ".";
                    return result;
                }

                object? agentController = GetAgentController(dolocApi);
                if (agentController == null)
                    return MotorVehicleRideFailed(result, "missing-agent-controller", "DolocAPI.AgentController was not available.");

                MethodInfo? getOff = FindMethodInHierarchy(agentController.GetType(), "GetOffMotor", 0);
                if (getOff == null)
                    return MotorVehicleRideFailed(result, "missing-get-off", "AgentControllerState.GetOffMotor was not found.");

                getOff.Invoke(agentController, null);
                result.After = dismountedSecondMotor == null ? GetOriginalMotorState() : BuildSecondMotorState(dismountedSecondMotor, "after-dismount");
                result.Success = true;
                result.Message = "Motor dismount requested owner=" + (owner?.UniqueID ?? "unknown") + " reason=" + (reason ?? string.Empty) + ".";
                runtime.SetHookStatus("Vehicle.MotorDismount", "experimental", "IMotorVehicleApi.DismountVehicle -> AgentControllerState.GetOffMotor", result.Message);
                return result;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Motor dismount failed.", ex.ToString());
                return MotorVehicleRideFailed(result, ex.GetType().Name, ex.Message);
            }
        }

        BridgeFeatureStatus IMotorVehicleApi.GetStatus(string uniqueId)
        {
            if (string.IsNullOrWhiteSpace(uniqueId))
                return new BridgeFeatureStatus(motorVehicleHooksInstalled ? "experimental" : "pending-hook", "Motor API exposes original motor state plus DTMAPI-managed second motor registration, summon, ride, dismount, and instance-scoped clone appearance. Second motor riding uses private AgentControllerState.motorController routing and remains experimental.");
            bool registered = secondMotors.Values.Any(v => v.OwnerUniqueId.Equals(uniqueId, StringComparison.OrdinalIgnoreCase));
            if (!registered)
                return new BridgeFeatureStatus(motorVehicleHooksInstalled ? "available" : "pending-hook", "No second motor is registered for this owner.");
            return new BridgeFeatureStatus(motorVehicleHooksInstalled ? "configured-experimental" : "configured-pending-hook", motorVehicleHooksInstalled ? "Second motor is registered; key/use, riding, and scoped clone-appearance hooks are installed but require third-save smoke evidence." : "Second motor is registered; waiting for motor hook targets.");
        }

        internal MotorVehicleSummonResult UseRegisteredSecondMotorKeyForSmoke(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId) || !secondMotorsByKeyItemId.TryGetValue(itemId, out SecondMotorRuntime vehicle))
            {
                return MotorVehicleSummonFailed(new MotorVehicleSummonResult
                {
                    VehicleId = itemId ?? string.Empty,
                    DisplayName = "Second Motor",
                    Before = new MotorVehicleState { VehicleId = itemId ?? string.Empty }
                }, "not-registered", "No DTMAPI second motor key is registered with this item id.");
            }

            var result = new MotorVehicleSummonResult
            {
                VehicleId = vehicle.Options.VehicleId,
                DisplayName = vehicle.Options.DisplayName,
                Before = BuildSecondMotorState(vehicle, "before-smoke-key-use")
            };

            if (!TryGenerateNativeItem(itemId, 1, out object? item, out string itemReason, out string itemMessage))
                return MotorVehicleSummonFailed(result, itemReason, itemMessage);

            try
            {
                MethodInfo? onUse = FindMethodInHierarchy(item!.GetType(), "OnUse", 0);
                if (onUse == null)
                    return MotorVehicleSummonFailed(result, "missing-on-use", "Generated native item " + item.GetType().FullName + " does not expose OnUse().");

                onUse.Invoke(item, null);
                result.After = BuildSecondMotorState(vehicle, "after-smoke-key-use");
                if (!result.After.IsVisible)
                    return MotorVehicleSummonFailed(result, FirstText(vehicle.LastFailureReason, "key-hook-not-observed"), FirstText(vehicle.LastMessage, "Generated second motor key OnUse did not leave the DTMAPI second motor visible; ItemMotorKey.OnUse prefix may not have intercepted the key."));

                result.Success = true;
                result.Message = "Native ItemFactory generated " + item.GetType().FullName + " and OnUse triggered the registered second motor key path.";
                runtime.SetHookStatus("Smoke.VehicleSecondMotorKey", "verified", "ItemFactory.GenerateItem -> ItemMotorKey.OnUse prefix", result.Message);
                return result;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Second motor key smoke use failed.", ex.ToString());
                return MotorVehicleSummonFailed(result, ex.GetType().Name, ex.Message);
            }
        }

        internal string ProbeSecondMotorAppearanceForSmoke(string vehicleId)
        {
            if (string.IsNullOrWhiteSpace(vehicleId) || !secondMotors.TryGetValue(vehicleId, out SecondMotorRuntime vehicle))
                return "appearanceIsolated=False, reason=not-registered";

            if (!EnsureSecondMotorInstance(vehicle, out string reason, out string message))
                return "appearanceIsolated=False, reason=" + reason + ", message=" + message;

            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? originalMotor = ReadStaticMember(dolocApi, "Motor");
            CountSecondMotorScopedTintRenderers(originalMotor, out int originalTotal, out int originalTinted, out int originalSkippedDriver);
            CountSecondMotorScopedTintRenderers(vehicle.Controller, out int secondTotal, out int secondTinted, out int secondSkippedDriver);

            bool customExpected = !vehicle.Options.UseOriginalMotorVisuals;
            bool isolated = !customExpected || (originalTinted == 0 && secondTinted > 0);
            string summary = "appearanceIsolated=" + isolated +
                ", originalScopedTint=" + originalTinted + "/" + originalTotal +
                ", originalSkippedDriver=" + originalSkippedDriver +
                ", secondScopedTint=" + secondTinted + "/" + secondTotal +
                ", secondSkippedDriver=" + secondSkippedDriver +
                ", " + FirstText(vehicle.AppearanceSummary, "appearance=not-applied");
            runtime.RuntimeMonitor.Log("Smoke exercise VehicleSecondMotor appearance probe " + summary);
            runtime.SetHookStatus("Vehicle.SecondMotorAppearance", isolated ? "experimental" : "failed", "DTMAPI cloned MotorController SpriteRenderer.color", summary);
            return summary;
        }

        internal MotorVehicleSummonResult UseOriginalMotorKeyForSmoke(string itemId)
        {
            const string originalVehicleId = "doloc.original_motor";
            var result = new MotorVehicleSummonResult
            {
                VehicleId = originalVehicleId,
                DisplayName = "Original Motor",
                Before = BuildOriginalMotorState("before-smoke-original-key-use")
            };

            if (string.IsNullOrWhiteSpace(itemId))
                return MotorVehicleSummonFailed(result, "missing-item-id", "Original motor key item id is required.");
            if (secondMotorsByKeyItemId.ContainsKey(itemId))
                return MotorVehicleSummonFailed(result, "registered-second-key", "Item " + itemId + " is registered as a DTMAPI second motor key, not the original motor key.");
            if (!TryGenerateNativeItem(itemId, 1, out object? item, out string itemReason, out string itemMessage))
                return MotorVehicleSummonFailed(result, itemReason, itemMessage);

            try
            {
                MethodInfo? onUse = FindMethodInHierarchy(item!.GetType(), "OnUse", 0);
                if (onUse == null)
                    return MotorVehicleSummonFailed(result, "missing-on-use", "Generated native item " + item.GetType().FullName + " does not expose OnUse().");

                onUse.Invoke(item, null);
                result.After = BuildOriginalMotorState("after-smoke-original-key-use");
                if (!result.After.IsVisible)
                    return MotorVehicleSummonFailed(result, "original-key-not-visible", "Generated original motor key OnUse did not leave the original motor visible.");

                result.Success = true;
                result.Message = "Native ItemFactory generated " + item.GetType().FullName + " and OnUse followed the original motor key path.";
                runtime.SetHookStatus("Smoke.VehicleOriginalMotorKey", "verified", "ItemFactory.GenerateItem -> native ItemMotorKey.OnUse", result.Message);
                return result;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Original motor key smoke use failed.", ex.ToString());
                return MotorVehicleSummonFailed(result, ex.GetType().Name, ex.Message);
            }
        }

        internal bool HandleMotorKeyUse(object item)
        {
            string itemId = ReadStringMember(item, "name");
            if (string.IsNullOrWhiteSpace(itemId) || !secondMotorsByKeyItemId.TryGetValue(itemId, out SecondMotorRuntime vehicle))
                return true;

            if (!IsOwnerOfficiallyEnabled(vehicle.OwnerUniqueId, out string enablementMessage))
            {
                string cleanup = CleanupSecondMotorRuntime(vehicle, "disabled key use", destroyGameObject: true);
                vehicle.LastFailureReason = "source-disabled";
                vehicle.LastMessage = enablementMessage + " cleanup={" + cleanup + "}";
                runtime.RuntimeMonitor.Log("Second motor key blocked because owner source is disabled item=" + itemId + " message=" + vehicle.LastMessage);
                runtime.SetHookStatus("Vehicle.SecondMotorKeyFailed", "failed", "ItemMotorKey.OnUse prefix + official enablement", vehicle.LastMessage);
                return false;
            }

            MotorVehicleSummonResult result = SummonSecondMotor(null!, vehicle, "key:" + itemId);
            runtime.RuntimeMonitor.Log("Second motor key intercepted item=" + itemId + " success=" + result.Success + " reason=" + result.FailureReason + " message=" + result.Message);
            runtime.SetHookStatus(result.Success ? "Vehicle.SecondMotorKey" : "Vehicle.SecondMotorKeyFailed", result.Success ? "experimental" : "failed", "ItemMotorKey.OnUse prefix", result.Message);
            return false;
        }

        internal bool HandleMotorInteract(object interactable)
        {
            if (!TryResolveSecondMotorFromInteractable(interactable, out SecondMotorRuntime vehicle))
                return true;

            bool success = TryStartSecondMotorRide(vehicle, "interact");
            runtime.SetHookStatus(success ? "Vehicle.SecondMotorRide" : "Vehicle.SecondMotorRideFailed", success ? "experimental" : "failed", "MotorInteractable.OnInteract prefix", vehicle.LastMessage);
            return false;
        }

        internal void NotifyMotorGetOn(object agentControllerState)
        {
            object? current = ReadMember(agentControllerState, "motorController");
            if (activeSecondMotor != null && current != null && ReferenceEquals(current, activeSecondMotor.Controller))
            {
                activeSecondMotor.IsRiding = true;
                MotorVehicleState state = BuildSecondMotorState(activeSecondMotor, "ride-on");
                runtime.RuntimeMonitor.Log("Second motor ride-on observed vehicle=" + activeSecondMotor.Options.VehicleId + " room=" + state.RoomId + ".");
                RaiseVehicleEvent("ride-on", activeSecondMotor.Options.VehicleId, state, "Second motor ride-on observed.");
                return;
            }

            MotorVehicleState original = BuildOriginalMotorState("ride-on");
            RaiseVehicleEvent("ride-on", original.VehicleId, original, "Original motor ride-on observed.");
        }

        internal void NotifyMotorGetOff(object agentControllerState)
        {
            if (activeSecondMotor == null)
            {
                MotorVehicleState original = BuildOriginalMotorState("ride-off");
                RaiseVehicleEvent("ride-off", original.VehicleId, original, "Original motor ride-off observed.");
                return;
            }

            SecondMotorRuntime vehicle = activeSecondMotor;
            vehicle.IsRiding = false;
            RestoreOriginalAgentMotorController(agentControllerState);
            RestoreOriginalMotorSnapshot(clearSnapshot: true);
            activeSecondMotor = null;
            MotorVehicleState state = BuildSecondMotorState(vehicle, "ride-off");
            string message = "Second motor ride-off restored original AgentControllerState.motorController and original motor snapshot.";
            vehicle.LastMessage = message;
            runtime.RuntimeMonitor.Log(message + " vehicle=" + vehicle.Options.VehicleId + ".");
            runtime.SetHookStatus("Vehicle.SecondMotorRideOff", "experimental", "AgentControllerState.GetOffMotor postfix", message);
            RaiseVehicleEvent("ride-off", vehicle.Options.VehicleId, state, message);
        }

        internal void ApplySecondMotorTuningForFixedUpdate(object motorController)
        {
            if (!secondMotorControllers.Contains(motorController))
                return;
            SecondMotorRuntime? vehicle = ResolveSecondMotorByController(motorController);
            if (vehicle == null || activeSecondMotorTuningSnapshot != null)
                return;

            object? globalParameter = ReadStaticMember(ResolveType("DolocAPI, Assembly-CSharp"), "GlobalParameter");
            if (globalParameter == null)
                return;

            double multiplier = Math.Max(0.1, vehicle.Options.SpeedMultiplier);
            activeSecondMotorTuningSnapshot = CaptureGlobalMotorTuning(globalParameter);
            WriteFloatMember(globalParameter, "MotorHorizontalAcceleration", (float)(activeSecondMotorTuningSnapshot.MotorHorizontalAcceleration * multiplier));
            WriteFloatMember(globalParameter, "MotorHorizontalRevertAcceleration", (float)(activeSecondMotorTuningSnapshot.MotorHorizontalRevertAcceleration * multiplier));
            WriteFloatMember(globalParameter, "MotorHorizontalDeceleration", (float)(activeSecondMotorTuningSnapshot.MotorHorizontalDeceleration * multiplier));
            WriteFloatMember(globalParameter, "MotorHorizontalMaxSpeed", (float)(activeSecondMotorTuningSnapshot.MotorHorizontalMaxSpeed * multiplier));
        }

        internal void RestoreSecondMotorTuningAfterFixedUpdate(object motorController)
        {
            if (activeSecondMotorTuningSnapshot == null)
                return;
            object? globalParameter = ReadStaticMember(ResolveType("DolocAPI, Assembly-CSharp"), "GlobalParameter");
            if (globalParameter != null)
                RestoreGlobalMotorTuning(globalParameter, activeSecondMotorTuningSnapshot);
            activeSecondMotorTuningSnapshot = null;
        }

        internal void NotifyOriginalMotorUnlocked()
        {
            MotorVehicleState state = BuildOriginalMotorState("unlock-hook");
            RaiseVehicleEvent("unlock", state.VehicleId, state, "DolocAPI.UnlockMotor completed.");
        }

        internal void NotifyOriginalMotorPositionChanged(object? room, object? position)
        {
            if (activeSecondMotor != null && !restoringOriginalMotorSnapshot)
            {
                SecondMotorRuntime vehicle = activeSecondMotor;
                vehicle.Room = room ?? ReadStaticMember(ResolveType("DolocAPI, Assembly-CSharp"), "CurrentRoom");
                vehicle.LastPosition = position;
                if (vehicle.Controller != null && position != null)
                    SetMotorControllerPosition(vehicle.Controller, position);
                RestoreOriginalMotorSnapshot(clearSnapshot: false);
                MotorVehicleState secondState = BuildSecondMotorState(vehicle, "position-hook-second-ride");
                string redirected = "Native SetMotorPosition observed while riding second motor; redirected position state to DTMAPI clone and restored original motor snapshot.";
                vehicle.LastMessage = redirected;
                runtime.RuntimeMonitor.Log(redirected + " vehicle=" + vehicle.Options.VehicleId + " room=" + secondState.RoomId + " x=" + FormatRatio(secondState.X) + " y=" + FormatRatio(secondState.Y) + ".");
                runtime.SetHookStatus("Vehicle.SecondMotorMapTransition", "experimental", "DolocAPI.SetMotorPosition postfix", redirected);
                RaiseVehicleEvent("position", vehicle.Options.VehicleId, secondState, redirected);
                return;
            }

            MotorVehicleState state = BuildOriginalMotorState("position-hook");
            RaiseVehicleEvent("position", state.VehicleId, state, "DolocAPI.SetMotorPosition completed.");
        }

        internal void NotifyEnterRoomForActiveSecondMotor(object? roomOrId, object? position, bool success)
        {
            if (!success || activeSecondMotor == null || position == null)
                return;

            object? targetRoom = ResolveRoomFromEnterRoomArgument(roomOrId);
            string targetRoomId = ReadStringMemberOrEmpty(targetRoom, "RoomId");
            if (string.IsNullOrWhiteSpace(targetRoomId))
                targetRoomId = roomOrId as string ?? string.Empty;
            if (string.IsNullOrWhiteSpace(targetRoomId))
                return;

            activeSecondMotor.PendingTransitionRoom = targetRoom;
            activeSecondMotor.PendingTransitionRoomId = targetRoomId;
            activeSecondMotor.PendingTransitionPosition = position;
            string message = "Second motor room transition target captured room=" + targetRoomId +
                " x=" + FormatRatio(ReadVectorComponent(position, "x")) +
                " y=" + FormatRatio(ReadVectorComponent(position, "y")) + ".";
            activeSecondMotor.LastMessage = message;
            runtime.SetHookStatus("Vehicle.SecondMotorMapTransition", "experimental", "DolocAPI.EnterRoom postfix", message);
        }

        private static object? ResolveRoomFromEnterRoomArgument(object? roomOrId)
        {
            if (roomOrId == null)
                return null;
            if (!string.IsNullOrWhiteSpace(ReadStringMemberOrEmpty(roomOrId, "RoomId")))
                return roomOrId;
            string roomId = roomOrId as string ?? string.Empty;
            if (string.IsNullOrWhiteSpace(roomId))
                return null;

            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            MethodInfo? queryRoom = dolocApi?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m =>
                {
                    ParameterInfo[] parameters = m.GetParameters();
                    return m.Name == "QueryRoom" &&
                        parameters.Length == 2 &&
                        parameters[0].ParameterType == typeof(string) &&
                        parameters[1].IsOut;
                });
            if (queryRoom == null)
                return null;
            object?[] args = { roomId, null };
            object? ok = queryRoom.Invoke(null, args);
            return ok is bool success && success ? args[1] : null;
        }

        private void UpdateActiveSecondMotorRoomSnapshot()
        {
            if (activeSecondMotor == null)
                return;

            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? currentRoom = ReadStaticMember(dolocApi, "CurrentRoom");
            string previousRoomId = ReadStringMemberOrEmpty(activeSecondMotor.Room, "RoomId");
            string currentRoomId = ReadStringMemberOrEmpty(currentRoom, "RoomId");
            bool roomChanged = !string.IsNullOrWhiteSpace(currentRoomId) &&
                !currentRoomId.Equals(previousRoomId, StringComparison.OrdinalIgnoreCase);
            bool appliedPendingTransition = false;
            if (activeSecondMotor.PendingTransitionPosition != null &&
                !string.IsNullOrWhiteSpace(activeSecondMotor.PendingTransitionRoomId) &&
                activeSecondMotor.PendingTransitionRoomId.Equals(currentRoomId, StringComparison.OrdinalIgnoreCase))
            {
                activeSecondMotor.Room = currentRoom ?? activeSecondMotor.PendingTransitionRoom;
                if (activeSecondMotor.Controller != null)
                {
                    SetMotorControllerPosition(activeSecondMotor.Controller, activeSecondMotor.PendingTransitionPosition);
                    activeSecondMotor.LastPosition = activeSecondMotor.PendingTransitionPosition;
                }
                object? agentController = GetAgentController(dolocApi);
                object? body = agentController == null ? null : ReadMember(agentController, "body");
                if (body != null)
                    SetMotorControllerPosition(body, activeSecondMotor.PendingTransitionPosition);
                string appliedMessage = "Second motor room transition applied room=" + currentRoomId +
                    " x=" + FormatRatio(ReadVectorComponent(activeSecondMotor.PendingTransitionPosition, "x")) +
                    " y=" + FormatRatio(ReadVectorComponent(activeSecondMotor.PendingTransitionPosition, "y")) + ".";
                activeSecondMotor.LastMessage = appliedMessage;
                runtime.SetHookStatus("Vehicle.SecondMotorMapTransition", "experimental", "DolocAPI.EnterRoom postfix + active ride sync", appliedMessage);
                activeSecondMotor.PendingTransitionRoom = null;
                activeSecondMotor.PendingTransitionRoomId = string.Empty;
                activeSecondMotor.PendingTransitionPosition = null;
                appliedPendingTransition = true;
            }
            if (!appliedPendingTransition && currentRoom != null)
                activeSecondMotor.Room = currentRoom;
            if (!appliedPendingTransition && activeSecondMotor.Controller != null && roomChanged)
            {
                object? agentPosition = ReadStaticMember(dolocApi, "AgentPosition");
                object? position2d = agentPosition == null ? null : CreateUnityVector2(ReadVectorComponent(agentPosition, "x"), ReadVectorComponent(agentPosition, "y"));
                if (position2d != null)
                {
                    SetMotorControllerPosition(activeSecondMotor.Controller, position2d);
                    activeSecondMotor.LastPosition = position2d;
                }
            }
            else if (!appliedPendingTransition && activeSecondMotor.Controller != null)
            {
                activeSecondMotor.LastPosition = ReadMember(activeSecondMotor.Controller, "position2d") ?? ReadMember(activeSecondMotor.Controller, "position") ?? activeSecondMotor.LastPosition;
            }
            RestoreOriginalMotorSnapshot(clearSnapshot: false);
            MirrorOriginalMotorTransformForSecondMotorAgentPosition(dolocApi);
        }

        private void MirrorOriginalMotorTransformForSecondMotorAgentPosition(Type? dolocApi)
        {
            if (activeSecondMotor?.Controller == null || originalMotorSnapshotBeforeSecondRide == null)
                return;

            string activeRoomId = ReadStringMemberOrEmpty(activeSecondMotor.Room, "RoomId");
            string originalRoomId = ReadStringMemberOrEmpty(originalMotorSnapshotBeforeSecondRide.Room, "RoomId");
            if (string.IsNullOrWhiteSpace(activeRoomId) ||
                activeRoomId.Equals(originalRoomId, StringComparison.OrdinalIgnoreCase))
                return;

            object? originalMotor = ReadStaticMember(dolocApi, "Motor");
            if (originalMotor == null || ReferenceEquals(originalMotor, activeSecondMotor.Controller))
                return;

            object? position = ReadMember(activeSecondMotor.Controller, "position2d") ?? ReadMember(activeSecondMotor.Controller, "position");
            if (position == null)
                return;

            SetMotorControllerPosition(originalMotor, position);
        }

        private MotorVehicleRegisterResult MotorVehicleRegisterFailed(MotorVehicleRegisterResult result, string reason, string message)
        {
            result.Success = false;
            result.FailureReason = reason ?? string.Empty;
            result.Message = message ?? string.Empty;
            runtime.SetHookStatus("Vehicle.SecondMotorRegistration", "failed", "IMotorVehicleApi.RegisterSecondMotor", result.Message);
            return result;
        }

        private MotorVehicleSummonResult MotorVehicleSummonFailed(MotorVehicleSummonResult result, string reason, string message)
        {
            result.Success = false;
            result.FailureReason = reason ?? string.Empty;
            result.Message = message ?? string.Empty;
            result.After = result.After.VehicleId.Length == 0 ? GetVehicleState(result.VehicleId) : result.After;
            runtime.RuntimeMonitor.Log("Motor vehicle request failed vehicle=" + result.VehicleId + " reason=" + result.FailureReason + " message=" + result.Message);
            return result;
        }

        private MotorVehicleSummonResult MotorVehicleSummonFailedWithCleanup(MotorVehicleSummonResult result, SecondMotorRuntime vehicle, string reason, string message, string cleanupReason)
        {
            string cleanup = CleanupSecondMotorRuntime(vehicle, cleanupReason + ": " + reason, destroyGameObject: true);
            result.After = BuildSecondMotorState(vehicle, "after-failed-summon-cleanup");
            string fullMessage = FirstText(message, "Second motor summon failed.") + " cleanup={" + cleanup + "}";
            return MotorVehicleSummonFailed(result, reason, fullMessage);
        }

        private MotorVehicleRideResult MotorVehicleRideFailed(MotorVehicleRideResult result, string reason, string message)
        {
            result.Success = false;
            result.FailureReason = reason ?? string.Empty;
            result.Message = message ?? string.Empty;
            result.After = result.After.VehicleId.Length == 0 ? GetVehicleState(result.VehicleId) : result.After;
            runtime.RuntimeMonitor.Log("Motor vehicle ride request failed action=" + result.Action + " vehicle=" + result.VehicleId + " reason=" + result.FailureReason + " message=" + result.Message);
            return result;
        }

        internal string CleanupSecondMotorResidueForBoundary(string reason)
        {
            int cleaned = 0;
            var summaries = new List<string>();
            foreach (SecondMotorRuntime vehicle in secondMotors.Values.ToArray())
            {
                string summary = CleanupSecondMotorRuntime(vehicle, reason, destroyGameObject: true);
                if (summary.IndexOf("cleaned=0", StringComparison.OrdinalIgnoreCase) < 0)
                    cleaned++;
                summaries.Add(vehicle.Options.VehicleId + "{" + summary + "}");
            }

            string message = "Second motor lifecycle cleanup reason=" + (reason ?? string.Empty) + " vehicles=" + secondMotors.Count + " cleanedVehicles=" + cleaned + " details=" + string.Join(";", summaries);
            runtime.RuntimeMonitor.Log(message);
            runtime.SetHookStatus("Vehicle.SecondMotorCleanup", "experimental", "SaveLoaded/ReturnedToTitle/failure cleanup", message);
            return message;
        }

        private string CleanupSecondMotorRuntime(SecondMotorRuntime vehicle, string reason, bool destroyGameObject)
        {
            if (vehicle == null)
                return "cleaned=0 reason=no-vehicle";

            int cleaned = 0;
            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                if (activeSecondMotor != null && ReferenceEquals(activeSecondMotor, vehicle))
                {
                    RestoreSecondMotorTuningAfterFixedUpdate(vehicle.Controller ?? new object());
                    RestoreOriginalAgentMotorController(GetAgentController(dolocApi));
                    RestoreOriginalMotorSnapshot(clearSnapshot: true);
                    activeSecondMotor = null;
                    cleaned++;
                }

                if (vehicle.Controller != null)
                {
                    MethodInfo? setVisible = FindMethodInHierarchy(vehicle.Controller.GetType(), "SetVisible", 1);
                    setVisible?.Invoke(vehicle.Controller, new object[] { false });
                    secondMotorControllers.Remove(vehicle.Controller);
                    cleaned++;
                }

                if (vehicle.Interactable != null)
                {
                    secondMotorInteractables.Remove(vehicle.Interactable);
                    cleaned++;
                }

                if (destroyGameObject && vehicle.GameObject != null)
                {
                    DestroyUnityObject(vehicle.GameObject);
                    cleaned++;
                }
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Second motor cleanup failed.", ex.ToString());
                return "cleaned=" + cleaned + ", error=" + ex.GetType().Name + ":" + ex.Message;
            }
            finally
            {
                vehicle.GameObject = null;
                vehicle.Controller = null;
                vehicle.Interactable = null;
                vehicle.Room = null;
                vehicle.LastPosition = null;
                vehicle.PendingTransitionRoom = null;
                vehicle.PendingTransitionRoomId = string.Empty;
                vehicle.PendingTransitionPosition = null;
                vehicle.IsRiding = false;
                vehicle.LastFailureReason = "cleaned";
                vehicle.LastMessage = "DTMAPI-owned second motor clone cleaned up. reason=" + (reason ?? string.Empty);
            }

            return "cleaned=" + cleaned + ", reason=" + (reason ?? string.Empty);
        }

        private bool IsOwnerOfficiallyEnabled(string ownerId, out string message)
        {
            message = string.Empty;
            if (string.IsNullOrWhiteSpace(ownerId) ||
                ownerId.Equals("unknown", StringComparison.OrdinalIgnoreCase) ||
                ownerId.Equals("DTMAPI.GameBridge.DolocTown", StringComparison.OrdinalIgnoreCase))
                return true;

            DTMAPI.Core.Manifesting.DiscoveredMod? discovered = runtime.DiscoveredMods
                .FirstOrDefault(mod => mod.Manifest.UniqueID.Equals(ownerId, StringComparison.OrdinalIgnoreCase));
            if (discovered == null)
                return true;
            if (discovered.OfficialEnabled)
                return true;

            message = "Owner " + ownerId + " is disabled by official enablement path " + FirstText(discovered.OfficialId, discovered.Source) + ". " + FirstText(discovered.EnablementReason, "Restart is required for DLL unload; GameBridge blocks runtime vehicle behavior immediately.");
            return false;
        }

        private static object? GetAgentController(Type? dolocApi)
        {
            object? gameStateManager = ReadStaticMember(dolocApi, "gameStateManager");
            return ReadStaticMember(dolocApi, "AgentController") ??
                (gameStateManager == null ? null : ReadMember(gameStateManager, "agentController"));
        }

        private MotorVehicleSummonResult SummonSecondMotor(IManifest? owner, SecondMotorRuntime vehicle, string source)
        {
            string ownerId = owner?.UniqueID ?? vehicle.OwnerUniqueId;
            var result = new MotorVehicleSummonResult
            {
                VehicleId = vehicle.Options.VehicleId,
                DisplayName = vehicle.Options.DisplayName,
                Before = BuildSecondMotorState(vehicle, "before-summon")
            };

            try
            {
                if (!IsOwnerOfficiallyEnabled(vehicle.OwnerUniqueId, out string enablementMessage))
                    return MotorVehicleSummonFailedWithCleanup(result, vehicle, "source-disabled", enablementMessage, "disabled summon");

                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                if (dolocApi == null)
                    return MotorVehicleSummonFailedWithCleanup(result, vehicle, "missing-dolocapi", "DolocAPI is not available.", "missing DolocAPI");
                if (!CanCallMotorInCurrentRoom(dolocApi, out string reason, out string message))
                    return MotorVehicleSummonFailedWithCleanup(result, vehicle, reason, message, "room rejected summon");
                if (!EnsureSecondMotorInstance(vehicle, out string ensureReason, out string ensureMessage))
                    return MotorVehicleSummonFailedWithCleanup(result, vehicle, ensureReason, ensureMessage, "ensure instance failed");

                object? currentRoom = ReadStaticMember(dolocApi, "CurrentRoom");
                object? target = BuildMotorSummonPositionNearAgent(1.15f);
                if (currentRoom == null || target == null)
                    return MotorVehicleSummonFailedWithCleanup(result, vehicle, "missing-position", "Current room or agent position was not available.", "missing summon position");

                vehicle.Room = currentRoom;
                vehicle.LastPosition = target;
                vehicle.LastFailureReason = string.Empty;
                vehicle.LastMessage = "Second motor summon requested source=" + source + " owner=" + ownerId + ".";

                MethodInfo? reset = FindMethodInHierarchy(vehicle.Controller!.GetType(), "Reset", 0);
                reset?.Invoke(vehicle.Controller, null);
                MethodInfo? setVisible = FindMethodInHierarchy(vehicle.Controller.GetType(), "SetVisible", 1);
                setVisible?.Invoke(vehicle.Controller, new object[] { true });
                if (!TryInvokeAutoFlyToAgent(vehicle.Controller))
                    SetMotorControllerPosition(vehicle.Controller, target);

                result.After = BuildSecondMotorState(vehicle, "after-summon");
                result.Success = true;
                result.Message = vehicle.LastMessage + " room=" + result.After.RoomId + " speedMultiplier=" + vehicle.Options.SpeedMultiplier.ToString("0.###") + ".";
                runtime.RuntimeMonitor.Log("Second motor summon OK vehicle=" + vehicle.Options.VehicleId + " key=" + vehicle.Options.KeyItemId + " " + result.Message);
                runtime.SetHookStatus("Vehicle.SecondMotorSummon", "experimental", "DTMAPI cloned MotorController + MotorController.AutoFlyTo", result.Message);
                RaiseVehicleEvent("summon", vehicle.Options.VehicleId, result.After, result.Message);
                return result;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Second motor summon failed.", ex.ToString());
                vehicle.LastFailureReason = ex.GetType().Name;
                vehicle.LastMessage = ex.Message;
                return MotorVehicleSummonFailedWithCleanup(result, vehicle, ex.GetType().Name, ex.Message, "summon exception");
            }
        }

        private bool EnsureSecondMotorInstance(SecondMotorRuntime vehicle, out string reason, out string message)
        {
            reason = string.Empty;
            message = string.Empty;
            if (vehicle.Controller != null)
                return true;

            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? originalMotor = ReadStaticMember(dolocApi, "Motor");
            if (originalMotor == null)
            {
                reason = "missing-original-motor";
                message = "DolocAPI.Motor is not available to clone.";
                return false;
            }

            object? clone = CloneUnityObject(originalMotor);
            if (clone == null)
            {
                reason = "clone-failed";
                message = "UnityEngine.Object.Instantiate failed for the original MotorController.";
                return false;
            }

            object? controller = originalMotor.GetType().IsInstanceOfType(clone) ? clone : GetComponent(clone, originalMotor.GetType());
            if (controller == null)
            {
                reason = "missing-cloned-controller";
                message = "The cloned object does not contain a MotorController component.";
                DestroyUnityObject(clone);
                return false;
            }

            object? gameObject = ReadMember(controller, "gameObject") ?? clone;
            SetMemberValue(gameObject, "name", "DTMAPI.SecondMotor." + vehicle.Options.VehicleId);
            SetMemberValue(controller, "isInitialized", false);
            MethodInfo? init = FindMethodInHierarchy(controller.GetType(), "Init", 0);
            init?.Invoke(controller, null);
            MethodInfo? reset = FindMethodInHierarchy(controller.GetType(), "Reset", 0);
            reset?.Invoke(controller, null);
            MethodInfo? setVisible = FindMethodInHierarchy(controller.GetType(), "SetVisible", 1);
            setVisible?.Invoke(controller, new object[] { false });

            object? interactable = ReadMember(controller, "motorInteractable");
            if (interactable == null)
            {
                Type? interactableType = ResolveType("DolocTown.MotorInteractable, Assembly-CSharp");
                if (interactableType != null)
                    interactable = GetComponentInChildren(gameObject, interactableType, includeInactive: true);
            }

            vehicle.Controller = controller;
            vehicle.GameObject = gameObject;
            vehicle.Interactable = interactable;
            secondMotorControllers.Add(controller);
            if (interactable != null)
                secondMotorInteractables.Add(interactable);

            string appearanceSummary = ApplySecondMotorScopedAppearance(vehicle);
            vehicle.LastMessage = "Cloned original MotorController for DTMAPI second motor; " + appearanceSummary + ".";
            runtime.RuntimeMonitor.Log(vehicle.LastMessage + " vehicle=" + vehicle.Options.VehicleId + " textureNote=" + vehicle.Options.TextureSourceNote);
            return true;
        }

        private string ApplySecondMotorScopedAppearance(SecondMotorRuntime vehicle)
        {
            if (vehicle.Controller == null || vehicle.GameObject == null)
            {
                vehicle.AppearanceSummary = "appearance=missing-controller";
                return vehicle.AppearanceSummary;
            }

            if (vehicle.Options.UseOriginalMotorVisuals)
            {
                vehicle.AppearanceSummary = "appearance=original-runtime-clone";
                return vehicle.AppearanceSummary;
            }

            object? color = CreateUnityColor(new DtmColor(SecondMotorScopedTintR, SecondMotorScopedTintG, SecondMotorScopedTintB, 1));
            if (color == null)
            {
                vehicle.AppearanceSummary = "appearance=custom-tint-failed reason=missing-unity-color";
                return vehicle.AppearanceSummary;
            }

            object? driverRenderer = ReadMember(vehicle.Controller, "driverRenderer");
            int total = 0;
            int skippedDriver = 0;
            int changed = 0;
            foreach (object renderer in GetSpriteRenderers(vehicle.GameObject, includeInactive: true))
            {
                total++;
                if (IsComponentUnder(renderer, driverRenderer))
                {
                    skippedDriver++;
                    continue;
                }

                if (SetMemberValue(renderer, "color", color))
                    changed++;
            }

            vehicle.AppearanceSummary = "appearance=instance-scoped-tint hex=" + SecondMotorScopedTintHex + " renderers=" + changed + "/" + total + " skippedDriver=" + skippedDriver;
            runtime.SetHookStatus("Vehicle.SecondMotorAppearance", changed > 0 ? "experimental" : "failed", "DTMAPI cloned MotorController SpriteRenderer.color", vehicle.AppearanceSummary);
            return vehicle.AppearanceSummary;
        }

        private bool TryResolveSecondMotorFromInteractable(object interactable, out SecondMotorRuntime vehicle)
        {
            vehicle = null!;
            if (interactable == null)
                return false;
            foreach (SecondMotorRuntime candidate in secondMotors.Values)
            {
                if (candidate.Interactable != null && ReferenceEquals(candidate.Interactable, interactable))
                {
                    vehicle = candidate;
                    return true;
                }
            }

            object? controller = ReadMember(interactable, "_motorController");
            if (controller == null)
                return false;
            SecondMotorRuntime? resolved = ResolveSecondMotorByController(controller);
            if (resolved == null)
                return false;
            vehicle = resolved;
            return true;
        }

        private SecondMotorRuntime? ResolveSecondMotorByController(object controller)
        {
            foreach (SecondMotorRuntime candidate in secondMotors.Values)
            {
                if (candidate.Controller != null && ReferenceEquals(candidate.Controller, controller))
                    return candidate;
            }
            return null;
        }

        private bool TryStartSecondMotorRide(SecondMotorRuntime vehicle, string reason)
        {
            try
            {
                if (!EnsureSecondMotorInstance(vehicle, out string ensureReason, out string ensureMessage))
                {
                    vehicle.LastFailureReason = ensureReason;
                    vehicle.LastMessage = ensureMessage;
                    return false;
                }

                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                if (dolocApi == null)
                {
                    vehicle.LastFailureReason = "missing-dolocapi";
                    vehicle.LastMessage = "DolocAPI is not available.";
                    return false;
                }

                if (!CanCallMotorInCurrentRoom(dolocApi, out string callReason, out string callMessage))
                {
                    vehicle.LastFailureReason = callReason;
                    vehicle.LastMessage = callMessage;
                    return false;
                }

                object? agentController = GetAgentController(dolocApi);
                if (agentController == null)
                {
                    vehicle.LastFailureReason = "missing-agent-controller";
                    vehicle.LastMessage = "DolocAPI.AgentController was not available.";
                    return false;
                }

                object? currentController = ReadMember(agentController, "motorController");
                if (originalAgentMotorController == null && currentController != null && !secondMotorControllers.Contains(currentController))
                    originalAgentMotorController = currentController;

                originalMotorSnapshotBeforeSecondRide = CaptureOriginalMotorSnapshot();
                if (!SetMemberValue(agentController, "motorController", vehicle.Controller!))
                {
                    vehicle.LastFailureReason = "controller-route-failed";
                    vehicle.LastMessage = "Could not route AgentControllerState.motorController to the DTMAPI second motor clone.";
                    return false;
                }

                activeSecondMotor = vehicle;
                MethodInfo? getOn = FindMethodInHierarchy(agentController.GetType(), "GetOnMotor", 0);
                if (getOn == null)
                {
                    RestoreOriginalAgentMotorController(agentController);
                    activeSecondMotor = null;
                    vehicle.LastFailureReason = "missing-get-on";
                    vehicle.LastMessage = "AgentControllerState.GetOnMotor was not found.";
                    return false;
                }

                getOn.Invoke(agentController, null);
                vehicle.IsRiding = true;
                vehicle.LastFailureReason = string.Empty;
                vehicle.LastMessage = "Second motor ride requested reason=" + reason + ".";
                runtime.RuntimeMonitor.Log("Second motor ride requested vehicle=" + vehicle.Options.VehicleId + " reason=" + reason + ".");
                RaiseVehicleEvent("ride-request", vehicle.Options.VehicleId, BuildSecondMotorState(vehicle, "ride-request"), vehicle.LastMessage);
                return true;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Second motor ride failed.", ex.ToString());
                vehicle.LastFailureReason = ex.GetType().Name;
                vehicle.LastMessage = ex.Message;
                return false;
            }
        }

        private void RestoreOriginalAgentMotorController(object? agentController)
        {
            if (agentController == null || originalAgentMotorController == null)
                return;
            SetMemberValue(agentController, "motorController", originalAgentMotorController);
        }

        private OriginalMotorSnapshot? CaptureOriginalMotorSnapshot()
        {
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? motor = ReadStaticMember(dolocApi, "Motor");
            object? archive = ReadStaticMember(dolocApi, "archiveHandle");
            object? farmData = archive == null ? null : ReadMember(archive, "farmData");
            object? agentData = farmData == null ? null : ReadMember(farmData, "agentData");
            object? motorData = agentData == null ? null : ReadMember(agentData, "motorData");
            object? room = motorData == null ? null : ReadMember(motorData, "CurrentRoom");
            room ??= ReadStaticMember(dolocApi, "CurrentRoom");
            object? position = motorData == null ? null : ReadMember(motorData, "position");
            position ??= motor == null ? null : ReadMember(motor, "position2d");
            bool unlocked = motorData != null && ReadBoolMember(motorData, "isUnlocked", false);
            if (motor == null || position == null)
                return null;
            return new OriginalMotorSnapshot(room, position, unlocked);
        }

        private void RestoreOriginalMotorSnapshot(bool clearSnapshot)
        {
            if (originalMotorSnapshotBeforeSecondRide == null || !originalMotorSnapshotBeforeSecondRide.Unlocked)
                return;
            try
            {
                restoringOriginalMotorSnapshot = true;
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                MethodInfo? setMotorPosition = dolocApi?.GetMethod("SetMotorPosition", BindingFlags.Public | BindingFlags.Static);
                if (setMotorPosition != null && originalMotorSnapshotBeforeSecondRide.Position != null)
                    setMotorPosition.Invoke(null, new[] { originalMotorSnapshotBeforeSecondRide.Room, originalMotorSnapshotBeforeSecondRide.Position });
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Failed to restore original motor snapshot after second-motor ride.", ex.ToString());
            }
            finally
            {
                restoringOriginalMotorSnapshot = false;
                if (clearSnapshot)
                    originalMotorSnapshotBeforeSecondRide = null;
            }
        }

        private MotorVehicleState BuildOriginalMotorState(string source)
        {
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? motor = ReadStaticMember(dolocApi, "Motor");
            object? archive = ReadStaticMember(dolocApi, "archiveHandle");
            object? farmData = archive == null ? null : ReadMember(archive, "farmData");
            object? agentData = farmData == null ? null : ReadMember(farmData, "agentData");
            object? motorData = agentData == null ? null : ReadMember(agentData, "motorData");
            object? room = motorData == null ? null : ReadMember(motorData, "CurrentRoom");
            room ??= ReadStaticMember(dolocApi, "CurrentRoom");
            object? position = motorData == null ? null : ReadMember(motorData, "position");
            position ??= motor == null ? null : ReadMember(motor, "position");
            object? globalParameter = ReadStaticMember(dolocApi, "GlobalParameter");
            double maxSpeed = globalParameter == null ? 0 : ReadDoubleMember(globalParameter, "MotorHorizontalMaxSpeed", 0);
            bool unlocked = motorData != null && ReadBoolMember(motorData, "isUnlocked", false);
            bool riding = activeSecondMotor == null && ReadStaticBoolMember(dolocApi, "IsAgentRiding", false);
            CanCallMotorInCurrentRoom(dolocApi, out string failureReason, out string message);
            return new MotorVehicleState
            {
                VehicleId = "doloc.original_motor",
                DisplayName = "Original Motor",
                IsOriginalMotor = true,
                IsRegistered = true,
                IsUnlocked = unlocked,
                IsVisible = motor != null && ReadBoolMember(motor, "isVisible", false),
                IsRiding = riding,
                IsAvailableInCurrentRoom = string.IsNullOrWhiteSpace(failureReason),
                RoomId = ReadStringMemberOrEmpty(room, "RoomId"),
                RoomTitle = BuildRoomTitle(room),
                X = ReadVectorComponent(position, "x"),
                Y = ReadVectorComponent(position, "y"),
                Z = ReadVectorComponent(position, "z"),
                EnduranceProgress = motor == null ? 0 : ReadDoubleMember(motor, "EnduranceProgress", 0),
                BaseMaxSpeed = maxSpeed,
                EffectiveMaxSpeed = maxSpeed,
                SpeedMultiplier = 1,
                KeyItemId = "motor_key",
                LastFailureReason = failureReason,
                LastMessage = FirstText(message, "Original motor state source=" + source + ".")
            };
        }

        private MotorVehicleState BuildSecondMotorState(SecondMotorRuntime vehicle, string source)
        {
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? position = vehicle.Controller == null ? vehicle.LastPosition : ReadMember(vehicle.Controller, "position");
            object? globalParameter = ReadStaticMember(dolocApi, "GlobalParameter");
            double maxSpeed = globalParameter == null ? 0 : ReadDoubleMember(globalParameter, "MotorHorizontalMaxSpeed", 0);
            CanCallMotorInCurrentRoom(dolocApi, out string failureReason, out string message);
            return new MotorVehicleState
            {
                VehicleId = vehicle.Options.VehicleId,
                OwnerUniqueId = vehicle.OwnerUniqueId,
                DisplayName = vehicle.Options.DisplayName,
                IsOriginalMotor = false,
                IsRegistered = true,
                IsUnlocked = true,
                IsVisible = vehicle.Controller != null && ReadBoolMember(vehicle.Controller, "isVisible", false),
                IsRiding = vehicle.IsRiding,
                IsAvailableInCurrentRoom = string.IsNullOrWhiteSpace(failureReason),
                RoomId = ReadStringMemberOrEmpty(vehicle.Room, "RoomId"),
                RoomTitle = BuildRoomTitle(vehicle.Room),
                X = ReadVectorComponent(position, "x"),
                Y = ReadVectorComponent(position, "y"),
                Z = ReadVectorComponent(position, "z"),
                EnduranceProgress = vehicle.Controller == null ? 1 : ReadDoubleMember(vehicle.Controller, "EnduranceProgress", 1),
                BaseMaxSpeed = maxSpeed,
                EffectiveMaxSpeed = maxSpeed * vehicle.Options.SpeedMultiplier,
                SpeedMultiplier = vehicle.Options.SpeedMultiplier,
                KeyItemId = vehicle.Options.KeyItemId,
                LastFailureReason = FirstText(vehicle.LastFailureReason, failureReason),
                LastMessage = FirstText(vehicle.LastMessage, message, "Second motor state source=" + source + ".")
            };
        }

        private bool CanCallMotorInCurrentRoom(Type? dolocApi, out string reason, out string message)
        {
            reason = string.Empty;
            message = string.Empty;
            object? currentRoom = ReadStaticMember(dolocApi, "CurrentRoom");
            if (currentRoom == null)
            {
                reason = "missing-current-room";
                message = "Current room is not available.";
                return false;
            }

            if (ReadStaticBoolMember(dolocApi, "IsAgentRiding", false))
            {
                reason = "already-riding";
                message = "Cannot summon or switch motors while already riding.";
                return false;
            }

            object? baseProto = ReadMember(currentRoom, "baseProto");
            bool isInHouse = ReadBoolMember(currentRoom, "IsInHouse", false) || (baseProto != null && ReadBoolMember(baseProto, "isInHouse", false));
            bool disableMotor = ReadBoolMember(currentRoom, "DisableMotor", false);
            if (isInHouse || disableMotor)
            {
                reason = isInHouse ? "in-house" : "disabled-room";
                message = "The current room disallows motor summon.";
                return false;
            }

            return true;
        }

        private object? BuildMotorSummonPositionNearAgent(float xOffset)
        {
            object? center = ReadAgentPositionCenterObject();
            if (center == null)
                return null;
            return CreateUnityVector2(ReadVectorComponent(center, "x") + xOffset, ReadVectorComponent(center, "y"));
        }

        private bool TryInvokeAutoFlyToAgent(object? motor)
        {
            if (motor == null)
                return false;
            MethodInfo? autoFlyTo = FindMethodInHierarchy(motor.GetType(), "AutoFlyTo", 2);
            if (autoFlyTo == null)
                return false;
            Type delegateType = autoFlyTo.GetParameters()[0].ParameterType;
            Delegate? getter = CreateAgentPositionGetterDelegate(delegateType);
            if (getter == null)
                return false;
            autoFlyTo.Invoke(motor, new object?[] { getter, null });
            return true;
        }

        private static void SetMotorControllerPosition(object controller, object position2d)
        {
            double x = ReadVectorComponent(position2d, "x");
            double y = ReadVectorComponent(position2d, "y");
            double z = ReadVectorComponent(ReadMember(controller, "position"), "z");
            if (double.IsNaN(z))
                z = 0;
            object? position3d = CreateUnityVector3(x, y, z);
            if (position3d != null)
                SetMemberValue(controller, "position", position3d);
            SetMemberValue(controller, "position2d", position2d);
            object? rb = ReadMember(controller, "rb");
            if (rb != null)
            {
                SetMemberValue(rb, "position", position2d);
                object? zero = CreateUnityVector2(0, 0);
                if (zero != null)
                    SetMemberValue(rb, "velocity", zero);
            }
            MethodInfo? clearVelocity = FindMethodInHierarchy(controller.GetType(), "ClearVelocity", 0);
            clearVelocity?.Invoke(controller, null);
        }

        private static Delegate? CreateAgentPositionGetterDelegate(Type delegateType)
        {
            MethodInfo? method = typeof(DolocTownExperimentalBridgeApi).GetMethod(nameof(ReadAgentPositionCenterObject), BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo? invoke = delegateType.GetMethod("Invoke");
            Type? returnType = invoke?.ReturnType;
            if (method == null || returnType == null)
                return null;
            Expression body = Expression.Convert(Expression.Call(method), returnType);
            return Expression.Lambda(delegateType, body).Compile();
        }

        private static object? ReadAgentPositionCenterObject()
        {
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? agent = ReadStaticMember(dolocApi, "agent") ?? ReadStaticMember(dolocApi, "Agent");
            object? center = agent == null ? null : ReadMember(agent, "PositionCenter");
            if (center != null)
                return center;
            object? position = ReadStaticMember(dolocApi, "AgentPosition");
            if (position == null)
                return null;
            return CreateUnityVector2(ReadVectorComponent(position, "x"), ReadVectorComponent(position, "y"));
        }

        private static string ReadStringMemberOrEmpty(object? instance, string name)
        {
            return instance == null ? string.Empty : ReadStringMember(instance, name);
        }

        private static string BuildRoomTitle(object? room)
        {
            if (room == null)
                return string.Empty;
            object? sceneConfig = ReadMember(room, "SceneConfig");
            return FirstText(ReadStringMemberOrEmpty(sceneConfig, "Title"), ReadStringMemberOrEmpty(room, "RoomId"), ReadStringMemberOrEmpty(room, "SceneRawName"));
        }

        private GlobalMotorTuningSnapshot CaptureGlobalMotorTuning(object globalParameter)
        {
            return new GlobalMotorTuningSnapshot(
                ReadDoubleMember(globalParameter, "MotorHorizontalAcceleration", 0),
                ReadDoubleMember(globalParameter, "MotorHorizontalRevertAcceleration", 0),
                ReadDoubleMember(globalParameter, "MotorHorizontalDeceleration", 0),
                ReadDoubleMember(globalParameter, "MotorHorizontalMaxSpeed", 0));
        }

        private static void RestoreGlobalMotorTuning(object globalParameter, GlobalMotorTuningSnapshot snapshot)
        {
            WriteFloatMember(globalParameter, "MotorHorizontalAcceleration", (float)snapshot.MotorHorizontalAcceleration);
            WriteFloatMember(globalParameter, "MotorHorizontalRevertAcceleration", (float)snapshot.MotorHorizontalRevertAcceleration);
            WriteFloatMember(globalParameter, "MotorHorizontalDeceleration", (float)snapshot.MotorHorizontalDeceleration);
            WriteFloatMember(globalParameter, "MotorHorizontalMaxSpeed", (float)snapshot.MotorHorizontalMaxSpeed);
        }

        private void RaiseVehicleEvent(string eventType, string vehicleId, MotorVehicleState state, string message)
        {
            VehicleChanged?.Invoke(this, new MotorVehicleEventArgs
            {
                EventType = eventType ?? string.Empty,
                VehicleId = vehicleId ?? string.Empty,
                State = state ?? new MotorVehicleState(),
                Message = message ?? string.Empty
            });
        }

        private static void CountSecondMotorScopedTintRenderers(object? controller, out int total, out int tinted, out int skippedDriver)
        {
            total = 0;
            tinted = 0;
            skippedDriver = 0;
            if (controller == null)
                return;

            object? gameObject = ReadMember(controller, "gameObject");
            if (gameObject == null)
                return;

            object? driverRenderer = ReadMember(controller, "driverRenderer");
            foreach (object renderer in GetSpriteRenderers(gameObject, includeInactive: true))
            {
                total++;
                if (IsComponentUnder(renderer, driverRenderer))
                {
                    skippedDriver++;
                    continue;
                }

                if (IsSecondMotorScopedTintColor(ReadMember(renderer, "color")))
                    tinted++;
            }
        }

        private static bool IsSecondMotorScopedTintColor(object? color)
        {
            if (color == null)
                return false;

            return Math.Abs(ReadDoubleMember(color, "r", -1) - SecondMotorScopedTintR) <= 0.025 &&
                Math.Abs(ReadDoubleMember(color, "g", -1) - SecondMotorScopedTintG) <= 0.025 &&
                Math.Abs(ReadDoubleMember(color, "b", -1) - SecondMotorScopedTintB) <= 0.025;
        }

        private sealed class SecondMotorRuntime
        {
            public SecondMotorRuntime(string ownerUniqueId, SecondMotorOptions options)
            {
                OwnerUniqueId = ownerUniqueId;
                Options = options;
            }

            public string OwnerUniqueId { get; set; }
            public SecondMotorOptions Options { get; set; }
            public object? GameObject { get; set; }
            public object? Controller { get; set; }
            public object? Interactable { get; set; }
            public object? Room { get; set; }
            public object? LastPosition { get; set; }
            public object? PendingTransitionRoom { get; set; }
            public string PendingTransitionRoomId { get; set; } = string.Empty;
            public object? PendingTransitionPosition { get; set; }
            public bool IsRiding { get; set; }
            public string LastFailureReason { get; set; } = string.Empty;
            public string LastMessage { get; set; } = string.Empty;
            public string AppearanceSummary { get; set; } = string.Empty;
        }

        private sealed class OriginalMotorSnapshot
        {
            public OriginalMotorSnapshot(object? room, object position, bool unlocked)
            {
                Room = room;
                Position = position;
                Unlocked = unlocked;
            }

            public object? Room { get; }
            public object Position { get; }
            public bool Unlocked { get; }
        }

        private sealed class GlobalMotorTuningSnapshot
        {
            public GlobalMotorTuningSnapshot(double motorHorizontalAcceleration, double motorHorizontalRevertAcceleration, double motorHorizontalDeceleration, double motorHorizontalMaxSpeed)
            {
                MotorHorizontalAcceleration = motorHorizontalAcceleration;
                MotorHorizontalRevertAcceleration = motorHorizontalRevertAcceleration;
                MotorHorizontalDeceleration = motorHorizontalDeceleration;
                MotorHorizontalMaxSpeed = motorHorizontalMaxSpeed;
            }

            public double MotorHorizontalAcceleration { get; }
            public double MotorHorizontalRevertAcceleration { get; }
            public double MotorHorizontalDeceleration { get; }
            public double MotorHorizontalMaxSpeed { get; }
        }
    }
}
