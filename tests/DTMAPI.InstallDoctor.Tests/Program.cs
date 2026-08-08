using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using DTMAPI.InstallDoctor;
using DTMAPI.PlayerDoctor;
using DTMAPI.Tooling.Metadata;
using Json.Schema;

namespace DTMAPI.InstallDoctor.Tests;

internal static class Program
{
    private static readonly Dictionary<string, JsonSchema> SchemaCache =
        new(StringComparer.OrdinalIgnoreCase);

    private static int failures;

    private static int Main()
    {
        Run(nameof(PeMetadataIsReadWithoutAssemblyLoad), PeMetadataIsReadWithoutAssemblyLoad);
        Run(nameof(TreeHashIsDeterministicAndContentSensitive), TreeHashIsDeterministicAndContentSensitive);
        Run(nameof(DoctorClassifiesAndNeverMutatesFixtureTree), DoctorClassifiesAndNeverMutatesFixtureTree);
        Run(nameof(AdvancedIdentityAndReceiptFailClosedMatrix), AdvancedIdentityAndReceiptFailClosedMatrix);
        Run(nameof(InstalledAdvancedCompatibilityUsesActualGameBytesAndSteamBuild), InstalledAdvancedCompatibilityUsesActualGameBytesAndSteamBuild);
        Run(nameof(DoctorEmbeddedAdvancedPolicySetExactlyMatchesCatalog), DoctorEmbeddedAdvancedPolicySetExactlyMatchesCatalog);
        Run(nameof(Draft202012SchemasValidateDoctorAndAdvancedContracts), Draft202012SchemasValidateDoctorAndAdvancedContracts);
        Run(nameof(MinimumVersionChecksMatchRuntimeCompatibilitySemantics), MinimumVersionChecksMatchRuntimeCompatibilitySemantics);
        Run(nameof(InstalledRuntimeVersionProbeRequiresOneCoherentFiveDllSet), InstalledRuntimeVersionProbeRequiresOneCoherentFiveDllSet);
        Run(nameof(InstalledRuntimeVersionProbeRejectsOptionalComponentPolicyDrift), InstalledRuntimeVersionProbeRejectsOptionalComponentPolicyDrift);
        Run(nameof(ScanContextBoundsInstalledGameAndUnderstandsPackagePayload), ScanContextBoundsInstalledGameAndUnderstandsPackagePayload);
        Run(nameof(PlayerDoctorCliIsReadOnlyAndHasBoundedExitContract), PlayerDoctorCliIsReadOnlyAndHasBoundedExitContract);

        if (failures > 0)
        {
            Console.Error.WriteLine("DTMAPI InstallDoctor tests failed: " + failures);
            return 1;
        }

        Console.WriteLine("DTMAPI InstallDoctor tests passed.");
        return 0;
    }

