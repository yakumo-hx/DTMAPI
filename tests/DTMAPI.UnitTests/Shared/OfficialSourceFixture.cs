#pragma warning disable CS0618 // Frozen compatibility contracts are intentionally exercised.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {

        private sealed class OfficialModInfoTestEntry
        {
            public OfficialModInfoTestEntry(string officialId, bool enabled, string source, int? priority = -1)
            {
                OfficialId = officialId;
                Enabled = enabled;
                Source = source;
                Priority = priority;
            }

            public string OfficialId { get; }
            public bool Enabled { get; }
            public string Source { get; }
            public int? Priority { get; }
        }

        private static void WriteOfficialModInfos(string persistentRoot, string officialId, bool enabled)
        {
            WriteOfficialModInfos(persistentRoot, new OfficialModInfoTestEntry(officialId, enabled, "Local"));
        }

        private static void WriteOfficialModInfos(string persistentRoot, params OfficialModInfoTestEntry[] entries)
        {
            string saveDir = Path.Combine(persistentRoot, "SAVE");
            Directory.CreateDirectory(saveDir);
            string modInfos = string.Join(", ", entries.Select(entry =>
                "\"" + entry.OfficialId + "\": { \"id\": \"" + entry.OfficialId + "\", \"enabled\": " + (entry.Enabled ? "true" : "false") +
                (entry.Priority.HasValue ? ", \"priority\": " + entry.Priority.Value.ToString(CultureInfo.InvariantCulture) : string.Empty) +
                ", \"source\": \"" + entry.Source + "\", \"title\": \"DTMAPI Test\" }"));
            File.WriteAllText(
                Path.Combine(saveDir, "mod_infos.json"),
                "{ \"modInfos\": { " + modInfos + " } }");
        }
    }
}
