using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal static class AuthorAssemblyObservation
    {
        // Disk SHA is never presented as a hash of resident Mono bytes. MVID is
        // read from the resident module, including assemblies whose Entry failed.
        internal static IEnumerable<KeyValuePair<string, string>> Read(DiscoveredMod selected)
        {
            string diskHash = "unavailable", diskMvid = "unavailable", residentMvid = "unavailable", match = "unavailable";
            try
            {
                if (!string.IsNullOrWhiteSpace(selected.Manifest.EntryDll))
                {
                    string path = Path.GetFullPath(Path.Combine(selected.RootPath, selected.Manifest.EntryDll));
                    byte[] bytes = File.ReadAllBytes(path);
                    diskMvid = PortableAssemblyReferenceInspector.Inspect(bytes).ModuleMvid;
                    using (var sha = SHA256.Create()) diskHash = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "");
                    var resident = AppDomain.CurrentDomain.GetAssemblies().Where(assembly =>
                    {
                        try { return !assembly.IsDynamic && AuthorSessionProtocol.PathsEqual(assembly.Location, path); }
                        catch { return false; }
                    }).ToArray();
                    if (resident.Length == 1)
                    {
                        residentMvid = resident[0].ManifestModule.ModuleVersionId.ToString("D");
                        match = string.Equals(diskMvid, residentMvid, StringComparison.OrdinalIgnoreCase).ToString();
                    }
                }
            }
            catch { /* An unavailable observation must not change Runtime authority. */ }
            return new[] {
                new KeyValuePair<string, string>("diskEntrySha256", diskHash),
                new KeyValuePair<string, string>("diskEntryMvid", diskMvid),
                new KeyValuePair<string, string>("residentEntryMvid", residentMvid),
                new KeyValuePair<string, string>("residentMatchesDiskMvid", match),
                new KeyValuePair<string, string>("codeReload", "restart-required")
            };
        }
    }
}
