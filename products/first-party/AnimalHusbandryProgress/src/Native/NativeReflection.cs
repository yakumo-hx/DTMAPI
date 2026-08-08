using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace Yuuka.DTMAPI.AnimalHusbandryProgress
{
    internal static class NativeReflection
    {
        internal static Type? ResolveType(string assemblyQualifiedName)
        {
            Type? type = Type.GetType(assemblyQualifiedName, false);
            if (type != null)
                return type;
            int comma = assemblyQualifiedName.IndexOf(',');
            string fullName = comma < 0 ? assemblyQualifiedName : assemblyQualifiedName.Substring(0, comma).Trim();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(fullName, false);
                if (type != null)
                    return type;
            }
            return null;
        }

        internal static object? ReadMember(object? instance, string name)
        {
            if (instance == null)
                return null;
            for (Type? type = instance.GetType(); type != null; type = type.BaseType)
            {
                PropertyInfo? property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property != null && property.GetIndexParameters().Length == 0)
                    return property.GetValue(instance);
                FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                    return field.GetValue(instance);
            }
            return null;
        }

        internal static object? ReadStaticMember(Type? type, string name)
        {
            if (type == null)
                return null;
            for (Type? current = type; current != null; current = current.BaseType)
            {
                PropertyInfo? property = current.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (property != null && property.GetIndexParameters().Length == 0)
                    return property.GetValue(null);
                FieldInfo? field = current.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (field != null)
                    return field.GetValue(null);
            }
            return null;
        }

        internal static bool SetMemberValue(object? instance, string name, object? value)
        {
            if (instance == null)
                return false;
            for (Type? type = instance.GetType(); type != null; type = type.BaseType)
            {
                PropertyInfo? property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property != null && property.CanWrite && property.GetIndexParameters().Length == 0)
                {
                    property.SetValue(instance, ConvertValue(value, property.PropertyType));
                    return true;
                }
                FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(instance, ConvertValue(value, field.FieldType));
                    return true;
                }
            }
            return false;
        }

        internal static string ReadStringMember(object? instance, string name)
            => ReadMember(instance, name) as string ?? string.Empty;

        internal static int ReadIntMember(object? instance, string name, int fallback)
        {
            object? value = ReadMember(instance, name);
            if (value == null)
                return fallback;
            try { return Convert.ToInt32(value, CultureInfo.InvariantCulture); }
            catch { return fallback; }
        }

        internal static bool ReadBoolMember(object? instance, string name, bool fallback)
        {
            object? value = ReadMember(instance, name);
            if (value == null)
                return fallback;
            try { return Convert.ToBoolean(value, CultureInfo.InvariantCulture); }
            catch { return fallback; }
        }

        internal static IEnumerable<object> EnumerateObjects(object? value)
        {
            if (!(value is IEnumerable enumerable))
                yield break;
            foreach (object? entry in enumerable)
                if (entry != null)
                    yield return entry;
        }

        internal static object? CloneUnityObject(object original)
        {
            Type? objectType = ResolveType("UnityEngine.Object, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Object, UnityEngine");
            MethodInfo? instantiate = objectType?.GetMethod("Instantiate", BindingFlags.Public | BindingFlags.Static, null, new[] { objectType }, null);
            return instantiate?.Invoke(null, new[] { original });
        }

        internal static void DestroyUnityObject(object instance)
        {
            Type? objectType = ResolveType("UnityEngine.Object, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Object, UnityEngine");
            MethodInfo? destroy = objectType?.GetMethod("Destroy", BindingFlags.Public | BindingFlags.Static, null, new[] { objectType }, null);
            destroy?.Invoke(null, new[] { instance });
        }

        internal static bool IsUnityObjectAlive(object? instance)
        {
            if (instance == null)
                return false;
            try
            {
                Type? objectType = ResolveType("UnityEngine.Object, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Object, UnityEngine");
                if (objectType != null && objectType.IsInstanceOfType(instance))
                {
                    MethodInfo? equality = objectType.GetMethod("op_Equality", BindingFlags.Public | BindingFlags.Static, null, new[] { objectType, objectType }, null);
                    if (equality?.Invoke(null, new object?[] { instance, null }) is bool isNull && isNull)
                        return false;
                }
                instance.GetType().GetMethod("GetInstanceID", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null)?.Invoke(instance, null);
                return true;
            }
            catch { return false; }
        }

        internal static void SafeSetActive(object? gameObject, bool active)
        {
            if (!IsUnityObjectAlive(gameObject))
                return;
            try { gameObject!.GetType().GetMethod("SetActive", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(bool) }, null)?.Invoke(gameObject, new object[] { active }); }
            catch { }
        }

        internal static void SafeDestroy(object? instance)
        {
            if (!IsUnityObjectAlive(instance))
                return;
            try { DestroyUnityObject(instance!); }
            catch { }
        }

        internal static object? GetComponent(object gameObject, Type componentType)
            => gameObject.GetType().GetMethod("GetComponent", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(Type) }, null)?.Invoke(gameObject, new object[] { componentType });

        internal static void SetParent(object transform, object parent, bool worldPositionStays)
        {
            foreach (MethodInfo method in transform.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (method.Name != "SetParent" || parameters.Length != 2 || parameters[1].ParameterType != typeof(bool) || !parameters[0].ParameterType.IsInstanceOfType(parent))
                    continue;
                method.Invoke(transform, new object[] { parent, worldPositionStays });
                return;
            }
            throw new MissingMethodException(transform.GetType().FullName, "SetParent(Transform, bool)");
        }

        internal static object? CreateVector3(double x, double y, double z)
        {
            Type? type = ResolveType("UnityEngine.Vector3, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Vector3, UnityEngine");
            ConstructorInfo? ctor = type?.GetConstructor(new[] { typeof(float), typeof(float), typeof(float) });
            return ctor?.Invoke(new object[] { (float)x, (float)y, (float)z });
        }

        internal static double ReadVectorComponent(object? vector, string name)
        {
            object? value = ReadMember(vector, name);
            if (value == null)
                return 0;
            try { return Convert.ToDouble(value, CultureInfo.InvariantCulture); }
            catch { return 0; }
        }

        internal static int DisableLocalizationComponents(object gameObject)
        {
            Type? componentType = ResolveType("UnityEngine.Component, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Component, UnityEngine");
            MethodInfo? getComponents = gameObject.GetType().GetMethod("GetComponentsInChildren", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(Type), typeof(bool) }, null);
            if (componentType == null || !(getComponents?.Invoke(gameObject, new object[] { componentType, true }) is IEnumerable components))
                return 0;
            int disabled = 0;
            foreach (object component in components)
            {
                string name = component.GetType().FullName ?? component.GetType().Name;
                if (name.IndexOf("Localization", StringComparison.OrdinalIgnoreCase) < 0 && name.IndexOf("Localisation", StringComparison.OrdinalIgnoreCase) < 0 && name.IndexOf("Localize", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                if (SetMemberValue(component, "enabled", false))
                    disabled++;
            }
            return disabled;
        }

        internal static void SetText(object? component, string value)
        {
            if (component == null)
                return;
            SetMemberValue(component, "text", value ?? string.Empty);
            SetMemberValue(component, "resizeTextForBestFit", false);
            SetMemberValue(component, "fontSize", 18);
            SetMemberValue(component, "resizeTextMinSize", 18);
            SetMemberValue(component, "resizeTextMaxSize", 18);
        }

        internal static void CaptureChildTextTargets(
            object gameObject,
            ICollection<object> titleTargets,
            ICollection<object> progressTargets)
        {
            if (titleTargets == null)
                throw new ArgumentNullException(nameof(titleTargets));
            if (progressTargets == null)
                throw new ArgumentNullException(nameof(progressTargets));
            Type? textType = ResolveType("UnityEngine.UI.Text, UnityEngine.UI");
            MethodInfo? getComponents = gameObject.GetType().GetMethod("GetComponentsInChildren", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(Type), typeof(bool) }, null);
            if (textType == null || !(getComponents?.Invoke(gameObject, new object[] { textType, true }) is IEnumerable components))
                return;
            foreach (object component in components)
            {
                string current = ReadStringMember(component, "text");
                if (current.IndexOf("/", StringComparison.Ordinal) >= 0 || current.IndexOf("0/100", StringComparison.Ordinal) >= 0)
                    progressTargets.Add(component);
                else if (!string.IsNullOrWhiteSpace(current))
                    titleTargets.Add(component);
            }
        }

        private static object? ConvertValue(object? value, Type targetType)
        {
            if (value == null || targetType.IsInstanceOfType(value))
                return value;
            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }
    }
}
