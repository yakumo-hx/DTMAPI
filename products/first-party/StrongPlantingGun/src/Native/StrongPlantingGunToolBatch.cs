using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using global::DTMAPI.Abstractions;

namespace DTMAPI.StrongPlantingGun
{
    internal sealed partial class StrongPlantingGunNativeRuntime
    {
        private IDtmRuntimeContext? batchContext;
        private IDisposable? batchContextSubscription;
        private ToolBatch? batch;
        internal int PendingBatchCount => batch == null ? 0 : 1;
        internal int PendingCallbackCount => batch?.Callbacks.FindAll(c => c.IsPending).Count ?? 0;

        internal void SetBatchContext(IDtmRuntimeContext context) => batchContext = context;

        private void SubscribeBatchContext()
        {
            if (batchContext != null && batchContextSubscription == null)
                batchContextSubscription = batchContext.Subscribe(_ => ObservePreviewContext());
        }

        internal void ObserveToolBatch()
        {
            if (batch == null) return;
            try
            {
                if (!IsBatchCurrent(batch)) CancelToolBatch("scope changed", refreshTip: false);
            }
            catch (Exception ex) { StopToolBatch(ex); }
        }

        private bool StartToolBatch(object gun)
        {
            if (!callbackAttached) return true;
            // A held/repeated use cannot start a second batch while its first animation is pending.
            if (batch != null) return false;
            try
            {
                PrepareGun(gun, "ItemFarmingGun.OnUseAsTool");
                object inventory = reflection.ReadMember(gun, "inventory") ?? throw new MissingMemberException("gun.inventory");
                if (reflection.ReadBool(inventory, "isEmpty", false)) return true; // Native empty-gun container UI.
                DtmRuntimeContextSnapshot? scope = batchContext?.Snapshot;
                object? room = NativeValue("CurrentRoom");
                object? archive = NativeValue("archiveHandle");
                object? agent = NativeValue("agent");
                if (scope == null || !scope.IsWorldReady || room == null || archive == null || agent == null ||
                    !ReferenceEquals(NativeValue("SelectedItem"), gun) ||
                    !reflection.ReadBool(agent, "IsCurrentStateSupportInteract", false) ||
                    !reflection.ReadBool(agent, "IsCurrentStateSupportUseItem", false)) return false;
                InvokeNoArg(gun, "InitCellTip");
                object tip = reflection.ReadMember(gun, "cellTip") ?? throw new MissingMemberException("gun.cellTip");
                var next = new ToolBatch(gun, inventory, room, archive, agent, scope,
                    reflection.ReadMember(tip, "CellAnchor") ?? throw new MissingMemberException("CellAnchor"),
                    reflection.ReadMember(tip, "CellSize") ?? throw new MissingMemberException("CellSize"),
                    reflection.ReadMember(room, "DM_terrain") ?? throw new MissingMemberException("DM_terrain"));
                foreach (object equipment in ReadUniqueEquipments(gun))
                    if (IsTargetCurrent(next, equipment) && FindMaterial(next, equipment, ItemKind.None, out _, out _) != null)
                        next.Targets.Add(equipment);
                if (next.Targets.Count == 0) return false;
                batch = next;
                LastConsumedItemCount = 0;
                Status = "batch-animation";
                InvokeNoArg(gun, "HideCellTip");
                PlayGunSound();
                Action animation = AddBatchCallback(next, () => BeginFlights(next));
                RequireMethod(agent, "_Interact", 2).Invoke(agent, new object[] { animation, "chop" });
                return false;
            }
            catch (Exception ex)
            {
                StopToolBatch(ex);
                return false; // Never fall back to a second native Use after an uncertain product attempt.
            }
        }

        private void BeginFlights(ToolBatch current)
        {
            if (!IsBatchCurrent(current)) { CancelToolBatch("animation scope changed", false); return; }
            var ready = new List<object>();
            foreach (object target in current.Targets)
            {
                if (!IsTargetCurrent(current, target)) continue;
                object? material = FindMaterial(current, target, ItemKind.None, out _, out _);
                if (material == null) continue;
                object? icon = reflection.ReadMember(material, "uiSprite");
                NativeCall("MoveFadeOutSprite", Vector2(NativeValue("AgentPosition")!),
                    Vector2(reflection.ReadMember(target, "PositionCenter")!), icon, 0.35f,
                    Enum.Parse(RequireStatic("MoveFadeOutSprite", 5).GetParameters()[4].ParameterType, "Linear"));
                ready.Add(target);
            }
            current.Remaining = ready.Count;
            if (ready.Count == 0) { CompleteBatch(current); return; }
            InvokeNoArg(current.Gun!, "ShineArea");
            Status = "batch-flight";
            foreach (object target in ready)
                NativeCall("Delay", 0.35f, AddBatchCallback(current, () => ExecuteTarget(current, target)));
        }

