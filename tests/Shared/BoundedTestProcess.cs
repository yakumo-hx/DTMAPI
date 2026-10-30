using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DTMAPI.Testing
{
    internal static class BoundedTestProcess
    {
        internal static (int ExitCode, string Output, string Error) Run(ProcessStartInfo start, int timeoutMilliseconds = 30000)
        {
            if (timeoutMilliseconds <= 0) throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds));
            start.UseShellExecute = false;
            start.CreateNoWindow = true;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
            using Process process = Process.Start(start) ?? throw new InvalidOperationException("Could not start test process: " + start.FileName);
            Task<string> output = process.StandardOutput.ReadToEndAsync();
            Task<string> error = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(timeoutMilliseconds))
            {
                try { process.Kill(entireProcessTree: true); }
                catch (InvalidOperationException) when (process.HasExited) { }
                process.WaitForExit(5000);
                throw new TimeoutException("Test process exceeded " + timeoutMilliseconds + " ms: " + start.FileName);
            }
            if (!Task.WaitAll(new Task[] { output, error }, 5000))
                throw new TimeoutException("Test process exited but redirected output did not close: " + start.FileName);
            return (process.ExitCode, output.Result, error.Result);
        }
    }
}
