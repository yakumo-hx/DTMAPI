using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DTMAPI.Abstractions;
using DolocTown;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DTMAPI.Internal.MesStorageProbe;

public sealed partial class ModEntry : DtmMod, IDisposable
{
    const string Owner = "DTMAPI.Internal.MesStorageProbe.Native";
    const string KeyA = "$DTMAPI__MES_storage_probe_v1";
    const string KeyB = "$DTMAPI__independent_owner_probe_v1";
    static ModEntry? self;
    readonly Harmony harmony = new Harmony(Owner);
    IDtmHelper helper = null!;
    IEnumerator<int>? run;
    string phase = "", evidence = "", persistent = "";
    bool done, allowSave, oldBackground;
    float deadline;
    int saves, loads, rejectedSaves;
    object Handler => Read(DolocAPI.dataPersistenceManager, "fileDataHandler")!;
    DialogueVariableStorage Storage => (DialogueVariableStorage)Read(DolocAPI.archiveHandle.cityData.dialogueManager, "variableStorage")!;
    string SavePath => (string)AccessTools.Method(Handler.GetType(), "GetDataFullPath").Invoke(Handler, new object[] { 4 });

    public override void Entry(IDtmHelper helper)
    {
        this.helper = helper;
        oldBackground = Application.runInBackground;
        Application.runInBackground = true;
        phase = Environment.GetEnvironmentVariable("DTMAPI_MES_STORAGE_PHASE") ?? "";
        evidence = Environment.GetEnvironmentVariable("DTMAPI_MES_STORAGE_EVIDENCE") ?? "";
        persistent = Environment.GetEnvironmentVariable("DTMAPI_MES_STORAGE_PERSISTENT") ?? "";
        Check(phase == "seed" || phase == "stage" || phase == "verify" || phase == "family" || phase == "migrate" || phase == "migrate-cold" || phase == "cloud-residue", "known phase " + phase);
        Check(File.Exists(Path.Combine(persistent, ".dtmapi-disposable-save-fixture.json")), "marked disposable profile");
        Check(Path.GetFullPath(Application.persistentDataPath) == Path.GetFullPath(persistent), "Unity profile isolated without a save redirect");
        Directory.CreateDirectory(evidence);
        self = this;
        harmony.Patch(AccessTools.Method(typeof(DolocAPI), "SaveGame"), prefix: new HarmonyMethod(typeof(ModEntry), nameof(SaveGuard)));
        helper.Events.GameLoop.UpdateTicked += Tick;
        helper.Events.Save.SaveLoaded += Loaded;
        helper.Events.Save.SaveSaved += Saved;
        deadline = Time.realtimeSinceStartup + 160;
        run = Execute();
        Log("ARMED phase=" + phase + "; persistent=" + Application.persistentDataPath);
    }
    static bool SaveGuard(ref bool __result)
    {
        if (self?.allowSave == true) return true;
        if (self != null) self.rejectedSaves++;
        self?.Log("FAIL unexpected save"); __result = false; return false;
    }
    void Loaded(object? sender, SaveLoadedEventArgs e) { loads++; Check(e.SaveSlot == 4 || (phase == "family" && e.SaveSlot == 0), "allowed native slot " + e.SaveSlot); }
    void Saved(object? sender, SaveSavedEventArgs e) { saves++; }
    void Tick(object? sender, UpdateTickedEventArgs e)
    {
        if (done) return;
        try
        {
            if (Time.realtimeSinceStartup > deadline) throw new Exception("experiment deadline");
            if (run != null && !run.MoveNext()) { run.Dispose(); run = null; done = true; }
        }
        catch (Exception ex)
        {
            Log("FAIL " + ex); File.WriteAllText(Path.Combine(evidence, phase + "-failure.txt"), ex.ToString());
            done = true; run?.Dispose(); run = null; Application.Quit();
        }
    }
    IEnumerator<int> Execute()
    {
        foreach (int v in Home()) yield return v;
        if (phase == "cloud-residue") PrepareCloudResidues();
        DolocAPI.userInput.ClearState(DolocAPI.gameStateManager.normalGameState);
        Check(DolocAPI.LoadGame(4), "native load");
        foreach (int v in Ready()) yield return v;
        Check(Path.GetDirectoryName(Path.GetFullPath(SavePath)) == Path.GetFullPath(Path.Combine(persistent, "SAVE")), "native file destination isolated");
        if (phase.StartsWith("migrate", StringComparison.Ordinal) || phase == "cloud-residue")
        {
            var steps = phase == "cloud-residue" ? CloudResiduePhase() : MigrationPhase();
            foreach (int v in steps) yield return v;
            DolocAPI.ReturnHome();
            // This fixture can unload the probe on ReturnHome. Do not depend
            // on another owner tick to finish exiting.
            Application.Quit();
            yield break;
        }
        string expectedPath = Path.Combine(evidence, "expected-payloads.json");
        Dictionary<string, string> expected;
        if (phase == "seed")
        {
            expected = new Dictionary<string, string> {
                [KeyA] = JsonConvert.SerializeObject(new { owner = "DTMAPI.MoreEquipmentSlotsMod", schema = 1, text = "中文引号\"反斜线\\换行\n", padding = new string('m', 8192) }),
                [KeyB] = JsonConvert.SerializeObject(new { owner = "DTMAPI.Internal.Control", schema = 991, untouched = "opaque-future-payload" })
            };
            Check(!Storage.Contains(KeyA) && !Storage.Contains(KeyB), "fresh namespace");
            File.WriteAllText(Path.Combine(evidence, "seed-story-before.json"), JsonConvert.SerializeObject(Storage, Formatting.Indented));
            foreach (var pair in expected) Storage.SetValue(pair.Key, pair.Value);
            File.WriteAllText(expectedPath, JsonConvert.SerializeObject(expected, Formatting.Indented));
            allowSave = true;
            try { Check(DolocAPI.SaveGame(4), "native SaveGame returns true"); }
            finally { allowSave = false; }
            DumpDisk("seed-committed");
            Storage.SetValue(KeyA, "UNSAVED-MUTATION");
            DolocAPI.ReturnHome();
            foreach (int v in Home()) yield return v;
            DolocAPI.userInput.ClearState(DolocAPI.gameStateManager.normalGameState);
            Check(DolocAPI.LoadGame(4), "unsaved return and reload");
            foreach (int v in Ready()) yield return v;
        }
        else expected = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(expectedPath))!;
        foreach (var pair in expected)
            Check(Storage.TryGetValue<string>(pair.Key, out string value) && value == pair.Value, "exact string and type " + pair.Key);
        if (phase == "family") foreach (int v in CheckNativeFileFamily(expected)) yield return v;
        if (phase == "stage")
        {
            // Fixture preparation only. Vanilla ItemSleepingBag uses the game's own
            // dialogue and sleep/save path after this probe and loader are removed.
            var inventory = DolocAPI.archiveHandle.InventorySystem.inventory;
            Check(inventory.FirstEmptyIndex >= 0, "fixture has room to preserve original first item");
            var oldItem = inventory.SwapItem(0, DolocAPI.GenerateItem("sleeping_bag", 2));
            Check(oldItem == null || inventory.PlaceItem(oldItem) == null, "original fixture item retained");
            allowSave = true;
            try { Check(DolocAPI.SaveGame(4), "native fixture preparation saved"); }
            finally { allowSave = false; }
        }
        DumpDisk(phase + "-observed");
        File.WriteAllText(Path.Combine(evidence, phase + "-story-after.json"), JsonConvert.SerializeObject(Storage, Formatting.Indented));
        File.WriteAllText(Path.Combine(evidence, phase + "-result.json"), JsonConvert.SerializeObject(new { passed = true, phase, loads, saves, persistent = Application.persistentDataPath, keys = expected.Keys }, Formatting.Indented));
        Log("PASS phase=" + phase + "; loads=" + loads + "; saves=" + saves);
        DolocAPI.ReturnHome();
        Application.Quit();
    }
    void DumpDisk(string name)
    {
        // Evidence only; never used as a game load source.
        File.WriteAllText(Path.Combine(evidence, name + "-archive.json"), ReadDisk().ToString(Formatting.None));
    }
    JObject ReadDisk() => JObject.Parse((string)AccessTools.Method(Handler.GetType(), "TryDecryptData").Invoke(Handler, new object[] { File.ReadAllText(SavePath) }));
    IEnumerable<int> CheckNativeFileFamily(Dictionary<string, string> expected)
    {
        var local = (DolocTown.GameData.LocalSave)Handler;
        string copyPath = Path.Combine(persistent, "SAVE", "doloc-save-0.data");
        Check(!File.Exists(copyPath) && !File.Exists(copyPath + ".bak"), "native first copy target is unused fixture slot 0");
        string sourceDigest = Digest(File.ReadAllBytes(SavePath));
        Check(local.DuplicateGame(4, out int target) && target == 0, "native DuplicateGame selects fixture slot 0");
        Check(local.LoadGame(target, out var copy) && copy != null, "native copied file decodes");
        var copyStorage = (DialogueVariableStorage)Read(copy!.cityData.dialogueManager, "variableStorage")!;
        foreach (var pair in expected)
            Check(copyStorage.TryGetValue<string>(pair.Key, out string value) && value == pair.Value, "copy carries exact owner string " + pair.Key);
        // Inactive deserialized graphs have not received native AfterLoadData.
        // Load the copy normally before asking the game to save a rename.
        DolocAPI.ReturnHome();
        foreach (int v in Home()) yield return v;
        DolocAPI.userInput.ClearState(DolocAPI.gameStateManager.normalGameState);
        Check(DolocAPI.LoadGame(target), "native copy enters full load lifecycle");
        foreach (int v in Ready()) yield return v;
        DolocAPI.archiveHandle.farmData.agentData.customPlayerName = "MES copied and renamed";
        allowSave = true;
        try { Check(DolocAPI.SaveGame(target), "renamed active copy native write"); }
        finally { allowSave = false; }
        Check(local.LoadGame(target, out var renamed) && renamed != null, "renamed copy native read is non-null");
        var renamedStorage = (DialogueVariableStorage)Read(renamed!.cityData.dialogueManager, "variableStorage")!;
        foreach (var pair in expected)
            Check(renamedStorage.TryGetValue<string>(pair.Key, out string value) && value == pair.Value, "rename retains owner string " + pair.Key);
        DolocAPI.ReturnHome();
        foreach (int v in Home()) yield return v;
        Check(local.DeleteGame(target) && !File.Exists(copyPath) && File.Exists(copyPath + ".bak"), "native delete moves copied full to bak");
        DolocAPI.dataPersistenceManager.NewGame(target);
        var freshStorage = Storage;
        foreach (var pair in expected) Check(!freshStorage.Contains(pair.Key), "same slot fresh native container has no inherited key " + pair.Key);
        Check(Digest(File.ReadAllBytes(SavePath)) == sourceDigest, "copy rename delete and fresh container leave source bytes unchanged");
        File.WriteAllText(Path.Combine(evidence, "family-details.json"), JsonConvert.SerializeObject(new { passed = true, target, sourceDigest, freshContainerOnly = true, limit = "No new-game tutorial/save flow or MoreSaves UI acceptance; no disk backup imported into the fresh object." }, Formatting.Indented));
        yield break;
    }
    IEnumerable<int> Ready()
    {
        while (!DolocAPI.IsDataLoaded || !DolocAPI.IsNormalState || DolocAPI.agent == null || !DolocAPI.agent.gameObject.activeInHierarchy) yield return 0;
        foreach (int v in Wait(1)) yield return v;
    }
    static IEnumerable<int> Home()
    {
        while (DolocAPI.userInput == null || !(DolocAPI.userInput.CurrentState is HomePageUiState)) yield return 0;
        foreach (int v in Wait(1)) yield return v;
    }
    static IEnumerable<int> Wait(float seconds) { float until = Time.realtimeSinceStartup + seconds; while (Time.realtimeSinceStartup < until) yield return 0; }
    static object? Read(object value, string name) => AccessTools.Field(value.GetType(), name).GetValue(value);
    void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); Log("check " + message); }
    void Log(string text) => Monitor.Log("MESStorageProbe " + text);
    public void Dispose()
    {
        // Runtime removes event subscriptions before calling Dispose.
        harmony.UnpatchSelf(); self = null; run?.Dispose(); run = null;
        Application.runInBackground = oldBackground;
        Log("cleanup hooks=0;roots=0");
    }
}