        private void ExecuteTarget(ToolBatch current, object target)
        {
            if (!IsBatchCurrent(current)) { CancelToolBatch("flight scope changed", false); return; }
            if (IsTargetCurrent(current, target))
            {
                foreach (ItemKind kind in new[] { ItemKind.Seed, ItemKind.Fertilizer, ItemKind.Film, ItemKind.Water })
                {
                    if (!IsBatchCurrent(current)) { CancelToolBatch("step scope changed", false); return; }
                    if (!IsTargetCurrent(current, target)) break;
                    object? source = FindMaterial(current, target, kind, out int slot, out object? material);
                    if (source == null || material == null) continue;
                    // Qualification, complete native action, and its one-unit cost are synchronous.
                    // Normal rejection costs nothing. Unknown native/receiver faults stop; never refund/replay.
                    if (!InvokeInteraction(current.Gun!, "DoInteract", target, material)) continue;
                    if (!ReferenceEquals(ReadInventoryItemAt(current.Inventory!, slot), source) ||
                        reflection.ReadInt(source, "count", 0) < 1)
                        throw new InvalidOperationException("Selected material changed during the native action; batch stopped without compensation.");
                    if (!TryCostAtIndex(current.Inventory!, slot, 1, exactItem: true))
                        throw new InvalidOperationException("Native action completed but its cost returned false; batch stopped without compensation.");
                    current.Consumed++;
                    LastConsumedItemCount = current.Consumed;
                }
            }
            if (--current.Remaining == 0) CompleteBatch(current);
        }

        // None asks for the first currently qualified material in the same category order.
        private object? FindMaterial(ToolBatch current, object target, ItemKind requested, out int slot, out object? one)
            => FindMaterial(current.Gun!, current.Inventory!, target, requested, out slot, out one);

        private object? FindMaterial(object gun, object inventory, object target, ItemKind requested, out int slot, out object? one)
        {
            foreach (ItemKind kind in new[] { ItemKind.Seed, ItemKind.Fertilizer, ItemKind.Film, ItemKind.Water })
            {
                if (requested != ItemKind.None && kind != requested) continue;
                int capacity = reflection.ReadInt(inventory, "capacity", 0);
                for (int index = 0; index < capacity; index++)
                {
                    object? item = ReadInventoryItemAt(inventory, index);
                    if (GetAllowedItemKind(item) != kind || reflection.ReadInt(item!, "count", 0) < 1) continue;
                    object clone = CloneItem(item!, 1) ?? throw new MissingMethodException("Item.Clone(int)");
                    if (!InvokeInteraction(gun, "CheckCanInteract", target, clone)) continue;
                    slot = index; one = clone; return item;
                }
            }
            slot = -1; one = null; return null;
        }

        private bool IsBatchCurrent(ToolBatch current)
        {
            if (!ReferenceEquals(batch, current) || !callbackAttached || current.Gun == null) return false;
            DtmRuntimeContextSnapshot? now = batchContext?.Snapshot;
            return now != null && now.IsWorldReady && now.RuntimeInstanceId == current.Scope.RuntimeInstanceId &&
                now.SaveSessionEpoch == current.Scope.SaveSessionEpoch && now.WorldEpoch == current.Scope.WorldEpoch &&
                ReferenceEquals(NativeValue("archiveHandle"), current.Archive) &&
                ReferenceEquals(NativeValue("CurrentRoom"), current.Room) &&
                ReferenceEquals(NativeValue("agent"), current.Agent) &&
                ReferenceEquals(NativeValue("SelectedItem"), current.Gun) &&
                ReferenceEquals(reflection.ReadMember(current.Gun, "inventory"), current.Inventory);
        }

        private bool IsTargetCurrent(ToolBatch current, object target)
        {
            if (reflection.ReadBool(target, "IsRemoved", true) ||
                !ReferenceEquals(reflection.ReadMember(target, "CurrentRoom"), current.Room)) return false;
            MethodInfo query = RequireMethod(current.Terrain!, "GetContentsFromArea", 2);
            Type equipment = typeof(DolocAPI).Assembly.GetType("DolocTown.Equipment", throwOnError: true)!;
            var contents = (IEnumerable)query.MakeGenericMethod(equipment).Invoke(current.Terrain, new[] { current.Anchor, current.Size });
            foreach (object candidate in contents) if (ReferenceEquals(candidate, target)) return true;
            return false;
        }

        private Action AddBatchCallback(ToolBatch current, Action action)
        {
            var once = new StrongPlantingGunToolCallback(() => {
                if (!ReferenceEquals(batch, current)) return;
                try { action(); } catch (Exception ex) { StopToolBatch(ex); }
            });
            current.Callbacks.Add(once);
            return once.Invoke;
        }

