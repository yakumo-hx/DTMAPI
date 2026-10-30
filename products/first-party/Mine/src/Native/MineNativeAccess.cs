using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DTMAPI.Mine
{
    internal sealed partial class MineNativeRuntime
    {
        internal static int ReadIntMember(
            object instance,
            string name,
            int fallback)
        {
            object? value = ReadMember(instance, name);
            return value == null
                ? fallback
                : Convert.ToInt32(value);
        }

        internal static Type? ResolveType(
            string assemblyQualifiedName)
        {
            Type? type = Type.GetType(assemblyQualifiedName);
            if (type != null)
                return type;
            int comma =
                assemblyQualifiedName.IndexOf(',');
            string typeName = comma >= 0
                ? assemblyQualifiedName
                    .Substring(0, comma).Trim()
                : assemblyQualifiedName;
            foreach (Assembly assembly in
                     AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    type = assembly.GetType(
                        typeName,
                        throwOnError: false);
                    if (type != null)
                        return type;
                }
                catch
                {
                }
            }
            return null;
        }

        internal static object? ReadStaticMember(
            Type? type,
            string name)
        {
            if (type == null)
                return null;
            FieldInfo? field = type.GetField(
                name,
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Static);
            if (field != null)
            {
                try
                {
                    object? value = field.GetValue(null);
                    if (value != null)
                        return value;
                }
                catch
                {
                }
            }
            PropertyInfo? property = type.GetProperty(
                name,
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Static);
            if (property == null)
                return null;
            try
            {
                return property.GetValue(null);
            }
            catch
            {
                return null;
            }
        }

        internal static object? ReadMember(
            object instance,
            string name)
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
                    try
                    {
                        object? value =
                            field.GetValue(instance);
                        if (value != null)
                            return value;
                    }
                    catch
                    {
                    }
                }

                PropertyInfo? property = type.GetProperty(
                    name,
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance);
                if (property != null)
                {
                    try
                    {
                        object? value =
                            property.GetValue(instance);
                        if (value != null)
                            return value;
                    }
                    catch
                    {
                    }
                }
            }
            return null;
        }

        internal static bool SetMemberValue(
            object instance,
            string name,
            object? value)
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
                if (field != null &&
                    (value == null ||
                     field.FieldType.IsInstanceOfType(value)))
                {
                    field.SetValue(instance, value);
                    return true;
                }

                PropertyInfo? property = type.GetProperty(
                    name,
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance);
                if (property != null &&
                    property.CanWrite &&
                    (value == null ||
                     property.PropertyType
                         .IsInstanceOfType(value)))
                {
                    property.SetValue(instance, value);
                    return true;
                }
            }
            return false;
        }

        internal static MethodInfo? FindMethodInHierarchy(
            Type? type,
            string name,
            int parameterCount)
        {
            for (Type? current = type;
                 current != null;
                 current = current.BaseType)
            {
                foreach (MethodInfo method in
                         current.GetMethods(
                             BindingFlags.Public |
                             BindingFlags.NonPublic |
                             BindingFlags.Instance |
                             BindingFlags.Static))
                {
                    if (method.Name == name &&
                        method.GetParameters().Length ==
                        parameterCount)
                    {
                        return method;
                    }
                }
            }
            return null;
        }

        internal static string FirstText(
            params string[] values)
        {
            foreach (string value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }
            return string.Empty;
        }

        internal static string ReadStringMember(
            object instance,
            string name) =>
            ReadMember(instance, name) as string ??
            string.Empty;

        internal static string ReadStringMember(
            object instance,
            string name,
            string fallback)
        {
            string value =
                ReadStringMember(instance, name);
            return string.IsNullOrWhiteSpace(value)
                ? fallback
                : value;
        }

        internal static object? CreateUnityVector3(
            double x,
            double y,
            double z)
        {
            Type? vector3 =
                ResolveType(
                    "UnityEngine.Vector3, UnityEngine.CoreModule") ??
                ResolveType(
                    "UnityEngine.Vector3, UnityEngine");
            return vector3 == null
                ? null
                : Activator.CreateInstance(
                    vector3,
                    (float)x,
                    (float)y,
                    (float)z);
        }

        private static object? CreateUnityVector2Int(
            int x,
            int y)
        {
            Type? vector2Int =
                ResolveType(
                    "UnityEngine.Vector2Int, UnityEngine.CoreModule") ??
                ResolveType(
                    "UnityEngine.Vector2Int, UnityEngine");
            return vector2Int == null
                ? null
                : Activator.CreateInstance(vector2Int, x, y);
        }

        internal static double ReadVectorComponent(
            object? vector,
            string name)
        {
            if (vector == null)
                return double.NaN;
            object? value = ReadMember(vector, name);
            return value == null
                ? double.NaN
                : Convert.ToDouble(value);
        }

        internal static bool TryGenerateNativeItem(
            string itemId,
            int count,
            out object? item,
            out string reason,
            out string message)
        {
            item = null;
            reason = string.Empty;
            message = string.Empty;
            Type? itemFactory =
                ResolveType(
                    "DolocTown.ItemFactory, Assembly-CSharp");
            MethodInfo? generateItem =
                itemFactory?
                    .GetMethods(
                        BindingFlags.Public |
                        BindingFlags.Static)
                    .FirstOrDefault(method =>
                    {
                        if (!method.Name.Equals(
                                "GenerateItem",
                                StringComparison.Ordinal))
                        {
                            return false;
                        }
                        ParameterInfo[] parameters =
                            method.GetParameters();
                        return parameters.Length == 3 &&
                            parameters[0].ParameterType ==
                                typeof(string) &&
                            parameters[1].ParameterType ==
                                typeof(int) &&
                            parameters[2].ParameterType.IsByRef;
                    });
            if (generateItem == null)
            {
                reason = "missing-item-factory";
                message =
                    "ItemFactory.GenerateItem(string,int,out Item) was not found.";
                return false;
            }

            object?[] args =
            {
                itemId,
                Math.Max(1, count),
                null
            };
            object? result =
                generateItem.Invoke(null, args);
            item = args[2];
            if (!(result is bool success) ||
                !success ||
                item == null)
            {
                reason = "item-generation-failed";
                message =
                    "ItemFactory.GenerateItem failed for " +
                    itemId + ".";
                return false;
            }
            return true;
        }

        private static int ClampInt(
            int value,
            int min,
            int max) =>
            Math.Max(min, Math.Min(max, value));

        private static double ClampDouble(
            double value,
            double min,
            double max) =>
            Math.Max(min, Math.Min(max, value));
    }
}
