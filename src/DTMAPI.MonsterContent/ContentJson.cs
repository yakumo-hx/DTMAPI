using System;
using System.Collections.Generic;
using System.Linq;

namespace DTMAPI.MonsterContent
{
    internal sealed class ContentJson
    {
        private readonly Func<string, string, string, Exception> errorFactory;
        internal ContentJson(Func<string, string, string, Exception> errorFactory) => this.errorFactory = errorFactory;
        internal Exception Error(string code, string field, string message) => errorFactory(code, field, message);
        internal ContentJson MapPath(Func<string, string> map) => new ContentJson((code, field, message) => Error(code, map(field), message));

        internal object? Read(string text)
        {
            // Embedded author resources are bounded by the CLR's representation, not a project quota.
            return DTMAPI.Internal.StrictJson.Read(text, (code, path, message) => Error(code, path, message), maximumBytes: int.MaxValue);
        }
        internal Dictionary<string, object?> Object(object? value, string path, string required, string optional = "")
        {
            if (!(value is Dictionary<string, object?> dict)) throw Error("InvalidShape", path, "Expected an object.");
            var must = required.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var allowed = new HashSet<string>(must.Concat(optional.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)), StringComparer.Ordinal);
            foreach (string key in dict.Keys) if (!allowed.Contains(key)) throw Error("UnknownField", path + "." + key, "Unknown field; input has not been altered.");
            foreach (string key in must) if (!dict.ContainsKey(key)) throw Error("MissingField", path + "." + key, "Required field is missing.");
            return dict;
        }
        internal List<object?> Array(object? value, string path) => value is List<object?> list ? list : throw Error("InvalidShape", path, "Expected an array.");
        internal string Text(object? value, string path) => value is string text ? text : throw Error("InvalidType", path, "Expected text.");
        internal bool Bool(object? value, string path) => value is bool result ? result : throw Error("InvalidType", path, "Expected boolean.");
        internal double Number(object? value, string path) => value is double d && !double.IsInfinity(d) && !double.IsNaN(d) ? d : throw Error("InvalidNumber", path, "Expected finite number.");
        internal int Int(object? value, string path)
        { double n = Number(value, path); if (n != Math.Truncate(n) || n < int.MinValue || n > int.MaxValue) throw Error("InvalidNumber", path, "Expected 32-bit integer."); return (int)n; }
        internal int? NullableInt(object? value, string path) => value == null ? (int?)null : Int(value, path);
        internal string Choice(object? value, string path, params string[] choices)
        { var s = Text(value, path); if (!choices.Contains(s)) throw Error("UnknownEnum", path, "Unknown value: " + s); return s; }
    }
}