    private static void PeMetadataIsReadWithoutAssemblyLoad()
    {
        string codeModPath = Fixture("DoctorCodeModFixture.dll");
        string pluginPath = Fixture("DoctorBepInExPluginFixture.dll");
        Assert(!IsAssemblyLoaded("DoctorCodeModFixture"), "CodeMod fixture must start unloaded.");
        Assert(!IsAssemblyLoaded("DoctorBepInExPluginFixture"), "BepInEx fixture must start unloaded.");

        var inspector = new PeMetadataInspector();
        PeMetadataInspection codeMod = inspector.Inspect(codeModPath);
        PeMetadataInspection plugin = inspector.Inspect(pluginPath);
        PeMetadataInspection abstractions = inspector.Inspect(Fixture("DTMAPI.Abstractions.dll"));

        Assert(codeMod.IsManaged && codeMod.DefinesDtmModSubclass, "CodeMod fixture should be recognized from metadata inheritance.");
        Assert(codeMod.ReferencedTypes.Contains("DTMAPI.Abstractions.ILampControlApi", StringComparer.Ordinal), "Lamp compatibility reference should remain visible in TypeRef metadata.");
        Assert(plugin.IsManaged && plugin.DefinesBepInExPlugin, "BepInEx fixture should be recognized from base type/attribute metadata.");
        Assert(!IsAssemblyLoaded("DoctorCodeModFixture"), "Inspect must not Assembly.Load the scanned CodeMod fixture.");
        Assert(!IsAssemblyLoaded("DoctorBepInExPluginFixture"), "Inspect must not Assembly.Load the scanned fixture.");
        Assert(abstractions.ObsoleteDeclarations.Count(value => value.DeclaringType.Contains("Lamp", StringComparison.Ordinal)) == 4,
            "The authoritative Abstractions metadata should expose all four obsolete Lamp compatibility types.");

        string bad = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(bad, new byte[] { 0x44, 0x54, 0x4d, 0x41, 0x50, 0x49 });
            Assert(inspector.Inspect(bad).Kind == PortableBinaryKind.DamagedOrUnknown, "Random bytes should fail closed as damaged/unknown.");
        }
        finally
        {
            File.Delete(bad);
        }
    }

    private static void TreeHashIsDeterministicAndContentSensitive()
    {
        string root = TempDirectory();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "nested"));
            File.WriteAllText(Path.Combine(root, "a.txt"), "alpha");
            File.WriteAllText(Path.Combine(root, "nested", "b.txt"), "beta");
            var hasher = new TreeHasher();
            TreeHashSnapshot first = hasher.Capture(root);
            TreeHashSnapshot second = hasher.Capture(root);
            Assert(first.Sha256 == second.Sha256 && first.FileCount == 2 && first.Errors.Count == 0, "Unchanged tree hashes should be deterministic and complete.");
            File.WriteAllText(Path.Combine(root, "nested", "b.txt"), "changed");
            TreeHashSnapshot changed = hasher.Capture(root);
            Assert(first.Sha256 != changed.Sha256, "Content changes must change the tree digest.");
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static void DoctorClassifiesAndNeverMutatesFixtureTree()
    {
        string root = TempDirectory();
        try
        {
            string runtime = MakeDirectory(root, "BepInEx", "plugins", "DTMAPI");
            File.Copy(Fixture("DTMAPI.Abstractions.dll"), Path.Combine(runtime, "DTMAPI.Abstractions.dll"));

            string external = MakeDirectory(root, "BepInEx", "plugins", "External");
            File.Copy(Fixture("DoctorBepInExPluginFixture.dll"), Path.Combine(external, "ExternalPlugin.dll"));
            string genericPluginManifest = Path.Combine(external, "manifest.json");
            File.WriteAllText(genericPluginManifest,
                "{\"name\":\"external-plugin\",\"version_number\":\"1.2.3\",\"website_url\":\"https://example.invalid\",\"description\":\"Thunderstore package metadata\",\"dependencies\":[]}");
            File.WriteAllBytes(Path.Combine(external, "Broken.dll"), new byte[] { 1, 2, 3, 4 });

            string misplacedCode = MakeDirectory(root, "BepInEx", "plugins", "WrongCodeMod");
            File.Copy(Fixture("DoctorCodeModFixture.dll"), Path.Combine(misplacedCode, "WrongCodeMod.dll"));

            string codeMod = MakeDirectory(root, "Mods", "GoodCodeMod");
            File.Copy(Fixture("DoctorCodeModFixture.dll"), Path.Combine(codeMod, "GoodCodeMod.dll"));
            File.WriteAllText(Path.Combine(codeMod, "manifest.json"),
                "{\"Name\":\"Good\",\"UniqueID\":\"Doctor.Good\",\"Version\":\"1.0.0\",\"Type\":\"CodeMod\",\"EntryDll\":\"GoodCodeMod.dll\",\"EntryType\":\"DoctorCodeModFixture.FixtureMod\"}");

            string contentPack = MakeDirectory(root, "Mods", "GoodContent");
            File.WriteAllText(Path.Combine(contentPack, "manifest.json"),
                "{\"Name\":\"Content\",\"UniqueID\":\"Doctor.Content\",\"Version\":\"1.0.0\",\"Type\":\"ContentPack\"}");

            string badContentPack = MakeDirectory(root, "Mods", "BadContent");
            File.WriteAllText(Path.Combine(badContentPack, "manifest.json"),
                "{\"Name\":\"Bad Content\",\"UniqueID\":\"Doctor.BadContent\",\"Version\":\"1.0.0\",\"Type\":\"ContentPack\",\"EntryDll\":\"payload.dll\"}");
            File.Copy(Fixture("DTMAPI.Tooling.Metadata.dll"), Path.Combine(badContentPack, "payload.dll"));

            string malformed = MakeDirectory(root, "Mods", "Malformed");
            File.WriteAllText(Path.Combine(malformed, "manifest.json"), "{ definitely not JSON");

            string reservedKind = MakeDirectory(root, "Mods", "ReservedKind");
            string reservedKindManifest = Path.Combine(reservedKind, "manifest.json");
            File.WriteAllText(
                reservedKindManifest,
                "{\"Name\":\"Reserved\",\"UniqueID\":\"Doctor.ReservedKind\",\"Version\":\"1.0.0\",\"Type\":\"CodeMod\",\"CodeModKind\":\"Advanced\",\"EntryDll\":\"Reserved.dll\"}");

            string misplacedPlugin = MakeDirectory(root, "Mods", "LooseExternal");
            File.Copy(Fixture("DoctorBepInExPluginFixture.dll"), Path.Combine(misplacedPlugin, "LooseExternal.dll"));

            string nativeSource = Path.Combine(Environment.SystemDirectory, "kernel32.dll");
            if (File.Exists(nativeSource))
                File.Copy(nativeSource, Path.Combine(external, "NativeHelper.dll"));

            TreeHashSnapshot before = new TreeHasher().Capture(root);
            DoctorReport report = new DoctorEngine().Inspect(root, new DoctorOptions
            {
                VerifyReadOnlyTreeHash = true,
                ScanContext = DoctorScanContext.InstalledGame
            });
            TreeHashSnapshot after = new TreeHasher().Capture(root);

            Assert(report.ReadOnlyByDesign && report.TreeUnchanged == true, "Doctor should report an unchanged fixture tree.");
            Assert(before.Sha256 == after.Sha256 && before.FileCount == after.FileCount, "Doctor must not mutate target bytes or paths.");
            Assert(HasArtifact(report, DoctorArtifactKind.DtmApiRuntime, DoctorPlacement.Expected), "Runtime assembly should be recognized in its installer-owned path.");
            Assert(HasArtifact(report, DoctorArtifactKind.ExternalBepInExPlugin, DoctorPlacement.Expected), "External BepInEx plugin should be recognized in plugins.");
            DoctorArtifact externalPlugin = report.Artifacts.Single(value => value.Path.EndsWith("ExternalPlugin.dll", StringComparison.OrdinalIgnoreCase));
            Assert(externalPlugin.Kind == DoctorArtifactKind.ExternalBepInExPlugin &&
                   externalPlugin.Placement == DoctorPlacement.Expected &&
                   externalPlugin.ManagedIdentity == DoctorManagedIdentity.ExternalBepInExPlugin &&
                   externalPlugin.ProvenanceStatus == DoctorProvenanceStatus.ExternalUnmanaged &&
                   externalPlugin.NativeRisk == DoctorNativeRisk.ExternalUnmanaged &&
                   externalPlugin.RestartPolicy == DoctorRestartPolicy.OutsideDtmApiManagement &&
                   externalPlugin.Findings.All(value => value.Severity != DoctorSeverity.Error),
                "A generic package manifest adjacent to an external BepInEx plugin must not change the plugin's ownership or placement.");
            Assert(report.Artifacts.All(value => !value.ManifestPath.Equals(genericPluginManifest, StringComparison.OrdinalIgnoreCase)),
                "InstalledGame must ignore generic/Thunderstore manifest.json rather than treating it as a DTMAPI CodeMod.");
            Assert(HasArtifact(report, DoctorArtifactKind.ExternalBepInExPlugin, DoctorPlacement.Misplaced), "External plugin in Mods should be marked misplaced.");
            Assert(HasArtifact(report, DoctorArtifactKind.DtmApiCodeMod, DoctorPlacement.Expected, "Doctor.Good"), "Manifest-owned CodeMod should be recognized.");
            DoctorArtifact legacy = Artifact(report, "Doctor.Good");
            Assert(legacy.ManagedIdentity == DoctorManagedIdentity.LegacyNativeCodeMod &&
                   legacy.DeclaredCodeModKind.Length == 0 &&
                   legacy.EffectiveCodeModKind == "LegacyNativeCompatibility" &&
                   legacy.DeclarationStatus == DoctorDeclarationStatus.LegacyCompatibility &&
                   legacy.ProvenanceStatus == DoctorProvenanceStatus.Unverified &&
                   legacy.NativeRisk == DoctorNativeRisk.ThirdPartyAuthorManaged &&
                   legacy.RestartPolicy == DoctorRestartPolicy.AuthorManagedRestartRequired &&
                   legacy.Findings.Any(finding =>
                       finding.Code == "legacy-native-author-managed" &&
                       finding.Severity == DoctorSeverity.Info),
                "An omitted-kind CodeMod must be visibly classified as third-party author-managed legacy native compatibility, never silently Strict or inferred Advanced.");
            Assert(HasArtifact(report, DoctorArtifactKind.DtmApiCodeMod, DoctorPlacement.Misplaced), "Loose CodeMod in plugins should be marked misplaced.");
            Assert(HasArtifact(report, DoctorArtifactKind.DtmApiContentPack, DoctorPlacement.Expected, "Doctor.Content"), "JSON-only ContentPack should be recognized.");
            Assert(report.Artifacts.Any(value => value.UniqueId == "Doctor.BadContent" && value.Findings.Any(finding => finding.Code == "content-pack-contains-dll")),
                "ContentPack DLLs must be rejected without execution.");
            Assert(report.Artifacts.Any(value => value.Findings.Any(finding => finding.Code == "deprecated-lamp-api")),
                "Old Lamp TypeRefs should receive retired-disabled migration guidance.");
            DoctorFinding fishingGuidance = legacy.Findings
                .Single(finding => finding.Message.Contains("DTMAPI.Abstractions.IFishingAutomationApi", StringComparison.Ordinal));
            Assert(fishingGuidance.Guidance.Contains("are frozen; do not add new consumers", StringComparison.Ordinal) &&
                   fishingGuidance.Guidance.Contains("Existing binary compatibility remains available until a separately approved breaking change", StringComparison.Ordinal) &&
                   fishingGuidance.Guidance.Contains("not a replacement public API", StringComparison.Ordinal) &&
                   !fishingGuidance.Guidance.Contains("through DTMAPI 0.5.5", StringComparison.Ordinal),
                "Frozen Fishing guidance must preserve existing binaries until a separate breaking decision and must not imply automatic 0.6 removal.");
            DoctorFinding cameraGuidance = legacy.Findings
                .Single(finding => finding.Message.Contains("DTMAPI.Abstractions.ICameraZoomApi", StringComparison.Ordinal));
            Assert(cameraGuidance.Guidance.Contains("ICameraZoomApi and ICameraViewApi are both frozen", StringComparison.Ordinal) &&
                   cameraGuidance.Guidance.Contains("do not add new consumers or migrate between them", StringComparison.Ordinal) &&
                   cameraGuidance.Guidance.Contains("Existing ICameraZoomApi binary compatibility remains available", StringComparison.Ordinal) &&
                   cameraGuidance.Guidance.Contains("first-party Zoom ProductNative mod", StringComparison.Ordinal) &&
                   cameraGuidance.Guidance.Contains("not a replacement public API", StringComparison.Ordinal) &&
                   !cameraGuidance.Guidance.Contains("Migrate playable camera zoom", StringComparison.Ordinal) &&
                   !cameraGuidance.Guidance.Contains("lease-based ICameraViewApi contract", StringComparison.Ordinal),
                "Frozen CameraZoom guidance must not recommend migration to the equally frozen CameraView contract.");
            Assert(report.Artifacts.Any(value => value.Kind == DoctorArtifactKind.DamagedAssembly), "Damaged DLL should fail closed without crashing the scan.");
            Assert(report.Artifacts.Any(value => value.Findings.Any(finding => finding.Code == "invalid-manifest")), "Malformed manifest should become a durable error finding.");
            DoctorArtifact reservedKindArtifact = report.Artifacts.Single(value => value.ManifestPath.Equals(reservedKindManifest, StringComparison.OrdinalIgnoreCase));
            Assert(
                reservedKindArtifact.Kind == DoctorArtifactKind.DtmApiCodeMod &&
                reservedKindArtifact.ManagedIdentity == DoctorManagedIdentity.AdvancedCodeMod &&
                reservedKindArtifact.DeclaredCodeModKind == "Advanced" &&
                reservedKindArtifact.EffectiveCodeModKind == "Advanced" &&
                reservedKindArtifact.ProvenanceStatus == DoctorProvenanceStatus.MissingReferenceReceipt &&
                reservedKindArtifact.NativeRisk == DoctorNativeRisk.ProductNative &&
                reservedKindArtifact.RestartPolicy == DoctorRestartPolicy.RestartRequiredAfterLoad &&
                reservedKindArtifact.Findings.Any(finding =>
                    finding.Code == "advanced-reference-receipt-missing"),
                "Doctor must recognize the explicit Advanced declaration but fail closed when its SDK reference receipt is missing.");
            if (File.Exists(nativeSource))
                Assert(report.Artifacts.Any(value => value.Kind == DoctorArtifactKind.NativeBinary), "Native PE should be classified without CLR loading.");

            DoctorReport packageReport = new DoctorEngine().Inspect(root, new DoctorOptions { ScanContext = DoctorScanContext.PackageArtifact });
            Assert(packageReport.Artifacts.Any(value => value.ManifestPath.Equals(genericPluginManifest, StringComparison.OrdinalIgnoreCase)),
                "PackageArtifact must preserve the Author SDK's explicit-tree behavior and inspect every manifest.json in that package tree.");

            using JsonDocument json = JsonDocument.Parse(DoctorReportFormatter.ToJson(report));
            Assert(json.RootElement.GetProperty("readOnlyByDesign").GetBoolean(), "Machine-readable report should preserve the read-only contract.");
            AssertSchemaValid(
                LoadSchema("doctor-report.schema.json"),
                DoctorReportFormatter.ToJson(report),
                "omitted-kind legacy native compatibility Doctor report");
            string human = DoctorReportFormatter.ToHuman(report);
            Assert(human.Contains("DTMAPI Install Doctor (read-only)", StringComparison.Ordinal), "Human report should identify its read-only boundary.");
            Assert(human.Contains("Managed identity: LegacyNativeCodeMod", StringComparison.Ordinal) &&
                   human.Contains("Managed identity: AdvancedCodeMod", StringComparison.Ordinal) &&
                   human.Contains("Native risk: ProductNative", StringComparison.Ordinal) &&
                   human.Contains("DTMAPI does not claim hot unload", StringComparison.Ordinal),
                "Human output must distinguish legacy-author-managed and Advanced identity/risk without changing the bounded summary contract.");
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static void AdvancedIdentityAndReceiptFailClosedMatrix()
    {
        string root = TempDirectory();
        try
        {
            AdvancedPackageFixture validFixture = WriteAdvancedPackage(MakeDirectory(root, "valid"), "DTMAPI.AdvancedFixture");
            DoctorReport validReport = new DoctorEngine().Inspect(validFixture.RootPath, new DoctorOptions
            {
                ScanContext = DoctorScanContext.PackageArtifact,
                VerifyReadOnlyTreeHash = true
            });
            DoctorArtifact valid = Artifact(validReport, "DTMAPI.AdvancedFixture");
            Assert(valid.ManagedIdentity == DoctorManagedIdentity.AdvancedCodeMod &&
                   valid.DeclarationStatus == DoctorDeclarationStatus.Explicit &&
                   valid.DeclaredCodeModKind == "Advanced" &&
                   valid.EffectiveCodeModKind == "Advanced" &&
                   valid.ProvenanceStatus == DoctorProvenanceStatus.VerifiedReferenceReceipt &&
                   valid.ReferenceCompatibility == DoctorCompatibilityStatus.ReceiptVerified &&
                   valid.GameCompatibility == DoctorCompatibilityStatus.NotChecked &&
                   valid.NativeRisk == DoctorNativeRisk.ProductNative &&
                   valid.ReferencePolicyId == "doloctown-23762374-g2-v1" &&
                   valid.ReferencePolicyVersion == 1 &&
                   valid.ReferencePolicySha256.Equals(TrackedPolicySha256, StringComparison.OrdinalIgnoreCase) &&
                   valid.ReferenceCount == 2 &&
                   valid.GameBuildId == "23762374" &&
                   valid.ExpectedHarmonyOwner == "dtmapi.mod.dtmapi.advancedfixture" &&
                   valid.RestartPolicy == DoctorRestartPolicy.RestartRequiredAfterLoad &&
                   valid.Findings.All(value => value.Severity != DoctorSeverity.Error),
                "A package-only synthetic Advanced fixture must verify declaration, actual manifest/entry bytes, tracked policy, native risk, Harmony owner and restart policy without claiming a local game check.");
            Assert(validReport.TreeUnchanged == true && !IsAssemblyLoaded("DoctorAdvancedCodeModFixture"),
                "Advanced Doctor validation must remain read-only and metadata-only.");

            using (JsonDocument json = JsonDocument.Parse(DoctorReportFormatter.ToJson(validReport)))
            {
                JsonElement artifact = json.RootElement.GetProperty("artifacts")[0];
                Assert(artifact.GetProperty("managedIdentity").GetString() == "AdvancedCodeMod" &&
                       artifact.GetProperty("provenanceStatus").GetString() == "VerifiedReferenceReceipt" &&
                       artifact.GetProperty("referenceCompatibility").GetString() == "ReceiptVerified" &&
                       artifact.GetProperty("restartPolicy").GetString() == "RestartRequiredAfterLoad",
                    "Machine JSON must add stable identity/provenance/compatibility/restart fields while preserving schemaVersion 2.");
            }

            AdvancedPackageFixture autoFishingFixture = WriteAdvancedPackage(
                MakeDirectory(root, "autofishing-valid"),
                "Yuuka.DTMAPI.AutoFishing");
            DoctorArtifact autoFishing = Artifact(
                new DoctorEngine().Inspect(autoFishingFixture.RootPath, new DoctorOptions { ScanContext = DoctorScanContext.PackageArtifact }),
                "Yuuka.DTMAPI.AutoFishing");
            Assert(autoFishing.ProvenanceStatus == DoctorProvenanceStatus.VerifiedReferenceReceipt &&
                   autoFishing.ReferenceCompatibility == DoctorCompatibilityStatus.ReceiptVerified &&
                   autoFishing.ReferencePolicyId == "doloctown-24456188-autofishing-v1" &&
                   autoFishing.ReferencePolicySha256.Equals(AutoFishingPolicySha256, StringComparison.OrdinalIgnoreCase) &&
                   autoFishing.ExpectedHarmonyOwner == "dtmapi.mod.yuuka.dtmapi.autofishing" &&
                   autoFishing.Findings.All(value => value.Severity != DoctorSeverity.Error),
                "The first admitted real-product pilot must verify only under its exact AutoFishing policy and UniqueID binding.");

            AdvancedPackageFixture historicalAutoFishingFixture = WriteAdvancedPackage(
                MakeDirectory(root, "autofishing-retained-055"),
                "Yuuka.DTMAPI.AutoFishing",
                useHistoricalAutoFishingPolicy: true);
            DoctorArtifact historicalAutoFishing = Artifact(
                new DoctorEngine().Inspect(historicalAutoFishingFixture.RootPath, new DoctorOptions { ScanContext = DoctorScanContext.PackageArtifact }),
                "Yuuka.DTMAPI.AutoFishing");
            Assert(historicalAutoFishing.ProvenanceStatus == DoctorProvenanceStatus.VerifiedReferenceReceipt &&
                   historicalAutoFishing.ReferenceCompatibility == DoctorCompatibilityStatus.ReceiptVerified &&
                   historicalAutoFishing.ReferencePolicyId == "doloctown-23762374-autofishing-v1" &&
                   historicalAutoFishing.ReferencePolicySha256.Equals(HistoricalAutoFishingPolicySha256, StringComparison.OrdinalIgnoreCase) &&
                   historicalAutoFishing.Findings.All(value => value.Severity != DoctorSeverity.Error),
                "The current Doctor must retain exact read-only acceptance for the published 0.5.5 AutoFishing policy while keeping that policy out of current authoring.");

            AdvancedPackageFixture forgedHistoricalAutoFishingFixture = WriteAdvancedPackage(
                MakeDirectory(root, "autofishing-retained-055-forged"),
                "Yuuka.DTMAPI.AutoFishing",
                useHistoricalAutoFishingPolicy: true);
            MutateJson(forgedHistoricalAutoFishingFixture.ReceiptPath, value => value["referencePolicySha256"] = new string('0', 64));
            RefreshAdvancedBindings(forgedHistoricalAutoFishingFixture);
            DoctorArtifact forgedHistoricalAutoFishing = Artifact(
                new DoctorEngine().Inspect(forgedHistoricalAutoFishingFixture.RootPath, new DoctorOptions { ScanContext = DoctorScanContext.PackageArtifact }),
                "Yuuka.DTMAPI.AutoFishing");
            Assert(forgedHistoricalAutoFishing.ProvenanceStatus == DoctorProvenanceStatus.ReferenceReceiptMismatch &&
                   forgedHistoricalAutoFishing.ReferenceCompatibility == DoctorCompatibilityStatus.Incompatible &&
                   forgedHistoricalAutoFishing.Findings.Any(value => value.Code == "advanced-reference-policy-mismatch"),
                "Historical Runtime/Doctor acceptance must remain exact-hash fail closed; a forged retained policy is never accepted.");

            AdvancedPackageFixture actionSpeedFixture = WriteAdvancedPackage(
                MakeDirectory(root, "actionspeed-valid"),
                "Yuuka.DTMAPI.ActionSpeed");
            DoctorArtifact actionSpeed = Artifact(
                new DoctorEngine().Inspect(actionSpeedFixture.RootPath, new DoctorOptions { ScanContext = DoctorScanContext.PackageArtifact }),
                "Yuuka.DTMAPI.ActionSpeed");
            Assert(actionSpeed.ProvenanceStatus == DoctorProvenanceStatus.VerifiedReferenceReceipt &&
                   actionSpeed.ReferenceCompatibility == DoctorCompatibilityStatus.ReceiptVerified &&
                   actionSpeed.ReferencePolicyId == "doloctown-23762374-actionspeed-v1" &&
                   actionSpeed.ReferencePolicySha256.Equals(ActionSpeedPolicySha256, StringComparison.OrdinalIgnoreCase) &&
                   actionSpeed.ExpectedHarmonyOwner == "dtmapi.mod.yuuka.dtmapi.actionspeed" &&
                   actionSpeed.Findings.All(value => value.Severity != DoctorSeverity.Error),
                "The third admitted real product must verify only under its exact ActionSpeed policy and UniqueID binding.");

            AdvancedPackageFixture oneActionFixture = WriteAdvancedPackage(
                MakeDirectory(root, "oneaction-valid"),
                "Yuuka.DTMAPI.OneActionComplete");
            DoctorArtifact oneAction = Artifact(
                new DoctorEngine().Inspect(oneActionFixture.RootPath, new DoctorOptions { ScanContext = DoctorScanContext.PackageArtifact }),
                "Yuuka.DTMAPI.OneActionComplete");
            Assert(oneAction.ProvenanceStatus == DoctorProvenanceStatus.VerifiedReferenceReceipt &&
                   oneAction.ReferenceCompatibility == DoctorCompatibilityStatus.ReceiptVerified &&
                   oneAction.ReferencePolicyId == "doloctown-23762374-oneactioncomplete-v1" &&
                   oneAction.ReferencePolicySha256.Equals(OneActionCompletePolicySha256, StringComparison.OrdinalIgnoreCase) &&
                   oneAction.ExpectedHarmonyOwner == "dtmapi.mod.yuuka.dtmapi.oneactioncomplete" &&
                   oneAction.Findings.All(value => value.Severity != DoctorSeverity.Error),
                "The unique second real-product pilot must verify only under its exact OneActionComplete policy and UniqueID binding.");

            AdvancedPackageFixture wrongProductBinding = WriteAdvancedPackage(
                MakeDirectory(root, "wrong-product-binding"),
                "DTMAPI.AdvancedFixture");
            MutateJson(wrongProductBinding.ReceiptPath, value =>
            {
                value["referencePolicyId"] = "doloctown-24456188-autofishing-v1";
                value["referencePolicySha256"] = AutoFishingPolicySha256;
            });
            RefreshAdvancedBindings(wrongProductBinding);
            DoctorArtifact wrongProduct = Artifact(
                new DoctorEngine().Inspect(wrongProductBinding.RootPath, new DoctorOptions { ScanContext = DoctorScanContext.PackageArtifact }),
                "DTMAPI.AdvancedFixture");
            Assert(wrongProduct.ProvenanceStatus == DoctorProvenanceStatus.ReferenceReceiptMismatch &&
                   wrongProduct.ReferenceCompatibility == DoctorCompatibilityStatus.Incompatible &&
                   wrongProduct.Findings.Any(value => value.Code == "advanced-reference-policy-mismatch"),
                "A tracked Advanced policy presented by the wrong registered UniqueID must fail closed.");

            AdvancedPackageFixture unknownPolicy = WriteAdvancedPackage(
                MakeDirectory(root, "unknown-policy"),
                "DTMAPI.AdvancedFixture");
            MutateJson(unknownPolicy.ReceiptPath, value => value["referencePolicyId"] = "doloctown-23762374-unknown-v1");
            RefreshAdvancedBindings(unknownPolicy);
            DoctorArtifact unknownPolicyArtifact = Artifact(
                new DoctorEngine().Inspect(unknownPolicy.RootPath, new DoctorOptions { ScanContext = DoctorScanContext.PackageArtifact }),
                "DTMAPI.AdvancedFixture");
            Assert(unknownPolicyArtifact.ProvenanceStatus == DoctorProvenanceStatus.ReferenceReceiptMismatch &&
                   unknownPolicyArtifact.ReferenceCompatibility == DoctorCompatibilityStatus.Incompatible &&
                   unknownPolicyArtifact.Findings.Any(value => value.Code == "advanced-reference-policy-mismatch"),
                "An unregistered Advanced referencePolicyId must fail closed even when the package marker is rebound to the mutated receipt bytes.");

            AdvancedPackageFixture forgedPolicy = WriteAdvancedPackage(MakeDirectory(root, "forged-policy"), "DTMAPI.AdvancedFixture");
            MutateJson(forgedPolicy.ReceiptPath, value => value["referencePolicySha256"] = new string('0', 64));
            DoctorArtifact forged = Artifact(
                new DoctorEngine().Inspect(forgedPolicy.RootPath, new DoctorOptions { ScanContext = DoctorScanContext.PackageArtifact }),
                "DTMAPI.AdvancedFixture");
            Assert(forged.ProvenanceStatus == DoctorProvenanceStatus.ReferenceReceiptMismatch &&
                   forged.ReferenceCompatibility == DoctorCompatibilityStatus.Incompatible &&
                   forged.GameCompatibility == DoctorCompatibilityStatus.NotChecked &&
                   forged.Findings.Any(value => value.Code == "advanced-reference-policy-mismatch"),
                "A self-consistent but untrusted/forged policy identity must fail closed against the embedded tracked policy authority.");

            AdvancedPackageFixture missingMarker = WriteAdvancedPackage(MakeDirectory(root, "missing-marker"), "DTMAPI.AdvancedFixture");
            File.Delete(missingMarker.MarkerPath);
            DoctorArtifact markerMissing = Artifact(
                new DoctorEngine().Inspect(missingMarker.RootPath, new DoctorOptions { ScanContext = DoctorScanContext.PackageArtifact }),
                "DTMAPI.AdvancedFixture");
            Assert(markerMissing.ProvenanceStatus == DoctorProvenanceStatus.MissingPackageBinding &&
                   markerMissing.ReferenceCompatibility == DoctorCompatibilityStatus.ReceiptVerified &&
                   markerMissing.Findings.Any(value => value.Code == "advanced-package-marker-missing"),
                "An otherwise valid Advanced receipt must not substitute for the SDK package-binding marker.");

            AdvancedPackageFixture invalidMarker = WriteAdvancedPackage(MakeDirectory(root, "invalid-marker"), "DTMAPI.AdvancedFixture");
            MutateJson(invalidMarker.MarkerPath, value => value["unexpected"] = true);
            DoctorArtifact markerInvalid = Artifact(
                new DoctorEngine().Inspect(invalidMarker.RootPath, new DoctorOptions { ScanContext = DoctorScanContext.PackageArtifact }),
                "DTMAPI.AdvancedFixture");
            Assert(markerInvalid.ProvenanceStatus == DoctorProvenanceStatus.InvalidPackageBinding &&
                   markerInvalid.ReferenceCompatibility == DoctorCompatibilityStatus.ReceiptVerified &&
                   markerInvalid.Findings.Any(value => value.Code == "advanced-package-marker-invalid"),
                "Unknown or hand-edited package marker fields must fail closed.");

            AdvancedPackageFixture runtimeReboundMarker = WriteAdvancedPackage(MakeDirectory(root, "runtime-rebound-marker"), "DTMAPI.AdvancedFixture");
            MutateJson(runtimeReboundMarker.MarkerPath, value => value["targetDtmApiVersion"] = "0.6.1");
            DoctorArtifact runtimeRebound = Artifact(
                new DoctorEngine().Inspect(runtimeReboundMarker.RootPath, new DoctorOptions { ScanContext = DoctorScanContext.PackageArtifact }),
                "DTMAPI.AdvancedFixture");
            Assert(runtimeRebound.ProvenanceStatus == DoctorProvenanceStatus.InvalidPackageBinding &&
                   runtimeRebound.ReferenceCompatibility == DoctorCompatibilityStatus.ReceiptVerified &&
                   runtimeRebound.Findings.Any(value => value.Code == "advanced-package-marker-invalid"),
                "Doctor must reject a package marker rebound from the tracked SDK 0.1/0.5.5 contract to the currently running 0.6.1 Runtime.");

            AdvancedPackageFixture nativePolicyMismatch = WriteAdvancedPackage(MakeDirectory(root, "native-policy-mismatch"), "DTMAPI.AdvancedFixture");
            File.Copy(Fixture("DoctorCodeModFixture.dll"), nativePolicyMismatch.EntryPath, overwrite: true);
            RefreshAdvancedBindings(nativePolicyMismatch);
            DoctorArtifact nativeMismatch = Artifact(
                new DoctorEngine().Inspect(nativePolicyMismatch.RootPath, new DoctorOptions { ScanContext = DoctorScanContext.PackageArtifact }),
                "DTMAPI.AdvancedFixture");
            Assert(nativeMismatch.ProvenanceStatus == DoctorProvenanceStatus.ReferenceReceiptMismatch &&
                   nativeMismatch.Findings.Any(value => value.Code == "advanced-native-reference-policy-mismatch"),
                "A self-consistent package whose actual native AssemblyRefs do not exactly match the receipt policy must fail closed.");

            AdvancedPackageFixture wrongHarmonyOwner = WriteAdvancedPackage(MakeDirectory(root, "wrong-harmony-owner"), "DTMAPI.AdvancedFixture");
            MutateJson(wrongHarmonyOwner.ReceiptPath, value => value["harmonyOwner"] = "dtmapi.mod.some.other.mod");
            DoctorArtifact wrongOwner = Artifact(
                new DoctorEngine().Inspect(wrongHarmonyOwner.RootPath, new DoctorOptions { ScanContext = DoctorScanContext.PackageArtifact }),
                "DTMAPI.AdvancedFixture");
            Assert(wrongOwner.ProvenanceStatus == DoctorProvenanceStatus.ReferenceReceiptMismatch &&
                   wrongOwner.ReferenceCompatibility == DoctorCompatibilityStatus.Incompatible &&
                   wrongOwner.ExpectedHarmonyOwner == "dtmapi.mod.dtmapi.advancedfixture" &&
                   wrongOwner.Findings.Any(value => value.Code == "advanced-harmony-owner-mismatch"),
                "A non-canonical or reused Advanced Harmony owner must fail closed with its own stable machine code while reporting the canonical expected owner.");

            AdvancedPackageFixture tamperedEntry = WriteAdvancedPackage(MakeDirectory(root, "tampered-entry"), "DTMAPI.AdvancedFixture");
            using (FileStream stream = new(tamperedEntry.EntryPath, FileMode.Append, FileAccess.Write, FileShare.None))
                stream.WriteByte(0x00);
            DoctorArtifact tampered = Artifact(
                new DoctorEngine().Inspect(tamperedEntry.RootPath, new DoctorOptions { ScanContext = DoctorScanContext.PackageArtifact }),
                "DTMAPI.AdvancedFixture");
            Assert(tampered.ProvenanceStatus == DoctorProvenanceStatus.ReferenceReceiptMismatch &&
                   tampered.Findings.Any(value => value.Code == "advanced-reference-receipt-hash-mismatch" && value.Message.Contains("entry DLL", StringComparison.Ordinal)),
                "Doctor must recompute entry bytes and reject payload drift rather than trusting receipt self-reporting.");

            AdvancedPackageFixture malformedReceipt = WriteAdvancedPackage(MakeDirectory(root, "malformed-receipt"), "DTMAPI.AdvancedFixture");
            MutateJson(malformedReceipt.ReceiptPath, value => value["unexpected"] = true);
            DoctorArtifact malformed = Artifact(
                new DoctorEngine().Inspect(malformedReceipt.RootPath, new DoctorOptions { ScanContext = DoctorScanContext.PackageArtifact }),
                "DTMAPI.AdvancedFixture");
            Assert(malformed.ProvenanceStatus == DoctorProvenanceStatus.InvalidReferenceReceipt &&
                   malformed.Findings.Any(value => value.Code == "advanced-reference-receipt-invalid"),
                "Unknown receipt fields must fail closed instead of being ignored.");

            AdvancedPackageFixture bundledReference = WriteAdvancedPackage(MakeDirectory(root, "bundled-reference"), "DTMAPI.AdvancedFixture");
            File.Copy(Fixture("DoctorBepInExPluginFixture.dll"), Path.Combine(Path.GetDirectoryName(bundledReference.EntryPath)!, "0Harmony.dll"));
            DoctorArtifact bundled = Artifact(
                new DoctorEngine().Inspect(bundledReference.RootPath, new DoctorOptions { ScanContext = DoctorScanContext.PackageArtifact }),
                "DTMAPI.AdvancedFixture");
            Assert(bundled.ProvenanceStatus == DoctorProvenanceStatus.InvalidPackageBinding &&
                   bundled.Findings.Any(value => value.Code == "bundled-native-runtime-dependency"),
                "An Advanced package must reject every DLL beyond its declared entry without retaining verified provenance.");

            AdvancedPackageFixture disguisedGame = WriteAdvancedPackage(MakeDirectory(root, "disguised-game"), "DTMAPI.AdvancedFixture");
            string disguisedGamePath = Path.Combine(Path.GetDirectoryName(disguisedGame.EntryPath)!, "official-player-bytes.bin");
            File.Copy(Fixture("Assembly-CSharp.dll"), disguisedGamePath);
            TreeHashSnapshot disguisedGameBefore = new TreeHasher().Capture(disguisedGame.RootPath);
            DoctorArtifact disguisedGameArtifact = Artifact(
                new DoctorEngine().Inspect(disguisedGame.RootPath, new DoctorOptions
                {
                    ScanContext = DoctorScanContext.PackageArtifact,
                    VerifyReadOnlyTreeHash = true
                }),
                "DTMAPI.AdvancedFixture");
            TreeHashSnapshot disguisedGameAfter = new TreeHasher().Capture(disguisedGame.RootPath);
            Assert(disguisedGameArtifact.ProvenanceStatus == DoctorProvenanceStatus.InvalidPackageBinding &&
                   disguisedGameArtifact.ProvenanceStatus != DoctorProvenanceStatus.VerifiedReferenceReceipt &&
                   disguisedGameArtifact.NativeRisk == DoctorNativeRisk.ForbiddenNativeReference &&
                   disguisedGameArtifact.Findings.Any(value => value.Code == "bundled-native-runtime-dependency" &&
                                                                value.Path.Equals(disguisedGamePath, StringComparison.OrdinalIgnoreCase) &&
                                                                value.Message.Contains("Assembly-CSharp", StringComparison.Ordinal)) &&
                   disguisedGameBefore.Sha256 == disguisedGameAfter.Sha256 &&
                   !IsAssemblyLoaded("Assembly-CSharp"),
                "Doctor must reject an Assembly-CSharp managed PE by internal AssemblyName even when renamed to .bin, remain read-only, and never load it.");

            AdvancedPackageFixture disguisedHarmony = WriteAdvancedPackage(MakeDirectory(root, "disguised-harmony"), "DTMAPI.AdvancedFixture");
            string disguisedHarmonyPath = Path.Combine(Path.GetDirectoryName(disguisedHarmony.EntryPath)!, "reference-cache.dat");
            File.Copy(Fixture("0Harmony.dll"), disguisedHarmonyPath);
            DoctorArtifact disguisedHarmonyArtifact = Artifact(
                new DoctorEngine().Inspect(disguisedHarmony.RootPath, new DoctorOptions { ScanContext = DoctorScanContext.PackageArtifact }),
                "DTMAPI.AdvancedFixture");
            Assert(disguisedHarmonyArtifact.ProvenanceStatus == DoctorProvenanceStatus.InvalidPackageBinding &&
                   disguisedHarmonyArtifact.Findings.Any(value => value.Code == "bundled-native-runtime-dependency" &&
                                                                   value.Path.Equals(disguisedHarmonyPath, StringComparison.OrdinalIgnoreCase) &&
                                                                   value.Message.Contains("0Harmony", StringComparison.Ordinal)) &&
                   !IsAssemblyLoaded("0Harmony"),
                "Doctor must reject a Harmony managed PE by internal AssemblyName even when renamed to .dat and must not execute or load it.");

            AdvancedPackageFixture disguisedHelper = WriteAdvancedPackage(MakeDirectory(root, "disguised-helper"), "DTMAPI.AdvancedFixture");
            string disguisedHelperPath = Path.Combine(Path.GetDirectoryName(disguisedHelper.EntryPath)!, "helper-cache.bin");
            File.Copy(Fixture("DoctorCodeModFixture.dll"), disguisedHelperPath);
            DoctorArtifact disguisedHelperArtifact = Artifact(
                new DoctorEngine().Inspect(disguisedHelper.RootPath, new DoctorOptions { ScanContext = DoctorScanContext.PackageArtifact }),
                "DTMAPI.AdvancedFixture");
            Assert(disguisedHelperArtifact.ProvenanceStatus == DoctorProvenanceStatus.InvalidPackageBinding &&
                   disguisedHelperArtifact.ReferenceCompatibility == DoctorCompatibilityStatus.Incompatible &&
                   disguisedHelperArtifact.GameCompatibility == DoctorCompatibilityStatus.NotChecked &&
                   disguisedHelperArtifact.Findings.Any(value => value.Code == "bundled-native-runtime-dependency" &&
                                                                   value.Path.Equals(disguisedHelperPath, StringComparison.OrdinalIgnoreCase)) &&
                   !IsAssemblyLoaded("DoctorCodeModFixture") &&
                   !IsAssemblyLoaded("DoctorAdvancedCodeModFixture"),
                "Doctor must reject every managed MZ outside the sole declared EntryDll even when the helper uses a benign AssemblyName and non-DLL extension.");

            AdvancedPackageFixture damagedExecutable = WriteAdvancedPackage(MakeDirectory(root, "damaged-executable"), "DTMAPI.AdvancedFixture");
            string damagedExecutablePath = Path.Combine(Path.GetDirectoryName(damagedExecutable.EntryPath)!, "native-cache.bin");
            File.WriteAllBytes(damagedExecutablePath, new byte[] { (byte)'M', (byte)'Z', 0, 1, 2, 3, 4, 5 });
            TreeHashSnapshot damagedBefore = new TreeHasher().Capture(damagedExecutable.RootPath);
            DoctorArtifact damagedExecutableArtifact = Artifact(
                new DoctorEngine().Inspect(damagedExecutable.RootPath, new DoctorOptions
                {
                    ScanContext = DoctorScanContext.PackageArtifact,
                    VerifyReadOnlyTreeHash = true
                }),
                "DTMAPI.AdvancedFixture");
            TreeHashSnapshot damagedAfter = new TreeHasher().Capture(damagedExecutable.RootPath);
            Assert(damagedExecutableArtifact.ProvenanceStatus == DoctorProvenanceStatus.InvalidPackageBinding &&
                   damagedExecutableArtifact.ReferenceCompatibility == DoctorCompatibilityStatus.Incompatible &&
                   damagedExecutableArtifact.GameCompatibility == DoctorCompatibilityStatus.NotChecked &&
                   damagedExecutableArtifact.Findings.Any(value => value.Code == "bundled-native-runtime-dependency" &&
                                                                      value.Path.Equals(damagedExecutablePath, StringComparison.OrdinalIgnoreCase) &&
                                                                      value.Message.Contains("MZ executable header", StringComparison.Ordinal)) &&
                   damagedBefore.Sha256 == damagedAfter.Sha256,
                "Doctor must fail closed on an unmanaged or damaged MZ payload, preserve the tree, and never retain verified provenance.");

            if (OperatingSystem.IsWindows())
            {
                AdvancedPackageFixture unreadablePayload = WriteAdvancedPackage(MakeDirectory(root, "unreadable-payload"), "DTMAPI.AdvancedFixture");
                string unreadablePath = Path.Combine(Path.GetDirectoryName(unreadablePayload.EntryPath)!, "opaque-cache.dat");
                File.WriteAllText(unreadablePath, "locked package payload", new UTF8Encoding(false));
                DoctorArtifact unreadableArtifact;
                using (var exclusive = new FileStream(unreadablePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    unreadableArtifact = Artifact(
                        new DoctorEngine().Inspect(unreadablePayload.RootPath, new DoctorOptions { ScanContext = DoctorScanContext.PackageArtifact }),
                        "DTMAPI.AdvancedFixture");
                }
                Assert(unreadableArtifact.ProvenanceStatus == DoctorProvenanceStatus.InvalidPackageBinding &&
                       unreadableArtifact.ReferenceCompatibility == DoctorCompatibilityStatus.Incompatible &&
                       unreadableArtifact.GameCompatibility == DoctorCompatibilityStatus.NotChecked &&
                       unreadableArtifact.Findings.Any(value => value.Code == "bundled-native-runtime-dependency" &&
                                                                value.Path.Equals(unreadablePath, StringComparison.OrdinalIgnoreCase) &&
                                                                value.Message.Contains("could not be read", StringComparison.Ordinal)),
                    "Doctor must treat an unreadable package payload as an invalid Advanced binding rather than silently certifying an incomplete scan.");
            }

            AdvancedPackageFixture nativeEntry = WriteAdvancedPackage(MakeDirectory(root, "native-entry"), "DTMAPI.AdvancedFixture");
            File.Copy(Fixture("Assembly-CSharp.dll"), nativeEntry.EntryPath, overwrite: true);
            RefreshAdvancedBindings(nativeEntry);
            DoctorArtifact nativeEntryArtifact = Artifact(
                new DoctorEngine().Inspect(nativeEntry.RootPath, new DoctorOptions { ScanContext = DoctorScanContext.PackageArtifact }),
                "DTMAPI.AdvancedFixture");
            Assert(nativeEntryArtifact.ProvenanceStatus == DoctorProvenanceStatus.InvalidPackageBinding &&
                   nativeEntryArtifact.Findings.Any(value => value.Code == "bundled-native-runtime-dependency" &&
                                                              value.Path.Equals(nativeEntry.EntryPath, StringComparison.OrdinalIgnoreCase)),
                "The sole declared EntryDll is still package payload and must be rejected when its internal identity is a forbidden native/runtime assembly.");

            AdvancedPackageFixture strictWithReceipt = WriteAdvancedPackage(MakeDirectory(root, "strict-with-receipt"), "Doctor.Strict.WithReceipt", "Strict");
            DoctorArtifact strict = Artifact(
                new DoctorEngine().Inspect(strictWithReceipt.RootPath, new DoctorOptions { ScanContext = DoctorScanContext.PackageArtifact }),
                "Doctor.Strict.WithReceipt");
            Assert(strict.ManagedIdentity == DoctorManagedIdentity.StrictCodeMod &&
                   strict.ProvenanceStatus == DoctorProvenanceStatus.ReferenceReceiptMismatch &&
                   strict.Findings.Any(value => value.Code == "advanced-reference-receipt-on-strict"),
                "Strict plus an Advanced receipt must fail closed and must not be promoted to Advanced.");

            string unknownRoot = MakeDirectory(root, "unknown-kind", "Content", "DTMAPI");
            File.Copy(Fixture("DoctorCodeModFixture.dll"), Path.Combine(unknownRoot, "Unknown.dll"));
            File.WriteAllText(Path.Combine(unknownRoot, "manifest.json"),
                "{\"Name\":\"Unknown\",\"UniqueID\":\"Doctor.UnknownKind\",\"Version\":\"1.0.0\",\"Type\":\"CodeMod\",\"CodeModKind\":\"Native\",\"EntryDll\":\"Content/DTMAPI/Unknown.dll\"}");
            DoctorArtifact unknown = Artifact(
                new DoctorEngine().Inspect(Directory.GetParent(Directory.GetParent(unknownRoot)!.FullName)!.FullName, new DoctorOptions { ScanContext = DoctorScanContext.PackageArtifact }),
                "Doctor.UnknownKind");
            Assert(unknown.Kind == DoctorArtifactKind.Unknown &&
                   unknown.ManagedIdentity == DoctorManagedIdentity.Unknown &&
                   unknown.DeclarationStatus == DoctorDeclarationStatus.Invalid &&
                   unknown.ProvenanceStatus == DoctorProvenanceStatus.InvalidDeclaration &&
                   unknown.Findings.Any(value => value.Code == "unknown-code-mod-kind"),
                "Unknown CodeModKind must remain an invalid identity, never a guessed Strict or Advanced Mod.");

            string contentRoot = MakeDirectory(root, "content-kind", "Content", "DTMAPI");
            File.WriteAllText(Path.Combine(contentRoot, "manifest.json"),
                "{\"Name\":\"Content\",\"UniqueID\":\"Doctor.ContentKind\",\"Version\":\"1.0.0\",\"Type\":\"ContentPack\",\"CodeModKind\":\"Advanced\"}");
            DoctorArtifact content = Artifact(
                new DoctorEngine().Inspect(Directory.GetParent(Directory.GetParent(contentRoot)!.FullName)!.FullName, new DoctorOptions { ScanContext = DoctorScanContext.PackageArtifact }),
                "Doctor.ContentKind");
            Assert(content.ManagedIdentity == DoctorManagedIdentity.ContentPack &&
                   content.Findings.Any(value => value.Code == "code-mod-kind-on-content-pack"),
                "ContentPack plus CodeModKind must fail closed without being treated as an Advanced code identity.");

            string legacyNativeRoot = MakeDirectory(root, "legacy-native", "Content", "DTMAPI");
            string legacyNativeEntry = Path.Combine(legacyNativeRoot, "LegacyNative.dll");
            string legacyNativeHelper = Path.Combine(legacyNativeRoot, "LegacyHelper.dll");
            File.Copy(Fixture("DoctorBepInExPluginFixture.dll"), legacyNativeEntry);
            File.Copy(Fixture("DTMAPI.Tooling.Metadata.dll"), legacyNativeHelper);
            File.WriteAllText(Path.Combine(legacyNativeRoot, "manifest.json"),
                "{\"Name\":\"Legacy Native\",\"UniqueID\":\"Doctor.LegacyNative\",\"Version\":\"1.0.0\",\"Type\":\"CodeMod\",\"EntryDll\":\"Content/DTMAPI/LegacyNative.dll\"}");
            DoctorArtifact legacyNative = Artifact(
                new DoctorEngine().Inspect(Directory.GetParent(Directory.GetParent(legacyNativeRoot)!.FullName)!.FullName, new DoctorOptions { ScanContext = DoctorScanContext.PackageArtifact }),
                "Doctor.LegacyNative");
            Assert(legacyNative.ManagedIdentity == DoctorManagedIdentity.LegacyNativeCodeMod &&
                   legacyNative.NativeRisk == DoctorNativeRisk.ThirdPartyAuthorManaged &&
                   legacyNative.RestartPolicy == DoctorRestartPolicy.AuthorManagedRestartRequired &&
                   legacyNative.Findings.Any(value =>
                       value.Code == "legacy-native-references-author-managed" &&
                       value.Severity == DoctorSeverity.Info) &&
                   legacyNative.Findings.Any(value =>
                       value.Code == "legacy-private-dependency-author-managed" &&
                       value.Path.Equals(legacyNativeHelper, StringComparison.OrdinalIgnoreCase) &&
                       value.Severity == DoctorSeverity.Info) &&
                   !legacyNative.Findings.Any(value =>
                       value.Code is "strict-native-reference" or "code-mod-extra-dll" or "bundled-native-runtime-dependency"),
                "Omitted-kind legacy native references and auxiliary DLLs must remain loadable and author-managed rather than becoming DTMAPI policy errors.");

            string legacyBundledRoot = MakeDirectory(root, "legacy-native-bundled", "Content", "DTMAPI");
            File.Copy(Fixture("DoctorBepInExPluginFixture.dll"), Path.Combine(legacyBundledRoot, "LegacyNative.dll"));
            string copiedHarmony = Path.Combine(legacyBundledRoot, "PrivateName.dll");
            File.Copy(Fixture("0Harmony.dll"), copiedHarmony);
            File.WriteAllText(Path.Combine(legacyBundledRoot, "manifest.json"),
                "{\"Name\":\"Legacy Native Bundled Platform\",\"UniqueID\":\"Doctor.LegacyNativeBundled\",\"Version\":\"1.0.0\",\"Type\":\"CodeMod\",\"EntryDll\":\"Content/DTMAPI/LegacyNative.dll\"}");
            DoctorArtifact legacyBundled = Artifact(
                new DoctorEngine().Inspect(Directory.GetParent(Directory.GetParent(legacyBundledRoot)!.FullName)!.FullName, new DoctorOptions { ScanContext = DoctorScanContext.PackageArtifact }),
                "Doctor.LegacyNativeBundled");
            Assert(
                legacyBundled.NativeRisk == DoctorNativeRisk.ForbiddenNativeReference &&
                legacyBundled.Findings.Any(value =>
                    value.Code == "bundled-native-runtime-dependency" &&
                    value.Path.Equals(copiedHarmony, StringComparison.OrdinalIgnoreCase)),
                "Legacy compatibility may carry private helpers, but a copied Harmony/game/DTMAPI platform assembly must still be identified as package-owned forbidden bytes.");

            string strictNativeRoot = MakeDirectory(root, "strict-native", "Content", "DTMAPI");
            File.Copy(Fixture("DoctorBepInExPluginFixture.dll"), Path.Combine(strictNativeRoot, "StrictNative.dll"));
            File.WriteAllText(Path.Combine(strictNativeRoot, "manifest.json"),
                "{\"Name\":\"Strict Native\",\"UniqueID\":\"Doctor.StrictNative\",\"Version\":\"1.0.0\",\"Type\":\"CodeMod\",\"CodeModKind\":\"Strict\",\"EntryDll\":\"Content/DTMAPI/StrictNative.dll\"}");
            DoctorArtifact strictNative = Artifact(
                new DoctorEngine().Inspect(Directory.GetParent(Directory.GetParent(strictNativeRoot)!.FullName)!.FullName, new DoctorOptions { ScanContext = DoctorScanContext.PackageArtifact }),
                "Doctor.StrictNative");
            Assert(strictNative.ManagedIdentity == DoctorManagedIdentity.StrictCodeMod &&
                   strictNative.NativeRisk == DoctorNativeRisk.ForbiddenNativeReference &&
                   strictNative.Findings.Any(value => value.Code == "strict-native-reference"),
                "Strict native references must remain blocked rather than being used to infer Advanced identity.");
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static void InstalledAdvancedCompatibilityUsesActualGameBytesAndSteamBuild()
    {
        string root = TempDirectory();
        try
        {
            string steamApps = MakeDirectory(root, "steamapps");
            string gameRoot = MakeDirectory(steamApps, "common", "Doloc Town");
            File.WriteAllText(Path.Combine(steamApps, "appmanifest_2285550.acf"),
                "\"AppState\"\n{\n  \"appid\" \"2285550\"\n  \"buildid\" \"23762374\"\n  \"InstalledDepots\"\n  {\n    \"2285551\"\n    {\n      \"buildid\" \"23519688\"\n    }\n  }\n}\n");

            string packageRoot = MakeDirectory(gameRoot, "Mods", "AdvancedFixture");
            WriteAdvancedPackage(packageRoot, "DTMAPI.AdvancedFixture");
            string harmony = Path.Combine(gameRoot, "BepInEx", "core", "0Harmony.dll");
            Directory.CreateDirectory(Path.GetDirectoryName(harmony)!);
            File.WriteAllText(harmony, "synthetic-not-official-harmony");
            string gameAssembly = Path.Combine(gameRoot, "DolocTown_Data", "Managed", "Assembly-CSharp.dll");
            Directory.CreateDirectory(Path.GetDirectoryName(gameAssembly)!);
            File.WriteAllText(gameAssembly, "synthetic-not-official-game-assembly");

            DoctorArtifact driftedReferences = Artifact(
                new DoctorEngine().Inspect(gameRoot, new DoctorOptions { ScanContext = DoctorScanContext.InstalledGame }),
                "DTMAPI.AdvancedFixture");
            Assert(driftedReferences.ProvenanceStatus == DoctorProvenanceStatus.VerifiedReferenceReceipt &&
                   driftedReferences.ReferenceCompatibility == DoctorCompatibilityStatus.ReceiptVerified &&
                   driftedReferences.GameCompatibility == DoctorCompatibilityStatus.Drift &&
                   driftedReferences.Findings.Any(value =>
                       value.Code == "advanced-game-context-drift" &&
                       value.Severity == DoctorSeverity.Info &&
                       value.Message.Contains("compiledBuild=23762374; installedBuild=23762374; references=0/2; drifted=2; unavailable=0", StringComparison.Ordinal)) &&
                   !driftedReferences.Findings.Any(value =>
                       value.Code is "advanced-reference-hash-mismatch" or "advanced-game-build-unverifiable" or "advanced-game-build-incompatible"),
                "Installed-game Doctor must retain strict receipt provenance but report current reference-byte changes as non-blocking Drift context; a nested depot build cannot override the top-level AppState buildid.");

            File.WriteAllText(Path.Combine(steamApps, "appmanifest_2285550.acf"),
                "\"AppState\"\n{\n  \"appid\" \"2285550\"\n  \"buildid\" \"99999999\"\n}\n");
            DoctorArtifact wrongBuild = Artifact(
                new DoctorEngine().Inspect(gameRoot, new DoctorOptions { ScanContext = DoctorScanContext.InstalledGame }),
                "DTMAPI.AdvancedFixture");
            Assert(wrongBuild.ReferenceCompatibility == DoctorCompatibilityStatus.ReceiptVerified &&
                   wrongBuild.GameCompatibility == DoctorCompatibilityStatus.Drift &&
                   wrongBuild.Findings.Any(value =>
                       value.Code == "advanced-game-context-drift" &&
                       value.Severity == DoctorSeverity.Info &&
                       value.Message.Contains("installedBuild=99999999", StringComparison.Ordinal)) &&
                   !wrongBuild.Findings.Any(value => value.Code == "advanced-game-build-incompatible"),
                "A readable Steam build mismatch must be non-blocking Drift context independent of strict package provenance.");

            File.Delete(Path.Combine(steamApps, "appmanifest_2285550.acf"));
            DoctorArtifact unknownBuild = Artifact(
                new DoctorEngine().Inspect(gameRoot, new DoctorOptions { ScanContext = DoctorScanContext.InstalledGame }),
                "DTMAPI.AdvancedFixture");
            Assert(unknownBuild.ReferenceCompatibility == DoctorCompatibilityStatus.ReceiptVerified &&
                   unknownBuild.GameCompatibility == DoctorCompatibilityStatus.Unknown &&
                   unknownBuild.Findings.Any(value =>
                       value.Code == "advanced-game-context-unknown" &&
                       value.Severity == DoctorSeverity.Info &&
                       value.Message.Contains("installedBuild=unknown", StringComparison.Ordinal)) &&
                   !unknownBuild.Findings.Any(value => value.Code == "advanced-game-build-unverifiable"),
                "An unavailable Steam build authority must remain non-blocking Unknown context without being guessed from path or product identity.");

            AdvancedPackageFixture invalidReceipt = WriteAdvancedPackage(
                MakeDirectory(gameRoot, "Mods", "AdvancedInvalidReceipt"),
                "Yuuka.DTMAPI.AutoFishing");
            MutateJson(invalidReceipt.ReceiptPath, value => value["referencePolicySha256"] = new string('0', 64));
            DoctorArtifact invalidInstalled = Artifact(
                new DoctorEngine().Inspect(gameRoot, new DoctorOptions { ScanContext = DoctorScanContext.InstalledGame }),
                "Yuuka.DTMAPI.AutoFishing");
            Assert(invalidInstalled.ReferenceCompatibility == DoctorCompatibilityStatus.Incompatible &&
                   invalidInstalled.GameCompatibility == DoctorCompatibilityStatus.NotChecked &&
                   invalidInstalled.Findings.Any(value => value.Code == "advanced-reference-policy-mismatch"),
                "An untrusted receipt must not be reinterpreted as evidence that the installed game is incompatible; game compatibility remains independently unchecked.");
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static void Draft202012SchemasValidateDoctorAndAdvancedContracts()
    {
        JsonSchema policySchema = LoadSchema("advanced-reference-policy.schema.json");
        JsonSchema registrySchema = LoadSchema("advanced-reference-policy-registry.schema.json");
        JsonSchema receiptSchema = LoadSchema("advanced-reference-receipt.schema.json");
        JsonSchema doctorSchema = LoadSchema("doctor-report.schema.json");

        string trackedPolicyJson = File.ReadAllText(SchemaFixture("doloctown-23762374-g2-v1.json"), Encoding.UTF8);
        AssertSchemaValid(policySchema, trackedPolicyJson, "tracked Advanced reference policy");
        string actionSpeedPolicyJson = File.ReadAllText(SchemaFixture("doloctown-23762374-actionspeed-v1.json"), Encoding.UTF8);
        AssertSchemaValid(policySchema, actionSpeedPolicyJson, "tracked ActionSpeed Advanced reference policy");
        string autoFishingPolicyJson = File.ReadAllText(SchemaFixture("doloctown-24456188-autofishing-v1.json"), Encoding.UTF8);
        AssertSchemaValid(policySchema, autoFishingPolicyJson, "tracked AutoFishing Advanced reference policy");
        string oneActionPolicyJson = File.ReadAllText(SchemaFixture("doloctown-23762374-oneactioncomplete-v1.json"), Encoding.UTF8);
        AssertSchemaValid(policySchema, oneActionPolicyJson, "tracked OneActionComplete Advanced reference policy");
        string registryJson = File.ReadAllText(SchemaFixture("advanced-reference-policy-registry.json"), Encoding.UTF8);
        AssertSchemaValid(registrySchema, registryJson, "structural Advanced policy registry");

        JsonObject extraRegistryRow = JsonNode.Parse(registryJson)!.AsObject();
        extraRegistryRow["policies"]!.AsArray().Add(extraRegistryRow["policies"]![0]!.DeepClone());
        AssertSchemaValid(registrySchema, extraRegistryRow.ToJsonString(), "structural Advanced policy registry extra row");

        JsonObject wrongRegistryBinding = JsonNode.Parse(registryJson)!.AsObject();
        wrongRegistryBinding["policies"]![0]!["requiredUniqueId"] = "DTMAPI.AdvancedFixture";
        AssertSchemaValid(registrySchema, wrongRegistryBinding.ToJsonString(), "structural Advanced policy registry binding");

        JsonObject emptyRegistry = JsonNode.Parse(registryJson)!.AsObject();
        emptyRegistry["policies"] = new JsonArray();
        AssertSchemaInvalid(registrySchema, emptyRegistry.ToJsonString(), "empty Advanced policy registry");

        string root = TempDirectory();
        try
        {
            AdvancedPackageFixture package = WriteAdvancedPackage(MakeDirectory(root, "package"), "DTMAPI.AdvancedFixture");
            string receiptJson = File.ReadAllText(package.ReceiptPath, Encoding.UTF8);
            AssertSchemaValid(receiptSchema, receiptJson, "valid Advanced reference receipt");

            DoctorReport packageReport = new DoctorEngine().Inspect(
                package.RootPath,
                new DoctorOptions { ScanContext = DoctorScanContext.PackageArtifact });
            string packageReportJson = DoctorReportFormatter.ToJson(packageReport);
            Assert(Artifact(packageReport, "DTMAPI.AdvancedFixture").ProvenanceStatus == DoctorProvenanceStatus.VerifiedReferenceReceipt,
                "The schema fixture must exercise a valid Advanced PackageArtifact projection.");
            AssertSchemaValid(doctorSchema, packageReportJson, "valid Advanced PackageArtifact Doctor report");

            JsonObject legacySchema2 = JsonNode.Parse(packageReportJson)!.AsObject();
            foreach (JsonObject artifact in legacySchema2["artifacts"]!.AsArray().Select(value => value!.AsObject()))
            {
                foreach (string property in AdvancedDoctorReportProperties())
                    artifact.Remove(property);
            }
            AssertSchemaValid(doctorSchema, legacySchema2.ToJsonString(), "legacy schema-2 Doctor report without G2 optional projections");

            string installedRoot = MakeDirectory(root, "installed");
            AdvancedPackageFixture blocked = WriteAdvancedPackage(
                MakeDirectory(installedRoot, "Mods", "BlockedAdvanced"),
                "DTMAPI.AdvancedFixture");
            MutateJson(blocked.ReceiptPath, value => value["referencePolicySha256"] = new string('0', 64));
            DoctorReport blockedInstalled = new DoctorEngine().Inspect(
                installedRoot,
                new DoctorOptions { ScanContext = DoctorScanContext.InstalledGame });
            DoctorArtifact blockedArtifact = Artifact(blockedInstalled, "DTMAPI.AdvancedFixture");
            Assert(blockedArtifact.GameCompatibility == DoctorCompatibilityStatus.NotChecked &&
                   blockedArtifact.ProvenanceStatus != DoctorProvenanceStatus.VerifiedReferenceReceipt,
                "Invalid receipt provenance must leave InstalledGame compatibility NotChecked.");
            AssertSchemaValid(doctorSchema, DoctorReportFormatter.ToJson(blockedInstalled), "blocked InstalledGame Doctor report");

            string playerRoot = MakeDirectory(root, "player");
            string playerMod = MakeDirectory(playerRoot, "Mods", "BlockedPlayerDoctor");
            File.Copy(Fixture("DoctorCodeModFixture.dll"), Path.Combine(playerMod, "Blocked.dll"));
            File.WriteAllText(
                Path.Combine(playerMod, "manifest.json"),
                "{\"Name\":\"Blocked\",\"UniqueID\":\"Doctor.Schema.BlockedPlayer\",\"Version\":\"1.0.0\",\"Type\":\"CodeMod\",\"CodeModKind\":\"Advanced\",\"EntryDll\":\"Blocked.dll\"}",
                new UTF8Encoding(false));
            string reportRoot = MakeDirectory(playerRoot, "DTMAPI", "reports");
            string playerJsonPath = Path.Combine(reportRoot, "doctor.schema.json");
            int playerExit = PlayerDoctorApplication.Run(new[]
            {
                "inspect", "--game-root", playerRoot,
                "--scan-context", "installed-game",
                "--json-output", playerJsonPath,
                "--quiet"
            }, new StringWriter(), new StringWriter());
            Assert(playerExit == 2 && File.Exists(playerJsonPath),
                "Blocked Player Doctor diagnostics must still emit the schema-2 report.");
            AssertSchemaValid(doctorSchema, File.ReadAllText(playerJsonPath, Encoding.UTF8), "blocked Player Doctor report");

            JsonObject illegalEnum = JsonNode.Parse(packageReportJson)!.AsObject();
            illegalEnum["scanContext"] = "HandAuthored";
            AssertSchemaInvalid(doctorSchema, illegalEnum.ToJsonString(), "illegal Doctor scanContext enum");

            JsonObject missingRequired = JsonNode.Parse(packageReportJson)!.AsObject();
            missingRequired.Remove("rootPath");
            AssertSchemaInvalid(doctorSchema, missingRequired.ToJsonString(), "Doctor report missing rootPath");

            JsonObject invalidPolicyPath = JsonNode.Parse(trackedPolicyJson)!.AsObject();
            invalidPolicyPath["references"]![0]!["gameRelativePath"] = "../0Harmony.dll";
            AssertSchemaInvalid(policySchema, invalidPolicyPath.ToJsonString(), "policy traversal DLL path");

            JsonObject invalidPolicyAssemblyPath = JsonNode.Parse(trackedPolicyJson)!.AsObject();
            invalidPolicyAssemblyPath["gameAssemblyRelativePath"] = "C:/DolocTown_Data/Managed/Assembly-CSharp.dll";
            AssertSchemaInvalid(policySchema, invalidPolicyAssemblyPath.ToJsonString(), "policy rooted game assembly DLL path");

            JsonObject invalidReceiptPath = JsonNode.Parse(receiptJson)!.AsObject();
            invalidReceiptPath["references"]![0]!["gameRelativePath"] = "BepInEx\\core\\0Harmony.dll";
            AssertSchemaInvalid(receiptSchema, invalidReceiptPath.ToJsonString(), "receipt non-normalized DLL path");

            JsonObject invalidReceiptAssemblyPath = JsonNode.Parse(receiptJson)!.AsObject();
            invalidReceiptAssemblyPath["gameAssemblyRelativePath"] = "DolocTown_Data/../Managed/Assembly-CSharp.dll";
            AssertSchemaInvalid(receiptSchema, invalidReceiptAssemblyPath.ToJsonString(), "receipt traversal game assembly DLL path");

            JsonObject invalidEntryPath = JsonNode.Parse(receiptJson)!.AsObject();
            invalidEntryPath["entryDllPath"] = "Content/DTMAPI/..\\Assembly-CSharp.dll";
            AssertSchemaInvalid(receiptSchema, invalidEntryPath.ToJsonString(), "receipt unsafe entry DLL path");
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static void DoctorEmbeddedAdvancedPolicySetExactlyMatchesCatalog()
    {
        const string registryResource = "DTMAPI.InstallDoctor.AdvancedReferencePolicyRegistry.json";
        const string policyResourcePrefix = "DTMAPI.InstallDoctor.AdvancedReferencePolicy.";
        const string policyResourceSuffix = ".json";

        Assembly doctorAssembly = typeof(DoctorEngine).Assembly;
        using Stream registryStream = doctorAssembly.GetManifestResourceStream(registryResource)
            ?? throw new InvalidDataException("Doctor embedded Advanced policy registry is unavailable.");
        using JsonDocument registry = JsonDocument.Parse(registryStream);
        string[] registeredPolicyIds = registry.RootElement
            .GetProperty("policies")
            .EnumerateArray()
            .Select(row => row.GetProperty("policyId").GetString()!)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        string[] embeddedPolicyIds = doctorAssembly
            .GetManifestResourceNames()
            .Where(name => name.StartsWith(policyResourcePrefix, StringComparison.Ordinal) &&
                           name.EndsWith(policyResourceSuffix, StringComparison.Ordinal))
            .Select(name => name.Substring(
                policyResourcePrefix.Length,
                name.Length - policyResourcePrefix.Length - policyResourceSuffix.Length))
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        Assert(
            embeddedPolicyIds.SequenceEqual(registeredPolicyIds, StringComparer.Ordinal),
            "Doctor embedded policy resource set must exactly match its embedded registry. Registry=" +
            string.Join(",", registeredPolicyIds) + " Embedded=" + string.Join(",", embeddedPolicyIds));

        string[] registeredProductIds = registry.RootElement
            .GetProperty("policies")
            .EnumerateArray()
            .Select(row => row.GetProperty("requiredUniqueId").GetString()!)
            .Where(value => !value.Equals("DTMAPI.AdvancedFixture", StringComparison.Ordinal))
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        using JsonDocument catalog = JsonDocument.Parse(
            File.ReadAllText(SchemaFixture("dtmapi-product-catalog.json"), Encoding.UTF8));
        string[] catalogAdvancedProductIds = catalog.RootElement
            .GetProperty("products")
            .EnumerateArray()
            .Where(row => row.TryGetProperty("codeModKind", out JsonElement kind) &&
                          kind.ValueKind == JsonValueKind.String &&
                          kind.GetString()!.Equals("Advanced", StringComparison.Ordinal))
            .Select(row => row.GetProperty("uniqueId").GetString()!)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        Assert(
            registeredProductIds.SequenceEqual(catalogAdvancedProductIds, StringComparer.Ordinal),
            "Doctor embedded real-product policy set must exactly match the Catalog implemented Advanced set. Doctor=" +
            string.Join(",", registeredProductIds) + " Catalog=" + string.Join(",", catalogAdvancedProductIds));
    }

    private static bool HasArtifact(DoctorReport report, DoctorArtifactKind kind, DoctorPlacement placement, string uniqueId = "")
    {
        return report.Artifacts.Any(value => value.Kind == kind && value.Placement == placement &&
                                             (uniqueId.Length == 0 || value.UniqueId == uniqueId));
    }

    private static void MinimumVersionChecksMatchRuntimeCompatibilitySemantics()
    {
        string root = TempDirectory();
        try
        {
            WriteContentManifest(root, "Current", "Doctor.Current", "0.5.5");
            WriteContentManifest(root, "Legacy", "Doctor.Legacy", "0.4.2");
            WriteContentManifest(root, "Future", "Doctor.Future", "0.5.6-alpha");
            WriteContentManifest(root, "Invalid", "Doctor.Invalid", "definitely-not-a-version");

            DoctorReport report = new DoctorEngine().Inspect(root, new DoctorOptions
            {
                InstalledDtmApiVersion = " 0.5.5-preview+build ",
                ScanContext = DoctorScanContext.InstalledGame
            });
            Assert(Artifact(report, "Doctor.Current").MinimumVersionStatus == DoctorMinimumVersionStatus.Compatible,
                "A suffix-bearing installed 0.5.5 version should satisfy minimum 0.5.5.");
            Assert(Artifact(report, "Doctor.Legacy").MinimumVersionStatus == DoctorMinimumVersionStatus.Compatible,
                "Legacy numeric minimums should remain compatible.");
            Assert(Artifact(report, "Doctor.Future").MinimumVersionStatus == DoctorMinimumVersionStatus.RequiresNewerRuntime,
                "A 0.5.6-alpha minimum should block installed 0.5.5 under Runtime suffix trimming semantics.");
            Assert(Artifact(report, "Doctor.Invalid").MinimumVersionStatus == DoctorMinimumVersionStatus.MinimumVersionInvalid,
                "An invalid manifest minimum should fail closed when compatibility is checked.");
            Assert(report.MinimumBlockedCount == 2 && report.ErrorCount == 2,
                "Future and invalid minimums should be represented as two structured blocking errors.");
            Assert(report.InstalledDtmApiVersion == " 0.5.5-preview+build " && report.RuntimeVersionCheckRequested,
                "The report should preserve the exact installed Runtime projection supplied by the caller.");
            Assert(DoctorReportFormatter.ToHuman(report).Contains("Minimum DTMAPI: 0.5.6-alpha (RequiresNewerRuntime)", StringComparison.Ordinal),
                "Human output should expose the declared minimum and compatibility status.");

            DoctorReport missing = new DoctorEngine().Inspect(root, new DoctorOptions { InstalledDtmApiVersion = string.Empty });
            Assert(missing.Findings.Any(value => value.Code == "installed-dtmapi-version-unavailable"),
                "An explicit unavailable Runtime must be a global structured error.");
            Assert(Artifact(missing, "Doctor.Current").MinimumVersionStatus == DoctorMinimumVersionStatus.RuntimeUnavailable,
                "Declared minimums should report RuntimeUnavailable when the player Runtime is missing.");
            Assert(DoctorReportFormatter.ToSummary(missing).Contains("runtime=missing", StringComparison.Ordinal) &&
                   DoctorReportFormatter.ToSummary(missing).Contains("minimumBlocked=4", StringComparison.Ordinal),
                "The bounded summary should expose missing Runtime and all blocked minimum projections.");

            DoctorReport uncheckedReport = new DoctorEngine().Inspect(root);
            Assert(!uncheckedReport.RuntimeVersionCheckRequested &&
                   uncheckedReport.Artifacts.All(value => value.MinimumVersionStatus == DoctorMinimumVersionStatus.NotChecked),
                "A null InstalledDtmApiVersion must preserve the intentional no-check contract.");
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static void InstalledRuntimeVersionProbeRequiresOneCoherentFiveDllSet()
    {
        string root = TempDirectory();
        try
        {
            string state = MakeDirectory(root, "DTMAPI");
            string plugins = MakeDirectory(root, "BepInEx", "plugins", "DTMAPI");
            string fixture = Fixture("DTMAPI.Abstractions.dll");
            string binary = FileVersionInfo.GetVersionInfo(fixture).FileVersion ?? string.Empty;
            Assert(Version.TryParse(binary, out Version? parsed) && parsed != null, "Fixture FileVersion should be numeric.");
            string product = parsed!.Major + "." + parsed.Minor + "." + Math.Max(parsed.Build, 0);
            WriteVersionReceipt(Path.Combine(state, "install-state.json"), product, binary);
            WriteVersionReceipt(Path.Combine(state, "release-manifest.json"), product, binary);
            foreach (string fileName in RuntimeFileNames())
                File.Copy(Fixture(fileName), Path.Combine(plugins, fileName));

            WriteVersionReceipt(Path.Combine(state, "install-state.json"), "0.5.4", binary);
            WriteVersionReceipt(Path.Combine(state, "release-manifest.json"), "0.5.4", binary);
            InstalledRuntimeVersionProbeResult legacyOmittedHost = InstalledRuntimeVersionProbe.Inspect(root);
            Assert(!legacyOmittedHost.Findings.Any(value => value.Code == "installed-runtime-optional-component-missing"),
                "Pre-0.5.5 receipts must retain their historical no-component shape without a Compatibility Host requirement.");
            WriteVersionReceipt(Path.Combine(state, "install-state.json"), product, binary);
            WriteVersionReceipt(Path.Combine(state, "release-manifest.json"), product, binary);

            InstalledRuntimeVersionProbeResult omittedHost = InstalledRuntimeVersionProbe.Inspect(root);
            Assert(omittedHost.Status == InstalledRuntimeVersionProbeStatus.Inconsistent &&
                   omittedHost.Findings.Any(value => value.Code == "installed-runtime-optional-component-missing"),
                "DTMAPI 0.5.5 and later must not be Doctor-green when both receipts omit the required Compatibility Host.");

            string compatibilityDirectory = MakeDirectory(root, "DTMAPI", "components", "compatibility");
            string componentPath = Path.Combine(compatibilityDirectory, "DTMAPI.GameBridge.DolocTown.Compatibility.dll");
            File.Copy(Fixture("DTMAPI.GameBridge.DolocTown.Compatibility.dll"), componentPath);
            WriteVersionReceiptWithOptionalComponent(Path.Combine(state, "install-state.json"), product, binary, componentPath, "first-frozen-abi-call", "dormant");
            WriteVersionReceiptWithOptionalComponent(Path.Combine(state, "release-manifest.json"), product, binary, componentPath, "first-frozen-abi-call", "dormant");

            InstalledRuntimeVersionProbeResult consistent = InstalledRuntimeVersionProbe.Inspect(root);
            Assert(consistent.Status == InstalledRuntimeVersionProbeStatus.Consistent && consistent.InstalledDtmApiVersion == product && consistent.Findings.Count == 0,
                "Two receipts, five matching Runtime FileVersions and the required Compatibility Host should yield one consistent installed version.");
            Assert(!IsAssemblyLoaded("DoctorCodeModFixture"), "FileVersion cross-checking must not Assembly.Load Runtime candidates.");

            File.Copy(
                Path.Combine(plugins, "DTMAPI.ModConfigMenu.dll"),
                Path.Combine(plugins, "DTMAPI.Core.dll"),
                overwrite: true);
            InstalledRuntimeVersionProbeResult substituted = InstalledRuntimeVersionProbe.Inspect(root);
            Assert(substituted.Status == InstalledRuntimeVersionProbeStatus.Invalid &&
                   substituted.InstalledDtmApiVersion.Length == 0 &&
                   substituted.Findings.Any(value => value.Code == "installed-runtime-assembly-identity-mismatch" &&
                                                       value.Path.EndsWith("DTMAPI.Core.dll", StringComparison.OrdinalIgnoreCase)),
                "A same-version DLL renamed over an exact Runtime filename must be rejected by internal AssemblyName metadata.");
            File.Copy(Fixture("DTMAPI.Core.dll"), Path.Combine(plugins, "DTMAPI.Core.dll"), overwrite: true);

            WriteVersionReceipt(Path.Combine(state, "release-manifest.json"), "99.0.0", binary);
            InstalledRuntimeVersionProbeResult inconsistent = InstalledRuntimeVersionProbe.Inspect(root);
            Assert(inconsistent.Status == InstalledRuntimeVersionProbeStatus.Inconsistent && inconsistent.InstalledDtmApiVersion.Length == 0 &&
                   inconsistent.Findings.Any(value => value.Code == "installed-runtime-receipt-version-inconsistent"),
                "Conflicting receipts must produce a structured error without selecting either version.");

            File.Delete(Path.Combine(plugins, RuntimeFileNames()[0]));
            InstalledRuntimeVersionProbeResult missing = InstalledRuntimeVersionProbe.Inspect(root);
            Assert(missing.Status == InstalledRuntimeVersionProbeStatus.Missing && missing.InstalledDtmApiVersion.Length == 0 &&
                   missing.Findings.Any(value => value.Code == "installed-runtime-assembly-set-incomplete"),
                "Any missing member of the five-DLL Runtime set must classify the installed version as missing.");
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static void InstalledRuntimeVersionProbeRejectsOptionalComponentPolicyDrift()
    {
        string root = TempDirectory();
        try
        {
            string state = MakeDirectory(root, "DTMAPI");
            string plugins = MakeDirectory(root, "BepInEx", "plugins", "DTMAPI");
            string compatibilityDirectory = MakeDirectory(root, "DTMAPI", "components", "compatibility");
            string fixture = Fixture("DTMAPI.Abstractions.dll");
            string binary = FileVersionInfo.GetVersionInfo(fixture).FileVersion ?? string.Empty;
            Assert(Version.TryParse(binary, out Version? parsed) && parsed != null, "Fixture FileVersion should be numeric.");
            string product = parsed!.Major + "." + parsed.Minor + "." + Math.Max(parsed.Build, 0);
            foreach (string fileName in RuntimeFileNames())
                File.Copy(Fixture(fileName), Path.Combine(plugins, fileName));

            string componentPath = Path.Combine(compatibilityDirectory, "DTMAPI.GameBridge.DolocTown.Compatibility.dll");
            File.Copy(Fixture("DTMAPI.GameBridge.DolocTown.Compatibility.dll"), componentPath);
            WriteVersionReceiptWithOptionalComponent(Path.Combine(state, "install-state.json"), product, binary, componentPath, "first-frozen-abi-call", "dormant");
            WriteVersionReceiptWithOptionalComponent(Path.Combine(state, "release-manifest.json"), product, binary, componentPath, "first-frozen-abi-call", "dormant");

            InstalledRuntimeVersionProbeResult consistent = InstalledRuntimeVersionProbe.Inspect(root);
            Assert(consistent.Status == InstalledRuntimeVersionProbeStatus.Consistent && consistent.Findings.Count == 0,
                "A canonical dormant-shipped optional component projection should remain valid.");

            File.Copy(Fixture("DoctorCodeModFixture.dll"), componentPath, overwrite: true);
            WriteVersionReceiptWithOptionalComponent(
                Path.Combine(state, "install-state.json"),
                product,
                binary,
                componentPath,
                "first-frozen-abi-call",
                "dormant",
                assemblyName: "DTMAPI.GameBridge.DolocTown.Compatibility",
                assemblyVersion: "0.5.3.0");
            WriteVersionReceiptWithOptionalComponent(
                Path.Combine(state, "release-manifest.json"),
                product,
                binary,
                componentPath,
                "first-frozen-abi-call",
                "dormant",
                assemblyName: "DTMAPI.GameBridge.DolocTown.Compatibility",
                assemblyVersion: "0.5.3.0");
            InstalledRuntimeVersionProbeResult substitutedBytes = InstalledRuntimeVersionProbe.Inspect(root);
            Assert(substitutedBytes.Status == InstalledRuntimeVersionProbeStatus.Invalid &&
                   substitutedBytes.Findings.Any(value => value.Code == "installed-runtime-optional-component-bytes-invalid"),
                "Coherent receipts must not authorize substituted bytes whose actual managed identity is not the Compatibility Host.");
            File.Copy(Fixture("DTMAPI.GameBridge.DolocTown.Compatibility.dll"), componentPath, overwrite: true);
            WriteVersionReceiptWithOptionalComponent(Path.Combine(state, "install-state.json"), product, binary, componentPath, "first-frozen-abi-call", "dormant");
            WriteVersionReceiptWithOptionalComponent(Path.Combine(state, "release-manifest.json"), product, binary, componentPath, "first-frozen-abi-call", "dormant");

            WriteVersionReceiptWithOptionalComponent(
                Path.Combine(state, "release-manifest.json"),
                product,
                binary,
                componentPath,
                "first-frozen-abi-call",
                "dormant",
                assemblyName: "DTMAPI.GameBridge.DolocTown.Substitute");
            InstalledRuntimeVersionProbeResult nameDrift = InstalledRuntimeVersionProbe.Inspect(root);
            Assert(nameDrift.Status == InstalledRuntimeVersionProbeStatus.Invalid &&
                   nameDrift.Findings.Any(value => value.Code == "installed-runtime-optional-component-receipt-invalid"),
                "Doctor must reject a freely substituted optional-component assembly identity.");

            WriteVersionReceiptWithOptionalComponent(
                Path.Combine(state, "release-manifest.json"),
                product,
                binary,
                componentPath,
                "first-frozen-abi-call",
                "dormant",
                assemblyVersion: "9.9.9.9");
            InstalledRuntimeVersionProbeResult versionDrift = InstalledRuntimeVersionProbe.Inspect(root);
            Assert(versionDrift.Status == InstalledRuntimeVersionProbeStatus.Invalid &&
                   versionDrift.Findings.Any(value => value.Code == "installed-runtime-optional-component-receipt-invalid"),
                "Doctor must reject a freely substituted optional-component AssemblyVersion.");

            WriteVersionReceiptWithOptionalComponent(
                Path.Combine(state, "release-manifest.json"),
                product,
                binary,
                componentPath,
                "first-frozen-abi-call",
                "dormant",
                targetFramework: "net8.0");
            InstalledRuntimeVersionProbeResult frameworkDrift = InstalledRuntimeVersionProbe.Inspect(root);
            Assert(frameworkDrift.Status == InstalledRuntimeVersionProbeStatus.Invalid &&
                   frameworkDrift.Findings.Any(value => value.Code == "installed-runtime-optional-component-receipt-invalid"),
                "Doctor must reject a freely substituted optional-component target framework.");

            WriteVersionReceiptWithOptionalComponent(Path.Combine(state, "release-manifest.json"), product, binary, componentPath, "first-frozen-abi-call", "resident");
            InstalledRuntimeVersionProbeResult stateDrift = InstalledRuntimeVersionProbe.Inspect(root);
            Assert(stateDrift.Status == InstalledRuntimeVersionProbeStatus.Invalid &&
                   stateDrift.Findings.Any(value => value.Code == "installed-runtime-optional-component-receipt-invalid"),
                "Doctor must fail closed when the release receipt moves a dormant component to resident state or disagrees with install-state.");

            WriteVersionReceiptWithOptionalComponent(Path.Combine(state, "install-state.json"), product, binary, componentPath, "startup", "dormant");
            WriteVersionReceiptWithOptionalComponent(Path.Combine(state, "release-manifest.json"), product, binary, componentPath, "startup", "dormant");
            InstalledRuntimeVersionProbeResult policyDrift = InstalledRuntimeVersionProbe.Inspect(root);
            Assert(policyDrift.Status == InstalledRuntimeVersionProbeStatus.Invalid &&
                   policyDrift.Findings.Any(value => value.Code == "installed-runtime-optional-component-receipt-invalid"),
                "Doctor must fail closed when an optional component no longer uses first-frozen-abi-call loading.");
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static void ScanContextBoundsInstalledGameAndUnderstandsPackagePayload()
    {
        string root = TempDirectory();
        try
        {
            string payload = MakeDirectory(root, "Content", "DTMAPIInstaller", "Payload", "BepInEx", "plugins", "DTMAPI");
            File.Copy(Fixture("DTMAPI.Abstractions.dll"), Path.Combine(payload, "DTMAPI.Abstractions.dll"));
            File.Copy(Fixture("DoctorCodeModFixture.dll"), Path.Combine(root, "loose-package-helper.dll"));
            string packagePlugin = Path.Combine(root, "3759797170.dll");
            File.Copy(Fixture("DoctorBepInExPluginFixture.dll"), packagePlugin);

            DoctorReport installed = new DoctorEngine().Inspect(root, new DoctorOptions { ScanContext = DoctorScanContext.InstalledGame });
            Assert(installed.Artifacts.Count == 0 && installed.Findings.Any(value => value.Code == "no-artifacts"),
                "InstalledGame must not traverse package payloads or arbitrary game-root files.");

            DoctorReport package = new DoctorEngine().Inspect(root, new DoctorOptions { ScanContext = DoctorScanContext.PackageArtifact });
            Assert(package.Artifacts.Any(value => value.Kind == DoctorArtifactKind.DtmApiRuntime && value.Placement == DoctorPlacement.Expected),
                "PackageArtifact should recognize a staged BepInEx/plugins/DTMAPI payload as expected rather than misplaced.");
            Assert(package.Artifacts.Any(value => value.Path.EndsWith("loose-package-helper.dll", StringComparison.OrdinalIgnoreCase)),
                "PackageArtifact should inspect the complete explicit package tree.");
            DoctorArtifact externalPackage = package.Artifacts.Single(value => value.Path == packagePlugin);
            Assert(externalPackage.Kind == DoctorArtifactKind.ExternalBepInExPlugin &&
                   externalPackage.Placement == DoctorPlacement.NotApplicable &&
                   externalPackage.Findings.Any(value => value.Code == "package-plugin-placement-not-evaluated" && value.Severity == DoctorSeverity.Warning) &&
                   externalPackage.Findings.All(value => value.Severity != DoctorSeverity.Error),
                "A root-level external plugin package must not be falsely condemned before its installed destination is known.");

            DoctorReport authorDefault = new DoctorEngine().Inspect(root);
            Assert(authorDefault.ScanContext == DoctorScanContext.PackageArtifact &&
                   authorDefault.Artifacts.Any(value => value.Path == packagePlugin),
                "The default Engine context must preserve Author SDK unpacked-package scanning compatibility.");
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static void PlayerDoctorCliIsReadOnlyAndHasBoundedExitContract()
    {
        string root = TempDirectory();
        try
        {
            string plugins = MakeDirectory(root, "BepInEx", "plugins", "External");
            string mods = MakeDirectory(root, "Mods");
            File.Copy(Fixture("DoctorBepInExPluginFixture.dll"), Path.Combine(plugins, "External.dll"));
            string reports = MakeDirectory(root, "DTMAPI", "reports");
            string jsonPath = Path.Combine(reports, "doctor.json");
            string textPath = Path.Combine(reports, "doctor.txt");
            string summaryPath = Path.Combine(reports, "doctor.summary.txt");
            TreeHashSnapshot pluginsBefore = new TreeHasher().Capture(Path.Combine(root, "BepInEx", "plugins"));
            TreeHashSnapshot modsBefore = new TreeHasher().Capture(mods);
            var stdout = new StringWriter();
            var stderr = new StringWriter();

            int exit = PlayerDoctorApplication.Run(new[]
            {
                "inspect", "--game-root", root,
                "--runtime-version", "0.5.5",
                "--scan-context", "installed-game",
                "--json-output", jsonPath,
                "--text-output", textPath,
                "--summary-output", summaryPath,
                "--quiet"
            }, stdout, stderr);
            Assert(exit == 0 && stdout.ToString().Length == 0 && stderr.ToString().Length == 0,
                "A clean quiet player inspection should return 0 without console noise.");
            Assert(!IsAssemblyLoaded("DoctorBepInExPluginFixture"), "Player CLI inspection must not Assembly.Load scanned plugins.");
            Assert(File.Exists(jsonPath) && File.Exists(textPath) && File.Exists(summaryPath),
                "Only explicitly requested report outputs should be written.");
            string summary = File.ReadAllText(summaryPath);
            Assert(summary.Contains("status=ok;artifacts=1;errors=0;warnings=0;misplaced=0;minimumBlocked=0;runtime=0.5.5", StringComparison.Ordinal),
                "The CLI summary should remain one bounded machine-readable line.");
            using (JsonDocument json = JsonDocument.Parse(File.ReadAllText(jsonPath)))
            {
                Assert(json.RootElement.GetProperty("schemaVersion").GetInt32() == 2 &&
                       json.RootElement.GetProperty("installedDtmApiVersion").GetString() == "0.5.5",
                    "JSON output should retain the schema and explicit Runtime version.");
            }
            Assert(pluginsBefore.Sha256 == new TreeHasher().Capture(Path.Combine(root, "BepInEx", "plugins")).Sha256 &&
                   modsBefore.Sha256 == new TreeHasher().Capture(mods).Sha256,
                "CLI report export must not mutate either installed scan root.");

            string advancedCli = MakeDirectory(mods, "AdvancedCli");
            File.Copy(Fixture("DoctorCodeModFixture.dll"), Path.Combine(advancedCli, "AdvancedCli.dll"));
            File.WriteAllText(Path.Combine(advancedCli, "manifest.json"),
                "{\"Name\":\"Advanced CLI\",\"UniqueID\":\"Doctor.AdvancedCli\",\"Version\":\"1.0.0\",\"Type\":\"CodeMod\",\"CodeModKind\":\"Advanced\",\"EntryDll\":\"AdvancedCli.dll\"}");
            string advancedJsonPath = Path.Combine(reports, "doctor.advanced.json");
            int advancedExit = PlayerDoctorApplication.Run(new[]
            {
                "inspect", "--game-root", root,
                "--runtime-version", "0.5.5",
                "--json-output", advancedJsonPath,
                "--quiet"
            }, new StringWriter(), new StringWriter());
            Assert(advancedExit == 2 && File.Exists(advancedJsonPath),
                "Player Doctor must return the completed-diagnostics exit code for a blocked Advanced package.");
            using (JsonDocument advancedJson = JsonDocument.Parse(File.ReadAllText(advancedJsonPath)))
            {
                JsonElement advancedArtifact = advancedJson.RootElement.GetProperty("artifacts")
                    .EnumerateArray()
                    .Single(value => value.GetProperty("uniqueId").GetString() == "Doctor.AdvancedCli");
                Assert(advancedArtifact.GetProperty("managedIdentity").GetString() == "AdvancedCodeMod" &&
                       advancedArtifact.GetProperty("provenanceStatus").GetString() == "MissingReferenceReceipt" &&
                       advancedArtifact.GetProperty("nativeRisk").GetString() == "ProductNative" &&
                       advancedArtifact.GetProperty("restartPolicy").GetString() == "RestartRequiredAfterLoad",
                    "Player Doctor JSON must independently expose Advanced identity/provenance/native-risk/restart facts.");
            }

            string forbidden = Path.Combine(mods, "doctor.json");
            int forbiddenExit = PlayerDoctorApplication.Run(new[]
            {
                "inspect", "--game-root", root, "--runtime-version", "0.5.5", "--json-output", forbidden
            }, new StringWriter(), stderr = new StringWriter());
            Assert(forbiddenExit == 1 && !File.Exists(forbidden) && stderr.ToString().Contains("outside the read-only scanned tree", StringComparison.Ordinal),
                "CLI must reject report writes into the inspected plugin/Mods tree.");

            string wrong = MakeDirectory(root, "BepInEx", "plugins", "WrongCodeMod");
            File.Copy(Fixture("DoctorCodeModFixture.dll"), Path.Combine(wrong, "WrongCodeMod.dll"));
            int diagnosticExit = PlayerDoctorApplication.Run(new[]
            {
                "inspect", "--game-root", root, "--runtime-version", "0.5.5", "--quiet"
            }, new StringWriter(), new StringWriter());
            Assert(diagnosticExit == 2, "Completed placement diagnostics with errors should return 2.");

            stderr = new StringWriter();
            int authorityExit = PlayerDoctorApplication.Run(new[] { "build", root }, new StringWriter(), stderr);
            Assert(authorityExit == 1 && stderr.ToString().Contains("Only inspect, help, and version are available", StringComparison.Ordinal),
                "Player Doctor must reject Author SDK build/deploy authority as a usage failure.");
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static DoctorArtifact Artifact(DoctorReport report, string uniqueId)
    {
        return report.Artifacts.Single(value => value.UniqueId == uniqueId);
    }

    private static void WriteContentManifest(string root, string directoryName, string uniqueId, string minimum)
    {
        string directory = MakeDirectory(root, "Mods", directoryName);
        File.WriteAllText(Path.Combine(directory, "manifest.json"),
            "{\"Name\":\"" + directoryName + "\",\"UniqueID\":\"" + uniqueId + "\",\"Version\":\"1.0.0\",\"Type\":\"ContentPack\",\"MinimumDTMApiVersion\":\"" + minimum + "\"}");
    }

    private static void WriteVersionReceipt(string path, string productVersion, string binaryVersion)
    {
        File.WriteAllText(path, "{\"DTMAPIVersion\":\"" + productVersion + "\",\"BinaryVersion\":\"" + binaryVersion + "\"}");
    }

    private static void WriteVersionReceiptWithOptionalComponent(
        string path,
        string productVersion,
        string binaryVersion,
        string componentPath,
        string loadPolicy,
        string defaultLoadState,
        string? assemblyName = null,
        string? assemblyVersion = null,
        string? targetFramework = null)
    {
        var file = new FileInfo(componentPath);
        string sha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(componentPath)));
        AssemblyName identity = AssemblyName.GetAssemblyName(componentPath);
        var receipt = new
        {
            DTMAPIVersion = productVersion,
            BinaryVersion = binaryVersion,
            OptionalComponents = new[]
            {
                new
                {
                    ComponentId = "gamebridge-compatibility-host",
                    Distribution = "dormant-shipped",
                    LoadPolicy = loadPolicy,
                    RelativePath = "DTMAPI/components/compatibility/DTMAPI.GameBridge.DolocTown.Compatibility.dll",
                    Path = Path.GetFullPath(componentPath),
                    Sha256 = sha256,
                    Length = file.Length,
                    AssemblyName = assemblyName ?? identity.Name ?? string.Empty,
                    AssemblyVersion = assemblyVersion ?? identity.Version?.ToString() ?? string.Empty,
                    FileVersion = FileVersionInfo.GetVersionInfo(componentPath).FileVersion ?? string.Empty,
                    TargetFramework = targetFramework ?? "netstandard2.0",
                    DefaultLoadState = defaultLoadState,
                    IncludedInDownloadPackage = true
                }
            }
        };
        File.WriteAllText(path, JsonSerializer.Serialize(receipt), new UTF8Encoding(false));
    }

    private const string TrackedPolicySha256 = "2E06DB24C3CBBB5E15639FB444BCB4D981F5961402773C0D66A24FAFEEDB8153";
    private const string ActionSpeedPolicySha256 = "BEA3A26C2F831387127A996C326FE0C3F650CCCD7F9555192D76998A2B472473";
    private const string AutoFishingPolicySha256 = "0D9A1FFDB8DAB1A5B43F4B88BCAE77588EEA1C5CA3C5AEDDBBF9F013683DE628";
    private const string HistoricalAutoFishingPolicySha256 = "C434506848F53E21A6371C487B43941FB3408031479FA85E96BB77145AAD5557";
    private const string OneActionCompletePolicySha256 = "A44AF9101D19941B5F019D625A7E74D4D49CCE7CBCF7833E8B4ADCB6BED80A8D";

    private static AdvancedPackageFixture WriteAdvancedPackage(
        string root,
        string uniqueId,
        string manifestKind = "Advanced",
        bool useHistoricalAutoFishingPolicy = false)
    {
        string packageRoot = Path.GetFullPath(root);
        string contentRoot = MakeDirectory(packageRoot, "Content", "DTMAPI");
        string entryPath = Path.Combine(contentRoot, "DoctorAdvancedCodeModFixture.dll");
        File.Copy(Fixture("DoctorAdvancedCodeModFixture.dll"), entryPath, overwrite: true);
        string manifestPath = Path.Combine(contentRoot, "manifest.json");
        bool autoFishingPolicy = uniqueId.Equals("Yuuka.DTMAPI.AutoFishing", StringComparison.Ordinal);
        string minimumDtmApiVersion = autoFishingPolicy && !useHistoricalAutoFishingPolicy ? "0.6.0" : "0.5.5";
        File.WriteAllText(
            manifestPath,
            "{\n" +
            "  \"Name\": \"Advanced Fixture\",\n" +
            "  \"Author\": \"DTMAPI\",\n" +
            "  \"Version\": \"1.0.0\",\n" +
            "  \"UniqueID\": \"" + uniqueId + "\",\n" +
            "  \"Type\": \"CodeMod\",\n" +
            "  \"CodeModKind\": \"" + manifestKind + "\",\n" +
            "  \"EntryDll\": \"Content/DTMAPI/DoctorAdvancedCodeModFixture.dll\",\n" +
            "  \"EntryType\": \"DoctorAdvancedCodeModFixture.FixtureMod\",\n" +
            "  \"MinimumDTMApiVersion\": \"" + minimumDtmApiVersion + "\"\n" +
            "}\n",
            new UTF8Encoding(false));

        var identity = new FileIdentityInspector();
        FileIdentitySnapshot manifest = identity.Inspect(manifestPath);
        FileIdentitySnapshot entry = identity.Inspect(entryPath);
        Assert(manifest.IsComplete && entry.IsComplete, "Synthetic Advanced package bytes must be hashable.");

        bool actionSpeedPolicy = uniqueId.Equals("Yuuka.DTMAPI.ActionSpeed", StringComparison.Ordinal);
        bool oneActionCompletePolicy = uniqueId.Equals("Yuuka.DTMAPI.OneActionComplete", StringComparison.Ordinal);
        string referencePolicyId = actionSpeedPolicy
            ? "doloctown-23762374-actionspeed-v1"
            : autoFishingPolicy
                ? useHistoricalAutoFishingPolicy
                    ? "doloctown-23762374-autofishing-v1"
                    : "doloctown-24456188-autofishing-v1"
                : oneActionCompletePolicy
                    ? "doloctown-23762374-oneactioncomplete-v1"
                    : "doloctown-23762374-g2-v1";
        string referencePolicySha256 = actionSpeedPolicy
            ? ActionSpeedPolicySha256
            : autoFishingPolicy
                ? useHistoricalAutoFishingPolicy
                    ? HistoricalAutoFishingPolicySha256
                    : AutoFishingPolicySha256
                : oneActionCompletePolicy
                    ? OneActionCompletePolicySha256
                    : TrackedPolicySha256;
        string gameBuildId = autoFishingPolicy && !useHistoricalAutoFishingPolicy ? "24456188" : "23762374";
        string gameAssemblySha256 = autoFishingPolicy && !useHistoricalAutoFishingPolicy
            ? "E861E07E3CB82A6A21EEFA292456452F5AD12C25EC57972A59762AD3F3530923"
            : "C416D461C2559DDE8FB34D6B279BA84330E1403D18AB2D32A0224C6760D06404";
        long gameAssemblyLength = autoFishingPolicy && !useHistoricalAutoFishingPolicy ? 6384128L : 5993984L;
        var receipt = new JsonObject
        {
            ["schemaVersion"] = 1,
            ["receiptKind"] = "DTMAPI.AdvancedCodeMod.ReferenceReceipt",
            ["referencePolicyId"] = referencePolicyId,
            ["referencePolicyVersion"] = 1,
            ["referencePolicySha256"] = referencePolicySha256,
            ["uniqueId"] = uniqueId,
            ["codeModKind"] = "Advanced",
            ["targetFramework"] = "netstandard2.0",
            ["gameBuildId"] = gameBuildId,
            ["gameAssemblyRelativePath"] = "DolocTown_Data/Managed/Assembly-CSharp.dll",
            ["gameAssemblySha256"] = gameAssemblySha256,
            ["manifestPath"] = "Content/DTMAPI/manifest.json",
            ["manifestSha256"] = manifest.Sha256,
            ["entryDllPath"] = "Content/DTMAPI/DoctorAdvancedCodeModFixture.dll",
            ["entryDllLength"] = entry.Length,
            ["entryDllSha256"] = entry.Sha256,
            ["harmonyOwner"] = "dtmapi.mod." + uniqueId.ToLowerInvariant(),
            ["references"] = new JsonArray
            {
                new JsonObject
                {
                    ["gameRelativePath"] = "BepInEx/core/0Harmony.dll",
                    ["assemblyName"] = "0Harmony",
                    ["length"] = 204800,
                    ["sha256"] = "1A21CC03424FC82C3DD1346905D16494536B9595AE4162228D99FB7C285C1031",
                    ["copyLocal"] = false
                },
                new JsonObject
                {
                    ["gameRelativePath"] = "DolocTown_Data/Managed/Assembly-CSharp.dll",
                    ["assemblyName"] = "Assembly-CSharp",
                    ["length"] = gameAssemblyLength,
                    ["sha256"] = gameAssemblySha256,
                    ["copyLocal"] = false
                }
            }
        };
        string receiptPath = Path.Combine(contentRoot, "dtmapi-advanced-references.json");
        File.WriteAllText(receiptPath, receipt.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n", new UTF8Encoding(false));
        FileIdentitySnapshot receiptIdentity = identity.Inspect(receiptPath);
        Assert(receiptIdentity.IsComplete, "Synthetic Advanced receipt bytes must be hashable.");
        var marker = new JsonObject
        {
            ["schemaVersion"] = 2,
            ["owner"] = "DTMAPI",
            ["uniqueId"] = uniqueId,
            ["version"] = "1.0.0",
            ["packageKind"] = "CodeMod",
            ["codeModKind"] = "Advanced",
            ["authorSdkVersion"] = "0.1.0",
            ["targetDtmApiVersion"] = "0.5.5",
            ["manifestPath"] = "Content/DTMAPI/manifest.json",
            ["manifestSha256"] = manifest.Sha256,
            ["entryDllPath"] = "Content/DTMAPI/DoctorAdvancedCodeModFixture.dll",
            ["entryDllSha256"] = entry.Sha256,
            ["advancedReferenceReceiptPath"] = "Content/DTMAPI/dtmapi-advanced-references.json",
            ["advancedReferenceReceiptSha256"] = receiptIdentity.Sha256,
            ["authority"] = "dtmapi-author-sdk-package-binding"
        };
        string markerPath = Path.Combine(contentRoot, "dtmapi-package.json");
        File.WriteAllText(markerPath, marker.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n", new UTF8Encoding(false));
        return new AdvancedPackageFixture
        {
            RootPath = packageRoot,
            ManifestPath = manifestPath,
            EntryPath = entryPath,
            ReceiptPath = receiptPath,
            MarkerPath = markerPath
        };
    }

    private static void MutateJson(string path, Action<JsonObject> mutation)
    {
        JsonObject root = JsonNode.Parse(File.ReadAllText(path, Encoding.UTF8))!.AsObject();
        mutation(root);
        File.WriteAllText(path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n", new UTF8Encoding(false));
    }

    private static void RefreshAdvancedBindings(AdvancedPackageFixture fixture)
    {
        var identity = new FileIdentityInspector();
        FileIdentitySnapshot entry = identity.Inspect(fixture.EntryPath);
        Assert(entry.IsComplete, "Mutated Advanced entry bytes must be hashable.");
        MutateJson(fixture.ReceiptPath, value =>
        {
            value["entryDllLength"] = entry.Length;
            value["entryDllSha256"] = entry.Sha256;
        });
        FileIdentitySnapshot receipt = identity.Inspect(fixture.ReceiptPath);
        Assert(receipt.IsComplete, "Mutated Advanced receipt bytes must be hashable.");
        MutateJson(fixture.MarkerPath, value =>
        {
            value["entryDllSha256"] = entry.Sha256;
            value["advancedReferenceReceiptSha256"] = receipt.Sha256;
        });
    }

    private sealed class AdvancedPackageFixture
    {
        public string RootPath { get; init; } = string.Empty;
        public string ManifestPath { get; init; } = string.Empty;
        public string EntryPath { get; init; } = string.Empty;
        public string ReceiptPath { get; init; } = string.Empty;
        public string MarkerPath { get; init; } = string.Empty;
    }

    private static string[] RuntimeFileNames() => new[]
    {
        "DTMAPI.BepInExBootstrap.dll",
        "DTMAPI.Abstractions.dll",
        "DTMAPI.Core.dll",
        "DTMAPI.GameBridge.DolocTown.dll",
        "DTMAPI.ModConfigMenu.dll"
    };

    private static string MakeDirectory(string root, params string[] parts)
    {
        string path = parts.Aggregate(root, Path.Combine);
        Directory.CreateDirectory(path);
        return path;
    }

    private static string Fixture(string fileName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, fileName);
        if (!File.Exists(path))
            throw new FileNotFoundException("Fixture output was not copied.", path);
        return path;
    }

    private static JsonSchema LoadSchema(string fileName)
    {
        if (!SchemaCache.TryGetValue(fileName, out JsonSchema? schema))
        {
            schema = JsonSchema.FromText(File.ReadAllText(SchemaFixture(fileName), Encoding.UTF8));
            SchemaCache.Add(fileName, schema);
        }
        return schema;
    }

    private static string SchemaFixture(string fileName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Schemas", fileName);
        if (!File.Exists(path))
            throw new FileNotFoundException("Schema fixture was not copied.", path);
        return path;
    }

    private static void AssertSchemaValid(JsonSchema schema, string instanceJson, string label)
    {
        using JsonDocument instance = JsonDocument.Parse(instanceJson);
        EvaluationResults result = schema.Evaluate(
            instance.RootElement,
            new EvaluationOptions { OutputFormat = OutputFormat.List });
        Assert(result.IsValid, label + " must satisfy the tracked Draft 2020-12 schema: " + result);
    }

    private static void AssertSchemaInvalid(JsonSchema schema, string instanceJson, string label)
    {
        using JsonDocument instance = JsonDocument.Parse(instanceJson);
        EvaluationResults result = schema.Evaluate(
            instance.RootElement,
            new EvaluationOptions { OutputFormat = OutputFormat.List });
        Assert(!result.IsValid, label + " must be rejected by the tracked Draft 2020-12 schema.");
    }

    private static IEnumerable<string> AdvancedDoctorReportProperties()
    {
        return new[]
        {
            "managedIdentity",
            "declaredCodeModKind",
            "effectiveCodeModKind",
            "declarationStatus",
            "provenanceStatus",
            "provenanceReceiptPath",
            "nativeRisk",
            "referenceCompatibility",
            "gameCompatibility",
            "referencePolicyId",
            "referencePolicyVersion",
            "referencePolicySha256",
            "referenceCount",
            "gameBuildId",
            "gameAssemblySha256",
            "expectedHarmonyOwner",
            "restartPolicy"
        };
    }

    private static bool IsAssemblyLoaded(string simpleName)
    {
        return AppDomain.CurrentDomain.GetAssemblies().Any(value => value.GetName().Name?.Equals(simpleName, StringComparison.OrdinalIgnoreCase) == true);
    }

    private static string TempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "dtmapi-doctor-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void Run(string name, Action test)
    {
        try
        {
            test();
            Console.WriteLine("PASS " + name);
        }
        catch (Exception ex)
        {
            failures++;
            Console.Error.WriteLine("FAIL " + name + ": " + ex);
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
