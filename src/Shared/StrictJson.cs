using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace DTMAPI.Internal
{
    // Linked source; no Runtime dependency. Domain callers preserve their own diagnostics.
    internal static class StrictJson
    {
        internal const int MaximumBytes = 12 * 1024 * 1024;
        internal static object? Read(string text, Func<string, string, string, Exception>? errorFactory = null, int maximumBytes = MaximumBytes)
        {
            errorFactory = errorFactory ?? ((code, path, message) => new InvalidDataException(code + ": " + path + ": " + message));
            if (Encoding.UTF8.GetByteCount(text) > maximumBytes) throw errorFactory("ProjectTooLarge", "$", "JSON exceeds " + (maximumBytes / (1024 * 1024)) + " MiB.");
            return new Reader(text, errorFactory).Document();
        }
        private sealed class Reader
        {
            private readonly string text;
            private int position;
            private readonly Func<string, string, string, Exception> errorFactory;
            internal Reader(string text, Func<string, string, string, Exception> errorFactory) { this.text = text; this.errorFactory = errorFactory; }
            private char Current => position < text.Length ? text[position] : '\0';
            private static bool Digit(char c) => c >= '0' && c <= '9';
            private Exception Error(string path, string message, string code = "InvalidJson") =>
                errorFactory(code, path, "JSON character " + (position + 1) + ": " + message);
            private bool Take(char c)
            {
                if (position >= text.Length || text[position] != c) return false;
                position++; return true;
            }
            private void Whitespace()
            {
                while (Current == ' ' || Current == '\t' || Current == '\r' || Current == '\n') position++;
            }
            internal object? Document()
            {
                // A file may start with one BOM; further BOMs are not JSON whitespace.
                Take('\uFEFF');
                var value = Value("$", 1);
                Whitespace();
                if (position != text.Length) throw Error("$", "Unexpected content after the root value.");
                return value;
            }
            private object? Value(string path, int depth)
            {
                Whitespace();
                if (depth > 64) throw Error(path, "JSON exceeds 64 levels.");
                if (position == text.Length) throw Error(path, "Expected a JSON value.");
                if (Take('{'))
                {
                    var result = new Dictionary<string, object?>(StringComparer.Ordinal);
                    Whitespace();
                    if (Take('}')) return result;
                    while (true)
                    {
                        Whitespace();
                        if (Current != '"') throw Error(path, "Expected a quoted property name.");
                        string key = String(path), child = path + "." + key;
                        if (result.ContainsKey(key)) throw Error(child, "Duplicate JSON property.", "DuplicateField");
                        Whitespace();
                        if (!Take(':')) throw Error(child, "Expected ':' after the property name.");
                        result.Add(key, Value(child, depth + 1));
                        Whitespace();
                        if (Take('}')) return result;
                        if (!Take(',')) throw Error(path, "Expected ',' or '}'.");
                    }
                }
                if (Take('['))
                {
                    var result = new List<object?>();
                    Whitespace();
                    if (Take(']')) return result;
                    while (true)
                    {
                        result.Add(Value(path + "[" + result.Count + "]", depth + 1));
                        Whitespace();
                        if (Take(']')) return result;
                        if (!Take(',')) throw Error(path, "Expected ',' or ']'.");
                    }
                }
                if (Current == '"') return String(path);
                if (Current == 't') { Literal("true", path); return true; }
                if (Current == 'f') { Literal("false", path); return false; }
                if (Current == 'n') { Literal("null", path); return null; }
                if (Current == '-' || Digit(Current)) return NumberValue(path);
                throw Error(path, "Expected a JSON value.");
            }
            private void Literal(string literal, string path)
            {
                foreach (char c in literal) if (!Take(c)) throw Error(path, "Invalid JSON literal.");
            }
            private string String(string path)
            {
                position++; // Opening quote was checked by the caller.
                var result = new StringBuilder();
                while (position < text.Length)
                {
                    char c = text[position++];
                    if (c == '"') return result.ToString();
                    if (c < 32) throw Error(path, "Unescaped control character in string.");
                    if (c != '\\') { result.Append(c); continue; }
                    if (position == text.Length) throw Error(path, "Unterminated escape.");
                    switch (text[position++])
                    {
                        case '"': result.Append('"'); break;
                        case '\\': result.Append('\\'); break;
                        case '/': result.Append('/'); break;
                        case 'b': result.Append('\b'); break;
                        case 'f': result.Append('\f'); break;
                        case 'n': result.Append('\n'); break;
                        case 'r': result.Append('\r'); break;
                        case 't': result.Append('\t'); break;
                        case 'u':
                            int value = 0;
                            for (int j = 0; j < 4; j++)
                            {
                                char hex = Current;
                                int digit = Digit(hex) ? hex - '0' : hex >= 'a' && hex <= 'f' ? hex - 'a' + 10 : hex >= 'A' && hex <= 'F' ? hex - 'A' + 10 : -1;
                                if (digit < 0) throw Error(path, "Expected four hexadecimal digits after '\\u'.");
                                value = value * 16 + digit; position++;
                            }
                            result.Append((char)value); break;
                        default: throw Error(path, "Invalid string escape.");
                    }
                }
                throw Error(path, "Unterminated string.");
            }
            private double NumberValue(string path)
            {
                int start = position;
                Take('-');
                if (Take('0'))
                {
                    if (Digit(Current)) throw Error(path, "Leading zero in number.");
                }
                else
                {
                    if (Current < '1' || Current > '9') throw Error(path, "Expected a digit.");
                    while (Digit(Current)) position++;
                }
                if (Take('.'))
                {
                    if (!Digit(Current)) throw Error(path, "Expected a digit after '.'.");
                    while (Digit(Current)) position++;
                }
                if (Take('e') || Take('E'))
                {
                    if (!Take('+')) Take('-');
                    if (!Digit(Current)) throw Error(path, "Expected an exponent digit.");
                    while (Digit(Current)) position++;
                }
                if (!double.TryParse(text.Substring(start, position - start), NumberStyles.Float, CultureInfo.InvariantCulture, out double value) || double.IsNaN(value) || double.IsInfinity(value))
                    throw Error(path, "Expected a finite JSON number.", "InvalidNumber");
                return value;
            }
        }
    }
}
