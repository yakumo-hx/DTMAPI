using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using DTMAPI.Abstractions;

namespace DTMAPI.Mine
{
    internal sealed partial class MineNativeRuntime
    {
        private readonly MineRuntimeServices runtime;
        private readonly MineHookInstaller hookInstaller;
        private readonly MineSessionScheduler scheduler =
            new MineSessionScheduler();
        private readonly MineRuntimeState state =
            new MineRuntimeState();
        private readonly Dictionary<string, Action> nativeRestores =
            new Dictionary<string, Action>(StringComparer.Ordinal);
        private readonly List<string> nativeRestoreOrder =
            new List<string>();
        private readonly Dictionary<object, object> scaleSnapshots =
            new Dictionary<object, object>(
                ReferenceEqualityComparer.Instance);
        private Random outputRandom = new Random();
        private DateTimeOffset lastMachineProductionPollAt =
            DateTimeOffset.MinValue;
        private bool saveActive;
        private bool active;
        private bool entryHooksAuthorized;
        private bool entryInitialized;
        private bool disposed;
        private MineConfig config = new MineConfig();
        private IManifest manifest = null!;
        private Func<bool> isOilAvailable = () => false;
        private string status = "waiting-for-save";
        private string lastMessage =
            "Runtime production will start after SaveLoaded.";

        internal MineNativeRuntime(
            IMonitor monitor,
            MineHookInstaller? hookInstaller = null)
        {
            runtime = new MineRuntimeServices(monitor);
            this.hookInstaller =
                hookInstaller ?? new MineHookInstaller(monitor);
        }

        internal string Status => status;
        internal string LastMessage => lastMessage;
        internal int InstalledPatchCount =>
            hookInstaller.InstalledPatchCount;
        internal int SchedulerEntryCount =>
            scheduler.Count;
        internal int PendingNativeRestoreCount =>
            nativeRestores.Count;
        internal bool IsActive => active;
        internal MineRuntimeState State => state;

        internal void InitializeAtEntry(
            MineConfig next,
            IManifest owner,
            Func<bool> oilAvailability)
        {
            ThrowIfDisposed();
            if (entryInitialized)
            {
                throw new InvalidOperationException(
                    "Mine Entry initialization may run only once.");
            }
            ApplyConfiguration(
                next,
                owner,
                oilAvailability);
            if (config.Enabled)
            {
                hookInstaller.InstallAtomically(this);
                entryHooksAuthorized = true;
                lastMessage =
                    "Runtime production is enabled; exact visual hooks were registered during Entry and remain inert until SaveLoaded.";
            }
            else
            {
                status = "disabled";
                lastMessage =
                    "Runtime production is disabled at cold Entry; no Mine native hooks were registered.";
            }
            entryInitialized = true;
        }

        internal void Configure(
            MineConfig next,
            IManifest owner,
            Func<bool> oilAvailability,
            string reason)
        {
            ThrowIfDisposed();
            if (!entryInitialized)
            {
                throw new InvalidOperationException(
                    "Mine configuration cannot change before Entry initialization.");
            }
            ApplyConfiguration(
                next,
                owner,
                oilAvailability);

            if (!config.Enabled)
            {
                DeactivateRuntime(
                    "configuration disabled: " + reason);
                status = "disabled";
                lastMessage =
                    "Runtime production is disabled; native Mine content remains available.";
                return;
            }

            if (!entryHooksAuthorized)
            {
                DeactivateRuntime(
                    "cold-disabled configuration requires restart: " +
                    reason);
                status = "restart-required";
                lastMessage =
                    "Runtime production was disabled at cold Entry. Enabling requires a restart so the exact hooks can be supervised during Entry.";
                return;
            }

            if (saveActive)
                ActivateRuntime("configuration applied: " + reason);
            else
            {
                if (PendingNativeRestoreCount > 0)
                {
                    status = "cleanup-failed";
                    lastMessage =
                        "Mine still has exact native restoration work queued; configuration refresh cannot reactivate production until cleanup succeeds.";
                }
                else
                {
                    status = "waiting-for-save";
                    lastMessage =
                        "Runtime production is enabled and will start after SaveLoaded.";
                }
            }
        }

