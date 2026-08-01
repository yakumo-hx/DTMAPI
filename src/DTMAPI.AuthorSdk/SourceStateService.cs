using DTMAPI.Authoring.Contracts;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DTMAPI.AuthorSdk;

internal static class SourceStateService
{
    private static readonly Regex UniqueIdPattern = new(@"^[A-Za-z][A-Za-z0-9]*(?:\.[A-Za-z0-9][A-Za-z0-9_-]*)+$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    public static CommandReport Execute(ParsedCommand command)
    {
        command.RequireOnlyOptions("game-root");
        if (command.Positionals.Count == 0)
            throw new CommandLineException("source requires status, local, workshop, or reproduction.");
        string gameRoot = RequireGameRoot(command);
        string action = string.Join(" ", command.Positionals.Take(2)).ToLowerInvariant();
        var report = new CommandReport { Command = "source", RootPath = gameRoot };
        try
        {
            using GameOperationLock operationLock = GameOperationLock.Acquire(gameRoot);
            if (command.Positionals[0].Equals("status", StringComparison.OrdinalIgnoreCase))
                Status(command, gameRoot, Load(gameRoot), report);
            else if (action == "local select")
            {
                DeploymentService.AssertNoPendingLocalInstallForSourceMutation(gameRoot);
                LocalSelect(command, gameRoot, Load(gameRoot), report);
            }
            else if (action == "local clear")
            {
                DeploymentService.AssertNoPendingLocalInstallForSourceMutation(gameRoot);
                LocalClear(command, gameRoot, Load(gameRoot), report);
            }
            else if (action == "workshop prepare")
            {
                DeploymentService.AssertNoPendingLocalInstallForSourceMutation(gameRoot);
                WorkshopPrepare(command, gameRoot, Load(gameRoot), report);
            }
            else if (action == "workshop clear")
            {
                DeploymentService.AssertNoPendingLocalInstallForSourceMutation(gameRoot);
                WorkshopClear(command, gameRoot, Load(gameRoot), report);
            }
            else if (action == "reproduction begin")
            {
                DeploymentService.AssertNoPendingLocalInstallForSourceMutation(gameRoot);
                ReproductionBegin(command, gameRoot, Load(gameRoot), report);
            }
            else if (action == "reproduction restore")
            {
                DeploymentService.AssertNoPendingLocalInstallForSourceMutation(gameRoot);
                ReproductionRestore(command, gameRoot, Load(gameRoot), report);
            }
            else
                throw new CommandLineException("Unknown source operation. Use status, local select/clear, workshop prepare/clear, or reproduction begin/restore.");
        }
        catch (CommandLineException)
        {
            throw;
        }
        catch (Exception ex)
        {
            report.Diagnostics.Add(Error("SDK500", ex.Message, AuthorStatePaths.SourceStatePath(gameRoot)));
        }
        report.Success = report.Diagnostics.All(diagnostic => diagnostic.Severity != DiagnosticSeverity.Error);
        return report;
    }

    internal static AuthorSourceState PrepareLocalInstall(string gameRoot)
    {
        AuthorSourceState state = Load(gameRoot);
        RefuseDuringReproduction(state);
        return state;
    }

    internal static void ValidateLocalInstallUniqueId(string uniqueId) => ValidateUniqueId(uniqueId);

    internal static string CommitLocalInstall(
        string gameRoot,
        AuthorSourceState state,
        string uniqueId,
        string sourcePath)
    {
        ValidateUniqueId(uniqueId);
        string fullSourcePath = Path.GetFullPath(sourcePath);
        PathSafety.RejectBepInExPluginDestination(fullSourcePath);
        string modsRoot = Path.Combine(gameRoot, "Mods");
        if (!Directory.Exists(fullSourcePath) ||
            !string.Equals(Path.GetDirectoryName(fullSourcePath), modsRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Local Development source must be one immediate package directory under game/Mods.");
        }

        RuntimeManifest manifest = ReadSourceManifest(fullSourcePath);
        if (!manifest.UniqueID.Equals(uniqueId, StringComparison.Ordinal))
            throw new InvalidDataException("Selected local source manifest UniqueID does not exactly match the requested UniqueID.");

        string digest = AuthorFileTreeDigest.Compute(fullSourcePath);
        Upsert(state, new AuthorSourceSelection
        {
            UniqueId = uniqueId,
            Mode = "LocalDevelopment",
            SourcePath = fullSourcePath,
            ExpectedTreeSha256 = digest
        });
        WriteState(gameRoot, state);
        return digest;
    }

    internal static string VerifyLocalInstall(
        string gameRoot,
        string uniqueId,
        string sourcePath)
    {
        AuthorSourceState state = Load(gameRoot);
        RefuseDuringReproduction(state);
        AuthorSourceSelection selection = state.Selections.SingleOrDefault(
            row => row.UniqueId.Equals(uniqueId, StringComparison.Ordinal))
            ?? throw new InvalidDataException("No exact Local Development source selection exists for " + uniqueId + ".");
        string expectedPath = Path.GetFullPath(sourcePath);
        if (!selection.Mode.Equals("LocalDevelopment", StringComparison.Ordinal) ||
            !PathsEqual(selection.SourcePath, expectedPath) ||
            !Directory.Exists(expectedPath))
        {
            throw new InvalidDataException("Local Development source selection does not match the committed managed destination.");
        }
        string digest = AuthorFileTreeDigest.Compute(expectedPath);
        if (!digest.Equals(selection.ExpectedTreeSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Local Development source selection tree has drifted from its committed digest.");
        return digest;
    }

    private static void LocalSelect(ParsedCommand command, string gameRoot, AuthorSourceState state, CommandReport report)
    {
        RequirePositionals(command, 4, "source local select requires UniqueID and source path.");
        RefuseDuringReproduction(state);
        string uniqueId = command.Positionals[2];
        ValidateUniqueId(uniqueId);
        string sourcePath = Path.GetFullPath(command.Positionals[3]);
        PathSafety.RejectBepInExPluginDestination(sourcePath);
        string modsRoot = Path.Combine(gameRoot, "Mods");
        if (!Directory.Exists(sourcePath) || !string.Equals(Path.GetDirectoryName(sourcePath), modsRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Local Development source must be one immediate package directory under game/Mods.");
        RuntimeManifest manifest = ReadSourceManifest(sourcePath);
        if (!manifest.UniqueID.Equals(uniqueId, StringComparison.Ordinal))
            throw new InvalidDataException("Selected local source manifest UniqueID does not exactly match the requested UniqueID.");
        string digest = AuthorFileTreeDigest.Compute(sourcePath);
        Upsert(state, new AuthorSourceSelection
        {
            UniqueId = uniqueId,
            Mode = "LocalDevelopment",
            SourcePath = sourcePath,
            ExpectedTreeSha256 = digest
        });
        WriteState(gameRoot, state);
        report.Values["mode"] = "LocalDevelopment";
        report.Values["uniqueID"] = uniqueId;
        report.Values["sourcePath"] = sourcePath;
        report.Values["expectedTreeSha256"] = digest;
        report.Values["treeDigestAlgorithm"] = AuthorFileTreeDigest.AlgorithmId;
        report.Diagnostics.Add(Info("SDK500", "Selected one exact Local Development source. A loaded CodeMod still requires restart."));
    }

    private static void LocalClear(ParsedCommand command, string gameRoot, AuthorSourceState state, CommandReport report)
    {
        RequirePositionals(command, 3, "source local clear requires UniqueID.");
        RefuseDuringReproduction(state);
        string uniqueId = command.Positionals[2];
        ValidateUniqueId(uniqueId);
        AuthorSourceSelection selection = state.Selections.FirstOrDefault(row => row.UniqueId.Equals(uniqueId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidDataException("No source selection exists for " + uniqueId + ".");
        if (!selection.Mode.Equals("LocalDevelopment", StringComparison.Ordinal))
            throw new InvalidDataException("The current selection is not Local Development; use its matching clear operation.");
        state.Selections.Remove(selection);
        WriteState(gameRoot, state);
        report.Values["mode"] = "PlayerWorkshop";
        report.Values["uniqueID"] = uniqueId;
        report.Diagnostics.Add(Info("SDK500", "Cleared the Local Development override; no package was moved or deleted."));
    }

    private static void WorkshopPrepare(ParsedCommand command, string gameRoot, AuthorSourceState state, CommandReport report)
    {
        RequirePositionals(command, 3, "source workshop prepare requires UniqueID.");
        RefuseDuringReproduction(state);
        string uniqueId = command.Positionals[2];
        ValidateUniqueId(uniqueId);
        Upsert(state, new AuthorSourceSelection
        {
            UniqueId = uniqueId,
            Mode = "WorkshopValidation",
            SourcePath = string.Empty,
            ExpectedTreeSha256 = string.Empty
        });
        WriteState(gameRoot, state);
        report.Values["mode"] = "WorkshopValidation";
        report.Values["uniqueID"] = uniqueId;
        AddWorkshopSnapshotReport(gameRoot, report);
        report.Values["nativeValidation"] = "not-proven-offline-runtime-must-verify";
        report.Diagnostics.Add(Warning("SDK501", "Workshop Validation was prepared, but the SDK does not treat directory presence as native subscription proof. Runtime must validate its ModManager snapshot.", AuthorStatePaths.WorkshopSnapshotPath(gameRoot)));
    }

    private static void WorkshopClear(ParsedCommand command, string gameRoot, AuthorSourceState state, CommandReport report)
    {
        RequirePositionals(command, 3, "source workshop clear requires UniqueID.");
        RefuseDuringReproduction(state);
        string uniqueId = command.Positionals[2];
        ValidateUniqueId(uniqueId);
        AuthorSourceSelection selection = state.Selections.FirstOrDefault(row => row.UniqueId.Equals(uniqueId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidDataException("No source selection exists for " + uniqueId + ".");
        if (!selection.Mode.Equals("WorkshopValidation", StringComparison.Ordinal))
            throw new InvalidDataException("The current selection is not Workshop Validation.");
        state.Selections.Remove(selection);
        WriteState(gameRoot, state);
        report.Values["mode"] = "PlayerWorkshop";
        report.Values["uniqueID"] = uniqueId;
        report.Diagnostics.Add(Info("SDK500", "Cleared the Workshop Validation preparation."));
    }

    private static void ReproductionBegin(ParsedCommand command, string gameRoot, AuthorSourceState state, CommandReport report)
    {
        RequirePositionals(command, 2, "source reproduction begin takes no additional arguments.");
        if (state.PlayerReproductionActive)
            throw new InvalidDataException("Player Reproduction is already active; restore its matching snapshot first.");
        string snapshotId = Guid.NewGuid().ToString("N");
        var snapshot = new AuthorSourceSnapshot
        {
            SchemaVersion = AuthorStatePaths.SchemaVersion,
            SnapshotId = snapshotId,
            GameRoot = gameRoot,
            Selections = CloneSelections(state.Selections)
        };
        string snapshotPath = AuthorStatePaths.SourceSnapshotPath(gameRoot, snapshotId);
        if (File.Exists(snapshotPath))
            throw new InvalidDataException("Source snapshot path unexpectedly exists.");
        AtomicStateFile.Write(snapshotPath, snapshot);
        state.PlayerReproductionActive = true;
        state.ReproductionSnapshotId = snapshotId;
        state.Selections.Clear();
        WriteState(gameRoot, state);
        report.Values["mode"] = "PlayerReproduction";
        report.Values["snapshotId"] = snapshotId;
        report.Values["snapshotPath"] = snapshotPath;
        report.Diagnostics.Add(Info("SDK500", "Snapshotted and atomically cleared every override for Player Reproduction. New selections are blocked until explicit matching restore."));
    }

    private static void ReproductionRestore(ParsedCommand command, string gameRoot, AuthorSourceState state, CommandReport report)
    {
        RequirePositionals(command, 3, "source reproduction restore requires the exact snapshot ID.");
        string snapshotId = command.Positionals[2];
        if (!state.PlayerReproductionActive || !state.ReproductionSnapshotId.Equals(snapshotId, StringComparison.Ordinal) || state.Selections.Count != 0)
            throw new InvalidDataException("Player Reproduction state does not match this snapshot or overrides appeared while reproduction was active.");
        string snapshotPath = AuthorStatePaths.SourceSnapshotPath(gameRoot, snapshotId);
        if (!File.Exists(snapshotPath))
            throw new InvalidDataException("Matching Player Reproduction snapshot file is missing.");
        AuthorSourceSnapshot snapshot = AtomicStateFile.ReadStrict<AuthorSourceSnapshot>(snapshotPath);
        if (snapshot.SchemaVersion != AuthorStatePaths.SchemaVersion || !snapshot.SnapshotId.Equals(snapshotId, StringComparison.Ordinal) || !PathsEqual(snapshot.GameRoot, gameRoot))
            throw new InvalidDataException("Player Reproduction snapshot identity/root is invalid.");
        ValidateSelections(snapshot.Selections);
        state.Selections = CloneSelections(snapshot.Selections);
        state.PlayerReproductionActive = false;
        state.ReproductionSnapshotId = string.Empty;
        WriteState(gameRoot, state);
        report.Values["mode"] = "PlayerWorkshop";
        report.Values["snapshotId"] = snapshotId;
        report.Values["restoredSelections"] = state.Selections.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        report.Diagnostics.Add(Info("SDK500", "Explicitly restored the exact matching Player Reproduction snapshot."));
    }

    private static void Status(ParsedCommand command, string gameRoot, AuthorSourceState state, CommandReport report)
    {
        if (command.Positionals.Count > 2)
            throw new CommandLineException("source status accepts at most one UniqueID.");
        string uniqueId = command.Positionals.Count == 2 ? command.Positionals[1] : string.Empty;
        if (uniqueId.Length > 0)
            ValidateUniqueId(uniqueId);
        report.Values["statePath"] = AuthorStatePaths.SourceStatePath(gameRoot);
        report.Values["playerReproductionActive"] = state.PlayerReproductionActive.ToString().ToLowerInvariant();
        report.Values["snapshotId"] = state.ReproductionSnapshotId;
        report.Values["selectionCount"] = state.Selections.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        report.Values["treeDigestAlgorithm"] = AuthorFileTreeDigest.AlgorithmId;
        if (uniqueId.Length > 0)
        {
            AuthorSourceSelection? selection = state.Selections.FirstOrDefault(row => row.UniqueId.Equals(uniqueId, StringComparison.OrdinalIgnoreCase));
            string mode = state.PlayerReproductionActive ? "PlayerReproduction" : selection?.Mode ?? "PlayerWorkshop";
            report.Values["mode"] = mode;
            report.Values["uniqueID"] = uniqueId;
            if (selection != null)
            {
                report.Values["sourcePath"] = selection.SourcePath;
                report.Values["expectedTreeSha256"] = selection.ExpectedTreeSha256;
                if (selection.Mode.Equals("LocalDevelopment", StringComparison.Ordinal))
                {
                    if (!Directory.Exists(selection.SourcePath) || !AuthorFileTreeDigest.Compute(selection.SourcePath).Equals(selection.ExpectedTreeSha256, StringComparison.OrdinalIgnoreCase))
                        report.Diagnostics.Add(Error("SDK502", "Local Development source tree is missing or drifted from expectedTreeSha256.", selection.SourcePath));
                }
                if (selection.Mode.Equals("WorkshopValidation", StringComparison.Ordinal))
                {
                    AddWorkshopSnapshotReport(gameRoot, report);
                    report.Values["nativeValidation"] = "not-proven-offline-runtime-must-verify";
                    report.Diagnostics.Add(Warning("SDK501", "Workshop Validation remains pending Runtime native-snapshot verification.", AuthorStatePaths.WorkshopSnapshotPath(gameRoot)));
                }
            }
        }
        report.Diagnostics.Insert(0, Info("SDK500", "Read package-external source state; override state grants no file ownership authority."));
    }

    private static AuthorSourceState Load(string gameRoot)
    {
        string path = AuthorStatePaths.SourceStatePath(gameRoot);
        if (!File.Exists(path))
            return new AuthorSourceState { SchemaVersion = AuthorStatePaths.SchemaVersion, GameRoot = gameRoot };
        AuthorSourceState state = AtomicStateFile.ReadStrict<AuthorSourceState>(path);
        ValidateState(state, gameRoot);
        return state;
    }

    internal static AuthorSourceState ValidateExactSnapshot(string gameRoot, byte[] bytes)
    {
        if (bytes.Length == 0)
            throw new InvalidDataException("Existing source-state snapshot cannot be empty.");

        string text;
        try
        {
            text = new UTF8Encoding(false, true).GetString(bytes);
        }
        catch (DecoderFallbackException ex)
        {
            throw new InvalidDataException("source-state snapshot is not strict UTF-8.", ex);
        }

        using JsonDocument document = JsonDocument.Parse(text, new JsonDocumentOptions
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow
        });
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("source-state snapshot must contain one JSON object.");
        RequireExactProperties(
            document.RootElement,
            new[] { "gameRoot", "playerReproductionActive", "reproductionSnapshotId", "schemaVersion", "selections" },
            "source-state snapshot");
        JsonElement selections = document.RootElement.GetProperty("selections");
        if (selections.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("source-state snapshot selections must be an array.");
        foreach (JsonElement selection in selections.EnumerateArray())
        {
            if (selection.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("source-state snapshot selection must be one JSON object.");
            RequireExactProperties(
                selection,
                new[] { "expectedTreeSha256", "mode", "sourcePath", "uniqueId" },
                "source-state snapshot selection");
        }

        AuthorSourceState state = JsonSerializer.Deserialize<AuthorSourceState>(text, JsonSupport.Tool)
            ?? throw new InvalidDataException("source-state snapshot must contain one JSON object.");
        ValidateState(state, gameRoot);
        return state;
    }

    internal static byte[] BuildCommittedLocalInstallState(
        string gameRoot,
        bool sourceBeforeExisted,
        byte[] sourceBeforeBytes,
        string uniqueId,
        string sourcePath,
        string expectedTreeSha256)
    {
        ValidateUniqueId(uniqueId);
        if (expectedTreeSha256.Length != 64 ||
            expectedTreeSha256.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new InvalidDataException("Committed local-install source tree digest is invalid.");
        }

        AuthorSourceState state = sourceBeforeExisted
            ? ValidateExactSnapshot(gameRoot, sourceBeforeBytes)
            : new AuthorSourceState
            {
                SchemaVersion = AuthorStatePaths.SchemaVersion,
                GameRoot = gameRoot
            };
        RefuseDuringReproduction(state);
        Upsert(state, new AuthorSourceSelection
        {
            UniqueId = uniqueId,
            Mode = "LocalDevelopment",
            SourcePath = Path.GetFullPath(sourcePath),
            ExpectedTreeSha256 = expectedTreeSha256
        });
        return SerializeState(gameRoot, state);
    }

    private static void ValidateState(AuthorSourceState state, string gameRoot)
    {
        if (state.SchemaVersion != AuthorStatePaths.SchemaVersion || !PathsEqual(state.GameRoot, gameRoot))
            throw new InvalidDataException("source-state.json schema/gameRoot does not match this installation.");
        if (state.Selections == null || state.ReproductionSnapshotId == null)
            throw new InvalidDataException("source-state.json contains a null required field.");
        ValidateSelections(state.Selections);
        if (!state.PlayerReproductionActive && state.ReproductionSnapshotId.Length != 0)
            throw new InvalidDataException("Inactive source state retains a reproductionSnapshotId.");
        if (state.PlayerReproductionActive && (state.ReproductionSnapshotId.Length == 0 || state.Selections.Count != 0))
            throw new InvalidDataException("Active Player Reproduction state must have one snapshot ID and zero selections.");
    }

    private static void RequireExactProperties(JsonElement value, string[] expected, string label)
    {
        string[] actual = value.EnumerateObject()
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        if (!actual.SequenceEqual(expected, StringComparer.Ordinal))
            throw new InvalidDataException(label + " has an unknown, missing, or duplicate field.");
    }

    private static void WriteState(string gameRoot, AuthorSourceState state)
    {
        AtomicStateFile.WriteBytes(
            AuthorStatePaths.SourceStatePath(gameRoot),
            SerializeState(gameRoot, state));
    }

    private static byte[] SerializeState(string gameRoot, AuthorSourceState state)
    {
        state.SchemaVersion = AuthorStatePaths.SchemaVersion;
        state.GameRoot = gameRoot;
        state.Selections = state.Selections.OrderBy(row => row.UniqueId, StringComparer.OrdinalIgnoreCase).ToList();
        return new UTF8Encoding(false).GetBytes(JsonSupport.SerializeTool(state));
    }

    private static RuntimeManifest ReadSourceManifest(string sourcePath)
    {
        string rootManifest = Path.Combine(sourcePath, "manifest.json");
        string packageManifest = Path.Combine(sourcePath, "Content", "DTMAPI", "manifest.json");
        string[] present = new[] { rootManifest, packageManifest }.Where(File.Exists).ToArray();
        if (present.Length != 1)
            throw new InvalidDataException("Local Development source must contain exactly one root or Content/DTMAPI manifest.json.");
        RuntimeManifest manifest = JsonSerializer.Deserialize<RuntimeManifest>(File.ReadAllText(present[0], Encoding.UTF8), JsonSupport.RuntimeManifest)
            ?? throw new InvalidDataException("Local Development manifest must contain one object.");
        if (manifest.Type is not ("CodeMod" or "ContentPack"))
            throw new InvalidDataException("Local Development source must be a DTMAPI CodeMod or ContentPack.");
        return manifest;
    }

    private static void AddWorkshopSnapshotReport(string gameRoot, CommandReport report)
    {
        string path = AuthorStatePaths.WorkshopSnapshotPath(gameRoot);
        report.Values["workshopSnapshotPath"] = path;
        if (!File.Exists(path))
        {
            report.Values["nativeSnapshotAvailable"] = "false";
            report.Values["nativeSnapshotReason"] = "missing";
            return;
        }
        try
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path, Encoding.UTF8));
            JsonElement root = document.RootElement;
            string snapshotRoot = root.TryGetProperty("gameRoot", out JsonElement rootValue) ? rootValue.GetString() ?? string.Empty : string.Empty;
            bool available = root.TryGetProperty("available", out JsonElement availableValue) && availableValue.ValueKind == JsonValueKind.True;
            if (!PathsEqual(snapshotRoot, gameRoot))
                throw new InvalidDataException("native snapshot gameRoot mismatch");
            report.Values["nativeSnapshotAvailable"] = available.ToString().ToLowerInvariant();
            report.Values["nativeSnapshotOwner"] = root.TryGetProperty("nativeOwner", out JsonElement owner) ? owner.GetString() ?? string.Empty : string.Empty;
            report.Values["nativeSnapshotCapturedAtUtc"] = root.TryGetProperty("capturedAtUtc", out JsonElement captured) ? captured.GetString() ?? string.Empty : string.Empty;
            report.Values["nativeSnapshotSubscriptions"] = root.TryGetProperty("subscriptions", out JsonElement rows) && rows.ValueKind == JsonValueKind.Array ? rows.GetArrayLength().ToString(System.Globalization.CultureInfo.InvariantCulture) : "0";
            report.Values["nativeSnapshotReason"] = available ? "runtime-captured-but-revalidate-at-load" : "runtime-reported-unavailable";
        }
        catch (Exception ex)
        {
            report.Values["nativeSnapshotAvailable"] = "false";
            report.Values["nativeSnapshotReason"] = "invalid: " + ex.Message;
        }
    }

    private static void ValidateSelections(IEnumerable<AuthorSourceSelection>? selections)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (AuthorSourceSelection selection in selections ?? Enumerable.Empty<AuthorSourceSelection>())
        {
            if (selection == null ||
                string.IsNullOrWhiteSpace(selection.UniqueId) ||
                !UniqueIdPattern.IsMatch(selection.UniqueId) ||
                !seen.Add(selection.UniqueId))
                throw new InvalidDataException("source-state.json contains an empty or duplicate UniqueID.");
            if (selection.Mode is not ("LocalDevelopment" or "WorkshopValidation"))
                throw new InvalidDataException("source-state.json contains an unsupported persisted mode: " + selection.Mode);
            if (selection.Mode == "LocalDevelopment" &&
                (string.IsNullOrWhiteSpace(selection.SourcePath) ||
                 selection.ExpectedTreeSha256 == null ||
                 selection.ExpectedTreeSha256.Length != 64 ||
                 selection.ExpectedTreeSha256.Any(character => !Uri.IsHexDigit(character))))
            {
                throw new InvalidDataException("LocalDevelopment selection lacks sourcePath/expectedTreeSha256.");
            }
            if (selection.Mode == "WorkshopValidation" &&
                ((selection.SourcePath?.Length ?? -1) != 0 ||
                 (selection.ExpectedTreeSha256?.Length ?? -1) != 0))
            {
                throw new InvalidDataException("WorkshopValidation selection cannot claim an offline source path/hash.");
            }
        }
    }

    private static void Upsert(AuthorSourceState state, AuthorSourceSelection replacement)
    {
        state.Selections.RemoveAll(row => row.UniqueId.Equals(replacement.UniqueId, StringComparison.OrdinalIgnoreCase));
        state.Selections.Add(replacement);
    }

    private static List<AuthorSourceSelection> CloneSelections(IEnumerable<AuthorSourceSelection> selections) => selections.Select(row => new AuthorSourceSelection
    {
        UniqueId = row.UniqueId,
        Mode = row.Mode,
        SourcePath = row.SourcePath,
        ExpectedTreeSha256 = row.ExpectedTreeSha256
    }).ToList();

    private static void RefuseDuringReproduction(AuthorSourceState state)
    {
        if (state.PlayerReproductionActive)
            throw new InvalidDataException("Player Reproduction is active. New/changed overrides are refused until explicit matching restore.");
    }

    private static void ValidateUniqueId(string uniqueId)
    {
        if (!UniqueIdPattern.IsMatch(uniqueId ?? string.Empty))
            throw new InvalidDataException("UniqueID must be dotted and path-safe.");
    }

    private static void RequirePositionals(ParsedCommand command, int count, string message)
    {
        if (command.Positionals.Count != count)
            throw new CommandLineException(message);
    }

    private static string RequireGameRoot(ParsedCommand command)
    {
        string value = command.Option("game-root");
        if (string.IsNullOrWhiteSpace(value))
            throw new CommandLineException("source requires --game-root.");
        string root = AuthorStatePaths.CanonicalGameRoot(value);
        if (!Directory.Exists(root))
            throw new CommandLineException("Game root does not exist: " + root);
        return root;
    }

    private static bool PathsEqual(string left, string right)
    {
        try
        {
            return string.Equals(Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static AuthorDiagnostic Error(string code, string message, string path) => new() { Code = code, Severity = DiagnosticSeverity.Error, Message = message, Path = path };
    private static AuthorDiagnostic Warning(string code, string message, string path) => new() { Code = code, Severity = DiagnosticSeverity.Warning, Message = message, Path = path };
    private static AuthorDiagnostic Info(string code, string message) => new() { Code = code, Severity = DiagnosticSeverity.Info, Message = message };
}
