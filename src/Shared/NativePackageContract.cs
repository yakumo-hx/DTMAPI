using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using static DTMAPI.Internal.Authoring.PackageDependencyContract;

namespace DTMAPI.Internal.Authoring
{
    internal sealed class NativeHostReference
    {
        public string GameRelativePath { get; set; } = "";
        public string AssemblyIdentity { get; set; } = "";
        public long Length { get; set; }
        public string Sha256 { get; set; } = "";
        public string Mode { get; set; } = "";
        public string SurfaceSha256 { get; set; } = "";
    }
    internal sealed class NativeRequiredMember
    {
        public System.Xml.Linq.XElement? GenericUse { get; set; }
        public string AssemblyIdentity { get; set; } = "";
        public string DeclaringType { get; set; } = "";
        public string Kind { get; set; } = "";
        public string Name { get; set; } = "";
        public bool IsStatic { get; set; }
        public string ReturnType { get; set; } = "";
        public string[] ParameterTypes { get; set; } = System.Array.Empty<string>();
        public string Key => GenericUse == null ? NativeSignature.MemberKey(AssemblyIdentity, DeclaringType, Kind, Name, IsStatic, ReturnType, ParameterTypes) : NativeGenericSignature.Key(GenericUse);
    }
    internal sealed class NativePackageContract
    {
        public const string FileName = "dtmapi-native-build.json";
        public int SchemaVersion { get; private set; } = 1;
        public string OwnerId { get; private set; } = "";
        public string ApiTarget { get; private set; } = "";
        public string ManifestSha256 { get; private set; } = "";
        public string EntrySha256 { get; private set; } = "";
        public string DependencyInventorySha256 { get; private set; } = "";
        public string GameBuild { get; private set; } = "";
        public string HarmonyOwner { get; private set; } = "";
        public string Generation { get; private set; } = "";
        public string GenerationInputSha256 { get; private set; } = "";
        public NativeHostReference[] MetadataDependencies { get; private set; } = System.Array.Empty<NativeHostReference>();
        public NativeHostReference[] References { get; private set; } = System.Array.Empty<NativeHostReference>();
        public NativeRequiredMember[] RequiredMembers { get; private set; } = System.Array.Empty<NativeRequiredMember>();

