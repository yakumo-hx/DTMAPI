// Historical R37/R38 fixture, excluded from current projects and SDK entrypoints.
// Its build recipe is retired by 20261023-0002; current Manbo is a pure Audio pack.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using DolocTown;
using DolocTown.Config;
using DolocTown.GameData;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;

namespace DTMAPI.Internal.ManboDelayedName;

// Independent, expiring observer. Never invokes the replacement service or installs platform hooks.
public sealed class ModEntry : DtmMod, IDisposable
{
    const string Product = "Yuuka.DTMAPI.ManboCardboardAudio", Event = "PLAY_RESOURCE_PAPER_BOX";
    const string NativeOwner = "DTMAPI.Internal.ManboDelayedName.Native";
    static ModEntry? current;
    readonly Harmony observer = new(NativeOwner);
    IDtmHelper helper = null!;
    Settings settings = null!;
    IAudioReplacementApi audio = null!;
    Type callbacks = null!;
    object runtime = null!, bridge = null!;
    string lastControl = "", operation = "startup";
    bool oldBackground, ready, worldReady, ended, disposed;
    float deadline, endAt;
    int nativeEntries, nativeOriginals, loads, saves;

    public override void Entry(IDtmHelper value)
    {
        helper = value;
        settings = helper.ReadConfig<Settings>();
        Need(Path.IsPathRooted(settings.Evidence) && DateTime.Parse(settings.ExpiresUtc).ToUniversalTime() > DateTime.UtcNow,
            "Fresh, expiring task evidence required.");
        Directory.CreateDirectory(settings.Evidence);
        current = this;
        oldBackground = Application.runInBackground;
        Application.runInBackground = true;
        callbacks = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("DTMAPI.GameBridge.DolocTown.DolocTownHookCallbacks")).First(t => t != null)!;
        audio = helper.ModRegistry.GetApi<IAudioReplacementApi>("DTMAPI.GameBridge.DolocTown")!;
        Need(audio != null, "Public audio API available.");
        // Observer postfix still runs when the production prefix suppresses the native body.
        observer.Patch(AccessTools.Method(typeof(WwiseSoundManager), "InternalPostSoundEvent"),
            postfix: new HarmonyMethod(typeof(ModEntry), nameof(NativeEventReturned)));
        observer.Patch(AccessTools.Method(typeof(DungeonResourceModelPaperBox), "OnInteract"),
            postfix: new HarmonyMethod(typeof(ModEntry), nameof(PaperBoxReturned)));
        foreach (var target in new[] { AccessTools.Method(typeof(DolocAPI), "SaveGame", new[] { typeof(int) }),
            AccessTools.Method(typeof(DataPersistenceManager), "SaveGame", new[] { typeof(int) }) })
            observer.Patch(target, prefix: new HarmonyMethod(typeof(ModEntry), nameof(NoSave)));
        helper.Events.Save.SaveLoaded += (_, e) => { loads++; Record("save-loaded", new { e.SaveSlot }); };
        helper.Events.Save.SaveSaved += (_, _) => { saves++; Fail(new Exception("Unexpected native save.")); };
        helper.Events.GameLoop.UpdateTicked += Tick;
        deadline = Time.realtimeSinceStartup + 480;
        Record("entry-waiting-runtime", new { timing = "Normal bridge initialization is observed on subsequent UpdateTicked." });
    }

    public static void NativeEventReturned(string __0, bool __result, bool __runOriginal)
    {
        if (current == null || __0 != Event) return;
        current.nativeEntries++;
        if (__runOriginal) current.nativeOriginals++;
        current.Record("native-event-return", new { soundEvent = __0, result = __result, originalBodyRan = __runOriginal,
            nativeEntries = current.nativeEntries, nativeOriginals = current.nativeOriginals });
    }
    public static void PaperBoxReturned() => current?.Record("paper-box-OnInteract-return", new { physicalInput = "requires external input evidence" });
    public static void NoSave()
    {
        current?.Fail(new Exception("NoNativeSave entry was reached."));
        throw new InvalidOperationException("A37 NoNativeSave");
    }

    void Tick(object? sender, UpdateTickedEventArgs args)
    {
        try
        {
            Need(Time.realtimeSinceStartup < deadline, "Bounded observation expired.");
            if (!ready)
            {
                runtime = callbacks.GetProperty("Runtime")!.GetValue(null)!;
                bridge = callbacks.GetProperty("Bridge")!.GetValue(null)!;
                if (runtime == null || bridge == null || DolocAPI.userInput?.CurrentState is not HomePageUiState ||
                    DolocConfig.Tables?.TbItem.DataList.Count == 0 || DolocAPI.Sound == null) return;
                Snapshot("runtime-ready");
                Write("ready.json", new { ready = true, nativeTitle = "HomePageUiState with item tables and native sound manager", nativeEntry = "WwiseSoundManager.PostSoundEvent(string, null callback, false) -> actual InternalPostSoundEvent Hook", physicalInput = false });
                ready = true;
            }
            if (!worldReady && loads > 0 && DolocAPI.IsDataLoaded && DolocAPI.CurrentRoom != null && DolocAPI.userInput.CurrentState is NormalGameState)
            {
                worldReady = true;
                Record("world-ready", new { nativeSlot = settings.NativeSlot, nativeState = "NormalGameState" });
                Write("world-ready.json", new { ready = true, nativeSlot = settings.NativeSlot });
            }
            if (endAt > 0 && Time.realtimeSinceStartup >= endAt) { endAt = 0; EndOwner(); }
            string controlPath = Path.Combine(settings.Evidence, "control.txt");
            if (!File.Exists(controlPath)) return;
            string control = File.ReadAllText(controlPath).Trim();
            if (control.Length == 0 || control == lastControl) return;
            lastControl = control;
            string[] parts = control.Split(new[] { '|' }, 2);
            Need(parts.Length == 2, "Control requires a unique nonce and an action.");
            string[] action = parts[1].Split(new[] { ' ' }, 2);
            operation = action.Length > 1 ? action[1] : action[0];
            switch (action[0])
            {
                case "status": Snapshot(operation); break;
                case "load":
                    Need(DolocAPI.userInput.CurrentState is HomePageUiState, "Normal native title is ready before load.");
                    var home = (HomePageUiState)DolocAPI.userInput.CurrentState;
                    home.Kill(); Need(DolocAPI.userInput.TryPopState(home), "Normal native title exit accepted.");
                    Need(DolocAPI.LoadGame(settings.NativeSlot), "Normal native LoadGame accepted."); break;
                case "scan":
                    var boxes = DolocAPI.CurrentRoom.DM_dungeonResource.AllDungeonResources.OfType<DungeonResourceModelPaperBox>().Where(b => b.IsValid).ToArray();
                    Record("paper-box-scan", new { count = boxes.Length,
                        positions = boxes.Select(b => b.PositionCenter.ToString()).ToArray() });
                    break;
                case "box":
                    var box = DolocAPI.CurrentRoom.DM_dungeonResource.AllDungeonResources.OfType<DungeonResourceModelPaperBox>().First(b => b.IsValid);
                    Record("native-event-request", new { route = "programmatic actual paper-box OnInteract", soundEvent = Event,
                        resource = box.ResourceName, position = box.PositionCenter.ToString(), physicalInput = false });
                    box.OnInteract(); Snapshot(operation); break;
                case "event": PostEvent(); break;
                case "name": DemandName(); break;
                case "missing-wav": RemoveWavAndSaveProductPage(); break;
                case "end-playing": PostEvent(); endAt = Time.realtimeSinceStartup + 0.12f; break;
                case "end-owner": EndOwner(); break;
                case "quit": Snapshot("before-normal-quit"); Record("normal-quit", new { loads, saves }); Application.Quit(); break;
                default: throw new Exception("Unknown A37 action: " + action[0]);
            }
            Write("ack.json", new { control, utc = DateTimeOffset.UtcNow.ToString("o"), operation });
        }
        catch (Exception error) { Fail(error); }
    }

    void PostEvent()
    {
        Need(DolocAPI.Sound != null, "Actual native sound manager exists.");
        Record("native-event-request", new { route = "programmatic complete native PostSoundEvent", soundEvent = Event, physicalInput = false });
        bool result = DolocAPI.Sound!.PostSoundEvent(Event, null, false);
        Record("native-request-return", new { result, acousticEvidence = "separate WASAPI capture required" });
        Snapshot(operation);
    }

    void DemandName()
    {
        string proof = Path.Combine(settings.Evidence, "audio-first.result.json");
        Need(File.Exists(proof) && JsonConvert.DeserializeObject<AudioProof>(File.ReadAllText(proof))!.Passed,
            "Device output matched the product WAV before the first name query.");
        Need(helper.Diagnostics.GetHookStatuses().Single(h => h.HookId == "Audio.SoundEventReplacement").Status == "verified",
            "First actual native event produced behavioral verification.");
        Snapshot("before-first-name-demand");
        var names = helper.ModRegistry.GetApi<IItemDisplayNameApi>("DTMAPI.GameBridge.DolocTown");
        Need(names != null, "Public name API available.");
        string item = DolocConfig.Tables.TbItem.DataList.First().Id;
        bool found = names!.TryGetDisplayName(item, out string title);
        Need(found && !string.IsNullOrWhiteSpace(title), "Real public name API resolves an actual native item.");
        Record("first-public-name-query", new { item, title, consumer = "independent QA; Animal Bell UI not exercised" });
        Snapshot("after-first-name-demand");
    }

    void RemoveWavAndSaveProductPage()
    {
        Snapshot("before-missing-wav");
        string path = audio.GetState(Product).Replacements.Single().AudioPath;
        string suffix = Path.Combine("DTMAPI_A37_Manbo", "Content", "DTMAPI", "assets", "manbo.wav");
        Need(path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) && File.Exists(path), "Only this task's admitted product WAV may be removed.");
        string retained = Path.Combine(settings.Evidence, "removed-manbo-after-admission.wav");
        Need(!File.Exists(retained), "One missing-WAV preparation only.");
        File.Copy(path, retained); File.Delete(path);
        var menu = runtime.GetType().GetProperty("ConfigMenuRuntime", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(runtime)!;
        var contract = menu.GetType().GetInterface("DTMAPI.Abstractions.IConfigMenuRuntime")!;
        var page = contract.GetMethod("GetPage")!.Invoke(menu, new object[] { Product })!;
        Need(page != null, "Actual optional Manbo config page exists.");
        page.GetType().GetMethod("Save")!.Invoke(page, null);
        Record("product-config-save", new { route = "programmatic existing ConfigMenuPage.Save -> registered product save callback -> public RegisterReplacement",
            physicalUi = false, removed = path, retained });
        Snapshot("after-missing-wav-config-save");
        Need(!audio.GetState(Product).Replacements.Single().PreloadReady, "Actual product re-registration is no longer ready.");
    }

    void EndOwner()
    {
        Need(!ended, "One exact owner deactivation only.");
        Snapshot("before-owner-end");
        var target = runtime.GetType().GetMethod("DeactivateOwner", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var reason = Enum.Parse(target.GetParameters()[1].ParameterType, "Unload");
        string result = (string)target.Invoke(runtime, new[] { (object)Product, reason, false, "" })!;
        ended = true;
        Record("core-owner-deactivation", new { owner = Product, result, command = "controlled QA calls existing Core owner deactivation; no manual service removal" });
        Need(result.Contains("remaining=0") && !result.Contains("deactivationDeferred=true"), "Core owner cleanup proves zero remaining.");
        Snapshot("after-owner-end");
    }

    void Snapshot(string label)
    {
        var demand = runtime.GetType().GetField("demandCoordinator", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(runtime)!;
        object demandSnapshot = demand.GetType().GetMethod("GetSnapshot", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(demand, null)!;
        var feature = bridge.GetType().GetField("audioReplacementFeature", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(bridge)!;
        var service = feature.GetType().GetProperty("Service", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(feature)!;
        string lifecycle = (string)service.GetType().GetMethod("GetAudioReplacementLifecycleSummary", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(service, null)!;
        Record("snapshot", new { label, state = audio.GetState(Product), hooks = helper.Diagnostics.GetHookStatuses().Where(h =>
                h.HookId.Contains("Audio.") || h.HookId.Contains("ItemDisplayName")),
            demands = demandSnapshot, lifecycle, warnings = helper.Diagnostics.GetWarnings().Where(w =>
                w.Message.Contains("install signal repeated")),
            nativeEntries, nativeOriginals, loads, saves, ended });
    }

    void Record(string kind, object data)
    {
        File.AppendAllText(Path.Combine(settings.Evidence, "events.jsonl"), JsonConvert.SerializeObject(new
            { utc = DateTimeOffset.UtcNow.ToString("o"), operation, kind, data }) + "\n");
        helper.Monitor.Log("A37 " + kind + " operation=" + operation);
    }
    void Write(string name, object data) => File.WriteAllText(Path.Combine(settings.Evidence, name), JsonConvert.SerializeObject(data, Formatting.Indented));
    static void Need(bool condition, string message) { if (!condition) throw new Exception(message); }
    void Fail(Exception error) { Write("failure.json", new { operation, error = error.ToString(), nativeEntries, nativeOriginals, loads, saves }); Application.Quit(); }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Record("runtime-dispose", new { loads, saves, ended });
        observer.UnpatchSelf(); Application.runInBackground = oldBackground; current = null;
    }
    public sealed class Settings
    {
        public string Evidence { get; set; } = "";
        public string ExpiresUtc { get; set; } = "";
        public int NativeSlot { get; set; } = 1;
    }
    public sealed class AudioProof { public bool Passed { get; set; } }
}
