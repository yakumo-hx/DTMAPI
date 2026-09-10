using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace DTMAPI.AbiCompatibilityHarness
{
    internal static partial class Program
    {
        private static int RunHelperAbi(IReadOnlyDictionary<string, string> options)
        {
            string baselinePath = RequireFile(options, "baseline-abstractions");
            string candidatePath = RequireFile(options, "candidate-abstractions");
            string implementerPath = RequireFile(options, "helper-implementer");
            string consumerPath = RequireFile(options, "helper-consumer");
            string frozenHash = RequireOption(options, "expected-baseline-sha256").ToUpperInvariant();
            RequireEqual(frozenHash, ComputeSha256(baselinePath), "frozen helper compilation input");
            var baselineContext = new ArtifactLoadContext();
            var candidateContext = new CandidateBindingLoadContext(candidatePath, Path.GetDirectoryName(consumerPath)!);
            try
            {
                var baseline = baselineContext.LoadFromAssemblyPath(baselinePath);
                var candidate = candidateContext.LoadCandidate();
                if (baseline.GetType("DTMAPI.Abstractions.IDtmHelperServices") != null)
                    throw new InvalidOperationException("Baseline must predate optional services.");
                string Shape(Assembly assembly) => string.Join("\n", assembly.GetType("DTMAPI.Abstractions.IDtmHelper", true)!
                    .GetMethods().Select(m => m.ToString()).OrderBy(x => x, StringComparer.Ordinal));
                RequireEqual(Shape(baseline), Shape(candidate), "old IDtmHelper required method shape");
                var implementer = candidateContext.LoadFromAssemblyPath(implementerPath);
                var consumer = candidateContext.LoadFromAssemblyPath(consumerPath);
                foreach (var assembly in new[] { implementer, consumer })
                {
                    string recordedHash = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                        .Single(a => a.Key == "FrozenAbstractionsSha256").Value!;
                    RequireEqual(frozenHash, recordedHash.ToUpperInvariant(), "fixture frozen compilation receipt");
                }
                object helper = Activator.CreateInstance(implementer.GetType("Frozen.LegacyHelper", true)!)!;
                object mod = Activator.CreateInstance(consumer.GetType("Frozen.LegacyConsumer", true)!)!;
                Type helperType = candidate.GetType("DTMAPI.Abstractions.IDtmHelper", true)!;
                if (!helperType.IsInstanceOfType(helper)) throw new InvalidOperationException("Old implementer did not bind to candidate IDtmHelper.");
                Type optional = candidate.GetType("DTMAPI.Abstractions.IDtmHelperServices", true)!;
                if (optional.IsInstanceOfType(helper)) throw new InvalidOperationException("Old helper unexpectedly implements the new optional interface.");
                // Invoke through candidate DtmMod's abstract slot, not merely a symbol comparison.
                candidate.GetType("DTMAPI.Abstractions.DtmMod", true)!.GetMethod("Entry")!.Invoke(mod, new[] { helper });
                int result = (int)mod.GetType().GetProperty("Result")!.GetValue(mod)!;
                int writes = (int)helper.GetType().GetProperty("Writes")!.GetValue(helper)!;
                if (result != 73 || writes != 1) throw new InvalidOperationException("Old helper/consumer calls returned unexpected results.");
                var extensions = candidate.GetType("DTMAPI.Abstractions.DtmHelperServiceExtensions", true)!;
                object? absent = extensions.GetMethod("GetOptionalService")!.MakeGenericMethod(typeof(IDisposable)).Invoke(null, new[] { helper });
                if (absent != null) throw new InvalidOperationException("An old helper must report optional services missing.");
                string report = JsonSerializer.Serialize(new
                {
                    passed = true, baselineSha256 = frozenHash, candidateSha256 = ComputeSha256(candidatePath),
                    implementerSha256 = ComputeSha256(implementerPath), consumerSha256 = ComputeSha256(consumerPath),
                    candidateBinding = helperType.Assembly.Location, oldHelperInstantiated = true,
                    oldConsumerEntryInvoked = true, readConfigResult = result, writeConfigCount = writes,
                    oldHelperOptionalServiceMissing = true, shippingMono = false
                }, new JsonSerializerOptions { WriteIndented = true });
                string? reportPath = GetOptionalPath(options, "report");
                if (reportPath != null) { Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!); File.WriteAllText(reportPath, report); }
                Console.WriteLine(report);
                return 0;
            }
            finally { candidateContext.Unload(); baselineContext.Unload(); }
        }
    }
}
