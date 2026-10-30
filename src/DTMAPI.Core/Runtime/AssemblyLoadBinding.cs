using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DTMAPI.Core.Manifesting;
using DTMAPI.Internal.Authoring;

namespace DTMAPI.Core.Runtime
{
    internal static class AssemblyLoadBinding
    {
        internal static Assembly BindDependency(string owner, string path,
            IDictionary<Assembly, DependencyLoadedAssembly> loaded, Action<string> log)
        {
            // The host owns binding. A previous metadata read, TFM label or disk
            // hash cannot grant or revoke permission to execute.
            Assembly result = Assembly.LoadFrom(path);
            string location = Location(result);
            // The CLR may unify this request with a resident compatible assembly.
            // Its location, build ID and version are observations, not qualification.
            // Missing types/methods or an unreplaceable identity fail at their actual use.
            if (!loaded.TryGetValue(result, out var binding))
                loaded.Add(result, binding = new DependencyLoadedAssembly { Assembly = result, Path = location, Identity = Identity(result.GetName()) });
            binding.Owners.Add(owner);
            log("DependencyAssembly bound name=" + result.GetName().Name + "; owner=" + owner + "; path=" + location);
            return result;
        }

        internal static string Location(Assembly assembly) => assembly.IsDynamic ? "" : assembly.Location;
        internal static PackageAssemblyIdentity Identity(AssemblyName name) => new PackageAssemblyIdentity
        {
            Name = name.Name ?? "", AssemblyVersion = name.Version?.ToString() ?? "", Culture = name.CultureName ?? "",
            PublicKeyToken = BitConverter.ToString(name.GetPublicKeyToken() ?? Array.Empty<byte>()).Replace("-", "").ToLowerInvariant()
        };
    }
}
