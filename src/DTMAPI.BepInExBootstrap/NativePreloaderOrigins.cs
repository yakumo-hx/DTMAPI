using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DTMAPI.Core.Manifesting;

namespace DTMAPI.BepInExBootstrap
{
    internal static class NativePreloaderOrigins
    {
        internal static void Capture(string gameRoot, Action<string> log)
        {
            try
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                Assembly[] preloaders = assemblies.Where(item => !item.IsDynamic && item.GetName().Name == "BepInEx.Preloader").ToArray();
                if (preloaders.Length != 1) return;
                Assembly preloader = preloaders[0];
                string expected = Path.GetFullPath(Path.Combine(gameRoot, "BepInEx", "core", "BepInEx.Preloader.dll"));
                if (string.IsNullOrEmpty(preloader.Location) || !string.Equals(Path.GetFullPath(preloader.Location), expected, StringComparison.OrdinalIgnoreCase)) return;
                Type? mapOwner = preloader.GetType("BepInEx.Preloader.RuntimeFixes.UnityPatches", false);
                var map = mapOwner?.GetProperty("AssemblyLocations", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) as IDictionary<string, string>;
                if (map == null) return;
                string managedRoot = Path.GetFullPath(Path.Combine(gameRoot, "DolocTown_Data", "Managed")) + Path.DirectorySeparatorChar;
                foreach (Assembly assembly in assemblies)
                {
                    if (assembly.IsDynamic || !string.IsNullOrEmpty(assembly.Location) || !map.TryGetValue(assembly.FullName, out string origin)) continue;
                    string full = Path.GetFullPath(origin);
                    if (!full.StartsWith(managedRoot, StringComparison.OrdinalIgnoreCase)) continue;
                    NativePackageVerifier.ObservePreloaderOrigin(assembly, full);
                    log("Native preloader original-file origin captured before managed discovery: " + assembly.FullName + "; source=" + full + "; transformed bodies remain loader-owned.");
                }
            }
            catch (Exception ex)
            {
                // Missing or changed loader internals never authorize a memory-loaded host.
                log("Native preloader origin unavailable: " + ex.GetType().Name + ": " + ex.Message);
            }
        }
    }
}
