using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DTMAPI.Mine
{
    internal sealed partial class MineNativeRuntime
    {
        private void CaptureMemberRestore(
            string key,
            object instance,
            params string[] memberNames)
        {
            if (nativeRestores.ContainsKey(key))
                return;
            foreach (string memberName in memberNames)
            {
                if (!TryReadMemberExact(
                        instance,
                        memberName,
                        out object? original))
                {
                    continue;
                }

                RegisterNativeRestore(
                    key,
                    () =>
                    {
                        if (!SetMemberValue(
                                instance,
                                memberName,
                                original))
                        {
                            throw new InvalidOperationException(
                                "Mine could not restore exact native member " +
                                instance.GetType().FullName +
                                "." + memberName + ".");
                        }
                    });
                return;
            }
            throw new MissingMemberException(
                instance.GetType().FullName,
                string.Join("|", memberNames));
        }

        private void CaptureDictionaryEntryRestore(
            string key,
            IDictionary dictionary,
            object entryKey)
        {
            if (nativeRestores.ContainsKey(key))
                return;
            bool existed = dictionary.Contains(entryKey);
            object? original =
                existed ? dictionary[entryKey] : null;
            RegisterNativeRestore(
                key,
                () =>
                {
                    if (existed)
                        dictionary[entryKey] = original;
                    else
                        dictionary.Remove(entryKey);
                });
        }

        private void CaptureListAdditionRestore(
            string key,
            IList list,
            object value)
        {
            if (nativeRestores.ContainsKey(key))
                return;
            RegisterNativeRestore(
                key,
                () =>
                {
                    if (list.Contains(value))
                        list.Remove(value);
                });
        }

        private void RegisterNativeRestore(
            string key,
            Action restore)
        {
            nativeRestores.Add(key, restore);
            nativeRestoreOrder.Add(key);
        }

        private void RestoreNativeMutations()
        {
            var failures = new List<Exception>();
            for (int i = nativeRestoreOrder.Count - 1;
                 i >= 0;
                 i--)
            {
                string key = nativeRestoreOrder[i];
                if (!nativeRestores.TryGetValue(
                        key,
                        out Action? restore))
                {
                    nativeRestoreOrder.RemoveAt(i);
                    continue;
                }
                try
                {
                    restore();
                    nativeRestores.Remove(key);
                    nativeRestoreOrder.RemoveAt(i);
                }
                catch (Exception ex)
                {
                    failures.Add(
                        new InvalidOperationException(
                            "Mine native restoration failed for " +
                            key + ".",
                            ex));
                }
            }
            if (failures.Count > 0)
            {
                throw new AggregateException(
                    "Mine could not restore all exact recipe/tech originals; " +
                    nativeRestores.Count +
                    " restoration item(s) remain queued for retry.",
                    failures);
            }
        }

        private void CaptureScale(object transform)
        {
            if (scaleSnapshots.ContainsKey(transform))
                return;
            object? original =
                ReadMember(transform, "localScale");
            if (original == null)
            {
                throw new MissingMemberException(
                    transform.GetType().FullName,
                    "localScale");
            }
            scaleSnapshots.Add(transform, original);
        }

        private void RestoreScale(object transform)
        {
            if (!scaleSnapshots.TryGetValue(
                    transform,
                    out object original))
            {
                return;
            }
            if (!SetMemberValue(
                    transform,
                    "localScale",
                    original))
            {
                throw new InvalidOperationException(
                    "Mine could not restore exact renderer localScale.");
            }
            scaleSnapshots.Remove(transform);
        }

        private void RestoreVisualScales()
        {
            var failures = new List<Exception>();
            foreach (KeyValuePair<object, object> entry in
                     new List<KeyValuePair<object, object>>(
                         scaleSnapshots))
            {
                try
                {
                    if (!SetMemberValue(
                            entry.Key,
                            "localScale",
                            entry.Value))
                    {
                        throw new InvalidOperationException(
                            "localScale member rejected its original value.");
                    }
                    scaleSnapshots.Remove(entry.Key);
                }
                catch (Exception ex)
                {
                    failures.Add(ex);
                }
            }
            if (failures.Count > 0)
            {
                throw new AggregateException(
                    "Mine could not restore all renderer/preview scales.",
                    failures);
            }
        }

        internal void RestoreRendererOnReuse(object renderer)
        {
            if (!active || disposed)
                return;
            try
            {
                object? transform =
                    ReadMember(renderer, "transform");
                if (transform != null)
                    RestoreScale(transform);
            }
            catch (Exception ex)
            {
                status = "runtime-failed";
                lastMessage =
                    "Mine renderer reuse cleanup failed: " +
                    ex.GetType().Name + ": " + ex.Message;
                runtime.Diagnostics.RecordError(
                    "DTMAPI.Mine",
                    "Mine renderer reuse cleanup failed; the exact original scale remains queued for owner cleanup.",
                    ex.ToString());
            }
        }

        private static MachineCycleSnapshot
            CaptureMachineCycleSnapshot(object equipment)
        {
            object? component =
                ReadMember(equipment, "IElectronicComponent");
            object? power = null;
            if (component != null)
            {
                if (!TryReadMemberExact(
                        component,
                        "power",
                        out power) ||
                    power == null)
                {
                    throw new MissingMemberException(
                        component.GetType().FullName,
                        "power");
                }
            }

            object? inventory =
                ReadMember(equipment, "inventory");
            object? inventoryCopy = null;
            if (inventory != null)
            {
                MethodInfo? copy =
                    FindMethodInHierarchy(
                        inventory.GetType(),
                        "Copy",
                        0);
                inventoryCopy = copy?.Invoke(inventory, null);
                if (inventoryCopy == null)
                {
                    throw new InvalidOperationException(
                        "Mine could not snapshot native case inventory before production.");
                }
            }

            return new MachineCycleSnapshot(
                component,
                power,
                inventory,
                inventoryCopy);
        }

        private static void RestoreMachineCycleSnapshot(
            MachineCycleSnapshot snapshot)
        {
            var failures = new List<Exception>();
            if (snapshot.Component != null &&
                snapshot.Power != null)
            {
                try
                {
                    if (!SetMemberValue(
                            snapshot.Component,
                            "power",
                            snapshot.Power))
                    {
                        throw new InvalidOperationException(
                            "Native electric power field rejected its original value.");
                    }
                }
                catch (Exception ex)
                {
                    failures.Add(ex);
                }
            }

            if (snapshot.Inventory != null &&
                snapshot.InventoryCopy != null)
            {
                try
                {
                    MethodInfo? overwrite =
                        snapshot.Inventory.GetType()
                            .GetMethods(
                                BindingFlags.Public |
                                BindingFlags.NonPublic |
                                BindingFlags.Instance)
                            .FirstOrDefault(method =>
                            {
                                if (method.Name != "Overwrite")
                                    return false;
                                ParameterInfo[] parameters =
                                    method.GetParameters();
                                return parameters.Length == 2 &&
                                    parameters[0].ParameterType
                                        .IsInstanceOfType(
                                            snapshot.InventoryCopy) &&
                                    parameters[1].ParameterType ==
                                        typeof(bool);
                            });
                    if (overwrite == null)
                    {
                        throw new MissingMethodException(
                            snapshot.Inventory.GetType().FullName,
                            "Overwrite(LinearInventory,bool)");
                    }
                    overwrite.Invoke(
                        snapshot.Inventory,
                        new[]
                        {
                            snapshot.InventoryCopy,
                            (object)true
                        });
                }
                catch (Exception ex)
                {
                    failures.Add(ex);
                }
            }

            if (failures.Count > 0)
            {
                throw new AggregateException(
                    "Mine production rollback could not restore exact native power/inventory.",
                    failures);
            }
        }

        private static bool TryReadMemberExact(
            object instance,
            string name,
            out object? value)
        {
            for (Type? type = instance.GetType();
                 type != null;
                 type = type.BaseType)
            {
                FieldInfo? field = type.GetField(
                    name,
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance);
                if (field != null)
                {
                    value = field.GetValue(instance);
                    return true;
                }
                PropertyInfo? property = type.GetProperty(
                    name,
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance);
                if (property != null)
                {
                    value = property.GetValue(instance);
                    return true;
                }
            }
            value = null;
            return false;
        }

        private sealed class MachineCycleSnapshot
        {
            internal MachineCycleSnapshot(
                object? component,
                object? power,
                object? inventory,
                object? inventoryCopy)
            {
                Component = component;
                Power = power;
                Inventory = inventory;
                InventoryCopy = inventoryCopy;
            }

            internal object? Component { get; }
            internal object? Power { get; }
            internal object? Inventory { get; }
            internal object? InventoryCopy { get; }
        }
    }
}
