// Independently implemented JSON span reader and narrowly scoped archive transform.
// This file contains no game implementation, resources, or encryption material.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace DtmApi.PlayerSaveRepair
{
    public sealed class JsonSpan
    {
        public int Start, End;
        public char Kind;
        public string Value;
        public Dictionary<string, JsonSpan> Fields;
        public List<JsonSpan> Items;
        public JsonSpan Get(string name)
        {
            JsonSpan value;
            if (Kind != '{' || !Fields.TryGetValue(name, out value))
                throw new FormatException("Required archive field is missing: " + name);
            return value;
        }
        public void Require(char kind)
        {
            if (Kind != kind) throw new FormatException("Archive field type is unsupported.");
        }
        public int Integer()
        {
            Require('n');
            int value;
            if (!Int32.TryParse(Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value))
                throw new FormatException("Expected an integer archive field.");
            return value;
        }
        public bool Boolean() { Require('b'); return Value == "true"; }
        public string Text() { Require('s'); return Value; }
    }

    // Ordinal property names, duplicate rejection and offsets are deliberate:
    // round-tripping through an object serializer would change unrelated bytes.
    public sealed class JsonReader
    {
        readonly string text;
        int position, nodes;
        JsonReader(string value) { text = value; }
        public static JsonSpan Parse(string value)
        {
            var reader = new JsonReader(value);
            JsonSpan result = reader.Read(0);
            reader.Space();
            if (reader.position != value.Length) throw new FormatException("Trailing JSON data.");
            return result;
        }
        void Space()
        {
            while (position < text.Length && (text[position] == ' ' || text[position] == '\t' ||
                text[position] == '\n' || text[position] == '\r')) position++;
        }
        bool Take(char value)
        {
            Space();
            if (position < text.Length && text[position] == value) { position++; return true; }
            return false;
        }
        void Expect(char value) { if (!Take(value)) throw new FormatException("Invalid JSON structure."); }
        string String()
        {
            Expect('"');
            var result = new StringBuilder();
            while (position < text.Length)
            {
                char ch = text[position++];
                if (ch == '"') return result.ToString();
                if (ch < 32) throw new FormatException("JSON string contains a control character.");
                if (ch != '\\') { result.Append(ch); continue; }
                if (position == text.Length) break;
                ch = text[position++];
                switch (ch)
                {
                    case '"': case '\\': case '/': result.Append(ch); break;
                    case 'b': result.Append('\b'); break;
                    case 'f': result.Append('\f'); break;
                    case 'n': result.Append('\n'); break;
                    case 'r': result.Append('\r'); break;
                    case 't': result.Append('\t'); break;
                    case 'u':
                        if (position + 4 > text.Length) throw new FormatException("Incomplete JSON escape.");
                        ushort code;
                        if (!UInt16.TryParse(text.Substring(position, 4), NumberStyles.HexNumber,
                            CultureInfo.InvariantCulture, out code)) throw new FormatException("Invalid JSON escape.");
                        result.Append((char)code); position += 4; break;
                    default: throw new FormatException("Invalid JSON escape.");
                }
            }
            throw new FormatException("Unterminated JSON string.");
        }
        JsonSpan Read(int depth)
        {
            if (depth > 128 || ++nodes > 1000000) throw new FormatException("JSON complexity limit exceeded.");
            Space();
            if (position == text.Length) throw new FormatException("Incomplete JSON.");
            var result = new JsonSpan { Start = position, Kind = text[position] };
            if (Take('{'))
            {
                result.Fields = new Dictionary<string, JsonSpan>(StringComparer.Ordinal);
                var propertyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (!Take('}'))
                {
                    do
                    {
                        string name = String(); Expect(':');
                        // Json.NET can bind case aliases to the same member. Such
                        // ambiguity cannot establish a native slot identity.
                        if (!propertyNames.Add(name)) throw new FormatException("Duplicate or case-ambiguous JSON property.");
                        result.Fields.Add(name, Read(depth + 1));
                    } while (Take(','));
                    Expect('}');
                }
            }
            else if (Take('['))
            {
                result.Items = new List<JsonSpan>();
                if (!Take(']'))
                {
                    do { result.Items.Add(Read(depth + 1)); } while (Take(','));
                    Expect(']');
                }
            }
            else if (text[position] == '"') { result.Kind = 's'; result.Value = String(); }
            else
            {
                int start = position;
                while (position < text.Length && ",]} \t\r\n".IndexOf(text[position]) < 0) position++;
                result.Value = text.Substring(start, position - start);
                if (result.Value == "true" || result.Value == "false") result.Kind = 'b';
                else if (result.Value == "null") result.Kind = '0';
                else if (Regex.IsMatch(result.Value, @"\A-?(?:0|[1-9][0-9]*)(?:\.[0-9]+)?(?:[eE][+-]?[0-9]+)?\z")) result.Kind = 'n';
                else throw new FormatException("Invalid JSON literal.");
            }
            result.End = position;
            return result;
        }
    }

    public sealed class RepairPlan
    {
        public string Status, Reason, SaveVersion;
        public int SlotIndex, EmailCount, InsertionByteOffset;
        public byte[] Insertion;
        public RepairPlan Stop(string status, string reason) { Status = status; Reason = reason; return this; }
    }

    public static class ArchiveFormat
    {
        public const string FormatId = "doloc-aes-cbc-utf8-v1";
        public const string RepairMode = "ruinedcity-mail-v1";
        public const int MaxArchiveBytes = 33554432;
        static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
        public static string Sha256(byte[] bytes)
        {
            using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "");
        }
        public static bool Same(byte[] left, byte[] right)
        {
            if (left.Length != right.Length) return false;
            for (int i = 0; i < left.Length; i++) if (left[i] != right[i]) return false;
            return true;
        }
        public static byte[] Decode(byte[] file, byte[] key, byte[] iv)
        {
            if (file.Length < 20 || file.Length > MaxArchiveBytes) throw new FormatException("Archive size is unsupported.");
            string encoded = Utf8.GetString(file);
            const string prefix = "DOLOC-TOWN:";
            if (!encoded.StartsWith(prefix, StringComparison.Ordinal)) throw new FormatException("Unsupported archive envelope.");
            string base64 = encoded.Substring(prefix.Length);
            byte[] cipher = Convert.FromBase64String(base64);
            if (Convert.ToBase64String(cipher) != base64) throw new FormatException("Noncanonical archive envelope.");
            byte[] plain;
            using (var aes = Aes.Create())
            {
                aes.Key = key; aes.IV = iv; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
                using (var transform = aes.CreateDecryptor()) plain = transform.TransformFinalBlock(cipher, 0, cipher.Length);
            }
            if (!Same(Utf8.GetBytes(Utf8.GetString(plain)), plain)) throw new FormatException("Unsupported UTF-8 plaintext.");
            return plain;
        }
        public static byte[] Encode(byte[] plain, byte[] key, byte[] iv)
        {
            if (plain.Length > MaxArchiveBytes) throw new FormatException("Archive size limit exceeded.");
            Utf8.GetString(plain);
            byte[] cipher;
            using (var aes = Aes.Create())
            {
                aes.Key = key; aes.IV = iv; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
                using (var transform = aes.CreateEncryptor()) cipher = transform.TransformFinalBlock(plain, 0, plain.Length);
            }
            return Utf8.GetBytes("DOLOC-TOWN:" + Convert.ToBase64String(cipher));
        }
        static bool Related(string value)
        {
            return value == "ruinedcity_main" || value.StartsWith("ruinedcity_main_", StringComparison.Ordinal) ||
                value.StartsWith("ruinedcity_main@", StringComparison.Ordinal);
        }
        static bool ContainsRelated(JsonSpan value)
        {
            if (value.Kind == 's') return Related(value.Value);
            if (value.Kind == '{')
                foreach (var pair in value.Fields) if (Related(pair.Key) || ContainsRelated(pair.Value)) return true;
            if (value.Kind == '[') foreach (var item in value.Items) if (ContainsRelated(item)) return true;
            return false;
        }
        static bool ContainsText(JsonSpan value, string expected)
        {
            if (value.Kind == 's') return value.Value == expected;
            if (value.Kind == '{') foreach (var pair in value.Fields) if (ContainsText(pair.Value, expected)) return true;
            if (value.Kind == '[') foreach (var item in value.Items) if (ContainsText(item, expected)) return true;
            return false;
        }
        static void ValidateDate(JsonSpan date)
        {
            foreach (string field in new [] { "TotalTUs", "TotalDays", "Minute", "Hour", "Day", "Month", "Year" })
                if (date.Get(field).Integer() < 0) throw new FormatException("Unsupported negative date field.");
            date.Get("WeekDay").Text();
        }
        public static RepairPlan Inspect(byte[] plain)
        {
            string text = Utf8.GetString(plain);
            JsonSpan root = JsonReader.Parse(text);
            var plan = new RepairPlan();
            var baseData = root.Get("baseData");
            plan.SlotIndex = root.Get("archiveIndex").Integer();
            plan.SaveVersion = baseData.Get("version").Text();
            if (plan.SlotIndex != 0 || baseData.Get("archiveIndex").Integer() != 0)
                return plan.Stop("Refused", "NativeSlotIdentityMismatch");
            if (plan.SaveVersion != "1.00.02" && plan.SaveVersion != "1.00.06")
                return plan.Stop("Refused", "UnsupportedSaveVersion");
            var farm = root.Get("farmData");
            var chains = farm.Get("missionChainManager");
            var handles = chains.Get("handles"); handles.Require('{');
            var completed = chains.Get("completedChains"); completed.Require('{');
            var missions = farm.Get("missionManager");
            var finished = missions.Get("finishMissions"); finished.Require('[');
            var decorators = missions.Get("finishDecorators"); decorators.Require('[');
            var active = missions.Get("totalMissions"); active.Require('[');
            // chainInfos is a definition projection, deliberately not active state.
            if (ContainsRelated(handles) || ContainsRelated(completed) || ContainsRelated(finished) || ContainsRelated(decorators) || ContainsRelated(active))
                return plan.Stop("Refused", "RuinedCityTaskAlreadyStartedOrCompleted");
            var emails = farm.Get("emailManager").Get("emails"); emails.Require('[');
            plan.EmailCount = emails.Items.Count;
            JsonSpan prerequisiteMail = null;
            foreach (var mail in emails.Items)
            {
                string id = mail.Get("id").Text();
                var attaches = mail.Get("emailAttaches"); attaches.Require('[');
                if (id == "ruinedcity_continue" || id == "ruinedcity_main" || ContainsRelated(attaches))
                    return plan.Stop("NoRepairNeeded", "RuinedCityMailAlreadyPresentIncludingRecycled");
                if (id == "wetland_main")
                {
                    if (prerequisiteMail != null) return plan.Stop("Refused", "AmbiguousPrerequisiteMail");
                    prerequisiteMail = mail;
                }
            }
            JsonSpan wetland;
            if (!completed.Fields.TryGetValue("wetland_main", out wetland) || wetland.Integer() < 1)
                return plan.Stop("Refused", "WetlandChainNotCompleted");
            for (int i = 0; i <= 6; i++)
                if (!ContainsText(finished, "wetland_main_" + i)) return plan.Stop("Refused", "WetlandMissionsIncomplete");
            // This is the implicit mission ID, not MissionDecorator.DecoratorId.
            // Native completed mission IDs belong to finishMissions.
            if (!ContainsText(finished, "wetland_main@1")) return plan.Stop("Refused", "WetlandMissionsIncomplete");
            if (prerequisiteMail == null || prerequisiteMail.Get("isNew").Boolean() || prerequisiteMail.Get("recycled").Boolean())
                return plan.Stop("Refused", "PrerequisiteMailMissingUnreadOrRecycled");
            var date = root.Get("timeData").Get("dateNow"); ValidateDate(date);
            var baseDate = baseData.Get("dateNow"); ValidateDate(baseDate);
            foreach (string field in new [] { "TotalTUs", "TotalDays", "Minute", "Hour", "Day", "Month", "Year", "WeekDay" })
                if (date.Get(field).Value != baseDate.Get(field).Value) return plan.Stop("Refused", "ArchiveDateMismatch");
            if (date.Get("TotalDays").Integer() <= prerequisiteMail.Get("sendData").Get("TotalDays").Integer())
                return plan.Stop("Refused", "WaitingForNativeNextDay");
            var dialogue = root.Get("cityData").Get("dialogueManager");
            var visits = dialogue.Get("visitedArgs"); visits.Require('{');
            JsonSpan visit;
            if (!visits.Fields.TryGetValue("ruinedcity_main_entsk", out visit) || visit.Integer() < 1)
                return plan.Stop("Refused", "RecoveryDialogueNotVisited");
            if (visits.Fields.TryGetValue("version_patch0900", out visit) && visit.Integer() > 0)
                return plan.Stop("Refused", "RecoveryMigrationAlreadyVisited");
            var pending = dialogue.Get("unhandledDialogueNodes"); pending.Require('[');
            var dialogueData = dialogue.Get("dialogueDatas"); dialogueData.Require('{');
            if (pending.Items.Count != 0 || ContainsText(dialogueData, "ruinedcity_main_entsk"))
                return plan.Stop("Refused", "DialogueStillPending");
            string rawDate = text.Substring(date.Start, date.End - date.Start);
            string mailJson = "{\"isNew\":true,\"emailAttaches\":[{\"$type\":\"DolocTown.EmailAttachMission, Assembly-CSharp\",\"missionChainId\":\"ruinedcity_main\",\"autoAccept\":true,\"isAccept\":false}],\"sendData\":" +
                rawDate + ",\"recycled\":false,\"collected\":false,\"id\":\"ruinedcity_continue\"}";
            plan.InsertionByteOffset = Utf8.GetByteCount(text.Substring(0, emails.Start + 1));
            plan.Insertion = Utf8.GetBytes(mailJson + (emails.Items.Count > 0 ? "," : ""));
            return plan.Stop("Eligible", "KnownMissingRecoveryMail");
        }
        public static byte[] Apply(byte[] source, RepairPlan plan)
        {
            if (plan.Status != "Eligible") throw new InvalidOperationException("Archive is not eligible for this repair.");
            byte[] result = new byte[source.Length + plan.Insertion.Length];
            Buffer.BlockCopy(source, 0, result, 0, plan.InsertionByteOffset);
            Buffer.BlockCopy(plan.Insertion, 0, result, plan.InsertionByteOffset, plan.Insertion.Length);
            Buffer.BlockCopy(source, plan.InsertionByteOffset, result, plan.InsertionByteOffset + plan.Insertion.Length,
                source.Length - plan.InsertionByteOffset);
            return result;
        }
        public static void Verify(byte[] source, byte[] candidate)
        {
            RepairPlan plan = Inspect(source);
            if (!Same(Apply(source, plan), candidate)) throw new InvalidOperationException("Candidate has changes outside the single allowed mail insertion.");
            byte[] restored = new byte[candidate.Length - plan.Insertion.Length];
            Buffer.BlockCopy(candidate, 0, restored, 0, plan.InsertionByteOffset);
            Buffer.BlockCopy(candidate, plan.InsertionByteOffset + plan.Insertion.Length, restored,
                plan.InsertionByteOffset, restored.Length - plan.InsertionByteOffset);
            if (!Same(source, restored)) throw new InvalidOperationException("Removing the allowed insertion did not restore original plaintext bytes.");
            RepairPlan result = Inspect(candidate);
            if (result.Status != "NoRepairNeeded" || result.EmailCount != plan.EmailCount + 1)
                throw new InvalidOperationException("Repaired mail uniqueness or count did not verify.");
        }
    }
}
