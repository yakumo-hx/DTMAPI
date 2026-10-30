using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace DTMAPI.MoreEquipmentSlots
{
    internal sealed class EquipmentSlotNativeItem
    {
        internal static Type ResolveApiType()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type? found = assembly.GetType("DolocAPI", false);
                if (found != null) return found;
            }
            throw new TypeLoadException("DolocAPI");
        }

        private readonly Type api;
        internal EquipmentSlotNativeItem(Type api) => this.api = api;

        internal object Create(string id)
        {
            Type factory = api.Assembly.GetType("DolocTown.ItemFactory", true)!;
            foreach (MethodInfo method in factory.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                ParameterInfo[] p = method.GetParameters();
                if (method.Name != "GenerateItem" || p.Length != 3 ||
                    p[0].ParameterType != typeof(string) || !p[2].IsOut) continue;
                object?[] args = { id, 1, null };
                if (method.Invoke(null, args) is bool ok && ok && args[2] != null)
                    return args[2]!;
                throw new InvalidDataException("Native item factory rejected " + id + ".");
            }
            throw new MissingMethodException(factory.FullName, "GenerateItem");
        }

        internal EquipmentSlotItemState Capture(object item, string? instanceId = null)
        {
            var state = new EquipmentSlotItemState
            {
                InstanceId = instanceId ?? Guid.NewGuid().ToString("N"),
                NativeType = item.GetType().FullName ?? string.Empty
            };
            if (ReadInt(item, "count") != 1)
                throw new InvalidDataException("Equipment requires one exact native item.");
            if (state.NativeType == "DolocTown.ItemHerbPackage")
                state.Filled = Read(item, "isFilled") is bool filled ? filled :
                    throw new InvalidDataException("Unreadable native herb-package state.");
            if (state.NativeType == "DolocTown.ItemCooldown")
                state.Cooldown = ReadInt(item, "cdDuration");
            if (!state.IsValid)
                throw new InvalidDataException("Unsupported native equipment state: " + state.NativeType);
            return state;
        }

        internal object Restore(EquipmentSlotStorageEntry slot)
        {
            object item = Create(slot.ItemId);
            EquipmentSlotItemState? state = slot.ItemState;
            if (state != null && (!state.IsValid || item.GetType().FullName != state.NativeType))
                throw new InvalidDataException("Native item type differs from the protected snapshot.");
            if (slot.IsShield) Call(item, "SetShieldValue", slot.ShieldValue);
            if (state?.Filled is bool filled) Call(item, "SetFilled", filled);
            if (state?.Cooldown is int cooldown &&
                !MoreEquipmentSlotsReflectionAccess.Set(item, "cdDuration", cooldown))
                throw new MissingMemberException(item.GetType().FullName, "cdDuration");
            return item;
        }

        internal object? FindBackpack(string id)
        {
            object inventory = Backpack();
            if (!(Call(inventory, "ReadAll") is IEnumerable items))
                throw new InvalidDataException("Native backpack items are unreadable.");
            foreach (object item in items)
                if (item != null && string.Equals(Read(item, "name") as string, id, StringComparison.Ordinal))
                    return item;
            return null;
        }

        internal object Backpack() =>
            Read(Read(MoreEquipmentSlotsReflectionAccess.ReadStatic(api, "archiveHandle"), "InventorySystem"), "inventory") ??
            throw new InvalidDataException("Native backpack is unavailable.");

        internal object? Withdraw(object item)
        {
            // Invoke the native instance-specific withdrawal, not CostItem(id).
            return Call(item, "CostSelfFromInventory", Backpack(), false);
        }

        internal bool Place(EquipmentSlotStorageEntry slot)
        {
            object item = Restore(slot);
            Type nativeItemType = api.Assembly.GetType("DolocTown.Item", true)!;
            MethodInfo method = api.GetMethod("TryPlaceInBackpack", new[] { nativeItemType, typeof(bool) }) ??
                throw new MissingMethodException(api.FullName, "TryPlaceInBackpack(Item,bool)");
            return method.Invoke(null, new[] { item, (object)false }) is bool ok && ok;
        }

        internal static object? Read(object? target, string name) =>
            MoreEquipmentSlotsReflectionAccess.Read(target, name);

        internal static int ReadInt(object target, string name) =>
            Convert.ToInt32(Read(target, name) ?? throw new MissingMemberException(target.GetType().FullName, name),
                CultureInfo.InvariantCulture);

        internal static object? Call(object target, string name, params object[] args)
        {
            foreach (MethodInfo method in target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                ParameterInfo[] p = method.GetParameters();
                if (method.Name != name || p.Length != args.Length) continue;
                bool match = true;
                for (int i = 0; i < p.Length; i++)
                    match &= args[i] == null || p[i].ParameterType.IsInstanceOfType(args[i]);
                if (match) return method.Invoke(target, args);
            }
            throw new MissingMethodException(target.GetType().FullName, name);
        }
    }
}
