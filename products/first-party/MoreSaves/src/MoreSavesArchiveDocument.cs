using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DTMAPI.MoreSaves
{
    // A loss check around the native codec, not another archive codec or repair path.
    internal static class MoreSavesArchiveDocument
    {
        private static readonly Regex Literals = new Regex("\"(?:\\\\.|[^\"\\\\])*\"|(?<number>-?(?:0|[1-9][0-9]*)(?:\\.[0-9]+)?(?:[Ee][+-]?[0-9]+)?)", RegexOptions.CultureInvariant);

        internal static void AssertCopy(string sourceJson, string copyJson, int sourceIndex, int targetIndex)
            => AssertChange(sourceJson, copyJson, sourceIndex, targetIndex, null);

        internal static JObject WithName(string sourceJson, string storedName)
        {
            JObject document;
            using (var reader = Reader(sourceJson)) document = JObject.Load(reader);
            JToken? body = document["farmData"]?["agentData"]?["customPlayerName"];
            JToken? summary = document["baseData"]?["customPlayerName"];
            if (body?.Type != JTokenType.String || summary?.Type != JTokenType.String)
                throw new InvalidDataException("Both existing name fields must be strings.");
            body.Replace(new JValue(storedName));
            summary.Replace(new JValue(storedName));
            return document;
        }

        internal static void AssertRename(string sourceJson, string renamedJson, string storedName)
            => AssertChange(sourceJson, renamedJson, -1, -1, storedName);

        private static void AssertChange(string sourceJson, string copyJson, int sourceIndex, int targetIndex, string? storedName)
        {
            using (var before = Reader(sourceJson))
            using (var after = Reader(copyJson))
            using (var sourceNumbers = Numbers(sourceJson).GetEnumerator())
            using (var copyNumbers = Numbers(copyJson).GetEnumerator())
            {
                int indexes = 0, names = 0;
                while (before.Read())
                {
                    if (!after.Read() || before.TokenType != after.TokenType || before.Depth != after.Depth || before.Path != after.Path)
                        throw new InvalidDataException("The native codec changed the archive structure.");
                    JsonToken kind = before.TokenType;
                    if (before.Depth == 0 && kind != JsonToken.StartObject && kind != JsonToken.EndObject)
                        throw new InvalidDataException("An archive must be a JSON object.");
                    if (kind == JsonToken.Integer || kind == JsonToken.Float)
                    {
                        if (!sourceNumbers.MoveNext() || !copyNumbers.MoveNext()) throw new InvalidDataException("Unsupported number representation.");
                        bool index = storedName == null && ((before.Depth == 1 && before.Path == "archiveIndex") ||
                            (before.Depth == 2 && before.Path == "baseData.archiveIndex"));
                        if (index)
                        {
                            if (kind != JsonToken.Integer || Convert.ToInt64(before.Value, CultureInfo.InvariantCulture) != sourceIndex ||
                                Convert.ToInt64(after.Value, CultureInfo.InvariantCulture) != targetIndex)
                                throw new InvalidDataException("The archive index does not match its selected slot.");
                            indexes++;
                        }
                        else if (!string.Equals(sourceNumbers.Current, copyNumbers.Current, StringComparison.OrdinalIgnoreCase))
                            // Compare original lexemes as well as reader tokens: parsing doubles alone can hide precision loss.
                            throw new InvalidDataException("The native codec changed a non-index number.");
                    }
                    else if (kind == JsonToken.String || kind == JsonToken.PropertyName || kind == JsonToken.Boolean || kind == JsonToken.Null)
                    {
                        bool name = storedName != null && kind == JsonToken.String &&
                            ((before.Depth == 3 && before.Path == "farmData.agentData.customPlayerName") ||
                             (before.Depth == 2 && before.Path == "baseData.customPlayerName"));
                        if (name)
                        {
                            if (!Equals(after.Value, storedName)) throw new InvalidDataException("The committed name is not the requested name.");
                            names++;
                        }
                        else if (!Equals(before.Value, after.Value)) throw new InvalidDataException("The native codec changed an unrelated value.");
                    }
                    else if (kind != JsonToken.StartObject && kind != JsonToken.EndObject && kind != JsonToken.StartArray && kind != JsonToken.EndArray)
                        throw new InvalidDataException("Unsupported JSON token in the archive.");
                }
                if (after.Read() || sourceNumbers.MoveNext() || copyNumbers.MoveNext() || (storedName == null ? indexes != 2 : names != 2))
                    throw new InvalidDataException("Both original target fields are required exactly once.");
            }
        }

        private static JsonTextReader Reader(string json) => new JsonTextReader(new StringReader(json))
        { DateParseHandling = DateParseHandling.None, FloatParseHandling = FloatParseHandling.Double, MaxDepth = 256 };

        private static IEnumerable<string> Numbers(string json)
        {
            foreach (Match match in Literals.Matches(json))
                if (match.Groups["number"].Success) yield return match.Value;
        }
    }
}
