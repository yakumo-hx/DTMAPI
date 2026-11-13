using System;
using System.Collections.Generic;
using System.IO;
namespace DTMAPI.Core.Manifesting
{
    // Process-local discovery state, never a certificate of a build workflow.
    internal sealed class StartupPackageReadTransaction : IDisposable
    {
        private readonly Dictionary<string, RuntimePackage> packages = new Dictionary<string, RuntimePackage>(StringComparer.OrdinalIgnoreCase);
        private bool disposed;
        internal RuntimePackage Read(ManifestModel manifest, string root, string manifestPath)
        {
            if (disposed) throw new ObjectDisposedException(nameof(StartupPackageReadTransaction));
            string key = Path.GetFullPath(manifestPath);
            if (!packages.TryGetValue(key, out var package)) packages.Add(key, package = DependencyPackageVerifier.Read(manifest, root, manifestPath));
            return package;
        }
        public void Dispose() { disposed = true; packages.Clear(); }
    }
}
