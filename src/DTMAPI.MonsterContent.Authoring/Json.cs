using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace DTMAPI.MonsterContent.Authoring
{
    public sealed class AuthoringException : Exception
    {
        public AuthoringException(string code, string field, string message) : base(message) { Diagnostic = new MonsterDiagnostic(code, message, stage: "Parse", field: field); }
        public MonsterDiagnostic Diagnostic { get; }
    }
    /// <summary>Strict JSON tree shared by the compiler and Mono reader.</summary>
    public static class Json
    {
        public static object? Read(string text)
        {
            // Embedded author resources are bounded by the CLR's representation, not a project quota.
            return DTMAPI.Internal.StrictJson.Read(text, (code, path, message) => new AuthoringException(code, path, message), maximumBytes: int.MaxValue);
        }
        public static Dictionary<string, object?> Object(object? value, string path, string required, string optional = "")
        {
            if (!(value is Dictionary<string, object?> dict)) throw new AuthoringException("InvalidShape", path, "Expected an object.");
            var must = required.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var allowed = new HashSet<string>(must.Concat(optional.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)), StringComparer.Ordinal);
            foreach (string key in dict.Keys) if (!allowed.Contains(key)) throw new AuthoringException("UnknownField", path + "." + key, "Unknown field; input has not been altered.");
            foreach (string key in must) if (!dict.ContainsKey(key)) throw new AuthoringException("MissingField", path + "." + key, "Required field is missing.");
            return dict;
        }
        public static List<object?> Array(object? value, string path) => value is List<object?> list ? list : throw new AuthoringException("InvalidShape", path, "Expected an array.");
        public static string Text(object? value, string path) => value is string text ? text : throw new AuthoringException("InvalidType", path, "Expected text.");
        public static bool Bool(object? value, string path) => value is bool result ? result : throw new AuthoringException("InvalidType", path, "Expected boolean.");
        public static double Number(object? value, string path) => value is double d && !double.IsInfinity(d) && !double.IsNaN(d) ? d : throw new AuthoringException("InvalidNumber", path, "Expected finite number.");
        public static int Int(object? value, string path)
        { double n = Number(value, path); if (n != Math.Truncate(n) || n < int.MinValue || n > int.MaxValue) throw new AuthoringException("InvalidNumber", path, "Expected 32-bit integer."); return (int)n; }
        public static int? NullableInt(object? value, string path) => value == null ? (int?)null : Int(value, path);
        public static string Choice(object? value, string path, params string[] choices)
        { var s = Text(value, path); if (!choices.Contains(s)) throw new AuthoringException("UnknownEnum", path, "Unknown value: " + s); return s; }
        public static Dictionary<string, object?> Map(params object?[] pairs)
        { var d = new Dictionary<string, object?>(StringComparer.Ordinal); for (int i = 0; i < pairs.Length; i += 2) d.Add((string)pairs[i]!, pairs[i + 1]); return d; }
        public static string Write(object? value) { var s = new StringBuilder(); WriteValue(s, value); return s + "\n"; }
        private static void WriteValue(StringBuilder s, object? value)
        {
            if (value == null) { s.Append("null"); return; }
            if (value is string text)
            {
                s.Append('"'); foreach (char c in text) { if (c == '"' || c == '\\') s.Append('\\').Append(c); else if (c < 32) s.Append("\\u").Append(((int)c).ToString("x4")); else s.Append(c); } s.Append('"'); return;
            }
            if (value is bool b) { s.Append(b ? "true" : "false"); return; }
            if (value is IDictionary<string, object?> dict)
            {
                s.Append('{'); bool first = true; foreach (var pair in dict.OrderBy(p => p.Key, StringComparer.Ordinal)) { if (!first) s.Append(','); first = false; WriteValue(s, pair.Key); s.Append(':'); WriteValue(s, pair.Value); } s.Append('}'); return;
            }
            if (value is System.Collections.IEnumerable rows) { s.Append('['); bool first = true; foreach (object? item in rows) { if (!first) s.Append(','); first = false; WriteValue(s, item); } s.Append(']'); return; }
            if (value is float f) { if (float.IsNaN(f) || float.IsInfinity(f)) throw new InvalidDataException("Nonfinite number."); s.Append(f.ToString("R", CultureInfo.InvariantCulture)); return; }
            if (value is double d) { if (double.IsNaN(d) || double.IsInfinity(d)) throw new InvalidDataException("Nonfinite number."); s.Append(d.ToString("R", CultureInfo.InvariantCulture)); return; }
            s.Append(System.Convert.ToString(value, CultureInfo.InvariantCulture));
        }
    }
}
