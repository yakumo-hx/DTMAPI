using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using Yarn;
using Op = Yarn.Instruction.Types.OpCode;

namespace DTMAPI.Internal.StoryS2
{
    // Only the build-checked, hash-bound S1 resource is admitted. This is not a
    // public bundle loader and does not replace S1's complete author preflight.
    internal sealed class Candidate
    {
        internal readonly Yarn.Program Program;
        internal readonly JObject Definition;
        internal readonly Dictionary<string, JObject> Lines;
        internal readonly Dictionary<string, string> Stories = new Dictionary<string, string>();
        internal readonly Dictionary<string, string> BaseLocales = new Dictionary<string, string>();
        private readonly HashSet<string> functionSignatures;
        private Candidate(byte[] bytes, string expectedHash)
        {
            Require(Hash(bytes) == expectedHash, "S2-CANDIDATE-HASH");
            using var zip = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
            Require(zip.Entries.Count == 3 && new HashSet<string>(zip.Entries.Select(e => e.FullName)).SetEquals(new[] { "candidate.json", "program.yarnc", "inventory.json" }), "S2-CANDIDATE-ENTRIES");
            byte[] Read(string name)
            {
                var entry = zip.GetEntry(name)!;
                Require(entry.Length <= 16 * 1024 * 1024, "S2-CANDIDATE-SIZE");
                using var stream = entry.Open(); using var buffer = new MemoryStream();
                stream.CopyTo(buffer); return buffer.ToArray();
            }
            Definition = JObject.Parse(System.Text.Encoding.UTF8.GetString(Read("candidate.json")));
            Require((string?)Definition["format"] == "dtmapi.story.candidate/1" && (string?)Definition["yarnVersion"] == "2.4.2" && (string?)Definition["profile"] == "s1-minimal/1", "S2-CANDIDATE-FORMAT");
            Program = Yarn.Program.Parser.ParseFrom(Read("program.yarnc"));
            var inventory = JObject.Parse(System.Text.Encoding.UTF8.GetString(Read("inventory.json")));
            functionSignatures = inventory["references"]!.Where(r => (string?)r["kind"] == "function").Select(r => (string)r["target"]!).ToHashSetCompat();
            Lines = Definition["lines"]!.Cast<JObject>().ToDictionary(l => (string)l["id"]!, StringComparer.Ordinal);
            foreach (var pack in Definition["packs"]!)
            {
                var manifest = pack["manifest"]!; string id = (string)manifest["packId"]!;
                BaseLocales.Add(id, (string)manifest["baseLocale"]!);
                foreach (var story in manifest["stories"]!)
                    Stories.Add(id + ":" + (string)story["id"]!, (string)pack["nodes"]![(string)story["entryNode"]!]!);
            }
            Require(Stories.Values.All(Program.Nodes.ContainsKey), "S2-STORY-DEFINITION");
        }
        internal static Candidate ReadEmbedded()
        {
            using var stream = typeof(Candidate).Assembly.GetManifestResourceStream("StoryS2.candidate.zip")!;
            using var bytes = new MemoryStream(); stream.CopyTo(bytes);
            return new Candidate(bytes.ToArray(), CandidateIdentity.Sha256);
        }
        internal static Candidate ReadForTest(byte[] bytes, string sha) => new Candidate(bytes, sha);
        internal static string Hash(byte[] bytes)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "");
        }
        internal static void Require(bool condition, string code)
        {
            if (!condition) throw new InvalidOperationException(code);
        }
        internal Yarn.Program Merge(Yarn.Program original)
        {
            Require(!Program.Nodes.Keys.Intersect(original.Nodes.Keys).Any(), "S2-NATIVE-NODE-COLLISION");
            Require(!Program.InitialValues.Keys.Intersect(original.InitialValues.Keys).Any(), "S2-NATIVE-VARIABLE-COLLISION");
            var nativeLines = original.Nodes.Keys.SelectMany(original.LineIDsForNode).ToHashSetCompat();
            Require(!Lines.Keys.Any(nativeLines.Contains), "S2-NATIVE-LINE-COLLISION");
            // Combine shares default Operands; explicitly clone both inputs.
            return Yarn.Program.Combine(original.Clone(), Program.Clone());
        }
        internal string Text(JObject line, string nativeLocale)
        {
            string authorLocale = nativeLocale == "zh" ? "zh-CN" : nativeLocale == "zh-Hant" ? "zh-TW" : nativeLocale == "pt" ? "pt-BR" : nativeLocale;
            return (string?)(line["text"]![authorLocale] ?? line["text"]![BaseLocales[(string)line["packId"]!]])
                ?? throw new InvalidOperationException("S2-LOCALE-FALLBACK");
        }
        internal void ValidateLibrary(Library library)
        {
            foreach (var node in Program.Nodes.Values)
                for (int i = 0; i < node.Instructions.Count; i++)
                {
                    var instruction = node.Instructions[i];
                    if (instruction.Opcode != Op.CallFunc) continue;
                    string name = instruction.Operands[0].StringValue;
                    Require(library.FunctionExists(name), "S2-NATIVE-FUNCTION-MISSING:" + name);
                    Require(i > 0 && node.Instructions[i - 1].Opcode == Op.PushFloat &&
                        node.Instructions[i - 1].Operands[0].FloatValue == library.GetFunction(name).Method.GetParameters().Length,
                        "S2-NATIVE-FUNCTION-SIGNATURE:" + name);
                    var signature = library.GetFunction(name).Method;
                    Require(functionSignatures.Contains(name + "(" + string.Join(",", signature.GetParameters().Select(p => p.ParameterType.Name)) + ")->" + signature.ReturnType.Name),
                        "S2-NATIVE-FUNCTION-SIGNATURE:" + name);
                }
        }
        internal void ValidateStorage(IVariableStorage storage)
        {
            foreach (var pair in Program.InitialValues)
            {
                try
                {
                    switch (pair.Value.ValueCase)
                    {
                        case Operand.ValueOneofCase.BoolValue: storage.TryGetValue<bool>(pair.Key, out _); break;
                        case Operand.ValueOneofCase.FloatValue: storage.TryGetValue<float>(pair.Key, out _); break;
                        case Operand.ValueOneofCase.StringValue: storage.TryGetValue<string>(pair.Key, out _); break;
                        default: throw new InvalidOperationException("S2-DEFAULT-TYPE:" + pair.Key);
                    }
                }
                catch (InvalidCastException error) { throw new InvalidOperationException("S2-STORAGE-TYPE:" + pair.Key, error); }
            }
        }
    }
    internal static class SetExtensions
    {
        internal static HashSet<string> ToHashSetCompat(this System.Collections.Generic.IEnumerable<string> input) => new HashSet<string>(input, StringComparer.Ordinal);
    }
}
