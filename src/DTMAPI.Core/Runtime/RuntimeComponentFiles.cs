using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DTMAPI.Core.Runtime
{
    internal sealed class RuntimeComponentFiles : IDisposable
    {
        private readonly string root;
        internal static readonly IReadOnlyDictionary<string, string> Components = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["DTMAPI.RuntimeModules"] = "modules",
            ["DTMAPI.AudioContent"] = "audio", ["DTMAPI.AudioContent.Authoring"] = "audio", ["DTMAPI.AudioPreparation"] = "audio", ["DTMAPI.Modules.Audio"] = "audio",
            ["DTMAPI.AnimalContent"] = "animals", ["DTMAPI.Modules.Animals"] = "animals",
            ["DTMAPI.MonsterContent"] = "monsters", ["DTMAPI.MonsterContent.Authoring"] = "monsters", ["DTMAPI.Modules.Monsters"] = "monsters"
        };
        internal RuntimeComponentFiles(string game) { root = Path.Combine(game, "DTMAPI", "components"); AppDomain.CurrentDomain.AssemblyResolve += Resolve; }
        internal string DirectoryFor(string component) => Path.Combine(root, Components[component]);
        internal Assembly Load(string component)
        {
            if (!Components.ContainsKey(component)) throw new InvalidOperationException("Unknown Runtime component: " + component);
            return Load(component, null);
        }
        private Assembly Load(string component, AssemblyName? request)
        {
            string path = Path.GetFullPath(Path.Combine(DirectoryFor(component), component + ".dll"));
            var metadata = AssemblyName.GetAssemblyName(path);
            if (metadata.Name != component || request?.Version != null && metadata.Version != request.Version)
                throw new FileLoadException("Runtime component binding mismatch: " + (request?.FullName ?? component), path);
            var assembly = Assembly.LoadFrom(path);
            if (assembly.GetName().FullName != metadata.FullName || !string.Equals(Path.GetFullPath(assembly.Location), path, StringComparison.OrdinalIgnoreCase))
                throw new FileLoadException("Runtime component already bound from a different package: " + component, assembly.Location);
            return assembly;
        }
        private Assembly? Resolve(object? sender, ResolveEventArgs args)
        {
            var request = new AssemblyName(args.Name);
            string name = request.Name ?? "";
            if (!Components.ContainsKey(name)) return null;
            string path = Path.Combine(DirectoryFor(name), name + ".dll");
            return File.Exists(path) ? Load(name, request) : null;
        }
        public void Dispose() => AppDomain.CurrentDomain.AssemblyResolve -= Resolve;
    }
}