        internal void SaveLoaded(int? slot)
        {
            ThrowIfDisposed();
            saveActive = true;
            DeactivateSession("SaveLoaded reset");
            ClearSessionState();
            if (config.Enabled && entryHooksAuthorized)
            {
                ActivateRuntime(
                    "SaveLoaded slot=" +
                    (slot?.ToString(
                        CultureInfo.InvariantCulture) ??
                     "unknown"));
            }
            else if (config.Enabled)
            {
                status = "restart-required";
                lastMessage =
                    "SaveLoaded kept runtime production inactive because Enabled changed from cold false; restart is required.";
            }
            else
            {
                status = "disabled";
                lastMessage =
                    "SaveLoaded kept runtime production disabled.";
            }
        }

        internal void Update()
        {
            if (!active || disposed)
                return;
            try
            {
                UpdateMachineProduction();
            }
            catch (Exception ex)
            {
                status = "runtime-failed";
                lastMessage =
                    ex.GetType().Name + ": " + ex.Message;
                runtime.RuntimeMonitor.Log(
                    "Mine runtime update failed; current due entries were retained. " +
                    ex,
                    LogLevel.Error);
            }
        }

        internal void ReturnedToTitle(string reason)
        {
            saveActive = false;
            DeactivateRuntime(reason);
            status = config.Enabled
                ? "waiting-for-save"
                : "disabled";
            lastMessage =
                "Mine session scheduler and native mutations were cleared at title.";
        }

        internal void DeactivateOwner(string reason)
        {
            if (disposed)
                return;
            saveActive = false;
            DeactivateRuntime(reason);
            disposed = true;
            status = "disposed";
            lastMessage =
                "Mine ProductNative owner was fully deactivated.";
        }

        internal string BuildStatusSummary()
        {
            return "status=" + status +
                ", active=" + active +
                ", hooks=" + InstalledPatchCount + "/" +
                MineProductContract.ExpectedHookCount +
                ", placed=" + state.PlacedMineCount +
                ", scheduler=" + SchedulerEntryCount +
                ", pendingNativeRestores=" +
                PendingNativeRestoreCount +
                ", cycle=" + config.CycleMinutes + "m" +
                ", observedTU=" +
                state.LastObservedTotalTUs +
                ", nextDueTU=" +
                state.NextDueTotalTUs +
                ", power=" +
                MineProductContract.FixedPowerCost +
                ", output=" +
                FirstText(this.state.LastOutputItemId, "none") +
                "x" + this.state.LastOutputCount +
                ", storage=" + this.state.LastStorageFilledSlots +
                "/" + this.state.LastStorageCapacity +
                ", runtimeMessage=" + lastMessage +
                ", machineMessage=" + this.state.LastMessage;
        }

        private void ActivateRuntime(string reason)
        {
            DeactivateSession("reconfigure before " + reason);
            ClearSessionState();
            var failures = new List<Exception>();
            try
            {
                hookInstaller.InstallAtomically(this);
                ConfigureNativeMineContent(BuildDefinition());
                if (ContainsFailure(state.NativeTechTreeSummary))
                {
                    throw new InvalidOperationException(
                        "Mine native activation did not complete: " +
                        state.NativeTechTreeSummary);
                }

                active = true;
                status = "active-session-derived";
                lastMessage =
                    "Runtime production active; scheduler starts from current native game time. reason=" +
                    reason;
                runtime.RuntimeMonitor.Log(
                    "Mine ProductNative activation complete owner=" +
                    MineProductContract.HarmonyOwner +
                    " hooks=" + InstalledPatchCount +
                    " schedulerMode=session-derived.");
            }
            catch (Exception activationFailure)
            {
                failures.Add(activationFailure);
                TryCleanup(
                    RestoreVisualScales,
                    failures);
                TryCleanup(
                    RestoreNativeMutations,
                    failures);
                TryCleanup(
                    () => hookInstaller.UnpatchOwnedHooks(this),
                    failures);
                ClearSessionState();
                active = false;
                status = PendingNativeRestoreCount > 0
                    ? "cleanup-failed"
                    : "activation-failed";
                lastMessage = PendingNativeRestoreCount > 0
                    ? "Mine activation failed and exact native restoration remains queued; cleanup retry or restart is required."
                    : activationFailure.GetType().Name +
                      ": " + activationFailure.Message;
                if (failures.Count == 1)
                    throw;
                throw new AggregateException(
                    "Mine activation failed and exact rollback encountered cleanup failures.",
                    failures);
            }
        }

