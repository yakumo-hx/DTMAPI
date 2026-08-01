using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DTMAPI.Tooling.Metadata;

namespace DTMAPI.InstallDoctor;

public sealed class DoctorEngine
{
    private static readonly HashSet<string> RuntimeAssemblyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "DTMAPI.BepInExBootstrap",
        "DTMAPI.Abstractions",
        "DTMAPI.Core",
        "DTMAPI.GameBridge.DolocTown",
        "DTMAPI.ModConfigMenu"
    };

    private readonly PeMetadataInspector metadataInspector = new();
    private readonly FileIdentityInspector fileIdentityInspector = new();
    private readonly TreeHasher treeHasher = new();
    private readonly AdvancedReferencePolicyAuthority advancedReferencePolicy = AdvancedReferencePolicyAuthority.Load();

    public DoctorReport Inspect(string rootPath, DoctorOptions? options = null)
    {
        options ??= new DoctorOptions();
        string root = Path.GetFullPath(rootPath);
        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException("Doctor root does not exist: " + root);

        TreeHashSnapshot? initialTree = options.VerifyReadOnlyTreeHash ? treeHasher.Capture(root) : null;
        var globalFindings = new List<DoctorFinding>(options.ContextFindings ?? Array.Empty<DoctorFinding>());
        AddInstalledVersionFinding(root, options.InstalledDtmApiVersion, globalFindings);
        string[] files = EnumerateScanFiles(root, options.ScanContext, globalFindings)
            .OrderBy(path => Relative(root, path), StringComparer.OrdinalIgnoreCase)
            .ToArray();

        string[] dllPaths = files
            .Where(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        string[] manifestPaths = files
            .Where(IsManifestName)
            .ToArray();

        var inspections = new Dictionary<string, PeMetadataInspection>(StringComparer.OrdinalIgnoreCase);
        foreach (string dllPath in dllPaths)
            inspections[dllPath] = metadataInspector.Inspect(dllPath);

        IReadOnlyList<ObsoleteDeclaration> obsoleteCatalog = inspections.Values
            .Where(value => value.IsManaged && value.AssemblyName.Equals("DTMAPI.Abstractions", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(value => value.AssemblyVersion)
            .Select(value => value.ObsoleteDeclarations)
            .FirstOrDefault() ?? Array.Empty<ObsoleteDeclaration>();

        var artifacts = new List<DoctorArtifact>();
        var associatedDlls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string manifestPath in manifestPaths)
        {
            ManifestProbe manifest = ManifestProbe.Read(manifestPath);
            if (!ShouldClassifyManifest(root, manifest, options.ScanContext))
                continue;
            artifacts.Add(ClassifyManifest(root, manifest, inspections, associatedDlls, obsoleteCatalog, options));
        }

        foreach ((string path, PeMetadataInspection inspection) in inspections.OrderBy(pair => Relative(root, pair.Key), StringComparer.OrdinalIgnoreCase))
        {
            if (associatedDlls.Contains(path))
                continue;
            artifacts.Add(ClassifyLooseAssembly(root, inspection, obsoleteCatalog, options.ScanContext));
        }

        if (artifacts.Count == 0)
        {
            globalFindings.Add(new DoctorFinding
            {
                Code = "no-artifacts",
                Severity = DoctorSeverity.Warning,
                Path = root,
                Message = "No DTMAPI manifest or candidate DLL was found in the inspected tree.",
                Guidance = "Point Doctor at the Doloc Town game root, a Workshop/local package root, or an unpacked mod directory."
            });
        }

        TreeHashSnapshot? finalTree = options.VerifyReadOnlyTreeHash ? treeHasher.Capture(root) : null;
        bool? treeUnchanged = initialTree == null || finalTree == null
            ? null
            : TreeSnapshotsEqual(initialTree, finalTree);
        if (treeUnchanged == false)
        {
            globalFindings.Add(new DoctorFinding
            {
                Code = "tree-changed-during-doctor",
                Severity = DoctorSeverity.Error,
                Path = root,
                Message = "The inspected tree changed while Doctor was running.",
                Guidance = "Doctor does not write to the target. Stop the game, installer, sync client, or other writer and repeat the read-only verification."
            });
        }
        AddTreeErrors(initialTree, "initial-tree-hash-incomplete", globalFindings);
        AddTreeErrors(finalTree, "final-tree-hash-incomplete", globalFindings);

        return new DoctorReport
        {
            RootPath = root,
            ScanContext = options.ScanContext,
            RuntimeVersionCheckRequested = options.InstalledDtmApiVersion != null,
            InstalledDtmApiVersion = options.InstalledDtmApiVersion ?? string.Empty,
            ReadOnlyVerificationRequested = options.VerifyReadOnlyTreeHash,
            TreeUnchanged = treeUnchanged,
            InitialTree = initialTree,
            FinalTree = finalTree,
            Artifacts = artifacts
                .OrderBy(value => Relative(root, value.Path), StringComparer.OrdinalIgnoreCase)
                .ThenBy(value => value.Kind)
                .ToArray(),
            Findings = globalFindings
                .OrderByDescending(value => value.Severity)
                .ThenBy(value => value.Code, StringComparer.Ordinal)
                .ThenBy(value => value.Path, StringComparer.OrdinalIgnoreCase)
                .ToArray()
        };
    }

    private DoctorArtifact ClassifyManifest(
        string scanRoot,
        ManifestProbe manifest,
        IReadOnlyDictionary<string, PeMetadataInspection> inspections,
        ISet<string> associatedDlls,
        IReadOnlyList<ObsoleteDeclaration> obsoleteCatalog,
        DoctorOptions options)
    {
        var findings = new List<DoctorFinding>();
        if (manifest.Error.Length > 0)
        {
            findings.Add(Error("invalid-manifest", manifest.Path, "The manifest could not be parsed: " + manifest.Error,
                "Repair or regenerate the JSON manifest before installing this package."));
            return Artifact(DoctorArtifactKind.Unknown, DoctorPlacement.NotApplicable, manifest.RootPath, manifest, null, findings);
        }

        if (manifest.UniqueId.Length == 0)
            findings.Add(Error("manifest-missing-unique-id", manifest.Path, "The manifest has no non-empty UniqueID.", "Assign a stable author-qualified UniqueID."));
        if (manifest.Version.Length == 0)
            findings.Add(Error("manifest-missing-version", manifest.Path, "The manifest has no non-empty Version.", "Set the package version explicitly."));

        DoctorMinimumVersionStatus minimumVersionStatus = EvaluateMinimumVersion(manifest, options.InstalledDtmApiVersion, findings);

        bool inBepInExPlugins = IsBepInExPluginPath(scanRoot, manifest.Path);
        if (manifest.HasExplicitType &&
            !manifest.Type.Equals("CodeMod", StringComparison.Ordinal) &&
            !manifest.Type.Equals("ContentPack", StringComparison.Ordinal))
        {
            findings.Add(Error("unknown-manifest-type", manifest.Path, "Unsupported manifest Type: " + manifest.Type + ".",
                "Use exactly CodeMod or ContentPack with canonical casing for DTMAPI author packages."));
            return Artifact(
                DoctorArtifactKind.Unknown,
                DoctorPlacement.NotApplicable,
                manifest.RootPath,
                manifest,
                null,
                findings,
                minimumVersionStatus,
                InvalidCodeModProjection(manifest));
        }

        if (manifest.Type.Equals("ContentPack", StringComparison.OrdinalIgnoreCase))
        {
            if (manifest.HasExplicitCodeModKind)
                findings.Add(Error("code-mod-kind-on-content-pack", manifest.Path, "A ContentPack must not declare CodeModKind.", "Remove CodeModKind; ContentPack is a separate managed identity with no code DLL."));
            if (manifest.EntryDll.Length > 0 || manifest.EntryType.Length > 0)
                findings.Add(Error("content-pack-with-code-fields", manifest.Path, "A ContentPack must not declare EntryDll or EntryType.", "Remove all code fields and keep the package JSON/assets-only."));
            if (manifest.EntryDll.Length > 0)
                findings.Add(Error("content-pack-entry-dll", manifest.Path, "A ContentPack must not declare EntryDll.", "Remove EntryDll and keep the package JSON/assets-only."));
            if (manifest.EntryType.Length > 0)
                findings.Add(Error("content-pack-entry-type", manifest.Path, "A ContentPack must not declare EntryType.", "Remove EntryType and keep the package JSON/assets-only."));

            string contentReceiptPath = ResolvePackageRelativePath(manifest.RootPath, AdvancedReferenceReceiptProbe.RelativePath);
            if (File.Exists(contentReceiptPath))
                findings.Add(Error("advanced-reference-receipt-on-content-pack", contentReceiptPath, "A ContentPack contains an Advanced CodeMod reference receipt.", "Remove the code receipt and all code payloads from the ContentPack."));

            string[] packageDlls = SafeEnumerateFiles(manifest.RootPath, findings)
                .Where(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            foreach (string packageDll in packageDlls)
            {
                associatedDlls.Add(packageDll);
                findings.Add(Error("content-pack-contains-dll", packageDll, "A ContentPack contains a DLL.",
                    "Remove executable/native code from the ContentPack. Use a reviewed CodeMod package only when code is required."));
            }

            foreach (ForbiddenNativePayload payload in FindForbiddenNativePayloads(manifest.RootPath, findings))
            {
                findings.Add(BundledNativePayloadFinding(payload));
            }

            DoctorPlacement placement = inBepInExPlugins ? DoctorPlacement.Misplaced : DoctorPlacement.Expected;
            if (placement == DoctorPlacement.Misplaced)
                findings.Add(Misplaced(manifest.Path, "DTMAPI ContentPacks do not belong under BepInEx/plugins."));
            return Artifact(
                DoctorArtifactKind.DtmApiContentPack,
                placement,
                manifest.RootPath,
                manifest,
                null,
                findings,
                minimumVersionStatus,
                new DoctorIdentityProjection
                {
                    ManagedIdentity = DoctorManagedIdentity.ContentPack,
                    DeclarationStatus = DoctorDeclarationStatus.Explicit,
                    ProvenanceStatus = DoctorProvenanceStatus.DeclaredOnly,
                    NativeRisk = DoctorNativeRisk.NoCode,
                    ReferenceCompatibility = DoctorCompatibilityStatus.NotApplicable,
                    GameCompatibility = DoctorCompatibilityStatus.NotApplicable
                });
        }

        if (!manifest.Type.Equals("CodeMod", StringComparison.OrdinalIgnoreCase))
        {
            findings.Add(Error("unknown-manifest-type", manifest.Path, "Unsupported manifest Type: " + manifest.Type + ".",
                "Use exactly CodeMod or ContentPack for DTMAPI author packages."));
            return Artifact(
                DoctorArtifactKind.Unknown,
                DoctorPlacement.NotApplicable,
                manifest.RootPath,
                manifest,
                null,
                findings,
                minimumVersionStatus,
                InvalidCodeModProjection(manifest));
        }

        if (manifest.HasExplicitCodeModKind && !manifest.HasExplicitType)
        {
            findings.Add(Error(
                "code-mod-kind-requires-code-mod-type",
                manifest.Path,
                "CodeModKind requires an explicit Type=CodeMod declaration.",
                "Declare Type exactly as CodeMod or remove CodeModKind to use the legacy native compatibility lane."));
            return Artifact(
                DoctorArtifactKind.Unknown,
                DoctorPlacement.NotApplicable,
                manifest.RootPath,
                manifest,
                null,
                findings,
                minimumVersionStatus,
                InvalidCodeModProjection(manifest));
        }

        if (manifest.HasExplicitCodeModKind &&
            !manifest.CodeModKind.Equals("Strict", StringComparison.Ordinal) &&
            !manifest.CodeModKind.Equals("Advanced", StringComparison.Ordinal))
        {
            findings.Add(Error(
                "unknown-code-mod-kind",
                manifest.Path,
                "Unsupported CodeModKind: " + manifest.CodeModKind + ".",
                "Use exactly Strict or Advanced. Omit CodeModKind only for the third-party legacy native compatibility lane."));
            return Artifact(
                DoctorArtifactKind.Unknown,
                DoctorPlacement.NotApplicable,
                manifest.RootPath,
                manifest,
                null,
                findings,
                minimumVersionStatus,
                InvalidCodeModProjection(manifest));
        }

        bool advanced = manifest.CodeModKind.Equals("Advanced", StringComparison.Ordinal);
        bool legacyNative = !manifest.HasExplicitCodeModKind;
        var identity = new DoctorIdentityProjection
        {
            ManagedIdentity = advanced
                ? DoctorManagedIdentity.AdvancedCodeMod
                : legacyNative
                    ? DoctorManagedIdentity.LegacyNativeCodeMod
                    : DoctorManagedIdentity.StrictCodeMod,
            DeclaredCodeModKind = manifest.HasExplicitCodeModKind ? manifest.CodeModKind : string.Empty,
            EffectiveCodeModKind = advanced ? "Advanced" : legacyNative ? "LegacyNativeCompatibility" : "Strict",
            DeclarationStatus = legacyNative
                ? DoctorDeclarationStatus.LegacyCompatibility
                : DoctorDeclarationStatus.Explicit,
            ProvenanceStatus = advanced
                ? DoctorProvenanceStatus.MissingReferenceReceipt
                : DoctorProvenanceStatus.Unverified,
            NativeRisk = advanced
                ? DoctorNativeRisk.ProductNative
                : legacyNative
                    ? DoctorNativeRisk.ThirdPartyAuthorManaged
                    : DoctorNativeRisk.StableApiOnly,
            ReferenceCompatibility = advanced
                ? DoctorCompatibilityStatus.Unverifiable
                : legacyNative
                    ? DoctorCompatibilityStatus.Unverifiable
                    : DoctorCompatibilityStatus.NotApplicable,
            GameCompatibility = advanced
                ? DoctorCompatibilityStatus.Unverifiable
                : legacyNative
                    ? DoctorCompatibilityStatus.Unverifiable
                    : DoctorCompatibilityStatus.NotApplicable,
            ExpectedHarmonyOwner = advanced ? CanonicalHarmonyOwner(manifest.UniqueId) : string.Empty,
            RestartPolicy = legacyNative
                ? DoctorRestartPolicy.AuthorManagedRestartRequired
                : DoctorRestartPolicy.RestartRequiredAfterLoad
        };
        if (legacyNative)
        {
            findings.Add(Info(
                "legacy-native-author-managed",
                manifest.Path,
                "This omitted-kind DtmMod uses the third-party native compatibility lane. DTMAPI manages discovery, ordering, Entry isolation, logs, and restart visibility; the author manages native hooks, static state, save side effects, and cleanup.",
                "Disable, unsubscribe, or replace this Mod only with a game restart. DTMAPI does not claim that unknown third-party hooks can be hot-unloaded."));
        }

        PeMetadataInspection? inspection = null;
        string entryPath = string.Empty;
        if (manifest.EntryDll.Length == 0)
        {
            findings.Add(Error("code-mod-missing-entry-dll", manifest.Path, "A CodeMod must declare EntryDll.", "Set EntryDll to a relative .dll path inside the package root."));
        }
        else if (!TryResolveContainedDll(manifest.RootPath, manifest.EntryDll, out entryPath, out string pathError))
        {
            findings.Add(Error("invalid-entry-dll-path", manifest.Path, pathError, "Use a relative .dll path that remains inside the package root."));
        }
        else if (!File.Exists(entryPath))
        {
            findings.Add(Error("missing-entry-dll", entryPath, "The declared EntryDll does not exist.", "Build the mod and pack the declared DLL before validation."));
        }
        else
        {
            associatedDlls.Add(entryPath);
            inspection = inspections.TryGetValue(entryPath, out PeMetadataInspection? cached) ? cached : metadataInspector.Inspect(entryPath);
            AddManagedEntryFindings(inspection, manifest, obsoleteCatalog, advanced, legacyNative, identity, findings);
        }

        ValidateCodeModPackage(
            scanRoot,
            manifest,
            inspection,
            entryPath,
            advanced,
            legacyNative,
            options.ScanContext,
            associatedDlls,
            identity,
            findings);

        DoctorPlacement codePlacement = inBepInExPlugins ? DoctorPlacement.Misplaced : DoctorPlacement.Expected;
        if (codePlacement == DoctorPlacement.Misplaced)
            findings.Add(Misplaced(manifest.Path, "Ordinary DTMAPI CodeMods belong in a DTMAPI Mods or official Content/DTMAPI package, not BepInEx/plugins."));
        return Artifact(DoctorArtifactKind.DtmApiCodeMod, codePlacement, manifest.RootPath, manifest, inspection, findings, minimumVersionStatus, identity);
    }

    private static void AddManagedEntryFindings(
        PeMetadataInspection inspection,
        ManifestProbe manifest,
        IReadOnlyList<ObsoleteDeclaration> obsoleteCatalog,
        bool advanced,
        bool legacyNative,
        DoctorIdentityProjection identity,
        ICollection<DoctorFinding> findings)
    {
        if (inspection.Kind == PortableBinaryKind.NativePortableExecutable)
        {
            if (!advanced)
                identity.NativeRisk = DoctorNativeRisk.ForbiddenNativeReference;
            findings.Add(Error("native-entry-dll", inspection.Path, "The CodeMod EntryDll is native and has no CLR metadata.",
                "Build an ordinary DTMAPI CodeMod as a managed netstandard2.0 assembly."));
            return;
        }
        if (inspection.Kind == PortableBinaryKind.DamagedOrUnknown)
        {
            if (!advanced)
                identity.NativeRisk = DoctorNativeRisk.Unknown;
            findings.Add(Error("damaged-entry-dll", inspection.Path, "The CodeMod EntryDll is unreadable or damaged: " + inspection.Error,
                "Rebuild or restore the DLL; Doctor will not attempt to load it."));
            return;
        }

        if (!inspection.DefinesDtmModSubclass)
        {
            findings.Add(new DoctorFinding
            {
                Code = "entry-type-not-proven",
                Severity = DoctorSeverity.Warning,
                Path = inspection.Path,
                Message = manifest.EntryType.Length == 0
                    ? "PE metadata does not show a type directly deriving from DTMAPI.Abstractions.DtmMod."
                    : "PE metadata does not show the declared EntryType directly deriving from DTMAPI.Abstractions.DtmMod.",
                Guidance = "Confirm EntryType and inheritance. Doctor intentionally does not load the assembly or execute type resolution."
            });
        }

        if (!IsNetStandard20(inspection.TargetFramework))
        {
            findings.Add(Error(
                "wrong-target-framework",
                inspection.Path,
                "The CodeMod EntryDll target framework is not netstandard2.0: " + (inspection.TargetFramework.Length == 0 ? "unavailable" : inspection.TargetFramework) + ".",
                "Build every game-loaded DTMAPI CodeMod for netstandard2.0."));
        }

        string[] nativeReferences = NonPlatformAssemblyReferences(inspection).ToArray();
        if (!advanced && !legacyNative && nativeReferences.Length > 0)
        {
            identity.NativeRisk = DoctorNativeRisk.ForbiddenNativeReference;
            findings.Add(Error(
                "strict-native-reference",
                inspection.Path,
                "Strict CodeMod directly references native/runtime assemblies: " + string.Join(", ", nativeReferences) + ".",
                "Rebuild through the Strict Author SDK without native references, or explicitly use the receipt-bound Advanced lane."));
        }
        else if (legacyNative && nativeReferences.Length > 0)
        {
            identity.NativeRisk = DoctorNativeRisk.ThirdPartyAuthorManaged;
            findings.Add(Info(
                "legacy-native-references-author-managed",
                inspection.Path,
                "The legacy compatibility EntryDll directly references native/runtime assemblies: " + string.Join(", ", nativeReferences) + ".",
                "DTMAPI permits cold-start loading but does not certify game-build compatibility, Harmony ownership, save safety, or hot cleanup for these references."));
        }

        foreach (DoctorFinding finding in DeprecatedApiGuidance.Find(inspection, obsoleteCatalog))
            findings.Add(finding);
    }

    private void ValidateCodeModPackage(
        string scanRoot,
        ManifestProbe manifest,
        PeMetadataInspection? inspection,
        string entryPath,
        bool advanced,
        bool legacyNative,
        DoctorScanContext scanContext,
        ISet<string> associatedDlls,
        DoctorIdentityProjection identity,
        ICollection<DoctorFinding> findings)
    {
        string receiptPath = ResolvePackageRelativePath(manifest.RootPath, AdvancedReferenceReceiptProbe.RelativePath);
        int findingsBeforePackageEnumeration = findings.Count;
        string[] packageFiles = SafeEnumerateFiles(manifest.RootPath, findings).ToArray();
        bool packageEnumerationIncomplete = findings
            .Skip(findingsBeforePackageEnumeration)
            .Any(value => value.Code is "directory-unreadable" or "directory-attributes-unreadable");
        string[] packageDlls = packageFiles
            .Where(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var reportedBundledPayloadPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        ForbiddenNativePayload[] legacyForbiddenPayloads = legacyNative
            ? FindForbiddenNativePayloads(packageFiles, entryPath)
                .Where(payload =>
                    payload.MatchedTrackedReferenceBytes ||
                    IsForbiddenBundledAssemblyName(payload.AssemblyName))
                .ToArray()
            : Array.Empty<ForbiddenNativePayload>();
        var legacyForbiddenPaths = new HashSet<string>(
            legacyForbiddenPayloads.Select(payload => Path.GetFullPath(payload.Path)),
            StringComparer.OrdinalIgnoreCase);
        bool invalidAdvancedPackageShape = advanced && packageEnumerationIncomplete;
        bool forbiddenNativePayloadFound = packageEnumerationIncomplete;
        if (packageEnumerationIncomplete)
        {
            findings.Add(Error(
                "package-tree-unreadable",
                manifest.RootPath,
                "The managed package tree could not be enumerated completely, so executable/native payload absence cannot be verified.",
                "Restore read access to the complete immutable package tree and run Doctor again; an incomplete scan never establishes Advanced provenance."));
        }
        foreach (string packageDll in packageDlls)
        {
            associatedDlls.Add(packageDll);
            if (entryPath.Length > 0 && PathsEqual(packageDll, entryPath))
                continue;
            if (legacyNative)
            {
                if (legacyForbiddenPaths.Contains(Path.GetFullPath(packageDll)))
                {
                    identity.NativeRisk = DoctorNativeRisk.ForbiddenNativeReference;
                    if (reportedBundledPayloadPaths.Add(Path.GetFullPath(packageDll)))
                    {
                        ForbiddenNativePayload payload = legacyForbiddenPayloads.First(value =>
                            PathsEqual(value.Path, packageDll));
                        findings.Add(BundledNativePayloadFinding(payload));
                    }
                    continue;
                }
                findings.Add(Info(
                    "legacy-private-dependency-author-managed",
                    packageDll,
                    "The legacy native compatibility package carries an auxiliary DLL that remains third-party author-managed.",
                    "DTMAPI may resolve this dependency during cold-start loading but does not validate or hot-unload its native/static side effects."));
                continue;
            }
            if (advanced)
            {
                invalidAdvancedPackageShape = true;
                reportedBundledPayloadPaths.Add(Path.GetFullPath(packageDll));
            }
            findings.Add(Error(
                advanced ? "bundled-native-runtime-dependency" : "code-mod-extra-dll",
                packageDll,
                advanced
                    ? "An Advanced CodeMod package contains a DLL other than its declared EntryDll. Native reference assemblies must never be bundled."
                    : "A Strict CodeMod package contains a DLL other than its declared EntryDll.",
                "Rebuild and pack through the Author SDK so the package contains exactly one entry DLL and no copied native/runtime dependencies."));
        }

        if (legacyNative)
        {
            foreach (ForbiddenNativePayload payload in legacyForbiddenPayloads)
            {
                identity.NativeRisk = DoctorNativeRisk.ForbiddenNativeReference;
                if (reportedBundledPayloadPaths.Add(Path.GetFullPath(payload.Path)))
                    findings.Add(BundledNativePayloadFinding(payload));
            }
            if (File.Exists(receiptPath))
            {
                findings.Add(Info(
                    "advanced-reference-receipt-ignored-on-legacy",
                    receiptPath,
                    "An omitted-kind legacy compatibility Mod carries an Advanced receipt, but omission never grants Advanced identity.",
                    "Declare and build Advanced through the tracked SDK policy when DTMAPI-owned ProductNative guarantees are required."));
            }
            return;
        }

        foreach (ForbiddenNativePayload payload in FindForbiddenNativePayloads(packageFiles, entryPath))
        {
            forbiddenNativePayloadFound = true;
            invalidAdvancedPackageShape |= advanced;
            if (!reportedBundledPayloadPaths.Add(Path.GetFullPath(payload.Path)))
                continue;
            findings.Add(BundledNativePayloadFinding(payload));
        }

        if (!advanced && forbiddenNativePayloadFound)
            identity.NativeRisk = DoctorNativeRisk.ForbiddenNativeReference;

        if (!advanced)
        {
            if (File.Exists(receiptPath))
            {
                identity.ProvenanceStatus = DoctorProvenanceStatus.ReferenceReceiptMismatch;
                identity.ProvenanceReceiptPath = receiptPath;
                identity.ReferenceCompatibility = DoctorCompatibilityStatus.Incompatible;
                findings.Add(Error(
                    "advanced-reference-receipt-on-strict",
                    receiptPath,
                    "A Strict CodeMod carries an Advanced reference receipt.",
                    "Remove the receipt or rebuild with an explicit, receipt-bound Advanced project declaration."));
            }
            return;
        }

        identity.ProvenanceReceiptPath = receiptPath;
        if (invalidAdvancedPackageShape)
        {
            identity.ProvenanceStatus = DoctorProvenanceStatus.InvalidPackageBinding;
            identity.NativeRisk = DoctorNativeRisk.ForbiddenNativeReference;
            identity.ReferenceCompatibility = DoctorCompatibilityStatus.Incompatible;
            identity.GameCompatibility = DoctorCompatibilityStatus.NotChecked;
            return;
        }

        if (!File.Exists(receiptPath))
        {
            findings.Add(Error(
                "advanced-reference-receipt-missing",
                receiptPath,
                "The Advanced CodeMod reference receipt is missing.",
                "Build and package the explicitly declared Advanced project through the Author SDK. Do not hand-author or bypass the receipt."));
            return;
        }

        AdvancedReferenceReceiptProbe receipt = AdvancedReferenceReceiptProbe.Read(receiptPath);
        if (receipt.Error.Length > 0)
        {
            identity.ProvenanceStatus = DoctorProvenanceStatus.InvalidReferenceReceipt;
            findings.Add(Error(
                "advanced-reference-receipt-invalid",
                receiptPath,
                "The Advanced reference receipt is invalid: " + receipt.Error,
                "Regenerate the package through the Author SDK; unknown, duplicate, missing, or malformed receipt fields fail closed."));
            return;
        }

        identity.ReferencePolicyId = receipt.ReferencePolicyId;
        identity.ReferencePolicyVersion = receipt.ReferencePolicyVersion;
        identity.ReferencePolicySha256 = receipt.ReferencePolicySha256;
        identity.ReferenceCount = receipt.References.Count;
        identity.GameBuildId = receipt.GameBuildId;
        identity.GameAssemblySha256 = receipt.GameAssemblySha256;

        var receiptMismatches = new List<string>();
        if (!receipt.UniqueId.Equals(manifest.UniqueId, StringComparison.Ordinal))
            receiptMismatches.Add("receipt uniqueId does not match manifest UniqueID");
        if (!receipt.CodeModKind.Equals("Advanced", StringComparison.Ordinal))
            receiptMismatches.Add("receipt codeModKind is not Advanced");

        string expectedHarmonyOwner = CanonicalHarmonyOwner(manifest.UniqueId);
        bool harmonyOwnerMismatch = !receipt.HarmonyOwner.Equals(expectedHarmonyOwner, StringComparison.Ordinal);
        if (harmonyOwnerMismatch)
        {
            findings.Add(Error(
                "advanced-harmony-owner-mismatch",
                receiptPath,
                "The Advanced reference receipt harmonyOwner does not match the canonical owner " + expectedHarmonyOwner + ".",
                "Regenerate the package through the Author SDK. Do not hand-author or reuse a Harmony owner from another mod."));
        }

        string expectedManifestPath = ResolvePackageRelativePath(manifest.RootPath, receipt.ManifestPath);
        if (!PathsEqual(expectedManifestPath, manifest.Path))
            receiptMismatches.Add("receipt manifestPath does not identify the inspected manifest");
        else
            CompareFileIdentity(expectedManifestPath, expectedLength: null, receipt.ManifestSha256, "manifest", receiptMismatches);

        string normalizedManifestEntry = manifest.EntryDll.Replace('\\', '/');
        if (!receipt.EntryDllPath.Equals(normalizedManifestEntry, StringComparison.Ordinal))
            receiptMismatches.Add("receipt entryDllPath does not match manifest EntryDll");
        if (entryPath.Length == 0 || !File.Exists(entryPath))
            receiptMismatches.Add("declared EntryDll is unavailable for receipt verification");
        else
            CompareFileIdentity(entryPath, receipt.EntryDllLength, receipt.EntryDllSha256, "entry DLL", receiptMismatches);

        bool receiptTargetFrameworkMismatch = !receipt.TargetFramework.Equals("netstandard2.0", StringComparison.Ordinal);
        if (receiptTargetFrameworkMismatch)
        {
            findings.Add(Error(
                "wrong-target-framework",
                receiptPath,
                "The Advanced reference receipt targetFramework must be exactly netstandard2.0; observed " + receipt.TargetFramework + ".",
                "Regenerate the package through the Author SDK without retargeting any game-loaded assembly."));
        }

        bool nativeReferencePolicyMismatch = false;
        if (inspection == null || !inspection.IsManaged)
            receiptMismatches.Add("entry DLL managed metadata is unavailable");
        else
        {
            if (!IsNetStandard20(inspection.TargetFramework))
                receiptMismatches.Add("entry DLL target framework is not the receipt-bound netstandard2.0 target");
            string[] expectedNativeReferences = receipt.References
                .Select(value => value.AssemblyName)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            string[] actualNativeReferences = NonPlatformAssemblyReferences(inspection)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            nativeReferencePolicyMismatch = !actualNativeReferences.SequenceEqual(expectedNativeReferences, StringComparer.OrdinalIgnoreCase);
            if (nativeReferencePolicyMismatch)
            {
                findings.Add(Error(
                    "advanced-native-reference-policy-mismatch",
                    inspection.Path,
                    "Advanced native AssemblyRefs must exactly match the tracked receipt policy; expected=" +
                    string.Join(",", expectedNativeReferences) + "; actual=" + string.Join(",", actualNativeReferences) + ".",
                    "Rebuild through the Advanced Author SDK using only the tracked reference policy. Do not add, remove, or ambient-probe native references."));
            }
        }

        bool trackedPolicyMismatch = !advancedReferencePolicy.Matches(receipt, out string policyMismatch);
        if (trackedPolicyMismatch)
        {
            findings.Add(Error(
                "advanced-reference-policy-mismatch",
                receiptPath,
                "The Advanced reference receipt does not match the Doctor-embedded tracked policy: " + policyMismatch + ".",
                "Regenerate the complete package from the tracked Advanced reference policy and exact game root."));
        }

        if (receiptMismatches.Count > 0 || harmonyOwnerMismatch || receiptTargetFrameworkMismatch || nativeReferencePolicyMismatch || trackedPolicyMismatch)
        {
            identity.ProvenanceStatus = DoctorProvenanceStatus.ReferenceReceiptMismatch;
            identity.ReferenceCompatibility = DoctorCompatibilityStatus.Incompatible;
            // A malformed or untrusted receipt is not evidence that the installed
            // game itself is incompatible. Stop before consuming its build/reference
            // claims and report the independent game-compatibility axis as unchecked.
            identity.GameCompatibility = DoctorCompatibilityStatus.NotChecked;
            if (receiptMismatches.Count > 0)
            {
                findings.Add(Error(
                    "advanced-reference-receipt-hash-mismatch",
                    receiptPath,
                    "The Advanced reference receipt does not match its manifest, payload, or tracked policy: " + string.Join("; ", receiptMismatches) + ".",
                    "Regenerate the entire package from the explicit game root and tracked reference policy. Do not edit receipt, manifest, or entry bytes independently."));
            }

            return;
        }

        identity.ReferenceCompatibility = DoctorCompatibilityStatus.ReceiptVerified;
        identity.GameCompatibility = DoctorCompatibilityStatus.NotChecked;
        if (!ValidateAdvancedPackageMarker(manifest, receipt, receiptPath, identity, findings))
            return;

        identity.ProvenanceStatus = DoctorProvenanceStatus.VerifiedReferenceReceipt;
        if (scanContext == DoctorScanContext.InstalledGame)
            ValidateInstalledAdvancedCompatibility(scanRoot, receipt, identity, findings);
    }

    private IReadOnlyList<ForbiddenNativePayload> FindForbiddenNativePayloads(
        string packageRoot,
        ICollection<DoctorFinding> findings)
    {
        return FindForbiddenNativePayloads(SafeEnumerateFiles(packageRoot, findings), string.Empty);
    }

    private IReadOnlyList<ForbiddenNativePayload> FindForbiddenNativePayloads(
        IEnumerable<string> packageFiles,
        string admittedEntryPath)
    {
        var payloads = new List<ForbiddenNativePayload>();
        foreach (string path in packageFiles)
        {
            try
            {
                // Hold a read-only share while hashing and inspecting metadata. This makes
                // an exclusive/unreadable payload a fail-closed package finding and prevents
                // another ordinary writer from replacing bytes between these checks.
                using var snapshot = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 128 * 1024,
                    options: FileOptions.SequentialScan);
                long length = snapshot.Length;
                int first = snapshot.ReadByte();
                int second = snapshot.ReadByte();
                bool hasMzHeader = first == 'M' && second == 'Z';
                snapshot.Position = 0;
                string sha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(snapshot));
                if (snapshot.Length != length)
                    throw new IOException("Package payload length changed during inspection.");

                AdvancedReferenceReceiptRow? trackedReference = advancedReferencePolicy.References.FirstOrDefault(reference =>
                    reference.Length == length &&
                    reference.Sha256.Equals(sha256, StringComparison.OrdinalIgnoreCase));

                string assemblyName = string.Empty;
                bool unverifiedExecutable = false;
                if (hasMzHeader)
                {
                    PeMetadataInspection inspection = metadataInspector.Inspect(path);
                    bool isAdmittedEntry = admittedEntryPath.Length > 0 && PathsEqual(path, admittedEntryPath);
                    if (inspection.IsManaged && IsForbiddenBundledAssemblyName(inspection.AssemblyName))
                        assemblyName = inspection.AssemblyName;
                    if (!inspection.IsManaged || !isAdmittedEntry)
                        unverifiedExecutable = true;
                }

                if (trackedReference == null && assemblyName.Length == 0 && !unverifiedExecutable)
                    continue;

                payloads.Add(new ForbiddenNativePayload
                {
                    Path = path,
                    AssemblyName = assemblyName.Length > 0 ? assemblyName : trackedReference?.AssemblyName ?? string.Empty,
                    MatchedTrackedReferenceBytes = trackedReference != null,
                    UnverifiedExecutable = unverifiedExecutable
                });
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                payloads.Add(new ForbiddenNativePayload
                {
                    Path = path,
                    Unreadable = true,
                    InspectionError = ex.GetType().Name + ": " + ex.Message
                });
            }
        }

        return payloads;
    }

    private static bool IsForbiddenBundledAssemblyName(string assemblyName)
    {
        return assemblyName.Equals("Assembly-CSharp", StringComparison.OrdinalIgnoreCase) ||
               assemblyName.Equals("0Harmony", StringComparison.OrdinalIgnoreCase) ||
               assemblyName.StartsWith("Harmony", StringComparison.OrdinalIgnoreCase) ||
               assemblyName.StartsWith("BepInEx", StringComparison.OrdinalIgnoreCase) ||
               assemblyName.StartsWith("UnityEngine", StringComparison.OrdinalIgnoreCase) ||
               assemblyName.StartsWith("Unity.", StringComparison.OrdinalIgnoreCase) ||
               assemblyName.Equals("DTMAPI.Abstractions", StringComparison.OrdinalIgnoreCase) ||
               assemblyName.Equals("DTMAPI.Core", StringComparison.OrdinalIgnoreCase) ||
               assemblyName.Equals("DTMAPI.BepInExBootstrap", StringComparison.OrdinalIgnoreCase) ||
               assemblyName.Equals("DTMAPI.GameBridge.DolocTown", StringComparison.OrdinalIgnoreCase) ||
               assemblyName.Equals("DTMAPI.GameBridge.DolocTown.Compatibility", StringComparison.OrdinalIgnoreCase) ||
               assemblyName.Equals("DTMAPI.ModConfigMenu", StringComparison.OrdinalIgnoreCase);
    }

    private static DoctorFinding BundledNativePayloadFinding(ForbiddenNativePayload payload)
    {
        string evidence = payload.MatchedTrackedReferenceBytes
            ? "its length/SHA-256 exactly matches the tracked native reference bytes for " + payload.AssemblyName
            : payload.Unreadable
                ? "it could not be read and inspected atomically (" + payload.InspectionError + ")"
                : payload.AssemblyName.Length > 0
                    ? "its managed PE AssemblyName is " + payload.AssemblyName
                    : "it has an MZ executable header but is not the one admitted managed EntryDll";
        return Error(
            "bundled-native-runtime-dependency",
            payload.Path,
            "The managed package contains a forbidden native/runtime payload regardless of its filename or extension; " + evidence + ".",
            "Remove the payload and rebuild through the Author SDK. Advanced references must resolve from the explicit game root with CopyLocal=false and must never be packaged.");
    }

    private bool ValidateAdvancedPackageMarker(
        ManifestProbe manifest,
        AdvancedReferenceReceiptProbe receipt,
        string receiptPath,
        DoctorIdentityProjection identity,
        ICollection<DoctorFinding> findings)
    {
        string markerPath = ResolvePackageRelativePath(manifest.RootPath, AdvancedPackageMarkerProbe.RelativePath);
        if (!File.Exists(markerPath))
        {
            identity.ProvenanceStatus = DoctorProvenanceStatus.MissingPackageBinding;
            findings.Add(Error(
                "advanced-package-marker-missing",
                markerPath,
                "The Advanced package is missing its Author SDK package-binding marker.",
                "Rebuild and package through the Author SDK. Do not hand-package or copy an Advanced payload directory."));
            return false;
        }

        AdvancedPackageMarkerProbe marker = AdvancedPackageMarkerProbe.Read(markerPath);
        FileIdentitySnapshot receiptIdentity = fileIdentityInspector.Inspect(receiptPath);
        bool invalid = marker.Error.Length > 0 ||
                       !receiptIdentity.IsComplete ||
                       marker.SchemaVersion != 2 ||
                       !marker.Owner.Equals("DTMAPI", StringComparison.Ordinal) ||
                       !marker.UniqueId.Equals(manifest.UniqueId, StringComparison.Ordinal) ||
                       !marker.Version.Equals(manifest.Version, StringComparison.Ordinal) ||
                       !marker.PackageKind.Equals("CodeMod", StringComparison.Ordinal) ||
                       !marker.CodeModKind.Equals("Advanced", StringComparison.Ordinal) ||
                       !marker.AuthorSdkVersion.Equals("0.1.0", StringComparison.Ordinal) ||
                       !marker.TargetDtmApiVersion.Equals("0.5.5", StringComparison.Ordinal) ||
                       !marker.ManifestPath.Equals(receipt.ManifestPath, StringComparison.Ordinal) ||
                       !marker.ManifestSha256.Equals(receipt.ManifestSha256, StringComparison.OrdinalIgnoreCase) ||
                       !marker.EntryDllPath.Equals(receipt.EntryDllPath, StringComparison.Ordinal) ||
                       !marker.EntryDllSha256.Equals(receipt.EntryDllSha256, StringComparison.OrdinalIgnoreCase) ||
                       !marker.AdvancedReferenceReceiptPath.Equals(AdvancedReferenceReceiptProbe.RelativePath, StringComparison.Ordinal) ||
                       !marker.AdvancedReferenceReceiptSha256.Equals(receiptIdentity.Sha256, StringComparison.OrdinalIgnoreCase) ||
                       !marker.Authority.Equals("dtmapi-author-sdk-package-binding", StringComparison.Ordinal);
        if (!invalid)
            return true;

        identity.ProvenanceStatus = DoctorProvenanceStatus.InvalidPackageBinding;
        string detail = marker.Error.Length > 0
            ? marker.Error
            : !receiptIdentity.IsComplete
                ? "reference receipt bytes are unavailable: " + receiptIdentity.Error
                : "marker values do not bind the exact Advanced manifest, entry DLL, reference receipt, SDK, and Runtime target";
        findings.Add(Error(
            "advanced-package-marker-invalid",
            markerPath,
            "The Advanced package-binding marker is invalid: " + detail + ".",
            "Regenerate the complete package through the Author SDK; unknown fields, stale hashes, and hand-edited bindings fail closed."));
        return false;
    }

    private void ValidateInstalledAdvancedCompatibility(
        string gameRoot,
        AdvancedReferenceReceiptProbe receipt,
        DoctorIdentityProjection identity,
        ICollection<DoctorFinding> findings)
    {
        SteamGameBuildProbe build = SteamGameBuildProbe.Inspect(gameRoot);
        bool gameBuildCompatible = false;
        if (build.Error.Length > 0)
        {
            identity.GameCompatibility = DoctorCompatibilityStatus.Unverifiable;
            findings.Add(Error(
                "advanced-game-build-unverifiable",
                build.ManifestPath.Length > 0 ? build.ManifestPath : gameRoot,
                "The installed Steam game build cannot be verified: " + build.Error,
                "Repair or verify the Steam installation and repeat Doctor. Advanced code remains blocked while the build is unknown."));
        }
        else if (!build.BuildId.Equals(receipt.GameBuildId, StringComparison.Ordinal))
        {
            identity.GameCompatibility = DoctorCompatibilityStatus.Incompatible;
            findings.Add(Error(
                "advanced-game-build-incompatible",
                build.ManifestPath,
                "The Advanced package targets game build " + receipt.GameBuildId + ", but Steam reports build " + build.BuildId + ".",
                "Use an Advanced package and tracked reference policy built for the installed game build."));
        }
        else
        {
            gameBuildCompatible = true;
        }

        bool anyUnverifiable = false;
        bool anyMismatch = false;
        bool gameAssemblyCompatible = false;
        foreach (AdvancedReferenceReceiptRow reference in receipt.References)
        {
            if (!TryResolveContainedPath(gameRoot, reference.GameRelativePath, out string referencePath, out string pathError))
            {
                anyUnverifiable = true;
                findings.Add(Error(
                    "advanced-reference-unverifiable",
                    gameRoot,
                    "Advanced reference path is invalid: " + pathError,
                    "Repair the package through the Author SDK. Reference paths must stay under the explicit game root."));
                continue;
            }

            FileIdentitySnapshot actual = fileIdentityInspector.Inspect(referencePath);
            if (!actual.IsComplete)
            {
                anyUnverifiable = true;
                findings.Add(Error(
                    "advanced-reference-unverifiable",
                    referencePath,
                    "Advanced reference cannot be read: " + actual.Error,
                    "Verify the game/BepInEx installation. Advanced code remains blocked while a receipt-bound reference is unavailable."));
                continue;
            }

            bool matches = actual.Length == reference.Length &&
                           actual.Sha256.Equals(reference.Sha256, StringComparison.OrdinalIgnoreCase);
            if (!matches)
            {
                anyMismatch = true;
                findings.Add(Error(
                    "advanced-reference-hash-mismatch",
                    referencePath,
                    "Advanced reference length/SHA-256 does not match the receipt for " + reference.AssemblyName + ".",
                    "Verify the installed game/BepInEx files and use the tracked policy for this exact build. Do not copy replacement DLLs into the Mod package."));
            }
            else if (reference.GameRelativePath.Equals(receipt.GameAssemblyRelativePath, StringComparison.Ordinal))
            {
                gameAssemblyCompatible = true;
            }
        }

        identity.ReferenceCompatibility = anyMismatch
            ? DoctorCompatibilityStatus.Incompatible
            : anyUnverifiable
                ? DoctorCompatibilityStatus.Unverifiable
                : DoctorCompatibilityStatus.Compatible;
        if (gameBuildCompatible)
        {
            identity.GameCompatibility = gameAssemblyCompatible
                ? DoctorCompatibilityStatus.Compatible
                : anyUnverifiable
                    ? DoctorCompatibilityStatus.Unverifiable
                    : DoctorCompatibilityStatus.Incompatible;
        }
    }

    private void CompareFileIdentity(
        string path,
        long? expectedLength,
        string expectedSha256,
        string label,
        ICollection<string> mismatches)
    {
        FileIdentitySnapshot actual = fileIdentityInspector.Inspect(path);
        if (!actual.IsComplete)
        {
            mismatches.Add(label + " bytes are unavailable: " + actual.Error);
            return;
        }
        if ((expectedLength.HasValue && actual.Length != expectedLength.Value) ||
            !actual.Sha256.Equals(expectedSha256, StringComparison.OrdinalIgnoreCase))
        {
            mismatches.Add(label + " length/SHA-256 does not match the receipt");
        }
    }

    private DoctorArtifact ClassifyLooseAssembly(
        string scanRoot,
        PeMetadataInspection inspection,
        IReadOnlyList<ObsoleteDeclaration> obsoleteCatalog,
        DoctorScanContext scanContext)
    {
        var findings = new List<DoctorFinding>();
        if (inspection.Kind == PortableBinaryKind.NativePortableExecutable)
        {
            DoctorPlacement placement = IsBepInExPluginPath(scanRoot, inspection.Path) ? DoctorPlacement.Unknown : DoctorPlacement.NotApplicable;
            if (placement == DoctorPlacement.Unknown)
            {
                findings.Add(new DoctorFinding
                {
                    Code = "native-binary-in-plugin-tree",
                    Severity = DoctorSeverity.Warning,
                    Path = inspection.Path,
                    Message = "A native binary is present under BepInEx/plugins and cannot be classified as a managed plugin.",
                    Guidance = "Confirm its documented owner and architecture. Doctor will not execute or load it."
                });
            }
            return Artifact(DoctorArtifactKind.NativeBinary, placement, inspection.Path, null, inspection, findings);
        }
        if (inspection.Kind == PortableBinaryKind.DamagedOrUnknown)
        {
            findings.Add(Error("damaged-or-unknown-dll", inspection.Path, "The DLL cannot be parsed safely: " + inspection.Error,
                "Restore it from its owning package or remove it through that package's documented uninstaller; Doctor performs no mutation."));
            return Artifact(DoctorArtifactKind.DamagedAssembly, DoctorPlacement.NotApplicable, inspection.Path, null, inspection, findings);
        }

        if (RuntimeAssemblyNames.Contains(inspection.AssemblyName))
        {
            DoctorPlacement placement = IsDtmApiRuntimePath(scanRoot, inspection.Path) ? DoctorPlacement.Expected : DoctorPlacement.Misplaced;
            if (placement == DoctorPlacement.Misplaced)
                findings.Add(Misplaced(inspection.Path, "DTMAPI Runtime assemblies belong under BepInEx/plugins/DTMAPI and are installer-owned."));
            return Artifact(DoctorArtifactKind.DtmApiRuntime, placement, inspection.Path, null, inspection, findings);
        }

        if (inspection.DefinesDtmModSubclass)
        {
            DoctorPlacement placement = DoctorPlacement.Misplaced;
            findings.Add(Misplaced(inspection.Path, IsBepInExPluginPath(scanRoot, inspection.Path)
                ? "An ordinary DTMAPI CodeMod was found under BepInEx/plugins and has no owning DTMAPI manifest."
                : "A DTMAPI CodeMod entry assembly has no owning manifest."));
            foreach (DoctorFinding finding in DeprecatedApiGuidance.Find(inspection, obsoleteCatalog))
                findings.Add(finding);
            return Artifact(DoctorArtifactKind.DtmApiCodeMod, placement, inspection.Path, null, inspection, findings);
        }

        if (inspection.DefinesBepInExPlugin)
        {
            bool inBepInExPlugins = IsBepInExPluginPath(scanRoot, inspection.Path);
            bool inMods = IsModsPath(scanRoot, inspection.Path, scanContext);
            DoctorPlacement placement = inBepInExPlugins
                ? DoctorPlacement.Expected
                : scanContext == DoctorScanContext.PackageArtifact && !inMods
                    ? DoctorPlacement.NotApplicable
                    : DoctorPlacement.Misplaced;
            if (placement == DoctorPlacement.Misplaced)
                findings.Add(Misplaced(inspection.Path, "An external BepInEx plugin belongs under BepInEx/plugins, not the DTMAPI Mods tree."));
            else if (placement == DoctorPlacement.NotApplicable)
            {
                findings.Add(new DoctorFinding
                {
                    Code = "package-plugin-placement-not-evaluated",
                    Severity = DoctorSeverity.Warning,
                    Path = inspection.Path,
                    Message = "This package artifact contains a BepInEx plugin entry, but its eventual installed placement cannot be proven from the package-relative path.",
                    Guidance = "Validate the owning package's documented install destination. Doctor will not load, move, adopt, enable, disable, or delete this DLL."
                });
            }
            return Artifact(DoctorArtifactKind.ExternalBepInExPlugin, placement, inspection.Path, null, inspection, findings);
        }

        if (IsBepInExPluginPath(scanRoot, inspection.Path) || IsModsPath(scanRoot, inspection.Path, scanContext))
        {
            findings.Add(new DoctorFinding
            {
                Code = "unknown-managed-dll",
                Severity = DoctorSeverity.Warning,
                Path = inspection.Path,
                Message = "The managed DLL is not a recognized DTMAPI Runtime component, DtmMod entry, or BepInEx plugin entry.",
                Guidance = "Confirm which package owns this helper DLL. Doctor will not adopt, move, delete, enable, or disable it."
            });
        }
        return Artifact(DoctorArtifactKind.Unknown, DoctorPlacement.Unknown, inspection.Path, null, inspection, findings);
    }

    private static DoctorArtifact Artifact(
        DoctorArtifactKind kind,
        DoctorPlacement placement,
        string path,
        ManifestProbe? manifest,
        PeMetadataInspection? inspection,
        IEnumerable<DoctorFinding> findings,
        DoctorMinimumVersionStatus minimumVersionStatus = DoctorMinimumVersionStatus.NotChecked,
        DoctorIdentityProjection? identity = null)
    {
        identity ??= DefaultIdentity(kind);
        return new DoctorArtifact
        {
            Kind = kind,
            Placement = placement,
            Path = path,
            ManifestPath = manifest?.Path ?? string.Empty,
            UniqueId = manifest?.UniqueId ?? string.Empty,
            Version = manifest?.Version ?? string.Empty,
            MinimumDtmApiVersion = manifest?.MinimumDtmApiVersion ?? string.Empty,
            MinimumVersionStatus = minimumVersionStatus,
            AssemblyName = inspection?.AssemblyName ?? string.Empty,
            AssemblyVersion = inspection?.AssemblyVersion?.ToString() ?? string.Empty,
            Sha256 = inspection?.Sha256 ?? string.Empty,
            ManagedIdentity = identity.ManagedIdentity,
            DeclaredCodeModKind = identity.DeclaredCodeModKind,
            EffectiveCodeModKind = identity.EffectiveCodeModKind,
            DeclarationStatus = identity.DeclarationStatus,
            ProvenanceStatus = identity.ProvenanceStatus,
            ProvenanceReceiptPath = identity.ProvenanceReceiptPath,
            NativeRisk = identity.NativeRisk,
            ReferenceCompatibility = identity.ReferenceCompatibility,
            GameCompatibility = identity.GameCompatibility,
            ReferencePolicyId = identity.ReferencePolicyId,
            ReferencePolicyVersion = identity.ReferencePolicyVersion,
            ReferencePolicySha256 = identity.ReferencePolicySha256,
            ReferenceCount = identity.ReferenceCount,
            GameBuildId = identity.GameBuildId,
            GameAssemblySha256 = identity.GameAssemblySha256,
            ExpectedHarmonyOwner = identity.ExpectedHarmonyOwner,
            RestartPolicy = identity.RestartPolicy,
            Findings = findings
                .OrderByDescending(value => value.Severity)
                .ThenBy(value => value.Code, StringComparer.Ordinal)
                .ThenBy(value => value.Path, StringComparer.OrdinalIgnoreCase)
                .ToArray()
        };
    }

    private static DoctorIdentityProjection DefaultIdentity(DoctorArtifactKind kind)
    {
        return kind switch
        {
            DoctorArtifactKind.DtmApiRuntime => new DoctorIdentityProjection
            {
                ManagedIdentity = DoctorManagedIdentity.DtmApiRuntime,
                ProvenanceStatus = DoctorProvenanceStatus.NotApplicable,
                NativeRisk = DoctorNativeRisk.NotApplicable
            },
            DoctorArtifactKind.DtmApiContentPack => new DoctorIdentityProjection
            {
                ManagedIdentity = DoctorManagedIdentity.ContentPack,
                DeclarationStatus = DoctorDeclarationStatus.Explicit,
                ProvenanceStatus = DoctorProvenanceStatus.DeclaredOnly,
                NativeRisk = DoctorNativeRisk.NoCode
            },
            DoctorArtifactKind.ExternalBepInExPlugin => new DoctorIdentityProjection
            {
                ManagedIdentity = DoctorManagedIdentity.ExternalBepInExPlugin,
                ProvenanceStatus = DoctorProvenanceStatus.ExternalUnmanaged,
                NativeRisk = DoctorNativeRisk.ExternalUnmanaged,
                RestartPolicy = DoctorRestartPolicy.OutsideDtmApiManagement
            },
            DoctorArtifactKind.NativeBinary => new DoctorIdentityProjection
            {
                ManagedIdentity = DoctorManagedIdentity.NativeBinary,
                NativeRisk = DoctorNativeRisk.Unknown
            },
            DoctorArtifactKind.DtmApiCodeMod => new DoctorIdentityProjection
            {
                ManagedIdentity = DoctorManagedIdentity.Unknown,
                DeclarationStatus = DoctorDeclarationStatus.Missing,
                ProvenanceStatus = DoctorProvenanceStatus.Unverified,
                NativeRisk = DoctorNativeRisk.Unknown
            },
            _ => new DoctorIdentityProjection()
        };
    }

    private static DoctorIdentityProjection InvalidCodeModProjection(ManifestProbe manifest)
    {
        return new DoctorIdentityProjection
        {
            ManagedIdentity = DoctorManagedIdentity.Unknown,
            DeclaredCodeModKind = manifest.HasExplicitCodeModKind ? manifest.CodeModKind : string.Empty,
            DeclarationStatus = DoctorDeclarationStatus.Invalid,
            ProvenanceStatus = DoctorProvenanceStatus.InvalidDeclaration,
            NativeRisk = DoctorNativeRisk.Unknown
        };
    }

    private static DoctorFinding Error(string code, string path, string message, string guidance)
    {
        return new DoctorFinding { Code = code, Severity = DoctorSeverity.Error, Path = path, Message = message, Guidance = guidance };
    }

    private static DoctorFinding Info(string code, string path, string message, string guidance)
    {
        return new DoctorFinding { Code = code, Severity = DoctorSeverity.Info, Path = path, Message = message, Guidance = guidance };
    }

    private static DoctorFinding Misplaced(string path, string message)
    {
        return new DoctorFinding
        {
            Code = "misplaced-artifact",
            Severity = DoctorSeverity.Error,
            Path = path,
            Message = message,
            Guidance = "Repackage through the Author SDK or use the owning installer/uninstaller. Doctor is read-only and will not move or adopt this file."
        };
    }

    private static DoctorMinimumVersionStatus EvaluateMinimumVersion(
        ManifestProbe manifest,
        string? installedDtmApiVersion,
        ICollection<DoctorFinding> findings)
    {
        if (string.IsNullOrWhiteSpace(manifest.MinimumDtmApiVersion))
            return installedDtmApiVersion == null
                ? DoctorMinimumVersionStatus.NotChecked
                : DoctorMinimumVersionStatus.NotDeclared;

        if (installedDtmApiVersion == null)
            return DoctorMinimumVersionStatus.NotChecked;

        if (!TryParseRuntimeVersion(manifest.MinimumDtmApiVersion, out Version minimum))
        {
            findings.Add(Error(
                "invalid-minimum-dtmapi-version",
                manifest.Path,
                "MinimumDTMApiVersion cannot be parsed with the Runtime compatibility rules: " + manifest.MinimumDtmApiVersion + ".",
                "Restore a valid numeric version. Suffixes after '-' or '+' are accepted for historical compatibility."));
            return DoctorMinimumVersionStatus.MinimumVersionInvalid;
        }

        if (string.IsNullOrWhiteSpace(installedDtmApiVersion))
        {
            findings.Add(Error(
                "minimum-dtmapi-version-runtime-unavailable",
                manifest.Path,
                "This package requires DTMAPI " + manifest.MinimumDtmApiVersion + ", but no installed Runtime version was available.",
                "Run 1_install_dtmapi.bat to install or repair DTMAPI, then repeat the read-only check."));
            return DoctorMinimumVersionStatus.RuntimeUnavailable;
        }

        if (!TryParseRuntimeVersion(installedDtmApiVersion, out Version installed))
        {
            findings.Add(Error(
                "minimum-dtmapi-version-runtime-invalid",
                manifest.Path,
                "This package requires DTMAPI " + manifest.MinimumDtmApiVersion + ", but the installed Runtime version is invalid: " + installedDtmApiVersion + ".",
                "Repair DTMAPI through 1_install_dtmapi.bat, then repeat the read-only check."));
            return DoctorMinimumVersionStatus.RuntimeVersionInvalid;
        }

        if (CompareVersions(installed, minimum) < 0)
        {
            findings.Add(Error(
                "minimum-dtmapi-version-blocked",
                manifest.Path,
                "This package requires DTMAPI " + manifest.MinimumDtmApiVersion + ", but the installed Runtime is " + installedDtmApiVersion + ".",
                "Run 1_install_dtmapi.bat to update DTMAPI before enabling this package."));
            return DoctorMinimumVersionStatus.RequiresNewerRuntime;
        }

        return DoctorMinimumVersionStatus.Compatible;
    }

    private static void AddInstalledVersionFinding(string root, string? installedDtmApiVersion, ICollection<DoctorFinding> findings)
    {
        if (installedDtmApiVersion == null)
            return;
        if (string.IsNullOrWhiteSpace(installedDtmApiVersion))
        {
            findings.Add(Error(
                "installed-dtmapi-version-unavailable",
                root,
                "No installed DTMAPI Runtime version was available for the player check.",
                "Run 1_install_dtmapi.bat to install or repair DTMAPI. Doctor will not install or modify it."));
            return;
        }
        if (!TryParseRuntimeVersion(installedDtmApiVersion, out _))
        {
            findings.Add(Error(
                "installed-dtmapi-version-invalid",
                root,
                "The installed DTMAPI Runtime version cannot be parsed: " + installedDtmApiVersion + ".",
                "Repair DTMAPI through 1_install_dtmapi.bat. Doctor will not replace or modify it."));
        }
    }

    private static bool TryParseRuntimeVersion(string value, out Version version)
    {
        version = new Version(0, 0, 0, 0);
        string text = (value ?? string.Empty).Trim();
        int suffixIndex = text.IndexOfAny(new[] { '-', '+' });
        if (suffixIndex >= 0)
            text = text.Substring(0, suffixIndex);
        if (!Version.TryParse(text, out Version? parsed) || parsed == null)
            return false;
        version = parsed;
        return true;
    }

    private static int CompareVersions(Version left, Version right)
    {
        int[] leftParts = { left.Major, left.Minor, Math.Max(left.Build, 0), Math.Max(left.Revision, 0) };
        int[] rightParts = { right.Major, right.Minor, Math.Max(right.Build, 0), Math.Max(right.Revision, 0) };
        for (int index = 0; index < leftParts.Length; index++)
        {
            int comparison = leftParts[index].CompareTo(rightParts[index]);
            if (comparison != 0)
                return comparison;
        }
        return 0;
    }

    private static bool TryResolveContainedDll(string root, string relativePath, out string fullPath, out string error)
    {
        if (!relativePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            fullPath = string.Empty;
            error = "EntryDll must name a .dll file: " + relativePath;
            return false;
        }

        if (!TryResolveContainedPath(root, relativePath, out fullPath, out error))
        {
            error = error.Replace("Path", "EntryDll", StringComparison.Ordinal);
            return false;
        }
        return true;
    }

    private static bool TryResolveContainedPath(string root, string relativePath, out string fullPath, out string error)
    {
        fullPath = string.Empty;
        error = string.Empty;
        if (Path.IsPathRooted(relativePath))
        {
            error = "Path must be relative, not rooted: " + relativePath;
            return false;
        }

        try
        {
            string rootFull = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            fullPath = Path.GetFullPath(Path.Combine(rootFull, relativePath));
            if (!IsUnder(fullPath, rootFull))
            {
                error = "Path escapes the owning root: " + relativePath;
                fullPath = string.Empty;
                return false;
            }
            if (ContainsReparsePoint(rootFull, fullPath, out string reparseError))
            {
                error = reparseError;
                fullPath = string.Empty;
                return false;
            }
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            error = "Path is invalid: " + ex.Message;
            fullPath = string.Empty;
            return false;
        }
    }

    private static bool ContainsReparsePoint(string root, string candidate, out string error)
    {
        error = string.Empty;
        string relative = Path.GetRelativePath(root, candidate);
        string current = root;
        foreach (string segment in relative.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            if (!File.Exists(current) && !Directory.Exists(current))
                continue;
            try
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                {
                    error = "Path traverses a reparse point: " + current;
                    return true;
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                error = "Path attributes are unavailable: " + ex.GetType().Name + ": " + ex.Message;
                return true;
            }
        }
        return false;
    }

    private static string ResolvePackageRelativePath(string root, string relativePath)
    {
        if (!TryResolveContainedPath(root, relativePath.Replace('/', Path.DirectorySeparatorChar), out string path, out string error))
            throw new InvalidDataException(error);
        return path;
    }

    private static string CanonicalHarmonyOwner(string uniqueId)
    {
        return uniqueId.Length == 0 ? string.Empty : "dtmapi.mod." + uniqueId.ToLowerInvariant();
    }

    private static bool IsNetStandard20(string targetFramework)
    {
        return targetFramework.Equals("netstandard2.0", StringComparison.OrdinalIgnoreCase) ||
               targetFramework.Equals(".NETStandard,Version=v2.0", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> NonPlatformAssemblyReferences(PeMetadataInspection inspection)
    {
        return inspection.AssemblyReferences
            .Where(value =>
                !value.Equals("DTMAPI.Abstractions", StringComparison.OrdinalIgnoreCase) &&
                !value.Equals("netstandard", StringComparison.OrdinalIgnoreCase) &&
                !value.Equals("mscorlib", StringComparison.OrdinalIgnoreCase) &&
                !value.Equals("System", StringComparison.OrdinalIgnoreCase) &&
                !value.StartsWith("System.", StringComparison.OrdinalIgnoreCase) &&
                !value.Equals("Microsoft.CSharp", StringComparison.OrdinalIgnoreCase))
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase);
    }

    private static bool PathsEqual(string left, string right)
    {
        return Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Equals(
                Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase);
    }

    private static bool TreeSnapshotsEqual(TreeHashSnapshot left, TreeHashSnapshot right)
    {
        return left.FileCount == right.FileCount &&
               left.TotalBytes == right.TotalBytes &&
               left.Sha256.Equals(right.Sha256, StringComparison.OrdinalIgnoreCase) &&
               left.Errors.SequenceEqual(right.Errors, StringComparer.Ordinal);
    }

    private static void AddTreeErrors(TreeHashSnapshot? snapshot, string code, ICollection<DoctorFinding> findings)
    {
        if (snapshot == null || snapshot.Errors.Count == 0)
            return;
        findings.Add(new DoctorFinding
        {
            Code = code,
            Severity = DoctorSeverity.Warning,
            Path = snapshot.RootPath,
            Message = "Tree hashing completed with " + snapshot.Errors.Count + " skipped or unreadable path(s).",
            Guidance = "Review InitialTree/FinalTree errors. An incomplete tree hash is evidence with an explicit limitation, not proof of a complete unchanged tree."
        });
    }

    private static IEnumerable<string> EnumerateScanFiles(
        string root,
        DoctorScanContext scanContext,
        ICollection<DoctorFinding> findings)
    {
        if (scanContext == DoctorScanContext.PackageArtifact)
        {
            foreach (string path in SafeEnumerateFiles(root, findings))
                yield return path;
            yield break;
        }

        string[] installedRoots =
        {
            Path.Combine(root, "BepInEx", "plugins"),
            Path.Combine(root, "Mods")
        };
        foreach (string installedRoot in installedRoots)
        {
            if (!Directory.Exists(installedRoot))
                continue;
            foreach (string path in SafeEnumerateFiles(installedRoot, findings))
                yield return path;
        }
    }

    private static IEnumerable<string> SafeEnumerateFiles(string root, ICollection<DoctorFinding> findings)
    {
        var pending = new Stack<string>();
        pending.Push(Path.GetFullPath(root));
        while (pending.Count > 0)
        {
            string directory = pending.Pop();
            string[] files;
            string[] directories;
            try
            {
                files = Directory.GetFiles(directory);
                directories = Directory.GetDirectories(directory);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                findings.Add(new DoctorFinding
                {
                    Code = "directory-unreadable",
                    Severity = DoctorSeverity.Warning,
                    Path = directory,
                    Message = ex.GetType().Name + ": " + ex.Message,
                    Guidance = "Grant read access or inspect this subtree separately; Doctor made no changes."
                });
                continue;
            }

            foreach (string file in files)
                yield return file;
            foreach (string child in directories)
            {
                try
                {
                    if ((File.GetAttributes(child) & FileAttributes.ReparsePoint) != 0)
                        continue;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    findings.Add(new DoctorFinding
                    {
                        Code = "directory-attributes-unreadable",
                        Severity = DoctorSeverity.Warning,
                        Path = child,
                        Message = ex.GetType().Name + ": " + ex.Message,
                        Guidance = "Inspect this path separately; Doctor made no changes."
                    });
                    continue;
                }
                pending.Push(child);
            }
        }
    }

    private static bool IsManifestName(string path)
    {
        string name = Path.GetFileName(path);
        return name.Equals("manifest.json", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("dtmapi.manifest.json", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldClassifyManifest(
        string root,
        ManifestProbe manifest,
        DoctorScanContext scanContext)
    {
        if (scanContext == DoctorScanContext.PackageArtifact)
            return true;

        if (Path.GetFileName(manifest.Path).Equals("dtmapi.manifest.json", StringComparison.OrdinalIgnoreCase) ||
            IsModsPath(root, manifest.Path, scanContext) ||
            IsDtmApiContentManifestPath(root, manifest.Path))
            return true;

        // BepInEx packages commonly carry a generic or Thunderstore manifest.json next to
        // their plugin DLL. In an installed-game scan that file isn't DTMAPI-owned unless
        // it carries both a DTMAPI identity and a DTMAPI-specific declaration marker.
        return manifest.Error.Length == 0 && manifest.HasStrongDtmApiMarker;
    }

    private static bool IsDtmApiContentManifestPath(string root, string path)
    {
        string relative = "/" + Relative(root, path).TrimStart('/') + "/";
        return relative.Contains("/Content/DTMAPI/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDtmApiRuntimePath(string root, string path)
    {
        string relative = "/" + Relative(root, path).TrimStart('/') + "/";
        return relative.Contains("/BepInEx/plugins/DTMAPI/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBepInExPluginPath(string root, string path)
    {
        string relative = "/" + Relative(root, path).TrimStart('/') + "/";
        return relative.Contains("/BepInEx/plugins/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsModsPath(string root, string path, DoctorScanContext scanContext)
    {
        string relative = "/" + Relative(root, path).TrimStart('/') + "/";
        return scanContext == DoctorScanContext.PackageArtifact
            ? relative.Contains("/Mods/", StringComparison.OrdinalIgnoreCase)
            : relative.StartsWith("/Mods/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUnder(string candidate, string root)
    {
        string relative = Path.GetRelativePath(root, candidate);
        return !relative.Equals("..", StringComparison.Ordinal) &&
               !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) &&
               !Path.IsPathRooted(relative);
    }

    private static string Relative(string root, string path) => Path.GetRelativePath(root, path).Replace('\\', '/');
}