        public static bool Select(byte[] manifest) => SelectedVersion(manifest) != 0;
        public static int SelectedVersion(byte[] manifest)
        {
            var root = ReadJson(manifest);
            string Name(System.Xml.Linq.XElement element) => (string?)element.Attribute("item") ?? element.Name.LocalName;
            var selectors = root.Elements().Where(element => Name(element).Equals("NativeContractVersion", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (selectors.Length == 0) return 0;
            if (selectors.Length != 1 || Name(selectors[0]) != "NativeContractVersion") Reject("selector-invalid", "Duplicate/mis-cased NativeContractVersion.");
            long selected = Number(selectors[0]);
            if (selected != 1 && selected != 2) Reject("contract-unsupported", "Expected NativeContractVersion=1 or 2; upgrade Runtime for newer contracts; no old-reader fallback.");
            int version = (int)selected;
            var fields = Object(root);
            if (Text(Required(fields, "CodeModKind")) != "Advanced" || Text(Required(fields, "Type")) != "CodeMod" || Number(Required(fields, "DependencyContractVersion")) != 1)
                Reject("selector-invalid", "Native V1 requires explicit Advanced CodeMod and DependencyContractVersion=1.");
            if (version == 2 && Text(Required(fields, "MinimumDTMApiVersion")) != "0.7.0") Reject("runtime-version", "Native V2 requires the V2-capable 0.7.0 Runtime candidate.");
            return version;
        }

        public static NativePackageContract Read(byte[] bytes)
        {
            var fields = Object(ReadJson(bytes), "schemaVersion", "ownerId", "apiTarget", "targetFramework", "manifestSha256", "entrySha256", "dependencyInventorySha256", "gameBuild", "references", "requiredMembers", "harmonyOwner", "referenceGeneration");
            string Value(string field) => Text(Required(fields, field));
            long selectedSchema = Number(Required(fields, "schemaVersion"));
            if (selectedSchema != 1 && selectedSchema != 2) Reject("contract-unsupported", "Expected native schemaVersion=1 or 2.");
            int schema = (int)selectedSchema;
            if (Value("targetFramework") != "netstandard2.0") Reject("framework-invalid", Value("targetFramework"));
            string owner = Value("ownerId"), harmonyOwner = Value("harmonyOwner");
            RequireOwner(owner, harmonyOwner);
            var references = new List<NativeHostReference>();
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var identities = new HashSet<string>(StringComparer.Ordinal);
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in Array(Required(fields, "references")))
            {
                var row = Object(item, "gameRelativePath", "assemblyIdentity", "length", "sha256", "mode", "surfaceSha256");
                string Get(string field) => Text(Required(row, field));
                var reference = new NativeHostReference { GameRelativePath = HostPath(Get("gameRelativePath")), AssemblyIdentity = Identity(Get("assemblyIdentity")), Length = Number(Required(row, "length")), Sha256 = Hash(Get("sha256")), Mode = Get("mode"), SurfaceSha256 = Get("surfaceSha256") };
                string name = new AssemblyName(reference.AssemblyIdentity).Name!;
                if (!paths.Add(reference.GameRelativePath) || !identities.Add(reference.AssemblyIdentity) || !names.Add(name)) Reject("reference-duplicate", name);
                if (reference.Length <= 0 || (reference.Mode != "direct" && reference.Mode != "metadata-surface")) Reject("reference-invalid", name);
                if (reference.Mode == "metadata-surface") Hash(reference.SurfaceSha256);
                else if (reference.SurfaceSha256 != "") Reject("reference-invalid", "Direct reference must not declare a surface hash.");
                if (name.StartsWith("DTMAPI.", StringComparison.OrdinalIgnoreCase) || name == "netstandard" || name == "mscorlib" || name == "System" || name.StartsWith("System.", StringComparison.OrdinalIgnoreCase)) Reject("reference-forbidden", name);
                if (Path.GetFileName(reference.GameRelativePath) != name + ".dll") Reject("reference-filename", name);
                references.Add(reference);
            }
            if (references.Count == 0) Reject("reference-empty", "Explicit installed host references required.");
            var members = Array(Required(fields, "requiredMembers")).Select(item => schema == 1 ? ReadMember(item) : ReadGenericMember(item)).ToArray();
            if (members.Select(member => member.Key).Distinct(StringComparer.Ordinal).Count() != members.Length || members.Any(member => !identities.Contains(member.AssemblyIdentity))) Reject("member-invalid", "Duplicate member or undeclared host identity.");
            var generation = Object(Required(fields, "referenceGeneration"), "toolVersion", "mode", "inputSha256", "metadataDependencies");
            if (Text(Required(generation, "toolVersion")) != "0.6.4" || Text(Required(generation, "mode")) != "metadata-surface-v1/cecil-0.11.6") Reject("generation-unsupported", "Unknown reference generation.");
            string inputHash = Hash(Text(Required(generation, "inputSha256")));
            var support = new List<NativeHostReference>();
            foreach (var item in Array(Required(generation, "metadataDependencies")))
            {
                var row = Object(item, "gameRelativePath", "assemblyIdentity", "length", "sha256");
                var dependency = new NativeHostReference { GameRelativePath = HostPath(Text(Required(row, "gameRelativePath"))), AssemblyIdentity = Identity(Text(Required(row, "assemblyIdentity"))), Length = Number(Required(row, "length")), Sha256 = Hash(Text(Required(row, "sha256"))) };
                if (dependency.Length <= 0 || !paths.Add(dependency.GameRelativePath)) Reject("generation-binding", "Duplicate/invalid metadata input.");
                support.Add(dependency);
            }
            if (inputHash != InputHash(references.Concat(support))) Reject("generation-binding", "Original host inputs disagree with generation digest.");
            return new NativePackageContract
            {
                SchemaVersion = schema, OwnerId = owner, ApiTarget = Value("apiTarget"), ManifestSha256 = Hash(Value("manifestSha256")), EntrySha256 = Hash(Value("entrySha256")), DependencyInventorySha256 = Hash(Value("dependencyInventorySha256")),
                GameBuild = Value("gameBuild"), HarmonyOwner = harmonyOwner, References = references.ToArray(), RequiredMembers = members,
                Generation = Text(Required(generation, "mode")), GenerationInputSha256 = inputHash, MetadataDependencies = support.ToArray()
            };
        }

        public static NativeRequiredMember ReadMember(System.Xml.Linq.XElement item)
        {
            var row = Object(item, "assemblyIdentity", "declaringType", "kind", "name", "isStatic", "returnType", "parameterTypes");
            string Get(string name) => Text(Required(row, name));
            var flag = Required(row, "isStatic");
            if ((string?)flag.Attribute("type") != "boolean" || (flag.Value != "true" && flag.Value != "false")) Reject("member-invalid", "isStatic must be boolean.");
            var member = new NativeRequiredMember { AssemblyIdentity = Identity(Get("assemblyIdentity")), DeclaringType = Get("declaringType"), Kind = Get("kind"), Name = Get("name"), IsStatic = flag.Value == "true", ReturnType = Get("returnType"), ParameterTypes = Array(Required(row, "parameterTypes")).Select(Text).ToArray() };
            if (member.DeclaringType.Length == 0 || member.DeclaringType.IndexOfAny(new[] { '|', ';', '`' }) >= 0) Reject("signature-unsupported", member.DeclaringType);
            if (member.Kind != "type" && member.Kind != "method" && member.Kind != "field") Reject("member-invalid", member.Kind);
            if (member.Kind == "type" ? member.Name != "" || member.IsStatic || member.ReturnType != "" || member.ParameterTypes.Length != 0 : member.Name.Length == 0 || member.ReturnType.Length == 0) Reject("member-invalid", member.Key);
            if (member.Kind == "field" && member.ParameterTypes.Length != 0) Reject("member-invalid", member.Key);
            if (member.Name.IndexOfAny(new[] { '|', ';' }) >= 0 || member.ParameterTypes.Concat(new[] { member.ReturnType }).Any(value => value.IndexOfAny(new[] { '|', ';', '\0' }) >= 0)) Reject("member-invalid", "Invalid signature delimiter.");
            return member;
        }

        public static NativeRequiredMember ReadGenericMember(System.Xml.Linq.XElement item)
        {
            NativeGenericSignature.Validate(item);
            var definition = NativeGenericSignature.Get(item, "definition");
            var declaring = NativeGenericSignature.Get(definition, "declaringType");
            return new NativeRequiredMember { GenericUse = new System.Xml.Linq.XElement(item),
                AssemblyIdentity = Identity(Text(Required(Object(declaring), "assemblyIdentity"))),
                DeclaringType = Text(Required(Object(declaring), "name")), Kind = Text(Required(Object(definition), "kind")),
                Name = Text(Required(Object(definition), "name")), IsStatic = NativeGenericSignature.Flag(NativeGenericSignature.Get(definition, "isStatic")) };
        }

        public void RequireBinding(string owner, string target, byte[] manifest, byte[] entry, byte[] dependencies)
        {
            if (SelectedVersion(manifest) != SchemaVersion || OwnerId != owner || ApiTarget != target || ManifestSha256 != ComputeHash(manifest) || EntrySha256 != ComputeHash(entry) || DependencyInventorySha256 != ComputeHash(dependencies)) Reject("package-binding", "Native provenance does not match manifest/entry/dependencies.");
            if (SchemaVersion == 2)
            {
                if (ApiTarget != "0.7.0") Reject("target-invalid", "Native V2 requires current 0.7.0 target.");
                var inventory = ReadInventory(dependencies);
                var identities = new HashSet<string>(inventory.Assemblies.Select(a => IdentityString(a.Identity)).Concat(inventory.Assemblies.SelectMany(a => a.References).Select(IdentityString)).Concat(References.Select(r => r.AssemblyIdentity)), StringComparer.Ordinal);
                foreach (var node in RequiredMembers.SelectMany(m => NativeGenericSignature.NamedNodes(m.GenericUse!)))
                {
                    string identity = NativeGenericSignature.Text(NativeGenericSignature.Get(node, "assemblyIdentity"));
                    if (identity != "bcl" && !identities.Contains(identity)) Reject("type-argument-reference", "Undeclared package/host identity: " + identity);
                }
            }
        }

        public bool AllowsReference(PackageAssemblyIdentity identity) => References.Any(reference => reference.AssemblyIdentity == IdentityString(identity));
        public static string IdentityString(PackageAssemblyIdentity identity) => identity.Name + ", Version=" + identity.AssemblyVersion + ", Culture=" + (identity.Culture.Length == 0 ? "neutral" : identity.Culture) + ", PublicKeyToken=" + (identity.PublicKeyToken.Length == 0 ? "null" : identity.PublicKeyToken);

        public static bool Contains(Assembly host, NativeRequiredMember required)
        {
            if (required.GenericUse != null) return NativeGenericSignature.Contains(host, required.GenericUse);
            if (host.GetName().FullName != required.AssemblyIdentity) return false;
            Type? type = host.GetType(required.DeclaringType, throwOnError: false, ignoreCase: false);
            if (type == null || type.IsGenericType) return false;
            if (required.Kind == "type") return true;
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            try
            {
                if (required.Kind == "field")
                {
                    FieldInfo? field = type.GetField(required.Name, flags);
                    return field != null && field.GetRequiredCustomModifiers().Length == 0 && field.GetOptionalCustomModifiers().Length == 0 && field.IsStatic == required.IsStatic && NativeSignature.TypeName(field.FieldType) == required.ReturnType;
                }
                foreach (MethodBase method in type.GetMethods(flags).Cast<MethodBase>().Concat(type.GetConstructors(flags)))
                {
                    if (method.Name != required.Name || method.IsStatic != required.IsStatic || method.IsGenericMethod || (method.CallingConvention & CallingConventions.VarArgs) != 0) continue;
                    if (method.GetParameters().Any(parameter => parameter.GetRequiredCustomModifiers().Length > 0 || parameter.GetOptionalCustomModifiers().Length > 0) ||
                        (method is MethodInfo modified && (modified.ReturnParameter.GetRequiredCustomModifiers().Length > 0 || modified.ReturnParameter.GetOptionalCustomModifiers().Length > 0))) continue;
                    try
                    {
                        if (NativeSignature.TypeName(method is MethodInfo info ? info.ReturnType : typeof(void)) == required.ReturnType && method.GetParameters().Select(parameter => NativeSignature.TypeName(parameter.ParameterType)).SequenceEqual(required.ParameterTypes)) return true;
                    }
                    catch (InvalidDataException ex) when (ex.Message.StartsWith("native-signature-unsupported:", StringComparison.Ordinal)) { }
                }
            }
            catch (InvalidDataException ex) when (ex.Message.StartsWith("native-signature-unsupported:", StringComparison.Ordinal)) { }
            return false;
        }

        public IReadOnlyList<string> VerifyHost(string gameRoot, Func<byte[], string> readIdentity, Func<byte[], NativeRequiredMember, bool> contains)
        {
            var warnings = new List<string>();
            foreach (var reference in References)
            {
                byte[] bytes = File.ReadAllBytes(ResolveFile(gameRoot, reference.GameRelativePath));
                if (readIdentity(bytes) != reference.AssemblyIdentity) Reject("host-identity-mismatch", reference.GameRelativePath);
                foreach (var member in RequiredMembers.Where(member => member.AssemblyIdentity == reference.AssemblyIdentity))
                    if (!contains(bytes, member)) Reject("required-member-missing", member.Key);
                if (bytes.LongLength != reference.Length || ComputeHash(bytes) != reference.Sha256) warnings.Add("native-unverified-game-version: " + reference.GameRelativePath + "; required signatures still match.");
            }
            return warnings;
        }

        public static string InputHash(IEnumerable<NativeHostReference> references) => ComputeHash(System.Text.Encoding.UTF8.GetBytes(string.Join("\n", references.OrderBy(reference => reference.GameRelativePath, StringComparer.Ordinal).Select(reference => reference.GameRelativePath + "|" + reference.AssemblyIdentity + "|" + reference.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + reference.Sha256))));
        public static void RequireOwner(string owner, string harmonyOwner)
        {
            if (!Regex.IsMatch(owner, @"^[A-Za-z][A-Za-z0-9]*(?:\.[A-Za-z0-9][A-Za-z0-9_-]*)+$", RegexOptions.CultureInvariant) ||
                !(harmonyOwner == owner || harmonyOwner.StartsWith(owner + ".", StringComparison.Ordinal)) || !Regex.IsMatch(harmonyOwner, @"^[A-Za-z0-9_.-]+$", RegexOptions.CultureInvariant)) Reject("harmony-owner-invalid", harmonyOwner);
        }
        public static string HostPath(string path)
        {
            Relative(path);
            if (!(path.StartsWith("DolocTown_Data/Managed/", StringComparison.Ordinal) || path.StartsWith("BepInEx/core/", StringComparison.Ordinal)) || !path.EndsWith(".dll", StringComparison.Ordinal)) Reject("reference-path", path);
            return path;
        }
        private static string Identity(string text)
        {
            try { var identity = new AssemblyName(text); if (identity.FullName != text || identity.Version == null || text.IndexOfAny(new[] { '|', ';', '\0' }) >= 0) Reject("identity-invalid", text); return text; }
            catch (Exception ex) when (ex is ArgumentException || ex is FileLoadException) { throw new InvalidDataException("native-identity-invalid: " + text, ex); }
        }
        private static void Reject(string code, string detail) => throw new InvalidDataException("native-" + code + ": " + detail);
    }
}
