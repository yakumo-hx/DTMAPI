using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace DTMAPI.Architecture.Tests
{
    // 读一遍仓库里的工程文件和源码，找出不符合 architecture.md 的地方。只看文本，不编译。
    // 结果用"工程名 + 工程内的相对路径"表示，所以整个工程搬到别的目录时，结果不变。
    internal sealed class RepositoryScan
    {
        // 不扫描的目录：构建输出和临时文件（都在 .gitignore 里）、本机工具、文档。
        private static readonly string[] ExcludedSegments =
        {
            "/bin/", "/obj/", "/.git/", "/.tools/", "/.vs/", "/.idea/", "/.vscode/", "/node_modules/",
            "/TestResults/", "/tmp/", "/temp/", "/dist/", "/output/", "/outputs/", "/artifacts/", "/.local-build/",
            "/docs/"
        };

        // 内置内容模块这一层可以引用的工程。AudioPreparation 是这一层用的辅助库。
        private static readonly string[] ModuleLayer =
        {
            "DTMAPI.Abstractions", "DTMAPI.RuntimeModules", "DTMAPI.AudioContent", "DTMAPI.AnimalContent",
            "DTMAPI.MonsterContent", "DTMAPI.AudioPreparation"
        };

        // 运行时工程，以及每个工程可以引用的工程。对应 architecture.md 的分层图。
        private static readonly Dictionary<string, string[]> AllowedReferences =
            new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["DTMAPI.Abstractions"] = new string[0],
                ["DTMAPI.ModConfigMenu"] = new[] { "DTMAPI.Abstractions" },
                ["DTMAPI.AudioContent"] = new[] { "DTMAPI.Abstractions" },
                ["DTMAPI.AnimalContent"] = new[] { "DTMAPI.Abstractions", "DTMAPI.AudioContent" },
                ["DTMAPI.MonsterContent"] = new[] { "DTMAPI.Abstractions", "DTMAPI.AudioContent" },
                ["DTMAPI.Core"] = new[] { "DTMAPI.Abstractions" },
                ["DTMAPI.GameBridge.DolocTown"] = new[] { "DTMAPI.Abstractions", "DTMAPI.Core" },
                ["DTMAPI.RuntimeModules"] = new[]
                {
                    "DTMAPI.Abstractions", "DTMAPI.AudioContent", "DTMAPI.AnimalContent", "DTMAPI.MonsterContent"
                },
                ["DTMAPI.Modules.Animals"] = ModuleLayer,
                ["DTMAPI.Modules.Audio"] = ModuleLayer,
                ["DTMAPI.Modules.Monsters"] = ModuleLayer,
                ["DTMAPI.BepInExBootstrap"] = new[]
                {
                    "DTMAPI.Abstractions", "DTMAPI.Core", "DTMAPI.GameBridge.DolocTown", "DTMAPI.ModConfigMenu",
                    "DTMAPI.BepInExStubs"
                },
                ["DTMAPI.BepInExStubs"] = new string[0],
                ["DTMAPI.AudioPreparation"] = new string[0]
            };

        // 分层规则第 2 条：这些工程不能引用游戏、Unity、Harmony 或 BepInEx。
        private static readonly HashSet<string> OfflineProjects = new HashSet<string>(StringComparer.Ordinal)
        {
            "DTMAPI.Abstractions", "DTMAPI.ModConfigMenu", "DTMAPI.AudioContent", "DTMAPI.AnimalContent",
            "DTMAPI.MonsterContent", "DTMAPI.Core", "DTMAPI.RuntimeModules"
        };

        private static readonly string[] GameReferencePrefixes =
        {
            "Assembly-CSharp", "UnityEngine", "Unity.", "0Harmony", "Harmony", "Lib.Harmony", "BepInEx",
            "$(DtmApiNativeReferences)"
        };

        // 公开的五个程序集。Mod 只能引用这几个。
        private static readonly HashSet<string> PublicAssemblies = new HashSet<string>(StringComparer.Ordinal)
        {
            "DTMAPI.Abstractions", "DTMAPI.ModConfigMenu", "DTMAPI.AudioContent", "DTMAPI.AnimalContent",
            "DTMAPI.MonsterContent"
        };

        // UniqueID 和工程名从 Mod 自己的 manifest 和 csproj 里读。这里补上平时用的短名字。
        private static readonly Regex ModNameWords = new Regex(
            "(MoreSaves|MoreEquipmentSlots|AutoFishing|ChestLocator|OneActionComplete|FishBreeding|StrongPlantingGun|AnimalHusbandryProgress|DebugConsole)",
            RegexOptions.CultureInvariant);

        private static readonly Regex InternalsVisibleToAttribute =
            new Regex(@"InternalsVisibleTo\(\s*""([^"",]+)", RegexOptions.CultureInvariant);

        private static readonly Regex QaAssembly = new Regex(@"(^|\.)QA$", RegexOptions.CultureInvariant);

        // Batch5、Phase0、Candidate11、Round2、G4、MC-29、PN-043 这类编号。
        private static readonly Regex StageNumber = new Regex(
            @"(?i:batch|phase|candidate|round)[-_]?\d+" +
            @"|(?<![A-Za-z0-9])G[1-9]\d?(?=[A-Z_\W]|$)" +
            @"|(?i:(?<![A-Za-z])(?:mc|pn)-?\d{2,3})",
            RegexOptions.CultureInvariant);

        private static readonly Regex Identifier = new Regex(@"[A-Za-z_][A-Za-z0-9_]*", RegexOptions.CultureInvariant);
        private static readonly Regex LineComment = new Regex(@"//.*", RegexOptions.CultureInvariant);
        private static readonly Regex BlockComment =
            new Regex(@"/\*.*?\*/", RegexOptions.Singleline | RegexOptions.CultureInvariant);
        private static readonly Regex StringLiteral =
            new Regex(@"@?""(?:[^""\\\n]|\\.)*""", RegexOptions.CultureInvariant);

        private static readonly Lazy<RepositoryScan> current = new Lazy<RepositoryScan>(() => new RepositoryScan(FindRoot()));

        private readonly string root;

        private RepositoryScan(string root)
        {
            this.root = root;
            List<ProjectFile> projects = EnumerateFiles(root)
                .Where(path => path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                .Select(path => new ProjectFile(path, CollectSources(Path.GetDirectoryName(path)!, true, new List<string>())))
                .ToList();

            var modTokens = new HashSet<string>(StringComparer.Ordinal);
            var modProjects = new HashSet<string>(StringComparer.Ordinal);
            ReadFirstPartyMods(modTokens, modProjects);
            if (modTokens.Count == 0)
                throw new InvalidOperationException("没找到任何第一方 Mod 的 manifest.json，仓库根目录可能找错了：" + root);
            var modToken = new Regex(
                "(?<![A-Za-z0-9_])(" +
                string.Join("|", modTokens.OrderByDescending(token => token.Length)
                    .ThenBy(token => token, StringComparer.Ordinal).Select(token => Regex.Escape(token))) +
                ")(?![A-Za-z0-9_])",
                RegexOptions.CultureInvariant);

            foreach (ProjectFile project in projects)
            {
                if (AllowedReferences.TryGetValue(project.Name, out string[]? allowed))
                {
                    foreach (string reference in project.ProjectReferences)
                    {
                        if (!allowed.Contains(reference))
                            References.Add(project.Name + " -> " + reference);
                    }
                    foreach (string grantee in project.InternalsVisibleTo)
                    {
                        if (IsModOrQa(grantee, modTokens))
                            References.Add(project.Name + " InternalsVisibleTo " + grantee);
                    }
                    foreach (string file in project.Files)
                    {
                        string text = File.ReadAllText(file);
                        string relative = project.Relative(file);
                        foreach (Match match in modToken.Matches(text))
                            ModNames.Add(project.Name + "/" + relative + ": " + match.Groups[1].Value);
                        foreach (Match match in ModNameWords.Matches(text))
                            ModNames.Add(project.Name + "/" + relative + ": " + match.Groups[1].Value);
                    }
                }

                if (OfflineProjects.Contains(project.Name))
                {
                    foreach (string reference in project.AssemblyReferences.Concat(project.PackageReferences))
                    {
                        if (GameReferencePrefixes.Any(prefix => reference.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                            References.Add(project.Name + " -> " + reference);
                    }
                }

                if (modProjects.Contains(project.Name))
                {
                    foreach (string reference in project.ProjectReferences)
                    {
                        if (!PublicAssemblies.Contains(reference))
                            References.Add(project.Name + " -> " + reference);
                    }
                    foreach (string reference in project.AssemblyReferences)
                    {
                        if (reference.StartsWith("DTMAPI.", StringComparison.Ordinal) && !PublicAssemblies.Contains(reference))
                            References.Add(project.Name + " -> " + reference);
                    }
                }

                foreach (string file in project.Files)
                {
                    string relative = project.Relative(file);
                    if (StageNumber.IsMatch(relative) ||
                        Identifier.Matches(StripCode(File.ReadAllText(file))).Any(match => StageNumber.IsMatch(match.Value)))
                    {
                        StageNumbers.Add(project.Name + "/" + relative);
                    }
                }
            }
        }

        internal static RepositoryScan Current => current.Value;

        internal HashSet<string> References { get; } = new HashSet<string>(StringComparer.Ordinal);

        internal HashSet<string> ModNames { get; } = new HashSet<string>(StringComparer.Ordinal);

        internal HashSet<string> StageNumbers { get; } = new HashSet<string>(StringComparer.Ordinal);

        // 从测试程序所在的目录往上找 DTMAPI.sln。第 1 步之后找到的是 public/，只检查公开的部分。
        private static string FindRoot()
        {
            DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "DTMAPI.sln")))
                directory = directory.Parent;
            return directory?.FullName ?? throw new InvalidOperationException("从测试目录往上找不到 DTMAPI.sln。");
        }

        // 第一方 Mod：放在 first-party/ 或 mods/ 下、带 manifest.json 的目录。
        private void ReadFirstPartyMods(HashSet<string> tokens, HashSet<string> projectNames)
        {
            var options = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
            foreach (string directory in EnumerateDirectories(root))
            {
                string parent = Path.GetFileName(Path.GetDirectoryName(directory) ?? string.Empty);
                string manifest = Path.Combine(directory, "manifest.json");
                if ((parent != "first-party" && parent != "mods") || !File.Exists(manifest))
                    continue;

                using (JsonDocument document = JsonDocument.Parse(File.ReadAllText(manifest), options))
                {
                    string uniqueId = ReadString(document.RootElement, "UniqueID");
                    if (uniqueId.Length == 0)
                        uniqueId = ReadString(document.RootElement, "UniqueId");
                    if (uniqueId.Length > 0)
                        tokens.Add(uniqueId);
                }

                foreach (string projectPath in Directory.EnumerateFiles(directory)
                    .Where(path => path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)))
                {
                    string name = Path.GetFileNameWithoutExtension(projectPath);
                    projectNames.Add(name);
                    tokens.Add(name);
                    foreach (XElement element in XDocument.Load(projectPath).Descendants())
                    {
                        if (element.Name.LocalName == "AssemblyName" && element.Value.Trim().Length > 0)
                            tokens.Add(element.Value.Trim());
                    }
                }
            }
        }

        private static string ReadString(JsonElement element, string name) =>
            element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(name, out JsonElement value) &&
            value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? string.Empty
                : string.Empty;

        private static bool IsModOrQa(string grantee, HashSet<string> modTokens) =>
            modTokens.Any(token => grantee == token || grantee.StartsWith(token + ".", StringComparison.Ordinal)) ||
            QaAssembly.IsMatch(grantee);

        // 去掉注释和字符串，只留下代码里的名字。
        private static string StripCode(string text)
        {
            string code = LineComment.Replace(text, string.Empty);
            code = BlockComment.Replace(code, string.Empty);
            return StringLiteral.Replace(code, "\"\"");
        }

        private bool IsExcluded(string directory)
        {
            string relative = "/" + Path.GetRelativePath(root, directory).Replace('\\', '/') + "/";
            return ExcludedSegments.Any(segment => relative.IndexOf(segment, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private IEnumerable<string> EnumerateDirectories(string directory)
        {
            if (IsExcluded(directory))
                yield break;
            yield return directory;
            foreach (string child in Directory.EnumerateDirectories(directory))
            {
                foreach (string nested in EnumerateDirectories(child))
                    yield return nested;
            }
        }

        private IEnumerable<string> EnumerateFiles(string directory) =>
            EnumerateDirectories(directory).SelectMany(path => Directory.EnumerateFiles(path));

        // 工程目录下的 .cs 文件。子目录里如果有自己的 csproj，就属于那个工程，不算在这里。
        private List<string> CollectSources(string directory, bool isProjectDirectory, List<string> sources)
        {
            if (IsExcluded(directory))
                return sources;
            string[] files = Directory.GetFiles(directory);
            if (!isProjectDirectory && files.Any(file => file.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)))
                return sources;
            sources.AddRange(files.Where(file => file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)));
            foreach (string child in Directory.GetDirectories(directory))
                CollectSources(child, false, sources);
            return sources;
        }

        private sealed class ProjectFile
        {
            internal ProjectFile(string path, List<string> sources)
            {
                Name = Path.GetFileNameWithoutExtension(path);
                directory = Path.GetDirectoryName(path)!;
                Files = sources.Append(path).ToList();
                foreach (XElement element in XDocument.Load(path).Descendants())
                {
                    string? include = (string?)element.Attribute("Include");
                    if (string.IsNullOrWhiteSpace(include))
                        continue;
                    switch (element.Name.LocalName)
                    {
                        case "ProjectReference":
                            ProjectReferences.Add(Path.GetFileNameWithoutExtension(include.Replace('\\', '/')));
                            break;
                        case "Reference":
                            AssemblyReferences.Add(include.Split(',')[0].Trim());
                            break;
                        case "PackageReference":
                            PackageReferences.Add(include.Trim());
                            break;
                        case "InternalsVisibleTo":
                            InternalsVisibleTo.Add(include.Split(',')[0].Trim());
                            break;
                    }
                }
                foreach (string source in sources)
                {
                    foreach (Match match in InternalsVisibleToAttribute.Matches(File.ReadAllText(source)))
                        InternalsVisibleTo.Add(match.Groups[1].Value);
                }
            }

            private readonly string directory;

            internal string Name { get; }

            // 工程里的 .cs 文件，加上 csproj 本身。
            internal List<string> Files { get; }

            internal List<string> ProjectReferences { get; } = new List<string>();

            internal List<string> AssemblyReferences { get; } = new List<string>();

            internal List<string> PackageReferences { get; } = new List<string>();

            internal List<string> InternalsVisibleTo { get; } = new List<string>();

            internal string Relative(string file) => Path.GetRelativePath(directory, file).Replace('\\', '/');
        }
    }
}
