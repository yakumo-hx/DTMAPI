using System;
using System.Reflection;

namespace DTMAPI.MoreEquipmentSlots
{
    internal static class MoreEquipmentSlotsReflectionAccess
    {
        private const BindingFlags DeclaredMembers =
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Instance |
            BindingFlags.Static |
            BindingFlags.DeclaredOnly;

        internal static object? Read(
            object? instance,
            string name)
        {
            if (instance == null)
                return null;

            FieldInfo? field =
                FindField(
                    instance.GetType(),
                    name);
            if (field != null)
                return field.GetValue(instance);

            PropertyInfo? property =
                FindProperty(
                    instance.GetType(),
                    name);
            return property?.GetValue(instance, null);
        }

        internal static object? ReadStatic(
            Type type,
            string name)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            FieldInfo? field =
                FindField(
                    type,
                    name,
                    requireStatic: true);
            if (field != null)
                return field.GetValue(null);

            PropertyInfo? property =
                FindProperty(
                    type,
                    name,
                    requireStatic: true);
            return property?.GetValue(null, null);
        }

        internal static bool Set(
            object? instance,
            string name,
            object? value)
        {
            if (instance == null)
                return false;

            FieldInfo? field =
                FindField(
                    instance.GetType(),
                    name);
            if (field != null)
            {
                field.SetValue(instance, value);
                return true;
            }

            PropertyInfo? property =
                FindProperty(
                    instance.GetType(),
                    name);
            if (property?.CanWrite != true)
                return false;
            property.SetValue(
                instance,
                value,
                null);
            return true;
        }

        private static FieldInfo? FindField(
            Type type,
            string name,
            bool requireStatic = false)
        {
            for (Type? current = type;
                 current != null;
                 current = current.BaseType)
            {
                foreach (FieldInfo field in
                    current.GetFields(
                        DeclaredMembers))
                {
                    if (field.Name.Equals(
                            name,
                            StringComparison.Ordinal) &&
                        (!requireStatic ||
                         field.IsStatic))
                    {
                        return field;
                    }
                }
            }

            return null;
        }

        private static PropertyInfo? FindProperty(
            Type type,
            string name,
            bool requireStatic = false)
        {
            for (Type? current = type;
                 current != null;
                 current = current.BaseType)
            {
                foreach (PropertyInfo property in
                    current.GetProperties(
                        DeclaredMembers))
                {
                    MethodInfo? getter =
                        property.GetGetMethod(true);
                    if (property.Name.Equals(
                            name,
                            StringComparison.Ordinal) &&
                        property.GetIndexParameters().Length == 0 &&
                        getter != null &&
                        (!requireStatic ||
                         getter.IsStatic))
                    {
                        return property;
                    }
                }
            }

            return null;
        }
    }
}
