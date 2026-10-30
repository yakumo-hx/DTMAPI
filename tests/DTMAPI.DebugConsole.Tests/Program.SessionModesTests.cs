using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using DTMAPI.Core.Runtime;
using DTMAPI.DebugConsole;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {

        private static void DebugConsoleCustomTimeIsBoundedAndIndependent()
        {
            var config = new DebugConsoleConfig();
            Assert(config.CustomTimeMultiplier == 4, "Fresh/reset config defaults to four.");
            Assert(DebugConsoleConfig.Normalize(0) == 1 && DebugConsoleConfig.Normalize(21) == 20 &&
                DebugConsoleConfig.Normalize(double.NaN) == 4 && DebugConsoleConfig.Normalize(7.2) == 7,
                "Custom time config must remain an integer from one through twenty.");
        }

        private static void DebugConsoleFrozenCoreOwnerBoundaryDiagnostic()
        {
            var start = new ProcessStartInfo
            {
                FileName = Path.Combine(AppContext.BaseDirectory, "HarmonyOwnerFixture", "DebugConsoleHarmonyOwnerFixture.exe"),
                Arguments = "--owner-boundary-diagnostic", UseShellExecute = false,
                RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true
            };
            var result = DTMAPI.Testing.BoundedTestProcess.Run(start);
            Console.WriteLine(result.Output);
            Console.WriteLine(result.Error);
            Assert(result.ExitCode == 0 && result.Output.Contains("G19_OWNER_BOUNDARY="), "Owner boundary diagnostic did not finish.");
        }

        private sealed class ModeTestArchive { public ModeTestFarm farmData = new ModeTestFarm(); }
        private sealed class ModeTestFarm { public object agentData = new object(); }
    }
}
