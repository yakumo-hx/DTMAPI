using System;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DTMAPI.MoreSaves
{
    // Card summaries and checks around the native copy/rename codec.
    internal static class MoreSavesArchiveDocument
    {
        internal static T? ReadSummary<T>(string json) where T : class
        {
            using (var reader = new JsonTextReader(new StringReader(json)))
            {
                do { if (!reader.Read()) throw new InvalidDataException("An archive must be a JSON object."); }
                while (reader.TokenType == JsonToken.Comment);
                if (reader.TokenType != JsonToken.StartObject)
                    throw new InvalidDataException("An archive must be a JSON object.");
                JToken? summary = null;
                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.PropertyName && reader.Depth == 1)
                    {
                        bool isSummary = Equals(reader.Value, "baseData");
                        do { if (!reader.Read()) throw new InvalidDataException("Incomplete archive property."); }
                        while (reader.TokenType == JsonToken.Comment);
                        if (isSummary) summary = JToken.ReadFrom(reader);
                        else reader.Skip();
                    }
                    if (reader.TokenType == JsonToken.EndObject && reader.Depth == 0)
                    {
                        while (reader.Read())
                            if (reader.TokenType != JsonToken.Comment)
                                throw new InvalidDataException("Unexpected content after the archive.");
                        return summary?.ToObject<T>();
                    }
                }
                throw new InvalidDataException("Incomplete archive.");
            }
        }

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
                        bool index = storedName == null && ((before.Depth == 1 && before.Path == "archiveIndex") ||
                            (before.Depth == 2 && before.Path == "baseData.archiveIndex"));
                        if (index)
                        {
                            if (kind != JsonToken.Integer || Convert.ToInt64(before.Value, CultureInfo.InvariantCulture) != sourceIndex ||
                                Convert.ToInt64(after.Value, CultureInfo.InvariantCulture) != targetIndex)
                                throw new InvalidDataException("The archive index does not match its selected slot.");
                            indexes++;
                        }
                        else if (!Equals(before.Value, after.Value))
                            // Compare values, not text: the native writer may print the same double with more digits
                            // (10.4999981 -> 10.499998099999999). Integers are read as long or BigInteger and stay exact.
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
                if (after.Read() || (storedName == null ? indexes != 2 : names != 2))
                    throw new InvalidDataException("Both original target fields are required exactly once.");
            }
        }

        private static JsonTextReader Reader(string json) => new JsonTextReader(new StringReader(json))
        { DateParseHandling = DateParseHandling.None, FloatParseHandling = FloatParseHandling.Double, MaxDepth = 256 };
    }
}
