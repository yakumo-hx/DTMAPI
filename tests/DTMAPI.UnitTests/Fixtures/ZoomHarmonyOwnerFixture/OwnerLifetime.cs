using System;
using System.IO;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;
using DTMAPI.Core.Manifesting;
using DTMAPI.Zoom;
namespace DTMAPI.ZoomHarmonyOwnerFixture
{
    internal static class OwnerLifetime
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        internal static void Check()
        {
            string owner = "DTMAPI.ZoomMod";
            var runtime = new DtmApiRuntime(new Host());
            object Invoke(object value, string method, params object[] args) => value.GetType().GetMethod(method, Flags)!.Invoke(value, args)!;
            object Get(string name) => runtime.GetType().GetProperty(name, Flags)!.GetValue(runtime)!;
            Invoke(runtime, "BeginOwnerEntry", owner);
            Action ensure = () => Invoke(runtime, "EnsureModOwnerRegistrationAllowed", owner);
            var manifest = new ManifestModel { UniqueID = owner, Name = "Zoom exact lifecycle", Version = "1.1.0" };
            var helperType = runtime.GetType().Assembly.GetType("DTMAPI.Core.Services.DtmHelper", true)!;
            var helper = (IDtmHelper)Activator.CreateInstance(helperType, new object[] {
                manifest, NullMonitor.Instance, Invoke(Get("Events"), "CreateOwnerBoundProxy", owner, ensure),
                Invoke(Get("Config"), "CreateOwnerBound", manifest, ensure), Invoke(Get("ModRegistry"), "CreateOwnerBoundRegistry", manifest, ensure),
                null!, null!, null!, null!, Invoke(Get("Input"), "CreateOwnerBound", owner, ensure), new Translation(), null!
            });
            var product = new ModEntry();
            var instances = runtime.GetType().GetField("ownerInstances", Flags)!.GetValue(runtime)!;
            Invoke(instances, "Capture", owner, product);
            product.Entry(helper);
            Invoke(instances, "Publish", owner);
            Invoke(runtime, "ActivateOwnerEntry", manifest);
            var zoom = (ZoomNativeRuntime)typeof(ModEntry).GetField("runtime", Flags)!.GetValue(product)!;
            zoom.SetViewScale(4, "lifecycle test");
            var reasonType = runtime.GetType().Assembly.GetType("DTMAPI.Core.Runtime.ModOwnerCleanupReason", true)!;
            object summary = Invoke(runtime, "DeactivateOwner", owner, Enum.Parse(reasonType, "RuntimeShutdown"), true);
            string value = summary.ToString()!;
            Console.WriteLine(value);
            if (!value.Contains("modDisposeFailures=0") || !value.Contains("remaining=0") || zoom.CurrentViewScale != 1 || zoom.PendingRestoreCount != 0 || zoom.InstalledPatchCount != 0)
                throw new Exception("Actual Zoom Owner cleanup failed: " + value);
        }
        private sealed class Translation : ITranslationHelper { public string Language => "english"; public string Get(string key, string fallback = "") => fallback; }
        private sealed class Host : IRuntimeHost
        {
            private readonly string root = Path.Combine(Path.GetTempPath(), "DTMAPI-Zoom-owner-" + Guid.NewGuid().ToString("N"));
            public string GamePath => root;
            public string PluginPath => Path.Combine(root, "BepInEx", "plugins");
            public string HostName => "ZoomProductFixture";
            public void Log(string message) { }
            public void LogWarning(string message) { }
            public void LogError(string message, Exception? exception = null) { }
        }
    }
}
