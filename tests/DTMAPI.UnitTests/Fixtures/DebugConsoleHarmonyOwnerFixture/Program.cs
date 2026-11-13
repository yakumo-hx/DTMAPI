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
    internal static partial class Program
    {
        private const string ProductOwner =
            "DTMAPI.DebugConsoleMod.Native";

        private static int Main(string[] args)
        {
            try
            {
                _ = typeof(DolocTown.AgentControllerState);
                if (args.Contains("--owner-boundary-diagnostic"))
                {
                    OwnerBoundaryDiagnostic();
                    return 0;
                }
                ProductOwnsAndReleasesPatches();
                PlatformPatcherCallsHarmonyDirectly();
                PlatformCleanupUsesPatchAssemblies();
                PlatformCleanupPreservesReentrantContributions();
                SessionModesRespectNativeOwners();
                OwnerBoundaryDiagnostic();
                Console.WriteLine(
                    "DebugConsoleHarmonyOwnerFixture: OK");
                return 0;
            }
            catch (Exception error)
            {
                Console.Error.WriteLine(error);
                Cleanup(ProductOwner);
                return 1;
            }
        }

        private static void
            ProductOwnsAndReleasesPatches()
        {
            CleanupAll();
            MethodInfo sharedTarget = typeof(DolocTown.AgentControllerState).GetMethods().Single(method => method.Name == "UseTool");
            MethodInfo foreignPatch = typeof(Program).GetMethod(nameof(ForeignPrefix), BindingFlags.Static | BindingFlags.NonPublic)!;
            new Harmony(ProductOwner).Patch(sharedTarget, prefix: new HarmonyMethod(foreignPatch));
            var product = new DebugConsoleHookInstaller();
            product.InstallAtomically();
            Assert(
                product.InstalledPatchCount == 12 && CountOwner(ProductOwner) == 13,
                "The product installs twelve actual patches even when another method uses the same Harmony ID.");
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
            product.Unpatch();
            Assert(
                product.InstalledPatchCount == 0 && CountOwner(ProductOwner) == 1 && Harmony.GetPatchInfo(sharedTarget)!.Prefixes.Any(patch => patch.PatchMethod == foreignPatch),
                "Product cleanup releases its own twelve methods and preserves the other contribution sharing its ID.");
            new Harmony(ProductOwner).Unpatch(sharedTarget, foreignPatch);
        }

        private static void ForeignPrefix() { }

        private static DtmApiRuntime CreateRuntime()
        {
            string sessionRoot = Environment.GetEnvironmentVariable(
                "DTMAPI_TEST_SESSION_ROOT") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(sessionRoot))
            {
                throw new InvalidOperationException(
                    "DebugConsoleHarmonyOwnerFixture requires the parent DTMAPI managed test session root.");
            }
            string root = Path.Combine(
                Path.GetFullPath(sessionRoot),
                "DebugConsoleHarmonyOwnerFixture",
                System.Diagnostics.Process.GetCurrentProcess().Id.ToString(
                    System.Globalization.CultureInfo.InvariantCulture));
            Directory.CreateDirectory(root);
            ConstructorInfo constructor = typeof(DtmApiRuntime).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single(candidate => candidate.GetParameters().Length == 3);
            return (DtmApiRuntime)constructor.Invoke(new object[] { new FakeHost(root), new ConfigMenuRegistry(), CreatePatchCleanup() });
        }

        private static int CountOwner(string owner)
        {
            return Harmony.GetAllPatchedMethods().Sum(target =>
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

        private static void CleanupAll()
        {
            Cleanup(ProductOwner);
            DebugConsoleInputGate.Reset();
            DebugConsoleMovementHooks.Reset();
            DebugConsoleMovementHooks.SessionIsCurrent = null;
            DebugConsoleModeHooks.Reset();
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
