using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using DTMAPI.Abstractions;

namespace DTMAPI.GameBridge.DolocTown.QA
{
    // Optional QA-only consumption of the already retained frozen-target fixtures.
    // No compiler or baseline Abstractions is loaded into the game process.
    internal static class RetainedHelperAbiProbe
    {
        internal static void RunIfRequested(Action<string> log)
        {
            string root = Environment.GetEnvironmentVariable("DTMAPI_QA_RETAINED_HELPER_ABI_ROOT") ?? string.Empty;
            if (root.Length == 0) return;
            root = Path.GetFullPath(root);
            string implementerHash = "CC38DBFC35177AC90F1581497FBF2239EBAC2AD59D332C4C985034DA8410801D";
            string consumerHash = "741A76C02D249A70228B90D5ADC285B75DCBE928720ACF3E637990010CC4233B";
            Assembly implementer = LoadExact(root, "Frozen.HelperImplementer.dll", implementerHash);
            Assembly consumerAssembly = LoadExact(root, "Frozen.HelperConsumer.dll", consumerHash);
            Type helperType = implementer.GetType("Frozen.LegacyHelper", throwOnError: true)!;
            Type consumerType = consumerAssembly.GetType("Frozen.LegacyConsumer", throwOnError: true)!;
            var helper = (IDtmHelper)Activator.CreateInstance(helperType)!;
            var consumer = (DtmMod)Activator.CreateInstance(consumerType)!;
            consumer.Entry(helper);
            int result = (int)consumerType.GetProperty("Result")!.GetValue(consumer)!;
            int writes = (int)helperType.GetProperty("Writes")!.GetValue(helper)!;
            bool optionalMissing = helper.GetOptionalService<IDtmScheduler>() == null;
            bool requiredMissing = false;
            try { helper.GetRequiredService<IDtmScheduler>("0.7.0", "0.7.0"); }
            catch (NotSupportedException) { requiredMissing = true; }
            if (result != 73 || writes != 1 || !optionalMissing || !requiredMissing)
                throw new InvalidOperationException("Retained helper ABI invocation did not match its frozen fixture contract.");
            log("RetainedHelperAbi PASS implementerSha256=" + implementerHash + "; consumerSha256=" + consumerHash +
                "; result=" + result + "; writes=" + writes + "; optionalMissing=" + optionalMissing +
                "; requiredMissing=" + requiredMissing + "; thread=" + Thread.CurrentThread.ManagedThreadId +
                "; candidateBinding=" + typeof(IDtmHelper).Assembly.Location + "; shippingMono=true; rebuilt=false.");
        }

        private static Assembly LoadExact(string root, string name, string expected)
        {
            string path = Path.Combine(root, name);
            byte[] bytes = File.ReadAllBytes(path);
            using (var sha = SHA256.Create())
            {
                string actual = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty);
                if (!string.Equals(actual, expected, StringComparison.Ordinal))
                    throw new InvalidDataException("Retained helper fixture hash mismatch: " + name);
            }
            return Assembly.Load(bytes);
        }
    }
}
