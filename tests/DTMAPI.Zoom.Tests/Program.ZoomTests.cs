using System;
using System.IO;
using System.Linq;
using System.Text.Json;
namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {
        internal static string FindRepositoryRootForZoom() => FindRepositoryRoot();
        private static void ZoomProductArchitectureBoundariesRemainOrdinary()
        {
            string root = Path.Combine(FindRepositoryRoot(), "mods", "Zoom");
            using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "manifest.json")));
            Assert(manifest.RootElement.GetProperty("HarmonyOwner").GetString() == "DTMAPI.ZoomMod.Native", "Zoom declares its product-owned Harmony identity.");
            Assert(manifest.RootElement.GetProperty("ManifestVersion").GetInt32() == 2 && manifest.RootElement.GetProperty("Type").GetString() == "CodeMod", "Zoom declares the current ordinary CodeMod format.");
            string source = string.Join("\n", Directory.GetFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));
            Assert(!source.Contains("ICameraViewApi", StringComparison.Ordinal) && !source.Contains("ICameraZoomApi", StringComparison.Ordinal), "Product owns its presentation; frozen services are not a second writer.");
            Assert(!source.Contains("FindObjectsOfType", StringComparison.Ordinal) && !source.Contains("RefreshResolution();", StringComparison.Ordinal), "Product must not initiate scene scans or native gameplay refresh.");
        }
    }
}