        private void DeactivateRuntime(string reason)
        {
            var failures = new List<Exception>();
            active = false;
            if (hookInstaller.InstalledPatchCount > 0)
            {
                TryCleanup(
                    () => hookInstaller.UnpatchOwnedHooks(this),
                    failures);
            }
            TryCleanup(
                () => DeactivateSession(reason),
                failures);
            if (failures.Count > 0)
            {
                status = "cleanup-failed";
                lastMessage =
                    "Mine cleanup could not prove exact restoration. reason=" +
                    reason;
                throw new AggregateException(
                    lastMessage,
                    failures);
            }
        }

        private void DeactivateSession(string reason)
        {
            var failures = new List<Exception>();
            active = false;
            TryCleanup(RestoreVisualScales, failures);
            TryCleanup(RestoreNativeMutations, failures);
            ClearSessionState();
            if (failures.Count > 0)
            {
                status = "cleanup-failed";
                lastMessage =
                    "Mine cleanup could not prove exact restoration. reason=" +
                    reason;
                throw new AggregateException(
                    lastMessage,
                    failures);
            }
        }

        private void ApplyConfiguration(
            MineConfig next,
            IManifest owner,
            Func<bool> oilAvailability)
        {
            if (next == null)
                throw new ArgumentNullException(nameof(next));
            config = next.Copy();
            config.Normalize();
            manifest = owner ??
                throw new ArgumentNullException(nameof(owner));
            isOilAvailable = oilAvailability ??
                throw new ArgumentNullException(
                    nameof(oilAvailability));
        }

        private MineDefinition BuildDefinition()
        {
            bool oil = isOilAvailable();
            return new MineDefinition
            {
                NativeTechNodeId = MineProductContract.MineItemId,
                NativeTechNodeTitle = "矿井",
                NativeTechNodeParentId = "alloy_material",
                NativeTechNodeAboveTitleContains = "指挥官",
                CycleMinutes = config.CycleMinutes,
                RecipeInputs =
                    config.UseOilRecipeReplacement && oil
                        ? BuildOilRecipe()
                        : Array.Empty<MineRecipeInput>(),
                OutputRules = BuildOutputRules(oil)
            };
        }

        private IReadOnlyList<MineOutputRule>
            BuildOutputRules(bool oil)
        {
            var rules = new List<MineOutputRule>
            {
                new MineOutputRule
                {
                    ItemId = "coal",
                    DisplayName = "Coal",
                    Weight = config.CoalWeight,
                    MinCount = 1,
                    MaxCount = 2
                },
                new MineOutputRule
                {
                    ItemId = "copper_ore",
                    DisplayName = "Copper ore",
                    Weight = config.CopperOreWeight,
                    MinCount = 1,
                    MaxCount = 2
                },
                new MineOutputRule
                {
                    ItemId = "iron_ore",
                    DisplayName = "Iron ore",
                    Weight = config.IronOreWeight,
                    MinCount = 1,
                    MaxCount = 1
                }
            };
            if (oil)
            {
                rules.Add(
                    new MineOutputRule
                    {
                        ItemId = MineProductContract.OilItemId,
                        DisplayName = "Oil",
                        Weight = config.OilWeight,
                        MinCount = 1,
                        MaxCount = 1
                    });
            }
            return rules;
        }

        private static IReadOnlyList<MineRecipeInput>
            BuildOilRecipe() =>
            new[]
            {
                new MineRecipeInput
                {
                    ItemId = "metal_framework",
                    Count = 10
                },
                new MineRecipeInput
                {
                    ItemId = "engine_core",
                    Count = 5
                },
                new MineRecipeInput
                {
                    ItemId = "steel_ingot",
                    Count = 20
                },
                new MineRecipeInput
                {
                    ItemId = MineProductContract.OilItemId,
                    Count = 10
                }
            };

        private void ClearSessionState()
        {
            scheduler.Clear();
            lastMachineProductionPollAt =
                DateTimeOffset.MinValue;
            outputRandom = new Random();
            state.PlacedMineCount = 0;
            state.ProductionCycleCount = 0;
            state.LastObservedTotalTUs = -1;
            state.NextDueTotalTUs = -1;
            state.LastOutputItemId = string.Empty;
            state.LastOutputDisplayName = string.Empty;
            state.LastOutputCount = 0;
            state.LastStorageFilledSlots = 0;
            state.LastStorageCapacity = 0;
            state.LastStorageLineCapacity = 0;
            state.LastMessage =
                "Mine session scheduler is empty.";
        }

