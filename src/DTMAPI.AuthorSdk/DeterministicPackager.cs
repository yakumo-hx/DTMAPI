using DTMAPI.Authoring.Contracts;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DTMAPI.AuthorSdk;

internal static class DeterministicPackager
{
    private static readonly DateTimeOffset FixedZipTime = new(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static CommandReport PackCommand(ParsedCommand command)
    {
        command.RequireOnlyOptions("compatibility-root", "output", "game-root");
        if (command.Positionals.Count != 1)
            throw new CommandLineException("pack requires one project directory.");
        return Pack(PathSafety.FullPath(command.Positionals[0]), command.Option("compatibility-root"), command.Option("output"), command.Option("game-root"));
    }

    public static CommandReport HashCommand(ParsedCommand command)
    {
        command.RequireOnlyOptions();
        if (command.Positionals.Count != 1)
            throw new CommandLineException("hash requires one file or directory.");
        string path = PathSafety.FullPath(command.Positionals[0]);
        var report = new CommandReport { Command = "hash", RootPath = path };
        if (File.Exists(path))
        {
            report.Sha256 = PathSafety.Sha256File(path);
            report.FileCount = 1;
        }
        else if (Directory.Exists(path))
        {
            string[] files = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).OrderBy(file => PathSafety.RelativePath(path, file), StringComparer.Ordinal).ToArray();
            PathSafety.RejectReparsePoints(path, Directory.EnumerateFileSystemEntries(path, "*", SearchOption.AllDirectories));
            report.Sha256 = AuthorFileTreeDigest.Compute(path);
            report.FileCount = files.Length;
            report.Values["treeDigestAlgorithm"] = AuthorFileTreeDigest.AlgorithmId;
        }
        else
        {
            report.Diagnostics.Add(Error("SDK300", "Path was not found.", path));
            return Finish(report);
        }
        report.Diagnostics.Add(Info("SDK000", "Computed a deterministic SHA-256 identity without changing the input."));
        return Finish(report);
    }

