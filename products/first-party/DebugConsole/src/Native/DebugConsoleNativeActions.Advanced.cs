using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using Native = DTMAPI.DebugConsole.DebugConsoleNativeAccess;

namespace DTMAPI.DebugConsole
{
    internal sealed partial class DebugConsoleNativeActions
    {
        public IReadOnlyList<TechPointDebugOption> GetTechPointOptions()
        {
            try
            {
                Type? type = Native.Resolve(
                    "DolocTown.Config.TechTree.TechPointType, Assembly-CSharp");
                if (type?.IsEnum != true)
                    return Array.Empty<TechPointDebugOption>();
                return Enum.GetValues(type)
                    .Cast<object>()
                    .Select(value =>
                    {
                        ReadTechPoint(value, out int points, out int level);
                        string id = value.ToString() ?? string.Empty;
                        return new TechPointDebugOption
                        {
                            Id = id,
                            DisplayName = id,
                            CurrentPoints = points,
                            CurrentLevel = level
                        };
                    })
                    .OrderBy(value => value.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            catch (Exception error)
            {
                runtime.Error("tech-point-options", error);
                return Array.Empty<TechPointDebugOption>();
            }
        }

        public IReadOnlyList<SpawnDebugOption> GetMonsterOptions()
            => GetMonsterCatalog()
                .Select(option => new SpawnDebugOption
                {
                    Id = option.Id,
                    DisplayName = option.DisplayName,
                    Category = option.Category,
                    IsAvailableInCurrentRoom = option.IsAvailable
                })
                .ToArray();

        public IReadOnlyList<SpawnDebugOption> GetResourceOptions()
        {
            try
            {
                Type? api = Native.Resolve("DolocAPI, Assembly-CSharp");
                bool available =
                    CurrentRoomImplements(
                        "DolocTown.IDungeonResourceHost") &&
                    Native.Method(
                        api,
                        "Command_ReplaceResource",
                        4,
                        true) != null;
                return Native.Enumerate(Native.TableList("TbResource"))
                    .Select(proto => new SpawnDebugOption
                    {
                        Id = Native.TextFirst(
                            proto,
                            "Id",
                            "id",
                            "Name"),
                        DisplayName = Native.First(
                            Native.Text(proto, "Title"),
                            Native.Text(proto, "Id")),
                        Category = Native.First(
                            Native.Read(proto, "ResourceClass")?.ToString() ?? string.Empty,
                            Native.Read(proto, "ResourceType")?.ToString() ?? string.Empty,
                            "resource"),
                        IsAvailableInCurrentRoom = available
                    })
                    .Where(option => option.Id.Length > 0)
                    .OrderBy(option => option.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .Take(80)
                    .ToArray();
            }
            catch (Exception error)
            {
                runtime.Error("resource-options", error);
                return Array.Empty<SpawnDebugOption>();
            }
        }

        public CreativeModeState GetCreativeModeState() =>
            new CreativeModeState
            {
                Enabled = creativeEnabled,
                RuntimeHooksInstalled = DebugConsoleCreativeHooks.Installed,
                GeneratorRuntimeAvailable = ItemAvailable("dtmapi_creative_generator"),
                GeneratorItemId = "dtmapi_creative_generator",
                LastMessage = creativeEnabled
                    ? "Creative ProductNative lease is active."
                    : "Creative ProductNative lease is inactive."
            };

        public TimeSkipResult AdvanceTime(
            IManifest owner,
            AdvancedTimeAdvanceKind kind,
            int amount)
        {
            amount = Math.Max(1, Math.Min(52, amount));
            object? global = Native.Read(
                Native.Resolve("DolocAPI, Assembly-CSharp"),
                "GlobalParameter");
            int hoursPerDay = Math.Max(1, Native.Int(global, "Day2Hour", 24));
            int minutesPerHour = Math.Max(1, Native.Int(global, "Hour2Min", 60));
            int days = kind == AdvancedTimeAdvanceKind.Week
                ? amount * 7
                : kind == AdvancedTimeAdvanceKind.Month
                    ? amount * 28
                    : amount;
            int minutes = checked(days * hoursPerDay * minutesPerHour);
            return AdvanceMinutes(
                owner,
                minutes,
                kind.ToString().ToLowerInvariant(),
                GetTimeState().Hour);
        }

        public TimeScaleDebugResult SetTimeScale(
            IManifest owner,
            double multiplier)
        {
            multiplier = Math.Min(16d, Math.Max(0.1d, multiplier));
            var result = new TimeScaleDebugResult
            {
                RequestedMultiplier = multiplier,
                BeforeMultiplier = timeScaleMultiplier
            };
            try
            {
                string ownerId = owner?.UniqueID ?? string.Empty;
                if (timeScaleLeaseActive &&
                    !timeScaleLeaseOwnerId.Equals(ownerId, StringComparison.OrdinalIgnoreCase))
                {
                    TimeScaleDebugResult previous =
                        ResetTimeScaleCore("lease replaced by " + ownerId);
                    if (!previous.Success)
                    {
                        return Fail(
                            result,
                            "previous-lease-restore-" + previous.FailureReason,
                            previous.Message);
                    }
                }
                if (!TryReadNativeTimeScale(
                        out double current,
                        out string failure,
                        out string message))
                    return Fail(result, failure, message);
                if (timeScaleLeaseActive &&
                    timeScaleMutationUncertain)
                {
                    return Fail(
                        result,
                        "time-scale-restore-pending",
                        "A previous time-scale mutation has uncertain native state; restore the retained lease before applying another multiplier.");
                }
                if (timeScaleLeaseActive &&
                    !NearlyEqual(current, timeScaleAppliedMultiplier))
                {
                    return Fail(
                        result,
                        "time-scale-foreign-mutation",
                        "Native currentTimeScale changed outside DebugConsole; refusing to overwrite the foreign value.");
                }
                bool acquiring = !timeScaleLeaseActive;
                if (acquiring)
                {
                    timeScaleOriginalMultiplier = current;
                    timeScaleLeaseOwnerId = ownerId;
                    timeScaleLeaseActive = true;
                }
                double previousApplied = acquiring
                    ? current
                    : timeScaleAppliedMultiplier;
                double previousMultiplier = timeScaleMultiplier;
                timeScalePriorAppliedMultiplier =
                    previousApplied;
                timeScaleAppliedMultiplier = multiplier;
                timeScaleMultiplier = multiplier;
                timeScaleMutationUncertain = true;
                if (!TrySetNativeTimeScale(
                        multiplier,
                        showTip: true,
                        out failure,
                        out message))
                {
                    TryRollbackTimeScaleMutation(
                        previousApplied,
                        previousMultiplier,
                        clearLeaseWhenRestored: acquiring,
                        ref failure,
                        ref message);
                    return Fail(result, failure, message);
                }
                if (!TryReadNativeTimeScale(
                        out double observed,
                        out failure,
                        out message) ||
                    !NearlyEqual(observed, multiplier))
                {
                    failure = string.IsNullOrWhiteSpace(failure)
                        ? "time-scale-apply-not-observed"
                        : failure;
                    message = string.IsNullOrWhiteSpace(message)
                        ? "Native currentTimeScale did not retain the requested multiplier."
                        : message;
                    TryRollbackTimeScaleMutation(
                        previousApplied,
                        previousMultiplier,
                        clearLeaseWhenRestored: acquiring,
                        ref failure,
                        ref message);
                    return Fail(result, failure, message);
                }
                timeScaleMutationUncertain = false;
                result.AfterMultiplier = multiplier;
                result.Success = true;
                result.Message = "Time-scale lease owner=" +
                    (owner?.UniqueID ?? "unknown") +
                    " multiplier=" +
                    multiplier.ToString("0.###", CultureInfo.InvariantCulture) + ".";
                LogMutation("time-scale-lease", true, result.Message);
                return result;
            }
            catch (Exception error)
            {
                runtime.Error("time-scale-set", error);
                return Fail(result, error.GetType().Name, error.Message);
            }
        }

        public TimeScaleDebugResult ResetTimeScale(
            IManifest owner,
            string reason) =>
            ResetTimeScaleCore(reason);

        public DebugValueResult AddMoney(IManifest owner, int amount)
        {
            amount = Math.Max(1, Math.Min(100000, amount));
            var result = new DebugValueResult
            {
                ValueId = "money",
                RequestedDelta = amount
            };
            try
            {
                object? archive = Native.Archive;
                Type? api = Native.Resolve("DolocAPI, Assembly-CSharp");
                if (archive == null || api == null)
                    return Fail(result, "missing-archive", "Native archive is unavailable.");
                result.BeforeValue = Native.Int(archive, "CurrentMoney");
                MethodInfo? command = Native.Method(api, "Command_AddMoney", 1, true);
                if (command != null)
                    command.Invoke(null, new object[] { amount });
                else if (!Native.Write(archive, "CurrentMoney", result.BeforeValue + amount))
                    return Fail(result, "missing-money-owner", "Native money command and CurrentMoney setter are unavailable.");
                result.AfterValue = Native.Int(
                    archive,
                    "CurrentMoney",
                    result.BeforeValue + amount);
                result.Success = result.AfterValue >= result.BeforeValue + amount;
                result.Message = "Money +" + amount +
                    " before=" + result.BeforeValue +
                    " after=" + result.AfterValue + ".";
                LogMutation("money", result.Success, result.Message);
                return result;
            }
            catch (Exception error)
            {
                runtime.Error("money", error);
                return Fail(result, error.GetType().Name, error.Message);
            }
        }

        public DebugValueResult AddTechPoint(
            IManifest owner,
            string pointTypeId,
            int amount)
        {
            pointTypeId = (pointTypeId ?? string.Empty).Trim();
            amount = Math.Max(1, Math.Min(1000, amount));
            var result = new DebugValueResult
            {
                ValueId = pointTypeId,
                RequestedDelta = amount
            };
            try
            {
                Type? api = Native.Resolve("DolocAPI, Assembly-CSharp");
                Type? type = Native.Resolve(
                    "DolocTown.Config.TechTree.TechPointType, Assembly-CSharp");
                if (api == null || type?.IsEnum != true)
                    return Fail(result, "missing-tech-point-owner", "Native tech-point owner is unavailable.");
                if (!Enum.GetNames(type).Any(name =>
                        name.Equals(pointTypeId, StringComparison.OrdinalIgnoreCase)))
                    return Fail(result, "not-whitelisted", "Unknown tech-point type.");
                object value = Enum.Parse(type, pointTypeId, true);
                ReadTechPoint(value, out int before, out _);
                result.BeforeValue = before;
                MethodInfo? add = ResolveAddTechPointMethod(api);
                if (add == null)
                    return Fail(result, "missing-add-tech-point", "DolocAPI.AddTechPoint is unavailable.");
                add.Invoke(null, new[] { value, (object)amount });
                ReadTechPoint(value, out int after, out _);
                result.AfterValue = after;
                result.Success = after >= before + amount;
                result.Message = pointTypeId + " +" + amount +
                    " before=" + before +
                    " after=" + after + ".";
                LogMutation("tech-point", result.Success, result.Message);
                return result;
            }
            catch (Exception error)
            {
                runtime.Error("tech-point", error);
                return Fail(result, error.GetType().Name, error.Message);
            }
        }

        public DebugCommandResult UnlockAllTechTrees(IManifest owner)
        {
            var result = new DebugCommandResult
            {
                CommandId = "unlock-all-tech-trees"
            };
            try
            {
                object? farm = Native.Read(Native.Archive, "farmData");
                object? unlocked = Native.Read(farm, "unlockedTechTree");
                if (unlocked == null)
                    return Fail(result, "missing-tech-tree-owner", "Native unlockedTechTree collection is unavailable.");
                int before = Native.Count(unlocked);
                foreach (object proto in Native.Enumerate(Native.TableList("TbTechTree")))
                {
                    string id = Native.TextFirst(proto, "Id", "id");
                    if (id.Length > 0 && Native.Add(unlocked, id))
                        result.AffectedCount++;
                }
                result.Success = result.AffectedCount > 0 ||
                    Native.Count(unlocked) >= before;
                result.Message = "Tech trees affected=" +
                    result.AffectedCount + ".";
                LogMutation("unlock-tech-trees", result.Success, result.Message);
                return result;
            }
            catch (Exception error)
            {
                runtime.Error("unlock-tech-trees", error);
                return Fail(result, error.GetType().Name, error.Message);
            }
        }

        public CropMaturityResult MatureAllCrops(IManifest owner)
        {
            var result = new CropMaturityResult();
            try
            {
                foreach (object equipment in EnumerateEquipments(Native.CurrentRoom))
                {
                    string name = equipment.GetType().FullName ??
                        equipment.GetType().Name;
                    if (name.IndexOf("PlantBasin", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    result.PlantBasinsVisited++;
                    object? crop = Native.Read(equipment, "Crop") ??
                        (name.IndexOf("PlantBasinGrass", StringComparison.OrdinalIgnoreCase) >= 0
                            ? equipment
                            : null);
                    if (crop != null && TryMature(crop))
                        result.CropsMatured++;
                }
                result.Success = result.PlantBasinsVisited > 0;
                result.FailureReason = result.Success ? string.Empty : "no-plant-basins";
                result.Message = "Matured " + result.CropsMatured +
                    " of " + result.PlantBasinsVisited + " PlantBasin crops.";
                LogMutation("mature-crops", result.Success, result.Message);
                return result;
            }
            catch (Exception error)
            {
                runtime.Error("mature-crops", error);
                return Fail(result, error.GetType().Name, error.Message);
            }
        }

        public CreativeModeResult SetCreativeMode(
            IManifest owner,
            bool enabled)
        {
            var result = new CreativeModeResult
            {
                Enabled = enabled,
                Before = GetCreativeModeState()
            };
            try
            {
                bool changed = enabled
                    ? EnableCreative(owner?.UniqueID ?? string.Empty)
                    : RestoreCreative("UI toggle", out _);
                result.After = GetCreativeModeState();
                result.Success = changed;
                result.FailureReason = changed
                    ? string.Empty
                    : "native-creative-lease-failed";
                result.Message = enabled
                    ? "Creative " + runtime.NativeOwnerLabel + " lease enabled."
                    : "Creative " + runtime.NativeOwnerLabel + " lease restored.";
                LogMutation("creative-lease", result.Success, result.Message);
                return result;
            }
            catch (Exception error)
            {
                runtime.Error("creative-lease", error);
                result.After = GetCreativeModeState();
                result.FailureReason = error.GetType().Name;
                result.Message = error.Message;
                return result;
            }
        }

        public InventoryGiveResult GiveCreativeGenerator(IManifest owner) =>
            GiveItem(owner, "dtmapi_creative_generator", 1);

        public SpawnActionResult SpawnMonster(
            IManifest owner,
            string monsterId,
            int count) =>
            SpawnMonsterNative(owner, monsterId, count);

        public SpawnDebugResult SpawnResource(
            IManifest owner,
            string resourceId,
            int count)
        {
            resourceId = (resourceId ?? string.Empty).Trim();
            count = Math.Max(1, Math.Min(10, count));
            var result = new SpawnDebugResult
            {
                SpawnId = resourceId,
                RequestedCount = count
            };
            try
            {
                SpawnDebugOption? allowed = GetResourceOptions()
                    .FirstOrDefault(option =>
                        option.Id.Equals(resourceId, StringComparison.OrdinalIgnoreCase) &&
                        option.IsAvailableInCurrentRoom);
                object? room = Native.CurrentRoom;
                if (allowed == null || room == null)
                    return Fail(result, "not-whitelisted-or-unsupported-room", "Resource is unavailable in the current room.");
                Type? api = Native.Resolve(
                    "DolocAPI, Assembly-CSharp");
                MethodInfo? replace = Native.Method(
                    api,
                    "Command_ReplaceResource",
                    4,
                    true);
                if (replace == null)
                    return Fail(result, "missing-resource-owner", "Native resource creation owner is unavailable.");
                int baseX = (int)Math.Round(Native.Vector(Native.AgentPosition, "x"));
                int baseY = (int)Math.Round(Native.Vector(Native.AgentPosition, "y"));
                var replaceArguments =
                    new object?[] { resourceId, 0, 0, null };
                for (int index = 0; index < count; index++)
                {
                    replaceArguments[1] = baseX + index % 5;
                    replaceArguments[2] = baseY + index / 5;
                    replace.Invoke(null, replaceArguments);
                    result.SpawnedCount++;
                }
                result.DisplayName = allowed.DisplayName;
                result.Success = result.SpawnedCount > 0;
                result.FailureReason = result.Success
                    ? string.Empty
                    : "native-create-returned-null";
                result.Message = "Spawned resource " + resourceId +
                    " count=" + result.SpawnedCount + ".";
                LogMutation("spawn-resource", result.Success, result.Message);
                return result;
            }
            catch (Exception error)
            {
                runtime.Error("spawn-resource", error);
                return Fail(result, error.GetType().Name, error.Message);
            }
        }

        BridgeFeatureStatus IAdvancedActions.GetStatus() =>
            Status(runtime.NativeOwnerLabel +
                " fixed advanced-action allowlist; no parser or arbitrary native command surface.");

        private TimeScaleDebugResult ResetTimeScaleCore(string reason)
        {
            var result = new TimeScaleDebugResult
            {
                RequestedMultiplier = timeScaleLeaseActive
                    ? timeScaleOriginalMultiplier
                    : 1d,
                BeforeMultiplier = timeScaleMultiplier
            };
            try
            {
                if (timeScaleLeaseActive)
                {
                    if (!TryReadNativeTimeScale(
                            out double current,
                            out string failure,
                            out string message))
                        return Fail(result, failure, message);
                    if (!NearlyEqual(current, timeScaleAppliedMultiplier) &&
                        !(timeScaleMutationUncertain &&
                          NearlyEqual(
                              current,
                              timeScalePriorAppliedMultiplier)) &&
                        !NearlyEqual(current, timeScaleOriginalMultiplier))
                    {
                        return Fail(
                            result,
                            "time-scale-foreign-mutation",
                            "Native currentTimeScale changed outside DebugConsole; exact original restoration remains pending.");
                    }
                    if (!NearlyEqual(current, timeScaleOriginalMultiplier))
                    {
                        if (!TrySetNativeTimeScale(
                                timeScaleOriginalMultiplier,
                                showTip: false,
                                out failure,
                                out message))
                            return Fail(result, failure, message);
                        if (!TryReadNativeTimeScale(
                                out double restored,
                                out failure,
                                out message) ||
                            !NearlyEqual(restored, timeScaleOriginalMultiplier))
                        {
                            return Fail(
                                result,
                                string.IsNullOrWhiteSpace(failure)
                                    ? "time-scale-restore-not-observed"
                                    : failure,
                                string.IsNullOrWhiteSpace(message)
                                    ? "Native currentTimeScale did not retain the exact original multiplier."
                                    : message);
                        }
                    }
                    result.AfterMultiplier = timeScaleOriginalMultiplier;
                    ClearTimeScaleLease();
                }
                else
                {
                    result.AfterMultiplier = 1d;
                }
                result.Success = true;
                result.Message =
                    "Time-scale lease restored to exact native original reason=" +
                    reason + ".";
                return result;
            }
            catch (Exception error)
            {
                runtime.Error("time-scale-reset", error);
                return Fail(result, error.GetType().Name, error.Message);
            }
        }

        private bool EnableCreative(string ownerId)
        {
            if ((creativeEnabled || creativeSnapshotValid) &&
                !creativeLeaseOwnerId.Equals(ownerId, StringComparison.OrdinalIgnoreCase))
            {
                if (!RestoreCreative(
                        "lease replaced by " + ownerId,
                        out _))
                    return false;
            }
            object? config = GetGameInitConfig();
            if (config == null)
                return false;
            if (!creativeSnapshotValid)
            {
                if (!TryReadCreativeFlags(
                        config,
                        out originalIgnoreMaterialCost,
                        out originalSkipMoneyVerify,
                        out originalIgnoreSpiritCost,
                        out _))
                    return false;
                creativeSnapshotValid = true;
                creativeLeaseOwnerId = ownerId ?? string.Empty;
            }
            creativeAttemptedMaterial = true;
            creativeAttemptedShop = true;
            creativeAttemptedSpirit = true;
            creativeMutationUncertain = true;
            if (!TryWriteCreativeFlags(
                    config,
                    true,
                    true,
                    true,
                    out _,
                    out bool rollbackVerified))
            {
                if (rollbackVerified)
                    ClearCreativeLease();
                return false;
            }
            creativeMutationUncertain = false;
            try
            {
                runtime.SetCreativeHookDemand(true);
                DebugConsoleCreativeHooks.Enable();
                creativeEnabled = true;
                return true;
            }
            catch
            {
                bool restored = TryWriteCreativeFlags(
                    config,
                    originalIgnoreMaterialCost,
                    originalSkipMoneyVerify,
                    originalIgnoreSpiritCost,
                    out _,
                    out _);
                DebugConsoleCreativeHooks.Disable();
                if (restored)
                    ClearCreativeLease();
                else
                    creativeMutationUncertain = true;
                throw;
            }
        }

        private bool RestoreCreative(
            string reason,
            out string failure)
        {
            failure = string.Empty;
            if (!creativeSnapshotValid)
            {
                DebugConsoleCreativeHooks.Disable();
                try
                {
                    runtime.SetCreativeHookDemand(false);
                }
                catch (Exception error)
                {
                    failure = error.GetType().Name + ": " + error.Message;
                    return false;
                }
                ClearCreativeLease();
                return true;
            }
            object? config = GetGameInitConfig();
            if (config == null)
            {
                failure = "GameInitConfig is unavailable.";
                return false;
            }
            if (!TryReadCreativeFlags(
                    config,
                    out bool currentMaterial,
                    out bool currentShop,
                    out bool currentSpirit,
                    out failure))
                return false;
            bool currentIsApplied =
                currentMaterial == creativeAttemptedMaterial &&
                currentShop == creativeAttemptedShop &&
                currentSpirit == creativeAttemptedSpirit;
            bool currentIsOriginal =
                currentMaterial == originalIgnoreMaterialCost &&
                currentShop == originalSkipMoneyVerify &&
                currentSpirit == originalIgnoreSpiritCost;
            bool currentIsOwnedUncertain =
                creativeMutationUncertain &&
                (currentMaterial == originalIgnoreMaterialCost ||
                 currentMaterial == creativeAttemptedMaterial) &&
                (currentShop == originalSkipMoneyVerify ||
                 currentShop == creativeAttemptedShop) &&
                (currentSpirit == originalIgnoreSpiritCost ||
                 currentSpirit == creativeAttemptedSpirit);
            if (!currentIsApplied &&
                !currentIsOriginal &&
                !currentIsOwnedUncertain)
            {
                failure =
                    "GameInitConfig creative flags changed outside DebugConsole; exact original restoration remains pending.";
                return false;
            }
            if (!currentIsOriginal &&
                !TryWriteCreativeFlags(
                    config,
                    originalIgnoreMaterialCost,
                    originalSkipMoneyVerify,
                    originalIgnoreSpiritCost,
                    out failure,
                    out _))
            {
                creativeMutationUncertain = true;
                return false;
            }
            DebugConsoleCreativeHooks.Disable();
            try
            {
                runtime.SetCreativeHookDemand(false);
            }
            catch (Exception error)
            {
                failure = error.GetType().Name + ": " + error.Message;
                return false;
            }
            ClearCreativeLease();
            runtime.Status(
                "DebugConsole.creative-restore",
                "restored",
                runtime.NativeOwnerLabel + " exact native lease",
                "Exact original GameInitConfig flags restored reason=" +
                reason + ".");
            return true;
        }

        private static bool TryWriteCreativeFlags(
            object config,
            bool material,
            bool shop,
            bool spirit,
            out string failure,
            out bool rollbackVerified)
        {
            rollbackVerified = false;
            if (!TryReadCreativeFlags(
                    config,
                    out bool beforeMaterial,
                    out bool beforeShop,
                    out bool beforeSpirit,
                    out failure))
                return false;
            try
            {
                if (!Native.Write(config, "ignoreMaterialCost", material) ||
                    !Native.Write(config, "skipMoneyVerifyInShop", shop) ||
                    !Native.Write(config, "ignoreSpiritCost", spirit))
                {
                    throw new InvalidOperationException(
                        "One or more GameInitConfig creative flags are not writable.");
                }
                if (!TryReadCreativeFlags(
                        config,
                        out bool observedMaterial,
                        out bool observedShop,
                        out bool observedSpirit,
                        out string readFailure) ||
                    observedMaterial != material ||
                    observedShop != shop ||
                    observedSpirit != spirit)
                {
                    throw new InvalidOperationException(
                        string.IsNullOrWhiteSpace(readFailure)
                            ? "GameInitConfig did not retain all requested creative flags."
                            : readFailure);
                }
                failure = string.Empty;
                return true;
            }
            catch (Exception error)
            {
                bool materialRestored = TryWriteCreativeFlag(
                    config,
                    "ignoreMaterialCost",
                    beforeMaterial);
                bool shopRestored = TryWriteCreativeFlag(
                    config,
                    "skipMoneyVerifyInShop",
                    beforeShop);
                bool spiritRestored = TryWriteCreativeFlag(
                    config,
                    "ignoreSpiritCost",
                    beforeSpirit);
                rollbackVerified =
                    materialRestored &&
                    shopRestored &&
                    spiritRestored &&
                    TryReadCreativeFlags(
                        config,
                        out bool restoredMaterial,
                        out bool restoredShop,
                        out bool restoredSpirit,
                        out _) &&
                    restoredMaterial == beforeMaterial &&
                    restoredShop == beforeShop &&
                    restoredSpirit == beforeSpirit;
                failure = error.GetType().Name + ": " +
                    error.Message +
                    (rollbackVerified
                        ? " Exact prior creative flags were restored."
                        : " Exact creative rollback remains pending.");
                return false;
            }
        }

        private static bool TryReadCreativeFlags(
            object config,
            out bool material,
            out bool shop,
            out bool spirit,
            out string failure)
        {
            material = false;
            shop = false;
            spirit = false;
            failure = string.Empty;
            try
            {
                object? materialValue =
                    Native.Read(config, "ignoreMaterialCost");
                object? shopValue =
                    Native.Read(config, "skipMoneyVerifyInShop");
                object? spiritValue =
                    Native.Read(config, "ignoreSpiritCost");
                if (materialValue == null ||
                    shopValue == null ||
                    spiritValue == null)
                {
                    failure =
                        "One or more GameInitConfig creative flags are unavailable.";
                    return false;
                }
                material = Convert.ToBoolean(
                    materialValue,
                    CultureInfo.InvariantCulture);
                shop = Convert.ToBoolean(
                    shopValue,
                    CultureInfo.InvariantCulture);
                spirit = Convert.ToBoolean(
                    spiritValue,
                    CultureInfo.InvariantCulture);
                return true;
            }
            catch (Exception error)
            {
                Exception cause =
                    error is TargetInvocationException invocation &&
                    invocation.InnerException != null
                        ? invocation.InnerException
                        : error;
                failure = cause.GetType().Name + ": " +
                    cause.Message;
                return false;
            }
        }

        private static bool TryWriteCreativeFlag(
            object config,
            string name,
            bool value)
        {
            try
            {
                return Native.Write(config, name, value);
            }
            catch
            {
                return false;
            }
        }

        private void ClearCreativeLease()
        {
            creativeSnapshotValid = false;
            creativeEnabled = false;
            creativeLeaseOwnerId = string.Empty;
            creativeAttemptedMaterial = false;
            creativeAttemptedShop = false;
            creativeAttemptedSpirit = false;
            creativeMutationUncertain = false;
        }

        private static bool TryReadNativeTimeScale(
            out double multiplier,
            out string failure,
            out string message)
        {
            multiplier = 1d;
            failure = string.Empty;
            message = string.Empty;
            Type? api = Native.Resolve("DolocAPI, Assembly-CSharp");
            try
            {
                object? manager = Native.Read(api, "timeScaleManager");
                object? value = Native.Read(
                    manager,
                    "currentTimeScale");
                if (manager == null || value == null)
                {
                    failure = "missing-time-scale-owner";
                    message =
                        "DolocAPI.timeScaleManager.currentTimeScale is unavailable.";
                    return false;
                }
                multiplier = Convert.ToDouble(
                    value,
                    CultureInfo.InvariantCulture);
                return true;
            }
            catch (Exception error)
            {
                Exception cause =
                    error is TargetInvocationException invocation &&
                    invocation.InnerException != null
                        ? invocation.InnerException
                        : error;
                failure = cause.GetType().Name;
                message = cause.Message;
                return false;
            }
        }

        private bool TryRollbackTimeScaleMutation(
            double rollbackMultiplier,
            double rollbackLogicalMultiplier,
            bool clearLeaseWhenRestored,
            ref string failure,
            ref string message)
        {
            bool wrote = TrySetNativeTimeScale(
                rollbackMultiplier,
                showTip: false,
                out string rollbackFailure,
                out string rollbackMessage);
            bool observed = wrote &&
                TryReadNativeTimeScale(
                    out double restored,
                    out rollbackFailure,
                    out rollbackMessage) &&
                NearlyEqual(restored, rollbackMultiplier);
            if (observed)
            {
                if (clearLeaseWhenRestored)
                    ClearTimeScaleLease();
                else
                {
                    timeScaleMultiplier =
                        rollbackLogicalMultiplier;
                    timeScaleAppliedMultiplier =
                        rollbackMultiplier;
                    timeScalePriorAppliedMultiplier =
                        rollbackMultiplier;
                    timeScaleMutationUncertain = false;
                }
                return true;
            }
            timeScaleMutationUncertain = true;
            message =
                (message ?? string.Empty) +
                " Exact rollback remains pending: " +
                FirstFailure(
                    string.IsNullOrWhiteSpace(rollbackFailure)
                        ? "time-scale-rollback-not-observed"
                        : rollbackFailure,
                    rollbackMessage);
            return false;
        }

        private static bool TrySetNativeTimeScale(
            double multiplier,
            bool showTip,
            out string failure,
            out string message)
        {
            failure = string.Empty;
            message = string.Empty;
            try
            {
                Type? api = Native.Resolve("DolocAPI, Assembly-CSharp");
                MethodInfo? set = api?.GetMethod(
                    "SetTimeScale",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(float), typeof(bool) },
                    null);
                if (set == null)
                {
                    failure = "missing-set-time-scale";
                    message =
                        "DolocAPI.SetTimeScale(float,bool) is unavailable.";
                    return false;
                }
                set.Invoke(
                    null,
                    new object[] { (float)multiplier, showTip });
                return true;
            }
            catch (Exception error)
            {
                failure = error.GetType().Name;
                message = error.Message;
                return false;
            }
        }

        private void ClearTimeScaleLease()
        {
            timeScaleMultiplier = 1d;
            timeScaleLeaseOwnerId = string.Empty;
            timeScaleOriginalMultiplier = 1d;
            timeScaleAppliedMultiplier = 1d;
            timeScalePriorAppliedMultiplier = 1d;
            timeScaleMutationUncertain = false;
            timeScaleLeaseActive = false;
        }

        private static object? GetGameInitConfig()
        {
            Type? api = Native.Resolve("DolocAPI, Assembly-CSharp");
            object? manager = Native.Read(api, "gameManager");
            return Native.Read(manager, "gameInitConfig");
        }

        private static bool ItemAvailable(string itemId)
        {
            Type? api = Native.Resolve("DolocAPI, Assembly-CSharp");
            MethodInfo? query = api?.GetMethod(
                "QueryItemProto",
                BindingFlags.Public | BindingFlags.Static);
            object?[] args = { itemId, null };
            return query?.Invoke(null, args) is bool found &&
                found &&
                args[1] != null;
        }

        private static bool CurrentRoomImplements(string interfaceName)
        {
            object? room = Native.CurrentRoom;
            return room != null &&
                room.GetType().GetInterfaces().Any(type =>
                    type.FullName?.Equals(
                        interfaceName,
                        StringComparison.Ordinal) == true);
        }

        private static void ReadTechPoint(
            object typeValue,
            out int points,
            out int level)
        {
            points = 0;
            level = 0;
            Type? operation = Native.Resolve(
                "DolocTown.GameData.ArchiveOperationGlobal, Assembly-CSharp");
            object? archive = Native.Archive;
            MethodInfo? getPoint = operation?.GetMethods(
                    BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method =>
                    method.Name == "GetTechPoint" &&
                    method.GetParameters().Length == 2);
            MethodInfo? getLevel = operation?.GetMethods(
                    BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method =>
                    method.Name == "GetTechLevel" &&
                    method.GetParameters().Length == 2);
            object? point = getPoint?.Invoke(null, new[] { archive, typeValue });
            object? currentLevel = getLevel?.Invoke(null, new[] { archive, typeValue });
            if (point != null)
                points = Convert.ToInt32(point, CultureInfo.InvariantCulture);
            if (currentLevel != null)
                level = Convert.ToInt32(currentLevel, CultureInfo.InvariantCulture);
        }

        private static IEnumerable<object> EnumerateEquipments(object? room)
        {
            if (room == null)
                yield break;
            var visited = new HashSet<object>();
            var pending = new Queue<object>();
            pending.Enqueue(room);
            while (pending.Count > 0)
            {
                object current = pending.Dequeue();
                if (!visited.Add(current))
                    continue;
                object? manager = Native.Read(current, "DM_equipment");
                foreach (object equipment in Native.Enumerate(
                    Native.Read(manager, "AllEquipments")))
                    yield return equipment;
                object? buildings = Native.Read(
                    Native.Read(current, "DM_building"),
                    "Buildings");
                foreach (object building in Native.Enumerate(buildings))
                {
                    object? child = Native.Read(building, "room");
                    if (child != null)
                        pending.Enqueue(child);
                }
            }
        }

        private static bool TryMature(object crop)
        {
            MethodInfo? debugSet = Native.Method(
                crop.GetType(),
                "DEBUG_SetLevel",
                1,
                false);
            if (debugSet != null)
            {
                ParameterInfo parameter = debugSet.GetParameters()[0];
                int mature = Math.Max(
                    1,
                    Native.Int(crop, "MaxLevel", Native.Int(crop, "maxLevel", 99)));
                debugSet.Invoke(
                    crop,
                    new[]
                    {
                        Convert.ChangeType(
                            mature,
                            parameter.ParameterType,
                            CultureInfo.InvariantCulture)
                    });
                return true;
            }
            return false;
        }

        private static MethodInfo? ResolveAddTechPointMethod(Type api)
        {
            if (!ReferenceEquals(addTechPointApiType, api))
            {
                addTechPointApiType = api;
                addTechPointMethod = null;
                addTechPointMethodResolved = false;
            }
            if (addTechPointMethodResolved)
                return addTechPointMethod;
            foreach (MethodInfo method in api.GetMethods(
                BindingFlags.Public | BindingFlags.Static))
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (method.Name == "AddTechPoint" &&
                    parameters.Length == 2 &&
                    parameters[1].ParameterType == typeof(int))
                {
                    addTechPointMethod = method;
                    break;
                }
            }
            addTechPointMethodResolved = true;
            return addTechPointMethod;
        }

        private static TimeScaleDebugResult Fail(
            TimeScaleDebugResult result,
            string reason,
            string message)
        {
            result.FailureReason = reason;
            result.Message = message;
            return result;
        }

        private static DebugValueResult Fail(
            DebugValueResult result,
            string reason,
            string message)
        {
            result.FailureReason = reason;
            result.Message = message;
            return result;
        }

        private static DebugCommandResult Fail(
            DebugCommandResult result,
            string reason,
            string message)
        {
            result.FailureReason = reason;
            result.Message = message;
            return result;
        }

        private static CropMaturityResult Fail(
            CropMaturityResult result,
            string reason,
            string message)
        {
            result.FailureReason = reason;
            result.Message = message;
            return result;
        }

        private static SpawnDebugResult Fail(
            SpawnDebugResult result,
            string reason,
            string message)
        {
            result.FailureReason = reason;
            result.Message = message;
            return result;
        }
    }
}
