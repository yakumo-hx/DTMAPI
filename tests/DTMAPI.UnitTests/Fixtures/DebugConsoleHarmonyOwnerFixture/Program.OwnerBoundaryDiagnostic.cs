using System;
using System.Linq;
using System.Reflection;
using DTMAPI.Core.Manifesting;
using DTMAPI.DebugConsole;

namespace DTMAPI.DebugConsoleHarmonyOwnerFixture
{
    internal static partial class Program
    {
        // Core's real DeactivateOwner uses GameBridge's patch cleanup, including
        // the product's preparation-failure retry boundary.
        private static void OwnerBoundaryDiagnostic()
        {
            CleanupAll();
            const string ownerId = "DTMAPI.DebugConsoleMod";
            var runtime = CreateRuntime();
            Type loaderType = runtime.GetType().Assembly.GetType("DTMAPI.Core.Runtime.DependencyAssemblyLoader", true)!;
            object loader = Activator.CreateInstance(loaderType, new object[] {
                Array.Empty<DiscoveredMod>(), Array.Empty<System.Collections.Generic.KeyValuePair<string, string>>(),
                new Action<string>(_ => { }), System.IO.Path.GetTempPath(), null! })!;
            SetField(runtime, "activeDependencyLoader", loader);
            Invoke(loader, "RecordBinding", ownerId, typeof(ModEntry).Assembly);
            var installer = new DebugConsoleHookInstaller();
            installer.InstallAtomically();
            var actions = new DebugConsoleNativeActions(new ModeRuntime());
            var entry = new ModEntry();
            SetField(entry, "actions", actions); SetField(entry, "hooks", installer);
            // Real god/movement demand has an active subscription. Preparation
            // must not request the owner-bound Events helper while Deactivating.
            SetField(entry, "updateSubscribed", true);
            var store = Field(runtime, "ownerInstances");
            Invoke(store, "Capture", ownerId, entry); Invoke(store, "Publish", ownerId);
            Invoke(runtime, "MarkManagedAssemblyLoadAttempt", ownerId);
            DolocAPI.archiveHandle = new DolocTown.GameData.ArchiveDataHandle();
            var manager = new DolocTown.TimeScaleManager(); DolocAPI.timeScaleManager = manager;
            DolocAPI.agent = new DolocTown.BodyController();
            actions.BeginSaveSession(); DebugConsoleModeHooks.Actions = actions;
            var owner = new ManifestModel { UniqueID = ownerId, Name = "Y", Version = "1.1.5", Author = "DTMAPI" };
            Invoke(runtime, "BeginOwnerEntry", ownerId);
            Invoke(runtime, "ActivateOwnerEntry", owner);
            Assert(actions.SetTimeScale(owner, 4).Success && actions.SetGodMode(owner, true).Success, "Diagnostic setup failed.");
            manager.FailWrite = true;
            MethodInfo deactivate = runtime.GetType().GetMethod("DeactivateOwner", BindingFlags.Instance | BindingFlags.NonPublic)!;
            object reason = Enum.Parse(deactivate.GetParameters()[1].ParameterType, "EntryFailed");
            string first = (string)deactivate.Invoke(runtime, new[] { (object)ownerId, reason, false })!;
            Assert(first.Contains("deactivationDeferred=true") && CountOwner(ProductOwner) == 12 && manager.Value == 4 && !actions.GodModeEnabled,
                "Core preparation did not retain observers with inactive effects: " + first);
            Console.WriteLine("first=" + first);
            Console.WriteLine("after-first: actual-product-hooks=" + CountOwner(ProductOwner) + "; time=4; god=false; retained-disposed=" + Field(entry, "disposed"));
            manager.SetTimeScale(4, false);
            int beforeRetry = manager.Writes;
            string second = (string)deactivate.Invoke(runtime, new[] { (object)ownerId, reason, false })!;
            Console.WriteLine("later-native-same-value=4; retry-value=" + manager.Value + "; retry-writes=" + (manager.Writes - beforeRetry));
            Console.WriteLine("second=" + second);
            Assert(manager.Value == 4 && manager.Writes == beforeRetry && second.Contains("modDisposeFailures=0") &&
                second.Contains("remaining=0") && CountOwner(ProductOwner) == 0 && (bool)Field(entry, "disposed"),
                "Preparation retry overwrote native same-value control or left cleanup roots.");
            Console.WriteLine("G19_OWNER_BOUNDARY=PRESERVED_NATIVE_CONTROL; FINAL_ROOTS_AND_HOOKS=0");
            installer.Unpatch();
        }

        private static object Field(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target)!;
        private static void SetField(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);
        private static object? Invoke(object target, string name, params object[] args) => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.Invoke(target, args);
    }
}
