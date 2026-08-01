using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DTMAPI.Core.Runtime;
using DTMAPI.DebugConsole;
using DTMAPI.ModConfigMenu;
using HarmonyLib;

namespace DTMAPI.DebugConsoleHarmonyOwnerFixture
{
    internal static class Program
    {
        private const string ProductOwner =
            "dtmapi.mod.dtmapi.debugconsolemod";
        private const string CompatibilityOwner =
            "dtmapi.compatibility.debugconsole.legacy";

        private static int Main()
        {
            try
            {
                _ = typeof(DolocTown.AgentControllerState);
                ProductFirstRejectsCompatibility();
                CompatibilityFirstRejectsProduct();
                Console.WriteLine(
                    "DebugConsoleHarmonyOwnerFixture: OK");
                return 0;
            }
            catch (Exception error)
            {
                Console.Error.WriteLine(error);
                Cleanup(ProductOwner);
                Cleanup(CompatibilityOwner);
                return 1;
            }
        }

        private static void
            ProductFirstRejectsCompatibility()
        {
            CleanupAll();
            var product = new DebugConsoleHookInstaller();
            product.InstallAtomically();
            Assert(
                CountOwner(ProductOwner) == 19,
                "Product-first did not install exactly nineteen real Harmony patches.");
            var player = new DolocTown.BodyController
            {
                NativeMoveSpeed = 10f,
                MoveScaler = 0.35f
            };
            var npc = new DolocTown.BodyController
            {
                NativeMoveSpeed = 7f,
                MoveScaler = 0.8f
            };
            DolocAPI.agent = player;
            DebugConsoleMovementHooks.SetMultiplier(player, 2d);
            Assert(
                Math.Abs(player.MoveSpeed - 20f) < 0.0001f &&
                Math.Abs(npc.MoveSpeed - 7f) < 0.0001f &&
                Math.Abs(player.MoveScaler - 0.35f) < 0.0001f,
                "Product final-speed multiplier did not affect only the player while preserving native MoveScaler.");
            player.NativeMoveSpeed = 12.5f;
            Assert(
                Math.Abs(player.MoveSpeed - 25f) < 0.0001f,
                "Product multiplier must compose with a later native/Buff speed change.");
            DebugConsoleMovementHooks.Reset();
            Assert(
                Math.Abs(player.MoveSpeed - 12.5f) < 0.0001f &&
                Math.Abs(player.MoveScaler - 0.35f) < 0.0001f,
                "Product movement reset must remove only its final-speed factor.");
            var compatibility =
                new CompatibilityDebugConsoleHookOwner(
                    CreateRuntime(),
                    installNativeHooks: true);
            AssertThrows<InvalidOperationException>(
                () => compatibility.SetModalOpen(true),
                "Compatibility did not reject the active ProductNative owner.");
            Assert(
                CountOwner(ProductOwner) == 19 &&
                CountOwner(CompatibilityOwner) == 0 &&
                !CompatibilityDebugConsoleInputHooks
                    .ModalOpen,
                "Rejected Compatibility install changed the product owner or retained desired/physical state.");
            product.Unpatch();
            Assert(
                CountOwner(ProductOwner) == 0,
                "Product-first cleanup did not reach zero.");
        }

        private static void
            CompatibilityFirstRejectsProduct()
        {
            CleanupAll();
            var compatibility =
                new CompatibilityDebugConsoleHookOwner(
                    CreateRuntime(),
                    installNativeHooks: true);
            compatibility.SetModalOpen(true);
            var player = new DolocTown.BodyController
            {
                NativeMoveSpeed = 9f,
                MoveScaler = 0.25f
            };
            DolocAPI.agent = player;
            compatibility.SetMovementMultiplier(player, 3d);
            Assert(
                CountOwner(CompatibilityOwner) == 4 &&
                compatibility.InstalledPatchCount == 4 &&
                Math.Abs(player.MoveSpeed - 27f) < 0.0001f &&
                Math.Abs(player.MoveScaler - 0.25f) < 0.0001f,
                "Compatibility-first did not install three input Prefixes plus one final-speed Postfix without changing MoveScaler.");
            var product = new DebugConsoleHookInstaller();
            AssertThrows<InvalidOperationException>(
                product.InstallAtomically,
                "ProductNative did not reject the active Compatibility owner.");
            Assert(
                CountOwner(CompatibilityOwner) == 4 &&
                CountOwner(ProductOwner) == 0,
                "Rejected ProductNative install changed the Compatibility owner.");
            compatibility.Shutdown(
                "compatibility-first fixture cleanup");
            Assert(
                CountOwner(CompatibilityOwner) == 0 &&
                compatibility.InstalledPatchCount == 0,
                "Compatibility exact-target cleanup did not reach zero.");
        }

