using DTMAPI.Authoring.Contracts;
using DTMAPI.AuthorSdk;
using DTMAPI.Testing;
using CoreManagedModClassifier = DTMAPI.Core.Manifesting.ManagedModClassifier;
using CoreManagedModIdentity = DTMAPI.Core.Manifesting.ManagedModIdentity;
using CoreManagedModClassification = DTMAPI.Core.Manifesting.ManagedModClassification;
using CoreManifestModel = DTMAPI.Core.Manifesting.ManifestModel;
using CoreManifestReader = DTMAPI.Core.Manifesting.ManifestReader;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Runtime.Versioning;

namespace DTMAPI.AuthorSdk.Tests;

internal static class Program
{
    private const long AdvancedAssemblyLength = 5993984;
    private const string AdvancedAssemblySha256 = "c416d461c2559dde8fb34d6b279ba84330e1403d18ab2d32a0224c6760d06404";
    private const long AutoFishingAssemblyLength = 6384128;
    private const string AutoFishingAssemblySha256 = "e861e07e3cb82a6a21eefa292456452f5ad12c25ec57972a59762ad3f3530923";
    private const long AdvancedHarmonyLength = 204800;
    private const string AdvancedHarmonySha256 = "1a21cc03424fc82c3dd1346905d16494536b9595ae4162228d99fb7c285c1031";
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public static async Task<int> Main(string[] args)
    {
        bool requireExactAdvancedReference = ParseArguments(args);
        string repository = FindRepository();
        using DtmApiTestSession testSession = DtmApiTestSession.Start("DTMAPI.AuthorSdk.Tests");
        string temporaryRoot = Path.Combine(testSession.RootPath, "workspace");
        string? previousStateRoot = Environment.GetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT");
        Directory.CreateDirectory(temporaryRoot);
        try
        {
            TestPublishedDeploymentJournalContract(repository);
            TestPublishedAdvancedReferencePolicyContract(repository);
            string compatibility = CreateCompatibilityPayload(repository, temporaryRoot);
            Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT", Path.Combine(temporaryRoot, "author-state"));
            await TestCodeModRoundTripAndDeterminism(temporaryRoot, compatibility).ConfigureAwait(false);
            bool advancedReferenceFixtureExecuted = await TestAdvancedCodeModReferencePackageAndDeploy(
                temporaryRoot,
                compatibility,
                repository,
                requireExactAdvancedReference).ConfigureAwait(false);
            await TestContentPackRoundTripAndDeterminism(temporaryRoot).ConfigureAwait(false);
            await TestValidationFailures(temporaryRoot, compatibility).ConfigureAwait(false);
            await TestUnsafeCommandsRefuse(temporaryRoot).ConfigureAwait(false);
            await TestPausedLegacyGameModsMutations(temporaryRoot).ConfigureAwait(false);
            DeploymentPackages deploymentPackages = await CreateDeploymentPackages(temporaryRoot, compatibility).ConfigureAwait(false);
            await TestLegacyDeploymentSchemaUpgrade(temporaryRoot, deploymentPackages).ConfigureAwait(false);
            await TestDeploymentTransactions(temporaryRoot, deploymentPackages).ConfigureAwait(false);
            await TestDeploymentAuthorityFailures(temporaryRoot, deploymentPackages).ConfigureAwait(false);
            await TestDeploymentFaultMatrix(temporaryRoot, deploymentPackages).ConfigureAwait(false);
            await TestAtomicLocalInstallOwnership(temporaryRoot, deploymentPackages).ConfigureAwait(false);
            await TestSourceModesAndTreeDigest(temporaryRoot, deploymentPackages).ConfigureAwait(false);
            await TestDoctorAndExplicitSession(temporaryRoot, deploymentPackages).ConfigureAwait(false);
            string selfContainedExe = Environment.GetEnvironmentVariable("DTMAPI_AUTHOR_SELF_CONTAINED_EXE") ?? string.Empty;
            if (selfContainedExe.Length > 0)
                await TestSelfContainedOfflineHost(temporaryRoot, selfContainedExe).ConfigureAwait(false);
            await TestCompatibilityTamper(temporaryRoot, compatibility).ConfigureAwait(false);
            Console.WriteLine(
                advancedReferenceFixtureExecuted
                    ? "DTMAPI Author SDK tests: OK; advancedReferenceFixture=executed"
                    : "DTMAPI Author SDK tests: OK (Advanced exact-reference fixture skipped; not release acceptance)");
            testSession.MarkSucceeded();
            return 0;
        }
        catch (Exception exception)
        {
            testSession.MarkFailed(exception);
            throw;
        }
        finally
        {
            SetAuthorFaultMutation(null);
            Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_FAULT", null);
            Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT", previousStateRoot);
        }
    }

    private static bool ParseArguments(string[] args)
    {
        bool requireExactAdvancedReference = false;
        foreach (string argument in args)
        {
            if (string.Equals(argument, "--require-exact-advanced-reference", StringComparison.Ordinal))
            {
                requireExactAdvancedReference = true;
                continue;
            }

            throw new ArgumentException("Unknown DTMAPI Author SDK test argument: " + argument);
        }

        return requireExactAdvancedReference;
    }

    private static void TestPublishedDeploymentJournalContract(string repository)
    {
        string schemaPath = Path.Combine(repository, "author-sdk", "schemas", "deployment-journal.schema.json");
        using JsonDocument schema = JsonDocument.Parse(File.ReadAllText(schemaPath, Encoding.UTF8));
        JsonElement root = schema.RootElement;
        Equal(
            AuthorSdkContract.DeploymentJournalSchemaVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
            root.GetProperty("properties").GetProperty("schemaVersion").GetProperty("const").GetInt32()
                .ToString(System.Globalization.CultureInfo.InvariantCulture),
            "Published deployment-journal schema matches the compiled current writer");
        True(
            root.GetProperty("required").EnumerateArray().Any(value => value.GetString() == "localInstall"),
            "Published deployment-journal schema requires the current nullable localInstall field");
        string[] expectedJournalFields = typeof(DeploymentJournal).GetProperties()
            .Select(property => JsonNamingPolicy.CamelCase.ConvertName(property.Name))
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        string[] actualJournalFields = root.GetProperty("required").EnumerateArray()
            .Select(value => value.GetString() ?? string.Empty)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        True(
            expectedJournalFields.SequenceEqual(actualJournalFields, StringComparer.Ordinal),
            "Published deployment-journal required fields match the compiled journal contract");
        string[] expectedLocalInstallFields = typeof(LocalInstallTransaction).GetProperties()
            .Select(property => JsonNamingPolicy.CamelCase.ConvertName(property.Name))
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        string[] actualLocalInstallFields = root.GetProperty("$defs").GetProperty("localInstallTransaction")
            .GetProperty("required").EnumerateArray()
            .Select(value => value.GetString() ?? string.Empty)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        True(
            expectedLocalInstallFields.SequenceEqual(actualLocalInstallFields, StringComparer.Ordinal),
            "Published localInstall required fields match the compiled transaction contract");
        Equal(
            "Prepared",
            root.GetProperty("$defs").GetProperty("localInstallTransaction").GetProperty("properties")
                .GetProperty("phase").GetProperty("const").GetString() ?? string.Empty,
            "Published deployment-journal schema exposes the durable local-install phase");

        string transactionId = new('a', 32);
        string packageSha256 = new('b', 64);
        DeploymentRecord next = new()
        {
            TransactionId = transactionId,
            PackageSha256 = packageSha256,
            PayloadTreeSha256 = new string('c', 64),
            ReceiptSha256 = new string('d', 64),
            Inventory = new DeploymentTreeInventory
            {
                TreeSha256 = new string('e', 64)
            }
        };
        DeploymentJournal prepared = new()
        {
            SchemaVersion = AuthorSdkContract.DeploymentJournalSchemaVersion,
            GameRoot = @"C:\Games\Doloc Town",
            GameRootKey = new string('f', 32),
            UniqueId = "Tests.Deployment",
            PackageKind = "CodeMod",
            CodeModKind = "Strict",
            DestinationPath = @"C:\Games\Doloc Town\Mods\Tests.Deployment",
            Status = "Prepared",
            LocalInstall = new LocalInstallTransaction
            {
                TransactionId = transactionId,
                Phase = "Prepared",
                ExpectedVersion = "1.0.0",
                ExpectedPackageSha256 = packageSha256,
                JournalBeforeExisted = false,
                JournalBeforeBase64 = string.Empty,
                SourceStateBeforeExisted = false,
                SourceStateBeforeBase64 = string.Empty,
                SourceStatePath = @"C:\State\source-state.json",
                StagingPath = @"C:\Games\Doloc Town\Mods\.dtmapi-author\staging\" + transactionId,
                RecoveryPath = string.Empty,
                FailedPath = @"C:\Games\Doloc Town\Mods\.dtmapi-author\failed\Tests.Deployment-" + transactionId,
                Next = next
            }
        };
        using JsonDocument preparedJson = JsonDocument.Parse(JsonSerializer.Serialize(prepared, JsonOptions));
        AssertJsonMatchesSchema(preparedJson.RootElement, schemaPath, "current schema-3 local-install journal");

        prepared.Status = "Absent";
        prepared.LocalInstall = null;
        using JsonDocument terminalJson = JsonDocument.Parse(JsonSerializer.Serialize(prepared, JsonOptions));
        AssertJsonMatchesSchema(terminalJson.RootElement, schemaPath, "current schema-3 terminal journal");

        string readme = File.ReadAllText(Path.Combine(repository, "author-sdk", "README.md"), Encoding.UTF8);
        True(
            readme.Contains("Current journal writes performed by legacy recovery/withdraw use schema 3.", StringComparison.Ordinal),
            "Published README limits current schema-3 journal writes to legacy recovery/withdraw");
        True(
            readme.Contains("Package-local receipts and package markers remain schema 2.", StringComparison.Ordinal),
            "Published README keeps receipt/marker schema 2 distinct from journal schema 3");
        foreach (string pausedCommand in new[] { "dtmapi-author deploy ", "dtmapi-author update " })
        {
            string[] matchingLines = readme.Split('\n')
                .Select(line => line.TrimEnd('\r'))
                .Where(line => line.StartsWith(pausedCommand, StringComparison.Ordinal))
                .ToArray();
            True(
                matchingLines.Length == 1 && matchingLines[0].EndsWith("  # paused: SDK003", StringComparison.Ordinal),
                "Published README labels the paused command at its first command example: " + pausedCommand.Trim());
        }

        string releaseCheck = File.ReadAllText(Path.Combine(repository, "tools", "scripts", "check-author-sdk-release.ps1"), Encoding.UTF8);
        True(
            releaseCheck.Contains("Current journal writes performed by legacy recovery/withdraw use schema $journalVersion.", StringComparison.Ordinal),
            "Published release checker uses the same bounded legacy journal-write statement as the README");
        True(
            !releaseCheck.Contains("New deployment journals and every journal write use schema $journalVersion.", StringComparison.Ordinal),
            "Published release checker does not revive the paused new-deployment journal statement");
    }

    private static void TestPublishedAdvancedReferencePolicyContract(string repository)
    {
        const string oldPolicyId = "doloctown-23762374-moreequipmentslots-v1";
        const string currentPolicyId = "doloctown-24456188-moreequipmentslots-v1";
        const string uniqueId = "DTMAPI.MoreEquipmentSlotsMod";
        const string oldPolicySha256 = "4b796b1e04921a411cef505df1b1cd63e14944a3b3a64f48ebc2e4860806b951";
        const string oldSurfaceSha256 = "bdaee793e089a9689ac441e6ca7099ab01c37cf60f1c275c6e5f01ba7cdf2f7d";
        const string currentPolicySha256 = "21655c30df79470ccc0a9a5222dcc566f2a44e3afbfa24d202180c29d2c6d4a1";
        const string currentSurfaceSha256 = "70d65f0bcb013c16e7232b91d3f232e2663072d6c8c661550519f1e8a806aeb0";

        string policyRoot = Path.Combine(repository, "author-sdk", "advanced-reference-policies");
        string historyRoot = Path.Combine(policyRoot, "history");
        True(!File.Exists(Path.Combine(policyRoot, oldPolicyId + ".json")) &&
             !File.Exists(Path.Combine(policyRoot, oldPolicyId + ".Assembly-CSharp.reference.cs.txt")),
            "Retired MoreEquipmentSlots 23762374 policy is absent from the active SDK set");
        Equal(oldPolicySha256, Sha256(Path.Combine(historyRoot, oldPolicyId + ".json")),
            "Retired MoreEquipmentSlots policy is preserved byte-exact in history");
        Equal(oldSurfaceSha256, Sha256(Path.Combine(historyRoot, oldPolicyId + ".Assembly-CSharp.reference.cs.txt")),
            "Retired MoreEquipmentSlots compiler surface is preserved byte-exact in history");
        Equal(currentPolicySha256, Sha256(Path.Combine(policyRoot, currentPolicyId + ".json")),
            "Current MoreEquipmentSlots policy bytes match the active registry authority");
        Equal(currentSurfaceSha256, Sha256(Path.Combine(policyRoot, currentPolicyId + ".Assembly-CSharp.reference.cs.txt")),
            "Current MoreEquipmentSlots native shield surface bytes match the active registry authority");

        JsonObject activeRegistry = JsonNode.Parse(File.ReadAllText(Path.Combine(policyRoot, "registry.json"), Encoding.UTF8))!.AsObject();
        JsonObject active = activeRegistry["policies"]!.AsArray().Select(node => node!.AsObject()).Single(value =>
            value["requiredUniqueId"]!.GetValue<string>().Equals(uniqueId, StringComparison.Ordinal));
        Equal(currentPolicyId, active["policyId"]!.GetValue<string>(),
            "MoreEquipmentSlots active registry selects only the current policy");
        Equal(currentPolicySha256, active["policySha256"]!.GetValue<string>().ToLowerInvariant(),
            "MoreEquipmentSlots active registry policy hash");
        Equal(currentSurfaceSha256, active["compilerSurfaceSha256"]!.GetValue<string>().ToLowerInvariant(),
            "MoreEquipmentSlots active registry compiler-surface hash");
        Equal("0.6.0", active["minimumDtmApiVersion"]!.GetValue<string>(),
            "MoreEquipmentSlots current policy requires the Runtime which embeds it");

        JsonObject historyRegistry = JsonNode.Parse(File.ReadAllText(Path.Combine(historyRoot, "runtime-registry.json"), Encoding.UTF8))!.AsObject();
        JsonObject historical = historyRegistry["policies"]!.AsArray().Select(node => node!.AsObject()).Single(value =>
            value["policyId"]!.GetValue<string>().Equals(oldPolicyId, StringComparison.Ordinal));
        Equal(uniqueId, historical["requiredUniqueId"]!.GetValue<string>(),
            "Retired MoreEquipmentSlots policy remains identity-bound in history");
        Equal(oldPolicySha256, historical["policySha256"]!.GetValue<string>().ToLowerInvariant(),
            "Retired MoreEquipmentSlots history registry policy hash");
        Equal(oldSurfaceSha256, historical["compilerSurfaceSha256"]!.GetValue<string>().ToLowerInvariant(),
            "Retired MoreEquipmentSlots history registry compiler-surface hash");
        Equal("0.5.5", historical["minimumDtmApiVersion"]!.GetValue<string>(),
            "Retired MoreEquipmentSlots receipt remains accepted by its historical Runtime floor");
    }

    private static async Task TestCodeModRoundTripAndDeterminism(string temp, string compatibility)
    {
        string one = Path.Combine(temp, "code-one");
        string two = Path.Combine(temp, "code-two");
        await ExpectSuccess("new code one", "new", "codemod", one, "--id", "Tests.Code", "--name", "Tests Code", "--author", "Tests").ConfigureAwait(false);
        await ExpectSuccess("new code two", "new", "codemod", two, "--id", "Tests.Code", "--name", "Tests Code", "--author", "Tests").ConfigureAwait(false);
        JsonObject strictManifest = JsonNode.Parse(File.ReadAllText(Path.Combine(one, "manifest.json"), Encoding.UTF8))!.AsObject();
        JsonObject strictAuthor = JsonNode.Parse(File.ReadAllText(Path.Combine(one, "dtmapi.author.json"), Encoding.UTF8))!.AsObject();
        Equal("Strict", strictManifest["CodeModKind"]!.GetValue<string>(), "new schema-2 Strict manifest declares its Runtime identity");
        Equal("Strict", strictAuthor["codeModKind"]!.GetValue<string>(), "new schema-2 Strict author project declares its SDK identity");
        await ExpectSuccess("validate code", "validate", one).ConfigureAwait(false);
        CommandReport build = await ExpectSuccess("build code", "build", one, "--compatibility-root", compatibility).ConfigureAwait(false);
        Equal("netstandard2.0", build.Values["targetFramework"], "CodeMod target framework");
        True(File.Exists(build.OutputPath), "CodeMod output DLL exists");

        string outputOne = Path.Combine(temp, "packages-one");
        string outputTwo = Path.Combine(temp, "packages-two");
        CommandReport packageOne = await ExpectSuccess("pack code one", "pack", one, "--compatibility-root", compatibility, "--output", outputOne).ConfigureAwait(false);
        CommandReport packageTwo = await ExpectSuccess("pack code two", "pack", two, "--compatibility-root", compatibility, "--output", outputTwo).ConfigureAwait(false);
        Equal(packageOne.Sha256, packageTwo.Sha256, "CodeMod package is byte-deterministic across source roots");
        using ZipArchive archive = ZipFile.OpenRead(packageOne.OutputPath);
        string[] entries = archive.Entries.Select(entry => entry.FullName).ToArray();
        Contains(entries, "info.json", "CodeMod info projection");
        Contains(entries, "Content/DTMAPI/manifest.json", "CodeMod packaged manifest");
        Contains(entries, "Content/DTMAPI/Tests.Code.dll", "CodeMod packaged DLL");
        True(entries.All(entry => !entry.Contains("BepInEx", StringComparison.OrdinalIgnoreCase)), "No ordinary Mod is packaged under BepInEx");
        string marker = ReadZipText(archive, "Content/DTMAPI/dtmapi-package.json");
        True(marker.Contains("dtmapi-author-sdk-package-binding", StringComparison.Ordinal), "Package marker binds the SDK-authored manifest/entry identity");
        string classifiedPackage = Path.Combine(temp, "strict-runtime-classification");
        ZipFile.ExtractToDirectory(packageOne.OutputPath, classifiedPackage);
        string packedManifestPath = Path.Combine(classifiedPackage, "Content", "DTMAPI", "manifest.json");
        CoreManifestModel packedManifest = new CoreManifestReader().Read(packedManifestPath);
        CoreManagedModClassification packedClassification = new CoreManagedModClassifier(classifiedPackage).Classify(
            packedManifest,
            classifiedPackage,
            packedManifestPath,
            "Local",
            nativeWorkshopSourceVerified: false,
            workshopId: null);
        True(
            packedClassification.Identity == CoreManagedModIdentity.StrictCodeMod &&
            packedClassification.EffectiveKind == "Strict",
            "new -> validate -> pack -> Core classify must preserve explicit Strict identity and SDK160 closure");

        string forbiddenEntry = Path.Combine(temp, "code-forbidden-entry-identity");
        CopyDirectory(one, forbiddenEntry);
        JsonObject forbiddenEntryManifest = JsonNode.Parse(File.ReadAllText(Path.Combine(forbiddenEntry, "manifest.json"), Encoding.UTF8))!.AsObject();
        forbiddenEntryManifest["EntryDll"] = "HarmonyStrictFixture.dll";
        WriteJson(Path.Combine(forbiddenEntry, "manifest.json"), forbiddenEntryManifest);
        JsonObject forbiddenEntryAuthor = JsonNode.Parse(File.ReadAllText(Path.Combine(forbiddenEntry, "dtmapi.author.json"), Encoding.UTF8))!.AsObject();
        forbiddenEntryAuthor["assemblyName"] = "HarmonyStrictFixture";
        WriteJson(Path.Combine(forbiddenEntry, "dtmapi.author.json"), forbiddenEntryAuthor);
        CommandReport forbiddenStrictEntry = await ExpectFailure("Strict forbidden EntryDll assembly identity", "build", forbiddenEntry, "--compatibility-root", compatibility).ConfigureAwait(false);
        True(forbiddenStrictEntry.Diagnostics.Any(value => value.Message.Contains("bundled-native-runtime-dependency", StringComparison.Ordinal) &&
                                                                     value.Message.Contains("HarmonyStrictFixture", StringComparison.Ordinal)),
            "Strict SDK build rejects a native/runtime-impersonating internal AssemblyName before packaging");
    }

