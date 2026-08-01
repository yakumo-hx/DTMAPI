using System;
using System.Globalization;
using System.Reflection;

namespace DTMAPI.Zoom
{
    internal static class ZoomNativeAccess
    {
        internal static bool TryReadOrthographicSize(
            out double value)
        {
            object? camera = ReadStaticMember(
                typeof(DolocAPI),
                "mainCamera");
            object? raw = ReadMember(
                camera,
                "orthographicSize");
            try
            {
                value = raw == null
                    ? 0d
                    : Convert.ToDouble(
                        raw,
                        CultureInfo.InvariantCulture);
                return value > 0d;
            }
            catch
            {
                value = 0d;
                return false;
            }
        }

        internal static bool TryWriteOrthographicSize(
            double value)
        {
            object? camera = ReadStaticMember(
                typeof(DolocAPI),
                "mainCamera");
            return WriteMember(
                camera,
                "orthographicSize",
                value);
        }

        internal static bool IsMainCameraDefinitelyAbsent()
        {
            object? camera;
            try
            {
                camera = ReadStaticMember(
                    typeof(DolocAPI),
                    "mainCamera");
            }
            catch
            {
                return false;
            }
            if (camera == null)
                return true;

            try
            {
                for (Type? type = camera.GetType();
                     type != null;
                     type = type.BaseType)
                {
                    MethodInfo? aliveOperator =
                        type.GetMethod(
                            "op_Implicit",
                            BindingFlags.Public |
                            BindingFlags.Static,
                            null,
                            new[] { type },
                            null);
                    if (aliveOperator?.ReturnType ==
                        typeof(bool))
                    {
                        object? alive =
                            aliveOperator.Invoke(
                                null,
                                new[] { camera });
                        return alive is bool value &&
                            !value;
                    }
                }
            }
            catch
            {
                // Reflection failure does not prove that a live Unity camera
                // was destroyed.
            }
            return false;
        }

        private static object? ReadStaticMember(
            Type type,
            string name)
        {
            FieldInfo? field = type.GetField(
                name,
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Static);
            if (field != null)
                return field.GetValue(null);
            return type.GetProperty(
                    name,
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Static)?
                .GetValue(null);
        }

        private static object? ReadMember(
            object? instance,
            string name)
        {
            if (instance == null)
                return null;
            Type type = instance.GetType();
            FieldInfo? field = type.GetField(
                name,
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance);
            if (field != null)
                return field.GetValue(instance);
            return type.GetProperty(
                    name,
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance)?
                .GetValue(instance);
        }

        private static bool WriteMember(
            object? instance,
            string name,
            double value)
        {
            if (instance == null)
                return false;
            Type type = instance.GetType();
            FieldInfo? field = type.GetField(
                name,
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(
                    instance,
                    Convert.ChangeType(
                        value,
                        field.FieldType,
                        CultureInfo.InvariantCulture));
                return true;
            }
            PropertyInfo? property = type.GetProperty(
                name,
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance);
            if (property?.CanWrite != true)
                return false;
            property.SetValue(
                instance,
                Convert.ChangeType(
                    value,
                    property.PropertyType,
                    CultureInfo.InvariantCulture));
            return true;
        }
    }
}
