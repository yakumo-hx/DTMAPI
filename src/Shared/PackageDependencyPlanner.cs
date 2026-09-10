using System;
using System.Collections.Generic;
using System.Linq;

namespace DTMAPI.Internal.Authoring
{
    internal sealed class PackagePlanInput
    {
        public string Id { get; set; } = string.Empty;
        public PackageSemanticVersion? Version { get; set; }
        public IReadOnlyList<PackageModDependency> Dependencies { get; set; } = Array.Empty<PackageModDependency>();
        public IReadOnlyList<PackageAssemblyRecord> Assemblies { get; set; } = Array.Empty<PackageAssemblyRecord>();
        public string PreflightFailure { get; set; } = string.Empty;
    }

    internal sealed class PackageResidentAssembly
    {
        public PackageAssemblyIdentity Identity { get; set; } = new PackageAssemblyIdentity();
        public string Sha256 { get; set; } = string.Empty;
        public bool ProvenHostOrPlanSource { get; set; }
    }

    internal sealed class PackageDependencyPlan
    {
        public IReadOnlyList<string> LoadOrder { get; set; } = Array.Empty<string>();
        public IReadOnlyDictionary<string, string> Blocked { get; set; } = new Dictionary<string, string>();
        public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();
        public IReadOnlyDictionary<string, string> AssemblyOwners { get; set; } = new Dictionary<string, string>();
    }

    internal static class PackageDependencyPlanner
    {
        private static readonly StringComparer IdComparer = StringComparer.OrdinalIgnoreCase;

