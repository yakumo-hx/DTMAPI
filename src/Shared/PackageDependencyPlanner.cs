using System;
using System.Collections.Generic;
using System.Linq;

namespace DTMAPI.Internal.Authoring
{
    internal sealed class PackagePlanInput
    {
        public string Id { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public IReadOnlyList<PackageModDependency> Dependencies { get; set; } = Array.Empty<PackageModDependency>();
        public string PreflightFailure { get; set; } = string.Empty;
        // Runtime supplies this after source selection; offline SDK inputs keep ID fallback.
        public int? OfficialPriority { get; set; }
        public bool IsProcessProvider { get; set; }
    }

    internal sealed class PackageDependencyPlan
    {
        public IReadOnlyList<string> LoadOrder { get; set; } = Array.Empty<string>();
        public IReadOnlyDictionary<string, string> Blocked { get; set; } = new Dictionary<string, string>();
        public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();
    }

    internal static class PackageDependencyPlanner
    {
        private static readonly StringComparer IdComparer = StringComparer.OrdinalIgnoreCase;

        public static PackageDependencyPlan Plan(IEnumerable<PackagePlanInput> inputs)
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
                    bool available = present && (dependency.LegacyOrderingOnly || VersionFits(provider!.Version, dependency.Range));
                    if (available) (dependency.Required ? required : optional)[node.Id].Add(provider!.Id);
                    else
                    {
                        string reason = node.Id + " -> " + dependency.Id + (present ? " (version outside range: " + provider!.Version + ")" : " (missing)");
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

            // A Mod dependency plan has no authority over candidate DLL identities.
            // CLR binding happens only when an entry or running code requests it.
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
                string? next = PackageLoadOrder.Next(remaining.Select(id => byId[id]), id => !required[id].Any(remaining.Contains) && !optional[id].Any(remaining.Contains));
                if (next == null)
                {
                    // Required SCCs have already been removed. Break only optional ordering edges.
                    next = PackageLoadOrder.Next(remaining.Select(id => byId[id]), id => !required[id].Any(remaining.Contains))!;
                    warnings.Add("optional-order-cycle-broken: " + next + " -> " + string.Join(", ", optional[next].Where(remaining.Contains)));
                }
                remaining.Remove(next); order.Add(next);
            }
            return new PackageDependencyPlan { LoadOrder = order, Blocked = blocked, Warnings = warnings };
        }
        private static bool VersionFits(string text, PackageVersionRange range)
        {
            try { return range.Contains(PackageSemanticVersion.Parse(text)); }
            catch (System.IO.InvalidDataException) { return false; }
        }
    }

    // Shared ready-candidate selection, not dependency admission or assembly binding policy.
    internal static class PackageLoadOrder
    {
        internal static IOrderedEnumerable<PackagePlanInput> Initial(IEnumerable<PackagePlanInput> nodes) =>
            nodes.OrderBy(node => node.IsProcessProvider ? 0 : 1)
                .ThenBy(node => node.OfficialPriority.GetValueOrDefault(-1))
                .ThenBy(node => node.Id, StringComparer.OrdinalIgnoreCase);

        internal static string? Next(IEnumerable<PackagePlanInput> nodes, Func<string, bool> ready) =>
            Initial(nodes).FirstOrDefault(node => ready(node.Id))?.Id;
    }
}
