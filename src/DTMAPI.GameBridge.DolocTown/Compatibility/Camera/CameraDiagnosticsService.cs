#pragma warning disable CS0618 // Frozen camera ABI diagnostics.
using System;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class CameraDiagnosticsService
    {
        private readonly DtmApiRuntime runtime;

        public CameraDiagnosticsService(DtmApiRuntime runtime)
        {
            this.runtime = runtime;
        }

        public void PublishHookStatuses()
        {
            runtime.SetHookStatus("Camera.ViewApi", "contract", "DTMAPI.GameBridge.DolocTown CameraFeature", "0.4.2 experimental playable camera view contract uses owner leases, DTMAPI arbitration, and orthographic-size-only writes. It does not call CameraController.RefreshResolution/SetPosition, DolocAPI.RefreshScanner, or panorama background/fog compensation.");
            runtime.SetHookStatus("Camera.ZoomApi", "obsolete-compatibility", "ICameraZoomApi -> ICameraViewApi", "ICameraZoomApi is obsolete and redirects to lease-based ICameraViewApi playable zoom.");
        }

        public void SetEnvironmentLifecyclePatched(bool patched)
        {
            runtime.SetHookStatus("Camera.ViewEnvironmentLifecycle", patched ? "experimental" : "pending", "Harmony Postfix: DolocAPI.SetEnvCamera", patched ? "Patched the native environment-camera reset boundary so active CameraView leases can reapply orthographic-size-only playable zoom after room transitions." : "Waiting for DolocAPI.SetEnvCamera to become patchable.");
        }

        public void SetViewPending(string message)
        {
            runtime.SetHookStatus("Camera.ViewApi", "pending", "DolocAPI.mainCamera.orthographicSize", message);
        }

        public void SetViewFailed(string message)
        {
            runtime.SetHookStatus("Camera.ViewApi", "failed", "DolocAPI.mainCamera.orthographicSize", message);
        }

        public void SetViewApplied(CameraViewResult result)
        {
            runtime.SetHookStatus("Camera.ViewApi", result.AppliedViewScale > 1.0001d ? "verified" : "vanilla", "DolocAPI.mainCamera.orthographicSize", result.Message);
            runtime.SetHookStatus("Camera.ZoomApi", "obsolete-compatibility", "ICameraZoomApi -> ICameraViewApi", "Obsolete CameraZoom calls are redirected to lease-based playable CameraView; CameraController.RefreshResolution/SetPosition, DolocAPI.RefreshScanner, background compensation, and fog compensation are not called.");
        }
    }

    internal static class CameraNativeReflection
    {
        public static double ClampDouble(double value, double min, double max)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return min;
            if (value < min)
                return min;
            return value > max ? max : value;
        }

        public static Type? ResolveType(string assemblyQualifiedName)
        {
            Type? type = Type.GetType(assemblyQualifiedName);
            if (type != null)
                return type;
            int comma = assemblyQualifiedName.IndexOf(',');
            string typeName = comma >= 0 ? assemblyQualifiedName.Substring(0, comma).Trim() : assemblyQualifiedName;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    type = assembly.GetType(typeName, throwOnError: false);
                    if (type != null)
                        return type;
                }
                catch
                {
                }
            }
            return null;
        }

        public static object? ReadStaticMember(Type? type, string name)
        {
            if (type == null)
                return null;

            FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
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

            PropertyInfo? property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (property != null)
            {
                try
                {
                    return property.GetValue(null);
                }
                catch
                {
                }
            }

            return null;
        }

        public static object? ReadMember(object instance, string name)
        {
            for (Type? type = instance.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    try
                    {
                        object? value = field.GetValue(instance);
                        if (value != null)
                            return value;
                    }
                    catch
                    {
                    }
                }

                PropertyInfo? property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property != null)
                {
                    try
                    {
                        object? value = property.GetValue(instance);
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

        public static string FirstText(params string[] values)
        {
            foreach (string value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }
            return string.Empty;
        }

        public static string ReadStringMember(object instance, string name)
        {
            object? value = ReadMember(instance, name);
            return value as string ?? string.Empty;
        }

        public static bool ReadBoolMember(object instance, string name, bool fallback)
        {
            object? value = ReadMember(instance, name);
            return value is bool result ? result : fallback;
        }
    }
}

#pragma warning restore CS0618
