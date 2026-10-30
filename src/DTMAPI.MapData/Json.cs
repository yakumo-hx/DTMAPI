using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DTMAPI.MapData
{
    internal sealed class InputException : Exception
    {
        internal InputException(string field, string message, string code = "invalid-shape") : base(message) { Field = field; Code = code; }
        internal string Field { get; }
        internal string Code { get; }
    }
    internal static class Json
    {
        internal static Dictionary<string, object?> D(object? o) => (Dictionary<string, object?>)o!;
        internal static List<object?> A(object? o) => (List<object?>)o!;
        internal static object? Get(object? o, string key) => D(o).TryGetValue(key, out var v) ? v : null;
        internal static string S(object? o, string key) => (string)Get(o, key)!;
        internal static int I(object? o, string key) => (int)Convert.ToDouble(Get(o, key), CultureInfo.InvariantCulture);
        internal static double N(object? o, string key) => Convert.ToDouble(Get(o, key), CultureInfo.InvariantCulture);
        internal static bool B(object? o, string key) => Get(o, key) is bool b && b;
        internal static Dictionary<string, object?> Map(params object?[] pairs)
        {
            var d = new Dictionary<string, object?>(StringComparer.Ordinal);
            for (int i = 0; i < pairs.Length; i += 2) d.Add((string)pairs[i]!, pairs[i + 1]);
            return d;
        }
        internal static object? Read(string text)
        {
            var o = Internal.StrictJson.Read(text, (code, path, message) => new InputException(code == "InvalidJson" ? "$" : path.StartsWith("$.", StringComparison.Ordinal) ? path.Substring(2) : path, message, code == "InvalidJson" ? "invalid-json" : "invalid-shape"), 16 * 1048576);
            Safe(o, "$", 0); return o;
        }
        private static void Safe(object? o, string path, int depth)
        {
            if (depth > 32) throw new InputException(path, "Nesting exceeds 32 levels.");
            bool ValidString(string s)
            {
                for (int i = 0; i < s.Length; i++) { if (char.IsHighSurrogate(s[i])) { if (++i >= s.Length || !char.IsLowSurrogate(s[i])) return false; } else if (char.IsLowSurrogate(s[i])) return false; }
                return true;
            }
            if (o is string text && !ValidString(text)) throw new InputException(path, "Unpaired Unicode surrogate.");
            if (o is Dictionary<string, object?> d) foreach (var p in d)
            {
                if (!ValidString(p.Key)) throw new InputException(path, "Unpaired Unicode in key.");
                string field = path + "." + p.Key;
                if (p.Key == "__proto__" || p.Key == "prototype" || p.Key == "constructor") throw new InputException(field, "Prototype-related fields are forbidden.");
                Safe(p.Value, field, depth + 1);
            }
            else if (o is List<object?> a) for (int i = 0; i < a.Count; i++) Safe(a[i], path + "[" + i + "]", depth + 1);
        }
        internal static string Write(object? value)
        {
            var text = new StringBuilder(); WriteValue(text, value); return text + "\n";
        }
        private static void WriteValue(StringBuilder s, object? v)
        {
            if (v == null) { s.Append("null"); return; }
            if (v is string text)
            {
                s.Append('"'); foreach (char c in text) { if (c == '"' || c == '\\') s.Append('\\').Append(c); else if (c < 32 || char.IsSurrogate(c)) s.Append("\\u").Append(((int)c).ToString("x4")); else s.Append(c); } s.Append('"'); return;
            }
            if (v is bool b) { s.Append(b ? "true" : "false"); return; }
            if (v is IDictionary<string, object?> d)
            {
                s.Append('{'); bool first = true;
                foreach (var p in d.OrderBy(p => p.Key, StringComparer.Ordinal)) { if (!first) s.Append(','); first = false; WriteValue(s, p.Key); s.Append(':'); WriteValue(s, p.Value); }
                s.Append('}'); return;
            }
            if (v is IEnumerable rows) { s.Append('['); bool first = true; foreach (var item in rows) { if (!first) s.Append(','); first = false; WriteValue(s, item); } s.Append(']'); return; }
            s.Append(v is double number ? number.ToString("R", CultureInfo.InvariantCulture) : Convert.ToString(v, CultureInfo.InvariantCulture));
        }
    }
    internal static class Schema
    {
        private static readonly Dictionary<string, object?> Contract = Load();
        private static Dictionary<string, object?> Load()
        {
            using (var stream = typeof(Schema).Assembly.GetManifestResourceStream("MapContract.json"))
            using (var reader = new StreamReader(stream!)) return Json.D(Internal.StrictJson.Read(reader.ReadToEnd()));
        }
        internal static void Check(object? value, string? name = null, string path = "") => Walk(value, name == null ? Contract : Json.D(Json.Get(Contract, "$defs"))[name], path);
        private static bool Match(string text, string pattern) => Regex.IsMatch(text, pattern, RegexOptions.CultureInvariant) && !text.EndsWith("\n", StringComparison.Ordinal);
        private static void Walk(object? value, object? shape, string path)
        {
            var s = Json.D(shape); if (s.TryGetValue("$ref", out var reference)) s = Json.D(Json.D(Json.Get(Contract, "$defs"))[(string)reference!]);
            void Fail(string? at = null) => throw new InputException(at ?? (path.Length == 0 ? "$" : path), "Invalid field type, value or limit.");
            string type = value == null ? "null" : value is Dictionary<string, object?> ? "object" : value is List<object?> ? "array" : value is string ? "string" : value is bool ? "boolean" : "number";
            if (s.TryGetValue("type", out var t))
            {
                var types = t is string one ? new[] { one } : Json.A(t).Cast<string>();
                if (!types.Any(k => k == type || k == "integer" && value is double d && Math.Truncate(d) == d && Math.Abs(d) <= 9007199254740991d)) Fail();
            }
            if (s.TryGetValue("enum", out var choices) && !Json.A(choices).Any(c => Equals(c, value))) Fail();
            if (value is double num && (double.IsNaN(num) || double.IsInfinity(num) || s.ContainsKey("minimum") && num < Json.N(s, "minimum") || s.ContainsKey("maximum") && num > Json.N(s, "maximum"))) Fail();
            if (value is string str && (s.ContainsKey("maxLength") && str.Length > Json.I(s, "maxLength") || s.ContainsKey("pattern") && !Match(str, Json.S(s, "pattern")))) Fail();
            if (value is List<object?> a)
            {
                if (s.ContainsKey("minItems") && a.Count < Json.I(s, "minItems") || s.ContainsKey("maxItems") && a.Count > Json.I(s, "maxItems")) Fail();
                if (s.TryGetValue("items", out var item)) for (int i = 0; i < a.Count; i++) Walk(a[i], item, path + "[" + i + "]");
            }
            if (value is Dictionary<string, object?> obj)
            {
                if (s.ContainsKey("maxProperties") && obj.Count > Json.I(s, "maxProperties")) Fail();
                string Field(string key) => path.Length == 0 ? key : path + "." + key;
                if (s.TryGetValue("required", out var required)) foreach (string key in Json.A(required).Cast<string>()) if (!obj.ContainsKey(key)) Fail(Field(key));
                var properties = s.TryGetValue("properties", out var ps) ? Json.D(ps) : Json.Map();
                foreach (var p in obj)
                {
                    if (s.ContainsKey("keyPattern") && !Match(p.Key, Json.S(s, "keyPattern"))) Fail(Field(p.Key));
                    if (properties.TryGetValue(p.Key, out var child) || s.TryGetValue("values", out child)) Walk(p.Value, child, Field(p.Key));
                }
            }
        }
    }
}