    public static CommandReport Pack(string root, string compatibilityRoot, string requestedOutput, string gameRoot = "")
    {
        var report = new CommandReport { Command = "pack", RootPath = Path.GetFullPath(root) };
        AuthorProjectContext context;
        try
        {
            context = ProjectValidator.LoadValidated(root, report.Diagnostics);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            report.Diagnostics.Add(Error("SDK301", ex.Message, report.RootPath));
            return Finish(report);
        }
        if (report.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
            return Finish(report);

        try
        {
            string builtDll = string.Empty;
            if (context.Kind == AuthorProjectKind.CodeMod)
            {
                CommandReport build = CodeModBuilder.Build(context.RootPath, compatibilityRoot, string.Empty, gameRoot);
                report.Diagnostics.AddRange(build.Diagnostics.Where(diagnostic => diagnostic.Severity != DiagnosticSeverity.Info));
                if (!build.Success)
                    return Finish(report);
                builtDll = build.OutputPath;
                report.Values["abstractionsSha256"] = build.Values["abstractionsSha256"];
                report.Values["compatibilityManifestSha256"] = build.Values["compatibilityManifestSha256"];
            }

            var payload = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);
            Add(payload, "info.json", SerializeInfo(context));
            JsonObject packagedManifest = (JsonObject)context.ManifestJson.DeepClone();
            if (context.Kind == AuthorProjectKind.CodeMod)
                packagedManifest["EntryDll"] = "Content/DTMAPI/" + Path.GetFileName(builtDll);
            byte[] manifestBytes = Utf8(JsonSerializer.Serialize(packagedManifest, JsonSupport.RuntimeManifest) + "\n");
            Add(payload, AuthorSdkContract.PackageManifestPath, manifestBytes);
            byte[] entryDllBytes = Array.Empty<byte>();
            string entryDllPath = string.Empty;
            if (context.Kind == AuthorProjectKind.CodeMod)
            {
                entryDllPath = "Content/DTMAPI/" + Path.GetFileName(builtDll);
                entryDllBytes = File.ReadAllBytes(builtDll);
                Add(payload, entryDllPath, entryDllBytes);
            }

            string advancedReceiptSha256 = string.Empty;
            if (context.Kind == AuthorProjectKind.CodeMod && context.CodeModKind == AuthorCodeModKind.Advanced)
            {
                ResolvedAdvancedReferenceSet resolved = AdvancedReferenceAssets.Resolve(context, gameRoot);
                AdvancedReferenceReceipt receipt = AdvancedReferenceAssets.CreateReceipt(
                    context,
                    resolved,
                    AuthorSdkContract.PackageManifestPath,
                    manifestBytes,
                    entryDllPath,
                    entryDllBytes);
                byte[] receiptBytes = Utf8(JsonSerializer.Serialize(receipt, JsonSupport.StrictToolContract) + "\n");
                advancedReceiptSha256 = PathSafety.Sha256Bytes(receiptBytes);
                Add(payload, AuthorSdkContract.AdvancedReferenceReceiptPath, receiptBytes);
            }

            AddContentDirectory(context, payload);
            AddOptionalDirectory(context.RootPath, "i18n", "i18n", payload);
            AddOptionalRootFile(context.RootPath, "icon.png", payload);
            AddOptionalRootFile(context.RootPath, "preview.png", payload);
            ManagedAssemblyInspector.ValidateNoBundledNativePayloads(payload, entryDllPath);
            Add(payload, "Content/DTMAPI/dtmapi-package.json", SerializePackageMarker(
                context,
                PathSafety.Sha256Bytes(manifestBytes),
                entryDllPath,
                entryDllBytes.Length == 0 ? string.Empty : PathSafety.Sha256Bytes(entryDllBytes),
                advancedReceiptSha256));

            string defaultName = SafeFileName(context.Manifest.UniqueID) + "-" + SafeFileName(context.Manifest.Version) + ".zip";
            string outputPath = ResolveOutput(context.RootPath, requestedOutput, defaultName);
            PathSafety.RejectBepInExPluginDestination(outputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            string temporary = outputPath + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                WriteZip(temporary, payload);
                string newHash = PathSafety.Sha256File(temporary);
                if (File.Exists(outputPath))
                {
                    string oldHash = PathSafety.Sha256File(outputPath);
                    if (!oldHash.Equals(newHash, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Output package already exists with different bytes. Bump the manifest version or remove the old author artifact explicitly: " + outputPath);
                    File.Delete(temporary);
                }
                else
                {
                    File.Move(temporary, outputPath);
                }

                var packageReport = new DeterministicPackageReport
                {
                    UniqueID = context.Manifest.UniqueID,
                    Version = context.Manifest.Version,
                    ProjectKind = context.Kind.ToString(),
                    CodeModKind = context.Kind == AuthorProjectKind.CodeMod ? context.CodeModKind.ToString() : string.Empty,
                    PackageFileName = Path.GetFileName(outputPath),
                    PackageSha256 = newHash,
                    ManifestSha256 = PathSafety.Sha256Bytes(manifestBytes),
                    EntryDllSha256 = entryDllBytes.Length == 0 ? string.Empty : PathSafety.Sha256Bytes(entryDllBytes),
                    AdvancedReferenceReceiptSha256 = advancedReceiptSha256,
                    Files = payload.Select(pair => new PackagedFile
                    {
                        Path = pair.Key,
                        Length = pair.Value.LongLength,
                        Sha256 = PathSafety.Sha256Bytes(pair.Value)
                    }).ToList()
                };
                string reportPath = outputPath + ".report.json";
                File.WriteAllText(reportPath, JsonSupport.SerializeTool(packageReport), new UTF8Encoding(false));

                report.OutputPath = outputPath;
                report.Sha256 = newHash;
                report.FileCount = payload.Count;
                report.Values["reportPath"] = reportPath;
                report.Values["projectKind"] = context.Kind.ToString();
                report.Values["codeModKind"] = context.Kind == AuthorProjectKind.CodeMod ? context.CodeModKind.ToString() : string.Empty;
                report.Values["uniqueID"] = context.Manifest.UniqueID;
                report.Values["version"] = context.Manifest.Version;
                report.Values["workshopContentRoot"] = "Content/DTMAPI";
                report.Values["manifestSha256"] = PathSafety.Sha256Bytes(manifestBytes);
                report.Values["entryDllSha256"] = entryDllBytes.Length == 0 ? string.Empty : PathSafety.Sha256Bytes(entryDllBytes);
                report.Values["advancedReferenceReceiptSha256"] = advancedReceiptSha256;
                report.Diagnostics.Insert(0, Info("SDK000", "Created a deterministic Workshop-shaped package. No upload or credential operation was attempted."));
            }
            finally
            {
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            report.Diagnostics.Add(Error("SDK302", ex.Message, report.RootPath));
        }
        return Finish(report);
    }

    private static void AddContentDirectory(AuthorProjectContext context, SortedDictionary<string, byte[]> payload)
    {
        if (!Directory.Exists(context.ContentPath))
            return;
        foreach (string file in Directory.EnumerateFiles(context.ContentPath, "*", SearchOption.AllDirectories).OrderBy(file => PathSafety.RelativePath(context.ContentPath, file), StringComparer.Ordinal))
        {
            string relative = PathSafety.RelativePath(context.ContentPath, file);
            if (relative.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Content payload cannot add DLLs: " + relative);
            string target = "Content/DTMAPI/" + relative;
            if (target.Equals(AuthorSdkContract.AdvancedReferenceReceiptPath, StringComparison.OrdinalIgnoreCase)
                || target.Equals("Content/DTMAPI/dtmapi-package.json", StringComparison.OrdinalIgnoreCase)
                || target.Equals(AuthorSdkContract.PackageManifestPath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Content payload cannot replace an SDK-reserved package contract: " + relative);
            Add(payload, target, File.ReadAllBytes(file));
        }
    }

    private static void AddOptionalDirectory(string root, string sourceRelative, string targetRelative, SortedDictionary<string, byte[]> payload)
    {
        string source = Path.Combine(root, sourceRelative);
        if (!Directory.Exists(source))
            return;
        foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories).OrderBy(file => PathSafety.RelativePath(source, file), StringComparer.Ordinal))
            Add(payload, targetRelative + "/" + PathSafety.RelativePath(source, file), File.ReadAllBytes(file));
    }

    private static void AddOptionalRootFile(string root, string name, SortedDictionary<string, byte[]> payload)
    {
        string path = Path.Combine(root, name);
        if (File.Exists(path))
            Add(payload, name, File.ReadAllBytes(path));
    }

    private static void Add(SortedDictionary<string, byte[]> payload, string path, byte[] bytes)
    {
        string normalized = path.Replace('\\', '/').TrimStart('/');
        if (normalized.Length == 0 || normalized.Split('/').Any(segment => segment is "" or "." or "..") || Path.IsPathRooted(normalized))
            throw new InvalidDataException("Invalid package entry path: " + path);
        if (payload.Keys.Any(existing => existing.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException("Case-insensitive package entry collision: " + normalized);
        payload.Add(normalized, bytes);
    }

    private static byte[] SerializeInfo(AuthorProjectContext context)
    {
        AuthorPublishMetadata publish = context.AuthorProject.Publish;
        var info = new JsonObject
        {
            ["name"] = context.Manifest.Name,
            ["author"] = context.Manifest.Author,
            ["version"] = context.Manifest.Version,
            ["description"] = context.Manifest.Description,
            ["tags"] = new JsonArray(publish.Tags.Select(tag => JsonValue.Create(tag)).ToArray()),
            ["localized_name"] = ToJsonObject(publish.LocalizedName),
            ["localized_description"] = ToJsonObject(publish.LocalizedDescription)
        };
        return Utf8(JsonSerializer.Serialize(info, JsonSupport.Tool) + "\n");
    }

    private static byte[] SerializePackageMarker(AuthorProjectContext context, string manifestSha256, string entryDllPath, string entryDllSha256, string advancedReceiptSha256)
    {
        var marker = new JsonObject
        {
            ["schemaVersion"] = 2,
            ["owner"] = "DTMAPI",
            ["uniqueId"] = context.Manifest.UniqueID,
            ["version"] = context.Manifest.Version,
            ["packageKind"] = context.Kind.ToString(),
            ["codeModKind"] = context.Kind == AuthorProjectKind.CodeMod ? context.CodeModKind.ToString() : string.Empty,
            ["authorSdkVersion"] = AuthorSdkContract.SdkVersion,
            ["targetDtmApiVersion"] = AuthorSdkContract.TargetRuntimeVersion,
            ["manifestPath"] = AuthorSdkContract.PackageManifestPath,
            ["manifestSha256"] = manifestSha256,
            ["entryDllPath"] = entryDllPath,
            ["entryDllSha256"] = entryDllSha256,
            ["advancedReferenceReceiptPath"] = advancedReceiptSha256.Length == 0 ? string.Empty : AuthorSdkContract.AdvancedReferenceReceiptPath,
            ["advancedReferenceReceiptSha256"] = advancedReceiptSha256,
            ["authority"] = "dtmapi-author-sdk-package-binding"
        };
        return Utf8(JsonSerializer.Serialize(marker, JsonSupport.Tool) + "\n");
    }

    private static JsonObject ToJsonObject(IEnumerable<KeyValuePair<string, string>> values)
    {
        var result = new JsonObject();
        foreach (KeyValuePair<string, string> pair in values.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            result[pair.Key] = pair.Value;
        return result;
    }

    private static void WriteZip(string path, IEnumerable<KeyValuePair<string, byte[]>> payload)
    {
        using FileStream stream = new(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: false, entryNameEncoding: Encoding.UTF8);
        foreach (KeyValuePair<string, byte[]> pair in payload.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            ZipArchiveEntry entry = archive.CreateEntry(pair.Key, CompressionLevel.Optimal);
            entry.LastWriteTime = FixedZipTime;
            entry.ExternalAttributes = (int)FileAttributes.Normal;
            using Stream target = entry.Open();
            target.Write(pair.Value, 0, pair.Value.Length);
        }
    }

    private static string ResolveOutput(string root, string requested, string defaultName)
    {
        if (string.IsNullOrWhiteSpace(requested))
            return Path.Combine(root, "dist", defaultName);
        string full = Path.GetFullPath(requested, root);
        return full.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ? full : Path.Combine(full, defaultName);
    }

    private static string SafeFileName(string value)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        return new string(value.Select(character => invalid.Contains(character) ? '_' : character).ToArray());
    }

    private static byte[] Utf8(string value) => new UTF8Encoding(false).GetBytes(value.Replace("\r\n", "\n", StringComparison.Ordinal));

    private static CommandReport Finish(CommandReport report)
    {
        report.Success = report.Diagnostics.All(diagnostic => diagnostic.Severity != DiagnosticSeverity.Error);
        return report;
    }

    private static AuthorDiagnostic Error(string code, string message, string path) => new() { Code = code, Severity = DiagnosticSeverity.Error, Message = message, Path = path };
    private static AuthorDiagnostic Info(string code, string message) => new() { Code = code, Severity = DiagnosticSeverity.Info, Message = message };
}
