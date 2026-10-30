using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DTMAPI.Internal.MesStorageProbe;

// Bounded method experiment only. The synthetic source-to-archive association is
// declared by the fixture, not inferred from a player's path, name or slot number.
public sealed partial class ModEntry
{
    string cloudKey = "", cloudExpected = "", cloudRoot = "";
    readonly Dictionary<string, byte[]> cloudResidues = new Dictionary<string, byte[]>();
    readonly Dictionary<string, string> cloudProtected = new Dictionary<string, string>();

    void PrepareCloudResidues()
    {
        var input = JObject.Parse(File.ReadAllText(Path.Combine(evidence, "cloud-input.json")));
        cloudKey = (string)input["key"]!;
        cloudExpected = (string)input["committedPayload"]!;
        Check(cloudKey == "$DTMAPI__MES_migration_schema4", "bounded native owner key");
        cloudRoot = Path.Combine(persistent, "mes-cloud-residue-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(cloudRoot);
        string raw = (string)input["localLegacyRaw"]!;
        string importedDigest = (string)JObject.Parse(cloudExpected)["importedSourceSha256"]!;
        foreach (string name in new[] { "slot-4.json", "slot-4.json.previous", "slot-4.candidate.json", "slot-4.journal.json" })
        {
            string path = Path.Combine(cloudRoot, name);
            byte[] bytes = Encoding.UTF8.GetBytes(raw + "\n");
            Check(!Digest(bytes).Equals(importedDigest, StringComparison.OrdinalIgnoreCase), "B residue differs from A imported bytes " + name);
            File.WriteAllBytes(path, bytes);
            cloudResidues.Add(path, bytes);
        }
        foreach (string name in new[] { "slot-5.json", "legacy-global.json", "unproven-recovery.json" })
        {
            string path = Path.Combine(cloudRoot, name);
            File.WriteAllText(path, raw, new UTF8Encoding(false));
            cloudProtected.Add(path, Digest(File.ReadAllBytes(path)));
        }
        Log("B residues exist before native load; no A-machine local receipt or path copied");
    }

    bool CloudArchiveIsCommitted()
    {
        if (!Storage.TryGetValue<string>(cloudKey, out string current) || current != cloudExpected || DiskValue(cloudKey) != current)
            return false;
        var payload = JObject.Parse(current);
        return (int?)payload["schema"] == 1 && (string?)payload["owner"] == "DTMAPI.MoreEquipmentSlotsMod" && payload["slots"] is JArray;
    }

    bool CanCleanCloudResidue(string path, string inspectedDigest) =>
        cloudResidues.ContainsKey(path) && CloudArchiveIsCommitted() && File.Exists(path) &&
        Digest(File.ReadAllBytes(path)).Equals(inspectedDigest, StringComparison.OrdinalIgnoreCase);

    IEnumerable<int> CloudResiduePhase()
    {
        string nativeBefore = Digest(File.ReadAllBytes(SavePath));
        Check(CloudArchiveIsCommitted(), "native cold read selects committed A data despite preexisting conflicting B files");
        var expectedSlots = (JArray)JObject.Parse(cloudExpected)["slots"]!;
        Check((int)expectedSlots[0]["shieldValue"]! == 3 && (bool)expectedSlots[1]["itemState"]!["filled"]! == false,
            "committed shield and herb state remain authoritative");

        string first = Path.Combine(cloudRoot, "slot-4.json");
        string firstDigest = Digest(cloudResidues[first]);
        var unsaved = JObject.Parse(cloudExpected);
        unsaved["slots"] = new JArray();
        Storage.SetValue(cloudKey, unsaved.ToString(Formatting.None));
        try { Check(!CanCleanCloudResidue(first, firstDigest) && File.Exists(first), "memory-only payload cannot authorize cleanup"); }
        finally { Storage.SetValue(cloudKey, cloudExpected); }

        File.AppendAllText(first, " ");
        Check(!CanCleanCloudResidue(first, firstDigest) && File.Exists(first), "B file changed since inspection is retained");
        File.WriteAllBytes(first, cloudResidues[first]); // Restore only this experiment-owned input.

        bool rejected = false;
        Check(CanCleanCloudResidue(first, firstDigest), "B-local snapshot permits cleanup without matching A source hash");
        using (var denyDelete = File.Open(first, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            try { File.Delete(first); }
            catch (IOException) { rejected = true; }
            catch (UnauthorizedAccessException) { rejected = true; }
        }
        Check(rejected && File.Exists(first) && CloudArchiveIsCommitted(), "delete failure preserves archive priority and leaves retryable residue");

        var removed = new List<string>();
        foreach (var pair in cloudResidues)
        {
            Check(CanCleanCloudResidue(pair.Key, Digest(pair.Value)), "accurately attributed B residue eligible " + Path.GetFileName(pair.Key));
            // No concurrent writer exists in this controlled fixture. Production
            // cleanup must hold file-identity protection across check and deletion.
            File.Delete(pair.Key);
            Check(!File.Exists(pair.Key) && CloudArchiveIsCommitted(), "cleanup never replaces or reimports archive data " + Path.GetFileName(pair.Key));
            removed.Add(Path.GetFileName(pair.Key));
        }
        foreach (var pair in cloudProtected)
            Check(!CanCleanCloudResidue(pair.Key, pair.Value) && File.Exists(pair.Key) && Digest(File.ReadAllBytes(pair.Key)) == pair.Value,
                "other archive or unproven source retained " + Path.GetFileName(pair.Key));

        Check(saves == 0 && rejectedSaves == 0 && Digest(File.ReadAllBytes(SavePath)) == nativeBefore, "cloud residue cleanup performs no native save and changes no archive bytes");
        File.WriteAllText(Path.Combine(evidence, "cloud-residue-result.json"), JsonConvert.SerializeObject(new {
            passed = true, phase, loads, saves, rejectedSaves, nativeBefore, cloudKey, cloudRoot, removed,
            protectedFiles = cloudProtected, committedPayload = cloudExpected,
            limit = "Native cold read plus explicitly attributed synthetic B-machine residues in one isolated profile. No Steam cloud transport, production importer, automatic attribution, or concurrent deletion proof."
        }, Formatting.Indented));
        Log("PASS phase=cloud-residue; loads=" + loads + "; saves=" + saves);
        yield break;
    }
}
