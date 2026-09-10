using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.MoreEquipmentSlots;
using HarmonyLib;

namespace DTMAPI.EquipmentSlotsLegacyTargetFixture
{
    internal static class Program
    {
        private static int Main()
        {
            string sessionRoot =
                Environment.GetEnvironmentVariable(
                    "DTMAPI_TEST_SESSION_ROOT") ??
                throw new InvalidOperationException(
                    "The legacy-target fixture requires the managed test session.");
            string root = Path.Combine(
                sessionRoot,
                "equipment-slots-legacy-target-" +
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var runtime =
                new MoreEquipmentSlotsNativeRuntime(
                    NullMonitor.Instance,
                    Path.Combine(root, "config.json"));
            bool rejected = false;
            try
            {
                runtime.Configure(
                    new MoreEquipmentSlotsConfig
                    {
                        Enabled = true
                    },
                    "legacy 237 target fixture");
            }
            catch (MissingMethodException ex)
            {
                rejected = ex.Message.IndexOf(
                    "RenderPassiveItems",
                    StringComparison.Ordinal) >= 0;
            }

            int ownerPatches =
                Harmony.GetAllPatchedMethods().Count(
                    method => Harmony.GetPatchInfo(method)?
                        .Owners.Contains(
                            MoreEquipmentSlotsProductContract
                                .HarmonyOwner) == true);
            FieldInfo callbackRoot =
                typeof(MoreEquipmentSlotsCallbacks).GetField(
                    "runtime",
                    BindingFlags.NonPublic |
                    BindingFlags.Static) ??
                throw new MissingFieldException(
                    typeof(MoreEquipmentSlotsCallbacks).FullName,
                    "runtime");
            bool sidecarAbsent =
                !Directory.EnumerateFiles(
                    root,
                    "*.json",
                    SearchOption.AllDirectories).Any();
            MoreEquipmentSlotsDiagnosticsSnapshot snapshot =
                runtime.GetDiagnosticsSnapshot();
            if (!rejected ||
                ownerPatches != 0 ||
                callbackRoot.GetValue(null) != null ||
                !sidecarAbsent ||
                snapshot.PatchCount != 0 ||
                snapshot.CallbackCount != 0 ||
                snapshot.RootCount != 0)
            {
                throw new InvalidOperationException(
                    "Legacy 237 missing RenderPassiveItems did not fail atomically before hooks, callbacks, UI roots or sidecar authority.");
            }

            runtime.DeactivateOwner("RuntimeShutdown");
            Console.WriteLine(
                "EquipmentSlotsLegacyTargetFixture: OK");
            return 0;
        }
    }
}
