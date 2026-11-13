using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using DolocTown;
using DTMAPI.MonsterContent;
using HarmonyLib;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private sealed class GroupExecution
        {
            internal readonly InstanceScope Scope;
            internal readonly MonsterHandle Handle;
            internal GroupExecution(InstanceScope scope) { Scope = scope; Handle = scope.Handle; }
            internal bool Valid(MonsterAI ai) => Scope.Active && !Scope.PendingGroupRelease && !Scope.PendingRemoveLogic
                && Scope.Handle.Equals(Handle) && ReferenceEquals(Scope.Controller?.MonsterAI, ai)
                && Scope.Logical?.IsDead == false && Scope.Binding.Definition.State == MonsterDefinitionState.Creatable;
        }
        private sealed class GroupMembership
        {
            internal readonly MonsterGroup Group;
            internal readonly MonsterAI Ai;
            internal readonly long Generation;
            internal GroupMembership(MonsterGroup group, MonsterAI ai, long generation)
            { Group = group; Ai = ai; Generation = generation; }
        }
        private sealed class GroupBroadcast
        {
            internal readonly NativeModule Module;
            internal readonly bool Nested;
            internal bool Active = true;
            internal Exception? Error;
            internal GroupBroadcast(NativeModule module, bool nested) { Module = module; Nested = nested; }
        }
        // Keep an old AI distinguishable after its Controller has returned to the pool.
        private readonly ConditionalWeakTable<MonsterAI, GroupExecution> groupExecutions = new ConditionalWeakTable<MonsterAI, GroupExecution>();
        private readonly Dictionary<MonsterAI, InstanceScope> groupOwners = new Dictionary<MonsterAI, InstanceScope>();

        private void InstallGroupHooks()
        {
            Patch(AccessTools.Method(typeof(MonsterGroup), "Add"), prefix: nameof(GroupAdding), postfix: nameof(GroupAdded));
            Patch(AccessTools.Method(typeof(MonsterGroup), "Foreach"), prefix: nameof(GroupBroadcasting), finalizer: nameof(GroupBroadcastReturned));
            Patch(AccessTools.DeclaredMethod(typeof(MonsterAI_FwbDrone), "OnHurt"), prefix: nameof(GroupHurtEntering), finalizer: nameof(GroupHurtReturned));
            Patch(AccessTools.DeclaredMethod(typeof(MonsterAI_FwbDrone), "_OnHurt"), prefix: nameof(GroupHurtAllowed));
        }
        private static bool GroupHurtAllowed(MonsterAI_FwbDrone __instance) => current == null
            || !current.groupExecutions.TryGetValue(__instance, out var execution) || execution.Valid(__instance);
        private static bool GroupHurtEntering(MonsterAI_FwbDrone __instance, out InstanceScope? __state)
        {
            __state = null;
            if (current == null || !current.groupExecutions.TryGetValue(__instance, out var execution)) return true;
            if (!execution.Valid(__instance)) return false;
            __state = execution.Scope; __state.NativeCallDepth++; return true;
        }
        private static Exception? GroupHurtReturned(InstanceScope? __state, Exception? __exception) => FinishTrafficCall(__state, __exception);
        private static bool GroupAdding(MonsterGroup __instance, MonsterAI entity)
        {
            if (entity == null || current == null || !current.groupExecutions.TryGetValue(entity, out var execution)) return true;
            if (!execution.Valid(entity)) return false;
            var scope = execution.Scope;
            if (!scope.Groups.Any(m => ReferenceEquals(m.Group, __instance) && ReferenceEquals(m.Ai, entity)))
                scope.Groups.Add(new GroupMembership(__instance, entity, scope.Handle.Generation));
            current.groupOwners[entity] = scope;
            return true;
        }
        private static void GroupAdded(MonsterGroup __instance, MonsterAI entity)
        {
            if (current != null && entity != null && current.groupOwners.ContainsKey(entity)
                && NativeAccess.Get<IList>(__instance, "monsters").Contains(entity)) current.Fault("GroupAcquired");
        }
        private static bool GroupBroadcasting(MonsterGroup __instance, ref Action<MonsterAI> handle, out GroupBroadcast? __state)
        {
            using var trace = OptimizationTrace.Begin(16);
            __state = null;
            var module = current;
            if (module == null) return true;
            var members = NativeAccess.Get<IList>(__instance, "monsters");
            if (!members.Cast<MonsterAI>().Any(ai => module.groupExecutions.TryGetValue(ai, out _))) return true;
            var original = handle;
            var state = new GroupBroadcast(module, NativeAccess.Get<bool>(__instance, "isLocked"));
            __state = state;
            handle = ai =>
            {
                if (!state.Active || state.Error != null) return;
                if (module.groupExecutions.TryGetValue(ai, out var execution) && !execution.Valid(ai)) return;
                try { original?.Invoke(ai); }
                // Preserve the failure, but let the native outer call finish its queue.
                catch (Exception ex) { state.Error = ex; }
            };
            if (!state.Nested) return true;
            foreach (var ai in members.Cast<MonsterAI>().ToArray()) handle(ai);
            return false; // The outer native call still owns the lock and queued mutations.
        }
        private static Exception? GroupBroadcastReturned(MonsterGroup __instance, GroupBroadcast? __state, Exception? __exception)
        {
            if (__state == null) return __exception;
            __state.Active = false;
            var errors = new List<Exception>();
            if (__exception != null) errors.Add(__exception);
            if (__state.Error != null) errors.Add(__state.Error);
            if (!__state.Nested)
            {
                if (__exception != null)
                {
                    NativeAccess.Set(__instance, "isLocked", false);
                    // An Add failure while draining must not discard unrelated queued peers.
                    if (NativeAccess.Get<bool>(__instance, "clearFlag")) __instance.Clear();
                    else
                    {
                        var queue = NativeAccess.Get<Queue<(MonsterAI, bool)>>(__instance, "changeQueue");
                        while (queue.Count > 0)
                        {
                            var change = queue.Dequeue();
                            Attempt(errors, () => { if (change.Item2) __instance.Add(change.Item1); else __instance.Remove(change.Item1); });
                        }
                    }
                }
                var module = __state.Module;
                foreach (var scope in module.instances.Values.Where(s => s.PendingGroupRelease
                    && s.Groups.Any(g => ReferenceEquals(g.Group, __instance))).ToArray())
                    Attempt(errors, () => module.Revoke(scope, scope.PendingRemoveLogic));
            }
            return errors.Count == 0 ? null : errors.Count == 1 ? errors[0] : new AggregateException(errors);
        }
        private bool ReleaseGroups(InstanceScope scope)
        {
            bool pending = false;
            var acquired = scope.Groups.ToArray();
            foreach (var member in acquired)
            {
                if (member.Generation != scope.Handle.Generation) throw new InvalidOperationException("Group membership belongs to another execution.");
                Fault("GroupCleanup");
                FilterQueue(NativeAccess.Get(member.Group, "changeQueue")!, item => !ReferenceEquals(NativeAccess.Get(item, "Item1"), member.Ai));
                member.Group.Remove(member.Ai);
                if (NativeAccess.Get<IList>(member.Group, "monsters").Contains(member.Ai))
                {
                    if (!NativeAccess.Get<bool>(member.Group, "isLocked")) throw new InvalidOperationException("Group still owns retired AI.");
                    pending = true; continue;
                }
                scope.Groups.Remove(member);
            }
            foreach (var member in acquired)
                if (!scope.Groups.Any(m => ReferenceEquals(m.Ai, member.Ai))) groupOwners.Remove(member.Ai);
            scope.PendingGroupRelease = pending;
            return !pending;
        }
    }
}