        public static PackageDependencyPlan Plan(IEnumerable<PackagePlanInput> inputs,
            ISet<string> reservedNames, IEnumerable<PackageResidentAssembly> residentAssemblies)
        {
            PackagePlanInput[] nodes = inputs.OrderBy(n => n.Id, IdComparer).ToArray();
            var blocked = new Dictionary<string, string>(IdComparer);
            var warnings = new List<string>();
            var byId = new Dictionary<string, PackagePlanInput>(IdComparer);
            foreach (PackagePlanInput node in nodes)
            {
                if (byId.ContainsKey(node.Id)) blocked[node.Id] = "dependency-id-duplicate: " + node.Id;
                else byId.Add(node.Id, node);
                if (node.PreflightFailure.Length > 0) blocked[node.Id] = node.PreflightFailure;
            }
            var required = byId.Keys.ToDictionary(id => id, id => new List<string>(), IdComparer);
            var optional = byId.Keys.ToDictionary(id => id, id => new List<string>(), IdComparer);
            foreach (PackagePlanInput node in byId.Values)
            {
                foreach (PackageModDependency dependency in node.Dependencies)
                {
                    bool present = byId.TryGetValue(dependency.Id, out PackagePlanInput? provider);
                    bool available = present && (dependency.LegacyOrderingOnly || (provider!.Version != null && dependency.Range.Contains(provider.Version)));
                    if (available) (dependency.Required ? required : optional)[node.Id].Add(provider!.Id);
                    else
                    {
                        string reason = node.Id + " -> " + dependency.Id + (present ? " (version outside range: " + (provider!.Version?.Text ?? "invalid SemVer") + ")" : " (missing)");
                        if (dependency.Required) blocked[node.Id] = "required-dependency-unavailable: " + reason;
                        else warnings.Add("optional-dependency-unavailable: " + reason);
                    }
                }
            }
            foreach (List<string> edges in required.Values.Concat(optional.Values)) edges.Sort(IdComparer);

            var state = new Dictionary<string, int>(IdComparer);
            var stack = new List<string>();
            void Visit(string id)
            {
                if (state.TryGetValue(id, out int value))
                {
                    if (value == 1)
                    {
                        int index = stack.FindIndex(item => IdComparer.Equals(item, id));
                        string chain = string.Join(" -> ", stack.Skip(index).Concat(new[] { id }));
                        foreach (string participant in stack.Skip(index)) blocked[participant] = "required-dependency-cycle: " + chain;
                    }
                    return;
                }
                state[id] = 1; stack.Add(id);
                foreach (string provider in required[id]) Visit(provider);
                stack.RemoveAt(stack.Count - 1); state[id] = 2;
            }
            foreach (string id in byId.Keys.OrderBy(id => id, IdComparer)) Visit(id);

            var residents = residentAssemblies.GroupBy(r => r.Identity.Name, IdComparer).ToDictionary(g => g.Key, g => g.ToArray(), IdComparer);
            var groups = nodes.SelectMany(node => node.Assemblies.Select(assembly => new { Owner = node.Id, Assembly = assembly }))
                .GroupBy(row => row.Assembly.Identity.Name, IdComparer).OrderBy(group => group.Key, IdComparer).ToArray();
            foreach (var group in groups)
            {
                var rows = group.ToArray();
                string participants = string.Join(", ", rows.Select(r => r.Owner + "/" + r.Assembly.Path));
                string reason = string.Empty;
                if (reservedNames.Contains(group.Key)) reason = "reserved-assembly-name";
                else if (rows.Length > 1 && rows.Any(r => r.Assembly.Role == "entry")) reason = "entry-assembly-name-conflict";
                else if (rows.Select(r => r.Assembly.Identity.Key + "/" + r.Assembly.Sha256).Distinct(StringComparer.Ordinal).Count() != 1) reason = "assembly-identity-or-bytes-conflict";
                else if (residents.TryGetValue(group.Key, out PackageResidentAssembly[]? existing) && existing.Any(r =>
                    !r.ProvenHostOrPlanSource || r.Identity.Key != rows[0].Assembly.Identity.Key || r.Sha256 != rows[0].Assembly.Sha256)) reason = "resident-conflict/restart-required";
                if (reason.Length > 0)
                    foreach (var row in rows) blocked[row.Owner] = reason + ": " + participants + "; rename or rebuild consistent libraries and restart.";
            }
            bool changed;
            do
            {
                changed = false;
                foreach (string id in byId.Keys.OrderBy(id => id, IdComparer))
                {
                    if (blocked.ContainsKey(id)) continue;
                    string? bad = required[id].FirstOrDefault(blocked.ContainsKey);
                    if (bad == null) continue;
                    blocked[id] = "required-dependency-blocked: " + id + " -> " + blocked[bad]; changed = true;
                }
            } while (changed);
            foreach (string id in byId.Keys.Where(id => !blocked.ContainsKey(id)))
                foreach (string provider in optional[id].Where(blocked.ContainsKey)) warnings.Add("optional-dependency-unavailable: " + id + " -> " + provider + " (preflight blocked)");

            var remaining = new HashSet<string>(byId.Keys.Where(id => !blocked.ContainsKey(id)), IdComparer);
            var order = new List<string>();
            while (remaining.Count > 0)
            {
                string? next = remaining.OrderBy(id => id, IdComparer).FirstOrDefault(id => !required[id].Any(remaining.Contains) && !optional[id].Any(remaining.Contains));
                if (next == null)
                {
                    // Required SCCs have already been removed. Break only optional ordering edges.
                    next = remaining.OrderBy(id => id, IdComparer).First(id => !required[id].Any(remaining.Contains));
                    warnings.Add("optional-order-cycle-broken: " + next + " -> " + string.Join(", ", optional[next].Where(remaining.Contains)));
                }
                remaining.Remove(next); order.Add(next);
            }
            var binding = new Dictionary<string, string>(IdComparer);
            foreach (var group in groups)
            {
                var candidate = group.Where(r => !blocked.ContainsKey(r.Owner)).OrderBy(r => r.Owner, IdComparer).ThenBy(r => r.Assembly.Path, StringComparer.Ordinal).FirstOrDefault();
                if (candidate != null) binding.Add(group.Key, candidate.Owner);
            }
            return new PackageDependencyPlan { LoadOrder = order, Blocked = blocked, Warnings = warnings, AssemblyOwners = binding };
        }
    }
}