        private static DtmApiRuntime CreateRuntime()
        {
            string root = Path.Combine(
                Path.GetTempPath(),
                "DebugConsoleHarmonyOwnerFixture");
            Directory.CreateDirectory(root);
            return new DtmApiRuntime(
                new FakeHost(root),
                new ConfigMenuRegistry());
        }

        private static int CountOwner(string owner)
        {
            return Targets().Sum(target =>
            {
                Patches? patches =
                    Harmony.GetPatchInfo(target);
                if (patches == null)
                    return 0;
                return patches.Prefixes.Count(
                           patch => patch.owner == owner) +
                    patches.Postfixes.Count(
                           patch => patch.owner == owner) +
                    patches.Transpilers.Count(
                           patch => patch.owner == owner) +
                    patches.Finalizers.Count(
                           patch => patch.owner == owner);
            });
        }

        private static MethodBase[] Targets()
        {
            Type input = typeof(
                DolocTown.AgentControllerState);
            Type api = typeof(DolocAPI);
            Type synthesizer = typeof(DolocTown.Synthesizer);
            Type bodyController = typeof(DolocTown.BodyController);
            return new MethodBase[]
            {
                Resolve(input, "UseTool", 1),
                Resolve(input, "UseItem", 1),
                Resolve(input, "EnterUICheck", 2),
                Resolve(api, "CostEnergy", 1),
                Resolve(api, "CostToolEnergy", 0),
                Resolve(api, "HasEnoughEnergy", 1),
                Resolve(api, "HasEnoughEnergyForUsingTool", 0),
                Resolve(api, "CostItem", 3),
                Resolve(api, "CostItem", 4),
                Resolve(api, "CostItemNoCheck", 2),
                Resolve(api, "CostItemNoCheck", 3),
                Resolve(api, "CostSelectedItem", 2),
                Resolve(api, "CostSelectedItem", 3),
                Resolve(api, "CostItemAt", 2),
                Resolve(api, "CanAfford", 2),
                Resolve(api, "CanAfford", 3),
                Resolve(api, "CanAffordMoney", 1),
                Resolve(synthesizer, "GetRecipeTime", 2),
                Resolve(bodyController, "get_MoveSpeed", 0)
            };
        }

        private static MethodInfo Resolve(
            Type type,
            string name,
            int parameterCount) =>
            type.GetMethods(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance |
                    BindingFlags.Static)
                .Single(method =>
                    method.Name == name &&
                    method.GetParameters().Length ==
                        parameterCount);

        private static void CleanupAll()
        {
            Cleanup(ProductOwner);
            Cleanup(CompatibilityOwner);
            CompatibilityDebugConsoleInputHooks.Reset();
            DebugConsoleInputGate.Reset();
            DebugConsoleCreativeHooks.Reset();
            DebugConsoleMovementHooks.Reset();
        }

        private static void Cleanup(string owner) =>
            Harmony.UnpatchID(owner);

        private static void Assert(
            bool condition,
            string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static void AssertThrows<TException>(
            Action action,
            string message)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            throw new InvalidOperationException(message);
        }

        private sealed class FakeHost : IRuntimeHost
        {
            internal FakeHost(string gamePath)
            {
                GamePath = gamePath;
                PluginPath = Path.Combine(
                    gamePath,
                    "BepInEx",
                    "plugins");
            }

            public string GamePath { get; }
            public string PluginPath { get; }
            public string HostName => "DebugConsoleFixture";
            public void Log(string message)
            {
            }

            public void LogWarning(string message)
            {
            }

            public void LogError(
                string message,
                Exception? exception = null)
            {
            }
        }
    }
}
