using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {
        private static void RetiredPublicApiIsAbsentAndCurrentContractsRemain()
        {
            Assembly api = typeof(IDtmHelper).Assembly;
            using Stream stream = typeof(Program).Assembly.GetManifestResourceStream("RetiredPublicTypes.txt")!;
            using var reader = new StreamReader(stream);
            string[] retired = reader.ReadToEnd().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            Assert(retired.Length == 186, "Retirement covers the agreed 151 + 8 + 27 types.");
            foreach (string name in retired)
                Assert(api.GetType(name, false) == null, "Retired public type remains: " + name);
            Assert(api.GetForwardedTypes().Length == 0, "Retirement must not leave forwarded compatibility shells.");
            foreach (Type current in new[] { typeof(IDtmHelper), typeof(IModRegistry), typeof(IItemDisplayNameApi), typeof(IMailDeliveryApi), typeof(ICropHarvestingApi) })
                Assert(current.IsPublic && current.Assembly == api, "Current contract must remain available: " + current.FullName);
            Assert(api.GetName().Version == new Version(0, 5, 3, 0), "Assembly identity remains 0.5.3.0.");
        }
    }
}
