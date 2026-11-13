using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DTMAPI.Internal.Authoring;
using DTMAPI.Core.Manifesting;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {
        private static void PackagePortableIdentityMatchesClrMetadataWithoutTargetExecution()
        {
            foreach (System.Reflection.Assembly assembly in new[] { typeof(DTMAPI.Abstractions.IDtmHelper).Assembly, typeof(DTMAPI.Core.Runtime.DtmApiRuntime).Assembly, typeof(object).Assembly })
            {
                var metadata = PortableAssemblyReferenceInspector.Inspect(assembly.Location);
                var identity = assembly.GetName();
                string Key(System.Reflection.AssemblyName name) => new PackageAssemblyIdentity
                {
                    Name = name.Name!, AssemblyVersion = name.Version!.ToString(), Culture = name.CultureName ?? "",
                    PublicKeyToken = BitConverter.ToString(name.GetPublicKeyToken() ?? Array.Empty<byte>()).Replace("-", "").ToLowerInvariant()
                }.Key;
                Assert(metadata.Identity.Key == Key(identity), "PE definition identity matches CLR, including strong-name token.");
                Assert(metadata.ReferenceIdentities.Select(r => r.Key).OrderBy(x => x).SequenceEqual(assembly.GetReferencedAssemblies().Select(Key).OrderBy(x => x)), "All exact AssemblyRef identities match metadata of already loaded controls.");
            }
        }

        private static void PackageSemVerUsesFullPrecedenceAndExplicitPrerelease()
        {
            string[] order = { "1.0.0-alpha", "1.0.0-alpha.1", "1.0.0-alpha.beta", "1.0.0-beta", "1.0.0-beta.2", "1.0.0-beta.11", "1.0.0-rc.1", "1.0.0" };
            for (int i = 0; i < order.Length; i++)
                for (int j = 0; j < order.Length; j++)
                    Assert(Math.Sign(PackageSemanticVersion.Parse(order[i]).CompareTo(PackageSemanticVersion.Parse(order[j]))) == Math.Sign(i - j), "SemVer precedence " + order[i] + " / " + order[j]);
            Assert(PackageSemanticVersion.Parse("1.2.3+a.01").CompareTo(PackageSemanticVersion.Parse("1.2.3+b")) == 0, "Build metadata does not change precedence.");
            Assert(PackageSemanticVersion.Parse("999999999999999999999999.0.0").CompareTo(PackageSemanticVersion.Parse("99999999999999999999999.9.9")) > 0, "Numeric identifiers do not overflow Int64.");
            Assert(PackageSemanticVersion.Parse("1.0.0-999999999999999999999").CompareTo(PackageSemanticVersion.Parse("1.0.0-1000000000000000000000")) < 0, "Large numeric prereleases compare numerically.");
            foreach (string invalid in new[] { "", "1", "1.2", "01.2.3", "1.02.3", "1.2.03", "1.2.3.4", "v1.2.3", " 1.2.3", "1.2.3\n", "1.2.3-01", "1.2.3-a..b", "1.2.3-", "1.2.3+", "1.2.3-中文", "1.2.3_a" })
                PackageReject(() => PackageSemanticVersion.Parse(invalid));
            var stable = new PackageVersionRange("1.0.0-beta", "2.0.0", false);
            Assert(!stable.Contains(PackageSemanticVersion.Parse("1.0.0-beta")) && stable.Contains(PackageSemanticVersion.Parse("1.0.0")) && !stable.Contains(PackageSemanticVersion.Parse("2.0.0")), "Prerelease opt-in and maximum exclusion.");
            var pre = new PackageVersionRange("1.0.0-beta", null, true);
            Assert(pre.Contains(PackageSemanticVersion.Parse("1.0.0-beta+build")) && !pre.Contains(PackageSemanticVersion.Parse("1.0.0-alpha")), "Minimum inclusive includes prerelease with metadata.");
            PackageReject(() => new PackageVersionRange("1.0.0+a", "1.0.0+b", true));
        }

        private static void PackageDependencyReaderRejectsAmbiguousNewWireFields()
        {
            string json = "{\"DependencyContractVersion\":1,\"UniqueID\":\"Test.Consumer\",\"Version\":\"1.0.0+build\",\"Dependencies\":[{\"UniqueID\":\"Test.Provider\",\"Required\":true,\"VersionRange\":{\"minimumInclusive\":\"1.0.0\",\"maximumExclusive\":\"2.0.0\",\"includePrerelease\":false}}]}";
            PackageDependencyManifest Read(string input) => PackageDependencyContract.ReadManifest(Encoding.UTF8.GetBytes(input))!;
            var value = Read(json);
            Assert(value.OwnerId == "Test.Consumer" && value.Dependencies.Single().Required, "New manifest identity and dependency parsed.");
            Assert(Read("\uFEFF" + json).OwnerId == value.OwnerId, "UTF-8 BOM remains valid for the explicit new reader.");
            foreach (string empty in new[] { "", "\uFEFF", " \r\n", "\uFEFF\uFEFF", "{" })
            {
                try { Read(empty); throw new Exception("Broken JSON was accepted."); }
                catch (InvalidDataException ex) { Assert(ex.Message.StartsWith("dependency-json-invalid", StringComparison.Ordinal), "Damaged dependency JSON reports the input category."); }
            }
            Assert(Read("{\"UniqueID\":\"Old.Mod\",\"Version\":\"legacy-alpha\",\"Dependencies\":[{\"UniqueID\":\"Provider\",\"IsRequired\":true}]}").Dependencies.Single().Required, "Old wire fields remain readable without a selector or proof.");
            Assert(Read("{\"UniqueID\":\"Simple.Mod\"}").Dependencies.Count == 0, "Dependencies are optional.");
            foreach (string selector in new[] { "2", "999", "\"damaged\"", "null", "{}", "true" })
            {
                var historical = Read(json.Replace("\"DependencyContractVersion\":1", "\"DependencyContractVersion\":" + selector));
                Assert(historical.Dependencies.Single().Required && historical.Dependencies.Single().Range.Contains(PackageSemanticVersion.Parse("1.5.0")),
                    "Historical tool fields cannot change an explicit dependency relationship.");
                Assert(Read("{\"UniqueID\":\"Simple.Mod\",\"DependencyContractVersion\":" + selector + "}").Dependencies.Count == 0,
                    "A package without dependencies does not need a dependency selector.");
            }
            foreach (string invalid in new[]
            {
                json.Replace("\"Required\":true", "\"Required\":true,\"Required\":true"),
                json.Replace("\"Required\":true", "\"Required\":\"true\""),
                json.Replace("\"includePrerelease\":false", "\"includePrerelease\":false,\"includePrerelease\":false"),
                json.Replace("\"maximumExclusive\":\"2.0.0\"", "\"maximumExclusive\":null")
            }) PackageReject(() => Read(invalid));
        }
        private static void PackageDependencyPlanOrdersDeclaredRelationships()
        {
            PackageModDependency Dep(string id, bool required = true) => new PackageModDependency(id, required, new PackageVersionRange("1.0.0", "2.0.0", false));
            PackagePlanInput Node(string id, params PackageModDependency[] dependencies) => new PackagePlanInput { Id = id, Version = "1.0.0", Dependencies = dependencies };
            var provider = Node("Test.Provider"); var consumer = Node("Test.Consumer", Dep(provider.Id)); var control = Node("Test.Control");
            PackageDependencyPlan Plan(params PackagePlanInput[] nodes) => PackageDependencyPlanner.Plan(nodes);
            var good = Plan(consumer, control, provider);
            Assert(good.Blocked.Count == 0 && good.LoadOrder.ToList().IndexOf(provider.Id) < good.LoadOrder.ToList().IndexOf(consumer.Id), "The plan orders declared dependencies without selecting any DLL.");
            provider.PreflightFailure = "required entry missing";
            var blocked = Plan(provider, consumer, control);
            Assert(blocked.Blocked[consumer.Id].Contains(consumer.Id) && blocked.Blocked[consumer.Id].Contains("required entry missing"), "Required failure chain reaches consumer.");
            var optional = Node("Test.Optional", Dep(provider.Id, false));
            Assert(Plan(optional, provider).LoadOrder.Contains(optional.Id), "Optional consumer remains available after provider failure.");
            provider.PreflightFailure = "";
            var a = Node("Test.A", Dep("Test.B")); var b = Node("Test.B", Dep("Test.A")); var c = Node("Test.C", Dep("Test.A"));
            Assert(Plan(a, b, c, control).Blocked.Count == 3, "Required SCC and incoming consumer closure are blocked.");
            a.Dependencies = new[] { Dep(b.Id, false) }; b.Dependencies = new[] { Dep(a.Id, false) };
            var forward = Plan(a, b); var reverse = Plan(b, a);
            Assert(forward.Blocked.Count == 0 && forward.LoadOrder.SequenceEqual(reverse.LoadOrder) && forward.Warnings.Single().Contains("optional-order-cycle-broken"), "Pure optional cycle has deterministic warning/order.");
        }

        private static void PackageReject(Action action)
        { try { action(); } catch (InvalidDataException) { return; } throw new Exception("Expected strict package contract rejection."); }
    }
}
