using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using DolocTown;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DTMAPI.Internal.MesStorageProbe;

public sealed partial class ModEntry
{
    bool postCommitFault;
    static void AfterLocalCommit(bool __result)
    {
        if (self?.postCommitFault == true && __result)
            throw new IOException("MES experiment: native file committed, notification failed");
    }
    static string Digest(byte[] data)
    {
        using var hash = SHA256.Create();
        return BitConverter.ToString(hash.ComputeHash(data)).Replace("-", "");
    }
    string DiskValue(string key) => (string)ReadDisk()["cityData"]!["dialogueManager"]!["variableStorage"]!["variables"]![key]!;
    IEnumerable<int> MigrationPhase()
    {
        // This is a private method experiment. Product migration has not been installed.
        Check(File.Exists(Path.Combine(evidence, "verify-result.json")), "carrier cold verification precedes migration experiment");
        var sources = (JArray)JObject.Parse(File.ReadAllText(Path.Combine(evidence, "migration-reader-results.json")))["sources"]!;
        string legacyRoot = Path.Combine(persistent, "mes-experiment-legacy");
        Directory.CreateDirectory(legacyRoot);
        var expected = new Dictionary<string, string>();
        foreach (JObject source in sources)
        {
            string name = (string)source["name"]!;
            Check(name == "schema3" || name == "schema4" || name == "global", "bounded source name");
            string key = "$DTMAPI__MES_migration_" + name;
            string path = Path.Combine(legacyRoot, name + ".json");
            string digest = (string)source["sha256"]!;
            var envelope = new JObject { ["schema"] = 1, ["owner"] = "DTMAPI.MoreEquipmentSlotsMod", ["importedSourceSha256"] = digest, ["slots"] = JObject.Parse((string)source["payload"]!)["slots"]!.DeepClone() };
            string value = envelope.ToString(Formatting.None);
            expected[key] = value;
            if (phase == "migrate")
            {
                Check(!Storage.Contains(key) && !File.Exists(path), "new bounded migration input " + name);
                File.WriteAllText(path, (string)source["raw"]!, new UTF8Encoding(false));
                Check(Digest(File.ReadAllBytes(path)).Equals(digest, StringComparison.OrdinalIgnoreCase), "exact source digest " + name);
                Storage.SetValue(key, value);
            }
            else
            {
                // Embedded data wins; leaving the old file cannot trigger another import.
                Check(Storage.TryGetValue<string>(key, out string read) && read == value && DiskValue(key) == value, "cold native data wins over old residue " + name);
                Check(File.Exists(path) && Digest(File.ReadAllBytes(path)).Equals(digest, StringComparison.OrdinalIgnoreCase), "old source survived interrupted cleanup " + name);
            }
        }
        if (phase == "migrate")
        {
            string before = Digest(File.ReadAllBytes(SavePath));
            int beforeEvents = saves;
            // Real OS sharing violation before native replacement, not a forged SaveSaved.
            using (var denyReplacement = File.Open(SavePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                allowSave = true;
                try { Check(!DolocAPI.SaveGame(4), "real native write failure returns false"); }
                finally { allowSave = false; }
            }
            Check(Digest(File.ReadAllBytes(SavePath)) == before && saves == beforeEvents, "failed native write preserves current and emits no SaveSaved");
            foreach (JObject source in sources) Check(File.Exists(Path.Combine(legacyRoot, (string)source["name"]! + ".json")), "failed save retains old source");
            harmony.Patch(AccessTools.Method(Handler.GetType(), "SaveGame"), postfix: new HarmonyMethod(typeof(ModEntry), nameof(AfterLocalCommit)));
            allowSave = true; postCommitFault = true;
            bool threw = false;
            try { DolocAPI.SaveGame(4); }
            catch (Exception ex) { threw = true; Log("observed post-commit exception " + ex.GetType().Name); }
            finally { allowSave = false; postCommitFault = false; }
            Check(threw && saves == beforeEvents, "outer exception cannot be interpreted as native rollback");
            foreach (var pair in expected) Check(DiskValue(pair.Key) == pair.Value, "native file contains exact payload despite outer exception " + pair.Key);
            // Deliberately exit before cleanup; the next independent process checks this window.
            Check(Directory.GetFiles(legacyRoot).Length == sources.Count, "commit-to-cleanup interruption preserves all legacy sources");
        }
        else
        {
            foreach (JObject source in sources)
            {
                string name = (string)source["name"]!, path = Path.Combine(legacyRoot, name + ".json");
                if (name == "global") { Log("preserved global source: experiment does not grant whole-file attribution"); continue; }
                string key = "$DTMAPI__MES_migration_" + name;
                string original = File.ReadAllText(path);
                // A changed source must not be deleted using an earlier import receipt.
                File.AppendAllText(path, " ");
                Check(!CanClean(path, key, expected[key], (string)source["sha256"]!), "changed source retained " + name);
                File.WriteAllText(path, original, new UTF8Encoding(false));
                Check(CanClean(path, key, expected[key], (string)source["sha256"]!), "exact committed source eligible " + name);
                bool failed = false;
                using (var denyDelete = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    try { File.Delete(path); } catch (IOException) { failed = true; }
                Check(failed && File.Exists(path) && DiskValue(key) == expected[key], "cleanup failure leaves committed data and source intact " + name);
                File.Delete(path);
                Check(!File.Exists(path) && DiskValue(key) == expected[key], "cleanup retry deletes only exact imported file " + name);
            }
            Check(File.Exists(Path.Combine(legacyRoot, "global.json")), "unproven global remainder retained");
            var slots = (JArray)JObject.Parse(expected["$DTMAPI__MES_migration_schema4"])["slots"]!;
            var shield = (ItemHatShield)DolocAPI.GenerateItem((string)slots[0]["itemId"]!, 1); shield.SetShieldValue((int)slots[0]["shieldValue"]!);
            var herb = (ItemHerbPackage)DolocAPI.GenerateItem((string)slots[1]["itemId"]!, 1); herb.SetFilled((bool)slots[1]["itemState"]!["filled"]!);
            var cooldown = (ItemCooldown)DolocAPI.GenerateItem((string)slots[2]["itemId"]!, 1); cooldown.cdDuration = (int)slots[2]["itemState"]!["cooldown"]!;
            Check(shield.ShieldValue == 3 && !herb.isFilled && cooldown.cdDuration == 144, "native item rehydration preserves shield/herb/cooldown without equipment activation");
        }
        DumpDisk(phase);
        File.WriteAllText(Path.Combine(evidence, phase + "-result.json"), JsonConvert.SerializeObject(new { passed = true, phase, loads, saves, expected, limit = "explicitly attributed fixture sources; no production migrator or automatic historical ownership claim" }, Formatting.Indented));
        Log("PASS phase=" + phase + "; loads=" + loads + "; saves=" + saves);
        yield break;
    }
    bool CanClean(string path, string key, string value, string digest) =>
        File.Exists(path) && DiskValue(key) == value && Digest(File.ReadAllBytes(path)).Equals(digest, StringComparison.OrdinalIgnoreCase);
}