    private static async Task TestContentPackRoundTripAndDeterminism(string temp)
    {
        string one = Path.Combine(temp, "content-one");
        string two = Path.Combine(temp, "content-two");
        await ExpectSuccess("new content one", "new", "contentpack", one, "--id", "Tests.Content", "--name", "Tests Content", "--author", "Tests").ConfigureAwait(false);
        await ExpectSuccess("new content two", "new", "contentpack", two, "--id", "Tests.Content", "--name", "Tests Content", "--author", "Tests").ConfigureAwait(false);
        await ExpectSuccess("validate content", "validate", one).ConfigureAwait(false);
        CommandReport packageOne = await ExpectSuccess("pack content one", "pack", one, "--output", Path.Combine(temp, "content-package-one")).ConfigureAwait(false);
        CommandReport packageTwo = await ExpectSuccess("pack content two", "pack", two, "--output", Path.Combine(temp, "content-package-two")).ConfigureAwait(false);
        Equal(packageOne.Sha256, packageTwo.Sha256, "ContentPack package is byte-deterministic across source roots");
        using ZipArchive archive = ZipFile.OpenRead(packageOne.OutputPath);
        True(archive.Entries.All(entry => !entry.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)), "ContentPack contains no DLL");
    }

    private static async Task<bool> TestAdvancedCodeModReferencePackageAndDeploy(
        string temp,
        string compatibility,
        string repository,
        bool requireExactAdvancedReference)
    {
        CommandReport unadmitted = await ExpectFailure(
            "new unadmitted Advanced identity",
            "new", "codemod", Path.Combine(temp, "advanced-unadmitted"),
            "--id", "Tests.Advanced",
            "--name", "Unadmitted Advanced Fixture",
            "--author", "Tests",
            "--code-mod-kind", "Advanced").ConfigureAwait(false);
        True(unadmitted.Diagnostics.Any(value => value.Message.Contains("not admitted", StringComparison.Ordinal)),
            "Advanced project creation fails closed when UniqueID has no exact policy binding");

        JsonObject authorSchema = JsonNode.Parse(File.ReadAllText(
            Path.Combine(repository, "author-sdk", "schemas", "dtmapi-author.schema.json"),
            Encoding.UTF8))!.AsObject();
        JsonObject referencePolicyIdSchema = authorSchema["$defs"]!["advanced"]!["properties"]!["referencePolicyId"]!.AsObject();
        Equal("string", referencePolicyIdSchema["type"]!.GetValue<string>(), "dtmapi.author schema keeps referencePolicyId structural");
        True(referencePolicyIdSchema["enum"] == null && referencePolicyIdSchema["pattern"] != null,
            "Advanced product admission is registry-driven rather than duplicated as a schema policy-id enum");

        string? advancedReferenceRoot = CreateExactAdvancedReferenceInput(temp, repository);
        string? autoFishingReferenceRoot = CreateExactAutoFishingReferenceInput(temp, repository);
        if (advancedReferenceRoot == null || autoFishingReferenceRoot == null)
        {
            if (requireExactAdvancedReference)
            {
                throw new InvalidOperationException(
                    "Release acceptance requires the exact local 23762374 and 24456188 Advanced reference inputs; " +
                    "set DTMAPI_AUTHOR_ADVANCED_REFERENCE_ROOT for the old exact fixture or restore both accepted local reverse baselines.");
            }
            Console.WriteLine("DTMAPI Author SDK Advanced fixture: SKIP (exact local 23762374/24456188 reference input unavailable)");
            return false;
        }

        string gameRoot = CreateAdvancedGameFixture(temp, advancedReferenceRoot, "advanced-valid", "23762374");
        string autoFishingGameRoot = CreateAdvancedGameFixture(
            temp,
            autoFishingReferenceRoot,
            "autofishing-valid",
            "24456188",
            AutoFishingAssemblyLength,
            AutoFishingAssemblySha256);
        string project = Path.Combine(temp, "advanced-project");
        await ExpectSuccess(
            "new Advanced fixture",
            "new", "codemod", project,
            "--id", "DTMAPI.AdvancedFixture",
            "--name", "Advanced Synthetic Fixture",
            "--author", "Tests",
            "--code-mod-kind", "Advanced").ConfigureAwait(false);

        JsonObject manifest = JsonNode.Parse(File.ReadAllText(Path.Combine(project, "manifest.json"), Encoding.UTF8))!.AsObject();
        Equal("Advanced", manifest["CodeModKind"]!.GetValue<string>(), "Advanced template manifest identity");
        JsonObject author = JsonNode.Parse(File.ReadAllText(Path.Combine(project, "dtmapi.author.json"), Encoding.UTF8))!.AsObject();
        Equal("2", author["schemaVersion"]!.GetValue<int>().ToString(System.Globalization.CultureInfo.InvariantCulture), "Advanced author schema version");
        Equal("0.5.5", author["targetDtmApiVersion"]!.GetValue<string>(), "Advanced explicit DTMAPI API target");
        Equal("doloctown-23762374-g2-v1", author["advanced"]!["referencePolicyId"]!.GetValue<string>(), "Advanced tracked reference policy intent");

        string sourcePath = Path.Combine(project, "src", "ModEntry.cs");
        File.WriteAllText(sourcePath, """
using System;
using DTMAPI.Abstractions;
using HarmonyLib;

namespace DTMAPI.AdvancedFixture;

public sealed class ModEntry : DtmMod
{
    public override void Entry(IDtmHelper helper)
    {
        bool hasLegacyDemoData = DolocAPI.Has087DemoData();
        var harmony = new Harmony("dtmapi.mod.dtmapi.advancedfixture");
        var original = AccessTools.Method(typeof(DolocAPI), nameof(DolocAPI.Has087DemoData))
            ?? throw new InvalidOperationException("Synthetic native target missing.");
        var postfix = AccessTools.Method(typeof(ModEntry), nameof(Observe))
            ?? throw new InvalidOperationException("Synthetic postfix missing.");
        harmony.Patch(original, postfix: new HarmonyMethod(postfix));
        helper.Monitor.Log("Advanced synthetic query=" + hasLegacyDemoData);
    }

    public static void Observe(bool __result)
    {
    }
}
""".Replace("\r\n", "\n", StringComparison.Ordinal), new UTF8Encoding(false));

        await ExpectSuccess("validate Advanced fixture", "validate", project).ConfigureAwait(false);
        CommandReport missingGame = await ExpectFailure("Advanced build requires explicit game root", "build", project, "--compatibility-root", compatibility).ConfigureAwait(false);
        HasCode(missingGame, "SDK202", "Advanced missing game-root failure");
        True(missingGame.Diagnostics.Any(value => value.Message.Contains("--game-root", StringComparison.Ordinal)), "Advanced missing game-root guidance");

        CommandReport build = await ExpectSuccess(
            "build Advanced fixture",
            "build", project,
            "--compatibility-root", compatibility,
            "--game-root", gameRoot).ConfigureAwait(false);
        Equal("Advanced", build.Values["codeModKind"], "Advanced build identity");
        Equal("netstandard2.0", build.Values["targetFramework"], "Advanced build target framework");
        Equal("23762374", build.Values["gameBuildId"], "Advanced build game identity");
        Equal("doloctown-23762374-g2-v1", build.Values["referencePolicyId"], "Advanced build policy identity");
        Equal("2e06db24c3cbbb5e15639fb444bcb4d981f5961402773c0d66a24fafeedb8153", build.Values["referencePolicySha256"], "Advanced build policy hash");
        Equal("aed8b4164e593f667a3a027a2c8adc34390ad3381faf74754582d43df9802dec", build.Values["compilerReferenceSurfaceSha256"], "Advanced G2 compiler surface hash");
        Equal("dtmapi.mod.dtmapi.advancedfixture", build.Values["harmonyOwner"], "Advanced canonical Harmony owner");

        string autoFishingProject = Path.Combine(temp, "advanced-autofishing-policy-project");
        await ExpectSuccess(
            "new AutoFishing Advanced policy fixture",
            "new", "codemod", autoFishingProject,
            "--id", "Yuuka.DTMAPI.AutoFishing",
            "--name", "AutoFishing Policy Fixture",
            "--author", "Tests",
            "--code-mod-kind", "Advanced").ConfigureAwait(false);
        JsonObject autoFishingAuthor = JsonNode.Parse(File.ReadAllText(Path.Combine(autoFishingProject, "dtmapi.author.json"), Encoding.UTF8))!.AsObject();
        Equal("doloctown-24456188-autofishing-v1", autoFishingAuthor["advanced"]!["referencePolicyId"]!.GetValue<string>(),
            "AutoFishing template selects its exact registered policy");
        JsonObject autoFishingManifest = JsonNode.Parse(File.ReadAllText(Path.Combine(autoFishingProject, "manifest.json"), Encoding.UTF8))!.AsObject();
        Equal("0.6.0", autoFishingManifest["MinimumDTMApiVersion"]!.GetValue<string>(),
            "AutoFishing template projects the Runtime floor required by its current policy");
        string autoFishingTooLow = Path.Combine(temp, "advanced-autofishing-too-low-floor");
        CopyDirectory(autoFishingProject, autoFishingTooLow);
        JsonObject autoFishingTooLowManifest = JsonNode.Parse(File.ReadAllText(Path.Combine(autoFishingTooLow, "manifest.json"), Encoding.UTF8))!.AsObject();
        autoFishingTooLowManifest["MinimumDTMApiVersion"] = "0.5.5";
        WriteJson(Path.Combine(autoFishingTooLow, "manifest.json"), autoFishingTooLowManifest);
        HasCode(
            await ExpectFailure("AutoFishing policy rejects understated Runtime floor", "validate", autoFishingTooLow).ConfigureAwait(false),
            "SDK166",
            "Policy-specific Runtime floor gate");
        File.WriteAllText(Path.Combine(autoFishingProject, "src", "ModEntry.cs"), """
using System;
using DTMAPI.Abstractions;
using HarmonyLib;

namespace Yuuka.DTMAPI.AutoFishing;

public sealed class ModEntry : DtmMod
{
    public override void Entry(IDtmHelper helper)
    {
        _ = typeof(DolocAPI).Assembly;
        _ = new Harmony("dtmapi.mod.yuuka.dtmapi.autofishing");
        helper.Monitor.Log("AutoFishing policy/compiler anchor verified.");
    }
}
""".Replace("\r\n", "\n", StringComparison.Ordinal), new UTF8Encoding(false));
        await ExpectSuccess("validate AutoFishing Advanced policy fixture", "validate", autoFishingProject).ConfigureAwait(false);
        CommandReport autoFishingBuild = await ExpectSuccess(
            "build AutoFishing Advanced policy fixture",
            "build", autoFishingProject,
            "--compatibility-root", compatibility,
            "--game-root", autoFishingGameRoot).ConfigureAwait(false);
        Equal("doloctown-24456188-autofishing-v1", autoFishingBuild.Values["referencePolicyId"], "AutoFishing build policy identity");
        Equal("0d9a1ffdb8dab1a5b43f4b88bcae77588eea1c5ca3c5aeddbbf9f013683de628", autoFishingBuild.Values["referencePolicySha256"], "AutoFishing build policy hash");
        Equal("bdaee793e089a9689ac441e6ca7099ab01c37cf60f1c275c6e5f01ba7cdf2f7d", autoFishingBuild.Values["compilerReferenceSurfaceSha256"], "AutoFishing compiler surface hash");
        Equal("dtmapi.mod.yuuka.dtmapi.autofishing", autoFishingBuild.Values["harmonyOwner"], "AutoFishing canonical Harmony owner");
        CommandReport autoFishingPackage = await ExpectSuccess(
            "pack AutoFishing Advanced policy fixture",
            "pack", autoFishingProject,
            "--compatibility-root", compatibility,
            "--game-root", autoFishingGameRoot,
            "--output", Path.Combine(temp, "advanced-autofishing-policy-packages")).ConfigureAwait(false);
        Equal("0.5.5", autoFishingPackage.TargetRuntimeVersion, "AutoFishing package keeps the frozen public API compile target");
        string loweredFloorPackage = CopyPackage(
            autoFishingPackage.OutputPath,
            Path.Combine(temp, "advanced-autofishing-lowered-runtime-floor.zip"));
        MutateAdvancedManifestFloorAndRefreshBindings(loweredFloorPackage, "0.5.5");
        string authorStateRoot = Environment.GetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT")
            ?? throw new InvalidOperationException("Author SDK test state root is unavailable.");
        int autoFishingJournalsBefore = Directory.Exists(authorStateRoot)
            ? Directory.EnumerateFiles(authorStateRoot, "Yuuka.DTMAPI.AutoFishing.journal.json", SearchOption.AllDirectories).Count()
            : 0;
        CommandReport loweredFloorDeploy = await ExpectFailure(
            "deploy AutoFishing package with receipt-bound lowered Runtime floor",
            "deploy", loweredFloorPackage,
            "--game-root", autoFishingGameRoot).ConfigureAwait(false);
        HasCode(loweredFloorDeploy, "SDK404", "Advanced policy-floor deploy rejection");
        int autoFishingJournalsAfter = Directory.Exists(authorStateRoot)
            ? Directory.EnumerateFiles(authorStateRoot, "Yuuka.DTMAPI.AutoFishing.journal.json", SearchOption.AllDirectories).Count()
            : 0;
        True(loweredFloorDeploy.Diagnostics.Any(value =>
                 value.Message.Contains("requires MinimumDTMApiVersion 0.6.0 or later", StringComparison.Ordinal)) &&
             !Directory.Exists(Path.Combine(autoFishingGameRoot, "Mods", "Yuuka.DTMAPI.AutoFishing")) &&
             autoFishingJournalsAfter == autoFishingJournalsBefore,
            "A hand-mutated manifest/receipt/marker trio cannot lower the policy Runtime floor or publish a destination/journal");
        CommandReport autoFishingDeployed = await ExpectSuccess(
            "deploy AutoFishing 0.6 Runtime-floor package",
            "deploy", autoFishingPackage.OutputPath,
            "--game-root", autoFishingGameRoot).ConfigureAwait(false);
        Equal("Advanced", autoFishingDeployed.Values["codeModKind"], "AutoFishing deployment identity");
        JsonObject deployedAutoFishingManifest = JsonNode.Parse(File.ReadAllText(
            Path.Combine(autoFishingDeployed.OutputPath, "Content", "DTMAPI", "manifest.json"),
            Encoding.UTF8))!.AsObject();
        Equal("0.6.0", deployedAutoFishingManifest["MinimumDTMApiVersion"]!.GetValue<string>(),
            "AutoFishing deployment preserves its policy-specific Runtime floor");
        JsonObject deployedAutoFishingMarker = JsonNode.Parse(File.ReadAllText(
            Path.Combine(autoFishingDeployed.OutputPath, "Content", "DTMAPI", "dtmapi-package.json"),
            Encoding.UTF8))!.AsObject();
        Equal("0.5.5", deployedAutoFishingMarker["targetDtmApiVersion"]!.GetValue<string>(),
            "AutoFishing deployment keeps the frozen public API marker target distinct from its Runtime floor");
        await ExpectSuccess(
            "withdraw AutoFishing 0.6 Runtime-floor package",
            "withdraw", "Yuuka.DTMAPI.AutoFishing",
            "--game-root", autoFishingGameRoot).ConfigureAwait(false);
        True(!Directory.Exists(Path.Combine(autoFishingGameRoot, "Mods", "Yuuka.DTMAPI.AutoFishing")),
            "AutoFishing Runtime-floor regression fixture leaves no deployed destination");

        string actionSpeedProject = Path.Combine(temp, "advanced-actionspeed-policy-project");
        await ExpectSuccess(
            "new ActionSpeed Advanced policy fixture",
            "new", "codemod", actionSpeedProject,
            "--id", "Yuuka.DTMAPI.ActionSpeed",
            "--name", "ActionSpeed Policy Fixture",
            "--author", "Tests",
            "--code-mod-kind", "Advanced").ConfigureAwait(false);
        JsonObject actionSpeedAuthor = JsonNode.Parse(File.ReadAllText(Path.Combine(actionSpeedProject, "dtmapi.author.json"), Encoding.UTF8))!.AsObject();
        Equal("doloctown-23762374-actionspeed-v1", actionSpeedAuthor["advanced"]!["referencePolicyId"]!.GetValue<string>(),
            "ActionSpeed template selects its exact registered policy");
        await ExpectSuccess("validate ActionSpeed Advanced policy fixture", "validate", actionSpeedProject).ConfigureAwait(false);

        string oneActionProject = Path.Combine(temp, "advanced-oneaction-policy-project");
        await ExpectSuccess(
            "new OneActionComplete Advanced policy fixture",
            "new", "codemod", oneActionProject,
            "--id", "Yuuka.DTMAPI.OneActionComplete",
            "--name", "OneActionComplete Policy Fixture",
            "--author", "Tests",
            "--code-mod-kind", "Advanced").ConfigureAwait(false);
        JsonObject oneActionAuthor = JsonNode.Parse(File.ReadAllText(Path.Combine(oneActionProject, "dtmapi.author.json"), Encoding.UTF8))!.AsObject();
        Equal("doloctown-23762374-oneactioncomplete-v1", oneActionAuthor["advanced"]!["referencePolicyId"]!.GetValue<string>(),
            "OneActionComplete template selects its exact registered policy");
        await ExpectSuccess("validate OneActionComplete Advanced policy fixture", "validate", oneActionProject).ConfigureAwait(false);

        string autoFishingG2MemberProject = Path.Combine(temp, "advanced-autofishing-g2-member-project");
        CopyDirectory(autoFishingProject, autoFishingG2MemberProject);
        File.WriteAllText(Path.Combine(autoFishingG2MemberProject, "src", "ModEntry.cs"), """
using DTMAPI.Abstractions;

namespace Yuuka.DTMAPI.AutoFishing;

public sealed class ModEntry : DtmMod
{
    public override void Entry(IDtmHelper helper)
    {
        _ = DolocAPI.Has087DemoData();
    }
}
""".Replace("\r\n", "\n", StringComparison.Ordinal), new UTF8Encoding(false));
        CommandReport autoFishingG2Member = await ExpectFailure(
            "AutoFishing policy cannot reuse G2 fixture member",
            "build", autoFishingG2MemberProject,
            "--compatibility-root", compatibility,
            "--game-root", autoFishingGameRoot).ConfigureAwait(false);
        HasCode(autoFishingG2Member, "CS0117", "AutoFishing compiler surface excludes G2 Has087DemoData");

        string moreEquipmentProject = Path.Combine(temp, "advanced-moreequipment-policy-project");
        await ExpectSuccess(
            "new MoreEquipmentSlots Advanced policy fixture",
            "new", "codemod", moreEquipmentProject,
            "--id", "DTMAPI.MoreEquipmentSlotsMod",
            "--name", "MoreEquipmentSlots Policy Fixture",
            "--author", "Tests",
            "--code-mod-kind", "Advanced").ConfigureAwait(false);
        JsonObject moreEquipmentAuthor = JsonNode.Parse(File.ReadAllText(Path.Combine(moreEquipmentProject, "dtmapi.author.json"), Encoding.UTF8))!.AsObject();
        JsonObject moreEquipmentManifest = JsonNode.Parse(File.ReadAllText(Path.Combine(moreEquipmentProject, "manifest.json"), Encoding.UTF8))!.AsObject();
        Equal("doloctown-24456188-moreequipmentslots-v1", moreEquipmentAuthor["advanced"]!["referencePolicyId"]!.GetValue<string>(),
            "MoreEquipmentSlots template selects its exact current policy");
        Equal("0.6.0", moreEquipmentManifest["MinimumDTMApiVersion"]!.GetValue<string>(),
            "MoreEquipmentSlots template projects the current policy Runtime floor");
        File.WriteAllText(Path.Combine(moreEquipmentProject, "src", "ModEntry.cs"), """
using DTMAPI.Abstractions;
using DolocTown;
using HarmonyLib;

namespace DTMAPI.MoreEquipmentSlots;

public sealed class ModEntry : DtmMod
{
    public override void Entry(IDtmHelper helper)
    {
        IAgentEquipmentShieldItem shield = new ShieldFixture();
        _ = shield.ShieldPercent;
        _ = shield.ShieldValue;
        _ = shield.TryBlockAttack(1, out _);
        _ = new Harmony("dtmapi.mod.dtmapi.moreequipmentslotsmod");
        helper.Monitor.Log("MoreEquipmentSlots exact native shield surface verified.");
    }

    private sealed class ShieldFixture : IAgentEquipmentShieldItem
    {
        public float ShieldPercent => 1f;
        public float ShieldValue => 1f;

        public bool TryBlockAttack(int damage, out int blockedDamage)
        {
            blockedDamage = damage;
            return true;
        }
    }
}
""".Replace("\r\n", "\n", StringComparison.Ordinal), new UTF8Encoding(false));
        await ExpectSuccess("validate MoreEquipmentSlots Advanced policy fixture", "validate", moreEquipmentProject).ConfigureAwait(false);
        CommandReport moreEquipmentBuild = await ExpectSuccess(
            "build MoreEquipmentSlots exact native shield surface",
            "build", moreEquipmentProject,
            "--compatibility-root", compatibility,
            "--game-root", autoFishingGameRoot).ConfigureAwait(false);
        Equal("doloctown-24456188-moreequipmentslots-v1", moreEquipmentBuild.Values["referencePolicyId"],
            "MoreEquipmentSlots build policy identity");
        Equal("21655c30df79470ccc0a9a5222dcc566f2a44e3afbfa24d202180c29d2c6d4a1", moreEquipmentBuild.Values["referencePolicySha256"],
            "MoreEquipmentSlots build policy hash");
        Equal("70d65f0bcb013c16e7232b91d3f232e2663072d6c8c661550519f1e8a806aeb0", moreEquipmentBuild.Values["compilerReferenceSurfaceSha256"],
            "MoreEquipmentSlots compiler surface hash");

        string moreEquipmentUntrackedTypeProject = Path.Combine(temp, "advanced-moreequipment-untracked-type-project");
        CopyDirectory(moreEquipmentProject, moreEquipmentUntrackedTypeProject);
        File.WriteAllText(Path.Combine(moreEquipmentUntrackedTypeProject, "src", "ModEntry.cs"), """
using DTMAPI.Abstractions;

namespace DTMAPI.MoreEquipmentSlots;

public sealed class ModEntry : DtmMod
{
    private static DolocTown.GameManager? Forbidden;

    public override void Entry(IDtmHelper helper)
    {
        _ = Forbidden;
    }
}
""".Replace("\r\n", "\n", StringComparison.Ordinal), new UTF8Encoding(false));
        CommandReport moreEquipmentUntrackedType = await ExpectFailure(
            "MoreEquipmentSlots policy rejects an untracked native type",
            "build", moreEquipmentUntrackedTypeProject,
            "--compatibility-root", compatibility,
            "--game-root", autoFishingGameRoot).ConfigureAwait(false);
        True(moreEquipmentUntrackedType.Diagnostics.Any(value => value.Code is "CS0234" or "CS0246"),
            "MoreEquipmentSlots compiler surface exposes no unrelated native type");

        string moreEquipmentUntrackedMemberProject = Path.Combine(temp, "advanced-moreequipment-untracked-member-project");
        CopyDirectory(moreEquipmentProject, moreEquipmentUntrackedMemberProject);
        File.WriteAllText(Path.Combine(moreEquipmentUntrackedMemberProject, "src", "ModEntry.cs"), """
using DTMAPI.Abstractions;
using DolocTown;

namespace DTMAPI.MoreEquipmentSlots;

public sealed class ModEntry : DtmMod
{
    public override void Entry(IDtmHelper helper)
    {
    }

    private static float ReadForbidden(IAgentEquipmentShieldItem shield) => shield.UntrackedValue;
}
""".Replace("\r\n", "\n", StringComparison.Ordinal), new UTF8Encoding(false));
        HasCode(
            await ExpectFailure(
                "MoreEquipmentSlots policy rejects an untracked shield member",
                "build", moreEquipmentUntrackedMemberProject,
                "--compatibility-root", compatibility,
                "--game-root", autoFishingGameRoot).ConfigureAwait(false),
            "CS1061",
            "MoreEquipmentSlots compiler surface exposes only the admitted shield members");

        string autoFishingShieldProject = Path.Combine(temp, "advanced-autofishing-shield-surface-project");
        CopyDirectory(autoFishingProject, autoFishingShieldProject);
        File.WriteAllText(Path.Combine(autoFishingShieldProject, "src", "ModEntry.cs"), """
using DTMAPI.Abstractions;

namespace Yuuka.DTMAPI.AutoFishing;

public sealed class ModEntry : DtmMod
{
    public override void Entry(IDtmHelper helper)
    {
        _ = typeof(DolocTown.IAgentEquipmentShieldItem);
    }
}
""".Replace("\r\n", "\n", StringComparison.Ordinal), new UTF8Encoding(false));
        CommandReport autoFishingShield = await ExpectFailure(
            "AutoFishing policy cannot reuse MoreEquipmentSlots native shield surface",
            "build", autoFishingShieldProject,
            "--compatibility-root", compatibility,
            "--game-root", autoFishingGameRoot).ConfigureAwait(false);
        True(autoFishingShield.Diagnostics.Any(value => value.Code is "CS0234" or "CS0246"),
            "MoreEquipmentSlots native shield interface is not exposed to another Advanced policy");

        string identityMismatchProject = Path.Combine(temp, "advanced-policy-identity-mismatch");
        CopyDirectory(autoFishingProject, identityMismatchProject);
        JsonObject mismatchManifest = JsonNode.Parse(File.ReadAllText(Path.Combine(identityMismatchProject, "manifest.json"), Encoding.UTF8))!.AsObject();
        mismatchManifest["UniqueID"] = "DTMAPI.AdvancedFixture";
        WriteJson(Path.Combine(identityMismatchProject, "manifest.json"), mismatchManifest);
        CommandReport identityMismatch = await ExpectFailure("AutoFishing policy wrong UniqueID", "validate", identityMismatchProject).ConfigureAwait(false);
        HasCode(identityMismatch, "SDK165", "AutoFishing policy UniqueID binding rejection");

        string outOfSurfaceProject = Path.Combine(temp, "advanced-out-of-surface-project");
        await ExpectSuccess(
            "new Advanced out-of-surface fixture",
            "new", "codemod", outOfSurfaceProject,
            "--id", "DTMAPI.AdvancedFixture",
            "--name", "Advanced Out Of Surface",
            "--author", "Tests",
            "--code-mod-kind", "Advanced").ConfigureAwait(false);
        File.WriteAllText(Path.Combine(outOfSurfaceProject, "src", "ModEntry.cs"), """
using DTMAPI.Abstractions;
using HarmonyLib;

namespace DTMAPI.AdvancedFixture;

public sealed class ModEntry : DtmMod
{
    private static readonly Harmony Harmony = new("dtmapi.mod.dtmapi.advancedfixture");
    private static GameManager? ForbiddenGeneralNativeSurface;

    public override void Entry(IDtmHelper helper)
    {
        _ = Harmony;
        _ = DolocAPI.Has087DemoData();
        _ = ForbiddenGeneralNativeSurface;
    }
}
""".Replace("\r\n", "\n", StringComparison.Ordinal), new UTF8Encoding(false));
        CommandReport outOfSurface = await ExpectFailure(
            "Advanced G2 rejects non-fixture native surface",
            "build", outOfSurfaceProject,
            "--compatibility-root", compatibility,
            "--game-root", gameRoot).ConfigureAwait(false);
        HasCode(outOfSurface, "CS0246", "Advanced G2 out-of-surface type rejection");

        string forbiddenAdvancedEntryProject = Path.Combine(temp, "advanced-forbidden-entry-identity");
        CopyDirectory(project, forbiddenAdvancedEntryProject);
        JsonObject forbiddenAdvancedManifest = JsonNode.Parse(File.ReadAllText(Path.Combine(forbiddenAdvancedEntryProject, "manifest.json"), Encoding.UTF8))!.AsObject();
        forbiddenAdvancedManifest["EntryDll"] = "BepInExAdvancedFixture.dll";
        WriteJson(Path.Combine(forbiddenAdvancedEntryProject, "manifest.json"), forbiddenAdvancedManifest);
        JsonObject forbiddenAdvancedAuthor = JsonNode.Parse(File.ReadAllText(Path.Combine(forbiddenAdvancedEntryProject, "dtmapi.author.json"), Encoding.UTF8))!.AsObject();
        forbiddenAdvancedAuthor["assemblyName"] = "BepInExAdvancedFixture";
        WriteJson(Path.Combine(forbiddenAdvancedEntryProject, "dtmapi.author.json"), forbiddenAdvancedAuthor);
        CommandReport forbiddenAdvancedEntry = await ExpectFailure(
            "Advanced forbidden EntryDll assembly identity",
            "build", forbiddenAdvancedEntryProject,
            "--compatibility-root", compatibility,
            "--game-root", gameRoot).ConfigureAwait(false);
        True(forbiddenAdvancedEntry.Diagnostics.Any(value => value.Message.Contains("bundled-native-runtime-dependency", StringComparison.Ordinal) &&
                                                                       value.Message.Contains("BepInExAdvancedFixture", StringComparison.Ordinal)),
            "Advanced SDK build rejects a native/runtime-impersonating internal AssemblyName even when its reference set is otherwise exact");

        string renamedHelper = Path.Combine(project, "content", "assets", "managed-helper.bin");
        Directory.CreateDirectory(Path.GetDirectoryName(renamedHelper)!);
        File.Copy(Path.Combine(compatibility, "DTMAPI.Abstractions.dll"), renamedHelper);
        CommandReport renamedHelperPack = await ExpectFailure(
            "Advanced pack rejects renamed managed helper",
            "pack", project,
            "--compatibility-root", compatibility,
            "--game-root", gameRoot,
            "--output", Path.Combine(temp, "advanced-renamed-helper-packages")).ConfigureAwait(false);
        True(renamedHelperPack.Diagnostics.Any(value => value.Message.Contains("bundled-native-runtime-dependency", StringComparison.Ordinal)),
            "Pack admits exactly one executable EntryDll and rejects a benign managed PE renamed to .bin");
        File.Delete(renamedHelper);

        CommandReport package = await ExpectSuccess(
            "pack Advanced fixture",
            "pack", project,
            "--compatibility-root", compatibility,
            "--game-root", gameRoot,
            "--output", Path.Combine(temp, "advanced-packages")).ConfigureAwait(false);
        Equal("Advanced", package.Values["codeModKind"], "Advanced package identity");
        using (ZipArchive archive = ZipFile.OpenRead(package.OutputPath))
        {
            string[] dllEntries = archive.Entries.Where(entry => entry.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)).Select(entry => entry.FullName).ToArray();
            True(dllEntries.SequenceEqual(new[] { "Content/DTMAPI/DTMAPI.AdvancedFixture.dll" }, StringComparer.Ordinal), "Advanced package contains only its entry DLL");
            True(archive.GetEntry("Content/DTMAPI/dtmapi-advanced-references.json") != null, "Advanced package contains its reference receipt");
            True(archive.Entries.All(entry => !entry.FullName.EndsWith("Assembly-CSharp.dll", StringComparison.OrdinalIgnoreCase)
                                                    && !entry.FullName.EndsWith("0Harmony.dll", StringComparison.OrdinalIgnoreCase)), "Advanced package redistributes no tracked native references");
            AdvancedReferenceReceipt receipt = JsonSerializer.Deserialize<AdvancedReferenceReceipt>(ReadZipText(archive, "Content/DTMAPI/dtmapi-advanced-references.json"), JsonOptions)
                ?? throw new InvalidOperationException("Advanced receipt JSON missing.");
            Equal("DTMAPI.AdvancedCodeMod.ReferenceReceipt", receipt.ReceiptKind, "Advanced receipt kind");
            Equal("DTMAPI.AdvancedFixture", receipt.UniqueId, "Advanced receipt UniqueID");
            Equal("Content/DTMAPI/manifest.json", receipt.ManifestPath, "Advanced receipt manifest path");
            Equal("Content/DTMAPI/DTMAPI.AdvancedFixture.dll", receipt.EntryDllPath, "Advanced receipt entry path");
            Equal("netstandard2.0", receipt.TargetFramework, "Advanced receipt target framework");
            Equal("dtmapi.mod.dtmapi.advancedfixture", receipt.HarmonyOwner, "Advanced receipt Harmony owner");
            True(receipt.EntryDllLength > 0 && receipt.EntryDllSha256.Length == 64 && receipt.ManifestSha256.Length == 64, "Advanced receipt payload hashes/length");
            string[] referenceNames = receipt.References.Select(reference => reference.AssemblyName).ToArray();
            True(referenceNames.SequenceEqual(new[] { "0Harmony", "Assembly-CSharp" }, StringComparer.Ordinal), "Advanced receipt exact reference policy set");
            True(receipt.References.All(reference => !reference.CopyLocal && reference.Length > 0 && reference.Sha256.Length == 64), "Advanced receipt references are hash/length bound and copyLocal=false");
        }

        string renamedPayload = Path.Combine(project, "content", "assets", "official-player-bytes.dat");
        Directory.CreateDirectory(Path.GetDirectoryName(renamedPayload)!);
        File.Copy(Path.Combine(gameRoot, "DolocTown_Data", "Managed", "Assembly-CSharp.dll"), renamedPayload);
        CommandReport renamedPack = await ExpectFailure(
            "Advanced pack rejects renamed native payload",
            "pack", project,
            "--compatibility-root", compatibility,
            "--game-root", gameRoot,
            "--output", Path.Combine(temp, "advanced-renamed-native-packages")).ConfigureAwait(false);
        True(renamedPack.Diagnostics.Any(value => value.Message.Contains("bundled-native-runtime-dependency", StringComparison.Ordinal)),
            "Pack rejects tracked game bytes independently of payload name and extension");
        File.Delete(renamedPayload);

        CommandReport deployed = await ExpectSuccess("deploy Advanced fixture", "deploy", package.OutputPath, "--game-root", gameRoot).ConfigureAwait(false);
        Equal("Advanced", deployed.Values["codeModKind"], "Advanced deployment identity");
        string installedReceipt = Path.Combine(deployed.OutputPath, "Content", "DTMAPI", "dtmapi-advanced-references.json");
        True(File.Exists(installedReceipt), "Advanced receipt survives deploy");
        await ExpectSuccess("Advanced deployment status", "deployment-status", "DTMAPI.AdvancedFixture", "--game-root", gameRoot).ConfigureAwait(false);

        string installedHelper = Path.Combine(deployed.OutputPath, "Content", "DTMAPI", "assets", "managed-helper.dat");
        Directory.CreateDirectory(Path.GetDirectoryName(installedHelper)!);
        string journalPath = deployed.Values["journalPath"];
        string originalReceiptText = File.ReadAllText(Path.Combine(deployed.OutputPath, ".dtmapi-author-receipt.json"), Encoding.UTF8);
        string originalJournalText = File.ReadAllText(journalPath, Encoding.UTF8);
        File.Copy(Path.Combine(compatibility, "DTMAPI.Abstractions.dll"), installedHelper);
        SynchronizeCurrentDeploymentInventory(deployed.OutputPath, journalPath);
        CommandReport installedHelperStatus = await ExpectFailure("Advanced installed status rejects renamed managed helper", "deployment-status", "DTMAPI.AdvancedFixture", "--game-root", gameRoot).ConfigureAwait(false);
        True(installedHelperStatus.Diagnostics.Any(value => value.Message.Contains("bundled-native-runtime-dependency", StringComparison.Ordinal)),
            "Installed-package validation rejects an inventory-authorized managed PE outside the sole EntryDll");
        File.Delete(installedHelper);
        File.WriteAllText(Path.Combine(deployed.OutputPath, ".dtmapi-author-receipt.json"), originalReceiptText, new UTF8Encoding(false));
        File.WriteAllText(journalPath, originalJournalText, new UTF8Encoding(false));

        string receiptTamperPackage = CopyPackage(package.OutputPath, Path.Combine(temp, "advanced-receipt-tamper.zip"));
        MutateZipJson(receiptTamperPackage, "Content/DTMAPI/dtmapi-advanced-references.json", value => value["gameAssemblySha256"] = new string('0', 64));
        string tamperGame = CreateAdvancedGameFixture(temp, advancedReferenceRoot, "advanced-tamper", "23762374");
        CommandReport receiptTamper = await ExpectFailure("Advanced forged receipt", "deploy", receiptTamperPackage, "--game-root", tamperGame).ConfigureAwait(false);
        HasCode(receiptTamper, "SDK404", "Advanced forged receipt rejection");
        True(!Directory.Exists(Path.Combine(tamperGame, "Mods", "DTMAPI.AdvancedFixture")), "Forged Advanced receipt publishes no destination");

        string duplicateRootPackage = CopyPackage(package.OutputPath, Path.Combine(temp, "advanced-duplicate-root-receipt.zip"));
        MutateAdvancedReceiptAndRefreshMarker(duplicateRootPackage, text =>
            ReplaceOnce(text, "  \"schemaVersion\": 1,", "  \"schemaVersion\": 999,\n  \"schemaVersion\": 1,"));
        string duplicateRootGame = CreateAdvancedGameFixture(temp, advancedReferenceRoot, "advanced-duplicate-root", "23762374");
        CommandReport duplicateRoot = await ExpectFailure("Advanced duplicate root receipt field", "deploy", duplicateRootPackage, "--game-root", duplicateRootGame).ConfigureAwait(false);
        HasCode(duplicateRoot, "SDK404", "Advanced duplicate root receipt rejection");
        True(duplicateRoot.Diagnostics.Any(value => value.Message.Contains("duplicate field schemaVersion", StringComparison.Ordinal)) &&
             !Directory.Exists(Path.Combine(duplicateRootGame, "Mods", "DTMAPI.AdvancedFixture")),
            "SDK rejects a marker-hash-synchronized receipt with a duplicate root field before publication");

        string unknownRootPackage = CopyPackage(package.OutputPath, Path.Combine(temp, "advanced-unknown-root-receipt.zip"));
        MutateAdvancedReceiptAndRefreshMarker(unknownRootPackage, text =>
            ReplaceOnce(text, "  \"schemaVersion\": 1,", "  \"unexpected\": true,\n  \"schemaVersion\": 1,"));
        string unknownRootGame = CreateAdvancedGameFixture(temp, advancedReferenceRoot, "advanced-unknown-root", "23762374");
        CommandReport unknownRoot = await ExpectFailure("Advanced unknown root receipt field", "deploy", unknownRootPackage, "--game-root", unknownRootGame).ConfigureAwait(false);
        True(unknownRoot.Diagnostics.Any(value => value.Message.Contains("unknown field unexpected", StringComparison.Ordinal)) &&
             !Directory.Exists(Path.Combine(unknownRootGame, "Mods", "DTMAPI.AdvancedFixture")),
            "SDK rejects a marker-hash-synchronized receipt with an unknown root field");

        string missingRootPackage = CopyPackage(package.OutputPath, Path.Combine(temp, "advanced-missing-root-receipt.zip"));
        MutateAdvancedReceiptAndRefreshMarker(missingRootPackage, text =>
        {
            JsonObject receipt = JsonNode.Parse(text)!.AsObject();
            receipt.Remove("harmonyOwner");
            return JsonSerializer.Serialize(receipt, JsonOptions) + "\n";
        });
        string missingRootGame = CreateAdvancedGameFixture(temp, advancedReferenceRoot, "advanced-missing-root", "23762374");
        CommandReport missingRoot = await ExpectFailure("Advanced missing root receipt field", "deploy", missingRootPackage, "--game-root", missingRootGame).ConfigureAwait(false);
        True(missingRoot.Diagnostics.Any(value => value.Message.Contains("missing required fields: harmonyOwner", StringComparison.Ordinal)) &&
             !Directory.Exists(Path.Combine(missingRootGame, "Mods", "DTMAPI.AdvancedFixture")),
            "SDK rejects a marker-hash-synchronized receipt with a missing root field");

        string duplicateNestedPackage = CopyPackage(package.OutputPath, Path.Combine(temp, "advanced-duplicate-nested-receipt.zip"));
        MutateAdvancedReceiptAndRefreshMarker(duplicateNestedPackage, text =>
            ReplaceOnce(text, "      \"copyLocal\": false", "      \"copyLocal\": true,\n      \"copyLocal\": false"));
        string duplicateNestedGame = CreateAdvancedGameFixture(temp, advancedReferenceRoot, "advanced-duplicate-nested", "23762374");
        CommandReport duplicateNested = await ExpectFailure("Advanced duplicate nested receipt field", "deploy", duplicateNestedPackage, "--game-root", duplicateNestedGame).ConfigureAwait(false);
        HasCode(duplicateNested, "SDK404", "Advanced duplicate nested receipt rejection");
        True(duplicateNested.Diagnostics.Any(value => value.Message.Contains("duplicate field copyLocal", StringComparison.Ordinal)) &&
             !Directory.Exists(Path.Combine(duplicateNestedGame, "Mods", "DTMAPI.AdvancedFixture")),
            "SDK rejects a marker-hash-synchronized receipt with a duplicate reference-row field before publication");

        string unknownNestedPackage = CopyPackage(package.OutputPath, Path.Combine(temp, "advanced-unknown-nested-receipt.zip"));
        MutateAdvancedReceiptAndRefreshMarker(unknownNestedPackage, text =>
            ReplaceOnce(text, "      \"copyLocal\": false", "      \"unexpected\": true,\n      \"copyLocal\": false"));
        string unknownNestedGame = CreateAdvancedGameFixture(temp, advancedReferenceRoot, "advanced-unknown-nested", "23762374");
        CommandReport unknownNested = await ExpectFailure("Advanced unknown nested receipt field", "deploy", unknownNestedPackage, "--game-root", unknownNestedGame).ConfigureAwait(false);
        True(unknownNested.Diagnostics.Any(value => value.Message.Contains("unknown field unexpected", StringComparison.Ordinal)) &&
             !Directory.Exists(Path.Combine(unknownNestedGame, "Mods", "DTMAPI.AdvancedFixture")),
            "SDK rejects a marker-hash-synchronized receipt with an unknown reference-row field");

        string missingNestedPackage = CopyPackage(package.OutputPath, Path.Combine(temp, "advanced-missing-nested-receipt.zip"));
        MutateAdvancedReceiptAndRefreshMarker(missingNestedPackage, text =>
        {
            JsonObject receipt = JsonNode.Parse(text)!.AsObject();
            receipt["references"]!.AsArray()[0]!.AsObject().Remove("copyLocal");
            return JsonSerializer.Serialize(receipt, JsonOptions) + "\n";
        });
        string missingNestedGame = CreateAdvancedGameFixture(temp, advancedReferenceRoot, "advanced-missing-nested", "23762374");
        CommandReport missingNested = await ExpectFailure("Advanced missing nested receipt field", "deploy", missingNestedPackage, "--game-root", missingNestedGame).ConfigureAwait(false);
        True(missingNested.Diagnostics.Any(value => value.Message.Contains("missing required fields: copyLocal", StringComparison.Ordinal)) &&
             !Directory.Exists(Path.Combine(missingNestedGame, "Mods", "DTMAPI.AdvancedFixture")),
            "SDK rejects a marker-hash-synchronized receipt with a missing reference-row field");

        string missingReceiptPackage = CopyPackage(package.OutputPath, Path.Combine(temp, "advanced-missing-receipt.zip"));
        RemoveZipEntry(missingReceiptPackage, "Content/DTMAPI/dtmapi-advanced-references.json");
        string missingReceiptGame = CreateAdvancedGameFixture(temp, advancedReferenceRoot, "advanced-missing-receipt", "23762374");
        await ExpectFailure("Advanced missing receipt", "deploy", missingReceiptPackage, "--game-root", missingReceiptGame).ConfigureAwait(false);

        string bundledNativePackage = CopyPackage(package.OutputPath, Path.Combine(temp, "advanced-bundled-native.zip"));
        AddZipFile(bundledNativePackage, "Content/DTMAPI/assets/payload.bin", Path.Combine(gameRoot, "BepInEx", "core", "0Harmony.dll"));
        string bundledGame = CreateAdvancedGameFixture(temp, advancedReferenceRoot, "advanced-bundled-native", "23762374");
        CommandReport bundledNative = await ExpectFailure("Advanced bundled native dependency", "deploy", bundledNativePackage, "--game-root", bundledGame).ConfigureAwait(false);
        True(bundledNative.Diagnostics.Any(value => value.Message.Contains("bundled-native-runtime-dependency", StringComparison.Ordinal)),
            "Deploy rejects 0Harmony bytes independently of payload name and extension");
        True(!Directory.Exists(Path.Combine(bundledGame, "Mods", "DTMAPI.AdvancedFixture")), "Bundled native dependency publishes no destination");

        string bundledHelperPackage = CopyPackage(package.OutputPath, Path.Combine(temp, "advanced-bundled-helper.zip"));
        AddZipFile(bundledHelperPackage, "Content/DTMAPI/assets/managed-helper.bin", Path.Combine(compatibility, "DTMAPI.Abstractions.dll"));
        string bundledHelperGame = CreateAdvancedGameFixture(temp, advancedReferenceRoot, "advanced-bundled-helper", "23762374");
        CommandReport bundledHelper = await ExpectFailure("Advanced bundled managed helper", "deploy", bundledHelperPackage, "--game-root", bundledHelperGame).ConfigureAwait(false);
        True(bundledHelper.Diagnostics.Any(value => value.Message.Contains("bundled-native-runtime-dependency", StringComparison.Ordinal)) &&
             !Directory.Exists(Path.Combine(bundledHelperGame, "Mods", "DTMAPI.AdvancedFixture")),
            "Deploy rejects a benign managed PE outside the sole EntryDll regardless of filename or extension");

        string wrongBuildGame = CreateAdvancedGameFixture(temp, advancedReferenceRoot, "advanced-wrong-build", "1");
        CommandReport wrongBuild = await ExpectFailure(
            "Advanced wrong Steam build",
            "pack", project,
            "--compatibility-root", compatibility,
            "--game-root", wrongBuildGame,
            "--output", Path.Combine(temp, "advanced-wrong-build-packages")).ConfigureAwait(false);
        True(wrongBuild.Diagnostics.Any(value => value.Message.Contains("Steam build", StringComparison.Ordinal)), "Advanced wrong build is explicit");

        File.AppendAllText(Path.Combine(gameRoot, "BepInEx", "core", "0Harmony.dll"), "drift", Encoding.UTF8);
        await ExpectFailure("Advanced deployment status rejects game-reference drift", "deployment-status", "DTMAPI.AdvancedFixture", "--game-root", gameRoot).ConfigureAwait(false);
        return true;
    }

    private static async Task TestValidationFailures(string temp, string compatibility)
    {
        string traversal = Path.Combine(temp, "invalid-traversal");
        await ExpectSuccess("new traversal fixture", "new", "contentpack", traversal, "--id", "Tests.Traversal", "--name", "Traversal", "--author", "Tests").ConfigureAwait(false);
        JsonObject author = JsonNode.Parse(File.ReadAllText(Path.Combine(traversal, "dtmapi.author.json")))!.AsObject();
        author["contentDirectory"] = "../escape";
        WriteJson(Path.Combine(traversal, "dtmapi.author.json"), author);
        await ExpectFailure("path traversal", "validate", traversal).ConfigureAwait(false);

        string contentDll = Path.Combine(temp, "invalid-content-dll");
        await ExpectSuccess("new DLL fixture", "new", "contentpack", contentDll, "--id", "Tests.Dll", "--name", "DLL", "--author", "Tests").ConfigureAwait(false);
        File.WriteAllBytes(Path.Combine(contentDll, "content", "bad.dll"), new byte[] { 0, 1, 2 });
        CommandReport dllFailure = await ExpectFailure("content DLL", "validate", contentDll).ConfigureAwait(false);
        HasCode(dllFailure, "SDK131", "ContentPack DLL rejection");

        string forbidden = Path.Combine(temp, "invalid-reference");
        await ExpectSuccess("new forbidden fixture", "new", "codemod", forbidden, "--id", "Tests.Forbidden", "--name", "Forbidden", "--author", "Tests").ConfigureAwait(false);
        File.AppendAllText(Path.Combine(forbidden, "src", "ModEntry.cs"), "\n// BepInEx direct reference\n", Encoding.UTF8);
        CommandReport forbiddenFailure = await ExpectFailure("forbidden reference", "validate", forbidden).ConfigureAwait(false);
        HasCode(forbiddenFailure, "SDK160", "Direct BepInEx reference rejection");

        string reservedKind = Path.Combine(temp, "reserved-code-mod-kind");
        await ExpectSuccess("new reserved kind fixture", "new", "codemod", reservedKind, "--id", "Tests.ReservedKind", "--name", "Reserved Kind", "--author", "Tests").ConfigureAwait(false);
        CommandReport validReservedPackage = await ExpectSuccess(
            "pack reserved kind baseline",
            "pack",
            reservedKind,
            "--compatibility-root",
            compatibility,
            "--output",
            Path.Combine(temp, "reserved-kind-baseline-package")).ConfigureAwait(false);
        JsonObject reservedManifest = JsonNode.Parse(File.ReadAllText(Path.Combine(reservedKind, "manifest.json"), Encoding.UTF8))!.AsObject();
        reservedManifest["CodeModKind"] = "Advanced";
        WriteJson(Path.Combine(reservedKind, "manifest.json"), reservedManifest);
        CommandReport reservedValidateFailure = await ExpectFailure("reserved CodeModKind validate", "validate", reservedKind).ConfigureAwait(false);
        HasCode(reservedValidateFailure, "SDK113", "Manifest-only Advanced identity rejection");
        True(reservedValidateFailure.Diagnostics.Any(value => value.Message.Contains("CodeModKind", StringComparison.Ordinal)), "Manifest-only Advanced rejection should name the mismatched field");
        CommandReport reservedPackFailure = await ExpectFailure(
            "reserved CodeModKind pack",
            "pack",
            reservedKind,
            "--compatibility-root",
            compatibility,
            "--output",
            Path.Combine(temp, "reserved-kind-rejected-package")).ConfigureAwait(false);
        HasCode(reservedPackFailure, "SDK113", "Manifest-only Advanced package rejection");

        string hostilePackage = Path.Combine(temp, "reserved-kind-hostile.zip");
        File.Copy(validReservedPackage.OutputPath, hostilePackage);
        AddReservedCodeModKindToPackage(hostilePackage);
        string hostileGame = NewGameRoot(temp, "reserved-kind-deploy");
        CommandReport reservedDeployFailure = await ExpectFailure("reserved CodeModKind deploy", "deploy", hostilePackage, "--game-root", hostileGame).ConfigureAwait(false);
        HasCode(reservedDeployFailure, "SDK404", "Reserved CodeModKind deployment rejection");
        True(reservedDeployFailure.Diagnostics.Any(value => value.Message.Contains("Advanced", StringComparison.Ordinal)), "Manifest-only Advanced deployment should name the missing verified lane");
        True(!Directory.Exists(Path.Combine(hostileGame, "Mods", "Tests.ReservedKind")), "Reserved CodeModKind deployment must not publish a Mod destination");

        string retiredLamp = Path.Combine(temp, "retired-lamp-dto");
        await ExpectSuccess("new retired Lamp fixture", "new", "codemod", retiredLamp, "--id", "Tests.RetiredLamp", "--name", "Retired Lamp", "--author", "Tests").ConfigureAwait(false);
        File.AppendAllText(
            Path.Combine(retiredLamp, "src", "ModEntry.cs"),
            "\n// DTO-only migration scan: LampManualToggleOptions LampManualToggleRegisterResult LampManualToggleState\n",
            Encoding.UTF8);
        CommandReport retiredLampValidation = await ExpectSuccess("retired Lamp DTO warning", "validate", retiredLamp).ConfigureAwait(false);
        HasCode(retiredLampValidation, "SDK170", "Retired Lamp DTO-only migration warning");

        string future = Path.Combine(temp, "runtime-floor-ahead-of-api-target");
        await ExpectSuccess("new future fixture", "new", "contentpack", future, "--id", "Tests.Future", "--name", "Future", "--author", "Tests").ConfigureAwait(false);
        JsonObject manifest = JsonNode.Parse(File.ReadAllText(Path.Combine(future, "manifest.json")))!.AsObject();
        manifest["MinimumDTMApiVersion"] = "0.6.1";
        WriteJson(Path.Combine(future, "manifest.json"), manifest);
        await ExpectSuccess("0.6.1 Runtime floor with 0.5.5 API target", "validate", future).ConfigureAwait(false);
        CommandReport futurePackage = await ExpectSuccess(
            "pack 0.6.1 Runtime floor with 0.5.5 API target",
            "pack", future,
            "--output", Path.Combine(temp, "runtime-floor-package")).ConfigureAwait(false);
        Equal("0.5.5", futurePackage.TargetRuntimeVersion, "0.6.1 Runtime-floor package reports the frozen public API target");
        string futureGame = NewGameRoot(temp, "runtime-floor-deploy");
        CommandReport futureDeployment = await ExpectSuccess(
            "deploy 0.6.1 Runtime floor with 0.5.5 API target",
            "deploy", futurePackage.OutputPath,
            "--game-root", futureGame).ConfigureAwait(false);
        JsonObject deployedFutureManifest = JsonNode.Parse(File.ReadAllText(
            Path.Combine(futureDeployment.OutputPath, "Content", "DTMAPI", "manifest.json"),
            Encoding.UTF8))!.AsObject();
        Equal("0.6.1", deployedFutureManifest["MinimumDTMApiVersion"]!.GetValue<string>(),
            "Deployment accepts and preserves the highest supported Runtime floor");
        await ExpectSuccess(
            "withdraw 0.6.1 Runtime-floor package",
            "withdraw", "Tests.Future",
            "--game-root", futureGame).ConfigureAwait(false);
        manifest["MinimumDTMApiVersion"] = "0.6.2";
        WriteJson(Path.Combine(future, "manifest.json"), manifest);
        HasCode(await ExpectFailure("unsupported future runtime", "validate", future).ConfigureAwait(false), "SDK105", "Highest supported Runtime floor gate");

        string olderMinimum = Path.Combine(temp, "older-minimum-runtime");
        await ExpectSuccess("new older minimum fixture", "new", "contentpack", olderMinimum, "--id", "Tests.OlderMinimum", "--name", "Older Minimum", "--author", "Tests").ConfigureAwait(false);
        JsonObject olderManifest = JsonNode.Parse(File.ReadAllText(Path.Combine(olderMinimum, "manifest.json")))!.AsObject();
        olderManifest["MinimumDTMApiVersion"] = "0.5.4";
        WriteJson(Path.Combine(olderMinimum, "manifest.json"), olderManifest);
        CommandReport olderValidation = await ExpectSuccess("older minimum warning", "validate", olderMinimum).ConfigureAwait(false);
        HasCode(olderValidation, "SDK106", "Older minimum compatibility warning");
        CommandReport olderPackage = await ExpectSuccess("pack older minimum", "pack", olderMinimum, "--output", Path.Combine(temp, "older-minimum-package")).ConfigureAwait(false);
        string olderGame = NewGameRoot(temp, "older-minimum-deploy");
        await ExpectSuccess("deploy older minimum to 0.5.5", "deploy", olderPackage.OutputPath, "--game-root", olderGame).ConfigureAwait(false);

        olderManifest["MinimumDTMApiVersion"] = "0.5.4-alpha";
        WriteJson(Path.Combine(olderMinimum, "manifest.json"), olderManifest);
        HasCode(await ExpectFailure("minimum runtime suffix", "validate", olderMinimum).ConfigureAwait(false), "SDK104", "Minimum Runtime version rejects labels consistently with schema and deployment");
        olderManifest["MinimumDTMApiVersion"] = "0.5";
        WriteJson(Path.Combine(olderMinimum, "manifest.json"), olderManifest);
        HasCode(await ExpectFailure("minimum runtime two parts", "validate", olderMinimum).ConfigureAwait(false), "SDK104", "Minimum Runtime version requires exact major.minor.patch shape");
        olderManifest["MinimumDTMApiVersion"] = new string('9', 80) + ".0.0";
        WriteJson(Path.Combine(olderMinimum, "manifest.json"), olderManifest);
        HasCode(await ExpectFailure("minimum runtime unbounded numeric", "validate", olderMinimum).ConfigureAwait(false), "SDK105", "Minimum Runtime comparison reports an ordinary version gate instead of overflowing");
    }

    private static async Task TestCompatibilityTamper(string temp, string compatibility)
    {
        string project = Path.Combine(temp, "tamper-project");
        await ExpectSuccess("new tamper fixture", "new", "codemod", project, "--id", "Tests.Tamper", "--name", "Tamper", "--author", "Tests").ConfigureAwait(false);
        string props = Path.Combine(compatibility, "DTMAPI.Author.props");
        File.AppendAllText(props, " ", Encoding.UTF8);
        JsonObject compatibilityManifest = JsonNode.Parse(File.ReadAllText(Path.Combine(compatibility, "compatibility.json"), Encoding.UTF8))!.AsObject();
        JsonObject propsEntry = compatibilityManifest["files"]!.AsArray().Select(node => node!.AsObject()).Single(file => file["path"]!.GetValue<string>().Equals("DTMAPI.Author.props", StringComparison.Ordinal));
        propsEntry["sha256"] = Sha256(props);
        WriteJson(Path.Combine(compatibility, "compatibility.json"), compatibilityManifest);
        CommandReport failure = await ExpectFailure("compatibility tamper", "build", project, "--compatibility-root", compatibility).ConfigureAwait(false);
        HasCode(failure, "SDK202", "Embedded compatibility contract rejects jointly replaced manifest and payload");
    }

    private static async Task TestUnsafeCommandsRefuse(string temp)
    {
        string existing = Path.Combine(temp, "existing");
        Directory.CreateDirectory(existing);
        await ExpectFailure("existing destination", "new", "contentpack", existing, "--id", "Tests.Existing", "--name", "Existing", "--author", "Tests").ConfigureAwait(false);
        await ExpectFailure("force option", "new", "contentpack", Path.Combine(temp, "forced"), "--id", "Tests.Force", "--name", "Force", "--author", "Tests", "--force", "true").ConfigureAwait(false);
        await ExpectFailure("upload absent", "upload", temp).ConfigureAwait(false);
        string pluginPath = Path.Combine(temp, "BepInEx", "plugins", "BadMod");
        await ExpectFailure("BepInEx plugin destination", "new", "codemod", pluginPath, "--id", "Tests.Plugin", "--name", "Plugin", "--author", "Tests").ConfigureAwait(false);
    }

    private static async Task TestPausedLegacyGameModsMutations(string temp)
    {
        string game = NewGameRoot(temp, "paused-legacy-game-mods");
        string legacySource = Path.Combine(game, "Mods", "Tests.Paused");
        Directory.CreateDirectory(legacySource);
        File.WriteAllText(Path.Combine(legacySource, "sentinel.txt"), "unchanged", new UTF8Encoding(false));
        string package = Path.Combine(temp, "paused-legacy-package.zip");
        File.WriteAllText(package, "not-read-by-paused-command", new UTF8Encoding(false));
        string stateRoot = Environment.GetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT")
            ?? throw new InvalidOperationException("Paused-command test requires the isolated Author state root.");
        Directory.CreateDirectory(stateRoot);
        string gameTreeBefore = CreateDeploymentInventory(game).TreeSha256;
        string stateTreeBefore = CreateDeploymentInventory(stateRoot).TreeSha256;
        string packageSha256 = new('a', 64);

        string[][] commands =
        {
            new[] { "deploy", package, "--game-root", game },
            new[] { "update", package, "--game-root", game },
            new[]
            {
                "install-local", package,
                "--game-root", game,
                "--expected-unique-id", "Tests.Paused",
                "--expected-version", "1.0.0",
                "--expected-package-sha256", packageSha256
            },
            new[] { "source", "local", "select", "Tests.Paused", legacySource, "--game-root", game }
        };

        foreach (string[] command in commands)
        {
            CommandReport report = await ExpectPublicFailure(
                "paused public " + string.Join(' ', command.Take(3)),
                command).ConfigureAwait(false);
            HasCode(report, "SDK003", "Paused legacy game/Mods mutation");
            Equal(gameTreeBefore, CreateDeploymentInventory(game).TreeSha256, "Paused command preserves the exact game tree");
            Equal(stateTreeBefore, CreateDeploymentInventory(stateRoot).TreeSha256, "Paused command preserves the exact Author state tree");
        }
    }

    private static async Task TestSelfContainedOfflineHost(string temp, string executable)
    {
        if (!File.Exists(executable))
            throw new InvalidOperationException("Self-contained test executable is missing: " + executable);
        string isolated = Path.Combine(temp, "empty-host");
        string project = Path.Combine(isolated, "project");
        string doctorRoot = Path.Combine(isolated, "doctor-empty");
        Directory.CreateDirectory(Path.Combine(isolated, "empty-nuget"));
        Directory.CreateDirectory(doctorRoot);
        string executableRoot = Path.GetDirectoryName(Path.GetFullPath(executable))!;
        True(File.Exists(Path.Combine(executableRoot, "DTMAPI.InstallDoctor.dll")) && File.Exists(Path.Combine(executableRoot, "DTMAPI.Tooling.Metadata.dll")), "Self-contained CLI carries Doctor support libraries");
        True(!File.Exists(Path.Combine(executableRoot, "dtmapi-doctor.exe")) && !File.Exists(Path.Combine(executableRoot, "dtmapi-doctor.runtimeconfig.json")), "Self-contained SDK has one CLI and no Doctor apphost");
        await RunExternal(executable, isolated, "doctor", doctorRoot, "--json").ConfigureAwait(false);
        await RunExternal(executable, isolated, "new", "codemod", project, "--id", "Tests.Offline", "--name", "Offline", "--author", "Tests", "--json").ConfigureAwait(false);
        await RunExternal(executable, isolated, "build", project, "--compatibility-root", Path.Combine(temp, "compatibility", "0.5.5"), "--json").ConfigureAwait(false);
        True(File.Exists(Path.Combine(project, "bin", "dtmapi-author", "Tests.Offline.dll")), "Self-contained empty-PATH CodeMod build output");
    }

    private static async Task<DeploymentPackages> CreateDeploymentPackages(string temp, string compatibility)
    {
        string root = Path.Combine(temp, "deployment-packages");
        string contentOne = Path.Combine(root, "content-v1");
        string contentTwo = Path.Combine(root, "content-v2");
        string codeTwo = Path.Combine(root, "code-v2");
        string other = Path.Combine(root, "other");
        await ExpectSuccess("new deploy content v1", "new", "contentpack", contentOne, "--id", "Tests.Deployment", "--name", "Deployment", "--author", "Tests", "--version", "1.0.0").ConfigureAwait(false);
        await ExpectSuccess("new deploy content v2", "new", "contentpack", contentTwo, "--id", "Tests.Deployment", "--name", "Deployment", "--author", "Tests", "--version", "1.1.0").ConfigureAwait(false);
        await ExpectSuccess("new deploy code v2", "new", "codemod", codeTwo, "--id", "Tests.Deployment", "--name", "Deployment", "--author", "Tests", "--version", "2.0.0").ConfigureAwait(false);
        await ExpectSuccess("new other package", "new", "contentpack", other, "--id", "Tests.Other", "--name", "Other", "--author", "Tests", "--version", "1.0.0").ConfigureAwait(false);
        CommandReport contentOnePack = await ExpectSuccess("pack deploy content v1", "pack", contentOne, "--output", Path.Combine(root, "dist-v1")).ConfigureAwait(false);
        CommandReport contentTwoPack = await ExpectSuccess("pack deploy content v2", "pack", contentTwo, "--output", Path.Combine(root, "dist-v2")).ConfigureAwait(false);
        CommandReport codeTwoPack = await ExpectSuccess("pack deploy code v2", "pack", codeTwo, "--compatibility-root", compatibility, "--output", Path.Combine(root, "dist-code")).ConfigureAwait(false);
        CommandReport otherPack = await ExpectSuccess("pack other", "pack", other, "--output", Path.Combine(root, "dist-other")).ConfigureAwait(false);
        return new DeploymentPackages(contentOnePack.OutputPath, contentTwoPack.OutputPath, codeTwoPack.OutputPath, otherPack.OutputPath);
    }

    private static async Task TestDeploymentTransactions(string temp, DeploymentPackages packages)
    {
        string game = NewGameRoot(temp, "deployment-happy");
        CommandReport deployed = await ExpectSuccess("deploy new", "deploy", packages.ContentV1, "--game-root", game).ConfigureAwait(false);
        string destination = Path.Combine(game, "Mods", "Tests.Deployment");
        True(PathsEqual(deployed.OutputPath, destination), "Deploy destination is exactly game/Mods/UniqueID");
        True(File.Exists(Path.Combine(destination, ".dtmapi-author-receipt.json")), "Package-local receipt exists");
        True(File.Exists(deployed.Values["journalPath"]), "Package-external journal exists");
        True(!Directory.EnumerateFiles(game, "mod_infos.json", SearchOption.AllDirectories).Any(), "Deploy did not write mod_infos.json");
        True(!deployed.OutputPath.Contains("BepInEx", StringComparison.OrdinalIgnoreCase), "Deploy did not target BepInEx/plugins");
        Equal("1.0.0", InstalledVersion(game, "Tests.Deployment"), "Initial deployed version");

        CommandReport updated = await ExpectSuccess("update exact", "update", packages.ContentV2, "--game-root", game).ConfigureAwait(false);
        Equal("1.1.0", InstalledVersion(game, "Tests.Deployment"), "Updated version");
        True(Directory.Exists(updated.Values["recoveryPath"]), "Update retains exact previous tree in same-volume recovery");
        CommandReport withdrawn = await ExpectSuccess("withdraw exact", "withdraw", "Tests.Deployment", "--game-root", game).ConfigureAwait(false);
        True(!Directory.Exists(destination), "Withdraw removes published destination only after exact verification");
        True(Directory.Exists(withdrawn.Values["recoveryPath"]), "Withdraw retains exact recovery tree");
        CommandReport withdrawnStatus = await ExpectSuccess("withdraw status", "deployment-status", "Tests.Deployment", "--game-root", game).ConfigureAwait(false);
        Equal("Withdrawn", withdrawnStatus.Values["status"], "Withdraw journal status");
        await ExpectSuccess("redeploy after receipt withdrawal", "deploy", packages.ContentV1, "--game-root", game).ConfigureAwait(false);

        string codeGame = NewGameRoot(temp, "deployment-code");
        await ExpectSuccess("deploy CodeMod", "deploy", packages.CodeV2, "--game-root", codeGame).ConfigureAwait(false);
        True(File.Exists(Path.Combine(codeGame, "Mods", "Tests.Deployment", "Content", "DTMAPI", "Tests.Deployment.dll")), "CodeMod deploy stays under game/Mods");

        string gameA = NewGameRoot(temp, "deployment-multiroot-a");
        string gameB = NewGameRoot(temp, "deployment-multiroot-b");
        CommandReport rootA = await ExpectSuccess("deploy root A", "deploy", packages.ContentV1, "--game-root", gameA).ConfigureAwait(false);
        CommandReport rootB = await ExpectSuccess("deploy root B", "deploy", packages.ContentV1, "--game-root", gameB).ConfigureAwait(false);
        True(!PathsEqual(rootA.Values["journalPath"], rootB.Values["journalPath"]), "Different game roots have isolated journals");
        await ExpectSuccess("update root A only", "update", packages.ContentV2, "--game-root", gameA).ConfigureAwait(false);
        Equal("1.1.0", InstalledVersion(gameA, "Tests.Deployment"), "Root A updated");
        Equal("1.0.0", InstalledVersion(gameB, "Tests.Deployment"), "Root B remained isolated");

        string installationRoot = Directory.GetParent(Directory.GetParent(rootB.Values["journalPath"])!.FullName)!.FullName;
        string lockPath = Path.Combine(installationRoot, "operation.lock");
        using (var lockStream = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            await ExpectFailure("same-root concurrent lock", "deployment-status", "Tests.Deployment", "--game-root", gameB).ConfigureAwait(false);
        await ExpectSuccess("other root remains usable after lock", "deployment-status", "Tests.Deployment", "--game-root", gameA).ConfigureAwait(false);
    }

    private static async Task TestLegacyDeploymentSchemaUpgrade(string temp, DeploymentPackages packages)
    {
        string contentUpdateGame = NewGameRoot(temp, "legacy-schema1-content-update");
        CommandReport contentDeploy = await ExpectSuccess("legacy content seed", "deploy", packages.ContentV1, "--game-root", contentUpdateGame).ConfigureAwait(false);
        LegacyDeploymentState legacyContent = ConvertInstalledDeploymentToLegacy(contentUpdateGame, contentDeploy.Values["journalPath"], "Tests.Deployment", "ContentPack");
        CommandReport contentStatus = await ExpectSuccess("legacy ContentPack status", "deployment-status", "Tests.Deployment", "--game-root", contentUpdateGame).ConfigureAwait(false);
        Equal("Installed", contentStatus.Values["status"], "Legacy ContentPack status");
        Equal("1", ReadSchemaVersion(legacyContent.JournalPath), "Status does not rewrite legacy ContentPack journal");
        await ExpectSuccess("legacy ContentPack update", "update", packages.ContentV2, "--game-root", contentUpdateGame).ConfigureAwait(false);
        AssertCurrentDeploymentState(contentUpdateGame, legacyContent.JournalPath, "Tests.Deployment", string.Empty);

        string contentWithdrawGame = NewGameRoot(temp, "legacy-schema1-content-withdraw");
        CommandReport contentWithdrawSeed = await ExpectSuccess("legacy content withdraw seed", "deploy", packages.ContentV1, "--game-root", contentWithdrawGame).ConfigureAwait(false);
        LegacyDeploymentState legacyWithdraw = ConvertInstalledDeploymentToLegacy(contentWithdrawGame, contentWithdrawSeed.Values["journalPath"], "Tests.Deployment", "ContentPack");
        await ExpectSuccess("legacy ContentPack withdraw", "withdraw", "Tests.Deployment", "--game-root", contentWithdrawGame).ConfigureAwait(false);
        Equal("3", ReadSchemaVersion(legacyWithdraw.JournalPath), "Legacy ContentPack withdraw upgrades journal to schema 3");
        True(!Directory.Exists(legacyWithdraw.DestinationPath), "Legacy ContentPack withdraw moves only the verified package tree");

        string strictUpdateGame = NewGameRoot(temp, "legacy-schema1-strict-update");
        CommandReport strictDeploy = await ExpectSuccess("legacy Strict seed", "deploy", packages.CodeV2, "--game-root", strictUpdateGame).ConfigureAwait(false);
        LegacyDeploymentState legacyStrict = ConvertInstalledDeploymentToLegacy(strictUpdateGame, strictDeploy.Values["journalPath"], "Tests.Deployment", "CodeMod");
        await ExpectSuccess("legacy Strict status", "deployment-status", "Tests.Deployment", "--game-root", strictUpdateGame).ConfigureAwait(false);
        Equal("1", ReadSchemaVersion(legacyStrict.JournalPath), "Status does not rewrite legacy Strict journal");
        await ExpectSuccess("legacy Strict update", "update", packages.CodeV2, "--game-root", strictUpdateGame).ConfigureAwait(false);
        AssertCurrentDeploymentState(strictUpdateGame, legacyStrict.JournalPath, "Tests.Deployment", "Strict");

        string strictRecoverGame = NewGameRoot(temp, "legacy-schema1-strict-recover");
        CommandReport strictRecoverSeed = await ExpectSuccess("legacy Strict recover seed", "deploy", packages.CodeV2, "--game-root", strictRecoverGame).ConfigureAwait(false);
        LegacyDeploymentState legacyRecover = ConvertInstalledDeploymentToLegacy(strictRecoverGame, strictRecoverSeed.Values["journalPath"], "Tests.Deployment", "CodeMod");
        PrepareLegacyUpdateForRecovery(legacyRecover);
        CommandReport recovered = await ExpectSuccess("legacy Strict prepared recover", "recover", "Tests.Deployment", "--game-root", strictRecoverGame).ConfigureAwait(false);
        Equal("true", recovered.Values["recovered"], "Legacy Strict prepared transaction recovered");
        Equal("3", ReadSchemaVersion(legacyRecover.JournalPath), "Legacy recover writes schema-3 journal");
        Equal("1", ReadSchemaVersion(Path.Combine(legacyRecover.DestinationPath, ".dtmapi-author-receipt.json")), "Recovered legacy package retains its exact schema-1 receipt");
        await ExpectSuccess("recovered legacy Strict status", "deployment-status", "Tests.Deployment", "--game-root", strictRecoverGame).ConfigureAwait(false);

        string mixedSchemaGame = NewGameRoot(temp, "legacy-schema1-journal-current-package-mix");
        CommandReport mixedSchemaDeploy = await ExpectSuccess("mixed schema seed", "deploy", packages.ContentV1, "--game-root", mixedSchemaGame).ConfigureAwait(false);
        string mixedJournalPath = mixedSchemaDeploy.Values["journalPath"];
        JsonObject mixedJournal = JsonNode.Parse(File.ReadAllText(mixedJournalPath, Encoding.UTF8))!.AsObject();
        mixedJournal["schemaVersion"] = 1;
        mixedJournal.Remove("codeModKind");
        WriteJson(mixedJournalPath, mixedJournal);
        await ExpectFailure("schema-1 journal cannot authorize schema-2 package", "deployment-status", "Tests.Deployment", "--game-root", mixedSchemaGame).ConfigureAwait(false);

        string advancedForgeryGame = NewGameRoot(temp, "legacy-schema1-never-advanced");
        CommandReport advancedForgerySeed = await ExpectSuccess("legacy Advanced forgery seed", "deploy", packages.CodeV2, "--game-root", advancedForgeryGame).ConfigureAwait(false);
        LegacyDeploymentState advancedForgery = ConvertInstalledDeploymentToLegacy(advancedForgeryGame, advancedForgerySeed.Values["journalPath"], "Tests.Deployment", "CodeMod");
        string manifestPath = Path.Combine(advancedForgery.DestinationPath, "Content", "DTMAPI", "manifest.json");
        MutateJson(manifestPath, manifest => manifest["CodeModKind"] = "Advanced");
        RefreshLegacyReceiptAndJournal(advancedForgery);
        CommandReport rejected = await ExpectFailure("schema-1 cannot become Advanced", "deployment-status", "Tests.Deployment", "--game-root", advancedForgeryGame).ConfigureAwait(false);
        True(rejected.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("never Advanced", StringComparison.Ordinal)), "Schema-1 Advanced rejection names the compatibility boundary");

        string identityDriftGame = NewGameRoot(temp, "legacy-schema1-identity-drift");
        CommandReport identityDriftSeed = await ExpectSuccess("legacy identity drift seed", "deploy", packages.ContentV1, "--game-root", identityDriftGame).ConfigureAwait(false);
        LegacyDeploymentState identityDrift = ConvertInstalledDeploymentToLegacy(identityDriftGame, identityDriftSeed.Values["journalPath"], "Tests.Deployment", "ContentPack");
        MutateLegacyReceiptAndRefreshRecord(identityDrift, receipt => receipt["uniqueId"] = "Tests.Other");
        await ExpectFailure("legacy receipt identity drift", "deployment-status", "Tests.Deployment", "--game-root", identityDriftGame).ConfigureAwait(false);

        string receiptPathDriftGame = NewGameRoot(temp, "legacy-schema1-receipt-path-drift");
        CommandReport receiptPathDriftSeed = await ExpectSuccess("legacy receipt path drift seed", "deploy", packages.ContentV1, "--game-root", receiptPathDriftGame).ConfigureAwait(false);
        LegacyDeploymentState receiptPathDrift = ConvertInstalledDeploymentToLegacy(receiptPathDriftGame, receiptPathDriftSeed.Values["journalPath"], "Tests.Deployment", "ContentPack");
        MutateLegacyReceiptAndRefreshRecord(receiptPathDrift, receipt => receipt["destinationRelativePath"] = "Mods/Tests.Other");
        await ExpectFailure("legacy receipt destination drift", "deployment-status", "Tests.Deployment", "--game-root", receiptPathDriftGame).ConfigureAwait(false);

        string receiptHashDriftGame = NewGameRoot(temp, "legacy-schema1-receipt-hash-drift");
        CommandReport receiptHashDriftSeed = await ExpectSuccess("legacy receipt hash drift seed", "deploy", packages.ContentV1, "--game-root", receiptHashDriftGame).ConfigureAwait(false);
        LegacyDeploymentState receiptHashDrift = ConvertInstalledDeploymentToLegacy(receiptHashDriftGame, receiptHashDriftSeed.Values["journalPath"], "Tests.Deployment", "ContentPack");
        MutateLegacyReceiptAndRefreshRecord(receiptHashDrift, receipt => receipt["payloadTreeSha256"] = new string('0', 64));
        await ExpectFailure("legacy receipt payload hash drift", "deployment-status", "Tests.Deployment", "--game-root", receiptHashDriftGame).ConfigureAwait(false);

        string journalPathDriftGame = NewGameRoot(temp, "legacy-schema1-journal-path-drift");
        CommandReport journalPathDriftSeed = await ExpectSuccess("legacy journal path drift seed", "deploy", packages.ContentV1, "--game-root", journalPathDriftGame).ConfigureAwait(false);
        LegacyDeploymentState journalPathDrift = ConvertInstalledDeploymentToLegacy(journalPathDriftGame, journalPathDriftSeed.Values["journalPath"], "Tests.Deployment", "ContentPack");
        MutateJson(journalPathDrift.JournalPath, journal => journal["destinationPath"] = Path.Combine(journalPathDriftGame, "Mods", "Tests.Other"));
        await ExpectFailure("legacy journal destination drift", "deployment-status", "Tests.Deployment", "--game-root", journalPathDriftGame).ConfigureAwait(false);

        string? installedGameRoot = FindInstalledGameRoot(FindRepository());
        if (installedGameRoot != null)
        {
            string renamedNativeGame = NewGameRoot(temp, "legacy-schema1-renamed-native");
            CommandReport renamedNativeSeed = await ExpectSuccess("legacy renamed native seed", "deploy", packages.ContentV1, "--game-root", renamedNativeGame).ConfigureAwait(false);
            LegacyDeploymentState renamedNative = ConvertInstalledDeploymentToLegacy(renamedNativeGame, renamedNativeSeed.Values["journalPath"], "Tests.Deployment", "ContentPack");
            File.Copy(
                Path.Combine(installedGameRoot, "DolocTown_Data", "Managed", "Assembly-CSharp.dll"),
                Path.Combine(renamedNative.DestinationPath, "Content", "official-player-bytes.bin"));
            RefreshLegacyReceiptAndJournal(renamedNative);
            CommandReport nativeRejected = await ExpectFailure("legacy renamed native payload", "deployment-status", "Tests.Deployment", "--game-root", renamedNativeGame).ConfigureAwait(false);
            True(nativeRejected.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("bundled-native-runtime-dependency", StringComparison.Ordinal)), "Legacy renamed native payload is rejected independently of filename/extension");
        }
        else
        {
            Console.WriteLine("DTMAPI Author SDK legacy renamed-native fixture: SKIP (explicit local Doloc Town game root unavailable)");
        }
    }

    private static async Task TestDeploymentAuthorityFailures(string temp, DeploymentPackages packages)
    {
        await AssertUpdateRefusedAfterMutation(temp, packages, "missing-receipt", (game, destination, journal) => File.Delete(Path.Combine(destination, ".dtmapi-author-receipt.json"))).ConfigureAwait(false);
        await AssertUpdateRefusedAfterMutation(temp, packages, "forged-receipt", (game, destination, journal) => MutateJson(Path.Combine(destination, ".dtmapi-author-receipt.json"), json => json["uniqueId"] = "Tests.Forged")).ConfigureAwait(false);
        await AssertUpdateRefusedAfterMutation(temp, packages, "tree-drift", (game, destination, journal) => File.AppendAllText(Path.Combine(destination, "info.json"), " ", Encoding.UTF8)).ConfigureAwait(false);
        await AssertUpdateRefusedAfterMutation(temp, packages, "unknown-addition", (game, destination, journal) => File.WriteAllText(Path.Combine(destination, "unknown.txt"), "unknown", Encoding.UTF8)).ConfigureAwait(false);
        await AssertUpdateRefusedAfterMutation(temp, packages, "wrong-root", (game, destination, journal) => MutateJson(journal, json => json["gameRoot"] = Path.Combine(game, "wrong"))).ConfigureAwait(false);
        await AssertUpdateRefusedAfterMutation(temp, packages, "wrong-root-key", (game, destination, journal) => MutateJson(journal, json => json["gameRootKey"] = new string('0', 32))).ConfigureAwait(false);
        await AssertUpdateRefusedAfterMutation(temp, packages, "wrong-path", (game, destination, journal) => MutateJson(journal, json => json["destinationPath"] = Path.Combine(game, "Mods", "Elsewhere"))).ConfigureAwait(false);
        await AssertUpdateRefusedAfterMutation(temp, packages, "wrong-id", (game, destination, journal) => MutateJson(journal, json => json["uniqueId"] = "Tests.Other")).ConfigureAwait(false);
        await AssertUpdateRefusedAfterMutation(temp, packages, "wrong-kind", (game, destination, journal) => MutateJson(journal, json => json["packageKind"] = "CodeMod")).ConfigureAwait(false);

        string kindGame = NewGameRoot(temp, "authority-kind-mismatch");
        await ExpectSuccess("kind fixture deploy", "deploy", packages.ContentV1, "--game-root", kindGame).ConfigureAwait(false);
        await ExpectFailure("package kind mismatch", "update", packages.CodeV2, "--game-root", kindGame).ConfigureAwait(false);
        Equal("1.0.0", InstalledVersion(kindGame, "Tests.Deployment"), "Kind mismatch preserved current tree");

        string receiptSource = NewGameRoot(temp, "authority-receipt-source");
        await ExpectSuccess("receipt-only source deploy", "deploy", packages.ContentV1, "--game-root", receiptSource).ConfigureAwait(false);
        string receiptOnly = NewGameRoot(temp, "authority-receipt-only");
        CopyDirectory(Path.Combine(receiptSource, "Mods", "Tests.Deployment"), Path.Combine(receiptOnly, "Mods", "Tests.Deployment"));
        await ExpectFailure("receipt-only update", "update", packages.ContentV2, "--game-root", receiptOnly).ConfigureAwait(false);
        await ExpectFailure("receipt-only withdraw", "withdraw", "Tests.Deployment", "--game-root", receiptOnly).ConfigureAwait(false);

        string journalOnly = NewGameRoot(temp, "authority-journal-only");
        await ExpectSuccess("journal-only fixture deploy", "deploy", packages.ContentV1, "--game-root", journalOnly).ConfigureAwait(false);
        Directory.Move(Path.Combine(journalOnly, "Mods", "Tests.Deployment"), Path.Combine(journalOnly, "orphaned-package"));
        await ExpectFailure("journal-only update", "update", packages.ContentV2, "--game-root", journalOnly).ConfigureAwait(false);
        await ExpectFailure("journal-only withdraw", "withdraw", "Tests.Deployment", "--game-root", journalOnly).ConfigureAwait(false);

        string unknownDestination = NewGameRoot(temp, "authority-unknown-destination");
        Directory.CreateDirectory(Path.Combine(unknownDestination, "Mods", "Tests.Deployment"));
        File.WriteAllText(Path.Combine(unknownDestination, "Mods", "Tests.Deployment", "owner.txt"), "user", Encoding.UTF8);
        await ExpectFailure("unknown destination deploy", "deploy", packages.ContentV1, "--game-root", unknownDestination).ConfigureAwait(false);
        Equal("user", File.ReadAllText(Path.Combine(unknownDestination, "Mods", "Tests.Deployment", "owner.txt")), "Unknown destination preserved");
    }

    private static async Task AssertUpdateRefusedAfterMutation(string temp, DeploymentPackages packages, string label, Action<string, string, string> mutate)
    {
        string game = NewGameRoot(temp, "authority-" + label);
        CommandReport deploy = await ExpectSuccess(label + " fixture deploy", "deploy", packages.ContentV1, "--game-root", game).ConfigureAwait(false);
        string destination = Path.Combine(game, "Mods", "Tests.Deployment");
        mutate(game, destination, deploy.Values["journalPath"]);
        await ExpectFailure(label + " update refusal", "update", packages.ContentV2, "--game-root", game).ConfigureAwait(false);
    }

    private static async Task TestDeploymentFaultMatrix(string temp, DeploymentPackages packages)
    {
        string[] deployPoints =
        {
            "deploy.after-stage",
            "deploy.prepare.before-journal",
            "deploy.prepare.after-journal",
            "deploy.publish.before-move",
            "deploy.publish.after-move",
            "deploy.publish-state.before-journal",
            "deploy.publish-state.after-journal",
            "deploy.commit.before-journal",
            "deploy.commit.after-journal"
        };
        foreach (string point in deployPoints)
        {
            string game = NewGameRoot(temp, "fault-" + SafeLabel(point));
            await ExpectFaultFailure(point, "deploy", packages.ContentV1, "--game-root", game).ConfigureAwait(false);
            bool committed = point == "deploy.commit.after-journal";
            True(Directory.Exists(Path.Combine(game, "Mods", "Tests.Deployment")) == committed, "Deploy fault state " + point);
            if (committed)
                Equal("1.0.0", InstalledVersion(game, "Tests.Deployment"), "Deploy post-commit version " + point);
        }

        string[] updatePoints =
        {
            "update.after-stage",
            "update.prepare.before-journal",
            "update.prepare.after-journal",
            "update.recovery.before-move",
            "update.recovery.after-move",
            "update.recovery-state.before-journal",
            "update.recovery-state.after-journal",
            "update.publish.before-move",
            "update.publish.after-move",
            "update.publish-state.before-journal",
            "update.publish-state.after-journal",
            "update.commit.before-journal",
            "update.commit.after-journal"
        };
        foreach (string point in updatePoints)
        {
            string game = NewGameRoot(temp, "fault-" + SafeLabel(point));
            await ExpectSuccess(point + " initial deploy", "deploy", packages.ContentV1, "--game-root", game).ConfigureAwait(false);
            await ExpectFaultFailure(point, "update", packages.ContentV2, "--game-root", game).ConfigureAwait(false);
            Equal(point == "update.commit.after-journal" ? "1.1.0" : "1.0.0", InstalledVersion(game, "Tests.Deployment"), "Update fault rollback/commit " + point);
        }

        string[] withdrawPoints =
        {
            "withdraw.prepare.before-journal",
            "withdraw.prepare.after-journal",
            "withdraw.recovery.before-move",
            "withdraw.recovery.after-move",
            "withdraw.recovery-state.before-journal",
            "withdraw.recovery-state.after-journal",
            "withdraw.commit.before-journal",
            "withdraw.commit.after-journal"
        };
        foreach (string point in withdrawPoints)
        {
            string game = NewGameRoot(temp, "fault-" + SafeLabel(point));
            await ExpectSuccess(point + " initial deploy", "deploy", packages.ContentV1, "--game-root", game).ConfigureAwait(false);
            await ExpectFaultFailure(point, "withdraw", "Tests.Deployment", "--game-root", game).ConfigureAwait(false);
            bool committed = point == "withdraw.commit.after-journal";
            True(Directory.Exists(Path.Combine(game, "Mods", "Tests.Deployment")) != committed, "Withdraw fault rollback/commit " + point);
            if (!committed)
                Equal("1.0.0", InstalledVersion(game, "Tests.Deployment"), "Withdraw rollback version " + point);
        }

        await TestPreparedCrashRecovery(temp, packages, "deploy.prepare.after-journal", "deploy", expectedVersionAfterRecovery: string.Empty).ConfigureAwait(false);
        await TestPreparedCrashRecovery(temp, packages, "deploy.publish.after-move", "deploy", expectedVersionAfterRecovery: string.Empty).ConfigureAwait(false);
        await TestPreparedCrashRecovery(temp, packages, "update.recovery.after-move", "update", expectedVersionAfterRecovery: "1.0.0").ConfigureAwait(false);
        await TestPreparedCrashRecovery(temp, packages, "update.publish.after-move", "update", expectedVersionAfterRecovery: "1.0.0").ConfigureAwait(false);
        await TestPreparedCrashRecovery(temp, packages, "withdraw.recovery.after-move", "withdraw", expectedVersionAfterRecovery: "1.0.0").ConfigureAwait(false);

        string recoveryFaultGame = NewGameRoot(temp, "fault-recovery-commit");
        await ExpectSuccess("recovery fault initial", "deploy", packages.ContentV1, "--game-root", recoveryFaultGame).ConfigureAwait(false);
        await ExpectCrashFailure("update.publish.after-move", "update", packages.ContentV2, "--game-root", recoveryFaultGame).ConfigureAwait(false);
        await ExpectFaultFailure("recover.commit.before-journal", "recover", "Tests.Deployment", "--game-root", recoveryFaultGame).ConfigureAwait(false);
        await ExpectSuccess("recovery retry after journal fault", "recover", "Tests.Deployment", "--game-root", recoveryFaultGame).ConfigureAwait(false);
        Equal("1.0.0", InstalledVersion(recoveryFaultGame, "Tests.Deployment"), "Recovery commit retry restored prior tree");

        await TestRecoveryMoveFault(temp, packages, "deploy.publish.after-move", "deploy", "recover.deploy.failed.before-move", string.Empty).ConfigureAwait(false);
        await TestRecoveryMoveFault(temp, packages, "deploy.publish.after-move", "deploy", "recover.deploy.failed.after-move", string.Empty).ConfigureAwait(false);
        await TestRecoveryMoveFault(temp, packages, "update.publish.after-move", "update", "recover.update.failed.before-move", "1.0.0").ConfigureAwait(false);
        await TestRecoveryMoveFault(temp, packages, "update.publish.after-move", "update", "recover.update.failed.after-move", "1.0.0").ConfigureAwait(false);
        await TestRecoveryMoveFault(temp, packages, "update.recovery.after-move", "update", "recover.update.restore.before-move", "1.0.0").ConfigureAwait(false);
        await TestRecoveryMoveFault(temp, packages, "update.recovery.after-move", "update", "recover.update.restore.after-move", "1.0.0").ConfigureAwait(false);
        await TestRecoveryMoveFault(temp, packages, "withdraw.recovery.after-move", "withdraw", "recover.withdraw.restore.before-move", "1.0.0").ConfigureAwait(false);
        await TestRecoveryMoveFault(temp, packages, "withdraw.recovery.after-move", "withdraw", "recover.withdraw.restore.after-move", "1.0.0").ConfigureAwait(false);

        string postCommitFaultGame = NewGameRoot(temp, "fault-recover-commit-after-journal");
        await ExpectSuccess("post-commit recovery fault initial", "deploy", packages.ContentV1, "--game-root", postCommitFaultGame).ConfigureAwait(false);
        await ExpectCrashFailure("update.publish.after-move", "update", packages.ContentV2, "--game-root", postCommitFaultGame).ConfigureAwait(false);
        await ExpectFaultFailure("recover.commit.after-journal", "recover", "Tests.Deployment", "--game-root", postCommitFaultGame).ConfigureAwait(false);
        CommandReport postCommitRetry = await ExpectSuccess("recovery retry after durable journal commit", "recover", "Tests.Deployment", "--game-root", postCommitFaultGame).ConfigureAwait(false);
        Equal("false", postCommitRetry.Values["recovered"], "Post-journal recovery fault is a lost response, not a second transaction");
        Equal("1.0.0", InstalledVersion(postCommitFaultGame, "Tests.Deployment"), "Post-journal recovery fault retained prior exact tree");
    }

    private static async Task TestAtomicLocalInstallOwnership(string temp, DeploymentPackages packages)
    {
        string unsafeGame = NewGameRoot(temp, "atomic-local-install-unsafe-id");
        await ExpectFailure(
            "atomic local unsafe expected identity",
            "install-local",
            packages.ContentV1,
            "--game-root",
            unsafeGame,
            "--expected-unique-id",
            "../Tests.Escape",
            "--expected-version",
            "1.0.0",
            "--expected-package-sha256",
            Sha256(packages.ContentV1)).ConfigureAwait(false);
        True(
            !Directory.EnumerateFileSystemEntries(unsafeGame, "*", SearchOption.AllDirectories).Any(),
            "Unsafe expected identity failed before any game-root write");

        string game = NewGameRoot(temp, "atomic-local-install");
        string v1Sha256 = Sha256(packages.ContentV1);
        string v2Sha256 = Sha256(packages.ContentV2);
        string otherSha256 = Sha256(packages.Other);
        CommandReport otherSeed = await ExpectSuccess(
            "atomic local unrelated source seed",
            "install-local",
            packages.Other,
            "--game-root",
            game,
            "--expected-unique-id",
            "Tests.Other",
            "--expected-version",
            "1.0.0",
            "--expected-package-sha256",
            otherSha256).ConfigureAwait(false);
        Equal("deploy", otherSeed.Values["operation"], "Atomic local unrelated seed operation");
        CommandReport seed = await ExpectSuccess(
            "atomic local seed",
            "install-local",
            packages.ContentV1,
            "--game-root",
            game,
            "--expected-unique-id",
            "Tests.Deployment",
            "--expected-version",
            "1.0.0",
            "--expected-package-sha256",
            v1Sha256).ConfigureAwait(false);
        Equal("deploy", seed.Values["operation"], "Atomic local seed operation");
        Equal("not-required-committed", seed.Values["rollback"], "Atomic local seed commit result");

        string destination = Path.Combine(game, "Mods", "Tests.Deployment");
        string journalPath = seed.Values["journalPath"];
        string sourceStatePath = seed.Values["sourceStatePath"];
        string destinationTree = CreateDeploymentInventory(destination).TreeSha256;
        string journalSha256 = Sha256(journalPath);
        string sourceStateSha256 = Sha256(sourceStatePath);

        CommandReport wrongProduct = await ExpectFailure(
            "atomic local valid wrong product",
            "install-local",
            packages.Other,
            "--game-root",
            game,
            "--expected-unique-id",
            "Tests.Deployment",
            "--expected-version",
            "1.0.0",
            "--expected-package-sha256",
            otherSha256).ConfigureAwait(false);
        Equal("not-required-before-deployment", wrongProduct.Values["rollback"], "Wrong-product deep validation rollback result");
        AssertAtomicLocalPreState(destination, journalPath, sourceStatePath, destinationTree, journalSha256, sourceStateSha256, "wrong product");
        await AssertUnrelatedLocalSourcePreserved(game, otherSha256, "wrong product").ConfigureAwait(false);

        CommandReport changedInput = await ExpectFailure(
            "atomic local changed package after preflight",
            "install-local",
            packages.ContentV2,
            "--game-root",
            game,
            "--expected-unique-id",
            "Tests.Deployment",
            "--expected-version",
            "1.1.0",
            "--expected-package-sha256",
            v1Sha256).ConfigureAwait(false);
        Equal("not-required-before-package-transaction", changedInput.Values["rollback"], "Changed package preflight rollback result");
        AssertAtomicLocalPreState(destination, journalPath, sourceStatePath, destinationTree, journalSha256, sourceStateSha256, "changed package");
        await AssertUnrelatedLocalSourcePreserved(game, otherSha256, "changed package").ConfigureAwait(false);

        foreach (string point in new[]
        {
            "install-local.prepare.after-journal",
            "install-local.after-deployment",
            "install-local.source.before-state",
            "install-local.after-source-selection",
            "update.commit.after-journal"
        })
        {
            CommandReport failed = await ExpectFaultFailure(
                point,
                "install-local",
                packages.ContentV2,
                "--game-root",
                game,
                "--expected-unique-id",
                "Tests.Deployment",
                "--expected-version",
                "1.1.0",
                "--expected-package-sha256",
                v2Sha256).ConfigureAwait(false);
            Equal("restored-exact-pre-state", failed.Values["rollback"], "Atomic local rollback result " + point);
            AssertAtomicLocalPreState(destination, journalPath, sourceStatePath, destinationTree, journalSha256, sourceStateSha256, point);
            await AssertUnrelatedLocalSourcePreserved(game, otherSha256, point).ConfigureAwait(false);
        }

        foreach (string point in new[]
        {
            "install-local.after-prepare",
            "install-local.prepare.after-journal",
            "update.publish.after-move",
            "update.commit.after-journal",
            "install-local.after-deployment",
            "install-local.after-source-selection"
        })
        {
            CommandReport crashed = await ExpectCrashFailure(
                point,
                "install-local",
                packages.ContentV2,
                "--game-root",
                game,
                "--expected-unique-id",
                "Tests.Deployment",
                "--expected-version",
                "1.1.0",
                "--expected-package-sha256",
                v2Sha256).ConfigureAwait(false);
            Equal("recovery-required", crashed.Values["rollback"], "Atomic local durable crash result " + point);
            CommandReport preparedStatus = await ExpectSuccess(
                "atomic local prepared status " + point,
                "deployment-status",
                "Tests.Deployment",
                "--game-root",
                game).ConfigureAwait(false);
            Equal("install-local", preparedStatus.Values["activeOperation"], "Atomic local prepared owner " + point);
            if (point.Equals("install-local.after-deployment", StringComparison.Ordinal))
            {
                await ExpectFaultFailure(
                    "install-local.recover.journal.before-state",
                    "recover",
                    "Tests.Deployment",
                    "--game-root",
                    game).ConfigureAwait(false);
                CommandReport retryableStatus = await ExpectSuccess(
                    "atomic local recovery failure status",
                    "deployment-status",
                    "Tests.Deployment",
                    "--game-root",
                    game).ConfigureAwait(false);
                Equal("install-local", retryableStatus.Values["activeOperation"], "Atomic local recovery failure retained durable owner");
            }
            CommandReport recovered = point.Equals("install-local.after-source-selection", StringComparison.Ordinal)
                ? await ExpectFaultSuccess(
                    "install-local.recover.journal.after-state",
                    "recover",
                    "Tests.Deployment",
                    "--game-root",
                    game).ConfigureAwait(false)
                : await ExpectSuccess(
                    "atomic local durable recover " + point,
                    "recover",
                    "Tests.Deployment",
                    "--game-root",
                    game).ConfigureAwait(false);
            Equal("true", recovered.Values["recovered"], "Atomic local durable recover result " + point);
            AssertAtomicLocalPreState(destination, journalPath, sourceStatePath, destinationTree, journalSha256, sourceStateSha256, "crash " + point);
            await AssertUnrelatedLocalSourcePreserved(game, otherSha256, "crash " + point).ConfigureAwait(false);
        }

        CommandReport updated = await ExpectSuccess(
            "atomic local update",
            "install-local",
            packages.ContentV2,
            "--game-root",
            game,
            "--expected-unique-id",
            "Tests.Deployment",
            "--expected-version",
            "1.1.0",
            "--expected-package-sha256",
            v2Sha256).ConfigureAwait(false);
        Equal("update", updated.Values["operation"], "Atomic local update operation");
        Equal("1.1.0", InstalledVersion(game, "Tests.Deployment"), "Atomic local update version");
        CommandReport source = await ExpectSuccess(
            "atomic local source status",
            "source",
            "status",
            "Tests.Deployment",
            "--game-root",
            game).ConfigureAwait(false);
        Equal("LocalDevelopment", source.Values["mode"], "Atomic local source mode");
        Equal(updated.Values["sourceTreeSha256"], source.Values["expectedTreeSha256"], "Atomic local source selection digest");
        CommandReport reconciled = await ExpectSuccess(
            "atomic local read-only reconciliation",
            "install-local-status",
            "Tests.Deployment",
            "--game-root",
            game,
            "--expected-version",
            "1.1.0",
            "--expected-package-sha256",
            v2Sha256).ConfigureAwait(false);
        Equal("CommittedLocalDevelopment", reconciled.Values["status"], "Atomic local reconciliation status");
        Equal(updated.Values["transactionId"], reconciled.Values["transactionId"], "Atomic local reconciliation transaction");
        await AssertUnrelatedLocalSourcePreserved(game, otherSha256, "committed update").ConfigureAwait(false);
        await TestTerminalLocalInstallCommitReconciliation(temp, packages, crash: false).ConfigureAwait(false);
        await TestTerminalLocalInstallCommitReconciliation(temp, packages, crash: true).ConfigureAwait(false);
        await TestUnknownLocalInstallAuthorityClassification(temp, packages, unreadable: false).ConfigureAwait(false);
        await TestUnknownLocalInstallAuthorityClassification(temp, packages, unreadable: true).ConfigureAwait(false);
        await TestPreMarkerLocalInstallOrphanCleanup(temp, packages).ConfigureAwait(false);
        await TestLocalInstallSnapshotSemanticPreflight(temp, packages).ConfigureAwait(false);
        await TestPendingLocalInstallGameRootBarrier(temp, packages).ConfigureAwait(false);
    }

    private static async Task TestPendingLocalInstallGameRootBarrier(
        string temp,
        DeploymentPackages packages)
    {
        string game = NewGameRoot(temp, "atomic-local-game-root-barrier");
        string v1Sha256 = Sha256(packages.ContentV1);
        string v2Sha256 = Sha256(packages.ContentV2);
        string otherSha256 = Sha256(packages.Other);
        CommandReport otherSeed = await ExpectSuccess(
            "pending barrier unrelated seed",
            "install-local",
            packages.Other,
            "--game-root",
            game,
            "--expected-unique-id",
            "Tests.Other",
            "--expected-version",
            "1.0.0",
            "--expected-package-sha256",
            otherSha256).ConfigureAwait(false);
        CommandReport seed = await ExpectSuccess(
            "pending barrier target seed",
            "install-local",
            packages.ContentV1,
            "--game-root",
            game,
            "--expected-unique-id",
            "Tests.Deployment",
            "--expected-version",
            "1.0.0",
            "--expected-package-sha256",
            v1Sha256).ConfigureAwait(false);
        string sourceStatePath = seed.Values["sourceStatePath"];
        byte[] exactSourceBefore = File.ReadAllBytes(sourceStatePath);

        CommandReport crashed = await ExpectCrashFailure(
            "install-local.after-prepare",
            "install-local",
            packages.ContentV2,
            "--game-root",
            game,
            "--expected-unique-id",
            "Tests.Deployment",
            "--expected-version",
            "1.1.0",
            "--expected-package-sha256",
            v2Sha256).ConfigureAwait(false);
        Equal("recovery-required", crashed.Values["rollback"], "Pending barrier crash classification");

        string journalPath = seed.Values["journalPath"];
        JsonObject preparedJournal =
            JsonNode.Parse(File.ReadAllText(journalPath, Encoding.UTF8))!.AsObject();
        string pendingStage = preparedJournal["localInstall"]!["stagingPath"]!.GetValue<string>();
        string pendingStageTree = CreateDeploymentInventory(pendingStage).TreeSha256;
        string preparedJournalSha256 = Sha256(journalPath);
        True(
            File.ReadAllBytes(sourceStatePath).SequenceEqual(exactSourceBefore),
            "Pending marker publication did not change source state");

        CommandReport sourceStatus = await ExpectSuccess(
            "pending barrier source status",
            "source",
            "status",
            "Tests.Other",
            "--game-root",
            game).ConfigureAwait(false);
        Equal("LocalDevelopment", sourceStatus.Values["mode"], "Source status remains usable while pending");
        CommandReport deploymentStatus = await ExpectSuccess(
            "pending barrier deployment status",
            "deployment-status",
            "Tests.Deployment",
            "--game-root",
            game).ConfigureAwait(false);
        Equal("install-local", deploymentStatus.Values["activeOperation"], "Deployment status exposes pending owner");
        CommandReport localStatus = await ExpectSuccess(
            "pending barrier local-install status",
            "install-local-status",
            "Tests.Deployment",
            "--game-root",
            game,
            "--expected-version",
            "1.1.0",
            "--expected-package-sha256",
            v2Sha256).ConfigureAwait(false);
        Equal("RecoveryRequired", localStatus.Values["status"], "Local-install status remains usable while pending");

        foreach ((string label, string[] arguments) in new[]
        {
            (
                "local",
                new[] { "source", "local", "clear", "Tests.Other", "--game-root", game }),
            (
                "workshop",
                new[] { "source", "workshop", "prepare", "Tests.Later", "--game-root", game }),
            (
                "reproduction",
                new[] { "source", "reproduction", "begin", "--game-root", game })
        })
        {
            CommandReport blocked = await ExpectFailure(
                "pending barrier blocks " + label + " source mutation",
                arguments).ConfigureAwait(false);
            True(
                blocked.Diagnostics.Any(value =>
                    value.Message.Contains("local-install recovery is pending", StringComparison.Ordinal)),
                "Pending barrier reports its game-root owner for " + label);
            True(
                File.ReadAllBytes(sourceStatePath).SequenceEqual(exactSourceBefore),
                "Pending barrier preserves exact source bytes for " + label);
            Equal(
                preparedJournalSha256,
                Sha256(journalPath),
                "Pending barrier preserves exact journal for " + label);
            Equal(
                pendingStageTree,
                CreateDeploymentInventory(pendingStage).TreeSha256,
                "Pending barrier preserves exact stage for " + label);
        }

        await ExpectFailure(
            "pending barrier blocks another install-local",
            "install-local",
            packages.Other,
            "--game-root",
            game,
            "--expected-unique-id",
            "Tests.Other",
            "--expected-version",
            "1.0.0",
            "--expected-package-sha256",
            otherSha256).ConfigureAwait(false);
        True(
            File.ReadAllBytes(sourceStatePath).SequenceEqual(exactSourceBefore),
            "Blocked unrelated install-local preserves exact source bytes");
        Equal(
            preparedJournalSha256,
            Sha256(journalPath),
            "Blocked unrelated install-local preserves pending journal");
        Equal(
            pendingStageTree,
            CreateDeploymentInventory(pendingStage).TreeSha256,
            "Blocked unrelated install-local cannot orphan-clean the pending stage");
        Equal(
            otherSeed.Values["sourceTreeSha256"],
            (await ExpectSuccess(
                "blocked unrelated install source status",
                "source",
                "status",
                "Tests.Other",
                "--game-root",
                game).ConfigureAwait(false)).Values["expectedTreeSha256"],
            "Blocked unrelated install-local preserves unrelated selection");

        CommandReport recovered = await ExpectSuccess(
            "pending barrier exact recovery",
            "recover",
            "Tests.Deployment",
            "--game-root",
            game).ConfigureAwait(false);
        Equal("true", recovered.Values["recovered"], "Pending barrier target remains recoverable");
        True(
            File.ReadAllBytes(sourceStatePath).SequenceEqual(exactSourceBefore),
            "Pending barrier recovery restores exact source pre-state");
        True(!Directory.Exists(pendingStage), "Successful recovery removes its owned pending stage");

        string driftGame = NewGameRoot(temp, "atomic-local-source-drift");
        CommandReport driftSeed = await ExpectSuccess(
            "source drift target seed",
            "install-local",
            packages.ContentV1,
            "--game-root",
            driftGame,
            "--expected-unique-id",
            "Tests.Deployment",
            "--expected-version",
            "1.0.0",
            "--expected-package-sha256",
            v1Sha256).ConfigureAwait(false);
        string driftSourcePath = driftSeed.Values["sourceStatePath"];
        byte[] driftSourceBefore = File.ReadAllBytes(driftSourcePath);
        await ExpectCrashFailure(
            "install-local.after-prepare",
            "install-local",
            packages.ContentV2,
            "--game-root",
            driftGame,
            "--expected-unique-id",
            "Tests.Deployment",
            "--expected-version",
            "1.1.0",
            "--expected-package-sha256",
            v2Sha256).ConfigureAwait(false);

        string driftJournalPath = driftSeed.Values["journalPath"];
        JsonObject driftJournal =
            JsonNode.Parse(File.ReadAllText(driftJournalPath, Encoding.UTF8))!.AsObject();
        string driftStage = driftJournal["localInstall"]!["stagingPath"]!.GetValue<string>();
        string driftDestinationTree =
            CreateDeploymentInventory(Path.Combine(driftGame, "Mods", "Tests.Deployment")).TreeSha256;
        string driftStageTree = CreateDeploymentInventory(driftStage).TreeSha256;
        string driftJournalSha256 = Sha256(driftJournalPath);
        JsonObject sourceState =
            JsonNode.Parse(File.ReadAllText(driftSourcePath, Encoding.UTF8))!.AsObject();
        sourceState["selections"]!.AsArray().Add(new JsonObject
        {
            ["uniqueId"] = "Tests.Later",
            ["mode"] = "WorkshopValidation",
            ["sourcePath"] = string.Empty,
            ["expectedTreeSha256"] = string.Empty
        });
        WriteJson(driftSourcePath, sourceState);
        byte[] externalDrift = File.ReadAllBytes(driftSourcePath);

        CommandReport driftRefused = await ExpectFailure(
            "source drift recovery refuses overwrite",
            "recover",
            "Tests.Deployment",
            "--game-root",
            driftGame).ConfigureAwait(false);
        True(
            driftRefused.Diagnostics.Any(value =>
                value.Message.Contains("matches neither", StringComparison.Ordinal)),
            "Unknown source drift reports the exact recovery boundary");
        True(
            File.ReadAllBytes(driftSourcePath).SequenceEqual(externalDrift),
            "Refused recovery preserves external source drift");
        Equal(
            driftJournalSha256,
            Sha256(driftJournalPath),
            "Refused recovery retains its retryable marker");
        Equal(
            driftDestinationTree,
            CreateDeploymentInventory(Path.Combine(driftGame, "Mods", "Tests.Deployment")).TreeSha256,
            "Refused recovery does not move destination");
        Equal(
            driftStageTree,
            CreateDeploymentInventory(driftStage).TreeSha256,
            "Refused recovery preserves pending stage");

        File.WriteAllBytes(driftSourcePath, driftSourceBefore);
        CommandReport driftRetry = await ExpectSuccess(
            "source drift exact retry",
            "recover",
            "Tests.Deployment",
            "--game-root",
            driftGame).ConfigureAwait(false);
        Equal("true", driftRetry.Values["recovered"], "Exact pre-state permits retry after external drift is resolved");
        True(
            File.ReadAllBytes(driftSourcePath).SequenceEqual(driftSourceBefore),
            "Successful retry preserves exact restored source bytes");
    }

    private static async Task TestPreMarkerLocalInstallOrphanCleanup(
        string temp,
        DeploymentPackages packages)
    {
        string game = NewGameRoot(temp, "atomic-local-pre-marker-orphans");
        string v1Sha256 = Sha256(packages.ContentV1);
        string v2Sha256 = Sha256(packages.ContentV2);
        CommandReport seed = await ExpectSuccess(
            "pre-marker orphan seed",
            "install-local",
            packages.ContentV1,
            "--game-root",
            game,
            "--expected-unique-id",
            "Tests.Deployment",
            "--expected-version",
            "1.0.0",
            "--expected-package-sha256",
            v1Sha256).ConfigureAwait(false);

        string transactionId = new('a', 32);
        string stateRoot = Path.GetDirectoryName(seed.Values["sourceStatePath"])!;
        string inputRoot = Path.Combine(stateRoot, "install-inputs");
        string orphanInput = Path.Combine(inputRoot, transactionId + ".zip");
        string stagingRoot = Path.Combine(game, "Mods", ".dtmapi-author", "staging");
        string orphanStage = Path.Combine(stagingRoot, transactionId);
        string unownedStage = Path.Combine(stagingRoot, "not-a-transaction");
        Directory.CreateDirectory(inputRoot);
        Directory.CreateDirectory(orphanStage);
        Directory.CreateDirectory(unownedStage);
        File.WriteAllText(orphanInput, "pre-marker immutable input", new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(orphanStage, "partial.txt"), "pre-marker stage", new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(unownedStage, "keep.txt"), "not SDK transaction-shaped", new UTF8Encoding(false));

        CommandReport updated = await ExpectSuccess(
            "pre-marker orphan cleanup update",
            "install-local",
            packages.ContentV2,
            "--game-root",
            game,
            "--expected-unique-id",
            "Tests.Deployment",
            "--expected-version",
            "1.1.0",
            "--expected-package-sha256",
            v2Sha256).ConfigureAwait(false);
        Equal("1", updated.Values["orphanInputsCleaned"], "One exact pre-marker input orphan cleaned");
        Equal("1", updated.Values["orphanStagesCleaned"], "One exact pre-marker staging orphan cleaned");
        True(!File.Exists(orphanInput), "Pre-marker input orphan no longer exists");
        True(!Directory.Exists(orphanStage), "Pre-marker staging orphan no longer exists");
        True(Directory.Exists(unownedStage), "Non-transaction-shaped hidden directory remains untouched");
    }

    private static async Task TestLocalInstallSnapshotSemanticPreflight(
        string temp,
        DeploymentPackages packages)
    {
        var mutations = new List<(string Label, Action<JsonObject> Mutate)>
        {
            ("journal-empty", journal =>
                journal["localInstall"]!["journalBeforeBase64"] = string.Empty),
            ("journal-empty-object", journal =>
                journal["localInstall"]!["journalBeforeBase64"] =
                    Convert.ToBase64String(Encoding.UTF8.GetBytes("{}"))),
            ("journal-wrong-root", journal =>
                MutateSnapshotObject(journal, "journalBeforeBase64", snapshot =>
                    snapshot["gameRoot"] = @"C:\Wrong\Doloc Town")),
            ("journal-wrong-product", journal =>
                MutateSnapshotObject(journal, "journalBeforeBase64", snapshot =>
                    snapshot["uniqueId"] = "Tests.Other")),
            ("source-empty", journal =>
                journal["localInstall"]!["sourceStateBeforeBase64"] = string.Empty),
            ("source-empty-object", journal =>
                journal["localInstall"]!["sourceStateBeforeBase64"] =
                    Convert.ToBase64String(Encoding.UTF8.GetBytes("{}"))),
            ("source-wrong-root", journal =>
                MutateSnapshotObject(journal, "sourceStateBeforeBase64", snapshot =>
                    snapshot["gameRoot"] = @"C:\Wrong\Doloc Town")),
            ("source-invalid-selection", journal =>
                MutateSnapshotObject(journal, "sourceStateBeforeBase64", snapshot =>
                    snapshot["selections"]![0]!["mode"] = "Unsupported"))
        };

        foreach ((string label, Action<JsonObject> mutate) in mutations)
        {
            string game = NewGameRoot(temp, "atomic-local-snapshot-" + label);
            string v1Sha256 = Sha256(packages.ContentV1);
            string v2Sha256 = Sha256(packages.ContentV2);
            CommandReport seed = await ExpectSuccess(
                "snapshot preflight seed " + label,
                "install-local",
                packages.ContentV1,
                "--game-root",
                game,
                "--expected-unique-id",
                "Tests.Deployment",
                "--expected-version",
                "1.0.0",
                "--expected-package-sha256",
                v1Sha256).ConfigureAwait(false);
            string journalPath = seed.Values["journalPath"];
            string sourceStatePath = seed.Values["sourceStatePath"];
            string destination = seed.OutputPath;
            string destinationTree = CreateDeploymentInventory(destination).TreeSha256;
            string sourceStateSha256 = Sha256(sourceStatePath);

            CommandReport crashed = await ExpectCrashFailureWithMutation(
                "install-local.after-prepare",
                () =>
                {
                    JsonObject journal = JsonNode.Parse(File.ReadAllText(journalPath, Encoding.UTF8))!.AsObject();
                    mutate(journal);
                    WriteJson(journalPath, journal);
                },
                "install-local",
                packages.ContentV2,
                "--game-root",
                game,
                "--expected-unique-id",
                "Tests.Deployment",
                "--expected-version",
                "1.1.0",
                "--expected-package-sha256",
                v2Sha256).ConfigureAwait(false);
            Equal(
                "authority-unknown-fail-closed",
                crashed.Values["rollback"],
                "Semantic snapshot corruption fails closed " + label);

            string corruptedJournalSha256 = Sha256(journalPath);
            await ExpectFailure(
                "semantic snapshot recover refuses " + label,
                "recover",
                "Tests.Deployment",
                "--game-root",
                game).ConfigureAwait(false);
            Equal(
                corruptedJournalSha256,
                Sha256(journalPath),
                "Semantic snapshot preflight does not rewrite journal " + label);
            Equal(
                sourceStateSha256,
                Sha256(sourceStatePath),
                "Semantic snapshot preflight does not rewrite source state " + label);
            Equal(
                destinationTree,
                CreateDeploymentInventory(destination).TreeSha256,
                "Semantic snapshot preflight does not move destination " + label);
        }
    }

    private static void MutateSnapshotObject(
        JsonObject journal,
        string property,
        Action<JsonObject> mutate)
    {
        JsonNode localInstall = journal["localInstall"]
            ?? throw new InvalidOperationException("Prepared localInstall marker is missing.");
        string encoded = localInstall[property]!.GetValue<string>();
        JsonObject snapshot = JsonNode.Parse(
            Encoding.UTF8.GetString(Convert.FromBase64String(encoded)))!.AsObject();
        mutate(snapshot);
        localInstall[property] = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(snapshot.ToJsonString(JsonOptions)));
    }

    private static async Task TestUnknownLocalInstallAuthorityClassification(
        string temp,
        DeploymentPackages packages,
        bool unreadable)
    {
        string label = unreadable ? "unreadable" : "structurally-invalid";
        string game = NewGameRoot(temp, "atomic-local-authority-" + label);
        string v1Sha256 = Sha256(packages.ContentV1);
        string v2Sha256 = Sha256(packages.ContentV2);
        CommandReport seed = await ExpectSuccess(
            "unknown authority seed " + label,
            "install-local",
            packages.ContentV1,
            "--game-root",
            game,
            "--expected-unique-id",
            "Tests.Deployment",
            "--expected-version",
            "1.0.0",
            "--expected-package-sha256",
            v1Sha256).ConfigureAwait(false);
        string journalPath = seed.Values["journalPath"];

        CommandReport failed = await ExpectCrashFailureWithMutation(
            "install-local.after-prepare",
            () =>
            {
                if (unreadable)
                {
                    File.WriteAllText(journalPath, "{", new UTF8Encoding(false));
                    return;
                }
                JsonObject journal = JsonNode.Parse(File.ReadAllText(journalPath, Encoding.UTF8))!.AsObject();
                journal["localInstall"]!["phase"] = "Corrupted";
                WriteJson(journalPath, journal);
            },
            "install-local",
            packages.ContentV2,
            "--game-root",
            game,
            "--expected-unique-id",
            "Tests.Deployment",
            "--expected-version",
            "1.1.0",
            "--expected-package-sha256",
            v2Sha256).ConfigureAwait(false);
        Equal(
            "authority-unknown-fail-closed",
            failed.Values["rollback"],
            "Corrupt durable authority makes no recovery claim " + label);
        True(
            failed.Diagnostics.Any(value =>
                value.Code == "SDK498" &&
                value.Message.Contains("no recovery claim is made", StringComparison.Ordinal)),
            "Corrupt durable authority reports fail-closed classification " + label);
    }

    private static async Task TestTerminalLocalInstallCommitReconciliation(
        string temp,
        DeploymentPackages packages,
        bool crash)
    {
        string label = crash ? "crash" : "fault";
        string game = NewGameRoot(temp, "atomic-local-terminal-" + label);
        string v1Sha256 = Sha256(packages.ContentV1);
        string v2Sha256 = Sha256(packages.ContentV2);
        await ExpectSuccess(
            "terminal local seed " + label,
            "install-local",
            packages.ContentV1,
            "--game-root",
            game,
            "--expected-unique-id",
            "Tests.Deployment",
            "--expected-version",
            "1.0.0",
            "--expected-package-sha256",
            v1Sha256).ConfigureAwait(false);
        CommandReport committed = crash
            ? await ExpectCrashSuccess(
                "install-local.commit.after-journal",
                "install-local",
                packages.ContentV2,
                "--game-root",
                game,
                "--expected-unique-id",
                "Tests.Deployment",
                "--expected-version",
                "1.1.0",
                "--expected-package-sha256",
                v2Sha256).ConfigureAwait(false)
            : await ExpectFaultSuccess(
                "install-local.commit.after-journal",
                "install-local",
                packages.ContentV2,
                "--game-root",
                game,
                "--expected-unique-id",
                "Tests.Deployment",
                "--expected-version",
                "1.1.0",
                "--expected-package-sha256",
                v2Sha256).ConfigureAwait(false);
        Equal("not-required-committed", committed.Values["rollback"], "Terminal local commit rollback " + label);
        Equal("reconciled-committed-after-write-exception", committed.Values["reconciliation"], "Terminal local commit reconciliation " + label);
        Equal("1.1.0", InstalledVersion(game, "Tests.Deployment"), "Terminal local installed version " + label);
        CommandReport recover = await ExpectSuccess(
            "terminal local no recovery " + label,
            "recover",
            "Tests.Deployment",
            "--game-root",
            game).ConfigureAwait(false);
        Equal("false", recover.Values["recovered"], "Terminal local commit has no retry owner " + label);
    }

    private static async Task AssertUnrelatedLocalSourcePreserved(
        string game,
        string expectedPackageSha256,
        string label)
    {
        CommandReport status = await ExpectSuccess(
            "unrelated local source " + label,
            "install-local-status",
            "Tests.Other",
            "--game-root",
            game,
            "--expected-version",
            "1.0.0",
            "--expected-package-sha256",
            expectedPackageSha256).ConfigureAwait(false);
        Equal("CommittedLocalDevelopment", status.Values["status"], "Unrelated local source status " + label);
    }

    private static void AssertAtomicLocalPreState(
        string destination,
        string journalPath,
        string sourceStatePath,
        string expectedDestinationTree,
        string expectedJournalSha256,
        string expectedSourceStateSha256,
        string label)
    {
        Equal(expectedDestinationTree, CreateDeploymentInventory(destination).TreeSha256, "Atomic local destination pre-state " + label);
        Equal(expectedJournalSha256, Sha256(journalPath), "Atomic local journal pre-state " + label);
        Equal(expectedSourceStateSha256, Sha256(sourceStatePath), "Atomic local source-state pre-state " + label);
    }

    private static async Task TestRecoveryMoveFault(string temp, DeploymentPackages packages, string crashPoint, string operation, string recoveryPoint, string expectedVersionAfterRecovery)
    {
        string game = NewGameRoot(temp, "fault-" + SafeLabel(recoveryPoint));
        if (operation is "update" or "withdraw")
            await ExpectSuccess(recoveryPoint + " initial deploy", "deploy", packages.ContentV1, "--game-root", game).ConfigureAwait(false);
        string[] operationArgs = operation switch
        {
            "deploy" => new[] { "deploy", packages.ContentV1, "--game-root", game },
            "update" => new[] { "update", packages.ContentV2, "--game-root", game },
            _ => new[] { "withdraw", "Tests.Deployment", "--game-root", game }
        };
        await ExpectCrashFailure(crashPoint, operationArgs).ConfigureAwait(false);
        await ExpectFaultFailure(recoveryPoint, "recover", "Tests.Deployment", "--game-root", game).ConfigureAwait(false);
        CommandReport retry = await ExpectSuccess(recoveryPoint + " retry", "recover", "Tests.Deployment", "--game-root", game).ConfigureAwait(false);
        Equal("true", retry.Values["recovered"], "Recovery move failure retained the Prepared journal " + recoveryPoint);
        string destination = Path.Combine(game, "Mods", "Tests.Deployment");
        if (expectedVersionAfterRecovery.Length == 0)
            True(!Directory.Exists(destination), "Recovery move retry restored absence " + recoveryPoint);
        else
            Equal(expectedVersionAfterRecovery, InstalledVersion(game, "Tests.Deployment"), "Recovery move retry restored prior exact tree " + recoveryPoint);
    }

    private static async Task TestPreparedCrashRecovery(string temp, DeploymentPackages packages, string point, string operation, string expectedVersionAfterRecovery)
    {
        string game = NewGameRoot(temp, "crash-" + SafeLabel(point));
        if (operation is "update" or "withdraw")
            await ExpectSuccess(point + " initial deploy", "deploy", packages.ContentV1, "--game-root", game).ConfigureAwait(false);
        string[] args = operation switch
        {
            "deploy" => new[] { "deploy", packages.ContentV1, "--game-root", game },
            "update" => new[] { "update", packages.ContentV2, "--game-root", game },
            _ => new[] { "withdraw", "Tests.Deployment", "--game-root", game }
        };
        await ExpectCrashFailure(point, args).ConfigureAwait(false);
        CommandReport recovered = await ExpectSuccess(point + " explicit recovery", "recover", "Tests.Deployment", "--game-root", game).ConfigureAwait(false);
        Equal("true", recovered.Values["recovered"], "Prepared crash recovery flag " + point);
        string destination = Path.Combine(game, "Mods", "Tests.Deployment");
        if (expectedVersionAfterRecovery.Length == 0)
            True(!Directory.Exists(destination), "Prepared deploy crash returned to absent state " + point);
        else
            Equal(expectedVersionAfterRecovery, InstalledVersion(game, "Tests.Deployment"), "Prepared crash restored version " + point);
    }

    private static async Task TestSourceModesAndTreeDigest(string temp, DeploymentPackages packages)
    {
        string vectorEmpty = Path.Combine(temp, "tree-vector-empty");
        Directory.CreateDirectory(vectorEmpty);
        CommandReport emptyHash = await ExpectSuccess("empty tree vector", "hash", vectorEmpty).ConfigureAwait(false);
        Equal("E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855", emptyHash.Sha256, "FileTree v1 empty vector");
        string vector = Path.Combine(temp, "tree-vector-two-files");
        Directory.CreateDirectory(Path.Combine(vector, "sub"));
        File.WriteAllBytes(Path.Combine(vector, "a.txt"), Encoding.UTF8.GetBytes("A"));
        File.WriteAllBytes(Path.Combine(vector, "sub", "β.bin"), new byte[] { 0, 1, 2, 255 });
        CommandReport vectorHash = await ExpectSuccess("two-file tree vector", "hash", vector).ConfigureAwait(false);
        Equal("8E7C6E58D4982A1C5D749524A8A56A4C2171D7301144EE5640ABAC7AE74CB4CD", vectorHash.Sha256, "FileTree v1 two-file vector");
        Equal("DTMAPI-FileTree-SHA256-v1", vectorHash.Values["treeDigestAlgorithm"], "FileTree algorithm identity");

        string game = NewGameRoot(temp, "source-modes");
        CommandReport deployed = await ExpectSuccess("source local package deploy", "deploy", packages.ContentV1, "--game-root", game).ConfigureAwait(false);
        string sourcePath = deployed.OutputPath;
        CommandReport local = await ExpectSuccess("source local select", "source", "local", "select", "Tests.Deployment", sourcePath, "--game-root", game).ConfigureAwait(false);
        Equal("LocalDevelopment", local.Values["mode"], "Local Development mode");
        True(local.Values["expectedTreeSha256"].Length == 64 && local.Values["expectedTreeSha256"].All(character => !char.IsLetter(character) || char.IsUpper(character)), "Source digest is uppercase 64 hex");
        CommandReport sourceStatus = await ExpectSuccess("source local status", "source", "status", "Tests.Deployment", "--game-root", game).ConfigureAwait(false);
        string statePath = sourceStatus.Values["statePath"];
        string expectedKey = ComputeGameRootKey(game);
        True(statePath.Replace('\\', '/').Contains("/installations/" + expectedKey + "/source-state.json", StringComparison.Ordinal), "Source-state path/key matches Core schema");
        using (JsonDocument stateDocument = JsonDocument.Parse(File.ReadAllText(statePath, Encoding.UTF8)))
        {
            string[] names = stateDocument.RootElement.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray();
            string[] expectedNames = { "gameRoot", "playerReproductionActive", "reproductionSnapshotId", "schemaVersion", "selections" };
            True(names.SequenceEqual(expectedNames.OrderBy(name => name, StringComparer.Ordinal), StringComparer.Ordinal), "source-state.json exact Core top-level schema");
        }

        CommandReport workshop = await ExpectSuccess("prepare Workshop Validation", "source", "workshop", "prepare", "Tests.Other", "--game-root", game).ConfigureAwait(false);
        Equal("not-proven-offline-runtime-must-verify", workshop.Values["nativeValidation"], "Workshop preparation never fakes native pass");
        CommandReport reproduction = await ExpectSuccess("begin Player Reproduction", "source", "reproduction", "begin", "--game-root", game).ConfigureAwait(false);
        string snapshotId = reproduction.Values["snapshotId"];
        await ExpectFailure("override blocked in reproduction", "source", "local", "clear", "Tests.Deployment", "--game-root", game).ConfigureAwait(false);
        await ExpectFailure("new Workshop mode blocked in reproduction", "source", "workshop", "prepare", "Tests.Blocked", "--game-root", game).ConfigureAwait(false);
        await ExpectFailure("wrong snapshot restore", "source", "reproduction", "restore", new string('0', 32), "--game-root", game).ConfigureAwait(false);
        await ExpectSuccess("matching snapshot restore", "source", "reproduction", "restore", snapshotId, "--game-root", game).ConfigureAwait(false);
        CommandReport restoredLocal = await ExpectSuccess("restored local status", "source", "status", "Tests.Deployment", "--game-root", game).ConfigureAwait(false);
        Equal("LocalDevelopment", restoredLocal.Values["mode"], "Player Reproduction restored Local Development selection");

        string installationRoot = Path.GetDirectoryName(statePath)!;
        WriteJson(Path.Combine(installationRoot, "workshop-subscriptions.json"), new
        {
            schemaVersion = 1,
            gameRoot = game,
            available = true,
            nativeOwner = "DolocTown.Config.ModManager.GetSubscribedMods",
            failure = "",
            capturedAtUtc = "2026-07-15T00:00:00.0000000+00:00",
            subscriptions = Array.Empty<object>()
        });
        CommandReport workshopStatus = await ExpectSuccess("Workshop status with native snapshot", "source", "status", "Tests.Other", "--game-root", game).ConfigureAwait(false);
        Equal("true", workshopStatus.Values["nativeSnapshotAvailable"], "Runtime-captured snapshot reported");
        Equal("not-proven-offline-runtime-must-verify", workshopStatus.Values["nativeValidation"], "Even available snapshot is not forged into offline pass");

        File.AppendAllText(Path.Combine(sourcePath, "info.json"), " ", Encoding.UTF8);
        await ExpectFailure("local source drift status", "source", "status", "Tests.Deployment", "--game-root", game).ConfigureAwait(false);

        string otherGame = NewGameRoot(temp, "source-modes-other-root");
        CommandReport otherDeploy = await ExpectSuccess("other root source deploy", "deploy", packages.ContentV1, "--game-root", otherGame).ConfigureAwait(false);
        await ExpectSuccess("other root local select", "source", "local", "select", "Tests.Deployment", otherDeploy.OutputPath, "--game-root", otherGame).ConfigureAwait(false);
        CommandReport otherStatus = await ExpectSuccess("other root source status", "source", "status", "Tests.Deployment", "--game-root", otherGame).ConfigureAwait(false);
        True(!PathsEqual(statePath, otherStatus.Values["statePath"]), "Source state is isolated across game roots");
    }

    private static async Task TestDoctorAndExplicitSession(string temp, DeploymentPackages packages)
    {
        string doctorRoot = Path.Combine(temp, "doctor-empty");
        Directory.CreateDirectory(doctorRoot);
        using (var humanOutput = new StringWriter())
        using (var humanError = new StringWriter())
        {
            int exitCode = await AuthorApplication.RunAsync(new[] { "doctor", doctorRoot }, humanOutput, humanError).ConfigureAwait(false);
            Equal("0", exitCode.ToString(System.Globalization.CultureInfo.InvariantCulture), "Doctor human route exit stdout=" + humanOutput + " stderr=" + humanError);
            True(humanOutput.ToString().Contains("DTMAPI Install Doctor (read-only)", StringComparison.Ordinal), "Doctor human route uses the support library formatter");
            True(humanError.ToString().Length == 0, "Doctor human route wrote no error");
        }
        using (var jsonOutput = new StringWriter())
        using (var jsonError = new StringWriter())
        {
            int exitCode = await AuthorApplication.RunAsync(new[] { "doctor", doctorRoot, "--json" }, jsonOutput, jsonError).ConfigureAwait(false);
            Equal("0", exitCode.ToString(System.Globalization.CultureInfo.InvariantCulture), "Doctor JSON route exit");
            using JsonDocument doctor = JsonDocument.Parse(jsonOutput.ToString());
            Equal(Path.GetFullPath(doctorRoot), doctor.RootElement.GetProperty("rootPath").GetString() ?? string.Empty, "Doctor JSON root");
            True(doctor.RootElement.GetProperty("readOnlyByDesign").GetBoolean(), "Doctor JSON preserves read-only authority");
            AssertJsonMatchesSchema(
                doctor.RootElement,
                Path.Combine(AppContext.BaseDirectory, "schemas", "doctor-report.schema.json"));
            True(jsonError.ToString().Length == 0, "Doctor JSON route wrote no error");
        }
        string testOutput = AppContext.BaseDirectory;
        True(File.Exists(Path.Combine(testOutput, "DTMAPI.InstallDoctor.dll")), "Doctor is distributed as the Author SDK support library");
        True(!File.Exists(Path.Combine(testOutput, "dtmapi-doctor.exe")) && !File.Exists(Path.Combine(testOutput, "dtmapi-doctor.runtimeconfig.json")), "One-CLI test output contains no independent Doctor apphost");
        Equal("0.1.0.0", System.Reflection.AssemblyName.GetAssemblyName(Path.Combine(testOutput, "DTMAPI.InstallDoctor.dll")).Version?.ToString() ?? string.Empty, "Doctor support library version");
        Equal("0.1.0.0", System.Reflection.AssemblyName.GetAssemblyName(Path.Combine(testOutput, "DTMAPI.Tooling.Metadata.dll")).Version?.ToString() ?? string.Empty, "Tooling metadata support library version");

        foreach (string point in new[] { "session.prepare.after-client", "session.prepare.after-descriptor" })
        {
            string atomicGame = NewGameRoot(temp, "explicit-session-" + SafeLabel(point));
            await ExpectFaultFailure(point, "session", "prepare", "--game-root", atomicGame).ConfigureAwait(false);
            string stateRoot = Path.Combine(Environment.GetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT")!, "installations", ComputeGameRootKey(atomicGame));
            True(!File.Exists(Path.Combine(stateRoot, "author-session.json")) && !File.Exists(Path.Combine(stateRoot, "author-session-client.json")), "Session prepare fault leaves no half-authorized state " + point);
            True(!Directory.Exists(stateRoot) || !Directory.EnumerateFiles(stateRoot, "*.tmp-*", SearchOption.TopDirectoryOnly).Any(), "Session prepare fault leaves no temporary secret " + point);
        }

        string game = NewGameRoot(temp, "explicit-session");
        CommandReport deployed = await ExpectSuccess("session source deploy", "deploy", packages.ContentV1, "--game-root", game).ConfigureAwait(false);
        string selectedRoot = deployed.OutputPath;
        CommandReport prepared = await ExpectSuccess("session prepare", "session", "prepare", "--game-root", game).ConfigureAwait(false);
        string descriptorPath = prepared.Values["descriptorPath"];
        string credentialPath = prepared.Values["credentialPath"];
        True(File.Exists(descriptorPath) && File.Exists(credentialPath), "Session prepare wrote descriptor and client credential");
        JsonObject descriptor = JsonNode.Parse(File.ReadAllText(descriptorPath, Encoding.UTF8))!.AsObject();
        JsonObject credential = JsonNode.Parse(File.ReadAllText(credentialPath, Encoding.UTF8))!.AsObject();
        string[] exactDescriptorProperties = { "createdAtUtc", "expiresAtUtc", "gameRoot", "pipeName", "runtimeVersion", "schemaVersion", "sessionId", "token" };
        True(descriptor.Select(pair => pair.Key).OrderBy(value => value, StringComparer.Ordinal).SequenceEqual(exactDescriptorProperties, StringComparer.Ordinal), "Session descriptor has the exact Core schema=1 property set");
        Equal(File.ReadAllText(descriptorPath, Encoding.UTF8), File.ReadAllText(credentialPath, Encoding.UTF8), "Descriptor and client receipt preserve the same credential identity");
        Equal("1", descriptor["schemaVersion"]!.GetValue<int>().ToString(System.Globalization.CultureInfo.InvariantCulture), "Session descriptor schema");
        Equal(Path.GetFullPath(game), descriptor["gameRoot"]!.GetValue<string>(), "Session descriptor game root");
        Equal("0.5.5", descriptor["runtimeVersion"]!.GetValue<string>(), "Session descriptor Runtime version");
        string sessionId = descriptor["sessionId"]!.GetValue<string>();
        string token = descriptor["token"]!.GetValue<string>();
        string pipeName = descriptor["pipeName"]!.GetValue<string>();
        True(Guid.TryParseExact(sessionId, "N", out Guid parsedSession) && parsedSession != Guid.Empty, "Session descriptor uses GUID N sessionId");
        True(token.Length == 43 && token.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_'), "Session token is 256-bit base64url without padding");
        Equal("dtmapi-author-" + ComputeGameRootKey(game) + "-" + sessionId, pipeName, "Session pipe derivation matches Core");
        DateTimeOffset created = DateTimeOffset.ParseExact(descriptor["createdAtUtc"]!.GetValue<string>(), "O", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind);
        DateTimeOffset expires = DateTimeOffset.ParseExact(descriptor["expiresAtUtc"]!.GetValue<string>(), "O", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind);
        True(created.Offset == TimeSpan.Zero && expires.Offset == TimeSpan.Zero && expires > created && expires - created == TimeSpan.FromMinutes(10), "Session descriptor is short-lived UTC and below Core's 15-minute maximum");
        True(!Directory.EnumerateFiles(Path.GetDirectoryName(descriptorPath)!, "*.tmp-*", SearchOption.TopDirectoryOnly).Any(), "Session secret atomic write left no temporary file");
        if (OperatingSystem.IsWindows())
            AssertCurrentUserOnlyAcl(descriptorPath, credentialPath);
        True(!JsonSerializer.Serialize(prepared, JsonOptions).Contains(token, StringComparison.Ordinal), "Session prepare report does not reveal token");
        CommandReport duplicatePrepare = await ExpectFailure("duplicate session prepare", "session", "prepare", "--game-root", game).ConfigureAwait(false);
        Equal("session-already-prepared", duplicatePrepare.Values["statusCode"], "Duplicate prepare refuses active credential");

        File.Delete(descriptorPath); // Simulate Runtime's one-shot atomic descriptor consumption.
        CommandReport sourceHash = await ExpectSuccess("session request hash", "hash", selectedRoot).ConfigureAwait(false);
        Task<JsonObject> snapshotServer = StartSessionServer(pipeName, request => BuildSessionResponse(request, "ok", "source-snapshot", "snapshot complete", token));
        CommandReport snapshot = await ExpectSuccess("session snapshot", "session", "snapshot", "Tests.Deployment", selectedRoot, "--game-root", game, "--timeout-seconds", "10").ConfigureAwait(false);
        JsonObject snapshotRequest = await snapshotServer.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        AssertSessionRequest(snapshotRequest, game, sessionId, token, "Tests.Deployment", selectedRoot, sourceHash.Sha256, "get-source-snapshot");
        Equal("ok", snapshot.Values["runtimeStatus"], "Snapshot Runtime status");
        Equal("[redacted]", snapshot.Values["response.echo"], "Runtime response values cannot echo the token");
        True(!JsonSerializer.Serialize(snapshot, JsonOptions).Contains(token, StringComparison.Ordinal), "Snapshot report never echoes token");

        Task<JsonObject> reloadServer = StartSessionServer(pipeName, request => BuildSessionResponse(request, "restart-required", "content-restart-required", "this format requires restart", string.Empty));
        CommandReport reload = await ExpectSuccess("session reload restart", "session", "reload", "Tests.Deployment", selectedRoot, "--game-root", game, "--timeout-seconds", "10").ConfigureAwait(false);
        JsonObject reloadRequest = await reloadServer.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        AssertSessionRequest(reloadRequest, game, sessionId, token, "Tests.Deployment", selectedRoot, sourceHash.Sha256, "reload-content");
        Equal("restart-required", reload.Values["runtimeStatus"], "Reload reports deterministic restart-required without treating it as transport failure");
        HasCode(reload, "SDK602", "Reload restart-required warning");

        Task<JsonObject> wrongResponseServer = StartSessionServer(pipeName, request =>
        {
            JsonObject response = BuildSessionResponse(request, "ok", "source-snapshot", "wrong identity", string.Empty);
            response["requestId"] = new string('0', 32);
            return response;
        });
        CommandReport wrongResponse = await ExpectFailure("session wrong response", "session", "snapshot", "Tests.Deployment", selectedRoot, "--game-root", game, "--timeout-seconds", "10").ConfigureAwait(false);
        await wrongResponseServer.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        Equal("pipe-response-identity-mismatch", wrongResponse.Values["statusCode"], "Wrong response identity fails closed");

        CommandReport connectTimeout = await ExpectFailure("session connect timeout", "session", "snapshot", "Tests.Deployment", selectedRoot, "--game-root", game, "--timeout-seconds", "1").ConfigureAwait(false);
        Equal("pipe-connect-timeout", connectTimeout.Values["statusCode"], "Missing explicit Runtime pipe reports a bounded connect timeout");

        Task<JsonObject> silentServer = StartSilentSessionServer(pipeName, TimeSpan.FromMilliseconds(1500));
        CommandReport responseTimeout = await ExpectFailure("session response timeout", "session", "snapshot", "Tests.Deployment", selectedRoot, "--game-root", game, "--timeout-seconds", "1").ConfigureAwait(false);
        await silentServer.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        Equal("pipe-response-timeout", responseTimeout.Values["statusCode"], "Silent Runtime pipe reports a bounded response timeout");

        File.Delete(credentialPath);
        CommandReport noCredential = await ExpectFailure("session no credential", "session", "snapshot", "Tests.Deployment", selectedRoot, "--game-root", game, "--timeout-seconds", "1").ConfigureAwait(false);
        Equal("session-credential-missing", noCredential.Values["statusCode"], "Session request never falls back to a missing token");
        await ExpectSuccess("session clear empty", "session", "clear", "--game-root", game).ConfigureAwait(false);

        CommandReport expiring = await ExpectSuccess("session expiry fixture", "session", "prepare", "--game-root", game).ConfigureAwait(false);
        string expiringSession = expiring.Values["sessionId"];
        JsonObject expired = JsonNode.Parse(File.ReadAllText(expiring.Values["descriptorPath"], Encoding.UTF8))!.AsObject();
        DateTimeOffset expiredCreated = DateTimeOffset.UtcNow.AddMinutes(-20);
        expired["createdAtUtc"] = expiredCreated.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
        expired["expiresAtUtc"] = expiredCreated.AddMinutes(10).ToString("O", System.Globalization.CultureInfo.InvariantCulture);
        WriteJson(expiring.Values["descriptorPath"], expired);
        WriteJson(expiring.Values["credentialPath"], expired);
        CommandReport replacedExpired = await ExpectSuccess("expired session replacement", "session", "prepare", "--game-root", game).ConfigureAwait(false);
        True(!replacedExpired.Values["sessionId"].Equals(expiringSession, StringComparison.Ordinal), "Expired matching descriptor/credential is atomically replaced with a fresh session");
        await ExpectSuccess("session final clear", "session", "clear", "--game-root", game).ConfigureAwait(false);
        True(!File.Exists(replacedExpired.Values["descriptorPath"]) && !File.Exists(replacedExpired.Values["credentialPath"]), "Session clear removes both reserved credential files");
    }

    [SupportedOSPlatform("windows")]
    private static void AssertCurrentUserOnlyAcl(params string[] paths)
    {
        SecurityIdentifier current = WindowsIdentity.GetCurrent().User ?? throw new InvalidOperationException("Current Windows user SID missing in ACL test.");
        foreach (string path in paths)
        {
            FileSecurity security = new FileInfo(path).GetAccessControl();
            True(security.AreAccessRulesProtected, "Session secret ACL blocks inherited principals: " + path);
            AuthorizationRuleCollection rules = security.GetAccessRules(includeExplicit: true, includeInherited: true, typeof(SecurityIdentifier));
            True(rules.Cast<FileSystemAccessRule>().All(rule => rule.IdentityReference.Equals(current) && !rule.IsInherited && rule.AccessControlType == AccessControlType.Allow), "Session secret ACL grants only the current user: " + path);
        }
    }

    private static Task<JsonObject> StartSessionServer(string pipeName, Func<JsonObject, JsonObject> responseFactory) => Task.Run(async () =>
    {
        using var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        await server.WaitForConnectionAsync().ConfigureAwait(false);
        using var reader = new StreamReader(server, new UTF8Encoding(false, true), detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);
        string frame = await reader.ReadLineAsync().ConfigureAwait(false) ?? throw new InvalidOperationException("Session client did not send a newline frame.");
        JsonObject request = JsonNode.Parse(frame)!.AsObject();
        string response = responseFactory(request).ToJsonString() + "\n";
        byte[] bytes = new UTF8Encoding(false).GetBytes(response);
        await server.WriteAsync(bytes).ConfigureAwait(false);
        await server.FlushAsync().ConfigureAwait(false);
        return request;
    });

    private static Task<JsonObject> StartSilentSessionServer(string pipeName, TimeSpan delay) => Task.Run(async () =>
    {
        using var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        await server.WaitForConnectionAsync().ConfigureAwait(false);
        using var reader = new StreamReader(server, new UTF8Encoding(false, true), detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);
        string frame = await reader.ReadLineAsync().ConfigureAwait(false) ?? throw new InvalidOperationException("Session client did not send a newline frame.");
        JsonObject request = JsonNode.Parse(frame)!.AsObject();
        await Task.Delay(delay).ConfigureAwait(false);
        return request;
    });

    private static JsonObject BuildSessionResponse(JsonObject request, string status, string code, string message, string echoValue) => new()
    {
        ["protocol"] = "dtmapi-author-session/1",
        ["runtime"] = "0.5.5",
        ["session"] = request["session"]!.GetValue<string>(),
        ["requestId"] = request["requestId"]!.GetValue<string>(),
        ["operation"] = request["operation"]!.GetValue<string>(),
        ["uniqueId"] = request["uniqueId"]!.GetValue<string>(),
        ["status"] = status,
        ["code"] = code,
        ["message"] = message,
        ["values"] = echoValue.Length == 0
            ? new JsonArray()
            : new JsonArray(new JsonObject { ["key"] = "echo", ["value"] = echoValue })
    };

    private static void AssertSessionRequest(JsonObject request, string gameRoot, string sessionId, string token, string uniqueId, string selectedRoot, string treeSha256, string operation)
    {
        string[] exact = { "expectedTreeSha256", "gameRoot", "operation", "protocol", "requestId", "runtime", "selectedRoot", "session", "token", "uniqueId" };
        True(request.Select(pair => pair.Key).OrderBy(value => value, StringComparer.Ordinal).SequenceEqual(exact, StringComparer.Ordinal), "Session request has exact Core JSONL fields");
        Equal("dtmapi-author-session/1", request["protocol"]!.GetValue<string>(), "Session request protocol");
        Equal("0.5.5", request["runtime"]!.GetValue<string>(), "Session request Runtime");
        Equal(Path.GetFullPath(gameRoot), request["gameRoot"]!.GetValue<string>(), "Session request game root");
        Equal(sessionId, request["session"]!.GetValue<string>(), "Session request sessionId");
        Equal(token, request["token"]!.GetValue<string>(), "Session request carries protected token on pipe only");
        True(Guid.TryParseExact(request["requestId"]!.GetValue<string>(), "N", out Guid requestId) && requestId != Guid.Empty, "Session request uses unique GUID N requestId");
        Equal(uniqueId, request["uniqueId"]!.GetValue<string>(), "Session request UniqueID");
        Equal(Path.GetFullPath(selectedRoot), request["selectedRoot"]!.GetValue<string>(), "Session request selected root");
        Equal(treeSha256, request["expectedTreeSha256"]!.GetValue<string>(), "Session request exact FileTree-v1 hash");
        Equal(operation, request["operation"]!.GetValue<string>(), "Session request operation");
    }

    private static async Task<CommandReport> ExpectFaultFailure(string point, params string[] args)
    {
        Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_FAULT", "fail:" + point);
        try
        {
            return await ExpectFailure("fault " + point, args).ConfigureAwait(false);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_FAULT", null);
        }
    }

    private static async Task<CommandReport> ExpectFaultSuccess(string point, params string[] args)
    {
        Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_FAULT", "fail:" + point);
        try
        {
            return await ExpectSuccess("reconciled fault " + point, args).ConfigureAwait(false);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_FAULT", null);
        }
    }

    private static async Task<CommandReport> ExpectCrashSuccess(string point, params string[] args)
    {
        Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_FAULT", "crash:" + point);
        try
        {
            CommandReport report = await ExpectSuccess("reconciled crash " + point, args).ConfigureAwait(false);
            HasCode(report, "SDK412", "Reconciled crash diagnostic " + point);
            return report;
        }
        finally
        {
            Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_FAULT", null);
        }
    }

    private static async Task<CommandReport> ExpectCrashFailure(string point, params string[] args)
    {
        Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_FAULT", "crash:" + point);
        try
        {
            CommandReport report = await ExpectFailure("crash " + point, args).ConfigureAwait(false);
            HasCode(report, "SDK498", "Simulated crash diagnostic " + point);
            return report;
        }
        finally
        {
            Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_FAULT", null);
        }
    }

    private static async Task<CommandReport> ExpectCrashFailureWithMutation(
        string point,
        Action mutation,
        params string[] args)
    {
        Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_FAULT", "crash:" + point);
        SetAuthorFaultMutation(_ => mutation());
        try
        {
            CommandReport report = await ExpectFailure("crash with authority mutation " + point, args).ConfigureAwait(false);
            HasCode(report, "SDK498", "Simulated corrupt-authority crash diagnostic " + point);
            return report;
        }
        finally
        {
            SetAuthorFaultMutation(null);
            Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_FAULT", null);
        }
    }

    private static void SetAuthorFaultMutation(Action<string>? action)
    {
        Type injector = typeof(AuthorApplication).Assembly.GetType(
            "DTMAPI.AuthorSdk.AuthorFaultInjector",
            throwOnError: true)!;
        System.Reflection.PropertyInfo property = injector.GetProperty(
            "BeforeThrowForTests",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Author fault mutation test seam is unavailable.");
        property.SetValue(null, action);
    }

    private static string NewGameRoot(string temp, string label)
    {
        string root = Path.Combine(temp, "games", label);
        Directory.CreateDirectory(root);
        return root;
    }

    private static string InstalledVersion(string gameRoot, string uniqueId)
    {
        string path = Path.Combine(gameRoot, "Mods", uniqueId, "Content", "DTMAPI", "manifest.json");
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path, Encoding.UTF8));
        return document.RootElement.GetProperty("Version").GetString() ?? string.Empty;
    }

    private static LegacyDeploymentState ConvertInstalledDeploymentToLegacy(string gameRoot, string journalPath, string uniqueId, string packageKind)
    {
        string destination = Path.Combine(gameRoot, "Mods", uniqueId);
        string receiptPath = Path.Combine(destination, ".dtmapi-author-receipt.json");
        JsonObject currentReceipt = JsonNode.Parse(File.ReadAllText(receiptPath, Encoding.UTF8))!.AsObject();
        var state = new LegacyDeploymentState
        {
            GameRoot = Path.GetFullPath(gameRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            JournalPath = journalPath,
            DestinationPath = destination,
            UniqueId = uniqueId,
            PackageKind = packageKind,
            TransactionId = currentReceipt["transactionId"]!.GetValue<string>(),
            GameRootKey = currentReceipt["gameRootKey"]!.GetValue<string>(),
            PackageSha256 = currentReceipt["packageSha256"]!.GetValue<string>()
        };

        string manifestPath = Path.Combine(destination, "Content", "DTMAPI", "manifest.json");
        JsonObject manifest = JsonNode.Parse(File.ReadAllText(manifestPath, Encoding.UTF8))!.AsObject();
        Equal(uniqueId, manifest["UniqueID"]!.GetValue<string>(), "Legacy conversion manifest identity");
        Equal(packageKind, manifest["Type"]!.GetValue<string>(), "Legacy conversion package kind");
        manifest.Remove("CodeModKind");
        WriteJson(manifestPath, manifest);
        RefreshLegacyReceiptAndJournal(state);
        return state;
    }

    private static void RefreshLegacyReceiptAndJournal(LegacyDeploymentState state)
    {
        WriteLegacyPackageMarker(state);
        DeploymentTreeInventory payload = CreateDeploymentInventory(state.DestinationPath, excludeReceipt: true);
        var receipt = new JsonObject
        {
            ["schemaVersion"] = 1,
            ["transactionId"] = state.TransactionId,
            ["gameRootKey"] = state.GameRootKey,
            ["uniqueId"] = state.UniqueId,
            ["packageKind"] = state.PackageKind,
            ["destinationRelativePath"] = "Mods/" + state.UniqueId,
            ["packageSha256"] = state.PackageSha256,
            ["payloadTreeSha256"] = payload.TreeSha256,
            ["payloadFileCount"] = payload.Files.Count,
            ["payloadDirectoryCount"] = payload.Directories.Count
        };
        string receiptPath = Path.Combine(state.DestinationPath, ".dtmapi-author-receipt.json");
        WriteJson(receiptPath, receipt);
        DeploymentTreeInventory full = CreateDeploymentInventory(state.DestinationPath);
        state.Record = new DeploymentRecord
        {
            TransactionId = state.TransactionId,
            PackageSha256 = state.PackageSha256,
            PayloadTreeSha256 = payload.TreeSha256,
            ReceiptSha256 = Sha256(receiptPath),
            Inventory = full
        };
        WriteLegacyJournal(state, "Installed", null);
    }

    private static void MutateLegacyReceiptAndRefreshRecord(LegacyDeploymentState state, Action<JsonObject> mutate)
    {
        string receiptPath = Path.Combine(state.DestinationPath, ".dtmapi-author-receipt.json");
        JsonObject receipt = JsonNode.Parse(File.ReadAllText(receiptPath, Encoding.UTF8))!.AsObject();
        mutate(receipt);
        WriteJson(receiptPath, receipt);
        state.Record.ReceiptSha256 = Sha256(receiptPath);
        state.Record.Inventory = CreateDeploymentInventory(state.DestinationPath);
        WriteLegacyJournal(state, "Installed", null);
    }

    private static void WriteLegacyPackageMarker(LegacyDeploymentState state)
    {
        string manifestPath = Path.Combine(state.DestinationPath, "Content", "DTMAPI", "manifest.json");
        JsonObject manifest = JsonNode.Parse(File.ReadAllText(manifestPath, Encoding.UTF8))!.AsObject();
        var marker = new JsonObject
        {
            ["schemaVersion"] = 1,
            ["owner"] = "DTMAPI",
            ["uniqueId"] = manifest["UniqueID"]!.GetValue<string>(),
            ["version"] = manifest["Version"]!.GetValue<string>(),
            ["packageKind"] = manifest["Type"]!.GetValue<string>(),
            ["authorSdkVersion"] = "0.1.0",
            ["targetRuntimeVersion"] = "0.5.5",
            ["authority"] = "metadata-only-not-an-ownership-receipt"
        };
        WriteJson(Path.Combine(state.DestinationPath, "Content", "DTMAPI", "dtmapi-package.json"), marker);
    }

    private static void PrepareLegacyUpdateForRecovery(LegacyDeploymentState state)
    {
        string transactionId = Guid.NewGuid().ToString("N");
        string hidden = Path.Combine(state.GameRoot, "Mods", ".dtmapi-author");
        string stagingPath = Path.Combine(hidden, "staging", transactionId);
        string recoveryPath = Path.Combine(hidden, "recovery", state.UniqueId + "-" + state.Record.TransactionId + "-" + transactionId);
        string failedPath = Path.Combine(hidden, "failed", state.UniqueId + "-" + transactionId);
        CopyDirectory(state.DestinationPath, stagingPath);
        File.WriteAllText(Path.Combine(stagingPath, "legacy-next-only.txt"), "prepared legacy update", new UTF8Encoding(false));
        DeploymentTreeInventory nextPayload = CreateDeploymentInventory(stagingPath, excludeReceipt: true);
        DeploymentTreeInventory nextInventory = CreateDeploymentInventory(stagingPath);
        var next = new DeploymentRecord
        {
            TransactionId = transactionId,
            PackageSha256 = state.PackageSha256,
            PayloadTreeSha256 = nextPayload.TreeSha256,
            ReceiptSha256 = Sha256(Path.Combine(stagingPath, ".dtmapi-author-receipt.json")),
            Inventory = nextInventory
        };
        var active = new DeploymentTransaction
        {
            TransactionId = transactionId,
            Operation = "update",
            Phase = "Prepared",
            StagingPath = stagingPath,
            RecoveryPath = recoveryPath,
            FailedPath = failedPath,
            Previous = state.Record,
            Next = next
        };
        WriteLegacyJournal(state, "Prepared", active);
    }

    private static void WriteLegacyJournal(LegacyDeploymentState state, string status, DeploymentTransaction? active)
    {
        var journal = new JsonObject
        {
            ["schemaVersion"] = 1,
            ["gameRoot"] = state.GameRoot,
            ["gameRootKey"] = state.GameRootKey,
            ["uniqueId"] = state.UniqueId,
            ["packageKind"] = state.PackageKind,
            ["destinationPath"] = state.DestinationPath,
            ["status"] = status,
            ["committed"] = JsonSerializer.SerializeToNode(state.Record, JsonOptions),
            ["active"] = active == null ? null : JsonSerializer.SerializeToNode(active, JsonOptions),
            ["recoveryArtifacts"] = new JsonArray()
        };
        WriteJson(state.JournalPath, journal);
    }

    private static DeploymentTreeInventory CreateDeploymentInventory(string root, bool excludeReceipt = false)
    {
        List<string> directories = Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();
        List<DeploymentInventoryFile> files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Select(path => new DeploymentInventoryFile
            {
                Path = Path.GetRelativePath(root, path).Replace('\\', '/'),
                Length = new FileInfo(path).Length,
                Sha256 = Sha256(path)
            })
            .Where(file => !excludeReceipt || !file.Path.Equals(".dtmapi-author-receipt.json", StringComparison.Ordinal))
            .OrderBy(file => file.Path, StringComparer.Ordinal)
            .ToList();
        using var aggregate = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (string directory in directories)
            aggregate.AppendData(Encoding.UTF8.GetBytes("D\0" + directory + "\0"));
        foreach (DeploymentInventoryFile file in files)
            aggregate.AppendData(Encoding.UTF8.GetBytes("F\0" + file.Path + "\0" + file.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\0" + file.Sha256 + "\0"));
        return new DeploymentTreeInventory
        {
            TreeSha256 = Convert.ToHexString(aggregate.GetHashAndReset()).ToLowerInvariant(),
            Directories = directories,
            Files = files
        };
    }

    private static string ReadSchemaVersion(string path)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path, Encoding.UTF8));
        return document.RootElement.GetProperty("schemaVersion").GetInt32().ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static void AssertCurrentDeploymentState(string gameRoot, string journalPath, string uniqueId, string codeModKind)
    {
        string destination = Path.Combine(gameRoot, "Mods", uniqueId);
        Equal("3", ReadSchemaVersion(journalPath), "Mutating a legacy deployment writes schema-3 journal");
        Equal("2", ReadSchemaVersion(Path.Combine(destination, ".dtmapi-author-receipt.json")), "Legacy update publishes schema-2 receipt");
        Equal("2", ReadSchemaVersion(Path.Combine(destination, "Content", "DTMAPI", "dtmapi-package.json")), "Legacy update publishes schema-2 package marker");
        JsonObject journal = JsonNode.Parse(File.ReadAllText(journalPath, Encoding.UTF8))!.AsObject();
        JsonObject receipt = JsonNode.Parse(File.ReadAllText(Path.Combine(destination, ".dtmapi-author-receipt.json"), Encoding.UTF8))!.AsObject();
        Equal(codeModKind, journal["codeModKind"]!.GetValue<string>(), "Upgraded journal managed identity");
        Equal(codeModKind, receipt["codeModKind"]!.GetValue<string>(), "Upgraded receipt managed identity");
    }

    private static void MutateJson(string path, Action<JsonObject> mutate)
    {
        JsonObject json = JsonNode.Parse(File.ReadAllText(path, Encoding.UTF8))!.AsObject();
        mutate(json);
        WriteJson(path, json);
    }

    private static void AddReservedCodeModKindToPackage(string packagePath)
    {
        const string manifestEntryName = "Content/DTMAPI/manifest.json";
        using ZipArchive archive = ZipFile.Open(packagePath, ZipArchiveMode.Update);
        ZipArchiveEntry manifestEntry = archive.GetEntry(manifestEntryName)
            ?? throw new InvalidOperationException("Missing ZIP entry " + manifestEntryName);
        string manifestText;
        using (var reader = new StreamReader(manifestEntry.Open(), Encoding.UTF8))
            manifestText = reader.ReadToEnd();
        JsonObject manifest = JsonNode.Parse(manifestText)!.AsObject();
        manifest["CodeModKind"] = "Advanced";
        manifestEntry.Delete();
        ZipArchiveEntry replacement = archive.CreateEntry(manifestEntryName, CompressionLevel.Optimal);
        using var writer = new StreamWriter(replacement.Open(), new UTF8Encoding(false));
        writer.Write(JsonSerializer.Serialize(manifest, JsonOptions));
        writer.Write('\n');
    }

    private static string? FindInstalledGameRoot(string repository)
    {
        string configured = Environment.GetEnvironmentVariable("DTMAPI_GAME_DIR") ?? string.Empty;
        if (configured.Length == 0)
        {
            string settingsPath = Path.Combine(repository, "local.settings.json");
            if (File.Exists(settingsPath))
            {
                using JsonDocument settings = JsonDocument.Parse(File.ReadAllText(settingsPath, Encoding.UTF8));
                if (settings.RootElement.TryGetProperty("GameDir", out JsonElement gameDir) && gameDir.ValueKind == JsonValueKind.String)
                    configured = gameDir.GetString() ?? string.Empty;
            }
        }
        if (configured.Length == 0 && OperatingSystem.IsWindows())
        {
            var steamRoots = new List<string>();
            foreach ((Microsoft.Win32.RegistryKey Root, string SubKey) item in new[]
                     {
                         (Microsoft.Win32.Registry.CurrentUser, @"Software\Valve\Steam"),
                         (Microsoft.Win32.Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Valve\Steam")
                     })
            {
                using Microsoft.Win32.RegistryKey? key = item.Root.OpenSubKey(item.SubKey);
                if (key?.GetValue("SteamPath") is string steamPath && Directory.Exists(steamPath))
                    steamRoots.Add(Path.GetFullPath(steamPath));
            }
            foreach (string steamRoot in steamRoots.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var libraryRoots = new List<string> { steamRoot };
                string libraryFile = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
                if (File.Exists(libraryFile))
                {
                    foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(
                                 File.ReadAllText(libraryFile, Encoding.UTF8),
                                 "\\\"path\\\"\\s+\\\"([^\\\"]+)\\\"",
                                 System.Text.RegularExpressions.RegexOptions.CultureInvariant))
                    {
                        string library = match.Groups[1].Value.Replace("\\\\", "\\", StringComparison.Ordinal);
                        if (Directory.Exists(library))
                            libraryRoots.Add(Path.GetFullPath(library));
                    }
                }
                foreach (string library in libraryRoots.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    string manifest = Path.Combine(library, "steamapps", "appmanifest_2285550.acf");
                    string candidate = Path.Combine(library, "steamapps", "common", "Doloc Town");
                    if (File.Exists(manifest) && Directory.Exists(candidate))
                    {
                        configured = candidate;
                        break;
                    }
                }
                if (configured.Length > 0)
                    break;
            }
        }
        if (configured.Length == 0)
            return null;
        string root = Path.GetFullPath(configured);
        string assembly = Path.Combine(root, "DolocTown_Data", "Managed", "Assembly-CSharp.dll");
        string harmony = Path.Combine(root, "BepInEx", "core", "0Harmony.dll");
        return Directory.Exists(root) && File.Exists(assembly) && File.Exists(harmony) ? root : null;
    }

    private static string? CreateExactAdvancedReferenceInput(string temp, string repository)
    {
        string configured = Environment.GetEnvironmentVariable("DTMAPI_AUTHOR_ADVANCED_REFERENCE_ROOT") ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(configured))
        {
            string configuredRoot = Path.GetFullPath(configured);
            AssertExactAdvancedReferenceFile(
                Path.Combine(configuredRoot, "DolocTown_Data", "Managed", "Assembly-CSharp.dll"),
                AdvancedAssemblyLength,
                AdvancedAssemblySha256,
                "configured 23762374 Assembly-CSharp");
            AssertExactAdvancedReferenceFile(
                Path.Combine(configuredRoot, "BepInEx", "core", "0Harmony.dll"),
                AdvancedHarmonyLength,
                AdvancedHarmonySha256,
                "configured Advanced Harmony");
            return configuredRoot;
        }

        return CreateExactTrackedAdvancedReferenceInput(
            temp,
            repository,
            "23762374_public_C416D4",
            "advanced-reference-input-23762374",
            AdvancedAssemblyLength,
            AdvancedAssemblySha256,
            "23762374");
    }

    private static string? CreateExactAutoFishingReferenceInput(string temp, string repository)
    {
        return CreateExactTrackedAdvancedReferenceInput(
            temp,
            repository,
            "24456188_test_E861E0",
            "advanced-reference-input-24456188",
            AutoFishingAssemblyLength,
            AutoFishingAssemblySha256,
            "24456188");
    }

    private static string? CreateExactTrackedAdvancedReferenceInput(
        string temp,
        string repository,
        string reverseBuildDirectory,
        string inputDirectory,
        long assemblyLength,
        string assemblySha256,
        string buildLabel)
    {
        string assemblySource = Path.Combine(
            repository,
            "references",
            "doloc-town",
            "reverse",
            "builds",
            reverseBuildDirectory,
            "raw-snapshot",
            "game",
            "DolocTown_Data",
            "Managed",
            "Assembly-CSharp.dll");
        string bepinexArchive = Path.Combine(
            repository,
            "tools",
            "release",
            "bootstrap",
            "BepInEx_win_x64_5.4.23.5.zip");
        if (!File.Exists(assemblySource) || !File.Exists(bepinexArchive))
            return null;

        AssertExactAdvancedReferenceFile(
            assemblySource,
            assemblyLength,
            assemblySha256,
            "local-only " + buildLabel + " reverse Assembly-CSharp");
        string inputRoot = Path.Combine(temp, inputDirectory);
        string assemblyDestination = Path.Combine(
            inputRoot,
            "DolocTown_Data",
            "Managed",
            "Assembly-CSharp.dll");
        string harmonyDestination = Path.Combine(
            inputRoot,
            "BepInEx",
            "core",
            "0Harmony.dll");
        Directory.CreateDirectory(Path.GetDirectoryName(assemblyDestination)!);
        Directory.CreateDirectory(Path.GetDirectoryName(harmonyDestination)!);
        File.Copy(assemblySource, assemblyDestination);
        AssertExactAdvancedReferenceFile(
            assemblyDestination,
            assemblyLength,
            assemblySha256,
            "staged " + buildLabel + " Assembly-CSharp");
        using (ZipArchive archive = ZipFile.OpenRead(bepinexArchive))
        {
            ZipArchiveEntry entry = archive.GetEntry("BepInEx/core/0Harmony.dll")
                ?? throw new InvalidOperationException("Bundled BepInEx archive is missing BepInEx/core/0Harmony.dll.");
            using Stream source = entry.Open();
            using FileStream destination = File.Create(harmonyDestination);
            source.CopyTo(destination);
        }
        AssertExactAdvancedReferenceFile(
            harmonyDestination,
            AdvancedHarmonyLength,
            AdvancedHarmonySha256,
            "bundled Advanced Harmony");
        return inputRoot;
    }

    private static void AssertExactAdvancedReferenceFile(
        string path,
        long expectedLength,
        string expectedSha256,
        string label)
    {
        if (!File.Exists(path))
            throw new InvalidOperationException(label + " is missing: " + path);
        var file = new FileInfo(path);
        string actualSha256 = Sha256(path);
        if (file.Length != expectedLength || !actualSha256.Equals(expectedSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                label + " identity mismatch. expected=" + expectedLength + "/" + expectedSha256 +
                " actual=" + file.Length + "/" + actualSha256 + ".");
        }
    }

    private static string CreateAdvancedGameFixture(
        string temp,
        string referenceInputRoot,
        string label,
        string buildId,
        long expectedAssemblyLength = AdvancedAssemblyLength,
        string expectedAssemblySha256 = AdvancedAssemblySha256)
    {
        string steamApps = Path.Combine(temp, "advanced-steam-" + label, "steamapps");
        string gameRoot = Path.Combine(steamApps, "common", "Doloc Town");
        Directory.CreateDirectory(gameRoot);
        File.WriteAllText(Path.Combine(gameRoot, "DolocTown.exe"), "synthetic path marker", new UTF8Encoding(false));
        foreach (string relative in new[]
                 {
                     "BepInEx/core/0Harmony.dll",
                     "DolocTown_Data/Managed/Assembly-CSharp.dll"
                 })
        {
            string source = Path.Combine(referenceInputRoot, relative.Replace('/', Path.DirectorySeparatorChar));
            string destination = Path.Combine(gameRoot, relative.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(source, destination);
            if (relative.EndsWith("Assembly-CSharp.dll", StringComparison.Ordinal))
            {
                AssertExactAdvancedReferenceFile(
                    destination,
                    expectedAssemblyLength,
                    expectedAssemblySha256,
                    "Advanced game fixture Assembly-CSharp");
            }
            else
            {
                AssertExactAdvancedReferenceFile(
                    destination,
                    AdvancedHarmonyLength,
                    AdvancedHarmonySha256,
                    "Advanced game fixture Harmony");
            }
        }
        Directory.CreateDirectory(steamApps);
        File.WriteAllText(
            Path.Combine(steamApps, "appmanifest_2285550.acf"),
            "\"AppState\"\n{\n\t\"appid\"\t\t\"2285550\"\n\t\"buildid\"\t\t\"" + buildId + "\"\n}\n",
            new UTF8Encoding(false));
        return gameRoot;
    }

    private static string CopyPackage(string source, string destination)
    {
        File.Copy(source, destination, overwrite: true);
        return destination;
    }

    private static void MutateZipJson(string packagePath, string entryName, Action<JsonObject> mutate)
    {
        using ZipArchive archive = ZipFile.Open(packagePath, ZipArchiveMode.Update);
        ZipArchiveEntry entry = archive.GetEntry(entryName) ?? throw new InvalidOperationException("Missing ZIP entry " + entryName);
        string text;
        using (var reader = new StreamReader(entry.Open(), Encoding.UTF8))
            text = reader.ReadToEnd();
        JsonObject json = JsonNode.Parse(text)!.AsObject();
        mutate(json);
        entry.Delete();
        ZipArchiveEntry replacement = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        using var writer = new StreamWriter(replacement.Open(), new UTF8Encoding(false));
        writer.Write(JsonSerializer.Serialize(json, JsonOptions));
        writer.Write('\n');
    }

    private static void MutateAdvancedReceiptAndRefreshMarker(string packagePath, Func<string, string> mutate)
    {
        const string receiptName = "Content/DTMAPI/dtmapi-advanced-references.json";
        const string markerName = "Content/DTMAPI/dtmapi-package.json";
        using ZipArchive archive = ZipFile.Open(packagePath, ZipArchiveMode.Update);
        ZipArchiveEntry receiptEntry = archive.GetEntry(receiptName) ?? throw new InvalidOperationException("Missing ZIP entry " + receiptName);
        ZipArchiveEntry markerEntry = archive.GetEntry(markerName) ?? throw new InvalidOperationException("Missing ZIP entry " + markerName);
        string receiptText;
        string markerText;
        using (var reader = new StreamReader(receiptEntry.Open(), Encoding.UTF8))
            receiptText = reader.ReadToEnd();
        using (var reader = new StreamReader(markerEntry.Open(), Encoding.UTF8))
            markerText = reader.ReadToEnd();
        string mutated = mutate(receiptText);
        if (mutated.Equals(receiptText, StringComparison.Ordinal))
            throw new InvalidOperationException("Advanced receipt hostile mutation made no change.");
        byte[] receiptBytes = new UTF8Encoding(false).GetBytes(mutated);
        JsonObject marker = JsonNode.Parse(markerText)!.AsObject();
        marker["advancedReferenceReceiptSha256"] = Convert.ToHexString(SHA256.HashData(receiptBytes)).ToLowerInvariant();

        receiptEntry.Delete();
        markerEntry.Delete();
        ZipArchiveEntry receiptReplacement = archive.CreateEntry(receiptName, CompressionLevel.Optimal);
        using (Stream stream = receiptReplacement.Open())
            stream.Write(receiptBytes);
        ZipArchiveEntry markerReplacement = archive.CreateEntry(markerName, CompressionLevel.Optimal);
        using (var writer = new StreamWriter(markerReplacement.Open(), new UTF8Encoding(false)))
        {
            writer.Write(JsonSerializer.Serialize(marker, JsonOptions));
            writer.Write('\n');
        }
    }

    private static void MutateAdvancedManifestFloorAndRefreshBindings(string packagePath, string minimumDtmApiVersion)
    {
        const string manifestName = "Content/DTMAPI/manifest.json";
        const string receiptName = "Content/DTMAPI/dtmapi-advanced-references.json";
        const string markerName = "Content/DTMAPI/dtmapi-package.json";
        using ZipArchive archive = ZipFile.Open(packagePath, ZipArchiveMode.Update);
        ZipArchiveEntry manifestEntry = archive.GetEntry(manifestName) ?? throw new InvalidOperationException("Missing ZIP entry " + manifestName);
        ZipArchiveEntry receiptEntry = archive.GetEntry(receiptName) ?? throw new InvalidOperationException("Missing ZIP entry " + receiptName);
        ZipArchiveEntry markerEntry = archive.GetEntry(markerName) ?? throw new InvalidOperationException("Missing ZIP entry " + markerName);
        string manifestText;
        string receiptText;
        string markerText;
        using (var reader = new StreamReader(manifestEntry.Open(), Encoding.UTF8))
            manifestText = reader.ReadToEnd();
        using (var reader = new StreamReader(receiptEntry.Open(), Encoding.UTF8))
            receiptText = reader.ReadToEnd();
        using (var reader = new StreamReader(markerEntry.Open(), Encoding.UTF8))
            markerText = reader.ReadToEnd();

        JsonObject manifest = JsonNode.Parse(manifestText)!.AsObject();
        manifest["MinimumDTMApiVersion"] = minimumDtmApiVersion;
        byte[] manifestBytes = new UTF8Encoding(false).GetBytes(JsonSerializer.Serialize(manifest, JsonOptions) + "\n");
        string manifestSha256 = Convert.ToHexString(SHA256.HashData(manifestBytes)).ToLowerInvariant();

        JsonObject receipt = JsonNode.Parse(receiptText)!.AsObject();
        receipt["manifestSha256"] = manifestSha256;
        byte[] receiptBytes = new UTF8Encoding(false).GetBytes(JsonSerializer.Serialize(receipt, JsonOptions) + "\n");
        string receiptSha256 = Convert.ToHexString(SHA256.HashData(receiptBytes)).ToLowerInvariant();

        JsonObject marker = JsonNode.Parse(markerText)!.AsObject();
        marker["manifestSha256"] = manifestSha256;
        marker["advancedReferenceReceiptSha256"] = receiptSha256;
        byte[] markerBytes = new UTF8Encoding(false).GetBytes(JsonSerializer.Serialize(marker, JsonOptions) + "\n");

        manifestEntry.Delete();
        receiptEntry.Delete();
        markerEntry.Delete();
        WriteZipBytes(archive, manifestName, manifestBytes);
        WriteZipBytes(archive, receiptName, receiptBytes);
        WriteZipBytes(archive, markerName, markerBytes);
    }

    private static void WriteZipBytes(ZipArchive archive, string entryName, byte[] bytes)
    {
        ZipArchiveEntry replacement = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        using Stream stream = replacement.Open();
        stream.Write(bytes);
    }

    private static string ReplaceOnce(string value, string oldValue, string newValue)
    {
        int index = value.IndexOf(oldValue, StringComparison.Ordinal);
        if (index < 0)
            throw new InvalidOperationException("Expected hostile receipt mutation anchor was not found: " + oldValue);
        return value[..index] + newValue + value[(index + oldValue.Length)..];
    }

    private static void RemoveZipEntry(string packagePath, string entryName)
    {
        using ZipArchive archive = ZipFile.Open(packagePath, ZipArchiveMode.Update);
        (archive.GetEntry(entryName) ?? throw new InvalidOperationException("Missing ZIP entry " + entryName)).Delete();
    }

    private static void AddZipFile(string packagePath, string entryName, string sourcePath)
    {
        using ZipArchive archive = ZipFile.Open(packagePath, ZipArchiveMode.Update);
        ZipArchiveEntry entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        using Stream source = File.OpenRead(sourcePath);
        using Stream destination = entry.Open();
        source.CopyTo(destination);
    }

    private static void SynchronizeCurrentDeploymentInventory(string destination, string journalPath)
    {
        string receiptPath = Path.Combine(destination, ".dtmapi-author-receipt.json");
        DeploymentTreeInventory payload = CreateDeploymentInventory(destination, excludeReceipt: true);
        JsonObject receipt = JsonNode.Parse(File.ReadAllText(receiptPath, Encoding.UTF8))!.AsObject();
        receipt["payloadTreeSha256"] = payload.TreeSha256;
        receipt["payloadFileCount"] = payload.Files.Count;
        receipt["payloadDirectoryCount"] = payload.Directories.Count;
        WriteJson(receiptPath, receipt);

        DeploymentTreeInventory full = CreateDeploymentInventory(destination);
        JsonObject journal = JsonNode.Parse(File.ReadAllText(journalPath, Encoding.UTF8))!.AsObject();
        JsonObject committed = journal["committed"]!.AsObject();
        committed["payloadTreeSha256"] = payload.TreeSha256;
        committed["receiptSha256"] = Sha256(receiptPath);
        committed["inventory"] = JsonSerializer.SerializeToNode(full, JsonOptions);
        WriteJson(journalPath, journal);
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (string directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, directory)));
        foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            string target = Path.Combine(destination, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
    }

    private static string ComputeGameRootKey(string gameRoot)
    {
        string canonical = Path.GetFullPath(gameRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).ToUpperInvariant();
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(hash.AsSpan(0, 16)).ToLowerInvariant();
    }

    private static bool PathsEqual(string left, string right) => string.Equals(Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
    private static string SafeLabel(string value) => new(value.Select(character => char.IsLetterOrDigit(character) ? character : '-').ToArray());

    private static async Task RunExternal(string executable, string isolated, params string[] arguments)
    {
        var start = new ProcessStartInfo(executable)
        {
            WorkingDirectory = isolated,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (string argument in arguments)
            start.ArgumentList.Add(argument);
        start.Environment["PATH"] = string.Empty;
        start.Environment["DOTNET_ROOT"] = string.Empty;
        start.Environment["DOTNET_ROOT_X64"] = string.Empty;
        start.Environment["NUGET_PACKAGES"] = Path.Combine(isolated, "empty-nuget");
        start.Environment["USERPROFILE"] = isolated;
        using Process process = Process.Start(start) ?? throw new InvalidOperationException("Failed to start self-contained CLI.");
        string output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
        string error = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
        await process.WaitForExitAsync().ConfigureAwait(false);
        if (process.ExitCode != 0)
            throw new InvalidOperationException("Self-contained CLI failed with empty PATH. exit=" + process.ExitCode + " stdout=" + output + " stderr=" + error);
    }

    private static string CreateCompatibilityPayload(string repository, string temp)
    {
        string root = Path.Combine(temp, "compatibility", "0.5.5");
        string? selfContainedExecutable = Environment.GetEnvironmentVariable("DTMAPI_AUTHOR_SELF_CONTAINED_EXE");
        var stagedCompatibilityRoots = new List<string>();
        if (!string.IsNullOrWhiteSpace(selfContainedExecutable))
        {
            stagedCompatibilityRoots.Add(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(selfContainedExecutable))!, "compatibility", "0.5.5"));
        }
        stagedCompatibilityRoots.Add(Path.Combine(
            repository,
            "dist",
            "author-sdk",
            "DTMAPI-Author-SDK-0.1.0-win-x64",
            "compatibility",
            "0.5.5"));
        foreach (string staged in stagedCompatibilityRoots.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (File.Exists(Path.Combine(staged, "compatibility.json"))
                && File.Exists(Path.Combine(staged, "DTMAPI.Abstractions.dll"))
                && Directory.Exists(Path.Combine(staged, "ref", "netstandard2.0")))
            {
                CopyDirectory(staged, root);
                return root;
            }
        }

        string referenceSource = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages", "netstandard.library", "2.0.3", "build", "netstandard2.0", "ref");
        if (!Directory.Exists(referenceSource))
            throw new InvalidOperationException("Focused SDK tests require the pinned NETStandard.Library 2.0.3 package in the test host cache.");
        string referenceTarget = Path.Combine(root, "ref", "netstandard2.0");
        Directory.CreateDirectory(referenceTarget);
        foreach (string source in Directory.EnumerateFiles(referenceSource, "*", SearchOption.TopDirectoryOnly).OrderBy(path => path, StringComparer.Ordinal))
            File.Copy(source, Path.Combine(referenceTarget, Path.GetFileName(source)));

        string abstractions = Path.Combine(repository, "src", "DTMAPI.Abstractions", "bin", "Release", "netstandard2.0", "DTMAPI.Abstractions.dll");
        if (!File.Exists(abstractions))
            throw new InvalidOperationException("Build DTMAPI.Abstractions Release before the Author SDK focused tests.");
        JsonObject contract = JsonNode.Parse(File.ReadAllText(Path.Combine(repository, "author-sdk", "compatibility", "0.5.5", "compatibility.contract.json")))!.AsObject();
        string expectedAbstractionsHash = contract["abstractionsSha256"]!.GetValue<string>();
        if (!Sha256(abstractions).Equals(expectedAbstractionsHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Focused SDK tests require the frozen Release PathMap DTMAPI.Abstractions.dll. "
                + "Run tools/scripts/build-author-sdk.ps1 first or provide its staged self-contained CLI through DTMAPI_AUTHOR_SELF_CONTAINED_EXE.");
        }
        File.Copy(abstractions, Path.Combine(root, "DTMAPI.Abstractions.dll"));
        File.Copy(Path.Combine(repository, "author-sdk", "compatibility", "0.5.5", "DTMAPI.Author.props"), Path.Combine(root, "DTMAPI.Author.props"));
        string packageRoot = Directory.GetParent(Directory.GetParent(Directory.GetParent(referenceSource)!.FullName)!.FullName)!.FullName;
        File.Copy(Path.Combine(packageRoot, "LICENSE.TXT"), Path.Combine(root, "NETStandard.Library.LICENSE.TXT"));
        File.Copy(Path.Combine(packageRoot, "THIRD-PARTY-NOTICES.TXT"), Path.Combine(root, "NETStandard.Library.THIRD-PARTY-NOTICES.TXT"));

        var files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .OrderBy(path => Path.GetRelativePath(root, path).Replace('\\', '/'), StringComparer.Ordinal)
            .Select(path => new CompatibilityFile
            {
                Path = Path.GetRelativePath(root, path).Replace('\\', '/'),
                Sha256 = Sha256(path),
                Kind = Path.GetFileName(path).Equals("DTMAPI.Abstractions.dll", StringComparison.OrdinalIgnoreCase) ? "abstractions"
                    : Path.GetRelativePath(root, path).Replace('\\', '/').StartsWith("ref/netstandard2.0/", StringComparison.Ordinal) ? "reference"
                    : path.EndsWith(".props", StringComparison.OrdinalIgnoreCase) ? "props"
                    : path.Contains("LICENSE", StringComparison.OrdinalIgnoreCase) ? "license" : "notice"
            })
            .ToList();
        var manifest = new CompatibilityManifest
        {
            SchemaVersion = 1,
            SdkVersion = "0.1.0",
            TargetRuntimeVersion = "0.5.5",
            AbstractionsAssemblyVersion = "0.5.3.0",
            AbstractionsFileVersion = "0.5.5.0",
            Files = files
        };
        WriteJson(Path.Combine(root, "compatibility.json"), manifest);
        return root;
    }

    private static async Task<CommandReport> ExpectSuccess(string label, params string[] args)
    {
        (int exitCode, CommandReport report, string error) = await Run(args).ConfigureAwait(false);
        if (exitCode != 0 || !report.Success)
            throw new InvalidOperationException(label + " expected success: " + error + JsonSerializer.Serialize(report, JsonOptions));
        return report;
    }

    private static async Task<CommandReport> ExpectFailure(string label, params string[] args)
    {
        (int exitCode, CommandReport report, _) = await Run(args).ConfigureAwait(false);
        if (exitCode == 0 || report.Success)
            throw new InvalidOperationException(label + " expected failure.");
        return report;
    }

    private static async Task<CommandReport> ExpectPublicFailure(string label, params string[] args)
    {
        (int exitCode, CommandReport report, _) = await RunPublic(args).ConfigureAwait(false);
        if (exitCode == 0 || report.Success)
            throw new InvalidOperationException(label + " expected public CLI failure.");
        return report;
    }

    private static async Task<(int ExitCode, CommandReport Report, string Error)> Run(params string[] args)
    {
        if (IsLegacyMutationCompatibilityFixture(args))
            return RunLegacyMutationCompatibilityFixture(args);

        return await RunPublic(args).ConfigureAwait(false);
    }

    private static bool IsLegacyMutationCompatibilityFixture(IReadOnlyList<string> args)
    {
        if (args.Count == 0)
            return false;
        if (args[0] is "deploy" or "update" or "install-local")
            return true;
        return args[0] == "source" &&
               args.Count >= 3 &&
               args[1].Equals("local", StringComparison.OrdinalIgnoreCase) &&
               args[2].Equals("select", StringComparison.OrdinalIgnoreCase);
    }

    private static (int ExitCode, CommandReport Report, string Error) RunLegacyMutationCompatibilityFixture(string[] args)
    {
        try
        {
            ParsedCommand command = ParsedCommand.Parse(args.Append("--json").ToArray());
            CommandReport report = command.Name switch
            {
                "deploy" => DeploymentService.Deploy(command),
                "update" => DeploymentService.Update(command),
                "install-local" => DeploymentService.InstallLocal(command),
                "source" => SourceStateService.Execute(command),
                _ => throw new InvalidOperationException("Unsupported legacy compatibility fixture command: " + command.Name)
            };
            return (report.Success ? 0 : 1, report, string.Empty);
        }
        catch (CommandLineException ex)
        {
            var report = new CommandReport { Command = "usage", Success = false };
            report.Diagnostics.Add(new AuthorDiagnostic
            {
                Code = "SDK001",
                Severity = DiagnosticSeverity.Error,
                Message = ex.Message
            });
            return (2, report, ex.Message);
        }
    }

    private static async Task<(int ExitCode, CommandReport Report, string Error)> RunPublic(params string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        int exitCode = await AuthorApplication.RunAsync(args.Append("--json").ToArray(), output, error).ConfigureAwait(false);
        CommandReport report = JsonSerializer.Deserialize<CommandReport>(output.ToString(), JsonOptions)
            ?? throw new InvalidOperationException("CLI did not return a JSON report. stderr=" + error);
        return (exitCode, report, error.ToString());
    }

    private static string ReadZipText(ZipArchive archive, string name)
    {
        ZipArchiveEntry entry = archive.GetEntry(name) ?? throw new InvalidOperationException("Missing ZIP entry " + name);
        using StreamReader reader = new(entry.Open(), Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static void WriteJson<T>(string path, T value) => File.WriteAllText(path, JsonSerializer.Serialize(value, JsonOptions) + "\n", new UTF8Encoding(false));

    private static string Sha256(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static string FindRepository()
    {
        DirectoryInfo? directory = new(Environment.CurrentDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "PROJECT.md")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
    }

    private static void AssertJsonMatchesSchema(JsonElement instance, string schemaPath, string label = "JSON instance")
    {
        using JsonDocument schemaDocument = JsonDocument.Parse(File.ReadAllText(schemaPath, Encoding.UTF8));
        var errors = new List<string>();
        ValidateSchemaNode(schemaDocument.RootElement, schemaDocument.RootElement, instance, "$", errors);
        True(errors.Count == 0,
            label + " must validate against " + Path.GetFileName(schemaPath) + ": " + string.Join(" | ", errors));
    }

    private static void ValidateSchemaNode(
        JsonElement schemaRoot,
        JsonElement schema,
        JsonElement instance,
        string instancePath,
        ICollection<string> errors)
    {
        if (schema.TryGetProperty("$ref", out JsonElement reference) && reference.ValueKind == JsonValueKind.String)
        {
            if (!TryResolveLocalSchemaReference(schemaRoot, reference.GetString() ?? string.Empty, out JsonElement resolved))
            {
                errors.Add(instancePath + " has an unresolved schema reference " + reference.GetString());
                return;
            }
            ValidateSchemaNode(schemaRoot, resolved, instance, instancePath, errors);
            return;
        }

        if (schema.TryGetProperty("oneOf", out JsonElement oneOf) && oneOf.ValueKind == JsonValueKind.Array)
        {
            int matches = 0;
            foreach (JsonElement candidate in oneOf.EnumerateArray())
            {
                var candidateErrors = new List<string>();
                ValidateSchemaNode(schemaRoot, candidate, instance, instancePath, candidateErrors);
                if (candidateErrors.Count == 0)
                    matches++;
            }
            if (matches != 1)
                errors.Add(instancePath + " matches " + matches + " oneOf branches instead of exactly one");
            return;
        }

        if (schema.TryGetProperty("type", out JsonElement type) && !MatchesAnySchemaType(instance, type))
        {
            errors.Add(instancePath + " has JSON type " + instance.ValueKind + ", expected " + type.GetRawText());
            return;
        }

        if (schema.TryGetProperty("const", out JsonElement constant) && !JsonScalarEquals(instance, constant))
            errors.Add(instancePath + " does not equal const " + constant.GetRawText());

        if (schema.TryGetProperty("enum", out JsonElement allowed) &&
            allowed.ValueKind == JsonValueKind.Array &&
            !allowed.EnumerateArray().Any(candidate => JsonScalarEquals(instance, candidate)))
            errors.Add(instancePath + " is not one of " + allowed.GetRawText());

        if (instance.ValueKind == JsonValueKind.Object)
        {
            if (schema.TryGetProperty("required", out JsonElement required) && required.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement nameElement in required.EnumerateArray())
                {
                    string name = nameElement.GetString() ?? string.Empty;
                    if (!instance.TryGetProperty(name, out _))
                        errors.Add(instancePath + " is missing required property '" + name + "'");
                }
            }

            if (schema.TryGetProperty("properties", out JsonElement properties) && properties.ValueKind == JsonValueKind.Object)
            {
                if (schema.TryGetProperty("additionalProperties", out JsonElement additionalProperties) &&
                    additionalProperties.ValueKind == JsonValueKind.False)
                {
                    foreach (JsonProperty instanceProperty in instance.EnumerateObject())
                    {
                        if (!properties.TryGetProperty(instanceProperty.Name, out _))
                            errors.Add(instancePath + " contains additional property '" + instanceProperty.Name + "'");
                    }
                }
                foreach (JsonProperty propertySchema in properties.EnumerateObject())
                {
                    if (instance.TryGetProperty(propertySchema.Name, out JsonElement propertyValue))
                        ValidateSchemaNode(schemaRoot, propertySchema.Value, propertyValue, instancePath + "." + propertySchema.Name, errors);
                }
            }
        }

        if (instance.ValueKind == JsonValueKind.Array &&
            schema.TryGetProperty("items", out JsonElement itemSchema))
        {
            int index = 0;
            foreach (JsonElement item in instance.EnumerateArray())
            {
                ValidateSchemaNode(schemaRoot, itemSchema, item, instancePath + "[" + index + "]", errors);
                index++;
            }
        }

        if (instance.ValueKind == JsonValueKind.String &&
            schema.TryGetProperty("minLength", out JsonElement minimumLength) &&
            (instance.GetString() ?? string.Empty).Length < minimumLength.GetInt32())
            errors.Add(instancePath + " is shorter than minLength=" + minimumLength.GetInt32());

        if (instance.ValueKind == JsonValueKind.String &&
            schema.TryGetProperty("pattern", out JsonElement pattern) &&
            !System.Text.RegularExpressions.Regex.IsMatch(instance.GetString() ?? string.Empty, pattern.GetString() ?? string.Empty))
            errors.Add(instancePath + " does not match pattern " + pattern.GetString());

        if (instance.ValueKind == JsonValueKind.Number &&
            schema.TryGetProperty("minimum", out JsonElement minimum) &&
            instance.GetDecimal() < minimum.GetDecimal())
            errors.Add(instancePath + " is below minimum=" + minimum.GetRawText());
    }

    private static bool TryResolveLocalSchemaReference(JsonElement root, string reference, out JsonElement resolved)
    {
        resolved = root;
        if (!reference.StartsWith("#/", StringComparison.Ordinal))
            return false;
        foreach (string encodedSegment in reference.Substring(2).Split('/'))
        {
            string segment = encodedSegment.Replace("~1", "/", StringComparison.Ordinal).Replace("~0", "~", StringComparison.Ordinal);
            if (resolved.ValueKind != JsonValueKind.Object || !resolved.TryGetProperty(segment, out JsonElement next))
                return false;
            resolved = next;
        }
        return true;
    }

    private static bool MatchesAnySchemaType(JsonElement instance, JsonElement type)
    {
        if (type.ValueKind == JsonValueKind.String)
            return MatchesSchemaType(instance, type.GetString() ?? string.Empty);
        return type.ValueKind == JsonValueKind.Array &&
               type.EnumerateArray().Any(value => value.ValueKind == JsonValueKind.String && MatchesSchemaType(instance, value.GetString() ?? string.Empty));
    }

    private static bool MatchesSchemaType(JsonElement instance, string type) => type switch
    {
        "object" => instance.ValueKind == JsonValueKind.Object,
        "array" => instance.ValueKind == JsonValueKind.Array,
        "string" => instance.ValueKind == JsonValueKind.String,
        "integer" => instance.ValueKind == JsonValueKind.Number && instance.TryGetInt64(out _),
        "number" => instance.ValueKind == JsonValueKind.Number,
        "boolean" => instance.ValueKind is JsonValueKind.True or JsonValueKind.False,
        "null" => instance.ValueKind == JsonValueKind.Null,
        _ => false
    };

    private static bool JsonScalarEquals(JsonElement left, JsonElement right)
    {
        if (left.ValueKind != right.ValueKind)
            return false;
        return left.ValueKind switch
        {
            JsonValueKind.String => left.GetString() == right.GetString(),
            JsonValueKind.Number => left.GetDecimal() == right.GetDecimal(),
            JsonValueKind.True or JsonValueKind.False => left.GetBoolean() == right.GetBoolean(),
            JsonValueKind.Null => true,
            _ => left.GetRawText() == right.GetRawText()
        };
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    private static void HasCode(CommandReport report, string code, string label) => True(report.Diagnostics.Any(diagnostic => diagnostic.Code == code), label + " missing " + code);
    private static void Contains(IEnumerable<string> values, string expected, string label) => True(values.Contains(expected, StringComparer.Ordinal), label);
    private static void Equal(string expected, string actual, string label) => True(string.Equals(expected, actual, StringComparison.Ordinal), label + ": expected=" + expected + " actual=" + actual);
    private static void True(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private sealed record DeploymentPackages(string ContentV1, string ContentV2, string CodeV2, string Other);

    private sealed class LegacyDeploymentState
    {
        public required string GameRoot { get; init; }
        public required string JournalPath { get; init; }
        public required string DestinationPath { get; init; }
        public required string UniqueId { get; init; }
        public required string PackageKind { get; init; }
        public required string TransactionId { get; init; }
        public required string GameRootKey { get; init; }
        public required string PackageSha256 { get; init; }
        public DeploymentRecord Record { get; set; } = new();
    }
}