        private void CompleteBatch(ToolBatch current)
        {
            int consumed = current.Consumed;
            CancelToolBatch("completed", refreshTip: true);
            Status = "batch-complete";
            LastMessage = "Native batch completed; consumed=" + consumed + ".";
            if (logGate.ShouldLog(false, "tool", consumed > 0)) monitor.Log("StrongPlantingGun " + LastMessage);
        }

        private void StopToolBatch(Exception error)
        {
            if (error is TargetInvocationException invocation && invocation.InnerException != null) error = invocation.InnerException;
            CancelToolBatch("unknown native failure", refreshTip: false);
            Status = "batch-stopped";
            LastMessage = "Batch stopped; actual state retained, no refund or replay. " + error.GetType().Name + ": " + error.Message;
            monitor.Log("StrongPlantingGun " + LastMessage, LogLevel.Error);
        }

        private void CancelToolBatch(string reason, bool refreshTip)
        {
            ToolBatch? old = batch;
            if (old == null) return;
            batch = null;
            object? gun = old.Gun;
            foreach (StrongPlantingGunToolCallback callback in old.Callbacks) callback.Cancel();
            old.Clear(); // Native delayed callbacks retain only cleared one-shot tickets after cancellation.
            Status = "batch-cancelled";
            LastMessage = "Batch ended: " + reason + ".";
            if (refreshTip && gun != null && ReferenceEquals(NativeValue("SelectedItem"), gun)) InvokeNoArg(gun, "InitCellTip");
        }

        private object? NativeValue(string name) => reflection.ReadStaticMember(typeof(DolocAPI), name);
        private MethodInfo RequireMethod(object instance, string name, int arguments) =>
            reflection.GetMethod(instance.GetType(), name, arguments) ?? throw new MissingMethodException(instance.GetType().FullName, name);
        private static MethodInfo RequireStatic(string name, int arguments)
        {
            foreach (MethodInfo method in typeof(DolocAPI).GetMethods(BindingFlags.Public | BindingFlags.Static))
                if (method.Name == name && method.GetParameters().Length == arguments) return method;
            throw new MissingMethodException("DolocAPI", name);
        }
        private static object? NativeCall(string name, params object?[] arguments) => RequireStatic(name, arguments.Length).Invoke(null, arguments);
        private object Vector2(object value)
        {
            Type vector2 = value.GetType().Assembly.GetType("UnityEngine.Vector2", throwOnError: true)!;
            return Activator.CreateInstance(vector2, Convert.ToSingle(reflection.ReadMember(value, "x")), Convert.ToSingle(reflection.ReadMember(value, "y")))!;
        }
        private void PlayGunSound()
        {
            object sound = NativeValue("Sound") ?? throw new MissingMemberException("DolocAPI.Sound");
            foreach (MethodInfo method in sound.GetType().GetMethods())
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (method.Name == "PostSoundEvent" && parameters.Length == 3 && parameters[0].ParameterType.Name == "SoundEvents")
                { method.Invoke(sound, new object?[] { Enum.Parse(parameters[0].ParameterType, "PLAY_USE_FARMING_GUN"), null, false }); return; }
            }
            throw new MissingMethodException("PostSoundEvent(SoundEvents,callback,bool)");
        }

        private sealed class ToolBatch
        {
            internal object? Gun, Inventory, Room, Archive, Agent, Terrain, Anchor, Size;
            internal readonly DtmRuntimeContextSnapshot Scope;
            internal readonly List<object> Targets = new List<object>();
            internal readonly List<StrongPlantingGunToolCallback> Callbacks = new List<StrongPlantingGunToolCallback>();
            internal int Remaining, Consumed;
            internal ToolBatch(object gun, object inventory, object room, object archive, object agent, DtmRuntimeContextSnapshot scope, object anchor, object size, object terrain)
            { Gun = gun; Inventory = inventory; Room = room; Archive = archive; Agent = agent; Scope = scope; Anchor = anchor; Size = size; Terrain = terrain; }
            internal void Clear() { Gun = Inventory = Room = Archive = Agent = Terrain = Anchor = Size = null; Targets.Clear(); Callbacks.Clear(); }
        }
    }

    internal sealed class StrongPlantingGunToolCallback
    {
        private Action? action;
        internal StrongPlantingGunToolCallback(Action action) => this.action = action;
        internal bool IsPending => action != null;
        internal void Cancel() => action = null;
        internal void Invoke()
        {
            Action? pending = action;
            action = null; // Clear before calling: repeated or reentrant native callbacks cannot replay the action.
            pending?.Invoke();
        }
    }
}
