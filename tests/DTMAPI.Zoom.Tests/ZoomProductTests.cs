using System;
using System.Diagnostics;
using System.IO;
namespace DTMAPI.UnitTests
{
    internal static class ZoomProductTests
    {
        internal static void RunAll() => PhysicalHarmonyFixtureCoversBothOrdersRollbackAndExactUnpatch();
        private static string FindRepositoryRoot() => Program.FindRepositoryRootForZoom();
        private static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        private static void PhysicalHarmonyFixtureCoversBothOrdersRollbackAndExactUnpatch()
        {
            string executable = Environment.GetEnvironmentVariable("ZOOM_HARMONY_FIXTURE") ??
                Path.Combine(
                    FindRepositoryRoot(),
                    "tests",
                    "DTMAPI.UnitTests",
                    "Fixtures",
                    "ZoomHarmonyOwnerFixture",
                    "bin",
                    "Release",
                    "net48",
                    "ZoomHarmonyOwnerFixture.exe");
            if (!File.Exists(executable))
            {
                throw new FileNotFoundException(
                    "The focused Unit build did not produce the physical Zoom Harmony owner fixture.",
                    executable);
            }

            var start = new ProcessStartInfo
            {
                FileName = executable,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            start.ArgumentList.Add("--output-flood-fixture");
            var flood = DTMAPI.Testing.BoundedTestProcess.Run(start);
            Assert(flood.ExitCode == 0 && flood.Output.Length >= 131072 && flood.Error.Length >= 131072,
                "The process helper must drain both pipes before waiting for exit.");
            start.ArgumentList.Clear();
            start.ArgumentList.Add("--hang-fixture");
            var watch = Stopwatch.StartNew();
            bool timedOut = false;
            try { DTMAPI.Testing.BoundedTestProcess.Run(start, 500); }
            catch (TimeoutException) { timedOut = true; }
            Assert(timedOut && watch.Elapsed.TotalSeconds < 10, "The process helper must terminate its hanging child within the deadline.");
            start.ArgumentList.Clear();
            start.Environment["ZOOM_TEST_CORE"] = typeof(DTMAPI.Core.Json.JsonFile).Assembly.Location;
            var result = DTMAPI.Testing.BoundedTestProcess.Run(start);
            string output = result.Output;
            string error = result.Error;
            Assert(
                result.ExitCode == 0 &&
                output.Contains(
                    "ZoomHarmonyOwnerFixture: OK",
                    StringComparison.Ordinal),
                "The physical Zoom Harmony owner fixture failed. exit=" +
                result.ExitCode +
                ", output=" +
                output +
                ", error=" +
                error);
        }


    }
}
