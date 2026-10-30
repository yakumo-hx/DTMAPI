using System;
using System.Collections;
using System.Collections.Generic;

namespace DTMAPI.MoreEquipmentSlots
{
    // Retains exact function ownership through partial OnCreate/OnRemove failure.
    // A failed removal blocks only its replacement; unrelated slots still clean up.
    internal sealed class EquipmentSlotEffects
    {
        private readonly Dictionary<int, Lease> leases = new Dictionary<int, Lease>();
        internal int Count => leases.Count;

        internal void Reconcile(IDictionary functions, IReadOnlyList<EffectInput> desired,
            Func<object, string, object> create)
        {
            var failures = new List<Exception>();
            var expected = new Dictionary<int, EffectInput>();
            foreach (EffectInput input in desired) expected.Add(input.Index, input);
            foreach (KeyValuePair<int, Lease> pair in new List<KeyValuePair<int, Lease>>(leases))
            {
                Lease lease = pair.Value;
                if (expected.TryGetValue(pair.Key, out EffectInput input) &&
                    lease.Active && ReferenceEquals(lease.Item, input.Item) &&
                    lease.Skill == input.Skill && ReferenceEquals(lease.Dictionary, functions) &&
                    ReferenceEquals(functions[lease.Item], lease.Function)) continue;
                try { lease.Remove(); leases.Remove(pair.Key); }
                catch (Exception ex) { failures.Add(ex); }
            }
            foreach (EffectInput input in desired)
            {
                if (leases.ContainsKey(input.Index)) continue;
                try
                {
                    object function = create(input.Item, input.Skill);
                    if (functions.Contains(input.Item))
                        throw new InvalidOperationException("Another owner already registered the exact item.");
                    var lease = new Lease(functions, input.Item, input.Skill, function);
                    leases.Add(input.Index, lease);
                    functions.Add(input.Item, function);
                    EquipmentSlotNativeItem.Call(function, "OnCreate");
                    lease.Active = true;
                }
                catch (Exception ex)
                {
                    failures.Add(ex);
                    if (leases.TryGetValue(input.Index, out Lease? failed) && failed != null)
                    {
                        try { failed.Remove(); leases.Remove(input.Index); }
                        catch (Exception cleanup) { failures.Add(cleanup); }
                    }
                }
            }
            if (failures.Count > 0) throw new AggregateException("Equipment effect reconciliation is incomplete.", failures);
        }

        internal void Clear()
        {
            var failures = new List<Exception>();
            foreach (KeyValuePair<int, Lease> pair in new List<KeyValuePair<int, Lease>>(leases))
            {
                try { pair.Value.Remove(); leases.Remove(pair.Key); }
                catch (Exception ex) { failures.Add(ex); }
            }
            if (failures.Count > 0) throw new AggregateException("Equipment effect removal is incomplete.", failures);
        }

        internal readonly struct EffectInput
        {
            internal EffectInput(int index, object item, string skill) { Index = index; Item = item; Skill = skill; }
            internal int Index { get; }
            internal object Item { get; }
            internal string Skill { get; }
        }

        private sealed class Lease
        {
            internal Lease(IDictionary dictionary, object item, string skill, object function)
            { Dictionary = dictionary; Item = item; Skill = skill; Function = function; }
            internal IDictionary Dictionary { get; }
            internal object Item { get; }
            internal string Skill { get; }
            internal object Function { get; }
            internal bool Active { get; set; }
            private bool removed;

            internal void Remove()
            {
                Active = false;
                if (ReferenceEquals(Dictionary[Item], Function)) Dictionary.Remove(Item);
                if (!removed)
                {
                    EquipmentSlotNativeItem.Call(Function, "OnRemove");
                    removed = true;
                }
            }
        }
    }
}