        private static bool ContainsFailure(string value) =>
            !string.IsNullOrWhiteSpace(value) &&
            (value.IndexOf(
                 "failed",
                 StringComparison.OrdinalIgnoreCase) >= 0 ||
             value.IndexOf(
                 "pending",
                 StringComparison.OrdinalIgnoreCase) >= 0);

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(
                    nameof(MineNativeRuntime));
        }

        private static void TryCleanup(
            Action cleanup,
            ICollection<Exception> failures)
        {
            try
            {
                cleanup();
            }
            catch (Exception ex)
            {
                failures.Add(ex);
            }
        }

        private static IEnumerable<object>
            EnumerateMachineCandidateEquipments(
                Type dolocApi,
                object archive,
                object currentRoom)
        {
            var visitedEquipment =
                new HashSet<object>(
                    ReferenceEqualityComparer.Instance);
            foreach (object room in
                     EnumerateMachineCandidateRooms(
                         dolocApi,
                         archive,
                         currentRoom))
            {
                foreach (object equipment in
                         EnumerateEquipments(room))
                {
                    if (visitedEquipment.Add(equipment))
                        yield return equipment;
                }
            }
        }

        private static IEnumerable<object>
            EnumerateMachineCandidateRooms(
                Type dolocApi,
                object archive,
                object currentRoom)
        {
            var visitedRooms =
                new HashSet<object>(
                    ReferenceEqualityComparer.Instance);
            var result = new List<object>();

            void AddRoom(object? room)
            {
                if (room == null)
                    return;
                if (visitedRooms.Add(room))
                    result.Add(room);
            }

            AddRoom(currentRoom);
            AddRoom(ReadMember(currentRoom, "RootRoom"));
            AddRoom(ReadStaticMember(dolocApi, "CurrentRootRoom"));
            AddRoom(ReadMember(archive, "currentRoom"));
            AddRoom(ReadMember(archive, "MainFarm"));
            object? farmData =
                ReadMember(archive, "farmData");
            if (farmData != null)
            {
                AddRoom(ReadMember(farmData, "currentRoom"));
                AddRoom(ReadMember(farmData, "MainFarm"));
            }
            return result;
        }

        private static IEnumerable<object>
            EnumerateEquipments(object room)
        {
            var visitedRooms =
                new HashSet<object>(
                    ReferenceEqualityComparer.Instance);
            foreach (object equipment in
                     EnumerateEquipments(room, visitedRooms))
            {
                yield return equipment;
            }
        }

        private static IEnumerable<object>
            EnumerateEquipments(
                object room,
                HashSet<object> visitedRooms)
        {
            if (room == null || !visitedRooms.Add(room))
                yield break;

            object? equipmentManager =
                ReadMember(room, "DM_equipment");
            object? allEquipments =
                equipmentManager == null
                    ? null
                    : ReadMember(
                        equipmentManager,
                        "AllEquipments");
            if (allEquipments is IEnumerable enumerable)
            {
                foreach (object? equipment in enumerable)
                {
                    if (equipment != null)
                        yield return equipment;
                }
            }

            object? buildingManager =
                ReadMember(room, "DM_building");
            object? buildings =
                buildingManager == null
                    ? null
                    : ReadMember(
                        buildingManager,
                        "Buildings");
            if (!(buildings is IEnumerable buildingEnumerable))
                yield break;

            foreach (object? building in buildingEnumerable)
            {
                if (building == null)
                    continue;
                object? childRoom =
                    ReadMember(building, "room");
                if (childRoom == null)
                    continue;
                foreach (object childEquipment in
                         EnumerateEquipments(
                             childRoom,
                             visitedRooms))
                {
                    yield return childEquipment;
                }
            }
        }

        private sealed class ReferenceEqualityComparer :
            IEqualityComparer<object>
        {
            internal static readonly ReferenceEqualityComparer
                Instance = new ReferenceEqualityComparer();

            public new bool Equals(object? x, object? y) =>
                ReferenceEquals(x, y);

            public int GetHashCode(object obj) =>
                RuntimeHelpers.GetHashCode(obj);
        }
    }
}
